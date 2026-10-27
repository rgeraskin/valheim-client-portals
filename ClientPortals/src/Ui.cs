using System.Collections.Generic;
using System.Linq;
using TMPro;
using GUIFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClientPortals;

/// <summary>
/// Builds the mod's windows out of the game's own UI: its dialog panel, crafting-list rows, tabs, buttons, scrollbar,
/// text input and fonts are cloned or copied, so everything looks and sounds like the game.
/// </summary>
internal static class Ui
{
    public const int Layer = 5; // UI
    public static readonly Color Orange = new(1f, 0.631f, 0.235f, 1f);
    public static readonly Color Beige = new(0.853f, 0.725f, 0.533f, 1f);
    public static readonly Color Dim = new(0.66f, 0.66f, 0.66f, 1f);

    static Dictionary<string, Sprite> sprites;
    static Sprite star, starOutline, badge, ring, ringThick, noSign, cross, dots, check;

    /// <summary>The game's GUI canvas (the one holding the inventory), where the windows go so they scale with the game's UI.</summary>
    public static Transform CanvasRoot
    {
        get
        {
            if (!InventoryGui.instance) return null;
            var canvas = InventoryGui.instance.GetComponentInParent<Canvas>();
            return canvas ? canvas.rootCanvas.transform : null;
        }
    }

    public static Sprite Sprite(string name)
    {
        if (sprites == null || sprites.Count == 0)
        {
            sprites = new Dictionary<string, Sprite>();
            foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
                if (s && !sprites.ContainsKey(s.name)) sprites[s.name] = s;
        }
        return sprites.TryGetValue(name, out var sprite) && sprite ? sprite : null;
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)) { layer = Layer };
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    /// <summary>Places <paramref name="rt"/> by its top-left corner, <paramref name="x"/>/<paramref name="y"/> from the parent's top-left.</summary>
    public static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    public static RectTransform Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    public static void CopyImage(Image to, Image from)
    {
        to.sprite = from.sprite;
        to.type = from.type;
        to.material = from.material;
        to.color = from.color;
        to.pixelsPerUnitMultiplier = from.pixelsPerUnitMultiplier;
        to.fillCenter = from.fillCenter;
    }

    /// <summary>
    /// The wooden panel of the game's dialogs (the portal tag dialog's own background). Its bottom edge is torn: with
    /// <paramref name="mirrorBottom"/> the panel is two halves of its upper part, the lower one upside down, so the
    /// bottom gets the same framed edge as the top.
    /// </summary>
    public static Image Panel(RectTransform rt, bool mirrorBottom = false)
    {
        var dialog = TextInput.instance && TextInput.instance.m_panel ? DialogBackground(TextInput.instance.m_panel.transform) : null;
        var upper = mirrorBottom && dialog ? UpperHalf(dialog.sprite) : null;
        if (upper)
        {
            Image Half(string name, float from, float to, bool flip)
            {
                var half = Rect(name, rt);
                half.anchorMin = new Vector2(0, from);
                half.anchorMax = new Vector2(1, to);
                half.offsetMin = half.offsetMax = Vector2.zero;
                if (flip) half.localScale = new Vector3(1, -1, 1);
                var image = half.gameObject.AddComponent<Image>();
                CopyImage(image, dialog);
                image.sprite = upper;
                return image;
            }
            Half("Lower", 0, 0.5f, flip: true);
            return Half("Upper", 0.5f, 1, flip: false);
        }
        var image = rt.gameObject.AddComponent<Image>();
        if (dialog) CopyImage(image, dialog);
        else
        {
            image.sprite = Sprite("woodpanel_trophys") ?? Sprite("woodpanel_settings");
            image.type = Image.Type.Sliced;
            var lit = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "litpanel");
            if (lit) image.material = lit;
            if (!image.sprite) image.color = new Color(0.16f, 0.12f, 0.08f, 0.97f);
        }
        return image;
    }

    static readonly Dictionary<Sprite, Sprite> upperHalves = new();

    /// <summary>The upper half of a sliced sprite, sliced at its sides and top only (null for a tightly packed sprite).</summary>
    static Sprite UpperHalf(Sprite sprite)
    {
        if (!sprite || sprite.packed && sprite.packingMode == SpritePackingMode.Tight) return null;
        if (upperHalves.TryGetValue(sprite, out var half) && half) return half;
        var r = sprite.textureRect;
        var b = sprite.border; // left, bottom, right, top
        half = UnityEngine.Sprite.Create(sprite.texture, new Rect(r.x, r.y + r.height / 2, r.width, r.height / 2), new Vector2(0.5f, 0.5f),
            sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(b.x, 0, b.z, Mathf.Min(b.w, r.height / 2)));
        half.name = sprite.name + " (upper half)";
        half.hideFlags = HideFlags.HideAndDontSave;
        return upperHalves[sprite] = half;
    }

    /// <summary>The biggest sliced image of a dialog: its background.</summary>
    static Image DialogBackground(Transform dialog) =>
        dialog.GetComponentsInChildren<Image>(true)
            .Where(i => i.sprite && i.type == Image.Type.Sliced)
            .OrderByDescending(i => i.rectTransform.rect.width * i.rectTransform.rect.height)
            .FirstOrDefault();

    /// <summary>A darker inset for the list, like the game's sunken item slots.</summary>
    public static Image Inset(RectTransform rt)
    {
        var image = rt.gameObject.AddComponent<Image>();
        var sprite = Sprite("item_background_sunken") ?? Sprite("panel_interior_bkg_128");
        if (sprite)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(1, 1, 1, 0.9f);
        }
        else image.color = new Color(0, 0, 0, 0.45f);
        return image;
    }

    static TMP_Text TitleTemplate => TextInput.instance ? TextInput.instance.m_topic : null;
    static TMP_Text BodyTemplate => InventoryGui.instance ? InventoryGui.instance.m_recipeDecription : null;

    /// <summary>A text in the game's header style (the tag dialog's title).</summary>
    public static TMP_Text Title(RectTransform rt, float size)
    {
        var text = Text(rt, TitleTemplate ?? BodyTemplate, size, Orange, TextAlignmentOptions.Center);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    /// <summary>
    /// Sets a text in the header font when it can draw it (Latin, Greek, Cyrillic), else in the body font: the header
    /// font draws nothing for Chinese, Japanese, Korean... even where its fallbacks claim the characters.
    /// </summary>
    public static void SetHeader(TMP_Text text, string value)
    {
        if (!text || text.text == value) return;
        bool headerScript = true;
        foreach (char c in value)
            if (c > '\u052F' && !char.IsWhiteSpace(c) && !char.IsPunctuation(c)) headerScript = false;
        // the whole look of the template: font, material (outline) and style (the fallback fonts have no bold)
        var template = headerScript ? TitleTemplate ?? BodyTemplate : BodyTemplate ?? TitleTemplate;
        if (template && text.font != template.font)
        {
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
            text.fontStyle = template.fontStyle;
        }
        text.text = value;
        text.ForceMeshUpdate();
    }

    /// <summary>Shrinks a text to fit its box (languages with taller or longer lines).</summary>
    public static TMP_Text Fit(TMP_Text text, float min)
    {
        text.enableAutoSizing = true;
        text.fontSizeMax = text.fontSize;
        text.fontSizeMin = min;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    /// <summary>A text in the game's body style (item descriptions).</summary>
    public static TMP_Text Body(RectTransform rt, float size, Color color, TextAlignmentOptions align) => Text(rt, BodyTemplate ?? TitleTemplate, size, color, align);

    static TMP_Text Text(RectTransform rt, TMP_Text template, float size, Color color, TextAlignmentOptions align)
    {
        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (template)
        {
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
            text.fontStyle = template.fontStyle;
            text.spriteAsset = template.spriteAsset;
        }
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.richText = true;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        // not Ellipsis: a line taller than its box (the fallback fonts of Chinese, Japanese... are) would vanish whole
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    /// <summary>
    /// A clone of a game object (button, tab, list row...) for the mod's own use: its own click handling, no keyboard/gamepad
    /// navigation, and none of the game's gamepad hotkeys (a cloned hotkey would also fire for the original's key).
    /// </summary>
    public static GameObject Clone(GameObject source, Transform parent, string name)
    {
        var go = Object.Instantiate(source, parent, false);
        go.name = name;
        go.SetActive(true);
        foreach (var pad in go.GetComponentsInChildren<UIGamePad>(true))
        {
            if (pad.m_hint) Object.DestroyImmediate(pad.m_hint);
            Object.DestroyImmediate(pad);
        }
        foreach (var selectable in go.GetComponentsInChildren<Selectable>(true))
        {
            selectable.navigation = new Navigation { mode = Navigation.Mode.None };
            if (selectable is Button button) button.onClick = new Button.ButtonClickedEvent();
            if (selectable is Toggle toggle) toggle.onValueChanged = new Toggle.ToggleEvent();
        }
        // the game's texts translate themselves on a language change: these are the mod's
        foreach (var localize in go.GetComponentsInChildren<Localize>(true)) Object.DestroyImmediate(localize);
        return go;
    }

    /// <summary>A game button (the crafting panel's) with <paramref name="label"/>; falls back to a plain button.</summary>
    public static Button Button(Transform parent, string name, System.Action onClick)
    {
        Button button;
        var source = InventoryGui.instance ? InventoryGui.instance.m_craftButton : null;
        if (source) button = Clone(source.gameObject, parent, name).GetComponent<Button>();
        else
        {
            var rt = Rect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = Sprite("button");
            image.type = Image.Type.Sliced;
            button = rt.gameObject.AddComponent<Button>();
            Body(Stretch(Rect("Text", rt)), 18, Color.white, TextAlignmentOptions.Center);
        }
        button.onClick.AddListener(() => onClick());
        return button;
    }

    /// <summary>A tab like the crafting panel's Craft/Upgrade tabs (the selected one is drawn by making it non-interactable, as the game does).</summary>
    public static Button Tab(Transform parent, string name, System.Action onClick)
    {
        var source = InventoryGui.instance ? InventoryGui.instance.m_tabCraft : null;
        if (!source) return Button(parent, name, onClick);
        var tab = Clone(source.gameObject, parent, name).GetComponent<Button>();
        tab.onClick.AddListener(() => onClick());
        return tab;
    }

    public static void SetLabel(Component control, string text)
    {
        var label = control.GetComponentInChildren<TMP_Text>(true);
        if (label && label.text != text) label.text = text;
    }

    // the game's buttons shrink each text to fit: a label that changes would change size
    static readonly Dictionary<TMP_Text, string> fittedFor = new();
    static readonly Dictionary<TMP_Text, float> largest = new();
    const float MinLabelSize = 10f;

    /// <summary>Sets a label at one font size: the largest at which every text of <paramref name="all"/> fits (with only <paramref name="text"/> in it: the size that fits it).</summary>
    public static void SetLabel(Component control, string text, string[] all)
    {
        var label = control.GetComponentInChildren<TMP_Text>(true);
        if (!label) return;
        string key = string.Join("\n", all);
        if (!fittedFor.TryGetValue(label, out var fitted) || fitted != key)
        {
            float size = float.MaxValue;
            foreach (var t in all) size = Mathf.Min(size, FittedSize(label, t));
            Freeze(label, size);
            fittedFor[label] = key;
        }
        if (label.text != text) label.text = text;
    }

    /// <summary>Sets the labels of a row of controls (tabs) at one font size: the largest at which all of them fit.</summary>
    public static void SetLabels(Component[] controls, string[] texts)
    {
        var labels = controls.Select(c => c.GetComponentInChildren<TMP_Text>(true)).ToArray();
        string key = string.Join("\n", texts);
        if (labels.Any(l => l && (!fittedFor.TryGetValue(l, out var fitted) || fitted != key)))
        {
            float size = float.MaxValue;
            for (int i = 0; i < labels.Length; i++)
                if (labels[i]) size = Mathf.Min(size, FittedSize(labels[i], texts[i]));
            foreach (var l in labels)
                if (l)
                {
                    Freeze(l, size);
                    fittedFor[l] = key;
                }
        }
        for (int i = 0; i < labels.Length; i++)
            if (labels[i] && labels[i].text != texts[i]) labels[i].text = texts[i];
    }

    /// <summary>The size the label's auto-sizing picks for <paramref name="text"/> (up to its own largest size).</summary>
    static float FittedSize(TMP_Text label, string text)
    {
        if (!largest.TryGetValue(label, out float max))
            largest[label] = max = label.enableAutoSizing ? label.fontSizeMax : label.fontSize;
        label.fontSizeMax = max;
        // small enough for the longest translation to fit: no text may run past its button
        label.fontSizeMin = Mathf.Min(MinLabelSize, max);
        label.enableAutoSizing = true;
        label.text = text;
        label.ForceMeshUpdate();
        return label.fontSize;
    }

    static void Freeze(TMP_Text label, float size)
    {
        label.enableAutoSizing = false;
        label.fontSize = size;
    }

    /// <summary>The crafting list's scrollbar.</summary>
    public static Scrollbar Scrollbar(Transform parent)
    {
        var source = InventoryGui.instance ? InventoryGui.instance.m_recipeListScroll : null;
        if (!source) return null;
        var bar = Clone(source.gameObject, parent, "Scrollbar").GetComponent<Scrollbar>();
        bar.onValueChanged = new Scrollbar.ScrollEvent();
        bar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
        return bar;
    }

    /// <summary>The text field of the game's text dialog.</summary>
    public static GuiInputField InputField(Transform parent)
    {
        var source = TextInput.instance ? TextInput.instance.m_inputField : null;
        if (!source) return null;
        var field = Clone(source.gameObject, parent, "Search").GetComponent<GuiInputField>();
        // the tag dialog's own handlers would rename the portal
        field.OnInputSubmit = new OnInputSubmitEvent();
        field.onValueChanged = new TMP_InputField.OnChangeEvent();
        field.onSubmit = new TMP_InputField.SubmitEvent();
        field.onEndEdit = new TMP_InputField.SubmitEvent();
        field.onDeselect = new TMP_InputField.SelectionEvent();
        field.restoreOriginalTextOnEscape = false;
        field.characterLimit = 32;
        field.lineType = TMP_InputField.LineType.SingleLine;
        // typing on the list starts the search with that letter: focusing the box must not select it for replacing
        field.onFocusSelectAll = false;
        field.text = "";
        AddClearButton(field);
        return field;
    }

    /// <summary>A × at the right end of a text field, shown while there is text: empties it and gives it the keyboard.</summary>
    static void AddClearButton(GuiInputField field)
    {
        var rt = Rect("Clear", field.transform);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(-8, 0);
        rt.sizeDelta = new Vector2(22, 22);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = Cross;
        image.color = Dim;
        var button = rt.gameObject.AddComponent<Button>();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var colors = button.colors;
        colors.highlightedColor = new Color(1.4f, 1.4f, 1.4f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() =>
        {
            field.text = "";
            field.ActivateInputField();
        });
        // the text stops short of it
        if (field.textViewport) field.textViewport.offsetMax -= new Vector2(28, 0);
        var shower = field.gameObject.AddComponent<ShowWhenText>();
        shower.Field = field;
        shower.Target = rt.gameObject;
    }

    /// <summary>Whether a keyboard key is held (the key that opened a window, a movement key still down...).</summary>
    public static bool AnyKeyHeld => UnityEngine.InputSystem.Keyboard.current is { } keyboard && keyboard.anyKey.isPressed;

    /// <summary>One row of the crafting list (icon, name, the "selected" frame).</summary>
    public static GameObject ListRow(Transform parent) =>
        InventoryGui.instance && InventoryGui.instance.m_recipeElementPrefab
            ? Clone(InventoryGui.instance.m_recipeElementPrefab, parent, "Portal")
            : null;

    public static void Deselect()
    {
        if (EventSystem.current && EventSystem.current.currentSelectedGameObject) EventSystem.current.SetSelectedGameObject(null);
    }

    // procedural shapes the game has no sprite for (white, tinted where used)
    public static Sprite Star => star ? star : star = Shape(64, StarDistance, 0);
    public static Sprite StarOutline => starOutline ? starOutline : starOutline = Shape(64, StarDistance, 4);
    public static Sprite Badge => badge ? badge : badge = Shape(48, RoundedSquare, 0);
    public static Sprite Ring => ring ? ring : ring = Shape(48, RoundedSquare, 3);
    public static Sprite RingThick => ringThick ? ringThick : ringThick = Shape(48, RoundedSquare, 5);
    public static Sprite NoSign => noSign ? noSign : noSign = Shape(48, NoSignDistance, 0);
    public static Sprite Cross => cross ? cross : cross = Shape(48, CrossDistance, 0);
    public static Sprite Dots => dots ? dots : dots = Shape(48, DotsDistance, 0);
    public static Sprite Check => check ? check : check = Shape(48, CheckDistance, 0);

    /// <summary>A check mark (✓).</summary>
    static float CheckDistance(Vector2 p, float half)
    {
        const float stroke = 3.6f;
        float s = half - 8;
        Vector2 a = new(-0.75f * s, 0.05f * s), b = new(-0.2f * s, -0.55f * s), c = new(0.8f * s, 0.6f * s);
        float Segment(Vector2 from, Vector2 to)
        {
            var d = to - from;
            float t = Mathf.Clamp01(Vector2.Dot(p - from, d) / d.sqrMagnitude);
            return (p - from - d * t).magnitude - stroke;
        }
        return Mathf.Min(Segment(a, b), Segment(b, c));
    }

    /// <summary>An ×.</summary>
    static float CrossDistance(Vector2 p, float half)
    {
        const float stroke = 3.2f;
        float r = half - 8;
        float Bar(Vector2 d)
        {
            float t = Mathf.Clamp(Vector2.Dot(p, d), -r, r);
            return (p - d * t).magnitude - stroke;
        }
        return Mathf.Min(Bar(new Vector2(1, 1).normalized), Bar(new Vector2(1, -1).normalized));
    }

    /// <summary>Three dots in a row (…: more, here rename).</summary>
    static float DotsDistance(Vector2 p, float half)
    {
        const float radius = 5f;
        float gap = (half - 4 - radius) / 1.2f;
        float d = float.MaxValue;
        for (int i = -1; i <= 1; i++) d = Mathf.Min(d, (p - new Vector2(i * gap, 0)).magnitude - radius);
        return d;
    }

    /// <summary>A circle crossed by a diagonal bar (the "not this one" sign).</summary>
    static float NoSignDistance(Vector2 p, float half)
    {
        const float stroke = 2.6f;
        float r = half - 4 - stroke;
        float circle = Mathf.Abs(p.magnitude - r) - stroke;
        // the bar from top-left to bottom-right, inside the circle
        var d = new Vector2(1, -1).normalized;
        float t = Mathf.Clamp(Vector2.Dot(p, d), -r, r);
        float bar = (p - d * t).magnitude - stroke;
        return Mathf.Min(circle, bar);
    }

    static float RoundedSquare(Vector2 p, float half)
    {
        const float r = 10f;
        float px = Mathf.Abs(p.x) - (half - 2 - r), py = Mathf.Abs(p.y) - (half - 2 - r);
        return new Vector2(Mathf.Max(px, 0), Mathf.Max(py, 0)).magnitude + Mathf.Min(Mathf.Max(px, py), 0) - r;
    }

    /// <summary>Signed distance to a five-pointed star.</summary>
    static float StarDistance(Vector2 p, float half)
    {
        float outer = half - 3, inner = outer * 0.45f;
        var pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float a = Mathf.PI / 2 + i * Mathf.PI / 5;
            float rad = i % 2 == 0 ? outer : inner;
            pts[i] = new Vector2(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad);
        }
        float d = float.MaxValue;
        bool inside = false;
        for (int i = 0, j = 9; i < 10; j = i++)
        {
            Vector2 a = pts[j], b = pts[i], e = b - a, w = p - a;
            float t = Mathf.Clamp01(Vector2.Dot(w, e) / e.sqrMagnitude);
            d = Mathf.Min(d, (w - e * t).magnitude);
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
        }
        return inside ? -d : d;
    }

    static Sprite Shape(int size, System.Func<Vector2, float, float> distance, float ring)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        float half = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = distance(new Vector2(x + 0.5f - half, y + 0.5f - half), half);
            float a = Mathf.Clamp01(0.5f - d);
            if (ring > 0) a *= Mathf.Clamp01(d + ring + 0.5f);
            t.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        t.Apply();
        var sprite = UnityEngine.Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    /// <summary>The build menu icon of the wooden portal.</summary>
    public static Sprite PortalIcon()
    {
        var prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab("portal_wood") : null;
        var piece = prefab ? prefab.GetComponent<Piece>() : null;
        return piece ? piece.m_icon : null;
    }
}

/// <summary>Tells a window which row the mouse is over, and which one it middle-clicks.</summary>
internal sealed class RowHover : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public int Index;
    public System.Action<int> OnEnter, OnMiddleClick;
    public void OnPointerEnter(PointerEventData eventData) => OnEnter?.Invoke(Index);

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle) OnMiddleClick?.Invoke(Index);
    }
}

/// <summary>Shows <see cref="Target"/> while <see cref="Field"/> has text.</summary>
internal sealed class ShowWhenText : MonoBehaviour
{
    public TMP_InputField Field;
    public GameObject Target;

    void LateUpdate()
    {
        bool show = Field && !string.IsNullOrEmpty(Field.text);
        if (Target && Target.activeSelf != show) Target.SetActive(show);
    }
}
