using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Config;
using AskMeFirst.Core.Models;
using AskMeFirst.Core.Routing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.Picker.ViewModels;

public sealed partial class PickerWindowViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan UnshortenTimeout = TimeSpan.FromSeconds(1);

    private readonly PickerRequest _request;
    private readonly IConfigWriter? _configWriter;
    private readonly IIconProvider _icons;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    private PickerResult _result = new Cancelled();

    public PickerWindowViewModel(
        PickerRequest request,
        ILogger logger,
        IConfigWriter? configWriter = null,
        IIconProvider? icons = null)
    {
        _request = request;
        _logger = logger;
        _configWriter = configWriter;
        _icons = icons ?? new NullIconProvider();

        DisplayUrl = request.OriginalUrl.ToString();
        SourceAppLabel = request.SourceApp is null ? "" : $"From {request.SourceApp}";
        BrowserOptions = BuildBrowserOptions(request);
        RememberOptions = BuildRememberOptions(request);
        SelectedBrowserIndex = 0;
        Status = PickerStatus.Loading;

        RememberOptionViewModel firstAvailable = RememberOptions.First(o => o.IsAvailable);
        firstAvailable.IsSelected = true;

        if (request.UnshortenTask is not null)
        {
            _ = ResolveUnshortenerAsync();
        }
        else
        {
            Status = PickerStatus.Ready;
        }
    }

    public ObservableCollection<BrowserOptionViewModel> BrowserOptions { get; }

    public ObservableCollection<RememberOptionViewModel> RememberOptions { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CommitCommand))]
    private PickerStatus _status;

    [ObservableProperty]
    private string _displayUrl = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSourceAppLabelVisible))]
    private string _sourceAppLabel = "";

    [ObservableProperty]
    private bool _isResolving;

    [ObservableProperty]
    private int _selectedBrowserIndex;

    public bool IsSourceAppLabelVisible => !string.IsNullOrEmpty(SourceAppLabel);

    public PickerResult Result => _result;

    [RelayCommand(CanExecute = nameof(CanCommit))]
    private void Commit()
    {
        if (SelectedBrowserIndex < 0 || SelectedBrowserIndex >= BrowserOptions.Count)
        {
            _logger.LogWarn("Commit aborted: no browser selected.");
            return;
        }

        RememberOptionViewModel? rememberOpt = RememberOptions.FirstOrDefault(o => o.IsSelected);
        if (rememberOpt is null)
        {
            _logger.LogWarn("Commit aborted: no remember option selected.");
            return;
        }

        Status = PickerStatus.Committing;
        _cts.Cancel();

        BrowserOptionViewModel browserOpt = BrowserOptions[SelectedBrowserIndex];
        Browser browser = browserOpt.Browser;
        if (browserOpt.Profile is not null)
        {
            browser = browser with { Profile = browserOpt.Profile };
        }

        if (rememberOpt.Kind != RememberKind.Once && _configWriter is not null)
        {
            Rule rule = BuildRememberRule(rememberOpt.Kind);
            _ = WriteRuleAsync(rule);
        }

        _logger.LogInfo($"Picker committed: {browser.DisplayName} ({rememberOpt.Kind})");
        _result = new Launched(browser, _request.OriginalUrl);
        Status = PickerStatus.Done;
    }

    private bool CanCommit() => Status is PickerStatus.Ready or PickerStatus.Resolving;

    [RelayCommand]
    private void Cancel()
    {
        if (Status == PickerStatus.Done)
        {
            return;
        }
        _cts.Cancel();
        _logger.LogInfo("Picker cancelled by user.");
        _result = new Cancelled();
        Status = PickerStatus.Done;
    }

    public void CancelIfNotDone()
    {
        if (Status != PickerStatus.Done)
        {
            Cancel();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task ResolveUnshortenerAsync()
    {
        if (_request.UnshortenTask is null)
        {
            return;
        }
        Status = PickerStatus.Resolving;
        IsResolving = true;
        try
        {
            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            timeoutCts.CancelAfter(UnshortenTimeout);
            string? resolved = await _request.UnshortenTask.WaitAsync(timeoutCts.Token).ConfigureAwait(true);
            if (resolved is not null)
            {
                DisplayUrl = resolved;
                _logger.LogInfo($"Unshortener resolved to {resolved}");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarn($"Unshortener failed: {ex.Message}");
        }
        finally
        {
            IsResolving = false;
            if (Status != PickerStatus.Done)
            {
                Status = PickerStatus.Ready;
            }
        }
    }

    private ObservableCollection<BrowserOptionViewModel> BuildBrowserOptions(PickerRequest request)
    {
        ObservableCollection<BrowserOptionViewModel> options = [];
        for (int i = 0; i < request.AvailableBrowsers.Count; i++)
        {
            PickerBrowserOption opt = request.AvailableBrowsers[i];
            int hotkey = i < 9 ? i + 1 : -1;
            options.Add(new BrowserOptionViewModel(opt.Browser, opt.Profile, hotkey, _icons));
        }
        return options;
    }

    private static ObservableCollection<RememberOptionViewModel> BuildRememberOptions(PickerRequest request)
    {
        bool hasSource = !string.IsNullOrEmpty(request.SourceApp);
        string host = request.OriginalUrl.Host;
        bool hasHost = !string.IsNullOrEmpty(host);

        return
        [
            new(RememberKind.Once, "Just this once"),
            new(RememberKind.AlwaysExactHost, $"Always {host}", isAvailable: hasHost, unavailableReason: "No host in URL"),
            new(RememberKind.AlwaysWildcardHost, $"Always *.{host}", isAvailable: hasHost, unavailableReason: "No host in URL"),
            new(RememberKind.AlwaysSource, $"Always {request.SourceApp ?? ""}", isAvailable: hasSource, unavailableReason: "Source app not detected"),
            new(RememberKind.SourcePlusHost, $"{request.SourceApp ?? "?"} + {host}", isAvailable: hasSource && hasHost, unavailableReason: "Need source + host"),
        ];
    }

    private Rule BuildRememberRule(RememberKind kind)
    {
        string? sourceApp = _request.SourceApp;
        string host = _request.OriginalUrl.Host;

        RuleWhen when = kind switch
        {
            RememberKind.AlwaysExactHost    => new RuleWhen { UrlMatchesAny = [host] },
            RememberKind.AlwaysWildcardHost => new RuleWhen { UrlMatchesAny = [$"*.{host}"] },
            RememberKind.AlwaysSource       => new RuleWhen { ProcessIn = sourceApp is null ? [] : [sourceApp] },
            RememberKind.SourcePlusHost     => new RuleWhen { ProcessIn = sourceApp is null ? [] : [sourceApp], UrlMatchesAny = [host] },
            _                               => new RuleWhen(),
        };

        string name = kind switch
        {
            RememberKind.AlwaysExactHost    => $"Remembered: * {host}",
            RememberKind.AlwaysWildcardHost => $"Remembered: * *.{host}",
            RememberKind.AlwaysSource       => $"Remembered: {sourceApp ?? ""}",
            RememberKind.SourcePlusHost     => $"Remembered: {sourceApp ?? ""} + {host}",
            _                               => "Remembered",
        };

        BrowserOptionViewModel browserOpt = BrowserOptions[SelectedBrowserIndex];
        RuleThen then = new()
        {
            Browser = browserOpt.Browser.Id,
            StripTracking = true,
        };

        return new Rule
        {
            Name = name,
            Priority = 50,
            When = when,
            Then = then,
        };
    }

    private async Task WriteRuleAsync(Rule rule)
    {
        if (_configWriter is null)
        {
            return;
        }
        try
        {
            await Task.Run(() => _configWriter.AppendRule(rule)).ConfigureAwait(false);
            _logger.LogInfo($"Wrote remember rule: {rule.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to write remember rule: {ex.Message}");
        }
    }
}