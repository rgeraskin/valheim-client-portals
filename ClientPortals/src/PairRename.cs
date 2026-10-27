using System.Collections;
using Splatform;
using UnityEngine;

namespace ClientPortals;

/// <summary>
/// Renames both ends of a tag-paired portal at once. Renaming one end in the game's dialog breaks the pair until the
/// other end gets the same name; here the portal is renamed the game's way and the other end's object gets the same
/// tag and author, so the server pairs them again (it checks every 5 s). Client-side: any player may set a portal tag.
/// </summary>
internal static class PairRename
{
    const float FetchTimeout = 5f;

    public static bool Enabled => Plugin.RenameBoth.Value && !Compat.TagConnectedPortals;

    public static bool CanRename(TeleportWorld portal) => Enabled && PortalWorld.IsPaired(portal);

    /// <summary><see cref="Plugin.RenameKey"/> at a paired portal opens the dialog.</summary>
    public static void UpdateKey()
    {
        if (!Enabled || Picker.IsOpen || Journal.Current == null || !Pad.Pressed(Plugin.RenameKey.Value) || TravelKey.GameUiOpen) return;
        var portal = TravelKey.Hovered();
        if (portal && CanRename(portal)) Open(portal);
    }

    /// <summary>Opens the game's text dialog for both ends of <paramref name="portal"/>'s pair.</summary>
    public static void Open(TeleportWorld portal)
    {
        var player = Player.m_localPlayer;
        if (!player || !CanRename(portal)) return;
        if (!PrivateArea.CheckAccess(portal.transform.position))
        {
            player.Message(MessageHud.MessageType.Center, "$piece_noaccess");
            return;
        }
        TextInput.instance.RequestText(new Receiver(portal), Texts.T("rename_both"), Plugin.NameLimit.Value);
    }

    sealed class Receiver : TextReceiver
    {
        readonly TeleportWorld portal;
        public Receiver(TeleportWorld portal) => this.portal = portal;
        public string GetText() => portal ? portal.GetText() : "";
        public void SetText(string text) => Apply(portal, text);
    }

    /// <summary>
    /// Renames a portal of the list, wherever it is: its object is asked from the server when this game knows it (the
    /// portal was seen this session), and a tag-paired one takes its other end along, so the pair stays connected.
    /// </summary>
    public static void OpenFor(PortalEntry entry)
    {
        if (!TextInput.instance) return;
        TextInput.instance.RequestText(new EntryReceiver(entry), Texts.T("rename"), Plugin.NameLimit.Value);
    }

    sealed class EntryReceiver : TextReceiver
    {
        readonly PortalEntry entry;
        public EntryReceiver(PortalEntry entry) => this.entry = entry;
        public string GetText() => Destinations.DisplayName(entry.Name, "");
        public void SetText(string text) => Plugin.Instance.StartCoroutine(RenameEntry(entry, text));
    }

    static IEnumerator RenameEntry(PortalEntry entry, string text)
    {
        var zdo = PortalWorld.Find(entry.Key);
        if (zdo == null && PortalWorld.TryGetSessionId(entry.Key, out var id))
        {
            ZDOMan.instance.RequestZDO(id);
            float until = Time.realtimeSinceStartup + FetchTimeout;
            while (zdo == null && Time.realtimeSinceStartup < until)
            {
                yield return null;
                zdo = PortalWorld.Find(entry.Key);
            }
        }
        if (zdo == null || !PrivateArea.CheckAccess(entry.Position, flash: false))
        {
            Plugin.Message(zdo == null ? Texts.T("rename_unreachable") : Localization.instance.Localize("$piece_noaccess"));
            yield break;
        }
        var other = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
        SetTag(zdo, text);
        if (!other.IsNone()) yield return RenameOther(other, text);
    }

    static void SetTag(ZDO zdo, string text)
    {
        zdo.Set(ZDOVars.s_tag, text);
        zdo.Set(ZDOVars.s_tagauthor, PlatformManager.DistributionPlatform.LocalUser.PlatformUserID.ToString());
        Journal.Refresh(zdo);
    }

    static void Apply(TeleportWorld portal, string text)
    {
        if (!portal || !portal.m_nview || !portal.m_nview.IsValid()) return;
        var other = portal.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
        portal.SetText(text);
        if (!other.IsNone()) Plugin.Instance.StartCoroutine(RenameOther(other, text));
    }

    /// <summary>The other end is usually far away: its object is asked from the server first.</summary>
    static IEnumerator RenameOther(ZDOID id, string text)
    {
        var zdo = ZDOMan.instance.GetZDO(id);
        if (zdo == null)
        {
            ZDOMan.instance.RequestZDO(id);
            float until = Time.realtimeSinceStartup + FetchTimeout;
            while (zdo == null && Time.realtimeSinceStartup < until)
            {
                yield return null;
                if (ZDOMan.instance == null) yield break;
                zdo = ZDOMan.instance.GetZDO(id);
            }
        }
        if (zdo == null || !zdo.IsValid())
        {
            Plugin.Message(Texts.T("rename_failed"));
            yield break;
        }
        SetTag(zdo, text);
    }
}
