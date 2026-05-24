using FFXIVRPC.Models;
using Xunit;

namespace FFXIVRPC.Tests;

public class PvPProfileSnapshotTests
{
    [Fact]
    public void IsEmpty_AllZeros_ReturnsTrue()
    {
        var s = new PvPProfileSnapshot(0, 0, 0, 0, 0, 0, 0, 0);
        Assert.True(s.IsEmpty);
    }

    [Fact]
    public void IsEmpty_SeriesClaimedRankNonZero_ReturnsFalse()
    {
        var s = new PvPProfileSnapshot(1, 0, 0, 0, 0, 0, 0, 0);
        Assert.False(s.IsEmpty);
    }

    [Fact]
    public void IsEmpty_CcRankNonZero_ReturnsFalse()
    {
        var s = new PvPProfileSnapshot(0, 0, 3, 0, 0, 0, 0, 0);
        Assert.False(s.IsEmpty);
    }

    [Fact]
    public void IsEmpty_CcCreditNonZero_ReturnsFalse()
    {
        var s = new PvPProfileSnapshot(0, 500, 0, 0, 0, 0, 0, 0);
        Assert.False(s.IsEmpty);
    }

    [Fact]
    public void IsEmpty_CcStarsNonZero_ReturnsFalse()
    {
        var s = new PvPProfileSnapshot(0, 0, 0, 0, 0, 0, 2, 0);
        Assert.False(s.IsEmpty);
    }

    [Fact]
    public void IsEmpty_OnlyHighestRank_ReturnsFalse()
    {
        var s = new PvPProfileSnapshot(0, 0, 0, 5, 0, 0, 0, 0);
        Assert.False(s.IsEmpty);
    }

    [Fact]
    public void IsEmpty_OnlyHighestRiser_ReturnsFalse()
    {
        var s = new PvPProfileSnapshot(0, 0, 0, 0, 0, 3, 0, 0);
        Assert.False(s.IsEmpty);
    }

    [Fact]
    public void IsEmpty_OnlyCurrentRiser_ReturnsFalse()
    {
        var s = new PvPProfileSnapshot(0, 0, 0, 0, 2, 0, 0, 0);
        Assert.False(s.IsEmpty);
    }

    [Fact]
    public void IsEmpty_OnlyHighestRisingStars_ReturnsFalse()
    {
        var s = new PvPProfileSnapshot(0, 0, 0, 0, 0, 0, 0, 4);
        Assert.False(s.IsEmpty);
    }
}
