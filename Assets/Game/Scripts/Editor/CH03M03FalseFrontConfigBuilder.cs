using System;
using System.IO;
using Game.Configs;
using Game.Missions.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M03FalseFrontConfigBuilder
    {
        public const string MissionId = CampaignMissionSequence.FalseFront;
        public const string ScenarioId = "scenario.ch03.m03.false_front";
        public const string MapId = "opmap.ch03.false_front_01";
        public const string MissionPath = "Assets/Game/Configs/Missions/Chapter03/MissionDefinition_Ch03_M03_FalseFront.asset";
        public const string ScenarioPath = "Assets/Game/Configs/Scenarios/Chapter03/ScenarioSetup_Ch03_M03_FalseFront.asset";
        public const string MapPath = "Assets/Game/Configs/OperationMaps/Chapter03/OperationMap_Ch03_FalseFront01.asset";

        [MenuItem("Game/Campaign/False Front/Build Configuration")]
        public static void Build()
        {
            foreach (string category in new[] {"Missions", "Scenarios", "OperationMaps"}) Directory.CreateDirectory("Assets/Game/Configs/" + category + "/Chapter03");
            AssetDatabase.Refresh(); BuildMap(); BuildScenario(); BuildMission(); M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            Require(MissionDefinitionContractValidation.TryValidateCatalog(Load<MissionDefinitionCatalogConfig>(M01FirstContactConfigBuilder.CatalogPath), out string error), error);
            AssetDatabase.SaveAssets(); Debug.Log("[FalseFrontConfig] result=Passed chapter=3 mission=3 fallback=authored-evacuation-checkpoints controls=Select,Move,Attack,Hold");
        }

        private static void BuildMap()
        {
            OperationMapDefinition map = Clone<OperationMapDefinition>(CH03M02SafehouseSweepConfigBuilder.MapPath, MapPath); var data = new SerializedObject(map);
            ReplaceStrings(data, "ch03.m02", "ch03.m03"); ReplaceStrings(data, "safehouse_sweep", "false_front");
            Set(data, "operationMapId", MapId); Set(data, "planningCameraId", "camera.ch03.m03.overview"); Set(data, "battleCameraId", "camera.ch03.m03.battle");
            var anchors = data.FindProperty("anchors");
            for (int i = 0; i < anchors.arraySize; i++)
            {
                var anchor = anchors.GetArrayElementAtIndex(i); string id = anchor.FindPropertyRelative("anchorId").stringValue; Vector3 position = anchor.FindPropertyRelative("position").vector3Value;
                if (id.EndsWith(".manifest", StringComparison.Ordinal)) anchor.FindPropertyRelative("position").vector3Value = new Vector3(696f, position.y, 438f);
                else if (id.EndsWith(".corrupt_manifest", StringComparison.Ordinal)) anchor.FindPropertyRelative("position").vector3Value = new Vector3(720f, position.y, 450f);
                else if (id.EndsWith(".delivery", StringComparison.Ordinal)) anchor.FindPropertyRelative("position").vector3Value = new Vector3(746f, position.y, 420f);
                else if (id.EndsWith(".hostile_a", StringComparison.Ordinal)) anchor.FindPropertyRelative("position").vector3Value = new Vector3(756f, position.y, 460f);
                else if (id.EndsWith(".hostile_b", StringComparison.Ordinal)) anchor.FindPropertyRelative("position").vector3Value = new Vector3(738f, position.y, 470f);
            }
            data.ApplyModifiedPropertiesWithoutUndo(); Require(map.TryValidateMetadata(out string error) && map.TryValidateLocalContentReferences(out error), error); EditorUtility.SetDirty(map);
        }

        private static void BuildScenario()
        {
            ScenarioSetupConfig scenario = Clone<ScenarioSetupConfig>(CH03M02SafehouseSweepConfigBuilder.ScenarioPath, ScenarioPath); var data = new SerializedObject(scenario);
            ReplaceStrings(data, "ch03.m02", "ch03.m03"); ReplaceStrings(data, "safehouse_sweep", "false_front");
            Set(data, "scenarioId", ScenarioId); Set(data, "operationMapId", MapId); Set(data, "deterministicSeed", 3003001);
            var market = data.FindProperty("marketLifeline"); Set(market, "manifestHoldMilliseconds", 5000); Set(market, "victoryHoldMilliseconds", 10000); Set(market, "deadlineMilliseconds", 540000);
            var routes = data.FindProperty("patrolRoutes"); for (int i = 0; i < routes.arraySize; i++) Set(routes.GetArrayElementAtIndex(i), "startDelayMilliseconds", i == 0 ? 65000 : 90000);
            data.ApplyModifiedPropertiesWithoutUndo(); Require(scenario.TryValidate(out string error), error); EditorUtility.SetDirty(scenario);
        }

        private static void BuildMission()
        {
            MissionDefinitionConfig mission = Clone<MissionDefinitionConfig>(CH03M02SafehouseSweepConfigBuilder.MissionPath, MissionPath); var data = new SerializedObject(mission);
            Set(data, "missionId", MissionId); Set(data, "scenarioId", ScenarioId); Set(data, "operationMapId", MapId);
            Set(data, "displayNameKey", "mission.false_front.name"); Set(data, "displaySummaryKey", "mission.false_front.summary"); Set(data, "locationNameKey", "mission.false_front.location");
            Set(data, "briefingSequenceId", "seq.ch03.m03.brief"); Set(data, "commsSequenceId", "seq.ch03.m03.comms"); Set(data, "debriefSequenceId", "seq.ch03.m03.debrief");
            string[] ids = {"verify", "evacuate", "ambush"}; MissionObjectiveRuleKind[] rules = {MissionObjectiveRuleKind.VerifyMarketManifest, MissionObjectiveRuleKind.DeliverMarketRelief, MissionObjectiveRuleKind.KeepMarketOpen}; int[] counts = {2, 3, 1}; string[] roles = {"role.market.manifest", "role.market.relief_convoy", "role.market.trade"};
            var objectives = data.FindProperty("objectives"); objectives.arraySize = 3;
            for (int i = 0; i < 3; i++) { var objective = objectives.GetArrayElementAtIndex(i); Set(objective, "objectiveId", "obj.ch03.m03." + ids[i]); Set(objective, "displayTextKey", "mission.false_front.objective." + ids[i]); Set(objective, "rule", (int)rules[i]); Set(objective, "missionRoleId", roles[i]); Set(objective, "targetConfigId", string.Empty); Set(objective, "requiredCount", counts[i]); Set(objective, "failureOnRuleBreak", true); }
            var stars = data.FindProperty("stars"); for (int i = 0; i < stars.arraySize; i++) Set(stars.GetArrayElementAtIndex(i), "displayTextKey", "mission.false_front.star." + (i + 1));
            var rewards = data.FindProperty("firstClearRewards"); if (rewards.arraySize > 0) Set(rewards.GetArrayElementAtIndex(0), "amount", 1300); if (rewards.arraySize > 1) Set(rewards.GetArrayElementAtIndex(1), "amount", 6500);
            data.ApplyModifiedPropertiesWithoutUndo(); Require(MissionDefinitionContractValidation.TryValidateDefinition(mission, out string error), error); EditorUtility.SetDirty(mission);
        }

        private static void ReplaceStrings(SerializedObject data, string from, string to) { var property = data.GetIterator(); if (!property.Next(true)) return; do { if (property.propertyType == SerializedPropertyType.String && property.stringValue.IndexOf(from, StringComparison.Ordinal) >= 0) property.stringValue = property.stringValue.Replace(from, to); } while (property.Next(true)); }
        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException(path);
        private static T Clone<T>(string source, string target) where T : ScriptableObject { T asset = AssetDatabase.LoadAssetAtPath<T>(target); if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, target); } EditorUtility.CopySerialized(Load<T>(source), asset); asset.name = Path.GetFileNameWithoutExtension(target); return asset; }
        private static void Set(SerializedObject data, string field, object value) => Set(data.FindProperty(field), value);
        private static void Set(SerializedProperty parent, string field, object value) => Set(parent.FindPropertyRelative(field), value);
        private static void Set(SerializedProperty property, object value) { switch (value) { case string text: property.stringValue = text; break; case int number: property.intValue = number; break; case bool flag: property.boolValue = flag; break; default: throw new ArgumentException(property.propertyPath); } }
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
    }
}
