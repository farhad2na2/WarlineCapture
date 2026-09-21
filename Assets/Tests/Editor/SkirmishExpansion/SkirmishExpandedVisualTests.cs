using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishExpandedVisualTests
    {
        [Test]
        public void GroundStagingBuilderExposesQueuesPadsAndIdentity()
        {
            GameObject staging = SkirmishGroundStagingBuilder.BuildHierarchy();
            try
            {
                Assert.IsTrue(SkirmishGroundStagingBuilder.HasRequiredSurfaces(staging));
                Assert.AreNotEqual("Expert Tent", staging.name);
                Assert.IsFalse(staging.name.IndexOf("Tent", StringComparison.OrdinalIgnoreCase) >= 0);
                var identity = staging.GetComponent<SkirmishGroundStagingIdentity>();
                Assert.AreEqual(SkirmishStructureIds.GroundStaging, identity.StructureId);
                Assert.AreEqual("Ground Staging", identity.DisplayNameEn);
                Assert.AreEqual(1, identity.VehicleQueues);
                Assert.AreEqual(1, identity.LogisticsQueues);
                Assert.IsFalse(identity.IsExpertTent);
                Assert.IsNotNull(SkirmishGroundStagingBuilder.FindChild(staging.transform, SkirmishGroundStagingBuilder.VehicleQueueName));
                Assert.IsNotNull(SkirmishGroundStagingBuilder.FindChild(staging.transform, SkirmishGroundStagingBuilder.LogisticsQueueName));
                Assert.IsNotNull(SkirmishGroundStagingBuilder.FindChild(staging.transform, SkirmishGroundStagingBuilder.SpawnPadName));
                Assert.IsNotNull(SkirmishGroundStagingBuilder.FindChild(staging.transform, SkirmishGroundStagingBuilder.RallyPadName));
            }
            finally
            {
                SkirmishVisualLifecycle.DestroyOwned(staging);
            }
        }

        [Test]
        public void RegistryInstantiateSpawnsVisibleStartingSet()
        {
            using var world = new World(nameof(RegistryInstantiateSpawnsVisibleStartingSet));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out SkirmishResolvedSetup setup);
            SkirmishVisualPrefabCatalog catalog = SkirmishVisualPrefabCatalog.CreateS002TestRegistry();
            try
            {
                SkirmishVisualSpawnService.BindCatalog(em, session, catalog);
                int spawned = SkirmishVisualSpawnService.AttachMissing(em, session, setup);
                Assert.Greater(spawned, 0);
                Assert.AreEqual(0, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SpawnVisualPending);
                Assert.IsTrue(HasVisibleKey(em, "Unit_Veh_Tank_USA"));
                Assert.IsTrue(HasVisibleKey(em, "Unit_Veh_APC_Slow"));
                Assert.IsTrue(HasVisibleKey(em, "Unit_Chr_Soldier_Male_02_Alt_04"));
                Assert.IsTrue(HasVisibleKey(em, "Building_GroundStaging"));
                Assert.IsTrue(HasGroundStagingState(em));
                GameObject staging = FirstInstance(em, "Building_GroundStaging");
                Assert.IsTrue(staging.activeInHierarchy);
                Assert.IsTrue(SkirmishGroundStagingBuilder.HasRequiredSurfaces(staging));
            }
            finally
            {
                SkirmishScenarioSpawnSystem.DestroyAttemptOwned(
                    em, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
                catalog.Dispose();
            }
        }

        [Test]
        public void ProducedTankGetsRegistryVisualWithoutTouchingPlayerOnlyStubs()
        {
            using var world = new World(nameof(ProducedTankGetsRegistryVisualWithoutTouchingPlayerOnlyStubs));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            SkirmishVisualPrefabCatalog catalog = SkirmishVisualPrefabCatalog.CreateS002TestRegistry();
            try
            {
                SkirmishVisualSpawnService.BindCatalog(em, session, catalog);
                Assert.IsTrue(SkirmishProductionService.TryProduce(
                    em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out _));
                Assert.IsTrue(HasVisibleKey(em, "Unit_Veh_Tank_USA"));
            }
            finally
            {
                SkirmishScenarioSpawnSystem.DestroyAttemptOwned(
                    em, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
                catalog.Dispose();
            }
        }

        [Test]
        public void CleanupDestroysVisualInstances()
        {
            using var world = new World(nameof(CleanupDestroysVisualInstances));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out SkirmishResolvedSetup setup);
            SkirmishVisualPrefabCatalog catalog = SkirmishVisualPrefabCatalog.CreateS002TestRegistry();
            SkirmishVisualSpawnService.BindCatalog(em, session, catalog);
            SkirmishVisualSpawnService.AttachMissing(em, session, setup);
            GameObject tank = FirstInstance(em, "Unit_Veh_Tank_USA");
            Assert.IsNotNull(tank);
            SkirmishScenarioSpawnSystem.DestroyAttemptOwned(
                em, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
            Assert.IsTrue(tank == null);
            catalog.Dispose();
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedVisualTests();
                suite.GroundStagingBuilderExposesQueuesPadsAndIdentity();
                suite.RegistryInstantiateSpawnsVisibleStartingSet();
                suite.ProducedTankGetsRegistryVisualWithoutTouchingPlayerOnlyStubs();
                suite.CleanupDestroysVisualInstances();
                Debug.Log("[SkirmishExpandedVisualTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishExpandedVisualTests] result=Failed\n" + exception);
                throw;
            }
        }

        private static void CompileAndSpawn(EntityManager em, out Entity session, out SkirmishResolvedSetup setup)
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out var matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS002.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                em,
                "S002",
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                104731,
                authored,
                matrix,
                manifest,
                out setup,
                out _,
                out var reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(
                em, session, setup, out SkirmishReasonCode reason, out byte visualPending), reason.ToString());
            Assert.AreEqual(0, visualPending);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishRosterProjectionSystem.Apply(
                em,
                owned,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                setup);
        }

        private static bool HasVisibleKey(EntityManager em, string key)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishVisualInstanceRecord), typeof(SkirmishVisualSpawnedComponent));
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var record = em.GetComponentObject<SkirmishVisualInstanceRecord>(entities[i]);
                if (record.PrefabKey == key && record.Instance != null && record.Instance.activeInHierarchy)
                    return true;
            }

            return false;
        }

        private static bool HasGroundStagingState(EntityManager em)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishGroundStagingStateComponent));
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var state = em.GetComponentData<SkirmishGroundStagingStateComponent>(entities[i]);
                if (state.VehicleQueues == 1 && state.LogisticsQueues == 1)
                    return true;
            }

            return false;
        }

        private static GameObject FirstInstance(EntityManager em, string key)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishVisualInstanceRecord));
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var record = em.GetComponentObject<SkirmishVisualInstanceRecord>(entities[i]);
                if (record.PrefabKey == key)
                    return record.Instance;
            }

            return null;
        }
    }
}
