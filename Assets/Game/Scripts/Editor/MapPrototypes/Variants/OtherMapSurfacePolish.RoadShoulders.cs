using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        const string ShoulderArt = "Assets/Game/Art/MapBeautify/RoadShoulders";
        const float ShoulderRange = 3f;
        enum RoadLayer
        {
            None,
            Paved,
            Walk,
            Hard,
            Soil
        }

        static RoadLayer ShoulderLayer(MeshRenderer r, Mesh mesh, int slot)
        {
            // Runway paint, bridge markings and accessories retain their own submeshes.
            if (slot > 0)
                return RoadLayer.None;
            string name = mesh.name;
            if (name.Contains("Sidewalk") || name == "DenseCity_KerbPaving" || name == "SM_Env_Road_Edge_01")
                return RoadLayer.Walk;
            if (r.name == "Asphalt" || r.name == "Pad_M01_Asphalt" || name == "BridgeAsphaltAndLines" || name == "DenseCity_PavingBridgeCutouts" || name == "DenseCity_FlatPaving" || name == "SM_Env_Road_01" || RunwayRenderer(r))
                return RoadLayer.Paved;
            if (r.name == "Pad_M01_Concrete" || name.Contains("Concrete"))
                return RoadLayer.Hard;
            if (name.Contains("Ground") || name.Contains("DirtRoad") || name.Contains("SandWear") || name.Contains("Dune"))
                return RoadLayer.Soil;
            return RoadLayer.None;
        }

        // These legacy models paint both the bitumen and its sand verge into a
        // single submesh. The neutral greys belong to pavement or lane paint.
        // The brown atlas swatches belong to the receiving soil.
        static bool HasPaintedVerge(MeshRenderer renderer, Mesh mesh)
        {
            if (mesh.name != "SM_Env_Road_01" && mesh.name != "SM_Env_Runway_01")
                return false;
            var texture = renderer.sharedMaterial.GetTexture("_BaseMap");
            return texture && (texture.name == "Road_Texture" || texture.name == "Road_01");
        }

        sealed class PaintedVerge
        {
            const int Size = 512;
            static readonly Dictionary<Texture, PaintedVerge> cache = new();
            readonly bool[] asphalt = new bool[Size * Size];
            public static void ClearCache() => cache.Clear();
            public static PaintedVerge Read(Texture original)
            {
                if (cache.TryGetValue(original, out var value))
                    return value;
                value = new PaintedVerge();
                var image = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                try
                {
                    if (!ImageConversion.LoadImage(image, File.ReadAllBytes(AssetDatabase.GetAssetPath(original))))
                        throw new IOException("Could not read authored road atlas " + original.name);
                    var pixels = image.GetPixels32();
                    for (int y = 0; y < Size; y++)
                        for (int x = 0; x < Size; x++)
                        {
                            int px = (2 * x + 1) * image.width / (2 * Size), py = (2 * y + 1) * image.height / (2 * Size);
                            Color32 color = pixels[py * image.width + px];
                            value.asphalt[y * Size + x] = color.r - color.b < 18;
                        }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }

                cache[original] = value;
                return value;
            }

            public bool IsAsphalt(Vector2 uv)
            {
                int x = Mathf.FloorToInt(Mathf.Repeat(uv.x, 1) * Size);
                int y = Mathf.FloorToInt(Mathf.Repeat(uv.y, 1) * Size);
                return asphalt[Mathf.Min(y, Size - 1) * Size + Mathf.Min(x, Size - 1)];
            }
        }

        // Rasterize the actual top triangles. Bounding boxes would fill the inside of bends,
        // bridge cutouts, and the gaps between disjoint islands in combined meshes.
        sealed class ShoulderTile
        {
            public readonly int width, height;
            public readonly Vector4 rect;
            public readonly float step;
            readonly float[] paved, walk;
            readonly float[] hard, soil;
            readonly Dictionary<Mesh, (Vector3[] vertices, int[] indices, Vector2[] uv)> geometry;
            public int overlapPixels, exposedPixels;
            public Vector3 walkExample, openExample;
            public ShoulderTile(float x, float z, int size, float spacing, Dictionary<Mesh, (Vector3[] vertices, int[] indices, Vector2[] uv)> geometry)
            {
                step = spacing;
                width = height = size;
                this.geometry = geometry;
                rect = new Vector4(x, z, width * step, height * step);
                paved = new float[size * size];
                walk = new float[paved.Length];
                hard = new float[paved.Length];
                soil = new float[paved.Length];
                Array.Fill(paved, float.NegativeInfinity);
                Array.Fill(walk, float.NegativeInfinity);
                Array.Fill(hard, float.NegativeInfinity);
                Array.Fill(soil, float.NegativeInfinity);
            }

            public void Draw(MeshRenderer r, Mesh mesh, RoadLayer layer)
            {
                bool painted = layer == RoadLayer.Paved && HasPaintedVerge(r, mesh);
                var verge = painted ? PaintedVerge.Read(r.sharedMaterial.GetTexture("_BaseMap")) : null;
                Vector2 scale = painted ? r.sharedMaterial.GetTextureScale("_BaseMap") : Vector2.one;
                Vector2 offset = painted ? r.sharedMaterial.GetTextureOffset("_BaseMap") : Vector2.zero;
                if (!geometry.TryGetValue(mesh, out var src))
                {
                    using var md = Mesh.AcquireReadOnlyMeshData(mesh);
                    var d = md[0];
                    using var v = new Unity.Collections.NativeArray<Vector3>(d.vertexCount, Unity.Collections.Allocator.Temp);
                    using var ix = new Unity.Collections.NativeArray<int>(d.GetSubMesh(0).indexCount, Unity.Collections.Allocator.Temp);
                    d.GetVertices(v);
                    d.GetIndices(ix, 0);
                    Vector2[] uv = null;
                    if (mesh.name == "SM_Env_Road_01" || mesh.name == "SM_Env_Runway_01")
                    {
                        using var coords = new Unity.Collections.NativeArray<Vector2>(d.vertexCount, Unity.Collections.Allocator.Temp);
                        d.GetUVs(0, coords);
                        uv = coords.ToArray();
                    }

                    geometry[mesh] = src = (v.ToArray(), ix.ToArray(), uv);
                }

                var local = r.localToWorldMatrix;
                var vertices = new Vector3[src.vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    var v = local.MultiplyPoint3x4(src.vertices[i]);
                    vertices[i] = new Vector3((v.x - rect.x) / step - .5f, v.y, (v.z - rect.y) / step - .5f);
                }

                for (int i = 0; i < src.indices.Length; i += 3)
                {
                    var a = vertices[src.indices[i]];
                    var b = vertices[src.indices[i + 1]];
                    var c = vertices[src.indices[i + 2]];
                    float bx = b.x - a.x, bz = b.z - a.z, cx = c.x - a.x, cz = c.z - a.z, det = bx * cz - cx * bz;
                    if (Mathf.Abs(det) < .00001f)
                        continue;
                    int x0 = Mathf.Max(0, Mathf.CeilToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)))), x1 = Mathf.Min(width - 1, Mathf.FloorToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
                    int z0 = Mathf.Max(0, Mathf.CeilToInt(Mathf.Min(a.z, Mathf.Min(b.z, c.z)))), z1 = Mathf.Min(height - 1, Mathf.FloorToInt(Mathf.Max(a.z, Mathf.Max(b.z, c.z))));
                    for (int z = z0; z <= z1; z++)
                        for (int x = x0; x <= x1; x++)
                        {
                            float u = ((x - a.x) * cz - (z - a.z) * cx) / det, v = (bx * (z - a.z) - bz * (x - a.x)) / det;
                            if (u < -.0001f || v < -.0001f || u + v > 1.0001f)
                                continue;
                            int k = z * width + x;
                            float y = a.y + (b.y - a.y) * u + (c.y - a.y) * v;
                            bool vergePixel = false;
                            if (verge != null)
                            {
                                var uvA = src.uv[src.indices[i]];
                                var uvB = src.uv[src.indices[i + 1]];
                                var uvC = src.uv[src.indices[i + 2]];
                                var uv = uvA + (uvB - uvA) * u + (uvC - uvA) * v;
                                vergePixel = !verge.IsAsphalt(Vector2.Scale(uv, scale) + offset);
                            }

                            if (vergePixel)
                                soil[k] = Mathf.Max(soil[k], y);
                            else if (layer == RoadLayer.Paved)
                                paved[k] = Mathf.Max(paved[k], y);
                            else if (layer == RoadLayer.Walk)
                                walk[k] = Mathf.Max(walk[k], y);
                            else if (layer == RoadLayer.Hard)
                                hard[k] = Mathf.Max(hard[k], y);
                            else
                                soil[k] = Mathf.Max(soil[k], y);
                        }
                }
            }

            // A bounded chamfer transform also carries the nearest pavement elevation
            // through the outer shoulder, keeping grade-separated surfaces independent.
            float[] Distances(Func<int, bool> seed, int[] nearest = null)
            {
                float cap = ShoulderRange + 2 * step;
                var d = new float[paved.Length];
                for (int i = 0; i < d.Length; i++)
                {
                    bool hit = seed(i);
                    d[i] = hit ? 0 : cap;
                    if (nearest != null)
                        nearest[i] = hit ? i : -1;
                }

                void Relax(int p, int q, float cost)
                {
                    float candidate = d[q] + cost;
                    if (candidate >= d[p])
                        return;
                    d[p] = candidate;
                    if (nearest != null)
                        nearest[p] = nearest[q];
                }

                float diag = step * 1.41421356f;
                for (int z = 0; z < height; z++)
                    for (int x = 0; x < width; x++)
                    {
                        int p = z * width + x;
                        if (x > 0)
                            Relax(p, p - 1, step);
                        if (z == 0)
                            continue;
                        Relax(p, p - width, step);
                        if (x > 0)
                            Relax(p, p - width - 1, diag);
                        if (x + 1 < width)
                            Relax(p, p - width + 1, diag);
                    }

                for (int z = height - 1; z >= 0; z--)
                    for (int x = width - 1; x >= 0; x--)
                    {
                        int p = z * width + x;
                        if (x + 1 < width)
                            Relax(p, p + 1, step);
                        if (z + 1 == height)
                            continue;
                        Relax(p, p + width, step);
                        if (x > 0)
                            Relax(p, p + width - 1, diag);
                        if (x + 1 < width)
                            Relax(p, p + width + 1, diag);
                    }

                return d;
            }

            public Color32[] Encode(Vector4 elevation, Vector3 focus, int border, int tileSize)
            {
                // A model's buried skirt is not an exposed asphalt perimeter.
                // Find the visible road-to-terrain crossing before measuring distance.
                for (int i = 0; i < paved.Length; i++)
                    if (soil[i] > paved[i] + .055f)
                        paved[i] = float.NegativeInfinity;
                var nearest = new int[paved.Length];
                var outer = Distances(i => !float.IsNegativeInfinity(paved[i]), nearest);
                var inner = Distances(i => float.IsNegativeInfinity(paved[i]));
                var hardDistance = Distances(i => nearest[i] >= 0 && (Mathf.Abs(walk[i] - paved[nearest[i]]) < .65f || Mathf.Abs(hard[i] - paved[nearest[i]]) < .65f));
                var soilDistance = Distances(i => nearest[i] >= 0 && Mathf.Abs(soil[i] - paved[nearest[i]]) < .85f);
                // Look out of the carriageway across each boundary. A curb beside the
                // road, or a second paved module across a narrow expansion joint, is
                // a hard boundary even when sand continues underneath both meshes.
                bool OpensOntoSoil(int i, int dx, int dz)
                {
                    int x = i % width + dx, z = i / width + dz;
                    if (x < 0 || x >= width || z < 0 || z >= height)
                        return false;
                    int q = z * width + x;
                    if (!float.IsNegativeInfinity(paved[q]) || hardDistance[q] < step * .5f || soilDistance[q] > .95f)
                        return false;
                    for (int k = 1; k <= 8; k++, x += dx, z += dz)
                    {
                        if (x < 0 || x >= width || z < 0 || z >= height)
                            break;
                        q = z * width + x;
                        if (Mathf.Abs(paved[q] - paved[i]) < .85f)
                            return false;
                        if (k <= 3 && hardDistance[q] < step * .5f)
                            return false;
                    }

                    return true;
                }

                bool Exposed(int i)
                {
                    return !float.IsNegativeInfinity(paved[i]) && inner[i] <= step * 1.01f && hardDistance[i] >= step * .5f && (OpensOntoSoil(i, 1, 0) || OpensOntoSoil(i, -1, 0) || OpensOntoSoil(i, 0, 1) || OpensOntoSoil(i, 0, -1));
                }

                var exposedDistance = Distances(Exposed);
                int stored = tileSize + 2;
                var pixels = new Color32[stored * stored];
                bool active = false;
                float closestOpen = float.PositiveInfinity, closestWalk = float.PositiveInfinity;
                for (int z = 0; z < stored; z++)
                    for (int x = 0; x < stored; x++)
                    {
                        int i = (z + border - 1) * width + x + border - 1;
                        bool road = !float.IsNegativeInfinity(paved[i]);
                        float signed = road ? inner[i] - .5f * step : .5f * step - outer[i];
                        float y = nearest[i] < 0 ? elevation.x : paved[nearest[i]];
                        bool contact = road && !float.IsNegativeInfinity(walk[i]) && paved[i] - walk[i] >= -.045f && paved[i] - walk[i] < .36f;
                        float clearance = Mathf.Clamp01(hardDistance[i] / .42f) * Mathf.InverseLerp(2.9f, .5f, exposedDistance[i]) * Mathf.InverseLerp(2.7f, .55f, soilDistance[i]);
                        bool supported = road && soil[i] >= paved[i] - .85f && soil[i] <= paved[i] + .025f;
                        byte B(float value) => (byte)Mathf.RoundToInt(255 * Mathf.Clamp01(value));
                        pixels[z * stored + x] = new Color32(B(.5f + signed / (2 * ShoulderRange)), contact ? (byte)255 : supported ? (byte)96 : (byte)0, B(clearance), B((y - elevation.x) / elevation.y));
                        bool exposed = exposedDistance[i] == 0;
                        if (contact || (Mathf.Abs(signed) < ShoulderRange && clearance > .15f))
                            active = true;
                        if (x == 0 || z == 0 || x == stored - 1 || z == stored - 1)
                            continue;
                        if (exposed)
                            exposedPixels++;
                        if (contact)
                            overlapPixels++;
                        if (!exposed && !contact)
                            continue;
                        var point = new Vector3(rect.x + (i % width + .5f) * step, y, rect.y + (i / width + .5f) * step);
                        float d = (point - focus).sqrMagnitude;
                        if (exposed && d < closestOpen)
                        {
                            openExample = point;
                            closestOpen = d;
                        }

                        if (contact && d < closestWalk)
                        {
                            walkExample = point;
                            closestWalk = d;
                        }
                    }

                return active ? pixels : null;
            }
        }

        sealed class RoadField
        {
            const int Tile = 96, Border = 13, Stored = Tile + 2;
            public const float step = .30f;
            const float Cell = Tile * step;
            public readonly Vector4 rect;
            readonly int columns, rows;
            readonly List<(MeshRenderer renderer, Mesh mesh, RoadLayer layer)> surfaces = new();
            public int exposedPixels, overlapPixels;
            public Vector3 openExample, walkExample;
            public RoadField(Bounds bounds)
            {
                float x = Mathf.Floor((bounds.min.x - 5) / Cell) * Cell, z = Mathf.Floor((bounds.min.z - 5) / Cell) * Cell;
                columns = Mathf.CeilToInt((bounds.max.x + 5 - x) / Cell);
                rows = Mathf.CeilToInt((bounds.max.z + 5 - z) / Cell);
                rect = new Vector4(x, z, columns * Cell, rows * Cell);
            }

            public void Draw(MeshRenderer r, Mesh mesh, RoadLayer layer) => surfaces.Add((r, mesh, layer));
            public Texture2D Save(string map, Vector3 focus, out Vector4 elevation)
            {
                var roads = surfaces.Where(s => s.layer == RoadLayer.Paved).ToArray();
                float low = roads.Min(s => s.renderer.bounds.min.y);
                elevation = new Vector4(low, Mathf.Max(.01f, roads.Max(s => s.renderer.bounds.max.y) - low), step, 0);
                var bins = new Dictionary<int, List<int>>();
                // Bin pavement first. Walks and hardstanding only enter tiles close to a road.
                foreach (var order in new[]
                {
                    true,
                    false
                }

                )
                    for (int i = 0; i < surfaces.Count; i++)
                    {
                        var surface = surfaces[i];
                        if ((surface.layer == RoadLayer.Paved) != order)
                            continue;
                        var b = surface.renderer.bounds;
                        float pad = (Border + 1) * step;
                        int x0 = Mathf.Max(0, Mathf.FloorToInt((b.min.x - pad - rect.x) / Cell)), x1 = Mathf.Min(columns - 1, Mathf.FloorToInt((b.max.x + pad - rect.x) / Cell));
                        int z0 = Mathf.Max(0, Mathf.FloorToInt((b.min.z - pad - rect.y) / Cell)), z1 = Mathf.Min(rows - 1, Mathf.FloorToInt((b.max.z + pad - rect.y) / Cell));
                        for (int z = z0; z <= z1; z++)
                            for (int x = x0; x <= x1; x++)
                            {
                                int key = z * columns + x;
                                if (!bins.TryGetValue(key, out var list))
                                {
                                    if (!order)
                                        continue;
                                    bins[key] = list = new List<int>();
                                }

                                list.Add(i);
                            }
                    }

                var pages = new List<(int key, Color32[] pixels)>();
                var geometry = new Dictionary<Mesh, (Vector3[] vertices, int[] indices, Vector2[] uv)>();
                foreach (var bin in bins.OrderBy(p => p.Key))
                {
                    float x = rect.x + (bin.Key % columns) * Cell, z = rect.y + (bin.Key / columns) * Cell;
                    var tile = new ShoulderTile(x - Border * step, z - Border * step, Tile + 2 * Border, step, geometry);
                    foreach (int id in bin.Value)
                    {
                        var s = surfaces[id];
                        tile.Draw(s.renderer, s.mesh, s.layer);
                    }

                    var pixels = tile.Encode(elevation, focus, Border, Tile);
                    exposedPixels += tile.exposedPixels;
                    overlapPixels += tile.overlapPixels;
                    if (tile.exposedPixels > 0 && (openExample == default || (tile.openExample - focus).sqrMagnitude < (openExample - focus).sqrMagnitude))
                        openExample = tile.openExample;
                    if (tile.overlapPixels > 0 && (walkExample == default || (tile.walkExample - focus).sqrMagnitude < (walkExample - focus).sqrMagnitude))
                        walkExample = tile.walkExample;
                    if (pixels != null)
                        pages.Add((bin.Key, pixels));
                }

                if (pages.Count == 0)
                    throw new InvalidOperationException(map + " has no asphalt transitions");
                int perRow = Mathf.CeilToInt(Mathf.Sqrt(pages.Count));
                // Large maps have many byte-identical pages. Share them without downsampling the road field.
                bool compact = perRow * Stored > 4096 || rows + perRow * Stored > 4096;
                var unique = compact ? new Dictionary<Color32[], int>(new RoadPageComparer()) : null;
                var stored = new List<Color32[]>();
                var addresses = new int[pages.Count];
                for (int i = 0; i < pages.Count; i++)
                {
                    var pixels = pages[i].pixels;
                    if (compact && unique.TryGetValue(pixels, out int existing))
                    {
                        addresses[i] = existing;
                        continue;
                    }
                    addresses[i] = stored.Count;
                    unique?.Add(pixels, stored.Count);
                    stored.Add(pixels);
                }
                perRow = Mathf.CeilToInt(Mathf.Sqrt(stored.Count));
                int w = Mathf.Max(columns, perRow * Stored), h = rows + Mathf.CeilToInt((float)stored.Count / perRow) * Stored;
                if (w > 8192 || h > 8192)
                    throw new InvalidOperationException("Road atlas exceeds mobile texture limits: " + map);
                var atlas = new Color32[w * h];
                for (int i = 0; i < pages.Count; i++)
                {
                    int id = addresses[i], key = pages[i].key;
                    atlas[key / columns * w + key % columns] = new Color32((byte)(id % perRow), (byte)(id / perRow), 255, 255);
                }
                for (int id = 0; id < stored.Count; id++)
                {
                    int x = id % perRow, z = id / perRow;
                    for (int j = 0; j < Stored; j++)
                        Array.Copy(stored[id], j * Stored, atlas, (rows + z * Stored + j) * w + x * Stored, Stored);
                }

                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                tex.SetPixels32(atlas);
                tex.Apply();
                string path = ShoulderArt + "/" + map + ".png";
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.sRGBTexture = false;
                imp.alphaSource = TextureImporterAlphaSource.FromInput;
                imp.alphaIsTransparency = false;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.filterMode = FilterMode.Bilinear;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.maxTextureSize = w > 4096 || h > 4096 ? 8192 : 4096;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
                var result = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (result.width != w || result.height != h)
                    throw new Exception("Road lookup atlas was resized: " + map);
                return result;
            }
        }

        sealed class RoadPageComparer : IEqualityComparer<Color32[]>
        {
            public bool Equals(Color32[] a, Color32[] b)
            {
                if (a.Length != b.Length)
                    return false;
                for (int i = 0; i < a.Length; i++)
                {
                    var left = a[i];
                    var right = b[i];
                    if (left.r != right.r || left.g != right.g || left.b != right.b || left.a != right.a)
                        return false;
                }
                return true;
            }

            public int GetHashCode(Color32[] pixels)
            {
                unchecked
                {
                    uint hash = 2166136261u;
                    foreach (var p in pixels)
                    {
                        uint rgba = (uint)(p.r | p.g << 8 | p.b << 16 | p.a << 24);
                        hash = (hash ^ rgba) * 16777619u;
                    }
                    return (int)hash;
                }
            }
        }

        static SerializedProperty SavedRoadProperty(SerializedObject material, string group, string name)
        {
            var list = material.FindProperty("m_SavedProperties." + group);
            for (int i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue == name)
                    return entry.FindPropertyRelative("second");
            }

            list.InsertArrayElementAtIndex(list.arraySize);
            var added = list.GetArrayElementAtIndex(list.arraySize - 1);
            added.FindPropertyRelative("first").stringValue = name;
            return added.FindPropertyRelative("second");
        }

        static void BindRoadField(Material m, Texture2D map, Vector4 rect, Vector4 levels, float role)
        {
            if (m.GetTexture("_StreetEdgeMap") == map && m.GetFloat("_StreetEdgeMode") == role && m.GetVector("_StreetEdgeRect") == rect && m.GetVector("_StreetEdgeLevels") == levels)
                return;
            // Old materials can acquire shader defaults on their first reimport.
            // Persist all four bindings together, then read back the imported asset.
            var serialized = new SerializedObject(m);
            var tex = SavedRoadProperty(serialized, "m_TexEnvs", "_StreetEdgeMap");
            tex.FindPropertyRelative("m_Texture").objectReferenceValue = map;
            tex.FindPropertyRelative("m_Scale").vector2Value = Vector2.one;
            tex.FindPropertyRelative("m_Offset").vector2Value = Vector2.zero;
            SavedRoadProperty(serialized, "m_Colors", "_StreetEdgeRect").colorValue = (Color)rect;
            SavedRoadProperty(serialized, "m_Colors", "_StreetEdgeLevels").colorValue = (Color)levels;
            SavedRoadProperty(serialized, "m_Floats", "_StreetEdgeMode").floatValue = role;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssetIfDirty(m);
            string path = AssetDatabase.GetAssetPath(m);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var loaded = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (loaded.GetTexture("_StreetEdgeMap") != map || loaded.GetFloat("_StreetEdgeMode") != role || loaded.GetVector("_StreetEdgeRect") != rect || loaded.GetVector("_StreetEdgeLevels") != levels)
                throw new InvalidOperationException("Road bindings did not survive import: " + path);
        }

        public static void BakeRoadShoulders() => BakeRoadShoulders(null);
        static void BakeRoadShoulders(string onlyMap)
        {
            Check();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            Directory.CreateDirectory(ShoulderArt);
            var materialMaps = new Dictionary<Material, string>();
            var scenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Game/Scenes/OperationMaps" }).Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith("/PreparedEntities.unity") || p.Contains("entity_presentation") || p == Game.Editor.StaticMapPresentationBaker.CurrentStagedOperationMapScenePath).OrderBy(p => p).ToArray();
            try
            {
                foreach (string source in scenes)
                {
                    string map = source == Game.Editor.StaticMapPresentationBaker.CurrentStagedOperationMapScenePath ? "StagedDesertBase" : source.Contains("dense_city") ? "DenseCity" : source.Contains("_candidate.unity") ? "DesertBase" : Path.GetFileName(Path.GetDirectoryName(source));
                    if (onlyMap != null && onlyMap != map)
                        continue;
                    bool legacy = map == "Frontier" || map == "DesertBase" || map == "StagedDesertBase";
                    var scene = EditorSceneManager.OpenScene(source, OpenSceneMode.Single);
                    var art = SceneRenderers(scene).Where(r => r.enabled && r.gameObject.activeInHierarchy).Select(r => (r, mesh: r.GetComponent<MeshFilter>()?.sharedMesh)).Where(x => x.mesh && x.r.sharedMaterial).Select(x => (x.r, x.mesh, layer: ShoulderLayer(x.r, x.mesh, 0))).ToArray();
                    var roads = art.Where(x => x.layer == RoadLayer.Paved).ToArray();
                    if (roads.Length == 0)
                        throw new InvalidOperationException("Missing road surfaces in " + map);
                    Bounds bounds = roads[0].r.bounds;
                    foreach (var road in roads)
                        bounds.Encapsulate(road.r.bounds);
                    var field = new RoadField(bounds);
                    bounds.Expand(new Vector3(10, 4000, 10));
                    if (legacy) SaveLegacySoilArea(map, bounds);
                    foreach (var x in art)
                        if (x.layer != RoadLayer.None && bounds.Intersects(x.r.bounds))
                        {
                            field.Draw(x.r, x.mesh, x.layer);
                            if (x.mesh.name == "BridgeAsphaltAndLines")
                                field.Draw(x.r, x.mesh, RoadLayer.Hard);
                        }

                    var focus = bounds.center;
                    if (!legacy)
                    {
                        var def = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                        focus = (def.Bounds.PlayableMin + def.Bounds.PlayableMax) * .5f;
                    }

                    var texture = field.Save(map, focus, out var elevation);
                    var sharedUses = art.GroupBy(x => x.r.sharedMaterial).ToDictionary(g => g.Key, g => g.Select(x => x.layer).Distinct().ToArray());
                    var assigned = new HashSet<Material>();
                    var copies = new Dictionary<(Material, RoadLayer), Material>();
                    var asphaltCount = 0;
                    foreach (var x in art)
                    {
                        if (x.layer != RoadLayer.Soil && x.layer != RoadLayer.Paved)
                            continue;
                        var original = x.r.sharedMaterial;
                        string path = AssetDatabase.GetAssetPath(original);
                        bool scoped = path.Contains("/" + map + "/") || Path.GetFileName(path).StartsWith(map + "_", StringComparison.Ordinal) || Path.GetFileNameWithoutExtension(path) == map;
                        // The combined city streets use a project-owned civic material with a GUID filename.
                        if (map == "DenseCity" && (x.mesh.name == "DenseCity_PavingBridgeCutouts" || x.mesh.name == "DenseCity_FlatPaving") && path.StartsWith(FixArt + "/Civic/", StringComparison.Ordinal))
                            scoped = true;
                        bool exclusive = sharedUses[original].Length == 1;
                        if (x.layer == RoadLayer.Soil && (!scoped || !exclusive || original.shader.name != GroundShaderName) && !legacy)
                            continue;
                        if (!bounds.Intersects(x.r.bounds) && x.layer == RoadLayer.Soil)
                            continue;
                        var m = original;
                        if (!scoped || !exclusive || original.shader.name != GroundShaderName)
                        {
                            var key = (original, x.layer);
                            if (!copies.TryGetValue(key, out m))
                            {
                                string clonePath = ShoulderArt + "/" + map + "_" + AssetDatabase.AssetPathToGUID(path) + "_" + x.layer + ".mat";
                                m = AssetDatabase.LoadAssetAtPath<Material>(clonePath);
                                if (!m)
                                {
                                    m = new Material(original);
                                    if (m.shader.name != GroundShaderName)
                                    {
                                        Texture atlas = original.mainTexture;
                                        Color tint = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : Color.white;
                                        m.shader = Shader.Find(GroundShaderName);
                                        m.SetTexture("_BaseMap", atlas);
                                        m.SetFloat("_StreetLitSource", original.shader.name == "Universal Render Pipeline/Lit" ? 1 : 0);
                                        m.SetColor("_BaseColor", tint);
                                        m.SetFloat("_GroundDetailStrength", 0);
                                        m.SetFloat("_MacroStrength", 0);
                                    }

                                    AssetDatabase.CreateAsset(m, clonePath);
                                }

                                copies.Add(key, m);
                            }

                        }

                        if (x.layer == RoadLayer.Paved)
                            asphaltCount++;
                        if (!assigned.Add(m))
                            continue;
                        if (materialMaps.TryGetValue(m, out var previous) && previous != map)
                            throw new Exception(path + " shared by " + map + " and " + previous);
                        materialMaps[m] = map;
                        BindRoadField(m, texture, field.rect, elevation, x.layer == RoadLayer.Paved ? (HasPaintedVerge(x.r, x.mesh) ? 3 : 2) : 1);
                    }

                    if (asphaltCount != roads.Length || (field.exposedPixels == 0 && field.overlapPixels == 0))
                        throw new Exception("Incomplete surface coverage for " + map);
                    Debug.Log($"[RoadShoulders] map={map} paved={asphaltCount} walks={art.Count(x => x.layer == RoadLayer.Walk)} exposedPixels={field.exposedPixels} overlapPixels={field.overlapPixels} materials={assigned.Count} copies={copies.Count} texel={RoadField.step:F3} open={field.openExample:F3} walk={field.walkExample:F3}");
                }

                if (ShaderUtil.ShaderHasError(Shader.Find(GroundShaderName)))
                    throw new Exception("Road surface shader failed compilation");
                Debug.Log("[RoadShoulders] result=Passed maps=" + (onlyMap == null ? scenes.Length : 1));
            }
            finally
            {
                PaintedVerge.ClearCache();
                RestoreVisualReviewSetup(setup);
            }
        }
    }
}
