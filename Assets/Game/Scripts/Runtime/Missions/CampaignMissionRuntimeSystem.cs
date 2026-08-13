using Game.Components;
using Game.Missions.Contracts;
using Unity.Burst;
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
            state.RequireForUpdate<CampaignMissionRuntimeComponent>();
            state.RequireForUpdate<CampaignMissionAttemptFactsComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
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
