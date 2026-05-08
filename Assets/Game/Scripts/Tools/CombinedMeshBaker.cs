using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CombinedMeshBaker : MonoBehaviour
{
    [SerializeField] private bool combineOnStart;
    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool disableSourceRenderers = true;
    [SerializeField] private bool destroySourceObjectsAtRuntime;
    [SerializeField] private string combinedRootName = "CombinedMesh";
    [SerializeField] private string bakedAssetFolder = "Assets/Game/GeneratedCombinedMeshes";
    [SerializeField] private Transform _combinedRoot;
    [SerializeField] private List<Mesh> _bakedMeshes = new();

    private readonly List<Mesh> _runtimeMeshes = new();

    public bool IncludeInactive => includeInactive;
    public bool DisableSourceRenderers => disableSourceRenderers;
    public string CombinedRootName => combinedRootName;
    public string BakedAssetFolder => bakedAssetFolder;
    public Transform CombinedRoot => _combinedRoot;
    public IReadOnlyList<Mesh> BakedMeshes => _bakedMeshes;

    private void Start()
    {
        if (combineOnStart)
            CombineAtRuntime();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _runtimeMeshes.Count; i++)
        {
            if (_runtimeMeshes[i] != null)
                Destroy(_runtimeMeshes[i]);
        }

        _runtimeMeshes.Clear();
    }

    public void CombineAtRuntime()
    {
        for (int i = 0; i < _runtimeMeshes.Count; i++)
        {
            if (_runtimeMeshes[i] != null)
                Destroy(_runtimeMeshes[i]);
        }

        _runtimeMeshes.Clear();
        ClearCombinedRootImmediate();

        var results = new List<CombinedMeshUtility.CombinedMeshResult>();
        if (!CombinedMeshUtility.TryBuildCombinedMeshes(transform, results, _combinedRoot, includeInactive))
            return;

        EnsureCombinedRoot();
        BuildCombinedHierarchy(results, true);

        if (destroySourceObjectsAtRuntime)
            DestroySourceObjects();
        else
            SetSourceRenderersEnabled(!disableSourceRenderers);
    }

#if UNITY_EDITOR
    public void SetCombinedRoot(Transform combinedRoot)
    {
        _combinedRoot = combinedRoot;
    }

    public void SetBakedMeshes(List<Mesh> meshes)
    {
        _bakedMeshes = meshes ?? new List<Mesh>();
    }

    public void ClearBakedMeshReferences()
    {
        _bakedMeshes.Clear();
    }
#endif

    private void EnsureCombinedRoot()
    {
        if (_combinedRoot != null)
            return;

        var combinedObject = new GameObject(string.IsNullOrWhiteSpace(combinedRootName) ? "CombinedMesh" : combinedRootName);
        combinedObject.transform.SetParent(transform, false);
        combinedObject.transform.localPosition = Vector3.zero;
        combinedObject.transform.localRotation = Quaternion.identity;
        combinedObject.transform.localScale = Vector3.one;
        _combinedRoot = combinedObject.transform;
    }

    private void ClearCombinedRootImmediate()
    {
        if (_combinedRoot == null)
            return;

        for (int i = _combinedRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = _combinedRoot.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private void BuildCombinedHierarchy(List<CombinedMeshUtility.CombinedMeshResult> results, bool trackRuntimeMeshes)
    {
        for (int i = 0; i < results.Count; i++)
        {
            CombinedMeshUtility.CombinedMeshResult result = results[i];
            var child = new GameObject($"{result.Name}_Combined");
            child.transform.SetParent(_combinedRoot, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            var meshFilter = child.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = result.Mesh;

            var meshRenderer = child.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = result.Material;

            if (trackRuntimeMeshes && result.Mesh != null)
                _runtimeMeshes.Add(result.Mesh);
        }
    }

    private void SetSourceRenderersEnabled(bool enabled)
    {
        var renderers = GetComponentsInChildren<MeshRenderer>(includeInactive);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
                continue;
            if (_combinedRoot != null && renderer.transform.IsChildOf(_combinedRoot))
                continue;

            renderer.enabled = enabled;
        }
    }

    private void DestroySourceObjects()
    {
        var childrenToDestroy = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null || child == _combinedRoot)
                continue;

            childrenToDestroy.Add(child.gameObject);
        }

        for (int i = 0; i < childrenToDestroy.Count; i++)
            Destroy(childrenToDestroy[i]);
    }
}
