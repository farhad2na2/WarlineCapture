using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private Vector3 watchGroupCenter;
        private bool watchGroupValid;

        private void ObserveGroupMap(ref AriaSkirmishObservation view)
        {
            watchGroupValid = false;
            if (watchMap == null || watchRaycast == null || UnityEngine.EventSystems.EventSystem.current == null) return;
            // Find the largest cluster using contacts already drawn on the player's map.
            // Logistics outliers should not stretch a selection box across the HUD.
            int best = 0;
            Vector3 center = default;
            foreach (var candidate in watchMap.PresentedContacts)
            {
                if (candidate.Model.Allegiance != MatchHudMinimapMarkerAllegiance.Player) continue;
                int count = 0; Vector3 sum = default;
                foreach (var ally in watchMap.PresentedContacts)
                    if (ally.Model.Allegiance == MatchHudMinimapMarkerAllegiance.Player &&
                        (ally.Model.Position - candidate.Model.Position).sqrMagnitude <= 30 * 30)
                    { count++; sum += ally.Model.Position; }
                if (count <= best) continue;
                best = count; center = sum / count;
            }
            if (best < 2) return;
            watchGroupCenter = center; watchGroupValid = true;
            Vector2 mapPoint = default; int points = 0;
            foreach (var ally in watchMap.PresentedContacts)
            {
                if (ally.Model.Allegiance != MatchHudMinimapMarkerAllegiance.Player ||
                    ally.Marker == null || (ally.Model.Position - center).sqrMagnitude > 30 * 30) continue;
                mapPoint += RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(watchMap.MapImage), ally.Marker.position);
                points++;
            }
            if (points == 0) return;
            mapPoint /= points;
            watchRaycast.position = mapPoint; watchHits.Clear();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(watchRaycast, watchHits);
            bool reachable = watchHits.Count > 0 && watchHits[0].gameObject.GetComponentInParent<MatchHudMinimapView>() == watchMap;
            view.FocusGroup = new AriaTouchTarget { Id = view.MapOpen ? -20008 : -20007, Position = mapPoint, Available = reachable };
            if (!view.MapOpen || !reachable || watchMap.ViewportRect == null ||
                !RectTransformUtility.RectangleContainsScreenPoint(watchMap.ViewportRect, mapPoint, ResolveEventCamera(watchMap.MapImage))) return;
            var viewport = watchMap.ViewportRect;
            var start = RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(watchMap.MapImage), viewport.TransformPoint(viewport.rect.center));
            view.FocusGroup.Position = start; view.FocusGroupDrag = true;
            view.FocusGroupDragEnd = (start - mapPoint).sqrMagnitude > 25 ? mapPoint : mapPoint + Vector2.up * 12;
        }
    }
}
