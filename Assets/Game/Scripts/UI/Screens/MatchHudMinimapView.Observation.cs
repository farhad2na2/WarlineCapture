using System.Collections.Generic;
using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MatchHudMinimapView
    {
        // Exactly the markers rendered on the player’s map, never the simulation roster.
        internal MatchHudMinimapProjectionGrid PresentedProjection;
        internal bool TryGetPresentedMapPoint(Vector3 world, Camera eventCamera, out Vector2 point)
        {
            point = default;
            if (MapRect == null || PresentedProjection.Width <= 0 || PresentedProjection.Height <= 0 ||
                !MatchHudMinimapProjectionUiSystemHelper.TryWorldToNormalized(PresentedProjection, world, out var normalized) ||
                normalized.x < 0 || normalized.x > 1 || normalized.y < 0 || normalized.y > 1) return false;
            var rect = MapRect.rect;
            point = RectTransformUtility.WorldToScreenPoint(eventCamera, MapRect.TransformPoint(
                new Vector3(rect.xMin + normalized.x * rect.width, rect.yMin + normalized.y * rect.height, 0)));
            return true;
        }
        internal readonly List<PresentedMapContact> PresentedContacts = new();
        internal readonly struct PresentedMapContact
        {
            internal readonly MatchHudMinimapMarkerModel Model;
            internal readonly RectTransform Marker;
            internal PresentedMapContact(MatchHudMinimapMarkerModel model, RectTransform marker)
            { Model = model; Marker = marker; }
        }
        internal void RecordPresentedContact(MatchHudMinimapMarkerModel model, RectTransform marker)
            => PresentedContacts.Add(new PresentedMapContact(model, marker));
    }
}
