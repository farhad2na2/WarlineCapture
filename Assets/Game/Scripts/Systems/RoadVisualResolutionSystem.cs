using UnityEngine;
using Unity.Entities;
using RoadVisualType = RoadNetworkCompositionSystemHelper.RoadVisualType;
using TileConnectionMask = RoadNetworkCompositionSystemHelper.TileConnectionMask;
using VariantData = RoadVisualVariantSystem.VariantData;

internal sealed partial class RoadVisualResolutionSystem : SystemBase
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
        public readonly RoadNetworkCompositionSystemHelper RoadNetworkCompositionSystemHelper;
        public readonly RoadVisualVariantSystem RoadVisualVariantSystem;
        public readonly RoadBuildVisualContextSystem.Context VisualContext;

        public Context(
            RoadNetworkCompositionSystemHelper roadNetworkSystem,
            RoadVisualVariantSystem roadVisualVariantSystem,
            RoadBuildVisualContextSystem.Context visualContext)
        {
            RoadNetworkCompositionSystemHelper = roadNetworkSystem;
            RoadVisualVariantSystem = roadVisualVariantSystem;
            VisualContext = visualContext;
        }
    }

    public static RoadVisualType ResolveVisualType(Context context, Vector2Int cell, TileConnectionMask mask)
    {
        if (context.RoadNetworkCompositionSystemHelper.AutobahnConnectorCells.Contains(cell))
            return RoadVisualType.AutobahnConnect;

        if (context.RoadNetworkCompositionSystemHelper.AutobahnCells.Contains(cell))
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

    public static GameObject GetPrefab(Context context, RoadVisualType type)
    {
        return RoadBuildVisualContextSystem.GetPrefab(context.VisualContext, type);
    }

    public static bool TryGetVariant(Context context, RoadVisualType type, TileConnectionMask mask, out VariantData variant)
    {
        variant = default;
        return context.RoadVisualVariantSystem != null &&
               context.RoadVisualVariantSystem.TryGetVariant(type, mask, out variant);
    }
}
