namespace AskMeFirst.MgmtUi.Services;

public interface IConfigFileWatcher : IDisposable
{
    event EventHandler? Changed;

    void Start();
}
