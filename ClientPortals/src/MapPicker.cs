using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ClientPortals.Texts;

namespace ClientPortals;

/// <summary>
/// The map view: the game's large map with every known portal drawn on it as a badge among the pins (or alone, with
/// <see cref="Plugin.OnlyPortals"/>). Mouse: hover a badge, click to travel, right-click to star it, middle-click to
/// ignore it. Gamepad: move the map under the crosshair (or jump between portals with the bumpers), south button to
/// travel, north to star, right stick press to ignore, west for only portals, triggers zoom; the map's own pin actions
/// are off meanwhile (they share those buttons). Keyboard and mouse: a search box (it has the keyboard when the view
/// opens) keeps only the matching portals on the map and zooms to them. Tab, the map key or the List view button go
/// back to the list, with the search; Esc cancels.
/// </summary>
internal static class MapPicker
{
    const float BadgeSize = 34f, HoverRadius = 26f, CloseDistance = 6f;

    sealed class Marker
    {
        public RectTransform Rt;
        public Image Fill, Ring, Icon;
        public PortalEntry Entry; // null: the portal in use
        public bool Favorite, Visible;
        // the name written under the badge (ShowNames), its size measured when it changes
        public TMP_Text Name;
        public Vector2 NameSize;
    }

    static TeleportWorld source;
    static int openFrame, closedFrame = -100;
    static int cycleIndex = -1;
    static bool backToList;
    static readonly List<Marker> markers = new();
    static int hovered = -1;

    // the overlay (lives in the map's own UI, rebuilt if the map is)
    static Minimap builtFor;
    static RectTransform badgeRoot, namesRoot, label, bar;
    static TMP_Text labelText, hintText, keysText;
    static Button listButton;
    static Toggle onlyPortalsToggle, namesToggle;
    /// <summary>The game's map pin name, the look of the portals' names (null: a plain text).</summary>
    static TMP_Text nameTemplate;
    /// <summary>Where names went this frame (and the badges): a name that would overlap one stays off.</summary>
    static readonly List<Rect> taken = new();
    /// <summary>The map's pin filters while <see cref="Plugin.OnlyPortals"/> hides them (null: not hidden).</summary>
    static bool[] savedPinFilter;
    static bool keysReleased, focusSearchWhenFree;
    static GUIFramework.GuiInputField searchField;
    static string search = "";
    static int matches;
    /// <summary>The zoom the map opened with, back when the search is cleared.</summary>
    static float openZoom;
    static Sprite portalIcon;
    // the map's own gamepad hints and its "visible to others" hotkey (Y), set aside while picking
    static readonly List<GameObject> hiddenHints = new();
    static UIGamePad publicHotkey;
    /// <summary>How many of the nearest portals the map zooms out to show when it opens.</summary>
    const int FitNearest = 8;
    const float MaxFitZoom = 0.25f;
    /// <summary>Zooming to the search's matches shows at least this far around them (meters).</summary>
    const float MinSearchHalf = 500f;
    const float BarWidth = 1000f, BarHeight = 150f, BarSide = 24f, ListButtonWidth = 250f;
    /// <summary>The space between a switch's label and its box, between the switches, and between the title and them.</summary>
    const float ToggleGap = 8f, TogglesApart = 40f, TitleGap = 24f;

    public static bool IsOpen => source;

    public static bool SearchFocused => IsOpen && searchField && searchField.isFocused;
    static bool searchWasFocused;
    /// <summary>Frames left to put the caret after the text: the box sets its own caret while it activates.</summary>
    static int caretToEnd;

    /// <summary>
    /// With keyboard and mouse the view has the keys, like the list: typing goes to the search box, Tab to the list, Esc
    /// cancels; the game (the map key, Esc) and other mods' hotkeys don't see them. Dragging and the pin clicks are the
    /// map's mouse, which stays; the wheel zoom is done here. Also the two frames after closing, so the key that closed
    /// the view doesn't reach the game too (Enter that travels to the one match would open the chat).
    /// </summary>
    public static bool BlocksInput => (IsOpen || Time.frameCount - closedFrame <= 2) && !Pad.Active;

    /// <summary>Letters typed while the search box doesn't have the keyboard: they go on in it.</summary>
    static readonly System.Text.StringBuilder typed = new();

    /// <summary>Opens the map view for <paramref name="portal"/>, searching <paramref name="filter"/> (the list's search).</summary>
    public static void Open(TeleportWorld portal, string filter = "")
    {
        var map = Minimap.instance;
        if (!map) return;
        if (builtFor != map || !badgeRoot) Build(map);
        source = portal;
        openFrame = Time.frameCount;
        cycleIndex = -1;
        backToList = false;
        search = filter ?? "";
        if (searchField) searchField.SetTextWithoutNotify(search);
        map.SetMapMode(Minimap.MapMode.Large);
        // the button that opened the map must not drop a pin when it is released
        map.inputDelay = 0.5f;
        map.m_mapOffset = Vector3.zero;
        ZoomToFit(map, portal);
        openZoom = map.LargeZoom;
        if (search.Trim().Length > 0) ZoomToMatches(map);
        Show(true);
        SetAsideMapControls(map, true);
        HidePins(map, Plugin.OnlyPortals.Value);
        if (onlyPortalsToggle) onlyPortalsToggle.SetIsOnWithoutNotify(Plugin.OnlyPortals.Value);
        if (namesToggle) namesToggle.SetIsOnWithoutNotify(Plugin.ShowNames.Value);
        // the search box takes the keyboard once the keys that opened the view are up (Tab would type in it)
        keysReleased = false;
        focusSearchWhenFree = !Pad.Active && searchField;
        typed.Clear();
        ListenToTyping(true);
    }

    static void ListenToTyping(bool on)
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;
        keyboard.onTextInput -= OnTextInput;
        if (on) keyboard.onTextInput += OnTextInput;
    }

    static void OnTextInput(char c)
    {
        if (IsOpen && keysReleased && !SearchFocused && !char.IsControl(c)) typed.Append(c);
    }

    public static void Close()
    {
        if (!IsOpen) return;
        source = null;
        closedFrame = Time.frameCount;
        focusSearchWhenFree = false;
        ListenToTyping(false);
        Show(false);
        SetAsideMapControls(Minimap.instance, false);
        HidePins(Minimap.instance, false);
        if (Minimap.instance && Minimap.instance.m_mode == Minimap.MapMode.Large) Minimap.instance.SetMapMode(Minimap.MapMode.Small);
    }

    static void SwitchToList()
    {
        var from = source;
        string filter = search;
        Close();
        Journal.LastViewMap = false;
        ListWindow.Open(from, filter, allTab: true);
    }

    static void Show(bool on)
    {
        if (badgeRoot) badgeRoot.gameObject.SetActive(on);
        if (namesRoot) namesRoot.gameObject.SetActive(on);
        if (bar) bar.gameObject.SetActive(on);
        if (label) label.gameObject.SetActive(false);
        if (!on && searchField) searchField.DeactivateInputField();
        hovered = -1;
    }

    static void Build(Minimap map)
    {
        builtFor = map;
        markers.Clear();
        portalIcon = Ui.PortalIcon();

        // the names under the badges, both above the map's pins
        namesRoot = Ui.Stretch(Ui.Rect("ClientPortals Names", map.m_pinRootLarge));
        namesRoot.SetAsLastSibling();
        badgeRoot = Ui.Stretch(Ui.Rect("ClientPortals Badges", map.m_pinRootLarge));
        badgeRoot.SetAsLastSibling();
        nameTemplate = map.m_pinNamePrefab ? map.m_pinNamePrefab.GetComponentInChildren<TMP_Text>(true) : null;

        var overlay = map.m_largeRoot.transform;
        label = Ui.Rect("ClientPortals Label", overlay);
        label.pivot = new Vector2(0.5f, 0);
        var labelBg = label.gameObject.AddComponent<Image>();
        labelBg.sprite = Ui.Sprite("button_small") ?? Ui.Sprite("text_field");
        labelBg.type = Image.Type.Sliced;
        labelBg.color = labelBg.sprite ? new Color(0.15f, 0.12f, 0.09f, 0.95f) : new Color(0, 0, 0, 0.75f);
        labelBg.raycastTarget = false;
        labelText = Ui.Body(Ui.Stretch(Ui.Rect("Text", label), 12, 4, 12, 4), 20, Color.white, TextAlignmentOptions.Center);
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        labelText.overflowMode = TextOverflowModes.Overflow;
        var fitter = label.gameObject.AddComponent<HorizontalLayoutGroup>();
        fitter.padding = new RectOffset(14, 14, 6, 6);
        fitter.childControlWidth = fitter.childControlHeight = true;
        label.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        label.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // the hint bar at the top: what to do; the search (keyboard and mouse) and the way back to the list; the keys
        bar = Ui.Rect("ClientPortals Bar", overlay);
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1);
        bar.pivot = new Vector2(0.5f, 1);
        bar.anchoredPosition = new Vector2(0, -70);
        bar.sizeDelta = new Vector2(BarWidth, BarHeight);
        Ui.Panel(bar, mirrorBottom: true);
        // the title on the left of the top row, the switches on its right
        hintText = Ui.Body(Ui.Place(Ui.Rect("Hint", bar), BarSide, 10, BarWidth - 2 * BarSide, 40), 22, Ui.Orange, TextAlignmentOptions.MidlineLeft);
        hintText.textWrappingMode = TextWrappingModes.NoWrap;
        Ui.Fit(hintText, 14);
        keysText = Ui.Body(Ui.Place(Ui.Rect("Keys", bar), BarSide, 107, BarWidth - 2 * BarSide, 36), 18, Color.white, TextAlignmentOptions.Center);
        keysText.textWrappingMode = TextWrappingModes.NoWrap;
        Ui.Fit(keysText, 12);
        listButton = Ui.Button(bar, "ListView", SwitchToList);
        Ui.Place((RectTransform)listButton.transform, BarWidth - BarSide - ListButtonWidth, 58, ListButtonWidth, 44);
        searchField = Ui.InputField(bar);
        if (searchField)
        {
            Ui.Place((RectTransform)searchField.transform, BarSide, 58, BarWidth - 2 * BarSide - ListButtonWidth - 16, 44);
            searchField.onValueChanged.AddListener(OnSearch);
        }
        // the switches in the bar (other mods put theirs next to the map's own in its corner)
        onlyPortalsToggle = BarToggle(map, "OnlyPortals", on =>
        {
            Plugin.OnlyPortals.Value = on;
            if (IsOpen) HidePins(Minimap.instance, on);
        });
        namesToggle = BarToggle(map, "PortalNames", on => Plugin.ShowNames.Value = on);
        toggleLabels = null;
        Show(false);
    }

    static Marker NewMarker()
    {
        var rt = Ui.Rect("Portal", badgeRoot);
        // the map's MapPointToLocalGuiPos measures from its bottom-left corner, like its own pins
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        var m = new Marker { Rt = rt, Fill = rt.gameObject.AddComponent<Image>() };
        m.Fill.sprite = Ui.Badge;
        m.Fill.raycastTarget = false;
        m.Ring = Ui.Stretch(Ui.Rect("Ring", rt)).gameObject.AddComponent<Image>();
        m.Ring.raycastTarget = false;
        m.Icon = Ui.Stretch(Ui.Rect("Icon", rt), 5, 5, 5, 5).gameObject.AddComponent<Image>();
        m.Icon.sprite = portalIcon;
        m.Icon.preserveAspect = true;
        m.Icon.raycastTarget = false;
        if (!portalIcon) m.Icon.enabled = false;
        m.Name = NewName();
        return m;
    }

    /// <summary>A portal's name under its badge, written like the map's pin names.</summary>
    static TMP_Text NewName()
    {
        TMP_Text text;
        if (nameTemplate)
        {
            text = Object.Instantiate(nameTemplate.gameObject, namesRoot, false).GetComponent<TMP_Text>();
            text.name = "Name";
            foreach (var localize in text.GetComponentsInChildren<Localize>(true)) Object.DestroyImmediate(localize);
        }
        else text = Ui.Body(Ui.Rect("Name", namesRoot), 16, Color.white, TextAlignmentOptions.Top);
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 1);
        text.alignment = TextAlignmentOptions.Top;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.text = "";
        text.gameObject.SetActive(false);
        return text;
    }

    /// <summary>
    /// A switch for the bar: the map's own "visible to other players" one (its label left of the box), its mark a plain
    /// check mark instead of the game's player-versus-player icon. <see cref="LayoutToggles"/> places it.
    /// </summary>
    static Toggle BarToggle(Minimap map, string name, UnityEngine.Events.UnityAction<bool> changed)
    {
        if (!map.m_publicPosition) return null;
        var toggle = Ui.Clone(map.m_publicPosition.gameObject, bar, name).GetComponent<Toggle>();
        var rt = (RectTransform)toggle.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        if (toggle.graphic is Image mark)
        {
            mark.sprite = Ui.Check;
            mark.color = Ui.Orange;
            mark.preserveAspect = true;
        }
        var text = toggle.GetComponentInChildren<TMP_Text>(true);
        if (text)
        {
            var trt = text.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0, 0.5f);
            trt.pivot = new Vector2(1, 0.5f);
            trt.anchoredPosition = new Vector2(-ToggleGap, 0);
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = false;
            text.fontSize = 18;
        }
        toggle.onValueChanged.AddListener(changed);
        return toggle;
    }

    /// <summary>The labels the switches were laid out for (null: not yet).</summary>
    static string toggleLabels;

    /// <summary>
    /// The switches side by side on the right of the bar's top row, with their gamepad buttons; the title gets the rest of
    /// the row. Laid out for the labels with the gamepad's buttons either way: switching between gamepad and mouse moves
    /// nothing.
    /// </summary>
    static void LayoutToggles(bool pad)
    {
        string onlyPad = Pad.Glyph("JoyButtonX") + " " + T("only_portals"), namesPad = Pad.Glyph(NamesButton) + " " + T("portal_names");
        string only = pad ? onlyPad : T("only_portals"), names = pad ? namesPad : T("portal_names");
        if (toggleLabels == only + "\n" + names) return;
        toggleLabels = only + "\n" + names;
        var toggles = new[] { onlyPortalsToggle, namesToggle };
        var labels = new[] { only, names };
        var padLabels = new[] { onlyPad, namesPad };
        float x = BarWidth - BarSide;
        for (int i = toggles.Length - 1; i >= 0; i--)
        {
            var text = toggles[i] ? toggles[i].GetComponentInChildren<TMP_Text>(true) : null;
            if (!text) continue;
            text.text = labels[i];
            float w = Mathf.Max(text.GetPreferredValues(labels[i]).x, text.GetPreferredValues(padLabels[i]).x);
            text.rectTransform.sizeDelta = new Vector2(w, 30f);
            // the box (its background is a bit wider than the switch), the gap, the label
            var rt = (RectTransform)toggles[i].transform;
            rt.anchoredPosition = new Vector2(x - BoxHalf(toggles[i]), -30f);
            x -= BoxHalf(toggles[i]) + ToggleHalf(toggles[i]) + ToggleGap + w + TogglesApart;
        }
        var title = hintText.rectTransform;
        title.sizeDelta = new Vector2(Mathf.Max(100f, x + TogglesApart - TitleGap - BarSide), title.sizeDelta.y);
    }

    static float ToggleHalf(Toggle toggle) => ((RectTransform)toggle.transform).rect.width / 2;

    static float BoxHalf(Toggle toggle) =>
        toggle.targetGraphic ? Mathf.Max(toggle.targetGraphic.rectTransform.rect.width / 2, ToggleHalf(toggle)) : ToggleHalf(toggle);

    /// <summary>The gamepad button that switches <see cref="Plugin.ShowNames"/> (the left stick's press: the map doesn't use it).</summary>
    const string NamesButton = "JoyLStick";

    public static void Update()
    {
        if (!IsOpen) return;
        var player = Player.m_localPlayer;
        var map = Minimap.instance;
        if (!map || map.m_mode != Minimap.MapMode.Large)
        {
            // closed by the player: the map key goes back to the list, Esc and the gamepad's back button cancel
            var from = source;
            source = null;
            Show(false);
            SetAsideMapControls(map, false);
            HidePins(map, false);
            if (backToList && from)
            {
                Journal.LastViewMap = false;
                ListWindow.Open(from, search, allTab: true);
            }
            return;
        }
        if (!player || player.IsDead() || player.IsTeleporting() || !source.m_nview || !source.m_nview.IsValid() ||
            Vector3.Distance(player.transform.position, source.transform.position) > CloseDistance)
        {
            Close();
            return;
        }

        Layout(map);
        Render();
        if (!keysReleased && !Ui.AnyKeyHeld) keysReleased = true;
        if (Time.frameCount <= openFrame + 1) return;
        if (focusSearchWhenFree && keysReleased)
        {
            focusSearchWhenFree = false;
            FocusSearch();
        }
        // typing on the map goes on in the search box (a click on the map took the keyboard from it)
        if (typed.Length > 0)
        {
            if (!SearchFocused && searchField)
            {
                searchField.text = search + typed;
                FocusSearch();
            }
            typed.Clear();
        }
        if (caretToEnd > 0 && SearchFocused)
        {
            caretToEnd--;
            // by the string: MoveTextEnd counts the drawn characters, stale right after the text changed
            searchField.stringPosition = searchField.text.Length;
        }

        if (Pad.Active)
        {
            // the map's own gamepad input is off while picking (see TakeInput): zoom like it does. Despite the names,
            // its "JoyMapZoomIn" (LT) shows more of the map and "JoyMapZoomOut" (RT) less
            if (ZInput.GetButton("JoyMapZoomIn")) map.LargeZoom += map.LargeZoom * Time.deltaTime * 2f;
            if (ZInput.GetButton("JoyMapZoomOut")) map.LargeZoom -= map.LargeZoom * Time.deltaTime * 2f;
        }
        if (BlocksInput)
        {
            // the map takes no keys meanwhile (BlocksInput): Tab goes to the list, Esc cancels, the wheel zooms as the
            // map does it
            if (ZInput.GetKeyDown(KeyCode.Tab))
            {
                SwitchToList();
                return;
            }
            // Esc empties the search first, then cancels
            if (ZInput.GetKeyDown(KeyCode.Escape))
            {
                if (search.Length == 0 || !searchField) Close();
                else
                {
                    searchField.text = "";
                    FocusSearch();
                }
                return;
            }
            map.LargeZoom -= Mathf.Clamp(ZInput.GetMouseScrollWheel(), -0.05f, 0.05f) * map.LargeZoom * 2f;
        }
        // Enter: one match travels there, several open the list with them (the first one selected), none leaves the box.
        // The box may let go of the keyboard on Enter before this sees the key: a frame later still counts.
        bool enter = ZInput.GetKeyDown(KeyCode.Return) || ZInput.GetKeyDown(KeyCode.KeypadEnter);
        bool typing = SearchFocused || searchWasFocused && enter;
        searchWasFocused = SearchFocused;
        if (typing)
        {
            if (enter) searchField.DeactivateInputField();
            if (enter && search.Trim().Length > 0)
            {
                var found = PortalWorld.DestinationsFrom(PortalWorld.PositionOf(source), ListTab.All, search, out _);
                var from = source;
                if (found.Count == 1)
                {
                    Close();
                    Travel.Go(from, found[0]);
                }
                else if (found.Count > 1)
                {
                    string filter = search;
                    Close();
                    ListWindow.Open(from, filter, focusList: true, allTab: true);
                }
            }
            return;
        }
        if (Pad.Down("JoyRBumper")) Cycle(map, player, +1);
        if (Pad.Down("JoyLBumper")) Cycle(map, player, -1);
        if (Pad.Down("JoyButtonX") && onlyPortalsToggle) onlyPortalsToggle.isOn = !onlyPortalsToggle.isOn;
        if (Pad.Down(NamesButton) && namesToggle) namesToggle.isOn = !namesToggle.isOn;
        if (hovered < 0) return;
        if (Pad.Down("JoyButtonA")) TravelToHovered();
        else if (Pad.Down("JoyButtonY")) Journal.ToggleFavorite(markers[hovered].Entry.Key);
        else if (Pad.Down("JoyRStick")) ListWindow.Ignore(markers[hovered].Entry);
    }

    /// <summary>Hides the map's pins (its pin filters all off) or shows them as they were.</summary>
    static void HidePins(Minimap map, bool hide)
    {
        if (!map || map.m_visibleIconTypes == null) return;
        if (hide && savedPinFilter == null)
        {
            savedPinFilter = (bool[])map.m_visibleIconTypes.Clone();
            for (int i = 0; i < map.m_visibleIconTypes.Length; i++) map.m_visibleIconTypes[i] = false;
        }
        else if (!hide && savedPinFilter != null)
        {
            if (savedPinFilter.Length == map.m_visibleIconTypes.Length) savedPinFilter.CopyTo(map.m_visibleIconTypes, 0);
            savedPinFilter = null;
        }
        else return;
        map.m_pinUpdateRequired = true;
    }

    /// <summary>Puts a badge on every known portal (and the one in use) and finds the one pointed at.</summary>
    static void Layout(Minimap map)
    {
        var fromPos = PortalWorld.PositionOf(source);
        var list = PortalWorld.DestinationsFrom(fromPos, ListTab.All, search, out _);
        matches = list.Count;
        while (markers.Count < list.Count + 1) markers.Add(NewMarker());

        var canvas = map.m_mapImageLarge.canvas;
        var cam = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 pointer = Pad.Active ? new Vector2(Screen.width / 2f, Screen.height / 2f) : (Vector2)ZInput.pointerPosition;
        float scale = canvas ? canvas.scaleFactor : 1f;
        float best = HoverRadius * scale;
        hovered = -1;

        for (int i = 0; i < markers.Count; i++)
        {
            var m = markers[i];
            Vector3 pos;
            if (i == 0) { m.Entry = null; pos = fromPos; }
            else if (i <= list.Count) { m.Entry = list[i - 1]; pos = m.Entry.Position; }
            else { m.Visible = false; m.Rt.gameObject.SetActive(false); continue; }

            m.Visible = map.IsPointVisible(pos, map.m_mapImageLarge);
            m.Rt.gameObject.SetActive(m.Visible);
            if (!m.Visible) continue;
            map.WorldToMapPoint(pos, out float mx, out float my);
            m.Rt.anchoredPosition = map.MapPointToLocalGuiPos(mx, my, map.m_mapImageLarge);
            m.Favorite = m.Entry != null && Journal.IsFavorite(m.Entry.Key);
            if (m.Entry == null) continue;
            float d = Vector2.Distance(pointer, RectTransformUtility.WorldToScreenPoint(cam, m.Rt.position));
            if (d < best)
            {
                best = d;
                hovered = i;
            }
        }
    }

    static void Render()
    {
        for (int i = 0; i < markers.Count; i++)
        {
            var m = markers[i];
            if (!m.Visible) continue;
            bool here = m.Entry == null, isHover = i == hovered;
            float size = BadgeSize * (here ? 1.15f : isHover ? 1.35f : 1f);
            m.Rt.sizeDelta = new Vector2(size, size);
            m.Fill.color = here ? new Color(0.20f, 0.45f, 0.18f) : isHover ? new Color(0.16f, 0.32f, 0.55f) : m.Favorite ? new Color(0.75f, 0.47f, 0.10f) : new Color(0.16f, 0.12f, 0.08f, 0.95f);
            m.Ring.sprite = isHover ? Ui.RingThick : Ui.Ring;
            m.Ring.color = here ? new Color(0.55f, 0.90f, 0.45f) : isHover ? new Color(0.55f, 0.80f, 1f) : m.Favorite ? new Color(1f, 0.85f, 0.52f) : new Color(0.83f, 0.64f, 0.31f);
            if (isHover) m.Rt.SetAsLastSibling();
        }

        PlaceNames();
        bool pad = Pad.Active;
        if (hovered >= 0)
        {
            var m = markers[hovered];
            var player = Player.m_localPlayer;
            int meters = player ? Mathf.RoundToInt(Vector3.Distance(player.transform.position, m.Entry.Position)) : 0;
            string distance = meters >= 1000 ? $"{meters / 1000f:0.0} km" : $"{meters} m";
            labelText.text = $"<color=#FFA13C>{Destinations.DisplayName(m.Entry.Name, T("unnamed"))}</color>  <color=#B8B8B8>{distance}</color>" + (m.Favorite ? "  <color=#FFD98A>★</color>" : "");
            label.gameObject.SetActive(true);
            PlaceLabel(m);
            label.SetAsLastSibling();
        }
        else if (label.gameObject.activeSelf) label.gameObject.SetActive(false);

        Ui.SetHeader(hintText, search.Trim().Length > 0 && matches == 0 ? T("empty_search") : T("map_hint"));
        // the same panel with a gamepad (a touch on a streaming device is the mouse: its search box is there)
        if (searchField && searchField.placeholder is TMP_Text placeholder && placeholder.text != T("search")) placeholder.text = T("search");
        LayoutToggles(pad);
        keysText.text = pad
            ? $"{Pad.Hint(Pad.Glyph("JoyButtonA"), T("travel"))}    {Pad.Hint(Pad.Glyph("JoyButtonY"), T("favorite"))}    {Pad.Hint(Pad.Glyph("JoyRStick"), T("ignore"))}    {Pad.Hint(Pad.Glyph("JoyLBumper") + Pad.Glyph("JoyRBumper"), T("next"))}    {Pad.Hint(Pad.Glyph("JoyButtonB"), T("cancel"))}"
            : $"{Pad.Hint(T("click"), T("travel"))}    {Pad.Hint(T("right_click"), T("favorite"))}    {Pad.Hint(T("middle_click"), T("ignore"))}    {Pad.Hint("Esc", T("cancel"))}";
        string listLabel = pad ? Pad.Glyph("JoyMap") + " " + T("list_view") : T("list_view") + " (Tab)";
        Ui.SetLabel(listButton, listLabel, new[] { listLabel });
        var listRect = (RectTransform)listButton.transform;
        float listX = !searchField ? (BarWidth - ListButtonWidth) / 2 : BarWidth - BarSide - ListButtonWidth;
        if (!Mathf.Approximately(listRect.anchoredPosition.x, listX)) listRect.anchoredPosition = new Vector2(listX, listRect.anchoredPosition.y);
        bar.SetAsLastSibling();
    }

    /// <summary>
    /// The hover label above the badge, or below it where the bar (or the map's top) would cover it; within the map left
    /// to right.
    /// </summary>
    static void PlaceLabel(Marker m)
    {
        var map = WorldRect(Minimap.instance.m_mapImageLarge.rectTransform);
        var barRect = WorldRect(bar);
        float offset = m.Rt.rect.height * m.Rt.lossyScale.y * 0.75f;
        float height = label.rect.height * label.lossyScale.y, width = label.rect.width * label.lossyScale.x;
        var at = m.Rt.position;
        float x = Mathf.Clamp(at.x, map.xMin + width / 2, Mathf.Max(map.xMin + width / 2, map.xMax - width / 2));
        var above = new Rect(x - width / 2, at.y + offset, width, height);
        var under = new Rect(x - width / 2, at.y - offset - height, width, height);
        bool below = (above.Overlaps(barRect) || above.yMax > map.yMax) && !under.Overlaps(barRect);
        label.pivot = new Vector2(0.5f, below ? 1f : 0f);
        label.position = new Vector3(x, at.y + (below ? -offset : offset), at.z);
    }

    static Rect WorldRect(RectTransform rt)
    {
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    /// <summary><paramref name="rt"/>'s rect in the badges' coordinates (from the badge root's bottom-left corner).</summary>
    static Rect BadgeRect(RectTransform rt)
    {
        var world = WorldRect(rt);
        Vector2 min = badgeRoot.InverseTransformPoint(world.min), max = badgeRoot.InverseTransformPoint(world.max);
        var origin = badgeRoot.rect.min;
        return Rect.MinMaxRect(min.x - origin.x, min.y - origin.y, max.x - origin.x, max.y - origin.y);
    }

    static readonly List<int> nameOrder = new();

    /// <summary>
    /// With <see cref="Plugin.ShowNames"/>, each portal's name under its badge: favorites first, then the nearest; a name
    /// that would cover another name, a badge or the bar, or leave the map, stays off (hovering shows it).
    /// </summary>
    static void PlaceNames()
    {
        bool show = Plugin.ShowNames.Value;
        taken.Clear();
        nameOrder.Clear();
        Rect bounds = default;
        if (show)
        {
            bounds = BadgeRect(Minimap.instance.m_mapImageLarge.rectTransform);
            taken.Add(BadgeRect(bar));
        }
        var player = Player.m_localPlayer;
        var here = player ? player.transform.position : Vector3.zero;
        for (int i = 0; i < markers.Count; i++)
        {
            var m = markers[i];
            if (show && m.Visible) taken.Add(new Rect(m.Rt.anchoredPosition - m.Rt.sizeDelta / 2, m.Rt.sizeDelta));
            if (show && m.Visible && m.Entry != null && i != hovered) nameOrder.Add(i);
            else if (m.Name.gameObject.activeSelf) m.Name.gameObject.SetActive(false);
        }
        if (!show) return;
        nameOrder.Sort((a, b) =>
        {
            var ma = markers[a];
            var mb = markers[b];
            if (ma.Favorite != mb.Favorite) return ma.Favorite ? -1 : 1;
            return Utils.DistanceXZ(here, ma.Entry.Position).CompareTo(Utils.DistanceXZ(here, mb.Entry.Position));
        });
        foreach (int i in nameOrder)
        {
            var m = markers[i];
            string name = Destinations.DisplayName(m.Entry.Name, T("unnamed"));
            if (m.Name.text != name)
            {
                m.Name.text = name;
                m.NameSize = m.Name.GetPreferredValues(name);
                m.Name.rectTransform.sizeDelta = m.NameSize;
            }
            var top = m.Rt.anchoredPosition - new Vector2(0, m.Rt.sizeDelta.y / 2 + 1);
            var rect = new Rect(top.x - m.NameSize.x / 2, top.y - m.NameSize.y, m.NameSize.x, m.NameSize.y);
            bool fits = bounds.Contains(rect.min) && bounds.Contains(rect.max);
            // its own badge ends just above it
            for (int k = 0; fits && k < taken.Count; k++)
                if (rect.Overlaps(taken[k])) fits = false;
            if (fits)
            {
                taken.Add(rect);
                m.Name.rectTransform.anchoredPosition = top;
                m.Name.color = m.Favorite ? new Color(1f, 0.85f, 0.54f) : Color.white;
            }
            if (m.Name.gameObject.activeSelf != fits) m.Name.gameObject.SetActive(fits);
        }
    }

    /// <summary>
    /// Zooms out (never in) so the nearest portals fit in the free part of the map (<see cref="FreeArea"/>), the player
    /// in the middle.
    /// </summary>
    static void ZoomToFit(Minimap map, TeleportWorld portal)
    {
        var player = Player.m_localPlayer;
        if (!player) return;
        var here = player.transform.position;
        var nearest = PortalWorld.DestinationsFrom(PortalWorld.PositionOf(portal), ListTab.All, "", out _);
        nearest.Sort((a, b) => Utils.DistanceXZ(here, a.Position).CompareTo(Utils.DistanceXZ(here, b.Position)));
        var free = FreeArea(map);
        var center = map.m_mapImageLarge.rectTransform.rect.center;
        // the meters one unit of the map image must show for each portal to be inside the free area
        float perUnit = 0f;
        for (int i = 0; i < nearest.Count && i < FitNearest; i++)
        {
            var d = nearest[i].Position - here;
            float dx = Mathf.Abs(d.x) + Padding, dz = Mathf.Abs(d.z) + Padding;
            perUnit = Mathf.Max(perUnit,
                dx / Mathf.Max(1f, d.x >= 0 ? free.xMax - center.x : center.x - free.xMin),
                dz / Mathf.Max(1f, d.z >= 0 ? free.yMax - center.y : center.y - free.yMin));
        }
        if (perUnit <= 0f) return;
        // at most ~4 km top to bottom: farther portals are a bumper press (or a zoom) away
        float zoom = Mathf.Min(ZoomOf(map, perUnit), MaxFitZoom);
        if (zoom > map.LargeZoom) map.LargeZoom = zoom;
    }

    /// <summary>Meters around a portal that stay on the map when zooming to it.</summary>
    const float Padding = 60f;

    /// <summary>
    /// The part of the large map no panel covers, in the map image's units: below the hint bar, left of the pin buttons,
    /// above the switches at the bottom.
    /// </summary>
    static Rect FreeArea(Minimap map)
    {
        var image = map.m_mapImageLarge.rectTransform;
        var r = image.rect;
        float top = r.yMax - r.height * 0.04f;
        if (bar)
        {
            var corners = new Vector3[4];
            bar.GetWorldCorners(corners);
            top = Mathf.Min(top, image.InverseTransformPoint(corners[0]).y - r.height * 0.04f);
        }
        return Rect.MinMaxRect(r.xMin + r.width * 0.04f, r.yMin + r.height * 0.16f, r.xMax - r.width * 0.1f, top);
    }

    /// <summary>The map's zoom (LargeZoom: the share of the map texture's height on screen) that shows <paramref name="perUnit"/> meters in a unit of the map image.</summary>
    static float ZoomOf(Minimap map, float perUnit) =>
        perUnit * map.m_mapImageLarge.rectTransform.rect.height / (map.m_textureSize * map.m_pixelSize);

    static float PerUnit(Minimap map) =>
        map.LargeZoom * map.m_textureSize * map.m_pixelSize / map.m_mapImageLarge.rectTransform.rect.height;

    static void FocusSearch()
    {
        if (!searchField || !searchField.gameObject.activeInHierarchy) return;
        searchField.ActivateInputField();
        caretToEnd = 4;
    }

    static void OnSearch(string text)
    {
        search = text ?? "";
        cycleIndex = -1;
        var map = Minimap.instance;
        var player = Player.m_localPlayer;
        if (!IsOpen || !map || !player) return;
        if (search.Trim().Length == 0)
        {
            map.m_mapOffset = Vector3.zero;
            map.LargeZoom = openZoom;
            return;
        }
        ZoomToMatches(map);
    }

    /// <summary>Centers and zooms (in or out) on the portals matching the search.</summary>
    static void ZoomToMatches(Minimap map)
    {
        var player = Player.m_localPlayer;
        var found = PortalWorld.DestinationsFrom(PortalWorld.PositionOf(source), ListTab.All, search, out _);
        if (found.Count == 0 || !player) return;
        Vector3 min = found[0].Position, max = found[0].Position;
        foreach (var e in found)
        {
            min = Vector3.Min(min, e.Position);
            max = Vector3.Max(max, e.Position);
        }
        // the matches fill the free part of the map (not under the bar or the buttons), at least MinSearchHalf around
        var free = FreeArea(map);
        map.LargeZoom = ZoomOf(map, Mathf.Max((max.x - min.x + 2 * Padding) / free.width, (max.z - min.z + 2 * Padding) / free.height,
            2 * MinSearchHalf / free.height));
        // centered in the free part: the map's middle moves off them by as much as that part is off the image's middle
        var shift = (free.center - map.m_mapImageLarge.rectTransform.rect.center) * PerUnit(map);
        var middle = (min + max) / 2f;
        map.m_mapOffset = new Vector3(middle.x - shift.x - player.transform.position.x, 0, middle.z - shift.y - player.transform.position.z);
    }

    /// <summary>While picking, the map's gamepad hints and its "visible to others" hotkey (it is Y, our favorite) are set aside.</summary>
    static void SetAsideMapControls(Minimap map, bool aside)
    {
        if (!map) return;
        if (aside)
        {
            hiddenHints.Clear();
            foreach (var hint in map.m_hints)
                if (hint && hint.activeSelf)
                {
                    hint.SetActive(false);
                    hiddenHints.Add(hint);
                }
            publicHotkey = map.m_publicPosition ? map.m_publicPosition.GetComponentInChildren<UIGamePad>(true) : null;
            if (publicHotkey)
            {
                publicHotkey.enabled = false;
                if (publicHotkey.m_hint && publicHotkey.m_hint.activeSelf)
                {
                    publicHotkey.m_hint.SetActive(false);
                    hiddenHints.Add(publicHotkey.m_hint);
                }
            }
        }
        else
        {
            foreach (var hint in hiddenHints)
                if (hint) hint.SetActive(true);
            hiddenHints.Clear();
            if (publicHotkey) publicHotkey.enabled = true;
            publicHotkey = null;
        }
    }

    /// <summary>Centers the map on the next (or previous) portal, nearest to the player first.</summary>
    static void Cycle(Minimap map, Player player, int step)
    {
        var list = PortalWorld.DestinationsFrom(PortalWorld.PositionOf(source), ListTab.All, search, out _);
        if (list.Count == 0) return;
        list.Sort((a, b) => Vector3.Distance(player.transform.position, a.Position).CompareTo(Vector3.Distance(player.transform.position, b.Position)));
        cycleIndex = ((cycleIndex + step) % list.Count + list.Count) % list.Count;
        var offset = list[cycleIndex].Position - player.transform.position;
        offset.y = 0;
        map.m_mapOffset = offset;
    }

    static void TravelToHovered()
    {
        if (hovered < 0) return;
        var target = markers[hovered].Entry;
        var from = source;
        Close();
        Travel.Go(from, target);
    }

    /// <summary>Clicking a portal badge travels instead of the map's own click (pins).</summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    static class LeftClick
    {
        static bool Prefix()
        {
            if (!IsOpen || hovered < 0 || Pad.Active) return true;
            TravelToHovered();
            return false;
        }
    }

    /// <summary>Right-clicking a portal badge stars it instead of removing a pin.</summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.RemovePinUnderPointer))]
    static class RightClick
    {
        static bool Prefix()
        {
            if (!IsOpen || hovered < 0 || Pad.Active) return true;
            Journal.ToggleFavorite(markers[hovered].Entry.Key);
            return false;
        }
    }

    /// <summary>Middle-clicking a portal badge ignores it (it leaves the map) instead of pinging.</summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapMiddleClick))]
    static class MiddleClick
    {
        static bool Prefix()
        {
            if (!IsOpen || hovered < 0 || Pad.Active) return true;
            ListWindow.Ignore(markers[hovered].Entry);
            return false;
        }
    }

    /// <summary>
    /// With a gamepad, picking owns the face buttons and bumpers, which the map uses for its pins (A adds one, X pings,
    /// RB deletes the one under the crosshair...): its button input is off while picking. Moving the map stays.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateMap))]
    static class TakeInput
    {
        static void Prefix(ref bool takeInput)
        {
            if (IsOpen && Pad.Active) takeInput = false;
        }
    }

    /// <summary>
    /// The map key closes the map as usual; in the map view it goes back to the list instead of cancelling. The gamepad's
    /// back button (the game closes the map with it) empties the search first.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapMode))]
    static class MapKey
    {
        static bool Prefix(Minimap.MapMode mode)
        {
            if (!IsOpen || mode == Minimap.MapMode.Large) return true;
            if (ZInput.GetButtonDown("JoyButtonB") && search.Length > 0 && searchField)
            {
                searchField.text = "";
                return false;
            }
            if (ZInput.GetButtonDown("Map") || ZInput.GetButtonDown("JoyMap")) backToList = true;
            return true;
        }
    }
}
