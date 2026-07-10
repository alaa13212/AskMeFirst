using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed class ProfileEntityDetailViewModel : EntityDetailViewModel
{
    private readonly IConfigPersister _persister;

    public ProfileEntityDetailViewModel(
        string profileId,
        MgmtUiWindowViewModel parent,
        IConfigPersister persister)
        : base(profileId, parent)
    {
        _persister = persister;
        ParentArea = AreaId.Profiles;
        Title = $"Profile · {profileId}";
    }

    public override string Label => EntityId;

    public override void Rebuild(IConfigPersister persister)
    {
        Rows.Clear();
        ProfileSpec? profile = Find();
        if (profile is null)
        {
            NotifyErrors($"Profile '{EntityId}' no longer exists.");
            return;
        }
        NotifyErrors(null);
        Rows.Add(new ProfileRowViewModel(profile, persister));
    }

    private ProfileSpec? Find()
    {
        foreach (ProfileSpec profile in _persister.Current.Profiles)
        {
            if (profile.Id == EntityId)
            {
                return profile;
            }
        }
        return null;
    }
}
