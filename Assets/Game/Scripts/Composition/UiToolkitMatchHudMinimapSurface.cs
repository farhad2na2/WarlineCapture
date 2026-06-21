using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UiImage = UnityEngine.UI.Image;

[DisallowMultipleComponent]
public sealed class UiToolkitMatchHudMinimapSurface : MonoBehaviour
{
    private const int OverlaySortingOrder = 5000;
    private const float MinimumVisibleSize = 8f;

    private UiToolkitShellView shellView;
    private MainMenuPlayUI mainMenu;
    private Canvas overlayCanvas;
    private RectTransform overlayRoot;
    private RectTransform panelRect;
    private RectTransform mapRect;
    private RectTransform viewportRect;
    private UiImage mapImage;
    private MatchHudMinimapView minimapView;
    private MainMenuPlayUI boundMainMenu;
    private Rect lastScreenRect;
    private bool hasLastScreenRect;
    private bool overlayVisible;
    private bool toolkitMinimapHidden;
    private bool isBound;

    public static UiToolkitMatchHudMinimapSurface Ensure(GameObject owner)
    {
        if (owner == null)
            return null;

        UiToolkitMatchHudMinimapSurface surface = owner.GetComponent<UiToolkitMatchHudMinimapSurface>();
        if (surface == null)
            surface = owner.AddComponent<UiToolkitMatchHudMinimapSurface>();
        return surface;
    }

    public void Configure(UiToolkitShellView view, MainMenuPlayUI runtimeUi)
    {
        shellView = view;
        mainMenu = runtimeUi;
        EnsureSurface();

        if (isBound && !ReferenceEquals(boundMainMenu, mainMenu))
        {
            boundMainMenu?.BindMatchHudMinimap(null);
            boundMainMenu = null;
            isBound = false;
        }

        if (mainMenu == null || minimapView == null || isBound)
            return;

        mainMenu.BindMatchHudMinimap(minimapView);
        mainMenu.NotifyStaticMinimapChanged();
        boundMainMenu = mainMenu;
        isBound = true;
    }

    public void Clear()
    {
        if (isBound)
            boundMainMenu?.BindMatchHudMinimap(null);

        isBound = false;
        boundMainMenu = null;
        hasLastScreenRect = false;
        SetOverlayVisible(false);
        SetToolkitMinimapLiveSurfaceHidden(false);
        shellView = null;
        mainMenu = null;
    }

    private void LateUpdate()
    {
        bool visible = TryResolveMapScreenRect(out Rect screenRect);
        SetOverlayVisible(visible);
        SetToolkitMinimapLiveSurfaceHidden(visible);
        if (!visible)
            return;

        ApplyScreenRect(screenRect);
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnDestroy()
    {
        Clear();
    }

    private void EnsureSurface()
    {
        if (overlayCanvas != null)
            return;

        GameObject root = new("UiToolkitMatchHudMinimapSurface");
        root.transform.SetParent(transform, false);
        overlayRoot = root.AddComponent<RectTransform>();
        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;

        overlayCanvas = root.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = OverlaySortingOrder;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        root.AddComponent<GraphicRaycaster>();

        GameObject panel = new("MinimapPanel");
        panel.transform.SetParent(root.transform, false);
        panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = Vector2.zero;
        minimapView = panel.AddComponent<MatchHudMinimapView>();

        GameObject map = new("Map");
        map.transform.SetParent(panel.transform, false);
        mapRect = map.AddComponent<RectTransform>();
        mapRect.anchorMin = Vector2.zero;
        mapRect.anchorMax = Vector2.one;
        mapRect.offsetMin = Vector2.zero;
        mapRect.offsetMax = Vector2.zero;
        map.AddComponent<RectMask2D>();
        mapImage = map.AddComponent<UiImage>();
        mapImage.color = Color.white;
        mapImage.raycastTarget = true;

        GameObject viewport = new("Viewport");
        viewport.transform.SetParent(map.transform, false);
        viewportRect = viewport.AddComponent<RectTransform>();
        UiImage viewportImage = viewport.AddComponent<UiImage>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        viewportImage.raycastTarget = false;
        Outline outline = viewport.AddComponent<Outline>();
        outline.effectColor = new Color(0.96f, 0.92f, 0.78f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        minimapView.Configure(mapImage, mapRect, viewportRect, null, null, mapRect);
        minimapView.SetProjectionMode(useFullMapProjection: true);
        SetOverlayVisible(false);
    }

    private bool TryResolveMapScreenRect(out Rect screenRect)
    {
        screenRect = default;
        VisualElement map = shellView != null ? shellView.MatchHudMinimapMap : null;
        VisualElement root = shellView != null ? shellView.Root : null;
        if (map == null || root == null || map.panel == null)
            return false;

        Rect panelRect = map.worldBound;
        if (panelRect.width < MinimumVisibleSize || panelRect.height < MinimumVisibleSize)
            return false;

        float rootWidth = Mathf.Max(1f, root.resolvedStyle.width);
        float rootHeight = Mathf.Max(1f, root.resolvedStyle.height);
        float scaleX = Screen.width / rootWidth;
        float scaleY = Screen.height / rootHeight;
        float left = panelRect.xMin * scaleX;
        float top = panelRect.yMin * scaleY;
        float width = panelRect.width * scaleX;
        float height = panelRect.height * scaleY;
        if (width < MinimumVisibleSize || height < MinimumVisibleSize)
            return false;

        screenRect = new Rect(left, Screen.height - top - height, width, height);
        return true;
    }

    private void SetOverlayVisible(bool visible)
    {
        if (overlayVisible == visible)
            return;

        overlayVisible = visible;
        if (overlayCanvas != null)
            overlayCanvas.enabled = visible;
    }

    private void SetToolkitMinimapLiveSurfaceHidden(bool hidden)
    {
        if (toolkitMinimapHidden == hidden)
            return;

        toolkitMinimapHidden = hidden;
        SetVisibility(shellView != null ? shellView.MatchHudMinimapMap : null, hidden);
        SetVisibility(shellView != null ? shellView.MatchHudMinimapViewport : null, hidden);
        SetVisibility(shellView != null ? shellView.MatchHudMinimapFriendlyA : null, hidden);
        SetVisibility(shellView != null ? shellView.MatchHudMinimapFriendlyB : null, hidden);
        SetVisibility(shellView != null ? shellView.MatchHudMinimapHostileA : null, hidden);
        SetVisibility(shellView != null ? shellView.MatchHudMinimapCivilian : null, hidden);
    }

    private static void SetVisibility(VisualElement element, bool hidden)
    {
        if (element == null)
            return;

        element.style.visibility = hidden ? Visibility.Hidden : Visibility.Visible;
    }

    private void ApplyScreenRect(Rect screenRect)
    {
        if (panelRect == null)
            return;

        if (hasLastScreenRect &&
            Approximately(lastScreenRect.xMin, screenRect.xMin) &&
            Approximately(lastScreenRect.yMin, screenRect.yMin) &&
            Approximately(lastScreenRect.width, screenRect.width) &&
            Approximately(lastScreenRect.height, screenRect.height))
        {
            return;
        }

        panelRect.anchoredPosition = new Vector2(screenRect.xMin, screenRect.yMin);
        panelRect.sizeDelta = screenRect.size;
        lastScreenRect = screenRect;
        hasLastScreenRect = true;
    }

    private static bool Approximately(float a, float b)
    {
        return Mathf.Abs(a - b) < 0.5f;
    }
}
