using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class RuleRowViewModel : DraftableRowViewModel
{
    private readonly Rule _source;

    public RuleRowViewModel(Rule source, IConfigPersister persister) : base(persister)
    {
        _source = source;
        _name = source.Name;
        _priority = source.Priority;
        _urlRegex = source.When?.UrlRegex ?? "";
    }

    public Rule Source => _source;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private int _priority;

    [ObservableProperty]
    private string _urlRegex;

    partial void OnNameChanged(string value) => NotifyDraftStateChanged();

    partial void OnPriorityChanged(int value) => NotifyDraftStateChanged();

    partial void OnUrlRegexChanged(string value) => NotifyDraftStateChanged();

    public override string Label => string.IsNullOrWhiteSpace(_source.Name) ? "(unnamed rule)" : _source.Name;

    public override bool HasDraft =>
        Name != _source.Name
        || Priority != _source.Priority
        || UrlRegex != (_source.When?.UrlRegex ?? "");

    protected override Func<AppConfig, AppConfig> BuildMutator()
    {
        string newName = Name;
        int newPriority = Priority;
        string newUrlRegex = UrlRegex;
        return config =>
        {
            List<Rule> updated = [];
            foreach (Rule existing in config.Rules)
            {
                if (ReferenceEquals(existing, _source) || existing.Name == _source.Name)
                {
                    updated.Add(existing with
                    {
                        Name = newName,
                        Priority = newPriority,
                        When = MergeUrlRegex(existing.When, newUrlRegex),
                    });
                }
                else
                {
                    updated.Add(existing);
                }
            }
            return config with { Rules = updated };
        };
    }

    protected override void ResetDrafts()
    {
        Name = _source.Name;
        Priority = _source.Priority;
        UrlRegex = _source.When?.UrlRegex ?? "";
    }

    protected override void OnSaveCommitted()
    {
        ResetDrafts();
    }

    private static RuleWhen MergeUrlRegex(RuleWhen? existing, string urlRegex)
    {
        RuleWhen current = existing ?? new RuleWhen();
        return string.IsNullOrWhiteSpace(urlRegex)
            ? current with { UrlRegex = null }
            : current with { UrlRegex = urlRegex };
    }
}
