using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Game.Components;
using Unity.Entities;

// Isolated source-host experiment. A fresh snapshot is compared byte-for-byte on
// every call; this does not assume that buffer versions track borrowed writes.
internal static class S003WalkabilitySnapshotCacheControl
{
    static readonly object Gate = new object();
    static GridWalkable[] prior;
    static string priorDigest;
    static int hits;
    static string Candidate(DynamicBuffer<GridWalkable> cells)
    {
        var snapshot = cells.Copy();
        var bytes = MemoryMarshal.AsBytes(snapshot.AsSpan());
        if (Marshal.SizeOf<GridWalkable>() != 1 || bytes.Length != snapshot.Length)
            throw new InvalidOperationException("Exact one-byte managed layout required");
        lock (Gate)
        {
            if (prior != null && bytes.SequenceEqual(MemoryMarshal.AsBytes(prior.AsSpan())))
            { hits++; return priorDigest; }
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            sha.AppendData(bytes);
            string digest = BitConverter.ToString(sha.GetHashAndReset()).Replace("-", "").ToLowerInvariant();
            prior = snapshot;
            priorDigest = digest;
            return digest;
        }
    }
    static void Check(DynamicBuffer<GridWalkable> cells)
    {
        if (GridWalkableSourceWitness.Digest(cells).ToString() != Candidate(cells))
            throw new InvalidOperationException("Snapshot cache differs from original digest");
    }
    static double Time(Action action) { var w = Stopwatch.StartNew(); action(); w.Stop(); return w.Elapsed.TotalMilliseconds; }
    static void Main()
    {
        foreach (int size in new[] { 0, 1, 2, 8191, 8192, 8193, 32769, 2097152 })
        {
            var values = new GridWalkable[size];
            for (int i = 0; i < size; i++) values[i] = new GridWalkable { Value = (byte)((i * 73 + i / 17) & 255) };
            var cells = new DynamicBuffer<GridWalkable>(values);
            Check(cells); int before = hits; Check(cells);
            if (hits != before + 1) throw new InvalidOperationException("Equal snapshot did not hit");
            if (size > 0)
            {
                var borrowed = cells.AsNativeArray(); int index = size / 2;
                var changed = borrowed[index]; changed.Value ^= 1; borrowed[index] = changed;
                before = hits; Check(cells);
                if (hits != before) throw new InvalidOperationException("Borrowed mutation was cached");
                changed.Value ^= 1; borrowed[index] = changed; Check(cells);
            }
            cells.Add(new GridWalkable { Value = 17 }); Check(cells);
            cells.RemoveAt(cells.Length - 1); Check(cells);
        }
        var large = new DynamicBuffer<GridWalkable>(new GridWalkable[2097152]); Check(large); Check(large);
        var oldTimes = new List<double>(); var cacheTimes = new List<double>();
        for (int i = 0; i < 12; i++)
        {
            if ((i & 1) == 0) { oldTimes.Add(Time(() => GridWalkableSourceWitness.Digest(large))); cacheTimes.Add(Time(() => Candidate(large))); }
            else { cacheTimes.Add(Time(() => Candidate(large))); oldTimes.Add(Time(() => GridWalkableSourceWitness.Digest(large))); }
        }
        Console.WriteLine("[S003WalkabilitySnapshotCacheControl] result=Passed cases=8 borrowedMutation=Passed resize=Passed paired=12 sourceOnly=1");
        Console.WriteLine("original_ms=" + string.Join(",", oldTimes.Select(x => x.ToString("R", System.Globalization.CultureInfo.InvariantCulture))));
        Console.WriteLine("candidate_ms=" + string.Join(",", cacheTimes.Select(x => x.ToString("R", System.Globalization.CultureInfo.InvariantCulture))));
    }
}
