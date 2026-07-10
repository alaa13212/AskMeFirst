using Xunit;

namespace AskMeFirst.Core.Tests;

public class SampleConfigTests
{
    [Fact]
    public void EmbeddedSample_MatchesSamplesFile_BothKeptInSync()
    {
        string repoRoot = LocateRepoRoot();
        string samplesPath = Path.Combine(repoRoot, "samples", "askmefirst.example.json");
        string embeddedPath = Path.Combine(repoRoot, "src", "AskMeFirst", "Resources", "askmefirst.example.json");

        string samplesContent = File.ReadAllText(samplesPath);
        string embeddedContent = File.ReadAllText(embeddedPath);

        Assert.Equal(samplesContent, embeddedContent);
    }

    private static string LocateRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "AskMeFirst.slnx");
            if (File.Exists(candidate))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException($"Could not locate repo root (AskMeFirst.slnx) from {AppContext.BaseDirectory}");
    }
}
