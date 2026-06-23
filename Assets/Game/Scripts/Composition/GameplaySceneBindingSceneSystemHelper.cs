using System.Collections.Generic;

public sealed class GameplaySceneBindingSceneSystemHelper
{
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
