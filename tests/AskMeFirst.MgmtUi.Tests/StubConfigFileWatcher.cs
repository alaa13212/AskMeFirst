namespace AskMeFirst.MgmtUi.Tests;

using AskMeFirst.MgmtUi.Services;

internal sealed class StubConfigFileWatcher : IConfigFileWatcher
{
    public event EventHandler? Changed;

    public bool Disposed { get; private set; }

    public bool Started { get; private set; }

    public int StartCount { get; private set; }

    public void Start()
    {
        StartCount++;
        Started = true;
    }

    public void RaiseChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        Disposed = true;
    }
}
