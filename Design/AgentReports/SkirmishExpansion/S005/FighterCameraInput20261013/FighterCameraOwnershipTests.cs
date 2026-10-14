using System;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

// Focused regression for the public camera used by minimap and route navigation.
// Disposable worlds exercise the production helper, not the Fighter's combat AI.
public sealed class FighterCameraOwnershipTests
{
    sealed class Fixture : IDisposable
    {
        readonly World previous = World.DefaultGameObjectInjectionWorld;
        readonly World world = new World("FighterCameraOwnership");
        readonly GameObject gameObject = new GameObject("FighterCameraOwnershipCamera");
        internal readonly Camera Camera;
        internal readonly RtsCameraSystem System;
        internal readonly SelectionUiCameraSystemHelper Helper;

        internal Fixture(float yaw)
        {
            World.DefaultGameObjectInjectionWorld = world;
            Camera = gameObject.AddComponent<Camera>();
            Camera.transform.SetPositionAndRotation(new Vector3(900, 24, 600), Quaternion.Euler(58, yaw, 0));
            Camera.fieldOfView = 36;
            System = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            var requests = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            Helper = new SelectionUiCameraSystemHelper(System, requests);
            Helper.Init(null, Camera);
        }

        public void Dispose()
        {
            Helper.Dispose();
            World.DefaultGameObjectInjectionWorld = previous;
            world.Dispose();
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    static void ZoomThenPan(bool zoomOut, float yaw, bool expectFix)
    {
        using var f = new Fixture(yaw);
        bool accepted = zoomOut ? f.Helper.RequestZoomOutLevel() : f.Helper.RequestZoomInLevel();
        Assert.IsTrue(accepted, "Start a genuine public HUD zoom transition.");
        Vector3 oldCenter = f.System.GetCameraGroundCenterWorld(f.Camera);
        Vector3 destination = oldCenter + new Vector3(150, 0, -100);
        var zoomState = f.Helper.ReadZoomControlState();
        f.Helper.MoveCameraGroundCenterTo(destination);
        AssertCentered(f, destination);
        f.Helper.UpdateZoomTransition();
        Vector3 after = f.System.GetCameraGroundCenterWorld(f.Camera);
        if (expectFix)
        {
            AssertCentered(f, destination);
            for (int i = 0; i < 8; i++)
            {
                f.Helper.UpdateZoomTransition();
                AssertCentered(f, destination);
            }
        }
        else
        {
            Assert.Less(Vector3.Distance(after, oldCenter), .01f, "Before-fix run must reproduce the old center snapback.");
            Assert.Greater(Vector3.Distance(after, destination), 100f);
        }
        var afterState = f.Helper.ReadZoomControlState();
        Assert.AreEqual(zoomState.ZoomInEnabled, afterState.ZoomInEnabled, "Pan must preserve requested zoom level.");
        Assert.AreEqual(zoomState.ZoomOutEnabled, afterState.ZoomOutEnabled);
    }

    static void AssertCentered(Fixture f, Vector3 destination)
    {
        // Ray/plane reconstruction at kilometer coordinates can differ by centimeters.
        // Also check the rendered projection so this cannot hide a camera snapback.
        float distance = Vector3.Distance(f.System.GetCameraGroundCenterWorld(f.Camera), destination);
        Vector3 viewport = f.Camera.WorldToViewportPoint(destination);
        float viewportError = Vector2.Distance(new Vector2(viewport.x, viewport.y), new Vector2(.5f, .5f));
        Debug.Log("[FighterCameraCenter] worldError=" + distance + " viewportError=" + viewportError);
        Assert.Less(distance, .05f, "Public zoom must retain the later pan within five centimeters.");
        Assert.Greater(viewport.z, 0f);
        Assert.Less(viewportError, .001f, "The requested point must remain centered in the rendered viewport.");
    }

    [TestCase(true, 10f)]
    [TestCase(false, 10f)]
    [TestCase(true, 40f)]
    [TestCase(false, 40f)]
    public void PublicPanRetargetsActiveZoom(bool zoomOut, float yaw) => ZoomThenPan(zoomOut, yaw, true);

    [Test]
    public void PublicPanStillCancelsProductionDeliveryFocus()
    {
        using var f = new Fixture(10);
        Assert.IsTrue(f.Helper.FocusProductionDelivery(new Vector3(930, 0, 710)));
        var destination = new Vector3(1100, 0, 430);
        f.Helper.MoveCameraGroundCenterTo(destination);
        float height = f.Camera.transform.position.y;
        for (int i = 0; i < 8; i++) f.Helper.UpdateZoomTransition();
        Assert.IsFalse(f.System.HasSmoothFocusTarget);
        Assert.IsFalse(f.System.HasSmoothPerspectiveTarget);
        Assert.Less(Vector3.Distance(f.System.GetCameraGroundCenterWorld(f.Camera), destination), .01f);
        Assert.AreEqual(height, f.Camera.transform.position.y, "Delivery zoom must relinquish manual camera ownership.");
    }

    [Test]
    public void PublicPanWithoutZoomPreservesCenterAndHeight()
    {
        using var f = new Fixture(10);
        float height = f.Camera.transform.position.y;
        var destination = new Vector3(1100, 0, 430);
        f.Helper.MoveCameraGroundCenterTo(destination);
        for (int i = 0; i < 8; i++) f.Helper.UpdateZoomTransition();
        Assert.Less(Vector3.Distance(f.System.GetCameraGroundCenterWorld(f.Camera), destination), .01f);
        Assert.AreEqual(height, f.Camera.transform.position.y);
    }

    public static void RunBeforeValidation() => Run(false);
    public static void RunAfterValidation() => Run(true);

    static void Run(bool expectFix)
    {
        try
        {
            foreach (float yaw in new[] { 10f, 40f })
            foreach (bool zoomOut in new[] { true, false }) ZoomThenPan(zoomOut, yaw, expectFix);
            var tests = new FighterCameraOwnershipTests();
            tests.PublicPanStillCancelsProductionDeliveryFocus();
            tests.PublicPanWithoutZoomPreservesCenterAndHeight();
            Debug.Log("[FighterCameraOwnership] result=Passed mode=" + (expectFix ? "After" : "Before") + " cases=6 scope=IsolatedProductionCameraHelper");
            ValidationExit.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogError("[FighterCameraOwnership] result=Failed " + error);
            ValidationExit.Exit(1);
        }
    }
}
