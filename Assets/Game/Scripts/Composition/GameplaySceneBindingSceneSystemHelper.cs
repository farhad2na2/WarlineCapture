using System.Collections.Generic;

public sealed class GameplaySceneBindingSceneSystemHelper
{
    public void BindRuntimeGridBlockerDebugViews(
        RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers,
        IReadOnlyList<GridAuthoring> grids)
    {
        if (grids == null)
            return;

        for (int i = 0; i < grids.Count; i++)
        {
            GridAuthoring grid = grids[i];
            if (grid == null)
                continue;

            grid.BindRuntimeGridBlockers(runtimeGridBlockers);
        }
    }
}
