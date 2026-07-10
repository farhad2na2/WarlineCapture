using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Rendering
{
    public enum StaticMapChunkRendererSafety
    {
        Safe,
        UnavailableOrInactive,
        GeneratedOutput,
        ExcludedAuthoringHierarchy,
        StaticBatch,
        LodGroup,
        UnsupportedComponents
    }

    public enum StaticMapChunkSourceEligibility
    {
        Eligible,
        UnavailableOrInactive,
        ExcludedHierarchy,
        Unsafe,
        MissingMesh,
        UnreadableMesh,
        TooLarge,
        UnsupportedMaterialLayout
    }

    public readonly struct StaticMapChunkSourceEvaluation
    {
        public readonly StaticMapChunkSourceEligibility Eligibility;
        public readonly MeshFilter MeshFilter;
        public readonly Mesh Mesh;
        public readonly Material Material;

        public bool IsEligible => Eligibility == StaticMapChunkSourceEligibility.Eligible;

        public StaticMapChunkSourceEvaluation(
            StaticMapChunkSourceEligibility eligibility,
            MeshFilter meshFilter = null,
            Mesh mesh = null,
            Material material = null)
        {
            Eligibility = eligibility;
            MeshFilter = meshFilter;
            Mesh = mesh;
            Material = material;
        }
    }

    public readonly struct StaticMapChunkBatchKey : IEquatable<StaticMapChunkBatchKey>
    {
        public readonly int ChunkX;
        public readonly int ChunkZ;
        public readonly Material Material;
        public readonly int LightmapIndex;
        public readonly int Layer;
        public readonly ShadowCastingMode ShadowCastingMode;
        public readonly bool ReceiveShadows;
        public readonly LightProbeUsage LightProbeUsage;
        public readonly ReflectionProbeUsage ReflectionProbeUsage;

        public StaticMapChunkBatchKey(
            int chunkX,
            int chunkZ,
            Material material,
            int lightmapIndex,
            int layer,
            ShadowCastingMode shadowCastingMode,
            bool receiveShadows,
            LightProbeUsage lightProbeUsage,
            ReflectionProbeUsage reflectionProbeUsage)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            Material = material;
            LightmapIndex = lightmapIndex;
            Layer = layer;
            ShadowCastingMode = shadowCastingMode;
            ReceiveShadows = receiveShadows;
            LightProbeUsage = lightProbeUsage;
            ReflectionProbeUsage = reflectionProbeUsage;
        }

        public bool Equals(StaticMapChunkBatchKey other)
        {
            return ChunkX == other.ChunkX &&
                   ChunkZ == other.ChunkZ &&
                   ReferenceEquals(Material, other.Material) &&
                   LightmapIndex == other.LightmapIndex &&
                   Layer == other.Layer &&
                   ShadowCastingMode == other.ShadowCastingMode &&
                   ReceiveShadows == other.ReceiveShadows &&
                   LightProbeUsage == other.LightProbeUsage &&
                   ReflectionProbeUsage == other.ReflectionProbeUsage;
        }

        public override bool Equals(object obj)
        {
            return obj is StaticMapChunkBatchKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ChunkX;
                hash = (hash * 397) ^ ChunkZ;
                hash = (hash * 397) ^ RuntimeHelpers.GetHashCode(Material);
                hash = (hash * 397) ^ LightmapIndex;
                hash = (hash * 397) ^ Layer;
                hash = (hash * 397) ^ (int)ShadowCastingMode;
                hash = (hash * 397) ^ (ReceiveShadows ? 1 : 0);
                hash = (hash * 397) ^ (int)LightProbeUsage;
                hash = (hash * 397) ^ (int)ReflectionProbeUsage;
                return hash;
            }
        }
    }

    public static class StaticMapChunkBatchingPolicy
    {
        public const string CombinedRootName = "RuntimeStaticMapBatches";
        public const float ChunkSize = 96f;
        public const float MaxSourceExtent = 80f;
        public const int MaxSourceVertices = 8000;
        public const int MaxBatchVertices = 55000;
        public const int MaxBatchRenderers = 64;
        public const int MinBatchRenderers = 2;

        public static StaticMapChunkSourceEvaluation EvaluateSource(
            MeshRenderer renderer,
            Transform combinedRoot,
            Transform mapBuildingAuthoringRoot,
            Transform mapVehicleAuthoringRoot,
            Transform decorationRoot)
        {
            StaticMapChunkRendererSafety safety = ClassifyRendererSafety(
                renderer,
                combinedRoot,
                mapBuildingAuthoringRoot,
                mapVehicleAuthoringRoot,
                decorationRoot);
            switch (safety)
            {
                case StaticMapChunkRendererSafety.UnavailableOrInactive:
                    return new StaticMapChunkSourceEvaluation(StaticMapChunkSourceEligibility.UnavailableOrInactive);
                case StaticMapChunkRendererSafety.GeneratedOutput:
                case StaticMapChunkRendererSafety.ExcludedAuthoringHierarchy:
                    return new StaticMapChunkSourceEvaluation(StaticMapChunkSourceEligibility.ExcludedHierarchy);
                case StaticMapChunkRendererSafety.StaticBatch:
                case StaticMapChunkRendererSafety.LodGroup:
                case StaticMapChunkRendererSafety.UnsupportedComponents:
                    return new StaticMapChunkSourceEvaluation(StaticMapChunkSourceEligibility.Unsafe);
            }

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (mesh == null || mesh.vertexCount == 0)
            {
                return new StaticMapChunkSourceEvaluation(
                    StaticMapChunkSourceEligibility.MissingMesh,
                    meshFilter,
                    mesh);
            }

            if (!mesh.isReadable)
            {
                return new StaticMapChunkSourceEvaluation(
                    StaticMapChunkSourceEligibility.UnreadableMesh,
                    meshFilter,
                    mesh);
            }

            if (mesh.vertexCount > MaxSourceVertices || IsLargeRenderer(renderer))
            {
                return new StaticMapChunkSourceEvaluation(
                    StaticMapChunkSourceEligibility.TooLarge,
                    meshFilter,
                    mesh);
            }

            if (mesh.subMeshCount != 1 || renderer.sharedMaterials.Length != 1 || renderer.sharedMaterial == null)
            {
                return new StaticMapChunkSourceEvaluation(
                    StaticMapChunkSourceEligibility.UnsupportedMaterialLayout,
                    meshFilter,
                    mesh);
            }

            return new StaticMapChunkSourceEvaluation(
                StaticMapChunkSourceEligibility.Eligible,
                meshFilter,
                mesh,
                renderer.sharedMaterial);
        }

        public static StaticMapChunkRendererSafety ClassifyRendererSafety(
            MeshRenderer renderer,
            Transform combinedRoot,
            Transform mapBuildingAuthoringRoot,
            Transform mapVehicleAuthoringRoot,
            Transform decorationRoot)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                return StaticMapChunkRendererSafety.UnavailableOrInactive;
            if (combinedRoot != null && renderer.transform.IsChildOf(combinedRoot))
                return StaticMapChunkRendererSafety.GeneratedOutput;
            if (IsInRoot(renderer.transform, mapBuildingAuthoringRoot) ||
                IsInRoot(renderer.transform, mapVehicleAuthoringRoot) ||
                IsInRoot(renderer.transform, decorationRoot))
            {
                return StaticMapChunkRendererSafety.ExcludedAuthoringHierarchy;
            }

            if (renderer.isPartOfStaticBatch)
                return StaticMapChunkRendererSafety.StaticBatch;
            if (renderer.GetComponentInParent<LODGroup>() != null)
                return StaticMapChunkRendererSafety.LodGroup;
            if (!HasOnlySafeComponents(renderer.gameObject))
                return StaticMapChunkRendererSafety.UnsupportedComponents;

            return StaticMapChunkRendererSafety.Safe;
        }

        public static StaticMapChunkBatchKey CreateBatchKey(MeshRenderer renderer, Material material)
        {
            Vector3 center = renderer.bounds.center;
            return new StaticMapChunkBatchKey(
                GetChunkCoordinate(center.x),
                GetChunkCoordinate(center.z),
                material,
                renderer.lightmapIndex,
                renderer.gameObject.layer,
                renderer.shadowCastingMode,
                renderer.receiveShadows,
                renderer.lightProbeUsage,
                renderer.reflectionProbeUsage);
        }

        public static int GetChunkCoordinate(float worldPosition)
        {
            return Mathf.FloorToInt(worldPosition / ChunkSize);
        }

        private static bool IsInRoot(Transform transform, Transform root)
        {
            return root != null && transform != null && transform.IsChildOf(root);
        }

        private static bool HasOnlySafeComponents(GameObject gameObject)
        {
            Component[] components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                    return false;
                if (component is Transform ||
                    component is MeshFilter ||
                    component is MeshRenderer ||
                    component is Collider)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static bool IsLargeRenderer(Renderer renderer)
        {
            Vector3 size = renderer.bounds.size;
            return size.x > MaxSourceExtent || size.y > MaxSourceExtent || size.z > MaxSourceExtent;
        }
    }
}
