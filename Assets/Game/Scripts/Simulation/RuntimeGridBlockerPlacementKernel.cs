using System;

namespace Game.Simulation
{
    public enum BlockerPrefabKind
    {
        Other,
        Tree,
        Rock,
        Bush,
        Grass
    }

    /// <summary>Shared data rules used by the runtime blocker owner and headless mission setup.</summary>
    public static class RuntimeGridBlockerPlacementKernel
    {
        public static BlockerPrefabKind ClassifyPrefabKind(string prefabName)
        {
            string name = prefabName.ToLowerInvariant();
            if (name.Contains("tree") || name.Contains("palm"))
                return BlockerPrefabKind.Tree;
            if (name.Contains("rock") || name.Contains("stone") || name.Contains("boulder"))
                return BlockerPrefabKind.Rock;
            if (name.Contains("bush") || name.Contains("shrub"))
                return BlockerPrefabKind.Bush;
            if (name.Contains("grass") || name.Contains("plant") || name.Contains("fern"))
                return BlockerPrefabKind.Grass;

            return BlockerPrefabKind.Other;
        }

        public static void ComputeFootprintCells(
            bool blocksOnlyCenterCell,
            bool hasBounds,
            float boundsWidth,
            float boundsDepth,
            float cellSize,
            out int width,
            out int depth)
        {
            if (blocksOnlyCenterCell)
            {
                width = 1;
                depth = 1;
                return;
            }
            if (!hasBounds || cellSize <= 0f)
            {
                width = 1;
                depth = 1;
                return;
            }

            // Keep division in float, matching the original math.ceil(float) owner.
            width = Math.Max(1, (int)Math.Ceiling(boundsWidth / cellSize));
            depth = Math.Max(1, (int)Math.Ceiling(boundsDepth / cellSize));
        }
    }
}
