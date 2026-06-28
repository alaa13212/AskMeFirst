namespace AskMeFirst.Core.Config;

public static class ConfigPath
{
    public const string AppFolderName = "AskMeFirst";

    public const string FileName = "config.json";

    public static string Combine(string parent)
    {
        return Path.Combine(parent, AppFolderName, FileName);
    }
}