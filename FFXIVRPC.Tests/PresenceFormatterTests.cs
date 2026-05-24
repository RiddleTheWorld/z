using FFXIVRPC;
using FFXIVRPC.Models;
using FFXIVRPC.Services;
using Xunit;

namespace FFXIVRPC.Tests;

public class PresenceFormatterTests
{
    private static Configuration AllOn => new()
    {
        ShowName = true,
        ShowWorld = true,
        ShowJob = true,
        ShowLevel = true,
        ShowLocation = true,
        ShowCrystallineConflict = true,
        ShowLegacyRank = true,
        ShowRankIcon = true,
        ShowLiveMatch = true,
        ShowElapsedTime = true,
    };

    private static Configuration AllOff => new()
    {
        ShowName = false,
        ShowWorld = false,
        ShowJob = false,
        ShowLevel = false,
        ShowLocation = false,
        ShowCrystallineConflict = false,
        ShowLegacyRank = false,
        ShowRankIcon = false,
        ShowLiveMatch = false,
        ShowElapsedTime = false,
    };

    private static PlayerInfo MakePlayer(
        string name = "Test",
        string homeWorld = "Gilgamesh",
        string currentWorld = "Gilgamesh",
        string? location = "Limsa Lominsa",
        string? job = "PLD",
        int level = 90)
    {
        return new PlayerInfo(name, homeWorld, currentWorld, location, job, level);
    }

    [Fact]
    public void Build_AllOn_IncludesEverything()
    {
        var pvp = new PvPProfileSnapshot(5, 0, 4, 0, 2, 0, 0, 0);
        var player = MakePlayer();
        var state = PresenceFormatter.Build(pvp, LiveMatchSnapshot.Empty, player, AllOn);

        Assert.Contains("Test", state.Details);
        Assert.Contains("Gilgamesh", state.Details);
        Assert.Contains("PLD", state.Details);
        Assert.Contains("Lv.90", state.Details);
        Assert.Contains("Limsa Lominsa", state.Details);
        Assert.Contains("Platinum 2", state.State);
        Assert.Contains("PvP Rank 5", state.State);
        Assert.Equal("platinum", state.LargeImageKey);
        Assert.Equal("Platinum", state.LargeImageText);
        Assert.Equal("ffxiv_logo", state.SmallImageKey);
        Assert.Equal("Final Fantasy XIV", state.SmallImageText);
    }

    [Fact]
    public void Build_NameOnly()
    {
        var cfg = new Configuration
        {
            ShowName = true,
            ShowWorld = false,
            ShowJob = false,
            ShowLevel = false,
            ShowLocation = false,
        };
        var state = PresenceFormatter.Build(null, LiveMatchSnapshot.Empty, MakePlayer(), cfg);

        Assert.Equal("Test", state.Details);
    }

    [Fact]
    public void Build_JobWithoutLevel()
    {
        var cfg = new Configuration
        {
            ShowName = false,
            ShowWorld = false,
            ShowJob = true,
            ShowLevel = false,
            ShowLocation = false,
        };
        var state = PresenceFormatter.Build(null, LiveMatchSnapshot.Empty, MakePlayer(), cfg);

        Assert.Equal("PLD", state.Details);
        Assert.DoesNotContain("Lv.", state.Details);
    }

    [Fact]
    public void Build_LevelWithoutJob()
    {
        var cfg = new Configuration
        {
            ShowName = true,
            ShowWorld = false,
            ShowJob = false,
            ShowLevel = true,
            ShowLocation = false,
        };
        var state = PresenceFormatter.Build(null, LiveMatchSnapshot.Empty, MakePlayer(), cfg);

        Assert.Contains("Test", state.Details);
        Assert.Contains("Lv.90", state.Details);
        Assert.DoesNotContain("PLD", state.Details);
    }

    [Fact]
    public void Build_VisitingWorld()
    {
        var player = MakePlayer(currentWorld: "Jenova");
        var state = PresenceFormatter.Build(null, LiveMatchSnapshot.Empty, player, AllOn);

        Assert.Contains("(visiting Jenova)", state.Details);
    }

    [Fact]
    public void Build_NoPvpData_ReturnsAdventuring()
    {
        var state = PresenceFormatter.Build(null, LiveMatchSnapshot.Empty, MakePlayer(), AllOn);
        Assert.Equal("Adventuring in Eorzea", state.State);
        Assert.Equal("ffxiv_logo", state.LargeImageKey);
    }

    [Fact]
    public void Build_EmptyPvp_ReturnsAdventuring()
    {
        var empty = new PvPProfileSnapshot(0, 0, 0, 0, 0, 0, 0, 0);
        var state = PresenceFormatter.Build(empty, LiveMatchSnapshot.Empty, MakePlayer(), AllOn);

        Assert.Equal("Adventuring in Eorzea", state.State);
    }

    [Fact]
    public void Build_RankIconDisabled_UsesLogo()
    {
        var cfg = AllOn;
        cfg.ShowRankIcon = false;

        var pvp = new PvPProfileSnapshot(0, 0, 5, 0, 0, 0, 0, 0);
        var state = PresenceFormatter.Build(pvp, LiveMatchSnapshot.Empty, MakePlayer(), cfg);

        Assert.Equal("ffxiv_logo", state.LargeImageKey);
        Assert.Equal("Final Fantasy XIV", state.LargeImageText);
    }

    [Fact]
    public void Build_AllPlayerOff_PvpOn_OnlyShowsPvp()
    {
        var cfg = AllOff;
        cfg.ShowCrystallineConflict = true;

        var pvp = new PvPProfileSnapshot(0, 0, 3, 0, 0, 0, 0, 0);
        var state = PresenceFormatter.Build(pvp, LiveMatchSnapshot.Empty, MakePlayer(), cfg);

        Assert.Equal("Final Fantasy XIV", state.Details);
        Assert.Contains("Gold", state.State);
    }

    [Fact]
    public void Build_DiscordFieldLimit_Respected()
    {
        var longName = new string('A', 200);
        var player = MakePlayer(name: longName, homeWorld: longName, job: longName, location: longName);
        var state = PresenceFormatter.Build(null, LiveMatchSnapshot.Empty, player, AllOn);

        Assert.True(state.Details.Length <= 128, $"Details was {state.Details.Length}");
        Assert.True(state.State.Length <= 128, $"State was {state.State.Length}");
        Assert.True(state.LargeImageText.Length <= 128, $"LargeImageText was {state.LargeImageText.Length}");
    }

    [Fact]
    public void Build_LegacyRank_Only()
    {
        var cfg = new Configuration { ShowLegacyRank = true };
        var pvp = new PvPProfileSnapshot(25, 0, 0, 0, 0, 0, 0, 0);
        var state = PresenceFormatter.Build(pvp, LiveMatchSnapshot.Empty, MakePlayer(), cfg);

        Assert.Equal("PvP Rank 25", state.State);
    }

    [Fact]
    public void Build_CcAndLegacy_Combined()
    {
        var pvp = new PvPProfileSnapshot(10, 0, 4, 0, 0, 0, 0, 0);
        var state = PresenceFormatter.Build(pvp, LiveMatchSnapshot.Empty, MakePlayer(), AllOn);

        Assert.Contains("Platinum", state.State);
        Assert.Contains("PvP Rank 10", state.State);
    }

    [Theory]
    [InlineData(1, "bronze", "Bronze")]
    [InlineData(2, "silver", "Silver")]
    [InlineData(3, "gold", "Gold")]
    [InlineData(4, "platinum", "Platinum")]
    [InlineData(5, "diamond", "Diamond")]
    [InlineData(6, "crystal", "Crystal")]
    [InlineData(7, "omega", "Omega")]
    [InlineData(8, "ultima", "Ultima")]
    public void Build_RankIcon_MapsCorrectly(byte rank, string expectedKey, string expectedText)
    {
        var pvp = new PvPProfileSnapshot(0, 0, rank, 0, 0, 0, 0, 0);
        var state = PresenceFormatter.Build(pvp, LiveMatchSnapshot.Empty, MakePlayer(), AllOn);

        Assert.Equal(expectedKey, state.LargeImageKey);
        Assert.Equal(expectedText, state.LargeImageText);
    }

    [Fact]
    public void Build_RankIcon_UnknownRank_FallsBackToLogo()
    {
        var pvp = new PvPProfileSnapshot(0, 0, 99, 0, 0, 0, 0, 0);
        var state = PresenceFormatter.Build(pvp, LiveMatchSnapshot.Empty, MakePlayer(), AllOn);

        Assert.Equal("ffxiv_logo", state.LargeImageKey);
    }

    [Fact]
    public void Build_RankIcon_RankZero_UsesLogo()
    {
        var pvp = new PvPProfileSnapshot(0, 0, 0, 0, 0, 0, 0, 0);
        var state = PresenceFormatter.Build(pvp, LiveMatchSnapshot.Empty, MakePlayer(), AllOn);

        Assert.Equal("ffxiv_logo", state.LargeImageKey);
        Assert.Equal("Final Fantasy XIV", state.LargeImageText);
    }

    private static LiveMatchSnapshot MakeLive(
        bool inMatch = true,
        string team = "Astra",
        int kills = 3,
        int deaths = 1,
        int astraProgress = 600,
        int umbraProgress = 300)
    {
        return new LiveMatchSnapshot(inMatch, team, kills, deaths, 0, astraProgress, umbraProgress, 0, 0);
    }

    [Fact]
    public void Build_LiveMatch_AstraPlayerShowsOwnProgress()
    {
        var live = MakeLive(team: "Astra", astraProgress: 700, umbraProgress: 300);
        var state = PresenceFormatter.Build(null, live, MakePlayer(), AllOn);

        Assert.Contains("Crystalline Conflict", state.Details);
        Assert.Contains("PLD Lv.90", state.Details);
        Assert.Contains("Limsa Lominsa", state.Details);
        Assert.Contains("K:3", state.State);
        Assert.Contains("D:1", state.State);
        Assert.Contains("Astra", state.State);
        Assert.Contains("70%", state.State);
        Assert.Equal("crystal", state.SmallImageKey);
        Assert.Equal("In Match", state.SmallImageText);
    }

    [Fact]
    public void Build_LiveMatch_UmbraPlayerShowsOwnProgress()
    {
        var live = MakeLive(team: "Umbra", astraProgress: 200, umbraProgress: 500);
        var state = PresenceFormatter.Build(null, live, MakePlayer(), AllOn);

        Assert.Contains("Umbra", state.State);
        Assert.Contains("50%", state.State);
    }

    [Fact]
    public void Build_LiveMatch_AstraPlayerShowsOwnLowProgress()
    {
        var live = MakeLive(team: "Astra", astraProgress: 200, umbraProgress: 800);
        var state = PresenceFormatter.Build(null, live, MakePlayer(), AllOn);

        Assert.Contains("Astra", state.State);
        Assert.Contains("20%", state.State);
        Assert.DoesNotContain("80%", state.State);
    }

    [Fact]
    public void Build_LiveMatch_UnknownTeam_ShowsNoTeamProgress()
    {
        var live = MakeLive(team: "Unknown", astraProgress: 400, umbraProgress: 400);
        var state = PresenceFormatter.Build(null, live, MakePlayer(), AllOn);

        Assert.Contains("K:3", state.State);
        Assert.Contains("D:1", state.State);
        Assert.DoesNotContain("Astra", state.State);
        Assert.DoesNotContain("Umbra", state.State);
        Assert.DoesNotContain("%", state.State);
    }

    [Fact]
    public void Build_LiveMatch_Disabled_ShowLiveMatchFalse()
    {
        var cfg = AllOn;
        cfg.ShowLiveMatch = false;
        var live = MakeLive();
        var state = PresenceFormatter.Build(null, live, MakePlayer(), cfg);

        Assert.DoesNotContain("In Game", state.State);
        Assert.DoesNotContain("K:", state.State);
    }

    [Fact]
    public void Build_LiveMatch_NoLocation_DefaultsToCrystallineConflict()
    {
        var player = MakePlayer(location: "");
        var live = MakeLive();
        var state = PresenceFormatter.Build(null, live, player, AllOn);

        Assert.Contains("Crystalline Conflict", state.Details);
    }

    [Fact]
    public void Build_LiveMatch_NoJob_NoLevel()
    {
        var cfg = AllOn;
        cfg.ShowJob = false;
        cfg.ShowLevel = false;
        var live = MakeLive();
        var state = PresenceFormatter.Build(null, live, MakePlayer(), cfg);

        Assert.DoesNotContain("PLD", state.Details);
        Assert.DoesNotContain("Lv.", state.Details);
    }

    [Fact]
    public void Clamp_WithinLimit_ReturnsOriginal()
    {
        Assert.Equal("hello", PresenceFormatter.Clamp("hello", 10));
    }

    [Fact]
    public void Clamp_ExceedsLimit_TruncatesWithEllipsis()
    {
        Assert.Equal("abc...", PresenceFormatter.Clamp("abcdefgh", 6));
    }

    [Fact]
    public void Clamp_ExactLength_ReturnsOriginal()
    {
        var s = new string('x', 5);
        Assert.Equal(s, PresenceFormatter.Clamp(s, 5));
    }

    [Fact]
    public void Clamp_EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", PresenceFormatter.Clamp("", 10));
    }

    [Fact]
    public void Clamp_NullValue_ReturnsEmpty()
    {
        Assert.Equal("", PresenceFormatter.Clamp(null, 10));
    }

    [Fact]
    public void Clamp_ZeroMaxLength_ReturnsEmpty()
    {
        Assert.Equal("", PresenceFormatter.Clamp("hello", 0));
    }

    [Fact]
    public void Clamp_NegativeMaxLength_ReturnsEmpty()
    {
        Assert.Equal("", PresenceFormatter.Clamp("hello", -1));
    }

    [Fact]
    public void Clamp_SmallMaxLength_NoEllipsis()
    {
        Assert.Equal("ab", PresenceFormatter.Clamp("abcde", 2));
    }
}
