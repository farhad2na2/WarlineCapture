using Game.Components;
using Game.Missions.Contracts;

namespace Game.Runtime
{
    internal static class CampaignMissionGroundedSignalRuleUtility
    {
        internal const int RecoveryMilliseconds = 6000;
        internal static bool Matches(in CampaignMissionGroundedSignalState state, in CampaignMissionRuntimeComponent runtime) =>
            state.Initialized != 0 && state.SessionToken.Equals(runtime.SessionToken) &&
            state.AttemptOrdinal == runtime.AttemptOrdinal && state.SourceVersion == runtime.SourceVersion;

        internal static int AdvanceRecovery(int current, int delta, bool canRecover) => !canRecover ? 0 :
            current >= RecoveryMilliseconds - System.Math.Max(0, delta) ? RecoveryMilliseconds : current + System.Math.Max(0, delta);

        internal static bool CanExtract(in CampaignMissionGroundedSignalState state) =>
            state.Failure == GroundedSignalFailure.None && state.Inserted != 0 && state.RelayDisabled != 0 && state.HardwareRecovered != 0;

        internal static bool TryAdvance(in CampaignMissionRuntimeComponent runtime, in CampaignMissionAttemptFactsComponent facts,
            in CampaignMissionGroundedSignalState state, bool ready, bool opening, out CampaignMissionRuntimeComponent next)
        {
            next = runtime;
            if (!Matches(in state, in runtime) || runtime.Outcome != MissionOutcomeKind.None || runtime.Phase >= MissionPhaseKind.Result) return false;
            var projected = facts;
            if (!CanExtract(in state))
            { projected.ExtractionDeparted = 0; projected.ExtractionPassengersDelivered = 0; }
            if (state.Failure != GroundedSignalFailure.None) projected.HostileRosterIntegrityFault = 1;
            return CampaignMissionExtractionRuleUtility.TryAdvance(in runtime, in projected, ready, opening, 2, out next);
        }
    }
}
