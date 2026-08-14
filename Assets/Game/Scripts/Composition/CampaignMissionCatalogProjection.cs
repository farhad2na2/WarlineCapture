using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    internal static class CampaignMissionCatalogProjection
    {
        public static bool TryProject(
            EntityManager entityManager,
            MissionDefinitionConfig mission,
            ScenarioSetupConfig scenario,
            OperationMapCatalogConfig operationMaps,
            uint sourceVersion,
            out Entity root,
            out string error)
        {
            root = Entity.Null;
            error = null;
            if (sourceVersion == 0 || !MissionDefinitionContractValidation.TryValidateDefinition(mission, out error) ||
                scenario == null || !scenario.TryValidate(out error) || operationMaps == null ||
                !operationMaps.TryValidate(out error) ||
                !string.Equals(mission.ScenarioId, scenario.ScenarioId, StringComparison.Ordinal) ||
                !string.Equals(mission.OperationMapId, scenario.OperationMapId, StringComparison.Ordinal) ||
                !operationMaps.TryResolve(mission.OperationMapId, out _))
            {
                error ??= "Mission, scenario, and operation-map identities must resolve as one validated launch unit.";
                return false;
            }

            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRootComponent>());
            using NativeArray<Entity> roots = query.ToEntityArray(Allocator.Temp);
            if (roots.Length > 1)
            {
                error = "Exactly zero or one campaign-mission root is permitted.";
                return false;
            }

            root = roots.Length == 1 ? roots[0] : CreateRoot(entityManager);
            EnsureProgressStore(entityManager, root);
            CampaignMissionCatalogComponent previous = entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            if (previous.SourceVersion == sourceVersion && previous.Blob.IsCreated &&
                previous.Blob.Value.Missions.Length == 1 &&
                previous.Blob.Value.Missions[0].MissionId.Equals(new FixedString64Bytes(mission.MissionId)))
            {
                error = null;
                return true;
            }

            BlobBuilder builder = new(Allocator.Temp);
            ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
            catalog.SchemaVersion = mission.SchemaVersion;
            BlobBuilderArray<CampaignMissionDefinitionBlob> definitions = builder.Allocate(ref catalog.Missions, 1);
            ref CampaignMissionDefinitionBlob definition = ref definitions[0];
            definition.MissionId = new FixedString64Bytes(mission.MissionId);
            definition.ScenarioId = new FixedString64Bytes(scenario.ScenarioId);
            definition.OperationMapId = new FixedString64Bytes(mission.OperationMapId);
            definition.DisplayNameKey = new FixedString64Bytes(mission.DisplayNameKey);
            definition.DisplaySummaryKey = new FixedString64Bytes(mission.DisplaySummaryKey);
            definition.LocationNameKey = new FixedString64Bytes(mission.LocationNameKey);
            definition.BriefingSequenceId = new FixedString64Bytes(mission.BriefingSequenceId);
            definition.SchemaVersion = mission.SchemaVersion;
            definition.DeterministicSeed = scenario.DeterministicSeed;
            definition.EncounterStartMilliseconds = scenario.EncounterStartMilliseconds;
            definition.BuildingDisabled = scenario.Restrictions.BuildingDisabled ? (byte)1 : (byte)0;
            definition.ProductionDisabled = scenario.Restrictions.ProductionDisabled ? (byte)1 : (byte)0;
            definition.EconomyDisabled = scenario.Restrictions.EconomyDisabled ? (byte)1 : (byte)0;
            definition.TransportDisabled = scenario.Restrictions.TransportDisabled ? (byte)1 : (byte)0;
            definition.AirDisabled = scenario.Restrictions.AirDisabled ? (byte)1 : (byte)0;
            definition.ReplayAllowed = mission.ReplayAllowed ? (byte)1 : (byte)0;
            definition.ReplayTutorialDefaultEnabled = mission.ReplayTutorialDefaultEnabled ? (byte)1 : (byte)0;
            ProjectObjectives(ref builder, ref definition, mission);
            ProjectForces(ref builder, ref definition, scenario);
            ProjectAmbient(ref builder, ref definition, scenario);
            ProjectStars(ref builder, ref definition, mission);
            ProjectRewards(ref builder, ref definition, mission);
            BlobAssetReference<CampaignMissionCatalogBlob> projected =
                builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
            builder.Dispose();

            CampaignMissionCatalogDisposalSystem.DisposeOwned(ref previous);
            entityManager.SetComponentData(root, new CampaignMissionCatalogComponent
            {
                Blob = projected,
                SourceVersion = sourceVersion,
                OwnsBlob = 1
            });
            error = null;
            return true;
        }

        private static void ProjectObjectives(
            ref BlobBuilder builder, ref CampaignMissionDefinitionBlob definition, MissionDefinitionConfig mission)
        {
            ReadOnlySpan<MissionObjectiveDefinitionConfig> source = mission.Objectives;
            BlobBuilderArray<CampaignMissionObjectiveBlob> projected =
                builder.Allocate(ref definition.Objectives, source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                projected[i] = new CampaignMissionObjectiveBlob
                {
                    ObjectiveId = new FixedString64Bytes(source[i].ObjectiveId),
                    DisplayTextKey = new FixedString64Bytes(source[i].DisplayTextKey),
                    MissionRoleId = new FixedString64Bytes(source[i].MissionRoleId),
                    Rule = source[i].Rule,
                    RequiredCount = source[i].RequiredCount,
                    FailureOnRuleBreak = source[i].FailureOnRuleBreak ? (byte)1 : (byte)0
                };
            }
        }

        private static void ProjectForces(
            ref BlobBuilder builder, ref CampaignMissionDefinitionBlob definition, ScenarioSetupConfig scenario)
        {
            ReadOnlySpan<ScenarioUnitGroupConfig> sourceGroups = scenario.UnitGroups;
            BlobBuilderArray<CampaignMissionForceGroupBlob> groups =
                builder.Allocate(ref definition.ForceGroups, sourceGroups.Length);
            for (int groupIndex = 0; groupIndex < sourceGroups.Length; groupIndex++)
            {
                ScenarioUnitGroupConfig sourceGroup = sourceGroups[groupIndex];
                ref CampaignMissionForceGroupBlob group = ref groups[groupIndex];
                group.GroupId = new FixedString64Bytes(sourceGroup.GroupId);
                group.FactionId = sourceGroup.FactionIndex;
                ReadOnlySpan<ScenarioUnitEntryConfig> sourceUnits = sourceGroup.Units;
                BlobBuilderArray<CampaignMissionForceUnitBlob> units =
                    builder.Allocate(ref group.Units, sourceUnits.Length);
                for (int unitIndex = 0; unitIndex < sourceUnits.Length; unitIndex++)
                {
                    ScenarioUnitEntryConfig source = sourceUnits[unitIndex];
                    units[unitIndex] = new CampaignMissionForceUnitBlob
                    {
                        SourceKey = new FixedString64Bytes(source.UnitConfigKey),
                        RuntimePrefabSourceKey = new FixedString64Bytes(source.RuntimePrefabSourceKey),
                        ExpectedAssetGuid = new FixedString64Bytes(source.ExpectedAssetGuid),
                        SpawnAnchorId = new FixedString64Bytes(source.SpawnAnchorId),
                        MissionRoleId = new FixedString64Bytes(source.MissionRoleId),
                        Count = source.Count
                    };
                }
            }

            ReadOnlySpan<ScenarioPatrolRouteConfig> sourceRoutes = scenario.PatrolRoutes;
            BlobBuilderArray<CampaignMissionPatrolRouteBlob> routes =
                builder.Allocate(ref definition.PatrolRoutes, sourceRoutes.Length);
            for (int routeIndex = 0; routeIndex < sourceRoutes.Length; routeIndex++)
            {
                ScenarioPatrolRouteConfig sourceRoute = sourceRoutes[routeIndex];
                ref CampaignMissionPatrolRouteBlob route = ref routes[routeIndex];
                route.RouteId = new FixedString64Bytes(sourceRoute.RouteId);
                route.UnitGroupId = new FixedString64Bytes(sourceRoute.UnitGroupId);
                route.StartDelayMilliseconds = sourceRoute.StartDelayMilliseconds;
                ReadOnlySpan<string> sourceAnchors = sourceRoute.AnchorIds;
                BlobBuilderArray<FixedString64Bytes> anchors =
                    builder.Allocate(ref route.AnchorIds, sourceAnchors.Length);
                for (int anchorIndex = 0; anchorIndex < sourceAnchors.Length; anchorIndex++)
                    anchors[anchorIndex] = new FixedString64Bytes(sourceAnchors[anchorIndex]);
            }
        }

        private static Entity CreateRoot(EntityManager entityManager)
        {
            Entity root = entityManager.CreateEntity(
                typeof(CampaignMissionRootComponent), typeof(CampaignMissionCatalogComponent),
                typeof(CampaignMissionLaunchQueueComponent), typeof(CampaignMissionRuntimeComponent),
                typeof(CampaignMissionAttemptFactsComponent), typeof(CampaignMissionGuidanceProjectionComponent));
            entityManager.AddBuffer<CampaignMissionLaunchRequestElement>(root);
            entityManager.AddBuffer<CampaignMissionLaunchResultElement>(root);
            entityManager.AddBuffer<CampaignMissionActionRequestElement>(root);
            entityManager.AddBuffer<CampaignMissionActionResultElement>(root);
            entityManager.AddBuffer<CampaignMissionSettlementRequestElement>(root);
            entityManager.AddBuffer<CampaignMissionSettlementResultElement>(root);
            entityManager.AddBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
            entityManager.SetName(root, "CampaignMissionRoot");
            return root;
        }

        private static void EnsureProgressStore(EntityManager entityManager, Entity root)
        {
            if (entityManager.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root)) return;
            entityManager.AddComponentObject(root, new CampaignMissionProgressStoreReferenceComponent
            {
                Store = new CampaignMissionProgressStore(SaveService.CreateDefault())
            });
        }

        private static void ProjectAmbient(
            ref BlobBuilder builder, ref CampaignMissionDefinitionBlob definition, ScenarioSetupConfig scenario)
        {
            ReadOnlySpan<ScenarioAmbientPresentationConfig> source = scenario.AmbientPresentations;
            BlobBuilderArray<CampaignMissionAmbientPresentationBlob> projected =
                builder.Allocate(ref definition.AmbientPresentations, source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                projected[i] = new CampaignMissionAmbientPresentationBlob
                {
                    PresentationId = new FixedString64Bytes(source[i].PresentationId),
                    AnchorId = new FixedString64Bytes(source[i].AnchorId),
                    RouteId = new FixedString64Bytes(source[i].RouteId),
                    InstanceCount = source[i].InstanceCount
                };
            }
        }

        private static void ProjectStars(
            ref BlobBuilder builder, ref CampaignMissionDefinitionBlob definition, MissionDefinitionConfig mission)
        {
            ReadOnlySpan<MissionStarDefinitionConfig> source = mission.Stars;
            BlobBuilderArray<CampaignMissionStarRuleBlob> projected =
                builder.Allocate(ref definition.StarRules, source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                projected[i] = new CampaignMissionStarRuleBlob
                {
                    StarIndex = source[i].StarIndex,
                    Rule = source[i].Rule,
                    DisplayTextKey = new FixedString64Bytes(source[i].DisplayTextKey),
                    Threshold = source[i].Threshold
                };
            }
        }

        private static void ProjectRewards(
            ref BlobBuilder builder, ref CampaignMissionDefinitionBlob definition, MissionDefinitionConfig mission)
        {
            ProjectRewardSet(ref builder, ref definition.FirstClearRewards, mission.FirstClearRewards);
            ProjectRewardSet(ref builder, ref definition.ReplayRewards, mission.ReplayRewards);
        }

        private static void ProjectRewardSet(
            ref BlobBuilder builder,
            ref BlobArray<CampaignMissionRewardBlob> destination,
            ReadOnlySpan<MissionRewardDefinitionConfig> source)
        {
            BlobBuilderArray<CampaignMissionRewardBlob> projected = builder.Allocate(ref destination, source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                projected[i] = new CampaignMissionRewardBlob
                {
                    Kind = source[i].Kind,
                    RewardConfigId = new FixedString64Bytes(source[i].RewardConfigId ?? string.Empty),
                    DisplayTextKey = new FixedString64Bytes(source[i].DisplayTextKey),
                    Amount = source[i].Amount
                };
            }
        }
    }
}
