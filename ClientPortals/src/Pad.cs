using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace ClientPortals;

/// <summary>Gamepad input and the game's own button glyphs (the sprites it shows in its key hints).</summary>
internal static class Pad
{
    /// <summary>Playing with a gamepad (and not its mouse emulation): what the game shows its gamepad hints for.</summary>
    public static bool Active => ZInput.IsGamepadActive() && !ZInput.IsMouseActive();

    public static bool Down(string button) => ZInput.GetButtonDown(button);

    /// <summary>The glyph of a gamepad button as the game draws it (Xbox, PlayStation or Switch, following the controller).</summary>
    public static string Glyph(string button) => Localization.instance.GetBoundKeyString(button, emptyStringOnMissing: true);

    /// <summary>A key hint in the game's style: <c>[key] text</c> with the key in yellow.</summary>
    public static string Hint(string key, string text) => $"<color=yellow><b>{key}</b></color> {text}";

    /// <summary>A keyboard key as the game names it (the player's binding).</summary>
    public static string KeyName(string binding) => Localization.instance.GetBoundKeyString(binding, emptyStringOnMissing: true);

    /// <summary>A key as the game writes it (the keyboard layout's letter: Q is Й on a Russian layout), modifiers first.</summary>
    public static string KeyName(KeyboardShortcut shortcut) =>
        string.Join(" + ", shortcut.Modifiers.Concat(new[] { shortcut.MainKey }).Select(KeyName));

    public static string KeyName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.LeftShift or KeyCode.RightShift: return "Shift";
            case KeyCode.LeftControl or KeyCode.RightControl: return "Ctrl";
            case KeyCode.LeftAlt or KeyCode.RightAlt: return "Alt";
        }
        try
        {
            string name = ZInput.KeyCodeToDisplayName(key);
            if (!string.IsNullOrEmpty(name) && !name.StartsWith("$")) return name.Length == 1 ? name.ToUpperInvariant() : name;
        }
        catch (System.Exception)
        {
            // not a key the game knows: its Unity name below
        }
        return key.ToString();
    }

    /// <summary>Whether a shortcut is held (its modifiers and key), read like the game reads keys.</summary>
    public static bool Held(KeyboardShortcut s) =>
        s.MainKey != KeyCode.None && ZInput.GetKey(s.MainKey, logWarning: false) && s.Modifiers.All(m => ZInput.GetKey(m, logWarning: false));

    /// <summary>Whether a shortcut was pressed this frame (its key went down with the modifiers held).</summary>
    public static bool Pressed(KeyboardShortcut s) =>
        s.MainKey != KeyCode.None && ZInput.GetKeyDown(s.MainKey, logWarning: false) && s.Modifiers.All(m => ZInput.GetKey(m, logWarning: false));
}
