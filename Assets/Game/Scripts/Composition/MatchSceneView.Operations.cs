using Game.Configs;
using Unity.Entities;

namespace Game.Composition
{
    public sealed partial class MatchSceneView
    {
        private OperationsReconMissionConfig operationsMissionConfig;
        // W38: any catalog operation.* mission with a registered native config (was the literal "operation.o001").
        internal bool IsOperationsSession => OperationsMissionRegistry.IsRegistered(MissionId);
        private OperationsReconMissionConfig OperationsMissionConfig
        {
            get
            {
                if (operationsMissionConfig != null) return operationsMissionConfig;
                World world = World.DefaultGameObjectInjectionWorld;
                if (world != null && world.IsCreated &&
                    OperationsReconLaunchProjection.TryGet(world.EntityManager, out Entity root, out _) &&
                    world.EntityManager.HasComponent<OperationsReconLaunchReference>(root))
                    operationsMissionConfig = world.EntityManager.GetComponentObject<OperationsReconLaunchReference>(root).Definition;
                return operationsMissionConfig;
            }
        }
    }
}
