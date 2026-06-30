namespace AskMeFirst.Picker.Services;

public sealed record ScreenBounds(int X, int Y, int Width, int Height, bool IsPrimary = true);

public sealed record ScreenInfo(IReadOnlyList<ScreenBounds> All)
{
    public ScreenBounds Primary
    {
        get
        {
            if(All.FirstOrDefault(s => s.IsPrimary) is {  } primary)
            {
                return primary;
            }
            return All.Count > 0 ? All[0] : new ScreenBounds(0, 0, 1920, 1080, IsPrimary: true);
        }
    }
}

public interface IScreenProvider
{
    ScreenInfo GetScreens();
}
