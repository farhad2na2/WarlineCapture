using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    /// <summary>
    /// Collapses stale raised plateaus that sit on a one-cell cliff above grade.
    /// Dense-city visual grading archived interior hills, but the shared baked
    /// surface still carried those heights, so units and foundations floated.
    /// </summary>
    public static class MapSurfaceFloatingShelfCorrection
    {
        public const float HighThreshold = 4f;
        public const float GradeThreshold = 1.5f;
        public const float CliffDrop = 3f;
        public const float GradualMinDrop = 0.3f;
        public const float GradualMaxDrop = 1.5f;
        public const float MinimumMeanHeight = 4.5f;
        public const float MinimumCliffRatio = 0.05f;
        public const float MaximumGradualRatio = 0.22f;
        public const float RemnantMinHeight = 3f;
        public const float RemnantDrop = 2.5f;
        public const int MinimumComponentCells = 64;
        public const int MaximumComponentCells = 6000;

        private static readonly int2[] Cardinals =
        {
            new int2(1, 0),
            new int2(-1, 0),
            new int2(0, 1),
            new int2(0, -1)
        };

        public static int ApplyCompact(
            BlobBuilderArray<MapSurfaceCompactSample> samples,
            int width,
            int height,
            float minHeight,
            float heightStep)
        {
            int cellCount = width * height;
            if (cellCount <= 0 || samples.Length != cellCount || heightStep <= 0f)
                return 0;

            var heights = new float[cellCount];
            for (int i = 0; i < cellCount; i++)
                heights[i] = minHeight + samples[i].PackedHeight * heightStep;

            int flattened = Apply(heights, width, height);
            if (flattened == 0)
                return 0;

            for (int i = 0; i < cellCount; i++)
            {
                MapSurfaceCompactSample sample = samples[i];
                ushort packed = PackHeight(heights[i], minHeight, heightStep);
                if (packed == sample.PackedHeight)
                    continue;

                sample.PackedHeight = packed;
                sample.NormalX = 0;
                sample.NormalY = sbyte.MaxValue;
                sample.NormalZ = 0;
                samples[i] = sample;
            }

            return flattened;
        }

        public static int ApplySamples(
            BlobBuilderArray<MapSurfaceSample> samples,
            int width,
            int height)
        {
            int cellCount = width * height;
            if (cellCount <= 0 || samples.Length != cellCount)
                return 0;

            var heights = new float[cellCount];
            for (int i = 0; i < cellCount; i++)
                heights[i] = samples[i].Height;

            int flattened = Apply(heights, width, height);
            if (flattened == 0)
                return 0;

            var up = new float3(0f, 1f, 0f);
            for (int i = 0; i < cellCount; i++)
            {
                MapSurfaceSample sample = samples[i];
                if (sample.Height == heights[i])
                    continue;

                sample.Height = heights[i];
                sample.Normal = up;
                sample.SlopeDegrees = 0f;
                samples[i] = sample;
            }

            return flattened;
        }

        public static int Apply(float[] heights, int width, int height)
        {
            int cellCount = width * height;
            if (heights == null || heights.Length != cellCount || width <= 0 || height <= 0)
                return 0;

            var visited = new byte[cellCount];
            var stack = new List<int>(256);
            var component = new List<int>(256);
            var gradeHeights = new List<float>(64);
            int flattened = 0;

            for (int origin = 0; origin < cellCount; origin++)
            {
                if (visited[origin] != 0 || heights[origin] < HighThreshold)
                    continue;

                stack.Clear();
                component.Clear();
                stack.Add(origin);
                visited[origin] = 1;

                while (stack.Count > 0)
                {
                    int index = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                    component.Add(index);

                    int x = index % width;
                    int y = index / width;
                    for (int dir = 0; dir < Cardinals.Length; dir++)
                    {
                        int nx = x + Cardinals[dir].x;
                        int ny = y + Cardinals[dir].y;
                        if ((uint)nx >= (uint)width || (uint)ny >= (uint)height)
                            continue;

                        int neighbor = nx + ny * width;
                        if (visited[neighbor] != 0 || heights[neighbor] < HighThreshold)
                            continue;

                        visited[neighbor] = 1;
                        stack.Add(neighbor);
                    }
                }

                if (!IsUnsupportedShelf(heights, width, height, component, gradeHeights, out float targetHeight))
                    continue;

                for (int i = 0; i < component.Count; i++)
                {
                    heights[component[i]] = targetHeight;
                    flattened++;
                }

                flattened += AbsorbUnsupportedRim(
                    heights,
                    visited,
                    width,
                    height,
                    component,
                    targetHeight);
            }

            return flattened;
        }

        private static bool IsUnsupportedShelf(
            float[] heights,
            int width,
            int height,
            List<int> component,
            List<float> gradeHeights,
            out float targetHeight)
        {
            targetHeight = 0f;
            int count = component.Count;
            if (count < MinimumComponentCells || count > MaximumComponentCells)
                return false;

            float heightSum = 0f;
            int cliffCells = 0;
            int gradualCells = 0;
            gradeHeights.Clear();

            for (int i = 0; i < count; i++)
            {
                int index = component[i];
                float cellHeight = heights[index];
                heightSum += cellHeight;

                int x = index % width;
                int y = index / width;
                float maxDrop = 0f;
                bool cliffsOntoGrade = false;
                for (int dir = 0; dir < Cardinals.Length; dir++)
                {
                    int nx = x + Cardinals[dir].x;
                    int ny = y + Cardinals[dir].y;
                    if ((uint)nx >= (uint)width || (uint)ny >= (uint)height)
                        continue;

                    float neighborHeight = heights[nx + ny * width];
                    float drop = cellHeight - neighborHeight;
                    if (drop > maxDrop)
                        maxDrop = drop;
                    if (neighborHeight < GradeThreshold && drop >= CliffDrop)
                    {
                        cliffsOntoGrade = true;
                        gradeHeights.Add(neighborHeight);
                    }
                }

                if (cliffsOntoGrade)
                    cliffCells++;
                else if (maxDrop >= GradualMinDrop && maxDrop <= GradualMaxDrop)
                    gradualCells++;
            }

            if (heightSum / count < MinimumMeanHeight ||
                gradeHeights.Count == 0 ||
                cliffCells / (float)count < MinimumCliffRatio ||
                gradualCells / (float)count > MaximumGradualRatio)
            {
                return false;
            }

            gradeHeights.Sort();
            targetHeight = gradeHeights[gradeHeights.Count / 2];
            return true;
        }

        private static int AbsorbUnsupportedRim(
            float[] heights,
            byte[] visited,
            int width,
            int height,
            List<int> seed,
            float targetHeight)
        {
            int absorbed = 0;
            int head = 0;
            while (head < seed.Count)
            {
                int index = seed[head++];
                int x = index % width;
                int y = index / width;
                for (int dir = 0; dir < Cardinals.Length; dir++)
                {
                    int nx = x + Cardinals[dir].x;
                    int ny = y + Cardinals[dir].y;
                    if ((uint)nx >= (uint)width || (uint)ny >= (uint)height)
                        continue;

                    int neighbor = nx + ny * width;
                    float neighborHeight = heights[neighbor];
                    if (neighborHeight < RemnantMinHeight ||
                        neighborHeight - targetHeight < RemnantDrop)
                        continue;

                    heights[neighbor] = targetHeight;
                    visited[neighbor] = 1;
                    seed.Add(neighbor);
                    absorbed++;
                }
            }

            return absorbed;
        }

        private static ushort PackHeight(float height, float minHeight, float heightStep)
        {
            float normalized = (height - minHeight) / heightStep;
            int packed = (int)math.round(normalized);
            if (packed < 0)
                return 0;
            if (packed > ushort.MaxValue)
                return ushort.MaxValue;
            return (ushort)packed;
        }
    }
}
