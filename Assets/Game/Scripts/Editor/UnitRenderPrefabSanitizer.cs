using UnityEditor;
using UnityEngine;
using SnivelerCode.GpuAnimation.Scripts.Authoring;

public static class UnitRenderPrefabSanitizer
{
    private static readonly string[] TargetFolders =
    {
        "Assets/Game/Prefabs/Generated/MidLOD"
    };

    public static void SanitizeUnitRenderPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", TargetFolders);
        int updated = 0;
        int removedLodGroups = 0;
        int disabledDynamicOcclusion = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                continue;

            bool changed = false;
            try
            {
                if (root.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true) != null)
                    continue;

                LODGroup[] lodGroups = root.GetComponentsInChildren<LODGroup>(true);
                for (int j = 0; j < lodGroups.Length; j++)
                {
                    Object.DestroyImmediate(lodGroups[j], true);
                    removedLodGroups++;
                    changed = true;
                }

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                {
                    if (!renderers[j].allowOcclusionWhenDynamic)
                        continue;

                    renderers[j].allowOcclusionWhenDynamic = false;
                    disabledDynamicOcclusion++;
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    updated++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[UnitRenderSanitizer] updated={updated} removedLodGroups={removedLodGroups} disabledDynamicOcclusion={disabledDynamicOcclusion}");
    }

    [MenuItem("Game/Tools/Unit Render/Remove Inherited Placeholder Character Models")]
    public static void RemoveInheritedPlaceholderCharacterModels()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Prefabs/Characters" });
        int updated = 0;
        int removed = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            if (path.EndsWith("/Unit.prefab", System.StringComparison.Ordinal))
                continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                continue;

            bool changed = false;
            try
            {
                for (int childIndex = root.transform.childCount - 1; childIndex >= 0; childIndex--)
                {
                    Transform child = root.transform.GetChild(childIndex);
                    if (child == null ||
                        !string.Equals(child.name, "Model", System.StringComparison.Ordinal) ||
                        child.GetComponent<MeshRenderer>() == null ||
                        child.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true) != null)
                    {
                        continue;
                    }

                    Object.DestroyImmediate(child.gameObject, true);
                    removed++;
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    updated++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[UnitRenderSanitizer] removedPlaceholderCharacterModels={removed} updated={updated}");
    }

    [MenuItem("Game/Tools/Unit Render/Validate Character Variant Models")]
    public static void ValidateCharacterVariantModels()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Prefabs/Characters" });
        int validated = 0;
        int failures = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            if (path.EndsWith("/Unit.prefab", System.StringComparison.Ordinal))
                continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                continue;

            try
            {
                int modelChildren = 0;
                int animatedModelChildren = 0;
                int placeholderModelChildren = 0;

                for (int childIndex = 0; childIndex < root.transform.childCount; childIndex++)
                {
                    Transform child = root.transform.GetChild(childIndex);
                    if (child == null || !string.Equals(child.name, "Model", System.StringComparison.Ordinal))
                        continue;

                    modelChildren++;
                    bool hasAnimatorIndex = child.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true) != null;
                    bool hasDirectMeshRenderer = child.GetComponent<MeshRenderer>() != null;
                    if (hasAnimatorIndex)
                        animatedModelChildren++;
                    else if (hasDirectMeshRenderer)
                        placeholderModelChildren++;
                }

                if (modelChildren != 1 || animatedModelChildren != 1 || placeholderModelChildren != 0)
                {
                    Debug.LogError($"[UnitRenderSanitizer] invalidCharacterModel path={path} modelChildren={modelChildren} animatedModelChildren={animatedModelChildren} placeholderModelChildren={placeholderModelChildren}");
                    failures++;
                }
                else
                {
                    validated++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        Debug.Log($"[UnitRenderSanitizer] validatedCharacterVariantModels={validated} failures={failures}");
        if (failures > 0)
            throw new System.InvalidOperationException($"Character variant model validation failed: {failures}");
    }

    [MenuItem("Game/Tools/Unit Render/Align Character Model Feet To Root")]
    public static void AlignCharacterModelFeetToRoot()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Prefabs/Characters" });
        int updated = 0;
        int aligned = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            if (path.EndsWith("/Unit.prefab", System.StringComparison.Ordinal))
                continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                continue;

            bool changed = false;
            try
            {
                Transform model = FindAnimatedModelChild(root.transform);
                if (model == null || !TryGetLocalBounds(root.transform, model, out Bounds bounds))
                    continue;

                Vector3 localPosition = model.localPosition;
                float desiredOffset = localPosition.y - bounds.min.y;
                if (Mathf.Abs(bounds.min.y) <= 0.001f)
                    desiredOffset = localPosition.y;

                if (!Mathf.Approximately(localPosition.y, desiredOffset))
                {
                    localPosition.y = desiredOffset;
                    model.localPosition = localPosition;
                    aligned++;
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    updated++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[UnitRenderSanitizer] alignedCharacterModelFeet={aligned} updated={updated}");
    }

    private static Transform FindAnimatedModelChild(Transform root)
    {
        for (int childIndex = 0; childIndex < root.childCount; childIndex++)
        {
            Transform child = root.GetChild(childIndex);
            if (child != null &&
                string.Equals(child.name, "Model", System.StringComparison.Ordinal) &&
                child.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true) != null)
            {
                return child;
            }
        }

        return null;
    }

    private static bool TryGetLocalBounds(Transform root, Transform model, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Matrix4x4 rootWorldToLocal = root.worldToLocalMatrix;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Bounds localBounds = renderer.localBounds;
            Matrix4x4 rendererToRoot = rootWorldToLocal * renderer.transform.localToWorldMatrix;
            Encapsulate(rendererToRoot.MultiplyPoint3x4(localBounds.min), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(localBounds.max), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(new Vector3(localBounds.min.x, localBounds.min.y, localBounds.max.z)), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(new Vector3(localBounds.min.x, localBounds.max.y, localBounds.min.z)), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(new Vector3(localBounds.max.x, localBounds.min.y, localBounds.min.z)), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(new Vector3(localBounds.min.x, localBounds.max.y, localBounds.max.z)), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(new Vector3(localBounds.max.x, localBounds.min.y, localBounds.max.z)), ref bounds, ref hasBounds);
            Encapsulate(rendererToRoot.MultiplyPoint3x4(new Vector3(localBounds.max.x, localBounds.max.y, localBounds.min.z)), ref bounds, ref hasBounds);
        }

        return hasBounds;
    }

    private static void Encapsulate(Vector3 point, ref Bounds bounds, ref bool hasBounds)
    {
        if (!hasBounds)
        {
            bounds = new Bounds(point, Vector3.zero);
            hasBounds = true;
            return;
        }

        bounds.Encapsulate(point);
    }

    public static void RestoreCharacterBakedLodGroups()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Prefabs/Generated/CharactersBaked" });
        int updated = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                continue;

            bool changed = false;
            try
            {
                MaterialAnimatorIndexAuthoring indexAuthoring = root.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true);
                if (indexAuthoring == null)
                    continue;

                LODGroup lodGroup = root.GetComponent<LODGroup>();
                if (lodGroup == null)
                {
                    lodGroup = root.AddComponent<LODGroup>();
                    changed = true;
                }

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    lodGroup.SetLODs(new[] { new LOD(0f, renderers) });
                    lodGroup.fadeMode = LODFadeMode.None;
                    lodGroup.animateCrossFading = false;
                    lodGroup.RecalculateBounds();
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    updated++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[UnitRenderSanitizer] restoredCharacterLodGroups={updated}");
    }

    public static void RestoreAnimatedMidLodGroups()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Prefabs/Generated/MidLOD" });
        int updated = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                continue;

            bool changed = false;
            try
            {
                MaterialAnimatorIndexAuthoring indexAuthoring = root.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true);
                if (indexAuthoring == null)
                    continue;

                LODGroup lodGroup = root.GetComponent<LODGroup>();
                if (lodGroup == null)
                {
                    lodGroup = root.AddComponent<LODGroup>();
                    changed = true;
                }

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    lodGroup.SetLODs(new[] { new LOD(0f, renderers) });
                    lodGroup.fadeMode = LODFadeMode.None;
                    lodGroup.animateCrossFading = false;
                    lodGroup.RecalculateBounds();
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    updated++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[UnitRenderSanitizer] restoredAnimatedMidLodGroups={updated}");
    }
}
