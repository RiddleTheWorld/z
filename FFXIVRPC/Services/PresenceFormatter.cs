using FFXIVRPC.Models;

namespace FFXIVRPC.Services;

public static class PresenceFormatter
{
    private const string LogoKey = "ffxiv_logo";
    internal const int DiscordFieldLimit = 128;
    private const float CcProgressPercentDivisor = 10f;

    public static DiscordPresenceState Build(PvPProfileSnapshot? pvp, LiveMatchSnapshot live, PlayerInfo player, Configuration config)
    {
        var (rankKey, rankText) = ImageFor(pvp, config);

        if (live.InMatch && config.ShowLiveMatch)
            return BuildMatchPresence(live, player, rankKey, rankText, config);

        return BuildCharacterPresence(pvp, player, rankKey, rankText, config);
    }

    private static DiscordPresenceState BuildCharacterPresence(PvPProfileSnapshot? pvp, PlayerInfo player,
        string rankKey, string rankText, Configuration config)
    {
        var details = Clamp(FormatPlayer(player, config), DiscordFieldLimit);
        if (string.IsNullOrEmpty(details))
            details = "Final Fantasy XIV";
        var state = Clamp(FormatRank(pvp, config), DiscordFieldLimit);

        return new DiscordPresenceState(details, state, rankKey, rankText, LogoKey, "Final Fantasy XIV");
    }

    private static string FormatRank(PvPProfileSnapshot? pvp, Configuration config)
    {
        if (pvp == null || pvp.Value.IsEmpty)
            return "Adventuring in Eorzea";

        string parts = "";

        if (config.ShowCrystallineConflict && pvp.Value.CrystallineConflictCurrentRank > 0)
        {
            parts = RankMapper.FormatCrystallineConflict(
                pvp.Value.CrystallineConflictCurrentRank,
                pvp.Value.CrystallineConflictCurrentRiser,
                pvp.Value.CrystallineConflictCurrentRisingStars,
                pvp.Value.CrystallineConflictCurrentCrystalCredit);
        }

        if (config.ShowLegacyRank && pvp.Value.SeriesClaimedRank > 0)
        {
            var legacy = RankMapper.FormatLegacyRank(pvp.Value.SeriesClaimedRank);
            parts = string.IsNullOrEmpty(parts) ? legacy : parts + " | " + legacy;
        }

        return string.IsNullOrEmpty(parts) ? "Adventuring in Eorzea" : parts;
    }

    private static (string Key, string Text) ImageFor(PvPProfileSnapshot? pvp, Configuration config)
    {
        if (!config.ShowRankIcon)
            return (LogoKey, "Final Fantasy XIV");

        var rank = pvp?.CrystallineConflictCurrentRank ?? 0;
        if (rank == 0)
            return (LogoKey, "Final Fantasy XIV");

        string key;
        switch (rank)
        {
            case 1: key = "bronze"; break;
            case 2: key = "silver"; break;
            case 3: key = "gold"; break;
            case 4: key = "platinum"; break;
            case 5: key = "diamond"; break;
            case 6: key = "crystal"; break;
            case 7: key = "omega"; break;
            case 8: key = "ultima"; break;
            default: key = LogoKey; break;
        }

        return (key, RankMapper.GetCrystallineConflictTierName(rank));
    }

    private static string FormatPlayer(PlayerInfo player, Configuration config)
    {
        string name = "";
        if (config.ShowName)
            name = player.Name;
        if (config.ShowWorld && !string.IsNullOrEmpty(player.HomeWorld))
        {
            var worldText = string.IsNullOrEmpty(name) ? player.HomeWorld : "@" + player.HomeWorld;
            name = string.IsNullOrEmpty(name) ? worldText : name + " " + worldText;
            if (player.IsVisiting && !string.IsNullOrEmpty(player.CurrentWorld))
                name += " (visiting " + player.CurrentWorld + ")";
        }

        var job = FormatJobLevel(player, config);
        var location = config.ShowLocation && !string.IsNullOrEmpty(player.Location) ? player.Location : "";

        return JoinNonEmpty(name, job, location);
    }

    private static string FormatJobLevel(PlayerInfo player, Configuration config)
    {
        if (config.ShowJob && !string.IsNullOrEmpty(player.Job))
        {
            return config.ShowLevel && player.Level > 0
                ? $"{player.Job} Lv.{player.Level}"
                : player.Job;
        }
        if (config.ShowLevel && player.Level > 0)
        {
            return $"Lv.{player.Level}";
        }
        return "";
    }

    private static DiscordPresenceState BuildMatchPresence(LiveMatchSnapshot live, PlayerInfo player,
        string imgKey, string imgText, Configuration config)
    {
        var job = FormatJobLevel(player, config);
        var arena = !string.IsNullOrEmpty(player.Location) ? player.Location : "Crystalline Conflict";
        var details = JoinNonEmpty($"Crystalline Conflict - {arena}", job);

        var myTeam = live.Team;
        int myProgress;
        if (myTeam == "Umbra") myProgress = live.UmbraProgress;
        else if (myTeam == "Astra") myProgress = live.AstraProgress;
        else myProgress = -1;

        var state = myProgress >= 0
            ? $"In Game - K:{live.Kills} D:{live.Deaths} - {myTeam} {myProgress / CcProgressPercentDivisor:F0}%"
            : $"In Game - K:{live.Kills} D:{live.Deaths}";

        return new DiscordPresenceState(
            Clamp(details, DiscordFieldLimit),
            Clamp(state, DiscordFieldLimit),
            imgKey,
            imgText,
            "crystal",
            "In Match"
        );
    }

    internal static string Clamp(string? value, int maxLength)
    {
        if (maxLength <= 0)
            return "";
        if (string.IsNullOrEmpty(value))
            return "";
        if (value.Length <= maxLength)
            return value;
        if (maxLength < 3)
            return value[..maxLength];
        return value[..(maxLength - 3)] + "...";
    }

    private static string JoinNonEmpty(string first, string second, string third)
    {
        if (string.IsNullOrEmpty(first))
            return string.IsNullOrEmpty(second) ? third : string.IsNullOrEmpty(third) ? second : second + " | " + third;
        if (string.IsNullOrEmpty(second))
            return string.IsNullOrEmpty(third) ? first : first + " | " + third;
        if (string.IsNullOrEmpty(third))
            return first + " | " + second;
        return first + " | " + second + " | " + third;
    }

    private static string JoinNonEmpty(string first, string second)
    {
        if (string.IsNullOrEmpty(first))
            return second;
        if (string.IsNullOrEmpty(second))
            return first;
        return first + " | " + second;
    }
}
