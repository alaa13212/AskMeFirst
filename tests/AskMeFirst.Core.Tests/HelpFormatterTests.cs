using AskMeFirst.Core.Commands;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class HelpFormatterTests
{
    private static FakeCmd FakeCommand(
        string name,
        string description,
        IReadOnlyList<string>? aliases = null,
        string usage = "")
    {
        return new FakeCmd(name, description, aliases ?? [], usage);
    }

    private static CommandRegistry Registry(params ICommand[] commands)
    {
        CommandRegistry registry = new();
        foreach (ICommand cmd in commands)
        {
            registry.Register(cmd);
        }
        return registry;
    }

    private static CommandRegistry RegistryWithDefault(ICommand defaultCmd, params ICommand[] commands)
    {
        CommandRegistry registry = Registry(commands);
        return registry.RegisterDefault(defaultCmd);
    }

    [Fact]
    public void Render_IncludesTitleAndUsageHeader()
    {
        CommandRegistry registry = Registry(FakeCommand("--version", "Print version."));
        string output = HelpFormatter.Render(registry);
        Assert.Contains("askmefirst", output);
        Assert.Contains("Usage:", output);
        Assert.Contains("askmefirst [command]", output);
        Assert.Contains("Commands:", output);
    }

    [Fact]
    public void Render_ListsNamedCommandWithAliases()
    {
        CommandRegistry registry = Registry(FakeCommand("--version", "Print version.", ["-V"], usage: "--version, -V"));
        string output = HelpFormatter.Render(registry);
        Assert.Contains("--version, -V", output);
        Assert.Contains("Print version.", output);
    }

    [Fact]
    public void Render_AlignsDescriptionsInColumns()
    {
        CommandRegistry registry = Registry(
            FakeCommand("--version", "Short desc."),
            FakeCommand("--list-with-longer-name", "Another desc."));
        string output = HelpFormatter.Render(registry);
        string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int descShort = Array.FindIndex(lines, l => l.Contains("Short desc."));
        int descLong = Array.FindIndex(lines, l => l.Contains("Another desc."));
        Assert.True(descShort >= 0 && descLong >= 0);
        int shortCol = lines[descShort].IndexOf("Short", StringComparison.Ordinal);
        int longCol = lines[descLong].IndexOf("Another", StringComparison.Ordinal);
        Assert.Equal(shortCol, longCol);
    }

    [Fact]
    public void Render_IncludesAllRegisteredCommands()
    {
        CommandRegistry registry = RegistryWithDefault(
            FakeCommand("open", "o", usage: "<url>"),
            FakeCommand("--version", "v", usage: "--version, -v"),
            FakeCommand("--help", "h", usage: "--help, -h, ?"),
            FakeCommand("--bench", "b", usage: "--bench, -b"),
            FakeCommand("--list", "l", usage: "--list, -l"));
        string output = HelpFormatter.Render(registry);
        Assert.Contains("--version", output);
        Assert.Contains("--help", output);
        Assert.Contains("--bench", output);
        Assert.Contains("--list", output);
        Assert.Contains("<url>", output);
    }

    [Fact]
    public void RegisterDefault_Twice_Throws()
    {
        CommandRegistry registry = new CommandRegistry()
            .RegisterDefault(FakeCommand("a", "first"));
        Assert.Throws<InvalidOperationException>(() => registry.RegisterDefault(FakeCommand("b", "second")));
    }

    private sealed record FakeCmd(
        string Name,
        string Description,
        IReadOnlyList<string> Aliases,
        string Usage) : ICommand
    {
        string ICommand.Name => Name;
        IReadOnlyList<string> ICommand.Aliases => Aliases;
        string ICommand.Usage => Usage;
        string ICommand.Description => Description;
        public int Execute(string[] args, CommandContext ctx) => 0;
    }
}
