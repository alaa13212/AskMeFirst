using System.Reflection;
using AskMeFirst.Core.Commands;
using AskMeFirst.Core.Config;

namespace AskMeFirst.Commands;

public sealed class InitCommand : ICommand
{
    public string Name => "init";
    public string Usage => "init";
    public string Description => "Write a starter config to the OS-standard path (skips if one already exists).";

    private const string ResourceName = "AskMeFirst.Resources.askmefirst.example.json";

    public Task<int> Execute(string[] args, CommandContext ctx)
    {
        IConfigPathResolver pathResolver = ctx.Resolve<IConfigPathResolver>();
        string configPath = pathResolver.DefaultConfigPath;

        if (File.Exists(configPath))
        {
            Console.WriteLine($"Config already exists at {configPath}.");
            Console.WriteLine($"Run '{ProgramInfo.ExecutableName} refresh' to reset the discovery cache, or edit the config directly.");
            return Task.FromResult(0);
        }

        string sample = LoadEmbeddedSample();
        string? dir = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(configPath, sample);
        Console.WriteLine($"Wrote {configPath}");
        return Task.FromResult(0);
    }

    private static string LoadEmbeddedSample()
    {
        Assembly assembly = typeof(InitCommand).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' not found in {assembly.GetName().Name}.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
