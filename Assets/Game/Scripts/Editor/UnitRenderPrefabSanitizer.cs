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
