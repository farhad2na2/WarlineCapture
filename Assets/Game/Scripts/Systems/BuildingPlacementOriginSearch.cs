using System;
using UnityEngine;

namespace Game.Runtime
{
    internal static class BuildingPlacementOriginSearch
    {
        // Opening a draggable preview must never synchronously search the whole operation map.
        // 32 cells cover nearby alternatives; a blocked area leaves a movable invalid preview.
        internal const int MaximumRadius = 32;

        internal static bool TryFind(RectInt origins, Vector2Int preferred,
            Func<Vector2Int, bool> isValid, out Vector2Int resolved)
        {
            resolved = preferred;
            if (origins.width <= 0 || origins.height <= 0 || isValid == null) return false;
            preferred = new Vector2Int(Mathf.Clamp(preferred.x, origins.xMin, origins.xMax - 1),
                Mathf.Clamp(preferred.y, origins.yMin, origins.yMax - 1));
            resolved = preferred;
            if (isValid(preferred)) return true;
            int radiusLimit = Mathf.Min(MaximumRadius, Mathf.Max(
                Mathf.Max(preferred.x - origins.xMin, origins.xMax - 1 - preferred.x),
                Mathf.Max(preferred.y - origins.yMin, origins.yMax - 1 - preferred.y)));
            for (int radius = 1; radius <= radiusLimit; radius++)
            {
                int left = preferred.x - radius, right = preferred.x + radius;
                int top = preferred.y - radius, bottom = preferred.y + radius;
                int minX = Mathf.Max(left, origins.xMin), maxX = Mathf.Min(right, origins.xMax - 1);
                if (top >= origins.yMin)
                    for (int x = minX; x <= maxX; x++)
                        if (Accept(new Vector2Int(x, top), isValid, out resolved)) return true;
                for (int y = Mathf.Max(top + 1, origins.yMin); y <= Mathf.Min(bottom - 1, origins.yMax - 1); y++)
                {
                    if (left >= origins.xMin && Accept(new Vector2Int(left, y), isValid, out resolved)) return true;
                    if (right < origins.xMax && Accept(new Vector2Int(right, y), isValid, out resolved)) return true;
                }
                if (bottom < origins.yMax)
                    for (int x = minX; x <= maxX; x++)
                        if (Accept(new Vector2Int(x, bottom), isValid, out resolved)) return true;
            }
            resolved = preferred;
            return false;
        }

        private static bool Accept(Vector2Int candidate, Func<Vector2Int, bool> isValid, out Vector2Int resolved)
        {
            resolved = candidate;
            return isValid(candidate);
        }
    }
}
