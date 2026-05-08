using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class CombinedMeshUtility
{
    public sealed class CombinedMeshResult
    {
        public string Name;
        public Material Material;
        public Mesh Mesh;
    }

    public static bool TryBuildCombinedMeshes(
        Transform root,
        List<CombinedMeshResult> results,
        Transform excludedRoot = null,
        bool includeInactive = true)
    {
        results?.Clear();
        if (root == null || results == null)
            return false;

        var renderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive);
        var combineByMaterial = new Dictionary<Material, List<CombineInstance>>();
        var materialOrder = new List<Material>();
        Matrix4x4 rootWorldToLocal = root.worldToLocalMatrix;

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
                continue;
            if (excludedRoot != null && renderer.transform.IsChildOf(excludedRoot))
                continue;

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (mesh == null)
                continue;
            if (!mesh.isReadable)
            {
                Debug.LogWarning($"CombinedMeshUtility skipped unreadable mesh '{mesh.name}' on '{renderer.name}'. Enable Read/Write on the model import settings to combine it at runtime.", renderer);
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            int subMeshCount = Mathf.Min(mesh.subMeshCount, materials.Length);
            Matrix4x4 localToRoot = rootWorldToLocal * renderer.transform.localToWorldMatrix;

            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
            {
                Material material = materials[subMeshIndex];
                if (material == null)
                    continue;

                if (!combineByMaterial.TryGetValue(material, out List<CombineInstance> combines))
                {
                    combines = new List<CombineInstance>();
                    combineByMaterial.Add(material, combines);
                    materialOrder.Add(material);
                }

                combines.Add(new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = subMeshIndex,
                    transform = localToRoot
                });
            }
        }

        for (int i = 0; i < materialOrder.Count; i++)
        {
            Material material = materialOrder[i];
            Mesh combinedMesh = new Mesh
            {
                name = $"{root.name}_{material.name}_Combined_{i}"
            };
            combinedMesh.indexFormat = IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combineByMaterial[material].ToArray(), true, true, false);
            combinedMesh.RecalculateBounds();

            results.Add(new CombinedMeshResult
            {
                Name = material.name,
                Material = material,
                Mesh = combinedMesh
            });
        }

        return results.Count > 0;
    }
}
