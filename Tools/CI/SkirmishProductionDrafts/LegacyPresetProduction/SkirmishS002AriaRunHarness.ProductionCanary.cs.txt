// DRAFT ONLY: outside Assets; requires the narrow harness seams in README.md.
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Configs;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.Skirmish.Contracts;
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
        private static ProductionCanaryContext productionCanary;

        public static void RunProductionJobFromEnvironment()
        {
            if (productionCanary != null || SessionState.GetBool(Key, false) ||
                EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Production canary requires an idle unowned Editor.");
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var request = SkirmishProductionCanaryRequest.Read(root,
                Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_JOB"),
                Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_PRODUCTION_CLAIM"),productionBatch==null,productionBatch?.Matrix);
            var packaged = SkirmishExpansionCatalogConfig.Load();
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
                // One existing normal Menu runner. No LaunchCatalog direct bootstrap.
                RunEditorJourney(request.Locale, request.Manual, false, request.CatalogId, request.Seed);
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
            productionCanary = null;
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
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
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
            productionCanary.Record(kind, facts);
            var request = productionCanary.Request;
            request.VerifyNativeSetupIdentity(identity);
            if (state.CatalogId.ToString() != request.CatalogId || setup.CatalogId != request.CatalogId ||
                setup.DefinitionId != (string)request.Job["definition_id"] || setup.ContentVersion != (int)request.Job["definition_version"] ||
                setup.Seed != request.Seed || setup.SizeId != SkirmishSizeId.Standard || setup.DifficultyId != SkirmishDifficultyId.Regular)
                throw new InvalidDataException("Actual resolved canary identity/setup differs from owned request.");
            productionCanary.CaptureVerifiedIdentity(identity,(string)facts["native_config_sha256"]);
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

        private sealed class ProductionCanaryContext
        {
            internal readonly SkirmishProductionCanaryRequest Request;
            internal bool LegacySavedResultVerified;
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
                Request = request;
                EnsureScenesClean();
                sceneSetup = EditorSceneManager.GetSceneManagerSetup();
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
                request.ReserveAdapterStart();
                using (File.Open(events, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { }
                Record("ContextCaptured", new JObject { ["source_sha256"] = (string)request.Job["candidate_sha256"],
                    ["compiled_dll_manifest_sha256"] = (string)request.Job["build_sha256"], ["scene_count"] = sceneSetup.Length });
            }

            internal void Record(string kind, JObject facts)
            {
                // Envelope prevents facts from replacing identity/sequence ownership.
                var row = new JObject { ["schema_version"] = 1, ["sequence"] = ++sequence,
                    ["job_sha256"] = Request.JobHash, ["attempt_id"] = (string)Request.Claim["attempt_id"],
                    ["worker"] = (string)Request.Claim["worker"], ["frame"] = Time.frameCount,
                    ["utc"] = DateTimeOffset.UtcNow.ToString("O"), ["kind"] = kind,
                    ["facts"] = facts ?? new JObject() };
                File.AppendAllText(events, row.ToString(Formatting.None) + "\n", new UTF8Encoding(false));
                ObserveProductionBatchNativeEvent(kind,facts);
            }

            internal void Restore()
            {
                if (restored) return;
                Record("RestoreStarting", new JObject());
                // Preserve any unexpected dirty scene for investigation; never discard.
                EnsureScenesClean();
                EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
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
