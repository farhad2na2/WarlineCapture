using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

// Read-only census of production inputs in the retained S073 source host.
// Entity strings below are source identities observed from that host; this
// adapter never creates entities, requests, plans, or inferred producer IDs.
internal static class SourceS073ProductionInputCensus
{
    const string OptIn = "WARLINE_S073_PRODUCTION_INPUT_CENSUS";

    static void Require(bool value, string message)
    {
        if (!value) throw new InvalidDataException(message);
    }

    static JObject VerifyPlanEntrySnapshotIsolation()
    {
        // This is an explicit control-only world, never part of the source census.
        // It exercises the shim accessor used by the compiled production owner
        // without scheduling or invoking that owner.
        var control = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 0;
        control.AllocateLogicalIdentity = () => new Entity { Index = ++next, Version = 1 };
        Entity populated = control.CreateEntity();
        var sourceEntries = control.AddBuffer<AIProductionPlanEntry>(populated);
        sourceEntries.Add(new AIProductionPlanEntry { UnitId = new FixedString64Bytes("source-unit") });
        Entity empty = control.CreateEntity();
        control.AddBuffer<AIProductionPlanEntry>(empty);

        var handle = control.GetBufferTypeHandle<AIProductionPlanEntry>(true);
        var chunk = new ArchetypeChunk { Manager = control, Entities = new[] { populated, empty }, Count = 2 };
        var accessor = chunk.GetBufferAccessor(ref handle);
        Require(accessor.Length == 2 && accessor[0].Length == 1 && accessor[0][0].UnitId.ToString() == "source-unit" &&
                accessor[1].Length == 0, "AI production plan entry snapshots lost values or entity alignment");
        var changedSnapshot = accessor[0][0];
        changedSnapshot.UnitId = new FixedString64Bytes("snapshot-only-change");
        accessor[0][0] = changedSnapshot;
        Require(control.GetBuffer<AIProductionPlanEntry>(populated)[0].UnitId.ToString() == "source-unit",
            "Writing a detached production-entry accessor snapshot changed its source buffer");
        return new JObject
        {
            ["scope"] = "IsolatedControlEntityManagerOnly",
            ["source_entity_manager_used"] = false,
            ["production_owner_invoked"] = false,
            ["populated_snapshot_value_verified"] = true,
            ["empty_buffer_alignment_verified"] = true,
            ["accessor_write_left_control_source_buffer_unchanged"] = true
        };
    }

    static string EntityId(Entity entity) => $"{entity.Index}:{entity.Version}";

    static JArray ConfiguredUnits(EntityManager source)
    {
        var rows = new JArray();
        using var query = source.CreateEntityQuery(typeof(BuildingConfiguredUnitReadModel));
        using var entities = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            var units = source.GetBuffer<BuildingConfiguredUnitReadModel>(entity);
            for (int i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                rows.Add(new JObject
                {
                    ["source_entity"] = EntityId(entity), ["buffer_index"] = i,
                    ["unit_id"] = unit.UnitId.ToString(), ["display_name"] = unit.DisplayName.ToString(),
                    ["materials_price"] = unit.Price, ["credits_cost"] = unit.CreditsCost,
                    ["can_request"] = unit.CanRequest, ["is_vehicle"] = unit.IsVehicle
                });
            }
        }
        return rows;
    }

    static JArray ProducerSlots(EntityManager source)
    {
        var rows = new JArray();
        using var query = source.CreateEntityQuery(typeof(BuildingProductionSlotReadModel));
        using var entities = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            var slots = source.GetBuffer<BuildingProductionSlotReadModel>(entity);
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                rows.Add(new JObject
                {
                    ["source_entity"] = EntityId(entity), ["buffer_index"] = i,
                    ["building_id"] = slot.BuildingId.ToString(), ["slot_index"] = slot.SlotIndex,
                    ["unit_source_key"] = slot.UnitSourceKey.ToString(), ["unit_id"] = slot.UnitId.ToString()
                });
            }
        }
        return rows;
    }

    static JObject ProducerSlotBufferState(EntityManager source)
    {
        using var query = source.CreateEntityQuery(typeof(BuildingProductionSlotReadModel));
        using var entities = query.ToEntityArray(Allocator.Temp);
        var buffers = new JArray();
        foreach (var entity in entities)
        {
            var slots = source.GetBuffer<BuildingProductionSlotReadModel>(entity);
            buffers.Add(new JObject
            {
                ["source_entity"] = EntityId(entity),
                ["buffer_present"] = true,
                ["buffer_length"] = slots.Length
            });
        }
        return new JObject
        {
            ["query_entity_count"] = entities.Length,
            ["any_buffer_present"] = entities.Length > 0,
            ["buffers"] = buffers,
            ["interpretation"] = entities.Length == 0
                ? "No source entity with the producer-slot read-model buffer; publisher may not have run or source boundary may not contain it."
                : "Buffer presence and length are observed separately; zero length means published/readable empty buffer, not inferred producer absence."
        };
    }

    internal static JArray SourceConfiguredDefinitionProductionSlots(System.Collections.Generic.IReadOnlyList<RuntimeDefinition> definitions)
    {
        if (definitions == null) throw new InvalidDataException("Pinned source configured building definitions required");
        var rows = new JArray();
        foreach (RuntimeDefinition definition in definitions)
        {
            if (definition == null || definition.Prefab == null)
                throw new InvalidDataException("Complete source configured building definition identity required");
            int count = definition.ProductionSlots != null && definition.ProductionSlots.Count > 0
                ? definition.ProductionSlots.Count
                : definition.QuaternarySpawnUnitPrefab != null ? 4
                : definition.TertiarySpawnUnitPrefab != null ? 3
                : definition.SecondarySpawnUnitPrefab != null ? 2
                : definition.SpawnUnitPrefab != null ? 1 : 0;
            var slots = new JArray();
            for (int index = 0; index < count; index++)
            {
                SourceUnitPrefab unit = definition.ProductionSlots != null && index < definition.ProductionSlots.Count
                    ? definition.ProductionSlots[index]?.SpawnUnitPrefab
                    : index switch
                    {
                        0 => definition.SpawnUnitPrefab,
                        1 => definition.SecondarySpawnUnitPrefab,
                        2 => definition.TertiarySpawnUnitPrefab,
                        3 => definition.QuaternarySpawnUnitPrefab,
                        _ => null
                    };
                if (unit == null)
                    throw new InvalidDataException("Source production slot has no configured unit prefab: " + definition.Prefab.name + "/" + index);
                int quantity = definition.ProductionSlots != null && index < definition.ProductionSlots.Count
                    ? Math.Max(1, definition.ProductionSlots[index]?.Quantity ?? 1)
                    : 1;
                string sourceKey = definition.ProductionSlots != null && index < definition.ProductionSlots.Count
                    ? definition.ProductionSlots[index]?.SpawnUnitSourceKey.ToString()
                    : null;
                slots.Add(new JObject
                {
                    ["slot_index"] = index,
                    ["unit_prefab_key"] = unit.name,
                    ["unit_source_key"] = string.IsNullOrEmpty(sourceKey) ? unit.name : sourceKey,
                    ["quantity"] = quantity
                });
            }
            rows.Add(new JObject
            {
                ["building_prefab_key"] = definition.Prefab.name,
                ["production_slot_count"] = count,
                ["production_slots"] = slots
            });
        }
        return rows;
    }

    static JArray ProductionRequests(EntityManager source)
    {
        var rows = new JArray();
        using var query = source.CreateEntityQuery(typeof(BuildingFactionUnitProductionRequest));
        using var entities = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            var requests = source.GetBuffer<BuildingFactionUnitProductionRequest>(entity);
            for (int i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                rows.Add(new JObject
                {
                    ["source_entity"] = EntityId(entity), ["buffer_index"] = i,
                    ["request_id"] = request.RequestId,
                    ["request_owner"] = request.RequestOwner == Entity.Null ? JValue.CreateNull() : new JValue(EntityId(request.RequestOwner)),
                    ["request_attempt"] = request.RequestAttempt.ToString(), ["faction_id"] = request.FactionId,
                    ["unit_id"] = request.UnitId.ToString(), ["status"] = request.Status,
                    ["result_code"] = request.ResultCode, ["producer_display_name"] = request.ProducerDisplayName.ToString(),
                    ["unit_display_name"] = request.UnitDisplayName.ToString(), ["cost"] = request.Cost,
                    ["materials_cost"] = request.MaterialsCost, ["resources_reserved"] = request.ResourcesReserved,
                    ["queue_count"] = request.QueueCount, ["produced_count"] = request.ProducedCount
                });
            }
        }
        return rows;
    }

    static JArray Plans(EntityManager source)
    {
        var rows = new JArray();
        using var query = source.CreateEntityQuery(typeof(AIProductionPlan));
        using var entities = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            var plan = source.GetComponentData<AIProductionPlan>(entity);
            var entries = new JArray();
            if (source.HasBuffer<AIProductionPlanEntry>(entity))
            {
                var buffer = source.GetBuffer<AIProductionPlanEntry>(entity);
                for (int i = 0; i < buffer.Length; i++)
                    entries.Add(new JObject { ["index"] = i, ["unit_id"] = buffer[i].UnitId.ToString() });
            }
            rows.Add(new JObject
            {
                ["source_entity"] = EntityId(entity), ["faction_id"] = plan.FactionId,
                ["enabled"] = plan.Enabled, ["next_unit_index"] = plan.NextUnitIndex,
                ["target_produced_units"] = plan.TargetProducedUnits, ["max_queued_units"] = plan.MaxQueuedUnits,
                ["unit_production_interval_seconds"] = plan.UnitProductionIntervalSeconds,
                ["last_production_time"] = plan.LastProductionTime, ["last_log_time"] = plan.LastLogTime,
                ["has_entries_buffer"] = source.HasBuffer<AIProductionPlanEntry>(entity), ["entries"] = entries
            });
        }
        return rows;
    }

    static JArray EconomyAndFuel(EntityManager source)
    {
        var rows = new JArray();
        using (var query = source.CreateEntityQuery(typeof(FactionEconomy)))
        using (var entities = query.ToEntityArray(Allocator.Temp))
        foreach (var entity in entities)
        {
            var economy = source.GetComponentData<FactionEconomy>(entity);
            var row = new JObject
            {
                ["source_entity"] = EntityId(entity), ["kind"] = "FactionEconomy",
                ["faction_id"] = economy.FactionId, ["money"] = economy.Money,
                ["materials_only_construction"] = economy.MaterialsOnlyConstruction,
                ["oil"] = economy.Oil, ["fuel"] = economy.Fuel,
                ["oil_income_rate"] = economy.OilIncomeRate, ["fuel_income_rate"] = economy.FuelIncomeRate,
                ["has_tactical_materials"] = source.HasComponent<FactionTacticalMaterialsComponent>(entity)
            };
            if (source.HasComponent<FactionTacticalMaterialsComponent>(entity))
            {
                var materials = source.GetComponentData<FactionTacticalMaterialsComponent>(entity);
                row["tactical_materials"] = new JObject
                {
                    ["faction_id"] = materials.FactionId, ["current"] = materials.Current,
                    ["capacity"] = materials.Capacity, ["version"] = materials.Version
                };
            }
            rows.Add(row);
        }
        using (var query = source.CreateEntityQuery(typeof(BuildingResourceStorageComponent)))
        using (var entities = query.ToEntityArray(Allocator.Temp))
        foreach (var entity in entities)
        {
            var storage = source.GetComponentData<BuildingResourceStorageComponent>(entity);
            rows.Add(new JObject
            {
                ["source_entity"] = EntityId(entity), ["kind"] = "BuildingResourceStorage",
                ["runtime_building_id"] = storage.RuntimeBuildingId, ["owner_faction_id"] = storage.OwnerFactionId,
                ["oil_capacity"] = storage.OilStorageCapacity, ["fuel_capacity"] = storage.FuelStorageCapacity,
                ["oil_barrels_per_day"] = storage.OilBarrelsPerDay, ["fuel_barrels_per_day"] = storage.FuelBarrelsPerDay,
                ["stored_oil_barrels"] = storage.StoredOilBarrels, ["stored_fuel_barrels"] = storage.StoredFuelBarrels,
                ["reserved_oil_inbound"] = storage.ReservedOilInboundBarrels,
                ["reserved_oil_outbound"] = storage.ReservedOilOutboundBarrels,
                ["reserved_fuel_inbound"] = storage.ReservedFuelInboundBarrels,
                ["reserved_fuel_outbound"] = storage.ReservedFuelOutboundBarrels,
                ["civilian_fuel_reserve"] = storage.CivilianFuelReserveBarrels,
                ["received_hauler_fuel"] = storage.ReceivedHaulerFuelBarrels, ["version"] = storage.Version
            });
        }
        return rows;
    }

    internal static JObject Run(EntityManager source, JArray sourceConfiguredDefinitionProductionSlots)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptIn), "1", StringComparison.Ordinal)) return null;
        using var matches = source.CreateEntityQuery(typeof(SkirmishMatchState));
        Require(matches.CalculateEntityCount() == 1, "S073 production census requires one retained source match.");
        var match = matches.GetSingleton<SkirmishMatchState>();
        Require(match.ScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex && match.Phase == SkirmishPhase.Preparing,
            "S073 production census requires the retained Industrial Basin Preparing source boundary.");
        return new JObject
        {
            ["kind"] = "S073ProductionInputCensus", ["schema_version"] = 1,
            ["scope"] = "ReadOnlySourceEntityManagerCensusAtRetainedPreparingBoundary",
            ["source_host_mutated"] = false, ["session_id"] = match.SessionId.ToString(),
            ["scenario_index"] = match.ScenarioIndex, ["phase"] = match.Phase.ToString(),
            ["plan_entry_buffer_snapshot_control"] = VerifyPlanEntrySnapshotIsolation(),
            ["ai_production_plans"] = Plans(source), ["configured_units"] = ConfiguredUnits(source),
            ["configured_producer_slots"] = ProducerSlots(source),
            ["producer_slot_buffer_state"] = ProducerSlotBufferState(source),
            ["source_configured_definition_production_slots"] = sourceConfiguredDefinitionProductionSlots == null
                ? throw new InvalidDataException("Pinned source configured building definitions required")
                : (JArray)sourceConfiguredDefinitionProductionSlots.DeepClone(),
            ["configured_unit_requests"] = ProductionRequests(source),
            ["economy_and_fuel_facts"] = EconomyAndFuel(source),
            ["identity_rule"] = "Entity identity fields are copied only from the source EntityManager. No entity identity or producer association is synthesized."
        };
    }
}
