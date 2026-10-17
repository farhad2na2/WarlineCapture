#if UNITY_EDITOR
using System;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishMissionViewProfileAuthoring
    {
        public static void CreateS005EngineeringCandidate()
        {
            const string definitionPath = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset";
            const string output = "Assets/Game/Resources/SkirmishMissionViewProfile_S005.asset";
            var definition = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(definitionPath);
            var candidate = SkirmishMissionViewProfile.CreateS005EngineeringCandidate(definition);
            candidate.name = "SkirmishMissionViewProfile_S005";
            try
            {
                var existing = AssetDatabase.LoadAssetAtPath<SkirmishMissionViewProfile>(output);
                if (existing != null)
                {
                    if (JsonUtility.ToJson(existing) != JsonUtility.ToJson(candidate))
                        throw new InvalidOperationException("Preserve existing authored/tuned mission profile; candidate differs.");
                    Debug.Log("[SkirmishMissionViewProfileAuthoring] result=Unchanged catalog=S005 engineeringTargetsOnly=1");
                    return;
                }
                AssetDatabase.CreateAsset(candidate, output); candidate = null;
                AssetDatabase.SaveAssets();
                Debug.Log("[SkirmishMissionViewProfileAuthoring] result=Created catalog=S005 engineeringTargetsOnly=1 nativeVisualAccepted=0 path=" + output);
            }
            finally { if (candidate != null) UnityEngine.Object.DestroyImmediate(candidate); }
        }
    }
}
#endif
