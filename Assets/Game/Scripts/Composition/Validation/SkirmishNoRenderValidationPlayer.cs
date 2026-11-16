#if WARLINE_VALIDATION_PLAYER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Ecs;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;

namespace Game.UI.Runtime
{
    // The actual built Unity player owns every system, job, ECB and source command.
    // This adapter requests launch/consent and observes a bounded smoke; it never updates a World.
    public sealed class SkirmishNoRenderValidationPlayer : MonoBehaviour
    {
        private static bool profilePrepared;
        private string mode, catalog, output;
        private int seed;
        private bool queued, rosterVerified, ariaStarted, done;
        private double wallStarted;
        private float ariaStartedAt, traceDue, simulationStartedElapsed, lastSimulationElapsed;
        private double simulationStartedWall;
        private int simulationStartedSamples;
        private StreamWriter trace;
        private string rosterFacts = "";
        private int errors;
        private InputSettings originalInput, validationInput;
        private bool runInBackgroundBefore;
        private float captureBefore;
        private int targetBefore, vsyncBefore;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void PrepareIsolatedSourceProfile()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-warlineValidationMode") < 0) return;
            string root = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            if (string.IsNullOrWhiteSpace(root)) throw new InvalidOperationException("Isolated validation save root required before scene load");
            var args = ParseArguments(Environment.GetCommandLineArgs());
            string expected = Path.Combine(Path.GetFullPath(args["-warlineValidationOutput"]), "save-profile");
            if (Path.GetFullPath(root) != expected) throw new InvalidOperationException("Save root must belong to current output");
            Directory.CreateDirectory(root);
            if (Directory.EnumerateFileSystemEntries(root).GetEnumerator().MoveNext())
                throw new InvalidOperationException("Fresh isolated source-profile directory required");
            var save = new SaveService(new JsonSaveRepository(root));
            var profile = save.LoadProfile();
            profile.firstLaunchStatus = FirstLaunchProfileState.Completed;
            profile.firstLaunchWatched = true;
            save.SaveProfile(profile);
            profilePrepared = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-warlineValidationMode") < 0) return;
            var go = new GameObject("SkirmishValidationPlayerOnly");
            DontDestroyOnLoad(go);
            go.AddComponent<SkirmishNoRenderValidationPlayer>();
        }
        private void Awake()
        {
            wallStarted = Time.realtimeSinceStartupAsDouble;
            runInBackgroundBefore = Application.runInBackground;
            captureBefore = Time.captureDeltaTime;
            targetBefore = Application.targetFrameRate;
            vsyncBefore = QualitySettings.vSyncCount;
            try
            {
                var args = ParseArguments(Environment.GetCommandLineArgs());
                mode = args["-warlineValidationMode"];
                catalog = args["-warlineValidationCatalog"];
                output = Path.GetFullPath(args["-warlineValidationOutput"]);
                if (!Directory.Exists(output)) throw new InvalidOperationException("Runner-owned fresh output directory required");
                if (File.Exists(Path.Combine(output, "result.json")) || File.Exists(Path.Combine(output, "trace.jsonl")))
                    throw new InvalidOperationException("Refusing to overwrite validation evidence");
                if (!int.TryParse(args["-warlineValidationSeed"], NumberStyles.None, CultureInfo.InvariantCulture, out seed) || seed <= 0)
                    throw new InvalidOperationException("Positive exact seed required");
                if (!profilePrepared) throw new InvalidOperationException("Isolated source-profile setup failed");
                if (mode != "StartupSmoke" || catalog != "S002")
                    throw new NotSupportedException("Only S002 StartupSmoke supported; Mission result/return proof pending");
                if (!Application.isBatchMode || SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                    throw new InvalidOperationException("Dedicated -batchmode -nographics PLAYER required");
                if (Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION") != "1")
                    throw new InvalidOperationException("Explicit validation-build background input authorization required");
                trace = new StreamWriter(new FileStream(Path.Combine(output, "trace.jsonl"), FileMode.CreateNew, FileAccess.Write, FileShare.Read));
                Application.logMessageReceived += ObserveLog;
                originalInput = InputSystem.settings;
                if (originalInput == null) throw new InvalidOperationException("Actual InputSystem settings required");
                validationInput = UnityEngine.Object.Instantiate(originalInput);
                validationInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings = validationInput;
                Application.runInBackground = true;
                Debug.Log("[SkirmishValidationPlayer] phase=Begin mode=" + mode + " buildGuid=" + Application.buildGUID +
                    " engine=" + Application.unityVersion + " graphics=" + SystemInfo.graphicsDeviceType + " screen=" + Screen.width + "x" + Screen.height + " sourceConsent=LogicalValidationGateway");
            }
            catch (Exception error) { Finish(false, error.GetType().Name + ":" + error.Message, ""); }
        }
        private static Dictionary<string, string> ParseArguments(string[] args)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var known = new HashSet<string> { "-warlineValidationMode", "-warlineValidationCatalog", "-warlineValidationSeed", "-warlineValidationOutput" };
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("-warline", StringComparison.Ordinal)) continue;
                if (!known.Contains(args[i]) || i + 1 == args.Length || !result.TryAdd(args[i], args[++i]))
                    throw new InvalidOperationException("Unknown/duplicate/incomplete validation argument");
            }
            foreach (string key in known) if (!result.ContainsKey(key)) throw new InvalidOperationException("Missing " + key);
            return result;
        }
        private void ObserveLog(string condition, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
        }
        private void Update()
        {
            if (done) return;
            try
            {
                if (Time.realtimeSinceStartupAsDouble - wallStarted > 180d) { Finish(false, "boundedWallTimeout", ""); return; }
                if (errors != 0) { Finish(false, "runtimeErrors=" + errors, ""); return; }
                if (SkirmishValidationClock.Failure != null) { Finish(false, SkirmishValidationClock.Failure, ""); return; }
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                var em = world.EntityManager;
                // Fix source-sized steps for the entire native loop, including menu/loading clocks.
                if (!SkirmishValidationClock.Active) SkirmishValidationPlayerLaunchBridge.BeginClock(world);
                if (!queued)
                {
                    if (!SkirmishValidationPlayerLaunchBridge.ShellReady(em)) return;
                    if (!SkirmishValidationPlayerLaunchBridge.QueueS002(em, seed)) throw new InvalidOperationException("Actual packaged S002 queue rejected");
                    queued = true;
                    Debug.Log("[SkirmishValidationPlayer] phase=Queued catalog=S002 seed=" + seed);
                    return;
                }
                if (!SkirmishValidationPlayerLaunchBridge.TryRead(em, out Entity session, out var match)) return;
                if (match.Phase != SkirmishPhase.Playing && match.Phase != SkirmishPhase.Finished)
                {
                    SkirmishValidationPlayerLaunchBridge.AdvanceExistingLaunch(em);
                    if (!SkirmishValidationPlayerLaunchBridge.TryRead(em, out session, out match)) return;
                }
                if (match.StartupFailure != SkirmishStartupFailureCode.None) { Finish(false, "actualStartupFailure=" + match.StartupFailure, ""); return; }
                float elapsed = SkirmishValidationPlayerLaunchBridge.Elapsed(em, session, in match);
                lastSimulationElapsed = elapsed;
                if (match.Phase == SkirmishPhase.Playing && simulationStartedWall == 0d)
                {
                    simulationStartedWall = Time.realtimeSinceStartupAsDouble;
                    simulationStartedElapsed = elapsed;
                    simulationStartedSamples = SkirmishValidationClock.Samples;
                }
                var aria = UiShellRuntimeGateway.ReadAriaPlay();
                if (elapsed >= traceDue)
                {
                    traceDue = elapsed + 1f;
                    trace.WriteLine(JsonUtility.ToJson(new TraceRow { frame = Time.frameCount, elapsed = elapsed,
                        phase = match.Phase.ToString(), outcome = match.Outcome.ToString(), ariaPhase = aria.Phase.ToString(),
                        ariaActions = aria.Actions, wallSeconds = Time.realtimeSinceStartupAsDouble - wallStarted }));
                    trace.Flush();
                }
                if (match.Phase == SkirmishPhase.Finished)
                { Finish(false, "terminalBeforeSmokeProof", match.Outcome.ToString()); return; }
                if (match.Phase != SkirmishPhase.Playing || !SkirmishValidationPlayerLaunchBridge.SimulationActive(em)) return;
                if (!rosterVerified)
                {
                    if (!SkirmishValidationPlayerLaunchBridge.StartupRosterReady(em, session, seed, out rosterFacts)) return;
                    rosterVerified = true;
                    Debug.Log("[SkirmishValidationPlayer] phase=ActualStartup " + rosterFacts);
                }
                if (!ariaStarted)
                {
                    if (elapsed < 4f || !UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.ActiveRoute != UIRoute.Match || shell.IsTransitionRunning ||
                        UiShellRuntimeGateway.ReadAriaPlayCapability() != AriaPlayCapability.BaseAssault) return;
                    if (!UiShellRuntimeGateway.TryStartAriaPlay()) return;
                    ariaStarted = true; ariaStartedAt = elapsed;
                    Debug.Log("[SkirmishValidationPlayer] phase=ActualAriaStarted consent=LogicalValidationGateway");
                    return;
                }
                if (aria.Phase == AriaPlayPhase.Blocked || aria.Phase == AriaPlayPhase.Manual)
                { Finish(false, "actualAriaStopped=" + aria.Phase, ""); return; }
                var audit = world.GetExistingSystemManaged<AriaPlayInputSystem>();
                if (elapsed - ariaStartedAt >= 10f && aria.Actions > 0 && audit != null && audit.CompletedGestures > 0 && audit.AcceptedSamples > 0 &&
                    audit.UnexpectedSamples == 0 && audit.MeasuredHumanInterventions == 0)
                { Finish(true, "actualStartupAndSourceTouchSmoke", ""); return; }
                if (elapsed - ariaStartedAt >= 60f) Finish(false, "sourceAriaGestureProofMissing", "");
            }
            catch (Exception error) { Finish(false, error.GetType().Name + ":" + error.Message, ""); }
        }
        private void Finish(bool passed, string reason, string outcome)
        {
            if (done) return;
            done = true;
            Application.logMessageReceived -= ObserveLog;
            trace?.Flush(); trace?.Dispose(); trace = null;
            try
            {
                if (!string.IsNullOrEmpty(output) && Directory.Exists(output))
                {
                    string path = Path.Combine(output, "result.json");
                    var result = new Result { mode = mode ?? "", catalog = catalog ?? "", seed = seed,
                        status = passed ? "Passed" : "Failed", reason = reason, terminal_outcome = outcome ?? "",
                        build_guid = Application.buildGUID, unity_version = Application.unityVersion, screen_width = Screen.width, screen_height = Screen.height,
                        wall_seconds = Time.realtimeSinceStartupAsDouble - wallStarted, source_startup = rosterFacts,
                        actual_roster_verified = rosterVerified, actual_aria_started = ariaStarted, clock_samples = SkirmishValidationClock.Samples,
                        simulation_seconds = simulationStartedWall > 0 ? lastSimulationElapsed - simulationStartedElapsed : 0,
                        simulation_wall_seconds = simulationStartedWall > 0 ? Time.realtimeSinceStartupAsDouble - simulationStartedWall : 0,
                        load_startup_wall_seconds = simulationStartedWall > 0 ? simulationStartedWall - wallStarted : Time.realtimeSinceStartupAsDouble - wallStarted,
                        step_seconds = 1f / 60f, steps = simulationStartedWall > 0 ? SkirmishValidationClock.Samples - simulationStartedSamples : 0 };
                    result.simulation_speed_ratio = result.simulation_wall_seconds > 0 ? result.simulation_seconds / result.simulation_wall_seconds : 0;
                    using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(file); writer.Write(JsonUtility.ToJson(result, true));
                }
            }
            catch (Exception error) { passed = false; Debug.LogError("[SkirmishValidationPlayer] receiptFailure=" + error); }
            Debug.Log("[SkirmishValidationPlayer] result=" + (passed ? "Passed" : "Failed") + " mode=" + (mode ?? "") + " reason=" + reason + " fullMission=0");
            Application.Quit(passed ? 0 : 2);
        }
        private void OnDestroy()
        {
            Application.logMessageReceived -= ObserveLog; trace?.Dispose();
            Time.captureDeltaTime = captureBefore; Application.targetFrameRate = targetBefore; QualitySettings.vSyncCount = vsyncBefore;
            SkirmishValidationClock.End();
            Application.runInBackground = runInBackgroundBefore;
            if (validationInput != null)
            {
                if (ReferenceEquals(InputSystem.settings, validationInput)) InputSystem.settings = originalInput;
                UnityEngine.Object.Destroy(validationInput);
            }
        }
        [Serializable] private sealed class TraceRow
        {
            public int frame, ariaActions;
            public float elapsed;
            public double wallSeconds;
            public string phase, outcome, ariaPhase;
        }
        [Serializable] private sealed class Result
        {
            public int schema_version = 1;
            public string kind = "SkirmishValidationPlayerResult", mode, catalog, status, reason, terminal_outcome, build_guid, unity_version, source_startup;
            public int seed, clock_samples, screen_width, screen_height;
            public bool full_mission = false, actual_roster_verified, actual_aria_started;
            public double wall_seconds, simulation_seconds, simulation_wall_seconds, simulation_speed_ratio, load_startup_wall_seconds;
            public float step_seconds;
            public int steps;
            public double speed_target_ratio = 10;
            public string speed_scope = "Measured actual native objective-clock progress / actual wall time; uncapped is not proof of acceleration";
            public string authority = "ActualUnityStandaloneSourceWorldDiagnostic";
            public string readiness_scope = "Logical validation consent plus observed source touch samples; no normal visual/player acceptance";
        }
    }
}
#endif
