using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

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
        /// <summary>A card must stay this far from every other district's leader segment.</summary>
        public const float ForeignLeaderClearance = 4f;
        private const float CueNearLimit = 160f;
        private const float CueWidth = 220f;
        private const float CueHeight = 44f;
        /// <summary>A card centre may sit this far outside its own hex edge.</summary>
        public const float CardReach = 120f;
        /// <summary>Halo is drawn this far outside the hex vertices. The clip mask cuts anything past the plate.</summary>
        public const float HaloReach = 22f;
        /// <summary>Laid-out gap. The check rejects anything under <see cref="MedalGapMinimum"/>.</summary>
        private const float MedalGap = 8f;
        public const float MedalGapMinimum = 6f;
        /// <summary>Smallest portrait kept beside the detail reserve. A smaller tile drops the reserve.</summary>
        public const float FullPortraitMinWidth = 124f;

        /// <summary>Smallest portrait height kept beside the detail reserve.</summary>
        public const float FullPortraitMinHeight = 156f;

        /// <summary>Shortest zoom cue the packer seats. A later fit must not widen it.</summary>
        public const float PackedCueMinWidth = 124f;

        public const float PortraitMinWidth = 112f;
        public const float PortraitMaxWidth = 160f;
        /// <summary>Capture prints sizes with one decimal of rounding. A tile that prints as the max still passes.</summary>
        public const float PortraitMaxSlop = 0.5f;
        public const float PortraitMinHeight = 140f;
        public const float PortraitMaxHeight = 212f;
        /// <summary>Compact 112-wide tiles may step one point under <see cref="NameFontFloor"/> so the longest Persian word still fits.</summary>
        public const float PortraitCompactFontFloor = 15f;
        public const float NameFontCeiling = 22f;
        public const float NameFontFloor = 16f;
        public const float StripCaptionCeiling = 12f;
        public const float StripCaptionFloor = 9f;

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
        /// (top 8, bottom 128) inside the header and footer bands. The strip widget is
        /// inactive; the inset stays so district glow still clears the plate.
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

        /// <summary>
        /// True when the detail rect sits on the left half of the plate. Callers that inset
        /// seating read this from the rect. Farsi places that rect at x = 10.
        /// </summary>
        public static bool DetailOccupiesLeft(float plateWidth, float detailX, float detailWidth)
        {
            if (detailWidth <= 1f || plateWidth <= 1f)
                return false;
            return detailX + detailWidth * 0.5f < plateWidth * 0.5f;
        }

        /// <summary>
        /// Map viewport. With the reserve off this is the whole plate. With the reserve on it is
        /// the open rect beside the detail panel. The painting itself cover-fits the full plate.
        /// </summary>
        public static Box OpenViewport(float plateWidth, float plateHeight, bool fa, bool reserveOn)
        {
            if (plateWidth < 2f || plateHeight < 2f)
                return new Box(0f, 0f, Math.Max(2f, plateWidth), Math.Max(2f, plateHeight));
            Box detail = DetailRect(plateWidth, plateHeight, fa, reserveOn);
            if (detail.W < 1f)
                return new Box(0f, 0f, plateWidth, plateHeight);
            if (DetailOccupiesLeft(plateWidth, detail.X, detail.W))
                return new Box(detail.XMax, 0f, Math.Max(2f, plateWidth - detail.XMax), plateHeight);
            return new Box(0f, 0f, Math.Max(2f, detail.XMin), plateHeight);
        }

        /// <summary>
        /// Farsi mirrors the theatre chrome the same way on every plate: the reserve sits on the
        /// trailing edge, medal 0 stays first in reading order, and the cue keeps its place in the
        /// open viewport. Map geography is not flipped.
        /// </summary>
        public static bool FaChromeMirrors(out string failure)
        {
            failure = null;
            const float plateWidth = 1294f;
            const float plateHeight = 724f;
            Box enDetail = DetailRect(plateWidth, plateHeight, false, true);
            Box faDetail = DetailRect(plateWidth, plateHeight, true, true);
            float mirrored = plateWidth - enDetail.XMax;
            if (Math.Abs(faDetail.X - 10f) > 1f
                || !DetailOccupiesLeft(plateWidth, faDetail.X, faDetail.W)
                || DetailOccupiesLeft(plateWidth, enDetail.X, enDetail.W)
                || enDetail.Center.X <= plateWidth * 0.5f || faDetail.Center.X >= plateWidth * 0.5f
                || Math.Abs(faDetail.X - mirrored) > 1f)
            {
                failure = "reserve side";
                return false;
            }

            float stripWidth = Math.Max(200f, plateWidth - 16f);
            for (int i = 0; i < 4; i++)
            {
                StripMedal(plateWidth, false, i, out Box enSlot, out _, out _);
                StripMedal(plateWidth, true, i, out Box faSlot, out _, out _);
                float mirrorX = stripWidth - enSlot.XMax;
                if (Math.Abs(faSlot.X - mirrorX) > 1f || Math.Abs(faSlot.W - enSlot.W) > 1f)
                {
                    failure = "medal " + i;
                    return false;
                }
            }

            StripMedal(plateWidth, true, 0, out Box recruit, out _, out _);
            StripMedal(plateWidth, true, 3, out Box commander, out _, out _);
            if (recruit.X <= commander.X)
            {
                failure = "medal order";
                return false;
            }

            Layout en = Arrange(plateWidth, plateHeight, false, true, "DB");
            Layout fa = Arrange(plateWidth, plateHeight, true, true, "DB");
            Box enView = OpenViewport(plateWidth, plateHeight, false, true);
            Box faView = OpenViewport(plateWidth, plateHeight, true, true);
            if (Math.Abs((en.Cue.X - enView.X) - (fa.Cue.X - faView.X)) > 1f
                || Math.Abs(en.Cue.Y - fa.Cue.Y) > 1f
                || Math.Abs(en.Cue.W - fa.Cue.W) > 1f)
            {
                failure = "cue";
                return false;
            }

            Layout enOpen = Arrange(plateWidth, plateHeight, false, false, "DB");
            Layout faOpen = Arrange(plateWidth, plateHeight, true, false, "DB");
            if (Math.Abs(enOpen.Cue.X - faOpen.Cue.X) > 1f || Math.Abs(enOpen.Cue.Y - faOpen.Cue.Y) > 1f)
            {
                failure = "open cue";
                return false;
            }

            return true;
        }

        public static Box ZoomHintRect(float plateWidth, float plateHeight)
        {
            float top = plateHeight - ZoomHintTopGap;
            float width = Math.Max(1f, plateWidth - ZoomHintInsetX);
            return new Box(ZoomHintInsetX, top, width, ZoomHintHeight);
        }

        /// <summary>Capture shot stamped onto every violation. Notch is "none", "notch", or "safe".</summary>
        public static string ShotRatio = "unset";
        public static string ShotNotch = "none";
        public static string ShotLocale = "en";
        public static string ShotSelected = "DB";
        public static bool ShotReserve;

        /// <summary>
        /// True only when this layout pass seated with the detail reserve applied.
        /// False means the plate was not inset and the detail panel is a bottom sheet.
        /// </summary>
        public static bool TheatreDetailReserved { get; private set; }

        /// <summary>
        /// Capture sets this false until the canvas has been forced to the shot size.
        /// Theatre seats and writes its JSON only while this is true, so one settled pass is the dump.
        /// </summary>
        public static bool SeatingReady = true;
        public static float ShotCanvasWidth;
        public static float ShotCanvasHeight;
        public static float ShotInsetLeft;
        public static float ShotInsetTop;
        public static float ShotInsetRight;
        public static float ShotInsetBottom;
        public static string DumpDirectory;

        /// <summary>
        /// 4:3 open viewport below which the detail reserve is not applied.
        /// X is the width floor (780) and Y is the height floor (704). Either axis under the floor skips the reserve.
        /// </summary>
        public static readonly Vec2 ReserveViewportFloor = new Vec2(780f, 704f);

        public static void SetShot(
            string ratio,
            string notch,
            string locale,
            bool reserve,
            float canvasWidth,
            float canvasHeight,
            float insetLeft,
            float insetTop,
            float insetRight,
            float insetBottom)
        {
            ShotRatio = string.IsNullOrEmpty(ratio) ? "unset" : ratio;
            ShotNotch = string.IsNullOrEmpty(notch) ? "none" : notch;
            ShotLocale = string.IsNullOrEmpty(locale) ? "en" : locale;
            ShotReserve = reserve;
            ShotCanvasWidth = canvasWidth;
            ShotCanvasHeight = canvasHeight;
            ShotInsetLeft = insetLeft;
            ShotInsetTop = insetTop;
            ShotInsetRight = insetRight;
            ShotInsetBottom = insetBottom;
        }

        public static string Stamp(string message)
        {
            if (string.IsNullOrEmpty(message) || message.IndexOf(" notch=", StringComparison.Ordinal) >= 0)
                return message;
            string locale = string.IsNullOrEmpty(ShotLocale) ? "en" : ShotLocale;
            return message
                + " ratio=" + ShotRatio
                + " notch=" + ShotNotch
                + " locale=" + locale
                + " reserve=" + (ShotReserve ? "on" : "off");
        }

        public static string DumpFileName(float plateWidth, float plateHeight, string selected)
        {
            string ratio = string.IsNullOrEmpty(ShotRatio) ? "unset" : ShotRatio.Replace(':', 'x');
            string notch = string.IsNullOrEmpty(ShotNotch) ? "none" : ShotNotch;
            string locale = string.IsNullOrEmpty(ShotLocale) ? "en" : ShotLocale;
            string map = string.IsNullOrEmpty(selected) ? "DB" : selected;
            return "seat-" + ratio + "-" + notch + "-" + locale
                + "-reserve-" + (ShotReserve ? "on" : "off")
                + "-plate-" + plateWidth.ToString("0.0", CultureInfo.InvariantCulture)
                + "x" + plateHeight.ToString("0.0", CultureInfo.InvariantCulture)
                + "-sel-" + map + ".json";
        }

        /// <summary>Safe-frame fit. Zero insets leave the canvas unchanged. Matches <c>SkirmishLibrarySafeFrame</c>.</summary>
        public static void ApplySafeFrame(
            float canvasWidth,
            float canvasHeight,
            float insetLeft,
            float insetTop,
            float insetRight,
            float insetBottom,
            out float usableWidth,
            out float usableHeight)
        {
            float horizontal = (insetLeft + insetRight) / Math.Max(1f, canvasWidth);
            float vertical = (insetTop + insetBottom) / Math.Max(1f, canvasHeight);
            if (horizontal + vertical < 0.004f)
            {
                usableWidth = canvasWidth;
                usableHeight = canvasHeight;
                return;
            }

            float fit = Clamp(Math.Min(1f - horizontal, 1f - vertical), 0.72f, 1f);
            usableWidth = canvasWidth * fit;
            usableHeight = canvasHeight * fit;
        }

        public static bool CanvasIsFourByThree(float canvasWidth, float canvasHeight)
        {
            if (canvasWidth < 2f || canvasHeight < 2f)
                return false;
            return canvasWidth / canvasHeight < 1.5f;
        }

        public static bool ShotIsFourByThree()
        {
            if (string.Equals(ShotRatio, "4:3", StringComparison.Ordinal))
                return true;
            return CanvasIsFourByThree(ShotCanvasWidth, ShotCanvasHeight);
        }

        /// <summary>
        /// Detail reserve. A 4:3 canvas leaves it off when the open viewport is under <see cref="ReserveViewportFloor"/>.
        /// Any ratio also leaves it off when the reserved plate cannot hold full portraits
        /// (at least <see cref="FullPortraitMinWidth"/> by <see cref="FullPortraitMinHeight"/>, names fitting, no word breaks).
        /// The detail panel is then a bottom sheet and the seats use the full plate.
        /// </summary>
        public static bool DetailReserveOn(
            float plateWidth,
            float plateHeight,
            bool fa,
            bool requested,
            bool fourByThree,
            string selected = null)
        {
            if (!requested || plateWidth < 600f || plateHeight < 2f)
                return false;
            Box view = OpenViewport(plateWidth, plateHeight, fa, true);
            bool holds = view.W + 0.5f >= ReserveViewportFloor.X && view.H + 0.5f >= ReserveViewportFloor.Y;
            if (!holds && fourByThree)
                return false;
            if (string.IsNullOrEmpty(selected))
                selected = "DB";
            return FullPortraitSeatsHold(plateWidth, plateHeight, fa, selected);
        }

        /// <summary>
        /// Reserved plate can keep the side panel only when every portrait is a full seat and its name fits.
        /// </summary>
        public static bool FullPortraitSeatsHold(float plateWidth, float plateHeight, bool fa, string selected)
        {
            if (string.IsNullOrEmpty(selected))
                selected = "DB";
            Layout layout = Arrange(plateWidth, plateHeight, fa, true, selected);
            if (layout.Seats == null || !layout.CardsLegal || !layout.CueNear || !layout.HexAspectLegal)
                return false;
            bool wide = plateHeight > 2f && plateWidth / plateHeight >= 2.4f;
            int selectedIndex = IndexOf(selected);
            if (selectedIndex < 0)
                selectedIndex = 0;
            Box cue = layout.Cue;
            for (int i = 0; i < layout.Seats.Length; i++)
            {
                Box card = layout.Seats[i].Card;
                if (card.W + 0.5f < FullPortraitMinWidth || card.H + 0.5f < FullPortraitMinHeight)
                    return false;
                float band = NameBandHeight(card, CardsOf(layout), i, DetailRect(plateWidth, plateHeight, fa, true), cue, ZoomHintRect(plateWidth, plateHeight), plateHeight);
                if (!TryFitZoneName(ZoneTitle(layout.Seats[i].MapId, fa), card, band, wide, layout.Seats[i].Hex, HexesOf(layout), out _, out _, out _))
                    return false;
                if (i == selectedIndex
                    && cue.W > 1f
                    && SegmentHitsRect(layout.Seats[i].Pin.Center, NearestOn(card, layout.Seats[i].Pin.Center), cue))
                    return false;
            }

            return true;
        }

        private static Box[] CardsOf(Layout layout)
        {
            var cards = new Box[layout.Seats.Length];
            for (int i = 0; i < layout.Seats.Length; i++)
                cards[i] = layout.Seats[i].Card;
            return cards;
        }

        private static Vec2[][] HexesOf(Layout layout)
        {
            var hexes = new Vec2[layout.Seats.Length][];
            for (int i = 0; i < layout.Seats.Length; i++)
                hexes[i] = layout.Seats[i].Hex;
            return hexes;
        }

        public struct NameMeasure
        {
            public string MapId;
            public string Text;
            public float Font;
            public float BoxWidth;
            public float BoxHeight;
            public float PreferredWidth;
            public float PreferredHeight;
            public float StatusHeight;
            public float Band;
            public float CardWidth;
            public float CardHeight;
        }

        public static string FormatDump(float plateWidth, float plateHeight, Box detail, Box viewport, NameMeasure[] names)
        {
            var text = new StringBuilder(1024);
            text.Append("{\n");
            text.Append("  \"ratio\": \"").Append(Json(ShotRatio)).Append("\",\n");
            text.Append("  \"notch\": \"").Append(Json(ShotNotch)).Append("\",\n");
            text.Append("  \"locale\": \"").Append(Json(ShotLocale)).Append("\",\n");
            text.Append("  \"selected\": \"").Append(Json(ShotSelected)).Append("\",\n");
            text.Append("  \"reserve\": ").Append(ShotReserve ? "true" : "false").Append(",\n");
            text.Append("  \"canvas\": [").Append(Num(ShotCanvasWidth)).Append(", ").Append(Num(ShotCanvasHeight)).Append("],\n");
            text.Append("  \"insets\": { \"left\": ").Append(Num(ShotInsetLeft))
                .Append(", \"top\": ").Append(Num(ShotInsetTop))
                .Append(", \"right\": ").Append(Num(ShotInsetRight))
                .Append(", \"bottom\": ").Append(Num(ShotInsetBottom)).Append(" },\n");
            text.Append("  \"plate\": { \"x\": 0, \"y\": 0, \"w\": ").Append(Num(plateWidth))
                .Append(", \"h\": ").Append(Num(plateHeight)).Append(" },\n");
            text.Append("  \"detail\": { \"x\": ").Append(Num(detail.X)).Append(", \"y\": ").Append(Num(detail.Y))
                .Append(", \"w\": ").Append(Num(detail.W)).Append(", \"h\": ").Append(Num(detail.H)).Append(" },\n");
            text.Append("  \"viewport\": { \"x\": ").Append(Num(viewport.X)).Append(", \"y\": ").Append(Num(viewport.Y))
                .Append(", \"w\": ").Append(Num(viewport.W)).Append(", \"h\": ").Append(Num(viewport.H)).Append(" },\n");
            float minW = 0f;
            float maxW = 0f;
            float minH = 0f;
            float maxH = 0f;
            bool anyCard = false;
            if (names != null)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i].CardWidth < 1f)
                        continue;
                    if (!anyCard)
                    {
                        minW = maxW = names[i].CardWidth;
                        minH = maxH = names[i].CardHeight;
                        anyCard = true;
                    }
                    else
                    {
                        minW = Math.Min(minW, names[i].CardWidth);
                        maxW = Math.Max(maxW, names[i].CardWidth);
                        minH = Math.Min(minH, names[i].CardHeight);
                        maxH = Math.Max(maxH, names[i].CardHeight);
                    }
                }
            }

            text.Append("  \"band\": { \"portraitMinW\": ").Append(PortraitMinWidth.ToString("0", CultureInfo.InvariantCulture))
                .Append(", \"portraitMaxW\": ").Append(PortraitMaxWidth.ToString("0", CultureInfo.InvariantCulture))
                .Append(", \"portraitMinH\": ").Append(PortraitMinHeight.ToString("0", CultureInfo.InvariantCulture))
                .Append(", \"portraitMaxH\": ").Append(PortraitMaxHeight.ToString("0", CultureInfo.InvariantCulture))
                .Append(", \"chosenMinW\": ")
                .Append(Num(minW)).Append(", \"chosenMaxW\": ").Append(Num(maxW))
                .Append(", \"chosenMinH\": ").Append(Num(minH)).Append(", \"chosenMaxH\": ").Append(Num(maxH)).Append(" },\n");
            text.Append("  \"names\": [\n");
            int count = names == null ? 0 : names.Length;
            for (int i = 0; i < count; i++)
            {
                NameMeasure name = names[i];
                text.Append("    { \"id\": \"").Append(Json(name.MapId))
                    .Append("\", \"text\": \"").Append(Json(name.Text))
                    .Append("\", \"font\": ").Append(Num(name.Font))
                    .Append(", \"boxW\": ").Append(Num(name.BoxWidth))
                    .Append(", \"boxH\": ").Append(Num(name.BoxHeight))
                    .Append(", \"preferredW\": ").Append(Num(name.PreferredWidth))
                    .Append(", \"preferredH\": ").Append(Num(name.PreferredHeight))
                    .Append(", \"statusH\": ").Append(Num(name.StatusHeight))
                    .Append(", \"band\": ").Append(Num(name.Band))
                    .Append(", \"cardW\": ").Append(Num(name.CardWidth))
                    .Append(", \"cardH\": ").Append(Num(name.CardHeight)).Append(" }");
                if (i + 1 < count)
                    text.Append(",");
                text.Append("\n");
            }

            text.Append("  ]\n}\n");
            return text.ToString();
        }

        public static bool TryWriteDump(string fileName, string json)
        {
            if (string.IsNullOrEmpty(DumpDirectory) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(json))
                return false;
            Directory.CreateDirectory(DumpDirectory);
            string path = Path.Combine(DumpDirectory, fileName);
            File.WriteAllText(path, json);
            return true;
        }

        public static bool TryReadDump(
            string json,
            out float plateWidth,
            out float plateHeight,
            out bool reserve,
            out string ratio,
            out string notch,
            out string locale,
            out string selected)
        {
            plateWidth = 0f;
            plateHeight = 0f;
            reserve = false;
            ratio = "unset";
            notch = "none";
            locale = "en";
            selected = "DB";
            if (string.IsNullOrEmpty(json) || !JsonStringsAreClean(json))
                return false;
            ratio = ReadString(json, "ratio", ratio);
            notch = ReadString(json, "notch", notch);
            locale = ReadString(json, "locale", locale);
            selected = ReadString(json, "selected", selected);
            reserve = json.IndexOf("\"reserve\": true", StringComparison.Ordinal) >= 0
                || json.IndexOf("\"reserve\":true", StringComparison.Ordinal) >= 0;
            int plate = json.IndexOf("\"plate\"", StringComparison.Ordinal);
            if (plate < 0)
                return false;
            plateWidth = ReadNumber(json, "\"w\"", plate);
            plateHeight = ReadNumber(json, "\"h\"", plate);
            return plateWidth > 2f && plateHeight > 2f;
        }

        /// <summary>Records whether the seating pass actually applied the detail reserve.</summary>
        public static void NoteDetailReserve(bool applied)
        {
            TheatreDetailReserved = applied;
        }

        /// <summary>
        /// Writes the plate, reserve and selected district that <see cref="Arrange"/> just used.
        /// The file name includes the plate, so a later pass at another size cannot replace it.
        /// </summary>
        public static void NoteSeating(float plateWidth, float plateHeight, bool fa, bool reserve, string selected, Layout layout)
        {
            ShotReserve = reserve;
            NoteDetailReserve(reserve);
            ShotSelected = string.IsNullOrEmpty(selected) ? "DB" : selected;
            if (!SeatingReady || string.IsNullOrEmpty(DumpDirectory))
                return;
            Box detail = DetailRect(plateWidth, plateHeight, fa, reserve);
            Box viewport = OpenViewport(plateWidth, plateHeight, fa, reserve);
            var names = new NameMeasure[MapIds.Length];
            bool wide = plateHeight > 2f && plateWidth / plateHeight >= 2.4f;
            var cards = new Box[MapIds.Length];
            var hexes = new Vec2[MapIds.Length][];
            for (int i = 0; i < MapIds.Length; i++)
            {
                cards[i] = layout.Seats != null && i < layout.Seats.Length ? layout.Seats[i].Card : default;
                hexes[i] = layout.Seats != null && i < layout.Seats.Length ? layout.Seats[i].Hex : null;
            }

            for (int i = 0; i < MapIds.Length; i++)
            {
                Box card = cards[i];
                string title = ZoneTitle(MapIds[i], fa);
                float band = card.W > 1f
                    ? NameBandHeight(card, cards, i, detail, layout.Cue, ZoomHintRect(plateWidth, plateHeight), plateHeight)
                    : 0f;
                bool fitted = TryFitZoneName(title, card, band, wide, hexes[i], hexes, out float font, out float nameHeight, out _);
                float fontUsed = font > 0.5f ? font : NameFontFloor;
                float box = Math.Max(1f, card.W - 12f);
                float lineWidth = card.W > 1f ? MeasuredTitleWidth(title, fontUsed, box) : 0f;
                if (lineWidth < 0.5f && !string.IsNullOrEmpty(title))
                    lineWidth = TextWidth(title, fontUsed);
                float lineHeight = fitted && nameHeight > 0.5f
                    ? nameHeight
                    : fontUsed * NameLineFactor(title) + 2f;
                names[i] = new NameMeasure
                {
                    MapId = MapIds[i],
                    Text = title,
                    Font = fontUsed,
                    BoxWidth = box,
                    BoxHeight = lineHeight,
                    PreferredWidth = lineWidth,
                    PreferredHeight = lineHeight,
                    Band = band,
                    CardWidth = card.W,
                    CardHeight = card.H
                };
            }

            DrawnPlateWidth = plateWidth;
            DrawnPlateHeight = plateHeight;
            DrawnDetail = detail;
            DrawnViewport = viewport;
            DrawnNames = names;
            TryWriteDump(
                DumpFileName(plateWidth, plateHeight, ShotSelected),
                FormatDump(plateWidth, plateHeight, detail, viewport, names));
        }

        private static float DrawnPlateWidth;
        private static float DrawnPlateHeight;
        private static Box DrawnDetail;
        private static Box DrawnViewport;
        private static NameMeasure[] DrawnNames;

        /// <summary>
        /// Replaces one dumped name with the width TMP measured after the label was fitted.
        /// The replay reads these numbers instead of estimating the Persian line again.
        /// </summary>
        public static void NoteDrawnName(
            string mapId,
            float font,
            float boxWidth,
            float boxHeight,
            float preferredWidth,
            float preferredHeight,
            float statusHeight,
            float cardHeight)
        {
            if (DrawnNames == null || string.IsNullOrEmpty(mapId))
                return;
            for (int i = 0; i < DrawnNames.Length; i++)
            {
                if (!string.Equals(DrawnNames[i].MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                NameMeasure name = DrawnNames[i];
                name.Font = font;
                name.BoxWidth = boxWidth;
                name.BoxHeight = boxHeight;
                name.PreferredWidth = preferredWidth;
                name.PreferredHeight = preferredHeight;
                name.StatusHeight = statusHeight;
                if (cardHeight > 1f)
                    name.CardHeight = cardHeight;
                DrawnNames[i] = name;
                break;
            }

            if (!SeatingReady || string.IsNullOrEmpty(DumpDirectory))
                return;
            TryWriteDump(
                DumpFileName(DrawnPlateWidth, DrawnPlateHeight, ShotSelected),
                FormatDump(DrawnPlateWidth, DrawnPlateHeight, DrawnDetail, DrawnViewport, DrawnNames));
        }

        /// <summary>
        /// True when a dumped name is wider than its box. <paramref name="mapId"/> is the first overflow.
        /// </summary>
        public static bool TryReadNameOverflow(
            string json,
            out string mapId,
            out string text,
            out float preferred,
            out float boxWidth)
        {
            mapId = string.Empty;
            text = string.Empty;
            preferred = 0f;
            boxWidth = 0f;
            if (string.IsNullOrEmpty(json) || !JsonStringsAreClean(json))
                return false;
            int names = json.IndexOf("\"names\"", StringComparison.Ordinal);
            if (names < 0)
                return false;
            int cursor = names;
            while (cursor < json.Length)
            {
                int id = json.IndexOf("\"id\"", cursor, StringComparison.Ordinal);
                if (id < 0)
                    break;
                int next = json.IndexOf("\"id\"", id + 4, StringComparison.Ordinal);
                string slice = next < 0 ? json.Substring(id) : json.Substring(id, next - id);
                float line = ReadNumber(slice, "\"preferredW\"", 0);
                float box = ReadNumber(slice, "\"boxW\"", 0);
                if (box > 1f && line > box + 2f)
                {
                    mapId = ReadString(slice, "id", string.Empty);
                    text = ReadString(slice, "text", string.Empty);
                    preferred = line;
                    boxWidth = box;
                    return true;
                }

                if (next < 0)
                    break;
                cursor = next;
            }

            return false;
        }

        /// <summary>
        /// True when a dumped name plus its status row does not fit in the card.
        /// The thumbnail and the pads stay, so the two text rows share what is left.
        /// A dump with no status height is left alone.
        /// </summary>
        public static bool TryReadNameStack(
            string json,
            out string mapId,
            out string text,
            out float nameHeight,
            out float statusHeight,
            out float cardHeight)
        {
            mapId = string.Empty;
            text = string.Empty;
            nameHeight = 0f;
            statusHeight = 0f;
            cardHeight = 0f;
            if (string.IsNullOrEmpty(json) || !JsonStringsAreClean(json))
                return false;
            int names = json.IndexOf("\"names\"", StringComparison.Ordinal);
            if (names < 0)
                return false;
            int cursor = names;
            while (cursor < json.Length)
            {
                int id = json.IndexOf("\"id\"", cursor, StringComparison.Ordinal);
                if (id < 0)
                    break;
                int next = json.IndexOf("\"id\"", id + 4, StringComparison.Ordinal);
                string slice = next < 0 ? json.Substring(id) : json.Substring(id, next - id);
                float nameH = ReadNumber(slice, "\"preferredH\"", 0);
                float statusH = ReadNumber(slice, "\"statusH\"", 0);
                float cardH = ReadNumber(slice, "\"cardH\"", 0);
                float room = cardH - NameStackChrome(cardH);
                if (statusH > 0.5f && cardH > 1f && nameH + statusH > room + 1f)
                {
                    mapId = ReadString(slice, "id", string.Empty);
                    text = ReadString(slice, "text", string.Empty);
                    nameHeight = nameH;
                    statusHeight = statusH;
                    cardHeight = cardH;
                    return true;
                }

                if (next < 0)
                    break;
                cursor = next;
            }

            return false;
        }

        /// <summary>Pads, thumbnail floor and the gaps between the name and the status row.</summary>
        public static float NameStackChrome(float cardHeight)
        {
            bool compact = cardHeight < 156f;
            float art = compact ? 36f : 44f;
            return 6f + art + 4f + 4f + 6f;
        }

        private static string Json(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var text = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '\\':
                        text.Append("\\\\");
                        break;
                    case '"':
                        text.Append("\\\"");
                        break;
                    case '\n':
                        text.Append("\\n");
                        break;
                    case '\r':
                        text.Append("\\r");
                        break;
                    case '\t':
                        text.Append("\\t");
                        break;
                    default:
                        if (c < 0x20)
                        {
                            text.Append("\\u");
                            text.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                            text.Append(c);
                        break;
                }
            }

            return text.ToString();
        }

        private static bool JsonStringsAreClean(string json)
        {
            bool inString = false;
            bool escape = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (!inString)
                {
                    if (c == '"')
                        inString = true;
                    continue;
                }

                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (c == '\\')
                {
                    escape = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = false;
                    continue;
                }

                if (c < 0x20)
                    return false;
            }

            return !inString;
        }

        private static string Num(float value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static string ReadString(string json, string key, string fallback)
        {
            string token = "\"" + key + "\"";
            int at = json.IndexOf(token, StringComparison.Ordinal);
            if (at < 0)
                return fallback;
            int colon = json.IndexOf(':', at + token.Length);
            if (colon < 0)
                return fallback;
            int open = json.IndexOf('"', colon + 1);
            if (open < 0)
                return fallback;
            int close = json.IndexOf('"', open + 1);
            if (close < 0)
                return fallback;
            return json.Substring(open + 1, close - open - 1);
        }

        private static float ReadNumber(string json, string token, int from)
        {
            int at = json.IndexOf(token, from, StringComparison.Ordinal);
            if (at < 0)
                return 0f;
            int colon = json.IndexOf(':', at + token.Length);
            if (colon < 0)
                return 0f;
            int start = colon + 1;
            while (start < json.Length && (json[start] == ' ' || json[start] == '\n'))
                start++;
            int end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.' || json[end] == '-'))
                end++;
            float value;
            if (!float.TryParse(json.Substring(start, end - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return 0f;
            return value;
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
            int stampFrom = violations.Count;
            string savedLocale = ShotLocale;
            bool savedReserve = ShotReserve;
            if (!string.IsNullOrEmpty(locale))
                ShotLocale = locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? "fa" : locale;
            ShotReserve = detail.W > 1f;
            bool fa = locale != null && locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            int count = MapIds.Length;
            bool wideFrame = plateHeight > 2f && plateWidth / plateHeight >= 2.4f;
            GrowCardsForNames(cards, hexes, count, detail, hasCue ? cue : default, hasHint ? hint : default, plateHeight, fa, wideFrame);
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

                if (!GlowInside(hexes[i], plateWidth, plateHeight, out float glowX, out float glowY))
                {
                    violations.Add(
                        "Theatre district glow is clipped by the map locale=" + locale +
                        " district=" + mapId +
                        " local=" + glowX.ToString("0.0", CultureInfo.InvariantCulture) +
                        "," + glowY.ToString("0.0", CultureInfo.InvariantCulture));
                }

                NoteOutside(violations, pins[i], plateWidth, plateHeight, "pin", mapId, locale, true);
                if (cards[i].W < 1f || cards[i].H < 1f)
                {
                    violations.Add(
                        "Theatre portrait seating was refused locale=" + locale +
                        " card=ZoneCard_" + mapId + ".");
                }
                else
                {
                    NoteOutside(violations, cards[i], plateWidth, plateHeight, "zone card", mapId, locale, true);
                    NoteCardEdge(violations, cards[i], plateWidth, plateHeight, mapId, locale);
                    if (cards[i].W < PortraitMinWidth || cards[i].W > PortraitMaxWidth + PortraitMaxSlop
                        || cards[i].H < PortraitMinHeight || cards[i].H > PortraitMaxHeight + PortraitMaxSlop)
                    {
                        violations.Add(
                            "Theatre zone card for " + mapId + " is " + cards[i].W.ToString("0", CultureInfo.InvariantCulture) +
                            "x" + cards[i].H.ToString("0", CultureInfo.InvariantCulture) +
                            " locale=" + locale + ". Expected a portrait tile about 112-160 by 140-212.");
                    }

                    if (!SeatHolds(cards[i], hexes[i], hexes))
                    {
                        violations.Add(
                            "Theatre zone card straddles a district edge locale=" + locale +
                            " card=ZoneCard_" + mapId + ".");
                    }

                    float band = NameBandHeight(cards[i], cards, i, detail, hasCue ? cue : default, hasHint ? hint : default, plateHeight);
                    NoteName(violations, cards[i], hexes[i], hexes, mapId, fa, wideFrame, band, locale);
                    NoteLeader(violations, pins[i], cards[i], detail, mapId, locale);
                    if (hasCue)
                        NoteLeaderCue(violations, pins[i], cards[i], cue, mapId, locale);
                    NoteCardHome(violations, cards[i], hexes[i], mapId, locale);
                    NoteLeaderHexes(violations, pins[i], cards[i], hexes, i, locale);
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

                float aspectWidth = plateWidth;
                float aspectHeight = plateHeight;
                if (detail.W > 1f)
                {
                    bool panelOnLeft = detail.Center.X < plateWidth * 0.5f;
                    Box view = OpenViewport(plateWidth, plateHeight, panelOnLeft, true);
                    if (view.W > 2f && view.H > 2f)
                    {
                        aspectWidth = view.W;
                        aspectHeight = view.H;
                    }
                }

                float expected = HexAspect(mapId, aspectWidth, aspectHeight);
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

            if (hasHint)
            {
                for (int i = 0; i < count; i++)
                {
                    if (cards[i].W > 1f && Overlaps(hint, cards[i], 2f))
                    {
                        violations.Add(
                            "Theatre pinch cue overlaps a zone card locale=" + locale +
                            " card=ZoneCard_" + MapIds[i] + ".");
                    }
                }
            }

            if (detail.W > 1f)
            {
                Box glowZone = new Box(
                    detail.X - HaloReach,
                    detail.Y - HaloReach,
                    detail.W + HaloReach * 2f,
                    detail.H + HaloReach * 2f);
                for (int i = 0; i < count; i++)
                {
                    if (cards[i].W > 1f && Overlaps(cards[i], detail, 0f))
                    {
                        violations.Add(
                            "Theatre zone card intersects the detail panel locale=" + locale +
                            " card=ZoneCard_" + MapIds[i] + ".");
                    }

                    if (pins[i].W > 1f && Overlaps(pins[i], detail, 0f))
                    {
                        violations.Add(
                            "Theatre pin intersects the detail panel locale=" + locale +
                            " pin=ZonePin_" + MapIds[i] + ".");
                    }

                    if (hexes[i] == null)
                        continue;
                    if (PolygonHitsRect(hexes[i], detail))
                    {
                        violations.Add(
                            "Theatre district hex intersects the detail panel locale=" + locale +
                            " district=" + MapIds[i] + ".");
                    }

                    if (PolygonHitsRect(hexes[i], glowZone))
                    {
                        violations.Add(
                            "Theatre district glow intersects the detail panel locale=" + locale +
                            " district=" + MapIds[i] + ".");
                    }
                }
            }

            NoteLeaderCrossings(violations, pins, cards, locale);
            NoteForeignLeaders(violations, pins, cards, locale);
            NoteStripMedals(violations, plateWidth, fa, locale);
            for (int i = stampFrom; i < violations.Count; i++)
                violations[i] = Stamp(violations[i]);
            ShotLocale = savedLocale;
            ShotReserve = savedReserve;
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

        private static void NoteName(
            IList<string> violations,
            Box card,
            Vec2[] own,
            Vec2[][] districts,
            string mapId,
            bool fa,
            bool wide,
            float bandHeight,
            string locale)
        {
            string title = ZoneTitle(mapId, fa);
            float width = Math.Max(1f, card.W - 12f);
            if (!TryFitZoneName(title, card, bandHeight, wide, own, districts, out _, out float nameHeight, out _))
            {
                if (nameHeight < 0.5f)
                    nameHeight = TextWidth(title, NameFontFloor);
                violations.Add(
                    "Theatre zone name draws past its box for " + mapId + " locale=" + locale +
                    " preferred=" + nameHeight.ToString("0.0", CultureInfo.InvariantCulture) +
                    " width=" + width.ToString("0.0", CultureInfo.InvariantCulture) +
                    " text=" + title + ".");
            }
        }

        /// <summary>
        /// Two-line district title. The name rect is the wrapped preferred height. The portrait
        /// may grow up to its band, and the font steps down to <see cref="NameFontFloor"/>.
        /// </summary>
        public static bool TryFitZoneName(
            string text,
            float cardWidth,
            float cardHeight,
            float bandHeight,
            bool wide,
            out float fontSize,
            out float nameHeight,
            out float grownHeight)
        {
            return TryFitZoneName(text, new Box(0f, 0f, cardWidth, cardHeight), bandHeight, wide, null, null, out fontSize, out nameHeight, out grownHeight);
        }

        private static bool TryFitZoneName(
            string text,
            Box card,
            float bandHeight,
            bool wide,
            Vec2[] own,
            Vec2[][] districts,
            out float fontSize,
            out float nameHeight,
            out float grownHeight)
        {
            fontSize = NameFontFloor;
            nameHeight = 0f;
            grownHeight = card.H;
            if (string.IsNullOrEmpty(text))
                return true;
            float width = Math.Max(1f, card.W - 12f);
            float factor = NameLineFactor(text);
            float band = Math.Max(card.H, Math.Min(PortraitMaxHeight, bandHeight));
            float fontFloor = card.W <= PortraitMinWidth + 8f ? PortraitCompactFontFloor : NameFontFloor;
            for (float size = NameFontCeiling; size >= fontFloor - 0.01f; size -= 1f)
            {
                if (!MeasureName(text, width, size, out int lines, out float longest, out float word))
                {
                    nameHeight = word > 0.5f ? word : TextWidth(text, size);
                    continue;
                }

                float needed = lines * size * factor + 2f;
                nameHeight = needed;
                float grown = card.H;
                if (MaxNameBoxHeight(grown, wide) + 1f < needed)
                {
                    grown = band;
                    if (MaxNameBoxHeight(grown, wide) + 1f < needed)
                    {
                        fontSize = size;
                        grownHeight = grown;
                        continue;
                    }

                    while (grown - 1f >= card.H && MaxNameBoxHeight(grown - 1f, wide) + 1f >= needed)
                        grown -= 1f;
                }

                var grownBox = new Box(card.X, card.Y, card.W, grown);
                if (own != null && districts != null && !SeatHolds(grownBox, own, districts))
                {
                    fontSize = size;
                    grownHeight = grown;
                    continue;
                }
                fontSize = size;
                grownHeight = grown;
                return longest <= width + 2f;
            }

            return false;
        }

        /// <summary>
        /// Moves a portrait off the pinch-zoom cue. It keeps its size when a higher seat
        /// is legal, and otherwise shortens only enough to clear the cue.
        /// </summary>
        private static void LiftCardsOffHint(Box[] cards, Vec2[][] hexes, Box detail, float plateWidth, float plateHeight)
        {
            if (cards == null)
                return;
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            for (int pass = 0; pass < 8; pass++)
            {
                bool moved = false;
                for (int i = 0; i < cards.Length; i++)
                {
                    if (cards[i].W < 1f || !Overlaps(cards[i], hint, 2f))
                        continue;
                    float lift = cards[i].YMax - (hint.YMin - 3f);
                    if (lift < 0.5f)
                        continue;
                    float raisedY = cards[i].Y - lift;
                    Box candidate = default;
                    bool found = false;
                    if (raisedY >= Edge)
                    {
                        var raised = new Box(cards[i].X, raisedY, cards[i].W, cards[i].H);
                        if (SeatAccepts(raised, i, cards, hexes, detail))
                        {
                            candidate = raised;
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        float limit = hint.YMin - 3f;
                        for (float y = cards[i].Y; y >= Edge; y -= 8f)
                        {
                            float nextH = Math.Min(cards[i].H, limit - y);
                            if (nextH + 0.5f < PortraitMinHeight)
                                continue;
                            var trial = new Box(cards[i].X, y, cards[i].W, nextH);
                            if (!SeatAccepts(trial, i, cards, hexes, detail))
                                continue;
                            candidate = trial;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        continue;
                    cards[i] = candidate;
                    moved = true;
                }

                if (!moved)
                    break;
            }
        }

        private static bool SeatAccepts(Box card, int index, Box[] cards, Vec2[][] hexes, Box detail)
        {
            if (hexes != null && index < hexes.Length && !SeatHolds(card, hexes[index], hexes))
                return false;
            if (detail.W > 1f && Overlaps(card, detail, 0f))
                return false;
            if (cards == null)
                return true;
            for (int j = 0; j < cards.Length; j++)
            {
                if (j == index || cards[j].W < 1f)
                    continue;
                if (Separation(card, cards[j]) < CardGap)
                    return false;
            }

            return true;
        }

        /// <summary>How far a top-left card may extend downward before it meets another seat, the cue, or the plate.</summary>
        public static float NameBandHeight(Box card, Box[] cards, int index, Box detail, Box cue, Box hint, float plateHeight)
        {
            float bottom = plateHeight - Edge;
            if (hint.W > 1f && RangesOverlap(card.XMin, card.XMax, hint.XMin, hint.XMax))
                bottom = Math.Min(bottom, hint.YMin - 1f);
            if (detail.W > 1f && RangesOverlap(card.XMin, card.XMax, detail.XMin, detail.XMax))
                bottom = Math.Min(bottom, detail.YMin - 1f);
            if (cue.W > 1f && RangesOverlap(card.XMin, card.XMax, cue.XMin, cue.XMax) && cue.YMin >= card.YMin - 1f)
                bottom = Math.Min(bottom, cue.YMin - 12f);
            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (i == index || cards[i].W < 1f)
                        continue;
                    if (!RangesOverlap(card.XMin - 12f, card.XMax + 12f, cards[i].XMin, cards[i].XMax))
                        continue;
                    if (cards[i].YMin + 1f < card.YMin)
                        continue;
                    bottom = Math.Min(bottom, cards[i].YMin - 12f);
                }
            }

            float room = bottom - card.Y;
            return Math.Max(card.H, Math.Min(PortraitMaxHeight, room));
        }

        /// <summary>Name-box height a portrait can give once the thumbnail keeps 44px and the ring keeps its size.</summary>
        public static float MaxNameBoxHeight(float cardHeight, bool wide)
        {
            bool compact = cardHeight < 156f;
            float ring = (cardHeight >= 190f ? 56f : compact ? 36f : 44f) + (wide ? 4f : 0f);
            float art = compact ? 36f : 44f;
            return cardHeight - 6f - art - 4f - ring - 4f - 6f;
        }

        private static void GrowCardsForNames(
            Box[] cards,
            Vec2[][] hexes,
            int count,
            Box detail,
            Box cue,
            Box hint,
            float plateHeight,
            bool fa,
            bool wide)
        {
            if (cards == null)
                return;
            int n = Math.Min(count, cards.Length);
            var bands = new float[n];
            for (int i = 0; i < n; i++)
            {
                if (cards[i].W < 1f)
                    continue;
                bands[i] = NameBandHeight(cards[i], cards, i, detail, cue, hint, plateHeight);
            }

            for (int i = 0; i < n; i++)
            {
                if (cards[i].W < 1f)
                    continue;
                string title = ZoneTitle(MapIds[i], fa);
                Vec2[] own = hexes != null && i < hexes.Length ? hexes[i] : null;
                if (!TryFitZoneName(title, cards[i], bands[i], wide, own, hexes, out float font, out _, out float grown))
                    continue;
                float target = grown;
                if (target <= cards[i].H + 0.5f)
                    target = ArabicLineBoxHeight(title, cards[i], bands[i], wide, font);
                if (target <= cards[i].H + 0.5f)
                    continue;
                var grownCard = new Box(cards[i].X, cards[i].Y, cards[i].W, target);
                bool crowded = false;
                for (int j = 0; j < n; j++)
                {
                    if (j == i || cards[j].W < 1f)
                        continue;
                    if (Separation(grownCard, cards[j]) < CardGap)
                    {
                        crowded = true;
                        break;
                    }
                }
                if (crowded || (own != null && hexes != null && !SeatHolds(grownCard, own, hexes)))
                    continue;
                cards[i] = grownCard;
            }
        }

        /// <summary>
        /// Extra height a short portrait needs so a wrapped Farsi title and the status row stack.
        /// Noto Arabic's line box is about 1.65em. Returns the current height when that stack
        /// already fits, or when the band cannot hold it.
        /// </summary>
        private static float ArabicLineBoxHeight(string text, Box card, float bandHeight, bool wide, float font)
        {
            if (string.IsNullOrEmpty(text) || font < 1f || card.H >= 190f)
                return card.H;
            bool arabic = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] > 127)
                {
                    arabic = true;
                    break;
                }
            }

            if (!arabic)
                return card.H;
            float width = Math.Max(1f, card.W - 12f);
            if (!MeasureName(text, width, font, out int lines, out _, out _) || lines < 2)
                return card.H;
            float needed = lines * font * 1.65f + 2f;
            if (MaxNameBoxHeight(card.H, wide) + 1f >= needed)
                return card.H;
            float band = Math.Max(card.H, Math.Min(PortraitMaxHeight, bandHeight));
            if (MaxNameBoxHeight(band, wide) + 1f < needed)
                return card.H;
            while (band - 1f >= card.H && MaxNameBoxHeight(band - 1f, wide) + 1f >= needed)
                band -= 1f;
            return band;
        }

        private static float NameLineFactor(string text)
        {
            return 1.25f;
        }

        private static float MeasuredTitleWidth(string text, float size, float boxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;
            if (MeasureName(text, boxWidth, size, out _, out float longest, out float word))
                return longest > 0.5f ? longest : TextWidth(text, size);
            return word > 0.5f ? word : TextWidth(text, size);
        }

        private static bool MeasureName(string text, float width, float size, out int lines, out float longest, out float wordWidth)
        {
            lines = 1;
            longest = 0f;
            wordWidth = 0f;
            string[] words = text.Split(' ');
            float line = 0f;
            for (int i = 0; i < words.Length; i++)
            {
                float word = TextWidth(words[i], size);
                if (word > wordWidth)
                    wordWidth = word;
                if (word > width + 2f)
                    return false;
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

            return lines <= 2;
        }

        private static bool RangesOverlap(float a0, float a1, float b0, float b1)
        {
            return a0 < b1 && b0 < a1;
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
                float factor = value <= 127 ? 0.62f : 0.95f;
                width += size * factor;
            }

            return width;
        }

        private static bool GlowInside(Vec2[] hex, float plateWidth, float plateHeight, out float localX, out float localY)
        {
            localX = 0f;
            localY = 0f;
            if (hex == null || hex.Length < 3)
                return false;
            bool inside = true;
            for (int i = 0; i < hex.Length; i++)
            {
                float x = hex[i].X;
                float y = hex[i].Y;
                float glowLeft = x - HaloReach;
                float glowRight = x + HaloReach;
                float glowTop = y - HaloReach;
                float glowBottom = y + HaloReach;
                bool outside = glowLeft < 1f || glowRight > plateWidth - 1f || glowTop < 1f || glowBottom > plateHeight - 1f;
                if (!outside)
                    continue;
                localX = glowLeft < 1f ? glowLeft : glowRight > plateWidth - 1f ? glowRight : x;
                float plateY = glowTop < 1f ? glowTop : glowBottom > plateHeight - 1f ? glowBottom : y;
                localY = -plateY;
                inside = false;
            }

            return inside;
        }

        private static void NoteCardHome(IList<string> violations, Box card, Vec2[] hex, string mapId, string locale)
        {
            if (card.W < 1f || CentreOwns(card, hex))
                return;
            float reach = CentreReach(card, hex);
            violations.Add(
                "Theatre zone card sits away from its district locale=" + locale +
                " card=ZoneCard_" + mapId +
                " distance=" + reach.ToString("0", CultureInfo.InvariantCulture) + ".");
        }

        private static void NoteLeaderCue(IList<string> violations, Box pin, Box card, Box cue, string mapId, string locale)
        {
            if (pin.W < 1f || card.W < 1f || cue.W < 1f)
                return;
            if (!SegmentHitsRect(pin.Center, NearestOn(card, pin.Center), cue))
                return;
            violations.Add(
                "Theatre leader intersects the zoom hint locale=" + locale +
                " map=" + mapId + ".");
        }

        private static void NoteLeaderHexes(IList<string> violations, Box pin, Box card, Vec2[][] hexes, int index, string locale)
        {
            if (pin.W < 1f || card.W < 1f || hexes == null)
                return;
            Vec2 from = pin.Center;
            Vec2 to = NearestOn(card, from);
            for (int j = 0; j < hexes.Length; j++)
            {
                if (j == index || hexes[j] == null)
                    continue;
                if (!SegmentHitsPolygon(from, to, hexes[j]))
                    continue;
                violations.Add(
                    "Theatre leader crosses a district locale=" + locale +
                    " leader=" + MapIds[index] +
                    " district=" + MapIds[j] + ".");
            }
        }

        private static void NoteForeignLeaders(IList<string> violations, Box[] pins, Box[] cards, string locale)
        {
            if (pins == null || cards == null)
                return;
            int count = Math.Min(pins.Length, Math.Min(cards.Length, MapIds.Length));
            for (int leader = 0; leader < count; leader++)
            {
                if (pins[leader].W < 1f || cards[leader].W < 1f)
                    continue;
                Vec2 from = pins[leader].Center;
                Vec2 to = NearestOn(cards[leader], from);
                for (int card = 0; card < count; card++)
                {
                    if (card == leader || cards[card].W < 1f)
                        continue;
                    float gap = SegmentClearance(from, to, cards[card]);
                    if (gap + 0.05f >= ForeignLeaderClearance)
                        continue;
                    violations.Add(
                        "Theatre zone card is within 4px of a foreign leader locale=" + locale +
                        " card=ZoneCard_" + MapIds[card] +
                        " leader=" + MapIds[leader] +
                        " gap=" + gap.ToString("0.0", CultureInfo.InvariantCulture) + ".");
                }
            }
        }

        private static void NoteLeaderCrossings(IList<string> violations, Box[] pins, Box[] cards, string locale)
        {
            if (pins == null || cards == null)
                return;
            int count = Math.Min(pins.Length, cards.Length);
            for (int i = 0; i < count; i++)
            {
                if (pins[i].W < 1f || cards[i].W < 1f)
                    continue;
                Vec2 from = pins[i].Center;
                Vec2 to = NearestOn(cards[i], from);
                for (int j = i + 1; j < count; j++)
                {
                    if (pins[j].W < 1f || cards[j].W < 1f)
                        continue;
                    Vec2 otherFrom = pins[j].Center;
                    Vec2 otherTo = NearestOn(cards[j], otherFrom);
                    if (!SegmentsCross(from, to, otherFrom, otherTo))
                        continue;
                    violations.Add(
                        "Theatre leaders cross locale=" + locale +
                        " " + MapIds[i] + " " + MapIds[j] + ".");
                }
            }
        }

        private static void NoteStripMedals(IList<string> violations, float plateWidth, bool fa, string locale)
        {
            string[] tiers = fa
                ? new[] { "تازه‌کار", "عادی", "کهنه‌کار", "فرمانده" }
                : new[] { "RECRUIT", "REGULAR", "VETERAN", "COMMANDER" };
            for (int i = 0; i < tiers.Length; i++)
            {
                StripMedal(plateWidth, fa, i, out _, out Box label, out Box count);
                float gap = HorizontalGap(label, count);
                if (gap < MedalGapMinimum)
                {
                    violations.Add(
                        "Theatre medal label overlaps its count locale=" + locale +
                        " tier=" + tiers[i] +
                        " gap=" + gap.ToString("0.0", CultureInfo.InvariantCulture) + ".");
                }

                float font = StripCaptionSize(label.W, tiers[i]);
                float width = TextWidth(tiers[i], font);
                if (width > label.W + 1f)
                {
                    violations.Add(
                        "Theatre medal label draws into its count locale=" + locale +
                        " tier=" + tiers[i] +
                        " preferred=" + width.ToString("0.0", CultureInfo.InvariantCulture) +
                        " width=" + label.W.ToString("0.0", CultureInfo.InvariantCulture) + ".");
                }
            }
        }

        /// <summary>Strip medal caption and count, in preview space. The strip is 16px narrower than the map.</summary>
        public static void StripMedal(float plateWidth, bool fa, int index, out Box slot, out Box label, out Box count)
        {
            float stripWidth = Math.Max(200f, plateWidth - 16f);
            const float captionWidth = 168f;
            const float summaryCountWidth = 96f;
            const float pairGap = 6f;
            float reserved = 8f + captionWidth + pairGap + summaryCountWidth + 16f;
            float slotWidth = Clamp((stripWidth - reserved) / 4f, 150f, 220f);
            float inner = Math.Max(96f, slotWidth - 8f);
            float run = slotWidth * 4f;
            float origin = fa ? stripWidth - reserved - run : reserved;
            int visual = fa ? 3 - index : index;
            float slotX = origin + visual * slotWidth;
            const float icon = 22f;
            const float gap = 6f;
            const float countBox = 36f;
            const float edge = 4f;
            float labelWidth = inner - (edge + icon + gap + MedalGap + countBox + edge);
            if (labelWidth < 48f)
                labelWidth = 48f;
            float labelX = fa ? edge + countBox + MedalGap : edge + icon + gap;
            float countX = fa ? edge : inner - edge - countBox;
            // The cell is 8px narrower than its pitch. English keeps that gutter on the right;
            // Persian keeps it on the left so the cell mirrors across the strip.
            float boxX = fa ? slotX + (slotWidth - inner) : slotX;
            slot = new Box(boxX, 4f, inner, 44f);
            label = new Box(boxX + labelX, 8f, Math.Max(1f, labelWidth), 28f);
            count = new Box(boxX + countX, 10f, countBox, 24f);
        }

        /// <summary>Caption size that fits the strip slot. Wide plates stay at 12; a narrow 4:3 slot steps down to 9.</summary>
        public static float StripCaptionSize(float labelWidth, string text)
        {
            if (string.IsNullOrEmpty(text) || labelWidth < 1f)
                return StripCaptionFloor;
            for (float size = StripCaptionCeiling; size >= StripCaptionFloor - 0.01f; size -= 1f)
            {
                if (TextWidth(text, size) <= labelWidth + 1f)
                    return size;
            }

            return StripCaptionFloor;
        }

        private static float HorizontalGap(Box left, Box right)
        {
            bool rowsOverlap = left.YMin < right.YMax && right.YMin < left.YMax;
            if (!rowsOverlap)
                return 999f;
            if (left.XMax <= right.XMin)
                return right.XMin - left.XMax;
            if (right.XMax <= left.XMin)
                return left.XMin - right.XMax;
            return -Math.Min(left.XMax, right.XMax) + Math.Max(left.XMin, right.XMin);
        }

        private static bool CentreOwns(Box card, Vec2[] hex)
        {
            if (hex == null || hex.Length < 3 || card.W < 1f)
                return false;
            if (PointInPolygon(card.Center, hex))
                return true;
            return DistanceToEdge(card.Center, hex) <= CardReach;
        }

        private static float CentreReach(Box card, Vec2[] hex)
        {
            if (hex == null || hex.Length < 3)
                return 999f;
            if (PointInPolygon(card.Center, hex))
                return 0f;
            return DistanceToEdge(card.Center, hex);
        }

        private static bool LeaderHitsOtherHex(Vec2 from, Vec2 to, Vec2[][] hexes, int index)
        {
            if (hexes == null)
                return false;
            for (int j = 0; j < hexes.Length; j++)
            {
                if (j == index)
                    continue;
                if (SegmentHitsPolygon(from, to, hexes[j]))
                    return true;
            }

            return false;
        }

        private static bool SegmentHitsPolygon(Vec2 a, Vec2 b, Vec2[] polygon)
        {
            if (polygon == null || polygon.Length < 3)
                return false;
            if (PointInPolygon(a, polygon) || PointInPolygon(b, polygon))
                return true;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (SegmentsCross(a, b, polygon[i], polygon[(i + 1) % polygon.Length]))
                    return true;
            }

            return false;
        }

        private static bool LeaderHitsRect(Box[] pins, Box[] cards, Box rect)
        {
            if (pins == null || cards == null || rect.W < 1f)
                return false;
            int count = Math.Min(pins.Length, cards.Length);
            for (int i = 0; i < count; i++)
            {
                if (pins[i].W < 1f || cards[i].W < 1f)
                    continue;
                if (SegmentHitsRect(pins[i].Center, NearestOn(cards[i], pins[i].Center), rect))
                    return true;
            }

            return false;
        }

        private static void BoundsOf(Vec2[] hex, out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = maxX = hex[0].X;
            minY = maxY = hex[0].Y;
            for (int i = 1; i < hex.Length; i++)
            {
                minX = Math.Min(minX, hex[i].X);
                maxX = Math.Max(maxX, hex[i].X);
                minY = Math.Min(minY, hex[i].Y);
                maxY = Math.Max(maxY, hex[i].Y);
            }
        }

        private static void NoteLeader(IList<string> violations, Box pin, Box card, Box detail, string mapId, string locale)
        {
            if (pin.W < 1f || card.W < 1f)
                return;
            Vec2 from = pin.Center;
            Vec2 to = NearestOn(card, from);
            float length = Distance(from, to);
            float draw = length - PinSize * 0.5f - 2f - ForeignLeaderClearance;
            if (draw < 4f)
            {
                violations.Add("Theatre leader is missing for " + mapId + " locale=" + locale + ".");
                return;
            }

            float ux = (to.X - from.X) / length;
            float uy = (to.Y - from.Y) / length;
            var drawnEnd = new Vec2(to.X - ux * ForeignLeaderClearance, to.Y - uy * ForeignLeaderClearance);
            float meet = SegmentClearance(from, drawnEnd, card);
            if (meet + 0.05f < ForeignLeaderClearance)
            {
                violations.Add(
                    "Theatre leader meets its zone card locale=" + locale +
                    " card=ZoneCard_" + mapId +
                    " gap=" + meet.ToString("0.0", CultureInfo.InvariantCulture) + ".");
            }

            if (detail.W > 1f && SegmentHitsRect(from, to, detail))
            {
                violations.Add(
                    "Theatre leader intersects the detail panel locale=" + locale +
                    " map=" + mapId + ".");
            }
        }

        private static void ShiftSeats(Vec2[][] hexes, Box[] pins, Box[] cards, ref Box cue, float dx, float dy)
        {
            if (Math.Abs(dx) < 0.01f && Math.Abs(dy) < 0.01f)
                return;
            if (hexes != null)
            {
                for (int i = 0; i < hexes.Length; i++)
                {
                    if (hexes[i] == null)
                        continue;
                    for (int p = 0; p < hexes[i].Length; p++)
                        hexes[i][p] = new Vec2(hexes[i][p].X + dx, hexes[i][p].Y + dy);
                }
            }

            if (pins != null)
            {
                for (int i = 0; i < pins.Length; i++)
                    pins[i] = ShiftBox(pins[i], dx, dy);
            }

            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                    cards[i] = ShiftBox(cards[i], dx, dy);
            }

            cue = ShiftBox(cue, dx, dy);
        }

        private static Box ShiftBox(Box box, float dx, float dy)
        {
            if (box.W < 1f && box.H < 1f)
                return box;
            return new Box(box.X + dx, box.Y + dy, box.W, box.H);
        }

        private static bool PolygonHitsRect(Vec2[] polygon, Box rect)
        {
            if (polygon == null || polygon.Length < 3 || rect.W < 1f || rect.H < 1f)
                return false;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vec2 point = polygon[i];
                if (point.X >= rect.XMin && point.X <= rect.XMax && point.Y >= rect.YMin && point.Y <= rect.YMax)
                    return true;
                if (SegmentHitsRect(point, polygon[(i + 1) % polygon.Length], rect))
                    return true;
            }

            return PointInPolygon(rect.Center, polygon);
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
            Box view = OpenViewport(plateWidth, plateHeight, fa, reserveOn);
            bool reframed = view.X > 0.5f || view.W < plateWidth - 0.5f;
            float frameWidth = Math.Max(2f, view.W);
            float frameHeight = Math.Max(2f, view.H);
            Vec2[][] hexes = BuildAll(frameWidth, frameHeight, reframed);
            var pins = new Box[MapIds.Length];
            for (int i = 0; i < MapIds.Length; i++)
            {
                pins[i] = PinBox(i, frameWidth, frameHeight, reframed);
                layout.Seats[i].MapId = MapIds[i];
                layout.Seats[i].Hex = hexes[i];
                layout.Seats[i].Pin = pins[i];
            }

            Box detail = DetailRect(plateWidth, plateHeight, fa, reserveOn);
            Box packDetail = reframed ? default : detail;
            int selected = IndexOf(selectedMapId);
            if (selected < 0)
                selected = 0;
            Box[] placed = null;
            Box seatedCue = default;
            TryPack(frameWidth, frameHeight, hexes, pins, packDetail, selected, out placed, out seatedCue);
            if (reframed)
                ShiftSeats(hexes, pins, placed, ref seatedCue, view.X, view.Y);
            if (placed != null)
            {
                bool wide = plateHeight > 2f && plateWidth / plateHeight >= 2.4f;
                GrowCardsForNames(
                    placed,
                    hexes,
                    MapIds.Length,
                    detail,
                    default,
                    ZoomHintRect(plateWidth, plateHeight),
                    plateHeight,
                    fa,
                    wide);
                LiftCardsOffHint(placed, hexes, detail, plateWidth, plateHeight);
            }

            bool cardsLegal = placed != null;
            if (placed == null)
                placed = new Box[MapIds.Length];
            for (int i = 0; i < MapIds.Length; i++)
            {
                layout.Seats[i].Hex = hexes[i];
                layout.Seats[i].Pin = pins[i];
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
            layout.HexAspectLegal = AspectsHold(hexes, frameWidth, frameHeight)
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
                + " w=" + layout.Cue.W.ToString("0.0", CultureInfo.InvariantCulture)
                + " h=" + layout.Cue.H.ToString("0.0", CultureInfo.InvariantCulture)
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
            return BuildAll(frameWidth, frameHeight, false);
        }

        private static Vec2[][] BuildAll(float frameWidth, float frameHeight, bool plainCover)
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
                Box pin = PinBox(i, frameWidth, frameHeight, plainCover);
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
                ContainOutline(ref placed[i], frameWidth, frameHeight, plainCover);
            RaiseAreaFloor(placed, frameWidth, frameHeight, plainCover);

            for (int i = 0; i < placed.Length; i++)
                hexes[i] = FlatHex(placed[i].Cx, placed[i].Cy, placed[i].Hw, placed[i].Hh);
            return hexes;
        }

        /// <summary>
        /// A district under the 40000 capture floor is grown uniformly, keeping its aspect,
        /// until it clears the floor or the outline, the pin, or a neighbour blocks another step.
        /// </summary>
        private static void RaiseAreaFloor(Placement[] placed, float frameWidth, float frameHeight, bool plainCover)
        {
            if (placed == null)
                return;
            for (int i = 0; i < placed.Length; i++)
            {
                for (int step = 0; step < 24; step++)
                {
                    if (HexArea(placed[i].Hw, placed[i].Hh) >= 40000f)
                        break;
                    var grown = placed[i];
                    grown.Hw *= 1.02f;
                    grown.Hh *= 1.02f;
                    ContainOutline(ref grown, frameWidth, frameHeight, plainCover);
                    if (HexArea(grown.Hw, grown.Hh) + 1f < HexArea(placed[i].Hw, placed[i].Hh))
                        break;
                    if (!DistrictFits(placed, i, grown, frameWidth, frameHeight))
                        break;
                    placed[i] = grown;
                }
            }
        }

        private static bool DistrictFits(Placement[] placed, int index, Placement candidate, float frameWidth, float frameHeight)
        {
            Vec2[] hex = FlatHex(candidate.Cx, candidate.Cy, candidate.Hw, candidate.Hh);
            if (!OutlineInside(hex, frameWidth, frameHeight, out _, out _))
                return false;
            if (OnPlateInterior(hex, frameWidth, frameHeight) < 8)
                return false;
            var pin = new Vec2(candidate.PinX, candidate.PinY);
            if (!PointInPolygon(pin, hex) || DistanceToEdge(pin, hex) < 8f)
                return false;
            for (int j = 0; j < placed.Length; j++)
            {
                if (j == index)
                    continue;
                Vec2[] other = FlatHex(placed[j].Cx, placed[j].Cy, placed[j].Hw, placed[j].Hh);
                if (PolygonsOverlap(hex, other, 0.75f))
                    return false;
                var otherPin = new Vec2(placed[j].PinX, placed[j].PinY);
                if (PointInPolygon(otherPin, hex) && DistanceToEdge(otherPin, hex) > 0.5f)
                    return false;
            }

            return true;
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
        private static void ContainOutline(ref Placement placement, float frameWidth, float frameHeight, bool plainCover)
        {
            float scale = 1f;
            for (int step = 0; step < 48; step++)
            {
                float halfW = placement.Hw * scale;
                float halfH = placement.Hh * scale;
                float edge = Math.Max(MapInset + OutlinePad, HaloReach + 1f);
                float minCx = edge + halfW;
                float maxCx = frameWidth - edge - halfW;
                float minCy = edge + halfH;
                float maxCy = frameHeight - edge - halfH;
                Box hint = ZoomHintRect(frameWidth, frameHeight);
                if (plainCover)
                    hint = new Box(0f, hint.Y, frameWidth, hint.H);
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
            if (TryPackKeep(plateWidth, plateHeight, hexes, pins, detail, selected, 18, 50000, false, out cards, out cue))
                return true;
            if (TryPackKeep(plateWidth, plateHeight, hexes, pins, detail, selected, 48, 250000, true, out cards, out cue))
                return true;
            return TryPackBesideSelection(plateWidth, plateHeight, hexes, pins, detail, selected, out cards, out cue);
        }

        private static bool TryPackKeep(
            float plateWidth,
            float plateHeight,
            Vec2[][] hexes,
            Box[] pins,
            Box detail,
            int selected,
            int keep,
            int budget,
            bool wide,
            out Box[] cards,
            out Box cue)
        {
            cards = null;
            cue = default;
            float[] widths = { 156f, 148f, 140f, 132f, 124f, 120f, 116f, PortraitMinWidth };
            float[] heights = { 208f, 196f, 184f, 172f, 156f, 152f, 146f, PortraitMinHeight };
            Box[] cues = CueSpots(plateWidth, plateHeight, pins, hexes, detail, selected);
            for (int size = 0; size < widths.Length; size++)
            {
                for (int c = 0; c < cues.Length; c++)
                {
                    if (cues[c].W < 1f)
                        continue;
                    if (!CueAccepted(cues[c], pins, null, hexes, detail, selected, plateWidth, plateHeight))
                        continue;
                    if (!SeatPortraits(plateWidth, plateHeight, hexes, pins, detail, cues[c], widths[size], heights[size], keep, budget, wide, out cards))
                        continue;
                    if (LeaderHitsRect(pins, cards, cues[c]))
                        continue;
                    cue = cues[c];
                    return true;
                }

                if (!SeatPortraits(plateWidth, plateHeight, hexes, pins, detail, default, widths[size], heights[size], keep, budget, wide, out cards))
                    continue;
                bool placedCue = TryPlaceCue(plateWidth, plateHeight, pins, cards, hexes, detail, selected, out cue);
                bool accepted = placedCue && CueAccepted(cue, pins, cards, hexes, detail, selected, plateWidth, plateHeight);
                bool leaderClear = accepted && !LeaderHitsRect(pins, cards, cue);
                if (leaderClear)
                    return true;
            }

            cards = null;
            cue = default;
            return false;
        }

        private static bool SeatPortraits(
            float plateWidth,
            float plateHeight,
            Vec2[][] hexes,
            Box[] pins,
            Box detail,
            Box cue,
            float cardWidth,
            float cardHeight,
            int keep,
            int budget,
            bool wide,
            out Box[] cards)
        {
            cards = null;
            var pools = new List<Box>[MapIds.Length];
            for (int i = 0; i < MapIds.Length; i++)
            {
                pools[i] = LocalCards(i, plateWidth, plateHeight, hexes, pins, detail, cue, cardWidth, cardHeight, keep, wide);
                if (pools[i].Count == 0)
                    return false;
            }

            int[] order = new int[MapIds.Length];
            for (int i = 0; i < order.Length; i++)
                order[i] = i;
            Array.Sort(order, delegate(int a, int b) { return pools[a].Count.CompareTo(pools[b].Count); });
            var placed = new Box[MapIds.Length];
            var used = new bool[MapIds.Length];
            int nodes = 0;
            if (!PlaceOwned(0, order, pools, placed, used, pins, hexes, ref nodes, budget))
                return false;
            cards = placed;
            return true;
        }

        private static List<Box> LocalCards(
            int index,
            float plateWidth,
            float plateHeight,
            Vec2[][] hexes,
            Box[] pins,
            Box detail,
            Box cue,
            float cardWidth,
            float cardHeight,
            int keep,
            bool wide)
        {
            var found = new List<Box>(48);
            float reach = wide ? 144f : 96f;
            float step = wide ? 16f : 24f;
            Vec2[] hex = hexes[index];
            if (hex == null || hex.Length < 3)
                return found;
            BoundsOf(hex, out float minX, out float minY, out float maxX, out float maxY);
            float cx = 0f;
            float cy = 0f;
            for (int i = 0; i < hex.Length; i++)
            {
                cx += hex[i].X;
                cy += hex[i].Y;
            }

            cx /= hex.Length;
            cy /= hex.Length;
            for (float dy = -reach; dy <= reach + 0.01f; dy += step)
            {
                for (float dx = -reach; dx <= reach + 0.01f; dx += step)
                    ConsiderCard(found, index, new Box(cx + dx - cardWidth * 0.5f, cy + dy - cardHeight * 0.5f, cardWidth, cardHeight), plateWidth, plateHeight, hexes, pins, detail, cue);
            }

            float[] slides = { -160f, -112f, -64f, -32f, 0f, 32f, 64f, 112f, 160f };
            float[] gaps = { 10f, 24f, 40f, 64f };
            float pinY = pins[index].Center.Y;
            float pinX = pins[index].Center.X;
            for (int g = 0; g < gaps.Length; g++)
            {
                float gap = gaps[g];
                for (int s = 0; s < slides.Length; s++)
                {
                    float slide = slides[s];
                    ConsiderCard(found, index, new Box(maxX + gap, pinY - cardHeight * 0.5f + slide, cardWidth, cardHeight), plateWidth, plateHeight, hexes, pins, detail, cue);
                    ConsiderCard(found, index, new Box(minX - gap - cardWidth, pinY - cardHeight * 0.5f + slide, cardWidth, cardHeight), plateWidth, plateHeight, hexes, pins, detail, cue);
                    ConsiderCard(found, index, new Box(pinX - cardWidth * 0.5f + slide, maxY + gap, cardWidth, cardHeight), plateWidth, plateHeight, hexes, pins, detail, cue);
                    ConsiderCard(found, index, new Box(pinX - cardWidth * 0.5f + slide, minY - gap - cardHeight, cardWidth, cardHeight), plateWidth, plateHeight, hexes, pins, detail, cue);
                    ConsiderCard(found, index, new Box(maxX + gap, Edge, cardWidth, cardHeight), plateWidth, plateHeight, hexes, pins, detail, cue);
                    ConsiderCard(found, index, new Box(minX - gap - cardWidth, Edge, cardWidth, cardHeight), plateWidth, plateHeight, hexes, pins, detail, cue);
                    ConsiderCard(found, index, new Box(Edge, maxY + gap, cardWidth, cardHeight), plateWidth, plateHeight, hexes, pins, detail, cue);
                }
            }

            found.Sort(delegate(Box a, Box b)
            {
                return HomeScore(a, pins[index], hex).CompareTo(HomeScore(b, pins[index], hex));
            });
            if (keep > 0 && found.Count > keep)
                found.RemoveRange(keep, found.Count - keep);
            return found;
        }

        private static void ConsiderCard(
            List<Box> found,
            int index,
            Box card,
            float plateWidth,
            float plateHeight,
            Vec2[][] hexes,
            Box[] pins,
            Box detail,
            Box cue)
        {
            if (!OnPlate(card, plateWidth, plateHeight) || !Clears(card, detail) || !ClearsPins(card, pins))
                return;
            if (cue.W > 1f && (Separation(card, cue) < 12f || SegmentHitsRect(pins[index].Center, NearestOn(card, pins[index].Center), cue)))
                return;
            if (!SeatHolds(card, hexes[index], hexes) || !CentreOwns(card, hexes[index]))
                return;
            if (Leader(pins[index], card) < LeaderMin)
                return;
            Vec2 from = pins[index].Center;
            Vec2 to = NearestOn(card, from);
            if (detail.W > 1f && SegmentHitsRect(from, to, detail))
                return;
            if (LeaderHitsOtherHex(from, to, hexes, index))
                return;
            for (int i = 0; i < found.Count; i++)
            {
                if (Math.Abs(found[i].X - card.X) < 8f && Math.Abs(found[i].Y - card.Y) < 8f)
                    return;
            }

            found.Add(card);
        }

        private static float HomeScore(Box card, Box pin, Vec2[] hex)
        {
            float leader = Leader(pin, card);
            bool inside = RestsInside(card, hex, InsideMargin);
            float reach = CentreReach(card, hex);
            return (inside ? 0f : 400f) + leader + reach;
        }

        private static bool PlaceOwned(
            int depth,
            int[] order,
            List<Box>[] pools,
            Box[] placed,
            bool[] used,
            Box[] pins,
            Vec2[][] hexes,
            ref int nodes,
            int budget)
        {
            nodes++;
            if (nodes > budget)
                return false;
            if (depth >= order.Length)
                return true;
            int index = order[depth];
            List<Box> options = pools[index];
            for (int n = 0; n < options.Count; n++)
            {
                Box card = options[n];
                bool blocked = false;
                Vec2 from = pins[index].Center;
                Vec2 to = NearestOn(card, from);
                for (int i = 0; i < placed.Length; i++)
                {
                    if (!used[i])
                        continue;
                    if (Separation(card, placed[i]) < CardGap || SegmentClearance(from, to, placed[i]) + 0.05f < ForeignLeaderClearance)
                    {
                        blocked = true;
                        break;
                    }

                    Vec2 otherFrom = pins[i].Center;
                    Vec2 otherTo = NearestOn(placed[i], otherFrom);
                    if (SegmentsCross(from, to, otherFrom, otherTo) || SegmentClearance(otherFrom, otherTo, card) + 0.05f < ForeignLeaderClearance)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (blocked)
                    continue;
                placed[index] = card;
                used[index] = true;
                if (PlaceOwned(depth + 1, order, pools, placed, used, pins, hexes, ref nodes, budget))
                    return true;
                used[index] = false;
            }

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
            var found = new List<Box>(16);
            Box pin = pins[selected];
            Vec2 pinCentre = pin.Center;
            for (int dist = 0; dist <= 160 && found.Count < 12; dist += 20)
            {
                int added = 0;
                for (int slide = 0; slide <= 120 && found.Count < 12 && added < 2; slide += 20)
                {
                    float[] slides = slide == 0 ? new[] { 0f } : new[] { -slide, (float)slide };
                    for (int s = 0; s < slides.Length && found.Count < 12; s++)
                    {
                        float offset = slides[s];
                        Box[] spots =
                        {
                            new Box(pinCentre.X - CueWidth * 0.5f + offset, pin.Y - 12f - CueHeight - dist, CueWidth, CueHeight),
                            new Box(pinCentre.X - CueWidth * 0.5f + offset, pin.YMax + 12f + dist, CueWidth, CueHeight),
                            new Box(pin.X - 12f - CueWidth - dist, pinCentre.Y - CueHeight * 0.5f + offset, CueWidth, CueHeight),
                            new Box(pin.XMax + 12f + dist, pinCentre.Y - CueHeight * 0.5f + offset, CueWidth, CueHeight)
                        };
                        for (int i = 0; i < spots.Length && found.Count < 12; i++)
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
                            added++;
                        }
                    }
                }
            }

            if (found.Count == 0)
                found.Add(default);
            return found.ToArray();
        }

        /// <summary>
        /// Used only after the ordinary pack misses. City Crossroads and Industrial Basin
        /// on the 4:3 reserve viewport have no 220px cue that clears their hex, so this
        /// tries a shorter pill and a finer grid. Plates that already packed never reach it.
        /// </summary>
        private static bool TryPackBesideSelection(
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
            float[] widths = { 156f, 148f, 140f, 132f, 124f, 120f, 116f, PortraitMinWidth };
            float[] heights = { 208f, 196f, 184f, 172f, 156f, 152f, 146f, PortraitMinHeight };
            float[] cueWidths = { CueWidth, 196f, 172f, 148f, PackedCueMinWidth };
            for (int pass = 0; pass < 2; pass++)
            {
                bool wide = pass == 1;
                int keep = wide ? 48 : 18;
                int budget = wide ? 250000 : 50000;
                for (int size = 0; size < widths.Length; size++)
                {
                    if (!SeatPortraits(
                        plateWidth, plateHeight, hexes, pins, detail, default,
                        widths[size], heights[size], keep, budget, wide, out cards))
                        continue;
                    for (int c = 0; c < cueWidths.Length; c++)
                    {
                        if (!TryPlaceCueWidth(
                            plateWidth, plateHeight, pins, cards, hexes, detail, selected, cueWidths[c], out cue))
                            continue;
                        if (!CueAccepted(cue, pins, cards, hexes, detail, selected, plateWidth, plateHeight))
                            continue;
                        if (LeaderHitsRect(pins, cards, cue))
                            continue;
                        return true;
                    }
                }

                for (int c = 0; c < cueWidths.Length; c++)
                {
                    Box[] spots = CueGrid(plateWidth, plateHeight, pins, hexes, detail, selected, cueWidths[c]);
                    for (int s = 0; s < spots.Length; s++)
                    {
                        for (int size = 0; size < widths.Length; size++)
                        {
                            if (!SeatPortraits(
                                plateWidth, plateHeight, hexes, pins, detail, spots[s],
                                widths[size], heights[size], keep, budget, wide, out cards))
                                continue;
                            if (LeaderHitsRect(pins, cards, spots[s]))
                                continue;
                            cue = spots[s];
                            return true;
                        }
                    }
                }
            }

            cards = null;
            cue = default;
            return false;
        }

        private static Box[] CueGrid(
            float plateWidth,
            float plateHeight,
            Box[] pins,
            Vec2[][] hexes,
            Box detail,
            int selected,
            float cueWidth)
        {
            var found = new List<Box>(6);
            Box pin = pins[selected];
            Vec2 pinCentre = pin.Center;
            for (int dist = 0; dist <= 150 && found.Count < 6; dist += 10)
            {
                for (int slide = -140; slide <= 140 && found.Count < 6; slide += 10)
                {
                    Box[] spots =
                    {
                        new Box(pinCentre.X - cueWidth * 0.5f + slide, pin.YMax + 8f + dist, cueWidth, CueHeight),
                        new Box(pinCentre.X - cueWidth * 0.5f + slide, pin.Y - 8f - CueHeight - dist, cueWidth, CueHeight),
                        new Box(pin.XMax + 8f + dist, pinCentre.Y - CueHeight * 0.5f + slide, cueWidth, CueHeight),
                        new Box(pin.X - 8f - cueWidth - dist, pinCentre.Y - CueHeight * 0.5f + slide, cueWidth, CueHeight)
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
            return TryPlaceCueWidth(plateWidth, plateHeight, pins, cards, hexes, detail, selected, CueWidth, out cue);
        }

        private static bool TryPlaceCueWidth(
            float plateWidth,
            float plateHeight,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            float cueWidth,
            out Box cue)
        {
            Box pin = pins[selected];
            Vec2 pinCentre = pin.Center;
            cue = new Box(pin.X, pin.YMax + 8f, cueWidth, CueHeight);
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
                            new Box(pinCentre.X - cueWidth * 0.5f + offset, pin.YMax + 12f + dist, cueWidth, CueHeight),
                            new Box(pinCentre.X - cueWidth * 0.5f + offset, pin.Y - 12f - CueHeight - dist, cueWidth, CueHeight),
                            new Box(pin.XMax + 12f + dist, pinCentre.Y - CueHeight * 0.5f + offset, cueWidth, CueHeight),
                            new Box(pin.X - 12f - cueWidth - dist, pinCentre.Y - CueHeight * 0.5f + offset, cueWidth, CueHeight)
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
                            if (LeaderHitsRect(pins, cards, spot))
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
            return PinBox(index, plateWidth, plateHeight, false);
        }

        private static Box PinBox(int index, float plateWidth, float plateHeight, bool plainCover)
        {
            float coverHeight = plainCover ? plateHeight : CoverFrameHeight(plateWidth, plateHeight);
            CoverCrop(plateWidth, plateHeight, plainCover, out float u, out float v, out float cropW, out float cropH);
            Vec2 label = LabelCentres[index];
            float px = (label.X - u) / Math.Max(0.001f, cropW) * plateWidth;
            float py = (label.Y - v) / Math.Max(0.001f, cropH) * coverHeight;
            if (coverHeight > 2f && plateHeight > coverHeight + 0.5f)
                py *= plateHeight / coverHeight;
            float x = Clamp(px - PinSize * 0.5f, SafeLeft, Math.Max(SafeLeft, plateWidth - SafeRight - PinSize));
            float y = Clamp(py - PinSize * 0.5f, SafeTop, Math.Max(SafeTop, plateHeight - SafeBottom - PinSize));
            return new Box(x, y, PinSize, PinSize);
        }

        /// <summary>Cover crop of the painting. <paramref name="plain"/> skips the full-plate detail reserve crop.</summary>
        public static void CoverCrop(float frameWidth, float frameHeight, bool plain, out float u, out float v, out float width, out float height)
        {
            float coverHeight = plain ? frameHeight : CoverFrameHeight(frameWidth, frameHeight);
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

        private static float SegmentClearance(Vec2 a, Vec2 b, Box rect)
        {
            if (rect.W < 1f || rect.H < 1f)
                return 999f;
            if (SegmentHitsRect(a, b, rect))
                return 0f;
            float best = PointRectDistance(a, rect);
            best = Math.Min(best, PointRectDistance(b, rect));
            best = Math.Min(best, PointSegmentDistance(new Vec2(rect.XMin, rect.YMin), a, b));
            best = Math.Min(best, PointSegmentDistance(new Vec2(rect.XMax, rect.YMin), a, b));
            best = Math.Min(best, PointSegmentDistance(new Vec2(rect.XMax, rect.YMax), a, b));
            best = Math.Min(best, PointSegmentDistance(new Vec2(rect.XMin, rect.YMax), a, b));
            return best;
        }

        private static float PointRectDistance(Vec2 point, Box rect)
        {
            float dx = 0f;
            if (point.X < rect.XMin)
                dx = rect.XMin - point.X;
            else if (point.X > rect.XMax)
                dx = point.X - rect.XMax;
            float dy = 0f;
            if (point.Y < rect.YMin)
                dy = rect.YMin - point.Y;
            else if (point.Y > rect.YMax)
                dy = point.Y - rect.YMax;
            if (dx == 0f && dy == 0f)
                return 0f;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static float PointSegmentDistance(Vec2 point, Vec2 a, Vec2 b)
        {
            float ex = b.X - a.X;
            float ey = b.Y - a.Y;
            float lengthSq = ex * ex + ey * ey;
            float t = lengthSq < 0.0001f
                ? 0f
                : Clamp(((point.X - a.X) * ex + (point.Y - a.Y) * ey) / lengthSq, 0f, 1f);
            float dx = point.X - (a.X + ex * t);
            float dy = point.Y - (a.Y + ey * t);
            return (float)Math.Sqrt(dx * dx + dy * dy);
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
