using System;
using Unity.Entities;

public sealed partial class BuildingRuntimeUpdateSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public readonly struct Context
    {
        public readonly Action UpdateBuildingStartupTick;
        public readonly Action UpdateBuildingSimulationTick;

        public Context(Action updateBuildingStartupTick, Action updateBuildingSimulationTick)
        {
            UpdateBuildingStartupTick = updateBuildingStartupTick;
            UpdateBuildingSimulationTick = updateBuildingSimulationTick;
        }
    }

    public void UpdateStartup(Context context)
    {
        context.UpdateBuildingStartupTick?.Invoke();
    }

    public void Update(Context context)
    {
        UpdateSimulation(context);
    }

    public void UpdateSimulation(Context context)
    {
        context.UpdateBuildingSimulationTick?.Invoke();
    }
}
