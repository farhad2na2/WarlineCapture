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
    public static class CH02M04PowerRelayConfigBuilder
    {
        public const string MissionId=CampaignMissionSequence.PowerRelay,ScenarioId="scenario.ch02.m04.power_relay",MapId="opmap.ch02.power_relay_01",Prefix="anchor.ch02.m04.";
        public const string MissionPath="Assets/Game/Configs/Missions/Chapter02/MissionDefinition_Ch02_M04_PowerRelay.asset";
        public const string ScenarioPath="Assets/Game/Configs/Scenarios/Chapter02/ScenarioSetup_Ch02_M04_PowerRelay.asset";
        public const string MapPath="Assets/Game/Configs/OperationMaps/Chapter02/OperationMap_Ch02_PowerRelay01.asset";
        [MenuItem("Game/Campaign/Power Relay/Build Configuration")]
        public static void Build()
        {
            foreach(string category in new[]{"Missions","Scenarios","OperationMaps"})Directory.CreateDirectory("Assets/Game/Configs/"+category+"/Chapter02");
            AssetDatabase.Refresh();BuildMap();BuildScenario();BuildMission();M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            Require(MissionDefinitionContractValidation.TryValidateCatalog(Load<MissionDefinitionCatalogConfig>(M01FirstContactConfigBuilder.CatalogPath),out string error),error);
            AssetDatabase.SaveAssets();Debug.Log("[PowerRelayConfig] result=Passed chapter=2 mission=4 controls=Select,Move,Attack,Hold");
        }
        private static void BuildMap()
        {
            var map=Clone<OperationMapDefinition>(CH02M03MarketLifelineConfigBuilder.MapPath,MapPath);var surface=Load<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(map.MapSurfaceDataReference.AssetGUID));
            Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob),"Power Relay surface unavailable");
            using(blob)
            {
                (string name,int x,int z,OperationMapAnchorKind kind,int faction,float radius)[] anchors={
                    ("short_route",724,449,OperationMapAnchorKind.Objective,1,6),("safe_route",700,466,OperationMapAnchorKind.Objective,1,7),("shelter",682,452,OperationMapAnchorKind.Objective,1,8),("repair",790,446,OperationMapAnchorKind.Objective,1,8),
                    ("squad_a",700,442,OperationMapAnchorKind.Deployment,1,3),("squad_b",710,438,OperationMapAnchorKind.Deployment,1,3),("engineers",754,430,OperationMapAnchorKind.Deployment,1,3),("families",715,455,OperationMapAnchorKind.Deployment,1,.35f),("fuel",746,420,OperationMapAnchorKind.Deployment,1,.35f),
                    ("hostile_a",783,474,OperationMapAnchorKind.Spawn,2,3),("hostile_b",805,442,OperationMapAnchorKind.Spawn,2,3),("return_rts",746,440,OperationMapAnchorKind.Camera,1,2)};
                var data=new SerializedObject(map);S(data,"operationMapId",MapId);data.FindProperty("additionalBuildingPlacements").objectReferenceValue=null;S(data,"planningCameraId","camera.ch02.m04.overview");S(data,"battleCameraId","camera.ch02.m04.battle");
                var bounds=data.FindProperty("bounds");bounds.FindPropertyRelative("playableMin").vector3Value=new Vector3(665,0,410);bounds.FindPropertyRelative("playableMax").vector3Value=new Vector3(830,100,495);MissionCameraBoundsAuthoring.Apply(bounds);
                var minimap=data.FindProperty("minimap");S(minimap,"minimapId","minimap.ch02.m04.power_relay");minimap.FindPropertyRelative("projectionOrigin").vector3Value=new Vector3(665,0,410);minimap.FindPropertyRelative("projectionSize").vector2Value=new Vector2(165,85);
                A(data.FindProperty("anchors"),anchors.Length,(entry,i)=>{var seed=anchors[i];int2 cell=RequiresVehicleFootprint(seed.name)?ResolveVehicleCell(ref blob,new int2(seed.x,seed.z),24):new int2(seed.x,seed.z);Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,cell,out var sample),seed.name);S(entry,"anchorId",Prefix+seed.name);S(entry,"kind",(int)seed.kind);S(entry,"factionId",seed.faction);S(entry,"laneIndex",0);entry.FindPropertyRelative("position").vector3Value=new Vector3(cell.x,sample.Height,cell.y);entry.FindPropertyRelative("eulerAngles").vector3Value=Vector3.zero;entry.FindPropertyRelative("radius").floatValue=seed.radius;Debug.Log($"[PowerRelayConfig] anchor={seed.name} requested=({seed.x},{seed.z}) resolved={cell} vehicleFootprint={(RequiresVehicleFootprint(seed.name)?"3x3":"none")}");});
                A(data.FindProperty("cameras"),3,(entry,i)=>{S(entry,"cameraId",i==0?"camera.ch02.m04.overview":i==1?"camera.ch02.m04.battle":"camera.ch02.m04.relay");Vector3 focus=i==0?new Vector3(744,0,447):i==1?new Vector3(790,0,454):new Vector3(790,0,446);Vector3 position=focus+new Vector3(0,i==0?78:54,-42);entry.FindPropertyRelative("position").vector3Value=position;entry.FindPropertyRelative("eulerAngles").vector3Value=Quaternion.LookRotation(focus-position).eulerAngles;entry.FindPropertyRelative("fieldOfView").floatValue=i==0?58:51;});
                S(data,"contentHash",string.Empty);S(data,"generatedMetadataHash",string.Empty);data.ApplyModifiedPropertiesWithoutUndo();using var sha=SHA256.Create();string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(map)))).Replace("-","").ToLowerInvariant();data.Update();S(data,"contentHash",hash);S(data,"generatedMetadataHash",hash);data.ApplyModifiedPropertiesWithoutUndo();
                Require(map.TryValidateMetadata(out string error)&&map.TryValidateLocalContentReferences(out error),error);EditorUtility.SetDirty(map);AssetDatabase.SaveAssets();
            }
        }
        private static void BuildScenario()
        {
            var scenario=Clone<ScenarioSetupConfig>(CH02M03MarketLifelineConfigBuilder.ScenarioPath,ScenarioPath);var data=new SerializedObject(scenario);S(data,"scenarioId",ScenarioId);S(data,"operationMapId",MapId);S(data,"deterministicSeed",2004001);S(data,"encounterStartMilliseconds",0);
            var map=Load<OperationMapDefinition>(MapPath);A(data.FindProperty("requiredAnchors"),map.Anchors.Length,(e,i)=>{S(e,"anchorId",map.Anchors[i].AnchorId);S(e,"kind",(int)map.Anchors[i].Kind);});
            foreach(string key in new[]{"buildingDisabled","productionDisabled","economyDisabled","transportDisabled","airDisabled"})S(data.FindProperty("restrictions"),key,true);
            foreach(string mode in new[]{"missionRuntime","defense","extraction","breach","gridlock","supplyLine","marketLifeline"})S(data.FindProperty(mode),"enabled",false);
            A(data.FindProperty("ambientPresentations"),0,null);A(data.FindProperty("unitGroups"),7,Group);A(data.FindProperty("patrolRoutes"),0,null);
            var power=data.FindProperty("powerRelay");S(power,"enabled",true);S(power,"shortRouteAnchorId",Prefix+"short_route");S(power,"safeRouteAnchorId",Prefix+"safe_route");S(power,"shelterAnchorId",Prefix+"shelter");S(power,"repairAnchorId",Prefix+"repair");S(power,"repairHoldMilliseconds",8000);S(power,"victoryHoldMilliseconds",10000);S(power,"deadlineMilliseconds",600000);power.FindPropertyRelative("routeRadius").floatValue=7;power.FindPropertyRelative("shelterRadius").floatValue=8;power.FindPropertyRelative("repairRadius").floatValue=8;
            var tour=power.FindPropertyRelative("cameraTour");S(tour,"StartHoldMilliseconds",800);S(tour,"PostHoldMilliseconds",1200);S(tour,"ApproachHoldMilliseconds",1700);S(tour,"ReturnHoldMilliseconds",500);tour.FindPropertyRelative("SmoothTimeSeconds").floatValue=.7f;tour.FindPropertyRelative("PostPerspective").vector4Value=new Vector4(52,67,0,55);tour.FindPropertyRelative("ApproachPerspective").vector4Value=new Vector4(48,69,0,52);
            data.ApplyModifiedPropertiesWithoutUndo();Require(scenario.TryValidate(out string error),error);EditorUtility.SetDirty(scenario);AssetDatabase.SaveAssets();
        }
        private static void Group(SerializedProperty group,int i)
        {
            string[] names={"squad_a","squad_b","engineers","families","fuel","hostile_a","hostile_b"};S(group,"groupId","group.ch02.m04."+names[i]);S(group,"factionIndex",i>=5?2:1);
            string[] units=i switch {0 or 1=>new[]{"Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"},2=>new[]{"Chr_Civilian_Female_01","Chr_Civilian_Female_02"},3=>new[]{"Veh_Truck_Canopy"},4=>new[]{"Veh_Truck_Tanker"},_=>new[]{"Chr_Insurgent_Male_03","Chr_Insurgent_Female_01","Chr_Insurgent_Female_02"}};
            A(group.FindPropertyRelative("units"),units.Length,(unit,n)=>{string key="Unit_"+units[n],path="Assets/Game/Prefabs/"+(i is 3 or 4?"Vehicles/":"Characters/")+key+".prefab";Require(Load<GameObject>(path)!=null,path);S(unit,"unitConfigKey","unit.power."+units[n].ToLowerInvariant()+"."+i+"."+n);S(unit,"runtimePrefabSourceKey",key);S(unit,"expectedAssetGuid",AssetDatabase.AssetPathToGUID(path));S(unit,"count",1);S(unit,"spawnAnchorId",Prefix+names[i]);S(unit,"missionRoleId",i==2?"role.power.engineer":i==3?"role.power.family_convoy":i==4?"role.power.fuel_service":i>=5?"role.power.hostile":"role.power.rifle");});
        }
        private static void BuildMission()
        {
            var mission=Clone<MissionDefinitionConfig>(CH02M03MarketLifelineConfigBuilder.MissionPath,MissionPath);var data=new SerializedObject(mission);S(data,"missionId",MissionId);S(data,"scenarioId",ScenarioId);S(data,"operationMapId",MapId);S(data,"displayNameKey","mission.power_relay.name");S(data,"displaySummaryKey","mission.power_relay.summary");S(data,"locationNameKey","mission.power_relay.location");foreach(string stage in new[]{"briefing","comms","debrief"})S(data,stage+"SequenceId","seq.ch02.m04."+(stage=="briefing"?"brief":stage));
            string[] names={"shelter","restore","secure"};MissionObjectiveRuleKind[] rules={MissionObjectiveRuleKind.EscortPowerRelayFamilies,MissionObjectiveRuleKind.RestorePowerRelay,MissionObjectiveRuleKind.SecurePowerRelay};A(data.FindProperty("objectives"),3,(e,i)=>{S(e,"objectiveId","obj.ch02.m04."+names[i]);S(e,"displayTextKey","mission.power_relay.objective."+names[i]);S(e,"rule",(int)rules[i]);S(e,"missionRoleId",i==0?"role.power.family_convoy":i==1?"role.power.engineer":"role.power.rifle");S(e,"targetConfigId",string.Empty);S(e,"requiredCount",i==1?2:1);S(e,"failureOnRuleBreak",true);});
            A(data.FindProperty("stars"),3,(e,i)=>{S(e,"starIndex",i+1);S(e,"rule",(int)(i==0?MissionStarRuleKind.CompleteMission:i==1?MissionStarRuleKind.NoCivilianLoss:MissionStarRuleKind.CompleteUnderMilliseconds));S(e,"displayTextKey","mission.power_relay.star."+(i+1));S(e,"threshold",i==2?420000:0);});
            A(data.FindProperty("firstClearRewards"),2,(e,i)=>{S(e,"kind",i==0?0:1);S(e,"rewardConfigId",i==0?"reward.commander_xp":string.Empty);S(e,"displayTextKey",i==0?"mission.reward.commander_xp":"mission.reward.credits");S(e,"amount",i==0?1000:5000);});A(data.FindProperty("replayRewards"),1,(e,i)=>{S(e,"kind",1);S(e,"rewardConfigId",string.Empty);S(e,"displayTextKey","mission.reward.credits");S(e,"amount",400);});
            data.ApplyModifiedPropertiesWithoutUndo();Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);EditorUtility.SetDirty(mission);AssetDatabase.SaveAssets();
        }
        private static bool RequiresVehicleFootprint(string anchorName)=>anchorName is "short_route" or "safe_route" or "shelter" or "repair" or "families" or "fuel";
        private static int2 ResolveVehicleCell(ref BlobAssetReference<MapSurfaceBlob> blob,int2 desired,int maxRadius)
        {
            ref MapSurfaceBlob value=ref blob.Value;
            var surface=new MapSurfaceComponent {SurfaceBlob=blob,GridOrigin=value.GridOrigin,CellSize=value.CellSize,Dimensions=value.Dimensions,HasSurfaceData=1};
            var grid=new GridConfig {Width=value.Dimensions.x,Height=value.Dimensions.y,CellSize=value.CellSize,Origin=value.GridOrigin};
            var validation=new MapSurfaceTraversalValidation();
            if(validation.CanTraverseFootprint(surface,1,grid,desired,new int2(3,3),true))return desired;
            for(int radius=1;radius<=maxRadius;radius++)
            {
                for(int x=-radius;x<=radius;x++)
                {
                    int2 top=desired+new int2(x,radius);if(validation.CanTraverseFootprint(surface,1,grid,top,new int2(3,3),true))return top;
                    int2 bottom=desired+new int2(x,-radius);if(validation.CanTraverseFootprint(surface,1,grid,bottom,new int2(3,3),true))return bottom;
                }
                for(int z=-radius+1;z<radius;z++)
                {
                    int2 right=desired+new int2(radius,z);if(validation.CanTraverseFootprint(surface,1,grid,right,new int2(3,3),true))return right;
                    int2 left=desired+new int2(-radius,z);if(validation.CanTraverseFootprint(surface,1,grid,left,new int2(3,3),true))return left;
                }
            }
            throw new InvalidOperationException($"No traversable 3x3 vehicle surface within {maxRadius} cells of {desired}.");
        }
        private static T Load<T>(string path) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new InvalidOperationException(path);
        private static T Clone<T>(string source,string target) where T:ScriptableObject{var asset=AssetDatabase.LoadAssetAtPath<T>(target);if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,target);}EditorUtility.CopySerialized(Load<T>(source),asset);asset.name=Path.GetFileNameWithoutExtension(target);return asset;}
        private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}private static void A(SerializedProperty a,int n,Action<SerializedProperty,int> fill){a.arraySize=n;for(int i=0;i<n;i++)fill?.Invoke(a.GetArrayElementAtIndex(i),i);}private static void S(SerializedObject o,string n,object v)=>S(o.FindProperty(n),v);private static void S(SerializedProperty o,string n,object v)=>S(o.FindPropertyRelative(n),v);private static void S(SerializedProperty p,object v){switch(v){case string s:p.stringValue=s;break;case int i:p.intValue=i;break;case bool b:p.boolValue=b;break;default:throw new ArgumentException(p.propertyPath);}}
    }
}
