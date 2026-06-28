using AskMeFirst.Core.Config;
using AskMeFirst.Core.Routing;
using Xunit;

namespace AskMeFirst.Core.Tests;

public class TrackingStripperTests
{
    private static readonly IReadOnlySet<string> BuiltIn = TrackingStripper.BuildTrackerSet(new AppConfig());

    [Fact]
    public void NoQuery_Unchanged()
    {
        Uri input = new("https://example.com/path");
        Assert.Equal(input, TrackingStripper.Strip(input, BuiltIn));
    }

    [Fact]
    public void TrackerStripped()
    {
        Uri input = new("https://example.com/?utm_source=foo");
        Uri result = TrackingStripper.Strip(input, BuiltIn);
        Assert.Equal("https://example.com/", result.ToString());
    }

    [Fact]
    public void NonTrackerKept()
    {
        Uri input = new("https://example.com/?q=hello");
        Uri result = TrackingStripper.Strip(input, BuiltIn);
        Assert.Equal("https://example.com/?q=hello", result.ToString());
    }

    [Fact]
    public void Mixed_OnlyTrackersStripped()
    {
        Uri input = new("https://example.com/?q=hello&utm_source=foo&page=2");
        Uri result = TrackingStripper.Strip(input, BuiltIn);
        Assert.Equal("https://example.com/?q=hello&page=2", result.ToString());
    }

    [Fact]
    public void KeyWithoutValue_StrippedIfTracked()
    {
        Uri input = new("https://example.com/?utm_source&q=hello");
        Uri result = TrackingStripper.Strip(input, BuiltIn);
        Assert.Equal("https://example.com/?q=hello", result.ToString());
    }

    [Fact]
    public void ValueWithEquals_KeptIfNotTracked()
    {
        Uri input = new("https://example.com/?token=a=b");
        Uri result = TrackingStripper.Strip(input, BuiltIn);
        Assert.Equal("https://example.com/?token=a=b", result.ToString());
    }

    [Fact]
    public void MultipleTrackers_AllStripped()
    {
        Uri input = new("https://example.com/?utm_source=a&utm_medium=b&gclid=c&q=keep");
        Uri result = TrackingStripper.Strip(input, BuiltIn);
        Assert.Equal("https://example.com/?q=keep", result.ToString());
    }

    [Fact]
    public void FragmentPreserved()
    {
        Uri input = new("https://example.com/?utm_source=foo#section");
        Uri result = TrackingStripper.Strip(input, BuiltIn);
        Assert.Equal("https://example.com/#section", result.ToString());
    }

    [Fact]
    public void OverrideReplacesBuiltIn()
    {
        AppConfig config = new() { TrackingParamsOverride = true, TrackingParamsExtra = ["custom"] };
        IReadOnlySet<string> set = TrackingStripper.BuildTrackerSet(config);

        Uri input = new("https://example.com/?utm_source=foo&custom=bar&q=keep");
        Uri result = TrackingStripper.Strip(input, set);
        Assert.Equal("https://example.com/?utm_source=foo&q=keep", result.ToString());
    }

    [Fact]
    public void ExtraAddsToBuiltIn()
    {
        AppConfig config = new() { TrackingParamsExtra = ["company_tracker"] };
        IReadOnlySet<string> set = TrackingStripper.BuildTrackerSet(config);

        Uri input = new("https://example.com/?utm_source=foo&company_tracker=bar&q=keep");
        Uri result = TrackingStripper.Strip(input, set);
        Assert.Equal("https://example.com/?q=keep", result.ToString());
    }

    [Fact]
    public void EmptyTrackerSet_NoChange()
    {
        IReadOnlySet<string> empty = new HashSet<string>();
        Uri input = new("https://example.com/?utm_source=foo");
        Assert.Equal(input, TrackingStripper.Strip(input, empty));
    }
}