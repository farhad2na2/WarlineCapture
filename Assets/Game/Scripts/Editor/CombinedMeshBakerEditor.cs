#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(CombinedMeshBaker))]
public sealed class CombinedMeshBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8f);

        var baker = (CombinedMeshBaker)target;
        if (GUILayout.Button("Bake Combined Mesh"))
            BakeInEditor(baker);

        if (GUILayout.Button("Clear Baked Mesh"))
            ClearBakedMesh(baker);
    }

    private static void BakeInEditor(CombinedMeshBaker baker)
    {
        if (baker == null)
            return;

        Undo.RecordObject(baker, "Bake Combined Mesh");
        ClearBakedMesh(baker);

        var results = new List<CombinedMeshUtility.CombinedMeshResult>();
        if (!CombinedMeshUtility.TryBuildCombinedMeshes(baker.transform, results, baker.CombinedRoot, baker.IncludeInactive))
        {
            int rendererCount = CountEligibleRenderers(baker);
            int unreadableMeshCount = CountUnreadableMeshes(baker);
            Debug.LogWarning(
                $"CombinedMeshBaker could not build any combined meshes for '{baker.name}'. " +
                $"Found {rendererCount} eligible MeshRenderer(s) and {unreadableMeshCount} unreadable source mesh(es). " +
                "Enable Read/Write on the source model import settings for any unreadable meshes, then reimport and bake again.",
                baker);
            return;
        }

        EnsureFolderExists(baker.BakedAssetFolder);

        Transform combinedRoot = baker.CombinedRoot;
        if (combinedRoot == null)
        {
            var combinedObject = new GameObject(string.IsNullOrWhiteSpace(baker.CombinedRootName) ? "CombinedMesh" : baker.CombinedRootName);
            Undo.RegisterCreatedObjectUndo(combinedObject, "Create Combined Mesh Root");
            combinedObject.transform.SetParent(baker.transform, false);
            combinedObject.transform.localPosition = Vector3.zero;
            combinedObject.transform.localRotation = Quaternion.identity;
            combinedObject.transform.localScale = Vector3.one;
            combinedRoot = combinedObject.transform;
            baker.SetCombinedRoot(combinedRoot);
            PrefabUtility.RecordPrefabInstancePropertyModifications(baker);
        }

        var bakedMeshes = new List<Mesh>();
        for (int i = 0; i < results.Count; i++)
        {
            CombinedMeshUtility.CombinedMeshResult result = results[i];
            string safeName = SanitizeFileName(result.Name);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(baker.BakedAssetFolder, $"{baker.gameObject.name}_{safeName}_Combined.asset").Replace("\\", "/"));
            AssetDatabase.CreateAsset(result.Mesh, assetPath);
            bakedMeshes.Add(result.Mesh);

            var child = new GameObject($"{result.Name}_Combined");
            Undo.RegisterCreatedObjectUndo(child, "Create Combined Mesh Child");
            child.transform.SetParent(combinedRoot, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            var meshFilter = Undo.AddComponent<MeshFilter>(child);
            meshFilter.sharedMesh = result.Mesh;

            var meshRenderer = Undo.AddComponent<MeshRenderer>(child);
            meshRenderer.sharedMaterial = result.Material;
        }

        baker.SetBakedMeshes(bakedMeshes);
        SetSourceRenderersEnabled(baker, !baker.DisableSourceRenderers);
        EditorUtility.SetDirty(baker);
        if (combinedRoot != null)
            EditorUtility.SetDirty(combinedRoot.gameObject);
        MarkOwnerDirty(baker);
        AssetDatabase.SaveAssets();

        Debug.Log($"CombinedMeshBaker baked {results.Count} combined mesh object(s) for '{baker.name}'.", baker);
    }

    private static void ClearBakedMesh(CombinedMeshBaker baker)
    {
        if (baker == null)
            return;

        Transform combinedRoot = baker.CombinedRoot;
        if (combinedRoot != null)
        {
            for (int i = combinedRoot.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(combinedRoot.GetChild(i).gameObject);
        }

        IReadOnlyList<Mesh> bakedMeshes = baker.BakedMeshes;
        for (int i = 0; i < bakedMeshes.Count; i++)
        {
            Mesh mesh = bakedMeshes[i];
            if (mesh == null)
                continue;

            string assetPath = AssetDatabase.GetAssetPath(mesh);
            if (!string.IsNullOrEmpty(assetPath))
                AssetDatabase.DeleteAsset(assetPath);
        }

        baker.ClearBakedMeshReferences();
        SetSourceRenderersEnabled(baker, true);
        EditorUtility.SetDirty(baker);
        MarkOwnerDirty(baker);
        AssetDatabase.SaveAssets();
    }

    private static void SetSourceRenderersEnabled(CombinedMeshBaker baker, bool enabled)
    {
        var renderers = baker.GetComponentsInChildren<MeshRenderer>(baker.IncludeInactive);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
                continue;
            if (baker.CombinedRoot != null && renderer.transform.IsChildOf(baker.CombinedRoot))
                continue;

            Undo.RecordObject(renderer, "Toggle Source Renderer");
            renderer.enabled = enabled;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void EnsureFolderExists(string assetFolder)
    {
        if (string.IsNullOrWhiteSpace(assetFolder))
            return;
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;

        string[] parts = assetFolder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Combined";

        char[] invalidChars = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidChars.Length; i++)
            value = value.Replace(invalidChars[i], '_');

        return value.Replace('/', '_').Replace('\\', '_');
    }

    private static int CountEligibleRenderers(CombinedMeshBaker baker)
    {
        int count = 0;
        var renderers = baker.GetComponentsInChildren<MeshRenderer>(baker.IncludeInactive);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
                continue;
            if (baker.CombinedRoot != null && renderer.transform.IsChildOf(baker.CombinedRoot))
                continue;

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
                count++;
        }

        return count;
    }

    private static int CountUnreadableMeshes(CombinedMeshBaker baker)
    {
        int count = 0;
        var renderers = baker.GetComponentsInChildren<MeshRenderer>(baker.IncludeInactive);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
                continue;
            if (baker.CombinedRoot != null && renderer.transform.IsChildOf(baker.CombinedRoot))
                continue;

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (mesh != null && !mesh.isReadable)
                count++;
        }

        return count;
    }

    private static void MarkOwnerDirty(CombinedMeshBaker baker)
    {
        var root = PrefabUtility.GetOutermostPrefabInstanceRoot(baker.gameObject);
        if (root != null)
            PrefabUtility.RecordPrefabInstancePropertyModifications(root);

        if (PrefabUtility.IsPartOfPrefabAsset(baker.gameObject))
        {
            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(baker.gameObject) ?? baker.gameObject.transform.root.gameObject;
            PrefabUtility.SavePrefabAsset(prefabRoot);
            return;
        }

        if (baker.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(baker.gameObject.scene);
    }
}
#endif
