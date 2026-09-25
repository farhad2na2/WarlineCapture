#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Unity.Collections;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static bool terminalProbeActive, terminalSnapshotValid;
        private static SkirmishMatchState terminalMatch;
        private static Entity terminalSession;
        private static FixedString64Bytes terminalAttempt;
        private static string terminalAction;
        private static float terminalElapsed;
        private static double terminalWall;
        private static float terminalTimeScale;
        private static int terminalPlayerHp, terminalEnemyHp, terminalStep;
        private static double terminalDue, terminalDeadline;
        private static AriaTouchInputUiSystemHelper terminalTouch;
        private static int terminalInterventions, terminalUnexpectedSamples;

        private static void StartTerminalProbe(EntityManager em, Entity session, SkirmishMatchState match, float elapsed)
        {
            terminalSnapshotValid = terminalProbeActive = true;
            terminalInterventions = terminalUnexpectedSamples = 0;
            terminalMatch = match;
            terminalSession = session;
            terminalAttempt = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            terminalAction = Environment.GetEnvironmentVariable("WARLINE_TERMINAL_ACTION") ?? "MAIN MENU";
            if (terminalAction != "MAIN MENU" && terminalAction != "REPLAY" && terminalAction != "ADJUST SETUP")
                throw new ArgumentException("Unsupported terminal action: " + terminalAction);
            terminalElapsed = elapsed;
            terminalWall = EditorApplication.timeSinceStartup - playingSince;
            terminalTimeScale = Time.timeScale;
            terminalPlayerHp = DesignatedBaseHp(em, session, 1);
            terminalEnemyHp = DesignatedBaseHp(em, session, 2);
            terminalStep = 0;
            terminalDue = EditorApplication.timeSinceStartup + 3d;
            terminalDeadline = terminalDue + 45d;
            var runner = new GameObject("SkirmishTerminalInputProbe");
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<SkirmishTerminalInputProbeFrame>();
        }

        internal static void TickTerminalPlayerFrame()
        {
            if (!terminalProbeActive) return;
            try
            {
                double now = EditorApplication.timeSinceStartup;
                if (now > terminalDeadline) { EndTerminalProbe(false, "returnTimeout"); return; }
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) { EndTerminalProbe(false, "worldLost"); return; }
                var em = world.EntityManager;
                terminalTouch?.Tick(Time.unscaledTime);
                if (terminalTouch != null && (terminalTouch.PlayerInterrupted || !terminalTouch.IsRunning))
                { EndTerminalProbe(false, "inputInterrupted"); return; }
                if (now < terminalDue) return;
                string prefix = LiveTracePath() + ".terminal";
                if (terminalStep == 0)
                {
                    if (!SkirmishLaunchProjection.TryGet(em, out var session, out var match) || session != terminalSession ||
                        match.Phase != SkirmishPhase.Finished || match.Outcome != terminalMatch.Outcome ||
                        Mathf.Abs(SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match) - terminalElapsed) > .05f ||
                        DesignatedBaseHp(em, session, 1) != terminalPlayerHp || DesignatedBaseHp(em, session, 2) != terminalEnemyHp ||
                        SkirmishLaunchProjection.IsSimulationActive(em))
                    { EndTerminalProbe(false, "terminalNotFrozen"); return; }
                    var view = UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
                    var button = view == null ? null : view.transform.Find("SkirmishResult/ResultCard/ResultActions/" + terminalAction)?.GetComponent<Button>();
                    if (!TryPresentedButtonPoint(button, out var point)) return;
                    ScreenCapture.CaptureScreenshot(prefix + "-result.png");
                    terminalTouch = new AriaTouchInputUiSystemHelper();
                    if (!terminalTouch.Start() || !terminalTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
                    { EndTerminalProbe(false, "returnTouchRejected"); return; }
                    terminalStep = 1;
                    terminalDue = now + 1d;
                    Debug.Log("[SkirmishTerminalProbe] frozen=1 returnInput=Touch target=" + terminalAction + " point=" + point);
                    return;
                }
                if (terminalStep == 1)
                {
                    if (terminalTouch.IsBusy || !UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.IsTransitionRunning) return;
                    if (terminalAction == "REPLAY")
                    {
                        if (!SkirmishLaunchProjection.TryGet(em, out var replay, out var replayMatch) ||
                            replayMatch.Phase != SkirmishPhase.Playing || !SkirmishLaunchProjection.IsSimulationActive(em)) return;
                        if (em.GetComponentData<SkirmishExpandedSessionComponent>(replay).SessionId.Equals(terminalAttempt) ||
                            SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, replay, in replayMatch) > 10f ||
                            DesignatedBaseHp(em, replay, 1) <= 0 || DesignatedBaseHp(em, replay, 2) <= 0)
                        { EndTerminalProbe(false, "replayNotFresh"); return; }
                    }
                    else
                    {
                        UIRoute expected = terminalAction == "ADJUST SETUP" ? UIRoute.QuickCustomSetup : UIRoute.MainMenu;
                        if (shell.ActiveRoute != expected) return;
                        if (SkirmishLaunchProjection.TryGet(em, out _, out _))
                        { EndTerminalProbe(false, "sessionNotCleaned"); return; }
                    }
                    ScreenCapture.CaptureScreenshot(prefix + "-destination.png");
                    terminalStep = 2;
                    terminalDue = now + 1d;
                    return;
                }
                EndTerminalProbe(true, "frozenResultAnd:" + terminalAction);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EndTerminalProbe(false, "exception");
            }
        }

        private static void EndTerminalProbe(bool passed, string reason)
        {
            terminalInterventions = (int)(terminalTouch?.PhysicalInterventions ?? 0);
            terminalUnexpectedSamples = (int)(terminalTouch?.UnexpectedSamples ?? 0);
            if (terminalUnexpectedSamples > 0) { passed = false; reason = "unexpectedTerminalInput"; }
            Debug.Log("[SkirmishTerminalProbe] result=" + (passed ? "Passed" : "Failed") + " reason=" + reason +
                " completed=" + (terminalTouch?.CompletedGestures ?? 0) + " interventions=" + (terminalTouch?.PhysicalInterventions ?? 0));
            terminalProbeActive = false;
            terminalTouch?.Dispose(); terminalTouch = null;
            Finish(!passed, passed ? null : "terminal:" + reason);
        }
    }

    internal sealed class SkirmishTerminalInputProbeFrame : MonoBehaviour
    {
        private void Update() => SkirmishS002AriaRunHarness.TickTerminalPlayerFrame();
    }
}
#endif
