using System.Windows.Input;
using AskMeFirst.Core.Config;
using AskMeFirst.MgmtUi.Areas;
using AskMeFirst.MgmtUi.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskMeFirst.MgmtUi.ViewModels;

public sealed partial class ProfileDetailViewModel : AreaDetailViewModel
{
    private readonly IConfigPersister _persister;
    private int _newProfileSeq;

    public ProfileDetailViewModel(MgmtUiWindowViewModel parent, IConfigPersister persister)
        : base(AreaId.Profiles, parent)
    {
        _persister = persister;
        Placeholder = "No profiles in config yet. Profiles from auto-detection land here.";
        AddCommand = new RelayCommand(AddProfile);
    }

    public override string Placeholder { get; }

    public override bool SupportsAdd => true;

    public override ICommand? AddCommand { get; }

    private void AddProfile()
    {
        _persister.Save(c =>
        {
            int seq = System.Threading.Interlocked.Increment(ref _newProfileSeq);
            ProfileSpec fresh = new()
            {
                Id = $"new-profile-{seq}",
                BrowserId = "firefox",
                Name = $"new-profile-{seq}",
                DisplayName = $"New profile ({seq})",
            };
            return c with { Profiles = [.. c.Profiles, fresh] };
        });
    }

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
