using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public enum FirstLaunchMissionHandoffState : byte { Pending, Accepted, Rejected }

    public static class FirstLaunchMissionHandoffOperation
    {
        public const string MissionId = "saga.ch01.m01.first_contact";
        public const string ScenarioId = "scenario.ch01.m01.first_contact";
        public const string OperationMapId = "opmap.ch01.district_edge_01";
        public const int DeterministicSeed = 1001001;

        public static MissionLaunchPayload Prepare(PlayerProfileSaveData profile, ulong narrativeToken, NarrativeGuidanceMode guidance)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            ulong token = profile.firstLaunchMissionTransitionToken;
            if (token == 0) token = narrativeToken != 0 ? narrativeToken : 1;
            string session = string.IsNullOrWhiteSpace(profile.firstLaunchMissionSessionToken)
                ? $"first-launch-m01-{token:x16}" : profile.firstLaunchMissionSessionToken;
            profile.firstLaunchMissionTransitionToken = token;
            profile.firstLaunchMissionSessionToken = session;
            return MissionLaunchPayloadFactory.Create(MissionId, ScenarioId, OperationMapId,
                MissionLaunchOriginKind.FirstLaunch, MissionRunKind.FirstClear, guidance, true,
                token, session, profile.firstLaunchMissionAttemptOrdinal, DeterministicSeed);
        }

        public static CampaignMissionLaunchRequestElement ToRequest(in MissionLaunchPayload payload) => new()
        {
            SchemaVersion = payload.SchemaVersion, MissionId = new FixedString64Bytes(payload.MissionId),
            ScenarioId = new FixedString64Bytes(payload.ScenarioId), OperationMapId = new FixedString64Bytes(payload.OperationMapId),
            LaunchOrigin = payload.LaunchOrigin, RunKind = payload.RunKind, Guidance = payload.Guidance,
            ReplayTutorialEnabled = payload.ReplayTutorialEnabled ? (byte)1 : (byte)0,
            TransitionToken = payload.TransitionToken, SessionToken = new FixedString64Bytes(payload.SessionToken),
            AttemptOrdinal = payload.AttemptOrdinal, DeterministicSeed = payload.DeterministicSeed
        };

        public static FirstLaunchMissionHandoffState Advance(EntityManager manager, in MissionLaunchPayload payload,
            ref bool published, ref byte rejectionCount)
        {
            using EntityQuery query = manager.CreateEntityQuery(ComponentType.ReadOnly<CampaignMissionRootComponent>());
            if (query.CalculateEntityCount() != 1) return FirstLaunchMissionHandoffState.Pending;
            Entity root = query.GetSingletonEntity();
            DynamicBuffer<CampaignMissionLaunchResultElement> results = manager.GetBuffer<CampaignMissionLaunchResultElement>(root);
            for (int i = 0; i < results.Length; i++)
            {
                CampaignMissionLaunchResultElement result = results[i];
                if (result.TransitionToken != payload.TransitionToken || result.AttemptOrdinal != payload.AttemptOrdinal ||
                    !result.SessionToken.Equals(new FixedString64Bytes(payload.SessionToken))) continue;
                results.RemoveAt(i);
                if (result.Accepted != 0) return FirstLaunchMissionHandoffState.Accepted;
                if (rejectionCount < 2) { rejectionCount++; published = false; }
                return FirstLaunchMissionHandoffState.Rejected;
            }
            if (!published)
            {
                manager.GetBuffer<CampaignMissionLaunchRequestElement>(root).Add(ToRequest(payload));
                published = true;
            }
            return FirstLaunchMissionHandoffState.Pending;
        }

        public static bool Matches(PlayerProfileSaveData profile, in MissionLaunchPayload payload) =>
            profile != null && profile.firstLaunchMissionTransitionToken == payload.TransitionToken &&
            profile.firstLaunchMissionAttemptOrdinal == payload.AttemptOrdinal &&
            string.Equals(profile.firstLaunchMissionSessionToken, payload.SessionToken, StringComparison.Ordinal);
    }
}
