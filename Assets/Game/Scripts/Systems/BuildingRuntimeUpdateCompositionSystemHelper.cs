using System;

public sealed class BuildingRuntimeUpdateCompositionSystemHelper
{
    public readonly struct Context
    {
        public readonly Action UpdateBuildingStartupTick;
        public readonly Action UpdateBuildingSimulationTick;
        public readonly RuntimeBuildingEntityLinkRegistry RuntimeBuildingEntityLinks;

        public Context(
            Action updateBuildingStartupTick,
            Action updateBuildingSimulationTick,
            RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks)
        {
            UpdateBuildingStartupTick = updateBuildingStartupTick;
            UpdateBuildingSimulationTick = updateBuildingSimulationTick;
            RuntimeBuildingEntityLinks = runtimeBuildingEntityLinks;
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
        context.RuntimeBuildingEntityLinks?.SyncLinks();
    }
}
