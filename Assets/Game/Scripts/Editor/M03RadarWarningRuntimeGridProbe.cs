using System;
using System.IO;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Editor
{
    public static class M03RadarWarningRuntimeGridProbe
    {
        public static void Capture(EntityManager em, string output)
        {
            em.CompleteAllTrackedJobs();
            using EntityQuery query = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(DynamicBlockerComponent));
            using EntityQuery surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            if (query.CalculateEntityCount() != 1 || surfaces.CalculateEntityCount() != 1)
                throw new InvalidOperationException("Runtime grid probe needs the authoritative grid and surface.");
            Entity entity = query.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(entity);
            var walkable = em.GetBuffer<GridWalkable>(entity, true);
            var blockers = em.GetComponentData<DynamicBlockerComponent>(entity);
            var surface = surfaces.GetSingleton<MapSurfaceComponent>();
            const int xMin = 540, zMin = 270, width = 560, height = 220;
            byte[] data = new byte[width * height];
            Color32[] colors = new Color32[data.Length];
            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                int2 cell = new(x + xMin, z + zMin);
                int index = cell.y * grid.Width + cell.x;
                bool terrain = MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value, cell, out var sample) &&
                               (sample.MovementMask & MapSurfaceMovementMask.WheeledVehicle) != 0;
                bool blocked = blockers.Blocked.IsSet(index);
                bool clear = terrain && walkable[index].Value != 0 && !blocked;
                data[z * width + x] = clear ? (byte)1 : (byte)0;
                colors[z * width + x] = clear ? new Color32(70,150,116,255) :
                    blocked ? new Color32(192,62,74,255) : new Color32(31,41,56,255);
            }
            File.WriteAllBytes(output + "/runtime_grid_540_270_560_220.bin", data);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(colors); texture.Apply();
            File.WriteAllBytes(output + "/runtime_grid.png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            ValidateConvoyRoute(em, data);
            ValidateSpawnedActors(em, data);
            using (var extras = new EntityQueryBuilder(Allocator.Temp).WithAll<UnitGrid, UnitMove, UnitHealth, Faction>()
                       .WithNone<CampaignMissionUnitRoleComponent, Prefab>().Build(em))
            using (var actors = extras.ToEntityArray(Allocator.Temp))
                foreach (var actor in actors)
                {
                    throw new InvalidOperationException($"Unexpected active actor outside M3's starting roster: {actor} source={(em.HasComponent<UnitSourcePrefabKey>(actor) ? em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString() : em.GetName(actor))}");
                }
            Debug.Log("[M03RuntimeGridProbe] wrote actual walkability, dynamic blockers and WheeledVehicle surface intersection");
        }

        private static void ValidateSpawnedActors(EntityManager em, byte[] grid)
        {
            using EntityQuery roots = em.CreateEntityQuery(typeof(CampaignMissionDefenseStateComponent), typeof(CampaignMissionDefenseMember));
            if (roots.CalculateEntityCount() != 1) throw new InvalidOperationException("Expected one live M3 roster for spawn clearance.");
            int checkedActors = 0;
            foreach (var member in em.GetBuffer<CampaignMissionDefenseMember>(roots.GetSingletonEntity(), true))
            {
                if (!em.Exists(member.Entity) || !em.HasComponent<UnitGrid>(member.Entity) || !em.HasComponent<UnitFootprint>(member.Entity))
                    throw new InvalidOperationException("A spawned M3 actor has no authoritative grid footprint.");
                int2 cell = em.GetComponentData<UnitGrid>(member.Entity).Cell;
                int2 size = math.max(1, em.GetComponentData<UnitFootprint>(member.Entity).Size);
                int2 min = UnitFootprintUtility.GetMinCell(cell, size);
                for (int z = 0; z < size.y; z++)
                for (int x = 0; x < size.x; x++)
                {
                    int px = min.x + x - 540, pz = min.y + z - 270;
                    if (px < 0 || px >= 560 || pz < 0 || pz >= 220 || grid[pz * 560 + px] == 0)
                        throw new InvalidOperationException($"M3 actor {member.Entity} faction={member.FactionId} starts blocked: cell={cell} footprint={size} blockedCell={min + new int2(x,z)}.");
                }
                checkedActors++;
            }
            if (checkedActors != 20) throw new InvalidOperationException($"Expected 20 clear spawned actors, found {checkedActors}.");
            Debug.Log($"[M03RuntimeGridProbe] spawnClearance=Passed actors={checkedActors} actualFootprints=true actualRuntimeBlockers=true");
        }

        private static void ValidateConvoyRoute(EntityManager em, byte[] grid)
        {
            using EntityQuery query = em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
            var metadata = query.GetSingleton<OperationMapMetadataComponent>();
            ref var map = ref metadata.Blob.Value;
            int2 previous = new(590,426);
            int segments = 0;
            for (int a = 0; a < map.Anchors.Length; a++)
            {
                ref var anchor = ref map.Anchors[a];
                if (!anchor.Id.ToString().StartsWith(M03RadarWarningMapBuilder.Prefix + "convoy_path_", StringComparison.Ordinal)) continue;
                int2 next = (int2)math.round(anchor.Position.xz);
                int steps = math.cmax(math.abs(next - previous));
                for (int i = 0; i <= steps; i++)
                {
                    int2 cell = (int2)math.round(math.lerp((float2)previous, (float2)next, steps == 0 ? 0 : (float)i / steps));
                    for (int z = -2; z <= 2; z++)
                    for (int x = -2; x <= 2; x++)
                    {
                        int px = cell.x + x - 540, pz = cell.y + z - 270;
                        if (px < 0 || px >= 560 || pz < 0 || pz >= 220 || grid[pz * 560 + px] == 0)
                            throw new InvalidOperationException($"Convoy route crosses a runtime blocker at {cell}, segment {segments}.");
                    }
                }
                previous = next; segments++;
            }
            if (segments < 2) throw new InvalidOperationException("No convoy route in loaded map.");
            Debug.Log($"[M03RuntimeGridProbe] result=Passed segments={segments} vehicleClearance=5 actualRuntimeBlockers=true");
        }
    }
}
