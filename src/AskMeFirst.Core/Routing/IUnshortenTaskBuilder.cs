namespace AskMeFirst.Core.Routing;

public interface IUnshortenTaskBuilder
{
    Task<string?>? Build(Uri url);
}
