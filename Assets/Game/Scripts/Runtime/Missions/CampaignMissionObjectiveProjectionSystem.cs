using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CampaignMissionRuntimeSystem))]
    public partial struct CampaignMissionObjectiveProjectionSystem : ISystem
    {
        private EntityQuery _boundaryQuery;

        public void OnCreate(ref SystemState state)
        {
            _boundaryQuery = state.GetEntityQuery(ComponentType.ReadOnly<MatchObjectiveProjectionBoundaryComponent>());
            state.RequireForUpdate<CampaignMissionRootComponent>();
            state.RequireForUpdate(_boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_boundaryQuery.CalculateEntityCount() != 1 ||
                !SystemAPI.TryGetSingleton(out CampaignMissionRuntimeComponent runtime) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionAttemptFactsComponent facts) ||
                !IsPublishable(in runtime, in facts))
            {
                return;
            }

            EntityManager entityManager = state.EntityManager;
            Entity boundary = _boundaryQuery.GetSingletonEntity();
            if (!entityManager.HasComponent<MatchObjectiveRuntimeStateComponent>(boundary))
                entityManager.AddComponentData(boundary, default(MatchObjectiveRuntimeStateComponent));
            if (!entityManager.HasBuffer<MatchObjectiveRuntimeElement>(boundary))
                entityManager.AddBuffer<MatchObjectiveRuntimeElement>(boundary);

            MatchObjectiveRuntimeStateComponent current =
                entityManager.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary);
            DynamicBuffer<MatchObjectiveRuntimeElement> objectives =
                entityManager.GetBuffer<MatchObjectiveRuntimeElement>(boundary);
            if (IsStale(in current, in runtime, in facts))
                return;

            MatchObjectiveRuntimeElement primary = BuildPrimary(in runtime, in facts);
            MatchObjectiveRuntimeElement protect = BuildProtect(in runtime, in facts);
            if (ProjectionMatches(in current, objectives, in runtime, in facts, in primary, in protect))
                return;
            if (current.Version == uint.MaxValue)
                return;

            entityManager.SetComponentData(boundary, new MatchObjectiveRuntimeStateComponent
            {
                Version = current.Version + 1u,
                MissionSourceVersion = runtime.Version,
                MissionId = runtime.MissionId,
                SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal,
                MatchStartedAt = 0f,
                ElapsedWholeSeconds = math.max(0, facts.ElapsedMilliseconds / 1000),
                HostileTotalCount = facts.HostileTotalCount,
                HostileDefeatedCount = facts.HostileDefeatedCount,
                CommandSquadAlive = facts.CommandSquadAlive,
                MatchActive = runtime.Outcome == MissionOutcomeKind.None ? (byte)1 : (byte)0
            });
            objectives.Clear();
            objectives.Add(primary);
            objectives.Add(protect);
        }

        internal static bool IsPublishable(
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            return runtime.Version > 0 && runtime.SourceVersion > 0 &&
                   runtime.MissionId.Equals(new FixedString64Bytes("saga.ch01.m01.first_contact")) &&
                   !runtime.SessionToken.IsEmpty && runtime.AttemptOrdinal >= 0 &&
                   facts.CommandSquadSpawned != 0 && facts.HostileTotalCount > 0 &&
                   facts.HostileDefeatedCount >= 0 &&
                   facts.HostileDefeatedCount <= facts.HostileTotalCount &&
                   facts.ElapsedMilliseconds >= 0;
        }

        internal static bool IsStale(
            in MatchObjectiveRuntimeStateComponent current,
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            if (current.MissionId.IsEmpty)
                return false;
            bool sameAttempt = current.MissionId.Equals(runtime.MissionId) &&
                               current.SessionToken.Equals(runtime.SessionToken) &&
                               current.AttemptOrdinal == runtime.AttemptOrdinal;
            if (!sameAttempt)
                return false;
            return runtime.Version < current.MissionSourceVersion ||
                   facts.ElapsedMilliseconds / 1000 < current.ElapsedWholeSeconds ||
                   facts.HostileTotalCount != current.HostileTotalCount ||
                   facts.HostileDefeatedCount < current.HostileDefeatedCount ||
                   (current.CommandSquadAlive == 0 && facts.CommandSquadAlive != 0);
        }

        private static MatchObjectiveRuntimeElement BuildPrimary(
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            bool complete = runtime.Outcome == MissionOutcomeKind.Victory ||
                            runtime.Phase >= MissionPhaseKind.SecureCorridor ||
                            facts.HostileDefeatedCount >= facts.HostileTotalCount;
            MatchObjectiveState objectiveState = runtime.Outcome == MissionOutcomeKind.Defeat
                ? MatchObjectiveState.Failed
                : complete ? MatchObjectiveState.Complete : MatchObjectiveState.Active;
            FixedString128Bytes body = new("Patrol neutralized ");
            body.Append(facts.HostileDefeatedCount);
            body.Append('/');
            body.Append(facts.HostileTotalCount);
            return new MatchObjectiveRuntimeElement
            {
                GoalId = 1,
                ObjectiveId = new FixedString64Bytes("obj.ch01.m01.destroy_patrol"),
                OperationMapAnchorId = new FixedString64Bytes("anchor.ch01.m01.patrol_objective"),
                State = objectiveState,
                Priority = 2,
                IsPrimary = 1,
                Title = new FixedString64Bytes("Destroy the hostile patrol"),
                Body = body
            };
        }

        private static MatchObjectiveRuntimeElement BuildProtect(
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            MatchObjectiveState objectiveState = facts.CommandSquadAlive == 0 ||
                                                 runtime.Outcome == MissionOutcomeKind.Defeat
                ? MatchObjectiveState.Failed
                : runtime.Outcome == MissionOutcomeKind.Victory
                    ? MatchObjectiveState.Complete
                    : MatchObjectiveState.Active;
            return new MatchObjectiveRuntimeElement
            {
                GoalId = 2,
                ObjectiveId = new FixedString64Bytes("obj.ch01.m01.keep_command_squad_alive"),
                OperationMapAnchorId = new FixedString64Bytes("anchor.ch01.m01.player_spawn"),
                State = objectiveState,
                Priority = 3,
                Title = new FixedString64Bytes("Keep the command squad alive"),
                Body = objectiveState == MatchObjectiveState.Failed
                    ? new FixedString128Bytes("Command squad lost")
                    : new FixedString128Bytes("Command squad operational"),
                ProtectsTarget = 1
            };
        }

        private static bool ProjectionMatches(
            in MatchObjectiveRuntimeStateComponent current,
            DynamicBuffer<MatchObjectiveRuntimeElement> objectives,
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts,
            in MatchObjectiveRuntimeElement primary,
            in MatchObjectiveRuntimeElement protect)
        {
            return current.MissionSourceVersion == runtime.Version &&
                   current.MissionId.Equals(runtime.MissionId) &&
                   current.SessionToken.Equals(runtime.SessionToken) &&
                   current.AttemptOrdinal == runtime.AttemptOrdinal &&
                   current.ElapsedWholeSeconds == math.max(0, facts.ElapsedMilliseconds / 1000) &&
                   current.HostileTotalCount == facts.HostileTotalCount &&
                   current.HostileDefeatedCount == facts.HostileDefeatedCount &&
                   current.CommandSquadAlive == facts.CommandSquadAlive &&
                   current.MatchActive == (runtime.Outcome == MissionOutcomeKind.None ? (byte)1 : (byte)0) &&
                   objectives.Length == 2 && ObjectiveEquals(objectives[0], primary) &&
                   ObjectiveEquals(objectives[1], protect);
        }

        private static bool ObjectiveEquals(
            MatchObjectiveRuntimeElement left,
            MatchObjectiveRuntimeElement right)
        {
            return left.GoalId == right.GoalId && left.ObjectiveId.Equals(right.ObjectiveId) &&
                   left.OperationMapAnchorId.Equals(right.OperationMapAnchorId) && left.State == right.State &&
                   left.Priority == right.Priority && left.IsPrimary == right.IsPrimary &&
                   left.Title.Equals(right.Title) && left.Body.Equals(right.Body) &&
                   left.ProtectsTarget == right.ProtectsTarget;
        }
    }
}
