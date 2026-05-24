using System;
using System.Threading;
using Dalamud.Plugin.Services;
using FFXIVRPC.Models;

namespace FFXIVRPC.Services;

internal sealed class PresenceCoordinator : IDisposable
{
    private readonly PvPDataReader _pvp;
    private readonly LiveMatchReader _liveMatch;
    private readonly PlayerInfoService _player;
    private readonly DiscordRpcService _discord;
    private readonly Configuration _config;

    private DiscordPresenceState? _last;
    private DateTime? _sessionStart;
    private DateTime? _matchStart;
    private volatile int _disconnectSignaled;
    private DateTime _lastSend = DateTime.MinValue;
    private DateTime _lastEval = DateTime.MinValue;
    private bool _hasSentOnce;
    private volatile bool _disposed;
    private bool _lastShowElapsedTime;

    private static readonly TimeSpan EvalThrottle = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SendThrottle = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ForceTickOffset = TimeSpan.FromSeconds(4.5);

    public PresenceCoordinator(
        PvPDataReader pvp,
        LiveMatchReader liveMatch,
        PlayerInfoService player,
        DiscordRpcService discord,
        Configuration config)
    {
        _pvp = pvp;
        _liveMatch = liveMatch;
        _player = player;
        _discord = discord;
        _config = config;
        _discord.Disconnected += OnDiscordDisconnected;
    }

    public void Tick(IClientState state)
    {
        if (_disposed)
            return;

        var live = _liveMatch.Tick();

        if (live.InMatch)
            _matchStart ??= DateTime.UtcNow;
        else
            _matchStart = null;

        if (!_discord.IsAvailable)
            return;

        if (!state.IsLoggedIn)
        {
            Clear();
            return;
        }

        _sessionStart ??= DateTime.UtcNow;

        var now = DateTime.UtcNow;

        if (Interlocked.Exchange(ref _disconnectSignaled, 0) != 0)
        {
            _last = null;
            _hasSentOnce = false;
            _lastSend = DateTime.MinValue;
        }

        if (now - _lastEval < EvalThrottle && _hasSentOnce)
            return;

        var info = _player.GetCurrentInfo();
        if (!info.HasCharacter)
        {
            Clear();
            return;
        }

        var snapshot = _pvp.Read();
        var next = PresenceFormatter.Build(snapshot, live, info, _config);

        _lastEval = now;

        if (_config.ShowElapsedTime != _lastShowElapsedTime)
        {
            _lastShowElapsedTime = _config.ShowElapsedTime;
            _last = null;
        }

        var lastSnapshot = _last;
        if (lastSnapshot.HasValue && next.Equals(lastSnapshot.Value))
            return;

        if (now - _lastSend < SendThrottle)
            return;

        DateTime? startTime = null;
        if (_config.ShowElapsedTime)
            startTime = live.InMatch ? _matchStart : _sessionStart;

        if (!_discord.TrySetPresence(next, startTime))
            return;

        _last = next;
        _lastSend = now;
        _hasSentOnce = true;
    }

    public void ClearPresence()
    {
        if (_disposed)
            return;

        Clear();
    }

    public void ForceTick(IClientState state)
    {
        _lastSend = DateTime.UtcNow - ForceTickOffset;
        _hasSentOnce = false;
        Tick(state);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _discord.Disconnected -= OnDiscordDisconnected;
        Clear();
        _discord.Dispose();
    }

    private void OnDiscordDisconnected()
    {
        Interlocked.Exchange(ref _disconnectSignaled, 1);
    }

    private void Clear()
    {
        _sessionStart = null;
        _matchStart = null;
        _hasSentOnce = false;
        _lastEval = DateTime.MinValue;
        _lastSend = DateTime.MinValue;
        if (_last is not null)
        {
            _discord.Clear();
            _last = null;
        }
    }
}
