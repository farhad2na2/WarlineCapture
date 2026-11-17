using System.Linq;
using Game.Configs;
using Game.Editor;
using Game.Skirmish.Contracts;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    public sealed class SkirmishInstancePadPlannerTests
    {
        private static SkirmishResolvedStructureEntry S(byte faction, string id, float x, float z) =>
            new SkirmishResolvedStructureEntry { FactionId = faction, StructureId = id, SpawnWorldX = x, SpawnWorldZ = z };

        private static MeasuredInstancePad M(string id, SkirmishLegalPadKind kind, byte faction, float x, float z, string anchor = null) =>
            new MeasuredInstancePad { Id = id, Kind = kind, Faction = faction, X = x, Z = z, Width = 10, Depth = 8, AnchorRole = anchor ?? ("anchor." + id) };

        private static InstancePadVariant V(SkirmishSizeId size, params SkirmishResolvedStructureEntry[] structures) =>
            new InstancePadVariant { Size = size, Difficulty = SkirmishDifficultyId.Regular, Structures = structures };

        private static bool Footprint(string structureId, out float width, out float depth) { width = 20; depth = 12; return true; }
        private static bool NoFootprint(string structureId, out float width, out float depth) { width = 0; depth = 0; return false; }

        [Test] public void OrdinalsCountPerFactionAndStructureInSetupOrder()
        {
            var structures = new[]
            {
                S(1, SkirmishStructureIds.Barracks, 0, 0), S(2, SkirmishStructureIds.Barracks, 0, 0), S(1, SkirmishStructureIds.Refinery, 0, 0),
                S(1, SkirmishStructureIds.Refinery, 0, 0), S(2, SkirmishStructureIds.Refinery, 0, 0)
            };
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 0 }, SkirmishInstancePadPlanner.Ordinals(structures));
        }

        [Test] public void MeasuredPadsAreAssignedInDraftOrderPerFactionAndKindAndStayStableAcrossVariants()
        {
            var measured = new[]
            {
                M("p.oil", SkirmishLegalPadKind.Supply, 1, 10, 10), M("p.ref", SkirmishLegalPadKind.Supply, 1, 30, 10),
                M("e.oil", SkirmishLegalPadKind.Supply, 2, 50, 10), M("p.tower", SkirmishLegalPadKind.Service, 1, 10, 30)
            };
            var small = V(SkirmishSizeId.Standard, S(1, SkirmishStructureIds.OilPump, 1, 1), S(2, SkirmishStructureIds.OilPump, 2, 2),
                S(1, SkirmishStructureIds.Refinery, 3, 3), S(1, SkirmishStructureIds.Watchtower, 4, 4));
            var big = V(SkirmishSizeId.War, S(1, SkirmishStructureIds.OilPump, 1, 1), S(2, SkirmishStructureIds.OilPump, 2, 2),
                S(1, SkirmishStructureIds.Refinery, 3, 3), S(1, SkirmishStructureIds.Watchtower, 4, 4));
            var plan = SkirmishInstancePadPlanner.Plan(new[] { small, big }, measured, NoFootprint);
            string Pad(SkirmishSizeId size, byte faction, string structure) =>
                plan.Rows.Single(r => r.Size == size && r.Faction == faction && r.StructureId == structure && r.Ordinal == 0).PadKey;
            foreach (var size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War })
            {
                Assert.AreEqual("m:p.oil", Pad(size, 1, SkirmishStructureIds.OilPump));
                Assert.AreEqual("m:p.ref", Pad(size, 1, SkirmishStructureIds.Refinery));
                Assert.AreEqual("m:e.oil", Pad(size, 2, SkirmishStructureIds.OilPump));
                Assert.AreEqual("m:p.tower", Pad(size, 1, SkirmishStructureIds.Watchtower));
            }
            Assert.AreEqual(4, plan.MeasuredPads);
            Assert.AreEqual(0, plan.DerivedPads);
            Assert.AreEqual(0, plan.UnusedMeasured);
        }

        [Test] public void MeasuredPadsComeBeforeDerivedAndDerivedUsesTheCompilerPositionAndFootprint()
        {
            var measured = new[] { M("p.oil", SkirmishLegalPadKind.Supply, 1, 10, 10), M("unused.dish", SkirmishLegalPadKind.Service, 1, 70, 70) };
            var variant = V(SkirmishSizeId.Standard, S(1, SkirmishStructureIds.OilPump, 1, 1), S(1, SkirmishStructureIds.Refinery, 33.5f, -4.25f));
            var plan = SkirmishInstancePadPlanner.Plan(new[] { variant }, measured, Footprint);
            Assert.AreEqual(2, plan.Rows.Count);
            Assert.AreEqual("m:p.oil", plan.Rows[0].PadKey);
            var derived = plan.Pads.Single(p => !p.Measured);
            Assert.AreEqual("d:1/" + SkirmishStructureIds.Refinery + "/0", plan.Rows[1].PadKey);
            Assert.AreEqual(33.5f, derived.X); Assert.AreEqual(-4.25f, derived.Z);
            Assert.AreEqual(20f, derived.Width); Assert.AreEqual(12f, derived.Depth);
            Assert.AreEqual(SkirmishLegalPadKind.Supply, derived.Kind);
            Assert.AreEqual(1, plan.MeasuredPads); Assert.AreEqual(1, plan.DerivedPads); Assert.AreEqual(1, plan.UnusedMeasured);
        }

        [Test] public void DerivedPadWithoutProvableFootprintFailsWithExplicitMessage()
        {
            var variant = V(SkirmishSizeId.Standard, S(1, SkirmishStructureIds.Refinery, 5, 5));
            var error = Assert.Throws<InstancePadPlanException>(() => SkirmishInstancePadPlanner.Plan(new[] { variant }, null, NoFootprint));
            StringAssert.Contains("footprint", error.Message);
            StringAssert.Contains(SkirmishStructureIds.Refinery, error.Message);
        }

        [Test] public void EveryVariantGetsExactlyOneRowPerStructureInstanceWithUniquePadsAndAnchors()
        {
            var measured = new[] { M("a", SkirmishLegalPadKind.BaseBarracks, 1, 1, 1), M("b", SkirmishLegalPadKind.GroundStaging, 1, 2, 2) };
            var variants = new[]
            {
                V(SkirmishSizeId.Standard, S(1, SkirmishStructureIds.Barracks, 1, 1), S(1, SkirmishStructureIds.GroundStaging, 2, 2),
                    S(2, SkirmishStructureIds.Barracks, 9, 9)),
                V(SkirmishSizeId.LargeWar, S(1, SkirmishStructureIds.Barracks, 1, 1), S(1, SkirmishStructureIds.GroundStaging, 2, 2),
                    S(2, SkirmishStructureIds.Barracks, 9, 9), S(2, SkirmishStructureIds.Barracks, 12, 9))
            };
            var plan = SkirmishInstancePadPlanner.Plan(variants, measured, Footprint);
            foreach (var variant in variants)
            {
                var rows = plan.Rows.Where(r => r.Size == variant.Size).ToArray();
                Assert.AreEqual(variant.Structures.Length, rows.Length);
                Assert.AreEqual(rows.Length, rows.Select(r => r.PadKey).Distinct().Count());
            }
            Assert.AreEqual(2, plan.Rows.Count(r => r.Faction == 2 && r.Size == SkirmishSizeId.LargeWar));
            Assert.AreEqual(2, plan.DerivedPads, "Enemy barracks ordinals 0 and 1 each need their own derived pad.");
        }

        [Test] public void TwoUsedMeasuredPadsOnOneAnchorOrAnUnsupportedKindFail()
        {
            var shared = new[] { M("a", SkirmishLegalPadKind.Supply, 1, 1, 1, "same"), M("b", SkirmishLegalPadKind.Supply, 1, 1, 1, "same") };
            var variant = V(SkirmishSizeId.Standard, S(1, SkirmishStructureIds.OilPump, 1, 1), S(1, SkirmishStructureIds.Refinery, 1, 1));
            Assert.Throws<InstancePadPlanException>(() => SkirmishInstancePadPlanner.Plan(new[] { variant }, shared, Footprint));
            var approach = V(SkirmishSizeId.Standard, S(2, SkirmishStructureIds.DefenseApproachA, 1, 1));
            Assert.Throws<InstancePadPlanException>(() => SkirmishInstancePadPlanner.Plan(new[] { approach }, null, Footprint));
        }

        [Test] public void MeasuredPadKindAndFactionMustMatchTheStructure()
        {
            var measured = new[] { M("svc", SkirmishLegalPadKind.Service, 1, 1, 1), M("enemy.oil", SkirmishLegalPadKind.Supply, 2, 2, 2) };
            var variant = V(SkirmishSizeId.Standard, S(1, SkirmishStructureIds.OilPump, 7, 7));
            var plan = SkirmishInstancePadPlanner.Plan(new[] { variant }, measured, Footprint);
            Assert.IsFalse(plan.Pads.Single().Measured);
            Assert.AreEqual(2, plan.UnusedMeasured);
        }

        [Test] public void DerivedSlugIsStableAndShort()
        {
            Assert.AreEqual("d_1_refinery_module_0", SkirmishInstancePadPlanner.DerivedSlug(1, SkirmishStructureIds.RefineryModule, 0));
        }
    }
}
