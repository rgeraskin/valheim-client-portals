using HarmonyLib;

namespace ClientPortals;

/// <summary>
/// Cartography tables keep an atlas of portals next to the shared map: reading the map teaches the portals on it,
/// writing records the ones the player knows. Players without the mod share the map as usual.
/// </summary>
[HarmonyPatch(typeof(MapTable))]
internal static class TablePatches
{
    static readonly int AtlasKey = "ClientPortals.Atlas".GetStableHashCode();

    static Atlas ReadTable(ZDO zdo) => Atlas.Deserialize(zdo.GetByteArray(AtlasKey));

    static bool Enabled(MapTable table) =>
        Plugin.CartographyTable.Value && Journal.Current != null && table.m_nview && table.m_nview.IsValid();

    // hover texts are rebuilt every frame: count again only when the table's data changes
    static byte[] counted;
    static int count;

    static string Count(MapTable table)
    {
        if (!Enabled(table)) return "";
        var zdo = table.m_nview.GetZDO();
        var data = zdo.GetByteArray(AtlasKey);
        if (data != counted || data == null)
        {
            count = ReadTable(zdo).KnownCount;
            counted = data;
        }
        return "\n" + Texts.T("table_count", count.ToString());
    }

    [HarmonyPostfix, HarmonyPatch(nameof(MapTable.GetReadHoverText))]
    static void ReadHover(MapTable __instance, ref string __result) => __result += Count(__instance);

    [HarmonyPostfix, HarmonyPatch(nameof(MapTable.GetWriteHoverText))]
    static void WriteHover(MapTable __instance, ref string __result) => __result += Count(__instance);

    [HarmonyPostfix, HarmonyPatch(nameof(MapTable.OnRead), typeof(Switch), typeof(Humanoid), typeof(ItemDrop.ItemData), typeof(bool))]
    static void Read(MapTable __instance, Humanoid user, ItemDrop.ItemData item, bool showMessage)
    {
        if (item != null || user != Player.m_localPlayer || !Enabled(__instance)) return;
        int learned = Journal.Merge(ReadTable(__instance.m_nview.GetZDO()).All);
        if (showMessage && learned > 0)
            Plugin.Message(Texts.T("table_learned", learned.ToString()), MessageHud.MessageType.TopLeft);
    }

    /// <summary>The table keeps the newer copy of every portal from both sides (removals included).</summary>
    [HarmonyPostfix, HarmonyPatch(nameof(MapTable.OnWrite))]
    static void Write(MapTable __instance, Humanoid user, ItemDrop.ItemData item, bool __result)
    {
        if (!__result || item != null || user != Player.m_localPlayer || !Enabled(__instance)) return;
        var zdo = __instance.m_nview.GetZDO();
        var table = ReadTable(zdo);
        Journal.Merge(table.All);
        int before = table.KnownCount;
        table.Merge(Journal.Current);
        __instance.m_nview.ClaimOwnership();
        zdo.Set(AtlasKey, table.Serialize());
        int added = table.KnownCount - before;
        if (added > 0)
            Plugin.Message(Texts.T("table_recorded", added.ToString()), MessageHud.MessageType.TopLeft);
    }
}
