using System;
using DiscordRPC;
using DiscordRPC.Message;
using DiscordRPC.Logging;
using Dalamud.Plugin.Services;
using FFXIVRPC.Models;

namespace FFXIVRPC.Services;

public sealed class DiscordRpcService : IDisposable
{
    private readonly IPluginLog _log;
    private readonly DiscordRpcClient _client;
    private bool _connected;
    private bool _disposed;

    public event Action? Disconnected;

    public DiscordRpcService(IPluginLog log, string appId)
    {
        _log = log;
        _client = new DiscordRpcClient(appId)
        {
            Logger = new ConsoleLogger { Level = LogLevel.Warning },
        };

        _client.OnReady += OnClientReady;
        _client.OnConnectionFailed += OnClientConnectionFailed;
        _client.OnClose += OnClientClose;

        try
        {
            _client.Initialize();
        }
        catch (Exception ex)
        {
            _connected = false;
            _log.Error(ex, "Discord RPC initialization failed.");
        }
    }

    private void OnClientReady(object sender, ReadyMessage e)
    {
        _connected = true;
        _log.Info("Discord ready: {0}", e.User);
    }

    private void OnClientConnectionFailed(object sender, ConnectionFailedMessage _)
    {
        _connected = false;
        _log.Warning("Discord RPC connection failed.");
        Disconnected?.Invoke();
    }

    private void OnClientClose(object sender, CloseMessage _)
    {
        _connected = false;
        _log.Warning("Discord RPC connection closed.");
        Disconnected?.Invoke();
    }

    public bool IsAvailable => !_disposed && _connected;

    public bool TrySetPresence(DiscordPresenceState state, DateTime? startTime = null)
    {
        if (!IsAvailable)
            return false;

        try
        {
            var presence = new RichPresence
            {
                Details = state.Details,
                State = state.State,
                Assets = new Assets
                {
                    LargeImageKey = state.LargeImageKey,
                    LargeImageText = PresenceFormatter.Clamp(state.LargeImageText, PresenceFormatter.DiscordFieldLimit),
                },
            };

            if (!string.IsNullOrEmpty(state.SmallImageKey))
            {
                presence.Assets.SmallImageKey = state.SmallImageKey;
                presence.Assets.SmallImageText = PresenceFormatter.Clamp(state.SmallImageText, PresenceFormatter.DiscordFieldLimit);
            }

            if (startTime.HasValue)
            {
                presence.Timestamps = new Timestamps
                {
                    Start = startTime.Value,
                };
            }

            _client.SetPresence(presence);
            return true;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Discord presence update failed.");
            _connected = false;
            Disconnected?.Invoke();
            return false;
        }
    }

    public void Clear()
    {
        if (!IsAvailable)
            return;

        try
        {
            _client.ClearPresence();
        }
        catch (Exception ex)
        {
            _log.Debug(ex, "Ignored Discord RPC clear exception.");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connected = false;

        _client.OnReady -= OnClientReady;
        _client.OnConnectionFailed -= OnClientConnectionFailed;
        _client.OnClose -= OnClientClose;

        try
        {
            _client.ClearPresence();
            _client.Dispose();
        }
        catch (Exception ex)
        {
            _log.Debug(ex, "Ignored Discord RPC shutdown exception.");
        }
    }
}
