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
    public static partial class CH05M01CitywideAlertConfigBuilder
    {
        private static void BuildBuildingPrefabs()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReservePrefabPath));AssetDatabase.Refresh();
            CloneBuilding(CH04M05ArmorBreakConfigBuilder.ReservePrefabPath,ReservePrefabPath,ReserveBuildingId,"Civic emergency reserve",240,0,null);
            CloneBuilding("Assets/Game/Prefabs/Buildings/CH03M04EvidenceChain/EvidenceChain_Clinic.prefab",ClinicPrefabPath,ClinicBuildingId,"District clinic",0,1000,new Vector2Int(14,12));
            CloneBuilding("Assets/Game/Prefabs/Buildings/Building_WaterTank.prefab",UtilityPrefabPath,UtilityBuildingId,"Water and power service",0,700,new Vector2Int(20,16));
            var config=Load<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");var data=new SerializedObject(config);var list=data.FindProperty("spawnables");
            foreach(string path in new[]{ReservePrefabPath,ClinicPrefabPath,UtilityPrefabPath,BarrackPrefabPath})
            {var prefab=Load<GameObject>(path);bool found=false;for(int i=0;i<list.arraySize;i++)found|=list.GetArrayElementAtIndex(i).objectReferenceValue==prefab;if(!found){int index=list.arraySize++;list.GetArrayElementAtIndex(index).objectReferenceValue=prefab;}}
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);
        }
        private static void CloneBuilding(string source,string target,string id,string label,int capacity,int health,Vector2Int? footprint)
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(source));
            try
            {
                PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                root.name=id;var definition=root.GetComponent<BuildingDefinitionAuthoring>();Require(definition!=null,"Building lacks real definition: "+source);
                // Resolve every canonical configured value before detaching the mission clone.
                definition.ApplyConfigIfAvailable();
                var data=new SerializedObject(definition);data.FindProperty("config").objectReferenceValue=null;S(data,"displayName",label);S(data,"description",id==ReserveBuildingId?"100 Fuel barrels supplied; 20 remain allocated to civic generators.":"Operational civic service protected by the response forces.");S(data,"canRequest",false);
                if(capacity>0)S(data,"fuelStorageCapacity",capacity);if(health>0)S(data,"maxHealth",health);if(footprint.HasValue)data.FindProperty("footprintCells").vector2IntValue=footprint.Value;
                if(id==UtilityBuildingId){data.FindProperty("productions").arraySize=0;var generator=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Generator_Large_01.prefab"),root.transform);generator.name="Civic emergency generator";generator.transform.localPosition=new Vector3(5,0,2);}
                data.ApplyModifiedPropertiesWithoutUndo();
                Require(definition.ConfiguredFootprintCells.x>0&&definition.ConfiguredFootprintCells.y>0&&definition.ConfiguredMaxHealth>0,"Mission building lacks a valid native footprint/health: "+id);
                if(id==ReserveBuildingId)Require(definition.ConfiguredFuelStorageCapacity==240&&definition.ConfiguredFootprintCells==new Vector2Int(26,25),"Citywide reserve canonical data drifted.");
                if(id==ClinicBuildingId)Require(definition.ConfiguredMaxHealth==1000&&definition.ConfiguredFootprintCells==new Vector2Int(14,12),"Citywide clinic data drifted.");
                if(id==UtilityBuildingId)Require(definition.ConfiguredMaxHealth==700&&definition.ConfiguredFootprintCells==new Vector2Int(20,16)&&definition.ConfiguredProductionCount==0,"Citywide utility compound data drifted.");
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,target);Require(prefab!=null&&prefab.name==id,"Citywide prefab identity mismatch: "+id);
                var saved=PrefabUtility.LoadPrefabContents(target);
                try
                {
                    Require(PrefabUtility.GetPrefabAssetType(prefab)!=PrefabAssetType.Variant,"Citywide mission building must be an independent prefab: "+id);
                    var savedDefinition=saved.GetComponent<BuildingDefinitionAuthoring>();Require(savedDefinition!=null,"Saved mission prefab lacks building definition: "+id);
                    Require(new SerializedObject(savedDefinition).FindProperty("config").objectReferenceValue==null,"Saved mission prefab retained source config ownership: "+id);
                    Require(savedDefinition.ConfiguredFootprintCells==definition.ConfiguredFootprintCells&&savedDefinition.ConfiguredMaxHealth==definition.ConfiguredMaxHealth&&savedDefinition.ConfiguredFuelStorageCapacity==definition.ConfiguredFuelStorageCapacity&&savedDefinition.ConfiguredProductionCount==definition.ConfiguredProductionCount&&!savedDefinition.ConfiguredCanRequest,"Saved mission prefab differs from qualified native values: "+id);
                }
                finally{PrefabUtility.UnloadPrefabContents(saved);}
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        private static void BuildScenario()
        {
            var scenario=Clone<ScenarioSetupConfig>(CH04M05ArmorBreakConfigBuilder.ScenarioPath,ScenarioPath);var d=new SerializedObject(scenario);Replace(d,"ch04.m05","ch05.m01");Replace(d,"armor_break","citywide_alert");S(d,"scenarioId",ScenarioId);S(d,"operationMapId",MapId);S(d,"deterministicSeed",5001001);
            var map=Load<OperationMapDefinition>(MapPath);A(d.FindProperty("requiredAnchors"),map.Anchors.Length,(p,i)=>{S(p,"anchorId",map.Anchors[i].AnchorId);S(p,"kind",(int)map.Anchors[i].Kind);});A(d.FindProperty("ambientPresentations"),0,null);
            var restrictions=d.FindProperty("restrictions");S(restrictions,"buildingDisabled",false);S(restrictions,"productionDisabled",false);S(restrictions,"economyDisabled",false);S(restrictions,"transportDisabled",false);S(restrictions,"airDisabled",false);
            var runtime=d.FindProperty("missionRuntime");S(runtime,"enabled",true);S(runtime,"startingCredits",0);S(runtime,"startingMaterials",180);S(runtime,"baseMissionRoleId","role.friendly.response.clinic");S(runtime,"baseAnchorId",Prefix+"forward_post");S(runtime,"requiredProducerConfigId","Building_Barrack");S(runtime.FindPropertyRelative("buildZone"),"anchorId",Prefix+"build_zone");
            S(runtime,"requiredUnitConfigId","Unit_Chr_Soldier_Male_02_Alt_04");A(runtime.FindPropertyRelative("buildCatalog"),1,(p,i)=>{S(p,"buildingConfigId","Building_Barrack");S(p,"maxCount",2);});
            string[] names={"response.clinic","response.utility","engineer.clinic","engineer.utility","g2a","radar","staff.clinic","staff.utility","hostile.clinic","hostile.utility","hostile.air"};
            string[] roles={"role.friendly.response.clinic","role.friendly.response.utility","role.friendly.engineer.clinic","role.friendly.engineer.utility","role.friendly.g2a","role.friendly.radar","role.civilian.protected","role.civilian.protected","role.hostile.clinic","role.hostile.utility","role.hostile.air"};
            string[][] prefabs={new[]{"Veh_Tank_USA","Veh_Tank_USA","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01"},new[]{"Veh_Tank_USA","Veh_Tank_USA","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01"},new[]{"Chr_Soldier_Male_02_Alt_02"},new[]{"Chr_Soldier_Female_02_Alt_01"},new[]{"Veh_Missle_Launcher_Air"},new[]{"Veh_Radar_Tank"},new[]{"Chr_Civilian_Female_01","Chr_Civilian_Male_01"},new[]{"Chr_Civilian_Female_02","Chr_Civilian_Male_01"},new[]{"Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01","Veh_APC_Fast"},new[]{"Veh_Tank_USA","Veh_Tank_USA","Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Female_01_Alt_01"},new[]{"Veh_Drone","Veh_Drone"}};
            string[] baseAnchors={"clinic_defense","utility_defense","clinic_engineer","utility_engineer","g2a","radar","staff_clinic","staff_utility","hostile_clinic","hostile_utility","air_approach"};
            A(d.FindProperty("unitGroups"),names.Length,(group,i)=>{S(group,"groupId","group.ch05.m01."+names[i]);S(group,"factionIndex",i<6?1:i<8?0:2);A(group.FindPropertyRelative("units"),prefabs[i].Length,(u,n)=>{
                string key="Unit_"+prefabs[i][n],path="Assets/Game/Prefabs/"+(prefabs[i][n].StartsWith("Veh_")?"Vehicles/":"Characters/")+key+".prefab";Load<GameObject>(path);
                string anchor=baseAnchors[i];if(i<2)anchor=n==1?(i==0?"clinic_armor_b":"utility_armor_b"):n>1?(i==0?"clinic_infantry":"utility_infantry"):anchor;if(i==9&&n==1)anchor="hostile_utility_b";
                S(u,"unitConfigKey","unit.ch05.m01."+prefabs[i][n].ToLowerInvariant());S(u,"runtimePrefabSourceKey",key);S(u,"expectedAssetGuid",AssetDatabase.AssetPathToGUID(path));S(u,"spawnAnchorId",Prefix+anchor);S(u,"missionRoleId",roles[i]);S(u,"count",1);
            });});
            string[] threats={"clinic","utility","air"};
            A(d.FindProperty("patrolRoutes"),3,(route,i)=>{S(route,"routeId","route.ch05.m01."+threats[i]);S(route,"unitGroupId","group.ch05.m01.hostile."+threats[i]);S(route,"startDelayMilliseconds",i==2?150000:130000);string[] stops=i==0?new[]{"hostile_clinic","clinic_contact","clinic"}:i==1?new[]{"hostile_utility","utility_contact","utility"}:new[]{"air_approach","air_contact","coverage"};A(route.FindPropertyRelative("anchorIds"),stops.Length,(p,n)=>S(p,Prefix+stops[n]));});
            var defense=d.FindProperty("defense");S(defense,"enabled",true);S(defense,"vehiclesSelfSupplied",false);S(defense,"authoredMapDefensesDormant",true);S(defense,"forwardPostStableId","citywide_alert.clinic");S(defense,"sensorMissionRoleId",string.Empty);S(defense,"radarPingCharges",0);S(defense,"innerCoreAnchorId",Prefix+"inner_core");S(defense,"initialProducerAnchorId",Prefix+"initial_barracks");
            A(defense.FindPropertyRelative("convoyElements"),3,(p,i)=>{S(p,"elementId","convoy.ch05.m01."+threats[i]);S(p,"unitGroupId","group.ch05.m01.hostile."+threats[i]);S(p,"routeId","route.ch05.m01."+threats[i]);S(p,"contactAnchorId",Prefix+threats[i]+"_contact");S(p,"warningAtMilliseconds",i==2?20000:0);S(p,"activationAtMilliseconds",i==2?150000:130000);S(p,"contactAtMilliseconds",i==2?165000:145000);});
            d.ApplyModifiedPropertiesWithoutUndo();Require(scenario.TryValidate(out string error),error);EditorUtility.SetDirty(scenario);
        }
        private static void BuildMission()
        {
            var mission=Clone<MissionDefinitionConfig>(CH04M05ArmorBreakConfigBuilder.MissionPath,MissionPath);var d=new SerializedObject(mission);Replace(d,"ch04.m05","ch05.m01");Replace(d,"armor_break","citywide_alert");S(d,"missionId",MissionId);S(d,"scenarioId",ScenarioId);S(d,"operationMapId",MapId);S(d,"displayNameKey","mission.citywide_alert.name");S(d,"displaySummaryKey","mission.citywide_alert.summary");S(d,"locationNameKey","mission.citywide_alert.location");foreach(string stage in new[]{"briefing","comms","debrief"})S(d,stage+"SequenceId","seq.ch05.m01."+(stage=="briefing"?"brief":stage));
            string[] names={"clinic","utility","perimeter"};string[] roles={"role.civilian.protected","role.civilian.protected","role.friendly.response.clinic"};int[] counts={2,2,11};
            A(d.FindProperty("objectives"),3,(p,i)=>{S(p,"objectiveId","obj.ch05.m01."+names[i]);S(p,"displayTextKey","mission.citywide_alert.objective."+names[i]);S(p,"rule",32+i);S(p,"missionRoleId",roles[i]);S(p,"requiredCount",counts[i]);S(p,"failureOnRuleBreak",i<2);});
            A(d.FindProperty("stars"),3,(p,i)=>{S(p,"starIndex",i+1);S(p,"rule",(int)(i==0?MissionStarRuleKind.CompleteMission:i==1?MissionStarRuleKind.NoSquadLoss:MissionStarRuleKind.NoCivilianLoss));S(p,"displayTextKey","mission.citywide_alert.star."+(i+1));S(p,"threshold",0);});
            A(d.FindProperty("firstClearRewards"),2,(p,i)=>{S(p,"kind",i==0?0:1);S(p,"rewardConfigId",i==0?"reward.commander_xp":string.Empty);S(p,"displayTextKey",i==0?"mission.reward.commander_xp":"mission.reward.credits");S(p,"amount",i==0?2500:11000);});
            d.ApplyModifiedPropertiesWithoutUndo();Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);EditorUtility.SetDirty(mission);
        }
    }
}
