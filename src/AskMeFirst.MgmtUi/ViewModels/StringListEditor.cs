using AskMeFirst.Core.Config;

namespace AskMeFirst.MgmtUi.ViewModels;

public enum StringListKind
{
    UnshortenDomain,
    TrackingParam,
}

internal static class StringListEditor
{
    public static AppConfig Replace(AppConfig c, StringListKind kind, int index, string value)
    {
        if (index < 0)
        {
            return c;
        }
        return kind switch
        {
            StringListKind.UnshortenDomain => ReplaceIn(c, c.UnshortenDomainsExtra, index, value,
                list => c with { UnshortenDomainsExtra = list }),
            StringListKind.TrackingParam => ReplaceIn(c, c.TrackingParamsExtra, index, value,
                list => c with { TrackingParamsExtra = list }),
            _ => c,
        };
    }

    public static AppConfig Remove(AppConfig c, StringListKind kind, int index)
    {
        if (index < 0)
        {
            return c;
        }
        return kind switch
        {
            StringListKind.UnshortenDomain => RemoveAt(c, c.UnshortenDomainsExtra, index,
                list => c with { UnshortenDomainsExtra = list }),
            StringListKind.TrackingParam => RemoveAt(c, c.TrackingParamsExtra, index,
                list => c with { TrackingParamsExtra = list }),
            _ => c,
        };
    }

    public static AppConfig Append(AppConfig c, StringListKind kind)
    {
        return kind switch
        {
            StringListKind.UnshortenDomain => c with { UnshortenDomainsExtra = [.. c.UnshortenDomainsExtra, ""] },
            StringListKind.TrackingParam => c with { TrackingParamsExtra = [.. c.TrackingParamsExtra, ""] },
            _ => c,
        };
    }

    public static IReadOnlyList<string> GetList(AppConfig c, StringListKind kind)
    {
        return kind switch
        {
            StringListKind.UnshortenDomain => c.UnshortenDomainsExtra,
            StringListKind.TrackingParam => c.TrackingParamsExtra,
            _ => [],
        };
    }

    private static AppConfig ReplaceIn(
        AppConfig c,
        IReadOnlyList<string> source,
        int index,
        string value,
        Func<IReadOnlyList<string>, AppConfig> set)
    {
        if (index >= source.Count)
        {
            return c;
        }
        List<string> updated = [.. source];
        if (string.IsNullOrWhiteSpace(value))
        {
            updated.RemoveAt(index);
        }
        else
        {
            updated[index] = value;
        }
        return set(updated);
    }

    private static AppConfig RemoveAt(
        AppConfig c,
        IReadOnlyList<string> source,
        int index,
        Func<IReadOnlyList<string>, AppConfig> set)
    {
        if (index >= source.Count)
        {
            return c;
        }
        List<string> updated = [.. source];
        updated.RemoveAt(index);
        return set(updated);
    }
}
