using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class UnitPrefabRenderAudit
{
    private sealed class AuditRow
    {
        public string PrefabPath;
        public string PrefabName;
        public string DisplayName;
        public int RendererCount;
        public int MeshCount;
        public int MaterialSlots;
        public int VertexCount;
        public int TriangleCount;
        public bool UsesVehicleMotion;
        public bool IsAirUnit;
        public bool HasMidLodPrefab;
    }

    [MenuItem("Game/Tools/Unit Render/Audit All Unit Prefabs")]
    private static void AuditAll()
    {
        LogAudit(LoadAllUnitPrefabs(), "all");
    }

    [MenuItem("Game/Tools/Unit Render/Audit Selected Unit Prefabs")]
    private static void AuditSelected()
    {
        Object[] selected = Selection.GetFiltered(typeof(GameObject), SelectionMode.Assets);
        List<GameObject> prefabs = new(selected.Length);
        for (int i = 0; i < selected.Length; i++)
        {
            GameObject prefab = selected[i] as GameObject;
            if (prefab == null || prefab.GetComponent<UnitGridAuthoring>() == null)
                continue;

            prefabs.Add(prefab);
        }

        if (prefabs.Count == 0)
        {
            Debug.LogWarning("[UnitRenderAudit] No unit prefabs selected.");
            return;
        }

        LogAudit(prefabs, "selected");
    }

    [MenuItem("Game/Tools/Unit Render/Report Missing Unit Visual Root References")]
    private static void ReportMissingVisualRootReferences()
    {
        List<GameObject> prefabs = LoadAllUnitPrefabs();
        StringBuilder sb = new();
        int missingModelRoot = 0;
        int missingDestroyedRoot = 0;

        sb.AppendLine($"[UnitRenderAudit] visualRootReferenceReport count={prefabs.Count}");
        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
            if (authoring == null)
                continue;

            SerializedObject serialized = new(authoring);
            SerializedProperty modelRoot = serialized.FindProperty("modelRoot");
            SerializedProperty destroyedRoot = serialized.FindProperty("destroyedRoot");
            bool hasModelChild = prefab.transform.Find("Model") != null;
            bool hasDestroyedChild = prefab.transform.Find("Destroyed") != null;
            bool hasExplicitModelRoot = modelRoot != null && modelRoot.objectReferenceValue != null;
            bool hasExplicitDestroyedRoot = destroyedRoot != null && destroyedRoot.objectReferenceValue != null;

            if ((!hasModelChild || hasExplicitModelRoot) && (!hasDestroyedChild || hasExplicitDestroyedRoot))
                continue;

            if (hasModelChild && !hasExplicitModelRoot)
                missingModelRoot++;
            if (hasDestroyedChild && !hasExplicitDestroyedRoot)
                missingDestroyedRoot++;

            sb.AppendLine(
                $"{prefab.name} | missingModelRoot={(hasModelChild && !hasExplicitModelRoot ? 1 : 0)} " +
                $"missingDestroyedRoot={(hasDestroyedChild && !hasExplicitDestroyedRoot ? 1 : 0)} " +
                $"path={AssetDatabase.GetAssetPath(prefab)}");
        }

        sb.AppendLine($"[UnitRenderAudit] missingModelRoot={missingModelRoot} missingDestroyedRoot={missingDestroyedRoot}");
        if (missingModelRoot > 0 || missingDestroyedRoot > 0)
            Debug.LogWarning(sb.ToString());
        else
            Debug.Log(sb.ToString());
    }

    private static List<GameObject> LoadAllUnitPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[]
        {
            "Assets/Game/Prefabs/Characters",
            "Assets/Game/Prefabs/Vehicles"
        });
        List<GameObject> prefabs = new(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponent<UnitGridAuthoring>() == null)
                continue;

            prefabs.Add(prefab);
        }

        return prefabs;
    }

    private static void LogAudit(List<GameObject> prefabs, string scope)
    {
        List<AuditRow> rows = new(prefabs.Count);
        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            AuditRow row = BuildRow(prefab);
            if (row != null)
                rows.Add(row);
        }

        rows.Sort((a, b) => b.TriangleCount.CompareTo(a.TriangleCount));

        StringBuilder sb = new();
        sb.AppendLine($"[UnitRenderAudit] scope={scope} count={rows.Count}");
        for (int i = 0; i < rows.Count; i++)
        {
            AuditRow row = rows[i];
            sb.AppendLine(
                $"{row.PrefabName} | display=\"{row.DisplayName}\" | tris={row.TriangleCount} verts={row.VertexCount} " +
                $"renderers={row.RendererCount} meshes={row.MeshCount} materialSlots={row.MaterialSlots} " +
                $"vehicleMotion={(row.UsesVehicleMotion ? 1 : 0)} air={(row.IsAirUnit ? 1 : 0)} midLod={(row.HasMidLodPrefab ? 1 : 0)} path={row.PrefabPath}");
        }

        Debug.Log(sb.ToString());
    }

    private static AuditRow BuildRow(GameObject prefab)
    {
        if (prefab == null)
            return null;

        UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
        if (authoring == null)
            return null;

        Transform modelRoot = ResolveModelRoot(prefab, authoring);

        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        MeshFilter[] meshFilters = modelRoot.GetComponentsInChildren<MeshFilter>(true);
        SkinnedMeshRenderer[] skinnedRenderers = modelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        int materialSlots = 0;
        int vertexCount = 0;
        int triangleCount = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;
            materialSlots += materials != null ? materials.Length : 0;
        }

        for (int i = 0; i < meshFilters.Length; i++)
        {
            Mesh mesh = meshFilters[i] != null ? meshFilters[i].sharedMesh : null;
            if (mesh == null)
                continue;

            vertexCount += mesh.vertexCount;
            triangleCount += SafeTriangleCount(mesh);
        }

        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            Mesh mesh = skinnedRenderers[i] != null ? skinnedRenderers[i].sharedMesh : null;
            if (mesh == null)
                continue;

            vertexCount += mesh.vertexCount;
            triangleCount += SafeTriangleCount(mesh);
        }

        return new AuditRow
        {
            PrefabPath = AssetDatabase.GetAssetPath(prefab),
            PrefabName = prefab.name,
            DisplayName = authoring.ConfiguredDisplayName,
            RendererCount = renderers.Length,
            MeshCount = meshFilters.Length + skinnedRenderers.Length,
            MaterialSlots = materialSlots,
            VertexCount = vertexCount,
            TriangleCount = triangleCount,
            UsesVehicleMotion = authoring.GetConfiguredFootprintCells().x > 1 || authoring.GetConfiguredFootprintCells().y > 1,
            IsAirUnit = authoring.IsAirUnit,
            HasMidLodPrefab = authoring.MidLodPrefab != null
        };
    }

    private static int SafeTriangleCount(Mesh mesh)
    {
        if (mesh == null)
            return 0;

        int triangles = 0;
        int subMeshCount = mesh.subMeshCount;
        for (int i = 0; i < subMeshCount; i++)
            triangles += (int)mesh.GetIndexCount(i) / 3;
        return triangles;
    }

    private static Transform ResolveModelRoot(GameObject prefab, UnitGridAuthoring authoring)
    {
        SerializedObject serialized = new(authoring);
        SerializedProperty modelRoot = serialized.FindProperty("modelRoot");
        if (modelRoot != null && modelRoot.objectReferenceValue is Transform explicitModelRoot)
            return explicitModelRoot;

        Transform child = prefab.transform.Find("Model");
        return child != null ? child : prefab.transform;
    }
}
