using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ClientPortals;

public enum SortMode { Name, Nearest, Farthest }

/// <summary>The tabs of the list: everything (but the ignored portals), the favorites, the ignored portals.</summary>
public enum ListTab { All, Favorites, Ignored }

/// <summary>What the destination list shows and in which order.</summary>
public sealed class ListOptions
{
    public SortMode Sort = SortMode.Name;
    public ListTab Tab = ListTab.All;
    public bool FavoritesFirst = true;
    public bool HideNearby;
    public float HideNearbyDistance = 30f;
    public bool HideTwins;
    public string Search = "";
}

public static class Destinations
{
    /// <summary>Closer than this to the portal in use is that portal itself.</summary>
    public const float SamePortal = 1.5f;

    static readonly Regex Tags = new("<[^>]*>", RegexOptions.Compiled);

    /// <summary>A portal's tag as the list shows it: rich text stripped, <paramref name="unnamed"/> when empty.</summary>
    public static string DisplayName(string tag, string unnamed = "Unnamed portal")
    {
        var s = Tags.Replace(tag ?? "", "").Trim();
        return s.Length == 0 ? unnamed : s;
    }

    /// <summary>
    /// A name as searches and name comparisons see it: lower case, ё as е. The game's runtime (Mono) folds only ASCII
    /// letters for <see cref="StringComparison.OrdinalIgnoreCase"/>, so Cyrillic, Greek... names would stay case-sensitive.
    /// </summary>
    public static string Fold(string s) => (s ?? "").ToLowerInvariant().Replace('ё', 'е');

    /// <summary>Whether a portal has no name (only rich text or spaces): unnamed portals are hidden together.</summary>
    public static bool IsUnnamed(PortalEntry e) => DisplayName(e.Name, "").Length == 0;

    /// <summary>
    /// The portals one can travel to from <paramref name="from"/>: never the portal itself; on the Ignored tab only the
    /// ignored ones, else never them, nor the nearby ones with <see cref="ListOptions.HideNearby"/>, nor the nearer of
    /// portals sharing a name with <see cref="ListOptions.HideTwins"/> (both counted in <paramref name="hidden"/>);
    /// filtered and sorted. Distances for sorting are measured from <paramref name="viewer"/>.
    /// </summary>
    public static List<PortalEntry> List(IEnumerable<PortalEntry> known, Vector3 from, Vector3 viewer, ListOptions o,
        Func<PortalPos, bool> isFavorite, Func<PortalEntry, bool> isIgnored, out int hidden)
    {
        hidden = 0;
        var candidates = new List<PortalEntry>();
        foreach (var e in known)
        {
            if (e.Removed) continue;
            float d = Vector3.Distance(from, e.Position);
            if (d < SamePortal) continue;
            if (isIgnored(e) != (o.Tab == ListTab.Ignored)) continue;
            if (o.Tab != ListTab.Ignored && o.HideNearby && d < o.HideNearbyDistance)
            {
                hidden++;
                continue;
            }
            candidates.Add(e);
        }
        if (o.Tab != ListTab.Ignored && o.HideTwins) hidden += RemoveNearerTwins(candidates, from);

        string search = Fold(o.Search).Trim();
        IEnumerable<PortalEntry> q = candidates;
        if (o.Tab == ListTab.Favorites) q = q.Where(e => isFavorite(e.Key));
        // the name itself: an unnamed portal's placeholder text never matches
        if (search.Length > 0) q = q.Where(e => Fold(DisplayName(e.Name, "")).IndexOf(search, StringComparison.Ordinal) >= 0);

        var ordered = o.FavoritesFirst ? q.OrderByDescending(e => isFavorite(e.Key)) : q.OrderBy(_ => 0);
        ordered = o.Sort switch
        {
            SortMode.Nearest => ordered.ThenBy(e => Vector3.Distance(viewer, e.Position)).ThenBy(e => DisplayName(e.Name), StringComparer.OrdinalIgnoreCase),
            SortMode.Farthest => ordered.ThenByDescending(e => Vector3.Distance(viewer, e.Position)).ThenBy(e => DisplayName(e.Name), StringComparer.OrdinalIgnoreCase),
            _ => ordered.ThenBy(e => DisplayName(e.Name), StringComparer.OrdinalIgnoreCase).ThenBy(e => Vector3.Distance(viewer, e.Position)),
        };
        return ordered.ToList();
    }

    /// <summary>
    /// Of portals sharing a name, keeps the one farthest from <paramref name="from"/>: in a portal room, the near one of
    /// a tag pair is the room's own end. Unnamed portals aren't twins. Returns how many were removed.
    /// </summary>
    public static int RemoveNearerTwins(List<PortalEntry> portals, Vector3 from)
    {
        var farthest = new Dictionary<string, PortalEntry>();
        foreach (var e in portals)
        {
            string name = Fold(DisplayName(e.Name, ""));
            if (name.Length == 0) continue;
            if (!farthest.TryGetValue(name, out var kept) || Vector3.Distance(from, e.Position) > Vector3.Distance(from, kept.Position))
                farthest[name] = e;
        }
        return portals.RemoveAll(e =>
        {
            string name = Fold(DisplayName(e.Name, ""));
            return name.Length > 0 && farthest[name] != e;
        });
    }

    /// <summary>The next sort mode of the sort button: name, nearest first, farthest first.</summary>
    public static SortMode Next(SortMode mode) => mode switch
    {
        SortMode.Name => SortMode.Nearest,
        SortMode.Nearest => SortMode.Farthest,
        _ => SortMode.Name,
    };

    /// <summary>Moves a list selection by <paramref name="delta"/>, clamped to the list (-1 when it is empty).</summary>
    public static int Step(int selected, int delta, int count)
    {
        if (count <= 0) return -1;
        return Mathf.Clamp(selected + delta, 0, count - 1);
    }

    /// <summary>The scroll offset that keeps row <paramref name="index"/> fully in a viewport of <paramref name="viewHeight"/>.</summary>
    public static float ScrollTo(int index, float rowPitch, float rowHeight, float viewHeight, float scroll)
    {
        if (index < 0) return scroll;
        float top = index * rowPitch, bottom = top + rowHeight;
        if (top < scroll) return top;
        if (bottom > scroll + viewHeight) return bottom - viewHeight;
        return scroll;
    }
}
