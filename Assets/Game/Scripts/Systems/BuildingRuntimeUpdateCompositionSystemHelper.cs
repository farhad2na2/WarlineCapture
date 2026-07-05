using System;

namespace Game.Runtime
{
    public sealed class BuildingRuntimeUpdateCompositionSystemHelper
    {
        public readonly struct Context
        {
            public readonly Action UpdateBuildingStartupTick;
            public readonly Action UpdateBuildingSimulationTick;
            public readonly RuntimeBuildingEntityLinkRegistry RuntimeBuildingEntityLinks;
            public readonly Func<bool> IsStartupComplete;

            public bool StartupComplete => IsStartupComplete == null || IsStartupComplete();

            public Context(
                Action updateBuildingStartupTick,
                Action updateBuildingSimulationTick,
                RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks,
                Func<bool> isStartupComplete = null)
            {
                UpdateBuildingStartupTick = updateBuildingStartupTick;
                UpdateBuildingSimulationTick = updateBuildingSimulationTick;
                RuntimeBuildingEntityLinks = runtimeBuildingEntityLinks;
                IsStartupComplete = isStartupComplete;
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
}
