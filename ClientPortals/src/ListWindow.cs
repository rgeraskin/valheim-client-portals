using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ClientPortals.Texts;

namespace ClientPortals;

/// <summary>
/// The destination list, built from the game's own UI pieces: search, All/Favorites/Ignored tabs, sort by name or
/// distance (nearest or farthest first), stars, ignore marks, renaming, and a switch to the map view. Mouse, keyboard
/// (arrows, Enter, Left/Right for the tabs, Insert to star, Delete to ignore, F2 to rename, Tab for the map view, Esc;
/// typing goes to the search box) and gamepad (D-pad or left stick to pick, bumpers for the tabs, south button to
/// travel, north to star, west to sort, right stick press to ignore, the map button to switch view, east to close).
/// </summary>
internal static class ListWindow
{
    const float W = 620f, H = 776f, Side = 30f;
    const float ListTop = 196f, ListHeight = 408f, Inner = 6f;
    const int PageRows = 8;
    /// <summary>Walking (or being pushed) this far from the portal closes the list.</summary>
    const float CloseDistance = 6f;

    static TeleportWorld source;
    static int openFrame, closedFrame = -100;
    static string search = "";
    static ListTab tab;
    static List<PortalEntry> rows = new();
    static int hidden;
    static int selected = -1;
    static PortalPos? selectedKey;
    static bool scrollToSelected, mouseSelects = true;
    static Vector3 lastMouse;
    /// <summary>The keys held when the list opened (walking in, the travel key) are let go: until then nothing types.</summary>
    static bool keysReleased;
    static bool focusSearchWhenFree;
    /// <summary>Frames left to put the caret after the text: the box sets its own caret while it activates.</summary>
    static int caretToEnd;
    /// <summary>Letters typed while the list (not the search box) has the keyboard: they start the search.</summary>
    static readonly StringBuilder typed = new();
    /// <summary>The game's text dialog renames a portal: the list waits behind it.</summary>
    static bool renaming;

    // the window
    static GameObject root;
    static TMP_Text title, from, footer, hints, empty;
    static GUIFramework.GuiInputField searchField;
    static Button tabAll, tabFavorites, tabIgnored, sortButton, mapButton, closeButton;
    static RectTransform viewport, content;
    static ScrollRect scroll;
    static readonly List<Row> pool = new();
    static float rowHeight = 34f, rowPitch = 38f;
    static Sprite portalIcon;

    sealed class Row
    {
        public GameObject Go;
        public TMP_Text Name, Distance;
        public Image Icon, Star, Ignore, Rename;
        public GameObject Selected;
        // the row's own hover look (the mouse resting on a row), off while the gamepad picks
        public Button Button;
        public ColorBlock Colors;
        public SpriteState Sprites;
        public bool HoverOff;
    }

    public static bool IsOpen => source && root && (root.activeSelf || renaming);

    /// <summary>Also the two frames after closing, so the key that closed the list doesn't reach the game too.</summary>
    public static bool BlocksInput => IsOpen || Time.frameCount - closedFrame <= 2;

    static bool SearchFocused => searchField && searchField.isFocused;

    /// <summary>
    /// Opens the list for <paramref name="portal"/>, filtered by <paramref name="filter"/> (the map view's search). The
    /// search box gets the keyboard, or with <paramref name="focusList"/> (several matches of the map's search) the first
    /// match is selected instead. From the map view (<paramref name="allTab"/>) the All tab shows, as the map does.
    /// </summary>
    public static void Open(TeleportWorld portal, string filter = "", bool focusList = false, bool allTab = false)
    {
        if (!root && !Build()) return;
        source = portal;
        if (allTab) tab = ListTab.All;
        openFrame = Time.frameCount;
        renaming = false;
        search = filter ?? "";
        if (searchField) searchField.SetTextWithoutNotify(search);
        selected = 0;
        selectedKey = null;
        content.anchoredPosition = Vector2.zero;
        mouseSelects = !Pad.Active;
        lastMouse = ZInput.pointerPosition;
        keysReleased = false;
        typed.Clear();
        ListenToTyping(true);
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        Refresh();
        Render();
        if (focusList) mouseSelects = false; // the pointer resting over a row must not take the selection
        // once the keys that opened the list are up: a held W (walking in) would type wwww
        focusSearchWhenFree = !focusList && !Pad.Active && searchField;
    }

    public static void Close()
    {
        if (!source && !(root && root.activeSelf)) return;
        source = null;
        closedFrame = Time.frameCount;
        renaming = false;
        focusSearchWhenFree = false;
        ListenToTyping(false);
        if (searchField) searchField.DeactivateInputField();
        if (root) root.SetActive(false);
        Ui.Deselect();
    }

    static bool Build()
    {
        var canvas = Ui.CanvasRoot;
        if (!canvas) return false;
        portalIcon = Ui.PortalIcon();
        pool.Clear(); // rows of a previous window went with the game's UI (back to the main menu)

        var r = Ui.Stretch(Ui.Rect("ClientPortals List", canvas));
        root = r.gameObject;
        // catches clicks around the window, like the game's dialogs
        r.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);

        var panel = Ui.Rect("Panel", r);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(W, H);
        Ui.Panel(panel, mirrorBottom: true);

        title = Ui.Title(Ui.Place(Ui.Rect("Title", panel), Side, 16, W - 2 * Side, 46), 36);
        from = Ui.Body(Ui.Place(Ui.Rect("From", panel), Side, 62, W - 2 * Side, 28), 19, Ui.Beige, TextAlignmentOptions.Center);

        searchField = Ui.InputField(panel);
        if (searchField)
        {
            Ui.Place((RectTransform)searchField.transform, Side, 98, W - 2 * Side, 40);
            searchField.onValueChanged.AddListener(OnSearch);
        }

        float tabWidth = (W - 2 * Side - 2 * 6) / 3;
        tabAll = Ui.Tab(panel, "All", () => SetTab(ListTab.All));
        Ui.Place((RectTransform)tabAll.transform, Side, 148, tabWidth, 42);
        tabFavorites = Ui.Tab(panel, "Favorites", () => SetTab(ListTab.Favorites));
        Ui.Place((RectTransform)tabFavorites.transform, Side + tabWidth + 6, 148, tabWidth, 42);
        tabIgnored = Ui.Tab(panel, "Ignored", () => SetTab(ListTab.Ignored));
        Ui.Place((RectTransform)tabIgnored.transform, Side + 2 * (tabWidth + 6), 148, tabWidth, 42);

        var inset = Ui.Place(Ui.Rect("List", panel), Side, ListTop, W - 2 * Side, ListHeight);
        Ui.Inset(inset);
        viewport = Ui.Stretch(Ui.Rect("Viewport", inset), Inner, Inner, Inner + 16, Inner);
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0); // so the wheel scrolls between rows too
        content = Ui.Rect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        scroll = inset.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        // the wheel scrolls by rows (HandleInput): the UI's own wheel steps are a few pixels
        scroll.scrollSensitivity = 0f;
        var bar = Ui.Scrollbar(inset);
        if (bar)
        {
            var br = (RectTransform)bar.transform;
            br.anchorMin = new Vector2(1, 0);
            br.anchorMax = new Vector2(1, 1);
            br.pivot = new Vector2(1, 0.5f);
            br.offsetMin = new Vector2(-14, Inner);
            br.offsetMax = new Vector2(-4, -Inner);
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
        empty = Ui.Body(Ui.Stretch(Ui.Rect("Empty", inset), 30, 30, 30, 30), 19, Ui.Dim, TextAlignmentOptions.Center);

        footer = Ui.Fit(Ui.Body(Ui.Place(Ui.Rect("Footer", panel), Side, ListTop + ListHeight + 4, W - 2 * Side, 48), 16, Ui.Dim, TextAlignmentOptions.Center), 11);
        hints = Ui.Body(Ui.Place(Ui.Rect("Hints", panel), Side, ListTop + ListHeight + 54, W - 2 * Side, 30), 18, Color.white, TextAlignmentOptions.Center);
        hints.textWrappingMode = TextWrappingModes.NoWrap;
        Ui.Fit(hints, 12);

        float buttonWidth = (W - 2 * Side - 2 * 10) / 3;
        // as far from the bottom edge as the list is from the sides
        float buttonsY = H - Side - 4 - 44;
        sortButton = Ui.Button(panel, "Sort", ToggleSort);
        Ui.Place((RectTransform)sortButton.transform, Side, buttonsY, buttonWidth, 44);
        mapButton = Ui.Button(panel, "MapView", SwitchToMap);
        Ui.Place((RectTransform)mapButton.transform, Side + buttonWidth + 10, buttonsY, buttonWidth, 44);
        closeButton = Ui.Button(panel, "Close", Close);
        Ui.Place((RectTransform)closeButton.transform, Side + 2 * (buttonWidth + 10), buttonsY, buttonWidth, 44);

        // the crafting list's row height, or a bit taller
        var prefab = InventoryGui.instance ? InventoryGui.instance.m_recipeElementPrefab : null;
        if (prefab) rowHeight = Mathf.Max(34f, ((RectTransform)prefab.transform).rect.height);
        rowPitch = rowHeight + 4f;

        root.SetActive(false);
        return true;
    }

    static Row NewRow(int index)
    {
        var go = Ui.ListRow(content);
        Row row = new() { Go = go };
        RectTransform rt;
        if (go)
        {
            rt = (RectTransform)go.transform;
            row.Icon = go.transform.Find("icon")?.GetComponent<Image>();
            row.Name = go.transform.Find("name")?.GetComponent<TMP_Text>();
            row.Selected = go.transform.Find("selected")?.gameObject;
            foreach (var extra in new[] { "Durability", "QualityLevel" })
            {
                var t = go.transform.Find(extra);
                if (t) t.gameObject.SetActive(false);
            }
        }
        else
        {
            rt = Ui.Rect("Portal", content);
            go = row.Go = rt.gameObject;
            go.AddComponent<Image>().color = new Color(0, 0, 0, 0.25f);
            go.AddComponent<Button>();
        }
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, rowHeight);
        rt.anchoredPosition = new Vector2(0, -index * rowPitch);

        if (row.Icon)
        {
            var ir = row.Icon.rectTransform;
            ir.anchorMin = ir.anchorMax = new Vector2(0, 0.5f);
            ir.pivot = new Vector2(0, 0.5f);
            ir.anchoredPosition = new Vector2(6, 0);
            ir.sizeDelta = new Vector2(rowHeight - 6, rowHeight - 6);
            row.Icon.color = Color.white;
        }
        if (!row.Name) row.Name = Ui.Body(Ui.Rect("name", rt), 19, Color.white, TextAlignmentOptions.Left);
        var nr = row.Name.rectTransform;
        nr.anchorMin = Vector2.zero;
        nr.anchorMax = Vector2.one;
        nr.offsetMin = new Vector2(rowHeight + 8, 0);
        nr.offsetMax = new Vector2(-220, 0);
        row.Name.alignment = TextAlignmentOptions.MidlineLeft;
        row.Name.textWrappingMode = TextWrappingModes.NoWrap;
        row.Name.overflowMode = TextOverflowModes.Ellipsis;

        var dr = Ui.Rect("distance", rt);
        dr.anchorMin = new Vector2(1, 0);
        dr.anchorMax = new Vector2(1, 1);
        dr.pivot = new Vector2(1, 0.5f);
        dr.offsetMin = new Vector2(-210, 0);
        dr.offsetMax = new Vector2(-104, 0);
        row.Distance = Ui.Body(dr, 16, Ui.Dim, TextAlignmentOptions.MidlineRight);
        row.Distance.textWrappingMode = TextWrappingModes.NoWrap;

        var sr = Ui.Rect("star", rt);
        sr.anchorMin = sr.anchorMax = new Vector2(1, 0.5f);
        sr.pivot = new Vector2(1, 0.5f);
        sr.anchoredPosition = new Vector2(-10, 0);
        sr.sizeDelta = new Vector2(24, 24);
        row.Star = sr.gameObject.AddComponent<Image>();
        var star = sr.gameObject.AddComponent<Button>();
        star.navigation = new Navigation { mode = Navigation.Mode.None };
        star.onClick.AddListener(() => ToggleFavorite(index));

        var ir2 = Ui.Rect("ignore", rt);
        ir2.anchorMin = ir2.anchorMax = new Vector2(1, 0.5f);
        ir2.pivot = new Vector2(1, 0.5f);
        ir2.anchoredPosition = new Vector2(-42, 0);
        ir2.sizeDelta = new Vector2(22, 22);
        row.Ignore = ir2.gameObject.AddComponent<Image>();
        row.Ignore.sprite = Ui.NoSign;
        var ignore = ir2.gameObject.AddComponent<Button>();
        ignore.navigation = new Navigation { mode = Navigation.Mode.None };
        ignore.onClick.AddListener(() => ToggleIgnored(index));

        var rr = Ui.Rect("rename", rt);
        rr.anchorMin = rr.anchorMax = new Vector2(1, 0.5f);
        rr.pivot = new Vector2(1, 0.5f);
        rr.anchoredPosition = new Vector2(-72, 0);
        rr.sizeDelta = new Vector2(24, 24);
        row.Rename = rr.gameObject.AddComponent<Image>();
        row.Rename.sprite = Ui.Dots;
        var rename = rr.gameObject.AddComponent<Button>();
        rename.navigation = new Navigation { mode = Navigation.Mode.None };
        rename.onClick.AddListener(() => Rename(index));

        row.Button = go.GetComponent<Button>();
        row.Colors = row.Button.colors;
        row.Sprites = row.Button.spriteState;
        row.Button.onClick.AddListener(() => TravelTo(index));
        var hover = go.AddComponent<RowHover>();
        hover.Index = index;
        hover.OnEnter = i =>
        {
            if (mouseSelects) Select(i, scrollIntoView: false);
        };
        hover.OnMiddleClick = ToggleIgnored;
        return row;
    }

    static void OnSearch(string text)
    {
        search = text ?? "";
        selected = 0;
        selectedKey = null;
        content.anchoredPosition = Vector2.zero;
        Refresh();
    }

    static void Refresh()
    {
        rows = PortalWorld.DestinationsFrom(PortalWorld.PositionOf(source), tab, search, out hidden);
        if (selectedKey is { } key)
        {
            int i = rows.FindIndex(r => r.Key == key);
            if (i >= 0) selected = i;
        }
        selected = rows.Count == 0 ? -1 : Mathf.Clamp(selected, 0, rows.Count - 1);
        selectedKey = selected >= 0 ? rows[selected].Key : null;
    }

    static void Select(int index, bool scrollIntoView)
    {
        selected = Destinations.Step(index, 0, rows.Count);
        selectedKey = selected >= 0 ? rows[selected].Key : null;
        scrollToSelected |= scrollIntoView;
    }

    static void SetTab(ListTab to)
    {
        if (tab == to) return;
        tab = to;
        selected = 0;
        selectedKey = null;
        content.anchoredPosition = Vector2.zero;
        Refresh();
    }

    /// <summary>The next (or previous) tab, around.</summary>
    static void StepTab(int step) => SetTab((ListTab)(((int)tab + step + 3) % 3));

    /// <summary>The next sort order, from the top of the list again.</summary>
    static void ToggleSort()
    {
        Plugin.SortBy.Value = Destinations.Next(Plugin.SortBy.Value);
        selected = 0;
        selectedKey = null;
        content.anchoredPosition = Vector2.zero;
        Refresh();
    }

    /// <summary>Ignores a portal (it leaves the list for the Ignored tab), or stops ignoring it there.</summary>
    static void ToggleIgnored(int index)
    {
        if (index < 0 || index >= rows.Count) return;
        Ignore(rows[index]);
        Refresh();
        scrollToSelected = true;
    }

    /// <summary>Ignores a portal or stops ignoring it, and tells where it went (also from the map view).</summary>
    public static void Ignore(PortalEntry e)
    {
        if (!Journal.ToggleIgnored(e)) return;
        Plugin.Message(Destinations.IsUnnamed(e) ? T("ignored_unnamed") : T("ignored", Destinations.DisplayName(e.Name, T("unnamed"))),
            MessageHud.MessageType.TopLeft);
    }

    /// <summary>Renames a portal in the game's text dialog; the list waits behind it.</summary>
    static void Rename(int index)
    {
        if (index < 0 || index >= rows.Count || !TextInput.instance) return;
        Select(index, scrollIntoView: true);
        if (SearchFocused) searchField.DeactivateInputField();
        renaming = true;
        root.SetActive(false);
        PairRename.OpenFor(rows[index]);
    }


    static void ToggleFavorite(int index)
    {
        if (index < 0 || index >= rows.Count) return;
        Journal.ToggleFavorite(rows[index].Key);
        Refresh();
        scrollToSelected = true;
    }

    static void TravelTo(int index)
    {
        if (index < 0 || index >= rows.Count || !source) return;
        var target = rows[index];
        var from = source;
        Close();
        Travel.Go(from, target);
    }

    static bool MapAvailable => !Game.m_noMap && Minimap.instance;

    static void SwitchToMap()
    {
        if (!MapAvailable || !source) return;
        var from = source;
        string filter = search;
        Close();
        Journal.LastViewMap = true;
        MapPicker.Open(from, filter);
    }

    static void FocusSearch()
    {
        if (!searchField) return;
        searchField.ActivateInputField();
        caretToEnd = 4;
    }

    /// <summary>The letters typed come from the keyboard's text events, the layout's own characters.</summary>
    static void ListenToTyping(bool on)
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;
        keyboard.onTextInput -= OnTextInput;
        if (on) keyboard.onTextInput += OnTextInput;
    }

    static void OnTextInput(char c)
    {
        if (IsOpen && !renaming && keysReleased && !SearchFocused && !char.IsControl(c)) typed.Append(c);
    }

    public static void Update()
    {
        if (!source) return;
        var player = Player.m_localPlayer;
        if (!root || !player || player.IsDead() || player.IsTeleporting() || !source.m_nview || !source.m_nview.IsValid() ||
            Vector3.Distance(player.transform.position, source.transform.position) > CloseDistance)
        {
            Close();
            return;
        }
        if (renaming)
        {
            if (TextInput.instance && TextInput.instance.m_panel && TextInput.instance.m_panel.gameObject.activeSelf) return;
            // back from the dialog: the Enter or Esc that closed it must not act on the list too
            renaming = false;
            openFrame = Time.frameCount;
            root.SetActive(true);
            scrollToSelected = true;
        }
        Refresh();
        HandleInput();
        if (source) Render();
    }

    static void HandleInput()
    {
        if (!keysReleased && !Ui.AnyKeyHeld) keysReleased = true;
        // the press that opened the list must not act on it too
        if (Time.frameCount <= openFrame + 1) return;
        if (focusSearchWhenFree && keysReleased)
        {
            focusSearchWhenFree = false;
            FocusSearch();
        }
        bool typing = SearchFocused;
        if (caretToEnd > 0 && typing)
        {
            caretToEnd--;
            // by the string: MoveTextEnd counts the drawn characters, stale right after the text changed
            searchField.stringPosition = searchField.text.Length;
        }
        if (!typing) Ui.Deselect(); // a clicked button must not also take the gamepad's submit
        // typing on the list goes on in the search box
        if (typed.Length > 0)
        {
            if (!typing && searchField && searchField.gameObject.activeInHierarchy)
            {
                searchField.text = search + typed;
                FocusSearch();
            }
            typed.Clear();
        }

        // Esc (B) empties the search first, then closes
        if (ZInput.GetKeyDown(KeyCode.Escape) || Pad.Down("JoyButtonB"))
        {
            if (search.Length == 0 || !searchField) Close();
            else
            {
                searchField.text = "";
                if (!Pad.Active) FocusSearch();
            }
            return;
        }
        bool shift = ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKey(KeyCode.RightShift);
        if (Pad.Down("JoyMap") || ZInput.GetKeyDown(KeyCode.Tab) && !shift)
        {
            SwitchToMap();
            return;
        }

        int move = 0;
        bool up = ZInput.GetKeyDown(KeyCode.UpArrow);
        if (Pad.Down("JoyDPadUp") || Pad.Down("JoyLStickUp") || up) move--;
        if (Pad.Down("JoyDPadDown") || Pad.Down("JoyLStickDown") || ZInput.GetKeyDown(KeyCode.DownArrow)) move++;
        if (Pad.Down("JoyLTrigger") || ZInput.GetKeyDown(KeyCode.PageUp)) move -= PageRows;
        if (Pad.Down("JoyRTrigger") || ZInput.GetKeyDown(KeyCode.PageDown)) move += PageRows;
        if (up && !typing && selected <= 0)
        {
            // Up from the top row goes back to the search box
            FocusSearch();
            return;
        }
        if (move != 0)
        {
            mouseSelects = false;
            // leave the search box, so the keys (Left/Right, Delete) work on the list
            if (typing) searchField.DeactivateInputField();
            Select(Destinations.Step(selected, move, rows.Count), scrollIntoView: true);
        }

        if (Pad.Down("JoyLBumper") || Pad.Down("JoyDPadLeft") || !typing && ZInput.GetKeyDown(KeyCode.LeftArrow)) StepTab(-1);
        if (Pad.Down("JoyRBumper") || Pad.Down("JoyDPadRight") || !typing && ZInput.GetKeyDown(KeyCode.RightArrow)) StepTab(1);
        if (Pad.Down("JoyButtonX")) ToggleSort();
        if (Pad.Down("JoyButtonY") || ZInput.GetKeyDown(KeyCode.Insert)) ToggleFavorite(selected);
        if (Pad.Down("JoyRStick") || !typing && ZInput.GetKeyDown(KeyCode.Delete)) ToggleIgnored(selected);
        if (ZInput.GetKeyDown(KeyCode.F2))
        {
            Rename(selected);
            return;
        }
        if (Pad.Down("JoyButtonA") || ZInput.GetKeyDown(KeyCode.Return) || ZInput.GetKeyDown(KeyCode.KeypadEnter))
        {
            TravelTo(selected);
            return;
        }

        Wheel();

        // the mouse takes over the selection again once it moves
        var mouse = ZInput.pointerPosition;
        if ((mouse - lastMouse).sqrMagnitude > 4f) mouseSelects = true;
        lastMouse = mouse;
    }

    const int WheelRows = 1;

    /// <summary>The mouse wheel scrolls the list a row a notch (the game's own wheel reads nothing meanwhile).</summary>
    static void Wheel()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        float y = mouse != null ? mouse.scroll.ReadValue().y : 0f;
        if (y == 0f) return;
        // Windows sends 120 a notch; some systems send notches
        float notches = Mathf.Abs(y) >= 10f ? y / 120f : y;
        float max = Mathf.Max(0, content.sizeDelta.y - viewport.rect.height);
        content.anchoredPosition = new Vector2(0, Mathf.Clamp(content.anchoredPosition.y - notches * WheelRows * rowPitch, 0, max));
        scroll.velocity = Vector2.zero;
    }

    static void Render()
    {
        bool pad = Pad.Active;
        var atlas = Journal.Current;
        if (atlas == null) return;
        var fromZdo = source.m_nview.GetZDO();

        Ui.SetHeader(title, T("title"));
        Set(from, T("from", Destinations.DisplayName(fromZdo != null ? PortalWorld.Tag(fromZdo) : "", T("unnamed"))));
        if (searchField && searchField.placeholder is TMP_Text placeholder) Set(placeholder, T("search"));

        tabAll.interactable = tab != ListTab.All;
        tabFavorites.interactable = tab != ListTab.Favorites;
        tabIgnored.interactable = tab != ListTab.Ignored;
        // each tab counts the rows it shows (with the search, without hidden portals), one font size for the three
        Ui.SetLabels(new Component[] { tabAll, tabFavorites, tabIgnored }, new[]
        {
            (pad ? Pad.Glyph("JoyLBumper") + " " : "") + $"{T("tab_all")} ({RowsOf(ListTab.All)})",
            $"{T("tab_favorites")} ({RowsOf(ListTab.Favorites)})",
            $"{T("tab_ignored")} ({RowsOf(ListTab.Ignored)})" + (pad ? " " + Pad.Glyph("JoyRBumper") : ""),
        });
        // the sort button keeps one font size through its three labels
        string sortGlyph = pad ? Pad.Glyph("JoyButtonX") + " " : "";
        var sortLabels = new[] { sortGlyph + T("sort_name"), sortGlyph + T("sort_nearest"), sortGlyph + T("sort_farthest") };
        Ui.SetLabel(sortButton, sortLabels[(int)Plugin.SortBy.Value], sortLabels);
        mapButton.gameObject.SetActive(MapAvailable);
        // the button's key: the gamepad's glyph in front (the game's way), the keyboard's after it
        string mapLabel = pad ? Pad.Glyph("JoyMap") + " " + T("map_view") : T("map_view") + " (Tab)";
        string closeLabel = pad ? Pad.Glyph("JoyButtonB") + " " + T("close") : T("close") + " (Esc)";
        Ui.SetLabel(mapButton, mapLabel, new[] { mapLabel });
        Ui.SetLabel(closeButton, closeLabel, new[] { closeLabel });

        string counts = T("known", atlas.KnownCount.ToString());
        if (hidden > 0) counts += "   ·   " + T("hidden", hidden.ToString());
        Set(footer, counts + "\n" + T("learn_more"));
        // in the search box the arrows and Delete edit the text: Up/Down leave it for the list first
        Set(hints, pad
            ? $"{Pad.Hint(Pad.Glyph("JoyDPadUp") + Pad.Glyph("JoyDPadDown"), T("select"))}     {Pad.Hint(Pad.Glyph("JoyButtonA"), T("travel"))}     {Pad.Hint(Pad.Glyph("JoyButtonY"), T("favorite"))}     {Pad.Hint(Pad.Glyph("JoyRStick"), T("ignore"))}"
            : SearchFocused
                ? $"{Pad.Hint("Enter", T("travel"))}     {Pad.Hint("↑↓", T("select"))}     {Pad.Hint("Ins", T("favorite"))}     {Pad.Hint("F2", T("rename"))}"
                : $"{Pad.Hint("Enter", T("travel"))}     {Pad.Hint("←→", T("tabs"))}     {Pad.Hint("Ins", T("favorite"))}     {Pad.Hint("Del", T("ignore"))}     {Pad.Hint("F2", T("rename"))}");

        // rows
        while (pool.Count < rows.Count) pool.Add(NewRow(pool.Count));
        var player = Player.m_localPlayer;
        Vector3 here = player ? player.transform.position : Vector3.zero;
        for (int i = 0; i < pool.Count; i++)
        {
            var row = pool[i];
            bool used = i < rows.Count;
            if (row.Go.activeSelf != used) row.Go.SetActive(used);
            if (!used) continue;
            var e = rows[i];
            Set(row.Name, Destinations.DisplayName(e.Name, T("unnamed")));
            int meters = Mathf.RoundToInt(Vector3.Distance(here, e.Position));
            Set(row.Distance, meters >= 1000 ? $"{meters / 1000f:0.0} km" : $"{meters} m");
            if (row.Icon && portalIcon && row.Icon.sprite != portalIcon) row.Icon.sprite = portalIcon;
            bool favorite = Journal.IsFavorite(e.Key);
            bool isSelected = i == selected;
            // the selected row is the game's orange bar: light text and star on it
            row.Star.sprite = favorite ? Ui.Star : Ui.StarOutline;
            row.Star.color = isSelected ? Color.white : favorite ? Ui.Orange : new Color(Ui.Dim.r, Ui.Dim.g, Ui.Dim.b, 0.7f);
            row.Distance.color = isSelected ? Color.white : Ui.Dim;
            // the ignore mark shows on the selected row (and on every row of the Ignored tab), renaming on the selected
            // row (mouse and keyboard: renaming needs typing)
            bool ignored = tab == ListTab.Ignored;
            row.Ignore.gameObject.SetActive(isSelected || ignored);
            row.Ignore.color = ignored ? Ui.Orange : isSelected ? Color.white : Ui.Dim;
            bool rename = isSelected && !pad;
            if (row.Rename.gameObject.activeSelf != rename) row.Rename.gameObject.SetActive(rename);
            if (row.Selected && row.Selected.activeSelf != isSelected) row.Selected.SetActive(isSelected);
            HoverLook(row, !pad);
        }
        content.sizeDelta = new Vector2(0, Mathf.Max(0, rows.Count * rowPitch - (rowPitch - rowHeight)));

        empty.gameObject.SetActive(rows.Count == 0);
        if (rows.Count == 0) Set(empty, EmptyText());

        if (scrollToSelected)
        {
            scrollToSelected = false;
            Canvas.ForceUpdateCanvases();
            float y = Destinations.ScrollTo(selected, rowPitch, rowHeight, viewport.rect.height, content.anchoredPosition.y);
            content.anchoredPosition = new Vector2(0, Mathf.Clamp(y, 0, Mathf.Max(0, content.sizeDelta.y - viewport.rect.height)));
            scroll.velocity = Vector2.zero;
        }
    }

    /// <summary>
    /// The mouse's hover tint on a row: off with the gamepad, where the row the pointer last rested on would stay tinted
    /// next to the gamepad's selection.
    /// </summary>
    static void HoverLook(Row row, bool on)
    {
        if (!row.Button || row.HoverOff == !on) return;
        row.HoverOff = !on;
        var colors = row.Colors;
        var sprites = row.Sprites;
        if (!on)
        {
            colors.highlightedColor = colors.normalColor;
            sprites.highlightedSprite = null;
        }
        row.Button.colors = colors;
        row.Button.spriteState = sprites;
    }

    static int RowsOf(ListTab t) =>
        t == tab ? rows.Count : PortalWorld.DestinationsFrom(PortalWorld.PositionOf(source), t, search, out _).Count;

    static void Set(TMP_Text text, string value)
    {
        if (text && text.text != value) text.text = value;
    }

    static string EmptyText()
    {
        if (tab == ListTab.Favorites && search.Length == 0) return T("empty_favorites");
        if (tab == ListTab.Ignored && search.Length == 0) return T("empty_ignored");
        if (search.Length > 0) return T("empty_search");
        if (hidden > 0) return T("empty_hidden");
        return T("empty_none");
    }
}
