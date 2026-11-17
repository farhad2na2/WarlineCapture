using System.Collections.Generic;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;

namespace Game.Tests.Editor
{
    // The Base Assault guard tower is a second faction-1 building.watchtower. Starting structures need unique stable ids or
    // the native player save rejects them (NativeActorIdentityInvalid:building.watchtower.1) and recovery cannot match them.
    public sealed class SkirmishStructureStableIdTests
    {
        private static SkirmishResolvedStructureEntry Entry(byte faction, string id) =>
            new SkirmishResolvedStructureEntry { FactionId = faction, StructureId = id };

        [Test] public void FirstStructureKeepsTheHistoricalId()
        {
            var tower = Entry(1, SkirmishStructureIds.Watchtower);
            Assert.AreEqual("building.watchtower.1", SkirmishScenarioSpawnSystem.StableStructureId(tower));
            Assert.AreEqual("building.watchtower.1", SkirmishScenarioSpawnSystem.StableStructureId(tower, 0));
        }

        [Test] public void SecondStructureOfOneIdAndFactionGetsAnOrdinalSuffix()
        {
            var structures = new[]
            {
                Entry(1, SkirmishStructureIds.Watchtower), Entry(2, SkirmishStructureIds.Watchtower),
                Entry(1, SkirmishStructureIds.WatchtowerApproach), Entry(1, SkirmishStructureIds.Watchtower) // the added guard tower
            };
            var ids = new HashSet<string>();
            for (int i = 0; i < structures.Length; i++)
                Assert.IsTrue(ids.Add(SkirmishScenarioSpawnSystem.StableStructureId(structures[i], SkirmishScenarioSpawnSystem.StructureOrdinal(structures, i))),
                    "duplicate stable id at " + i);
            var ordinals = new int[structures.Length];
            for (int i = 0; i < structures.Length; i++) ordinals[i] = SkirmishScenarioSpawnSystem.StructureOrdinal(structures, i);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 1 }, ordinals);
            Assert.AreEqual("building.watchtower.1.2", SkirmishScenarioSpawnSystem.StableStructureId(structures[3], 1));
        }

        [Test] public void BoundStructuresCarryTheUniqueIds()
        {
            using var world = new World("structure-stable-id");
            var em = world.EntityManager;
            var structures = new[] { Entry(1, SkirmishStructureIds.Watchtower), Entry(1, SkirmishStructureIds.Watchtower) };
            var ids = new HashSet<string>();
            for (int i = 0; i < structures.Length; i++)
            {
                Entity entity = SkirmishScenarioSpawnSystem.CreateStructure(em, "attempt", structures[i], SkirmishScenarioSpawnSystem.StructureOrdinal(structures, i));
                Assert.IsTrue(ids.Add(em.GetComponentData<Game.Components.SkirmishAttemptOwnedComponent>(entity).StableObjectId.ToString()));
            }
            CollectionAssert.AreEquivalent(new[] { "building.watchtower.1", "building.watchtower.1.2" }, ids);
        }
    }
}
