using System;
using System.IO;
using Game.Operations.Content;

namespace Game.Tests.Editor.Operations
{
    /// <summary>
    /// Host/Editor wiring checks for the Operations ARIA Play Mode capture runner.
    /// Proves compile/entry-point wiring only; does not claim AriaWon or playable.
    /// </summary>
    public static class OperationsAriaPlayModeCaptureChecks
    {
        public const int ExpectedCheckCount = 7;
        public const string PassMarker = "[OperationsAriaPlayModeCaptureValidation] result=Passed checks=7";
        public const string LiveCapturePassMarkerPrefix = "[OperationsAriaPlayModeCapture] result=Passed";

        public static void RunAll()
        {
            CaptureEntryPointsExist();
            PresentationIsOpsOwned();
            InvokeScriptWiresExecuteMethods();
            BannedSeamsStayClosed();
            ScaffoldsRemainPendingUntilLiveCapture();
            CaptureUsesEnterPlaymodeAndScreenshot();
            WiringMarkerDistinctFromLiveCapture();
        }

        public static void CaptureEntryPointsExist()
        {
            string source = ReadTestSource("OperationsAriaPlayModeCapture.cs");
            Require(source.Contains("RunO001RegularEn"), "o001_entry");
            Require(source.Contains("RunO002RegularEn"), "o002_entry");
            Require(source.Contains("RunO003RegularEn"), "o003_entry");
            Require(source.Contains("EnterPlaymode"), "enter_playmode");
            Require(source.Contains("TryPlayUnassistedWin") || source.Contains("OperationsAriaObjectivePlanner.Plan"), "planner_path");
        }

        public static void PresentationIsOpsOwned()
        {
            string source = ReadCapturePresentationSource();
            string world = ReadCaptureSource("OperationsTacticalWorldShell.cs");
            Require(source.Contains("OperationsAriaPlayModePresentation"), "presenter");
            Require(source.Contains("ShowVictory"), "victory");
            Require(source.Contains("OnGUI"), "ongui");
            Require(source.Contains("result.victory"), "localized_victory");
            Require(source.Contains("namespace Game.Operations.Capture"), "runtime_namespace");
            Require(source.Contains("OperationsTacticalWorldShell"), "world_shell");
            Require(source.Contains("BuildLocalizedObjectives"), "localized_objectives");
            Require(source.Contains("PhonePanelRect"), "phone_mock");
            Require(world.Contains("CreatePrimitive"), "world_primitives");
            Require(!source.Contains("MatchSceneView"), "no_match_scene");
            Require(!source.Contains("AriaPlayCapability"), "no_watch_capability");
            Require(!source.Contains("Ops-owned win screen (Watch shared-UI seam not opened)"), "no_dev_footer");
        }

        public static void InvokeScriptWiresExecuteMethods()
        {
            string root = FindRepoRoot();
            string capture = File.ReadAllText(Path.Combine(root, "Tools", "Operations", "Invoke-OperationsAriaPlayModeCapture.ps1"));
            string wiring = File.ReadAllText(Path.Combine(root, "Tools", "Operations", "Invoke-OperationsAriaPlayModeCaptureValidation.ps1"));
            Require(capture.Contains("OperationsAriaPlayModeCapture.RunO001RegularEn"), "invoke_o001");
            Require(capture.Contains("OperationsAriaPlayModeCapture.RunO002RegularEn"), "invoke_o002");
            Require(capture.Contains("OperationsAriaPlayModeCapture.RunO003RegularEn"), "invoke_o003");
            Require(capture.Contains("WarlineCapture-Operations"), "shadow_only");
            // Live Play Mode capture must omit Unity -quit; runner owns EditorApplication.Exit.
            Require(capture.Contains("InvokeUnity.ps1"), "invoke_unity_direct");
            Require(capture.Contains("$unityArguments = @(\"-executeMethod\""), "execute_only_args");
            Require(!capture.Contains("$unityArguments = @(\"-quit\""), "no_quit_arg_array");
            Require(capture.Contains("cli_quit=omitted") || capture.Contains("Omit -quit"), "omit_quit_documented");
            Require(capture.Contains("EditorApplication.Exit") || capture.Contains("owns EditorApplication.Exit"), "docs_exit");
            // Pass marker + evidence beat unreadable InvokeUnity exit codes (non-batchmode Exit).
            Require(capture.Contains("hasPassMarker") || capture.Contains("pass marker and evidence"), "exit_code_softened");
            Require(wiring.Contains(PassMarker) || wiring.Contains("[OperationsAriaPlayModeCaptureValidation] result=Passed checks=7"), "wiring_marker");
            Require(wiring.Contains("OperationsAriaPlayModeCaptureValidation.RunFocusedValidation"), "wiring_method");
            // Wiring validation may still use the sync -quit helper; live capture must not.
            Require(wiring.Contains("InvokeUnityExecuteMethodValidation.ps1"), "wiring_uses_sync_helper");
        }

        public static void BannedSeamsStayClosed()
        {
            string capture = ReadTestSource("OperationsAriaPlayModeCapture.cs");
            string presentation = ReadCapturePresentationSource();
            Require(!capture.Contains("MatchSceneView"), "capture_no_match");
            Require(!capture.Contains("SaveDataModel"), "capture_no_save");
            Require(!capture.Contains("SkirmishExpansion"), "capture_no_skirmish");
            Require(!capture.Contains("AriaPlayCapability"), "capture_no_watch");
            Require(!presentation.Contains("MatchSceneView"), "presenter_no_match");
            Require(!presentation.Contains("AriaPlayCapability"), "presenter_no_watch");
            Require(capture.Contains("watch_seam=not_opened") || capture.Contains("NotOpened"), "watch_not_opened");
            Require(capture.Contains("presenter_ensure_failed"), "null_safe_ensure");
        }

        public static void ScaffoldsRemainPendingUntilLiveCapture()
        {
            // Regular EN captures are recorded AriaWon (O001 URP recapture + O002/O003 from main).
            // FA scaffolds stay pending. This wiring check does not claim playable.
            string root = FindRepoRoot();
            string[] paths =
            {
                Path.Combine(root, "Design", "AgentReports", "Operations", "host-aria-evidence", "operation.o001", "Regular", "1102", "result.en.json"),
                Path.Combine(root, "Design", "AgentReports", "Operations", "host-aria-evidence", "operation.o002", "Regular", "1103", "result.en.json"),
                Path.Combine(root, "Design", "AgentReports", "Operations", "host-aria-evidence", "operation.o003", "Regular", "1104", "result.en.json")
            };
            for (int index = 0; index < paths.Length; index++)
            {
                string json = File.ReadAllText(paths[index]);
                Require(json.Contains("\"status\": \"AriaWon\""), "recorded_ariawon:" + index);
                Require(json.Contains("\"victory\": true"), "recorded_victory:" + index);
                Require(json.Contains("win-screen.en.png"), "recorded_capture:" + index);
                Require(!json.Contains("PendingAriaWon"), "recorded_not_scaffold:" + index);
            }

            string faScaffold = File.ReadAllText(Path.Combine(
                root,
                "Design",
                "AgentReports",
                "Operations",
                "host-aria-evidence",
                "operation.o001",
                "Regular",
                "9102",
                "result.fa.json"));
            Require(faScaffold.Contains("PendingAriaWon"), "fa_scaffold_pending");
            Require(faScaffold.Contains("PENDING_LIVE_BUILD"), "fa_scaffold_placeholder");

            Require(OperationsAriaEvidenceHarness.CanonicalRegularSeeds[0] == 1102);
            Require(OperationsAriaEvidenceHarness.CanonicalRegularSeeds[1] == 1103);
            Require(OperationsAriaEvidenceHarness.CanonicalRegularSeeds[2] == 1104);
        }

        public static void CaptureUsesEnterPlaymodeAndScreenshot()
        {
            string source = ReadTestSource("OperationsAriaPlayModeCapture.cs");
            Require(source.Contains("ScreenCapture.CaptureScreenshot"), "screenshot");
            Require(source.Contains("win-screen"), "win_screen_name");
            Require(source.Contains("Status = \"AriaWon\"") || source.Contains("status=AriaWon"), "ariawon_write");
            Require(source.Contains("CapturePassMarkerPrefix"), "live_marker_const");
            Require(source.Contains("EditorApplication.Exit"), "owns_editor_exit");
            Require(source.Contains("owns_editor_exit=1") || source.Contains("ExitPendingKey"), "async_lifecycle");
            Require(source.Contains("presenter_ensure_failed"), "null_safe_presenter");
            Require(source.Contains("Game.Operations.Capture"), "uses_runtime_capture_asm");
            Require(source.Contains("TryWriteFallbackWinScreen") || source.Contains("TryEncodeFallbackPng"), "fallback_png");
        }

        public static void WiringMarkerDistinctFromLiveCapture()
        {
            Require(PassMarker.Contains("PlayModeCaptureValidation"), "validation_name");
            Require(PassMarker.Contains("checks=7"), "check_count");
            Require(!PassMarker.Contains("AriaWon"), "wiring_not_ariawon");
            Require(LiveCapturePassMarkerPrefix != PassMarker, "markers_differ");
            Require(LiveCapturePassMarkerPrefix.Contains("[OperationsAriaPlayModeCapture]"), "live_marker");
        }

        static string ReadTestSource(string fileName)
        {
            string path = Path.Combine(FindRepoRoot(), "Assets", "Tests", "Editor", "Operations", fileName);
            return File.ReadAllText(path);
        }

        static string ReadCapturePresentationSource() =>
            ReadCaptureSource("OperationsAriaPlayModePresentation.cs");

        static string ReadCaptureSource(string fileName)
        {
            string path = Path.Combine(
                FindRepoRoot(),
                "Assets",
                "Game",
                "Scripts",
                "Operations",
                "Capture",
                fileName);
            return File.ReadAllText(path);
        }

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

        static void Require(bool condition, string message = "require")
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
