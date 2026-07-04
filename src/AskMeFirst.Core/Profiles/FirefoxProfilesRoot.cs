namespace AskMeFirst.Core.Profiles;

public static class FirefoxProfilesRoot
{
    public static string Get()
    {
        if (OperatingSystem.IsWindows())
        {
            string? appData = Environment.GetEnvironmentVariable("APPDATA");
            return Path.Combine(appData ?? @"C:\Users\Default\AppData\Roaming", "Mozilla", "Firefox", "Profiles");
        }
        if (OperatingSystem.IsMacOS())
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", "Firefox", "Profiles");
        }
        string linuxHome = Environment.GetEnvironmentVariable("HOME")
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string canonical = Path.Combine(linuxHome, ".mozilla", "firefox");
        if (Directory.Exists(canonical))
        {
            return canonical;
        }
        string alt = Path.Combine(linuxHome, ".config", "mozilla", "firefox");
        return Directory.Exists(alt) ? alt : canonical;
    }
}