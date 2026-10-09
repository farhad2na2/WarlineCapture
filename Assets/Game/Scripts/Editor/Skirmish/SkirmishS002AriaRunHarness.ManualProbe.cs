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
        private static bool manualCycleHasStructure, manualNeedsReplacement;
        private static double manualReplacementNext;
        private static uint manualReplacementReceipt;
        private static uint manualCompletedGestures, manualUnexpectedSamples, manualPhysicalInterventions;
        private static uint manualPriorGestures;
        private static bool manualReceiptAuditMeasured;
        private static int manualReceiptAuditViolations, manualRecordedCommands;
        private static int manualVerifiedAttackMoves, manualAttackMovesBeforeOrder;
        private static bool manualAwaitingStructureOrder;
        private static int manualOrderRetries;
        private static bool manualCameraFocusPending;
        private static bool manualCameraFocusStarted;
        private static Vector3 manualCameraFocusSite;

        public static void RunS004ManualAndExit()
        {
            Environment.SetEnvironmentVariable("WARLINE_S004_MANUAL_PROBE", "1");
            RunS004AriaAndExit();
        }

        private static void StartS004ManualProbe()
        {
            manualSlot = manualStep = manualActions = manualAttackReceipts = 0;
            bool resumedCombat = playerResumeCombatInputCarryPending;
            manualPriorGestures = 0;
            if (resumedCombat)
            {
                var carryEm = World.DefaultGameObjectInjectionWorld.EntityManager;
                if (!playerResumeProbeDone || playerResumeProbeActive || !playerResumeCombat ||
                    !SkirmishLaunchProjection.TryGet(carryEm, out var carriedSession, out _) ||
                    carryEm.GetComponentData<SkirmishExpandedSessionComponent>(carriedSession).SessionId.ToString() != playerResumeSessionId ||
                    playerResumeCombatAttackReceipts <= 0 || !AriaCommandEvidence.Active)
                    throw new InvalidOperationException("Cannot carry combat input into a different or unverified attempt.");
                manualPriorGestures = playerResumeCarriedGestures;
                manualAttackReceipts = playerResumeCombatAttackReceipts;
                manualActions = (int)manualPriorGestures;
                playerResumeCombatInputCarryPending = false;
            }
            manualCycleHasStructure = manualNeedsReplacement = false;
            manualReplacementNext = 0; manualReplacementReceipt = 0;
            manualCompletedGestures = manualUnexpectedSamples = manualPhysicalInterventions = 0;
            manualReceiptAuditMeasured = false; manualReceiptAuditViolations = -1; manualRecordedCommands = 0;
            manualVerifiedAttackMoves = manualAttackMovesBeforeOrder = 0;
            manualAwaitingStructureOrder = false;
            manualOrderRetries = 0;
            manualCameraFocusPending = false;
            manualCameraFocusStarted = false;
            AriaCommandEvidence.Recorded -= ObserveManualAttackMoveReceipt;
            AriaCommandEvidence.Recorded += ObserveManualAttackMoveReceipt;
            // Pre-save combat shares the already observed recruitment stream.
            // A verified combat Continue retains that same attempt's recorder;
            // fresh manual play starts its own strict receipt audit.
            if (!resumedCombat && (!playerResumeProbeActive || !playerResumeCombat)) AriaCommandEvidence.Begin();
            manualDue = EditorApplication.timeSinceStartup + 1d;
            manualDeadline = manualDue + 600d;
            manualTouch = new AriaTouchInputUiSystemHelper();
            if (!manualTouch.Start()) throw new InvalidOperationException("Manual touch driver unavailable.");
            var runner = new GameObject("S004ManualControlsProbe");
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<SkirmishS004ManualInputFrame>();
            Debug.Log("[S004ManualNative] started=1 input=Touch ariaStarted=0 strategy=visibleSquadCardsEscortAndAttackVisibleBase");
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (SkirmishLaunchProjection.TryGet(em, out var session, out _))
                Debug.Log("[S004NativeCheckpoint] captureAvailable=" +
                    (SkirmishCheckpointService.TryCapture(em, session, out _) ? 1 : 0) +
                    " acceptance=PendingNativeForceRecovery");
        }

        private static void StopS004ManualProbe()
        {
            AriaCommandEvidence.Recorded -= ObserveManualAttackMoveReceipt;
            if (manualTouch != null)
            {
                manualCompletedGestures = manualPriorGestures + manualTouch.CompletedGestures;
                manualUnexpectedSamples = manualTouch.UnexpectedSamples;
                manualPhysicalInterventions = manualTouch.PhysicalInterventions;
                manualRecordedCommands = AriaCommandEvidence.AcceptedCommands;
                manualReceiptAuditMeasured = AriaCommandEvidence.TryReadAudit(
                    manualCompletedGestures, manualUnexpectedSamples, out manualReceiptAuditViolations);
                Debug.Log("[S004ManualInput] completed=" + manualTouch.CompletedGestures +
                    " interventions=" + manualTouch.PhysicalInterventions + " unexpected=" + manualTouch.UnexpectedSamples);
                manualTouch.Dispose();
                manualTouch = null;
            }
        }

        private static void ObserveManualAttackMoveReceipt(string operation, uint receipt, bool verified)
        {
            if (manualTouch != null && verified && operation is "AttackMove" or "ArmyAttack")
                manualVerifiedAttackMoves++;
        }

        internal static void TickS004ManualPlayerFrame()
        {
            if (manualTouch == null || terminalProbeActive) return;
            try
            {
                manualTouch.Tick(Time.unscaledTime);
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                if (!SkirmishLaunchProjection.TryGet(world.EntityManager, out var session, out var match) ||
                    match.Phase != SkirmishPhase.Playing) return;
                if (UiShellRuntimeGateway.ReadAriaPlay().Active)
                    throw new InvalidOperationException("ARIA unexpectedly active during manual-controls test.");
                if (!Mathf.Approximately(Time.timeScale, 1f))
                    throw new InvalidOperationException("Manual test is not running at normal speed.");
                ObservePaidGroundRole(world.EntityManager, session);
                if (manualTouch.PlayerInterrupted || !manualTouch.IsRunning || manualTouch.UnexpectedSamples > 0)
                    throw new InvalidOperationException("Manual controls input interrupted.");
                double now = EditorApplication.timeSinceStartup;
                if (now > manualDeadline) throw new TimeoutException("Manual mission did not finish within ten minutes.");
                if (manualTouch.IsBusy || now < manualDue) return;
                // The full-map modal hides the squad tray and public markers.
                // Finish the latched camera gesture before interpreting that
                // temporary presentation change as a casualty/roster change.
                if (manualCameraFocusPending)
                {
                    TickManualCameraFocus(now);
                    return;
                }
                // Opening Build deliberately hides the squad tray/command bar.
                // Finish that native drawer sequence before requiring army controls.
                var commands = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>(FindObjectsInactive.Include);
                if (manualNeedsReplacement && now >= manualReplacementNext &&
                    TickManualReplacement(world.EntityManager, now, commands)) return;
                if (!UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page)) return;
                var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
                var hud = UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
                if (tray == null || hud == null || commands == null || !commands.isActiveAndEnabled) return;
                // A one-page army has no off-page structure counter. React even
                // if its last counter died during an outstanding order sequence.
                if (page.PageCount == 1 && page.StructureMask == 0) manualNeedsReplacement = true;
                if (manualNeedsReplacement && now >= manualReplacementNext &&
                    TickManualReplacement(world.EntityManager, now, commands)) return;
                manualCycleHasStructure |= page.StructureMask != 0;

                if (manualSlot >= 5)
                {
                    if (page.NextPage)
                    {
                        if (ManualTap(tray.NextPageButton)) manualSlot = manualStep = 0;
                    }
                    else
                    {
                        manualNeedsReplacement = !manualCycleHasStructure;
                        manualCycleHasStructure = false;
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
                // Casualties can move the selected group to another visible
                // slot between selection and issuing its order. Re-select from
                // the current public roster before using that slot's role.
                if (manualStep >= 2 && manualStep <= 4 && (page.SelectedMask & bit) == 0)
                {
                    Debug.Log("[S004ManualAttack] phase=RosterChanged page=" + page.PageIndex +
                        " slot=" + manualSlot + " selectedMask=" + page.SelectedMask + " input=Touch");
                    manualStep = 0;
                    return;
                }
                // Structure counters attack the public base marker. Other
                // combat groups escort them with a native Attack-move on ground.
                // Never retry an unsupported APC/rifle attack on the structure.
                if ((page.AssaultMask & bit) == 0)
                { manualSlot++; manualStep = 0; return; }
                if (manualStep == 0)
                {
                    manualOrderRetries = 0;
                    airProbeDefenseMapAttempts = 0;
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
                    // Attack is a toggle. A rejected target must not make the
                    // next retry toggle targeting off before the world touch.
                    if (UiShellRuntimeGateway.TryReadMatchHudCommandState(out var mode) &&
                        mode.ActiveCommandMode == Game.Tactical.Contracts.TacticalCommandMode.Attack)
                        manualStep = 3;
                    else if (ManualTap(commands.AttackButton)) manualStep = 3;
                }
                else if (manualStep == 3)
                {
                    if (!TryManualAssaultPoint(hud, (page.StructureMask & bit) != 0, manualSlot, now, out var point)) return;
                    int beforeOrder = manualVerifiedAttackMoves;
                    if (ManualGesture(point))
                    {
                        manualAwaitingStructureOrder = (page.StructureMask & bit) != 0;
                        manualAttackMovesBeforeOrder = beforeOrder;
                        manualStep = 4;
                    }
                }
                else if (manualStep == 4)
                {
                    // The page mask acknowledges direct structure attacks. Ground
                    // escorts use the shared AttackMove owner and its verified
                    // native input receipt, so they must not wait for that mask.
                    bool acknowledged = manualAwaitingStructureOrder
                        ? (page.AttackOrderMask & bit) != 0
                        : manualVerifiedAttackMoves > manualAttackMovesBeforeOrder;
                    if (!acknowledged)
                    {
                        if (++manualOrderRetries < 3) { manualStep = 2; return; }
                        // Retain rejected-order evidence and continue the public
                        // roster scan so loss of the last structure counter can
                        // reach the ordinary paid-reinforcement strategy.
                        Debug.Log("[S004ManualAttack] phase=Unacknowledged page=" + page.PageIndex +
                            " slot=" + manualSlot + " attempts=" + manualOrderRetries + " input=Touch");
                        manualSlot++; manualStep = 0;
                        return;
                    }
                    manualAttackReceipts++;
                    Debug.Log("[S004ManualAttack] verified=1 page=" + page.PageIndex + " slot=" + manualSlot +
                        " kind=" + (manualAwaitingStructureOrder ? "StructureAttack" : "EscortAttackOrAdvance"));
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

        private static bool TryManualAssaultPoint(SkirmishMatchView hud, bool structure, int slot, double now, out Vector2 point)
        {
            point = default;
            if (!hud.EnemyBaseMarkerVisible || Camera.main == null) return false;
            Vector3 site = hud.EnemyBaseObjectivePosition;
            if (structure) point = hud.VisibleEnemyBasePoint;
            else
            {
                // The same objective positions are already exposed by the HUD.
                // This is an escort destination, not a hidden enemy unit lookup.
                Vector3 forward = hud.EnemyBaseObjectivePosition - hud.PlayerBaseObjectivePosition;
                forward.y = 0;
                if (forward.sqrMagnitude < 1f) return false;
                forward.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, forward);
                site = hud.EnemyBaseObjectivePosition - forward * 40f + side * ((slot - 2) * 3f);
                Vector3 screen = Camera.main.WorldToScreenPoint(site);
                if (screen.z <= 0) return false;
                point = screen;
            }
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(
                    new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                    { position = point }, hits);
            var popup = UnityEngine.Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            if (Screen.safeArea.Contains(point) && hits.Count == 0 && (popup == null || !popup.IsOpen)) return true;
            // The selected-unit camera can leave the objective behind the army
            // HUD. Pan through the same public full-map controls used by players,
            // then retry the original touch; this issues no troop orders.
            manualCameraFocusSite = site;
            manualCameraFocusPending = true;
            manualCameraFocusStarted = false;
            airProbeDefenseMapStep = 0;
            TickManualCameraFocus(now);
            return false;
        }

        private static void TickManualCameraFocus(double now)
        {
            var popup = UnityEngine.Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            if (manualCameraFocusStarted && airProbeDefenseMapStep == 0 && airProbeDefenseMapAttempts > 0 &&
                (popup == null || !popup.IsOpen))
            {
                manualCameraFocusPending = false;
                return;
            }
            helipadProbeTouch = manualTouch;
            try
            {
                FocusPresentedGuardSite(now, manualCameraFocusSite);
                manualCameraFocusStarted |= airProbeDefenseMapStep != 0;
                if (manualTouch.IsBusy) manualActions++;
                manualDue = Math.Max(now + .1, helipadProbeNext);
            }
            finally { helipadProbeTouch = null; }
        }

        private static bool TickManualReplacement(EntityManager em, double now, MatchOverlayCommandControlsView commands)
        {
            if (!SkirmishLaunchProjection.TryGet(em, out var session, out _) ||
                !UiShellRuntimeGateway.TryReadSkirmish(out var model)) return false;
            var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            if (em.HasBuffer<SkirmishProductionReservation>(session))
                foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session))
                    if (receipt.FactionId == 1 && receipt.Role == SkirmishRoleKind.Rocketeer &&
                        receipt.MaterialsPaid == 120 && receipt.ReservationId > manualReplacementReceipt &&
                        receipt.Phase is SkirmishReservationPhase.Reserved or SkirmishReservationPhase.Producing or SkirmishReservationPhase.Live)
                    {
                        if (drawer != null && drawer.IsOpen) { ManualTap(drawer.CloseButton); return true; }
                        manualReplacementReceipt = receipt.ReservationId;
                        manualNeedsReplacement = false;
                        manualReplacementNext = now + 60;
                        Debug.Log("[S005ManualReplacement] phase=PaidQueue role=Rocketeer cost=120 input=Touch receipt=" + receipt.ReservationId);
                        manualSlot = 0; manualStep = 10;
                        return true;
                    }
            // This is a player strategy in the Editor test, using the displayed
            // bank and catalog. It never grants stocks or calls production APIs.
            if (model.OwnMaterials < 120) return false;
            var catalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
            Button button = drawer != null && drawer.IsOpen
                ? catalog?.ResolveCatalogTarget(BuildDrawerCategory.Soldiers, "Unit_Chr_Ghillie_Male_01") : commands?.BuildButton;
            if (!ManualTap(button)) ManualCatalogScroll(button);
            return true;
        }
        private static void ManualCatalogScroll(Button button)
        {
            if (button == null || !button.IsActive() || !button.IsInteractable()) return;
            var scroll = button.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.viewport == null) return;
            var viewport = scroll.viewport;
            var rect = (RectTransform)button.transform;
            Vector3 local = viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            var bounds = viewport.rect;
            if (bounds.Contains(local)) return;
            bool up = local.y < bounds.yMin;
            var canvas = button.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 from = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(new Vector3(
                bounds.xMin + bounds.width * .25f, bounds.yMin + bounds.height * (up ? .25f : .75f), 0)));
            Vector2 to = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(new Vector3(
                bounds.xMin + bounds.width * .25f, bounds.yMin + bounds.height * (up ? .75f : .25f), 0)));
            if (!Screen.safeArea.Contains(from) || !Screen.safeArea.Contains(to)) return;
            if (manualTouch.TryGesture(from, to, .5f, .9f, Time.unscaledTime))
            { manualActions++; manualDue = EditorApplication.timeSinceStartup + 1.5; }
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
