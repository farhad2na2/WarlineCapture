using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    internal partial struct BuildingPlacementQueryCompositionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // RequireForUpdate intentionally omitted: disabled composition helper; OnUpdate never runs.
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public BuildingPlacementQueryUiSystemHelper.Context Create(BuildingGameplaySourceCompositionSystemHelper source)
        {
            return source.BuildingPlacementQueryUiSystemHelper.CreateContext(new BuildingPlacementQueryUiSystemHelper.Source(
                source.RuntimeBuildingSystem.Buildings,
                () => source.RuntimeBuildingSystem.CurrentActiveBuildingId,
                BuildingDefinitionPrefabSystemHelper.GetProductionCount,
                BuildingDefinitionPrefabSystemHelper.GetProductionPrefab,
                source.BuildingEntityManagerAccessSystem.TryGetEntityManager));
        }
    }
}
