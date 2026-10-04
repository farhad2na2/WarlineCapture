#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static AriaTouchInputUiSystemHelper manualTouch;
        private static int manualSlot, manualStep, manualActions, manualAttackReceipts;
        private static double manualDue, manualDeadline;

        public static void RunS004ManualAndExit()
        {
            Environment.SetEnvironmentVariable("WARLINE_S004_MANUAL_PROBE", "1");
            RunS004AriaAndExit();
        }

        private static void StartS004ManualProbe()
        {
            manualSlot = manualStep = manualActions = manualAttackReceipts = 0;
            manualDue = EditorApplication.timeSinceStartup + 1d;
            manualDeadline = manualDue + 600d;
            manualTouch = new AriaTouchInputUiSystemHelper();
            if (!manualTouch.Start()) throw new InvalidOperationException("Manual touch driver unavailable.");
            var runner = new GameObject("S004ManualControlsProbe");
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<SkirmishS004ManualInputFrame>();
            Debug.Log("[S004ManualNative] started=1 input=Touch ariaStarted=0 strategy=visibleSquadCardsAttackVisibleBase");
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (SkirmishLaunchProjection.TryGet(em, out var session, out _))
                Debug.Log("[S004NativeCheckpoint] captureAvailable=" +
                    (SkirmishCheckpointService.TryCapture(em, session, out _) ? 1 : 0) +
                    " acceptance=PendingNativeForceRecovery");
        }

        private static void StopS004ManualProbe()
        {
            if (manualTouch != null)
            {
                Debug.Log("[S004ManualInput] completed=" + manualTouch.CompletedGestures +
                    " interventions=" + manualTouch.PhysicalInterventions + " unexpected=" + manualTouch.UnexpectedSamples);
                manualTouch.Dispose();
                manualTouch = null;
            }
        }

        internal static void TickS004ManualPlayerFrame()
        {
            if (manualTouch == null || terminalProbeActive) return;
            try
            {
                manualTouch.Tick(Time.unscaledTime);
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                if (!SkirmishLaunchProjection.TryGet(world.EntityManager, out _, out var match) ||
                    match.Phase != SkirmishPhase.Playing) return;
                if (UiShellRuntimeGateway.ReadAriaPlay().Active)
                    throw new InvalidOperationException("ARIA unexpectedly active during manual-controls test.");
                if (!Mathf.Approximately(Time.timeScale, 1f))
                    throw new InvalidOperationException("Manual test is not running at normal speed.");
                if (manualTouch.PlayerInterrupted || !manualTouch.IsRunning || manualTouch.UnexpectedSamples > 0)
                    throw new InvalidOperationException("Manual controls input interrupted.");
                double now = EditorApplication.timeSinceStartup;
                if (now > manualDeadline) throw new TimeoutException("Manual mission did not finish within ten minutes.");
                if (manualTouch.IsBusy || now < manualDue) return;
                if (!UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page)) return;
                var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
                var hud = UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
                var commands = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
                if (tray == null || hud == null || commands == null) return;

                if (manualSlot >= 5)
                {
                    if (page.NextPage)
                    {
                        if (ManualTap(tray.NextPageButton)) manualSlot = manualStep = 0;
                    }
                    else
                    {
                        manualDue = now + 15d;
                        manualStep = 10;
                        manualSlot = 0;
                    }
                    return;
                }
                if (manualStep == 10)
                {
                    if (page.PreviousPage) { ManualTap(tray.PreviousPageButton); return; }
                    manualStep = 0;
                }
                int bit = 1 << manualSlot;
                if (manualStep == 0)
                {
                    // StructureMask means structure-damaging assault squads (for example
                    // Rocketeers), not scenery or buildings. Include them in the attack.
                    if ((page.AssaultMask & bit) == 0)
                    { manualSlot++; return; }
                    if (page.SelectedGroups > 0)
                    { ManualTap(tray.ClearSelectionButton); return; }
                    if (ManualTap(tray.VisibleCardButton(manualSlot))) manualStep = 1;
                }
                else if (manualStep == 1)
                {
                    if ((page.SelectedMask & bit) == 0) return;
                    if (ManualTap(hud.EnemyFocusButton)) manualStep = 2;
                }
                else if (manualStep == 2)
                {
                    if (ManualTap(commands.AttackButton)) manualStep = 3;
                }
                else if (manualStep == 3)
                {
                    // Use only the objective marker exposed to the player, never a hidden entity position.
                    if (!hud.EnemyBaseMarkerVisible || !Screen.safeArea.Contains(hud.VisibleEnemyBasePoint)) return;
                    if (ManualGesture(hud.VisibleEnemyBasePoint)) manualStep = 4;
                }
                else if (manualStep == 4)
                {
                    if ((page.AttackOrderMask & bit) == 0) { manualStep = 2; return; }
                    manualAttackReceipts++;
                    Debug.Log("[S004ManualAttack] verified=1 page=" + page.PageIndex + " slot=" + manualSlot);
                    ScreenCapture.CaptureScreenshot(LiveTracePath() + ".manual-orders.png");
                    manualSlot++;
                    manualStep = 0;
                }
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Finish(true, "manualControls:" + error.Message);
            }
        }

        private static bool ManualTap(Button button) => TryPresentedButtonPoint(button, out var point) && ManualGesture(point);

        private static bool ManualGesture(Vector2 point)
        {
            if (!manualTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime)) return false;
            manualActions++;
            manualDue = EditorApplication.timeSinceStartup + .8d;
            return true;
        }
    }

    internal sealed class SkirmishS004ManualInputFrame : MonoBehaviour
    {
        private void Update() => SkirmishS002AriaRunHarness.TickS004ManualPlayerFrame();
    }
}
#endif
