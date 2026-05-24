using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace FFXIVRPC.Windows;

public class MainWindow : Window
{
    private readonly Plugin _plugin;
    private readonly ConfigWindow _config;

    public MainWindow(Plugin plugin, ConfigWindow config) : base("FFXIV RPC Status###ffxivrpc_main")
    {
        _plugin = plugin;
        _config = config;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(340, 160),
            MaximumSize = new Vector2(600, 400)
        };
    }

    public override void Draw()
    {
        ImGui.Text("FFXIV Discord Rich Presence");
        ImGui.Separator();

        if (!_plugin.IsKillHookActive)
        {
            ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "Kill tracking unavailable (kills will show 0)");
        }

        var config = _plugin.Config;

        bool anyPvpEnabled = config.ShowCrystallineConflict || config.ShowLegacyRank
            || config.ShowRankIcon || config.ShowLiveMatch || config.ShowElapsedTime;
        bool anyPlayerEnabled = config.ShowName || config.ShowWorld
            || config.ShowJob || config.ShowLevel || config.ShowLocation;

        if (!anyPvpEnabled)
        {
            ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "All PvP options are off.");
            ImGui.SameLine();
            if (ImGui.Button("Config##pvp"))
                _config.Toggle();
        }

        if (!anyPlayerEnabled)
        {
            ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "No player info enabled!");
            ImGui.SameLine();
            if (ImGui.Button("Config##player"))
                _config.Toggle();
        }

        ImGui.Text("Status: Active");
        ImGui.Text("Only updates on change (throttled).");

        if (ImGui.Button("Open Config"))
            _config.Toggle();
    }
}
