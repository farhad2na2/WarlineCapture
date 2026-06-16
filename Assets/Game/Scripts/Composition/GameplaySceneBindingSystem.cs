using System.Collections.Generic;
using Unity.Entities;

public sealed partial class GameplaySceneBindingSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void BindRuntimeGridBlockerDebugViews(RuntimeGridBlockerSystem runtimeGridBlockers)
    {
        IReadOnlyList<GridAuthoring> grids = GridAuthoring.Instances;
        for (int i = 0; i < grids.Count; i++)
        {
            GridAuthoring grid = grids[i];
            if (grid == null || !grid.gameObject.scene.IsValid())
                continue;

            grid.BindRuntimeGridBlockers(runtimeGridBlockers);
        }
    }
}
