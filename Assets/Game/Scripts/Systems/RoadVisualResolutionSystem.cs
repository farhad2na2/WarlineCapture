using UnityEngine;
using RoadVisualType = RoadNetworkSystem.RoadVisualType;
using TileConnectionMask = RoadNetworkSystem.TileConnectionMask;
using VariantData = RoadVisualVariantSystem.VariantData;

internal sealed class RoadVisualResolutionSystem
{
    public readonly struct Context
    {
        public readonly RoadNetworkSystem RoadNetworkSystem;
        public readonly RoadVisualVariantSystem RoadVisualVariantSystem;
        public readonly RoadBuildVisualContextSystem RoadBuildVisualContextSystem;
        public readonly RoadBuildVisualContextSystem.Context VisualContext;

        public Context(
            RoadNetworkSystem roadNetworkSystem,
            RoadVisualVariantSystem roadVisualVariantSystem,
            RoadBuildVisualContextSystem roadBuildVisualContextSystem,
            RoadBuildVisualContextSystem.Context visualContext)
        {
            RoadNetworkSystem = roadNetworkSystem;
            RoadVisualVariantSystem = roadVisualVariantSystem;
            RoadBuildVisualContextSystem = roadBuildVisualContextSystem;
            VisualContext = visualContext;
        }
    }

    public RoadVisualType ResolveVisualType(Context context, Vector2Int cell, TileConnectionMask mask)
    {
        if (context.RoadNetworkSystem.AutobahnConnectorCells.Contains(cell))
            return RoadVisualType.AutobahnConnect;

        if (context.RoadNetworkSystem.AutobahnCells.Contains(cell))
            return RoadVisualType.Autobahn;

        bool isStraight = (mask.North && mask.South) || (mask.East && mask.West);
        if (isStraight)
        {
            // Straight roads keep using the standard road visuals unless explicitly marked as autobahn.
        }

        switch (mask.Count)
        {
            case 0:
                return RoadVisualType.None;

            case 1:
                return RoadVisualType.End;

            case 2:
                if (mask.North && mask.South)
                    return RoadVisualType.Straight;

                if (mask.East && mask.West)
                    return RoadVisualType.Straight;

                if (mask.North && mask.East)
                    return RoadVisualType.Corner;
                return RoadVisualType.Corner;

            case 3:
                return RoadVisualType.TIntersection;

            default:
                return RoadVisualType.Intersection;
        }
    }

    public GameObject GetPrefab(Context context, RoadVisualType type)
    {
        return context.RoadBuildVisualContextSystem.GetPrefab(context.VisualContext, type);
    }

    public bool TryGetVariant(Context context, RoadVisualType type, TileConnectionMask mask, out VariantData variant)
    {
        return context.RoadVisualVariantSystem.TryGetVariant(type, mask, out variant);
    }
}
