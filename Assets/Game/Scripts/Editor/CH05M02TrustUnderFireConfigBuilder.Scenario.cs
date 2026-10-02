using System;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Configs;
using Game.Missions.Contracts;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static partial class CH05M02TrustUnderFireConfigBuilder
    {
        private static void BuildBuildingPrefabs()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ShelterPrefabPath));AssetDatabase.Refresh();
            CloneBuilding("Assets/Game/Prefabs/Buildings/Tent_Refugee.prefab",ShelterPrefabPath,ShelterBuildingId,"Protected evacuation shelter",0,1000,new Vector2Int(14,12));
            CloneBuilding("Assets/Game/Prefabs/Buildings/Building_Satelite_Dish.prefab",BroadcastPrefabPath,BroadcastBuildingId,"Verified broadcast source",0,1000,new Vector2Int(14,12));
            CloneBuilding(CH04M05ArmorBreakConfigBuilder.ReservePrefabPath,ReservePrefabPath,ReserveBuildingId,"Evacuation emergency reserve",240,0,new Vector2Int(26,25));
            var config=Load<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");var data=new SerializedObject(config);var list=data.FindProperty("spawnables");
            foreach(string path in new[]{ShelterPrefabPath,BroadcastPrefabPath,ReservePrefabPath})
            {var prefab=Load<GameObject>(path);bool found=false;for(int i=0;i<list.arraySize;i++)found|=list.GetArrayElementAtIndex(i).objectReferenceValue==prefab;if(!found){int index=list.arraySize++;list.GetArrayElementAtIndex(index).objectReferenceValue=prefab;}}
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);
        }
        private static void CloneBuilding(string source,string target,string id,string label,int capacity,int health,Vector2Int footprint)
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(source));
            try
            {
                PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);root.name=id;
                var definition=root.GetComponent<BuildingDefinitionAuthoring>();Require(definition!=null,"Building lacks actual definition: "+source);definition.ApplyConfigIfAvailable();
                var data=new SerializedObject(definition);data.FindProperty("config").objectReferenceValue=null;S(data,"displayName",label);S(data,"description",id==ReserveBuildingId?"100 Fuel supplied; 20 allocated to shelters.":id==ShelterBuildingId?"Protected relief checkpoint for the named evacuation convoy.":"Preserve the transmitter and verify the source on foot.");S(data,"canRequest",false);data.FindProperty("productions").arraySize=0;data.FindProperty("footprintCells").vector2IntValue=footprint;
                if(capacity>0)S(data,"fuelStorageCapacity",capacity);if(health>0)S(data,"maxHealth",health);data.ApplyModifiedPropertiesWithoutUndo();
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,target);Require(prefab!=null&&prefab.name==id&&PrefabUtility.GetPrefabAssetType(prefab)!=PrefabAssetType.Variant,"Mission prefab must persist independent identity: "+id);
                var saved=PrefabUtility.LoadPrefabContents(target);
                try{var native=saved.GetComponent<BuildingDefinitionAuthoring>();Require(native!=null&&new SerializedObject(native).FindProperty("config").objectReferenceValue==null&&native.ConfiguredFootprintCells==footprint&&native.ConfiguredMaxHealth==definition.ConfiguredMaxHealth&&native.ConfiguredFuelStorageCapacity==definition.ConfiguredFuelStorageCapacity&&native.ConfiguredProductionCount==0&&!native.ConfiguredCanRequest,"Saved Trust building differs from actual qualified configuration: "+id);}
                finally{PrefabUtility.UnloadPrefabContents(saved);}
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        private static void BuildScenario()
        {
            var scenario=Clone<ScenarioSetupConfig>(CH05M01CitywideAlertConfigBuilder.ScenarioPath,ScenarioPath);var d=new SerializedObject(scenario);
            Replace(d,"ch05.m01","ch05.m02");Replace(d,"citywide_alert","trust_under_fire");S(d,"scenarioId",ScenarioId);S(d,"operationMapId",MapId);S(d,"deterministicSeed",5002001);S(d,"encounterStartMilliseconds",0);
            var map=Load<OperationMapDefinition>(MapPath);A(d.FindProperty("requiredAnchors"),map.Anchors.Length,(p,i)=>{S(p,"anchorId",map.Anchors[i].AnchorId);S(p,"kind",(int)map.Anchors[i].Kind);});A(d.FindProperty("ambientPresentations"),0,null);
            foreach(string key in new[]{"buildingDisabled","productionDisabled","economyDisabled","transportDisabled","airDisabled"})S(d.FindProperty("restrictions"),key,true);
            foreach(string mode in new[]{"missionRuntime","extraction","breach","gridlock","supplyLine","marketLifeline","powerRelay","routeReopened"})S(d.FindProperty(mode),"enabled",false);
            string[] threats={"north","south","relay"};
            A(d.FindProperty("patrolRoutes"),3,(route,i)=>{S(route,"routeId","route.ch05.m02."+threats[i]);S(route,"unitGroupId","group.ch05.m02.hostile."+threats[i]);S(route,"startDelayMilliseconds",0);string anchor=i==2?"hostile_relay_b":"hostile_"+threats[i];A(route.FindPropertyRelative("anchorIds"),2,(p,n)=>S(p,Prefix+anchor));});
            string[] groups={"escort.north","escort.south","convoy.north","convoy.south","engineer","staff.north","staff.south","hostile.north","hostile.south","hostile.relay"};
            string[] roles={"role.friendly.escort.north","role.friendly.escort.south","role.friendly.convoy.north","role.friendly.convoy.south","role.friendly.engineer","role.civilian.protected","role.civilian.protected","role.hostile.north","role.hostile.south","role.hostile.relay"};
            string[] rifle={"Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"};
            string[][] units={new[]{"Veh_Tank_USA"}.Concat(rifle).ToArray(),new[]{"Veh_Tank_USA"}.Concat(rifle).ToArray(),new[]{"Veh_Truck_Canopy"},new[]{"Veh_Truck_Canopy"},new[]{"Chr_Soldier_Male_02_Alt_02"},new[]{"Chr_Civilian_Female_01","Chr_Civilian_Male_01"},new[]{"Chr_Civilian_Female_02","Chr_Civilian_Male_01"},new[]{"Veh_APC_Fast","Chr_Insurgent_Male_03","Chr_Insurgent_Female_01"},new[]{"Veh_APC_Fast","Chr_Insurgent_Male_03","Chr_Insurgent_Female_01"},new[]{"Chr_Insurgent_Male_03","Chr_Insurgent_Female_01","Chr_Insurgent_Female_02"}};
            A(d.FindProperty("unitGroups"),groups.Length,(g,i)=>{S(g,"groupId","group.ch05.m02."+groups[i]);S(g,"factionIndex",i<5?1:i<7?0:2);A(g.FindPropertyRelative("units"),units[i].Length,(u,n)=>{
                string name=units[i][n],key="Unit_"+name,path="Assets/Game/Prefabs/"+(name.StartsWith("Veh_")?"Vehicles/":"Characters/")+key+".prefab";Load<GameObject>(path);
                string anchor=i==0?(n==0?"escort_north":"infantry_north"):i==1?(n==0?"escort_south":"infantry_south"):i==2?"north_start":i==3?"south_start":i==4?"engineer_start":i==5?"staff_north":i==6?"staff_south":i==7?(n==0?"hostile_north":"hostile_north_infantry"):i==8?(n==0?"hostile_south":"hostile_south_infantry"):"hostile_relay_"+(char)('a'+n);
                S(u,"unitConfigKey","unit.trust."+name.ToLowerInvariant()+"."+i+"."+n);S(u,"runtimePrefabSourceKey",key);S(u,"expectedAssetGuid",AssetDatabase.AssetPathToGUID(path));S(u,"spawnAnchorId",Prefix+anchor);S(u,"missionRoleId",roles[i]);S(u,"count",1);
            });});
            var defense=d.FindProperty("defense");S(defense,"enabled",true);S(defense,"vehiclesSelfSupplied",false);S(defense,"authoredMapDefensesDormant",true);S(defense,"forwardPostStableId","trust_under_fire.shelter.north");S(defense,"innerCoreAnchorId",Prefix+"inner_core");S(defense,"sensorMissionRoleId",string.Empty);S(defense,"initialProducerAnchorId",Prefix+"initial_barracks");S(defense,"radarPingCharges",0);A(defense.FindPropertyRelative("convoyElements"),3,(p,i)=>{S(p,"elementId","convoy.ch05.m02."+threats[i]);S(p,"unitGroupId","group.ch05.m02.hostile."+threats[i]);S(p,"routeId","route.ch05.m02."+threats[i]);S(p,"contactAnchorId",Prefix+(i==2?"relay_gate":"hostile_"+threats[i]));S(p,"warningAtMilliseconds",0);S(p,"activationAtMilliseconds",0);S(p,"contactAtMilliseconds",1000);});
            A(defense.FindPropertyRelative("guidanceSteps"),8,(p,i)=>{S(p,"stepId","guidance.ch05.m02."+(i+1));S(p,"titleKey","mission.trust_under_fire.tutorial."+(i+1)+".title");S(p,"bodyKey","mission.trust_under_fire.tutorial."+(i+1)+".body");S(p,"action",i is 1 or 3 or 4 or 6?4:i==7?5:9);S(p,"completion",i is 0 or 2 or 5?9:i==4?4:5);S(p,"optional",false);});
            d.ApplyModifiedPropertiesWithoutUndo();Require(scenario.TryValidate(out string error),error);EditorUtility.SetDirty(scenario);
        }
        private static void BuildMission()
        {
            var mission=Clone<MissionDefinitionConfig>(CH05M01CitywideAlertConfigBuilder.MissionPath,MissionPath);var d=new SerializedObject(mission);Replace(d,"ch05.m01","ch05.m02");Replace(d,"citywide_alert","trust_under_fire");S(d,"missionId",MissionId);S(d,"scenarioId",ScenarioId);S(d,"operationMapId",MapId);S(d,"displayNameKey","mission.trust_under_fire.name");S(d,"displaySummaryKey","mission.trust_under_fire.summary");S(d,"locationNameKey","mission.trust_under_fire.location");foreach(string stage in new[]{"briefing","comms","debrief"})S(d,stage+"SequenceId","seq.ch05.m02."+(stage=="briefing"?"brief":stage));
            string[] names={"north","south","broadcast"};string[] roles={"role.friendly.convoy.north","role.friendly.convoy.south","role.hostile.relay"};int[] counts={1,1,3};
            A(d.FindProperty("objectives"),3,(p,i)=>{S(p,"objectiveId","obj.ch05.m02."+names[i]);S(p,"displayTextKey","mission.trust_under_fire.objective."+names[i]);S(p,"rule",35+i);S(p,"missionRoleId",roles[i]);S(p,"targetConfigId",string.Empty);S(p,"requiredCount",counts[i]);S(p,"failureOnRuleBreak",true);});
            A(d.FindProperty("stars"),3,(p,i)=>{S(p,"starIndex",i+1);S(p,"rule",(int)(i==0?MissionStarRuleKind.CompleteMission:i==1?MissionStarRuleKind.NoSquadLoss:MissionStarRuleKind.NoCivilianLoss));S(p,"displayTextKey","mission.trust_under_fire.star."+(i+1));S(p,"threshold",0);});
            A(d.FindProperty("firstClearRewards"),3,(p,i)=>{S(p,"kind",i==1?1:0);S(p,"rewardConfigId",i==0?"reward.commander_xp":i==1?string.Empty:"reward.ch05.m02.supply_drop_unlock");S(p,"displayTextKey",i==0?"mission.reward.commander_xp":i==1?"mission.reward.credits":"mission.trust_under_fire.reward.supply");S(p,"amount",i==0?2700:i==1?12000:1);});
            d.ApplyModifiedPropertiesWithoutUndo();Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);EditorUtility.SetDirty(mission);
        }
    }
}
