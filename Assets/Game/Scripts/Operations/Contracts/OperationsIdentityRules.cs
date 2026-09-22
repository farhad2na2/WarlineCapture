using System;

namespace Game.Operations.Contracts
{
    /// <summary>
    /// Operations-owned identity grammar. Shared <c>Game.Configs.OperationMapIdentityRules</c>
    /// accepts the published <c>opmap.operations.&lt;slug&gt;</c> and <c>scenario.operations.o###</c>
    /// forms as well as the existing chapter and skirmish forms.
    /// </summary>
    public static class OperationsIdentityRules
    {
        public const int MaximumIdLength = 60;
        public const int MaximumEvidenceLength = 240;
        public const int CurrentSchemaVersion = 1;
        public const int MissionCount = 60;
        public const int DistrictCount = 6;

        public static bool IsValidCommonId(string value)
        {
            return TryParse(value, stackalloc IdSegment[8], out _);
        }

        public static bool IsValidMissionId(string value)
        {
            Span<IdSegment> segments = stackalloc IdSegment[2];
            return TryParse(value, segments, out int count) &&
                   count == 2 &&
                   IsEqual(value, segments[0], "operation") &&
                   IsNumbered(value, segments[1], 'o', 3, 1, MissionCount);
        }

        public static bool IsValidScenarioId(string value)
        {
            Span<IdSegment> segments = stackalloc IdSegment[3];
            return TryParse(value, segments, out int count) &&
                   count == 3 &&
                   IsEqual(value, segments[0], "scenario") &&
                   IsEqual(value, segments[1], "operations") &&
                   IsNumbered(value, segments[2], 'o', 3, 1, MissionCount);
        }

        public static bool IsValidOperationMapId(string value)
        {
            Span<IdSegment> segments = stackalloc IdSegment[3];
            return TryParse(value, segments, out int count) &&
                   count == 3 &&
                   IsEqual(value, segments[0], "opmap") &&
                   IsEqual(value, segments[1], "operations") &&
                   IsKnownMapSlug(value, segments[2]);
        }

        public static bool IsValidDistrictId(string value)
        {
            Span<IdSegment> segments = stackalloc IdSegment[3];
            return TryParse(value, segments, out int count) &&
                   count == 3 &&
                   IsEqual(value, segments[0], "district") &&
                   IsEqual(value, segments[1], "operations") &&
                   IsNumbered(value, segments[2], 'd', 2, 1, DistrictCount);
        }

        public static bool IsValidActionId(string value)
        {
            return TryMatchPrefixedSlug(value, "action", "operations", out IdSegment slug) &&
                   IsKnownActionSlug(value, slug);
        }

        public static bool IsValidAnchorId(string value)
        {
            Span<IdSegment> segments = stackalloc IdSegment[4];
            return TryParse(value, segments, out int count) &&
                   count == 4 &&
                   IsEqual(value, segments[0], "anchor") &&
                   IsEqual(value, segments[1], "operations") &&
                   IsNumbered(value, segments[2], 'd', 2, 1, DistrictCount);
        }

        public static bool IsValidRouteId(string value)
        {
            Span<IdSegment> segments = stackalloc IdSegment[4];
            if (!TryParse(value, segments, out int count) || count < 2)
                return false;
            if (!IsEqual(value, segments[0], "route"))
                return false;
            if (count == 2)
                return IsKnownRouteSlug(value, segments[1]);
            return count == 4 &&
                   IsEqual(value, segments[1], "operations") &&
                   IsNumbered(value, segments[2], 'd', 2, 1, DistrictCount);
        }

        public static bool IsValidRoleId(string value)
        {
            Span<IdSegment> segments = stackalloc IdSegment[3];
            return TryParse(value, segments, out int count) &&
                   count == 3 &&
                   IsEqual(value, segments[0], "role") &&
                   (IsEqual(value, segments[1], "friendly") ||
                    IsEqual(value, segments[1], "hostile") ||
                    IsEqual(value, segments[1], "civilian") ||
                    IsEqual(value, segments[1], "neutral"));
        }

        public static bool IsValidGeneratedId(string value, string requiredNamespace)
        {
            Span<IdSegment> segments = stackalloc IdSegment[3];
            return TryParse(value, segments, out int count) &&
                   count == 3 &&
                   IsEqual(value, segments[0], requiredNamespace) &&
                   IsEqual(value, segments[1], "operations") &&
                   segments[2].Length >= 8 &&
                   segments[2].Length <= 32;
        }

        public static bool IsValidFeatureId(string value)
        {
            Span<IdSegment> segments = stackalloc IdSegment[6];
            return TryParse(value, segments, out int count) &&
                   count >= 2 &&
                   IsEqual(value, segments[0], "feature");
        }

        public static bool IsValidContentId(string value) =>
            IsValidMissionId(value) ||
            IsValidScenarioId(value) ||
            IsValidOperationMapId(value) ||
            IsValidDistrictId(value) ||
            IsValidActionId(value) ||
            IsValidAnchorId(value) ||
            IsValidRouteId(value) ||
            IsValidRoleId(value) ||
            IsValidFeatureId(value);

        public static bool SharedValidatorCurrentlyAcceptsOperationsMapId => true;
        public static bool SharedValidatorCurrentlyAcceptsOperationsScenarioId => true;

        public static string MapIdForDistrict(int districtNumber) => districtNumber switch
        {
            1 => "opmap.operations.old_quarter",
            2 => "opmap.operations.civic_center",
            3 => "opmap.operations.industrial_belt",
            4 => "opmap.operations.river_crossing",
            5 => "opmap.operations.highland_approach",
            6 => "opmap.operations.airport_perimeter",
            _ => throw new ArgumentOutOfRangeException(nameof(districtNumber))
        };

        public static string DistrictId(int districtNumber)
        {
            if (districtNumber is < 1 or > DistrictCount)
                throw new ArgumentOutOfRangeException(nameof(districtNumber));
            return "district.operations.d" + districtNumber.ToString("00");
        }

        public static string MissionId(int missionNumber)
        {
            if (missionNumber is < 1 or > MissionCount)
                throw new ArgumentOutOfRangeException(nameof(missionNumber));
            return "operation.o" + missionNumber.ToString("000");
        }

        public static string ScenarioId(int missionNumber)
        {
            if (missionNumber is < 1 or > MissionCount)
                throw new ArgumentOutOfRangeException(nameof(missionNumber));
            return "scenario.operations.o" + missionNumber.ToString("000");
        }

        public static bool TryParseMissionNumber(string missionId, out int number)
        {
            number = 0;
            Span<IdSegment> segments = stackalloc IdSegment[2];
            if (!TryParse(missionId, segments, out int count) ||
                count != 2 ||
                !IsEqual(missionId, segments[0], "operation") ||
                !IsNumbered(missionId, segments[1], 'o', 3, 1, MissionCount))
                return false;
            number = ParseNumber(missionId, segments[1], 1);
            return true;
        }

        public static bool TryParseDistrictNumber(string districtId, out int number)
        {
            number = 0;
            Span<IdSegment> segments = stackalloc IdSegment[3];
            if (!TryParse(districtId, segments, out int count) ||
                count != 3 ||
                !IsEqual(districtId, segments[0], "district") ||
                !IsEqual(districtId, segments[1], "operations") ||
                !IsNumbered(districtId, segments[2], 'd', 2, 1, DistrictCount))
                return false;
            number = ParseNumber(districtId, segments[2], 1);
            return true;
        }

        private static bool TryMatchPrefixedSlug(
            string value, string first, string second, out IdSegment slug)
        {
            slug = default;
            Span<IdSegment> segments = stackalloc IdSegment[3];
            if (!TryParse(value, segments, out int count) || count != 3)
                return false;
            if (!IsEqual(value, segments[0], first) || !IsEqual(value, segments[1], second))
                return false;
            slug = segments[2];
            return true;
        }

        private static bool IsKnownMapSlug(string value, IdSegment segment) =>
            IsEqual(value, segment, "old_quarter") ||
            IsEqual(value, segment, "civic_center") ||
            IsEqual(value, segment, "industrial_belt") ||
            IsEqual(value, segment, "river_crossing") ||
            IsEqual(value, segment, "highland_approach") ||
            IsEqual(value, segment, "airport_perimeter");

        private static bool IsKnownActionSlug(string value, IdSegment segment) =>
            IsEqual(value, segment, "analyze") ||
            IsEqual(value, segment, "community") ||
            IsEqual(value, segment, "service") ||
            IsEqual(value, segment, "patrol") ||
            IsEqual(value, segment, "allocate") ||
            IsEqual(value, segment, "deescalate");

        private static bool IsKnownRouteSlug(string value, IdSegment segment) =>
            IsEqual(value, segment, "main") ||
            IsEqual(value, segment, "safe") ||
            IsEqual(value, segment, "flank") ||
            IsEqual(value, segment, "enemy_cargo");

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
                if (value[segment.Start + index] != expected[index])
                    return false;
            return true;
        }

        private static bool IsNumbered(
            string value, IdSegment segment, char prefix, int digits, int minimum, int maximum)
        {
            if (segment.Length != 1 + digits || value[segment.Start] != prefix)
                return false;
            int number = 0;
            for (int index = 1; index < segment.Length; index++)
            {
                char character = value[segment.Start + index];
                if (character is < '0' or > '9')
                    return false;
                number = (number * 10) + (character - '0');
            }

            return number >= minimum && number <= maximum;
        }

        private static int ParseNumber(string value, IdSegment segment, int prefixLength)
        {
            int number = 0;
            for (int index = prefixLength; index < segment.Length; index++)
                number = (number * 10) + (value[segment.Start + index] - '0');
            return number;
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

    internal static class OperationsContractText
    {
        public static void RequireId(string value, string parameterName, Func<string, bool> validator)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > OperationsIdentityRules.MaximumIdLength)
                throw new ArgumentException("A non-blank identity of at most 60 ASCII bytes is required.", parameterName);
            if (!validator(value))
                throw new ArgumentException("Identity does not match the Operations grammar.", parameterName);
        }

        public static void RequireToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > OperationsIdentityRules.MaximumIdLength)
                throw new ArgumentException("A non-blank token of at most 60 ASCII bytes is required.", parameterName);
        }

        public static void RequireEvidence(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > OperationsIdentityRules.MaximumEvidenceLength)
                throw new ArgumentException("A non-blank evidence string of at most 240 ASCII bytes is required.", parameterName);
        }

        public static void RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
