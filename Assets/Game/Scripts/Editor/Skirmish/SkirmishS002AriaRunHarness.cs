#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Launches expanded S002 Regular Standard and starts ARIA through the shipping touch driver.
    /// Appends a runs.csv row only after the match finishes or the harness aborts. Never stamps Victory.
    /// </summary>
    [InitializeOnLoad]
    public static partial class SkirmishS002AriaRunHarness
    {
        private const string Key = "Warline.S002.AriaRun";
        private const string QuitPendingKey = "Warline.Skirmish.ValidationQuitPending";
        private const string QuitCodeKey = "Warline.Skirmish.ValidationQuitCode";
        private const string CandidateHashKey = "Warline.Skirmish.CandidateSourceHash";
        private const string CatalogKey = "Warline.Skirmish.AriaCatalog";
        private const string SeedKey = "Warline.S002.AriaSeed";
        private const string LocaleKey = "Warline.S002.AriaLocale";
        private const string StartedKey = "Warline.S002.AriaStarted";
        private const string NormalKey = "Warline.S002.AriaNormal";
        private const string PlayingSinceKey = "Warline.S002.AriaPlayingSince";
        private const string BackgroundPreviousKey = "Warline.S002.AriaBackgroundPrevious";
        private const string WatchCaptureKey = "Warline.S002.AriaWatchCaptured";
        private const string RegistryPath = "Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset";
        private const double WallClockBudgetSeconds = 1500d;

        private static UnityEngine.InputSystem.InputSettings previousInputSettings, validationInputSettings;
        private static bool previousRunInBackground;

        private static void ConfigureBackgroundInput()
        {
            if (validationInputSettings != null || Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION") != "1") return;
            previousInputSettings = UnityEngine.InputSystem.InputSystem.settings;
            validationInputSettings = UnityEngine.Object.Instantiate(previousInputSettings);
            validationInputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            validationInputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            UnityEngine.InputSystem.InputSystem.settings = validationInputSettings;
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
        }

        private static void RestoreBackgroundInput()
        {
            if (validationInputSettings == null) return;
            UnityEngine.InputSystem.InputSystem.settings = previousInputSettings;
            Application.runInBackground = previousRunInBackground;
            UnityEngine.Object.Destroy(validationInputSettings);
            validationInputSettings = previousInputSettings = null;
        }

        private static int stage;
        private static double next;
        private static double deadline;
        private static double playingSince;
        private static bool normalSpeed = true;
        private static SkirmishS002NormalSpeedLatch speedLatch = SkirmishS002NormalSpeedLatch.Start();
        private static int seed = SkirmishAcceptanceCensusCapture.FirstVisitSeed;
        private static string locale = GameLocalization.EnglishLocaleCode;
        private static string catalogId = SkirmishAcceptanceCensusCapture.CatalogId;

        static SkirmishS002AriaRunHarness()
        {
            AriaTouchInputUiSystemHelper.Evidence += RecordTouchEvidence;
            AriaCommandEvidence.Recorded += RecordCommandEvidence;
            if (SessionState.GetBool(QuitPendingKey, false))
                EditorApplication.update += FinishQuitAfterPlayMode;
            if (SessionState.GetBool(Key, false))
            {
                seed = SessionState.GetInt(SeedKey, SkirmishAcceptanceCensusCapture.FirstVisitSeed);
                locale = SessionState.GetString(LocaleKey, GameLocalization.EnglishLocaleCode);
                catalogId = SessionState.GetString(CatalogKey, SkirmishAcceptanceCensusCapture.CatalogId);
                speedLatch = SkirmishS002NormalSpeedLatch.Start();
                speedLatch.Normal = SessionState.GetBool(NormalKey, true);
                normalSpeed = speedLatch.Normal;
                playingSince = SessionState.GetFloat(PlayingSinceKey, 0f);
                EditorApplication.update += Tick;
            }
        }

        private static void RecordCommandEvidence(string operation, uint receipt, bool verified)
        {
            if (!SessionState.GetBool(Key, false)) return;
            File.AppendAllText(LiveTracePath() + ".commands.jsonl", JsonUtility.ToJson(new CommandEvidence
            { frame = Time.frameCount, operation = operation, receipt = receipt, verified = verified }) + "\n");
        }
        [Serializable]
        private struct CommandEvidence { public int frame; public string operation; public uint receipt; public bool verified; }

        private static void RecordTouchEvidence(AriaTouchInputUiSystemHelper driver, string kind, Vector2 position)
        {
            if (!SessionState.GetBool(Key, false) || stage < 3) return;
            File.AppendAllText(LiveTracePath() + ".touch.jsonl", JsonUtility.ToJson(new TouchEvidence
            {
                frame = Time.frameCount, time = Time.unscaledTime, kind = kind, device = driver.DeviceId,
                generation = driver.Generation, dispatched = driver.DispatchedGestures,
                completed = driver.CompletedGestures, samples = driver.AcceptedSamples,
                unexpected = driver.UnexpectedSamples, interventions = driver.PhysicalInterventions,
                position = position
            }) + "\n");
        }

        [Serializable]
        private struct TouchEvidence
        {
            public int frame, device;
            public float time;
            public string kind;
            public uint generation, dispatched, completed, samples, unexpected, interventions;
            public Vector2 position;
        }

        [MenuItem("Tools/Warline/Skirmish/Launch S002 ARIA Watch 104731 en")]
        public static void Launch104731En() => Launch(104731, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S002 ARIA Watch 104731 fa-IR")]
        public static void Launch104731Fa() => Launch(104731, GameLocalization.PersianLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S002 ARIA Watch 130365 en")]
        public static void Launch130365En() => Launch(130365, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S002 ARIA Watch 130365 fa-IR")]
        public static void Launch130365Fa() => Launch(130365, GameLocalization.PersianLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S002 ARIA Watch 155923 en")]
        public static void Launch155923En() => Launch(155923, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S002 ARIA Watch 155923 fa-IR")]
        public static void Launch155923Fa() => Launch(155923, GameLocalization.PersianLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S003 ARIA Watch 104732 en")]
        public static void LaunchS003104732En() =>
            LaunchCatalog(SkirmishAcceptanceCensusCapture.S003CatalogId, 104732, GameLocalization.EnglishLocaleCode);

        /// <summary>
        /// Wrapper entry for mission 5 (S003 Regular Standard). Keeps the Editor
        /// alive for the match, allows the touch driver while the window is in
        /// the background, and quits after the row is written. WARLINE_SKIRMISH_CATALOG
        /// and WARLINE_S002_SEED override the S003 / 104732 default. Do not pass
        /// -quit; that returns before play mode.
        /// </summary>
        public static void RunFocusedAriaAndExit()
        {
            SessionState.SetString(BackgroundPreviousKey,
                Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION") ?? "");
            Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION", "1");
            Environment.SetEnvironmentVariable("WARLINE_ARIA_QUIT", "1");
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG")))
                Environment.SetEnvironmentVariable(
                    "WARLINE_SKIRMISH_CATALOG",
                    SkirmishAcceptanceCensusCapture.S003CatalogId);
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_S002_SEED")))
            {
                string catalog = Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG");
                int defaultSeed = catalog == SkirmishAcceptanceCensusCapture.S003CatalogId
                    ? 104732
                    : SkirmishAcceptanceCensusCapture.FirstVisitSeed;
                Environment.SetEnvironmentVariable(
                    "WARLINE_S002_SEED",
                    defaultSeed.ToString(CultureInfo.InvariantCulture));
            }

            LaunchFromEnvironment();
        }

        public static void RunS003AriaInputDiagnosticAndExit()
        {
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_INPUT_DIAGNOSTIC", "1");
            RunFocusedAriaAndExit();
        }

        // Startup diagnostics deliberately produce no ARIA acceptance row.
        private static int nativeReviewStage;
        private static bool nativeReviewSizeApplied;
        public static void RunS003NativeReviewAndExit()
        {
            Environment.SetEnvironmentVariable("WARLINE_S003_NATIVE_REVIEW", "1");
            nativeReviewStage = 0;
            nativeReviewSizeApplied = false;
            RunS003StartupAndExit();
        }

        private static bool CaptureNativeReview(double now)
        {
            int width = int.TryParse(Environment.GetEnvironmentVariable("WARLINE_REVIEW_WIDTH"), out int requestedWidth) ? requestedWidth : 1920;
            int height = int.TryParse(Environment.GetEnvironmentVariable("WARLINE_REVIEW_HEIGHT"), out int requestedHeight) ? requestedHeight : 1080;
            if (!nativeReviewSizeApplied)
            {
                MainMenuV3PrefabBuilder.SetGameViewResolution(width, height);
                nativeReviewSizeApplied = true;
                next = now + 2d;
                return false;
            }
            string prefix = "/private/tmp/s003-native-" + GameLocalization.CurrentLocaleCode + "-" + width + "x" + height;
            if (nativeReviewStage == 0)
            {
                foreach (var mapView in UnityEngine.Object.FindObjectsByType<MatchHudMinimapView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    var mapRect = mapView.transform as RectTransform;
                    var corners = new Vector3[4];
                    mapRect?.GetWorldCorners(corners);
                    Debug.Log("[SkirmishNativeOpening] minimap=" + mapView.name + " parent=" + mapView.transform.parent?.name +
                        " anchors=" + mapRect?.anchorMin + "/" + mapRect?.anchorMax +
                        " position=" + mapRect?.anchoredPosition + " worldCorners=" + corners[0] + "/" + corners[2]);
                }
                // Screen.width inside EditorApplication.update can describe the
                // Editor window. Validate the written PNG, which is the real render.
                ScreenCapture.CaptureScreenshot(prefix + "-opening.png");
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                if (SkirmishLaunchProjection.TryGet(em, out _, out var match) &&
                    em.Exists(match.PlayerMainBase) && Camera.main != null)
                {
                    var basePosition = em.GetComponentData<LocalTransform>(match.PlayerMainBase).Position;
                    Debug.Log("[SkirmishNativeOpening] baseWorld=" + basePosition +
                        " baseViewport=" + Camera.main.WorldToViewportPoint(basePosition) +
                        " camera=" + Camera.main.transform.position);
                }
                nativeReviewStage = 1;
                next = now + 1d;
                if (Environment.GetEnvironmentVariable("WARLINE_PAGINATION_OPENING_ONLY") == "1")
                {
                    if (Environment.GetEnvironmentVariable("WARLINE_PAGINATION_SELECTED_CAPTURE") == "1")
                    {
                        nativeReviewStage = 90;
                        return false;
                    }
                    Debug.Log("[SkirmishSquadPaginationOpening] result=Captured input=UiRequest visualReview=Pending path=" + prefix + "-opening.png");
                    return true;
                }
                return false;
            }
            if (nativeReviewStage == 90)
            {
                if (!UiShellRuntimeGateway.TrySelectExpandedPresentedSlot(0)) return false;
                nativeReviewStage = 91;
                next = now + 2d;
                return false;
            }
            if (nativeReviewStage == 91)
            {
                ScreenCapture.CaptureScreenshot(prefix + "-selected-fixture.png");
                Debug.Log("[SkirmishSquadPaginationSelected] result=Captured input=FixtureDirectSelection visualReview=Pending path=" + prefix + "-selected-fixture.png");
                return true;
            }
            if (nativeReviewStage == 1)
            {
                if (!UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.Build)) return false;
                nativeReviewStage = 2;
                next = now + 2d;
                return false;
            }
            if (nativeReviewStage == 2)
            {
                var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();
                if (drawer == null || !drawer.IsOpen || drawer.ReadinessButton == null ||
                    !drawer.ReadinessButton.gameObject.activeInHierarchy) return false;
                ScreenCapture.CaptureScreenshot(prefix + "-build.png");
                nativeReviewStage = 3;
                next = now + 1d;
                return false;
            }
            foreach (string suffix in new[] { "-opening.png", "-build.png" })
            {
                string path = prefix + suffix;
                if (!File.Exists(path)) return false;
                byte[] png = File.ReadAllBytes(path);
                if (png.Length < 24 || png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71)
                    throw new InvalidOperationException("Native capture is not a PNG: " + path);
                int actualWidth = png[16] << 24 | png[17] << 16 | png[18] << 8 | png[19];
                int actualHeight = png[20] << 24 | png[21] << 16 | png[22] << 8 | png[23];
                if (actualWidth != width || actualHeight != height)
                    throw new InvalidOperationException($"Native PNG size mismatch: {actualWidth}x{actualHeight}, expected {width}x{height}.");
            }
            Debug.Log("[SkirmishS003NativeReview] result=Captured input=UiRequest visualReview=Pending pngDimensions=Verified prefix=" + prefix);
            return true;
        }

        public static void RunS003StartupAndExit()
        {
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", "1");
            RunFocusedAriaAndExit();
        }

        public static void LaunchFromEnvironment()
        {
            string seedText = Environment.GetEnvironmentVariable("WARLINE_S002_SEED");
            string localeText = Environment.GetEnvironmentVariable("WARLINE_S002_LOCALE");
            string catalogText = Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG");
            if (!int.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                parsed = SkirmishAcceptanceCensusCapture.FirstVisitSeed;
            if (string.IsNullOrEmpty(localeText))
                localeText = GameLocalization.EnglishLocaleCode;
            if (string.IsNullOrEmpty(catalogText))
                catalogText = SkirmishAcceptanceCensusCapture.CatalogId;
            LaunchCatalog(catalogText, parsed, localeText);
        }

        public static void Launch(int requestedSeed, string requestedLocale)
        {
            LaunchCatalog(SkirmishAcceptanceCensusCapture.CatalogId, requestedSeed, requestedLocale);
        }

        public static void LaunchCatalog(string requestedCatalog, int requestedSeed, string requestedLocale)
        {
            string error;
            bool accepted = requestedCatalog == SkirmishAcceptanceCensusCapture.S003CatalogId
                ? SkirmishAriaAcceptancePayload.TryCreateFirstVisitS003(requestedSeed, requestedLocale, out _, out error)
                : SkirmishAriaAcceptancePayload.TryCreateFirstVisitS002(requestedSeed, requestedLocale, out _, out error);
            if (!accepted)
                throw new InvalidOperationException(error);
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit play mode before launching the ARIA watch.");

            catalogId = string.IsNullOrEmpty(requestedCatalog)
                ? SkirmishAcceptanceCensusCapture.CatalogId
                : requestedCatalog;
            seed = requestedSeed;
            locale = requestedLocale;
            SessionState.SetString(CandidateHashKey,
                SkirmishCandidateSourceHash.Compute(Path.GetFullPath(Path.Combine(Application.dataPath, ".."))));
            stage = 0;
            terminalSnapshotValid = terminalProbeActive = false;
            terminalInterventions = terminalUnexpectedSamples = 0;
            next = 0d;
            deadline = 0d;
            playingSince = 0d;
            speedLatch = SkirmishS002NormalSpeedLatch.Start();
            normalSpeed = true;
            SessionState.SetString(Key + ".artifactId", Guid.NewGuid().ToString("N"));
            SessionState.SetBool(Key, true);
            SessionState.SetBool(NormalKey, true);
            SessionState.SetInt(SeedKey, seed);
            SessionState.SetString(LocaleKey, locale);
            SessionState.SetString(CatalogKey, catalogId);
            SessionState.SetString(StartedKey, string.Empty);
            SessionState.SetFloat(PlayingSinceKey, 0f);
            SessionState.SetBool(WatchCaptureKey, false);
            Directory.CreateDirectory(EvidenceDirectory());

            string profileRoot = ValidationProfileRoot();
            SessionState.SetString("Warline.AriaPlayValidation",
                Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT") ?? "");
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT", profileRoot);
            var save = new SaveService(new JsonSaveRepository(profileRoot));
            var profile = save.LoadProfile();
            profile.firstLaunchStatus = FirstLaunchProfileState.Completed;
            profile.firstLaunchWatched = true;
            save.SaveProfile(profile);

            EditorApplication.update -= Tick;
            // A unique trace per launch preserves earlier candidates and failures.
            // SessionState keeps the identity across play-mode domain reloads.
            if (Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY") != "1")
                ResetLiveTrace(LiveTracePath());
            EditorApplication.update += Tick;
            int width = int.TryParse(Environment.GetEnvironmentVariable("WARLINE_REVIEW_WIDTH"), out int requestedWidth)
                ? Mathf.Clamp(requestedWidth, 1280, 2400) : 1920;
            int height = int.TryParse(Environment.GetEnvironmentVariable("WARLINE_REVIEW_HEIGHT"), out int requestedHeight)
                ? Mathf.Clamp(requestedHeight, 720, 1440) : 1080;
            MainMenuV3PrefabBuilder.SetGameViewResolution(width, height);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            EditorApplication.EnterPlaymode();
        }

        public static string EvidenceDirectory()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string relative = catalogId == SkirmishAcceptanceCensusCapture.S003CatalogId
                ? SkirmishAcceptanceScaffold.S003RelativeReportEvidenceDirectory
                : SkirmishAcceptanceScaffold.RelativeReportEvidenceDirectory;
            return Path.Combine(root, relative);
        }

        private static string RunsCsvPath(string root)
        {
            return Path.GetFullPath(Path.Combine(root, CandidateEvidenceRelative(), SkirmishAcceptanceScaffold.RunsFileName));
        }

        private static string CandidateEvidenceRelative()
        {
            string hash = SessionState.GetString(CandidateHashKey, "");
            if (hash.Length != 64) throw new InvalidOperationException("Missing candidate source fingerprint.");
            string report = catalogId == SkirmishAcceptanceCensusCapture.S003CatalogId
                ? SkirmishAcceptanceScaffold.S003RelativeReportEvidenceDirectory
                : SkirmishAcceptanceScaffold.RelativeReportEvidenceDirectory;
            return report + "/candidates/" + hash;
        }

        private static string ValidationProfileRoot()
        {
            return Path.Combine(Path.GetTempPath(), "aria-play-validation-profile");
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
                return;
            if (terminalProbeActive)
            {
                if (EditorApplication.timeSinceStartup > terminalDeadline) EndTerminalProbe(false, "playerFrameTimeout");
                return;
            }
            // The result screen intentionally freezes timeScale before the next
            // scheduled harness tick. Audit only Playing frames, never the
            // frozen terminal UI or startup before simulation begins.
            World speedWorld = World.DefaultGameObjectInjectionWorld;
            if (speedWorld != null && speedWorld.IsCreated &&
                SkirmishLaunchProjection.TryGet(speedWorld.EntityManager, out _, out var speedMatch) &&
                speedMatch.Phase == SkirmishPhase.Playing)
            {
                speedLatch = SkirmishS002AriaRunLog.ObserveTimeScale(speedLatch, Time.timeScale);
                if (!speedLatch.Normal)
                    SessionState.SetBool(NormalKey, false);
                normalSpeed = speedLatch.Normal;
            }

            double now = EditorApplication.timeSinceStartup;
            if (deadline == 0d)
                deadline = now + (Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY") == "1"
                    ? 120d : WallClockBudgetSeconds);
            if (now > deadline)
            {
                Finish(abort: true, abortReason: "timeout");
                return;
            }

            if (now < next)
                return;

            if (helipadProbeStage > 0)
                return;

            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated)
                    return;
                EntityManager em = world.EntityManager;
                using (var expanded = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)))
                {
                    if (expanded.CalculateEntityCount() == 1)
                    {
                        var state = expanded.GetSingleton<SkirmishExpandedSessionComponent>();
                        if (state.FailureCode != SkirmishReasonCode.None)
                        {
                            Finish(true, "expandedStartupFailure=" + state.FailureCode);
                            return;
                        }
                    }
                }
                if (stage == 0)
                {
                    GameLocalization.SetLocale(locale, false);
                    if (!SkirmishLaunchProjection.IsShellReadyToEnterMatch(em))
                    {
                        next = now + 0.5d;
                        return;
                    }

                    if (!TryQueueExpanded(em))
                        return;
                    SkirmishAriaAcceptancePayload payload;
                    bool selected = catalogId == SkirmishAcceptanceCensusCapture.S003CatalogId
                        ? SkirmishAriaAcceptancePayload.TryCreateFirstVisitS003(seed, locale, out payload, out _)
                        : SkirmishAriaAcceptancePayload.TryCreateFirstVisitS002(seed, locale, out payload, out _);
                    if (!selected)
                        return;
                    Debug.Log("[SkirmishS002AriaRun] selected " + payload.FormatSelectedConfiguration());
                    if (TryReadCensus(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), out SkirmishAcceptanceCensus census))
                        Debug.Log(SkirmishAcceptanceCensusCapture.FormatLog(census));
                    stage = 1;
                    next = now + 2d;
                    return;
                }

                if (!SkirmishLaunchProjection.TryGet(em, out Entity session, out SkirmishMatchState match))
                    return;
                if (stage <= 1)
                {
                    SkirmishLaunchProjection.TryEnterMatch(em);
                    SkirmishLaunchProjection.TryBeginGameplayIfLoaded();
                    SkirmishLaunchProjection.TryRequestPlay(em);
                    SkirmishLaunchProjection.TryArmExpandedSimulation(em);
                }
                if (match.StartupFailure != SkirmishStartupFailureCode.None)
                {
                    Finish(abort: true, abortReason: "startupFailure=" + match.StartupFailure);
                    return;
                }

                float elapsed = SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match);
                bool simulationActive = SkirmishLaunchProjection.IsSimulationActive(em);

                if (stage == 1)
                {
                    if (match.Phase != SkirmishPhase.Playing)
                    {
                        next = now + 0.5d;
                        return;
                    }

                    if (playingSince <= 0d)
                    {
                        playingSince = now;
                        SessionState.SetFloat(PlayingSinceKey, (float)playingSince);
                    }

                    // Expanded spawn marks Playing before LoadingGate arms simulation.
                    // Wait for SimulationActive so ARIA is not started against a frozen clock.
                    if (!simulationActive)
                    {
                        if (SkirmishS002AriaRunLog.IsSimulationNotAdvancing(
                                true, false, elapsed, now - playingSince))
                        {
                            Finish(abort: true, abortReason: SkirmishS002AriaRunLog.AbortReasonSimulationNotAdvancing);
                            return;
                        }

                        next = now + 0.5d;
                        return;
                    }

                    ConfigureBackgroundInput();
                    SessionState.SetString(StartedKey, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
                    if (Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY") != "1") AriaCommandEvidence.Begin();
                    stage = 2;
                    next = now + 1d;
                    return;
                }

                if (playingSince > 0d &&
                    SkirmishS002AriaRunLog.IsSimulationNotAdvancing(
                        match.Phase == SkirmishPhase.Playing,
                        simulationActive,
                        elapsed,
                        now - playingSince))
                {
                    Finish(abort: true, abortReason: SkirmishS002AriaRunLog.AbortReasonSimulationNotAdvancing);
                    return;
                }

                if (stage == 2)
                {
                    if (elapsed < 4f || !UiShellRuntimeGateway.TryReadShellState(out var readyShell) ||
                        readyShell.CurrentMode != UiShellMode.MatchHud || readyShell.IsTransitionRunning ||
                        readyShell.Phase is not (UiShellTransitionPhase.MatchHudReady or UiShellTransitionPhase.Idle))
                    { next = now + .5d; return; }
                    if (Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY") == "1")
                    {
                        if (elapsed < 2f) { next = now + 0.5d; return; }
                        if (nativeProductionStage > 0)
                        {
                            return;
                        }
                        using var buildings = em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo), typeof(SkirmishAttemptOwnedComponent));
                        using var ready = em.CreateEntityQuery(typeof(SkirmishSharedBuildingsReady));
                        using var banks = em.CreateEntityQuery(typeof(FactionTacticalMaterialsComponent));
                        using var units = em.CreateEntityQuery(typeof(SkirmishUnitRoleComponent), typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishVisualSpawnedComponent));
                        var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                        int expectedUnits = 0;
                        foreach (var force in setup.Forces) expectedUnits += math.max(1, force.Quantity);
                        int playerBanks = 0, enemyBanks = 0;
                        int claimedScenery = 0, mismatchedOwners = 0;
                        using var boundBuildings = buildings.ToEntityArray(Allocator.Temp);
                        foreach (var building in boundBuildings)
                        {
                            if (em.HasComponent<OperationMapBuildingComponent>(building)) claimedScenery++;
                            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(building).FactionId;
                            if (em.GetComponentData<RuntimeBuildingCombatInfo>(building).OwnerFactionId != owner)
                                mismatchedOwners++;
                        }
                        using var sharedUnits = em.CreateEntityQuery(typeof(SkirmishUnitRoleComponent), typeof(SkirmishSharedActorTag), typeof(CombatTargetPolicy));
                        using var suppressedUnits = em.CreateEntityQuery(typeof(SkirmishSharedActorTag), typeof(CampaignMissionCombatSuppressedTag));
                        using var controls = em.CreateEntityQuery(typeof(FactionControlEntry));
                        int genericControllers = 0;
                        using var controlEntities = controls.ToEntityArray(Allocator.Temp);
                        foreach (var controlEntity in controlEntities)
                            foreach (var control in em.GetBuffer<FactionControlEntry>(controlEntity))
                                if ((control.FactionId == setup.PlayerFaction || control.FactionId == setup.EnemyFaction) && control.AIControlled != 0)
                                    genericControllers++;
                        using var bankValues = banks.ToComponentDataArray<FactionTacticalMaterialsComponent>(Allocator.Temp);
                        foreach (var bank in bankValues)
                        {
                            if (bank.FactionId == setup.PlayerFaction) playerBanks++;
                            if (bank.FactionId == setup.EnemyFaction) enemyBanks++;
                        }
                        bool supplyInitialized = em.HasComponent<SkirmishSharedSupplyInitialized>(session);
                        int playerOil = SkirmishStartingSupplyService.ReadOil(em, session, 1);
                        int playerFuel = SkirmishStartingSupplyService.ReadFuel(em, session, 1);
                        Debug.Log("[SkirmishS003Startup] physicalSupply=" + supplyInitialized +
                            " oil=" + playerOil + " usableFuel=" + playerFuel);
                        Debug.Log("[SkirmishS003Startup] sharedBuildings=" + buildings.CalculateEntityCount()
                            + " expectedBuildings=" + setup.Structures.Length + " units=" + units.CalculateEntityCount()
                            + " expectedUnits=" + expectedUnits + " readySessions=" + ready.CalculateEntityCount()
                            + " materialBanks=" + banks.CalculateEntityCount()
                            + " sharedCombatUnits=" + sharedUnits.CalculateEntityCount()
                            + " suppressedSharedUnits=" + suppressedUnits.CalculateEntityCount()
                            + " genericStrategyControllers=" + genericControllers
                            + " claimedScenery=" + claimedScenery + " mismatchedBuildingOwners=" + mismatchedOwners);
                        bool invalid = ready.CalculateEntityCount() != 1 || buildings.CalculateEntityCount() != setup.Structures.Length ||
                            units.CalculateEntityCount() != expectedUnits || playerBanks != 1 || enemyBanks != 1 ||
                            sharedUnits.CalculateEntityCount() != expectedUnits || suppressedUnits.CalculateEntityCount() != 0 ||
                            controls.IsEmptyIgnoreFilter || genericControllers != 0 || claimedScenery != 0 || mismatchedOwners != 0 ||
                            !supplyInitialized || (setup.OilEach > 0 && playerOil <= 0) || (setup.UsableFuelEach > 0 && playerFuel <= 0);
                        if (!invalid && Environment.GetEnvironmentVariable("WARLINE_S003_PRODUCTION_PROBE") == "1")
                        {
                            nativeProductionStage = 1;
                            StartNativeProductionProbe(session);
                            return;
                        }
                        if (!invalid && Environment.GetEnvironmentVariable("WARLINE_S003_HELIPAD_PROBE") == "1")
                        {
                            StartNativeHelipadProbe(session);
                            return;
                        }
                        if (!invalid && Environment.GetEnvironmentVariable("WARLINE_S003_NATIVE_REVIEW") == "1" &&
                            !CaptureNativeReview(now)) return;
                        Finish(invalid, "sharedStartupDiagnostic");
                        return;
                    }
                    if (!UiShellRuntimeGateway.TryStartAriaPlay())
                    {
                        next = now + 0.5d;
                        return;
                    }

                    ScreenCapture.CaptureScreenshot(LiveTracePath() + ".opening.png");
                    Debug.Log("[SkirmishS002AriaRun] watchStarted=1 seed=" + seed.ToString(CultureInfo.InvariantCulture) + " locale=" + locale);
                    stage = 3;
                    next = now + 1d;
                    return;
                }

                AppendTrace(em, session, in match, simulationActive, elapsed);
                if (elapsed >= 10f && !SessionState.GetBool(WatchCaptureKey, false))
                {
                    SessionState.SetBool(WatchCaptureKey, true);
                    ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDirectory(), "s002-aria-watch-controls.png"));
                }
                if (match.Phase == SkirmishPhase.Finished)
                {
                    StartTerminalProbe(em, session, match, elapsed);
                    return;
                }

                if (elapsed >= 180f && Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_INPUT_DIAGNOSTIC") == "1")
                {
                    Finish(abort: false, abortReason: "diagnosticWindowComplete");
                    return;
                }
                // A stopped/blocked driver is failed evidence. The harness must
                // never silently consent again or override a player's Stop.
                AriaPlayModel aria = UiShellRuntimeGateway.ReadAriaPlay();
                if (aria.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked)
                {
                    Finish(abort: true, abortReason: "ariaStopped:" + aria.Phase);
                    return;
                }
                next = now + 2d;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(abort: true, abortReason: "exception");
            }
        }

        private static bool TryQueueExpanded(EntityManager em)
        {
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out List<SkirmishSetupMatrixRow> matrix, out string error))
                throw new InvalidOperationException(error);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            bool airMobile = catalogId == SkirmishAcceptanceCensusCapture.S003CatalogId;
            SkirmishScenarioDefinitionConfig definition = airMobile ? authored.DefinitionS003 : authored.DefinitionS002;
            string queuedCatalog = airMobile
                ? SkirmishAcceptanceCensusCapture.S003CatalogId
                : SkirmishAcceptanceCensusCapture.CatalogId;
            var manifest = new SkirmishContentManifest
            {
                RequiredFeatureIds = definition.RequiredFeatureIds
            };
            UnitPrefabRegistryAuthoringConfig registry =
                AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(RegistryPath);
            if (SkirmishLaunchProjection.TryGet(em, out _, out _))
                return false;
            if (!SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                    em,
                    queuedCatalog,
                    SkirmishDifficultyId.Regular,
                    SkirmishSizeId.Standard,
                    seed,
                    authored,
                    matrix,
                    manifest,
                    out SkirmishResolvedSetup setup,
                    out _,
                    out List<SkirmishCompileReason> reasons,
                    registry))
                throw new InvalidOperationException(reasons == null || reasons.Count == 0 ? queuedCatalog + " queue failed" : reasons[0].ToString());

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "[SkirmishS002AriaRun] queued catalog={0} seed={1} hash={2:X8} acceptance=Pending normal_speed=1",
                setup.CatalogId,
                setup.Seed,
                setup.SetupHash));
            return true;
        }

        private static void AppendTrace(
            EntityManager em,
            Entity session,
            in SkirmishMatchState match,
            bool simulationActive,
            float elapsed)
        {
            Directory.CreateDirectory(EvidenceDirectory());
            float clockElapsed = elapsed;
            byte clockPaused = 0;
            if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
            {
                SkirmishObjectiveClockComponent clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
                clockElapsed = clock.ElapsedSeconds;
                clockPaused = clock.Paused;
            }

            int playerBaseHp = DesignatedBaseHp(em, session, 1);
            int enemyBaseHp = DesignatedBaseHp(em, session, 2);
            int requests = 0;
            int following = 0;
            int playerUnits = 0;
            float leadX = -1f;
            float leadZ = -1f;
            using (EntityQuery units = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent), typeof(LocalTransform)))
            using (NativeArray<Entity> entities = units.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity unit = entities[i];
                    if (em.GetComponentData<SkirmishAttemptOwnedComponent>(unit).FactionId != 1)
                        continue;
                    playerUnits++;
                    if (em.HasComponent<UnitPathRequest>(unit)) requests++;
                    if (em.HasComponent<UnitPathFollow>(unit)) following++;
                    if (leadX < 0f)
                    {
                        float3 position = em.GetComponentData<LocalTransform>(unit).Position;
                        leadX = position.x;
                        leadZ = position.z;
                    }
                }
            }
            string line = string.Format(
                CultureInfo.InvariantCulture,
                "{{\"elapsed\":{0},\"phase\":\"{1}\",\"outcome\":\"{2}\",\"reason\":\"{3}\",\"simulationActive\":{4},\"clockElapsed\":{5},\"clockPaused\":{6},\"playerBaseHp\":{7},\"enemyBaseHp\":{8},\"playerUnits\":{9},\"pathRequests\":{10},\"pathFollowing\":{11},\"leadX\":{12},\"leadZ\":{13}}}\n",
                elapsed.ToString("0.###", CultureInfo.InvariantCulture),
                match.Phase,
                match.Outcome,
                match.Reason,
                simulationActive ? 1 : 0,
                clockElapsed.ToString("0.###", CultureInfo.InvariantCulture),
                clockPaused,
                playerBaseHp,
                enemyBaseHp,
                playerUnits,
                requests,
                following,
                leadX.ToString("0.###", CultureInfo.InvariantCulture),
                leadZ.ToString("0.###", CultureInfo.InvariantCulture));
            File.AppendAllText(LiveTracePath(), line);
            AppendActorDiagnostic(em, elapsed);
            AppendInputDiagnostic(em, elapsed);
        }

        [Serializable]
        private sealed class InputDiagnostic
        {
            public float elapsed;
            public string phase, intent, observation;
            public int actions, attempts, targetId, observedTargetId, stopReason, pageSeen, navigationStage;
            public float progressAt, objectiveProgressAt, openingUntil;
            public bool assaultStarted, assaultIssued;
            public int materials, infantry, page;
            public bool drawer, riflePending, truckCommitted, readiness, pad;
            public AriaTouchTarget recruit;
        }

        private static void AppendInputDiagnostic(EntityManager em, float elapsed)
        {
            using var query = em.CreateEntityQuery(typeof(AriaSkirmishObservationComponent),
                typeof(AriaPlaySessionComponent), typeof(AriaSkirmishPlanComponent), typeof(AriaPlayObservationComponent));
            if (query.CalculateEntityCount() != 1) return;
            Entity boundary = query.GetSingletonEntity();
            var view = em.GetComponentData<AriaSkirmishObservationComponent>(boundary).Value;
            var input = em.GetComponentData<AriaPlaySessionComponent>(boundary);
            var plan = em.GetComponentData<AriaSkirmishPlanComponent>(boundary);
            var target = em.GetComponentData<AriaPlayObservationComponent>(boundary);
            var sample = new InputDiagnostic
            {
                elapsed = elapsed,
                phase = input.Phase.ToString(), intent = plan.Intent.ToString(), observation = target.Kind.ToString(),
                actions = input.Actions, attempts = input.Attempts, targetId = input.TargetId,
                observedTargetId = target.TargetId, stopReason = input.StopReason,
                pageSeen = plan.AssaultPageSeen, navigationStage = plan.MapNavigationStage,
                progressAt = plan.LastProgressAt, objectiveProgressAt = input.LastObjectiveProgressAt,
                openingUntil = plan.OpeningUntil, assaultStarted = plan.AssaultStarted != 0, assaultIssued = plan.AssaultIssued != 0,
                materials = view.OwnMaterials, infantry = view.Infantry, page = view.ExpandedPageIndex,
                drawer = view.DrawerOpen, riflePending = view.RifleRecruitPending,
                truckCommitted = view.LogisticsTruckCommitted, readiness = view.ReadinessEligible,
                pad = view.PadPresent, recruit = view.Recruit
            };
            File.AppendAllText(LiveTracePath() + ".input.jsonl", JsonUtility.ToJson(sample) + "\n");
        }

        [Serializable]
        private sealed class ActorDiagnostic
        {
            public float elapsed;
            public string id, role;
            public byte faction;
            public Vector3 position;
            public float health, speed;
            public int pathIndex = -1, pathLength;
            public bool engaged, stationary, air;
            public bool hauling;
            public int haulSource, haulDestination, haulPhase, haulStatus, haulReason;
            public float cargoOil, cargoFuel;
        }

        private static void AppendActorDiagnostic(EntityManager em, float elapsed)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishUnitRoleComponent), typeof(LocalTransform));
            using var entities = query.ToEntityArray(Allocator.Temp);
            var lines = new System.Text.StringBuilder();
            foreach (var unit in entities)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                var sample = new ActorDiagnostic
                {
                    elapsed = elapsed, id = owned.StableObjectId.ToString(), faction = owned.FactionId,
                    role = em.GetComponentData<SkirmishUnitRoleComponent>(unit).Role.ToString(),
                    position = em.GetComponentData<LocalTransform>(unit).Position,
                    health = em.HasComponent<UnitHealth>(unit) ? em.GetComponentData<UnitHealth>(unit).Current : -1,
                    speed = em.HasComponent<UnitMove>(unit) ? em.GetComponentData<UnitMove>(unit).Speed : -1,
                    pathIndex = em.HasComponent<UnitPathFollow>(unit) ? em.GetComponentData<UnitPathFollow>(unit).PathIndex : -1,
                    pathLength = em.HasComponent<UnitPathRange>(unit) ? em.GetComponentData<UnitPathRange>(unit).Length : 0,
                    engaged = em.HasComponent<EngageTarget>(unit),
                    stationary = em.HasComponent<CampaignMissionStationaryUnitTag>(unit),
                    air = em.HasComponent<UnitAirMovement>(unit)
                };
                sample.hauling = em.HasComponent<UnitResourceHaulOrder>(unit);
                if (sample.hauling)
                {
                    var haul = em.GetComponentData<UnitResourceHaulOrder>(unit);
                    sample.haulSource = haul.SourceBuildingId; sample.haulDestination = haul.DestinationBuildingId;
                    sample.haulPhase = haul.Phase;
                }
                if (em.HasComponent<UnitResourceHaulStatus>(unit))
                {
                    var status = em.GetComponentData<UnitResourceHaulStatus>(unit);
                    sample.haulStatus = status.StatusCode; sample.haulReason = status.ReasonCode;
                }
                if (em.HasComponent<UnitResourceHauler>(unit))
                {
                    var cargo = em.GetComponentData<UnitResourceHauler>(unit);
                    sample.cargoOil = cargo.CargoOilBarrels; sample.cargoFuel = cargo.CargoFuelBarrels;
                }
                lines.AppendLine(JsonUtility.ToJson(sample));
            }
            File.AppendAllText(LiveTracePath() + ".actors.jsonl", lines.ToString());
        }

        private static int DesignatedBaseHp(EntityManager em, Entity session, byte faction)
        {
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return -1;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using EntityQuery query = em.CreateEntityQuery(
                typeof(SkirmishObjectiveRoleComponent),
                typeof(SkirmishAttemptOwnedComponent),
                typeof(UnitHealth));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            SkirmishObjectiveRoleKind role = faction == 1
                ? SkirmishObjectiveRoleKind.PlayerBase
                : SkirmishObjectiveRoleKind.EnemyBase;
            for (int i = 0; i < entities.Length; i++)
            {
                SkirmishAttemptOwnedComponent owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]);
                if (!owned.SessionId.Equals(sessionId) || owned.FactionId != faction)
                    continue;
                if (em.GetComponentData<SkirmishObjectiveRoleComponent>(entities[i]).Role != role)
                    continue;
                return em.GetComponentData<UnitHealth>(entities[i]).Current;
            }

            return -1;
        }

        private static string LiveTracePath()
        {
            string token = string.Equals(locale, GameLocalization.EnglishLocaleCode, StringComparison.Ordinal) ? "en" : "fa";
            string catalogToken = string.IsNullOrEmpty(catalogId) ? "s002" : catalogId.ToLowerInvariant();
            return Path.Combine(
                EvidenceDirectory(),
                string.Format(CultureInfo.InvariantCulture, "{0}-aria-live-{1}-{2}-{3}.jsonl", catalogToken, seed, token,
                    SessionState.GetString(Key + ".artifactId", "unidentified")));
        }

        internal static void ResetLiveTrace(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, string.Empty);
        }

        internal static void CopyLiveTraceToEvidence(string livePath, string evidencePath)
        {
            if (!string.IsNullOrEmpty(livePath) && File.Exists(livePath))
                File.Copy(livePath, evidencePath, true);
            else
                File.WriteAllText(evidencePath, string.Empty);
        }

        private static void Finish(bool abort, string abortReason)
        {
            EditorApplication.update -= Tick;
            RestoreBackgroundInput();
            nativeProductionTouch?.Dispose(); nativeProductionTouch = null;
            terminalProbeActive = false;
            terminalTouch?.Dispose(); terminalTouch = null;
            string currentSourceHash = SkirmishCandidateSourceHash.Compute(Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
            if (currentSourceHash != SessionState.GetString(CandidateHashKey, ""))
            {
                abort = true;
                abortReason = "candidateSourceChangedDuringRun";
            }
            SessionState.SetBool(Key, false);
            SessionState.SetFloat(PlayingSinceKey, 0f);
            string previous = SessionState.GetString("Warline.AriaPlayValidation", "");
            Environment.SetEnvironmentVariable(
                "WARLINE_VALIDATION_SAVE_ROOT",
                string.IsNullOrEmpty(previous) ? null : previous);
            string backgroundPrevious = SessionState.GetString(BackgroundPreviousKey, "");
            Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",
                string.IsNullOrEmpty(backgroundPrevious) ? null : backgroundPrevious);

            if (Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY") == "1")
            {
                Debug.Log("[SkirmishS003Startup] result=" + (abort ? "Failed" : "Passed") + " reason=" + abortReason);
                Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", null);
                EditorApplication.ExitPlaymode();
                RequestQuitIfAsked(abort ? 2 : 0);
                return;
            }

            if (Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_INPUT_DIAGNOSTIC") == "1")
            {
                CopyLiveTraceToEvidence(LiveTracePath(), Path.Combine(EvidenceDirectory(), "s003-input-diagnostic-" + SessionState.GetString(Key + ".artifactId", "unidentified") + ".jsonl"));
                Debug.Log("[SkirmishS003InputDiagnostic] result=" + (abort ? "Failed" : "Captured") +
                    " reason=" + abortReason + " acceptance=Unmeasured");
                UiShellRuntimeGateway.StopAriaPlay();
                EditorApplication.ExitPlaymode();
                RequestQuitIfAsked(abort ? 2 : 0);
                return;
            }

            SkirmishMatchState match = default;
            Entity session = Entity.Null;
            bool haveMatch = false;
            float duration = 0f;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated &&
                SkirmishLaunchProjection.TryGet(world.EntityManager, out session, out match))
            {
                haveMatch = true;
                duration = SkirmishLaunchProjection.ReadMatchElapsedSeconds(world.EntityManager, session, in match);
            }

            if (terminalSnapshotValid)
            {
                haveMatch = true; match = terminalMatch; session = terminalSession; duration = terminalElapsed;
            }

            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string runsPath = RunsCsvPath(root);
            string csv = File.Exists(runsPath) ? File.ReadAllText(runsPath) : SkirmishAcceptanceScaffold.RequiredHeader + "\n";
            if (!SkirmishS002AriaRunLog.TryNextAriaRunId(csv, catalogId, seed, locale, out string runId, out string idError))
            {
                Debug.LogError("[SkirmishS002AriaRun] result=Failed " + idError);
                UiShellRuntimeGateway.StopAriaPlay();
                EditorApplication.ExitPlaymode();
                RequestQuitIfAsked(2);
                return;
            }

            string evidenceName = runId.ToLowerInvariant();
            string evidenceRelative = CandidateEvidenceRelative();
            string traceRelative = evidenceRelative + "/" + evidenceName + ".jsonl";
            string logRelative = evidenceRelative + "/" + evidenceName + "-log.txt";
            string traceAbsolute = Path.Combine(root, traceRelative.Replace('/', Path.DirectorySeparatorChar));
            string logAbsolute = Path.Combine(root, logRelative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(traceAbsolute));
            CopyLiveTraceToEvidence(LiveTracePath(), traceAbsolute);
            foreach (string suffix in new[] { ".input.jsonl", ".actors.jsonl", ".touch.jsonl", ".commands.jsonl" })
                if (File.Exists(LiveTracePath() + suffix))
                    File.Copy(LiveTracePath() + suffix, traceAbsolute + suffix, true);

            double wall = playingSince > 0d ? EditorApplication.timeSinceStartup - playingSince : 0d;
            normalSpeed = SkirmishS002AriaRunLog.FinishNormalSpeed(speedLatch,
                terminalSnapshotValid ? terminalTimeScale : Time.timeScale, duration,
                terminalSnapshotValid ? terminalWall : wall,
                terminalSnapshotValid && terminalMatch.Phase == SkirmishPhase.Finished);
            Debug.Log("[SkirmishNormalSpeed] latch=" + speedLatch.Normal +
                " terminalScale=" + (terminalSnapshotValid ? terminalTimeScale : Time.timeScale) +
                " matchSeconds=" + duration + " wallSeconds=" +
                (terminalSnapshotValid ? terminalWall : wall) + " counted=" + normalSpeed);
            if (!normalSpeed)
                SessionState.SetBool(NormalKey, false);
            string outcome = haveMatch ? match.Outcome.ToString() : string.Empty;
            if (haveMatch && match.Outcome == SkirmishOutcome.None)
                outcome = string.Empty;
            var inputAudit = world != null && world.IsCreated
                ? world.GetExistingSystemManaged<Game.UI.Shell.Ecs.AriaPlayInputSystem>() : null;
            int measuredInputViolations = -1;
            bool measuredInput = inputAudit != null && inputAudit.MeasuredHumanInterventions >= 0 &&
                AriaCommandEvidence.TryReadAudit(inputAudit.CompletedGestures, inputAudit.UnexpectedSamples,
                    out measuredInputViolations);
            if (measuredInput) measuredInputViolations += terminalUnexpectedSamples;
            Debug.Log("[SkirmishInputEvidence] gestures=" + (inputAudit?.DispatchedGestures ?? 0) +
                " completed=" + (inputAudit?.CompletedGestures ?? 0) + " samples=" + (inputAudit?.AcceptedSamples ?? 0) +
                " unexpected=" + (inputAudit?.UnexpectedSamples ?? 0) +
                " interventions=" + (inputAudit?.MeasuredHumanInterventions ?? -1) + " recordedCommands=" + AriaCommandEvidence.AcceptedCommands +
                " receiptViolations=" + AriaCommandEvidence.Violations +
                " acceptedCommandCoverage=" + (measuredInput ? "SupportedAriaControlsV2" : "Pending"));
            var facts = new SkirmishS002AriaTerminalFacts
            {
                RunId = runId,
                CatalogId = catalogId,
                Seed = seed,
                Locale = locale,
                Device = "Editor",
                NormalSpeed = normalSpeed,
                StartedAtUtc = SessionState.GetString(StartedKey, string.Empty),
                MatchFinished = haveMatch && match.Phase == SkirmishPhase.Finished && match.Outcome != SkirmishOutcome.None,
                MatchOutcome = outcome,
                EndReason = abort ? abortReason : haveMatch ? match.Reason.ToString() : abortReason,
                DurationSeconds = haveMatch ? duration : 0f,
                // Missing provenance is unknown, never a measured clean run.
                InputViolations = measuredInputViolations,
                HumanInterventions = inputAudit != null && inputAudit.MeasuredHumanInterventions >= 0
                    ? inputAudit.MeasuredHumanInterventions + terminalInterventions : -1,
                TracePath = traceRelative,
                LogPath = logRelative,
                ForcedVictory = false,
                Aborted = abort || !haveMatch || match.Phase != SkirmishPhase.Finished
            };
            if (TryReadCensus(root, out SkirmishAcceptanceCensus census))
            {
                facts.DefinitionVersion = census.DefinitionVersion;
                facts.CodeHash = census.CodeHash;
                facts.ConfigHash = census.ConfigHash;
            }

            bool appended = SkirmishS002AriaRunLog.TryAppendTerminalRow(runsPath, in facts, out string row, out string error);
            bool counted = appended && SkirmishS002AriaRunLog.IsCountedAriaWin(in facts, RowResult(row));
            string log = string.Format(
                CultureInfo.InvariantCulture,
                "[SkirmishS002AriaRun] appended={0} countedWin={1} error={2}\n{3}\n",
                appended ? 1 : 0,
                counted ? 1 : 0,
                error ?? string.Empty,
                row ?? string.Empty);
            File.WriteAllText(logAbsolute, log);
            if (appended)
                Debug.Log(log.Trim());
            else
                Debug.LogError(log.Trim());

            AriaCommandEvidence.End();
            UiShellRuntimeGateway.StopAriaPlay();
            EditorApplication.ExitPlaymode();
            RequestQuitIfAsked(counted ? 0 : 2);
        }

        private static bool TryReadCensus(string root, out SkirmishAcceptanceCensus census)
        {
            census = null;
            if (!SkirmishSetupMatrixTable.TryLoad(root, out List<SkirmishSetupMatrixRow> matrix, out _))
                return false;
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            if (catalogId == SkirmishAcceptanceCensusCapture.S003CatalogId)
            {
                bool captured = SkirmishAcceptanceCensusCapture.TryCaptureS003RegularStandard(
                    authored, matrix, seed, out census, out _);
                if (captured) census.CodeHash = SessionState.GetString(CandidateHashKey, "");
                return captured;
            }

            return SkirmishAcceptanceCensusCapture.TryCapture(
                authored,
                matrix,
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                seed,
                out census,
                out _);
        }

        private static void RequestQuitIfAsked(int exitCode)
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("WARLINE_ARIA_QUIT"), "1", StringComparison.Ordinal))
                return;
            // A delayCall scheduled during ExitPlaymode can be discarded by domain reload.
            // Persist the request and wait until the native play-mode transition completes.
            SessionState.SetInt(QuitCodeKey, exitCode);
            SessionState.SetBool(QuitPendingKey, true);
            EditorApplication.update -= FinishQuitAfterPlayMode;
            EditorApplication.update += FinishQuitAfterPlayMode;
        }

        private static void FinishQuitAfterPlayMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            int exitCode = SessionState.GetInt(QuitCodeKey, 2);
            SessionState.EraseBool(QuitPendingKey);
            SessionState.EraseInt(QuitCodeKey);
            EditorApplication.update -= FinishQuitAfterPlayMode;
            Debug.Log("[SkirmishValidationExit] result=Requested code=" + exitCode);
            EditorApplication.Exit(exitCode);
        }

        private static string RowResult(string row)
        {
            if (string.IsNullOrEmpty(row))
                return string.Empty;
            string[] fields = row.Split(',');
            return fields.Length > 13 ? fields[13] : string.Empty;
        }
    }
}
#endif
