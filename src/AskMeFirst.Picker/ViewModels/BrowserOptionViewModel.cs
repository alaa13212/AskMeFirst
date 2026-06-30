using System.Globalization;
using AskMeFirst.Core.Abstractions;
using AskMeFirst.Core.Models;
using Avalonia.Media.Imaging;

namespace AskMeFirst.Picker.ViewModels;

public sealed class BrowserOptionViewModel : IDisposable
{
    private readonly byte[]? _primaryBytes;
    private readonly byte[]? _overlayBytes;
    private Bitmap? _primaryIcon;
    private Bitmap? _overlayIcon;
    private bool _disposed;

    public BrowserOptionViewModel(Browser browser, BrowserProfile? profile, int hotkey, IIconProvider icons)
    {
        Browser = browser;
        Profile = profile;
        Hotkey = hotkey;

        PrimaryLabel = profile?.Name ?? browser.DisplayName;
        SubtitleLabel = profile is null ? "" : browser.DisplayName;
        IsSubtitleVisible = profile is not null;

        HotkeyLabel = hotkey >= 1 && hotkey <= 9
            ? hotkey.ToString(CultureInfo.InvariantCulture)
            : "";

        byte[]? profilePic = profile is null ? null : icons.GetProfileIconPng(browser.Id, profile);
        byte[]? browserPic = icons.GetBrowserIconPng(browser.Id, browser.ExecutablePath);

        _primaryBytes = profilePic ?? browserPic;
        _overlayBytes = profilePic is not null ? browserPic : null;
    }

    public Browser Browser { get; }

    public BrowserProfile? Profile { get; }

    public string PrimaryLabel { get; }

    public string SubtitleLabel { get; }

    public bool IsSubtitleVisible { get; }

    public string HotkeyLabel { get; }

    public int Hotkey { get; }

    public bool HasOverlay => _overlayBytes is not null && _overlayBytes.Length > 0;

    public Bitmap? PrimaryIcon
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_primaryIcon is not null)
            {
                return _primaryIcon;
            }
            _primaryIcon = TryDecode(_primaryBytes);
            return _primaryIcon;
        }
    }

    public Bitmap? OverlayIcon
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_overlayIcon is not null)
            {
                return _overlayIcon;
            }
            _overlayIcon = TryDecode(_overlayBytes);
            return _overlayIcon;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _primaryIcon?.Dispose();
        _overlayIcon?.Dispose();
    }

    private static Bitmap? TryDecode(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }
        try
        {
            return new Bitmap(new MemoryStream(bytes));
        }
        catch
        {
            return null;
        }
    }
}
