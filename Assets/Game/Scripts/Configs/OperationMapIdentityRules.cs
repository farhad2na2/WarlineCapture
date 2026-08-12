using System;

namespace Game.Configs
{
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

        public static bool IsValidSourceGlobalObjectId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 127)
                return false;

            string[] segments = value.Split('-');
            if (segments.Length != 5 ||
                !string.Equals(segments[0], "GlobalObjectId_V1", StringComparison.Ordinal) ||
                !uint.TryParse(segments[1], out _) || segments[2].Length != 32 ||
                !ulong.TryParse(segments[3], out _) || !ulong.TryParse(segments[4], out _))
                return false;

            for (int index = 0; index < segments[2].Length; index++)
            {
                char character = segments[2][index];
                if (!(character is >= '0' and <= '9' or >= 'a' and <= 'f'))
                    return false;
            }

            return true;
        }

        public static bool IsValidGeneratedStableId(string value)
        {
            const string prefix = "densecity.";
            if (string.IsNullOrEmpty(value) || value.Length != prefix.Length + 64 ||
                !value.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            for (int index = prefix.Length; index < value.Length; index++)
            {
                char character = value[index];
                if (!(character is >= '0' and <= '9' or >= 'a' and <= 'f'))
                    return false;
            }

            return true;
        }

        public static bool IsValidScenarioId(string value)
        {
            Span<IdSegment> segments = stackalloc IdSegment[4];
            if (!TryParse(value, segments, out int count) ||
                !IsEqual(value, segments[0], "scenario"))
                return false;

            if (count == 3)
                return IsEqual(value, segments[1], "skirmish");

            return count == 4 &&
                IsNumbered(value, segments[1], 'c', 'h') &&
                IsNumbered(value, segments[2], 'm');
        }

        public static bool IsValidCameraId(string value) => IsValidScopedId(value, "camera", 3, 6);
        public static bool IsValidMinimapId(string value) => IsValidScopedId(value, "minimap", 3, 6);
        public static bool IsValidAnchorId(string value) => IsValidScopedId(value, "anchor", 3, 7);

        private static bool IsValidScopedId(
            string value, string requiredNamespace, int minimumSegments, int maximumSegments)
        {
            Span<IdSegment> segments = stackalloc IdSegment[7];
            return TryParse(value, segments.Slice(0, maximumSegments), out int count) &&
                count >= minimumSegments && IsEqual(value, segments[0], requiredNamespace);
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
                if (value[segment.Start + index] != expected[index]) return false;
            return true;
        }

        private static bool IsNumbered(
            string value, IdSegment segment, char firstPrefix, char secondPrefix = '\0')
        {
            int prefixLength = secondPrefix == '\0' ? 1 : 2;
            if (segment.Length <= prefixLength || value[segment.Start] != firstPrefix)
                return false;
            if (prefixLength == 2 && value[segment.Start + 1] != secondPrefix)
                return false;

            for (int index = prefixLength; index < segment.Length; index++)
                if (value[segment.Start + index] is < '0' or > '9') return false;
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
        public const int Hash128HexLength = 32;
        public const int Sha256HexLength = 64;

        public static bool IsValidHash128(string value) => IsValidLowerHex(value, Hash128HexLength);
        public static bool IsValidSha256(string value) => IsValidLowerHex(value, Sha256HexLength);

        private static bool IsValidLowerHex(string value, int expectedLength)
        {
            if (value == null || value.Length != expectedLength)
                return false;

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!(character is >= '0' and <= '9' or >= 'a' and <= 'f'))
                    return false;
            }

            return true;
        }
    }
}
