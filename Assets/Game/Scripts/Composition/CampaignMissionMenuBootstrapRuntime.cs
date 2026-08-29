using System;
using Game.Components;
using Game.Configs;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    internal sealed class CampaignMissionMenuBootstrapRuntime
    {
        private const uint CampaignMissionSourceVersion = 1;
        private readonly CampaignMissionDebriefCompositionSystemHelper debrief = new();
        private readonly IGameTextResolver textResolver = new GameTextResolverAdapter();
        private OperationMapRuntimeBootstrapSceneSystemHelper campaignOperationMapBootstrap;
        private World campaignMissionWorld;
        private Entity campaignMissionRoot;
        private Entity campaignOperationMapRoot;
        private EntityQuery shellBoundaryQuery;
        private bool hasShellBoundaryQuery;
        private bool debriefInitialized;
        private bool campaignMissionCatalogProjected;
        private int campaignOperationMapGeneration;

        public void Update(MenuBootstrapView view, float unscaledDeltaTime = 0f)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            Prepare(view, world.EntityManager);
            TickDebrief(view, world.EntityManager, unscaledDeltaTime);
        }

        private void TickDebrief(
            MenuBootstrapView view,
            EntityManager entityManager,
            float unscaledDeltaTime)
        {
            if (!debriefInitialized)
            {
                debrief.Initialize(view, textResolver);
                debriefInitialized = true;
            }
            if (!hasShellBoundaryQuery)
            {
                shellBoundaryQuery = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<UiShellStateComponent>());
                hasShellBoundaryQuery = true;
            }
            if (shellBoundaryQuery.CalculateEntityCount() != 1)
                return;
            Entity boundary = shellBoundaryQuery.GetSingletonEntity();
            UiShellStateComponent shellState =
                entityManager.GetComponentData<UiShellStateComponent>(boundary);
            debrief.Tick(unscaledDeltaTime, entityManager, in shellState);
        }

        private void Prepare(MenuBootstrapView view, EntityManager entityManager)
        {
            bool hasChapterCatalog = view.CampaignMissionCatalog != null;
            bool hasLegacyMission = view.CampaignMissionDefinition != null && view.CampaignScenarioSetup != null;
            if ((!hasChapterCatalog && !hasLegacyMission) || view.CampaignOperationMapCatalog == null)
                return;

            if (campaignMissionWorld != entityManager.World)
            {
                Shutdown();
                campaignMissionWorld = entityManager.World;
            }

            if (!campaignMissionCatalogProjected && !TryProjectCatalog(
                    view, entityManager, hasChapterCatalog, out campaignMissionRoot, out string catalogError))
            {
                UnityEngine.Debug.LogError($"[CampaignMissionBootstrap] catalog={catalogError}");
                return;
            }
            campaignMissionCatalogProjected = true;
            if (campaignMissionRoot == Entity.Null || !entityManager.Exists(campaignMissionRoot))
            {
                campaignMissionCatalogProjected = false;
                return;
            }

            DynamicBuffer<CampaignMissionLaunchRequestElement> requests =
                entityManager.GetBuffer<CampaignMissionLaunchRequestElement>(campaignMissionRoot);
            if (requests.Length == 0)
                return;

            CampaignMissionLaunchRequestElement request = requests[0];
            if (!MatchesConfiguredMission(view, in request))
                return;
            if (!view.CampaignOperationMapCatalog.TryResolve(
                    request.OperationMapId.ToString(), out OperationMapDefinition definition))
            {
                UnityEngine.Debug.LogError(
                    $"[CampaignMissionBootstrap] operation-map definition is missing for " +
                    $"'{request.OperationMapId}'.");
                return;
            }

            CampaignMissionOperationMapReferenceComponent operationMapReference =
                entityManager.GetComponentObject<CampaignMissionOperationMapReferenceComponent>(
                    campaignMissionRoot);
            operationMapReference.MissionId = request.MissionId;
            operationMapReference.ScenarioId = request.ScenarioId;
            operationMapReference.OperationMapId = request.OperationMapId;
            operationMapReference.SessionToken = request.SessionToken;
            operationMapReference.Definition = definition;
            operationMapReference.TransitionToken = request.TransitionToken;
            operationMapReference.SourceVersion = entityManager
                .GetComponentData<CampaignMissionCatalogComponent>(campaignMissionRoot).SourceVersion;
            operationMapReference.AttemptOrdinal = request.AttemptOrdinal;

            if (HasMatchingOperationMap(entityManager, in request))
                return;

            campaignOperationMapBootstrap ??= new OperationMapRuntimeBootstrapSceneSystemHelper(entityManager.World);
            MatchSceneView.ResolveInitialOperationMapReadiness(
                true, out _, out OperationMapReadinessFlags requiredFlags);
            int generation = NextOperationMapGeneration(entityManager);
            if (generation <= 0)
            {
                UnityEngine.Debug.LogError("[CampaignMissionBootstrap] operation-map generation overflow.");
                return;
            }
            if (campaignOperationMapBootstrap.TryPublish(
                    definition,
                    request.ScenarioId,
                    request.MissionId,
                    generation,
                    OperationMapReadinessFlags.Metadata,
                    requiredFlags,
                    out campaignOperationMapRoot,
                    out string mapError))
            {
                campaignOperationMapGeneration = generation;
                return;
            }

            UnityEngine.Debug.LogError($"[CampaignMissionBootstrap] operationMap={mapError}");
            campaignOperationMapBootstrap.Dispose();
            campaignOperationMapBootstrap = null;
        }

        private bool HasMatchingOperationMap(
            EntityManager entityManager,
            in CampaignMissionLaunchRequestElement request)
        {
            if (campaignOperationMapRoot == Entity.Null || !entityManager.Exists(campaignOperationMapRoot) ||
                !entityManager.HasComponent<ActiveOperationMapComponent>(campaignOperationMapRoot))
                return false;
            ActiveOperationMapComponent active =
                entityManager.GetComponentData<ActiveOperationMapComponent>(campaignOperationMapRoot);
            return active.OperationMapId.Equals(request.OperationMapId) &&
                   active.ScenarioId.Equals(request.ScenarioId) && active.MissionId.Equals(request.MissionId);
        }

        private static bool MatchesConfiguredMission(
            MenuBootstrapView view,
            in CampaignMissionLaunchRequestElement request)
        {
            if (view.CampaignMissionCatalog != null)
            {
                if (!view.CampaignMissionCatalog.TryResolve(
                        request.MissionId.ToString(), out MissionDefinitionConfig mission,
                        out ScenarioSetupConfig scenario))
                    return false;
                return request.ScenarioId.Equals(new FixedString64Bytes(scenario.ScenarioId)) &&
                       request.OperationMapId.Equals(new FixedString64Bytes(mission.OperationMapId));
            }

            return view.CampaignMissionDefinition != null && view.CampaignScenarioSetup != null &&
                   request.MissionId.Equals(new FixedString64Bytes(view.CampaignMissionDefinition.MissionId)) &&
                   request.ScenarioId.Equals(new FixedString64Bytes(view.CampaignScenarioSetup.ScenarioId)) &&
                   request.OperationMapId.Equals(
                       new FixedString64Bytes(view.CampaignMissionDefinition.OperationMapId));
        }

        private static bool TryProjectCatalog(
            MenuBootstrapView view,
            EntityManager entityManager,
            bool hasChapterCatalog,
            out Entity root,
            out string error) => hasChapterCatalog
            ? CampaignMissionCatalogProjection.TryProject(
                entityManager, view.CampaignMissionCatalog, view.CampaignOperationMapCatalog,
                CampaignMissionSourceVersion, out root, out error)
            : CampaignMissionCatalogProjection.TryProject(
                entityManager, view.CampaignMissionDefinition, view.CampaignScenarioSetup,
                view.CampaignOperationMapCatalog, CampaignMissionSourceVersion, out root, out error);

        private int NextOperationMapGeneration(EntityManager entityManager)
        {
            int latest = campaignOperationMapGeneration;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRootComponent>(),
                ComponentType.ReadOnly<ActiveOperationMapComponent>());
            int rootCount = query.CalculateEntityCount();
            if (rootCount > 1)
                return 0;
            if (rootCount == 1)
            {
                Entity root = query.GetSingletonEntity();
                latest = System.Math.Max(latest,
                    entityManager.GetComponentData<ActiveOperationMapComponent>(root).Generation);
            }
            if (campaignOperationMapRoot != Entity.Null && entityManager.Exists(campaignOperationMapRoot) &&
                entityManager.HasComponent<ActiveOperationMapComponent>(campaignOperationMapRoot))
            {
                latest = System.Math.Max(latest,
                    entityManager.GetComponentData<ActiveOperationMapComponent>(campaignOperationMapRoot).Generation);
            }

            return latest == int.MaxValue ? 0 : latest + 1;
        }

        public void Shutdown()
        {
            debrief.Shutdown();
            debriefInitialized = false;
            if (hasShellBoundaryQuery && campaignMissionWorld != null && campaignMissionWorld.IsCreated)
                shellBoundaryQuery.Dispose();
            hasShellBoundaryQuery = false;
            campaignOperationMapBootstrap?.Dispose();
            campaignOperationMapBootstrap = null;
            campaignMissionWorld = null;
            campaignMissionRoot = Entity.Null;
            campaignOperationMapRoot = Entity.Null;
            campaignMissionCatalogProjected = false;
            campaignOperationMapGeneration = 0;
        }
    }

    internal static class CampaignMissionNarrativeCompositionUtility
    {
        internal static bool IsPresentationReady(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage stage,
            in UiShellStateComponent shellState) =>
            stage != CampaignMissionDebriefCompositionSystemHelper.SequenceStage.None &&
            shellState.CurrentMode == UiShellMode.MatchHud &&
            shellState.ActiveRoute == UIRoute.Match &&
            // The opening comic claims the opaque match-intro curtain before any HUD frame is exposed.
            (stage == CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Brief ||
             shellState.IsTransitionRunning == 0);

        internal static FirstLaunchNarrativeLanguage ReadLanguage()
        {
            PlayerProfileSaveData profile = SaveService.CreateDefault().LoadProfile();
            return Enum.TryParse(
                    profile?.firstLaunchLanguage,
                    true,
                    out FirstLaunchNarrativeLanguage language) &&
                language == FirstLaunchNarrativeLanguage.Persian
                    ? FirstLaunchNarrativeLanguage.Persian
                    : FirstLaunchNarrativeLanguage.English;
        }

        internal static void LogStage(
            string status,
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage stage,
            in FixedString64Bytes sequenceId,
            in FixedString64Bytes session,
            int attemptOrdinal) => UnityEngine.Debug.Log(
            $"[CampaignMissionNarrative] status={status} stage={stage} sequence={sequenceId} " +
            $"session={session} attempt={attemptOrdinal}");
    }
}
