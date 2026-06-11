using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MatchHudMinimapProjectionSystemTests
{
    [Test]
    public void WorldAndNormalizedProjectionRoundTripUsesGridBounds()
    {
        MatchHudMinimapProjectionGrid grid = new(new Vector3(10f, 2f, 20f), 100f, 200f);
        Vector3 world = new(60f, 99f, 120f);

        Assert.IsTrue(MatchHudMinimapProjectionSystem.TryWorldToNormalized(grid, world, out Vector2 normalized));
        Assert.AreEqual(0.5f, normalized.x, 0.0001f);
        Assert.AreEqual(0.5f, normalized.y, 0.0001f);

        Vector3 roundTrip = MatchHudMinimapProjectionSystem.NormalizedToWorld(grid, normalized);
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
            Assert.IsTrue(MatchHudMinimapProjectionSystem.TryGetCameraViewportRect(camera, grid, out Rect rect));
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
            GridConfig grid = new()
            {
                Width = 100,
                Height = 100,
                CellSize = 10f,
                Origin = float3.zero
            };

            MatchHudMinimapProjectionGrid localGrid = MatchHudMinimapProjectionSystem.CreateCameraCenteredGrid(grid, camera, 1.5f);

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
            GridConfig grid = new()
            {
                Width = 100,
                Height = 100,
                CellSize = 10f,
                Origin = float3.zero
            };

            MatchHudMinimapProjectionGrid localGrid = MatchHudMinimapProjectionSystem.CreateCameraCenteredGrid(grid, camera, 1.5f);

            Assert.IsTrue(MatchHudMinimapProjectionSystem.TryGetCameraViewportRect(camera, localGrid, out Rect rect));
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
            GridConfig grid = new()
            {
                Width = 100,
                Height = 100,
                CellSize = 10f,
                Origin = float3.zero
            };

            MatchHudMinimapProjectionGrid localGrid = MatchHudMinimapProjectionSystem.CreateCameraCenteredGrid(grid, camera, 1.5f);

            Assert.Less(localGrid.Origin.x, 0f);
            Assert.IsTrue(MatchHudMinimapProjectionSystem.TryGetCameraViewportRect(camera, localGrid, out Rect rect));
            Assert.AreEqual(0.5f, rect.center.x, 0.01f);
            Assert.AreEqual(0.5f, rect.center.y, 0.01f);
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

            MatchHudMinimapProjectionSystem.ConfigureCaptureCamera(camera, grid, ~0);

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
    public void CenteredGridAllowsWindowPastMapEdgeToKeepRequestedCenter()
    {
        GridConfig grid = new()
        {
            Width = 100,
            Height = 80,
            CellSize = 10f,
            Origin = float3.zero
        };

        MatchHudMinimapProjectionGrid localGrid = MatchHudMinimapProjectionSystem.CreateCenteredGrid(
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
        GridConfig grid = new()
        {
            Width = 100,
            Height = 80,
            CellSize = 10f,
            Origin = float3.zero
        };

        Vector3 clamped = MatchHudMinimapProjectionSystem.ClampWorldToGrid(grid, new Vector3(-50f, 999f, 1200f));

        Assert.AreEqual(0f, clamped.x, 0.0001f);
        Assert.AreEqual(0f, clamped.y, 0.0001f);
        Assert.AreEqual(800f, clamped.z, 0.0001f);
    }

    [Test]
    public void NormalizedToWorldClampsOutOfRangeInput()
    {
        MatchHudMinimapProjectionGrid grid = new(Vector3.zero, 100f, 50f);

        Vector3 world = MatchHudMinimapProjectionSystem.NormalizedToWorld(grid, new Vector2(2f, -1f));

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
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World world = new("RuntimeMinimapReplacesDefaultSpriteAndCentersViewportOnBind");
        World.DefaultGameObjectInjectionWorld = world;
        GameObject panel = new("MinimapPanel_RuntimeBind");
        GameObject cameraObject = new("MinimapRuntimeBindCamera");
        Texture2D defaultTexture = new(4, 4, TextureFormat.RGBA32, false);
        MatchHudMinimapInputSystem inputSystem = null;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = em.CreateEntity(typeof(GridConfig));
            em.SetComponentData(gridEntity, new GridConfig
            {
                Width = 100,
                Height = 100,
                CellSize = 10f,
                Origin = float3.zero
            });

            DynamicBuffer<GridRoad> roads = em.AddBuffer<GridRoad>(gridEntity);
            roads.ResizeUninitialized(100 * 100);
            for (int i = 0; i < roads.Length; i++)
                roads[i] = new GridRoad { Value = (byte)(i % 17 == 0 ? 1 : 0) };

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

            SelectionUiCameraSystem cameraSystem = new(new RtsCameraSystem(), new RtsCameraRequestSystem());
            cameraSystem.Init(null, camera);
            RuntimeGameplayStateSystem runtimeState = new();
            inputSystem = new MatchHudMinimapInputSystem();
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                LogAssert.Expect(LogType.Error, "RenderTexture.Create failed");
            inputSystem.Bind(view, runtimeState, cameraSystem);

            Assert.IsTrue(mapImage.enabled, "Runtime minimap must keep the existing Map Image enabled.");
            Assert.NotNull(mapImage.sprite, "Runtime minimap must assign a generated sprite to the existing Map Image.");
            Assert.AreEqual("Runtime_MatchHudMinimapSprite", mapImage.sprite.name);

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
            inputSystem?.Dispose();
            Object.DestroyImmediate(defaultTexture);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(panel);
            World.DefaultGameObjectInjectionWorld = previousWorld;
            world.Dispose();
        }
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
}
