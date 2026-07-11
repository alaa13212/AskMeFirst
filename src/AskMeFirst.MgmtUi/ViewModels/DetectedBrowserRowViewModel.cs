using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class DetectedBrowserRowViewModel : RowViewModelBase
{
    private readonly DetectedBrowser _detected;
    private readonly IConfigPersister _persister;
    private bool _hasAddButton;

    public DetectedBrowserRowViewModel(DetectedBrowser detected, IConfigPersister persister, bool withAddButton)
    {
        _detected = detected;
        _persister = persister;
        _hasAddButton = withAddButton;
    }

    public string Id => _detected.Browser.Id;

    public string DisplayName => _detected.Browser.DisplayName;

    public string ExecutablePath => _detected.Browser.ExecutablePath;

    public int ProfileCount => _detected.Profiles.Count;

    public bool HasAddButton => _hasAddButton;

    public override string Label => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;

    [RelayCommand]
    private void AddToConfig()
    {
        _persister.Save(c =>
        {
            BrowserSpec fresh = new()
            {
                Id = _detected.Browser.Id,
                DisplayName = _detected.Browser.DisplayName,
                Executable = _detected.Browser.ExecutablePath,
            };
            return c with { Browsers = [.. c.Browsers, fresh] };
        });
    }
}
