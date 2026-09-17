using UnityEngine;
using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public sealed partial class MainMenuPlayUI : IBuildingPlacementViewportQuery
    {
        public Vector2 GetPlacementViewportCenter(bool isValid)
        {
            var bar = _buildPlacementConfirmationBarView;
            if (bar == null) return new Vector2(.5f, .6f);
            bar.Root.GetComponent<BuildPlacementConfirmationBarDesignLayoutView>()?.ApplyValidityState(isValid);
            Canvas.ForceUpdateCanvases();
            var safe = Screen.safeArea;
            float bottom = Mathf.Max(safe.yMin, PlacementScreenRect(bar.Root).yMax);
            float right = safe.xMax;
            var root = bar.transform.root;
            foreach (var aria in root.GetComponentsInChildren<AriaTutorialBriefingView>(true))
                if (aria.IsPresentationVisible)
                    right = Mathf.Min(right, PlacementScreenRect(aria.BriefingLayout).xMin);
            foreach (var map in root.GetComponentsInChildren<MatchHudMinimapView>(false))
                if (map.MapRect != null) right = Mathf.Min(right, PlacementScreenRect(map.MapRect).xMin);
            // Keep clear of the resource header as well as the footer and right rail.
            float top = safe.yMax - safe.height * .12f;
            return new Vector2((safe.xMin + Mathf.Max(right, safe.center.x)) * .5f / Screen.width,
                (Mathf.Min(bottom, safe.center.y) + top) * .5f / Screen.height);
        }

        private static Rect PlacementScreenRect(RectTransform rect)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = min;
            foreach (var corner in corners)
            { var point = RectTransformUtility.WorldToScreenPoint(camera, corner); min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
