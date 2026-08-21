using System;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;

namespace Game.Runtime
{
    public static class MissionLaunchPayloadFactory
    {
        public const int CurrentSchemaVersion = 1;
        private const string AlwaysGuidedMissionId = "saga.ch01.m01.first_contact";

        public static MissionLaunchPayload Create(
            string missionId,
            string scenarioId,
            string operationMapId,
            MissionLaunchOriginKind launchOrigin,
            MissionRunKind runKind,
            NarrativeGuidanceMode guidance,
            bool replayTutorialEnabled,
            ulong transitionToken,
            string sessionToken,
            int attemptOrdinal,
            int deterministicSeed)
        {
            ValidateGuidance(guidance);
            replayTutorialEnabled |= string.Equals(
                missionId,
                AlwaysGuidedMissionId,
                StringComparison.Ordinal);
            return new MissionLaunchPayload(
                CurrentSchemaVersion,
                missionId,
                scenarioId,
                operationMapId,
                launchOrigin,
                runKind,
                guidance,
                replayTutorialEnabled,
                transitionToken,
                sessionToken,
                attemptOrdinal,
                deterministicSeed);
        }

        public static MissionLaunchPayload CreateRetry(
            in MissionLaunchPayload previous,
            ulong transitionToken)
        {
            if (previous.SchemaVersion != CurrentSchemaVersion)
                throw new ArgumentException("Only the current mission payload schema can be retried.", nameof(previous));
            if (previous.AttemptOrdinal == int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(previous), "Attempt ordinal cannot be incremented.");

            return Create(
                previous.MissionId,
                previous.ScenarioId,
                previous.OperationMapId,
                previous.LaunchOrigin,
                MissionRunKind.Retry,
                previous.Guidance,
                previous.ReplayTutorialEnabled,
                transitionToken,
                previous.SessionToken,
                previous.AttemptOrdinal + 1,
                previous.DeterministicSeed);
        }

        private static void ValidateGuidance(NarrativeGuidanceMode guidance)
        {
            if (guidance is < NarrativeGuidanceMode.Full or > NarrativeGuidanceMode.Minimal)
                throw new ArgumentOutOfRangeException(nameof(guidance));
        }
    }
}
