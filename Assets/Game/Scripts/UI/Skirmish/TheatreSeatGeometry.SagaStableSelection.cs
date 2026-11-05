using System;
using System.Collections.Generic;

namespace Game.UI.Runtime
{
    public static partial class TheatreSeatGeometry
    {
        private static bool _buildingStableSaga;
        private static bool TryArrangeStableSagaSelection(float width, float height, bool fa, bool reserve, string selected, out Layout result)
        {
            result = default;
            if (_buildingStableSaga || selected == "IB" || IndexOf(selected) < 0)
                return false;
            Layout basis;
            _buildingStableSaga = true;
            try
            {
                basis = Arrange(width, height, fa, reserve, "IB");
            }
            finally
            {
                _buildingStableSaga = false;
            }

            if (basis.Seats == null || !basis.CardsLegal || !basis.CueNear || !basis.HexAspectLegal)
                return false;
            var raw = new Vec2[MapIds.Length][];
            var cards = new Box[MapIds.Length];
            var pins = new Box[MapIds.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = CloneShape(basis.Seats[i].Hex);
                cards[i] = basis.Seats[i].Card;
                pins[i] = basis.Seats[i].Pin;
            }

            int selectedIndex = IndexOf(selected);
            var detail = DetailRect(width, height, fa, reserve);
            var hint = ZoomHintRect(width, height);
            var errors = SagaSeatViolations(raw, cards, pins, basis.Cue, "IB", width, height, fa, detail, hint);
            if (errors.Count > 0)
                return false;
            var coastOthers = (Vec2[][])raw.Clone();
            coastOthers[selectedIndex] = null;
            var coast = FinishTerritory(CloneShape(raw[selectedIndex]), cards[selectedIndex], pins[selectedIndex], selected, coastOthers, default, width, height, detail, hint);
            BoundsOf(coast, out float left, out float top, out float right, out float bottom);
            float w = fa ? 206 : 196, h = fa ? 56 : 48;
            var choices = new List<Box>();
            foreach (float x in new[]
            {
                cards[selectedIndex].Center.X,
                pins[selectedIndex].Center.X,
                (left + right) * .5f
            }

            )
            {
                float xx = Clamp(x - w * .5f, 12, width - w - 12);
                choices.Add(new Box(xx, top - CueHexStroke - 3 - h, w, h));
                choices.Add(new Box(xx, bottom + CueHexStroke + 3, w, h));
            }

            foreach (float y in new[]
            {
                cards[selectedIndex].Center.Y,
                pins[selectedIndex].Center.Y
            }

            )
            {
                choices.Add(new Box(left - CueHexStroke - 3 - w, y - h * .5f, w, h));
                choices.Add(new Box(right + CueHexStroke + 3, y - h * .5f, w, h));
            }

            var painted = new Vec2[raw.Length][];
            for (int i = 0; i < raw.Length; i++)
            {
                var rest = (Vec2[][])raw.Clone();
                rest[i] = null;
                painted[i] = FinishTerritory(CloneShape(raw[i]), cards[i], pins[i], MapIds[i], rest, i == IndexOf("IB") ? basis.Cue : default, width, height, detail, hint);
            }

            var expanded = new List<Box>();
            foreach (var spot in choices)
                for (int offset = -64; offset <= 64; offset += 16)
                {
                    bool horizontal = spot.Y < top || spot.Y > bottom;
                    expanded.Add(new Box(spot.X + (horizontal ? offset : 0), spot.Y + (horizontal ? 0 : offset), spot.W, spot.H));
                }

            // Native text fitting can need a shelf beyond the card's immediate
            // shoulder. Search those shelves before falling back inside a portrait.
            foreach (var spot in choices)
                for (int offset = 96; offset <= 256; offset += 32)
                    foreach (int direction in new[] { -1, 1 })
                    {
                        bool horizontal = spot.Y < top || spot.Y > bottom;
                        expanded.Add(new Box(spot.X + (horizontal ? offset * direction : 0), spot.Y + (horizontal ? 0 : offset * direction), spot.W, spot.H));
                    }
            choices = expanded;
            choices.Add(new Box(cards[selectedIndex].X + 4, cards[selectedIndex].Y + 6, Math.Max(PackedCueMinWidth, cards[selectedIndex].W - 8), 44));
            foreach (var cue in choices)
            {
                if (!OnPlate(cue, width, height) || !Clears(cue, detail))
                    continue;
                bool inside = CueInsideCard(cue, cards[selectedIndex]), clear = true;
                for (int i = 0; i < raw.Length; i++)
                    if ((i != selectedIndex || !inside) && (!FarsiPillClearsDistrict(cue, painted[i], false) || Overlaps(cue, cards[i], 0)))
                        clear = false;
                if (!clear)
                    continue;
                var trial = (Box[])pins.Clone();
                if (!CueCountsAsNear(cue, cards[selectedIndex], trial[selectedIndex], coast))
                {
                    var drawn = (Vec2[][])raw.Clone();
                    drawn[selectedIndex] = coast;
                    if (!TryMoveSagaPinNearCue(drawn, cards, trial, cue, selectedIndex, detail, width, height, fa, out var moved))
                        continue;
                    trial[selectedIndex] = moved;
                }

                var after = SagaSeatViolations(raw, cards, trial, cue, selected, width, height, fa, detail, hint);
                if (after.Count > 0)
                    continue;
                result = basis;
                result.Seats = (Seat[])basis.Seats.Clone();
                for (int i = 0; i < raw.Length; i++)
                    result.Seats[i].Hex = CloneShape(raw[i]);
                result.Seats[selectedIndex].Pin = trial[selectedIndex];
                result.SelectedMapId = selected;
                result.Cue = cue;
                result.CueNear = true;
                result.LogLines = BuildLog(fa ? "fa" : "en", result);
                return true;
            }

            return false;
        }
    }
}
