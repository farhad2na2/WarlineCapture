#if UNITY_EDITOR
using System;
using System.IO;
using Game.Operations.Capture;
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
    /// Game View capture for O001–O003 mobile-ready content bound into the landed
    /// <see cref="OperationsAriaPlayModePresentation"/> shell. Writes PNGs under
    /// Design/AgentReports/Operations/mobile-ready-o001-o003/_Evidence/.
    /// Windows Ops shadow only: D:\Projects\WarlineCapture-Operations.
    /// Does not open Match/Watch shared UI and does not claim AriaWon.
    /// </summary>
    public static class OperationsMobileReadyPlayModeCapture
    {
        public const string PassMarker = "[OperationsMobileReadyPlayModeCapture] result=Passed";
        public const string FailMarker = "[OperationsMobileReadyPlayModeCapture] result=Failed";
        public const string EvidenceRelativeDir =
            "Design/AgentReports/Operations/mobile-ready-o001-o003/_Evidence";

        public const string CoachScanPng = "o001-coach-scan.en.png";
        public const string CoachEvidencePng = "o001-coach-evidence.en.png";
        public const string CoachExtractPng = "o001-coach-extract.en.png";
        public const string EscortChipsPng = "o002-escort-chips.en.png";
        public const string ResultDeltasPng = "result-trust-intel-heat.en.png";

        const string ActiveKey = "OperationsMobileReadyPlayModeCapture.Active";
        const string PhaseKey = "OperationsMobileReadyPlayModeCapture.Phase";
        const string ShotsKey = "OperationsMobileReadyPlayModeCapture.Shots";
        const string ShotIndexKey = "OperationsMobileReadyPlayModeCapture.ShotIndex";
        const string FrameKey = "OperationsMobileReadyPlayModeCapture.Frame";
        const string StartedAtKey = "OperationsMobileReadyPlayModeCapture.StartedAt";
        const string ExitPendingKey = "OperationsMobileReadyPlayModeCapture.ExitPending";
        const string ExitCodeKey = "OperationsMobileReadyPlayModeCapture.ExitCode";
        const double TimeoutSeconds = 600d;

        const string ShotScan = "scan";
        const string ShotEvidence = "evidence";
        const string ShotExtract = "extract";
        const string ShotEscort = "escort";
        const string ShotResult = "result";

        enum Phase
        {
            Idle = 0,
            WaitingForPlayMode = 1,
            Present = 2,
            WaitCapture = 3
        }

        static OperationsLoopSession _loop;
        static int _nextId = 0xE200;

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

        [MenuItem("Operations/Mobile Ready/Capture O001 Coach")]
        public static void RunO001Coach() => Start(ShotScan + "," + ShotEvidence + "," + ShotExtract);

        [MenuItem("Operations/Mobile Ready/Capture O002 Escort Chips")]
        public static void RunO002Escort() => Start(ShotEscort);

        [MenuItem("Operations/Mobile Ready/Capture Result Deltas")]
        public static void RunResultDeltas() => Start(ShotResult);

        [MenuItem("Operations/Mobile Ready/Capture All Mobile-Ready Evidence")]
        public static void RunAllEvidence() =>
            Start(ShotScan + "," + ShotEvidence + "," + ShotExtract + "," + ShotEscort + "," + ShotResult);

        static void Start(string shots)
        {
            try
            {
                _loop = null;
                _nextId = 0xE200;
                SessionState.SetBool(ActiveKey, true);
                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForPlayMode);
                SessionState.SetString(ShotsKey, shots ?? string.Empty);
                SessionState.SetInt(ShotIndexKey, 0);
                SessionState.SetInt(FrameKey, 0);
                SessionState.SetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);
                RegisterCallbacks();
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorApplication.EnterPlaymode();
                Debug.Log(
                    "[OperationsMobileReadyPlayModeCapture] started shots=" + shots +
                    " evidence=" + EvidenceRelativeDir +
                    " shell=OperationsAriaPlayModePresentation" +
                    " watch_seam=not_opened cli_quit=forbidden owns_editor_exit=1");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
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
                SessionState.SetInt(PhaseKey, (int)Phase.Present);
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
                if (phase == Phase.Present)
                {
                    if (!PresentCurrentShot(out string presentFailure))
                    {
                        Finish(false, presentFailure);
                        return;
                    }

                    SessionState.SetInt(FrameKey, 0);
                    SessionState.SetInt(PhaseKey, (int)Phase.WaitCapture);
                    return;
                }

                if (phase == Phase.WaitCapture)
                    WaitForPng();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(false, exception.Message);
            }
        }

        static void WaitForPng()
        {
            int frame = SessionState.GetInt(FrameKey, 0) + 1;
            SessionState.SetInt(FrameKey, frame);
            string fileName = CurrentFileName();
            if (frame == 8)
                CaptureGameView(fileName);

            string absolute = AbsolutePng(fileName);
            if (frame < 8)
                return;

            if (!File.Exists(absolute) || new FileInfo(absolute).Length == 0)
            {
                if (frame == 30 || frame == 60 || frame == 90)
                    TryFallback(fileName);
                if (frame > 120)
                {
                    if (TryFallback(fileName) && File.Exists(absolute) && new FileInfo(absolute).Length > 0)
                    {
                        AdvanceShot();
                        return;
                    }

                    Finish(false, "png_missing=" + fileName);
                }

                return;
            }

            Debug.Log("[OperationsMobileReadyPlayModeCapture] png=" + EvidenceRelativeDir + "/" + fileName);
            AdvanceShot();
        }

        static void AdvanceShot()
        {
            int next = SessionState.GetInt(ShotIndexKey, 0) + 1;
            string[] shots = Shots();
            if (next >= shots.Length)
            {
                Finish(true, "pngs=" + string.Join(",", FileNames(shots)));
                return;
            }

            SessionState.SetInt(ShotIndexKey, next);
            SessionState.SetInt(PhaseKey, (int)Phase.Present);
            _loop = null;
        }

        static bool PresentCurrentShot(out string failure)
        {
            failure = string.Empty;
            string shot = CurrentShot();
            OperationsAriaPlayModePresentation presenter = OperationsAriaPlayModePresentation.Ensure();
            if (presenter == null)
            {
                failure = "presenter_ensure_failed";
                return false;
            }

            if (shot == ShotResult)
            {
                _loop = BuildResultLoop();
                if (!OperationsMissionResultProjection.TryRead(_loop, out OperationsMissionResultUiFrame ui) ||
                    ui.Deltas.Length != 3)
                {
                    failure = "result_frame";
                    return false;
                }

                presenter.Language = "en";
                presenter.ShowMissionResult(_loop);
                if (!presenter.ResultDeltasBound || string.IsNullOrEmpty(presenter.ContinueLabel))
                {
                    failure = "result_chrome";
                    return false;
                }

                return true;
            }

            if (shot == ShotEscort)
            {
                _loop = BuildEscortLoop();
                if (!OperationsEscortRepairControls.TryRead(_loop, out OperationsEscortRepairControlFrame controls) ||
                    !controls.WarningVisible ||
                    controls.WarningKey != "operations.warning.clinic")
                {
                    failure = "escort_frame";
                    return false;
                }

                presenter.ShowPlayHud(_loop, "operation.o002", "en", 2103);
                if (!presenter.WarningVisible || presenter.ChipLabels == null || presenter.ChipLabels.Length < 2)
                {
                    failure = "escort_chrome";
                    return false;
                }

                return true;
            }

            _loop = BuildCoachLoop(shot);
            var coach = new OperationsOnboardingCoach();
            if (!coach.TryRead(_loop, out OperationsCoachFrame frame))
            {
                failure = "coach_frame";
                return false;
            }

            OperationsCoachStepKind expected = shot == ShotEvidence
                ? OperationsCoachStepKind.Evidence
                : shot == ShotExtract
                    ? OperationsCoachStepKind.Extract
                    : OperationsCoachStepKind.Scan;
            if (frame.Step != expected)
            {
                failure = "coach_step=" + frame.Step;
                return false;
            }

            presenter.ShowPlayHud(_loop, "operation.o001", "en", 2101);
            if (!presenter.CoachVisible)
            {
                failure = "coach_chrome";
                return false;
            }

            return true;
        }

        static void CaptureGameView(string fileName)
        {
            string absolute = AbsolutePng(fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? EvidenceAbsoluteDir());
            if (File.Exists(absolute))
                File.Delete(absolute);
            ScreenCapture.CaptureScreenshot(absolute);
            Debug.Log("[OperationsMobileReadyPlayModeCapture] screenshot=" + EvidenceRelativeDir + "/" + fileName);
        }

        static bool TryFallback(string fileName)
        {
            OperationsAriaPlayModePresentation presenter = OperationsAriaPlayModePresentation.Ensure();
            if (presenter == null || _loop == null)
                return false;
            if (CurrentShot() == ShotResult)
                presenter.ShowMissionResult(_loop);
            else if (CurrentShot() == ShotEscort)
                presenter.ShowPlayHud(_loop, "operation.o002", "en", 2103);
            else
                presenter.ShowPlayHud(_loop, "operation.o001", "en", 2101);

            string absolute = AbsolutePng(fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? EvidenceAbsoluteDir());
            bool wrote = presenter.TryEncodeFallbackPng(absolute, 1920, 1080);
            if (wrote)
                Debug.Log("[OperationsMobileReadyPlayModeCapture] fallback_png=" + EvidenceRelativeDir + "/" + fileName);
            return wrote;
        }

        static OperationsLoopSession BuildCoachLoop(string shot)
        {
            OperationsLoopSession loop = Reach("operation.o001", 2101);
            if (shot == ShotScan)
                return loop;

            CompleteScans(loop);
            if (shot == ShotEvidence)
                return loop;

            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            Require(map.TryGetByAlias("site.evidence", out OperationsGreyboxAnchor evidence));
            Require(loop.Move("unit.d01.rifle.02", evidence.AnchorId).Accepted);
            for (int step = 0; step < 6; step++)
                loop.Advance(1);
            Require(loop.Interact("unit.d01.rifle.02", "site.d01.relay_evidence").Accepted);
            for (int step = 0; step < 30 && !NodeComplete(loop, "interact_relay"); step++)
                loop.Advance(1);
            Require(NodeComplete(loop, "interact_relay"));
            loop.Advance(1);
            return loop;
        }

        static void CompleteScans(OperationsLoopSession loop)
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            Require(map.TryGetByAlias("site.signal_a", out OperationsGreyboxAnchor a));
            Require(map.TryGetByAlias("site.signal_b", out OperationsGreyboxAnchor b));
            Require(map.TryGetByAlias("site.signal_c", out OperationsGreyboxAnchor c));
            Require(loop.Move("unit.d01.recon.01", a.AnchorId).Accepted);
            Require(loop.Move("unit.d01.recon.02", b.AnchorId).Accepted);
            Require(loop.Move("unit.d01.rifle.01", c.AnchorId).Accepted);
            for (int step = 0; step < 8; step++)
                loop.Advance(1);
            Require(loop.Observe("unit.d01.recon.01", "site.d01.signal_a").Accepted);
            Require(loop.Scan("unit.d01.recon.01", "site.d01.signal_a").Accepted);
            Require(loop.Observe("unit.d01.recon.02", "site.d01.signal_b").Accepted);
            Require(loop.Scan("unit.d01.recon.02", "site.d01.signal_b").Accepted);
            Require(loop.Observe("unit.d01.rifle.01", "site.d01.signal_c").Accepted);
            Require(loop.Scan("unit.d01.rifle.01", "site.d01.signal_c").Accepted);
            for (int step = 0; step < 20; step++)
                loop.Advance(1);
            Require(NodeComplete(loop, "scan_signals"));
        }

        static OperationsLoopSession BuildEscortLoop()
        {
            OperationsLoopSession loop = Reach("operation.o002", 2103);
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            Require(map.TryGetByAlias("site.junction", out OperationsGreyboxAnchor junction));
            Require(loop.Move("unit.d01.recon.01", junction.AnchorId).Accepted);
            for (int step = 0; step < 8; step++)
                loop.Advance(1);
            Require(loop.Observe("unit.d01.recon.01", "site.d01.junction").Accepted);
            Require(loop.Scan("unit.d01.recon.01", "site.d01.junction").Accepted);
            for (int step = 0; step < 20; step++)
                loop.Advance(1);
            Require(NodeComplete(loop, "scan_junction"));
            loop.Advance(1);
            return loop;
        }

        static OperationsLoopSession BuildResultLoop()
        {
            OperationsLoopSession loop = Reach("operation.o001", 2105);
            Require(OperationsAriaInputSkills.TryPlayVisibleControlWin(loop, "operation.o001"));
            Require(loop.BeginResult().Accepted);
            Require(loop.CompleteResult().Accepted);
            string settleId = Id();
            Require(loop.BeginSettlement(settleId).Accepted);
            Require(loop.CompleteSettlement(settleId).Accepted);
            return loop;
        }

        static OperationsLoopSession Reach(string missionId, int seed)
        {
            OperationsLoopSession loop = OperationsLoopSession.Create(seed, new byte[] { 9, 9, 9 }, new byte[] { 8, 8 });
            Require(loop.TryOffer(missionId, out OperationsOfferSaveData offer), missionId);
            int district = 1;
            for (int number = 1; number <= 6; number++)
            {
                if (OperationsIdentityRules.DistrictId(number) == offer.districtId)
                    district = number;
            }

            Require(loop.OpenDistrict(district).Accepted);
            Require(loop.OpenBriefing(offer.offerId).Accepted);
            string deployId = Id();
            Require(loop.BeginDeploy(deployId).Accepted);
            Require(loop.CompleteAttempt(deployId).Accepted);
            Require(loop.BeginLaunch().Accepted);
            Require(loop.CompleteLaunch().Accepted);
            Require(loop.BeginActive(true, true, true, loop.ContentHash).Accepted);
            Require(loop.CompleteActive().Accepted);
            return loop;
        }

        static string Id() => "cmd.operations." + (_nextId++).ToString("x8");

        static bool NodeComplete(OperationsLoopSession loop, string nodeId) =>
            loop.TryNode(nodeId, out OperationsTacticalNodeState state) &&
            state.Phase == OperationsTacticalNodePhase.Complete;

        static void Require(bool condition, string message = "require")
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        static string CurrentShot()
        {
            string[] shots = Shots();
            int index = SessionState.GetInt(ShotIndexKey, 0);
            if (index < 0 || index >= shots.Length)
                return string.Empty;
            return shots[index];
        }

        static string CurrentFileName()
        {
            switch (CurrentShot())
            {
                case ShotEvidence: return CoachEvidencePng;
                case ShotExtract: return CoachExtractPng;
                case ShotEscort: return EscortChipsPng;
                case ShotResult: return ResultDeltasPng;
                default: return CoachScanPng;
            }
        }

        static string[] Shots()
        {
            string raw = SessionState.GetString(ShotsKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
                return Array.Empty<string>();
            return raw.Split(',');
        }

        static string[] FileNames(string[] shots)
        {
            var names = new string[shots.Length];
            for (int index = 0; index < shots.Length; index++)
            {
                switch (shots[index])
                {
                    case ShotEvidence: names[index] = CoachEvidencePng; break;
                    case ShotExtract: names[index] = CoachExtractPng; break;
                    case ShotEscort: names[index] = EscortChipsPng; break;
                    case ShotResult: names[index] = ResultDeltasPng; break;
                    default: names[index] = CoachScanPng; break;
                }
            }

            return names;
        }

        static string EvidenceAbsoluteDir() =>
            Path.Combine(FindRepoRoot(), EvidenceRelativeDir.Replace('/', Path.DirectorySeparatorChar));

        static string AbsolutePng(string fileName) => Path.Combine(EvidenceAbsoluteDir(), fileName);

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

            throw new InvalidOperationException("repo_root");
        }

        static void Finish(bool passed, string message)
        {
            EditorApplication.update -= Update;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            SessionState.EraseBool(ActiveKey);
            SessionState.EraseInt(PhaseKey);
            SessionState.EraseString(ShotsKey);
            SessionState.EraseInt(ShotIndexKey);
            SessionState.EraseInt(FrameKey);
            SessionState.EraseFloat(StartedAtKey);
            OperationsAriaPlayModePresentation.ClearCache();
            _loop = null;

            if (passed)
                Debug.Log(PassMarker + " " + message);
            else
                Debug.LogError(FailMarker + " " + message);

            SessionState.SetBool(ExitPendingKey, true);
            SessionState.SetInt(ExitCodeKey, passed ? 0 : 1);
            EditorApplication.update -= ExitWhenIdle;
            EditorApplication.update += ExitWhenIdle;
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
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
    }
}
#endif
