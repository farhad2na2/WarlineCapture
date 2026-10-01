using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime.Pathfinding;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class CH02M05RouteReopenedConfigBuilder
    {
        public const string MissionId=CampaignMissionSequence.RouteReopened,ScenarioId="scenario.ch02.m05.route_reopened",MapId="opmap.ch02.route_reopened_port_review",Prefix="anchor.ch02.m05.";
        public const string MissionPath="Assets/Game/Configs/Missions/Chapter02/MissionDefinition_Ch02_M05_RouteReopened.asset";
        public const string ScenarioPath="Assets/Game/Configs/Scenarios/Chapter02/ScenarioSetup_Ch02_M05_RouteReopened.asset";
        public const string LegacyMapPath="Assets/Game/Configs/OperationMaps/Chapter02/OperationMap_Ch02_RouteReopened01.asset";
        public const string MapPath="Assets/Game/Configs/OperationMaps/Chapter02/OperationMap_Ch02M05_PortReview.asset";
        [MenuItem("Game/Campaign/Route Reopened/Build Configuration")]
        public static void Build()
        {
            foreach(string category in new[]{"Missions","Scenarios","OperationMaps"})Directory.CreateDirectory("Assets/Game/Configs/"+category+"/Chapter02");
            AssetDatabase.Refresh();BuildPortMap();BuildScenario();BuildMission();M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            Require(MissionDefinitionContractValidation.TryValidateCatalog(Load<MissionDefinitionCatalogConfig>(M01FirstContactConfigBuilder.CatalogPath),out string error),error);
            AssetDatabase.SaveAssets();Debug.Log("[RouteReopenedConfig] result=Passed chapter=2 mission=5 controls=Select,Move,Attack,Hold");
        }
        private static void BuildScenario()
        {
            var scenario=Clone<ScenarioSetupConfig>(CH02M03MarketLifelineConfigBuilder.ScenarioPath,ScenarioPath);var data=new SerializedObject(scenario);S(data,"scenarioId",ScenarioId);S(data,"operationMapId",MapId);S(data,"deterministicSeed",2005001);S(data,"encounterStartMilliseconds",0);
            var map=Load<OperationMapDefinition>(MapPath);A(data.FindProperty("requiredAnchors"),map.Anchors.Length,(e,i)=>{S(e,"anchorId",map.Anchors[i].AnchorId);S(e,"kind",(int)map.Anchors[i].Kind);});
            foreach(string key in new[]{"buildingDisabled","productionDisabled","economyDisabled","transportDisabled","airDisabled"})S(data.FindProperty("restrictions"),key,true);
            foreach(string mode in new[]{"missionRuntime","defense","extraction","breach","gridlock","supplyLine","marketLifeline","powerRelay"})S(data.FindProperty(mode),"enabled",false);
            A(data.FindProperty("ambientPresentations"),0,null);A(data.FindProperty("unitGroups"),7,Group);A(data.FindProperty("patrolRoutes"),0,null);
            var route=data.FindProperty("routeReopened");S(route,"enabled",true);S(route,"reliefGoalAnchorId",Prefix+"relief_goal");S(route,"fuelGoalAnchorId",Prefix+"fuel_goal");S(route,"disruptedLinkAnchorId",Prefix+"disrupted_link");S(route,"hubGateAnchorId",Prefix+"hub_gate");S(route,"recordsAnchorId",Prefix+"records");S(route,"linkRepairHoldMilliseconds",7000);S(route,"recordsHoldMilliseconds",10000);S(route,"deadlineMilliseconds",600000);route.FindPropertyRelative("deliveryRadius").floatValue=8;route.FindPropertyRelative("repairRadius").floatValue=7;route.FindPropertyRelative("hubRadius").floatValue=8;route.FindPropertyRelative("recordsRadius").floatValue=7;
            var tour=route.FindPropertyRelative("cameraTour");S(tour,"StartHoldMilliseconds",800);S(tour,"PostHoldMilliseconds",1200);S(tour,"ApproachHoldMilliseconds",1700);S(tour,"ReturnHoldMilliseconds",500);tour.FindPropertyRelative("SmoothTimeSeconds").floatValue=1.5f;tour.FindPropertyRelative("PostPerspective").vector4Value=new Vector4(52,67,0,55);tour.FindPropertyRelative("ApproachPerspective").vector4Value=new Vector4(48,69,0,52);
            data.ApplyModifiedPropertiesWithoutUndo();Require(scenario.TryValidate(out string error),error);EditorUtility.SetDirty(scenario);AssetDatabase.SaveAssets();
        }
        private static void Group(SerializedProperty group,int i)
        {
            string[] names={"squad_a","squad_b","engineers","relief","fuel","hostile_a","hostile_b"};S(group,"groupId","group.ch02.m05."+names[i]);S(group,"factionIndex",i>=5?2:1);
            string[] units=i switch {0 or 1=>new[]{"Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"},2=>new[]{"Chr_Civilian_Female_01"},3=>new[]{"Veh_Truck_Canopy"},4=>new[]{"Veh_Truck_Tanker"},_=>new[]{"Chr_Insurgent_Male_03","Chr_Insurgent_Female_01","Chr_Insurgent_Female_02"}};
            A(group.FindPropertyRelative("units"),units.Length,(unit,n)=>{string key="Unit_"+units[n],path="Assets/Game/Prefabs/"+(i is 3 or 4?"Vehicles/":"Characters/")+key+".prefab";Require(Load<GameObject>(path)!=null,path);S(unit,"unitConfigKey","unit.route."+units[n].ToLowerInvariant()+"."+i+"."+n);S(unit,"runtimePrefabSourceKey",key);S(unit,"expectedAssetGuid",AssetDatabase.AssetPathToGUID(path));S(unit,"count",i==2?2:1);S(unit,"spawnAnchorId",Prefix+names[i]);S(unit,"missionRoleId",i==2?"role.route.engineer":i==3?"role.route.relief_convoy":i==4?"role.route.fuel_convoy":i>=5?"role.route.hostile":"role.route.rifle");});
        }
        private static void BuildMission()
        {
            var mission=Clone<MissionDefinitionConfig>(CH02M03MarketLifelineConfigBuilder.MissionPath,MissionPath);var data=new SerializedObject(mission);S(data,"missionId",MissionId);S(data,"scenarioId",ScenarioId);S(data,"operationMapId",MapId);S(data,"displayNameKey","mission.route_reopened.name");S(data,"displaySummaryKey","mission.route_reopened.summary");S(data,"locationNameKey","mission.route_reopened.location");foreach(string stage in new[]{"briefing","comms","debrief"})S(data,stage+"SequenceId","seq.ch02.m05."+(stage=="briefing"?"brief":stage));
            string[] names={"lifelines","link","hub","records"};MissionObjectiveRuleKind[] rules={MissionObjectiveRuleKind.SustainRouteLifelines,MissionObjectiveRuleKind.RestoreRouteLink,MissionObjectiveRuleKind.CaptureRouteHub,MissionObjectiveRuleKind.PreserveRouteRecords};string[] roles={"role.route.relief_convoy","role.route.engineer","role.route.rifle","role.route.rifle"};A(data.FindProperty("objectives"),4,(e,i)=>{S(e,"objectiveId","obj.ch02.m05."+names[i]);S(e,"displayTextKey","mission.route_reopened.objective."+names[i]);S(e,"rule",(int)rules[i]);S(e,"missionRoleId",roles[i]);S(e,"targetConfigId",string.Empty);S(e,"requiredCount",i==0?2:i==1?2:1);S(e,"failureOnRuleBreak",true);});
            A(data.FindProperty("stars"),3,(e,i)=>{S(e,"starIndex",i+1);S(e,"rule",(int)(i==0?MissionStarRuleKind.CompleteMission:i==1?MissionStarRuleKind.NoCivilianLoss:MissionStarRuleKind.BreachSupportSurvives));S(e,"displayTextKey","mission.route_reopened.star."+(i+1));S(e,"threshold",0);});
            A(data.FindProperty("firstClearRewards"),2,(e,i)=>{S(e,"kind",i==0?0:1);S(e,"rewardConfigId",i==0?"reward.commander_xp":string.Empty);S(e,"displayTextKey",i==0?"mission.reward.commander_xp":"mission.reward.credits");S(e,"amount",i==0?1200:6000);});A(data.FindProperty("replayRewards"),1,(e,i)=>{S(e,"kind",1);S(e,"rewardConfigId",string.Empty);S(e,"displayTextKey","mission.reward.credits");S(e,"amount",500);});
            data.ApplyModifiedPropertiesWithoutUndo();Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);EditorUtility.SetDirty(mission);AssetDatabase.SaveAssets();
        }
        private static T Load<T>(string path) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new InvalidOperationException(path);
        private static T Clone<T>(string source,string target) where T:ScriptableObject{var asset=AssetDatabase.LoadAssetAtPath<T>(target);if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,target);}EditorUtility.CopySerialized(Load<T>(source),asset);asset.name=Path.GetFileNameWithoutExtension(target);return asset;}
        private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}private static void A(SerializedProperty a,int n,Action<SerializedProperty,int> fill){a.arraySize=n;for(int i=0;i<n;i++)fill?.Invoke(a.GetArrayElementAtIndex(i),i);}private static void S(SerializedObject o,string n,object v)=>S(o.FindProperty(n),v);private static void S(SerializedProperty o,string n,object v)=>S(o.FindPropertyRelative(n),v);private static void S(SerializedProperty p,object v){switch(v){case string s:p.stringValue=s;break;case int i:p.intValue=i;break;case bool b:p.boolValue=b;break;default:throw new ArgumentException(p.propertyPath);}}
    }
}
