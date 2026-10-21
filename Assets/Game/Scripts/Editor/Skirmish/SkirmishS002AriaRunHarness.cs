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

        private static ValidationBackgroundInputScope backgroundInputScope;
        private static double nativeShadersSettledAt = -1d;
        private static bool nativeShadersReady;
        private static bool nativeOpeningRenderViewAccepted;
        private static bool finishingEvidence;
        private static System.Threading.Tasks.Task<string> finishSourceHash;
        private static System.Diagnostics.Stopwatch finishFingerprintTimer;
        private static bool finishAbort;
        private static string finishAbortReason;

        private static bool NativeShadersAreReady(double now)
        {
            if (nativeShadersReady) return true;
            if (ShaderUtil.anythingCompiling)
            {
                nativeShadersSettledAt = -1d;
                next = now + .5d;
                return false;
            }
            if (nativeShadersSettledAt < 0d) nativeShadersSettledAt = now;
            if (now - nativeShadersSettledAt < 2d)
            {
                next = now + .5d;
                return false;
            }
            nativeShadersReady = true;
            Debug.Log("[SkirmishNativeShaderGate] result=Passed compiling=0 settledSeconds=2");
            return true;
        }

        private static void ConfigureBackgroundInput()
        {
            if (Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION") != "1") return;
            if (backgroundInputScope != null && backgroundInputScope.IsActive) return;
            backgroundInputScope?.Dispose();
            backgroundInputScope = new ValidationBackgroundInputScope();
        }

        private static void RestoreBackgroundInput()
        {
            backgroundInputScope?.Dispose();
            backgroundInputScope = null;
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
            ObserveFixedWingReturnReceipt(operation, receipt, verified);
            File.AppendAllText(LiveTracePath() + ".commands.jsonl", JsonUtility.ToJson(new CommandEvidence
            { frame = Time.frameCount, operation = operation, receipt = receipt, verified = verified }) + "\n");
        }
        [Serializable]
        private struct CommandEvidence { public int frame; public string operation; public uint receipt; public bool verified; }

        private static void RecordTouchEvidence(AriaTouchInputUiSystemHelper driver, string kind, Vector2 position)
        {
            if (!SessionState.GetBool(Key, false)) return;
            bool saveResumeInput = playerResumeProbeActive && (ReferenceEquals(driver, playerResumeTouch) ||
                playerResumeCombat && ReferenceEquals(driver, manualTouch));
            bool manualInput = ReferenceEquals(driver, manualTouch);
            bool menuInput = ReferenceEquals(driver, menuTouch);
            if ((saveResumeInput || manualInput) && kind == "Ended")
                AriaCommandEvidence.ObserveRelease(Time.frameCount, position);
            if (stage < 3 && !saveResumeInput && !manualInput && !menuInput) return;
            File.AppendAllText(LiveTracePath() + ".touch.jsonl", JsonUtility.ToJson(new TouchEvidence
            {
                frame = Time.frameCount, time = Time.unscaledTime, kind = kind, device = driver.DeviceId,
                phase = menuInput ? "Menu" : ReferenceEquals(driver, terminalTouch) ? "Terminal" :
                    saveResumeInput ? "PlayerResume" : manualInput ? "Manual" : "Runtime",
                generation = driver.Generation, dispatched = driver.DispatchedGestures,
                completed = driver.CompletedGestures, samples = driver.AcceptedSamples,
                unexpected = driver.UnexpectedSamples, interventions = driver.PhysicalInterventions,
                position = position,
                matchFinished = UiShellRuntimeGateway.TryReadSkirmish(out var touchMatch) && touchMatch.Finished,
                acceptedCommands = AriaCommandEvidence.AcceptedCommands,
                receiptViolations = AriaCommandEvidence.Violations,
                commandAuditActive = AriaCommandEvidence.Active
            }) + "\n");
        }

        [Serializable]
        private struct TouchEvidence
        {
            public int frame, device, acceptedCommands, receiptViolations;
            public bool matchFinished, commandAuditActive;
            public float time;
            public string kind, phase;
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
                int defaultSeed = catalog == SkirmishAcceptanceCensusCapture.S004CatalogId ? SkirmishS004FirstVisit.SeedA
                    : catalog == SkirmishAcceptanceCensusCapture.S003CatalogId ? SkirmishS003FirstVisit.SeedA
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
            string reviewDirectory = Environment.GetEnvironmentVariable("WARLINE_NATIVE_REVIEW_DIRECTORY");
            if (string.IsNullOrWhiteSpace(reviewDirectory))
                reviewDirectory = Application.platform == RuntimePlatform.OSXEditor ? "/private/tmp" : Path.GetTempPath();
            reviewDirectory = Path.GetFullPath(reviewDirectory);
            Directory.CreateDirectory(reviewDirectory);
            string prefix = Path.Combine(reviewDirectory, catalogId.ToLowerInvariant() + "-native-" + GameLocalization.CurrentLocaleCode + "-" + width + "x" + height);
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
                if(Environment.GetEnvironmentVariable("WARLINE_MAP_REVIEW_ONLY")=="1")
                {
                    string path=prefix+"-opening.png";
                    if(!File.Exists(path)) return false;
                    byte[] png=File.ReadAllBytes(path);
                    if(png.Length<24 || png[0]!=137 || png[1]!=80 || png[2]!=78 || png[3]!=71)
                        throw new InvalidOperationException("Native map capture is not a PNG.");
                    int actualWidth=png[16]<<24|png[17]<<16|png[18]<<8|png[19];
                    int actualHeight=png[20]<<24|png[21]<<16|png[22]<<8|png[23];
                    if(actualWidth!=width || actualHeight!=height)
                        throw new InvalidOperationException("Native map capture dimensions differ from requested dimensions.");
                    if (Environment.GetEnvironmentVariable("WARLINE_S005_READONLY_AIR_SOURCE_FIT") == "1" &&
                        !TickReadOnlyS005AirportSourceFit())
                    { next = now + .01d; return false; }
                    if (catalogId == "S005") CaptureS005NativeCheckpointStaging(prefix);
                    Debug.Log("[SkirmishNativeMapReview] result=Captured playerReady=0 path="+path);
                    return true;
                }
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

        private static void CaptureS005NativeCheckpointStaging(string prefix)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if (sessions.CalculateEntityCount() != 1)
                throw new InvalidOperationException("Native checkpoint staging requires one expanded session.");
            bool captured = SkirmishNativeCheckpointSnapshotService.TryCapture(
                em, sessions.GetSingletonEntity(), out var snapshot, out string failure);
            var nativePrefabs = SkirmishNativeCheckpointPrefabRegistry.Find(em, sessions.GetSingletonEntity());
            int nativePrefabCount = 0;
            bool nativePrefabsValid = nativePrefabs != null && nativePrefabs.Validate(out nativePrefabCount);
            string path = prefix + "-checkpoint-staging.json";
            File.WriteAllText(path, captured ? JsonUtility.ToJson(snapshot, true) :
                JsonUtility.ToJson(new NativeCheckpointStagingFailure { Failure = failure }, true));
            bool persisted = false;
            bool decoded = false;
            bool prepared = false;
            string preparationFailure = captured ? string.Empty : failure;
            if (captured)
            {
                Entity session = sessions.GetSingletonEntity();
                var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                string savePath = prefix + "-checkpoint-wire.json";
                persisted = SkirmishNativeCheckpointCodec.TryWriteAtomic(savePath, snapshot, setup);
                decoded = persisted && SkirmishNativeCheckpointCodec.TryRead(savePath, setup, out snapshot);
                if (decoded)
                {
                    prepared = SkirmishNativeCheckpointRecoveryPreparation.TryPrepare(em, session, snapshot, setup,
                        nativePrefabs.Resolve, out _, out preparationFailure);
                }
            }
            Debug.Log("[S005NativeCheckpointStaging] captureAvailable=" + (captured ? 1 : 0) +
                " nativeRecoveryComplete=0 actors=" + (snapshot?.Actors?.Length ?? 0) +
                " mapReferences=" + (snapshot?.MapReferences?.Length ?? 0) + " persisted=" + (persisted ? 1 : 0) +
                " banks=" + (snapshot?.Banks?.Length ?? 0) + " schema=" + (snapshot?.Version ?? 0) +
                " decoded=" + (decoded ? 1 : 0) + " prepared=" + (prepared ? 1 : 0) +
                " runtimePrefabRegistry=" + (nativePrefabsValid ? 1 : 0) + " runtimePrefabs=" + nativePrefabCount +
                " failure=" + failure + " preparationFailure=" + preparationFailure + " path=" + path);
            if (em.HasComponent<SkirmishNativeCheckpointRecoveryRecord>(sessions.GetSingletonEntity()))
            {
                var recovery = em.GetComponentObject<SkirmishNativeCheckpointRecoveryRecord>(sessions.GetSingletonEntity());
                if (!recovery.Complete || !captured || !persisted || !decoded || !prepared || !nativePrefabsValid)
                    throw new InvalidOperationException("Fresh native recovery diagnostic failed: " + recovery.Failure);
                Debug.Log("[S005NativeRecoveryDiagnostic] result=Passed freshNativeScene=1 input=Diagnostic " +
                    "normalInputRecovery=0 playerReady=0 actors=" + snapshot.Actors.Length +
                    " sources=" + snapshot.MapReferences.Length + " banks=" + snapshot.Banks.Length +
                    " editorPrefabResolver=0 runtimePrefabs=" + nativePrefabCount);
                Environment.SetEnvironmentVariable("WARLINE_NATIVE_RECOVERY_DIAGNOSTIC", null);
            }
        }

        [Serializable] private sealed class NativeCheckpointStagingFailure { public string Failure; }

        public static void RunS003StartupAndExit()
        {
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", "1");
            RunFocusedAriaAndExit();
        }

        public static void RunS004AriaAndExit()
        {
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG", SkirmishAcceptanceCensusCapture.S004CatalogId);
            RunFocusedAriaAndExit();
        }

        public static void RunS005AriaEn() => LaunchS005Validation(false);
        public static void RunS005StartupEn() => LaunchS005Validation(true);
        public static void RunS005MapReviewAndExit()
        {
            Environment.SetEnvironmentVariable("WARLINE_S003_NATIVE_REVIEW","1");
            Environment.SetEnvironmentVariable("WARLINE_MAP_REVIEW_ONLY","1");
            nativeReviewStage=0;nativeReviewSizeApplied=false;
            RunS005StartupEn();
        }

        private static void LaunchS005Validation(bool startupOnly)
        {
            SessionState.SetBool("S004EditorJourney.OptionsSaved", true);
            SessionState.SetBool("S004EditorJourney.OptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt("S004EditorJourney.Options", (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions |= EnterPlayModeOptions.DisableDomainReload;
            AssetDatabase.DisallowAutoRefresh();
            SessionState.SetBool("Warline.MissionValidation.ResumeRefresh", true);
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG", "S005");
            Environment.SetEnvironmentVariable("WARLINE_S002_SEED", "104734");
            Environment.SetEnvironmentVariable("WARLINE_S002_LOCALE", "en");
            Environment.SetEnvironmentVariable("WARLINE_S004_MENU_JOURNEY", null);
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", startupOnly ? "1" : null);
            RunFocusedAriaAndExit();
        }

        public static bool NativeRenderingIsReady(EntityManager em)
        {
            using var databases = em.CreateEntityQuery(typeof(OperationMapRenderDatabaseComponent));
            if (databases.IsEmptyIgnoreFilter) return true;
            if (databases.CalculateEntityCount() != 1) return false;
            var database = databases.GetSingletonEntity();
            if (!em.HasComponent<OperationMapRenderPackedReadinessComponent>(database) ||
                !em.HasComponent<OperationMapRenderVirtualizationStateComponent>(database) ||
                !em.HasComponent<OperationMapRenderVirtualizationMetricsComponent>(database)) return false;
            var state = em.GetComponentData<OperationMapRenderVirtualizationStateComponent>(database);
            var metrics = em.GetComponentData<OperationMapRenderVirtualizationMetricsComponent>(database);
            return state.Initialized != 0 && state.InitialViewApplied != 0 && state.RebuildCount > 0 &&
                state.ActiveSlotCount > 0 && state.OverflowCount == 0 && metrics.OverflowCount == 0 &&
                metrics.EnabledSlotCount == state.ActiveSlotCount && metrics.Capacity >= state.ActiveSlotCount;
        }

        // Packed-view completeness is an opening invariant. Later camera movement may
        // legitimately leave the current view with no active packed slots.
        public static bool NativeRenderingStartupGateAllows(EntityManager em, ref bool openingViewAccepted)
        {
            if (openingViewAccepted) return true;
            if (!NativeRenderingIsReady(em)) return false;
            openingViewAccepted = true;
            return true;
        }

        private static string SelectedReportDirectory() => productionCanary != null
            ? productionCanary.Request.ArtifactDirectory : catalogId switch
        {
            "S005" => "Design/AgentReports/SkirmishExpansion/S005/",
            SkirmishAcceptanceCensusCapture.S004CatalogId => SkirmishAcceptanceScaffold.S004RelativeReportEvidenceDirectory,
            SkirmishAcceptanceCensusCapture.S003CatalogId => SkirmishAcceptanceScaffold.S003RelativeReportEvidenceDirectory,
            _ => SkirmishAcceptanceScaffold.RelativeReportEvidenceDirectory
        };

        private static bool TryCreateSelectedPayload(string catalog, int selectedSeed, string selectedLocale,
            out SkirmishAriaAcceptancePayload payload, out string error)
        {
            if(codedSizeCameraActive)
            {
                payload=default;error=null;
                return catalog==codedSizeExpected.CatalogId && selectedSeed==codedSizeExpected.Seed && selectedLocale==codedSizeLocale;
            }
            if(productionCanary!=null)
            {
                var request=productionCanary.Request;
                if(catalog!=request.CatalogId||selectedSeed!=request.Seed||selectedLocale!=request.Locale)
                    throw new InvalidOperationException("Payload differs from exact claimed normal journey");
                if(request.IsLegacyPreset) { SkirmishLegacyProductionIdentity.ValidateJob(request.Job); payload=default; error=null; return true; }
                return SkirmishAriaAcceptancePayload.TryCreate(catalog,(string)request.Job["definition_id"],
                    (int)request.Job["definition_version"],SkirmishSizeId.Standard,SkirmishDifficultyId.Regular,
                    selectedSeed,selectedLocale,out payload,out error);
            }
            if (catalog == "S005")
                return SkirmishAriaAcceptancePayload.TryCreate("S005", "skirmish.s005", 2,
                    SkirmishSizeId.Standard, SkirmishDifficultyId.Regular, selectedSeed, selectedLocale, out payload, out error);
            if (catalog == SkirmishAcceptanceCensusCapture.S004CatalogId)
                return SkirmishAriaAcceptancePayload.TryCreateFirstVisitS004(selectedSeed, selectedLocale, out payload, out error);
            if (catalog == SkirmishAcceptanceCensusCapture.S003CatalogId)
                return SkirmishAriaAcceptancePayload.TryCreateFirstVisitS003(selectedSeed, selectedLocale, out payload, out error);
            return SkirmishAriaAcceptancePayload.TryCreateFirstVisitS002(selectedSeed, selectedLocale, out payload, out error);
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
            // Default-off diagnostic owns both engine calls; normal mission Tick never runs.
            bool noMissionTransition = Environment.GetEnvironmentVariable("WARLINE_S074_NO_MISSION_TRANSITION") == "1";
            var transitionOwner = noMissionTransition
                ? AppDomain.CurrentDomain.GetData("Warline.S074.NoMissionTransition.Owner") as Action<Action, Action>
                : null;
            if (noMissionTransition && (transitionOwner == null || productionBatch != null || productionCanary != null ||
                SessionState.GetBool(Key, false) || SessionState.GetBool(QuitPendingKey, false) ||
                EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating))
                throw new InvalidOperationException("Exact idle diagnostic transition owner required; no normal fallback.");
            nativeShadersReady = false;
            nativeShadersSettledAt = -1d;
            nativeOpeningRenderViewAccepted = false;
            string error;
            bool accepted = TryCreateSelectedPayload(requestedCatalog, requestedSeed, requestedLocale, out _, out error);
            if (!accepted)
                throw new InvalidOperationException(error);
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit play mode before launching the ARIA watch.");

            catalogId = string.IsNullOrEmpty(requestedCatalog)
                ? SkirmishAcceptanceCensusCapture.CatalogId
                : requestedCatalog;
            seed = requestedSeed;
            locale = requestedLocale;
            Debug.Log("[SkirmishCandidateFingerprint] phase=Start owner=LaunchCatalog");
            var launchFingerprintTimer = System.Diagnostics.Stopwatch.StartNew();
            string launchFingerprint = SkirmishCandidateSourceHash.Compute(Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
            launchFingerprintTimer.Stop();
            Debug.Log("[SkirmishCandidateFingerprint] phase=Completed owner=LaunchCatalog elapsedMilliseconds=" + launchFingerprintTimer.Elapsed.TotalMilliseconds.ToString("R", CultureInfo.InvariantCulture) + " sha256=" + launchFingerprint);
            RecordProductionBatchFingerprint("Begin",launchFingerprint,launchFingerprintTimer.Elapsed.TotalMilliseconds);
            SessionState.SetString(CandidateHashKey, launchFingerprint);
            stage = 0;
            finishingEvidence = false;
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
            if (Environment.GetEnvironmentVariable("WARLINE_S004_MENU_JOURNEY") == "1")
            {
                // Isolated test profile satisfies the existing Chapter 1 mode unlock.
                profile.campaignMissionProgress = new CampaignMissionProgressSaveData[5];
                for (int i = 0; i < 5; i++)
                    profile.campaignMissionProgress[i] = new CampaignMissionProgressSaveData
                    { missionId = Game.Missions.Contracts.CampaignMissionSequence.IdAt(i), available = true, firstClearCompleted = true, bestStars = 3 };
                Debug.Log("[S004MenuProfileFixture] chapter1Completed=5 progressionGate=Preserved isolatedProfile=1");
            }
            save.SaveProfile(profile);

            EditorApplication.update -= Tick;
            // A unique trace per launch preserves earlier candidates and failures.
            // SessionState keeps the identity across play-mode domain reloads.
            if (Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY") != "1")
                ResetLiveTrace(LiveTracePath());
            if (!noMissionTransition) EditorApplication.update += Tick;
            int width = int.TryParse(Environment.GetEnvironmentVariable("WARLINE_REVIEW_WIDTH"), out int requestedWidth)
                ? Mathf.Clamp(requestedWidth, 1280, 2400) : 1920;
            int height = int.TryParse(Environment.GetEnvironmentVariable("WARLINE_REVIEW_HEIGHT"), out int requestedHeight)
                ? Mathf.Clamp(requestedHeight, 720, 1440) : 1080;
            MainMenuV3PrefabBuilder.SetGameViewResolution(width, height);
            Action openMenu = () =>
            {
                // Reopening an identical saved Menu may rebuild engine-owned companion objects.
                // Preserve dirty scenes; this does not normalize preview scenes or cleanup evidence.
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                        throw new InvalidOperationException("Preserve dirty Editor scene before Menu transition");
                var currentSetup = UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (currentSetup.Length == 1 && currentSetup[0].path == "Assets/Game/Scenes/Menu.unity" &&
                    currentSetup[0].isLoaded && currentSetup[0].isActive && activeScene.IsValid() &&
                    activeScene.isLoaded && activeScene.path == "Assets/Game/Scenes/Menu.unity" && !activeScene.isDirty)
                    return;
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            };
            if (noMissionTransition)
            {
                // Prevent InitializeOnLoad from resuming normal Tick after a real domain reload.
                SessionState.SetBool(Key, false);
                SessionState.SetBool(NormalKey, false);
                transitionOwner(
                    openMenu,
                    () => EditorApplication.EnterPlaymode());
                return;
            }
            openMenu();
            EditorApplication.EnterPlaymode();
        }

        public static string EvidenceDirectory()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string relative = SelectedReportDirectory();
            return Path.Combine(root, relative);
        }

        public static void AbortFailedValidationAndExit()
        {
            Finish(true, "incompleteNativeInputFlow");
        }

        private static string RunsCsvPath(string root)
        {
            return Path.GetFullPath(Path.Combine(root, CandidateEvidenceRelative(), SkirmishAcceptanceScaffold.RunsFileName));
        }

        private static string CandidateEvidenceRelative()
        {
            string hash = SessionState.GetString(CandidateHashKey, "");
            if (hash.Length != 64) throw new InvalidOperationException("Missing candidate source fingerprint.");
            string report = SelectedReportDirectory();
            return report + "/candidates/" + hash;
        }

        private static string ValidationProfileRoot()
        {
            if(productionCanary!=null)return Path.Combine(productionCanary.Request.ArtifactDirectory,"save-profile");
            return Path.Combine(Path.GetTempPath(), "aria-play-validation-profile",
                SessionState.GetString(Key + ".artifactId", "missing-artifact"));
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
                return;
            if (playerResumeProbeActive) return;
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
                    ? (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_NATIVE_RECOVERY_DIAGNOSTIC")) ? 120d : 360d)
                    : WallClockBudgetSeconds);
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
                foreach (var view in UnityEngine.Object.FindObjectsByType<MatchSceneView>(FindObjectsSortMode.None))
                    if (view.OperationMapContentFailureCode != OperationMapLoadResultCode.None)
                    {
                        Finish(true, "operationMapFailure=" + view.OperationMapContentFailureCode);
                        return;
                    }
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
                    bool menuJourney = Environment.GetEnvironmentVariable("WARLINE_S004_MENU_JOURNEY") == "1";
                    if ((!menuJourney || menuStep == 0) && !SkirmishLaunchProjection.IsShellReadyToEnterMatch(em))
                    {
                        next = now + 0.5d;
                        return;
                    }

                    if (menuJourney)
                    {
                        if (!EnterS004ThroughPresentedControls()) return;
                    }
                    else if (!TryQueueExpanded(em)) return;
                    SkirmishAriaAcceptancePayload payload;
                    bool selected = TryCreateSelectedPayload(catalogId, seed, locale, out payload, out _);
                    if (!selected)
                        return;
                    if(codedSizeCameraActive) Debug.Log("[CodedBaCameraJourney] selected catalog="+catalogId+" size="+codedSizeExpected.SizeId+" qualification=0");
                    else Debug.Log("[SkirmishS002AriaRun] selected " + (productionCanary?.Request.IsLegacyPreset==true ? "LegacyPreset catalog="+catalogId+" seed="+seed+" nativeSettings=CodedPreset" : payload.FormatSelectedConfiguration()));
                    if (!codedSizeCameraActive && TryReadCensus(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), out SkirmishAcceptanceCensus census))
                        Debug.Log(SkirmishAcceptanceCensusCapture.FormatLog(census));
                    stage = 1;
                    next = now + 2d;
                    return;
                }

                if (!SkirmishLaunchProjection.TryGet(em, out Entity session, out SkirmishMatchState match))
                    return;
                bool nativeS005MenuJourney = (catalogId == "S005" || productionCanary != null || codedSizeCameraActive) && Environment.GetEnvironmentVariable("WARLINE_S004_MENU_JOURNEY") == "1";
                if (stage <= 1 && !nativeS005MenuJourney)
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
                    if (!NativeShadersAreReady(now)) return;
                    if (!NativeRenderingStartupGateAllows(em, ref nativeOpeningRenderViewAccepted))
                    {
                        try { Debug.Log("[SkirmishNativeRenderingDiagnostic] " + CaptureNativeRenderingFailureDiagnostic(em)); }
                        catch (Exception diagnosticError) { Debug.LogWarning("[SkirmishNativeRenderingDiagnostic] captured=0 error=" + diagnosticError); }
                        try
                        {
                            string failedView = LiveTracePath() + ".packed-render-failed-unqualified.png";
                            ScreenCapture.CaptureScreenshot(failedView);
                            Debug.Log("[SkirmishNativeRenderingFailureView] captureRequested=1 qualified=0 path=" + failedView);
                        }
                        catch (Exception screenshotError) { Debug.LogWarning("[SkirmishNativeRenderingFailureView] captureRequested=0 qualified=0 error=" + screenshotError); }
                        Debug.LogError("[SkirmishNativeRendering] result=Failed reason=incompletePackedRenderInitialization");
                        Finish(true, "incompletePackedRenderInitialization");
                        return;
                    }
                    if (codedSizeCameraActive) { TickCodedSizeCamera(em, session, in match, simulationActive, elapsed); return; }
                    if (Environment.GetEnvironmentVariable("WARLINE_S005_PAID_BUILDING_CLEARANCE_ONLY") == "1" &&
                        Environment.GetEnvironmentVariable("WARLINE_NATIVE_PLACEMENT_CLEARANCE") == "1")
                    {
                        ScreenCapture.CaptureScreenshot(LiveTracePath() + ".placement-clearance-starting.png");
                        SkirmishBuildingPlacementProbe.InspectSharedSceneryClearance(LiveTracePath() + ".placement-clearance.json");
                        Environment.SetEnvironmentVariable("WARLINE_NATIVE_PLACEMENT_CLEARANCE", null);
                    }
                    if (Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY") == "1")
                    {
                        if (TickS005OpeningGroupCandidates(em,session)) return;
                        if (TickS005WarmRenderExitDiagnostic(em,session)) return;
                        if (elapsed < 2f) { next = now + 0.5d; return; }
                        if (nativeProductionStage > 0)
                        {
                            return;
                        }
                        // The opening roster was already audited before the first scan frame.
                        // Live combat may destroy actors while this observational scan yields;
                        // those normal lifecycle changes are not failed opening initialization.
                        if (Environment.GetEnvironmentVariable("WARLINE_S005_READONLY_AIR_SOURCE_FIT") == "1" &&
                            airportSourceFitScan != null && airportSourceFitScan.frames > 0)
                        {
                            if (!CaptureNativeReview(now)) return;
                            Finish(false, "readOnlyAirportSourceFit");
                            return;
                        }
                        using var buildings = em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo), typeof(SkirmishAttemptOwnedComponent));
                        using var ready = em.CreateEntityQuery(typeof(SkirmishSharedBuildingsReady));
                        using var banks = em.CreateEntityQuery(typeof(FactionTacticalMaterialsComponent));
                        using var units = em.CreateEntityQuery(typeof(SkirmishUnitRoleComponent), typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishVisualSpawnedComponent));
                        var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                        int expectedUnits = 0;
                        foreach (var force in setup.Forces) expectedUnits += math.max(1, force.Quantity);
                        bool rosterValid = NativeStartupRosterMatches(em, session, setup, out int initialUnits, out int deliveredUnits);
                        Debug.Log("[SkirmishNativeStartupRoster] result=" + (rosterValid ? "Passed" : "Failed") +
                            " initial=" + initialUnits + " expectedInitial=" + expectedUnits + " paidDelivered=" + deliveredUnits);
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
                            !rosterValid || units.CalculateEntityCount() != initialUnits + deliveredUnits || playerBanks != 1 || enemyBanks != 1 ||
                            sharedUnits.CalculateEntityCount() != units.CalculateEntityCount() || suppressedUnits.CalculateEntityCount() != 0 ||
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
                        if (!invalid && Environment.GetEnvironmentVariable("WARLINE_NATIVE_PLACEMENT_CLEARANCE") == "1")
                        {
                            ScreenCapture.CaptureScreenshot(LiveTracePath() + ".placement-clearance-starting.png");
                            SkirmishBuildingPlacementProbe.InspectSharedSceneryClearance(LiveTracePath() + ".placement-clearance.json");
                            Environment.SetEnvironmentVariable("WARLINE_NATIVE_PLACEMENT_CLEARANCE", null);
                        }
                        Finish(invalid, "sharedStartupDiagnostic");
                        return;
                    }
                    if(catalogId==SkirmishAcceptanceCensusCapture.S004CatalogId)
                    {
                        var guards=UnityEngine.Object.FindObjectsByType<SkirmishS004CampaignLighting>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                        var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                        bool matchSunActive=Array.Exists(lights,l=>l.type==LightType.Directional && l.gameObject.scene.name=="Match" && l.enabled);
                        var sun=RenderSettings.sun;
                        if(guards.Length!=1 || matchSunActive || sun==null || !sun.enabled || sun.gameObject.scene.name!="RuntimeBinding")
                        {
                            Debug.LogError("[S004NativeCampaignLighting] result=Failed guards="+guards.Length+" matchSunActive="+matchSunActive);
                            Finish(true,"campaignLightingNotApplied");return;
                        }
                        Debug.Log("[S004NativeCampaignLighting] result=Passed campaignSun="+sun.name+" intensity="+sun.intensity+" matchSunActive=0");
                    }
                    if (Environment.GetEnvironmentVariable("WARLINE_S005_PLAYER_RESUME_PROBE") == "1" && !playerResumeProbeDone)
                    { StartPlayerResumeProbe(em, session); return; }
                    if (Environment.GetEnvironmentVariable("WARLINE_S004_MANUAL_PROBE") == "1")
                    {
                        StartS004ManualProbe();
                        ScreenCapture.CaptureScreenshot(LiveTracePath() + ".opening.png");
                        stage = 3;
                        next = now + 1d;
                        return;
                    }
                    if (!StartS004AriaThroughPresentedControls())
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

                if (OwnedPadBoundaryDiagnostic.TryFinish()) return;
                if (elapsed >= 180f && !OwnedPadBoundaryDiagnostic.Active && Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_INPUT_DIAGNOSTIC") == "1")
                {
                    Finish(abort: false, abortReason: "diagnosticWindowComplete");
                    return;
                }
                // A stopped/blocked driver is failed evidence. The harness must
                // never silently consent again or override a player's Stop.
                AriaPlayModel aria = UiShellRuntimeGateway.ReadAriaPlay();
                if (Environment.GetEnvironmentVariable("WARLINE_S004_MANUAL_PROBE") != "1" &&
                    (aria.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked))
                {
                    if (sagaMenuProof)
                        ScreenCapture.CaptureScreenshot(LiveTracePath() + ".aria-stopped-" + aria.Phase + ".png");
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
            if (SkirmishLaunchProjection.TryGet(em, out _, out _)) return false;
            var config = QuickGameConfig.Defaults;
            config.ScenarioIndex = catalogId == "S005" ? SkirmishPresetConfig.DesertBaseCombinedArmsFieldScenarioIndex
                : catalogId == SkirmishAcceptanceCensusCapture.S004CatalogId
                ? SkirmishPresetConfig.DesertBaseAirMobileEstablishedScenarioIndex
                : catalogId == SkirmishAcceptanceCensusCapture.S003CatalogId
                    ? SkirmishPresetConfig.DesertBaseAirMobileFieldScenarioIndex
                    : SkirmishPresetConfig.DesertBaseEstablishedScenarioIndex;
            config.MapSeed = seed;
            if (!SkirmishLaunchProjection.TryQueue(em, config))
                throw new InvalidOperationException(catalogId + " packaged library queue failed");
            using var sessions = em.CreateEntityQuery(typeof(SkirmishResolvedSetupRecord));
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(sessions.GetSingletonEntity()).Setup;
            QueueNativeRecoveryDiagnostic(em, sessions.GetSingletonEntity(), setup);
            if (setup.CatalogId != catalogId || setup.Seed != seed)
                throw new InvalidOperationException("Library dispatch selected the wrong scenario");
            if (!SkirmishLaunchProjection.TryGet(em, out _, out var queuedMatch) ||
                queuedMatch.ScenarioIndex != config.ScenarioIndex)
                throw new InvalidOperationException("Library dispatch lost the mission scenario index");
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[SkirmishS002AriaRun] queued catalog={0} seed={1} hash={2:X8} acceptance=Pending normal_speed=1",
                setup.CatalogId, setup.Seed, setup.SetupHash));
            return true;
        }

        private static void AppendTrace(
            EntityManager em,
            Entity session,
            in SkirmishMatchState match,
            bool simulationActive,
            float elapsed)
        {
            var traceTiming=EditorTraceTimings.Begin(elapsed);
            long traceTimingStart=traceTiming!=null?EditorTraceTimings.Now():0, traceTimingTick=traceTimingStart;
            bool traceTimingCompleted=false;
            try
            {
            RecordProductionResolvedSnapshot(em, session, in match, simulationActive, elapsed, "NativeResolvedSnapshot");
            if(traceTiming!=null)traceTiming.nativeSnapshotMs=EditorTraceTimings.Since(ref traceTimingTick);
            Directory.CreateDirectory(EvidenceDirectory());
            if(traceTiming!=null)traceTiming.directoryMs=EditorTraceTimings.Since(ref traceTimingTick);
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
            if(traceTiming!=null)traceTiming.baseQueriesMs=EditorTraceTimings.Since(ref traceTimingTick);
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

            if(traceTiming!=null)traceTiming.unitQueryMs=EditorTraceTimings.Since(ref traceTimingTick);
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

            if(traceTiming!=null)traceTiming.traceSerializeMs=EditorTraceTimings.Since(ref traceTimingTick);
            OwnedDiagnosticTraceIo.Append(LiveTracePath(), line, true);

            if(traceTiming!=null)traceTiming.traceWriteMs=EditorTraceTimings.Since(ref traceTimingTick);
            AppendActorDiagnostic(em, elapsed, match.EnemyMainBase);
            AppendInputDiagnostic(em, elapsed);
            traceTimingCompleted=true;
            }
            finally{EditorTraceTimings.Complete(traceTiming,traceTimingStart,traceTimingCompleted);}
        }

        // Editor-owned diagnostic: reuse the SAME already serialized public sample.
        private static class OwnedPadBoundaryDiagnostic
        {
            static readonly bool enabled=Environment.GetEnvironmentVariable("WARLINE_HOME_PAD_BOUNDARY_DIAGNOSTIC")=="1";
            static readonly bool inputDiagnostic=Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_INPUT_DIAGNOSTIC")=="1";
            static object owner;
            static InputDiagnostic latest;
            static bool sequenceObserved;
            internal static bool Active => enabled && productionCanary!=null;
            internal static void Record(InputDiagnostic value,bool nativePath)
            {
                if(!Active||!nativePath)return;
                if(!inputDiagnostic)throw new InvalidOperationException("Owned pad diagnostic requires existing input diagnostic completion path.");
                if(!ReferenceEquals(owner,productionCanary))
                {owner=productionCanary;latest=null;sequenceObserved=false;}
                latest=value;
                if(value.padStage!=0)sequenceObserved=true;
            }
            internal static bool TryTakeCapture(out InputDiagnostic sample,out string status)
            {
                sample=null;status=null;
                if(!Active||!ReferenceEquals(owner,productionCanary)||latest==null||latest.frame!=Time.frameCount)return false;
                status=sequenceObserved&&latest.pad?"PadPresent":latest.padStage==8?"SearchStopped":null;
                if(status==null)return false;
                sample=latest;latest=null;return true;
            }
            internal static bool TryFinish()
            {
                if(!TryTakeCapture(out var sample,out string status))return false;
                Debug.Log("[HomePadBoundaryDiagnostic] result=Captured status="+status+
                    " frame="+sample.frame+" padStage="+sample.padStage+" actions="+sample.actions+
                    " elapsed="+sample.elapsed.ToString("R",CultureInfo.InvariantCulture)+" acceptance=Unmeasured");
                Finish(abort:false,abortReason:"padBoundary:"+status);
                return true;
            }
        }

        [Serializable]
        private sealed class InputDiagnostic
        {
            public float elapsed;
            // Diagnostic-only mirrors of existing public planner/touch/receipt state.
            public float observationTime;
            public int padStage, airCycleStage, airCycleCount;
            public Game.UI.Runtime.SkirmishMatchView.NativeHomePadSiteEvidence padSiteEvidence0, padSiteEvidence1, padSiteEvidence2, padSiteEvidence3;
            public bool padSiteEvidenceSameFrame;
            public int padTargetId, padActions, padSiteIndex, padFeedbackFrame;
            public float padDeadline, padReadyAt;
            public AriaTouchTarget padFocusPlayer, padCatalogTarget, padSite0, padSite1, padSite2, padSite3, padConfirm, padCancel;
            public byte targetPending, advanceNavigation;
            public bool airProfile;
            public float airCycleDeadline;
            public bool assaultAtBase, enemyDesignatedAlive;
            public AriaTouchTarget focusEnemy;
            public byte assaultFocusStage;
            public int assaultFocusActions, assaultFocusTarget, assaultFocusFrame;
            public float assaultFocusDeadline;
            public int lastCompletedTargetId, lastCompletedFrame;
            public UnityEngine.Vector2 lastCompletedPosition;
            public byte precisionStage, precisionAttempts;
            public uint precisionGroup, precisionRoster, precisionBaseline, precisionCurrentSlotGroup;
            public int precisionSlot, precisionPage, precisionIssueFrame, precisionActions;
            public float precisionDeadline, precisionReadyAt;
            public Game.UI.Contracts.UiExpandedPrecisionAttackResult precisionAttack;
            public string phase, intent, observation;
            public int actions, attempts, targetId, observedTargetId, stopReason, pageSeen, navigationStage;
            public float progressAt, objectiveProgressAt, openingUntil;
            public int frame, observationFrame, pageCount, totalGroups, selectedGroups;
            public int assaultMask, attackOrderMask, selectedMask, structureMask;
            public int squadCardMask, squadTargetMask, squadDragMask, airMask;
            public AriaTouchTarget enemyBase, squad4;
            public Game.UI.Runtime.SkirmishMatchView.NativeEnemyBaseTouchEvidence enemyBaseEvidence;
            public bool enemyBaseEvidenceFrameMatches, enemyBaseEvidenceTargetMatches;
            public Game.UI.Runtime.MatchHudSquadTouchNavigation.SlotResolutionDiagnostic squad4Resolution;
            public bool squad4ResolutionFrameMatches, squad4ResolutionTargetMatches, squad4ResolutionPageMatches;
            public uint rosterRevision, sweepRosterRevision;
            public int pagedToAssault, assaultSelecting, structureOrdered, pagesOrdered, homeDefenseStage;
            public bool attackMode, selectionVisible, presentedHomeThreat, mapOpen, placementOpen, approachVisible;
            public float publicApproachDistance, bestApproachDistance;
            public AriaAssaultMapObservation assaultMap;
            public byte assaultInspectStage;
            public int assaultInspectMapIdentity, assaultInspectOpenedFrame;
            public uint assaultInspectRevision;
            public byte approachBaselineObserved;
            public bool assaultStarted, assaultIssued;
            public int materials, infantry, page, structureAttackUnits;
            public bool drawer, riflePending, rocketeerPending, truckCommitted, readiness, pad;
            public AriaTouchTarget recruit;
        }

        private static void AppendInputDiagnostic(EntityManager em, float elapsed)
        {
            AppendInputDiagnosticToPath(em, elapsed, null);
        }

        private static void AppendInputDiagnosticToPath(EntityManager em, float elapsed, string ownedOutputPath)
        {
            var traceTiming=ownedOutputPath==null?EditorTraceTimings.Current:null;
            long traceTimingTick=traceTiming!=null?EditorTraceTimings.Now():0;
            using var query = em.CreateEntityQuery(typeof(AriaSkirmishObservationComponent),
                typeof(AriaSkirmishObservationOwner), typeof(AriaSkirmishPlanComponent));
            if (query.CalculateEntityCount() != 1) return;
            Entity storage=query.GetSingletonEntity();
            Entity boundary=em.GetComponentData<AriaSkirmishObservationOwner>(storage).Boundary;
            if(!Game.UI.Shell.Ecs.AriaSkirmishObservationStorage.TryGet(em,boundary,out var current)||current!=storage||
                !em.HasComponent<AriaPlaySessionComponent>(boundary)||!em.HasComponent<AriaPlayObservationComponent>(boundary))return;
            var view = em.GetComponentData<AriaSkirmishObservationComponent>(storage).Value;
            var input = em.GetComponentData<AriaPlaySessionComponent>(boundary);
            var plan = em.GetComponentData<AriaSkirmishPlanComponent>(storage);
            var target = em.GetComponentData<AriaPlayObservationComponent>(boundary);
            var squad4Resolution = Game.UI.Runtime.MatchHudSquadTouchNavigation.ReadSlotDiagnostic(4);
            var enemyBaseEvidence = Game.UI.Runtime.SkirmishMatchView.NativeEnemyBaseTouchSnapshot;
            var sample = new InputDiagnostic
            {
                elapsed = elapsed,
                padSiteEvidence0 = SkirmishMatchView.NativeHomePadSite0,
                padSiteEvidence1 = SkirmishMatchView.NativeHomePadSite1,
                padSiteEvidence2 = SkirmishMatchView.NativeHomePadSite2,
                padSiteEvidence3 = SkirmishMatchView.NativeHomePadSite3,
                padSiteEvidenceSameFrame = SkirmishMatchView.NativeHomePadSite0.collected &&
                    SkirmishMatchView.NativeHomePadSite1.collected && SkirmishMatchView.NativeHomePadSite2.collected &&
                    SkirmishMatchView.NativeHomePadSite3.collected && SkirmishMatchView.NativeHomePadSite0.frame == view.Frame &&
                    SkirmishMatchView.NativeHomePadSite1.frame == view.Frame &&
                    SkirmishMatchView.NativeHomePadSite2.frame == view.Frame &&
                    SkirmishMatchView.NativeHomePadSite3.frame == view.Frame,
                padTargetId = plan.PadTargetId, padActions = plan.PadAction,
                padSiteIndex = plan.PadSiteIndex, padFeedbackFrame = plan.PadFeedbackFrame,
                padDeadline = plan.PadDeadline, padReadyAt = plan.PadReadyAt,
                padFocusPlayer = view.FocusPlayer, padCatalogTarget = view.AirPad,
                padSite0 = view.PadSite0, padSite1 = view.PadSite1,
                padSite2 = view.PadSite2, padSite3 = view.PadSite3,
                padConfirm = view.PlacementConfirm, padCancel = view.PlacementCancel,
                padStage = plan.PadStage, targetPending = plan.TargetPending,
                advanceNavigation = plan.AdvanceNavigation, airProfile = view.AirProfile,
                airCycleStage = plan.AirCycleStage, airCycleCount = plan.AirCycleCount,
                airCycleDeadline = plan.AirCycleDeadline,
                observationTime = view.Time, assaultAtBase = view.AssaultAtBase,
                enemyDesignatedAlive = view.EnemyDesignatedAlive, focusEnemy = view.FocusEnemy,
                assaultFocusStage = plan.AssaultFocusStage, assaultFocusActions = plan.AssaultFocusActions,
                assaultFocusTarget = plan.AssaultFocusTarget, assaultFocusFrame = plan.AssaultFocusFrame,
                assaultFocusDeadline = plan.AssaultFocusDeadline,
                lastCompletedTargetId = input.LastCompletedTargetId, lastCompletedFrame = input.LastCompletedFrame,
                lastCompletedPosition = input.LastCompletedPosition,
                precisionStage = plan.PrecisionStage, precisionAttempts = plan.PrecisionAttempts,
                precisionGroup = plan.PrecisionGroup, precisionRoster = plan.PrecisionRoster,
                precisionBaseline = plan.PrecisionBaseline, precisionCurrentSlotGroup = view.ExpandedGroup(plan.PrecisionSlot),
                precisionSlot = plan.PrecisionSlot, precisionPage = plan.PrecisionPage,
                precisionIssueFrame = plan.PrecisionIssueFrame, precisionActions = plan.PrecisionActions,
                precisionDeadline = plan.PrecisionDeadline, precisionReadyAt = plan.PrecisionReadyAt,
                precisionAttack = view.PrecisionAttack,
                phase = input.Phase.ToString(), intent = plan.Intent.ToString(), observation = target.Kind.ToString(),
                actions = input.Actions, attempts = input.Attempts, targetId = input.TargetId,
                observedTargetId = target.TargetId, stopReason = input.StopReason,
                pageSeen = plan.AssaultPageSeen, navigationStage = plan.MapNavigationStage,
                frame = view.Frame, observationFrame = target.Frame,
                pageCount = view.ExpandedPageCount, totalGroups = view.ExpandedTotalGroups, selectedGroups = view.ExpandedSelectedGroups,
                assaultMask = view.ExpandedAssaultMask, attackOrderMask = view.ExpandedAttackOrderMask,
                selectedMask = view.ExpandedSelectedMask, structureMask = view.ExpandedStructureMask,
                squadCardMask = view.AvailableSquads, airMask = view.ExpandedAirMask,
                squadTargetMask = (view.Squad0.Available ? 1 : 0) | (view.Squad1.Available ? 2 : 0) | (view.Squad2.Available ? 4 : 0) | (view.Squad3.Available ? 8 : 0) | (view.Squad4.Available ? 16 : 0),
                squadDragMask = (view.Squad0.Drag ? 1 : 0) | (view.Squad1.Drag ? 2 : 0) | (view.Squad2.Drag ? 4 : 0) | (view.Squad3.Drag ? 8 : 0) | (view.Squad4.Drag ? 16 : 0),
                enemyBase = view.EnemyBase, enemyBaseEvidence = enemyBaseEvidence,
                enemyBaseEvidenceFrameMatches = enemyBaseEvidence.collected && enemyBaseEvidence.frame == view.Frame,
                enemyBaseEvidenceTargetMatches = enemyBaseEvidence.collected &&
                    enemyBaseEvidence.target.Id == view.EnemyBase.Id &&
                    enemyBaseEvidence.target.Position.Equals(view.EnemyBase.Position) &&
                    enemyBaseEvidence.target.Available == view.EnemyBase.Available &&
                    enemyBaseEvidence.target.Drag == view.EnemyBase.Drag &&
                    enemyBaseEvidence.target.DragEnd.Equals(view.EnemyBase.DragEnd) &&
                    enemyBaseEvidence.target.ScrollTargetId == view.EnemyBase.ScrollTargetId &&
                    enemyBaseEvidence.target.ScrollDistanceViewports.Equals(view.EnemyBase.ScrollDistanceViewports),
                squad4 = view.Squad4,
                squad4Resolution = squad4Resolution,
                squad4ResolutionFrameMatches = squad4Resolution.collected && squad4Resolution.frame == view.Frame,
                squad4ResolutionPageMatches = squad4Resolution.collected && squad4Resolution.slotLookupEvaluated &&
                    squad4Resolution.page == view.ExpandedPageIndex,
                squad4ResolutionTargetMatches = squad4Resolution.collected &&
                    squad4Resolution.target.Id == view.Squad4.Id &&
                    squad4Resolution.target.Available == view.Squad4.Available &&
                    squad4Resolution.target.Drag == view.Squad4.Drag &&
                    squad4Resolution.target.Position.Equals(view.Squad4.Position) &&
                    squad4Resolution.target.DragEnd.Equals(view.Squad4.DragEnd) &&
                    squad4Resolution.target.ScrollTargetId == view.Squad4.ScrollTargetId &&
                    squad4Resolution.target.ScrollDistanceViewports.Equals(view.Squad4.ScrollDistanceViewports),
                rosterRevision = view.ExpandedRosterRevision, sweepRosterRevision = plan.SweepRosterRevision,
                pagedToAssault = plan.PagedToAssault, assaultSelecting = plan.AssaultSelecting,
                structureOrdered = plan.StructureOrdered, pagesOrdered = plan.PagesOrdered, homeDefenseStage = plan.HomeDefenseStage,
                attackMode = view.AttackMode, selectionVisible = view.SelectionVisible, presentedHomeThreat = view.PresentedHomeThreat,
                mapOpen = view.MapOpen, placementOpen = view.PlacementOpen, approachVisible = view.AssaultDistanceVisible,
                publicApproachDistance = view.NearestFriendlyObjectiveDistance, bestApproachDistance = plan.BestAssaultDistance,
                assaultMap = view.AssaultMap, assaultInspectStage = plan.AssaultInspectStage,
                assaultInspectMapIdentity = plan.AssaultInspectMapIdentity, assaultInspectOpenedFrame = plan.AssaultInspectOpenedFrame,
                assaultInspectRevision = plan.AssaultInspectRevision,
                approachBaselineObserved = plan.AssaultDistanceObserved,
                progressAt = plan.LastProgressAt, objectiveProgressAt = input.LastObjectiveProgressAt,
                openingUntil = plan.OpeningUntil, assaultStarted = plan.AssaultStarted != 0, assaultIssued = plan.AssaultIssued != 0,
                materials = view.OwnMaterials, infantry = view.Infantry, page = view.ExpandedPageIndex,
                drawer = view.DrawerOpen, riflePending = view.RifleRecruitPending,
                rocketeerPending = view.RocketeerRecruitPending, structureAttackUnits = view.OwnStructureAttackUnits,
                truckCommitted = view.LogisticsTruckCommitted, readiness = view.ReadinessEligible,
                pad = view.PadPresent, recruit = view.Recruit
            };

            if(traceTiming!=null)traceTiming.inputCollectMs=EditorTraceTimings.Since(ref traceTimingTick);
            string inputOutput=ownedOutputPath ?? (LiveTracePath() + ".input.jsonl");
            string serializedInput=JsonUtility.ToJson(sample)+"\n";
            if(traceTiming!=null)traceTiming.inputSerializeMs=EditorTraceTimings.Since(ref traceTimingTick);
            OwnedDiagnosticTraceIo.Append(inputOutput, serializedInput, ownedOutputPath == null);
            if(traceTiming!=null)traceTiming.inputWriteMs=EditorTraceTimings.Since(ref traceTimingTick);
            OwnedPadBoundaryDiagnostic.Record(sample,ownedOutputPath==null);
        }




        // Opt-in Editor diagnostic only. Each append is flushed before returning, just as
        // AppendAllText closes its writer before the caller continues.
        private static class OwnedDiagnosticTraceIo
        {
            static readonly bool enabled = string.Equals(Environment.GetEnvironmentVariable(
                "WARLINE_EDITOR_PERSISTENT_TRACE_IO"), "1", StringComparison.Ordinal);
            static OwnedTraceAppendSession session;
            static bool subscribed;

            internal static void Append(string path, string text, bool nativePath)
            {
                if (!enabled || !nativePath || productionCanary == null)
                {
                    CloseOwned("FallbackWriter"); // Preserve direct/nonowned access even if it aliases an owned file.
                    File.AppendAllText(path, text);
                    return;
                }
                try
                {
                    string trace = Path.GetFullPath(LiveTracePath());
                    if (session == null || !session.Matches(productionCanary, trace))
                    {
                        CloseOwned("OwnerOrPathChanged");
                        session = new OwnedTraceAppendSession(productionCanary,
                            productionCanary.Request.ArtifactDirectory, trace);
                        EditorApplication.playModeStateChanged += OnPlayMode;
                        EditorApplication.quitting += OnQuit;
                        AssemblyReloadEvents.beforeAssemblyReload += OnReload;
                        subscribed = true;
                    }
                    session.Append(path, text);
                }
                catch (Exception failure) { CloseAfterFailure(failure); throw; }
            }

            static void OnPlayMode(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.ExitingPlayMode) CloseOwned("ExitingPlayMode");
            }
            static void OnQuit() => CloseOwned("EditorQuitting");
            static void OnReload() => CloseOwned("BeforeAssemblyReload");
            static void CloseAfterFailure(Exception failure)
            {
                try { CloseOwned("AppendFailure", failure.GetType().Name); }
                catch (OutOfMemoryException) { throw; }
                catch { /* Preserve the original append failure. */ }
            }
            internal static void Close() => CloseOwned("Explicit");
            internal static void CloseOwned(string reason, string priorFailure = null)
            {
                var owned = session;
                session = null;
                if (subscribed)
                {
                    EditorApplication.playModeStateChanged -= OnPlayMode;
                    EditorApplication.quitting -= OnQuit;
                    AssemblyReloadEvents.beforeAssemblyReload -= OnReload;
                    subscribed = false;
                }
                if (owned == null) return;
                int opened = owned.OpenedStreamCount;
                long started = System.Diagnostics.Stopwatch.GetTimestamp();
                Exception closeFailure = null;
                try { owned.Dispose(); }
                catch (Exception failure) { closeFailure = failure; throw; }
                finally
                {
                    double elapsed = (System.Diagnostics.Stopwatch.GetTimestamp() - started) *
                        1000d / System.Diagnostics.Stopwatch.Frequency;
                    // Stopwatch ends before logging. This is lifecycle close, not Finish/UI duration.
                    string failure = closeFailure?.GetType().Name ?? priorFailure ?? "None";
                    try
                    {
                        Debug.Log("[OwnedDiagnosticTraceIoClose] result=" + (failure == "None" ? "Closed" : "Failed") +
                            " elapsedMs=" + elapsed.ToString("R", CultureInfo.InvariantCulture) +
                            " openedStreams=" + opened + " reason=" + reason + " failure=" + failure);
                    }
                    catch (Exception loggingFailure) when (!(loggingFailure is OutOfMemoryException))
                    {
                        // A secondary logging failure must not replace the owned I/O failure.
                        if (closeFailure == null && priorFailure == null) throw;
                    }
                }
            }
        }

        private sealed class OwnedTraceAppendSession : IDisposable
        {
            static readonly string[] suffixes = { "", ".actors.jsonl", ".combat.jsonl", ".input.jsonl" };
            static readonly StringComparison pathComparison = Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            readonly object owner;
            readonly string trace;
            readonly string root;
            readonly Dictionary<string, StreamWriter> writers =
                new Dictionary<string, StreamWriter>(StringComparer.Ordinal);
            bool disposed;
            int openedStreamCount;
            internal int OpenedStreamCount => openedStreamCount;
            internal OwnedTraceAppendSession(object owner, string directory, string trace)
            {
                if (owner == null) throw new ArgumentNullException(nameof(owner));
                this.owner = owner;
                root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
                this.trace = Path.GetFullPath(trace);
                if (!string.Equals(Path.GetDirectoryName(this.trace), root, pathComparison))
                    throw new InvalidDataException("Trace must be directly inside the owned artifact directory.");
                // No new directories, arbitrary names, or inferred attempt ownership.
                ValidateAncestors();
            }
            void ValidateAncestors()
            {
                // Fresh observations at each of at most four stream opens, not a cached stat proof.
                for (var current = new DirectoryInfo(root); current != null; current = current.Parent)
                {
                    FileAttributes attributes = File.GetAttributes(current.FullName);
                    if ((attributes & FileAttributes.Directory) == 0 ||
                        (attributes & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidDataException("Reparse/non-directory diagnostic ancestor rejected.");
                }
            }
            internal bool Matches(object owner, string trace) =>
                !disposed && ReferenceEquals(this.owner, owner) &&
                string.Equals(this.trace, Path.GetFullPath(trace), StringComparison.Ordinal);

            internal void Append(string path, string text)
            {
                if (disposed) throw new ObjectDisposedException(nameof(OwnedTraceAppendSession));
                try
                {
                    string full = Path.GetFullPath(path);
                    // Return canonical key: aliases cannot open additional streams.
                    string key = null;
                    foreach (string suffix in suffixes)
                        if (string.Equals(full, trace + suffix, pathComparison)) { key = trace + suffix; break; }
                    if (key == null) throw new InvalidDataException("Unowned diagnostic output rejected.");
                    if (!writers.TryGetValue(key, out var writer))
                    {
                        if (writers.Count >= 4) throw new InvalidOperationException("Four diagnostic streams maximum.");
                        ValidateAncestors();
                        try
                        {
                            if ((File.GetAttributes(key) & FileAttributes.ReparsePoint) != 0)
                                throw new InvalidDataException("Reparse diagnostic output rejected.");
                        }
                        catch (FileNotFoundException) { } // Only absent leaf permits creation. No File.Exists suppression.
                        FileStream stream = null;
                        try
                        {
                            stream = new FileStream(key, FileMode.Append, FileAccess.Write, FileShare.Read);
                            writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false), 1024);
                            writers.Add(key, writer);
                            openedStreamCount++;
                            stream = null; // Dictionary owns the writer after successful insertion.
                        }
                        finally { stream?.Dispose(); }
                    }
                    writer.Write(text);
                    writer.Flush(); // Same per-append visibility; no delayed evidence buffer.
                }
                catch
                {
                    try { Dispose(); } catch (OutOfMemoryException) { throw; } catch { /* Retain the original failure. */ }
                    throw;
                }
            }
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                Exception first = null;
                foreach (var writer in writers.Values)
                    try { writer.Dispose(); } catch (Exception failure) { if (first == null || failure is OutOfMemoryException) first = failure; }
                writers.Clear();
                if (first != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw();
            }
        }

        // Explicit, bounded editor-only attribution. Default evidence and gameplay are unchanged.
        [Serializable] private sealed class EditorTraceTimingRow
        {
            public int frame;
            public float simulationElapsed;
            public double observationTime, overallMs, nativeSnapshotMs, directoryMs, baseQueriesMs,
                unitQueryMs, traceSerializeMs, traceWriteMs, actorQueryMs, actorCollectSerializeMs,
                actorWriteMs, combatCollectMs, combatSerializeMs, combatWriteMs,
                inputCollectMs, inputSerializeMs, inputWriteMs;
            public bool completed;
        }
        [Serializable] private sealed class EditorTraceTimingEnvelope
        {
            public int schema=1, maximumSamples=1024, omittedSamples;
            public string scope="EditorAppendTraceSubstepAttributionOnly", tracePath, closeReason;
            public bool gameplayChanged=false, qualificationClaim=false;
            public EditorTraceTimingRow[] samples;
        }
        private static class EditorTraceTimings
        {
            internal const int MaximumSamples=1024;
            internal static EditorTraceTimingRow Current;
            // Cached once per editor domain; fresh owned diagnostic runs use fresh Editors.
            static readonly bool enabled=string.Equals(Environment.GetEnvironmentVariable("WARLINE_EDITOR_TRACE_TIMINGS"),"1",StringComparison.Ordinal);
            static readonly System.Collections.Generic.List<EditorTraceTimingRow> rows=new System.Collections.Generic.List<EditorTraceTimingRow>();
            static string trace;
            static int omitted;
            static bool armed, closed;
            internal static long Now()=>System.Diagnostics.Stopwatch.GetTimestamp();
            internal static double Since(ref long start)
            {
                long end=Now();double milliseconds=(end-start)*1000d/System.Diagnostics.Stopwatch.Frequency;
                start=end;return milliseconds;
            }
            internal static EditorTraceTimingRow Begin(float elapsed)
            {
                if(!enabled||closed)return null;
                if(!armed)
                {
                    trace=Path.GetFullPath(LiveTracePath());
                    string allowed=Path.GetFullPath(EvidenceDirectory()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                    if(!trace.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Trace timing requires existing owned evidence path");
                    armed=true;
                    EditorApplication.playModeStateChanged+=OnPlayMode;
                    EditorApplication.quitting+=OnQuit;
                }
                if(rows.Count>=MaximumSamples){omitted++;return null;}
                return Current=new EditorTraceTimingRow{frame=Time.frameCount,simulationElapsed=elapsed,observationTime=Time.realtimeSinceStartupAsDouble};
            }
            internal static void Complete(EditorTraceTimingRow row,long start,bool completed)
            {
                Current=null;
                if(row==null)return;
                row.overallMs=Since(ref start);row.completed=completed;rows.Add(row);
            }
            static void OnPlayMode(PlayModeStateChange state)
            {
                if(state==PlayModeStateChange.ExitingPlayMode)Flush("ExitingPlayMode");
            }
            static void OnQuit()=>Flush("EditorQuitting");
            internal static void Flush(string reason)
            {
                if(!enabled||!armed||closed)return;
                closed=true;
                EditorApplication.playModeStateChanged-=OnPlayMode;
                EditorApplication.quitting-=OnQuit;
                try
                {
                    var envelope=new EditorTraceTimingEnvelope{tracePath=trace,closeReason=reason,
                        omittedSamples=omitted,samples=rows.ToArray()};
                    byte[] bytes=new System.Text.UTF8Encoding(false).GetBytes(JsonUtility.ToJson(envelope));
                    if(bytes.Length>2*1024*1024)throw new InvalidDataException("Bounded trace timing output exceeds2MiB");
                    using(var stream=new FileStream(trace+".editor-timings.json",FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                        stream.Write(bytes,0,bytes.Length);
                    Debug.Log("[EditorAppendTraceTimings] result=Captured samples="+rows.Count+" omitted="+omitted+
                        " scope=DiagnosticOnly nativeEvidenceUnchanged=1");
                }
                catch(Exception failure) when(!(failure is OutOfMemoryException))
                {
                    Debug.LogWarning("[EditorAppendTraceTimings] result=Unavailable reason="+failure.GetType().Name+
                        " scope=DiagnosticOnly nativeEvidenceUnchanged=1");
                }
                finally{rows.Clear();}
            }
        }

        [Serializable] private sealed class CombatTargetDiagnostic
        {
            public int index, version;
            public bool exists, owned, sessionMatches, hasTransform, hasHealth, hasPolicy;
            public string stableId, sessionId, role;
            public byte faction, nativeFaction, isStructure, domain;
            public bool hasFaction;
            public Vector3 position;
            public float health, distance;
            public bool fogEvaluated, fogVisible, policyEvaluated, policyAllows;
            // These are sampled predicates, not the historical attack-job branch.
            public bool losEvaluated, nativeRejectionCollected;
        }
        [Serializable] private sealed class ActorCombatDiagnostic
        {
            public int frame;
            public float elapsed;
            public string id, sessionId, role;
            public byte faction, nativeFaction;
            public bool hasFaction, hasAcceptedIntent;
            public byte acceptedIntentOrder, engageCommanded;
            public int acceptedTargetIndex, acceptedTargetVersion;
            public int2 engageCell;
            public Vector3 engagePosition;
            public Vector3 position;
            public bool hasAttackMove, hasEngage, hasAttack, hasCombat, capReached;
            public int2 moveDestination;
            public Vector3 movePosition;
            public float attackRange;
            public int damage, aggroRangeCells;
            public byte canAttack, autoEngage, allowedDomains;
            public CombatTargetDiagnostic engagedTarget, designatedEnemyBase;
        }
        private static CombatTargetDiagnostic ReadCombatTargetDiagnostic(
            EntityManager em, Entity actor, in SkirmishAttemptOwnedComponent owner, Entity target)
        {
            var result = new CombatTargetDiagnostic
            { index = target.Index, version = target.Version, exists = em.Exists(target) };
            if (!result.exists) return result;
            result.hasFaction = em.HasComponent<Faction>(target);
            if (result.hasFaction) result.nativeFaction = em.GetComponentData<Faction>(target).Id;
            result.owned = em.HasComponent<SkirmishAttemptOwnedComponent>(target);
            if (result.owned)
            {
                var targetOwner = em.GetComponentData<SkirmishAttemptOwnedComponent>(target);
                result.stableId = targetOwner.StableObjectId.ToString();
                result.sessionId = targetOwner.SessionId.ToString();
                result.sessionMatches = targetOwner.SessionId.Equals(owner.SessionId);
                result.faction = targetOwner.FactionId; result.isStructure = targetOwner.IsStructure;
                result.fogEvaluated = true;
                result.fogVisible = SkirmishFogService.IsVisibleToFaction(em, target, owner.FactionId);
            }
            if (em.HasComponent<SkirmishUnitRoleComponent>(target))
                result.role = em.GetComponentData<SkirmishUnitRoleComponent>(target).Role.ToString();
            result.hasTransform = em.HasComponent<LocalTransform>(target);
            if (result.hasTransform)
            {
                result.position = em.GetComponentData<LocalTransform>(target).Position;
                result.distance = math.distance(em.GetComponentData<LocalTransform>(actor).Position.xz,
                    new float2(result.position.x, result.position.z));
            }
            result.hasHealth = em.HasComponent<UnitHealth>(target);
            if (result.hasHealth) result.health = em.GetComponentData<UnitHealth>(target).Current;
            result.hasPolicy = em.HasComponent<CombatTargetPolicy>(target);
            if (result.hasPolicy)
            {
                var policy = em.GetComponentData<CombatTargetPolicy>(target);
                result.domain = (byte)policy.Domain;
                if (em.HasComponent<CombatTargetPolicy>(actor))
                {
                    result.policyEvaluated = true;
                    if (result.owned) policy.Visible = 1;
                    result.policyAllows = (!result.owned || (result.sessionMatches && result.fogVisible)) &&
                        CombatTargetPolicyUtility.Allows(true, em.GetComponentData<CombatTargetPolicy>(actor), true, policy);
                }
            }
            return result;
        }
        private static ActorCombatDiagnostic ReadActorCombatDiagnostic(
            EntityManager em, Entity actor, in SkirmishAttemptOwnedComponent owner,
            ActorDiagnostic observed, Entity enemyBase)
        {
            var result = new ActorCombatDiagnostic
            {
                frame = observed.frame, elapsed = observed.elapsed, id = observed.id, role = observed.role,
                faction = owner.FactionId, sessionId = owner.SessionId.ToString(), position = observed.position,
                hasAttackMove = em.HasComponent<AttackMoveOrder>(actor),
                hasEngage = em.HasComponent<EngageTarget>(actor),
                hasAttack = em.HasComponent<UnitAttack>(actor), hasCombat = em.HasComponent<UnitCombat>(actor)
            };
            result.hasFaction = em.HasComponent<Faction>(actor);
            if (result.hasFaction) result.nativeFaction = em.GetComponentData<Faction>(actor).Id;
            result.hasAcceptedIntent = em.HasComponent<SkirmishMoveIntentComponent>(actor);
            if (result.hasAcceptedIntent)
            {
                var intent = em.GetComponentData<SkirmishMoveIntentComponent>(actor);
                result.acceptedIntentOrder = (byte)intent.Order;
                result.acceptedTargetIndex = intent.AttackTarget.Index; result.acceptedTargetVersion = intent.AttackTarget.Version;
            }
            if (result.hasEngage)
            {
                var engage = em.GetComponentData<EngageTarget>(actor);
                result.engageCell = engage.Cell; result.engagePosition = engage.Position; result.engageCommanded = engage.IsCommanded;
            }
            if (result.hasAttackMove)
            {
                var order = em.GetComponentData<AttackMoveOrder>(actor);
                result.moveDestination = order.Destination; result.movePosition = order.Position;
            }
            if (result.hasAttack)
            {
                var attack = em.GetComponentData<UnitAttack>(actor);
                result.attackRange = attack.Range; result.damage = attack.Damage;
            }
            if (result.hasCombat)
            {
                var combat = em.GetComponentData<UnitCombat>(actor);
                result.aggroRangeCells = combat.AggroRangeCells;
                result.canAttack = combat.CanAttack; result.autoEngage = combat.AutoEngage;
            }
            if (em.HasComponent<CombatTargetPolicy>(actor))
                result.allowedDomains = (byte)em.GetComponentData<CombatTargetPolicy>(actor).AllowedTargets;
            result.engagedTarget = ReadCombatTargetDiagnostic(em, actor, owner,
                result.hasEngage ? em.GetComponentData<EngageTarget>(actor).Target : Entity.Null);
            result.designatedEnemyBase = ReadCombatTargetDiagnostic(em, actor, owner, enemyBase);
            return result;
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
            public int frame, retryResumeFrame;
            public bool hasGrid, hasTarget, hasPathRequest, hasPathFollow, hasPathRange, hasRetryCooldown, hasLongDistanceMove;
            public int2 gridCell, targetCell, requestedGoal, haulGoal, longDistanceGoal;
            public float haulActionEndsAt;
            public bool engaged, stationary, air;
            public bool hauling;
            public int haulSource, haulDestination, haulPhase, haulStatus, haulReason;
            public float cargoOil, cargoFuel;
        }

        private static void AppendActorDiagnostic(EntityManager em, float elapsed, Entity enemyBase = default)
        {
            AppendActorDiagnosticToPath(em, elapsed, enemyBase, null);
        }

        private static void AppendActorDiagnosticToPath(EntityManager em, float elapsed, Entity enemyBase, string ownedTracePath)
        {
            var traceTiming=ownedTracePath==null?EditorTraceTimings.Current:null;
            long traceTimingTick=traceTiming!=null?EditorTraceTimings.Now():0;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishUnitRoleComponent), typeof(LocalTransform));
            using var entities = query.ToEntityArray(Allocator.Temp);
            if(traceTiming!=null)traceTiming.actorQueryMs=EditorTraceTimings.Since(ref traceTimingTick);
            var lines = new System.Text.StringBuilder();
            var combatSamples = Game.UI.Runtime.SkirmishMatchView.NativeCombatEvidenceEnabled
                ? new System.Collections.Generic.List<ActorCombatDiagnostic>(16) : null;
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
                sample.frame = Time.frameCount;
                sample.hasGrid = em.HasComponent<UnitGrid>(unit);
                if (sample.hasGrid) sample.gridCell = em.GetComponentData<UnitGrid>(unit).Cell;
                sample.hasTarget = em.HasComponent<UnitTarget>(unit);
                if (sample.hasTarget) sample.targetCell = em.GetComponentData<UnitTarget>(unit).Cell;
                sample.hasPathRequest = em.HasComponent<UnitPathRequest>(unit);
                if (sample.hasPathRequest) sample.requestedGoal = em.GetComponentData<UnitPathRequest>(unit).Goal;
                sample.hasPathFollow = em.HasComponent<UnitPathFollow>(unit);
                sample.hasPathRange = em.HasComponent<UnitPathRange>(unit);
                sample.hasRetryCooldown = em.HasComponent<UnitPathRetryCooldown>(unit);
                if (sample.hasRetryCooldown) sample.retryResumeFrame = em.GetComponentData<UnitPathRetryCooldown>(unit).ResumeFrame;
                sample.hasLongDistanceMove = em.HasComponent<UnitLongDistanceMove>(unit);
                if (sample.hasLongDistanceMove) sample.longDistanceGoal = em.GetComponentData<UnitLongDistanceMove>(unit).FinalGoal;
                sample.hauling = em.HasComponent<UnitResourceHaulOrder>(unit);
                if (sample.hauling)
                {
                    var haul = em.GetComponentData<UnitResourceHaulOrder>(unit);
                    sample.haulSource = haul.SourceBuildingId; sample.haulDestination = haul.DestinationBuildingId;
                    sample.haulPhase = haul.Phase;
                    sample.haulGoal = haul.TargetCell; sample.haulActionEndsAt = haul.ActionEndsAt;
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
                if (combatSamples != null && owned.FactionId == 1 && owned.IsStructure == 0 &&
                    em.HasComponent<SkirmishRoleOverlayComponent>(unit))
                {
                    var overlay = em.GetComponentData<SkirmishRoleOverlayComponent>(unit);
                    if (overlay.Damage > 0 && (overlay.TargetDomains & SkirmishTargetDomain.Structure) != 0)
                    {
                        if (combatSamples.Count < 16)
                        {
                            long combatStart=traceTiming!=null?EditorTraceTimings.Now():0;
                            combatSamples.Add(ReadActorCombatDiagnostic(em, unit, owned, sample, enemyBase));
                            if(traceTiming!=null)traceTiming.combatCollectMs+=EditorTraceTimings.Since(ref combatStart);
                        }
                        else combatSamples[15].capReached = true;
                    }
                }
            }

            if(traceTiming!=null)traceTiming.actorCollectSerializeMs=EditorTraceTimings.Since(ref traceTimingTick);
            if(traceTiming!=null)traceTiming.actorCollectSerializeMs-=traceTiming.combatCollectMs;
            OwnedDiagnosticTraceIo.Append((ownedTracePath ?? LiveTracePath()) + ".actors.jsonl", lines.ToString(), ownedTracePath == null);
            if(traceTiming!=null)traceTiming.actorWriteMs=EditorTraceTimings.Since(ref traceTimingTick);
            if (combatSamples != null && combatSamples.Count > 0)
            {
                var combatLines = new System.Text.StringBuilder();
                foreach (var combatSample in combatSamples)
                    combatLines.AppendLine(JsonUtility.ToJson(combatSample));

            if(traceTiming!=null)traceTiming.combatSerializeMs=EditorTraceTimings.Since(ref traceTimingTick);
                OwnedDiagnosticTraceIo.Append((ownedTracePath ?? LiveTracePath()) + ".combat.jsonl", combatLines.ToString(), ownedTracePath == null);
            if(traceTiming!=null)traceTiming.combatWriteMs=EditorTraceTimings.Since(ref traceTimingTick);
            }
        }

        private static int DesignatedBaseHp(EntityManager em, Entity session, byte faction)
        {
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return productionCanary?.Request.IsLegacyPreset==true ? SkirmishLegacyProductionIdentity.BaseHp(em,em.GetComponentData<SkirmishMatchState>(session),faction) : -1;
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
            OwnedDiagnosticTraceIo.CloseOwned("ResetLiveTrace");
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
            if(codedSizeCameraActive) { FinishCodedSizeCamera(false,abortReason);return; }
            StopS005GuardObservation();
            if (finishingEvidence) return;
            finishingEvidence = true;
            try { OwnedDiagnosticTraceIo.CloseOwned("Finish"); }
            catch (Exception ioError) when (!(ioError is OutOfMemoryException))
            { abort = true; abortReason = "diagnosticAppendClose:" + ioError.GetType().Name; Debug.LogError(ioError); }
            EditorApplication.update -= Tick;
            RestoreBackgroundInput();
            StopS004ManualProbe();
            StopS004AriaConsentProbe();
            StopS004MenuProbe();
            playerResumeProbeActive = false;
            playerResumeTouch?.Dispose(); playerResumeTouch = null;
            nativeProductionTouch?.Dispose(); nativeProductionTouch = null;
            terminalProbeActive = false;
            terminalTouch?.Dispose(); terminalTouch = null;
            helipadProbeStage = 0;
            helipadProbeTouch?.Dispose(); helipadProbeTouch = null;
            finishAbort = abort;
            finishAbortReason = abortReason;
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            // Hash every source byte, but keep the native Editor responsive while
            // reading the shared Campaign asset library. Unity APIs stay on the
            // Editor thread, and no receipt is written until this task completes.
            Debug.Log("[SkirmishCandidateFingerprint] phase=Start owner=Finish aborted=" + abort + " reason=" + abortReason);
            finishFingerprintTimer = System.Diagnostics.Stopwatch.StartNew();
            finishSourceHash = System.Threading.Tasks.Task.Run(() => SkirmishCandidateSourceHash.Compute(root));
            EditorApplication.update += PollFinishSourceHash;
        }

        private static void PollFinishSourceHash()
        {
            if (finishSourceHash == null || !finishSourceHash.IsCompleted) return;
            EditorApplication.update -= PollFinishSourceHash;
            var completed = finishSourceHash;
            finishSourceHash = null;
            finishFingerprintTimer?.Stop();
            Debug.Log("[SkirmishCandidateFingerprint] phase=Completed owner=Finish elapsedMilliseconds=" + (finishFingerprintTimer?.Elapsed.TotalMilliseconds ?? -1d).ToString("R", CultureInfo.InvariantCulture) + " taskStatus=" + completed.Status + (completed.Status == System.Threading.Tasks.TaskStatus.RanToCompletion ? " sha256=" + completed.Result : string.Empty));
            productionBatchFinishHashMilliseconds=finishFingerprintTimer?.Elapsed.TotalMilliseconds??-1;
            finishFingerprintTimer = null;
            if (completed.IsFaulted || completed.IsCanceled)
            {
                Debug.LogError("[SkirmishSourceFingerprint] result=Failed error=" + completed.Exception);
                FinishEvidenceOrScheduleOwnedFailure(true, "candidateSourceFingerprintFailed", string.Empty);
                return;
            }
            FinishEvidenceOrScheduleOwnedFailure(finishAbort, finishAbortReason, completed.Result);
        }


        // Evidence failures must not strand the already-latched owned Finish.
        private static void RunFinishEvidenceBoundary(Action evidence,Action<Exception> report,Action end,
            Action stop,Action exitPlay,Action requestFailedQuit)
        {
            try{evidence();}
            catch(Exception failure) when(!(failure is OutOfMemoryException))
            {
                try{report(failure);}
                finally
                {
                    try{end();}
                    finally
                    {
                        try{stop();}
                        finally
                        {
                            try{exitPlay();}
                            finally{requestFailedQuit();}
                        }
                    }
                }
            }
        }
        private static void FinishEvidenceOrScheduleOwnedFailure(bool abort,string reason,string source)
            =>RunFinishEvidenceBoundary(()=>FinishEvidence(abort,reason,source),
                error=>Debug.LogError("[SkirmishFinishEvidenceFailure] result=Failed original="+error),
                AriaCommandEvidence.End,()=>UiShellRuntimeGateway.StopAriaPlay(),
                ()=>EditorApplication.ExitPlaymode(),()=>RequestQuitIfAsked(2));

        private static void RequireOrdinaryRunLogDirectory(string directory)
        {
            for(var current=new DirectoryInfo(directory);current!=null;current=current.Parent)
            {
                var attributes=File.GetAttributes(current.FullName);
                if((attributes&FileAttributes.Directory)==0||(attributes&FileAttributes.ReparsePoint)!=0)
                    throw new InvalidDataException("Owned run-log ordinary directory ancestry required.");
            }
        }
        private static void RequireFreshRunLogDestination(string path)
        {
            try{File.GetAttributes(path);throw new IOException("Owned run-log destination already exists.");}
            catch(FileNotFoundException){}
            catch(DirectoryNotFoundException){}
        }
        // Same installed UTF8-no-BOM replacement bytes. Only short owned opens;
        // existing File.Copy transfers to/from the long actual evidence path.
        private static void WriteRunLogInOwnedAttempt(string attemptRoot,string candidate,string output,string text)
        {
            string root=Path.GetFullPath(attemptRoot).TrimEnd(Path.DirectorySeparatorChar);
            string destination=Path.GetFullPath(output),parent=Path.GetDirectoryName(destination);
            if(!System.Text.RegularExpressions.Regex.IsMatch(candidate??"","^[a-f0-9]{64}$")||
                !string.Equals(parent,Path.Combine(root,"candidates",candidate),StringComparison.Ordinal)||
                !System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(destination),"^[a-z0-9][a-z0-9_.-]*-log[.]txt$"))
                throw new InvalidDataException("Exact owned candidate run-log destination required.");
            RequireOrdinaryRunLogDirectory(parent);RequireFreshRunLogDestination(destination);
            string token=Guid.NewGuid().ToString("N");
            string temporary=Path.Combine(root,".run-log-"+token+".tmp");
            string verified=Path.Combine(root,".run-log-"+token+".verified");
            Exception originalFailure=null;
            try
            {
                RequireOrdinaryRunLogDirectory(root);
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                using(var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)))
                {writer.Write(text);writer.Flush();stream.Flush(true);}
                byte[] expected=File.ReadAllBytes(temporary);
                RequireOrdinaryRunLogDirectory(parent);RequireFreshRunLogDestination(destination);
                File.Copy(temporary,destination,false);
                if((File.GetAttributes(destination)&(FileAttributes.Directory|FileAttributes.ReparsePoint))!=0)
                    throw new InvalidDataException("Owned copied run-log must be an ordinary file.");
                RequireOrdinaryRunLogDirectory(root);
                File.Copy(destination,verified,false);
                byte[] actual=File.ReadAllBytes(verified);
                bool same=expected.Length==actual.Length;
                for(int i=0;same&&i<expected.Length;i++)same=expected[i]==actual[i];
                if(!same)throw new IOException("Owned copied run-log bytes differ.");
            }
            catch(Exception failure){originalFailure=failure;throw;}
            finally
            {
                Exception cleanupFailure=null;
                foreach(string path in new[]{verified,temporary})
                    try
                    {
                        if(!File.Exists(path))continue;
                        RequireOrdinaryRunLogDirectory(root);
                        if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)
                            throw new InvalidDataException("Owned temporary run-log reparse cleanup refused.");
                        File.Delete(path);
                    }
                    catch(Exception cleanup) when(!(cleanup is OutOfMemoryException))
                    {cleanupFailure=cleanupFailure??cleanup;Debug.LogError("[SkirmishRunLogTemporaryCleanup] result=Failed error="+cleanup);}
                // Preserve the primary error; cleanup failure cannot become success.
                if(originalFailure==null&&cleanupFailure!=null)throw cleanupFailure;
            }
        }


        private static void FinishEvidence(bool abort, string abortReason, string currentSourceHash)
        {
            OwnedDiagnosticTraceIo.CloseOwned("FinishEvidence");
            EditorTraceTimings.Flush("FinishEvidence");
            try { RecordProductionBatchFingerprint("End",currentSourceHash,productionBatchFinishHashMilliseconds); }
            catch(Exception fingerprintError) { abort=true;abortReason="productionBatchFingerprint:"+fingerprintError.GetType().Name;Debug.LogError(fingerprintError); }
            if (currentSourceHash != SessionState.GetString(CandidateHashKey, ""))
            {
                abort = true;
                abortReason = "candidateSourceChangedDuringRun";
            }
            SessionState.SetBool(Key, false);
            SessionState.SetFloat(PlayingSinceKey, 0f);
            RecordProductionProfileBeforeRestoreAndResetEnvironment();

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

            if (Environment.GetEnvironmentVariable("WARLINE_S005_PAID_BUILDING_CLEARANCE_ONLY") == "1")
            {
                bool passed = !abort && abortReason == "sharedPaidBuildingClearanceOnly";
                Debug.Log("[S005PaidBuildingClearanceCompletion] result=" + (passed ? "Passed" : "Failed") +
                    " reason=" + abortReason + " codeHash=" + currentSourceHash +
                    " scope=NativeStartingAndPaidBuildingClearance completeMatchCertified=0 countedWin=0");
                Environment.SetEnvironmentVariable("WARLINE_S005_PAID_BUILDING_CLEARANCE_ONLY", null);
                AriaCommandEvidence.End();
                EditorApplication.ExitPlaymode();
                RequestQuitIfAsked(passed ? 0 : 2);
                return;
            }

            if (Environment.GetEnvironmentVariable("WARLINE_S005_NATIVE_RECOVERY_ONLY") == "1")
            {
                bool passed = !abort && playerResumeProbeDone;
                Debug.Log("[S005NativeRecoveryCase] result=" + (passed ? "Passed" : "Failed") +
                    " reason=" + abortReason + " codeHash=" + currentSourceHash +
                    " scope=PaidBoardedPassengersSaveContinueUnload completeMatchCertified=0 countedWin=0");
                Environment.SetEnvironmentVariable("WARLINE_S005_NATIVE_RECOVERY_ONLY", null);
                EditorApplication.ExitPlaymode();
                RequestQuitIfAsked(passed ? 0 : 2);
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

            if (Environment.GetEnvironmentVariable("WARLINE_S004_MANUAL_PROBE") == "1")
            {
                bool cleanManualAudit = manualReceiptAuditMeasured && manualReceiptAuditViolations == 0 &&
                    manualUnexpectedSamples == 0 && manualPhysicalInterventions == 0;
                Debug.Log("[S004ManualReceiptAudit] result=" + (cleanManualAudit ? "Passed" : "Failed") +
                    " measured=" + (manualReceiptAuditMeasured ? 1 : 0) + " touches=" + manualCompletedGestures +
                    " commands=" + manualRecordedCommands + " violations=" + manualReceiptAuditViolations +
                    " unexpected=" + manualUnexpectedSamples + " interventions=" + manualPhysicalInterventions);
                bool passed = !abort && haveMatch && match.Outcome == SkirmishOutcome.Victory &&
                    terminalSnapshotValid && manualAttackReceipts > 0 && cleanManualAudit;
                ReportPaidGroundRoleObservation();
                AriaCommandEvidence.End();
                Debug.Log("[S004ManualNative] result=" + (passed ? "Passed" : "Failed") +
                    " outcome=" + match.Outcome + " seconds=" + duration + " reason=" + abortReason +
                    " input=Touch ariaStarted=0 actions=" + manualActions + " attackReceipts=" + manualAttackReceipts);
                EditorApplication.ExitPlaymode();
                RequestQuitIfAsked(passed ? 0 : 2);
                return;
            }

            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string runsPath = RunsCsvPath(root);
            string csv = File.Exists(runsPath) ? File.ReadAllText(runsPath) : SkirmishAcceptanceScaffold.RequiredHeader + "\n";
            string runId=null,idError=null;
            bool matrixJob=productionBatch?.Matrix!=null;
            if(matrixJob)
            {
                productionBatch.Matrix.ValidateJob(productionCanary.Request.Job);
                runId=catalogId+"-aria-matrix-"+(string)productionCanary.Request.Claim["attempt_id"];
            }
            if (!matrixJob&&!SkirmishS002AriaRunLog.TryNextAriaRunId(csv, catalogId, seed, locale, out runId, out idError))
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
            foreach (string suffix in new[] { ".input.jsonl", ".actors.jsonl", ".touch.jsonl", ".commands.jsonl", ".economy.jsonl" })
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
            bool nativeProvenance=!matrixJob;
            if(matrixJob)nativeProvenance=productionCanary.TryApplyNativeProvenance(ref facts,SessionState.GetString(CandidateHashKey,""));
            else if (TryReadCensus(root, out SkirmishAcceptanceCensus census))
            {
                facts.DefinitionVersion = census.DefinitionVersion;
                facts.CodeHash = census.CodeHash;
                facts.ConfigHash = census.ConfigHash;
            }

            bool legacyJob=productionCanary?.Request.IsLegacyPreset==true;
            string row,error;
            bool appended = legacyJob ? TryAppendLegacyProductionTerminal(runsPath,in facts,out row,out error) : SkirmishS002AriaRunLog.TryAppendTerminalRow(runsPath, in facts, out row, out error);
            bool counted = !legacyJob && appended && nativeProvenance && (productionBatch==null||productionBatch.RuntimeErrorsClean) && SkirmishS002AriaRunLog.IsCountedAriaWin(in facts, RowResult(row));
            string log = string.Format(
                CultureInfo.InvariantCulture,
                "[SkirmishS002AriaRun] appended={0} countedWin={1} error={2}\n{3}\n",
                appended ? 1 : 0,
                counted ? 1 : 0,
                error ?? string.Empty,
                row ?? string.Empty);
            if(productionCanary==null)File.WriteAllText(logAbsolute,log);
            else WriteRunLogInOwnedAttempt(productionCanary.Request.ArtifactDirectory,
                (string)productionCanary.Request.Job["candidate_sha256"],logAbsolute,log);
            if (appended)
                Debug.Log(log.Trim());
            else
                Debug.LogError(log.Trim());

            AriaCommandEvidence.End();
            UiShellRuntimeGateway.StopAriaPlay();
            EditorApplication.ExitPlaymode();
            bool operational=matrixJob&&(productionBatch==null||productionBatch.RuntimeErrorsClean)&&(legacyJob ? SkirmishLegacyProductionIdentity.IsOperationallyComplete(in facts,appended,terminalSnapshotValid,nativeProvenance&&productionCanary.LegacySavedResultVerified) : SkirmishProductionMatrixAuthority.IsOperationallyComplete(in facts,appended,terminalSnapshotValid));
            if(matrixJob)Debug.Log("[SkirmishProductionOperationalRun] result="+(operational?"Completed":"Failed")+
                " outcome="+outcome+" countedWin="+(counted?1:0)+" queueSuccessEnabled=0");
            RequestQuitIfAsked(matrixJob?(operational?0:2):(counted?0:2));
        }

        private static bool TryReadCensus(string root, out SkirmishAcceptanceCensus census)
        {
            census = null;
            if(productionBatch?.Matrix!=null)return false; // Matrix records use verified native setup observations, never the S002 fallback.
            if (!SkirmishSetupMatrixTable.TryLoad(root, out List<SkirmishSetupMatrixRow> matrix, out _))
                return false;
            var packaged = SkirmishExpansionCatalogConfig.Load();
            if (packaged == null) return false;
            SkirmishExpansionAuthoredSet authored = packaged.ToAuthoredSet();
            if (catalogId == "S005")
            {
                bool captured = SkirmishAcceptanceCensusCapture.TryCaptureCatalog(authored, catalogId, matrix,
                    SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, seed, out census, out _);
                if (captured) census.CodeHash = SessionState.GetString(CandidateHashKey, "");
                return captured;
            }
            if (catalogId == SkirmishAcceptanceCensusCapture.S004CatalogId)
            {
                bool captured = SkirmishAcceptanceCensusCapture.TryCaptureS004RegularStandard(
                    authored, matrix, seed, out census, out _);
                if (captured) census.CodeHash = SessionState.GetString(CandidateHashKey, "");
                return captured;
            }
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
            try
            {
                CompleteCodedSizeCameraAfterOwnedPlay();
                RestoreS004EditorJourneyOptions();
                RestoreProductionCanaryAfterOwnedPlay();
            }
            catch (Exception cleanupError)
            {
                exitCode = 2;
                Debug.LogError("[SkirmishProductionCanaryCleanup] result=Failed error=" + cleanupError);
                // Preserve context/artifacts for investigation; complete wrapper as failed.
            }
            if(TryCompleteProductionBatchJob(exitCode)) return;
            Debug.Log("[SkirmishValidationExit] result=Requested code=" + exitCode);
            if (ExistingEditorValidation.IsRunning || Array.IndexOf(Environment.GetCommandLineArgs(), "-executeMethod") < 0)
            {
                MissionEditorValidationExit.Complete(exitCode == 0);
                return;
            }
            productionBatchExitOwner?.RecordActualExitInvocation(exitCode);
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
