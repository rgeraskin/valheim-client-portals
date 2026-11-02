using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ClientPortals;

/// <summary>
/// The local player's known portals, favorites and ignored portals for the current world, saved with the character
/// (<see cref="Player.m_customData"/>), one entry per world.
/// </summary>
internal static class Journal
{
    const string AtlasKeyPrefix = "ClientPortals.Atlas.";
    const string FavoritesKeyPrefix = "ClientPortals.Favorites.";
    const string IgnoredKeyPrefix = "ClientPortals.Ignored.";
    const string IgnoreUnnamedKeyPrefix = "ClientPortals.IgnoreUnnamed.";
    const string LastViewKey = "ClientPortals.LastView";
    /// <summary>Removal markers live this long (game time), long enough to spread through the tables.</summary>
    static readonly long RemovedLifetime = TimeSpan.FromDays(60).Ticks;

    static Player owner;
    static long world;
    static Atlas atlas;
    static readonly HashSet<PortalPos> favorites = new();
    static readonly HashSet<PortalPos> ignored = new();
    /// <summary>Unnamed portals all share one (empty) name: they are ignored together.</summary>
    static bool ignoreUnnamed;

    /// <summary>The atlas of the local player in the current world, or null outside a game.</summary>
    public static Atlas Current => Ensure() ? atlas : null;

    public static long Now => ZNet.instance ? ZNet.instance.GetTime().Ticks : DateTime.UtcNow.Ticks;

    static bool Ensure()
    {
        var player = Player.m_localPlayer;
        if (!player || !ZNet.instance) return false;
        long uid = ZNet.instance.GetWorldUID();
        if (player != owner || uid != world || atlas == null) Load(player, uid);
        return true;
    }

    static void Load(Player player, long uid)
    {
        owner = player;
        world = uid;
        favorites.Clear();
        ignored.Clear();
        ignoreUnnamed = false;
        var data = player.m_customData;
        if (data.TryGetValue(AtlasKeyPrefix + uid, out var saved))
        {
            atlas = Atlas.Deserialize(FromBase64(saved));
            if (data.TryGetValue(FavoritesKeyPrefix + uid, out var favs)) ReadSet(FromBase64(favs), favorites);
            if (data.TryGetValue(IgnoredKeyPrefix + uid, out var ign)) ReadSet(FromBase64(ign), ignored);
            ignoreUnnamed = data.TryGetValue(IgnoreUnnamedKeyPrefix + uid, out var unnamed) && unnamed == "1";
        }
        else atlas = new Atlas();
        atlas.PruneRemoved(Now - RemovedLifetime);
    }

    public static void Save()
    {
        if (!owner || atlas == null) return;
        owner.m_customData[AtlasKeyPrefix + world] = Convert.ToBase64String(atlas.Serialize());
        owner.m_customData[FavoritesKeyPrefix + world] = Convert.ToBase64String(WriteSet(favorites));
        owner.m_customData[IgnoredKeyPrefix + world] = Convert.ToBase64String(WriteSet(ignored));
        if (ignoreUnnamed) owner.m_customData[IgnoreUnnamedKeyPrefix + world] = "1";
        else owner.m_customData.Remove(IgnoreUnnamedKeyPrefix + world);
    }

    /// <summary>Records a portal object; tells the player when it is a new one and <paramref name="announce"/> is set.</summary>
    public static bool Learn(ZDO zdo, bool announce)
    {
        var a = Current;
        if (a == null || !PortalWorld.IsPortal(zdo)) return false;
        PortalWorld.Remember(zdo);
        var key = PortalPos.Of(zdo.GetPosition());
        string name = PortalWorld.Tag(zdo);
        bool changed = !a.TryGet(key, out var before) || before.Removed || before.Name != name;
        bool learned = a.Observe(zdo.GetPosition(), zdo.GetRotation(), name, Now);
        if (changed || learned) Save();
        if (learned && announce)
            Plugin.Message(Texts.T("new", Destinations.DisplayName(name, Texts.T("unnamed"))), MessageHud.MessageType.TopLeft);
        return learned;
    }

    /// <summary>Updates a portal the player already knows (renamed), without learning unknown ones.</summary>
    public static void Refresh(ZDO zdo)
    {
        var a = Current;
        if (a == null) return;
        if (a.Knows(PortalPos.Of(zdo.GetPosition()))) Learn(zdo, announce: false);
    }

    public static bool Forget(PortalPos key)
    {
        var a = Current;
        if (a == null || !a.Forget(key, Now)) return false;
        Save();
        return true;
    }

    /// <summary>
    /// A known portal turned up elsewhere (moved, or rebuilt nearby under its name): the old place is forgotten, the new
    /// one learned, and it stays a favorite or ignored.
    /// </summary>
    public static void Moved(PortalPos from, ZDO now)
    {
        if (!Ensure()) return;
        var to = PortalPos.Of(now.GetPosition());
        if (to == from) return;
        if (favorites.Remove(from)) favorites.Add(to);
        if (ignored.Remove(from)) ignored.Add(to);
        atlas.Forget(from, Now);
        Learn(now, announce: false);
        Save();
    }

    /// <summary>Takes in what a cartography table holds. Returns how many portals were learned.</summary>
    public static int Merge(IEnumerable<PortalEntry> entries)
    {
        var a = Current;
        if (a == null) return 0;
        int learned = a.Merge(entries);
        Save();
        return learned;
    }

    public static bool IsFavorite(PortalPos key) => Ensure() && favorites.Contains(key);

    public static void ToggleFavorite(PortalPos key)
    {
        if (!Ensure()) return;
        if (!favorites.Remove(key)) favorites.Add(key);
        Save();
    }

    /// <summary>Ignored portals stay off the list and the map (they have their own tab), like a portal room's ends.</summary>
    public static bool IsIgnored(PortalEntry e) => Ensure() && (ignored.Contains(e.Key) || ignoreUnnamed && Destinations.IsUnnamed(e));

    /// <summary>
    /// Ignores a portal, or stops ignoring it; an unnamed one takes every unnamed portal with it. Returns whether it is
    /// ignored now.
    /// </summary>
    public static bool ToggleIgnored(PortalEntry e)
    {
        if (!Ensure()) return false;
        bool now = !IsIgnored(e);
        if (Destinations.IsUnnamed(e))
        {
            ignoreUnnamed = now;
            // unnamed portals ignored one by one (before they went together) come back together too
            if (!now)
                foreach (var other in atlas.Known)
                    if (Destinations.IsUnnamed(other)) ignored.Remove(other.Key);
        }
        else if (now) ignored.Add(e.Key);
        else ignored.Remove(e.Key);
        Save();
        return now;
    }

    /// <summary>Whether the last view used was the map (for <see cref="PickerView.Last"/>), saved with the character.</summary>
    public static bool LastViewMap
    {
        get => Player.m_localPlayer && Player.m_localPlayer.m_customData.TryGetValue(LastViewKey, out var v) && v == "Map";
        set
        {
            if (Player.m_localPlayer) Player.m_localPlayer.m_customData[LastViewKey] = value ? "Map" : "List";
        }
    }

    static byte[] WriteSet(HashSet<PortalPos> set)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(1);
        w.Write(set.Count);
        foreach (var f in set) { w.Write(f.X); w.Write(f.Y); w.Write(f.Z); }
        w.Flush();
        return ms.ToArray();
    }

    static void ReadSet(byte[] data, HashSet<PortalPos> into)
    {
        if (data == null || data.Length < 8) return;
        try
        {
            using var r = new BinaryReader(new MemoryStream(data));
            if (r.ReadInt32() != 1) return;
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++) into.Add(new PortalPos(r.ReadInt32(), r.ReadInt32(), r.ReadInt32()));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[{Plugin.Name}] could not read a portal set: {e.Message}");
        }
    }

    static byte[] FromBase64(string s)
    {
        try { return Convert.FromBase64String(s); }
        catch (FormatException) { return null; }
    }
}
