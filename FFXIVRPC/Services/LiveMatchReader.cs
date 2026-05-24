using System;
using System.Threading;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVRPC.Models;

namespace FFXIVRPC.Services;

public sealed unsafe class LiveMatchReader : IDisposable
{
    private readonly IPluginLog _log;
    private readonly IObjectTable _objectTable;

    private delegate void ProcessKillDelegate(
        nint agent, nint killerPlayer, uint killStreak, nint victimPlayer, uint localPlayerTeam);
    private readonly Hook<ProcessKillDelegate>? _processKillHook;
    private readonly bool _hookInstalled;

    private const int CCPlayerSize = 0x138;
    private const int CCPlayer_EntityId = 0xE0;

    private const int ICCD_Base = 0x1F90;
    private const int ICCD_CrystalPosition = ICCD_Base + 0x1A8;
    private const int ICCD_AstraProgress = ICCD_Base + 0x1AC;
    private const int ICCD_UmbraProgress = ICCD_Base + 0x1B0;
    private const int ICCD_AstraOnPoint = ICCD_Base + 0x1B4;
    private const int ICCD_UmbraOnPoint = ICCD_Base + 0x1B8;
    private const int MatchNullTimeoutTicks = 30;

    private int _kills;
    private int _deaths;
    private volatile uint _localEntityId;
    private volatile uint _gameTeam;
    private volatile bool _teamKnown;
    private volatile bool _matchStarted;

    private volatile uint _lastHp;
    private volatile int _deadFlag; // 0=alive, 1=dead

    private int _lastAstraProgress = -1;
    private int _lastUmbraProgress = -1;
    private int _nullPlayerTicks;
    private int _nullFrameworkTicks;
    private LiveMatchSnapshot _lastGoodSnapshot = LiveMatchSnapshot.Empty;
    private volatile bool _disposed;

    public bool IsHookActive => _hookInstalled;

    public LiveMatchReader(IPluginLog log, ISigScanner sigScanner, IGameInteropProvider gameInterop, IObjectTable objectTable)
    {
        _log = log;
        _objectTable = objectTable;
        _gameTeam = 0xFFFFFFFF;
        _teamKnown = false;

        try
        {
            var addr = sigScanner.ScanText(
                "40 55 53 41 54 41 55 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 48 8B 01");

            _log.Information("ScanText returned 0x{0:X}", (long)addr);

            if (addr != nint.Zero)
            {
                _processKillHook = gameInterop.HookFromAddress<ProcessKillDelegate>(addr, OnProcessKill);
                _processKillHook.Enable();
                _hookInstalled = true;
                _log.Information("ProcessKill hook enabled at 0x{0:X}", (long)addr);
            }
            else
            {
                _log.Error("ProcessKill signature not found; live K/D will not work");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Hook install failed");
        }
    }

    public LiveMatchSnapshot Tick()
    {
        try
        {
            if (_disposed) return LiveMatchSnapshot.Empty;
            var localPlayer = _objectTable.LocalPlayer;
            if (localPlayer == null)
            {
                _nullPlayerTicks++;
                if (_nullPlayerTicks > MatchNullTimeoutTicks && _matchStarted)
                    EndMatch();
                return _lastGoodSnapshot.InMatch ? _lastGoodSnapshot : LiveMatchSnapshot.Empty;
            }
            _nullPlayerTicks = 0;

            _localEntityId = localPlayer.EntityId;
            var currentHp = localPlayer.CurrentHp;
            if (_matchStarted && _lastHp > 0 && currentHp == 0 && Interlocked.CompareExchange(ref _deadFlag, 1, 0) == 0)
            {
                Interlocked.Increment(ref _deaths);
                _log.Information("death via HP tick, D={0}", Volatile.Read(ref _deaths));
            }
            else if (currentHp > 0)
            {
                _deadFlag = 0;
            }
            _lastHp = currentHp;

            var ef = EventFramework.Instance();
            if (ef == null)
            {
                _nullFrameworkTicks++;
                if (_nullFrameworkTicks > MatchNullTimeoutTicks && _matchStarted)
                    EndMatch();
                return _lastGoodSnapshot.InMatch ? _lastGoodSnapshot : LiveMatchSnapshot.Empty;
            }
            _nullFrameworkTicks = 0;

            var icd = ef->GetInstanceContentDirector();
            if (icd == null)
            {
                if (_matchStarted) EndMatch();
                return LiveMatchSnapshot.Empty;
            }

            if (icd->InstanceContentType != InstanceContentType.CrystallineConflict)
            {
                if (_matchStarted) EndMatch();
                _lastAstraProgress = -1;
                _lastUmbraProgress = -1;
                return LiveMatchSnapshot.Empty;
            }

            var directorBase = (nint)icd;
            int aProg = *(int*)(directorBase + ICCD_AstraProgress);
            int uProg = *(int*)(directorBase + ICCD_UmbraProgress);

            if (!_matchStarted)
            {
                StartMatch(icd);
            }
            else if (_lastAstraProgress >= 0)
            {
                if (_lastAstraProgress > 0 && _lastUmbraProgress > 0
                    && aProg == 0 && uProg == 0)
                {
                    _log.Information("progress reset (was A={0} U={1}), treating as new match",
                        _lastAstraProgress, _lastUmbraProgress);
                    EndMatch();
                    StartMatch(icd);
                }
            }
            _lastAstraProgress = aProg;
            _lastUmbraProgress = uProg;

            string team = _teamKnown
                ? (_gameTeam == 0 ? "Astra" : "Umbra")
                : "Unknown";

            var snapshot = new LiveMatchSnapshot(
                true, team, Volatile.Read(ref _kills), Volatile.Read(ref _deaths),
                *(int*)(directorBase + ICCD_CrystalPosition),
                aProg, uProg,
                *(int*)(directorBase + ICCD_AstraOnPoint),
                *(int*)(directorBase + ICCD_UmbraOnPoint));
            _lastGoodSnapshot = snapshot;
            return snapshot;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Tick failed, returning last good snapshot");
            return _lastGoodSnapshot.InMatch ? _lastGoodSnapshot : LiveMatchSnapshot.Empty;
        }
    }

    private void StartMatch(InstanceContentDirector* director)
    {
        Interlocked.Exchange(ref _kills, 0);
        Interlocked.Exchange(ref _deaths, 0);
        _deadFlag = 0;
        _gameTeam = 0xFFFFFFFF;
        _teamKnown = false;
        _nullPlayerTicks = 0;
        _nullFrameworkTicks = 0;
        _lastAstraProgress = -1;
        _lastUmbraProgress = -1;

        var localPlayer = _objectTable.LocalPlayer;
        _localEntityId = localPlayer!.EntityId;
        _lastHp = localPlayer.CurrentHp;
        if (_lastHp == 0)
        {
            _deadFlag = 1;
            _log.Warning("match start while dead — deaths before load are lost");
        }

        _matchStarted = true;
    }

    private void EndMatch()
    {
        _log.Information("match ended K={0} D={1}", _kills, _deaths);
        _matchStarted = false;
        _lastAstraProgress = -1;
        _lastUmbraProgress = -1;
        _lastGoodSnapshot = LiveMatchSnapshot.Empty;
    }

    private void OnProcessKill(nint agent, nint killerPlayer, uint killStreak, nint victimPlayer, uint localPlayerTeam)
    {
        try
        {
            if (_disposed) goto Original;
            if (!_matchStarted) goto Original;
            if (killerPlayer == nint.Zero || victimPlayer == nint.Zero) goto Original;

            if (!_teamKnown && localPlayerTeam <= 1)
            {
                _gameTeam = localPlayerTeam;
                _teamKnown = true;
                _log.Information("team resolved from ProcessKill: {0} ({1})", localPlayerTeam,
                    localPlayerTeam == 0 ? "Astra" : "Umbra");
            }

            var killerEid = *(uint*)(killerPlayer + CCPlayer_EntityId);
            var victimEid = *(uint*)(victimPlayer + CCPlayer_EntityId);

            if (killerEid == _localEntityId)
            {
                Interlocked.Increment(ref _kills);
                _log.Information("kill! K={0}", Volatile.Read(ref _kills));
            }
            if (victimEid == _localEntityId)
            {
                if (Interlocked.CompareExchange(ref _deadFlag, 1, 0) == 0)
                {
                    Interlocked.Increment(ref _deaths);
                    _log.Information("hook death! D={0}", Volatile.Read(ref _deaths));
                }
                _lastHp = 0;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "ProcessKill detour error");
        }

    Original:
        _processKillHook?.Original(agent, killerPlayer, killStreak, victimPlayer, localPlayerTeam);
    }

    public void Dispose()
    {
        _disposed = true;
        _processKillHook?.Disable();
        _processKillHook?.Dispose();
    }
}
