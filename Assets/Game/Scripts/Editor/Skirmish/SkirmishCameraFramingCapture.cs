#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Passive camera-framing capture for the owner request "camera too close" (2026-11-17).
    /// Rides on the existing normal-input launchers (ARIA watch for ground views, the S005
    /// aircraft cycle probe for helicopter flight); issues no unit orders and no selection.
    /// Ground zoom levels use the same SelectionUiCamera entry points the HUD zoom buttons call.
    /// Editor-only and independent of the framing code, so the same file can be cherry-picked
    /// onto missions/integration for the matched "before" captures.
    ///
    /// Output: Design/AgentReports/SkirmishExpansion/CameraFraming/{label}/
    ///   {catalog}-{seed}-{w}x{h}-ground-{opening|wide|close|default}.png, ...-ground.jsonl
    ///   {catalog}-{w}x{h}-air-{nn}-{event}.png, ...-air.jsonl
    /// {seed} is the seed of the launched session; {w}x{h} is the Game view's fixed target
    /// size (WARLINE_REVIEW_WIDTH/HEIGHT), which is what ScreenCapture writes. Screen.width/height
    /// are not used: inside EditorApplication.update they report the current Editor GUI view
    /// (1836x1016 on the lane machine), not the Game view.
    /// </summary>
    [InitializeOnLoad]
    public static class SkirmishCameraFramingCapture
    {
        const string ModeEnv = "WARLINE_CAMERA_FRAMING_CAPTURE", LabelEnv = "WARLINE_CAMERA_FRAMING_LABEL", ExitEnv = "WARLINE_CAMERA_FRAMING_EXIT";
        const int SettleFrames = 45, MaxAirCaptures = 14;
        public const int DefaultSeed = 104729, S002Seed = 130365;
        const string ReviewWidthEnv = "WARLINE_REVIEW_WIDTH", ReviewHeightEnv = "WARLINE_REVIEW_HEIGHT";

        static string mode, label;
        static int step, stableFrames, airCaptures, lastAirCaptureFrame = -100000, pendingClearedFrame = -1;
        static Vector3 lastPosition; static Quaternion lastRotation; static float lastFov;
        static double groundDeadline;
        static bool reviewSizeReapplied;
        static readonly Dictionary<int, AirTrack> tracks = new();
        sealed class AirTrack { public float groundedY = float.NaN, lastY, lastHeading; public double headingAt; public bool tookOff, cruised, landed; public double airborneSince; }

        static SkirmishCameraFramingCapture()
        {
            mode = Environment.GetEnvironmentVariable(ModeEnv);
            if (string.IsNullOrEmpty(mode)) return;
            label = Environment.GetEnvironmentVariable(LabelEnv);
            if (string.IsNullOrEmpty(label)) label = "unlabelled";
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            Game.Runtime.AircraftProductionFocusEvidence.Recorded -= OnAircraftFocus;
            Game.Runtime.AircraftProductionFocusEvidence.Recorded += OnAircraftFocus;
        }

        // ------------------------------------------------------------ entry points
        public static void RunS026GroundFramingCapture() => LaunchGround("S026");
        public static void RunS002GroundFramingCapture() => LaunchGround("S002");
        /// <summary>Helicopter takeoff/turn/cruise/landing and return to ground through the existing S005 normal-touch aircraft cycle.</summary>
        public static void RunS005HelicopterFramingCapture()
        {
            Arm("aircraft");
            SkirmishS002AriaRunHarness.RunS005AircraftCycleProbeAndExit();
        }

        static void LaunchGround(string catalog)
        {
            Arm("ground");
            SkirmishS002AriaRunHarness.LaunchCatalog(catalog, catalog == "S002" ? S002Seed : DefaultSeed, GameLocalization.EnglishLocaleCode);
        }

        static void Arm(string requested)
        {
            Environment.SetEnvironmentVariable(ModeEnv, requested);
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(LabelEnv))) Environment.SetEnvironmentVariable(LabelEnv, "unlabelled");
            mode = requested; label = Environment.GetEnvironmentVariable(LabelEnv);
            step = 0; stableFrames = 0; airCaptures = 0; tracks.Clear(); reviewSizeReapplied = false;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            Game.Runtime.AircraftProductionFocusEvidence.Recorded -= OnAircraftFocus;
            Game.Runtime.AircraftProductionFocusEvidence.Recorded += OnAircraftFocus;
            Directory.CreateDirectory(OutputDirectory());
            Debug.Log("[SkirmishCameraFramingCapture] armed mode=" + requested + " label=" + label + " output=" + OutputDirectory());
        }

        static string OutputDirectory() => Path.GetFullPath(Path.Combine(Application.dataPath, "..",
            "Design/AgentReports/SkirmishExpansion/CameraFraming", label ?? "unlabelled"));

        // ------------------------------------------------------------ review size
        static bool TryRequestedSize(out int width, out int height)
        {
            height = 0;
            return int.TryParse(Environment.GetEnvironmentVariable(ReviewWidthEnv), NumberStyles.Integer, CultureInfo.InvariantCulture, out width) &&
                int.TryParse(Environment.GetEnvironmentVariable(ReviewHeightEnv), NumberStyles.Integer, CultureInfo.InvariantCulture, out height) &&
                width > 0 && height > 0;
        }

        /// <summary>Main Game view render target size (internal PlayModeView API, read by reflection).</summary>
        static bool TryGameViewTargetSize(out int width, out int height)
        {
            width = height = 0;
            Assembly editor = typeof(EditorWindow).Assembly;
            foreach (string typeName in new[] { "UnityEditor.PlayModeView", "UnityEditor.GameView" })
            {
                MethodInfo method = editor.GetType(typeName)?.GetMethod("GetMainPlayModeViewTargetSize",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (method == null || method.ReturnType != typeof(Vector2)) continue;
                var size = (Vector2)method.Invoke(null, null);
                width = Mathf.RoundToInt(size.x); height = Mathf.RoundToInt(size.y);
                return width > 0 && height > 0;
            }
            return false;
        }

        static void ReviewSize(out int width, out int height, out string source)
        {
            if (TryGameViewTargetSize(out width, out height)) { source = "GameViewTarget"; return; }
            if (TryRequestedSize(out width, out height)) { source = "Requested"; return; }
            width = Screen.width; height = Screen.height; source = "Screen";
        }

        /// <summary>
        /// Captures only when the Game view renders at the requested fixed size. One re-apply of the
        /// harness's fixed-resolution entry is allowed (play-mode entry can reselect the window size);
        /// a second mismatch fails closed instead of writing mislabelled evidence.
        /// </summary>
        static bool ReviewSizeReady()
        {
            if (!TryRequestedSize(out int requestedWidth, out int requestedHeight) ||
                !TryGameViewTargetSize(out int width, out int height) ||
                (width == requestedWidth && height == requestedHeight)) return true;
            if (!reviewSizeReapplied)
            {
                reviewSizeReapplied = true; stableFrames = 0;
                Debug.Log("[SkirmishCameraFramingCapture] reapplyGameViewSize requested=" + requestedWidth + "x" + requestedHeight + " actual=" + width + "x" + height);
                MainMenuV3PrefabBuilder.SetGameViewResolution(requestedWidth, requestedHeight);
                return false;
            }
            Debug.LogError("[SkirmishCameraFramingCapture] result=Failed reason=GameViewSizeMismatch requested=" + requestedWidth + "x" + requestedHeight +
                " actual=" + width + "x" + height + " label=" + label);
            mode = null;
            if (Environment.GetEnvironmentVariable(ExitEnv) == "1") SkirmishS002AriaRunHarness.AbortFailedValidationAndExit();
            return false;
        }

        // ------------------------------------------------------------------- tick
        static void Tick()
        {
            if (!EditorApplication.isPlaying || string.IsNullOrEmpty(mode)) return;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            var em = world.EntityManager;
            if (!TryReadSession(em, out var session, out var native)) return;
            var camera = Camera.main;
            if (camera == null) return;
            try
            {
                if (mode == "ground") TickGround(em, session, native, camera);
                else if (mode == "aircraft") TickAircraft(em, native, camera);
            }
            catch (Exception error)
            {
                Debug.LogError("[SkirmishCameraFramingCapture] result=Failed step=" + step + " error=" + error.Message);
                mode = null;
            }
        }

        static bool TryReadSession(EntityManager em, out Entity session, out SkirmishExpandedSessionComponent native)
        {
            session = Entity.Null; native = default;
            using var query = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if (query.CalculateEntityCount() != 1) return false;
            session = query.GetSingletonEntity(); native = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            return native.Phase == SkirmishSessionPhase.Playing && native.SpawnComplete == 1;
        }

        static bool Settled(Camera camera)
        {
            var position = camera.transform.position; var rotation = camera.transform.rotation;
            bool still = (position - lastPosition).sqrMagnitude < .0001f && Quaternion.Angle(rotation, lastRotation) < .02f && Mathf.Abs(camera.fieldOfView - lastFov) < .01f;
            lastPosition = position; lastRotation = rotation; lastFov = camera.fieldOfView;
            stableFrames = still ? stableFrames + 1 : 0;
            return stableFrames >= SettleFrames;
        }

        static SelectionUiCameraSystemHelper HudCamera()
        {
            var scene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            return scene != null ? scene.MatchBootstrap?.SelectionUiCamera : null;
        }

        // ----------------------------------------------------------------- ground
        static readonly string[] GroundShots = { "opening", "wide", "close", "default" };
        static void TickGround(EntityManager em, Entity session, SkirmishExpandedSessionComponent native, Camera camera)
        {
            if (step >= GroundShots.Length) return;
            if (step == 0)
            {
                bool owned = SkirmishS005OpeningViewPolicy.OwnsIntroduction(em, session) ||
                    SkirmishMissionLogicalBindingProvider.OwnsOpeningIntroduction(em, session);
                if (groundDeadline <= 0) groundDeadline = EditorApplication.timeSinceStartup + 60;
                if (!owned && EditorApplication.timeSinceStartup < groundDeadline) { stableFrames = 0; return; }
            }
            if (!ReviewSizeReady() || !Settled(camera)) return;
            string catalog = native.CatalogId.ToString();
            ReviewSize(out int width, out int height, out _);
            string prefix = catalog + "-" + native.Seed.ToString(CultureInfo.InvariantCulture) + "-" + width + "x" + height + "-ground";
            string png = Path.Combine(OutputDirectory(), prefix + "-" + GroundShots[step] + ".png");
            ScreenCapture.CaptureScreenshot(png);
            AppendJson(prefix + ".jsonl", GroundShots[step], camera, em, native, png, null);
            var hud = HudCamera();
            bool requested = hud != null && step switch
            {
                0 => hud.RequestZoomOutLevel(),                             // HUD zoom-out: wide tactical view
                1 => hud.RequestZoomInLevel() && hud.RequestZoomInLevel(),  // HUD zoom-in twice: close/select view
                2 => hud.RequestZoomOutLevel(),                             // back to the default view
                _ => true
            };
            Debug.Log("[SkirmishCameraFramingCapture] shot=" + GroundShots[step] + " catalog=" + catalog + " png=" + png +
                " nextZoomRequested=" + (requested ? 1 : 0) + " unitOrders=0");
            step++; stableFrames = 0;
            if (step >= GroundShots.Length)
            {
                Debug.Log("[SkirmishCameraFramingCapture] result=Captured mode=ground catalog=" + catalog + " label=" + label + " visualReview=Pending");
                if (Environment.GetEnvironmentVariable(ExitEnv) == "1") SkirmishS002AriaRunHarness.AbortFailedValidationAndExit();
            }
        }

        // --------------------------------------------------------------- aircraft
        static void OnAircraftFocus(Game.Runtime.AircraftProductionFocusFact fact)
        {
            if (mode != "aircraft" || fact == null) return;
            if (fact.stage == "Applied") CaptureAir("lens-applied", Camera.main, null);
            if (fact.stage == "Cleared") pendingClearedFrame = Time.frameCount + 30; // the restored ground view
        }

        static void TickAircraft(EntityManager em, SkirmishExpandedSessionComponent native, Camera camera)
        {
            if (pendingClearedFrame >= 0 && Time.frameCount >= pendingClearedFrame) { pendingClearedFrame = -1; CaptureAir("switch-back-ground", camera, null); }
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent), typeof(LocalTransform), typeof(UnitHealth));
            using var actors = query.ToEntityArray(Allocator.Temp);
            double now = EditorApplication.timeSinceStartup;
            foreach (var actor in actors)
            {
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                var role = em.GetComponentData<SkirmishUnitRoleComponent>(actor);
                if (owner.FactionId != 1 || owner.IsStructure != 0 || role.Category != SkirmishPopulationCategory.Air ||
                    em.GetComponentData<UnitHealth>(actor).Current <= 0) continue;
                var transform = em.GetComponentData<LocalTransform>(actor);
                var forward = math.mul(transform.Rotation, new float3(0, 0, 1));
                float heading = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, y = transform.Position.y;
                if (!tracks.TryGetValue(actor.Index, out var track)) tracks[actor.Index] = track = new AirTrack { groundedY = y, lastY = y, lastHeading = heading, headingAt = now };
                var viewport = camera.WorldToViewportPoint(transform.Position);
                bool visible = viewport.z > 0 && viewport.x > -.1f && viewport.x < 1.1f && viewport.y > -.1f && viewport.y < 1.1f;
                string subject = role.Role + "#" + actor.Index;
                if (!track.tookOff && y > track.groundedY + 4f) { track.tookOff = true; track.airborneSince = now; CaptureAir("takeoff", camera, subject + " visible=" + visible); }
                if (track.tookOff && !track.landed && Mathf.Abs(Mathf.DeltaAngle(heading, track.lastHeading)) > 35f && now - track.headingAt < 3)
                { CaptureAir("turn", camera, subject + " visible=" + visible); track.lastHeading = heading; track.headingAt = now; }
                if (now - track.headingAt >= 3) { track.lastHeading = heading; track.headingAt = now; }
                if (track.tookOff && !track.cruised && now - track.airborneSince > 4 && Mathf.Abs(y - track.lastY) < .05f)
                { track.cruised = true; CaptureAir("cruise", camera, subject + " visible=" + visible); }
                if (track.tookOff && !track.landed && y < track.groundedY + 1.5f && now - track.airborneSince > 4)
                { track.landed = true; CaptureAir("landing", camera, subject + " visible=" + visible); }
                track.lastY = y;
            }
        }

        static void CaptureAir(string kind, Camera camera, string subject)
        {
            if (camera == null || airCaptures >= MaxAirCaptures || Time.frameCount - lastAirCaptureFrame < 20) return;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated || !TryReadSession(world.EntityManager, out _, out var native) || !ReviewSizeReady()) return;
            lastAirCaptureFrame = Time.frameCount;
            ReviewSize(out int width, out int height, out _);
            string prefix = native.CatalogId + "-" + width + "x" + height + "-air";
            string png = Path.Combine(OutputDirectory(), prefix + "-" + airCaptures.ToString("00", CultureInfo.InvariantCulture) + "-" + kind + ".png");
            airCaptures++;
            ScreenCapture.CaptureScreenshot(png);
            AppendJson(prefix + ".jsonl", kind, camera, world.EntityManager, native, png, subject);
        }

        // ------------------------------------------------------------------- facts
        [Serializable] sealed class Shot
        {
            public string label, shot, catalog, png, subject, sizeSource; public int frame, seed, width, height, editorScreenWidth, editorScreenHeight;
            public float elapsed, fieldOfView, aspect, cameraAboveSubjectMin; public Vector3 cameraPosition, cameraEuler, groundCenterZeroPlane;
            public bool profileResolved; public float profileNormalHeight, profileMaxHeight, profileMinHeight, profileGroundY;
            public int ownAirVisible, ownAirAboveCamera, unitOrdersIssued;
        }

        static void AppendJson(string file, string shot, Camera camera, EntityManager em, SkirmishExpandedSessionComponent native, string png, string subject)
        {
            ReviewSize(out int width, out int height, out string sizeSource);
            var row = new Shot { label = label, shot = shot, catalog = native.CatalogId.ToString(), png = png, subject = subject, frame = Time.frameCount,
                seed = native.Seed, width = width, height = height, sizeSource = sizeSource, editorScreenWidth = Screen.width, editorScreenHeight = Screen.height, elapsed = Time.timeSinceLevelLoad, fieldOfView = camera.fieldOfView, aspect = camera.aspect,
                cameraPosition = camera.transform.position, cameraEuler = camera.transform.eulerAngles, cameraAboveSubjectMin = float.NaN, unitOrdersIssued = 0 };
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance)) row.groundCenterZeroPlane = ray.GetPoint(distance);
            if (SkirmishMissionViewProfileApplication.TryResolve(em, out var profile))
            {
                row.profileResolved = true; row.profileNormalHeight = profile.NormalHeight; row.profileMaxHeight = profile.MaxHeight;
                row.profileMinHeight = profile.MinHeight; row.profileGroundY = profile.GroundReferenceY;
            }
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent), typeof(LocalTransform));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).FactionId != 1 ||
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Category != SkirmishPopulationCategory.Air) continue;
                Vector3 position = em.GetComponentData<LocalTransform>(actor).Position;
                var viewport = camera.WorldToViewportPoint(position);
                if (viewport.z <= 0 || viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) continue;
                row.ownAirVisible++;
                float above = camera.transform.position.y - position.y;
                if (above < 0) row.ownAirAboveCamera++; // camera below a visible aircraft: underside risk
                row.cameraAboveSubjectMin = float.IsNaN(row.cameraAboveSubjectMin) ? above : Mathf.Min(row.cameraAboveSubjectMin, above);
            }
            File.AppendAllText(Path.Combine(OutputDirectory(), file), JsonUtility.ToJson(row) + "\n");
        }
    }
}
#endif
