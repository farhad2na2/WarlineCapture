using System;
using System.Collections.Generic;
using Game.Authoring;
using Game.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Composition
{
    /// <summary>Render static S004 scenery in spatial instance groups without changing its gameplay objects.</summary>
    public sealed class SkirmishS004SceneryInstancing : MonoBehaviour
    {
        private const float CellSize = 64f;
        private sealed class Batch
        {
            public Mesh Mesh;
            public Material Material;
            public int SubMesh, Layer;
            public ShadowCastingMode Shadows;
            public bool ReceiveShadows;
            public Bounds Bounds;
            public readonly List<Matrix4x4> Pending = new();
            public Matrix4x4[] Matrices;
        }
        private readonly Dictionary<(Mesh, Material, int, int, int, int, ShadowCastingMode, bool), Batch> groups = new();
        private readonly Dictionary<(Material, Color, Color), Material> materials = new();
        private readonly List<MeshRenderer> suppressed = new();
        private readonly HashSet<Transform> towns = new();
        private MaterialPropertyBlock properties;
        private readonly Plane[] planes = new Plane[6];
        private Camera camera;
        private int expectedTowns;
        private float nextTownScan;
        private bool failed;
        public int SuppressedRendererCount => suppressed.Count;
        public int BatchCount => groups.Count;

        public void Configure(Camera worldCamera, Transform mapRoot, StaticMapPresentationManifest manifest,
            int townCount, bool includeSource)
        {
            camera = worldCamera;
            expectedTowns = townCount;
            properties ??= new MaterialPropertyBlock();
            if (includeSource)
            {
                var byPath = new Dictionary<string, MeshRenderer>(StringComparer.Ordinal);
                foreach (var renderer in mapRoot.GetComponentsInChildren<MeshRenderer>(true))
                    byPath[HierarchyPath(renderer.transform, mapRoot)] = renderer;
                foreach (var source in manifest.Sources)
                {
                    if (!byPath.TryGetValue(source.SourceHierarchyPath, out var renderer))
                        throw new InvalidOperationException("S004 static source missing: " + source.SourceHierarchyPath);
                    Add(renderer);
                }
                Seal();
            }
            Debug.Log("[S004SceneryInstancing] stage=SourceReady renderers=" + suppressed.Count + " batches=" + groups.Count);
        }

        private void LateUpdate()
        {
            if (failed || camera == null) return;
            try
            {
                if (towns.Count < expectedTowns && Time.unscaledTime >= nextTownScan)
                {
                    nextTownScan = Time.unscaledTime + 2f;
                    foreach (var authoring in UnityEngine.Object.FindObjectsByType<BuildingDefinitionAuthoring>())
                    {
                        if (!authoring.name.StartsWith("S004_TownBlock_", StringComparison.Ordinal) ||
                            !towns.Add(authoring.transform)) continue;
                        foreach (var renderer in authoring.GetComponentsInChildren<MeshRenderer>()) Add(renderer, true);
                    }
                    Seal();
                    if (towns.Count >= expectedTowns)
                        Debug.Log("[S004SceneryInstancing] result=Passed towns=" + towns.Count + " renderers=" + suppressed.Count + " batches=" + groups.Count);
                }
                GeometryUtility.CalculateFrustumPlanes(camera, planes);
                foreach (var batch in groups.Values)
                {
                    if (!GeometryUtility.TestPlanesAABB(planes, batch.Bounds)) continue;
                    var render = new RenderParams(batch.Material)
                    {
                        camera = camera, layer = batch.Layer, worldBounds = batch.Bounds,
                        shadowCastingMode = batch.Shadows, receiveShadows = batch.ReceiveShadows,
                        lightProbeUsage = LightProbeUsage.BlendProbes
                    };
                    for (int start = 0; start < batch.Matrices.Length; start += 511)
                        Graphics.RenderMeshInstanced(render, batch.Mesh, batch.SubMesh, batch.Matrices,
                            Math.Min(511, batch.Matrices.Length - start), start);
                }
            }
            catch (Exception exception)
            {
                failed = true;
                Restore();
                Debug.LogError("[S004SceneryInstancing] result=Failed restored=1 " + exception);
            }
        }

        private void Add(MeshRenderer renderer, bool town = false)
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.forceRenderingOff) return;
            renderer.GetPropertyBlock(properties);
            if (!properties.isEmpty && !town) return; // Preserve unknown authored overrides.
            var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null) return;
            var shared = renderer.sharedMaterials;
            if (shared.Length == 0 || Array.Exists(shared, material => material == null)) return;
            int x = Mathf.FloorToInt(renderer.bounds.center.x / CellSize);
            int z = Mathf.FloorToInt(renderer.bounds.center.z / CellSize);
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var original = shared[Math.Min(sub, shared.Length - 1)];
                Color baseColor = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : Color.white;
                Color color = original.HasProperty("_Color") ? original.GetColor("_Color") : Color.white;
                // Town prefabs have only the shared building faction colour override.
                if (town && properties.HasColor("_BaseColor")) baseColor = properties.GetColor("_BaseColor");
                if (town && properties.HasColor("_Color")) color = properties.GetColor("_Color");
                var materialKey = (original, baseColor, color);
                if (!materials.TryGetValue(materialKey, out var material))
                {
                    material = new Material(original) { enableInstancing = true, hideFlags = HideFlags.DontSave };
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
                    if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                    materials.Add(materialKey, material);
                }
                var key = (mesh, material, sub, x, z, renderer.gameObject.layer, renderer.shadowCastingMode, renderer.receiveShadows);
                if (!groups.TryGetValue(key, out var batch))
                {
                    batch = new Batch { Mesh = mesh, Material = material, SubMesh = sub, Layer = renderer.gameObject.layer,
                        Shadows = renderer.shadowCastingMode, ReceiveShadows = renderer.receiveShadows, Bounds = renderer.bounds };
                    groups.Add(key, batch);
                }
                batch.Bounds.Encapsulate(renderer.bounds);
                batch.Pending.Add(renderer.localToWorldMatrix);
                batch.Matrices = null;
            }
            renderer.enabled = false;
            suppressed.Add(renderer);
        }
        private void Seal()
        {
            foreach (var batch in groups.Values)
                if (batch.Matrices == null) batch.Matrices = batch.Pending.ToArray();
        }
        private static string HierarchyPath(Transform item, Transform root)
        {
            var parts = new List<string>();
            for (var current = item; current != null; current = current.parent)
            {
                parts.Add(current.name + "[" + current.GetSiblingIndex() + "]");
                if (current == root) break;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }
        private void Restore()
        {
            foreach (var renderer in suppressed) if (renderer != null) renderer.enabled = true;
            suppressed.Clear();
        }
        private void OnDisable() { failed = true; Restore(); }
        private void OnDestroy()
        {
            Restore();
            foreach (var material in materials.Values)
                if (material != null) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
            materials.Clear(); groups.Clear();
        }
    }
}
