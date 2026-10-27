using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace ClientPortals;

public enum LabelColor { Custom, White, SoftGold, Red, Green, Blue, Yellow, Cyan, Magenta, Orange, RGBCycleSlow, RGBCycleMedium, RGBCycleFast }

/// <summary>
/// Portal names floating above the portals (the settings of the Teleporter Overhead Text mod): seen from a distance,
/// hidden behind walls, a key to show or hide them. Off while that mod is installed.
/// </summary>
internal static class PortalLabels
{
    public static ConfigEntry<bool> Enabled;
    public static ConfigEntry<KeyboardShortcut> ToggleKey;
    public static ConfigEntry<float> HeightOffset;
    public static ConfigEntry<string> FontName;
    public static ConfigEntry<float> Size;
    public static ConfigEntry<int> FontSize;
    public static ConfigEntry<LabelColor> ColorPreset;
    public static ConfigEntry<Color> CustomColor;
    public static ConfigEntry<float> MaxDistance;
    public static ConfigEntry<bool> RequireLineOfSight;
    public static ConfigEntry<bool> ShowWhenEmpty;
    public static ConfigEntry<float> UpdateInterval;

    /// <summary>Shown or hidden with the toggle key (the Enabled setting stays as it is).</summary>
    static bool shown = true;

    public static bool Active => Enabled.Value && shown && !Compat.OverheadText;

    public static void Bind(ConfigFile config)
    {
        Enabled = config.Bind("Labels", "Enabled", false,
            "Portal names float above the portals. Off while the Teleporter Overhead Text mod is installed");
        ToggleKey = config.Bind("Labels", "ToggleKey", new KeyboardShortcut(KeyCode.PageUp), "Shows or hides the names in game. None: no key");
        HeightOffset = config.Bind("Labels", "HeightOffset", 2.5f,
            new ConfigDescription("Height of a name above the portal, in meters (at least above a tall portal's top)", new AcceptableValueRange<float>(0.5f, 8f)));
        FontName = config.Bind("Labels", "Font", "Game", "Font of the names: Game (the game's font), Arial, or the name of a font installed on your computer");
        Size = config.Bind("Labels", "Size", 0.12f, new ConfigDescription("Size of the names in the world", new AcceptableValueRange<float>(0.03f, 0.5f)));
        FontSize = config.Bind("Labels", "FontSize", 64, new ConfigDescription("Resolution of the letters: higher is sharper", new AcceptableValueRange<int>(16, 128)));
        ColorPreset = config.Bind("Labels", "ColorPreset", LabelColor.SoftGold, "Color of the names. Custom: the Color setting. RGBCycle*: changing rainbow colors");
        CustomColor = config.Bind("Labels", "Color", new Color(1f, 184 / 255f, 56 / 255f, 1f),
            "The color when ColorPreset is Custom; its alpha (opacity) also applies to the RGBCycle presets");
        MaxDistance = config.Bind("Labels", "MaxDistance", 40f,
            new ConfigDescription("Names farther than this from you (in meters) are hidden. 0: any distance", new AcceptableValueRange<float>(0f, 200f)));
        RequireLineOfSight = config.Bind("Labels", "RequireLineOfSight", true, "Hide a name when a wall or other solid hides its portal");
        ShowWhenEmpty = config.Bind("Labels", "ShowWhenEmpty", false, "Unnamed portals show a placeholder name");
        UpdateInterval = config.Bind("Labels", "UpdateInterval", 0.5f,
            new ConfigDescription("How often (in seconds) a name is read again from its portal", new AcceptableValueRange<float>(0.1f, 5f)));
    }

    public static void Update()
    {
        if (!Enabled.Value || Compat.OverheadText || !ToggleDown() || Typing()) return;
        shown = !shown;
        Debug.Log($"[{Plugin.Name}] portal names {(shown ? "shown" : "hidden")}");
    }

    static bool ToggleDown()
    {
        var shortcut = ToggleKey.Value;
        if (shortcut.MainKey == KeyCode.None || !ZInput.GetKeyDown(shortcut.MainKey, logWarning: false)) return false;
        return shortcut.Modifiers.All(k => ZInput.GetKey(k, logWarning: false));
    }

    static bool Typing() => Console.IsVisible() || Chat.instance && Chat.instance.HasFocus() || TextInput.IsVisible();

    public static Color CurrentColor()
    {
        var custom = CustomColor.Value;
        switch (ColorPreset.Value)
        {
            case LabelColor.White: return Color.white;
            case LabelColor.SoftGold: return new Color(1f, 184 / 255f, 56 / 255f);
            case LabelColor.Red: return new Color(1f, 0.25f, 0.2f);
            case LabelColor.Green: return new Color(0.35f, 1f, 0.35f);
            case LabelColor.Blue: return new Color(0.35f, 0.55f, 1f);
            case LabelColor.Yellow: return new Color(1f, 0.95f, 0.3f);
            case LabelColor.Cyan: return new Color(0.3f, 1f, 1f);
            case LabelColor.Magenta: return new Color(1f, 0.35f, 1f);
            case LabelColor.Orange: return new Color(1f, 0.6f, 0.15f);
            case LabelColor.RGBCycleSlow: return Cycle(0.05f, custom.a);
            case LabelColor.RGBCycleMedium: return Cycle(0.15f, custom.a);
            case LabelColor.RGBCycleFast: return Cycle(0.4f, custom.a);
            default: return custom;
        }
    }

    static Color Cycle(float speed, float alpha)
    {
        var c = Color.HSVToRGB(Time.time * speed % 1f, 0.75f, 1f);
        c.a = alpha;
        return c;
    }

    static readonly Dictionary<string, Font> fonts = new();

    /// <summary>The font of the Font setting: the game's, a loaded or installed font by name, else Unity's built-in one.</summary>
    public static Font ResolveFont(string name)
    {
        if (fonts.TryGetValue(name ?? "", out var font) && font) return font;
        var loaded = Resources.FindObjectsOfTypeAll<Font>();
        if (string.IsNullOrEmpty(name) || name == "Game")
            font = loaded.Where(f => f.name.StartsWith("AveriaSerifLibre")).OrderByDescending(f => f.name.Contains("Bold")).FirstOrDefault();
        else if (name != "Arial")
        {
            font = loaded.FirstOrDefault(f => f.name == name);
            if (!font)
            {
                try { font = Font.CreateDynamicFontFromOSFont(name, FontSize.Value); }
                catch { font = null; }
            }
        }
        if (!font) font = Builtin();
        fonts[name ?? ""] = font;
        return font;
    }

    static Font Builtin()
    {
        foreach (var file in new[] { "LegacyRuntime.ttf", "Arial.ttf" })
        {
            try
            {
                var font = Resources.GetBuiltinResource<Font>(file);
                if (font) return font;
            }
            catch
            {
                // not in this Unity version
            }
        }
        return null;
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Awake))]
    static class Attach
    {
        static void Postfix(TeleportWorld __instance)
        {
            if (!Compat.OverheadText && !__instance.GetComponent<PortalLabel>()) __instance.gameObject.AddComponent<PortalLabel>();
        }
    }
}

/// <summary>The floating name of one portal: a text mesh with a dark outline, turned to the camera.</summary>
internal sealed class PortalLabel : MonoBehaviour
{
    static readonly Vector2[] OutlineOffsets = { new(-1, 0), new(1, 0), new(0, -1), new(0, 1) };
    static readonly RaycastHit[] hits = new RaycastHit[16];
    static int solidMask;
    const float AboveTop = 0.45f;

    TeleportWorld portal;
    Transform root;
    TextMesh front;
    readonly TextMesh[] outline = new TextMesh[4];
    readonly List<MeshRenderer> renderers = new();
    string text = "";
    float nextRead;
    string fontName;
    int fontSize;
    float size = -1f;
    float top = -1f;

    void Awake() => portal = GetComponent<TeleportWorld>();

    void OnDestroy()
    {
        if (root) Destroy(root.gameObject);
    }

    void Build()
    {
        root = new GameObject("ClientPortals Label").transform;
        root.SetParent(transform, false);
        for (int i = 0; i < outline.Length; i++) outline[i] = Mesh("Outline", new Color(0, 0, 0, 0.85f));
        front = Mesh("Text", Color.white);
        front.transform.localPosition = new Vector3(0, 0, -0.01f);
    }

    TextMesh Mesh(string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        var mesh = go.AddComponent<TextMesh>();
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        var r = go.GetComponent<MeshRenderer>();
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        renderers.Add(r);
        return mesh;
    }

    void LateUpdate()
    {
        if (!PortalLabels.Active || !portal || !portal.m_nview || !portal.m_nview.IsValid())
        {
            Show(false);
            return;
        }
        if (!root) Build();
        if (Time.time >= nextRead)
        {
            nextRead = Time.time + PortalLabels.UpdateInterval.Value;
            SetText(Destinations.DisplayName(portal.GetText(), PortalLabels.ShowWhenEmpty.Value ? Texts.T("unnamed") : ""));
        }
        var cam = Utils.GetMainCamera();
        var player = Player.m_localPlayer;
        float max = PortalLabels.MaxDistance.Value;
        if (text.Length == 0 || !cam || !player || max > 0 && (player.transform.position - transform.position).sqrMagnitude > max * max ||
            PortalLabels.RequireLineOfSight.Value && !InSight(cam))
        {
            Show(false);
            return;
        }
        Appearance();
        var look = root.position - cam.transform.position;
        if (look.sqrMagnitude > 0.0001f) root.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        Show(true);
    }

    void SetText(string value)
    {
        if (value == text) return;
        text = value;
        front.text = value;
        foreach (var o in outline) o.text = value;
    }

    void Appearance()
    {
        root.localPosition = Vector3.up * Mathf.Max(PortalLabels.HeightOffset.Value, Top() + AboveTop);
        string name = PortalLabels.FontName.Value;
        int resolution = PortalLabels.FontSize.Value;
        if (name != fontName || resolution != fontSize)
        {
            fontName = name;
            fontSize = resolution;
            var font = PortalLabels.ResolveFont(name);
            foreach (var mesh in new[] { outline[0], outline[1], outline[2], outline[3], front })
            {
                mesh.fontSize = resolution;
                if (!font) continue;
                mesh.font = font;
                mesh.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
        }
        float s = PortalLabels.Size.Value;
        if (!Mathf.Approximately(s, size))
        {
            size = s;
            front.characterSize = s;
            for (int i = 0; i < outline.Length; i++)
            {
                outline[i].characterSize = s;
                // the outline's width follows the text's size
                outline[i].transform.localPosition = (Vector3)(OutlineOffsets[i] * s / 3f);
            }
        }
        front.color = PortalLabels.CurrentColor();
    }

    /// <summary>The top of the portal's own meshes (above its origin), measured once.</summary>
    float Top()
    {
        if (top >= 0f) return top;
        float y = 0f;
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (!r || r.transform.IsChildOf(root)) continue;
            var b = r.bounds;
            y = Mathf.Max(y, transform.InverseTransformPoint(new Vector3(b.center.x, b.max.y, b.center.z)).y);
        }
        if (y > 0.5f) top = y;
        return y;
    }

    /// <summary>Nothing solid between the camera and the portal (the player excepted).</summary>
    bool InSight(Camera cam)
    {
        if (solidMask == 0) solidMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle", "viewblock");
        var from = cam.transform.position;
        var to = transform.position + Vector3.up * 1.2f - from;
        float distance = to.magnitude;
        if (distance < 0.05f) return true;
        int n = Physics.RaycastNonAlloc(from, to / distance, hits, distance, solidMask, QueryTriggerInteraction.Ignore);
        Collider nearest = null;
        float best = float.MaxValue;
        var player = Player.m_localPlayer;
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            if (!c || player && c.transform.IsChildOf(player.transform) || hits[i].distance >= best) continue;
            best = hits[i].distance;
            nearest = c;
        }
        return !nearest || nearest.transform.IsChildOf(transform);
    }

    void Show(bool on)
    {
        foreach (var r in renderers)
            if (r && r.enabled != on) r.enabled = on;
    }
}
