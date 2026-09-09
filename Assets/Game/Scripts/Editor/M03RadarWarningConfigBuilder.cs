using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningConfigBuilder
    {
        public const string MissionId = "saga.ch01.m03.radar_warning";
        public const string ScenarioId = "scenario.ch01.m03.radar_warning";
        public const string MissionPath = "Assets/Game/Configs/Missions/Chapter01/MissionDefinition_Ch01_M03_RadarWarning.asset";
        public const string ScenarioPath = "Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M03_RadarWarning.asset";
        public const string AnchorPrefix = M03RadarWarningMapBuilder.Prefix;
        public const string GroupPrefix = "group.ch01.m03.";
        public const string RoutePrefix = "route.ch01.m03.";

        [MenuItem("Game/Campaign/M03/Build Canonical Data")]
        public static void Build()
        {
            M03RadarWarningMapBuilder.Build();
            BuildScenario(); BuildMission();
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            MissionDefinitionCatalogConfig catalog = Load<MissionDefinitionCatalogConfig>(M02EstablishBaseConfigBuilder.MissionCatalogPath);
            Require(MissionDefinitionContractValidation.TryValidateCatalog(catalog,out string error),error);
            Require(catalog.TryResolve(MissionId,out MissionDefinitionConfig mission) && mission != null,"M03 catalog missing.");
            OperationMapCatalogConfig maps = Load<OperationMapCatalogConfig>(M02EstablishBaseConfigBuilder.OperationMapCatalogPath);
            Require(maps.TryValidate(out error),error);
            Debug.Log($"[M03RadarWarningConfigBuilder] result=Passed missions={catalog.Entries.Length} maps={maps.Definitions.Length} hostiles=7 civilians=4 rifles=8 sensor=1");
        }

        private static void BuildScenario()
        {
            ScenarioSetupConfig scenario = Clone<ScenarioSetupConfig>(M02EstablishBaseConfigBuilder.ScenarioPath, ScenarioPath);
            OperationMapDefinition map = Load<OperationMapDefinition>(M03RadarWarningMapBuilder.Path);
            SerializedObject data = new(scenario);
            Set(data,"scenarioId",ScenarioId); Set(data,"operationMapId",M03RadarWarningMapBuilder.MapId);
            Set(data,"deterministicSeed",3003001); Set(data,"encounterStartMilliseconds",45000);
            var routeAnchors = new List<string>();
            SerializedProperty anchors = data.FindProperty("requiredAnchors"); anchors.arraySize = map.Anchors.Length;
            for (int i = 0; i < map.Anchors.Length; i++)
            {
                var anchor = map.Anchors[i]; SerializedProperty entry = anchors.GetArrayElementAtIndex(i);
                Set(entry,"anchorId",anchor.AnchorId); Set(entry,"kind",(int)anchor.Kind);
                if (anchor.AnchorId.StartsWith(AnchorPrefix+"convoy_path_",StringComparison.Ordinal)) routeAnchors.Add(anchor.AnchorId);
            }
            Array(data.FindProperty("unitGroups"),6,PopulateGroup);
            Array(data.FindProperty("ambientPresentations"),0,null);
            Array(data.FindProperty("patrolRoutes"),2,(route,index) =>
            {
                string name = index == 0 ? "vanguard" : "main_body";
                Set(route,"routeId",RoutePrefix+name); Set(route,"unitGroupId",GroupPrefix+name);
                Set(route,"startDelayMilliseconds",index == 0 ? 45000 : 140000);
                Strings(route.FindPropertyRelative("anchorIds"),routeAnchors);
            });
            SerializedProperty runtime = data.FindProperty("missionRuntime");
            Set(runtime,"startingCredits",50000); Set(runtime,"startingMaterials",100);
            Set(runtime,"baseAnchorId",AnchorPrefix+"forward_post");
            SerializedProperty buildZone = runtime.FindPropertyRelative("buildZone");
            Set(buildZone,"anchorId",AnchorPrefix+"build_zone"); Set(buildZone,"halfWidthCells",105); Set(buildZone,"halfHeightCells",65);
            string[] buildingIds = {"Building_Barrack","Building_GuardTower","Building_Road_Barrier"};
            Array(runtime.FindPropertyRelative("buildCatalog"),3,(entry,index) =>
            { Set(entry,"buildingConfigId",buildingIds[index]); Set(entry,"maxCount",index+1); });
            SerializedProperty oldWave = runtime.FindPropertyRelative("delayedWave");
            foreach (string name in new[]{"unitGroupId","routeId","targetMissionRoleId"}) Set(oldWave,name,string.Empty);
            Set(oldWave,"warningAtMilliseconds",0); Set(oldWave,"activationAtMilliseconds",0);
            SerializedProperty defense = data.FindProperty("defense");
            Set(defense,"enabled",true); Set(defense,"innerCoreAnchorId",AnchorPrefix+"inner_core");
            Set(defense,"vehiclesSelfSupplied",true);
            Set(defense,"authoredMapDefensesDormant",true);
            Set(defense,"forwardPostStableId",M03RadarWarningMapBuilder.ForwardPostStableId);
            defense.FindPropertyRelative("innerCoreRadius").floatValue = 6f;
            Set(defense,"sensorMissionRoleId","role.friendly.ground_sensor");
            Set(defense,"initialProducerAnchorId",AnchorPrefix+"initial_barracks");
            Set(defense,"radarPingCharges",2); Set(defense,"radarPingCooldownMilliseconds",60000);
            PopulateGuidance(defense);
            var tour=defense.FindPropertyRelative("cameraTour");
            Set(tour,"StartHoldMilliseconds",750); Set(tour,"PostHoldMilliseconds",1000);
            Set(tour,"ApproachHoldMilliseconds",1500); Set(tour,"ReturnHoldMilliseconds",500);
            tour.FindPropertyRelative("SmoothTimeSeconds").floatValue=.65f;
            tour.FindPropertyRelative("PostPerspective").vector4Value=new Vector4(55,65,0,55);
            tour.FindPropertyRelative("ApproachPerspective").vector4Value=new Vector4(50,67,0,55);
            Array(defense.FindPropertyRelative("convoyElements"),2,(element,index) =>
            {
                string name = index == 0 ? "vanguard" : "main_body";
                Set(element,"elementId","convoy.ch01.m03."+name); Set(element,"unitGroupId",GroupPrefix+name);
                Set(element,"routeId",RoutePrefix+name); Set(element,"contactAnchorId",AnchorPrefix+"contact");
                Set(element,"warningAtMilliseconds",index == 0 ? 0 : 100000);
                Set(element,"activationAtMilliseconds",index == 0 ? 45000 : 140000);
                Set(element,"contactAtMilliseconds",index == 0 ? 65000 : 165000);
            });
            data.ApplyModifiedPropertiesWithoutUndo();
            Require(scenario.TryValidate(out string error),error);
            EditorUtility.SetDirty(scenario); AssetDatabase.SaveAssets();
        }

        private static void BuildMission()
        {
            MissionDefinitionConfig mission = Clone<MissionDefinitionConfig>(M02EstablishBaseConfigBuilder.MissionPath,MissionPath);
            SerializedObject data = new(mission);
            Set(data,"missionId",MissionId); Set(data,"scenarioId",ScenarioId); Set(data,"operationMapId",M03RadarWarningMapBuilder.MapId);
            Set(data,"displayNameKey","mission.m03.name"); Set(data,"displaySummaryKey","mission.m03.summary"); Set(data,"locationNameKey","mission.m03.location");
            Set(data,"briefingSequenceId","seq.ch01.m03.brief"); Set(data,"commsSequenceId","seq.ch01.m03.comms"); Set(data,"debriefSequenceId","seq.ch01.m03.debrief");
            string[] names = {"stop_convoy","protect_post","prevent_breach"};
            MissionObjectiveRuleKind[] rules = {MissionObjectiveRuleKind.DestroyMissionRole,MissionObjectiveRuleKind.DefendMissionRole,MissionObjectiveRuleKind.PreventCoreBreach};
            Array(data.FindProperty("objectives"),3,(entry,index) =>
            {
                Set(entry,"objectiveId","obj.ch01.m03."+names[index]); Set(entry,"displayTextKey","mission.m03.objective."+names[index]);
                Set(entry,"rule",(int)rules[index]); Set(entry,"missionRoleId",index == 0 ? "role.hostile.convoy" : "role.friendly.forward_post");
                Set(entry,"targetConfigId",string.Empty); Set(entry,"requiredCount",index == 0 ? 7 : 1); Set(entry,"failureOnRuleBreak",index != 0);
            });
            MissionStarRuleKind[] stars = {MissionStarRuleKind.CompleteMission,MissionStarRuleKind.NoCivilianLoss,MissionStarRuleKind.NoPostDamage};
            string[] starNames = {"complete","civilians_safe","post_undamaged"};
            Array(data.FindProperty("stars"),3,(entry,index) =>
            { Set(entry,"starIndex",index+1); Set(entry,"rule",(int)stars[index]); Set(entry,"displayTextKey","mission.m03.star."+starNames[index]); Set(entry,"threshold",0); });
            string[] rewards = {"reward.commander_xp",string.Empty,"reward.ch01.m03.guard_tower_unlock","reward.ch01.m03.radar_ping_unlock"};
            string[] keys = {"mission.reward.commander_xp","mission.reward.credits","mission.m03.reward.guard_tower","mission.m03.reward.radar_ping"};
            int[] amounts = {400,2000,1,1};
            Array(data.FindProperty("firstClearRewards"),4,(entry,index) =>
            { Set(entry,"kind",index == 1 ? 1 : 0); Set(entry,"rewardConfigId",rewards[index]); Set(entry,"displayTextKey",keys[index]); Set(entry,"amount",amounts[index]); });
            data.ApplyModifiedPropertiesWithoutUndo();
            Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);
            EditorUtility.SetDirty(mission); AssetDatabase.SaveAssets();
        }

        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException(path);
        private static T Clone<T>(string basis, string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset,path); }
            EditorUtility.CopySerialized(Load<T>(basis),asset); return asset;
        }
        private static void Require(bool value,string error) { if (!value) throw new InvalidOperationException(error); }
        private static void Array(SerializedProperty array,int count,Action<SerializedProperty,int> fill)
        { array.arraySize = count; for (int i=0;i<count;i++) fill(array.GetArrayElementAtIndex(i),i); }
        private static void Strings(SerializedProperty array,IReadOnlyList<string> values)
        { Array(array,values.Count,(entry,index) => entry.stringValue = values[index]); }
        private static void Set(SerializedObject target,string name,object value) => Set(target.FindProperty(name),value);
        private static void Set(SerializedProperty target,string name,object value) => Set(target.FindPropertyRelative(name),value);
        private static void Set(SerializedProperty property,object value)
        {
            switch(value) { case string s: property.stringValue=s; break; case int i: property.intValue=i; break; case bool b: property.boolValue=b; break; default: throw new ArgumentException(property.propertyPath); }
        }
    }
}
