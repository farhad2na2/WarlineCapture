#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;

namespace Game.Tests.Editor
{
    /// <summary>
    /// S026 owner playtest (2026-11-17): the World's 16 MiB EntityQueryManager block allocator ran out at 9:36 and the
    /// deadline settlement then threw. These tests cover the toggle census, the budget arithmetic and the headroom swap on
    /// a real (empty) World.
    /// </summary>
    public sealed class EcsQueryBudgetTests
    {
        [Test]
        public void CensusCountsOnlyTypesWhoseRemovalIsAnExistingArchetype()
        {
            var archetypes = new List<int[]>
            {
                new[] { 1, 2, 3 },        // base unit
                new[] { 3, 1, 2, 7 },     // + 7 (toggled: base exists)
                new[] { 1, 2, 3, 8 },     // + 8 (toggled: base exists)
                new[] { 1, 2, 3, 7, 8 },  // + 7 + 8 (both toggles: each removal exists)
                new[] { 4, 5 },           // unrelated family
                new[] { 4, 5, 9 },        // + 9 (toggled)
            };
            var rows = EcsArchetypeToggleCensus.Rank(archetypes, 10);
            var pairs = new Dictionary<int, int>();
            foreach (var row in rows) pairs[row.TypeIndex] = row.Pairs;
            Assert.AreEqual(2, pairs[7], "7 is toggled on {1,2,3} and on {1,2,3,8}");
            Assert.AreEqual(2, pairs[8]);
            Assert.AreEqual(1, pairs[9]);
            Assert.IsFalse(pairs.ContainsKey(1), "1 is never removed: {2,3} does not exist");
            Assert.IsFalse(pairs.ContainsKey(4));
            Assert.AreEqual(7, rows[0].TypeIndex, "ties rank by type index");
            Assert.AreEqual("7:2/2,8:2/2,9:1/1", EcsArchetypeToggleCensus.Format(rows, null));
        }

        [Test]
        public void CensusTopLimitsRows()
        {
            var archetypes = new List<int[]> { new[] { 1 }, new[] { 1, 2 }, new[] { 1, 3 }, new[] { 1, 4 } };
            Assert.AreEqual(2, EcsArchetypeToggleCensus.Rank(archetypes, 2).Count);
            Assert.AreEqual(0, EcsArchetypeToggleCensus.Rank(null, 2).Count);
        }

        [Test]
        public void ThresholdsAndFormat()
        {
            Assert.AreEqual(0, EcsQueryBudgetMath.Threshold(127, 256));
            Assert.AreEqual(50, EcsQueryBudgetMath.Threshold(128, 256));
            Assert.AreEqual(75, EcsQueryBudgetMath.Threshold(192, 256));
            Assert.AreEqual(90, EcsQueryBudgetMath.Threshold(256, 256));
            Assert.AreEqual(0, EcsQueryBudgetMath.Threshold(5, 0));
            string line = EcsQueryBudgetMath.FormatSnapshot("Default World", 4000, 1700, 256, 2048, true, 600);
            StringAssert.StartsWith("[EcsQueryBudget] world=Default World archetypes=4000 queryShapes=1700 blocks=256/2048 usedMiB=16.00 budgetMiB=128 pct=12 legacy16MiBpct=100 headroom=1", line);
        }

        [Test]
        public void TemporarySwitchOffLeavesTheBudgetUnchanged()
        {
            var world = new World("EcsQueryBudgetSwitchOff");
            bool previous = EcsQueryBudget.TemporaryHeadroomEnabled;
            try
            {
                EcsQueryBudget.TemporaryHeadroomEnabled = false;
                Assert.IsFalse(EcsQueryBudget.TryEnsureHeadroom(world, EcsQueryBudget.HeadroomBudgetBytes, out string detail));
                Assert.AreEqual("switchOff", detail);
                Assert.IsTrue(EcsQueryBudget.TryRead(world, out var snapshot));
                Assert.AreEqual(EcsQueryBudgetMath.DefaultBudgetBytes / EcsQueryBudgetMath.BlockBytes, snapshot.BudgetBlocks);
            }
            finally
            {
                EcsQueryBudget.TemporaryHeadroomEnabled = previous;
                world.Dispose();
            }
        }

        [Test]
        public void HeadroomWidensAnEmptyWorldAndTheWorldStillDisposes()
        {
            var world = new World("EcsQueryBudgetTests");
            try
            {
                Assert.IsTrue(EcsQueryBudget.TryRead(world, out var before), "read");
                Assert.IsTrue(before.Valid, "layout: " + EcsQueryBudget.LayoutError);
                Assert.AreEqual(EcsQueryBudgetMath.DefaultBudgetBytes / EcsQueryBudgetMath.BlockBytes, before.BudgetBlocks);
                using (var early = world.EntityManager.CreateEntityQuery(typeof(EcsQueryBudgetProbeA))) { }

                Assert.IsTrue(EcsQueryBudget.TryEnsureHeadroom(world, EcsQueryBudget.HeadroomBudgetBytes, out string detail), detail);
                Assert.IsTrue(EcsQueryBudget.TryRead(world, out var after));
                Assert.AreEqual(EcsQueryBudget.HeadroomBudgetBytes / EcsQueryBudgetMath.BlockBytes, after.BudgetBlocks);
                Assert.IsTrue(after.HeadroomApplied);
                Assert.IsTrue(EcsQueryBudget.TryEnsureHeadroom(world, EcsQueryBudget.HeadroomBudgetBytes, out detail));
                Assert.AreEqual("alreadyApplied", detail);

                // Old query data (in retired blocks) and new allocations (in the new allocator) both keep working.
                var em = world.EntityManager;
                var entity = em.CreateEntity(typeof(EcsQueryBudgetProbeA));
                em.AddComponent<EcsQueryBudgetProbeB>(entity);
                using (var old = em.CreateEntityQuery(typeof(EcsQueryBudgetProbeA)))
                using (var fresh = em.CreateEntityQuery(typeof(EcsQueryBudgetProbeA), typeof(EcsQueryBudgetProbeB)))
                {
                    Assert.AreEqual(1, old.CalculateEntityCount());
                    Assert.AreEqual(1, fresh.CalculateEntityCount());
                }
                em.RemoveComponent<EcsQueryBudgetProbeA>(entity);
                Assert.IsTrue(EcsQueryBudget.TryRead(world, out var used));
                Assert.GreaterOrEqual(used.UsedBlocks, 1);
            }
            finally
            {
                world.Dispose();
                EcsQueryBudget.ReleaseRetiredAllocators();
            }
            Assert.IsFalse(EcsQueryBudget.IsHeadroomApplied(world), "retired allocator released after World.Dispose");
        }
    }

    public struct EcsQueryBudgetProbeA : IComponentData { public int Value; }
    public struct EcsQueryBudgetProbeB : IComponentData { public int Value; }
}
#endif
