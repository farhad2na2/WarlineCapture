#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Operations.Content;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Tactical;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    /// <summary>
    /// Windows Ops shadow Play Mode capture for O001–O003 Regular EN.
    /// Enters Play Mode, drives planner ARIA through loop visible-controls, presents an
    /// Ops-owned victory screen, writes PNG + result.en.json (status AriaWon).
    /// Does not open shipping Match scene view / Watch virtual-touch shared-UI seams.
    /// </summary>
    public static class OperationsAriaPlayModeCapture
    {
        public const string CapturePassMarkerPrefix = OperationsAriaPlayModeCaptureChecks.LiveCapturePassMarkerPrefix;
        public const string CaptureFailMarker = "[OperationsAriaPlayModeCapture] result=Failed";

        const string ActiveKey = "OperationsAriaPlayModeCapture.Active";
        const string PhaseKey = "OperationsAriaPlayModeCapture.Phase";
        const string MissionKey = "OperationsAriaPlayModeCapture.Mission";
        const string SeedKey = "OperationsAriaPlayModeCapture.Seed";
        const string LanguageKey = "OperationsAriaPlayModeCapture.Language";
        const string StartedAtKey = "OperationsAriaPlayModeCapture.StartedAt";
        const string FrameKey = "OperationsAriaPlayModeCapture.Frame";
        const string CaptureIndexKey = "OperationsAriaPlayModeCapture.CaptureIndex";
        const double TimeoutSeconds = 600d;

        enum Phase
        {
            Idle = 0,
            WaitingForPlayMode = 1,
            Playing = 2,
            PresentVictory = 3,
            WaitCaptureFrames = 4,
            WriteEvidence = 5
        }

        static OperationsLoopSession _loop;
        static OperationsAriaEvidenceRecord _record;
        static readonly List<string> CaptureRelativePaths = new List<string>();
        static readonly List<string> IntentTrace = new List<string>();
        static int _playStep;
        static int[] _beforeDistrict = Array.Empty<int>();
        static string _offerId = string.Empty;
        static string _districtId = string.Empty;
        static string _settleId = string.Empty;
        static int _completionTick = -1;
        static bool _won;

        const string ExitPendingKey = "OperationsAriaPlayModeCapture.ExitPending";
        const string ExitCodeKey = "OperationsAriaPlayModeCapture.ExitCode";

        [InitializeOnLoadMethod]
        static void ResumeActiveCapture()
        {
            if (SessionState.GetBool(ExitPendingKey, false))
            {
                EditorApplication.update -= ExitWhenIdle;
                EditorApplication.update += ExitWhenIdle;
            }

            if (!SessionState.GetBool(ActiveKey, false))
                return;
            RegisterCallbacks();
        }

        static void ExitWhenIdle()
        {
            if (!SessionState.GetBool(ExitPendingKey, false))
            {
                EditorApplication.update -= ExitWhenIdle;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            int code = SessionState.GetInt(ExitCodeKey, 1);
            SessionState.EraseBool(ExitPendingKey);
            SessionState.EraseInt(ExitCodeKey);
            EditorApplication.update -= ExitWhenIdle;
            EditorApplication.Exit(code);
        }

        static void Finish(bool passed, string message)
        {
            EditorApplication.update -= Update;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            SessionState.EraseBool(ActiveKey);
            SessionState.EraseInt(PhaseKey);
            SessionState.EraseString(MissionKey);
            SessionState.EraseInt(SeedKey);
            SessionState.EraseString(LanguageKey);
            SessionState.EraseFloat(StartedAtKey);
            SessionState.EraseInt(FrameKey);
            SessionState.EraseInt(CaptureIndexKey);
            ResetRuntimeState();

            if (passed)
                UnityEngine.Debug.Log(CapturePassMarkerPrefix + " " + message);
            else
                UnityEngine.Debug.LogError(CaptureFailMarker + " " + message);

            SessionState.SetBool(ExitPendingKey, true);
            SessionState.SetInt(ExitCodeKey, passed ? 0 : 1);
            EditorApplication.update -= ExitWhenIdle;
            EditorApplication.update += ExitWhenIdle;
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
        }

        public static void RunO001RegularEn() => StartCapture("operation.o001", 1102, "en");

        public static void RunO002RegularEn() => StartCapture("operation.o002", 1103, "en");

        public static void RunO003RegularEn() => StartCapture("operation.o003", 1104, "en");

        public static void RunFocusedCaptureFromEnvironment()
        {
            string mission = Environment.GetEnvironmentVariable("OPERATIONS_ARIA_CAPTURE_MISSION");
            string seedText = Environment.GetEnvironmentVariable("OPERATIONS_ARIA_CAPTURE_SEED");
            string language = Environment.GetEnvironmentVariable("OPERATIONS_ARIA_CAPTURE_LANGUAGE");
            if (string.IsNullOrEmpty(mission))
                mission = "operation.o001";
            int seed = 1102;
            if (!string.IsNullOrEmpty(seedText))
                int.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed);
            else if (mission == "operation.o002")
                seed = 1103;
            else if (mission == "operation.o003")
                seed = 1104;
            if (string.IsNullOrEmpty(language))
                language = "en";
            StartCapture(mission, seed, language);
        }

        static void StartCapture(string missionId, int seed, string language)
        {
            try
            {
                ResetRuntimeState();
                SessionState.SetBool(ActiveKey, true);
                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForPlayMode);
                SessionState.SetString(MissionKey, missionId);
                SessionState.SetInt(SeedKey, seed);
                SessionState.SetString(LanguageKey, language);
                SessionState.SetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);
                SessionState.SetInt(FrameKey, 0);
                SessionState.SetInt(CaptureIndexKey, 0);
                RegisterCallbacks();
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorApplication.EnterPlaymode();
                UnityEngine.Debug.Log(
                    "[OperationsAriaPlayModeCapture] started mission=" + missionId +
                    " seed=" + seed.ToString(CultureInfo.InvariantCulture) +
                    " language=" + language +
                    " watch_seam=not_opened");
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                Finish(false, exception.Message);
            }
        }

        static void RegisterCallbacks()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveKey, false))
                return;
            if (state == PlayModeStateChange.EnteredPlayMode)
                SessionState.SetInt(PhaseKey, (int)Phase.Playing);
        }

        static void Update()
        {
            if (!SessionState.GetBool(ActiveKey, false))
                return;

            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedAtKey, 0f) > TimeoutSeconds)
            {
                Finish(false, "timeout");
                return;
            }

            Phase phase = (Phase)SessionState.GetInt(PhaseKey, (int)Phase.Idle);
            if (phase == Phase.WaitingForPlayMode)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.EnterPlaymode();
                return;
            }

            if (!EditorApplication.isPlaying)
                return;

            try
            {
                if (phase == Phase.Playing)
                {
                    TickPlaying();
                    return;
                }

                if (phase == Phase.PresentVictory)
                {
                    PresentVictoryScreen();
                    SessionState.SetInt(PhaseKey, (int)Phase.WaitCaptureFrames);
                    SessionState.SetInt(FrameKey, 0);
                    return;
                }

                if (phase == Phase.WaitCaptureFrames)
                {
                    int frame = SessionState.GetInt(FrameKey, 0) + 1;
                    SessionState.SetInt(FrameKey, frame);
                    if (frame == 8)
                        CaptureNamed("win-screen");
                    if (frame < 8)
                        return;
                    if (!WinScreenFileExists())
                    {
                        if (frame > 120)
                        {
                            Finish(false, "win_screen_missing");
                            return;
                        }

                        return;
                    }

                    SessionState.SetInt(PhaseKey, (int)Phase.WriteEvidence);
                    return;
                }

                if (phase == Phase.WriteEvidence)
                {
                    WriteEvidenceAndFinish();
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                Finish(false, exception.Message);
            }
        }

        static void TickPlaying()
        {
            string missionId = SessionState.GetString(MissionKey, "operation.o001");
            int seed = SessionState.GetInt(SeedKey, 1102);
            string language = SessionState.GetString(LanguageKey, "en");

            if (_loop == null)
            {
                if (!TryBeginMission(missionId, seed, language, out string beginFailure))
                {
                    Finish(false, beginFailure);
                    return;
                }

                OperationsAriaPlayModePresentation.Ensure().ShowPlayHud(_loop, missionId, language, seed);
                CaptureNamed("play-01-start");
                return;
            }

            if (!_loop.MissionTerminal)
            {
                StepUnassistedOnce();
                OperationsAriaPlayModePresentation.Ensure().ShowPlayHud(_loop, missionId, language, seed);
                if (_playStep == 40)
                    CaptureNamed("play-02-mid");
                if (_playStep >= 1200 && !_loop.MissionTerminal)
                {
                    Finish(false, "max_steps");
                    return;
                }

                return;
            }

            _won = _loop.MissionOutcome == OperationsOutcomeKind.Victory;
            if (_loop.TryMissionTick(out int tick))
                _completionTick = tick;

            if (!SettleThroughResult(out string settleFailure))
            {
                Finish(false, settleFailure);
                return;
            }

            CaptureNamed("play-03-terminal");
            _record = BuildRecord(missionId, seed, language);
            if (!_record.Victory)
            {
                Finish(false, "outcome:" + _record.TerminalOutcome);
                return;
            }

            SessionState.SetInt(PhaseKey, (int)Phase.PresentVictory);
        }

        static bool TryBeginMission(string missionId, int seed, string language, out string failure)
        {
            failure = string.Empty;
            _loop = OperationsLoopSession.Create(seed, new byte[] { 9, 9, 9 }, new byte[] { 8, 8 });
            if (missionId == "operation.o003" && !_loop.TryOffer(missionId, out _))
            {
                if (!BootstrapO003Prerequisite(_loop, language, out failure))
                    return false;
            }

            if (!_loop.TryOffer(missionId, out OperationsOfferSaveData offer))
            {
                failure = "offer";
                return false;
            }

            _offerId = offer.offerId;
            _districtId = offer.districtId;
            int districtNumber = DistrictNumber(offer.districtId);
            _beforeDistrict = SnapshotDistrict(_loop, districtNumber);
            if (!_loop.OpenDistrict(districtNumber).Accepted || !_loop.OpenBriefing(offer.offerId).Accepted)
            {
                failure = "briefing";
                return false;
            }

            string deployId = NextId();
            if (!_loop.BeginDeploy(deployId).Accepted || !_loop.CompleteAttempt(deployId).Accepted)
            {
                failure = "deploy";
                return false;
            }

            if (!_loop.BeginLaunch().Accepted || !_loop.CompleteLaunch().Accepted)
            {
                failure = "launch";
                return false;
            }

            if (!_loop.BeginActive(true, true, true, _loop.ContentHash).Accepted || !_loop.CompleteActive().Accepted)
            {
                failure = "active";
                return false;
            }

            IntentTrace.Clear();
            _playStep = 0;
            return true;
        }

        static bool BootstrapO003Prerequisite(OperationsLoopSession loop, string language, out string failure)
        {
            failure = string.Empty;
            if (!loop.TryOffer("operation.o001", out OperationsOfferSaveData offer))
            {
                failure = "o001_offer";
                return false;
            }

            int districtNumber = DistrictNumber(offer.districtId);
            if (!loop.OpenDistrict(districtNumber).Accepted || !loop.OpenBriefing(offer.offerId).Accepted)
            {
                failure = "o001_briefing";
                return false;
            }

            string deployId = NextId();
            if (!loop.BeginDeploy(deployId).Accepted || !loop.CompleteAttempt(deployId).Accepted ||
                !loop.BeginLaunch().Accepted || !loop.CompleteLaunch().Accepted ||
                !loop.BeginActive(true, true, true, loop.ContentHash).Accepted || !loop.CompleteActive().Accepted)
            {
                failure = "o001_active";
                return false;
            }

            if (!OperationsAriaInputSkills.TryPlayUnassistedWin(loop))
            {
                failure = "o001_win";
                return false;
            }

            if (!loop.BeginResult().Accepted || !loop.CompleteResult().Accepted)
            {
                failure = "o001_result";
                return false;
            }

            string settleId = NextId();
            if (!loop.BeginSettlement(settleId).Accepted || !loop.CompleteSettlement(settleId).Accepted ||
                !loop.BeginReturn().Accepted || !loop.CompleteReturn().Accepted ||
                !loop.RequestEndDay(NextId()).Accepted)
            {
                failure = "o001_settle";
                return false;
            }

            _ = language;
            return true;
        }

        static bool StepUnassistedOnce()
        {
            _playStep++;
            const int maxSteps = 1200;
            if (_playStep > maxSteps)
                return false;

            OperationsAriaIntent[] plan = OperationsAriaObjectivePlanner.Plan(_loop);
            OperationsAriaIntent intent = default;
            bool haveIntent = false;
            for (int index = 0; index < plan.Length; index++)
            {
                if (plan[index].Skill == OperationsAriaSkillKind.Focus)
                    continue;
                intent = plan[index];
                haveIntent = true;
                break;
            }

            if (!haveIntent)
            {
                IntentTrace.Add(_playStep + ":advance");
                _loop.Advance(1);
                return true;
            }

            if (intent.Skill == OperationsAriaSkillKind.Extract)
            {
                for (int index = 0; index < plan.Length; index++)
                {
                    if (plan[index].Skill != OperationsAriaSkillKind.Extract)
                        continue;
                    OperationsTacticalCommandResult extract = OperationsAriaInputSkills.TryExecute(_loop, plan[index]);
                    IntentTrace.Add(_playStep + ":Extract:" + plan[index].ActorId + ":" + extract.Reason);
                }

                _loop.Advance(1);
                return true;
            }

            if (intent.ActorId.Length > 0 &&
                _loop.TryActor(intent.ActorId, out OperationsTacticalActorState actor) &&
                actor.ChannelTicks > 0 &&
                (intent.Skill == OperationsAriaSkillKind.Scan ||
                 intent.Skill == OperationsAriaSkillKind.Interact ||
                 intent.Skill == OperationsAriaSkillKind.Repair ||
                 intent.Skill == OperationsAriaSkillKind.Observe))
            {
                IntentTrace.Add(_playStep + ":wait_channel:" + intent.ActorId);
                _loop.Advance(1);
                return true;
            }

            OperationsTacticalCommandResult result = OperationsAriaInputSkills.TryExecute(_loop, intent);
            IntentTrace.Add(_playStep + ":" + intent.Skill + ":" + intent.ActorId + "->" + intent.TargetId + "/" + intent.RouteId + ":" + result.Reason);
            _loop.Advance(1);
            return true;
        }

        static bool SettleThroughResult(out string failure)
        {
            failure = string.Empty;
            if (!_loop.BeginResult().Accepted || !_loop.CompleteResult().Accepted)
            {
                failure = "result";
                return false;
            }

            _settleId = NextId();
            if (!_loop.BeginSettlement(_settleId).Accepted || !_loop.CompleteSettlement(_settleId).Accepted)
            {
                failure = "settlement";
                return false;
            }

            if (!_loop.BeginReturn().Accepted || !_loop.CompleteReturn().Accepted)
            {
                failure = "return";
                return false;
            }

            return true;
        }

        static void PresentVictoryScreen()
        {
            OperationsAriaPlayModePresentation.Ensure().ShowVictory(_loop, _record);
            UnityEngine.Debug.Log(
                "[OperationsAriaPlayModeCapture] victory_screen mission=" + _record.MissionId +
                " seed=" + _record.Seed.ToString(CultureInfo.InvariantCulture));
        }

        static string CurrentEvidenceAbsoluteDir()
        {
            string missionId = SessionState.GetString(MissionKey, "operation.o001");
            int seed = SessionState.GetInt(SeedKey, 1102);
            string relativeDir = OperationsAriaEvidenceHarness.EvidenceRelativePath(missionId, "Regular", seed);
            return Path.Combine(FindRepoRoot(), relativeDir.Replace('/', Path.DirectorySeparatorChar));
        }

        static bool WinScreenFileExists()
        {
            string language = SessionState.GetString(LanguageKey, "en");
            string absolutePath = Path.Combine(CurrentEvidenceAbsoluteDir(), "win-screen." + language + ".png");
            return File.Exists(absolutePath) && new FileInfo(absolutePath).Length > 0;
        }

        static void CaptureNamed(string stem)
        {
            string missionId = SessionState.GetString(MissionKey, "operation.o001");
            int seed = SessionState.GetInt(SeedKey, 1102);
            string language = SessionState.GetString(LanguageKey, "en");
            string relativeDir = OperationsAriaEvidenceHarness.EvidenceRelativePath(missionId, "Regular", seed);
            string fileName = stem + "." + language + ".png";
            string relativePath = relativeDir.Replace('\\', '/') + "/" + fileName;
            string absoluteDir = Path.Combine(FindRepoRoot(), relativeDir.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(absoluteDir);
            string absolutePath = Path.Combine(absoluteDir, fileName);
            ScreenCapture.CaptureScreenshot(absolutePath);
            if (!CaptureRelativePaths.Contains(relativePath))
                CaptureRelativePaths.Add(relativePath);
            SessionState.SetInt(CaptureIndexKey, SessionState.GetInt(CaptureIndexKey, 0) + 1);
            UnityEngine.Debug.Log("[OperationsAriaPlayModeCapture] screenshot=" + relativePath);
        }

        static void WriteEvidenceAndFinish()
        {
            string missionId = _record.MissionId;
            int seed = _record.Seed;
            string language = _record.Language;
            string relativeDir = OperationsAriaEvidenceHarness.EvidenceRelativePath(missionId, "Regular", seed);
            string absoluteDir = Path.Combine(FindRepoRoot(), relativeDir.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(absoluteDir);

            string winRelative = relativeDir.Replace('\\', '/') + "/win-screen." + language + ".png";
            string winAbsolute = Path.Combine(absoluteDir, "win-screen." + language + ".png");
            string pngHash = File.Exists(winAbsolute) ? Sha256Hex(winAbsolute) : "PENDING_CAPTURE_FILE";

            _record.Status = "AriaWon";
            _record.Platform = "windows-editor";
            _record.WatchVirtualTouch = "NotOpened";
            _record.ConfigHashPlaceholder = _record.ContentHash;
            _record.CodeHashPlaceholder = ResolveCodeHash();
            _record.CapturePathPlaceholder = winRelative;
            _record.Victory = true;
            _record.Notes =
                "Play Mode Ops-owned victory screen capture on WarlineCapture-Operations. " +
                "Watch virtual-touch seam not opened. png_sha256=" + pngHash +
                " chronological=" + string.Join(";", CaptureRelativePaths) +
                ". Programmer 2 verifies win-screen PNG before catalog aria_win_acceptance flip. " +
                "Playable Operations mission ready is not claimed by this runner alone.";

            string jsonPath = Path.Combine(absoluteDir, OperationsAriaEvidenceHarness.ResultFileName(language));
            File.WriteAllText(jsonPath, _record.ToJson(), new UTF8Encoding(false));
            UnityEngine.Debug.Log("[OperationsAriaPlayModeCapture] wrote " + relativeDir.Replace('\\', '/') + "/" +
                                  OperationsAriaEvidenceHarness.ResultFileName(language));
            Finish(
                true,
                "mission=" + missionId +
                " seed=" + seed.ToString(CultureInfo.InvariantCulture) +
                " capture=" + winRelative +
                " status=AriaWon");
        }

        static OperationsAriaEvidenceRecord BuildRecord(string missionId, int seed, string language)
        {
            int districtNumber = DistrictNumber(_districtId);
            int[] after = SnapshotDistrict(_loop, districtNumber);
            string terminalReason = string.Empty;
            string terminalOutcome = _loop.MissionOutcome.ToString();
            int civilianDeaths = 0;
            int taskForceLosses = 0;
            string resultHash = _loop.ResultHash;
            if (_loop.TryCommittedResult(out OperationsMissionResult missionResult))
            {
                terminalReason = missionResult.TerminalReason;
                terminalOutcome = missionResult.Outcome.ToString();
                civilianDeaths = missionResult.CivilianDeaths;
                taskForceLosses = missionResult.TaskForceLosses;
                resultHash = missionResult.ResultHash;
            }

            bool victory = _won && _loop.MissionVictory(missionId);
            return new OperationsAriaEvidenceRecord
            {
                Status = victory ? "AriaWon" : "HostAttemptFailed",
                MissionId = missionId,
                DistrictId = _districtId,
                OfferId = _offerId,
                RunId = _loop.RunId,
                SessionId = _loop.SessionId,
                ScenarioId = _loop.ScenarioId,
                Seed = seed,
                Difficulty = _loop.Difficulty.ToString(),
                Language = language,
                Platform = "windows-editor",
                InputSource = "ARIA",
                InputPipeline = "operations_loop_visible_controls",
                WatchVirtualTouch = "NotOpened",
                RestartCount = _loop.RestartCount,
                AttemptOrdinal = _loop.AttemptOrdinal,
                ContentHash = _loop.ContentHash,
                TerminalReason = terminalReason,
                TerminalOutcome = terminalOutcome,
                Victory = victory,
                ObjectiveCompletionTick = _completionTick,
                CivilianDeaths = civilianDeaths,
                TaskForceLosses = taskForceLosses,
                ReceivedCredits = _loop.Credits,
                ReceivedXp = _loop.CommanderXp,
                SettlementTransactionId = _settleId,
                ResultHash = resultHash,
                SettledRevision = -1,
                BeforeDistrict = _beforeDistrict,
                AfterDistrict = after,
                IntentTrace = IntentTrace.ToArray(),
                Notes = "Play Mode capture in progress."
            };
        }

        static void ResetRuntimeState()
        {
            _loop = null;
            _record = null;
            CaptureRelativePaths.Clear();
            IntentTrace.Clear();
            _playStep = 0;
            _beforeDistrict = Array.Empty<int>();
            _offerId = string.Empty;
            _districtId = string.Empty;
            _settleId = string.Empty;
            _completionTick = -1;
            _won = false;
        }

        static int[] SnapshotDistrict(OperationsLoopSession loop, int number)
        {
            var district = loop.District(number);
            return new[]
            {
                district.Security,
                district.Trust,
                district.Infrastructure,
                district.EnemyInfluence,
                district.IntelConfidence,
                district.Heat,
                district.SupplyReadiness
            };
        }

        static int DistrictNumber(string districtId)
        {
            for (int number = 1; number <= 6; number++)
            {
                if (OperationsIdentityRules.DistrictId(number) == districtId)
                    return number;
            }

            throw new InvalidOperationException(districtId);
        }

        static int _serial = 0xE300;
        static string NextId() => "cmd.operations." + (_serial++).ToString("x8");

        static string FindRepoRoot()
        {
            string directory = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(directory))
            {
                if (File.Exists(Path.Combine(directory, "AGENTS.md")) &&
                    Directory.Exists(Path.Combine(directory, "Assets", "Game", "Scripts", "Operations")))
                    return directory;
                directory = Directory.GetParent(directory)?.FullName;
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        static string ResolveCodeHash()
        {
            try
            {
                var start = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse HEAD",
                    WorkingDirectory = FindRepoRoot(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var process = Process.Start(start))
                {
                    if (process == null)
                        return "git_unavailable";
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0 && output.Length >= 7)
                        return output;
                }
            }
            catch
            {
                // Fall through.
            }

            return "unity-" + Application.unityVersion;
        }

        static string Sha256Hex(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                for (int index = 0; index < hash.Length; index++)
                    builder.Append(hash[index].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }
    }
}
#endif
