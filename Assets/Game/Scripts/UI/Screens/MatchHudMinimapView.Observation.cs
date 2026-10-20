using System.Collections.Generic;
using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MatchHudMinimapView
    {
        // Exactly the markers rendered on the player’s map, never the simulation roster.
        internal MatchHudMinimapProjectionGrid PresentedProjection;
        public bool TryGetPresentedMapPoint(Vector3 world, Camera eventCamera, out Vector2 point)
        {
            point = default;
            if (!isActiveAndEnabled || MapRect == null || !MapRect.gameObject.activeInHierarchy ||
                !float.IsFinite(PresentedProjection.Width) || !float.IsFinite(PresentedProjection.Height) ||
                PresentedProjection.Width <= 0 || PresentedProjection.Height <= 0 ||
                !MatchHudMinimapProjectionUiSystemHelper.TryWorldToNormalized(PresentedProjection, world, out var normalized) ||
                !float.IsFinite(normalized.x) || !float.IsFinite(normalized.y) ||
                normalized.x < 0 || normalized.x > 1 || normalized.y < 0 || normalized.y > 1) return false;
            var rect = MapRect.rect;
            if (!float.IsFinite(rect.xMin) || !float.IsFinite(rect.yMin) ||
                !float.IsFinite(rect.width) || !float.IsFinite(rect.height) || rect.width <= 0 || rect.height <= 0) return false;
            // Positive local dimensions can still project to a collapsed map while
            // its transform/layout is being rebuilt. Do not report its center as a
            // presented command point until the screen surface has positive area.
            Vector2 a = ProjectMapPoint(new Vector3(rect.xMin, rect.yMin, 0), eventCamera);
            Vector2 b = ProjectMapPoint(new Vector3(rect.xMax, rect.yMin, 0), eventCamera);
            Vector2 c = ProjectMapPoint(new Vector3(rect.xMin, rect.yMax, 0), eventCamera);
            float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            if (!FiniteMapPoint(a) || !FiniteMapPoint(b) || !FiniteMapPoint(c) || !float.IsFinite(area) || area == 0) return false;
            Vector2 projected = ProjectMapPoint(new Vector3(rect.xMin + normalized.x * rect.width,
                rect.yMin + normalized.y * rect.height, 0), eventCamera);
            if (!FiniteMapPoint(projected)) return false;
            point = projected;
            return true;
        }
        private Vector2 ProjectMapPoint(Vector3 local, Camera eventCamera)
            => RectTransformUtility.WorldToScreenPoint(eventCamera, MapRect.TransformPoint(local));
        private static bool FiniteMapPoint(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
        internal bool HasCurrentPresentedMapArea(Camera eventCamera)
        {
            var grid = PresentedProjection;
            return TryGetPresentedMapPoint(grid.Origin + new Vector3(grid.Width * .5f,0,grid.Height * .5f),
                eventCamera, out _);
        }
        internal bool IsContactCurrentlyPresented(in PresentedMapContact contact, Camera eventCamera)
        {
            var marker = contact.Marker;
            var image = contact.Image;
            var root = MarkerRoot != null ? MarkerRoot : MapRect;
            if (!isActiveAndEnabled || root == null || marker == null || image == null ||
                !marker.IsChildOf(root) || !marker.gameObject.activeInHierarchy || !image.isActiveAndEnabled ||
                image.canvasRenderer.cull || image.color.a <= 0 || image.canvasRenderer.GetAlpha() <= 0 ||
                marker.rect.width <= 0 || marker.rect.height <= 0 || !HasCurrentPresentedMapArea(eventCamera)) return false;
            for (Transform node = marker; node != null; node = node.parent)
            {
                if (!node.TryGetComponent<CanvasGroup>(out var group) || !group.enabled) continue;
                if (group.alpha <= 0) return false;
                if (group.ignoreParentGroups) break;
            }
            var point = RectTransformUtility.WorldToScreenPoint(eventCamera, marker.TransformPoint(marker.rect.center));
            return FiniteMapPoint(point) && Screen.safeArea.Contains(point) &&
                RectTransformUtility.RectangleContainsScreenPoint(MapRect, point, eventCamera);
        }

        internal uint PresentedMarkerRevision;
        internal int PresentedMarkerFrame;
        internal float PresentedMarkerTime;
        internal bool PresentedMarkersComplete;
        internal void BeginPresentedMarkerUpdate()
        {
            PresentedMarkersComplete = false;
            PresentedMarkerFrame = 0;
        }
        internal void CompletePresentedMarkerUpdate()
        {
            unchecked { PresentedMarkerRevision++; if (PresentedMarkerRevision == 0) PresentedMarkerRevision++; }
            PresentedMarkerFrame = Time.frameCount;
            PresentedMarkerTime = Time.unscaledTime;
            PresentedMarkersComplete = true;
        }
        internal void InvalidatePresentedMarkerUpdate()
        {
            PresentedMarkersComplete = false;
            PresentedMarkerFrame = 0;
            PresentedMarkerTime = float.NegativeInfinity;
        }
        internal readonly List<PresentedMapContact> PresentedContacts = new();
        internal readonly struct PresentedMapContact
        {
            internal readonly MatchHudMinimapMarkerModel Model;
            internal readonly RectTransform Marker;
            internal readonly UnityEngine.UI.Image Image;
            internal PresentedMapContact(MatchHudMinimapMarkerModel model, RectTransform marker)
            { Model = model; Marker = marker; Image = marker != null ? marker.GetComponent<UnityEngine.UI.Image>() : null; }
        }
        internal void RecordPresentedContact(MatchHudMinimapMarkerModel model, RectTransform marker)
            => PresentedContacts.Add(new PresentedMapContact(model, marker));
    }
}
