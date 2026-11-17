using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        private const float SagaPortraitDropBudget = 0f;
        // Localised cue sizing can choose the clear shelf below the compact portrait.
        // Keep its marker on the facing coast, using all existing geometry predicates.
        private void ReseatSagaZoomMarker(TMP_Text cue, string selectedId, bool fa)
        {
            if (cue == null || cue.rectTransform.parent is not RectTransform parent)
                return;
            var ids = TheatreSeatGeometry.MapIds;
            var shapes = new TheatreSeatGeometry.Vec2[ids.Length][];
            var cards = new TheatreSeatGeometry.Box[ids.Length];
            var pins = new TheatreSeatGeometry.Box[ids.Length];
            int selected = System.Array.IndexOf(ids, selectedId);
            if (selected < 0)
                return;
            for (int i = 0; i < ids.Length; i++)
            {
                if (parent.Find("ZoneCard_" + ids[i])is not RectTransform card || parent.Find("ZonePin_" + ids[i])is not RectTransform pin || parent.Find("ZoneOutline_" + ids[i])is not RectTransform outline)
                    return;
                Rect cr = PlateRect(card), pr = PlateRect(pin);
                cards[i] = new TheatreSeatGeometry.Box(cr.x, cr.y, cr.width, cr.height);
                pins[i] = new TheatreSeatGeometry.Box(pr.x, pr.y, pr.width, pr.height);
                var points = OutlinePlatePoints(outline);
                shapes[i] = new TheatreSeatGeometry.Vec2[points.Length];
                for (int j = 0; j < points.Length; j++)
                    shapes[i][j] = new TheatreSeatGeometry.Vec2(points[j].x, points[j].y);
            }

            Rect q = PlateRect(cue.rectTransform), detail = SeatingDetailRect(parent.rect.width, parent.rect.height, fa);
            var selectedCard = cards[selected];
            float top = float.MaxValue, bottom = 0, left = float.MaxValue, right = 0;
            foreach (var point in shapes[selected])
            {
                top = Mathf.Min(top, point.Y);
                bottom = Mathf.Max(bottom, point.Y);
                left = Mathf.Min(left, point.X);
                right = Mathf.Max(right, point.X);
            }

            var choices = new List<Rect>();
            if (!q.Overlaps(new Rect(selectedCard.X, selectedCard.Y, selectedCard.W, selectedCard.H)))
                choices.Add(q);
            float gap = TheatreSeatGeometry.CueHexStroke + 3, h = Mathf.Max(56, q.height);
            foreach (float center in new[]
            {
                selectedCard.Center.X,
                pins[selected].Center.X
            }

            )
                foreach (float offset in new[]
                {
                    0f,
                    -32f,
                    32f,
                    -64f,
                    64f,
                    -128f,
                    128f,
                    -192f,
                    192f,
                    -256f,
                    256f
                }

                )
                {
                    float x = center - q.width * .5f + offset;
                    choices.Add(new Rect(x, bottom + gap, q.width, h));
                    choices.Add(new Rect(x, top - gap - h, q.width, h));
                }

            choices.Add(new Rect(left - gap - q.width, selectedCard.Center.Y - h * .5f, q.width, h));
            choices.Add(new Rect(right + gap, selectedCard.Center.Y - h * .5f, q.width, h));
            TheatreSeatGeometry.Box moved = default;
            bool found = false;
            foreach (var trial in choices)
                if (TheatreSeatGeometry.TryMoveSagaPinNearCue(shapes, cards, pins, new TheatreSeatGeometry.Box(trial.x, trial.y, trial.width, trial.height), selected, new TheatreSeatGeometry.Box(detail.x, detail.y, detail.width, detail.height), parent.rect.width, parent.rect.height, fa, out moved))
                {
                    q = trial;
                    found = true;
                    break;
                }

            if (!found)
                return;
            var marker = (RectTransform)parent.Find("ZonePin_" + selectedId);
            PlaceTopLeft(marker, moved.X, moved.Y, moved.W, moved.H);
            KeepAuthoredSize(marker);
            RefreshZoneLeader(parent, selectedId);
            PlaceTopLeft(cue.rectTransform, q.x, q.y, q.width, q.height);
            KeepAuthoredSize(cue.rectTransform);
            SyncCuePlate(parent, q);
            ReseatFramedCards(parent);
            cue.transform.SetAsLastSibling();
            AttachZoomPointer(cue.rectTransform);
        }
    }
}
