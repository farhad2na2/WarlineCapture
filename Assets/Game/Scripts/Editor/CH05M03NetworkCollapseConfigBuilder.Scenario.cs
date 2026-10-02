using System;
using System.Linq;
using Game.Configs;
using Game.Missions.Contracts;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static partial class CH05M03NetworkCollapseConfigBuilder
    {
        private static void BuildScenario()
        {
            var scenario=Clone<ScenarioSetupConfig>(CH05M02TrustUnderFireConfigBuilder.ScenarioPath,ScenarioPath);var d=new SerializedObject(scenario);
            Replace(d,"ch05.m02","ch05.m03");Replace(d,"trust_under_fire","network_collapse");S(d,"scenarioId",ScenarioId);S(d,"operationMapId",MapId);S(d,"deterministicSeed",5003001);S(d,"encounterStartMilliseconds",0);
            var map=Load<OperationMapDefinition>(MapPath);A(d.FindProperty("requiredAnchors"),map.Anchors.Length,(p,i)=>{S(p,"anchorId",map.Anchors[i].AnchorId);S(p,"kind",(int)map.Anchors[i].Kind);});A(d.FindProperty("ambientPresentations"),0,null);
            foreach(string key in new[]{"buildingDisabled","productionDisabled","economyDisabled","airDisabled"})S(d.FindProperty("restrictions"),key,true);S(d.FindProperty("restrictions"),"transportDisabled",false);
            foreach(string mode in new[]{"missionRuntime","extraction","breach","gridlock","supplyLine","marketLifeline","powerRelay","routeReopened"})S(d.FindProperty(mode),"enabled",false);
            A(d.FindProperty("patrolRoutes"),3,(route,i)=>{S(route,"routeId","route.ch05.m03.guards."+(i+1));S(route,"unitGroupId","group.ch05.m03.guards."+(i+1));S(route,"startDelayMilliseconds",0);A(route.FindPropertyRelative("anchorIds"),2,(p,n)=>S(p,Prefix+"guard_"+(i+1)+"_a"));});
            S(d.FindProperty("missionRuntime"),"startingCredits",0);S(d.FindProperty("missionRuntime"),"startingMaterials",80);
            string[] groups={"escort","engineer","carrier","staff.one","staff.two","node.1","node.2","node.3","guards.1","guards.2","guards.3"};
            string[] roles={"role.friendly.escort","role.friendly.engineer","role.friendly.extraction_apc","role.civilian.protected","role.civilian.protected","role.hostile.node.1","role.hostile.node.2","role.hostile.node.3","role.hostile.guard.1","role.hostile.guard.2","role.hostile.guard.3"};
            string[][] units={new[]{"Veh_Tank_USA","Veh_Tank_USA","Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"},new[]{"Chr_Soldier_Male_02_Alt_02"},new[]{"Veh_APC_Heavy"},new[]{"Chr_Civilian_Female_01","Chr_Civilian_Male_01"},new[]{"Chr_Civilian_Female_02","Chr_Civilian_Male_01"},new[]{"Veh_Radar_Tank"},new[]{"Veh_Radar_Tank"},new[]{"Veh_Radar_Tank"},new[]{"Chr_Insurgent_Male_03","Chr_Insurgent_Female_01"},new[]{"Chr_Insurgent_Male_03","Chr_Insurgent_Female_01"},new[]{"Chr_Insurgent_Male_03","Chr_Insurgent_Female_01"}};
            A(d.FindProperty("unitGroups"),groups.Length,(g,i)=>{S(g,"groupId","group.ch05.m03."+groups[i]);S(g,"factionIndex",i<3?1:i<5?0:i<8?3:2);A(g.FindPropertyRelative("units"),units[i].Length,(u,n)=>{
                string name=units[i][n],key="Unit_"+name,path="Assets/Game/Prefabs/"+(name.StartsWith("Veh_")?"Vehicles/":"Characters/")+key+".prefab";Load<GameObject>(path);
                string anchor=i==0?(n==0?"escort_tanks":n==1?"escort_tank_b":"escort_infantry"):i==1?"engineer_start":i==2?"carrier_start":i==3?"staff_one":i==4?"staff_two":i<8?"node_"+(i-4):"guard_"+(i-7)+"_"+(n==0?"a":"b");
                S(u,"unitConfigKey","unit.network."+name.ToLowerInvariant()+"."+i+"."+n);S(u,"runtimePrefabSourceKey",key);S(u,"expectedAssetGuid",AssetDatabase.AssetPathToGUID(path));S(u,"spawnAnchorId",Prefix+anchor);S(u,"missionRoleId",roles[i]);S(u,"count",1);
            });});
            var defense=d.FindProperty("defense");S(defense,"enabled",true);S(defense,"vehiclesSelfSupplied",false);S(defense,"authoredMapDefensesDormant",true);S(defense,"forwardPostStableId","network_collapse.audit");S(defense,"innerCoreAnchorId",Prefix+"inner_core");S(defense,"sensorMissionRoleId",string.Empty);S(defense,"initialProducerAnchorId",Prefix+"initial_barracks");S(defense,"radarPingCharges",0);
            A(defense.FindPropertyRelative("convoyElements"),3,(p,i)=>{S(p,"elementId","convoy.ch05.m03.guards."+(i+1));S(p,"unitGroupId","group.ch05.m03.guards."+(i+1));S(p,"routeId","route.ch05.m03.guards."+(i+1));S(p,"contactAnchorId",Prefix+"guard_"+(i+1)+"_a");S(p,"warningAtMilliseconds",0);S(p,"activationAtMilliseconds",0);S(p,"contactAtMilliseconds",1000);});
            A(defense.FindPropertyRelative("guidanceSteps"),8,(p,i)=>{S(p,"stepId","guidance.ch05.m03."+(i+1));S(p,"titleKey","mission.network_collapse.tutorial."+(i+1)+".title");S(p,"bodyKey","mission.network_collapse.tutorial."+(i+1)+".body");S(p,"action",i%2==0?4:9);S(p,"completion",i%2==0?5:9);S(p,"optional",false);});
            d.ApplyModifiedPropertiesWithoutUndo();Require(scenario.TryValidate(out string error),error);EditorUtility.SetDirty(scenario);
        }
        private static void BuildMission()
        {
            var mission=Clone<MissionDefinitionConfig>(CH05M02TrustUnderFireConfigBuilder.MissionPath,MissionPath);var d=new SerializedObject(mission);Replace(d,"ch05.m02","ch05.m03");Replace(d,"trust_under_fire","network_collapse");S(d,"missionId",MissionId);S(d,"scenarioId",ScenarioId);S(d,"operationMapId",MapId);S(d,"displayNameKey","mission.network_collapse.name");S(d,"displaySummaryKey","mission.network_collapse.summary");S(d,"locationNameKey","mission.network_collapse.location");foreach(string stage in new[]{"briefing","comms","debrief"})S(d,stage+"SequenceId","seq.ch05.m03."+(stage=="briefing"?"brief":stage));
            string[] objectives={"nodes","audit","extraction"};A(d.FindProperty("objectives"),3,(p,i)=>{S(p,"objectiveId","obj.ch05.m03."+objectives[i]);S(p,"displayTextKey","mission.network_collapse.objective."+objectives[i]);S(p,"rule",38+i);S(p,"missionRoleId",i==0?"role.hostile.node.1":"role.friendly.engineer");S(p,"targetConfigId",string.Empty);S(p,"requiredCount",i==0?3:1);S(p,"failureOnRuleBreak",true);});
            A(d.FindProperty("stars"),3,(p,i)=>{S(p,"starIndex",i+1);S(p,"rule",(int)(i==0?MissionStarRuleKind.CompleteMission:i==1?MissionStarRuleKind.NoSquadLoss:MissionStarRuleKind.NoCivilianLoss));S(p,"displayTextKey","mission.network_collapse.star."+(i+1));S(p,"threshold",0);});
            A(d.FindProperty("firstClearRewards"),2,(p,i)=>{S(p,"kind",i==1?1:0);S(p,"rewardConfigId",i==0?"reward.commander_xp":string.Empty);S(p,"displayTextKey",i==0?"mission.reward.commander_xp":"mission.reward.credits");S(p,"amount",i==0?2900:13000);});
            d.ApplyModifiedPropertiesWithoutUndo();Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);EditorUtility.SetDirty(mission);
        }
    }
}
