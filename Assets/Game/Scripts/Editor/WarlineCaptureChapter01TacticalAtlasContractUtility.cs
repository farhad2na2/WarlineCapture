#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WarlineCaptureChapter01TacticalAtlasContractUtility
{
    public const string AssetPath = "Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_atlas_contract.asset";

    [MenuItem("WarlineCapture/Design/Build Chapter01 Tactical Atlas Contract")]
    public static void BuildOrRefresh()
    {
        LoadOrCreate();
        Debug.Log($"WARLINECAPTURE_CH01_TACTICAL_ATLAS_CONTRACT_BUILT asset={AssetPath}");
    }

    public static Chapter01TacticalAtlasContract LoadOrCreate()
    {
        Chapter01TacticalAtlasContract contract = AssetDatabase.LoadAssetAtPath<Chapter01TacticalAtlasContract>(AssetPath);
        if (contract == null)
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(AssetPath)));
            contract = ScriptableObject.CreateInstance<Chapter01TacticalAtlasContract>();
            contract.ConfigureDefaults();
            AssetDatabase.CreateAsset(contract, AssetPath);
            AssetDatabase.SaveAssets();
            return contract;
        }

        contract.ConfigureDefaults();
        EditorUtility.SetDirty(contract);
        AssetDatabase.SaveAssets();
        return contract;
    }
}
#endif
