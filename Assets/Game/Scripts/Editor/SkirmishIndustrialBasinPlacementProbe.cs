#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class SkirmishIndustrialBasinPlacementProbe
    {
        private const string Key = "Warline.IndustrialBasinPlacement";
        private const string EvidenceDirectory = "/private/tmp/warline-s3-aria";
        private static int stage;
        private static double next;
        private static double deadline;

        static SkirmishIndustrialBasinPlacementProbe()
        {
            if (SessionState.GetBool(Key, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Tools/Warline/Skirmish/Launch S3 Industrial Basin Placement Probe")]
        public static void Launch()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit play mode first.");
            SessionState.SetBool(Key, true);
            stage = 0;
            next = 0;
            deadline = 0;
            Directory.CreateDirectory(EvidenceDirectory);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
                return;
            double now = EditorApplication.timeSinceStartup;
            if (deadline == 0)
                deadline = now + 180;
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
                    GameLocalization.SetLocale("en", false);
                    if (!SkirmishLaunchProjection.IsShellReadyToEnterMatch(em))
                    {
                        next = now + 0.5;
                        return;
                    }
                    QuickGameConfig config = QuickGameConfig.Defaults;
                    config.MapSeed = 104729;
                    config.ScenarioIndex = SkirmishPresetConfig.IndustrialBasinScenarioIndex;
                    if (SkirmishLaunchProjection.TryGet(em, out _, out _))
                        return;
                    if (!SkirmishLaunchProjection.TryQueue(em, config))
                        return;
                    stage = 1;
                    next = now + 2;
                    return;
                }

                if (!SkirmishLaunchProjection.TryGet(em, out _, out SkirmishMatchState match))
                    return;
                SkirmishLaunchProjection.TryEnterMatch(em);
                SkirmishLaunchProjection.TryBeginGameplayIfLoaded();
                SkirmishLaunchProjection.TryRequestPlay(em);

                if (match.StartupFailure != SkirmishStartupFailureCode.None ||
                    match.Phase == SkirmishPhase.Playing)
                {
                    bool passed;
                    string audit = SkirmishLayoutAudit.Inspect(em, out passed);
                    string path = Path.Combine(EvidenceDirectory, "placement-audit.txt");
                    File.WriteAllText(path,
                        $"phase={match.Phase} fail={match.StartupFailure} auditPassed={passed}\n{audit}");
                    Debug.Log($"[SkirmishIndustrialBasinPlacement] result={(passed && match.Phase == SkirmishPhase.Playing ? "Passed" : "Failed")} path={path}\n{audit}");
                    Finish(passed && match.Phase == SkirmishPhase.Playing ? "passed" : "failed");
                }
                else
                    next = now + 0.5;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish("exception");
            }
        }

        private static void Finish(string reason)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(Key, false);
            Debug.Log($"[SkirmishIndustrialBasinPlacement] done reason={reason}");
            // Keep play mode so follow-up eval can inspect the live map.
        }
    }
}
#endif
