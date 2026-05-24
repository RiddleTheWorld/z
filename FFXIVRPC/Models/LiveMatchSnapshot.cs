namespace FFXIVRPC.Models;

public readonly record struct LiveMatchSnapshot(
    bool InMatch,
    string Team,
    int Kills,
    int Deaths,
    int CrystalPosition,
    int AstraProgress,
    int UmbraProgress,
    int AstraOnPoint,
    int UmbraOnPoint)
{
    public static LiveMatchSnapshot Empty => new(false, "Unknown", 0, 0, 0, 0, 0, 0, 0);
}
