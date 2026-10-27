using System.Collections.Generic;
using System.Linq;
using ClientPortals;
using UnityEngine;
using Xunit;

public class DestinationTests
{
    static readonly Vector3 Here = Vector3.zero;

    static PortalEntry P(string name, float x, bool removed = false) =>
        new() { Name = name, Position = new Vector3(x, 0, 0), Removed = removed };

    static readonly List<PortalEntry> Known = new()
    {
        P("home", 0.5f),        // the portal in use
        P("Base shed", 20f),    // nearby
        P("Swamp", 900f),
        P("ashlands", 3000f),
        P("<b>Mountain</b>", 400f),
        P("gone", 50f, removed: true),
    };

    static List<string> Names(ListOptions o, HashSet<string> favorites, out int hidden, HashSet<string> ignored = null, List<PortalEntry> known = null)
    {
        known ??= Known;
        string NameOf(PortalPos k) => known.First(e => e.Key == k).Name;
        return Destinations.List(known, Here, Here, o, k => favorites.Contains(NameOf(k)), e => ignored != null && ignored.Contains(e.Name), out hidden)
            .Select(e => Destinations.DisplayName(e.Name)).ToList();
    }

    [Fact]
    public void NeverTheSourcePortalNorRemovedOnes()
    {
        var names = Names(new ListOptions(), new HashSet<string>(), out int hidden);
        Assert.Equal(new[] { "ashlands", "Base shed", "Mountain", "Swamp" }, names);
        Assert.Equal(0, hidden);
    }

    [Fact]
    public void SortsByDistanceBothWays()
    {
        Assert.Equal(new[] { "Base shed", "Mountain", "Swamp", "ashlands" }, Names(new ListOptions { Sort = SortMode.Nearest }, new HashSet<string>(), out _));
        Assert.Equal(new[] { "ashlands", "Swamp", "Mountain", "Base shed" }, Names(new ListOptions { Sort = SortMode.Farthest }, new HashSet<string>(), out _));
    }

    [Fact]
    public void SortButtonCyclesThroughThreeModes()
    {
        Assert.Equal(SortMode.Nearest, Destinations.Next(SortMode.Name));
        Assert.Equal(SortMode.Farthest, Destinations.Next(SortMode.Nearest));
        Assert.Equal(SortMode.Name, Destinations.Next(SortMode.Farthest));
    }

    [Fact]
    public void IgnoredPortalsOnlyOnTheirTab()
    {
        var ignored = new HashSet<string> { "Swamp", "Base shed" };
        var o = new ListOptions { HideNearby = true, HideNearbyDistance = 30f };
        Assert.Equal(new[] { "ashlands", "Mountain" }, Names(o, new HashSet<string>(), out int hidden, ignored));
        Assert.Equal(0, hidden); // ignored isn't hidden: it has its own tab
        o.Tab = ListTab.Ignored;
        // the Ignored tab shows them all, nearby ones too
        Assert.Equal(new[] { "Base shed", "Swamp" }, Names(o, new HashSet<string>(), out _, ignored));
        o.Tab = ListTab.Favorites;
        Assert.Empty(Names(o, new HashSet<string> { "Swamp" }, out _, ignored));
    }

    [Fact]
    public void HidesTheNearerOfPortalsSharingAName()
    {
        var known = new List<PortalEntry>
        {
            P("home", 0.5f),     // the portal in use, in the portal room
            P("Swamp", 10f),     // the room's end of the Swamp pair
            P("swamp", 900f),    // the far end
            P("Mine", 12f),
            P("", 14f), P("", 16f), // unnamed portals aren't twins
        };
        var names = Names(new ListOptions { HideTwins = true, Sort = SortMode.Nearest }, new HashSet<string>(), out int hidden, known: known);
        Assert.Equal(new[] { "Mine", "Unnamed portal", "Unnamed portal", "swamp" }, names);
        Assert.Equal(1, hidden);
        Assert.Equal(5, Names(new ListOptions(), new HashSet<string>(), out _, known: known).Count);
    }

    [Fact]
    public void HidesNearbyPortalsAndCountsThem()
    {
        var names = Names(new ListOptions { HideNearby = true, HideNearbyDistance = 30f }, new HashSet<string>(), out int hidden);
        Assert.DoesNotContain("Base shed", names);
        Assert.Equal(1, hidden);
    }

    [Fact]
    public void FavoritesFirstAndFavoritesTab()
    {
        var favs = new HashSet<string> { "Swamp" };
        Assert.Equal("Swamp", Names(new ListOptions { FavoritesFirst = true }, favs, out _)[0]);
        Assert.Equal("ashlands", Names(new ListOptions { FavoritesFirst = false }, favs, out _)[0]);
        Assert.Equal(new[] { "Swamp" }, Names(new ListOptions { Tab = ListTab.Favorites }, favs, out _));
    }

    [Fact]
    public void SearchIgnoresCaseAndRichText()
    {
        Assert.Equal(new[] { "Mountain" }, Names(new ListOptions { Search = "MOUNT" }, new HashSet<string>(), out _));
        Assert.Empty(Names(new ListOptions { Search = "<b>" }, new HashSet<string>(), out _));
    }

    [Fact]
    public void SearchIgnoresCaseInEveryScript()
    {
        var known = new List<PortalEntry> { P("Болото", 100f), P("Ёлки", 200f), P("Σπίτι", 300f) };
        Assert.Equal(new[] { "Болото" }, Names(new ListOptions { Search = "БОЛ" }, new HashSet<string>(), out _, known: known));
        Assert.Equal(new[] { "Ёлки" }, Names(new ListOptions { Search = "елк" }, new HashSet<string>(), out _, known: known));
        Assert.Equal(new[] { "Σπίτι" }, Names(new ListOptions { Search = "ΣΠΊ" }, new HashSet<string>(), out _, known: known));
        // twins too: "Болото" and "болото" are one name
        known.Add(P("болото", 900f));
        Assert.Single(Names(new ListOptions { HideTwins = true, Search = "бол" }, new HashSet<string>(), out _, known: known));
    }

    [Fact]
    public void SearchNeverMatchesTheUnnamedPlaceholder()
    {
        var known = new List<PortalEntry> { P("", 100f), P("Swamp", 200f) };
        Assert.Equal(new[] { "Swamp" }, Names(new ListOptions { Search = "a" }, new HashSet<string>(), out _, known: known));
    }

    [Fact]
    public void DisplayNames()
    {
        Assert.Equal("Unnamed portal", Destinations.DisplayName(null));
        Assert.Equal("Unnamed portal", Destinations.DisplayName("  <color=red></color> "));
        Assert.Equal("Home", Destinations.DisplayName(" <i>Home</i>"));
    }

    [Theory]
    [InlineData(0, -1, 5, 0)]
    [InlineData(4, 1, 5, 4)]
    [InlineData(2, 8, 5, 4)]
    [InlineData(3, -8, 5, 0)]
    [InlineData(0, 1, 0, -1)]
    [InlineData(-1, 1, 3, 0)]
    public void SelectionStaysInTheList(int selected, int delta, int count, int expected) =>
        Assert.Equal(expected, Destinations.Step(selected, delta, count));

    [Fact]
    public void ScrollFollowsTheSelection()
    {
        // rows 40 high every 44, a viewport of 396 (9 rows)
        Assert.Equal(0f, Destinations.ScrollTo(0, 44, 40, 396, 0));
        Assert.Equal(44f * 10 + 40 - 396, Destinations.ScrollTo(10, 44, 40, 396, 0));
        Assert.Equal(44f * 2, Destinations.ScrollTo(2, 44, 40, 396, 200));
        Assert.Equal(100f, Destinations.ScrollTo(4, 44, 40, 396, 100));
    }
}
