using System;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
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
        public void AuthoredGroundStagingPrefabIsPersistedForLiveMatch()
        {
            Assert.AreEqual(
                SkirmishGroundStagingPrefabBuilder.PrefabPath,
                SkirmishGroundStagingPrefabAccess.PrefabPath);
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(File.Exists(Path.Combine(projectRoot, SkirmishGroundStagingPrefabAccess.PrefabPath)));
            Assert.IsTrue(File.Exists(Path.Combine(projectRoot, SkirmishGroundStagingPrefabAccess.AuthoredConfigPath)));
            Assert.IsTrue(SkirmishGroundStagingPrefabAccess.TryLoadAuthored(out GameObject staging, out bool owned));
            try
            {
                Assert.IsTrue(SkirmishGroundStagingPrefabAccess.IsUsable(staging));
                Assert.AreNotEqual("Expert Tent", staging.name);
            }
            finally
            {
                if (owned)
                    SkirmishVisualLifecycle.DestroyOwned(staging);
            }
        }

        [Test]
        public void ExpandedLaunchBindsExplicitRegistryInsteadOfStandIns()
        {
            using var world = new World(nameof(ExpandedLaunchBindsExplicitRegistryInsteadOfStandIns));
            EntityManager em = world.EntityManager;
            UnitPrefabRegistryAuthoringConfig registry = ScriptableObject.CreateInstance<UnitPrefabRegistryAuthoringConfig>();
            SkirmishVisualPrefabCatalog standIn = SkirmishVisualPrefabCatalog.CreateS002TestRegistry();
            try
            {
                registry.UnitSpawnPrefabs.Add(standIn.TryGet("Unit_Veh_Tank_USA", out GameObject tank) ? tank : null);
                registry.UnitSpawnPrefabs.Add(standIn.TryGet("Unit_Veh_APC_Slow", out GameObject apc) ? apc : null);
                registry.UnitSpawnPrefabs.Add(
                    standIn.TryGet("Unit_Chr_Soldier_Male_02_Alt_04", out GameObject rifle) ? rifle : null);
                registry.UnitSpawnPrefabs.Add(
                    standIn.TryGet("Building_GroundStaging", out GameObject staging) ? staging : null);
                CompileAndSpawn(em, out Entity session, out SkirmishResolvedSetup setup, registry);
                Assert.IsTrue(em.HasComponent<SkirmishVisualPrefabCatalogRecord>(session));
                SkirmishVisualPrefabCatalog bound =
                    em.GetComponentObject<SkirmishVisualPrefabCatalogRecord>(session).Catalog;
                Assert.IsNotNull(bound);
                Assert.IsTrue(bound.BoundFromRegistry);
                int spawned = SkirmishVisualSpawnService.AttachMissing(em, session, setup);
                Assert.Greater(spawned, 0);
                Assert.IsTrue(HasVisibleKey(em, "Unit_Veh_Tank_USA"));
                Assert.IsTrue(HasVisibleKey(em, "Unit_Veh_APC_Slow"));
                Assert.IsTrue(HasVisibleKey(em, "Unit_Chr_Soldier_Male_02_Alt_04"));
                Assert.IsTrue(HasVisibleKey(em, "Building_GroundStaging"));
                Assert.IsTrue(FirstSpawnedFromRegistry(em, "Unit_Veh_Tank_USA"));
            }
            finally
            {
                if (em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).CalculateEntityCount() == 1)
                {
                    Entity session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
                    SkirmishScenarioSpawnSystem.DestroyAttemptOwned(
                        em, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
                    if (em.HasComponent<SkirmishVisualPrefabCatalogRecord>(session))
                        em.GetComponentObject<SkirmishVisualPrefabCatalogRecord>(session).Catalog?.Dispose();
                }

                standIn.Dispose();
                UnityEngine.Object.DestroyImmediate(registry);
            }
        }

        [Test]
        public void VisualSpawnSystemAttachesOutsideLiveQuery()
        {
            using var world = new World(nameof(VisualSpawnSystemAttachesOutsideLiveQuery));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishVisualPrefabCatalog catalog = SkirmishVisualPrefabCatalog.CreateS002TestRegistry();
            try
            {
                SkirmishVisualSpawnService.BindCatalog(em, session, catalog);
                SkirmishExpandedSessionComponent sessionComponent =
                    em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                sessionComponent.SpawnComplete = 1;
                sessionComponent.Phase = SkirmishSessionPhase.Playing;
                em.SetComponentData(session, sessionComponent);

                world.GetOrCreateSystem<SkirmishVisualSpawnSystem>().Update(world.Unmanaged);

                Assert.IsTrue(HasVisibleKey(em, "Unit_Veh_Tank_USA"));
                Assert.IsTrue(HasVisibleKey(em, "Building_GroundStaging"));
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

        [Test]
        public void S003RegistrySpawnsAaAndLightHelicopterFromExistingKeys()
        {
            using var world = new World(nameof(S003RegistrySpawnsAaAndLightHelicopterFromExistingKeys));
            EntityManager em = world.EntityManager;
            SkirmishVisualPrefabCatalog standIn = SkirmishVisualPrefabCatalog.CreateS003AirRegistry();
            UnitPrefabRegistryAuthoringConfig registry = ScriptableObject.CreateInstance<UnitPrefabRegistryAuthoringConfig>();
            try
            {
                AddPrefab(registry, standIn, "Unit_Veh_Missle_Launcher_Air");
                AddPrefab(registry, standIn, "Unit_Veh_Helicopter_Attack_Small");
                AddPrefab(registry, standIn, "Building_GroundStaging");
                AddPrefab(registry, standIn, "Building_Helipad");
                AddPrefab(registry, standIn, "Building_Barrack");
                AddPrefab(registry, standIn, "Unit_Chr_Soldier_Male_02_Alt_04");
                AddPrefab(registry, standIn, "Unit_Chr_Ghillie_Male_01");
                AddPrefab(registry, standIn, "Unit_Veh_Light_Armored_Car");
                AddPrefab(registry, standIn, "Unit_Veh_APC_Slow");
                CompileAndSpawnS003(em, out Entity session, out _, registry);
                SkirmishVisualPrefabCatalog bound =
                    em.GetComponentObject<SkirmishVisualPrefabCatalogRecord>(session).Catalog;
                Assert.IsTrue(bound.BoundFromRegistry);
                Assert.IsTrue(bound.Contains("Unit_Veh_Missle_Launcher_Air"));
                Assert.IsTrue(bound.Contains("Unit_Veh_Helicopter_Attack_Small"));
                Assert.IsTrue(bound.Contains("Building_Helipad"));

                SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
                Assert.IsTrue(SkirmishProductionService.TryProduce(
                    em, session, SkirmishRoleIds.AntiAir, 1, authored.ArmyAir, out _));
                Assert.IsTrue(HasVisibleKey(em, "Unit_Veh_Missle_Launcher_Air"));
                Assert.IsTrue(HasVisibleKey(em, "Building_GroundStaging"));
                Assert.IsTrue(HasGroundStagingState(em));
                Assert.IsTrue(FirstSpawnedFromRegistry(em, "Unit_Veh_Missle_Launcher_Air"));
                Assert.IsTrue(SkirmishGroundStagingBuilder.HasRequiredSurfaces(
                    FirstInstance(em, "Building_GroundStaging")));

                var research = em.GetComponentData<SkirmishResearchStateComponent>(session);
                research.Readiness = SkirmishReadinessStage.Established;
                em.SetComponentData(session, research);
                var stock = em.GetComponentData<SkirmishEconomyStockComponent>(session);
                stock.Materials = 450;
                em.SetComponentData(session, stock);
                FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
                Entity pad = SkirmishScenarioSpawnSystem.CreateStructure(
                    em,
                    sessionId,
                    new SkirmishResolvedStructureEntry
                    {
                        FactionId = 1,
                        StructureId = SkirmishStructureIds.Helipad
                    });
                em.AddComponentData(pad, new UnitHealth { Current = 400, Max = 400 });
                Assert.IsTrue(SkirmishProductionService.TryProduce(
                    em, session, SkirmishRoleIds.AttackHeliLight, 1, authored.ArmyAir, out _));
                Assert.IsTrue(HasVisibleKey(em, "Unit_Veh_Helicopter_Attack_Small"));
                Assert.IsTrue(HasVisibleKey(em, "Building_Helipad"));
                Assert.IsTrue(FirstSpawnedFromRegistry(em, "Unit_Veh_Helicopter_Attack_Small"));
            }
            finally
            {
                if (em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).CalculateEntityCount() == 1)
                {
                    Entity session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
                    SkirmishScenarioSpawnSystem.DestroyAttemptOwned(
                        em, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
                    if (em.HasComponent<SkirmishVisualPrefabCatalogRecord>(session))
                        em.GetComponentObject<SkirmishVisualPrefabCatalogRecord>(session).Catalog?.Dispose();
                }

                standIn.Dispose();
                UnityEngine.Object.DestroyImmediate(registry);
            }
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedVisualTests();
                suite.GroundStagingBuilderExposesQueuesPadsAndIdentity();
                suite.AuthoredGroundStagingPrefabIsPersistedForLiveMatch();
                suite.RegistryInstantiateSpawnsVisibleStartingSet();
                suite.ExpandedLaunchBindsExplicitRegistryInsteadOfStandIns();
                suite.ProducedTankGetsRegistryVisualWithoutTouchingPlayerOnlyStubs();
                suite.VisualSpawnSystemAttachesOutsideLiveQuery();
                suite.CleanupDestroysVisualInstances();
                suite.S003RegistrySpawnsAaAndLightHelicopterFromExistingKeys();
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
            CompileAndSpawn(em, out session, out setup, null);
        }

        private static void CompileAndSpawn(
            EntityManager em,
            out Entity session,
            out SkirmishResolvedSetup setup,
            UnitPrefabRegistryAuthoringConfig registry)
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
                out var reasons,
                registry),
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

        private static void CompileAndSpawnS003(
            EntityManager em,
            out Entity session,
            out SkirmishResolvedSetup setup,
            UnitPrefabRegistryAuthoringConfig registry)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out var matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS003.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                em,
                "S003",
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                SkirmishS003FirstVisit.SeedA,
                authored,
                matrix,
                manifest,
                out setup,
                out _,
                out var reasons,
                registry),
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

        private static void AddPrefab(
            UnitPrefabRegistryAuthoringConfig registry,
            SkirmishVisualPrefabCatalog catalog,
            string key)
        {
            registry.UnitSpawnPrefabs.Add(catalog.TryGet(key, out GameObject prefab) ? prefab : null);
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

        private static bool FirstSpawnedFromRegistry(EntityManager em, string key)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishVisualInstanceRecord), typeof(SkirmishVisualSpawnedComponent));
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var record = em.GetComponentObject<SkirmishVisualInstanceRecord>(entities[i]);
                if (record.PrefabKey != key)
                    continue;
                return em.GetComponentData<SkirmishVisualSpawnedComponent>(entities[i]).FromRegistry != 0;
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
