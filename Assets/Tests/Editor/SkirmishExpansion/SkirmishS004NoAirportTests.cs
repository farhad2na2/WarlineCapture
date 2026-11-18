using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Owner decision 2026-11-18: S004 (Desert Base, army A, Established) offers no Airport and no
    /// fixed-wing aircraft to either faction, because the source has no legal Airport site. Army
    /// profile A, the air cap and every other mission stay unchanged.
    /// </summary>
    public sealed class SkirmishS004NoAirportTests
    {
        private const string Marker = "[SkirmishS004NoAirportTests]";
        private const string AirportPrefab = "Building_Airport";
        private static readonly string[] FixedWing =
            { SkirmishRoleIds.Fighter, SkirmishRoleIds.Strike, SkirmishRoleIds.TransportPlane };
        private static readonly SkirmishRoleKind[] FixedWingKinds =
            { SkirmishRoleKind.Fighter, SkirmishRoleKind.Strike, SkirmishRoleKind.TransportPlane };
        private static readonly SkirmishSizeId[] Sizes =
            { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar };

        public static void RunFocusedValidation()
        {
            try
            {
                var tests = new SkirmishS004NoAirportTests();
                tests.S004OffersNoAirportOrFixedWingToEitherFaction();
                tests.S003ArmyAStillOffersAirportAndFixedWing();
                tests.S002AndS026CompileTheirUnchangedArmySlices();
                tests.OnlyS004WithholdsRoles();
                tests.S004DoesNotOfferFullArsenalResearch();
                Debug.Log(Marker + " result=Passed tests=5 s004Sizes=3 fullArsenalOffered=0");
            }
            catch (Exception error) { Debug.LogError(Marker + " result=Failed\n" + error); throw; }
        }

        [Test]
        public void S004OffersNoAirportOrFixedWingToEitherFaction()
        {
            CollectionAssert.AreEquivalent(FixedWing, SkirmishMissionRoleExclusions.UnavailableRoleIds("S004"));
            foreach (var size in Sizes)
            {
                var setup = Compile("S004", size, SkirmishS004FirstVisit.SeedA);
                string label = "S004/" + size;
                Assert.AreEqual(SkirmishArmyProfileId.AirMobile, setup.ArmyProfileId, label);
                // Full Arsenal unlocks only the withheld roles here, so Established is the last useful stage.
                Assert.AreEqual(SkirmishReadinessStage.Established, SkirmishMissionRoleExclusions.HighestUsefulReadiness(setup), label);
                Assert.IsTrue(SkirmishSetupMatrixTable.TryFind(Matrix(), "S004", size, out var row), label);
                Assert.AreEqual(row.TacticalAirCapEach, setup.TacticalAirCapEach, label + " air cap unchanged");
                foreach (var overlay in setup.RoleOverlays)
                {
                    CollectionAssert.DoesNotContain(FixedWing, overlay.RoleId, label);
                    Assert.AreNotEqual(SkirmishProducerKind.Airport, overlay.Producer, label + " " + overlay.RoleId);
                }
                // Helicopters, the Helipad and drones stay.
                foreach (var kept in new[] { SkirmishRoleKind.TransportHeli, SkirmishRoleKind.AttackHeliLight,
                             SkirmishRoleKind.AttackHeli, SkirmishRoleKind.Drone, SkirmishRoleKind.AntiAir })
                    Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(setup.RoleOverlays, kept, out _), label + " keeps " + kept);
                foreach (var structure in setup.Structures)
                    Assert.AreNotEqual(SkirmishStructureIds.Airport, structure.StructureId, label);

                using (var catalog = CreateCatalog(setup))
                {
                    // Build drawer: no Airport card and no jet or transport plane card.
                    Assert.IsFalse(catalog.Config.Spawnables.Exists(p => p != null && p.name == AirportPrefab), label);
                    foreach (var kind in FixedWingKinds)
                        Assert.IsFalse(catalog.TryRole(SkirmishRoleCatalogConfig.RuntimePrefabKey(kind), out _), label + " " + kind);
                    Assert.IsTrue(catalog.TryRole(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.AttackHeli), out _), label);
                    // Gate 3 event=available.
                    var available = new SortedSet<string>(StringComparer.Ordinal);
                    Assert.Greater(SkirmishS002AriaRunHarness.GateAvailableTypes(setup, catalog, available), 0, label);
                    foreach (var id in FixedWing) CollectionAssert.DoesNotContain(available, id, label);
                    CollectionAssert.DoesNotContain(available, SkirmishStructureIds.Airport, label);
                    CollectionAssert.Contains(available, SkirmishRoleIds.AttackHeli, label);
                    CollectionAssert.Contains(available, SkirmishRoleIds.Drone, label);
                }

                // Neither faction can produce fixed-wing, even with an Airport and Full Arsenal.
                var army = SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
                Assert.IsTrue(army.Allows(SkirmishRoleIds.Fighter), "army profile A itself stays unchanged");
                for (int i = 0; i < FixedWing.Length; i++)
                {
                    var decision = EvaluateWithEveryProducer(setup, army, FixedWing[i], FixedWingKinds[i]);
                    Assert.IsFalse(decision.Accepted, label + " " + FixedWing[i]);
                    Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, decision.Reason, label + " " + FixedWing[i]);
                    Assert.AreEqual("overlay", decision.Field, label + " " + FixedWing[i]);
                }
            }
        }

        [Test]
        public void S003ArmyAStillOffersAirportAndFixedWing()
        {
            Assert.IsEmpty(SkirmishMissionRoleExclusions.UnavailableRoleIds("S003"));
            var setup = Compile("S003", SkirmishSizeId.Standard, SkirmishS003FirstVisit.SeedA);
            Assert.AreEqual(SkirmishArmyProfileId.AirMobile, setup.ArmyProfileId);
            Assert.AreEqual(SkirmishReadinessStage.FullArsenal, SkirmishMissionRoleExclusions.HighestUsefulReadiness(setup));
            var army = SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
            for (int i = 0; i < FixedWing.Length; i++)
            {
                Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(setup.RoleOverlays, FixedWingKinds[i], out var overlay), FixedWing[i]);
                Assert.AreEqual(SkirmishProducerKind.Airport, overlay.Producer, FixedWing[i]);
                Assert.IsTrue(EvaluateWithEveryProducer(setup, army, FixedWing[i], FixedWingKinds[i]).Accepted, FixedWing[i]);
            }
            using var catalog = CreateCatalog(setup);
            Assert.IsTrue(catalog.Config.Spawnables.Exists(p => p != null && p.name == AirportPrefab));
            foreach (var kind in FixedWingKinds)
                Assert.IsTrue(catalog.TryRole(SkirmishRoleCatalogConfig.RuntimePrefabKey(kind), out _), kind.ToString());
            var available = new SortedSet<string>(StringComparer.Ordinal);
            SkirmishS002AriaRunHarness.GateAvailableTypes(setup, catalog, available);
            foreach (var id in FixedWing) CollectionAssert.Contains(available, id);
            CollectionAssert.Contains(available, SkirmishStructureIds.Airport);
        }

        [Test]
        public void S002AndS026CompileTheirUnchangedArmySlices()
        {
            var none = Array.Empty<string>();
            var sample = SkirmishRoleOverlayCatalog.CreateAirMobileSlice();
            Assert.AreSame(sample, SkirmishRoleOverlayCatalog.WithoutRoles(sample, none), "Empty exclusion keeps the exact array");
            Assert.IsEmpty(SkirmishMissionRoleExclusions.UnavailableRoleIds("S002"));
            Assert.IsEmpty(SkirmishMissionRoleExclusions.UnavailableRoleIds("S026"));
            foreach (var size in Sizes)
            {
                var s002 = Compile("S002", size, 155926);
                var s026 = Compile("S026", size, 155926);
                AssertSameOverlays(SkirmishRoleOverlayCatalog.CreateS002GroundSlice(), s002.RoleOverlays, "S002/" + size);
                AssertSameOverlays(SkirmishRoleOverlayCatalog.CreateGroundManeuverSlice(), s026.RoleOverlays, "S026/" + size);
                // Their readiness research keeps Full Arsenal exactly as before.
                Assert.AreEqual(SkirmishReadinessStage.FullArsenal, SkirmishMissionRoleExclusions.HighestUsefulReadiness(s002), "S002/" + size);
                Assert.AreEqual(SkirmishReadinessStage.FullArsenal, SkirmishMissionRoleExclusions.HighestUsefulReadiness(s026), "S026/" + size);
            }
        }

        [Test]
        public void S004DoesNotOfferFullArsenalResearch()
        {
            using var world = new Unity.Entities.World(nameof(S004DoesNotOfferFullArsenalResearch));
            var em = world.EntityManager;
            var packaged = SkirmishExpansionCatalogConfig.Load();
            Assert.IsNotNull(packaged);
            Assert.IsTrue(packaged.TryGetDefinition("S004", out var definition));
            Assert.IsTrue(Game.Composition.SkirmishExpandedLaunchResolver.TryCompileAndQueue(em, "S004",
                SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, SkirmishS004FirstVisit.SeedA, packaged.ToAuthoredSet(),
                Matrix(), new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds },
                out var setup, out _, out var reasons), reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            var session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(em, session, setup, out var spawnReason, out _), spawnReason.ToString());
            Assert.AreEqual(SkirmishReadinessStage.Established, em.GetComponentData<SkirmishResearchStateComponent>(session).Readiness);
            Assert.AreEqual(SkirmishReadinessStage.Established, SkirmishResearchService.HighestUsefulReadiness(em, session));
            int materials = SkirmishMaterialsService.Read(em, session, 1);
            Assert.GreaterOrEqual(materials, SkirmishResearchCosts.ReadinessFullArsenalMaterials, "Refusal is not a stock shortfall");
            foreach (byte faction in new byte[] { 1, 2 })
            {
                Assert.IsFalse(SkirmishResearchService.CanQueue(em, session, SkirmishResearchKind.Readiness, faction, out var decision, out _), "faction " + faction);
                Assert.AreEqual(SkirmishReasonCode.AlreadyCompleted, decision.Reason, "faction " + faction);
                Assert.IsFalse(SkirmishResearchService.TryQueue(em, session, SkirmishResearchKind.Readiness, faction, out _), "faction " + faction);
            }
            Assert.AreEqual(materials, SkirmishMaterialsService.Read(em, session, 1), "No Full Arsenal payment");
            Assert.AreEqual(0, em.GetBuffer<SkirmishResearchQueueItem>(session).Length);
        }

        [Test]
        public void OnlyS004WithholdsRoles()
        {
            var catalog = SkirmishExpansionCatalogConfig.Load();
            Assert.IsNotNull(catalog);
            int checkedDefinitions = 0;
            foreach (var definition in catalog.Definitions)
            {
                if (definition == null) continue;
                checkedDefinitions++;
                if (definition.CatalogId == "S004")
                    CollectionAssert.AreEquivalent(FixedWing, SkirmishMissionRoleExclusions.UnavailableRoleIds(definition.CatalogId));
                else
                    Assert.IsEmpty(SkirmishMissionRoleExclusions.UnavailableRoleIds(definition.CatalogId), definition.CatalogId);
            }
            Assert.Greater(checkedDefinitions, 4);
        }

        private static void AssertSameOverlays(SkirmishRoleOverlay[] expected, SkirmishRoleOverlay[] actual, string label)
        {
            Assert.AreEqual(expected.Length, actual.Length, label);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(JsonUtility.ToJson(new OverlayBox { Value = expected[i] }),
                    JsonUtility.ToJson(new OverlayBox { Value = actual[i] }), label + " #" + i);
        }

        [Serializable] private sealed class OverlayBox { public SkirmishRoleOverlay Value; }

        private static SkirmishProductionDecision EvaluateWithEveryProducer(SkirmishResolvedSetup setup,
            SkirmishArmyProfileConfig army, string roleId, SkirmishRoleKind kind) =>
            SkirmishProductionEligibility.Evaluate(new SkirmishProductionRequest
            {
                RoleId = roleId, RoleKind = kind, SquadCount = 1,
                BarracksPresent = true, GroundStagingPresent = true, HelipadPresent = true,
                AirportPresent = true, IntelStationPresent = true, EnforceStocks = false
            }, army, SkirmishReadinessStage.FullArsenal, setup.RoleOverlays);

        private static List<SkirmishSetupMatrixRow> matrix;
        private static List<SkirmishSetupMatrixRow> Matrix()
        {
            if (matrix == null)
                Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out matrix, out string error), error);
            return matrix;
        }

        private static SkirmishResolvedSetup Compile(string catalogId, SkirmishSizeId size, int seed)
        {
            var catalog = SkirmishExpansionCatalogConfig.Load();
            Assert.IsNotNull(catalog, "Packaged skirmish expansion catalog required");
            Assert.IsTrue(catalog.TryGetDefinition(catalogId, out var definition), catalogId);
            Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, size, seed,
                new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, Matrix(),
                out var setup, out var reasons), catalogId + "/" + size + ": " + string.Join(";", reasons));
            return setup;
        }

        private static SkirmishProductionCatalog CreateCatalog(SkirmishResolvedSetup setup)
        {
            var baseline = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Skirmish/Construction.asset");
            var scene = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
            Assert.IsNotNull(baseline);
            Assert.IsNotNull(scene);
            return SkirmishProductionCatalog.Create(setup, baseline, scene);
        }
    }
}
