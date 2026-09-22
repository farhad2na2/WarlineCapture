#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishMapLayoutBuilder
    {
        public const string SharedLayoutPath = "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishLayout_DB_BA.asset";
        public const string ScenarioLayoutPath = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishLayout_S002.asset";

        [MenuItem("Tools/Warline/Skirmish/Rebuild Desert Base Layout")]
        public static void RebuildMenu() => Debug.Log(RebuildDesertBaseAssault());

        public static string RebuildDesertBaseAssault()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Author measured layouts in Edit mode.");

            SkirmishExpansionAuthoredSet set = SkirmishExpansionCatalogFactory.CreateInMemory();
            var reasons = new List<SkirmishCompileReason>();
            if (!SkirmishMapLayoutValidation.TryValidateDesertBaseAssault(set.LayoutDbBa, reasons))
                throw new InvalidOperationException(reasons.Count == 0 ? "layout validation failed" : reasons[0].ToString());

            Persist(set.LayoutDbBa, SharedLayoutPath);
            var scenarioCopy = ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            EditorUtility.CopySerialized(set.LayoutDbBa, scenarioCopy);
            Persist(scenarioCopy, ScenarioLayoutPath);
            AssetDatabase.SaveAssets();
            return "[SkirmishMapLayoutBuilder] result=Passed layout=layout.skirmish.db.ba";
        }

        private static T Persist<T>(T asset, string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }

            EditorUtility.CopySerialized(asset, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
#endif
