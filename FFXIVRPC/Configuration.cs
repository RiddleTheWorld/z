using Dalamud.Configuration;
using System;

namespace FFXIVRPC;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public const string DefaultDiscordAppId = "1507761070729859122";

    public string DiscordAppId { get; set; } = DefaultDiscordAppId;

    public bool ShowCrystallineConflict { get; set; } = true;
    public bool ShowLegacyRank { get; set; } = false;
    public bool ShowLiveMatch { get; set; } = true;
    public bool ShowRankIcon { get; set; } = true;
    public bool ShowElapsedTime { get; set; } = true;

    public bool ShowName { get; set; } = true;
    public bool ShowWorld { get; set; } = true;
    public bool ShowJob { get; set; } = true;
    public bool ShowLevel { get; set; } = true;
    public bool ShowLocation { get; set; } = true;
}
