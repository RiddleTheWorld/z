using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using FFXIVRPC.Models;

namespace FFXIVRPC.Services;

internal sealed class PlayerInfoService
{
    private readonly IPlayerState _playerState;
    private readonly IClientState _clientState;
    private readonly IPluginLog _log;
    private readonly ExcelSheet<World>? _worldSheet;
    private readonly Dictionary<uint, string> _worldCache = new();
    private readonly ExcelSheet<ClassJob>? _classJobSheet;
    private readonly Dictionary<uint, string?> _jobCache = new();
    private readonly ExcelSheet<TerritoryType>? _territoryTypeSheet;
    private readonly Dictionary<uint, string?> _territoryCache = new();

    public PlayerInfoService(IPlayerState playerState, IClientState clientState, IDataManager dataManager, IPluginLog log)
    {
        _playerState = playerState;
        _clientState = clientState;
        _log = log;
        _worldSheet = dataManager.GetExcelSheet<World>();
        _classJobSheet = dataManager.GetExcelSheet<ClassJob>();
        _territoryTypeSheet = dataManager.GetExcelSheet<TerritoryType>();
    }

    public PlayerInfo GetCurrentInfo()
    {
        if (!_playerState.IsLoaded)
            return PlayerInfo.Empty;

        var name = _playerState.CharacterName;
        if (string.IsNullOrEmpty(name))
            return PlayerInfo.Empty;

        return new PlayerInfo(
            name,
            GetWorld(_playerState.HomeWorld.RowId),
            GetWorld(_playerState.CurrentWorld.RowId),
            GetTerritory(_clientState.TerritoryType),
            GetJob(_playerState.ClassJob.RowId),
            _playerState.Level
        );
    }

    private string GetWorld(uint id)
    {
        if (id == 0) return "";
        if (_worldCache.TryGetValue(id, out var cached)) return cached;

        try
        {
            var name = _worldSheet?.GetRowOrDefault(id)?.Name.ToString() ?? "";
            _worldCache[id] = name;
            return name;
        }
        catch (Exception ex)
        {
            _log.Debug(ex, "World lookup failed for {0}", id);
            return "";
        }
    }

    private string? GetJob(uint id)
    {
        if (id == 0) return null;
        if (_jobCache.TryGetValue(id, out var cached)) return cached;

        string? name = null;
        try
        {
            name = _classJobSheet?.GetRowOrDefault(id)?.Abbreviation.ToString();
        }
        catch (Exception ex)
        {
            _log.Debug(ex, "Job lookup failed for {0}", id);
        }

        _jobCache[id] = name;
        return name;
    }

    private string? GetTerritory(uint id)
    {
        if (id == 0) return null;
        if (_territoryCache.TryGetValue(id, out var cached)) return cached;

        try
        {
            var name = _territoryTypeSheet?.GetRowOrDefault(id)?.PlaceName.Value.Name.ToString();
            var result = string.IsNullOrWhiteSpace(name) ? null : name;
            _territoryCache[id] = result;
            return result;
        }
        catch (Exception ex)
        {
            _log.Debug(ex, "Territory lookup failed for {0}", id);
            return null;
        }
    }
}
