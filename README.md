# FFXIV RPC

A Dalamud plugin that publishes your FFXIV status to Discord Rich Presence.

## What It Shows

- Crystalline Conflict rank (Bronze through Ultima)
- Live K/D and team progress during active CC matches
- Optional legacy PvP rank
- Character name, world, job, level, and location
- Rank-appropriate badge image

## Setup

1. Build in Release.
2. Copy output to `%APPDATA%\XIVLauncher\installedPlugins\FFXIVRPC\`.
3. Enable in `/xlplugins`.
4. Use `/ffxivrpc` for status and config.

## Discord Application

The plugin needs a Discord application with these image assets uploaded:

| Asset key | Description |
|-----------|-------------|
| `ffxiv_logo` | Default fallback image |
| `bronze` | Bronze rank badge |
| `silver` | Silver rank badge |
| `gold` | Gold rank badge |
| `platinum` | Platinum rank badge |
| `diamond` | Diamond rank badge |
| `crystal` | Crystal rank badge (large) + match badge (small) |
| `omega` | Omega rank badge |
| `ultima` | Ultima rank badge |

Set your Discord App ID in `/ffxivrpc config`.

## Configuration

All display elements are toggleable via `/ffxivrpc config`:

| Option | Default | Description |
|--------|---------|-------------|
| Show Name | On | Character name |
| Show World | On | Home world, with visiting indicator |
| Show Job | On | Current job abbreviation |
| Show Level | On | Character level |
| Show Location | On | Current zone |
| Show CC Rank | On | Current CC tier, riser, stars, credit |
| Show Legacy Rank | Off | Series claimed rank |
| Show Live K/D | On | Kill/death count and team progress during active CC matches; overrides rank display |
| Show Rank Icon | On | Discord large image matching tier |
| Show Elapsed Time | On | Session timer in Discord; shows match time during CC |

## Notes

- PvP data is read through FFXIVClientStructs. Field layouts are verified but may shift after game patches.
- Live match K/D uses a ProcessKill hook and HP tracking; kills from environmental sources may not be counted.
- Updates are throttled to 5-second intervals to avoid spamming Discord.
- Old Python bridge tooling has been removed.
