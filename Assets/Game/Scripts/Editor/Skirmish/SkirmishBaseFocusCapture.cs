#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// S002 alert-tap framing capture (designer retest 2026-11-18). Launches the same ARIA
    /// watch session as <see cref="SkirmishCameraFramingCapture.RunS002GroundFramingCapture"/>
    /// (seed 130365), waits for the base alert to report the attacker in sight, then presses the
    /// alert row's focus button (its own onClick, the handler a tap runs: camera only) and
    /// captures the first rendered impact cue after that tap.
    ///
    /// Output: Design/AgentReports/SkirmishExpansion/CameraFraming/{label}/
    ///   S002-130365-{w}x{h}[-{locale}]-ground-alert-focus.png and ...-ground-alert-focus.jsonl
    /// The row records the home base, the attacker the HUD showed, both as viewport points, and
    /// the live HUD play area, so "both inside the play area" is checkable from the file.
    /// Env: WARLINE_CAMERA_FRAMING_LABEL, WARLINE_CAPTURE_LOCALE, WARLINE_CAMERA_FRAMING_EXIT=1.
    /// </summary>
    [InitializeOnLoad]
    public static class SkirmishBaseFocusCapture
    {
        const string ModeEnv = "WARLINE_BASE_FOCUS_CAPTURE", LabelEnv = "WARLINE_CAMERA_FRAMING_LABEL", ExitEnv = "WARLINE_CAMERA_FRAMING_EXIT";
        public const string ShotName = "alert-focus";
        const double AlertDeadlineSeconds = 420d;
        const double ImpactDeadlineSeconds = 90d;

        static bool armed, tapped, done;
        static string label, locale = GameLocalization.EnglishLocaleCode;
        static int tapFrame, firstHitEvent;
        static double deadline, tappedAt;

        static SkirmishBaseFocusCapture()
        {
            if (Environment.GetEnvironmentVariable(ModeEnv) != "alert") return;
            label = Environment.GetEnvironmentVariable(LabelEnv);
            if (string.IsNullOrEmpty(label)) label = "unlabelled";
            if (!SkirmishCameraFramingCapture.TryResolveCaptureLocale(Environment.GetEnvironmentVariable(SkirmishCameraFramingCapture.LocaleEnv), out locale, out _))
                locale = GameLocalization.EnglishLocaleCode;
            armed = true;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }

        /// <summary>S002, seed 130365: the alert-tap frame while the attacker is in sight.</summary>
        public static void RunS002AlertFocusCapture()
        {
            if (!SkirmishCameraFramingCapture.TryResolveCaptureLocale(Environment.GetEnvironmentVariable(SkirmishCameraFramingCapture.LocaleEnv), out locale, out string error))
                throw new InvalidOperationException(error);
            Environment.SetEnvironmentVariable(ModeEnv, "alert");
            label = Environment.GetEnvironmentVariable(LabelEnv);
            if (string.IsNullOrEmpty(label)) { label = "unlabelled"; Environment.SetEnvironmentVariable(LabelEnv, label); }
            armed = true; tapped = false; done = false; firstHitEvent = 0; deadline = 0d;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            Directory.CreateDirectory(OutputDirectory());
            Debug.Log("[SkirmishBaseFocusCapture] armed label=" + label + " locale=" + locale + " output=" + OutputDirectory());
            SkirmishS002AriaRunHarness.LaunchCatalog("S002", SkirmishCameraFramingCapture.S002Seed, locale);
        }

        static string OutputDirectory() => Path.GetFullPath(Path.Combine(Application.dataPath, "..",
            "Design/AgentReports/SkirmishExpansion/CameraFraming", label ?? "unlabelled"));

        static void Tick()
        {
            if (!armed || done || !EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            if (tapped && now - tappedAt >= ImpactDeadlineSeconds)
            { Fail($"firstImpactCueTimeout tapFrame={tapFrame} hitEvent={firstHitEvent} waitedSeconds={now - tappedAt:F2}"); return; }
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            var em = world.EntityManager;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishMatchState));
            if (sessions.CalculateEntityCount() != 1) return;
            var session = sessions.GetSingletonEntity();
            var native = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var match = em.GetComponentData<SkirmishMatchState>(session);
            if (native.Phase >= SkirmishSessionPhase.Finished) { Fail("matchEndedBeforeCapture:phase=" + native.Phase); return; }
            if (native.Phase != SkirmishSessionPhase.Playing || native.SpawnComplete != 1) return;
            if (tapped) return;
            if (deadline <= 0d) deadline = now + AlertDeadlineSeconds;
            if (now > deadline) { Fail("noAttackerInSightBeforeDeadline"); return; }
            var view = UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
            if (view == null) return;
            var button = view.BaseUnderFireFocusButton;
            if (!view.BaseAlertAttackerShown || button == null || !button.isActiveAndEnabled || !button.interactable ||
                !UiShellRuntimeGateway.TryReadSkirmish(out var model)) return;
            if (model.BaseHitEventId != 0) { Fail("firstHitPrecededAlertTap hitEvent=" + model.BaseHitEventId); return; }
            button.onClick.Invoke(); // The alert row's own handler: Send(FocusPlayer). Camera only.
            tapped = true; tapFrame = Time.frameCount; tappedAt = now;
            Debug.Log($"[SkirmishBaseFocusCapture] tapped=AlertFocus frame={tapFrame} attacker={view.BaseAlertAttackerWorld} " +
                $"baseHealth={model.PlayerHealth} waitingFor=FirstImpactCue timeoutSeconds={ImpactDeadlineSeconds} unitOrders=0");
            view.StartCoroutine(CaptureFirstImpact(em, match.PlayerMainBase, native, view));
        }

        static IEnumerator CaptureFirstImpact(EntityManager em, Entity home, SkirmishExpandedSessionComponent native, SkirmishMatchView view)
        {
            var endOfFrame = new WaitForEndOfFrame();
            while (!done)
            {
                yield return endOfFrame;
                if (done) break;
                try
                {
                    if (!UiShellRuntimeGateway.TryReadSkirmish(out var model) || model.BaseHitEventId <= 0) continue;
                    if (firstHitEvent == 0)
                    {
                        firstHitEvent = model.BaseHitEventId;
                        Debug.Log($"[SkirmishBaseFocusCapture] firstHitEvent={firstHitEvent} frame={Time.frameCount} baseHealth={model.PlayerHealth}");
                    }
                    if (model.BaseHitEventId != firstHitEvent)
                        throw new InvalidOperationException($"firstImpactCueMissed firstHitEvent={firstHitEvent} latestHitEvent={model.BaseHitEventId}");
                    var camera = Camera.main;
                    // Look for the HUD objects on the rendered frame, since the hit fact outlives its animation.
                    if (camera == null || !CueVisible(view, "BaseHitImpact")) continue;
                    Capture(em, home, native, view, camera, model.PlayerHealth, CueVisible(view, "BaseHitShot"));
                }
                catch (Exception error) { Fail(error.Message); }
            }
        }

        static bool CueVisible(SkirmishMatchView view, string name)
        {
            var cue = view.transform.Find(name);
            return cue != null && cue.TryGetComponent<Graphic>(out var graphic) && graphic.isActiveAndEnabled &&
                !graphic.canvasRenderer.cull && graphic.color.a > 0f;
        }

        static void Capture(EntityManager em, Entity home, SkirmishExpandedSessionComponent native,
            SkirmishMatchView view, Camera camera, int baseHealth, bool shotLineVisible)
        {
            int width = camera.pixelWidth, height = camera.pixelHeight;
            string prefix = SkirmishCameraFramingCapture.GroundFilePrefix("S002", native.Seed, width, height, locale);
            string png = Path.Combine(OutputDirectory(), prefix + "-" + ShotName + ".png");
            SkirmishCameraFramingCapture.SaveRenderedFrame(png, width, height);
            var row = new Row { label = label, shot = ShotName, catalog = native.CatalogId.ToString(), attempt = native.SessionId.ToString(), locale = locale, png = png, seed = native.Seed,
                width = width, height = height, frame = Time.frameCount, tapFrame = tapFrame, cameraPosition = camera.transform.position,
                cameraEuler = camera.transform.eulerAngles, fieldOfView = camera.fieldOfView, aspect = camera.aspect, unitOrdersIssued = 0,
                hitEvent = firstHitEvent, baseHealth = baseHealth, shotLineVisible = shotLineVisible,
                waitedSeconds = EditorApplication.timeSinceStartup - tappedAt };
            row.attackerShown = view.BaseAlertAttackerShown;
            row.attackerWorld = view.BaseAlertAttackerWorld;
            if (SkirmishPublicObjectiveProjection.TryReadFocusPosition(em, home, out float3 homeWorld))
            { row.homeKnown = true; row.homeWorld = homeWorld; row.homeViewport = camera.WorldToViewportPoint(homeWorld); }
            if (row.attackerShown) row.attackerViewport = camera.WorldToViewportPoint(row.attackerWorld);
            if (view.TryReadSafePlayArea(out var pixels) && SkirmishCameraFraming.TryPixelsToViewport(pixels, camera.pixelRect, out var play))
            {
                row.playAreaKnown = true; row.playArea = new Vector4(play.xMin, play.yMin, play.xMax, play.yMax);
                row.homeInPlayArea = row.homeKnown && row.homeViewport.z > 0 && play.Contains(row.homeViewport);
                row.attackerInPlayArea = row.attackerShown && row.attackerViewport.z > 0 && play.Contains(row.attackerViewport);
            }
            File.AppendAllText(Path.Combine(OutputDirectory(), prefix + "-" + ShotName + ".jsonl"), JsonUtility.ToJson(row) + "\n");
            done = true;
            Debug.Log("[SkirmishBaseFocusCapture] result=Captured shot=" + ShotName + " png=" + png +
                $" attempt={row.attempt} frame={row.frame} hitEvent={row.hitEvent} baseHealth={row.baseHealth} " +
                $"waitedSeconds={row.waitedSeconds:F2} impactRingVisible=1 shotLineVisible={(shotLineVisible ? 1 : 0)}" +
                " homeInPlayArea=" + (row.homeInPlayArea ? 1 : 0) + " attackerInPlayArea=" + (row.attackerInPlayArea ? 1 : 0) +
                " playAreaKnown=" + (row.playAreaKnown ? 1 : 0) + " unitOrders=0 visualReview=Pending");
            if (Environment.GetEnvironmentVariable(ExitEnv) == "1") SkirmishS002AriaRunHarness.AbortFailedValidationAndExit();
        }

        static void Fail(string reason)
        {
            done = true;
            Debug.LogError("[SkirmishBaseFocusCapture] result=Failed reason=" + reason);
            if (Environment.GetEnvironmentVariable(ExitEnv) == "1") SkirmishS002AriaRunHarness.AbortFailedValidationAndExit();
        }

        [Serializable] sealed class Row
        {
            public string label, shot, catalog, locale, png, attempt; public int seed, width, height, frame, tapFrame, unitOrdersIssued, hitEvent, baseHealth;
            public double waitedSeconds; public bool shotLineVisible;
            public Vector3 cameraPosition, cameraEuler, homeWorld, attackerWorld, homeViewport, attackerViewport; public float fieldOfView, aspect;
            public bool homeKnown, attackerShown, playAreaKnown, homeInPlayArea, attackerInPlayArea; public Vector4 playArea;
        }
    }
}
#endif
