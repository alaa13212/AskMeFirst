using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class HttpUnshortenerTests
{
    [Fact]
    public async Task ResolveAsync_SuccessWithFinalUrl_ReturnsFinalUrl()
    {
        HttpRequestMessage finalReq = new(HttpMethod.Head, new Uri("https://example.com/final"));
        RecordingHandler handler = new(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            RequestMessage = finalReq,
        });
        using HttpUnshortener sut = Build(handler);

        string? result = await sut.ResolveAsync(new Uri("https://t.co/abc"), CancellationToken.None);

        Assert.Equal("https://example.com/final", result);
    }

    [Fact]
    public async Task ResolveAsync_SuccessWithoutRedirect_ReturnsOriginalUrl()
    {
        using HttpUnshortener sut = Build(new RecordingHandler(new HttpResponseMessage(System.Net.HttpStatusCode.OK)));

        string? result = await sut.ResolveAsync(new Uri("https://t.co/abc"), CancellationToken.None);

        Assert.Equal("https://t.co/abc", result);
    }

    [Fact]
    public async Task ResolveAsync_NotFound_ReturnsNull()
    {
        using HttpUnshortener sut = Build(new RecordingHandler(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)));

        string? result = await sut.ResolveAsync(new Uri("https://t.co/missing"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_HeadBlocked_ReturnsNull()
    {
        using HttpUnshortener sut = Build(new RecordingHandler(new HttpResponseMessage(System.Net.HttpStatusCode.MethodNotAllowed)));

        string? result = await sut.ResolveAsync(new Uri("https://t.co/blocked"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_ServerError_ReturnsNull()
    {
        using HttpUnshortener sut = Build(new RecordingHandler(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)));

        string? result = await sut.ResolveAsync(new Uri("https://t.co/error"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_Cancelled_ReturnsNull()
    {
        using HttpUnshortener sut = Build(new BlockingHandler(), TimeSpan.FromSeconds(10));
        using CancellationTokenSource cts = new();

        Task<string?> task = sut.ResolveAsync(new Uri("https://t.co/slow"), cts.Token);
        cts.Cancel();
        string? result = await task;

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_NetworkError_ReturnsNull()
    {
        using HttpUnshortener sut = Build(new ThrowingHandler(new HttpRequestException("connection refused")));

        string? result = await sut.ResolveAsync(new Uri("https://t.co/down"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_SetsUserAgent()
    {
        RecordingHandler handler = new(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        using HttpUnshortener sut = Build(handler);

        await sut.ResolveAsync(new Uri("https://t.co/abc"), CancellationToken.None);

        Assert.NotNull(handler.Requests[0].Headers.UserAgent);
        Assert.Equal("AskMeFirst/1.0 (Unshortening)", handler.Requests[0].Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task ResolveAsync_UsesHeadMethod()
    {
        RecordingHandler handler = new(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        using HttpUnshortener sut = Build(handler);

        await sut.ResolveAsync(new Uri("https://t.co/abc"), CancellationToken.None);

        Assert.Equal(HttpMethod.Head, handler.Requests[0].Method);
    }

    private static HttpUnshortener Build(RecordingHandler handler, TimeSpan? timeout = null)
    {
        return new HttpUnshortener(handler, timeout ?? TimeSpan.FromSeconds(1), new FakeLogger());
    }

    private static HttpUnshortener Build(BlockingHandler handler, TimeSpan timeout)
    {
        return new HttpUnshortener(handler, timeout, new FakeLogger());
    }

    private static HttpUnshortener Build(ThrowingHandler handler)
    {
        return new HttpUnshortener(handler, TimeSpan.FromSeconds(1), new FakeLogger());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses;
        public List<HttpRequestMessage> Requests { get; } = [];

        public RecordingHandler(params HttpResponseMessage[] responses)
        {
            this.responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (responses.Count == 0)
            {
                HttpResponseMessage fallback = new(System.Net.HttpStatusCode.OK);
                fallback.RequestMessage = request;
                return Task.FromResult(fallback);
            }
            HttpResponseMessage resp = responses.Dequeue();
            if (resp.RequestMessage is null)
            {
                resp.RequestMessage = request;
            }
            return Task.FromResult(resp);
        }
    }

    private sealed class BlockingHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource<HttpResponseMessage> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            return tcs.Task;
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Exception toThrow;

        public ThrowingHandler(Exception toThrow)
        {
            this.toThrow = toThrow;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(toThrow);
        }
    }
}