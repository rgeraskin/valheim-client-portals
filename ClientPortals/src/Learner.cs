using System.Collections.Generic;
using UnityEngine;

namespace ClientPortals;

/// <summary>Learns portals around the player (and through tag pairs), and notices known portals that are gone.</summary>
internal static class Learner
{
    const float Interval = 1f;
    /// <summary>How long a known portal must be missing from a loaded area before it is forgotten (its object may still be on its way from the server).</summary>
    const float MissingFor = 5f;
    const float PairRequestInterval = 5f;

    static float next;
    static readonly Dictionary<PortalPos, float> missingSince = new();
    static readonly Dictionary<ZDOID, float> pairRequested = new();
    static readonly HashSet<PortalPos> present = new();

    public static void Tick()
    {
        if (Time.time < next) return;
        next = Time.time + Interval;

        var player = Player.m_localPlayer;
        var atlas = Journal.Current;
        if (!player || atlas == null || !ZNet.instance || player.IsTeleporting()) return;

        Vector3 here = player.transform.position;
        float nearby = Plugin.NearbyDistance.Value;
        bool knowAll = ZNet.instance.IsServer() && Plugin.HostKnowsAll.Value;

        present.Clear();
        foreach (var zdo in PortalWorld.Loaded())
        {
            if (!PortalWorld.IsPortal(zdo)) continue;
            Vector3 pos = zdo.GetPosition();
            present.Add(PortalPos.Of(pos));
            bool near = Utils.DistanceXZ(here, pos) <= nearby;

            if (knowAll) Journal.Learn(zdo, announce: false);
            else if (near && Plugin.Nearby.Value) Journal.Learn(zdo, announce: true);
            else Journal.Refresh(zdo);

            if (near && Plugin.TagPairs.Value) LearnPair(zdo);
        }

        if (Plugin.ForgetMissing.Value) CheckMissing(atlas, here, nearby);
    }

    static void LearnPair(ZDO portal)
    {
        var id = portal.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
        if (id.IsNone()) return;
        // asked for now and then even when this game has it: the server only sends changes of objects near a player,
        // so a far end fetched earlier keeps its old name here (seen with two clients: a pair renamed by another player)
        if (!pairRequested.TryGetValue(id, out float at) || Time.time - at > PairRequestInterval)
        {
            pairRequested[id] = Time.time;
            ZDOMan.instance.RequestZDO(id);
        }
        var other = ZDOMan.instance.GetZDO(id);
        if (other != null) Journal.Learn(other, announce: true);
    }

    static void CheckMissing(Atlas atlas, Vector3 here, float nearby)
    {
        List<PortalEntry> gone = null;
        foreach (var e in atlas.Known)
        {
            var key = e.Key;
            if (present.Contains(key) || Utils.DistanceXZ(here, e.Position) > nearby || !ZoneSystem.instance.IsZoneLoaded(e.Position))
            {
                missingSince.Remove(key);
                continue;
            }
            if (!missingSince.TryGetValue(key, out float since))
            {
                missingSince[key] = Time.time;
                continue;
            }
            if (Time.time - since >= MissingFor) (gone ??= new List<PortalEntry>()).Add(e);
        }
        if (gone == null) return;
        foreach (var e in gone)
        {
            missingSince.Remove(e.Key);
            if (Journal.Forget(e.Key))
                Plugin.Message(Texts.T("gone", Destinations.DisplayName(e.Name, Texts.T("unnamed"))), MessageHud.MessageType.TopLeft);
        }
    }
}
