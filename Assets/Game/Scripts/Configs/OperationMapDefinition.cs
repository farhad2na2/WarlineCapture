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
        [SerializeField] private string contentHash;

        public string OperationMapId => operationMapId;
        public int SchemaVersion => schemaVersion;
        public int ContentVersion => contentVersion;
        public string ContentHash => contentHash;

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
}
