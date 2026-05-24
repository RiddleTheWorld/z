namespace FFXIVRPC.Models;

public readonly record struct PvPProfileSnapshot(
    byte SeriesClaimedRank,
    ushort CrystallineConflictCurrentCrystalCredit,
    byte CrystallineConflictCurrentRank,
    byte CrystallineConflictHighestRank,
    byte CrystallineConflictCurrentRiser,
    byte CrystallineConflictHighestRiser,
    byte CrystallineConflictCurrentRisingStars,
    byte CrystallineConflictHighestRisingStars)
{
    public bool IsEmpty => SeriesClaimedRank == 0
        && CrystallineConflictCurrentRank == 0
        && CrystallineConflictHighestRank == 0
        && CrystallineConflictCurrentCrystalCredit == 0
        && CrystallineConflictCurrentRiser == 0
        && CrystallineConflictHighestRiser == 0
        && CrystallineConflictCurrentRisingStars == 0
        && CrystallineConflictHighestRisingStars == 0;
}
