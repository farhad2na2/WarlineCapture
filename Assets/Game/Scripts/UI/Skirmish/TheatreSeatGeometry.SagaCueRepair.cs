using System;
using System.Collections.Generic;

namespace Game.UI.Runtime
{
    public static partial class TheatreSeatGeometry
    {
        private static void RepairSagaCue(ref Layout layout, float width, float height, bool fa, bool reserve)
        {
            var raw = new Vec2[MapIds.Length][];
            var cards = new Box[MapIds.Length];
            var pins = new Box[MapIds.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = CloneShape(layout.Seats[i].Hex);
                cards[i] = layout.Seats[i].Card;
                pins[i] = layout.Seats[i].Pin;
            }

            int selected = IndexOf(layout.SelectedMapId);
            var detail = DetailRect(width, height, fa, reserve);
            var hint = ZoomHintRect(width, height);
            var before = SagaSeatViolations(raw, cards, pins, layout.Cue, layout.SelectedMapId, width, height, fa, detail, hint);
            if (!before.Exists(x => x.Contains("zoom pill overlaps a district")))
                return;
            var others = (Vec2[][])raw.Clone();
            others[selected] = null;
            var coast = FinishTerritory(CloneShape(raw[selected]), cards[selected], pins[selected], MapIds[selected], others, layout.Cue, width, height, detail, hint);
            BoundsOf(coast, out float left, out float top, out float right, out float bottom);
            float w = fa ? 206 : 196, h = fa ? 56 : 48;
            var choices = new List<Box>();
            foreach (float x in new[]
            {
                cards[selected].Center.X,
                pins[selected].Center.X,
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
                cards[selected].Center.Y,
                pins[selected].Center.Y
            }

            )
            {
                choices.Add(new Box(left - CueHexStroke - 3 - w, y - h * .5f, w, h));
                choices.Add(new Box(right + CueHexStroke + 3, y - h * .5f, w, h));
            }

            foreach (var cue in choices)
            {
                if (!OnPlate(cue, width, height) || !Clears(cue, detail))
                    continue;
                var trial = (Box[])pins.Clone();
                if (!CueCountsAsNear(cue, cards[selected], trial[selected], coast))
                {
                    var drawn = (Vec2[][])raw.Clone();
                    drawn[selected] = coast;
                    if (!TryMoveSagaPinNearCue(drawn, cards, trial, cue, selected, detail, width, height, fa, out var moved))
                        continue;
                    trial[selected] = moved;
                }

                var after = SagaSeatViolations(raw, cards, trial, cue, layout.SelectedMapId, width, height, fa, detail, hint);
                if (after.Count < before.Count && SagaNoNewViolations(before, after))
                {
                    layout.Cue = cue;
                    layout.CueNear = true;
                    layout.Seats[selected].Pin = trial[selected];
                    layout.LogLines = BuildLog(fa ? "fa" : "en", layout);
                    return;
                }
            }
        }
    }
}
