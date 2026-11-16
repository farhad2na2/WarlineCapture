using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Composition;
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

internal static class SourceS073LegacyNormalRun
{
    private const string SourceTransportDiagnosticOptIn = "WARLINE_S073_SOURCE_TRANSPORT_DIAGNOSTIC";
    private const string AriaCapabilityCensusOptIn = "WARLINE_S073_ARIA_CAPABILITY_CENSUS";
#if WARLINE_SOURCE_S073_ORIGINAL_ARIA_PROBE
    private const string OriginalAriaStepProbeOptIn = "WARLINE_S073_ORIGINAL_ARIA_STEP_PROBE";
#endif
    // The source-host runtime owns no presentation Camera; map camera records
    // are read as inert metadata and never instantiated or applied here.
    private static readonly SourceS073HostAbsentPresentationCameraCapability HostCameraOwnership =
        SourceS073HostAbsentPresentationCameraCapability.IssueForSourceHost();
    static bool CorroboratesDiagnosticTerminal(EntityManager em, SkirmishMatchState session,
        string originalSessionId, int requestedTickBudget, JObject scheduler)
    {
        string outcome = session.Outcome.ToString();
        int ticks = (int?)scheduler?["ticks_executed"] ?? 0;
        JToken schedulerBudget = scheduler?["tick_budget"];
        bool budgetMatches = requestedTickBudget == 0
            ? schedulerBudget?.Type == JTokenType.String && (string)schedulerBudget == "until_original_terminal_outcome"
            : requestedTickBudget > 0 && schedulerBudget?.Type == JTokenType.Integer &&
                (int?)schedulerBudget == requestedTickBudget && ticks <= requestedTickBudget;
        return em != null && em.World != null && em.World.IsCreated && scheduler != null &&
            (string)scheduler["status"] == "RecurringOriginalOwnerTicksExecuted" &&
            budgetMatches && ticks > 0 &&
            session.Phase == SkirmishPhase.Finished &&
            (session.Outcome == SkirmishOutcome.Victory || session.Outcome == SkirmishOutcome.Defeat ||
                session.Outcome == SkirmishOutcome.Draw) &&
            !string.IsNullOrEmpty(originalSessionId) && session.SessionId.ToString() == originalSessionId &&
            (string)scheduler["session_id"] == originalSessionId &&
            (string)scheduler["terminal_phase"] == session.Phase.ToString() &&
            (string)scheduler["terminal_outcome"] == outcome &&
            (ulong?)scheduler["world_sequence"] == em.WorldUnmanaged.SequenceNumber;
    }

    static void TraceStage(string stage) => Console.Error.WriteLine("[S073SourceStage] " + stage + " utc=" + DateTime.UtcNow.ToString("O"));
    static void TraceDeepStage(string stage)
    {
        if (string.Equals(Environment.GetEnvironmentVariable("WARLINE_S073_DEEP_STARTUP_TRACE"), "1", StringComparison.Ordinal))
            TraceStage(stage);
    }

    private static JObject CaptureOptInOpeningFocusPrerequisiteCensus(EntityManager em, SkirmishMatchState match)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("WARLINE_S073_OPENING_FOCUS_CENSUS"), "1", StringComparison.Ordinal))
            return null;

        Entity target = match.PlayerMainBase;
        bool targetExists = target != Entity.Null && em.Exists(target);
        bool hasTransform = targetExists && em.HasComponent<LocalTransform>(target);
        bool hasAttemptOwner = targetExists && em.HasComponent<SkirmishAttemptOwnedComponent>(target);
        bool hasVisualSpawned = targetExists && em.HasComponent<SkirmishVisualSpawnedComponent>(target);
        var baseRow = new JObject
        {
            ["entity"] = target.ToString(), ["valid_entity"] = targetExists,
            ["has_local_transform"] = hasTransform,
            ["has_attempt_owned_component"] = hasAttemptOwner,
            ["has_visual_spawned_component"] = hasVisualSpawned
        };
        if (hasTransform)
        {
            var transform = em.GetComponentData<LocalTransform>(target);
            baseRow["local_transform_position"] = new JArray(transform.Position.x, transform.Position.y, transform.Position.z);
        }
        if (hasAttemptOwner)
        {
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(target);
            baseRow["attempt_owner_session_id"] = owner.SessionId.ToString();
            baseRow["attempt_owner_faction_id"] = owner.FactionId;
            baseRow["attempt_owner_stable_object_id"] = owner.StableObjectId.ToString();
        }
        if (hasVisualSpawned)
            baseRow["visual_spawned_value"] = em.GetComponentData<SkirmishVisualSpawnedComponent>(target).Spawned;

        int requestCount;
        using (var requests = em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent)))
            requestCount = requests.CalculateEntityCount();

        int nearbyEligibleOwnedUnitCount = 0;
        bool nearbyCountApplicable = hasTransform && hasAttemptOwner;
        if (nearbyCountApplicable)
        {
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(target);
            var focusPosition = em.GetComponentData<LocalTransform>(target).Position;
            using var army = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishUnitRoleComponent), typeof(UnitHealth), typeof(LocalTransform));
            using var actors = army.ToEntityArray(Allocator.Temp);
            foreach (Entity actor in actors)
            {
                var candidate = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (!candidate.SessionId.Equals(owner.SessionId) || candidate.FactionId != owner.FactionId ||
                    em.GetComponentData<UnitHealth>(actor).Current <= 0 ||
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Category == Game.Skirmish.Contracts.SkirmishPopulationCategory.LogisticsSupport)
                    continue;
                var position = em.GetComponentData<LocalTransform>(actor).Position;
                if (Unity.Mathematics.math.distancesq(position.xz, focusPosition.xz) > 80f * 80f) continue;
                nearbyEligibleOwnedUnitCount++;
            }
        }

        return new JObject
        {
            ["kind"] = "S073OpeningFocusPrerequisiteCensus",
            ["schema_version"] = 1,
            ["scope"] = "ReadOnlySameWorldSourceHostPrerequisites",
            ["match_player_main_base"] = baseRow,
            ["runtime_camera_focus_request_singleton_count"] = requestCount,
            ["shell_owner_availability"] = "unknown_not_captured_in_source_host",
            ["nearby_owned_focus_eligible_unit_count"] = nearbyEligibleOwnedUnitCount,
            ["nearby_count_applicable"] = nearbyCountApplicable,
            ["focus_invoked"] = false,
            ["native_focus_invoked"] = false,
            ["input_injected"] = false,
            ["readiness_established"] = false,
            ["mission_ready"] = false
        };
    }

    // Opt-in read-only explanation of the live legacy five-slot ARIA structure
    // mask. This observes the roster immediately before the first recurring
    // source-host step and never creates an action or changes ECS state.
    private static JObject CaptureOptInAriaCapabilityCensus(EntityManager em, SkirmishMatchState match)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(AriaCapabilityCensusOptIn), "1", StringComparison.Ordinal))
            return null;

        using var roster = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<SkirmishSquadMember>(), ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitHealth>(), ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitMove>() },
            None = new[] { ComponentType.ReadOnly<RuntimeBuildingCombatTag>(), ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<UnitTransportPassenger>() }
        });
        using var entities = roster.ToEntityArray(Allocator.Temp);
        var units = new JArray();
        int structureMask = 0;
        foreach (Entity unit in entities)
        {
            if (em.GetComponentData<Faction>(unit).Id != FactionIdentity.PlayerFactionId ||
                em.GetComponentData<UnitHealth>(unit).Current <= 0) continue;
            var member = em.GetComponentData<SkirmishSquadMember>(unit);
            bool hasAttack = em.HasComponent<UnitAttack>(unit);
            bool hasCombat = em.HasComponent<UnitCombat>(unit);
            bool hasPolicy = em.HasComponent<CombatTargetPolicy>(unit);
            UnitAttack attack = hasAttack ? em.GetComponentData<UnitAttack>(unit) : default;
            UnitCombat combat = hasCombat ? em.GetComponentData<UnitCombat>(unit) : default;
            CombatTargetPolicy policy = hasPolicy ? em.GetComponentData<CombatTargetPolicy>(unit) : default;
            bool enemyExistsForUnit = match.EnemyMainBase != Entity.Null && em.Exists(match.EnemyMainBase);
            bool structureEligible = member.Slot <= 4 && hasAttack && attack.Damage > 0 && hasCombat &&
                combat.CanAttack != 0 && hasPolicy && (policy.AllowedTargets & CombatTargetDomain.Structure) != 0;
            if (structureEligible) structureMask |= 1 << member.Slot;
            UnitGrid grid = em.GetComponentData<UnitGrid>(unit);
            bool hasAttemptIdentity = em.HasComponent<SkirmishAttemptOwnedComponent>(unit);
            SkirmishAttemptOwnedComponent attemptIdentity = hasAttemptIdentity
                ? em.GetComponentData<SkirmishAttemptOwnedComponent>(unit) : default;
            var row = new JObject
            {
                ["entity"] = unit.ToString(), ["cell"] = new JArray(grid.Cell.x, grid.Cell.y),
                ["unit_key"] = em.HasComponent<UnitSourcePrefabKey>(unit) ? em.GetComponentData<UnitSourcePrefabKey>(unit).Value.ToString() : null,
                ["slot"] = member.Slot, ["has_unit_attack"] = hasAttack,
                ["stable_object_id"] = hasAttemptIdentity ? attemptIdentity.StableObjectId.ToString() : null,
                ["damage"] = hasAttack ? attack.Damage : (int?)null,
                ["has_unit_combat"] = hasCombat,
                ["can_attack"] = hasCombat ? combat.CanAttack != 0 : (bool?)null,
                ["combat_target_policy_present"] = hasPolicy,
                ["combat_target_policy_domain"] = hasPolicy ? policy.Domain.ToString() : null,
                ["combat_target_policy_allowed"] = hasPolicy ? policy.AllowedTargets.ToString() : null,
                ["original_utility_allows_enemy_main_base"] = enemyExistsForUnit
                    ? CombatTargetPolicyUtility.Allows(em, unit, match.EnemyMainBase) : (bool?)null,
                ["aria_structure_mask_eligible"] = structureEligible
            };
            units.Add(row);
        }

        Entity enemy = match.EnemyMainBase;
        bool enemyExists = enemy != Entity.Null && em.Exists(enemy);
        bool enemyHasPolicy = enemyExists && em.HasComponent<CombatTargetPolicy>(enemy);
        CombatTargetPolicy enemyPolicy = enemyHasPolicy ? em.GetComponentData<CombatTargetPolicy>(enemy) : default;
        var target = new JObject
        {
            ["entity"] = enemy.ToString(), ["exists"] = enemyExists,
            ["combat_target_policy_present"] = enemyHasPolicy,
            ["domain"] = enemyHasPolicy ? enemyPolicy.Domain.ToString() : null,
            ["allowed_targets"] = enemyHasPolicy ? enemyPolicy.AllowedTargets.ToString() : null
        };
        return new JObject
        {
            ["kind"] = "S073AriaFirstStepCapabilityCensus", ["schema_version"] = 1,
            ["scope"] = "ReadOnlySameWorldSourceDiagnosticBeforeFirstRecurringAriaStep",
            ["player_squad_units"] = units, ["aria_structure_mask_from_observed_eligibility"] = structureMask,
            ["enemy_main_base_target"] = target, ["actions_synthesized"] = false,
            ["gameplay_state_changed"] = false, ["native_readiness_established"] = false,
            ["mission_ready"] = false
        };
    }

    static string FingerprintPlacementMask(bool[] mask)
    {
        if (mask == null || mask.Length != 2048 * 1024) throw new InvalidDataException("Complete S073 placement mask required");
        var bytes = new byte[(mask.Length + 7) / 8];
        for (int i = 0; i < mask.Length; i++) if (mask[i]) bytes[i >> 3] |= (byte)(1 << (i & 7));
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(bytes).Select(value => value.ToString("x2")));
    }
    static string Sha(string path)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    static string FindUnitySceneByGuid(string projectRoot, string guid)
    {
        if (string.IsNullOrEmpty(guid) || guid.Length != 32)
            throw new InvalidDataException("Selected operation-map scene GUID required");
        var matches = new List<string>();
        foreach (string meta in Directory.EnumerateFiles(Path.Combine(projectRoot, "Assets"), "*.unity.meta", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(meta);
            if (System.Text.RegularExpressions.Regex.IsMatch(text,
                @"(?m)^guid: " + System.Text.RegularExpressions.Regex.Escape(guid) + @"\r?$"))
                matches.Add(meta.Substring(0, meta.Length - ".meta".Length));
        }
        if (matches.Count != 1)
            throw new InvalidDataException("Selected operation-map runtime scene GUID must resolve uniquely; matches=" + matches.Count);
        return matches[0];
    }

    static int ReadSceneReferencedConfigInt(string scenePath, string sceneField, string configPath, string valueField, out string configSha)
    {
        string scene = File.ReadAllText(scenePath);
        string metaPath = configPath + ".meta";
        string meta = File.ReadAllText(metaPath);
        var guidMatch = System.Text.RegularExpressions.Regex.Match(meta, @"(?m)^guid: ([0-9a-f]{32})\r?$");
        if (!guidMatch.Success || System.Text.RegularExpressions.Regex.Matches(meta, @"(?m)^guid: [0-9a-f]{32}\r?$").Count != 1)
            throw new InvalidDataException("Unique referenced runtime config GUID required: " + configPath);
        string guid = guidMatch.Groups[1].Value;
        string reference = @"(?m)^  " + System.Text.RegularExpressions.Regex.Escape(sceneField) +
            @": \{fileID: 11400000, guid: " + guid + @", type: 2\}\r?$";
        if (System.Text.RegularExpressions.Regex.Matches(scene, reference).Count != 1)
            throw new InvalidDataException("Actual Match scene config reference required: " + sceneField);
        string config = File.ReadAllText(configPath);
        var valueMatch = System.Text.RegularExpressions.Regex.Match(config,
            @"(?m)^  " + System.Text.RegularExpressions.Regex.Escape(valueField) + @": (-?[0-9]+)\r?$");
        if (!valueMatch.Success || System.Text.RegularExpressions.Regex.Matches(config,
            @"(?m)^  " + System.Text.RegularExpressions.Regex.Escape(valueField) + @": -?[0-9]+\r?$").Count != 1 ||
            !int.TryParse(valueMatch.Groups[1].Value, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out int value))
            throw new InvalidDataException("Exact integer config field required: " + valueField);
        configSha = Sha(configPath);
        return value;
    }

    static string Descriptor(JObject owner, string name)
    {
        var row = owner[name] as JObject ?? throw new InvalidDataException("Required S073 bundle descriptor: " + name);
        string path = Path.GetFullPath((string)row["path"]);
        string expected = (string)row["sha256"];
        if (expected == null || expected.Length != 64 || Sha(path) != expected)
            throw new InvalidDataException("S073 bundle input hash mismatch: " + name);
        return path;
    }

    private static JObject RunOptInMoveOrderDiagnostic(
        EntityManager em, Entity grid, Entity mapRoot, Entity session, string outputDirectory)
    {
        const string optIn = "WARLINE_S073_PATH_OWNER_DIAGNOSTIC";
        if (!string.Equals(Environment.GetEnvironmentVariable(optIn), "1", StringComparison.Ordinal))
            return null;

        using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
        using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
        using var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
        if (matches.CalculateEntityCount() != 1 || maps.CalculateEntityCount() != 1 || gameplay.CalculateEntityCount() != 1 ||
            matches.GetSingletonEntity() != session || maps.GetSingletonEntity() != mapRoot ||
            matches.GetSingleton<SkirmishMatchState>().ScenarioIndex != 3 ||
            matches.GetSingleton<SkirmishMatchState>().Seed != 104729 ||
            matches.GetSingleton<SkirmishMatchState>().Phase != SkirmishPhase.Preparing ||
            matches.GetSingleton<SkirmishMatchState>().Outcome != SkirmishOutcome.None)
            throw new InvalidDataException("Opt-in S073 source-host order diagnostic requires the retained Preparing session");

        var active = maps.GetSingleton<ActiveOperationMapComponent>();
        var metadata = maps.GetSingleton<OperationMapMetadataComponent>();
        if (active.OperationMapId.ToString() != "opmap.skirmish.industrial_basin" || active.Generation != 1 ||
            !metadata.Blob.IsCreated || metadata.Generation != 1 || metadata.PhysicalSourceValidated != 0 ||
            metadata.Blob.Value.SourceOperationMapId.ToString() != "opmap.skirmish.desert_base_01")
            throw new InvalidDataException("Opt-in S073 diagnostic cannot alter or validate physical map metadata");

        RuntimeGameplayStateComponent beforeGameplay = gameplay.GetSingleton<RuntimeGameplayStateComponent>();
        if (beforeGameplay.PlayRequested != 1 || beforeGameplay.SimulationActive != 0)
            throw new InvalidDataException("Opt-in path-owner diagnostic requires the existing prepared PlayRequested=1, SimulationActive=0 boundary");
        if (!em.Exists(grid) || !em.HasComponent<GridConfig>(grid) || !em.HasComponent<DynamicBlockerComponent>(grid) ||
            !em.HasComponent<DynamicOccupancyComponent>(grid) || !em.HasComponent<PathPoolComponent>(grid) ||
            !em.HasBuffer<GridWalkable>(grid) || !em.HasBuffer<GridRoad>(grid) ||
            !em.HasBuffer<GridRoadSidewalk>(grid) || !em.HasBuffer<GridRoadDirt>(grid))
            throw new InvalidDataException("Original pathfinding owner grid inputs are incomplete");
        GridConfig gridConfig = em.GetComponentData<GridConfig>(grid);
        DynamicBlockerComponent blockers = em.GetComponentData<DynamicBlockerComponent>(grid);
        DynamicOccupancyComponent occupancy = em.GetComponentData<DynamicOccupancyComponent>(grid);
        long cellCount = (long)gridConfig.Width * gridConfig.Height;
        if (cellCount <= 0 || cellCount > 2_097_152 || blockers.WriteInProgress != 0 ||
            blockers.Counts.Length != cellCount || blockers.Blocked.Length != cellCount ||
            occupancy.Occupied.Length != cellCount || em.GetBuffer<GridRoad>(grid).Length != cellCount ||
            em.GetBuffer<GridWalkable>(grid).Length != cellCount ||
            em.GetBuffer<GridRoadSidewalk>(grid).Length != cellCount || em.GetBuffer<GridRoadDirt>(grid).Length != cellCount)
            throw new InvalidDataException("Original path request owner grid buffers do not match its configured bounds");

        using var queueQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitMoveOrderQueueComponent>());
        if (queueQuery.CalculateEntityCount() > 1)
            throw new InvalidDataException("Unique original move-order queue required");
        Entity preexistingQueue = queueQuery.IsEmptyIgnoreFilter ? Entity.Null : queueQuery.GetSingletonEntity();
        int requestCountBefore = 0, resultCountBefore = 0;
        UnitMoveOrderQueueComponent queueBefore = default;
        if (preexistingQueue != Entity.Null)
        {
            if (!em.HasBuffer<UnitMoveOrderRequestElement>(preexistingQueue) || !em.HasBuffer<UnitMoveOrderResultElement>(preexistingQueue))
                throw new InvalidDataException("Existing original move-order queue buffers are incomplete");
            requestCountBefore = em.GetBuffer<UnitMoveOrderRequestElement>(preexistingQueue).Length;
            resultCountBefore = em.GetBuffer<UnitMoveOrderResultElement>(preexistingQueue).Length;
            queueBefore = em.GetComponentData<UnitMoveOrderQueueComponent>(preexistingQueue);
            if (requestCountBefore != 0 || resultCountBefore != 0)
                throw new InvalidDataException("Diagnostic cannot consume or evict pre-existing original move-order queue data");
        }

        using var candidates = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitFootprint>(),
                ComponentType.ReadOnly<UnitMovementBehavior>(), ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitSourcePrefabKey>() },
            None = new[] { ComponentType.ReadOnly<Prefab>(), ComponentType.ReadOnly<Disabled>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>(), ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<UnitAirMovement>(), ComponentType.ReadOnly<UnitTarget>(),
                ComponentType.ReadOnly<UnitPathRequest>(), ComponentType.ReadOnly<ManualMoveOrderTag>(),
                ComponentType.ReadOnly<UnitPathFollow>(), ComponentType.ReadOnly<UnitPathRange>(),
                ComponentType.ReadOnly<UnitPathRetryCooldown>(), ComponentType.ReadOnly<UnitLongDistanceMove>(),
                ComponentType.ReadOnly<EngageTarget>(), ComponentType.ReadOnly<AttackMoveOrder>(),
                ComponentType.ReadOnly<HoldPositionOrderTag>(), ComponentType.ReadOnly<AutoWanderMoveTag>(),
                ComponentType.ReadOnly<BaseBreachOrder>(), ComponentType.ReadOnly<UnitTransportBoardingTarget>(),
                ComponentType.ReadOnly<UnitTransportDeployOrder>(), ComponentType.ReadOnly<UnitTransportRopeDisembarkRequest>(),
                ComponentType.ReadOnly<UnitTransportAirdropRequest>(), ComponentType.ReadOnly<SupportCollectOrderComponent>(),
                ComponentType.ReadOnly<UnitResourceHaulOrder>(), ComponentType.ReadOnly<SplitFrontLauncherCommandState>() }
        });
        using var candidateEntities = candidates.ToEntityArray(Allocator.Temp);
        Entity unit = Entity.Null;
        int2 currentCell = default;
        UnitFootprint footprint = default;
        Faction faction = default;
        FixedString64Bytes sourceKey = default;
        var walkable = em.GetBuffer<GridWalkable>(grid);
        bool IsFreeWalkable(int2 origin, int2 size, bool allowCurrentOccupancy = false)
        {
            if (size.x <= 0 || size.y <= 0 || origin.x < 0 || origin.y < 0 ||
                origin.x + size.x > gridConfig.Width || origin.y + size.y > gridConfig.Height)
                return false;
            for (int y = origin.y; y < origin.y + size.y; y++)
            for (int x = origin.x; x < origin.x + size.x; x++)
            {
                int index = y * gridConfig.Width + x;
                if (walkable[index].Value == 0 || blockers.Counts[index] != 0 ||
                    blockers.Blocked.IsSet(index) || !allowCurrentOccupancy && occupancy.Occupied.IsSet(index))
                    return false;
            }
            return true;
        }
        foreach (Entity candidate in candidateEntities)
        {
            var candidateFaction = em.GetComponentData<Faction>(candidate);
            var cell = em.GetComponentData<UnitGrid>(candidate).Cell;
            var candidateFootprint = em.GetComponentData<UnitFootprint>(candidate);
            if (candidateFaction.Id != FactionIdentity.PlayerFactionId || !IsFreeWalkable(cell, candidateFootprint.Size, allowCurrentOccupancy: true))
                continue;
            unit = candidate;
            currentCell = cell;
            footprint = candidateFootprint;
            faction = candidateFaction;
            sourceKey = em.GetComponentData<UnitSourcePrefabKey>(candidate).Value;
            break;
        }
        if (unit == Entity.Null)
            throw new InvalidDataException("No real spawned player source unit with untouched movement state and path-owner inputs exists");

        int2 goal = currentCell + new int2(1, 0);
        if (!IsFreeWalkable(goal, footprint.Size))
        {
            goal = currentCell + new int2(-1, 0);
            if (!IsFreeWalkable(goal, footprint.Size))
                throw new InvalidDataException("No adjacent walkable and unoccupied source-grid target exists for the real spawned unit");
        }

        // Opt-in evidence only. Capture before injecting this harness's temporary
        // move order so the retained source-world queue and grid remain observational.
        JObject pathInputCensus = SourceS073PathInputCensus.Capture(
            em, grid, currentCell, goal, sourceKey.ToString());

        bool accepted = false;
        int requestId = UnitMoveOrderRequestSystem.EnqueueImmediateMoveOrder(em, unit, goal);
        UnitMoveOrderRequestSystem.ProcessPendingRequests(em);
        if (!UnitMoveOrderRequestSystem.TryGetResult(em, requestId, out UnitMoveOrderResultElement commandResult) ||
            commandResult.Issued == 0 || !em.HasComponent<UnitTarget>(unit) ||
            !em.GetComponentData<UnitTarget>(unit).Cell.Equals(goal) || !em.HasComponent<UnitPathRequest>(unit) ||
            !em.GetComponentData<UnitPathRequest>(unit).Goal.Equals(goal) || !em.HasComponent<ManualMoveOrderTag>(unit))
            throw new InvalidDataException("Original S073 move-order owner did not accept the diagnostic request");
        accepted = true;

        // Initialize and update the original pathfinding owner against this retained
        // source host. The movement owner is never updated, and play/simulation state
        // remains at the pre-existing prepared boundary.
        var deferred = new OwnedEndSimulationBuffers();
        var deferredEntity = em.CreateEntity();
        em.AddComponentData(deferredEntity, new EndSimulationEntityCommandBufferSystem.Singleton { Owner = deferred });
        int pathPoolCountBefore = em.GetComponentData<PathPoolComponent>(grid).Cells.Length;
        bool pathApplied = false;
        int pathOwnerUpdates = 0;
        using (var pathOwners = new OriginalGroundOwners(em, generatedFresh: true))
        {
            for (int frame = 0; frame < 8; frame++)
            {
                pathOwners.PathPhase();
                pathOwnerUpdates++;
                if (!pathOwners.HasPendingPath)
                {
                    pathApplied = em.HasComponent<UnitPathFollow>(unit) && em.HasComponent<UnitPathRange>(unit);
                    if (!em.HasComponent<UnitPathRequest>(unit)) break;
                }
            }
            if (pathOwners.HasPendingPath)
                throw new InvalidDataException("Original pathfinding owner retained pending work beyond the bounded diagnostic updates");
        }
        int pathPoolCountAfter = em.GetComponentData<PathPoolComponent>(grid).Cells.Length;
        if (!UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, unit) ||
            em.HasComponent<UnitTarget>(unit) || em.HasComponent<UnitPathRequest>(unit) ||
            em.HasComponent<ManualMoveOrderTag>(unit) || em.HasComponent<UnitPathFollow>(unit) || em.HasComponent<UnitPathRange>(unit))
            throw new InvalidDataException("Original move-order owner did not clear the temporary path diagnostic state");

        using var queuesAfter = em.CreateEntityQuery(ComponentType.ReadOnly<UnitMoveOrderQueueComponent>());
        if (queuesAfter.CalculateEntityCount() != 1)
            throw new InvalidDataException("Unique diagnostic move-order queue required after processing");
        Entity queue = queuesAfter.GetSingletonEntity();
        if (em.GetBuffer<UnitMoveOrderRequestElement>(queue).Length != 0)
            throw new InvalidDataException("Diagnostic left queued movement requests behind");
        if (preexistingQueue == Entity.Null) em.DestroyEntity(queue);
        else
        {
            var results = em.GetBuffer<UnitMoveOrderResultElement>(queue);
            while (results.Length > resultCountBefore) results.RemoveAt(results.Length - 1);
            if (results.Length != resultCountBefore || requestCountBefore != 0)
                throw new InvalidDataException("Diagnostic did not restore the pre-existing move-order queue boundary");
            em.SetComponentData(queue, queueBefore);
        }

        RuntimeGameplayStateComponent afterGameplay = gameplay.GetSingleton<RuntimeGameplayStateComponent>();
        if (beforeGameplay.PlayRequested != afterGameplay.PlayRequested ||
            beforeGameplay.SimulationActive != afterGameplay.SimulationActive ||
            em.GetComponentData<OperationMapMetadataComponent>(mapRoot).PhysicalSourceValidated != 0 ||
            matches.GetSingleton<SkirmishMatchState>().Phase != SkirmishPhase.Preparing)
            throw new InvalidDataException("Diagnostic changed gameplay admission or physical-validation state");

        var diagnostic = new JObject
        {
            ["kind"] = "S073SourceHostMoveOrderOwnerDiagnostic", ["schema_version"] = 1,
            ["status"] = pathApplied ? "PathApplied" : "PathNotApplied",
            ["scope"] = "OriginalMoveOrderRequestOwnerAndOriginalPathfindingOwner_SinglePathRequest_NoMovementOwnerUpdate_NoMissionAdmissionClaim",
            ["session_id"] = matches.GetSingleton<SkirmishMatchState>().SessionId.ToString(),
            ["operation_map_id"] = active.OperationMapId.ToString(),
            ["source_operation_map_id"] = metadata.Blob.Value.SourceOperationMapId.ToString(),
            ["source_identity_hash"] = metadata.Blob.Value.SourceIdentityHash.ToString(),
            ["source_content_hash"] = metadata.Blob.Value.SourceContentHash.ToString(),
            ["unit"] = new JObject { ["entity_index"] = unit.Index, ["entity_version"] = unit.Version,
                ["faction"] = faction.Id, ["source_prefab_key"] = sourceKey.ToString(),
                ["start_cell"] = new JArray(currentCell.x, currentCell.y), ["target_cell"] = new JArray(goal.x, goal.y) },
            ["source_world_path_input_census"] = pathInputCensus,
            ["original_move_order_owner_invoked"] = true, ["temporary_order_accepted"] = accepted,
            ["original_pathfinding_owner_invoked"] = true, ["pathfinding_owner_updates"] = pathOwnerUpdates,
            ["path_request_observed"] = true, ["path_applied"] = pathApplied,
            ["path_pool_cells_before"] = pathPoolCountBefore, ["path_pool_cells_after"] = pathPoolCountAfter,
            ["temporary_order_and_path_state_cleared_by_original_owner"] = true,
            ["movement_owner_invoked"] = false, ["movement_observed"] = false,
            ["play_requested_before"] = beforeGameplay.PlayRequested,
            ["play_requested_after"] = afterGameplay.PlayRequested,
            ["simulation_active_before"] = beforeGameplay.SimulationActive,
            ["simulation_active_after"] = afterGameplay.SimulationActive,
            ["physical_source_validated"] = false,
            ["scheduler_or_mission_admission_claimed"] = false,
            ["native_player_input_observed"] = false, ["mission_admission_claimed"] = false,
            ["mission_ready"] = false
        };
        string path = Path.Combine(outputDirectory, "s073-move-order-owner-diagnostic.json");
        using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(file)) writer.Write(diagnostic.ToString(Formatting.Indented) + "\n");
        diagnostic["output_path"] = path;
        diagnostic["output_sha256"] = Sha(path);
        Console.WriteLine("[S073MoveOrderOwnerDiagnostic] result=Recorded pathfindingOwnerInvoked=1 pathApplied=" +
            (pathApplied ? "1" : "0") + " movementOwnerInvoked=0 physicalSourceValidated=0");
        return diagnostic;
    }

    internal static int Run(JObject run)
    {
        var workerTotalWatch = Stopwatch.StartNew();
        var workerSetupWatch = Stopwatch.StartNew();
        double? workerSetupWallSeconds = null;
        bool schedulerInvocationAttempted = false;
        JArray sourceConfiguredDefinitionProductionSlots = null;
        JObject sourceHostAdmission = null;
        JObject ariaStep = null;
        string ariaStepFailure = null;
        JObject legacyOwnerSchedulerTick = null;
        JObject sourceTransportDiagnosticProvenance = null;
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL || WARLINE_SOURCE_S073_RESOURCE_CENSUS
        JArray resourceEconomyRuntimeCensus = null;
#endif
        bool diagnosticSourceActivation = false;
        JToken sourceTransportDiagnosticOption = run["source_host_transport_diagnostic"];
        if (sourceTransportDiagnosticOption != null && sourceTransportDiagnosticOption.Type != JTokenType.Boolean)
            throw new InvalidDataException("Source transport diagnostic selector must be an explicit boolean");
        bool sourceTransportDiagnosticRequested = (bool?)sourceTransportDiagnosticOption == true;
        bool sourceTransportDiagnosticEnvironmentOptIn = string.Equals(
            Environment.GetEnvironmentVariable(SourceTransportDiagnosticOptIn), "1", StringComparison.Ordinal);
        if (sourceTransportDiagnosticRequested != sourceTransportDiagnosticEnvironmentOptIn)
            throw new InvalidDataException("Source transport diagnostic requires matching explicit run-profile and environment opt-ins");
        // An omitted max_ticks is a normal mission run: continue until the original
        // match rules report a terminal phase/outcome. Explicit diagnostic runs may
        // supply a bounded prefix.
        int schedulerTickBudget = 0;
        if (run["max_ticks"] != null)
        {
            if (run["max_ticks"].Type != JTokenType.Integer)
                throw new InvalidDataException("S073 scheduler tick budget must be an integer");
            schedulerTickBudget = (int)run["max_ticks"];
        }
        if (schedulerTickBudget < 0 || schedulerTickBudget > 3600 || run["max_ticks"] != null && schedulerTickBudget == 0)
            throw new InvalidDataException("S073 explicit scheduler tick budget must be within the bounded owner range 1..3600");
        string directory = Path.GetFullPath((string)run["output_directory"]);
        string bundlePath = Descriptor(run, "s073_bundle");
        JObject bundle = JObject.Parse(File.ReadAllText(bundlePath));
        string aiMode = (string)run["s073_source_mode"] ?? "qualified-native-observation";
        bool currentSourceDiagnostic = aiMode == "current-source-unverified-native-parity";
        JToken diagnosticAdmissionOption = run["s073_source_host_diagnostic_admission"];
        if (diagnosticAdmissionOption != null && diagnosticAdmissionOption.Type != JTokenType.Boolean)
            throw new InvalidDataException("S073 source-host diagnostic admission selector must be an explicit boolean");
        bool diagnosticAdmissionRequested = (bool?)diagnosticAdmissionOption == true;
        JToken historicalInputOption = run["s073_historical_input_diagnostic"];
        if (historicalInputOption != null && historicalInputOption.Type != JTokenType.Boolean)
            throw new InvalidDataException("S073 historical input diagnostic selector must be a boolean");
        bool historicalInputDiagnostic = (bool?)historicalInputOption == true;
        if (historicalInputDiagnostic && (!currentSourceDiagnostic || (!diagnosticAdmissionRequested && schedulerTickBudget == 0)))
            throw new InvalidDataException("S073 historical input requires current-source and a bounded preparing or admitted source-host diagnostic");
        if (sourceTransportDiagnosticRequested && (!currentSourceDiagnostic || !diagnosticAdmissionRequested))
            throw new InvalidDataException("Source transport diagnostic requires the explicit source-host diagnostic profile and admission");
        if (aiMode != "qualified-native-observation" && !currentSourceDiagnostic)
            throw new InvalidDataException("Unsupported S073 AI startup validation mode");
        if ((int?)bundle["schema_version"] != 1 || (string)bundle["kind"] != "S073LegacyStartupBundle" ||
            (string)bundle["mission"] != "S073" || (int?)bundle["seed"] != 104729)
            throw new InvalidDataException("Exact finite S073 legacy startup bundle required");
        string snapshotPath = Descriptor(bundle, "prefab_capture");
        string identityPath = Descriptor(bundle, "source_identity");
        string configPath = Descriptor(bundle, "legacy_config");
        string launchPath = Descriptor(bundle, "launch_capture");
        string selectedMapPath = Descriptor(bundle, "selected_map");
        string physicalPath = Descriptor(bundle, "campaign_physical");
        string waterPath = Descriptor(bundle, "campaign_water");
        string startupPath = Descriptor(bundle, "campaign_startup");
        string maskPath = Descriptor(bundle, "authored_mask");
        string missionScenePath = Descriptor(bundle, "mission_scene");
        string productionPath = Descriptor(bundle, "production_prefabs");
        string definitionsPath = Descriptor(bundle, "building_definitions");
        string placementConfigPath = Descriptor(bundle, "building_placement_config");
        // Retained catalog captures are tied to a specific InitialForces source
        // closure. Current-source diagnostics build the same read models from
        // current serialized assets through their original owner semantics.
        string nativeCatalogReceipt = currentSourceDiagnostic || bundle["native_configured_catalog_receipt"] == null ? null : Descriptor(bundle, "native_configured_catalog_receipt");
        string nativeUnitReceipt = currentSourceDiagnostic || bundle["native_configured_unit_receipt"] == null ? null : Descriptor(bundle, "native_configured_unit_receipt");
        string nativeAIReceipt = currentSourceDiagnostic || bundle["native_ai_snapshot_receipt"] == null ? null : Descriptor(bundle, "native_ai_snapshot_receipt");
        string aiGraph = currentSourceDiagnostic || bundle["native_ai_source_graph"] == null ? null : Descriptor(bundle, "native_ai_source_graph");
        string exchangeGraph = currentSourceDiagnostic || bundle["native_exchange_source_graph"] == null ? null : Descriptor(bundle, "native_exchange_source_graph");
        string nativeSceneBinding = bundle["native_scene_binding_receipt"] == null ? null : Descriptor(bundle, "native_scene_binding_receipt");
        string[] nativeBlockerNames = { "native_water_capture_receipt", "native_water_capture_snapshot", "native_water_capture_mask", "native_water_startup_parity_receipt" };
        JArray ignoredNativeBlockerInputs = currentSourceDiagnostic
            ? new JArray(nativeBlockerNames.Where(name => bundle[name] != null)) : new JArray();
        string nativeWaterReceipt = currentSourceDiagnostic || bundle["native_water_capture_receipt"] == null ? null : Descriptor(bundle, "native_water_capture_receipt");
        string nativeWaterSnapshot = currentSourceDiagnostic || bundle["native_water_capture_snapshot"] == null ? null : Descriptor(bundle, "native_water_capture_snapshot");
        string nativeWaterMask = currentSourceDiagnostic || bundle["native_water_capture_mask"] == null ? null : Descriptor(bundle, "native_water_capture_mask");
        string nativeWaterParity = currentSourceDiagnostic || bundle["native_water_startup_parity_receipt"] == null ? null : Descriptor(bundle, "native_water_startup_parity_receipt");
        if (((nativeWaterReceipt == null ? 0 : 1) + (nativeWaterSnapshot == null ? 0 : 1) + (nativeWaterMask == null ? 0 : 1) != 0 &&
             (nativeWaterReceipt == null || nativeWaterSnapshot == null || nativeWaterMask == null)) ||
            (nativeWaterParity != null && nativeWaterMask == null))
            throw new InvalidDataException("S073 native blocker-mask evidence descriptors must be complete when supplied");
        if ((nativeAIReceipt == null) != (aiGraph == null))
            throw new InvalidDataException("Native AI observation and authored AI graph must be supplied together");
        if (exchangeGraph != null && nativeAIReceipt == null)
            throw new InvalidDataException("Exchange startup requires the source AI startup inputs");
        if (nativeUnitReceipt != null && nativeCatalogReceipt == null)
            throw new InvalidDataException("Qualified building metadata is required before unit metadata publication");
        string relocationPath = bundle["capture_relocation"] == null ? null : Descriptor(bundle, "capture_relocation");
        string physicalBlockerPath = bundle["streamed_physical_blocker_capture"] == null ? null : Descriptor(bundle, "streamed_physical_blocker_capture");
        if (physicalBlockerPath != null && Sha(physicalBlockerPath) != "1f375a666f474a73d2aaac5c0b1930a774b117349a1e074f618ecbbd57b01bd7")
            throw new InvalidDataException("Only the native-compared S073 source capture is supported for the experimental physical blocker projection");
        JArray ignoredNativeRoadInputs = currentSourceDiagnostic && bundle["native_physical_road_receipt"] != null
            ? new JArray("native_physical_road_receipt", "native_physical_road_mask", "native_physical_sidewalk_mask") : new JArray();
        byte[] capturedSurfaceRoads = null, capturedSidewalks = null;
        JObject physicalRoadReceipt = null;
        if (bundle["native_physical_road_receipt"] != null && !currentSourceDiagnostic)
        {
            string receiptPath = Descriptor(bundle, "native_physical_road_receipt");
            string roadPath = Descriptor(bundle, "native_physical_road_mask");
            string sidewalkPath = Descriptor(bundle, "native_physical_sidewalk_mask");
            var receipt = JObject.Parse(File.ReadAllText(receiptPath)); physicalRoadReceipt = receipt;
            if ((string)receipt["kind"] != "S073PhysicalRoadMaskCaptureReceipt" || (string)receipt["validation_status"] != "Passed" ||
                (int?)receipt["wrapper_exit_code"] != 0 || (bool?)receipt["required_markers_present"] != true ||
                (string)receipt["source_identity_hash"] != "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900" ||
                (string)receipt["source_content_hash"] != "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c" ||
                (string)receipt["surface_mask_sha256"] != Sha(roadPath) || (string)receipt["sidewalk_mask_sha256"] != Sha(sidewalkPath) ||
                (bool?)receipt["mission_ready"] != false || (bool?)receipt["campaign_gameplay_state_copied"] != false)
                throw new InvalidDataException("Exact validated physical-road capture provenance required");
            capturedSurfaceRoads = File.ReadAllBytes(roadPath); capturedSidewalks = File.ReadAllBytes(sidewalkPath);
            if (capturedSurfaceRoads.Length != 2048 * 1024 || capturedSidewalks.Length != capturedSurfaceRoads.Length ||
                capturedSurfaceRoads.Any(v => v > 1) || capturedSidewalks.Any(v => v > 1))
                throw new InvalidDataException("Exact row-major physical road and sidewalk masks required");
        }
        if (Sha(maskPath + ".json") != Sha(launchPath))
            throw new InvalidDataException("S073 authored mask must retain its exact native launch manifest");
        Descriptor(bundle, "base_graph");
        JObject config = JObject.Parse(File.ReadAllText(configPath));
        JObject identity = JObject.Parse(File.ReadAllText(identityPath));
        JObject snapshot = JObject.Parse(File.ReadAllText(snapshotPath));
        JObject launch = JObject.Parse(File.ReadAllText(launchPath));
        string sourceGraphPath = Descriptor(run, "selected_operation_map_graph");
        if ((string)config["kind"] != "S073LegacyInitialSpawnExternalData" ||
            (string)config["mission"] != "S073" ||
            (string)config["source_artifacts"]?["name_readback_sha256"] != Sha(identityPath) ||
            (string)config["source_artifacts"]?["prefab_capture_sha256"] != Sha(snapshotPath) ||
            (string)identity["kind"] != "S073LegacyPrefabSourceNameReadback" ||
            (string)identity["authority"] != "AssetDatabase exact GUID/local-fileID GameObject names; source data only" ||
            identity["unity_world_allocated"]?.Value<bool>() != false ||
            identity["mission_world_allocated"]?.Value<bool>() != false ||
            (string)launch["kind"] != "S073LiveOriginalAuthoredBuildingMask" ||
            (int?)launch["scenario_index"] != SkirmishPresetConfig.IndustrialBasinScenarioIndex ||
            (int?)launch["seed"] != QuickGameConfig.Defaults.MapSeed ||
            (string)launch["selected_source_path"] != "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset")
            throw new InvalidDataException("S073 source-readback lineage or retained native launch identity mismatch");

        string repo = Path.GetFullPath((string)bundle["source_repository_root"]);
        string assetRepo = Path.GetFullPath((string)bundle["asset_repository_root"]);
        if (physicalRoadReceipt != null)
        {
            var roadPins = physicalRoadReceipt["source_pins"] as JObject;
            const string roadScene = "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity";
            if (roadPins == null || roadPins.Count < 5 || roadPins[roadScene] == null)
                throw new InvalidDataException("Physical-road current source and runtime scene pins required");
            foreach (var pin in roadPins.Properties())
            {
                string path = Path.GetFullPath(Path.Combine(repo, pin.Name.Replace('/', Path.DirectorySeparatorChar)));
                if (!path.StartsWith(repo + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Sha(path) != (string)pin.Value)
                    throw new InvalidDataException("Physical-road source changed: " + pin.Name);
            }
        }
        string rotationPath = Descriptor(bundle, "operation_map_rotation_reference");
        const string selectedMapRelative = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset";
        if (selectedMapPath != Path.GetFullPath(Path.Combine(assetRepo, selectedMapRelative)) ||
            (string)snapshot["source_pins"]?[selectedMapRelative] != Sha(selectedMapPath))
            throw new InvalidDataException("Selected S073 map must match the retained prefab-capture source pin");

        JObject graph;
        using (var text = File.OpenText(sourceGraphPath))
        using (var reader = new JsonTextReader(text) { MaxDepth = 32, DateParseHandling = DateParseHandling.None })
            graph = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        var materializer = new SourceConfigMaterializer(graph);
        OperationMapDefinition selected = materializer.ByPath<OperationMapDefinition>(selectedMapRelative);
        SkirmishBattleCatalogConfig canonicalCatalog = materializer.ByPath<SkirmishBattleCatalogConfig>(
            "Assets/Game/Resources/SkirmishBattleCatalog.asset");
        if (selected.OperationMapId != "opmap.skirmish.industrial_basin" ||
            selected.ContentHash != "cbbaa8366cbfb5e0ad9bc6bf70a98b48503df10e3d0c5c45757295af841a7a6d" ||
            selected.SourceIdentityHash != "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900" ||
            selected.SourceBinding.SourceOperationMapId != "opmap.skirmish.desert_base_01" ||
            selected.SourceSceneReference == null)
            throw new InvalidDataException("Materialized S073 OperationMap source identity changed");
        SourceS073LegacySpawnRequestAdapter.RequireSharedPlacementSource(selected);
        var rotation = SourceOperationMapRotationReference.LoadPinned(rotationPath, Sha(rotationPath), assetRepo);
        if (!selected.TryCreatePersistentMetadataBlob(rotation, out var metadata, out string metadataError))
            throw new InvalidDataException("Original S073 operation-map metadata compiler rejected: " + metadataError);
        try
        {
            if (metadata.Value.Anchors.Length != 2 || metadata.Value.Cameras.Length != 1)
                throw new InvalidDataException("Original S073 metadata inventory changed");
        }
        finally { metadata.Dispose(); }
        var sourcePins = config["source_artifacts"]?["configuration_asset_sha256"] as JObject ??
            throw new InvalidDataException("S073 source configuration pins required");
        foreach (var pin in sourcePins.Properties())
        {
            string path = Path.GetFullPath(Path.Combine(assetRepo, pin.Name.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(assetRepo + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Sha(path) != (string)pin.Value)
                throw new InvalidDataException("S073 current authored configuration changed: " + pin.Name);
        }

        var data = config["initial_spawn"].ToObject<LegacyInitialSpawnData>();
        var preset = config["preset_values"] as JObject ?? throw new InvalidDataException("Pinned S073 preset values required");
        if ((string)preset["asset_path"] != "Assets/Game/Resources/SkirmishIndustrialBasin.asset" ||
            (int?)preset["reinforcementInfantryTarget"] < 1 || (int?)preset["attackSquadSize"] is < 4 or > 8 ||
            Sha(Path.Combine(assetRepo, "Assets/Game/Resources/SkirmishIndustrialBasin.asset")) != (string)preset["asset_sha256"])
            throw new InvalidDataException("Current S073 preset scalar identity changed");

        var em = new EntityManager();
        int next = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var buildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>();
        using var host = new AutonomousWorld(em, buildings);
        host.PrepareFreshInitialRoads(new JObject
        {
            ["kind"] = "SkirmishSourceSimulationRun", ["schema_version"] = 1,
            ["repository_root"] = repo, ["source_repository_root"] = repo,
            ["output_directory"] = directory,
            ["mission_scene"] = new JObject { ["path"] = missionScenePath, ["sha256"] = Sha(missionScenePath) }
        });
        string[] keys = data.UnitPrefabRegistryKeys.Concat(data.Factions.SelectMany(f => f.Units).Select(u => u.SourceKey))
            .Distinct(StringComparer.Ordinal).ToArray();
        var imported = GeneratedReferenceTemplateImporter.Import(em, snapshot,
            Path.GetDirectoryName(snapshotPath), keys);
        if (imported.Prefabs.Count != 4 || imported.ImportedEntities != 140 || imported.ImportedComponents != 862)
            throw new InvalidDataException("S073 initial prefab source closure changed");
        if (!SkirmishLegacyS073StartupOwner.TryQueueCapturedSelectionBeforeStartup(em,
                (int)launch["scenario_index"], (int)launch["seed"]))
            throw new InvalidOperationException("Original S073 queue owner rejected the captured selection");
        using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
        if (matches.CalculateEntityCount() != 1) throw new InvalidDataException("Unique queued S073 session required");
        var session = matches.GetSingleton<SkirmishMatchState>();
        if (session.Phase != SkirmishPhase.Queued || session.Seed != 104729)
            throw new InvalidDataException("Original S073 queue owner did not retain its queued state");
        string originalSessionId = session.SessionId.ToString();

        // The source host has not loaded the physical scene. This invokes the
        // original preparation owner in its observed startup order, but the
        // empty scenery query is not a claim about the streamed native scene.
        using (var scenery = em.CreateEntityQuery(typeof(OperationMapBuildingComponent), typeof(Faction)))
            if (scenery.CalculateEntityCount() != 0)
                throw new InvalidDataException("Unexpected pre-stream S073 scenery in source host");
        SkirmishLaunchProjection.PrepareMap(em);
        session = matches.GetSingleton<SkirmishMatchState>();
        if (session.Phase != SkirmishPhase.Preparing || session.Seed != 104729)
            throw new InvalidDataException("Original S073 PrepareMap owner did not enter Preparing");
        var startup = SkirmishLegacyS073StartupOwner.InitializeFromSource(em, data, imported.Prefabs,
            (int)preset["reinforcementInfantryTarget"], (int)preset["attackSquadSize"]);
        if (!startup.LegacyOwner.Initialized || startup.LegacyOwner.InitialBuildingEntryCount != 10)
            throw new InvalidDataException("Original S073 legacy initialization owner rejected source data");
        SkirmishLegacyLaunchOwner.ApplySeed(em);
        using (var configs = em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig)))
            if (configs.CalculateEntityCount() != 1 || configs.GetSingleton<InitialUnitsSpawnConfig>().RandomSeed != (uint)session.Seed)
                throw new InvalidDataException("Original S073 seed owner did not seed the startup configuration");

        // This publishes only the original metadata root. The native source scene
        // is not loaded here, so physical-source validation remains a separate gate.
        using var publisher = new OperationMapRuntimeBootstrapSceneSystemHelper(em, rotation);
        SourceOperationMapInitialReadiness.ResolveInitialOperationMapReadiness(false, out var ready, out var required);
        var scenarioId = new FixedString64Bytes(SkirmishPresetConfig.IndustrialBasinScenarioSetupId);
        var missionId = new FixedString64Bytes(SkirmishPresetConfig.IndustrialBasinMissionId);
        if (!publisher.TryPublish(selected, in scenarioId, in missionId, 1, ready, required, out var mapRoot, out string publishError))
            throw new InvalidOperationException("Original S073 map publication rejected: " + publishError);
        if (!publisher.TryGetPublishedRoot(em.World, out var ownedRoot) || ownedRoot != mapRoot)
            throw new InvalidDataException("Original publisher did not retain its actual S073 root");
        var activeMap = em.GetComponentData<ActiveOperationMapComponent>(mapRoot);
        var mapMetadata = em.GetComponentData<OperationMapMetadataComponent>(mapRoot);
        if (activeMap.OperationMapId.ToString() != selected.OperationMapId || activeMap.ScenarioId.ToString() != scenarioId.ToString() ||
            activeMap.MissionId.ToString() != missionId.ToString() || activeMap.Generation != 1 ||
            mapMetadata.Generation != 1 || !mapMetadata.Blob.IsCreated || mapMetadata.PhysicalSourceValidated != 0)
            throw new InvalidDataException("Published S073 source-only root identity or physical-source state changed");
        if (mapMetadata.Blob.Value.OperationMapId.ToString() != selected.OperationMapId ||
            mapMetadata.Blob.Value.SourceOperationMapId.ToString() != selected.SourceBinding.SourceOperationMapId ||
            mapMetadata.Blob.Value.SourceIdentityHash.ToString() != selected.SourceBinding.SourceIdentityHash ||
            mapMetadata.Blob.Value.SourceContentHash.ToString() != selected.SourceBinding.SourceContentHash ||
            mapMetadata.Blob.Value.Anchors.Length != 2 || mapMetadata.Blob.Value.Cameras.Length != 1)
            throw new InvalidDataException("Published S073 metadata does not retain its exact Campaign source binding");
        // Run the same data-only selection used by the original camera owner.
        // This host has not applied a Camera or advanced its input/follow lifecycle.
        ref OperationMapBlob cameraMetadata = ref mapMetadata.Blob.Value;
        bool planningCameraResolved = OperationMapCameraPoseSelectionData.TryResolve(ref cameraMetadata, true, out var planningCamera);
        bool battleCameraResolved = OperationMapCameraPoseSelectionData.TryResolve(ref cameraMetadata, false, out var battleCamera);
        JObject CameraPose(bool resolved, OperationMapCameraBlob pose) => resolved ? new JObject {
            ["id"] = pose.Id.ToString(),
            ["position"] = new JArray(pose.Position.x, pose.Position.y, pose.Position.z),
            ["rotation_xyzw"] = new JArray(pose.Rotation.value.x, pose.Rotation.value.y, pose.Rotation.value.z, pose.Rotation.value.w),
            ["field_of_view"] = pose.FieldOfView, ["orthographic_size"] = pose.OrthographicSize,
            ["orthographic"] = pose.IsOrthographic != 0
        } : null;
        var cameraMetadataSelection = new JObject {
            ["scope"] = "OriginalSharedMetadataPoseSelection_NoCameraOrLifecycle",
            ["planning_resolved"] = planningCameraResolved, ["planning"] = CameraPose(planningCameraResolved, planningCamera),
            ["battle_resolved"] = battleCameraResolved, ["battle"] = CameraPose(battleCameraResolved, battleCamera),
            ["world_camera_applied"] = false, ["live_camera_motion_validated"] = false, ["mission_ready"] = false
        };
        var reuseRequest = new CampaignMissionLaunchRequestElement
        {
            MissionId = missionId, ScenarioId = scenarioId,
            OperationMapId = new FixedString64Bytes(selected.OperationMapId)
        };
        if (!CampaignMissionOperationMapReuseUtility.TryMatchLogicalIdentity(em, selected, mapRoot, reuseRequest,
                out var matchingRoot, out _) || matchingRoot != mapRoot)
            throw new InvalidDataException("Original reuse identity comparison rejected the selected S073 logical root");
        var wrongMission = reuseRequest;
        wrongMission.MissionId = new FixedString64Bytes("skirmish.invalid");
        var wrongScenario = reuseRequest;
        wrongScenario.ScenarioId = new FixedString64Bytes("scenario.skirmish.invalid");
        if (CampaignMissionOperationMapReuseUtility.TryMatchLogicalIdentity(em, selected, mapRoot, wrongMission,
                out _, out _) || CampaignMissionOperationMapReuseUtility.TryMatchLogicalIdentity(em, selected, mapRoot,
                wrongScenario, out _, out _) || em.GetComponentData<OperationMapMetadataComponent>(mapRoot).PhysicalSourceValidated != 0)
            throw new InvalidDataException("Original reuse identity comparison accepted a mismatched launch or changed physical state");

        JObject aiDiagnostic = null;
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL
        JObject aiEconomyBirthObservation = null;
        var initialEconomyBirthObservations = new JArray();
#endif
        if (nativeAIReceipt != null && !currentSourceDiagnostic)
        {
            var observed = S073NativeAIInputs.Load(repo, nativeAIReceipt, (string)bundle["native_ai_snapshot_receipt"]["sha256"]);
            var resolverOwner = new InitialFactionSpawnCellSystem();
            AIStartupSystem.TryResolveFactionSpawnCell resolver = (byte faction, out int2 cell) =>
                resolverOwner.TryGetConfiguredFactionSpawnCell(em, observed.Fallbacks, faction, out cell);
            observed.RequireObservedCells(resolver);
            double sourceTime = em.WorldUnmanaged.Time.ElapsedTime;
            AIStartupSystem.Result aiResult = default;
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL
            aiEconomyBirthObservation = SourceS073EconomyStartupBirthObserver.Observe(
                em,
                "S073AIStartupSourceBinding.Initialize",
                () => aiResult = S073AIStartupSourceBinding.Initialize(em, repo, aiGraph,
                    (string)bundle["native_ai_source_graph"]["sha256"], observed.Settings, resolver, sourceTime));
#else
            aiResult = S073AIStartupSourceBinding.Initialize(em, repo, aiGraph,
                (string)bundle["native_ai_source_graph"]["sha256"], observed.Settings, resolver, sourceTime);
#endif
            aiDiagnostic = new JObject {
                ["scope"] = "SourceOwner_ObservedSettings_PostStartupResolverComparison_NotNativeReadiness",
                ["native_observation_session"] = (string)observed.Snapshot["session_id"],
                ["source_elapsed_time"] = sourceTime,
                ["post_startup_resolver_cells_matched"] = true,
                ["has_player_auto_mode"] = aiResult.HasPlayerAutoMode,
                ["player_auto_mode_enabled"] = aiResult.PlayerAutoModeEnabled,
                ["native_world_state_copied"] = false, ["startup_admission_granted"] = false
            };
        }
        else if (currentSourceDiagnostic)
        {
            string currentAiGraph = Descriptor(run, "s073_current_authored_ai_source_graph");
            AISettingsSnapshot settings = AISettingsSnapshot.Defaults;
            MatchAISettingsStartupProjection.TryConsume(em, out settings);
            var resolverOwner = new InitialFactionSpawnCellSystem();
            AIStartupSystem.TryResolveFactionSpawnCell resolver = (byte faction, out int2 cell) =>
                resolverOwner.TryGetConfiguredFactionSpawnCell(em, null, faction, out cell);
            double sourceTime = em.WorldUnmanaged.Time.ElapsedTime;
            AIStartupSystem.Result aiResult = default;
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL
            aiEconomyBirthObservation = SourceS073EconomyStartupBirthObserver.Observe(
                em, "S073AIStartupSourceBinding.Initialize",
                () => aiResult = S073AIStartupSourceBinding.Initialize(
                    em, repo, currentAiGraph, (string)run["s073_current_authored_ai_source_graph"]["sha256"],
                    settings, resolver, sourceTime));
#else
            aiResult = S073AIStartupSourceBinding.Initialize(
                em, repo, currentAiGraph, (string)run["s073_current_authored_ai_source_graph"]["sha256"],
                settings, resolver, sourceTime);
#endif
            aiDiagnostic = new JObject {
                ["scope"] = "OriginalAIStartupOwner_CurrentAuthoredConfigAndCurrentWorldInputs_NativeParityUnverified",
                ["source_elapsed_time"] = sourceTime,
                ["native_observation_used"] = false,
                ["post_startup_native_resolver_comparison"] = false,
                ["has_player_auto_mode"] = aiResult.HasPlayerAutoMode,
                ["player_auto_mode_enabled"] = aiResult.PlayerAutoModeEnabled,
                ["native_world_state_copied"] = false,
                ["startup_admission_granted"] = false
            };
        }

        ResourceExchangeRecipeConfigSet exchangeConfig = null;
        JObject exchangeDiagnostic = null;
        if (exchangeGraph != null)
        {
            exchangeConfig = S073ExchangeSourceBinding.Load(repo, exchangeGraph,
                (string)bundle["native_exchange_source_graph"]["sha256"]);
            var owner = new ResourceExchangeStartupProjectionSystemHelper(em);
            var player = owner.Initialize(exchangeConfig);
            var ai = owner.InitializeEligibleAIFactions(exchangeConfig);
            if (!player.Projected || player.RecipeCount != 5 || player.Reason != ResourceExchangeReason.None ||
                ai.ScenarioAllowsAIExchange || ai.ProjectedFactionCount != 0 || ai.Reason != ResourceExchangeReason.None)
                throw new InvalidDataException("Original source exchange startup did not resolve the authored legacy gate");
            exchangeDiagnostic = new JObject {
                ["scope"] = "SourceOwner_SceneAuthoredConfig_NotNativeStartupOrReadiness",
                ["player_projected"] = player.Projected, ["recipe_count"] = player.RecipeCount,
                ["player_reason"] = player.Reason.ToString(), ["ai_exchange_allowed"] = ai.ScenarioAllowsAIExchange,
                ["ai_eligible_factions"] = ai.EligibleFactionCount, ["ai_projected_factions"] = ai.ProjectedFactionCount,
                ["ai_reason"] = ai.Reason.ToString(), ["native_world_state_copied"] = false,
                ["startup_admission_granted"] = false
            };
        }

        // The captured placement domains are source inputs, not evidence that a
        // native scene is loaded in this source host.
        TraceStage("placement_inputs_begin");
        var authored = SourceS073LegacySpawnRequestAdapter.LoadOriginalAuthoredBuildingCache(selected,
            maskPath, Sha(maskPath), selected.SourceSceneReference.AssetGUID);
        var domain = SourceS073LegacySpawnRequestAdapter.LoadSharedPlacementSource(selected,
            physicalPath, Sha(physicalPath), waterPath, Sha(waterPath), startupPath, Sha(startupPath), repo,
            relocationPath, relocationPath == null ? null : Sha(relocationPath), historicalInputDiagnostic);
        domain = domain.BindValidatedS073NativeAuthoredBuildingMask(selected.OperationMapId,
            selected.SourceIdentityHash, selected.ContentHash, authored);
        TraceStage("placement_masks_begin");
        var placementMasks = domain.Build(2048, 1024, 1f, 0f, 0f, 0f);
        var waterMask = domain.BuildWaterMask(true, 2048, 1024, 1f, 0f, 0f, 0f);
        JObject placementMaskFingerprint = null;
        JObject nativeWaterComparison = null;
        JObject nativeWaterStartupParity = null;
        if (nativeWaterReceipt != null)
            waterMask = SourceS073NativeWaterMaskAdapter.ReadQualified(domain, repo,
                nativeWaterSnapshot, (string)bundle["native_water_capture_snapshot"]["sha256"],
                nativeWaterMask, (string)bundle["native_water_capture_mask"]["sha256"],
                nativeWaterReceipt, (string)bundle["native_water_capture_receipt"]["sha256"], out nativeWaterComparison);
        if (nativeWaterParity != null)
        {
            nativeWaterStartupParity = SourceS073NativeStartupWaterParity.Validate(repo, nativeWaterParity,
                (string)bundle["native_water_startup_parity_receipt"]["sha256"],
                (string)bundle["native_water_capture_mask"]["sha256"]);
            if ((bool?)nativeWaterStartupParity["s073_mask_matches_playing_mask_hash"] != true)
                throw new InvalidDataException("Provided S073 native water mask differs from the observed startup/playing parity evidence");
        }
        var gridAsset = materializer.ByPath<GridAuthoringSceneConfigAsset>("Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset");
        var surfaceAsset = materializer.ByPath<MapSurfaceDataAsset>("Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset");
        if (gridAsset.Width != 2048 || gridAsset.Height != 1024 || gridAsset.CellSize != 1f ||
            gridAsset.Origin.x != 0 || gridAsset.Origin.y != 0 || gridAsset.Origin.z != 0 ||
            placementMasks.AuthoredBuildings.Length != 2048 * 1024 || waterMask.Length != 2048 * 1024 ||
            !surfaceAsset.TryCreateRuntimeBlobAsset(Allocator.Persistent, out var surfaceBlob))
            throw new InvalidDataException("S073 selected Campaign grid, surface, or mask inputs changed");
        TraceStage("grid_baking_begin");
        Entity grid = em.CreateEntity();
        GeneratedGridSourceBaking.Bake(em, grid, gridAsset);
        GeneratedFreshGridStorage.Initialize(em, grid);
        var sourceHostGrid = em.GetComponentData<GridConfig>(grid);
        if (!OperationMapMetadataUtility.MatchesGridFrame(in mapMetadata.Blob.Value.Grid, in sourceHostGrid))
            throw new InvalidDataException("S073 materialized grid frame differs from the original published OperationMapGrid metadata");
        JObject nativeSceneBindingDiagnostic = null;
        if (nativeSceneBinding != null)
        {
            string qualified = Path.GetFullPath(Path.Combine(repo, "Logs/SkirmishProduction/s073-native-entity-scene-binding-20261101-r2/receipt-strict-r3.json"));
            if (nativeSceneBinding != qualified) throw new InvalidDataException("Exact qualified native scene-binding receipt required");
            var actualGrid = em.GetComponentData<GridConfig>(grid);
            nativeSceneBindingDiagnostic = S073NativeSourceBinding.Join(repo, new JObject {
                ["source_metadata"] = new JObject {
                    ["operation_map_id"] = activeMap.OperationMapId.ToString(),
                    ["source_identity_hash"] = mapMetadata.Blob.Value.SourceIdentityHash.ToString(),
                    ["source_content_hash"] = mapMetadata.Blob.Value.SourceContentHash.ToString(),
                    ["generated_metadata_hash"] = mapMetadata.Blob.Value.GeneratedMetadataHash.ToString()
                },
                ["grid_frame"] = new JObject { ["width"] = actualGrid.Width, ["height"] = actualGrid.Height,
                    ["cell_size"] = actualGrid.CellSize,
                    ["origin"] = new JArray(actualGrid.Origin.x, actualGrid.Origin.y, actualGrid.Origin.z) }
            });
            if ((bool?)nativeSceneBindingDiagnostic["binding_metadata_matches_source"] != true)
                throw new InvalidDataException("Source map or grid frame differs from the qualified native binding observation");
        }
        TraceStage("road_sync_begin");
        host.SyncS073LegacyDiagnosticRoads(grid);
        TraceStage("road_sync_end");
        JObject physicalBlockerProjection = null;
        List<GeneratedReferenceTemplateImporter.FixedMapPhysicalBlocker> physicalOnlyBlockers = null;
        if (physicalBlockerPath != null)
        {
            TraceStage("physical_blocker_import_begin");
            var source = JObject.Parse(File.ReadAllText(physicalBlockerPath));
            int roots = GeneratedReferenceTemplateImporter.ProjectStreamedPhysicalBlockers(em, source, Path.GetDirectoryName(physicalBlockerPath));
            var state = new SystemState { EntityManager = em, Enabled = true };
            var owner = new StaticGridBlockerUpdateSystem();
            state.Begin(); owner.OnCreate(ref state); state.End();
            state.Begin();
            try { if (!state.Enabled || !state.RequiredQueriesPresent) throw new InvalidDataException("S073 physical blocker owner prerequisites missing"); owner.OnUpdate(ref state); }
            finally { state.End(); }
            var blockers = em.GetComponentData<DynamicBlockerComponent>(grid);
            int occupied = 0; long counts = 0;
            var blockedBytes = new byte[blockers.Counts.Length];
            for (int i = 0; i < blockers.Counts.Length; i++)
            {
                int count = blockers.Counts[i];
                if (count < 0 || blockers.Blocked.IsSet(i) != (count > 0)) throw new InvalidDataException("Physical blocker owner grid inconsistent");
                if (count > 0) { occupied++; blockedBytes[i] = 1; }
                counts += count;
            }
            string blockedSha;
            using (var hash = SHA256.Create()) blockedSha = BitConverter.ToString(hash.ComputeHash(blockedBytes)).Replace("-", "").ToLowerInvariant();
            if (roots != 4931 || occupied != 170610 || counts != 189301 || blockers.WriteInProgress != 0 ||
                blockedSha != "3efb6be2e964833f161426034d877f298805be9769cba23e1e179f8ba0c52d0c")
                throw new InvalidDataException("Source-only physical blocker projection changed from the isolated owner evidence");
            if (Sha(physicalBlockerPath) != "1f375a666f474a73d2aaac5c0b1930a774b117349a1e074f618ecbbd57b01bd7")
                throw new InvalidDataException("Physical blocker capture changed during projection");
            physicalBlockerProjection = new JObject { ["scope"] = "ExperimentalSourceGeometryOnly_NotLoadedScene",
                ["roots"] = roots, ["blocked_cells_before_starting_buildings"] = occupied, ["overlap_count_sum"] = counts,
                ["blocked_mask_sha256_before_starting_buildings"] = blockedSha,
                ["original_blocker_owner_invoked"] = true, ["campaign_gameplay_state_copied"] = false,
                ["native_order_claimed"] = false, ["physical_scene_admitted"] = false };
            var campaignPhysical = JObject.Parse(File.ReadAllText(physicalPath));
            physicalOnlyBlockers = GeneratedReferenceTemplateImporter.ProjectFixedMapNonBuildingBlockers(
                em, campaignPhysical, Path.GetDirectoryName(physicalPath), selected.SourceBinding.SourceOperationMapId);
            if (physicalOnlyBlockers.Count != 23)
                throw new InvalidDataException("Exact 23 fixed-map non-building blockers required");
            var physicalOnlyState = new SystemState { EntityManager = em, Enabled = true };
            var physicalOnlyOwner = new StaticGridBlockerUpdateSystem();
            physicalOnlyState.Begin(); physicalOnlyOwner.OnCreate(ref physicalOnlyState); physicalOnlyState.End();
            physicalOnlyState.Begin();
            try
            {
                if (!physicalOnlyState.Enabled || !physicalOnlyState.RequiredQueriesPresent)
                    throw new InvalidDataException("S073 physical-only blocker owner prerequisites missing");
                physicalOnlyOwner.OnUpdate(ref physicalOnlyState);
            }
            finally { physicalOnlyState.End(); }
            physicalBlockerProjection["fixed_map_nonbuilding_import_count"] = physicalOnlyBlockers.Count;
            physicalBlockerProjection["fixed_map_nonbuilding_campaign_state_copied"] = false;
            TraceStage("physical_blocker_import_end");
        }
        Entity surface = em.CreateEntity();
        em.AddComponentData(surface, new MapSurfaceComponent
        {
            SurfaceBlob = surfaceBlob, GridOrigin = new float3(gridAsset.Origin.x, gridAsset.Origin.y, gridAsset.Origin.z),
            CellSize = gridAsset.CellSize, Dimensions = new int2(gridAsset.Width, gridAsset.Height), HasSurfaceData = 1
        });

        SkirmishPresetRuntimeProjection.BindValidatedSourceReadback(em, data,
            (int)preset["reinforcementInfantryTarget"], (int)preset["attackSquadSize"], (uint)session.Seed);
        bool economyProjectionMatched;
        JObject initialSpawnStep = null;
        S073NativeConfiguredUnits configuredUnits = null;
        JObject queuedSpawnStep;
        JObject placementResult;
        JObject recoveryDiagnostic;
        try
        {
            InitialUnitsSpawnConfig fallback = default;
            InitialUnitsSpawnConfig economyValues = SkirmishPresetRuntimeProjection.ResolveLegacyInitialOwnerValues(em, fallback);
            economyProjectionMatched = economyValues.InitialDollars == Math.Max(0, data.InitialDollars) &&
                economyValues.InitialMaterials == Math.Max(0, data.InitialMaterials) &&
                economyValues.MaterialsCapacity == Math.Max(0, data.MaterialsCapacity) &&
                economyValues.InitialAiMaterials == Math.Max(0, data.InitialAiMaterials) &&
                economyValues.AiMaterialsCapacity == Math.Max(0, data.AiMaterialsCapacity);
            if (!economyProjectionMatched)
                throw new InvalidDataException("Original tactical-material startup owner did not resolve the pinned S073 economy values");

            using var plans = em.CreateEntityQuery(typeof(CustomGameStartupStateComponent), typeof(InitialUnitsSpawnConfig));
            if (plans.CalculateEntityCount() != 1)
                throw new InvalidDataException("Unique original S073 spawn plan required");
            var plan = plans.GetSingletonEntity();
            var boundary = em.CreateEntity();
            em.AddComponentData(boundary, new BuildingRuntimeStateTag());
            em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
            em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
            em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
            // This source-world diagnostic is not the managed startup sequence:
            // catalog owners are not bound yet; exchange is optional and source-only.
            TraceStage("materials_recovery_begin");
            var recovery = new MaterialsScenarioRecoveryStartupSystemHelper(em).Validate(exchangeConfig);
            TraceStage("materials_recovery_end_" + recovery.Code);
            recoveryDiagnostic = new JObject {
                ["owner"] = "MaterialsScenarioRecoveryStartupSystemHelper.Validate",
                ["scope"] = "IncompleteSourceWorld_NotNativeStartupOrReadiness",
                ["exchange_config_supplied"] = exchangeConfig != null,
                ["valid"] = recovery.IsValid,
                ["code"] = recovery.Code.ToString(),
                ["validated_factions"] = recovery.ValidatedFactionCount,
                ["startup_admission_granted"] = false
            };
            // This is the source host's explicit start transition, not a native
            // player input or a claim that the physical scene has loaded.
            RuntimeGameplayStartTransition.ResetForGameplayStart(em);
            var initialOwner = new OriginalS073InitialUnitsSpawnOwner(em);
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL
            JObject firstStepObservation = SourceS073EconomyStartupBirthObserver.Observe(
                em,
                "OriginalS073InitialUnitsSpawnOwner.Step",
                () => initialSpawnStep = initialOwner.Step());
            if (firstStepObservation != null) initialEconomyBirthObservations.Add(firstStepObservation);
#else
            initialSpawnStep = initialOwner.Step();
#endif
            queuedSpawnStep = (JObject)initialSpawnStep.DeepClone();
            var requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            if ((bool?)initialSpawnStep["owner_update_invoked"] != true || requests.Length != 10 ||
                (bool?)initialSpawnStep["owner_marked_completion"] != false)
                throw new InvalidDataException("Original S073 initial-spawn owner did not queue its ten pending buildings");
            int row = 0;
            foreach (var faction in data.Factions)
            foreach (var building in faction.Buildings)
            {
                var request = requests[row];
                if (request.RequestId != row + 1 || request.Status != BuildingRuntimeSpawnRequest.Pending ||
                    request.PlanEntity != plan || request.FactionId != faction.FactionId ||
                    request.PreferredOrigin.x != faction.SpawnX + building.OriginOffsetX ||
                    request.PreferredOrigin.y != faction.SpawnY + building.OriginOffsetY ||
                    request.BuildingId.ToString() != BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(building.LookupKey) ||
                    !GeneratedRuntimeSpawnProcessor.IsLegacyInitialRequest(em, request))
                    throw new InvalidDataException("Original S073 building request changed at row " + row);
                row++;
            }
            if (row != 10) throw new InvalidDataException("Original S073 building row accounting changed");

            TraceDeepStage("production_templates_begin");
            var templates = JObject.Parse(File.ReadAllText(productionPath));
            string[] productionRoots = {
                "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_APC_Fast", "Unit_Veh_APC_Heavy", "Unit_Veh_APC_Slow",
                "Unit_Veh_Light_Armored_Car", "Unit_Veh_Missle_Launcher_Air", "Unit_Veh_Missle_Launcher_Ground",
                "Unit_Veh_Radar_Tank", "Unit_Veh_Tank_USA", "Unit_Veh_Truck_Canopy", "Unit_Veh_Truck_Tanker", "Unit_Veh_Truck_Tray"
            };
            if ((string)templates["kind"] != "AuthoredSkirmishPrefabReferenceData" ||
                (string)templates["authority"] != "ActualSourceAssetsAndNativeBakers_NotMissionState" ||
                (templates["opening_prefabs"] as JObject)?.Count != productionRoots.Length ||
                productionRoots.Any(key => templates["opening_prefabs"]?[key] == null))
                throw new InvalidDataException("Exact native S073 production-prefab source closure required");
            var recipeManager = new EntityManager { LogicalAllocatorAdmitted = true };
            int recipeNext = 1;
            recipeManager.AllocateLogicalIdentity = () => new Entity { Index = recipeNext++, Version = 1 };
            var recipeTemplates = GeneratedReferenceTemplateImporter.Import(recipeManager, templates,
                Path.GetDirectoryName(productionPath), productionRoots);
            TraceDeepStage("production_templates_end");
            if (recipeTemplates.Prefabs.Count != productionRoots.Length)
                throw new InvalidDataException("S073 production-prefab source closure changed");
            TraceDeepStage("building_definitions_begin");
            var definitions = GeneratedBuildingDefinitions.LoadS073Legacy(definitionsPath, Sha(definitionsPath),
                recipeTemplates.Prefabs, config, repo);
            TraceDeepStage("building_definitions_end");
            if (definitions.Definitions.Count != 6)
                throw new InvalidDataException("Exact six original S073 building definitions required");
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL
            sourceConfiguredDefinitionProductionSlots = SourceS073ProductionInputCensus.SourceConfiguredDefinitionProductionSlots(definitions.Definitions);
#endif
            if (placementConfigPath != Path.GetFullPath(Path.Combine(repo, "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset")))
                throw new InvalidDataException("Current source building placement configuration required");
            var matchPlane = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(placementConfigPath), "(?m)^  buildPlaneY: ([^\\r\\n]+)$");
            if (!matchPlane.Success || !float.TryParse(matchPlane.Groups[1].Value,
                    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float buildPlaneY))
                throw new InvalidDataException("Source-authored building placement plane required");
            var roads = domain.CopyOriginalOwnerRoadMask();
            var surfaceRoads = new bool[2048 * 1024];
            BuildingPlacementRoadSurfaceMask.Append(em, surface, em.GetComponentData<GridConfig>(grid), surfaceRoads);
            if (physicalBlockerProjection != null)
            {
                var samples = new JArray();
                foreach (var rect in new[] { (x: 950, y: 438, w: 28, h: 15), (x: 887, y: 444, w: 19, h: 16) })
                {
                    var original = new JArray(); var surfaceCells = new JArray(); var sidewalks = new JArray();
                    var capturedSurface = new JArray(); var capturedSidewalk = new JArray();
                    for (int y = rect.y; y < rect.y + rect.h; y++) for (int x = rect.x; x < rect.x + rect.w; x++)
                    {
                        int index = y * 2048 + x;
                        if (roads[index]) original.Add(new JArray(x, y));
                        if (surfaceRoads[index]) surfaceCells.Add(new JArray(x, y));
                        if (placementMasks.Sidewalks[index]) sidewalks.Add(new JArray(x, y));
                        if (capturedSurfaceRoads != null && capturedSurfaceRoads[index] != 0) capturedSurface.Add(new JArray(x, y));
                        if (capturedSidewalks != null && capturedSidewalks[index] != 0) capturedSidewalk.Add(new JArray(x, y));
                    }
                    samples.Add(new JObject { ["origin"] = new JArray(rect.x, rect.y), ["footprint"] = new JArray(rect.w, rect.h),
                        ["captured_road_cells"] = original, ["surface_road_cells"] = surfaceCells, ["sidewalk_cells"] = sidewalks,
                        ["native_surface_road_cells"] = capturedSurface, ["native_sidewalk_cells"] = capturedSidewalk });
                }
                physicalBlockerProjection["rejected_native_origin_road_inputs"] = samples;
            }
            for (int i = 0; i < roads.Length; i++)
            {
                roads[i] |= surfaceRoads[i] || capturedSurfaceRoads != null && capturedSurfaceRoads[i] != 0;
                if (capturedSidewalks != null) placementMasks.Sidewalks[i] |= capturedSidewalks[i] != 0;
            }
            if (string.Equals(Environment.GetEnvironmentVariable("WARLINE_S073_PLACEMENT_MASK_FINGERPRINT"), "1", StringComparison.Ordinal))
                placementMaskFingerprint = new JObject {
                    ["scope"] = "SourceHostActualPrePlacementInputs_NotNativeReadiness",
                    ["cells"] = authored.Length,
                    ["encoding"] = "row-major bitset; cell i is bit (i mod 8) of byte floor(i/8)",
                    ["authored_sha256"] = FingerprintPlacementMask(authored),
                    ["domain_authored_sha256"] = FingerprintPlacementMask(placementMasks.AuthoredBuildings),
                    ["roads_sha256"] = FingerprintPlacementMask(roads),
                    ["sidewalks_sha256"] = FingerprintPlacementMask(placementMasks.Sidewalks),
                    ["water_sha256"] = FingerprintPlacementMask(waterMask),
                    ["mission_ready"] = false
                };
            TraceDeepStage("configured_catalog_begin");
            JObject currentBuildingCatalogEvidence = null, currentUnitCatalogEvidence = null;
            Dictionary<SourceUnitPrefab, BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry> configuredMetadata;
            S073AssetGuidIndex currentAssetGuidIndex = currentSourceDiagnostic ? new S073AssetGuidIndex(repo) : null;
            if (currentSourceDiagnostic)
                configuredMetadata = S073NativeConfiguredCatalog.BindCurrentAuthored(repo, definitions.Definitions, currentAssetGuidIndex, out currentBuildingCatalogEvidence);
            else
                configuredMetadata = nativeCatalogReceipt == null ? null :
                    S073NativeConfiguredCatalog.Bind(repo, nativeCatalogReceipt, (string)bundle["native_configured_catalog_receipt"]["sha256"], definitions.Definitions);
            if (currentSourceDiagnostic)
                configuredUnits = S073NativeConfiguredUnits.BindCurrentAuthored(em, imported, repo, identityPath,
                    (string)bundle["source_identity"]["sha256"], snapshotPath,
                    (string)bundle["prefab_capture"]["sha256"], Path.Combine(repo, "Assets/Game/Configs/Skirmish/UnitRegistry.asset"),
                    currentAssetGuidIndex, out currentUnitCatalogEvidence);
            else
                configuredUnits = nativeUnitReceipt == null ? null : S073NativeConfiguredUnits.Bind(em, imported, repo,
                    nativeUnitReceipt, (string)bundle["native_configured_unit_receipt"]["sha256"], identityPath,
                    (string)bundle["source_identity"]["sha256"], snapshotPath, (string)bundle["prefab_capture"]["sha256"]);
            TraceDeepStage("configured_catalog_end");
            TraceDeepStage("starting_buildings_begin");
            placementResult = SourceS073LegacySpawnRequestAdapter.ProcessOriginalLegacyStartingBuildings(em, grid,
                boundary, definitions.Definitions, buildings, authored, roads, placementMasks.Sidewalks, waterMask, buildPlaneY,
                configuredMetadata, configuredUnits, diagnosticAdmissionRequested && configuredMetadata != null && configuredUnits != null);
            TraceDeepStage("starting_buildings_end");
            if (currentBuildingCatalogEvidence != null || currentUnitCatalogEvidence != null)
                placementResult["current_source_configured_catalog_readback"] = new JObject {
                    ["scope"] = "CurrentSerializedOwnerInputs_NoNativeParityOrReadiness",
                    ["buildings"] = currentBuildingCatalogEvidence,
                    ["units"] = currentUnitCatalogEvidence
                };
            JObject productionSpawnTransformDiagnostic = SourceS073ProductionSpawnTransformDiagnostic.Capture(
                em, definitions.Definitions, buildings, definitionsPath,
                (string)bundle["building_definitions"]?["sha256"]);
            if (productionSpawnTransformDiagnostic != null)
                placementResult["production_spawn_transform_diagnostic"] = productionSpawnTransformDiagnostic;
            placementResult["configured_metadata_scope"] = configuredMetadata == null ?
                "LegacyFallback_NotNativeConfiguredCatalog" : currentSourceDiagnostic ?
                "CurrentSourceAssetOwnerProjection_NativeParityUnverified" : "PinnedNativeAuthoringMetadata_NotRuntimeOverlayOrReadiness";
            placementResult["unit_spawn_owner"] = initialSpawnStep;
            if (aiDiagnostic != null && configuredMetadata != null && configuredUnits != null)
            {
                var afterPublication = new MaterialsScenarioRecoveryStartupSystemHelper(em).Validate(exchangeConfig);
                recoveryDiagnostic["after_source_catalog_publication"] = new JObject {
                    ["scope"] = "IncompleteSourceWorld_NotNativeStartupOrReadiness",
                    ["exchange_config_supplied"] = exchangeConfig != null,
                    ["valid"] = afterPublication.IsValid, ["code"] = afterPublication.Code.ToString(),
                    ["validated_factions"] = afterPublication.ValidatedFactionCount,
                    ["startup_admission_granted"] = false
                };
            }
            if ((string)placementResult["status"] == "Passed")
            {
                var blockerState = new SystemState { EntityManager = em, Enabled = true };
                var blockerOwner = new StaticGridBlockerUpdateSystem();
                blockerState.Begin(); blockerOwner.OnCreate(ref blockerState); blockerState.End();
                blockerState.Begin();
                if (blockerState.Enabled && blockerState.RequiredQueriesPresent) blockerOwner.OnUpdate(ref blockerState);
                blockerState.End();
                if (physicalBlockerProjection != null)
                {
                    var combined = em.GetComponentData<DynamicBlockerComponent>(grid);
                    var rectangleEvidence = new JArray();
                    foreach (var expected in physicalOnlyBlockers)
                    {
                        if (!em.Exists(expected.Entity) || !em.HasComponent<StaticBlockerPrevBounds>(expected.Entity))
                            throw new InvalidDataException("Fixed-map physical-only blocker lacks native owner history");
                        var history = em.GetComponentData<StaticBlockerPrevBounds>(expected.Entity);
                        if (history.AppliedGrid != grid || history.StorageEpoch != combined.StorageEpoch ||
                            !math.all(history.Min == expected.Min) || !math.all(history.Max == expected.Max) ||
                            combined.WriteInProgress != 0)
                            throw new InvalidDataException("Fixed-map physical-only blocker rectangle differs from captured source geometry");
                        rectangleEvidence.Add(new JObject { ["source_id"] = expected.SourceId,
                            ["min"] = new JArray(history.Min.x, history.Min.y),
                            ["max_exclusive"] = new JArray(history.Max.x, history.Max.y),
                            ["captured_source_rectangle_match"] = true });
                    }
                    physicalBlockerProjection["fixed_map_nonbuilding_rectangles"] = rectangleEvidence;
                    physicalBlockerProjection["fixed_map_nonbuilding_all_captured_rectangles_match"] = rectangleEvidence.Count == 23;
                    var bytes = new byte[combined.Counts.Length]; int blocked = 0; long total = 0;
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        int count = combined.Counts[i];
                        if (count < 0 || combined.Blocked.IsSet(i) != (count > 0)) throw new InvalidDataException("Combined static blocker storage inconsistent");
                        if (count > 0) { bytes[i] = 1; blocked++; } total += count;
                    }
                    string hash; using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                    using var domainQuery = em.CreateEntityQuery(typeof(StaticGridBlocker), typeof(UnitGrid), typeof(GridBlockerSize));
                    using var blockerEntities = domainQuery.ToEntityArray(Allocator.Temp);
                    var others = new JArray(); int sourceCount = 0, boundCount = 0;
                    foreach (var entity in blockerEntities)
                    {
                        bool source = em.HasComponent<OperationMapBuildingComponent>(entity);
                        if (source) sourceCount++;
                        bool hasHistory = em.HasComponent<StaticBlockerPrevBounds>(entity);
                        var h = hasHistory ? em.GetComponentData<StaticBlockerPrevBounds>(entity) : default;
                        bool bound = hasHistory && h.AppliedGrid == grid && h.StorageEpoch == combined.StorageEpoch && combined.WriteInProgress == 0;
                        if (bound) boundCount++;
                        if (!source) others.Add(new JObject { ["faction"] = em.HasComponent<Faction>(entity) ? (int?)em.GetComponentData<Faction>(entity).Id : null,
                            ["source_key"] = em.HasComponent<UnitSourcePrefabKey>(entity) ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() : null,
                            ["history_bound"] = bound, ["min"] = hasHistory ? new JArray(h.Min.x, h.Min.y) : null,
                            ["max"] = hasHistory ? new JArray(h.Max.x, h.Max.y) : null,
                            ["friendly_pass_faction"] = hasHistory ? (int?)h.FriendlyPassFactionId : null });
                    }
                    physicalBlockerProjection["combined_after_starting_buildings"] = new JObject {
                        ["blocked_cells"] = blocked, ["count_sum"] = total, ["blocked_mask_sha256"] = hash,
                        ["static_domain_count"] = blockerEntities.Length, ["source_building_count"] = sourceCount,
                        ["histories_bound_to_grid"] = boundCount, ["other_blockers"] = others,
                        ["scope"] = "Source-host combined blocker domain; not native scene admission" };
                }
                TraceDeepStage("initial_unit_owner_begin");
                for (int frame = 0; frame < 16 && (string)initialSpawnStep["status"] != "Completed"; frame++)
                {
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL
                    JObject stepObservation = SourceS073EconomyStartupBirthObserver.Observe(
                        em,
                        "OriginalS073InitialUnitsSpawnOwner.Step",
                        () => initialSpawnStep = initialOwner.Step());
                    if (stepObservation != null) initialEconomyBirthObservations.Add(stepObservation);
#else
                    initialSpawnStep = initialOwner.Step();
#endif
                }
                TraceDeepStage("initial_unit_owner_end");
                placementResult["unit_spawn_owner"] = initialSpawnStep;
                placementResult["units_spawned"] = (int?)initialSpawnStep["units_after"] == 24;
                placementResult["initial_spawn_completion_marked"] = (bool?)initialSpawnStep["owner_marked_completion"] == true;
                if (Environment.GetEnvironmentVariable("WARLINE_S073_SOURCE_OCCUPANCY_CENSUS") == "1")
                {
                    if ((bool?)initialSpawnStep["original_dynamic_occupancy_owner_invoked"] != true ||
                        (int?)initialSpawnStep["units_after"] != 24)
                        throw new InvalidDataException("Opt-in S073 occupancy census requires the actual original occupancy owner after the complete 24-unit source roster");
                    placementResult["source_occupancy_contributor_census"] = SourceS073OccupancyContributorCensus.Capture(em);
                }
                if (SourceS073NeutralOccupancyProjectionImporter.IsOptedIn)
                {
                    if ((bool?)initialSpawnStep["original_dynamic_occupancy_owner_invoked"] != true ||
                        (int?)initialSpawnStep["units_after"] != 24)
                        throw new InvalidDataException("Opt-in neutral projection requires the actual original occupancy owner and unchanged 24-unit source roster");
                    placementResult["neutral_occupancy_projection_import"] =
                        SourceS073NeutralOccupancyProjectionImporter.ImportAndRebuild(
                            em, grid, mapRoot, matches.GetSingletonEntity(), plan, repo, assetRepo);
                }
                if ((bool)placementResult["units_spawned"] && (bool)placementResult["initial_spawn_completion_marked"])
                {
                    using var spawned = em.CreateEntityQuery(new EntityQueryDesc
                    {
                        All = new[] { ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitSourcePrefabKey>() },
                        None = new[] { ComponentType.ReadOnly<Prefab>(), ComponentType.ReadOnly<Disabled>(),
                            ComponentType.ReadOnly<RuntimeBuildingCombatTag>(), ComponentType.ReadOnly<StaticGridBlocker>() }
                    });
                    using var units = spawned.ToEntityArray(Allocator.Temp);
                    if (units.Length != 24) throw new InvalidDataException("Original S073 source roster must retain 24 live units");
                    host.RetainS073LegacyDiagnosticStartup(em, grid, surface, matches.GetSingletonEntity(), boundary, plan, units.ToArray());
                    placementResult["tactical_ai_owner_input_census"] = SourceS073TacticalAIOwnerCensus.Capture(
                        em, matches.GetSingletonEntity(), directory);
                    placementResult["same_world_host_retained"] = true;
                    placementResult["host_initialization_stage"] = host.InitializationStage;
                }
            }

            SourceS073LegacyCombatPresetProvider.Bind(em, Path.Combine(assetRepo, "Assets/Game/Resources/SkirmishIndustrialBasin.asset"));
            try
            {
                if (diagnosticAdmissionRequested)
                {
                    string selectedScenePath = FindUnitySceneByGuid(assetRepo, selected.SourceSceneReference.AssetGUID);
                    string selectedSceneText = File.ReadAllText(selectedScenePath);
                    bool entitySceneMode = System.Text.RegularExpressions.Regex.Matches(selectedSceneText,
                        @"(?m)^  canonicalPresentationMode: 2\r?$").Count == 1;
                    bool selectedEntityScene = selected.PresentationKind == OperationMapPresentationKind.EntityScene;
                    string cityConfigPath = Path.Combine(assetRepo, "Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset");
                    int cityCount = ReadSceneReferencedConfigInt(missionScenePath, "runtimeCitySpawnerConfig", cityConfigPath, "cityCount", out string cityConfigSha);
                    int citySpawnOnStart = ReadSceneReferencedConfigInt(missionScenePath, "runtimeCitySpawnerConfig", cityConfigPath, "spawnOnStart", out string cityConfigShaAgain);
                    if (cityConfigSha != cityConfigShaAgain)
                        throw new InvalidDataException("Runtime city config changed during source-host admission read");

                    bool buildingReferenceMissing = string.IsNullOrEmpty(selected.BuildingPlacementsReference.AssetGUID);
                    bool vehicleReferenceMissing = string.IsNullOrEmpty(selected.VehiclePlacementsReference.AssetGUID);
                    bool buildingPlacementComplete = GameplayStartCompletionPredicate.MapPlacementOwnerComplete(
                        buildingReferenceMissing, false, false, false, false, false);
                    bool vehiclePlacementComplete = GameplayStartCompletionPredicate.MapPlacementOwnerComplete(
                        vehicleReferenceMissing, false, false, false, false, false);
                    using var startupPlans = em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig));
                    using var initializedPlans = em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig), typeof(InitialUnitsSpawnInitialized));
                    int totalPlans = startupPlans.CalculateEntityCount();
                    int initializedPlansCount = initializedPlans.CalculateEntityCount();
                    bool startupCompletion = (string)placementResult["status"] == "Passed" &&
                        (bool?)initialSpawnStep?["owner_marked_completion"] == true &&
                        buildingPlacementComplete && vehiclePlacementComplete;
                    bool buildingStartupComplete = GameplayStartCompletionPredicate.BuildingStartupComplete(startupCompletion);
                    bool citySpawned = GameplayStartCompletionPredicate.RuntimeCityOwnerSpawned(false, cityCount);
                    bool cityOwnerReady = GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(
                        citySpawnOnStart != 0 && cityCount > 0, citySpawned);
                    bool legacyPresentationEnabled = GameplayStartCompletionPredicate.LegacyPresentationEnabled(entitySceneMode);
                    bool blockerOwnerReady = GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(
                        legacyPresentationEnabled, !legacyPresentationEnabled);
                    // Decorations are presentation-only and are intentionally excluded from
                    // this diagnostic gameplay admission; native loading still checks them.
                    bool decorationsOwnerReady = GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(false, false);
                    using var gameplayStateQuery = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
                    if (gameplayStateQuery.CalculateEntityCount() != 1)
                        throw new InvalidDataException("Unique source-host gameplay state required for diagnostic admission");
                    RuntimeGameplayStateComponent gameplayBeforeAdmission = gameplayStateQuery.GetSingleton<RuntimeGameplayStateComponent>();
                    bool sourceHostReady = (bool?)placementResult["same_world_host_retained"] == true;
                    bool runtimeWorldAvailable = GameplayStartCompletionPredicate.RuntimeWorldAvailable(
                        em.World != null, em.World != null && em.World.IsCreated);
                    bool initialConfigsComplete = GameplayStartCompletionPredicate.InitialSpawnConfigsComplete(
                        totalPlans, initializedPlansCount);
                    bool gateComplete = GameplayStartCompletionPredicate.GameplayStartRequested(
                            sourceHostReady, gameplayBeforeAdmission.PlayRequested != 0) &&
                        buildingStartupComplete && cityOwnerReady && blockerOwnerReady && decorationsOwnerReady &&
                        runtimeWorldAvailable && initialConfigsComplete;

                    sourceHostAdmission = new JObject
                    {
                        ["kind"] = "S073SourceHostDiagnosticGameplayAdmission",
                        ["scope"] = "SameWorldOriginalStartupOwners_NativeReadinessAndMapBlockerParityUnverified",
                        ["opt_in"] = true,
                        ["source_host_retained"] = sourceHostReady,
                        ["selected_map_entity_scene"] = selectedEntityScene,
                        ["selected_scene_path"] = selectedScenePath,
                        ["selected_scene_sha256"] = Sha(selectedScenePath),
                        ["entity_scene_canonical_mode"] = entitySceneMode,
                        ["building_placement_reference_missing"] = buildingReferenceMissing,
                        ["vehicle_placement_reference_missing"] = vehicleReferenceMissing,
                        ["building_placement_owner_complete"] = buildingPlacementComplete,
                        ["vehicle_placement_owner_complete"] = vehiclePlacementComplete,
                        ["building_placement_policy_owner"] = "GameplayStartCompletionPredicate.MapPlacementOwnerComplete",
                        ["original_initial_unit_owner_complete"] = (bool?)initialSpawnStep?["owner_marked_completion"] == true,
                        ["legacy_building_registration_status"] = placementResult["status"],
                        ["building_startup_complete"] = buildingStartupComplete,
                        ["runtime_city_config_path"] = cityConfigPath,
                        ["runtime_city_config_sha256"] = cityConfigSha,
                        ["runtime_city_count"] = cityCount,
                        ["runtime_city_spawn_on_start"] = citySpawnOnStart != 0,
                        ["runtime_city_owner_spawned"] = citySpawned,
                        ["runtime_city_owner_ready"] = cityOwnerReady,
                        ["legacy_runtime_blocker_owner_enabled"] = legacyPresentationEnabled,
                        ["runtime_grid_blocker_owner_ready"] = blockerOwnerReady,
                        ["runtime_decorations_admitted_as_cosmetic"] = true,
                        ["runtime_decorations_owner_ready"] = decorationsOwnerReady,
                        ["initial_spawn_config_count"] = totalPlans,
                        ["initialized_initial_spawn_config_count"] = initializedPlansCount,
                        ["runtime_world_available"] = runtimeWorldAvailable,
                        ["play_requested"] = gameplayBeforeAdmission.PlayRequested != 0,
                        ["shared_start_completion_predicate_passed"] = gateComplete,
                        ["physical_source_validated_before"] = em.GetComponentData<OperationMapMetadataComponent>(mapRoot).PhysicalSourceValidated != 0,
                        ["native_readiness_claimed"] = false,
                        ["map_blocker_parity_verified"] = false,
                        ["mission_outcome_claimed"] = false
                    };
                    if (selectedEntityScene && !entitySceneMode)
                        sourceHostAdmission["rejection"] = "Selected map EntityScene kind disagrees with canonical runtime scene mode";
                    else if (!buildingReferenceMissing || !vehicleReferenceMissing)
                        sourceHostAdmission["rejection"] = "Selected OperationMap has configured legacy placement assets requiring original placement owners";
                    else if (!gateComplete)
                        sourceHostAdmission["rejection"] = "Shared original gameplay start completion predicate is not satisfied";

                    if (selectedEntityScene && entitySceneMode && buildingReferenceMissing && vehicleReferenceMissing && gateComplete)
                    {
                        diagnosticSourceActivation = RuntimeGameplayStartTransition.TryActivateSourceHostDiagnostic(em, gateComplete);
                        sourceHostAdmission["activated_source_host_simulation"] = diagnosticSourceActivation;
                        sourceHostAdmission["physical_source_validated_after"] = em.GetComponentData<OperationMapMetadataComponent>(mapRoot).PhysicalSourceValidated != 0;
                    }
                }

                var rules = new SkirmishRulesSystem();
                var rulesState = new SystemState { EntityManager = em, Enabled = true };
                rulesState.Begin(); rules.OnCreate(ref rulesState); rulesState.End();
                rulesState.Begin();
                try
                {
                    if (!rulesState.RequiredQueriesPresent) throw new InvalidDataException("Original S073 rules owner prerequisites are missing");
                    rules.OnUpdate(ref rulesState);
                }
                finally { rulesState.End(); }
                var afterRules = matches.GetSingleton<SkirmishMatchState>();
                if (!diagnosticSourceActivation &&
                    (afterRules.Phase != SkirmishPhase.Preparing || afterRules.Outcome != SkirmishOutcome.None))
                    throw new InvalidDataException("Original S073 rules owner advanced without source-host diagnostic admission");
                if (diagnosticSourceActivation)
                {
                    if (afterRules.Phase == SkirmishPhase.Playing && afterRules.Outcome == SkirmishOutcome.None)
                    {
                        // Native SkirmishRulesSystem assigns player squad membership on
                        // its next Playing update after the Preparing-to-Playing handoff.
                        // Preserve that owner order before the source-host ARIA observation.
                        rulesState.Begin();
                        try
                        {
                            if (!rulesState.RequiredQueriesPresent)
                                throw new InvalidDataException("Original S073 rules owner lost prerequisites before its Playing update");
                            rules.OnUpdate(ref rulesState);
                        }
                        finally { rulesState.End(); }
                        var afterRosterRules = matches.GetSingleton<SkirmishMatchState>();
                        if (afterRosterRules.Phase == SkirmishPhase.Playing && afterRosterRules.Outcome == SkirmishOutcome.None)
                        {
                            JObject ariaCapabilityCensus = CaptureOptInAriaCapabilityCensus(em, afterRosterRules);
#if WARLINE_SOURCE_S073_ORIGINAL_ARIA_PROBE
                            JObject originalAriaStepProbe = SourceS073OriginalAriaStepDiagnostic.Capture(
                                em, afterRosterRules, OriginalAriaStepProbeOptIn);
#endif
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL || WARLINE_SOURCE_S073_RESOURCE_CENSUS
                            if (SourceS073ResourceEconomyRuntimeCensus.Enabled)
                            {
                                resourceEconomyRuntimeCensus = new JArray
                                {
                                    SourceS073ResourceEconomyRuntimeCensus.Capture(em, "diagnostic_startup_before_recurring_ticks", 0, run)
                                };
                            }
#endif
                            SourceS073LegacyProductionRuntime productionRuntime = null;
                            JToken playerProductionDiagnostic = null;
                            try
                            {
                                TraceDeepStage("production_runtime_bind_begin");
                                productionRuntime = SourceS073LegacyProductionRuntime.Bind(
                                    repo, em, grid, boundary, buildings, configuredUnits, historicalInputDiagnostic);
                                if (sourceTransportDiagnosticRequested)
                                {
                                    sourceTransportDiagnosticProvenance = productionRuntime
                                        .BindSourceHostTransportDiagnostic(repo, HostCameraOwnership, imported);
                                }
                                string recruitDiagnostic = Environment.GetEnvironmentVariable("WARLINE_S073_SYNTHETIC_PLAYER_RECRUIT_DIAGNOSTIC");
                                if (Environment.GetEnvironmentVariable("WARLINE_S073_NO_PLAYER_POLICY_DIAGNOSTIC") == "1" && !string.IsNullOrEmpty(recruitDiagnostic))
                                    throw new NotSupportedException("No-player-policy diagnostic cannot issue explicit player production commands");
                                if (recruitDiagnostic == "1")
                                    playerProductionDiagnostic = productionRuntime.RequestPlayerConfiguredUnit(0, 0);
                                else if (recruitDiagnostic == "queue-capacity")
                                {
                                    var recruitRequests = new JArray();
                                    for (int i = 0; i < 5; i++)
                                        recruitRequests.Add(productionRuntime.RequestPlayerConfiguredUnit(0, 0));
                                    playerProductionDiagnostic = new JObject {
                                        ["scope"] = "FiveExplicitLogicalSourceCommands_NotNativeInput",
                                        ["requests"] = recruitRequests, ["native_input_observed"] = false,
                                        ["mission_ready"] = false
                                    };
                                }
                                else if (!string.IsNullOrEmpty(recruitDiagnostic))
                                    throw new NotSupportedException("Unknown explicit source recruit diagnostic");
                                TraceDeepStage("production_runtime_bind_end");
                                workerSetupWatch.Stop();
                                workerSetupWallSeconds = workerSetupWatch.Elapsed.TotalSeconds;
                                schedulerInvocationAttempted = true;
                                legacyOwnerSchedulerTick = SourceS073LegacyTacticalOwnerScheduler.RunRecurringTicks(
                                    em, host, canonicalCatalog, productionRuntime, ref rules, ref rulesState, schedulerTickBudget);
                                var recordedSteps = legacyOwnerSchedulerTick["aria_steps"] as JArray;
                                ariaStep = new JObject {
                                    ["kind"] = "S073LegacySharedAriaRecurringPolicySteps",
                                    ["planner_route"] = (bool?)legacyOwnerSchedulerTick["no_player_policy_diagnostic"] == true
                                        ? "NoPlayerPolicy_ExplicitSourceDiagnostic" : "LogicalSourceGroundBaseAssaultPolicy_NotNativeS073LegacyPlanner",
                                    ["no_player_policy_diagnostic"] = (bool?)legacyOwnerSchedulerTick["no_player_policy_diagnostic"] == true,
                                    ["native_s073_planner_equivalence"] = false,
                                    ["step_count"] = recordedSteps?.Count ?? 0,
                                    ["accepted_action_count"] = (int?)legacyOwnerSchedulerTick["aria_actions_accepted"] ?? 0,
                                    ["last_step"] = recordedSteps != null && recordedSteps.Count > 0 ? recordedSteps[recordedSteps.Count - 1].DeepClone() : null,
                                    ["readiness_claimed"] = false,
                                    ["mission_outcome_claimed"] = false
                                };
                                if (ariaCapabilityCensus != null)
                                    ariaStep["first_step_capability_census"] = ariaCapabilityCensus;
                                if (playerProductionDiagnostic != null)
                                    ariaStep["synthetic_player_recruit_diagnostic"] = playerProductionDiagnostic;
#if WARLINE_SOURCE_S073_ORIGINAL_ARIA_PROBE
                                if (originalAriaStepProbe != null)
                                    ariaStep["original_aria_step_diagnostic"] = originalAriaStepProbe;
#endif
                                ariaStepFailure = (string)legacyOwnerSchedulerTick["aria_failure"];
                                session = matches.GetSingleton<SkirmishMatchState>();
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL || WARLINE_SOURCE_S073_RESOURCE_CENSUS
                                if (resourceEconomyRuntimeCensus != null)
                                    resourceEconomyRuntimeCensus.Add(SourceS073ResourceEconomyRuntimeCensus.Capture(
                                        em, "post_recurring_ticks", (int?)legacyOwnerSchedulerTick["ticks_executed"] ?? 0, run));
#endif
                            }
                            catch (Exception error)
                            {
                                JObject failureProductionSnapshot = null;
                                string failureSnapshotError = null;
                                if (productionRuntime != null)
                                {
                                    try { failureProductionSnapshot = productionRuntime.Snapshot(); }
                                    catch (Exception snapshotError) { failureSnapshotError = snapshotError.ToString(); }
                                }
                                JToken simulationAdvanceTime = error.Data.Contains("S073SimulationAdvanceWallSeconds")
                                    ? JToken.FromObject(error.Data["S073SimulationAdvanceWallSeconds"])
                                    : legacyOwnerSchedulerTick?["simulation_advance_wall_seconds"]?.DeepClone();
                                string simulationAdvanceStatus = error.Data.Contains("S073SimulationAdvanceWallSeconds")
                                    ? "MeasuredLoopFailed"
                                    : (string)legacyOwnerSchedulerTick?["simulation_advance_timing_status"] ?? "NotMeasured_LoopNotEntered";
                                legacyOwnerSchedulerTick = new JObject {
                                    ["status"] = "Failed",
                                    ["exception"] = error.GetType().Name,
                                    ["message"] = error.Message,
                                    ["stack_trace"] = error.StackTrace,
                                    ["source_production_snapshot_after_failure"] = failureProductionSnapshot,
                                    ["source_production_snapshot_error"] = failureSnapshotError,
                                    ["original_owner_scheduler_invoked"] = schedulerInvocationAttempted,
                                    ["ticks_executed"] = error.Data.Contains("S073CompletedSchedulerTicks")
                                        ? JToken.FromObject(error.Data["S073CompletedSchedulerTicks"]) : JValue.CreateNull(),
                                    ["simulation_advance_wall_seconds"] = simulationAdvanceTime ?? JValue.CreateNull(),
                                    ["simulation_advance_timing_status"] = simulationAdvanceStatus,
                                    ["simulation_advance_timing_scope"] = "Stopwatch around recurring tick loop only; excludes owner creation, ground-owner setup, command-buffer cleanup and result serialization"
                                };
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL || WARLINE_SOURCE_S073_RESOURCE_CENSUS
                                if (resourceEconomyRuntimeCensus != null)
                                    resourceEconomyRuntimeCensus.Add(SourceS073ResourceEconomyRuntimeCensus.Capture(
                                        em, "post_recurring_ticks_after_scheduler_failure", 0, run));
#endif
                            }
                            using var admittedRoster = em.CreateEntityQuery(new EntityQueryDesc
                            {
                                All = new[] { ComponentType.ReadOnly<SkirmishSquadMember>(), ComponentType.ReadOnly<Faction>(),
                                    ComponentType.ReadOnly<UnitHealth>(), ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitMove>() },
                                None = new[] { ComponentType.ReadOnly<RuntimeBuildingCombatTag>(), ComponentType.ReadOnly<StaticGridBlocker>(),
                                    ComponentType.ReadOnly<UnitTransportPassenger>() }
                            });
                            int eligiblePlayerRoster = 0;
                            using (var members = admittedRoster.ToEntityArray(Allocator.Temp))
                                foreach (Entity member in members)
                                    if (em.GetComponentData<Faction>(member).Id == FactionIdentity.PlayerFactionId &&
                                        em.GetComponentData<UnitHealth>(member).Current > 0)
                                        eligiblePlayerRoster++;
                            try
                            {
                                // The recurring source-host owner loop now advances the
                                // shared ARIA policy at each original fixed-step boundary.
                                // Preserve the roster observation alongside that result.
                                if (ariaStep != null) ariaStep["eligible_player_roster_count_before_step"] = eligiblePlayerRoster;
                                if (ariaStep != null) ariaStep["original_rules_owner_followup_update_invoked"] = true;
                            }
                            catch (Exception error) { ariaStepFailure = error.GetType().Name + ": " + error.Message; }
                        }
                        else
                        {
                            ariaStepFailure = "Original SkirmishRulesSystem Playing update did not retain a live nonterminal session";
                        }
                    }
                    else
                    {
                        ariaStepFailure = "Original SkirmishRulesSystem did not enter Playing from admitted source-host startup";
                    }
                }
                if (physicalBlockerProjection != null)
                {
                    using var sourceQuery = em.CreateEntityQuery(typeof(OperationMapBuildingComponent), typeof(Faction), typeof(UnitHealth));
                    using var sourceEntities = sourceQuery.ToEntityArray(Allocator.Temp);
                    if (sourceEntities.Length != 4931) throw new InvalidDataException("Exact projected source scenery required");
                    var rows = new JArray();
                    foreach (var entity in sourceEntities)
                    {
                        if (!em.HasComponent<SkirmishSceneryNormalized>(entity) || !em.HasComponent<RuntimeBuildingCombatInfo>(entity) ||
                            !em.HasComponent<OperationMapBuildingDestroyedComponent>(entity) ||
                            em.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(entity) ||
                            em.HasComponent<AIControlledTag>(entity) || em.HasComponent<BuildingResourceStorageComponent>(entity) ||
                            em.HasComponent<MaterialFabricationComponent>(entity) || em.HasComponent<OperationMapBuildingProductionQueueComponent>(entity) ||
                            em.HasBuffer<OperationMapBuildingUnitProductionRequest>(entity))
                            throw new InvalidDataException("Source scenery must be neutral, undestroyed, and free of Campaign runtime state");
                        var building = em.GetComponentData<OperationMapBuildingComponent>(entity);
                        var health = em.GetComponentData<UnitHealth>(entity);
                        var info = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                        var transform = em.GetComponentData<LocalTransform>(entity);
                        if (em.GetComponentData<Faction>(entity).Id != 0 || info.OwnerFactionId != 0 || health.Max <= 0 || health.Current != health.Max)
                            throw new InvalidDataException("Authoring-derived neutral source scenery values required");
                        rows.Add(new JObject { ["stable_id"] = building.StableId.ToString(),
                            ["source_global_object_id"] = building.SourceGlobalObjectId.ToString(), ["placement_index"] = building.PlacementIndex,
                            ["faction"] = 0, ["ai"] = false, ["combat_owner"] = 0,
                            ["position"] = new JArray(transform.Position.x, transform.Position.y, transform.Position.z),
                            ["health_current"] = health.Current, ["health_max"] = health.Max });
                    }
                    string sceneryPath = Path.Combine(directory, "neutral-scenery-readback.json");
                    var scenery = new JObject { ["kind"] = "S073SourceNeutralSceneryReadback", ["source_capture_sha256"] = Sha(physicalBlockerPath),
                        ["normalization_owner"] = "SkirmishWorldSetup.NormalizeScenery", ["entities"] = rows,
                        ["campaign_runtime_state_copied"] = false, ["complete_gameplay_payload"] = false, ["mission_ready"] = false };
                    using (var file = new FileStream(sceneryPath, FileMode.CreateNew, FileAccess.Write))
                    using (var writer = new StreamWriter(file)) writer.Write(scenery.ToString(Formatting.Indented) + "\n");
                    physicalBlockerProjection["neutral_scenery_readback"] = new JObject { ["path"] = sceneryPath, ["sha256"] = Sha(sceneryPath), ["count"] = rows.Count,
                        ["scope"] = "Authoring-derived neutral identity, health and transforms; no resource, production or session state" };
                }
            }
            finally { SourceS073LegacyCombatPresetProvider.Unbind(em); }
        }
        finally
        {
            SkirmishPresetRuntimeProjection.ReleaseBoundSourceReadback(em);
        }

        host.CompleteInitialRoadInputs();
        var roadReceipt = host.InitialRoadReceipt;
        if ((bool?)roadReceipt?["actual_original_sync_executed"] != true ||
            (int?)roadReceipt?["cells"] != 2048 * 1024)
            throw new InvalidDataException("Original initial-road owner did not retain its full S073 grid projection");
        session = matches.GetSingleton<SkirmishMatchState>();
        bool terminalDiagnostic = diagnosticSourceActivation &&
            CorroboratesDiagnosticTerminal(em, session, originalSessionId, schedulerTickBudget, legacyOwnerSchedulerTick);
        if (sourceTransportDiagnosticRequested && sourceTransportDiagnosticProvenance == null)
            throw new InvalidDataException("Opted-in source transport diagnostic did not pass source-host camera and captured-projection admission");
        if ((!diagnosticSourceActivation && session.Phase != SkirmishPhase.Preparing) ||
            (diagnosticSourceActivation && session.Phase != SkirmishPhase.Playing && !terminalDiagnostic) ||
            session.Seed != 104729 || (session.Outcome != SkirmishOutcome.None && !terminalDiagnostic) ||
            (session.Phase == SkirmishPhase.Finished && !terminalDiagnostic) ||
            em.GetComponentData<OperationMapMetadataComponent>(mapRoot).PhysicalSourceValidated != 0)
            throw new InvalidDataException("S073 source-host post-scheduler guard rejected state: phase=" + session.Phase +
                " outcome=" + session.Outcome + " seed=" + session.Seed +
                " physical_source_validated=" + em.GetComponentData<OperationMapMetadataComponent>(mapRoot).PhysicalSourceValidated +
                " terminal_corroborated=" + terminalDiagnostic);
        JObject sourceRawBlockerOccupancy = SourceS073PathInputCensus.CaptureRaw(em, grid, directory, run);
        JObject openingFocusPrerequisiteCensus = CaptureOptInOpeningFocusPrerequisiteCensus(em, matches.GetSingleton<SkirmishMatchState>());
        JObject moveOrderOwnerDiagnostic = RunOptInMoveOrderDiagnostic(
            em, grid, mapRoot, matches.GetSingletonEntity(), directory);
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL
        JObject tacticalAIDisposableCopy = SourceS073TacticalAIDisposableCopyFixture.Run(
            em, directory, Path.Combine(assetRepo, "Assets/Game/Resources/SkirmishIndustrialBasin.asset"), data,
            (int)preset["reinforcementInfantryTarget"], (int)preset["attackSquadSize"],
            (JObject)placementResult["original_birth_identity_capture"], aiEconomyBirthObservation,
            initialEconomyBirthObservations, buildings, configuredUnits, repo, historicalInputDiagnostic, HostCameraOwnership,
            productionPath, Sha(productionPath));
        JObject legacyEconomyStorageCensus = SourceS073LegacyEconomyStorageCensus.Run(em);
        JObject productionInputCensusBefore = SourceS073ProductionInputCensus.Run(em, sourceConfiguredDefinitionProductionSlots);
        JObject productionInputCensusAfter = productionInputCensusBefore == null ? null : SourceS073ProductionInputCensus.Run(em, sourceConfiguredDefinitionProductionSlots);
        JObject productionInputCensus = null;
        if (productionInputCensusBefore != null)
        {
            if (!JToken.DeepEquals(productionInputCensusBefore, productionInputCensusAfter))
                throw new InvalidDataException("S073 production input census changed across its read-only source guard snapshots");
            productionInputCensus = new JObject
            {
                ["kind"] = "S073ProductionInputCensusGuardedReadback",
                ["schema_version"] = 1,
                ["source_guard_before"] = productionInputCensusBefore,
                ["source_guard_after"] = productionInputCensusAfter,
                ["source_guard_unchanged"] = true,
                ["original_production_owner_invoked"] = false
            };
        }
#endif
        JObject derivedMovementFixture = SourceS073DerivedMovementFixture.Run(
            em, grid, matches.GetSingletonEntity(), directory);

        JObject loadingGateDiagnostic;
        using (var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
        {
            if (gameplay.CalculateEntityCount() != 1)
                throw new InvalidDataException("Unique source-owned gameplay start state required");
            RuntimeGameplayStateComponent gameplayState = gameplay.GetSingleton<RuntimeGameplayStateComponent>();
            bool sourceLoadingGateComplete = SourceS073LoadingGate.Evaluate(
                em,
                gameplayInitialized: false,
                playRequested: gameplayState.PlayRequested != 0,
                buildingStartupComplete: false,
                citySpawned: false,
                blockersSpawned: false,
                decorationsSpawned: false);
            RuntimeGameplayStateComponent afterGate = gameplay.GetSingleton<RuntimeGameplayStateComponent>();
            byte expectedSimulationActive = diagnosticSourceActivation ? (byte)1 : (byte)0;
            if (sourceLoadingGateComplete || afterGate.SimulationActive != gameplayState.SimulationActive ||
                afterGate.PlayRequested != gameplayState.PlayRequested ||
                (terminalDiagnostic && (gameplayState.SimulationActive != 0 || gameplayState.PlayRequested != 0 ||
                    gameplayState.SelectionModeActive != 0 || gameplayState.BuildModeActive != 0)) ||
                (!terminalDiagnostic && gameplayState.SimulationActive != expectedSimulationActive))
                throw new InvalidDataException("Native loading gate rejected observed source state: gate_complete=" + sourceLoadingGateComplete +
                    " simulation_active_before=" + gameplayState.SimulationActive + " simulation_active_after=" + afterGate.SimulationActive +
                    " play_requested_before=" + gameplayState.PlayRequested + " play_requested_after=" + afterGate.PlayRequested +
                    " expected_nonterminal_active=" + expectedSimulationActive + " terminal_corroborated=" + terminalDiagnostic);
            loadingGateDiagnostic = new JObject
            {
                ["kind"] = "S073SourceLoadingGateDiagnostic",
                ["owner"] = "GameplayStartCompletionPredicate",
                ["scope"] = "SharedPredicate_UnknownManagedFactsFailClosed_NoActivation",
                ["gameplay_initialized_known"] = false,
                ["play_requested"] = gameplayState.PlayRequested != 0,
                ["building_startup_complete_known"] = false,
                ["city_spawned_known"] = false,
                ["grid_blockers_spawned_known"] = false,
                ["decorations_spawned_known"] = false,
                ["runtime_world_available"] = em.World != null && em.World.IsCreated,
                ["complete"] = sourceLoadingGateComplete,
                ["simulation_active_after"] = afterGate.SimulationActive != 0,
                ["source_host_diagnostic_simulation_active"] = diagnosticSourceActivation,
                ["native_runtime_state_copied"] = false,
                ["mission_ready"] = false
            };
        }

        var result = new JObject
        {
            ["schema_version"] = 1, ["kind"] = "SkirmishSourceSimulationRunReceipt",
            ["mission"] = "S073", ["seed"] = session.Seed,
            ["status"] = (string)placementResult["status"] != "Passed" ? "PreparingPlacementRejected" :
                diagnosticSourceActivation && session.Phase == SkirmishPhase.Finished &&
                    (session.Outcome == SkirmishOutcome.Victory || session.Outcome == SkirmishOutcome.Defeat || session.Outcome == SkirmishOutcome.Draw) ? "DiagnosticTerminalOutcomeNativeReadinessUnverified" :
                diagnosticSourceActivation && session.Phase == SkirmishPhase.Playing ? "DiagnosticPlayingNativeReadinessUnverified" :
                "PreparingPhysicalSourceRequired", ["process_exit"] = 2,
            ["initialization_owner"] = "CustomGameStartupSystemHelper.InitializeFromLegacyData",
            ["launch_owner"] = "SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin",
            ["seed_owner"] = "SkirmishLegacyLaunchOwner.ApplySeed",
            ["original_preparemap_before_startup"] = true,
            ["preparation_scenery_scope"] = "SourceHostWithoutLoadedPhysicalScene",
            ["legacy_initial_building_entries"] = startup.LegacyOwner.InitialBuildingEntryCount,
            ["preset_readback_bound_to_queued_entity_manager"] = true,
            ["economy_owner_projection_matched"] = economyProjectionMatched,
            ["economy_owner_applied"] = false,
            ["ai_startup_diagnostic"] = aiDiagnostic,
            ["ai_startup_mode"] = currentSourceDiagnostic ? "OriginalCurrentAuthoredConfigOwner_NativeParityUnverified" : nativeAIReceipt != null ? "QualifiedNativeObservationBound" : "NotInvoked_NoAIObservationSupplied",
            ["exchange_startup_diagnostic"] = exchangeDiagnostic,
            ["materials_recovery_diagnostic"] = recoveryDiagnostic,
            ["source_blocker_occupancy_raw_capture"] = sourceRawBlockerOccupancy,
            ["tactical_ai_owner_input_census"] = placementResult["tactical_ai_owner_input_census"],
            ["session_phase"] = session.Phase.ToString(), ["session_outcome"] = session.Outcome.ToString(), ["session_id"] = session.SessionId.ToString(),
            ["selected_operation_map_source_materialized"] = true,
            ["operation_map_id"] = selected.OperationMapId,
            ["operation_map_content_hash"] = selected.ContentHash,
            ["operation_map_source_identity_hash"] = selected.SourceIdentityHash,
            ["source_scene_guid"] = selected.SourceSceneReference.AssetGUID,
            ["source_map_anchor_count"] = selected.Anchors.Length,
            ["source_map_camera_count"] = selected.Cameras.Length,
            ["source_camera_metadata_selection"] = cameraMetadataSelection,
            ["original_metadata_compiler_passed"] = true,
            ["published_map_grid_frame_matches_source_host_grid"] = true,
            ["rotation_reference_sha256"] = Sha(rotationPath),
            ["world_initialized"] = false, ["operation_map_published"] = true,
            ["source_host_diagnostic_activation"] = diagnosticSourceActivation,
            ["source_host_transport_diagnostic"] = sourceTransportDiagnosticProvenance == null
                ? new JObject { ["status"] = "NotRequested", ["native_state_copied"] = false,
                    ["native_transport_delivery_bound"] = false, ["mission_ready"] = false,
                    ["mission_outcome_claimed"] = false }
                : (JObject)sourceTransportDiagnosticProvenance.DeepClone(),
            ["source_host_terminal_observation"] = new JObject
            {
                ["observed"] = session.Phase == SkirmishPhase.Finished && session.Outcome != SkirmishOutcome.None,
                ["phase"] = session.Phase.ToString(),
                ["outcome"] = session.Outcome.ToString(),
                ["observation_scope"] = "ActualSourceHostMatchStateReadback",
                ["original_owner_terminal_corroborated"] = terminalDiagnostic,
                ["native_terminal_validated"] = false,
                ["mission_outcome_claimed"] = false
            },
            ["source_host_diagnostic_gameplay_admission"] = sourceHostAdmission,
            ["source_host_aria_policy_step"] = ariaStep,
            ["source_host_aria_policy_step_failure"] = ariaStepFailure,
            ["source_host_original_tactical_owner_scheduler_tick"] = legacyOwnerSchedulerTick,
            ["source_host_opening_focus_prerequisite_census"] = openingFocusPrerequisiteCensus,
            ["source_placement_input_fingerprints"] = placementMaskFingerprint,
            ["simulation_advance_wall_seconds"] = legacyOwnerSchedulerTick?["simulation_advance_wall_seconds"]?.DeepClone(),
            ["simulation_advance_timing_status"] = legacyOwnerSchedulerTick == null ? "NotMeasured_SchedulerNotInvoked" :
                legacyOwnerSchedulerTick["simulation_advance_timing_status"]?.DeepClone(),
            ["simulation_advance_timing_scope"] = "Stopwatch around recurring tick loop only; excludes owner creation, ground-owner setup, command-buffer cleanup and result serialization",
            ["worker_setup_wall_seconds"] = workerSetupWallSeconds,
            ["worker_setup_timing_status"] = schedulerInvocationAttempted ? "Measured" : "NotMeasured_SchedulerNotInvoked",
            ["worker_setup_timing_scope"] = "Run entry through immediately before the original recurring scheduler invocation; null when no scheduler invocation occurs",
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL || WARLINE_SOURCE_S073_RESOURCE_CENSUS
            ["source_host_resource_economy_runtime_census"] = resourceEconomyRuntimeCensus,
#endif
            ["operation_map_publication_scope"] = "MetadataOnly_PhysicalSourceNotValidated",
            ["operation_map_physical_source_validated"] = false,
            ["native_source_binding_comparison"] = nativeSceneBindingDiagnostic,
            ["original_logical_reuse_identity_matched"] = true,
            ["mismatched_mission_rejected"] = true,
            ["mismatched_scenario_rejected"] = true,
            ["source_grid_baked"] = true, ["source_surface_decoded"] = true,
            ["experimental_physical_blocker_projection"] = physicalBlockerProjection,
            ["original_initial_road_sync_executed"] = true,
            ["initial_road_projection"] = roadReceipt,
            ["placement_input_scope"] = historicalInputDiagnostic ? "HistoricalCapturedCampaignSource_CurrentSourceEquivalenceUnverified" : "CapturedCampaignSource_NotLoadedSceneOrSourceFit",
            ["historical_input_diagnostic"] = historicalInputDiagnostic,
            ["historical_input_current_source_equivalence"] = false,
            ["captured_water_footprints"] = domain.WaterFootprintCount,
            ["map_blocker_parity_status"] = !historicalInputDiagnostic && (bool?)nativeWaterStartupParity?["s073_mask_matches_playing_mask_hash"] == true ? "qualified-native-observation" : "unverified",
            ["map_blocker_parity_verified"] = !historicalInputDiagnostic && (bool?)nativeWaterStartupParity?["s073_mask_matches_playing_mask_hash"] == true,
            ["map_blocker_parity_scope"] = "PlacementBlockerMaskOnly_GroundMovementAndFullSceneParityNotAsserted",
            ["native_blocker_inputs_ignored_in_explicit_diagnostic"] = ignoredNativeBlockerInputs,
            ["native_road_inputs_ignored_in_explicit_diagnostic"] = ignoredNativeRoadInputs,
            ["native_water_mask_comparison"] = nativeWaterComparison,
            ["native_water_startup_parity"] = nativeWaterStartupParity,
            ["source_host_start_transition_invoked"] = true,
            ["move_order_owner_diagnostic"] = moveOrderOwnerDiagnostic,
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL
            ["legacy_economy_storage_census"] = legacyEconomyStorageCensus,
            ["production_input_census"] = productionInputCensus,
            ["ai_startup_economy_birth_observation"] = aiEconomyBirthObservation,
            ["initial_units_owner_economy_birth_observations"] = initialEconomyBirthObservations,
#endif
            ["source_derived_movement_fixture"] = derivedMovementFixture,
            ["loading_gate_source_diagnostic"] = loadingGateDiagnostic,
            ["native_player_input_observed"] = false,
            ["initial_spawn_owner_step"] = queuedSpawnStep,
            ["original_building_requests_queued"] = 10,
            ["building_and_unit_startup_diagnostic"] = placementResult,
            ["original_legacy_rules_preparing_step_passed"] = !diagnosticSourceActivation,
            ["original_legacy_rules_diagnostic_playing_step_passed"] = diagnosticSourceActivation && session.Phase == SkirmishPhase.Playing,
            ["scheduler_tick_executed"] = (int?)legacyOwnerSchedulerTick?["ticks_executed"] > 0,
            ["all_original_owner_gates_open_on_last_tick"] = (bool?)legacyOwnerSchedulerTick?["all_original_owner_gates_open"] == true,
            ["mission_ready"] = false,
            ["blocking_gate"] = diagnosticSourceActivation
                ? $"S073 source-host executed {(int?)legacyOwnerSchedulerTick?["ticks_executed"] ?? 0} recurring original-owner ticks; phase={session.Phase}, outcome={session.Outcome}. Native physical-source readiness and map-blocker parity remain unverified"
                : (string)placementResult["status"] == "Passed"
                ? "S073 physical-source validation and autonomous scheduler admission remain incomplete"
                : "S073 source building placement did not complete; physical-source validation remains incomplete",
            ["source_readback_sha256"] = Sha(configPath), ["source_identity_sha256"] = Sha(identityPath),
            ["launch_capture_sha256"] = Sha(launchPath)
        };
        result["worker_total_timing_scope"] = "Run entry through completed result object assembly; measured immediately before JSON serialization and excludes serialization, file write, and post-write cleanup";
        workerTotalWatch.Stop();
        result["worker_total_wall_seconds"] = workerTotalWatch.Elapsed.TotalSeconds;
        File.WriteAllText(Path.Combine(directory, "result.json"), result.ToString(Formatting.Indented));
        Console.WriteLine("[ActualS073LegacyStartup] " + result.ToString(Formatting.None));
        RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage(em, grid);
        em.DestroyEntity(surface);
        surfaceBlob.Dispose();
        return 2;
    }
}

// Executes one source-host tick through the original legacy tactical systems.
// This is deliberately not the normal Unity scheduler: every owner retains its
// own RequiredQueriesPresent gate, and an incomplete owner set is reported as
// partial rather than being filled with synthetic runtime read models.
internal static class SourceS073LegacyTacticalOwnerScheduler
{
    // Independent singleton-query counts reproduce the original OnCreate
    // RequireForUpdate component domain, without adding components or asserting
    // that an uncalled owner did useful work.
    static JObject CaptureClosedOwnerQueryInputs(EntityManager em)
    {
        int Count(Type component) { using var query = em.CreateEntityQuery(component); return query.CalculateEntityCount(); }
        return new JObject {
            ["scope"] = "FinalSameWorldOriginalRequireForUpdateQueryCounts_NoMutation",
            ["unit_engaged_movement"] = new JObject {
                ["GridConfig"] = Count(typeof(GridConfig)), ["EngageTarget"] = Count(typeof(EngageTarget)),
                ["GridRoad"] = Count(typeof(GridRoad)), ["GridRoadSidewalk"] = Count(typeof(GridRoadSidewalk)),
                ["GridRoadDirt"] = Count(typeof(GridRoadDirt)), ["DynamicBlockerComponent"] = Count(typeof(DynamicBlockerComponent)),
                ["UnitAttack"] = Count(typeof(UnitAttack)) },
            ["building_defense_attack"] = new JObject { ["BuildingDefenseWeapon"] = Count(typeof(BuildingDefenseWeapon)) },
            ["proves_native_world_or_owner_parity"] = false, ["mission_ready"] = false };
    }
    // Compact opt-in snapshots at the actual production-owner boundaries. The
    // snapshots retain only source-owned tactical materials and the real
    // request-consumer buffer; the full resource census remains startup/end only.
    static JObject CaptureProductionPhaseState(EntityManager em, SourceS073LegacyProductionRuntime productionRuntime,
        int sourceTick, string phase)
    {
        using (var matches = em.CreateEntityQuery(typeof(SkirmishMatchState)))
        {
            if (matches.CalculateEntityCount() != 1)
                throw new InvalidDataException("S073 production phase trace requires one same-world match.");
            var match = matches.GetSingleton<SkirmishMatchState>();
            var materials = new JArray();
            using (var query = em.CreateEntityQuery(typeof(FactionEconomy), typeof(FactionTacticalMaterialsComponent)))
            using (var entities = query.ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    var economy = em.GetComponentData<FactionEconomy>(entity);
                    var bank = em.GetComponentData<FactionTacticalMaterialsComponent>(entity);
                    materials.Add(new JObject { ["entity"] = entity.Index + ":" + entity.Version,
                        ["economy_faction_id"] = economy.FactionId, ["materials_faction_id"] = bank.FactionId,
                        ["current"] = bank.Current, ["capacity"] = bank.Capacity,
                        ["lifetime_spent"] = bank.LifetimeSpent,
                        ["lifetime_production_spent"] = bank.LifetimeProductionSpent,
                        ["version"] = bank.Version });
                }
            }
            JObject runtime = productionRuntime.Snapshot();
            var observed = new JObject { ["tactical_materials"] = materials,
                ["request_counts"] = runtime["request_counts"]?.DeepClone(),
                ["requests"] = runtime["requests"]?.DeepClone() };
            return new JObject { ["source_tick"] = sourceTick,
                ["world_sequence"] = em.WorldUnmanaged.SequenceNumber,
                ["session_id"] = match.SessionId.ToString(), ["session_phase"] = match.Phase.ToString(),
                ["session_outcome"] = match.Outcome.ToString(), ["phase"] = phase,
                ["observed"] = observed };
        }
    }

    // Read-only diagnostic around the original AIProductionSystem. It exposes the
    // source owner's actual plan, economy and runtime production summary so a zero
    // request result can be attributed to the owner's real decision gates.
    static JObject CaptureProductionDecisionInputs(EntityManager em)
    {
        var plans = new JArray();
        using (var query = em.CreateEntityQuery(
                   ComponentType.ReadOnly<AIProductionPlan>(),
                   ComponentType.ReadOnly<AIProductionPlanEntry>()))
        using (var entities = query.ToEntityArray(Allocator.Temp))
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                AIProductionPlan plan = em.GetComponentData<AIProductionPlan>(entity);
                var entries = new JArray();
                var unitCounts = new JArray();
                DynamicBuffer<AIProductionPlanEntry> planEntries = em.GetBuffer<AIProductionPlanEntry>(entity, true);
                for (int entryIndex = 0; entryIndex < planEntries.Length; entryIndex++)
                {
                    string unitId = planEntries[entryIndex].UnitId.ToString();
                    entries.Add(unitId);
                    int produced = 0, queued = 0;
                    using (var boundaryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
                               ComponentType.ReadOnly<BuildingRuntimeUnitProductionSummary>()))
                    {
                        if (boundaryQuery.CalculateEntityCount() == 1)
                        {
                            var summaries = em.GetBuffer<BuildingRuntimeUnitProductionSummary>(boundaryQuery.GetSingletonEntity(), true);
                            for (int summaryIndex = 0; summaryIndex < summaries.Length; summaryIndex++)
                            {
                                var summary = summaries[summaryIndex];
                                if (summary.FactionId != plan.FactionId || !summary.UnitId.ToString().Equals(unitId, StringComparison.Ordinal))
                                    continue;
                                produced = summary.ProducedCount;
                                queued = summary.QueuedCount;
                                break;
                            }
                        }
                    }
                    unitCounts.Add(new JObject { ["unit_id"] = unitId, ["produced_count"] = produced, ["queued_count"] = queued });
                }

                JObject economy = null;
                using (var economyQuery = em.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>()))
                using (var economyEntities = economyQuery.ToEntityArray(Allocator.Temp))
                {
                    for (int economyIndex = 0; economyIndex < economyEntities.Length; economyIndex++)
                    {
                        var state = em.GetComponentData<FactionEconomy>(economyEntities[economyIndex]);
                        if (state.FactionId != plan.FactionId) continue;
                        economy = new JObject { ["money"] = state.Money, ["oil"] = state.Oil, ["fuel"] = state.Fuel,
                            ["oil_income_rate"] = state.OilIncomeRate, ["fuel_income_rate"] = state.FuelIncomeRate };
                        if (em.HasComponent<FactionTacticalMaterialsComponent>(economyEntities[economyIndex]))
                        {
                            var materials = em.GetComponentData<FactionTacticalMaterialsComponent>(economyEntities[economyIndex]);
                            economy["materials"] = materials.Current;
                            economy["materials_capacity"] = materials.Capacity;
                        }
                        break;
                    }
                }

                plans.Add(new JObject { ["faction_id"] = plan.FactionId, ["enabled"] = plan.Enabled != 0,
                    ["next_unit_index"] = plan.NextUnitIndex, ["target_produced_units"] = plan.TargetProducedUnits,
                    ["max_queued_units"] = plan.MaxQueuedUnits, ["unit_production_interval_seconds"] = plan.UnitProductionIntervalSeconds,
                    ["last_production_time"] = plan.LastProductionTime, ["entries"] = entries,
                    ["runtime_unit_counts"] = unitCounts, ["economy"] = economy });
            }
        }

        int pending = 0, succeeded = 0, failed = 0;
        using (var boundaryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
                   ComponentType.ReadOnly<BuildingFactionUnitProductionRequest>()))
        {
            if (boundaryQuery.CalculateEntityCount() == 1)
            {
                var requests = em.GetBuffer<BuildingFactionUnitProductionRequest>(boundaryQuery.GetSingletonEntity(), true);
                for (int i = 0; i < requests.Length; i++)
                {
                    if (requests[i].Status == BuildingFactionUnitProductionRequest.Pending) pending++;
                    else if (requests[i].Status == BuildingFactionUnitProductionRequest.Succeeded) succeeded++;
                    else if (requests[i].Status == BuildingFactionUnitProductionRequest.Failed) failed++;
                }
            }
        }
        return new JObject { ["scope"] = "ReadOnlyOriginalAIProductionPlanAndEconomyState_NoInjection",
            ["plans"] = plans, ["request_counts"] = new JObject { ["pending"] = pending, ["succeeded"] = succeeded, ["failed"] = failed } };
    }

    internal static JObject RunRecurringTicks(EntityManager em, AutonomousWorld host,
        SkirmishBattleCatalogConfig canonicalCatalog, SourceS073LegacyProductionRuntime productionRuntime,
        ref SkirmishRulesSystem rules,
        ref SystemState rulesState, int tickBudget)
    {
        if (em == null || host == null || canonicalCatalog == null || productionRuntime == null || tickBudget < 0 || tickBudget > 3600 ||
            !ReferenceEquals(host.Entities, em) || em.World == null || !em.World.IsCreated)
            throw new NotSupportedException("Bounded same-world S073 source scheduler inputs required");
        using (var matches = em.CreateEntityQuery(typeof(SkirmishMatchState)))
        using (var expanded = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)))
        {
            if (matches.CalculateEntityCount() != 1 || expanded.CalculateEntityCount() != 0)
                throw new NotSupportedException("Exactly one non-expanded original legacy match required");
            var match = matches.GetSingleton<SkirmishMatchState>();
            if (match.Phase != SkirmishPhase.Playing || match.Outcome != SkirmishOutcome.None)
                throw new NotSupportedException("Original S073 tactical owners require the live Playing session");
        }

        const float delta = 1f / 60f;
        var economy = new AIEconomySystem();
        var faction = new AIFactionControlSystem();
        var planner = new AIBuildPlannerSystem();
        var production = new AIProductionSystem();
        var squads = new AISquadSystem();
        var targeting = new AITargetingSystem();
        var combatOrders = new AICombatOrderSystem();
        var engagement = new UnitEngagementSystem();
        var engagedMovement = new UnitEngagedMovementSystem();
        var attacks = new UnitAttackSystem();
        var buildingDefense = new BuildingDefenseAttackSystem();
        var deaths = new UnitDeathSystem();
        var economyState = new SystemState { EntityManager = em, Enabled = true };
        var factionState = new SystemState { EntityManager = em, Enabled = true };
        var plannerState = new SystemState { EntityManager = em, Enabled = true };
        var productionState = new SystemState { EntityManager = em, Enabled = true };
        var squadState = new SystemState { EntityManager = em, Enabled = true };
        var targetingState = new SystemState { EntityManager = em, Enabled = true };
        var combatState = new SystemState { EntityManager = em, Enabled = true };
        var engagementState = new SystemState { EntityManager = em, Enabled = true };
        var engagedMovementState = new SystemState { EntityManager = em, Enabled = true };
        var attackState = new SystemState { EntityManager = em, Enabled = true };
        var buildingDefenseState = new SystemState { EntityManager = em, Enabled = true };
        var deathState = new SystemState { EntityManager = em, Enabled = true };
        bool targetingCreated = false, combatCreated = false, attacksCreated = false;
        bool defenseCreated = false, deathsCreated = false;
        JObject lastStateAfterControl = null, lastStateAfterSquads = null;
        JObject productionDecisionInputsBefore = CaptureProductionDecisionInputs(em);
        JObject productionDecisionInputsAfter = null;
        JArray lastRows = null;
        var ariaSteps = new JArray();
        var productionPhaseTrace = new JArray();
        JObject previousProductionPhaseState = null;
        int targetingUpdates = 0, combatOrderUpdates = 0, engagementUpdates = 0;
        int pathUpdates = 0, engagedMovementUpdates = 0, gridMovementUpdates = 0;
        int attackUpdates = 0, buildingDefenseUpdates = 0, deathUpdates = 0;
        int rulesUpdates = 0, ariaActionsAccepted = 0, ticksExecuted = 0;
        string ariaFailure = null;
        bool ariaStopped = false;
        bool noPlayerPolicy = string.Equals(Environment.GetEnvironmentVariable("WARLINE_S073_NO_PLAYER_POLICY_DIAGNOSTIC"), "1", StringComparison.Ordinal);
        if (noPlayerPolicy && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_S073_SYNTHETIC_PLAYER_RECRUIT_DIAGNOSTIC")))
            throw new NotSupportedException("No-player-policy diagnostic cannot issue explicit player production commands");
        Stopwatch simulationAdvanceWatch = null;
        double? simulationAdvanceWallSeconds = null;
        Entity schedulerEndSimulationEntity = Entity.Null;
        OwnedEndSimulationBuffers schedulerEndSimulationBuffers = null;
        var ownerCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        void CountOwner(string name, bool required, bool enabled, bool invoked)
        {
            ownerCounts[name] = ownerCounts.TryGetValue(name, out int previous) ? previous + (invoked ? 1 : 0) : (invoked ? 1 : 0);
            lastRows.Add(new JObject { ["owner"] = name, ["required_queries_present"] = required,
                ["enabled"] = enabled, ["on_update_invoked"] = invoked });
        }

        try
        {
            economyState.Begin(); try { economy.OnCreate(ref economyState); } finally { economyState.End(); }
            factionState.Begin(); try { faction.OnCreate(ref factionState); } finally { factionState.End(); }
            plannerState.Begin(); try { planner.OnCreate(ref plannerState); } finally { plannerState.End(); }
            productionState.Begin(); try { production.OnCreate(ref productionState); } finally { productionState.End(); }
            squadState.Begin(); try { squads.OnCreate(ref squadState); } finally { squadState.End(); }
            targetingState.Begin(); try { targeting.OnCreate(ref targetingState); targetingCreated = true; } finally { targetingState.End(); }
            combatState.Begin(); try { combatOrders.OnCreate(ref combatState); combatCreated = true; } finally { combatState.End(); }
            engagementState.Begin(); try { engagement.OnCreate(ref engagementState); } finally { engagementState.End(); }
            engagedMovementState.Begin(); try { engagedMovement.OnCreate(ref engagedMovementState); } finally { engagedMovementState.End(); }
            attackState.Begin(); try { attacks.OnCreate(ref attackState); attacksCreated = true; } finally { attackState.End(); }
            buildingDefenseState.Begin(); try { buildingDefense.OnCreate(ref buildingDefenseState); defenseCreated = true; } finally { buildingDefenseState.End(); }
            deathState.Begin(); try { deaths.OnCreate(ref deathState); deathsCreated = true; } finally { deathState.End(); }

            using (var endSimulationQuery = em.CreateEntityQuery(typeof(EndSimulationEntityCommandBufferSystem.Singleton)))
            {
                int count = endSimulationQuery.CalculateEntityCount();
                if (count > 1) throw new NotSupportedException("Unique original EndSimulation command-buffer owner required");
                if (count == 0)
                {
                    schedulerEndSimulationBuffers = new OwnedEndSimulationBuffers();
                    schedulerEndSimulationEntity = em.CreateEntity();
                    em.AddComponentData(schedulerEndSimulationEntity,
                        new EndSimulationEntityCommandBufferSystem.Singleton { Owner = schedulerEndSimulationBuffers });
                }
            }
            using var ground = new OriginalGroundOwners(em, generatedFresh: true);
            simulationAdvanceWatch = Stopwatch.StartNew();
            try
            {
            for (int frame = 1; tickBudget == 0 || frame <= tickBudget; frame++)
            {
                using (var matchQuery = em.CreateEntityQuery(typeof(SkirmishMatchState)))
                {
                    SkirmishMatchState live = matchQuery.GetSingleton<SkirmishMatchState>();
                    if (live.Phase != SkirmishPhase.Playing || live.Outcome != SkirmishOutcome.None) break;
                }
                double elapsed = em.WorldUnmanaged.Time.ElapsedTime + delta;
                em.WorldUnmanaged.Time = new TimeData { ElapsedTime = elapsed, DeltaTime = delta };
                UnityEngine.Time.frameCount = frame;
                UnityEngine.Time.time = (float)elapsed;
                UnityEngine.Time.unscaledTime = (float)elapsed;
                UnityEngine.Time.deltaTime = delta;
                UnityEngine.Time.unscaledDeltaTime = delta;
                UnityEngine.Time.timeScale = 1f;
                lastRows = new JArray();

                economyState.Begin(); try { bool q = economyState.RequiredQueriesPresent; bool called = economyState.Enabled && q; if (called) economy.OnUpdate(ref economyState); CountOwner("AIEconomySystem", q, economyState.Enabled, called); } finally { economyState.End(); }
                factionState.Begin(); try { bool q = factionState.RequiredQueriesPresent; bool called = factionState.Enabled && q; if (called) faction.OnUpdate(ref factionState); CountOwner("AIFactionControlSystem", q, factionState.Enabled, called); } finally { factionState.End(); }
                lastStateAfterControl = CaptureS073TacticalOwnerState(em);
                plannerState.Begin(); try { bool q = plannerState.RequiredQueriesPresent; bool called = plannerState.Enabled && q; if (called) planner.OnUpdate(ref plannerState); CountOwner("AIBuildPlannerSystem", q, plannerState.Enabled, called); } finally { plannerState.End(); }
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL || WARLINE_SOURCE_S073_RESOURCE_CENSUS
                bool traceProductionPhases = SourceS073ResourceEconomyRuntimeCensus.Enabled;
                JObject beforeProductionOwner = traceProductionPhases
                    ? CaptureProductionPhaseState(em, productionRuntime, frame, "before_AIProductionSystem_OnUpdate") : null;
#endif
                productionState.Begin(); try { bool q = productionState.RequiredQueriesPresent; bool called = productionState.Enabled && q; if (called) production.OnUpdate(ref productionState); CountOwner("AIProductionSystem", q, productionState.Enabled, called); } finally { productionState.End(); }
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL || WARLINE_SOURCE_S073_RESOURCE_CENSUS
                JObject afterProductionOwner = traceProductionPhases
                    ? CaptureProductionPhaseState(em, productionRuntime, frame, "after_AIProductionSystem_OnUpdate") : null;
                JObject beforeRequestConsumer = traceProductionPhases
                    ? CaptureProductionPhaseState(em, productionRuntime, frame, "before_SourceS073LegacyProductionRuntime_Step") : null;
#endif
                productionRuntime.Step((float)elapsed, delta);
                CountOwner("S073OriginalProductionRequestAndDeliveryOwner", true, true, true);
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL || WARLINE_SOURCE_S073_RESOURCE_CENSUS
                JObject afterRequestConsumer = traceProductionPhases
                    ? CaptureProductionPhaseState(em, productionRuntime, frame, "after_SourceS073LegacyProductionRuntime_Step") : null;
                if (traceProductionPhases)
                {
                    bool selectedDiagnosticTick = frame >= 898 && frame <= 903;
                    // Compare only observed state payloads; stage labels and ticks are provenance,
                    // not changes in the resources or request buffer.
                    bool stateChanged = selectedDiagnosticTick ||
                        (previousProductionPhaseState != null && !JToken.DeepEquals(previousProductionPhaseState, beforeProductionOwner["observed"])) ||
                        !JToken.DeepEquals(beforeProductionOwner["observed"], afterProductionOwner["observed"]) ||
                        !JToken.DeepEquals(afterProductionOwner["observed"], beforeRequestConsumer["observed"]) ||
                        !JToken.DeepEquals(beforeRequestConsumer["observed"], afterRequestConsumer["observed"]);
                    if (stateChanged)
                    {
                        productionPhaseTrace.Add(beforeProductionOwner);
                        productionPhaseTrace.Add(afterProductionOwner);
                        productionPhaseTrace.Add(beforeRequestConsumer);
                        productionPhaseTrace.Add(afterRequestConsumer);
                    }
                    previousProductionPhaseState = (JObject)afterRequestConsumer["observed"].DeepClone();
                }
#endif
                squadState.Begin(); try { bool q = squadState.RequiredQueriesPresent; bool called = squadState.Enabled && q; if (called) squads.OnUpdate(ref squadState); CountOwner("AISquadSystem", q, squadState.Enabled, called); } finally { squadState.End(); }
                lastStateAfterSquads = CaptureS073TacticalOwnerState(em);
                targetingState.Begin(); try { bool q = targetingState.RequiredQueriesPresent; bool called = targetingState.Enabled && q; if (called) { targeting.OnUpdate(ref targetingState); targetingUpdates++; } CountOwner("AITargetingSystem", q, targetingState.Enabled, called); } finally { targetingState.End(); }
                combatState.Begin(); try { bool q = combatState.RequiredQueriesPresent; bool called = combatState.Enabled && q; if (called) { combatOrders.OnUpdate(ref combatState); combatOrderUpdates++; } CountOwner("AICombatOrderSystem", q, combatState.Enabled, called); } finally { combatState.End(); }

                ground.OccupancyPhase();
                engagementState.Begin(); try { bool q = engagementState.RequiredQueriesPresent; bool called = engagementState.Enabled && q; if (called) { engagement.OnUpdate(ref engagementState); engagementUpdates++; } CountOwner("UnitEngagementSystem", q, engagementState.Enabled, called); } finally { engagementState.End(); }
                engagedMovementState.Begin(); try { bool q = engagedMovementState.RequiredQueriesPresent; bool called = engagedMovementState.Enabled && q; if (called) { engagedMovement.OnUpdate(ref engagedMovementState); engagedMovementUpdates++; } CountOwner("UnitEngagedMovementSystem", q, engagedMovementState.Enabled, called); } finally { engagedMovementState.End(); }
                if (ground.PathPhase(frame)) pathUpdates++;
                if (ground.MovementPhase()) gridMovementUpdates++;
                attackState.Begin(); try { bool q = attackState.RequiredQueriesPresent; bool called = attackState.Enabled && q; if (called) { attacks.OnUpdate(ref attackState); attackUpdates++; } CountOwner("UnitAttackSystem", q, attackState.Enabled, called); } finally { attackState.End(); }
                buildingDefenseState.Begin(); try { bool q = buildingDefenseState.RequiredQueriesPresent; bool called = buildingDefenseState.Enabled && q; if (called) { buildingDefense.OnUpdate(ref buildingDefenseState); buildingDefenseUpdates++; } CountOwner("BuildingDefenseAttackSystem", q, buildingDefenseState.Enabled, called); } finally { buildingDefenseState.End(); }
                deathState.Begin(); try { bool q = deathState.RequiredQueriesPresent; bool called = deathState.Enabled && q; if (called) { deaths.OnUpdate(ref deathState); deathUpdates++; } CountOwner("UnitDeathSystem", q, deathState.Enabled, called); } finally { deathState.End(); }
                ground.EndSimulationPhase();
                rulesState.Begin(); try { bool q = rulesState.RequiredQueriesPresent; bool called = rulesState.Enabled && q; if (called) { rules.OnUpdate(ref rulesState); rulesUpdates++; } CountOwner("SkirmishRulesSystem", q, rulesState.Enabled, called); } finally { rulesState.End(); }

                ticksExecuted++;
                using (var matchQuery = em.CreateEntityQuery(typeof(SkirmishMatchState)))
                {
                    SkirmishMatchState live = matchQuery.GetSingleton<SkirmishMatchState>();
                    if (live.Phase == SkirmishPhase.Playing && live.Outcome == SkirmishOutcome.None && !ariaStopped && !noPlayerPolicy)
                    {
                        try
                        {
                            host.RecordS073LegacyDiagnosticTick(frame);
                            JObject step = host.StepS073LegacyAriaPolicy(canonicalCatalog, frame, (float)elapsed);
                            bool accepted = (bool?)step["source_accepted"] == true;
                            if (accepted) ariaActionsAccepted++;
                            JToken observation = step["observation"];
                            ariaSteps.Add(new JObject { ["frame"] = frame, ["time_seconds"] = elapsed,
                                ["decision_kind"] = (string)step["decision_kind"], ["action"] = (string)step["action"],
                                ["source_api"] = (string)step["source_api"], ["source_accepted"] = (bool?)step["source_accepted"],
                                ["target_id"] = (int?)step["target_id"], ["stopped"] = (bool?)step["stopped"],
                                ["selected_count"] = (int?)observation?["SelectedCount"],
                                ["available_squads"] = JValue.CreateNull(),
                                ["visible_hostile_combat"] = JValue.CreateNull(),
                                ["unassigned_observation_fields"] = new JArray("AvailableSquads", "VisibleHostileCombat"),
                                ["legacy_squad_admitted"] = new JArray(Enumerable.Range(0, 5)
                                    .Select(slot => (bool?)observation?["Squad" + slot]?["Admitted"])),
                                ["legacy_group_count"] = (int?)observation?["ExpandedTotalGroups"],
                                ["legacy_structure_mask"] = (int?)observation?["ExpandedStructureMask"],
                                ["legacy_assault_mask"] = (int?)observation?["ExpandedAssaultMask"],
                                ["infantry"] = (int?)observation?["Infantry"] });
                            ariaStopped = (bool?)step["stopped"] == true;
                        }
                        catch (Exception error)
                        {
                            ariaFailure = error.GetType().Name + ": " + error.Message;
                            ariaStopped = true;
                        }
                    }
                    if (live.Phase != SkirmishPhase.Playing || live.Outcome != SkirmishOutcome.None) break;
                }
            }
            }
            catch (Exception error)
            {
                simulationAdvanceWatch.Stop();
                simulationAdvanceWallSeconds = simulationAdvanceWatch.Elapsed.TotalSeconds;
                error.Data["S073SimulationAdvanceWallSeconds"] = simulationAdvanceWallSeconds.Value;
                error.Data["S073CompletedSchedulerTicks"] = ticksExecuted;
                throw;
            }
            finally
            {
                if (simulationAdvanceWatch.IsRunning) simulationAdvanceWatch.Stop();
                simulationAdvanceWallSeconds = simulationAdvanceWatch.Elapsed.TotalSeconds;
            }
        }
        catch (Exception error)
        {
            if (simulationAdvanceWallSeconds.HasValue)
                error.Data["S073SimulationAdvanceWallSeconds"] = simulationAdvanceWallSeconds.Value;
            throw;
        }
        finally
        {
            try
            {
            if (targetingCreated) { targetingState.Begin(); try { targeting.OnDestroy(ref targetingState); } finally { targetingState.End(); } }
            if (combatCreated) { combatState.Begin(); try { combatOrders.OnDestroy(ref combatState); } finally { combatState.End(); } }
            if (attacksCreated) { attackState.Begin(); try { attacks.OnDestroy(ref attackState); } finally { attackState.End(); } }
            if (defenseCreated) { buildingDefenseState.Begin(); try { buildingDefense.OnDestroy(ref buildingDefenseState); } finally { buildingDefenseState.End(); } }
            if (deathsCreated) { deathState.Begin(); try { deaths.OnDestroy(ref deathState); } finally { deathState.End(); } }
            if (schedulerEndSimulationEntity != Entity.Null && em.Exists(schedulerEndSimulationEntity))
            {
                schedulerEndSimulationBuffers?.Playback(em);
                em.DestroyEntity(schedulerEndSimulationEntity);
            }
            }
            catch (Exception error)
            {
                if (simulationAdvanceWallSeconds.HasValue)
                    error.Data["S073SimulationAdvanceWallSeconds"] = simulationAdvanceWallSeconds.Value;
                throw;
            }
        }

        using var finalMatch = em.CreateEntityQuery(typeof(SkirmishMatchState));
        SkirmishMatchState final = finalMatch.GetSingleton<SkirmishMatchState>();
        productionDecisionInputsAfter = CaptureProductionDecisionInputs(em);
        bool latestAllGates = lastRows != null && lastRows.OfType<JObject>().All(row =>
            (bool?)row["required_queries_present"] == true && (bool?)row["enabled"] == true &&
            (bool?)row["on_update_invoked"] == true);
        return new JObject { ["status"] = ticksExecuted > 0 ? "RecurringOriginalOwnerTicksExecuted" : "NoTickExecuted",
            ["scope"] = noPlayerPolicy
                ? "SameWorldOriginalLegacyAI_Movement_Engagement_Attack_Defense_Death_Rules_NoPlayerPolicy_1over60_NativeReadinessUnverified"
                : "SameWorldOriginalLegacyAI_Movement_Engagement_Attack_Defense_Death_RulesAndSharedAria_1over60_NativeReadinessUnverified",
            ["simulation_advance_wall_seconds"] = simulationAdvanceWallSeconds.HasValue
                ? JToken.FromObject(simulationAdvanceWallSeconds.Value) : JValue.CreateNull(),
            ["simulation_advance_timing_status"] = simulationAdvanceWallSeconds.HasValue ? "Measured" : "NotMeasured_LoopNotEntered",
            ["simulation_advance_timing_scope"] = "Stopwatch around recurring tick loop only; excludes owner creation, ground-owner setup, command-buffer cleanup and result serialization",
            ["tick_budget"] = tickBudget == 0 ? JToken.FromObject("until_original_terminal_outcome") : JToken.FromObject(tickBudget),
            ["ticks_executed"] = ticksExecuted,
            ["elapsed_seconds"] = em.WorldUnmanaged.Time.ElapsedTime, ["delta_seconds"] = delta,
            ["owner_update_counts"] = JObject.FromObject(ownerCounts),
            ["original_production_runtime"] = productionRuntime.Snapshot(), ["path_owner_updates"] = pathUpdates,
            ["grid_movement_owner_updates"] = gridMovementUpdates, ["unit_attack_owner_updates"] = attackUpdates,
            ["building_defense_owner_updates"] = buildingDefenseUpdates, ["unit_death_owner_updates"] = deathUpdates,
            ["unit_engagement_owner_updates"] = engagementUpdates, ["engaged_movement_owner_updates"] = engagedMovementUpdates,
            ["ai_targeting_owner_updates"] = targetingUpdates, ["ai_combat_order_owner_updates"] = combatOrderUpdates,
            ["rules_owner_updates"] = rulesUpdates, ["last_owner_queries"] = lastRows,
            ["source_world_after_faction_control_owner"] = lastStateAfterControl,
            ["source_world_after_squad_owner"] = lastStateAfterSquads,
            ["ai_production_decision_inputs_before"] = productionDecisionInputsBefore,
            ["ai_production_decision_inputs_after"] = productionDecisionInputsAfter,
#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL || WARLINE_SOURCE_S073_RESOURCE_CENSUS
            ["production_owner_phase_trace"] = productionPhaseTrace,
            ["production_owner_phase_trace_opt_in"] = SourceS073ResourceEconomyRuntimeCensus.Enabled,
            ["production_owner_phase_trace_scope"] = "ReadOnlySameWorldBeforeAfterOriginalAIProductionSystemAndActualRequestConsumer_OnChangesAndTicks898To903",
#endif
            ["no_player_policy_diagnostic"] = noPlayerPolicy,
            ["aria_steps"] = ariaSteps, ["aria_actions_accepted"] = ariaActionsAccepted,
            ["aria_failure"] = ariaFailure, ["aria_stopped"] = ariaStopped,
            ["final_closed_owner_query_inputs"] = CaptureClosedOwnerQueryInputs(em),
            ["all_original_owner_gates_open"] = latestAllGates,
            ["terminal_phase"] = final.Phase.ToString(), ["terminal_outcome"] = final.Outcome.ToString(),
            ["session_id"] = final.SessionId.ToString(), ["world_sequence"] = em.WorldUnmanaged.SequenceNumber,
            ["mission_ready"] = false, ["physical_source_validated"] = false };
    }

    internal static JObject RunOneTick(EntityManager em, ref SkirmishRulesSystem rules, ref SystemState rulesState)
    {
        if (em == null || em.World == null || !em.World.IsCreated)
            throw new NotSupportedException("Live S073 source-host EntityManager required");
        using (var matches = em.CreateEntityQuery(typeof(SkirmishMatchState)))
        using (var expanded = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)))
        {
            if (matches.CalculateEntityCount() != 1 || expanded.CalculateEntityCount() != 0)
                throw new NotSupportedException("Exactly one non-expanded original legacy match required");
            var session = matches.GetSingletonEntity();
            var match = em.GetComponentData<SkirmishMatchState>(session);
            if (match.Phase != SkirmishPhase.Playing || match.Outcome != SkirmishOutcome.None)
                throw new NotSupportedException("Original S073 tactical owners require the live Playing session");
        }

        const float delta = 1f / 60f;
        double elapsed = em.WorldUnmanaged.Time.ElapsedTime + delta;
        em.WorldUnmanaged.Time = new TimeData { ElapsedTime = elapsed, DeltaTime = delta };

        var economy = new AIEconomySystem();
        var faction = new AIFactionControlSystem();
        var planner = new AIBuildPlannerSystem();
        var production = new AIProductionSystem();
        var squads = new AISquadSystem();
        var targeting = new AITargetingSystem();
        var combatOrders = new AICombatOrderSystem();
        var economyState = new SystemState { EntityManager = em, Enabled = true };
        var factionState = new SystemState { EntityManager = em, Enabled = true };
        var plannerState = new SystemState { EntityManager = em, Enabled = true };
        var productionState = new SystemState { EntityManager = em, Enabled = true };
        var squadState = new SystemState { EntityManager = em, Enabled = true };
        var targetingState = new SystemState { EntityManager = em, Enabled = true };
        var combatState = new SystemState { EntityManager = em, Enabled = true };
        var rows = new JArray();

        economyState.Begin(); try { economy.OnCreate(ref economyState); } finally { economyState.End(); }
        factionState.Begin(); try { faction.OnCreate(ref factionState); } finally { factionState.End(); }
        plannerState.Begin(); try { planner.OnCreate(ref plannerState); } finally { plannerState.End(); }
        productionState.Begin(); try { production.OnCreate(ref productionState); } finally { productionState.End(); }
        squadState.Begin(); try { squads.OnCreate(ref squadState); } finally { squadState.End(); }
        targetingState.Begin(); try { targeting.OnCreate(ref targetingState); } finally { targetingState.End(); }
        combatState.Begin(); try { combatOrders.OnCreate(ref combatState); } finally { combatState.End(); }
        rows.Add(new JObject { ["owner"] = "AIEconomySystem", ["required_queries_present"] = economyState.RequiredQueriesPresent, ["enabled"] = economyState.Enabled, ["stage"] = "Created" });
        rows.Add(new JObject { ["owner"] = "AIFactionControlSystem", ["required_queries_present"] = factionState.RequiredQueriesPresent, ["enabled"] = factionState.Enabled, ["stage"] = "Created" });
        rows.Add(new JObject { ["owner"] = "AIBuildPlannerSystem", ["required_queries_present"] = plannerState.RequiredQueriesPresent, ["enabled"] = plannerState.Enabled, ["stage"] = "Created" });
        rows.Add(new JObject { ["owner"] = "AIProductionSystem", ["required_queries_present"] = productionState.RequiredQueriesPresent, ["enabled"] = productionState.Enabled, ["stage"] = "Created" });
        rows.Add(new JObject { ["owner"] = "AISquadSystem", ["required_queries_present"] = squadState.RequiredQueriesPresent, ["enabled"] = squadState.Enabled, ["stage"] = "Created" });
        rows.Add(new JObject { ["owner"] = "AITargetingSystem", ["required_queries_present"] = targetingState.RequiredQueriesPresent, ["enabled"] = targetingState.Enabled, ["stage"] = "Created" });
        rows.Add(new JObject { ["owner"] = "AICombatOrderSystem", ["required_queries_present"] = combatState.RequiredQueriesPresent, ["enabled"] = combatState.Enabled, ["stage"] = "Created" });

        bool economyEnabled = economyState.Enabled;
        bool factionEnabled = factionState.Enabled;
        bool plannerEnabled = plannerState.Enabled;
        bool productionEnabled = productionState.Enabled;
        bool squadEnabled = squadState.Enabled;
        bool targetingEnabled = targetingState.Enabled;
        bool combatEnabled = combatState.Enabled;
        bool economyRequired = false, factionRequired = false, plannerRequired = false;
        bool productionRequired = false, squadRequired = false, targetingRequired = false, combatRequired = false;
        JObject afterFactionControl = null;
        JObject afterSquadOwner = null;
        bool economyInvoked = false, factionInvoked = false, plannerInvoked = false, productionInvoked = false;
        bool squadInvoked = false, targetingInvoked = false, combatInvoked = false;
        // Query requirements are live ECS queries. Earlier owners can create a
        // component that opens a downstream owner's gate (AISquadSystem creates
        // AISquad for targeting and combat). Re-evaluate each gate immediately
        // before its original update instead of retaining the OnCreate snapshot.
        economyState.Begin(); try { economyRequired = economyState.RequiredQueriesPresent; if (economyEnabled && economyRequired) { economy.OnUpdate(ref economyState); economyInvoked = true; } } finally { economyState.End(); }
        factionState.Begin(); try { factionRequired = factionState.RequiredQueriesPresent; if (factionEnabled && factionRequired) { faction.OnUpdate(ref factionState); factionInvoked = true; } } finally { factionState.End(); }
        afterFactionControl = CaptureS073TacticalOwnerState(em);
        plannerState.Begin(); try { plannerRequired = plannerState.RequiredQueriesPresent; if (plannerEnabled && plannerRequired) { planner.OnUpdate(ref plannerState); plannerInvoked = true; } } finally { plannerState.End(); }
        productionState.Begin(); try { productionRequired = productionState.RequiredQueriesPresent; if (productionEnabled && productionRequired) { production.OnUpdate(ref productionState); productionInvoked = true; } } finally { productionState.End(); }
        squadState.Begin(); try { squadRequired = squadState.RequiredQueriesPresent; if (squadEnabled && squadRequired) { squads.OnUpdate(ref squadState); squadInvoked = true; } } finally { squadState.End(); }
        afterSquadOwner = CaptureS073TacticalOwnerState(em);
        targetingState.Begin(); try { targetingRequired = targetingState.RequiredQueriesPresent; if (targetingEnabled && targetingRequired) { targeting.OnUpdate(ref targetingState); targetingInvoked = true; } } finally { targetingState.End(); }
        combatState.Begin(); try { combatRequired = combatState.RequiredQueriesPresent; if (combatEnabled && combatRequired) { combatOrders.OnUpdate(ref combatState); combatInvoked = true; } } finally { combatState.End(); }
        rows.Add(new JObject { ["owner"] = "AIEconomySystem", ["required_queries_present"] = economyRequired, ["enabled"] = economyEnabled, ["on_update_invoked"] = economyInvoked, ["stage"] = "UpdatedOrGated" });
        rows.Add(new JObject { ["owner"] = "AIFactionControlSystem", ["required_queries_present"] = factionRequired, ["enabled"] = factionEnabled, ["on_update_invoked"] = factionInvoked, ["stage"] = "UpdatedOrGated" });
        rows.Add(new JObject { ["owner"] = "AIBuildPlannerSystem", ["required_queries_present"] = plannerRequired, ["enabled"] = plannerEnabled, ["on_update_invoked"] = plannerInvoked, ["stage"] = "UpdatedOrGated" });
        rows.Add(new JObject { ["owner"] = "AIProductionSystem", ["required_queries_present"] = productionRequired, ["enabled"] = productionEnabled, ["on_update_invoked"] = productionInvoked, ["stage"] = "UpdatedOrGated" });
        rows.Add(new JObject { ["owner"] = "AISquadSystem", ["required_queries_present"] = squadRequired, ["enabled"] = squadEnabled, ["on_update_invoked"] = squadInvoked, ["stage"] = "UpdatedOrGated" });
        rows.Add(new JObject { ["owner"] = "AITargetingSystem", ["required_queries_present"] = targetingRequired, ["enabled"] = targetingEnabled, ["on_update_invoked"] = targetingInvoked, ["stage"] = "UpdatedOrGated" });
        rows.Add(new JObject { ["owner"] = "AICombatOrderSystem", ["required_queries_present"] = combatRequired, ["enabled"] = combatEnabled, ["on_update_invoked"] = combatInvoked, ["stage"] = "UpdatedOrGated" });

        bool rulesRequired = rulesState.RequiredQueriesPresent;
        bool rulesEnabled = rulesState.Enabled;
        bool rulesInvoked = false;
        rulesState.Begin();
        try
        {
            if (rulesEnabled && rulesRequired) { rules.OnUpdate(ref rulesState); rulesInvoked = true; }
        }
        finally { rulesState.End(); }
        rows.Add(new JObject { ["owner"] = "SkirmishRulesSystem", ["required_queries_present"] = rulesRequired,
            ["enabled"] = rulesEnabled, ["on_update_invoked"] = rulesInvoked, ["stage"] = "UpdatedOrGated" });

        bool allGates = rows.OfType<JObject>().Where(row => (string)row["stage"] == "UpdatedOrGated").All(row =>
            (bool?)row["required_queries_present"] == true && (bool?)row["enabled"] == true &&
            (bool?)row["on_update_invoked"] == true);
        return new JObject {
            ["status"] = allGates ? "OriginalOwnerTickPassed" : "Partial_OriginalOwnerQueriesClosed",
            ["scope"] = "SameWorldOriginalLegacyAIAndRulesOwners_SourceHostCadence_1over60_NativeReadinessUnverified",
            ["tick"] = 1, ["elapsed_seconds"] = elapsed, ["delta_seconds"] = delta,
            ["original_owner_call_order"] = new JArray("AIEconomySystem", "AIFactionControlSystem", "AIBuildPlannerSystem",
                "AIProductionSystem", "AISquadSystem", "AITargetingSystem", "AICombatOrderSystem", "SkirmishRulesSystem"),
            ["owners"] = rows, ["all_original_owner_gates_open"] = allGates,
            ["source_world_after_faction_control_owner"] = afterFactionControl,
            ["source_world_after_squad_owner"] = afterSquadOwner,
            ["physical_source_validated"] = false, ["mission_ready"] = false
        };
    }

    private static JObject CaptureS073TacticalOwnerState(EntityManager em)
    {
        using var eligible = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitHealth>(), ComponentType.ReadOnly<AIControlledTag>() },
            None = new[] { ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>(),
                ComponentType.ReadOnly<SkirmishBaseDefender>(), ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
                ComponentType.ReadOnly<UnitResourceHauler>() }
        });
        using var entities = eligible.ToEntityArray(Allocator.Temp);
        var candidateCounts = new SortedDictionary<byte, int>();
        foreach (Entity entity in entities)
        {
            Faction faction = em.GetComponentData<Faction>(entity);
            UnitHealth health = em.GetComponentData<UnitHealth>(entity);
            if (health.Current <= 0) continue;
            candidateCounts.TryGetValue(faction.Id, out int count);
            candidateCounts[faction.Id] = count + 1;
        }

        var plans = new JArray();
        using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<AISquadPlan>()))
        using (var planEntities = query.ToEntityArray(Allocator.Temp))
            foreach (Entity entity in planEntities)
            {
                AISquadPlan plan = em.GetComponentData<AISquadPlan>(entity);
                plans.Add(new JObject { ["faction_id"] = plan.FactionId, ["enabled"] = plan.Enabled != 0,
                    ["min_units"] = plan.MinUnits, ["max_units"] = plan.MaxUnits,
                    ["max_active_squads"] = plan.MaxActiveSquads, ["next_squad_id"] = plan.NextSquadId });
            }

        var controls = new JArray();
        using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<FactionControlConfigTag>()))
        {
            if (query.CalculateEntityCount() == 1)
            {
                Entity entity = query.GetSingletonEntity();
                if (em.HasBuffer<FactionControlEntry>(entity))
                {
                    DynamicBuffer<FactionControlEntry> entries = em.GetBuffer<FactionControlEntry>(entity, true);
                    foreach (FactionControlEntry entry in entries)
                        controls.Add(new JObject { ["faction_id"] = entry.FactionId,
                            ["ai_controlled"] = entry.AIControlled != 0, ["player_faction"] = entry.IsPlayerFaction != 0 });
                }
            }
        }

        using var squads = em.CreateEntityQuery(ComponentType.ReadOnly<AISquad>());
        using var squadEntities = squads.ToEntityArray(Allocator.Temp);
        int squadUnitCount = 0;
        foreach (Entity entity in squadEntities)
            if (em.HasBuffer<AISquadUnit>(entity)) squadUnitCount += em.GetBuffer<AISquadUnit>(entity, true).Length;

        return new JObject { ["eligible_live_ai_unit_counts_by_faction"] = JObject.FromObject(candidateCounts),
            ["faction_control_entries"] = controls, ["squad_plans"] = plans,
            ["aisquad_entities"] = squadEntities.Length, ["aisquadunit_elements"] = squadUnitCount,
            ["scope"] = "ReadOnly component snapshot after original owner update; no entities or gameplay values injected" };
    }
}
