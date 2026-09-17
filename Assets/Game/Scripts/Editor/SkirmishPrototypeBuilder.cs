using System;
using System.Collections.Generic;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishPrototypeBuilder
    {
        public const string Root = "Assets/Game/Configs/Skirmish";
        [MenuItem("Tools/Warline/Skirmish/Rebuild Base Assault Preset")]
        public static void Rebuild()
        {
            System.IO.Directory.CreateDirectory(Root);
            System.IO.Directory.CreateDirectory("Assets/Game/Resources");
            AssetDatabase.Refresh();
            var initial = Copy<InitialUnitsSpawnerAuthoringConfig>(
                "Assets/Game/Configs/Scene/MatchSubScene_InitialUnitsSpawner_Config.asset", "InitialForces");
            var so = new SerializedObject(initial);
            Set(so,"blockerCount",0); Set(so,"createFactionBases",false); Set(so,"enableBlockerChurn",false);
            Set(so,"initialDollars",0); Set(so,"initialMaterials",220); Set(so,"materialsCapacity",600);
            Set(so,"initialAiMaterials",220); Set(so,"aiMaterialsCapacity",600);
            Set(so,"initialOil",0); Set(so,"initialFuel",160); Set(so,"spawnRadiusCells",6);
            var factions = so.FindProperty("factions"); factions.arraySize=2;
            for(int i=0;i<2;i++)
            {
                var faction=factions.GetArrayElementAtIndex(i);
                faction.FindPropertyRelative("factionId").intValue=i+1;
                faction.FindPropertyRelative("spawnCell").vector2IntValue=new Vector2Int(i==0?900:1190,i==0?490:530);
                var units=faction.FindPropertyRelative("units"); units.arraySize=5;
                Unit(units,0,"Characters/Unit_Chr_Soldier_Male_02_Alt_04",4,new Vector2Int(0,0));
                Unit(units,1,"Characters/Unit_Chr_Soldier_Male_02_Alt_04",4,new Vector2Int(12,0));
                Unit(units,2,"Vehicles/Unit_Veh_Light_Armored_Car",1,new Vector2Int(5,10));
                Unit(units,3,"Vehicles/Unit_Veh_Truck_Tanker",1,new Vector2Int(10,30));
                Unit(units,4,"Vehicles/Unit_Veh_Truck_Tray",2,new Vector2Int(0,30));
                var buildings=faction.FindPropertyRelative("buildings"); buildings.arraySize=5;
                Building(buildings,0,"Building_Barrack",new Vector2Int(i==0?-13:-10,-30));
                Building(buildings,1,"Building_Ammunition_Depot",new Vector2Int(i==0?-3:-10,i==0?50:40));
                Building(buildings,2,"Building_OilPump",new Vector2Int(i==0?-20:-10,i==0?83:90));
                Building(buildings,3,"Building_Refinery",new Vector2Int(i==0?0:70,i==0?100:10));
                Building(buildings,4,"Building_Fuel_Bladder",new Vector2Int(i==0?25:5,i==0?80:110));
            }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(initial);
            var placement=Copy<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset","Construction");
            var registry=Copy<UnitPrefabRegistryAuthoringConfig>("Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset","UnitRegistry");
            registry.UnitSpawnPrefabs.Clear();
            registry.UnitSpawnPrefabs.Add(Prefab("Characters/Unit_Chr_Soldier_Male_02_Alt_04"));
            registry.UnitSpawnPrefabs.Add(Prefab("Vehicles/Unit_Veh_Truck_Tray"));
            registry.UnitSpawnPrefabs.Add(Prefab("Vehicles/Unit_Veh_Truck_Tanker"));
            EditorUtility.SetDirty(registry);
            so=new SerializedObject(placement); so.FindProperty("initialUnitsConfig").objectReferenceValue=initial;
            so.FindProperty("unitPrefabRegistryConfig").objectReferenceValue=registry;
            Set(so,"maxQueuedUnitProductions",4);
            string[] roster={"Building_Barrack","Building_GuardTower","Building_Ammunition_Depot","Building_OilPump","Building_Refinery","Building_Fuel_Bladder"};
            var spawnables=so.FindProperty("spawnables"); spawnables.arraySize=roster.Length;
            for(int i=0;i<roster.Length;i++) spawnables.GetArrayElementAtIndex(i).objectReferenceValue=Prefab("Buildings/"+roster[i]);
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(placement);
            var enemy=Copy<AIControllerConfig>("Assets/Game/Configs/Scene/Game_AI_Enemy_Config.asset","Enemy");
            so=new SerializedObject(enemy); Set(so,"startingMoney",0); Set(so,"incomeMultiplier",1f);
            Set(so,"unitProductionIntervalSeconds",15f); Set(so,"attackIntervalSeconds",45f);
            Set(so,"buildIntervalSeconds",35f); Set(so,"defenseRadiusCells",55); Set(so,"aggression",0.6f);
            Strings(so,"preferredBuildingIds",new[]{"Building_GuardTower"});
            Strings(so,"preferredUnitIds",new[]{"Unit_Chr_Soldier_Male_02_Alt_04"});
            Strings(so,"preferredVehicleIds",Array.Empty<string>()); so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(enemy);
            var player=Copy<AIControllerConfig>("Assets/Game/Configs/Scene/Game_AI_PlayerAuto_Config.asset","Player");
            so=new SerializedObject(player); Set(so,"startingMoney",0); Set(so,"incomeMultiplier",1f); Set(so,"autoControlsPlayerFaction",false);
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(player);
            var ai=Asset<AIPlanEntryStartupConfig>(Root+"/Plan.asset"); so=new SerializedObject(ai);
            Strings(so,"fallbackBuildingIds",new[]{"Building_GuardTower"});
            Strings(so,"fallbackProductionUnitIds",new[]{"Unit_Chr_Soldier_Male_02_Alt_04"}); so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(ai);
            var vehicles=Asset<MapVehiclePlacementConfig>(Root+"/MapVehicles.asset"); vehicles.EditorSetPlacements(new());
            var preset=Asset<SkirmishPresetConfig>("Assets/Game/Resources/SkirmishBaseAssault.asset");
            preset.buildingPlacement=placement; preset.aiControllers=new[]{enemy,player}; preset.aiPlan=ai; preset.mapVehicles=vehicles;
            EditorUtility.SetDirty(preset); AssetDatabase.SaveAssets();
            Debug.Log("[SkirmishPrototypeBuilder] preset rebuilt");
        }
        private static T Asset<T>(string path) where T:ScriptableObject
        {var value=AssetDatabase.LoadAssetAtPath<T>(path); if(value!=null)return value;value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);return value;}
        private static T Copy<T>(string source,string name) where T:ScriptableObject
        {
            string path=Root+"/"+name+".asset";
            var original=AssetDatabase.LoadAssetAtPath<T>(source);
            if(original==null)throw new InvalidOperationException(source);
            var target=AssetDatabase.LoadAssetAtPath<T>(path);
            if(target==null)
            {
                if(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))AssetDatabase.DeleteAsset(path);
                target=UnityEngine.Object.Instantiate(original);
                AssetDatabase.CreateAsset(target,path);
            }
            else EditorUtility.CopySerialized(original,target);
            target.name=name;return target;
        }
        private static GameObject Prefab(string path)
        {var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/"+path+".prefab");if(p==null)throw new InvalidOperationException(path);return p;}
        private static void Unit(SerializedProperty array,int i,string prefab,int count,Vector2Int offset)
        {var p=array.GetArrayElementAtIndex(i);p.FindPropertyRelative("prefab").objectReferenceValue=Prefab(prefab);p.FindPropertyRelative("count").intValue=count;p.FindPropertyRelative("spawnOffset").vector2IntValue=offset;}
        private static void Building(SerializedProperty array,int i,string prefab,Vector2Int offset)
        {var p=array.GetArrayElementAtIndex(i);p.FindPropertyRelative("prefab").objectReferenceValue=Prefab("Buildings/"+prefab);p.FindPropertyRelative("originOffset").vector2IntValue=offset;}
        private static void Strings(SerializedObject so,string name,string[] values)
        {var p=so.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).stringValue=values[i];}
        private static void Set(SerializedObject so,string name,int value)=>so.FindProperty(name).intValue=value;
        private static void Set(SerializedObject so,string name,bool value)=>so.FindProperty(name).boolValue=value;
        private static void Set(SerializedObject so,string name,float value)=>so.FindProperty(name).floatValue=value;
    }
}
