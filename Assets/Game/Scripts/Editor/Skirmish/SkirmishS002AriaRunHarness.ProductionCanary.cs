// DRAFT ONLY: outside Assets; requires the narrow harness seams in README.md.
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Configs;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Transforms;
using Game.UI.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Entities;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {

        private const string ProductionBaselineMenuPath="Assets/Game/Scenes/Menu.unity";
        private static JArray ProductionSceneSetupFacts(SceneSetup[] setup)
            =>new JArray(setup.Select(s=>new JObject{["path"]=s.path,["loaded"]=s.isLoaded,["active"]=s.isActive}));
        private static void RequireProductionRestorableSetup(SceneSetup[] setup)
        {
            if(setup==null||setup.Length==0||setup.Count(s=>s.isActive&&s.isLoaded)!=1||
                setup.Any(s=>string.IsNullOrWhiteSpace(s.path)||AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)==null)||
                setup.Select(s=>s.path).Distinct(StringComparer.Ordinal).Count()!=setup.Length)
                throw new InvalidOperationException("Actual nonempty path-backed active-loaded scene baseline required before reservation");
            var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(!active.IsValid()||!active.isLoaded||active.path!=setup.Single(s=>s.isActive).path)
                throw new InvalidOperationException("Actual active scene differs from captured restorable baseline");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            {
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if(scene.isDirty)throw new InvalidOperationException("Preserve dirty Editor scene");
                if(!EditorSceneManager.IsPreviewScene(scene)&&!setup.Any(s=>s.isLoaded&&s.path==scene.path))
                    throw new InvalidOperationException("Unrepresented Editor scene cannot be discarded");
            }
        }
        private static JObject PrepareProductionRestorableSceneBaseline()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)
                throw new InvalidOperationException("Idle Editor required for actual scene baseline");
            if(SessionState.GetBool("Warline.MissionValidation.ResumeRefresh",false))
                throw new InvalidOperationException("Another validation owns refresh suspension before scene capture");
            var before=EditorSceneManager.GetSceneManagerSetup();
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Preserve dirty Editor scene before baseline capture");
            string transition="PreservedExisting";
            int emptyBootstrapScenes=0;
            if(before.Length==0)
            {
                if(EditorSceneManager.previewSceneCount!=0)
                    throw new InvalidOperationException("Unowned preview scenes prevent empty-scene baseline establishment");
                for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                {
                    var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if(!scene.IsValid()||!string.IsNullOrEmpty(scene.path)||scene.rootCount!=0)
                        throw new InvalidOperationException("Unowned/content-bearing scene prevents Menu baseline establishment");
                    emptyBootstrapScenes++;
                }
                if(emptyBootstrapScenes>1||AssetDatabase.LoadAssetAtPath<SceneAsset>(ProductionBaselineMenuPath)==null)
                    throw new InvalidOperationException("Existing Menu asset and at most one clean rootless bootstrap scene required");
                Debug.Log("[ProductionRestorableSceneBaseline] transition=OpeningExistingMenu beforeSetupCount=0 emptyBootstrapScenes="+emptyBootstrapScenes);
                EditorSceneManager.OpenScene(ProductionBaselineMenuPath,OpenSceneMode.Single);
                transition="OpenedExistingMenu";
            }
            var after=EditorSceneManager.GetSceneManagerSetup();
            RequireProductionRestorableSetup(after);
            if(transition=="PreservedExisting"&&!JToken.DeepEquals(ProductionSceneSetupFacts(before),ProductionSceneSetupFacts(after)))
                throw new InvalidOperationException("Nonempty existing restorable scene setup changed");
            if(transition=="OpenedExistingMenu"&&(after.Length!=1||after[0].path!=ProductionBaselineMenuPath))
                throw new InvalidOperationException("Fresh Menu baseline was not captured exactly");
            var facts=new JObject{["transition"]=transition,["before_setup"]=ProductionSceneSetupFacts(before),
                ["after_setup"]=ProductionSceneSetupFacts(after),["empty_bootstrap_scenes"]=emptyBootstrapScenes,
                ["utc_before_context_capture"]=DateTimeOffset.UtcNow.ToString("O"),
                ["gameplay_dispatched"]=false,["job_reserved"]=false};
            Debug.Log("[ProductionRestorableSceneBaseline] result=Prepared transition="+transition+
                " sceneCount="+after.Length+" beforeContextCapture=1");
            return facts;
        }
        public static void RunProductionRestorableSceneBaselineControls()
        {
            if(productionCanary!=null||productionBatch!=null||SessionState.GetBool(Key,false)||
                EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||
                EditorSceneManager.GetSceneManagerSetup().Length!=0)
                throw new InvalidOperationException("Native empty-setup baseline control requires fresh idle unowned Editor; do not fake empty input");
            int controls=0;
            var first=PrepareProductionRestorableSceneBaseline();
            if((string)first["transition"]!="OpenedExistingMenu")throw new InvalidOperationException("Actual empty-to-Menu transition missing");
            var baseline=EditorSceneManager.GetSceneManagerSetup();RequireProductionRestorableSetup(baseline);controls++;
            var repeated=PrepareProductionRestorableSceneBaseline();
            if((string)repeated["transition"]!="PreservedExisting"||
                !JToken.DeepEquals(ProductionSceneSetupFacts(baseline),ProductionSceneSetupFacts(EditorSceneManager.GetSceneManagerSetup())))
                throw new InvalidOperationException("Existing scene baseline was not a true no-op");
            controls++;
            UnityEngine.SceneManagement.Scene owned=default;
            try
            {
                owned=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                if(!owned.IsValid()||owned.rootCount!=0||owned.isDirty)throw new InvalidOperationException("Fresh owned empty fixture required");
                EditorSceneManager.RestoreSceneManagerSetup(baseline);
                owned=default;
                RequireProductionRestorableSetup(EditorSceneManager.GetSceneManagerSetup());
                if(!JToken.DeepEquals(ProductionSceneSetupFacts(baseline),ProductionSceneSetupFacts(EditorSceneManager.GetSceneManagerSetup())))
                    throw new InvalidOperationException("Real Menu restore differs from captured setup");
                controls++;
                owned=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                EditorSceneManager.MarkSceneDirty(owned);
                bool refused=false;
                try{PrepareProductionRestorableSceneBaseline();}catch(InvalidOperationException){refused=true;}
                if(!refused||!owned.IsValid()||!owned.isDirty)
                    throw new InvalidOperationException("Actual dirty owned scene must be preserved/refused");
                controls++;
            }
            finally
            {
                // Only this exact native fixture scene may be discarded.
                if(owned.IsValid()&&!EditorSceneManager.CloseScene(owned,true))
                    throw new InvalidOperationException("Owned temporary baseline fixture cleanup failed");
                EditorSceneManager.RestoreSceneManagerSetup(baseline);
                RequireProductionRestorableSetup(EditorSceneManager.GetSceneManagerSetup());
            }
            Debug.Log("[ProductionRestorableSceneBaselineControls] result=Passed controls="+controls+
                " actualEmptyToMenu=1 actualRestore=1 dirtyRefusal=1 jobReservations=0 gameplayCertified=0");
        }

        private static ProductionCanaryContext productionCanary;
        private static IDisposable productionCounterpartScope;

        public static void RunProductionJobFromEnvironment() => RunOwnedProductionEntry(false);
        public static void RunProductionPaidPlaneSupplementFromEnvironment() => RunOwnedProductionEntry(true);
        private static void RequireProductionPaidPlaneSupplement(JObject job, string artifacts,
            bool inBatch, string output, string inputDiagnostic, string padDiagnostic)
        {
            string expectedOutput = Path.Combine(artifacts, "paid-plane.jsonl");
            if (inBatch || (int)job["schema_version"] != 1 || (string)job["catalog_id"] != "S005" ||
                (int)job["seed"] != 155926 || (string)job["control_mode"] != "manual" || (string)job["locale"] != "EN" ||
                (string)job["size"] != "Standard" || (string)job["difficulty"] != "Regular" ||
                inputDiagnostic == "1" || padDiagnostic == "1" || string.IsNullOrWhiteSpace(output) ||
                !Path.IsPathRooted(output) || !string.Equals(Path.GetFullPath(output), expectedOutput, StringComparison.Ordinal))
                throw new InvalidDataException("Exact owned manual S005 EN155926 paid-plane supplemental request/output required.");
        }
        private static void RunOwnedProductionEntry(bool paidPlaneSupplement)
        {
            if(productionBatch!=null){RunOwnedProductionEntryReady(paidPlaneSupplement);return;}
            string job=Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_JOB");
            string claim=Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_CLAIM");
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            var request=SkirmishProductionCanaryRequest.Read(root,job,claim,true,null);
            string rawJobHash=SkirmishProductionCanaryRequest.FileHash(request.JobPath);
            string claimHash=SkirmishProductionCanaryRequest.FileHash(request.ClaimPath);
            BeginProductionNativeInputReadiness(report=>
            {
                if(Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_JOB")!=job||
                    Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_CLAIM")!=claim||
                    SkirmishProductionCanaryRequest.FileHash(request.JobPath)!=rawJobHash||
                    SkirmishProductionCanaryRequest.FileHash(request.ClaimPath)!=claimHash)
                    throw new InvalidDataException("Actual owned request changed during input initialization.");
                RunOwnedProductionEntryReady(paidPlaneSupplement);
            },FailProductionNativeInputReadiness);
        }
        private static void RunOwnedProductionEntryReady(bool paidPlaneSupplement)
        {
            if (productionCanary != null || SessionState.GetBool(Key, false) ||
                EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Production canary requires an idle unowned Editor.");
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var request = SkirmishProductionCanaryRequest.Read(root,
                Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_JOB"),
                Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_CLAIM"),productionBatch==null,productionBatch?.Matrix);
            if (paidPlaneSupplement) RequireProductionPaidPlaneSupplement(request.Job, request.ArtifactDirectory,
                productionBatch != null, Environment.GetEnvironmentVariable("WARLINE_S005_PLANE_CANARY_OUTPUT"),
                Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_INPUT_DIAGNOSTIC"),
                Environment.GetEnvironmentVariable("WARLINE_HOME_PAD_BOUNDARY_DIAGNOSTIC"));
            var packaged = !request.IsLegacyPreset&&SkirmishMissionDispatch.IsExpandedCounterpart(request.CatalogId)
                ? SkirmishProtectedExpandedCounterpartScope.LoadDevelopmentCatalog(request.CatalogId) : SkirmishExpansionCatalogConfig.Load();
            SkirmishScenarioDefinitionConfig definition;
            if (request.IsLegacyPreset) SkirmishLegacyProductionIdentity.RequireBinding(request.CatalogId);
            else if (packaged == null || !packaged.TryGetDefinition(request.CatalogId, out definition) ||
                definition.DefinitionId != (string)request.Job["definition_id"] ||
                definition.ContentVersion != (int)request.Job["definition_version"])
                throw new InvalidDataException("Actual packaged canary definition/version differs.");
            if(productionBatch==null) request.VerifyNativePins(() => SkirmishCandidateSourceHash.Compute(root));
            var context = new ProductionCanaryContext(request);
            productionCanary = context;
            try
            {
                if(!request.IsLegacyPreset&&SkirmishMissionDispatch.IsExpandedCounterpart(request.CatalogId))
                    productionCounterpartScope=SkirmishProtectedExpandedCounterpartScope.Open(packaged,request.CatalogId);
                // One existing normal Menu runner. No LaunchCatalog direct bootstrap.
                bool ownedInputDiagnosticRequested = Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_INPUT_DIAGNOSTIC") == "1";
                if (paidPlaneSupplement) RunS005NormalPaidPlaneCruiseCameraCanary();
                else RunEditorJourney(request.Locale, request.Manual, false, request.CatalogId, request.Seed);
                if (ownedInputDiagnosticRequested) Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_INPUT_DIAGNOSTIC", "1");
                // Existing method defaults ARIA to ADJUST SETUP; v1 canary requires Main Menu.
                Environment.SetEnvironmentVariable("WARLINE_TERMINAL_ACTION", "MAIN MENU");
                context.Record("DispatchReturned", new JObject { ["editorPlayingOrWillChange"] = EditorApplication.isPlayingOrWillChangePlaymode });
            }
            catch (Exception error)
            {
                context.Record("DispatchException", new JObject { ["type"] = error.GetType().FullName, ["message"] = error.Message });
                // Do not restore scenes or release input ownership while an owned Play
                // transition remains active. Existing abort/finish lifecycle owns it.
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    Finish(true, "productionCanaryDispatchException:" + error.GetType().Name);
                else
                {
                    StopS004MenuProbe(); StopS004ManualProbe(); StopS004AriaConsentProbe();
                    EditorApplication.update -= Tick;
                    SessionState.SetBool(Key, false);
                    RestoreS004EditorJourneyOptions();
                    RestoreProductionCanaryAfterOwnedPlay();
                }
                throw;
            }
        }

        // Call at the existing FinishQuitAfterPlayMode seam, AFTER Play/compile/update
        // have ended and RestoreS004EditorJourneyOptions(), BEFORE wrapper completion.
        private static void RestoreProductionCanaryAfterOwnedPlay()
        {
            if (productionCanary == null) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Production cleanup cannot run during Play/compile/update.");
            var owned = productionCanary;
            // Retain the context if restore fails, preventing another dispatch.
            owned.Restore();
            productionCounterpartScope?.Dispose(); productionCounterpartScope=null;
            productionCanary = null;
        }

        private static void RecordProductionProfileBeforeRestoreAndResetEnvironment()
        {
            try { productionCanary?.RecordProfileBeforeRestore(); }
            finally
            {
                string previous = SessionState.GetString("Warline.AriaPlayValidation", "");
                Environment.SetEnvironmentVariable(
                    "WARLINE_VALIDATION_SAVE_ROOT",
                    string.IsNullOrEmpty(previous) ? null : previous);
                string backgroundPrevious = SessionState.GetString(BackgroundPreviousKey, "");
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",
                    string.IsNullOrEmpty(backgroundPrevious) ? null : backgroundPrevious);
            }
        }

        // Call only from actual native observation points; facts are not job flags.
        // These rows do not establish success. Preserve Defeat/Draw/failure unchanged.
        private static void RecordProductionNativeEvent(string kind, JObject nativeFacts)
        {
            if (productionCanary != null) productionCanary.Record(kind, nativeFacts);
        }

        private static void RecordProductionResolvedSnapshot(EntityManager em, Entity session,
            in SkirmishMatchState match, bool simulationActive, float elapsed, string kind)
        {
            if (productionCanary == null) return;
            if (productionCanary.Request.IsLegacyPreset) { RecordProductionLegacySnapshot(em,session,in match,simulationActive,elapsed,kind); return; }
            var observerBefore = BeginProductionObserverWindow(em, in match);
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var facts = CaptureProductionExpandedSnapshot(em, session, in match, simulationActive, elapsed, in state, setup);
            var identity = (JObject)facts["native_setup_identity"];
            productionCanary.Record(kind, facts);
            var request = productionCanary.Request;
            request.VerifyNativeSetupIdentity(identity);
            if (state.CatalogId.ToString() != request.CatalogId || setup.CatalogId != request.CatalogId ||
                setup.DefinitionId != (string)request.Job["definition_id"] || setup.ContentVersion != (int)request.Job["definition_version"] ||
                setup.Seed != request.Seed || setup.SizeId != SkirmishSizeId.Standard || setup.DifficultyId != SkirmishDifficultyId.Regular)
                throw new InvalidDataException("Actual resolved canary identity/setup differs from owned request.");
            productionCanary.CaptureVerifiedIdentity(identity,(string)facts["native_config_sha256"]);
            EndProductionObserverWindow(em, in match, observerBefore, "ExpandedResolvedSnapshot");
        }

        private static JObject CaptureProductionExpandedSnapshot(EntityManager em, Entity session,
            in SkirmishMatchState match, bool simulationActive, float elapsed,
            in SkirmishExpandedSessionComponent state, SkirmishResolvedSetup setup)
        {
            // Read actual resolved native values/current localization. Never derive
            // the census from requested job fields, environment or padded uint hashes.
            var identity = SkirmishProductionCanaryRequest.CreateNativeSetupIdentity(setup.CatalogId,
                setup.DefinitionId, setup.ContentVersion, setup.DifficultyId.ToString(), setup.SizeId.ToString(),
                setup.Seed, GameLocalization.CurrentLocaleCode, setup.SetupHash, setup.ContentHash);
            var facts = new JObject { ["native_world_sequence"] = em.WorldUnmanaged.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["native_session_id"] = state.SessionId.ToString(),
                ["catalog_id"] = setup.CatalogId, ["definition_id"] = setup.DefinitionId,
                ["definition_version"] = setup.ContentVersion, ["seed"] = setup.Seed,
                ["size"] = setup.SizeId.ToString(), ["difficulty"] = setup.DifficultyId.ToString(),
                ["native_content_identity"] = setup.ContentHash,
                ["native_config_sha256"] = SkirmishAcceptanceCensusCapture.ComputeConfigHash(setup),
                ["native_setup_hash_uint32"] = setup.SetupHash,
                ["actual_native_locale"] = GameLocalization.CurrentLocaleCode,
                ["native_setup_identity"] = identity,
                ["phase"] = match.Phase.ToString(), ["outcome"] = match.Outcome.ToString(),
                ["outcome_reason"] = match.Reason.ToString(), ["simulation_active"] = simulationActive,
                ["elapsed_seconds"] = elapsed, ["time_scale"] = Time.timeScale,
                ["normal_speed_latch"] = speedLatch.Normal, ["native_trace_path"] = LiveTracePath(),
                ["actual_save_root"] = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT") };
            if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
            {
                var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
                facts["clock_elapsed_seconds"] = clock.ElapsedSeconds;
                facts["clock_paused"] = clock.Paused; facts["clock_playing"] = clock.Playing;
            }
            if (em.HasComponent<SkirmishResultComponent>(session))
            {
                var result = em.GetComponentData<SkirmishResultComponent>(session);
                facts["result_session_id"] = result.SessionId.ToString(); facts["result_frozen"] = result.Frozen;
                facts["result_outcome"] = result.Outcome.ToString();
            }
            return facts;
        }

        private static Newtonsoft.Json.Linq.JToken ObserverValue(object value)
        {
            if (value == null) return Newtonsoft.Json.Linq.JValue.CreateNull();
            Type type = value.GetType();
            if (type.IsEnum) return new Newtonsoft.Json.Linq.JValue(value.ToString());
            if (type.IsPrimitive || value is string || value is decimal)
                return new Newtonsoft.Json.Linq.JValue(value);
            if (type.FullName.StartsWith("Unity.Collections.FixedString", StringComparison.Ordinal))
                return new Newtonsoft.Json.Linq.JValue(value.ToString());
            if (value is FixedList512Bytes<uint> groups)
            {
                var items = new JArray();
                for (int i = 0; i < groups.Length; i++) items.Add(groups[i]);
                return items;
            }
            if (type.FullName.StartsWith("Unity.Collections.FixedList", StringComparison.Ordinal))
                throw new InvalidDataException("Unsupported observer fixed-list payload; admission remains pending.");
            var result = new Newtonsoft.Json.Linq.JObject();
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Array.Sort(fields, (a,b) => string.CompareOrdinal(a.Name,b.Name));
            foreach (var field in fields) result[field.Name] = ObserverValue(field.GetValue(value));
            return result;
        }
        private static void ObserverComponent<T>(EntityManager em, Entity entity, Newtonsoft.Json.Linq.JObject values)
            where T : unmanaged, IComponentData
        {
            values[typeof(T).FullName] = em.HasComponent<T>(entity)
                ? ObserverValue(em.GetComponentData<T>(entity)) : Newtonsoft.Json.Linq.JValue.CreateNull();
        }
        private static void ObserverBuffer<T>(EntityManager em, Entity entity, Newtonsoft.Json.Linq.JObject values)
            where T : unmanaged, IBufferElementData
        {
            if (!em.HasBuffer<T>(entity)) { values[typeof(T).FullName] = Newtonsoft.Json.Linq.JValue.CreateNull(); return; }
            var result = new Newtonsoft.Json.Linq.JArray();
            var buffer = em.GetBuffer<T>(entity, true);
            for (int i=0;i<buffer.Length;i++) result.Add(ObserverValue(buffer[i]));
            values[typeof(T).FullName] = result;
        }
        private static Newtonsoft.Json.Linq.JObject CaptureProductionObserverState(EntityManager em)
        {
            using var allEntities = em.CreateEntityQuery(new EntityQueryDesc {
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab });
            using var query = em.CreateEntityQuery(new EntityQueryDesc {
                Any = new ComponentType[] { ComponentType.ReadOnly<SkirmishMainBase>(), ComponentType.ReadOnly<AriaPlayObservationComponent>(), ComponentType.ReadOnly<AriaPlaySessionComponent>(), ComponentType.ReadOnly<AriaSkirmishObservationComponent>(), ComponentType.ReadOnly<AriaSkirmishObservationOwner>(), ComponentType.ReadOnly<AriaSkirmishPlanComponent>(), ComponentType.ReadOnly<AttackMoveOrder>(), ComponentType.ReadOnly<EngageTarget>(), ComponentType.ReadOnly<SkirmishActionRequest>(), ComponentType.ReadOnly<SkirmishArmySelectionComponent>(), ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(), ComponentType.ReadOnly<SkirmishCapacityComponent>(), ComponentType.ReadOnly<SkirmishContactKnowledgeElement>(), ComponentType.ReadOnly<SkirmishContactLedgerElement>(), ComponentType.ReadOnly<SkirmishContactSightComponent>(), ComponentType.ReadOnly<SkirmishEconomyStockComponent>(), ComponentType.ReadOnly<SkirmishExpandedSessionComponent>(), ComponentType.ReadOnly<SkirmishFogStateComponent>(), ComponentType.ReadOnly<SkirmishMatchState>(), ComponentType.ReadOnly<SkirmishMoveIntentComponent>(), ComponentType.ReadOnly<SkirmishObjectiveClockComponent>(), ComponentType.ReadOnly<SkirmishObjectiveStateComponent>(), ComponentType.ReadOnly<SkirmishProductionReservation>(), ComponentType.ReadOnly<SkirmishResultComponent>(), ComponentType.ReadOnly<SkirmishRoleOverlayComponent>(), ComponentType.ReadOnly<SkirmishVehicleFuelRemainderComponent>(), ComponentType.ReadOnly<UiShellStateComponent>(), ComponentType.ReadOnly<UnitAirMovement>(), ComponentType.ReadOnly<UnitAttack>(), ComponentType.ReadOnly<UnitAttackOrderQueueComponent>(), ComponentType.ReadOnly<UnitAttackOrderRequestElement>(), ComponentType.ReadOnly<UnitAttackOrderResultElement>(), ComponentType.ReadOnly<UnitCombat>(), ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitLongDistanceMove>(), ComponentType.ReadOnly<UnitMove>(), ComponentType.ReadOnly<UnitPathFollow>(), ComponentType.ReadOnly<UnitPathRange>(), ComponentType.ReadOnly<UnitPathRequest>(), ComponentType.ReadOnly<UnitPathRetryCooldown>(), ComponentType.ReadOnly<UnitResourceHaulOrder>(), ComponentType.ReadOnly<UnitResourceHaulStatus>(), ComponentType.ReadOnly<UnitResourceHauler>(), ComponentType.ReadOnly<UnitTarget>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab });
            using var entities = query.ToEntityArray(Allocator.Temp);
            // Complete actual relevant set; no mission-size cap or sample truncation.
            var ordered = entities.ToArray();
            Array.Sort(ordered,(a,b)=>a.Index!=b.Index?a.Index.CompareTo(b.Index):a.Version.CompareTo(b.Version));
            var result = new Newtonsoft.Json.Linq.JObject();
            foreach (var entity in ordered)
            {
                var values = new Newtonsoft.Json.Linq.JObject();
                using var types = em.GetComponentTypes(entity, Allocator.Temp);
                var names = new List<string>();
                for(int i=0;i<types.Length;i++) names.Add(types[i].GetManagedType().FullName);
                names.Sort(StringComparer.Ordinal);
                values["componentMembership"] = new Newtonsoft.Json.Linq.JArray(names);
                if (em.HasComponent<SkirmishResolvedSetupRecord>(entity))
                    values["resolvedSetupManaged"] = JsonUtility.ToJson(em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup);
                if (em.HasComponent<SkirmishLaunchSnapshot>(entity))
                    values["legacyLaunchConfiguration"] = JsonUtility.ToJson(em.GetComponentObject<SkirmishLaunchSnapshot>(entity).Configuration);
                ObserverComponent<SkirmishExpandedSessionComponent>(em,entity,values);
                ObserverComponent<SkirmishMatchState>(em,entity,values);
                ObserverComponent<SkirmishMainBase>(em,entity,values);
                ObserverComponent<SkirmishObjectiveStateComponent>(em,entity,values);
                ObserverComponent<SkirmishObjectiveClockComponent>(em,entity,values);
                ObserverComponent<SkirmishResultComponent>(em,entity,values);
                ObserverComponent<SkirmishEconomyStockComponent>(em,entity,values);
                ObserverComponent<SkirmishCapacityComponent>(em,entity,values);
                ObserverComponent<SkirmishVehicleFuelRemainderComponent>(em,entity,values);
                ObserverComponent<LocalTransform>(em,entity,values);
                ObserverComponent<UnitHealth>(em,entity,values);
                ObserverComponent<Faction>(em,entity,values);
                ObserverComponent<CombatTargetPolicy>(em,entity,values);
                ObserverComponent<SkirmishAttemptOwnedComponent>(em,entity,values);
                ObserverComponent<SkirmishRoleOverlayComponent>(em,entity,values);
                ObserverComponent<SkirmishMoveIntentComponent>(em,entity,values);
                ObserverComponent<AttackMoveOrder>(em,entity,values);
                ObserverComponent<EngageTarget>(em,entity,values);
                ObserverComponent<UnitAttack>(em,entity,values);
                ObserverComponent<UnitCombat>(em,entity,values);
                ObserverComponent<UnitMove>(em,entity,values);
                ObserverComponent<UnitGrid>(em,entity,values);
                ObserverComponent<UnitTarget>(em,entity,values);
                ObserverComponent<UnitPathRequest>(em,entity,values);
                ObserverComponent<UnitPathFollow>(em,entity,values);
                ObserverComponent<UnitPathRange>(em,entity,values);
                ObserverComponent<UnitPathRetryCooldown>(em,entity,values);
                ObserverComponent<UnitLongDistanceMove>(em,entity,values);
                ObserverComponent<UnitAirMovement>(em,entity,values);
                ObserverComponent<UnitResourceHaulOrder>(em,entity,values);
                ObserverComponent<UnitResourceHaulStatus>(em,entity,values);
                ObserverComponent<UnitResourceHauler>(em,entity,values);
                ObserverComponent<UnitAttackOrderQueueComponent>(em,entity,values);
                ObserverComponent<SkirmishArmySelectionComponent>(em,entity,values);
                ObserverComponent<SkirmishFogStateComponent>(em,entity,values);
                ObserverComponent<SkirmishContactSightComponent>(em,entity,values);
                ObserverComponent<AriaPlaySessionComponent>(em,entity,values);
                ObserverComponent<AriaPlayObservationComponent>(em,entity,values);
                ObserverComponent<AriaSkirmishObservationComponent>(em,entity,values);
                ObserverComponent<AriaSkirmishObservationOwner>(em,entity,values);
                ObserverComponent<AriaSkirmishPlanComponent>(em,entity,values);
                ObserverComponent<UiShellStateComponent>(em,entity,values);
                ObserverBuffer<SkirmishProductionReservation>(em,entity,values);
                ObserverBuffer<SkirmishContactKnowledgeElement>(em,entity,values);
                ObserverBuffer<SkirmishContactLedgerElement>(em,entity,values);
                ObserverBuffer<UnitAttackOrderRequestElement>(em,entity,values);
                ObserverBuffer<UnitAttackOrderResultElement>(em,entity,values);
                ObserverBuffer<SkirmishActionRequest>(em,entity,values);
                result[entity.Index+":"+entity.Version] = SkirmishProductionCanaryRequest.Hash(Encoding.UTF8.GetBytes(values.ToString(Formatting.None)));
            }
            string root = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            var profile = new JObject { ["root"] = root, ["changeVersion"] = JsonSaveRepository.ChangeVersion };
            if (!string.IsNullOrEmpty(root))
            {
                if (!Path.IsPathRooted(root)) throw new InvalidDataException("Absolute owned observer profile root required.");
                string fullRoot = Path.GetFullPath(root);
                if (Directory.Exists(fullRoot) && (File.GetAttributes(fullRoot) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Observer profile root is a reparse point.");
                string quickGame = Path.Combine(fullRoot, SaveService.QuickGameFileName);
                bool exists = File.Exists(quickGame); profile["quickGameExists"] = exists;
                if (exists)
                {
                    if ((File.GetAttributes(quickGame) & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidDataException("Observer quickgame file is a reparse point.");
                    profile["quickGameSha256"] = SkirmishProductionCanaryRequest.FileHash(quickGame);
                    profile["quickGameLength"] = new FileInfo(quickGame).Length;
                }
            }
            return new JObject { ["capturedFrame"] = Time.frameCount, ["worldSequence"] = em.WorldUnmanaged.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["worldEntityCount"] = allEntities.CalculateEntityCount(), ["queriedRelevantEntityCount"] = ordered.Length, ["entities"] = result, ["profile"] = profile };
        }

        // Explicit QA flag; at most one Playing and one Finished window per owned attempt.
        private static readonly bool ProductionObserverProofEnabled =
            Environment.GetEnvironmentVariable("WARLINE_PRODUCTION_READ_ONLY_OBSERVER_PROOF") == "1";
        private static JObject BeginProductionObserverWindow(EntityManager em, in SkirmishMatchState match)
        {
            if (productionCanary == null ||
                !ProductionObserverProofEnabled ||
                (match.Phase != SkirmishPhase.Playing && match.Phase != SkirmishPhase.Finished)) return null;
            bool terminal = match.Phase == SkirmishPhase.Finished;
            if (terminal ? productionCanary.ObserverTerminalProofCaptured : productionCanary.ObserverLiveProofCaptured) return null;
            if (terminal) productionCanary.ObserverTerminalProofCaptured = true;
            else productionCanary.ObserverLiveProofCaptured = true;
            productionCanary.ObserverProofStartedTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            try { return CaptureProductionObserverState(em); }
            catch (OutOfMemoryException) { throw; }
            catch (Exception error)
            {
                productionCanary.Record("NativeReadOnlyObserverProofUnsupported",new JObject {
                    ["native_session_id"]=match.SessionId.ToString(),["phase"]=match.Phase.ToString(),
                    ["reason"]=error.ToString(),["read_only_observers"]=false });
                return null;
            }
        }
        private static void EndProductionObserverWindow(EntityManager em, in SkirmishMatchState match,
            JObject before, string scope)
        {
            if (before == null) return;
            try
            {
                var after=CaptureProductionObserverState(em);
                bool equal=JToken.DeepEquals(before,after);
                productionCanary.Record("NativeReadOnlyObserverProof",new JObject {
                    ["schema"]="NativeReadOnlyObserverWindow-v1",["scope"]=scope,
                    ["native_world_sequence"]=em.WorldUnmanaged.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["native_session_id"]=match.SessionId.ToString(),["catalog_id"]=productionCanary.Request.CatalogId,
                    ["phase"]=match.Phase.ToString(),["status"]=equal?"ComparedEqual":"MutationObserved",
                    ["beforeSha256"]=SkirmishProductionCanaryRequest.Hash(System.Text.Encoding.UTF8.GetBytes(before.ToString(Formatting.None))),
                    ["afterSha256"]=SkirmishProductionCanaryRequest.Hash(System.Text.Encoding.UTF8.GetBytes(after.ToString(Formatting.None))),
                    ["capturedEntityCount"]=((JObject)before["entities"]).Count,
                    ["queriedRelevantEntityCount"]=before["queriedRelevantEntityCount"].DeepClone(),
                    ["coverageComplete"]=((JObject)before["entities"]).Count==(int)before["queriedRelevantEntityCount"],
                    ["capturedFrameBefore"]=before["capturedFrame"].DeepClone(), ["capturedFrameAfter"]=after["capturedFrame"].DeepClone(),
                    ["worldEntityCount"]=before["worldEntityCount"].DeepClone(),
                    ["observerElapsedMilliseconds"]=(System.Diagnostics.Stopwatch.GetTimestamp()-productionCanary.ObserverProofStartedTimestamp)*1000.0/System.Diagnostics.Stopwatch.Frequency,
                    ["captureQueriesReadOnly"]=true,["worldUpdateBetweenSnapshots"]=false,
                    ["diagnosticFactsFedToAria"]=false,["missionReady"]=false });
            }
            catch (OutOfMemoryException) { throw; }
            catch (Exception error)
            {
                productionCanary.Record("NativeReadOnlyObserverProofUnsupported",new JObject {
                    ["native_session_id"]=match.SessionId.ToString(),["phase"]=match.Phase.ToString(),
                    ["reason"]=error.ToString(),["read_only_observers"]=false });
            }
        }

        private static void RecordProductionTerminalReturn(bool passed, string reason)
        {
            if (productionCanary == null) return;
            var facts = new JObject { ["native_terminal_session_id"] = terminalAttempt.ToString(),
                ["owner_probe_passed"] = passed, ["owner_probe_reason"] = reason,
                ["owner_probe_step"] = terminalStep, ["terminal_action"] = terminalAction,
                ["destination_visible_since"] = terminalDestinationVisibleSince,
                ["driver_device"] = terminalTouch == null ? -1 : terminalTouch.DeviceId,
                ["driver_generation"] = terminalTouch == null ? 0 : terminalTouch.Generation,
                ["completed_gestures"] = terminalTouch == null ? 0 : terminalTouch.CompletedGestures,
                ["unexpected_samples"] = terminalTouch == null ? 0 : terminalTouch.UnexpectedSamples,
                ["physical_interventions"] = terminalTouch == null ? 0 : terminalTouch.PhysicalInterventions };
            if (UiShellRuntimeGateway.TryReadShellState(out var shell))
            { facts["actual_shell_route"] = shell.ActiveRoute.ToString(); facts["actual_shell_transition_running"] = shell.IsTransitionRunning; }
            var world = World.DefaultGameObjectInjectionWorld;
            facts["native_world_present"] = world != null && world.IsCreated;
            if (world != null && world.IsCreated)
                facts["native_match_session_present"] = SkirmishLaunchProjection.TryGet(world.EntityManager, out _, out _);
            facts["native_terminal_outcome"] = terminalMatch.Outcome.ToString();
            if (world != null && world.IsCreated)
                facts["native_terminal_session_count"] = SkirmishTerminalPresentationObservation.NativeSessionCount(world.EntityManager);
            facts["rendered_result_presentation"] = terminalResultPresentation == null ? JValue.CreateNull() : JObject.FromObject(terminalResultPresentation);
            var menu = ReadTerminalMenuPresentation();
            facts["localized_main_menu_presentation"] = menu == null ? JValue.CreateNull() : JObject.FromObject(menu);
            facts["native_outcome_matches_rendered_title"] = SkirmishTerminalPresentationObservation.AcceptResult(
                terminalResultPresentation, terminalAttempt.ToString(), terminalMatch.Outcome);
            if (menu != null)
            {
                facts["presented_menu_control"] = "Hotspot";
                facts["presented_menu_control_path"] = menu.controlPath;
                facts["presented_menu_point_x"] = menu.pointX;
                facts["presented_menu_point_y"] = menu.pointY;
                facts["presented_menu_title"] = menu.actualTitle;
                facts["presented_menu_locale"] = menu.locale;
            }
            facts["legacy_result_saved_verified"] = productionCanary.Request.IsLegacyPreset && productionCanary.LegacySavedResultVerified;
            productionCanary.Record("TerminalReturnOwnerCompleted", facts);
        }

        internal static JObject PaidPoint(Game.Runtime.SkirmishProductionService.ProductionLedgerPoint point)
            {
                var bank=point.Bank; var receipt=point.Reservation;
                return new JObject { ["faction"]=point.Faction,["bank_index"]=point.BankIndex,["bank_version"]=point.BankVersion,
                    ["materials"]=point.Materials,["fuel"]=point.Fuel,["bank"]=JObject.FromObject(bank),
                    ["reservation"]=!point.HasReservation?null:new JObject {
                        ["id"]=receipt.ReservationId,["role"]=receipt.Role.ToString(),["faction"]=receipt.FactionId,
                        ["phase"]=receipt.Phase.ToString(),["producer"]=receipt.ProducerRuntimeId,
                        ["paid"]=receipt.MaterialsPaid,["members"]=receipt.MemberCount,["delivered"]=receipt.DeliveredMembers,
                        ["remaining"]=receipt.RemainingMembers,["supply"]=receipt.SupplyCost } };
            }

        internal static JObject ResearchPointJson(Game.Runtime.SkirmishResearchService.ResearchLedgerPoint point)
            {
                var item=point.Item;
                return new JObject { ["bank_index"]=point.BankEntity.Index,["bank_version"]=point.BankEntity.Version,
                    ["bank"]=JObject.FromObject(point.Bank),["materials"]=point.Materials,["fuel"]=point.Fuel,
                    ["next_research_id"]=point.NextResearchId,["state"]=JObject.FromObject(point.State),
                    ["research"]=!point.HasItem?null:new JObject { ["id"]=item.ResearchId,["kind"]=item.Kind.ToString(),
                        ["phase"]=item.Phase.ToString(),["faction"]=item.FactionId,["producer"]=item.Producer.ToString(),
                        ["paid"]=item.MaterialsPaid,["remaining_seconds"]=item.RemainingSeconds } };
            }

        internal static JObject ConstructionPoint(Game.Runtime.SkirmishConstructionLedger.Point p)
            {
                var i=p.EnemyIntent;var r=p.Request;
                return new JObject { ["bank_index"]=p.BankEntity.Index,["bank_version"]=p.BankEntity.Version,
                    ["economy"]=JObject.FromObject(p.Economy),["bank"]=JObject.FromObject(p.Bank),
                    ["session_index"]=p.Session.Index,["session_version"]=p.Session.Version,
                    ["enemy_intent"]=!p.HasEnemyIntent?null:new JObject { ["version"]=i.Version,["session"]=i.SessionId.ToString(),
                        ["phase"]=i.Phase,["action"]=i.ActionKey.ToString(),["pad"]=i.PadId.ToString(),["building"]=i.BuildingId.ToString(),
                        ["origin_x"]=i.Origin.x,["origin_y"]=i.Origin.y,["width"]=i.Footprint.x,["depth"]=i.Footprint.y,
                        ["paid"]=i.MaterialsPaid,["request_id"]=i.RequestId,["candidate"]=i.CandidateIndex,["desired_count"]=i.DesiredCount,
                        ["result_stable_id"]=i.ResultStableObjectId.ToString(),["retry_after"]=i.RetryAfter },
                    ["request"]=!p.HasRequest?null:new JObject { ["id"]=r.RequestId,["kind"]=r.RequestKind,["status"]=r.Status,
                        ["building"]=r.BuildingId.ToString(),["faction"]=r.FactionId,["materials"]=r.MaterialsCost,
                        ["preferred_x"]=r.PreferredOrigin.x,["preferred_y"]=r.PreferredOrigin.y,["actual_x"]=r.ActualOrigin.x,["actual_y"]=r.ActualOrigin.y,
                        ["actual_width"]=r.ActualFootprint.x,["actual_depth"]=r.ActualFootprint.y,["runtime_id"]=r.BuildingRuntimeId,
                        ["plan_index"]=r.PlanEntity.Index,["plan_version"]=r.PlanEntity.Version,["authored_grant"]=r.AuthoredStartingGrant,
                        ["allow_nonbuildable"]=r.AllowNonBuildableEnemy,["require_origin"]=r.RequirePreferredOrigin } };
            }

        internal static JObject PaidProductionDecisionJson(Game.Runtime.SkirmishProductionService.ProductionLedgerObservation row)
        {
            return !row.HasDecision?null:new JObject { ["materials"]=row.Decision.MaterialsCost,
                            ["fuel"]=row.Decision.FuelCost,["refund"]=row.Decision.RefundedMaterials,
                            ["accepted"]=row.Decision.Accepted,["reason"]=row.Decision.Reason.ToString(),
                            ["reservation"]=row.Decision.ReservationId };
        }

        internal static JObject PaidProductionActorJson(Game.Runtime.SkirmishProductionService.ProductionLedgerObservation row)
        {
            var em=row.Manager;
            return !row.HasActorOwner?null:new JObject { ["index"]=row.Actor.Index,["version"]=row.Actor.Version,
                            ["session"]=row.ActorOwner.SessionId.ToString(),["stable_id"]=row.ActorOwner.StableObjectId.ToString(),
                            ["faction"]=row.ActorOwner.FactionId,["reservation"]=row.ActorOwner.ReservationId,
                            ["shared_actor"]=em.HasComponent<SkirmishSharedActorTag>(row.Actor),
                            ["is_structure"]=row.ActorOwner.IsStructure,["capacity_released"]=row.ActorOwner.CapacityReleased,
                            ["role"]=em.GetComponentData<SkirmishUnitRoleComponent>(row.Actor).Role.ToString(),
                            ["source_key"]=em.GetComponentData<UnitSourcePrefabKey>(row.Actor).Value.ToString() };
        }

        internal static JObject PaidResearchDecisionJson(Game.Runtime.SkirmishResearchService.ResearchLedgerObservation row)
        {
            return row.HasDecision?JObject.FromObject(row.Decision):null;
        }

        internal static JObject PaidConstructionActorJson(Game.Runtime.SkirmishConstructionLedger.Observation row)
        {
            var em=row.After.Manager;var actor=row.ActorOwner;
            return !row.HasActorOwner?null:new JObject { ["index"]=row.Actor.Index,["version"]=row.Actor.Version,
                            ["session"]=actor.SessionId.ToString(),["stable_id"]=actor.StableObjectId.ToString(),["faction"]=actor.FactionId,["is_structure"]=actor.IsStructure,
                            ["shared_actor"]=em.HasComponent<SkirmishSharedActorTag>(row.Actor),["source_key"]=em.GetComponentData<UnitSourcePrefabKey>(row.Actor).Value.ToString() };
        }

        internal static string SerializeProductionNativeRecord(JObject row)
            =>row.ToString(Formatting.None);

        internal static void RequirePaidObserverGroupAvailable(bool enabled, bool legacy)
        {
            if (!enabled) return;
            if (legacy) throw new InvalidDataException("Native queued ledger requires expanded setup");
            if (Game.Runtime.SkirmishProductionService.ProductionLedgerObserver != null)
                throw new InvalidOperationException("Production ledger already observed");
            if (Game.Runtime.SkirmishResearchService.ResearchLedgerObserver != null)
                throw new InvalidOperationException("Research ledger already observed");
            if (Game.Runtime.SkirmishConstructionLedger.Observer != null)
                throw new InvalidOperationException("Construction ledger already observed");
        }
        internal static void GuardPaidObserverReservation(bool enabled, bool legacy, Action reserve)
        {
            RequirePaidObserverGroupAvailable(enabled, legacy);
            reserve();
        }
        internal static void StartPaidObserverGroup(bool enabled, Action production, Action research,
            Action construction, Action closeConstruction, Action closeResearch, Action closeProduction)
        {
            if (!enabled) return;
            try { production(); research(); construction(); }
            catch
            {
                ClosePaidObserverGroup(closeConstruction, closeResearch, closeProduction, () => { });
                throw;
            }
        }
        internal static void ClosePaidObserverGroup(Action construction, Action research,
            Action production, Action restore)
        {
            try
            {
                try { construction(); }
                finally { try { research(); } finally { production(); } }
            }
            finally { restore(); }
        }
        internal static bool ReleasePaidProductionObserver(
            Action<Game.Runtime.SkirmishProductionService.ProductionLedgerObservation> owned)
        {
            if (owned == null || Game.Runtime.SkirmishProductionService.ProductionLedgerObserver != owned) return false;
            Game.Runtime.SkirmishProductionService.ProductionLedgerObserver = null;
            return true;
        }
        internal static bool ReleasePaidResearchObserver(
            Action<Game.Runtime.SkirmishResearchService.ResearchLedgerObservation> owned)
        {
            if (owned == null || Game.Runtime.SkirmishResearchService.ResearchLedgerObserver != owned) return false;
            Game.Runtime.SkirmishResearchService.ResearchLedgerObserver = null;
            return true;
        }

        private sealed class ProductionCanaryContext
        {
            internal readonly SkirmishProductionCanaryRequest Request;
            internal bool LegacySavedResultVerified;
            internal bool ObserverLiveProofCaptured, ObserverTerminalProofCaptured;
            internal long ObserverProofStartedTimestamp;
            private JObject verifiedNativeIdentity;
            private string verifiedNativeConfigHash;
            internal void CaptureVerifiedIdentity(JObject identity,string configHash)
            {
                Request.VerifyNativeSetupIdentity(identity);
                if(!System.Text.RegularExpressions.Regex.IsMatch(configHash??"","^[a-f0-9]{64}$")||
                    (verifiedNativeIdentity!=null&&(!JToken.DeepEquals(verifiedNativeIdentity,identity)||verifiedNativeConfigHash!=configHash)))
                    throw new InvalidDataException("Verified native setup/config identity changed within attempt");
                verifiedNativeIdentity=(JObject)identity.DeepClone();verifiedNativeConfigHash=configHash;
            }
            internal bool TryApplyNativeProvenance(ref SkirmishS002AriaTerminalFacts facts,string actualSourceHash)
            {
                if(verifiedNativeIdentity==null)return false;
                Request.VerifyNativeSetupIdentity(verifiedNativeIdentity);
                if((string)verifiedNativeIdentity["catalog_id"]!=facts.CatalogId||(int)verifiedNativeIdentity["seed"]!=facts.Seed||
                    (string)verifiedNativeIdentity["locale"]!=(facts.Locale=="en"?"EN":"FA")||
                    actualSourceHash!=(string)Request.Job["candidate_sha256"])throw new InvalidDataException("Native run provenance differs from exact claimed attempt");
                facts.DefinitionVersion=Request.IsLegacyPreset?0:(int)verifiedNativeIdentity["definition_version"];facts.CodeHash=actualSourceHash;facts.ConfigHash=verifiedNativeConfigHash;return true;
            }
            private readonly Dictionary<string, string> environment = new Dictionary<string, string>(StringComparer.Ordinal);
            private readonly SceneSetup[] sceneSetup;
            private readonly bool optionsEnabled;
            private readonly EnterPlayModeOptions options;
            private readonly bool refreshAlreadyOwned;
            private int sequence;
            private bool restored;
            private readonly string events;

            internal ProductionCanaryContext(SkirmishProductionCanaryRequest request)
            {
                RequirePaidObserverGroupAvailable(Environment.GetEnvironmentVariable("WARLINE_PRODUCTION_RESOURCE_LEDGER")=="1",request.IsLegacyPreset);
                Request = request;
                EnsureScenesClean();
                var restorableBaseline=PrepareProductionRestorableSceneBaseline();
                sceneSetup = EditorSceneManager.GetSceneManagerSetup();
                RequireProductionRestorableSetup(sceneSetup);
                optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
                options = EditorSettings.enterPlayModeOptions;
                refreshAlreadyOwned = SessionState.GetBool("Warline.MissionValidation.ResumeRefresh", false);
                if (refreshAlreadyOwned) throw new InvalidOperationException("Another validation owns refresh suspension.");
                foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                {
                    string key = (string)entry.Key;
                    if (key.StartsWith("WARLINE_", StringComparison.Ordinal)) environment[key] = (string)entry.Value;
                }
                events = Path.Combine(request.ArtifactDirectory, "adapter-native-events.jsonl");
                // Exclusive ownership, including concurrent public method invocations.
                GuardPaidObserverReservation(Environment.GetEnvironmentVariable("WARLINE_PRODUCTION_RESOURCE_LEDGER")=="1",request.IsLegacyPreset,request.ReserveAdapterStart);
                using (File.Open(events, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { }
                Record("ContextCaptured", new JObject { ["source_sha256"] = (string)request.Job["candidate_sha256"],
                    ["compiled_dll_manifest_sha256"] = (string)request.Job["build_sha256"], ["scene_count"] = sceneSetup.Length,
                    ["restorable_scene_baseline"] = restorableBaseline });
                RecordProfileStage("ProfileBeforeDispatch");
                StartPaidObserverGroup(Environment.GetEnvironmentVariable("WARLINE_PRODUCTION_RESOURCE_LEDGER")=="1",
                    StartPaidProductionLedger,StartPaidResearchLedger,StartConstructionLedger,
                    StopConstructionLedger,StopPaidResearchLedger,StopPaidProductionLedger);
            }

            internal void Record(string kind, JObject facts)
            {
                // Envelope prevents facts from replacing identity/sequence ownership.
                var row = new JObject { ["schema_version"] = 1, ["sequence"] = ++sequence,
                    ["job_sha256"] = Request.JobHash, ["attempt_id"] = (string)Request.Claim["attempt_id"],
                    ["worker"] = (string)Request.Claim["worker"], ["frame"] = Time.frameCount,
                    ["utc"] = DateTimeOffset.UtcNow.ToString("O"), ["kind"] = kind,
                    ["facts"] = facts ?? new JObject() };
                File.AppendAllText(events, SerializeProductionNativeRecord(row) + "\n", new UTF8Encoding(false));
                ObserveProductionBatchNativeEvent(kind,facts);
            }


            private Action<Game.Runtime.SkirmishResearchService.ResearchLedgerObservation> researchLedgerSink;
            private int researchLedgerCount;
            private bool researchLedgerFailed;
            private long researchLedgerFaultStart;
            private void StartPaidResearchLedger()
            {
                if(Environment.GetEnvironmentVariable("WARLINE_PRODUCTION_RESOURCE_LEDGER")!="1")return;
                if(Request.IsLegacyPreset || Game.Runtime.SkirmishResearchService.ResearchLedgerObserver!=null)
                    throw new InvalidOperationException("Owned research ledger unavailable");
                researchLedgerFaultStart=Game.Runtime.SkirmishResearchService.ResearchLedgerObservationFaults;
                researchLedgerSink=ObservePaidResearch;
                Record("NativeResearchLedgerStarted",new JObject { ["ledger_schema"]=1,["scope"]="ResearchCommittedOriginsPartial",["remaining_time_history_measured"]=false,["progress_rows_emitted"]=false,["paid_resources_qualified"]=false });
                Game.Runtime.SkirmishResearchService.ResearchLedgerObserver=researchLedgerSink;
            }

            private void ObservePaidResearch(Game.Runtime.SkirmishResearchService.ResearchLedgerObservation row)
            {
                if(researchLedgerFailed)return;
                try
                {
                    if(++researchLedgerCount>4096)throw new InvalidDataException("Research ledger bounded capture failed");
                    var em=row.Manager;var native=em.GetComponentData<SkirmishExpandedSessionComponent>(row.Session);
                    var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(row.Session).Setup;
                    var identity=SkirmishProductionCanaryRequest.CreateNativeSetupIdentity(setup.CatalogId,setup.DefinitionId,setup.ContentVersion,
                        setup.DifficultyId.ToString(),setup.SizeId.ToString(),setup.Seed,GameLocalization.CurrentLocaleCode,setup.SetupHash,setup.ContentHash);
                    Request.VerifyNativeSetupIdentity(identity);
                    Record("NativeResearchTransaction",new JObject { ["ledger_schema"]=1,["scope"]="ResearchCommittedOriginsPartial",
                        ["ledger_sequence"]=researchLedgerCount,["operation"]=row.Operation,
                        ["native_world_sequence"]=em.WorldUnmanaged.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["native_session_id"]=native.SessionId.ToString(),["native_setup_identity"]=identity,
                        ["native_config_sha256"]=SkirmishAcceptanceCensusCapture.ComputeConfigHash(setup),
                        ["before"]=ResearchPointJson(row.Before),["after"]=ResearchPointJson(row.After),
                        ["decision"]=PaidResearchDecisionJson(row),
                        ["remaining_time_history_measured"]=false,["paid_resources_qualified"]=false });
                }
                catch(Exception error)when(!(error is OutOfMemoryException))
                {
                    researchLedgerFailed=true;
                    try{Record("NativeResearchLedgerFailure",new JObject { ["exception_type"]=error.GetType().FullName });}
                    catch(Exception failure)when(!(failure is OutOfMemoryException)){}
                }
            }
            private void StopPaidResearchLedger()
            {
                if(researchLedgerSink==null)return;
                bool owned=ReleasePaidResearchObserver(researchLedgerSink);
                researchLedgerSink=null;
                try{Record("NativeResearchLedgerClosed",new JObject { ["ledger_schema"]=1,["scope"]="ResearchCommittedOriginsPartial",
                    ["transactions"]=researchLedgerCount,["capture_failed"]=researchLedgerFailed,["sink_owned"]=owned,
                    ["observer_faults"]=Game.Runtime.SkirmishResearchService.ResearchLedgerObservationFaults-researchLedgerFaultStart,
                    ["remaining_time_history_measured"]=false,["progress_rows_emitted"]=false,
                    ["paid_resources_qualified"]=false });}
                catch(Exception error)when(!(error is OutOfMemoryException)){}
            }

            private System.Action<Game.Runtime.SkirmishProductionService.ProductionLedgerObservation> paidLedgerSink;
            private int paidLedgerCount;
            private bool paidLedgerFailed;
            private long paidLedgerFaultStart;
            private void StartPaidProductionLedger()
            {
                if(Environment.GetEnvironmentVariable("WARLINE_PRODUCTION_RESOURCE_LEDGER")!="1")return;
                if(Request.IsLegacyPreset)throw new InvalidDataException("Native queued ledger requires expanded setup");
                if(Game.Runtime.SkirmishProductionService.ProductionLedgerObserver!=null)
                    throw new InvalidOperationException("Production ledger already observed");
                paidLedgerFaultStart=Game.Runtime.SkirmishProductionService.ProductionLedgerObservationFaults;
                paidLedgerSink=ObservePaidProduction;
                Record("NativeProductionLedgerStarted",new JObject { ["ledger_schema"]=1,
                    ["scope"]="ProductionCommittedOriginsPartial",["paid_resources_qualified"]=false });
                Game.Runtime.SkirmishProductionService.ProductionLedgerObserver=paidLedgerSink;
            }

            private void ObservePaidProduction(Game.Runtime.SkirmishProductionService.ProductionLedgerObservation row)
            {
                if(paidLedgerFailed)return;
                try
                {
                    if(++paidLedgerCount>4096)throw new InvalidDataException("Production ledger bounded capture failed");
                    var em=row.Manager;var state=em.GetComponentData<SkirmishExpandedSessionComponent>(row.Session);
                    var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(row.Session).Setup;
                    var identity=SkirmishProductionCanaryRequest.CreateNativeSetupIdentity(setup.CatalogId,setup.DefinitionId,
                        setup.ContentVersion,setup.DifficultyId.ToString(),setup.SizeId.ToString(),setup.Seed,
                        GameLocalization.CurrentLocaleCode,setup.SetupHash,setup.ContentHash);
                    Request.VerifyNativeSetupIdentity(identity);
                    var facts=new JObject { ["ledger_schema"]=1,["scope"]="ProductionCommittedOriginsPartial",
                        ["ledger_sequence"]=paidLedgerCount,["operation"]=row.Operation,["member_ordinal"]=row.MemberOrdinal,
                        ["native_world_sequence"]=em.WorldUnmanaged.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["native_session_id"]=state.SessionId.ToString(),["native_setup_identity"]=identity,
                        ["native_config_sha256"]=SkirmishAcceptanceCensusCapture.ComputeConfigHash(setup),
                        ["before"]=PaidPoint(row.Before),["after"]=PaidPoint(row.After),
                        ["decision"]=PaidProductionDecisionJson(row),
                        ["actor"]=PaidProductionActorJson(row),
                        ["paid_resources_qualified"]=false };
                    Record("NativeProductionTransaction",facts);
                }
                catch(Exception error) when (!(error is OutOfMemoryException))
                {
                    paidLedgerFailed=true;
                    try { Record("NativeProductionLedgerFailure",new JObject { ["exception_type"]=error.GetType().FullName }); }
                    catch(Exception failure) when (!(failure is OutOfMemoryException)) { }
                }
            }
            private void StopPaidProductionLedger()
            {
                if(paidLedgerSink==null)return;
                bool same=ReleasePaidProductionObserver(paidLedgerSink);
                paidLedgerSink=null;
                long faults=Game.Runtime.SkirmishProductionService.ProductionLedgerObservationFaults-paidLedgerFaultStart;
                try { Record("NativeProductionLedgerClosed",new JObject { ["ledger_schema"]=1,
                    ["scope"]="ProductionCommittedOriginsPartial",["transactions"]=paidLedgerCount,["capture_failed"]=paidLedgerFailed,
                    ["observer_faults"]=faults,["sink_owned"]=same,["paid_resources_qualified"]=false,
                    ["uncovered_operations"]=new JArray("Construction","Research","Repair","FabricationExchangeReward","PhysicalFuelHistory") }); }
                catch(Exception error) when (!(error is OutOfMemoryException)) { }
            }

            private Action<Game.Runtime.SkirmishConstructionLedger.Observation> constructionLedgerSink;
            private long constructionLedgerFaultStart;
            private int constructionLedgerCount;
            private bool constructionLedgerFailed;
            private readonly System.Collections.Generic.Dictionary<object,int> constructionOwnerSequences=new System.Collections.Generic.Dictionary<object,int>();
            private void StartConstructionLedger()
            {
                if(Environment.GetEnvironmentVariable("WARLINE_PRODUCTION_RESOURCE_LEDGER")!="1")return;
                if(Request.IsLegacyPreset || Game.Runtime.SkirmishConstructionLedger.Observer!=null)throw new InvalidOperationException("Owned construction ledger unavailable");
                constructionLedgerFaultStart=Game.Runtime.SkirmishConstructionLedger.Faults;
                constructionLedgerSink=ObserveConstructionLedger;
                Record("NativeConstructionLedgerStarted",new JObject { ["ledger_schema"]=1,["scope"]="ConstructionCommittedBoundariesPartial",["paid_resources_qualified"]=false });
                Game.Runtime.SkirmishConstructionLedger.OpenOwned(constructionLedgerSink);
            }

            private void ObserveConstructionLedger(Game.Runtime.SkirmishConstructionLedger.Observation row)
            {
                if(constructionLedgerFailed)return;
                try
                {
                    if(++constructionLedgerCount>4096)throw new InvalidDataException("Construction ledger bounded capture failed");
                    var em=row.After.Manager;var session=row.After.Session;
                    if(session==Entity.Null)throw new InvalidDataException("Actual owned construction session required");
                    var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                    var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                    var identity=SkirmishProductionCanaryRequest.CreateNativeSetupIdentity(setup.CatalogId,setup.DefinitionId,setup.ContentVersion,
                        setup.DifficultyId.ToString(),setup.SizeId.ToString(),setup.Seed,GameLocalization.CurrentLocaleCode,setup.SetupHash,setup.ContentHash);
                    Request.VerifyNativeSetupIdentity(identity);
                    var actor=row.ActorOwner;
                    int ownerSequence=0;
                    if(row.TransactionOwner!=null && !constructionOwnerSequences.TryGetValue(row.TransactionOwner,out ownerSequence))
                    {ownerSequence=constructionOwnerSequences.Count+1;constructionOwnerSequences.Add(row.TransactionOwner,ownerSequence);}
                    Record("NativeConstructionTransaction",new JObject { ["ledger_schema"]=1,["scope"]="ConstructionCommittedBoundariesPartial",
                        ["ledger_sequence"]=constructionLedgerCount,["operation"]=row.Operation,
                        ["native_session_id"]=native.SessionId.ToString(),["native_world_sequence"]=em.WorldUnmanaged.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["native_config_sha256"]=SkirmishAcceptanceCensusCapture.ComputeConfigHash(setup),["native_setup_identity"]=identity,
                        ["before"]=ConstructionPoint(row.Before),["after"]=ConstructionPoint(row.After),
                        ["transaction_owner_sequence"]=ownerSequence,["transaction_id"]=row.TransactionId,["credits_quoted"]=row.CreditsQuoted,["materials_quoted"]=row.MaterialsQuoted,
                        ["prefab_name"]=row.PrefabName,["origin_x"]=row.Origin.x,["origin_y"]=row.Origin.y,["rotated"]=row.Rotated,
                        ["committed_count"]=row.CommittedCount,["expected_count"]=row.ExpectedCount,
                        ["actor"]=PaidConstructionActorJson(row),
                        ["paid_resources_qualified"]=false });
                }
                catch(Exception error)when(!(error is OutOfMemoryException))
                {
                    constructionLedgerFailed=true;
                    try{Record("NativeConstructionLedgerFailure",new JObject { ["exception_type"]=error.GetType().FullName });}
                    catch(Exception failure)when(!(failure is OutOfMemoryException)){}
                }
            }
            private void StopConstructionLedger()
            {
                if(constructionLedgerSink==null)return;
                bool owned=Game.Runtime.SkirmishConstructionLedger.CloseOwned(constructionLedgerSink);
                constructionLedgerSink=null;
                try{Record("NativeConstructionLedgerClosed",new JObject { ["ledger_schema"]=1,["scope"]="ConstructionCommittedBoundariesPartial",
                    ["transactions"]=constructionLedgerCount,["capture_failed"]=constructionLedgerFailed,["sink_owned"]=owned,
                    ["observer_faults"]=Game.Runtime.SkirmishConstructionLedger.Faults-constructionLedgerFaultStart,
                    ["uncovered_operations"]=new JArray("PlayerWallMemberIdentities","AlternateConstructionEntryPoints","RepairEconomyFuelOrigins","WholeAttemptConservation"),
                    ["paid_resources_qualified"]=false });}
                catch(Exception error)when(!(error is OutOfMemoryException)){}
            }

            private ProfileByteJournal profileByteJournal;

            // Observation only. A failed/partial journal never changes normal cleanup.
            internal void RecordProfileBeforeRestore() => RecordProfileStage("ProfileBeforeRestore");
            private void RecordProfileStage(string stage)
            {
                ObserveProfileJournal(
                    () => {
                        if (profileByteJournal == null) profileByteJournal = new ProfileByteJournal(Request);
                        Record(stage, profileByteJournal.Capture());
                    },
                    error => Record("ProfileJournalFailure", new JObject { ["stage"] = stage,
                        ["exception_type"] = error.GetType().FullName }));
            }
            // Missing evidence refuses admission; diagnostic IO cannot interrupt existing cleanup.
            private static void ObserveProfileJournal(Action observe, Action<Exception> recordFailure)
            {
                try { observe(); }
                catch (Exception error) when (!(error is OutOfMemoryException))
                {
                    try { recordFailure(error); }
                    catch (Exception writeError) when (!(writeError is OutOfMemoryException))
                    { Debug.LogWarning("Profile journal evidence unavailable; admission remains missing."); }
                }
            }

            private sealed class ProfileByteJournal
            {
                private readonly string originalRoot, ownedRoot, originalEnvironment, catalog;
                private readonly byte[] buffer = new byte[65536];
                internal ProfileByteJournal(SkirmishProductionCanaryRequest request)
                {
                    originalEnvironment = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
                    originalRoot = EffectiveRoot();
                    ownedRoot = Path.GetFullPath(Path.Combine(request.ArtifactDirectory, "save-profile"));
                    catalog = request.CatalogId;
                }
                private static string EffectiveRoot()
                {
                    string value = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
                    return Path.GetFullPath(string.IsNullOrWhiteSpace(value) ? Application.persistentDataPath : value);
                }
                internal JObject Capture() => new JObject { ["journal_version"] = 3,
                    ["catalog_id"] = catalog, ["original_environment"] = originalEnvironment,
                    ["actual_environment"] = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"),
                    ["actual_effective_root"] = EffectiveRoot(),
                    ["original"] = SnapshotScopedV3(originalRoot, catalog, buffer, true), ["owned"] = SnapshotScoped(ownedRoot, catalog, buffer, false) };
                // Preserve the strict v1 private fixture seam and its exact schema.
                private static JObject Snapshot(string root, string catalog, byte[] buffer) => SnapshotCore(root,catalog,buffer,false,false);
                private static JObject SnapshotScoped(string root,string catalog,byte[] buffer,bool original) => SnapshotCore(root,catalog,buffer,original,true);
                private static JObject SnapshotScopedV3(string root,string catalog,byte[] buffer,bool original) =>
                    original ? SnapshotCore(root,catalog,buffer,true,true,true) : SnapshotScoped(root,catalog,buffer,false);
                // Only measured save rows expand. No additional non-save exclusions.
                private static void AddOriginalCatalogGroups(string root,string catalog,List<string> names)
                {
                    if(!SkirmishMissionDispatch.Canonical(catalog))throw new InvalidDataException("Noncanonical profile catalog");
                    var groups=new SortedSet<string>(StringComparer.Ordinal);int count=0;
                    if(Directory.Exists(root))foreach(string path in Directory.EnumerateFileSystemEntries(root))
                    {
                        if(count++>=32)throw new InvalidDataException("Profile entry bound exceeded");
                        RejectReparse(path);string name=Path.GetFileName(path);
                        if(names.Contains(name)||name=="TestResults.xml"||name=="Unity")continue;
                        var match=System.Text.RegularExpressions.Regex.Match(name,@"^skirmish-(s[0-9]{3})-native[.]json(?:[.](?:tmp|bak|lock))?$");
                        if(!match.Success||!SkirmishMissionDispatch.Canonical(match.Groups[1].Value.ToUpperInvariant())||
                            Directory.Exists(path)||!File.Exists(path))
                            throw new InvalidDataException("Unknown profile file or directory");
                        string id=match.Groups[1].Value;
                        if(id!=catalog.ToLowerInvariant())groups.Add(id);
                    }
                    foreach(string id in groups)foreach(string suffix in new[]{"",".tmp",".bak",".lock"})
                        names.Add("skirmish-"+id+"-native.json"+suffix);
                }
                private static JObject SnapshotCore(string root, string catalog, byte[] buffer,bool allowEngineEntries,bool emitNamespace,bool crossCatalog=false)
                {
                    RejectReparse(root);
                    if (File.Exists(root)) throw new InvalidDataException("Profile root is a file");
                    var names = new List<string>();
                    foreach (string name in new[] { SaveService.ProfileFileName, SaveService.SettingsFileName,
                        SaveService.QuickGameFileName, "skirmish-" + catalog.ToLowerInvariant() + "-native.json" })
                    foreach (string suffix in new[] { "", ".tmp", ".bak", ".lock" }) names.Add(name + suffix);
                    if(crossCatalog)AddOriginalCatalogGroups(root,catalog,names);
                    bool directoryExists = Directory.Exists(root);
                    var entries = EnumerateScopedEntries(root,names,allowEngineEntries,out var excluded,out var allEntries);
                    var rows = new JArray(); long total = 0;
                    foreach (string relative in names)
                    {
                        string path = Path.Combine(root, relative);
                        RejectReparse(path);
                        if (Directory.Exists(path)) throw new InvalidDataException("Profile file is a directory");
                        bool exists = File.Exists(path); long length = 0; string digest = null;
                        if (exists)
                        {
                            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                            using (var hash = System.Security.Cryptography.SHA256.Create())
                            {
                                length = stream.Length;
                                if (length > 8388608 || total + length > 16777216)
                                    throw new InvalidDataException("Profile journal byte bound exceeded");
                                long read = 0; int count;
                                while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                                {
                                    read += count;
                                    if (read > length) throw new InvalidDataException("Profile grew during observation");
                                    hash.TransformBlock(buffer, 0, count, buffer, 0);
                                }
                                if (read != length || stream.Length != length)
                                    throw new InvalidDataException("Profile changed during observation");
                                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                                digest = BitConverter.ToString(hash.Hash).Replace("-", "").ToLowerInvariant();
                            }
                            total += length;
                        }
                        rows.Add(new JObject { ["name"] = relative, ["exists"] = exists,
                            ["length"] = length, ["sha256"] = digest });
                    }
                    var finalEntries = EnumerateScopedEntries(root,names,allowEngineEntries,out var finalExcluded,out var finalAllEntries);
                    RejectReparse(root);
                    if (File.Exists(root) || directoryExists != Directory.Exists(root) ||
                        !JToken.DeepEquals(new JArray(entries), new JArray(finalEntries))||
                        !JToken.DeepEquals(new JArray(allEntries),new JArray(finalAllEntries))||!JToken.DeepEquals(excluded,finalExcluded))
                        throw new InvalidDataException("Profile directory changed during observation");
                    var result=new JObject { ["root"] = root, ["directory_exists"] = directoryExists,
                        ["directory_entries"] = new JArray(entries), ["enumeration_limit"] = 32,
                        ["files"] = rows, ["observed_bytes"] = total };
                    if(emitNamespace)
                    {
                        result["namespace_scope"]=crossCatalog?"OriginalCanonicalCatalogSaveFiles_KnownEngineSiblingsExcluded":allowEngineEntries?"OriginalCurrentCatalogSaveFiles_KnownEngineSiblingsExcluded":"OwnedCurrentCatalogSaveFiles_Exclusive";
                        result["excluded_non_save_entries"]=excluded;result["total_directory_entries"]=new JArray(allEntries);
                    }
                    return result;
                }
                private static List<string> EnumerateKnownEntries(string root,List<string> names) =>
                    EnumerateScopedEntries(root,names,false,out _,out _);
                private static List<string> EnumerateScopedEntries(string root,List<string> names,bool allowEngineEntries,out JArray excluded,out List<string> allEntries)
                {
                    RejectReparse(root);var entries=new List<string>();allEntries=new List<string>();var nonSave=new List<JObject>();
                    if(Directory.Exists(root))
                    foreach(string path in Directory.EnumerateFileSystemEntries(root))
                    {
                        if(allEntries.Count>=32)throw new InvalidDataException("Profile entry bound exceeded");
                        RejectReparse(path);string name=Path.GetFileName(path);
                        if(allEntries.Contains(name))throw new InvalidDataException("Duplicate profile entry");
                        allEntries.Add(name);
                        if(allowEngineEntries&&(name=="TestResults.xml"||name=="Unity"))
                        {
                            bool directory=Directory.Exists(path),file=File.Exists(path);
                            if((name=="Unity"&&(!directory||file))||(name=="TestResults.xml"&&(directory||!file)))
                                throw new InvalidDataException("Known non-save sibling has wrong type");
                            nonSave.Add(new JObject{["name"]=name,["kind"]=directory?"Directory":"File"});
                            continue; // Never recurse, read, hash or mutate engine/test content.
                        }
                        if(!names.Contains(name)||Directory.Exists(path)||!File.Exists(path))
                            throw new InvalidDataException("Unknown profile file or directory");
                        entries.Add(name);
                    }
                    entries.Sort(StringComparer.Ordinal);allEntries.Sort(StringComparer.Ordinal);
                    excluded=new JArray(nonSave.OrderBy(v=>(string)v["name"],StringComparer.Ordinal));return entries;
                }
                private static void RejectReparse(string path)
                {
                    for (string current = Path.GetFullPath(path); !string.IsNullOrEmpty(current);
                        current = Path.GetDirectoryName(current))
                    {
                        FileAttributes attributes;
                        try { attributes = File.GetAttributes(current); }
                        catch (FileNotFoundException) { continue; }
                        catch (DirectoryNotFoundException) { continue; }
                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                            throw new InvalidDataException("Profile journal reparse path refused");
                    }
                }
            }

            internal void Restore()
            {
                if (restored) return;
                ClosePaidObserverGroup(StopConstructionLedger,StopPaidResearchLedger,StopPaidProductionLedger,RestoreCanaryState);
            }

            private void RestoreCanaryState()
            {
                Record("RestoreStarting", new JObject());
                // Preserve any unexpected dirty scene for investigation; never discard.
                EnsureScenesClean();
                EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                RequireProductionRestorableSetup(EditorSceneManager.GetSceneManagerSetup());
                if(!JToken.DeepEquals(ProductionSceneSetupFacts(sceneSetup),ProductionSceneSetupFacts(EditorSceneManager.GetSceneManagerSetup())))
                    throw new InvalidOperationException("Actual scene restore differs from captured nonempty baseline");
                EditorSettings.enterPlayModeOptions = options;
                EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
                var currentKeys = new List<string>();
                foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                    if (((string)entry.Key).StartsWith("WARLINE_", StringComparison.Ordinal)) currentKeys.Add((string)entry.Key);
                foreach (var key in currentKeys) if (!environment.ContainsKey(key)) Environment.SetEnvironmentVariable(key, null);
                foreach (var pair in environment) Environment.SetEnvironmentVariable(pair.Key, pair.Value);
                // Balance exactly the suspension owned by RunEditorJourney. Existing
                // wrapper sees the erased flag and must not release it a second time.
                if (!refreshAlreadyOwned && SessionState.GetBool("Warline.MissionValidation.ResumeRefresh", false))
                {
                    SessionState.EraseBool("Warline.MissionValidation.ResumeRefresh");
                    AssetDatabase.AllowAutoRefresh();
                }
                RecordProfileStage("ProfileAfterRestore");
                Record("ContextRestored", new JObject { ["actual_save_root"] = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"),
                    ["actual_enter_play_options_enabled"] = EditorSettings.enterPlayModeOptionsEnabled,
                    ["actual_enter_play_options"] = (int)EditorSettings.enterPlayModeOptions,
                    ["actual_scene_count"] = EditorSceneManager.GetSceneManagerSetup().Length,
                    ["native_qualification"] = "pending_adapter_canary" });
                restored = true;
            }

            private static void EnsureScenesClean()
            {
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                        throw new InvalidOperationException("Preserve dirty Editor scenes; canary cannot open/restore scenes.");
            }
        }
    }
}
#endif
