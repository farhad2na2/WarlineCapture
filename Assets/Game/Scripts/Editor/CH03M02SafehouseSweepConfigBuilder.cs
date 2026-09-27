using System;
using System.IO;
using Game.Configs;
using Game.Missions.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M02SafehouseSweepConfigBuilder
    {
        public const string MissionId = CampaignMissionSequence.SafehouseSweep;
        public const string ScenarioId = "scenario.ch03.m02.safehouse_sweep";
        public const string MapId = "opmap.ch03.safehouse_sweep_01";
        public const string MissionPath = "Assets/Game/Configs/Missions/Chapter03/MissionDefinition_Ch03_M02_SafehouseSweep.asset";
        public const string ScenarioPath = "Assets/Game/Configs/Scenarios/Chapter03/ScenarioSetup_Ch03_M02_SafehouseSweep.asset";
        public const string MapPath = "Assets/Game/Configs/OperationMaps/Chapter03/OperationMap_Ch03_SafehouseSweep01.asset";

        [MenuItem("Game/Campaign/Safehouse Sweep/Build Configuration")]
        public static void Build()
        {
            foreach (string category in new[] {"Missions", "Scenarios", "OperationMaps"}) Directory.CreateDirectory("Assets/Game/Configs/" + category + "/Chapter03");
            AssetDatabase.Refresh(); BuildMap(); BuildScenario(); BuildMission(); M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            Require(MissionDefinitionContractValidation.TryValidateCatalog(Load<MissionDefinitionCatalogConfig>(M01FirstContactConfigBuilder.CatalogPath), out string error), error);
            AssetDatabase.SaveAssets(); Debug.Log("[SafehouseSweepConfig] result=Passed chapter=3 mission=2 fallback=authored-perimeter-and-ledger controls=Select,Move,Attack,Hold");
        }

        private static void BuildMap()
        {
            OperationMapDefinition map = Clone<OperationMapDefinition>(CH03M01SignalTraceConfigBuilder.MapPath, MapPath); var data = new SerializedObject(map);
            ReplaceStrings(data, "ch03.m01", "ch03.m02"); ReplaceStrings(data, "signal_trace", "safehouse_sweep"); Set(data, "operationMapId", MapId); Set(data, "planningCameraId", "camera.ch03.m02.overview"); Set(data, "battleCameraId", "camera.ch03.m02.battle");
            var anchors = data.FindProperty("anchors");
            for (int i = 0; i < anchors.arraySize; i++)
            {
                var anchor = anchors.GetArrayElementAtIndex(i); string id = anchor.FindPropertyRelative("anchorId").stringValue; Vector3 position = anchor.FindPropertyRelative("position").vector3Value;
                if (id.EndsWith(".manifest", StringComparison.Ordinal)) anchor.FindPropertyRelative("position").vector3Value = new Vector3(694f, position.y, 438f);
                else if (id.EndsWith(".corrupt_manifest", StringComparison.Ordinal)) anchor.FindPropertyRelative("position").vector3Value = new Vector3(720f, position.y, 452f);
                else if (id.EndsWith(".delivery", StringComparison.Ordinal)) anchor.FindPropertyRelative("position").vector3Value = new Vector3(735f, position.y, 421f);
            }
            data.ApplyModifiedPropertiesWithoutUndo(); Require(map.TryValidateMetadata(out string error) && map.TryValidateLocalContentReferences(out error), error); EditorUtility.SetDirty(map);
        }

        private static void BuildScenario()
        {
            ScenarioSetupConfig scenario = Clone<ScenarioSetupConfig>(CH03M01SignalTraceConfigBuilder.ScenarioPath, ScenarioPath); var data = new SerializedObject(scenario);
            ReplaceStrings(data, "ch03.m01", "ch03.m02"); ReplaceStrings(data, "signal_trace", "safehouse_sweep"); Set(data, "scenarioId", ScenarioId); Set(data, "operationMapId", MapId); Set(data, "deterministicSeed", 3002001); data.ApplyModifiedPropertiesWithoutUndo();
            Require(scenario.TryValidate(out string error), error); EditorUtility.SetDirty(scenario);
        }

        private static void BuildMission()
        {
            MissionDefinitionConfig mission = Clone<MissionDefinitionConfig>(CH03M01SignalTraceConfigBuilder.MissionPath, MissionPath); var data = new SerializedObject(mission);
            Set(data, "missionId", MissionId); Set(data, "scenarioId", ScenarioId); Set(data, "operationMapId", MapId); Set(data, "displayNameKey", "mission.safehouse_sweep.name"); Set(data, "displaySummaryKey", "mission.safehouse_sweep.summary"); Set(data, "locationNameKey", "mission.safehouse_sweep.location");
            Set(data, "briefingSequenceId", "seq.ch03.m02.brief"); Set(data, "commsSequenceId", "seq.ch03.m02.comms"); Set(data, "debriefSequenceId", "seq.ch03.m02.debrief");
            string[] ids = {"confirm", "protect", "ledger"}; MissionObjectiveRuleKind[] rules = {MissionObjectiveRuleKind.VerifyMarketManifest, MissionObjectiveRuleKind.DeliverMarketRelief, MissionObjectiveRuleKind.KeepMarketOpen}; int[] counts = {2, 3, 1}; string[] roles = {"role.market.manifest", "role.market.relief_convoy", "role.market.trade"};
            var objectives = data.FindProperty("objectives"); objectives.arraySize = 3;
            for (int i = 0; i < 3; i++) { var objective = objectives.GetArrayElementAtIndex(i); Set(objective, "objectiveId", "obj.ch03.m02." + ids[i]); Set(objective, "displayTextKey", "mission.safehouse_sweep.objective." + ids[i]); Set(objective, "rule", (int)rules[i]); Set(objective, "missionRoleId", roles[i]); Set(objective, "targetConfigId", string.Empty); Set(objective, "requiredCount", counts[i]); Set(objective, "failureOnRuleBreak", true); }
            var stars = data.FindProperty("stars"); for (int i = 0; i < stars.arraySize; i++) Set(stars.GetArrayElementAtIndex(i), "displayTextKey", "mission.safehouse_sweep.star." + (i + 1));
            var rewards = data.FindProperty("firstClearRewards"); if (rewards.arraySize > 0) Set(rewards.GetArrayElementAtIndex(0), "amount", 1200); if (rewards.arraySize > 1) Set(rewards.GetArrayElementAtIndex(1), "amount", 6000);
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
