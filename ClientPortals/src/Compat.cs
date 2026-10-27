using BepInEx.Bootstrap;
using UnityEngine;

namespace ClientPortals;

/// <summary>
/// Mods that do one of ClientPortals' optional features themselves: when one is installed, that feature stays off here
/// so the two don't both act.
/// </summary>
internal static class Compat
{
    const string TagConnectedPortalsGuid = "neobotics.valheim_mod.tagconnectedportals";
    const string OverheadTextGuid = "ModdedWolf.Teleporter_Overhead_Text";
    const string ZenModLibGuid = "ZenDragon.Zen.ModLib";

    /// <summary>TagConnectedPortals renames both ends of a pair (its modifier + Use).</summary>
    public static bool TagConnectedPortals => Chainloader.PluginInfos.ContainsKey(TagConnectedPortalsGuid);

    /// <summary>Teleporter Overhead Text draws portal names above the portals.</summary>
    public static bool OverheadText => Chainloader.PluginInfos.ContainsKey(OverheadTextGuid);

    /// <summary>
    /// Zen.ModLib's "Alternate Interact Key" (Q by default; None: off): the game's alt-use on one key, and the game's
    /// key hints for alt-use rewritten to it.
    /// </summary>
    static KeyCode ZenAltKey
    {
        get
        {
            if (!Chainloader.PluginInfos.TryGetValue(ZenModLibGuid, out var info) || !info.Instance) return KeyCode.None;
            return info.Instance.Config.TryGetEntry<KeyCode>("Input", "Alternate Interact Key", out var entry) ? entry.Value : KeyCode.None;
        }
    }

    public static void Log()
    {
        var zen = ZenAltKey;
        if (zen != KeyCode.None && zen == Plugin.TravelKey.Value.MainKey)
            Debug.Log($"[{Plugin.Name}] Zen.ModLib's Alternate Interact Key is {zen}, like TravelKey: at a portal it opens the destinations");
        if (TagConnectedPortals)
            Debug.Log($"[{Plugin.Name}] TagConnectedPortals is installed: renaming both ends of a pair is left to it (RenameBoth is off)");
        if (OverheadText)
            Debug.Log($"[{Plugin.Name}] Teleporter Overhead Text is installed: portal names above portals are left to it (Labels are off)");
    }
}
