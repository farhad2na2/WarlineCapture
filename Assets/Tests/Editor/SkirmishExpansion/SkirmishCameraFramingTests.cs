using Game.Configs;
using Game.Editor;
using Game.UI.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Pure framing math and the shipped opening data. Native pictures, normal-input
    // play and phone acceptance remain separate gates (capture plan in STATUS).
    public sealed class SkirmishCameraFramingTests
    {
        const float Ground = -5.31082f;
        static readonly float[] LandscapeAspects = { 2400f / 1080f, 1920f / 1080f };

        static SkirmishMissionViewProfile Profile(float min, float normal, float max, float pitch = 65f, float yaw = 35f)
        {
            var profile = ScriptableObject.CreateInstance<SkirmishMissionViewProfile>();
            profile.GroundReferenceY = Ground; profile.MinHeight = min; profile.NormalHeight = normal; profile.MaxHeight = max;
            profile.BuildHeight = max; profile.Pitch = pitch; profile.Yaw = yaw; profile.NormalFieldOfView = 46; profile.BuildFieldOfView = 50;
            return profile;
        }

        [Test] public void GroundFramingIsWiderThanTheAuthoredFloorAndKeepsCloseZoom()
        {
            var profile = Profile(Ground + 12, Ground + 18, Ground + 20); // the S026 engineering view
            try
            {
                var framing = SkirmishCameraFraming.Resolve(profile);
                Assert.IsTrue(framing.IsValid);
                Assert.AreEqual(profile.MinHeight, framing.CloseHeight, .001f, "Close/select zoom keeps the authored minimum");
                Assert.AreEqual(Ground + SkirmishCameraFraming.TacticalHeightAboveGround, framing.TacticalHeight, .001f);
                Assert.Greater(framing.WideHeight, framing.TacticalHeight);
                Assert.Greater(framing.TacticalHeight, profile.MaxHeight, "Default is wider than the old maximum");
                Assert.AreEqual(SkirmishCameraFraming.TacticalPitch, framing.Pitch);
                Assert.AreEqual(35f, framing.Yaw); Assert.IsFalse(framing.HasMissionYaw);
                float slant = (framing.WideHeight - Ground) / Mathf.Sin(framing.Pitch * Mathf.Deg2Rad);
                Assert.LessOrEqual(slant, SkirmishCameraFraming.MaximumCenterSlantDistance + .001f, "Centre line stays inside the 64 m enemy impostor distance");
                Assert.That(framing.BuildHeight, Is.InRange(framing.TacticalHeight, framing.WideHeight));
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test] public void GroundFramingNeverMovesCloserThanAnAlreadyWideProfile()
        {
            var profile = Profile(Ground + 15, Ground + 44, Ground + 46, 55);
            try
            {
                var framing = SkirmishCameraFraming.Resolve(profile, 200f);
                Assert.AreEqual(profile.NormalHeight, framing.TacticalHeight, .001f);
                Assert.GreaterOrEqual(framing.WideHeight, profile.MaxHeight);
                Assert.AreEqual(200f, framing.Yaw); Assert.IsTrue(framing.HasMissionYaw);
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test] public void TacticalViewKeepsInfantryTapTargetsAtLandscapePhoneSize()
        {
            var profile = Profile(Ground + 12, Ground + 18, Ground + 20);
            try
            {
                var framing = SkirmishCameraFraming.Resolve(profile);
                // A 1.8 m soldier at the screen centre of a 1080-pixel-high landscape phone.
                float slant = (framing.TacticalHeight - Ground) / Mathf.Sin(framing.Pitch * Mathf.Deg2Rad);
                float pixels = 1080f * 1.8f / (2f * slant * Mathf.Tan(framing.FieldOfView * .5f * Mathf.Deg2Rad));
                Assert.GreaterOrEqual(pixels, 44f, "Default view soldier height in pixels");
                float closeSlant = (framing.CloseHeight - Ground) / Mathf.Sin(framing.Pitch * Mathf.Deg2Rad);
                Assert.Greater(1080f * 1.8f / (2f * closeSlant * Mathf.Tan(framing.FieldOfView * .5f * Mathf.Deg2Rad)), pixels * 2f);
            }
            finally { Object.DestroyImmediate(profile); }
        }

        static SkirmishMapLayoutConfig Layout(string path)
        {
            var layout = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(path);
            Assert.IsNotNull(layout, path);
            return layout;
        }

        [Test] public void S026OpeningFramesGroupBarracksAndJunctionLookingDownTheAvenue()
        {
            var config = SkirmishOpeningCameraConfig.Load("S026");
            Assert.IsNotNull(config);
            var layout = Layout("Assets/Game/Configs/SkirmishExpansion/Scenarios/S026/SkirmishLayout_S026.asset");
            Assert.IsTrue(config.Matches("S026", layout), "Opening data is pinned to the exact S026 layout");
            Assert.IsFalse(config.Matches("S025", layout));
            Assert.IsTrue(config.TryResolveYaw(layout, out float yaw)); Assert.AreEqual(180f, yaw, .5f);
            Assert.IsTrue(config.TryResolveFramePoints(layout, out var points)); Assert.AreEqual(2, points.Length);
            Assert.Less(Vector2.Distance(points[0], new Vector2(962f, 723.5f)), .5f, "Player barracks anchor");
            Assert.Less(Vector2.Distance(points[1], new Vector2(982f, 692f)), .01f, "Measured road junction");
            var profile = Profile(Ground + 12, Ground + 18, Ground + 20);
            try
            {
                var framing = SkirmishCameraFraming.Resolve(profile, yaw);
                var group = new Vector3(1012f, Ground + 1f, 750f);
                foreach (float aspect in LandscapeAspects)
                {
                    Assert.IsTrue(SkirmishCameraFraming.TryComposeOpening(framing, group, points, aspect, out var eye, out bool all));
                    Assert.IsTrue(all, "aspect " + aspect);
                    Assert.That(eye.y, Is.InRange(framing.TacticalHeight - .01f, framing.WideHeight + .01f));
                    Assert.IsTrue(SkirmishCameraFraming.TryViewport(eye, framing.Pitch, framing.Yaw, framing.FieldOfView, aspect, group, out var groupViewport));
                    Assert.Less(groupViewport.y, .4f, "Own group sits at the near edge");
                    foreach (var point in points)
                    {
                        Assert.IsTrue(SkirmishCameraFraming.TryViewport(eye, framing.Pitch, framing.Yaw, framing.FieldOfView, aspect, new Vector3(point.x, group.y, point.y), out var viewport));
                        Assert.That(viewport.x, Is.InRange(SkirmishCameraFraming.SafeMinU - .01f, SkirmishCameraFraming.SafeMaxU + .01f));
                        Assert.That(viewport.y, Is.InRange(SkirmishCameraFraming.SafeMinV - .01f, SkirmishCameraFraming.SafeMaxV + .01f));
                    }
                    Assert.IsTrue(SkirmishCameraFraming.TryViewport(eye, framing.Pitch, framing.Yaw, framing.FieldOfView, aspect, new Vector3(982f, group.y, 692f), out var junction));
                    Assert.Greater(junction.y, groupViewport.y + .3f, "Junction and the avenue beyond it lie up-screen");
                    var centre = SkirmishCameraFraming.ZeroPlaneGroundCenter(eye, framing.Pitch, framing.Yaw);
                    Assert.AreEqual(0f, centre.y);
                    var back = SkirmishCameraFraming.CameraForGroundCenter(centre, eye.y, framing.Pitch, framing.Yaw);
                    Assert.Less(Vector3.Distance(back, eye), .01f, "Request round-trips through the camera's y=0 ground centre");
                }
            }
            finally { Object.DestroyImmediate(profile); }
        }

        // Re-pinned 2026-11-18 (designer retest): yaw 107.8 toward highway.near and the base.player
        // anchor (949, 345) became yaw 65, the live Barracks (cell 1001, 303) and an approach point
        // toward supply.expansion.a, composed in the HUD-free play area. Full checks:
        // SkirmishS002CameraRetestTests.
        [Test] public void S002OpeningFramesTheLiveBaseSquadAndApproach()
        {
            var config = SkirmishOpeningCameraConfig.Load("S002");
            Assert.IsNotNull(config);
            var layout = Layout("Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishLayout_S002.asset");
            Assert.IsTrue(config.Matches("S002", layout));
            Assert.IsTrue(config.TryResolveYaw(layout, out float yaw)); Assert.AreEqual(65f, yaw, .01f);
            Assert.IsFalse(config.TryResolveFramePoints(layout, out _), "Without the live base the opening falls back instead of framing an anchor");
            var home = new Vector2(1001.5f, 303.5f);
            Assert.IsTrue(config.TryResolveFramePoints(layout, true, home, out var points)); Assert.AreEqual(2, points.Length);
            Assert.Less(Vector2.Distance(points[0], home), .001f, "Live base");
            Assert.AreEqual(15f, Vector2.Distance(points[1], home), .01f, "Approach point");
            var profile = Profile(12, 20, 20, 65, 307);
            try
            {
                var framing = SkirmishCameraFraming.Resolve(profile, yaw);
                foreach (float aspect in LandscapeAspects)
                {
                    Assert.IsTrue(SkirmishCameraFraming.TryComposeOpening(framing, new Vector3(983f, 0f, 318.5f), points, aspect, config.OpeningViewport, out var eye, out bool all));
                    Assert.IsTrue(all); Assert.That(eye.y, Is.InRange(framing.TacticalHeight - .01f, framing.WideHeight + .01f));
                }
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test] public void HelicopterViewShowsWholeAircraftGroundAndHeadingFromAbove()
        {
            var helicopter = new Bounds(new Vector3(500f, 24f, 300f), new Vector3(14f, 5f, 16f)); // rotor disc included
            foreach (float aspect in LandscapeAspects)
            foreach (bool airborne in new[] { false, true })
            {
                var heading = new Vector2(.6f, -.8f);
                Assert.IsTrue(SkirmishCameraFraming.TryAircraftPose(helicopter, Ground, heading, airborne, 50f, 180f, 60f,
                    SkirmishCameraFraming.AircraftMaximumViewportFraction, aspect, .3f, 1f, 300f, out var position, out var rotation, out var lookAt));
                var euler = new Vector3(50f, 180f, 0f); // 50 >= fov/2 + margin, so the authored pitch is kept
                Assert.Less(Quaternion.Angle(rotation, Quaternion.Euler(euler)), .01f);
                Assert.Greater(euler.x - 30f, 0f, "Top frustum edge still descends: no underside frames");
                Assert.GreaterOrEqual(position.y, helicopter.max.y + SkirmishCameraFraming.AircraftClearanceAboveTop - .01f);
                Assert.GreaterOrEqual(Vector3.Distance(position, lookAt), SkirmishCameraFraming.AircraftMinimumDistance - .01f);
                var min = new Vector2(1, 1); var max = new Vector2(0, 0);
                for (int i = 0; i < 8; i++)
                {
                    var corner = helicopter.center + Vector3.Scale(helicopter.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Assert.IsTrue(SkirmishCameraFraming.TryViewport(position, euler.x, euler.y, 60f, aspect, corner, out var viewport));
                    min = Vector2.Min(min, viewport); max = Vector2.Max(max, viewport);
                }
                Assert.Greater(min.x, 0f); Assert.Less(max.x, 1f); Assert.Greater(min.y, 0f); Assert.Less(max.y, 1f);
                Assert.LessOrEqual(Mathf.Max(max.x - min.x, max.y - min.y), SkirmishCameraFraming.AircraftMaximumViewportFraction + .02f, "Aircraft leaves room for context");
                var below = new Vector3(helicopter.center.x, Mathf.Max(Ground, helicopter.min.y - SkirmishCameraFraming.AircraftMaximumGroundDrop), helicopter.center.z);
                Assert.IsTrue(SkirmishCameraFraming.TryViewport(position, euler.x, euler.y, 60f, aspect, below, out var ground));
                Assert.That(ground.x, Is.InRange(0f, 1f)); Assert.That(ground.y, Is.InRange(0f, 1f), "Ground under the aircraft is in frame");
                if (airborne)
                {
                    var ahead = below + new Vector3(heading.x, 0, heading.y).normalized * SkirmishCameraFraming.AircraftLeadMetres;
                    Assert.IsTrue(SkirmishCameraFraming.TryViewport(position, euler.x, euler.y, 60f, aspect, ahead, out var lead));
                    Assert.That(lead.x, Is.InRange(0f, 1f)); Assert.That(lead.y, Is.InRange(0f, 1f), "Direction of travel is in frame");
                }
            }
        }

        [Test] public void AircraftViewRefusesInsteadOfClampingAnOversizedFit()
        {
            Assert.IsFalse(SkirmishCameraFraming.TryAircraftPose(new Bounds(Vector3.zero, Vector3.one * 10000f), Ground, Vector2.up, true,
                50f, 0f, 60f, .3f, 16f / 9f, .3f, 1f, 300f, out _, out _, out _));
            Assert.IsFalse(SkirmishCameraFraming.TryAircraftPose(new Bounds(Vector3.zero, Vector3.one), Ground, Vector2.up, true,
                50f, 0f, 60f, .3f, float.NaN, .3f, 1f, 300f, out _, out _, out _));
        }

        [Test] public void CaptureLocaleDefaultsToEnglishAndAcceptsOnlyQueueLocales()
        {
            Assert.IsTrue(SkirmishCameraFramingCapture.TryResolveCaptureLocale(null, out var locale, out _)); Assert.AreEqual(GameLocalization.EnglishLocaleCode, locale);
            Assert.IsTrue(SkirmishCameraFramingCapture.TryResolveCaptureLocale("  ", out locale, out _)); Assert.AreEqual(GameLocalization.EnglishLocaleCode, locale);
            Assert.IsTrue(SkirmishCameraFramingCapture.TryResolveCaptureLocale("en", out locale, out _)); Assert.AreEqual("en", locale);
            Assert.IsTrue(SkirmishCameraFramingCapture.TryResolveCaptureLocale("fa-IR", out locale, out _)); Assert.AreEqual(GameLocalization.PersianLocaleCode, locale);
            foreach (var refused in new[] { "fa", "FA-IR", "de", "en-US" })
            {
                Assert.IsFalse(SkirmishCameraFramingCapture.TryResolveCaptureLocale(refused, out _, out var error), refused);
                StringAssert.Contains(SkirmishCameraFramingCapture.LocaleEnv, error);
            }
        }

        [Test] public void CaptureFileNamesCarryTheLocaleOnlyWhenNotEnglish()
        {
            // English names stay byte-identical to the committed "after" sets.
            Assert.AreEqual("S002-130365-2400x1080-ground", SkirmishCameraFramingCapture.GroundFilePrefix("S002", 130365, 2400, 1080, "en"));
            Assert.AreEqual("S002-130365-2400x1080-ground", SkirmishCameraFramingCapture.GroundFilePrefix("S002", 130365, 2400, 1080, null));
            Assert.AreEqual("S026-2400x1080-air", SkirmishCameraFramingCapture.AirFilePrefix("S026", 2400, 1080, "en"));
            Assert.AreEqual("S002-130365-2400x1080-fa-IR-ground", SkirmishCameraFramingCapture.GroundFilePrefix("S002", 130365, 2400, 1080, "fa-IR"));
            Assert.AreEqual("S026-1920x1080-fa-IR-air", SkirmishCameraFramingCapture.AirFilePrefix("S026", 1920, 1080, "fa-IR"));
        }

        [Test] public void HelicopterCardIsFoundByItsGroupEvenWhenAirMaskIsEmpty()
        {
            // A delivered transport helicopter is a support role: its card is presented but AirMask stays 0.
            var page = new UiExpandedSquadPage { Group0 = 3, Group1 = 4, Group2 = 9, AirMask = 0, AssaultMask = 0b011, SupportSlots = 3 << 4 };
            Assert.AreEqual(2, SkirmishCameraFramingCapture.HelicopterCardSlot(page, 9));
            Assert.AreEqual(-1, SkirmishCameraFramingCapture.HelicopterCardSlot(page, 12), "card on another page");
            Assert.AreEqual(-1, SkirmishCameraFramingCapture.HelicopterCardSlot(page, 0), "no group and no assault air");
            page.AirMask = 1 << 1;
            Assert.AreEqual(1, SkirmishCameraFramingCapture.HelicopterCardSlot(page, 0), "assault air is the fallback without a group");
            Assert.AreEqual(2, SkirmishCameraFramingCapture.HelicopterCardSlot(page, 9), "the group wins over another air card");
            string described = SkirmishCameraFramingCapture.DescribeSquadPage(true, page, 9);
            StringAssert.Contains("airMask=0x02", described); StringAssert.Contains("groups=[3,4,9,0,0]", described); StringAssert.Contains("page=0/0", described);
            StringAssert.Contains("squadPage=unread", SkirmishCameraFramingCapture.DescribeSquadPage(false, default, 9));
        }

        public static void RunFocusedValidation()
        {
            var tests = new SkirmishCameraFramingTests();
            tests.GroundFramingIsWiderThanTheAuthoredFloorAndKeepsCloseZoom();
            tests.GroundFramingNeverMovesCloserThanAnAlreadyWideProfile();
            tests.TacticalViewKeepsInfantryTapTargetsAtLandscapePhoneSize();
            tests.S026OpeningFramesGroupBarracksAndJunctionLookingDownTheAvenue();
            tests.S002OpeningFramesTheLiveBaseSquadAndApproach();
            tests.HelicopterViewShowsWholeAircraftGroundAndHeadingFromAbove();
            tests.AircraftViewRefusesInsteadOfClampingAnOversizedFit();
            tests.CaptureLocaleDefaultsToEnglishAndAcceptsOnlyQueueLocales();
            tests.CaptureFileNamesCarryTheLocaleOnlyWhenNotEnglish();
            tests.HelicopterCardIsFoundByItsGroupEvenWhenAirMaskIsEmpty();
            Debug.Log("[SkirmishCameraFramingTests] result=Passed tests=10 nativeCaptureReview=Pending phoneAcceptance=Pending");
        }
    }
}
