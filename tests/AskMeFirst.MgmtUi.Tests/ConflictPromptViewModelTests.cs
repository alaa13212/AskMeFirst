using AskMeFirst.MgmtUi.Services;
using AskMeFirst.MgmtUi.ViewModels;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class ConflictPromptViewModelTests
{
    [Fact]
    public void KeepCommand_SetsKeepDrafts_AndResolvesTrue()
    {
        ConflictPromptViewModel prompt = new("/tmp/config.json");

        Assert.False(prompt.Resolved);
        prompt.KeepCommand.Execute(null);

        Assert.True(prompt.KeepDrafts);
        Assert.False(prompt.DiscardDrafts);
        Assert.True(prompt.Resolved);
    }

    [Fact]
    public void DiscardCommand_SetsDiscardDrafts_AndResolvesTrue()
    {
        ConflictPromptViewModel prompt = new("/tmp/config.json");

        prompt.DiscardCommand.Execute(null);

        Assert.False(prompt.KeepDrafts);
        Assert.True(prompt.DiscardDrafts);
        Assert.True(prompt.Resolved);
    }
}
