using System;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitDeathSystem))]
    public partial struct CampaignMissionRuntimeSystem : ISystem
    {
        private static readonly FixedString64Bytes StaleResultActionReason = "stale-result-action";
        private static readonly FixedString64Bytes UnsupportedResultActionReason = "unsupported-result-action";
        private static readonly FixedString64Bytes ResultNotSettledReason = "result-not-settled";
        private static readonly FixedString64Bytes InvalidResultTransitionReason = "invalid-result-transition";
        private static readonly FixedString64Bytes RetryUnavailableReason = "retry-unavailable";
        private static readonly FixedString64Bytes RetryAlreadyQueuedReason = "retry-already-queued";
        private static readonly FixedString64Bytes MoveTargetAnchorId = "anchor.ch01.m01.move_target";

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CampaignMissionRootComponent>();
            state.RequireForUpdate<CampaignMissionRuntimeComponent>();
            state.RequireForUpdate<CampaignMissionAttemptFactsComponent>();
            state.RequireForUpdate<CampaignMissionActionRequestElement>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingletonEntity<CampaignMissionRootComponent>(out Entity root) &&
                TryConsumeAction(state.EntityManager, root))
                return;

            if (!SystemAPI.TryGetSingleton(out CampaignMissionRuntimeComponent activeRuntime) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionAttemptFactsComponent _))
                return;

            RefRW<CampaignMissionAttemptFactsComponent> factsRw =
                SystemAPI.GetSingletonRW<CampaignMissionAttemptFactsComponent>();
            CampaignMissionAttemptFactsComponent projectedFacts = factsRw.ValueRO;
            bool commandSquadSelected = projectedFacts.CommandSquadSpawned != 0;
            if (activeRuntime.Outcome == MissionOutcomeKind.None &&
                activeRuntime.Phase is >= MissionPhaseKind.FindSquad and <= MissionPhaseKind.SecureCorridor &&
                projectedFacts.CommandSquadSpawned != 0)
            {
                float3 moveTarget = default;
                float moveRadius = 0f;
                OperationMapAnchorBlob moveAnchor = default;
                bool hasMoveTarget = SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) &&
                    metadata.Blob.IsCreated && CampaignMissionSpawnSystem.TryFindAnchor(
                        ref metadata.Blob.Value, MoveTargetAnchorId, out moveAnchor);
                if (hasMoveTarget)
                {
                    moveTarget = moveAnchor.Position;
                    moveRadius = math.max(0.25f, moveAnchor.Radius);
                }
                ComponentLookup<EngageTarget> engageTargets = SystemAPI.GetComponentLookup<EngageTarget>(true);
                ComponentLookup<CampaignMissionUnitRoleComponent> roles =
                    SystemAPI.GetComponentLookup<CampaignMissionUnitRoleComponent>(true);
                ComponentLookup<Faction> factions = SystemAPI.GetComponentLookup<Faction>(true);
                ComponentLookup<UnitHealth> health = SystemAPI.GetComponentLookup<UnitHealth>(true);
                ComponentLookup<SelectedUnitTag> selectedUnits = SystemAPI.GetComponentLookup<SelectedUnitTag>(true);
                int aliveFriendly = 0;
                int aliveHostile = 0;
                int expectedFriendly = 0;
                bool moveTargetReached = false;
                bool commandedHostileAttack = false;

                if (SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) &&
                    CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in activeRuntime, out int definitionIndex))
                {
                    ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
                    expectedFriendly = CountFriendlyUnits(ref definition);
                }

                foreach ((RefRO<CampaignMissionUnitRoleComponent> role,
                          RefRO<Faction> faction,
                          RefRO<UnitHealth> unitHealth,
                          RefRO<LocalTransform> transform,
                          Entity entity)
                         in SystemAPI.Query<RefRO<CampaignMissionUnitRoleComponent>, RefRO<Faction>,
                             RefRO<UnitHealth>, RefRO<LocalTransform>>().WithEntityAccess())
                {
                    if (!role.ValueRO.SessionToken.Equals(activeRuntime.SessionToken) || unitHealth.ValueRO.Current <= 0)
                        continue;
                    if (faction.ValueRO.Id > 1)
                    {
                        aliveHostile++;
                        continue;
                    }
                    aliveFriendly++;
                    commandSquadSelected &= selectedUnits.HasComponent(entity);
                    float2 offset = transform.ValueRO.Position.xz - moveTarget.xz;
                    if (hasMoveTarget && math.lengthsq(offset) <= moveRadius * moveRadius)
                        moveTargetReached = true;
                    if (!engageTargets.HasComponent(entity))
                        continue;
                    EngageTarget target = engageTargets[entity];
                    if (target.IsCommanded == 0 || target.Target == Entity.Null ||
                        !roles.HasComponent(target.Target) || !factions.HasComponent(target.Target) ||
                        !health.HasComponent(target.Target) || health[target.Target].Current <= 0 ||
                        !roles[target.Target].SessionToken.Equals(activeRuntime.SessionToken) ||
                        factions[target.Target].Id <= 1)
                        continue;
                    commandedHostileAttack = true;
                }

                projectedFacts.ElapsedMilliseconds = SaturatingAddMilliseconds(
                    projectedFacts.ElapsedMilliseconds, SystemAPI.Time.DeltaTime);
                projectedFacts.CommandSquadAlive = aliveFriendly > 0 ? (byte)1 : (byte)0;
                if (expectedFriendly > 0)
                {
                    projectedFacts.SquadLossCount = math.max(
                        projectedFacts.SquadLossCount, math.max(0, expectedFriendly - aliveFriendly));
                }
                int defeated = math.clamp(projectedFacts.HostileTotalCount - aliveHostile,
                    0, projectedFacts.HostileTotalCount);
                projectedFacts.HostileDefeatedCount = math.max(projectedFacts.HostileDefeatedCount, defeated);
                if (moveTargetReached)
                    projectedFacts.MoveToCoverComplete = 1;
                if (commandedHostileAttack)
                {
                    projectedFacts.ThreatConfirmed = 1;
                    projectedFacts.AttackIssued = 1;
                }
                commandSquadSelected &= aliveFriendly > 0;
                factsRw.ValueRW = projectedFacts;
            }
            foreach ((RefRW<CampaignMissionRuntimeComponent> runtime,
                      RefRO<CampaignMissionAttemptFactsComponent> facts)
                     in SystemAPI.Query<RefRW<CampaignMissionRuntimeComponent>,
                         RefRO<CampaignMissionAttemptFactsComponent>>())
            {
                CampaignMissionRuntimeComponent next = runtime.ValueRO;
                if (!TryEvaluate(in runtime.ValueRO, in facts.ValueRO, commandSquadSelected, out next))
                    continue;
                runtime.ValueRW = next;
            }
        }
        private static int SaturatingAddMilliseconds(int current, float deltaSeconds)
        {
            int delta = (int)math.min(int.MaxValue, math.max(0f, math.round(deltaSeconds * 1000f)));
            return current >= int.MaxValue - delta ? int.MaxValue : current + delta;
        }
        private static int CountFriendlyUnits(ref CampaignMissionDefinitionBlob definition)
        {
            int count = 0;
            for (int groupIndex = 0; groupIndex < definition.ForceGroups.Length; groupIndex++)
            {
                ref CampaignMissionForceGroupBlob group = ref definition.ForceGroups[groupIndex];
                if (group.FactionId > 1)
                    continue;
                for (int unitIndex = 0; unitIndex < group.Units.Length; unitIndex++)
                    count += group.Units[unitIndex].Count;
            }
            return count;
        }
        internal static bool TryConsumeAction(EntityManager entityManager, Entity root)
        {
            if (!entityManager.HasComponent<CampaignMissionRuntimeComponent>(root) ||
                !entityManager.HasBuffer<CampaignMissionActionRequestElement>(root) ||
                !entityManager.HasBuffer<CampaignMissionActionResultElement>(root))
                return false;
            DynamicBuffer<CampaignMissionActionRequestElement> requests =
                entityManager.GetBuffer<CampaignMissionActionRequestElement>(root);
            if (requests.Length == 0)
                return false;

            CampaignMissionActionRequestElement request = requests[0];
            requests.RemoveAt(0);
            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            bool correlated = request.TransitionToken == runtime.TransitionToken &&
                              request.SessionToken.Equals(runtime.SessionToken) &&
                              request.AttemptOrdinal == runtime.AttemptOrdinal;
            bool accepted = false;
            FixedString64Bytes reason = default;
            if (!correlated || runtime.Phase != MissionPhaseKind.Result)
                reason = StaleResultActionReason;
            else if (request.Action == MissionActionKind.Continue)
                accepted = TryContinue(entityManager, root, ref runtime, out reason);
            else if (request.Action == MissionActionKind.Retry)
                accepted = TryQueueRetry(entityManager, root, in runtime, in request, out reason);
            else
                reason = UnsupportedResultActionReason;

            if (accepted && request.Action == MissionActionKind.Continue)
                entityManager.SetComponentData(root, runtime);
            entityManager.GetBuffer<CampaignMissionActionResultElement>(root).Add(
                new CampaignMissionActionResultElement
                {
                    Action = request.Action,
                    Accepted = accepted ? (byte)1 : (byte)0,
                    TransitionToken = request.TransitionToken,
                    SessionToken = request.SessionToken,
                    AttemptOrdinal = request.AttemptOrdinal,
                    ReasonCode = reason
                });
            return true;
        }
        [BurstDiscard]
        public static void TryConsumeActionManaged(
            EntityManager entityManager, Entity root, ref bool consumed)
        {
            if (!entityManager.HasComponent<CampaignMissionRuntimeComponent>(root) ||
                !entityManager.HasBuffer<CampaignMissionActionRequestElement>(root) ||
                !entityManager.HasBuffer<CampaignMissionActionResultElement>(root))
                return;
            DynamicBuffer<CampaignMissionActionRequestElement> requests =
                entityManager.GetBuffer<CampaignMissionActionRequestElement>(root);
            if (requests.Length == 0 || requests[0].Action != MissionActionKind.Exit)
            {
                consumed = TryConsumeAction(entityManager, root);
                return;
            }

            CampaignMissionActionRequestElement request = requests[0];
            requests.RemoveAt(0);
            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            bool correlated = request.TransitionToken == runtime.TransitionToken &&
                              request.SessionToken.Equals(runtime.SessionToken) &&
                              request.AttemptOrdinal == runtime.AttemptOrdinal;
            FixedString64Bytes reason = default;
            bool accepted = correlated && TryExit(entityManager, root, in runtime, out reason);
            if (!correlated)
                reason = new FixedString64Bytes("stale-exit-action");
            entityManager.GetBuffer<CampaignMissionActionResultElement>(root).Add(
                new CampaignMissionActionResultElement
                {
                    Action = request.Action,
                    Accepted = accepted ? (byte)1 : (byte)0,
                    TransitionToken = request.TransitionToken,
                    SessionToken = request.SessionToken,
                    AttemptOrdinal = request.AttemptOrdinal,
                    ReasonCode = reason
                });
            consumed = true;
        }
        private static bool TryExit(
            EntityManager entityManager, Entity root, in CampaignMissionRuntimeComponent runtime,
            out FixedString64Bytes reason)
        {
            reason = default;
            if (runtime.Phase < MissionPhaseKind.Preparing || runtime.Phase > MissionPhaseKind.SecureCorridor ||
                !entityManager.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))
            {
                reason = new FixedString64Bytes("exit-unavailable");
                return false;
            }

            CampaignMissionProgressStore store = entityManager
                .GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store;
            try
            {
                if (store == null)
                    throw new InvalidOperationException("Campaign mission progress store is unavailable.");
                store.SetPendingResume(runtime.MissionId.ToString(), true, runtime.AttemptOrdinal);
            }
            catch (Exception)
            {
                reason = new FixedString64Bytes("exit-persistence-failed");
                return false;
            }

            EntityCommandBuffer cleanup = new(Allocator.Temp);
            CampaignMissionLaunchSystem.QueueAttemptCleanup(entityManager, ref cleanup, root);
            cleanup.Playback(entityManager);
            cleanup.Dispose();
            if (entityManager.HasComponent<CampaignMissionAttemptFactsComponent>(root))
                entityManager.SetComponentData(root, default(CampaignMissionAttemptFactsComponent));
            entityManager.SetComponentData(root, default(CampaignMissionRuntimeComponent));
            return true;
        }
        private static bool TryContinue(
            EntityManager entityManager, Entity root, ref CampaignMissionRuntimeComponent runtime,
            out FixedString64Bytes reason)
        {
            reason = default;
            if (runtime.Outcome != MissionOutcomeKind.Victory ||
                !entityManager.HasBuffer<CampaignMissionSettlementResultElement>(root))
            {
                reason = ResultNotSettledReason;
                return false;
            }
            DynamicBuffer<CampaignMissionSettlementResultElement> settlements =
                entityManager.GetBuffer<CampaignMissionSettlementResultElement>(root, true);
            bool settled = false;
            for (int index = settlements.Length - 1; index >= 0; index--)
            {
                CampaignMissionSettlementResultElement candidate = settlements[index];
                if (candidate.SourceVersion == runtime.Version &&
                    candidate.SessionToken.Equals(runtime.SessionToken) && candidate.Accepted != 0)
                {
                    settled = true;
                    break;
                }
            }
            if (!settled)
            {
                reason = ResultNotSettledReason;
                return false;
            }
            MissionPhaseKind nextPhase = runtime.ReturnDestination == MissionReturnDestinationKind.CommandBase
                ? MissionPhaseKind.DebriefFirstClear : MissionPhaseKind.ReturnReplay;
            if (!TryTransition(in runtime, nextPhase, runtime.Outcome, runtime.ReturnDestination, out runtime))
            {
                reason = InvalidResultTransitionReason;
                return false;
            }
            return true;
        }
        private static bool TryQueueRetry(
            EntityManager entityManager, Entity root, in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionActionRequestElement action, out FixedString64Bytes reason)
        {
            reason = default;
            if (runtime.Outcome != MissionOutcomeKind.Defeat || runtime.TransitionToken == ulong.MaxValue ||
                runtime.AttemptOrdinal == int.MaxValue ||
                !entityManager.HasBuffer<CampaignMissionLaunchRequestElement>(root))
            {
                reason = RetryUnavailableReason;
                return false;
            }
            DynamicBuffer<CampaignMissionLaunchRequestElement> launches =
                entityManager.GetBuffer<CampaignMissionLaunchRequestElement>(root);
            if (launches.Length != 0)
            {
                reason = RetryAlreadyQueuedReason;
                return false;
            }
            launches.Add(new CampaignMissionLaunchRequestElement
            {
                SchemaVersion = MissionLaunchPayloadFactory.CurrentSchemaVersion,
                MissionId = runtime.MissionId,
                ScenarioId = runtime.ScenarioId,
                OperationMapId = runtime.OperationMapId,
                LaunchOrigin = runtime.LaunchOrigin,
                RunKind = MissionRunKind.Retry,
                Guidance = runtime.Guidance,
                ReplayTutorialEnabled = action.ReplayTutorialEnabled,
                TransitionToken = runtime.TransitionToken + 1ul,
                SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal + 1,
                DeterministicSeed = runtime.DeterministicSeed
            });
            return true;
        }
        public static bool TryEvaluate(
            in CampaignMissionRuntimeComponent current,
            in CampaignMissionAttemptFactsComponent facts,
            out CampaignMissionRuntimeComponent next)
            => TryEvaluate(in current, in facts, false, out next);

        public static bool TryEvaluate(
            in CampaignMissionRuntimeComponent current, in CampaignMissionAttemptFactsComponent facts,
            bool commandSquadSelected, out CampaignMissionRuntimeComponent next)
        {
            next = current;
            if (!TryResolveAutomaticTransition(in current, in facts, commandSquadSelected,
                    out MissionPhaseKind phase,
                    out MissionOutcomeKind outcome, out MissionReturnDestinationKind destination))
                return false;
            return TryTransition(in current, phase, outcome, destination, out next);
        }
        public static bool TryTransition(
            in CampaignMissionRuntimeComponent current,
            MissionPhaseKind phase,
            MissionOutcomeKind outcome,
            MissionReturnDestinationKind destination,
            out CampaignMissionRuntimeComponent next)
        {
            next = current;
            if (!IsValidState(in current) || !IsValidTransition(current.Phase, phase) ||
                !IsValidOutcome(phase, outcome, destination) || current.Version == uint.MaxValue)
                return false;

            if (current.Outcome != MissionOutcomeKind.None &&
                (current.Phase != MissionPhaseKind.Result || phase == MissionPhaseKind.Result ||
                 outcome != current.Outcome || destination != current.ReturnDestination))
                return false;

            if (current.Phase == phase && current.Outcome == outcome &&
                current.ReturnDestination == destination)
                return false;

            next.Phase = phase;
            next.Outcome = outcome;
            next.ReturnDestination = destination;
            next.Version = current.Version + 1u;
            return true;
        }

        public static bool IsValidTransition(MissionPhaseKind from, MissionPhaseKind to)
        {
            if (from == to)
                return true;
            return from switch
            {
                MissionPhaseKind.Preparing => to == MissionPhaseKind.InteractiveBrief,
                MissionPhaseKind.InteractiveBrief => to == MissionPhaseKind.FindSquad,
                MissionPhaseKind.FindSquad => to is MissionPhaseKind.MoveToCover or
                    MissionPhaseKind.Engage or MissionPhaseKind.Result,
                MissionPhaseKind.MoveToCover => to is MissionPhaseKind.ConfirmThreat or MissionPhaseKind.Result,
                MissionPhaseKind.ConfirmThreat => to is MissionPhaseKind.Engage or MissionPhaseKind.Result,
                MissionPhaseKind.Engage => to is MissionPhaseKind.SecureCorridor or MissionPhaseKind.Result,
                MissionPhaseKind.SecureCorridor => to == MissionPhaseKind.Result,
                MissionPhaseKind.Result => to is MissionPhaseKind.DebriefFirstClear or MissionPhaseKind.ReturnReplay,
                _ => false
            };
        }

        private static bool TryResolveAutomaticTransition(
            in CampaignMissionRuntimeComponent current,
            in CampaignMissionAttemptFactsComponent facts,
            bool commandSquadSelected,
            out MissionPhaseKind phase,
            out MissionOutcomeKind outcome,
            out MissionReturnDestinationKind destination)
        {
            phase = current.Phase;
            outcome = current.Outcome;
            destination = current.ReturnDestination;
            if (current.Phase == MissionPhaseKind.Preparing &&
                (current.ReadyReadiness & current.RequiredReadiness) == current.RequiredReadiness)
                phase = MissionPhaseKind.InteractiveBrief;
            else if (current.Phase == MissionPhaseKind.InteractiveBrief)
                phase = MissionPhaseKind.FindSquad;
            else if (current.Phase == MissionPhaseKind.FindSquad &&
                     (facts.CommandSquadSpawned == 0 || facts.CommandSquadAlive == 0))
                return ResolveDefeat(out phase, out outcome, out destination);
            else if (current.Phase == MissionPhaseKind.FindSquad && current.RunKind != MissionRunKind.FirstClear &&
                     current.ReplayTutorialEnabled == 0)
                phase = MissionPhaseKind.Engage;
            else if (current.Phase == MissionPhaseKind.FindSquad && commandSquadSelected)
                phase = MissionPhaseKind.MoveToCover;
            else if (current.Phase == MissionPhaseKind.MoveToCover && facts.CommandSquadAlive == 0)
                return ResolveDefeat(out phase, out outcome, out destination);
            else if (current.Phase == MissionPhaseKind.MoveToCover && facts.MoveToCoverComplete != 0)
                phase = MissionPhaseKind.ConfirmThreat;
            else if (current.Phase == MissionPhaseKind.ConfirmThreat && facts.CommandSquadAlive == 0)
                return ResolveDefeat(out phase, out outcome, out destination);
            else if (current.Phase == MissionPhaseKind.ConfirmThreat && facts.ThreatConfirmed != 0)
                phase = MissionPhaseKind.Engage;
            else if (current.Phase == MissionPhaseKind.Engage && facts.CommandSquadAlive == 0)
                return ResolveDefeat(out phase, out outcome, out destination);
            else if (current.Phase == MissionPhaseKind.Engage && facts.HostileTotalCount > 0 &&
                     facts.HostileDefeatedCount >= facts.HostileTotalCount)
                phase = MissionPhaseKind.SecureCorridor;
            else if (current.Phase == MissionPhaseKind.SecureCorridor)
            {
                phase = MissionPhaseKind.Result;
                outcome = MissionOutcomeKind.Victory;
                destination = current.LaunchOrigin == MissionLaunchOriginKind.FirstLaunch
                    ? MissionReturnDestinationKind.CommandBase
                    : MissionReturnDestinationKind.CampaignOperations;
            }
            return phase != current.Phase || outcome != current.Outcome ||
                   destination != current.ReturnDestination;
        }

        private static bool ResolveDefeat(
            out MissionPhaseKind phase,
            out MissionOutcomeKind outcome,
            out MissionReturnDestinationKind destination)
        {
            phase = MissionPhaseKind.Result;
            outcome = MissionOutcomeKind.Defeat;
            destination = MissionReturnDestinationKind.CampaignOperations;
            return true;
        }

        private static bool IsValidState(in CampaignMissionRuntimeComponent state) =>
            state.Version > 0 && state.SourceVersion > 0 && state.AttemptOrdinal >= 0 &&
            state.DeterministicSeed != 0 && !state.MissionId.IsEmpty &&
            !state.ScenarioId.IsEmpty && !state.OperationMapId.IsEmpty &&
            !state.SessionToken.IsEmpty && state.Phase is >= MissionPhaseKind.Preparing and
                <= MissionPhaseKind.ReturnReplay && state.Outcome is >= MissionOutcomeKind.None and
                <= MissionOutcomeKind.Defeat;

        private static bool IsValidOutcome(
            MissionPhaseKind phase,
            MissionOutcomeKind outcome,
            MissionReturnDestinationKind destination)
        {
            if (phase < MissionPhaseKind.Result)
                return outcome == MissionOutcomeKind.None && destination == MissionReturnDestinationKind.None;
            if (phase == MissionPhaseKind.Result)
                return outcome != MissionOutcomeKind.None && destination != MissionReturnDestinationKind.None;
            return outcome == MissionOutcomeKind.Victory && destination != MissionReturnDestinationKind.None;
        }
    }
}
