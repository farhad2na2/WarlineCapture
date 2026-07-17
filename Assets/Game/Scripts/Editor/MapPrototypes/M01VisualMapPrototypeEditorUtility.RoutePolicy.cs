namespace Game.Editor
{
#if UNITY_EDITOR
    using UnityEngine;

    public static partial class M01VisualMapPrototypeEditorUtility
    {
        public static int GetLocalRoadPolicyViolationCount()
        {
            return CountLocalRoadPolicyViolations(false);
        }

        private static int CountLocalRoadPolicyViolations(bool logDetails)
        {
            int violationCount = 0;
            float totalLength = 0f;
            if (LocalRoadSegments.Length == 0 || LocalRoadSegments.Length > MaximumLocalRoadSegmentCount)
            {
                violationCount++;
                LogLocalRoadPolicyViolation(
                    logDetails,
                    $"reason=segmentCount actual={LocalRoadSegments.Length} maximum={MaximumLocalRoadSegmentCount}");
            }

            for (int segmentIndex = 0; segmentIndex < LocalRoadSegments.Length; segmentIndex++)
            {
                LocalRoadSegmentDefinition segment = LocalRoadSegments[segmentIndex];
                float width = segment.Dusty ? LocalRoadWidth : 4.2f;
                float length = Vector3.Distance(segment.Start, segment.End);
                totalLength += length;

                if (width > LocalRoadWidth + 0.001f ||
                    length < MinimumLocalRoadLength ||
                    length > MaximumLocalRoadLength)
                {
                    violationCount++;
                    LogLocalRoadPolicyViolation(
                        logDetails,
                        $"reason=segmentEnvelope road={segment.Name} width={width:0.00} length={length:0.00} " +
                        $"allowedLength={MinimumLocalRoadLength:0.00}-{MaximumLocalRoadLength:0.00} " +
                        $"maximumWidth={LocalRoadWidth:0.00}");
                }

                if (segmentIndex > 0 &&
                    Vector3.Distance(LocalRoadSegments[segmentIndex - 1].End, segment.Start) > LocalRoadEndpointTolerance)
                {
                    violationCount++;
                    LogLocalRoadPolicyViolation(
                        logDetails,
                        $"reason=disconnected previous={LocalRoadSegments[segmentIndex - 1].Name} road={segment.Name}");
                }
            }

            if (totalLength > MaximumLocalRoadTotalLength)
            {
                violationCount++;
                LogLocalRoadPolicyViolation(
                    logDetails,
                    $"reason=totalLength actual={totalLength:0.00} maximum={MaximumLocalRoadTotalLength:0.00}");
            }

            for (int firstIndex = 0; firstIndex < LocalRoadSegments.Length; firstIndex++)
            {
                for (int secondIndex = firstIndex + 2; secondIndex < LocalRoadSegments.Length; secondIndex++)
                {
                    LocalRoadSegmentDefinition first = LocalRoadSegments[firstIndex];
                    LocalRoadSegmentDefinition second = LocalRoadSegments[secondIndex];
                    if (!LineSegmentsIntersectXZ(first.Start, first.End, second.Start, second.End))
                        continue;

                    violationCount++;
                    LogLocalRoadPolicyViolation(
                        logDetails,
                        $"reason=selfIntersection first={first.Name} second={second.Name}");
                }
            }

            if (logDetails)
            {
                Debug.Log(
                    $"[M01LocalRoadPolicy] result={(violationCount == 0 ? "Passed" : "Failed")} " +
                    $"segments={LocalRoadSegments.Length} maximumWidth={LocalRoadWidth:0.00} " +
                    $"maximumSegmentLength={MaximumLocalRoadLength:0.00} totalLength={totalLength:0.00} " +
                    $"maximumTotalLength={MaximumLocalRoadTotalLength:0.00} topology=simpleChain violations={violationCount}");
            }

            return violationCount;
        }

        private static void LogLocalRoadPolicyViolation(bool enabled, string message)
        {
            if (enabled)
                Debug.Log($"[M01LocalRoadPolicyViolation] {message}");
        }

        private static bool LineSegmentsIntersectXZ(Vector3 firstStart, Vector3 firstEnd, Vector3 secondStart, Vector3 secondEnd)
        {
            Vector2 a = new(firstStart.x, firstStart.z);
            Vector2 b = new(firstEnd.x, firstEnd.z);
            Vector2 c = new(secondStart.x, secondStart.z);
            Vector2 d = new(secondEnd.x, secondEnd.z);
            float abC = Cross2D(b - a, c - a);
            float abD = Cross2D(b - a, d - a);
            float cdA = Cross2D(d - c, a - c);
            float cdB = Cross2D(d - c, b - c);
            const float epsilon = 0.0001f;

            if (((abC > epsilon && abD < -epsilon) || (abC < -epsilon && abD > epsilon)) &&
                ((cdA > epsilon && cdB < -epsilon) || (cdA < -epsilon && cdB > epsilon)))
            {
                return true;
            }

            return Mathf.Abs(abC) <= epsilon && PointLiesOnSegment(c, a, b, epsilon) ||
                   Mathf.Abs(abD) <= epsilon && PointLiesOnSegment(d, a, b, epsilon) ||
                   Mathf.Abs(cdA) <= epsilon && PointLiesOnSegment(a, c, d, epsilon) ||
                   Mathf.Abs(cdB) <= epsilon && PointLiesOnSegment(b, c, d, epsilon);
        }

        private static float Cross2D(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }

        private static bool PointLiesOnSegment(Vector2 point, Vector2 start, Vector2 end, float epsilon)
        {
            return point.x >= Mathf.Min(start.x, end.x) - epsilon &&
                   point.x <= Mathf.Max(start.x, end.x) + epsilon &&
                   point.y >= Mathf.Min(start.y, end.y) - epsilon &&
                   point.y <= Mathf.Max(start.y, end.y) + epsilon;
        }
    }
#endif
}
