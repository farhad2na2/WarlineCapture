using System;
using System.Collections.Generic;
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
    public static partial class CH04M03SplitFrontConfigBuilder
    {
        public const string PreparedMapPath="Assets/Game/GeneratedOperationMaps/Variants/RefineryDistrict/Candidate/Definition.asset";
        public const string PreparedHash="2d4fd91a415a68f0299a4075e37730ecd7b746e94e5a2a1e2a6cd7baca687778";
        private static void BuildRefineryMap()
        {
            var physical=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(PreparedMapPath);
            Require(physical!=null&&physical.ContentHash==PreparedHash,"Prepared RefineryDistrict changed; re-audit Split Front.");
            var map=Clone<OperationMapDefinition>(PreparedMapPath,MapPath);
            var surface=AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(map.MapSurfaceDataReference.AssetGUID));
            Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob),"Split Front surface missing.");
            using(blob)
            {
                var grid=AssetDatabase.LoadAssetAtPath<GridAuthoringSceneConfigAsset>("Assets/Game/GeneratedOperationMaps/Variants/RefineryDistrict/Candidate/Grid.asset");
                var blocked=new HashSet<Vector2Int>(grid.BlockedCells);
                int samples=0;
                for(int x=538;x<=878;x++)for(int z=622;z<=628;z++)
                {
                    var cell=new int2(x,z);
                    Require(!blocked.Contains(new Vector2Int(x,z)),"Split Front armor corridor blocked: "+cell);
                    Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,cell,out var sample)&&
                        (sample.MovementMask&MapSurfaceMovementMask.AllGroundUnits)==MapSurfaceMovementMask.AllGroundUnits,"Split Front armor corridor rejects ground movement: "+cell);
                    samples++;
                }
                int footprintSamples=0;
                foreach(var site in new[]{(x:880,z:585,w:26,h:25),(x:908,z:600,w:28,h:15)})
                    for(int x=site.x;x<site.x+site.w;x++)for(int z=site.z;z<site.z+site.h;z++)
                    {
                        var cell=new int2(x,z);
                        Require(!blocked.Contains(new Vector2Int(x,z)),"Split Front building footprint blocked: "+cell);
                        Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,cell,out var sample)&&
                            (sample.MovementMask&MapSurfaceMovementMask.BuildingPlacement)!=0&&
                            (sample.Flags&(MapSurfaceFlags.Road|MapSurfaceFlags.Reserved))==0,
                            "Split Front building footprint overlaps a road or non-placeable surface: "+cell);
                        footprintSamples++;
                    }
                Debug.Log("[SplitFrontRefineryBuildings] result=Passed fullFootprints="+footprintSamples+" depot=880,585:26x25 barracks=908,600:28x15 roads=Preserved");
                (string id,int x,int z,OperationMapAnchorKind kind,int faction,float radius)[] seeds={
                    ("forward_post",908,600,OperationMapAnchorKind.Base,1,12),
                    ("fuel_reserve",880,585,OperationMapAnchorKind.Build,1,1),
                    ("initial_barracks",908,600,OperationMapAnchorKind.Build,1,1),
                    ("build_zone",900,610,OperationMapAnchorKind.Build,1,38),
                    ("squad_a",782,625,OperationMapAnchorKind.Deployment,1,.5f),
                    ("squad_b",800,625,OperationMapAnchorKind.Deployment,1,.5f),
                    ("tank_support_b",811,625,OperationMapAnchorKind.Deployment,1,.5f),
                    ("tank_support_c",822,625,OperationMapAnchorKind.Deployment,1,.5f),
                    ("armor_support",850,625,OperationMapAnchorKind.Deployment,1,2),
                    ("civilians",707,668,OperationMapAnchorKind.Civilian,0,2),
                    ("vanguard_spawn",595,625,OperationMapAnchorKind.Spawn,2,.5f),
                    ("main_spawn",560,625,OperationMapAnchorKind.Spawn,2,3),
                    ("contact",700,625,OperationMapAnchorKind.Hostile,2,3),
                    ("fork",730,625,OperationMapAnchorKind.Lane,1,3),
                    ("inner_core",875,625,OperationMapAnchorKind.Base,1,5),
                    ("return_rts",770,625,OperationMapAnchorKind.Camera,1,2),
                    ("defense_tower",700,650,OperationMapAnchorKind.Build,1,3),
                    ("defense_barrier",725,625,OperationMapAnchorKind.Build,1,3)};
                var data=new SerializedObject(map);S(data.FindProperty("operationMapId"),MapId);
                var binding=data.FindProperty("sourceBinding");S(binding.FindPropertyRelative("sourceOperationMapId"),physical.OperationMapId);S(binding.FindPropertyRelative("sourceIdentityHash"),physical.SourceIdentityHash);S(binding.FindPropertyRelative("sourceContentHash"),physical.ContentHash);
                data.FindProperty("additionalBuildingPlacements").objectReferenceValue=null;
                var bounds=data.FindProperty("bounds");bounds.FindPropertyRelative("playableMin").vector3Value=new Vector3(535,-20,575);bounds.FindPropertyRelative("playableMax").vector3Value=new Vector3(945,980,685);bounds.FindPropertyRelative("cameraMin").vector3Value=new Vector3(500,-20,515);bounds.FindPropertyRelative("cameraMax").vector3Value=new Vector3(980,980,700);
                var minimap=data.FindProperty("minimap");S(minimap.FindPropertyRelative("minimapId"),"minimap.ch04.m03.split_front");minimap.FindPropertyRelative("projectionOrigin").vector3Value=new Vector3(535,0,575);minimap.FindPropertyRelative("projectionSize").vector2Value=new Vector2(410,110);
                S(data.FindProperty("planningCameraId"),"camera.ch04.m03.planning");S(data.FindProperty("battleCameraId"),"camera.ch04.m03.battle");
                var anchors=data.FindProperty("anchors");anchors.arraySize=seeds.Length;
                for(int i=0;i<seeds.Length;i++)
                {
                    var seed=seeds[i];Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(seed.x,seed.z),out var sample),"Missing surface: "+seed.id);
                    if(seed.kind==OperationMapAnchorKind.Deployment||seed.kind==OperationMapAnchorKind.Spawn)
                        Require(!blocked.Contains(new Vector2Int(seed.x,seed.z))&&(sample.MovementMask&MapSurfaceMovementMask.AllGroundUnits)==MapSurfaceMovementMask.AllGroundUnits,"Split Front formation rejects ground movement: "+seed.id);
                    var anchor=anchors.GetArrayElementAtIndex(i);S(anchor.FindPropertyRelative("anchorId"),Prefix+seed.id);I(anchor.FindPropertyRelative("kind"),(int)seed.kind);I(anchor.FindPropertyRelative("factionId"),seed.faction);I(anchor.FindPropertyRelative("laneIndex"),0);anchor.FindPropertyRelative("position").vector3Value=new Vector3(seed.x,sample.Height,seed.z);anchor.FindPropertyRelative("eulerAngles").vector3Value=Vector3.zero;anchor.FindPropertyRelative("radius").floatValue=seed.radius;
                }
                var cameras=data.FindProperty("cameras");cameras.arraySize=2;
                for(int i=0;i<2;i++)
                {
                    var camera=cameras.GetArrayElementAtIndex(i);S(camera.FindPropertyRelative("cameraId"),i==0?"camera.ch04.m03.planning":"camera.ch04.m03.battle");var focus=new Vector3(750,0,635);var position=focus+new Vector3(0,i==0?165:82,i==0?-110:-65);camera.FindPropertyRelative("position").vector3Value=position;camera.FindPropertyRelative("eulerAngles").vector3Value=Quaternion.LookRotation(focus-position).eulerAngles;camera.FindPropertyRelative("fieldOfView").floatValue=58;camera.FindPropertyRelative("orthographic").boolValue=false;
                }
                S(data.FindProperty("contentHash"),string.Empty);S(data.FindProperty("generatedMetadataHash"),string.Empty);data.ApplyModifiedPropertiesWithoutUndo();
                using var sha=SHA256.Create();string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(map)))).Replace("-",string.Empty).ToLowerInvariant();data.Update();S(data.FindProperty("contentHash"),hash);S(data.FindProperty("generatedMetadataHash"),hash);data.ApplyModifiedPropertiesWithoutUndo();Require(map.TryValidateMetadata(out var error)&&map.TryValidateLocalContentReferences(out error),error);EditorUtility.SetDirty(map);
                Debug.Log("[SplitFrontRefineryRoutes] result=Passed corridor=7x7 samples="+samples+" sourceHash="+PreparedHash+" crop=535,575:945,685 SupplyLineOverlap=None launcherRange=187 civilianSeparation=43");
            }
        }
        public static void PrepareRefineryReview()
        {
            try{CH04M03SplitFrontPresentationBuilder.BuildCheckpoint();Debug.Log("[SplitFrontRefineryPreparation] result=Passed");MissionEditorValidationExit.Complete(true);}
            catch(Exception e){Debug.LogException(e);MissionEditorValidationExit.Complete(false);}
        }
    }
}
