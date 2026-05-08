#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WarlineCaptureChapter01TacticalScaleContractUtility
{
    public const string AssetPath = "Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_scale_contract.asset";

    public static Chapter01TacticalScaleContract LoadOrCreate()
    {
        Chapter01TacticalScaleContract contract = AssetDatabase.LoadAssetAtPath<Chapter01TacticalScaleContract>(AssetPath);
        if (contract == null)
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(AssetPath)));
            contract = ScriptableObject.CreateInstance<Chapter01TacticalScaleContract>();
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
