#if UNITY_EDITOR
using System;
using System.Collections;
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
using Game.UI.Shell.Contracts.Ecs;
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
    /// S005 air events: lens-applied, return-from-recruitment, takeoff, turn, cruise, landing, switch-back-ground.
    /// S026 player-directed flight: one row per frame in {catalog}-{w}x{h}[-{locale}]-air-flight.jsonl (camera
    /// pose, aircraft pose/heading, airframe share, rotor/landing-patch on screen, selected card role) with shots
    /// selected, takeoff, mid-turn, cruise, approach, touchdown, switch-back-ground, switch-back-settled, and a
    /// fail-closed ...-air-flight-summary.jsonl.
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
        static string openingPoseEvidence;
        static bool reviewSizeReapplied, drawerWasOpen, openingScheduled;
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
        /// Designer retest 2026-11-18 (owner: whole aircraft, rotor, heading and landing patch readable together;
        /// the player reacquires the ground squad). S026 seed 104729, ARIA off, touch only. Buys one helicopter
        /// through the recruitment drawer, selects its squad card, taps Move on open ground to the side, then taps
        /// Move on the home helipad (the landing patch) during the outbound leg, so it turns about 180 degrees,
        /// cruises, approaches and touches down on the patch; then taps an infantry squad card and the helicopter
        /// card so only the ground squad is selected. No money grant, no entity spawning, no Return command.
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
            openingScheduled = false; groundDeadline = 0d; openingPoseEvidence = null;
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
            catch (Exception error) { FailCapture(error.Message); }
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
            string catalog = native.CatalogId.ToString();
            if (step == 0 && catalog == "S002")
            {
                double now = EditorApplication.timeSinceStartup;
                if (groundDeadline <= 0d) groundDeadline = now + 60d;
                if (now >= groundDeadline) { FailCapture("openingPoseTimeout " + (openingPoseEvidence ?? "noRenderedFrame")); return; }
                if (openingScheduled || !ReviewSizeReady()) return;
                var scene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
                if (scene == null) return;
                openingScheduled = true;
                scene.StartCoroutine(CaptureS002Opening(em, session, native));
                return;
            }
            if (step == 0)
            {
                bool owned = SkirmishS005OpeningViewPolicy.OwnsIntroduction(em, session) ||
                    SkirmishMissionLogicalBindingProvider.OwnsOpeningIntroduction(em, session);
                if (groundDeadline <= 0) groundDeadline = EditorApplication.timeSinceStartup + 60;
                if (!owned && EditorApplication.timeSinceStartup < groundDeadline) { stableFrames = 0; return; }
            }
            if (!ReviewSizeReady() || !Settled(camera)) return;
            ReviewSize(out int width, out int height, out _);
            string prefix = GroundFilePrefix(catalog, native.Seed, width, height, captureLocale);
            string png = Path.Combine(OutputDirectory(), prefix + "-" + GroundShots[step] + ".png");
            ScreenCapture.CaptureScreenshot(png);
            AppendJson(prefix + ".jsonl", GroundShots[step], camera, em, native, png, null);
            AdvanceGroundShot(catalog, png);
        }

        static IEnumerator CaptureS002Opening(EntityManager em, Entity session, SkirmishExpandedSessionComponent native)
        {
            var endOfFrame = new WaitForEndOfFrame();
            using var intro = em.CreateEntityQuery(typeof(MatchIntroTransitionComponent));
            using var requests = em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            using var cameraStates = em.CreateEntityQuery(typeof(RtsCameraStateComponent));
            int firstPlayableFrame = -1;
            RuntimeCameraFocusRequestComponent opening = default;
            while (mode == "ground" && step == 0)
            {
                yield return endOfFrame;
                if (mode != "ground") break;
                try
                {
                    if (!em.Exists(session) || em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase != SkirmishSessionPhase.Playing)
                        throw new InvalidOperationException("matchEndedBeforeOpeningCapture");
                    var camera = Camera.main;
                    var request = requests.CalculateEntityCount() == 1 ? requests.GetSingleton<RuntimeCameraFocusRequestComponent>() : default;
                    bool owned = SkirmishS005OpeningViewPolicy.OwnsIntroduction(em, session) && request.UseExplicitPerspective == 1 &&
                        math.distancesq(request.World, em.GetComponentData<SkirmishS005OpeningFocusOwner>(session).GroundRequest) < .000001f;
                    if (owned) opening = request;
                    var transition = intro.CalculateEntityCount() == 1 ? intro.GetSingleton<MatchIntroTransitionComponent>() : default;
                    bool playable = transition.State == MatchIntroTransitionStateKind.Complete && transition.InputLocked == 0 &&
                        SkirmishLaunchProjection.IsSimulationActive(em);
                    if (playable && firstPlayableFrame < 0) firstPlayableFrame = Time.frameCount;
                    var perspective = opening.Perspective;
                    var capturedGround = camera != null ? GroundCenter(camera) : default;
                    var state = cameraStates.CalculateEntityCount() == 1 ? cameraStates.GetSingleton<RtsCameraStateComponent>() : default;
                    openingPoseEvidence = $"firstPlayableFrame={firstPlayableFrame} frame={Time.frameCount} ownedRequest={owned} " +
                        $"composedYaw={perspective.z:F2} capturedYaw={camera?.transform.eulerAngles.y:F2} " +
                        $"composedGround={opening.World} capturedGround={capturedGround:F2} " +
                        $"smoothFocus={state.HasSmoothFocusTarget} smoothPerspective={state.HasSmoothPerspectiveTarget}";
                    // Compare with the consumed mission request while both camera transitions are free to finish.
                    if (!playable || !owned || request.Requested != 0 || camera == null || camera.orthographic ||
                        Vector3.Distance(capturedGround, (Vector3)request.World) > .5f ||
                        Quaternion.Angle(camera.transform.rotation, Quaternion.Euler(perspective.y, perspective.z, 0f)) > .2f ||
                        Mathf.Abs(camera.transform.position.y - perspective.x) > .1f || Mathf.Abs(camera.fieldOfView - perspective.w) > .1f)
                        continue;

                    ReviewSize(out int width, out int height, out _);
                    string prefix = GroundFilePrefix("S002", native.Seed, width, height, captureLocale);
                    string png = Path.Combine(OutputDirectory(), prefix + "-opening.png");
                    SaveRenderedFrame(png, width, height);
                    AppendJson(prefix + ".jsonl", "opening", camera, em, native, png, null);
                    Debug.Log("[SkirmishCameraFramingCapture] openingFrame=Captured attempt=" + native.SessionId + " " + openingPoseEvidence);
                    AdvanceGroundShot("S002", png);
                }
                catch (Exception error) { FailCapture(error.Message); }
            }
        }

        internal static void SaveRenderedFrame(string png, int width, int height)
        {
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            if (texture == null) throw new InvalidOperationException("Game view screenshot unavailable");
            try
            {
                if (texture.width != width || texture.height != height)
                    throw new InvalidOperationException($"Game view PNG size {texture.width}x{texture.height}, expected {width}x{height}");
                File.WriteAllBytes(png, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.Destroy(texture); }
        }

        static void FailCapture(string reason)
        {
            Debug.LogError("[SkirmishCameraFramingCapture] result=Failed step=" + step + " reason=" + reason);
            bool ground = mode == "ground";
            mode = null;
            if (ground && Environment.GetEnvironmentVariable(ExitEnv) == "1") SkirmishS002AriaRunHarness.AbortFailedValidationAndExit();
        }

        static void AdvanceGroundShot(string catalog, string png)
        {
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
            if (flightActive) { lastFocusFact = fact.stage + (string.IsNullOrEmpty(fact.reason) ? "" : ":" + fact.reason); return; }
            if (fact.stage == "Applied") CaptureAir("lens-applied", Camera.main, null);
            // S005 cycle only. This fired on any lens clear, including the lens's own geometry
            // timeout, so it never proved a switch to the ground squad (the S026 flight checks the selection).
            if (fact.stage == "Cleared") pendingClearedFrame = Time.frameCount + 30;
        }

        static void TickAircraft(EntityManager em, SkirmishExpandedSessionComponent native, Camera camera)
        {
            // The S026 player-directed flight records every frame itself; the event-driven shots
            // below remain for the S005 aircraft cycle.
            if (flightActive) { RecordFlightFrame(em, native, camera); return; }
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

        // ------------------------------------------------- S026 player-directed flight
        // One jsonl row per rendered frame from helicopter selection to after the switch-back:
        //   {catalog}-{w}x{h}[-{locale}]-air-flight.jsonl, shots ...-air-flight-{nn}-{shot}.png
        // Rows carry runId, so repeated runs under one label stay separable.
        [Serializable] sealed class FlightRow
        {
            public string label, runId, catalog, locale, stage, shot, png, cameraName, focusFact, lensOrigin, lensBoundsSource, lensClearReason,
                aircraftRole, airframeSource, selectedCardRole, selectedRoles;
            public int frame, cameraInstanceId, aircraftIndex, selectedOwn, selectedInfantry, selectedInfantryOnScreen, selectedOffPage, unitOrdersIssued;
            public float realtime, fieldOfView, aspect, aircraftHeading, airframeFraction, airframeFractionCap, lensDistance, cameraToLensPoseError, distanceToPatch;
            public Vector3 cameraPosition, cameraEuler, aircraftPosition, landingPatch, lensLookAt, lensPatch;
            public Quaternion cameraRotation;
            public Vector2 airframeViewportMin, airframeViewportMax, landingPatchViewport;
            public bool aircraftAirborne, aircraftReturningHome, aircraftHasMoveTarget, aircraftSelected, rotorFullyOnScreen,
                hasLandingPatch, landingPatchOnScreen, landingPatchInSafeBand, lensApplied, lensFollowing, lensPatchFramed;
        }
        [Serializable] sealed class FlightSummary
        {
            public string label, runId, result, failures; public int rows, flightRows, followingRows, measuredRows, distinctCameraPositions, shots;
            public float cameraTravel, aircraftTravel, maxAirframeFraction, maxTurn, touchdownToPatch, maxLensPoseError;
            public string switchBackSelectedCardRole, switchBackSelectedRoles; public int switchBackInfantryOnScreen;
        }

        static bool flightActive;
        static Entity flightAircraft;
        static string flightStage = "", flightRunId = "", pendingFlightShot, lastFocusFact = "";
        static Vector3? flightPatch;
        static int lastFlightFrame = -1, flightShots;
        static readonly List<FlightRow> flightRows = new();
        static readonly HashSet<string> FlightStages = new(StringComparer.Ordinal) { "takeoff", "landing-move", "turn", "cruise", "approach" };

        internal static void BeginFlight(Entity aircraft, Vector3? patch)
        {
            if (mode != "aircraft") return;
            flightActive = true; flightAircraft = aircraft; flightPatch = patch; flightStage = "selected"; pendingFlightShot = null;
            flightRunId = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
            lastFlightFrame = -1; flightShots = 0; flightRows.Clear(); lastFocusFact = "";
            Debug.Log("[SkirmishCameraFramingCapture] flight=Begin runId=" + flightRunId + " aircraft=" + aircraft);
        }
        internal static void EndFlight() { flightActive = false; pendingFlightShot = null; }
        internal static void SetFlightStage(string stage, Vector3? patch) { flightStage = stage ?? ""; if (patch.HasValue) flightPatch = patch; }
        internal static void RequestFlightShot(string shot) { if (flightActive) pendingFlightShot = shot; }

        static void RecordFlightFrame(EntityManager em, SkirmishExpandedSessionComponent native, Camera camera)
        {
            if (Time.frameCount == lastFlightFrame || !ReviewSizeReady()) return;
            lastFlightFrame = Time.frameCount;
            ReviewSize(out int width, out int height, out _);
            string prefix = AirFilePrefix(native.CatalogId.ToString(), width, height, captureLocale) + "-flight";
            var row = new FlightRow { label = label, runId = flightRunId, catalog = native.CatalogId.ToString(), locale = captureLocale, stage = flightStage,
                frame = Time.frameCount, realtime = Time.realtimeSinceStartup, cameraName = camera.name, cameraInstanceId = camera.GetEntityId().GetHashCode(),
                cameraPosition = camera.transform.position, cameraRotation = camera.transform.rotation, cameraEuler = camera.transform.eulerAngles,
                fieldOfView = camera.fieldOfView, aspect = camera.aspect, focusFact = lastFocusFact, airframeFraction = float.NaN,
                airframeFractionCap = SkirmishCameraFraming.AircraftMaximumViewportFraction, distanceToPatch = float.NaN, cameraToLensPoseError = float.NaN,
                airframeSource = "Unavailable", unitOrdersIssued = 0 };
            if (em.Exists(flightAircraft) && em.HasComponent<LocalTransform>(flightAircraft))
            {
                var transform = em.GetComponentData<LocalTransform>(flightAircraft);
                var forward = math.mul(transform.Rotation, new float3(0, 0, 1));
                row.aircraftIndex = flightAircraft.Index; row.aircraftPosition = transform.Position;
                row.aircraftHeading = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                row.aircraftRole = em.HasComponent<SkirmishUnitRoleComponent>(flightAircraft) ? em.GetComponentData<SkirmishUnitRoleComponent>(flightAircraft).Role.ToString() : "";
                row.aircraftSelected = em.HasComponent<SelectedUnitTag>(flightAircraft);
                row.aircraftHasMoveTarget = em.HasComponent<UnitTarget>(flightAircraft);
                if (em.HasComponent<UnitAirComponent>(flightAircraft))
                {
                    var air = em.GetComponentData<UnitAirComponent>(flightAircraft);
                    row.aircraftAirborne = air.Airborne != 0; row.aircraftReturningHome = air.ReturningHome != 0;
                }
                // Airframe share and rotor visibility from the measured visible geometry (rotor disc included);
                // the role-sized stand-in box is used and labelled only when measured geometry is not readable.
                Bounds box = default; bool haveBox = false;
                if (SkirmishMissionViewProfileApplication.TryReadAircraftFollowBounds(em, flightAircraft, out _, out box)) { row.airframeSource = "Measured"; haveBox = true; }
                else if (SkirmishMissionViewProfileApplication.TryNominalAircraftBounds(em, flightAircraft, out _, out box)) { row.airframeSource = "Nominal"; haveBox = true; }
                if (haveBox && TryViewportBox(camera, box, out var min, out var max))
                {
                    row.airframeViewportMin = min; row.airframeViewportMax = max;
                    row.airframeFraction = Mathf.Max(max.x - min.x, max.y - min.y);
                    row.rotorFullyOnScreen = min.x >= 0f && min.y >= 0f && max.x <= 1f && max.y <= 1f;
                }
                if (flightPatch.HasValue)
                {
                    row.hasLandingPatch = true; row.landingPatch = flightPatch.Value;
                    row.distanceToPatch = math.distance(transform.Position.xz, new float2(flightPatch.Value.x, flightPatch.Value.z));
                    var viewport = camera.WorldToViewportPoint(flightPatch.Value);
                    row.landingPatchViewport = new Vector2(viewport.x, viewport.y);
                    row.landingPatchOnScreen = viewport.z > camera.nearClipPlane && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
                    row.landingPatchInSafeBand = row.landingPatchOnScreen && viewport.x >= .08f && viewport.x <= .92f && viewport.y >= .15f && viewport.y <= .85f;
                }
            }
            var cameraOwner = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<RtsCameraSystem>();
            if (cameraOwner != null)
            {
                row.lensClearReason = cameraOwner.LastAircraftFocusClearReason ?? "";
                if (cameraOwner.TryReadAircraftFollowState(out var follow))
                {
                    row.lensApplied = follow.Applied; row.lensFollowing = follow.Following; row.lensOrigin = follow.Origin; row.lensBoundsSource = follow.BoundsSource;
                    row.lensLookAt = follow.LookAt; row.lensDistance = follow.Distance; row.lensPatch = follow.LandingPatch; row.lensPatchFramed = follow.LandingPatchFramed;
                    // Same camera the lens drives? Its pose must be the look-at minus forward * distance.
                    if (follow.Applied) row.cameraToLensPoseError = Vector3.Distance(camera.transform.position, follow.LookAt - camera.transform.forward * follow.Distance);
                }
            }
            ReadSelection(em, camera, row);
            if (pendingFlightShot != null)
            {
                string png = Path.Combine(OutputDirectory(), prefix + "-" + flightShots.ToString("00", CultureInfo.InvariantCulture) + "-" + pendingFlightShot + ".png");
                ScreenCapture.CaptureScreenshot(png);
                row.shot = pendingFlightShot; row.png = png; flightShots++; pendingFlightShot = null;
                Debug.Log("[SkirmishCameraFramingCapture] flightShot=" + row.shot + " stage=" + row.stage + " camera=" + row.cameraPosition +
                    " airframeFraction=" + row.airframeFraction.ToString("0.000", CultureInfo.InvariantCulture) + " rotorOnScreen=" + (row.rotorFullyOnScreen ? 1 : 0) +
                    " patchOnScreen=" + (row.landingPatchOnScreen ? 1 : 0) + " selectedCardRole=" + row.selectedCardRole + " png=" + png);
            }
            flightRows.Add(row);
            File.AppendAllText(Path.Combine(OutputDirectory(), prefix + ".jsonl"), JsonUtility.ToJson(row) + "\n");
        }

        static bool TryViewportBox(Camera camera, Bounds box, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.MaxValue, float.MaxValue); max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                var corner = box.center + Vector3.Scale(box.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var viewport = camera.WorldToViewportPoint(corner);
                if (!(viewport.z > camera.nearClipPlane) || !float.IsFinite(viewport.x) || !float.IsFinite(viewport.y)) return false;
                min = Vector2.Min(min, viewport); max = Vector2.Max(max, viewport);
            }
            return true;
        }

        /// <summary>Selected own units (ECS) and the roles of the squad cards the HUD shows as selected.</summary>
        static void ReadSelection(EntityManager em, Camera camera, FlightRow row)
        {
            var roles = new SortedSet<string>(StringComparer.Ordinal);
            var groupRoles = new Dictionary<uint, string>();
            using (var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent), typeof(UnitHealth), typeof(LocalTransform)))
            using (var actors = query.ToEntityArray(Allocator.Temp))
                foreach (var actor in actors)
                {
                    var own = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                    if (own.FactionId != 1 || own.IsStructure != 0 || em.GetComponentData<UnitHealth>(actor).Current <= 0) continue;
                    var role = em.GetComponentData<SkirmishUnitRoleComponent>(actor);
                    if (em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor))
                    {
                        uint group = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId;
                        if (group != 0 && !groupRoles.ContainsKey(group)) groupRoles[group] = role.Role + "/" + role.Category;
                    }
                    if (!em.HasComponent<SelectedUnitTag>(actor)) continue;
                    row.selectedOwn++; roles.Add(role.Role + "/" + role.Category);
                    if (role.Category != SkirmishPopulationCategory.Infantry) continue;
                    row.selectedInfantry++;
                    var viewport = camera.WorldToViewportPoint(em.GetComponentData<LocalTransform>(actor).Position);
                    if (viewport.z > camera.nearClipPlane && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f) row.selectedInfantryOnScreen++;
                }
            row.selectedRoles = string.Join(",", roles);
            var cards = new List<string>();
            if (UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page))
            {
                for (int slot = 0; slot < 5; slot++)
                    if ((page.SelectedMask & (1 << slot)) != 0)
                        cards.Add(groupRoles.TryGetValue(page.Group(slot), out var cardRole) ? cardRole : "group" + page.Group(slot));
                row.selectedOffPage = page.SelectedOffPage;
            }
            row.selectedCardRole = string.Join(",", cards);
        }

        /// <summary>
        /// Fail-closed checks over the recorded flight (designer retest 2026-11-18): the camera moved with
        /// the flight and is the camera the follow view drives, the airframe stayed within the share cap with
        /// its rotor on screen in the flight frames, the aircraft turned at least 60 degrees and touched down
        /// on the landing patch, and after the switch-back only the infantry squad is selected and on screen.
        /// </summary>
        internal static bool TryEvaluateFlight(float maxTurn, float touchdownToPatch, out string summary)
        {
            var failures = new List<string>();
            var result = new FlightSummary { label = label, runId = flightRunId, rows = flightRows.Count, maxTurn = maxTurn, touchdownToPatch = touchdownToPatch };
            var flight = flightRows.FindAll(r => FlightStages.Contains(r.stage));
            result.flightRows = flight.Count;
            var distinct = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < flight.Count; i++)
            {
                var r = flight[i];
                distinct.Add(r.cameraPosition.x.ToString("0.00", CultureInfo.InvariantCulture) + "," + r.cameraPosition.y.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                    r.cameraPosition.z.ToString("0.00", CultureInfo.InvariantCulture));
                if (i > 0) { result.cameraTravel += Vector3.Distance(flight[i - 1].cameraPosition, r.cameraPosition); result.aircraftTravel += Vector3.Distance(flight[i - 1].aircraftPosition, r.aircraftPosition); }
                if (r.lensFollowing) result.followingRows++;
                if (r.lensFollowing && float.IsFinite(r.cameraToLensPoseError)) result.maxLensPoseError = Mathf.Max(result.maxLensPoseError, r.cameraToLensPoseError);
                if (r.airframeSource == "Measured" && r.lensFollowing && r.aircraftAirborne && float.IsFinite(r.airframeFraction))
                { result.measuredRows++; result.maxAirframeFraction = Mathf.Max(result.maxAirframeFraction, r.airframeFraction); }
                if (r.shot != null) result.shots++;
            }
            result.distinctCameraPositions = distinct.Count;
            if (flight.Count < 20) failures.Add("flightRows=" + flight.Count + "<20");
            if (distinct.Count < 10 || result.cameraTravel < 5f)
                failures.Add("cameraPositionUnchanged distinct=" + distinct.Count + " cameraTravel=" + result.cameraTravel.ToString("0.0", CultureInfo.InvariantCulture) +
                    " aircraftTravel=" + result.aircraftTravel.ToString("0.0", CultureInfo.InvariantCulture));
            else if (result.cameraTravel < .5f * result.aircraftTravel)
                failures.Add("cameraDidNotFollow cameraTravel=" + result.cameraTravel.ToString("0.0", CultureInfo.InvariantCulture) + " aircraftTravel=" + result.aircraftTravel.ToString("0.0", CultureInfo.InvariantCulture));
            if (result.followingRows < .8f * Mathf.Max(1, flight.Count)) failures.Add("followNotActive followingRows=" + result.followingRows + "/" + flight.Count + " lastClear=" + (flightRows.Count > 0 ? flightRows[flightRows.Count - 1].lensClearReason : ""));
            if (result.maxLensPoseError > .5f) failures.Add("loggedCameraIsNotTheFollowCamera maxPoseError=" + result.maxLensPoseError.ToString("0.00", CultureInfo.InvariantCulture));
            if (result.measuredRows < 10) failures.Add("airframeFractionNotMeasured measuredRows=" + result.measuredRows);
            if (result.maxAirframeFraction > SkirmishCameraFraming.AircraftMaximumViewportFraction + .02f)
                failures.Add("airframeFractionAboveCap max=" + result.maxAirframeFraction.ToString("0.000", CultureInfo.InvariantCulture));
            foreach (var r in flightRows)
                if (r.shot != null && (FlightStages.Contains(r.stage) || r.stage == "touchdown") && !r.rotorFullyOnScreen)
                    failures.Add("rotorNotOnScreen shot=" + r.shot + " source=" + r.airframeSource);
            if (!flightRows.Exists(r => r.shot == "approach" && r.landingPatchOnScreen)) failures.Add("landingPatchNotOnScreenAtApproach");
            foreach (string shot in new[] { "takeoff", "mid-turn", "cruise", "approach", "touchdown", "switch-back-ground", "switch-back-settled" })
                if (!flightRows.Exists(r => r.shot == shot)) failures.Add("missingShot=" + shot);
            if (!(maxTurn >= 60f)) failures.Add("turnBelow60 maxTurn=" + maxTurn.ToString("0.0", CultureInfo.InvariantCulture));
            if (!(touchdownToPatch <= 8f)) failures.Add("touchdownNotOnPatch toPatch=" + touchdownToPatch.ToString("0.0", CultureInfo.InvariantCulture));
            var settled = flightRows.FindLast(r => r.shot == "switch-back-settled");
            if (settled == null) failures.Add("switchBackNotRecorded");
            else
            {
                result.switchBackSelectedCardRole = settled.selectedCardRole; result.switchBackSelectedRoles = settled.selectedRoles; result.switchBackInfantryOnScreen = settled.selectedInfantryOnScreen;
                bool cardsInfantry = !string.IsNullOrEmpty(settled.selectedCardRole);
                foreach (var card in settled.selectedCardRole.Split(',')) if (!card.EndsWith("/" + SkirmishPopulationCategory.Infantry, StringComparison.Ordinal)) cardsInfantry = false;
                if (settled.aircraftSelected || settled.selectedInfantry == 0 || settled.selectedInfantry != settled.selectedOwn || !cardsInfantry)
                    failures.Add("switchBackRoleNotInfantry aircraftSelected=" + (settled.aircraftSelected ? 1 : 0) + " selectedRoles=[" + settled.selectedRoles + "] selectedCardRole=[" + settled.selectedCardRole + "]");
                if (settled.selectedInfantryOnScreen == 0) failures.Add("groundSquadNotOnScreenAfterSwitchBack camera=" + settled.cameraPosition);
            }
            result.result = failures.Count == 0 ? "Passed" : "Failed"; result.failures = string.Join(";", failures);
            summary = "rows=" + result.rows + " flightRows=" + result.flightRows + " distinctCamera=" + result.distinctCameraPositions +
                " cameraTravel=" + result.cameraTravel.ToString("0.0", CultureInfo.InvariantCulture) + " aircraftTravel=" + result.aircraftTravel.ToString("0.0", CultureInfo.InvariantCulture) +
                " maxAirframeFraction=" + result.maxAirframeFraction.ToString("0.000", CultureInfo.InvariantCulture) + " maxTurn=" + maxTurn.ToString("0.0", CultureInfo.InvariantCulture) +
                " touchdownToPatch=" + touchdownToPatch.ToString("0.0", CultureInfo.InvariantCulture) + " switchBackCards=[" + result.switchBackSelectedCardRole + "]" +
                (failures.Count == 0 ? "" : " failures=" + result.failures);
            try
            {
                if (flightRows.Count > 0)
                {
                    ReviewSize(out int width, out int height, out _);
                    string prefix = AirFilePrefix(flightRows[0].catalog, width, height, captureLocale) + "-flight";
                    File.AppendAllText(Path.Combine(OutputDirectory(), prefix + "-summary.jsonl"), JsonUtility.ToJson(result) + "\n");
                }
            }
            catch (Exception error) { failures.Add("summaryWriteFailed:" + error.GetType().Name); summary += " summaryWriteFailed"; }
            Debug.Log("[SkirmishCameraFramingCapture] flight=" + result.result + " runId=" + flightRunId + " " + summary + " visualReview=Pending phoneAcceptance=Pending");
            return failures.Count == 0;
        }

        // ------------------------------------------------------------------- facts
        [Serializable] sealed class Shot
        {
            public string label, shot, catalog, locale, png, subject, sizeSource, attempt; public int frame, seed, width, height, editorScreenWidth, editorScreenHeight;
            public float elapsed, fieldOfView, aspect, cameraAboveSubjectMin; public Vector3 cameraPosition, cameraEuler, groundCenterZeroPlane;
            public bool profileResolved; public float profileNormalHeight, profileMaxHeight, profileMinHeight, profileGroundY;
            public int ownAirVisible, ownAirAboveCamera, unitOrdersIssued;
        }

        static void AppendJson(string file, string shot, Camera camera, EntityManager em, SkirmishExpandedSessionComponent native, string png, string subject)
        {
            ReviewSize(out int width, out int height, out string sizeSource);
            var row = new Shot { label = label, shot = shot, catalog = native.CatalogId.ToString(), locale = captureLocale, png = png, subject = subject, frame = Time.frameCount, attempt = native.SessionId.ToString(),
                seed = native.Seed, width = width, height = height, sizeSource = sizeSource, editorScreenWidth = Screen.width, editorScreenHeight = Screen.height, elapsed = Time.timeSinceLevelLoad, fieldOfView = camera.fieldOfView, aspect = camera.aspect,
                cameraPosition = camera.transform.position, cameraEuler = camera.transform.eulerAngles, cameraAboveSubjectMin = float.NaN, unitOrdersIssued = 0 };
            row.groundCenterZeroPlane = GroundCenter(camera);
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

        static Vector3 GroundCenter(Camera camera)
        {
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            return new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance) ? ray.GetPoint(distance) : default;
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
        // Player-directed flight (designer retest 2026-11-18). In Skirmish a helicopter lands only on
        // its home pad: one that reaches a Move goal flies home and lands by itself
        // (UnitAirMovementSystem). The landing patch is therefore the home helipad, and the landing
        // is the player's Move tapped on that pad during a sideways outbound leg (a turn of about
        // 180 degrees), not the command-wheel Return.
        const double S026HeliFollowWaitSeconds = 10d;
        const float S026HeliOutboundMinimumMetres = 30f, S026HeliLandingTapMinimumMetres = 15f;
        const float S026HeliApproachMetres = 25f, S026HeliTouchdownToleranceMetres = 8f;
        static readonly (SkirmishRoleKind kind, string roleId, string prefab)[] S026HeliChoices =
        {
            (SkirmishRoleKind.TransportHeli, SkirmishRoleIds.TransportHeli, "Unit_Veh_Helicopter_Transport"),
            (SkirmishRoleKind.AttackHeli, SkirmishRoleIds.AttackHeli, "Unit_Veh_Helicopter_Attack"),
            (SkirmishRoleKind.AttackHeliLight, SkirmishRoleIds.AttackHeliLight, "Unit_Veh_Helicopter_Attack_Small"),
        };
        static Entity s026HeliSession, s026HeliActor;
        static int s026HeliChoice = -1, s026HeliMoveAttempts, s026HeliPageTurns;
        static bool s026HeliPageForward = true;
        static double s026HeliCardDeadline;
        static float s026HeliWaitStartMatch = -1f;
        static double s026HeliMoveAt, s026HeliAirborneAt, s026HeliLandedAt, s026HeliLastStatusAt, s026HeliStageAt, s026HeliLandingTapAt, s026HeliLastHeadingAt;
        static Vector3 s026HeliMoveGoal, s026HeliPatch, s026HeliPatchTap;
        static float s026HeliHeadingAtTap, s026HeliMaxTurn, s026HeliLastHeading, s026HeliTouchdownDistance = float.NaN;
        static bool s026HeliTurnShot, s026HeliCruiseShot, s026HeliApproachShot, s026HeliLandingOrderSeen;
        static int s026HeliSettledShot;
        static uint s026InfantryGroup;
        static string s026HeliLastReason = "", s026HeliPatchDiagnostic = "";

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
            s026HeliChoice = -1; s026HeliMoveAttempts = 0; s026HeliPageTurns = 0;
            s026HeliPageForward = true; s026HeliCardDeadline = 0d;
            s026HeliWaitStartMatch = -1f; s026HeliMoveAt = s026HeliAirborneAt = s026HeliLandedAt = s026HeliLastStatusAt = 0d;
            s026HeliStageAt = s026HeliLandingTapAt = s026HeliLastHeadingAt = 0d;
            s026HeliMoveGoal = s026HeliPatch = s026HeliPatchTap = default;
            s026HeliHeadingAtTap = s026HeliMaxTurn = s026HeliLastHeading = 0f; s026HeliTouchdownDistance = float.NaN;
            s026HeliTurnShot = s026HeliCruiseShot = s026HeliApproachShot = s026HeliLandingOrderSeen = false; s026HeliSettledShot = 0; s026InfantryGroup = 0;
            s026HeliLastReason = ""; s026HeliPatchDiagnostic = "";
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
            SkirmishCameraFramingCapture.EndFlight();
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
                    S026NextStage(S026HeliStageBase + 5, now);
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
            var transformNow = em.GetComponentData<LocalTransform>(s026HeliActor);
            Vector3 heliPosition = transformNow.Position;
            float heading = S026Heading(transformNow);
            float toHome = math.distance(transformNow.Position.xz, air.HomePosition.xz);
            // 105: the selected helicopter's follow view must be live before the flight starts.
            if (helipadProbeStage == S026HeliStageBase + 5)
            {
                if (s026HeliStageAt <= 0d) s026HeliStageAt = now;
                var cameraOwner = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RtsCameraSystem>();
                if (cameraOwner != null && cameraOwner.TryReadAircraftFollowState(out var follow) && follow.Following && follow.Actor == s026HeliActor)
                {
                    Debug.Log("[S026HelicopterFraming] followApplied bounds=" + follow.BoundsSource + " distance=" + follow.Distance.ToString("0.0", CultureInfo.InvariantCulture));
                    SkirmishCameraFramingCapture.BeginFlight(s026HeliActor, null);
                    SkirmishCameraFramingCapture.RequestFlightShot("selected");
                    S026NextStage(S026HeliStageBase + 6, now);
                    return;
                }
                if (now - s026HeliStageAt > S026HeliFollowWaitSeconds)
                    EndS026HelicopterFraming(false, "aircraftFollowNotApplied waitedSeconds=" + S026HeliFollowWaitSeconds.ToString("0", CultureInfo.InvariantCulture) +
                        " lensContext=" + (cameraOwner != null && cameraOwner.HasAircraftFocusContext ? 1 : 0) +
                        " lastClear=" + (cameraOwner?.LastAircraftFocusClearReason ?? "none") + " " + S026HeliAirState(em, air, controls));
                return;
            }
            // 106: HUD Move, then a touch on visible open ground to the side of the helicopter
            // (a sideways leg keeps the home pad on screen behind it for the landing Move).
            if (helipadProbeStage == S026HeliStageBase + 6)
            {
                if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command) || command.ActiveCommandMode != TacticalCommandMode.Move)
                { TapHelipadButton(controls?.MoveButton, now); return; }
                if (!TryS026MoveGround(em, out var point, out var world3))
                { EndS026HelicopterFraming(false, "noVisibleOpenGroundForMove " + S026HeliAirState(em, air, controls)); return; }
                if (!helipadProbeTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
                { EndS026HelicopterFraming(false, "moveTouchRejected"); return; }
                Debug.Log("[S026HelicopterFraming] input=Touch command=Move leg=Outbound world=" + world3 + " point=" + point);
                s026HeliMoveAttempts++; s026HeliMoveAt = now; s026HeliMoveGoal = world3;
                SkirmishCameraFramingCapture.SetFlightStage("outbound-move", null);
                S026NextStage(S026HeliStageBase + 7, now);
                helipadProbeNext = now + 1d;
                return;
            }
            // 107: takeoff must follow the order.
            if (helipadProbeStage == S026HeliStageBase + 7)
            {
                if (air.Airborne == 0)
                {
                    if (now - s026HeliMoveAt < 15d) return;
                    if (s026HeliMoveAttempts >= 2) { EndS026HelicopterFraming(false, "moveOrderDidNotLaunchHelicopter " + S026HeliAirState(em, air, controls)); return; }
                    S026NextStage(S026HeliStageBase + 6, now);
                    return;
                }
                s026HeliAirborneAt = now;
                Debug.Log("[S026HelicopterFraming] airborne=1 " + S026HeliAirState(em, air, controls));
                SkirmishCameraFramingCapture.SetFlightStage("takeoff", null);
                SkirmishCameraFramingCapture.RequestFlightShot("takeoff");
                S026NextStage(S026HeliStageBase + 8, now);
                return;
            }
            // 108: outbound leg. Tap Move on the landing patch (the home helipad, where a Skirmish
            // helicopter lands) once it is clear of the pad, on screen and inside the safe band.
            if (helipadProbeStage == S026HeliStageBase + 8)
            {
                helipadProbeNext = now + .1d;
                bool outbound = em.HasComponent<UnitTarget>(s026HeliActor) && air.ReturningHome == 0;
                float toGoal = math.distance(transformNow.Position.xz, new float2(s026HeliMoveGoal.x, s026HeliMoveGoal.z));
                if (!outbound || toGoal < 10f)
                { EndS026HelicopterFraming(false, "landingPatchNotTappedBeforeOutboundArrival toGoal=" + toGoal.ToString("0.0", CultureInfo.InvariantCulture) + " lastPatch=[" + s026HeliPatchDiagnostic + "] " + S026HeliAirState(em, air, controls)); return; }
                if (now - s026HeliStageAt > 45d)
                { EndS026HelicopterFraming(false, "outboundLegTimeout " + S026HeliAirState(em, air, controls)); return; }
                // Arm Move during the climb-out so the patch tap itself is not delayed.
                if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command) || command.ActiveCommandMode != TacticalCommandMode.Move)
                { TapHelipadButton(controls?.MoveButton, now); return; }
                if (toHome < S026HeliLandingTapMinimumMetres) return;
                if (!TryS026PatchPoint(air.HomePosition, out var patchPoint, out var patchWorld)) return; // wait until it is visible
                if (!helipadProbeTouch.TryGesture(patchPoint, patchPoint, .15f, 0f, Time.unscaledTime))
                { EndS026HelicopterFraming(false, "landingMoveTouchRejected"); return; }
                s026HeliPatch = air.HomePosition; s026HeliPatchTap = patchWorld; s026HeliHeadingAtTap = heading; s026HeliMaxTurn = 0f;
                s026HeliLandingTapAt = now; s026HeliTurnShot = s026HeliCruiseShot = s026HeliApproachShot = s026HeliLandingOrderSeen = false; s026HeliLastHeading = heading; s026HeliLastHeadingAt = now;
                Debug.Log("[S026HelicopterFraming] input=Touch command=Move leg=LandingPatch patch=" + s026HeliPatch + " tapWorld=" + patchWorld +
                    " point=" + patchPoint + " headingAtTap=" + heading.ToString("0.0", CultureInfo.InvariantCulture) + " toHome=" + toHome.ToString("0.0", CultureInfo.InvariantCulture));
                SkirmishCameraFramingCapture.SetFlightStage("landing-move", s026HeliPatch);
                S026NextStage(S026HeliStageBase + 9, now);
                return;
            }
            // 109: the turn to the patch, cruise, approach and touchdown on the patch.
            if (helipadProbeStage == S026HeliStageBase + 9)
            {
                helipadProbeNext = now + .05d;
                float toPatch = math.distance(transformNow.Position.xz, new float2(s026HeliPatch.x, s026HeliPatch.z));
                // The landing must come from the player's Move on the patch, not the automatic return
                // after reaching the outbound goal: the live Move goal has to move onto the patch.
                if (!s026HeliLandingOrderSeen)
                {
                    if (em.HasComponent<UnitTarget>(s026HeliActor) && TryS026CellWorld(em, em.GetComponentData<UnitTarget>(s026HeliActor).Cell, out var goalWorld))
                    {
                        float goalToPatch = math.distance(new float2(goalWorld.x, goalWorld.z), new float2(s026HeliPatch.x, s026HeliPatch.z));
                        if (goalToPatch <= S026HeliTouchdownToleranceMetres + 4f)
                        { s026HeliLandingOrderSeen = true; Debug.Log("[S026HelicopterFraming] landingMoveAccepted goal=" + goalWorld + " goalToPatch=" + goalToPatch.ToString("0.0", CultureInfo.InvariantCulture)); }
                        else if (now - s026HeliLandingTapAt > 3d)
                        { EndS026HelicopterFraming(false, "landingMoveNotAccepted goal=" + goalWorld + " patch=" + s026HeliPatch + " " + S026HeliAirState(em, air, controls)); return; }
                    }
                    else if (air.ReturningHome != 0)
                    { EndS026HelicopterFraming(false, "landingByAutomaticReturnNotPlayerMove " + S026HeliAirState(em, air, controls)); return; }
                    else if (air.Airborne != 0 && toPatch <= S026HeliTouchdownToleranceMetres + 4f)
                    { s026HeliLandingOrderSeen = true; Debug.Log("[S026HelicopterFraming] landingMoveAccepted arrivedAtPatch=1"); }
                    else if (now - s026HeliLandingTapAt > 3d)
                    { EndS026HelicopterFraming(false, "landingMoveNotObserved " + S026HeliAirState(em, air, controls)); return; }
                }
                float turned = Mathf.Abs(Mathf.DeltaAngle(s026HeliHeadingAtTap, heading));
                if (air.Airborne != 0) s026HeliMaxTurn = Mathf.Max(s026HeliMaxTurn, turned);
                bool steady = Mathf.Abs(Mathf.DeltaAngle(s026HeliLastHeading, heading)) < 2f;
                if (now - s026HeliLastHeadingAt >= .5d) { s026HeliLastHeading = heading; s026HeliLastHeadingAt = now; }
                if (air.Airborne != 0 && !s026HeliTurnShot && turned >= 30f)
                { s026HeliTurnShot = true; SkirmishCameraFramingCapture.SetFlightStage("turn", s026HeliPatch); SkirmishCameraFramingCapture.RequestFlightShot("mid-turn"); return; }
                if (air.Airborne != 0 && s026HeliTurnShot && !s026HeliCruiseShot && s026HeliMaxTurn >= 60f && steady && toPatch > 20f)
                { s026HeliCruiseShot = true; SkirmishCameraFramingCapture.SetFlightStage("cruise", s026HeliPatch); SkirmishCameraFramingCapture.RequestFlightShot("cruise"); return; }
                if (air.Airborne != 0 && s026HeliTurnShot && !s026HeliApproachShot && toPatch <= S026HeliApproachMetres)
                { s026HeliApproachShot = true; SkirmishCameraFramingCapture.SetFlightStage("approach", s026HeliPatch); SkirmishCameraFramingCapture.RequestFlightShot("approach"); return; }
                if (air.Airborne == 0 && now - s026HeliLandingTapAt > 1d)
                {
                    if (!s026HeliLandingOrderSeen)
                    { EndS026HelicopterFraming(false, "touchdownWithoutAcceptedLandingMove " + S026HeliAirState(em, air, controls)); return; }
                    if (s026HeliLandedAt <= 0d)
                    {
                        s026HeliLandedAt = now; s026HeliTouchdownDistance = toPatch;
                        Debug.Log("[S026HelicopterFraming] touchdown=1 toPatch=" + toPatch.ToString("0.00", CultureInfo.InvariantCulture) +
                            " maxTurn=" + s026HeliMaxTurn.ToString("0.0", CultureInfo.InvariantCulture) + " " + S026HeliAirState(em, air, controls));
                        SkirmishCameraFramingCapture.SetFlightStage("touchdown", s026HeliPatch);
                        if (toPatch > S026HeliTouchdownToleranceMetres)
                        { EndS026HelicopterFraming(false, "touchdownAwayFromLandingPatch toPatch=" + toPatch.ToString("0.0", CultureInfo.InvariantCulture) + " " + S026HeliAirState(em, air, controls)); return; }
                    }
                    if (now - s026HeliLandedAt < 1.5d) return;
                    SkirmishCameraFramingCapture.RequestFlightShot("touchdown");
                    S026NextStage(S026HeliStageBase + 10, now);
                    return;
                }
                if (now - s026HeliLandingTapAt > 120d)
                    EndS026HelicopterFraming(false, "touchdownNotObserved toPatch=" + toPatch.ToString("0.0", CultureInfo.InvariantCulture) + " maxTurn=" +
                        s026HeliMaxTurn.ToString("0.0", CultureInfo.InvariantCulture) + " " + S026HeliAirState(em, air, controls));
                return;
            }
            // 110: switch back: tap an infantry squad card (a card tap adds to the selection), then the
            // helicopter's card to drop it, so only the ground squad is selected.
            if (helipadProbeStage == S026HeliStageBase + 10)
            {
                if (s026HeliStageAt <= 0d) s026HeliStageAt = now;
                if (s026InfantryGroup == 0 && !TryS026InfantryGroup(em, out s026InfantryGroup))
                { EndS026HelicopterFraming(false, "noLivingInfantrySquadForSwitchBack"); return; }
                bool heliSelected = em.HasComponent<SelectedUnitTag>(s026HeliActor);
                bool infantrySelected = S026GroupSelected(em, s026InfantryGroup);
                if (infantrySelected && !heliSelected)
                {
                    Debug.Log("[S026HelicopterFraming] switchBack=1 infantryGroup=" + s026InfantryGroup + " heliSelected=0");
                    SkirmishCameraFramingCapture.SetFlightStage("switch-back", null);
                    S026NextStage(S026HeliStageBase + 11, now);
                    helipadProbeNext = now + 1.5d;
                    return;
                }
                if (now - s026HeliStageAt > S026HeliCardWaitSeconds)
                { EndS026HelicopterFraming(false, "switchBackSelectionNotObserved infantrySelected=" + (infantrySelected ? 1 : 0) + " heliSelected=" + (heliSelected ? 1 : 0) + " infantryGroup=" + s026InfantryGroup); return; }
                bool pageRead = UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page);
                var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
                if (!pageRead || tray == null) return;
                uint heliGroup = em.HasComponent<SkirmishArmyGroupMembershipComponent>(s026HeliActor) ? em.GetComponentData<SkirmishArmyGroupMembershipComponent>(s026HeliActor).GroupId : 0u;
                if (!infantrySelected) S026TapGroupCardStep(page, s026InfantryGroup, tray, now, "infantry");
                else if (heliGroup != 0) S026TapGroupCardStep(page, heliGroup, tray, now, "helicopterOff");
                else EndS026HelicopterFraming(false, "helicopterGroupUnknownForDeselect");
                return;
            }
            // 111/112: frames after the switch-back, then the capture's fail-closed checks.
            if (helipadProbeStage == S026HeliStageBase + 11)
            {
                SkirmishCameraFramingCapture.RequestFlightShot("switch-back-ground");
                S026NextStage(S026HeliStageBase + 12, now);
                helipadProbeNext = now + 2d;
                return;
            }
            if (helipadProbeStage == S026HeliStageBase + 12)
            {
                if (s026HeliSettledShot == 0) { s026HeliSettledShot = 1; SkirmishCameraFramingCapture.RequestFlightShot("switch-back-settled"); helipadProbeNext = now + 1d; return; }
                bool passed = SkirmishCameraFramingCapture.TryEvaluateFlight(s026HeliMaxTurn, s026HeliTouchdownDistance, out string summary);
                EndS026HelicopterFraming(passed, (passed ? "playerDirectedFlightTurnTouchdownSwitchBack " : "flightAssertionsFailed ") + summary);
            }
        }

        static void S026NextStage(int stage, double now) { helipadProbeStage = stage; s026HeliStageAt = now; }

        static bool TryS026CellWorld(EntityManager em, int2 cell, out Vector3 world)
        {
            world = default;
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (grids.CalculateEntityCount() != 1) return false;
            world = GridUtils.CellToWorldCenter(grids.GetSingleton<GridConfig>(), cell);
            return true;
        }

        static float S026Heading(LocalTransform transform)
        {
            var forward = math.mul(transform.Rotation, new float3(0, 0, 1));
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        /// <summary>Screen point of the landing patch when it is on screen, inside the HUD-safe band and not under UI.</summary>
        static bool TryS026PatchPoint(Vector3 patch, out Vector2 point, out Vector3 world)
        {
            point = default; world = default;
            var camera = Camera.main;
            if (camera == null) return false;
            var screen = camera.WorldToScreenPoint(patch);
            s026HeliPatchDiagnostic = "screen=" + screen + " size=" + Screen.width + "x" + Screen.height;
            if (!(screen.z > camera.nearClipPlane)) return false;
            point = new Vector2(screen.x, screen.y);
            if (point.x < Screen.width * .08f || point.x > Screen.width * .92f || point.y < Screen.height * .2f || point.y > Screen.height * .85f) return false;
            if (!Screen.safeArea.Contains(point)) return false;
            var hits = new List<RaycastResult>();
            if (EventSystem.current != null)
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            if (hits.Count > 0) { s026HeliPatchDiagnostic += " uiHit=" + hits[0].gameObject.name; return false; }
            var ray = camera.ScreenPointToRay(point);
            if (!new Plane(Vector3.up, patch).Raycast(ray, out float distance)) return false;
            world = ray.GetPoint(distance);
            return true;
        }

        static bool TryS026InfantryGroup(EntityManager em, out uint group)
        {
            group = 0;
            var attempt = em.GetComponentData<SkirmishExpandedSessionComponent>(s026HeliSession).SessionId;
            var counts = new Dictionary<uint, int>();
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent),
                typeof(SkirmishArmyGroupMembershipComponent), typeof(UnitHealth));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                var own = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (own.FactionId != 1 || own.IsStructure != 0 || !own.SessionId.Equals(attempt) ||
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Category != SkirmishPopulationCategory.Infantry ||
                    em.GetComponentData<UnitHealth>(actor).Current <= 0) continue;
                uint id = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId;
                if (id == 0) continue;
                counts.TryGetValue(id, out int count); counts[id] = count + 1;
            }
            int best = 0;
            foreach (var pair in counts) if (pair.Value > best || pair.Value == best && pair.Key < group) { best = pair.Value; group = pair.Key; }
            return group != 0;
        }

        static bool S026GroupSelected(EntityManager em, uint group)
        {
            using var query = em.CreateEntityQuery(typeof(SelectedUnitTag), typeof(SkirmishArmyGroupMembershipComponent), typeof(UnitHealth));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
                if (em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId == group && em.GetComponentData<UnitHealth>(actor).Current > 0) return true;
            return false;
        }

        /// <summary>One normal touch toward tapping a group's squad card: on this page, anywhere in the tray roster, or a page turn.</summary>
        static void S026TapGroupCardStep(UiExpandedSquadPage page, uint group, MatchHudSquadTrayView tray, double now, string purpose)
        {
            int slot = SkirmishCameraFramingCapture.HelicopterCardSlot(page, group);
            if (slot >= 0)
            {
                if (TapHelipadSquadSlot(tray, slot, now))
                    Debug.Log("[S026HelicopterFraming] input=Touch squadCard purpose=" + purpose + " slot=" + slot + " group=" + group + " page=" + page.PageIndex);
                return;
            }
            var target = MatchHudSquadTouchNavigation.GroupTarget(tray, group);
            if (target.Available)
            {
                if (HelipadSquadInput(target, now, false))
                    Debug.Log("[S026HelicopterFraming] input=Touch squadCard purpose=" + purpose + " group=" + group + " kind=" + (target.Drag ? "TrayScroll" : "CardTap"));
                return;
            }
            if (page.PageCount <= 1) return;
            if (s026HeliPageForward && !page.NextPage) s026HeliPageForward = false;
            else if (!s026HeliPageForward && !page.PreviousPage) s026HeliPageForward = true;
            if (SwipeHelipadSquadPage(tray, s026HeliPageForward, now))
                Debug.Log("[S026HelicopterFraming] input=Touch squadPage purpose=" + purpose + " forward=" + (s026HeliPageForward ? 1 : 0) + " page=" + page.PageIndex + "/" + page.PageCount);
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

        /// <summary>
        /// Visible open ground for the outbound leg: at least 30 m out and preferably to the side
        /// of the view, so the home pad stays on screen behind the helicopter for the landing Move.
        /// </summary>
        static bool TryS026MoveGround(EntityManager em, out Vector2 point, out Vector3 world)
        {
            point = default; world = default;
            var camera = Camera.main;
            if (camera == null) return false;
            Vector3 from = em.GetComponentData<LocalTransform>(s026HeliActor).Position;
            var ground = new Plane(Vector3.up, new Vector3(0f, from.y, 0f));
            var right = camera.transform.right; right.y = 0f;
            if (right.sqrMagnitude < 1e-6f) return false;
            right.Normalize();
            float best = 0f;
            foreach (var v in new[] { new Vector2(.88f, .40f), new Vector2(.22f, .40f), new Vector2(.80f, .45f), new Vector2(.20f, .45f),
                new Vector2(.86f, .30f), new Vector2(.75f, .60f), new Vector2(.25f, .60f), new Vector2(.5f, .72f), new Vector2(.5f, .38f) })
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
                var flat = new Vector3(hit.x - from.x, 0f, hit.z - from.z);
                float reach = flat.magnitude;
                if (reach < S026HeliOutboundMinimumMetres) continue;
                float score = reach * (.25f + Mathf.Abs(Vector3.Dot(flat / reach, right)));
                if (score <= best) continue;
                best = score; point = candidate; world = hit;
            }
            return best > 0f;
        }

        /// <summary>Read-only diagnostics of the probed helicopter's native air state.</summary>
        static string S026HeliAirState(EntityManager em, UnitAirComponent air, MatchOverlayCommandControlsView controls)
        {
            var position = em.GetComponentData<LocalTransform>(s026HeliActor).Position;
            return "airborne=" + air.Airborne + " returningHome=" + air.ReturningHome + " homeInitialized=" + air.HomeInitialized +
                " usesRunway=" + air.UsesRunway + " moveTarget=" + (em.HasComponent<UnitTarget>(s026HeliActor) ? 1 : 0) +
                " manualMove=" + (em.HasComponent<ManualMoveOrderTag>(s026HeliActor) ? 1 : 0) +
                " selected=" + (em.HasComponent<SelectedUnitTag>(s026HeliActor) ? 1 : 0) +
                " height=" + position.y.ToString("0.0", CultureInfo.InvariantCulture) +
                " toHome=" + math.distance(position, air.HomePosition).ToString("0.0", CultureInfo.InvariantCulture) +
                " wheelOpen=" + (controls != null && controls.CommandWheelPanel != null && controls.CommandWheelPanel.IsOpen ? 1 : 0);
        }
    }
}
#endif
