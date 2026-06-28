namespace AskMeFirst.Core.Routing;

public interface IProcessNameNormalizer
{
    string Normalize(string rawName, string? bundleId = null, string? executablePath = null);
}