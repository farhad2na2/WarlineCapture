using System.Collections.Generic;
using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MatchHudMinimapView
    {
        // Exactly the markers rendered on the player’s map, never the simulation roster.
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
