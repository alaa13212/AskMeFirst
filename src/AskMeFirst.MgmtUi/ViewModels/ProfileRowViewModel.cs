using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class ProfileRowViewModel : DraftableRowViewModel
{
    private readonly ProfileSpec _source;
    private readonly IConfigPersister _persister;

    public ProfileRowViewModel(ProfileSpec source, IConfigPersister persister)
        : base(persister)
    {
        _source = source;
        _persister = persister;
        _displayName = source.DisplayName ?? "";
        _pinned = source.Pinned;
    }

    public ProfileSpec Source => _source;

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private bool _pinned;

    partial void OnDisplayNameChanged(string value) => NotifyDraftStateChanged();

    partial void OnPinnedChanged(bool value)
    {
        TryAutosavePinned(value);
        OnPropertyChanged(nameof(HasDraft));
    }

    public override string Label
    {
        get
        {
            string name = _source.Name ?? _source.DisplayName ?? _source.Id;
            string browser = string.IsNullOrEmpty(_source.BrowserId) ? "" : $" ({_source.BrowserId})";
            return string.IsNullOrWhiteSpace(name) ? "(unnamed profile)" : name + browser;
        }
    }

    public override bool HasDraft => DisplayName != (_source.DisplayName ?? "");

    protected override Func<AppConfig, AppConfig> BuildMutator()
    {
        string newDisplayName = DisplayName;
        return config =>
        {
            List<ProfileSpec> updated = [];
            foreach (ProfileSpec existing in config.Profiles)
            {
                if (ReferenceEquals(existing, _source) || existing.Id == _source.Id)
                {
                    updated.Add(existing with
                    {
                        DisplayName = string.IsNullOrWhiteSpace(newDisplayName) ? null : newDisplayName,
                    });
                }
                else
                {
                    updated.Add(existing);
                }
            }
            return config with { Profiles = updated };
        };
    }

    protected override void ResetDrafts()
    {
        DisplayName = _source.DisplayName ?? "";
    }

    protected override void OnSaveCommitted()
    {
        ResetDrafts();
    }

    private void TryAutosavePinned(bool pinned)
    {
        _persister.Save(c =>
        {
            List<ProfileSpec> updated = [];
            foreach (ProfileSpec existing in c.Profiles)
            {
                ProfileSpec replaced = (ReferenceEquals(existing, _source) || existing.Id == _source.Id)
                    ? existing with { Pinned = pinned }
                    : existing;
                updated.Add(replaced);
            }
            return c with { Profiles = updated };
        });
    }
}
