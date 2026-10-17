#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Game.Configs;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private const string ProductionBatchOwnerKey="Warline.Skirmish.ProductionBatchOwner";
        private static ProductionBatchContext productionBatch;
        private static double productionBatchFinishHashMilliseconds=-1;
        private static ProductionBatchContext productionBatchExitOwner;

        // Run through a focused checked wrapper BEFORE the candidate is frozen.
        // It warms the exact native sources and exports real compiled setup facts;
        // it never enters Play, reserves jobs or establishes mission acceptance.
        public static void PrepareProductionBatchSources()
        {
            if(productionBatch!=null||productionCanary!=null||SessionState.GetBool(Key,false)||
                EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)
                throw new InvalidOperationException("Batch source preparation requires an idle owned Editor");
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string path=Environment.GetEnvironmentVariable("WARLINE_PRODUCTION_BATCH_PREPARATION_OUTPUT");
            if(string.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path)||
                !Path.GetFullPath(path).StartsWith(Path.Combine(root,"Logs","SkirmishProduction")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Fresh absolute durable preparation output required");
            var timer=Stopwatch.StartNew();var before=SkirmishProductionBatchOwnershipCensus.Capture();
            ProductionBatchContext.PrepareNativeSceneBaseline();
            var report=new JObject { ["schema_version"]=1,["collector_kind"]="SkirmishProductionBatchNativePreparation",
                ["before_census"]=before,["after_census"]=SkirmishProductionBatchOwnershipCensus.Capture(),
                ["native_compiled_setups"]=SkirmishProductionBatchOwnershipCensus.CaptureCompiledCanaryIdentities(),
                ["elapsed_seconds"]=timer.Elapsed.TotalSeconds,["native_canaries_executed"]=0,["queue_success_enabled"]=false };
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(stream,new UTF8Encoding(false)))writer.Write(report.ToString(Formatting.Indented));
            UnityEngine.Debug.Log("[SkirmishProductionBatchPreparation] result=Passed sources=2 compiledIdentities=2 gameplayCertified=0 output="+path);
        }

        [InitializeOnLoadMethod] private static void PreserveInterruptedProductionBatch()
        {
            if(!string.IsNullOrEmpty(SessionState.GetString(ProductionBatchOwnerKey,""))&&productionBatch==null)
                UnityEngine.Debug.LogError("[SkirmishProductionBatch] result=Failed reason=DomainReloadInterruptedOwnedBatch continuationDisabled=1");
            // Never reconstruct lost live World/input leases or erase a quit request.
            // The missing completion marker makes the checked wrapper fail closed.
        }
        public static void RunProductionBatchCanariesFromEnvironment()
        {
            if(productionBatch!=null||productionCanary!=null||SessionState.GetBool(Key,false)||
                SessionState.GetBool(QuitPendingKey,false)||!string.IsNullOrEmpty(SessionState.GetString(ProductionBatchOwnerKey,""))||
                EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||
                (EditorSettings.enterPlayModeOptions&EnterPlayModeOptions.DisableSceneReload)!=0)
                throw new InvalidOperationException("Batch requires an idle unowned Editor with native scene reload enabled.");
            var context=new ProductionBatchContext(Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_BATCH"));
            productionBatch=context;
            SessionState.SetString(ProductionBatchOwnerKey,context.OutputDirectory);
            context.DispatchNext();
        }

        private static void RecordProductionBatchFingerprint(string endpoint,string actualSource,double elapsedMilliseconds)
        {
            if(productionBatch==null)return;
            productionBatch.Fingerprint(endpoint,actualSource,elapsedMilliseconds);
        }
        private static void ObserveProductionBatchNativeEvent(string kind,JObject facts)
        {
            if(productionBatch==null)return;
            productionBatch.Observe(kind,facts);
        }
        // Called only after the existing native quit owner consumed its own pending
        // request and the single-job context restored scenes/options/environment.
        private static bool TryCompleteProductionBatchJob(int actualOwnerExitCode)
        {
            if(productionBatch==null)return false;
            productionBatch.AfterOwnedPlay(actualOwnerExitCode);
            return true;
        }

        internal sealed class ProductionRuntimeErrorGate
        {
            private int count;
            internal int Count=>System.Threading.Volatile.Read(ref count);
            internal void Observe(string condition,string stackTrace,LogType type)
            {
                if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)System.Threading.Interlocked.Increment(ref count);
            }
            internal void EnsureClean()
            { if(Count!=0)throw new InvalidOperationException("Production runtime Error/Exception/Assert observations="+Count); }
        }
        private sealed class ProductionBatchContext
        {
            internal readonly string OutputDirectory;
            private readonly string descriptorPath,descriptorHash,manifestPath;
            private readonly JObject descriptor;
            private readonly JArray jobs;
            internal readonly SkirmishProductionMatrixAuthority Matrix;
            private readonly Dictionary<string,string> previousEnvironment=new Dictionary<string,string>(StringComparer.Ordinal);
            private readonly SkirmishProductionBatchContract contract;
            private readonly Stopwatch wall=Stopwatch.StartNew();
            private readonly DateTimeOffset enteredAt=DateTimeOffset.UtcNow;
            private readonly string events;
            private SkirmishProductionCanaryRequest current;
            private int next,completed;
            private double cleanupDeadline;
            private double jobDeadline;
            private bool stopped;
            private readonly string baselineOwnership;
            private bool finalExitRequested;
            private int exitInvocations;
            private readonly ProductionRuntimeErrorGate runtimeErrors=new ProductionRuntimeErrorGate();
            private bool runtimeObservationArmed;
            private volatile bool quitGateEmitted;
            private int finalRequestedCode=2;
            internal bool RuntimeErrorsClean=>runtimeErrors.Count==0;
            private void ArmRuntimeObservation()
            {
                if(runtimeObservationArmed)return;
                runtimeObservationArmed=true;
                Application.logMessageReceivedThreaded+=ObserveRuntimeLog;
                EditorApplication.quitting+=RuntimeQuittingGate;
            }
            private void ObserveRuntimeLog(string condition,string stackTrace,LogType type)
            {
                runtimeErrors.Observe(condition,stackTrace,type);
                // Retain this observer until process exit: a later quitting callback
                // can invalidate an earlier clean marker. Log, not LogError, avoids recursion.
                if(quitGateEmitted&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))
                    UnityEngine.Debug.Log("[SkirmishProductionRuntimeGate] result=Failed errors="+runtimeErrors.Count+" reason=ErrorAfterQuitGate");
            }
            private void RuntimeQuittingGate()
            {
                quitGateEmitted=true;
                bool clean=RuntimeErrorsClean&&stopped&&completed==jobs.Count&&finalExitRequested&&finalRequestedCode==0&&exitInvocations==1;
                Write("RuntimeQuittingGate",new JObject{["errors"]=runtimeErrors.Count,["completed_jobs"]=completed,["passed"]=clean});
                UnityEngine.Debug.Log("[SkirmishProductionRuntimeGate] result="+(clean?"Passed":"Failed")+" errors="+runtimeErrors.Count+" jobs="+completed+" actualWrapperExitStillRequired=1");
            }

            internal ProductionBatchContext(string path)
            {
                descriptorPath=Path.GetFullPath(path);descriptorHash=SkirmishProductionCanaryRequest.FileHash(descriptorPath);
                descriptor=SkirmishProductionCanaryRequest.ReadObject(descriptorPath);
                bool matrixMode=(int?)descriptor["schema_version"]==2;
                Exact(descriptor,"schema_version batch_id artifact_directory candidate_sha256 build_sha256 candidate_manifest_path candidate_manifest_sha256 jobs"+(matrixMode?" sampling_plan_path sampling_plan_sha256":""));
                if(descriptor["schema_version"]?.Type!=JTokenType.Integer||((int)descriptor["schema_version"]!=1&&!matrixMode))
                    throw new InvalidDataException("Unknown batch schema.");
                string id=Text(descriptor,"batch_id");
                if(!System.Text.RegularExpressions.Regex.IsMatch(id,"^[A-Za-z0-9][A-Za-z0-9_.-]{0,79}$"))throw new InvalidDataException("Unsafe batch ID.");
                jobs=descriptor["jobs"] as JArray;
                if(jobs==null||jobs.Count<1||jobs.Count>(matrixMode?1200:2)||(!matrixMode&&jobs.Count!=2))throw new InvalidDataException("Finite matrix batches require1..1200 jobs; legacy canary requires exactly two.");
                if(matrixMode)Matrix=new SkirmishProductionMatrixAuthority(Path.GetFullPath(Path.Combine(Application.dataPath,"..")),Text(descriptor,"sampling_plan_path"),Text(descriptor,"sampling_plan_sha256"));
                OutputDirectory=Path.GetFullPath(Text(descriptor,"artifact_directory"));
                var root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
                if(!OutputDirectory.StartsWith(Path.GetFullPath(Path.Combine(root,"Logs","SkirmishProduction"))+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Batch evidence must use this project's durable ignored validation directory.");
                foreach(string tree in new[]{"Assets","Packages","ProjectSettings"})
                    if(OutputDirectory.StartsWith(Path.Combine(root,tree)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||
                        string.Equals(OutputDirectory,Path.Combine(root,tree),StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Batch artifacts must be outside every hashed source tree.");
                if(Path.GetFileName(OutputDirectory)!=id||!Directory.Exists(OutputDirectory))throw new InvalidDataException("Prepare a unique matching batch artifact directory.");
                string sourceManifest=Path.GetFullPath(Text(descriptor,"candidate_manifest_path"));
                byte[] manifestBytes=File.ReadAllBytes(sourceManifest);
                if(SkirmishProductionCanaryRequest.Hash(manifestBytes)!=Text(descriptor,"candidate_manifest_sha256"))throw new InvalidDataException("Shared complete manifest bytes changed.");
                var manifest=SkirmishProductionCanaryRequest.ReadObject(sourceManifest);
                Exact(manifest,"source_manifest candidate_sha256 build_manifest build_sha256");
                if(!(manifest["source_manifest"] is JArray sourceRows)||sourceRows.Count<1||
                    !(manifest["build_manifest"] is JArray buildRows)||buildRows.Count<1||
                    (string)manifest["candidate_sha256"]!=Text(descriptor,"candidate_sha256")||
                    (string)manifest["build_sha256"]!=Text(descriptor,"build_sha256"))throw new InvalidDataException("Shared complete source/DLL manifest identity absent or mismatched.");
                SkirmishProductionBatchManifest.Validate(manifest);
                var attempts=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var catalogs=new HashSet<string>(StringComparer.Ordinal);
                var samplingSlots=new HashSet<string>(StringComparer.Ordinal);
                foreach(var token in jobs)
                {
                    var row=token as JObject;Exact(row,"job_path claim_path");
                    var request=SkirmishProductionCanaryRequest.Read(root,Text(row,"job_path"),Text(row,"claim_path"),false,Matrix);
                    catalogs.Add(request.CatalogId);
                    if(Matrix!=null&&!samplingSlots.Add(request.CatalogId+"|"+request.Seed+"|"+(string)request.Job["locale"]))throw new InvalidDataException("Duplicate matrix sampling slot in finite batch");
                    if(SkirmishProductionCanaryRequest.Canonical(request.Job["source_manifest"])!=SkirmishProductionCanaryRequest.Canonical(sourceRows))
                        throw new InvalidDataException("Job must reference exactly the complete frozen candidate census");
                    if(!attempts.Add(request.ArtifactDirectory)||(string)request.Job["candidate_sha256"]!=(string)descriptor["candidate_sha256"]||
                        (string)request.Job["build_sha256"]!=(string)descriptor["build_sha256"])
                        throw new InvalidDataException("Batch attempts must be distinct and pin one frozen candidate/build.");
                }
                if(Matrix==null&&(catalogs.Count!=2||!catalogs.Contains("S004")||!catalogs.Contains("S005")))
                    throw new InvalidDataException("Exactly one S004 and one S005 real development canary required");
                Matrix?.ValidateBatch(jobs.OfType<JObject>().Select(v=>SkirmishProductionCanaryRequest.ReadObject(Text(v,"job_path"))));
                for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                    if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty Editor scene.");
                // One atomic start sentinel. A crashed or partial batch is never resumed
                // by overwriting it; original evidence remains available for a new claim.
                using(var output=new FileStream(Path.Combine(OutputDirectory,"batch-started.json"),FileMode.CreateNew,FileAccess.Write))
                using(var writer=new StreamWriter(output,new UTF8Encoding(false)))writer.Write(descriptor.ToString(Formatting.Indented));
                // The immutable content-addressed candidate store owns full rows
                // once. Every job records this exact file/hash reference.
                manifestPath=sourceManifest;
                events=Path.Combine(OutputDirectory,"batch-events.jsonl");
                using(File.Open(events,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){}
                Write("BeforeNativeScenePreparation",new JObject {
                    ["census"]=SkirmishProductionBatchOwnershipCensus.Capture(),
                    ["entry_process_age_seconds"]=(enteredAt.UtcDateTime-Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds });
                // Same-process native streaming preparation establishes package-owned
                // lazy companion scenes BEFORE the strict baseline. No gameplay
                // systems/input/default World or unrelated preview scene are touched.
                var preparationTimer=Stopwatch.StartNew();
                PrepareNativeSceneBaseline(jobs.Select(v=>SkirmishProductionCanaryRequest.ReadObject(Text((JObject)v,"job_path"))));
                preparationTimer.Stop();
                var ownership=SkirmishProductionBatchOwnershipCensus.Capture();
                baselineOwnership=SkirmishProductionBatchOwnershipCensus.CleanupSignature(ownership);
                contract=new SkirmishProductionBatchContract((string)descriptor["candidate_sha256"],(string)descriptor["build_sha256"],
                    (string)descriptor["candidate_manifest_sha256"],Worlds(),Scenes(),EditorSceneManager.previewSceneCount);
                foreach(string key in new[]{"WARLINE_SKIRMISH_PRODUCTION_JOB","WARLINE_SKIRMISH_PRODUCTION_CLAIM","WARLINE_ARIA_QUIT"})
                    previousEnvironment[key]=Environment.GetEnvironmentVariable(key);
                Write("BatchCaptured",new JObject { ["baseline_worlds"]=new JArray(contract.BaselineWorlds),
                    ["baseline_scenes"]=contract.BaselineScenes,["baseline_preview_count"]=contract.BaselinePreviewCount,
                    ["baseline_ownership_census"]=ownership,["scene_preparation_milliseconds"]=preparationTimer.Elapsed.TotalMilliseconds });
            }
            internal void DispatchNext()
            {
                if(stopped)return;
                ArmRuntimeObservation();
                try
                {
                    runtimeErrors.EnsureClean();
                    if(next==jobs.Count){Complete();return;}
                    var row=(JObject)jobs[next];
                    current=SkirmishProductionCanaryRequest.Read(Path.GetFullPath(Path.Combine(Application.dataPath,"..")),Text(row,"job_path"),Text(row,"claim_path"),false,Matrix);
                    contract.Start((string)current.Job["candidate_sha256"],(string)current.Job["build_sha256"]);
                    jobDeadline=EditorApplication.timeSinceStartup+(int)current.Job["timeout_seconds"];
                    EditorApplication.update-=Watchdog;EditorApplication.update+=Watchdog;
                    Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_JOB",current.JobPath);
                    Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_CLAIM",current.ClaimPath);
                    Environment.SetEnvironmentVariable("WARLINE_ARIA_QUIT","1");
                    next++;
                    Write("Dispatch",new JObject { ["job_sha256"]=current.JobHash,["attempt_id"]=(string)current.Claim["attempt_id"],
                        ["manifest_path"]=manifestPath,["manifest_sha256"]=contract.ManifestFileHash,
                        ["before_play_ownership_census"]=SkirmishProductionBatchOwnershipCensus.Capture() });
                    RunProductionJobFromEnvironment();
                }
                catch(Exception error){Fail(error);}
            }
            internal void Fingerprint(string endpoint,string actualSource,double milliseconds)
            {
                CheckLiveInputs();
                var dllTimer=Stopwatch.StartNew();
                string folder=Path.Combine(current.ProjectRoot,"Library/ScriptAssemblies");
                var rows=new JArray(Directory.GetFiles(folder,"*.dll").OrderBy(p=>p,StringComparer.Ordinal).Select(p=>new JObject {
                    ["path"]=p.Substring(current.ProjectRoot.Length+1).Replace('\\','/'),["sha256"]=SkirmishProductionCanaryRequest.FileHash(p) }));
                if(rows.Count==0)throw new InvalidDataException("Actual native DLL files absent.");
                string build=SkirmishProductionCanaryRequest.Hash(Encoding.UTF8.GetBytes(SkirmishProductionCanaryRequest.Canonical(rows)));
                dllTimer.Stop();
                contract.Fingerprint(endpoint,actualSource,build,SkirmishProductionCanaryRequest.FileHash(manifestPath));
                Write("NativeFingerprint",new JObject { ["endpoint"]=endpoint,["candidate_sha256"]=actualSource,
                    ["build_sha256"]=build,["source_hash_milliseconds"]=milliseconds,["dll_hash_milliseconds"]=dllTimer.Elapsed.TotalMilliseconds,
                    ["manifest_path"]=manifestPath,["manifest_sha256"]=contract.ManifestFileHash,["job_sha256"]=current.JobHash });
            }
            internal void Observe(string kind,JObject facts)
            {
                if(kind=="NormalMenuLaunchObserved"||kind=="NativeResolvedSnapshot"||kind=="TerminalSnapshot")
                {
                    if((string)facts["phase"]=="Finished"&&(string)facts["result_session_id"]!=(string)facts["native_session_id"])
                        throw new InvalidDataException("Frozen native result session differs.");
                    if((string)facts["phase"]=="Finished"&&(string)facts["result_outcome"]!=(string)facts["outcome"])
                        throw new InvalidDataException("Frozen native result outcome differs.");
                    contract.NativeSnapshot((string)facts["native_world_sequence"],(string)facts["native_session_id"],(string)facts["phase"],
                        (string)facts["outcome"],facts["result_frozen"]?.Type==JTokenType.Integer&&(int)facts["result_frozen"]!=0);
                }
                if(kind=="TerminalReturnOwnerCompleted")
                {
                    if(Matrix!=null&&(facts["native_terminal_session_count"]?.Type!=JTokenType.Integer||
                        (int)facts["native_terminal_session_count"]!=0||
                        facts["native_outcome_matches_rendered_title"]?.Type!=JTokenType.Boolean||
                        !(bool)facts["native_outcome_matches_rendered_title"]||
                        facts["localized_main_menu_presentation"]==null||facts["localized_main_menu_presentation"].Type==JTokenType.Null))
                        throw new InvalidDataException("Matrix continuation requires actual zero native sessions and joined rendered result/localized MainMenu");
                    if(facts["owner_probe_passed"]?.Type!=JTokenType.Boolean||!(bool)facts["owner_probe_passed"])
                        throw new InvalidDataException("Actual terminal owner reported failure.");
                    contract.Return((string)facts["native_terminal_session_id"],(string)facts["actual_shell_route"],
                        facts["actual_shell_transition_running"]?.Type!=JTokenType.Boolean||(bool)facts["actual_shell_transition_running"],
                        facts["native_match_session_present"]?.Type!=JTokenType.Boolean||(bool)facts["native_match_session_present"],
                        (string)facts["presented_menu_control"],(string)facts["terminal_action"]);
                }
            }
            internal void AfterOwnedPlay(int code)
            {
                EditorApplication.update-=Watchdog;
                if(stopped){productionBatch=null;QueueFinalExit(2);return;}
                try
                {
                    runtimeErrors.EnsureClean();
                    contract.Finish(code);cleanupDeadline=EditorApplication.timeSinceStartup+30;
                    EditorApplication.update-=WaitForCleanup;EditorApplication.update+=WaitForCleanup;
                }
                catch(Exception error){Fail(error);}
            }
            private void WaitForCleanup()
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)
                {
                    if(EditorApplication.timeSinceStartup>cleanupDeadline)Fail(new TimeoutException("Owned Play/compile/import did not settle before batch cleanup deadline."));
                    return;
                }
                try
                {
                    CheckLiveInputs();
                    if(!Worlds().SequenceEqual(contract.BaselineWorlds)||Scenes()!=contract.BaselineScenes||EditorSceneManager.previewSceneCount!=contract.BaselinePreviewCount)
                    {
                        if(EditorApplication.timeSinceStartup<=cleanupDeadline)return;
                    }
                    var census=SkirmishProductionBatchOwnershipCensus.Capture();
                    try
                    {
                        if(SkirmishProductionBatchOwnershipCensus.CleanupSignature(census)!=baselineOwnership&&
                            EditorApplication.timeSinceStartup<=cleanupDeadline)return;
                    }
                    catch(Exception) when(EditorApplication.timeSinceStartup<=cleanupDeadline){return;}
                    Write("CleanupOwnershipCensus",new JObject { ["job_sha256"]=current.JobHash,["census"]=census });
                    if(SkirmishProductionBatchOwnershipCensus.CleanupSignature(census)!=baselineOwnership)
                        throw new InvalidOperationException("Native scene/root/preview/provider/session ownership did not restore exactly");
                    contract.Cleanup(Worlds(),Scenes(),EditorSceneManager.previewSceneCount);
                    if(productionCanary!=null||menuTouch!=null||manualTouch!=null||ariaConsentTouch!=null||terminalTouch!=null||playerResumeTouch!=null||nativeProductionTouch!=null||helipadProbeTouch!=null||backgroundInputScope!=null)
                        throw new InvalidOperationException("Owned input helper remains alive after native teardown.");
                    Write("CleanupMeasured",new JObject { ["worlds"]=new JArray(Worlds()),["scenes"]=Scenes(),["preview_count"]=EditorSceneManager.previewSceneCount,
                        ["job_sha256"]=current.JobHash,["queue_success_enabled"]=false });
                    EditorApplication.update-=WaitForCleanup;completed++;
                    EditorApplication.delayCall+=DispatchNext;
                }
                catch(Exception error){Fail(error);}
            }
            private void CheckLiveInputs()
            {
                runtimeErrors.EnsureClean();
                Matrix?.Recheck();
                if(SkirmishProductionCanaryRequest.FileHash(descriptorPath)!=descriptorHash||
                    SkirmishProductionCanaryRequest.Hash(Encoding.UTF8.GetBytes(SkirmishProductionCanaryRequest.Canonical(SkirmishProductionCanaryRequest.ReadObject(current.JobPath))))!=current.JobHash||
                    SkirmishProductionCanaryRequest.Canonical(SkirmishProductionCanaryRequest.ReadObject(current.ClaimPath))!=SkirmishProductionCanaryRequest.Canonical(current.Claim)||
                    SkirmishProductionCanaryRequest.Canonical(SkirmishProductionCanaryRequest.ReadObject(Path.Combine(current.ArtifactDirectory,"claim.json")))!=SkirmishProductionCanaryRequest.Canonical(current.Claim))
                    throw new InvalidDataException("Batch descriptor or canonical live job/claim changed.");
            }
            private void Complete()
            {
                runtimeErrors.EnsureClean();
                stopped=true;Write("BatchCompleted",new JObject { ["completed_jobs"]=completed,["elapsed_seconds"]=wall.Elapsed.TotalSeconds,["queue_success_enabled"]=false });
                SessionState.EraseString(ProductionBatchOwnerKey);productionBatch=null;
                UnityEngine.Debug.Log("[SkirmishProductionBatch] result=Completed jobs="+completed+" queueSuccessEnabled=0");
                QueueFinalExit(0);
            }
            private void Fail(Exception error)
            {
                if(stopped)return;stopped=true;contract.Fail(error.ToString());EditorApplication.update-=WaitForCleanup;
                EditorApplication.update-=Watchdog;
                Write("BatchFailed",new JObject { ["error"]=error.ToString(),["completed_jobs"]=completed,["queue_success_enabled"]=false });
                UnityEngine.Debug.LogError("[SkirmishProductionBatch] result=Failed error="+error);
                // During an owned Play session, only its existing Finish path exits it.
                // Preserve ownership/context and source artifacts on partial cleanup.
                if(EditorApplication.isPlayingOrWillChangePlaymode)Finish(true,"productionBatch:"+error.GetType().Name);
                else {productionBatch=null;QueueFinalExit(2);}
            }
            private void Watchdog()
            {
                if(!stopped&&EditorApplication.timeSinceStartup>jobDeadline)
                    Fail(new TimeoutException("Owned batch job exceeded its explicit wall deadline; no game outcome inferred."));
            }
            private void QueueFinalExit(int code)
            {
                if(finalExitRequested)throw new InvalidOperationException("A batch owner already requested its single final native exit");
                if(!RuntimeErrorsClean)code=2;
                finalRequestedCode=code;
                finalExitRequested=true;productionBatchExitOwner=this;
                Write("FinalExitQueued",new JObject { ["requested_code"]=code,["actual_process_exit"]="Pending checked wrapper" });
                // Never call the native Exit owner from inside its current update
                // callback. Consume the old request, restore, then queue once.
                EditorApplication.delayCall+=()=>RequestFinalExit(code);
            }
            private void RequestFinalExit(int code)
            {
                Environment.SetEnvironmentVariable("WARLINE_ARIA_QUIT","1");
                RequestQuitIfAsked(RuntimeErrorsClean?code:2);
                RestoreEnvironment();
            }
            internal void RecordActualExitInvocation(int code)
            {
                if(!finalExitRequested||++exitInvocations!=1)
                    throw new InvalidOperationException("Repeated or unowned native Editor exit invocation refused");
                Write("NativeExitInvocation",new JObject { ["requested_code"]=code,["invocations"]=exitInvocations,
                    ["actual_process_exit"]="Pending external checked-wrapper receipt" });
            }

            internal static void PrepareNativeSceneBaseline(IEnumerable<JObject> plannedJobs=null)
            {
                var original=World.DefaultGameObjectInjectionWorld;
                var systems=DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming,true);
                foreach(var system in systems)
                    if(TypeManager.GetSystemName(system).ToString().StartsWith("Game.",StringComparison.Ordinal))
                        throw new InvalidOperationException("Gameplay system unexpectedly entered streaming-only batch preparation");
                var catalog=SkirmishExpansionCatalogConfig.Load();
                    var guids=new HashSet<string>(StringComparer.Ordinal);
                    foreach(string id in plannedJobs==null?new[]{"S004","S005"}:plannedJobs.Select(v=>(string)v["catalog_id"]).Distinct(StringComparer.Ordinal))
                    {
                        if(catalog==null||!catalog.TryGetDefinition(id,out var definition)||definition.CampaignMap==null)
                            throw new InvalidDataException("Actual packaged canary source definition missing");
                        guids.Add(definition.CampaignMap.NavigationMetadata.AuthoredSubSceneGuid);
                    }
                    guids.Add(AssetDatabase.AssetPathToGUID("Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity"));
                    foreach(string guid in guids.OrderBy(x=>x,StringComparer.Ordinal))
                    {
                      using(var world=new World("ProductionBatchNativeScenePreparation",WorldFlags.Game))
                      {
                        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world,systems);
                        if(string.IsNullOrWhiteSpace(guid))throw new InvalidDataException("Exact source SubScene identity absent");
                        var scene=SceneSystem.LoadSceneAsync(world.Unmanaged,new Unity.Entities.Hash128(guid),new SceneSystem.LoadParameters {
                            Flags=SceneLoadFlags.BlockOnImport|SceneLoadFlags.BlockOnStreamIn });
                        world.Update();
                        if(!SceneSystem.IsSceneLoaded(world.Unmanaged,scene))throw new InvalidOperationException("Native source preparation incomplete");
                    }
                }
                if(World.DefaultGameObjectInjectionWorld!=original)
                    throw new InvalidOperationException("Streaming preparation changed default World ownership");
            }
            private void RestoreEnvironment(){foreach(var pair in previousEnvironment)Environment.SetEnvironmentVariable(pair.Key,pair.Value);}
            private void Write(string kind,JObject facts)
            {
                File.AppendAllText(events,new JObject { ["schema_version"]=1,["kind"]=kind,["utc"]=DateTimeOffset.UtcNow.ToString("O"),
                    ["elapsed_seconds"]=wall.Elapsed.TotalSeconds,["facts"]=facts }.ToString(Formatting.None)+"\n",new UTF8Encoding(false));
            }
            private static string[] Worlds()
            {var rows=new List<string>();foreach(var world in World.All)if(world.IsCreated)rows.Add(world.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));return rows.OrderBy(v=>v,StringComparer.Ordinal).ToArray();}
            private static string Scenes()=>SkirmishProductionCanaryRequest.Canonical(new JArray(EditorSceneManager.GetSceneManagerSetup().Select(s=>new JObject {
                ["path"]=s.path,["loaded"]=s.isLoaded,["active"]=s.isActive })));
            private static string Text(JObject value,string key)
            {
                if(value?[key]?.Type!=JTokenType.String||string.IsNullOrEmpty((string)value[key]))throw new InvalidDataException("Missing batch text: "+key);return (string)value[key];
            }
            private static void Exact(JObject value,string keys)
            {
                if(value==null||!value.Properties().Select(p=>p.Name).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(keys.Split(' ').OrderBy(x=>x,StringComparer.Ordinal)))
                    throw new InvalidDataException("Unknown or missing batch fields.");
            }
        }
    }
}
#endif
