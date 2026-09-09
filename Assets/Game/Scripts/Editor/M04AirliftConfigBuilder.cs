using System;
using System.Collections.Generic;
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
    /// <summary>Rebuilds only M04's logical map and authoring data; never changes the shared physical map.</summary>
    public static class M04AirliftConfigBuilder
    {
        public const string MissionId="saga.ch01.m04.airlift", ScenarioId="scenario.ch01.m04.airlift";
        public const string MissionPath="Assets/Game/Configs/Missions/Chapter01/MissionDefinition_Ch01_M04_Airlift.asset";
        public const string ScenarioPath="Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M04_Airlift.asset";
        public const string MapPath="Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_Airlift01.asset";
        public const string MapId="opmap.ch01.airlift_01", Prefix="anchor.ch01.m04.";
        public const string PassengerRole="role.friendly.specialist", CarrierRole="role.friendly.rescue_apc", AircraftRole="role.friendly.airlift";
        public static void Build()
        {
            BuildMap(); BuildScenario(); BuildMission(); M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            Require(MissionDefinitionContractValidation.TryValidateCatalog(Load<MissionDefinitionCatalogConfig>(M02EstablishBaseConfigBuilder.MissionCatalogPath),out string error),error);
            Debug.Log("[M04AirliftConfig] result=Passed specialists=4 escorts=8 apc=1 helicopter=1 hostiles=4");
        }
        private static void BuildMap()
        {
            var map=Clone<OperationMapDefinition>(M03RadarWarningMapBuilder.Path,MapPath);
            var surface=Load<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(map.MapSurfaceDataReference.AssetGUID));
            Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob),"M04 surface unavailable");
            using(blob)
            {
                // Main east-west road surveyed in M03. M04 occupies a separate logical window/anchor set.
                (string id,int x,int z,OperationMapAnchorKind kind,int faction,float radius)[] seeds={
                    ("squad_a",940,426,OperationMapAnchorKind.Deployment,1,4),
                    ("squad_b",960,426,OperationMapAnchorKind.Deployment,1,4),
                    ("carrier",979,426,OperationMapAnchorKind.Deployment,1,4),
                    ("specialists",801,427,OperationMapAnchorKind.Civilian,1,4),
                    ("landing",1060,428,OperationMapAnchorKind.Deployment,1,14),
                    ("aircraft",1060,428,OperationMapAnchorKind.Deployment,1,8),
                    ("departure",1085,465,OperationMapAnchorKind.Camera,1,14),
                    ("return_rts",960,426,OperationMapAnchorKind.Camera,1,3),
                    ("hostiles",590,426,OperationMapAnchorKind.Spawn,2,5),
                    ("route_00",622,426,OperationMapAnchorKind.Lane,2,3),
                    ("route_01",705,427,OperationMapAnchorKind.Lane,2,3),
                    ("route_02",801,427,OperationMapAnchorKind.Lane,2,3),
                    ("route_03",876,426,OperationMapAnchorKind.Lane,2,3),
                    ("route_04",940,426,OperationMapAnchorKind.Lane,2,3),
                    ("route_05",1004,426,OperationMapAnchorKind.Lane,2,3),
                    ("route_06",1060,428,OperationMapAnchorKind.Lane,2,3)};
                var data=new SerializedObject(map); S(data,"operationMapId",MapId);
                S(data,"planningCameraId","camera.ch01.m04.planning"); S(data,"battleCameraId","camera.ch01.m04.battle");
                S(data.FindProperty("minimap"),"minimapId","minimap.ch01.m04.airlift");
                A(data.FindProperty("anchors"),seeds.Length,(entry,i)=>
                {
                    var seed=seeds[i]; Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(seed.x,seed.z),out var sample),seed.id);
                    S(entry,"anchorId",Prefix+seed.id); S(entry,"kind",(int)seed.kind); S(entry,"factionId",seed.faction); S(entry,"laneIndex",0);
                    entry.FindPropertyRelative("position").vector3Value=new Vector3(seed.x,sample.Height,seed.z);
                    entry.FindPropertyRelative("eulerAngles").vector3Value=Vector3.zero; entry.FindPropertyRelative("radius").floatValue=seed.radius;
                });
                A(data.FindProperty("cameras"),2,(entry,i)=>
                {
                    S(entry,"cameraId","camera.ch01.m04."+(i==0?"planning":"battle"));
                    var pos=new Vector3(960,i==0?95:78,i==0?480:470);
                    entry.FindPropertyRelative("position").vector3Value=pos;
                    entry.FindPropertyRelative("eulerAngles").vector3Value=Quaternion.LookRotation(new Vector3(960,0,426)-pos).eulerAngles;
                    entry.FindPropertyRelative("fieldOfView").floatValue=58;
                });
                using var sha=SHA256.Create();
                string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes("M04-Airlift-v1:"+map.ContentHash+":road801-1060:departure1085-465"))).Replace("-",string.Empty).ToLowerInvariant();
                S(data,"contentHash",hash); S(data,"generatedMetadataHash",hash); data.ApplyModifiedPropertiesWithoutUndo();
                Require(map.TryValidateMetadata(out string error)&&map.TryValidateLocalContentReferences(out error),error);
                EditorUtility.SetDirty(map); AssetDatabase.SaveAssets();
            }
        }
        private static void BuildScenario()
        {
            var scenario=Clone<ScenarioSetupConfig>(M02EstablishBaseConfigBuilder.ScenarioPath,ScenarioPath);
            var data=new SerializedObject(scenario); S(data,"scenarioId",ScenarioId); S(data,"operationMapId",MapId);
            S(data,"deterministicSeed",4004001); S(data,"encounterStartMilliseconds",120000);
            var map=Load<OperationMapDefinition>(MapPath);
            A(data.FindProperty("requiredAnchors"),map.Anchors.Length,(entry,i)=>{S(entry,"anchorId",map.Anchors[i].AnchorId);S(entry,"kind",(int)map.Anchors[i].Kind);});
            var restrictions=data.FindProperty("restrictions");
            foreach(string name in new[]{"buildingDisabled","productionDisabled","economyDisabled"}) S(restrictions,name,true);
            S(restrictions,"transportDisabled",false); S(restrictions,"airDisabled",false);
            A(data.FindProperty("ambientPresentations"),0,null); S(data.FindProperty("defense"),"enabled",false);
            var runtime=data.FindProperty("missionRuntime"); S(runtime,"enabled",false); S(runtime,"baseAnchorId",Prefix+"return_rts");
            S(runtime,"startingCredits",0); S(runtime,"startingMaterials",0);
            A(data.FindProperty("unitGroups"),6,Group);
            A(data.FindProperty("patrolRoutes"),1,(entry,i)=>
            {
                S(entry,"routeId","route.ch01.m04.pursuit"); S(entry,"unitGroupId","group.ch01.m04.hostiles"); S(entry,"startDelayMilliseconds",120000);
                A(entry.FindPropertyRelative("anchorIds"),7,(a,n)=>a.stringValue=Prefix+"route_"+n.ToString("00"));
            });
            var extraction=data.FindProperty("extraction"); S(extraction,"enabled",true);
            S(extraction,"passengerRoleId",PassengerRole); S(extraction,"carrierRoleId",CarrierRole); S(extraction,"aircraftRoleId",AircraftRole);
            S(extraction,"rescueAnchorId",Prefix+"specialists"); S(extraction,"landingAnchorId",Prefix+"landing"); S(extraction,"departureAnchorId",Prefix+"departure");
            S(extraction,"requiredPassengers",4); S(extraction,"secureHoldMilliseconds",20000); S(extraction,"deadlineMilliseconds",600000);
            extraction.FindPropertyRelative("landingRadius").floatValue=18; extraction.FindPropertyRelative("departureRadius").floatValue=14;
            S(extraction,"vehiclesSelfSupplied",true); S(extraction,"authoredMapDefensesDormant",true);
            var tour=extraction.FindPropertyRelative("cameraTour"); S(tour,"StartHoldMilliseconds",750);S(tour,"PostHoldMilliseconds",1200);
            S(tour,"ApproachHoldMilliseconds",1500);S(tour,"ReturnHoldMilliseconds",500);tour.FindPropertyRelative("SmoothTimeSeconds").floatValue=.65f;
            tour.FindPropertyRelative("PostPerspective").vector4Value=new Vector4(48,65,0,55);tour.FindPropertyRelative("ApproachPerspective").vector4Value=new Vector4(55,67,0,55);
            data.ApplyModifiedPropertiesWithoutUndo(); Require(scenario.TryValidate(out string error),error); EditorUtility.SetDirty(scenario); AssetDatabase.SaveAssets();
        }
        private static void Group(SerializedProperty group,int i)
        {
            string[] names={"squad_a","squad_b","carrier","specialists","aircraft","hostiles"};
            S(group,"groupId","group.ch01.m04."+names[i]); S(group,"factionIndex",i==5?2:1);
            string[] prefabs=i switch {
                0 or 1 => new[]{"Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"},
                2=>new[]{"Veh_APC_Fast"}, 3=>new[]{"Chr_Civilian_Female_01","Chr_Civilian_Female_02","Chr_Civilian_Male_01","Chr_Civilian_Male_02"},
                4=>new[]{"Veh_Helicopter_Transport"},_=>new[]{"Chr_Insurgent_Male_03","Chr_Insurgent_Female_01","Chr_Insurgent_Male_03","Chr_Insurgent_Female_02"}};
            A(group.FindPropertyRelative("units"),prefabs.Length,(unit,n)=>
            {
                string key="Unit_"+prefabs[n]; string path="Assets/Game/Prefabs/"+(i is 2 or 4?"Vehicles/":"Characters/")+key+".prefab";
                Require(Load<GameObject>(path)!=null,path); S(unit,"unitConfigKey","unit.m04."+prefabs[n].ToLowerInvariant());
                S(unit,"runtimePrefabSourceKey",key); S(unit,"expectedAssetGuid",AssetDatabase.AssetPathToGUID(path)); S(unit,"count",1);
                S(unit,"spawnAnchorId",Prefix+names[i]); S(unit,"missionRoleId",i switch {2=>CarrierRole,3=>PassengerRole,4=>AircraftRole,5=>"role.hostile.pursuit",_=>"role.friendly.command_squad"});
            });
        }
        private static void BuildMission()
        {
            var mission=Clone<MissionDefinitionConfig>(M02EstablishBaseConfigBuilder.MissionPath,MissionPath); var data=new SerializedObject(mission);
            S(data,"missionId",MissionId); S(data,"scenarioId",ScenarioId); S(data,"operationMapId",MapId);
            S(data,"displayNameKey","mission.m04.name");S(data,"displaySummaryKey","mission.m04.summary");S(data,"locationNameKey","mission.m04.location");
            foreach(string stage in new[]{"briefing","comms","debrief"}) S(data,stage+"SequenceId","seq.ch01.m04."+(stage=="briefing"?"brief":stage));
            string[] names={"extract","transport","landing"};
            MissionObjectiveRuleKind[] rules={MissionObjectiveRuleKind.ExtractPassengers,MissionObjectiveRuleKind.ProtectExtractionTransport,MissionObjectiveRuleKind.SecureLandingZone};
            A(data.FindProperty("objectives"),3,(entry,i)=>{S(entry,"objectiveId","obj.ch01.m04."+names[i]);S(entry,"displayTextKey","mission.m04.objective."+names[i]);
                S(entry,"rule",(int)rules[i]);S(entry,"missionRoleId",i==0?PassengerRole:i==1?CarrierRole:AircraftRole);S(entry,"targetConfigId",string.Empty);S(entry,"requiredCount",i==0?4:1);S(entry,"failureOnRuleBreak",i!=2);});
            MissionStarRuleKind[] stars={MissionStarRuleKind.CompleteMission,MissionStarRuleKind.NoSquadLoss,MissionStarRuleKind.CompleteUnderMilliseconds};
            A(data.FindProperty("stars"),3,(entry,i)=>{S(entry,"starIndex",i+1);S(entry,"rule",(int)stars[i]);S(entry,"displayTextKey","mission.m04.star."+(i+1));S(entry,"threshold",i==2?420000:0);});
            A(data.FindProperty("firstClearRewards"),4,(entry,i)=>{S(entry,"kind",i==1?1:0);S(entry,"rewardConfigId",i switch {0=>"reward.commander_xp",1=>string.Empty,2=>"reward.ch01.m04.laila_unlock",_=>"reward.ch01.m04.transport_unlock"});
                S(entry,"displayTextKey",i switch {0=>"mission.reward.commander_xp",1=>"mission.reward.credits",2=>"mission.m04.reward.laila",_=>"mission.m04.reward.transport"});S(entry,"amount",i==0?500:i==1?2500:1);});
            data.ApplyModifiedPropertiesWithoutUndo(); Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);EditorUtility.SetDirty(mission);AssetDatabase.SaveAssets();
        }
        private static T Load<T>(string path) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path)??throw new InvalidOperationException(path);
        private static T Clone<T>(string source,string target) where T:ScriptableObject {var asset=AssetDatabase.LoadAssetAtPath<T>(target);if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,target);}EditorUtility.CopySerialized(Load<T>(source),asset);return asset;}
        private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}
        private static void A(SerializedProperty array,int count,Action<SerializedProperty,int> fill){array.arraySize=count;for(int i=0;i<count;i++)fill(array.GetArrayElementAtIndex(i),i);}
        private static void S(SerializedObject data,string name,object value)=>S(data.FindProperty(name),value);
        private static void S(SerializedProperty data,string name,object value)=>S(data.FindPropertyRelative(name),value);
        private static void S(SerializedProperty property,object value){switch(value){case string s:property.stringValue=s;break;case int i:property.intValue=i;break;case bool b:property.boolValue=b;break;default:throw new ArgumentException(property.propertyPath);}}
    }
}
