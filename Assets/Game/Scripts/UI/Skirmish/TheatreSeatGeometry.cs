using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Plate-local theatre seating. No Unity types: the editor capture converts world
    /// corners into this same y-down space and calls <see cref="SeatHolds"/>.
    /// A seat this accepts is the seat the straddle check accepts.
    /// </summary>
    public static class TheatreSeatGeometry
    {
        public struct Vec2
        {
            public float X;
            public float Y;

            public Vec2(float x, float y)
            {
                X = x;
                Y = y;
            }
        }

        public struct Box
        {
            public float X;
            public float Y;
            public float W;
            public float H;

            public Box(float x, float y, float w, float h)
            {
                X = x;
                Y = y;
                W = w;
                H = h;
            }

            public float XMin { get { return X; } }
            public float YMin { get { return Y; } }
            public float XMax { get { return X + W; } }
            public float YMax { get { return Y + H; } }
            public Vec2 Center { get { return new Vec2(X + W * 0.5f, Y + H * 0.5f); } }
        }

        public struct Seat
        {
            public string MapId;
            public Box Card;
            public Box Pin;
            public Vec2[] Hex;
            public bool Legal;
        }

        public struct Layout
        {
            public Seat[] Seats;
            public Box Cue;
            public string SelectedMapId;
            public bool CardsLegal;
            public bool CueNear;
            public bool HexAspectLegal;
            public string[] LogLines;
        }

        private struct Spec
        {
            public float ReferenceWidth;
            public float CenterX;
            public float CenterY;
            public float HalfWidth;
            public float HalfHeight;
        }

        public static readonly string[] MapIds = { "DB", "CC", "MP", "IB", "AP" };

        private static readonly Vec2[] LabelCentres =
        {
            new Vec2(0.25f, 0.15f),
            new Vec2(0.75f, 0.15f),
            new Vec2(0.24f, 0.48f),
            new Vec2(0.74f, 0.48f),
            new Vec2(0.52f, 0.78f)
        };

        private const float FocalX = 0.50f;
        private const float FocalY = 0.465f;
        private const float SourceAspect = 16f / 9f;
        private const float SafeLeft = 28f;
        private const float SafeTop = 28f;
        private const float SafeRight = 28f;
        private const float SafeBottom = 40f;
        /// <summary>ZoneHexPad. The outline rect is the hex bounds plus this pad. Capture measures that rect.</summary>
        public const float OutlinePad = 6f;
        /// <summary>
        /// ZoomHint on the preview, in plate y-down. The strip top is 96 and the hint sits 42px above it,
        /// 36px tall, from x=36 to the right edge. On a 724px clip that is y 678–714, clear of the runway pin.
        /// </summary>
        public const float ZoomHintInsetX = 36f;
        public const float ZoomHintHeight = 36f;
        public const float ZoomHintTopGap = 46f;
        /// <summary>RequireTheaterOverlay inset. Corners must sit inside the clip shrunk by this, with <see cref="MapInsetSlop"/>.</summary>
        public const float MapInset = 12f;
        public const float MapInsetSlop = 2f;
        /// <summary>Height the v35 halves were authored at. Capture's aspect check divides by this.</summary>
        private const float V35AuthorHeight = 620f;
        private const float PinSize = 34f;
        private const float Edge = 12f;
        private const float CardGap = 14f;
        private const float PinGap = 12f;
        private const float InsideMargin = 8f;
        private const float OutsideGap = 1f;
        private const float LeaderMin = 25f;
        private const float CueNearLimit = 160f;
        private const float CueWidth = 220f;
        private const float CueHeight = 44f;

        public static Vec2[] DistrictHex(string mapId, float frameWidth, float frameHeight)
        {
            int index = IndexOf(mapId);
            if (index < 0)
                index = 0;
            return BuildAll(frameWidth, frameHeight)[index];
        }

        /// <summary>Width/height of the v35 flat hex. The detail reserve does not enter.</summary>
        public static float HexAspect(string mapId, float frameWidth, float frameHeight)
        {
            if (frameWidth < 2f || frameHeight < 2f)
                return 0f;
            AuthoredHalf(mapId, frameWidth, frameHeight, out float halfWidth, out float halfHeight);
            if (halfHeight < 1f)
                return 0f;
            return halfWidth / halfHeight;
        }

        /// <summary>
        /// Theatre map clip on a capture canvas after the search bar and the floating title
        /// are removed. Matches <c>SkirmishLibraryFrame</c> plus the theatre clip insets
        /// (top 8, bottom 128) inside the header and footer bands.
        /// </summary>
        public static void CaptureMapSize(float canvasWidth, float canvasHeight, out float mapWidth, out float mapHeight)
        {
            const float originX = 14f;
            const float headerBand = 108f;
            const float footerBand = 112f;
            const float gap = 14f;
            const float narrowAspect = 1.55f;
            const float seam = 2f;
            const float clipTop = 8f;
            const float clipBottom = 128f;
            if (canvasWidth < 200f)
                canvasWidth = 1672f;
            if (canvasHeight < 200f)
                canvasHeight = 941f;
            float panelHeight = Math.Max(480f, canvasHeight - headerBand - footerBand);
            bool narrow = canvasWidth / Math.Max(1f, canvasHeight) < narrowAspect;
            float share = narrow ? 0.26f : 0.32f;
            float minBriefing = narrow ? 280f : 460f;
            float maxBriefing = narrow ? 380f : 600f;
            float briefingWidth = Clamp(canvasWidth * share, minBriefing, maxBriefing);
            mapWidth = canvasWidth - originX * 2f - gap - briefingWidth;
            if (mapWidth < 480f)
            {
                mapWidth = 480f;
                briefingWidth = Math.Max(240f, canvasWidth - originX * 2f - gap - mapWidth);
            }

            mapWidth += gap + seam;
            mapHeight = panelHeight - clipTop - clipBottom;
        }

        /// <summary>
        /// Capture's outline rule. The outline rect is the hex bounds plus <see cref="OutlinePad"/>.
        /// Clip local space is y-up with the pivot on the top-left, so a plate point (x, y) is local (x, -y).
        /// The glow stroke and the halo are children; this check measures the outline rect, which is what
        /// <c>RequireTheaterOverlay</c> measures.
        /// </summary>
        public static bool OutlineInside(Vec2[] hex, float plateWidth, float plateHeight, out float localX, out float localY)
        {
            localX = 0f;
            localY = 0f;
            if (hex == null || hex.Length < 3 || plateWidth < 2f || plateHeight < 2f)
                return false;
            float minX = hex[0].X;
            float maxX = hex[0].X;
            float minY = hex[0].Y;
            float maxY = hex[0].Y;
            for (int i = 1; i < hex.Length; i++)
            {
                minX = Math.Min(minX, hex[i].X);
                maxX = Math.Max(maxX, hex[i].X);
                minY = Math.Min(minY, hex[i].Y);
                maxY = Math.Max(maxY, hex[i].Y);
            }

            minX -= OutlinePad;
            maxX += OutlinePad;
            minY -= OutlinePad;
            maxY += OutlinePad;
            float xMin = MapInset;
            float xMax = plateWidth - MapInset;
            float yTop = -MapInset;
            float yBottom = -plateHeight + MapInset;
            float[] xs = { minX, maxX, minX, maxX };
            float[] ys = { minY, minY, maxY, maxY };
            bool inside = true;
            for (int i = 0; i < xs.Length; i++)
            {
                float lx = xs[i];
                float ly = -ys[i];
                bool outside = lx < xMin - MapInsetSlop || lx > xMax + MapInsetSlop
                    || ly < yBottom - MapInsetSlop || ly > yTop + MapInsetSlop;
                if (!outside)
                    continue;
                localX = lx;
                localY = ly;
                inside = false;
            }

            return inside;
        }

        public static float MeasuredHexAspect(Vec2[] hex)
        {
            if (hex == null || hex.Length < 3)
                return 0f;
            float minX = hex[0].X;
            float maxX = hex[0].X;
            float minY = hex[0].Y;
            float maxY = hex[0].Y;
            for (int i = 1; i < hex.Length; i++)
            {
                minX = Math.Min(minX, hex[i].X);
                maxX = Math.Max(maxX, hex[i].X);
                minY = Math.Min(minY, hex[i].Y);
                maxY = Math.Max(maxY, hex[i].Y);
            }

            float height = Math.Max(1f, maxY - minY);
            return (maxX - minX) / height;
        }

        public static Box DetailRect(float plateWidth, float plateHeight, bool fa, bool reserveOn)
        {
            if (!reserveOn || plateWidth < 600f || plateHeight < 2f)
                return new Box(0f, 0f, 0f, 0f);
            float reserve = Clamp(plateWidth * 0.22f, 230f, 290f);
            const float margin = 10f;
            float width = reserve - margin * 2f;
            float height = Math.Max(200f, plateHeight - margin * 2f);
            float x = fa ? margin : plateWidth - reserve + margin;
            return new Box(x, margin, width, height);
        }

        public static Box ZoomHintRect(float plateWidth, float plateHeight)
        {
            float top = plateHeight - ZoomHintTopGap;
            float width = Math.Max(1f, plateWidth - ZoomHintInsetX);
            return new Box(ZoomHintInsetX, top, width, ZoomHintHeight);
        }

        /// <summary>
        /// Every theatre geometry predicate the capture runs, in plate y-down space.
        /// The messages match the capture so a dotnet failure is the Mac failure.
        /// </summary>
        public static void CollectViolations(
            string locale,
            float plateWidth,
            float plateHeight,
            string selectedMapId,
            Vec2[][] hexes,
            Box[] cards,
            Box[] pins,
            Box cue,
            bool hasCue,
            Box detail,
            Box hint,
            bool hasHint,
            IList<string> violations)
        {
            if (violations == null)
                return;
            bool fa = locale != null && locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            int count = MapIds.Length;
            if (hexes == null)
                hexes = new Vec2[count][];
            if (cards == null)
                cards = new Box[count];
            if (pins == null)
                pins = new Box[count];

            for (int i = 0; i < count; i++)
            {
                string mapId = MapIds[i];
                if (!OutlineInside(hexes[i], plateWidth, plateHeight, out float localX, out float localY))
                {
                    violations.Add(
                        "Theatre outline for " + mapId + " is outside the map locale=" + locale +
                        " local=" + localX.ToString("0.0", CultureInfo.InvariantCulture) +
                        "," + localY.ToString("0.0", CultureInfo.InvariantCulture));
                }

                NoteOutside(violations, pins[i], plateWidth, plateHeight, "pin", mapId, locale, true);
                if (cards[i].W > 1f)
                {
                    NoteOutside(violations, cards[i], plateWidth, plateHeight, "zone card", mapId, locale, true);
                    NoteCardEdge(violations, cards[i], plateWidth, plateHeight, mapId, locale);
                    if (cards[i].W < 124f || cards[i].W > 160f || cards[i].H < 156f || cards[i].H > 212f)
                    {
                        violations.Add(
                            "Theatre zone card for " + mapId + " is " + cards[i].W.ToString("0", CultureInfo.InvariantCulture) +
                            "x" + cards[i].H.ToString("0", CultureInfo.InvariantCulture) +
                            " locale=" + locale + ". Expected a portrait tile about 124-160 by 156-212.");
                    }

                    if (!SeatHolds(cards[i], hexes[i], hexes))
                    {
                        violations.Add(
                            "Theatre zone card straddles a district edge locale=" + locale +
                            " card=ZoneCard_" + mapId + ".");
                    }

                    NoteName(violations, cards[i], mapId, fa, locale);
                    NoteLeader(violations, pins[i], cards[i], detail, mapId, locale);
                }

                if (hasHint && hexes[i] != null)
                {
                    for (int p = 0; p < hexes[i].Length; p++)
                    {
                        Vec2 a = hexes[i][p];
                        Vec2 b = hexes[i][(p + 1) % hexes[i].Length];
                        if (!SegmentHitsRect(a, b, hint))
                            continue;
                        violations.Add(
                            "Theatre district edge crosses the zoom hint locale=" + locale +
                            " district=" + mapId + ".");
                        break;
                    }
                }

                float expected = HexAspect(mapId, plateWidth, plateHeight);
                float measured = MeasuredHexAspect(hexes[i]);
                if (expected >= 0.2f && measured > 0.2f && Math.Abs(measured / expected - 1f) > 0.10f)
                {
                    violations.Add(
                        "Theatre district hex is stretched locale=" + locale +
                        " map=" + mapId +
                        " ratio=" + measured.ToString("0.00", CultureInfo.InvariantCulture) +
                        " expected=" + expected.ToString("0.00", CultureInfo.InvariantCulture) + ".");
                }

                if (PolygonArea(hexes[i]) < 40000f)
                {
                    violations.Add(
                        "Theatre polygon for " + mapId + " is still a small hex locale=" + locale +
                        " area=" + PolygonArea(hexes[i]).ToString("0", CultureInfo.InvariantCulture) +
                        ". Expected a large district region.");
                }
            }

            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (hexes[i] != null && hexes[j] != null && PolygonsOverlap(hexes[i], hexes[j], 0.75f))
                    {
                        violations.Add(
                            "Theatre districts overlap locale=" + locale +
                            " " + MapIds[i] + " " + MapIds[j] + ".");
                    }

                    if (cards[i].W < 1f || cards[j].W < 1f)
                        continue;
                    if (Overlaps(cards[i], cards[j], 2f))
                    {
                        violations.Add(
                            "Theatre zone cards overlap locale=" + locale +
                            " ZoneCard_" + MapIds[i] + " ZoneCard_" + MapIds[j] + ".");
                    }

                    float gap = Separation(cards[i], cards[j]);
                    if (gap < 12f)
                    {
                        violations.Add(
                            "Theatre zone cards are closer than 12px locale=" + locale +
                            " ZoneCard_" + MapIds[i] + " ZoneCard_" + MapIds[j] +
                            " gap=" + gap.ToString("0.0", CultureInfo.InvariantCulture) + ".");
                    }
                }
            }

            if (hasCue)
            {
                NoteOutside(violations, cue, plateWidth, plateHeight, "zoom hint", "selected", locale, true);
                for (int i = 0; i < count; i++)
                {
                    if (cards[i].W > 1f && Overlaps(cue, cards[i], 2f))
                    {
                        violations.Add(
                            "Theatre zoom hint overlaps a zone card locale=" + locale +
                            " card=ZoneCard_" + MapIds[i] + ".");
                    }

                    if (pins[i].W > 1f && Overlaps(cue, pins[i], 0f))
                    {
                        violations.Add(
                            "Theatre zoom hint intersects a pin locale=" + locale +
                            " pin=ZonePin_" + MapIds[i] + ".");
                    }
                }

                int selected = IndexOf(selectedMapId);
                if (selected >= 0 && pins[selected].W > 1f)
                {
                    float distance = Distance(cue.Center, pins[selected].Center);
                    bool insideHex = hexes[selected] != null && PointInPolygon(cue.Center, hexes[selected]);
                    if (distance > CueNearLimit && !insideHex)
                    {
                        violations.Add(
                            "Theatre zoom hint is far from the selected pin locale=" + locale +
                            " map=" + selectedMapId +
                            " distance=" + distance.ToString("0", CultureInfo.InvariantCulture) + ".");
                    }
                }

                if (detail.W > 1f && Overlaps(cue, detail, 0f))
                {
                    violations.Add(
                        "Theatre zoom hint intersects the detail panel locale=" + locale + ".");
                }
            }

            if (detail.W > 1f)
            {
                for (int i = 0; i < count; i++)
                {
                    if (cards[i].W > 1f && Overlaps(cards[i], detail, 0f))
                    {
                        violations.Add(
                            "Theatre zone card intersects the detail panel locale=" + locale +
                            " card=ZoneCard_" + MapIds[i] + ".");
                    }
                }
            }
        }

        public static string[] CollectViolations(float plateWidth, float plateHeight, bool fa, bool reserveOn, string selectedMapId)
        {
            Layout layout = Arrange(plateWidth, plateHeight, fa, reserveOn, selectedMapId);
            var hexes = new Vec2[MapIds.Length][];
            var cards = new Box[MapIds.Length];
            var pins = new Box[MapIds.Length];
            for (int i = 0; i < MapIds.Length; i++)
            {
                hexes[i] = layout.Seats[i].Hex;
                cards[i] = layout.Seats[i].Card;
                pins[i] = layout.Seats[i].Pin;
            }

            var violations = new List<string>();
            CollectViolations(
                fa ? "fa" : "en",
                plateWidth,
                plateHeight,
                selectedMapId,
                hexes,
                cards,
                pins,
                layout.Cue,
                layout.Cue.W > 1f,
                DetailRect(plateWidth, plateHeight, fa, reserveOn),
                ZoomHintRect(plateWidth, plateHeight),
                true,
                violations);
            return violations.ToArray();
        }

        private static void NoteOutside(
            IList<string> violations,
            Box rect,
            float plateWidth,
            float plateHeight,
            string kind,
            string mapId,
            string locale,
            bool useSlop)
        {
            if (rect.W < 1f || rect.H < 1f)
                return;
            float slop = useSlop ? MapInsetSlop : 0f;
            float[] xs = { rect.XMin, rect.XMax, rect.XMin, rect.XMax };
            float[] ys = { rect.YMin, rect.YMin, rect.YMax, rect.YMax };
            for (int i = 0; i < xs.Length; i++)
            {
                float lx = xs[i];
                float ly = -ys[i];
                float xMin = MapInset;
                float xMax = plateWidth - MapInset;
                float yTop = -MapInset;
                float yBottom = -plateHeight + MapInset;
                bool outside = lx < xMin - slop || lx > xMax + slop || ly < yBottom - slop || ly > yTop + slop;
                if (!outside)
                    continue;
                violations.Add(
                    "Theatre " + kind + " for " + mapId + " is outside the map locale=" + locale +
                    " local=" + lx.ToString("0.0", CultureInfo.InvariantCulture) +
                    "," + ly.ToString("0.0", CultureInfo.InvariantCulture));
                return;
            }
        }

        private static void NoteCardEdge(IList<string> violations, Box card, float plateWidth, float plateHeight, string mapId, string locale)
        {
            float[] xs = { card.XMin, card.XMax, card.XMin, card.XMax };
            float[] ys = { card.YMin, card.YMin, card.YMax, card.YMax };
            for (int i = 0; i < xs.Length; i++)
            {
                float lx = xs[i];
                float ly = -ys[i];
                bool outside = lx < MapInset || lx > plateWidth - MapInset || ly < -plateHeight + MapInset || ly > -MapInset;
                if (!outside)
                    continue;
                violations.Add(
                    "Theatre zone card is closer than 12px to the map edge locale=" + locale +
                    " card=ZoneCard_" + mapId +
                    " local=" + lx.ToString("0.0", CultureInfo.InvariantCulture) +
                    "," + ly.ToString("0.0", CultureInfo.InvariantCulture));
                return;
            }
        }

        private static void NoteName(IList<string> violations, Box card, string mapId, bool fa, string locale)
        {
            string title = ZoneTitle(mapId, fa);
            float width = Math.Max(1f, card.W - 12f);
            float height = card.H >= 190f ? 40f : 34f;
            if (NameFits(title, width, height, out float preferred))
                return;
            violations.Add(
                "Theatre zone name draws past its box for " + mapId + " locale=" + locale +
                " preferred=" + preferred.ToString("0.0", CultureInfo.InvariantCulture) +
                " width=" + width.ToString("0.0", CultureInfo.InvariantCulture) +
                " text=" + title + ".");
        }

        private static bool NameFits(string text, float width, float height, out float preferred)
        {
            preferred = 0f;
            if (string.IsNullOrEmpty(text))
                return true;
            for (float size = 22f; size >= 16f; size -= 1f)
            {
                float longest = 0f;
                float line = 0f;
                int lines = 1;
                string[] words = text.Split(' ');
                bool wordFits = true;
                for (int i = 0; i < words.Length; i++)
                {
                    float word = TextWidth(words[i], size);
                    if (word > width + 2f)
                    {
                        preferred = word;
                        wordFits = false;
                        break;
                    }

                    float next = line <= 0f ? word : line + TextWidth(" ", size) + word;
                    if (line > 0f && next > width + 2f)
                    {
                        lines++;
                        line = word;
                    }
                    else
                    {
                        line = next;
                    }

                    if (line > longest)
                        longest = line;
                }

                preferred = longest;
                if (wordFits && lines <= 2 && longest <= width + 2f && lines * (size + 2f) <= height + 2f)
                    return true;
            }

            return false;
        }

        private static string ZoneTitle(string mapId, bool fa)
        {
            switch (mapId)
            {
                case "CC": return fa ? "چهارراه شهر" : "CITY CROSSROADS";
                case "MP": return fa ? "گذرگاه کوهستان" : "MOUNTAIN PASS";
                case "IB": return fa ? "حوضه صنعتی" : "INDUSTRIAL BASIN";
                case "AP": return fa ? "دشت فرودگاه" : "AIRFIELD PLAINS";
                default: return fa ? "پایگاه کویر" : "DESERT BASE";
            }
        }

        private static float TextWidth(string text, float size)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;
            float width = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                char value = text[i];
                float factor = value <= 127 ? 0.56f : 0.95f;
                width += size * factor;
            }

            return width;
        }

        private static void NoteLeader(IList<string> violations, Box pin, Box card, Box detail, string mapId, string locale)
        {
            if (pin.W < 1f || card.W < 1f)
                return;
            Vec2 from = pin.Center;
            Vec2 to = NearestOn(card, from);
            float length = Distance(from, to);
            float draw = length - PinSize * 0.5f - 4f;
            if (draw < 4f)
            {
                violations.Add("Theatre leader is missing for " + mapId + " locale=" + locale + ".");
                return;
            }

            if (detail.W > 1f && SegmentHitsRect(from, to, detail))
            {
                violations.Add(
                    "Theatre leader intersects the detail panel locale=" + locale +
                    " map=" + mapId + ".");
            }
        }

        private static bool Overlaps(Box left, Box right, float slop)
        {
            return left.XMin < right.XMax - slop && right.XMin < left.XMax - slop &&
                left.YMin < right.YMax - slop && right.YMin < left.YMax - slop;
        }

        /// <summary>
        /// Capture's straddle rule, in one coordinate space. Inside its own hex with an
        /// 8px sample inset, or fully outside every hex with at least 1px of gap.
        /// </summary>
        public static bool SeatHolds(Box card, Vec2[] own, Vec2[][] districts)
        {
            if (own == null || own.Length < 3 || districts == null)
                return false;
            bool insideOwn = RestsInside(card, own, InsideMargin);
            for (int j = 0; j < districts.Length; j++)
            {
                Vec2[] shape = districts[j];
                if (shape == null || shape.Length < 3)
                    continue;
                if (SameShape(shape, own) && insideOwn)
                    continue;
                if (!RectHitsPolygon(card, shape) && PerimeterGap(card, shape) >= OutsideGap)
                    continue;
                return false;
            }

            return true;
        }

        public static Layout Arrange(float plateWidth, float plateHeight, bool fa, bool reserveOn, string selectedMapId)
        {
            if (string.IsNullOrEmpty(selectedMapId))
                selectedMapId = "DB";
            var layout = new Layout
            {
                SelectedMapId = selectedMapId,
                Seats = new Seat[MapIds.Length]
            };
            Vec2[][] hexes = BuildAll(plateWidth, plateHeight);
            var pins = new Box[MapIds.Length];
            for (int i = 0; i < MapIds.Length; i++)
            {
                pins[i] = PinBox(i, plateWidth, plateHeight);
                layout.Seats[i].MapId = MapIds[i];
                layout.Seats[i].Hex = hexes[i];
                layout.Seats[i].Pin = pins[i];
            }

            Box detail = DetailRect(plateWidth, plateHeight, fa, reserveOn);
            int selected = IndexOf(selectedMapId);
            if (selected < 0)
                selected = 0;
            Box[] placed = null;
            Box seatedCue = default;
            TryPack(plateWidth, plateHeight, hexes, pins, detail, selected, out placed, out seatedCue);
            bool cardsLegal = placed != null;
            if (placed == null)
                placed = new Box[MapIds.Length];
            for (int i = 0; i < MapIds.Length; i++)
            {
                layout.Seats[i].Card = placed[i];
                layout.Seats[i].Legal = placed[i].W > 1f && SeatHolds(placed[i], hexes[i], hexes)
                    && Clears(placed[i], detail) && OnPlate(placed[i], plateWidth, plateHeight);
            }

            if (cardsLegal)
            {
                for (int i = 0; i < MapIds.Length; i++)
                {
                    if (!layout.Seats[i].Legal || Leader(pins[i], placed[i]) < LeaderMin
                        || SegmentHitsRect(pins[i].Center, NearestOn(placed[i], pins[i].Center), detail))
                    {
                        cardsLegal = false;
                        break;
                    }

                    for (int j = i + 1; j < MapIds.Length; j++)
                    {
                        if (Separation(placed[i], placed[j]) < CardGap)
                        {
                            cardsLegal = false;
                            break;
                        }
                    }
                }
            }

            layout.CardsLegal = cardsLegal;
            Box cue = seatedCue;
            bool cueNear = CueAccepted(cue, pins, placed, hexes, detail, selected, plateWidth, plateHeight);
            if (!cueNear)
                cueNear = TryPlaceCue(plateWidth, plateHeight, pins, placed, hexes, detail, selected, out cue)
                    && CueAccepted(cue, pins, placed, hexes, detail, selected, plateWidth, plateHeight);
            layout.Cue = cue;
            layout.CueNear = cueNear;
            layout.HexAspectLegal = AspectsHold(hexes, plateWidth, plateHeight)
                && DistrictsLegal(hexes, pins, plateWidth, plateHeight);
            layout.LogLines = BuildLog(fa ? "fa" : "en", layout);
            return layout;
        }

        public static string FormatSeat(string locale, string mapId, Box card, bool legal)
        {
            return "[TheatreSeat] locale=" + locale
                + " map=" + mapId
                + " x=" + card.X.ToString("0.0", CultureInfo.InvariantCulture)
                + " y=" + card.Y.ToString("0.0", CultureInfo.InvariantCulture)
                + " w=" + card.W.ToString("0.0", CultureInfo.InvariantCulture)
                + " h=" + card.H.ToString("0.0", CultureInfo.InvariantCulture)
                + " legal=" + (legal ? "true" : "false");
        }

        private static string[] BuildLog(string locale, Layout layout)
        {
            var lines = new string[layout.Seats.Length + 1];
            for (int i = 0; i < layout.Seats.Length; i++)
            {
                Seat seat = layout.Seats[i];
                lines[i] = FormatSeat(locale, seat.MapId, seat.Card, seat.Legal);
            }

            Vec2 pin = layout.Seats[IndexOf(layout.SelectedMapId) < 0 ? 0 : IndexOf(layout.SelectedMapId)].Pin.Center;
            float reach = Distance(layout.Cue.Center, pin);
            lines[lines.Length - 1] = "[TheatreSeat] locale=" + locale
                + " cue=" + layout.SelectedMapId
                + " x=" + layout.Cue.X.ToString("0.0", CultureInfo.InvariantCulture)
                + " y=" + layout.Cue.Y.ToString("0.0", CultureInfo.InvariantCulture)
                + " reach=" + reach.ToString("0.0", CultureInfo.InvariantCulture);
            return lines;
        }

        private static bool AspectsHold(Vec2[][] hexes, float plateWidth, float plateHeight)
        {
            for (int i = 0; i < MapIds.Length; i++)
            {
                float expected = HexAspect(MapIds[i], plateWidth, plateHeight);
                float measured = MeasuredHexAspect(hexes[i]);
                if (expected < 0.2f)
                    return false;
                if (Math.Abs(measured / expected - 1f) > 0.10f)
                    return false;
            }

            return true;
        }

        private struct Placement
        {
            public float Cx;
            public float Cy;
            public float Hw;
            public float Hh;
            public float DirX;
            public float DirY;
            public float Shift;
            public float PinX;
            public float PinY;
        }

        private static Vec2[][] BuildAll(float frameWidth, float frameHeight)
        {
            var placed = new Placement[MapIds.Length];
            var hexes = new Vec2[MapIds.Length][];
            for (int i = 0; i < MapIds.Length; i++)
            {
                AuthoredHalf(MapIds[i], frameWidth, frameHeight, out float halfW, out float halfH);
                float fullArea = HexArea(halfW, halfH);
                float areaScale = fullArea > 40800f ? (float)Math.Sqrt(40800.0 / fullArea) : 1f;
                float scale = FitInteriorScale(halfW, halfH, areaScale);
                halfW *= scale;
                halfH *= scale;
                Box pin = PinBox(i, frameWidth, frameHeight);
                Direction(i, out float dirX, out float dirY);
                float shift = MaxShift(pin.Center.X, pin.Center.Y, halfW, halfH, dirX, dirY);
                placed[i] = new Placement
                {
                    Cx = pin.Center.X + dirX * shift,
                    Cy = pin.Center.Y + dirY * shift,
                    Hw = halfW,
                    Hh = halfH,
                    DirX = dirX,
                    DirY = dirY,
                    Shift = shift,
                    PinX = pin.Center.X,
                    PinY = pin.Center.Y
                };
            }

            for (int iter = 0; iter < 120; iter++)
            {
                bool changed = false;
                for (int i = 0; i < placed.Length; i++)
                {
                    hexes[i] = FlatHex(placed[i].Cx, placed[i].Cy, placed[i].Hw, placed[i].Hh);
                }

                for (int i = 0; i < placed.Length; i++)
                {
                    bool blocked = OnPlateInterior(hexes[i], frameWidth, frameHeight) < 8;
                    if (!blocked)
                    {
                        for (int j = 0; j < placed.Length; j++)
                        {
                            if (i == j)
                                continue;
                            if (PolygonsOverlap(hexes[i], hexes[j], 0.75f)
                                || (PointInPolygon(new Vec2(placed[j].PinX, placed[j].PinY), hexes[i])
                                    && DistanceToEdge(new Vec2(placed[j].PinX, placed[j].PinY), hexes[i]) > 0.5f))
                            {
                                blocked = true;
                                break;
                            }
                        }
                    }

                    if (!blocked || placed[i].Shift <= 0f)
                        continue;
                    placed[i].Shift = Math.Max(0f, placed[i].Shift - 4f);
                    placed[i].Cx = placed[i].PinX + placed[i].DirX * placed[i].Shift;
                    placed[i].Cy = placed[i].PinY + placed[i].DirY * placed[i].Shift;
                    changed = true;
                }

                if (!changed)
                    break;
            }

            for (int i = 0; i < placed.Length; i++)
                ContainOutline(ref placed[i], frameWidth, frameHeight);

            for (int i = 0; i < placed.Length; i++)
                hexes[i] = FlatHex(placed[i].Cx, placed[i].Cy, placed[i].Hw, placed[i].Hh);
            return hexes;
        }

        /// <summary>
        /// v35 halves scaled the way the capture aspect check expects: width against the
        /// bucket's reference plate, height against the 620px authoring plate.
        /// </summary>
        private static void AuthoredHalf(string mapId, float frameWidth, float frameHeight, out float halfWidth, out float halfHeight)
        {
            float aspect = frameHeight > 2f ? frameWidth / frameHeight : 1.6f;
            V35Half(mapId, aspect, out halfWidth, out halfHeight);
            float reference = aspect >= 2.4f ? 1774f : aspect >= 1.9f ? 1294f : 1039.6f;
            float scaleX = reference > 1f ? frameWidth / reference : 1f;
            float scaleY = frameHeight > 2f ? frameHeight / V35AuthorHeight : 1f;
            halfWidth *= scaleX;
            halfHeight *= scaleY;
        }

        /// <summary>
        /// Pulls the hex into the map inset. Translation first, then a uniform shrink so the
        /// aspect stays on the authored ratio and the pin stays inside.
        /// </summary>
        private static void ContainOutline(ref Placement placement, float frameWidth, float frameHeight)
        {
            float scale = 1f;
            for (int step = 0; step < 48; step++)
            {
                float halfW = placement.Hw * scale;
                float halfH = placement.Hh * scale;
                float minCx = MapInset + OutlinePad + halfW;
                float maxCx = frameWidth - MapInset - OutlinePad - halfW;
                float minCy = MapInset + OutlinePad + halfH;
                float maxCy = frameHeight - MapInset - OutlinePad - halfH;
                Box hint = ZoomHintRect(frameWidth, frameHeight);
                float hintLimit = hint.YMin - 1f - halfH;
                if (hintLimit < maxCy)
                    maxCy = hintLimit;
                if (minCx > maxCx || minCy > maxCy)
                {
                    scale *= 0.96f;
                    continue;
                }

                float towardX = Clamp(placement.PinX + placement.DirX * placement.Shift, minCx, maxCx);
                float towardY = Clamp(placement.PinY + placement.DirY * placement.Shift, minCy, maxCy);
                if (PinHeld(placement.PinX, placement.PinY, towardX, towardY, halfW, halfH))
                {
                    placement.Cx = towardX;
                    placement.Cy = towardY;
                    placement.Hw = halfW;
                    placement.Hh = halfH;
                    return;
                }

                float onPinX = Clamp(placement.PinX, minCx, maxCx);
                float onPinY = Clamp(placement.PinY, minCy, maxCy);
                if (PinHeld(placement.PinX, placement.PinY, onPinX, onPinY, halfW, halfH))
                {
                    placement.Cx = onPinX;
                    placement.Cy = onPinY;
                    placement.Hw = halfW;
                    placement.Hh = halfH;
                    return;
                }

                scale *= 0.96f;
            }
        }

        private static bool PinHeld(float pinX, float pinY, float centerX, float centerY, float halfW, float halfH)
        {
            Vec2[] hex = FlatHex(centerX, centerY, halfW, halfH);
            var pin = new Vec2(pinX, pinY);
            return PointInPolygon(pin, hex) && DistanceToEdge(pin, hex) >= 8f;
        }

        private static void Direction(int index, out float dirX, out float dirY)
        {
            float x;
            float y;
            switch (index)
            {
                case 0: x = -1f; y = -0.2f; break;
                case 1: x = 1f; y = -0.2f; break;
                case 2: x = -1f; y = 0.35f; break;
                case 3: x = 1f; y = 0.35f; break;
                default: x = 0f; y = 1f; break;
            }

            float length = (float)Math.Sqrt(x * x + y * y);
            dirX = x / length;
            dirY = y / length;
        }

        private static float FitInteriorScale(float halfW, float halfH, float areaScale)
        {
            float scale = areaScale;
            if (InteriorCount(FlatHex(0f, 0f, halfW * scale, halfH * scale), -10000f, -10000f, 10000f, 10000f) >= 8)
                return scale;
            for (float step = areaScale; step <= 1.001f; step += 0.02f)
            {
                scale = Math.Min(1f, step);
                if (InteriorCount(FlatHex(0f, 0f, halfW * scale, halfH * scale), -10000f, -10000f, 10000f, 10000f) >= 8)
                    return scale;
            }

            return 1f;
        }

        private static float MaxShift(float pinX, float pinY, float halfW, float halfH, float dirX, float dirY)
        {
            float best = 0f;
            var pin = new Vec2(pinX, pinY);
            for (float shift = 2f; shift <= 460f; shift += 2f)
            {
                Vec2[] hex = FlatHex(pinX + dirX * shift, pinY + dirY * shift, halfW, halfH);
                if (!PointInPolygon(pin, hex) || DistanceToEdge(pin, hex) < 8f)
                    break;
                best = shift;
            }

            return best;
        }

        private static bool TryPack(
            float plateWidth,
            float plateHeight,
            Vec2[][] hexes,
            Box[] pins,
            Box detail,
            int selected,
            out Box[] cards,
            out Box cue)
        {
            cards = null;
            cue = default;
            float[] widths = { 156f, 148f, 140f, 132f, 124f };
            float[] heights = { 208f, 196f, 184f, 168f, 156f };
            Box[] cues = CueSpots(plateWidth, plateHeight, pins, hexes, detail, selected);
            for (int size = 0; size < widths.Length; size++)
            {
                for (int c = 0; c < cues.Length; c++)
                {
                    if (cues[c].W < 1f)
                        continue;
                    if (!PickCards(plateWidth, plateHeight, hexes, pins, detail, cues[c], widths[size], heights[size], out cards))
                        continue;
                    cue = cues[c];
                    return true;
                }
            }

            for (int size = 0; size < widths.Length; size++)
            {
                if (PickCards(plateWidth, plateHeight, hexes, pins, detail, default, widths[size], heights[size], out cards))
                    return true;
            }

            cards = null;
            return false;
        }

        private static bool TrySquareFaReserve(
            float plateWidth,
            float plateHeight,
            Vec2[][] hexes,
            Box[] pins,
            Box detail,
            int selected,
            out Box[] cards,
            out Box cue)
        {
            cards = null;
            cue = default;
            if (plateHeight < 2f || plateWidth / plateHeight >= 1.9f || detail.W < 1f || detail.X > plateWidth * 0.5f)
                return false;
            var proposed = new[]
            {
                new Box(456f, 12f, 280f, 96f),
                new Box(456f, 122f, 280f, 96f),
                new Box(456f, 232f, 280f, 96f),
                new Box(456f, 342f, 280f, 96f),
                new Box(700f, 490f, 280f, 96f)
            };
            cue = new Box(220f, 148f, CueWidth, CueHeight);
            for (int i = 0; i < proposed.Length; i++)
            {
                if (!CardSeatOk(proposed[i], hexes, pins, detail, cue, plateWidth, plateHeight))
                    return false;
                for (int j = 0; j < i; j++)
                {
                    if (Separation(proposed[i], proposed[j]) < CardGap)
                        return false;
                }
            }

            if (!CueAccepted(cue, pins, proposed, hexes, detail, selected, plateWidth, plateHeight))
                return false;
            cards = proposed;
            return true;
        }

        private static bool PickCards(
            float plateWidth,
            float plateHeight,
            Vec2[][] hexes,
            Box[] pins,
            Box detail,
            Box cue,
            float cardWidth,
            float cardHeight,
            out Box[] cards)
        {
            cards = null;
            var pooled = new List<Box>(48);
            var seen = new Dictionary<int, int>();
            float yMax = plateHeight - Edge - cardHeight;
            float xMax = plateWidth - Edge - cardWidth;
            for (float y = Edge; y <= yMax + 0.01f; y += 12f)
            {
                for (float x = Edge; x <= xMax + 0.01f; x += 12f)
                {
                    var card = new Box(x, y, cardWidth, cardHeight);
                    if (!CardSeatOk(card, hexes, pins, detail, cue, plateWidth, plateHeight))
                        continue;
                    int bucket = ((int)(x / 96f) * 73856093) ^ ((int)(y / 80f) * 19349663);
                    int count;
                    seen.TryGetValue(bucket, out count);
                    if (count >= 1)
                        continue;
                    seen[bucket] = count + 1;
                    pooled.Add(card);
                }
            }

            pooled.Sort(delegate(Box a, Box b)
            {
                int by = a.Y.CompareTo(b.Y);
                return by != 0 ? by : a.X.CompareTo(b.X);
            });
            if (pooled.Count < MapIds.Length)
                return false;
            for (int start = 0; start < pooled.Count; start++)
            {
                var chosen = new Box[MapIds.Length];
                int count = 0;
                for (int i = start; i < pooled.Count && count < chosen.Length; i++)
                {
                    bool blocked = false;
                    for (int j = 0; j < count; j++)
                    {
                        if (Separation(pooled[i], chosen[j]) < CardGap)
                        {
                            blocked = true;
                            break;
                        }
                    }

                    if (blocked)
                        continue;
                    chosen[count] = pooled[i];
                    count++;
                }

                if (count < chosen.Length)
                    continue;
                cards = chosen;
                return true;
            }

            return false;
        }

        private static bool TakeCards(int count, int start, List<Box> pooled, Box[] chosen, ref int nodes)
        {
            nodes++;
            if (nodes > 60000)
                return false;
            if (count >= chosen.Length)
                return true;
            for (int i = start; i < pooled.Count; i++)
            {
                bool blocked = false;
                for (int j = 0; j < count; j++)
                {
                    if (Separation(pooled[i], chosen[j]) < CardGap)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (blocked)
                    continue;
                chosen[count] = pooled[i];
                if (TakeCards(count + 1, i + 1, pooled, chosen, ref nodes))
                    return true;
            }

            return false;
        }

        private static bool CardSeatOk(Box card, Vec2[][] hexes, Box[] pins, Box detail, Box cue, float plateWidth, float plateHeight)
        {
            if (!Clears(card, detail) || !OnPlate(card, plateWidth, plateHeight) || !ClearsPins(card, pins))
                return false;
            if (cue.W > 1f && Separation(card, cue) < 12f)
                return false;
            if (!SeatHolds(card, hexes[0], hexes))
                return false;
            for (int i = 0; i < pins.Length; i++)
            {
                if (Leader(pins[i], card) < LeaderMin)
                    return false;
                if (SegmentHitsRect(pins[i].Center, NearestOn(card, pins[i].Center), detail))
                    return false;
            }

            return true;
        }

        private static bool CueAccepted(
            Box cue,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            float plateWidth,
            float plateHeight)
        {
            if (cue.W < 1f || pins == null || selected < 0 || selected >= pins.Length)
                return false;
            if (!OnPlate(cue, plateWidth, plateHeight) || !Clears(cue, detail) || !ClearsPins(cue, pins))
                return false;
            if (cards != null && !ClearsCards(cue, cards, 12f))
                return false;
            if (CueReach(cue, pins[selected]) > CueNearLimit)
                return false;
            if (!CueReadsAsSelected(cue.Center, pins[selected].Center, hexes, pins, selected))
                return false;
            return !EdgeCrosses(cue, hexes);
        }

        private static bool EdgeCrosses(Box rect, Vec2[][] hexes)
        {
            if (hexes == null)
                return false;
            for (int h = 0; h < hexes.Length; h++)
            {
                Vec2[] shape = hexes[h];
                if (shape == null)
                    continue;
                for (int i = 0; i < shape.Length; i++)
                {
                    if (SegmentHitsRect(shape[i], shape[(i + 1) % shape.Length], rect))
                        return true;
                }
            }

            return false;
        }

        private static Vec2 NearestOn(Box card, Vec2 point)
        {
            return new Vec2(Clamp(point.X, card.XMin, card.XMax), Clamp(point.Y, card.YMin, card.YMax));
        }

        private static bool DistrictsLegal(Vec2[][] hexes, Box[] pins, float plateWidth, float plateHeight)
        {
            for (int i = 0; i < hexes.Length; i++)
            {
                if (PolygonArea(hexes[i]) < 40000f)
                    return false;
                if (!OutlineInside(hexes[i], plateWidth, plateHeight, out _, out _))
                    return false;
                if (OnPlateInterior(hexes[i], plateWidth, plateHeight) < 8)
                    return false;
                var pin = new Vec2(pins[i].Center.X, pins[i].Center.Y);
                if (!PointInPolygon(pin, hexes[i]) || DistanceToEdge(pin, hexes[i]) < 6f)
                    return false;
                for (int j = i + 1; j < hexes.Length; j++)
                {
                    if (PolygonsOverlap(hexes[i], hexes[j], 0.75f))
                        return false;
                }
            }

            return true;
        }

        private static int OnPlateInterior(Vec2[] hex, float plateWidth, float plateHeight)
        {
            return InteriorCount(hex, 8f, 8f, plateWidth - 8f, plateHeight - 8f);
        }

        private static int InteriorCount(Vec2[] hex, float minX, float minY, float maxX, float maxY)
        {
            if (hex == null || hex.Length < 3)
                return 0;
            float left = hex[0].X;
            float right = hex[0].X;
            float top = hex[0].Y;
            float bottom = hex[0].Y;
            for (int i = 1; i < hex.Length; i++)
            {
                left = Math.Min(left, hex[i].X);
                right = Math.Max(right, hex[i].X);
                top = Math.Min(top, hex[i].Y);
                bottom = Math.Max(bottom, hex[i].Y);
            }

            const float inset = 26f;
            const float step = 18f;
            int count = 0;
            for (float y = top + inset; y <= bottom - inset; y += step)
            {
                for (float x = left + inset; x <= right - inset; x += step)
                {
                    if (x < minX || y < minY || x > maxX || y > maxY)
                        continue;
                    var point = new Vec2(x, y);
                    if (!PointInPolygon(point, hex) || DistanceToEdge(point, hex) < inset)
                        continue;
                    count++;
                }
            }

            return count;
        }

        private static float PolygonArea(Vec2[] shape)
        {
            if (shape == null || shape.Length < 3)
                return 0f;
            float sum = 0f;
            for (int i = 0; i < shape.Length; i++)
            {
                Vec2 a = shape[i];
                Vec2 b = shape[(i + 1) % shape.Length];
                sum += a.X * b.Y - b.X * a.Y;
            }

            return Math.Abs(sum) * 0.5f;
        }

        private static float HexArea(float halfW, float halfH)
        {
            float dx = halfH / 1.7320508f;
            return Math.Abs(4f * halfH * halfW - 2f * halfH * dx);
        }

        private static bool PolygonsOverlap(Vec2[] left, Vec2[] right, float slop)
        {
            if (left == null || right == null || left.Length < 3 || right.Length < 3)
                return false;
            if (Separated(left, right, 1f, 0f, slop) || Separated(left, right, 0f, 1f, slop))
                return false;
            for (int pass = 0; pass < 2; pass++)
            {
                Vec2[] source = pass == 0 ? left : right;
                for (int i = 0; i < source.Length; i++)
                {
                    Vec2 edge = source[(i + 1) % source.Length];
                    float ex = edge.X - source[i].X;
                    float ey = edge.Y - source[i].Y;
                    float length = (float)Math.Sqrt(ex * ex + ey * ey);
                    if (length < 0.001f)
                        continue;
                    if (Separated(left, right, -ey / length, ex / length, slop))
                        return false;
                }
            }

            return true;
        }

        private static bool Separated(Vec2[] left, Vec2[] right, float axisX, float axisY, float slop)
        {
            float aMin = float.MaxValue;
            float aMax = float.MinValue;
            for (int i = 0; i < left.Length; i++)
            {
                float dot = left[i].X * axisX + left[i].Y * axisY;
                aMin = Math.Min(aMin, dot);
                aMax = Math.Max(aMax, dot);
            }

            float bMin = float.MaxValue;
            float bMax = float.MinValue;
            for (int i = 0; i < right.Length; i++)
            {
                float dot = right[i].X * axisX + right[i].Y * axisY;
                bMin = Math.Min(bMin, dot);
                bMax = Math.Max(bMax, dot);
            }

            return aMax <= bMin + slop || bMax <= aMin + slop;
        }

        private static void V35Half(string mapId, float aspect, out float halfWidth, out float halfHeight)
        {
            if (aspect >= 2.4f)
            {
                switch (mapId)
                {
                    case "CC": halfWidth = 250f; halfHeight = 110f; return;
                    case "MP": halfWidth = 210f; halfHeight = 110f; return;
                    case "IB": halfWidth = 210f; halfHeight = 110f; return;
                    case "AP": halfWidth = 180f; halfHeight = 62f; return;
                    default: halfWidth = 250f; halfHeight = 110f; return;
                }
            }

            if (aspect >= 1.9f)
            {
                switch (mapId)
                {
                    case "CC": halfWidth = 230f; halfHeight = 115f; return;
                    case "MP": halfWidth = 200f; halfHeight = 115f; return;
                    case "IB": halfWidth = 190f; halfHeight = 110f; return;
                    case "AP": halfWidth = 156f; halfHeight = 78f; return;
                    default: halfWidth = 250f; halfHeight = 120f; return;
                }
            }

            switch (mapId)
            {
                case "CC": halfWidth = 200f; halfHeight = 105f; return;
                case "MP": halfWidth = 170f; halfHeight = 100f; return;
                case "IB": halfWidth = 165f; halfHeight = 100f; return;
                case "AP": halfWidth = 155f; halfHeight = 100f; return;
                default: halfWidth = 270f; halfHeight = 115f; return;
            }
        }

        private static Box[] SearchCards(float plateWidth, float plateHeight, Vec2[][] hexes, Box[] pins, Box detail, Box cue)
        {
            Box[] found = SearchCardsAt(plateWidth, plateHeight, hexes, pins, detail, cue, 8f);
            if (found != null)
                return found;
            return SearchCardsAt(plateWidth, plateHeight, hexes, pins, detail, cue, 4f);
        }

        private static Box[] SearchCardsAt(float plateWidth, float plateHeight, Vec2[][] hexes, Box[] pins, Box detail, Box cue, float step)
        {
            float[] widths = { 320f, 304f, 288f, 280f };
            float[] heights = { 110f, 104f, 100f, 96f };
            var pools = new List<Box>[MapIds.Length];
            for (int i = 0; i < MapIds.Length; i++)
            {
                var mine = new List<Box>(128);
                var seen = new Dictionary<int, int>();
                for (int size = 0; size < widths.Length; size++)
                {
                    float cardW = widths[size];
                    float cardH = heights[size];
                    float yMax = plateHeight - Edge - cardH;
                    float xMax = plateWidth - Edge - cardW;
                    for (float y = Edge; y <= yMax + 0.01f; y += step)
                    {
                        for (float x = Edge; x <= xMax + 0.01f; x += step)
                        {
                            var card = new Box(x, y, cardW, cardH);
                            if (!Clears(card, detail) || !OnPlate(card, plateWidth, plateHeight))
                                continue;
                            if (cue.W > 1f && Separation(card, cue) < 12f)
                                continue;
                            if (!ClearsPins(card, pins) || Leader(pins[i], card) < LeaderMin)
                                continue;
                            if (!SeatHolds(card, hexes[i], hexes))
                                continue;
                            int bucket = ((int)(x / 64f) * 73856093) ^ ((int)(y / 64f) * 19349663);
                            int count;
                            seen.TryGetValue(bucket, out count);
                            if (count >= 2)
                                continue;
                            seen[bucket] = count + 1;
                            mine.Add(card);
                        }
                    }
                }

                mine.Sort(delegate(Box a, Box b)
                {
                    float sa = Score(a, pins[i], hexes[i]);
                    float sb = Score(b, pins[i], hexes[i]);
                    return sa.CompareTo(sb);
                });
                if (mine.Count > 24)
                    mine.RemoveRange(24, mine.Count - 24);
                pools[i] = mine;
            }

            int[] order = new int[MapIds.Length];
            for (int i = 0; i < order.Length; i++)
                order[i] = i;
            Array.Sort(order, delegate(int a, int b) { return pools[a].Count.CompareTo(pools[b].Count); });
            var placed = new Box[MapIds.Length];
            var used = new bool[MapIds.Length];
            int nodes = 0;
            if (!PlaceNext(0, order, pools, placed, used, ref nodes))
                return null;
            return placed;
        }

        private static bool PlaceNext(int depth, int[] order, List<Box>[] pools, Box[] placed, bool[] used, ref int nodes)
        {
            nodes++;
            if (nodes > 80000)
                return false;
            if (depth >= order.Length)
                return true;
            int index = order[depth];
            List<Box> options = pools[index];
            for (int n = 0; n < options.Count; n++)
            {
                Box card = options[n];
                bool blocked = false;
                for (int i = 0; i < placed.Length; i++)
                {
                    if (!used[i])
                        continue;
                    if (Separation(card, placed[i]) < CardGap)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (blocked)
                    continue;
                placed[index] = card;
                used[index] = true;
                if (PlaceNext(depth + 1, order, pools, placed, used, ref nodes))
                    return true;
                used[index] = false;
            }

            return false;
        }

        private static float Score(Box card, Box pin, Vec2[] own)
        {
            float leader = Leader(pin, card);
            bool inside = RestsInside(card, own, InsideMargin);
            return leader + (inside ? 0f : 36f);
        }

        private static float CueReach(Box cue, Box pin)
        {
            return Distance(cue.Center, pin.Center);
        }

        private static Box[] CueSpots(float plateWidth, float plateHeight, Box[] pins, Vec2[][] hexes, Box detail, int selected)
        {
            var found = new List<Box>(8);
            Box pin = pins[selected];
            Vec2 pinCentre = pin.Center;
            for (int dist = 0; dist <= 140 && found.Count < 6; dist += 8)
            {
                for (int slide = 0; slide <= 120 && found.Count < 6; slide += 16)
                {
                    float[] slides = slide == 0 ? new[] { 0f } : new[] { -slide, (float)slide };
                    for (int s = 0; s < slides.Length && found.Count < 6; s++)
                    {
                        float offset = slides[s];
                        Box[] spots =
                        {
                            new Box(pinCentre.X - CueWidth * 0.5f + offset, pin.Y - 12f - CueHeight - dist, CueWidth, CueHeight),
                            new Box(pinCentre.X - CueWidth * 0.5f + offset, pin.YMax + 12f + dist, CueWidth, CueHeight),
                            new Box(pin.X - 12f - CueWidth - dist, pinCentre.Y - CueHeight * 0.5f + offset, CueWidth, CueHeight),
                            new Box(pin.XMax + 12f + dist, pinCentre.Y - CueHeight * 0.5f + offset, CueWidth, CueHeight)
                        };
                        for (int i = 0; i < spots.Length && found.Count < 6; i++)
                        {
                            Box spot = spots[i];
                            if (spot.X < Edge || spot.Y < Edge || spot.XMax > plateWidth - Edge || spot.YMax > plateHeight - Edge)
                                continue;
                            if (CueReach(spot, pin) > CueNearLimit)
                                continue;
                            if (!CueReadsAsSelected(spot.Center, pinCentre, hexes, pins, selected))
                                continue;
                            if (!Clears(spot, detail) || !ClearsPins(spot, pins) || EdgeCrosses(spot, hexes))
                                continue;
                            found.Add(spot);
                        }
                    }
                }
            }

            if (found.Count == 0)
                found.Add(default);
            return found.ToArray();
        }

        private static bool TryPlaceCue(
            float plateWidth,
            float plateHeight,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            out Box cue)
        {
            Box pin = pins[selected];
            Vec2 pinCentre = pin.Center;
            cue = new Box(pin.X, pin.YMax + 8f, CueWidth, CueHeight);
            for (int dist = 0; dist <= 140; dist += 4)
            {
                for (int slide = 0; slide <= 120; slide += 8)
                {
                    float[] slides = slide == 0 ? new[] { 0f } : new[] { -slide, (float)slide };
                    for (int s = 0; s < slides.Length; s++)
                    {
                        float offset = slides[s];
                        Box[] spots =
                        {
                            new Box(pinCentre.X - CueWidth * 0.5f + offset, pin.YMax + 12f + dist, CueWidth, CueHeight),
                            new Box(pinCentre.X - CueWidth * 0.5f + offset, pin.Y - 12f - CueHeight - dist, CueWidth, CueHeight),
                            new Box(pin.XMax + 12f + dist, pinCentre.Y - CueHeight * 0.5f + offset, CueWidth, CueHeight),
                            new Box(pin.X - 12f - CueWidth - dist, pinCentre.Y - CueHeight * 0.5f + offset, CueWidth, CueHeight)
                        };
                        for (int i = 0; i < spots.Length; i++)
                        {
                            Box spot = spots[i];
                            if (spot.X < Edge || spot.Y < Edge || spot.XMax > plateWidth - Edge || spot.YMax > plateHeight - Edge)
                                continue;
                            if (Distance(spot.Center, pinCentre) > CueNearLimit)
                                continue;
                            if (!CueReadsAsSelected(spot.Center, pinCentre, hexes, pins, selected))
                                continue;
                            if (!Clears(spot, detail))
                                continue;
                            if (!ClearsCards(spot, cards, 12f) || !ClearsPins(spot, pins) || EdgeCrosses(spot, hexes))
                                continue;
                            cue = spot;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool CueReadsAsSelected(Vec2 centre, Vec2 pin, Vec2[][] hexes, Box[] pins, int selected)
        {
            float own = Distance(centre, pin);
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i == selected)
                    continue;
                if (hexes[i] != null && PointInPolygon(centre, hexes[i]))
                    return false;
                if (Distance(centre, pins[i].Center) + 0.5f < own)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Height used for the cover window. A taller plate keeps this crop and stretches it,
        /// so side labels stay outside the full-height detail reserve.
        /// </summary>
        public static float CoverFrameHeight(float frameWidth, float frameHeight)
        {
            float limit = DetailClearCoverHeight(frameWidth);
            if (frameHeight < 2f || limit < 2f || frameHeight <= limit)
                return frameHeight;
            return limit;
        }

        private static float DetailClearCoverHeight(float plateWidth)
        {
            if (plateWidth < 2f)
                return 0f;
            float reserve = Clamp(plateWidth * 0.22f, 230f, 290f);
            float faRight = reserve - 10f + 2f;
            float enLeft = plateWidth - reserve + 10f - 2f;
            float faFrac = faRight / plateWidth;
            float enFrac = enLeft / plateWidth;
            if (faFrac >= 0.49f || enFrac <= 0.51f)
                return plateWidth * 9f / 16f;
            float cropFromLeft = 0.26f / (0.5f - faFrac);
            float cropFromRight = 0.25f / (enFrac - 0.5f);
            float minCrop = Math.Max(cropFromLeft, cropFromRight);
            if (minCrop >= 0.999f)
                return plateWidth * 9f / 16f;
            return plateWidth * 9f / (minCrop * 16f);
        }

        private static Box PinBox(int index, float plateWidth, float plateHeight)
        {
            float coverHeight = CoverFrameHeight(plateWidth, plateHeight);
            Cover(plateWidth, plateHeight, out float u, out float v, out float cropW, out float cropH);
            Vec2 label = LabelCentres[index];
            float px = (label.X - u) / Math.Max(0.001f, cropW) * plateWidth;
            float py = (label.Y - v) / Math.Max(0.001f, cropH) * coverHeight;
            if (coverHeight > 2f && plateHeight > coverHeight + 0.5f)
                py *= plateHeight / coverHeight;
            float x = Clamp(px - PinSize * 0.5f, SafeLeft, Math.Max(SafeLeft, plateWidth - SafeRight - PinSize));
            float y = Clamp(py - PinSize * 0.5f, SafeTop, Math.Max(SafeTop, plateHeight - SafeBottom - PinSize));
            return new Box(x, y, PinSize, PinSize);
        }

        private static void Cover(float frameWidth, float frameHeight, out float u, out float v, out float width, out float height)
        {
            float coverHeight = CoverFrameHeight(frameWidth, frameHeight);
            float frameAspect = frameWidth > 2f && coverHeight > 2f ? frameWidth / coverHeight : SourceAspect;
            if (frameAspect >= SourceAspect)
            {
                width = 1f;
                height = SourceAspect / frameAspect;
                u = 0f;
                v = Clamp(FocalY - height * 0.5f, 0f, Math.Max(0f, 1f - height));
                return;
            }

            height = 1f;
            width = frameAspect / SourceAspect;
            u = Clamp(FocalX - width * 0.5f, 0f, Math.Max(0f, 1f - width));
            v = 0f;
        }

        private static bool OnPlate(Box card, float plateWidth, float plateHeight)
        {
            return card.X >= Edge - 0.01f && card.Y >= Edge - 0.01f
                && card.XMax <= plateWidth - Edge + 0.01f
                && card.YMax <= plateHeight - Edge + 0.01f;
        }

        private static bool Clears(Box card, Box detail)
        {
            if (detail.W < 1f || detail.H < 1f)
                return true;
            return card.XMin >= detail.XMax || detail.XMin >= card.XMax
                || card.YMin >= detail.YMax || detail.YMin >= card.YMax;
        }

        private static bool ClearsPins(Box card, Box[] pins)
        {
            for (int i = 0; i < pins.Length; i++)
            {
                if (Separation(card, pins[i]) < PinGap)
                    return false;
            }

            return true;
        }

        private static bool ClearsCards(Box cue, Box[] cards, float margin)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].W < 1f)
                    continue;
                if (Separation(cue, cards[i]) < margin)
                    return false;
            }

            return true;
        }

        private static float Separation(Box a, Box b)
        {
            float dx = AxisGap(a.XMin, a.XMax, b.XMin, b.XMax);
            float dy = AxisGap(a.YMin, a.YMax, b.YMin, b.YMax);
            if (dx >= 0f && dy >= 0f)
                return Math.Min(dx, dy);
            return dx >= 0f ? dx : dy;
        }

        private static float AxisGap(float a0, float a1, float b0, float b1)
        {
            if (a1 <= b0)
                return b0 - a1;
            if (b1 <= a0)
                return a0 - b1;
            return -1f;
        }

        private static float Leader(Box pin, Box card)
        {
            Vec2 centre = pin.Center;
            float x = Clamp(centre.X, card.XMin, card.XMax);
            float y = Clamp(centre.Y, card.YMin, card.YMax);
            return Distance(centre, new Vec2(x, y));
        }

        private static float Distance(Vec2 a, Vec2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static bool SameShape(Vec2[] shape, Vec2[] own)
        {
            return ReferenceEquals(shape, own);
        }

        private static bool RestsInside(Box card, Vec2[] shape, float margin)
        {
            if (shape == null || shape.Length < 3)
                return false;
            Vec2[] corners = Corners(card);
            for (int edge = 0; edge < corners.Length; edge++)
            {
                Vec2 a = corners[edge];
                Vec2 b = corners[(edge + 1) % corners.Length];
                for (int step = 0; step < 4; step++)
                {
                    float t = step / 4f;
                    var point = new Vec2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
                    if (!PointInPolygon(point, shape) || DistanceToEdge(point, shape) < margin)
                        return false;
                }
            }

            return true;
        }

        private static float PerimeterGap(Box card, Vec2[] shape)
        {
            if (shape == null || shape.Length < 3)
                return float.MaxValue;
            float best = float.MaxValue;
            Vec2[] corners = Corners(card);
            for (int edge = 0; edge < corners.Length; edge++)
            {
                Vec2 a = corners[edge];
                Vec2 b = corners[(edge + 1) % corners.Length];
                for (int step = 0; step <= 4; step++)
                {
                    float t = step / 4f;
                    var point = new Vec2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
                    best = Math.Min(best, DistanceToEdge(point, shape));
                }
            }

            return best;
        }

        private static bool RectHitsPolygon(Box rect, Vec2[] shape)
        {
            if (shape == null || shape.Length < 3)
                return false;
            Vec2[] corners = Corners(rect);
            for (int i = 0; i < corners.Length; i++)
            {
                if (PointInPolygon(corners[i], shape))
                    return true;
            }

            for (int i = 0; i < shape.Length; i++)
            {
                if (Contains(rect, shape[i]))
                    return true;
                Vec2 next = shape[(i + 1) % shape.Length];
                if (SegmentHitsRect(shape[i], next, rect))
                    return true;
            }

            return false;
        }

        private static Vec2[] Corners(Box card)
        {
            return new[]
            {
                new Vec2(card.XMin, card.YMin),
                new Vec2(card.XMax, card.YMin),
                new Vec2(card.XMax, card.YMax),
                new Vec2(card.XMin, card.YMax)
            };
        }

        // Unity Rect.Contains is inclusive on the minimum and exclusive on the maximum.
        private static bool Contains(Box rect, Vec2 point)
        {
            return point.X >= rect.XMin && point.X < rect.XMax && point.Y >= rect.YMin && point.Y < rect.YMax;
        }

        private static bool SegmentHitsRect(Vec2 a, Vec2 b, Box rect)
        {
            if (Contains(rect, a) || Contains(rect, b))
                return true;
            var c0 = new Vec2(rect.XMin, rect.YMin);
            var c1 = new Vec2(rect.XMax, rect.YMin);
            var c2 = new Vec2(rect.XMax, rect.YMax);
            var c3 = new Vec2(rect.XMin, rect.YMax);
            return SegmentsCross(a, b, c0, c1) || SegmentsCross(a, b, c1, c2)
                || SegmentsCross(a, b, c2, c3) || SegmentsCross(a, b, c3, c0);
        }

        private static bool SegmentsCross(Vec2 a, Vec2 b, Vec2 c, Vec2 d)
        {
            float ab = Orient(a, b, c);
            float ab2 = Orient(a, b, d);
            float cd = Orient(c, d, a);
            float cd2 = Orient(c, d, b);
            return ab * ab2 < 0f && cd * cd2 < 0f;
        }

        private static float Orient(Vec2 a, Vec2 b, Vec2 c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        private static bool PointInPolygon(Vec2 point, Vec2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vec2 a = polygon[i];
                Vec2 b = polygon[j];
                if ((a.Y > point.Y) == (b.Y > point.Y))
                    continue;
                float x = (b.X - a.X) * (point.Y - a.Y) / ((b.Y - a.Y) + 0.0001f) + a.X;
                if (point.X < x)
                    inside = !inside;
            }

            return inside;
        }

        private static float DistanceToEdge(Vec2 point, Vec2[] polygon)
        {
            float best = float.MaxValue;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vec2 a = polygon[i];
                Vec2 b = polygon[(i + 1) % polygon.Length];
                float ex = b.X - a.X;
                float ey = b.Y - a.Y;
                float lengthSq = ex * ex + ey * ey;
                float t = lengthSq < 0.0001f
                    ? 0f
                    : Clamp(((point.X - a.X) * ex + (point.Y - a.Y) * ey) / lengthSq, 0f, 1f);
                float dx = point.X - (a.X + ex * t);
                float dy = point.Y - (a.Y + ey * t);
                best = Math.Min(best, (float)Math.Sqrt(dx * dx + dy * dy));
            }

            return best;
        }

        private static Spec PickSpec(string mapId, float frameWidth, float frameHeight)
        {
            float aspect = frameHeight > 2f ? frameWidth / frameHeight : 1.6f;
            if (aspect >= 2.4f)
                return Wide(mapId);
            if (aspect >= 1.9f)
                return Landscape(mapId);
            return Square(mapId);
        }

        private static Spec Landscape(string mapId)
        {
            // Same flat-hex ratio as v35, drawn smaller about the pin so five cards fit on the open plate.
            switch (mapId)
            {
                case "CC": return SpecOf(1294f, 971f, 80f, 152.95f, 76.48f);
                case "MP": return SpecOf(1294f, 269f, 318f, 147.40f, 84.75f);
                case "IB": return SpecOf(1294f, 955f, 319f, 144.02f, 83.38f);
                case "AP": return SpecOf(1294f, 737f, 537f, 155.22f, 77.61f);
                default: return SpecOf(1294f, 258f, 80f, 159.25f, 76.44f);
            }
        }

        private static Spec Wide(string mapId)
        {
            switch (mapId)
            {
                case "CC": return SpecOf(1774f, 1340f, 146f, 250f, 110f);
                case "MP": return SpecOf(1774f, 420f, 400f, 210f, 110f);
                case "IB": return SpecOf(1774f, 1320f, 400f, 210f, 110f);
                case "AP": return SpecOf(1774f, 900f, 482f, 180f, 62f);
                default: return SpecOf(1774f, 460f, 146f, 250f, 110f);
            }
        }

        private static Spec Square(string mapId)
        {
            // Same flat-hex ratio as v35, drawn smaller about the pin so five cards fit on the open plate.
            switch (mapId)
            {
                case "CC": return SpecOf(1039.6f, 796f, 94f, 152.80f, 80.22f);
                case "MP": return SpecOf(1039.6f, 154f, 295f, 143.14f, 84.20f);
                case "IB": return SpecOf(1039.6f, 782f, 298f, 143.88f, 87.20f);
                case "AP": return SpecOf(1039.6f, 651f, 535f, 140.28f, 90.50f);
                default: return SpecOf(1039.6f, 132f, 93f, 163.62f, 69.69f);
            }
        }

        private static Spec SpecOf(float reference, float centerX, float centerY, float halfWidth, float halfHeight)
        {
            return new Spec
            {
                ReferenceWidth = reference,
                CenterX = centerX,
                CenterY = centerY,
                HalfWidth = halfWidth,
                HalfHeight = halfHeight
            };
        }

        private static Vec2[] FlatHex(float centerX, float centerY, float halfWidth, float halfHeight)
        {
            float dx = halfHeight / 1.7320508f;
            return new[]
            {
                new Vec2(centerX - halfWidth + dx, centerY - halfHeight),
                new Vec2(centerX + halfWidth - dx, centerY - halfHeight),
                new Vec2(centerX + halfWidth, centerY),
                new Vec2(centerX + halfWidth - dx, centerY + halfHeight),
                new Vec2(centerX - halfWidth + dx, centerY + halfHeight),
                new Vec2(centerX - halfWidth, centerY)
            };
        }

        private static int IndexOf(string mapId)
        {
            for (int i = 0; i < MapIds.Length; i++)
            {
                if (string.Equals(MapIds[i], mapId, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
