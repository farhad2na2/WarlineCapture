using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;

public sealed class ProBuilderShapeBakerWindow : EditorWindow
{
    private const string DefaultOutputFolder = "Assets/Game/BakedMeshes/ProBuilder";

    private GameObject _root;
    private string _outputFolder = DefaultOutputFolder;
    private bool _includeInactive = true;
    private bool _removeProBuilderComponents = true;

    [MenuItem("Tools/Game/ProBuilder Shape Baker")]
    private static void OpenWindow()
    {
        var window = GetWindow<ProBuilderShapeBakerWindow>("ProBuilder Shape Baker");
        window.minSize = new Vector2(430f, 220f);
    }

    private void OnEnable()
    {
        if (_root == null)
            _root = Selection.activeGameObject;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Bake ProBuilder shapes into regular MeshFilter assets.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(8f);

        using (new EditorGUILayout.HorizontalScope())
        {
            _root = (GameObject)EditorGUILayout.ObjectField("Root", _root, typeof(GameObject), true);
            if (GUILayout.Button("Use Selection", GUILayout.Width(110f)))
                _root = Selection.activeGameObject;
        }

        _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
        _includeInactive = EditorGUILayout.ToggleLeft("Include Inactive Children", _includeInactive);
        _removeProBuilderComponents = EditorGUILayout.ToggleLeft("Remove ProBuilder Components After Bake", _removeProBuilderComponents);

        EditorGUILayout.Space(10f);
        EditorGUILayout.HelpBox(
            "Bake all ProBuilderMesh objects under the selected root. The tool saves duplicated mesh assets, keeps the existing MeshRenderer, and can strip ProBuilder components after bake.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(_root == null))
        {
            if (GUILayout.Button("Bake ProBuilder Shapes", GUILayout.Height(32f)))
                Bake();
        }
    }

    private void Bake()
    {
        if (_root == null)
        {
            EditorUtility.DisplayDialog("ProBuilder Shape Baker", "Select a GameObject or prefab root first.", "OK");
            return;
        }

        if (!IsAssetsFolder(_outputFolder))
        {
            EditorUtility.DisplayDialog("Invalid Output Folder", "Output folder must be inside the Unity Assets folder.", "OK");
            return;
        }

        EnsureFolderExists(_outputFolder);

        string assetPath = AssetDatabase.GetAssetPath(_root);
        bool isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(_root) && !string.IsNullOrEmpty(assetPath);

        int bakedCount = isPrefabAsset
            ? BakePrefabAsset(assetPath)
            : BakeHierarchy(_root);

        AssetDatabase.SaveAssets();
        Debug.Log(
            bakedCount > 0
                ? $"ProBuilder Shape Baker: baked {bakedCount} ProBuilder shape(s) under '{_root.name}'."
                : $"ProBuilder Shape Baker: no ProBuilderMesh objects were found under '{_root.name}'.",
            _root);
    }

    private int BakePrefabAsset(string prefabPath)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            int bakedCount = BakeHierarchy(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            return bakedCount;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private int BakeHierarchy(GameObject root)
    {
        var meshes = root.GetComponentsInChildren<ProBuilderMesh>(_includeInactive);
        if (meshes == null || meshes.Length == 0)
            return 0;

        int bakedCount = 0;
        foreach (ProBuilderMesh proBuilderMesh in meshes)
        {
            if (proBuilderMesh == null)
                continue;

            if (BakeSingleMesh(proBuilderMesh))
                bakedCount++;
        }

        if (!Application.isPlaying)
        {
            if (EditorUtility.IsPersistent(root))
                EditorUtility.SetDirty(root);
            else if (root.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(root.scene);
        }

        return bakedCount;
    }

    private bool BakeSingleMesh(ProBuilderMesh proBuilderMesh)
    {
        proBuilderMesh.ToMesh();
        proBuilderMesh.Refresh();

        GameObject go = proBuilderMesh.gameObject;
        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning($"Skipping '{go.name}' because no baked mesh was generated.", go);
            return false;
        }

        Mesh bakedMesh = Object.Instantiate(meshFilter.sharedMesh);
        bakedMesh.name = $"{go.name}_Baked";
        string meshPath = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/{SanitizeFileName(bakedMesh.name)}.asset");
        AssetDatabase.CreateAsset(bakedMesh, meshPath);

        Undo.RecordObject(meshFilter, "Assign Baked Mesh");
        meshFilter.sharedMesh = bakedMesh;
        EditorUtility.SetDirty(meshFilter);

        if (_removeProBuilderComponents)
        {
            RemoveShapeComponent(go);

            Undo.DestroyObjectImmediate(proBuilderMesh);
        }

        return true;
    }

    private static bool IsAssetsFolder(string folder)
    {
        return !string.IsNullOrWhiteSpace(folder) && folder.Replace("\\", "/").StartsWith("Assets/");
    }

    private static void EnsureFolderExists(string folder)
    {
        string normalized = folder.Replace("\\", "/");
        string[] parts = normalized.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
            return;

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
        var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (invalidChars.Contains(chars[i]))
                chars[i] = '_';
        }

        return new string(chars);
    }

    private static void RemoveShapeComponent(GameObject go)
    {
        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            var type = component.GetType();
            if (type.FullName == "UnityEngine.ProBuilder.Shapes.ProBuilderShape")
            {
                Undo.DestroyObjectImmediate(component);
                return;
            }
        }
    }
}
