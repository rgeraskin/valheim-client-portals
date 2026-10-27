using System.Collections.Generic;
using UnityEngine;

namespace ClientPortals;

/// <summary>Portal objects as the game's network layer holds them.</summary>
internal static class PortalWorld
{
    /// <summary>The id each portal has in this session, so travel can ask the server for a portal that isn't loaded.</summary>
    static readonly Dictionary<PortalPos, ZDOID> sessionIds = new();
    static ZDOMan sessionOf;

    public static bool IsPortal(ZDO zdo) =>
        zdo != null && zdo.IsValid() && Game.instance && Game.instance.PortalPrefabHash.Contains(zdo.GetPrefab());

    public static string Tag(ZDO zdo) => zdo.GetString(ZDOVars.s_tag);

    /// <summary>The portal objects this game currently has: all of them on a server or single player, the nearby ones on a client.</summary>
    public static List<ZDO> Loaded() => ZDOMan.instance != null ? ZDOMan.instance.GetPortalList() : new List<ZDO>();

    public static void Remember(ZDO zdo)
    {
        if (sessionOf != ZDOMan.instance)
        {
            sessionIds.Clear();
            sessionOf = ZDOMan.instance;
        }
        sessionIds[PortalPos.Of(zdo.GetPosition())] = zdo.m_uid;
    }

    public static bool TryGetSessionId(PortalPos key, out ZDOID id)
    {
        id = ZDOID.None;
        return sessionOf == ZDOMan.instance && sessionIds.TryGetValue(key, out id);
    }

    /// <summary>The portal object at <paramref name="key"/> if this game has it loaded.</summary>
    public static ZDO Find(PortalPos key)
    {
        if (ZDOMan.instance == null) return null;
        if (TryGetSessionId(key, out var id))
        {
            var zdo = ZDOMan.instance.GetZDO(id);
            if (IsPortal(zdo) && PortalPos.Of(zdo.GetPosition()) == key) return zdo;
        }
        foreach (var zdo in Loaded())
        {
            if (IsPortal(zdo) && PortalPos.Of(zdo.GetPosition()) == key)
            {
                Remember(zdo);
                return zdo;
            }
        }
        return null;
    }

    public static bool IsPaired(TeleportWorld portal)
    {
        var zdo = portal && portal.m_nview ? portal.m_nview.GetZDO() : null;
        return zdo != null && !zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal).IsNone();
    }

    public static Vector3 PositionOf(TeleportWorld portal)
    {
        var zdo = portal.m_nview ? portal.m_nview.GetZDO() : null;
        return zdo != null ? zdo.GetPosition() : portal.transform.position;
    }

    /// <summary>Whether the player knows somewhere to go from this portal (what lights it up with <see cref="Plugin.ShowActive"/>).</summary>
    public static bool HasDestinations(Vector3 from)
    {
        var atlas = Journal.Current;
        if (atlas == null) return false;
        bool hide = Plugin.HideNearby.Value;
        float hideDistance = Plugin.HideNearbyDistance.Value;
        foreach (var e in atlas.Known)
        {
            float d = Vector3.Distance(from, e.Position);
            if (d >= Destinations.SamePortal && (!hide || d >= hideDistance) && !Journal.IsIgnored(e)) return true;
        }
        return false;
    }

    /// <summary>The destination list for a portal, with the user's options.</summary>
    public static List<PortalEntry> DestinationsFrom(Vector3 from, ListTab tab, string search, out int hidden)
    {
        hidden = 0;
        var atlas = Journal.Current;
        var player = Player.m_localPlayer;
        if (atlas == null || !player) return new List<PortalEntry>();
        var options = new ListOptions
        {
            Sort = Plugin.SortBy.Value,
            Tab = tab,
            FavoritesFirst = Plugin.FavoritesFirst.Value,
            HideNearby = Plugin.HideNearby.Value,
            HideNearbyDistance = Plugin.HideNearbyDistance.Value,
            HideTwins = Plugin.HideTwins.Value,
            Search = search,
        };
        return Destinations.List(atlas.Known, from, player.transform.position, options, Journal.IsFavorite, Journal.IsIgnored, out hidden);
    }
}
