using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

// Opt-in observer at the request-to-registration call site. The callback receives
// the exact request value being processed and the RuntimeBuildingEntity returned
// by source registration, before the placement result is returned to the request
// owner. It is deliberately read-only and does not add ECS components.
internal sealed class SourceS073OriginalBirthIdentityCapture
{
    internal const string OptIn = "WARLINE_S073_ORIGINAL_BIRTH_IDENTITY_CAPTURE";
    readonly EntityManager em;
    readonly List<JObject> rows = new List<JObject>();
    readonly Entity sessionEntity;
    readonly string sessionId;
    readonly int scenarioIndex;
    readonly int seed;
    readonly byte phase;
    readonly ulong worldSequence;
    Entity observedPlan;
    int expectedFuelSourceRows = -1;

    internal SourceS073OriginalBirthIdentityCapture(EntityManager manager)
    {
        em = manager ?? throw new ArgumentNullException(nameof(manager));
        using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
        if (matches.CalculateEntityCount() != 1) throw new InvalidDataException("Unique source S073 session required for direct birth capture");
        sessionEntity = matches.GetSingletonEntity();
        if (!em.Exists(sessionEntity)) throw new InvalidDataException("Source S073 session disappeared during direct birth capture");
        var match = em.GetComponentData<SkirmishMatchState>(sessionEntity);
        sessionId = match.SessionId.ToString();
        scenarioIndex = match.ScenarioIndex;
        seed = match.Seed;
        phase = (byte)match.Phase;
        worldSequence = em.WorldUnmanaged.SequenceNumber;
    }

    internal static bool Enabled => Environment.GetEnvironmentVariable(OptIn) == "1";

    internal void Observe(BuildingRuntimeSpawnRequest request, RuntimeBuildingEntity building)
    {
        if (building == null || building.CombatEntity == Entity.Null || !em.Exists(building.CombatEntity) ||
            request.RequestId <= 0 || !GeneratedRuntimeSpawnProcessor.IsLegacyInitialRequest(em, request))
            throw new InvalidDataException("Direct original S073 request and registered runtime building required");

        if (observedPlan == Entity.Null)
        {
            observedPlan = request.PlanEntity;
            expectedFuelSourceRows = CountFuelSourceRows(observedPlan);
        }
        else if (observedPlan != request.PlanEntity)
            throw new InvalidDataException("One original startup plan identity required for direct birth capture");

        var planBuildings = em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(request.PlanEntity, true);
        var planSpawns = em.GetBuffer<InitialUnitsFactionSpawnEntry>(request.PlanEntity, true);
        var matchingSourceRows = new List<JObject>();
        foreach (var source in planBuildings)
        foreach (var faction in planSpawns)
        {
            string key = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(source.PrefabLookupKey.ToString());
            if (source.FactionId == request.FactionId && faction.FactionId == request.FactionId &&
                key == request.BuildingId.ToString() &&
                faction.SpawnCell.x + source.OriginOffset.x == request.PreferredOrigin.x &&
                faction.SpawnCell.y + source.OriginOffset.y == request.PreferredOrigin.y)
            {
                matchingSourceRows.Add(new JObject
                {
                    ["faction_id"] = source.FactionId,
                    ["building_id"] = key,
                    ["spawn_cell"] = new JArray(faction.SpawnCell.x, faction.SpawnCell.y),
                    ["origin_offset"] = new JArray(source.OriginOffset.x, source.OriginOffset.y),
                    ["requested_origin"] = new JArray(request.PreferredOrigin.x, request.PreferredOrigin.y)
                });
            }
        }
        if (matchingSourceRows.Count != 1)
            throw new InvalidDataException("Original startup buffers must identify exactly one source building row for the observed request");

        if (building.Definition?.Prefab == null ||
            BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(building.Definition.Prefab.name) != request.BuildingId.ToString() ||
            !building.HasOwnerFaction || building.OwnerFactionId != request.FactionId ||
            building.OriginCell.x < 0 || building.OriginCell.y < 0)
            throw new InvalidDataException("Registered runtime identity does not agree with the directly observed source request");

        rows.Add(new JObject
        {
            ["binding"] = "DirectRequestContextToRegistrationReturn",
            ["request_id"] = request.RequestId,
            ["plan_entity"] = $"{request.PlanEntity.Index}:{request.PlanEntity.Version}",
            ["source_config_rows"] = new JArray(matchingSourceRows),
            ["request_building_id"] = request.BuildingId.ToString(),
            ["request_faction_id"] = request.FactionId,
            ["request_preferred_origin"] = new JArray(request.PreferredOrigin.x, request.PreferredOrigin.y),
            ["runtime_building_id"] = building.Id,
            ["runtime_definition_id"] = building.Definition.Prefab.name,
            ["runtime_definition_key"] = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(building.Definition.Prefab.name),
            ["runtime_faction_id"] = building.OwnerFactionId,
            ["runtime_origin"] = new JArray(building.OriginCell.x, building.OriginCell.y),
            ["combat_entity"] = $"{building.CombatEntity.Index}:{building.CombatEntity.Version}"
        });
    }

    internal JObject Snapshot()
    {
        if (!em.Exists(sessionEntity)) throw new InvalidDataException("Source session was destroyed during direct birth capture");
        var match = em.GetComponentData<SkirmishMatchState>(sessionEntity);
        if (match.SessionId.ToString() != sessionId || match.ScenarioIndex != scenarioIndex ||
            (byte)match.Phase != phase || em.WorldUnmanaged.SequenceNumber != worldSequence)
            throw new InvalidDataException("Source session/world identity changed during direct birth capture");
        return new JObject
        {
            ["scope"] = "OptInDirectOriginalLegacyRequestToRegistrationIdentity",
            ["read_only"] = true,
            ["session_id"] = sessionId,
            ["scenario_index"] = scenarioIndex,
            ["seed"] = seed,
            ["phase"] = phase,
            ["world_sequence_number"] = worldSequence,
            ["session_entity"] = $"{sessionEntity.Index}:{sessionEntity.Version}",
            ["rows"] = new JArray(rows.Select(x => (JObject)x.DeepClone())),
            ["expected_fuel_bladder_source_rows"] = expectedFuelSourceRows,
            ["fuel_bladder_bindings"] = rows.Count(x => (string)x["request_building_id"] == "building_fuel_bladder" &&
                (string)x["runtime_definition_key"] == "building_fuel_bladder"),
            ["fuel_bladder_bindings_match_source_count"] = expectedFuelSourceRows >= 0 &&
                rows.Count(x => (string)x["request_building_id"] == "building_fuel_bladder" &&
                    (string)x["runtime_definition_key"] == "building_fuel_bladder") == expectedFuelSourceRows,
            ["association_basis"] = "Observer receives exact BuildingRuntimeSpawnRequest in request placement context and the RuntimeBuildingEntity returned by registration on the same call stack"
        };
    }

    int CountFuelSourceRows(Entity plan)
    {
        int count = 0;
        var source = em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(plan, true);
        foreach (var row in source)
            if (BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(row.PrefabLookupKey.ToString()) == "building_fuel_bladder")
                count++;
        return count;
    }
}
