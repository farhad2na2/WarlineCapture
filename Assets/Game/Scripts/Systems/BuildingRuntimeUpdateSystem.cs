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
