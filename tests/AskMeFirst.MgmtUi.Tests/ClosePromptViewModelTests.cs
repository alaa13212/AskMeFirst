using AskMeFirst.MgmtUi.ViewModels;
using Xunit;

namespace AskMeFirst.MgmtUi.Tests;

public class ClosePromptViewModelTests
{
    [Fact]
    public void SaveCommand_SetsSaveDrafts_AndResolves()
    {
        ClosePromptViewModel prompt = new();

        Assert.False(prompt.Resolved);
        prompt.SaveCommand.Execute(null);

        Assert.True(prompt.SaveDrafts);
        Assert.False(prompt.DiscardDrafts);
        Assert.False(prompt.Cancelled);
        Assert.True(prompt.Resolved);
    }

    [Fact]
    public void DiscardCommand_SetsDiscardDrafts_AndResolves()
    {
        ClosePromptViewModel prompt = new();

        prompt.DiscardCommand.Execute(null);

        Assert.False(prompt.SaveDrafts);
        Assert.True(prompt.DiscardDrafts);
        Assert.False(prompt.Cancelled);
        Assert.True(prompt.Resolved);
    }

    [Fact]
    public void CancelCommand_SetsCancelled_AndResolves()
    {
        ClosePromptViewModel prompt = new();

        prompt.CancelCommand.Execute(null);

        Assert.False(prompt.SaveDrafts);
        Assert.False(prompt.DiscardDrafts);
        Assert.True(prompt.Cancelled);
        Assert.True(prompt.Resolved);
    }
}
