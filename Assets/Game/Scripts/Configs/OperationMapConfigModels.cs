using System;
using Game.Components;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct OperationMapBoundsConfig
    {
        [SerializeField] private Vector3 worldMin;
        [SerializeField] private Vector3 worldMax;
        [SerializeField] private Vector3 playableMin;
        [SerializeField] private Vector3 playableMax;
        [SerializeField] private Vector3 cameraMin;
        [SerializeField] private Vector3 cameraMax;

        public Vector3 WorldMin => worldMin;
        public Vector3 WorldMax => worldMax;
        public Vector3 PlayableMin => playableMin;
        public Vector3 PlayableMax => playableMax;
        public Vector3 CameraMin => cameraMin;
        public Vector3 CameraMax => cameraMax;

        public OperationMapBoundsConfig(
            Vector3 worldMin,
            Vector3 worldMax,
            Vector3 playableMin,
            Vector3 playableMax,
            Vector3 cameraMin,
            Vector3 cameraMax)
        {
            this.worldMin = worldMin;
            this.worldMax = worldMax;
            this.playableMin = playableMin;
            this.playableMax = playableMax;
            this.cameraMin = cameraMin;
            this.cameraMax = cameraMax;
        }

        public bool TryValidate(out string error)
        {
            if (!OperationMapConfigValidation.IsFinite(worldMin) ||
                !OperationMapConfigValidation.IsFinite(worldMax) ||
                !OperationMapConfigValidation.IsFinite(playableMin) ||
                !OperationMapConfigValidation.IsFinite(playableMax) ||
                !OperationMapConfigValidation.IsFinite(cameraMin) ||
                !OperationMapConfigValidation.IsFinite(cameraMax))
            {
                error = "Operation-map bounds must contain only finite values.";
                return false;
            }

            if (!OperationMapConfigValidation.HasValidExtent(worldMin, worldMax) ||
                !OperationMapConfigValidation.HasValidExtent(playableMin, playableMax) ||
                !OperationMapConfigValidation.HasValidExtent(cameraMin, cameraMax))
            {
                error = "World, playable, and camera bounds require positive X/Z extents and an ordered vertical range.";
                return false;
            }

            if (!OperationMapConfigValidation.Contains(worldMin, worldMax, playableMin, playableMax) ||
                !OperationMapConfigValidation.Contains(worldMin, worldMax, cameraMin, cameraMax))
            {
                error = "Playable and camera bounds must remain inside world bounds.";
                return false;
            }

            error = null;
            return true;
        }
    }

    [Serializable]
    public struct OperationMapGridMetadataConfig
    {
        [SerializeField] private string assetGuid;
        [SerializeField] private string contentHash;
        [SerializeField] private Vector3 origin;
        [SerializeField] private Vector2Int dimensions;
        [SerializeField] private float cellSize;
        [SerializeField] private int authoredBlockedCellCount;

        public string AssetGuid => assetGuid;
        public string ContentHash => contentHash;
        public Vector3 Origin => origin;
        public Vector2Int Dimensions => dimensions;
        public float CellSize => cellSize;
        public int AuthoredBlockedCellCount => authoredBlockedCellCount;

        public OperationMapGridMetadataConfig(
            string assetGuid,
            string contentHash,
            Vector3 origin,
            Vector2Int dimensions,
            float cellSize,
            int authoredBlockedCellCount)
        {
            this.assetGuid = assetGuid;
            this.contentHash = contentHash;
            this.origin = origin;
            this.dimensions = dimensions;
            this.cellSize = cellSize;
            this.authoredBlockedCellCount = authoredBlockedCellCount;
        }

        public bool TryValidate(out string error)
        {
            if (!OperationMapHashRules.IsValidHash128(assetGuid) ||
                !OperationMapHashRules.IsValidSha256(contentHash))
            {
                error = "Grid metadata requires a lowercase asset GUID and SHA-256 content hash.";
                return false;
            }

            if (!OperationMapConfigValidation.IsFinite(origin) ||
                dimensions.x <= 0 || dimensions.y <= 0 ||
                !OperationMapConfigValidation.IsFinitePositive(cellSize) ||
                authoredBlockedCellCount < 0)
            {
                error = "Grid metadata requires a finite origin, positive dimensions/cell size, and a non-negative blocked-cell count.";
                return false;
            }

            error = null;
            return true;
        }
    }

    [Serializable]
    public struct OperationMapSurfaceMetadataConfig
    {
        [SerializeField] private string assetGuid;
        [SerializeField] private string contentHash;
        [SerializeField] private string runtimeBlobHash;
        [SerializeField] private int surfaceCount;
        [SerializeField] private int payloadVersion;
        [SerializeField] private byte payloadEncoding;
        [SerializeField] private float minimumHeight;
        [SerializeField] private float maximumHeight;

        public string AssetGuid => assetGuid;
        public string ContentHash => contentHash;
        public string RuntimeBlobHash => runtimeBlobHash;
        public int SurfaceCount => surfaceCount;
        public int PayloadVersion => payloadVersion;
        public byte PayloadEncoding => payloadEncoding;
        public float MinimumHeight => minimumHeight;
        public float MaximumHeight => maximumHeight;

        public OperationMapSurfaceMetadataConfig(
            string assetGuid,
            string contentHash,
            string runtimeBlobHash,
            int surfaceCount,
            int payloadVersion,
            byte payloadEncoding,
            float minimumHeight,
            float maximumHeight)
        {
            this.assetGuid = assetGuid;
            this.contentHash = contentHash;
            this.runtimeBlobHash = runtimeBlobHash;
            this.surfaceCount = surfaceCount;
            this.payloadVersion = payloadVersion;
            this.payloadEncoding = payloadEncoding;
            this.minimumHeight = minimumHeight;
            this.maximumHeight = maximumHeight;
        }

        public bool TryValidate(out string error)
        {
            if (!OperationMapHashRules.IsValidHash128(assetGuid) ||
                !OperationMapHashRules.IsValidSha256(contentHash) ||
                !OperationMapHashRules.IsValidHash128(runtimeBlobHash))
            {
                error = "Surface metadata requires lowercase asset/runtime identities and a SHA-256 content hash.";
                return false;
            }

            if (surfaceCount <= 0 || payloadVersion <= 0 ||
                !OperationMapConfigValidation.IsFinite(minimumHeight) ||
                !OperationMapConfigValidation.IsFinite(maximumHeight) ||
                maximumHeight < minimumHeight)
            {
                error = "Surface metadata requires positive counts/version and an ordered finite height range.";
                return false;
            }

            error = null;
            return true;
        }
    }

    [Serializable]
    public struct OperationMapNavigationMetadataConfig
    {
        [SerializeField] private string authoredSubSceneGuid;
        [SerializeField] private long gridAuthoringLocalId;
        [SerializeField] private int staticGridBlockerCount;
        [SerializeField] private bool usesSurfaceMovementMetadata;
        [SerializeField] private bool supportsDynamicBlockers;
        [SerializeField] private bool supportsDynamicOccupancy;

        public string AuthoredSubSceneGuid => authoredSubSceneGuid;
        public long GridAuthoringLocalId => gridAuthoringLocalId;
        public int StaticGridBlockerCount => staticGridBlockerCount;
        public bool UsesSurfaceMovementMetadata => usesSurfaceMovementMetadata;
        public bool SupportsDynamicBlockers => supportsDynamicBlockers;
        public bool SupportsDynamicOccupancy => supportsDynamicOccupancy;

        public OperationMapNavigationMetadataConfig(
            string authoredSubSceneGuid,
            long gridAuthoringLocalId,
            int staticGridBlockerCount,
            bool usesSurfaceMovementMetadata,
            bool supportsDynamicBlockers,
            bool supportsDynamicOccupancy)
        {
            this.authoredSubSceneGuid = authoredSubSceneGuid;
            this.gridAuthoringLocalId = gridAuthoringLocalId;
            this.staticGridBlockerCount = staticGridBlockerCount;
            this.usesSurfaceMovementMetadata = usesSurfaceMovementMetadata;
            this.supportsDynamicBlockers = supportsDynamicBlockers;
            this.supportsDynamicOccupancy = supportsDynamicOccupancy;
        }

        public bool TryValidate(out string error)
        {
            if (!OperationMapHashRules.IsValidHash128(authoredSubSceneGuid) ||
                gridAuthoringLocalId <= 0 || staticGridBlockerCount < 0)
            {
                error = "Navigation metadata requires a lowercase subscene GUID, positive grid-authoring local id, and non-negative static-blocker count.";
                return false;
            }

            if (!usesSurfaceMovementMetadata)
            {
                error = "Navigation metadata must resolve path movement through the operation-map surface payload.";
                return false;
            }

            error = null;
            return true;
        }
    }

    [Serializable]
    public struct OperationMapCameraConfig
    {
        [SerializeField] private string cameraId;
        [SerializeField] private Vector3 position;
        [SerializeField] private Vector3 eulerAngles;
        [SerializeField] private bool orthographic;
        [SerializeField] private float fieldOfView;
        [SerializeField] private float orthographicSize;
        [SerializeField] private bool clampToCameraBounds;

        public string CameraId => cameraId;
        public Vector3 Position => position;
        public Vector3 EulerAngles => eulerAngles;
        public bool Orthographic => orthographic;
        public float FieldOfView => fieldOfView;
        public float OrthographicSize => orthographicSize;
        public bool ClampToCameraBounds => clampToCameraBounds;

        public OperationMapCameraConfig(
            string cameraId,
            Vector3 position,
            Vector3 eulerAngles,
            bool orthographic,
            float fieldOfView,
            float orthographicSize,
            bool clampToCameraBounds)
        {
            this.cameraId = cameraId;
            this.position = position;
            this.eulerAngles = eulerAngles;
            this.orthographic = orthographic;
            this.fieldOfView = fieldOfView;
            this.orthographicSize = orthographicSize;
            this.clampToCameraBounds = clampToCameraBounds;
        }

        public bool TryValidate(out string error)
        {
            if (!OperationMapIdentityRules.IsValidCameraId(cameraId))
            {
                error = $"Invalid operation-map camera id: '{cameraId ?? "<null>"}'.";
                return false;
            }

            if (!OperationMapConfigValidation.IsFinite(position) ||
                !OperationMapConfigValidation.IsFinite(eulerAngles))
            {
                error = $"Camera '{cameraId}' transform must contain only finite values.";
                return false;
            }

            if (!OperationMapConfigValidation.IsFinite(fieldOfView) ||
                !OperationMapConfigValidation.IsFinite(orthographicSize))
            {
                error = $"Camera '{cameraId}' projection values must be finite.";
                return false;
            }

            if (orthographic)
            {
                if (!OperationMapConfigValidation.IsFinitePositive(orthographicSize))
                {
                    error = $"Orthographic camera '{cameraId}' must have a positive size.";
                    return false;
                }
            }
            else if (!OperationMapConfigValidation.IsFinite(fieldOfView) ||
                     fieldOfView <= 1f || fieldOfView >= 179f)
            {
                error = $"Perspective camera '{cameraId}' field of view must be between 1 and 179 degrees.";
                return false;
            }

            error = null;
            return true;
        }
    }

    [Serializable]
    public struct OperationMapMinimapConfig
    {
        [SerializeField] private string minimapId;
        [SerializeField] private Vector3 projectionOrigin;
        [SerializeField] private Vector2 projectionSize;
        [SerializeField] private float orientationDegrees;

        public string MinimapId => minimapId;
        public Vector3 ProjectionOrigin => projectionOrigin;
        public Vector2 ProjectionSize => projectionSize;
        public float OrientationDegrees => orientationDegrees;

        public OperationMapMinimapConfig(
            string minimapId,
            Vector3 projectionOrigin,
            Vector2 projectionSize,
            float orientationDegrees)
        {
            this.minimapId = minimapId;
            this.projectionOrigin = projectionOrigin;
            this.projectionSize = projectionSize;
            this.orientationDegrees = orientationDegrees;
        }

        public bool TryValidate(out string error)
        {
            if (!OperationMapIdentityRules.IsValidMinimapId(minimapId))
            {
                error = $"Invalid operation-map minimap id: '{minimapId ?? "<null>"}'.";
                return false;
            }

            if (!OperationMapConfigValidation.IsFinite(projectionOrigin) ||
                !OperationMapConfigValidation.IsFinite(projectionSize) ||
                projectionSize.x <= 0f || projectionSize.y <= 0f ||
                !OperationMapConfigValidation.IsFinite(orientationDegrees))
            {
                error = $"Minimap '{minimapId}' requires a finite origin/orientation and positive projection size.";
                return false;
            }

            error = null;
            return true;
        }
    }

    [Serializable]
    public struct OperationMapAnchorConfig
    {
        [SerializeField] private string anchorId;
        [SerializeField] private OperationMapAnchorKind kind;
        [SerializeField] private Vector3 position;
        [SerializeField] private Vector3 eulerAngles;
        [SerializeField] private float radius;
        [SerializeField] private int factionId;
        [SerializeField] private int laneIndex;

        public string AnchorId => anchorId;
        public OperationMapAnchorKind Kind => kind;
        public Vector3 Position => position;
        public Vector3 EulerAngles => eulerAngles;
        public float Radius => radius;
        public int FactionId => factionId;
        public int LaneIndex => laneIndex;

        public OperationMapAnchorConfig(
            string anchorId,
            OperationMapAnchorKind kind,
            Vector3 position,
            Vector3 eulerAngles,
            float radius,
            int factionId = -1,
            int laneIndex = -1)
        {
            this.anchorId = anchorId;
            this.kind = kind;
            this.position = position;
            this.eulerAngles = eulerAngles;
            this.radius = radius;
            this.factionId = factionId;
            this.laneIndex = laneIndex;
        }

        public bool TryValidate(out string error)
        {
            if (!OperationMapIdentityRules.IsValidAnchorId(anchorId))
            {
                error = $"Invalid operation-map anchor id: '{anchorId ?? "<null>"}'.";
                return false;
            }

            if (kind == OperationMapAnchorKind.None || !Enum.IsDefined(typeof(OperationMapAnchorKind), kind))
            {
                error = $"Anchor '{anchorId}' must use a defined non-None kind.";
                return false;
            }

            if (!OperationMapConfigValidation.IsFinite(position) ||
                !OperationMapConfigValidation.IsFinite(eulerAngles) ||
                !OperationMapConfigValidation.IsFinite(radius) || radius < 0f)
            {
                error = $"Anchor '{anchorId}' transform and radius must be finite, with a non-negative radius.";
                return false;
            }

            if ((kind == OperationMapAnchorKind.Runway || kind == OperationMapAnchorKind.Helipad) && radius <= 0f)
            {
                error = $"Infrastructure anchor '{anchorId}' requires a positive half-length or clearance radius.";
                return false;
            }

            if (factionId < -1 || laneIndex < -1)
            {
                error = $"Anchor '{anchorId}' faction and lane metadata must be -1 or non-negative.";
                return false;
            }

            error = null;
            return true;
        }
    }

    internal static class OperationMapConfigValidation
    {
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static bool IsFinite(Vector2 value) => IsFinite(value.x) && IsFinite(value.y);

        public static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        public static bool IsFinitePositive(float value) => IsFinite(value) && value > 0f;

        public static bool HasValidExtent(Vector3 min, Vector3 max) =>
            min.x < max.x && min.y <= max.y && min.z < max.z;

        public static bool Contains(Vector3 outerMin, Vector3 outerMax, Vector3 innerMin, Vector3 innerMax) =>
            innerMin.x >= outerMin.x && innerMin.y >= outerMin.y && innerMin.z >= outerMin.z &&
            innerMax.x <= outerMax.x && innerMax.y <= outerMax.y && innerMax.z <= outerMax.z;

        public static bool Contains(Vector3 min, Vector3 max, Vector3 point) =>
            point.x >= min.x && point.y >= min.y && point.z >= min.z &&
            point.x <= max.x && point.y <= max.y && point.z <= max.z;
    }
}
