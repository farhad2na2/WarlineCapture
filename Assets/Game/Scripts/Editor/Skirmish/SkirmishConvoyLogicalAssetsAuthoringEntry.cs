#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Root-owned explicit logical authoring step, separate from validation.
    // Creates only two mode-owned config assets, with original GUIDs retained on
    // repeat calls. Existing mismatches fail; this never repairs or replaces them.
    public static class SkirmishConvoyLogicalAssetsAuthoringEntry
    {
        public static void EnsurePinnedLogicalAssets()
        {
            var catalog=SkirmishExpansionCatalogConfig.Load();
            Require(catalog!=null,"Actual shared authored catalog missing.");
            var basis=catalog.ToAuthoredSet().Roles;
            Require(basis!=null&&AssetDatabase.Contains(basis)&&!EditorUtility.IsDirty(basis),"Clean persistent explicit role basis required.");
            var definitionObject=AssetDatabase.LoadMainAssetAtPath(SkirmishConvoyTruckDefinition.AssetPath);
            var rolesObject=AssetDatabase.LoadMainAssetAtPath(SkirmishConvoyTruckAuthoring.RoleCatalogPath);
            Require((definitionObject==null)==(rolesObject==null),"Partial existing logical Cargo Truck assets require review; nothing replaced.");
            bool created=definitionObject==null;
            if(created)SkirmishConvoyTruckAuthoring.CreateDefinitionAndRoleCatalog(basis);
            VerifyExisting(basis);
            Debug.Log("[SkirmishConvoyLogicalAssetsAuthoringEntry] result=Passed created="+(created?2:0)+" physicalAssetsCreated=0 readiness=Pending");
        }

        private static void VerifyExisting(SkirmishRoleCatalogConfig basis)
        {
            var actual=AssetDatabase.LoadAssetAtPath<SkirmishConvoyTruckDefinition>(SkirmishConvoyTruckDefinition.AssetPath);
            var roles=AssetDatabase.LoadAssetAtPath<SkirmishRoleCatalogConfig>(SkirmishConvoyTruckAuthoring.RoleCatalogPath);
            Require(actual!=null&&roles!=null&&actual.IsValid&&!EditorUtility.IsDirty(actual)&&!EditorUtility.IsDirty(roles),"Clean exact existing logical assets required.");
            Require(ReferenceEquals(actual,SkirmishConvoyTruckDefinition.Load()),"Resources definition must resolve exact persistent asset.");
            const string prefab="Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Tray.prefab";
            Require(AssetDatabase.GetAssetPath(actual.ChassisPrefab)==prefab&&
                AssetDatabase.GetAssetPath(actual.ChassisConfig)==SkirmishConvoyTruckDefinition.ExistingConfigPath&&
                actual.ChassisPrefabGuid==AssetDatabase.AssetPathToGUID(prefab)&&actual.ChassisPrefabSha256==Sha(prefab)&&
                actual.ChassisConfigGuid==AssetDatabase.AssetPathToGUID(SkirmishConvoyTruckDefinition.ExistingConfigPath)&&
                actual.ChassisConfigSha256==Sha(SkirmishConvoyTruckDefinition.ExistingConfigPath),"Existing chassis/config pins differ; do not overwrite.");
            Require(basis.Roles!=null&&roles.Roles!=null&&roles.CatalogId=="skirmish.roles.convoy.v1"&&
                roles.ContentVersion==basis.ContentVersion&&roles.Roles.Length==basis.Roles.Length+1,"Existing role catalog shape differs.");
            Require(basis.Roles.All(row=>row.RoleId!=SkirmishRoleIds.ConvoyLogic&&row.Kind!=SkirmishRoleKind.ConvoyLogic),"Shared basis unexpectedly contains convoy role.");
            var ids=roles.Roles.Select(row=>row.RoleId).ToArray();
            Require(ids.All(id=>!string.IsNullOrWhiteSpace(id))&&ids.Distinct(StringComparer.Ordinal).Count()==ids.Length,"Ambiguous existing role IDs.");
            foreach(var row in basis.Roles)
                Require(roles.TryGet(row.RoleId,out var retained)&&JsonUtility.ToJson(retained)==JsonUtility.ToJson(row),"Existing role catalog changed shared binding: "+row.RoleId);
            Require(roles.TryGet(SkirmishRoleIds.ConvoyLogic,out var objective)&&
                JsonUtility.ToJson(objective)==JsonUtility.ToJson(actual.RoleBinding()),"Existing mode-owned convoy binding differs.");
        }
        private static string Sha(string path)
        {using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}
    }
}
#endif
