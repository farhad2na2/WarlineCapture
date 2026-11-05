using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Configs;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Only a local menu-input fixture preparation. All geometry, provenance, bindings and
    // strict Play-entry guards are preserved. The runner restores these three files afterward.
    public static class MainMenuModeCardsValidationDependencies
    {
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Idle validation Editor required.");
            string[] paths={SkirmishRigidAircraftGeometryAuthoring.Output,SkirmishCanonicalAircraftGeometryAuthoring.Output,SkirmishAircraftRotorGeometryAuthoring.Output};
            string[] pins={"3903e2a1a93af11e7254bbd3bf5a1dcf9849b83e403505f8ae19450daa54b9ae",
                "5028f5eea8aef4d31caee2d1a730cd8704c29a6a020f97784ae3e9533edfcb80",
                "0eb796b92051e4ac6d16e09769a227cf687b0fbc38b8f1f79517ce151e1bcf5d"};
            var installed=paths.Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>).ToArray();
            for(int i=0;i<3;i++)
                if(Hash(paths[i])!=pins[i] || installed[i]==null || EditorUtility.IsDirty(installed[i]))
                    throw new InvalidOperationException("Exact clean baseline cache required: "+paths[i]);
            var rotor=(SkirmishAircraftRotorGeometryConfig)installed[2];
            ScriptableObject[] measured={SkirmishRigidAircraftGeometryAuthoring.Measure(),
                SkirmishCanonicalAircraftGeometryAuthoring.Measure(),SkirmishAircraftRotorGeometryAuthoring.Measure(rotor.NativeTemplateProvenanceSha256)};
            try
            {
                var expected=measured.Select(m=>JObject.Parse(JsonUtility.ToJson(m))).ToArray();
                for(int i=0;i<3;i++)
                {
                    var before=JObject.Parse(JsonUtility.ToJson(installed[i]));
                    var comparison=(JObject)expected[i].DeepClone();RemoveStamps(before);RemoveStamps(comparison);
                    if(!JToken.DeepEquals(before,comparison))
                        throw new InvalidOperationException("Menu validation refuses geometry/binding/provenance drift: "+paths[i]);
                }
                string backup=MainMenuModeCardsValidation.Root+"/DependencyBackup";
                Directory.CreateDirectory(backup);
                for(int i=0;i<3;i++)
                {
                    string guid=AssetDatabase.AssetPathToGUID(paths[i]),meta=Hash(paths[i]+".meta");
                    File.Copy(paths[i],backup+"/"+Path.GetFileName(paths[i])+".original",true);
                    var target=new SerializedObject(installed[i]);var fresh=new SerializedObject(measured[i]);
                    var property=target.GetIterator();bool children=true;
                    while(property.Next(children))
                    {
                        children=true;
                        if(property.name!="DependencyHash" || property.propertyType!=SerializedPropertyType.String)continue;
                        var value=fresh.FindProperty(property.propertyPath);
                        if(value==null || value.propertyType!=SerializedPropertyType.String)throw new InvalidOperationException("Dependency shape changed.");
                        property.stringValue=value.stringValue;
                    }
                    target.ApplyModifiedPropertiesWithoutUndo();
                    if(!JToken.DeepEquals(JObject.Parse(JsonUtility.ToJson(installed[i])),expected[i]))
                        throw new InvalidOperationException("Unexpected non-stamp change.");
                    EditorUtility.SetDirty(installed[i]);AssetDatabase.SaveAssetIfDirty(installed[i]);
                    if(AssetDatabase.AssetPathToGUID(paths[i])!=guid || Hash(paths[i]+".meta")!=meta)
                        throw new InvalidOperationException("Cache GUID/meta changed.");
                }
                SkirmishRigidAircraftGeometryAuthoring.VerifyInstalledTransportMetadata();
                SkirmishCanonicalAircraftGeometryAuthoring.VerifyInstalled();
                SkirmishAircraftRotorGeometryAuthoring.ValidateInstalled();
                File.WriteAllText(backup+"/receipt.json",new JObject{["scope"]="LocalMenuInputFixtureDependencyStampsOnly",
                    ["geometryAndBindingsChanged"]=false,["originalStrictGuardsPassed"]=true,["restoreRequired"]=true}.ToString());
                Debug.Log("[MainMenuInputDependencies] result=Passed caches=3 geometryChanged=false originalStrictGuards=Passed restorationRequired=true");
            }
            finally{foreach(var m in measured)UnityEngine.Object.DestroyImmediate(m);}
        }
        private static void RemoveStamps(JToken token)
        {
            if(token is JObject obj){obj.Remove("DependencyHash");foreach(var property in obj.Properties())RemoveStamps(property.Value);}
            else if(token is JArray array)foreach(var child in array)RemoveStamps(child);
        }
        private static string Hash(string path)
        {using var stream=File.OpenRead(path);using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
    }
}
