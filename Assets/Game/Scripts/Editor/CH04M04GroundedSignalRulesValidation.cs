using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using UnityEngine;

namespace Game.Editor
{
    public static class CH04M04GroundedSignalRulesValidation
    {
        public static void Run()
        {
            Require(CampaignMissionSequence.Next(CampaignMissionSequence.SplitFront) == CampaignMissionSequence.GroundedSignal, "Grounded Signal progression missing");
            Require(SupportProfileMigration.AbilityForGrant(CampaignMissionSequence.GroundedSignal, "reward.ch04.m04.paratrooper_reinforcements_unlock") == "ability.paratrooper_reinforcements", "Canonical paratroopers reward does not unlock");
            Require(SupportProfileMigration.AbilityForGrant(CampaignMissionSequence.GroundedSignal, "reward.ch04.m04.paratroopers_unlock") == "ability.paratrooper_reinforcements", "Existing reward alias does not unlock");
            Require(SupportProfileMigration.AbilityForGrant(CampaignMissionSequence.SplitFront, "reward.ch04.m04.paratroopers_unlock") == string.Empty, "Wrong mission grants paratroopers");
            var runtime = new CampaignMissionRuntimeComponent { MissionId = CampaignMissionSequence.GroundedSignal,
                ScenarioId = "scenario.grounded", OperationMapId = "map.grounded", SessionToken = "grounded-attempt", AttemptOrdinal = 1,
                SourceVersion = 1, Version = 1, DeterministicSeed = 13, Phase = MissionPhaseKind.Engage };
            var state = new CampaignMissionGroundedSignalState { SessionToken = runtime.SessionToken, AttemptOrdinal = 1, SourceVersion = 1, Initialized = 1 };
            var facts = new CampaignMissionAttemptFactsComponent { ExtractionPassengerTotal = 2, ExtractionCarrierLegCount = 2,
                ExtractionPassengersDelivered = 2, ExtractionDeparted = 1 };
            Require(!Advance(runtime, facts, state, out _), "Exit fabricated victory before insertion");
            state.Inserted = 1;
            Require(!Advance(runtime, facts, state, out _), "Exit fabricated victory before relay disable");
            state.RelayDisabled = 1;
            Require(!Advance(runtime, facts, state, out _), "Exit fabricated victory before hardware custody");
            Require(CampaignMissionGroundedSignalRuleUtility.AdvanceRecovery(5000, 1000, false) == 0, "Leaving/boarding/contest did not cancel recovery");
            Require(CampaignMissionGroundedSignalRuleUtility.AdvanceRecovery(5000, 1000, true) == 6000, "Valid recovery did not complete");
            Require(CampaignMissionGroundedSignalRuleUtility.AdvanceRecovery(5000, int.MaxValue, true) == 6000, "Recovery overflowed");
            state.HardwareRecovered = 1;
            Require(Advance(runtime, facts, state, out var victory) && victory.Outcome == MissionOutcomeKind.Victory, "Complete original roster custody rejected");
            Require(!Advance(victory, facts, state, out _), "Result settled twice");
            foreach (GroundedSignalFailure failure in Enum.GetValues(typeof(GroundedSignalFailure)))
            {
                if (failure == GroundedSignalFailure.None) continue;
                var failed = state; failed.Failure = failure;
                Require(Advance(runtime, facts, failed, out var defeat) && defeat.Outcome == MissionOutcomeKind.Defeat, "Failure did not prevent stale exit victory: " + failure);
            }
            var stale = state; stale.SessionToken = "other";
            Require(!Advance(runtime, facts, stale, out _), "Stale session settled");
            stale = state; stale.AttemptOrdinal++;
            Require(!Advance(runtime, facts, stale, out _), "Prior retry state settled");
            stale = state; stale.SourceVersion++;
            Require(!Advance(runtime, facts, stale, out _), "Stale source settled");
            var incomplete = facts; incomplete.ExtractionPassengersDelivered = 1;
            Require(!Advance(runtime, incomplete, state, out _), "One original specialist counted as complete extraction");
            incomplete = facts; incomplete.CivilianLossCount = 1;
            Require(Advance(runtime, incomplete, state, out var loss) && loss.Outcome == MissionOutcomeKind.Defeat, "Specialist loss accepted as victory");
            Debug.Log("[GroundedSignalRules] result=Passed insertion=required relay=required hardware=required recovery=cancel,complete,overflow failures=all stale=session,retry,source result=idempotent originals=both");
        }
        private static bool Advance(in CampaignMissionRuntimeComponent runtime, in CampaignMissionAttemptFactsComponent facts,
            in CampaignMissionGroundedSignalState state, out CampaignMissionRuntimeComponent next) =>
            CampaignMissionGroundedSignalRuleUtility.TryAdvance(in runtime, in facts, in state, true, true, out next);
        private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
    }
}
