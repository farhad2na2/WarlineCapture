using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class BuildDrawerHudOcclusionView
    {
        private Canvas guidedCanvas;
        private GraphicRaycaster guidedRaycaster;
        private bool addedCanvas, addedRaycaster, previousOverrideSorting;
        private int previousSortingOrder;
        private RectTransform guidedDrawer;
        private Vector2 previousAnchorMax;
        private MainMenuV3SectionLayoutView guidedLayout;
        private bool previousLayoutEnabled;

        internal static bool RequiresTutorialAccess(UiAssistantPanelModel panel) =>
            panel.HasRecommendation && panel.TutorialStepCount == 9 &&
            panel.TutorialStep is 2 or 3 or 4 or 6;

        private void PreserveTutorial(AriaTutorialBriefingView aria)
        {
            guidedCanvas = aria.GetComponent<Canvas>();
            addedCanvas = guidedCanvas == null;
            if (addedCanvas) guidedCanvas = aria.gameObject.AddComponent<Canvas>();
            previousOverrideSorting = guidedCanvas.overrideSorting;
            previousSortingOrder = guidedCanvas.sortingOrder;
            var popupCanvas = GetComponentInParent<Canvas>();
            guidedCanvas.overrideSorting = true;
            guidedCanvas.sortingOrder = (popupCanvas != null ? popupCanvas.sortingOrder : 0) + 1;
            guidedRaycaster = aria.GetComponent<GraphicRaycaster>();
            addedRaycaster = guidedRaycaster == null;
            if (addedRaycaster) guidedRaycaster = aria.gameObject.AddComponent<GraphicRaycaster>();

            // Keep the existing ARIA rail beside the existing drawer, above its scrim.
            // The section layout scales its authored contents into the available width.
            guidedDrawer = GetComponent<BuildDrawerView>()?.DrawerRoot?.transform as RectTransform;
            if (guidedDrawer != null) previousAnchorMax = guidedDrawer.anchorMax;
            var parent = guidedDrawer != null ? guidedDrawer.parent as RectTransform : null;
            if (parent == null || parent.rect.width <= 0f) return;
            var corners = new Vector3[4];
            ((RectTransform)aria.transform).GetWorldCorners(corners);
            float left = parent.InverseTransformPoint(corners[0]).x;
            float rightEdge = Mathf.Clamp((left - parent.rect.xMin - 16f) / parent.rect.width, .6f, .85f);
            guidedDrawer.anchorMax = new Vector2(rightEdge, previousAnchorMax.y);
            guidedLayout = guidedDrawer.GetComponentInChildren<MainMenuV3SectionLayoutView>();
            if (guidedLayout != null)
            {
                previousLayoutEnabled = guidedLayout.enabled;
                guidedLayout.enabled = false;
                FitGuidedDrawer();
            }
        }

        private void LateUpdate() => FitGuidedDrawer();

        private void FitGuidedDrawer()
        {
            if (guidedDrawer == null || guidedLayout == null) return;
            var frame = (RectTransform)guidedLayout.transform;
            var size = guidedDrawer.rect.size;
            var reference = guidedLayout.ReferenceResolution;
            float scale = Mathf.Min(size.x / reference.x, size.y / reference.y);
            frame.localScale = Vector3.one * scale;
            frame.position = guidedDrawer.TransformPoint(guidedDrawer.rect.center);
        }

        private void RestoreTutorial()
        {
            if (guidedDrawer != null) guidedDrawer.anchorMax = previousAnchorMax;
            guidedDrawer = null;
            if (guidedLayout != null)
            {
                guidedLayout.enabled = previousLayoutEnabled;
                guidedLayout.RefreshLayout();
            }
            guidedLayout = null;
            if (guidedRaycaster != null && addedRaycaster)
                UIShellContentView.DestroyRegionObject(guidedRaycaster);
            if (guidedCanvas != null)
            {
                if (addedCanvas) UIShellContentView.DestroyRegionObject(guidedCanvas);
                else
                {
                    guidedCanvas.overrideSorting = previousOverrideSorting;
                    guidedCanvas.sortingOrder = previousSortingOrder;
                }
            }
            guidedRaycaster = null;
            guidedCanvas = null;
        }

        private void HideOtherHeaderChildren(Transform parent, Transform aria)
        {
            foreach (Transform child in parent)
            {
                if (child == aria) continue;
                if (aria.IsChildOf(child)) HideOtherHeaderChildren(child, aria);
                else RememberSection(child.gameObject);
            }
        }
    }
}
