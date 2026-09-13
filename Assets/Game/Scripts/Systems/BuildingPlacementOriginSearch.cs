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

        // If the nearby lot is crowded, sample the authored mission zone without a map-wide scan.
        // At most 169 checks; select the closest valid sample to keep the entry camera move short.
        internal static bool TryFindAcrossBounds(RectInt origins, Vector2Int preferred,
            Func<Vector2Int, bool> isValid, out Vector2Int resolved)
        {
            resolved=preferred;
            if(origins.width<=0 || origins.height<=0 || isValid==null) return false;
            bool found=false; long nearest=long.MaxValue;
            int columns=Mathf.Min(12,origins.width-1), rows=Mathf.Min(12,origins.height-1);
            for(int y=0;y<=rows;y++)
                for(int x=0;x<=columns;x++)
                {
                    var candidate=new Vector2Int(origins.xMin+(columns==0?0:x*(origins.width-1)/columns),
                        origins.yMin+(rows==0?0:y*(origins.height-1)/rows));
                    long dx=candidate.x-preferred.x,dy=candidate.y-preferred.y,distance=dx*dx+dy*dy;
                    if(distance>=nearest || !isValid(candidate)) continue;
                    resolved=candidate;nearest=distance;found=true;
                }
            return found;
        }
    }
}
