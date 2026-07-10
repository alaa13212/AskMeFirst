using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class StringListDetailViewModel : AreaDetailViewModel
{
    private readonly IConfigPersister _persister;
    private readonly StringListKind _kind;

    public StringListDetailViewModel(
        AreaId areaId,
        MgmtUiWindowViewModel parent,
        StringListKind kind,
        string title,
        IConfigPersister persister)
        : base(areaId, parent)
    {
        _kind = kind;
        _persister = persister;
        Title = title;
        Placeholder = "Empty list — add the first entry below.";
    }

    public override string Placeholder { get; }

    [RelayCommand]
    private void Add()
    {
        _persister.Save(c => StringListEditor.Append(c, _kind));
    }

    public override void Rebuild(IConfigPersister persister)
    {
        Rows.Clear();
        IReadOnlyList<string> list = StringListEditor.GetList(persister.Current, _kind);
        for (int i = 0; i < list.Count; i++)
        {
            Rows.Add(new DraftableStringRowViewModel(i, list[i], _kind, persister));
        }
        NotifyRowsChanged();
    }
}
