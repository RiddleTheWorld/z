using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVRPC.Services;
using FFXIVRPC.Windows;

namespace FFXIVRPC;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;

    public Configuration Config { get; }
    public WindowSystem Windows { get; } = new("FFXIVRPC");

    private readonly ConfigWindow _configWindow;
    private readonly MainWindow _mainWindow;
    private readonly PlayerInfoService _playerInfo;
    private readonly PvPDataReader _pvpReader;
    private readonly LiveMatchReader _liveMatch;
    private PresenceCoordinator _coordinator;

    public bool IsKillHookActive => _liveMatch.IsHookActive;

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        if (string.IsNullOrWhiteSpace(Config.DiscordAppId))
        {
            Config.DiscordAppId = Configuration.DefaultDiscordAppId;
            SaveConfig();
            Log.Information("Discord App ID defaulted. Set your own in /ffxivrpc config.");
        }

        _playerInfo = new PlayerInfoService(PlayerState, ClientState, DataManager, Log);
        _pvpReader = new PvPDataReader(Log);
        _liveMatch = new LiveMatchReader(Log, SigScanner, GameInterop, ObjectTable);
        var discord = new DiscordRpcService(Log, Config.DiscordAppId);
        _coordinator = new PresenceCoordinator(_pvpReader, _liveMatch, _playerInfo, discord, Config);

        _configWindow = new ConfigWindow(this);
        _mainWindow = new MainWindow(this, _configWindow);
        Windows.AddWindow(_configWindow);
        Windows.AddWindow(_mainWindow);

        PluginInterface.UiBuilder.Draw += Windows.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        CommandManager.AddHandler("/ffxivrpc", new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle status window. /ffxivrpc config"
        });

        Framework.Update += OnTick;
        ClientState.TerritoryChanged += OnZoneChange;
        ClientState.Login += OnLogin;
        ClientState.Logout += OnLogout;

        Log.Information("FFXIV RPC {0} loaded", PluginInterface.Manifest.AssemblyVersion);
    }

    public void Dispose()
    {
        Framework.Update -= OnTick;
        ClientState.TerritoryChanged -= OnZoneChange;
        ClientState.Login -= OnLogin;
        ClientState.Logout -= OnLogout;

        CommandManager.RemoveHandler("/ffxivrpc");

        PluginInterface.UiBuilder.Draw -= Windows.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        Windows.RemoveAllWindows();

        _coordinator.Dispose();
        _liveMatch.Dispose();
    }

    private void OnTick(IFramework _) => _coordinator.Tick(ClientState);
    private void OnZoneChange(uint _) => _coordinator.ForceTick(ClientState);
    private void OnLogin() => _coordinator.ForceTick(ClientState);
    private void OnLogout(int _, int __) => _coordinator.ForceTick(ClientState);

    private void OnCommand(string _, string args)
    {
        if (args.Trim().Equals("config", StringComparison.OrdinalIgnoreCase))
            _configWindow.Toggle();
        else
            _mainWindow.Toggle();
    }

    private void ToggleConfigUi() => _configWindow.Toggle();

    public void SaveConfig()
    {
        try
        {
            PluginInterface.SavePluginConfig(Config);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save plugin configuration");
        }
    }

    internal void ForceUpdate() => _coordinator.ForceTick(ClientState);

    internal void ReconnectDiscord()
    {
        if (string.IsNullOrWhiteSpace(Config.DiscordAppId))
        {
            Log.Warning("Cannot reconnect Discord: App ID is empty or whitespace");
            return;
        }

        var old = _coordinator;
        PresenceCoordinator? next = null;
        try
        {
            var discord = new DiscordRpcService(Log, Config.DiscordAppId);
            next = new PresenceCoordinator(_pvpReader, _liveMatch, _playerInfo, discord, Config);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create new Discord connection; existing coordinator preserved");
            return;
        }

        _coordinator = next;
        old.Dispose();

        Log.Information("Discord reconnected with App ID: {0}", Config.DiscordAppId);
        ForceUpdate();
    }
}
