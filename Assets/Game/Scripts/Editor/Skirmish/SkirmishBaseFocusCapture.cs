#if UNITY_EDITOR
using System;
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

namespace Game.Editor
{
    /// <summary>
    /// S002 alert-tap framing capture (designer retest 2026-11-18). Launches the same ARIA
    /// watch session as <see cref="SkirmishCameraFramingCapture.RunS002GroundFramingCapture"/>
    /// (seed 130365), waits for the base alert to report the attacker in sight, then presses the
    /// alert row's focus button (its own onClick, the handler a tap runs: camera only) and
    /// captures the settled frame once.
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
        const int SettleFrames = 30, MinimumFramesAfterTap = 20;
        const double AlertDeadlineSeconds = 420d;

        static bool armed, tapped, done;
        static string label, locale = GameLocalization.EnglishLocaleCode;
        static int tapFrame, stableFrames;
        static double deadline;
        static Vector3 lastPosition; static Quaternion lastRotation;

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
            armed = true; tapped = false; done = false; stableFrames = 0; deadline = 0d;
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
            var world = World.DefaultGameObjectInjectionWorld;
            var camera = Camera.main;
            if (world == null || !world.IsCreated || camera == null) return;
            var em = world.EntityManager;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishMatchState));
            if (sessions.CalculateEntityCount() != 1) return;
            var session = sessions.GetSingletonEntity();
            var native = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var match = em.GetComponentData<SkirmishMatchState>(session);
            if (native.Phase >= SkirmishSessionPhase.Finished) { Fail("matchEndedBeforeCapture:phase=" + native.Phase); return; }
            if (native.Phase != SkirmishSessionPhase.Playing || native.SpawnComplete != 1) return;
            if (deadline <= 0d) deadline = EditorApplication.timeSinceStartup + AlertDeadlineSeconds;
            if (EditorApplication.timeSinceStartup > deadline) { Fail(tapped ? "cameraNotSettled" : "noAttackerInSightBeforeDeadline"); return; }
            var view = UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
            if (view == null) return;
            if (!tapped)
            {
                var button = view.BaseUnderFireFocusButton;
                if (!view.BaseAlertAttackerShown || button == null || !button.isActiveAndEnabled || !button.interactable) return;
                button.onClick.Invoke(); // The alert row's own handler: Send(FocusPlayer). Camera only.
                tapped = true; tapFrame = Time.frameCount; stableFrames = 0;
                Debug.Log("[SkirmishBaseFocusCapture] tapped=AlertFocus frame=" + tapFrame + " attacker=" + view.BaseAlertAttackerWorld + " unitOrders=0");
                return;
            }
            bool still = (camera.transform.position - lastPosition).sqrMagnitude < .0001f && Quaternion.Angle(camera.transform.rotation, lastRotation) < .02f;
            lastPosition = camera.transform.position; lastRotation = camera.transform.rotation;
            stableFrames = still ? stableFrames + 1 : 0;
            if (Time.frameCount - tapFrame < MinimumFramesAfterTap || stableFrames < SettleFrames) return;

            int width = camera.pixelWidth, height = camera.pixelHeight;
            string prefix = SkirmishCameraFramingCapture.GroundFilePrefix("S002", native.Seed, width, height, locale);
            string png = Path.Combine(OutputDirectory(), prefix + "-" + ShotName + ".png");
            ScreenCapture.CaptureScreenshot(png);
            var row = new Row { label = label, shot = ShotName, catalog = native.CatalogId.ToString(), locale = locale, png = png, seed = native.Seed,
                width = width, height = height, frame = Time.frameCount, tapFrame = tapFrame, cameraPosition = camera.transform.position,
                cameraEuler = camera.transform.eulerAngles, fieldOfView = camera.fieldOfView, aspect = camera.aspect, unitOrdersIssued = 0 };
            row.attackerShown = view.BaseAlertAttackerShown;
            row.attackerWorld = view.BaseAlertAttackerWorld;
            if (SkirmishPublicObjectiveProjection.TryReadFocusPosition(em, match.PlayerMainBase, out float3 home))
            { row.homeKnown = true; row.homeWorld = home; row.homeViewport = camera.WorldToViewportPoint(home); }
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
            public string label, shot, catalog, locale, png; public int seed, width, height, frame, tapFrame, unitOrdersIssued;
            public Vector3 cameraPosition, cameraEuler, homeWorld, attackerWorld, homeViewport, attackerViewport; public float fieldOfView, aspect;
            public bool homeKnown, attackerShown, playAreaKnown, homeInPlayArea, attackerInPlayArea; public Vector4 playArea;
        }
    }
}
#endif
