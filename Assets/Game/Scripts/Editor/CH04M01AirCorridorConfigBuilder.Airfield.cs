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
    public static partial class CH04M01AirCorridorConfigBuilder
    {
        public const string PreparedMapPath=M04AirliftConfigBuilder.PreparedMapPath;
        public const string PreparedHash=M04AirliftConfigBuilder.PreparedHash;
        private static void BuildAirfieldMap()
        {
            var physical=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(PreparedMapPath);
            Require(physical!=null && physical.ContentHash==PreparedHash,"Prepared airfield changed; re-audit Air Corridor.");
            ValidateGroundRoutes();
            var map=Clone<OperationMapDefinition>(PreparedMapPath,MapPath);
            var surface=AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(map.MapSurfaceDataReference.AssetGUID));
            Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob),"Air Corridor surface missing.");
            using(blob)
            {
                (string id,int x,int z,OperationMapAnchorKind kind,int faction,float radius,bool air)[] seeds={
                    ("forward_post",650,682,OperationMapAnchorKind.Base,1,12,false),
                    ("initial_barracks",515,630,OperationMapAnchorKind.Build,1,1,false),
                    ("build_zone",515,630,OperationMapAnchorKind.Build,1,35,false),
                    ("squad_a",600,610,OperationMapAnchorKind.Deployment,1,4,false),
                    ("squad_b",670,600,OperationMapAnchorKind.Deployment,1,4,false),
                    ("ground_sensor",630,600,OperationMapAnchorKind.Deployment,1,3,false),
                    ("civilians",650,590,OperationMapAnchorKind.Deployment,1,3,false),
                    ("evacuation",680,590,OperationMapAnchorKind.Civilian,0,3,false),
                    ("vanguard_spawn",430,560,OperationMapAnchorKind.Spawn,2,5,true),
                    ("main_spawn",980,690,OperationMapAnchorKind.Spawn,2,5,true),
                    ("contact",520,580,OperationMapAnchorKind.Hostile,2,4,true),
                    ("fork",580,600,OperationMapAnchorKind.Lane,1,3,false),
                    ("north_contact",900,660,OperationMapAnchorKind.Hostile,2,4,true),
                    ("inner_core",650,610,OperationMapAnchorKind.Base,1,6,true),
                    ("return_rts",630,600,OperationMapAnchorKind.Camera,1,2,false),
                    ("defense_tower",515,620,OperationMapAnchorKind.Build,1,4,false),
                    ("defense_barrier",535,620,OperationMapAnchorKind.Build,1,3,false)};
                var data=new SerializedObject(map);S(data.FindProperty("operationMapId"),MapId);
                var binding=data.FindProperty("sourceBinding");S(binding.FindPropertyRelative("sourceOperationMapId"),physical.OperationMapId);S(binding.FindPropertyRelative("sourceIdentityHash"),physical.SourceIdentityHash);S(binding.FindPropertyRelative("sourceContentHash"),physical.ContentHash);
                data.FindProperty("additionalBuildingPlacements").objectReferenceValue=null;
                S(data.FindProperty("planningCameraId"),"camera.ch04.m01.planning");S(data.FindProperty("battleCameraId"),"camera.ch04.m01.battle");
                var bounds=data.FindProperty("bounds");bounds.FindPropertyRelative("playableMin").vector3Value=new Vector3(420,-20,520);bounds.FindPropertyRelative("playableMax").vector3Value=new Vector3(1000,980,700);bounds.FindPropertyRelative("cameraMin").vector3Value=new Vector3(400,-20,500);bounds.FindPropertyRelative("cameraMax").vector3Value=new Vector3(1000,980,700);
                S(data.FindProperty("minimap").FindPropertyRelative("minimapId"),"minimap.ch04.m01.air_corridor");
                var anchors=data.FindProperty("anchors");anchors.arraySize=seeds.Length;
                for(int i=0;i<seeds.Length;i++)
                {
                    var seed=seeds[i];Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(seed.x,seed.z),out var sample),"Missing surface "+seed.id);
                    var a=anchors.GetArrayElementAtIndex(i);S(a.FindPropertyRelative("anchorId"),Prefix+seed.id);I(a.FindPropertyRelative("kind"),(int)seed.kind);I(a.FindPropertyRelative("factionId"),seed.faction);I(a.FindPropertyRelative("laneIndex"),seed.id=="north_contact"?1:0);
                    a.FindPropertyRelative("position").vector3Value=new Vector3(seed.x,sample.Height+(seed.air?18:0),seed.z);a.FindPropertyRelative("eulerAngles").vector3Value=Vector3.zero;a.FindPropertyRelative("radius").floatValue=seed.radius;
                }
                var cameras=data.FindProperty("cameras");cameras.arraySize=2;
                for(int i=0;i<2;i++)
                {
                    var c=cameras.GetArrayElementAtIndex(i);var focus=new Vector3(615,0,620);var pos=focus+new Vector3(0,i==0?125:90,i==0?-95:-65);
                    S(c.FindPropertyRelative("cameraId"),"camera.ch04.m01."+(i==0?"planning":"battle"));c.FindPropertyRelative("position").vector3Value=pos;c.FindPropertyRelative("eulerAngles").vector3Value=Quaternion.LookRotation(focus-pos).eulerAngles;c.FindPropertyRelative("fieldOfView").floatValue=58;c.FindPropertyRelative("orthographic").boolValue=false;
                }
                S(data.FindProperty("contentHash"),string.Empty);S(data.FindProperty("generatedMetadataHash"),string.Empty);data.ApplyModifiedPropertiesWithoutUndo();
                using var sha=SHA256.Create();string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(map)))).Replace("-",string.Empty).ToLowerInvariant();data.Update();S(data.FindProperty("contentHash"),hash);S(data.FindProperty("generatedMetadataHash"),hash);data.ApplyModifiedPropertiesWithoutUndo();
                Require(map.TryValidateMetadata(out var error)&&map.TryValidateLocalContentReferences(out error),error);EditorUtility.SetDirty(map);AssetDatabase.SaveAssets();
            }
        }
        private static void ValidateGroundRoutes()
        {
            var grid=AssetDatabase.LoadAssetAtPath<GridAuthoringSceneConfigAsset>("Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Grid.asset");
            var blocked=new HashSet<Vector2Int>(grid.BlockedCells);
            bool Fits(Vector2Int p,int radius){for(int z=-radius;z<=radius;z++)for(int x=-radius;x<=radius;x++)if(blocked.Contains(p+new Vector2Int(x,z)))return false;return true;}
            foreach(var p in new[]{new Vector2Int(600,610),new Vector2Int(670,600),new Vector2Int(630,600),new Vector2Int(650,590)})Require(Fits(p,3),"Air Corridor formation blocked "+p);
            Require(Fits(new Vector2Int(515,630),9),"Producer service footprint blocked.");
            var start=new Vector2Int(600,610);var goal=new Vector2Int(580,600);var q=new Queue<Vector2Int>();var distance=new Dictionary<Vector2Int,int>{{start,0}};q.Enqueue(start);
            while(q.Count>0){var p=q.Dequeue();if(p==goal){Debug.Log("[AirCorridorAirfieldRoutes] result=Passed westCoverageCells="+distance[p]+" vehicleClearance=2 formations=7x7 producer=19x19 sourceHash="+PreparedHash);return;}foreach(var step in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}){var n=p+step;if(n.x<420||n.x>730||n.y<520||n.y>700||distance.ContainsKey(n)||!Fits(n,2))continue;distance.Add(n,distance[p]+1);q.Enqueue(n);}}
            throw new InvalidOperationException("West launcher cannot reach coverage point.");
        }
        public static void PrepareAirfieldReview()
        {
            try{CH04M01AirCorridorPresentationBuilder.BuildCheckpoint();Debug.Log("[AirCorridorAirfieldPreparation] result=Passed");MissionEditorValidationExit.Complete(true);}
            catch(Exception e){Debug.LogException(e);MissionEditorValidationExit.Complete(false);}
        }
    }
}
