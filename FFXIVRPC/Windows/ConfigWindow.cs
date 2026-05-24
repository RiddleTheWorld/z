using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace FFXIVRPC.Windows;

public class ConfigWindow : Window
{
    private readonly Plugin _plugin;

    public ConfigWindow(Plugin plugin) : base("FFXIV RPC Configuration###ffxivrpc_config")
    {
        _plugin = plugin;
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse;
        Size = new Vector2(420, 600);
        SizeCondition = ImGuiCond.Always;
    }

    public override void Draw()
    {
        var cfg = _plugin.Config;

        ImGui.Text("Discord Application");
        ImGui.Separator();

        var appId = cfg.DiscordAppId;
        if (ImGui.InputText("App ID", ref appId, 64))
        {
            var trimmed = appId.Trim();
            if (ulong.TryParse(trimmed, out _))
            {
                cfg.DiscordAppId = trimmed;
                _plugin.SaveConfig();
            }
        }
        HelpMarker("Discord Application ID for Rich Presence.\nLeave as default unless you have your own application.");

        if (ImGui.Button("Reset to Default"))
        {
            cfg.DiscordAppId = Configuration.DefaultDiscordAppId;
            _plugin.SaveConfig();
        }

        ImGui.Spacing();
        ImGui.Text("Player Info");
        ImGui.Separator();

        BoolCheckbox("Show Name", cfg.ShowName, v => cfg.ShowName = v);
        BoolCheckbox("Show World", cfg.ShowWorld, v => cfg.ShowWorld = v);
        BoolCheckbox("Show Job", cfg.ShowJob, v => cfg.ShowJob = v);
        BoolCheckbox("Show Level", cfg.ShowLevel, v => cfg.ShowLevel = v);
        BoolCheckbox("Show Location", cfg.ShowLocation, v => cfg.ShowLocation = v);

        ImGui.Spacing();
        ImGui.Text("PvP Display");
        ImGui.Separator();

        BoolCheckbox("Show CC Rank", cfg.ShowCrystallineConflict, v => cfg.ShowCrystallineConflict = v);
        BoolCheckbox("Show Legacy Rank", cfg.ShowLegacyRank, v => cfg.ShowLegacyRank = v);
        BoolCheckbox("Show Live K/D", cfg.ShowLiveMatch, v => cfg.ShowLiveMatch = v);
        HelpMarker("Live K/D overrides rank display during active CC matches.");
        BoolCheckbox("Show Rank Icon", cfg.ShowRankIcon, v => cfg.ShowRankIcon = v);
        HelpMarker("Shows your CC tier icon as the small Discord image.");
        BoolCheckbox("Show Elapsed Time", cfg.ShowElapsedTime, v => cfg.ShowElapsedTime = v);
        HelpMarker("Shows session time elapsed in Discord. During CC matches, shows match time instead.");

        ImGui.Spacing();
        ImGui.Text("Status");
        ImGui.Separator();

        if (ImGui.Button("Reconnect Discord"))
        {
            _plugin.ReconnectDiscord();
        }

        ImGui.SameLine();

        if (ImGui.Button("Force Update"))
        {
            _plugin.ForceUpdate();
        }

        ImGui.Spacing();
        ImGui.TextWrapped("Updates throttled to 1 per 5s.");
    }

    private void BoolCheckbox(string label, bool current, Action<bool> set)
    {
        var copy = current;
        if (ImGui.Checkbox(label, ref copy))
        {
            set(copy);
            _plugin.SaveConfig();
        }
    }

    private static void HelpMarker(string desc)
    {
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(400f);
            ImGui.TextUnformatted(desc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }
}
