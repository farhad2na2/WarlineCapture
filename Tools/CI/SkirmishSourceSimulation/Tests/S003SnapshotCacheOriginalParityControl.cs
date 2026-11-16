using System;
using Game.Components;
using Unity.Entities;

// The reference struct is generated from the exact current original source by
// renaming its type; the other witness is the experimental generated owner.
internal static class S003SnapshotCacheOriginalParityControl
{
    static int checks;
    static void Check(DynamicBuffer<GridWalkable> cells)
    {
        string expected = GridWalkableSourceWitnessReference.Digest(cells).ToString();
        string actual = GridWalkableSourceWitness.Digest(cells).ToString();
        if (!String.Equals(expected, actual, StringComparison.Ordinal))
            throw new InvalidOperationException("Generated cache differs from original at check " + checks);
        checks++;
    }
    static void Main()
    {
        var random = new Random(315003);
        foreach (int size in new[] { 0, 1, 2, 31, 8191, 8192, 8193, 32769, 2097152 })
        {
            var values = new GridWalkable[size];
            for (int i = 0; i < size; i++) values[i] = new GridWalkable { Value = (byte)random.Next(256) };
            var a = new DynamicBuffer<GridWalkable>(values);
            var b = new DynamicBuffer<GridWalkable>(values);
            Check(a); Check(a); Check(b);
            int count = size > 100000 ? 8 : 40;
            for (int i = 0; i < count; i++)
            {
                if (a.Length > 0)
                {
                    int index = random.Next(a.Length); var borrowed = a.AsNativeArray();
                    var row = borrowed[index]; row.Value ^= (byte)(1 << random.Next(8)); borrowed[index] = row;
                    Check(a); Check(b); Check(a);
                }
                a.Add(new GridWalkable { Value = (byte)random.Next(256) }); Check(a);
                a.RemoveAt(a.Length - 1); Check(a);
            }
            a.Clear(); Check(a); Check(b);
        }
        Console.WriteLine("[S003SnapshotCacheOriginalParityControl] result=Passed cases=9 borrowedMutation=Passed crossBuffer=Passed resize=Passed checks=" + checks + " sourceOnly=1");
    }
}
