using System;
using System.Collections.Generic;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static AriaTouchInputUiSystemHelper helipadProbeTouch;
        private static int helipadProbeStage, helipadProbeSite;
        private static bool helipadProbeTappedSite;
        private static Vector3 helipadProbeExpectedSite;
        private static double helipadProbeDeadline, helipadProbeNext;
        private static readonly HashSet<int> initialHelipadObjects = new();
        private static bool helipadProbeAircraftCycle;

        public static void RunS003HelipadProbeAndExit()
        {
            helipadProbeAircraftCycle = false;
            LaunchS003HelipadProbe();
        }

        public static void RunS003AircraftCycleProbeAndExit()
        {
            helipadProbeAircraftCycle = true;
            LaunchS003HelipadProbe();
        }

        private static void LaunchS003HelipadProbe()
        {
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG", "S003");
            Environment.SetEnvironmentVariable("WARLINE_S003_HELIPAD_PROBE", "1");
            helipadProbeStage = 0;
            RunS003StartupAndExit();
        }

        private static void StartNativeHelipadProbe(Entity session)
        {
            helipadProbeTouch = null;
            helipadProbeStage = 1;
            helipadProbeSite = 0;
            helipadProbeTappedSite = false;
            helipadProbeExpectedSite = Vector3.zero;
            airProbePurchaseAt = airProbeServiceAt = airProbeSortieAt = airProbeLastStatusAt = 0d;
            airProbePageTurns = 0;
            initialHelipadObjects.Clear();
            foreach (var item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (item.name.StartsWith("Helipad_", StringComparison.Ordinal))
                    initialHelipadObjects.Add(item.gameObject.GetEntityId().GetHashCode());
            new GameObject("NativeHelipadInputProbe").AddComponent<SkirmishHelipadInputProbeFrame>().Session = session;
            helipadProbeDeadline = UnityEditor.EditorApplication.timeSinceStartup +
                (helipadProbeAircraftCycle ? 900d : 180d);
            deadline = helipadProbeDeadline + 20d;
        }

        internal static void TickHelipadPlayerFrame(Entity session)
        {
            if (helipadProbeStage == 0) return;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated || !world.EntityManager.Exists(session)) return;
            double now = UnityEditor.EditorApplication.timeSinceStartup;
            next = now + .1d;
            if (helipadProbeTouch == null)
            {
                helipadProbeTouch = new AriaTouchInputUiSystemHelper();
                if (!helipadProbeTouch.Start()) { EndHelipadProbe(false, "touchStartRejected"); return; }
            }
            helipadProbeTouch.Tick(Time.unscaledTime);
            if (helipadProbeTouch.PlayerInterrupted || !helipadProbeTouch.IsRunning)
            { EndHelipadProbe(false, "inputInterrupted"); return; }
            if (now > helipadProbeDeadline) { EndHelipadProbe(false, "placementTimeout"); return; }
            if (!Mathf.Approximately(Time.timeScale, 1f))
            {
                if (UiShellRuntimeGateway.TryReadSkirmish(out var frozen) && frozen.Finished)
                    EndHelipadProbe(false, "matchEndedBeforeProbeCompleted:stage=" + helipadProbeStage);
                else EndHelipadProbe(false, "notNormalSpeed");
                return;
            }
            if (helipadProbeTouch.IsBusy || now < helipadProbeNext) return;

            var skirmish = UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
            var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            var catalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
            var placement = UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>(FindObjectsInactive.Include);
            var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if (skirmish == null) return;

            if (helipadProbeStage == 1)
            {
                if (TapHelipadButton(skirmish.PlayerFocusButton, now)) helipadProbeStage = 2;
                return;
            }
            if (helipadProbeStage == 2)
            {
                helipadProbeStage = 3;
                helipadProbeNext = now + .5d;
                return;
            }

            if (helipadProbeStage == 3)
            {
                if (!UiShellRuntimeGateway.TryReadSkirmishReadiness(out var readiness)) return;
                if (readiness.Stage >= 2) { helipadProbeStage = 4; return; }
                if (readiness.InProgress) return;
                if (!readiness.CanUpgrade) { EndHelipadProbe(false, "readinessUnavailable:" + readiness.Reason); return; }
                TapHelipadButton(drawer != null && drawer.IsOpen ? drawer.ReadinessButton : controls?.BuildButton, now);
                return;
            }
            if (helipadProbeStage == 4)
            {
                if (placement != null && placement.HasPendingPlacement)
                { helipadProbeStage = 5; return; }
                Button build = drawer != null && drawer.IsOpen
                    ? catalog?.ResolveCatalogTarget(BuildDrawerCategory.Buildings, "Building_Helipad")
                    : controls?.BuildButton;
                if (!TapHelipadButton(build, now) && build != null)
                    ScrollHelipadCatalogTo(build, now);
                return;
            }
            if (helipadProbeStage == 5)
            {
                if (placement == null || !placement.HasPendingPlacement)
                { EndHelipadProbe(false, "placementClosedBeforeSite"); return; }
                if (placement.ConfirmButton != null && placement.ConfirmButton.IsInteractable() && helipadProbeTappedSite)
                {
                    Debug.Log($"[SkirmishS003HelipadProbe] confirmedSite={helipadProbeSite - 1} status={placement.StatusText?.text}");
                    if (TapHelipadButton(placement.ConfirmButton, now)) helipadProbeStage = 6;
                    return;
                }
                if (helipadProbeTappedSite)
                {
                    if (UiShellRuntimeGateway.TryReadBuildPlacementConfirmationBar(out var feedback))
                        Debug.Log($"[SkirmishS003HelipadProbe] feedback site={helipadProbeSite - 1} status={feedback.Status} viewStatus={placement.StatusText?.text} canConfirm={feedback.CanConfirm} button={placement.ConfirmButton?.IsInteractable()}");
                    helipadProbeTappedSite = false;
                }
                if (helipadProbeSite >= 4) { EndHelipadProbe(false, "noLegalSite"); return; }
                Vector3 home = skirmish.PlayerBaseObjectivePosition;
                Vector3 forward = skirmish.EnemyBaseObjectivePosition - home;
                forward.y = 0;
                if (forward.sqrMagnitude < 1) { EndHelipadProbe(false, "missingBaseDirection"); return; }
                forward.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, forward);
                Vector3 site = home + forward * (helipadProbeSite / 2 == 0 ? 18f : 12f) -
                    side * (helipadProbeSite % 2 == 0 ? 34f : 42f);
                var point = Camera.main != null ? Camera.main.WorldToScreenPoint(site) : Vector3.zero;
                if (point.z <= 0 || !Screen.safeArea.Contains(point))
                {
                    Debug.Log($"[SkirmishS003HelipadProbe] siteOffscreen screen={point}");
                    helipadProbeSite++;
                    return;
                }
                if (!helipadProbeTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
                { EndHelipadProbe(false, "siteTouchRejected"); return; }
                Debug.Log("[SkirmishS003HelipadProbe] site=" + site + " screen=" + point);
                helipadProbeTappedSite = true;
                helipadProbeExpectedSite = site;
                helipadProbeSite++;
                helipadProbeNext = now + 1d;
                return;
            }
            if (helipadProbeStage == 6)
            {
                if (placement != null && placement.HasPendingPlacement) return;
                foreach (var item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!item.name.StartsWith("Helipad_", StringComparison.Ordinal) ||
                        initialHelipadObjects.Contains(item.gameObject.GetEntityId().GetHashCode())) continue;
                    var renderers = item.GetComponentsInChildren<Renderer>(true);
                    int usable = 0;
                    bool realModel = false;
                    Bounds bounds = default;
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                            renderer is not MeshRenderer || !renderer.TryGetComponent<MeshFilter>(out var filter) ||
                            filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0 || renderer.sharedMaterial == null) continue;
                        if (usable++ == 0) bounds = renderer.bounds;
                        else bounds.Encapsulate(renderer.bounds);
                        if (renderer.name == "Model") realModel = true;
                    }
                    Vector3 delta = item.position - helipadProbeExpectedSite;
                    delta.y = 0;
                    bool correctSite = delta.sqrMagnitude <= 2f * 2f;
                    Debug.Log($"[SkirmishS003HelipadProbe] committed={item.name} position={item.position} expected={helipadProbeExpectedSite} correctSite={correctSite} active={item.gameObject.activeInHierarchy} renderers={renderers.Length} usable={usable} realModel={realModel} bounds={bounds}");
                    ScreenCapture.CaptureScreenshot("/private/tmp/s003-helipad-placed.png");
                    if (!correctSite || !realModel)
                    { EndHelipadProbe(false, "committedVisualMissingOrMoved"); return; }
                    helipadProbeStage = 7;
                    helipadProbeNext = now + 1d;
                    return;
                }
            }
            if (helipadProbeStage == 7)
            {
                if (TapHelipadButton(controls?.SelectButton, now)) helipadProbeStage = 8;
                return;
            }
            if (helipadProbeStage == 8)
            {
                var emptyGround = new Vector2(Screen.width * .68f, Screen.height * .6f);
                if (!helipadProbeTouch.TryGesture(emptyGround, emptyGround, .15f, 0f, Time.unscaledTime))
                { EndHelipadProbe(false, "deselectTouchRejected"); return; }
                helipadProbeStage = 9;
                helipadProbeNext = now + 1d;
                return;
            }
            if (helipadProbeStage == 9)
            {
                if (!UiShellRuntimeGateway.TryReadMatchHudSelection(out var selected) || selected.Visible)
                { EndHelipadProbe(false, "deselectNotObserved"); return; }
                ScreenCapture.CaptureScreenshot("/private/tmp/s003-helipad-unselected.png");
                if (helipadProbeAircraftCycle) { helipadProbeStage = 10; helipadProbeNext = now + 1d; }
                else EndHelipadProbe(true, "committedModelAtTouchedSiteAndUnselected");
                return;
            }
            if (helipadProbeStage >= 10)
                TickAircraftCycleProbe(now, skirmish, drawer, catalog, controls);
        }

        private static bool TapHelipadButton(Button button, double now)
        {
            if (!TryPresentedButtonPoint(button, out var point)) return false;
            if (!helipadProbeTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
            { EndHelipadProbe(false, "buttonTouchRejected:" + button.name); return false; }
            Debug.Log("[SkirmishS003HelipadProbe] touch=" + button.name + " point=" + point);
            helipadProbeNext = now + .8d;
            return true;
        }

        private static bool ScrollHelipadCatalogTo(Button button, double now)
        {
            ScrollRect scroll = button.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.viewport == null) return false;
            RectTransform viewport = scroll.viewport;
            Rect bounds = viewport.rect;
            Vector3 local = viewport.InverseTransformPoint(((RectTransform)button.transform).TransformPoint(
                ((RectTransform)button.transform).rect.center));
            bool up = local.y < bounds.yMin;
            var canvas = button.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 from = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(new Vector3(
                bounds.xMin + bounds.width * .25f, bounds.yMin + bounds.height * (up ? .25f : .75f), 0)));
            Vector2 to = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(new Vector3(
                bounds.xMin + bounds.width * .25f, bounds.yMin + bounds.height * (up ? .75f : .25f), 0)));
            if (!Screen.safeArea.Contains(from) || !Screen.safeArea.Contains(to)) return false;
            if (!helipadProbeTouch.TryGesture(from, to, .5f, .9f, Time.unscaledTime))
            { EndHelipadProbe(false, "catalogScrollRejected"); return false; }
            Debug.Log("[SkirmishS003HelipadProbe] scroll=" + from + " to=" + to);
            helipadProbeNext = now + 1d;
            return true;
        }

        private static void EndHelipadProbe(bool passed, string reason)
        {
            Debug.Log("[SkirmishS003HelipadProbe] result=" + (passed ? "Passed" : "Failed") +
                " reason=" + reason + " input=Touch samples=" + (helipadProbeTouch?.AcceptedSamples ?? 0) +
                " acceptance=" + (helipadProbeAircraftCycle ? "AircraftLifecycleTouchProbe" : "PlacementVisualOnly"));
            helipadProbeStage = 0;
            Finish(!passed, reason);
        }
    }

    internal sealed class SkirmishHelipadInputProbeFrame : MonoBehaviour
    {
        internal Entity Session;
        private void Update() => SkirmishS002AriaRunHarness.TickHelipadPlayerFrame(Session);
    }
}
