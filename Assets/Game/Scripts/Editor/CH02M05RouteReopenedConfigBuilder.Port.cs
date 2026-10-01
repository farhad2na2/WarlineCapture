using System;
using System.Collections.Generic;
using System.IO;
using Game.Authoring;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class CH02M05RouteReopenedConfigBuilder
    {
        public const string PreparedMapPath="Assets/Game/GeneratedOperationMaps/Variants/AshLinePort/Candidate/Definition.asset";
        public const string PreparedHash="3f8bceea706372ba2a6b0584d6e7820c73ce3e40c30c53acdd937d9998cae8d5";
        public const string RecordsOfficePath="Assets/Game/Prefabs/Buildings/CH02M05RouteReopened/RouteReopened_RecordsOffice.prefab";
        public const string PlacementsPath="Assets/Game/Configs/OperationMaps/Chapter02/RouteReopenedPortPlacements.asset";
        private static void BuildPortMap()
        {
            var physical=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(PreparedMapPath);
            Require(physical!=null && physical.ContentHash==PreparedHash,"Prepared port changed; re-audit Route Reopened.");
            ValidateGroundRoutes();
            var map=Clone<OperationMapDefinition>(PreparedMapPath,MapPath);
            var surface=AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(map.MapSurfaceDataReference.AssetGUID));
            Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob),"Route Reopened surface missing.");
            using(blob)
            {
                ValidateBridgeSurfaces(ref blob.Value);
                (string id,int x,int z,OperationMapAnchorKind kind,int faction,float radius,bool air)[] seeds={
                    ("relief_goal",620,385,OperationMapAnchorKind.Objective,1,8,false),
                    ("fuel_goal",740,505,OperationMapAnchorKind.Objective,1,8,false),
                    ("disrupted_link",625,505,OperationMapAnchorKind.Objective,1,7,false),
                    ("hub_gate",595,565,OperationMapAnchorKind.Objective,1,8,false),
                    ("records",605,595,OperationMapAnchorKind.Objective,1,7,false),
                    ("squad_a",605,525,OperationMapAnchorKind.Deployment,1,3,false),
                    ("squad_b",615,535,OperationMapAnchorKind.Deployment,1,3,false),
                    ("engineers",625,495,OperationMapAnchorKind.Deployment,1,3,false),
                    ("relief",720,385,OperationMapAnchorKind.Deployment,1,.35f,false),
                    ("fuel",640,505,OperationMapAnchorKind.Deployment,1,.35f,false),
                    ("hostile_a",605,620,OperationMapAnchorKind.Spawn,2,3,false),
                    ("hostile_b",605,635,OperationMapAnchorKind.Spawn,2,3,false),
                    ("return_rts",620,505,OperationMapAnchorKind.Camera,1,2,false)};
                var data=new SerializedObject(map);S(data,"operationMapId",MapId);
                var binding=data.FindProperty("sourceBinding");S(binding.FindPropertyRelative("sourceOperationMapId"),physical.OperationMapId);S(binding.FindPropertyRelative("sourceIdentityHash"),physical.SourceIdentityHash);S(binding.FindPropertyRelative("sourceContentHash"),physical.ContentHash);
                data.FindProperty("additionalBuildingPlacements").objectReferenceValue=BuildRecordsOffice(ref blob.Value);
                S(data.FindProperty("planningCameraId"),"camera.ch02.m05.planning");S(data.FindProperty("battleCameraId"),"camera.ch02.m05.battle");
                var bounds=data.FindProperty("bounds");bounds.FindPropertyRelative("playableMin").vector3Value=new Vector3(540,-20,365);bounds.FindPropertyRelative("playableMax").vector3Value=new Vector3(770,980,640);bounds.FindPropertyRelative("cameraMin").vector3Value=new Vector3(510,-20,345);bounds.FindPropertyRelative("cameraMax").vector3Value=new Vector3(770,980,640);
                S(data.FindProperty("minimap").FindPropertyRelative("minimapId"),"minimap.ch02.m05.route_reopened");
                var anchors=data.FindProperty("anchors");anchors.arraySize=seeds.Length;
                for(int i=0;i<seeds.Length;i++)
                {
                    var seed=seeds[i];Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(seed.x,seed.z),out var sample),"Missing surface "+seed.id);
                    var a=anchors.GetArrayElementAtIndex(i);S(a.FindPropertyRelative("anchorId"),Prefix+seed.id);S(a.FindPropertyRelative("kind"),(int)seed.kind);S(a.FindPropertyRelative("factionId"),seed.faction);S(a.FindPropertyRelative("laneIndex"),seed.id is "fuel" or "fuel_goal"?1:0);
                    a.FindPropertyRelative("position").vector3Value=new Vector3(seed.x,sample.Height+(seed.air?18:0),seed.z);a.FindPropertyRelative("eulerAngles").vector3Value=Vector3.zero;a.FindPropertyRelative("radius").floatValue=seed.radius;
                }
                var cameras=data.FindProperty("cameras");cameras.arraySize=2;
                for(int i=0;i<2;i++)
                {
                    var c=cameras.GetArrayElementAtIndex(i);var focus=new Vector3(650,0,500);var pos=focus+new Vector3(0,i==0?155:70,i==0?-110:-50);
                    S(c.FindPropertyRelative("cameraId"),"camera.ch02.m05."+(i==0?"planning":"battle"));c.FindPropertyRelative("position").vector3Value=pos;c.FindPropertyRelative("eulerAngles").vector3Value=Quaternion.LookRotation(focus-pos).eulerAngles;c.FindPropertyRelative("fieldOfView").floatValue=58;c.FindPropertyRelative("orthographic").boolValue=false;
                }
                S(data.FindProperty("contentHash"),string.Empty);S(data.FindProperty("generatedMetadataHash"),string.Empty);data.ApplyModifiedPropertiesWithoutUndo();
                using var sha=SHA256.Create();string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(map)))).Replace("-",string.Empty).ToLowerInvariant();data.Update();S(data.FindProperty("contentHash"),hash);S(data.FindProperty("generatedMetadataHash"),hash);data.ApplyModifiedPropertiesWithoutUndo();
                Require(map.TryValidateMetadata(out var error)&&map.TryValidateLocalContentReferences(out error),error);EditorUtility.SetDirty(map);AssetDatabase.SaveAssets();
            }
        }
        private static void ValidateBridgeSurfaces(ref MapSurfaceBlob surface)
        {
            int samples=0;
            foreach(int lane in new[]{505,385})
                for(int x=640;x<=720;x++)for(int z=lane-2;z<=lane+2;z++)
                {
                    Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface,new int2(x,z),out var cell),"Missing crossing surface.");
                    Require((cell.MovementMask&MapSurfaceMovementMask.AllGroundUnits)==MapSurfaceMovementMask.AllGroundUnits,"Bridge approach rejects ground movement "+cell.Cell);
                    if(x>=661&&x<=699)Require(cell.SurfaceType==MapSurfaceType.BridgeDeck&&cell.Height>=0,"Canal-bed crossing "+cell.Cell);
                    samples++;
                }
            foreach(var p in new[]{new int2(680,450),new int2(680,600),new int2(680,730)})
                Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface,p,out var cell)&&cell.MovementMask==MapSurfaceMovementMask.None,"Unqualified canal/quay destination is walkable "+p);
            Debug.Log("[RouteReopenedPortSurfaces] result=Passed crossingSamples="+samples+" masks=Infantry,Wheeled,Tracked water=blocked deckAboveCanal=Passed");
        }
        private static MapBuildingPlacementConfig BuildRecordsOffice(ref MapSurfaceBlob surface)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RecordsOfficePath));AssetDatabase.Refresh();
            var root=PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/Buildings/M05Compound/M05_Archive.prefab");
            GameObject office;
            try
            {
                root.name="RouteReopened_RecordsOffice";
                var definition=new SerializedObject(root.GetComponent<BuildingDefinitionAuthoring>());
                definition.FindProperty("displayName").stringValue="Port Records Office";
                definition.FindProperty("description").stringValue="Preserve the port routing records. Secure the entrance without damaging the office.";
                definition.FindProperty("maxHealth").intValue=350;
                definition.ApplyModifiedPropertiesWithoutUndo();office=PrefabUtility.SaveAsPrefabAsset(root,RecordsOfficePath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            // Entrance hold remains outside the 14x8 building footprint, with enough
            // space for infantry to pass between the office and the quay.
            Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface,new int2(588,595),out var sample),"Records office surface missing.");
            var position=new Vector3(588,sample.Height,595);
            var placements=AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(PlacementsPath);
            if(placements==null){placements=ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();AssetDatabase.CreateAsset(placements,PlacementsPath);}
            placements.EditorSetPlacements(new List<MapBuildingPlacementConfigEntry>{new("RouteReopened/RecordsOffice","ProtectedRecords",office,2,position,position,Vector3.zero,Vector3.one,0,false,true,true)});
            placements.EditorSetUseExistingStaticPresentationWhenAuthoringVisualMissing(false);return placements;
        }
        private static void ValidateGroundRoutes()
        {
            var grid=Load<GridAuthoringSceneConfigAsset>("Assets/Game/GeneratedOperationMaps/Variants/AshLinePort/Candidate/Grid.asset");
            var blocked=new HashSet<Vector2Int>(grid.BlockedCells);
            bool Fits(Vector2Int p,int radius){for(int z=-radius;z<=radius;z++)for(int x=-radius;x<=radius;x++)if(blocked.Contains(p+new Vector2Int(x,z)))return false;return true;}
            foreach(var p in new[]{new Vector2Int(605,525),new Vector2Int(615,535),new Vector2Int(625,495),new Vector2Int(605,620),new Vector2Int(605,635),new Vector2Int(595,565),new Vector2Int(605,595)})Require(Fits(p,3),"Route formation/entrance blocked "+p);
            for(int x=579;x<=597;x++)for(int z=589;z<=601;z++)Require(!blocked.Contains(new Vector2Int(x,z)),"Records office footprint/clearance blocked "+new Vector2Int(x,z));
            foreach(int bridgeZ in new[]{505,385})for(int x=640;x<=720;x++)Require(Fits(new Vector2Int(x,bridgeZ),2),"Bridge 5x5 corridor blocked "+bridgeZ+" at "+x);
            Debug.Log("[RouteReopenedPortRoutes] result=Passed bridges=2 corridor=5x5 formations=7x7 sourceHash="+PreparedHash);
        }
        public static void PreparePortReview()
        {
            try{CH02M05RouteReopenedPresentationBuilder.BuildCheckpoint();Debug.Log("[RouteReopenedPortPreparation] result=Passed");MissionEditorValidationExit.Complete(true);}
            catch(Exception e){Debug.LogException(e);MissionEditorValidationExit.Complete(false);}
        }
    }
}
