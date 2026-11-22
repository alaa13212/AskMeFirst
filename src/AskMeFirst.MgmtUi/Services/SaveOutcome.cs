namespace AskMeFirst.MgmtUi.Services;

public readonly record struct SaveOutcome(bool Ok, string? Error)
{
    public static SaveOutcome Success() => new(true, null);

    public static SaveOutcome Failure(string error) => new(false, error);
}
