using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

// Read-only, opt-in census for the retained S073 legacy source host. This is
// intentionally separate from the disposable AI copier and does not mutate ECS.
internal static class SourceS073LegacyEconomyStorageCensus
{
    const string OptIn = "WARLINE_S073_LEGACY_ECONOMY_STORAGE_CENSUS";

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    static JObject EconomyRow(EntityManager em, Entity entity, FixedString64Bytes sessionId)
    {
        var economy = em.GetComponentData<FactionEconomy>(entity);
        bool attemptOwned = em.HasComponent<SkirmishAttemptOwnedComponent>(entity);
        string ownerSession = null;
        string ownerObjectId = null;
        bool ownerMatches = false;
        if (attemptOwned)
        {
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
            ownerSession = owner.SessionId.ToString();
            ownerObjectId = owner.StableObjectId.ToString();
            ownerMatches = owner.SessionId.Equals(sessionId);
        }
        var row = new JObject
        {
            ["entity"] = $"{entity.Index}:{entity.Version}",
            ["faction_id"] = economy.FactionId,
            ["money"] = economy.Money,
            ["materials_only_construction"] = economy.MaterialsOnlyConstruction,
            ["oil"] = economy.Oil,
            ["fuel"] = economy.Fuel,
            ["oil_income_rate"] = economy.OilIncomeRate,
            ["fuel_income_rate"] = economy.FuelIncomeRate,
            ["attempt_owned"] = attemptOwned,
            ["attempt_session_id"] = ownerSession,
            ["attempt_stable_object_id"] = ownerObjectId,
            ["attempt_session_matches"] = ownerMatches,
            ["has_policy"] = em.HasComponent<FactionEconomyPolicy>(entity)
        };
        if (em.HasComponent<FactionEconomyPolicy>(entity))
        {
            var policy = em.GetComponentData<FactionEconomyPolicy>(entity);
            row["policy"] = new JObject
            {
                ["enabled"] = policy.Enabled,
                ["income_multiplier"] = policy.IncomeMultiplier,
                ["oil_sell_price"] = policy.OilSellPrice,
                ["fuel_sell_price"] = policy.FuelSellPrice,
                ["sell_interval_seconds"] = policy.SellIntervalSeconds
            };
        }
        row["has_tactical_materials"] = em.HasComponent<FactionTacticalMaterialsComponent>(entity);
        if (em.HasComponent<FactionTacticalMaterialsComponent>(entity))
        {
            var materials = em.GetComponentData<FactionTacticalMaterialsComponent>(entity);
            row["tactical_materials"] = new JObject
            {
                ["faction_id"] = materials.FactionId,
                ["current"] = materials.Current,
                ["capacity"] = materials.Capacity,
                ["lifetime_fabricated"] = materials.LifetimeFabricated,
                ["lifetime_imported"] = materials.LifetimeImported,
                ["lifetime_rewarded"] = materials.LifetimeRewarded,
                ["lifetime_spent"] = materials.LifetimeSpent,
                ["version"] = materials.Version
            };
        }
        return row;
    }

    static JObject StorageRow(EntityManager em, Entity entity, FixedString64Bytes sessionId)
    {
        var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
        var key = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
        var faction = em.GetComponentData<Faction>(entity).Id;
        var combat = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
        bool attemptOwned = em.HasComponent<SkirmishAttemptOwnedComponent>(entity);
        bool attemptMatches = false;
        string ownerSession = null;
        string stableObjectId = null;
        if (attemptOwned)
        {
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
            ownerSession = owner.SessionId.ToString();
            stableObjectId = owner.StableObjectId.ToString();
            attemptMatches = owner.SessionId.Equals(sessionId);
        }
        bool operationMapScenery = em.HasComponent<OperationMapBuildingComponent>(entity);
        bool campaignRole = em.HasComponent<CampaignMissionUnitRoleComponent>(entity);
        return new JObject
        {
            ["entity"] = $"{entity.Index}:{entity.Version}",
            ["source_prefab_key"] = key,
            ["faction_component_id"] = faction,
            ["runtime_owner_faction_id"] = combat.OwnerFactionId,
            ["runtime_building_id"] = combat.RuntimeBuildingId,
            ["origin_cell"] = new JArray(combat.OriginCell.x, combat.OriginCell.y),
            ["footprint_cells"] = new JArray(combat.FootprintCells.x, combat.FootprintCells.y),
            ["operation_map_scenery"] = operationMapScenery,
            ["campaign_mission_unit_role"] = campaignRole,
            ["attempt_owned"] = attemptOwned,
            ["attempt_session_id"] = ownerSession,
            ["attempt_stable_object_id"] = stableObjectId,
            ["attempt_session_matches"] = attemptMatches,
            ["classification"] = operationMapScenery ? "CampaignOperationMapScenery_Excluded" :
                campaignRole ? "CampaignMissionEntity_Excluded" :
                attemptMatches ? "ExactLegacySessionOwned" : "LegacyInitialForceCandidate_RequiresSourceForceBinding",
            ["storage"] = new JObject
            {
                ["owner_faction_id"] = storage.OwnerFactionId,
                ["oil_capacity"] = storage.OilStorageCapacity,
                ["fuel_capacity"] = storage.FuelStorageCapacity,
                ["oil_barrels_per_day"] = storage.OilBarrelsPerDay,
                ["fuel_barrels_per_day"] = storage.FuelBarrelsPerDay,
                ["stored_oil_barrels"] = storage.StoredOilBarrels,
                ["stored_fuel_barrels"] = storage.StoredFuelBarrels,
                ["reserved_oil_inbound"] = storage.ReservedOilInboundBarrels,
                ["reserved_oil_outbound"] = storage.ReservedOilOutboundBarrels,
                ["reserved_fuel_inbound"] = storage.ReservedFuelInboundBarrels,
                ["reserved_fuel_outbound"] = storage.ReservedFuelOutboundBarrels,
                ["civilian_fuel_reserve"] = storage.CivilianFuelReserveBarrels,
                ["received_hauler_fuel"] = storage.ReceivedHaulerFuelBarrels,
                ["version"] = storage.Version
            }
        };
    }

    internal static JObject Run(EntityManager source)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptIn), "1", StringComparison.Ordinal)) return null;

        using var matches = source.CreateEntityQuery(typeof(SkirmishMatchState));
        Require(matches.CalculateEntityCount() == 1, "S073 economy census requires exactly one retained legacy match.");
        var match = matches.GetSingleton<SkirmishMatchState>();
        Require(match.ScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex &&
                match.Phase == SkirmishPhase.Preparing,
            "S073 economy census requires the retained Industrial Basin Preparing source host.");

        using var economies = source.CreateEntityQuery(typeof(FactionEconomy));
        using var economyEntities = economies.ToEntityArray(Allocator.Temp);
        var factionEconomyCounts = new Dictionary<byte, int>();
        var factionMaterialBankCounts = new Dictionary<byte, int>();
        var factionMatchingMaterialBankCounts = new Dictionary<byte, int>();
        int exactEconomySessionOwners1 = 0, exactEconomySessionOwners2 = 0;
        var economyRows = new JArray();
        foreach (var entity in economyEntities)
        {
            var row = EconomyRow(source, entity, match.SessionId);
            byte faction = (byte)row["faction_id"].Value<int>();
            factionEconomyCounts[faction] = factionEconomyCounts.TryGetValue(faction, out int count) ? count + 1 : 1;
            bool hasMaterials = source.HasComponent<FactionTacticalMaterialsComponent>(entity);
            if (hasMaterials) factionMaterialBankCounts[faction] = factionMaterialBankCounts.TryGetValue(faction, out int materialCount) ? materialCount + 1 : 1;
            if (hasMaterials && source.GetComponentData<FactionTacticalMaterialsComponent>(entity).FactionId == faction)
                factionMatchingMaterialBankCounts[faction] = factionMatchingMaterialBankCounts.TryGetValue(faction, out int matchingMaterialCount) ? matchingMaterialCount + 1 : 1;
            bool exactOwner = (bool)row["attempt_session_matches"];
            if (exactOwner && faction == 1) exactEconomySessionOwners1++;
            if (exactOwner && faction == 2) exactEconomySessionOwners2++;
            economyRows.Add(row);
        }

        using var initialConfigQuery = source.CreateEntityQuery(typeof(InitialUnitsSpawnConfig));
        Require(initialConfigQuery.CalculateEntityCount() == 1,
            "S073 economy census requires one source-owned InitialUnitsSpawnConfig readback.");
        var initialConfig = initialConfigQuery.GetSingleton<InitialUnitsSpawnConfig>();
        var sourceInitialExpectations = new JObject
        {
            ["initial_dollars"] = 0,
            ["initial_materials"] = 220,
            ["materials_capacity"] = 600,
            ["initial_ai_materials"] = 220,
            ["ai_materials_capacity"] = 600,
            ["initial_oil"] = 0,
            ["initial_fuel"] = 160
        };
        var runtimeInitialReadback = new JObject
        {
            ["initial_dollars"] = initialConfig.InitialDollars,
            ["initial_materials"] = initialConfig.InitialMaterials,
            ["materials_capacity"] = initialConfig.MaterialsCapacity,
            ["initial_ai_materials"] = initialConfig.InitialAiMaterials,
            ["ai_materials_capacity"] = initialConfig.AiMaterialsCapacity,
            ["initial_oil"] = initialConfig.InitialOil,
            ["initial_fuel"] = initialConfig.InitialFuel
        };
        bool sourceInitialExpectationsMatch = JToken.DeepEquals(sourceInitialExpectations, runtimeInitialReadback);

        using var storageQuery = source.CreateEntityQuery(
            typeof(BuildingResourceStorageComponent), typeof(UnitSourcePrefabKey), typeof(Faction),
            typeof(RuntimeBuildingCombatInfo));
        using var storageEntities = storageQuery.ToEntityArray(Allocator.Temp);
        var storageRows = new JArray();
        int playerBladders = 0, enemyBladders = 0, excludedCampaignScenery = 0;
        int playerOwnerMatches = 0, enemyOwnerMatches = 0, playerSessionOwned = 0, enemySessionOwned = 0;
        foreach (var entity in storageEntities)
        {
            var key = source.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            bool bladder = key.IndexOf("fuel_bladder", StringComparison.OrdinalIgnoreCase) >= 0;
            bool campaignScenery = source.HasComponent<OperationMapBuildingComponent>(entity) ||
                                   source.HasComponent<CampaignMissionUnitRoleComponent>(entity);
            if (campaignScenery) excludedCampaignScenery++;
            if (bladder && !campaignScenery)
            {
                byte faction = source.GetComponentData<Faction>(entity).Id;
                var storage = source.GetComponentData<BuildingResourceStorageComponent>(entity);
                bool ownerMatches = storage.OwnerFactionId == faction &&
                    source.GetComponentData<RuntimeBuildingCombatInfo>(entity).OwnerFactionId == faction;
                bool exactSessionOwner = source.HasComponent<SkirmishAttemptOwnedComponent>(entity) &&
                    source.GetComponentData<SkirmishAttemptOwnedComponent>(entity).SessionId.Equals(match.SessionId);
                if (faction == 1) { playerBladders++; if (ownerMatches) playerOwnerMatches++; if (exactSessionOwner) playerSessionOwned++; }
                else if (faction == 2) { enemyBladders++; if (ownerMatches) enemyOwnerMatches++; if (exactSessionOwner) enemySessionOwned++; }
            }
            storageRows.Add(StorageRow(source, entity, match.SessionId));
        }

        // Preserve ambiguity as evidence. Disposable-copy admission requires one
        // 1/2 economy bank and one unambiguously source-bound bladder per faction.
        var economyMultiplicity = new JObject();
        foreach (var pair in factionEconomyCounts) economyMultiplicity[pair.Key.ToString()] = pair.Value;
        return new JObject
        {
            ["kind"] = "S073LegacyEconomyStorageCensus",
            ["schema_version"] = 1,
            ["source_host_mutated"] = false,
            ["session_id"] = match.SessionId.ToString(),
            ["scenario_index"] = match.ScenarioIndex,
            ["phase"] = match.Phase.ToString(),
            ["source_initial_forces_provenance"] = "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset; retained source SHA-256 in Fixtures/S073/retained/provenance.json",
            ["source_initial_value_expectations"] = sourceInitialExpectations,
            ["runtime_initial_units_spawn_config"] = runtimeInitialReadback,
            ["expectations_match_runtime_readback"] = sourceInitialExpectationsMatch,
            ["runtime_readback_provenance"] = "InitialUnitsSpawnConfig in retained source host; it is populated from the pinned legacy_config bundle validated by the S073 source runner.",
            ["economies_by_faction"] = economyMultiplicity,
            ["economy_banks_with_tactical_materials_by_faction"] = DictionaryToJson(factionMaterialBankCounts),
            ["economy_banks_with_matching_tactical_materials_faction_id_by_faction"] = DictionaryToJson(factionMatchingMaterialBankCounts),
            ["exact_attempt_owned_economy_bank_counts"] = new JObject
            {
                ["faction_1"] = exactEconomySessionOwners1,
                ["faction_2"] = exactEconomySessionOwners2
            },
            ["economies"] = economyRows,
            ["storage_population"] = storageRows,
            ["fuel_bladder_candidate_counts"] = new JObject
            {
                ["faction_1"] = playerBladders,
                ["faction_2"] = enemyBladders,
                ["faction_1_runtime_owner_matches"] = playerOwnerMatches,
                ["faction_2_runtime_owner_matches"] = enemyOwnerMatches,
                ["faction_1_exact_attempt_owned"] = playerSessionOwned,
                ["faction_2_exact_attempt_owned"] = enemySessionOwned,
                ["campaign_scenery_excluded"] = excludedCampaignScenery
            },
            ["disposable_copy_admissible"] = factionEconomyCounts.TryGetValue((byte)1, out int playerBanks) && playerBanks == 1 &&
                factionEconomyCounts.TryGetValue((byte)2, out int enemyBanks) && enemyBanks == 1 &&
                factionMaterialBankCounts.TryGetValue((byte)1, out int playerMaterialBanks) && playerMaterialBanks == 1 &&
                factionMaterialBankCounts.TryGetValue((byte)2, out int enemyMaterialBanks) && enemyMaterialBanks == 1 &&
                factionMatchingMaterialBankCounts.TryGetValue((byte)1, out int playerMatchingMaterialBanks) && playerMatchingMaterialBanks == 1 &&
                factionMatchingMaterialBankCounts.TryGetValue((byte)2, out int enemyMatchingMaterialBanks) && enemyMatchingMaterialBanks == 1 &&
                sourceInitialExpectationsMatch &&
                playerBladders == 1 && enemyBladders == 1 && playerOwnerMatches == 1 && enemyOwnerMatches == 1 &&
                playerSessionOwned == 1 && enemySessionOwned == 1 &&
                exactEconomySessionOwners1 == 1 && exactEconomySessionOwners2 == 1,
            ["admission_rule"] = "Require one FactionEconomy plus same-entity FactionTacticalMaterialsComponent per faction 1/2; one fuel_bladder storage entity per faction with Faction and RuntimeBuildingCombatInfo owners matching storage OwnerFactionId; exclude OperationMapBuildingComponent/CampaignMissionUnitRoleComponent scenery. Exact import admission also requires SkirmishAttemptOwnedComponent.SessionId to match this legacy session on both banks and both bladders. If any ownership tag is absent, keep this as census evidence only until exact source declarations bind the entities."
        };
    }

    static JObject DictionaryToJson(Dictionary<byte, int> values)
    {
        var result = new JObject();
        foreach (var pair in values) result[pair.Key.ToString()] = pair.Value;
        return result;
    }
}
