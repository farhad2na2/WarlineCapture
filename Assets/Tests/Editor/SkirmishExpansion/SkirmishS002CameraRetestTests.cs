using System.Collections.Generic;
using Game.Configs;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// S002 designer retest 2026-11-18 (Design/Orchestration/owner_packets/2026-11-18-s002-designer-retest.md):
    /// opening frame and alert-tap framing math. Fixture positions come from Unity evidence:
    /// the live Barracks cell (1001, 303) and the Battle Tank firing cell (1001, 278) in
    /// /private/tmp/warline-s002-review-20261118.log, the opening Rifle group centroid
    /// (983, 318.5) from the seed 130365 actor trace, and the play areas logged by
    /// [SkirmishHudChipPlacement] at 1920x1080. Native captures, normal-input play and phone
    /// acceptance remain separate gates.
    /// </summary>
    public sealed class SkirmishS002CameraRetestTests
    {
        static readonly Vector3 Home = new(1001.5f, 0f, 303.5f);       // live Barracks
        static readonly Vector3 RifleGroup = new(983f, 0f, 318.5f);    // opening Rifle group centroid
        static readonly Vector3 TankFiring = new(1001.1f, 0f, 277.9f); // tank stops here and hits the base at ~68 s
        static readonly float[] Aspects = { 1920f / 1080f, 2400f / 1080f };
        const float MobileCap = 40f, Headroom = 1.5f;
        static readonly Rect Screen1920 = new(0, 0, 1920, 1080);
        // [SkirmishHudChipPlacement] playArea lines from the designer retest (pixels, y up).
        static readonly Rect CalmPlayArea1920 = Rect.MinMaxRect(457, 241, 1444, 855);
        static readonly Rect AlertPlayArea1920 = Rect.MinMaxRect(437, 241, 1920, 611);

        static SkirmishMapLayoutConfig Layout()
        {
            var layout = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>("Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishLayout_S002.asset");
            Assert.IsNotNull(layout);
            return layout;
        }

        static SkirmishMissionViewProfile Profile()
        {
            var profile = SkirmishMissionViewProfile.Load("S002");
            Assert.IsNotNull(profile, "S002 mission view profile");
            return profile;
        }

        static Vector2[] OpeningPoints(SkirmishOpeningCameraConfig config, SkirmishMapLayoutConfig layout)
        {
            Assert.IsTrue(config.TryResolveFramePoints(layout, true, new Vector2(Home.x, Home.z), out var points));
            return points;
        }

        static Vector3[] World(Vector3 group, Vector2[] landmarks)
        {
            var result = new Vector3[landmarks.Length + 1];
            result[0] = group;
            for (int i = 0; i < landmarks.Length; i++) result[i + 1] = new Vector3(landmarks[i].x, group.y, landmarks[i].y);
            return result;
        }

        static Rect Viewport(Rect pixels)
        {
            Assert.IsTrue(SkirmishCameraFraming.TryPixelsToViewport(pixels, Screen1920, out var viewport));
            return viewport;
        }

        static Vector2 View(in SkirmishGroundFraming framing, Vector3 eye, float aspect, Vector3 point)
        {
            Assert.IsTrue(SkirmishCameraFraming.TryViewport(eye, framing.Pitch, framing.Yaw, framing.FieldOfView, aspect, point, out var viewport));
            return viewport;
        }

        static void AssertInside(Vector2 viewport, Rect area, string what)
        {
            const float tolerance = .006f;
            Assert.That(viewport.x, Is.InRange(area.xMin - tolerance, area.xMax + tolerance), what + " u");
            Assert.That(viewport.y, Is.InRange(area.yMin - tolerance, area.yMax + tolerance), what + " v");
        }

        [Test] public void OpeningConfigFramesTheLiveBaseAndTheTankRoad()
        {
            var config = SkirmishOpeningCameraConfig.Load("S002");
            var layout = Layout();
            Assert.IsTrue(config.Matches("S002", layout));
            Assert.IsTrue(config.UseFixedYaw); Assert.IsTrue(config.FrameActualPlayerBase); Assert.IsTrue(config.FrameInsideHudPlayArea);
            Assert.AreEqual(SkirmishCameraFraming.OpeningFitViewport, config.OpeningViewport);
            Assert.IsTrue(config.TryResolveYaw(layout, out float yaw)); Assert.AreEqual(65f, yaw, .001f);
            Assert.IsFalse(config.TryResolveFramePoints(layout, false, default, out _), "No live base: fall back, never frame the anchor");
            var points = OpeningPoints(config, layout);
            Assert.AreEqual(2, points.Length);
            Assert.AreEqual(new Vector2(Home.x, Home.z), points[0]);
            // supply.expansion.a projects to (1156.1, 190.0), on the road the tank drives in on.
            Assert.IsTrue(layout.TryGetAnchor("supply.expansion.a", out var anchor));
            Assert.IsTrue(layout.TryProjectLocalToMap(anchor.WorldX, anchor.WorldZ, out float x, out float z));
            Assert.Less(Vector2.Distance(new Vector2(x, z), new Vector2(1156.1f, 190f)), .5f);
            var toward = (new Vector2(x, z) - points[0]).normalized;
            Assert.AreEqual(15f, Vector2.Distance(points[1], points[0]), .01f);
            Assert.Greater(Vector2.Dot((points[1] - points[0]).normalized, toward), .9999f);
            // The base.player anchor is not where the Barracks stands.
            Assert.IsTrue(layout.TryGetAnchor("base.player", out var playerAnchor));
            Assert.IsTrue(layout.TryProjectLocalToMap(playerAnchor.WorldX, playerAnchor.WorldZ, out float ax, out float az));
            Assert.Greater(Vector2.Distance(new Vector2(ax, az), new Vector2(Home.x, Home.z)), 60f);
        }

        [Test] public void OpeningShowsBaseSquadAndApproachInsideThePlayArea()
        {
            var config = SkirmishOpeningCameraConfig.Load("S002");
            var layout = Layout();
            Assert.IsTrue(config.TryResolveYaw(layout, out float yaw));
            var framing = SkirmishCameraFraming.Resolve(Profile(), yaw);
            var points = OpeningPoints(config, layout);
            foreach (float aspect in Aspects)
            {
                Assert.IsTrue(SkirmishCameraFraming.TryComposeOpening(framing, RifleGroup, points, aspect, config.OpeningViewport, out var eye, out bool all));
                Assert.IsTrue(all, "aspect " + aspect);
                Assert.LessOrEqual(eye.y, MobileCap - Headroom, "Fits under the mobile cap without a platform clamp");
                Assert.GreaterOrEqual(eye.y, framing.TacticalHeight - .01f);
                var play = SkirmishCameraFraming.DefaultHudPlayViewport;
                var home = View(framing, eye, aspect, Home);
                var squad = View(framing, eye, aspect, RifleGroup);
                var approach = View(framing, eye, aspect, new Vector3(points[1].x, 0f, points[1].y));
                AssertInside(home, play, "base"); AssertInside(squad, play, "squad"); AssertInside(approach, play, "approach");
                // The tank's firing cell sits right of the base at the screen's right edge (the side trouble comes from).
                var tank = View(framing, eye, aspect, TankFiring);
                Assert.That(tank.x, Is.InRange(home.x, 1.05f)); Assert.That(tank.y, Is.InRange(0f, home.y));
                Debug.Log("[SkirmishS002CameraRetestTests] opening aspect=" + aspect.ToString("F3") + " yaw=" + yaw + " height=" + eye.y.ToString("F2") +
                    " base=" + home.ToString("F3") + " squad=" + squad.ToString("F3") + " approach=" + approach.ToString("F3") + " tank=" + tank.ToString("F3") +
                    " groundRequest=" + SkirmishCameraFraming.ZeroPlaneGroundCenter(eye, framing.Pitch, framing.Yaw).ToString("F2"));
            }
        }

        [Test] public void LegacyOpeningLeftTheBaseOffTheTopAndTheAnchorUnderTheCards()
        {
            // The merged 3b3a3cf14 opening: yaw 107.8, base.player anchor, legacy band.
            var framing = SkirmishCameraFraming.Resolve(Profile(), 107.8f);
            var group = new Vector3(983f, -.08f, 318.5f);
            var anchor = new Vector2(949f, 344.7f);
            Assert.IsTrue(SkirmishCameraFraming.TryComposeOpening(framing, group, new[] { anchor }, 16f / 9f, out var eye, out _));
            Assert.AreEqual(31.94f, eye.y, .6f, "Near the logged opening height (31.94)");
            Assert.Less(View(framing, eye, 16f / 9f, new Vector3(anchor.x, group.y, anchor.y)).y, SkirmishCameraFraming.HudPlayMinV, "Anchor under the squad cards");
            Assert.Greater(View(framing, eye, 16f / 9f, Home).y, 1f, "Live Barracks above the frame");
        }

        [Test] public void AuthoredYaw65FitsOpeningAndAlertWithMobileHeadroom()
        {
            var config = SkirmishOpeningCameraConfig.Load("S002");
            var layout = Layout();
            var profile = Profile();
            var points = World(RifleGroup, OpeningPoints(config, layout));
            var alert = SkirmishCameraFraming.Inset(Viewport(AlertPlayArea1920), SkirmishCameraFraming.FitMarginFraction);
            float limit = MobileCap - Headroom;
            bool Fits(float yaw)
            {
                var framing = SkirmishCameraFraming.Resolve(profile, yaw);
                foreach (float aspect in Aspects)
                {
                    if (!SkirmishCameraFraming.TryFitLowest(framing, points, aspect, config.OpeningViewport, framing.TacticalHeight, limit, out _, out bool opening) || !opening) return false;
                    if (!SkirmishCameraFraming.TryFitLowest(framing, new[] { Home, TankFiring }, aspect, alert, framing.TacticalHeight, limit, out _, out bool both) || !both) return false;
                }
                return true;
            }
            Assert.IsTrue(config.TryResolveYaw(layout, out float yaw));
            Assert.AreEqual(65f, yaw, .001f);
            Assert.IsTrue(Fits(yaw), "Authored yaw fits both landscape aspects with headroom");
            Assert.IsTrue(Fits(60f), "The measured fixtures also fit at 60 degrees");
            // North-up fits the opening alone; the alert pair is what needs the turn.
            var north = SkirmishCameraFraming.Resolve(profile, 0f);
            Assert.IsTrue(SkirmishCameraFraming.TryFitLowest(north, points, 16f / 9f, config.OpeningViewport, north.TacticalHeight, limit, out _, out bool northOpening) && northOpening);
            Assert.IsFalse(Fits(0f), "North-up does not fit both views with headroom");
        }

        [Test] public void DefaultPlayAreaLiesInsideEveryMeasuredHudLayout()
        {
            var play = SkirmishCameraFraming.DefaultHudPlayViewport;
            var calm = Viewport(CalmPlayArea1920);
            Assert.IsTrue(calm.xMin <= play.xMin && calm.yMin <= play.yMin && calm.xMax >= play.xMax && calm.yMax >= play.yMax, "1920 " + calm);
            // S026 2400x1080 chip layout (SkirmishHudChipPlacementTests), top-left pixels.
            var screen = new Rect(0, 0, 2400, 1080);
            Rect Top(float l, float t, float r, float b) => Rect.MinMaxRect(l, 1080 - b, r, 1080 - t);
            var hud = new List<Rect> { Top(18, 12, 454, 468), Top(18, 486, 384, 734), Top(718, 12, 1418, 92), Top(1438, 18, 1602, 92),
                Top(722, 112, 1752, 222), Top(790, 234, 1610, 298), Top(1926, 12, 2382, 578), Top(18, 840, 816, 1062), Top(834, 888, 2382, 1062) };
            Assert.IsTrue(SkirmishHudChipPlacement.TrySafePlayArea(screen, hud, screen.center, out var pixels));
            Assert.IsTrue(SkirmishCameraFraming.TryPixelsToViewport(pixels, screen, out var s026));
            Assert.IsTrue(s026.xMin <= play.xMin && s026.yMin <= play.yMin && s026.xMax >= play.xMax && s026.yMax >= play.yMax, "2400 " + s026);
            Assert.IsFalse(SkirmishCameraFraming.TryPixelsToViewport(pixels, new Rect(0, 0, 0, 1080), out _));
        }

        [Test] public void AlertTapFramesBaseAndTankInsideTheLiveAlertPlayArea()
        {
            var framing = SkirmishCameraFraming.Resolve(Profile(), 65f);
            var play = Viewport(AlertPlayArea1920);
            foreach (float aspect in Aspects)
            {
                Assert.IsTrue(SkirmishCameraFraming.TryFrameHomeThreat(framing, Home, true, TankFiring, aspect, play, MobileCap, out var eye, out var kind, out float corridor));
                Assert.AreEqual(SkirmishBaseFocusKind.HomeAndThreat, kind); Assert.AreEqual(1f, corridor);
                Assert.That(eye.y, Is.InRange(framing.TacticalHeight - .01f, MobileCap + .01f));
                var fit = SkirmishCameraFraming.Inset(play, SkirmishCameraFraming.FitMarginFraction);
                var home = View(framing, eye, aspect, Home); var tank = View(framing, eye, aspect, TankFiring);
                AssertInside(home, fit, "base"); AssertInside(tank, fit, "tank");
                // Deterministic: the same facts give the same frame (no drift between taps).
                Assert.IsTrue(SkirmishCameraFraming.TryFrameHomeThreat(framing, Home, true, TankFiring, aspect, play, MobileCap, out var again, out _, out _));
                Assert.Less(Vector3.Distance(eye, again), .001f);
                Debug.Log("[SkirmishS002CameraRetestTests] alert aspect=" + aspect.ToString("F3") + " kind=" + kind + " height=" + eye.y.ToString("F2") +
                    " base=" + home.ToString("F3") + " tank=" + tank.ToString("F3") + " groundRequest=" + SkirmishCameraFraming.ZeroPlaneGroundCenter(eye, framing.Pitch, framing.Yaw).ToString("F2"));
            }
        }

        [Test] public void FarAttackerStaysInThePlayAreaWithTheWayBackTowardBase()
        {
            // An attacker beyond what the widest view can pair with the base (north-up, 85 m away).
            var framing = SkirmishCameraFraming.Resolve(Profile(), 0f);
            var play = Viewport(AlertPlayArea1920);
            var far = new Vector3(Home.x + 60f, 0f, Home.z - 60f);
            foreach (float aspect in Aspects)
            {
                Assert.IsTrue(SkirmishCameraFraming.TryFrameHomeThreat(framing, Home, true, far, aspect, play, MobileCap, out var eye, out var kind, out float corridor));
                Assert.AreEqual(SkirmishBaseFocusKind.ThreatTowardHome, kind);
                Assert.That(corridor, Is.InRange(.05f, .99f));
                Assert.AreEqual(Mathf.Min(framing.WideHeight, MobileCap), eye.y, .01f, "Widest allowed view");
                var fit = SkirmishCameraFraming.Inset(play, SkirmishCameraFraming.FitMarginFraction);
                var attacker = View(framing, eye, aspect, far);
                var reach = View(framing, eye, aspect, Vector3.Lerp(far, Home, corridor));
                AssertInside(attacker, fit, "attacker"); AssertInside(reach, fit, "way back toward base");
                var home = View(framing, eye, aspect, Home);
                Assert.Greater(Vector2.Dot((home - attacker).normalized, (reach - attacker).normalized), .99f, "Base lies along the framed way");
            }
        }

        [Test] public void HiddenAttackerFramesOnlyTheStruckSide()
        {
            var source = new Vector3(Home.x - 70f, 0f, Home.z + 50f); // fogged: never framed itself
            Assert.IsTrue(SkirmishCameraFraming.TryDamagedSide(Home, source, SkirmishCameraFraming.DamagedSideMetres, out var side));
            Assert.AreEqual(SkirmishCameraFraming.DamagedSideMetres, Vector3.Distance(side, Home), .001f);
            Assert.Greater(Vector3.Distance(side, source), 60f);
            Assert.Greater(Vector3.Dot((side - Home).normalized, (source - Home).normalized), .9999f);
            Assert.IsFalse(SkirmishCameraFraming.TryDamagedSide(Home, Home, 18f, out _));
            var framing = SkirmishCameraFraming.Resolve(Profile(), 65f);
            var play = Viewport(AlertPlayArea1920);
            foreach (float aspect in Aspects)
            {
                Assert.IsTrue(SkirmishCameraFraming.TryFrameHomeThreat(framing, Home, true, side, aspect, play, MobileCap, out var eye, out var kind, out _));
                Assert.AreEqual(SkirmishBaseFocusKind.HomeAndThreat, kind);
                var fit = SkirmishCameraFraming.Inset(play, SkirmishCameraFraming.FitMarginFraction);
                AssertInside(View(framing, eye, aspect, Home), fit, "base"); AssertInside(View(framing, eye, aspect, side), fit, "struck side");
            }
        }

        [Test] public void CalmBaseTapCentresTheBaseInThePlayArea()
        {
            var framing = SkirmishCameraFraming.Resolve(Profile(), 65f);
            var play = Viewport(CalmPlayArea1920);
            foreach (float aspect in Aspects)
            {
                Assert.IsTrue(SkirmishCameraFraming.TryFrameHomeThreat(framing, Home, false, default, aspect, play, MobileCap, out var eye, out var kind, out _));
                Assert.AreEqual(SkirmishBaseFocusKind.Home, kind);
                Assert.AreEqual(Mathf.Min(framing.WideHeight, MobileCap), eye.y, .01f);
                var home = View(framing, eye, aspect, Home);
                Assert.Less(Vector2.Distance(home, play.center), .01f);
                // An attacker standing on the base is treated as no separate threat.
                Assert.IsTrue(SkirmishCameraFraming.TryFrameHomeThreat(framing, Home, true, Home + new Vector3(.2f, 0, .2f), aspect, play, MobileCap, out _, out kind, out _));
                Assert.AreEqual(SkirmishBaseFocusKind.Home, kind);
            }
            Assert.IsFalse(SkirmishCameraFraming.TryFrameHomeThreat(framing, Home, false, default, 16f / 9f, new Rect(0, 0, 0, 0), MobileCap, out _, out _, out _));
        }

        public static void RunFocusedValidation()
        {
            var tests = new SkirmishS002CameraRetestTests();
            tests.OpeningConfigFramesTheLiveBaseAndTheTankRoad();
            tests.OpeningShowsBaseSquadAndApproachInsideThePlayArea();
            tests.LegacyOpeningLeftTheBaseOffTheTopAndTheAnchorUnderTheCards();
            tests.AuthoredYaw65FitsOpeningAndAlertWithMobileHeadroom();
            tests.DefaultPlayAreaLiesInsideEveryMeasuredHudLayout();
            tests.AlertTapFramesBaseAndTankInsideTheLiveAlertPlayArea();
            tests.FarAttackerStaysInThePlayAreaWithTheWayBackTowardBase();
            tests.HiddenAttackerFramesOnlyTheStruckSide();
            tests.CalmBaseTapCentresTheBaseInThePlayArea();
            SkirmishCameraFramingTests.RunFocusedValidation(); // its own marker: [SkirmishCameraFramingTests] result=Passed
            Debug.Log("[SkirmishS002CameraRetestTests] result=Passed tests=9 framingTests=10 nativeCaptureReview=Pending normalInputPlay=Pending phoneAcceptance=Pending");
        }
    }
}
