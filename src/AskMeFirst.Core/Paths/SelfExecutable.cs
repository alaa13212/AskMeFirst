namespace AskMeFirst.Core.Paths;

public static class SelfExecutable
{
    public static bool IsSelf(string? otherPath)
    {
        if (string.IsNullOrEmpty(otherPath))
        {
            return false;
        }
        string? self = Environment.ProcessPath;
        if (string.IsNullOrEmpty(self))
        {
            return false;
        }
        try
        {
            string selfFull = Path.GetFullPath(self);
            string otherFull = Path.GetFullPath(otherPath);
            StringComparison cmp = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return string.Equals(selfFull, otherFull, cmp);
        }
        catch
        {
            return false;
        }
    }
}