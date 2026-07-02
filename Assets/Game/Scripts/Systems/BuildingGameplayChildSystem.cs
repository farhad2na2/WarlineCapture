using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    internal partial struct BuildingGameplayChildSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // RequireForUpdate intentionally omitted: disabled composition helper; OnUpdate never runs.
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public BuildingGameplaySourceCompositionSystemHelper Create()
        {
            return new BuildingGameplaySourceCompositionSystemHelper();
        }
    }
}
