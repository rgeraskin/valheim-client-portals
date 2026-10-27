using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using ClientPortals;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace ClientPortalsDevHarness;

/// <summary>
/// Development-only test driver. With CLIENTPORTALS_HARNESS_DIR set (or a harness_dir.txt next to the dll) it loads a
/// local world with a test character and runs the commands that appear in &lt;dir&gt;/cmd.txt, logging to log.txt and
/// saving screenshots to shots/. Input goes through a virtual gamepad and the keyboard device, so the game and the
/// mod read it the way they read a player's.
/// </summary>
[BepInPlugin("rgeraskin.ClientPortals.DevHarness", "ClientPortals DevHarness", "0.0.1")]
[BepInDependency(Plugin.Guid)]
public class Harness : BaseUnityPlugin
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    string dir;
    StreamWriter log;
    Gamepad pad;
    List<ZDO> portals = new();
    string originalLanguage;
    bool keepHealth;

    static string Env(string name, string fallback) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : fallback;
    static string CharName => Env("CLIENTPORTALS_CHAR", "PortalTester");
    static string WorldName => Env("CLIENTPORTALS_WORLD", "PortalTest");

    void Awake()
    {
        dir = Environment.GetEnvironmentVariable("CLIENTPORTALS_HARNESS_DIR");
        var file = Path.Combine(Path.GetDirectoryName(Info.Location) ?? "", "harness_dir.txt");
        if (string.IsNullOrEmpty(dir) && File.Exists(file)) dir = File.ReadAllText(file).Trim();
        if (string.IsNullOrEmpty(dir))
        {
            Logger.LogInfo("CLIENTPORTALS_HARNESS_DIR not set, harness idle");
            enabled = false;
            return;
        }
        Directory.CreateDirectory(Path.Combine(dir, "shots"));
        log = new StreamWriter(Path.Combine(dir, "log.txt"), append: true) { AutoFlush = true };
        Application.runInBackground = true;
        // the game window may not have the focus on the test machine: keep reading the (virtual) devices
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        Log("harness up");
        StartCoroutine(Run());
    }

    void Log(string msg)
    {
        log?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
        Logger.LogInfo(msg);
    }

    void Update()
    {
        var player = Player.m_localPlayer;
        if (player && keepHealth && player.GetHealth() < player.GetMaxHealth()) player.SetHealth(player.GetMaxHealth());
    }

    IEnumerator Run()
    {
        while (FejdStartup.instance == null || FejdStartup.instance.m_profiles == null) yield return new WaitForSeconds(0.5f);
        yield return new WaitForSeconds(2f);
        if (Env("CLIENTPORTALS_AUTOSTART", "") == "1") StartWorld();
        while (true)
        {
            yield return new WaitForSeconds(0.3f);
            var cmdFile = Path.Combine(dir, "cmd.txt");
            if (!File.Exists(cmdFile)) continue;
            var lines = File.ReadAllLines(cmdFile);
            File.Delete(cmdFile);
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                Log("> " + line);
                IEnumerator routine = null;
                try { routine = Execute(line.Split(new[] { ' ' }, 2)[0], Args(line)); }
                catch (Exception e) { Log("! " + e); }
                if (routine != null) yield return SafeRun(routine);
            }
            Log("done");
        }
    }

    static string[] Args(string line)
    {
        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Skip(1).ToArray();
    }

    IEnumerator SafeRun(IEnumerator routine)
    {
        while (true)
        {
            object current;
            try
            {
                if (!routine.MoveNext()) yield break;
                current = routine.Current;
            }
            catch (Exception e)
            {
                Log("! " + e);
                yield break;
            }
            yield return current;
        }
    }

    // ------------------------------------------------------------------ world and character

    void StartWorld()
    {
        var fs = FejdStartup.instance;
        var file = CharName.ToLowerInvariant();
        var profile = new PlayerProfile(file, FileHelpers.FileSource.Local);
        if (!PlayerProfile.HaveProfile(file))
        {
            profile.SetName(CharName);
            profile.m_firstSpawn = false;
            profile.Save();
            Log("created local character " + CharName);
        }
        else if (profile.Load() && profile.m_firstSpawn)
        {
            profile.m_firstSpawn = false;
            profile.Save();
        }
        fs.UpdateCharacterList();
        fs.SetSelectedProfile(file);
        // a profile the game hasn't listed falls back to the first one: never play (and save) someone's real character
        var selected = fs.m_profiles.Count > fs.m_profileIndex && fs.m_profileIndex >= 0 ? fs.m_profiles[fs.m_profileIndex] : null;
        if (selected == null || selected.GetFilename() != file)
        {
            Log($"! the game selected '{selected?.GetName()}' instead of {CharName}: not starting");
            return;
        }
        fs.OnCharacterStart();
        var world = World.GetCreateWorld(WorldName, FileHelpers.FileSource.Local);
        if (!World.HaveWorld(WorldName))
        {
            world = new World(WorldName, "PortalTest1") { m_fileSource = FileHelpers.FileSource.Local, m_needsDB = false };
            world.SaveWorldFWLData(DateTime.Now);
            Log("created local world " + WorldName);
        }
        fs.m_world = world;
        fs.m_openServerToggle.isOn = false;
        fs.m_publicServerToggle.isOn = false;
        fs.m_crossplayServerToggle.isOn = false;
        fs.OnWorldStart();
        Log($"starting world {WorldName} as {CharName}");
    }

    // ------------------------------------------------------------------ commands

    IEnumerator Execute(string cmd, string[] a)
    {
        var player = Player.m_localPlayer;
        switch (cmd)
        {
            case "start": StartWorld(); break;
            case "wait": yield return new WaitForSeconds(F(a[0])); break;
            case "frames":
                for (int i = 0; i < (int)F(a[0]); i++) yield return null;
                break;
            case "waitplayer":
                while (!Player.m_localPlayer || Player.m_localPlayer.IsTeleporting() || Player.m_localPlayer.InIntro()) yield return new WaitForSeconds(0.5f);
                Log("player ready at " + Player.m_localPlayer.transform.position);
                break;
            case "god":
                player.SetGodMode(true);
                EnvMan.instance.m_debugTimeOfDay = true;
                EnvMan.instance.m_debugTime = 0.45f;
                EnvMan.instance.m_debugEnv = "Clear";
                ZoneSystem.instance.SetGlobalKey(GlobalKeys.PassiveMobs);
                ZoneSystem.instance.SetGlobalKey(GlobalKeys.EventRate, 0f);
                keepHealth = true;
                Log("god on, health kept full, clear noon, passive mobs, no raids");
                break;
            case "portals": ListPortals(a.Length > 0 ? (int)F(a[0]) : 20); break;
            case "tpportal": yield return TeleportToPortal((int)F(a[0]), a.Length > 1 ? F(a[1]) : 3f); break;
            case "tpname":
                {
                    // tpname <tag> [nth]: in front of a portal by its tag (indexes shift as objects load)
                    ListPortals(0); // the game recycles objects: never reuse an old list
                    int nth = a.Length > 1 ? (int)F(a[1]) : 0;
                    int idx = -1;
                    for (int i = 0, seen = 0; i < portals.Count; i++)
                        if (portals[i].GetString(ZDOVars.s_tag) == a[0] && seen++ == nth)
                        {
                            idx = i;
                            break;
                        }
                    if (idx < 0) Log("no portal " + a[0]);
                    else yield return TeleportToPortal(idx, 3f);
                    break;
                }
            case "tp": yield return Teleport(new Vector3(F(a[0]), 0, F(a[1])), Quaternion.identity, ground: true); break;
            case "open":
                {
                    // open: the picker of the nearest portal (what the travel key does at a portal)
                    var portal = NearestPortal();
                    if (portal) Picker.Open(portal);
                    Log(portal ? $"opened the picker at {Tag(portal)}: list={ListWindow.IsOpen} map={MapPicker.IsOpen}" : "no portal within 8 m");
                    break;
                }
            case "altuse":
                {
                    // altuse: alt + use at the nearest portal (Shift + E): renames both ends of a pair with RenameBoth
                    var portal = NearestPortal();
                    Log(portal ? $"interact (alt) with {Tag(portal)}: {portal.Interact(player, false, true)}" : "no portal within 8 m");
                    break;
                }
            case "walkin":
                {
                    var portal = NearestPortal();
                    if (portal) portal.Teleport(player);
                    Log(portal ? "walked into " + Tag(portal) : "no portal within 8 m");
                    break;
                }
            case "hover":
                {
                    var portal = NearestPortal();
                    Log(portal ? "hover: " + portal.GetHoverText().Replace("\n", " | ") : "no portal");
                    break;
                }
            case "pad": yield return Press(a[0], a.Length > 1 ? (int)F(a[1]) : 3); break;
            case "stick": yield return Stick(F(a[0]), F(a[1]), a.Length > 2 ? F(a[2]) : 0.5f); break;
            case "key": yield return Key(a[0]); break;
            case "keydown":
                {
                    // keydown <Key>[+Key]: holds keys until keyup (a held movement key while walking into a portal)
                    var keys = a[0].Split('+').Select(k => (UnityEngine.InputSystem.Key)Enum.Parse(typeof(UnityEngine.InputSystem.Key), k, true)).ToArray();
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
                    for (int i = 0; i < 3; i++) yield return null;
                    break;
                }
            case "keyup":
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                for (int i = 0; i < 3; i++) yield return null;
                break;
            case "type":
                {
                    // type <text>: text events of the keyboard (what a layout's letters send), one character a frame
                    foreach (char c in string.Join(" ", a))
                    {
                        InputSystem.QueueTextEvent(Keyboard.current, c);
                        yield return null;
                    }
                    for (int i = 0; i < 3; i++) yield return null;
                    break;
                }
            case "scroll":
                {
                    // scroll <y>: the mouse wheel for a few frames
                    float y = F(a[0]);
                    float before = GameCamera.instance.m_distance, zoomBefore = Minimap.instance.LargeZoom;
                    for (int i = 0; i < 4; i++)
                    {
                        InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = Mouse.current.position.ReadValue(), scroll = new Vector2(0, y) });
                        yield return null;
                    }
                    InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = Mouse.current.position.ReadValue() });
                    for (int i = 0; i < 3; i++) yield return null;
                    Log($"scroll {y}: camera distance {before:0.00} -> {GameCamera.instance.m_distance:0.00}, map zoom {zoomBefore:0.000} -> {Minimap.instance.LargeZoom:0.000}");
                    break;
                }
            case "mapmiddleclick": Minimap.instance.OnMapMiddleClick(null); Log("map middle click"); break;
            case "keyname":
                {
                    var rk = Plugin.RenameKey.Value;
                    Log($"RenameKey='{rk}' main={rk.MainKey}({(int)rk.MainKey}) mods=[{string.Join(",", rk.Modifiers.Select(m => $"{m}({(int)m})"))}] name='{(global::ClientPortals.Pad.KeyName(rk))}'");
                    foreach (var k in new[] { KeyCode.E, KeyCode.Q, KeyCode.LeftShift, KeyCode.LeftAlt })
                    {
                        string disp;
                        try { disp = ZInput.KeyCodeToDisplayName(k); } catch (Exception e) { disp = "! " + e.GetType().Name; }
                        Log($"  {k}({(int)k}): KeyName='{(global::ClientPortals.Pad.KeyName(k))}' display='{disp}'");
                    }
                    break;
                }
            case "names":
                {
                    // names: the map view's badges and which of them show their name
                    var shown = MapPicker.markers.Where(m => m.Visible && m.Entry != null).ToList();
                    Log($"names: {shown.Count(m => m.Name.gameObject.activeSelf)}/{shown.Count} shown (ShowNames={Plugin.ShowNames.Value}): " +
                        string.Join(", ", shown.Select(m => (m.Name.gameObject.activeSelf ? "" : "-") + m.Entry.Name)));
                    break;
                }
            case "hudtext": Log("hud hover: " + Hud.instance.m_hoverName.text.Replace("\n", " | ")); break;
            case "mapblur": MapPicker.searchField.DeactivateInputField(); for (int i = 0; i < 3; i++) yield return null; Log("map search focused=" + MapPicker.searchField.isFocused); break;
            case "focus":
                Log($"focus: list search={ListWindow.searchField?.isFocused} text='{ListWindow.searchField?.text}' caret={ListWindow.searchField?.stringPosition} " +
                    $"map search={MapPicker.searchField?.isFocused} text='{MapPicker.searchField?.text}' legacy B down={Input.GetKeyDown(KeyCode.B)} " +
                    $"legacy patched={HarmonyLib.Harmony.GetPatchInfo(typeof(Input).GetMethod("GetKeyDown", new[] { typeof(KeyCode) }))?.Prefixes.Count} " +
                    $"pins shown={Minimap.instance.m_visibleIconTypes.Count(v => v)}/{Minimap.instance.m_visibleIconTypes.Length} onlyPortals={Plugin.OnlyPortals.Value} netPanel={ConnectPanel.IsVisible()} listScroll={(ListWindow.content ? ListWindow.content.anchoredPosition.y : -1):0} tab={ListWindow.tab} mapBlocks={MapPicker.BlocksInput} textDialog={TextInput.instance.m_panel.activeSelf}");
                break;
            case "search":
                if (ListWindow.searchField) ListWindow.searchField.text = a.Length > 0 ? string.Join(" ", a) : "";
                break;
            case "click": Click(a[0]); break;
            case "mapsearch":
                if (MapPicker.searchField)
                {
                    // like a click into the box, then typing
                    MapPicker.searchField.ActivateInputField();
                    MapPicker.searchField.text = a.Length > 0 ? string.Join(" ", a) : "";
                    MapPicker.searchField.MoveTextEnd(false);
                }
                Log(MapPicker.searchField ? $"map search '{MapPicker.searchField.text}' matches={MapPicker.matches}" : "no map search field");
                break;
            case "give":
                {
                    // give <prefab> [count]: into the inventory (the first free slot, the hotbar row first)
                    var added = player.GetInventory().AddItem(a[0], a.Length > 1 ? (int)F(a[1]) : 1, 1, 0, 0L, "", cheated: true);
                    Log(added != null ? $"gave {a[0]} at {added.m_gridPos}" : "could not give " + a[0]);
                    break;
                }
            case "keyprobe":
                {
                    // keyprobe <Key> <KeyCode>: presses a key and logs what the input layers and the travel key see, frame by frame
                    var key = (UnityEngine.InputSystem.Key)Enum.Parse(typeof(UnityEngine.InputSystem.Key), a[0], true);
                    var code = (KeyCode)Enum.Parse(typeof(KeyCode), a[1], true);
                    var keyboard = Keyboard.current;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
                    for (int i = 0; i < 4; i++)
                    {
                        yield return null;
                        Log($"frame {i}: device={keyboard[key].isPressed}/{keyboard[key].wasPressedThisFrame} zinput={ZInput.GetKey(code)}/{ZInput.GetKeyDown(code)} " +
                            $"travel held={TravelKey.Held} padPending={TravelKey.padPending} hovered={(TravelKey.Hovered() ? "yes" : "no")} picker={Picker.IsOpen} cfg={Plugin.TravelKey.Value} " +
                            $"busy={Travel.Busy} journal={Journal.Current != null} text={TextInput.IsVisible()} menu={Menu.IsVisible()} console={Console.IsVisible()} " +
                            $"chat={(Chat.instance && Chat.instance.HasFocus())} inv={InventoryGui.IsVisible()} map={Minimap.IsOpen()} build={Hud.IsPieceSelectionVisible()}");
                    }
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    for (int i = 0; i < 4; i++) yield return null;
                    break;
                }
            case "ray":
                {
                    // ray: what the camera ray (the game's hover ray) hits, and the player's hover object
                    var cam = GameCamera.instance.transform;
                    var hits = Physics.RaycastAll(cam.position, cam.forward, 50f, player.m_interactMask).OrderBy(h => h.distance);
                    foreach (var h in hits.Take(6))
                        Log($"  hit {Actions_Path(h.collider.transform)} at {h.distance:0.0} m, from eye {Vector3.Distance(player.m_eye.position, h.point):0.0} (max {player.m_maxInteractDistance})");
                    Log($"hover object: {(player.GetHoverObject() ? Actions_Path(player.GetHoverObject().transform) : "none")}");
                    break;
                }
            case "clearnature":
                {
                    // clearnature <radius>: removes trees, stumps, bushes, rocks and pickables around the player (showcase scenes)
                    float r = F(a[0]);
                    var here = player.transform.position;
                    int n = 0;
                    foreach (var nv in FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
                    {
                        if (!nv || !nv.IsValid() || Vector3.Distance(nv.transform.position, here) > r) continue;
                        var go = nv.gameObject;
                        if (go.GetComponent<TreeBase>() || go.GetComponent<TreeLog>() || go.GetComponent<Destructible>() ||
                            go.GetComponent<MineRock>() || go.GetComponent<MineRock5>() || go.GetComponent<Pickable>())
                        {
                            ZNetScene.instance.Destroy(go);
                            n++;
                        }
                    }
                    Log($"cleared {n} objects within {r} m");
                    break;
                }
            case "textinput":
                Log($"text dialog visible={TextInput.instance && TextInput.instance.m_panel.activeSelf} topic='{TextInput.instance?.m_topic.text}' text='{TextInput.instance?.m_inputField.text}' limit={TextInput.instance?.m_inputField.characterLimit}");
                break;
            case "textenter":
                {
                    // textenter <text...>: types into the game's text dialog and confirms it
                    var ti = TextInput.instance;
                    if (!ti || !ti.m_panel.activeSelf) { Log("! no text dialog"); break; }
                    ti.m_inputField.text = string.Join(" ", a);
                    ti.OnEnter();
                    Log("text entered: " + string.Join(" ", a));
                    break;
                }
            case "state": State(); break;
            case "ui": DumpUi(); break;
            case "shot": yield return Shot(a[0]); break;
            case "lang":
                if (a[0] == "restore")
                {
                    if (originalLanguage != null) Localization.instance.SetLanguage(originalLanguage);
                    Log("language restored to " + Localization.instance.GetSelectedLanguage());
                }
                else
                {
                    originalLanguage ??= Localization.instance.GetSelectedLanguage();
                    Localization.instance.SetLanguage(a[0]);
                    Log($"language {Localization.instance.GetSelectedLanguage()} (was {originalLanguage}): {Texts.T("title")} / {Texts.T("known", "7")}");
                }
                break;
            case "cfg": SetConfig(a[0], a[1]); break;
            case "forget": ResetJournal(); break;
            case "table": SpawnTable(); break;
            case "tablewrite":
                {
                    var t = NearestTable();
                    Log(t ? "write: " + t.OnWrite(t.m_writeSwitch, player, null) + " " + t.GetWriteHoverText().Replace("\n", " | ") : "no table");
                    break;
                }
            case "tableread":
                {
                    var t = NearestTable();
                    if (t) t.OnRead(t.m_readSwitch, player, null);
                    Log(t ? "read; known now " + Journal.Current?.KnownCount : "no table");
                    break;
                }
            case "journal":
                {
                    var pl = Player.m_localPlayer;
                    Log("customData keys: " + string.Join(", ", pl.m_customData.Keys.Select(k => $"{k}({pl.m_customData[k].Length})")));
                    Log($"journal owner={Journal.owner?.GetPlayerName()} world={Journal.world} uid={ZNet.instance.GetWorldUID()} profile={Game.instance.GetPlayerProfile().GetName()}");
                    foreach (var e in Journal.Current.All.OrderBy(e => e.Name).Take(a.Length > 0 ? (int)F(a[0]) : 80))
                        Log($"  {(e.Removed ? "x" : " ")} '{e.Name}' {e.Position} fav={Journal.IsFavorite(e.Key)} stamp={e.Stamp}");
                    break;
                }
            case "mapinfo":
                {
                    var m = Minimap.instance;
                    Log($"map mode={m.m_mode} zoom={m.LargeZoom:0.000} offset={m.m_mapOffset} uv={m.m_mapImageLarge.uvRect} hintsShown={m.m_hints.Count(h => h && h.activeSelf)} publicHotkey={(m.m_publicPosition.GetComponentInChildren<UIGamePad>(true)?.enabled)} inventory={InventoryGui.IsVisible()}");
                    break;
                }
            case "mouseat":
            case "pointat":
                {
                    // mouseat <name>: the mouse over a map badge (by portal name), as a player would point at it;
                    // pointat: only the game's mouse (the system cursor stays where it is, the test machine may be in use)
                    var cam = Minimap.instance.m_mapImageLarge.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Minimap.instance.m_mapImageLarge.canvas.worldCamera;
                    var m = MapPicker.markers.FirstOrDefault(x => x.Visible && x.Entry != null && x.Entry.Name == (a.Length > 0 ? string.Join(" ", a) : x.Entry.Name));
                    if (m == null) { Log("no visible badge"); break; }
                    var p = RectTransformUtility.WorldToScreenPoint(cam, m.Rt.position);
                    if (cmd == "mouseat") Mouse.current.WarpCursorPosition(p);
                    InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = p });
                    for (int i = 0; i < 6; i++) yield return null;
                    Log($"mouse at {p} over '{m.Entry.Name}', hovered={(MapPicker.hovered >= 0 ? MapPicker.markers[MapPicker.hovered].Entry?.Name : "-")}");
                    break;
                }
            case "pointxy":
                {
                    // pointxy <x> <y>: only the game's mouse at a screen point (the system cursor stays)
                    var p = new Vector2(F(a[0]), F(a[1]));
                    InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = p });
                    for (int i = 0; i < 6; i++) yield return null;
                    Log($"game mouse at {p}");
                    break;
                }
            case "mapclick": Minimap.instance.OnMapLeftClick(); Log("map left click"); break;
            case "maprightclick": Minimap.instance.RemovePinUnderPointer(); Log("map right click"); break;
            case "settle":
                {
                    // until the player is not teleporting, then a moment for the area to load
                    float until = Time.time + 40f;
                    while (Player.m_localPlayer.IsTeleporting() && Time.time < until) yield return new WaitForSeconds(0.3f);
                    yield return new WaitForSeconds(a.Length > 0 ? F(a[0]) : 2f);
                    Log("settled at " + Player.m_localPlayer.transform.position);
                    break;
                }
            case "placeportal":
                {
                    // placeportal <prefab> <x> <z> <yaw> <tag...>: a portal on the ground, named
                    var prefab = ZNetScene.instance.GetPrefab(a[0]);
                    var pos = new Vector3(F(a[1]), 0, F(a[2]));
                    // the ground where it is loaded (a portal above it has no support and breaks), else the generator's height
                    float ground = ZoneSystem.instance.IsZoneLoaded(pos) && ZoneSystem.instance.GetSolidHeight(pos, out float solid, 1000)
                        ? solid : WorldGenerator.instance.GetHeight(pos.x, pos.z);
                    pos.y = Mathf.Max(ground, ZoneSystem.instance.m_waterLevel + 0.5f);
                    var go = Instantiate(prefab, pos, Quaternion.Euler(0, F(a[3]), 0));
                    var tag = string.Join(" ", a.Skip(4));
                    go.GetComponent<ZNetView>().GetZDO().Set(ZDOVars.s_tag, tag);
                    Log($"placed {a[0]} '{tag}' at {pos}");
                    break;
                }
            case "findbiome":
                {
                    // findbiome <Biome> [minDist] [maxDist]: flat-ish ground of that biome, nearest first from the origin
                    var biome = (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), a[0]);
                    float min = a.Length > 1 ? F(a[1]) : 0, max = a.Length > 2 ? F(a[2]) : 6000;
                    var wg = WorldGenerator.instance;
                    bool found = false;
                    for (float r = min; r <= max && !found; r += 50f)
                        for (int i = 0, n = Mathf.Max(8, (int)(r / 25)); i < n && !found; i++)
                        {
                            float ang = 2 * Mathf.PI * i / n, x = Mathf.Cos(ang) * r, z = Mathf.Sin(ang) * r;
                            if (wg.GetBiome(x, z) != biome || wg.GetHeight(x, z) < ZoneSystem.instance.m_waterLevel + 2) continue;
                            if (Mathf.Abs(wg.GetHeight(x + 6, z) - wg.GetHeight(x - 6, z)) > 2.5f || Mathf.Abs(wg.GetHeight(x, z + 6) - wg.GetHeight(x, z - 6)) > 2.5f) continue;
                            Log($"{biome} at {x:0} {z:0} height {wg.GetHeight(x, z):0}");
                            found = true;
                        }
                    if (!found) Log("no " + biome);
                    break;
                }
            case "exploreall": Minimap.instance.ExploreAll(); Log("map explored"); break;
            case "hud":
                Hud.instance.m_rootObject.SetActive(a[0] == "on");
                MessageHud.instance.m_messageText.gameObject.SetActive(a[0] == "on");
                MessageHud.instance.m_messageCenterText.gameObject.SetActive(a[0] == "on");
                break;
            case "time": EnvMan.instance.m_debugTime = F(a[0]); break;
            case "env": EnvMan.instance.m_debugEnv = a[0]; break;
            case "favorite":
                {
                    var tag = string.Join(" ", a);
                    var e = Journal.Current.Known.FirstOrDefault(x => x.Name == tag);
                    if (e != null) Journal.ToggleFavorite(e.Key);
                    Log(e != null ? $"favorite '{tag}' = {Journal.IsFavorite(e.Key)}" : "unknown portal " + tag);
                    break;
                }
            case "camshot":
                {
                    // camshot <name> <yaw> <pitch> <dist> <height>: the camera on the nearest portal, from its front
                    var portal = NearestPortal();
                    var gc = GameCamera.instance;
                    gc.enabled = false;
                    var center = portal.transform.position + Vector3.up * F(a[4]);
                    var dir = Quaternion.Euler(F(a[2]), portal.transform.eulerAngles.y + 180f + F(a[1]), 0f) * Vector3.forward;
                    gc.m_camera.transform.position = center - dir * F(a[3]);
                    gc.m_camera.transform.rotation = Quaternion.LookRotation(dir);
                    for (int i = 0; i < 20; i++) yield return null;
                    yield return Shot(a[0]);
                    gc.enabled = true;
                    break;
                }
            case "tpoffset":
                {
                    // tpoffset <right> <forward>: next to the nearest portal (in its frame), facing it
                    var portal = NearestPortal();
                    var t = portal.transform;
                    var pos = t.position + t.right * F(a[0]) + t.forward * F(a[1]);
                    pos.y = ZoneSystem.instance.GetGroundHeight(pos);
                    var look = t.position - pos;
                    look.y = 0;
                    yield return Teleport(pos, Quaternion.LookRotation(look), ground: false);
                    break;
                }
            case "aim":
                {
                    // aim <right> <up>: the player (and the camera) looks at a point of the nearest portal's frame
                    var t = NearestPortal().transform;
                    var target = t.position + t.right * F(a[0]) + Vector3.up * F(a[1]);
                    Player.m_localPlayer.SetLookDir((target - GameCamera.instance.m_camera.transform.position).normalized, 0f);
                    for (int i = 0; i < 10; i++) yield return null;
                    break;
                }
            case "hideplayer":
                Player.m_localPlayer.m_visual.SetActive(a[0] != "on");
                break;
            case "title":
                {
                    var t = ListWindow.title;
                    Log($"title text='{t.text}' font={t.font?.name} mat={t.fontSharedMaterial?.name} style={t.fontStyle} size={t.fontSize} color={t.color} rendered={t.textInfo?.characterCount} visible={t.textInfo?.characterInfo?.Take(t.textInfo.characterCount).Count(c => c.isVisible)} fallbacks={string.Join(",", t.font?.fallbackFontAssetTable?.Select(f => f?.name) ?? new string[0])}");
                    var b = ListWindow.from;
                    Log($"from  text='{b.text}' font={b.font?.name} mat={b.fontSharedMaterial?.name} style={b.fontStyle} fallbacks={string.Join(",", b.font?.fallbackFontAssetTable?.Select(f => f?.name) ?? new string[0])}");
                    break;
                }
            case "mousepark":
                Mouse.current.WarpCursorPosition(new Vector2(Screen.width - 2, 2));
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = new Vector2(Screen.width - 2, 2) });
                break;
            case "where": Log($"player at {player.transform.position} teleporting={player.IsTeleporting()}"); break;
            case "save":
                // save: the world and the character, like the game's own save, so a restart loads what was written
                ZNet.instance.Save(true);
                Game.instance.SavePlayerProfile(true);
                Log("saved the world and the character");
                break;
            case "quit": Application.Quit(); break;
            default: Log("unknown command " + cmd); break;
        }
    }

    static float F(string s) => float.Parse(s, Inv);

    // ------------------------------------------------------------------ portals

    static string Tag(TeleportWorld p) => p.m_nview && p.m_nview.GetZDO() != null ? p.m_nview.GetZDO().GetString(ZDOVars.s_tag) : "?";

    void ListPortals(int max)
    {
        var here = Player.m_localPlayer.transform.position;
        portals = ZDOMan.instance.GetPortalList().Where(z => z.IsValid())
            .OrderBy(z => z.GetString(ZDOVars.s_tag)).ThenBy(z => z.GetPosition().x).ToList();
        Log($"{portals.Count} portal objects loaded; known {Journal.Current?.KnownCount}");
        for (int i = 0; i < portals.Count && i < max; i++)
        {
            var z = portals[i];
            var paired = !z.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal).IsNone();
            Log($"  #{i} '{z.GetString(ZDOVars.s_tag)}' {ZNetScene.instance.GetPrefab(z.GetPrefab())?.name} at {z.GetPosition()} dist={Vector3.Distance(here, z.GetPosition()):0} paired={paired}");
        }
    }

    IEnumerator TeleportToPortal(int index, float distance)
    {
        if (portals.Count == 0) ListPortals(0);
        var z = portals[index];
        var rot = z.GetRotation();
        var pos = z.GetPosition() + rot * Vector3.forward * distance;
        yield return Teleport(pos, rot * Quaternion.Euler(0, 180, 0), ground: false);
    }

    IEnumerator Teleport(Vector3 pos, Quaternion rot, bool ground)
    {
        var player = Player.m_localPlayer;
        if (ground) pos.y = Mathf.Max(ZoneSystem.instance.m_waterLevel, ZoneSystem.instance.GetGroundHeight(pos));
        if (!player.TeleportTo(pos + Vector3.up * 0.5f, rot, distantTeleport: true)) Log("! teleport refused (already teleporting?)");
        yield return null;
        float until = Time.time + 40f;
        while (player.IsTeleporting() && Time.time < until) yield return new WaitForSeconds(0.3f);
        if (player.IsTeleporting()) Log("! still teleporting after 40 s");
        yield return new WaitForSeconds(1.5f);
        Log("at " + player.transform.position);
    }

    static TeleportWorld NearestPortal()
    {
        var here = Player.m_localPlayer.transform.position;
        return FindObjectsByType<TeleportWorld>(FindObjectsSortMode.None)
            .Where(p => Vector3.Distance(here, p.transform.position) < 8f)
            .OrderBy(p => Vector3.Distance(here, p.transform.position)).FirstOrDefault();
    }

    static MapTable NearestTable()
    {
        var here = Player.m_localPlayer.transform.position;
        return FindObjectsByType<MapTable>(FindObjectsSortMode.None)
            .Where(t => Vector3.Distance(here, t.transform.position) < 10f)
            .OrderBy(t => Vector3.Distance(here, t.transform.position)).FirstOrDefault();
    }

    void SpawnTable()
    {
        var player = Player.m_localPlayer;
        var prefab = ZNetScene.instance.GetPrefab("piece_cartographytable");
        var pos = player.transform.position + player.transform.right * 3f;
        pos.y = ZoneSystem.instance.GetGroundHeight(pos);
        Instantiate(prefab, pos, player.transform.rotation);
        Log("cartography table at " + pos);
    }

    // ------------------------------------------------------------------ the mod's data

    void ResetJournal()
    {
        var player = Player.m_localPlayer;
        foreach (var k in player.m_customData.Keys.Where(k => k.StartsWith("ClientPortals.")).ToList())
            player.m_customData.Remove(k);
        Journal.owner = null;
        Log("journal reset; known " + Journal.Current?.KnownCount);
    }

    static string Actions_Path(Transform t)
    {
        var path = t.name;
        for (var p = t.parent; p; p = p.parent) path = p.name + "/" + path;
        return path;
    }

    void SetConfig(string key, string value)
    {
        var entry = Plugin.Instance.Config.Keys.Where(k => k.Key == key).Select(k => Plugin.Instance.Config[k]).FirstOrDefault();
        if (entry == null)
        {
            Log("no config key " + key);
            return;
        }
        entry.BoxedValue = BepInEx.Configuration.TomlTypeConverter.ConvertToValue(value, entry.SettingType);
        Log($"{key} = {entry.BoxedValue}");
    }

    void State()
    {
        var sb = new StringBuilder();
        var atlas = Journal.Current;
        sb.Append($"known={atlas?.KnownCount} favorites={Journal.favorites.Count} sort={Plugin.SortBy.Value} view={Plugin.View.Value} lastMap={Journal.LastViewMap} ignored={Journal.ignored.Count} lang={Texts.Language} gamepad={ZInput.IsGamepadActive()} padActive={(global::ClientPortals.Pad.Active)} gameLang={Localization.instance.GetSelectedLanguage()}");
        sb.Append($" | list open={ListWindow.IsOpen} tab={ListWindow.tab} selected={ListWindow.selected} rows={ListWindow.rows.Count} search='{ListWindow.search}'");
        if (ListWindow.IsOpen)
            sb.Append(" [" + string.Join(", ", ListWindow.rows.Take(12).Select((r, i) => (i == ListWindow.selected ? "*" : "") + Destinations.DisplayName(r.Name))) + "]");
        sb.Append($" | map open={MapPicker.IsOpen} mode={Minimap.instance?.m_mode} hovered={(MapPicker.hovered >= 0 && MapPicker.hovered < MapPicker.markers.Count ? Destinations.DisplayName(MapPicker.markers[MapPicker.hovered].Entry?.Name) : "-")} markers={MapPicker.markers.Count(m => m.Visible)}");
        sb.Append($" | blocks={ListWindow.BlocksInput}/{MapPicker.BlocksInput} textInputVisible={TextInput.IsVisible()} travelBusy={Travel.Busy}");
        var p = Player.m_localPlayer;
        if (p) sb.Append($" | right hand={p.GetCurrentWeapon()?.m_shared.m_name} equipped={string.Join(",", p.GetInventory().GetEquippedItems().Select(i => i.m_shared.m_name))} autorun={p.m_autoRun} crouching={p.IsCrouching()} running={p.IsRunning()}");
        Log(sb.ToString());
    }

    /// <summary>Every visible piece of the mod's windows: name, screen rect, text. To check the layout without looking.</summary>
    void DumpUi()
    {
        var pub = Minimap.instance && Minimap.instance.m_publicPosition ? Minimap.instance.m_publicPosition.transform.parent.gameObject : null;
        foreach (var root in new[] { ListWindow.root, MapPicker.bar ? MapPicker.bar.gameObject : null, MapPicker.label ? MapPicker.label.gameObject : null, pub })
        {
            if (!root || !root.activeInHierarchy) continue;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>())
            {
                if (!rt.gameObject.activeInHierarchy) continue;
                var corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                if (corners[2].y < 0 || corners[0].y > Screen.height) continue; // scrolled out of view
                var text = rt.GetComponent<TMP_Text>();
                var image = rt.GetComponent<Image>();
                string path = rt == root.transform ? rt.name : rt.parent.name + "/" + rt.name;
                Log($"ui {path} ({corners[0].x:0},{corners[0].y:0})-({corners[2].x:0},{corners[2].y:0})" +
                    (image ? $" img={image.sprite?.name ?? "-"}/{image.type}" : "") +
                    (text ? $" text='{text.text.Replace("\n", "\\n")}' size={text.fontSize} font={text.font?.name} overflow={text.isTextOverflowing} width={text.preferredWidth:0}/{rt.rect.width:0}" : "") +
                    (rt.GetComponent<Selectable>() is { } s ? $" interactable={s.interactable}" : ""));
            }
        }
    }

    void Click(string name)
    {
        var root = ListWindow.IsOpen ? ListWindow.root : MapPicker.bar ? MapPicker.bar.gameObject : null;
        var button = root ? root.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == name) : null;
        if (button) button.onClick.Invoke();
        var toggle = !button && root ? root.GetComponentsInChildren<Toggle>().FirstOrDefault(t => t.name == name) : null;
        if (toggle) toggle.isOn = !toggle.isOn;
        Log(button ? "clicked " + name : toggle ? $"toggled {name}: {toggle.isOn}" : "no button " + name);
    }

    // ------------------------------------------------------------------ input

    Gamepad VirtualPad()
    {
        if (pad == null || !pad.added) pad = InputSystem.AddDevice<Gamepad>("ClientPortalsHarnessPad");
        return pad;
    }

    static readonly Dictionary<string, GamepadButton> Buttons = new()
    {
        ["a"] = GamepadButton.South, ["b"] = GamepadButton.East, ["x"] = GamepadButton.West, ["y"] = GamepadButton.North,
        ["lb"] = GamepadButton.LeftShoulder, ["rb"] = GamepadButton.RightShoulder,
        ["up"] = GamepadButton.DpadUp, ["down"] = GamepadButton.DpadDown, ["left"] = GamepadButton.DpadLeft, ["right"] = GamepadButton.DpadRight,
        ["select"] = GamepadButton.Select, ["start"] = GamepadButton.Start, ["ls"] = GamepadButton.LeftStick, ["rs"] = GamepadButton.RightStick,
    };

    /// <summary>pad &lt;button[+button]&gt; [frames]: presses gamepad buttons (lt/rt are the triggers) for some frames, then releases them.</summary>
    IEnumerator Press(string spec, int frames)
    {
        var device = VirtualPad();
        var state = new GamepadState();
        foreach (var b in spec.ToLowerInvariant().Split('+'))
        {
            if (b == "lt") state.leftTrigger = 1f;
            else if (b == "rt") state.rightTrigger = 1f;
            else if (Buttons.TryGetValue(b, out var button)) state = state.WithButton(button);
            else Log("unknown button " + b);
        }
        InputSystem.QueueStateEvent(device, state);
        for (int i = 0; i < frames; i++) yield return null;
        InputSystem.QueueStateEvent(device, new GamepadState());
        for (int i = 0; i < 4; i++) yield return null;
    }

    IEnumerator Stick(float x, float y, float seconds)
    {
        var device = VirtualPad();
        InputSystem.QueueStateEvent(device, new GamepadState { leftStick = new Vector2(x, y) });
        yield return new WaitForSeconds(seconds);
        InputSystem.QueueStateEvent(device, new GamepadState());
        for (int i = 0; i < 4; i++) yield return null;
    }

    /// <summary>key &lt;Key[+Key]&gt;: a keyboard press through the keyboard device (Key names of the Input System: Escape, Tab, M, Enter, DownArrow...).</summary>
    IEnumerator Key(string spec)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            Log("no keyboard");
            yield break;
        }
        var keys = spec.Split('+').Select(k => (UnityEngine.InputSystem.Key)Enum.Parse(typeof(UnityEngine.InputSystem.Key), k, true)).ToArray();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        for (int i = 0; i < 3; i++) yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        for (int i = 0; i < 4; i++) yield return null;
    }

    IEnumerator Shot(string name)
    {
        for (int i = 0; i < 6; i++) yield return null;
        yield return new WaitForEndOfFrame();
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        var path = Path.Combine(dir, "shots", name + ".png");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Destroy(tex);
        Log("shot " + path);
    }
}
