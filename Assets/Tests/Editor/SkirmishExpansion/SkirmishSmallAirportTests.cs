using System;
using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// W34 (owner-approved 2026-11-19/21): building.airport_small produces fighter and strike only;
    /// transport_plane needs the full Airport. No mission offers the small airport yet, and the full
    /// Airport, its missions and S004's exclusion are unchanged.
    /// </summary>
    public sealed class SkirmishSmallAirportTests
    {
        private const string Marker = "[SkirmishSmallAirportTests]";
        private static readonly SkirmishRoleKind[] Jets = { SkirmishRoleKind.Fighter, SkirmishRoleKind.Strike };
        private static readonly SkirmishProducerKind[] Airports = { SkirmishProducerKind.Airport, SkirmishProducerKind.SmallAirport };

        public static void RunFocusedValidation()
        {
            try
            {
                var t = new SkirmishSmallAirportTests();
                t.IdAndPrefabMappingRoundTripAndFullAirportMappingIsUnchanged();
                t.FighterAndStrikeQualifyFromEitherAirportTransportPlaneOnlyFromFull();
                t.TransportPlaneNeverResolvesToTheSmallAirport();
                t.SmallAirportMapFormMovesJetsAndWithholdsTransportPlane();
                t.EnemyBuildsTheAirportTypeItsMapOffers();
                t.LiveQueueAndPerceptionResolveTheLivingAirportType();
                t.GateModelListsTheSmallAirportForItsJets();
                t.SmallAirportPrefabMatchesTheSpec();
                t.FullAirportAssetsAndRolesAreUnchanged();
                t.NoMissionOffersTheSmallAirportAndS004KeepsItsExclusion();
                Debug.Log(Marker + " result=Passed tests=10");
            }
            catch (Exception error) { Debug.LogError(Marker + " result=Failed\n" + error); throw; }
        }

        [Test]
        public void IdAndPrefabMappingRoundTripAndFullAirportMappingIsUnchanged()
        {
            Assert.AreEqual("building.airport_small", SkirmishStructureIds.AirportSmall);
            Assert.AreEqual("Building_Airport_Small", SkirmishStructureIds.VisualKey(SkirmishStructureIds.AirportSmall));
            Assert.AreEqual(SkirmishStructureIds.AirportSmall, SkirmishStructureIds.FromVisualKey("Building_Airport_Small"));
            Assert.AreEqual(SkirmishProducerKind.SmallAirport, SkirmishStructureIds.ProducerFor(SkirmishStructureIds.AirportSmall));
            Assert.AreEqual(SkirmishStructureIds.AirportSmall, SkirmishStructureIds.StructureFor(SkirmishProducerKind.SmallAirport));
            Assert.AreEqual("Building_Airport_Small", SkirmishProductionCatalog.ProducerPrefabKey(SkirmishProducerKind.SmallAirport));
            Assert.AreEqual(SkirmishLegalPadKind.AirReturn, Game.Configs.SkirmishMapLayoutBuilder.PadKindForStructure(SkirmishStructureIds.AirportSmall));
            Assert.AreEqual(7, (int)SkirmishProducerKind.SmallAirport, "appended; serialized values unchanged");

            // Full Airport: same id, prefab key, producer and serialized value as before W34.
            Assert.AreEqual(4, (int)SkirmishProducerKind.Airport);
            Assert.AreEqual("building.airport", SkirmishStructureIds.Airport);
            Assert.AreEqual("Building_Airport", SkirmishStructureIds.VisualKey(SkirmishStructureIds.Airport));
            Assert.AreEqual(SkirmishStructureIds.Airport, SkirmishStructureIds.FromVisualKey("Building_Airport"));
            Assert.AreEqual(SkirmishProducerKind.Airport, SkirmishStructureIds.ProducerFor(SkirmishStructureIds.Airport));
            Assert.AreEqual("Building_Airport", SkirmishProductionCatalog.ProducerPrefabKey(SkirmishProducerKind.Airport));
            Assert.AreEqual(SkirmishLegalPadKind.AirReturn, Game.Configs.SkirmishMapLayoutBuilder.PadKindForStructure(SkirmishStructureIds.Airport));
            foreach (SkirmishProducerKind kind in Enum.GetValues(typeof(SkirmishProducerKind)))
            {
                string structure = SkirmishStructureIds.StructureFor(kind);
                if (structure.Length > 0) Assert.AreEqual(kind, SkirmishStructureIds.ProducerFor(structure), kind.ToString());
            }
        }

        [Test]
        public void FighterAndStrikeQualifyFromEitherAirportTransportPlaneOnlyFromFull()
        {
            var army = SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.AirMobile);
            var full = SkirmishRoleOverlayCatalog.CreateAirMobileSlice();
            var small = SkirmishRoleOverlayCatalog.WithSmallAirport(full);
            foreach (var jet in Jets)
            {
                foreach (var overlays in new[] { full, small })
                {
                    var own = overlays == full ? SkirmishProducerKind.Airport : SkirmishProducerKind.SmallAirport;
                    var other = own == SkirmishProducerKind.Airport ? SkirmishProducerKind.SmallAirport : SkirmishProducerKind.Airport;
                    AssertAccepted(Evaluate(army, overlays, jet, airport: true, small: true), own, jet + " prefers its overlay airport");
                    AssertAccepted(Evaluate(army, overlays, jet, airport: own == SkirmishProducerKind.Airport,
                        small: own == SkirmishProducerKind.SmallAirport), own, jet + " own");
                    AssertAccepted(Evaluate(army, overlays, jet, airport: other == SkirmishProducerKind.Airport,
                        small: other == SkirmishProducerKind.SmallAirport), other, jet + " either airport qualifies");
                    var none = Evaluate(army, overlays, jet, airport: false, small: false);
                    Assert.IsFalse(none.Accepted, jet + " no airport");
                    Assert.AreEqual(SkirmishReasonCode.MissingProducer, none.Reason);
                    Assert.AreEqual(own == SkirmishProducerKind.Airport ? "producer.airport" : "producer.airport_small", none.Field);
                }
            }

            // transport_plane: only the full Airport; a small airport alone never qualifies.
            AssertAccepted(Evaluate(army, full, SkirmishRoleKind.TransportPlane, airport: true, small: true),
                SkirmishProducerKind.Airport, "transport with both");
            var smallOnly = Evaluate(army, full, SkirmishRoleKind.TransportPlane, airport: false, small: true);
            Assert.IsFalse(smallOnly.Accepted);
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, smallOnly.Reason);
            Assert.AreEqual("producer.airport", smallOnly.Field);
            // A small-airport map withholds transport_plane altogether.
            var withheld = Evaluate(army, small, SkirmishRoleKind.TransportPlane, airport: true, small: true);
            Assert.IsFalse(withheld.Accepted);
            Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, withheld.Reason);
            Assert.AreEqual("overlay", withheld.Field);

            // Non-airport producers keep their single-producer rule.
            var heli = Evaluate(army, full, SkirmishRoleKind.AttackHeli, airport: true, small: true);
            Assert.IsFalse(heli.Accepted);
            Assert.AreEqual("producer.helipad", heli.Field);
            Assert.IsTrue(Evaluate(army, full, SkirmishRoleKind.Drone, airport: false, small: true, intel: true).Accepted,
                "drones stay on the satellite dish");
            Assert.AreEqual("producer.intel_station", Evaluate(army, full, SkirmishRoleKind.Drone, airport: true, small: true).Field);
        }

        [Test]
        public void TransportPlaneNeverResolvesToTheSmallAirport()
        {
            var army = SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.AirMobile);
            var malformed = SkirmishRoleOverlayCatalog.CreateAirMobileSlice();
            for (int i = 0; i < malformed.Length; i++)
                if (malformed[i].RoleKind == SkirmishRoleKind.TransportPlane) malformed[i].Producer = SkirmishProducerKind.SmallAirport;
            foreach (bool airport in new[] { false, true })
            foreach (bool small in new[] { false, true })
            {
                var decision = Evaluate(army, malformed, SkirmishRoleKind.TransportPlane, airport, small);
                Assert.IsFalse(decision.Accepted, "malformed small transport airport=" + airport + " small=" + small);
                Assert.AreNotEqual(SkirmishProducerKind.SmallAirport, decision.Producer);
                foreach (var overlayProducer in Airports)
                {
                    Assert.AreNotEqual(SkirmishProducerKind.SmallAirport, SkirmishAirportTypes.Resolve(
                        overlayProducer, SkirmishRoleKind.TransportPlane, airport, small));
                    Assert.IsFalse(SkirmishAirportTypes.Serves(SkirmishProducerKind.SmallAirport,
                        SkirmishRoleKind.TransportPlane, overlayProducer));
                }
            }
            for (int k = 1; k <= (int)SkirmishRoleKind.ConvoyLogic; k++)
            {
                var role = (SkirmishRoleKind)k;
                Assert.AreEqual(role == SkirmishRoleKind.Fighter || role == SkirmishRoleKind.Strike,
                    SkirmishAirportTypes.SmallAirportServes(role), role.ToString());
            }
        }

        [Test]
        public void SmallAirportMapFormMovesJetsAndWithholdsTransportPlane()
        {
            var full = SkirmishRoleOverlayCatalog.CreateAirMobileSlice();
            var small = SkirmishRoleOverlayCatalog.WithSmallAirport(full);
            Assert.AreEqual(full.Length - 1, small.Length);
            Assert.IsFalse(SkirmishRoleOverlayCatalog.TryGet(small, SkirmishRoleKind.TransportPlane, out _));
            foreach (var overlay in full)
            {
                if (overlay.RoleKind == SkirmishRoleKind.TransportPlane) continue;
                Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(small, overlay.RoleKind, out var moved), overlay.RoleId);
                var expected = overlay;
                if (overlay.Producer == SkirmishProducerKind.Airport) expected.Producer = SkirmishProducerKind.SmallAirport;
                Assert.AreEqual(expected, moved, "only the producer of fighter/strike changes: " + overlay.RoleId);
            }
            Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(small, SkirmishRoleKind.Drone, out var drone));
            Assert.AreEqual(SkirmishProducerKind.IntelStation, drone.Producer);
            var ground = SkirmishRoleOverlayCatalog.CreateS002GroundSlice();
            Assert.AreSame(ground, SkirmishRoleOverlayCatalog.WithSmallAirport(ground), "no airport role keeps the array");
            // The source slice itself is not modified.
            Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(full, SkirmishRoleKind.Fighter, out var fighter));
            Assert.AreEqual(SkirmishProducerKind.Airport, fighter.Producer);
        }

        [Test]
        public void EnemyBuildsTheAirportTypeItsMapOffers()
        {
            var full = SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice();
            var small = SkirmishRoleOverlayCatalog.WithSmallAirport(full);
            foreach (var jet in Jets)
            {
                Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(full, jet, out var fullJet));
                Assert.AreEqual(SkirmishStructureIds.Airport, SkirmishEnemyEconomicCoordinator.ConstructionStructureFor(fullJet));
                Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(small, jet, out var smallJet));
                Assert.AreEqual(SkirmishStructureIds.AirportSmall, SkirmishEnemyEconomicCoordinator.ConstructionStructureFor(smallJet));
            }
            Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(full, SkirmishRoleKind.TransportPlane, out var transport));
            Assert.AreEqual(SkirmishStructureIds.Airport, SkirmishEnemyEconomicCoordinator.ConstructionStructureFor(transport));
            transport.Producer = SkirmishProducerKind.SmallAirport;
            Assert.AreEqual(string.Empty, SkirmishEnemyEconomicCoordinator.ConstructionStructureFor(transport),
                "the enemy never builds a small airport for transport_plane");
            // Every existing overlay keeps its previous construction target.
            foreach (var overlay in full)
                Assert.AreEqual(SkirmishStructureIds.FromVisualKey(SkirmishProductionCatalog.ProducerPrefabKey(overlay.Producer)),
                    SkirmishEnemyEconomicCoordinator.ConstructionStructureFor(overlay), overlay.RoleId);
        }

        [Test]
        public void LiveQueueAndPerceptionResolveTheLivingAirportType()
        {
            using var world = new World("SmallAirportQueue");
            var em = world.EntityManager;
            var setup = new SkirmishResolvedSetup
            {
                CatalogId = "small.airport.fixture", ArmyProfileId = SkirmishArmyProfileId.AirMobile,
                Readiness = SkirmishReadinessStage.FullArsenal, LogisticsSupportCapEach = 2,
                RoleOverlays = SkirmishRoleOverlayCatalog.CreateAirMobileSlice()
            };
            var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishEnemyStockComponent),
                typeof(SkirmishEnemyCapacityComponent));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent { SessionId = "small.airport.attempt", Phase = SkirmishSessionPhase.Playing, SpawnComplete = 1 });
            em.SetComponentData(session, new SkirmishEnemyStockComponent { Materials = 5000, Fuel = 5000 });
            em.SetComponentData(session, new SkirmishEnemyCapacityComponent { InfantryCap = 48, GroundCap = 8, AirCap = 8, SupplyCap = 200 });
            em.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = setup });
            var army = SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);

            Structure(em, SkirmishStructureIds.AirportSmall, "small");
            Assert.IsTrue(SkirmishProductionService.HasLivingProducer(em, session, SkirmishProducerKind.SmallAirport, 2));
            Assert.IsFalse(SkirmishProductionService.HasLivingProducer(em, session, SkirmishProducerKind.Airport, 2));
            foreach (var jet in Jets)
            {
                var quote = Quote(em, session, army, jet);
                AssertAccepted(quote, SkirmishProducerKind.SmallAirport, jet + " from the small airport");
                Assert.IsTrue(SkirmishProductionService.HasLivingProducerFor(em, session, Overlay(setup, jet), 2));
            }
            var transport = Quote(em, session, army, SkirmishRoleKind.TransportPlane);
            Assert.IsFalse(transport.Accepted);
            Assert.AreEqual("producer.airport", transport.Field);
            Assert.IsFalse(SkirmishProductionService.HasLivingProducerFor(em, session, Overlay(setup, SkirmishRoleKind.TransportPlane), 2));
            Assert.IsFalse(SkirmishProductionService.HasLivingProducerFor(em, session, Overlay(setup, SkirmishRoleKind.AttackHeli), 2));

            var perception = new SkirmishPublicPerception
            {
                OwnMaterials = 5000, OwnFuel = 5000, AirCap = 8, OwnSupplyCap = 200, SmallAirportPresent = true, Playing = true
            };
            Assert.IsTrue(SkirmishStrategyScoring.TryAfford(army, SkirmishReadinessStage.FullArsenal, setup.RoleOverlays,
                perception, SkirmishRoleIds.Fighter, SkirmishRoleKind.Fighter));
            Assert.IsFalse(SkirmishStrategyScoring.TryAfford(army, SkirmishReadinessStage.FullArsenal, setup.RoleOverlays,
                perception, SkirmishRoleIds.TransportPlane, SkirmishRoleKind.TransportPlane));

            Structure(em, SkirmishStructureIds.Airport, "full");
            AssertAccepted(Quote(em, session, army, SkirmishRoleKind.TransportPlane), SkirmishProducerKind.Airport, "transport from the full Airport");
            foreach (var jet in Jets)
                AssertAccepted(Quote(em, session, army, jet), SkirmishProducerKind.Airport, jet + " keeps the overlay's full Airport");
        }

        [Test]
        public void GateModelListsTheSmallAirportForItsJets()
        {
            var setup = Compile("S003", SkirmishSizeId.Standard, SkirmishS003FirstVisit.SeedA);
            setup.RoleOverlays = SkirmishRoleOverlayCatalog.WithSmallAirport(setup.RoleOverlays);
            var baseline = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Skirmish/Construction.asset");
            var authored = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
            var smallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmallAirportPrefabBuilder.PrefabPath);
            Assert.IsNotNull(baseline);
            Assert.IsNotNull(authored);
            Assert.IsNotNull(smallPrefab);
            var scene = UnityEngine.Object.Instantiate(authored);
            try
            {
                scene.Spawnables.Add(smallPrefab);
                using var catalog = SkirmishProductionCatalog.Create(setup, baseline, scene);
                Assert.IsTrue(catalog.TryGetRolePrefabs(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.Fighter), out var producer, out _));
                Assert.AreSame(smallPrefab, producer);
                Assert.IsFalse(catalog.TryRole(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.TransportPlane), out _));
                var available = new SortedSet<string>(StringComparer.Ordinal);
                SkirmishS002AriaRunHarness.GateAvailableTypes(setup, catalog, available);
                CollectionAssert.Contains(available, SkirmishStructureIds.AirportSmall);
                CollectionAssert.Contains(available, SkirmishRoleIds.Fighter);
                CollectionAssert.Contains(available, SkirmishRoleIds.Strike);
                CollectionAssert.DoesNotContain(available, SkirmishRoleIds.TransportPlane);
                CollectionAssert.DoesNotContain(available, SkirmishStructureIds.Airport);
            }
            finally { UnityEngine.Object.DestroyImmediate(scene); }
        }

        [Test]
        public void SmallAirportPrefabMatchesTheSpec()
        {
            var check = SmallAirportPrefabBuilder.Verify();
            Assert.AreEqual(new Vector2Int(120, 40), check.RuntimeFootprint);
            Assert.That(check.RunwayLength, Is.InRange(80f, 120f));
            Assert.AreEqual(check.Spawns, check.SpawnsInside);
            CollectionAssert.AreEquivalent(new[] { "Unit_Veh_Jet_01", "Unit_Veh_Jet_02" }, check.Productions);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmallAirportPrefabBuilder.PrefabPath);
            Assert.IsTrue(BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata(prefab, out var metadata));
            Assert.AreEqual(SmallAirportPrefabBuilder.DisplayName, metadata.DisplayName);
            Assert.AreEqual(new Vector2Int(120, 40), metadata.FootprintCells);
            Assert.IsNotNull(metadata.DestroyedVisualPrefab);
            Assert.AreEqual("Building_Airport_Small_Destroyed", metadata.DestroyedVisualPrefab.name);
        }

        [Test]
        public void FullAirportAssetsAndRolesAreUnchanged()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmallAirportPrefabBuilder.BasePrefabPath);
            var config = AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(SmallAirportPrefabBuilder.BaseConfigPath);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(config);
            Assert.AreEqual("Airport", config.DisplayName);
            var productions = new List<string>();
            foreach (var entry in config.Productions) productions.Add(entry.SpawnUnitPrefab.name);
            CollectionAssert.AreEqual(new[] { "Unit_Veh_Plane_Transport", "Unit_Veh_Drone", "Unit_Veh_Jet_01", "Unit_Veh_Jet_02" }, productions);
            Assert.AreEqual("Building_Airport_Destroyed", config.DestroyedVisualPrefab.name);
            var authoring = prefab.GetComponent<BuildingDefinitionAuthoring>();
            Assert.AreSame(config, authoring.AuthoringConfig);
            Assert.AreEqual(new Vector2Int(210, 55), authoring.ConfiguredFootprintCells);
            var definitions = new BuildingDefinitionPrefabSystemHelper();
            definitions.ConfigureAuthoringMetadataResolvers(
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
            var definition = definitions.CreateRuntimeBuildingDefinition(prefab, prefab.name, "", Vector2Int.one, 1, new BuildingRunwaySystem());
            Assert.AreEqual(new Vector2Int(210, 55), definition.FootprintCells);
            Assert.IsTrue(definition.HasRunway);
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(prefab));
            Assert.AreEqual("Assets/Game/Prefabs/Buildings/Building.prefab",
                AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(prefab)));

            // The canonical role catalog and the full-airport slice still name the full Airport.
            foreach (var kind in new[] { SkirmishRoleKind.Fighter, SkirmishRoleKind.Strike, SkirmishRoleKind.TransportPlane })
            {
                Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(SkirmishRoleOverlayCatalog.CreateAirMobileSlice(), kind, out var overlay));
                Assert.AreEqual(SkirmishProducerKind.Airport, overlay.Producer, kind.ToString());
                Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice(), kind, out overlay));
                Assert.AreEqual(SkirmishProducerKind.Airport, overlay.Producer, kind.ToString());
            }
        }

        [Test]
        public void NoMissionOffersTheSmallAirportAndS004KeepsItsExclusion()
        {
            foreach (string path in new[] { "Assets/Game/Configs/Skirmish/Construction.asset", "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset" })
            {
                var config = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(path);
                Assert.IsNotNull(config, path);
                Assert.IsFalse(config.Spawnables.Exists(p => p != null && p.name == "Building_Airport_Small"), path);
            }
            var missions = SkirmishExpansionCatalogConfig.Load();
            Assert.IsNotNull(missions);
            // Unfinished rows may refuse to compile and log why; this scan only reads what compiles.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            int compiled = 0;
            foreach (var definition in missions.Definitions)
            {
                if (definition == null) continue;
                if (!SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, 130365,
                        new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, Matrix(), out var setup, out _))
                    continue;
                compiled++;
                foreach (var overlay in setup.RoleOverlays)
                    Assert.AreNotEqual(SkirmishProducerKind.SmallAirport, overlay.Producer, definition.CatalogId + " " + overlay.RoleId);
                foreach (var structure in setup.Structures)
                    Assert.AreNotEqual(SkirmishStructureIds.AirportSmall, structure.StructureId, definition.CatalogId);
            }
            Assert.Greater(compiled, 0);

            var s004 = Compile("S004", SkirmishSizeId.Standard, SkirmishS004FirstVisit.SeedA);
            foreach (var overlay in s004.RoleOverlays)
                Assert.IsFalse(SkirmishAirportTypes.IsAirport(overlay.Producer), "S004 " + overlay.RoleId);
            foreach (var structure in s004.Structures)
            {
                Assert.AreNotEqual(SkirmishStructureIds.Airport, structure.StructureId);
                Assert.AreNotEqual(SkirmishStructureIds.AirportSmall, structure.StructureId);
            }
        }

        private static SkirmishProductionDecision Evaluate(SkirmishArmyProfileConfig army, SkirmishRoleOverlay[] overlays,
            SkirmishRoleKind role, bool airport, bool small, bool intel = false) =>
            SkirmishProductionEligibility.Evaluate(new SkirmishProductionRequest
            {
                RoleId = SkirmishRoleIds.ToId(role), RoleKind = role, SquadCount = 1,
                BarracksPresent = true, GroundStagingPresent = true, HelipadPresent = false,
                AirportPresent = airport, SmallAirportPresent = small, IntelStationPresent = intel, EnforceStocks = false
            }, army, SkirmishReadinessStage.FullArsenal, overlays);

        private static void AssertAccepted(SkirmishProductionDecision decision, SkirmishProducerKind producer, string label)
        {
            Assert.IsTrue(decision.Accepted, label + " reason=" + decision.Reason + " field=" + decision.Field);
            Assert.AreEqual(producer, decision.Producer, label);
        }

        private static SkirmishProductionDecision Quote(EntityManager em, Entity session, SkirmishArmyProfileConfig army, SkirmishRoleKind role) =>
            SkirmishProductionService.EvaluateQueueAtReadiness(em, session, SkirmishRoleIds.ToId(role), 1, army, 2,
                SkirmishReadinessStage.FullArsenal);

        private static SkirmishRoleOverlay Overlay(SkirmishResolvedSetup setup, SkirmishRoleKind role)
        {
            Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(setup.RoleOverlays, role, out var overlay), role.ToString());
            return overlay;
        }

        private static void Structure(EntityManager em, string structureId, string stable)
        {
            var e = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishStructureIdentityComponent), typeof(UnitHealth));
            em.SetComponentData(e, new SkirmishAttemptOwnedComponent { SessionId = "small.airport.attempt", FactionId = 2, StableObjectId = stable, IsStructure = 1 });
            em.SetComponentData(e, new SkirmishStructureIdentityComponent { Producer = SkirmishStructureIds.ProducerFor(structureId) });
            em.SetComponentData(e, new UnitHealth { Current = 1800, Max = 1800 });
        }

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
            Assert.IsNotNull(catalog);
            Assert.IsTrue(catalog.TryGetDefinition(catalogId, out var definition), catalogId);
            Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, size, seed,
                new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, Matrix(),
                out var setup, out var reasons), catalogId + "/" + size + ": " + string.Join(";", reasons));
            return setup;
        }
    }
}
