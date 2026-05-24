using FFXIVRPC.Models;
using Xunit;

namespace FFXIVRPC.Tests;

public class DiscordPresenceStateTests
{
    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        var a = new DiscordPresenceState("details", "state", "largeKey", "largeText", "smallKey", "smallText");
        var b = new DiscordPresenceState("details", "state", "largeKey", "largeText", "smallKey", "smallText");

        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentDetails_NotEqual()
    {
        var a = new DiscordPresenceState("a", "state", "largeKey", "largeText", "smallKey", "smallText");
        var b = new DiscordPresenceState("b", "state", "largeKey", "largeText", "smallKey", "smallText");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_DifferentState_NotEqual()
    {
        var a = new DiscordPresenceState("details", "a", "largeKey", "largeText", "smallKey", "smallText");
        var b = new DiscordPresenceState("details", "b", "largeKey", "largeText", "smallKey", "smallText");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_DifferentLargeImageKey_NotEqual()
    {
        var a = new DiscordPresenceState("details", "state", "a", "largeText", "smallKey", "smallText");
        var b = new DiscordPresenceState("details", "state", "b", "largeText", "smallKey", "smallText");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_DifferentLargeImageText_NotEqual()
    {
        var a = new DiscordPresenceState("details", "state", "largeKey", "a", "smallKey", "smallText");
        var b = new DiscordPresenceState("details", "state", "largeKey", "b", "smallKey", "smallText");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_DifferentSmallImageKey_NotEqual()
    {
        var a = new DiscordPresenceState("details", "state", "largeKey", "largeText", "a", "smallText");
        var b = new DiscordPresenceState("details", "state", "largeKey", "largeText", "b", "smallText");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_DifferentSmallImageText_NotEqual()
    {
        var a = new DiscordPresenceState("details", "state", "largeKey", "largeText", "smallKey", "a");
        var b = new DiscordPresenceState("details", "state", "largeKey", "largeText", "smallKey", "b");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GetHashCode_SameValues_SameHash()
    {
        var a = new DiscordPresenceState("x", "y", "z", "w", "p", "q");
        var b = new DiscordPresenceState("x", "y", "z", "w", "p", "q");

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
