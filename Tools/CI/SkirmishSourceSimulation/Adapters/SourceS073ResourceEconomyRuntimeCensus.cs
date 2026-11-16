using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

// Opt-in, read-only snapshots of the original S073 source host. This records
// observed values only; it does not create balances, profiles, or ownership.
internal static class SourceS073ResourceEconomyRuntimeCensus
{
    const string OptIn = "WARLINE_S073_RESOURCE_ECONOMY_RUNTIME_CENSUS";

    internal static bool Enabled => string.Equals(
        Environment.GetEnvironmentVariable(OptIn), "1", StringComparison.Ordinal);

    internal static JObject Capture(EntityManager source, string stage, int ticksExecuted, JObject run)
    {
        if (!Enabled) return null;
        if (source == null || source.World == null || !source.World.IsCreated)
            throw new InvalidDataException("S073 resource census requires the retained source EntityManager.");

        using var matches = source.CreateEntityQuery(typeof(SkirmishMatchState));
        if (matches.CalculateEntityCount() != 1)
            throw new InvalidDataException("S073 resource census requires one source match entity.");
        var matchEntity = matches.GetSingletonEntity();
        var match = source.GetComponentData<SkirmishMatchState>(matchEntity);
        if (match.ScenarioIndex != SkirmishPresetConfig.IndustrialBasinScenarioIndex || match.Seed != 104729)
            throw new InvalidDataException("S073 resource census requires the admitted Industrial Basin source scenario and seed.");
        var buildReceipt = run?["source_build_receipt"] as JObject;
        var executable = run?["source_executable"] as JObject;
        if (buildReceipt == null || executable == null ||
            string.IsNullOrWhiteSpace((string)buildReceipt["path"]) ||
            string.IsNullOrWhiteSpace((string)buildReceipt["sha256"]) ||
            string.IsNullOrWhiteSpace((string)executable["path"]) ||
            string.IsNullOrWhiteSpace((string)executable["sha256"]))
            throw new InvalidDataException("S073 resource census requires the admitted source build and executable provenance descriptors.");

        var economies = new JArray();
        using (var query = source.CreateEntityQuery(typeof(FactionEconomy)))
        using (var entities = query.ToEntityArray(Allocator.Temp))
        {
            foreach (var entity in entities)
            {
                var value = source.GetComponentData<FactionEconomy>(entity);
                bool hasMaterials = source.HasComponent<FactionTacticalMaterialsComponent>(entity);
                bool hasPolicy = source.HasComponent<FactionEconomyPolicy>(entity);
                bool hasOwner = source.HasComponent<SkirmishAttemptOwnedComponent>(entity);
                var row = new JObject
                {
                    ["entity"] = Id(entity),
                    ["owner_component"] = "FactionEconomy",
                    ["faction_id"] = value.FactionId,
                    ["money"] = value.Money,
                    ["materials_only_construction"] = value.MaterialsOnlyConstruction,
                    ["oil"] = value.Oil,
                    ["fuel"] = value.Fuel,
                    ["oil_income_rate"] = value.OilIncomeRate,
                    ["fuel_income_rate"] = value.FuelIncomeRate,
                    ["last_sell_time"] = value.LastSellTime,
                    ["last_log_time"] = value.LastLogTime,
                    ["has_tactical_materials"] = hasMaterials,
                    ["has_economy_policy"] = hasPolicy,
                    ["has_attempt_owner"] = hasOwner
                };
                if (hasMaterials)
                {
                    var bank = source.GetComponentData<FactionTacticalMaterialsComponent>(entity);
                    row["tactical_materials"] = new JObject
                    {
                        ["component_faction_id"] = bank.FactionId,
                        ["current"] = bank.Current,
                        ["capacity"] = bank.Capacity,
                        ["lifetime_fabricated"] = bank.LifetimeFabricated,
                        ["lifetime_imported"] = bank.LifetimeImported,
                        ["lifetime_rewarded"] = bank.LifetimeRewarded,
                        ["lifetime_exported"] = bank.LifetimeExported,
                        ["lifetime_spent"] = bank.LifetimeSpent,
                        ["lifetime_production_spent"] = bank.LifetimeProductionSpent,
                        ["version"] = bank.Version
                    };
                }
                if (hasPolicy)
                {
                    var policy = source.GetComponentData<FactionEconomyPolicy>(entity);
                    row["economy_policy"] = new JObject
                    {
                        ["enabled"] = policy.Enabled,
                        ["income_multiplier"] = policy.IncomeMultiplier,
                        ["oil_sell_price"] = policy.OilSellPrice,
                        ["fuel_sell_price"] = policy.FuelSellPrice,
                        ["sell_interval_seconds"] = policy.SellIntervalSeconds
                    };
                }
                if (hasOwner)
                {
                    var owner = source.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                    row["attempt_owner"] = new JObject
                    {
                        ["session_id"] = owner.SessionId.ToString(),
                        ["stable_object_id"] = owner.StableObjectId.ToString(),
                        ["matches_source_session"] = owner.SessionId.Equals(match.SessionId)
                    };
                }
                economies.Add(row);
            }
        }

        var buildings = new JArray();
        using (var query = source.CreateEntityQuery(typeof(BuildingResourceStorageComponent)))
        using (var entities = query.ToEntityArray(Allocator.Temp))
        {
            foreach (var entity in entities)
            {
                var storage = source.GetComponentData<BuildingResourceStorageComponent>(entity);
                bool hasProfile = source.HasComponent<BuildingResourceProductionProfileComponent>(entity);
                bool hasFaction = source.HasComponent<Faction>(entity);
                bool hasCombatOwner = source.HasComponent<RuntimeBuildingCombatInfo>(entity);
                bool hasPrefabKey = source.HasComponent<UnitSourcePrefabKey>(entity);
                bool hasAttemptOwner = source.HasComponent<SkirmishAttemptOwnedComponent>(entity);
                var row = new JObject
                {
                    ["entity"] = Id(entity),
                    ["owner_component"] = "BuildingResourceStorageComponent",
                    ["storage_runtime_building_id"] = storage.RuntimeBuildingId,
                    ["storage_owner_faction_id"] = storage.OwnerFactionId,
                    ["oil_storage_capacity"] = storage.OilStorageCapacity,
                    ["fuel_storage_capacity"] = storage.FuelStorageCapacity,
                    ["oil_barrels_per_day"] = storage.OilBarrelsPerDay,
                    ["fuel_barrels_per_day"] = storage.FuelBarrelsPerDay,
                    ["stored_oil_barrels"] = storage.StoredOilBarrels,
                    ["stored_fuel_barrels"] = storage.StoredFuelBarrels,
                    ["reserved_oil_inbound_barrels"] = storage.ReservedOilInboundBarrels,
                    ["reserved_oil_outbound_barrels"] = storage.ReservedOilOutboundBarrels,
                    ["reserved_fuel_inbound_barrels"] = storage.ReservedFuelInboundBarrels,
                    ["reserved_fuel_outbound_barrels"] = storage.ReservedFuelOutboundBarrels,
                    ["civilian_fuel_reserve_barrels"] = storage.CivilianFuelReserveBarrels,
                    ["received_hauler_fuel_barrels"] = storage.ReceivedHaulerFuelBarrels,
                    ["storage_version"] = storage.Version,
                    ["has_production_profile"] = hasProfile,
                    ["has_faction_component"] = hasFaction,
                    ["has_runtime_building_combat_owner"] = hasCombatOwner,
                    ["has_source_prefab_key"] = hasPrefabKey,
                    ["has_attempt_owner"] = hasAttemptOwner
                };
                if (hasProfile)
                {
                    var profile = source.GetComponentData<BuildingResourceProductionProfileComponent>(entity);
                    row["production_profile"] = new JObject
                    {
                        ["period_seconds"] = profile.PeriodSeconds,
                        ["oil_per_period"] = profile.OilPerPeriod,
                        ["fuel_per_period"] = profile.FuelPerPeriod,
                        ["oil_per_fuel_batch"] = profile.OilPerFuelBatch,
                        ["fuel_per_batch"] = profile.FuelPerBatch,
                        ["fuel_batch_seconds"] = profile.FuelBatchSeconds,
                        ["fuel_batch_progress_seconds"] = profile.FuelBatchProgressSeconds
                    };
                }
                if (hasFaction) row["faction_component_id"] = source.GetComponentData<Faction>(entity).Id;
                if (hasCombatOwner)
                {
                    var owner = source.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                    row["runtime_building_owner"] = new JObject
                    {
                        ["owner_faction_id"] = owner.OwnerFactionId,
                        ["runtime_building_id"] = owner.RuntimeBuildingId
                    };
                }
                if (hasPrefabKey) row["source_prefab_key"] = source.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                if (hasAttemptOwner)
                {
                    var owner = source.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                    row["attempt_owner"] = new JObject
                    {
                        ["session_id"] = owner.SessionId.ToString(),
                        ["stable_object_id"] = owner.StableObjectId.ToString(),
                        ["matches_source_session"] = owner.SessionId.Equals(match.SessionId)
                    };
                }
                buildings.Add(row);
            }
        }

        var aiPlans = new JArray();
        using (var query = source.CreateEntityQuery(typeof(AIProductionPlan), typeof(AIProductionPlanEntry)))
        using (var entities = query.ToEntityArray(Allocator.Temp))
        {
            foreach (var entity in entities)
            {
                var plan = source.GetComponentData<AIProductionPlan>(entity);
                var entries = new JArray();
                var buffer = source.GetBuffer<AIProductionPlanEntry>(entity, true);
                for (int i = 0; i < buffer.Length; i++) entries.Add(buffer[i].UnitId.ToString());
                aiPlans.Add(new JObject
                {
                    ["entity"] = Id(entity),
                    ["owner_component"] = "AIProductionPlan",
                    ["faction_id"] = plan.FactionId,
                    ["enabled"] = plan.Enabled != 0,
                    ["next_unit_index"] = plan.NextUnitIndex,
                    ["target_produced_units"] = plan.TargetProducedUnits,
                    ["max_queued_units"] = plan.MaxQueuedUnits,
                    ["unit_production_interval_seconds"] = plan.UnitProductionIntervalSeconds,
                    ["last_production_time"] = plan.LastProductionTime,
                    ["entries"] = entries
                });
            }
        }

        var configuredUnits = new JArray();
        using (var boundaryQuery = source.CreateEntityQuery(typeof(BuildingRuntimeStateTag), typeof(BuildingConfiguredUnitReadModel)))
        {
            int count = boundaryQuery.CalculateEntityCount();
            if (count == 1)
            {
                var boundary = boundaryQuery.GetSingletonEntity();
                var buffer = source.GetBuffer<BuildingConfiguredUnitReadModel>(boundary, true);
                for (int i = 0; i < buffer.Length; i++)
                {
                    var unit = buffer[i];
                    configuredUnits.Add(new JObject
                    {
                        ["registry_index"] = i,
                        ["unit_id"] = unit.UnitId.ToString(),
                        ["display_name"] = unit.DisplayName.ToString(),
                        ["can_request"] = unit.CanRequest != 0,
                        ["credits_cost"] = unit.CreditsCost,
                        ["materials_cost"] = unit.Price,
                        ["is_vehicle"] = unit.IsVehicle != 0
                    });
                }
            }
        }

        var productionRequests = new JArray();
        JObject productionRequestCounts = null;
        using (var boundaryQuery = source.CreateEntityQuery(typeof(BuildingRuntimeStateTag), typeof(BuildingFactionUnitProductionRequest)))
        {
            if (boundaryQuery.CalculateEntityCount() == 1)
            {
                int pending = 0, succeeded = 0, failed = 0;
                var buffer = source.GetBuffer<BuildingFactionUnitProductionRequest>(boundaryQuery.GetSingletonEntity(), true);
                for (int i = 0; i < buffer.Length; i++)
                {
                    var request = buffer[i];
                    if (request.Status == BuildingFactionUnitProductionRequest.Pending) pending++;
                    else if (request.Status == BuildingFactionUnitProductionRequest.Succeeded) succeeded++;
                    else if (request.Status == BuildingFactionUnitProductionRequest.Failed) failed++;
                    productionRequests.Add(new JObject
                    {
                        ["request_id"] = request.RequestId,
                        ["request_owner_entity"] = Id(request.RequestOwner),
                        ["request_attempt"] = request.RequestAttempt.ToString(),
                        ["faction_id"] = request.FactionId,
                        ["unit_id"] = request.UnitId.ToString(),
                        ["status"] = request.Status,
                        ["result_code"] = request.ResultCode,
                        ["producer_display_name"] = request.ProducerDisplayName.ToString(),
                        ["unit_display_name"] = request.UnitDisplayName.ToString(),
                        ["credits_cost"] = request.Cost,
                        ["materials_cost"] = request.MaterialsCost,
                        ["resources_reserved"] = request.ResourcesReserved != 0,
                        ["queue_count"] = request.QueueCount,
                        ["produced_count"] = request.ProducedCount
                    });
                }
                productionRequestCounts = new JObject { ["pending"] = pending, ["succeeded"] = succeeded, ["failed"] = failed };
            }
        }

        return new JObject
        {
            ["kind"] = "S073ResourceEconomyRuntimeCensus",
            ["schema_version"] = 1,
            ["stage"] = stage,
            ["ticks_executed"] = ticksExecuted,
            ["source_world_available"] = source.World != null && source.World.IsCreated,
            ["match_entity"] = Id(matchEntity),
            ["session_id"] = match.SessionId.ToString(),
            ["scenario_index"] = match.ScenarioIndex,
            ["phase"] = match.Phase.ToString(),
            ["outcome"] = match.Outcome.ToString(),
            ["source_mutated_by_census"] = false,
            ["provenance"] = new JObject
            {
                ["world_owner"] = "SourceS073LegacyNormalRun retained EntityManager",
                ["economy_owner_component"] = typeof(FactionEconomy).FullName,
                ["storage_owner_component"] = typeof(BuildingResourceStorageComponent).FullName,
                ["profile_owner_component"] = typeof(BuildingResourceProductionProfileComponent).FullName,
                ["faction_materials_owner_component"] = typeof(FactionTacticalMaterialsComponent).FullName,
                ["snapshot_method"] = "Direct component readback from original source-world entity queries",
                ["fabricated_or_defaulted_values"] = false,
                ["source_build_receipt"] = buildReceipt.DeepClone(),
                ["source_executable"] = executable.DeepClone()
            },
            ["faction_economies"] = economies,
            ["building_resource_storage_and_profiles"] = buildings,
            ["ai_production_plans"] = aiPlans,
            ["configured_units"] = configuredUnits,
            ["production_request_counts"] = productionRequestCounts,
            ["production_requests"] = productionRequests
        };
    }

    static string Id(Entity entity) => $"{entity.Index}:{entity.Version}";
}
