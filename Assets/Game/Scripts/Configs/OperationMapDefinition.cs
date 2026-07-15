using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Operation Maps/Operation Map Definition")]
    public sealed class OperationMapDefinition : ScriptableObject
    {
        [SerializeField] private string operationMapId;
        [SerializeField, Min(1)] private int schemaVersion = 1;
        [SerializeField, Min(1)] private int contentVersion = 1;
        [SerializeField] private string sourceIdentityHash;
        [SerializeField] private string contentHash;
        [SerializeField] private string generatedMetadataHash;
        [SerializeField] private OperationMapBoundsConfig bounds;
        [SerializeField] private OperationMapCameraConfig[] cameras = Array.Empty<OperationMapCameraConfig>();
        [SerializeField] private string planningCameraId;
        [SerializeField] private string battleCameraId;
        [SerializeField] private OperationMapMinimapConfig minimap;
        [SerializeField] private OperationMapAnchorConfig[] anchors = Array.Empty<OperationMapAnchorConfig>();

        public string OperationMapId => operationMapId;
        public int SchemaVersion => schemaVersion;
        public int ContentVersion => contentVersion;
        public string SourceIdentityHash => sourceIdentityHash;
        public string ContentHash => contentHash;
        public string GeneratedMetadataHash => generatedMetadataHash;
        public OperationMapBoundsConfig Bounds => bounds;
        public ReadOnlySpan<OperationMapCameraConfig> Cameras => cameras;
        public string PlanningCameraId => planningCameraId;
        public string BattleCameraId => battleCameraId;
        public OperationMapMinimapConfig Minimap => minimap;
        public ReadOnlySpan<OperationMapAnchorConfig> Anchors => anchors;

        public bool TryValidateIdentity(out string error)
        {
            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId))
            {
                error = $"Invalid operation-map id: '{operationMapId ?? "<null>"}'.";
                return false;
            }

            if (schemaVersion < 1 || contentVersion < 1)
            {
                error = "Schema and content versions must be positive.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryValidateMetadata(out string error)
        {
            if (!TryValidateIdentity(out error) ||
                !TryValidateHashes(out error) ||
                !bounds.TryValidate(out error))
                return false;

            if (cameras == null || cameras.Length == 0)
            {
                error = "At least one operation-map camera record is required.";
                return false;
            }

            for (int index = 0; index < cameras.Length; index++)
            {
                if (!cameras[index].TryValidate(out error))
                    return false;

                if (!OperationMapConfigValidation.Contains(bounds.CameraMin, bounds.CameraMax, cameras[index].Position))
                {
                    error = $"Camera '{cameras[index].CameraId}' position must remain inside camera bounds.";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(cameras[index].CameraId, cameras[previous].CameraId, StringComparison.Ordinal))
                    {
                        error = $"Duplicate operation-map camera id: '{cameras[index].CameraId}'.";
                        return false;
                    }
                }
            }

            if (!ContainsCamera(planningCameraId))
            {
                error = $"Planning camera id '{planningCameraId ?? "<null>"}' does not resolve to a camera record.";
                return false;
            }

            if (!ContainsCamera(battleCameraId))
            {
                error = $"Battle camera id '{battleCameraId ?? "<null>"}' does not resolve to a camera record.";
                return false;
            }

            if (!minimap.TryValidate(out error))
                return false;

            if (anchors == null || anchors.Length == 0)
            {
                error = "At least one typed operation-map anchor record is required.";
                return false;
            }

            for (int index = 0; index < anchors.Length; index++)
            {
                if (!anchors[index].TryValidate(out error))
                    return false;

                if (!OperationMapConfigValidation.Contains(bounds.WorldMin, bounds.WorldMax, anchors[index].Position))
                {
                    error = $"Anchor '{anchors[index].AnchorId}' position must remain inside world bounds.";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(anchors[index].AnchorId, anchors[previous].AnchorId, StringComparison.Ordinal))
                    {
                        error = $"Duplicate operation-map anchor id: '{anchors[index].AnchorId}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        public bool TryValidateHashes(out string error)
        {
            if (!OperationMapHashRules.IsValidSha256(sourceIdentityHash))
            {
                error = "Operation-map source identity hash must be 64 lowercase hexadecimal characters.";
                return false;
            }

            if (!OperationMapHashRules.IsValidSha256(contentHash))
            {
                error = "Operation-map content hash must be 64 lowercase hexadecimal characters.";
                return false;
            }

            if (!OperationMapHashRules.IsValidSha256(generatedMetadataHash))
            {
                error = "Operation-map generated-metadata hash must be 64 lowercase hexadecimal characters.";
                return false;
            }

            error = null;
            return true;
        }

        private bool ContainsCamera(string cameraId)
        {
            if (!OperationMapIdentityRules.IsValidCameraId(cameraId))
                return false;

            for (int index = 0; index < cameras.Length; index++)
            {
                if (string.Equals(cameraId, cameras[index].CameraId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }

    public static class OperationMapIdentityRules
    {
        public const int MaximumIdLength = 60;

        public static bool IsValidOperationMapId(string value)
        {
            Span<IdSegment> segments = stackalloc IdSegment[3];
            return TryParse(value, segments, out int count) &&
                count == 3 &&
                IsEqual(value, segments[0], "opmap") &&
                (IsEqual(value, segments[1], "skirmish") ||
                 IsNumbered(value, segments[1], 'c', 'h'));
        }

        public static bool IsValidScenarioId(string value)
        {
            Span<IdSegment> segments = stackalloc IdSegment[4];
            if (!TryParse(value, segments, out int count) ||
                !IsEqual(value, segments[0], "scenario"))
            {
                return false;
            }

            if (count == 3)
                return IsEqual(value, segments[1], "skirmish");

            return count == 4 &&
                IsNumbered(value, segments[1], 'c', 'h') &&
                IsNumbered(value, segments[2], 'm');
        }

        public static bool IsValidCameraId(string value) => IsValidScopedId(value, "camera", 3, 6);

        public static bool IsValidMinimapId(string value) => IsValidScopedId(value, "minimap", 3, 6);

        public static bool IsValidAnchorId(string value) => IsValidScopedId(value, "anchor", 3, 7);

        private static bool IsValidScopedId(string value, string requiredNamespace, int minimumSegments, int maximumSegments)
        {
            Span<IdSegment> segments = stackalloc IdSegment[7];
            return TryParse(value, segments.Slice(0, maximumSegments), out int count) &&
                count >= minimumSegments &&
                IsEqual(value, segments[0], requiredNamespace);
        }

        private static bool TryParse(string value, Span<IdSegment> segments, out int count)
        {
            count = 0;
            if (string.IsNullOrEmpty(value) || value.Length > MaximumIdLength)
                return false;

            int segmentStart = 0;
            for (int index = 0; index <= value.Length; index++)
            {
                if (index < value.Length && value[index] != '.')
                {
                    char character = value[index];
                    bool valid = character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_';
                    if (!valid || (index == segmentStart && character == '_'))
                        return false;
                    continue;
                }

                int length = index - segmentStart;
                if (length == 0 || count >= segments.Length)
                    return false;

                segments[count++] = new IdSegment(segmentStart, length);
                segmentStart = index + 1;
            }

            return true;
        }

        private static bool IsEqual(string value, IdSegment segment, string expected)
        {
            if (segment.Length != expected.Length)
                return false;

            for (int index = 0; index < segment.Length; index++)
            {
                if (value[segment.Start + index] != expected[index])
                    return false;
            }

            return true;
        }

        private static bool IsNumbered(
            string value,
            IdSegment segment,
            char firstPrefix,
            char secondPrefix = '\0')
        {
            int prefixLength = secondPrefix == '\0' ? 1 : 2;
            if (segment.Length <= prefixLength || value[segment.Start] != firstPrefix)
                return false;
            if (prefixLength == 2 && value[segment.Start + 1] != secondPrefix)
                return false;

            for (int index = prefixLength; index < segment.Length; index++)
            {
                char character = value[segment.Start + index];
                if (character is < '0' or > '9')
                    return false;
            }

            return true;
        }

        private readonly struct IdSegment
        {
            public readonly int Start;
            public readonly int Length;

            public IdSegment(int start, int length)
            {
                Start = start;
                Length = length;
            }
        }
    }

    public static class OperationMapHashRules
    {
        public const int Sha256HexLength = 64;

        public static bool IsValidSha256(string value)
        {
            if (value == null || value.Length != Sha256HexLength)
                return false;

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                bool isDigit = character is >= '0' and <= '9';
                bool isLowerHex = character is >= 'a' and <= 'f';
                if (!isDigit && !isLowerHex)
                    return false;
            }

            return true;
        }
    }
}
