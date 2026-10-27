using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ClientPortals;

[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin
{
    public const string Guid = "rgeraskin.ClientPortals";
    public const string Name = "ClientPortals";
    public const string Version = "0.1.0";

    public static Plugin Instance { get; private set; }

    // General
    public static ConfigEntry<string> Language;
    // Learning
    public static ConfigEntry<bool> Nearby;
    public static ConfigEntry<float> NearbyDistance;
    public static ConfigEntry<bool> TagPairs;
    public static ConfigEntry<bool> CartographyTable;
    public static ConfigEntry<bool> HostKnowsAll;
    public static ConfigEntry<bool> ForgetMissing;
    // List
    public static ConfigEntry<bool> HideNearby;
    public static ConfigEntry<float> HideNearbyDistance;
    public static ConfigEntry<SortMode> SortBy;
    public static ConfigEntry<bool> FavoritesFirst;
    public static ConfigEntry<bool> HideTwins;
    // Portal
    public static ConfigEntry<bool> ShowActive;
    public static ConfigEntry<bool> WalkIn;
    public static ConfigEntry<PickerView> View;
    public static ConfigEntry<KeyboardShortcut> TravelKey;
    // Rename
    public static ConfigEntry<bool> RenameBoth;
    public static ConfigEntry<KeyboardShortcut> RenameKey;
    public static ConfigEntry<int> NameLimit;
    // Map
    public static ConfigEntry<bool> OnlyPortals;
    public static ConfigEntry<bool> ShowNames;

    void Awake()
    {
        Instance = this;

        var languages = new string[Texts.GameLanguages.Length + 1];
        languages[0] = Texts.Auto;
        Texts.GameLanguages.CopyTo(languages, 1);
        Language = Config.Bind("General", "Language", Texts.Auto,
            new ConfigDescription("Language of the mod's texts. Auto: the game's language", new AcceptableValueList<string>(languages)));

        Nearby = Config.Bind("Learning", "Nearby", true, "Learn a portal when you get close to it");
        NearbyDistance = Config.Bind("Learning", "NearbyDistance", 20f,
            new ConfigDescription("How close (in meters) you need to get to a portal to learn it", new AcceptableValueRange<float>(5f, 100f)));
        TagPairs = Config.Bind("Learning", "TagPairs", true,
            "When you get close to a portal paired by tag (vanilla), you also learn the portal on the other side");
        CartographyTable = Config.Bind("Learning", "CartographyTable", true,
            "Reading a cartography table teaches you the portals recorded on it, and writing records the portals you know");
        HostKnowsAll = Config.Bind("Learning", "HostKnowsAll", true,
            "Single player or hosting: every portal in the world is already known. Set to false to learn portals the same way as on a dedicated server");
        ForgetMissing = Config.Bind("Learning", "ForgetMissing", true,
            "When you get close to where a known portal was and it is gone, it is removed from your list");

        HideNearby = Config.Bind("List", "HideNearby", false, "Hide portals close to the portal you are using (on the list and on the map)");
        HideNearbyDistance = Config.Bind("List", "HideNearbyDistance", 30f,
            new ConfigDescription("Portals closer than this (in meters) to the portal you are using are hidden when HideNearby is on",
                new AcceptableValueRange<float>(5f, 500f)));
        SortBy = Config.Bind("List", "SortBy", SortMode.Name,
            "How the list is sorted: Name, Nearest (first) or Farthest (first). Can also be changed in the list window");
        FavoritesFirst = Config.Bind("List", "FavoritesFirst", true, "Favorite portals always come first in the list");
        HideTwins = Config.Bind("List", "HideTwins", true,
            "Of portals with the same name, show only the one farthest from the portal you are using (on the list and on the map): " +
            "in a portal room, the near one is the room's end of a tag pair");

        ShowActive = Config.Bind("Portal", "ShowActive", true,
            "Unpaired portals light up (color and swirl) when you know other destinations, like a paired portal. Visual only, on your game");
        WalkIn = Config.Bind("Portal", "WalkIn", true,
            "Walking into an unpaired portal opens the destination list. Tag-paired portals keep the vanilla behavior");
        View = Config.Bind("Portal", "View", PickerView.Last,
            "What a portal opens: the List, the Map view (known portals drawn on the map: click one to travel, right-click to mark a favorite), " +
            "or the Last view you used. Switch views from the window (the map key)");
        TravelKey = Config.Bind("Portal", "TravelKey", new KeyboardShortcut(KeyCode.Q),
            "The key that opens the destinations at a portal you look at (with a gamepad: the left stick press). " +
            "While held at a portal, it doesn't do its game action (Q: run on its own)");

        RenameBoth = Config.Bind("Rename", "RenameBoth", false,
            "Rename both ends of a tag-paired (vanilla) portal at once, so the pair stays connected: RenameKey " +
            "(or the gamepad's alt + use) at the portal. Off while the TagConnectedPortals mod is installed");
        RenameKey = Config.Bind("Rename", "RenameKey", new KeyboardShortcut(KeyCode.E, KeyCode.LeftShift),
            "The key that renames both ends of a tag-paired portal you look at (with RenameBoth)");
        NameLimit = Config.Bind("Rename", "NameLimit", 15,
            new ConfigDescription("Most characters of a portal name when renaming (the game allows 10): both ends, one portal in the list, and the game's own rename at a portal", new AcceptableValueRange<int>(10, 100)));

        OnlyPortals = Config.Bind("Map", "OnlyPortals", false,
            "The map view shows only the portals, not the map's other pins. Also switched in the map view");
        ShowNames = Config.Bind("Map", "ShowNames", true,
            "The map view writes each portal's name under it (where the names don't overlap), not only on hover. Also switched in the map view");

        PortalLabels.Bind(Config);

        new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
    }

    // every plugin has loaded by now
    void Start() => Compat.Log();

    void Update()
    {
        Learner.Tick();
        ListWindow.Update();
        MapPicker.Update();
        PortalLabels.Update();
        global::ClientPortals.TravelKey.Update();
        PairRename.UpdateKey();
    }

    internal static void Message(string text, MessageHud.MessageType type = MessageHud.MessageType.Center)
    {
        if (Player.m_localPlayer) Player.m_localPlayer.Message(type, text);
    }
}
