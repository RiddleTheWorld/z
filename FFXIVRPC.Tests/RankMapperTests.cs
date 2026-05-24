using FFXIVRPC.Models;
using Xunit;

namespace FFXIVRPC.Tests;

public class RankMapperTests
{
    [Theory]
    [InlineData(0, "Unranked")]
    [InlineData(1, "Bronze")]
    [InlineData(2, "Silver")]
    [InlineData(3, "Gold")]
    [InlineData(4, "Platinum")]
    [InlineData(5, "Diamond")]
    [InlineData(6, "Crystal")]
    [InlineData(7, "Omega")]
    [InlineData(8, "Ultima")]
    [InlineData(255, "Unknown(255)")]
    public void GetCrystallineConflictTierName_MapsCorrectly(byte rank, string expected)
    {
        Assert.Equal(expected, RankMapper.GetCrystallineConflictTierName(rank));
    }

    [Fact]
    public void FormatCrystallineConflict_RankZero_ReturnsEmpty()
    {
        Assert.Equal("", RankMapper.FormatCrystallineConflict(0, 0, 0, 0));
    }

    [Fact]
    public void FormatCrystallineConflict_BronzeWithRiser()
    {
        var result = RankMapper.FormatCrystallineConflict(1, 2, 0, 0);
        Assert.Equal("Bronze 2", result);
    }

    [Fact]
    public void FormatCrystallineConflict_BronzeNoRiser()
    {
        var result = RankMapper.FormatCrystallineConflict(1, 0, 0, 0);
        Assert.Equal("Bronze", result);
    }

    [Fact]
    public void FormatCrystallineConflict_SilverWithStars()
    {
        var result = RankMapper.FormatCrystallineConflict(2, 1, 3, 0);
        Assert.Equal("Silver 1 (3 stars)", result);
    }

    [Fact]
    public void FormatCrystallineConflict_CrystalWithCredit()
    {
        var result = RankMapper.FormatCrystallineConflict(6, 0, 0, 1250);
        Assert.Equal("Crystal (1250 CC)", result);
    }

    [Fact]
    public void FormatCrystallineConflict_CrystalWithCreditAndStars()
    {
        var result = RankMapper.FormatCrystallineConflict(6, 0, 2, 3400);
        Assert.Equal("Crystal (3400 CC) (2 stars)", result);
    }

    [Fact]
    public void FormatCrystallineConflict_OmegaWithCredit()
    {
        var result = RankMapper.FormatCrystallineConflict(7, 0, 0, 5000);
        Assert.Equal("Omega (5000 CC)", result);
    }

    [Fact]
    public void FormatCrystallineConflict_UltimaWithCreditAndStars()
    {
        var result = RankMapper.FormatCrystallineConflict(8, 0, 3, 9999);
        Assert.Equal("Ultima (9999 CC) (3 stars)", result);
    }

    [Fact]
    public void FormatCrystallineConflict_InvalidRank_ReturnsTierName()
    {
        var result = RankMapper.FormatCrystallineConflict(9, 0, 0, 0);
        Assert.Equal("Unknown(9)", result);
    }

    [Fact]
    public void FormatLegacyRank_Zero_ReturnsEmpty()
    {
        Assert.Equal("", RankMapper.FormatLegacyRank(0));
    }

    [Fact]
    public void FormatLegacyRank_NonZero_ReturnsFormatted()
    {
        Assert.Equal("PvP Rank 25", RankMapper.FormatLegacyRank(25));
    }
}
