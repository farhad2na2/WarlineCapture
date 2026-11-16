using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Simulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Opt-in diagnostic copier. It builds a bounded, disposable source-derived input
// world; it does not schedule AI owners or make a mission readiness claim.
internal static partial class SourceS073TacticalAIDisposableCopyFixture
{
    const string OptIn = "WARLINE_S073_TACTICAL_AI_COPY_FIXTURE";
    const string OwnerOptIn = "WARLINE_S073_TACTICAL_AI_RUN_OWNERS";
    const string RulesOptIn = "WARLINE_S073_TACTICAL_AI_RUN_RULES_OWNER";
    const string TargetPolicyDiagnosticOptIn = "WARLINE_S073_TACTICAL_AI_TARGET_POLICY_DIAGNOSTIC";
    const string MovementOwnerDiagnosticOptIn = "WARLINE_S073_TACTICAL_AI_MOVEMENT_OWNER_DIAGNOSTIC";
    const int MaxOwnerTicks = 3;
    const int TargetPolicyDiagnosticTicks = 122;
    const int MovementOwnerDiagnosticTicks = 180;
    const float OwnerTickDelta = 0.5f;
    const float MovementOwnerTickDelta = 1f/60f;
    const int MaxPopulation = 5200;

    sealed class SameRunBirthInputs
    {
        internal readonly Dictionary<byte, Entity> EconomyBanks = new Dictionary<byte, Entity>();
        internal readonly Dictionary<byte, JObject> FinalEconomySnapshots = new Dictionary<byte, JObject>();
        internal readonly Dictionary<byte, Entity> FuelBladders = new Dictionary<byte, Entity>();
        internal readonly Dictionary<byte, BuildingResourceStorageComponent> FuelBladderStorage = new Dictionary<byte, BuildingResourceStorageComponent>();
        internal JObject Audit;
    }

    static void CopyIf<T>(EntityManager src, Entity from, EntityManager dst, Entity to) where T : struct
    { if (src.HasComponent<T>(from)) dst.AddComponentData(to, src.GetComponentData<T>(from)); }

    static void CopyBuffer<T>(EntityManager src, Entity from, EntityManager dst, Entity to) where T : struct, IBufferElementData
    {
        if (!src.HasBuffer<T>(from)) return;
        var input = src.GetBuffer<T>(from);
        var output = dst.AddBuffer<T>(to);
        for (int i = 0; i < input.Length; i++) output.Add(input[i]);
    }

    static void CopyRulesStartupConfig(EntityManager src, Entity from, EntityManager dst, Entity to)
    {
        if (!src.HasComponent<InitialUnitsSpawnConfig>(from)) return;
        InitialUnitsSpawnConfig config = src.GetComponentData<InitialUnitsSpawnConfig>(from);
        // The original Rules owner reads only the authored numeric startup fields.
        // Visual/presentation prefab identities are outside this tactical copy.
        config.BlockerPrefab = Entity.Null;
        config.UnitSelectionMarkerPrefab = Entity.Null;
        config.UnitHealthBarPrefab = Entity.Null;
        dst.AddComponentData(to, config);
    }

    static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }

    static Entity ParseEntityIdentity(string value, string label)
    {
        string[] parts = (value ?? string.Empty).Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out int index) || !int.TryParse(parts[1], out int version) || index < 0 || version <= 0)
            throw new InvalidDataException("Invalid " + label + " entity identity");
        return new Entity { Index = index, Version = version };
    }

    static void ValidateObservationEnvelope(EntityManager source, SkirmishMatchState match, JObject observation, string owner)
    {
        Require(observation != null && (string)observation["kind"] == "S073EconomyStartupBirthObservation" &&
            (string)observation["owner_invocation"] == owner && (string)observation["scope"] ==
            "SameLiveWorldEntityManagerImmediatelyBeforeAndAfterSuppliedOriginalOwnerInvocation" &&
            (string)observation["source_host_label"] == "RetainedS073LegacySourceHost" &&
            (bool?)observation["observer_mutated_ecs"] == false && (bool?)observation["original_owner_invoked_once"] == true,
            owner + " same-world opt-in owner observation required");
        ulong sequence = source.WorldUnmanaged.SequenceNumber;
        Require((ulong)observation["world_sequence_number_before"] == sequence &&
            (ulong)observation["world_sequence_number_after"] == sequence &&
            (string)observation["session_id"] == match.SessionId.ToString() &&
            (int?)observation["scenario_index"] == match.ScenarioIndex && (int?)observation["seed"] == match.Seed &&
            (string)observation["phase"] == match.Phase.ToString() &&
            JToken.DeepEquals(observation["match_identity_before"], observation["match_identity_after"]),
            owner + " session/world identity changed across original owner invocation");
        var identity = (JObject)observation["match_identity_after"];
        Require((string)identity["session_id"] == match.SessionId.ToString() &&
            (int?)identity["scenario_index"] == match.ScenarioIndex && (int?)identity["seed"] == match.Seed &&
            (string)identity["phase"] == match.Phase.ToString(), owner + " match identity differs from live source");
    }

    static bool MatchesEconomy(EntityManager source, Entity entity, JObject snapshot)
    {
        if (!source.Exists(entity) || !source.HasComponent<FactionEconomy>(entity)) return false;
        FactionEconomy e = source.GetComponentData<FactionEconomy>(entity);
        JObject x = (JObject)snapshot["economy"];
        if (x == null || (int?)x["money"] != e.Money || (byte?)x["materials_only_construction"] != e.MaterialsOnlyConstruction ||
            (float?)x["oil"] != e.Oil || (float?)x["fuel"] != e.Fuel || (float?)x["oil_income_rate"] != e.OilIncomeRate ||
            (float?)x["fuel_income_rate"] != e.FuelIncomeRate || (float?)x["last_sell_time"] != e.LastSellTime ||
            (float?)x["last_log_time"] != e.LastLogTime) return false;
        bool hasPolicy = source.HasComponent<FactionEconomyPolicy>(entity);
        if ((bool?)snapshot["has_policy"] != hasPolicy) return false;
        if (hasPolicy)
        {
            FactionEconomyPolicy p = source.GetComponentData<FactionEconomyPolicy>(entity);
            JObject y = (JObject)snapshot["policy"];
            if (y == null || (byte?)y["enabled"] != p.Enabled || (float?)y["income_multiplier"] != p.IncomeMultiplier ||
                (int?)y["oil_sell_price"] != p.OilSellPrice || (int?)y["fuel_sell_price"] != p.FuelSellPrice ||
                (float?)y["sell_interval_seconds"] != p.SellIntervalSeconds) return false;
        }
        bool hasMaterials = source.HasComponent<FactionTacticalMaterialsComponent>(entity);
        if ((bool?)snapshot["has_tactical_materials"] != hasMaterials) return false;
        if (hasMaterials)
        {
            FactionTacticalMaterialsComponent m = source.GetComponentData<FactionTacticalMaterialsComponent>(entity);
            JObject y = (JObject)snapshot["tactical_materials"];
            if (y == null || (byte?)y["faction_id"] != m.FactionId || (int?)y["current"] != m.Current ||
                (int?)y["capacity"] != m.Capacity || (uint?)y["version"] != m.Version) return false;
        }
        return true;
    }

    static bool SameStorage(BuildingResourceStorageComponent x, BuildingResourceStorageComponent y)
    {
        return x.RuntimeBuildingId == y.RuntimeBuildingId && x.OwnerFactionId == y.OwnerFactionId &&
            x.OilStorageCapacity == y.OilStorageCapacity && x.FuelStorageCapacity == y.FuelStorageCapacity &&
            x.OilBarrelsPerDay == y.OilBarrelsPerDay && x.FuelBarrelsPerDay == y.FuelBarrelsPerDay &&
            x.StoredOilBarrels == y.StoredOilBarrels && x.StoredFuelBarrels == y.StoredFuelBarrels &&
            x.ReservedOilInboundBarrels == y.ReservedOilInboundBarrels &&
            x.ReservedOilOutboundBarrels == y.ReservedOilOutboundBarrels &&
            x.ReservedFuelInboundBarrels == y.ReservedFuelInboundBarrels &&
            x.ReservedFuelOutboundBarrels == y.ReservedFuelOutboundBarrels &&
            x.CivilianFuelReserveBarrels == y.CivilianFuelReserveBarrels &&
            x.ReceivedHaulerFuelBarrels == y.ReceivedHaulerFuelBarrels && x.Version == y.Version;
    }

    static JObject OptionalObject(JObject parent, string property, string label)
    {
        JToken token = parent?[property];
        if (token == null || token.Type == JTokenType.Null) return null;
        if (token is JObject value) return value;
        throw new InvalidDataException(label + "." + property + " must be an object when present; observed " + token.Type);
    }

    static SameRunBirthInputs ValidateSameRunBirthInputs(EntityManager source, Entity sessionEntity,
        SkirmishMatchState match, JObject directBirthIdentity, JObject aiObservation, JArray stepObservations)
    {
        Require(directBirthIdentity != null && (string)directBirthIdentity["scope"] ==
            "OptInDirectOriginalLegacyRequestToRegistrationIdentity" && (bool?)directBirthIdentity["read_only"] == true &&
            (string)directBirthIdentity["session_id"] == match.SessionId.ToString() &&
            (int?)directBirthIdentity["scenario_index"] == match.ScenarioIndex && (int?)directBirthIdentity["seed"] == match.Seed &&
            (byte?)directBirthIdentity["phase"] == (byte)match.Phase &&
            (ulong?)directBirthIdentity["world_sequence_number"] == source.WorldUnmanaged.SequenceNumber &&
            (string)directBirthIdentity["session_entity"] == $"{sessionEntity.Index}:{sessionEntity.Version}" &&
            (int?)directBirthIdentity["expected_fuel_bladder_source_rows"] == 2 &&
            (int?)directBirthIdentity["fuel_bladder_bindings"] == 2 &&
            (bool?)directBirthIdentity["fuel_bladder_bindings_match_source_count"] == true,
            "Direct same-run S073 building request-to-entity capture required");

        ValidateObservationEnvelope(source, match, aiObservation, "S073AIStartupSourceBinding.Initialize");
        Require((int?)aiObservation["economy_bank_count_before"] == 0 &&
            (int?)aiObservation["economy_bank_count_after"] == 2 && (int?)aiObservation["created_bank_count"] == 2 &&
            (int?)aiObservation["existing_bank_count_after"] == 0 && (int?)aiObservation["removed_bank_count"] == 0,
            "AI startup must directly create exactly the two source faction economy banks");
        Require(stepObservations != null && stepObservations.Count > 0,
            "At least one same-run original InitialUnits step observation required");
        JObject finalStep = (JObject)stepObservations[stepObservations.Count - 1];
        ValidateObservationEnvelope(source, match, finalStep, "OriginalS073InitialUnitsSpawnOwner.Step");
        Require((int?)finalStep["economy_bank_count_before"] == 2 && (int?)finalStep["economy_bank_count_after"] == 2 &&
            (int?)finalStep["created_bank_count"] == 0 && (int?)finalStep["existing_bank_count_after"] == 2 &&
            (int?)finalStep["removed_bank_count"] == 0,
            "Final InitialUnits step must preserve the exact AI-startup economy bank population");

        var result = new SameRunBirthInputs();
        var aiBanks = (JArray)aiObservation["banks_after"];
        var finalBanks = (JArray)finalStep["banks_after"];
        Require(aiBanks != null && aiBanks.Count == 2 && finalBanks != null && finalBanks.Count == 2,
            "Exact two-bank startup and final InitialUnits observations required");
        foreach (JObject bank in aiBanks)
        {
            byte faction = (byte)bank["faction_id"].Value<int>();
            Require(faction is 1 or 2 && (string)bank["birth"] == "CreatedDuringOriginalOwnerInvocation" &&
                (bool?)bank["has_attempt_owner"] == false && (bool?)bank["has_policy"] == true &&
                (bool?)bank["has_tactical_materials"] == true &&
                !result.EconomyBanks.ContainsKey(faction), "Unique source-created economy bank for each faction required");
            Entity entity = ParseEntityIdentity((string)bank["entity_identity"], "economy bank");
            Require(entity.Index == (int)bank["entity_index"] && entity.Version == (int)bank["entity_version"] &&
                source.Exists(entity) && source.HasComponent<FactionEconomy>(entity) &&
                source.GetComponentData<FactionEconomy>(entity).FactionId == faction,
                "AI startup bank observation is not the same live source entity");
            JObject final = finalBanks.OfType<JObject>().SingleOrDefault(x => (string)x["entity_identity"] == (string)bank["entity_identity"]);
            Require(final != null && (string)final["birth"] == "ExistingEntity" &&
                MatchesEconomy(source, entity, final) && (int?)final["tactical_materials"]?["current"] == 220 &&
                (int?)final["tactical_materials"]?["capacity"] == 600,
                "Final InitialUnits step did not retain the exact source bank with 220/600 tactical materials");
            result.EconomyBanks.Add(faction, entity);
            result.FinalEconomySnapshots.Add(faction, (JObject)final.DeepClone());
        }
        Require(result.EconomyBanks.Count == 2 && result.EconomyBanks.ContainsKey(1) && result.EconomyBanks.ContainsKey(2),
            "Both exact source faction banks are required");

        var directRows = (JArray)directBirthIdentity["rows"];
        Require(directRows != null && directRows.Count == 10, "All ten original starting building requests must have direct entity identities");
        var seenRequests = new HashSet<int>(); var seenEntities = new HashSet<Entity>();
        foreach (JObject row in directRows)
        {
            Require((string)row["binding"] == "DirectRequestContextToRegistrationReturn" &&
                (int?)row["request_id"] > 0 && seenRequests.Add((int)row["request_id"]) &&
                (string)row["request_building_id"] == (string)row["runtime_definition_key"] &&
                (int?)row["request_faction_id"] == (int?)row["runtime_faction_id"],
                "Direct source request and returned runtime building identity differ");
            var sourceRows = (JArray)row["source_config_rows"];
            Require(sourceRows != null && sourceRows.Count == 1 &&
                (byte?)sourceRows[0]["faction_id"] == (byte?)row["request_faction_id"] &&
                (string)sourceRows[0]["building_id"] == (string)row["request_building_id"],
                "Direct request must resolve one exact original startup buffer row");
            Entity entity = ParseEntityIdentity((string)row["combat_entity"], "runtime building combat");
            Require(seenEntities.Add(entity) && source.Exists(entity) && source.HasComponent<RuntimeBuildingCombatTag>(entity) &&
                source.HasComponent<RuntimeBuildingCombatInfo>(entity) && source.HasComponent<UnitSourcePrefabKey>(entity) &&
                source.HasComponent<Faction>(entity) && source.HasComponent<UnitHealth>(entity),
                "Directly observed source building combat entity is missing or duplicated");
            string key = source.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString().ToLowerInvariant();
            var factionComponent = source.GetComponentData<Faction>(entity);
            var combat = source.GetComponentData<RuntimeBuildingCombatInfo>(entity);
            Require(key == (string)row["request_building_id"] && factionComponent.Id == (byte)row["request_faction_id"] &&
                combat.RuntimeBuildingId == (int)row["runtime_building_id"] && combat.OwnerFactionId == factionComponent.Id &&
                combat.OriginCell.x == (int)row["runtime_origin"][0] && combat.OriginCell.y == (int)row["runtime_origin"][1] &&
                !source.HasComponent<OperationMapBuildingComponent>(entity) &&
                !source.HasComponent<CampaignMissionUnitRoleComponent>(entity) &&
                !source.HasComponent<SkirmishAttemptOwnedComponent>(entity),
                "Direct birth entity is not the same source-owned ordinary building identity");
            if ((string)row["request_building_id"] == "building_fuel_bladder")
            {
                byte faction = (byte)row["request_faction_id"];
                Require(faction is 1 or 2 && !result.FuelBladders.ContainsKey(faction) &&
                    source.HasComponent<BuildingResourceStorageComponent>(entity),
                    "Exact direct-start bladder must carry source-owned resource storage");
                BuildingResourceStorageComponent storage = source.GetComponentData<BuildingResourceStorageComponent>(entity);
                Require(storage.RuntimeBuildingId == combat.RuntimeBuildingId && storage.OwnerFactionId == faction &&
                    storage.FuelStorageCapacity > 0 && float.IsFinite(storage.StoredFuelBarrels),
                    "Direct-start bladder storage identity and owner must match the source building");
                result.FuelBladders.Add(faction, entity);
                result.FuelBladderStorage.Add(faction, storage);
            }
        }
        Require(seenRequests.Count == 10 && result.FuelBladders.Count == 2 &&
            result.FuelBladders.ContainsKey(1) && result.FuelBladders.ContainsKey(2),
            "Same-run direct request capture must bind one fuel bladder for each faction");
        result.Audit = new JObject
        {
            ["session_id"] = match.SessionId.ToString(), ["world_sequence_number"] = source.WorldUnmanaged.SequenceNumber,
            ["ai_startup_created_banks"] = 2, ["final_initial_units_step_retained_banks"] = 2,
            ["direct_original_building_requests"] = seenRequests.Count, ["direct_source_fuel_bladder_bindings"] = result.FuelBladders.Count,
            ["fuel_bladder_entities"] = new JArray(result.FuelBladders.OrderBy(x => x.Key).Select(x => new JObject
                { ["faction_id"] = x.Key, ["source_entity"] = $"{x.Value.Index}:{x.Value.Version}" })),
            ["economy_bank_entities"] = new JArray(result.EconomyBanks.OrderBy(x => x.Key).Select(x => new JObject
                { ["faction_id"] = x.Key, ["source_entity"] = $"{x.Value.Index}:{x.Value.Version}" })),
            ["campaign_economy_storage_imported"] = false, ["source_world_mutated"] = false
        };
        return result;
    }

    internal static JObject Run(EntityManager source, string outputDirectory, string combatPresetPath,
        LegacyInitialSpawnData sourceLegacyConfig, int reinforcementInfantryTarget, int attackSquadSize,
        JObject directBirthIdentity,
        JObject aiEconomyBirthObservation, JArray initialUnitsEconomyBirthObservations,
        Dictionary<int, RuntimeBuildingEntity> sourceBuildings, S073NativeConfiguredUnits sourceConfiguredUnits,
        string repositoryRoot, bool historicalInputDiagnostic, SourceS073HostAbsentPresentationCameraCapability sourceCameraCapability,
        string productionPrefabPath, string productionPrefabSha256)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptIn), "1", StringComparison.Ordinal)) return null;
        using var matches = source.CreateEntityQuery(typeof(SkirmishMatchState));
        using var gameplay = source.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
        Require(matches.CalculateEntityCount() == 1 && matches.GetSingleton<SkirmishMatchState>().Phase == SkirmishPhase.Preparing,
            "Tactical AI copy requires the retained inactive Preparing source session");
        SkirmishMatchState sourceMatch = matches.GetSingleton<SkirmishMatchState>();
        Require(sourceMatch.Seed == 104729 && sourceMatch.ScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex,
            "S073 disposable copy requires the exact Industrial Basin mission and seed");
        Require(gameplay.CalculateEntityCount() == 1 && gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive == 0,
            "Tactical AI copy requires source simulation inactive");
        RuntimeGameplayStateComponent sourceGameplayBefore=gameplay.GetSingleton<RuntimeGameplayStateComponent>();
        Entity sourceSessionEntity = matches.GetSingletonEntity();
        Require(sourceLegacyConfig != null, "Pinned source legacy startup config required for copied original rules owner");
        SameRunBirthInputs sameRunBirths = ValidateSameRunBirthInputs(source, sourceSessionEntity, sourceMatch,
            directBirthIdentity, aiEconomyBirthObservation, initialUnitsEconomyBirthObservations);
        JObject sourceGuardBefore = CaptureSourceGuard(source, sourceSessionEntity, gameplay.GetSingletonEntity(), sameRunBirths);

        using var units = source.CreateEntityQuery(typeof(UnitHealth));
        using var factionGrid = source.CreateEntityQuery(typeof(Faction), typeof(UnitGrid));
        using var squads = source.CreateEntityQuery(typeof(AISquad));
        using var plans = source.CreateEntityQuery(typeof(AISquadPlan));
        using var buildPlans = source.CreateEntityQuery(typeof(AIBuildPlan));
        using var productionPlans = source.CreateEntityQuery(typeof(AIProductionPlan));
        using var productionBoundaries = source.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
            ComponentType.ReadOnly<BuildingConfiguredUnitReadModel>(),
            ComponentType.ReadOnly<BuildingRuntimeUnitProductionSummary>(),
            ComponentType.ReadWrite<BuildingFactionUnitProductionRequest>());
        using var productionSlots = source.CreateEntityQuery(typeof(BuildingProductionSlotReadModel));
        using var productionSpawnPoints = source.CreateEntityQuery(typeof(BuildingFactionProductionSpawnPointReadModel));
        using var priorities = source.CreateEntityQuery(typeof(AITargetPrioritySetting));
        using var startupPlans = source.CreateEntityQuery(typeof(CustomGameStartupStateComponent), typeof(InitialUnitsSpawnConfig));
        using var grids = source.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
        using var mapSurfaces=source.CreateEntityQuery(typeof(MapSurfaceComponent));
        using var controls = source.CreateEntityQuery(typeof(FactionControlConfigTag), typeof(FactionControlEntry));
        using var diagnostics = source.CreateEntityQuery(typeof(RuntimeDiagnosticsStateComponent));
        using var logQueues = source.CreateEntityQuery(typeof(AIDiagnosticLogQueueComponent));
        using var campaignRoles = source.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent));
        using var baseCandidates = source.CreateEntityQuery(typeof(RuntimeBuildingCombatTag),typeof(UnitSourcePrefabKey),typeof(Faction),typeof(UnitHealth));
        using var orderedSourceBaseCandidates = baseCandidates.ToEntityArray(Allocator.Temp);
        var sourceBaseOrder = orderedSourceBaseCandidates.ToArray();
        Require(units.CalculateEntityCount() > 0 && units.CalculateEntityCount() <= MaxPopulation, "Source combat population exceeds the explicit copy bound");
        Require(plans.CalculateEntityCount() > 0 && plans.CalculateEntityCount() <= 16 && priorities.CalculateEntityCount() <= 16,
            "AI plan/priority population is outside the bounded fixture domain");
        Require(grids.CalculateEntityCount() == 1, "Exactly one source grid with complete AI breach geometry is required");
        Require(mapSurfaces.CalculateEntityCount()<=1,"At most one source map-surface singleton is supported by the disposable movement copy");
        Require(startupPlans.CalculateEntityCount() == 1, "Exactly one source-owned startup plan required for original rules preset projection");
        Entity sourceStartupPlan = startupPlans.GetSingletonEntity();
        Entity sourceGrid = grids.GetSingletonEntity();
        GridConfig gridConfig = source.GetComponentData<GridConfig>(sourceGrid);
        long cellsLong = (long)gridConfig.Width * gridConfig.Height;
        Require(cellsLong > 0 && cellsLong <= 2_097_152, "Source grid exceeds the explicit cell bound");

        // Union only the source owner query population and their singleton/config roots.
        var originals = new HashSet<Entity>();
        AddQueryEntities(source, units, originals); AddQueryEntities(source, factionGrid, originals); AddQueryEntities(source, squads, originals);
        AddQueryEntities(source, plans, originals); AddQueryEntities(source, priorities, originals);
        AddQueryEntities(source, grids, originals); AddQueryEntities(source, controls, originals);
        AddQueryEntities(source,mapSurfaces,originals);
        AddQueryEntities(source, diagnostics, originals); AddQueryEntities(source, logQueues, originals);
        AddQueryEntities(source, matches, originals);
        AddSingletonEntities<RuntimeGameplayStateComponent>(source, originals);
        originals.Add(sourceStartupPlan);
        foreach (var bank in sameRunBirths.EconomyBanks.Values) originals.Add(bank);
        foreach (var bladder in sameRunBirths.FuelBladders.Values) originals.Add(bladder);
        AddQueryEntities(source, buildPlans, originals); AddQueryEntities(source, productionPlans, originals);
        AddQueryEntities(source, productionBoundaries, originals);
        AddQueryEntities(source, productionSlots, originals); AddQueryEntities(source, productionSpawnPoints, originals);
        AddSessionBaseReference(source,sourceMatch.PlayerMainBase,originals,"PlayerMainBase");
        AddSessionBaseReference(source,sourceMatch.EnemyMainBase,originals,"EnemyMainBase");
        Require(originals.Count <= MaxPopulation + 64, "Combined source closure exceeds the explicit entity bound");
        JObject closureAudit;
        try { closureAudit=AuditSourceClosure(source, originals, sourceGrid, units.CalculateEntityCount(), factionGrid.CalculateEntityCount(),
            plans.CalculateEntityCount(), buildPlans.CalculateEntityCount(), productionPlans.CalculateEntityCount(),
            priorities.CalculateEntityCount(), squads.CalculateEntityCount(), matches.CalculateEntityCount(),
            controls.CalculateEntityCount(), campaignRoles.CalculateEntityCount(), new HashSet<Entity>(sameRunBirths.EconomyBanks.Values),
            new HashSet<Entity>(sameRunBirths.FuelBladders.Values)); }
        catch(Exception ex)
        {
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(Path.Combine(outputDirectory,"s073-tactical-ai-disposable-copy.json"),new JObject {
                ["kind"]="S073TacticalAIDisposableCopyFixture",["schema_version"]=1,
                ["status"]="FailClosed_SourceClosureAuditFailed",["source_entity_count"]=originals.Count,
                ["source_combat_population"]=units.CalculateEntityCount(),["source_faction_grid_population"]=factionGrid.CalculateEntityCount(),
                ["source_grid_cells"]=(int)cellsLong,["source_session_phase"]=(byte)sourceMatch.Phase,
                ["source_world_mutated"]=false,["fixture_world_created"]=false,["ai_owner_invoked"]=false,
                ["failure"]=ex.Message
            }.ToString(Formatting.Indented)+"\n");
            throw new InvalidDataException("Tactical AI copy blocked by source closure audit; retained fail-closed evidence.",ex);
        }

        var copy = new EntityManager();
        int next = 1;
        copy.LogicalAllocatorAdmitted = true;
        copy.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var remap = new Dictionary<Entity,Entity>();
        var disposableEntities = new HashSet<Entity>();
        try
        {
        foreach(var e in originals){Entity clone=copy.CreateEntity();remap.Add(e,clone);disposableEntities.Add(clone);}
        AssertExternalReferenceRejected(remap);
        foreach (var pair in remap)
        {
            Entity a = pair.Key, b = pair.Value;
            CopyIf<Faction>(source,a,copy,b); CopyIf<UnitGrid>(source,a,copy,b); CopyIf<UnitHealth>(source,a,copy,b);
            // Original SkirmishRulesSystem discovers faction bases from the real
            // prefab key. Preserve that source identity; never infer or assign a
            // base identity in the fixture.
            CopyIf<UnitSourcePrefabKey>(source,a,copy,b);
            CopyIf<UnitCombat>(source,a,copy,b); CopyIf<UnitAttack>(source,a,copy,b); CopyIf<LocalTransform>(source,a,copy,b);
            // Tactical movement inputs are copied only from the corresponding
            // live source entity. They are never synthesized to satisfy an owner.
            CopyIf<UnitMove>(source,a,copy,b); CopyIf<UnitFootprint>(source,a,copy,b);
            CopyIf<UnitMovementBehavior>(source,a,copy,b); CopyIf<UnitVehicleKinematics>(source,a,copy,b);
            CopyIf<UnitVehicleMovement>(source,a,copy,b);
            CopyIf<MapSurfaceComponent>(source,a,copy,b);
            CopyIf<AIControlledTag>(source,a,copy,b); CopyIf<CampaignMissionUnitRoleComponent>(source,a,copy,b);
            CopyIf<RuntimeBuildingCombatTag>(source,a,copy,b); CopyIf<RuntimeBuildingCombatInfo>(source,a,copy,b);
            CopyIf<OperationMapBuildingComponent>(source,a,copy,b);
            CopyIf<StaticGridBlocker>(source,a,copy,b); CopyIf<GridBlockerSize>(source,a,copy,b);
            // AI targeting only needs this exclusion marker's presence. Do not
            // import cargo amounts or other live logistics resource state.
            if (source.HasComponent<UnitResourceHauler>(a)) copy.AddComponentData(b, default(UnitResourceHauler));
            CopyIf<FuelLogisticsOilSourceTag>(source,a,copy,b);
            CopyIf<FuelLogisticsRefineryInputTag>(source,a,copy,b); CopyIf<FuelLogisticsRefineryOutputTag>(source,a,copy,b);
            CopyIf<FuelLogisticsFuelStorageTag>(source,a,copy,b); CopyIf<UnitAirMovement>(source,a,copy,b);
            CopyIf<AISquadPlan>(source,a,copy,b); CopyIf<AITargetPrioritySetting>(source,a,copy,b);
            CopyIf<AIBuildPlan>(source,a,copy,b); CopyIf<AIProductionPlan>(source,a,copy,b);
            CopyIf<FactionEconomy>(source,a,copy,b); CopyIf<FactionEconomyPolicy>(source,a,copy,b);
            CopyIf<FactionTacticalMaterialsComponent>(source,a,copy,b);
            if (sameRunBirths.FuelBladders.Values.Contains(a)) CopyIf<BuildingResourceStorageComponent>(source,a,copy,b);
            CopyIf<RuntimeGameplayStateComponent>(source,a,copy,b); CopyIf<FactionControlConfigTag>(source,a,copy,b);
            CopyIf<RuntimeDiagnosticsStateComponent>(source,a,copy,b); CopyIf<AIDiagnosticLogQueueComponent>(source,a,copy,b);
            CopyIf<AISquad>(source,a,copy,b); CopyIf<AISquadMember>(source,a,copy,b);
            CopyIf<SkirmishBaseDefender>(source,a,copy,b);
            CopyIf<SkirmishMatchState>(source,a,copy,b);
            CopyIf<SkirmishMainBase>(source,a,copy,b);
            CopyIf<CustomGameStartupStateComponent>(source,a,copy,b); CopyRulesStartupConfig(source,a,copy,b);
            CopyIf<EngageTarget>(source,a,copy,b); CopyIf<BaseBreachOrder>(source,a,copy,b);
            CopyIf<UnitPathRequest>(source,a,copy,b); CopyIf<UnitTarget>(source,a,copy,b);
            CopyIf<UnitPathFollow>(source,a,copy,b); CopyIf<UnitPathRange>(source,a,copy,b);
            CopyIf<UnitPathRetryCooldown>(source,a,copy,b); CopyIf<ManualControlledTag>(source,a,copy,b);
            CopyIf<AICombatOrderTag>(source,a,copy,b); CopyIf<ManualMoveOrderTag>(source,a,copy,b);
            CopyIf<ManualMoveGroupMemberTag>(source,a,copy,b); CopyIf<AutoWanderMoveTag>(source,a,copy,b);
            CopyBuffer<AISquadUnit>(source,a,copy,b); CopyBuffer<FactionControlEntry>(source,a,copy,b);
            CopyBuffer<AIDiagnosticLogComponent>(source,a,copy,b); CopyBuffer<SkirmishTrackedUnit>(source,a,copy,b);
            CopyBuffer<InitialUnitsFactionSpawnEntry>(source,a,copy,b);
            CopyBuffer<AIProductionPlanEntry>(source,a,copy,b);
            CopyIf<BuildingRuntimeStateTag>(source,a,copy,b);
            CopyBuffer<BuildingConfiguredUnitReadModel>(source,a,copy,b);
            CopyBuffer<BuildingRuntimeUnitProductionSummary>(source,a,copy,b);
            CopyBuffer<BuildingRuntimeFactionSummary>(source,a,copy,b);
            CopyBuffer<BuildingRuntimeFactionUsableFuelSummary>(source,a,copy,b);
            CopyBuffer<BuildingFactionUnitProductionRequest>(source,a,copy,b);
            CopyBuffer<BuildingProductionSlotReadModel>(source,a,copy,b);
            CopyBuffer<BuildingFactionProductionSpawnPointReadModel>(source,a,copy,b);
            if (a == sourceGrid)
            {
                CopyIf<GridConfig>(source,a,copy,b); CopyBuffer<GridWalkable>(source,a,copy,b);
                CopyBuffer<GridRoad>(source,a,copy,b); CopyBuffer<GridRoadSidewalk>(source,a,copy,b);
                CopyBuffer<GridRoadDirt>(source,a,copy,b);
                CopyDynamicGridStorage(source,a,copy,b,(int)cellsLong);
                copy.AddComponentData(b,new PathPoolComponent{Cells=new NativeList<int2>(64,Allocator.Persistent)});
            }
        }

        // Rewrite every entity edge in the copied owner closure. Any edge leaving
        // that closure is an unhandled dependency and fails closed.
        foreach (var pair in remap)
        {
            Entity from = pair.Key, to = pair.Value;
            if (source.HasComponent<AISquad>(from)) { var x=copy.GetComponentData<AISquad>(to); x.TargetEntity=Map(x.TargetEntity,remap); copy.SetComponentData(to,x); }
            if (source.HasComponent<AISquadMember>(from)) { var x=copy.GetComponentData<AISquadMember>(to); x.Squad=Map(x.Squad,remap); copy.SetComponentData(to,x); }
            if (source.HasBuffer<AISquadUnit>(from)) { var b=copy.GetBuffer<AISquadUnit>(to); for(int i=0;i<b.Length;i++){var x=b[i];x.Unit=Map(x.Unit,remap);b[i]=x;} }
            if (source.HasComponent<EngageTarget>(from)) { var x=copy.GetComponentData<EngageTarget>(to);x.Target=Map(x.Target,remap);copy.SetComponentData(to,x); }
            if (source.HasComponent<BaseBreachOrder>(from)) { var x=copy.GetComponentData<BaseBreachOrder>(to);x.FinalTarget=Map(x.FinalTarget,remap);x.BreachTarget=Map(x.BreachTarget,remap);copy.SetComponentData(to,x); }
            if (source.HasComponent<SkirmishMatchState>(from)) { var x=copy.GetComponentData<SkirmishMatchState>(to);x.PlayerMainBase=Map(x.PlayerMainBase,remap);x.EnemyMainBase=Map(x.EnemyMainBase,remap);copy.SetComponentData(to,x); }
            if (source.HasBuffer<SkirmishTrackedUnit>(from)) { var b=copy.GetBuffer<SkirmishTrackedUnit>(to);for(int i=0;i<b.Length;i++){var x=b[i];x.Entity=Map(x.Entity,remap);b[i]=x;} }
            if (source.HasBuffer<BuildingFactionUnitProductionRequest>(from))
            { var b=copy.GetBuffer<BuildingFactionUnitProductionRequest>(to);for(int i=0;i<b.Length;i++){var x=b[i];x.RequestOwner=Map(x.RequestOwner,remap);b[i]=x;} }
        }
        ValidateCopiedPresence(source,copy,remap,sameRunBirths);
        foreach (var bank in sameRunBirths.EconomyBanks)
            Require(MatchesEconomy(copy, remap[bank.Value], sameRunBirths.FinalEconomySnapshots[bank.Key]),
                "Copied source economy bank differs from the same-run final InitialUnits observation");
        foreach (var bladder in sameRunBirths.FuelBladders)
            Require(copy.HasComponent<BuildingResourceStorageComponent>(remap[bladder.Value]) &&
                SameStorage(source.GetComponentData<BuildingResourceStorageComponent>(bladder.Value),
                    copy.GetComponentData<BuildingResourceStorageComponent>(remap[bladder.Value])),
                "Directly observed source bladder storage was not copied exactly");
        using (var copiedBaseCandidates = copy.CreateEntityQuery(typeof(RuntimeBuildingCombatTag),typeof(UnitSourcePrefabKey),typeof(Faction),typeof(UnitHealth)))
        using (var orderedCopiedBaseCandidates = copiedBaseCandidates.ToEntityArray(Allocator.Temp))
        {
            Require(orderedCopiedBaseCandidates.Length == sourceBaseOrder.Length,
                "Copied original rules base-discovery candidate population differs from the source");
            for (int i = 0; i < sourceBaseOrder.Length; i++)
                Require(orderedCopiedBaseCandidates[i] == Map(sourceBaseOrder[i], remap),
                    "Copied original rules base-discovery order differs from the source at candidate " + i);
        }
        SkirmishMatchState fixtureMatch=copy.GetComponentData<SkirmishMatchState>(remap[matches.GetSingletonEntity()]);
        Require(fixtureMatch.SessionId.Equals(sourceMatch.SessionId) && fixtureMatch.Seed==sourceMatch.Seed &&
            fixtureMatch.ScenarioIndex==sourceMatch.ScenarioIndex && fixtureMatch.Phase==sourceMatch.Phase &&
            fixtureMatch.StartupFailure==sourceMatch.StartupFailure && fixtureMatch.Outcome==sourceMatch.Outcome &&
            fixtureMatch.Reason==sourceMatch.Reason && fixtureMatch.ElapsedSeconds==sourceMatch.ElapsedSeconds &&
            fixtureMatch.PlayerUnitsLost==sourceMatch.PlayerUnitsLost && fixtureMatch.EnemyUnitsLost==sourceMatch.EnemyUnitsLost &&
            fixtureMatch.PlayerBuildingsLost==sourceMatch.PlayerBuildingsLost && fixtureMatch.EnemyBuildingsLost==sourceMatch.EnemyBuildingsLost &&
            fixtureMatch.ResultSaved==sourceMatch.ResultSaved && fixtureMatch.SurrenderRequested==sourceMatch.SurrenderRequested &&
            fixtureMatch.PlayerMainBase==Map(sourceMatch.PlayerMainBase,remap) && fixtureMatch.EnemyMainBase==Map(sourceMatch.EnemyMainBase,remap),
            "Copied SkirmishMatchState scalar/session flags or remapped base references differ");
        // Source activation must remain untouched; only the disposable copy is activated.
        using var copiedGameplay = copy.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
        Require(copiedGameplay.CalculateEntityCount()==1,"Copied runtime gameplay singleton missing or ambiguous");
        var active= copiedGameplay.GetSingleton<RuntimeGameplayStateComponent>(); active.PlayRequested=1;active.SimulationActive=1;
        copy.SetComponentData(copiedGameplay.GetSingletonEntity(),active);
        bool runOwners=string.Equals(Environment.GetEnvironmentVariable(OwnerOptIn),"1",StringComparison.Ordinal);
        bool runRulesOwner=runOwners && string.Equals(Environment.GetEnvironmentVariable(RulesOptIn),"1",StringComparison.Ordinal);
        bool runTargetPolicyDiagnostic=runOwners && string.Equals(Environment.GetEnvironmentVariable(TargetPolicyDiagnosticOptIn),"1",StringComparison.Ordinal);
        bool runMovementOwnerDiagnostic=runOwners && string.Equals(Environment.GetEnvironmentVariable(MovementOwnerDiagnosticOptIn),"1",StringComparison.Ordinal);
        Require(!runTargetPolicyDiagnostic || runRulesOwner,
            "S073 target-policy diagnostic requires the opt-in original Rules owner on the disposable copy");
        Require(!runMovementOwnerDiagnostic || (runTargetPolicyDiagnostic && runRulesOwner),
            "S073 movement-owner diagnostic requires the original Rules owner and the complete 122-tick target-policy diagnostic");
        RuntimeGameplayStateComponent sourceGameplayAfter=gameplay.GetSingleton<RuntimeGameplayStateComponent>();
        Require(sourceGameplayBefore.Equals(sourceGameplayAfter),"Source runtime gameplay state changed during copy");
        bool sourcePresetWasBound=SourceS073LegacyCombatPresetProvider.TryGetBound(source,out var sourcePresetBefore);
        Console.WriteLine("[S073DisposableCopyStage] before_source_production_owner");
        JObject productionOwnerExecution=RunSourceProductionOwnerOnCopy(source,copy,remap,sameRunBirths,
            disposableEntities,combatPresetPath,sourceLegacyConfig,reinforcementInfantryTarget,attackSquadSize,
            sourceBuildings,sourceConfiguredUnits,repositoryRoot,historicalInputDiagnostic,sourceCameraCapability,productionPrefabPath,productionPrefabSha256);
        Console.WriteLine("[S073DisposableCopyStage] after_source_production_owner");
        JObject productionRulesOwner = OptionalObject(productionOwnerExecution, "pre_production_rules_owner", "production_owner_execution");
        bool productionRulesOwnerInvoked=(bool?)productionRulesOwner?["owner_invoked"]==true;
        JObject ownerExecution=null;
        bool productionRequested=string.Equals(Environment.GetEnvironmentVariable(ProductionOwnerOptIn),"1",StringComparison.Ordinal);
        if(runOwners && !productionRulesOwnerInvoked && productionRequested)
            ownerExecution=new JObject { ["status"]="Skipped_ProductionOwnerNotAdmitted", ["bounded_ticks"]=0,
                ["reason"]="Production preflight did not admit the original Rules owner; no fallback owner ticks executed",
                ["mission_ready"]=false };
        else if(runOwners&&!productionRulesOwnerInvoked)
        {
            Console.WriteLine("[S073DisposableCopyStage] before_original_ai_owners");
            try { ownerExecution=RunOriginalOwnersOnCopy(copy,remap[matches.GetSingletonEntity()],disposableEntities,
                combatPresetPath,remap,runRulesOwner,runMovementOwnerDiagnostic,sourceLegacyConfig,reinforcementInfantryTarget,attackSquadSize); }
            catch(Exception ex)
            {
                JObject failureGuardAfter=null; bool? failureSourceMutated=null;
                try
                {
                    failureGuardAfter=CaptureSourceGuard(source,sourceSessionEntity,gameplay.GetSingletonEntity(),sameRunBirths);
                    failureSourceMutated=!JToken.DeepEquals(sourceGuardBefore,failureGuardAfter);
                }
                catch { /* retain unknown rather than infer from a pre-failure snapshot */ }
                Directory.CreateDirectory(outputDirectory);
                File.WriteAllText(Path.Combine(outputDirectory,"s073-tactical-ai-disposable-copy.json"),new JObject {
                    ["kind"]="S073TacticalAIDisposableCopyFixture",["schema_version"]=1,["status"]="OwnerExecutionFailed",
                    ["source_world_mutated"]=failureSourceMutated,["source_mutation_verification"]=
                        failureSourceMutated.HasValue ? (failureSourceMutated.Value ? "SourceGuardChanged" : "SourceGuardUnchanged") : "Unknown_NoPostFailureSnapshot",
                    ["source_guard_before"]=sourceGuardBefore,["source_guard_after_failure"]=failureGuardAfter,["failure"]=ex.ToString(),
                    ["owner_opt_in"]=true,["mission_admission_claimed"]=false,["physical_validation_claimed"]=false,["mission_ready"]=false
                }.ToString(Formatting.Indented)+"\n");
                throw;
            }
            Console.WriteLine("[S073DisposableCopyStage] after_original_ai_owners");
        }
        Console.WriteLine("[S073DisposableCopyStage] before_final_source_guard");
        JObject sourceGuardAfter=CaptureSourceGuard(source,matches.GetSingletonEntity(),gameplay.GetSingletonEntity(),sameRunBirths);
        Console.WriteLine("[S073DisposableCopyStage] after_final_source_guard");
        Require(JToken.DeepEquals(sourceGuardBefore,sourceGuardAfter),"Source AI owner inputs or session state changed during disposable owner run");
        bool sourcePresetStillBound=SourceS073LegacyCombatPresetProvider.TryGetBound(source,out var sourcePresetAfter);
        Require(sourcePresetStillBound==sourcePresetWasBound&&(!sourcePresetWasBound||ReferenceEquals(sourcePresetBefore,sourcePresetAfter)),
            "Disposable owner run changed source combat preset binding");

        Directory.CreateDirectory(outputDirectory);
        Console.WriteLine("[S073DisposableCopyStage] before_result_shape_readback");
        JObject originalRulesOwner = OptionalObject(ownerExecution, "original_rules_owner", "owner_execution");
        JObject movementOwnerDiagnostic = OptionalObject(ownerExecution, "movement_owner_diagnostic", "owner_execution");
        productionRulesOwner = OptionalObject(productionOwnerExecution, "pre_production_rules_owner", "production_owner_execution");
        bool rulesPreflightOnly=(string)ownerExecution?["status"]=="FailClosed_RulesOwnerPreflightOnly" ||
            (string)productionRulesOwner?["status"]=="FailClosed_RulesOwnerPreflightOnly";
        bool ownerGateAuditPassed=(bool?)ownerExecution?["owner_required_query_gate_audit_passed"]==true;
        bool movementObserved=(string)movementOwnerDiagnostic?["status"]=="MovementObserved";
        bool productionOwnerInvoked=(bool?)productionOwnerExecution?["owner_invoked"]==true;
        var result = new JObject { ["kind"]="S073TacticalAIDisposableCopyFixture",["schema_version"]=1,
            ["status"]=rulesPreflightOnly?"RulesOwnerPreflightOnly_FailClosed":
                productionOwnerInvoked?"SourceProductionOwnerExperimentCompleted":
                !runOwners?"CopiedSourceClosure_Audited_NoOwnersRequested":
                !ownerGateAuditPassed?"CopiedSourceOwnerWindowCompleted_WithRequiredQueryGatesClosed":
                runMovementOwnerDiagnostic&&!movementObserved?"CopiedSourceOwnerWindowCompleted_NoMovementObserved":"CopiedBoundedSourceOwnerClosure_Audited",
            ["source_entity_count"]=originals.Count,
            ["source_combat_population"]=units.CalculateEntityCount(),["source_grid_cells"]=(int)cellsLong,
            ["entity_references_remapped"]=true,["copied_component_presence_validated"]=true,["mutable_buffers_deep_copied"]=true,
            ["original_rules_base_candidate_order_preserved"] = true,["original_rules_base_candidate_population"] = sourceBaseOrder.Length,
            ["source_match_phase_preserved"]=(byte)sourceMatch.Phase,["fixture_match_phase"]=(byte)copy.GetComponentData<SkirmishMatchState>(remap[matches.GetSingletonEntity()]).Phase,
            ["fixture_only_transition"] = rulesPreflightOnly ?
                "Copied RuntimeGameplayStateComponent activated; original Rules owner preflighted and failed closed before mutation" :
                ((bool?)originalRulesOwner?["owner_invoked"] == true || productionRulesOwnerInvoked) ?
                    "Copied RuntimeGameplayStateComponent activated; original Rules owner invoked against source-owned disposable inputs" :
                    "Copied RuntimeGameplayStateComponent activated; bounded AI-owner experiment path",
            ["same_run_birth_input_audit"] = sameRunBirths.Audit,
            ["source_closure_audit"]=closureAudit,
            ["source_guard_before"]=sourceGuardBefore,["source_guard_after"]=sourceGuardAfter,
            ["source_preset_binding_unchanged"]=true,["combat_preset_source_sha256"]=SourceS073LegacyCombatPresetProvider.SourceAssetSha256,
            ["production_owner_execution"]=productionOwnerExecution,
            ["source_production_owner_invoked"]=productionOwnerInvoked,
            ["production_completion_claimed"]=false,
            ["owner_execution"]=ownerExecution,["ai_owner_invoked"]=runOwners && (int?)ownerExecution?["bounded_ticks"]>0,
            ["original_rules_owner_invoked"]=(bool?)originalRulesOwner?["owner_invoked"]==true || productionRulesOwnerInvoked,
            ["tactical_ai_owner_required_query_gate_audit_passed"]=runOwners?(bool?)ownerGateAuditPassed:null,
            ["source_derived_movement_observed"]=runMovementOwnerDiagnostic?(bool?)movementObserved:null,
            ["movement_owner_phase_scope"]=runMovementOwnerDiagnostic?
                "Isolated post-122-tick disposable-copy movement phase; Rules and AI owners do not continue during this phase":"NotRequested",
            ["movement_phase_scheduler_parity_claimed"]=false,
            ["campaign_runtime_garrisons_or_resources_imported"]=false,["source_world_mutated"]=false,
            ["mission_admission_claimed"]=false,["physical_validation_claimed"]=false,
            ["mission_ready"]=false };
        File.WriteAllText(Path.Combine(outputDirectory,"s073-tactical-ai-disposable-copy.json"),result.ToString(Formatting.Indented)+"\n");
        return result;
        }
        finally { DisposeCopy(copy, disposableEntities); }
    }

    static Entity Map(Entity e, Dictionary<Entity,Entity> remap)
    { if (e == Entity.Null) return e; if (!remap.TryGetValue(e,out var mapped)) throw new InvalidDataException("Unclosed tactical AI entity reference: "+e); return mapped; }
    static void AddQueryEntities(EntityManager em, EntityQuery q, HashSet<Entity> into)
    { using var a=q.ToEntityArray(Allocator.Temp); foreach(var e in a) into.Add(e); }
    static void AddSessionBaseReference(EntityManager em,Entity e,HashSet<Entity> into,string label)
    {if(e==Entity.Null)return;Require(em.Exists(e),"SkirmishMatchState "+label+" reference does not exist in source");into.Add(e);}
    static void AddSingletonEntities<T>(EntityManager em, HashSet<Entity> into) where T:struct
    { using var q=em.CreateEntityQuery(typeof(T)); Require(q.CalculateEntityCount()==1,"Required singleton count for "+typeof(T).Name); into.Add(q.GetSingletonEntity()); }

    static JObject AuditSourceClosure(EntityManager em,HashSet<Entity> closure,Entity grid,int unitCount,int factionGridCount,
        int planCount,int buildPlanCount,int productionPlanCount,int priorityCount,int squadCount,int matchCount,int controlCount,
        int campaignRoleCount,HashSet<Entity> sourceEconomyBanks,HashSet<Entity> sourceFuelBladders)
    {
        // Match the retained S073 census domain. Any changed population must be
        // recaptured and re-reviewed before this copier accepts it.
        Require(unitCount==4965 && factionGridCount==4965 && planCount==2 && buildPlanCount==2 && productionPlanCount==2 &&
            priorityCount==2 && squadCount==0 &&
            matchCount==1 && controlCount==1 && campaignRoleCount==0,
            "Source owner populations differ from retained census: units="+unitCount+", factionGrid="+factionGridCount+
            ", plans="+planCount+", buildPlans="+buildPlanCount+", productionPlans="+productionPlanCount+
            ", priorities="+priorityCount+", squads="+squadCount+", sessions="+matchCount+
            ", controls="+controlCount+", campaignRoles="+campaignRoleCount);
        using var members=em.CreateEntityQuery(typeof(AISquadMember));
        using var aiControlled=em.CreateEntityQuery(typeof(AIControlledTag));
        using var diagQueue=em.CreateEntityQuery(typeof(AIDiagnosticLogQueueComponent));
        using var logBuffers=em.CreateEntityQuery(typeof(AIDiagnosticLogComponent));
        using var factionEntries=em.CreateEntityQuery(typeof(FactionControlEntry));
        using var walkable=em.CreateEntityQuery(typeof(GridWalkable));
        Require(members.CalculateEntityCount()==0 && aiControlled.CalculateEntityCount()==0 && diagQueue.CalculateEntityCount()==0 &&
            logBuffers.CalculateEntityCount()==0 && factionEntries.CalculateEntityCount()==1 && walkable.CalculateEntityCount()==1,
            "Source AI support populations differ from retained owner census");
        Require(em.GetBuffer<FactionControlEntry>(factionEntries.GetSingletonEntity()).Length==2 &&
            em.GetBuffer<GridWalkable>(grid).Length==2_097_152,"Source support buffer lengths differ from retained owner census");

        Type[] copiedTypes = {
            typeof(Faction),typeof(UnitGrid),typeof(UnitHealth),typeof(UnitCombat),typeof(UnitAttack),typeof(LocalTransform),
            typeof(UnitMove),typeof(UnitFootprint),typeof(UnitMovementBehavior),typeof(UnitVehicleKinematics),typeof(UnitVehicleMovement),
            typeof(MapSurfaceComponent),
            typeof(AIControlledTag),typeof(CampaignMissionUnitRoleComponent),typeof(RuntimeBuildingCombatTag),typeof(RuntimeBuildingCombatInfo),
            typeof(StaticGridBlocker),typeof(GridBlockerSize),typeof(UnitResourceHauler),typeof(FuelLogisticsOilSourceTag),
            typeof(FuelLogisticsRefineryInputTag),typeof(FuelLogisticsRefineryOutputTag),typeof(FuelLogisticsFuelStorageTag),
            typeof(UnitAirMovement),typeof(AISquadPlan),typeof(AITargetPrioritySetting),typeof(RuntimeGameplayStateComponent),
            typeof(FactionControlConfigTag),typeof(RuntimeDiagnosticsStateComponent),typeof(AIDiagnosticLogQueueComponent),
            typeof(AISquad),typeof(AISquadMember),typeof(SkirmishBaseDefender),typeof(SkirmishMatchState),typeof(SkirmishMainBase),typeof(EngageTarget),
            typeof(BaseBreachOrder),typeof(UnitPathRequest),typeof(UnitTarget),typeof(UnitPathFollow),typeof(UnitPathRange),typeof(SkirmishTrackedUnit),
            typeof(UnitPathRetryCooldown),typeof(ManualControlledTag),
            typeof(AICombatOrderTag),typeof(ManualMoveOrderTag),typeof(ManualMoveGroupMemberTag),typeof(AutoWanderMoveTag),
            typeof(AIBuildPlan),typeof(AIProductionPlan),typeof(AIProductionPlanEntry),typeof(FactionEconomy),typeof(FactionEconomyPolicy),
            typeof(FactionTacticalMaterialsComponent),typeof(BuildingResourceStorageComponent),
            typeof(BuildingRuntimeStateTag),typeof(BuildingConfiguredUnitReadModel),typeof(BuildingRuntimeUnitProductionSummary),
            typeof(BuildingRuntimeFactionSummary),typeof(BuildingRuntimeFactionUsableFuelSummary),
            typeof(BuildingFactionUnitProductionRequest),typeof(BuildingProductionSlotReadModel),
            typeof(BuildingFactionProductionSpawnPointReadModel),
            typeof(CustomGameStartupStateComponent),typeof(InitialUnitsSpawnConfig),typeof(InitialUnitsFactionSpawnEntry),
            typeof(GridConfig),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent),
            typeof(AISquadUnit),typeof(FactionControlEntry),typeof(AIDiagnosticLogComponent),typeof(GridWalkable)
        };
        var discovered=new HashSet<string>(StringComparer.Ordinal);
        foreach(Type t in copiedTypes)FindEntityFields(t,t.Name,new HashSet<Type>(),discovered);
        var expected=new HashSet<string>(StringComparer.Ordinal) {
            typeof(AISquad).Name+".TargetEntity",typeof(AISquadMember).Name+".Squad",typeof(AISquadUnit).Name+".Unit",
            typeof(SkirmishMatchState).Name+".PlayerMainBase",typeof(SkirmishMatchState).Name+".EnemyMainBase",
            typeof(EngageTarget).Name+".Target",typeof(BaseBreachOrder).Name+".FinalTarget",typeof(BaseBreachOrder).Name+".BreachTarget",
            typeof(SkirmishTrackedUnit).Name+".Entity",
            typeof(BuildingFactionUnitProductionRequest).Name+".RequestOwner",
            typeof(InitialUnitsSpawnConfig).Name+".BlockerPrefab",
            typeof(InitialUnitsSpawnConfig).Name+".UnitSelectionMarkerPrefab",typeof(InitialUnitsSpawnConfig).Name+".UnitHealthBarPrefab"
        };
        Require(discovered.SetEquals(expected),"Entity-reference field schema differs from audited edge inventory: "+string.Join(",",discovered));
        foreach(Entity e in closure)
        {
            if(em.HasComponent<AISquad>(e))RequireClosedReference(em.GetComponentData<AISquad>(e).TargetEntity,closure,"AISquad.TargetEntity");
            if(em.HasComponent<AISquadMember>(e))RequireClosedReference(em.GetComponentData<AISquadMember>(e).Squad,closure,"AISquadMember.Squad");
            if(em.HasBuffer<AISquadUnit>(e))foreach(var row in em.GetBuffer<AISquadUnit>(e))RequireClosedReference(row.Unit,closure,"AISquadUnit.Unit");
            if(em.HasBuffer<SkirmishTrackedUnit>(e))foreach(var row in em.GetBuffer<SkirmishTrackedUnit>(e))RequireClosedReference(row.Entity,closure,"SkirmishTrackedUnit.Entity");
            if(em.HasBuffer<BuildingFactionUnitProductionRequest>(e))foreach(var row in em.GetBuffer<BuildingFactionUnitProductionRequest>(e))
            {RequireClosedReference(row.RequestOwner,closure,"BuildingFactionUnitProductionRequest.RequestOwner");}
            if(em.HasComponent<SkirmishMatchState>(e)){var x=em.GetComponentData<SkirmishMatchState>(e);RequireClosedReference(x.PlayerMainBase,closure,"SkirmishMatchState.PlayerMainBase");RequireClosedReference(x.EnemyMainBase,closure,"SkirmishMatchState.EnemyMainBase");}
            if(em.HasComponent<EngageTarget>(e))RequireClosedReference(em.GetComponentData<EngageTarget>(e).Target,closure,"EngageTarget.Target");
            if(em.HasComponent<BaseBreachOrder>(e)){var x=em.GetComponentData<BaseBreachOrder>(e);RequireClosedReference(x.FinalTarget,closure,"BaseBreachOrder.FinalTarget");RequireClosedReference(x.BreachTarget,closure,"BaseBreachOrder.BreachTarget");}
        }
        var closureEconomy=closure.Where(e=>em.HasComponent<FactionEconomy>(e)).ToArray();
        var closureStorage=closure.Where(e=>em.HasComponent<BuildingResourceStorageComponent>(e)).ToArray();
        Require(sourceEconomyBanks.Count==2 && closureEconomy.Length==sourceEconomyBanks.Count &&
            closureEconomy.All(sourceEconomyBanks.Contains) &&
            sourceFuelBladders.Count==2 && sourceFuelBladders.All(e=>closure.Contains(e) &&
                em.HasComponent<BuildingResourceStorageComponent>(e)),
            "Closure must contain exactly the admitted source economy banks and both direct bladder entities");
        Require(!copiedTypes.Contains(typeof(BuildingResourceProductionProfileComponent)),
            "Campaign production profile state accidentally entered copied type set");
        return new JObject { ["status"]="Passed_SourceClosureAndTypedEdgeAudit",["retained_census_population_match"]=true,
            ["copied_type_count"]=copiedTypes.Length,["discovered_entity_reference_fields"]=new JArray(discovered.OrderBy(x=>x)),
            ["expected_entity_reference_fields"]=new JArray(expected.OrderBy(x=>x)),["owner_entity_edges_resolve_in_copy_closure"]=true,
            ["startup_visual_prefab_references_stripped_from_copy"]=true,
            ["campaign_role_population"]=campaignRoleCount,["campaign_owned_resource_state_imported"]=false,
            ["source_closure_storage_component_count_not_imported_by_default"]=closureStorage.Length,
            ["source_owned_mission_economy_and_bladder_storage_only"]=true };
    }

    static void FindEntityFields(Type type,string path,HashSet<Type> stack,HashSet<string> found)
    {
        if(type==typeof(Entity)){found.Add(path);return;}
        if(type.IsPrimitive||type.IsEnum||type==typeof(string)||type.IsPointer||!stack.Add(type))return;
        if(type.IsArray){FindEntityFields(type.GetElementType(),path+"[]",stack,found);stack.Remove(type);return;}
        if(type.IsGenericType)foreach(Type arg in type.GetGenericArguments())FindEntityFields(arg,path+"<"+arg.Name+">",stack,found);
        if(type.IsValueType)foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
            FindEntityFields(field.FieldType,path+"."+field.Name,stack,found);
        stack.Remove(type);
    }

    static void RequireClosedReference(Entity e,HashSet<Entity> closure,string edge)
    {if(e!=Entity.Null&&!closure.Contains(e))throw new InvalidDataException("Unclosed source entity reference at "+edge+": "+e);}

    static void AssertExternalReferenceRejected(Dictionary<Entity,Entity> remap)
    {
        var outside=new Entity{Index=int.MaxValue,Version=1};
        try{_ = Map(outside,remap);}catch(InvalidDataException){return;}
        throw new InvalidDataException("Negative control failed: external Entity reference was not rejected");
    }

    static JObject CaptureSourceGuard(EntityManager em,Entity sessionEntity,Entity gameplayEntity,SameRunBirthInputs sameRunBirths)
    {
        var match=em.GetComponentData<SkirmishMatchState>(sessionEntity);
        var gameplay=em.GetComponentData<RuntimeGameplayStateComponent>(gameplayEntity);
        var controls=new JArray();
        using(var q=em.CreateEntityQuery(typeof(FactionControlEntry)))
        using(var es=q.ToEntityArray(Allocator.Temp))
            foreach(var e in es)foreach(var row in em.GetBuffer<FactionControlEntry>(e))
                controls.Add(new JObject{["faction"]=row.FactionId,["ai"]=row.AIControlled,["player"]=row.IsPlayerFaction,["last_log_time"]=row.LastLogTime});
        var banks=new JArray();
        foreach(var pair in sameRunBirths.EconomyBanks.OrderBy(x=>x.Key))
        {
            var economy=em.GetComponentData<FactionEconomy>(pair.Value);
            var materials=em.GetComponentData<FactionTacticalMaterialsComponent>(pair.Value);
            var policy=em.GetComponentData<FactionEconomyPolicy>(pair.Value);
            banks.Add(new JObject { ["faction"] = pair.Key, ["entity"] = $"{pair.Value.Index}:{pair.Value.Version}",
                ["money"] = economy.Money, ["materials_only"] = economy.MaterialsOnlyConstruction,
                ["oil"] = economy.Oil, ["fuel"] = economy.Fuel, ["last_sell"] = economy.LastSellTime,
                ["last_log"] = economy.LastLogTime, ["materials_current"] = materials.Current,
                ["materials_capacity"] = materials.Capacity, ["materials_version"] = materials.Version,
                ["policy_enabled"] = policy.Enabled, ["policy_sell_interval"] = policy.SellIntervalSeconds });
        }
        var bladders=new JArray();
        foreach(var pair in sameRunBirths.FuelBladders.OrderBy(x=>x.Key))
        {
            var storage=em.GetComponentData<BuildingResourceStorageComponent>(pair.Value);
            var combat=em.GetComponentData<RuntimeBuildingCombatInfo>(pair.Value);
            bladders.Add(new JObject { ["faction"] = pair.Key, ["entity"] = $"{pair.Value.Index}:{pair.Value.Version}",
                ["runtime_building_id"] = combat.RuntimeBuildingId, ["owner"] = storage.OwnerFactionId,
                ["fuel_capacity"] = storage.FuelStorageCapacity, ["stored_fuel"] = storage.StoredFuelBarrels,
                ["reserved_inbound"] = storage.ReservedFuelInboundBarrels, ["reserved_outbound"] = storage.ReservedFuelOutboundBarrels,
                ["version"] = storage.Version });
        }
        return new JObject {
            ["world_sequence_number"] = em.WorldUnmanaged.SequenceNumber,
            ["session_id"] = match.SessionId.ToString(),
            ["phase"]=(byte)match.Phase,["seed"]=match.Seed,["outcome"]=(byte)match.Outcome,["reason"]=(byte)match.Reason,
            ["player_base_index"]=match.PlayerMainBase.Index,["player_base_version"]=match.PlayerMainBase.Version,
            ["enemy_base_index"]=match.EnemyMainBase.Index,["enemy_base_version"]=match.EnemyMainBase.Version,
            ["elapsed_seconds"]=match.ElapsedSeconds,["result_saved"]=match.ResultSaved,["surrender_requested"]=match.SurrenderRequested,
            ["gameplay_play_requested"]=gameplay.PlayRequested,["gameplay_simulation_active"]=gameplay.SimulationActive,
            ["faction_control_entries"]=controls,["ai_controlled_tags"]=Count<AIControlledTag>(em),["squads"]=Count<AISquad>(em),
            ["squad_members"]=Count<AISquadMember>(em),["engage_targets"]=Count<EngageTarget>(em),
            ["base_breach_orders"]=Count<BaseBreachOrder>(em),["path_requests"]=Count<UnitPathRequest>(em),
            ["source_economy_banks"]=banks,["source_direct_fuel_bladders"]=bladders
        };
    }

    static int Count<T>(EntityManager em) where T:struct
    {using var q=em.CreateEntityQuery(typeof(T));return q.CalculateEntityCount();}

    static JObject CaptureSquadEligibility(EntityManager em,Dictionary<Entity,Entity> sourceToCopy,string boundary)
    {
        using var tagged=em.CreateEntityQuery(typeof(Faction),typeof(UnitGrid),typeof(UnitHealth),typeof(AIControlledTag));
        using var eligible=em.CreateEntityQuery(new EntityQueryDesc{
            All=new[]{ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<UnitGrid>(),ComponentType.ReadOnly<UnitHealth>(),ComponentType.ReadOnly<AIControlledTag>()},
            None=new[]{ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>(),ComponentType.ReadOnly<SkirmishBaseDefender>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),ComponentType.ReadOnly<UnitResourceHauler>()}});
        using var factionUnits=em.CreateEntityQuery(typeof(Faction),typeof(UnitGrid),typeof(UnitHealth));
        using var plans=em.CreateEntityQuery(typeof(AISquadPlan));
        using var controlQuery=em.CreateEntityQuery(typeof(FactionControlConfigTag),typeof(FactionControlEntry));
        var reverse=sourceToCopy.ToDictionary(x=>x.Value,x=>x.Key);
        var planRows=new JArray();var plansByFaction=new Dictionary<byte,AISquadPlan>();
        using(var entities=plans.ToEntityArray(Allocator.Temp))
        foreach(var entity in entities)
        {
            var plan=em.GetComponentData<AISquadPlan>(entity);plansByFaction[plan.FactionId]=plan;
            planRows.Add(new JObject{["faction"]=plan.FactionId,["enabled"]=plan.Enabled,["min_units"]=plan.MinUnits,
                ["max_units"]=plan.MaxUnits,["max_active_squads"]=plan.MaxActiveSquads,["next_squad_id"]=plan.NextSquadId,
                ["source_entity"]=reverse.TryGetValue(entity,out var sourcePlan)?$"{sourcePlan.Index}:{sourcePlan.Version}":null});
        }
        var controls=new JArray();
        if(controlQuery.CalculateEntityCount()==1)
            foreach(var entry in em.GetBuffer<FactionControlEntry>(controlQuery.GetSingletonEntity()))
                controls.Add(new JObject{["faction"]=entry.FactionId,["ai_controlled"]=entry.AIControlled!=0});
        var rows=new JArray();var byFaction=new Dictionary<byte,int>();var candidateByFaction=new Dictionary<byte,int>();
        var role=new Dictionary<string,int>(StringComparer.Ordinal);
        using(var entities=tagged.ToEntityArray(Allocator.Temp))
        foreach(var entity in entities)
        {
            byte faction=em.GetComponentData<Faction>(entity).Id;
            byFaction[faction]=byFaction.TryGetValue(faction,out int current)?current+1:1;
            var exclusions=new JArray();
            if(em.HasComponent<CampaignMissionUnitRoleComponent>(entity))exclusions.Add("CampaignMissionUnitRoleComponent");
            if(em.HasComponent<SkirmishBaseDefender>(entity))exclusions.Add("SkirmishBaseDefender");
            if(em.HasComponent<RuntimeBuildingCombatTag>(entity))exclusions.Add("RuntimeBuildingCombatTag");
            if(em.HasComponent<UnitResourceHauler>(entity))exclusions.Add("UnitResourceHauler");
            if(exclusions.Count==0)
            {
                if(em.HasComponent<AISquadMember>(entity))exclusions.Add("AISquadMember");
                if(em.HasComponent<StaticGridBlocker>(entity))exclusions.Add("StaticGridBlocker");
                if(em.HasComponent<EngageTarget>(entity))exclusions.Add("EngageTarget");
                if(em.HasComponent<UnitPathRequest>(entity))exclusions.Add("UnitPathRequest");
                if(em.GetComponentData<UnitHealth>(entity).Current<=0)exclusions.Add("UnitHealth.Current<=0");
            }
            if(exclusions.Count==0)candidateByFaction[faction]=candidateByFaction.TryGetValue(faction,out int n)?n+1:1;
            string key=em.HasComponent<UnitSourcePrefabKey>(entity)
                ?em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString():null;
            string group=exclusions.Count==0?"Candidate":string.Join("+",exclusions.OfType<JValue>().Select(x=>(string)x));
            role[group]=role.TryGetValue(group,out int rc)?rc+1:1;
            if(rows.Count<64)
                rows.Add(new JObject{["source_entity"]=reverse.TryGetValue(entity,out var sourceUnit)?$"{sourceUnit.Index}:{sourceUnit.Version}":null,
                    ["fixture_entity"]=$"{entity.Index}:{entity.Version}",["faction"]=faction,["source_prefab_key"]=key,
                    ["health"]=em.GetComponentData<UnitHealth>(entity).Current,["campaign_role"]=em.HasComponent<CampaignMissionUnitRoleComponent>(entity),
                    ["base_defender"]=em.HasComponent<SkirmishBaseDefender>(entity),["skirmish_squad_member"]=em.HasComponent<SkirmishSquadMember>(entity),
                    ["squad_member"]=em.HasComponent<AISquadMember>(entity),["static_blocker"]=em.HasComponent<StaticGridBlocker>(entity),
                    ["runtime_building"]=em.HasComponent<RuntimeBuildingCombatTag>(entity),["resource_hauler"]=em.HasComponent<UnitResourceHauler>(entity),
                    ["engage_target"]=em.HasComponent<EngageTarget>(entity),["path_request"]=em.HasComponent<UnitPathRequest>(entity),
                    ["eligible_exclusions"]=exclusions});
        }
        var planSummary=new JArray();
        foreach(var pair in plansByFaction.OrderBy(x=>x.Key))
        {
            int taggedCount=byFaction.TryGetValue(pair.Key,out var t)?t:0;
            int eligibleCount=candidateByFaction.TryGetValue(pair.Key,out var c)?c:0;
            planSummary.Add(new JObject{["faction"]=pair.Key,["enabled"]=pair.Value.Enabled,
                ["ai_controlled_by_runtime_control"]=controls.OfType<JObject>().Any(x=>(byte?)x["faction"]==pair.Key&&(bool?)x["ai_controlled"]==true),
                ["tagged_units"]=taggedCount,["final_available_candidates"]=eligibleCount,
                ["required_min_units"]=math.clamp(math.max(1,pair.Value.MinUnits),1,math.max(1,pair.Value.MaxUnits)),
                ["max_units"]=pair.Value.MaxUnits,["max_active_squads"]=pair.Value.MaxActiveSquads,
                ["can_form_squad_from_current_candidates"]=pair.Value.Enabled!=0&&eligibleCount>=math.clamp(math.max(1,pair.Value.MinUnits),1,math.max(1,pair.Value.MaxUnits))});
        }
        var factionUnitRows=new JArray();var factionUnitCounts=new Dictionary<byte,int>();var prefabCounts=new Dictionary<string,int>(StringComparer.Ordinal);
        var specialCounts=new Dictionary<string,int>(StringComparer.Ordinal);
        using(var entities=factionUnits.ToEntityArray(Allocator.Temp))
        foreach(var entity in entities)
        {
            byte faction=em.GetComponentData<Faction>(entity).Id;
            if(faction is not (1 or 2)||em.HasComponent<RuntimeBuildingCombatTag>(entity))continue;
            factionUnitCounts[faction]=factionUnitCounts.TryGetValue(faction,out int n)?n+1:1;
            string key=em.HasComponent<UnitSourcePrefabKey>(entity)?em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString():"<missing>";
            string bucket=faction+":"+key;prefabCounts[bucket]=prefabCounts.TryGetValue(bucket,out int c)?c+1:1;
            foreach(var flag in new[]{("CampaignMissionUnitRoleComponent",em.HasComponent<CampaignMissionUnitRoleComponent>(entity)),
                ("SkirmishBaseDefender",em.HasComponent<SkirmishBaseDefender>(entity)),
                ("SkirmishSquadMember",em.HasComponent<SkirmishSquadMember>(entity)),
                ("AISquadMember",em.HasComponent<AISquadMember>(entity)),("StaticGridBlocker",em.HasComponent<StaticGridBlocker>(entity)),
                ("UnitResourceHauler",em.HasComponent<UnitResourceHauler>(entity)),("UnitMove",em.HasComponent<UnitMove>(entity))})
                if(flag.Item2)specialCounts[faction+":"+flag.Item1]=specialCounts.TryGetValue(faction+":"+flag.Item1,out int fc)?fc+1:1;
            if(factionUnitRows.Count<64)
                factionUnitRows.Add(new JObject{["source_entity"]=reverse.TryGetValue(entity,out var sourceEntity)?$"{sourceEntity.Index}:{sourceEntity.Version}":null,
                    ["faction"]=faction,["source_prefab_key"]=key,["campaign_role"]=em.HasComponent<CampaignMissionUnitRoleComponent>(entity),
                    ["base_defender"]=em.HasComponent<SkirmishBaseDefender>(entity),["skirmish_squad_member"]=em.HasComponent<SkirmishSquadMember>(entity),
                    ["squad_member"]=em.HasComponent<AISquadMember>(entity),["static_blocker"]=em.HasComponent<StaticGridBlocker>(entity),
                    ["hauler"]=em.HasComponent<UnitResourceHauler>(entity),["has_unit_move"]=em.HasComponent<UnitMove>(entity)});
        }
        return new JObject{["boundary"]=boundary,["tagged_ai_unit_population"]=tagged.CalculateEntityCount(),
            ["tagged_ai_unit_population_by_faction"]=JObject.FromObject(byFaction),["exact_aisquad_unit_query_population"]=eligible.CalculateEntityCount(),
            ["final_candidates_by_faction_after_original_exclusions"]=JObject.FromObject(candidateByFaction),
            ["candidate_exclusion_groups"]=JObject.FromObject(role),["plans"]=planRows,["plan_feasibility"]=planSummary,
            ["runtime_faction_controls"]=controls,["source_backed_ai_unit_rows"]=rows,
            ["source_faction_unit_population_by_faction"]=JObject.FromObject(factionUnitCounts),
            ["source_faction_unit_population_by_prefab_key"]=JObject.FromObject(prefabCounts),
            ["source_faction_unit_component_counts"]=JObject.FromObject(specialCounts),["source_faction_unit_rows"]=factionUnitRows,
            ["source_based_exclusion_policy"]="AISquadSystem unit query excludes campaign roles, SkirmishBaseDefender, runtime buildings, and haulers; candidate builder also excludes assigned members, static blockers, engaged/path-request units, and dead units. SkirmishSquadMember is a player quick-select slot and is not an AISquadSystem exclusion."};
    }

    static JObject RunOriginalOwnersOnCopy(EntityManager em,Entity session,HashSet<Entity> cleanup,
        string combatPresetPath,Dictionary<Entity,Entity> sourceToCopy,bool runRulesOwner,bool runMovementOwnerDiagnostic,
        LegacyInitialSpawnData sourceLegacyConfig,int reinforcementInfantryTarget,int attackSquadSize,
        bool preparingRulesOnly=false)
    {
        Require(!preparingRulesOnly||runRulesOwner,"Preparing-only Rules invocation requires the opt-in original Rules owner");
        Require(Count<AIControlledTag>(em)==0 && Count<AISquad>(em)==0 && Count<EngageTarget>(em)==0 && Count<BaseBreachOrder>(em)==0,
            "Owner execution requires the observed source state with no injected AI tags, squads, or targets");
        var before=CensusOwnerOutputs(em);
        var match=em.GetComponentData<SkirmishMatchState>(session);
        Require(match.Phase==SkirmishPhase.Preparing,"Disposable copy must start from the observed Preparing session");
        JObject squadEligibilityBeforeRules=runRulesOwner?CaptureSquadEligibility(em,sourceToCopy,"BeforeOriginalRulesOwner"):null;
        bool targetPolicyDiagnostic=string.Equals(Environment.GetEnvironmentVariable(TargetPolicyDiagnosticOptIn),"1",StringComparison.Ordinal);
        int ownerTickLimit=targetPolicyDiagnostic?TargetPolicyDiagnosticTicks:MaxOwnerTicks;
        JObject movementOwnerPreflightBefore=runMovementOwnerDiagnostic?CaptureMovementOwnerPreflight(em,"Before122TickOwnerWindow"):null;
        JObject rulesOwnerObservation=null;
        if (runRulesOwner)
        {
            // Run the real Preparing branch only when its bounded source-owned
            // inputs have been admitted. Missing inputs remain a non-mutating gate.
            using var buildings = em.CreateEntityQuery(typeof(RuntimeBuildingCombatTag),typeof(UnitSourcePrefabKey),typeof(Faction),typeof(UnitHealth));
            using var economies = em.CreateEntityQuery(typeof(FactionEconomy));
            using var storage = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
            using var tracked = em.CreateEntityQuery(typeof(SkirmishTrackedUnit));
            using var buildPlans = em.CreateEntityQuery(typeof(AIBuildPlan));
            using var productionPlans = em.CreateEntityQuery(typeof(AIProductionPlan));
            using var startupPlans = em.CreateEntityQuery(typeof(CustomGameStartupStateComponent),typeof(InitialUnitsSpawnConfig));
            var missing = new JArray();
            if (buildings.IsEmptyIgnoreFilter) missing.Add("source RuntimeBuildingCombatTag + UnitSourcePrefabKey + Faction + UnitHealth building population");
            if (economies.CalculateEntityCount()!=2) missing.Add("exactly two copied same-run source FactionEconomy banks");
            if (storage.CalculateEntityCount()!=2) missing.Add("exactly two copied same-run directly observed source bladder storage entities");
            if (buildPlans.CalculateEntityCount()!=2 || productionPlans.CalculateEntityCount()!=2)
                missing.Add("two source AIBuildPlan and AIProductionPlan entities for original supply seeding");
            if (startupPlans.CalculateEntityCount()!=1) missing.Add("one copied source startup plan for the original preset projection");
            if (!em.HasBuffer<SkirmishTrackedUnit>(session)) missing.Add("copied SkirmishTrackedUnit session buffer");
            var candidates = new JArray();
            using (var q = em.CreateEntityQuery(typeof(RuntimeBuildingCombatTag),typeof(UnitSourcePrefabKey),typeof(Faction),typeof(UnitHealth)))
            using (var es = q.ToEntityArray(Allocator.Temp))
                foreach (var e in es)
                {
                    string key = em.GetComponentData<UnitSourcePrefabKey>(e).Value.ToString();
                    if (em.HasComponent<OperationMapBuildingComponent>(e) || !key.ToLowerInvariant().Contains("building_barrack")) continue;
                    candidates.Add(new JObject { ["entity_index"] = e.Index, ["entity_version"] = e.Version,
                        ["faction"] = em.GetComponentData<Faction>(e).Id, ["source_prefab_key"] = key,
                        ["excluded_operation_map_building"] = em.HasComponent<OperationMapBuildingComponent>(e) });
                }
            if (candidates.Count != 2 || !candidates.OfType<JObject>().Any(x=>(int?)x["faction"]==1) ||
                !candidates.OfType<JObject>().Any(x=>(int?)x["faction"]==2))
                missing.Add("one source Barracks per faction in original Rules base discovery query");
            if (missing.Count > 0)
                return new JObject { ["status"] = "FailClosed_RulesOwnerPreflightOnly",
                    ["original_owner"] = "SkirmishRulesSystem.OnUpdate.Preparing",
                    ["owner_invoked"] = false, ["preparing_session_preserved"] = true,
                    ["base_discovery_candidates_in_original_query_order"] = candidates,
                    ["candidate_population"] = candidates.Count,
                    ["copied_tactical_economy_count"] = economies.CalculateEntityCount(),
                    ["copied_building_storage_count"] = storage.CalculateEntityCount(),
                    ["session_tracked_unit_buffer_present"] = em.HasBuffer<SkirmishTrackedUnit>(session),
                    ["blocked_source_dependencies"] = missing,
                    ["campaign_runtime_resources_imported"] = false,
                    ["source_world_or_session_mutated"] = false,
                    ["mission_admission_claimed"] = false, ["physical_validation_claimed"] = false, ["mission_ready"] = false };

            bool hadPriorLegacyPreset = SourceS073LegacyCombatPresetProvider.TryGetBound(em, out var priorLegacyPreset);
            SourceS073LegacyCombatPresetProvider.Bind(em, combatPresetPath);
            try
            {
                SkirmishPresetRuntimeProjection.BindValidatedSourceReadback(em, sourceLegacyConfig,
                    reinforcementInfantryTarget, attackSquadSize, (uint)match.Seed);
                try
                {
                var preparingRulesState = new SystemState { EntityManager = em, Enabled = true };
                var preparingRules = new SkirmishRulesSystem();
                preparingRulesState.Begin(); try { preparingRules.OnCreate(ref preparingRulesState); } finally { preparingRulesState.End(); }
                preparingRulesState.Begin();
                try
                {
                    if (!preparingRulesState.Enabled || !preparingRulesState.RequiredQueriesPresent)
                        throw new InvalidDataException("Original SkirmishRulesSystem required-query gate is closed on disposable source copy");
                    preparingRules.OnUpdate(ref preparingRulesState);
                }
                finally { preparingRulesState.End(); }
                match = em.GetComponentData<SkirmishMatchState>(session);
                Require(match.Phase == SkirmishPhase.Playing && match.Outcome == SkirmishOutcome.None &&
                    match.PlayerMainBase != Entity.Null && match.EnemyMainBase != Entity.Null,
                    "Original Rules owner did not admit the copied source banks and bases into Playing");
                rulesOwnerObservation = new JObject
                {
                    ["original_owner"] = "SkirmishRulesSystem.OnUpdate.Preparing",
                    ["owner_invoked"] = true, ["required_queries_present"] = true,
                    ["copy_phase_before"] = (byte)SkirmishPhase.Preparing, ["copy_phase_after"] = (byte)match.Phase,
                    ["player_base"] = $"{match.PlayerMainBase.Index}:{match.PlayerMainBase.Version}",
                    ["enemy_base"] = $"{match.EnemyMainBase.Index}:{match.EnemyMainBase.Version}",
                    ["copied_economy_banks"] = economies.CalculateEntityCount(),
                    ["copied_direct_bladder_storage"] = storage.CalculateEntityCount(),
                    ["squad_eligibility_before_rules"] = squadEligibilityBeforeRules,
                    ["squad_eligibility_after_rules"] = CaptureSquadEligibility(em,sourceToCopy,"AfterOriginalRulesOwner_PreAIOwners"),
                    ["source_world_mutated"] = false, ["mission_admission_claimed"] = false,
                    ["physical_validation_claimed"] = false, ["mission_ready"] = false
                };
                }
                finally { SkirmishPresetRuntimeProjection.ReleaseBoundSourceReadback(em); }
            }
            finally { SourceS073LegacyCombatPresetProvider.RestoreBinding(em, hadPriorLegacyPreset ? priorLegacyPreset : null); }
        }
        else
        {
            // Preserve the existing explicit AI-owner experiment path. The new
            // rules-owner path above must transition the copied session itself.
            match.Phase=SkirmishPhase.Playing;
            em.SetComponentData(session,match);
        }

        if(preparingRulesOnly)
        {
            using var economiesAfterRules=em.CreateEntityQuery(typeof(FactionEconomy));
            using var economyEntitiesAfterRules=economiesAfterRules.ToEntityArray(Allocator.Temp);
            var economyRows=new JArray();
            foreach(var economyEntity in economyEntitiesAfterRules)
            {
                var economy=em.GetComponentData<FactionEconomy>(economyEntity);
                economyRows.Add(new JObject{["faction_id"]=economy.FactionId,["money"]=economy.Money,
                    ["materials_only_construction"]=economy.MaterialsOnlyConstruction,
                    ["oil"]=economy.Oil,["fuel"]=economy.Fuel});
            }
            return new JObject{["status"]="PreparingRulesOwnerOnly",
                ["original_rules_owner"]=rulesOwnerObservation,["owner_invoked"]=(bool?)rulesOwnerObservation?["owner_invoked"]==true,
                ["bounded_ticks"]=0,["non_rules_ai_owners_invoked"]=false,
                ["copied_economies_after_rules"]=economyRows,
                ["source_world_or_session_mutated"]=false,["mission_admission_claimed"]=false,
                ["physical_validation_claimed"]=false,["mission_ready"]=false};
        }

        var controlState=new SystemState{EntityManager=em,Enabled=true};
        var squadState=new SystemState{EntityManager=em,Enabled=true};
        var targetState=new SystemState{EntityManager=em,Enabled=true};
        var orderState=new SystemState{EntityManager=em,Enabled=true};
        var rulesState=new SystemState{EntityManager=em,Enabled=true};
        var control=new AIFactionControlSystem();var squad=new AISquadSystem();var targeting=new AITargetingSystem();var combat=new AICombatOrderSystem();
        var rules=new SkirmishRulesSystem();
        bool controlCreated=false,squadCreated=false,targetCreated=false,orderCreated=false,rulesCreated=false;
        bool presetBound=false;
        int controlRuns=0,squadRuns=0,targetRuns=0,orderRuns=0;
        var ticks=new JArray();
        bool hadPriorPreset=SourceS073LegacyCombatPresetProvider.TryGetBound(em,out var priorPreset);
        bool bindingRestored=false;
        try
        {
            SourceS073LegacyCombatPresetProvider.Bind(em,combatPresetPath);presetBound=true;
            controlState.Begin();try{controlCreated=true;control.OnCreate(ref controlState);}finally{controlState.End();}
            squadState.Begin();try{squadCreated=true;squad.OnCreate(ref squadState);}finally{squadState.End();}
            targetState.Begin();try{targetCreated=true;targeting.OnCreate(ref targetState);}finally{targetState.End();}
            orderState.Begin();try{orderCreated=true;combat.OnCreate(ref orderState);}finally{orderState.End();}
            if(targetPolicyDiagnostic){rulesState.Begin();try{rulesCreated=true;rules.OnCreate(ref rulesState);}finally{rulesState.End();}}

            for(int tick=0;tick<ownerTickLimit;tick++)
            {
                double elapsed=(tick+1)*(double)OwnerTickDelta;
                em.WorldUnmanaged.Time=new TimeData{ElapsedTime=elapsed,DeltaTime=OwnerTickDelta};
                bool capturePolicyBoundary=targetPolicyDiagnostic&&(tick==0||tick>=ownerTickLimit-5);
                JObject policyBefore=capturePolicyBoundary?CaptureTargetPolicyState(em,session,tick+1,combatPresetPath,"BeforeAIOwners"):null;
                controlState.Begin();try{if(controlState.Enabled&&controlState.RequiredQueriesPresent){control.OnUpdate(ref controlState);controlRuns++;}}finally{controlState.End();}
                squadState.Begin();try{if(squadState.Enabled&&squadState.RequiredQueriesPresent){squad.OnUpdate(ref squadState);squadRuns++;}}finally{squadState.End();}
                targetState.Begin();try{if(targetState.Enabled&&targetState.RequiredQueriesPresent){targeting.OnUpdate(ref targetState);targetRuns++;}}finally{targetState.End();}
                orderState.Begin();try{if(orderState.Enabled&&orderState.RequiredQueriesPresent){combat.OnUpdate(ref orderState);orderRuns++;}}finally{orderState.End();}
                JObject policyAfterAI=capturePolicyBoundary?CaptureTargetPolicyState(em,session,tick+1,combatPresetPath,"AfterAIOwnersBeforeRulesOwner"):null;
                if(targetPolicyDiagnostic)
                {
                    rulesState.Begin();
                    try
                    {
                        if(!rulesState.Enabled||!rulesState.RequiredQueriesPresent)
                            throw new InvalidDataException("Original SkirmishRulesSystem required-query gate closed during target-policy diagnostic");
                        rules.OnUpdate(ref rulesState);
                    }
                    finally{rulesState.End();}
                }
                JObject policyAfterRules=capturePolicyBoundary?CaptureTargetPolicyState(em,session,tick+1,combatPresetPath,"AfterOriginalRulesOwner"):null;
                CollectOwnerCreatedEntities(em,cleanup);
                ticks.Add(new JObject{["tick"]=tick+1,["world_elapsed_seconds"]=elapsed,
                    ["match_elapsed_before_ai_seconds"]=policyBefore?["match_elapsed_seconds"]?.DeepClone(),
                    ["match_elapsed_after_rules_seconds"]=policyAfterRules?["match_elapsed_seconds"]?.DeepClone(),
                    ["target_policy_observation_before_ai"]=policyBefore,
                    ["target_policy_observation_after_ai_before_rules"]=policyAfterAI,
                    ["target_policy_observation_after_rules"]=policyAfterRules,
                    ["owners_invoked"]=new JArray(
                    controlState.RequiredQueriesPresent?"AIFactionControlSystem":null,
                    squadState.RequiredQueriesPresent?"AISquadSystem":null,
                    targetState.RequiredQueriesPresent?"AITargetingSystem":null,
                    orderState.RequiredQueriesPresent?"AICombatOrderSystem":null,
                    targetPolicyDiagnostic&&rulesState.RequiredQueriesPresent?"SkirmishRulesSystem.Playing":null),["outputs"]=CensusOwnerOutputs(em)});
            }
        }
        finally
        {
            try
            {
                CollectOwnerCreatedEntities(em,cleanup);
                if(orderCreated){orderState.Begin();try{combat.OnDestroy(ref orderState);}finally{orderState.End();}}
                if(targetCreated){targetState.Begin();try{targeting.OnDestroy(ref targetState);}finally{targetState.End();}}
            }
            finally
            {
                if(presetBound){SourceS073LegacyCombatPresetProvider.RestoreBinding(em,hadPriorPreset?priorPreset:null);bindingRestored=true;}
            }
        }
        var after=CensusOwnerOutputs(em);
        JObject squadEligibilityAfterOwners=CaptureSquadEligibility(em,sourceToCopy,"After122TickOriginalAIOwners");
        JObject aiSquadPlanFeasibility=((JArray)squadEligibilityAfterOwners["plan_feasibility"])
            .OfType<JObject>().FirstOrDefault(x=>(byte?)x["faction"]==2);
        var skippedOwners=new JArray();
        if(controlRuns<ownerTickLimit)skippedOwners.Add(new JObject{["owner"]="AIFactionControlSystem",["updates"]=controlRuns,["requested_updates"]=ownerTickLimit});
        if(squadRuns<ownerTickLimit)skippedOwners.Add(new JObject{["owner"]="AISquadSystem",["updates"]=squadRuns,["requested_updates"]=ownerTickLimit,
            ["source_configuration_can_legitimately_have_no_eligible_squads"]=true});
        if(targetRuns<ownerTickLimit)skippedOwners.Add(new JObject{["owner"]="AITargetingSystem",["updates"]=targetRuns,["requested_updates"]=ownerTickLimit,
            ["required_query_gate"]="AISquad",["required_query_population_at_window_end"]=Count<AISquad>(em),
            ["source_squad_plan_feasibility"]=aiSquadPlanFeasibility?.DeepClone(),
            ["source_configuration_can_legitimately_have_no_eligible_squads"]=Count<AISquad>(em)==0});
        if(orderRuns<ownerTickLimit)skippedOwners.Add(new JObject{["owner"]="AICombatOrderSystem",["updates"]=orderRuns,["requested_updates"]=ownerTickLimit,
            ["required_query_gate"]="AISquad",["required_query_population_at_window_end"]=Count<AISquad>(em),
            ["source_squad_plan_feasibility"]=aiSquadPlanFeasibility?.DeepClone(),
            ["source_configuration_can_legitimately_have_no_eligible_squads"]=Count<AISquad>(em)==0});
        AuditCopyEntityEdges(em);
        JObject movementOwnerDiagnostic=runMovementOwnerDiagnostic
            ? RunMovementOwnersAfterTargetPolicy(em,session,cleanup,sourceToCopy,movementOwnerPreflightBefore)
            : null;
        return new JObject{["status"]="OriginalAIOwnerWindowCompletedWithRequiredQueryGateAudit",["owners"]=new JArray(
            "AIFactionControlSystem","AISquadSystem","AITargetingSystem","AICombatOrderSystem"),
            ["all_owners_invoked_every_tick"]=skippedOwners.Count==0,["owners_with_closed_required_query_gates"]=skippedOwners,
            ["owner_required_query_gate_audit_passed"]=skippedOwners.Count==0,
            ["original_rules_owner"] = rulesOwnerObservation,
            ["squad_eligibility_after_ai_owners"] = squadEligibilityAfterOwners,
            ["invocations"]=new JObject{["AIFactionControlSystem"]=controlRuns,["AISquadSystem"]=squadRuns,["AITargetingSystem"]=targetRuns,["AICombatOrderSystem"]=orderRuns},
            ["bounded_ticks"]=ownerTickLimit,["delta_seconds"]=OwnerTickDelta,["world_elapsed_seconds_max"]=ownerTickLimit*OwnerTickDelta,
            ["target_policy_diagnostic_opt_in"]=targetPolicyDiagnostic,
            ["movement_owner_diagnostic_opt_in"]=runMovementOwnerDiagnostic,
            ["movement_owner_diagnostic"]=movementOwnerDiagnostic,
            ["match_elapsed_advanced_only_by_original_rules_owner"]=targetPolicyDiagnostic,
            ["target_policy_diagnostic_scope"]=targetPolicyDiagnostic?"ReadOnly observations around original AI owners and original SkirmishRulesSystem Playing update on the disposable source-derived copy":"NotRequested",
            ["target_policy_claims"]="Observed only; no target or match elapsed value injected by fixture",
            ["owner_order_per_tick"]=new JArray("AIFactionControlSystem","AISquadSystem","AITargetingSystem","AICombatOrderSystem"),
            ["ticks"]=ticks,["outputs_before"]=before,["outputs_after"]=after,["created_entities_owned_for_cleanup"]=cleanup.Count,
            ["pinned_combat_preset_asset"]=Path.GetFullPath(combatPresetPath),["pinned_combat_preset_sha256"]=SourceS073LegacyCombatPresetProvider.SourceAssetSha256,
            ["prior_copy_provider_binding_restored"]=bindingRestored,
            ["no_fixture_injected_ai_tags_or_targets"]=true,["source_world_or_session_mutated"]=false,
            ["scheduler_parity_claimed"]=false,["mission_admission_claimed"]=false,["physical_validation_claimed"]=false,["mission_ready"]=false};
    }

    static JObject CaptureMovementOwnerPreflight(EntityManager em,string boundary)
    {
        using var movementUnits=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{
            ComponentType.ReadOnly<UnitGrid>(),ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<UnitMove>(),ComponentType.ReadOnly<UnitFootprint>(),ComponentType.ReadOnly<UnitMovementBehavior>(),
            ComponentType.ReadOnly<UnitVehicleKinematics>(),ComponentType.ReadOnly<UnitVehicleMovement>()}});
        using var approachUnits=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<EngageTarget>(),
            ComponentType.ReadOnly<UnitAttack>(),ComponentType.ReadOnly<UnitFootprint>(),ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<UnitMove>()},None=new[]{ComponentType.ReadOnly<UnitAirMovement>(),
            ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),ComponentType.ReadOnly<BaseBreachOrder>()}});
        using var pathRequests=em.CreateEntityQuery(typeof(UnitPathRequest));
        using var grid=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(DynamicBlockerComponent));
        using var surface=em.CreateEntityQuery(typeof(MapSurfaceComponent));
        using var ecb=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{
            ComponentType.ReadOnly<EndSimulationEntityCommandBufferSystem.Singleton>()},
            Options=EntityQueryOptions.IncludeSystems});
        var sample=new JArray();
        using(var entities=movementUnits.ToEntityArray(Allocator.Temp))
        {
            foreach(var entity in entities.Take(16))
            {
                var p=em.GetComponentData<LocalTransform>(entity).Position;
                var cell=em.GetComponentData<UnitGrid>(entity).Cell;
                sample.Add(new JObject{["entity"]=$"{entity.Index}:{entity.Version}",
                    ["faction"]=em.GetComponentData<Faction>(entity).Id,["position"]=new JArray(p.x,p.y,p.z),
                    ["cell"]=new JArray(cell.x,cell.y)});
            }
        }
        return new JObject{["boundary"]=boundary,["source_derived_movement_unit_population"]=movementUnits.CalculateEntityCount(),
            ["ai_controlled_tag_population"]=Count<AIControlledTag>(em),["source_derived_squad_population"]=Count<AISquad>(em),
            ["source_derived_engage_target_population"]=Count<EngageTarget>(em),
            ["source_derived_engage_approach_query_population"]=approachUnits.CalculateEntityCount(),
            ["source_derived_path_request_population"]=pathRequests.CalculateEntityCount(),
            ["copied_grid_and_dynamic_occupancy_population"]=grid.CalculateEntityCount(),
            ["copied_map_surface_population"]=surface.CalculateEntityCount(),
            ["native_world_end_simulation_ecb_singleton_population"]=ecb.CalculateEntityCount(),
            ["source_derived_unit_position_sample"]=sample,
            ["component_sources"]="Copied from the same source entity only; no movement component synthesized"};
    }

    static JObject RunMovementOwnersAfterTargetPolicy(EntityManager em,Entity session,HashSet<Entity> cleanup,
        Dictionary<Entity,Entity> sourceToCopy,JObject beforeWindow)
    {
        Require(beforeWindow!=null && (int?)beforeWindow["source_derived_engage_approach_query_population"]>=0,
            "Movement-owner phase requires the completed target-policy preflight");
        Require(em.GetComponentData<SkirmishMatchState>(session).Phase==SkirmishPhase.Playing,
            "Original Rules owner must have placed the copied session in Playing before movement owners run");
        Require(em.CreateEntityQuery(typeof(EndSimulationEntityCommandBufferSystem.Singleton)).IsEmptyIgnoreFilter,
            "Disposable source copy unexpectedly already owns an EndSimulation buffer singleton");
        var deferred=new OwnedEndSimulationBuffers();
        Entity deferredEntity=em.CreateEntity();cleanup.Add(deferredEntity);
        em.AddComponentData(deferredEntity,new EndSimulationEntityCommandBufferSystem.Singleton{Owner=deferred});
        UnitMoveOrderRequestSystem.EnsureQueueEntity(em);
        using(var queue=em.CreateEntityQuery(typeof(UnitMoveOrderQueueComponent)))
        {
            Require(queue.CalculateEntityCount()==1,"Original move-order owner requires one fixture-local queue entity");
            cleanup.Add(queue.GetSingletonEntity());
        }
        using var ground=new OriginalGroundOwners(em,generatedFresh:true);
        using var combat=new OriginalCombatOwners(em);
        var approachState=new SystemState{EntityManager=em,Enabled=true};
        var approach=new SkirmishCombatApproachSystem();
        approachState.Begin();try{approach.OnCreate(ref approachState);}finally{approachState.End();}
            var before=CaptureMovementPositions(em);
            var copyToSource=sourceToCopy.ToDictionary(x=>x.Value,x=>x.Key);
            var perTick=new JArray();
            int validationInvocation=0,engageInvocation=0,syncInvocation=0,approachInvocation=0,pathInvocation=0,
                chaseInvocation=0,gridMovementInvocation=0,occupancyInvocation=0,pathDrainInvocation=0;
            double baseElapsed=TargetPolicyDiagnosticTicks*(double)OwnerTickDelta;
            for(int tick=0;tick<MovementOwnerDiagnosticTicks;tick++)
            {
                double elapsed=baseElapsed+(tick+1)*(double)MovementOwnerTickDelta;
                em.WorldUnmanaged.Time=new TimeData{ElapsedTime=elapsed,DeltaTime=MovementOwnerTickDelta};
                if(combat.ValidatePhase())validationInvocation++;
                if(ground.OccupancyPhase())occupancyInvocation++;
                if(combat.AcquisitionPhase())engageInvocation++;
                if(combat.SynchronizePhase())syncInvocation++;
                approachState.Begin();
                try
                {
                    if(approachState.Enabled&&approachState.RequiredQueriesPresent)
                    {approach.OnUpdate(ref approachState);approachInvocation++;}
                }
                finally{approachState.End();}
                if(ground.PathPhase())pathInvocation++;
                if(combat.ChasePhase())chaseInvocation++;
                if(ground.MovementPhase())gridMovementInvocation++;
                ground.EndSimulationPhase();
                CollectOwnerCreatedEntities(em,cleanup);
                var afterTick=CaptureMovementPositions(em);
                perTick.Add(new JObject{["tick_after_target_policy_window"]=tick+1,["elapsed_seconds"]=elapsed,
                    ["engage_targets"]=Count<EngageTarget>(em),["path_requests"]=Count<UnitPathRequest>(em),
                    ["position_changed_unit_count_since_window_start"]=CountMovementChanges(before,afterTick)});
            }
            int pendingDrainTicks=0;
            while(ground.HasPendingPath&&pendingDrainTicks<4)
            {
                pendingDrainTicks++;
                double elapsed=baseElapsed+(MovementOwnerDiagnosticTicks+pendingDrainTicks)*(double)MovementOwnerTickDelta;
                em.WorldUnmanaged.Time=new TimeData{ElapsedTime=elapsed,DeltaTime=MovementOwnerTickDelta};
                if(ground.PathPhase())pathDrainInvocation++;
                ground.EndSimulationPhase();
            }
            Require(!ground.HasPendingPath,"Original pathfinding owner retained pending work after the bounded completion drain");
            var after=CaptureMovementPositions(em);
            var changed=new JArray();
            foreach(var pair in before.OrderBy(x=>x.Key.Index))
            {
                if(!after.TryGetValue(pair.Key,out var value)||pair.Value.Equals(value))continue;
                changed.Add(new JObject{["fixture_entity"]=$"{pair.Key.Index}:{pair.Key.Version}",
                    ["source_entity"]=copyToSource.TryGetValue(pair.Key,out var sourceEntity)?$"{sourceEntity.Index}:{sourceEntity.Version}":null,
                    ["start_position"]=PositionArray(pair.Value),["end_position"]=PositionArray(value)});
            }
            var invoked=new JArray();
            if(validationInvocation>0)invoked.Add("EngageTargetValidateSystem");
            if(occupancyInvocation>0)invoked.Add("DynamicOccupancyRebuildSystem");
            if(engageInvocation>0)invoked.Add("UnitEngagementSystem");
            if(syncInvocation>0)invoked.Add("EngageTargetSyncSystem");
            if(approachInvocation>0)invoked.Add("SkirmishCombatApproachSystem");
            if(pathInvocation>0||pathDrainInvocation>0)invoked.Add("UnitPathfindingSystem");
            if(chaseInvocation>0)invoked.Add("UnitEngagedMovementSystem");
            if(gridMovementInvocation>0)invoked.Add("UnitGridMovementSystem");
            return new JObject{["status"]=changed.Count>0?"MovementObserved":"NoMovementObserved",
                ["scope"]="IsolatedPostTargetPolicyOriginalEngagementCombatApproachPathEngagedMovementAndGridMovementPhaseOn122TickSourceTargetPolicyDisposableCopy",
                ["before_122_tick_window"]=beforeWindow,["after_122_tick_window"]=CaptureMovementOwnerPreflight(em,"BeforeMovementOwnerTicks"),
                ["movement_owner_ticks"]=MovementOwnerDiagnosticTicks,["delta_seconds"]=MovementOwnerTickDelta,
                ["pending_path_completion_drain_ticks"]=pendingDrainTicks,["pending_path_work_at_end"]=ground.HasPendingPath,
                ["map_surface_source_component_copied"]=(int?)beforeWindow["copied_map_surface_population"]==1,
                ["map_surface_native_or_physical_parity_claimed"]=false,
                ["post_target_policy_phase_isolated"]=true,["rules_and_ai_owners_continued_during_movement_phase"]=false,
                ["timestep_approximation"]="Fixed 1/60 second owner calls with explicit source-order phase orchestration; not native World scheduler parity",
                ["owner_phases_requested"]=new JArray("EngageTargetValidateSystem","DynamicOccupancyRebuildSystem",
                    "UnitEngagementSystem","EngageTargetSyncSystem","SkirmishCombatApproachSystem",
                    "UnitPathfindingSystem","UnitEngagedMovementSystem","UnitGridMovementSystem"),
                ["owners_invoked"]=invoked,
                ["source_squad_population_after_target_policy_window"]=Count<AISquad>(em),
                ["source_assigned_targets_or_paths_present"]=Count<EngageTarget>(em)>0||Count<UnitPathRequest>(em)>0||Count<UnitPathFollow>(em)>0,
                ["zero_movement_explanation"]=changed.Count==0 && Count<AISquad>(em)==0
                    ? "Source AI emitted no squad, so original targeting/combat-order owners remained behind their AISquad required-query gates; no EngageTarget or path order existed for movement owners."
                    : changed.Count==0?"No copied source-derived unit position changed during the owner window.":null,
                ["invocations"]=new JObject{["EngageTargetValidateSystem"]=validationInvocation,
                    ["DynamicOccupancyRebuildSystem"]=occupancyInvocation,["UnitEngagementSystem"]=engageInvocation,
                    ["EngageTargetSyncSystem"]=syncInvocation,["SkirmishCombatApproachSystem"]=approachInvocation,
                    ["UnitPathfindingSystem"]=pathInvocation,["UnitPathfindingSystem.CompletionDrain"]=pathDrainInvocation,
                    ["UnitEngagedMovementSystem"]=chaseInvocation,["UnitGridMovementSystem"]=gridMovementInvocation,
                    ["EndSimulationEntityCommandBufferSystem.Playback"]=MovementOwnerDiagnosticTicks+pendingDrainTicks},
                ["owner_phase_calls"]=new JObject{["EngageTargetValidateSystem"]=MovementOwnerDiagnosticTicks,
                    ["DynamicOccupancyRebuildSystem"]=MovementOwnerDiagnosticTicks,["UnitEngagementSystem"]=MovementOwnerDiagnosticTicks,
                    ["EngageTargetSyncSystem"]=MovementOwnerDiagnosticTicks,["SkirmishCombatApproachSystem"]=MovementOwnerDiagnosticTicks,
                    ["UnitPathfindingSystem"]=MovementOwnerDiagnosticTicks,["UnitEngagedMovementSystem"]=MovementOwnerDiagnosticTicks,
                    ["UnitGridMovementSystem"]=MovementOwnerDiagnosticTicks},
                ["source_derived_unit_position_changes"]=changed,["moved_source_derived_unit_count"]=changed.Count,
                ["tick_observations"]=perTick,["no_fixture_injected_targets_or_admission"]=true,
                ["fixture_injected_move_orders"]=false,["source_world_or_session_mutated"]=false,
                ["scheduler_parity_claimed"]=false,["mission_admission_claimed"]=false,["mission_ready"]=false};
    }

    static Dictionary<Entity,float3> CaptureMovementPositions(EntityManager em)
    {
        var positions=new Dictionary<Entity,float3>();
        using var query=em.CreateEntityQuery(typeof(Faction),typeof(UnitGrid),typeof(LocalTransform),typeof(UnitMove),typeof(UnitFootprint));
        using var entities=query.ToEntityArray(Allocator.Temp);
        foreach(var entity in entities)positions.Add(entity,em.GetComponentData<LocalTransform>(entity).Position);
        return positions;
    }

    static int CountMovementChanges(Dictionary<Entity,float3> before,Dictionary<Entity,float3> after)
    {return before.Count(pair=>after.TryGetValue(pair.Key,out var position)&&!pair.Value.Equals(position));}

    static JArray PositionArray(float3 p)=>new JArray(p.x,p.y,p.z);


    static JObject CensusOwnerOutputs(EntityManager em)
    {
        int squads=Count<AISquad>(em),members=Count<AISquadMember>(em),tags=Count<AIControlledTag>(em);
        int path=Count<UnitPathRequest>(em),engage=Count<EngageTarget>(em),breach=Count<BaseBreachOrder>(em);
        var rows=new JArray();
        using(var q=em.CreateEntityQuery(typeof(AISquad)))
        using(var es=q.ToEntityArray(Allocator.Temp))
        {
            if(es.Length>64)throw new InvalidDataException("Disposable AI output exceeded 64 squad entities");
            foreach(var e in es){var s=em.GetComponentData<AISquad>(e);int n=em.HasBuffer<AISquadUnit>(e)?em.GetBuffer<AISquadUnit>(e).Length:0;
                rows.Add(new JObject{["entity_index"]=e.Index,["entity_version"]=e.Version,["squad_id"]=s.SquadId,["faction"]=s.FactionId,
                    ["target_index"]=s.TargetEntity.Index,["target_version"]=s.TargetEntity.Version,["target_kind"]=s.TargetKind,["members"]=n});}
        }
        return new JObject{["ai_controlled_tags"]=tags,["squads"]=squads,["squad_members"]=members,["unit_path_requests"]=path,
            ["engage_targets"]=engage,["base_breach_orders"]=breach,["squad_rows"]=rows};
    }

    // This observer reads the disposable copy only. Squad targets are read from
    // the actual owner-written components; candidate/member rows expose the
    // exact source policy gates without selecting or assigning any target.
    static JObject CaptureTargetPolicyState(EntityManager em,Entity session,int tick,string combatPresetPath,string boundary)
    {
        var match=em.GetComponentData<SkirmishMatchState>(session);
        var gameplayQuery=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
        try
        {
            Require(gameplayQuery.CalculateEntityCount()==1,"Target-policy observer requires unique gameplay singleton");
            var gameplay=gameplayQuery.GetSingleton<RuntimeGameplayStateComponent>();
            var preset=SourceS073LegacyCombatPresetProvider.Resolve(em);
            Require(preset!=null,"Target-policy observer requires the copied original preset");
            JObject playerBase=PositionRecord(em,match.PlayerMainBase),enemyBase=PositionRecord(em,match.EnemyMainBase);
            var candidates=new JArray();
            using(var query=em.CreateEntityQuery(new EntityQueryDesc{
                All=new[]{ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<UnitHealth>(),ComponentType.ReadOnly<UnitAttack>(),ComponentType.ReadOnly<LocalTransform>()},
                None=new[]{ComponentType.ReadOnly<UnitResourceHauler>(),ComponentType.ReadOnly<OperationMapBuildingComponent>()}}))
            using(var entities=query.ToEntityArray(Allocator.Temp))
            {
                if(entities.Length>MaxPopulation)throw new InvalidDataException("Target-policy candidate census exceeded source copy population bound");
                foreach(var entity in entities)
                {
                    var faction=em.GetComponentData<Faction>(entity).Id;
                    var health=em.GetComponentData<UnitHealth>(entity).Current;
                    var attack=em.GetComponentData<UnitAttack>(entity);
                    var position=em.GetComponentData<LocalTransform>(entity).Position;
                    candidates.Add(new JObject{
                        ["entity"]=new JArray(entity.Index,entity.Version),["faction_id"]=faction,
                        ["health_current"]=health,["attack_damage"]=attack.Damage,["attack_range"]=attack.Range,
                        ["x"]=position.x,["z"]=position.z,
                        ["excluded_resource_hauler"]=em.HasComponent<UnitResourceHauler>(entity),
                        ["excluded_operation_map_building"]=em.HasComponent<OperationMapBuildingComponent>(entity),
                        ["target_candidate_query_member"]=true
                    });
                }
            }
            var squads=new JArray();
            using(var query=em.CreateEntityQuery(typeof(AISquad)))
            using(var entities=query.ToEntityArray(Allocator.Temp))
            {
                foreach(var squadEntity in entities)
                {
                    var squad=em.GetComponentData<AISquad>(squadEntity);
                    JObject target=PositionRecord(em,squad.TargetEntity);
                    var members=new JArray();
                    if(em.HasBuffer<AISquadUnit>(squadEntity))
                    foreach(var item in em.GetBuffer<AISquadUnit>(squadEntity,true))
                    {
                        Entity unit=item.Unit; bool exists=em.Exists(unit);
                        bool healthPresent=exists&&em.HasComponent<UnitHealth>(unit);
                        bool alive=healthPresent&&em.GetComponentData<UnitHealth>(unit).Current>0;
                        bool transformPresent=exists&&em.HasComponent<LocalTransform>(unit);
                        bool attackPresent=exists&&em.HasComponent<UnitAttack>(unit);
                        var member=new JObject{["entity"]=new JArray(unit.Index,unit.Version),["exists"]=exists,
                            ["health_component_present"]=healthPresent,["alive_gate_passes"]=alive,
                            ["transform_component_present"]=transformPresent,["attack_component_present"]=attackPresent,
                            ["member_eligible_by_original_policy"]=alive&&transformPresent&&attackPresent};
                        if(alive&&transformPresent&&attackPresent)
                        {
                            var p=em.GetComponentData<LocalTransform>(unit).Position;var a=em.GetComponentData<UnitAttack>(unit);
                            member["x"]=p.x;member["z"]=p.z;member["attack_range"]=a.Range;
                        }
                        members.Add(member);
                    }
                    squads.Add(new JObject{["entity"]=new JArray(squadEntity.Index,squadEntity.Version),
                        ["squad_id"]=squad.SquadId,["faction_id"]=squad.FactionId,
                        ["target_faction_id"]=squad.TargetFactionId,["target_kind"]=(int)squad.TargetKind,
                        ["target_entity"]=target,["target_cell"]=new JArray(squad.TargetCell.x,squad.TargetCell.y),
                        ["member_count"]=members.Count,["members"]=members});
                }
            }
            return new JObject{["tick"]=tick,["boundary"]=boundary,["world_elapsed_seconds"]=em.WorldUnmanaged.Time.ElapsedTime,
                ["world_delta_seconds"]=em.WorldUnmanaged.Time.DeltaTime,["match_elapsed_seconds"]=match.ElapsedSeconds,
                ["match_phase"]=match.Phase.ToString(),["outcome"]=match.Outcome.ToString(),
                ["simulation_active"]=gameplay.SimulationActive!=0,["play_requested"]=gameplay.PlayRequested!=0,
                ["required_rule_gates"]=new JObject{["session_unique"]=true,["phase_playing"]=match.Phase==SkirmishPhase.Playing,
                    ["simulation_active"]=gameplay.SimulationActive!=0,["preset_scenario_index"]=match.ScenarioIndex},
                ["preset"] = new JObject{["asset_path"]=Path.GetFullPath(combatPresetPath),
                    ["sha256"]=SourceS073LegacyCombatPresetProvider.SourceAssetSha256,
                    ["first_attack_seconds"]=preset.firstAttackSeconds,["base_defense_radius"]=preset.baseDefenseRadius},
                ["bases"]=new JObject{["player"]=playerBase,["enemy"]=enemyBase},
                ["target_candidate_query_count"]=candidates.Count,["target_candidates"]=candidates,
                ["squad_count"]=squads.Count,["squads"]=squads,
                ["target_assignment_owner"]="SkirmishCombatPolicy.TryAssignTargets called by original AITargetingSystem",
                ["read_only_observer"]=true,["fixture_injected_target"]=false};
        }
        finally{gameplayQuery.Dispose();}
    }

    static JObject PositionRecord(EntityManager em,Entity entity)
    {
        bool exists=entity!=Entity.Null&&em.Exists(entity);
        var row=new JObject{["entity"]=entity==Entity.Null?JValue.CreateNull():new JArray(entity.Index,entity.Version),
            ["exists"]=exists,["health_component_present"]=exists&&em.HasComponent<UnitHealth>(entity),
            ["transform_component_present"]=exists&&em.HasComponent<LocalTransform>(entity)};
        if(exists&&em.HasComponent<Faction>(entity))row["faction_id"]=em.GetComponentData<Faction>(entity).Id;
        if(exists&&em.HasComponent<UnitHealth>(entity))row["health_current"]=em.GetComponentData<UnitHealth>(entity).Current;
        if(exists&&em.HasComponent<LocalTransform>(entity)){var p=em.GetComponentData<LocalTransform>(entity).Position;row["x"]=p.x;row["y"]=p.y;row["z"]=p.z;}
        if(exists&&em.HasComponent<UnitGrid>(entity)){var cell=em.GetComponentData<UnitGrid>(entity).Cell;row["cell"]=new JArray(cell.x,cell.y);}
        return row;
    }

    static void CollectOwnerCreatedEntities(EntityManager em,HashSet<Entity> cleanup)
    {
        foreach(Type t in new[]{typeof(AISquad),typeof(AIDiagnosticLogQueueComponent)})
        {using var q=em.CreateEntityQuery(t);using var es=q.ToEntityArray(Allocator.Temp);foreach(var e in es)cleanup.Add(e);}
    }

    static void AuditCopyEntityEdges(EntityManager em)
    {
        foreach(Type t in new[]{typeof(AISquad),typeof(AISquadMember),typeof(AISquadUnit),typeof(EngageTarget),typeof(BaseBreachOrder),typeof(SkirmishMatchState)})
        {
            using var q=t==typeof(AISquad)?em.CreateEntityQuery(typeof(AISquad)):t==typeof(AISquadMember)?em.CreateEntityQuery(typeof(AISquadMember)):
                t==typeof(AISquadUnit)?em.CreateEntityQuery(typeof(AISquadUnit)):t==typeof(EngageTarget)?em.CreateEntityQuery(typeof(EngageTarget)):
                t==typeof(BaseBreachOrder)?em.CreateEntityQuery(typeof(BaseBreachOrder)):em.CreateEntityQuery(typeof(SkirmishMatchState));
            using var entities=q.ToEntityArray(Allocator.Temp);
            foreach(var e in entities)
            {
                if(t==typeof(AISquad)){var x=em.GetComponentData<AISquad>(e);Require(x.TargetEntity==Entity.Null||em.Exists(x.TargetEntity),"Owner output has unresolved AISquad.TargetEntity");}
                if(t==typeof(AISquadMember))Require(em.Exists(em.GetComponentData<AISquadMember>(e).Squad),"Owner output has unresolved AISquadMember.Squad");
                if(t==typeof(AISquadUnit))foreach(var x in em.GetBuffer<AISquadUnit>(e))Require(em.Exists(x.Unit),"Owner output has unresolved AISquadUnit.Unit");
                if(t==typeof(EngageTarget))Require(em.Exists(em.GetComponentData<EngageTarget>(e).Target),"Owner output has unresolved EngageTarget.Target");
                if(t==typeof(BaseBreachOrder)){var x=em.GetComponentData<BaseBreachOrder>(e);Require((x.FinalTarget==Entity.Null||em.Exists(x.FinalTarget))&&(x.BreachTarget==Entity.Null||em.Exists(x.BreachTarget)),"Owner output has unresolved BaseBreachOrder edge");}
                if(t==typeof(SkirmishMatchState)){var x=em.GetComponentData<SkirmishMatchState>(e);Require((x.PlayerMainBase==Entity.Null||em.Exists(x.PlayerMainBase))&&(x.EnemyMainBase==Entity.Null||em.Exists(x.EnemyMainBase)),"Owner output has unresolved session base edge");}
            }
        }
    }

    static void ValidateCopiedPresence(EntityManager src, EntityManager dst, Dictionary<Entity,Entity> remap, SameRunBirthInputs sameRunBirths)
    {
        foreach (var p in remap)
        {
            Entity a=p.Key,b=p.Value;
            Same<Faction>(src,dst,a,b);Same<UnitGrid>(src,dst,a,b);Same<UnitHealth>(src,dst,a,b);
            Same<UnitCombat>(src,dst,a,b);Same<UnitAttack>(src,dst,a,b);Same<LocalTransform>(src,dst,a,b);
            Same<AIControlledTag>(src,dst,a,b);Same<CampaignMissionUnitRoleComponent>(src,dst,a,b);
            Same<RuntimeBuildingCombatTag>(src,dst,a,b);Same<RuntimeBuildingCombatInfo>(src,dst,a,b);
            Same<StaticGridBlocker>(src,dst,a,b);Same<GridBlockerSize>(src,dst,a,b);Same<UnitResourceHauler>(src,dst,a,b);
            Same<FuelLogisticsOilSourceTag>(src,dst,a,b);Same<FuelLogisticsRefineryInputTag>(src,dst,a,b);
            Same<FuelLogisticsRefineryOutputTag>(src,dst,a,b);Same<FuelLogisticsFuelStorageTag>(src,dst,a,b);
            Same<UnitAirMovement>(src,dst,a,b);Same<AISquadPlan>(src,dst,a,b);Same<AITargetPrioritySetting>(src,dst,a,b);
            Same<RuntimeGameplayStateComponent>(src,dst,a,b);Same<FactionControlConfigTag>(src,dst,a,b);
            Same<RuntimeDiagnosticsStateComponent>(src,dst,a,b);Same<AIDiagnosticLogQueueComponent>(src,dst,a,b);
            Same<AISquad>(src,dst,a,b);Same<AISquadMember>(src,dst,a,b);Same<SkirmishBaseDefender>(src,dst,a,b);
            Same<SkirmishMainBase>(src,dst,a,b);
            Same<SkirmishMatchState>(src,dst,a,b);Same<EngageTarget>(src,dst,a,b);Same<BaseBreachOrder>(src,dst,a,b);
            Same<UnitPathRequest>(src,dst,a,b);Same<UnitTarget>(src,dst,a,b);Same<UnitPathFollow>(src,dst,a,b);
            Same<UnitPathRetryCooldown>(src,dst,a,b);Same<ManualControlledTag>(src,dst,a,b);
            Same<UnitPathRange>(src,dst,a,b);Same<AICombatOrderTag>(src,dst,a,b);Same<ManualMoveOrderTag>(src,dst,a,b);
            Same<ManualMoveGroupMemberTag>(src,dst,a,b);Same<AutoWanderMoveTag>(src,dst,a,b);Same<GridConfig>(src,dst,a,b);
            Same<AIBuildPlan>(src,dst,a,b);Same<AIProductionPlan>(src,dst,a,b);
            Same<FactionEconomy>(src,dst,a,b);Same<FactionEconomyPolicy>(src,dst,a,b);
            Same<FactionTacticalMaterialsComponent>(src,dst,a,b);
            Same<CustomGameStartupStateComponent>(src,dst,a,b);Same<InitialUnitsSpawnConfig>(src,dst,a,b);
            if(src.HasComponent<InitialUnitsSpawnConfig>(a))
            {
                var x=src.GetComponentData<InitialUnitsSpawnConfig>(a);var y=dst.GetComponentData<InitialUnitsSpawnConfig>(b);
                Require(y.BlockerPrefab==Entity.Null&&y.UnitSelectionMarkerPrefab==Entity.Null&&y.UnitHealthBarPrefab==Entity.Null&&
                    x.BlockerCount==y.BlockerCount&&x.SpawnRadiusCells==y.SpawnRadiusCells&&x.RandomSeed==y.RandomSeed&&
                    x.InitialMaterials==y.InitialMaterials&&x.MaterialsCapacity==y.MaterialsCapacity&&
                    x.InitialAiMaterials==y.InitialAiMaterials&&x.AiMaterialsCapacity==y.AiMaterialsCapacity&&
                    x.InitialOil==y.InitialOil&&x.InitialFuel==y.InitialFuel&&x.CreateFactionBases==y.CreateFactionBases,
                    "Copy startup config did not preserve original tactical scalar values with presentation refs stripped");
            }
            Require(dst.HasComponent<BuildingResourceStorageComponent>(b)==sameRunBirths.FuelBladders.Values.Contains(a),
                "Only directly observed source fuel bladder storage may enter the disposable copy");
            SameBuffer<AISquadUnit>(src,dst,a,b);SameBuffer<FactionControlEntry>(src,dst,a,b);
            SameBuffer<AIDiagnosticLogComponent>(src,dst,a,b);SameBuffer<GridWalkable>(src,dst,a,b);
            SameBuffer<SkirmishTrackedUnit>(src,dst,a,b);
            SameBuffer<InitialUnitsFactionSpawnEntry>(src,dst,a,b);
            SameBuffer<AIProductionPlanEntry>(src,dst,a,b);
            SameBuffer<BuildingConfiguredUnitReadModel>(src,dst,a,b);
            SameBuffer<BuildingRuntimeUnitProductionSummary>(src,dst,a,b);
            SameBuffer<BuildingRuntimeFactionSummary>(src,dst,a,b);
            SameBuffer<BuildingRuntimeFactionUsableFuelSummary>(src,dst,a,b);
            SameBuffer<BuildingProductionSlotReadModel>(src,dst,a,b);
            SameBuffer<BuildingFactionProductionSpawnPointReadModel>(src,dst,a,b);
            Require(src.HasComponent<BuildingRuntimeStateTag>(a)==dst.HasComponent<BuildingRuntimeStateTag>(b),
                "Building runtime boundary tag mismatch");
            if(src.HasBuffer<BuildingFactionUnitProductionRequest>(a))
            {
                var x=src.GetBuffer<BuildingFactionUnitProductionRequest>(a);var y=dst.GetBuffer<BuildingFactionUnitProductionRequest>(b);
                Require(x.Length==y.Length,"Production request buffer length mismatch");
                for(int i=0;i<x.Length;i++)Require(y[i].RequestId==x[i].RequestId&&y[i].FactionId==x[i].FactionId&&
                    y[i].UnitId.Equals(x[i].UnitId)&&y[i].Status==x[i].Status&&y[i].ResultCode==x[i].ResultCode&&
                    y[i].Cost==x[i].Cost&&y[i].MaterialsCost==x[i].MaterialsCost&&y[i].ResourcesReserved==x[i].ResourcesReserved&&
                    y[i].RequestOwner==Map(x[i].RequestOwner,remap),
                    "Production request buffer source values or entity remaps differ");
            }
            if(src.HasComponent<AISquad>(a))Require(dst.GetComponentData<AISquad>(b).TargetEntity==Map(src.GetComponentData<AISquad>(a).TargetEntity,remap),"AISquad.TargetEntity remap mismatch");
            if(src.HasComponent<AISquadMember>(a))Require(dst.GetComponentData<AISquadMember>(b).Squad==Map(src.GetComponentData<AISquadMember>(a).Squad,remap),"AISquadMember.Squad remap mismatch");
            if(src.HasComponent<EngageTarget>(a))Require(dst.GetComponentData<EngageTarget>(b).Target==Map(src.GetComponentData<EngageTarget>(a).Target,remap),"EngageTarget.Target remap mismatch");
            if(src.HasComponent<BaseBreachOrder>(a))
            {
                var x=src.GetComponentData<BaseBreachOrder>(a);var y=dst.GetComponentData<BaseBreachOrder>(b);
                Require(y.FinalTarget==Map(x.FinalTarget,remap)&&y.BreachTarget==Map(x.BreachTarget,remap),"BaseBreachOrder entity remap mismatch");
            }
            if(src.HasComponent<SkirmishMatchState>(a))
            {
                var x=src.GetComponentData<SkirmishMatchState>(a);var y=dst.GetComponentData<SkirmishMatchState>(b);
                Require(y.PlayerMainBase==Map(x.PlayerMainBase,remap)&&y.EnemyMainBase==Map(x.EnemyMainBase,remap),"SkirmishMatchState base remap mismatch");
            }
            if(src.HasBuffer<AISquadUnit>(a))
            {
                var x=src.GetBuffer<AISquadUnit>(a);var y=dst.GetBuffer<AISquadUnit>(b);
                for(int i=0;i<x.Length;i++)Require(y[i].Unit==Map(x[i].Unit,remap),"AISquadUnit entity remap/value mismatch");
            }
            if(src.HasBuffer<SkirmishTrackedUnit>(a))
            {
                var x=src.GetBuffer<SkirmishTrackedUnit>(a);var y=dst.GetBuffer<SkirmishTrackedUnit>(b);
                for(int i=0;i<x.Length;i++)Require(y[i].Entity==Map(x[i].Entity,remap) &&
                    y[i].FactionId==x[i].FactionId && y[i].Building==x[i].Building,"SkirmishTrackedUnit entity remap/value mismatch");
            }
            if(src.HasBuffer<FactionControlEntry>(a))
            {
                var x=src.GetBuffer<FactionControlEntry>(a);var y=dst.GetBuffer<FactionControlEntry>(b);
                for(int i=0;i<x.Length;i++)Require(x[i].FactionId==y[i].FactionId && x[i].AIControlled==y[i].AIControlled &&
                    x[i].IsPlayerFaction==y[i].IsPlayerFaction && x[i].LastLogTime==y[i].LastLogTime,"FactionControlEntry value mismatch");
                if(x.Length>0){var original=x[0];var probe=y[0];probe.LastLogTime+=1;y[0]=probe;
                    Require(x[0].LastLogTime==original.LastLogTime,"FactionControlEntry buffer aliases source storage");y[0]=original;}
            }
            if(src.HasBuffer<AIDiagnosticLogComponent>(a))
            {
                var x=src.GetBuffer<AIDiagnosticLogComponent>(a);var y=dst.GetBuffer<AIDiagnosticLogComponent>(b);
                for(int i=0;i<x.Length;i++)Require(x[i].Message.ToString()==y[i].Message.ToString() && x[i].Severity==y[i].Severity,"AIDiagnosticLogComponent value mismatch");
            }
            if(src.HasComponent<DynamicBlockerComponent>(a))
            {
                var x=src.GetComponentData<DynamicBlockerComponent>(a);var y=dst.GetComponentData<DynamicBlockerComponent>(b);
                Require(x.Counts.Length==y.Counts.Length && x.FriendlyPassFactionIds.Length==y.FriendlyPassFactionIds.Length && x.Blocked.Length==y.Blocked.Length,
                    "Dynamic blocker storage dimensions mismatch");
                for(int i=0;i<x.Counts.Length;i++)Require(x.Counts[i]==y.Counts[i] && x.FriendlyPassFactionIds[i]==y.FriendlyPassFactionIds[i] && x.Blocked.IsSet(i)==y.Blocked.IsSet(i),"Dynamic blocker data mismatch");
                if(x.Counts.Length>0)
                {
                    int before=x.Counts[0];y.Counts[0]=before+1;
                    Require(x.Counts[0]==before,"Dynamic blocker native array aliases source storage");y.Counts[0]=before;
                    bool blocked=x.Blocked.IsSet(0);y.Blocked.Set(0,!blocked);
                    Require(x.Blocked.IsSet(0)==blocked,"Dynamic blocker bit array aliases source storage");y.Blocked.Set(0,blocked);
                }
            }
            if(src.HasComponent<DynamicOccupancyComponent>(a))
            {
                var x=src.GetComponentData<DynamicOccupancyComponent>(a);var y=dst.GetComponentData<DynamicOccupancyComponent>(b);
                Require(x.Occupied.Length==y.Occupied.Length,"Dynamic occupancy dimensions mismatch");
                for(int i=0;i<x.Occupied.Length;i++)Require(x.Occupied.IsSet(i)==y.Occupied.IsSet(i),"Dynamic occupancy data mismatch");
                if(x.Occupied.Length>0){bool original=x.Occupied.IsSet(0);y.Occupied.Set(0,!original);
                    Require(x.Occupied.IsSet(0)==original,"Dynamic occupancy bit array aliases source storage");y.Occupied.Set(0,original);}
            }
            if(src.HasBuffer<GridWalkable>(a))
            {
                var x=src.GetBuffer<GridWalkable>(a);var y=dst.GetBuffer<GridWalkable>(b);
                Require(x.Length==y.Length,"GridWalkable buffer length mismatch");
                for(int i=0;i<x.Length;i++)Require(x[i].Value==y[i].Value,"GridWalkable value mismatch");
            }
        }
    }

    static void Same<T>(EntityManager src,EntityManager dst,Entity a,Entity b) where T:struct
    {Require(src.HasComponent<T>(a)==dst.HasComponent<T>(b),typeof(T).Name+" component presence mismatch");}
    static void SameBuffer<T>(EntityManager src,EntityManager dst,Entity a,Entity b) where T:struct,IBufferElementData
    {Require(src.HasBuffer<T>(a)==dst.HasBuffer<T>(b),typeof(T).Name+" buffer presence mismatch"); if(src.HasBuffer<T>(a))Require(src.GetBuffer<T>(a).Length==dst.GetBuffer<T>(b).Length,typeof(T).Name+" buffer length mismatch");}

    static void CopyDynamicGridStorage(EntityManager src, Entity from, EntityManager dst, Entity to, int cells)
    {
        var sb=src.GetComponentData<DynamicBlockerComponent>(from);
        var so=src.GetComponentData<DynamicOccupancyComponent>(from);
        Require(sb.WriteInProgress==0 && sb.Counts.Length==cells && sb.FriendlyPassFactionIds.Length==cells && sb.Blocked.IsCreated && sb.Blocked.Length==cells,
            "Source dynamic blocker storage is not committed or grid-sized");
        Require(so.Occupied.IsCreated && so.Occupied.Length==cells,"Source dynamic occupancy is not grid-sized");
        var bits=new bool[cells]; for(int i=0;i<cells;i++)bits[i]=sb.Blocked.IsSet(i);
        var occupied=new bool[cells]; for(int i=0;i<cells;i++)occupied[i]=so.Occupied.IsSet(i);
        dst.AddComponentData(to,new DynamicBlockerComponent { GridSize=sb.GridSize,Counts=new NativeArray<int>(sb.Counts.ToArray()),
            FriendlyPassFactionIds=new NativeArray<byte>(sb.FriendlyPassFactionIds.ToArray()),Blocked=new NativeBitArray(bits),
            StorageEpoch=sb.StorageEpoch,AppliedStorageEpoch=sb.AppliedStorageEpoch,AppliedRevision=sb.AppliedRevision,
            AppliedGrid=sb.AppliedGrid,WriteInProgress=0,EpochExhausted=sb.EpochExhausted });
        dst.AddComponentData(to,new DynamicOccupancyComponent { GridSize=so.GridSize,Occupied=new NativeBitArray(occupied) });
    }

    static void DisposeCopy(EntityManager copy, IEnumerable<Entity> entities)
    {
        // Dispose explicitly owned native grid backing before retiring its ECS owner.
        foreach(var e in entities)
        {
            if(copy.HasComponent<DynamicBlockerComponent>(e))
            {
                var b=copy.GetComponentData<DynamicBlockerComponent>(e);b.Counts.Dispose();b.FriendlyPassFactionIds.Dispose();b.Blocked.Dispose();
            }
            if(copy.HasComponent<DynamicOccupancyComponent>(e))
            {var o=copy.GetComponentData<DynamicOccupancyComponent>(e);o.Occupied.Dispose();}
            if(copy.HasComponent<PathPoolComponent>(e))
            {var p=copy.GetComponentData<PathPoolComponent>(e);if(p.Cells.IsCreated)p.Cells.Dispose();}
            if(copy.HasComponent<DynamicBlockerComponent>(e))copy.RemoveComponent<DynamicBlockerComponent>(e);
            if(copy.HasComponent<DynamicOccupancyComponent>(e))copy.RemoveComponent<DynamicOccupancyComponent>(e);
            if(copy.HasComponent<PathPoolComponent>(e))copy.RemoveComponent<PathPoolComponent>(e);
        }
        foreach(var e in entities)if(copy.Exists(e))copy.DestroyEntity(e);
    }
}
