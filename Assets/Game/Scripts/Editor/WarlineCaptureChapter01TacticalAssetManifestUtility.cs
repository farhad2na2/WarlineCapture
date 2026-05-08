#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WarlineCaptureChapter01TacticalAssetManifestUtility
{
    public const string AssetPath = "Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_asset_manifest.asset";

    [MenuItem("WarlineCapture/Design/Build Chapter01 Tactical Asset Manifest")]
    public static void BuildOrRefresh()
    {
        LoadOrCreate();
        Debug.Log($"WARLINECAPTURE_CH01_TACTICAL_ASSET_MANIFEST_BUILT asset={AssetPath}");
    }

    public static Chapter01TacticalAssetManifest LoadOrCreate()
    {
        Chapter01TacticalAssetManifest manifest = AssetDatabase.LoadAssetAtPath<Chapter01TacticalAssetManifest>(AssetPath);
        if (manifest == null)
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(AssetPath)));
            manifest = ScriptableObject.CreateInstance<Chapter01TacticalAssetManifest>();
            manifest.ConfigureDefaults();
            AssetDatabase.CreateAsset(manifest, AssetPath);
            AssetDatabase.SaveAssets();
            return manifest;
        }

        manifest.ConfigureDefaults();
        EditorUtility.SetDirty(manifest);
        AssetDatabase.SaveAssets();
        return manifest;
    }
}
#endif
