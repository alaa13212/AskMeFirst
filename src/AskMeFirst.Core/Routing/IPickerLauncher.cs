namespace AskMeFirst.Core.Routing;

public interface IPickerLauncher
{
    PickerResult Show(PickerRequest request);
}