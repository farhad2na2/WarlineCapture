using System;

public sealed class BuildingRuntimeUpdateSystem
{
    public readonly struct Context
    {
        public readonly Action UpdateBuildingRuntime;

        public Context(Action updateBuildingRuntime)
        {
            UpdateBuildingRuntime = updateBuildingRuntime;
        }
    }

    public void Update(Context context)
    {
        context.UpdateBuildingRuntime?.Invoke();
    }
}
