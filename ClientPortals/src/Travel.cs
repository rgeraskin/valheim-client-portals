using System.Collections;
using UnityEngine;

namespace ClientPortals;

/// <summary>Teleports the local player from a portal to a known portal, with the game's rules.</summary>
internal static class Travel
{
    const float FetchTimeout = 6f;
    const float ArrivalTimeout = 60f;
    const float FindTimeout = 6f;
    /// <summary>After arriving, the portal one stands in front of doesn't send the player on (through its tag pair, or by walking in).</summary>
    const float ArrivalGrace = 2.5f;

    static float arrivedAt = -100f;

    public static bool Busy { get; private set; }
    public static bool JustArrived => Time.time - arrivedAt < ArrivalGrace;

    public static void Go(TeleportWorld from, PortalEntry target)
    {
        if (Busy || !from || target == null) return;
        Plugin.Instance.StartCoroutine(Run(from, target.Key));
    }

    /// <summary>The game's portal rules (world modifiers, boss fights, restricted items), with its messages.</summary>
    public static bool Allowed(Player player, TeleportWorld from)
    {
        if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_blocked");
            return false;
        }
        if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBossPortals) &&
            (RandEventSystem.instance.GetBossEvent() != null || (ZoneSystem.instance.GetGlobalKey(GlobalKeys.activeBosses, out float bosses) && bosses > 0f)))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_blockedbyboss");
            return false;
        }
        if (!player.IsTeleportable(from.m_allowAllItems))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
            return false;
        }
        return true;
    }

    static IEnumerator Run(TeleportWorld from, PortalPos target)
    {
        var player = Player.m_localPlayer;
        var atlas = Journal.Current;
        if (!player || atlas == null || !atlas.TryGet(target, out var entry) || entry.Removed || !Allowed(player, from)) yield break;

        Busy = true;
        try
        {
            // The portal as the server has it now; a client only has nearby objects, so ask for it when its id is known.
            var zdo = PortalWorld.Find(target);
            if (zdo == null && PortalWorld.TryGetSessionId(target, out var id))
            {
                ZDOMan.instance.RequestZDO(id);
                float until = Time.time + FetchTimeout;
                while (zdo == null && Time.time < until)
                {
                    yield return null;
                    zdo = PortalWorld.Find(target);
                }
            }
            Vector3 pos = zdo != null ? zdo.GetPosition() : entry.Position;
            Quaternion rot = zdo != null ? zdo.GetRotation() : entry.Rotation;

            player = Player.m_localPlayer;
            if (!player || !from || !Allowed(player, from)) yield break;
            Vector3 exit = pos + rot * Vector3.forward * from.m_exitDistance + Vector3.up;
            if (!player.TeleportTo(exit, rot, distantTeleport: true)) yield break;
            Game.instance.IncrementPlayerStat(PlayerStatType.PortalsUsed);

            float deadline = Time.time + ArrivalTimeout;
            yield return null;
            while (Player.m_localPlayer == player && player.IsTeleporting() && Time.time < deadline) yield return null;
            if (Player.m_localPlayer != player || player.IsDead()) yield break;
            arrivedAt = Time.time;

            // Check the portal is still there; if it was destroyed meanwhile, say so and forget it.
            float findUntil = Time.time + FindTimeout;
            ZDO arrived = null;
            while (arrived == null && Time.time < findUntil)
            {
                arrived = PortalWorld.Find(target);
                if (arrived == null) yield return null;
            }
            arrivedAt = Time.time;
            if (arrived == null)
            {
                Journal.Forget(target);
                Plugin.Message(Texts.T("missing"));
                yield break;
            }
            Journal.Learn(arrived, announce: false);
        }
        finally
        {
            Busy = false;
        }
    }
}
