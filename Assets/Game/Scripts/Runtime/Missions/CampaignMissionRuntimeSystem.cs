using System;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CampaignMissionRuntimeSystem : ISystem
    {
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

            foreach ((RefRW<CampaignMissionRuntimeComponent> runtime,
                      RefRO<CampaignMissionAttemptFactsComponent> facts)
                     in SystemAPI.Query<RefRW<CampaignMissionRuntimeComponent>,
                         RefRO<CampaignMissionAttemptFactsComponent>>())
            {
                CampaignMissionRuntimeComponent next = runtime.ValueRO;
                if (!TryEvaluate(in runtime.ValueRO, in facts.ValueRO, out next))
                    continue;
                runtime.ValueRW = next;
            }
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
                reason = new FixedString64Bytes("stale-result-action");
            else if (request.Action == MissionActionKind.Continue)
                accepted = TryContinue(entityManager, root, ref runtime, out reason);
            else if (request.Action == MissionActionKind.Retry)
                accepted = TryQueueRetry(entityManager, root, in runtime, in request, out reason);
            else
                reason = new FixedString64Bytes("unsupported-result-action");

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
                reason = new FixedString64Bytes("result-not-settled");
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
                reason = new FixedString64Bytes("result-not-settled");
                return false;
            }
            MissionPhaseKind nextPhase = runtime.ReturnDestination == MissionReturnDestinationKind.CommandBase
                ? MissionPhaseKind.DebriefFirstClear : MissionPhaseKind.ReturnReplay;
            if (!TryTransition(in runtime, nextPhase, runtime.Outcome, runtime.ReturnDestination, out runtime))
            {
                reason = new FixedString64Bytes("invalid-result-transition");
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
                reason = new FixedString64Bytes("retry-unavailable");
                return false;
            }
            DynamicBuffer<CampaignMissionLaunchRequestElement> launches =
                entityManager.GetBuffer<CampaignMissionLaunchRequestElement>(root);
            if (launches.Length != 0)
            {
                reason = new FixedString64Bytes("retry-already-queued");
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
        {
            next = current;
            if (!TryResolveAutomaticTransition(in current, in facts, out MissionPhaseKind phase,
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

            // A published result is immutable, but its presentation may advance to the
            // matching debrief/return phase while preserving the exact outcome and route.
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
            else if (current.Phase == MissionPhaseKind.FindSquad &&
                     (facts.CommandSquadSpawned == 0 || facts.CommandSquadAlive == 0))
                return ResolveDefeat(out phase, out outcome, out destination);
            else if (current.Phase == MissionPhaseKind.FindSquad &&
                     current.ReplayTutorialEnabled == 0)
                phase = MissionPhaseKind.Engage;
            else if (current.Phase == MissionPhaseKind.FindSquad && facts.CommandSquadAlive != 0)
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
