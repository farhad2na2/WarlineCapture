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
    public static class SkirmishS002AriaRunHarness
    {
        private const string Key = "Warline.S002.AriaRun";
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
            stage = 0;
            next = 0d;
            deadline = 0d;
            playingSince = 0d;
            speedLatch = SkirmishS002NormalSpeedLatch.Start();
            normalSpeed = true;
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
            // One live jsonl per seed and locale. Finish copies that whole file
            // into the run evidence, so each launch truncates it. A domain reload
            // resumes Tick without calling Launch and keeps the in-progress run.
            ResetLiveTrace(LiveTracePath());
            EditorApplication.update += Tick;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
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
            string relative = catalogId == SkirmishAcceptanceCensusCapture.S003CatalogId
                ? Path.Combine(SkirmishAcceptanceScaffold.S003RelativeReportEvidenceDirectory, "..", SkirmishAcceptanceScaffold.RunsFileName)
                : SkirmishAcceptanceScaffold.RelativeRunsPath;
            return Path.GetFullPath(Path.Combine(root, relative));
        }

        private static string ValidationProfileRoot()
        {
            return Path.Combine(Path.GetTempPath(), "aria-play-validation-profile");
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
                return;
            speedLatch = SkirmishS002AriaRunLog.ObserveTimeScale(speedLatch, Time.timeScale);
            if (Time.timeScale != 1f)
                Time.timeScale = 1f;
            if (!speedLatch.Normal)
                SessionState.SetBool(NormalKey, false);
            normalSpeed = speedLatch.Normal;

            double now = EditorApplication.timeSinceStartup;
            if (deadline == 0d)
                deadline = now + WallClockBudgetSeconds;
            if (now > deadline)
            {
                Finish(abort: true, abortReason: "timeout");
                return;
            }

            if (now < next)
                return;

            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated)
                    return;
                EntityManager em = world.EntityManager;
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
                SkirmishLaunchProjection.TryEnterMatch(em);
                SkirmishLaunchProjection.TryBeginGameplayIfLoaded();
                SkirmishLaunchProjection.TryRequestPlay(em);
                SkirmishLaunchProjection.TryArmExpandedSimulation(em);
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

                    SessionState.SetString(StartedKey, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
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
                    if (!UiShellRuntimeGateway.TryStartAriaPlay())
                    {
                        next = now + 0.5d;
                        return;
                    }

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
                // Stale-frame and unfocus drops return the touch driver to Manual.
                // The shipping start call is legal from Manual; keep it armed until
                // the match itself finishes. This does not write a result.
                AriaPlayModel aria = UiShellRuntimeGateway.ReadAriaPlay();
                if (aria.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked)
                    UiShellRuntimeGateway.TryStartAriaPlay();
                if (match.Phase == SkirmishPhase.Finished)
                {
                    Finish(abort: false, abortReason: null);
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
                "[SkirmishS002AriaRun] queued catalog={0} seed={1} hash={2:X8} playable=1 gatesAssault=0 normal_speed=1",
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
                string.Format(CultureInfo.InvariantCulture, "{0}-aria-live-{1}-{2}.jsonl", catalogToken, seed, token));
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
            SessionState.SetBool(Key, false);
            SessionState.SetFloat(PlayingSinceKey, 0f);
            string previous = SessionState.GetString("Warline.AriaPlayValidation", "");
            Environment.SetEnvironmentVariable(
                "WARLINE_VALIDATION_SAVE_ROOT",
                string.IsNullOrEmpty(previous) ? null : previous);
            string backgroundPrevious = SessionState.GetString(BackgroundPreviousKey, "");
            Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",
                string.IsNullOrEmpty(backgroundPrevious) ? null : backgroundPrevious);

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
            string evidenceRelative = catalogId == SkirmishAcceptanceCensusCapture.S003CatalogId
                ? SkirmishAcceptanceScaffold.S003RelativeReportEvidenceDirectory
                : SkirmishAcceptanceScaffold.RelativeReportEvidenceDirectory;
            string traceRelative = evidenceRelative + "/" + evidenceName + ".jsonl";
            string logRelative = evidenceRelative + "/" + evidenceName + "-log.txt";
            string traceAbsolute = Path.Combine(root, traceRelative.Replace('/', Path.DirectorySeparatorChar));
            string logAbsolute = Path.Combine(root, logRelative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(EvidenceDirectory());
            CopyLiveTraceToEvidence(LiveTracePath(), traceAbsolute);

            double wall = playingSince > 0d ? EditorApplication.timeSinceStartup - playingSince : 0d;
            normalSpeed = SkirmishS002AriaRunLog.FinishNormalSpeed(speedLatch, Time.timeScale, duration, wall);
            if (!normalSpeed)
                SessionState.SetBool(NormalKey, false);
            string outcome = haveMatch ? match.Outcome.ToString() : string.Empty;
            if (haveMatch && match.Outcome == SkirmishOutcome.None)
                outcome = string.Empty;
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
                InputViolations = 0,
                HumanInterventions = 0,
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
                return SkirmishAcceptanceCensusCapture.TryCaptureS003RegularStandard(
                    authored,
                    matrix,
                    seed,
                    out census,
                    out _);
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
            EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
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
