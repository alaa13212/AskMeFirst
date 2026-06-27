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
        List<(string Label, string Text)> rows = [];
        foreach (ICommand cmd in registry.All())
        {
            string label = FormatCommandLabel(cmd, isDefault: ReferenceEquals(cmd, registry.Default));
            string text = ReferenceEquals(cmd, registry.Default)
                ? $"{cmd.Description} (default)"
                : cmd.Description;
            rows.Add((label, text));
        }
        return rows;
    }

    private static string FormatCommandLabel(ICommand cmd, bool isDefault)
    {
        if (isDefault)
        {
            return cmd.Usage;
        }
        if (cmd.Aliases.Count == 0)
        {
            return cmd.Name;
        }
        return $"{cmd.Name}, {string.Join(", ", cmd.Aliases)}";
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