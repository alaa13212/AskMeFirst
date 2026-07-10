namespace AskMeFirst.Core.Routing;

public interface IShortenerDomainList
{
    bool IsKnown(string host);
}