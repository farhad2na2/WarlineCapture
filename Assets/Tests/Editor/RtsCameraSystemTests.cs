#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class RtsCameraSystemTests
{
    private readonly System.Collections.Generic.List<GameObject> _createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _createdObjects.Count; i++)
            Object.DestroyImmediate(_createdObjects[i]);
        _createdObjects.Clear();
    }

    [Test]
    public void Dragging_CanBeSetAndCleared()
    {
        var cameraSystem = new RtsCameraSystem();

        cameraSystem.SetDragging(true);
        Assert.IsTrue(cameraSystem.IsDragging);

        cameraSystem.ClearDragging();
        Assert.IsFalse(cameraSystem.IsDragging);
    }

    [Test]
    public void SetSmoothFocusTarget_StoresGroundTarget()
    {
        var cameraSystem = new RtsCameraSystem();

        cameraSystem.SetSmoothFocusTarget(new Vector3(3f, 12f, -4f), resetVelocity: true);

        Assert.IsTrue(cameraSystem.HasSmoothFocusTarget);
        Assert.AreEqual(new Vector3(3f, 0f, -4f), cameraSystem.SmoothFocusTarget);
    }

    [Test]
    public void UpdateSmoothFocus_WhenAlreadyAtTargetClearsTarget()
    {
        var cameraSystem = new RtsCameraSystem();
        Vector3 target = new(5f, 0f, 7f);
        cameraSystem.SetSmoothFocusTarget(target, resetVelocity: true);

        Vector3 smoothed = cameraSystem.UpdateSmoothFocus(target, 0.25f);

        Assert.AreEqual(target, smoothed);
        Assert.IsFalse(cameraSystem.HasSmoothFocusTarget);
    }

    [Test]
    public void ResetSession_ClearsDragAndSmoothFocus()
    {
        var cameraSystem = new RtsCameraSystem();
        cameraSystem.SetDragging(true);
        cameraSystem.SetSmoothFocusTarget(new Vector3(1f, 0f, 2f), resetVelocity: true);

        cameraSystem.ResetSession();

        Assert.IsFalse(cameraSystem.IsDragging);
        Assert.IsFalse(cameraSystem.HasSmoothFocusTarget);
    }

    [Test]
    public void ResetCameraModeSession_ClearsModeTransitionState()
    {
        var cameraSystem = new RtsCameraSystem
        {
            WasPlayRequested = true,
            WasBuildModeActive = true,
            IsZoomTransitionActive = true,
            NormalIsoModeActive = true,
            FullscreenIsoTargetHeight = 20f,
            FullscreenIsoTargetOrthographicSize = 12f
        };

        cameraSystem.ResetCameraModeSession();

        Assert.IsFalse(cameraSystem.WasPlayRequested);
        Assert.IsFalse(cameraSystem.WasBuildModeActive);
        Assert.IsFalse(cameraSystem.IsZoomTransitionActive);
        Assert.IsFalse(cameraSystem.NormalIsoModeActive);
    }

    [Test]
    public void PanCamera_MovesAlongFlattenedCameraAxes()
    {
        var cameraSystem = new RtsCameraSystem();
        Camera camera = CreateCamera(new Vector3(0f, 10f, -10f), Quaternion.Euler(45f, 0f, 0f));

        bool moved = cameraSystem.PanCamera(camera, new Vector2(10f, 0f), 0.1f);

        Assert.IsTrue(moved);
        Assert.That(camera.transform.position.x, Is.EqualTo(-1f).Within(0.0001f));
        Assert.That(camera.transform.position.y, Is.EqualTo(10f).Within(0.0001f));
        Assert.That(camera.transform.position.z, Is.EqualTo(-10f).Within(0.0001f));
    }

    [Test]
    public void ApplyPerspectiveCameraModeInstant_ConfiguresPerspectiveCamera()
    {
        var cameraSystem = new RtsCameraSystem();
        Camera camera = CreateCamera(Vector3.zero, Quaternion.identity);
        camera.orthographic = true;

        cameraSystem.ApplyPerspectiveCameraModeInstant(camera, 24f, 58f, 10f, 36f);

        Assert.IsFalse(camera.orthographic);
        Assert.That(camera.transform.position.y, Is.EqualTo(24f).Within(0.0001f));
        Assert.That(camera.transform.rotation.eulerAngles.x, Is.EqualTo(58f).Within(0.0001f));
        Assert.That(camera.transform.rotation.eulerAngles.y, Is.EqualTo(10f).Within(0.0001f));
        Assert.That(camera.fieldOfView, Is.EqualTo(36f).Within(0.0001f));
    }

    [Test]
    public void MoveCameraGroundCenterTo_PreservesHeightAndMovesGroundCenter()
    {
        var cameraSystem = new RtsCameraSystem();
        Camera camera = CreateCamera(new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f));

        cameraSystem.MoveCameraGroundCenterTo(camera, new Vector3(5f, 0f, 7f));

        Vector3 groundCenter = cameraSystem.GetCameraGroundCenterWorld(camera);
        Assert.That(groundCenter.x, Is.EqualTo(5f).Within(0.0001f));
        Assert.That(groundCenter.z, Is.EqualTo(7f).Within(0.0001f));
        Assert.That(camera.transform.position.y, Is.EqualTo(10f).Within(0.0001f));
    }

    [Test]
    public void UpdateFullscreenIsoZoom_ClampsTargets()
    {
        var cameraSystem = new RtsCameraSystem
        {
            FullscreenIsoTargetHeight = 20f,
            FullscreenIsoTargetOrthographicSize = 10f
        };

        cameraSystem.UpdateFullscreenIsoZoom(1f, 100f, 1f, 10f, 45f);

        Assert.That(cameraSystem.FullscreenIsoTargetHeight, Is.EqualTo(10f).Within(0.0001f));
        Assert.That(cameraSystem.FullscreenIsoTargetOrthographicSize, Is.EqualTo(8f).Within(0.0001f));
    }

    private Camera CreateCamera(Vector3 position, Quaternion rotation)
    {
        var gameObject = new GameObject("RtsCameraSystemTests.Camera");
        _createdObjects.Add(gameObject);
        Camera camera = gameObject.AddComponent<Camera>();
        camera.transform.position = position;
        camera.transform.rotation = rotation;
        return camera;
    }
}
#endif
