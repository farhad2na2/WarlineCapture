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
using Game.Composition;
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

        // Installed InputSystem1.19 restores native devices on its zero-input initial
        // update. Establish that real package-owned state before the strict baseline.
        private static JObject PrepareProductionNativeInputBaseline()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||
                productionCanary!=null||SessionState.GetBool(Key,false))
                throw new InvalidOperationException("Native input preparation requires idle pre-dispatch Editor state.");
            var type=typeof(UnityEngine.InputSystem.InputSystem);
            var package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(type.Assembly);
            if(package==null||package.name!="com.unity.inputsystem"||package.version!="1.19.0"||
                SkirmishProductionCanaryRequest.FileHash(Path.Combine(package.resolvedPath,"InputSystem","InputSystem.cs"))!="c6a32a4cd7af54de398e3d71ee89317d7b69af78176087c409890c7973b731eb"||
                SkirmishProductionCanaryRequest.FileHash(Path.Combine(package.resolvedPath,"InputSystem","InputManager.cs"))!="71c69ea70d421d7c44c890fd8a7c6ee9d2c20990a1a00265071db9c9f691dbb4")
                throw new InvalidOperationException("Unsupported InputSystem package/source contract; zero-input preparation requires revalidation.");
            var method=type.GetMethod("RunInitialUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static,
                null,Type.EmptyTypes,null);
            if(method==null||method.DeclaringType!=type||method.ReturnType!=typeof(void))
                throw new InvalidOperationException("Reviewed InputSystem zero-input initialization API unavailable; no normal-update fallback.");
            var before=SkirmishProductionBatchOwnershipCensus.Capture();
            method.Invoke(null,null);
            var after=SkirmishProductionBatchOwnershipCensus.Capture();
            return new JObject { ["schema_version"]=1,["initialization"]="InputSystem.RunInitialUpdate",
                ["update_type"]="None",["input_events_processed"]=false,["unity_version"]=Application.unityVersion,
                ["input_assembly"]=type.Assembly.FullName,["package_name"]=package.name,["package_version"]=package.version,["before_devices"]=before["input_devices"].DeepClone(),
                ["after_devices"]=after["input_devices"].DeepClone() };
        }

        private const string ProductionInputPreparationKey="Warline.Production.NativeInputPreparation";
        private static bool productionInputPreparationPending;
        private sealed class ProductionNativeInputReadinessPolicy
        {
            private readonly double started;
            private double last;
            private JArray previous;
            internal int Observations { get; private set; }
            internal ProductionNativeInputReadinessPolicy(double now)
            {
                if(double.IsNaN(now)||double.IsInfinity(now))throw new InvalidDataException("Finite native input preparation time required.");
                started=last=now;
            }
            internal static bool HasWindowsNativeDevices(JArray devices)
            {
                if(devices==null)return false;
                bool Has(string layout)=>devices.OfType<JObject>().Any(d=>
                    d["layout"]?.Type==JTokenType.String&&(string)d["layout"]==layout&&
                    d["native"]?.Type==JTokenType.Boolean&&(bool)d["native"]&&
                    d["enabled"]?.Type==JTokenType.Boolean&&(bool)d["enabled"]&&
                    d["added"]?.Type==JTokenType.Boolean&&(bool)d["added"]);
                return Has("Keyboard")&&Has("Mouse");
            }
            internal bool Observe(double now,JArray devices)
            {
                if(double.IsNaN(now)||double.IsInfinity(now)||now<=last)
                    throw new InvalidDataException("Distinct increasing genuine Editor update observation required.");
                if(now-started>=120)throw new TimeoutException("Native input initialization exceeded120s before mission dispatch.");
                last=now;Observations++;
                if(!HasWindowsNativeDevices(devices)){previous=null;return false;}
                bool stable=previous!=null&&JToken.DeepEquals(previous,devices);
                previous=(JArray)devices.DeepClone();
                return stable;
            }
        }
        private static void BeginProductionNativeInputReadiness(Action<JObject> ready,Action<Exception> failed)
        {
            if(ready==null||failed==null||productionInputPreparationPending||
                SessionState.GetBool(ProductionInputPreparationKey,false)||productionBatch!=null||productionCanary!=null||
                SessionState.GetBool(Key,false)||SessionState.GetBool(QuitPendingKey,false)||
                EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)
                throw new InvalidOperationException("Fresh idle unowned native input initialization required.");
            var initial=PrepareProductionNativeInputBaseline();
            double start=EditorApplication.timeSinceStartup;
            var policy=new ProductionNativeInputReadinessPolicy(start);
            EditorApplication.CallbackFunction tick=null;
            Action quitting=null;
            productionInputPreparationPending=true;SessionState.SetBool(ProductionInputPreparationKey,true);
            void Detach()
            {
                EditorApplication.update-=tick;EditorApplication.quitting-=quitting;
                productionInputPreparationPending=false;SessionState.SetBool(ProductionInputPreparationKey,false);
            }
            quitting=()=>{Detach();UnityEngine.Debug.Log("[ProductionNativeInputReadiness] result=Abandoned missionDispatched=0");};
            tick=()=>
            {
                try
                {
                    if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||
                        productionBatch!=null||productionCanary!=null||SessionState.GetBool(Key,false))
                        throw new InvalidOperationException("Native input initialization lost idle ownership.");
                    var devices=(JArray)SkirmishProductionBatchOwnershipCensus.Capture()["input_devices"];
                    double now=EditorApplication.timeSinceStartup;
                    if(!policy.Observe(now,devices))return;
                    var report=new JObject { ["initial"]=initial,["stable_devices"]=devices.DeepClone(),
                        ["genuine_editor_updates"]=policy.Observations,["stable_observations"]=2,
                        ["elapsed_seconds"]=now-start,["initialization_timeout_seconds"]=120,
                        ["ordinary_input_update_invoked"]=false,["mission_dispatched"]=false };
                    Detach();
                    UnityEngine.Debug.Log("[ProductionNativeInputReadiness] result=Ready "+report.ToString(Newtonsoft.Json.Formatting.None));
                    ready(report);
                }
                catch(Exception error){Detach();failed(error);}
            };
            EditorApplication.quitting+=quitting;EditorApplication.update+=tick;
        }
        private static void FailProductionNativeInputReadiness(Exception error)
        {
            UnityEngine.Debug.LogException(error);
            // Existing dispatch exception handling owns an already-started Play teardown.
            if(!EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.Exit(2);
        }
        private static Action PreflightProductionBatchInputRequests(string path)
        {
            var pins=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            void Pin(string p){p=Path.GetFullPath(p);pins[p]=SkirmishProductionCanaryRequest.FileHash(p);}
            var descriptor=SkirmishProductionCanaryRequest.ReadObject(path);Pin(path);
            var jobs=descriptor["jobs"]as JArray;
            if(jobs==null||jobs.Count<1||jobs.Count>1200)throw new InvalidDataException("Finite actual batch requests required before input initialization.");
            var root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            var matrix=(int?)descriptor["schema_version"]==2
                ?new SkirmishProductionMatrixAuthority(root,(string)descriptor["sampling_plan_path"],(string)descriptor["sampling_plan_sha256"]):null;
            foreach(JObject row in jobs)
            {
                var request=SkirmishProductionCanaryRequest.Read(root,(string)row["job_path"],(string)row["claim_path"],false,matrix);
                Pin(request.JobPath);Pin(request.ClaimPath);
            }
            return ()=>
            {
                foreach(var row in pins)
                    if(SkirmishProductionCanaryRequest.FileHash(row.Key)!=row.Value)
                        throw new InvalidDataException("Actual batch request changed during native input initialization.");
            };
        }

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
            ProductionBatchContext.BeginNativeSceneBaseline(null,preparation=>
            {
            var report=new JObject { ["schema_version"]=1,["collector_kind"]="SkirmishProductionBatchNativePreparation",
                ["before_census"]=before,["after_census"]=SkirmishProductionBatchOwnershipCensus.Capture(),
                ["native_compiled_setups"]=SkirmishProductionBatchOwnershipCensus.CaptureCompiledCanaryIdentities(),
                ["elapsed_seconds"]=timer.Elapsed.TotalSeconds,["native_canaries_executed"]=0,["queue_success_enabled"]=false,
                ["async_scene_preparation"]=preparation };
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(stream,new UTF8Encoding(false)))writer.Write(report.ToString(Formatting.Indented));
            // Focused source-preparation entry is launched GUI/no-quit; it owns this terminal only after completion.
            UnityEngine.Debug.Log("[SkirmishProductionBatchPreparation] result=Passed sources=2 compiledIdentities=2 gameplayCertified=0 output="+path);
            EditorApplication.Exit(0);
            },FailProductionNativeInputReadiness);
        }

        [InitializeOnLoadMethod] private static void PreserveInterruptedProductionBatch()
        {
            if(!string.IsNullOrEmpty(SessionState.GetString(ProductionBatchOwnerKey,""))&&productionBatch==null)
                UnityEngine.Debug.LogError("[SkirmishProductionBatch] result=Failed reason=DomainReloadInterruptedOwnedBatch continuationDisabled=1");
            // Never reconstruct lost live World/input leases or erase a quit request.
            // The missing completion marker makes the checked wrapper fail closed.
        }
        // Warmup follows the existing coded runtime binding; it never authors a source.
        internal static OperationMapDefinition ResolveProductionBatchWarmupSource(SkirmishScenarioDefinitionConfig definition)
        {
            if (definition == null) throw new InvalidDataException("Actual packaged canary definition missing");
            if (definition.CampaignMap != null) return definition.CampaignMap;
            string id = definition.CatalogId;
            if ((id != "S002" && id != "S003" && id != "S004") ||
                definition.OperationMapId != SkirmishLaunchProjection.OperationMapId ||
                !SkirmishMissionDispatch.TryGetLegacyIndex(id, out int index))
                throw new InvalidDataException("Explicit packaged canary source definition missing: " + id);
            // S004 has an existing runtime environment override before the preset
            // fallback (SkirmishS004Environment.LoadMapForSession). Resolve that same
            // resource; a shared preset is not proof of a shared physical map.
            if (id == "S004")
            {
                var environment = Resources.Load<OperationMapDefinition>("SkirmishS004OperationMap");
                if (environment == null || environment.OperationMapId != definition.OperationMapId)
                    throw new InvalidDataException("Actual S004 environment source missing or changed");
                return environment;
            }
            // S002/S003 coded dispatches load the very same existing Desert Base preset.
            // Its omitted operationMap is resolved through the serialized normal
            // MatchSceneView catalog by the existing production identity authority.
            var baseline = SkirmishLegacyProductionIdentity.RequireBinding("S001");
            if (SkirmishPresetConfig.Load(index) != baseline)
                throw new InvalidDataException("Coded warmup preset differs from normal Desert Base binding: " + id);
            var source = SkirmishLegacyProductionIdentity.ResolveSource("S001", out _);
            if (source == null || source.OperationMapId != definition.OperationMapId)
                throw new InvalidDataException("Coded warmup source differs from actual normal map: " + id);
            return source;
        }

        public static void RunProductionBatchCanariesFromEnvironment()
        {
            BeginProductionBatchWithCompanionWarmup(false, false);
        }

        // Focused only: real warmup and real final-exit owner, never dispatch a mission.
        public static void RunProductionCompanionWarmupShutdownControl()
        {
            RequireOwnedWarmupShutdownControl();
            SkirmishProductionBatchOwnershipCensus.RunPackageCompanionShutdownControls();
            BeginProductionBatchWithCompanionWarmup(true, false);
        }
        public static void RunProductionCompanionWarmupShutdownExpectedFailureControl()
        {
            RequireOwnedWarmupShutdownControl();
            SkirmishProductionBatchOwnershipCensus.RunPackageCompanionShutdownControls();
            BeginProductionBatchWithCompanionWarmup(true, true);
        }
        private static void RequireOwnedWarmupShutdownControl()
        {
            if (ExistingEditorValidation.IsRunning || Array.IndexOf(Environment.GetCommandLineArgs(), "-executeMethod") < 0)
                throw new InvalidOperationException("Focused warmup shutdown requires its checked executeMethod-owned Editor");
        }
        private static void BeginProductionBatchWithCompanionWarmup(bool shutdownControl, bool expectedFailure)
        {
            if(productionBatch!=null||productionCanary!=null||SessionState.GetBool(Key,false)||
                SessionState.GetBool(QuitPendingKey,false)||!string.IsNullOrEmpty(SessionState.GetString(ProductionBatchOwnerKey,""))||
                EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||
                (EditorSettings.enterPlayModeOptions&EnterPlayModeOptions.DisableSceneReload)!=0)
                throw new InvalidOperationException("Batch requires an idle unowned Editor with native scene reload enabled.");
            string path=Path.GetFullPath(Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_BATCH"));
            var validateRequests=PreflightProductionBatchInputRequests(path);
            BeginProductionNativeInputReadiness(inputReport=>
            {
                void ValidateRequests()
                {
                    if(Path.GetFullPath(Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_BATCH"))!=path)
                        throw new InvalidDataException("Batch owner environment changed during preparation.");
                    validateRequests();
                }
                ValidateRequests();
                var scenePreparation=PrepareProductionRestorableSceneBaseline();
                var originalScenes=ProductionSceneSetupFacts(EditorSceneManager.GetSceneManagerSetup());
                var before=SkirmishProductionBatchOwnershipCensus.Capture();
                SkirmishProductionBatchOwnershipCensus.CleanupSignature(before);
                if(((JArray)before["worlds"]).Count!=0)
                    throw new InvalidOperationException("Preserve existing Worlds; companion preparation requires idle World-free Editor.");
                var descriptor=SkirmishProductionCanaryRequest.ReadObject(path);
                string rawOutput=(string)descriptor["artifact_directory"];
                string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
                if(string.IsNullOrWhiteSpace(rawOutput)||!Path.IsPathRooted(rawOutput)||
                    rawOutput.Replace('\\','/').Split('/').Any(p=>p==".."))
                    throw new InvalidDataException("Ordinary absolute owned batch output required.");
                string output=Path.GetFullPath(rawOutput);
                if(!output.StartsWith(Path.Combine(root,"Logs","SkirmishProduction")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||
                    !Directory.Exists(output)||Path.GetFileName(output)!=(string)descriptor["batch_id"])
                    throw new InvalidDataException("Existing exact batch evidence directory required.");
                for(var cursor=new DirectoryInfo(output);cursor!=null;cursor=cursor.Parent)
                    if(cursor.Exists&&(cursor.Attributes&FileAttributes.ReparsePoint)!=0)
                        throw new InvalidDataException("Ordinary batch evidence ancestry required.");
                string profile=Path.Combine(output,"companion-prebaseline.isolated-profile");
                if(Directory.Exists(profile)||File.Exists(profile))
                    throw new InvalidDataException("Fresh owned warmup profile required; interrupted batches cannot resume.");
                var originalOptions=EditorSettings.enterPlayModeOptions;
                bool originalOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;
                bool originalBackground=Application.runInBackground;
                string originalSaveRoot=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
                string originalLocale=GameLocalization.CurrentLocaleCode;
                var facts=new JObject{["schema_version"]=1,["input_readiness"]=inputReport,
                    ["restorable_scene_preparation"]=scenePreparation,["before_census"]=before,
                    ["original_context"]=new JObject{["options"]=(int)originalOptions,["optionsEnabled"]=originalOptionsEnabled,
                        ["background"]=originalBackground,["saveRoot"]=originalSaveRoot,["locale"]=originalLocale,["scenes"]=originalScenes.DeepClone()},
                    ["plays"]=0,["mission_dispatched"]=false,["context_restored"]=false,
                    ["strict_cleanup_projection_weakened"]=false,["stages"]=new JArray()};
                var runtimeErrors=new ProductionRuntimeErrorGate();
                EditorApplication.CallbackFunction tick=null;
                Action quitting=null;
                double deadline=EditorApplication.timeSinceStartup+120d;
                int phase=0;
                bool detached=false;
                Exception failure=null;
                void Record(string stage)
                {
                    var census=SkirmishProductionBatchOwnershipCensus.Capture();
                    ((JArray)facts["stages"]).Add(new JObject{["stage"]=stage,["frame"]=Time.frameCount,["census"]=census});
                    UnityEngine.Debug.Log("[ProductionCompanionWarmup] stage="+stage+" "+facts.ToString(Formatting.None));
                    SkirmishProductionBatchOwnershipCensus.CleanupSignature(census);
                }
                void Detach(bool releaseOwnership)
                {
                    if(detached)return;detached=true;
                    EditorApplication.update-=tick;EditorApplication.quitting-=quitting;
                    Application.logMessageReceivedThreaded-=runtimeErrors.Observe;
                    if(releaseOwnership){productionInputPreparationPending=false;SessionState.SetBool(ProductionInputPreparationKey,false);}
                }
                List<string> RestoreSettings()
                {
                    var errors=new List<string>();
                    void Restore(string name,Action action){try{action();}catch(Exception error){errors.Add(name+": "+error);}}
                    Restore("options",()=>EditorSettings.enterPlayModeOptions=originalOptions);
                    Restore("optionsEnabled",()=>EditorSettings.enterPlayModeOptionsEnabled=originalOptionsEnabled);
                    Restore("background",()=>Application.runInBackground=originalBackground);
                    Restore("saveRoot",()=>Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",originalSaveRoot));
                    Restore("locale",()=>{GameLocalization.SetLocale(originalLocale,false);});
                    try{facts["restoration_observed"]=new JObject{
                        ["options"]=(int)EditorSettings.enterPlayModeOptions,["optionsEnabled"]=EditorSettings.enterPlayModeOptionsEnabled,
                        ["background"]=Application.runInBackground,["saveRoot"]=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"),
                        ["locale"]=GameLocalization.CurrentLocaleCode,["scenes"]=ProductionSceneSetupFacts(EditorSceneManager.GetSceneManagerSetup())};}
                    catch(Exception error){errors.Add("restoration witness: "+error);}
                    facts["restoration_errors"]=new JArray(errors);
                    return errors;
                }
                void Finish()
                {
                    try
                    {
                    var errors=RestoreSettings();
                    bool settingsEqual=EditorSettings.enterPlayModeOptions==originalOptions&&
                        EditorSettings.enterPlayModeOptionsEnabled==originalOptionsEnabled&&Application.runInBackground==originalBackground&&
                        Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT")==originalSaveRoot&&GameLocalization.CurrentLocaleCode==originalLocale;
                    bool scenesEqual=JToken.DeepEquals(originalScenes,ProductionSceneSetupFacts(EditorSceneManager.GetSceneManagerSetup()));
                    facts["context_restored"]=errors.Count==0&&settingsEqual&&scenesEqual&&!EditorApplication.isPlayingOrWillChangePlaymode;
                    UnityEngine.Debug.Log("[ProductionCompanionWarmup] stage=RestorationObserved "+facts.ToString(Formatting.None));
                        if(!(bool)facts["context_restored"])throw new InvalidOperationException("Owned companion preparation context did not restore exactly.");
                        RequireProductionRestorableSetup(EditorSceneManager.GetSceneManagerSetup());
                        var after=SkirmishProductionBatchOwnershipCensus.Capture();
                        facts["after_census"]=after;
                        UnityEngine.Debug.Log("[ProductionCompanionWarmup] stage=RestorationCensusObserved "+facts.ToString(Formatting.None));
                        SkirmishProductionBatchOwnershipCensus.CleanupSignature(after);
                        if(((JArray)after["worlds"]).Count!=0)throw new InvalidOperationException("Companion preparation left live Worlds.");
                        if(failure!=null)throw failure;
                        runtimeErrors.EnsureClean();ValidateRequests();
                        Detach(true);
                        // Play may rebuild native devices. Reuse the existing real two-update
                        // readiness window after restoration; never infer it from the census.
                        BeginProductionNativeInputReadiness(postPlayInputReport=>
                        {
                            ValidateRequests();
                            if(productionBatch!=null||productionCanary!=null||SessionState.GetBool(Key,false)||
                                SessionState.GetBool(QuitPendingKey,false)||!string.IsNullOrEmpty(SessionState.GetString(ProductionBatchOwnerKey,""))||
                                productionInputPreparationPending||SessionState.GetBool(ProductionInputPreparationKey,false)||
                                EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)
                                throw new InvalidOperationException("Post-Play input handoff lost idle batch ownership.");
                            RequireProductionRestorableSetup(EditorSceneManager.GetSceneManagerSetup());
                            if(EditorSettings.enterPlayModeOptions!=originalOptions||
                                EditorSettings.enterPlayModeOptionsEnabled!=originalOptionsEnabled||
                                Application.runInBackground!=originalBackground||
                                Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT")!=originalSaveRoot||
                                GameLocalization.CurrentLocaleCode!=originalLocale||
                                !JToken.DeepEquals(originalScenes,ProductionSceneSetupFacts(EditorSceneManager.GetSceneManagerSetup())))
                                throw new InvalidOperationException("Restored context changed during post-Play input readiness.");
                            var handoff=SkirmishProductionBatchOwnershipCensus.Capture();
                            facts["post_input_handoff_census"]=handoff;
                            facts["post_play_input_readiness"]=postPlayInputReport;
                            UnityEngine.Debug.Log("[ProductionCompanionWarmup] stage=PostInputHandoffObserved "+facts.ToString(Formatting.None));
                            SkirmishProductionBatchOwnershipCensus.CleanupSignature(handoff);
                            if(((JArray)handoff["worlds"]).Count!=0)
                                throw new InvalidOperationException("Post-Play input handoff left live Worlds.");
                            UnityEngine.Debug.Log("[ProductionCompanionWarmup] result=Prepared plays=1 missionDispatched=0 contextRestored=1 "+facts.ToString(Formatting.None));
                            var context=new ProductionBatchContext(path);
                            productionBatch=context;
                            SessionState.SetString(ProductionBatchOwnerKey,context.OutputDirectory);
                            if (shutdownControl) context.CompleteWarmupShutdownControl(facts, expectedFailure);
                            else context.BeginNativeScenePreparation();
                        },error=>
                        {
                            var restoreErrors=RestoreSettings();
                            try{RequireProductionRestorableSetup(EditorSceneManager.GetSceneManagerSetup());}
                            catch(Exception restoreError){restoreErrors.Add("restorable scene witness: "+restoreError);}
                            facts["restoration_errors"]=new JArray(restoreErrors);
                            facts["context_restored"]=restoreErrors.Count==0&&!EditorApplication.isPlayingOrWillChangePlaymode&&
                                JToken.DeepEquals(facts["original_context"],facts["restoration_observed"]);
                            facts["failure"]="PostPlayNativeInputReadiness: "+error;
                            UnityEngine.Debug.Log("[ProductionCompanionWarmup] result=Failed missionDispatched=0 "+facts.ToString(Formatting.None));
                            FailProductionNativeInputReadiness(error);
                        });
                    }
                    catch(Exception error)
                    {
                        RestoreSettings();
                        facts["failure"]=error.ToString();Detach(!EditorApplication.isPlayingOrWillChangePlaymode);
                        UnityEngine.Debug.Log("[ProductionCompanionWarmup] result=Failed missionDispatched=0 "+facts.ToString(Formatting.None));
                        FailProductionNativeInputReadiness(error);
                    }
                }
                void Fail(Exception error)
                {
                    if(failure==null)failure=error;
                    facts["failure"]=failure.ToString();
                    UnityEngine.Debug.Log("[ProductionCompanionWarmup] stage=FailureObserved "+facts.ToString(Formatting.None));
                    if(EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        if(phase!=2){phase=2;deadline=EditorApplication.timeSinceStartup+30d;EditorApplication.ExitPlaymode();}
                    }
                    else Finish();
                }
                quitting=()=>
                {
                    RestoreSettings();Detach(false);
                    UnityEngine.Debug.Log("[ProductionCompanionWarmup] result=Abandoned missionDispatched=0 contextRestored=0 "+facts.ToString(Formatting.None));
                };
                tick=()=>
                {
                    try
                    {
                        if(phase==0)return; // Async helper owns its bounded120s pending-load failure path.
                        if(EditorApplication.timeSinceStartup>=deadline)
                        {
                            if(phase==2&&EditorApplication.isPlayingOrWillChangePlaymode)
                            {
                                facts["failure"]="Owned Play exit exceeded30s; no baseline or mission dispatch.";
                                RestoreSettings();Detach(false);
                                UnityEngine.Debug.Log("[ProductionCompanionWarmup] result=Failed missionDispatched=0 contextRestored=0 "+facts.ToString(Formatting.None));
                                EditorApplication.Exit(2);return;
                            }
                            throw new TimeoutException("Owned companion Play preparation exceeded120s.");
                        }
                        if(productionBatch!=null||productionCanary!=null||SessionState.GetBool(Key,false)||
                            SessionState.GetBool(QuitPendingKey,false)||!productionInputPreparationPending||
                            !SessionState.GetBool(ProductionInputPreparationKey,false))
                            throw new InvalidOperationException("Companion preparation lost exclusive pre-dispatch ownership.");
                        if(phase==2)
                        {
                            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
                            if(((JArray)SkirmishProductionBatchOwnershipCensus.Capture()["worlds"]).Count!=0)return;
                            Record("AfterExitPlay");facts["plays"]=1;Finish();return;
                        }
                        runtimeErrors.EnsureClean();
                        if(phase==0)return; // Exact async streaming callback, not an early Play update, owns entry.
                        if(!EditorApplication.isPlaying||Time.frameCount<1)return;
                        Record("PreparedInPlay");
                        phase=2;deadline=EditorApplication.timeSinceStartup+30d;
                        EditorApplication.ExitPlaymode();
                    }
                    catch(Exception error){Fail(error);}
                };
                productionInputPreparationPending=true;SessionState.SetBool(ProductionInputPreparationKey,true);
                EditorApplication.quitting+=quitting;EditorApplication.update+=tick;
                Application.logMessageReceivedThreaded+=runtimeErrors.Observe;
                try
                {
                    Record("BeforeStreamingWarmup");
                    ProductionBatchContext.BeginNativeSceneBaseline(((JArray)descriptor["jobs"]).OfType<JObject>()
                        .Select(v=>SkirmishProductionCanaryRequest.ReadObject((string)v["job_path"])),preparation=>
                    {
                        try
                        {
                            ValidateRequests();
                            if(detached||failure!=null||EditorApplication.timeSinceStartup>=deadline||
                                productionBatch!=null||productionCanary!=null||SessionState.GetBool(Key,false)||
                                SessionState.GetBool(QuitPendingKey,false)||!productionInputPreparationPending||
                                !SessionState.GetBool(ProductionInputPreparationKey,false))
                                throw new InvalidOperationException("Async preparation lost its existing warmup deadline/owner.");
                            facts["async_scene_preparation"]=preparation;
                            Record("AfterStreamingWarmup");
                            EditorSettings.enterPlayModeOptionsEnabled=true;
                            EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
                            Application.runInBackground=true;
                            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",profile);
                            phase=1;
                            EditorApplication.EnterPlaymode();
                        }
                        catch(Exception error){Fail(error);}
                    },Fail);
                }
                catch(Exception error){Fail(error);}
            },FailProductionNativeInputReadiness);
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
            private SkirmishProductionBatchContract contract;
            private bool nativePreparationStarted,nativePreparationCompleted;
            private readonly Stopwatch wall=Stopwatch.StartNew();
            private readonly DateTimeOffset enteredAt=DateTimeOffset.UtcNow;
            private readonly string events;
            private SkirmishProductionCanaryRequest current;
            private int next,completed;
            private double cleanupDeadline;
            private double jobDeadline;
            private bool stopped;
            private string baselineOwnership;
            private bool finalExitRequested;
            private int exitInvocations;
            private readonly ProductionRuntimeErrorGate runtimeErrors=new ProductionRuntimeErrorGate();
            private bool runtimeObservationArmed;
            private volatile bool quitGateEmitted;
            private int finalRequestedCode=2;
            private bool warmupShutdownControl;
            private bool warmupShutdownExpectedFailure;
            private bool warmupShutdownCleanupVerified;
            private JObject warmupShutdownBefore;
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
                if (warmupShutdownControl)
                {
                    bool observed=RuntimeErrorsClean&&stopped&&warmupShutdownCleanupVerified&&finalExitRequested&&
                        finalRequestedCode==(warmupShutdownExpectedFailure?2:0)&&exitInvocations==1;
                    Write("WarmupShutdownControlQuittingGate",new JObject { ["observed_expected_lifecycle"]=observed,
                        ["expected_negative"]=warmupShutdownExpectedFailure,["passed"]=observed&&!warmupShutdownExpectedFailure,
                        ["errors"]=runtimeErrors.Count,["mission_dispatched"]=false,["mission_credit"]=0 });
                    UnityEngine.Debug.Log("[ProductionWarmupShutdownControlQuittingGate] result="+
                        (observed&&!warmupShutdownExpectedFailure?"Passed":"Failed")+" expectedNegative="+warmupShutdownExpectedFailure);
                    return;
                }
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
                var restorableBaseline=PrepareProductionRestorableSceneBaseline();
                // One atomic start sentinel. A crashed or partial batch is never resumed
                // by overwriting it; original evidence remains available for a new claim.
                using(var output=new FileStream(Path.Combine(OutputDirectory,"batch-started.json"),FileMode.CreateNew,FileAccess.Write))
                using(var writer=new StreamWriter(output,new UTF8Encoding(false)))writer.Write(descriptor.ToString(Formatting.Indented));
                // The immutable content-addressed candidate store owns full rows
                // once. Every job records this exact file/hash reference.
                manifestPath=sourceManifest;
                events=Path.Combine(OutputDirectory,"batch-events.jsonl");
                using(File.Open(events,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){}
                Write("RestorableSceneBaselinePrepared",restorableBaseline);
                Write("BeforeNativeScenePreparation",new JObject {
                    ["census"]=SkirmishProductionBatchOwnershipCensus.Capture(),
                    ["entry_process_age_seconds"]=(enteredAt.UtcDateTime-Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds });
                // Same-process native streaming preparation establishes package-owned
                // lazy companion scenes BEFORE the strict baseline. No gameplay
                // systems/input/default World or unrelated preview scene are touched.
                foreach(string key in new[]{"WARLINE_SKIRMISH_PRODUCTION_JOB","WARLINE_SKIRMISH_PRODUCTION_CLAIM","WARLINE_ARIA_QUIT"})
                    previousEnvironment[key]=Environment.GetEnvironmentVariable(key);
            }
            internal void CompleteWarmupShutdownControl(JObject facts, bool expectedFailure)
            {
                if (!ReferenceEquals(productionBatch,this)||stopped||nativePreparationStarted||nativePreparationCompleted||
                    (int?)facts["plays"]!=1||(bool?)facts["context_restored"]!=true||
                    EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||
                    Worlds().Length!=0||SessionState.GetString(ProductionBatchOwnerKey,"")!=OutputDirectory)
                    throw new InvalidOperationException("Actual restored one-Play World-free warmup owner required");
                var before=(JObject)facts["before_census"];
                if (((JArray)before["companion_package_owner_fields"]).OfType<JObject>().Any(f=>(bool?)f["valid"]==true)||
                    ((JArray)SkirmishProductionBatchOwnershipCensus.Capture()["companion_package_owner_fields"]).OfType<JObject>()
                        .Count(f=>(bool?)f["valid"]==true)!=2)
                    throw new InvalidOperationException("Real warmup must create both exact package owner scenes after the five owner controls");
                warmupShutdownBefore=(JObject)before.DeepClone();
                warmupShutdownControl=true;warmupShutdownExpectedFailure=expectedFailure;
                ArmRuntimeObservation();
                Write("WarmupShutdownControlRestored",new JObject { ["warmup"]=facts.DeepClone(),
                    ["expected_negative"]=expectedFailure,["mission_dispatched"]=false,["completed_jobs"]=0 });
                stopped=true;SessionState.EraseString(ProductionBatchOwnerKey);productionBatch=null;
                QueueFinalExit(expectedFailure?2:0);
            }
            internal void BeginNativeScenePreparation()
            {
                if(nativePreparationStarted||nativePreparationCompleted||stopped)
                    throw new InvalidOperationException("Native scene preparation is exactly once before dispatch.");
                nativePreparationStarted=true;ArmRuntimeObservation();
                var validatePreparedRequests=PreflightProductionBatchInputRequests(descriptorPath);
                var preparationTimer=Stopwatch.StartNew();
                BeginNativeSceneBaseline(jobs.Select(v=>SkirmishProductionCanaryRequest.ReadObject(Text((JObject)v,"job_path"))),preparation=>
                {
                    try
                    {
                        runtimeErrors.EnsureClean();
                        if(!ReferenceEquals(productionBatch,this)||stopped||
                            SkirmishProductionCanaryRequest.FileHash(descriptorPath)!=descriptorHash)
                            throw new InvalidOperationException("Async preparation lost its exact undispatched batch owner.");
                        validatePreparedRequests();
                        Write("NativeInputBaselinePrepared",PrepareProductionNativeInputBaseline());
                        RequireProductionRestorableSetup(EditorSceneManager.GetSceneManagerSetup());
                        var ownership=SkirmishProductionBatchOwnershipCensus.Capture();
                        baselineOwnership=SkirmishProductionBatchOwnershipCensus.CleanupSignature(ownership);
                        contract=new SkirmishProductionBatchContract((string)descriptor["candidate_sha256"],(string)descriptor["build_sha256"],
                            (string)descriptor["candidate_manifest_sha256"],Worlds(),Scenes(),EditorSceneManager.previewSceneCount);
                        preparationTimer.Stop();
                        Write("BatchCaptured",new JObject { ["baseline_worlds"]=new JArray(contract.BaselineWorlds),
                            ["baseline_scenes"]=contract.BaselineScenes,["baseline_preview_count"]=contract.BaselinePreviewCount,
                            ["baseline_ownership_census"]=ownership,["scene_preparation_milliseconds"]=preparationTimer.Elapsed.TotalMilliseconds,
                            ["async_scene_preparation"]=preparation });
                        nativePreparationCompleted=true;DispatchNext();
                    }
                    catch(Exception error){Fail(error);}
                },Fail);
            }
            internal void DispatchNext()
            {
                if(stopped)return;
                if(!nativePreparationCompleted||contract==null)
                    throw new InvalidOperationException("No native dispatch before exact async preparation completion.");
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
                    bool legacy=productionCanary?.Request.IsLegacyPreset==true;
                    if(legacy != ((string)facts["native_identity_kind"]=="LegacyPreset")) throw new InvalidDataException("Native identity kind differs from claim");
                    if(!legacy&&(string)facts["phase"]=="Finished"&&(string)facts["result_session_id"]!=(string)facts["native_session_id"])
                        throw new InvalidDataException("Frozen native result session differs.");
                    if(!legacy&&(string)facts["phase"]=="Finished"&&(string)facts["result_outcome"]!=(string)facts["outcome"])
                        throw new InvalidDataException("Frozen native result outcome differs.");
                    contract.NativeSnapshot((string)facts["native_world_sequence"],(string)facts["native_session_id"],(string)facts["phase"],
                        (string)facts["outcome"],legacy ? facts["legacy_match_frozen"]?.Type==JTokenType.Boolean&&(bool)facts["legacy_match_frozen"] : facts["result_frozen"]?.Type==JTokenType.Integer&&(int)facts["result_frozen"]!=0);
                }
                if(kind=="TerminalReturnOwnerCompleted")
                {
                    if(productionCanary?.Request.IsLegacyPreset==true && (facts["legacy_result_saved_verified"]?.Type!=JTokenType.Boolean||!(bool)facts["legacy_result_saved_verified"])) throw new InvalidDataException("Actual legacy saved result proof missing");
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
                    DispatchNext();
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
                if(stopped)return;stopped=true;contract?.Fail(error.ToString());EditorApplication.update-=WaitForCleanup;
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
                // RequestFinalExit schedules the existing persistent quit/update owner;
                // it does not invoke EditorApplication.Exit inside this callback.
                RequestFinalExit(code);
            }
            private void RequestFinalExit(int code)
            {
                try
                {
                    var cleanup=SkirmishProductionBatchOwnershipCensus.CloseEmptyPackageCompanionScenesAtFinalExit();
                    Write("FinalPackageCompanionCleanup", cleanup);
                    if (warmupShutdownControl)
                    {
                        var after=SkirmishProductionBatchOwnershipCensus.Capture();
                        JArray Previews(JObject census)=>new JArray(((JArray)census["scenes"]).OfType<JObject>()
                            .Where(s=>(bool?)s["preview"]==true).OrderBy(s=>(string)s["handle"]).Select(s=>s.DeepClone()));
                        if ((int?)cleanup["closed_count"]!=2||(bool?)cleanup["foreign_scenes_unchanged"]!=true||
                            (bool?)cleanup["worlds_disposed"]!=true||Worlds().Length!=0||
                            !JToken.DeepEquals(Previews(warmupShutdownBefore),Previews(after)))
                            throw new InvalidOperationException("Focused real warmup cleanup must close exact owned two and preserve original foreign previews");
                        warmupShutdownCleanupVerified=true;
                        Write("WarmupShutdownControlCleanupVerified",new JObject { ["before"]=warmupShutdownBefore.DeepClone(),
                            ["after"]=after,["cleanup"]=cleanup.DeepClone(),["expected_negative"]=warmupShutdownExpectedFailure,
                            ["mission_dispatched"]=false,["persistent_allocations_qualified"]=false });
                    }
                }
                catch(Exception error)
                {
                    code=2;finalRequestedCode=2;
                    Write("FinalPackageCompanionCleanupFailed",new JObject{["error"]=error.ToString(),["persistent_allocation_cleanliness_claimed"]=false});
                    UnityEngine.Debug.LogError("[ProductionFinalCompanionCleanup] result=Failed error="+error);
                }
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
                if (warmupShutdownControl)
                {
                    bool observed=warmupShutdownCleanupVerified&&RuntimeErrorsClean&&code==(warmupShutdownExpectedFailure?2:0);
                    string result=observed?(warmupShutdownExpectedFailure?"ExpectedFailure":"Passed"):"Failed";
                    UnityEngine.Debug.Log("[ProductionWarmupShutdownControl] result="+result+
                        (observed?" plays=1 ownedClosed=2 foreignPreserved=1 worlds=0":" cleanupVerified="+warmupShutdownCleanupVerified)+
                        " actualExitInvocations="+exitInvocations+" requestedCode="+code+
                        " nativeExitPending=1 missionDispatched=0 missionCredit=0 persistentAllocationsQualified=0");
                }
            }

            private static bool nativeScenePreparationPending;
            // A failed pending operation is not disposed or called complete; Root-owned Editor shutdown owns it.
            private static World failedPendingNativeSceneWorld;
            internal static void BeginNativeSceneBaseline(IEnumerable<JObject> plannedJobs,Action<JObject> ready,Action<Exception> failed)
            {
                if(ready==null||failed==null||nativeScenePreparationPending||failedPendingNativeSceneWorld!=null||
                    EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)
                    throw new InvalidOperationException("Fresh idle owned async native scene preparation required.");
                var original=World.DefaultGameObjectInjectionWorld;
                var systems=DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming,true);
                foreach(var system in systems)
                    if(TypeManager.GetSystemName(system).ToString().StartsWith("Game.",StringComparison.Ordinal))
                        throw new InvalidOperationException("Gameplay system unexpectedly entered streaming-only batch preparation");
                var catalog=SkirmishExpansionCatalogConfig.Load();
                    var guids=new HashSet<string>(StringComparer.Ordinal);
                    foreach(var mode in plannedJobs==null?new[]{new JObject{["catalog_id"]="S004",["schema_version"]=1},new JObject{["catalog_id"]="S005",["schema_version"]=1}}:
                        plannedJobs.OfType<JObject>().GroupBy(v=>(string)v["catalog_id"]+":"+(string)v["schema_version"],StringComparer.Ordinal).Select(g=>g.First()).ToArray())
                    {
                        string id=(string)mode["catalog_id"];
                        if(SkirmishLegacyProductionIdentity.IsProtected(id))
                        {
                            if((int?)mode["schema_version"]==3)
                            {
                                SkirmishProtectedCounterpartAuthority.Validate(mode,Path.GetFullPath(Path.Combine(Application.dataPath,"..")));
                                var authored=SkirmishProtectedExpandedCounterpartScope.LoadDevelopmentCatalog(id);
                                if(!authored.TryGetDefinition(id,out var counterpart)||counterpart.CampaignMap==null)
                                    throw new InvalidDataException("Exact measured counterpart warmup source missing");
                                guids.Add(counterpart.CampaignMap.NavigationMetadata.AuthoredSubSceneGuid);
                            }
                            else guids.Add(SkirmishLegacyProductionIdentity.ResolveSource(id,out _).NavigationMetadata.AuthoredSubSceneGuid);
                            continue;
                        }
                        if(catalog==null||!catalog.TryGetDefinition(id,out var definition)||definition==null)
                            throw new InvalidDataException("Actual packaged canary source definition missing");
                        guids.Add(ResolveProductionBatchWarmupSource(definition).NavigationMetadata.AuthoredSubSceneGuid);
                    }
                    guids.Add(AssetDatabase.AssetPathToGUID("Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity"));
                var ordered=guids.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
                if(ordered.Length==0||ordered.Any(string.IsNullOrWhiteSpace))
                    throw new InvalidDataException("Exact nonempty source SubScene identities required.");
                World world=null;Entity scene=Entity.Null;string guid=null;int nextGuid=0,worldUpdates=0,totalUpdates=0;
                bool done=false;double start=EditorApplication.timeSinceStartup,last=start;
                var completed=new JArray();
                EditorApplication.CallbackFunction tick=null;Action quitting=null;
                nativeScenePreparationPending=true;
                void Detach(){EditorApplication.update-=tick;EditorApplication.quitting-=quitting;}
                void Fail(Exception error,bool notify)
                {
                    if(done)return;done=true;Detach();
                    try
                    {
                        if(world!=null&&world.IsCreated)
                        {
                            if(scene!=Entity.Null&&SceneSystem.IsSceneLoaded(world.Unmanaged,scene))world.Dispose();
                            else
                            {
                                failedPendingNativeSceneWorld=world;
                                error=new AggregateException(error,new InvalidOperationException("Incomplete owned streaming World retained until checked Editor shutdown; no retry/pass."));
                            }
                        }
                    }
                    catch(Exception disposeError){failedPendingNativeSceneWorld=world;error=new AggregateException(error,disposeError);}
                    nativeScenePreparationPending=failedPendingNativeSceneWorld!=null;
                    UnityEngine.Debug.Log("[ProductionAsyncScenePreparation] result=Failed completed="+completed.Count+" pendingWorldRetained="+(failedPendingNativeSceneWorld!=null));
                    if(notify)failed(error);
                }
                quitting=()=>Fail(new InvalidOperationException("Editor quit before async source preparation completed."),false);
                tick=()=>
                {
                    if(done)return;
                    try
                    {
                        double now=EditorApplication.timeSinceStartup;
                        if(double.IsNaN(now)||double.IsInfinity(now)||now<=last)
                            throw new InvalidOperationException("Distinct increasing genuine Editor update required.");
                        last=now;
                        if(now-start>=120d)throw new TimeoutException("Owned async source preparation exceeded120s; mission not dispatched.");
                        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||
                            World.DefaultGameObjectInjectionWorld!=original)
                            throw new InvalidOperationException("Async preparation lost idle/default World ownership.");
                        if(world==null)
                        {
                            guid=ordered[nextGuid];worldUpdates=0;
                            world=new World("ProductionBatchNativeScenePreparation",WorldFlags.Game);
                            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world,systems);
                            scene=SceneSystem.LoadSceneAsync(world.Unmanaged,new Unity.Entities.Hash128(guid),new SceneSystem.LoadParameters {
                                Flags=SceneLoadFlags.BlockOnImport }); // No BlockOnStreamIn or WaitForCompletion fallback.
                        }
                        world.Update();worldUpdates++;totalUpdates++;
                        if(!world.EntityManager.Exists(scene)||!world.EntityManager.HasComponent<SceneReference>(scene)||
                            world.EntityManager.GetComponentData<SceneReference>(scene).SceneGUID!=new Unity.Entities.Hash128(guid))
                            throw new InvalidOperationException("Retained exact requested scene entity/GUID changed.");
                        if(!SceneSystem.IsSceneLoaded(world.Unmanaged,scene))return;
                        var row=new JObject{["guid"]=guid,["world_sequence"]=world.SequenceNumber.ToString(),["entity_index"]=scene.Index,
                            ["entity_version"]=scene.Version,["loaded_before_dispose"]=true,["genuine_editor_updates"]=worldUpdates};
                        world.Dispose();world=null;scene=Entity.Null;
                        if(World.DefaultGameObjectInjectionWorld!=original)
                            throw new InvalidOperationException("Streaming preparation changed default World ownership.");
                        row["world_disposed"]=true;completed.Add(row);nextGuid++;
                        if(nextGuid<ordered.Length)return; // Next exact GUID starts on a later real Editor update.
                        done=true;Detach();nativeScenePreparationPending=false;
                        var report=new JObject{["requested_guids"]=new JArray(ordered),["completed"]=completed,
                            ["genuine_editor_updates"]=totalUpdates,["elapsed_seconds"]=now-start,["timeout_seconds"]=120,
                            ["default_world_unchanged"]=true,["blocking_stream_in"]=false,["mission_dispatched"]=false};
                        UnityEngine.Debug.Log("[ProductionAsyncScenePreparation] result=Ready "+report.ToString(Formatting.None));
                        try{ready(report);}catch(Exception callbackError){failed(callbackError);}
                    }
                    catch(Exception error){Fail(error,true);}
                };
                EditorApplication.quitting+=quitting;EditorApplication.update+=tick;
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
