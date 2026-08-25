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
                !SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionRuntimeComponent runtime) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionAttemptFactsComponent facts) ||
                !TryResolveDefinition(in catalog, in runtime, out int definitionIndex))
            {
                return;
            }

            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            if (!IsPublishable(in runtime, in facts, ref definition))
                return;

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
            if (IsStale(in current, in runtime, in facts) ||
                ProjectionMatches(
                    in current, objectives, catalog.SourceVersion, in runtime, in facts, ref definition) ||
                current.Version == uint.MaxValue)
            {
                return;
            }

            entityManager.SetComponentData(
                boundary,
                BuildState(in current, catalog.SourceVersion, in runtime, in facts));
            objectives.Clear();
            for (int index = 0; index < definition.Objectives.Length; index++)
                objectives.Add(BuildObjective(ref definition, index, in runtime, in facts));
        }

        internal static bool TryResolveDefinition(
            in CampaignMissionCatalogComponent catalog,
            in CampaignMissionRuntimeComponent runtime,
            out int definitionIndex)
        {
            definitionIndex = -1;
            return catalog.Blob.IsCreated && catalog.SourceVersion > 0 &&
                   runtime.Version > 0 && runtime.SourceVersion == catalog.SourceVersion &&
                   !runtime.MissionId.IsEmpty && !runtime.ScenarioId.IsEmpty &&
                   !runtime.OperationMapId.IsEmpty && !runtime.SessionToken.IsEmpty &&
                   runtime.AttemptOrdinal >= 0 &&
                   CampaignMissionSpawnSystem.TryFindDefinition(
                       in catalog, in runtime, out definitionIndex);
        }

        internal static bool IsPublishable(
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts,
            ref CampaignMissionDefinitionBlob definition)
        {
            if (!runtime.MissionId.Equals(definition.MissionId) ||
                !runtime.ScenarioId.Equals(definition.ScenarioId) ||
                !runtime.OperationMapId.Equals(definition.OperationMapId) ||
                facts.ElapsedMilliseconds < 0 || facts.HostileTotalCount < 0 ||
                facts.HostileDefeatedCount < 0 || facts.HostileDefeatedCount > facts.HostileTotalCount ||
                facts.RequiredBuildingCompletedCount < 0 || facts.RequiredUnitProducedCount < 0 ||
                facts.CommandSquadAlive > 1 || facts.ForwardPostBound > 1 ||
                facts.ForwardPostDamaged > 1 || facts.ForwardPostDestroyed > 1 ||
                definition.Objectives.Length == 0)
            {
                return false;
            }

            int buildRules = 0;
            int produceRules = 0;
            int defendRules = 0;
            for (int index = 0; index < definition.Objectives.Length; index++)
            {
                ref CampaignMissionObjectiveBlob objective = ref definition.Objectives[index];
                if (objective.ObjectiveId.IsEmpty || objective.DisplayTextKey.IsEmpty ||
                    objective.RequiredCount <= 0 || HasDuplicateObjectiveId(ref definition, index))
                    return false;

                switch (objective.Rule)
                {
                    case MissionObjectiveRuleKind.DestroyMissionRole:
                        if (objective.MissionRoleId.IsEmpty || !objective.TargetConfigId.IsEmpty ||
                            facts.HostileTotalCount != objective.RequiredCount)
                            return false;
                        break;
                    case MissionObjectiveRuleKind.ProtectMissionRole:
                        if (objective.MissionRoleId.IsEmpty || !objective.TargetConfigId.IsEmpty ||
                            facts.CommandSquadSpawned == 0)
                            return false;
                        break;
                    case MissionObjectiveRuleKind.BuildStructure:
                        buildRules++;
                        if (objective.TargetConfigId.IsEmpty || !objective.MissionRoleId.IsEmpty ||
                            definition.BuildZone.AnchorId.IsEmpty)
                            return false;
                        break;
                    case MissionObjectiveRuleKind.ProduceUnit:
                        produceRules++;
                        if (objective.TargetConfigId.IsEmpty || !objective.MissionRoleId.IsEmpty ||
                            definition.BaseAnchorId.IsEmpty)
                            return false;
                        break;
                    case MissionObjectiveRuleKind.DefendMissionRole:
                        defendRules++;
                        if (objective.MissionRoleId.IsEmpty || !objective.TargetConfigId.IsEmpty ||
                            definition.BaseAnchorId.IsEmpty ||
                            !objective.MissionRoleId.Equals(definition.BaseMissionRoleId))
                            return false;
                        break;
                    default:
                        return false;
                }
            }

            return buildRules <= 1 && produceRules <= 1 && defendRules <= 1;
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
                   facts.RequiredBuildingCompletedCount < current.RequiredBuildingCompletedCount ||
                   facts.RequiredUnitProducedCount < current.RequiredUnitProducedCount ||
                   (current.CommandSquadAlive == 0 && facts.CommandSquadAlive != 0) ||
                   (current.ForwardPostBound != 0 && facts.ForwardPostBound == 0) ||
                   (current.ForwardPostDamaged != 0 && facts.ForwardPostDamaged == 0) ||
                   (current.ForwardPostDestroyed != 0 && facts.ForwardPostDestroyed == 0) ||
                   (current.MatchActive == 0 && runtime.Outcome == MissionOutcomeKind.None);
        }

        private static MatchObjectiveRuntimeStateComponent BuildState(
            in MatchObjectiveRuntimeStateComponent current,
            uint catalogSourceVersion,
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts) =>
            new()
            {
                Version = current.Version + 1u,
                MissionCatalogSourceVersion = catalogSourceVersion,
                MissionSourceVersion = runtime.Version,
                MissionId = runtime.MissionId,
                SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal,
                MatchStartedAt = 0f,
                ElapsedWholeSeconds = math.max(0, facts.ElapsedMilliseconds / 1000),
                HostileTotalCount = facts.HostileTotalCount,
                HostileDefeatedCount = facts.HostileDefeatedCount,
                RequiredBuildingCompletedCount = facts.RequiredBuildingCompletedCount,
                RequiredUnitProducedCount = facts.RequiredUnitProducedCount,
                CommandSquadAlive = facts.CommandSquadAlive,
                ForwardPostBound = facts.ForwardPostBound,
                ForwardPostDamaged = facts.ForwardPostDamaged,
                ForwardPostDestroyed = facts.ForwardPostDestroyed,
                MatchActive = runtime.Outcome == MissionOutcomeKind.None ? (byte)1 : (byte)0
            };

        private static MatchObjectiveRuntimeElement BuildObjective(
            ref CampaignMissionDefinitionBlob definition,
            int index,
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            ref CampaignMissionObjectiveBlob objective = ref definition.Objectives[index];
            MatchObjectiveState objectiveState = ResolveState(in objective, in runtime, in facts);
            return new MatchObjectiveRuntimeElement
            {
                GoalId = index + 1,
                ObjectiveId = objective.ObjectiveId,
                OperationMapAnchorId = ResolveAnchor(ref definition, in objective),
                State = objectiveState,
                Priority = (byte)math.min(byte.MaxValue, index + 2),
                IsPrimary = index == 0 ? (byte)1 : (byte)0,
                Title = ResolveTitle(objective.Rule),
                Body = ResolveBody(in objective, objectiveState, in facts),
                ProtectsTarget = objective.Rule is MissionObjectiveRuleKind.ProtectMissionRole or
                    MissionObjectiveRuleKind.DefendMissionRole ? (byte)1 : (byte)0
            };
        }

        private static MatchObjectiveState ResolveState(
            in CampaignMissionObjectiveBlob objective,
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            if (runtime.Outcome == MissionOutcomeKind.Defeat)
                return MatchObjectiveState.Failed;
            if (runtime.Outcome == MissionOutcomeKind.Victory)
                return MatchObjectiveState.Complete;

            return objective.Rule switch
            {
                MissionObjectiveRuleKind.DestroyMissionRole =>
                    runtime.Phase >= MissionPhaseKind.SecureCorridor ||
                    facts.HostileDefeatedCount >= objective.RequiredCount
                        ? MatchObjectiveState.Complete : MatchObjectiveState.Active,
                MissionObjectiveRuleKind.ProtectMissionRole =>
                    facts.CommandSquadAlive == 0 ? MatchObjectiveState.Failed : MatchObjectiveState.Active,
                MissionObjectiveRuleKind.BuildStructure =>
                    facts.RequiredBuildingCompletedCount >= objective.RequiredCount
                        ? MatchObjectiveState.Complete : MatchObjectiveState.Active,
                MissionObjectiveRuleKind.ProduceUnit =>
                    facts.RequiredUnitProducedCount >= objective.RequiredCount
                        ? MatchObjectiveState.Complete : MatchObjectiveState.Active,
                MissionObjectiveRuleKind.DefendMissionRole => facts.ForwardPostBound == 0
                    ? MatchObjectiveState.Blocked
                    : facts.ForwardPostDestroyed != 0
                    ? MatchObjectiveState.Failed
                    : facts.ForwardPostDamaged != 0 ? MatchObjectiveState.Warning : MatchObjectiveState.Active,
                _ => MatchObjectiveState.Blocked
            };
        }

        private static FixedString64Bytes ResolveAnchor(
            ref CampaignMissionDefinitionBlob definition,
            in CampaignMissionObjectiveBlob objective)
        {
            return objective.Rule switch
            {
                MissionObjectiveRuleKind.DestroyMissionRole =>
                    new FixedString64Bytes("anchor.ch01.m01.patrol_objective"),
                MissionObjectiveRuleKind.ProtectMissionRole =>
                    new FixedString64Bytes("anchor.ch01.m01.player_spawn"),
                MissionObjectiveRuleKind.BuildStructure => definition.BuildZone.AnchorId,
                MissionObjectiveRuleKind.ProduceUnit or MissionObjectiveRuleKind.DefendMissionRole =>
                    definition.BaseAnchorId,
                _ => default
            };
        }

        private static FixedString64Bytes ResolveTitle(MissionObjectiveRuleKind rule) => rule switch
        {
            MissionObjectiveRuleKind.DestroyMissionRole => new FixedString64Bytes("Destroy the hostile patrol"),
            MissionObjectiveRuleKind.ProtectMissionRole => new FixedString64Bytes("Keep the command squad alive"),
            MissionObjectiveRuleKind.BuildStructure => new FixedString64Bytes("Build the forward Barracks"),
            MissionObjectiveRuleKind.ProduceUnit => new FixedString64Bytes("Produce a rifle squad"),
            MissionObjectiveRuleKind.DefendMissionRole => new FixedString64Bytes("Defend the forward post"),
            _ => default
        };

        private static FixedString128Bytes ResolveBody(
            in CampaignMissionObjectiveBlob objective,
            MatchObjectiveState objectiveState,
            in CampaignMissionAttemptFactsComponent facts)
        {
            switch (objective.Rule)
            {
                case MissionObjectiveRuleKind.DestroyMissionRole:
                    return BuildProgressBody(
                        new FixedString128Bytes("Patrol neutralized "),
                        facts.HostileDefeatedCount,
                        objective.RequiredCount);
                case MissionObjectiveRuleKind.ProtectMissionRole:
                    return objectiveState == MatchObjectiveState.Failed
                        ? new FixedString128Bytes("Command squad lost")
                        : new FixedString128Bytes("Command squad operational");
                case MissionObjectiveRuleKind.BuildStructure:
                    return BuildProgressBody(
                        new FixedString128Bytes("Barracks completed "),
                        facts.RequiredBuildingCompletedCount,
                        objective.RequiredCount);
                case MissionObjectiveRuleKind.ProduceUnit:
                    return BuildProgressBody(
                        new FixedString128Bytes("Rifle squads ready "),
                        facts.RequiredUnitProducedCount,
                        objective.RequiredCount);
                case MissionObjectiveRuleKind.DefendMissionRole:
                    if (facts.ForwardPostBound == 0)
                        return new FixedString128Bytes("Forward post unavailable");
                    if (facts.ForwardPostDestroyed != 0)
                        return new FixedString128Bytes("Forward post destroyed");
                    if (facts.ForwardPostDamaged != 0)
                        return new FixedString128Bytes("Forward post under attack");
                    if (facts.DefenseWaveActivated != 0)
                        return new FixedString128Bytes("Defend against the hostile patrol");
                    return new FixedString128Bytes("Forward post operational");
                default:
                    return default;
            }
        }

        private static FixedString128Bytes BuildProgressBody(
            FixedString128Bytes prefix,
            int current,
            int required)
        {
            FixedString128Bytes body = new(prefix);
            body.Append(math.min(current, required));
            body.Append('/');
            body.Append(required);
            return body;
        }

        private static bool ProjectionMatches(
            in MatchObjectiveRuntimeStateComponent current,
            DynamicBuffer<MatchObjectiveRuntimeElement> objectives,
            uint catalogSourceVersion,
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts,
            ref CampaignMissionDefinitionBlob definition)
        {
            if (current.MissionCatalogSourceVersion != catalogSourceVersion ||
                current.MissionSourceVersion != runtime.Version ||
                !current.MissionId.Equals(runtime.MissionId) ||
                !current.SessionToken.Equals(runtime.SessionToken) ||
                current.AttemptOrdinal != runtime.AttemptOrdinal ||
                current.ElapsedWholeSeconds != math.max(0, facts.ElapsedMilliseconds / 1000) ||
                current.HostileTotalCount != facts.HostileTotalCount ||
                current.HostileDefeatedCount != facts.HostileDefeatedCount ||
                current.RequiredBuildingCompletedCount != facts.RequiredBuildingCompletedCount ||
                current.RequiredUnitProducedCount != facts.RequiredUnitProducedCount ||
                current.CommandSquadAlive != facts.CommandSquadAlive ||
                current.ForwardPostBound != facts.ForwardPostBound ||
                current.ForwardPostDamaged != facts.ForwardPostDamaged ||
                current.ForwardPostDestroyed != facts.ForwardPostDestroyed ||
                current.MatchActive != (runtime.Outcome == MissionOutcomeKind.None ? (byte)1 : (byte)0) ||
                objectives.Length != definition.Objectives.Length)
            {
                return false;
            }

            for (int index = 0; index < objectives.Length; index++)
            {
                MatchObjectiveRuntimeElement expected =
                    BuildObjective(ref definition, index, in runtime, in facts);
                if (!ObjectiveEquals(objectives[index], expected))
                    return false;
            }

            return true;
        }

        private static bool HasDuplicateObjectiveId(ref CampaignMissionDefinitionBlob definition, int index)
        {
            for (int prior = 0; prior < index; prior++)
            {
                if (definition.Objectives[prior].ObjectiveId.Equals(definition.Objectives[index].ObjectiveId))
                    return true;
            }

            return false;
        }

        private static bool ObjectiveEquals(
            MatchObjectiveRuntimeElement left,
            MatchObjectiveRuntimeElement right)
        {
            return left.GoalId == right.GoalId && left.ObjectiveId.Equals(right.ObjectiveId) &&
                   left.OperationMapAnchorId.Equals(right.OperationMapAnchorId) && left.State == right.State &&
                   left.Priority == right.Priority && left.IsPrimary == right.IsPrimary &&
                   left.Title.Equals(right.Title) && left.Body.Equals(right.Body) &&
                   left.TargetEntity == right.TargetEntity && left.TargetCell.Equals(right.TargetCell) &&
                   left.WorldPosition.Equals(right.WorldPosition) &&
                   left.HasTargetCell == right.HasTargetCell &&
                   left.HasWorldPosition == right.HasWorldPosition &&
                   left.ProtectsTarget == right.ProtectsTarget;
        }
    }
}
