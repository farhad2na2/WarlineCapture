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
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class SkirmishS004GameViewCapture
    {
        private const string Key = "Warline.S004.GameViewCapture";
        private const string RegistryPath = "Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset";
        private static int stage;
        private static double next;
        private static double deadline;
        private static string lastCapturePath;

        static SkirmishS004GameViewCapture()
        {
            if (SessionState.GetBool(Key, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Tools/Warline/Skirmish/Launch S004 Regular Standard Game View")]
        public static void LaunchRegularStandardMenu() => LaunchRegularStandard();

        public static void RunFocusedLaunchRegularStandard() => LaunchRegularStandard();

        public static void LaunchRegularStandard()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit play mode before launching S004 Regular Standard.");

            SessionState.SetBool(Key, true);
            stage = 0;
            next = 0;
            deadline = 0;
            lastCapturePath = null;
            Directory.CreateDirectory(EvidenceDirectory());

            SessionState.SetString("Warline.AriaPlayValidation",
                Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT") ?? "");
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",
                "/private/tmp/aria-play-validation-profile");
            var save = new SaveService(new JsonSaveRepository("/private/tmp/aria-play-validation-profile"));
            var profile = save.LoadProfile();
            profile.firstLaunchStatus = FirstLaunchProfileState.Completed;
            profile.firstLaunchWatched = true;
            save.SaveProfile(profile);

            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            EditorApplication.EnterPlaymode();
        }

        public static string EvidenceDirectory()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(root, SkirmishAcceptanceScaffold.RelativeEvidenceDirectory);
        }

        public static string ReportEvidenceDirectory()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(root, SkirmishAcceptanceScaffold.S004RelativeReportEvidenceDirectory);
        }

        public static string DescribeLaunchPayload()
        {
            if (!SkirmishAriaAcceptancePayload.TryCreateFirstVisitS004(
                    SkirmishS004FirstVisit.SeedA,
                    GameLocalization.EnglishLocaleCode,
                    out SkirmishAriaAcceptancePayload payload,
                    out string error))
                throw new InvalidOperationException(error);
            return "[SkirmishS004GameView] selected " + payload.FormatSelectedConfiguration();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
                return;

            double now = EditorApplication.timeSinceStartup;
            if (deadline == 0d)
                deadline = now + 180d;
            if (now > deadline)
            {
                Finish("timeout");
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
                    GameLocalization.SetLocale(GameLocalization.EnglishLocaleCode, false);
                    if (!SkirmishLaunchProjection.IsShellReadyToEnterMatch(em))
                    {
                        next = now + 0.5d;
                        return;
                    }

                    if (!TryQueueExpanded(em))
                        return;
                    Debug.Log(DescribeLaunchPayload());
                    stage = 1;
                    next = now + 2d;
                    return;
                }

                if (!SkirmishLaunchProjection.TryGet(em, out _, out SkirmishMatchState match))
                    return;

                SkirmishLaunchProjection.TryEnterMatch(em);
                SkirmishLaunchProjection.TryBeginGameplayIfLoaded();
                SkirmishLaunchProjection.TryRequestPlay(em);

                if (match.StartupFailure != SkirmishStartupFailureCode.None)
                {
                    Finish("startupFailure=" + match.StartupFailure);
                    return;
                }

                if (match.Phase != SkirmishPhase.Playing)
                {
                    next = now + 0.5d;
                    return;
                }

                if (stage == 1)
                {
                    if (match.Seed != SkirmishS004FirstVisit.SeedA)
                    {
                        Finish("seed=" + match.Seed.ToString(CultureInfo.InvariantCulture));
                        return;
                    }

                    lastCapturePath = CapturePlayingFrame(in match);
                    stage = 2;
                    next = now + 1d;
                    return;
                }

                Finish("captured");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish("exception");
            }
        }

        private static bool TryQueueExpanded(EntityManager em)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!SkirmishSetupMatrixTable.TryLoad(root, out List<SkirmishSetupMatrixRow> matrix, out string error))
                throw new InvalidOperationException(error);

            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var manifest = new SkirmishContentManifest
            {
                RequiredFeatureIds = authored.DefinitionS004.RequiredFeatureIds
            };
            UnitPrefabRegistryAuthoringConfig registry =
                AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(RegistryPath);

            if (SkirmishLaunchProjection.TryGet(em, out _, out _))
                return false;
            if (!SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                    em,
                    SkirmishAcceptanceCensusCapture.S004CatalogId,
                    SkirmishDifficultyId.Regular,
                    SkirmishSizeId.Standard,
                    SkirmishS004FirstVisit.SeedA,
                    authored,
                    matrix,
                    manifest,
                    out SkirmishResolvedSetup setup,
                    out _,
                    out List<SkirmishCompileReason> reasons,
                    registry))
                throw new InvalidOperationException(reasons == null || reasons.Count == 0 ? "S004 queue failed" : reasons[0].ToString());

            if (setup.CatalogId != SkirmishAcceptanceCensusCapture.S004CatalogId ||
                setup.DefinitionId != SkirmishAcceptanceCensusCapture.S004DefinitionId ||
                setup.Seed != SkirmishS004FirstVisit.SeedA ||
                setup.DifficultyId != SkirmishDifficultyId.Regular ||
                setup.SizeId != SkirmishSizeId.Standard)
                throw new InvalidOperationException("S004 Game View queue drifted from Regular Standard seed 104733.");

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "[SkirmishS004GameView] queued catalog={0} seed={1} hash={2:X8} playable=0",
                setup.CatalogId,
                setup.Seed,
                setup.SetupHash));
            return true;
        }

        private static string CapturePlayingFrame(in SkirmishMatchState match)
        {
            string primary = EvidenceDirectory();
            string report = ReportEvidenceDirectory();
            Directory.CreateDirectory(primary);
            Directory.CreateDirectory(report);
            string png = Path.Combine(primary, SkirmishAcceptanceScaffold.S004PlayingPngFileName);
            ScreenCapture.CaptureScreenshot(png);
            WriteSidecar(primary, in match);
            WriteSidecar(report, in match);
            Debug.Log("[SkirmishS004GameView] captured " + png);
            return png;
        }

        private static void WriteSidecar(string directory, in SkirmishMatchState match)
        {
            File.WriteAllText(
                Path.Combine(directory, SkirmishAcceptanceScaffold.S004GameViewSidecarFileName),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{{\"catalog\":\"{0}\",\"definition\":\"{1}\",\"size\":\"Standard\",\"difficulty\":\"Regular\",\"seed\":{2},\"phase\":\"{3}\",\"outcome\":\"{4}\",\"forcedVictory\":0}}\n",
                    SkirmishAcceptanceCensusCapture.S004CatalogId,
                    SkirmishAcceptanceCensusCapture.S004DefinitionId,
                    match.Seed,
                    match.Phase,
                    match.Outcome));
        }

        private static void CopyPngToReportFolder()
        {
            string source = Path.Combine(EvidenceDirectory(), SkirmishAcceptanceScaffold.S004PlayingPngFileName);
            if (!File.Exists(source))
                return;
            Directory.CreateDirectory(ReportEvidenceDirectory());
            File.Copy(
                source,
                Path.Combine(ReportEvidenceDirectory(), SkirmishAcceptanceScaffold.S004PlayingPngFileName),
                true);
        }

        private static void Finish(string reason)
        {
            if (reason == "captured")
                CopyPngToReportFolder();
            EditorApplication.update -= Tick;
            SessionState.SetBool(Key, false);
            string previous = SessionState.GetString("Warline.AriaPlayValidation", "");
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",
                string.IsNullOrEmpty(previous) ? null : previous);
            Debug.Log("[SkirmishS004GameView] result=" +
                      (reason == "captured" ? "Passed" : "Failed") +
                      " reason=" + reason +
                      (string.IsNullOrEmpty(lastCapturePath) ? string.Empty : " evidence=" + lastCapturePath) +
                      " stayInPlayMode=1");
        }
    }
}
#endif
