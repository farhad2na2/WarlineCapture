using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    internal sealed class CampaignMissionMenuBootstrapRuntime
    {
        private const uint CampaignMissionSourceVersion = 1;
        private OperationMapRuntimeBootstrapSceneSystemHelper campaignOperationMapBootstrap;
        private World campaignMissionWorld;
        private Entity campaignMissionRoot;
        private Entity campaignOperationMapRoot;
        private bool campaignMissionCatalogProjected;

        public void Update(MenuBootstrapView view)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            Prepare(view, world.EntityManager);
        }

        private void Prepare(MenuBootstrapView view, EntityManager entityManager)
        {
            if (view.CampaignMissionDefinition == null || view.CampaignScenarioSetup == null ||
                view.CampaignOperationMapCatalog == null)
                return;

            if (campaignMissionWorld != entityManager.World)
            {
                Shutdown();
                campaignMissionWorld = entityManager.World;
            }

            if (!campaignMissionCatalogProjected && !CampaignMissionCatalogProjection.TryProject(
                    entityManager, view.CampaignMissionDefinition, view.CampaignScenarioSetup,
                    view.CampaignOperationMapCatalog, CampaignMissionSourceVersion,
                    out campaignMissionRoot, out string catalogError))
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
            if (!MatchesConfiguredMission(view, in request) ||
                !view.CampaignOperationMapCatalog.TryResolve(
                    request.OperationMapId.ToString(), out OperationMapDefinition definition))
                return;

            if (HasMatchingOperationMap(entityManager, in request))
                return;

            campaignOperationMapBootstrap ??= new OperationMapRuntimeBootstrapSceneSystemHelper(entityManager.World);
            MatchSceneView.ResolveInitialOperationMapReadiness(
                true, out _, out OperationMapReadinessFlags requiredFlags);
            if (campaignOperationMapBootstrap.TryPublish(
                    definition,
                    request.ScenarioId,
                    request.MissionId,
                    1,
                    OperationMapReadinessFlags.Metadata,
                    requiredFlags,
                    out campaignOperationMapRoot,
                    out string mapError))
                return;

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
            in CampaignMissionLaunchRequestElement request) =>
            request.MissionId.Equals(new FixedString64Bytes(view.CampaignMissionDefinition.MissionId)) &&
            request.ScenarioId.Equals(new FixedString64Bytes(view.CampaignScenarioSetup.ScenarioId)) &&
            request.OperationMapId.Equals(new FixedString64Bytes(view.CampaignMissionDefinition.OperationMapId));

        public void Shutdown()
        {
            campaignOperationMapBootstrap?.Dispose();
            campaignOperationMapBootstrap = null;
            campaignMissionWorld = null;
            campaignMissionRoot = Entity.Null;
            campaignOperationMapRoot = Entity.Null;
            campaignMissionCatalogProjected = false;
        }
    }
}
