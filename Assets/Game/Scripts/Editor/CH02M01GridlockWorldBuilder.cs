using System;
using System.Collections.Generic;
using System.IO;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Attempt-only relief corridor presentation; shared physical city assets are never edited.</summary>
    public static class CH02M01GridlockWorldBuilder
    {
        public const string Root="Assets/Game/Prefabs/Buildings/CH02M01Gridlock";
        public const string PlacementsPath="Assets/Game/Configs/OperationMaps/Chapter02/GridlockPlacements.asset";
        public static void Build()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var obstacle=Create(CH02M01GridlockConfigBuilder.ObstructionId,"Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Barrier_Tall_01.prefab",new Vector3(2,2,16),"Blocked service lane");
            var hospital=Create("Gridlock_Hospital_Relief_Ward","Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Refugee_01.prefab",new Vector3(12,6,12),"Hospital relief ward");
            var depot=Create("Gridlock_Relief_Depot","Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Open_01.prefab",new Vector3(10,5,8),"Relief depot");
            var damage=Create("Gridlock_Formal_Route_Collapse","Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Barrier_Tall_01.prefab",new Vector3(4,4,18),"Collapsed formal road");
            var config=AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
            var so=new SerializedObject(config);var list=so.FindProperty("spawnables");bool found=false;
            for(int i=0;i<list.arraySize;i++) found|=list.GetArrayElementAtIndex(i).objectReferenceValue==obstacle;
            if(!found){int i=list.arraySize;list.InsertArrayElementAtIndex(i);list.GetArrayElementAtIndex(i).objectReferenceValue=obstacle;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);}
            var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(M03RadarWarningMapBuilder.Path);
            var surface=AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID));
            if(!surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob)) throw new InvalidOperationException("No Gridlock surface");
            var entries=new List<MapBuildingPlacementConfigEntry>();
            using(blob)
            {
                void Add(GameObject prefab,int x,int z)
                {
                    if(!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(x,z),out var ground)) throw new InvalidOperationException("Gridlock scenery has no surface");
                    var pos=new Vector3(x,ground.Height,z);
                    entries.Add(new MapBuildingPlacementConfigEntry("Gridlock/"+prefab.name,"ReliefInfrastructure",prefab,0,pos,pos,Vector3.zero,Vector3.one,0,false,true,true));
                }
                Add(hospital,816,443);Add(depot,678,450);Add(damage,751,459);
            }
            var placements=AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(PlacementsPath);
            if(placements==null){placements=ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();AssetDatabase.CreateAsset(placements,PlacementsPath);}
            placements.EditorSetPlacements(entries);placements.EditorSetUseExistingStaticPresentationWhenAuthoringVisualMissing(false);AssetDatabase.SaveAssets();
        }
        private static GameObject Create(string name,string sourcePath,Vector3 size,string title)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath)??throw new InvalidOperationException(sourcePath);
            var root=new GameObject(name);
            try
            {
                var geometry=new GameObject("Geometry");geometry.transform.SetParent(root.transform,false);
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(source,geometry.transform);
                Bounds bounds=default;bool first=true;
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>()){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
                if(first) throw new InvalidOperationException("Gridlock missing geometry: "+name);
                var scale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);
                geometry.transform.localScale=scale;geometry.transform.localPosition=-Vector3.Scale(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),scale);
                var authoring=root.AddComponent<BuildingDefinitionAuthoring>();var data=new SerializedObject(authoring);
                data.FindProperty("displayName").stringValue=title;data.FindProperty("description").stringValue=string.Empty;
                data.FindProperty("footprintCells").vector2IntValue=new Vector2Int(Mathf.CeilToInt(size.x),Mathf.CeilToInt(size.z));
                data.FindProperty("canRequest").boolValue=false;data.FindProperty("maxHealth").intValue=1000;
                data.ApplyModifiedPropertiesWithoutUndo();return PrefabUtility.SaveAsPrefabAsset(root,Root+"/"+name+".prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
