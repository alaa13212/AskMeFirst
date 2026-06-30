namespace AskMeFirst.Core.Paths;

public static class PathTail
{
    public static string Segment(string path)
    {
        string normalized = path.Replace('/', '\\');
        int lastSlash = normalized.LastIndexOf('\\');
        return lastSlash >= 0 ? normalized[(lastSlash + 1)..] : normalized;
    }
}
