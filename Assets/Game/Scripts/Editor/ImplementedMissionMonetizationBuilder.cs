using System;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class ImplementedMissionMonetizationBuilder
    {
        public static void ApplyRewardCopy()
        {
            M05BreachAssaultPresentationBuilder.ImportCopy();
            AssetDatabase.SaveAssets();
            Debug.Log("[ImplementedMissionRewardCopy] result=Passed M05=teaser-removed legacy-parts=preserved");
        }
        public static void ApplyEconomy()
        {
            Apply(M02EstablishBaseConfigBuilder.ScenarioPath, 120);
            Apply(M03RadarWarningConfigBuilder.ScenarioPath, 140);
            Apply(CH04M01AirCorridorConfigBuilder.ScenarioPath, 140);
            Apply(CH04M02SteelPushConfigBuilder.ScenarioPath, 200);
            AssetDatabase.SaveAssets();
            Debug.Log("[ImplementedMissionEconomyAssets] result=Passed missions=4 credits=0 materials=120,140,140,200");
        }

        private static void Apply(string path, int materials)
        {
            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(path);
            if (scenario == null) throw new InvalidOperationException(path);
            var data = new SerializedObject(scenario);
            var runtime = data.FindProperty("missionRuntime");
            runtime.FindPropertyRelative("startingCredits").intValue = 0;
            runtime.FindPropertyRelative("startingMaterials").intValue = materials;
            data.ApplyModifiedPropertiesWithoutUndo();
            if (!scenario.TryValidate(out string error)) throw new InvalidOperationException(error);
            EditorUtility.SetDirty(scenario);
        }
    }
}
