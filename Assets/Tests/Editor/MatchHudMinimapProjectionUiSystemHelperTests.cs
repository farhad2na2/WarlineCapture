using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Game.UI.Contracts;
using Game.UI.Runtime;

public sealed class MatchHudMinimapProjectionUiSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new MatchHudMinimapProjectionUiSystemHelperTests();
            tests.WorldAndNormalizedProjectionRoundTripUsesGridBounds();
            tests.CameraViewportRectProjectsToNormalizedMapRect();
            tests.CameraCenteredGridUsesLocalWindowAroundCamera();
            tests.CameraCenteredGridCentersPerspectiveViewportFootprint();
            tests.CameraCenteredGridKeepsViewportCenteredNearMapEdge();
            tests.CameraProjectionHelpersDoNotAllocateAfterWarmup();
            tests.CaptureCameraUsesProjectionGridAspectAndCenter();
            tests.CompactRuntimeMinimapUsesCameraLocalAreaForMarkers();
            tests.CenteredGridAllowsWindowPastMapEdgeToKeepRequestedCenter();
            tests.ClampWorldToGridKeepsFocusInsideAuthoredMap();
            tests.NormalizedToWorldClampsOutOfRangeInput();
            tests.ViewportRectUsesMapPositionWhenViewportIsNotMapChild();
            tests.RebindingAfterDestroyedMapViewRecreatesMarkerPool();
            tests.ViewportDragUsesViewportParentSpaceWhenMapIsFramed();
            tests.ViewportDragCanStartFromVisibleOutlinePadding();
            tests.FullMapViewportDragRequestsCameraMoveThroughInputHelper();
            tests.FullMapProjectionExpandsToKeepCameraViewportInsideMap();
            Debug.Log("[MatchHudMinimapProjectionFocusedValidation] result=Passed tests=17");
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[MatchHudMinimapProjectionFocusedValidation] result=Failed");
            throw;
        }
    }

    [Test]
    public void WorldAndNormalizedProjectionRoundTripUsesGridBounds()
    {
        MatchHudMinimapProjectionGrid grid = new(new Vector3(10f, 2f, 20f), 100f, 200f);
        Vector3 world = new(60f, 99f, 120f);

        Assert.IsTrue(MatchHudMinimapProjectionUiSystemHelper.TryWorldToNormalized(grid, world, out Vector2 normalized));
        Assert.AreEqual(0.5f, normalized.x, 0.0001f);
        Assert.AreEqual(0.5f, normalized.y, 0.0001f);

        Vector3 roundTrip = MatchHudMinimapProjectionUiSystemHelper.NormalizedToWorld(grid, normalized);
        Assert.AreEqual(60f, roundTrip.x, 0.0001f);
        Assert.AreEqual(2f, roundTrip.y, 0.0001f);
        Assert.AreEqual(120f, roundTrip.z, 0.0001f);
    }

    [Test]
    public void CameraViewportRectProjectsToNormalizedMapRect()
    {
        GameObject cameraObject = new("MinimapProjectionTestCamera");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 25f;
            camera.aspect = 1f;
            camera.transform.position = new Vector3(50f, 100f, 50f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            MatchHudMinimapProjectionGrid grid = new(Vector3.zero, 100f, 100f);
            Assert.IsTrue(MatchHudMinimapProjectionUiSystemHelper.TryGetCameraViewportRect(camera, grid, out Rect rect));
            Assert.AreEqual(0.25f, rect.xMin, 0.01f);
            Assert.AreEqual(0.25f, rect.yMin, 0.01f);
            Assert.AreEqual(0.5f, rect.width, 0.01f);
            Assert.AreEqual(0.5f, rect.height, 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void CameraCenteredGridUsesLocalWindowAroundCamera()
    {
        GameObject cameraObject = new("MinimapLocalProjectionTestCamera");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 25f;
            camera.aspect = 1.5f;
            camera.transform.position = new Vector3(500f, 100f, 500f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            MatchHudMinimapGridModel grid = CreateGridModel(100, 100, 10f);

            MatchHudMinimapProjectionGrid localGrid = MatchHudMinimapProjectionUiSystemHelper.CreateCameraCenteredGrid(grid, camera, 1.5f);

            Assert.Less(localGrid.Width, 1000f);
            Assert.Less(localGrid.Height, 1000f);
            Assert.GreaterOrEqual(localGrid.Height, 200f);
            Assert.AreEqual(500f, localGrid.Origin.x + localGrid.Width * 0.5f, 0.01f);
            Assert.AreEqual(500f, localGrid.Origin.z + localGrid.Height * 0.5f, 0.01f);
            Assert.AreEqual(1.5f, localGrid.Width / localGrid.Height, 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void CameraCenteredGridCentersPerspectiveViewportFootprint()
    {
        GameObject cameraObject = new("MinimapPerspectiveProjectionTestCamera");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 45f;
            camera.aspect = 1.5f;
            camera.transform.position = new Vector3(500f, 100f, 430f);
            camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            MatchHudMinimapGridModel grid = CreateGridModel(100, 100, 10f);

            MatchHudMinimapProjectionGrid localGrid = MatchHudMinimapProjectionUiSystemHelper.CreateCameraCenteredGrid(grid, camera, 1.5f);

            Assert.IsTrue(MatchHudMinimapProjectionUiSystemHelper.TryGetCameraViewportRect(camera, localGrid, out Rect rect));
            Assert.AreEqual(0.5f, rect.center.x, 0.03f);
            Assert.AreEqual(0.5f, rect.center.y, 0.03f);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void CameraCenteredGridKeepsViewportCenteredNearMapEdge()
    {
        GameObject cameraObject = new("MinimapEdgeProjectionTestCamera");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 25f;
            camera.aspect = 1.5f;
            camera.transform.position = new Vector3(-50f, 100f, 900f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            MatchHudMinimapGridModel grid = CreateGridModel(100, 100, 10f);

            MatchHudMinimapProjectionGrid localGrid = MatchHudMinimapProjectionUiSystemHelper.CreateCameraCenteredGrid(grid, camera, 1.5f);

            Assert.Less(localGrid.Origin.x, 0f);
            Assert.IsTrue(MatchHudMinimapProjectionUiSystemHelper.TryGetCameraViewportRect(camera, localGrid, out Rect rect));
            Assert.AreEqual(0.5f, rect.center.x, 0.01f);
            Assert.AreEqual(0.5f, rect.center.y, 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void CameraProjectionHelpersDoNotAllocateAfterWarmup()
    {
        GameObject cameraObject = new("MinimapProjectionAllocationTestCamera");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 25f;
            camera.aspect = 1.5f;
            camera.transform.position = new Vector3(500f, 100f, 500f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            MatchHudMinimapGridModel gridConfig = CreateGridModel(100, 100, 10f);
            MatchHudMinimapProjectionGrid grid = new(Vector3.zero, 1000f, 1000f);

            Assert.IsTrue(MatchHudMinimapProjectionUiSystemHelper.TryGetCameraViewportRect(camera, grid, out _));
            _ = MatchHudMinimapProjectionUiSystemHelper.CreateCameraCenteredGrid(gridConfig, camera, 1.5f);

            bool projected = false;
            Assert.That(() =>
            {
                bool result = true;
                for (int i = 0; i < 128; i++)
                {
                    result &= MatchHudMinimapProjectionUiSystemHelper.TryGetCameraViewportRect(camera, grid, out Rect rect);
                    MatchHudMinimapProjectionGrid localGrid =
                        MatchHudMinimapProjectionUiSystemHelper.CreateCameraCenteredGrid(gridConfig, camera, 1.5f);
                    result &= rect.width > 0f && rect.height > 0f && localGrid.Width > 0f && localGrid.Height > 0f;
                }

                projected = result;
            }, new NUnit.Framework.Constraints.NotConstraint(
                UnityEngine.TestTools.Constraints.Is.AllocatingGCMemory()));

            Assert.IsTrue(projected);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void CaptureCameraUsesProjectionGridAspectAndCenter()
    {
        GameObject cameraObject = new("MinimapCaptureProjectionTestCamera");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            MatchHudMinimapProjectionGrid grid = new(new Vector3(100f, 0f, 200f), 300f, 200f);

            MatchHudMinimapProjectionUiSystemHelper.ConfigureCaptureCamera(camera, grid, ~0);

            Assert.IsTrue(camera.orthographic);
            Assert.AreEqual(100f, camera.orthographicSize, 0.001f);
            Assert.AreEqual(1.5f, camera.aspect, 0.001f);
            Assert.AreEqual(250f, camera.transform.position.x, 0.001f);
            Assert.AreEqual(300f, camera.transform.position.z, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void CompactRuntimeMinimapUsesCameraLocalAreaForMarkers()
    {
        GameObject cameraObject = new("MinimapCompactAreaCamera");
        Texture2D defaultTexture = new(4, 4, TextureFormat.RGBA32, false);
        MatchHudMinimapInputUiSystemHelper inputSystem = null;
        GameObject panel = null;
        bool restoreLogAssertIgnore = false;
        try
        {
            panel = CreateMinimapPanel("MinimapPanel_CompactArea", defaultTexture, out MatchHudMinimapView view, out _, out _);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 25f;
            camera.aspect = view.MapRect.rect.width / view.MapRect.rect.height;
            camera.transform.position = new Vector3(500f, 100f, 500f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            FakeMatchRuntimeState runtimeState = new();
            FakeMatchHudCameraControl cameraControl = new(camera);
            FakeMinimapDataSource minimapDataSource = new(CreateGridModel(100, 100, 10f));
            minimapDataSource.Markers.Add(new MatchHudMinimapMarkerModel(
                new Vector3(500f, 0f, 500f),
                MatchHudMinimapMarkerAllegiance.Player));
            minimapDataSource.Markers.Add(new MatchHudMinimapMarkerModel(
                new Vector3(900f, 0f, 900f),
                MatchHudMinimapMarkerAllegiance.Enemy));

            inputSystem = new MatchHudMinimapInputUiSystemHelper();
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                LogAssert.ignoreFailingMessages = true;
                restoreLogAssertIgnore = true;
            }

            inputSystem.Bind(
                view,
                runtimeState,
                cameraControl,
                minimapDataSource,
                useFullMapProjection: false,
                showViewport: false,
                allowViewportDrag: false,
                allowMapFocus: false,
                allowZoom: false,
                openFullMapOnClick: true,
                useStableFullMapProjection: false);
            inputSystem.Update();

            Assert.AreEqual(1, CountActiveRuntimeMarkers(view.MapRect));
            Assert.Less(minimapDataSource.LastMarkerArea.Width, minimapDataSource.Grid.WorldWidth);
            Assert.Less(minimapDataSource.LastMarkerArea.Height, minimapDataSource.Grid.WorldHeight);
            Assert.IsFalse(view.UseFullMapProjection);
        }
        finally
        {
            if (restoreLogAssertIgnore)
                LogAssert.ignoreFailingMessages = false;
            inputSystem?.Dispose();
            Object.DestroyImmediate(defaultTexture);
            Object.DestroyImmediate(cameraObject);
            if (panel != null)
                Object.DestroyImmediate(panel);
        }
    }

    [Test]
    public void CenteredGridAllowsWindowPastMapEdgeToKeepRequestedCenter()
    {
        MatchHudMinimapGridModel grid = CreateGridModel(100, 80, 10f);

        MatchHudMinimapProjectionGrid localGrid = MatchHudMinimapProjectionUiSystemHelper.CreateCenteredGrid(
            grid,
            new Vector3(-200f, 0f, 2000f),
            new Vector2(120f, 100f),
            1.5f);

        Assert.Less(localGrid.Origin.x, 0f);
        Assert.Greater(localGrid.Origin.z + localGrid.Height, 800f);
        Assert.AreEqual(-200f, localGrid.Origin.x + localGrid.Width * 0.5f, 0.001f);
        Assert.AreEqual(2000f, localGrid.Origin.z + localGrid.Height * 0.5f, 0.001f);
    }

    [Test]
    public void ClampWorldToGridKeepsFocusInsideAuthoredMap()
    {
        MatchHudMinimapGridModel grid = CreateGridModel(100, 80, 10f);

        Vector3 clamped = MatchHudMinimapProjectionUiSystemHelper.ClampWorldToGrid(grid, new Vector3(-50f, 999f, 1200f));

        Assert.AreEqual(0f, clamped.x, 0.0001f);
        Assert.AreEqual(0f, clamped.y, 0.0001f);
        Assert.AreEqual(800f, clamped.z, 0.0001f);
    }

    [Test]
    public void NormalizedToWorldClampsOutOfRangeInput()
    {
        MatchHudMinimapProjectionGrid grid = new(Vector3.zero, 100f, 50f);

        Vector3 world = MatchHudMinimapProjectionUiSystemHelper.NormalizedToWorld(grid, new Vector2(2f, -1f));

        Assert.AreEqual(100f, world.x, 0.0001f);
        Assert.AreEqual(0f, world.z, 0.0001f);
    }

    [Test]
    public void ViewportRectUsesMapPositionWhenViewportIsNotMapChild()
    {
        GameObject panel = new("MinimapPanel");
        try
        {
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(900f, 610f);

            RectTransform mapRect = CreateRect("Map", panelRect, new Vector2(832f, 562f), new Vector2(-1f, 0f));
            Image mapImage = mapRect.gameObject.AddComponent<Image>();
            RectTransform frameRect = CreateRect("Frame", panelRect, new Vector2(900f, 610f), Vector2.zero);
            RectTransform viewportRect = CreateRect("Viewport", frameRect, new Vector2(250f, 154f), Vector2.zero);
            MatchHudMinimapView view = panel.AddComponent<MatchHudMinimapView>();
            view.Configure(mapImage, mapRect, viewportRect, null, null, panelRect);

            view.SetViewportNormalizedRect(new Rect(0.25f, 0.25f, 0.5f, 0.5f));

            Rect mapInFrame = GetRectInParent(mapRect, frameRect);
            Rect viewportInFrame = GetRectInParent(viewportRect, frameRect);
            Assert.AreEqual(mapInFrame.xMin + mapInFrame.width * 0.25f, viewportInFrame.xMin, 0.01f);
            Assert.AreEqual(mapInFrame.xMin + mapInFrame.width * 0.75f, viewportInFrame.xMax, 0.01f);
            Assert.AreEqual(mapInFrame.yMin + mapInFrame.height * 0.25f, viewportInFrame.yMin, 0.01f);
            Assert.AreEqual(mapInFrame.yMin + mapInFrame.height * 0.75f, viewportInFrame.yMax, 0.01f);

            view.SetViewportNormalizedRect(new Rect(0.8f, 0.8f, 0.5f, 0.5f));
            viewportInFrame = GetRectInParent(viewportRect, frameRect);
            Assert.AreEqual(mapInFrame.xMax, viewportInFrame.xMax, 0.01f);
            Assert.AreEqual(mapInFrame.yMax, viewportInFrame.yMax, 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(panel);
        }
    }

    [Test]
    public void RuntimeMinimapReplacesDefaultSpriteOnExistingImageAndCentersViewportOnBind()
    {
        GameObject panel = new("MinimapPanel_RuntimeBind");
        GameObject cameraObject = new("MinimapRuntimeBindCamera");
        Texture2D defaultTexture = new(4, 4, TextureFormat.RGBA32, false);
        MatchHudMinimapInputUiSystemHelper inputSystem = null;
        bool restoreLogAssertIgnore = false;
        try
        {
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(900f, 610f);
            RectTransform mapRect = CreateRect("Map", panelRect, new Vector2(832f, 562f), Vector2.zero);
            Image mapImage = mapRect.gameObject.AddComponent<Image>();
            mapImage.sprite = Sprite.Create(defaultTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);

            RectTransform viewportRect = CreateRect("Viewport", panelRect, new Vector2(250f, 154f), Vector2.zero);
            Button zoomInButton = CreateRect("ZoomIn", panelRect, new Vector2(32f, 32f), Vector2.zero).gameObject.AddComponent<Button>();
            Button zoomOutButton = CreateRect("ZoomOut", panelRect, new Vector2(32f, 32f), Vector2.zero).gameObject.AddComponent<Button>();
            MatchHudMinimapView view = panel.AddComponent<MatchHudMinimapView>();
            view.Configure(mapImage, mapRect, viewportRect, zoomInButton, zoomOutButton, panelRect);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 25f;
            camera.aspect = mapRect.rect.width / mapRect.rect.height;
            camera.transform.position = new Vector3(500f, 100f, 500f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            FakeMatchRuntimeState runtimeState = new();
            FakeMatchHudCameraControl cameraControl = new(camera);
            FakeMinimapDataSource minimapDataSource = new(CreateGridModel(100, 100, 10f));
            for (int i = 0; i < 100; i++)
            {
                minimapDataSource.Roads.Add(new MatchHudMinimapRoadCellModel(
                    new Vector3(i * 10f + 5f, 0f, i * 10f + 5f),
                    10f,
                    MatchHudMinimapRoadKind.Road));
            }

            inputSystem = new MatchHudMinimapInputUiSystemHelper();
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                LogAssert.ignoreFailingMessages = true;
                restoreLogAssertIgnore = true;
            }
            inputSystem.Bind(
                view,
                runtimeState,
                cameraControl,
                minimapDataSource);

            Assert.IsTrue(mapImage.enabled, "Runtime minimap must keep the existing Map Image enabled.");
            Assert.NotNull(mapImage.sprite, "Runtime minimap must assign a generated sprite to the existing Map Image.");
            StringAssert.StartsWith("Runtime_MatchHudMinimap", mapImage.sprite.name);

            Rect viewportInPanel = GetRectInParent(viewportRect, panelRect);
            Rect mapInPanel = GetRectInParent(mapRect, panelRect);
            Assert.AreEqual(mapInPanel.center.x, viewportInPanel.center.x, 0.5f);
            Assert.AreEqual(mapInPanel.center.y, viewportInPanel.center.y, 0.5f);

            float zoomedOutViewportWidth = viewportInPanel.width;
            zoomInButton.GetComponent<MatchHudMinimapZoomPressRelay>().OnPointerDown(null);
            viewportInPanel = GetRectInParent(viewportRect, panelRect);

            Assert.Greater(viewportInPanel.width, zoomedOutViewportWidth);
            Assert.IsFalse(runtimeState.ZoomInHeld);
            Assert.IsFalse(runtimeState.ZoomOutHeld);

            zoomOutButton.GetComponent<MatchHudMinimapZoomPressRelay>().OnPointerDown(null);
            viewportInPanel = GetRectInParent(viewportRect, panelRect);

            Assert.AreEqual(zoomedOutViewportWidth, viewportInPanel.width, 0.5f);
            Assert.IsFalse(runtimeState.ZoomInHeld);
            Assert.IsFalse(runtimeState.ZoomOutHeld);
        }
        finally
        {
            if (restoreLogAssertIgnore)
                LogAssert.ignoreFailingMessages = false;
            inputSystem?.Dispose();
            Object.DestroyImmediate(defaultTexture);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(panel);
        }
    }

    [Test]
    public void RebindingAfterDestroyedMapViewRecreatesMarkerPool()
    {
        GameObject cameraObject = new("MinimapRebindCamera");
        Texture2D defaultTexture = new(4, 4, TextureFormat.RGBA32, false);
        MatchHudMinimapInputUiSystemHelper inputSystem = null;
        GameObject firstPanel = null;
        GameObject secondPanel = null;
        bool restoreLogAssertIgnore = false;
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 25f;
            camera.aspect = 1f;
            camera.transform.position = new Vector3(500f, 100f, 500f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            FakeMatchRuntimeState runtimeState = new();
            FakeMatchHudCameraControl cameraControl = new(camera);
            FakeMinimapDataSource minimapDataSource = new(CreateGridModel(100, 100, 10f));
            minimapDataSource.Markers.Add(new MatchHudMinimapMarkerModel(
                new Vector3(500f, 0f, 500f),
                MatchHudMinimapMarkerAllegiance.Player));

            inputSystem = new MatchHudMinimapInputUiSystemHelper();
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                LogAssert.ignoreFailingMessages = true;
                restoreLogAssertIgnore = true;
            }

            firstPanel = CreateMinimapPanel("MinimapPanel_First", defaultTexture, out MatchHudMinimapView firstView, out Button firstZoomIn, out _);
            inputSystem.Bind(firstView, runtimeState, cameraControl, minimapDataSource);
            inputSystem.Update();
            firstZoomIn.GetComponent<MatchHudMinimapZoomPressRelay>().OnPointerDown(null);
            inputSystem.Unbind();
            Object.DestroyImmediate(firstPanel);
            firstPanel = null;

            secondPanel = CreateMinimapPanel("MinimapPanel_Second", defaultTexture, out MatchHudMinimapView secondView, out Button secondZoomIn, out Button secondZoomOut);
            inputSystem.Bind(secondView, runtimeState, cameraControl, minimapDataSource);
            secondZoomIn.GetComponent<MatchHudMinimapZoomPressRelay>().OnPointerDown(null);
            secondZoomOut.GetComponent<MatchHudMinimapZoomPressRelay>().OnPointerDown(null);

            Assert.IsNotNull(secondView.MapRect.Find("MinimapMarker"), "Rebound minimap must create fresh marker images under the live map view.");
        }
        finally
        {
            if (restoreLogAssertIgnore)
                LogAssert.ignoreFailingMessages = false;
            inputSystem?.Dispose();
            Object.DestroyImmediate(defaultTexture);
            Object.DestroyImmediate(cameraObject);
            if (firstPanel != null)
                Object.DestroyImmediate(firstPanel);
            if (secondPanel != null)
                Object.DestroyImmediate(secondPanel);
        }
    }

    [Test]
    public void ViewportDragUsesViewportParentSpaceWhenMapIsFramed()
    {
        GameObject panel = new("MinimapPanel_ViewportDrag");
        try
        {
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(900f, 610f);
            RectTransform frameRect = CreateRect("Frame", panelRect, new Vector2(900f, 610f), Vector2.zero);
            RectTransform mapRect = CreateRect("Map", frameRect, new Vector2(832f, 562f), new Vector2(-1f, 0f));
            Image mapImage = mapRect.gameObject.AddComponent<Image>();
            RectTransform viewportRect = CreateRect("Viewport", frameRect, new Vector2(250f, 154f), Vector2.zero);
            Image viewportImage = viewportRect.gameObject.AddComponent<Image>();
            viewportImage.raycastTarget = false;
            MatchHudMinimapView view = mapRect.gameObject.AddComponent<MatchHudMinimapView>();
            view.Configure(mapImage, mapRect, viewportRect, null, null, mapRect);
            view.ApplyInteractionOptions(
                useFullMapProjection: true,
                showViewport: true,
                allowViewportDrag: true,
                allowMapFocus: true,
                allowZoom: false,
                openFullMapOnClick: false);
            view.SetViewportNormalizedRect(new Rect(0.25f, 0.25f, 0.25f, 0.25f));

            Vector2 focused = default;
            view.FocusRequested += value => focused = value;
            MatchHudMinimapViewportDragRelay relay = viewportRect.GetComponent<MatchHudMinimapViewportDragRelay>();
            Assert.IsNotNull(relay);
            Assert.IsTrue(viewportImage.raycastTarget);

            relay.OnPointerDown(new PointerEventData(null)
            {
                position = GetScreenPoint(viewportRect, 0.5f, 0.5f)
            });
            relay.OnDrag(new PointerEventData(null)
            {
                position = GetScreenPoint(mapRect, 0.75f, 0.25f)
            });
            relay.OnPointerUp(new PointerEventData(null));

            Assert.AreEqual(0.75f, focused.x, 0.02f);
            Assert.AreEqual(0.25f, focused.y, 0.02f);
        }
        finally
        {
            Object.DestroyImmediate(panel);
        }
    }

    [Test]
    public void ViewportDragCanStartFromVisibleOutlinePadding()
    {
        GameObject panel = new("MinimapPanel_ViewportOutlineDrag");
        try
        {
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(900f, 610f);
            RectTransform frameRect = CreateRect("Frame", panelRect, new Vector2(900f, 610f), Vector2.zero);
            RectTransform mapRect = CreateRect("Map", frameRect, new Vector2(832f, 562f), new Vector2(-1f, 0f));
            Image mapImage = mapRect.gameObject.AddComponent<Image>();
            RectTransform viewportRect = CreateRect("Viewport", frameRect, new Vector2(250f, 154f), Vector2.zero);
            Image viewportImage = viewportRect.gameObject.AddComponent<Image>();
            viewportImage.raycastTarget = false;
            MatchHudMinimapView view = mapRect.gameObject.AddComponent<MatchHudMinimapView>();
            view.Configure(mapImage, mapRect, viewportRect, null, null, mapRect);
            view.ApplyInteractionOptions(
                useFullMapProjection: true,
                showViewport: true,
                allowViewportDrag: true,
                allowMapFocus: true,
                allowZoom: false,
                openFullMapOnClick: false);
            view.SetViewportNormalizedRect(new Rect(0.25f, 0.25f, 0.25f, 0.25f));

            Vector2 focused = default;
            view.FocusRequested += value => focused = value;
            Vector2 startOnVisibleOutline = GetScreenPoint(viewportRect, 1f, 0.5f) + new Vector2(8f, 0f);

            view.OnPointerDown(new PointerEventData(null)
            {
                position = startOnVisibleOutline
            });
            view.OnDrag(new PointerEventData(null)
            {
                position = GetScreenPoint(mapRect, 0.75f, 0.25f)
            });
            view.OnPointerUp(new PointerEventData(null));

            Assert.AreEqual(0.625f, focused.x, 0.02f);
            Assert.AreEqual(0.25f, focused.y, 0.02f);
        }
        finally
        {
            Object.DestroyImmediate(panel);
        }
    }

    [Test]
    public void FullMapViewportDragRequestsCameraMoveThroughInputHelper()
    {
        GameObject cameraObject = new("MinimapFullMapDragCamera");
        Texture2D defaultTexture = new(4, 4, TextureFormat.RGBA32, false);
        MatchHudMinimapInputUiSystemHelper inputSystem = null;
        GameObject panel = null;
        bool restoreLogAssertIgnore = false;
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 25f;
            camera.aspect = 1f;
            camera.transform.position = new Vector3(500f, 100f, 500f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            FakeMatchRuntimeState runtimeState = new();
            FakeMatchHudCameraControl cameraControl = new(camera);
            FakeMinimapDataSource minimapDataSource = new(CreateGridModel(100, 100, 10f));
            panel = CreateMinimapPanel("MinimapPanel_FullMapDrag", defaultTexture, out MatchHudMinimapView view, out _, out _);
            inputSystem = new MatchHudMinimapInputUiSystemHelper();
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                LogAssert.ignoreFailingMessages = true;
                restoreLogAssertIgnore = true;
            }

            inputSystem.Bind(
                view,
                runtimeState,
                cameraControl,
                minimapDataSource,
                useFullMapProjection: true,
                showViewport: true,
                allowViewportDrag: true,
                allowMapFocus: true,
                allowZoom: false,
                openFullMapOnClick: false);
            MatchHudMinimapViewportDragRelay relay = view.ViewportRect.GetComponent<MatchHudMinimapViewportDragRelay>();
            Assert.IsNotNull(relay);

            relay.OnPointerDown(new PointerEventData(null)
            {
                position = GetScreenPoint(view.ViewportRect, 0.5f, 0.5f)
            });
            relay.OnDrag(new PointerEventData(null)
            {
                position = GetScreenPoint(view.MapRect, 0.8f, 0.2f)
            });
            relay.OnPointerUp(new PointerEventData(null));

            Assert.AreEqual(1, cameraControl.MoveCount);
            Assert.AreEqual(800f, cameraControl.LastMoveTarget.x, 1f);
            Assert.AreEqual(200f, cameraControl.LastMoveTarget.z, 1f);
            Assert.IsTrue(runtimeState.SuppressNextWorldClick);
        }
        finally
        {
            if (restoreLogAssertIgnore)
                LogAssert.ignoreFailingMessages = false;
            inputSystem?.Dispose();
            Object.DestroyImmediate(defaultTexture);
            Object.DestroyImmediate(cameraObject);
            if (panel != null)
                Object.DestroyImmediate(panel);
        }
    }

    [Test]
    public void FullMapProjectionExpandsToKeepCameraViewportInsideMap()
    {
        GameObject cameraObject = new("MinimapFullMapExpandedCamera");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 50f;
            camera.aspect = 1.5f;
            camera.transform.position = new Vector3(-30f, 100f, -20f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            MatchHudMinimapGridModel grid = CreateGridModel(100, 100, 10f);

            MatchHudMinimapProjectionGrid expandedGrid =
                MatchHudMinimapProjectionUiSystemHelper.CreateFullGridIncludingCamera(grid, camera);

            Assert.Less(expandedGrid.Origin.x, 0f);
            Assert.Less(expandedGrid.Origin.z, 0f);
            Assert.Greater(expandedGrid.Width, 1000f);
            Assert.Greater(expandedGrid.Height, 1000f);
            Assert.IsTrue(MatchHudMinimapProjectionUiSystemHelper.TryGetCameraViewportRect(camera, expandedGrid, out Rect rect));
            Assert.GreaterOrEqual(rect.xMin, -0.0001f);
            Assert.GreaterOrEqual(rect.yMin, -0.0001f);
            Assert.LessOrEqual(rect.xMax, 1.0001f);
            Assert.LessOrEqual(rect.yMax, 1.0001f);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    private static MatchHudMinimapGridModel CreateGridModel(int width, int height, float cellSize)
    {
        return new MatchHudMinimapGridModel(Vector3.zero, width, height, cellSize);
    }

    private static GameObject CreateMinimapPanel(
        string name,
        Texture2D defaultTexture,
        out MatchHudMinimapView view,
        out Button zoomInButton,
        out Button zoomOutButton)
    {
        GameObject panel = new(name);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(900f, 610f);
        RectTransform mapRect = CreateRect("Map", panelRect, new Vector2(832f, 562f), Vector2.zero);
        Image mapImage = mapRect.gameObject.AddComponent<Image>();
        mapImage.sprite = Sprite.Create(defaultTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        RectTransform viewportRect = CreateRect("Viewport", panelRect, new Vector2(250f, 154f), Vector2.zero);
        zoomInButton = CreateRect("ZoomIn", panelRect, new Vector2(32f, 32f), Vector2.zero).gameObject.AddComponent<Button>();
        zoomOutButton = CreateRect("ZoomOut", panelRect, new Vector2(32f, 32f), Vector2.zero).gameObject.AddComponent<Button>();
        view = panel.AddComponent<MatchHudMinimapView>();
        view.Configure(mapImage, mapRect, viewportRect, zoomInButton, zoomOutButton, mapRect);
        return panel;
    }

    private static RectTransform CreateRect(string name, RectTransform parent, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject gameObject = new(name);
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        return rect;
    }

    private static Rect GetRectInParent(RectTransform rect, RectTransform parent)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 local = parent.InverseTransformPoint(corners[i]);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static Vector2 GetScreenPoint(RectTransform rect, float normalizedX, float normalizedY)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector3 bottomLeft = corners[0];
        Vector3 topLeft = corners[1];
        Vector3 bottomRight = corners[3];
        Vector3 world = bottomLeft +
            (bottomRight - bottomLeft) * normalizedX +
            (topLeft - bottomLeft) * normalizedY;
        return RectTransformUtility.WorldToScreenPoint(null, world);
    }

    private static int CountActiveRuntimeMarkers(RectTransform root)
    {
        int count = 0;
        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].gameObject.name == "MinimapMarker" && images[i].gameObject.activeSelf)
                count++;
        }

        return count;
    }

    private sealed class FakeMatchRuntimeState : IMatchRuntimeState
    {
        public bool PlayRequested { get; set; }
        public bool SimulationActive { get; set; }
        public bool SelectionModeActive { get; set; }
        public bool BuildModeActive { get; set; }
        public bool ZoomInHeld { get; set; }
        public bool ZoomOutHeld { get; set; }
        public bool SuppressNextWorldClick { get; set; }
    }

    private sealed class FakeMatchHudCameraControl : IMatchHudCameraControl
    {
        public FakeMatchHudCameraControl(Camera worldCamera)
        {
            WorldCamera = worldCamera;
        }

        public Camera WorldCamera { get; }
        public bool IsCameraDragging => false;
        public Vector3 LastMoveTarget { get; private set; }
        public int MoveCount { get; private set; }

        public void MoveCameraGroundCenterTo(Vector3 worldPosition)
        {
            LastMoveTarget = worldPosition;
            MoveCount++;
        }

        public void UpdateZoomTransition()
        {
        }

        public MatchHudZoomControlState ReadZoomControlState()
        {
            return MatchHudZoomControlState.Default;
        }

        public bool RequestZoomInLevel()
        {
            return true;
        }

        public bool RequestZoomOutLevel()
        {
            return true;
        }
    }

    private sealed class FakeMinimapDataSource : IMatchHudMinimapDataSource
    {
        private readonly MatchHudMinimapGridModel grid;

        public FakeMinimapDataSource(MatchHudMinimapGridModel grid)
        {
            this.grid = grid;
        }

        public MatchHudMinimapGridModel Grid => grid;
        public MatchHudMinimapAreaModel LastMarkerArea { get; private set; }
        public List<MatchHudMinimapRoadCellModel> Roads { get; } = new();
        public List<MatchHudMinimapMarkerModel> Markers { get; } = new();
        public List<MatchHudMinimapSurfaceFeatureModel> SurfaceFeatures { get; } = new();

        public bool TryGetGrid(out MatchHudMinimapGridModel resolvedGrid)
        {
            resolvedGrid = grid;
            return grid.IsValid;
        }

        public void GetMarkers(MatchHudMinimapAreaModel area, List<MatchHudMinimapMarkerModel> markers)
        {
            LastMarkerArea = area;
            markers.Clear();
            for (int i = 0; i < Markers.Count; i++)
            {
                MatchHudMinimapMarkerModel marker = Markers[i];
                if (area.ContainsXZ(marker.Position))
                    markers.Add(marker);
            }
        }

        public void GetRoadCells(MatchHudMinimapAreaModel area, List<MatchHudMinimapRoadCellModel> roadCells)
        {
            roadCells.Clear();
            roadCells.AddRange(Roads);
        }

        public void GetSurfaceFeatures(MatchHudMinimapAreaModel area, List<MatchHudMinimapSurfaceFeatureModel> features)
        {
            features.Clear();
            features.AddRange(SurfaceFeatures);
        }
    }
}
