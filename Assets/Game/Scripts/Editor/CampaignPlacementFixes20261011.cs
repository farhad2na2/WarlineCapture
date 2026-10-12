using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Game.Components;
using UnityEditor;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
namespace Game.Editor
{
    public static class CampaignPlacementFixes20261011
    {
        static void SetAnchor(OperationMapDefinition map,string suffix,int x,int z)
        {
            var surface=AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(map.MapSurfaceDataReference.AssetGUID));
            if(!surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob))throw new Exception("Missing surface");
            float height;
            using(blob)
            {
                var cell=(int2)math.floor((new float2(x,z)-blob.Value.GridOrigin.xz)/blob.Value.CellSize);
                if(!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,cell,out var sample))throw new Exception("Missing anchor support "+suffix);
                height=sample.Height;
            }
            var data=new SerializedObject(map);var anchors=data.FindProperty("anchors");bool found=false;
            for(int i=0;i<anchors.arraySize;i++)
            {var a=anchors.GetArrayElementAtIndex(i);if(!a.FindPropertyRelative("anchorId").stringValue.EndsWith("."+suffix,StringComparison.Ordinal))continue;a.FindPropertyRelative("position").vector3Value=new Vector3(x,height,z);found=true;}
            if(!found)throw new Exception("Anchor missing "+suffix);data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(map);
        }
        static void Rehash(OperationMapDefinition map)
        {
            var data=new SerializedObject(map);data.FindProperty("contentHash").stringValue="";data.FindProperty("generatedMetadataHash").stringValue="";data.ApplyModifiedPropertiesWithoutUndo();
            using var sha=SHA256.Create();var hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(map)))).Replace("-","").ToLowerInvariant();
            data.Update();data.FindProperty("contentHash").stringValue=hash;data.FindProperty("generatedMetadataHash").stringValue=hash;data.ApplyModifiedPropertiesWithoutUndo();
            if(!map.TryValidateMetadata(out var error)||!map.TryValidateLocalContentReferences(out error))throw new Exception(error);AssetDatabase.SaveAssetIfDirty(map);
        }
        public static void ApplyFirstContactCameraMargin()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Editor must be idle");
            var map=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>("Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_DistrictEdge01.asset");
            var data=new SerializedObject(map);var minimum=data.FindProperty("bounds").FindPropertyRelative("cameraMin");
            var position=minimum.vector3Value;position.z=map.Bounds.PlayableMin.z-24f;minimum.vector3Value=position;
            data.ApplyModifiedPropertiesWithoutUndo();Rehash(map);M01FirstContactConfigBuilder.RefreshChapterCatalogs();AssetDatabase.SaveAssets();
            Debug.Log("[CampaignFirstContactCameraMargin] result=Passed cameraSouthMargin=24 gameplayBounds=unchanged nativeVerification=pending");
        }
        public static void ApplyChapterFiveQualifiedLots()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Editor must be idle");
            var network=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(CH05M03NetworkCollapseConfigBuilder.MapPath);
            SetAnchor(network,"civic_2_origin",1206,448);Rehash(network);
            var corridor=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(CH05M04LastCorridorConfigBuilder.MapPath);
            SetAnchor(corridor,"receiving_origin",1233,455);SetAnchor(corridor,"receiving",1240,461);SetAnchor(corridor,"fuel_receive",1213,380);Rehash(corridor);
            var command=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(CH05M05CommandNodeConfigBuilder.MapPath);
            SetAnchor(command,"utility_origin",1233,455);SetAnchor(command,"utility",1240,461);
            SetAnchor(command,"audit_origin",1130,477);SetAnchor(command,"audit",1137,483);
            SetAnchor(command,"fuel_reserve",1219,380);SetAnchor(command,"initial_barracks",1219,380);Rehash(command);
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();AssetDatabase.SaveAssets();
            Debug.Log("[CampaignChapterFiveQualifiedLots] result=Passed requiredLots=6 budgetsAndReadiness=unchanged runtimePlacementVerification=pending");
        }
        public static void ApplyQualifiedLots()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Editor must be idle");
            var steel=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(CH04M02SteelPushConfigBuilder.MapPath);
            SetAnchor(steel,"fuel_reserve",870,650);SetAnchor(steel,"forward_post",882,661);SetAnchor(steel,"initial_barracks",814,600);SetAnchor(steel,"build_zone",795,640);Rehash(steel);
            var scenario=AssetDatabase.LoadMainAssetAtPath(CH04M02SteelPushConfigBuilder.ScenarioPath);var data=new SerializedObject(scenario);var zone=data.FindProperty("missionRuntime").FindPropertyRelative("buildZone");
            zone.FindPropertyRelative("halfWidthCells").intValue=105;zone.FindPropertyRelative("halfHeightCells").intValue=40;data.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(scenario);
            var supply=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>("Assets/Game/Configs/OperationMaps/Chapter02/OperationMap_Ch02M02_RefineryReview.asset");
            SetAnchor(supply,"refinery",741,472);Rehash(supply);
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();AssetDatabase.SaveAssets();
            Debug.Log("[CampaignQualifiedLots] result=Passed steelReserve=870,650 steelBarracks=814,600 refinery=741,472 unchanged=physicalSources,budgets,missionIds runtimePlacementVerification=pending");
        }
    }
}
