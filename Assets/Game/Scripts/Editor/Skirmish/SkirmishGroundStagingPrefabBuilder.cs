#if UNITY_EDITOR
using System.IO;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishGroundStagingPrefabBuilder
    {
        public const string PrefabPath = SkirmishGroundStagingPrefabAccess.PrefabPath;

        [MenuItem("Tools/Warline/Skirmish/Rebuild Ground Staging")]
        public static void RebuildMenu() => Debug.Log(Rebuild());

        public static string Rebuild()
        {
            Directory.CreateDirectory("Assets/Game/Prefabs/Skirmish");
            PersistAuthoredConfig();
            GameObject hierarchy = SkirmishGroundStagingBuilder.BuildHierarchy(
                SkirmishGroundStagingPrefabAccess.LoadAuthoredConfig());
            hierarchy.hideFlags = HideFlags.None;
            hierarchy.SetActive(true);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(hierarchy, PrefabPath);
            Object.DestroyImmediate(hierarchy);
            if (prefab == null || !SkirmishGroundStagingBuilder.HasRequiredSurfaces(prefab))
                return "[SkirmishGroundStagingPrefabBuilder] result=Failed";
            AssetDatabase.ImportAsset(PrefabPath);
            return "[SkirmishGroundStagingPrefabBuilder] result=Passed path=" + PrefabPath;
        }

        private static void PersistAuthoredConfig()
        {
            SkirmishGroundStagingConfig config =
                AssetDatabase.LoadAssetAtPath<SkirmishGroundStagingConfig>(
                    SkirmishGroundStagingPrefabAccess.AuthoredConfigPath);
            if (config != null)
                return;
            config = ScriptableObject.CreateInstance<SkirmishGroundStagingConfig>();
            config.ConfigureEstablished();
            AssetDatabase.CreateAsset(config, SkirmishGroundStagingPrefabAccess.AuthoredConfigPath);
        }
    }
}
#endif
