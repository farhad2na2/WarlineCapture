using System.Collections.Generic;
using Game.Authoring;
using Game.Runtime;
using Unity.Entities;

namespace Game.Composition
{
    public sealed class GameplaySceneBindingSceneSystemHelper
    {
#if UNITY_EDITOR
        public void BindRuntimeGridBlockerDebugViews(
            RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers,
            World runtimeWorld,
            IReadOnlyList<GridAuthoring> grids)
        {
            if (grids == null)
                return;

            for (int i = 0; i < grids.Count; i++)
            {
                GridAuthoring grid = grids[i];
                if (grid == null)
                    continue;

                grid.BindRuntimeDebugSources(runtimeGridBlockers, runtimeWorld);
            }
        }
#endif
    }
}
