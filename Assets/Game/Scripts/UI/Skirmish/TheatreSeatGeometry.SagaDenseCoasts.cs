using System;
using System.Collections.Generic;

namespace Game.UI.Runtime
{
    public static partial class TheatreSeatGeometry
    {
        // Authored UI geography at the narrowest supported saga viewport. Shared
        // boundaries let the five districts occupy two thirds of the board while
        // preserving the existing aspect, portrait, leader and painted-halo gates.
        // These are menu overlays; no Campaign map geometry is generated or changed.
        private const float SagaCoastReferenceWidth = 852.088f;
        private const float SagaCoastReferenceHeight = 844f;
        private static readonly Vec2[][] SagaDistrictShelves =
        {
            new[] { new Vec2(24.00000f, 403.12100f), new Vec2(24.00000f, 24.00000f), new Vec2(522.48694f, 24.00000f), new Vec2(315.89779f, 403.12698f) },
            new[] { new Vec2(551.27643f, 457.65060f), new Vec2(373.22977f, 433.76534f), new Vec2(364.78661f, 425.21946f), new Vec2(583.41412f, 24.00000f), new Vec2(828.08801f, 24.00000f), new Vec2(828.08801f, 456.62124f) },
            new[] { new Vec2(24.00000f, 456.62100f), new Vec2(320.60963f, 456.62707f), new Vec2(329.84748f, 465.97732f), new Vec2(220.40783f, 778.00000f), new Vec2(24.00000f, 778.00000f) },
            new[] { new Vec2(580.39914f, 511.04268f), new Vec2(828.08801f, 510.12161f), new Vec2(828.08801f, 778.00000f), new Vec2(618.82391f, 778.00000f) },
            new[] { new Vec2(378.65296f, 488.47213f), new Vec2(525.94314f, 508.23136f), new Vec2(564.77256f, 778.00000f), new Vec2(277.10320f, 778.00000f) }
        };

        private static bool TryArrangeDenseSaga(float width, float height, bool fa,
            bool reserve, string selectedId, out Layout layout)
        {
            layout = default;
            Box view = OpenViewport(width, height, fa, reserve);
            int selected = IndexOf(selectedId);
            if (!reserve || height < 820f || view.W < 820f || selected < 0)
                return false;
            RatioPlateWidth = view.W;
            RatioPlateHeight = view.H;
            CompactCoast = false;
            var shapes = new Vec2[MapIds.Length][];
            var cards = new Box[MapIds.Length];
            var pins = new Box[MapIds.Length];
            for (int i = 0; i < MapIds.Length; i++)
            {
                shapes[i] = BuildSagaShelfCoast(i, view, height);
                if (!CoastVisualCentre(shapes[i], out float cx, out float cy))
                    return false;
                float cardWidth = view.W < 960f ? 136f : 160f;
                cards[i] = new Box(cx - cardWidth * .5f, cy - 96f, cardWidth, 192f);
                if (!SeatSagaShelfCard(ref cards[i], shapes[i], cx, cy))
                    return false;
                if (!PlaceSagaShelfPin(shapes[i], cards[i], out pins[i]))
                    return false;
            }

            // The zoom affordance occupies the selected portrait's preview rather
            // than covering its title or forcing a large empty shelf between regions.
            Box card = cards[selected];
            Box cue = new Box(card.X + 4f, card.Y + (fa ? 82f : 102f), card.W - 8f, fa ? 64f : 44f);
            Box detail = DetailRect(width, height, fa, reserve);
            Box hint = ZoomHintRect(width, height);
            var failures = SagaSeatViolations(shapes, cards, pins, cue, selectedId,
                width, height, fa, detail, hint);
            if (failures.Count != 0 || !DrawnGlowGapsHold(shapes))
                return false;
            layout = new Layout
            {
                Seats = new Seat[MapIds.Length], SelectedMapId = selectedId,
                Cue = cue, CardsLegal = true, CueNear = true, HexAspectLegal = true
            };
            for (int i = 0; i < MapIds.Length; i++)
                layout.Seats[i] = new Seat
                { MapId = MapIds[i], Card = cards[i], Pin = pins[i], Hex = shapes[i], Legal = true };
            layout.LogLines = BuildLog(fa ? "fa" : "en", layout);
            return true;
        }

        private static bool SeatSagaShelfCard(ref Box card, Vec2[] coast, float cx, float cy)
        {
            if (RestsInside(card, coast, InsideMargin))
                return true;
            for (int radius = 6; radius <= 18; radius += 6)
                for (int dx = -radius; dx <= radius; dx += 6)
                    for (int dy = -radius; dy <= radius; dy += 6)
                    {
                        var trial = new Box(cx - card.W * .5f + dx, cy - card.H * .5f + dy, card.W, card.H);
                        if (!RestsInside(trial, coast, InsideMargin))
                            continue;
                        card = trial;
                        return true;
                    }
            return false;
        }

        private static bool PlaceSagaShelfPin(Vec2[] coast, Box card, out Box pin)
        {
            pin = default;
            BoundsOf(coast, out float left, out float top, out float right, out float bottom);
            var preferred = new Vec2(card.X - 36f, card.Y + 30f);
            float best = float.MaxValue;
            for (float y = top + 10f; y < bottom - 34f; y += 6f)
                for (float x = left + 10f; x < right - 34f; x += 6f)
                {
                    var trial = new Box(x, y, 24f, 24f);
                    if (!RestsInside(trial, coast, 6f) || Separation(trial, card) < PinGap
                        || Leader(trial, card) < 28f)
                        continue;
                    float cost = Distance(trial.Center, preferred);
                    if (cost >= best)
                        continue;
                    best = cost;
                    pin = trial;
                }
            return pin.W > 1f;
        }

        private static Vec2[] BuildSagaShelfCoast(int district, Box view, float height)
        {
            Vec2[] shelf = SagaDistrictShelves[district];
            var coast = new List<Vec2>();
            for (int i = 0; i < shelf.Length; i++)
            {
                Vec2 previous = shelf[(i + shelf.Length - 1) % shelf.Length];
                Vec2 corner = shelf[i], next = shelf[(i + 1) % shelf.Length];
                float incoming = Distance(previous, corner), outgoing = Distance(corner, next);
                float trim = Math.Min(36f, Math.Min(incoming, outgoing) * .24f);
                var start = new Vec2(corner.X + (previous.X - corner.X) * trim / incoming,
                    corner.Y + (previous.Y - corner.Y) * trim / incoming);
                var end = new Vec2(corner.X + (next.X - corner.X) * trim / outgoing,
                    corner.Y + (next.Y - corner.Y) * trim / outgoing);
                for (int sample = 0; sample < 4; sample++)
                {
                    float t = sample / 4f, u = 1f - t;
                    coast.Add(new Vec2(u * u * start.X + 2f * t * u * corner.X + t * t * end.X,
                        u * u * start.Y + 2f * t * u * corner.Y + t * t * end.Y));
                }
                Vec2 afterNext = shelf[(i + 2) % shelf.Length];
                float nextTrim = Math.Min(36f, Math.Min(outgoing, Distance(next, afterNext)) * .24f);
                var edgeEnd = new Vec2(next.X + (corner.X - next.X) * nextTrim / outgoing,
                    next.Y + (corner.Y - next.Y) * nextTrim / outgoing);
                float length = Distance(end, edgeEnd);
                float nx = -(edgeEnd.Y - end.Y) / length, ny = (edgeEnd.X - end.X) / length;
                int steps = Math.Max(2, (int)(length / 15f));
                for (int sample = 0; sample < steps; sample++)
                {
                    float t = sample / (float)steps;
                    float wave = (float)(3f + 7f * Math.Pow(Math.Sin(t * length / 12f + district + i), 2));
                    wave *= Math.Min(1f, Math.Min(t, 1f - t) * 8f);
                    coast.Add(new Vec2(end.X + (edgeEnd.X - end.X) * t + nx * wave,
                        end.Y + (edgeEnd.Y - end.Y) * t + ny * wave));
                }
            }
            var result = coast.ToArray();
            for (int i = 0; i < result.Length; i++)
                result[i] = new Vec2(view.X + result[i].X / SagaCoastReferenceWidth * view.W,
                    result[i].Y / SagaCoastReferenceHeight * height);
            return result;
        }

        // Finishing must be idempotent for a coast that already satisfies every
        // drawn-frame gate. Re-fitting an accepted coast was discarding its area.
        private static bool SagaCoastAlreadyFinished(Vec2[] coast, Box card, Box pin,
            Vec2[][] others, Box cue, float width, float height, Box detail, Box hint, float expected)
        {
            return FrameAndCentre(coast, card, width, height, detail, hint, expected)
                && !TerritoryReadsRectangular(coast)
                && InteriorUnblocked(coast, card, pin, cue, hint) >= 8
                && NeighbourGapsHold(coast, coast, others);
        }
    }
}
