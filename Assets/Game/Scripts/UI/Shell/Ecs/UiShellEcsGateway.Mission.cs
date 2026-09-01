using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static EntityQuery missionRootQuery;
        private static bool hasMissionRootQuery;
        private static uint cachedMissionResultVersion;
        private static byte cachedMissionSettlementAccepted;
        private static byte cachedMissionSettlementFirstClear;
        private static UiMissionResultPopupModel cachedMissionResult;
        private static World cachedMissionResultWorld;
        private static Entity cachedMissionResultRoot;
        private static FixedString64Bytes cachedMissionResultSession;
        private static int cachedMissionResultAttempt;
        private static bool hasCachedMatchHudStatus;
        private static World cachedMatchHudStatusWorld;
        private static Entity cachedMatchHudStatusBoundary;
        private static UiMatchHudStatusSurfacesComponent cachedMatchHudStatusComponent;
        private static UiMatchHudStatusSurfacesModel cachedMatchHudStatus;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetMissionProjectionCaches()
        {
            cachedMissionResultVersion = 0;
            cachedMissionSettlementAccepted = 0;
            cachedMissionSettlementFirstClear = 0;
            cachedMissionResult = UiMissionResultPopupModel.VictoryDefault;
            cachedMissionResultWorld = null;
            cachedMissionResultRoot = Entity.Null;
            cachedMissionResultSession = default;
            cachedMissionResultAttempt = 0;
            hasCachedMatchHudStatus = false;
            cachedMatchHudStatusWorld = null;
            cachedMatchHudStatusBoundary = Entity.Null;
            cachedMatchHudStatusComponent = default;
            cachedMatchHudStatus = UiMatchHudStatusSurfacesModel.Default;
        }

        public static bool TryEnqueueMissionResultAction(UiMissionResultActionKind action) =>
            UiShellActionAdapter.TryEnqueueMissionResultAction(action);

        public static bool TryRestartCurrentMission() =>
            UiShellActionAdapter.TryRestartCurrentMission();

        private static bool TryGetMissionRoot(out EntityManager entityManager, out Entity root)
        {
            entityManager = default;
            root = Entity.Null;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;
            if (cachedWorld != world)
                ResetWorldBoundQueries(world);
            if (!hasMissionRootQuery)
            {
                missionRootQuery = world.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<CampaignMissionRootComponent>());
                hasMissionRootQuery = true;
            }
            if (missionRootQuery.IsEmptyIgnoreFilter)
                return false;
            entityManager = world.EntityManager;
            root = missionRootQuery.GetSingletonEntity();
            return true;
        }
    }
}
