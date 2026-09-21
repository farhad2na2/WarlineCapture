using UnityEngine;

namespace Game.Configs
{
    public static class SkirmishGroundStagingPrefabAccess
    {
        public const string PrefabPath = "Assets/Game/Prefabs/Skirmish/Building_GroundStaging.prefab";
        public const string ResourcesKey = "Skirmish/Building_GroundStaging";
        public const string AuthoredConfigPath = "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishGroundStaging.asset";

        public static bool TryLoadAuthored(out GameObject prefab, out bool owned)
        {
            owned = false;
            prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (IsUsable(prefab))
                return true;
#endif
            prefab = Resources.Load<GameObject>(ResourcesKey);
            if (IsUsable(prefab))
                return true;

            SkirmishGroundStagingConfig config = LoadAuthoredConfig();
            prefab = SkirmishGroundStagingBuilder.BuildHierarchy(config);
            owned = prefab != null;
            return owned;
        }

        public static SkirmishGroundStagingConfig LoadAuthoredConfig()
        {
#if UNITY_EDITOR
            SkirmishGroundStagingConfig authored =
                UnityEditor.AssetDatabase.LoadAssetAtPath<SkirmishGroundStagingConfig>(AuthoredConfigPath);
            if (authored != null)
                return authored;
#endif
            SkirmishGroundStagingConfig[] loaded = Resources.FindObjectsOfTypeAll<SkirmishGroundStagingConfig>();
            return loaded != null && loaded.Length > 0 ? loaded[0] : null;
        }

        public static bool IsUsable(GameObject prefab)
        {
            return prefab != null && SkirmishGroundStagingBuilder.HasRequiredSurfaces(prefab);
        }
    }
}
