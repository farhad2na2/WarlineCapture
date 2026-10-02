using System;
using System.IO;
using Game.Authoring;
using Game.Configs;
using Game.Missions.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class CH04M05ArmorBreakConfigBuilder
    {
        private static void BuildReservePrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReservePrefabPath));AssetDatabase.Refresh();
            var root=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(CH04M02SteelPushConfigBuilder.ReservePrefabPath));
            try
            {
                root.name=ReserveBuildingId;var definition=new SerializedObject(root.GetComponent<BuildingDefinitionAuthoring>());
                S(definition,"fuelStorageCapacity",240);S(definition,"description","Finite Fuel allocation: 200 barrels, including 40 protected for relief generators and water pumps.");definition.ApplyModifiedPropertiesWithoutUndo();
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,ReservePrefabPath);var config=Load<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
                var data=new SerializedObject(config);var list=data.FindProperty("spawnables");bool found=false;for(int i=0;i<list.arraySize;i++)found|=list.GetArrayElementAtIndex(i).objectReferenceValue==prefab;
                if(!found){int index=list.arraySize++;list.GetArrayElementAtIndex(index).objectReferenceValue=prefab;data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);}
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        private static void BuildScenario()
        {
            var scenario=Clone<ScenarioSetupConfig>(CH04M02SteelPushConfigBuilder.ScenarioPath,ScenarioPath);var d=new SerializedObject(scenario);Replace(d,"ch04.m02","ch04.m05");Replace(d,"steel_push","armor_break");S(d,"scenarioId",ScenarioId);S(d,"operationMapId",MapId);S(d,"deterministicSeed",4005001);
            var map=Load<OperationMapDefinition>(MapPath);A(d.FindProperty("requiredAnchors"),map.Anchors.Length,(p,i)=>{S(p,"anchorId",map.Anchors[i].AnchorId);S(p,"kind",(int)map.Anchors[i].Kind);});A(d.FindProperty("ambientPresentations"),0,null);
            var restrictions=d.FindProperty("restrictions");S(restrictions,"buildingDisabled",false);S(restrictions,"productionDisabled",false);S(restrictions,"economyDisabled",false);S(restrictions,"transportDisabled",false);S(restrictions,"airDisabled",false);
            var runtime=d.FindProperty("missionRuntime");S(runtime,"enabled",true);S(runtime,"startingCredits",0);S(runtime,"startingMaterials",200);S(runtime,"baseMissionRoleId","role.friendly.fuel_reserve");S(runtime,"baseAnchorId",Prefix+"forward_post");S(runtime,"requiredProducerConfigId",ReserveBuildingId);S(runtime.FindPropertyRelative("buildZone"),"anchorId",Prefix+"build_zone");
            A(runtime.FindPropertyRelative("buildCatalog"),1,(p,i)=>{S(p,"buildingConfigId",ReserveBuildingId);S(p,"maxCount",1);});
            // Scenario owns exactly 11 friendly units, 12 military hostiles and 3 neutral relief staff.
            string[] names={"g2a","g2g","radar","armor","command_squad","aircraft","battery","hostile_armor","command","air","relief"};
            string[] anchors={"launcher_air","launcher_ground","radar","armor","command_squad","aircraft","battery","hostile_armor","command","air_approach","relief"};
            string[] roles={"role.friendly.g2a","role.friendly.g2g","role.friendly.radar","role.friendly.armor","role.friendly.command_squad","role.friendly.aircraft","role.hostile.battery","role.hostile.armor","role.hostile.command","role.hostile.air","role.civilian.protected"};
            string[][] prefabs={new[]{"Veh_Missle_Launcher_Air"},new[]{"Veh_Missle_Launcher_Ground"},new[]{"Veh_Radar_Tank"},new[]{"Veh_Tank_USA","Veh_Tank_USA","Veh_APC_Fast"},new[]{"Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"},new[]{"Veh_Helicopter_Attack"},new[]{"Veh_Missle_Launcher_Ground"},new[]{"Veh_Tank_USA","Veh_Tank_USA","Veh_APC_Fast"},new[]{"Veh_Radar_Tank","Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"},new[]{"Veh_Drone","Veh_Drone","Veh_Jet_01"},new[]{"Chr_Civilian_Female_01","Chr_Civilian_Female_02","Chr_Civilian_Male_01"}};
            A(d.FindProperty("unitGroups"),names.Length,(group,i)=>{S(group,"groupId","group.ch04.m05."+names[i]);S(group,"factionIndex",i<6?1:i<10?2:0);A(group.FindPropertyRelative("units"),prefabs[i].Length,(u,n)=>{
                string key="Unit_"+prefabs[i][n],path="Assets/Game/Prefabs/"+(prefabs[i][n].StartsWith("Veh_")?"Vehicles/":"Characters/")+key+".prefab";Load<GameObject>(path);
                S(u,"unitConfigKey","unit.ch04.m05."+prefabs[i][n].ToLowerInvariant());S(u,"runtimePrefabSourceKey",key);S(u,"expectedAssetGuid",AssetDatabase.AssetPathToGUID(path));string anchor=anchors[i];if((i==3||i==7)&&n>0)anchor+="_"+(n==1?"b":"c");if(i==8&&n>0)anchor="command_staff";S(u,"spawnAnchorId",Prefix+anchor);S(u,"missionRoleId",roles[i]);S(u,"count",1);
            });});
            string[] threats={"battery","hostile_armor","command","air"};
            A(d.FindProperty("patrolRoutes"),4,(route,i)=>{S(route,"routeId","route.ch04.m05."+threats[i]);S(route,"unitGroupId","group.ch04.m05."+threats[i]);S(route,"startDelayMilliseconds",i==3?20000:0);string[] stops=i==0?new[]{"battery","battery"}:i==1?new[]{"hostile_armor","assault_approach","command"}:i==2?new[]{"command","command"}:new[]{"air_flank","air_contact","coverage"};A(route.FindPropertyRelative("anchorIds"),stops.Length,(p,n)=>S(p,Prefix+stops[n]));});
            var defense=d.FindProperty("defense");S(defense,"enabled",true);S(defense,"vehiclesSelfSupplied",false);S(defense,"authoredMapDefensesDormant",true);S(defense,"forwardPostStableId","armor_break.fuel_reserve");S(defense,"sensorMissionRoleId",string.Empty);S(defense,"radarPingCharges",0);
            A(defense.FindPropertyRelative("convoyElements"),4,(p,i)=>{S(p,"elementId","convoy.ch04.m05."+threats[i]);S(p,"unitGroupId","group.ch04.m05."+threats[i]);S(p,"routeId","route.ch04.m05."+threats[i]);S(p,"contactAnchorId",Prefix+(i==0?"battery":i==2?"command":i==3?"air_contact":"contact"));S(p,"warningAtMilliseconds",0);S(p,"activationAtMilliseconds",i==3?20000:0);S(p,"contactAtMilliseconds",i==3?50000:1000);});
            d.ApplyModifiedPropertiesWithoutUndo();Require(scenario.TryValidate(out string error),error);EditorUtility.SetDirty(scenario);
        }
        private static void BuildMission()
        {
            var mission=Clone<MissionDefinitionConfig>(CH04M02SteelPushConfigBuilder.MissionPath,MissionPath);var d=new SerializedObject(mission);Replace(d,"ch04.m02","ch04.m05");Replace(d,"steel_push","armor_break");S(d,"missionId",MissionId);S(d,"scenarioId",ScenarioId);S(d,"operationMapId",MapId);S(d,"displayNameKey","mission.armor_break.name");S(d,"displaySummaryKey","mission.armor_break.summary");S(d,"locationNameKey","mission.armor_break.location");foreach(string stage in new[]{"briefing","comms","debrief"})S(d,stage+"SequenceId","seq.ch04.m05."+(stage=="briefing"?"brief":stage));
            string[] names={"military","authority","relief"};string[] roles={"role.hostile.command","role.friendly.command_squad","role.civilian.protected"};int[] counts={12,4,3};
            A(d.FindProperty("objectives"),3,(p,i)=>{S(p,"objectiveId","obj.ch04.m05."+names[i]);S(p,"displayTextKey","mission.armor_break.objective."+names[i]);S(p,"rule",29+i);S(p,"missionRoleId",roles[i]);S(p,"requiredCount",counts[i]);S(p,"failureOnRuleBreak",i==2);});
            A(d.FindProperty("stars"),3,(p,i)=>{S(p,"starIndex",i+1);S(p,"rule",(int)(i==0?MissionStarRuleKind.CompleteMission:i==1?MissionStarRuleKind.NoSquadLoss:MissionStarRuleKind.NoCivilianLoss));S(p,"displayTextKey","mission.armor_break.star."+(i+1));S(p,"threshold",0);});
            A(d.FindProperty("firstClearRewards"),2,(p,i)=>{S(p,"kind",i==0?0:1);S(p,"rewardConfigId",i==0?"reward.commander_xp":string.Empty);S(p,"displayTextKey",i==0?"mission.reward.commander_xp":"mission.reward.credits");S(p,"amount",i==0?2500:11000);});
            d.ApplyModifiedPropertiesWithoutUndo();Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);EditorUtility.SetDirty(mission);
        }
    }
}
