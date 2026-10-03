using System;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Configs;
using Game.Missions.Contracts;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static partial class CH05M05CommandNodeConfigBuilder
    {
        private static void BuildBuildingPrefabs()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CivicPrefabPath));AssetDatabase.Refresh();
            CloneBuilding("Assets/Game/Prefabs/Buildings/CH03M04EvidenceChain/EvidenceChain_Clinic.prefab",ClinicPrefabPath,ClinicBuildingId,"Protected civic clinic",0,1000,new Vector2Int(14,12));
            CloneBuilding("Assets/Game/Prefabs/Buildings/CH03M04EvidenceChain/EvidenceChain_Clinic.prefab",UtilityPrefabPath,UtilityBuildingId,"Protected civic utility",0,1000,new Vector2Int(14,12));
            CloneBuilding("Assets/Game/Prefabs/Buildings/CH03M04EvidenceChain/EvidenceChain_Clinic.prefab",AuditPrefabPath,AuditBuildingId,"Civic Relay audit archive",0,1000,new Vector2Int(14,12));
            CloneBuilding(CH04M05ArmorBreakConfigBuilder.ReservePrefabPath,ReservePrefabPath,ReserveBuildingId,"Civic Relay Fuel reserve",240,0,new Vector2Int(26,25));
            var config=Load<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");var data=new SerializedObject(config);var list=data.FindProperty("spawnables");
            foreach(string path in new[]{ClinicPrefabPath,UtilityPrefabPath,AuditPrefabPath,ReservePrefabPath})
            {var prefab=Load<GameObject>(path);bool found=false;for(int i=0;i<list.arraySize;i++)found|=list.GetArrayElementAtIndex(i).objectReferenceValue==prefab;if(!found){int index=list.arraySize++;list.GetArrayElementAtIndex(index).objectReferenceValue=prefab;}}
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);
        }
        private static void CloneBuilding(string source,string target,string id,string label,int capacity,int health,Vector2Int footprint)
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(source));
            try
            {
                PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);root.name=id;
                var definition=root.GetComponent<BuildingDefinitionAuthoring>();Require(definition!=null,"Building lacks actual definition: "+source);definition.ApplyConfigIfAvailable();
                var data=new SerializedObject(definition);data.FindProperty("config").objectReferenceValue=null;S(data,"displayName",label);S(data,"description",id==ReserveBuildingId?"Finite Fuel with a protected civic reserve.":id==AuditBuildingId?"Protected complete audit archive.":"Protected civilian service; military network links are isolated separately.");S(data,"canRequest",false);data.FindProperty("productions").arraySize=0;data.FindProperty("footprintCells").vector2IntValue=footprint;
                if(capacity>0)S(data,"fuelStorageCapacity",capacity);if(health>0)S(data,"maxHealth",health);data.ApplyModifiedPropertiesWithoutUndo();
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,target);Require(prefab!=null&&prefab.name==id&&PrefabUtility.GetPrefabAssetType(prefab)!=PrefabAssetType.Variant,"Mission prefab must persist independent identity: "+id);
                var saved=PrefabUtility.LoadPrefabContents(target);
                try{var native=saved.GetComponent<BuildingDefinitionAuthoring>();Require(native!=null&&new SerializedObject(native).FindProperty("config").objectReferenceValue==null&&native.ConfiguredFootprintCells==footprint&&native.ConfiguredMaxHealth==definition.ConfiguredMaxHealth&&native.ConfiguredFuelStorageCapacity==definition.ConfiguredFuelStorageCapacity&&native.ConfiguredProductionCount==0&&!native.ConfiguredCanRequest,"Saved Command Node building differs from actual qualified configuration: "+id);}
                finally{PrefabUtility.UnloadPrefabContents(saved);}
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
