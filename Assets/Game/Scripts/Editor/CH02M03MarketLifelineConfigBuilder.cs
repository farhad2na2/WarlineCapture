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
    public static class CH02M03MarketLifelineConfigBuilder
    {
        public const string MissionId=CampaignMissionSequence.MarketLifeline,ScenarioId="scenario.ch02.m03.market_lifeline",MapId="opmap.ch02.old_market_01",Prefix="anchor.ch02.m03.";
        public const string MissionPath="Assets/Game/Configs/Missions/Chapter02/MissionDefinition_Ch02_M03_MarketLifeline.asset";
        public const string ScenarioPath="Assets/Game/Configs/Scenarios/Chapter02/ScenarioSetup_Ch02_M03_MarketLifeline.asset";
        public const string MapPath="Assets/Game/Configs/OperationMaps/Chapter02/OperationMap_Ch02_OldMarket01.asset";
        [MenuItem("Game/Campaign/Market Lifeline/Build Configuration")]
        public static void Build()
        {
            foreach(string category in new[]{"Missions","Scenarios","OperationMaps"})Directory.CreateDirectory("Assets/Game/Configs/"+category+"/Chapter02");
            AssetDatabase.Refresh();BuildMap();BuildScenario();BuildMission();M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            Require(MissionDefinitionContractValidation.TryValidateCatalog(Load<MissionDefinitionCatalogConfig>(M01FirstContactConfigBuilder.CatalogPath),out string error),error);
            AssetDatabase.SaveAssets();Debug.Log("[MarketLifelineConfig] result=Passed chapter=2 mission=3 controls=Select,Move,Attack,Hold");
        }
        private static void BuildMap()
        {
            var map=Clone<OperationMapDefinition>(CH02M02SupplyLineConfigBuilder.MapPath,MapPath);var surface=Load<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(map.MapSurfaceDataReference.AssetGUID));
            Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob),"Market surface unavailable");
            using(blob)
            {
                (string name,int x,int z,OperationMapAnchorKind kind,int faction,float radius)[] anchors={
                    ("delivery",797,451,OperationMapAnchorKind.Objective,1,8),("manifest",698,440,OperationMapAnchorKind.Objective,1,6),("corrupt_manifest",748,462,OperationMapAnchorKind.Objective,1,6),
                    ("convoy",684,426,OperationMapAnchorKind.Deployment,1,4),("corrupt_convoy",738,456,OperationMapAnchorKind.Deployment,1,3),("squad_a",699,430,OperationMapAnchorKind.Deployment,1,3),("squad_b",715,430,OperationMapAnchorKind.Deployment,1,3),
                    ("hostile_a",786,478,OperationMapAnchorKind.Spawn,2,3),("hostile_b",731,481,OperationMapAnchorKind.Spawn,2,3),("return_rts",741,434,OperationMapAnchorKind.Camera,1,2)};
                var data=new SerializedObject(map);S(data,"operationMapId",MapId);data.FindProperty("additionalBuildingPlacements").objectReferenceValue=null;
                S(data,"planningCameraId","camera.ch02.m03.overview");S(data,"battleCameraId","camera.ch02.m03.battle");
                var bounds=data.FindProperty("bounds");bounds.FindPropertyRelative("playableMin").vector3Value=new Vector3(665,0,410);bounds.FindPropertyRelative("playableMax").vector3Value=new Vector3(830,100,495);MissionCameraBoundsAuthoring.Apply(bounds);
                var minimap=data.FindProperty("minimap");S(minimap,"minimapId","minimap.ch02.m03.old_market");minimap.FindPropertyRelative("projectionOrigin").vector3Value=new Vector3(665,0,410);minimap.FindPropertyRelative("projectionSize").vector2Value=new Vector2(165,85);
                A(data.FindProperty("anchors"),anchors.Length,(entry,i)=>{var seed=anchors[i];Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(seed.x,seed.z),out var sample),seed.name);S(entry,"anchorId",Prefix+seed.name);S(entry,"kind",(int)seed.kind);S(entry,"factionId",seed.faction);S(entry,"laneIndex",0);entry.FindPropertyRelative("position").vector3Value=new Vector3(seed.x,sample.Height,seed.z);entry.FindPropertyRelative("eulerAngles").vector3Value=Vector3.zero;entry.FindPropertyRelative("radius").floatValue=seed.radius;});
                A(data.FindProperty("cameras"),3,(entry,i)=>{S(entry,"cameraId",i==0?"camera.ch02.m03.overview":i==1?"camera.ch02.m03.battle":"camera.ch02.m03.manifest");Vector3 focus=i==0?new Vector3(745,0,448):i==1?new Vector3(782,0,452):new Vector3(748,0,462);Vector3 position=focus+new Vector3(0,i==0?76:55,-40);entry.FindPropertyRelative("position").vector3Value=position;entry.FindPropertyRelative("eulerAngles").vector3Value=Quaternion.LookRotation(focus-position).eulerAngles;entry.FindPropertyRelative("fieldOfView").floatValue=i==0?58:52;});
                S(data,"contentHash",string.Empty);S(data,"generatedMetadataHash",string.Empty);data.ApplyModifiedPropertiesWithoutUndo();using var sha=SHA256.Create();string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(map)))).Replace("-","").ToLowerInvariant();data.Update();S(data,"contentHash",hash);S(data,"generatedMetadataHash",hash);data.ApplyModifiedPropertiesWithoutUndo();
                Require(map.TryValidateMetadata(out string error)&&map.TryValidateLocalContentReferences(out error),error);EditorUtility.SetDirty(map);AssetDatabase.SaveAssets();
            }
        }
        private static void BuildScenario()
        {
            var scenario=Clone<ScenarioSetupConfig>(CH02M02SupplyLineConfigBuilder.ScenarioPath,ScenarioPath);var data=new SerializedObject(scenario);S(data,"scenarioId",ScenarioId);S(data,"operationMapId",MapId);S(data,"deterministicSeed",2003001);S(data,"encounterStartMilliseconds",0);
            var map=Load<OperationMapDefinition>(MapPath);A(data.FindProperty("requiredAnchors"),map.Anchors.Length,(e,i)=>{S(e,"anchorId",map.Anchors[i].AnchorId);S(e,"kind",(int)map.Anchors[i].Kind);});
            foreach(string key in new[]{"buildingDisabled","productionDisabled","economyDisabled","transportDisabled","airDisabled"})S(data.FindProperty("restrictions"),key,true);
            foreach(string mode in new[]{"missionRuntime","defense","extraction","breach","gridlock","supplyLine"})S(data.FindProperty(mode),"enabled",false);
            A(data.FindProperty("ambientPresentations"),0,null);A(data.FindProperty("unitGroups"),6,Group);
            A(data.FindProperty("patrolRoutes"),2,(e,i)=>{S(e,"routeId","route.ch02.m03.hostile."+i);S(e,"unitGroupId","group.ch02.m03.hostile_"+(i==0?"a":"b"));S(e,"startDelayMilliseconds",35000+i*35000);A(e.FindPropertyRelative("anchorIds"),2,(a,n)=>a.stringValue=Prefix+(n==0?(i==0?"hostile_a":"hostile_b"):(i==0?"delivery":"corrupt_manifest")));});
            var market=data.FindProperty("marketLifeline");S(market,"enabled",true);S(market,"deliveryAnchorId",Prefix+"delivery");S(market,"manifestAnchorId",Prefix+"manifest");S(market,"corruptManifestAnchorId",Prefix+"corrupt_manifest");S(market,"requiredDeliveries",3);S(market,"manifestHoldMilliseconds",6000);S(market,"victoryHoldMilliseconds",10000);S(market,"deadlineMilliseconds",600000);market.FindPropertyRelative("deliveryRadius").floatValue=8;market.FindPropertyRelative("manifestRadius").floatValue=6;
            var tour=market.FindPropertyRelative("cameraTour");S(tour,"StartHoldMilliseconds",800);S(tour,"PostHoldMilliseconds",1100);S(tour,"ApproachHoldMilliseconds",1600);S(tour,"ReturnHoldMilliseconds",500);tour.FindPropertyRelative("SmoothTimeSeconds").floatValue=.7f;tour.FindPropertyRelative("PostPerspective").vector4Value=new Vector4(52,65,0,55);tour.FindPropertyRelative("ApproachPerspective").vector4Value=new Vector4(48,68,0,52);
            data.ApplyModifiedPropertiesWithoutUndo();Require(scenario.TryValidate(out string error),error);EditorUtility.SetDirty(scenario);AssetDatabase.SaveAssets();
        }
        private static void Group(SerializedProperty group,int i)
        {
            string[] names={"squad_a","squad_b","convoy","corrupt_convoy","hostile_a","hostile_b"};S(group,"groupId","group.ch02.m03."+names[i]);S(group,"factionIndex",i>=4?2:1);
            string[] units=i switch {0 or 1=>new[]{"Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"},2=>new[]{"Veh_Truck_Tray","Veh_Truck_Tray","Veh_Truck_Canopy"},3=>new[]{"Veh_Truck_Tray"},_=>new[]{"Chr_Insurgent_Male_03","Chr_Insurgent_Female_01","Chr_Insurgent_Female_02"}};
            A(group.FindPropertyRelative("units"),units.Length,(unit,n)=>{string key="Unit_"+units[n],path="Assets/Game/Prefabs/"+(i is 2 or 3?"Vehicles/":"Characters/")+key+".prefab";Require(Load<GameObject>(path)!=null,path);S(unit,"unitConfigKey","unit.market."+units[n].ToLowerInvariant()+"."+i+"."+n);S(unit,"runtimePrefabSourceKey",key);S(unit,"expectedAssetGuid",AssetDatabase.AssetPathToGUID(path));S(unit,"count",1);S(unit,"spawnAnchorId",Prefix+names[i]);S(unit,"missionRoleId",i==2?"role.market.relief_convoy":i==3?"role.market.corrupt_transfer":i>=4?"role.market.hostile":"role.market.rifle");});
        }
        private static void BuildMission()
        {
            var mission=Clone<MissionDefinitionConfig>(CH02M02SupplyLineConfigBuilder.MissionPath,MissionPath);var data=new SerializedObject(mission);S(data,"missionId",MissionId);S(data,"scenarioId",ScenarioId);S(data,"operationMapId",MapId);S(data,"displayNameKey","mission.market_lifeline.name");S(data,"displaySummaryKey","mission.market_lifeline.summary");S(data,"locationNameKey","mission.market_lifeline.location");foreach(string stage in new[]{"briefing","comms","debrief"})S(data,stage+"SequenceId","seq.ch02.m03."+(stage=="briefing"?"brief":stage));
            string[] names={"delivery","manifest","market"};A(data.FindProperty("objectives"),3,(e,i)=>{S(e,"objectiveId","obj.ch02.m03."+names[i]);S(e,"displayTextKey","mission.market_lifeline.objective."+names[i]);S(e,"rule",(int)MissionObjectiveRuleKind.DeliverMarketRelief+i);S(e,"missionRoleId",i==0?"role.market.relief_convoy":i==1?"role.market.manifest":"role.market.trade");S(e,"targetConfigId",string.Empty);S(e,"requiredCount",i==0?3:i==1?2:1);S(e,"failureOnRuleBreak",true);});
            A(data.FindProperty("stars"),3,(e,i)=>{S(e,"starIndex",i+1);S(e,"rule",(int)(i==0?MissionStarRuleKind.CompleteMission:i==1?MissionStarRuleKind.NoSquadLoss:MissionStarRuleKind.CompleteUnderMilliseconds));S(e,"displayTextKey","mission.market_lifeline.star."+(i+1));S(e,"threshold",i==2?420000:0);});
            A(data.FindProperty("firstClearRewards"),2,(e,i)=>{S(e,"kind",i==0?0:1);S(e,"rewardConfigId",i==0?"reward.commander_xp":string.Empty);S(e,"displayTextKey",i==0?"mission.reward.commander_xp":"mission.reward.credits");S(e,"amount",i==0?900:4500);});A(data.FindProperty("replayRewards"),1,(e,i)=>{S(e,"kind",1);S(e,"rewardConfigId",string.Empty);S(e,"displayTextKey","mission.reward.credits");S(e,"amount",350);});
            data.ApplyModifiedPropertiesWithoutUndo();Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);EditorUtility.SetDirty(mission);AssetDatabase.SaveAssets();
        }
        private static T Load<T>(string path) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new InvalidOperationException(path);
        private static T Clone<T>(string source,string target) where T:ScriptableObject{var asset=AssetDatabase.LoadAssetAtPath<T>(target);if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,target);}EditorUtility.CopySerialized(Load<T>(source),asset);asset.name=Path.GetFileNameWithoutExtension(target);return asset;}
        private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}private static void A(SerializedProperty a,int n,Action<SerializedProperty,int> fill){a.arraySize=n;for(int i=0;i<n;i++)fill(a.GetArrayElementAtIndex(i),i);}private static void S(SerializedObject o,string n,object v)=>S(o.FindProperty(n),v);private static void S(SerializedProperty o,string n,object v)=>S(o.FindPropertyRelative(n),v);private static void S(SerializedProperty p,object v){switch(v){case string s:p.stringValue=s;break;case int i:p.intValue=i;break;case bool b:p.boolValue=b;break;default:throw new ArgumentException(p.propertyPath);}}
    }
}
