#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
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
    /// <summary>
    /// Long-session runtime regression (WARLINE_LONG_SESSION=1, run_aria_queue.py --long-session). Gate from the program
    /// lead (2026-11-17): once per runtime candidate, S026 Regular/Standard seed 104729 through the real menu
    /// (--menu-journey), past reinforcement purchases to deadline expiry, with no allocator error, the result screen
    /// shown and MAIN MENU reached. ARIA is not started. The driver only uses presented touch controls: every
    /// <see cref="LongSessionPurchaseSeconds"/> seconds it opens Build, buys one infantry role from the presented catalog and
    /// closes the drawer. No troop order is issued; delivered units defend where they arrive. The existing terminal probe
    /// verifies the frozen result and the MAIN MENU return. Every Unity error/exception logged from the match start to the
    /// end of the run is counted; the run ends with one <c>[SkirmishLongSessionRegression]</c> line.
    /// </summary>
    public static partial class SkirmishS002AriaRunHarness
    {
        private const double LongSessionPurchaseSeconds = 30d;
        private const double LongSessionPurchaseTimeout = 25d;
        private const int LongSessionMinimumMaterials = 150;
        private static readonly SkirmishRoleKind[] LongSessionRoles =
            { SkirmishRoleKind.Rifle, SkirmishRoleKind.Rocketeer, SkirmishRoleKind.Gunner };

        private static AriaTouchInputUiSystemHelper longTouch;
        private static GameObject longRunner;
        private static double longDue, longNextPurchase, longPurchaseDeadline;
        private static int longStep, longRole;
        private static uint longReceiptBefore;
        private static bool longCapturing, longEmitted, longTerminalNoted, longStarted;
        private static SkirmishLongSessionRegressionLog.Evidence longEvidence;
        private static readonly object longLock = new();

        internal static bool LongSessionRequested =>
            Environment.GetEnvironmentVariable(SkirmishLongSessionRegressionLog.EnvironmentVariable) == "1";

        private static void StartLongSessionDriver()
        {
            StopLongSessionDriver();
            longEvidence = new SkirmishLongSessionRegressionLog.Evidence { Catalog = catalogId, Seed = seed };
            longEmitted = longTerminalNoted = false;
            longStarted = true;
            longStep = longRole = 0;
            longReceiptBefore = 0;
            double now = EditorApplication.timeSinceStartup;
            longDue = now + 1d;
            longNextPurchase = now + 5d;
            BeginLongSessionCapture();
            longTouch = new AriaTouchInputUiSystemHelper();
            if (!longTouch.Start()) throw new InvalidOperationException("Long-session touch driver unavailable.");
            longRunner = new GameObject("SkirmishLongSessionControls");
            UnityEngine.Object.DontDestroyOnLoad(longRunner);
            longRunner.AddComponent<SkirmishLongSessionInputFrame>();
            EcsQueryBudgetSystem.Log(World.DefaultGameObjectInjectionWorld, 0, false);
            Debug.Log("[SkirmishLongSession] started=1 catalog=" + catalogId + " seed=" + seed +
                " input=Touch ariaStarted=0 strategy=periodicInfantryPurchaseNoOrders interval=" + LongSessionPurchaseSeconds);
        }

        private static void StopLongSessionDriver()
        {
            if (longRunner != null) { UnityEngine.Object.DestroyImmediate(longRunner); longRunner = null; }
            longTouch?.Dispose();
            longTouch = null;
        }

        private static void BeginLongSessionCapture()
        {
            if (longCapturing) return;
            Application.logMessageReceivedThreaded += CaptureLongSessionLog;
            longCapturing = true;
        }

        private static void EndLongSessionCapture()
        {
            if (!longCapturing) return;
            Application.logMessageReceivedThreaded -= CaptureLongSessionLog;
            longCapturing = false;
        }

        private static void CaptureLongSessionLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            lock (longLock)
            {
                if (SkirmishLongSessionRegressionLog.IsAllocatorError(condition)) longEvidence.AllocatorErrors++;
                if (type == LogType.Exception) longEvidence.Exceptions++; else longEvidence.Errors++;
                if (longEvidence.FirstError == null) longEvidence.FirstError = type + ":" + condition;
            }
        }

        // Called by StartTerminalProbe while the frozen match state is still readable.
        private static void NoteLongSessionTerminal(EntityManager em, Entity session, in SkirmishMatchState match, float elapsed)
        {
            if (!LongSessionRequested || !longStarted) return;
            StopLongSessionDriver();
            longTerminalNoted = true;
            longEvidence.Elapsed = elapsed;
            longEvidence.Outcome = match.Outcome.ToString();
            string reason = em.HasComponent<SkirmishObjectiveStateComponent>(session)
                ? em.GetComponentData<SkirmishObjectiveStateComponent>(session).Reason.ToString() : "unknown";
            longEvidence.EndReason = reason;
            longEvidence.Expiry = SkirmishLongSessionRegressionLog.IsExpiry(reason);
            var snapshot = EcsQueryBudgetSystem.Log(World.DefaultGameObjectInjectionWorld, elapsed, true);
            longEvidence.Archetypes = snapshot.Archetypes;
            longEvidence.QueryShapes = snapshot.QueryShapes;
            longEvidence.QueryBlocks = snapshot.UsedBlocks;
            longEvidence.QueryBudgetBlocks = snapshot.BudgetBlocks;
        }

        // Called by EndTerminalProbe before Finish.
        private static void NoteLongSessionReturn(bool resultAccepted, bool returned)
        {
            if (!LongSessionRequested || !longStarted) return;
            longEvidence.ResultScreen = resultAccepted;
            longEvidence.ReturnToMenu = returned;
        }

        // Called once from Finish; also covers runs that never reached the terminal probe.
        private static void EmitLongSessionRegression(bool abort, string abortReason)
        {
            if (!LongSessionRequested || longEmitted) return;
            longEmitted = true;
            StopLongSessionDriver();
            EndLongSessionCapture();
            SkirmishLongSessionRegressionLog.Evidence evidence;
            lock (longLock) evidence = longEvidence;
            if (!longStarted) evidence = new SkirmishLongSessionRegressionLog.Evidence { Catalog = catalogId, Seed = seed };
            evidence.Abort = abort ? abortReason ?? "abort" : null;
            if (!longTerminalNoted)
            {
                var snapshot = EcsQueryBudgetSystem.Log(World.DefaultGameObjectInjectionWorld, evidence.Elapsed, true);
                evidence.Archetypes = snapshot.Archetypes; evidence.QueryShapes = snapshot.QueryShapes;
                evidence.QueryBlocks = snapshot.UsedBlocks; evidence.QueryBudgetBlocks = snapshot.BudgetBlocks;
            }
            string line = SkirmishLongSessionRegressionLog.Format(evidence);
            if (SkirmishLongSessionRegressionLog.Verdict(evidence) == "Passed") Debug.Log(line); else Debug.LogWarning(line);
            longStarted = false;
        }

        internal static void TickLongSessionPlayerFrame()
        {
            if (longTouch == null || terminalProbeActive || finishingEvidence) return;
            try
            {
                longTouch.Tick(Time.unscaledTime);
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                var em = world.EntityManager;
                if (!SkirmishLaunchProjection.TryGet(em, out var session, out var match) || match.Phase != SkirmishPhase.Playing) return;
                if (UiShellRuntimeGateway.ReadAriaPlay().Active)
                    throw new InvalidOperationException("ARIA active during the long-session regression.");
                if (longTouch.PlayerInterrupted || !longTouch.IsRunning || longTouch.UnexpectedSamples > 0)
                    throw new InvalidOperationException("Long-session touch input interrupted.");
                double now = EditorApplication.timeSinceStartup;
                if (longTouch.IsBusy || now < longDue) return;
                var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
                bool open = drawer != null && drawer.IsOpen;
                if (longStep == 0)
                {
                    if (open) { LongSessionTap(drawer.CloseButton, now); return; }
                    if (now < longNextPurchase) return;
                    if (SkirmishMaterialsService.Read(em, session, 1) < LongSessionMinimumMaterials)
                    { longNextPurchase = now + 10d; return; }
                    longReceiptBefore = LatestPlayerReceipt(em, session);
                    longPurchaseDeadline = now + LongSessionPurchaseTimeout;
                    longStep = 1;
                    return;
                }
                if (longStep == 1)
                {
                    var role = LongSessionRoles[longRole % LongSessionRoles.Length];
                    uint latest = LatestPlayerReceipt(em, session);
                    if (latest > longReceiptBefore)
                    {
                        longEvidence.Purchases++;
                        Debug.Log("[SkirmishLongSession] purchase=" + longEvidence.Purchases + " role=" + role + " receipt=" + latest +
                            " input=Touch t=" + SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match).ToString("F0"));
                        longStep = 2;
                        return;
                    }
                    if (now > longPurchaseDeadline)
                    {
                        longEvidence.PurchaseFailures++;
                        Debug.Log("[SkirmishLongSession] purchaseSkipped role=" + role + " reason=noReceiptWithin" + LongSessionPurchaseTimeout + "s");
                        longStep = 2;
                        return;
                    }
                    var commands = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>(FindObjectsInactive.Include);
                    var catalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
                    Button button = open ? catalog?.ResolveCatalogTarget(BuildDrawerCategory.Soldiers, LongSessionPrefabKey(em, session, role))
                        : commands?.BuildButton;
                    if (button == null)
                    {
                        // This role is not in the presented catalog: try the next one in the same drawer visit.
                        if (open) longRole++;
                        longDue = now + 1d;
                        return;
                    }
                    if (!LongSessionTap(button, now)) LongSessionCatalogScroll(button, now);
                    return;
                }
                if (open) { LongSessionTap(drawer.CloseButton, now); return; }
                longStep = 0;
                longRole++;
                longNextPurchase = now + LongSessionPurchaseSeconds;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Finish(true, "longSession:" + error.Message);
            }
        }

        private static uint LatestPlayerReceipt(EntityManager em, Entity session)
        {
            uint latest = 0;
            if (!em.HasBuffer<SkirmishProductionReservation>(session)) return 0;
            foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session))
                if (receipt.FactionId == 1 && receipt.ReservationId > latest &&
                    receipt.Phase is SkirmishReservationPhase.Reserved or SkirmishReservationPhase.Producing or SkirmishReservationPhase.Live)
                    latest = receipt.ReservationId;
            return latest;
        }

        // Same resolution as SkirmishProductionService.PrefabKey: the setup's force entry, else the role catalog default.
        private static string LongSessionPrefabKey(EntityManager em, Entity session, SkirmishRoleKind role)
        {
            var setup = em.HasComponent<SkirmishResolvedSetupRecord>(session)
                ? em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup : null;
            if (setup?.Forces != null)
                foreach (var force in setup.Forces)
                    if (force.RoleKind == role && !string.IsNullOrEmpty(force.RuntimePrefabKey)) return force.RuntimePrefabKey;
            return SkirmishRoleCatalogConfig.RuntimePrefabKey(role);
        }

        private static bool LongSessionTap(Button button, double now)
        {
            if (!TryPresentedButtonPoint(button, out var point)) return false;
            if (!longTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime)) return false;
            longDue = now + .8d;
            return true;
        }

        private static void LongSessionCatalogScroll(Button button, double now)
        {
            longDue = now + .5d;
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
            if (longTouch.TryGesture(from, to, .5f, .9f, Time.unscaledTime)) longDue = now + 1.5d;
        }
    }

    internal sealed class SkirmishLongSessionInputFrame : MonoBehaviour
    {
        private void Update() => SkirmishS002AriaRunHarness.TickLongSessionPlayerFrame();
    }
}
#endif
