using System;
using System.Linq;
using Game.Configs;
using Game.Missions.Contracts;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static partial class CH05M04LastCorridorConfigBuilder
    {
        private static void BuildScenario()
        {
            var scenario=Clone<ScenarioSetupConfig>(CH05M03NetworkCollapseConfigBuilder.ScenarioPath,ScenarioPath);var d=new SerializedObject(scenario);
            Replace(d,"ch05.m03","ch05.m04");Replace(d,"network_collapse","last_corridor");S(d,"scenarioId",ScenarioId);S(d,"operationMapId",MapId);S(d,"deterministicSeed",5004001);S(d,"encounterStartMilliseconds",0);
            var map=Load<OperationMapDefinition>(MapPath);A(d.FindProperty("requiredAnchors"),map.Anchors.Length,(p,i)=>{S(p,"anchorId",map.Anchors[i].AnchorId);S(p,"kind",(int)map.Anchors[i].Kind);});A(d.FindProperty("ambientPresentations"),0,null);
            foreach(string key in new[]{"buildingDisabled","productionDisabled","economyDisabled","airDisabled"})S(d.FindProperty("restrictions"),key,true);S(d.FindProperty("restrictions"),"transportDisabled",false);
            foreach(string mode in new[]{"missionRuntime","extraction","breach","gridlock","supplyLine","marketLifeline","powerRelay","routeReopened"})S(d.FindProperty(mode),"enabled",false);
            string[] threatNames={"blockade","route"};
            A(d.FindProperty("patrolRoutes"),2,(route,i)=>{S(route,"routeId","route.ch05.m04."+threatNames[i]);S(route,"unitGroupId","group.ch05.m04.hostile."+threatNames[i]);S(route,"startDelayMilliseconds",0);A(route.FindPropertyRelative("anchorIds"),2,(p,n)=>S(p,Prefix+(i==0?"blockade":"route_hostile")));});
            S(d.FindProperty("missionRuntime"),"startingCredits",0);S(d.FindProperty("missionRuntime"),"startingMaterials",80);
            string[] groups={"escort","engineer","key_carrier","medicine","fuel","reinforcement","staff","hostile.blockade","hostile.route","broken_link"};
            string[] roles={"role.friendly.escort","role.friendly.engineer","role.friendly.key_carrier","role.friendly.medicine","role.friendly.fuel","role.friendly.reinforcement","role.civilian.protected","role.hostile.blockade","role.hostile.route","role.infrastructure.broken_link"};
            string[][] units={new[]{"Veh_Tank_USA","Veh_Tank_USA","Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"},new[]{"Chr_Soldier_Male_02_Alt_02"},new[]{"Veh_APC_Heavy"},new[]{"Veh_Truck_Canopy"},new[]{"Veh_Truck_Canopy"},new[]{"Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"},new[]{"Chr_Civilian_Female_01","Chr_Civilian_Male_01","Chr_Civilian_Female_02","Chr_Civilian_Male_01"},new[]{"Veh_APC_Fast","Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01"},new[]{"Chr_Insurgent_Male_03","Chr_Insurgent_Female_01","Chr_Insurgent_Male_03"},new[]{"Veh_Truck_Canopy"}};
            A(d.FindProperty("unitGroups"),groups.Length,(g,i)=>{S(g,"groupId","group.ch05.m04."+groups[i]);S(g,"factionIndex",i<6?1:i==6||i==9?0:i==7?3:2);A(g.FindPropertyRelative("units"),units[i].Length,(u,n)=>{
                string name=units[i][n],key="Unit_"+name,path="Assets/Game/Prefabs/"+(name.StartsWith("Veh_")?"Vehicles/":"Characters/")+key+".prefab";Load<GameObject>(path);
                string anchor=i==0?(n==0?"escort_tanks":n==1?"escort_tank_b":"escort_infantry"):i==1?"engineer_start":i==2?"key_carrier_start":i==3?"medicine_start":i==4?"fuel_start":i==5?"reinforcement_start":i==6?"staff":i==7?(n==0?"blockade_apc":"blockade"):i==8?"route_hostile":"broken_link";
                S(u,"unitConfigKey","unit.corridor."+name.ToLowerInvariant()+"."+i+"."+n);S(u,"runtimePrefabSourceKey",key);S(u,"expectedAssetGuid",AssetDatabase.AssetPathToGUID(path));S(u,"spawnAnchorId",Prefix+anchor);S(u,"missionRoleId",roles[i]);S(u,"count",1);
            });});
            var defense=d.FindProperty("defense");S(defense,"enabled",true);S(defense,"vehiclesSelfSupplied",false);S(defense,"authoredMapDefensesDormant",true);S(defense,"forwardPostStableId","last_corridor.receiving");S(defense,"innerCoreAnchorId",Prefix+"inner_core");S(defense,"sensorMissionRoleId",string.Empty);S(defense,"initialProducerAnchorId",Prefix+"initial_barracks");S(defense,"radarPingCharges",0);
            A(defense.FindPropertyRelative("convoyElements"),2,(p,i)=>{S(p,"elementId","convoy.ch05.m04."+threatNames[i]);S(p,"unitGroupId","group.ch05.m04.hostile."+threatNames[i]);S(p,"routeId","route.ch05.m04."+threatNames[i]);S(p,"contactAnchorId",Prefix+(i==0?"blockade":"route_hostile"));S(p,"warningAtMilliseconds",0);S(p,"activationAtMilliseconds",0);S(p,"contactAtMilliseconds",1000);});
            A(defense.FindPropertyRelative("guidanceSteps"),8,(p,i)=>{S(p,"stepId","guidance.ch05.m04."+(i+1));S(p,"titleKey","mission.last_corridor.tutorial."+(i+1)+".title");S(p,"bodyKey","mission.last_corridor.tutorial."+(i+1)+".body");S(p,"action",i%2==0?4:9);S(p,"completion",i%2==0?5:9);S(p,"optional",false);});
            d.ApplyModifiedPropertiesWithoutUndo();Require(scenario.TryValidate(out string error),error);EditorUtility.SetDirty(scenario);
        }
        private static void BuildMission()
        {
            var mission=Clone<MissionDefinitionConfig>(CH05M03NetworkCollapseConfigBuilder.MissionPath,MissionPath);var d=new SerializedObject(mission);Replace(d,"ch05.m03","ch05.m04");Replace(d,"network_collapse","last_corridor");S(d,"missionId",MissionId);S(d,"scenarioId",ScenarioId);S(d,"operationMapId",MapId);S(d,"displayNameKey","mission.last_corridor.name");S(d,"displaySummaryKey","mission.last_corridor.summary");S(d,"locationNameKey","mission.last_corridor.location");foreach(string stage in new[]{"briefing","comms","debrief"})S(d,stage+"SequenceId","seq.ch05.m04."+(stage=="briefing"?"brief":stage));
            string[] objectives={"supplies","link","authority"};string[] objectiveKeys={"deliveries","repair","keys"};A(d.FindProperty("objectives"),3,(p,i)=>{S(p,"objectiveId","obj.ch05.m04."+objectives[i]);S(p,"displayTextKey","mission.last_corridor.objective."+objectiveKeys[i]);S(p,"rule",41+i);S(p,"missionRoleId",i==0?"role.friendly.medicine":i==1?"role.friendly.engineer":"role.friendly.key_carrier");S(p,"targetConfigId",string.Empty);S(p,"requiredCount",i==0?3:i==1?1:2);S(p,"failureOnRuleBreak",true);});
            A(d.FindProperty("stars"),3,(p,i)=>{S(p,"starIndex",i+1);S(p,"rule",(int)(i==0?MissionStarRuleKind.CompleteMission:i==1?MissionStarRuleKind.NoSquadLoss:MissionStarRuleKind.NoCivilianLoss));S(p,"displayTextKey","mission.last_corridor.star."+(i+1));S(p,"threshold",0);});
            A(d.FindProperty("firstClearRewards"),2,(p,i)=>{S(p,"kind",i==1?1:0);S(p,"rewardConfigId",i==0?"reward.commander_xp":string.Empty);S(p,"displayTextKey",i==0?"mission.reward.commander_xp":"mission.reward.credits");S(p,"amount",i==0?3100:14000);});
            d.ApplyModifiedPropertiesWithoutUndo();Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);EditorUtility.SetDirty(mission);
        }
    }
}
