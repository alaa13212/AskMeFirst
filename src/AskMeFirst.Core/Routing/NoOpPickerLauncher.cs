namespace AskMeFirst.Core.Routing;

public sealed class NoOpPickerLauncher(Abstractions.ILogger logger) : IPickerLauncher
{
    public PickerResult Show(PickerRequest request)
    {
        logger.LogWarn(
            $"Picker requested for {request.OriginalUrl} but no picker implementation is wired. " +
            "Returning Cancelled. (This indicates a missing AskMeFirst.Picker project reference.)");
        return new Cancelled();
    }
}