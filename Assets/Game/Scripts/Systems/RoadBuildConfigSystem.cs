using Unity.Entities;
using UnityEngine;

public sealed partial class RoadBuildConfigSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public readonly struct Snapshot
    {
        public readonly Camera WorldCamera;
        public readonly GameObject StraightPrefab;
        public readonly GameObject TIntersectionPrefab;
        public readonly GameObject IntersectionPrefab;
        public readonly GameObject EndPrefab;
        public readonly GameObject CornerPrefab;
        public readonly GameObject AutobahnPrefab;
        public readonly GameObject AutobahnConnectPrefab;
        public readonly Vector3 GridOrigin;
        public readonly float BuildPlaneY;
        public readonly float RoadGridSize;
        public readonly int ChunkSizeInCells;
        public readonly float PreviewAlpha;
        public readonly GameObject SoldierBasePrefab;
        public readonly Vector2Int SoldierBaseFootprintCells;
        public readonly float PlacementOutlineHeight;
        public readonly float PlacementOutlineWidth;
        public readonly Color PlacementValidColor;
        public readonly Color PlacementInvalidColor;

        public Snapshot(RoadBuildSystemConfig config)
        {
            WorldCamera = config.WorldCamera;
            StraightPrefab = config.StraightPrefab;
            TIntersectionPrefab = config.TIntersectionPrefab;
            IntersectionPrefab = config.IntersectionPrefab;
            EndPrefab = config.EndPrefab;
            CornerPrefab = config.CornerPrefab;
            AutobahnPrefab = config.AutobahnPrefab;
            AutobahnConnectPrefab = config.AutobahnConnectPrefab;
            GridOrigin = config.GridOrigin;
            BuildPlaneY = config.BuildPlaneY;
            RoadGridSize = config.RoadGridSize;
            ChunkSizeInCells = config.ChunkSizeInCells;
            PreviewAlpha = config.PreviewAlpha;
            SoldierBasePrefab = config.SoldierBasePrefab;
            SoldierBaseFootprintCells = config.SoldierBaseFootprintCells;
            PlacementOutlineHeight = config.PlacementOutlineHeight;
            PlacementOutlineWidth = config.PlacementOutlineWidth;
            PlacementValidColor = config.PlacementValidColor;
            PlacementInvalidColor = config.PlacementInvalidColor;
        }
    }

    public bool TryCreateSnapshot(RoadBuildSystemConfig config, out Snapshot snapshot)
    {
        if (config == null)
        {
            snapshot = default;
            return false;
        }

        snapshot = new Snapshot(config);
        return true;
    }
}
