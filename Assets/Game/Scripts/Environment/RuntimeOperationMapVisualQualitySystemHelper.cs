using System;
using Game.Configs;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class RuntimeOperationMapVisualQualitySystemHelper
    {
        private Material _foundationMaterialInstance;
        private int _loggedObstructionCount;

        public int FoundationVisualCount { get; private set; }
        public int SuppressedObstructionCount { get; private set; }

        public GameObject CreateFoundation(RuntimeOperationMapFoundationSettings settings, Transform parent)
        {
            if (!settings.IsConfigured || parent == null)
                return null;

            GameObject foundation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foundation.name = "RuntimeMapFoundation";
            foundation.transform.SetParent(parent, false);
            foundation.transform.SetPositionAndRotation(settings.Position, Quaternion.identity);
            foundation.transform.localScale = settings.Scale;

            Collider collider = foundation.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);

            _foundationMaterialInstance = new Material(settings.Material)
            {
                name = settings.Material.name + "_RuntimeFoundation"
            };
            ApplyColor(_foundationMaterialInstance, settings.Color);
            _foundationMaterialInstance.enableInstancing = true;
            MeshRenderer renderer = foundation.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = _foundationMaterialInstance;

            FoundationVisualCount = 1;
            return foundation;
        }

        public void ApplyClearanceRules(
            GameObject visual,
            RuntimeOperationMapVisualStage stage,
            RuntimeOperationMapVisualCleanupSettings cleanup)
        {
            if (visual == null)
                return;
            if (stage != RuntimeOperationMapVisualStage.DistrictModules || !cleanup.IsConfigured)
                return;

            Transform[] transforms = visual.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                bool obstructionName = IsLargeTerrainObstruction(candidate.name);
                bool moduleTerrainName = IsModuleTerrain(candidate.name);
                bool clipCandidateName = IsClipCandidate(candidate.name);
                if (!obstructionName && !moduleTerrainName && !clipCandidateName)
                    continue;
                if (!candidate.gameObject.activeInHierarchy ||
                    !TryCalculateRendererBounds(candidate, out Bounds candidateBounds))
                    continue;

                bool largeObstruction = obstructionName && IsMeaningfullyLarge(candidate.name, candidateBounds);
                bool oversizedTerrainPad = moduleTerrainName &&
                                           Mathf.Max(candidateBounds.size.x, candidateBounds.size.z) >= 12f;
                bool outsideAuthoredFootprint = clipCandidateName &&
                                                cleanup.IsConfigured &&
                                                !cleanup.Contains(candidateBounds.center);
                if (!largeObstruction && !oversizedTerrainPad && !outsideAuthoredFootprint)
                    continue;

                candidate.gameObject.SetActive(false);
                SuppressedObstructionCount++;
                if (largeObstruction && _loggedObstructionCount < 12)
                {
                    _loggedObstructionCount++;
                    Debug.Log(
                        $"[RuntimeMapQuality] action=suppressDistrictObstruction name={candidate.name} " +
                        $"center={candidateBounds.center} size={candidateBounds.size}");
                }
                else if (outsideAuthoredFootprint && _loggedObstructionCount < 12)
                {
                    _loggedObstructionCount++;
                    Debug.Log(
                        $"[RuntimeMapQuality] action=suppressOutsideDistrictFootprint name={candidate.name} " +
                        $"center={candidateBounds.center} size={candidateBounds.size}");
                }
            }
        }

        public void Dispose()
        {
            FoundationVisualCount = 0;
            SuppressedObstructionCount = 0;
            _loggedObstructionCount = 0;
            if (_foundationMaterialInstance != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(_foundationMaterialInstance);
                else
                    UnityEngine.Object.DestroyImmediate(_foundationMaterialInstance);
            }

            _foundationMaterialInstance = null;
        }

        private static bool IsMeaningfullyLarge(string objectName, Bounds bounds)
        {
            if (Contains(objectName, "_Env_SandDunes_") || Contains(objectName, "_Env_Boulder_"))
                return true;

            return Mathf.Max(bounds.size.x, bounds.size.z) >= 7f || bounds.size.y >= 3.5f;
        }

        private static bool TryCalculateRendererBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool initialized = false;
            bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!renderer.enabled || renderer is ParticleSystemRenderer || renderer is TrailRenderer)
                    continue;

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return initialized;
        }

        private static bool IsLargeTerrainObstruction(string objectName)
        {
            return Contains(objectName, "_Env_Rock_") ||
                   Contains(objectName, "_Env_SandDunes_") ||
                   Contains(objectName, "_Env_Boulder_");
        }

        private static bool IsModuleTerrain(string objectName)
        {
            return Contains(objectName, "_Env_Ground_Hill") ||
                   Contains(objectName, "_Env_Ground_Round") ||
                   Contains(objectName, "_Env_SandEdge") ||
                   Contains(objectName, "_Env_SandDunes_");
        }

        private static bool IsClipCandidate(string objectName)
        {
            return IsLargeTerrainObstruction(objectName) ||
                   IsModuleTerrain(objectName) ||
                   Contains(objectName, "_Env_Road_") ||
                   Contains(objectName, "_Env_Sidewalk_") ||
                   Contains(objectName, "_Prop_Powerpole_") ||
                   Contains(objectName, "_Prop_Street_Light_") ||
                   Contains(objectName, "_Prop_Washingline_") ||
                   Contains(objectName, "_Prop_Wire_Lights_") ||
                   Contains(objectName, "_Prop_Fence_");
        }

        private static bool Contains(string value, string fragment)
        {
            return value?.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }
    }
}
