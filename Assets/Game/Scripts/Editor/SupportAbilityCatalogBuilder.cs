#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class SupportAbilityCatalogBuilder
    {
        public const string CatalogPath="Assets/Game/Configs/Support/SupportAbilityCatalog.asset";
        public const string PoliciesPath="Assets/Game/Configs/Support/SupportMissionPolicies.asset";
        public const string BindingsPath="Assets/Game/Resources/Support/SupportRuntimeBindings.asset";
        [Serializable] private class Balance { public Ability[] abilities; }
        [Serializable] private class Ability { public string id; public int chargesPerMission; public float cooldownSeconds; public Cost resourceCost; public Definition supportDefinition; }
        [Serializable] private class Cost { public int fuel; }
        [Serializable] private class Definition { public float radiusMeters,durationSeconds,approachSeconds; public ushort incomingDirectRangedDamagePermille; public int rawDamage,materialsAmount; public string unlockMission; }
        private static readonly string[] Icons={
            "Assets/Synty/InterfaceMilitaryCombatHUD/Sprites/Icons_Resources/ICON_SM_Wep_Grenade_Smoke_01_Military.png",
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Veh_Jet_01_Action_512.png",
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Veh_Plane_Transport_Card_512.png",
            "Assets/Game/Art/UI/Icons/scn09_icon_supply_crate.png" };
        private static readonly string[] Prefabs={
            "Assets/PolygonMilitary/Prefabs/FX/FX_Smoke_Medium_01.prefab",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Jet_01.prefab",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab",
            "Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_EmergencyDrop_Crate_01.prefab" };
        [MenuItem("Game/Support/Build Approved Catalog")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Game/Configs/Support");Directory.CreateDirectory("Assets/Game/Resources/Support");
            string json=File.ReadAllText("Design/BalanceConfigs/Combat_Balance_Config_v0_1.json");
            var source=JsonUtility.FromJson<Balance>(json);var catalog=GetOrCreate<SupportAbilityCatalogConfig>(CatalogPath);
            catalog.Abilities=new SupportAbilityConfig[4];
            for(int i=0;i<4;i++)
            {
                var kind=(SupportAbilityKind)(i+1);var id=SupportAbilityCatalogConfig.Id(kind);
                var a=Array.Find(source.abilities,x=>x.id==id);
                if(a==null || a.supportDefinition==null) throw new InvalidOperationException("Missing canonical typed definition: "+id);
                var d=a.supportDefinition;
                catalog.Abilities[i]=new SupportAbilityConfig { Kind=kind,Id=id,NameKey="support.name."+(i+1),DescriptionKey="support.description."+(i+1),
                    Charges=a.chargesPerMission,FuelCost=a.resourceCost.fuel,CooldownSeconds=a.cooldownSeconds,
                    Radius=d.radiusMeters,DurationSeconds=d.durationSeconds,ApproachSeconds=d.approachSeconds,Damage=d.rawDamage,Materials=d.materialsAmount,
                    DirectDamagePermille=d.incomingDirectRangedDamagePermille==0?(ushort)1000:d.incomingDirectRangedDamagePermille,
                    UnlockMission=d.unlockMission,ProductionReady=false,Icon=AssetDatabase.LoadAssetAtPath<Sprite>(Icons[i]),SourcePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs[i]) };
            }
            using(var sha=SHA256.Create()) catalog.SourceHash=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json))).Replace("-","").ToLowerInvariant();
            if(!catalog.TryValidate(out string error)) throw new InvalidOperationException(error);
            var policies=GetOrCreate<SupportMissionPolicyConfig>(PoliciesPath);
            policies.Missions=new SupportMissionPolicyEntry[25];
            for(int i=0;i<25;i++)
            {
                string key=$"CH{i/5+1:00}-M{i%5+1:00}";byte mask=(byte)(i<17?0:i==17?1:i==18?3:i<22?7:15);
                string runtimeId=key;
                policies.Missions[i]=new SupportMissionPolicyEntry { MissionId=runtimeId,AllowedMask=mask,LessonKind=i==17?SupportAbilityKind.Smoke:i==18?SupportAbilityKind.Strike:i==19?SupportAbilityKind.Paratroopers:i==22?SupportAbilityKind.Supply:SupportAbilityKind.None };
            }
            var barracks=AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset");
            if(barracks==null||barracks.Productions.Count!=1||barracks.Productions[0].SpawnUnitPrefab==null||barracks.Productions[0].Quantity<=0)
                throw new InvalidOperationException("Canonical Barracks rifle recipe missing.");
            var registry=AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>("Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset");
            var registryData=new SerializedObject(registry);
            registryData.FindProperty("supportInfantryPrefab").objectReferenceValue=barracks.Productions[0].SpawnUnitPrefab;
            registryData.FindProperty("supportInfantryCount").intValue=barracks.Productions[0].Quantity;
            registryData.FindProperty("supportParachutePrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("a567fd209adc22643bffbc9030f3005a"));
            registryData.FindProperty("supportCratePrefab").objectReferenceValue=catalog.Abilities[3].SourcePrefab;
            registryData.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(registry);
            var bindings=GetOrCreate<SupportRuntimeBindingsConfig>(BindingsPath);bindings.Catalog=catalog;bindings.Policies=policies;
            EditorUtility.SetDirty(catalog);EditorUtility.SetDirty(policies);EditorUtility.SetDirty(bindings);
            AssetDatabase.SaveAssets();
            Debug.Log("[SupportAbilityCatalogBuilder] result=Passed abilities=4 policies=25 productionEnabled=0 hash="+catalog.SourceHash);
        }
        private static T GetOrCreate<T>(string path) where T:ScriptableObject
        { var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}return asset; }
    }
}
#endif
