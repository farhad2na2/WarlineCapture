using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static EntityQuery missionBriefingQuery;
        private static bool hasMissionBriefingQuery;

        private static bool TryGetMissionBriefingBoundary(
            out EntityManager entityManager,
            out Entity boundary)
        {
            entityManager = default;
            boundary = Entity.Null;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;
            if (cachedWorld != world)
                ResetWorldBoundQueries(world);
            if (!hasMissionBriefingQuery)
            {
                missionBriefingQuery = world.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<UiMissionBriefingComponent>());
                hasMissionBriefingQuery = true;
            }
            if (missionBriefingQuery.CalculateEntityCount() != 1)
                return false;
            entityManager = world.EntityManager;
            boundary = missionBriefingQuery.GetSingletonEntity();
            return true;
        }
    }
}
