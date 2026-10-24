#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    // Retains the existing live composition owner solely for initial read-only export.
    // It never initializes a substitute registry or calls mutating selection getters.
    public static class S002InitialWorldManagedAuthority
    {
        sealed class Owner
        {
            internal BuildingGameplaySourceCompositionSystemHelper Source;
        }
        static readonly List<Owner> Owners = new List<Owner>();

        internal static void Register(BuildingGameplaySourceCompositionSystemHelper source)
        {
            if (Environment.GetEnvironmentVariable("WARLINE_S002_INITIAL_WORLD_BUNDLE") != "1") return;
            if (source == null)
                throw new InvalidOperationException("Initial-world building source must bind an actual World.");
            for (int i = 0; i < Owners.Count; i++)
                if (ReferenceEquals(Owners[i].Source, source))
                {
                    return;
                }
            Owners.Add(new Owner { Source = source });
        }

        public static Dictionary<string, object> Capture(World world)
        {
            using var budget = S002InitialWorldBoundaryWitness.BeginPrivateCaptureBudget();
            Owner owner = null;
            foreach (var candidate in Owners)
            {
                // Read the World already bound by the original EnsureEntityQueries;
                // never resolve DefaultWorld or call a getter that initializes queries.
                var boundWorld = Field(candidate.Source.BuildingGameplayEcsQueryCompositionSystemHelper, "_queryWorld") as World;
                if (!ReferenceEquals(boundWorld, world)) continue;
                if (owner != null) throw new InvalidOperationException("Multiple managed building composition owners in initial World.");
                owner = candidate;
            }
            if (owner == null) throw new InvalidOperationException("Actual global managed building registry owner was not observed.");
            var source = owner.Source;
            var registry = source.RuntimeBuildingSystem;
            if (registry.Count < 0 || registry.Count > 8192)
                throw new InvalidOperationException("Managed building registry exceeds the bounded export profile before row allocation.");
            var buildings = new List<object>();
            foreach (var pair in registry.Buildings)
            {
                var b = pair.Value;
                if (b == null || b.Id != pair.Key) throw new InvalidOperationException("Managed building identity mismatch.");
                buildings.Add(new Dictionary<string, object>
                {
                    ["id"] = b.Id, ["definition"] = b.Definition,
                    ["origin_cell"] = b.OriginCell, ["combat_entity"] = b.CombatEntity,
                    ["blocker_entity"] = b.BlockerEntity,
                    ["instance_present"] = b.Instance != null,
                    ["instance_transform"] = b.Instance != null ? b.Instance.transform : null,
                    ["instance_position"] = b.Instance != null ? (object)b.Instance.transform.position : null,
                    ["instance_rotation"] = b.Instance != null ? (object)b.Instance.transform.rotation : null,
                    ["instance_scale"] = b.Instance != null ? (object)b.Instance.transform.localScale : null,
                    ["door_transform"] = b.DoorZ,
                    ["door_closed_local_euler_z"] = b.DoorClosedLocalEulerZ,
                    ["door_open_local_euler_z"] = b.DoorOpenLocalEulerZ,
                    ["door_open_01"] = b.DoorOpen01,
                    ["production_spawn_local_positions"] = b.ProductionSpawnLocalPositions,
                    ["produced_unit_slots"] = b.ProducedUnitSlots,
                    ["produced_units"] = b.ProducedUnits,
                    ["produced_unit_prefabs"] = b.ProducedUnitPrefabs,
                    ["produced_unit_source_keys"] = b.ProducedUnitSourceKeys,
                    ["pending_productions"] = b.PendingProductions,
                    ["active_transport"] = b.ActiveTransport,
                    ["is_destroyed"] = b.IsDestroyed, ["is_city_generated"] = b.IsCityGenerated,
                    ["has_owner_faction"] = b.HasOwnerFaction, ["owner_faction_id"] = b.OwnerFactionId,
                    ["destroyed_cleanup_at"] = b.DestroyedCleanupAt,
                    ["stored_oil_barrels"] = b.StoredOilBarrels, ["stored_fuel_barrels"] = b.StoredFuelBarrels
                });
            }
            return new Dictionary<string, object>
            {
                ["source_type"] = source.GetType().FullName,
                ["world_sequence"] = world.SequenceNumber,
                ["registry_next_building_id"] = Field(registry, "_nextBuildingId"),
                ["building_spawn_random_state"] = source.BuildingSpawnRandomState,
                ["selected_building_id"] = registry.SelectedBuildingId,
                ["active_building_id"] = registry.ActiveBuildingId,
                ["registry_enumeration_order"] = buildings,
                ["production_queue_owner"] = Fields(source.BuildingProductionQueueCompositionSystemHelper, world),
                ["production_update_owner"] = Fields(source.BuildingProductionUpdateCompositionSystemHelper, world),
                ["production_transport_owner"] = Fields(source.BuildingProductionTransportPresentationSystemHelper, world),
                ["definition_prefab_owner"] = Fields(source.BuildingDefinitionPrefabSystemHelper, world),
                ["placement_startup_owner"] = Fields(source.BuildingPlacementStartupSystemHelper, world),
                ["runtime_processing_owner"] = Fields(source.BuildingRuntimeProcessingCompositionSystemHelper, world),
                ["faction_resource_owner"] = Fields(source.FactionResourceCompositionSystemHelper, world),
                ["resource_hauler_bridge_owner"] = Fields(source.BuildingResourceHaulerBridgeCompositionSystemHelper, world),
                ["resource_hauler_utility_owner"] = Fields(source.ResourceHaulerUtilitySystemHelper, world)
            };
        }
        static object Field(object owner, string name)
        {
            var field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null) throw new InvalidOperationException("Source-owned field missing: " + name);
            return field.GetValue(owner);
        }
        static Dictionary<string, object> Fields(object owner, World captureWorld)
        {
            if (owner == null) throw new InvalidOperationException("Managed consumer owner absent.");
            var result = new Dictionary<string, object> { ["source_type"] = owner.GetType().FullName };
            for (var type = owner.GetType(); type != null && type != typeof(object); type = type.BaseType)
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!field.IsStatic) result.Add(type.FullName + "." + field.Name, BindingValue(field.GetValue(owner), captureWorld));
            return result;
        }
        static object BindingValue(object value, World captureWorld)
        {
            if (value is Camera camera)
                return new Dictionary<string, object>
                {
                    ["kind"] = "source-camera-initial-frame-v1", ["source_type"] = camera.GetType().FullName,
                    ["transform"] = camera.transform, ["enabled"] = camera.enabled,
                    ["orthographic"] = camera.orthographic, ["orthographic_size"] = camera.orthographicSize,
                    ["field_of_view"] = camera.fieldOfView, ["aspect"] = camera.aspect,
                    ["near_clip_plane"] = camera.nearClipPlane, ["far_clip_plane"] = camera.farClipPlane,
                    ["projection_matrix"] = camera.projectionMatrix, ["world_to_camera_matrix"] = camera.worldToCameraMatrix,
                    ["rect"] = camera.rect, ["pixel_rect"] = camera.pixelRect,
                    ["pixel_width"] = camera.pixelWidth, ["pixel_height"] = camera.pixelHeight,
                    ["culling_mask"] = camera.cullingMask, ["target_display"] = camera.targetDisplay,
                    ["future_camera_input_required"] = true
                };
            if (value is World world)
            {
                if (!world.IsCreated || !ReferenceEquals(world, captureWorld))
                    throw new InvalidOperationException("Foreign or disposed managed query-cache World cannot rebind to this initial World.");
                return new Dictionary<string, object>
                {
                    ["kind"] = "retained-world-binding-v1", ["world_sequence"] = world.IsCreated ? (object)world.SequenceNumber : null,
                    ["is_created"] = world.IsCreated, ["name"] = world.Name,
                    ["rebind_to_retained_world"] = true
                };
            }
            if (value is EntityQuery query)
            {
                if (!query.Equals(default(EntityQuery)) && !S002InitialWorldQueryFilterAuthority.BelongsToWorld(query, captureWorld))
                    throw new InvalidOperationException("Managed cached query belongs to another World; no query copy or rebind permitted.");
                return new Dictionary<string, object>
                {
                    ["kind"] = "source-query-binding-v1", ["actual"] = S002InitialWorldBoundaryWitness.Query(query, captureWorld.EntityManager.GetEntityTypeHandle()),
                    ["rebind_from_captured_source_description"] = true
                };
            }
            if (value is ComponentType component)
                return new Dictionary<string, object>
                {
                    ["kind"] = "source-component-type-binding-v1",
                    ["source_type"] = TypeManager.GetType(component.TypeIndex)?.FullName,
                    ["captured_type_index"] = component.TypeIndex.Value,
                    ["access_mode"] = component.AccessModeType,
                    ["chunk_component"] = component.IsChunkComponent
                };
            if (value != null && value.GetType().IsGenericType &&
                value.GetType().GetGenericTypeDefinition() == typeof(WorldScopedComponentQueryCache<>))
                return new Dictionary<string, object>
                {
                    ["kind"] = "world-scoped-component-query-cache-v1", ["source_type"] = value.GetType().FullName,
                    ["component_source_type"] = value.GetType().GetGenericArguments()[0].FullName,
                    ["source_creation_recipe"] = "WorldScopedComponentQueryCache<T>.Get(EntityManager)",
                    ["source_path"] = "Assets/Game/Scripts/Systems/WorldScopedComponentQueryCache.cs",
                    ["actual_fields"] = Fields(value, captureWorld), ["rebind_required"] = true
                };
            return value;
        }
    }
}
#endif
