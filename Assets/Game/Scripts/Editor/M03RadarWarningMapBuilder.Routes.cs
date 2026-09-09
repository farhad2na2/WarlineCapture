using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningMapBuilder
    {
        // Surveyed against the loaded physical map's GridWalkable and DynamicBlocker data.
        // The eastern road reaches the post without crossing the authored barracks footprints.
        // The Play Mode grid probe revalidates every segment, so a changed physical map fails QA.
        private static int2[] AuthoredConvoyStops() => new int2[]
        {
            new(590,426), new(622,426), new(654,426), new(673,427), new(705,427),
            new(737,427), new(769,427), new(801,427), new(830,426), new(862,426),
            new(876,426), new(908,426), new(940,426), new(972,426), new(1004,426),
            new(1005,395), new(1004,367), new(1003,366), new(998,346), new(985,346),
            new(954,342), new(942,342)
        };

        private static bool Clear(ref MapSurfaceBlob surface, int2 cell, int halfWidth)
        {
            float minimum = float.MaxValue, maximum = float.MinValue;
            for (int z = -halfWidth; z <= halfWidth; z++)
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                int2 candidate = cell + new int2(x,z);
                if (!Window.Contains(new Vector2Int(candidate.x,candidate.y)) ||
                    !MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface,candidate,out MapSurfaceSample sample) ||
                    (sample.MovementMask & MapSurfaceMovementMask.WheeledVehicle) == 0) return false;
                minimum = math.min(minimum,sample.Height); maximum = math.max(maximum,sample.Height);
            }
            return maximum-minimum <= 1.25f;
        }

        private static int2 Nearest(ref MapSurfaceBlob surface, int2 seed, int halfWidth)
        {
            for (int radius = 0; radius <= 24; radius++)
            for (int z = -radius; z <= radius; z++)
            for (int x = -radius; x <= radius; x++)
            {
                if (math.max(math.abs(x),math.abs(z)) != radius) continue;
                int2 cell = seed + new int2(x,z);
                if (Clear(ref surface,cell,halfWidth)) return cell;
            }
            throw new InvalidOperationException($"No clear {halfWidth*2+1}-cell footprint near {seed}.");
        }

        private static List<int2> FindRoute(ref MapSurfaceBlob surface, params int2[] stops)
        {
            bool[] walkable = new bool[Window.width * Window.height];
            for (int z = 0; z < Window.height; z++)
            for (int x = 0; x < Window.width; x++)
                walkable[z*Window.width+x] = Clear(ref surface,new int2(x+Window.xMin,z+Window.yMin),2);
            var result = new List<int2>();
            for (int leg = 1; leg < stops.Length; leg++)
            {
                List<int2> full = BreadthFirst(walkable,stops[leg-1],stops[leg]);
                int cursor = 0;
                while (cursor < full.Count-1)
                {
                    int next = cursor+1;
                    while (next+1 < full.Count && math.distance((float2)full[cursor],(float2)full[next+1]) <= 32 &&
                           HasLine(walkable,full[cursor],full[next+1])) next++;
                    if (result.Count == 0 || !result[result.Count-1].Equals(full[next])) result.Add(full[next]);
                    cursor = next;
                }
            }
            Require(result.Count >= 2, "Convoy route needs multiple validated waypoints.");
            return result;
        }

        private static List<int2> BreadthFirst(bool[] walkable, int2 start, int2 goal)
        {
            int[] parents = new int[walkable.Length]; Array.Fill(parents,-1);
            var pending = new Queue<int>();
            int source = Index(start), target = Index(goal);
            Require(walkable[source] && walkable[target], "Route endpoint is blocked.");
            parents[source] = source; pending.Enqueue(source);
            int2[] directions = { new(1,0), new(0,-1), new(-1,0), new(0,1) };
            while (pending.Count > 0 && parents[target] < 0)
            {
                int current = pending.Dequeue(); int2 cell = Cell(current);
                foreach (int2 direction in directions)
                {
                    int2 nextCell = cell+direction;
                    if (!Window.Contains(new Vector2Int(nextCell.x,nextCell.y))) continue;
                    int next = Index(nextCell);
                    if (!walkable[next] || parents[next] >= 0) continue;
                    parents[next] = current; pending.Enqueue(next);
                }
            }
            Require(parents[target] >= 0, $"No vehicle-safe route {start} -> {goal}.");
            var route = new List<int2>();
            for (int current = target; current != source; current = parents[current]) route.Add(Cell(current));
            route.Add(start); route.Reverse(); return route;
        }

        private static bool HasLine(bool[] walkable, int2 a, int2 b)
        {
            int steps = math.max(math.abs(b.x-a.x),math.abs(b.y-a.y));
            for (int i = 0; i <= steps; i++)
            {
                int2 cell = (int2)math.round(math.lerp((float2)a,(float2)b,steps == 0 ? 0 : (float)i/steps));
                if (!walkable[Index(cell)]) return false;
            }
            return true;
        }
        private static int Index(int2 cell) => (cell.y-Window.yMin)*Window.width + cell.x-Window.xMin;
        private static int2 Cell(int index) => new(index%Window.width+Window.xMin,index/Window.width+Window.yMin);
    }
}
