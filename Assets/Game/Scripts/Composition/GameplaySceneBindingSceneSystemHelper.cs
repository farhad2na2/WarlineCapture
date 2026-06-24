using UnityEngine;

public sealed class GameplaySceneBindingSceneSystemHelper
{
    public void BindRuntimeGridBlockerDebugViews(RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers)
    {
        GridAuthoring[] grids = Object.FindObjectsByType<GridAuthoring>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < grids.Length; i++)
        {
            GridAuthoring grid = grids[i];
            if (grid == null || !grid.gameObject.scene.IsValid())
                continue;

            grid.BindRuntimeGridBlockers(runtimeGridBlockers);
        }
    }
}
