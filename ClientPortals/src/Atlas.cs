using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

namespace ClientPortals;

/// <summary>
/// Identifies a portal by where it stands, to the decimeter. The game renumbers every object (ZDOID)
/// each time the world loads, so ids can't be kept between sessions; portals don't move.
/// </summary>
public readonly struct PortalPos : IEquatable<PortalPos>
{
    public readonly int X, Y, Z;

    public PortalPos(int x, int y, int z) { X = x; Y = y; Z = z; }

    public static PortalPos Of(Vector3 p) =>
        new(Mathf.RoundToInt(p.x * 10f), Mathf.RoundToInt(p.y * 10f), Mathf.RoundToInt(p.z * 10f));

    public bool Equals(PortalPos o) => X == o.X && Y == o.Y && Z == o.Z;
    public override bool Equals(object obj) => obj is PortalPos o && Equals(o);
    public override int GetHashCode() => unchecked((X * 397 ^ Y) * 397 ^ Z);
    public static bool operator ==(PortalPos a, PortalPos b) => a.Equals(b);
    public static bool operator !=(PortalPos a, PortalPos b) => !a.Equals(b);
    public override string ToString() => $"({X / 10f:0.0}, {Y / 10f:0.0}, {Z / 10f:0.0})";
}

/// <summary>A portal the player knows of. <see cref="Removed"/> entries are kept so that a removal travels through cartography tables like any other change.</summary>
public sealed class PortalEntry
{
    public Vector3 Position;
    public Quaternion Rotation = Quaternion.identity;
    public string Name = "";
    /// <summary>When this entry last changed (game time ticks); the newer of two copies wins a merge.</summary>
    public long Stamp;
    public bool Removed;

    public PortalPos Key => PortalPos.Of(Position);

    public PortalEntry Clone() => (PortalEntry)MemberwiseClone();
}

/// <summary>A set of known portals: what a player knows (per world) and what a cartography table holds.</summary>
public sealed class Atlas
{
    const int Version = 1;

    readonly Dictionary<PortalPos, PortalEntry> entries = new();

    public IEnumerable<PortalEntry> All => entries.Values;
    public IEnumerable<PortalEntry> Known => entries.Values.Where(e => !e.Removed);
    public int KnownCount => entries.Values.Count(e => !e.Removed);

    public bool TryGet(PortalPos key, out PortalEntry entry) => entries.TryGetValue(key, out entry);

    public bool Knows(PortalPos key) => entries.TryGetValue(key, out var e) && !e.Removed;

    /// <summary>Records a portal seen in the world. Returns true when it was not known before (new, or known as removed).</summary>
    public bool Observe(Vector3 position, Quaternion rotation, string name, long stamp)
    {
        var key = PortalPos.Of(position);
        name ??= "";
        if (entries.TryGetValue(key, out var e))
        {
            bool learned = e.Removed;
            if (learned || e.Name != name || Quaternion.Angle(e.Rotation, rotation) > 1f)
            {
                e.Position = position;
                e.Rotation = rotation;
                e.Name = name;
                e.Removed = false;
                e.Stamp = Math.Max(stamp, e.Stamp + 1);
            }
            return learned;
        }
        entries[key] = new PortalEntry { Position = position, Rotation = rotation, Name = name, Stamp = stamp };
        return true;
    }

    /// <summary>Marks a known portal as gone. Returns true if it was known.</summary>
    public bool Forget(PortalPos key, long stamp)
    {
        if (!entries.TryGetValue(key, out var e) || e.Removed) return false;
        e.Removed = true;
        e.Stamp = Math.Max(stamp, e.Stamp + 1);
        return true;
    }

    /// <summary>Takes the newer copy of every entry of <paramref name="other"/>. Returns how many portals became known.</summary>
    public int Merge(Atlas other) => Merge(other.All);

    public int Merge(IEnumerable<PortalEntry> incoming)
    {
        int learned = 0;
        foreach (var o in incoming)
        {
            var key = o.Key;
            if (entries.TryGetValue(key, out var mine))
            {
                if (o.Stamp <= mine.Stamp) continue;
                if (mine.Removed && !o.Removed) learned++;
            }
            else if (!o.Removed) learned++;
            entries[key] = o.Clone();
        }
        return learned;
    }

    /// <summary>Drops removal markers older than <paramref name="before"/>; they have had time to spread.</summary>
    public int PruneRemoved(long before)
    {
        var old = entries.Where(kv => kv.Value.Removed && kv.Value.Stamp < before).Select(kv => kv.Key).ToList();
        foreach (var k in old) entries.Remove(k);
        return old.Count;
    }

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using (var z = new DeflateStream(ms, CompressionMode.Compress, leaveOpen: true))
        using (var w = new BinaryWriter(z))
        {
            w.Write(Version);
            w.Write(entries.Count);
            foreach (var e in entries.Values)
            {
                w.Write(e.Position.x); w.Write(e.Position.y); w.Write(e.Position.z);
                w.Write(e.Rotation.x); w.Write(e.Rotation.y); w.Write(e.Rotation.z); w.Write(e.Rotation.w);
                w.Write(e.Name ?? "");
                w.Write(e.Stamp);
                w.Write(e.Removed);
            }
        }
        return ms.ToArray();
    }

    /// <summary>Reads <see cref="Serialize"/>'s output; anything unreadable gives an empty atlas.</summary>
    public static Atlas Deserialize(byte[] data)
    {
        var atlas = new Atlas();
        if (data == null || data.Length == 0) return atlas;
        try
        {
            using var r = new BinaryReader(new DeflateStream(new MemoryStream(data), CompressionMode.Decompress));
            if (r.ReadInt32() != Version) return atlas;
            int n = r.ReadInt32();
            var read = new List<PortalEntry>(n);
            for (int i = 0; i < n; i++)
            {
                read.Add(new PortalEntry
                {
                    Position = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()),
                    Rotation = new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()),
                    Name = r.ReadString(),
                    Stamp = r.ReadInt64(),
                    Removed = r.ReadBoolean(),
                });
            }
            atlas.Merge(read);
        }
        catch (Exception)
        {
            return new Atlas();
        }
        return atlas;
    }
}
