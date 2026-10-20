#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static bool sagaRestartActive, sagaRestartEnding, sagaRestartRefresh;
        private static bool sagaRestartOptionsEnabled;
        private static EnterPlayModeOptions sagaRestartOptions;
        private static string sagaRestartPreviousProfile, sagaRestartPreviousBackground;
        private static JObject sagaRestartPrior;
        private static string sagaRestartPath, sagaRestartSha, sagaRestartHash;
        private static double sagaRestartDeadline, sagaRestartFinishAt;
        private static int sagaRestartCode;

        // Reads the profile earned by a separate, completed native ARIA process.
        // This entry never seeds medals, starts a match, or injects UI callbacks.
        public static void RunSagaProcessRestartProof()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Restart proof requires a fresh, idle Editor process.");
            sagaRestartPath = Path.GetFullPath(Environment.GetEnvironmentVariable("WARLINE_SAGA_MEDAL_PROOF") ?? throw new InvalidOperationException("Prior native medal proof path required."));
            string owned = Path.GetFullPath(Path.Combine(Application.dataPath, "../Design/AgentReports/SkirmishExpansion/S002/_Evidence")) + Path.DirectorySeparatorChar;
            if (!sagaRestartPath.StartsWith(owned, StringComparison.Ordinal) || !sagaRestartPath.EndsWith("-saga-medal-proof.json", StringComparison.Ordinal))
                throw new InvalidOperationException("Exact owned native medal proof required.");
            byte[] bytes = File.ReadAllBytes(sagaRestartPath);
            using (var sha = SHA256.Create()) sagaRestartSha = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            sagaRestartPrior = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
            int priorPid = (int?)sagaRestartPrior["producerPid"] ?? 0;
            if (priorPid <= 0 || priorPid == System.Diagnostics.Process.GetCurrentProcess().Id ||
                (string)sagaRestartPrior["outcome"] != "Victory" || (bool?)sagaRestartPrior["diskReloadHasMedal"] != true ||
                ((bool?)sagaRestartPrior["nativeStopRibbonVisible"] != true && (bool?)sagaRestartPrior["nativeMissionCardMedalVisible"] != true))
                throw new InvalidOperationException("A distinct completed native victory process is required.");
            catalogId = (string)sagaRestartPrior["scenario"];
            if (catalogId != "S002") throw new InvalidOperationException("This restart probe is scoped to S002.");
            locale = (string)sagaRestartPrior["locale"];
            if (locale != "en" && locale != "fa-IR") throw new InvalidOperationException("Known native locale required.");
            string profile = Path.GetFullPath((string)sagaRestartPrior["profileRoot"] ?? "");
            string profileParent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "aria-play-validation-profile"));
            if (Path.GetDirectoryName(profile) != profileParent || !Guid.TryParseExact(Path.GetFileName(profile), "N", out _) || !Directory.Exists(profile))
                throw new InvalidOperationException("Original isolated native profile directory required.");
            sagaRestartHash = SkirmishCandidateSourceHash.Compute(Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
            if (sagaRestartHash != (string)sagaRestartPrior["candidateSourceHash"])
                throw new InvalidOperationException("Restart must use the same candidate source as the actual victory.");
            sagaRestartPreviousProfile = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            sagaRestartPreviousBackground = Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION");
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT", profile);
            Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION", "1");
            // Load only: no first-launch, progression, ownership or medal fixture.
            var ledger = new SkirmishMedalLedger(SaveService.CreateDefault().LoadProfile().skirmishMedals, SkirmishBattleCatalogConfig.Load().Entries);
            terminalSagaDifficulty = (SkirmishDifficultyId)Enum.Parse(typeof(SkirmishDifficultyId), (string)sagaRestartPrior["difficulty"]);
            if (!ledger.Has(catalogId, terminalSagaDifficulty)) throw new InvalidOperationException("Earned medal missing at cold load.");
            sagaRestartOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            sagaRestartOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions |= EnterPlayModeOptions.DisableDomainReload;
            AssetDatabase.DisallowAutoRefresh();
            sagaRestartRefresh = true;
            sagaRestartActive = true;
            sagaRestartEnding = false;
            sagaRestartDeadline = EditorApplication.timeSinceStartup + 300;
            menuTouch = null;
            menuStep = menuLibraryBeat = 0;
            MainMenuV3PrefabBuilder.SetGameViewResolution(2400, 1080);
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            EditorApplication.update += TickSagaRestart;
            EditorApplication.EnterPlaymode();
        }

        private static void TickSagaRestart()
        {
            if (sagaRestartEnding)
            {
                if (EditorApplication.timeSinceStartup < sagaRestartFinishAt) return;
                if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.ExitPlaymode(); return; }
                EditorApplication.update -= TickSagaRestart;
                EditorSettings.enterPlayModeOptions = sagaRestartOptions;
                EditorSettings.enterPlayModeOptionsEnabled = sagaRestartOptionsEnabled;
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT", sagaRestartPreviousProfile);
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION", sagaRestartPreviousBackground);
                if (sagaRestartRefresh) { AssetDatabase.AllowAutoRefresh(); sagaRestartRefresh = false; }
                EditorApplication.Exit(sagaRestartCode);
                return;
            }
            try
            {
                if (EditorApplication.timeSinceStartup > sagaRestartDeadline) { EndSagaRestart(false, "deadline"); return; }
                if (!EditorApplication.isPlaying) return;
                GameLocalization.SetLocale(locale, false);
                if (menuTouch == null) EnterS004ThroughPresentedControls();
            }
            catch (Exception e) { Debug.LogException(e); EndSagaRestart(false, "exception"); }
        }

        private static void VerifySagaRestartPresented()
        {
            try
            {
                var setup = UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Exclude)
                    .FirstOrDefault(x => x.IsBaseAssaultSetup && x.SelectedScenarioId == catalogId);
                if (setup == null || setup.Library.Level != SkirmishLibraryLevel.Battlefield) return;
                var ledger = new SkirmishMedalLedger(SaveService.CreateDefault().LoadProfile().skirmishMedals, SkirmishBattleCatalogConfig.Load().Entries);
                var mask = terminalSagaDifficulty switch { SkirmishDifficultyId.Recruit => SkirmishMedalMask.Recruit, SkirmishDifficultyId.Regular => SkirmishMedalMask.Regular, SkirmishDifficultyId.Veteran => SkirmishMedalMask.Veteran, _ => SkirmishMedalMask.Commander };
                bool ribbon = setup.GetComponentsInChildren<Transform>(false).Any(x => x.name.StartsWith("EarnedRibbon_", StringComparison.Ordinal) && x.parent.name == SkirmishLibraryControlNames.BattleCard(catalogId));
                bool cardMedal=false;
                if(setup.ApprovedAtlas!=null)
                {
                    foreach(var region in setup.ApprovedAtlas.Regions)foreach(var card in region.Cards)
                        if(card.Rect.name==SkirmishLibraryControlNames.BattleCard(catalogId))
                        {var icon=card.MedalIcons[(int)terminalSagaDifficulty-1];cardMedal=icon.gameObject.activeInHierarchy && icon.sprite==Resources.Load<Sprite>(SkirmishBattleLibraryArt.Medal(true,terminalSagaDifficulty));}
                    ribbon=cardMedal;
                }
                if (!ledger.Has(catalogId, terminalSagaDifficulty) || (setup.Library.Briefing.Medals & mask) == 0 || !ribbon || setup.Library.Theater.OverallWonCount < 1)
                    throw new InvalidOperationException("Cold profile/UI medal, ribbon or theatre totals did not persist.");
                if (menuTouch.CompletedGestures < 2 || menuTouch.UnexpectedSamples != 0 || menuTouch.PhysicalInterventions != 0)
                    throw new InvalidOperationException("Native restart navigation input was not clean.");
                string prefix = sagaRestartPath + ".process-restart";
                ScreenCapture.CaptureScreenshot(prefix + ".png");
                File.WriteAllText(prefix + ".json", new JObject { ["result"] = "Passed", ["priorProof"] = sagaRestartPath, ["priorProofSha256"] = sagaRestartSha, ["priorPid"] = sagaRestartPrior["producerPid"], ["restartPid"] = System.Diagnostics.Process.GetCurrentProcess().Id, ["candidateSourceHash"] = sagaRestartHash, ["locale"] = locale, ["scenario"] = catalogId, ["nativeTouchGestures"] = menuTouch.CompletedGestures, ["diskMedal"] = true, ["nativeRibbon"] = setup.ApprovedAtlas==null, ["nativeMissionCardMedal"] = cardMedal, ["nativeBriefingMedal"] = true, ["profileSeeded"] = false }.ToString());
                EndSagaRestart(true, "coldProfileNativeMenuMedal");
            }
            catch (Exception e) { Debug.LogException(e); EndSagaRestart(false, "nativeMenuProof"); }
        }

        private static void EndSagaRestart(bool pass, string reason)
        {
            if (sagaRestartEnding) return;
            sagaRestartEnding = true;
            sagaRestartActive = false;
            sagaRestartCode = pass ? 0 : 2;
            sagaRestartFinishAt = EditorApplication.timeSinceStartup + 2;
            StopS004MenuProbe();
            RestoreBackgroundInput();
            Debug.Log("[SagaProcessRestart] result=" + (pass ? "Passed" : "Failed") + " reason=" + reason + " profileSeeded=0 nativeTouch=1");
        }
    }
}
#endif
