using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class BrowserRowViewModel : DraftableRowViewModel
{
    private readonly BrowserSpec _source;

    public BrowserRowViewModel(BrowserSpec source, IConfigPersister persister)
        : base(persister)
    {
        _source = source;
        _displayName = source.DisplayName;
        _executable = source.Executable;
    }

    public BrowserSpec Source => _source;

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private string _executable;

    partial void OnDisplayNameChanged(string value) => NotifyDraftStateChanged();

    partial void OnExecutableChanged(string value) => NotifyDraftStateChanged();

    public override string Label => string.IsNullOrWhiteSpace(_source.Id) ? "(unnamed browser)" : _source.Id;

    public override bool HasDraft =>
        DisplayName != _source.DisplayName
        || Executable != _source.Executable;

    protected override Func<AppConfig, AppConfig> BuildMutator()
    {
        string newDisplayName = DisplayName;
        string newExecutable = Executable;
        return config =>
        {
            List<BrowserSpec> updated = [];
            foreach (BrowserSpec existing in config.Browsers)
            {
                if (existing.Id == _source.Id)
                {
                    updated.Add(existing with
                    {
                        DisplayName = newDisplayName,
                        Executable = newExecutable,
                    });
                }
                else
                {
                    updated.Add(existing);
                }
            }
            return config with { Browsers = updated };
        };
    }

    protected override void ResetDrafts()
    {
        DisplayName = _source.DisplayName;
        Executable = _source.Executable;
    }

    protected override void OnSaveCommitted()
    {
        ResetDrafts();
    }
}
