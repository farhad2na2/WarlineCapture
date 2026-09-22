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
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public delegate bool SkirmishExpandedAriaPayloadFactory(
        int seed,
        string locale,
        out SkirmishAriaAcceptancePayload payload,
        out string error);

    /// <summary>
    /// Mission ids, seeds, and report paths for one expanded ARIA watch.
    /// S002, S003, S004, and S005 share <see cref="SkirmishExpandedAriaWatchDriver"/>; they do not open a second watch path.
    /// </summary>
    public sealed class SkirmishExpandedAriaWatchProfile
    {
        public string CatalogId;
        public string ActiveKey;
        public string SeedKey;
        public string LocaleKey;
        public string StartedKey;
        public string NormalKey;
        public string PlayingSinceKey;
        public string EnvironmentSeedVariable;
        public string EnvironmentLocaleVariable;
        public string LogTag;
        public int DefaultSeed;
        public string EvidenceDirectoryRelative;
        public string RunsPathRelative;
        public string LiveTraceFormat;
        public string InvalidSeedError;
        public Func<int, bool> IsFirstVisitSeed;
        public SkirmishExpandedAriaPayloadFactory TryCreatePayload;
    }

    /// <summary>
    /// Launches expanded Regular Standard and starts ARIA through the shipping touch driver.
    /// Appends a runs.csv row only after the match finishes or the harness aborts. Never stamps Victory.
    /// After Playing, <see cref="SkirmishExpandedAriaFailFast"/> aborts with simulationNotAdvancing
    /// within about 45s when simulation is inactive or match elapsed stays at 0.
    /// </summary>
    public sealed class SkirmishExpandedAriaWatchDriver
    {
        private const string RegistryPath = "Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset";
        private const string ValidationSessionKey = "Warline.AriaPlayValidation";
        private const double WallClockBudgetSeconds = 1500d;

        private readonly SkirmishExpandedAriaWatchProfile profile;
        private int stage;
        private double next;
        private double deadline;
        private double playingSince;
        private bool normalSpeed = true;
        private int seed;
        private string locale = GameLocalization.EnglishLocaleCode;

        public SkirmishExpandedAriaWatchDriver(SkirmishExpandedAriaWatchProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            this.profile = profile;
            seed = profile.DefaultSeed;
        }

        public void ArmIfSessionPending()
        {
            if (!SessionState.GetBool(profile.ActiveKey, false))
                return;

            seed = SessionState.GetInt(profile.SeedKey, profile.DefaultSeed);
            locale = SessionState.GetString(profile.LocaleKey, GameLocalization.EnglishLocaleCode);
            normalSpeed = SessionState.GetBool(profile.NormalKey, true);
            playingSince = SessionState.GetFloat(profile.PlayingSinceKey, 0f);
            EditorApplication.update += Tick;
        }

        public void LaunchFromEnvironment()
        {
            string seedText = Environment.GetEnvironmentVariable(profile.EnvironmentSeedVariable);
            string localeText = Environment.GetEnvironmentVariable(profile.EnvironmentLocaleVariable);
            if (!int.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                parsed = profile.DefaultSeed;
            if (string.IsNullOrEmpty(localeText))
                localeText = GameLocalization.EnglishLocaleCode;
            Launch(parsed, localeText);
        }

        public void Launch(int requestedSeed, string requestedLocale)
        {
            if (profile.TryCreatePayload == null ||
                !profile.TryCreatePayload(requestedSeed, requestedLocale, out _, out string error))
                throw new InvalidOperationException(error ?? "ARIA payload was refused.");
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit play mode before launching " + profile.CatalogId + " ARIA watch.");

            seed = requestedSeed;
            locale = requestedLocale;
            stage = 0;
            next = 0d;
            deadline = 0d;
            playingSince = 0d;
            normalSpeed = true;
            SessionState.SetBool(profile.ActiveKey, true);
            SessionState.SetBool(profile.NormalKey, true);
            SessionState.SetInt(profile.SeedKey, seed);
            SessionState.SetString(profile.LocaleKey, locale);
            SessionState.SetString(profile.StartedKey, string.Empty);
            SessionState.SetFloat(profile.PlayingSinceKey, 0f);
            Directory.CreateDirectory(EvidenceDirectory());

            string profileRoot = ValidationProfileRoot();
            SessionState.SetString(ValidationSessionKey,
                Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT") ?? "");
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT", profileRoot);
            var save = new SaveService(new JsonSaveRepository(profileRoot));
            var loaded = save.LoadProfile();
            loaded.firstLaunchStatus = FirstLaunchProfileState.Completed;
            loaded.firstLaunchWatched = true;
            save.SaveProfile(loaded);

            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            EditorApplication.EnterPlaymode();
        }

        public string DescribeLaunch(int requestedSeed, string requestedLocale)
        {
            if (profile.TryCreatePayload == null ||
                !profile.TryCreatePayload(requestedSeed, requestedLocale, out SkirmishAriaAcceptancePayload payload, out string error))
                throw new InvalidOperationException(error ?? "ARIA payload was refused.");
            return payload.FormatSelectedConfiguration();
        }

        public string EvidenceDirectory()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(root, profile.EvidenceDirectoryRelative);
        }

        private static string ValidationProfileRoot()
        {
            return Path.Combine(Path.GetTempPath(), "aria-play-validation-profile");
        }

        private void Tick()
        {
            if (!EditorApplication.isPlaying)
                return;
            if (Time.timeScale != 1f)
            {
                normalSpeed = false;
                SessionState.SetBool(profile.NormalKey, false);
                Time.timeScale = 1f;
            }

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
                    if (profile.TryCreatePayload == null ||
                        !profile.TryCreatePayload(seed, locale, out SkirmishAriaAcceptancePayload payload, out _))
                        return;
                    Debug.Log(profile.LogTag + " selected " + payload.FormatSelectedConfiguration());
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
                        SessionState.SetFloat(profile.PlayingSinceKey, (float)playingSince);
                    }

                    // Expanded spawn marks Playing before LoadingGate arms simulation.
                    // Wait for SimulationActive so ARIA is not started against a frozen clock.
                    if (!simulationActive)
                    {
                        if (SkirmishExpandedAriaFailFast.IsSimulationNotAdvancing(
                                true,
                                false,
                                elapsed,
                                SkirmishExpandedAriaFailFast.WallSecondsSincePlaying(now, playingSince)))
                        {
                            Finish(abort: true, abortReason: SkirmishExpandedAriaFailFast.AbortReasonSimulationNotAdvancing);
                            return;
                        }

                        next = now + 0.5d;
                        return;
                    }

                    SessionState.SetString(
                        profile.StartedKey,
                        DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
                    stage = 2;
                    next = now + 1d;
                    return;
                }

                if (playingSince > 0d &&
                    SkirmishExpandedAriaFailFast.IsSimulationNotAdvancing(
                        match.Phase == SkirmishPhase.Playing,
                        simulationActive,
                        elapsed,
                        SkirmishExpandedAriaFailFast.WallSecondsSincePlaying(now, playingSince)))
                {
                    Finish(abort: true, abortReason: SkirmishExpandedAriaFailFast.AbortReasonSimulationNotAdvancing);
                    return;
                }

                if (stage == 2)
                {
                    if (!UiShellRuntimeGateway.TryStartAriaPlay())
                    {
                        next = now + 0.5d;
                        return;
                    }

                    Debug.Log(profile.LogTag + " watchStarted=1 seed=" + seed.ToString(CultureInfo.InvariantCulture) + " locale=" + locale);
                    stage = 3;
                    next = now + 1d;
                    return;
                }

                AppendTrace(em, session, in match, simulationActive, elapsed);
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

        private bool TryQueueExpanded(EntityManager em)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!SkirmishSetupMatrixTable.TryLoad(root, out List<SkirmishSetupMatrixRow> matrix, out string error))
                throw new InvalidOperationException(error);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            if (!SkirmishExpansionCatalogFactory.TryGetDefinition(
                    authored,
                    profile.CatalogId,
                    out SkirmishScenarioDefinitionConfig definition))
                throw new InvalidOperationException(profile.CatalogId + " definition is missing.");
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
                    profile.CatalogId,
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
                throw new InvalidOperationException(reasons == null || reasons.Count == 0 ? profile.CatalogId + " queue failed" : reasons[0].ToString());

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "{0} queued catalog={1} seed={2} hash={3:X8} playable=0 normal_speed=1",
                profile.LogTag,
                setup.CatalogId,
                setup.Seed,
                setup.SetupHash));
            return true;
        }

        private void AppendTrace(
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

            string line = string.Format(
                CultureInfo.InvariantCulture,
                "{{\"elapsed\":{0},\"phase\":\"{1}\",\"outcome\":\"{2}\",\"reason\":\"{3}\",\"simulationActive\":{4},\"clockElapsed\":{5},\"clockPaused\":{6}}}\n",
                elapsed.ToString("0.###", CultureInfo.InvariantCulture),
                match.Phase,
                match.Outcome,
                match.Reason,
                simulationActive ? 1 : 0,
                clockElapsed.ToString("0.###", CultureInfo.InvariantCulture),
                clockPaused);
            File.AppendAllText(LiveTracePath(), line);
        }

        private string LiveTracePath()
        {
            string token = string.Equals(locale, GameLocalization.EnglishLocaleCode, StringComparison.Ordinal) ? "en" : "fa";
            string name = string.Format(CultureInfo.InvariantCulture, profile.LiveTraceFormat, seed, token);
            return Path.Combine(EvidenceDirectory(), name);
        }

        private void Finish(bool abort, string abortReason)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(profile.ActiveKey, false);
            SessionState.SetFloat(profile.PlayingSinceKey, 0f);
            string previous = SessionState.GetString(ValidationSessionKey, "");
            Environment.SetEnvironmentVariable(
                "WARLINE_VALIDATION_SAVE_ROOT",
                string.IsNullOrEmpty(previous) ? null : previous);

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
            string runsPath = Path.Combine(root, profile.RunsPathRelative);
            string csv = File.Exists(runsPath) ? File.ReadAllText(runsPath) : SkirmishAcceptanceScaffold.RequiredHeader + "\n";
            if (!SkirmishExpandedAriaRunLog.TryNextAriaRunId(
                    profile.CatalogId,
                    profile.IsFirstVisitSeed,
                    profile.InvalidSeedError,
                    csv,
                    seed,
                    locale,
                    out string runId,
                    out string idError))
            {
                Debug.LogError(profile.LogTag + " result=Failed " + idError);
                UiShellRuntimeGateway.StopAriaPlay();
                EditorApplication.ExitPlaymode();
                return;
            }

            string evidenceName = runId.ToLowerInvariant();
            string evidenceRelative = (profile.EvidenceDirectoryRelative ?? string.Empty).Replace('\\', '/');
            string traceRelative = evidenceRelative + "/" + evidenceName + ".jsonl";
            string logRelative = evidenceRelative + "/" + evidenceName + "-log.txt";
            string traceAbsolute = Path.Combine(root, traceRelative.Replace('/', Path.DirectorySeparatorChar));
            string logAbsolute = Path.Combine(root, logRelative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(EvidenceDirectory());
            if (File.Exists(LiveTracePath()))
                File.Copy(LiveTracePath(), traceAbsolute, true);
            else
                File.WriteAllText(traceAbsolute, string.Empty);

            string outcome = haveMatch ? match.Outcome.ToString() : string.Empty;
            if (haveMatch && match.Outcome == SkirmishOutcome.None)
                outcome = string.Empty;
            var facts = new SkirmishExpandedAriaTerminalFacts
            {
                RunId = runId,
                Seed = seed,
                Locale = locale,
                Device = "Editor",
                NormalSpeed = normalSpeed,
                StartedAtUtc = SessionState.GetString(profile.StartedKey, string.Empty),
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

            bool appended = SkirmishExpandedAriaRunLog.TryAppendTerminalRow(
                profile.CatalogId,
                runsPath,
                in facts,
                out string row,
                out string error);
            bool counted = appended && SkirmishExpandedAriaRunLog.IsCountedAriaWin(in facts, RowResult(row));
            string log = string.Format(
                CultureInfo.InvariantCulture,
                "{0} appended={1} countedWin={2} error={3}\n{4}\n",
                profile.LogTag,
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
        }

        private bool TryReadCensus(string root, out SkirmishAcceptanceCensus census)
        {
            census = null;
            if (!SkirmishSetupMatrixTable.TryLoad(root, out List<SkirmishSetupMatrixRow> matrix, out _))
                return false;
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            if (!SkirmishExpansionCatalogFactory.TryGetDefinition(
                    authored,
                    profile.CatalogId,
                    out SkirmishScenarioDefinitionConfig definition))
                return false;
            var manifest = new SkirmishContentManifest
            {
                RequiredFeatureIds = definition.RequiredFeatureIds
            };
            if (!SkirmishSetupCompiler.TryCompile(
                    definition,
                    SkirmishDifficultyId.Regular,
                    SkirmishSizeId.Standard,
                    seed,
                    manifest,
                    matrix,
                    out SkirmishResolvedSetup setup,
                    out _))
                return false;
            census = SkirmishAcceptanceCensusCapture.FromSetup(setup);
            return census != null;
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
