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
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    ///   {catalog}-{seed}-{w}x{h}[-{locale}]-ground-{opening|wide|close|default}.png, ...-ground.jsonl
    ///   {catalog}-{w}x{h}[-{locale}]-air-{nn}-{event}.png, ...-air.jsonl
    /// {locale} comes from WARLINE_CAPTURE_LOCALE (en or fa-IR, the locales the ARIA queue accepts;
    /// default en) and appears in the names only when it is not English, so English names are unchanged.
    /// Air events: lens-applied, return-from-recruitment, takeoff, turn, cruise, landing, switch-back-ground.
    /// {seed} is the seed of the launched session; {w}x{h} is the Game view's fixed target
    /// size (WARLINE_REVIEW_WIDTH/HEIGHT), which is what ScreenCapture writes. Screen.width/height
    /// are not used: inside EditorApplication.update they report the current Editor GUI view
    /// (1836x1016 on the lane machine), not the Game view.
    /// </summary>
    [InitializeOnLoad]
    public static class SkirmishCameraFramingCapture
    {
        const string ModeEnv = "WARLINE_CAMERA_FRAMING_CAPTURE", LabelEnv = "WARLINE_CAMERA_FRAMING_LABEL", ExitEnv = "WARLINE_CAMERA_FRAMING_EXIT";
        const int SettleFrames = 45, MaxAirCaptures = 16;
        public const int DefaultSeed = 104729, S002Seed = 130365;
        const string ReviewWidthEnv = "WARLINE_REVIEW_WIDTH", ReviewHeightEnv = "WARLINE_REVIEW_HEIGHT";
        public const string LocaleEnv = "WARLINE_CAPTURE_LOCALE";

        static string mode, label, captureLocale = GameLocalization.EnglishLocaleCode;
        static int step, stableFrames, airCaptures, lastAirCaptureFrame = -100000, pendingClearedFrame = -1;
        static Vector3 lastPosition; static Quaternion lastRotation; static float lastFov;
        static double groundDeadline;
        static bool reviewSizeReapplied, drawerWasOpen;
        static readonly Dictionary<int, AirTrack> tracks = new();
        sealed class AirTrack { public float groundedY = float.NaN, lastY, lastHeading; public double headingAt; public bool tookOff, cruised, landed; public double airborneSince; }

        static SkirmishCameraFramingCapture()
        {
            mode = Environment.GetEnvironmentVariable(ModeEnv);
            if (string.IsNullOrEmpty(mode)) return;
            label = Environment.GetEnvironmentVariable(LabelEnv);
            if (string.IsNullOrEmpty(label)) label = "unlabelled";
            if (!TryResolveCaptureLocale(Environment.GetEnvironmentVariable(LocaleEnv), out captureLocale, out string localeError))
            {
                Debug.LogError("[SkirmishCameraFramingCapture] result=Failed reason=" + localeError);
                mode = null; captureLocale = GameLocalization.EnglishLocaleCode;
                return;
            }
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
        /// <summary>
        /// Owner playtest 2026-11-17 ("returning from recruitment sometimes framed the underside of a large
        /// aircraft"): S026 seed 104729, ARIA off. Buys one helicopter through the recruitment drawer's own
        /// card and primary action (touch), returns from recruitment, then flies it with the HUD Move command
        /// and brings it home with the command-wheel Return action. No money grant, no entity spawning.
        /// </summary>
        public static void RunS026HelicopterFramingCapture()
        {
            Arm("aircraft");
            SkirmishS002AriaRunHarness.RunS026HelicopterFramingProbeAndExit(DefaultSeed, captureLocale);
        }

        static void LaunchGround(string catalog)
        {
            Arm("ground");
            SkirmishS002AriaRunHarness.LaunchCatalog(catalog, catalog == "S002" ? S002Seed : DefaultSeed, captureLocale);
        }

        static void Arm(string requested)
        {
            // Fail before any Editor state changes: an unsupported locale must not launch a mislabelled run.
            if (!TryResolveCaptureLocale(Environment.GetEnvironmentVariable(LocaleEnv), out string resolvedLocale, out string localeError))
                throw new InvalidOperationException(localeError);
            captureLocale = resolvedLocale;
            Environment.SetEnvironmentVariable(ModeEnv, requested);
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(LabelEnv))) Environment.SetEnvironmentVariable(LabelEnv, "unlabelled");
            mode = requested; label = Environment.GetEnvironmentVariable(LabelEnv);
            step = 0; stableFrames = 0; airCaptures = 0; tracks.Clear(); reviewSizeReapplied = false; drawerWasOpen = false;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            Game.Runtime.AircraftProductionFocusEvidence.Recorded -= OnAircraftFocus;
            Game.Runtime.AircraftProductionFocusEvidence.Recorded += OnAircraftFocus;
            Directory.CreateDirectory(OutputDirectory());
            Debug.Log("[SkirmishCameraFramingCapture] armed mode=" + requested + " label=" + label + " locale=" + captureLocale + " output=" + OutputDirectory());
        }

        // ------------------------------------------------------------ locale and file names
        /// <summary>
        /// WARLINE_CAPTURE_LOCALE: empty means English; otherwise exactly one of the locale codes the
        /// ARIA queue accepts (en, fa-IR). Anything else is refused rather than guessed.
        /// </summary>
        public static bool TryResolveCaptureLocale(string requested, out string locale, out string error)
        {
            error = null;
            locale = GameLocalization.EnglishLocaleCode;
            if (string.IsNullOrWhiteSpace(requested)) return true;
            string trimmed = requested.Trim();
            if (string.Equals(trimmed, GameLocalization.EnglishLocaleCode, StringComparison.Ordinal) ||
                string.Equals(trimmed, GameLocalization.PersianLocaleCode, StringComparison.Ordinal))
            { locale = trimmed; return true; }
            error = "unsupportedCaptureLocale " + LocaleEnv + "=" + requested + " (use " +
                GameLocalization.EnglishLocaleCode + " or " + GameLocalization.PersianLocaleCode + ")";
            return false;
        }

        static string LocaleToken(string locale) =>
            string.IsNullOrEmpty(locale) || string.Equals(locale, GameLocalization.EnglishLocaleCode, StringComparison.Ordinal) ? "" : "-" + locale;

        /// <summary>{catalog}-{seed}-{w}x{h}[-{locale}]-ground; the locale is omitted for English.</summary>
        public static string GroundFilePrefix(string catalog, int seed, int width, int height, string locale) =>
            catalog + "-" + seed.ToString(CultureInfo.InvariantCulture) + "-" + width.ToString(CultureInfo.InvariantCulture) + "x" +
            height.ToString(CultureInfo.InvariantCulture) + LocaleToken(locale) + "-ground";

        /// <summary>{catalog}-{w}x{h}[-{locale}]-air; the locale is omitted for English.</summary>
        public static string AirFilePrefix(string catalog, int width, int height, string locale) =>
            catalog + "-" + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) + LocaleToken(locale) + "-air";

        // ------------------------------------------------------------ helicopter squad card
        /// <summary>
        /// Presented slot (0-4) of the helicopter's squad card on the current page, or -1. With a known
        /// group id the card is matched by group, which covers every helicopter kind: AirMask marks only
        /// assault air (attack helicopters, strike), so a delivered transport helicopter, a support role,
        /// never sets it. Without a group id the first assault-air card is the fallback.
        /// </summary>
        public static int HelicopterCardSlot(UiExpandedSquadPage page, uint groupId)
        {
            for (int slot = 0; slot < 5; slot++)
                if (groupId != 0 ? page.Group(slot) == groupId : (page.AirMask & (1 << slot)) != 0) return slot;
            return -1;
        }

        /// <summary>Observed squad page state for a failure reason (masks in hex, page as index/count).</summary>
        public static string DescribeSquadPage(bool pageRead, UiExpandedSquadPage page, uint groupId)
        {
            var text = "heliGroup=" + groupId.ToString(CultureInfo.InvariantCulture);
            if (!pageRead) return text + " squadPage=unread";
            return text + " page=" + page.PageIndex.ToString(CultureInfo.InvariantCulture) + "/" + page.PageCount.ToString(CultureInfo.InvariantCulture) +
                " totalGroups=" + page.TotalGroups.ToString(CultureInfo.InvariantCulture) +
                " next=" + (page.NextPage ? 1 : 0) + " previous=" + (page.PreviousPage ? 1 : 0) +
                " airMask=0x" + page.AirMask.ToString("x2", CultureInfo.InvariantCulture) +
                " assaultMask=0x" + page.AssaultMask.ToString("x2", CultureInfo.InvariantCulture) +
                " selectedMask=0x" + page.SelectedMask.ToString("x2", CultureInfo.InvariantCulture) +
                " supportSlots=0x" + page.SupportSlots.ToString("x3", CultureInfo.InvariantCulture) +
                " selectedGroups=" + page.SelectedGroups.ToString(CultureInfo.InvariantCulture) +
                " groups=[" + string.Join(",", page.Group0, page.Group1, page.Group2, page.Group3, page.Group4) + "]";
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
            string prefix = GroundFilePrefix(catalog, native.Seed, width, height, captureLocale);
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
                Debug.Log("[SkirmishCameraFramingCapture] result=Captured mode=ground catalog=" + catalog + " locale=" + captureLocale + " label=" + label + " visualReview=Pending");
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
            // First tick after the recruitment drawer closes: the view the player returns to.
            var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            bool drawerOpen = drawer != null && drawer.IsOpen;
            if (drawerWasOpen && !drawerOpen) CaptureAir("return-from-recruitment", camera, null, true);
            drawerWasOpen = drawerOpen;
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

        static void CaptureAir(string kind, Camera camera, string subject, bool immediate = false)
        {
            if (camera == null || airCaptures >= MaxAirCaptures || (!immediate && Time.frameCount - lastAirCaptureFrame < 20)) return;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated || !TryReadSession(world.EntityManager, out _, out var native) || !ReviewSizeReady()) return;
            lastAirCaptureFrame = Time.frameCount;
            ReviewSize(out int width, out int height, out _);
            string prefix = AirFilePrefix(native.CatalogId.ToString(), width, height, captureLocale);
            string png = Path.Combine(OutputDirectory(), prefix + "-" + airCaptures.ToString("00", CultureInfo.InvariantCulture) + "-" + kind + ".png");
            airCaptures++;
            ScreenCapture.CaptureScreenshot(png);
            AppendJson(prefix + ".jsonl", kind, camera, world.EntityManager, native, png, subject);
        }

        // ------------------------------------------------------------------- facts
        [Serializable] sealed class Shot
        {
            public string label, shot, catalog, locale, png, subject, sizeSource; public int frame, seed, width, height, editorScreenWidth, editorScreenHeight;
            public float elapsed, fieldOfView, aspect, cameraAboveSubjectMin; public Vector3 cameraPosition, cameraEuler, groundCenterZeroPlane;
            public bool profileResolved; public float profileNormalHeight, profileMaxHeight, profileMinHeight, profileGroundY;
            public int ownAirVisible, ownAirAboveCamera, unitOrdersIssued;
        }

        static void AppendJson(string file, string shot, Camera camera, EntityManager em, SkirmishExpandedSessionComponent native, string png, string subject)
        {
            ReviewSize(out int width, out int height, out string sizeSource);
            var row = new Shot { label = label, shot = shot, catalog = native.CatalogId.ToString(), locale = captureLocale, png = png, subject = subject, frame = Time.frameCount,
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
    /// <summary>
    /// S026 helicopter framing driver (normal touch input, ARIA off). Runs on the startup-only
    /// launch path through the existing helipad probe frame and its touch helpers.
    /// </summary>
    public static partial class SkirmishS002AriaRunHarness
    {
        internal const string S026HelicopterFramingEnv = "WARLINE_S026_HELICOPTER_FRAMING";
        internal const int S026HeliStageBase = 100;
        const float S026HeliFundsWaitMatchSeconds = 360f;
        // The delivered helicopter's squad card can appear a few frames after the entity (group record,
        // drawer projection and tray roster rebuild); wait this long in real time before failing.
        internal const double S026HeliCardWaitSeconds = 20d;
        static readonly (SkirmishRoleKind kind, string roleId, string prefab)[] S026HeliChoices =
        {
            (SkirmishRoleKind.TransportHeli, SkirmishRoleIds.TransportHeli, "Unit_Veh_Helicopter_Transport"),
            (SkirmishRoleKind.AttackHeli, SkirmishRoleIds.AttackHeli, "Unit_Veh_Helicopter_Attack"),
            (SkirmishRoleKind.AttackHeliLight, SkirmishRoleIds.AttackHeliLight, "Unit_Veh_Helicopter_Attack_Small"),
        };
        static Entity s026HeliSession, s026HeliActor;
        static int s026HeliChoice = -1, s026HeliMoveAttempts, s026HeliPageTurns;
        static bool s026HeliReturnPhase, s026HeliPageForward = true;
        static double s026HeliCardDeadline;
        static float s026HeliWaitStartMatch = -1f;
        static double s026HeliMoveAt, s026HeliAirborneAt, s026HeliLandedAt, s026HeliLastStatusAt;
        static string s026HeliLastReason = "";

        public static void RunS026HelicopterFramingProbeAndExit(int seed, string locale = GameLocalization.EnglishLocaleCode)
        {
            Environment.SetEnvironmentVariable("WARLINE_S005_CAMERA_TOUCH_ONLY", null);
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_AIRCRAFT_CYCLE", "0");
            Environment.SetEnvironmentVariable(S026HelicopterFramingEnv, "1");
            Environment.SetEnvironmentVariable("WARLINE_S003_HELIPAD_PROBE", "1");
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG", "S026");
            Environment.SetEnvironmentVariable("WARLINE_S002_SEED", seed.ToString(CultureInfo.InvariantCulture));
            Environment.SetEnvironmentVariable("WARLINE_S002_LOCALE", string.IsNullOrEmpty(locale) ? GameLocalization.EnglishLocaleCode : locale);
            Environment.SetEnvironmentVariable("WARLINE_S004_MENU_JOURNEY", null);
            helipadProbeStage = 0;
            // Startup-only: ARIA is never begun (no AriaCommandEvidence/gate units); all input below is touch.
            RunS003StartupAndExit();
        }

        static void StartS026HelicopterFraming(Entity session)
        {
            helipadProbeTouch = null;
            helipadProbeAircraftCycle = false;
            s026HeliSession = session; s026HeliActor = Entity.Null;
            s026HeliChoice = -1; s026HeliMoveAttempts = 0; s026HeliPageTurns = 0; s026HeliReturnPhase = false;
            s026HeliPageForward = true; s026HeliCardDeadline = 0d;
            s026HeliWaitStartMatch = -1f; s026HeliMoveAt = s026HeliAirborneAt = s026HeliLandedAt = s026HeliLastStatusAt = 0d;
            s026HeliLastReason = "";
            helipadProbeStage = S026HeliStageBase;
            new GameObject("NativeS026HelicopterFramingProbe").AddComponent<SkirmishHelipadInputProbeFrame>().Session = session;
            helipadProbeDeadline = UnityEditor.EditorApplication.timeSinceStartup + 1500d;
            deadline = helipadProbeDeadline + 20d;
            Debug.Log("[S026HelicopterFraming] started catalog=" + catalogId + " input=Touch aria=Off");
        }

        static void EndS026HelicopterFraming(bool passed, string reason)
        {
            Debug.Log("[S026HelicopterFraming] result=" + (passed ? "Passed" : "Failed") + " reason=" + reason +
                " input=Touch aria=Off samples=" + (helipadProbeTouch?.AcceptedSamples ?? 0) + " visualReview=Pending");
            Environment.SetEnvironmentVariable(S026HelicopterFramingEnv, null);
            helipadProbeStage = 0;
            Finish(!passed, reason);
        }

        static float S026MatchSeconds(EntityManager em)
        {
            using var clocks = em.CreateEntityQuery(typeof(SkirmishObjectiveClockComponent));
            return clocks.CalculateEntityCount() == 1 ? clocks.GetSingleton<SkirmishObjectiveClockComponent>().ElapsedSeconds : Time.time;
        }

        static bool S026HeliPendingOrLive(EntityManager em, SkirmishRoleKind role, out Entity actor)
        {
            actor = Entity.Null;
            var attempt = em.GetComponentData<SkirmishExpandedSessionComponent>(s026HeliSession).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent),
                typeof(UnitAirComponent), typeof(UnitHealth));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var candidate in actors)
            {
                var own = em.GetComponentData<SkirmishAttemptOwnedComponent>(candidate);
                if (own.FactionId != 1 || own.CapacityReleased != 0 || !own.SessionId.Equals(attempt) ||
                    em.GetComponentData<SkirmishUnitRoleComponent>(candidate).Role != role ||
                    em.GetComponentData<UnitHealth>(candidate).Current <= 0) continue;
                actor = candidate; return true;
            }
            if (em.HasBuffer<SkirmishProductionReservation>(s026HeliSession))
                foreach (var item in em.GetBuffer<SkirmishProductionReservation>(s026HeliSession, true))
                    if (item.FactionId == 1 && item.Role == role &&
                        item.Phase is SkirmishReservationPhase.Reserved or SkirmishReservationPhase.Producing) return true;
            return false;
        }

        static void TickS026HelicopterFraming(double now, SkirmishMatchView skirmish, BuildDrawerView drawer,
            BuildDrawerCatalogRuntimeView catalog, MatchOverlayCommandControlsView controls)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated || !world.EntityManager.Exists(s026HeliSession)) return;
            var em = world.EntityManager;
            if (UiShellRuntimeGateway.TryReadSkirmish(out var model) && model.Finished)
            { EndS026HelicopterFraming(false, "matchEndedBeforeCaptureCompleted:stage=" + helipadProbeStage); return; }
            float match = S026MatchSeconds(em);
            if (now - s026HeliLastStatusAt >= 10d)
            {
                s026HeliLastStatusAt = now;
                Debug.Log("[S026HelicopterFraming] stage=" + helipadProbeStage + " matchSeconds=" + match.ToString("0.0", CultureInfo.InvariantCulture) +
                    " materials=" + model.OwnMaterials + " choice=" + (s026HeliChoice >= 0 ? S026HeliChoices[s026HeliChoice].prefab : "none") +
                    " lastQueueReason=" + s026HeliLastReason);
            }

            // 100: wait on the match clock until the recruitment drawer would accept a helicopter.
            if (helipadProbeStage == S026HeliStageBase)
            {
                if (s026HeliWaitStartMatch < 0f) s026HeliWaitStartMatch = match;
                var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(s026HeliSession).Setup;
                var army = SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
                if (army == null) { EndS026HelicopterFraming(false, "armyProfileUnavailable"); return; }
                var reasons = new List<string>();
                for (int i = 0; i < S026HeliChoices.Length; i++)
                {
                    var decision = SkirmishProductionService.EvaluateQueue(em, s026HeliSession, S026HeliChoices[i].roleId, 1, army, 1);
                    if (decision.Accepted) { s026HeliChoice = i; break; }
                    reasons.Add(S026HeliChoices[i].kind + ":" + decision.Reason + (string.IsNullOrEmpty(decision.Field) ? "" : "/" + decision.Field));
                }
                s026HeliLastReason = string.Join(",", reasons);
                if (s026HeliChoice >= 0)
                {
                    Debug.Log("[S026HelicopterFraming] affordable prefab=" + S026HeliChoices[s026HeliChoice].prefab + " materials=" + model.OwnMaterials +
                        " waitedMatchSeconds=" + (match - s026HeliWaitStartMatch).ToString("0.0", CultureInfo.InvariantCulture) + " moneyGranted=0");
                    helipadProbeStage = S026HeliStageBase + 1;
                    return;
                }
                if (match - s026HeliWaitStartMatch > S026HeliFundsWaitMatchSeconds)
                    EndS026HelicopterFraming(false, "helicopterNotQueueableWithin6MatchMinutes materials=" + model.OwnMaterials + " reasons=" + s026HeliLastReason);
                helipadProbeNext = now + 1d;
                return;
            }
            var choice = S026HeliChoices[s026HeliChoice];
            // 101: recruitment drawer -> Aircrafts tab -> helicopter card -> primary action (the buy button).
            if (helipadProbeStage == S026HeliStageBase + 1)
            {
                if (S026HeliPendingOrLive(em, choice.kind, out _))
                {
                    Debug.Log("[S026HelicopterFraming] purchased prefab=" + choice.prefab + " input=RecruitmentPrimaryAction");
                    helipadProbeStage = S026HeliStageBase + 2;
                    return;
                }
                if (drawer != null && drawer.IsOpen && catalog != null && catalog.TryReadSelectedRequest(out var key, out var cost, out var failure, out var producer) &&
                    key == choice.prefab && failure != BuildingUiCommandFailure.None)
                {
                    s026HeliLastReason = "drawer:" + failure + " cost=" + cost + " producer=" + producer;
                    if (match - s026HeliWaitStartMatch > S026HeliFundsWaitMatchSeconds)
                    { EndS026HelicopterFraming(false, "recruitmentRejectedHelicopter " + s026HeliLastReason); return; }
                }
                Button target = drawer != null && drawer.IsOpen ? catalog?.ResolveCatalogTarget(BuildDrawerCategory.Aircrafts, choice.prefab) : controls?.BuildButton;
                if (drawer != null && drawer.IsOpen && target == null)
                { EndS026HelicopterFraming(false, "helicopterCardNotOffered prefab=" + choice.prefab); return; }
                if (!TapHelipadButton(target, now) && target != null) ScrollHelipadCatalogTo(target, now);
                return;
            }
            // 102: return from recruitment (drawer Close).
            if (helipadProbeStage == S026HeliStageBase + 2)
            {
                if (drawer != null && drawer.IsOpen) { TapHelipadButton(drawer.CloseButton, now); return; }
                Debug.Log("[S026HelicopterFraming] returnedFromRecruitment=1");
                helipadProbeStage = S026HeliStageBase + 3;
                return;
            }
            // 103: wait for the paid helicopter to be delivered.
            if (helipadProbeStage == S026HeliStageBase + 3)
            {
                if (!S026HeliPendingOrLive(em, choice.kind, out var actor)) { EndS026HelicopterFraming(false, "helicopterReservationLost"); return; }
                if (actor == Entity.Null) return;
                s026HeliActor = actor;
                Debug.Log("[S026HelicopterFraming] delivered entity=" + actor + " prefab=" + choice.prefab);
                helipadProbeStage = S026HeliStageBase + 4;
                helipadProbeNext = now + 3d;
                return;
            }
            if (!em.Exists(s026HeliActor) || em.GetComponentData<UnitHealth>(s026HeliActor).Current <= 0)
            { EndS026HelicopterFraming(false, "helicopterLost:stage=" + helipadProbeStage); return; }
            var air = em.GetComponentData<UnitAirComponent>(s026HeliActor);
            // 104: select the helicopter through its squad card, as a player would: find the card of the
            // helicopter's own group on the squad tray (scrolling or paging the tray if needed) and tap it.
            // A transport helicopter is a support role, so it is never in AirMask (assault air only); the
            // group id is the identity that covers every helicopter kind. Waits up to S026HeliCardWaitSeconds
            // of real time for the card to appear; no unit order is issued here.
            if (helipadProbeStage == S026HeliStageBase + 4)
            {
                if (em.HasComponent<SelectedUnitTag>(s026HeliActor))
                {
                    s026HeliCardDeadline = 0d; s026HeliPageTurns = 0; s026HeliPageForward = true;
                    helipadProbeStage = S026HeliStageBase + (s026HeliReturnPhase ? 7 : 5);
                    return;
                }
                if (s026HeliCardDeadline <= 0d) s026HeliCardDeadline = now + S026HeliCardWaitSeconds;
                uint group = em.HasComponent<SkirmishArmyGroupMembershipComponent>(s026HeliActor)
                    ? em.GetComponentData<SkirmishArmyGroupMembershipComponent>(s026HeliActor).GroupId : 0u;
                bool pageRead = UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page);
                var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
                if (now < s026HeliCardDeadline)
                {
                    if (pageRead && tray != null) S026SelectHelicopterCardStep(page, group, tray, now);
                    return;
                }
                EndS026HelicopterFraming(false, "helicopterSquadCardNotFound waitedSeconds=" +
                    S026HeliCardWaitSeconds.ToString("0", CultureInfo.InvariantCulture) + " role=" + choice.kind +
                    " " + SkirmishCameraFramingCapture.DescribeSquadPage(pageRead, page, group) +
                    " tray=" + (tray != null ? "present trayPage=" + tray.LogicalPageIndex : "absent") + " pageTurns=" + s026HeliPageTurns);
                return;
            }
            // 105: HUD Move command, then a touch on visible open ground away from the helicopter.
            if (helipadProbeStage == S026HeliStageBase + 5)
            {
                if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command) || command.ActiveCommandMode != TacticalCommandMode.Move)
                { TapHelipadButton(controls?.MoveButton, now); return; }
                if (!TryS026MoveGround(em, out var point, out var world3))
                { EndS026HelicopterFraming(false, "noVisibleOpenGroundForMove"); return; }
                if (!helipadProbeTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
                { EndS026HelicopterFraming(false, "moveTouchRejected"); return; }
                Debug.Log("[S026HelicopterFraming] input=Touch command=Move world=" + world3 + " point=" + point);
                s026HeliMoveAttempts++; s026HeliMoveAt = now;
                helipadProbeStage = S026HeliStageBase + 6;
                helipadProbeNext = now + 1d;
                return;
            }
            // 106: takeoff must follow the order; cruise for a while (capture fires takeoff/turn/cruise).
            if (helipadProbeStage == S026HeliStageBase + 6)
            {
                if (air.Airborne == 0 && s026HeliAirborneAt <= 0d)
                {
                    if (now - s026HeliMoveAt < 15d) return;
                    if (s026HeliMoveAttempts >= 2) { EndS026HelicopterFraming(false, "moveOrderDidNotLaunchHelicopter"); return; }
                    helipadProbeStage = S026HeliStageBase + 4;
                    return;
                }
                if (s026HeliAirborneAt <= 0d) { s026HeliAirborneAt = now; Debug.Log("[S026HelicopterFraming] airborne=1"); }
                if (now - s026HeliAirborneAt < 20d) return;
                s026HeliReturnPhase = true;
                helipadProbeStage = S026HeliStageBase + 7;
                return;
            }
            // 107: bring it home with the command wheel's visible Return action.
            if (helipadProbeStage == S026HeliStageBase + 7)
            {
                if (air.ReturningHome != 0) { Debug.Log("[S026HelicopterFraming] input=Touch command=Return"); helipadProbeStage = S026HeliStageBase + 8; return; }
                if (!em.HasComponent<SelectedUnitTag>(s026HeliActor))
                {
                    if (UiShellRuntimeGateway.TryReadMatchHudCommandState(out var mode) && mode.ActiveCommandMode != TacticalCommandMode.None &&
                        mode.ActiveCommandMode != TacticalCommandMode.Select) { TapHelipadButton(controls?.SelectButton, now); return; }
                    helipadProbeStage = S026HeliStageBase + 4; s026HeliPageTurns = 0; s026HeliCardDeadline = 0d; // reselect, then come back here
                    return;
                }
                TapHelipadButton(controls?.CommandWheelPanel?.NextReturnButton, now);
                if (now - s026HeliAirborneAt > 120d) EndS026HelicopterFraming(false, "returnCommandNotObserved");
                return;
            }
            // 108: touchdown at home; leave time for the landing and ground switch-back frames.
            if (helipadProbeStage == S026HeliStageBase + 8)
            {
                bool home = air.HomeInitialized != 0 && air.Airborne == 0 && air.ReturningHome == 0 &&
                    math.distance(em.GetComponentData<LocalTransform>(s026HeliActor).Position, air.HomePosition) <= 1.5f;
                if (!home) { if (now - s026HeliAirborneAt > 300d) EndS026HelicopterFraming(false, "touchdownNotObserved"); return; }
                if (s026HeliLandedAt <= 0d) { s026HeliLandedAt = now; Debug.Log("[S026HelicopterFraming] touchdown=1"); }
                if (now - s026HeliLandedAt < 4d) return;
                EndS026HelicopterFraming(true, "paidHelicopterRecruitReturnMoveCruiseReturnTouchdown");
            }
        }

        /// <summary>
        /// At most one normal touch toward selecting the helicopter's card per call: clear a different
        /// selection, tap the card on the current page, tap or scroll to the card anywhere in the tray
        /// roster, else page the tray forward to the last page and back. Does nothing while a selection
        /// of the card is pending or the input helper is busy (the caller re-reads next frame).
        /// </summary>
        static void S026SelectHelicopterCardStep(UiExpandedSquadPage page, uint group, MatchHudSquadTrayView tray, double now)
        {
            int slot = SkirmishCameraFramingCapture.HelicopterCardSlot(page, group);
            if (slot >= 0 && (page.SelectedMask & (1 << slot)) != 0) return; // selected; the tag follows
            if (page.SelectedGroups > 0) { DeselectHelipadSquads(tray, now); return; }
            if (slot >= 0)
            {
                if (TapHelipadSquadSlot(tray, slot, now))
                    Debug.Log("[S026HelicopterFraming] input=Touch squadCard slot=" + slot + " group=" + group + " page=" + page.PageIndex);
                return;
            }
            if (group != 0)
            {
                var target = MatchHudSquadTouchNavigation.GroupTarget(tray, group);
                if (target.Available)
                {
                    if (HelipadSquadInput(target, now, false))
                        Debug.Log("[S026HelicopterFraming] input=Touch squadCard group=" + group + " kind=" + (target.Drag ? "TrayScroll" : "CardTap"));
                    return;
                }
            }
            if (page.PageCount <= 1) return; // single page: the card has not been presented yet
            if (s026HeliPageForward && !page.NextPage) s026HeliPageForward = false;
            else if (!s026HeliPageForward && !page.PreviousPage) s026HeliPageForward = true;
            if (!SwipeHelipadSquadPage(tray, s026HeliPageForward, now)) return;
            s026HeliPageTurns++;
            Debug.Log("[S026HelicopterFraming] input=Touch squadPage forward=" + (s026HeliPageForward ? 1 : 0) + " page=" + page.PageIndex + "/" + page.PageCount);
        }

        static bool TryS026MoveGround(EntityManager em, out Vector2 point, out Vector3 world)
        {
            point = default; world = default;
            var camera = Camera.main;
            if (camera == null) return false;
            Vector3 from = em.GetComponentData<LocalTransform>(s026HeliActor).Position;
            var ground = new Plane(Vector3.up, new Vector3(0f, from.y, 0f));
            foreach (var v in new[] { new Vector2(.78f, .55f), new Vector2(.22f, .55f), new Vector2(.5f, .72f), new Vector2(.5f, .38f), new Vector2(.7f, .7f), new Vector2(.3f, .7f) })
            {
                var candidate = new Vector2(Screen.width * v.x, Screen.height * v.y);
                if (!Screen.safeArea.Contains(candidate)) continue;
                var hits = new List<RaycastResult>();
                if (EventSystem.current != null)
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = candidate }, hits);
                if (hits.Count > 0) continue;
                var ray = camera.ScreenPointToRay(candidate);
                if (!ground.Raycast(ray, out float distance)) continue;
                var hit = ray.GetPoint(distance);
                if (Vector3.Distance(new Vector3(hit.x, 0, hit.z), new Vector3(from.x, 0, from.z)) < 40f) continue;
                point = candidate; world = hit; return true;
            }
            return false;
        }
    }
}
#endif
