using System;
using System.Collections.Generic;

namespace Game.UI.Runtime
{
    public static partial class TheatreSeatGeometry
    {
        public const float SagaReferenceHeight = 1080f;
        public static void ResolveSagaFrame(float parentWidth, float parentHeight, out float width, out float height, out float scale)
        {
            height = SagaReferenceHeight;
            scale = Math.Max(1f, parentHeight) / height;
            width = Math.Max(1f, parentWidth) / scale;
        }

        public static float SagaSummaryWidth(float width, float height) => width / Math.Max(1f, height) < MapPanelNarrowAspect ? Math.Max(280f, Math.Min(380f, width * .26f)) : Math.Max(300f, Math.Min(480f, width * .20f));
        public static float SagaDetailReserve(float width) => width < 1200f ? Math.Max(230f, Math.Min(290f, width * .22f)) : Math.Max(230f, Math.Min(460f, width * .27f));
        private static float SagaCoastOccupancy(Vec2[] shape)
        {
            BoundsOf(shape, out float left, out float top, out float right, out float bottom);
            double area = 0;
            for (int i = 0; i < shape.Length; i++)
            {
                var a = shape[i];
                var b = shape[(i + 1) % shape.Length];
                area += a.X * b.Y - b.X * a.Y;
            }

            return (float)(Math.Abs(area) * .5 / Math.Max(1, (right - left) * (bottom - top)));
        }

        private static bool SagaCueChromeHolds(Layout layout, float width, float height, bool fa, bool reserve)
        {
            int selected = IndexOf(layout.SelectedMapId);
            return layout.Seats != null && layout.Cue.W >= PackedCueMinWidth && layout.Cue.H >= 36f && OnPlate(layout.Cue, width, height) && Clears(layout.Cue, DetailRect(width, height, fa, reserve)) && CueCountsAsNear(layout.Cue, layout.Seats[selected].Card, layout.Seats[selected].Pin, layout.Seats[selected].Hex);
        }

        public static string SagaCueRepairReport = "";
        public static bool TryMoveSagaPinNearCue(Vec2[][] shapes, Box[] cards, Box[] pins, Box cue, int selected, Box detail, float width, float height, bool fa, out Box moved)
        {
            moved = pins[selected];
            SagaCueRepairReport = "already-near";
            if (!OnPlate(cue, width, height) || !Clears(cue, detail))
                return false;
            for (int i = 0; i < shapes.Length; i++)
            {
                if (!FarsiPillClearsDistrict(cue, shapes[i], i == selected)
                    || (i == selected && PolygonHitsRect(shapes[i], cue)))
                    return false;
            }
            var hint = ZoomHintRect(width, height);
            var before = SagaSeatViolations(shapes, cards, pins, cue, MapIds[selected], width, height, fa, detail, hint);
            foreach (string failure in before)
                if (failure.Contains("zoom") && !failure.Contains("far from the selected pin"))
                    return false;
            if (CueCountsAsNear(cue, cards[selected], moved, shapes[selected]))
                return true;
            Box original = pins[selected], card = cards[selected];
            BoundsOf(shapes[selected], out float left, out float top, out float right, out float bottom);
            var candidates = new List<Box>();
            // The marker is decorative; the whole district remains the touch target.
            // Prefer the existing size, then compact sizes that fit the facing coast.
            foreach (float size in new[]
            {
                original.W,
                28f,
                24f
            }

            )
            {
                float markerW = Math.Min(original.W, size), markerH = Math.Min(original.H, size);
                for (float y = top; y <= bottom - markerH; y += 4)
                    for (float x = left; x <= right - markerW; x += 4)
                    {
                        var trial = new Box(x, y, markerW, markerH);
                        if (Leader(trial, card) < LeaderMin || Separation(trial, card) < PinGap || !CueCountsAsNear(cue, card, trial, shapes[selected]) || !RestsInside(trial, shapes[selected], 0))
                            continue;
                        candidates.Add(trial);
                    }

                candidates.Sort((a, b) => Distance(a.Center, cue.Center).CompareTo(Distance(b.Center, cue.Center)));
                foreach (var trial in candidates)
                {
                    pins[selected] = trial;
                    var after = SagaSeatViolations(shapes, cards, pins, cue, MapIds[selected], width, height, fa, detail, hint);
                    if (after.Count <= before.Count && SagaNoNewViolations(before, after))
                    {
                        moved = trial;
                        SagaCueRepairReport = "placed marker=" + trial.X + "," + trial.Y + "," + trial.W + "," + trial.H;
                        return true;
                    }

                    pins[selected] = original;
                }

                candidates.Clear();
            }

            SagaCueRepairReport = "No legal facing marker for " + MapIds[selected];
            return false;
        }

        public static bool TryParkSagaCue(Vec2[][] hexes, Box[] pins, Box[] cards, int selected, Box detail, float width, float height, bool fa, out Box cue)
        {
            cue = default;
            if (cards == null || selected < 0 || selected >= cards.Length)
                return false;
            return TrySpotSelectedCue(hexes, pins, cards, ref cue, selected, detail, width, height, fa, cards[selected], fa ? 206 : 196, fa ? 56 : 48);
        }

        // Recheck the drawn coast after the legacy cue and territory passes. A late cue move
        // must not leave an otherwise legal portrait visibly off centre.
        private static void FinalizeSagaSeats(ref Layout layout, float width, float height, bool fa, bool reserve)
        {
            if (layout.Seats == null)
                return;
            var hexes = new Vec2[layout.Seats.Length][];
            var cards = new Box[layout.Seats.Length];
            var pins = new Box[layout.Seats.Length];
            for (int i = 0; i < layout.Seats.Length; i++)
            {
                hexes[i] = CloneShape(layout.Seats[i].Hex);
                cards[i] = layout.Seats[i].Card;
                pins[i] = layout.Seats[i].Pin;
            }

            Box detail = DetailRect(width, height, fa, reserve), hint = ZoomHintRect(width, height);
            var failures = SagaSeatViolations(hexes, cards, pins, layout.Cue, layout.SelectedMapId, width, height, fa, detail, hint);
            if (failures.Count == 0)
            {
                layout.CardsLegal = layout.HexAspectLegal = layout.CueNear = true;
                return;
            }

            bool changed = false;
            Box workingCue = layout.Cue;
            int selection = IndexOf(layout.SelectedMapId);
            for (int i = 0; i < cards.Length; i++)
            {
                if (!failures.Exists(x => x.Contains("region centre map=" + MapIds[i] + " ")))
                    continue;
                var others = (Vec2[][])hexes.Clone();
                others[i] = null;
                var coast = FinishTerritory(CloneShape(hexes[i]), cards[i], pins[i], MapIds[i], others, layout.SelectedMapId == MapIds[i] ? layout.Cue : default, width, height, detail, hint);
                if (!CoastVisualCentre(coast, out float cx, out float cy))
                    continue;
                Box original = cards[i], originalPin = pins[i];
                bool found = false;
                int probes = 0;
                for (int radius = 0; radius <= 18 && !found && probes < 16; radius += 6)
                    for (int dx = -radius; dx <= radius && !found && probes < 16; dx += 6)
                        for (int dy = -radius; dy <= radius && !found && probes < 16; dy += 6)
                        {
                            var candidate = new Box(cx - original.W * .5f + dx, cy - original.H * .5f + dy, original.W, original.H);
                            if (!SeatHolds(candidate, hexes[i], hexes) || !Clears(candidate, detail) || !OnPlate(candidate, width, height))
                                continue;
                            var pinChoices = new[]
                            {
                                originalPin,
                                new Box(candidate.X - originalPin.W - 20, candidate.Center.Y - originalPin.H * .5f, originalPin.W, originalPin.H),
                                new Box(candidate.XMax + 20, candidate.Center.Y - originalPin.H * .5f, originalPin.W, originalPin.H),
                                new Box(candidate.Center.X - originalPin.W * .5f, candidate.Y - originalPin.H - 20, originalPin.W, originalPin.H),
                                new Box(candidate.Center.X - originalPin.W * .5f, candidate.YMax + 20, originalPin.W, originalPin.H)
                            };
                            foreach (var pin in pinChoices)
                            {
                                if (Leader(pin, candidate) < LeaderMin)
                                    continue;
                                if (probes++ >= 16)
                                    break;
                                cards[i] = candidate;
                                pins[i] = pin;
                                Box candidateCue = workingCue;
                                if (i == selection && (CueInsideCard(workingCue, original) || CueIsCardTopTab(original, workingCue)))
                                    candidateCue = ShiftBox(workingCue, candidate.X - original.X, candidate.Y - original.Y);
                                var next = SagaSeatViolations(hexes, cards, pins, candidateCue, layout.SelectedMapId, width, height, fa, detail, hint);
                                if (next.Count < failures.Count && SagaNoNewViolations(failures, next))
                                {
                                    failures = next;
                                    workingCue = candidateCue;
                                    changed = found = true;
                                    break;
                                }

                                cards[i] = original;
                                pins[i] = originalPin;
                            }
                        }
            }

            if (!changed)
            {
                RepairSagaCue(ref layout, width, height, fa, reserve);
                return;
            }

            for (int i = 0; i < cards.Length; i++)
            {
                layout.Seats[i].Card = cards[i];
                layout.Seats[i].Pin = pins[i];
            }

            layout.Cue = workingCue;
            layout.CueNear = CueCountsAsNear(workingCue, cards[selection], pins[selection], hexes[selection]);
            layout.LogLines = BuildLog(fa ? "fa" : "en", layout);
            RepairSagaCue(ref layout, width, height, fa, reserve);
        }

        private static List<string> SagaSeatViolations(Vec2[][] shapes, Box[] cards, Box[] pins, Box cue, string selected, float width, float height, bool fa, Box detail, Box hint)
        {
            var failures = new List<string>();
            CollectViolations(fa ? "fa" : "en", width, height, selected, shapes, (Box[])cards.Clone(), (Box[])pins.Clone(), cue, cue.W > 1f, detail, hint, true, failures);
            return failures;
        }

        private static bool SagaNoNewViolations(List<string> before, List<string> after)
        {
            foreach (string failure in after)
                if (!before.Contains(failure))
                    return false;
            return true;
        }

        /// <summary>The compact reserve path returns early, so it also needs the generic selected-card cue repair.</summary>
        private static void FinalizeCompactSagaCue(ref Layout layout, float width, float height, bool fa, int selected)
        {
            if (layout.Seats == null)
                return;
            var hexes = new Vec2[layout.Seats.Length][];
            var cards = new Box[layout.Seats.Length];
            var pins = new Box[layout.Seats.Length];
            for (int i = 0; i < layout.Seats.Length; i++)
            {
                hexes[i] = CloneShape(layout.Seats[i].Hex);
                cards[i] = layout.Seats[i].Card;
                pins[i] = layout.Seats[i].Pin;
            }

            Box cue = layout.Cue;
            Box detail = DetailRect(width, height, fa, true);
            if (!TryParkSelectedCue(hexes, pins, cards, ref cue, selected, detail, width, height, fa))
                return;
            bool priorFloor = CompactFloorActive;
            CompactFloorActive = true;
            bool legal = CompactSeatsLegal(hexes, cards, pins, cue, detail, width, height);
            CompactFloorActive = priorFloor;
            bool aspects = AspectsHold(hexes, RatioPlateWidth, RatioPlateHeight) && DistrictsLegal(hexes, pins, width, height);
            if (!legal || !aspects)
                return;
            for (int i = 0; i < layout.Seats.Length; i++)
            {
                layout.Seats[i].Hex = hexes[i];
                layout.Seats[i].Card = cards[i];
                layout.Seats[i].Pin = pins[i];
                layout.Seats[i].Legal = SeatHolds(cards[i], hexes[i], hexes) && Clears(cards[i], detail) && OnPlate(cards[i], width, height);
            }

            layout.Cue = cue;
            layout.CueNear = CueCountsAsNear(cue, cards[selected], pins[selected], hexes[selected]);
            layout.CardsLegal = legal;
            layout.HexAspectLegal = aspects;
            layout.LogLines = BuildLog(fa ? "fa" : "en", layout);
        }
    }
}
