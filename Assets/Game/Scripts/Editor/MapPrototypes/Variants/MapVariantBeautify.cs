using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Configs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor.MapVariants
{
    // Presentation-only pass for prepared candidates. It runs after gameplay classification, so nothing
    // it adds enters the placement inventory, semantic hash, movement grid or walkable surface.
    // Flat paint may sit anywhere; anything taller than paint is limited to cells no unit can occupy
    // in any campaign mission bound to this map (static blockers, unreachable pockets, outside bounds).
    internal static class MapVariantBeautify
    {
        internal const string Version = "map-beautify-v8";
        private const string MissionConfigRoot = "Assets/Game/Configs/OperationMaps";
        private const string AtlasProperty = "_BaseMap";
        private const int GridWidth = 2048, GridHeight = 1024;
        private const int MaxPaintVertices = 60000;
        private const float SlabSize = 4f;
        private const float MaxLowPropHeight = 0.9f;

        internal static bool Supports(string map) => map is "RefineryDistrict" or "CityEdgeAirfield" or "AshLinePort" or "Frontier";

        [Serializable]
        internal sealed class Report
        {
            public string version = Version;
            public int missionDefinitions, clearanceCircles, reachableCells, deadPocketCells;
            public int paintMeshes, paintTriangles, slabs, sandySlabs, lines, stains, patches, tracks;
            public int drifts, props, propRejectedNearUnits, vegetation, edgeDressing, cracks, vergeClusters, vignettes;
            public float maxLowPropHeight;
            public string missionUnion;
        }

        private sealed class Gameplay
        {
            public Vector3 Offset;
            public bool[] Blocked, Reachable;
            public Rect Union;
            public readonly List<(Vector2 center, float radius)> Clearance = new();

            public bool InGrid(int x, int z) => x >= 0 && z >= 0 && x < GridWidth && z < GridHeight;

            // Source rect is safe for render-only geometry that units would visibly pass through.
            public bool TallAllowed(Rect source)
            {
                foreach (var (c, r) in Clearance)
                {
                    float dx = Mathf.Max(source.xMin - c.x, 0f, c.x - source.xMax);
                    float dz = Mathf.Max(source.yMin - c.y, 0f, c.y - source.yMax);
                    if (dx * dx + dz * dz < r * r) return false;
                }
                Rect union = MapVariantBuilder.Inset(Union, -4f);
                int x0 = Mathf.FloorToInt(source.xMin + Offset.x), x1 = Mathf.CeilToInt(source.xMax + Offset.x);
                int z0 = Mathf.FloorToInt(source.yMin + Offset.z), z1 = Mathf.CeilToInt(source.yMax + Offset.z);
                for (int z = z0; z < z1; z++)
                for (int x = x0; x < x1; x++)
                {
                    if (!union.Contains(new Vector2(x + .5f - Offset.x, z + .5f - Offset.z))) continue;
                    if (!InGrid(x, z)) return false;
                    int i = z * GridWidth + x;
                    if (Reachable[i] && !Blocked[i]) return false;
                }
                return true;
            }

            public bool NearUnits(Vector2 source, float radius)
            {
                foreach (var (c, r) in Clearance)
                    if ((source - c).sqrMagnitude < (r + radius) * (r + radius)) return true;
                return false;
            }
        }

        public static Report Apply(MapVariantBuilder b, string preparedMapId, bool[] staticMovement, Vector3 offset)
        {
            var report = new Report();
            Gameplay g = ResolveGameplay(b, preparedMapId, staticMovement, offset, report);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MapVariantBuilder.GroundMaterialPath) ??
                           throw new InvalidOperationException("[MapBeautify] Missing ground material.");
            var palette = new AtlasPalette(material.GetTexture(AtlasProperty) as Texture2D, MapVariantBuilder.ResolveGroundUv());
            var groundDetail = new Material(material) { name = "DetailedDesertGround" };
            string groundPath = "Assets/Game/Art/MapBeautify/DetailedDesertGround.mat";
            var oldGround = AssetDatabase.LoadAssetAtPath<Material>(groundPath);
            if (oldGround == null) AssetDatabase.CreateAsset(groundDetail, groundPath);
            else { UnityEngine.Object.DestroyImmediate(groundDetail); groundDetail = oldGround; }
            ConfigureGroundDetail(groundDetail, .8f);
            foreach (var renderer in b.Layer(MapVariantLayer.Ground).GetComponentsInChildren<MeshRenderer>())
                if (renderer.name.StartsWith("Ground_")) renderer.sharedMaterial = groundDetail;
            var paint = new PaintSink(b.Layer(MapVariantLayer.Ground), material, report);
            string concretePath = "Assets/Game/Art/MapBeautify/" + (b.MapId == "AshLinePort" ? "PortAsphalt" : "Concrete") + ".mat";
            var concreteMaterial = AssetDatabase.LoadAssetAtPath<Material>(concretePath);
            if (concreteMaterial == null)
            {
                System.IO.Directory.CreateDirectory("Assets/Game/Art/MapBeautify");
                concreteMaterial = new Material(material);
                AssetDatabase.CreateAsset(concreteMaterial, concretePath);
            }
            concreteMaterial.shader = Shader.Find("Universal Render Pipeline/Lit");
            const string texturePath = "Assets/Game/Art/MapBeautify/WeatheredConcreteV5.asset";
            var concreteTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (concreteTexture == null)
            {
                concreteTexture = new Texture2D(512, 512, TextureFormat.RGB24, true) { name = "WeatheredConcrete", wrapMode = TextureWrapMode.Repeat };
                var pixels = new Color[512 * 512];
                for (int y = 0; y < 512; y++)
                for (int x = 0; x < 512; x++)
                {
                    float broad = Mathf.PerlinNoise(x / 93f, y / 93f);
                    float grain = Mathf.PerlinNoise(x / 3.2f, y / 3.2f);
                    float wear = Mathf.PerlinNoise(x / 22f + 51f, y / 22f + 23f);
                    float value = .38f + broad * .065f + grain * .012f + wear * .012f;
                    pixels[y * 512 + x] = new Color(value * .97f, value, value * 1.015f);
                }
                concreteTexture.SetPixels(pixels); concreteTexture.Apply(true, false);
                AssetDatabase.CreateAsset(concreteTexture, texturePath);
            }
            concreteMaterial.SetTexture("_BaseMap", concreteTexture);
            concreteMaterial.SetTexture("_BumpMap", null);
            concreteMaterial.DisableKeyword("_NORMALMAP");
            concreteMaterial.SetColor("_BaseColor", b.MapId == "AshLinePort" ? new Color(.64f, .66f, .68f, 1f) : new Color(.85f,.82f,.75f,1f));
            concreteMaterial.SetFloat("_Smoothness", .12f);
            EditorUtility.SetDirty(concreteMaterial);
            var sandPaint = new PaintSink(b.Layer(MapVariantLayer.Ground), groundDetail, report, "SandWear");
            var concretePaint = new PaintSink(b.Layer(MapVariantLayer.Ground), concreteMaterial, report, "Concrete");
            var random = new System.Random(b.Seed ^ 0x5eed);
            float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

            var pads = b.Placements.Where(p => p.Surface && p.Instance != null).ToList();
            var padRects = pads.Select(p => new Rect(p.Center - p.HalfSize, p.HalfSize * 2f)).ToList();
            bool OnPad(Vector2 p, float margin) => padRects.Any(r => MapVariantBuilder.Inset(r, -margin).Contains(p));
            var solid = new StructureMask(b);

            PaintPads(b, pads, palette, paint, concretePaint, Range, report);
            PaintWearAndVerge(b, pads, palette, paint, sandPaint, Range, report);
            // World-projected macro/detail textures supply the sand variation without stamped polygon islands.
            PaintTracks(b, palette, sandPaint, Range, OnPad, solid, report);
            paint.Flush();
            concretePaint.Flush();
            sandPaint.Flush();

            bool previous = b.RecordPlacements;
            b.RecordPlacements = false;
            try
            {
                SandDrifts(b, g, OnPad, report);
                EdgeDressing(b, padRects, OnPad, g, report);
                DeadSpaceProps(b, g, solid, Range, report);
                Groves(b, g, Range, OnPad, report);
                DetailClusters(b, g, pads, Range, report);
            }
            finally { b.RecordPlacements = previous; }
            Debug.Log("[MapBeautify] result=Applied map=" + b.MapId + " " + JsonUtility.ToJson(report));
            return report;
        }

        internal static void ConfigureGroundDetail(Material material, float strength)
        {
            material.shader = Shader.Find("Game/Environment/GroundMacroVariation");
            material.SetTexture("_DesertDetailMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Rendering/Textures/T_GroundDetail_Desert_Albedo.png"));
            material.SetTexture("_GreenDetailMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Rendering/Textures/T_GroundDetail_Desert_Albedo.png"));
            material.SetTexture("_DesertDetailNormal", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Rendering/Textures/T_GroundDetail_Desert_Normal.png"));
            material.SetFloat("_DetailFadeStart", 180f); material.SetFloat("_DetailFadeEnd", 650f);
            material.SetFloat("_GroundDetailTiling", .13f); material.SetFloat("_GroundDetailStrength", strength);
            material.SetFloat("_GroundDetailNormalStrength", .65f); material.SetFloat("_DetailStrength", .12f);
            material.SetFloat("_MacroScale", .045f); material.SetFloat("_MacroStrength", .8f);
            material.SetFloat("_MacroContrastLow", .1f); material.SetFloat("_MacroContrastHigh", .8f);
            material.SetColor("_MacroTintA", new Color(.82f,.73f,.57f)); material.SetColor("_MacroTintB", new Color(1.06f,1.02f,.9f));
            material.SetFloat("_Smoothness", .04f); EditorUtility.SetDirty(material);
        }

        private static Gameplay ResolveGameplay(MapVariantBuilder b, string preparedMapId, bool[] staticMovement, Vector3 offset, Report report)
        {
            var g = new Gameplay { Offset = offset, Blocked = staticMovement, Reachable = new bool[GridWidth * GridHeight] };
            var seeds = new List<Vector2Int>();
            bool any = false;
            Rect union = default;
            foreach (string guid in AssetDatabase.FindAssets("t:OperationMapDefinition", new[] { MissionConfigRoot }))
            {
                var definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition == null || definition.SourceBinding.SourceOperationMapId != preparedMapId) continue;
                report.missionDefinitions++;
                Vector3 min = definition.Bounds.PlayableMin - offset, max = definition.Bounds.PlayableMax - offset;
                Rect playable = Rect.MinMaxRect(min.x, min.z, max.x, max.z);
                union = any ? Rect.MinMaxRect(Mathf.Min(union.xMin, playable.xMin), Mathf.Min(union.yMin, playable.yMin),
                    Mathf.Max(union.xMax, playable.xMax), Mathf.Max(union.yMax, playable.yMax)) : playable;
                any = true;
                foreach (OperationMapAnchorConfig anchor in definition.Anchors)
                {
                    if (anchor.Kind is OperationMapAnchorKind.None) continue;
                    Vector2 p = new(anchor.Position.x - offset.x, anchor.Position.z - offset.z);
                    float clearance = anchor.Kind switch
                    {
                        OperationMapAnchorKind.Objective or OperationMapAnchorKind.Build or OperationMapAnchorKind.Base or
                            OperationMapAnchorKind.Resource => 18f,
                        OperationMapAnchorKind.Spawn or OperationMapAnchorKind.Deployment or OperationMapAnchorKind.Hostile or
                            OperationMapAnchorKind.Civilian => 9f,
                        _ => 5f
                    };
                    g.Clearance.Add((p, Mathf.Max(anchor.Radius, 0f) + clearance));
                    seeds.Add(new Vector2Int(Mathf.FloorToInt(anchor.Position.x), Mathf.FloorToInt(anchor.Position.z)));
                }
                if (definition.AdditionalBuildingPlacements != null)
                    foreach (var placement in definition.AdditionalBuildingPlacements.Placements)
                        g.Clearance.Add((new Vector2(placement.WorldCenter.x - offset.x, placement.WorldCenter.z - offset.z), 20f));
            }
            // Without bound missions the whole prepared playable area is treated as gameplay space.
            g.Union = any ? union : b.Playable;
            report.missionUnion = $"{g.Union.xMin},{g.Union.yMin},{g.Union.xMax},{g.Union.yMax}";
            report.clearanceCircles = g.Clearance.Count;
            seeds.Add(new Vector2Int(Mathf.FloorToInt(b.Playable.center.x + offset.x), Mathf.FloorToInt(b.Playable.center.y + offset.z)));
            foreach (var cell in b.RoadCells.Keys)
            {
                Vector2 c = b.RoadCellCenter(cell);
                if (b.Playable.Contains(c)) seeds.Add(new Vector2Int(Mathf.FloorToInt(c.x + offset.x), Mathf.FloorToInt(c.y + offset.z)));
            }

            // Conservative reachability over every unblocked cell of the prepared playable area.
            Rect runtimePlayable = new(b.Playable.position + new Vector2(offset.x, offset.z), b.Playable.size);
            var queue = new Queue<int>();
            foreach (var s in seeds)
            {
                if (!g.InGrid(s.x, s.y)) continue;
                int i = s.y * GridWidth + s.x;
                if (staticMovement[i] || g.Reachable[i]) continue;
                g.Reachable[i] = true; queue.Enqueue(i);
            }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue(); int x = i % GridWidth, z = i / GridWidth;
                foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int nx = x + dx, nz = z + dz;
                    if (!g.InGrid(nx, nz) || !runtimePlayable.Contains(new Vector2(nx + .5f, nz + .5f))) continue;
                    int n = nz * GridWidth + nx;
                    if (g.Reachable[n] || staticMovement[n]) continue;
                    g.Reachable[n] = true; queue.Enqueue(n);
                }
            }
            for (int z = 0; z < GridHeight; z++)
            for (int x = 0; x < GridWidth; x++)
            {
                if (!runtimePlayable.Contains(new Vector2(x + .5f, z + .5f))) continue;
                int i = z * GridWidth + x;
                if (g.Reachable[i]) report.reachableCells++;
                else if (!staticMovement[i]) report.deadPocketCells++;
            }
            return g;
        }

        // ------------------------------------------------------------------ paint

        private static void PaintPads(MapVariantBuilder b, List<MapVariantPlacement> pads, AtlasPalette palette, PaintSink paint, PaintSink concretePaint,
            Func<float, float, float> range, Report report)
        {
            Vector2 sand = palette.GroundUv;
            Vector2 patched = palette.Nearest(new Color(0.47f, 0.46f, 0.44f));
            Vector2 oil = palette.Nearest(new Color(0.34f, 0.32f, 0.28f));
            Vector2 yellow = palette.Nearest(new Color(0.88f, 0.70f, 0.22f));
            Vector2 white = palette.Nearest(new Color(0.88f, 0.87f, 0.82f));
            var concrete = Rect.MinMaxRect(0f, 0f, 1f, 1f);
            const float window = .65f;

            foreach (MapVariantPlacement pad in pads)
            {
                bool paved = pad.PrefabPath == MapVariantKits.ConcretePad || pad.PrefabPath == MapVariantKits.AsphaltPad;
                if (!paved || Mathf.Abs(pad.Yaw) > 0.01f) continue;
                float top = pad.Instance.GetComponent<Renderer>().bounds.max.y;
                Rect area = new(pad.Center - pad.HalfSize, pad.HalfSize * 2f);
                // A neutral undercoat prevents the original brown pad showing through the joints.
                concretePaint.TexturedQuad(area, top + .008f, new Rect(0f, 0f, 1f, 1f), 0);
                int nx = Mathf.Max(1, Mathf.RoundToInt(area.width / SlabSize)), nz = Mathf.Max(1, Mathf.RoundToInt(area.height / SlabSize));
                float sx = area.width / nx, sz = area.height / nz;
                for (int z = 0; z < nz; z++)
                for (int x = 0; x < nx; x++)
                {
                    Rect slab = Rect.MinMaxRect(area.xMin + x * sx + .025f, area.yMin + z * sz + .025f, area.xMin + (x + 1) * sx - .025f, area.yMin + (z + 1) * sz - .025f);
                    float u = range(concrete.xMin, concrete.xMax - window), v = range(concrete.yMin, concrete.yMax - window);
                    concretePaint.TexturedQuad(slab, top + .012f, new Rect(u, v, window, window), random90: (int)range(0f, 3.999f));
                    report.slabs++;
                }

                // Painted yard boundary, inset from the fence line.
                Rect line = MapVariantBuilder.Inset(area, 2.2f);
                if (line.width > 6f && line.height > 6f)
                {
                    paint.Strip(new Vector2(line.xMin, line.yMin), new Vector2(line.xMax, line.yMin), 0.28f, top + .03f, yellow);
                    paint.Strip(new Vector2(line.xMax, line.yMin), new Vector2(line.xMax, line.yMax), 0.28f, top + .03f, yellow);
                    paint.Strip(new Vector2(line.xMax, line.yMax), new Vector2(line.xMin, line.yMax), 0.28f, top + .03f, yellow);
                    paint.Strip(new Vector2(line.xMin, line.yMax), new Vector2(line.xMin, line.yMin), 0.28f, top + .03f, yellow);
                    report.lines += 4;
                }

                // Parking bays and oil drips under every vehicle parked on this pad.
                foreach (MapVariantPlacement vehicle in b.Placements.Where(p => p.Layer == MapVariantLayer.Vehicles && area.Contains(p.Center)))
                {
                    bool alongZ = vehicle.HalfSize.y >= vehicle.HalfSize.x;
                    float halfLength = alongZ ? vehicle.HalfSize.y : vehicle.HalfSize.x, halfWidth = alongZ ? vehicle.HalfSize.x : vehicle.HalfSize.y;
                    Quaternion yaw = Quaternion.Euler(0f, vehicle.Yaw, 0f);
                    Vector3 f3 = yaw * (alongZ ? Vector3.forward : Vector3.right), s3 = yaw * (alongZ ? Vector3.right : Vector3.back);
                    Vector2 forward = new(f3.x, f3.z), side = new(s3.x, s3.z);
                    foreach (float sign in new[] { -1f, 1f })
                    {
                        Vector2 c = vehicle.Center + side * sign * (halfWidth + .55f);
                        paint.Strip(c - forward * (halfLength + .6f), c + forward * (halfLength + .6f), 0.16f, top + .03f, white);
                        report.lines++;
                    }
                    if (range(0f, 1f) < 0.75f)
                    {
                        paint.Blob(b, vehicle.Center + forward * range(-halfLength * .6f, halfLength * .6f), range(.35f, .8f), top + .022f, oil, range, flat: true);
                        report.stains++;
                    }
                }
                int stains = Mathf.RoundToInt(area.width * area.height / 850f);
                for (int i = 0; i < stains; i++)
                {
                    Vector2 c = new(range(area.xMin + 3f, area.xMax - 3f), range(area.yMin + 3f, area.yMax - 3f));
                    paint.Blob(b, c, range(.3f, .9f), top + .02f, oil, range, flat: true);
                    report.stains++;
                }
            }
        }

        // Flat erosion and wear follow each compound boundary; no gameplay geometry is added.
        private static void PaintWearAndVerge(MapVariantBuilder b, List<MapVariantPlacement> pads, AtlasPalette palette,
            PaintSink paint, PaintSink sandPaint, Func<float, float, float> range, Report report)
        {
            Vector2 sand = palette.GroundUv;
            Vector2 dust = palette.Nearest(new Color(.49f, .43f, .32f));
            Vector2 crack = palette.Nearest(new Color(.29f, .28f, .26f));
            Vector2 yellow = palette.Nearest(new Color(.88f, .7f, .22f));
            foreach (var pad in pads.Where(p => p.PrefabPath == MapVariantKits.ConcretePad || p.PrefabPath == MapVariantKits.AsphaltPad))
            {
                Rect r = new(pad.Center - pad.HalfSize, pad.HalfSize * 2f);
                float y = pad.Instance.GetComponent<Renderer>().bounds.max.y;
                for (float t = 0; t < 2f * (r.width + r.height); t += range(3f, 7f))
                {
                    Vector2 c = PointOnPerimeter(r, t);
                    float radius = range(1.2f, 3.8f);
                    sandPaint.Blob(b, c, radius, y + .045f, sand, range, true);
                    report.patches++;
                }
                for (int i = 0; i < r.width * r.height / 260f; i++)
                {
                    Vector2 p = new(range(r.xMin + 2f, r.xMax - 2f), range(r.yMin + 2f, r.yMax - 2f));
                    float angle = range(0, Mathf.PI * 2f);
                    for (int j = 0; j < 5; j++)
                    {
                        angle += range(-.55f, .55f);
                        Vector2 next = p + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * range(.6f, 1.8f);
                        if (!r.Contains(next)) break;
                        paint.Strip(p, next, range(.04f, .09f), y + .026f, crack);
                        if (j == 2) paint.Strip(p, p + new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * range(.8f, 1.8f), .04f, y + .026f, crack);
                        p = next;
                    }
                    report.cracks++;
                }
                // Dashed service lanes break the broad paved spaces into readable work areas.
                if (r.width > 50f && r.height > 35f)
                    for (float x = r.xMin + 8f; x < r.xMax - 8f; x += 6f)
                    {
                        Vector2 a = new(x, r.yMin + 7f);
                        paint.Strip(a, a + Vector2.right * 3f, .16f, y + .03f, yellow);
                        report.lines++;
                    }
            }
            if (b.MapId == "CityEdgeAirfield")
            {
                foreach (var pad in b.Placements.Where(p => p.PrefabPath == MapVariantCityAirfield.Helipad && p.Instance != null))
                {
                    float y = pad.Instance.GetComponentsInChildren<Renderer>().Max(r => r.bounds.max.y) + .035f;
                    for (int i = 0; i < 32; i++)
                    {
                        float a = i * Mathf.PI / 16f, next = (i + 1) * Mathf.PI / 16f;
                        paint.Strip(pad.Center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 12f,
                            pad.Center + new Vector2(Mathf.Cos(next), Mathf.Sin(next)) * 12f, .24f, y, yellow);
                    }
                    report.lines += 32;
                }
            }
            if (b.MapId == "Frontier" || b.MapId == "AshLinePort")
            foreach (var cargo in b.Placements.Where(p => p.PrefabPath.Contains("Container") && p.Instance != null))
            {
                var slab=pads.FirstOrDefault(p => new Rect(p.Center-p.HalfSize,p.HalfSize*2f).Contains(cargo.Center));
                if(slab?.Instance == null) continue;
                float y=slab.Instance.GetComponent<Renderer>().bounds.max.y+.032f;
                Vector2 h=cargo.HalfSize+Vector2.one*.65f, c=cargo.Center;
                var corners=new[]{c+new Vector2(-h.x,-h.y),c+new Vector2(h.x,-h.y),c+h,c+new Vector2(-h.x,h.y)};
                for(int i=0;i<4;i++) paint.Strip(corners[i],corners[(i+1)%4],.12f,y,yellow);
                report.lines+=4;
            }
        }

        // Dense, low ground cover is safe along verges; tall cargo only fits verified dead space.
        private static void DetailClusters(MapVariantBuilder b, Gameplay g, List<MapVariantPlacement> pads,
            Func<float, float, float> range, Report report)
        {
            foreach (var pad in pads.Where(p => p.PrefabPath == MapVariantKits.ConcretePad || p.PrefabPath == MapVariantKits.AsphaltPad))
            {
                Rect r = new(pad.Center - pad.HalfSize, pad.HalfSize * 2f);
                for (float t = range(0f, 5f); t < 2f * (r.width + r.height); t += range(7f, 13f))
                {
                    Vector2 c = PointOnPerimeter(r, t);
                    if (g.NearUnits(c, 3f) || b.DistanceToRoad(c) < 3f) continue;
                    int placed = 0;
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 p = c + new Vector2(range(-2.3f, 2.3f), range(-2.3f, 2.3f));
                        if (!b.World.Contains(p) || g.NearUnits(p, .8f) || b.DistanceToRoad(p) < 2f) continue;
                        string prefab = b.Pick(i % 4 == 0 ? MapVariantKits.Pebbles : MapVariantKits.GrassClumps);
                        float scale = Mathf.Min(range(.85f, 1.45f), .72f / Mathf.Max(.1f, b.Info(prefab).LocalBounds.size.y));
                        var options = PlaceOptions.Vegetation.WithScale(scale).WithSink(.04f).WithPadding(0f);
                        options.IgnoreOccupancy = true; options.Reserve = false;
                        if (b.Place(prefab, MapVariantLayer.Vegetation, p, range(0, 360f), options) != null) { placed++; report.vegetation++; }
                    }
                    if (placed > 0) report.vergeClusters++;
                    // A small pallet, broken crate and rubble make a single maintenance vignette.
                    foreach (string prefab in new[] { MapVariantKits.YardCargo[1], MapVariantKits.YardCargo[2], DeadSpaceKit[6] })
                    {
                        Vector2 p = c + new Vector2(range(-2f, 2f), range(-2f, 2f));
                        float height = b.Info(prefab).LocalBounds.size.y;
                        if (height > MaxLowPropHeight) continue;
                        if (g.NearUnits(p, 1f) || b.DistanceToRoad(p) < 3f) continue;
                        if (b.Place(prefab, MapVariantLayer.Props, p, range(0, 360f), PlaceOptions.Prop.WithPadding(.08f)) != null)
                        { report.props++; report.vignettes++; }
                    }
                }
            }
        }

        private static void PaintGroundPatches(MapVariantBuilder b, AtlasPalette palette, PaintSink paint, Func<float, float, float> range,
            Func<Vector2, float, bool> onPad, StructureMask solid, Report report)
        {
            Color baseColor = palette.Sample(palette.GroundUv);
            (Vector2 outer, Vector2 inner)[] tones =
            {
                (palette.Nearest(baseColor * 0.93f), palette.Nearest(baseColor * 0.85f)),
                (palette.Nearest(Color.Lerp(baseColor, Color.white, 0.10f)), palette.Nearest(Color.Lerp(baseColor, Color.white, 0.2f))),
                (palette.Nearest(baseColor * 0.93f), palette.Nearest(new Color(baseColor.r * 1.0f, baseColor.g * 0.83f, baseColor.b * 0.7f)))
            };
            Rect area = MapVariantBuilder.Inset(b.Playable, -160f);
            int count = Mathf.RoundToInt(area.width * area.height / 650f);
            for (int i = 0; i < count; i++)
            {
                Vector2 c = new(range(area.xMin, area.xMax), range(area.yMin, area.yMax));
                float r = range(4f, 13f);
                if (!b.World.Contains(c) || onPad(c, r + 1f) || b.DistanceToRoad(c) < r + 3f || solid.Contains(c)) continue;
                b.Height.SampleRange(new Rect(c - Vector2.one * r, Vector2.one * r * 2f), out float min, out float max);
                if (max - min > 2.5f) continue;
                float pick = range(0f, 1f);
                var tone = tones[pick < 0.45f ? 0 : pick < 0.88f ? 1 : 2];
                paint.Blob(b, c, r, .05f, tone.outer, range, flat: false);
                paint.Blob(b, c + new Vector2(range(-r, r), range(-r, r)) * .2f, r * range(.35f, .6f), .075f, tone.inner, range, flat: false);
                report.patches++;
            }
        }

        private static void PaintTracks(MapVariantBuilder b, AtlasPalette palette, PaintSink paint, Func<float, float, float> range,
            Func<Vector2, float, bool> onPad, StructureMask solid, Report report)
        {
            Color baseColor = palette.Sample(palette.GroundUv);
            Vector2 body = palette.Nearest(Color.Lerp(baseColor, Color.white, 0.13f));
            Vector2 rut = palette.Nearest(baseColor * 0.8f);
            Rect area = MapVariantBuilder.Inset(b.Playable, -120f);
            var roads = b.RoadCells.Keys.Where(c => area.Contains(b.RoadCellCenter(c))).OrderBy(c => c.x).ThenBy(c => c.y).ToList();
            int count = Mathf.RoundToInt(area.width * area.height / 9000f);
            for (int t = 0; t < count && roads.Count > 0; t++)
            {
                Vector2Int cell = roads[(int)range(0f, roads.Count - 0.001f)];
                bool alongX = b.RoadCells.ContainsKey(cell + Vector2Int.right) || b.RoadCells.ContainsKey(cell + Vector2Int.left);
                Vector2 dir = alongX ? (range(0f, 1f) < .5f ? Vector2.up : Vector2.down) : (range(0f, 1f) < .5f ? Vector2.right : Vector2.left);
                Vector2 p = b.RoadCellCenter(cell) + dir * (MapVariantBuilder.RoadGridSize * .5f + 1.5f) +
                            new Vector2(dir.y, dir.x) * range(-4f, 4f);
                float heading = Mathf.Atan2(dir.y, dir.x) + range(-.45f, .45f), turn = 0f;
                float length = range(35f, 170f);
                var points = new List<Vector2>();
                for (float d = 0f; d < length; d += 2f)
                {
                    if (!area.Contains(p) || onPad(p, 2.5f) || solid.Contains(p) || (d > 6f && b.DistanceToRoad(p) < 2f)) break;
                    points.Add(p);
                    turn = Mathf.Clamp(turn + range(-.05f, .05f), -.08f, .08f);
                    heading += turn;
                    p += new Vector2(Mathf.Cos(heading), Mathf.Sin(heading)) * 2f;
                }
                if (points.Count < 10) continue;
                paint.Ribbon(b, points, 3.1f, 0f, .045f, body);
                paint.Ribbon(b, points, .45f, -.8f, .06f, rut);
                paint.Ribbon(b, points, .45f, .8f, .06f, rut);
                report.tracks++;
            }
        }

        // ------------------------------------------------------------------ objects

        private static void SandDrifts(MapVariantBuilder b, Gameplay g, Func<Vector2, float, bool> onPad, Report report)
        {
            var fences = b.Placements.Where(p => p.PrefabPath == MapVariantKits.Fence && p.Instance != null).ToList();
            int index = 0;
            foreach (MapVariantPlacement fence in fences)
            {
                Vector3 n3 = Quaternion.Euler(0f, fence.Yaw, 0f) * (fence.HalfSize.x >= fence.HalfSize.y ? Vector3.forward : Vector3.right);
                Vector2 normal = new(n3.x, n3.z);
                foreach (float sign in new[] { -1f, 1f })
                {
                    if (index++ % 5 > 2) continue;
                    Vector2 q = fence.Center + normal * sign * 1.6f;
                    if (onPad(q, 0.5f) || b.DistanceToRoad(q) < 4f || g.NearUnits(q, 2f)) continue;
                    if (!b.IsFree(new Rect(q - Vector2.one * .4f, Vector2.one * .8f))) continue;
                    string prefab = b.Pick(MapVariantKits.SandEdges);
                    float scale = b.Range(.7f, 1.05f);
                    float height = b.Info(prefab).LocalBounds.size.y * scale;
                    if (height > MaxLowPropHeight) continue;
                    var options = PlaceOptions.Vegetation.WithScale(scale).WithGroundDelta(.5f);
                    options.IgnoreOccupancy = true; options.Reserve = false;
                    if (b.Place(prefab, MapVariantLayer.Vegetation, q, fence.Yaw + (sign > 0 ? 0f : 180f), options) == null) continue;
                    report.maxLowPropHeight = Mathf.Max(report.maxLowPropHeight, height);
                    report.drifts++;
                }
            }
        }

        // Grass tufts and pebbles gather along pad edges where wind and run-off collect them.
        private static void EdgeDressing(MapVariantBuilder b, List<Rect> pads, Func<Vector2, float, bool> onPad, Gameplay g, Report report)
        {
            foreach (Rect pad in pads)
            {
                Rect ring = MapVariantBuilder.Inset(pad, -2.2f);
                float perimeter = 2f * (ring.width + ring.height);
                for (float t = 0f; t < perimeter; t += b.Range(.8f, 1.7f))
                {
                    Vector2 p = PointOnPerimeter(ring, t) + new Vector2(b.Range(-.8f, .8f), b.Range(-.8f, .8f));
                    if (onPad(p, 0.3f) || b.DistanceToRoad(p) < 3f || g.NearUnits(p, 1f)) continue;
                    bool grass = b.Chance(.7f);
                    string prefab = b.Pick(grass ? MapVariantKits.GrassClumps : MapVariantKits.Pebbles);
                    var options = PlaceOptions.Vegetation.WithPadding(.05f).WithSink(.05f).WithScale(b.Range(.8f, 1.5f));
                    options.Scale = Mathf.Min(options.Scale, .72f / Mathf.Max(.1f, b.Info(prefab).LocalBounds.size.y));
                    options.IgnoreOccupancy = true; options.Reserve = false;
                    if (b.Place(prefab, MapVariantLayer.Vegetation, p, b.Range(0f, 360f), options) != null) report.edgeDressing++;
                }
            }
        }

        private static readonly string[] DeadSpaceKit =
        {
            MapVariantKits.YardCargo[0], MapVariantKits.YardCargo[1], MapVariantKits.YardCargo[2], MapVariantKits.YardCargo[3],
            MapVariantKits.YardCargo[4], MapVariantKits.YardCargo[6],
            "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_DebrisPile_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_DebrisPile_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Rubble_Pile_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Rubble_Pile_03.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Block_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Vehicle_Debris_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Vehicle_Debris_05.prefab"
        };

        // Story clutter: crates, barrels, debris and wreck parts packed into space units can never enter.
        private static void DeadSpaceProps(MapVariantBuilder b, Gameplay g, StructureMask solid, Func<float, float, float> range, Report report)
        {
            Rect area = MapVariantBuilder.Inset(b.Playable, -40f);
            for (int attempt = 0; attempt < 14000; attempt++)
            {
                Vector2 seed = new(range(area.xMin, area.xMax), range(area.yMin, area.yMax));
                if (!solid.Near(seed) || !TryProp(b, g, seed, range, report)) continue;
                int cluster = (int)range(1f, 5.99f);
                for (int i = 0; i < cluster * 3; i++)
                    TryProp(b, g, seed + new Vector2(range(-3f, 3f), range(-3f, 3f)), range, report);
            }
        }

        private static bool TryProp(MapVariantBuilder b, Gameplay g, Vector2 p, Func<float, float, float> range, Report report)
        {
            string prefab = DeadSpaceKit[(int)range(0f, DeadSpaceKit.Length - .001f)];
            float yaw = (int)range(0f, 3.999f) * 90f + range(-12f, 12f);
            Vector2 half = b.Footprint(prefab) * .5f;
            Rect rect = MapVariantBuilder.FootprintAabb(p, half, yaw, .1f);
            if (!b.World.Contains(rect.min) || !b.World.Contains(rect.max) || b.DistanceToRoad(p) < 3f) return false;
            if (!g.TallAllowed(rect)) { report.propRejectedNearUnits++; return false; }
            if (b.Place(prefab, MapVariantLayer.Props, p, yaw, PlaceOptions.Prop.WithPadding(.1f)) == null) return false;
            report.props++;
            return true;
        }

        // Palm groves with grass beneath them, only where no mission unit can stand.
        private static void Groves(MapVariantBuilder b, Gameplay g, Func<float, float, float> range, Func<Vector2, float, bool> onPad, Report report)
        {
            Rect area = MapVariantBuilder.Inset(b.Playable, -140f);
            int groves = 0;
            for (int attempt = 0; attempt < 900 && groves < 45; attempt++)
            {
                Vector2 c = new(range(area.xMin, area.xMax), range(area.yMin, area.yMax));
                Rect core = new(c - Vector2.one * 7f, Vector2.one * 14f);
                if (!b.World.Contains(core.min) || !b.World.Contains(core.max) || onPad(c, 8f) || b.DistanceToRoad(c) < 10f || !g.TallAllowed(core)) continue;
                int placed = 0;
                int palms = (int)range(2f, 5.99f);
                for (int i = 0; i < palms * 3 && placed < palms; i++)
                {
                    Vector2 p = c + new Vector2(range(-6f, 6f), range(-6f, 6f));
                    if (b.Place(b.Pick(MapVariantKits.Palms), MapVariantLayer.Vegetation, p, range(0f, 360f),
                            PlaceOptions.Vegetation.WithScale(range(.85f, 1.2f))) != null) placed++;
                }
                if (placed == 0) continue;
                for (int i = 0; i < 18; i++)
                {
                    Vector2 p = c + new Vector2(range(-8f, 8f), range(-8f, 8f));
                    string[] kit = i % 3 == 0 ? MapVariantKits.Shrubs : MapVariantKits.GrassClumps;
                    if (b.Place(b.Pick(kit), MapVariantLayer.Vegetation, p, range(0f, 360f),
                            PlaceOptions.Vegetation.WithPadding(.05f).WithScale(range(.8f, 1.4f))) != null) report.vegetation++;
                }
                report.vegetation += placed;
                groves++;
            }
        }

        private static Vector2 PointOnPerimeter(Rect r, float t)
        {
            t = Mathf.Repeat(t, 2f * (r.width + r.height));
            if (t < r.width) return new Vector2(r.xMin + t, r.yMin);
            t -= r.width;
            if (t < r.height) return new Vector2(r.xMax, r.yMin + t);
            t -= r.height;
            if (t < r.width) return new Vector2(r.xMax - t, r.yMax);
            t -= r.width;
            return new Vector2(r.xMin, r.yMax - t);
        }

        // ------------------------------------------------------------------ helpers

        // 4 m cells covered by structures, vehicles, fences and props; dressing layers are ignored.
        private sealed class StructureMask
        {
            private const float Cell = 4f;
            private readonly HashSet<Vector2Int> _cells = new();

            public StructureMask(MapVariantBuilder b)
            {
                foreach (MapVariantPlacement p in b.Placements)
                {
                    // Gameplay owners have already replaced their source instance; their footprint still counts.
                    if (p.Surface || p.Backdrop || p.Layer is MapVariantLayer.Vegetation or MapVariantLayer.Ground or
                            MapVariantLayer.Roads or MapVariantLayer.Backdrop or MapVariantLayer.Zones) continue;
                    Rect r = MapVariantBuilder.FootprintAabb(p.Center, p.HalfSize, p.Yaw, 0f);
                    for (int z = Mathf.FloorToInt(r.yMin / Cell); z <= Mathf.FloorToInt(r.yMax / Cell); z++)
                    for (int x = Mathf.FloorToInt(r.xMin / Cell); x <= Mathf.FloorToInt(r.xMax / Cell); x++)
                        _cells.Add(new Vector2Int(x, z));
                }
            }

            private static Vector2Int Key(Vector2 p) => new(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell));

            public bool Contains(Vector2 p) => _cells.Contains(Key(p));

            public bool Near(Vector2 p)
            {
                Vector2Int k = Key(p);
                for (int z = -1; z <= 1; z++)
                for (int x = -1; x <= 1; x++)
                    if (_cells.Contains(k + new Vector2Int(x, z))) return true;
                return false;
            }
        }

        // Reads the shared atlas once. Flat paint uses one texel per polygon, so derivatives are zero and
        // the sample never bleeds; candidates still require a uniform neighbourhood for distant mips.
        private sealed class AtlasPalette
        {
            private const int Size = 512;
            private readonly Color[] _pixels;
            private readonly List<int> _flat = new();
            public readonly Vector2 GroundUv;

            public AtlasPalette(Texture2D atlas, Vector2 groundUv)
            {
                if (atlas == null) throw new InvalidOperationException("[MapBeautify] Ground material has no atlas.");
                GroundUv = groundUv;
                var target = RenderTexture.GetTemporary(Size, Size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                RenderTexture previous = RenderTexture.active;
                var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                try
                {
                    Graphics.Blit(atlas, target);
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                    texture.Apply();
                    _pixels = texture.GetPixels();
                }
                finally
                {
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(target);
                    UnityEngine.Object.DestroyImmediate(texture);
                }
                for (int y = 4; y < Size - 4; y += 2)
                for (int x = 4; x < Size - 4; x += 2)
                    if (Uniform(x, y)) _flat.Add(y * Size + x);
                if (_flat.Count == 0) throw new InvalidOperationException("[MapBeautify] Atlas has no flat palette cells.");
            }

            private bool Uniform(int cx, int cy)
            {
                Color c = _pixels[cy * Size + cx];
                for (int y = cy - 3; y <= cy + 3; y++)
                for (int x = cx - 3; x <= cx + 3; x++)
                {
                    Color o = _pixels[y * Size + x];
                    if (Mathf.Abs(o.r - c.r) + Mathf.Abs(o.g - c.g) + Mathf.Abs(o.b - c.b) > 0.03f) return false;
                }
                return true;
            }

            public Color Sample(Vector2 uv) =>
                _pixels[Mathf.Clamp((int)(uv.y * Size), 0, Size - 1) * Size + Mathf.Clamp((int)(uv.x * Size), 0, Size - 1)];

            public Vector2 Nearest(Color target)
            {
                int best = _flat[0]; float bestDistance = float.MaxValue;
                foreach (int i in _flat)
                {
                    Color c = _pixels[i];
                    float d = (c.r - target.r) * (c.r - target.r) + (c.g - target.g) * (c.g - target.g) + (c.b - target.b) * (c.b - target.b);
                    if (d < bestDistance) { bestDistance = d; best = i; }
                }
                return new Vector2((best % Size + .5f) / Size, (best / Size + .5f) / Size);
            }
        }

        // Accumulates paint geometry and emits it as ground-layer meshes that share the ground material.
        private sealed class PaintSink
        {
            private readonly Transform _parent;
            private readonly Material _material;
            private readonly Report _report;
            private readonly List<Vector3> _vertices = new();
            private readonly List<Vector3> _normals = new();
            private readonly List<Vector2> _uvs = new();
            private readonly List<int> _triangles = new();
            private int _index;
            private readonly string _name;

            public PaintSink(Transform parent, Material material, Report report, string name = "Paint") { _parent = parent; _material = material; _report = report; _name = name; }

            private int Vertex(Vector3 p, Vector3 n, Vector2 uv) { _vertices.Add(p); _normals.Add(n); _uvs.Add(uv); return _vertices.Count - 1; }

            private void Reserve(int vertices) { if (_vertices.Count + vertices > MaxPaintVertices) Flush(); }

            public void Quad(Rect r, float y, Vector2 uv) => TexturedQuad(r, y, new Rect(uv, Vector2.zero), 0);

            public void TexturedQuad(Rect r, float y, Rect uv, int random90)
            {
                Reserve(4);
                Vector2[] corners = { new(uv.xMin, uv.yMin), new(uv.xMin, uv.yMax), new(uv.xMax, uv.yMax), new(uv.xMax, uv.yMin) };
                int a = Vertex(new Vector3(r.xMin, y, r.yMin), Vector3.up, corners[random90 % 4]);
                int bb = Vertex(new Vector3(r.xMin, y, r.yMax), Vector3.up, corners[(random90 + 1) % 4]);
                int c = Vertex(new Vector3(r.xMax, y, r.yMax), Vector3.up, corners[(random90 + 2) % 4]);
                int d = Vertex(new Vector3(r.xMax, y, r.yMin), Vector3.up, corners[(random90 + 3) % 4]);
                _triangles.AddRange(new[] { a, bb, c, a, c, d });
            }

            public void Strip(Vector2 from, Vector2 to, float width, float y, Vector2 uv)
            {
                Reserve(4);
                Vector2 dir = (to - from).normalized, side = new Vector2(-dir.y, dir.x) * width * .5f;
                int a = Vertex(new Vector3(from.x - side.x, y, from.y - side.y), Vector3.up, uv);
                int bb = Vertex(new Vector3(from.x + side.x, y, from.y + side.y), Vector3.up, uv);
                int c = Vertex(new Vector3(to.x + side.x, y, to.y + side.y), Vector3.up, uv);
                int d = Vertex(new Vector3(to.x - side.x, y, to.y - side.y), Vector3.up, uv);
                _triangles.AddRange(new[] { a, bb, c, a, c, d });
            }

            // Irregular disc. Terrain blobs follow the height field with rings every ~3 m; flat blobs sit at y.
            public void Blob(MapVariantBuilder b, Vector2 c, float radius, float lift, Vector2 uv, Func<float, float, float> range, bool flat)
            {
                const int segments = 14;
                int rings = flat ? 1 : Mathf.Max(1, Mathf.CeilToInt(radius / 3f));
                Reserve(1 + segments * rings);
                var shape = new float[segments];
                float phase = range(0f, 10f);
                for (int s = 0; s < segments; s++)
                    shape[s] = radius * (0.72f + 0.28f * Mathf.PerlinNoise(phase + s * 0.55f, phase * 0.3f) + range(-.06f, .06f));
                Vector3 Point(Vector2 p) => new(p.x, flat ? lift : b.Height.Sample(p.x, p.y) + lift, p.y);
                int center = Vertex(Point(c), flat ? Vector3.up : Normal(b, c), uv);
                int first = _vertices.Count;
                for (int ring = 1; ring <= rings; ring++)
                for (int s = 0; s < segments; s++)
                {
                    float angle = s * Mathf.PI * 2f / segments;
                    Vector2 p = c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * shape[s] * ring / rings;
                    Vertex(Point(p), flat ? Vector3.up : Normal(b, p), uv);
                }
                for (int s = 0; s < segments; s++)
                {
                    int n = (s + 1) % segments;
                    _triangles.AddRange(new[] { center, first + n, first + s });
                    for (int ring = 1; ring < rings; ring++)
                    {
                        int i0 = first + (ring - 1) * segments, i1 = first + ring * segments;
                        _triangles.AddRange(new[] { i0 + s, i0 + n, i1 + n, i0 + s, i1 + n, i1 + s });
                    }
                }
            }

            public void Ribbon(MapVariantBuilder b, List<Vector2> points, float width, float offset, float lift, Vector2 uv)
            {
                Reserve(points.Count * 2);
                int start = _vertices.Count;
                for (int i = 0; i < points.Count; i++)
                {
                    Vector2 dir = (points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(i - 1, 0)]).normalized;
                    Vector2 side = new(-dir.y, dir.x);
                    float taper = Mathf.Clamp01(Mathf.Min(i, points.Count - 1 - i) / 3f) * .7f + .3f;
                    Vector2 center = points[i] + side * offset;
                    foreach (float sign in new[] { -1f, 1f })
                    {
                        Vector2 p = center + side * sign * width * .5f * taper;
                        Vertex(new Vector3(p.x, b.Height.Sample(p.x, p.y) + lift, p.y), Normal(b, p), uv);
                    }
                }
                for (int i = 0; i + 1 < points.Count; i++)
                {
                    int a = start + i * 2;
                    _triangles.AddRange(new[] { a, a + 2, a + 3, a, a + 3, a + 1 });
                }
            }

            private static Vector3 Normal(MapVariantBuilder b, Vector2 p)
            {
                float dx = b.Height.Sample(p.x + 1f, p.y) - b.Height.Sample(p.x - 1f, p.y);
                float dz = b.Height.Sample(p.x, p.y + 1f) - b.Height.Sample(p.x, p.y - 1f);
                return new Vector3(-dx, 2f, -dz).normalized;
            }

            public void Flush()
            {
                if (_vertices.Count == 0) return;
                var mesh = new Mesh { name = $"Beautify_{Version}_{_name}_{_index++:00}", indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(_parent, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                _report.paintMeshes++;
                _report.paintTriangles += _triangles.Count / 3;
                _vertices.Clear(); _normals.Clear(); _uvs.Clear(); _triangles.Clear();
            }
        }
    }
}
