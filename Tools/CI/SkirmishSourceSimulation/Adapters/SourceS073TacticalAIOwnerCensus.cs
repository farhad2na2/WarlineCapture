using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

// Opt-in, read-only inventory of the actual retained S073 tactical-AI owner inputs.
internal static class SourceS073TacticalAIOwnerCensus
{
    const string EnableKey = "WARLINE_S073_TACTICAL_AI_OWNER_CENSUS";
    const string InventoryPath = "Logs/SkirmishProduction/s073-ai-owner-dependency-inventory-20261101-r1.json";
    static string Sha(string path)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    static EntityQuery Query(EntityManager em, string name)
    {
        Type type = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType("Game.Components." + name, false) ??
                   (name == "LocalTransform" ? assembly.GetType("Unity.Transforms.LocalTransform", false) : null);
            if (type != null) break;
        }
        if (type == null) throw new InvalidDataException("S073 AI census type could not be resolved: " + name);
        return em.CreateEntityQuery(type);
    }
    static JObject Ref(EntityManager em, Entity source, Entity target)
    {
        bool nullRef = target == Entity.Null;
        bool exists = !nullRef && em.Exists(target);
        return new JObject { ["source"] = new JArray(source.Index, source.Version),
            ["target"] = new JArray(target.Index, target.Version), ["null"] = nullRef,
            ["target_exists"] = exists, ["ownership"] = exists ? "reference_resolves_in_retained_source_world" : nullRef ? "unset" : "unresolved_in_retained_source_world" };
    }
    internal static JObject Capture(EntityManager em, Entity session, string outputDirectory)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(EnableKey), "1", StringComparison.Ordinal)) return null;
        string inventory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), InventoryPath));
        if (!File.Exists(inventory)) throw new InvalidDataException("S073 tactical AI census inventory is missing");
        JObject inv = JObject.Parse(File.ReadAllText(inventory));
        if ((string)inv["kind"] != "S073TacticalAIOwnerDependencyInventory" ||
            (string)inv["status"] != "SourceInventoryVerified_RuntimeWorldNotObserved")
            throw new InvalidDataException("Unexpected S073 tactical AI inventory identity/status");
        if (!em.Exists(session)) throw new InvalidDataException("S073 tactical AI census session entity must remain live");
        SkirmishMatchState sessionBefore = em.GetComponentData<SkirmishMatchState>(session);
        using (var gameplayQuery = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
        {
            if (gameplayQuery.CalculateEntityCount() != 1 || sessionBefore.Seed != 104729 ||
                sessionBefore.Phase != SkirmishPhase.Preparing || sessionBefore.Outcome != SkirmishOutcome.None)
                throw new InvalidDataException("S073 tactical AI census requires unique gameplay state and the retained Preparing session");
            RuntimeGameplayStateComponent gameplayBefore = gameplayQuery.GetSingleton<RuntimeGameplayStateComponent>();
            if (gameplayBefore.PlayRequested != 1 || gameplayBefore.SimulationActive != 0)
                throw new InvalidDataException("S073 tactical AI census requires PlayRequested=1 and SimulationActive=0 at the retained boundary");
        }
        RuntimeGameplayStateComponent gameplaySnapshot;
        using (var gameplayQuery = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
            gameplaySnapshot = gameplayQuery.GetSingleton<RuntimeGameplayStateComponent>();
        var counts = new JObject();
        string[] types = { "RuntimeGameplayStateComponent", "AISquadPlan", "Faction", "UnitGrid", "SkirmishMatchState",
            "FactionControlConfigTag", "AISquad", "AISquadMember", "AISquadUnit", "FactionControlEntry",
            "AIDiagnosticLogQueueComponent", "AIDiagnosticLogComponent", "AITargetPrioritySetting", "AIControlledTag",
            "CampaignMissionUnitRoleComponent", "UnitHealth", "UnitCombat", "UnitAttack", "LocalTransform",
            "RuntimeBuildingCombatTag", "RuntimeBuildingCombatInfo", "StaticGridBlocker", "GridBlockerSize",
            "UnitResourceHauler", "FuelLogisticsOilSourceTag", "FuelLogisticsRefineryInputTag",
            "FuelLogisticsRefineryOutputTag", "FuelLogisticsFuelStorageTag", "EngageTarget", "UnitPathRequest",
            "BaseBreachOrder", "UnitAirMovement", "GridConfig", "GridWalkable", "DynamicBlockerComponent", "DynamicOccupancyComponent" };
        foreach (string name in types)
        using (var query = Query(em, name)) counts[name] = query.CalculateEntityCount();

        var references = new JArray();
        using (var squads = em.CreateEntityQuery(ComponentType.ReadOnly<AISquad>()))
        using (var entities = squads.ToEntityArray(Allocator.Temp))
            foreach (Entity e in entities) references.Add(new JObject { ["edge"] = "AISquad.TargetEntity", ["owner_use"] = "AITargetingSystem writes; AICombatOrderSystem reads/validates", ["reference"] = Ref(em, e, em.GetComponentData<AISquad>(e).TargetEntity) });
        using (var members = em.CreateEntityQuery(ComponentType.ReadOnly<AISquadMember>()))
        using (var entities = members.ToEntityArray(Allocator.Temp))
            foreach (Entity e in entities) references.Add(new JObject { ["edge"] = "AISquadMember.Squad", ["owner_use"] = "AISquadSystem writes; candidate filter reads component presence", ["reference"] = Ref(em, e, em.GetComponentData<AISquadMember>(e).Squad) });
        using (var targets = em.CreateEntityQuery(ComponentType.ReadOnly<EngageTarget>()))
        using (var entities = targets.ToEntityArray(Allocator.Temp))
            foreach (Entity e in entities) references.Add(new JObject { ["edge"] = "EngageTarget.Target", ["owner_use"] = "AICombatOrderSystem writes/removes; filters and refresh checks read", ["reference"] = Ref(em, e, em.GetComponentData<EngageTarget>(e).Target) });
        using (var orders = em.CreateEntityQuery(ComponentType.ReadOnly<BaseBreachOrder>()))
        using (var entities = orders.ToEntityArray(Allocator.Temp))
            foreach (Entity e in entities) { var order = em.GetComponentData<BaseBreachOrder>(e);
                references.Add(new JObject { ["edge"] = "BaseBreachOrder.FinalTarget", ["owner_use"] = "AICombatOrderSystem writes and reads", ["reference"] = Ref(em, e, order.FinalTarget) });
                references.Add(new JObject { ["edge"] = "BaseBreachOrder.BreachTarget", ["owner_use"] = "AICombatOrderSystem writes and reads", ["reference"] = Ref(em, e, order.BreachTarget) }); }

        var buffers = new JObject();
        using (var q = em.CreateEntityQuery(ComponentType.ReadOnly<AISquadUnit>()))
        using (var es = q.ToEntityArray(Allocator.Temp)) { int n = 0, refs = 0, unresolved = 0;
            foreach (Entity e in es) { var b = em.GetBuffer<AISquadUnit>(e, true); n += b.Length; foreach (var x in b) { refs++; if (!em.Exists(x.Unit)) unresolved++; } }
            buffers["AISquadUnit"] = new JObject { ["buffer_entities"] = es.Length, ["elements"] = n, ["entity_references"] = refs, ["unresolved_references"] = unresolved, ["owner_use"] = "AISquadSystem creates/populates; AICombatOrderSystem reads per squad" }; }
        using (var q = em.CreateEntityQuery(ComponentType.ReadOnly<FactionControlEntry>()))
        using (var es = q.ToEntityArray(Allocator.Temp)) { int n = 0; foreach (Entity e in es) n += em.GetBuffer<FactionControlEntry>(e, true).Length;
            buffers["FactionControlEntry"] = new JObject { ["buffer_entities"] = es.Length, ["elements"] = n, ["owner_use"] = "AISquadSystem and AICombatOrderSystem read when singleton is present" }; }
        using (var q = em.CreateEntityQuery(ComponentType.ReadOnly<AIDiagnosticLogComponent>()))
        using (var es = q.ToEntityArray(Allocator.Temp)) { int n = 0; foreach (Entity e in es) n += em.GetBuffer<AIDiagnosticLogComponent>(e, true).Length;
            buffers["AIDiagnosticLogComponent"] = new JObject { ["buffer_entities"] = es.Length, ["elements"] = n, ["owner_use"] = "AI owners append when verbose AI logs are enabled" }; }
        using (var q = em.CreateEntityQuery(ComponentType.ReadOnly<GridWalkable>()))
        using (var es = q.ToEntityArray(Allocator.Temp)) { int n = 0; foreach (Entity e in es) n += em.GetBuffer<GridWalkable>(e, true).Length;
            buffers["GridWalkable"] = new JObject { ["buffer_entities"] = es.Length, ["elements"] = n, ["owner_use"] = "AICombatOrderSystem reads full grid for breach approach" }; }

        SkirmishMatchState sessionAfter = em.GetComponentData<SkirmishMatchState>(session);
        RuntimeGameplayStateComponent gameplayAfter;
        using (var gameplayQuery = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
        {
            if (gameplayQuery.CalculateEntityCount() != 1)
                throw new InvalidDataException("S073 tactical AI census gameplay-state singleton count changed during observation");
            gameplayAfter = gameplayQuery.GetSingleton<RuntimeGameplayStateComponent>();
        }
        bool guardedStateUnchanged = sessionBefore.Phase == sessionAfter.Phase && sessionBefore.Seed == sessionAfter.Seed &&
            sessionBefore.Outcome == sessionAfter.Outcome && gameplaySnapshot.PlayRequested == gameplayAfter.PlayRequested &&
            gameplaySnapshot.SimulationActive == gameplayAfter.SimulationActive;
        if (!guardedStateUnchanged || sessionAfter.Phase != SkirmishPhase.Preparing || sessionAfter.Outcome != SkirmishOutcome.None ||
            gameplayAfter.PlayRequested != 1 || gameplayAfter.SimulationActive != 0)
            throw new InvalidDataException("S073 tactical AI census must leave Preparing and gameplay admission flags unchanged");

        var row = new JObject { ["kind"] = "S073TacticalAIOwnerInputCensus", ["schema_version"] = 1,
            ["inventory_path"] = InventoryPath, ["inventory_sha256"] = Sha(inventory), ["session_entity"] = new JArray(session.Index, session.Version),
            ["guarded_source_state_before"] = new JObject { ["phase"] = sessionBefore.Phase.ToString(), ["seed"] = sessionBefore.Seed,
                ["outcome"] = sessionBefore.Outcome.ToString(), ["play_requested"] = gameplaySnapshot.PlayRequested != 0,
                ["simulation_active"] = gameplaySnapshot.SimulationActive != 0 },
            ["guarded_source_state_after"] = new JObject { ["phase"] = sessionAfter.Phase.ToString(), ["seed"] = sessionAfter.Seed,
                ["outcome"] = sessionAfter.Outcome.ToString(), ["play_requested"] = gameplayAfter.PlayRequested != 0,
                ["simulation_active"] = gameplayAfter.SimulationActive != 0 },
            ["guarded_source_state_unchanged"] = guardedStateUnchanged,
            ["entity_component_presence_counts"] = counts, ["buffer_counts"] = buffers, ["entity_reference_edges"] = references,
            ["owner_ownership_basis"] = "Inventory owner_use annotations plus direct EntityManager observations at retained source boundary; target resolution means entity exists in this source world",
            ["capture_boundary"] = "Retained S073 legacy normal run after original startup setup; before census serialization",
            ["world_mutated"] = false, ["owner_systems_invoked"] = false, ["native_state_imported"] = false,
            ["mission_admission_claim"] = false, ["scheduler_parity_claim"] = false, ["mission_ready"] = false,
            ["scope"] = "Observed component presence, buffer lengths and declared Entity reference resolution only; does not prove complete semantic owner closure or runtime owner behavior" };
        string path = Path.Combine(Path.GetFullPath(outputDirectory), "s073-tactical-ai-owner-census.json");
        using (var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) using (var writer = new StreamWriter(fs)) writer.Write(row.ToString(Formatting.Indented) + "\n");
        row["artifact_path"] = path; row["artifact_sha256"] = Sha(path);
        return row;
    }
}
