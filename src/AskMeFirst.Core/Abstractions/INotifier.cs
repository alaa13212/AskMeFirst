namespace AskMeFirst.Core.Abstractions;

public interface INotifier
{
    void Show(string title, string message);
}

public sealed class NullNotifier : INotifier
{
    public void Show(string title, string message)
    {
    }
}