using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed class ProfileDetailViewModel : AreaDetailViewModel
{
    private readonly IConfigPersister _persister;

    public ProfileDetailViewModel(MgmtUiWindowViewModel parent, IConfigPersister persister)
        : base(AreaId.Profiles, parent)
    {
        _persister = persister;
        Placeholder = "No profiles in config yet.";
    }

    public override string Placeholder { get; }

    public override void Rebuild(IConfigPersister persister)
    {
        Rows.Clear();
        foreach (ProfileSpec profile in persister.Current.Profiles)
        {
            Rows.Add(new ProfileRowViewModel(profile, persister));
        }
        NotifyRowsChanged();
    }
}
