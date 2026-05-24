namespace FFXIVRPC.Models;

public readonly record struct DiscordPresenceState(
    string Details,
    string State,
    string LargeImageKey,
    string LargeImageText,
    string SmallImageKey,
    string SmallImageText);
