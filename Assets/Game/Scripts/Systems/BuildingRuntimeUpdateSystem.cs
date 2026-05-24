using System;

public sealed class BuildingRuntimeUpdateSystem
{
    public readonly struct Context
    {
        public readonly Action UpdateBuildingRuntimeTick;

        public Context(Action updateBuildingRuntimeTick)
        {
            UpdateBuildingRuntimeTick = updateBuildingRuntimeTick;
        }
    }

    public void Update(Context context)
    {
        context.UpdateBuildingRuntimeTick?.Invoke();
    }
}
