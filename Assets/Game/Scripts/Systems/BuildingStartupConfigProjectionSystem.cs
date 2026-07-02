using Unity.Entities;
using Game.Configs;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    internal partial struct BuildingStartupConfigProjectionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // RequireForUpdate intentionally omitted: disabled startup helper; composition calls its methods directly.
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public static int ResolveInitialDollars(BuildingPlacementSystemConfig buildingPlacementConfig)
        {
            return buildingPlacementConfig != null && buildingPlacementConfig.InitialUnitsConfig != null
                ? buildingPlacementConfig.InitialUnitsConfig.InitialDollars
                : 0;
        }
    }
}
