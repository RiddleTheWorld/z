namespace FFXIVRPC.Models;

public static class RankMapper
{
    public static string GetCrystallineConflictTierName(byte rank) => rank switch
    {
        0 => "Unranked",
        1 => "Bronze",
        2 => "Silver",
        3 => "Gold",
        4 => "Platinum",
        5 => "Diamond",
        6 => "Crystal",
        7 => "Omega",
        8 => "Ultima",
        _ => $"Unknown({rank})"
    };

    public static string FormatCrystallineConflict(byte rank, byte riser, byte stars, ushort credit)
    {
        if (rank == 0) return "";

        var tier = GetCrystallineConflictTierName(rank);

        // Unknown/future ranks — just show the tier name, no riser or credit
        if (rank > 8) return tier;

        var starText = stars > 0 ? $" ({stars} stars)" : "";

        if (rank >= 6)
            return $"{tier} ({credit} CC){starText}";

        var sub = riser > 0 ? $" {riser}" : "";
        return $"{tier}{sub}{starText}";
    }

    public static string FormatLegacyRank(byte rank) =>
        rank == 0 ? "" : $"PvP Rank {rank}";
}
