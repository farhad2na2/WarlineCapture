#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        // Explicit opt-in QA observation. No global profiler/deep-profile settings
        // or normal Runtime diagnostics registration are changed.
        public static void RunS005OpeningWitnessWithLateEcsCensus()
        {
            if(!Environment.GetCommandLineArgs().Any(a=>string.Equals(a,"-warlineProfilerMarkers",StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Existing explicit -warlineProfilerMarkers opt-in required");
            LateEcsRecorderCensus.Arm(Environment.GetEnvironmentVariable("WARLINE_S005_OPENING_GROUP_OUTPUT")+".ecs-recorders.jsonl");
            try{RunS005OpeningPlacementWitnessDiagnostic();}
            catch{LateEcsRecorderCensus.Close("DispatchFailed");throw;}
        }

        static class LateEcsRecorderCensus
        {
            const int MaximumRecorders=512,MaximumHandleFacts=1024,MaximumSamples=12;
            sealed class Owned {public string Name,Category;public ProfilerRecorder Recorder;}
            static readonly Dictionary<string,Owned> owned=new Dictionary<string,Owned>(StringComparer.Ordinal);
            static string path,worldSequence,worldName,attempt;
            static bool active,earlyDiscovered;
            static int playingFrame=-1,nextSampleFrame,discoveries,samples,errors;
            static double lastWriteMilliseconds;

            internal static void Arm(string output)
            {
                if(active||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)
                    throw new InvalidOperationException("Fresh idle late ECS census required");
                string allowed=Path.GetFullPath("Logs/SkirmishProduction")+Path.DirectorySeparatorChar;
                if(string.IsNullOrEmpty(output)||!Path.IsPathRooted(output)||!Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Fresh existing witness output path required");
                path=Path.GetFullPath(output);Directory.CreateDirectory(Path.GetDirectoryName(path));
                using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write)){}
                worldSequence=worldName=attempt=null;playingFrame=-1;nextSampleFrame=0;discoveries=samples=errors=0;
                earlyDiscovered=false;lastWriteMilliseconds=0;
                Write(new JObject{["kind"]="Requested",["settingsChanged"]=false,["readOnlyDiagnostic"]=true,
                    ["maximumRecorders"]=MaximumRecorders,["maximumHandleFacts"]=MaximumHandleFacts,["maximumSamples"]=MaximumSamples,
                    ["discoverySchedule"]="FirstPlayWorld_ThenPlaying_ThenPlayingFramePlus15_Plus45",
                    ["sampleTiming"]="EditorUpdate reads last completed recorder sample; not a same-frame exclusive CPU attribution"});
                active=true;EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=OnPlayMode;EditorApplication.quitting+=OnQuit;
            }
            static void Tick()
            {
                if(!active||!EditorApplication.isPlaying)return;
                var watch=Stopwatch.StartNew();
                try
                {
                    var world=World.DefaultGameObjectInjectionWorld;
                    if(world==null||!world.IsCreated){if(worldSequence!=null)Close("DefaultWorldLost");return;}
                    string sequence=world.SequenceNumber.ToString();
                    if(worldSequence!=null&&worldSequence!=sequence){Close("DefaultWorldChanged");return;}
                    worldSequence=sequence;worldName=world.Name;
                    if(!earlyDiscovered){Discover("FirstPlayWorld",watch);earlyDiscovered=true;}
                    if(!SkirmishLaunchProjection.TryGet(world.EntityManager,out _,out var match)||match.Phase!=SkirmishPhase.Playing)return;
                    if(attempt!=null&&attempt!=match.SessionId.ToString()){Close("NativeAttemptChanged");return;}
                    attempt=match.SessionId.ToString();
                    if(playingFrame<0){playingFrame=Time.frameCount;Discover("Playing",watch);}
                    if(discoveries==2&&Time.frameCount-playingFrame>=15)Discover("PlayingPlus15",watch);
                    if(discoveries==3&&Time.frameCount-playingFrame>=45)Discover("PlayingPlus45",watch);
                    if(Time.frameCount<nextSampleFrame||samples>=MaximumSamples)return;
                    nextSampleFrame=Time.frameCount+5;samples++;
                    var values=new JArray();
                    foreach(var row in owned.Values.OrderBy(r=>r.Name,StringComparer.Ordinal))
                    {
                        var recorder=row.Recorder;
                        values.Add(new JObject{["name"]=row.Name,["category"]=row.Category,["valid"]=recorder.Valid,
                            ["sampleCount"]=recorder.Valid?recorder.Count:0,
                            ["lastValueRaw"]=recorder.Valid&&recorder.Count>0?(JToken)recorder.LastValue:JValue.CreateNull(),
                            ["availability"]=!recorder.Valid?"RecorderInvalid":recorder.Count==0?"NoCompletedSample":"Recorded"});
                    }
                    Write(new JObject{["kind"]="Samples",["sampleIndex"]=samples,["matchElapsedSeconds"]=match.ElapsedSeconds,
                        ["recorderValues"]=values,["observationMillisecondsBeforeWrite"]=watch.Elapsed.TotalMilliseconds});
                }
                catch(Exception failure)
                {errors++;try{Write(new JObject{["kind"]="ObservationError",["error"]=failure.ToString(),["captured"]=false});}finally{Close("ObservationFailed");}}
            }
            static void Discover(string stage,Stopwatch watch)
            {
                var runtime=ReadExistingRuntimeRecorders(out var runtimeNames);
                var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
                var candidates=new List<Tuple<ProfilerRecorderDescription,int>>();
                for(int i=0;i<handles.Count;i++)
                {
                    var desc=ProfilerRecorderHandle.GetDescription(handles[i]);string name=desc.Name;
                    if(name.StartsWith(worldName+" ",StringComparison.Ordinal)&&name.Contains("System",StringComparison.Ordinal))
                        candidates.Add(Tuple.Create(desc,i));
                }
                var facts=new JArray();var seen=new HashSet<string>(StringComparer.Ordinal);
                foreach(var candidate in candidates.OrderBy(v=>v.Item1.Name,StringComparer.Ordinal))
                {
                    if(facts.Count>=MaximumHandleFacts)break;
                    var desc=candidate.Item1;string name=desc.Name,category=desc.Category.Name;string key=category+"|"+name;
                    var fact=new JObject{["name"]=name,["category"]=category,["unitType"]=desc.UnitType.ToString(),
                        ["availableHandleIndex"]=candidate.Item2,["available"]=true,
                        ["existingRuntimeRecordedNamePresent"]=runtimeNames==null?JValue.CreateNull():(JToken)runtimeNames.Contains(name),
                        ["association"]="ExactDefaultWorldNamePrefix_NotExclusiveSystemTiming"};
                    facts.Add(fact);
                    if(!seen.Add(key)){fact["status"]="DuplicateCategoryAndNameHandle";continue;}
                    if(owned.ContainsKey(key)){fact["status"]="PreviouslyRegistered";continue;}
                    if(owned.Count>=MaximumRecorders){fact["status"]="RecorderCap_NotMeasured";continue;}
                    try
                    {
                        var recorder=ProfilerRecorder.StartNew(desc.Category,name);
                        if(!recorder.Valid){recorder.Dispose();fact["status"]="StartNewInvalid_NotMeasured";continue;}
                        owned.Add(key,new Owned{Name=name,Category=category,Recorder=recorder});fact["status"]="NewRecorder";
                    }
                    catch(Exception failure){fact["status"]="StartNewFailed_NotMeasured";fact["error"]=failure.ToString();}
                }
                discoveries++;
                Write(new JObject{["kind"]="HandleCensus",["stage"]=stage,["discoveryIndex"]=discoveries,
                    ["availableHandlesAllCategories"]=handles.Count,["matchingWorldSystemHandles"]=candidates.Count,
                    ["omittedHandleFacts"]=Math.Max(0,candidates.Count-facts.Count),
                    ["ownedRecorders"]=owned.Count,["handleFacts"]=facts,["observationMillisecondsBeforeWrite"]=watch.Elapsed.TotalMilliseconds});
                Write(new JObject{["kind"]="ExistingRuntimeRecorderCensus",["discoveryIndex"]=discoveries,["facts"]=runtime,
                    ["borrowedRecordersStoppedOrDisposed"]=false,["categoryHandleEquivalenceInferred"]=false});
            }
            static JObject ReadExistingRuntimeRecorders(out HashSet<string> names)
            {
                names=null;
                try
                {
                    var world=World.DefaultGameObjectInjectionWorld;
                    if(world==null||!world.IsCreated||!new PerformanceDiagnosticsReferenceCompositionSystemHelper().TryGet(world.EntityManager,out var diagnostics))
                        return new JObject{["captured"]=false,["reason"]="ExistingRuntimeDiagnosticsUnavailable"};
                    var field=typeof(PerformanceDiagnosticsSystemHelper).GetField("_markerRecorders",BindingFlags.NonPublic|BindingFlags.Instance);
                    if(field==null||!(field.GetValue(diagnostics) is IEnumerable list))
                        return new JObject{["captured"]=false,["reason"]="ExistingRuntimeRecorderSchemaUnavailable"};
                    var seen=new HashSet<string>(StringComparer.Ordinal);var rows=new JArray();int total=0;
                    foreach(var item in list)
                    {
                        total++;
                        if(total>MaximumHandleFacts)continue;
                        var type=item.GetType();var nf=type.GetField("Name");var rf=type.GetField("Recorder");
                        if(nf==null||rf==null||rf.FieldType!=typeof(ProfilerRecorder))
                            return new JObject{["captured"]=false,["reason"]="ExistingRuntimeNamedRecorderSchemaChanged"};
                        string name=(string)nf.GetValue(item);var borrowed=(ProfilerRecorder)rf.GetValue(item);
                        if(name==null||!seen.Add(name))return new JObject{["captured"]=false,["reason"]="AmbiguousExistingRuntimeRecorderNames"};
                        // Borrowed recorder value copy: never Start, Stop or Dispose it.
                        if(name.StartsWith(worldName+" ",StringComparison.Ordinal)&&name.Contains("System",StringComparison.Ordinal))
                            rows.Add(new JObject{["name"]=name,["valid"]=borrowed.Valid,["sampleCount"]=borrowed.Valid?borrowed.Count:0,
                                ["lastValueRaw"]=borrowed.Valid&&borrowed.Count>0?(JToken)borrowed.LastValue:JValue.CreateNull()});
                    }
                    names=total<=MaximumHandleFacts?seen:null;
                    return new JObject{["captured"]=true,["totalRuntimeRecorderRows"]=total,["omittedRuntimeRecorderRows"]=Math.Max(0,total-MaximumHandleFacts),
                        ["ecsRowsIncludingZero"]=rows,["namesComplete"]=total<=MaximumHandleFacts};
                }
                catch(Exception failure){return new JObject{["captured"]=false,["reason"]="RuntimeRecorderReadFailed",["error"]=failure.ToString()};}
            }
            static void Write(JObject row)
            {
                row["schema"]=1;row["frame"]=Time.frameCount;row["renderedFrame"]=Time.renderedFrameCount;
                row["worldSequence"]=worldSequence;row["worldName"]=worldName;row["attempt"]=attempt;
                row["previousWriteMilliseconds"]=lastWriteMilliseconds;row["gameplayOrProfilerSettingsChanged"]=false;
                var watch=Stopwatch.StartNew();File.AppendAllText(path,row.ToString(Newtonsoft.Json.Formatting.None)+"\n");
                lastWriteMilliseconds=watch.Elapsed.TotalMilliseconds;
            }
            static void OnPlayMode(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)Close("ExitingPlayMode");}
            static void OnQuit()=>Close("EditorQuitting");
            internal static void Close(string reason)
            {
                if(!active)return;active=false;
                EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=OnPlayMode;EditorApplication.quitting-=OnQuit;
                int disposed=0;var disposalErrors=new JArray();
                foreach(var row in owned.Values)
                {try{row.Recorder.Dispose();disposed++;}catch(Exception failure){errors++;disposalErrors.Add(new JObject{["name"]=row.Name,["error"]=failure.ToString()});}}
                owned.Clear();
                try{Write(new JObject{["kind"]="Closed",["reason"]=reason,["discoveries"]=discoveries,["samples"]=samples,["errors"]=errors,
                    ["disposedRecorders"]=disposed,["remainingOwnedRecorders"]=owned.Count,["disposeErrors"]=disposalErrors,["missionCertified"]=false});}
                catch(Exception failure){errors++;UnityEngine.Debug.LogWarning("[LateEcsRecorderCensus] finalWriteFailed="+failure);}
                UnityEngine.Debug.Log("[LateEcsRecorderCensus] result=CollectedDiagnosticOnly discoveries="+discoveries+" samples="+samples+" errors="+errors+" disposed="+disposed+" output="+path);
            }
        }
    }
}
#endif
