using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class BuildDrawerHudOcclusionView
    {
        private Canvas guidedCanvas, popupSortingCanvas;
        private GraphicRaycaster guidedRaycaster;
        private bool addedCanvas, addedRaycaster, previousOverrideSorting;
        private int previousSortingOrder, previousSortingLayer;
        private RectTransform guidedDrawer, guidedAria;
        private readonly Vector3[] guideCorners = new Vector3[4];
        private Vector2 previousAnchorMax;
        private MainMenuV3SectionLayoutView guidedLayout;
        private bool previousLayoutEnabled;

        internal static bool RequiresTutorialAccess(UiAssistantPanelModel panel) =>
            panel.HasRecommendation &&
            (panel.TutorialStepCount == 9 && panel.TutorialStep is 2 or 3 or 4 or 6 ||
             panel.TutorialStepCount == 12 && panel.TutorialStep is 4 or 9);

        private void PreserveTutorial(AriaTutorialBriefingView aria)
        {
            guidedCanvas = aria.GetComponent<Canvas>();
            addedCanvas = guidedCanvas == null;
            if (addedCanvas) guidedCanvas = aria.gameObject.AddComponent<Canvas>();
            previousOverrideSorting = guidedCanvas.overrideSorting;
            previousSortingOrder = guidedCanvas.sortingOrder;
            previousSortingLayer = guidedCanvas.sortingLayerID;
            popupSortingCanvas = GetComponentInParent<Canvas>();
            while (popupSortingCanvas != null && !popupSortingCanvas.overrideSorting && !popupSortingCanvas.isRootCanvas)
                popupSortingCanvas = popupSortingCanvas.transform.parent?.GetComponentInParent<Canvas>();
            RaiseTutorialAbovePopup();
            guidedRaycaster = aria.GetComponent<GraphicRaycaster>();
            addedRaycaster = guidedRaycaster == null;
            if (addedRaycaster) guidedRaycaster = aria.gameObject.AddComponent<GraphicRaycaster>();

            // Keep the existing ARIA rail beside the existing drawer, above its scrim.
            // The section layout scales its authored contents into the available width.
            guidedDrawer = GetComponent<BuildDrawerView>()?.DrawerRoot?.transform as RectTransform;
            if (guidedDrawer != null) previousAnchorMax = guidedDrawer.anchorMax;
            if (guidedDrawer == null) return;
            guidedAria = (RectTransform)aria.transform;
            guidedLayout = guidedDrawer.GetComponentInChildren<MainMenuV3SectionLayoutView>();
            if (guidedLayout != null)
            {
                previousLayoutEnabled = guidedLayout.enabled;
                guidedLayout.enabled = false;
                FitGuidedDrawer();
            }
        }

        private void LateUpdate()
        {
            RaiseTutorialAbovePopup();
            FitGuidedDrawer();
        }

        private void RaiseTutorialAbovePopup()
        {
            if (guidedCanvas == null) return;
            // Local batching canvases inherit this sorting owner.
            guidedCanvas.overrideSorting = true;
            if (popupSortingCanvas != null) guidedCanvas.sortingLayerID = popupSortingCanvas.sortingLayerID;
            guidedCanvas.sortingOrder = (popupSortingCanvas != null ? popupSortingCanvas.sortingOrder : 0) + 1;
        }

        private void FitGuidedDrawer()
        {
            if (guidedDrawer == null || guidedLayout == null || guidedAria == null) return;
            // Opening motion changes the parent's transform after Configure; fit to the settled rail each frame.
            var parent = guidedDrawer.parent as RectTransform;
            if (parent == null || parent.rect.width <= 0f) return;
            guidedAria.GetWorldCorners(guideCorners);
            float left = parent.InverseTransformPoint(guideCorners[0]).x;
            float edge = Mathf.Clamp((left - parent.rect.xMin - 16f) / parent.rect.width, .6f, .85f);
            guidedDrawer.anchorMax = new Vector2(edge, previousAnchorMax.y);
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
            guidedAria = null;
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
                    guidedCanvas.sortingLayerID = previousSortingLayer;
                }
            }
            guidedRaycaster = null;
            guidedCanvas = null;
            popupSortingCanvas = null;
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
