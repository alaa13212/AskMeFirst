namespace AskMeFirst.MgmtUi.Services;

public sealed class FileConfigFileWatcher : IConfigFileWatcher
{
    private const int DebounceMs = 200;
    private readonly string? _dir;
    private readonly string _fileName;
    private readonly FileSystemWatcher _fsw;
    private readonly System.Timers.Timer _debounce;
    private bool _disposed;

    public FileConfigFileWatcher(string configPath)
    {
        _dir = Path.GetDirectoryName(configPath);
        _fileName = Path.GetFileName(configPath);
        if (string.IsNullOrEmpty(_dir))
        {
            _dir = Directory.GetCurrentDirectory();
        }
        Directory.CreateDirectory(_dir);
        _fsw = new FileSystemWatcher(_dir, _fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
        };
        _fsw.Changed += OnFileSystemEvent;
        _fsw.Created += OnFileSystemEvent;
        _fsw.Renamed += OnRenamed;

        _debounce = new System.Timers.Timer(DebounceMs)
        {
            AutoReset = false,
            Enabled = false,
        };
        _debounce.Elapsed += (_, _) => RaiseChanged();
    }

    public event EventHandler? Changed;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _fsw.EnableRaisingEvents = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _fsw.EnableRaisingEvents = false;
        _fsw.Changed -= OnFileSystemEvent;
        _fsw.Created -= OnFileSystemEvent;
        _fsw.Renamed -= OnRenamed;
        _fsw.Dispose();
        _debounce.Dispose();
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        Schedule();
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        Schedule();
    }

    private void Schedule()
    {
        if (_disposed)
        {
            return;
        }
        _debounce.Stop();
        _debounce.Start();
    }

    private void RaiseChanged()
    {
        EventHandler? handler = Changed;
        if (handler is not null)
        {
            handler(this, EventArgs.Empty);
        }
    }
}
