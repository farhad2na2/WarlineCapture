using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Focused component/registration contracts in owned Worlds, not a mission or
    // normal-input playthrough. No source assets or DefaultWorld are modified.
    public static class OperationMapMissionProductionOwnershipTests
    {
        private static int checks;
        private static readonly GridConfig Grid = new() { Width = 2048, Height = 1024, CellSize = 1 };

        public static void Run()
        {
            checks = 0;
            var definition = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(
                "Assets/Game/Resources/Skirmish/S002Definition.asset");
            Need(definition != null, "actual_source_definition");
            Need(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out _), "actual_source_matrix");
            Need(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard, 104731,
                new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds },
                matrix, out var setup, out _), "actual_s002_compile");
            Need(setup.RoleOverlays.All(x => x.Producer != SkirmishProducerKind.Helipad &&
                x.Producer != SkirmishProducerKind.Airport), "coded_ground_support_and_offensive_air_absent_from_actual_overlay");
            Need(setup.Structures.All(x => SkirmishStructureIds.ProducerFor(x.StructureId) != SkirmishProducerKind.Helipad),
                "coded_ground_no_helipad_grant");
            Need(SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.GroundManeuver)
                .Allows(SkirmishRoleIds.TransportHeli), "army_label_alone_is_not_policy");
            CampaignStrict(setup);
            ContextFailures(setup);
            MissionOwners(setup);
            Debug.Log($"[OperationMapMissionProductionOwnershipTests] Passed checks={checks} scope=OwnedWorldContractsOnly");
        }

        private static void CampaignStrict(SkirmishResolvedSetup setup)
        {
            using var f = new Fixture(setup, expanded: false);
            f.PublishRoot();
            f.GroundRows(32);
            Need(!OperationMapHelipadReadModelUtility.TryBind(f.Em, f.Rows, in Grid, out bool active, out string error) &&
                active && error.Contains("no unique runtime production-slot owner"), "campaign_strict_missing_owners");
            Need(f.Rows.Length == 32, "campaign_trybind_failure_no_partial_mutation");
            OperationMapHelipadReadModelUtility.Bind(f.Em, f.Rows, in Grid);
            Need(f.Rows.Length == 0, "old_32_to_0_campaign_counterexample_preserved");
            f.AddCampaignRows();
            Need(OperationMapHelipadReadModelUtility.TryBind(f.Em, f.Rows, in Grid, out _, out _), "campaign_real_anchor_points_bind");
            var duplicate = f.Rows[0];
            f.Rows.Add(duplicate);
            Need(!OperationMapHelipadReadModelUtility.TryBind(f.Em, f.Rows, in Grid, out _, out _), "campaign_duplicate_owner_reject");

            using var legacy = new Fixture(setup, expanded: false);
            legacy.Em.AddComponentData(legacy.Em.CreateEntity(), new SkirmishMatchState { ScenarioIndex = 0 });
            legacy.PublishRoot(); legacy.GroundRows(1);
            Need(!legacy.Bind(), "genuine_legacy_match_preserves_campaign_strict_binding");

            using var coded = new Fixture(setup);
            coded.PublishRoot();
            coded.GroundRows(32);
            Need(OperationMapHelipadReadModelUtility.TryBind(coded.Em, coded.Rows, in Grid, out active, out error) && active,
                "actual_coded_s002_neutral_campaign_anchors_are_not_attempt_grants");
            Need(coded.Rows.Length == 32, "actual_coded_ground_rows_preserved");
            Need(SkirmishMissionProductionInfrastructure.TryValidateInitialHelipadGrants(
                coded.Em, coded.Rows, in Grid, out _), "actual_coded_initial_grants_satisfied_without_fabricated_helipad");
        }

        private static void ContextFailures(SkirmishResolvedSetup setup)
        {
            using (var f = new Fixture(setup))
            {
                f.GroundRows(1);
                f.Em.RemoveComponent<SkirmishProductionCatalogRecord>(f.Session);
                Need(!f.Bind(), "missing_catalog_fails");
            }
            using (var f = new Fixture(setup))
            {
                f.Em.RemoveComponent<SkirmishResolvedSetupRecord>(f.Session);
                Need(!f.Bind(), "missing_setup_fails");
            }
            using (var f = new Fixture(setup))
            {
                f.Em.RemoveComponent<SkirmishProductionCatalogRecord>(f.Session);
                f.Em.AddComponentObject(f.Em.CreateEntity(), new SkirmishProductionCatalogRecord { Catalog = f.Catalog });
                Need(!f.Bind(), "catalog_on_foreign_entity_fails");
            }
            using (var f = new Fixture(setup))
            {
                f.Em.RemoveComponent<SkirmishMatchState>(f.Session);
                Need(!f.Bind(), "lost_match_fails");
            }
            using (var f = new Fixture(setup))
            {
                f.Em.RemoveComponent<SkirmishExpandedSessionComponent>(f.Session);
                f.Em.RemoveComponent<SkirmishResolvedSetupRecord>(f.Session);
                f.Em.RemoveComponent<SkirmishProductionCatalogRecord>(f.Session);
                Need(!f.Bind(), "coded_expanded_match_lost_all_records_never_campaign_fallback");
            }
            using (var f = new Fixture(setup))
            {
                var state = f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session);
                state.IsLegacy = 1;
                f.Em.SetComponentData(f.Session, state);
                Need(!f.Bind(), "corrupt_legacy_flag_never_campaign_fallback");
            }
            using (var f = new Fixture(setup))
            {
                f.Em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
                Need(!f.Bind(), "duplicate_session_fails");
            }
            using (var f = new Fixture(setup))
            {
                f.Em.AddComponentObject(f.Em.CreateEntity(), new SkirmishProductionCatalogRecord { Catalog = f.Catalog });
                Need(!f.Bind(), "duplicate_catalog_fails");
            }
            using (var f = new Fixture(setup))
            {
                var state = f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session);
                state.Seed++;
                f.Em.SetComponentData(f.Session, state);
                Need(!f.Bind(), "changed_session_identity_fails");
            }
            using (var f = new Fixture(setup))
            using (var foreign = new Fixture(GenericSetup(setup)))
            {
                f.Em.GetComponentObject<SkirmishProductionCatalogRecord>(f.Session).Catalog = foreign.Catalog;
                Need(!f.Bind(), "foreign_compiled_catalog_fails");
            }
        }

        private static void MissionOwners(SkirmishResolvedSetup source)
        {
            var setup = GenericSetup(source);
            Need(setup.RoleOverlays.Any(x => x.RoleKind == SkirmishRoleKind.TransportHeli &&
                x.Producer == SkirmishProducerKind.Helipad), "generic_ground_support_air_is_not_omitted");
            using (var f = new Fixture(setup))
            {
                f.GroundRows(2);
                Need(f.Bind(), "missing_buildable_producer_is_ordinary_unavailability");
                Need(!SkirmishMissionProductionInfrastructure.TryValidateInitialHelipadGrants(f.Em, f.Rows, in Grid, out _),
                    "missing_required_initial_grants_fail_separate_check");
                f.AddRegisteredHelipad(1, constructed: false);
                f.AddRegisteredHelipad(2, constructed: true);
                Need(f.Bind() && f.Rows.Length == 4, "generic_mission_points_not_campaign_coordinates");
                Need(SkirmishMissionProductionInfrastructure.TryValidateInitialHelipadGrants(f.Em, f.Rows, in Grid, out _),
                    "two_actual_registered_mission_owners_satisfy_initial_grants");
                var duplicate = f.Rows[f.Rows.Length - 1];
                f.Rows.Add(duplicate);
                int count = f.Rows.Length;
                Need(!f.Bind() && f.Rows.Length == count, "duplicate_slot_reject_no_mutation");
            }
            using (var f = new Fixture(setup))
            {
                f.GroundRows(2);
                var dead = f.AddRegisteredHelipad(1, constructed: false);
                f.AddRegisteredHelipad(2, constructed: true);
                f.Em.SetComponentData(dead.CombatEntity, new UnitHealth { Current = 0, Max = 500 });
                Need(!dead.IsDestroyed, "health_zero_before_managed_halfsecond_retirement");
                Need(!SkirmishMissionProductionInfrastructure.TryValidateInitialHelipadGrants(f.Em, f.Rows, in Grid, out _) &&
                    f.Rows.Length == 4, "initial_dead_grant_reject_readonly");
                Need(f.Bind() && f.Rows.Length == 3, "dead_real_owner_only_its_helipad_row_removed");
                Need(f.Rows[0].BuildingId.ToString() == "building_barrack" &&
                    f.Rows[1].BuildingId.ToString() == "building_barrack", "ground_rows_survive_dead_helipad");
                Need(f.Bind() && f.Rows.Length == 3, "later_unavailability_does_not_recreate_grant");
            }
            using (var f = new Fixture(setup))
            {
                var owner = f.AddRegisteredHelipad(1, constructed: true);
                var owned = f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(owner.CombatEntity);
                owned.SessionId = new FixedString64Bytes("different-attempt");
                f.Em.SetComponentData(owner.CombatEntity, owned);
                Need(!f.Bind(), "foreign_attempt_owner_reject");
            }
            using (var f = new Fixture(setup))
            {
                var owner = f.AddRegisteredHelipad(1, constructed: true);
                var rows = f.Rows;
                var row = rows[0]; row.SlotIndex = owner.ProductionSpawnLocalPositions.Length; rows[0] = row;
                Need(!f.Bind(), "slot_bounds_from_real_registered_building");
            }
            using (var f = new Fixture(setup))
            {
                var owner = f.AddRegisteredHelipad(1, constructed: true);
                f.Em.RemoveComponent<SkirmishNativeBuildingCheckpointRecord>(owner.CombatEntity);
                Need(!f.Bind(), "missing_registered_owner_record_reject");
            }
            using (var f = new Fixture(setup))
            {
                f.GroundRows(1);
                var dead = f.AddRegisteredHelipad(1, constructed: false);
                f.Em.SetComponentData(dead.CombatEntity, new UnitHealth { Current = 0, Max = 500 });
                f.AddRegisteredHelipad(2, constructed: true);
                var rows = f.Rows;
                var malformed = rows[2]; malformed.WorldPosition.x = float.NaN; rows[2] = malformed;
                Need(!f.Bind() && rows.Length == 3, "validate_every_row_before_pruning_any_dead_row");
            }
            using (var f = new Fixture(setup))
            {
                var owner = f.AddRegisteredHelipad(1, constructed: false);
                Entity duplicate = f.Em.CreateEntity(typeof(RuntimeBuildingCombatInfo), typeof(SkirmishAttemptOwnedComponent),
                    typeof(SkirmishStructureIdentityComponent), typeof(UnitHealth));
                f.Em.SetComponentData(duplicate, f.Em.GetComponentData<RuntimeBuildingCombatInfo>(owner.CombatEntity));
                f.Em.SetComponentData(duplicate, f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(owner.CombatEntity));
                f.Em.SetComponentData(duplicate, f.Em.GetComponentData<SkirmishStructureIdentityComponent>(owner.CombatEntity));
                f.Em.SetComponentData(duplicate, new UnitHealth { Current = 500, Max = 500 });
                f.Em.AddComponentObject(duplicate, new SkirmishNativeBuildingCheckpointRecord
                { Building = new RuntimeBuildingEntity { Id = owner.Id, CombatEntity = duplicate, Instance = owner.Instance,
                    HasOwnerFaction = true, OwnerFactionId = owner.OwnerFactionId,
                    ProductionSpawnLocalPositions = owner.ProductionSpawnLocalPositions } });
                Need(!f.Bind(), "ambiguous_actual_runtime_owner_reject");
            }
        }

        private static SkirmishResolvedSetup GenericSetup(SkirmishResolvedSetup source) => new()
        {
            // Controlled ownership contract, NOT a compiled/admitted S026 world.
            CatalogId = "S026", DefinitionId = "skirmish.s026", ContentVersion = 1,
            SetupHash = 0x71a, Seed = source.Seed, SizeId = source.SizeId, DifficultyId = source.DifficultyId,
            ObjectiveKind = source.ObjectiveKind, ArmyProfileId = SkirmishArmyProfileId.GroundManeuver,
            OperationMapId = source.OperationMapId, LayoutId = source.LayoutId,
            RoleOverlays = SkirmishRoleOverlayCatalog.CreateGroundManeuverSlice(),
            Structures = new[]
            {
                new SkirmishResolvedStructureEntry { FactionId = 1, StructureId = SkirmishStructureIds.Helipad },
                new SkirmishResolvedStructureEntry { FactionId = 2, StructureId = SkirmishStructureIds.Helipad }
            }
        };

        private sealed class Fixture : IDisposable
        {
            internal readonly World World = new("MissionInfrastructureControls");
            internal EntityManager Em => World.EntityManager;
            internal readonly Entity Session;
            internal readonly SkirmishProductionCatalog Catalog;
            private readonly Entity boundary;
            private readonly RuntimeBuildingCollection<RuntimeBuildingEntity> registry = new();
            private readonly List<GameObject> objects = new();
            private OperationMapRuntimeBootstrapSceneSystemHelper root;
            internal DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> Rows => Em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);

            internal Fixture(SkirmishResolvedSetup setup, bool expanded = true)
            {
                try
                {
                    boundary = Em.CreateEntity(); Em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
                    if (!expanded) return;
                    int scenarioIndex = SkirmishMissionDispatch.TryGetLegacyIndex(setup.CatalogId, out int selectedIndex)
                        ? selectedIndex : 8;
                    // Same original sources as MatchSceneView.ResolvedSkirmishBuildingPlacement:
                    // preset bootstrap owns GroundStaging; scene config owns authored unit registry.
                    var baseline = SkirmishPresetConfig.Load(scenarioIndex)?.buildingPlacement;
                    var scene = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                        "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
                    Need(baseline != null && scene != null, "native_preset_and_scene_bootstrap_inputs");
                    Catalog = SkirmishProductionCatalog.Create(setup, baseline, scene,
                        BuildingProductionUnitMetadataPrefabSystemHelper.TryGetMetadata);
                    Session = Em.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishMatchState));
                    var id = new FixedString64Bytes(Guid.NewGuid().ToString("N"));
                    Em.SetComponentData(Session, new SkirmishExpandedSessionComponent
                    {
                        SessionId = id, CatalogId = new(setup.CatalogId), DefinitionId = new(setup.DefinitionId),
                        ContentVersion = setup.ContentVersion, SetupHash = setup.SetupHash, Seed = setup.Seed,
                        SizeId = setup.SizeId, DifficultyId = setup.DifficultyId, ObjectiveKind = setup.ObjectiveKind,
                        Phase = SkirmishSessionPhase.Playing, InitializationComplete = 1, SpawnComplete = 1
                    });
                    Em.SetComponentData(Session, new SkirmishMatchState { SessionId = id, Seed = setup.Seed,
                        ScenarioIndex = scenarioIndex });
                    Em.AddComponentObject(Session, new SkirmishResolvedSetupRecord { Setup = setup });
                    Em.AddComponentObject(Session, new SkirmishProductionCatalogRecord { Catalog = Catalog });
                }
                catch
                {
                    // A constructor throw prevents using/Dispose at the callsite.
                    Catalog?.Dispose();
                    if (World.IsCreated) World.Dispose();
                    foreach (var value in objects) UnityEngine.Object.DestroyImmediate(value);
                    throw;
                }
            }

            internal bool Bind() => OperationMapHelipadReadModelUtility.TryBind(Em, Rows, in Grid, out _, out _);
            internal void GroundRows(int count)
            {
                for (int i = 0; i < count; i++) Rows.Add(new BuildingFactionProductionSpawnPointReadModel
                { BuildingId = new("building_barrack"), BuildingRuntimeId = 100 + i, FactionId = 1, SlotIndex = 0,
                    Cell = new int2(100 + i, 100), WorldPosition = new float3(100 + i, 0, 100) });
            }

            internal void PublishRoot()
            {
                var definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                    "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset");
                root = new OperationMapRuntimeBootstrapSceneSystemHelper(World);
                Need(root.TryPublish(definition, new FixedString64Bytes("scenario.skirmish.desert_base_standard"),
                    new FixedString64Bytes("skirmish.base_assault"), 1, OperationMapReadinessFlags.Metadata,
                    OperationMapReadinessFlags.Metadata, out _, out _), "actual55e_original_metadata_producer");
            }

            internal void AddCampaignRows()
            {
                Need(OperationMapMetadataUtility.TryResolveActiveMetadata(Em, out var metadata, out _, out _), "campaign_metadata_live");
                for (int i = 0; i < metadata.Value.Anchors.Length; i++)
                {
                    var anchor = metadata.Value.Anchors[i];
                    if (anchor.Kind != OperationMapAnchorKind.Helipad) continue;
                    Need(OperationMapMetadataUtility.TryResolveHelipadGeometry(in anchor, out float3 center, out _, out _), "native_anchor_geometry");
                    Rows.Add(new BuildingFactionProductionSpawnPointReadModel { BuildingId = new("building_helipad"),
                        BuildingRuntimeId = i + 1, FactionId = (byte)anchor.FactionId, SlotIndex = 0,
                        Cell = GridUtils.WorldToCell(in Grid, center), WorldPosition = center });
                }
            }

            internal RuntimeBuildingEntity AddRegisteredHelipad(byte faction, bool constructed)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Buildings/Building_Helipad.prefab");
                var definition = new BuildingDefinitionPrefabSystemHelper().CreateRuntimeBuildingDefinition(
                    prefab, "Helipad", "Controlled registration fixture", Vector2Int.one, 500, null);
                var instance = new GameObject("Owned registration control"); objects.Add(instance);
                instance.transform.position = new Vector3(100 + faction * 50, 0, 120);
                bool GetEm(out EntityManager value) { value = Em; return true; }
                Entity CreateCombat(int id, Vector2Int origin, BuildingDefinition d, byte initialOwner,
                    Quaternion rotation, RuntimeActorBirthContext birth)
                {
                    var entity = Em.CreateEntity(typeof(RuntimeBuildingCombatInfo), typeof(UnitHealth),
                        typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishStructureIdentityComponent));
                    Em.SetComponentData(entity, new RuntimeBuildingCombatInfo { RuntimeBuildingId = id,
                        OwnerFactionId = faction, OriginCell = new int2(origin.x, origin.y), FootprintCells = new int2(1, 1) });
                    Em.SetComponentData(entity, new UnitHealth { Current = 500, Max = 500 });
                    Em.SetComponentData(entity, new SkirmishAttemptOwnedComponent
                    { SessionId = Em.GetComponentData<SkirmishExpandedSessionComponent>(Session).SessionId,
                        IsStructure = 1, FactionId = faction, ReservationId = constructed ? 1001u : 0u });
                    Em.SetComponentData(entity, new SkirmishStructureIdentityComponent
                    { StructureId = new(SkirmishStructureIds.Helipad), Producer = SkirmishProducerKind.Helipad });
                    return entity;
                }
                var context = new BuildingRuntimeCreationCompositionSystemHelper.Context(registry, null, default, null,
                    () => false, GetEm, null, null, _ => false, null, null, CreateCombat, null, null, null, null, null);
                // Invoke the ORIGINAL normal registration body. Its unconditional
                // publishRuntimeLink adds the checkpoint record, not a test stub.
                var building = new BuildingRuntimeCreationCompositionSystemHelper().RegisterRuntimeBuilding(
                    context, definition, instance, new Vector2Int(100, 100), false);
                building.HasOwnerFaction = true; building.OwnerFactionId = faction;
                Need(Em.HasComponent<SkirmishNativeBuildingCheckpointRecord>(building.CombatEntity) &&
                    ReferenceEquals(Em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(building.CombatEntity).Building, building),
                    constructed ? "normal_registration_record_for_constructed_owner" : "normal_registration_record_for_starting_owner");
                Need(building.ProductionSpawnLocalPositions?.Length > 0, "actual_prefab_has_production_slot");
                Vector3 point = instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[0]);
                var world = new float3(point.x, point.y, point.z);
                Rows.Add(new BuildingFactionProductionSpawnPointReadModel { BuildingId = new("building_helipad"),
                    BuildingRuntimeId = building.Id, FactionId = faction, SlotIndex = 0,
                    Cell = GridUtils.WorldToCell(in Grid, world), WorldPosition = world });
                return building;
            }

            public void Dispose()
            {
                root?.Dispose();
                Catalog?.Dispose();
                World.Dispose();
                foreach (var value in objects) UnityEngine.Object.DestroyImmediate(value);
            }
        }

        private static void Need(bool value, string id)
        {
            if (!value) throw new InvalidOperationException("MissionInfrastructureControl failed: " + id);
            checks++;
        }
    }
}
