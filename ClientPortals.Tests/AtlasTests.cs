using System.Linq;
using ClientPortals;
using UnityEngine;
using Xunit;

public class AtlasTests
{
    static readonly Vector3 A = new(100.01f, 30f, -250f), B = new(-1200f, 45.5f, 800f);

    [Fact]
    public void PositionKeyIgnoresSubDecimeterNoise()
    {
        Assert.Equal(PortalPos.Of(A), PortalPos.Of(A + new Vector3(0.03f, -0.02f, 0.01f)));
        Assert.NotEqual(PortalPos.Of(A), PortalPos.Of(A + new Vector3(0.2f, 0, 0)));
    }

    [Fact]
    public void ObserveLearnsOnceAndFollowsRenames()
    {
        var atlas = new Atlas();
        Assert.True(atlas.Observe(A, Quaternion.identity, "home", 10));
        Assert.False(atlas.Observe(A, Quaternion.identity, "home", 20));
        Assert.True(atlas.TryGet(PortalPos.Of(A), out var e));
        Assert.Equal(10, e.Stamp);

        Assert.False(atlas.Observe(A, Quaternion.identity, "Home base", 30));
        Assert.Equal("Home base", e.Name);
        Assert.Equal(30, e.Stamp);
    }

    [Fact]
    public void ForgottenPortalsAreRelearned()
    {
        var atlas = new Atlas();
        atlas.Observe(A, Quaternion.identity, "a", 1);
        Assert.True(atlas.Forget(PortalPos.Of(A), 2));
        Assert.False(atlas.Forget(PortalPos.Of(A), 3));
        Assert.Equal(0, atlas.KnownCount);
        Assert.True(atlas.Observe(A, Quaternion.identity, "a", 4));
        Assert.Equal(1, atlas.KnownCount);
    }

    [Fact]
    public void StampsOnlyMoveForward()
    {
        // a change recorded with an older clock (another player's) still counts as newer
        var atlas = new Atlas();
        atlas.Observe(A, Quaternion.identity, "a", 100);
        atlas.Forget(PortalPos.Of(A), 50);
        Assert.True(atlas.TryGet(PortalPos.Of(A), out var e));
        Assert.True(e.Stamp > 100);
    }

    [Fact]
    public void MergeKeepsTheNewerCopyAndCountsWhatWasLearned()
    {
        var mine = new Atlas();
        mine.Observe(A, Quaternion.identity, "old name", 10);

        var table = new Atlas();
        table.Observe(A, Quaternion.identity, "new name", 20);
        table.Observe(B, Quaternion.identity, "far", 5);

        Assert.Equal(1, mine.Merge(table));
        Assert.Equal(2, mine.KnownCount);
        Assert.True(mine.TryGet(PortalPos.Of(A), out var a));
        Assert.Equal("new name", a.Name);

        // the incoming copy is not shared with the source
        table.Observe(B, Quaternion.identity, "renamed", 30);
        Assert.True(mine.TryGet(PortalPos.Of(B), out var b));
        Assert.Equal("far", b.Name);
    }

    [Fact]
    public void RemovalsTravelThroughMerges()
    {
        var mine = new Atlas();
        mine.Observe(A, Quaternion.identity, "a", 10);
        var table = new Atlas();
        table.Merge(mine);
        table.Forget(PortalPos.Of(A), 20);

        Assert.Equal(0, mine.Merge(table));
        Assert.Equal(0, mine.KnownCount);

        // an older sighting doesn't bring it back
        var stale = new Atlas();
        stale.Observe(A, Quaternion.identity, "a", 15);
        Assert.Equal(0, mine.Merge(stale));
        Assert.Equal(0, mine.KnownCount);
    }

    [Fact]
    public void SerializationRoundTrips()
    {
        var atlas = new Atlas();
        var rot = new Quaternion(0, Mathf.Sin(68.5f * Mathf.Deg2Rad), 0, Mathf.Cos(68.5f * Mathf.Deg2Rad)); // 137° about Y (Quaternion.Euler is native)
        atlas.Observe(A, rot, "<color=red>Ashlands</color> camp", 123456789);
        atlas.Observe(B, Quaternion.identity, "", 42);
        atlas.Forget(PortalPos.Of(B), 43);

        var copy = Atlas.Deserialize(atlas.Serialize());
        Assert.Equal(2, copy.All.Count());
        Assert.Equal(1, copy.KnownCount);
        Assert.True(copy.TryGet(PortalPos.Of(A), out var a));
        Assert.Equal("<color=red>Ashlands</color> camp", a.Name);
        Assert.Equal(123456789, a.Stamp);
        Assert.True(Quaternion.Angle(rot, a.Rotation) < 0.01f);
        Assert.True(copy.TryGet(PortalPos.Of(B), out var b));
        Assert.True(b.Removed);
    }

    [Fact]
    public void UnreadableDataGivesAnEmptyAtlas()
    {
        Assert.Equal(0, Atlas.Deserialize(null).KnownCount);
        Assert.Equal(0, Atlas.Deserialize(new byte[] { 1, 2, 3, 4, 5 }).KnownCount);
    }

    [Fact]
    public void OldRemovalMarkersArePruned()
    {
        var atlas = new Atlas();
        atlas.Observe(A, Quaternion.identity, "a", 10);
        atlas.Observe(B, Quaternion.identity, "b", 10);
        atlas.Forget(PortalPos.Of(A), 20);
        Assert.Equal(1, atlas.PruneRemoved(100));
        Assert.Single(atlas.All);
        Assert.Equal(0, atlas.PruneRemoved(100));
    }
}
