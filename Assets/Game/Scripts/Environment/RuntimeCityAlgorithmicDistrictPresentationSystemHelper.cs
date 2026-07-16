using System.Collections.Generic;
using Game.Configs;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class RuntimeCityAlgorithmicDistrictPresentationSystemHelper
    {
        private const float OuterTransitionScale = 1.34f;
        private const float TransitionScale = 1.15f;
        private readonly List<Mesh> _generatedMeshes = new();
        private readonly List<Material> _generatedMaterials = new();
        private Transform _root;

        public int SurfaceCount { get; private set; }

        public void CreateSurfaces(
            IReadOnlyList<RuntimeOperationMapAlgorithmicDistrictSurfaceSettings> settings,
            uint seed,
            Vector3 cityCenter,
            float roadCellWorldSize,
            Color baseGroundColor,
            Transform parent)
        {
            if (settings == null || settings.Count == 0 || parent == null)
                return;

            var rootObject = new GameObject("RuntimeCityDistrictSurfaces");
            _root = rootObject.transform;
            _root.SetParent(parent, false);
            float cellSize = Mathf.Max(0.1f, roadCellWorldSize);
            for (int i = 0; i < settings.Count; i++)
            {
                RuntimeOperationMapAlgorithmicDistrictSurfaceSettings district = settings[i];
                if (!district.IsConfigured)
                    continue;

                Vector3 districtPosition = cityCenter + new Vector3(
                    district.OffsetInRoadCells.x * cellSize,
                    0f,
                    district.OffsetInRoadCells.y * cellSize);
                Vector3 districtScale = new(
                    district.SizeInRoadCells.x * cellSize,
                    1f,
                    district.SizeInRoadCells.y * cellSize);
                uint districtSeed = seed + district.SeedOffset;
                CreateSurface(
                    district.SurfaceName + "_OuterTransition",
                    district.Material,
                    districtSeed,
                    districtPosition + Vector3.up * 0.003f,
                    new Vector3(
                        districtScale.x * OuterTransitionScale,
                        districtScale.y,
                        districtScale.z * OuterTransitionScale),
                    Color.Lerp(baseGroundColor, district.Color, 0.14f));
                CreateSurface(
                    district.SurfaceName + "_Transition",
                    district.Material,
                    districtSeed,
                    districtPosition + Vector3.up * 0.006f,
                    new Vector3(
                        districtScale.x * TransitionScale,
                        districtScale.y,
                        districtScale.z * TransitionScale),
                    Color.Lerp(baseGroundColor, district.Color, 0.35f));
                CreateSurface(
                    district.SurfaceName,
                    district.Material,
                    districtSeed,
                    districtPosition + Vector3.up * 0.012f,
                    districtScale,
                    district.Color);
            }
        }

        public void Dispose()
        {
            if (_root != null)
                DestroyObject(_root.gameObject);
            for (int i = 0; i < _generatedMeshes.Count; i++)
                DestroyObject(_generatedMeshes[i]);
            for (int i = 0; i < _generatedMaterials.Count; i++)
                DestroyObject(_generatedMaterials[i]);

            _generatedMeshes.Clear();
            _generatedMaterials.Clear();
            _root = null;
            SurfaceCount = 0;
        }

        private void CreateSurface(
            string surfaceName,
            Material sourceMaterial,
            uint seed,
            Vector3 position,
            Vector3 scale,
            Color color)
        {
            GameObject surface = RuntimeOperationMapSurfaceGeometrySystemHelper.CreateIrregularSurface(
                surfaceName,
                seed,
                _root);
            surface.transform.position = position;
            surface.transform.localScale = scale;

            MeshFilter filter = surface.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                _generatedMeshes.Add(filter.sharedMesh);

            var material = new Material(sourceMaterial)
            {
                name = sourceMaterial.name + "_Runtime_" + surfaceName,
                enableInstancing = true
            };
            ApplyColor(material, color);
            _generatedMaterials.Add(material);
            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
            SurfaceCount++;
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        private static void DestroyObject(Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }
}
