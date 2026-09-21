#if UNITY_EDITOR
using System.IO;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishGroundStagingPrefabBuilder
    {
        public const string PrefabPath = "Assets/Game/Prefabs/Skirmish/Building_GroundStaging.prefab";

        [MenuItem("Tools/Warline/Skirmish/Rebuild Ground Staging")]
        public static void RebuildMenu() => Debug.Log(Rebuild());

        public static string Rebuild()
        {
            Directory.CreateDirectory("Assets/Game/Prefabs/Skirmish");
            GameObject hierarchy = SkirmishGroundStagingBuilder.BuildHierarchy();
            hierarchy.hideFlags = HideFlags.None;
            hierarchy.SetActive(true);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(hierarchy, PrefabPath);
            Object.DestroyImmediate(hierarchy);
            if (prefab == null || !SkirmishGroundStagingBuilder.HasRequiredSurfaces(prefab))
                return "[SkirmishGroundStagingPrefabBuilder] result=Failed";
            return "[SkirmishGroundStagingPrefabBuilder] result=Passed path=" + PrefabPath;
        }
    }
}
#endif
