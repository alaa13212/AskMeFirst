using System.Text;

namespace AskMeFirst.Core.Commands;

public static class HelpFormatter
{
    public static string Render(CommandRegistry registry)
    {
        StringBuilder sb = new();
        sb.AppendLine("askmefirst — smart browser router");
        sb.AppendLine();
        sb.AppendLine("Usage:");
        sb.AppendLine("  askmefirst [command] [args...]");
        sb.AppendLine();
        sb.AppendLine("Commands:");
        sb.Append(FormatColumns(BuildRows(registry)));
        return sb.ToString();
    }

    private static List<(string Label, string Text)> BuildRows(CommandRegistry registry)
    {
        return registry.All()
            .Select(cmd => ( cmd.Usage, cmd.Description ))
            .ToList();
    }

    private static string FormatColumns(List<(string Label, string Text)> rows)
    {
        if (rows.Count == 0)
        {
            return "";
        }

        int maxLabel = rows.Max(r => r.Label.Length);
        StringBuilder sb = new();
        foreach ((string label, string text) in rows)
        {
            sb.Append("  ");
            sb.Append(label.PadRight(maxLabel + 2));
            sb.AppendLine(text);
        }
        return sb.ToString();
    }
}
