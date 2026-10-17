#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Explicit root-owned logical asset authoring. Does not edit source chassis,
    // Campaign geometry, shared role catalogs, missions or publication flags.
    public static class SkirmishConvoyTruckAuthoring
    {
        public const string RoleCatalogPath="Assets/Game/Resources/SkirmishConvoyRoleCatalog.asset";
        public static void CreateDefinitionAndRoleCatalog(SkirmishRoleCatalogConfig explicitBasis)
        {
            if(explicitBasis==null||!AssetDatabase.Contains(explicitBasis)||explicitBasis.Roles==null)
                throw new ArgumentException("Explicit persistent shared role catalog basis required.");
            foreach(var role in explicitBasis.Roles)if(role.Kind==SkirmishRoleKind.ConvoyLogic||role.RoleId==SkirmishRoleIds.ConvoyLogic)
                throw new ArgumentException("Basis already has a convoy binding; do not silently replace it.");
            if(AssetDatabase.LoadMainAssetAtPath(SkirmishConvoyTruckDefinition.AssetPath)!=null||AssetDatabase.LoadMainAssetAtPath(RoleCatalogPath)!=null)
                throw new InvalidOperationException("Existing logical assets require explicit pin review; never replace GUIDs.");
            const string prefab="Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Tray.prefab";
            var definition=ScriptableObject.CreateInstance<SkirmishConvoyTruckDefinition>();
            var catalog=UnityEngine.Object.Instantiate(explicitBasis);
            try
            {
                definition.ChassisPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
                definition.ChassisConfig=AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(SkirmishConvoyTruckDefinition.ExistingConfigPath);
                definition.ChassisPrefabGuid=AssetDatabase.AssetPathToGUID(prefab);definition.ChassisPrefabSha256=Sha(prefab);
                definition.ChassisConfigGuid=AssetDatabase.AssetPathToGUID(SkirmishConvoyTruckDefinition.ExistingConfigPath);
                definition.ChassisConfigSha256=Sha(SkirmishConvoyTruckDefinition.ExistingConfigPath);
                if(!definition.IsValid)throw new InvalidOperationException("Actual existing shared Cargo Truck contract does not match.");
                var serialized=new SerializedObject(catalog);serialized.FindProperty("catalogId").stringValue="skirmish.roles.convoy.v1";
                var roles=serialized.FindProperty("roles");int index=roles.arraySize;roles.arraySize++;
                var added=roles.GetArrayElementAtIndex(index);var binding=definition.RoleBinding();
                added.FindPropertyRelative("RoleId").stringValue=binding.RoleId;
                added.FindPropertyRelative("Kind").intValue=(int)binding.Kind;
                added.FindPropertyRelative("SourceConfigPath").stringValue=binding.SourceConfigPath;
                added.FindPropertyRelative("RuntimePrefabKey").stringValue=binding.RuntimePrefabKey;
                added.FindPropertyRelative("Producer").intValue=(int)binding.Producer;
                added.FindPropertyRelative("RequiredReadiness").intValue=(int)binding.RequiredReadiness;
                added.FindPropertyRelative("Disposition").intValue=(int)binding.Disposition;
                added.FindPropertyRelative("CapabilityCertified").boolValue=false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if(!catalog.TryGet(SkirmishRoleIds.ConvoyLogic,out var actual)||actual.Kind!=SkirmishRoleKind.ConvoyLogic||actual.Producer!=SkirmishProducerKind.AuthoredOnly)
                    throw new InvalidOperationException("Logical convoy role serialization failed preflight.");
                // All inputs validated before the first persistent mutation.
                AssetDatabase.CreateAsset(definition,SkirmishConvoyTruckDefinition.AssetPath);
                AssetDatabase.CreateAsset(catalog,RoleCatalogPath);AssetDatabase.SaveAssets();
            }
            finally
            {
                if(!AssetDatabase.Contains(definition))UnityEngine.Object.DestroyImmediate(definition);
                if(!AssetDatabase.Contains(catalog))UnityEngine.Object.DestroyImmediate(catalog);
            }
        }
        private static string Sha(string path)
        {using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    }
}
#endif
