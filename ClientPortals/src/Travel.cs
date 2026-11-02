using System.Collections;
using HarmonyLib;
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

    /// <summary>
    /// A portal rebuilt within this distance of its old place under the same name counts as moved: the trip goes on to it.
    /// </summary>
    const float MovedRadius = 50f;
    /// <summary>How long the loading screen waits for the destination portal's object after the area is ready.</summary>
    const float HoldFor = 4f;
    /// <summary>
    /// The least time from the start of the trip before a portal counts as gone: vanilla's 8 s and the hold. Mods that
    /// end teleports early (QuickTeleport) call the area ready before the server has sent every object in it.
    /// </summary>
    const float GoneAfter = 8f + HoldFor;

    static float arrivedAt = -100f;

    /// <summary>The trip in progress, checked at the destination while the loading screen is still up.</summary>
    sealed class Trip
    {
        public PortalPos Key;
        public string Name;
        public Vector3 Position;
        public float ExitDistance;
        public Vector3 HomeExit;
        public Quaternion HomeRotation;
        public float Started;
        public float HoldUntil = -1f;
        public bool Checked;
        public bool Returned;
    }

    static Trip trip;

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
            var zdo = PortalWorld.Find(target) ?? PortalWorld.BySessionId(target);
            if (zdo == null && PortalWorld.TryGetSessionId(target, out var id))
            {
                ZDOMan.instance.RequestZDO(id);
                float until = Time.time + FetchTimeout;
                while (zdo == null && Time.time < until)
                {
                    yield return null;
                    zdo = PortalWorld.Find(target) ?? PortalWorld.BySessionId(target);
                }
            }
            if (zdo != null && PortalPos.Of(zdo.GetPosition()) != target)
            {
                // the same object somewhere else: a mod moved it
                Journal.Moved(target, zdo);
                target = PortalPos.Of(zdo.GetPosition());
            }
            Vector3 pos = zdo != null ? zdo.GetPosition() : entry.Position;
            Quaternion rot = zdo != null ? zdo.GetRotation() : entry.Rotation;

            player = Player.m_localPlayer;
            if (!player || !from || !Allowed(player, from)) yield break;
            Quaternion home = from.transform.rotation;
            trip = new Trip
            {
                Key = target,
                Name = entry.Name,
                Position = pos,
                ExitDistance = from.m_exitDistance,
                HomeExit = Exit(PortalWorld.PositionOf(from), home, from.m_exitDistance),
                HomeRotation = home,
                Started = Time.time,
            };
            if (!player.TeleportTo(Exit(pos, rot, from.m_exitDistance), rot, distantTeleport: true)) yield break;
            Game.instance.IncrementPlayerStat(PlayerStatType.PortalsUsed);

            float deadline = Time.time + ArrivalTimeout;
            yield return null;
            while (Player.m_localPlayer == player && player.IsTeleporting() && Time.time < deadline) yield return null;
            if (Player.m_localPlayer != player || player.IsDead()) yield break;
            arrivedAt = Time.time;
            // checked under the loading screen: back home from a portal that is gone, or on to where it moved
            if (trip.Returned)
            {
                yield return LoadingScreenGone();
                Plugin.Message(Texts.T("returned", Destinations.DisplayName(trip.Name, Texts.T("unnamed"))));
                yield break;
            }
            target = trip.Key;

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
                yield return LoadingScreenGone();
                Plugin.Message(Texts.T("missing"));
                yield break;
            }
            Journal.Learn(arrived, announce: false);
        }
        finally
        {
            Busy = false;
            trip = null;
        }
    }

    /// <summary>
    /// Waits for the loading screen to fade out (at most a few seconds): the game's center message fades after 4 s,
    /// so one shown under the loading screen is gone before the player can read it.
    /// </summary>
    static IEnumerator LoadingScreenGone()
    {
        float until = Time.time + 5f;
        while (Hud.instance && Hud.instance.m_loadingScreen.gameObject.activeSelf && Time.time < until) yield return null;
    }

    static Vector3 Exit(Vector3 portal, Quaternion rotation, float distance) => portal + rotation * Vector3.forward * distance + Vector3.up;

    /// <summary>
    /// Runs before the game's teleport step while the loading screen is up. The game ends a distant teleport once 8 s
    /// have passed and the area is loaded: by then the destination's objects have come from the server, so the
    /// destination portal is checked here, before the player shows up. A portal rebuilt nearby under its name takes
    /// the trip; with none, the loading screen waits a little longer for the object, then takes the player back to the
    /// portal they left from, and the portal is forgotten.
    /// </summary>
    internal static void CheckDestination(Player player, float dt)
    {
        var t = trip;
        if (t == null || t.Checked || player != Player.m_localPlayer || !player.m_teleporting || !player.m_distantTeleport) return;
        if (player.m_teleportTimer + dt <= 8f || !ZNetScene.instance || !ZNetScene.instance.IsAreaReady(player.m_teleportTargetPos)) return;

        if (PortalWorld.Find(t.Key) != null)
        {
            t.Checked = true;
            return;
        }
        var moved = PortalWorld.NearestNamed(t.Name, t.Position, MovedRadius);
        if (moved != null)
        {
            t.Checked = true;
            Journal.Moved(t.Key, moved);
            t.Key = PortalPos.Of(moved.GetPosition());
            player.m_teleportTargetPos = Exit(moved.GetPosition(), moved.GetRotation(), t.ExitDistance);
            player.m_teleportTargetRot = moved.GetRotation();
            return;
        }
        if (t.HoldUntil < 0f) t.HoldUntil = Mathf.Max(Time.time + HoldFor, t.Started + GoneAfter);
        if (Time.time < t.HoldUntil)
        {
            // keeps the game from ending the teleport this frame (this prefix runs last: a mod that sets the timer
            // every frame, like QuickTeleport, has already done so)
            player.m_teleportTimer = 6f;
            return;
        }

        t.Checked = true;
        t.Returned = true;
        // the message waits for the arrival home (Run): the loading screen would hide it
        Journal.Forget(t.Key);
        player.m_teleportTargetPos = t.HomeExit;
        player.m_teleportTargetRot = t.HomeRotation;
        // the game waits for the home area to load again, at least 2 s more
        player.m_teleportTimer = 6f;
    }

    /// <summary>
    /// The server tells every client about every destroyed object, near or far: a known portal destroyed anywhere in this
    /// session leaves the list at once, not only when someone with the mod passes by.
    /// </summary>
    internal static void Destroyed(ZDOID id)
    {
        if (!PortalWorld.TryGetKey(id, out var key)) return;
        var atlas = Journal.Current;
        if (atlas == null || !atlas.TryGet(key, out var e) || e.Removed) return;
        // a portal object that was moved stands elsewhere now; only the place the id stood for is gone
        var zdo = ZDOMan.instance.GetZDO(id);
        if (zdo != null && PortalPos.Of(zdo.GetPosition()) != key) return;
        if (Journal.Forget(key))
            Plugin.Message(Texts.T("gone", Destinations.DisplayName(e.Name, Texts.T("unnamed"))), MessageHud.MessageType.TopLeft);
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.UpdateTeleport))]
static class TravelCheckPatch
{
    // after other prefixes: teleport-speed mods rewrite m_teleportTimer (and dt) in theirs
    [HarmonyPriority(Priority.Last)]
    static void Prefix(Player __instance, float dt) => Travel.CheckDestination(__instance, dt);
}

[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.HandleDestroyedZDO))]
static class PortalDestroyedPatch
{
    static void Prefix(ZDOID uid) => Travel.Destroyed(uid);
}
