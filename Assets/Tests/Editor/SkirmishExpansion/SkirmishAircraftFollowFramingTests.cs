using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Pure follow-framing math for the selected helicopter (designer retest 2026-11-18: whole aircraft,
    // rotor, heading and landing patch readable together). Native captures and phone acceptance are
    // separate gates.
    public sealed class SkirmishAircraftFollowFramingTests
    {
        const float Ground = -5.31082f, Pitch = 50f, Yaw = 180f, Fov = 60f, Near = .3f;
        static readonly float[] Aspects = { 2400f / 1080f, 1920f / 1080f };
        static readonly Bounds Helicopter = new Bounds(new Vector3(500f, 24f, 300f), new Vector3(14f, 5f, 16f)); // rotor disc included
        static readonly Vector2 Heading = new Vector2(.6f, -.8f);

        static bool Solve(Bounds aircraft, Vector3? patch, float aspect, out Vector3 position, out Vector3 lookAt, out bool framed) =>
            SkirmishCameraFraming.TryAircraftFollowPose(aircraft, Ground, Heading, true, patch, Pitch, Yaw, Fov,
                SkirmishCameraFraming.AircraftMaximumViewportFraction, aspect, Near, 1f, 300f, out position, out _, out lookAt, out framed);

        [Test] public void FollowLooksAtTheAirframeAndKeepsANearLandingPatchInTheSafeBand()
        {
            foreach (float aspect in Aspects)
            foreach (var offset in new[] { new Vector3(45f, 0f, 0f), new Vector3(-30f, 0f, 0f), new Vector3(0f, 0f, -40f), new Vector3(15f, 0f, 15f), new Vector3(-25f, 0f, -30f) })
            {
                var patch = new Vector3(Helicopter.center.x + offset.x, Ground, Helicopter.center.z + offset.z);
                Assert.IsTrue(Solve(Helicopter, patch, aspect, out var position, out var lookAt, out bool framed));
                Assert.That(Vector3.Distance(lookAt, Helicopter.center), Is.LessThan(.001f), "Look-at is the airframe, not a ground point under it");
                Assert.IsTrue(framed, "Patch " + offset + " is inside the follow distance");
                Assert.GreaterOrEqual(Vector3.Distance(position, lookAt), SkirmishCameraFraming.AircraftMinimumDistance - .01f);
                Assert.LessOrEqual(Vector3.Distance(position, lookAt), SkirmishCameraFraming.AircraftFollowPatchMaximumDistance + .01f);
                Assert.GreaterOrEqual(position.y, Helicopter.max.y + SkirmishCameraFraming.AircraftClearanceAboveTop - .01f);
                Assert.IsTrue(SkirmishCameraFraming.TryViewportShare(position, Pitch, Yaw, Fov, aspect, Helicopter, out float share, out bool inside));
                Assert.IsTrue(inside, "Whole airframe and rotor on screen");
                Assert.LessOrEqual(share, SkirmishCameraFraming.AircraftMaximumViewportFraction + .005f, "Airframe within the 30% cap");
                Assert.IsTrue(SkirmishCameraFraming.TryViewport(position, Pitch, Yaw, Fov, aspect, Helicopter.center, out var centre));
                Assert.That(centre.x, Is.EqualTo(.5f).Within(.001f)); Assert.That(centre.y, Is.EqualTo(.5f).Within(.001f));
                Assert.IsTrue(SkirmishCameraFraming.TryViewport(position, Pitch, Yaw, Fov, aspect, patch, out var onScreen));
                float bandV = SkirmishCameraFraming.AircraftPatchSafeFractionVertical * .5f, bandH = SkirmishCameraFraming.AircraftSafeFraction * .5f;
                Assert.That(onScreen.y, Is.InRange(.5f - bandV - .001f, .5f + bandV + .001f), "Patch clear of the cards and top strip");
                Assert.That(onScreen.x, Is.InRange(.5f - bandH - .001f, .5f + bandH + .001f));
            }
        }

        [Test] public void FarLandingPatchCapsTheDistanceAndKeepsItsBearingOnTheFrameEdge()
        {
            foreach (float aspect in Aspects)
            {
                var patch = new Vector3(Helicopter.center.x + 400f, Ground, Helicopter.center.z - 300f);
                Assert.IsTrue(Solve(Helicopter, patch, aspect, out var position, out var lookAt, out bool framed));
                Assert.IsFalse(framed);
                Assert.That(Vector3.Distance(position, lookAt), Is.EqualTo(SkirmishCameraFraming.AircraftFollowPatchMaximumDistance).Within(.01f));
                // The same bearing, scaled to the edge, is still on screen: the player sees where the patch lies.
                Assert.IsTrue(Solve(Helicopter, null, aspect, out _, out _, out _));
                var toward = Helicopter.center + (patch - Helicopter.center).normalized * 25f;
                Assert.IsTrue(SkirmishCameraFraming.TryViewport(position, Pitch, Yaw, Fov, aspect, toward, out var bearing));
                Assert.That(bearing.x, Is.InRange(0f, 1f)); Assert.That(bearing.y, Is.InRange(0f, 1f));
                Assert.IsTrue(SkirmishCameraFraming.TryViewportShare(position, Pitch, Yaw, Fov, aspect, Helicopter, out _, out bool inside));
                Assert.IsTrue(inside);
            }
        }

        [Test] public void FollowPoseMovesExactlyWithTheAircraft()
        {
            var patch = new Vector3(530f, Ground, 280f);
            Assert.IsTrue(Solve(Helicopter, patch, Aspects[0], out var before, out _, out _));
            var delta = new Vector3(-37.5f, 0f, 81.25f); // level flight: the ground under it is unchanged
            var moved = new Bounds(Helicopter.center + delta, Helicopter.size);
            Assert.IsTrue(Solve(moved, patch + new Vector3(delta.x, 0f, delta.z), Aspects[0], out var after, out var lookAt, out _));
            Assert.That(Vector3.Distance(after - before, delta), Is.LessThan(.01f), "Camera translates with the airframe");
            Assert.That(Vector3.Distance(lookAt, moved.center), Is.LessThan(.001f));
        }

        [Test] public void FollowWithoutPatchMatchesTheCompatibilityPose()
        {
            Assert.IsTrue(SkirmishCameraFraming.TryAircraftPose(Helicopter, Ground, Heading, true, Pitch, Yaw, Fov,
                SkirmishCameraFraming.AircraftMaximumViewportFraction, Aspects[0], Near, 1f, 300f, out var position, out _, out var lookAt));
            Assert.IsTrue(Solve(Helicopter, null, Aspects[0], out var follow, out var followLook, out bool framed));
            Assert.IsFalse(framed); Assert.AreEqual(position, follow); Assert.AreEqual(lookAt, followLook);
        }

        [Test] public void FollowDistanceEasesWidensFasterAndHoldsWithinOneFrame()
        {
            Assert.AreEqual(40f, RtsCameraSystem.FollowDistance(40f, 60f, 0f), "Several ticks in one frame keep the distance");
            Assert.AreEqual(55f, RtsCameraSystem.FollowDistance(0f, 55f, .1f), "No prior distance takes the target");
            float widen = RtsCameraSystem.FollowDistance(40f, 60f, .1f) - 40f, narrow = 60f - RtsCameraSystem.FollowDistance(60f, 40f, .1f);
            Assert.Greater(widen, 0f); Assert.Greater(narrow, 0f); Assert.Greater(widen, narrow, "Widening outpaces narrowing");
            float value = 40f;
            for (int i = 0; i < 120; i++) value = RtsCameraSystem.FollowDistance(value, 60f, 1f / 30f);
            Assert.That(value, Is.EqualTo(60f).Within(.01f));
            Assert.LessOrEqual(RtsCameraSystem.FollowDistance(40f, 60f, 10f), RtsCameraSystem.FollowDistance(40f, 60f, .25f) + .0001f, "A frame hitch is clamped");
        }

        public static void RunFocusedValidation()
        {
            var tests = new SkirmishAircraftFollowFramingTests();
            tests.FollowLooksAtTheAirframeAndKeepsANearLandingPatchInTheSafeBand();
            tests.FarLandingPatchCapsTheDistanceAndKeepsItsBearingOnTheFrameEdge();
            tests.FollowPoseMovesExactlyWithTheAircraft();
            tests.FollowWithoutPatchMatchesTheCompatibilityPose();
            tests.FollowDistanceEasesWidensFasterAndHoldsWithinOneFrame();
            Debug.Log("[SkirmishAircraftFollowFramingTests] result=Passed tests=5 nativeCaptureReview=Pending phoneAcceptance=Pending");
        }
    }
}
