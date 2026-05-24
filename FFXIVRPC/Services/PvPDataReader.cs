using System;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVRPC.Models;

namespace FFXIVRPC.Services;

internal sealed class PvPDataReader
{
    private readonly IPluginLog _log;
    private DateTime _lastErrorLogged = DateTime.MinValue;
    private const int ErrorLogIntervalSeconds = 60;

    public PvPDataReader(IPluginLog log)
    {
        _log = log;
    }

    public unsafe PvPProfileSnapshot? Read()
    {
        try
        {
            var profile = PvPProfile.Instance();
            if (profile == null || !profile->IsLoaded)
                return null;

            _lastErrorLogged = DateTime.MinValue;
            return new PvPProfileSnapshot(
                profile->SeriesClaimedRank,
                profile->CrystallineConflictCurrentCrystalCredit,
                profile->CrystallineConflictCurrentRank,
                profile->CrystallineConflictHighestRank,
                profile->CrystallineConflictCurrentRiser,
                profile->CrystallineConflictHighestRiser,
                profile->CrystallineConflictCurrentRisingStars,
                profile->CrystallineConflictHighestRisingStars
            );
        }
        catch (Exception ex)
        {
            var now = DateTime.UtcNow;
            if (now - _lastErrorLogged >= TimeSpan.FromSeconds(ErrorLogIntervalSeconds))
            {
                _log.Error(ex, "PvPProfile read failed");
                _lastErrorLogged = now;
            }
            return null;
        }
    }
}
