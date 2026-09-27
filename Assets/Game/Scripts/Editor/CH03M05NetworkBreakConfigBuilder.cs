using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Game.Missions.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M05NetworkBreakConfigBuilder
    {
        public const string MissionId = CampaignMissionSequence.NetworkBreak;
        public const string ScenarioId = "scenario.ch03.m05.network_break";
        public const string MapId = "opmap.ch03.network_break_01";
        public const string MissionPath = "Assets/Game/Configs/Missions/Chapter03/MissionDefinition_Ch03_M05_NetworkBreak.asset";
        public const string ScenarioPath = "Assets/Game/Configs/Scenarios/Chapter03/ScenarioSetup_Ch03_M05_NetworkBreak.asset";
        public const string MapPath = "Assets/Game/Configs/OperationMaps/Chapter03/OperationMap_Ch03_NetworkBreak01.asset";

        [MenuItem("Game/Campaign/Network Break/Build Configuration")]
        public static void Build()
        {
            foreach (string category in new[] {"Missions", "Scenarios", "OperationMaps"})
                Directory.CreateDirectory("Assets/Game/Configs/" + category + "/Chapter03");
            AssetDatabase.Refresh();
            BuildMap(); BuildScenario(); BuildMission();
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            var catalog = AssetDatabase.LoadAssetAtPath<MissionDefinitionCatalogConfig>(M01FirstContactConfigBuilder.CatalogPath);
            Require(catalog != null, "Mission catalog missing");
            Require(MissionDefinitionContractValidation.TryValidateCatalog(catalog, out string error), error);
            AssetDatabase.SaveAssets();
            Debug.Log("[NetworkBreakConfig] result=Passed chapter=3 mission=5 gate=controlled core=verified archive=preserved");
        }

        private static void BuildMap()
        {
            var map = Clone<OperationMapDefinition>(M05BreachAssaultConfigBuilder.MapPath, MapPath);
            var data = new SerializedObject(map);
            ReplaceStrings(data);
            Set(data, "operationMapId", MapId);
            Set(data, "planningCameraId", "camera.ch03.m05.planning");
            Set(data, "battleCameraId", "camera.ch03.m05.battle");
            using var sha = SHA256.Create();
            string hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes("CH03M05-NetworkBreak-v1:" + map.ContentHash)))
                .Replace("-", string.Empty).ToLowerInvariant();
            Set(data, "contentHash", hash); Set(data, "generatedMetadataHash", hash);
            data.ApplyModifiedPropertiesWithoutUndo();
            Require(map.TryValidateMetadata(out string error) && map.TryValidateLocalContentReferences(out error), error);
            EditorUtility.SetDirty(map);
        }

        private static void BuildScenario()
        {
            var scenario = Clone<ScenarioSetupConfig>(M05BreachAssaultConfigBuilder.ScenarioPath, ScenarioPath);
            var data = new SerializedObject(scenario);
            ReplaceStrings(data);
            Set(data, "scenarioId", ScenarioId); Set(data, "operationMapId", MapId);
            Set(data, "deterministicSeed", 3005001);
            var breach = data.FindProperty("breach");
            Set(breach, "secureHoldMilliseconds", 15000);
            Set(breach, "deadlineMilliseconds", 660000);
            Set(breach, "counterattackWarningMilliseconds", 10000);
            data.ApplyModifiedPropertiesWithoutUndo();
            Require(scenario.TryValidate(out string error), error);
            EditorUtility.SetDirty(scenario);
        }

        private static void BuildMission()
        {
            var mission = Clone<MissionDefinitionConfig>(M05BreachAssaultConfigBuilder.MissionPath, MissionPath);
            var data = new SerializedObject(mission);
            ReplaceStrings(data);
            Set(data, "missionId", MissionId); Set(data, "scenarioId", ScenarioId); Set(data, "operationMapId", MapId);
            Set(data, "displayNameKey", "mission.network_break.name");
            Set(data, "displaySummaryKey", "mission.network_break.summary");
            Set(data, "locationNameKey", "mission.network_break.location");
            foreach (string stage in new[] {"brief", "comms", "debrief"})
                Set(data, (stage == "brief" ? "briefing" : stage) + "SequenceId", "seq.ch03.m05." + stage);
            string[] objectives = {"breach", "disable", "archive"};
            var items = data.FindProperty("objectives");
            for (int i = 0; i < items.arraySize; i++)
            {
                Set(items.GetArrayElementAtIndex(i), "objectiveId", "obj.ch03.m05." + objectives[i]);
                Set(items.GetArrayElementAtIndex(i), "displayTextKey", "mission.network_break.objective." + objectives[i]);
            }
            var stars = data.FindProperty("stars");
            for (int i = 0; i < stars.arraySize; i++)
                Set(stars.GetArrayElementAtIndex(i), "displayTextKey", "mission.network_break.star." + (i + 1));
            var rewards = data.FindProperty("firstClearRewards");
            rewards.arraySize = 2;
            Set(rewards.GetArrayElementAtIndex(0), "rewardConfigId", "reward.commander_xp");
            Set(rewards.GetArrayElementAtIndex(0), "displayTextKey", "mission.reward.commander_xp");
            Set(rewards.GetArrayElementAtIndex(0), "amount", 1500);
            Set(rewards.GetArrayElementAtIndex(1), "rewardConfigId", string.Empty);
            Set(rewards.GetArrayElementAtIndex(1), "displayTextKey", "mission.reward.credits");
            Set(rewards.GetArrayElementAtIndex(1), "amount", 7000);
            data.ApplyModifiedPropertiesWithoutUndo();
            Require(MissionDefinitionContractValidation.TryValidateDefinition(mission, out string error), error);
            EditorUtility.SetDirty(mission);
        }

        private static T Clone<T>(string source, string target) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(target);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, target); }
            var basis = AssetDatabase.LoadAssetAtPath<T>(source) ?? throw new InvalidOperationException(source);
            EditorUtility.CopySerialized(basis, asset);
            asset.name = Path.GetFileNameWithoutExtension(target);
            return asset;
        }
        private static void ReplaceStrings(SerializedObject data)
        {
            var property = data.GetIterator();
            if (!property.Next(true)) return;
            do
            {
                if (property.propertyType != SerializedPropertyType.String) continue;
                property.stringValue = property.stringValue.Replace("ch01.m05", "ch03.m05")
                    .Replace("ch01.breach_assault", "ch03.network_break")
                    .Replace("breach_assault", "network_break");
            } while (property.Next(true));
        }
        private static void Set(SerializedObject data, string field, object value) => Set(data.FindProperty(field), value);
        private static void Set(SerializedProperty data, string field, object value) => Set(data.FindPropertyRelative(field), value);
        private static void Set(SerializedProperty property, object value)
        {
            if (property == null) throw new InvalidOperationException("Missing Network Break serialized field");
            switch (value)
            {
                case string text: property.stringValue = text; break;
                case int number: property.intValue = number; break;
                default: throw new ArgumentException(property.propertyPath);
            }
        }
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
    }
}
