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
                actual.ChassisPrefabGuid==AssetDatabase.AssetPathToGUID(prefab)&&PinMatches("prefab",actual.ChassisPrefabSha256,prefab)&&
                actual.ChassisConfigGuid==AssetDatabase.AssetPathToGUID(SkirmishConvoyTruckDefinition.ExistingConfigPath)&&
                PinMatches("config",actual.ChassisConfigSha256,SkirmishConvoyTruckDefinition.ExistingConfigPath),"Existing chassis/config pins differ; do not overwrite.");
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
        // The pins were written on a CRLF checkout; this repository's .gitattributes keeps LF (* text=auto eol=lf), so the same
        // content hashes differently per checkout. Accept a pin that equals the raw, LF-normalised or CRLF-normalised hash. Any
        // real content change matches none of them and still fails: nothing is re-pinned or overwritten here.
        private static bool PinMatches(string what,string pinned,string path)
        {
            string match=SkirmishConvoyPinMatch.Match(pinned,File.ReadAllBytes(path));
            if(match==null)return false;
            if(match!="raw")Debug.Log("[SkirmishConvoyLogicalAssets] pin="+what+" match="+match+" lineEndingNormalized=1 pinned="+pinned+" raw="+Sha(path)+" contentChanged=0");
            return true;
        }
        private static string Sha(string path)
        {using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}
    }
}

namespace Game.Editor
{
    // Pure pin comparison, separately testable: which line-ending form of the file bytes hashes to the pin.
    public static class SkirmishConvoyPinMatch
    {
        public static string Match(string pinned,byte[] bytes)
        {
            if(string.IsNullOrEmpty(pinned)||bytes==null)return null;
            if(Hash(bytes)==pinned)return "raw";
            var lf=new System.Collections.Generic.List<byte>(bytes.Length);
            for(int i=0;i<bytes.Length;i++)if(!(bytes[i]==13&&i+1<bytes.Length&&bytes[i+1]==10))lf.Add(bytes[i]);
            byte[] lfBytes=lf.ToArray();
            if(Hash(lfBytes)==pinned)return "lf";
            var crlf=new System.Collections.Generic.List<byte>(lfBytes.Length+lfBytes.Length/16);
            foreach(byte b in lfBytes){if(b==10)crlf.Add(13);crlf.Add(b);}
            return Hash(crlf.ToArray())==pinned?"crlf":null;
        }
        public static string Hash(byte[] bytes){using var hash=System.Security.Cryptography.SHA256.Create();return System.BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    }
}
#endif
