using HarmonyLib;
using UnityEngine;

namespace ClientPortals;

/// <summary>What a portal opens: the list, the map view, or the one used last.</summary>
public enum PickerView { Last, List, Map }

/// <summary>Opens the destination picker in the view of <see cref="Plugin.View"/>.</summary>
internal static class Picker
{
    public static bool IsOpen => ListWindow.IsOpen || MapPicker.IsOpen;

    public static void Open(TeleportWorld portal)
    {
        if (IsOpen || Travel.Busy || !portal) return;
        bool map = Plugin.View.Value switch
        {
            PickerView.Map => true,
            PickerView.List => false,
            _ => Journal.LastViewMap,
        };
        if (map && !Game.m_noMap && Minimap.instance) MapPicker.Open(portal);
        else ListWindow.Open(portal);
    }
}

[HarmonyPatch(typeof(TeleportWorld))]
internal static class PortalPatches
{
    [HarmonyPostfix, HarmonyPatch(nameof(TeleportWorld.GetHoverText))]
    static void HoverText(TeleportWorld __instance, ref string __result)
    {
        if (Journal.Current == null) return;
        __result += "\n" + (Pad.Active ? PadHint(TravelKey.Hint) : KeyHint(TravelKey.Hint)) + Texts.T("travel_to");
        if (!PairRename.CanRename(__instance)) return;
        if (Pad.Active)
        {
            // the key hint the game shows for its own alt-use actions, as the game writes it: a mod that moves alt-use
            // to its own button (Zen.ModLib) puts that button in its place
            string keys = ZInput.IsNonClassicFunctionality() ? "$KEY_AltKeys + $KEY_Use" : "$KEY_AltPlace + $KEY_Use";
            __result += Localization.instance.Localize($"\n[<color=yellow><b>{keys}</b></color>] ") + Texts.T("rename_both");
        }
        else __result += "\n" + KeyHint(Pad.KeyName(Plugin.RenameKey.Value)) + Texts.T("rename_both");
    }

    /// <summary>
    /// A keyboard key hint like the game's, its tags in another order: a mod rewriting the game's hints (Zen.ModLib turns
    /// "[Shift + E]" into its alternate interact key, Q) must leave the mod's own keys alone, they are read here.
    /// </summary>
    static string KeyHint(string key) => $"[<b><color=yellow>{key}</color></b>] ";

    /// <summary>A gamepad button's hint as the game writes it: the HUD drops the brackets around a glyph written this way.</summary>
    static string PadHint(string glyph) => $"[<color=yellow><b>{glyph}</b></color>] ";

    /// <summary>
    /// The gamepad's alt + use at a paired portal renames both ends with <see cref="Plugin.RenameBoth"/>. On the keyboard
    /// the travel key and <see cref="Plugin.RenameKey"/> act at a portal (<see cref="TravelKey.Update"/>,
    /// <see cref="PairRename.UpdateKey"/>): while one is held, the game's own use of the portal (its tag dialog) stays out,
    /// also when a mod makes that key the game's alt-use (Zen.ModLib's Q) or Shift + E plain use.
    /// </summary>
    [HarmonyPrefix, HarmonyPatch(nameof(TeleportWorld.Interact))]
    static bool Interact(TeleportWorld __instance, Humanoid human, bool hold, bool alt, ref bool __result)
    {
        if (hold || human != Player.m_localPlayer || Journal.Current == null) return true;
        if (Pad.Active)
        {
            if (!alt || !PairRename.CanRename(__instance)) return true;
            PairRename.Open(__instance);
        }
        else if (!TravelKey.KeyHeld && !(PairRename.CanRename(__instance) && Pad.Held(Plugin.RenameKey.Value))) return true;
        __result = true;
        return false;
    }

    /// <summary>Walking into an unpaired portal opens the picker; arriving doesn't send the player on.</summary>
    [HarmonyPrefix, HarmonyPatch(nameof(TeleportWorld.Teleport))]
    static bool Teleport(TeleportWorld __instance, Player player)
    {
        if (player != Player.m_localPlayer) return true;
        if (Travel.JustArrived || Travel.Busy) return false;
        if (PortalWorld.IsPaired(__instance)) return true;
        if (Plugin.WalkIn.Value && PortalWorld.HasDestinations(PortalWorld.PositionOf(__instance))) Picker.Open(__instance);
        return true;
    }

    /// <summary>An unpaired portal with somewhere to go looks like a paired one: color, and the swirl when the player could go through.</summary>
    [HarmonyPostfix, HarmonyPatch(nameof(TeleportWorld.UpdatePortal))]
    static void UpdatePortal(TeleportWorld __instance)
    {
        if (!Plugin.ShowActive.Value || !__instance.m_nview || !__instance.m_nview.IsValid() || !__instance.m_proximityRoot) return;
        if (PortalWorld.IsPaired(__instance) || !PortalWorld.HasDestinations(PortalWorld.PositionOf(__instance))) return;
        __instance.m_hadTarget = true;
        var player = Player.GetClosestPlayer(__instance.m_proximityRoot.position, __instance.m_activationRange);
        __instance.m_target_found.SetActive(player && player == Player.m_localPlayer && player.IsTeleportable(__instance.m_allowAllItems));
    }
}

/// <summary>
/// The travel key at a portal (Q, or the gamepad's left stick press) opens the picker. Both are game keys too (Q runs
/// on its own, the stick press crouches or runs): while one is held at a portal, the player's controls take no input,
/// so the press does only this. The stick press is also the game's alt key on a gamepad (alt + use renames a pair):
/// its picker opens when the stick is let go without the use button pressed meanwhile.
/// </summary>
internal static class TravelKey
{
    const string PadButton = "JoyLStick";

    /// <summary>The portal the player is looking at (close enough to use), or null.</summary>
    public static TeleportWorld Hovered()
    {
        var player = Player.m_localPlayer;
        var hover = player ? player.GetHoverObject() : null;
        return hover ? hover.GetComponentInParent<TeleportWorld>() : null;
    }

    /// <summary>The keyboard's travel key is held.</summary>
    public static bool KeyHeld => Pad.Held(Plugin.TravelKey.Value);

    static bool Held => KeyHeld || ZInput.GetButton(PadButton);

    /// <summary>The stick was pressed at a portal and nothing else happened yet: let go, it opens the picker.</summary>
    static bool padPending;

    /// <summary>The key as the hover text shows it: the gamepad's button glyph, or the keyboard key as the game names it.</summary>
    public static string Hint => Pad.Active ? Pad.Glyph(PadButton) : Pad.KeyName(Plugin.TravelKey.Value);

    public static bool Suppressing => !Picker.IsOpen && Journal.Current != null && Held && Hovered();

    public static void Update()
    {
        if (Picker.IsOpen || Travel.Busy || Journal.Current == null || GameUiOpen)
        {
            padPending = false;
            return;
        }
        bool open = Pad.Pressed(Plugin.TravelKey.Value);
        if (Pad.Down(PadButton)) padPending = true;
        if (padPending && (ZInput.GetButtonDown("JoyUse") || !Hovered())) padPending = false;
        if (padPending && ZInput.GetButtonUp(PadButton))
        {
            padPending = false;
            open = true;
        }
        var portal = open ? Hovered() : null;
        if (portal) Picker.Open(portal);
    }

    /// <summary>A window or text field of the game has the keys.</summary>
    public static bool GameUiOpen =>
        TextInput.IsVisible() || Menu.IsVisible() || Console.IsVisible() || Chat.instance && Chat.instance.HasFocus() ||
        InventoryGui.IsVisible() || Minimap.IsOpen() || Hud.IsPieceSelectionVisible();

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
    static class NoControls
    {
        static void Postfix(bool look, ref bool __result)
        {
            if (!look && __result && Suppressing) __result = false;
        }
    }
}

/// <summary>
/// While the list is open (or the map view's search box is being typed in), the game treats it like its own text
/// dialog: no moving, no camera, free cursor, Esc doesn't open the menu, the map key doesn't close the map.
/// </summary>
[HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
internal static class BlockGameInput
{
    static void Postfix(ref bool __result) => __result |= ListWindow.BlocksInput || MapPicker.BlocksInput;
}

/// <summary>
/// The quick slot bar reads the D-pad (up uses the selected slot, left/right select one) without asking about dialogs
/// like the list: not while it is open. Its icons still update.
/// </summary>
[HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.Update))]
internal static class NoHotbarUnderTheList
{
    static bool Prefix(HotkeyBar __instance)
    {
        if (!ListWindow.BlocksInput) return true;
        __instance.UpdateIcons(Player.m_localPlayer);
        return false;
    }
}

/// <summary>The inventory opens on the gamepad's Y (our favorite button) without asking about dialogs like the list: not while it is open.</summary>
[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
internal static class NoInventoryUnderTheList
{
    static bool Prefix() => !ListWindow.BlocksInput || InventoryGui.IsVisible();
}

/// <summary>
/// The mouse wheel scrolls the list (<see cref="ListWindow"/> reads the mouse itself): the game's wheel reads nothing
/// meanwhile, so the camera behind the list doesn't zoom (the game doesn't ask about dialogs there).
/// </summary>
[HarmonyPatch(typeof(ZInput), "Internal_GetMouseScrollWheel")]
internal static class NoWheelUnderTheList
{
    static void Postfix(ref float __result)
    {
        if (ListWindow.BlocksInput) __result = 0f;
    }
}

/// <summary>
/// Other mods often read their hotkeys straight from Unity's keyboard (B for a build camera...) without asking whether
/// a text field has the keys: while the list or the map view is open (letters go to its search box), those reads see
/// no key. The game itself reads the keyboard elsewhere (its input system), as does this
/// mod, and text fields get their characters from text events.
/// </summary>
[HarmonyPatch]
internal static class NoKeysForOtherMods
{
    static bool Typing => ListWindow.BlocksInput || MapPicker.BlocksInput;

    [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKey), typeof(KeyCode))]
    static bool Key(KeyCode key, ref bool __result) => Pass(key, ref __result);

    [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), typeof(KeyCode))]
    static bool KeyDown(KeyCode key, ref bool __result) => Pass(key, ref __result);

    [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), typeof(KeyCode))]
    static bool KeyUp(KeyCode key, ref bool __result) => Pass(key, ref __result);

    /// <summary>Keyboard keys only: mouse buttons and joysticks come after them in <see cref="KeyCode"/>.</summary>
    static bool Pass(KeyCode key, ref bool __result)
    {
        if (key >= KeyCode.Mouse0 || !Typing) return true;
        __result = false;
        return false;
    }
}

/// <summary>The game's own tag dialog at a portal (10 characters) allows <see cref="Plugin.NameLimit"/>, like the mod's rename dialogs.</summary>
[HarmonyPatch(typeof(TextInput), nameof(TextInput.RequestText))]
internal static class LongerPortalNames
{
    static void Prefix(TextReceiver sign, ref int charLimit)
    {
        if (sign is TeleportWorld) charLimit = Mathf.Max(charLimit, Plugin.NameLimit.Value);
    }
}

/// <summary>F2 renames in the list; the game's network panel (F2 too, without asking about dialogs) stays as it was.</summary>
[HarmonyPatch(typeof(ConnectPanel), nameof(ConnectPanel.Update))]
internal static class NoNetworkPanelUnderTheList
{
    static void Prefix(ConnectPanel __instance, out bool __state) => __state = __instance.m_root && __instance.m_root.gameObject.activeSelf;

    static void Postfix(ConnectPanel __instance, bool __state)
    {
        if (ListWindow.BlocksInput && __instance.m_root && __instance.m_root.gameObject.activeSelf != __state)
            __instance.m_root.gameObject.SetActive(__state);
    }
}
