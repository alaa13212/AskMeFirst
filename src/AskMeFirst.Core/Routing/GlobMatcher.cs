using System.Text;
using System.Text.RegularExpressions;

namespace AskMeFirst.Core.Routing;

public static class GlobMatcher
{
    private static readonly Dictionary<string, Regex> Cache = new(StringComparer.Ordinal);

    public static bool Matches(string pattern, string host, string hostPath)
    {
        bool includePath = pattern.Contains('/');
        string text = includePath ? hostPath : host;
        Regex regex = GetOrCompile(GlobToRegex(pattern));
        return regex.IsMatch(text);
    }

    public static string GlobToRegex(string glob)
    {
        StringBuilder sb = new(glob.Length * 2);
        sb.Append('^');
        AppendGlob(sb, glob);
        sb.Append('$');
        return sb.ToString();
    }

    private static void AppendGlob(StringBuilder sb, string glob)
    {
        if (glob.StartsWith("*.", StringComparison.Ordinal))
        {
            sb.Append("([^./]*\\.)?");
            AppendGlobBody(sb, glob[2..]);
            return;
        }
        AppendGlobBody(sb, glob);
    }

    private static void AppendGlobBody(StringBuilder sb, string glob)
    {
        int i = 0;
        while (i < glob.Length)
        {
            char c = glob[i];
            if (c == '*' && i + 1 < glob.Length && glob[i + 1] == '*')
            {
                sb.Append(".*");
                i += 2;
            }
            else if (c == '*')
            {
                sb.Append("[^./]*");
                i++;
            }
            else if (IsRegexSpecial(c))
            {
                sb.Append('\\').Append(c);
                i++;
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }
    }

    private static bool IsRegexSpecial(char c)
    {
        return c is '\\' or '.' or '+' or '?' or '(' or ')' or '[' or ']' or '{' or '}' or '|' or '^' or '$';
    }

    private static Regex GetOrCompile(string pattern)
    {
        if (Cache.TryGetValue(pattern, out Regex? existing))
        {
            return existing;
        }
        Regex compiled = new(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        Cache[pattern] = compiled;
        return compiled;
    }
}