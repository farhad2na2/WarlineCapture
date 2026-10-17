#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        public static void CollectCanonicalAircraftRootProvenance()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && ground30MemoryWriter == null,
                "Idle original-system Support collector required; no memory-system experiment may own it");
            string output=Environment.GetEnvironmentVariable("WARLINE_AIRCRAFT_ROOT_SUPPORT_OUTPUT");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(!string.IsNullOrWhiteSpace(output)&&Path.IsPathRooted(output)&&Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),
                "Fresh absolute Support provenance output under Logs/SkirmishProduction required");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using var stream=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
            using var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)){AutoFlush=true};
            Action<string,JObject> write=(phase,facts)=>writer.WriteLine(new JObject{
                ["phase"]=phase,["utc"]=DateTime.UtcNow.ToString("o"),["facts"]=facts,
                ["templateOnly"]=true,["cameraAccepted"]=false,["normalMissionReady"]=false}.ToString(Newtonsoft.Json.Formatting.None));
            // Use the same actual package initializer and exact ownership census as
            // the current source survey. This may initialize package-owned companion
            // scenes; it happens before our owned baseline, never by closing previews.
            var prewarm=Ground30PrewarmCompanions();
            var ownershipBefore=Ground30SceneOwnership();
            var worldsBefore=CaptureMaterializationWorldCensus().ToArray();
            var defaultBefore=World.DefaultGameObjectInjectionWorld;
            int scenesBefore=UnityEngine.SceneManagement.SceneManager.sceneCount;
            int previewsBefore=UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
            var originalSystems=DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming,true);
            var systemNames=new JArray(originalSystems.Select(s=>TypeManager.GetSystemName(s).ToString()));
            write("OwnedBaseline",new JObject{["companionPrewarm"]=prewarm,["sceneOwnership"]=ownershipBefore.DeepClone(),
                ["worlds"]=new JArray(worldsBefore),["systemSelector"]="OriginalEditorInclusiveStreaming",
                ["streamingSystems"]=systemNames,["systemCount"]=originalSystems.Count,["supportPath"]=Support,["supportSha256"]=Hash(Support)});
            bool collected=false;string collection=null;
            try
            {
                // ground30MemoryWriter remains null: StreamingWorld therefore takes
                // the original Editor-inclusive system set, never the five-system experiment.
                using(var world=StreamingWorld())
                {
                    write("WorldCreated",new JObject{["worldSequence"]=world.SequenceNumber,["worldName"]=world.Name});
                    Load(world,AssetDatabase.AssetPathToGUID(Support));
                    write("SupportLoaded",new JObject{["worldSequence"]=world.SequenceNumber});
                    collection=SkirmishAircraftRootProvenance.CollectExistingWorld(world);
                    Require(File.Exists(collection),"Exact collector output missing");
                    var report=JObject.Parse(File.ReadAllText(collection));
                    write("ProvenanceCollected",new JObject{["path"]=collection,["sha256"]=Hash(collection),
                        ["bakedRegistryAvailable"]=report["bakedRegistryAvailable"],
                        ["sourceRoleCount"]=report["sourceRoleCount"],["bakedRowCount"]=report["bakedRowCount"]});
                    Require(HasCompleteCanonicalAircraftCoverage(report,out string coverageError),coverageError);
                }
                collected=true;
            }
            catch(Exception error){write("CollectionFailed",new JObject{["error"]=error.ToString()});throw;}
            finally
            {
                var ownershipAfter=Ground30SceneOwnership();
                bool restored=worldsBefore.SequenceEqual(CaptureMaterializationWorldCensus())&&ReferenceEquals(defaultBefore,World.DefaultGameObjectInjectionWorld)&&
                    scenesBefore==UnityEngine.SceneManagement.SceneManager.sceneCount&&previewsBefore==UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount&&
                    JToken.DeepEquals(ownershipBefore,ownershipAfter);
                write("OwnedCleanup",new JObject{["collected"]=collected,["ownershipRestored"]=restored,["sceneOwnership"]=ownershipAfter,["worlds"]=new JArray(CaptureMaterializationWorldCensus())});
                Require(restored,"Aircraft Support collection changed exact World/scene/preview ownership; no arbitrary cleanup allowed");
            }
            Debug.Log("[CanonicalAircraftRootSupportProvenance] result=Collected rolesExpected=7 templateOnly=1 defaultWorldPreserved=1 cameraAccepted=0 output="+output+" collection="+collection);
        }
        static bool HasCompleteCanonicalAircraftCoverage(JObject report,out string error)
        {
            error="Actual seven-role Support template coverage unavailable or contradictory";
            if(report["bakedRegistryAvailable"]?.Type!=JTokenType.Boolean||(bool)report["bakedRegistryAvailable"]!=true||
                report["sources"] is not JArray sources||sources.Count!=7||report["bakedRows"] is not JArray baked)return false;
            var sourceKeys=sources.OfType<JObject>().Select(r=>(string)r["runtimePrefabKey"]).ToArray();
            if(sourceKeys.Length!=7||sourceKeys.Any(string.IsNullOrWhiteSpace)||sourceKeys.Distinct(StringComparer.Ordinal).Count()!=7)return false;
            var expected=new System.Collections.Generic.HashSet<string>(sourceKeys,StringComparer.Ordinal);
            var handles=new System.Collections.Generic.Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var token in baked)
            {
                if(token is not JObject row||row["sourceTemplate"]?.Type!=JTokenType.Boolean||(bool)row["sourceTemplate"]!=true||
                    row["prefabActor"] is not JObject actor||actor["index"]?.Type!=JTokenType.Integer||actor["version"]?.Type!=JTokenType.Integer||
                    (long)actor["index"]<0||(long)actor["version"]<=0)return false;
                string key=(string)row["key"];
                if(key==null||!expected.Contains(key))return false;
                string handle=actor["index"].ToString()+":"+actor["version"].ToString();
                if(handles.TryGetValue(key,out string oldHandle)&&oldHandle!=handle)return false;
                handles[key]=handle; // repeated registry references to the same actual prefab may deduplicate.
            }
            if(handles.Count!=7||handles.Values.Distinct(StringComparer.Ordinal).Count()!=7)return false;
            error=null;return true;
        }
    }
}
#endif
