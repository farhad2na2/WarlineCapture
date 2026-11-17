using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Runtime
{
    /// <summary>
    /// Pure census of archetype "toggle dimensions". Entities keeps every archetype a World ever created, and each new
    /// archetype is matched against every cached query shape in the World's fixed EntityQueryManager block allocator
    /// (16 MiB in Entities 6.5, never freed until the World is disposed). A component that is added/removed at runtime
    /// doubles the archetype family it lives in. A type T is counted for archetype A when A contains T and the archetype
    /// A minus T also exists: that pair can only appear when T was structurally toggled (or authored both ways).
    /// </summary>
    public static class EcsArchetypeToggleCensus
    {
        public readonly struct Row
        {
            public readonly int TypeIndex;
            public readonly int Pairs;
            public readonly int Containing;
            public Row(int typeIndex, int pairs, int containing) { TypeIndex = typeIndex; Pairs = pairs; Containing = containing; }
        }

        /// <summary>Archetypes are given as their component type indices (any order, no duplicates).</summary>
        public static List<Row> Rank(IReadOnlyList<int[]> archetypes, int top)
        {
            var result = new List<Row>();
            if (archetypes == null || archetypes.Count == 0 || top <= 0) return result;
            var sorted = new List<int[]>(archetypes.Count);
            var signatures = new HashSet<Signature>();
            foreach (var types in archetypes)
            {
                if (types == null) continue;
                var copy = (int[])types.Clone();
                Array.Sort(copy);
                sorted.Add(copy);
                signatures.Add(Signature.Of(copy, -1));
            }
            var pairs = new Dictionary<int, int>();
            var containing = new Dictionary<int, int>();
            foreach (var types in sorted)
            {
                for (int i = 0; i < types.Length; i++)
                {
                    containing.TryGetValue(types[i], out int c);
                    containing[types[i]] = c + 1;
                    if (!signatures.Contains(Signature.Of(types, i))) continue;
                    pairs.TryGetValue(types[i], out int p);
                    pairs[types[i]] = p + 1;
                }
            }
            foreach (var entry in pairs) result.Add(new Row(entry.Key, entry.Value, containing[entry.Key]));
            result.Sort((a, b) => a.Pairs != b.Pairs ? b.Pairs.CompareTo(a.Pairs) : a.TypeIndex.CompareTo(b.TypeIndex));
            if (result.Count > top) result.RemoveRange(top, result.Count - top);
            return result;
        }

        public static string Format(IReadOnlyList<Row> rows, Func<int, string> name)
        {
            var text = new StringBuilder();
            for (int i = 0; i < rows.Count; i++)
            {
                if (i > 0) text.Append(',');
                string label = name != null ? name(rows[i].TypeIndex) : null;
                text.Append(string.IsNullOrEmpty(label) ? rows[i].TypeIndex.ToString(CultureInfo.InvariantCulture) : label)
                    .Append(':').Append(rows[i].Pairs.ToString(CultureInfo.InvariantCulture))
                    .Append('/').Append(rows[i].Containing.ToString(CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }

        // Sorted type list with one position skipped (-1 = none). Equality compares the actual lists, so a hash collision
        // can never fabricate a toggle pair.
        private readonly struct Signature : IEquatable<Signature>
        {
            private readonly int[] types;
            private readonly int skip;
            private readonly int hash;

            private Signature(int[] types, int skip, int hash) { this.types = types; this.skip = skip; this.hash = hash; }

            public static Signature Of(int[] sortedTypes, int skip)
            {
                unchecked
                {
                    int h = (int)2166136261;
                    for (int i = 0; i < sortedTypes.Length; i++)
                        if (i != skip) h = (h ^ sortedTypes[i]) * 16777619;
                    return new Signature(sortedTypes, skip, h);
                }
            }

            private int Count => skip >= 0 ? types.Length - 1 : types.Length;

            public bool Equals(Signature other)
            {
                if (hash != other.hash || Count != other.Count) return false;
                int i = 0, j = 0;
                while (true)
                {
                    if (i == skip) i++;
                    if (j == other.skip) j++;
                    bool endA = i >= types.Length, endB = j >= other.types.Length;
                    if (endA || endB) return endA && endB;
                    if (types[i] != other.types[j]) return false;
                    i++; j++;
                }
            }

            public override bool Equals(object obj) => obj is Signature other && Equals(other);
            public override int GetHashCode() => hash;
        }
    }

    /// <summary>Pure budget arithmetic and log formatting for <see cref="EcsQueryBudget"/>.</summary>
    public static class EcsQueryBudgetMath
    {
        public const int BlockBytes = 1 << 16; // Unity.Entities.BlockAllocator.ms_BlockSize
        public const int DefaultBudgetBytes = 16 * 1024 * 1024;

        /// <summary>Highest crossed warning threshold (0, 50, 75 or 90 percent).</summary>
        public static int Threshold(int usedBlocks, int budgetBlocks)
        {
            if (budgetBlocks <= 0 || usedBlocks <= 0) return 0;
            long percent = 100L * usedBlocks / budgetBlocks;
            return percent >= 90 ? 90 : percent >= 75 ? 75 : percent >= 50 ? 50 : 0;
        }

        public static string FormatSnapshot(string world, int archetypes, int queryShapes, int usedBlocks, int budgetBlocks,
            bool headroomApplied, double elapsedSeconds)
        {
            double usedMiB = usedBlocks * (double)BlockBytes / (1024 * 1024);
            double budgetMiB = budgetBlocks * (double)BlockBytes / (1024 * 1024);
            int percent = budgetBlocks > 0 ? (int)(100L * usedBlocks / budgetBlocks) : -1;
            return string.Format(CultureInfo.InvariantCulture,
                "[EcsQueryBudget] world={0} archetypes={1} queryShapes={2} blocks={3}/{4} usedMiB={5:F2} budgetMiB={6:F0} " +
                "pct={7} legacy16MiBpct={8} headroom={9} t={10:F0}",
                world, archetypes, queryShapes, usedBlocks, budgetBlocks, usedMiB, budgetMiB, percent,
                (int)(100.0 * usedBlocks * BlockBytes / DefaultBudgetBytes), headroomApplied ? 1 : 0, elapsedSeconds);
        }
    }
}
