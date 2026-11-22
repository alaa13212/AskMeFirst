using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class DraftableStringRowViewModel : DraftableRowViewModel
{
    private readonly int _index;
    private readonly StringListKind _kind;
    private readonly IConfigPersister _persister;
    private readonly string _original;

    public DraftableStringRowViewModel(
        int index,
        string value,
        StringListKind kind,
        IConfigPersister persister)
        : base(persister)
    {
        _index = index;
        _kind = kind;
        _persister = persister;
        _value = value;
        _original = value;
    }

    [ObservableProperty]
    private string _value;

    partial void OnValueChanged(string value) => NotifyDraftStateChanged();

    public override string Label => Value;

    public override bool HasDraft => Value != _original;

    [RelayCommand]
    private void Delete()
    {
        _persister.Save(c => StringListEditor.Remove(c, _kind, _index));
    }

    protected override Func<AppConfig, AppConfig> BuildMutator()
    {
        int index = _index;
        string newValue = Value;
        StringListKind kind = _kind;
        return config => StringListEditor.Replace(config, kind, index, newValue);
    }

    protected override void ResetDrafts()
    {
        Value = _original;
    }

    protected override void OnSaveCommitted()
    {
        ResetDrafts();
    }
}
