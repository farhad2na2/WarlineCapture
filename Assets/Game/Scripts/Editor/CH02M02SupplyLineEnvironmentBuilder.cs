using System;
using System.Collections.Generic;
using System.IO;
using Game.Authoring;
using Game.Configs;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH02M02SupplyLineEnvironmentBuilder
    {
        public const string Root="Assets/Game/Prefabs/Environment/Demo2Adapted/Logistics";
        public const string DepotPath=Root+"/SupplyLine_Reserve_Depot.prefab";
        public const string PlacementsPath="Assets/Game/Configs/OperationMaps/Chapter02/SupplyLinePlacements.asset";
        public static void Build()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            BuildProductionVariant("Building_OilPump",200,0);BuildProductionVariant("Building_Refinery",0,400);
            string[] names={"warehouse","container","loaded_pallet","medical_crate"};
            string[] sources={"Buildings/SM_Bld_Warehouse_01","Props/SM_Prop_Container_01","Props/SM_Prop_Pallet_Loaded_01","Props/SM_Prop_Crate_Medical_01"};
            var outputs=new List<GameObject>();var manifest=new List<string>();
            for(int i=0;i<names.Length;i++)
            {
                string source="Assets/Synty/PolygonBattleRoyale/Prefabs/"+sources[i]+".prefab";
                string output=Root+"/ENV_D2_"+names[i]+".prefab";
                var original=AssetDatabase.LoadAssetAtPath<GameObject>(source)??throw new InvalidOperationException(source);
                var wrapper=new GameObject("ENV_D2_"+names[i]);
                try
                {
                    var visual=(GameObject)PrefabUtility.InstantiatePrefab(original,wrapper.transform);
                    visual.name="Visual";Bounds bounds=BoundsOf(visual);
                    visual.transform.localPosition=-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                    foreach(var c in visual.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                    foreach(var c in visual.GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(c);
                    foreach(var c in visual.GetComponentsInChildren<AudioSource>(true))UnityEngine.Object.DestroyImmediate(c);
                    foreach(var c in visual.GetComponentsInChildren<Camera>(true))UnityEngine.Object.DestroyImmediate(c);
                    var prefab=PrefabUtility.SaveAsPrefabAsset(wrapper,output);outputs.Add(prefab);
                    manifest.Add($"{names[i]} | {source} | {AssetDatabase.AssetPathToGUID(source)} | {AssetDatabase.GetAssetDependencyHash(source)} | {output} | {AssetDatabase.AssetPathToGUID(output)} | outputDependency={AssetDatabase.GetAssetDependencyHash(output)} | bounds={bounds.size} | renderers={prefab.GetComponentsInChildren<Renderer>(true).Length}");
                }
                finally{UnityEngine.Object.DestroyImmediate(wrapper);}
            }
            var depot=new GameObject("SupplyLine_Reserve_Depot");
            try
            {
                var geometry=new GameObject("Geometry");geometry.transform.SetParent(depot.transform,false);
                for(int i=0;i<outputs.Count;i++)
                {
                    var child=(GameObject)PrefabUtility.InstantiatePrefab(outputs[i],geometry.transform);
                    child.transform.localPosition=i switch {0=>Vector3.zero,1=>new Vector3(11,0,0),2=>new Vector3(9,0,-8),_=>new Vector3(9,0,-5)};
                }
                Bounds b=BoundsOf(geometry);geometry.transform.localPosition-=new Vector3(b.center.x,b.min.y,b.center.z);
                var authoring=depot.AddComponent<BuildingDefinitionAuthoring>();
                var basis=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Buildings/Building_Fuel_Bladder.prefab").GetComponent<BuildingDefinitionAuthoring>();
                EditorUtility.CopySerialized(basis,authoring);var data=new SerializedObject(authoring);
                data.FindProperty("config").objectReferenceValue=null;
                data.FindProperty("displayName").stringValue="Emergency Fuel Reserve";
                data.FindProperty("description").stringValue="Protect 20 barrels for clinics and water pumps, plus 20 for JRC relief vehicles.";
                data.FindProperty("footprintCells").vector2IntValue=new Vector2Int(Mathf.CeilToInt(b.size.x)+2,Mathf.CeilToInt(b.size.z)+2);
                data.FindProperty("canRequest").boolValue=true;data.FindProperty("fuelStorageCapacity").intValue=200;
                data.FindProperty("destroyedVisualPrefab").objectReferenceValue=null;
                data.ApplyModifiedPropertiesWithoutUndo();
                var prefab=PrefabUtility.SaveAsPrefabAsset(depot,DepotPath);
                var config=AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
                var so=new SerializedObject(config);var list=so.FindProperty("spawnables");bool found=false;
                for(int i=0;i<list.arraySize;i++)found|=list.GetArrayElementAtIndex(i).objectReferenceValue==prefab;
                if(!found){int n=list.arraySize++;list.GetArrayElementAtIndex(n).objectReferenceValue=prefab;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);}
            }
            finally{UnityEngine.Object.DestroyImmediate(depot);}
            var placements=AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(PlacementsPath);
            if(placements==null){placements=ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();AssetDatabase.CreateAsset(placements,PlacementsPath);}
            placements.EditorSetPlacements(new List<MapBuildingPlacementConfigEntry>());
            Directory.CreateDirectory("Design/AgentReports/CH02M02SupplyLine");
            File.WriteAllLines("Design/AgentReports/CH02M02SupplyLine/environment_manifest.txt",manifest);
            AssetDatabase.SaveAssets();Debug.Log("[SupplyLineEnvironment] result=Passed authored=4 owner=ReserveDepot acceptance=Pending");
        }
        private static void BuildProductionVariant(string id,float oil,float fuel)
        {
            string folder="Assets/Game/Prefabs/Buildings/CH02M02SupplyLine";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Buildings/"+id+".prefab");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                root.name="SupplyLine_"+id;var authoring=root.GetComponent<BuildingDefinitionAuthoring>();var data=new SerializedObject(authoring);
                data.FindProperty("config").objectReferenceValue=null;
                data.FindProperty("oilBarrelsPerDay").floatValue=oil;data.FindProperty("fuelBarrelsPerDay").floatValue=fuel;
                data.ApplyModifiedPropertiesWithoutUndo();var prefab=PrefabUtility.SaveAsPrefabAsset(root,folder+"/"+root.name+".prefab");
                var config=AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
                var so=new SerializedObject(config);var list=so.FindProperty("spawnables");bool found=false;
                for(int i=0;i<list.arraySize;i++)found|=list.GetArrayElementAtIndex(i).objectReferenceValue==prefab;
                if(!found){int n=list.arraySize++;list.GetArrayElementAtIndex(n).objectReferenceValue=prefab;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);}
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        private static Bounds BoundsOf(GameObject root)
        {
            Bounds bounds=default;bool first=true;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);foreach(var m in renderer.sharedMaterials)if(m==null)throw new InvalidOperationException("Missing material "+root.name);}
            if(first)throw new InvalidOperationException("No geometry "+root.name);return bounds;
        }
    }
}
