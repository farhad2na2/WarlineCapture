using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH02M02SupplyLineConfigBuilder
    {
        public const string MissionId="saga.ch02.m02.supply_line", ScenarioId="scenario.ch02.m02.supply_line";
        public const string MapId="opmap.ch02.supply_yard_01", Prefix="anchor.ch02.m02.";
        public const string MissionPath="Assets/Game/Configs/Missions/Chapter02/MissionDefinition_Ch02_M02_SupplyLine.asset";
        public const string ScenarioPath="Assets/Game/Configs/Scenarios/Chapter02/ScenarioSetup_Ch02_M02_SupplyLine.asset";
        public const string MapPath="Assets/Game/Configs/OperationMaps/Chapter02/OperationMap_Ch02_SupplyYard01.asset";
        public static void Build()
        {
            foreach(string category in new[]{"Missions","Scenarios","OperationMaps"}) Directory.CreateDirectory("Assets/Game/Configs/"+category+"/Chapter02");
            AssetDatabase.Refresh();
            CH02M02SupplyLineEnvironmentBuilder.Build();BuildMap();BuildScenario();BuildMission();
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            Require(MissionDefinitionContractValidation.TryValidateCatalog(Load<MissionDefinitionCatalogConfig>(M01FirstContactConfigBuilder.CatalogPath),out string error),error);
            AssetDatabase.SaveAssets();Debug.Log("[SupplyLineConfig] result=Passed chapter=2 mission=2");
        }

        private static void BuildMap()
        {
            var map=Clone<OperationMapDefinition>(M03RadarWarningMapBuilder.Path,MapPath);
            var surface=Load<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(map.MapSurfaceDataReference.AssetGUID));
            Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob),"Gridlock surface unavailable");
            using(blob)
            {
                (string name,int x,int z,OperationMapAnchorKind kind,int faction,float radius)[] anchors={
                    ("oil",684,450,OperationMapAnchorKind.Objective,1,5),
                    ("refinery",737,457,OperationMapAnchorKind.Objective,1,5),
                    ("storage",796,450,OperationMapAnchorKind.Objective,1,5),
                    ("squad_a",695,426,OperationMapAnchorKind.Deployment,1,3),
                    ("squad_b",760,426,OperationMapAnchorKind.Deployment,1,3),
                    ("oil_hauler",704,426,OperationMapAnchorKind.Deployment,1,1),
                    ("fuel_hauler",779,426,OperationMapAnchorKind.Deployment,1,1),
                    ("screen_a",715,475,OperationMapAnchorKind.Spawn,2,3),
                    ("screen_b",785,475,OperationMapAnchorKind.Spawn,2,3),
                    ("return_rts",735,426,OperationMapAnchorKind.Camera,1,2),
                    ("alternate_lane",750,426,OperationMapAnchorKind.Lane,1,5)};
                var data=new SerializedObject(map);S(data,"operationMapId",MapId);
                data.FindProperty("additionalBuildingPlacements").objectReferenceValue=Load<MapBuildingPlacementConfig>(CH02M02SupplyLineEnvironmentBuilder.PlacementsPath);
                S(data,"planningCameraId","camera.ch02.m02.overview");S(data,"battleCameraId","camera.ch02.m02.battle");
                var bounds=data.FindProperty("bounds");bounds.FindPropertyRelative("playableMin").vector3Value=new Vector3(665,0,410);
                bounds.FindPropertyRelative("playableMax").vector3Value=new Vector3(830,100,490);MissionCameraBoundsAuthoring.Apply(bounds);
                var minimap=data.FindProperty("minimap");S(minimap,"minimapId","minimap.ch02.m02.supply_yard");
                minimap.FindPropertyRelative("projectionOrigin").vector3Value=new Vector3(665,0,410);minimap.FindPropertyRelative("projectionSize").vector2Value=new Vector2(165,80);
                A(data.FindProperty("anchors"),anchors.Length,(entry,i)=>
                {
                    var seed=anchors[i];Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(seed.x,seed.z),out var sample),seed.name);
                    S(entry,"anchorId",Prefix+seed.name);S(entry,"kind",(int)seed.kind);S(entry,"factionId",seed.faction);S(entry,"laneIndex",0);
                    entry.FindPropertyRelative("position").vector3Value=new Vector3(seed.x,sample.Height,seed.z);
                    entry.FindPropertyRelative("eulerAngles").vector3Value=Vector3.zero;entry.FindPropertyRelative("radius").floatValue=seed.radius;
                });
                A(data.FindProperty("cameras"),2,(entry,i)=>
                {
                    S(entry,"cameraId",i==0?"camera.ch02.m02.overview":"camera.ch02.m02.battle");
                    Vector3 focus=i==0?new Vector3(748,0,435):new Vector3(735,0,426);Vector3 position=focus+new Vector3(0,70,-40);
                    entry.FindPropertyRelative("position").vector3Value=position;entry.FindPropertyRelative("eulerAngles").vector3Value=Quaternion.LookRotation(focus-position).eulerAngles;
                    entry.FindPropertyRelative("fieldOfView").floatValue=55;
                });
                S(data,"contentHash",string.Empty);S(data,"generatedMetadataHash",string.Empty);
                data.ApplyModifiedPropertiesWithoutUndo();
                using var sha=SHA256.Create();string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(map)))).Replace("-","").ToLowerInvariant();
                data.Update();
                S(data,"contentHash",hash);S(data,"generatedMetadataHash",hash);data.ApplyModifiedPropertiesWithoutUndo();
                Require(map.TryValidateMetadata(out string error)&&map.TryValidateLocalContentReferences(out error),error);EditorUtility.SetDirty(map);AssetDatabase.SaveAssets();
            }
        }
        private static void BuildScenario()
        {
            var scenario=Clone<ScenarioSetupConfig>(M02EstablishBaseConfigBuilder.ScenarioPath,ScenarioPath);var data=new SerializedObject(scenario);
            S(data,"scenarioId",ScenarioId);S(data,"operationMapId",MapId);S(data,"deterministicSeed",2002001);S(data,"encounterStartMilliseconds",0);
            var map=Load<OperationMapDefinition>(MapPath);A(data.FindProperty("requiredAnchors"),map.Anchors.Length,(e,i)=>{S(e,"anchorId",map.Anchors[i].AnchorId);S(e,"kind",(int)map.Anchors[i].Kind);});
            foreach(string key in new[]{"buildingDisabled","productionDisabled","economyDisabled","transportDisabled","airDisabled"}) S(data.FindProperty("restrictions"),key,true);
            foreach(string mode in new[]{"missionRuntime","defense","extraction","breach","gridlock"}) S(data.FindProperty(mode),"enabled",false);
            S(data.FindProperty("missionRuntime"),"startingCredits",0);S(data.FindProperty("missionRuntime"),"startingMaterials",0);
            A(data.FindProperty("ambientPresentations"),0,null);A(data.FindProperty("unitGroups"),6,Group);
            A(data.FindProperty("patrolRoutes"),2,(e,i)=>
            {
                S(e,"routeId","route.ch02.m02.attack."+i);S(e,"unitGroupId","group.ch02.m02."+(i==0?"screen_a":"screen_b"));S(e,"startDelayMilliseconds",45000+i*45000);
                A(e.FindPropertyRelative("anchorIds"),2,(a,n)=>a.stringValue=Prefix+(n==0?(i==0?"screen_a":"screen_b"):(i==0?"oil":"storage")));
            });
            var g=data.FindProperty("supplyLine");S(g,"enabled",true);
            S(g,"oilAnchorId",Prefix+"oil");S(g,"refineryAnchorId",Prefix+"refinery");S(g,"storageAnchorId",Prefix+"storage");
            S(g,"alternateLaneAnchorId",Prefix+"alternate_lane");
            S(g,"oilBuildingId","SupplyLine_Building_OilPump");S(g,"refineryBuildingId","SupplyLine_Building_Refinery");S(g,"storageBuildingId","SupplyLine_Reserve_Depot");
            S(g,"reserveBarrels",40);S(g,"civilianReserveBarrels",20);S(g,"holdMilliseconds",20000);S(g,"deadlineMilliseconds",720000);
            data.ApplyModifiedPropertiesWithoutUndo();Require(scenario.TryValidate(out string error),error);EditorUtility.SetDirty(scenario);AssetDatabase.SaveAssets();
        }
        private static void Group(SerializedProperty group,int i)
        {
            string[] names={"squad_a","squad_b","oil_hauler","fuel_hauler","screen_a","screen_b"};
            S(group,"groupId","group.ch02.m02."+names[i]);S(group,"factionIndex",i>=4?2:1);
            string[] units=i switch {0 or 1=>new[]{"Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"},
                2=>new[]{"Veh_Truck_Tray"},3=>new[]{"Veh_Truck_Tanker"},_=>new[]{"Chr_Insurgent_Male_03","Chr_Insurgent_Female_01","Chr_Insurgent_Female_02"}};
            A(group.FindPropertyRelative("units"),units.Length,(unit,n)=>
            {
                string key="Unit_"+units[n],path="Assets/Game/Prefabs/"+(i==2 || i==3?"Vehicles/":"Characters/")+key+".prefab";Require(Load<GameObject>(path)!=null,path);
                S(unit,"unitConfigKey","unit.supply_line."+units[n].ToLowerInvariant());S(unit,"runtimePrefabSourceKey",key);S(unit,"expectedAssetGuid",AssetDatabase.AssetPathToGUID(path));
                S(unit,"count",1);S(unit,"spawnAnchorId",Prefix+names[i]);S(unit,"missionRoleId",i switch {2=>"role.supply.oil_hauler",3=>"role.supply.fuel_hauler",4 or 5=>"role.hostile.screen",_=>"role.friendly.command_squad"});
            });
        }
        private static void BuildMission()
        {
            var mission=Clone<MissionDefinitionConfig>(M02EstablishBaseConfigBuilder.MissionPath,MissionPath);var data=new SerializedObject(mission);
            S(data,"missionId",MissionId);S(data,"scenarioId",ScenarioId);S(data,"operationMapId",MapId);
            S(data,"displayNameKey","mission.supply_line.name");S(data,"displaySummaryKey","mission.supply_line.summary");S(data,"locationNameKey","mission.supply_line.location");
            foreach(string stage in new[]{"briefing","comms","debrief"}) S(data,stage+"SequenceId","seq.ch02.m02."+(stage=="briefing"?"brief":stage));
            string[] names={"oil","fuel","reserve"};
            A(data.FindProperty("objectives"),3,(e,i)=>{S(e,"objectiveId","obj.ch02.m02."+names[i]);S(e,"displayTextKey","mission.supply_line.objective."+names[i]);
                S(e,"rule",(int)MissionObjectiveRuleKind.TransferSupplyOil+i);S(e,"missionRoleId","role.supply."+names[i]);S(e,"targetConfigId",string.Empty);S(e,"requiredCount",i==2?40:1);S(e,"failureOnRuleBreak",true);});
            A(data.FindProperty("stars"),3,(e,i)=>{S(e,"starIndex",i+1);S(e,"rule",(int)(i==0?MissionStarRuleKind.CompleteMission:i==1?MissionStarRuleKind.NoSquadLoss:MissionStarRuleKind.CompleteUnderMilliseconds));S(e,"displayTextKey","mission.supply_line.star."+(i+1));S(e,"threshold",i==2?480000:0);});
            A(data.FindProperty("firstClearRewards"),2,(e,i)=>{S(e,"kind",i==0?0:1);S(e,"rewardConfigId",i==0?"reward.commander_xp":string.Empty);S(e,"displayTextKey",i==0?"mission.reward.commander_xp":"mission.reward.credits");S(e,"amount",i==0?800:4000);});
            A(data.FindProperty("replayRewards"),1,(e,i)=>{S(e,"kind",1);S(e,"rewardConfigId",string.Empty);S(e,"displayTextKey","mission.reward.credits");S(e,"amount",300);});
            data.ApplyModifiedPropertiesWithoutUndo();Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);EditorUtility.SetDirty(mission);AssetDatabase.SaveAssets();
        }
        private static T Load<T>(string path) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new InvalidOperationException(path);
        private static T Clone<T>(string source,string target) where T:ScriptableObject {var a=AssetDatabase.LoadAssetAtPath<T>(target);if(a==null){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,target);}EditorUtility.CopySerialized(Load<T>(source),a);a.name=System.IO.Path.GetFileNameWithoutExtension(target);return a;}
        private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}
        private static void A(SerializedProperty a,int n,Action<SerializedProperty,int> fill){a.arraySize=n;for(int i=0;i<n;i++)fill(a.GetArrayElementAtIndex(i),i);}
        private static void S(SerializedObject o,string n,object v)=>S(o.FindProperty(n),v);
        private static void S(SerializedProperty o,string n,object v)=>S(o.FindPropertyRelative(n),v);
        private static void S(SerializedProperty p,object v){switch(v){case string s:p.stringValue=s;break;case int i:p.intValue=i;break;case bool b:p.boolValue=b;break;default:throw new ArgumentException(p.propertyPath);}}
    }
}
