using System.Collections.Generic;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Rendering
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public sealed partial class DesertGroundDressingSystem : SystemBase
    {
        const int YardLimit = 6;
        const int Radius = 8;
        const float PatchSize = 13f;
        static readonly int Wind = Shader.PropertyToID("_WarlineWindSand");
        static readonly int Ready = Shader.PropertyToID("_WarlineDesertReady");
        static readonly int Yards = Shader.PropertyToID("_WarlineYards");
        static readonly int YardCount = Shader.PropertyToID("_WarlineYardCount");
        static readonly int Disabled = Shader.PropertyToID("_GroundVariationDisabled");
        readonly Vector4[] yards = new Vector4[YardLimit];
        readonly List<Vector4> candidates = new();
        readonly List<RectInt> buildings = new();
        EntityQuery gridQuery, surfaceQuery, basesQuery, structuresQuery, padsQuery, footprintQuery, mapQuery, metadataQuery;
        DesertDressingPalette palette;
        Material[] materials;
        Matrix4x4[][] instances;
        int[] counts;
        int yardCount;
        int2 lastPatch;
        double refreshTime;
        Entity lastSurface;
        FixedString64Bytes lastMap, lastSource;
        bool allowProps;

        protected override void OnCreate()
        {
            gridQuery = GetEntityQuery(ComponentType.ReadOnly<GridConfig>(), ComponentType.ReadOnly<GridWalkable>());
            surfaceQuery = GetEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            mapQuery = GetEntityQuery(ComponentType.ReadOnly<ActiveOperationMapComponent>());
            metadataQuery = GetEntityQuery(ComponentType.ReadOnly<OperationMapMetadataComponent>());
            basesQuery = GetEntityQuery(ComponentType.ReadOnly<SkirmishMainBase>());
            structuresQuery = GetEntityQuery(ComponentType.ReadOnly<SkirmishStructureIdentityComponent>());
            padsQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingFactionProductionSpawnPointReadModel>());
            footprintQuery = GetEntityQuery(ComponentType.ReadOnly<RuntimeBuildingCombatInfo>());
            RequireForUpdate(surfaceQuery);
        }

        protected override void OnStartRunning()
        {
            if (!palette)
            {
                palette = Resources.Load<DesertDressingPalette>("DesertDressing");
                if (!palette) return;
                int n = palette.pieces.Length;
                materials = new Material[n];
                instances = new Matrix4x4[n][];
                counts = new int[n];
                for (int i = 0; i < n; i++)
                {
                    var source = palette.pieces[i].material;
                    if (source)
                    {
                        materials[i] = new Material(source) { enableInstancing = true, hideFlags = HideFlags.DontSave };
                        if (source.HasProperty("_BaseColor"))
                            materials[i].SetColor("_BaseColor", source.GetColor("_BaseColor") * palette.pieces[i].tint);
                    }
                    instances[i] = new Matrix4x4[1023];
                }
            }
            Shader.SetGlobalTexture(Wind, palette.windSand);
            Shader.SetGlobalFloat(Ready, palette.windSand ? 1f : 0f);
            lastSurface = Entity.Null;
        }

        protected override void OnStopRunning()
        {
            Shader.SetGlobalFloat(Ready, 0);
            Shader.SetGlobalInteger(YardCount, 0);
        }

        protected override void OnDestroy()
        {
            if (materials != null)
                foreach (var material in materials)
                    if (material) Object.Destroy(material);
        }

        protected override void OnUpdate()
        {
            var camera = Camera.main;
            if (!palette || !camera || Shader.GetGlobalFloat(Disabled) > .5f ||
                surfaceQuery.CalculateEntityCount() != 1 || gridQuery.CalculateEntityCount() != 1) return;
            Entity surfaceEntity = surfaceQuery.GetSingletonEntity();
            Entity gridEntity = gridQuery.GetSingletonEntity();
            var surface = EntityManager.GetComponentData<MapSurfaceComponent>(surfaceEntity);
            if (!surface.SurfaceBlob.IsCreated || surface.HasSurfaceData == 0) return;

            var grid = EntityManager.GetComponentData<GridConfig>(gridEntity);
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            if (ray.direction.y >= -.015f) return;
            var center = ray.GetPoint(Mathf.Max(0, (grid.Origin.y - ray.origin.y) / ray.direction.y));
            int2 patch = (int2)math.floor(new float2(center.x, center.z) / PatchSize);
            FixedString64Bytes mapId = mapQuery.TryGetSingleton(out ActiveOperationMapComponent map) ? map.OperationMapId : default;
            double now = SystemAPI.Time.ElapsedTime;
            if (surfaceEntity != lastSurface || !mapId.Equals(lastMap) || math.any(patch != lastPatch) || now >= refreshTime)
            {
                FixedString64Bytes sourceId = default;
                if (metadataQuery.TryGetSingleton(out OperationMapMetadataComponent metadata) && metadata.Blob.IsCreated &&
                    metadata.Generation == map.Generation && metadata.Blob.Value.OperationMapId.Equals(mapId))
                    sourceId = metadata.Blob.Value.SourceOperationMapId;
                if (!mapId.Equals(lastMap) || !sourceId.Equals(lastSource) || surfaceEntity != lastSurface)
                {
                    // Scenario variants inherit the dressing of their authored physical source.
                    allowProps = System.Array.IndexOf(palette.scrubMapIds, mapId.ToString()) >= 0 ||
                        System.Array.IndexOf(palette.scrubMapIds, sourceId.ToString()) >= 0;
                    lastSource = sourceId;
                }
                lastSurface = surfaceEntity;
                lastMap = mapId;
                lastPatch = patch;
                refreshTime = now + 2;
                GatherYards(center, grid);
                System.Array.Clear(counts, 0, counts.Length);
                if (allowProps) PrepareInstances(gridEntity, grid, surface, patch);
            }

            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] == 0 || !materials[i]) continue;
                var mesh = palette.pieces[i].mesh;
                var render = new RenderParams(materials[i]) { camera = camera, receiveShadows = true,
                    shadowCastingMode = ShadowCastingMode.On, lightProbeUsage = LightProbeUsage.Off };
                if (SystemInfo.supportsInstancing)
                    Graphics.RenderMeshInstanced(render, mesh, 0, instances[i], counts[i]);
                else
                    for (int j = 0; j < counts[i]; j++) Graphics.RenderMesh(render, mesh, 0, instances[i][j]);
            }
        }

        void GatherYards(Vector3 center, GridConfig grid)
        {
            candidates.Clear();
            using (var roots = basesQuery.ToEntityArray(Allocator.Temp))
                foreach (var e in roots) AddBuildingYard(e, grid);
            using (var es = structuresQuery.ToEntityArray(Allocator.Temp))
                foreach (var e in es)
                {
                    var s = EntityManager.GetComponentData<SkirmishStructureIdentityComponent>(e);
                    if (s.DesignatedBase == 0 && s.Producer != SkirmishProducerKind.GroundStaging) continue;
                    AddBuildingYard(e, grid);
                }
            using (var es = padsQuery.ToEntityArray(Allocator.Temp))
                foreach (var e in es)
                    foreach (var pad in EntityManager.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(e, true))
                        AddYard(pad.WorldPosition, new float2(.26f, .966f));
            var point = new Vector2(center.x, center.z);
            // Select nearby anchors on large maps; overlapping buildings share the same yard.
            candidates.Sort((a, b) => ((Vector2)a - point).sqrMagnitude.CompareTo(((Vector2)b - point).sqrMagnitude));
            yardCount = Mathf.Min(YardLimit, candidates.Count);
            for (int i = 0; i < yardCount; i++) yards[i] = candidates[i];
            Shader.SetGlobalVectorArray(Yards, yards);
            Shader.SetGlobalInteger(YardCount, yardCount);
        }

        void AddBuildingYard(Entity e, GridConfig grid)
        {
            float2 heading = new float2(0, 1);
            bool located = EntityManager.HasComponent<LocalTransform>(e);
            LocalTransform root = located ? EntityManager.GetComponentData<LocalTransform>(e) : default;
            if (located) heading = root.Forward().xz;
            if (EntityManager.HasComponent<RuntimeBuildingCombatInfo>(e))
            {
                var info = EntityManager.GetComponentData<RuntimeBuildingCombatInfo>(e);
                if (math.all(info.FootprintCells > 0))
                {
                    var center = GridUtils.CellToWorldCenter(grid, info.OriginCell);
                    center.xz += new float2(info.FootprintCells - 1) * (grid.CellSize * .5f);
                    AddYard(center, heading);
                    return;
                }
            }
            if (located) AddYard(root.Position, heading);
        }

        void AddYard(float3 position, float2 heading)
        {
            if (!math.all(math.isfinite(position))) return;
            foreach (var yard in candidates)
                if (math.distancesq(position.xz, new float2(yard.x, yard.y)) < 18 * 18) return;
            heading = math.normalizesafe(heading, new float2(0, 1));
            candidates.Add(new Vector4(position.x, position.z, heading.x, heading.y));
        }

        void PrepareInstances(Entity gridEntity, GridConfig grid, MapSurfaceComponent surface, int2 patch)
        {
            buildings.Clear();
            using (var footprints = footprintQuery.ToComponentDataArray<RuntimeBuildingCombatInfo>(Allocator.Temp))
                foreach (var f in footprints)
                    buildings.Add(new RectInt(f.OriginCell.x-1, f.OriginCell.y-1, f.FootprintCells.x+2, f.FootprintCells.y+2));
            var open = EntityManager.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray();
            var roads = EntityManager.HasBuffer<GridRoad>(gridEntity)
                ? EntityManager.GetBuffer<GridRoad>(gridEntity, true).AsNativeArray() : default;
            for (int z = -Radius; z <= Radius; z++)
            for (int x = -Radius; x <= Radius; x++)
            {
                int2 key = patch + new int2(x, z);
                uint seed = math.hash(new int3(key, 709));
                var random = Unity.Mathematics.Random.CreateFromIndex(seed == uint.MaxValue ? 0 : seed);
                float2 origin = (new float2(key) + random.NextFloat2(.12f, .88f)) * PatchSize;
                bool nearYard = false;
                for (int i = 0; i < yardCount; i++)
                    nearYard |= math.distancesq(origin, new float2(yards[i].x, yards[i].y)) < 45 * 45;
                float density = noise.snoise(origin * .031f);
                if (random.NextFloat() > .55f + .21f * density) continue;
                float pick = random.NextFloat();
                const int bush = 0, slab = 1, rubble = 2, brick = 3, stone = 4, flake = 5, grass = 6;
                int main = nearYard && pick < .28f ? rubble : pick > .72f ? bush : pick > .48f ? grass : stone;
                for (int j = 0, n = random.NextInt(2, 5); j < n; j++)
                {
                    int piece = main;
                    if (j > 0)
                    {
                        float mix = random.NextFloat();
                        piece = main == rubble ? brick : mix < .28f ? grass : mix < .61f ? flake : slab;
                    }
                    float2 p = origin + random.NextFloat2(-2.4f, 2.4f);
                    float size = (j == 0 ? 1f : .48f) * random.NextFloat(.56f, 1.20f);
                    float angle = random.NextFloat(0, 360);
                    if (piece >= counts.Length || !palette.pieces[piece].mesh || counts[piece] >= instances[piece].Length) continue;
                    int2 cell = GridUtils.WorldToCell(grid, new float3(p.x, 0, p.y));
                    if (!GridUtils.InBounds(cell, grid.Width, grid.Height)) continue;
                    int index = GridUtils.CellToIndex(cell, grid.Width);
                    if (index >= open.Length || open[index].Value == 0 ||
                        (roads.IsCreated && index < roads.Length && roads[index].Value != 0)) continue;
                    bool inside = false;
                    foreach (var rect in buildings)
                        if (rect.Contains(new Vector2Int(cell.x, cell.y))) { inside = true; break; }
                    if (inside || !MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value, cell, out var land) ||
                        land.SurfaceType != MapSurfaceType.Terrain || land.Normal.y < .97f ||
                        (land.MovementMask & MapSurfaceMovementMask.Infantry) == 0 ||
                        (land.Flags & (MapSurfaceFlags.Road | MapSurfaceFlags.Bridge | MapSurfaceFlags.Layered |
                            MapSurfaceFlags.Ramp | MapSurfaceFlags.Reserved | MapSurfaceFlags.Highway)) != 0) continue;

                    var art = palette.pieces[piece];
                    var bounds = art.mesh.bounds;
                    float horizontal = art.width * size / Mathf.Max(.01f, Mathf.Max(bounds.size.x, bounds.size.z));
                    var scale = new Vector3(horizontal, art.height * size / Mathf.Max(.01f, bounds.size.y), horizontal);
                    var offset = p - GridUtils.CellToWorldCenter(grid, cell).xz;
                    float height = land.Height - math.dot(land.Normal.xz, offset) / land.Normal.y;
                    var turn = Quaternion.AngleAxis(angle, Vector3.up);
                    Vector3 pivot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    Vector3 pos = new Vector3(p.x, height - .035f * size, p.y) - turn * Vector3.Scale(pivot, scale);
                    instances[piece][counts[piece]++] = Matrix4x4.TRS(pos, turn, scale);
                }
            }
        }
    }
}
