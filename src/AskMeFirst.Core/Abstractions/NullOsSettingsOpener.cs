namespace AskMeFirst.Core.Abstractions;

public sealed class NullOsSettingsOpener : IOsSettingsOpener
{
    public bool TryOpen() => false;
}
