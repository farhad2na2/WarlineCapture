using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Game.Components;
using Unity.Entities;

// Source-host experiment only: the original witness remains authoritative.
internal static class S003WalkabilityDigestPerformanceControl
{
    static string Candidate(DynamicBuffer<GridWalkable> cells)
    {
        if (Marshal.SizeOf<GridWalkable>() != 1) throw new InvalidOperationException("Exact one-byte source layout required");
        GridWalkable[] copy = cells.Copy();
        byte[] bytes = MemoryMarshal.AsBytes(copy.AsSpan()).ToArray();
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    static string CandidateIncremental(DynamicBuffer<GridWalkable> cells)
    {
        if (Marshal.SizeOf<GridWalkable>() != 1) throw new InvalidOperationException("Exact one-byte source layout required");
        GridWalkable[] copy = cells.Copy();
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        sha.AppendData(MemoryMarshal.AsBytes(copy.AsSpan()));
        return BitConverter.ToString(sha.GetHashAndReset()).Replace("-", "").ToLowerInvariant();
    }

    static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static void Main()
    {
        foreach (int size in new[] { 0, 1, 2, 8191, 8192, 8193, 32769, 2097152 })
        {
            var rows = new GridWalkable[size];
            for (int i = 0; i < size; i++) rows[i] = new GridWalkable { Value = (byte)((i * 73 + i / 17) & 255) };
            var buffer = new DynamicBuffer<GridWalkable>(rows);
            string original = GridWalkableSourceWitness.Digest(buffer).ToString();
            Need(original == Candidate(buffer) && original == CandidateIncremental(buffer), "Original source digest differs at length " + size);
            if (size > 0)
            {
                int position = size / 2; var row = buffer[position]; row.Value ^= 1; buffer[position] = row;
                string changed = GridWalkableSourceWitness.Digest(buffer).ToString();
                Need(changed != original && changed == Candidate(buffer) && changed == CandidateIncremental(buffer), "Mutated source buffer digest differs at length " + size);
            }
        }
        var large = new GridWalkable[2097152];
        for (int i = 0; i < large.Length; i++) large[i] = new GridWalkable { Value = (byte)((i * 19) & 1) };
        var cells = new DynamicBuffer<GridWalkable>(large);
        Need(GridWalkableSourceWitness.Digest(cells).ToString() == Candidate(cells), "Benchmark inputs differ");
        var originalTimes = new List<double>(); var candidateTimes = new List<double>(); var incrementalTimes = new List<double>();
        for (int i = 0; i < 12; i++)
        {
            Action old = () => GridWalkableSourceWitness.Digest(cells);
            Action next = () => Candidate(cells);
            Action incremental = () => CandidateIncremental(cells);
            if (i % 3 == 0) { originalTimes.Add(Time(old)); candidateTimes.Add(Time(next)); incrementalTimes.Add(Time(incremental)); }
            else if (i % 3 == 1) { candidateTimes.Add(Time(next)); incrementalTimes.Add(Time(incremental)); originalTimes.Add(Time(old)); }
            else { incrementalTimes.Add(Time(incremental)); originalTimes.Add(Time(old)); candidateTimes.Add(Time(next)); }
        }
        Console.WriteLine("[S003WalkabilityDigestPerformanceControl] result=Passed cases=8 mutation=Passed paired=12 sourceOnly=1");
        Console.WriteLine("original_ms=" + string.Join(",", originalTimes.Select(x => x.ToString("R", System.Globalization.CultureInfo.InvariantCulture))));
        Console.WriteLine("candidate_ms=" + string.Join(",", candidateTimes.Select(x => x.ToString("R", System.Globalization.CultureInfo.InvariantCulture))));
        Console.WriteLine("incremental_ms=" + string.Join(",", incrementalTimes.Select(x => x.ToString("R", System.Globalization.CultureInfo.InvariantCulture))));
    }

    static double Time(Action action)
    {
        var watch = Stopwatch.StartNew(); action(); watch.Stop(); return watch.Elapsed.TotalMilliseconds;
    }
}
