using AskMeFirst.MgmtUi.Areas;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed class NoFormsDetailViewModel : AreaDetailViewModel
{
    public NoFormsDetailViewModel(AreaId areaId, MgmtUiWindowViewModel parent, string placeholder)
        : base(areaId, parent)
    {
        Placeholder = placeholder;
    }

    public override string Placeholder { get; }

    public override void Rebuild(Services.IConfigPersister persister)
    {
    }
}
