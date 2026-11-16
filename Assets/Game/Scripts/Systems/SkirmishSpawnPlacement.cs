using Game.Components;
using Game.Runtime.Pathfinding;
using Game.Runtime.Spawning;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class SkirmishSpawnPlacement
    {
        private struct SurfaceAdmission : ISpawnSurfaceAdmission
        {
            public MapSurfaceComponent Surface;
            public GridConfig Grid;
            public bool Vehicle;
            public bool CanTraverseFootprint(int x, int z, int sizeX, int sizeZ)
                => new MapSurfaceTraversalValidation().CanTraverseFootprint(Surface, 1, Grid,
                    new int2(x, z), new int2(sizeX, sizeZ), Vehicle);
        }
        internal static bool EnsureTraversable(EntityManager em, in GridConfig grid,
            NativeArray<GridWalkable> walkable, NativeBitArray blocked, NativeBitArray occupied,
            ref NativeBitArray reserved, Entity prefab, int2 size, ref int2 cell)
        {
            using var match = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (match.IsEmptyIgnoreFilter) return true;
            using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            int count = surfaces.CalculateEntityCount();
            // Preserve lazy prefab reads: no singleton means no component access.
            if (count != 1) return false;
            var surface = surfaces.GetSingleton<MapSurfaceComponent>();
            bool vehicle = em.HasComponent<UnitMovementBehavior>(prefab) && em.GetComponentData<UnitMovementBehavior>(prefab).UsesVehicleMotion != 0;
            var admission = new SurfaceAdmission { Surface = surface, Grid = grid, Vehicle = vehicle };
            var storage = new SpawnCellUtility.NativeStorage { Walkable = walkable, Blocked = blocked, Occupied = occupied, Reserved = reserved };
            int x = cell.x, z = cell.y;
            bool result = SpawnCellSearchKernel.EnsureTraversable(ref storage, ref admission,
                grid.Width, grid.Height, size.x, size.y, true, count, ref x, ref z);
            reserved = storage.Reserved; cell = new int2(x, z); return result;
        }
    }
}
