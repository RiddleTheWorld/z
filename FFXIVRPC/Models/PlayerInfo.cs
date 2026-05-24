namespace FFXIVRPC.Models;

public readonly record struct PlayerInfo(
    string Name,
    string HomeWorld,
    string CurrentWorld,
    string? Location,
    string? Job,
    int Level)
{
    public static PlayerInfo Empty => new("", "", "", string.Empty, string.Empty, 0);

    public bool HasCharacter => !string.IsNullOrEmpty(Name);

    public bool IsVisiting => !string.IsNullOrEmpty(CurrentWorld) && CurrentWorld != HomeWorld;
}
