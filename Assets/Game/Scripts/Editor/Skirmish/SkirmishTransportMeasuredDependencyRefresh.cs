using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Game.Configs;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Explicit one-time checked preparation. Existing geometry and verifier stay unchanged.
    public static class SkirmishTransportMeasuredDependencyRefresh
    {
        const string Audit="Design/AgentReports/SkirmishExpansion/S005/aircraft-framing-inventory/AIRCRAFT_GIT_BYTE_PINS_20261015.json";
        const string AuditSha="c93576b6ac4df12d90367739b371c8cd00316dafc3e8cd861c11d496d64b680f";
        const string CacheSha="cdb8a88b59c1f85b30803f956b6b0b368519bed3459726c2180b7669f045d99a";
        const string MetaSha="688cb44c10b88ffd62541007b249ce77bdf158431794d7b316676cad7f20920b";
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)
                throw new InvalidOperationException("Idle checked preparation required");
            string path=SkirmishRigidAircraftGeometryAuthoring.Output;
            if(Hash(path)!=CacheSha||Hash(path+".meta")!=MetaSha)throw new InvalidOperationException("Exact preserved pre-refresh cache bytes required");
            var installed=AssetDatabase.LoadAssetAtPath<SkirmishRigidAircraftGeometryConfig>(path);
            if(installed==null||EditorUtility.IsDirty(installed))throw new InvalidOperationException("Clean installed Transport cache required");
            // Mutation of an already frozen runtime lookup is forbidden. A fresh
            // checked Editor must perform this before any framing lookup.
            var index=typeof(SkirmishRigidAircraftGeometryConfig).GetField("index",BindingFlags.NonPublic|BindingFlags.Instance);
            if(index==null||index.GetValue(installed)!=null)throw new InvalidOperationException("Transport lookup already indexed; fresh checked preparation required");
            var nativeDependencies=ValidateHistoricalSourceBytes();
            var measured=SkirmishRigidAircraftGeometryAuthoring.Measure();
            try
            {
                foreach(var source in measured.Sources)
                    if(!nativeDependencies.TryGetValue(source.Path,out string expectedHash)||source.DependencyHash!=expectedHash)
                        throw new InvalidOperationException("Current measured dependency differs from fresh native report: "+source.Path);
                if(!SameGeometryIgnoringDependencyStamps(installed,measured,out string error))throw new InvalidOperationException("Dependency-only refresh refused: "+error);
                string guid=AssetDatabase.AssetPathToGUID(path);
                var expected=ExpectedStampOnlySerialization(installed,measured);
                string backup=Path.GetFullPath("Logs/SkirmishProduction/TransportDependencyRefresh/"+Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(backup);File.Copy(path,Path.Combine(backup,"cache-before.asset"),false);File.Copy(path+".meta",Path.Combine(backup,"cache-before.asset.meta"),false);
                File.WriteAllText(Path.Combine(backup,"measured-before-save.json"),JsonUtility.ToJson(measured,true));
                for(int i=0;i<3;i++)installed.Sources[i].DependencyHash=measured.Sources[i].DependencyHash;
                if(!JToken.DeepEquals(expected,JObject.Parse(JsonUtility.ToJson(installed))))throw new InvalidOperationException("Unexpected cache field mutation");
                EditorUtility.SetDirty(installed);AssetDatabase.SaveAssetIfDirty(installed);
                if(AssetDatabase.AssetPathToGUID(path)!=guid||Hash(path+".meta")!=MetaSha)throw new InvalidOperationException("Existing cache GUID/meta changed");
                SkirmishRigidAircraftGeometryAuthoring.VerifyInstalledTransportMetadata();
                File.WriteAllText(Path.Combine(backup,"receipt.json"),new JObject{["scope"]="MeasuredDependencyStampsOnly",["beforeSha256"]=CacheSha,["afterSha256"]=Hash(path),["guid"]=guid,["sources"]=3,["draws"]=57,["geometryChanged"]=false,["cameraAccepted"]=false}.ToString());
                Debug.Log("[TransportMeasuredDependencyRefresh] result=Passed sources=3 draws=57 geometryChanged=0 guidPreserved=1 nativeCameraAcceptance=Pending backup="+backup);
            }
            finally{UnityEngine.Object.DestroyImmediate(measured);}
        }
        public static bool StampSerializationControl(SkirmishRigidAircraftGeometryConfig installed,SkirmishRigidAircraftGeometryConfig measured)
        {return ExpectedStampOnlySerialization(installed,measured)["Sources"].Select(s=>(string)s["DependencyHash"]).SequenceEqual(measured.Sources.Select(s=>s.DependencyHash));}
        public static void GraphShapeControl(int corruption)
        {
            var graphs=new JArray();var lods=new[]{"Detail","Mid","Low"};
            for(int i=0;i<3;i++)graphs.Add(new JObject{["lod"]=lods[i],["rootAssetPath"]=SkirmishRigidAircraftGeometryAuthoring.Prefabs[i],["sourcePresent"]=true,["captured"]=true,["dependencyHashBefore"]="fresh",["dependencyHashAfter"]="fresh",["dependencies"]=new JArray(new JObject{["assetPath"]=SkirmishRigidAircraftGeometryAuthoring.Prefabs[i]})});
            if(corruption==1)graphs.RemoveAt(2);else if(corruption==2)graphs[2]["lod"]="Mid";else if(corruption!=0)throw new ArgumentOutOfRangeException(nameof(corruption));
            if(RequireThreeTransportGraphs(graphs).Count!=3)throw new InvalidOperationException("Three exact graph values required");
        }
        static JObject ExpectedStampOnlySerialization(SkirmishRigidAircraftGeometryConfig installed,SkirmishRigidAircraftGeometryConfig measured)
        {
            var expected=JObject.Parse(JsonUtility.ToJson(installed));
            if(!(expected["Sources"] is JArray sources)||sources.Count!=3||measured.Sources.Length!=3)
                throw new InvalidOperationException("Actual serialized three-source value schema required before mutation");
            for(int i=0;i<3;i++)
            {
                if(sources[i]["DependencyHash"]?.Type!=JTokenType.String)throw new InvalidOperationException("Serialized dependency stamp missing");
                sources[i]["DependencyHash"]=measured.Sources[i].DependencyHash;
            }
            return expected;
        }
        static Dictionary<string,string> RequireThreeTransportGraphs(JToken graphs)
        {
            if(!(graphs is JArray rows)||rows.Count!=3)throw new InvalidOperationException("Exactly three Transport LOD graphs required");
            var result=new Dictionary<string,string>(StringComparer.Ordinal);var lods=new[]{"Detail","Mid","Low"};
            for(int i=0;i<3;i++)
            {
                var graph=rows[i];string root=(string)graph["rootAssetPath"];
                if((string)graph["lod"]!=lods[i]||root!=SkirmishRigidAircraftGeometryAuthoring.Prefabs[i]||
                    !(bool)graph["sourcePresent"]||!(bool)graph["captured"]||
                    string.IsNullOrEmpty((string)graph["dependencyHashAfter"])||(string)graph["dependencyHashBefore"]!=(string)graph["dependencyHashAfter"]||
                    !(graph["dependencies"] is JArray dependencies)||!dependencies.Any(d=>(string)d["assetPath"]==root)||
                    !result.TryAdd(root,(string)graph["dependencyHashAfter"]))
                    throw new InvalidOperationException("Exact unique complete native Transport LOD graph required");
            }
            return result;
        }
        public static bool SameGeometryIgnoringDependencyStamps(SkirmishRigidAircraftGeometryConfig installed,SkirmishRigidAircraftGeometryConfig measured,out string error)
        {
            error="ExactTransport3Sources57DrawsRequired";
            if(installed==null||measured==null||!installed.IsValid||!measured.IsValid||installed.Sources.Length!=3||measured.Sources.Length!=3||installed.Draws.Length!=57||measured.Draws.Length!=57)return false;
            foreach(var draw in measured.Draws)
            {
                var old=installed.Draws.SingleOrDefault(d=>d.Mesh==draw.Mesh&&d.SubMesh==draw.SubMesh);
                if(old==null||!old.LocalBounds.center.Equals(draw.LocalBounds.center)||!old.LocalBounds.size.Equals(draw.LocalBounds.size))
                {error="ExactDrawBoundsChanged";return false;}
            }
            var comparison=ScriptableObject.CreateInstance<SkirmishRigidAircraftGeometryConfig>();
            try
            {
                comparison.SchemaVersion=measured.SchemaVersion;comparison.Draws=measured.Draws;
                comparison.Sources=measured.Sources.Select((s,i)=>new SkirmishRigidAircraftGeometryConfig.Source{Prefab=s.Prefab,Path=s.Path,DependencyHash=installed.Sources[i].DependencyHash}).ToArray();
                return SkirmishRigidAircraftGeometryAuthoring.Equal(installed,comparison,out error);
            }
            finally{UnityEngine.Object.DestroyImmediate(comparison);}
        }
        static Dictionary<string,string> ValidateHistoricalSourceBytes()
        {
            if(Hash(Audit)!=AuditSha)throw new InvalidOperationException("Exact preserved historical source audit required");
            var audit=JObject.Parse(File.ReadAllText(Audit));
            if((string)audit["native_report_sha256"]!="8462a93b09b74531161c18ebd6d069b37891274effc4c2536f361533836aac0f")throw new InvalidOperationException("Historical native provenance mismatch");
            string report=Environment.GetEnvironmentVariable("WARLINE_AIRCRAFT_ROOT_PROVENANCE_REPORT");
            if(string.IsNullOrEmpty(report)||Hash(report)!=Environment.GetEnvironmentVariable("WARLINE_AIRCRAFT_ROOT_PROVENANCE_SHA256"))throw new InvalidOperationException("Explicit fresh native report SHA required");
            var native=JObject.Parse(File.ReadAllText(report));
            if((string)native["scope"]!="ReadOnlyAllSevenCanonicalAircraftRootBladeDrawBindings"||(int)native["sourceRoleCount"]!=7||(int)native["bakedRowCount"]!=7||!(bool)native["bakedRegistryAvailable"])
                throw new InvalidOperationException("Complete actual seven-role native report required");
            var source=native["sources"].Single(s=>(string)s["role"]=="role.transport_plane");
            var originals=audit["all_existing_static_pins"].ToDictionary(p=>(string)p["path"],StringComparer.Ordinal);
            var expectedDependencies=RequireThreeTransportGraphs(source["importerDependencyGraphs"]);
            foreach(var graph in source["importerDependencyGraphs"])
            {
                foreach(var dependency in graph["dependencies"].Where(d=>(string)d["sourceKind"]=="Assets"))
                foreach(string kind in new[]{"source","meta"})
                {
                    string logical=(string)dependency["assetPath"]+(kind=="meta"?".meta":"");var fact=dependency[kind];
                    if((string)fact["status"]!="HashedRawBytes"||!originals.TryGetValue(logical,out var old))throw new InvalidOperationException("Historical source bytes unavailable: "+logical);
                    string actual=Hash(logical);
                    bool original=actual==(string)old["current_sha256"];
                    bool canonical=(bool)old["crlf_normalized_equal_to_git"]&&actual==(string)old["git_blob_sha256"];
                    if(actual!=(string)fact["sha256"]||(!original&&!canonical))throw new InvalidOperationException("Transport source content drift: "+logical);
                }
            }
            return expectedDependencies;
        }
        static string Hash(string path)
        {using var stream=File.OpenRead(path);using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
    }
}
