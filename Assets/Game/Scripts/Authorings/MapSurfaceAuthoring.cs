using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Game.Components;
using Game.Configs;

namespace Game.Authoring
{
    [Serializable]
    public struct MapSurfaceSceneOverlayAuthoringData
    {
        public Vector3 Center;
        public Quaternion Rotation;
        public Vector2 HalfExtents;
        public float Height;
        public Vector3 Normal;
        public MapSurfaceType SurfaceType;
        public MapSurfaceMovementMask MovementMask;
        public MapSurfaceFlags Flags;
        public int LayerId;

        public MapSurfaceSceneOverlay ToRuntimeOverlay()
        {
            return new MapSurfaceSceneOverlay
            {
                Center = Center,
                Rotation = Rotation,
                HalfExtents = HalfExtents,
                Height = Height,
                Normal = Normal,
                SurfaceType = SurfaceType,
                MovementMask = MovementMask,
                Flags = Flags,
                LayerId = LayerId
            };
        }
    }

    [DisallowMultipleComponent]
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Game.Authoring", sourceClassName: "MapSurfaceAuthoring")]
    public sealed class MapSurfaceAuthoring : MonoBehaviour
    {
        [SerializeField] private MapSurfaceDataAsset bakedSurfaceData;
        [SerializeField] private GridAuthoringConfig gridConfig;
        [SerializeField, Min(1)] private int samplesPerCellAxis = 2;
        [SerializeField, Min(0.01f)] private float maxSampleHeightDelta = 0.05f;
        [SerializeField, Min(0f)] private float maxBuildingSlopeDegrees = 8f;
        [SerializeField, Min(0f)] private float maxInfantrySlopeDegrees = 35f;
        [SerializeField, Min(0f)] private float maxVehicleSlopeDegrees = 22f;
        [SerializeField] private MapSurfaceSceneOverlayAuthoringData[] sceneOverlays =
            Array.Empty<MapSurfaceSceneOverlayAuthoringData>();

        public MapSurfaceDataAsset BakedSurfaceData => bakedSurfaceData;
        public GridAuthoringConfig GridConfig => gridConfig;
        public int SamplesPerCellAxis => samplesPerCellAxis;
        public float MaxSampleHeightDelta => maxSampleHeightDelta;
        public float MaxBuildingSlopeDegrees => maxBuildingSlopeDegrees;
        public float MaxInfantrySlopeDegrees => maxInfantrySlopeDegrees;
        public float MaxVehicleSlopeDegrees => maxVehicleSlopeDegrees;
        public MapSurfaceSceneOverlayAuthoringData[] SceneOverlays =>
            sceneOverlays ?? Array.Empty<MapSurfaceSceneOverlayAuthoringData>();

        [BakingVersion("WarlineCapture", 1)]
        private sealed class Baker : Baker<MapSurfaceAuthoring>
        {
            public override void Bake(MapSurfaceAuthoring authoring)
            {
                MapSurfaceDataAsset surfaceData = authoring.BakedSurfaceData;
                if (surfaceData == null)
                    return;

                DependsOn(surfaceData);

                Unity.Entities.Hash128 surfaceHash = surfaceData.ComputeRuntimeBlobHash();
                if (!TryGetBlobAssetReference(surfaceHash, out BlobAssetReference<MapSurfaceBlob> surfaceBlob))
                {
                    if (!surfaceData.TryCreateRuntimeBlobAsset(Allocator.Persistent, out surfaceBlob))
                        return;

                    AddBlobAssetWithCustomHash(ref surfaceBlob, surfaceHash);
                }

                Entity entity = GetEntity(TransformUsageFlags.None);
                ref MapSurfaceBlob blob = ref surfaceBlob.Value;
                GetSurfaceFeatureFlags(ref blob, out byte hasLayeredCells, out byte hasRoadSurfaces, out byte hasBridgeSurfaces);

                AddComponent(entity, new MapSurfaceComponent
                {
                    SurfaceBlob = surfaceBlob,
                    GridOrigin = blob.GridOrigin,
                    CellSize = blob.CellSize,
                    Dimensions = blob.Dimensions,
                    HasSurfaceData = 1,
                    HasLayeredCells = hasLayeredCells,
                    HasRoadSurfaces = hasRoadSurfaces,
                    HasBridgeSurfaces = hasBridgeSurfaces
                });
                AddComponent(entity, new MapSurfacePathCostComponent
                {
                    EnableSlopeCost = 0,
                    GentleSlopeTraversalCost = 0,
                    SteepSlopeTraversalCost = 0
                });
            }

            private static void GetSurfaceFeatureFlags(
                ref MapSurfaceBlob blob,
                out byte hasLayeredCells,
                out byte hasRoadSurfaces,
                out byte hasBridgeSurfaces)
            {
                hasLayeredCells = 0;
                hasRoadSurfaces = 0;
                hasBridgeSurfaces = 0;

                hasLayeredCells = (byte)(MapSurfaceBlobAccess.IsLayered(ref blob) ? 1 : 0);

                int surfaceCount = MapSurfaceBlobAccess.SurfaceCount(ref blob);
                for (int i = 0; i < surfaceCount; i++)
                {
                    if (!MapSurfaceBlobAccess.TryGetSurfaceByIndex(ref blob, i, out MapSurfaceSample sample))
                        continue;

                    if (sample.SurfaceType == MapSurfaceType.Road ||
                        sample.SurfaceType == MapSurfaceType.DirtRoad ||
                        sample.SurfaceType == MapSurfaceType.Highway ||
                        sample.SurfaceType == MapSurfaceType.BridgeDeck ||
                        sample.SurfaceType == MapSurfaceType.Ramp ||
                        (sample.Flags & MapSurfaceFlags.Road) != 0)
                    {
                        hasRoadSurfaces = 1;
                    }

                    if (sample.SurfaceType == MapSurfaceType.BridgeDeck ||
                        (sample.Flags & MapSurfaceFlags.Bridge) != 0)
                    {
                        hasBridgeSurfaces = 1;
                    }

                    if (hasRoadSurfaces != 0 && hasBridgeSurfaces != 0)
                        break;
                }
            }
        }
    }
}
