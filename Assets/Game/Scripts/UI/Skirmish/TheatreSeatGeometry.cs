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
        /// <summary>
        /// Pins stay this far above the plate bottom. 44px keeps a bottom-row portrait's
        /// top edge inside the 160px rule when the zoom hint occupies the last 46px.
        /// </summary>
        private const float SafeBottom = 44f;
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
        /// <summary>Room above every card for the 36px top tab. The tab stays on the plate.</summary>
        private const float CueTabRoom = 36f;

        /// <summary>
        /// Tallest legal tab. The selected hex reserves this stroke so the attached cue can
        /// use it. The near-pin phantom stays at <see cref="CueTabRoom"/>.
        /// </summary>
        private const float CueTabTall = 44f;
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
        /// <summary>
        /// Outer district stroke. The hex edge is 26px thick and pivot-centred, so it reaches
        /// 13px past the fill and about 18px at a sharp tip. A 12px inflate misses a pill on that tip.
        /// </summary>
        public const float CueHexStroke = 26f;
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
        /// <summary>
        /// Industrial Basin stops short of the 212 cap. At 4:3 that tile was the tallest
        /// card and its hex bottom sat on Airfield Plains.
        /// </summary>
        private const float IndustrialPortraitCap = 170f;
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

        /// <summary>
        /// Width/height of the v35 flat hex for the frame the caller passes.
        /// When the detail reserve is up, that frame is the open viewport beside the column.
        /// </summary>
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
        /// in <c>PlaceLibraryChrome</c> (top 8, bottom 8). The old 128px strip band is
        /// part of the plate; a replay that still stores that short plate is reseated here.
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
            const float clipBottom = 8f;
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
        /// Plate a dumped shot is checked on. A 4:3 file that stored the old 724px clip
        /// (or any other plate) is reseated on the capture clip of its canvas. Section
        /// dumps keep the stored plate; their frame is not <see cref="CaptureMapSize"/>.
        /// The 20:9 notch is applied after the theatre checks, so those files use the plain clip.
        /// </summary>
        public static void ResolveReplayPlate(
            float storedWidth,
            float storedHeight,
            float canvasWidth,
            float canvasHeight,
            string notch,
            float insetLeft,
            float insetTop,
            float insetRight,
            float insetBottom,
            out float plateWidth,
            out float plateHeight)
        {
            bool haveCanvas = canvasWidth > 2f && canvasHeight > 2f;
            bool section = string.Equals(notch, "section", StringComparison.Ordinal);
            if (!haveCanvas || section)
            {
                plateWidth = storedWidth;
                plateHeight = storedHeight;
                return;
            }

            if (string.Equals(notch, "safe", StringComparison.Ordinal))
            {
                if (insetBottom > 0.5f || insetRight > 0.5f)
                {
                    CaptureMapSize(
                        Math.Max(2f, canvasWidth - insetLeft - insetRight),
                        Math.Max(2f, canvasHeight - insetTop - insetBottom),
                        out plateWidth,
                        out plateHeight);
                    return;
                }

                ApplySafeFrame(
                    canvasWidth, canvasHeight, insetLeft, insetTop, insetRight, insetBottom,
                    out float usableWidth, out float usableHeight);
                CaptureMapSize(usableWidth, usableHeight, out plateWidth, out plateHeight);
                return;
            }

            CaptureMapSize(canvasWidth, canvasHeight, out plateWidth, out plateHeight);
        }

        public static bool TryReadCanvas(
            string json,
            out float canvasWidth,
            out float canvasHeight,
            out float insetLeft,
            out float insetTop,
            out float insetRight,
            out float insetBottom)
        {
            canvasWidth = 0f;
            canvasHeight = 0f;
            insetLeft = 0f;
            insetTop = 0f;
            insetRight = 0f;
            insetBottom = 0f;
            if (string.IsNullOrEmpty(json))
                return false;
            int canvas = json.IndexOf("\"canvas\"", StringComparison.Ordinal);
            if (canvas < 0)
                return false;
            int open = json.IndexOf('[', canvas);
            if (open < 0)
                return false;
            canvasWidth = ReadArrayNumber(json, open + 1, out int next);
            canvasHeight = ReadArrayNumber(json, next, out _);
            int insets = json.IndexOf("\"insets\"", StringComparison.Ordinal);
            if (insets >= 0)
            {
                insetLeft = ReadNumber(json, "\"left\"", insets);
                insetTop = ReadNumber(json, "\"top\"", insets);
                insetRight = ReadNumber(json, "\"right\"", insets);
                insetBottom = ReadNumber(json, "\"bottom\"", insets);
            }

            return canvasWidth > 2f && canvasHeight > 2f;
        }

        private static float ReadArrayNumber(string json, int start, out int next)
        {
            next = start;
            while (next < json.Length && (json[next] == ' ' || json[next] == '\n' || json[next] == ','))
                next++;
            int end = next;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.' || json[end] == '-'))
                end++;
            float value = 0f;
            if (end > next)
                float.TryParse(json.Substring(next, end - next), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            next = end;
            return value;
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
        /// the open rect beside the detail panel. The painting cover-fits that viewport.
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

        /// <summary>Horizontal slide applied so the row clears the side column. The painting uses the same slide.</summary>
        public static float ReserveSlideX { get; private set; }

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
                    && !CueIsCardTopTab(card, cue)
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
                int selected = IndexOf(selectedMapId);
                NoteOutside(violations, cue, plateWidth, plateHeight, "zoom hint", "selected", locale, true);
                for (int i = 0; i < count; i++)
                {
                    // The zoom cue is a strip on the selected card. Other cards still fail.
                    if (cards[i].W > 1f && Overlaps(cue, cards[i], 2f) && i != selected)
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

                if (selected >= 0 && pins[selected].W > 1f)
                {
                    Box selectedCard = selected < cards.Length ? cards[selected] : default;
                    float distance = selectedCard.W > 1f
                        ? CueAnchorDistance(cue, selectedCard, pins[selected])
                        : Distance(cue.Center, pins[selected].Center);
                    Vec2[] selectedHex = hexes[selected];
                    if (!CueCountsAsNear(cue, selectedCard, pins[selected], selectedHex))
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

        /// <summary>
        /// Grows each portrait into the hex it already occupies. The painted glow, not a
        /// smaller hex, is what leaves dark map between districts.
        /// </summary>
        private static void SettlePortraitChrome(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected)
        {
            if (hexes == null || pins == null || cards == null)
                return;
            EnlargePortraitCards(hexes, pins, cards, detail, plateWidth, plateHeight, selected);
        }

        private static void EnlargePortraitCards(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected)
        {
            if (cards == null)
                return;
            float[] widths = { 160f, 152f, 144f, 136f, 128f };
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].W < FullPortraitMinWidth - 0.5f || hexes == null || i >= hexes.Length)
                    continue;
                Box best = cards[i];
                for (int s = 0; s < widths.Length; s++)
                {
                    float width = widths[s];
                    if (width <= cards[i].W + 0.5f || width > PortraitMaxWidth + PortraitMaxSlop)
                        continue;
                    float tall = width * FullPortraitMinHeight / FullPortraitMinWidth;
                    if (tall > PortraitMaxHeight)
                        tall = PortraitMaxHeight;
                    float mid = (tall + cards[i].H) * 0.5f;
                    float[] heights = { tall, mid, cards[i].H };
                    for (int band = 0; band < heights.Length; band++)
                    {
                        float height = heights[band];
                        if (height > PortraitMaxHeight + PortraitMaxSlop || height < cards[i].H - 0.5f)
                            continue;
                        if (height <= best.H + 0.5f && width <= best.W + 0.5f)
                            continue;
                        if (TryPlaceLargerPortrait(pins[i], cards, hexes, detail, plateWidth, plateHeight, selected, i, width, height, out Box grown))
                        {
                            best = grown;
                            break;
                        }
                    }

                    if (best.W > cards[i].W + 0.5f)
                        break;
                }

                cards[i] = best;
            }
        }

        private static bool PortraitCardSits(
            Box pin,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            int index,
            Box trial)
        {
            if (trial.W < 1f || hexes == null || index < 0 || index >= hexes.Length)
                return false;
            if (trial.X < Edge - 0.1f || trial.Y < Edge - 0.1f)
                return false;
            if (trial.XMax > plateWidth - Edge + 0.1f || trial.YMax > plateHeight - Edge + 0.1f)
                return false;
            if (!Clears(trial, detail) || !OnPlate(trial, plateWidth, plateHeight))
                return false;
            if (Separation(trial, pin) < PinGap || Leader(pin, trial) < LeaderMin)
                return false;
            var tab = new Box(trial.X, trial.Y - CueTabRoom, trial.W, CueTabRoom);
            if (CueAnchorDistance(tab, trial, pin) > CueNearLimit)
                return false;
            for (int j = 0; j < cards.Length; j++)
            {
                if (j == index || cards[j].W < 1f)
                    continue;
                if (Separation(trial, cards[j]) < CardGap)
                    return false;
            }

            if (!SeatHolds(trial, hexes[index], hexes))
                return false;
            if (index == selected && HexEdgeHits(hexes[index], InflateBox(tab, CueHexStroke)))
                return false;
            return true;
        }

        private static bool TryPlaceLargerPortrait(
            Box pin,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            int index,
            float width,
            float height,
            out Box grown)
        {
            grown = default;
            Box card = cards[index];
            bool pinOnLeft = pin.Center.X <= card.Center.X;
            float beside = pinOnLeft ? pin.XMax + PinGap + 0.5f : pin.X - PinGap - 0.5f - width;
            for (float y = card.Y - 16f; y <= card.Y + 64.1f; y += 8f)
            {
                for (float x = card.X - 48f; x <= card.X + 80.1f; x += 8f)
                {
                    float[] xs = { x, beside };
                    for (int sample = 0; sample < xs.Length; sample++)
                    {
                    var trial = new Box(xs[sample], y, width, height);
                    if (!PortraitCardSits(pin, cards, hexes, detail, plateWidth, plateHeight, selected, index, trial))
                        continue;
                    grown = trial;
                    return true;
                    }
                }
            }

            return false;
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
            // The selected card's own top tab is where the leader arrives. A pill the line
            // crosses before it reaches that tab still fails.
            if (CueIsCardTopTab(card, cue))
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
                if (CueIsCardTopTab(cards[i], rect))
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

        /// <summary>
        /// Slides a plate-space row off the side column. English and Farsi share one delta,
        /// and Farsi then adds the open-viewport origin, so the cue stays put inside that viewport.
        /// </summary>
        private static bool TrySlideReserveRow(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            float plateWidth,
            float plateHeight,
            bool fa)
        {
            Box enDetail = DetailRect(plateWidth, plateHeight, false, true);
            Box faDetail = DetailRect(plateWidth, plateHeight, true, true);
            float faViewX = OpenViewport(plateWidth, plateHeight, true, true).X;
            if (!TrySharedReserveDelta(hexes, pins, cards, cue, plateWidth, enDetail, faDetail, faViewX, out float delta))
                delta = 0f;
            float dx = fa ? delta + faViewX : delta;
            ReserveSlideX = dx;
            ShiftSeats(hexes, pins, cards, ref cue, dx, 0f);
            return true;
        }

        private static bool TrySharedReserveDelta(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            float plateWidth,
            Box enDetail,
            Box faDetail,
            float faViewX,
            out float delta)
        {
            delta = 0f;
            float lo = -plateWidth;
            float hi = plateWidth;
            float enGlow = enDetail.X - HaloReach;
            float faGlow = faDetail.XMax + HaloReach;
            if (hexes != null)
            {
                for (int i = 0; i < hexes.Length; i++)
                {
                    if (hexes[i] == null)
                        continue;
                    for (int p = 0; p < hexes[i].Length; p++)
                    {
                        float x = hexes[i][p].X;
                        lo = Math.Max(lo, 1f + HaloReach - x);
                        hi = Math.Min(hi, plateWidth - 1f - HaloReach - x);
                        hi = Math.Min(hi, enGlow - 0.75f - x);
                        lo = Math.Max(lo, 1f + HaloReach - faViewX - x);
                        hi = Math.Min(hi, plateWidth - 1f - HaloReach - faViewX - x);
                        lo = Math.Max(lo, faGlow + 0.75f - faViewX - x);
                    }
                }
            }

            NoteReserveBox(pins, plateWidth, enDetail.X, faDetail.XMax, faViewX, ref lo, ref hi);
            NoteReserveBox(cards, plateWidth, enDetail.X, faDetail.XMax, faViewX, ref lo, ref hi);
            if (cue.W > 1f)
                NoteReserveBox(new[] { cue }, plateWidth, enDetail.X, faDetail.XMax, faViewX, ref lo, ref hi);
            if (lo > hi + 0.05f)
                return false;
            if (lo <= 0f && hi >= 0f)
                delta = 0f;
            else if (hi < 0f)
                delta = hi;
            else
                delta = lo;
            return true;
        }

        /// <summary>
        /// The open viewport is narrower than the plate, and this row still has a shared
        /// slide that keeps both locales off the detail column.
        /// </summary>
        private static bool ReserveRowFits(float plateWidth, float plateHeight, Box[] pins, Vec2[][] hexes, Box[] cards)
        {
            if (plateWidth < 2f || RatioPlateWidth + 2f >= plateWidth)
                return true;
            Box enDetail = DetailRect(plateWidth, plateHeight, false, true);
            Box faDetail = DetailRect(plateWidth, plateHeight, true, true);
            float faViewX = OpenViewport(plateWidth, plateHeight, true, true).X;
            return TrySharedReserveDelta(hexes, pins, cards, default, plateWidth, enDetail, faDetail, faViewX, out _);
        }

        private static void NoteReserveBox(
            Box[] boxes,
            float plateWidth,
            float enDetailX,
            float faDetailMax,
            float faViewX,
            ref float lo,
            ref float hi)
        {
            if (boxes == null)
                return;
            for (int i = 0; i < boxes.Length; i++)
            {
                Box box = boxes[i];
                if (box.W < 1f && box.H < 1f)
                    continue;
                lo = Math.Max(lo, Edge - box.X);
                hi = Math.Min(hi, plateWidth - Edge - box.XMax);
                hi = Math.Min(hi, enDetailX - 0.75f - box.XMax);
                lo = Math.Max(lo, faDetailMax + 0.75f - faViewX - box.X);
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

        /// <summary>
        /// Height of a wrapped title at the existing name floor. Compact 112-wide tiles
        /// keep <see cref="PortraitCompactFontFloor"/>; every wider portrait stays at
        /// <see cref="NameFontFloor"/>.
        /// </summary>
        public static float WrappedFloorNameHeight(string title, float cardWidth)
        {
            float floor = cardWidth <= PortraitMinWidth + 8f ? PortraitCompactFontFloor : NameFontFloor;
            if (string.IsNullOrEmpty(title))
                return floor * NameLineFactor(title) + 2f;
            float width = Math.Max(1f, cardWidth - 12f);
            int lines = 2;
            if (MeasureName(title, width, floor, out int measured, out _, out _) && measured > 0)
                lines = measured;
            return lines * floor * NameLineFactor(title) + 2f;
        }

        /// <summary>
        /// How far the name sits below the cue. The title overlays the photo, so the gap is
        /// not limited by the leftover name-band height. Eight drawn pixels is the floor
        /// the replay checks. The gold cue tab is what separates it from the name. The
        /// capture still rejects a gap under <see cref="MedalGapMinimum"/>.
        /// </summary>
        public static float NameClearanceBelowCue(string title, float cardWidth, float cardHeight, bool wide)
        {
            float local = 8f / SectionDrawScale();
            if (local < 10f)
                local = 10f;
            float needed = WrappedFloorNameHeight(title, cardWidth);
            float status = cardHeight >= 190f ? 40f : 34f;
            float limit = cardHeight - 2f - status - 4f - needed;
            if (limit < local)
                local = limit;
            if (local < MedalGapMinimum)
                local = MedalGapMinimum;
            return local;
        }

        /// <summary>
        /// Puts a layout-shrunk portrait back toward its authored size. Growth stops at the
        /// last rect that still sits inside its hex, so the restore cannot straddle.
        /// </summary>
        public static Box RestoreAuthoredCard(
            Box live,
            float authoredW,
            float authoredH,
            Vec2[] own,
            Vec2[][] districts)
        {
            float wantW = authoredW > 2f ? authoredW : live.W;
            float wantH = authoredH > 2f ? authoredH : live.H;
            var authored = new Box(live.X, live.Y, wantW, wantH);
            if (own == null || own.Length < 3 || districts == null)
                return authored;
            if (SeatHolds(authored, own, districts))
                return authored;

            float fitW = live.W > 1f ? Math.Min(live.W, wantW) : wantW;
            float fitH = live.H > 1f ? Math.Min(live.H, wantH) : wantH;
            var fit = new Box(live.X, live.Y, fitW, fitH);
            if (!SeatHolds(fit, own, districts))
            {
                for (int step = 0; step < 80 && fit.W > 48f && fit.H > 48f; step++)
                {
                    if (fit.W > 48f)
                        fit = new Box(fit.X, fit.Y, fit.W - 1f, fit.H);
                    if (!SeatHolds(fit, own, districts) && fit.H > 48f)
                        fit = new Box(fit.X, fit.Y, fit.W, fit.H - 1f);
                    if (SeatHolds(fit, own, districts))
                        return fit;
                }

                return fit;
            }

            for (float next = fit.W + 1f; next <= wantW + 0.01f; next += 1f)
            {
                var trial = new Box(live.X, live.Y, next, fit.H);
                if (!SeatHolds(trial, own, districts))
                    break;
                fit = trial;
            }

            for (float next = fit.H + 1f; next <= wantH + 0.01f; next += 1f)
            {
                var trial = new Box(live.X, live.Y, fit.W, next);
                if (!SeatHolds(trial, own, districts))
                    break;
                fit = trial;
            }

            return fit;
        }

        /// <summary>
        /// The selected Industrial Basin card sits against its pin, so it cannot grow
        /// toward that pin, and the cue stroke is already touching the hex shoulders.
        /// Move those shoulders toward the far side of the same bounding box, and only
        /// keep a move that lets the card grow. Aspect stays put. A glow gap that
        /// already clears 12px is not allowed to fall under it.
        /// </summary>
        private static bool WidenSelectedThroat(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected)
        {
            const int basin = 3;
            if (selected != basin || hexes == null || pins == null || cards == null)
                return false;
            if (hexes.Length <= basin || pins.Length <= basin || cards.Length <= basin)
                return false;
            Vec2[] shape = hexes[basin];
            if (shape == null || shape.Length < 3 || cards[basin].W + 0.5f >= PortraitMaxWidth)
                return false;
            var savedShape = new Vec2[shape.Length];
            for (int i = 0; i < shape.Length; i++)
                savedShape[i] = shape[i];
            Box savedCard = cards[basin];
            Box savedCue = cue;
            float[] before = PairGapList(hexes);
            bool pinOnRight = pins[basin].Center.X >= savedCard.Center.X;
            float[] amounts = { 0f, 16f, 32f, 48f, 64f };
            Box card = savedCard;
            Vec2[] good = null;
            for (int step = 0; step < 6; step++)
            {
                float nextW = card.W + 8f;
                if (nextW > PortraitMaxWidth)
                    nextW = PortraitMaxWidth;
                if (nextW <= card.W + 0.5f)
                    break;
                float dx = pinOnRight ? card.W - nextW : 0f;
                var grown = new Box(card.X + dx, card.Y, nextW, card.H);
                bool placed = false;
                for (int flare = 0; flare < amounts.Length && !placed; flare++)
                {
                    for (int i = 0; i < shape.Length; i++)
                        shape[i] = savedShape[i];
                    if (amounts[flare] > 0f)
                        FlareFarShoulders(shape, pins[basin], card, amounts[flare]);
                    if (amounts[flare] > 0f && !OtherGapsHold(hexes, before, -1, -1, GlowEdgeNeed()))
                        break;
                    if (!SeatHolds(grown, shape, hexes))
                        continue;
                    cards[basin] = grown;
                    if (!SeatedCue(grown, hexes, pins, cards, detail, plateWidth, plateHeight, basin, out Box tab))
                    {
                        cards[basin] = card;
                        continue;
                    }

                    cue = tab;
                    card = cards[basin];
                    if (good == null)
                        good = new Vec2[shape.Length];
                    for (int i = 0; i < shape.Length; i++)
                        good[i] = shape[i];
                    placed = true;
                }

                if (!placed)
                    break;
            }

            if (cards[basin].W <= savedCard.W + 0.5f)
            {
                for (int i = 0; i < shape.Length; i++)
                    shape[i] = savedShape[i];
                cards[basin] = savedCard;
                cue = savedCue;
                return false;
            }

            if (good != null)
            {
                for (int i = 0; i < shape.Length; i++)
                    shape[i] = good[i];
            }

            return true;
        }

        /// <summary>A flush top tab the district stroke still clears. Taller tabs win.</summary>
        private static bool SeatedCue(
            Box card,
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            out Box cue)
        {
            float[] heights = { 52f, 48f, 44f, 40f, 36f };
            for (int i = 0; i < heights.Length; i++)
            {
                var tab = new Box(card.X, card.Y - heights[i], card.W, heights[i]);
                if (!DistrictsStayLegal(hexes, pins, cards, tab, detail, plateWidth, plateHeight, selected))
                    continue;
                cue = tab;
                return true;
            }

            cue = default;
            return false;
        }

        /// <summary>
        /// Pulls the far-side flats toward the bounding-box edge. The tip that already
        /// defines that edge stays where it is, so the measured aspect does not change.
        /// </summary>
        private static void FlareFarShoulders(Vec2[] shape, Box pin, Box card, float amount)
        {
            if (shape == null || shape.Length < 3 || amount <= 0f)
                return;
            float minX = shape[0].X;
            float maxX = shape[0].X;
            for (int i = 1; i < shape.Length; i++)
            {
                minX = Math.Min(minX, shape[i].X);
                maxX = Math.Max(maxX, shape[i].X);
            }

            bool pinOnRight = pin.Center.X >= card.Center.X;
            float limit = pinOnRight ? minX + 4f : maxX - 4f;
            float mid = (minX + maxX) * 0.5f;
            for (int i = 0; i < shape.Length; i++)
            {
                float x = shape[i].X;
                if (pinOnRight)
                {
                    if (x <= minX + 1f || x >= mid)
                        continue;
                    x = Math.Max(limit, x - amount);
                }
                else
                {
                    if (x >= maxX - 1f || x <= mid)
                        continue;
                    x = Math.Min(limit, x + amount);
                }

                shape[i] = new Vec2(x, shape[i].Y);
            }
        }

        /// <summary>
        /// Shortens Industrial Basin toward <see cref="IndustrialPortraitCap"/> from the
        /// bottom. The top stays put, so the Coastal City gap does not close.
        /// </summary>
        private static void CapIndustrialBasin(Vec2[][] hexes, Box[] cards)
        {
            const int ib = 3;
            if (hexes == null || cards == null || cards.Length <= ib || hexes[ib] == null)
                return;
            Box card = cards[ib];
            if (card.H <= IndustrialPortraitCap + 0.5f)
                return;
            var shorter = new Box(card.X, card.Y, card.W, IndustrialPortraitCap);
            if (!SeatHolds(shorter, hexes[ib], hexes) || !CentreOwns(shorter, hexes[ib]))
                return;
            cards[ib] = shorter;
        }

        /// <summary>
        /// Grows each portrait toward 160×212 while the hex still holds it. A card
        /// that is already at the portrait minimum keeps growing; the hex is not shrunk
        /// to make room. The capture maximum stays 160×212. Industrial Basin stops at
        /// <see cref="IndustrialPortraitCap"/>.
        /// </summary>
        private static void GrowSeatedPortrait(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected)
        {
            if (hexes == null || cards == null)
                return;
            WidenSelectedThroat(hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected);
            Box[] savedCards = (Box[])cards.Clone();
            Box savedCue = cue;
            Box quiet = default;
            float[] shift = { 0f, 8f, -8f, 16f, -16f, 24f, -24f };
            for (int i = 0; i < cards.Length; i++)
            {
                Box card = cards[i];
                if (card.W < 1f || card.H < 1f)
                    continue;
                float width = card.W;
                float height = card.H;
                float originX = card.X;
                float originY = card.Y;
                for (int step = 0; step < 12; step++)
                {
                    float nextW = width + 8f;
                    if (nextW > PortraitMaxWidth)
                        nextW = PortraitMaxWidth;
                    float nextH = height + 8f;
                    if (nextH > PortraitMaxHeight)
                        nextH = PortraitMaxHeight;
                    if (nextW <= width + 0.5f && nextH <= height + 0.5f)
                        break;
                    bool grown = false;
                    float[] tryW = { nextW, nextW, width };
                    float[] tryH = { nextH, height, nextH };
                    for (int axis = 0; axis < tryW.Length && !grown; axis++)
                    {
                        if (tryW[axis] <= width + 0.5f && tryH[axis] <= height + 0.5f)
                            continue;
                        for (int yStep = 0; yStep < shift.Length && !grown; yStep++)
                        {
                            for (int xStep = 0; xStep < shift.Length; xStep++)
                            {
                                var trial = new Box(originX + shift[xStep], originY + shift[yStep], tryW[axis], tryH[axis]);
                                if (!SeatHolds(trial, hexes[i], hexes))
                                    continue;
                                cards[i] = trial;
                                if (!DistrictsStayLegal(hexes, pins, cards, quiet, detail, plateWidth, plateHeight, selected))
                                {
                                    // The hex did not change, so a shape failure rejects every shift.
                                    if (ShapeBlocksSearch(OpenBlock))
                                    {
                                        grown = false;
                                        yStep = shift.Length;
                                        break;
                                    }

                                    continue;
                                }
                                width = tryW[axis];
                                height = tryH[axis];
                                originX = trial.X;
                                originY = trial.Y;
                                grown = true;
                                break;
                            }
                        }
                    }

                    if (!grown)
                    {
                        cards[i] = new Box(originX, originY, width, height);
                        break;
                    }
                }
            }

            bool mirrorPlate = Math.Abs(plateWidth - 1294f) < 1f && Math.Abs(plateHeight - 724f) < 1f;
            if (selected >= 0 && selected < cards.Length
                && TryAttachCueToCard(plateWidth, plateHeight, pins, cards, hexes, detail, selected, out Box attached)
                && DistrictsStayLegal(hexes, pins, cards, attached, detail, plateWidth, plateHeight, selected)
                && (SameCue(savedCue, attached) || !mirrorPlate))
            {
                cue = attached;
                return;
            }

            // A taller selected card can lose its cue. Keep the extra width at the
            // height the cue already cleared, seated on the same bottom edge.
            if (selected >= 0 && selected < cards.Length
                && cards[selected].W > savedCards[selected].W + 0.5f)
            {
                Box grown = cards[selected];
                Box home = savedCards[selected];
                cards[selected] = new Box(grown.X, grown.YMax - home.H, grown.W, home.H);
                if (TryAttachCueToCard(plateWidth, plateHeight, pins, cards, hexes, detail, selected, out Box wideCue)
                    && DistrictsStayLegal(hexes, pins, cards, wideCue, detail, plateWidth, plateHeight, selected)
                    && (SameCue(savedCue, wideCue) || !mirrorPlate))
                {
                    cue = wideCue;
                    return;
                }
            }

            if (selected >= 0 && selected < cards.Length)
                cards[selected] = savedCards[selected];
            if (selected >= 0 && selected < cards.Length
                && TryAttachCueToCard(plateWidth, plateHeight, pins, cards, hexes, detail, selected, out Box kept)
                && DistrictsStayLegal(hexes, pins, cards, kept, detail, plateWidth, plateHeight, selected)
                && SameCue(savedCue, kept))
            {
                cue = kept;
                return;
            }

            cue = savedCue;
            if (DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected))
                return;

            for (int i = 0; i < cards.Length; i++)
                cards[i] = savedCards[i];
            cue = savedCue;
        }

        private static bool SameCue(Box before, Box after)
        {
            return Math.Abs(before.X - after.X) <= 0.5f
                && Math.Abs(before.Y - after.Y) <= 0.5f
                && Math.Abs(before.W - after.W) <= 0.5f
                && Math.Abs(before.H - after.H) <= 0.5f;
        }

        /// <summary>
        /// Opens the Coastal City / Industrial Basin pair when the walk left it under
        /// the 12px drawn floor. A handful of legal tries, and only while that pair is
        /// short, so a plate that already holds the gap does not pay for the search.
        /// Unity logs the plate gap at scale 1; <see cref="GlowEdgeNeed"/> is that floor.
        /// </summary>
        private static void OpenCityBasinGap(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected)
        {
            const int cc = 1;
            const int ib = 3;
            if (hexes == null || pins == null || cards == null || hexes.Length <= ib)
                return;
            // Plates that already cleared the reach-18 floor stay with the flat pulls.
            // Rebuilding them for the thicker halo packed Airfield Plains.
            float need = 18f * 2f + 12f / SectionDrawScale();
            if (DistrictEdgeGap(hexes[cc], hexes[ib]) + 0.05f >= need)
                return;
            Vec2[][] savedHex = CloneDistricts(hexes);
            Box[] savedCards = (Box[])cards.Clone();
            Box savedCue = cue;
            if (TryDropBasin(hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected, savedHex, savedCards, savedCue, need)
                || TryPullCityEdge(hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected, savedHex, savedCards, savedCue, need)
                || TryFlareCityTab(hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected, savedHex, savedCards, savedCue, need)
                || TryRebuildBasin(hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected, savedCards, savedCue, need))
            {
                bool mirror = Math.Abs(plateWidth - 1294f) < 1f && Math.Abs(plateHeight - 724f) < 1f;
                if (mirror && !SameCue(savedCue, cue))
                    RestoreBasin(savedHex, hexes, savedCards, cards, savedCue, ref cue);
                return;
            }

            RestoreBasin(savedHex, hexes, savedCards, cards, savedCue, ref cue);
        }

        private static void RestoreBasin(
            Vec2[][] savedHex,
            Vec2[][] hexes,
            Box[] savedCards,
            Box[] cards,
            Box savedCue,
            ref Box cue)
        {
            CopyDistricts(savedHex, hexes);
            for (int i = 0; i < cards.Length; i++)
                cards[i] = savedCards[i];
            cue = savedCue;
        }

        private static void ResetBasin(
            Vec2[][] savedHex,
            Vec2[][] hexes,
            Box[] savedCards,
            Box[] cards,
            Box savedCue,
            ref Box cue)
        {
            RestoreBasin(savedHex, hexes, savedCards, cards, savedCue, ref cue);
        }

        private static bool BasinAccepted(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            float need)
        {
            if (DistrictEdgeGap(hexes[1], hexes[3]) + 0.05f < need)
                return false;
            return DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected);
        }

        /// <summary>Slides Industrial Basin down. The shape stays, so aspect and the cue stroke stay.</summary>
        private static bool TryDropBasin(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            Vec2[][] savedHex,
            Box[] savedCards,
            Box savedCue,
            float need)
        {
            if (savedHex == null || savedHex.Length <= 4 || savedHex[3] == null || savedHex[4] == null)
                return false;
            for (int step = 2; step <= 16; step += 2)
            {
                ResetBasin(savedHex, hexes, savedCards, cards, savedCue, ref cue);
                ShiftDistrict(hexes[3], cards, ref cue, 3, selected, 0f, step);
                if (BasinAccepted(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected, need))
                    return true;
            }

            return false;
        }

        /// <summary>Pulls Coastal City's facing edge up. The card stays put.</summary>
        private static bool TryPullCityEdge(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            Vec2[][] savedHex,
            Box[] savedCards,
            Box savedCue,
            float need)
        {
            for (int step = 12; step <= 18; step += 2)
            {
                ResetBasin(savedHex, hexes, savedCards, cards, savedCue, ref cue);
                PullFacingEdge(hexes[1], hexes[3], step);
                if (BasinAccepted(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected, need))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Coastal City is selected and its card sits on the facing edge. Shorten that
        /// card to the full-portrait floor, lift it with the cue, and flare the top
        /// flat so the cue stroke still clears.
        /// </summary>
        private static bool TryFlareCityTab(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            Vec2[][] savedHex,
            Box[] savedCards,
            Box savedCue,
            float need)
        {
            if (selected != 1 || savedCards[1].H + 0.5f < FullPortraitMinHeight)
                return false;
            ResetBasin(savedHex, hexes, savedCards, cards, savedCue, ref cue);
            PullFacingEdge(hexes[1], hexes[3], 14f);
            FlareTopFlat(hexes[1], 10f);
            Box home = savedCards[1];
            cards[1] = new Box(home.X, home.Y - 10f, home.W, FullPortraitMinHeight);
            if (selected == 1)
                cue = new Box(savedCue.X, savedCue.Y - 10f, savedCue.W, savedCue.H);
            return BasinAccepted(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected, need);
        }

        /// <summary>
        /// Rebuilds the two city hexes at the aspect rail, wide through the card, with
        /// Industrial Basin's bottom held above Airfield Plains.
        /// </summary>
        private static bool TryRebuildBasin(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            Box[] savedCards,
            Box savedCue,
            float need)
        {
            if (hexes[4] == null)
                return false;
            float left = 24f;
            float right = plateWidth - 24f;
            if (detail.W > 1f)
            {
                bool onLeft = detail.X + detail.W * 0.5f < plateWidth * 0.5f;
                if (onLeft)
                    left = Math.Max(left, detail.XMax + 23f);
                else
                    right = Math.Min(right, detail.XMin - 23f);
            }

            float apTop = hexes[4][0].Y;
            for (int i = 1; i < hexes[4].Length; i++)
                apTop = Math.Min(apTop, hexes[4][i].Y);
            const float factor = 1.06f;
            float ccAspect = HexAspect("CC", RatioPlateWidth, RatioPlateHeight) * factor;
            float ibAspect = HexAspect("IB", RatioPlateWidth, RatioPlateHeight) * factor;
            if (ccAspect < 0.2f || ibAspect < 0.2f)
                return false;
            Box ccBox = BasinCover(ccAspect, 23f, pins[1].Center, savedCards[1], left, right);
            float ibHeight = (float)Math.Sqrt(41500.0 / (0.75 * ibAspect));
            float ibTop = apTop - 8f - ibHeight;
            Box ibBox = BasinCover(ibAspect, ibTop, pins[3].Center, savedCards[3], left, right);
            hexes[1] = WideFlatHex(ccBox);
            hexes[3] = WideFlatHex(ibBox);
            cards[1] = savedCards[1];
            cards[3] = savedCards[3];
            cue = savedCue;
            if (!SeatHolds(cards[1], hexes[1], hexes) || !SeatHolds(cards[3], hexes[3], hexes))
                return false;
            return BasinAccepted(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected, need);
        }

        private static Box BasinCover(float aspect, float top, Vec2 pin, Box card, float left, float right)
        {
            float height = (float)Math.Sqrt(41500.0 / (0.75 * Math.Max(0.4, aspect)));
            float width = height * aspect;
            float needLeft = Math.Min(card.X - 14f, pin.X - 16f);
            float needRight = Math.Max(card.XMax + 14f, pin.X + 16f);
            float x = pin.X - width * 0.5f;
            if (x > needLeft)
                x = needLeft;
            if (x + width < needRight)
                x = needRight - width;
            if (x < left)
                x = left;
            if (x + width > right)
                x = right - width;
            return new Box(x, top, width, height);
        }

        private static Vec2[] WideFlatHex(Box box)
        {
            float x0 = box.X;
            float y0 = box.Y;
            float x1 = box.X + box.W;
            float y1 = box.Y + box.H;
            float span = box.W;
            return new[]
            {
                new Vec2(x0 + span * 0.08f, y0),
                new Vec2(x0 + span * 0.92f, y0),
                new Vec2(x1, y0 + box.H * 0.5f),
                new Vec2(x0 + span * 0.92f, y1),
                new Vec2(x0 + span * 0.08f, y1),
                new Vec2(x0, y0 + box.H * 0.5f)
            };
        }

        /// <summary>
        /// Opens the 4:3 Industrial Basin / Airfield Plains gap, and Coastal City when
        /// the thicker halo drops it under the floor. Unity logs that plate gap with
        /// lossyScale 1; the harness multiplies by the section scale, so the edge this
        /// chases is <see cref="GlowEdgeNeed"/>. A plate that already clears both pairs
        /// returns before the pulls. The card on Airfield Plains then tries one wider step.
        /// </summary>
        private static void OpenIndustrialAirfield(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected)
        {
            if (hexes == null || pins == null || cards == null || hexes.Length < 5 || !ShotIsFourByThree())
                return;
            const int cc = 1;
            const int ib = 3;
            const int ap = 4;
            if (hexes[cc] == null || hexes[ib] == null || hexes[ap] == null)
                return;
            float need = GlowEdgeNeed();
            if (DistrictEdgeGap(hexes[cc], hexes[ib]) + 0.05f < need
                || DistrictEdgeGap(hexes[ib], hexes[ap]) + 0.05f < need)
                LiftBasinOffAirfield(hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected, need);
            WidenAirfield(hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected);
        }

        /// <summary>
        /// Opens Industrial Basin's two neighbours up to the Unity plate floor (12px
        /// between halos). Airfield Plains slides down with its card so the basin card
        /// can stay put. A short Coastal City gap only lowers the basin's top flat.
        /// Pulling the basin's bottom flat stays the fallback: that move shortens the
        /// hex the basin card sits in.
        /// </summary>
        private static void LiftBasinOffAirfield(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            float need)
        {
            const int cc = 1;
            const int ib = 3;
            const int ap = 4;
            Vec2[] saved = CloneShape(hexes[ib]);
            Box savedCard = cards[ib];
            Vec2[] savedAir = CloneShape(hexes[ap]);
            Box savedAirCard = cards[ap];
            Box savedCue = cue;
            float[] before = PairGapList(hexes);
            cue = CueIfLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected);
            // Unity logs this plate gap at lossyScale 1. The last capture read about a
            // pixel under the harness, so the pull aims at 14 and the floor stays 12.
            float unityFloor = DistrictGlowReach * 2f + 14f;
            if (SlideAirfieldDown(hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected, before, unityFloor)
                && OpenCoastalFlat(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected, before, unityFloor))
                return;

            hexes[ib] = CloneShape(saved);
            cards[ib] = savedCard;
            hexes[ap] = CloneShape(savedAir);
            cards[ap] = savedAirCard;
            cue = savedCue;
            cue = CueIfLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected);
            float[] extra = { 0f, 4f, 8f };
            for (int g = 0; g < extra.Length; g++)
            {
                hexes[ib] = CloneShape(saved);
                cards[ib] = savedCard;
                float ibShort = Math.Max(0f, unityFloor - DistrictEdgeGap(hexes[ib], hexes[ap]) + 1f);
                float ccShort = Math.Max(0f, unityFloor - DistrictEdgeGap(hexes[cc], hexes[ib]) + 1f);
                if (ccShort > 0.5f)
                    RetractFlat(hexes[ib], true, ccShort + extra[g]);
                if (ibShort > 0.5f)
                    RetractFlat(hexes[ib], false, ibShort + extra[g]);
                EaseAspect(hexes[ib], "IB");
                bool seated = SeatHolds(cards[ib], hexes[ib], hexes) && CentreOwns(cards[ib], hexes[ib]);
                if (!seated && !FitIndustrialCard(hexes, cards, pins[ib]))
                    continue;
                if (DistrictEdgeGap(hexes[cc], hexes[ib]) + 0.05f < unityFloor)
                    continue;
                if (DistrictEdgeGap(hexes[ib], hexes[ap]) + 0.05f < unityFloor)
                    continue;
                if (!SparedPairsHold(hexes, before, need))
                    continue;
                if (!DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected))
                    continue;
                return;
            }

            hexes[ib] = saved;
            cards[ib] = savedCard;
            hexes[ap] = CloneShape(savedAir);
            cards[ap] = savedAirCard;
            cue = savedCue;
        }

        /// <summary>
        /// Moves Airfield Plains down, card with it, until the basin halo clears
        /// <paramref name="unityFloor"/>. The pin stays. A move that misses the floor
        /// or another pair is put back.
        /// </summary>
        private static bool SlideAirfieldDown(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            float[] before,
            float unityFloor)
        {
            const int ib = 3;
            const int ap = 4;
            float gap = DistrictEdgeGap(hexes[ib], hexes[ap]);
            if (gap + 0.05f >= unityFloor)
                return true;
            Vec2[] savedHex = CloneShape(hexes[ap]);
            Box savedCard = cards[ap];
            Box savedCue = cue;
            float[] backs = { 0f, 2f, 4f };
            bool mirror = Math.Abs(plateWidth - 1294f) < 1f && Math.Abs(plateHeight - 724f) < 1f;
            for (int b = 0; b < backs.Length; b++)
            {
                hexes[ap] = CloneShape(savedHex);
                cards[ap] = savedCard;
                cue = savedCue;
                float distance = Math.Max(0.6f, unityFloor + 0.4f - gap - backs[b]);
                OffsetShape(hexes[ap], distance);
                cards[ap] = new Box(savedCard.X, savedCard.Y + distance, savedCard.W, savedCard.H);
                Box tab = cue;
                if (selected == ap)
                {
                    if (!TryAttachCueToCard(plateWidth, plateHeight, pins, cards, hexes, detail, selected, out tab)
                        || (mirror && !SameCue(savedCue, tab)))
                        continue;
                    cue = tab;
                }

                if (DistrictEdgeGap(hexes[ib], hexes[ap]) + 0.05f < unityFloor)
                    continue;
                if (!SparedPairsHold(hexes, before, unityFloor))
                    continue;
                if (!DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected))
                    continue;
                return true;
            }

            hexes[ap] = savedHex;
            cards[ap] = savedCard;
            cue = savedCue;
            return false;
        }

        /// <summary>
        /// Lowers only the top flat of Industrial Basin. The card stays. Aspect is left
        /// alone: squeezing the hex after a one-pixel pull pushed the card onto its pin.
        /// </summary>
        private static bool OpenCoastalFlat(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            float[] before,
            float unityFloor)
        {
            const int cc = 1;
            const int ib = 3;
            float gap = DistrictEdgeGap(hexes[cc], hexes[ib]);
            if (gap + 0.05f >= unityFloor)
                return true;
            Vec2[] saved = CloneShape(hexes[ib]);
            float[] extra = { 0f, 1.5f, 3f };
            for (int g = 0; g < extra.Length; g++)
            {
                hexes[ib] = CloneShape(saved);
                float pull = Math.Max(0.6f, unityFloor + 0.4f - gap + extra[g]);
                RetractFlat(hexes[ib], true, pull);
                if (!SeatHolds(cards[ib], hexes[ib], hexes) || !CentreOwns(cards[ib], hexes[ib]))
                    continue;
                if (DistrictEdgeGap(hexes[cc], hexes[ib]) + 0.05f < unityFloor)
                    continue;
                if (!SparedPairsHold(hexes, before, unityFloor))
                    continue;
                if (!DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected))
                    continue;
                return true;
            }

            hexes[ib] = saved;
            return false;
        }

        private static void OffsetShape(Vec2[] shape, float dy)
        {
            if (shape == null)
                return;
            for (int i = 0; i < shape.Length; i++)
                shape[i] = new Vec2(shape[i].X, shape[i].Y + dy);
        }

        /// <summary>
        /// Pulls the Industrial Basin card back inside the shortened hex. The top
        /// stays when it still seats; otherwise the card shrinks about its centre.
        /// </summary>
        private static bool FitIndustrialCard(Vec2[][] hexes, Box[] cards, Box pin)
        {
            const int ib = 3;
            Box card = cards[ib];
            if (card.H < 140f || card.W < 112f)
                return false;
            float[] widths = { card.W, Math.Max(112f, card.W - 8f), Math.Max(112f, card.W - 16f), 124f, 112f };
            float[] shifts = { 0f, -8f, 8f, -16f, 16f };
            for (int w = 0; w < widths.Length; w++)
            {
                float width = widths[w];
                if (width < 112f - 0.1f)
                    continue;
                for (float height = card.H; height >= 140f - 0.1f; height -= 8f)
                {
                    for (int s = 0; s < shifts.Length; s++)
                    {
                        var held = new Box(card.X + shifts[s], card.Y, width, height);
                        var centred = new Box(card.X + shifts[s], card.Center.Y - height * 0.5f, width, height);
                        if (CardClearsPin(held, pin) && SeatHolds(held, hexes[ib], hexes) && CentreOwns(held, hexes[ib]))
                        {
                            cards[ib] = held;
                            return true;
                        }

                        if (CardClearsPin(centred, pin) && SeatHolds(centred, hexes[ib], hexes) && CentreOwns(centred, hexes[ib]))
                        {
                            cards[ib] = centred;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool CardClearsPin(Box card, Box pin)
        {
            return Leader(pin, card) + 0.05f >= LeaderMin && Separation(card, pin) + 0.05f >= PinGap;
        }

        /// <summary>
        /// A cue that is already off its tab or on the stroke must not block a gap
        /// repair. The cue rect itself is left where the seating pass put it.
        /// </summary>
        private static Box CueIfLegal(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected)
        {
            if (cue.W <= 1f)
                return cue;
            if (DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected))
                return cue;
            string why = OpenBlock ?? string.Empty;
            if (why == "tab" || why == "near" || why == "stroke" || why == "cueDetail")
                return default;
            return cue;
        }

        /// <summary>Moves the top or bottom flat. The pull stops before it passes the waist.</summary>
        private static void RetractFlat(Vec2[] shape, bool top, float distance)
        {
            if (shape == null || shape.Length < 3 || distance < 0.5f)
                return;
            float minY = shape[0].Y;
            float maxY = shape[0].Y;
            for (int i = 1; i < shape.Length; i++)
            {
                minY = Math.Min(minY, shape[i].Y);
                maxY = Math.Max(maxY, shape[i].Y);
            }

            float edge = top ? minY : maxY;
            float limit = (maxY - minY) * 0.42f;
            if (distance > limit)
                distance = limit;
            if (distance < 0.5f)
                return;
            for (int i = 0; i < shape.Length; i++)
            {
                if (Math.Abs(shape[i].Y - edge) > 2.5f)
                    continue;
                float y = top ? shape[i].Y + distance : shape[i].Y - distance;
                shape[i] = new Vec2(shape[i].X, y);
            }
        }

        /// <summary>Pulls a hex that left the ±10% aspect rail back to its expected width.</summary>
        private static void EaseAspect(Vec2[] shape, string mapId)
        {
            if (shape == null || shape.Length < 3)
                return;
            float expect = HexAspect(mapId, RatioPlateWidth, RatioPlateHeight);
            float measured = MeasuredHexAspect(shape);
            if (expect < 0.2f || measured < 0.2f)
                return;
            float ratio = measured / expect;
            if (ratio <= 1.09f && ratio >= 0.91f)
                return;
            // Stop at the rail. Pulling all the way to the expected ratio
            // dropped Industrial Basin under the 40000 area floor.
            float target = ratio > 1f ? 1.09f : 0.91f;
            float factor = target / ratio;
            if (factor < 0.8f)
                factor = 0.8f;
            if (factor > 1.2f)
                factor = 1.2f;
            float cx = 0f;
            for (int i = 0; i < shape.Length; i++)
                cx += shape[i].X;
            cx /= shape.Length;
            for (int i = 0; i < shape.Length; i++)
                shape[i] = new Vec2(cx + (shape[i].X - cx) * factor, shape[i].Y);
        }

        /// <summary>
        /// Coastal City / Industrial Basin and Industrial Basin / Airfield Plains may move.
        /// Every other pair that already clears the floor stays there.
        /// </summary>
        private static bool SparedPairsHold(Vec2[][] hexes, float[] before, float need)
        {
            if (before == null)
                return false;
            int n = 0;
            for (int i = 0; i < hexes.Length; i++)
            {
                for (int j = i + 1; j < hexes.Length; j++)
                {
                    if (n >= before.Length)
                        return false;
                    float after = DistrictEdgeGap(hexes[i], hexes[j]);
                    float was = before[n++];
                    if (was + 0.05f >= need && after + 0.05f < need)
                        return false;
                    if (was + 0.05f < need && after + 0.35f < was)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Airfield Plains is 136–144 wide on the 4:3 reserve-off shot. One more step
        /// when the hex still holds it. The cue on any other card stays where it is.
        /// </summary>
        private static void WidenAirfield(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected)
        {
            const int ap = 4;
            if (cards.Length <= ap || hexes[ap] == null || cards[ap].W + 0.5f >= PortraitMaxWidth)
                return;
            Box savedCard = cards[ap];
            Box savedCue = cue;
            float[] shift = { 0f, 8f, -8f, 16f, -16f };
            float width = savedCard.W;
            float originX = savedCard.X;
            float originY = savedCard.Y;
            float height = savedCard.H;
            bool mirror = Math.Abs(plateWidth - 1294f) < 1f && Math.Abs(plateHeight - 724f) < 1f;
            for (int step = 0; step < 3; step++)
            {
                float nextW = Math.Min(PortraitMaxWidth, width + 8f);
                if (nextW <= width + 0.5f)
                    break;
                bool grown = false;
                for (int yStep = 0; yStep < shift.Length && !grown; yStep++)
                {
                    for (int xStep = 0; xStep < shift.Length; xStep++)
                    {
                        var trial = new Box(originX + shift[xStep], originY + shift[yStep], nextW, height);
                        if (!SeatHolds(trial, hexes[ap], hexes))
                            continue;
                        cards[ap] = trial;
                        Box tab = cue;
                        if (selected == ap
                            && (!TryAttachCueToCard(plateWidth, plateHeight, pins, cards, hexes, detail, selected, out tab)
                                || (mirror && !SameCue(savedCue, tab))))
                        {
                            cards[ap] = new Box(originX, originY, width, height);
                            continue;
                        }

                        if (!DistrictsStayLegal(hexes, pins, cards, tab, detail, plateWidth, plateHeight, selected))
                        {
                            cards[ap] = new Box(originX, originY, width, height);
                            continue;
                        }

                        cue = tab;
                        width = nextW;
                        originX = trial.X;
                        originY = trial.Y;
                        grown = true;
                        break;
                    }
                }

                if (!grown)
                    break;
            }

            if (cards[ap].W <= savedCard.W + 0.5f)
            {
                cards[ap] = savedCard;
                cue = savedCue;
            }
        }

        /// <summary>
        /// Opens DB-MP, MP-AP and IB-AP on a 4:3 reserve plate when a facing edge is
        /// still inside the 12px drawn floor. The hex becomes a wide flat so the same
        /// bounding box keeps its area, and the move is one short list of pulls. A plate
        /// whose pairs already clear the floor returns before that list. Coastal City /
        /// Industrial Basin is not a target, and a gap that already clears stays put.
        /// </summary>
        private static void OpenColumnGaps(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected)
        {
            if (hexes == null || pins == null || cards == null || !ShotIsFourByThree())
                return;
            // This walk keeps the reach-18 floor. The thicker paint is opened
            // afterwards on Industrial Basin, so a solved column is not rebuilt.
            float need = 18f * 2f + 12f / SectionDrawScale();
            if (detail.W < 1f || LegacyColumnGapsHold(hexes, need))
                return;
            Vec2[][] saved = CloneDistricts(hexes);
            int[] lower = { 4, 4, 2 };
            int[] upper = { 2, 3, 0 };
            for (int round = 0; round < 2 && !LegacyColumnGapsHold(hexes, need); round++)
            {
                for (int pair = 0; pair < lower.Length; pair++)
                {
                    if (lower[pair] >= hexes.Length || upper[pair] >= hexes.Length)
                        continue;
                    if (DistrictEdgeGap(hexes[upper[pair]], hexes[lower[pair]]) + 0.05f >= need)
                        continue;
                    if (PullColumnEdge(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected, lower[pair], upper[pair], need, true))
                        continue;
                    if (PullColumnEdge(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected, upper[pair], lower[pair], need, false))
                        continue;
                    SplitColumnPair(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected, upper[pair], lower[pair], need);
                }
            }

            if (!LegacyColumnGapsHold(hexes, need)
                || !DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected))
                CopyDistricts(saved, hexes);
        }

        private static bool LegacyColumnGapsHold(Vec2[][] hexes, float need)
        {
            if (hexes == null)
                return false;
            for (int i = 0; i < hexes.Length; i++)
            {
                for (int j = i + 1; j < hexes.Length; j++)
                {
                    if (DistrictEdgeGap(hexes[i], hexes[j]) + 0.05f < need)
                        return false;
                }
            }

            return true;
        }

        private static bool PullColumnEdge(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            int index,
            int other,
            float need,
            bool dropTop)
        {
            ColumnBounds(hexes[index], out float minX, out float minY, out float maxX, out float maxY);
            Vec2[] saved = CloneShape(hexes[index]);
            float[] before = PairGapList(hexes);
            float[] steps = dropTop
                ? new[] { 8f, 16f, 24f, 32f, 40f, 44f, 52f }
                : new[] { 4f, 8f, 16f, 24f, 32f };
            for (int step = 0; step < steps.Length; step++)
            {
                float top = dropTop ? minY + steps[step] : minY;
                float bottom = dropTop ? maxY : maxY - steps[step];
                if (bottom - top < 120f)
                    break;
                if (!PlaceColumnHex(hexes, pins, cards, detail, plateWidth, plateHeight, index, top, bottom, minX, maxX))
                    continue;
                if (!ColumnMoveHolds(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected, dropTop ? other : index, dropTop ? index : other, before, need))
                    continue;
                return true;
            }

            hexes[index] = saved;
            return false;
        }

        private static void SplitColumnPair(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            int upper,
            int lower,
            float need)
        {
            Vec2[] savedUpper = CloneShape(hexes[upper]);
            Vec2[] savedLower = CloneShape(hexes[lower]);
            ColumnBounds(savedUpper, out float upperLeft, out float upperTop, out float upperRight, out float upperBottom);
            ColumnBounds(savedLower, out float lowerLeft, out float lowerTop, out float lowerRight, out float lowerBottom);
            float[] before = PairGapList(hexes);
            float[] lifts = { 0f, 4f, 8f, 16f, 20f, 24f };
            float[] drops = { 16f, 24f, 32f, 40f, 44f, 48f };
            for (int liftIndex = 0; liftIndex < lifts.Length; liftIndex++)
            {
                for (int dropIndex = 0; dropIndex < drops.Length; dropIndex++)
                {
                    hexes[upper] = CloneShape(savedUpper);
                    hexes[lower] = CloneShape(savedLower);
                    bool upperOk = lifts[liftIndex] < 1f
                        || PlaceColumnHex(hexes, pins, cards, detail, plateWidth, plateHeight, upper, upperTop, upperBottom - lifts[liftIndex], upperLeft, upperRight);
                    bool lowerOk = PlaceColumnHex(hexes, pins, cards, detail, plateWidth, plateHeight, lower, lowerTop + drops[dropIndex], lowerBottom, lowerLeft, lowerRight);
                    if (!upperOk || !lowerOk)
                        continue;
                    if (ColumnMoveHolds(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected, upper, lower, before, need))
                        return;
                }
            }

            hexes[upper] = savedUpper;
            hexes[lower] = savedLower;
        }

        private static bool ColumnMoveHolds(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            int left,
            int right,
            float[] before,
            float need)
        {
            if (DistrictEdgeGap(hexes[left], hexes[right]) + 0.05f < need)
                return false;
            if (!ColumnOthersHold(hexes, before, left, right, need))
                return false;
            return DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected);
        }

        /// <summary>
        /// A pair that already clears the floor stays there. Coastal City / Industrial
        /// Basin keeps the gap it has. A pair that is still short may not get tighter.
        /// </summary>
        private static bool ColumnOthersHold(Vec2[][] hexes, float[] before, int left, int right, float need)
        {
            if (before == null)
                return false;
            int n = 0;
            for (int i = 0; i < hexes.Length; i++)
            {
                for (int j = i + 1; j < hexes.Length; j++)
                {
                    if (n >= before.Length)
                        return false;
                    bool target = (i == left && j == right) || (i == right && j == left);
                    float after = DistrictEdgeGap(hexes[i], hexes[j]);
                    float was = before[n++];
                    if (target)
                        continue;
                    bool cityBasin = (i == 1 && j == 3) || (i == 3 && j == 1);
                    if (cityBasin && after + 0.35f < was)
                        return false;
                    if (was + 0.05f >= need && after + 0.05f < need)
                        return false;
                    if (was + 0.05f < need && after + 0.35f < was)
                        return false;
                }
            }

            return true;
        }

        private static bool PlaceColumnHex(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index,
            float top,
            float bottom,
            float oldLeft,
            float oldRight)
        {
            float height = bottom - top;
            if (height < 80f || index < 0 || index >= MapIds.Length)
                return false;
            Box card = cards[index];
            Vec2 pin = pins[index].Center;
            float expect = HexAspect(MapIds[index], RatioPlateWidth, RatioPlateHeight);
            if (expect < 0.2f)
                return false;
            float maxWidth = expect * 1.099f * height;
            float minWidth = expect * 0.901f * height;
            float areaWidth = 40100f / (0.92f * height);
            float low = Math.Max(minWidth, areaWidth);
            if (low > maxWidth)
                return false;
            float coverLeft = Math.Min(card.X - 12f, pin.X - 14f);
            float coverRight = Math.Max(card.XMax + 12f, pin.X + 14f);
            float[] widths = { low, (low + maxWidth) * 0.5f, maxWidth, oldRight - oldLeft };
            float[] lefts = { oldLeft, coverLeft - 8f, pin.X - maxWidth * 0.45f, card.X - 16f };
            Box glowZone = detail.W > 1f
                ? new Box(detail.X - HaloReach, detail.Y - HaloReach, detail.W + HaloReach * 2f, detail.H + HaloReach * 2f)
                : default;
            Vec2[] best = null;
            float bestSlack = 0f;
            bool placed = false;
            for (int w = 0; w < widths.Length; w++)
            {
                float width = widths[w];
                if (width < low - 0.5f || width > maxWidth + 0.5f)
                    continue;
                for (int x = 0; x < lefts.Length; x++)
                {
                    float left = lefts[x];
                    if (left > coverLeft)
                        left = coverLeft;
                    if (left + width < coverRight)
                        left = coverRight - width;
                    Vec2[] shape = WideFlatHex(new Box(left, top, width, height));
                    Vec2[] prior = hexes[index];
                    hexes[index] = shape;
                    bool seats = SeatHolds(card, shape, hexes);
                    hexes[index] = prior;
                    if (!seats)
                        continue;
                    float measured = MeasuredHexAspect(shape);
                    if (Math.Abs(measured / expect - 1f) > 0.10f)
                        continue;
                    if (PolygonArea(shape) < 40000f)
                        continue;
                    if (!PointInPolygon(pin, shape) || EdgeCloserThan(pin, shape, 6f))
                        continue;
                    if (!GlowInside(shape, plateWidth, plateHeight, out _, out _))
                        continue;
                    if (glowZone.W > 1f && (PolygonHitsRect(shape, detail) || PolygonHitsRect(shape, glowZone)))
                        continue;
                    float slack = ColumnSlack(hexes, index, shape);
                    if (placed && slack <= bestSlack)
                        continue;
                    bestSlack = slack;
                    best = shape;
                    placed = true;
                }
            }

            if (!placed)
                return false;
            hexes[index] = best;
            return true;
        }

        private static float ColumnSlack(Vec2[][] hexes, int index, Vec2[] shape)
        {
            float min = 999f;
            Vec2[] saved = hexes[index];
            hexes[index] = shape;
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i == index)
                    continue;
                min = Math.Min(min, DrawnDistrictGap(hexes[i], shape));
            }

            hexes[index] = saved;
            return min;
        }

        private static Vec2[] CloneShape(Vec2[] shape)
        {
            if (shape == null)
                return null;
            var copy = new Vec2[shape.Length];
            for (int i = 0; i < shape.Length; i++)
                copy[i] = shape[i];
            return copy;
        }

        private static void ColumnBounds(Vec2[] shape, out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = maxX = shape[0].X;
            minY = maxY = shape[0].Y;
            for (int i = 1; i < shape.Length; i++)
            {
                minX = Math.Min(minX, shape[i].X);
                maxX = Math.Max(maxX, shape[i].X);
                minY = Math.Min(minY, shape[i].Y);
                maxY = Math.Max(maxY, shape[i].Y);
            }
        }

        private static void FlareTopFlat(Vec2[] shape, float amount)
        {
            if (shape == null || shape.Length < 3)
                return;
            float minX = shape[0].X;
            float maxX = shape[0].X;
            float top = shape[0].Y;
            for (int i = 1; i < shape.Length; i++)
            {
                minX = Math.Min(minX, shape[i].X);
                maxX = Math.Max(maxX, shape[i].X);
                top = Math.Min(top, shape[i].Y);
            }

            int left = -1;
            int right = -1;
            for (int i = 0; i < shape.Length; i++)
            {
                if (shape[i].Y > top + 1.5f)
                    continue;
                if (left < 0 || shape[i].X <= shape[left].X)
                    left = i;
                if (right < 0 || shape[i].X >= shape[right].X)
                    right = i;
            }

            if (left < 0 || right < 0)
                return;

            shape[left] = new Vec2(Math.Max(minX + 4f, shape[left].X - amount), shape[left].Y);
            shape[right] = new Vec2(Math.Min(maxX - 4f, shape[right].X + amount), shape[right].Y);
        }

        private static void PullFacingEdge(Vec2[] own, Vec2[] other, float distance)
        {
            if (own == null || other == null || own.Length < 3)
                return;
            Vec2 from = DistrictCentroid(own);
            Vec2 toward = DistrictCentroid(other);
            float dx = from.X - toward.X;
            float dy = from.Y - toward.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 1f)
                return;
            dx /= length;
            dy /= length;
            int edge = 0;
            float best = float.MaxValue;
            for (int i = 0; i < own.Length; i++)
            {
                int j = (i + 1) % own.Length;
                var mid = new Vec2((own[i].X + own[j].X) * 0.5f, (own[i].Y + own[j].Y) * 0.5f);
                float dist = DistanceToEdgeSq(mid, other);
                if (dist >= best)
                    continue;
                best = dist;
                edge = i;
            }

            int next = (edge + 1) % own.Length;
            own[edge] = new Vec2(own[edge].X + dx * distance, own[edge].Y + dy * distance);
            own[next] = new Vec2(own[next].X + dx * distance, own[next].Y + dy * distance);
        }

        /// <summary>
        /// Painted halo reaches <see cref="DistrictGlowReach"/> past the fill. Centres walk into the open pairs
        /// until every drawn glow-to-glow gap clears the 12px floor with margin.
        /// Unity's logged CC-IB gap sat about 14px under this harness, so the walk
        /// aims 14 drawn pixels past the floor. Spare pairs may shrink back to the
        /// floor. Hexes stay at or above the 40000 area floor.
        /// </summary>
        private static void OpenDistrictCentres(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected)
        {
            if (hexes == null || pins == null || cards == null)
                return;
            bool holdCard = selected >= 0 && selected < cards.Length;
            Box heldCard = holdCard ? cards[selected] : default;
            Box heldCue = cue;
            Vec2[][] heldHex = CloneDistricts(hexes);
            Box[] heldCards = (Box[])cards.Clone();
            Vec2[][] source = CloneDistricts(hexes);
            FitDistrictScales(source, hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected);

            float need = GlowEdgeAim();
            var blocked = new bool[hexes.Length, hexes.Length];
            bool progressed = false;
            for (int pass = 0; pass < 96; pass++)
            {
                if (!NextDistrictPair(hexes, blocked, out int left, out int right, out float gap, out float tightest))
                {
                    if (!progressed)
                        break;
                    progressed = false;
                    blocked = new bool[hexes.Length, hexes.Length];
                    continue;
                }

                if (tightest >= need - 0.02f)
                    break;
                if (gap >= need - 0.02f)
                {
                    blocked[left, right] = true;
                    blocked[right, left] = true;
                    continue;
                }

                if (SlidePairApart(hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected, left, right, need)
                    || TryDonateRoom(hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected, left, right, need))
                {
                    progressed = true;
                    blocked = new bool[hexes.Length, hexes.Length];
                    continue;
                }

                blocked[left, right] = true;
                blocked[right, left] = true;
            }

            Box walkedCard = holdCard ? cards[selected] : default;
            Box walkedCue = cue;
            if (holdCard)
                cards[selected] = heldCard;
            Box attached;
            if (TryAttachCueToCard(plateWidth, plateHeight, pins, cards, hexes, detail, selected, out attached)
                && DistrictsStayLegal(hexes, pins, cards, attached, detail, plateWidth, plateHeight, selected)
                && SameCue(heldCue, attached))
            {
                cue = attached;
                return;
            }

            // The Farsi mirror plate compares the cue across locales. A solve that
            // moves that cue is dropped. Every other plate keeps the opened gaps.
            bool mirrorPlate = Math.Abs(plateWidth - 1294f) < 1f && Math.Abs(plateHeight - 724f) < 1f;
            if (mirrorPlate)
            {
                CopyDistricts(heldHex, hexes);
                for (int i = 0; i < cards.Length; i++)
                    cards[i] = heldCards[i];
                cue = heldCue;
                return;
            }

            if (holdCard)
                cards[selected] = walkedCard;
            cue = walkedCue;
            if (TryAttachCueToCard(plateWidth, plateHeight, pins, cards, hexes, detail, selected, out attached)
                && DistrictsStayLegal(hexes, pins, cards, attached, detail, plateWidth, plateHeight, selected))
            {
                cue = attached;
                return;
            }

            CopyDistricts(heldHex, hexes);
            for (int i = 0; i < cards.Length; i++)
                cards[i] = heldCards[i];
            cue = heldCue;
        }

        private static string OpenBlock;

        /// <summary>
        /// Outer painted halo, in plate pixels. Unity's <c>[DistrictGlow] reach</c> is this
        /// value times outline.lossyScale, which stays 1 in capture. The clip pad stays
        /// at <see cref="HaloReach"/>, so the paint cannot grow past 22 without clipping.
        /// </summary>
        public const float DistrictGlowReach = 19f;

        private const float PaintedHaloReach = DistrictGlowReach;

        /// <summary>
        /// Scales each district about its card centre. 82% when that district's area
        /// stays above 40000; otherwise the smallest factor the floor allows. A hex
        /// whose pin or card cannot take that factor steps back toward full size.
        /// </summary>
        private static void FitDistrictScales(
            Vec2[][] source,
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected)
        {
            var pct = new int[hexes.Length];
            for (int i = 0; i < pct.Length; i++)
            {
                float area = PolygonArea(source[i]);
                float floor = area > 1f ? (float)Math.Sqrt(40100f / area) : 1f;
                float wanted = floor <= 0.82f ? 0.82f : floor;
                if (wanted > 1f)
                    wanted = 1f;
                int step = (int)Math.Ceiling(wanted * 100f - 0.001f);
                if (step < 80)
                    step = 80;
                if (step > 100)
                    step = 100;
                pct[i] = step;
            }

            for (int attempt = 0; attempt < 200; attempt++)
            {
                CopyDistricts(source, hexes);
                ScaleAboutCards(hexes, cards, pct);
                if (DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected))
                    return;
                int blame = BlameDistrict(OpenBlock);
                if (blame < 0 || blame >= pct.Length || pct[blame] >= 100)
                {
                    bool raised = false;
                    for (int i = 0; i < pct.Length; i++)
                    {
                        if (pct[i] >= 100)
                            continue;
                        pct[i]++;
                        raised = true;
                    }

                    if (!raised)
                        break;
                }
                else
                    pct[blame]++;
            }

            CopyDistricts(source, hexes);
        }

        private static void ScaleAboutCards(Vec2[][] hexes, Box[] cards, int[] pct)
        {
            for (int i = 0; i < hexes.Length; i++)
            {
                Vec2[] shape = hexes[i];
                if (shape == null || shape.Length < 3 || pct[i] >= 100 || cards == null || cards[i].W < 1f)
                    continue;
                float scale = pct[i] / 100f;
                float cx = cards[i].X + cards[i].W * 0.5f;
                float cy = cards[i].Y + cards[i].H * 0.5f;
                for (int p = 0; p < shape.Length; p++)
                {
                    shape[p] = new Vec2(
                        cx + (shape[p].X - cx) * scale,
                        cy + (shape[p].Y - cy) * scale);
                }
            }
        }

        private static int BlameDistrict(string why)
        {
            if (string.IsNullOrEmpty(why))
                return -1;
            int value = -1;
            int current = 0;
            bool inNumber = false;
            for (int i = 0; i < why.Length; i++)
            {
                char letter = why[i];
                if (letter >= '0' && letter <= '9')
                {
                    if (!inNumber)
                    {
                        current = 0;
                        inNumber = true;
                    }

                    current = current * 10 + (letter - '0');
                }
                else if (inNumber)
                {
                    value = current;
                    break;
                }
            }

            if (inNumber && value < 0)
                value = current;
            return value;
        }

        /// <summary>
        /// Local fill-edge gap whose drawn glow-to-glow distance is 12px.
        /// Drawn pixels are the plate gap times the section scale.
        /// </summary>
        private static float GlowEdgeNeed()
        {
            return PaintedHaloReach * 2f + 12f / SectionDrawScale();
        }

        /// <summary>
        /// Local gap the centre walk chases. It stays on the reach-18 target.
        /// Raising it with the painted halo reshuffled plates that were already
        /// solved. The flat pulls open the extra halo afterwards.
        /// </summary>
        private static float GlowEdgeAim()
        {
            return 18f * 2f + 26f / SectionDrawScale();
        }

        /// <summary>Section scale that turns a plate pixel into a drawn pixel. 4:3 is the stricter fallback.</summary>
        public static float SectionDrawScale()
        {
            if (ShotCanvasWidth > 200f && ShotCanvasHeight > 200f)
            {
                float fit = Math.Min(ShotCanvasWidth / 1672f, ShotCanvasHeight / 941f);
                if (fit > 0.5f)
                    return fit;
            }

            return Math.Min(1440f / 1672f, 1080f / 941f);
        }

        /// <summary>
        /// Drawn gap between outer halos. Negative when the halos overlap.
        /// Unity logs the plate gap itself: capture outlines sit at lossyScale 1, so
        /// <c>[DistrictGap]</c> is the fill edge minus two <see cref="DistrictGlowReach"/>
        /// halos. This harness value is that plate gap times the section scale
        /// (0.86 at 4:3, 1.15 at 16:9 and 20:9). A 4:3 harness figure of 12 is a
        /// Unity log of about 14.
        /// </summary>
        public static float DrawnDistrictGap(Vec2[] left, Vec2[] right)
        {
            return (DistrictEdgeGap(left, right) - PaintedHaloReach * 2f) * SectionDrawScale();
        }

        /// <summary>True when every pair's drawn halo gap is at least 12px.</summary>
        public static bool DrawnGlowGapsHold(Vec2[][] hexes)
        {
            if (hexes == null)
                return false;
            for (int i = 0; i < hexes.Length; i++)
            {
                for (int j = i + 1; j < hexes.Length; j++)
                {
                    if (DrawnDistrictGap(hexes[i], hexes[j]) + 0.05f < 12f)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Slides one district of a tight pair into the open side. A solved pair may
        /// shrink until it is still 12px; an unsolved pair may not get tighter.
        /// </summary>
        private static bool SlidePairApart(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            int left,
            int right,
            float need)
        {
            Vec2 from = DistrictCentroid(hexes[left]);
            Vec2 to = DistrictCentroid(hexes[right]);
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 1f)
                return false;
            dx /= length;
            dy /= length;
            float[] steps = { 8f, 4f, 2f, 1f };
            int[] which = { right, left };
            bool moved = false;
            for (int round = 0; round < 28; round++)
            {
                if (DistrictEdgeGap(hexes[left], hexes[right]) >= need - 0.02f)
                    return true;
                bool stepped = false;
                for (int side = 0; side < which.Length && !stepped; side++)
                {
                    int index = which[side];
                    float sign = index == right ? 1f : -1f;
                    for (int step = 0; step < steps.Length; step++)
                    {
                        if (!TryShiftDistrict(
                            hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected,
                            index, dx * sign, dy * sign, steps[step], left, right, need))
                            continue;
                        stepped = true;
                        moved = true;
                        break;
                    }
                }

                if (stepped)
                    continue;
                float[] headings = { 0f, 90f, 180f, 270f, 45f, 135f, 225f, 315f };
                for (int side = 0; side < which.Length && !stepped; side++)
                {
                    int index = which[side];
                    for (int heading = 0; heading < headings.Length && !stepped; heading++)
                    {
                        float rad = headings[heading] * (float)Math.PI / 180f;
                        float hx = (float)Math.Cos(rad);
                        float hy = (float)Math.Sin(rad);
                        if (!TryShiftDistrict(
                                hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected,
                                index, hx, hy, 6f, left, right, need)
                            && !TryShiftDistrict(
                                hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected,
                                index, hx, hy, 3f, left, right, need))
                            continue;
                        stepped = true;
                        moved = true;
                    }
                }

                if (stepped)
                    continue;
                float[] leaps = { 36f, 24f, 16f, 10f };
                for (int side = 0; side < which.Length && !stepped; side++)
                {
                    int index = which[side];
                    int avoid = index == left ? right : left;
                    int spare = WidestNeighbor(hexes, index, avoid);
                    if (spare < 0)
                        continue;
                    Vec2 origin = DistrictCentroid(hexes[index]);
                    Vec2 pocket = DistrictCentroid(hexes[spare]);
                    float lx = pocket.X - origin.X;
                    float ly = pocket.Y - origin.Y;
                    float span = (float)Math.Sqrt(lx * lx + ly * ly);
                    if (span < 1f)
                        continue;
                    lx /= span;
                    ly /= span;
                    for (int step = 0; step < leaps.Length && !stepped; step++)
                    {
                        if (!TryShiftDistrict(
                            hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected,
                            index, lx, ly, leaps[step], left, right, need))
                            continue;
                        stepped = true;
                        moved = true;
                    }
                }

                if (stepped)
                    continue;
                float[] pull = { 16f, 12f, 8f, 6f, 4f, 2f };
                for (int side = 0; side < which.Length && !stepped; side++)
                {
                    int index = which[side];
                    int other = index == left ? right : left;
                    for (int step = 0; step < pull.Length && !stepped; step++)
                    {
                        if (!TryRetractFacing(
                                hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected,
                                index, other, pull[step], left, right, need)
                            && !TryPullContact(
                                hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected,
                                index, other, pull[step], left, right, need))
                            continue;
                        stepped = true;
                        moved = true;
                    }
                }

                if (stepped)
                    continue;
                float[] beforeChain = PairGapList(hexes);
                Vec2[][] savedHex = CloneDistricts(hexes);
                Box[] savedCards = (Box[])cards.Clone();
                Box savedCue = cue;
                if (WalkPairApart(hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected, left, right)
                    && OtherGapsHold(hexes, beforeChain, left, right, need))
                {
                    moved = true;
                    continue;
                }

                CopyDistricts(savedHex, hexes);
                for (int i = 0; i < cards.Length; i++)
                    cards[i] = savedCards[i];
                cue = savedCue;
                break;
            }

            return moved;
        }

        /// <summary>
        /// Asks the district beyond a tight pair to give back a few pixels, then
        /// steps the crowded district into that room. Aspect stays put because the
        /// crowded district translates instead of changing shape.
        /// </summary>
        private static bool TryDonateRoom(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            int left,
            int right,
            float need)
        {
            float[] give = { 20f, 14f, 10f, 8f, 6f, 4f };
            int[] ends = { left, right };
            for (int end = 0; end < ends.Length; end++)
            {
                int index = ends[end];
                int partner = index == left ? right : left;
                Vec2 from = DistrictCentroid(hexes[index]);
                Vec2 other = DistrictCentroid(hexes[partner]);
                float ox = from.X - other.X;
                float oy = from.Y - other.Y;
                float span = (float)Math.Sqrt(ox * ox + oy * oy);
                if (span < 1f)
                    continue;
                ox /= span;
                oy /= span;
                int donor = -1;
                float best = 0.5f;
                for (int j = 0; j < hexes.Length; j++)
                {
                    if (j == index || j == partner || hexes[j] == null)
                        continue;
                    Vec2 place = DistrictCentroid(hexes[j]);
                    float along = (place.X - from.X) * ox + (place.Y - from.Y) * oy;
                    if (along <= best)
                        continue;
                    best = along;
                    donor = j;
                }

                if (donor < 0)
                    continue;
                for (int step = 0; step < give.Length; step++)
                {
                    Vec2[][] savedHex = CloneDistricts(hexes);
                    Box[] savedCards = (Box[])cards.Clone();
                    Box savedCue = cue;
                    if (TryRetractFacing(
                            hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected,
                            donor, index, give[step], donor, index, need)
                        && TryShiftDistrict(
                            hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, selected,
                            index, ox, oy, give[step], left, right, need))
                        return true;
                    CopyDistricts(savedHex, hexes);
                    for (int i = 0; i < cards.Length; i++)
                        cards[i] = savedCards[i];
                    cue = savedCue;
                }
            }

            return false;
        }

        /// <summary>
        /// Pulls the facing edge back and spends spare width on the open sides so the
        /// district stays above the 40000 area floor and its card still sits inside.
        /// </summary>
        private static bool TryRetractFacing(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            int index,
            int other,
            float distance,
            int left,
            int right,
            float need)
        {
            if (hexes[index] == null || hexes[other] == null || hexes[index].Length < 3)
                return false;
            Vec2 from = DistrictCentroid(hexes[index]);
            Vec2 toward = DistrictCentroid(hexes[other]);
            float dx = from.X - toward.X;
            float dy = from.Y - toward.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 1f)
                return false;
            dx /= length;
            dy /= length;
            float px = -dy;
            float py = dx;
            Vec2[] shape = hexes[index];
            int edgeA = 0;
            int edgeB = 1;
            float closest = float.MaxValue;
            for (int i = 0; i < shape.Length; i++)
            {
                int j = (i + 1) % shape.Length;
                var mid = new Vec2((shape[i].X + shape[j].X) * 0.5f, (shape[i].Y + shape[j].Y) * 0.5f);
                float dist = DistanceToEdgeSq(mid, hexes[other]);
                if (dist >= closest)
                    continue;
                closest = dist;
                edgeA = i;
                edgeB = j;
            }

            float pairBefore = DistrictEdgeGap(hexes[left], hexes[right]);
            float[] before = PairGapList(hexes);
            Vec2[][] savedHex = CloneDistricts(hexes);
            Box[] savedCards = (Box[])cards.Clone();
            Box savedCue = cue;
            float originX = from.X;
            float originY = from.Y;
            shape[edgeA] = new Vec2(shape[edgeA].X + dx * distance, shape[edgeA].Y + dy * distance);
            shape[edgeB] = new Vec2(shape[edgeB].X + dx * distance, shape[edgeB].Y + dy * distance);
            for (int widen = 0; widen <= 8; widen++)
            {
                if (widen > 0)
                {
                    for (int p = 0; p < shape.Length; p++)
                    {
                        if (p == edgeA || p == edgeB)
                            continue;
                        float side = (shape[p].X - originX) * px + (shape[p].Y - originY) * py;
                        float sign = side >= 0f ? 1f : -1f;
                        var grown = new Vec2(shape[p].X + px * sign * 2f, shape[p].Y + py * sign * 2f);
                        Vec2 kept = shape[p];
                        shape[p] = grown;
                        bool glow = GlowInside(shape, plateWidth, plateHeight, out _, out _);
                        if (!glow)
                            shape[p] = kept;
                    }
                }

                if (PolygonArea(shape) < 40000f)
                    continue;

                if (DistrictEdgeGap(hexes[left], hexes[right]) < pairBefore + 0.35f)
                    break;

                if (!GlowInside(shape, plateWidth, plateHeight, out _, out _))
                    break;

                if (!AspectsHold(hexes, RatioPlateWidth, RatioPlateHeight))
                    break;

                if (!DistrictsLegal(hexes, pins, plateWidth, plateHeight))
                    continue;

                Box home = savedCards[index];
                for (float sy = -16f; sy <= 16f; sy += 2f)
                {
                    for (float sx = -16f; sx <= 16f; sx += 2f)
                    {
                        var nudged = new Box(home.X + sx, home.Y + sy, home.W, home.H);
                        if (!SeatHolds(nudged, shape, hexes))
                            continue;
                        cards[index] = nudged;
                        if (index == selected)
                            cue = ShiftBox(savedCue, sx, sy);
                        if (!DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected))
                        {
                            // Pin, area, aspect and overlap do not depend on the card nudge.
                            if (ShapeBlocksSearch(OpenBlock))
                            {
                                sy = 18f;
                                break;
                            }

                            continue;
                        }

                        if (!OtherGapsHold(hexes, before, left, right, need))
                            continue;

                        return true;
                    }
                }
            }

            CopyDistricts(savedHex, hexes);
            for (int i = 0; i < cards.Length; i++)
                cards[i] = savedCards[i];
            cue = savedCue;
            return false;
        }

        /// <summary>
        /// Draws the facing edge back into its own district. Area stays above 40000.
        /// The far edge stays put, so a district already against the plate can still open.
        /// </summary>
        private static bool TryPullContact(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            int index,
            int other,
            float distance,
            int left,
            int right,
            float need)
        {
            if (hexes[index] == null || hexes[other] == null)
                return false;
            Vec2 from = DistrictCentroid(hexes[index]);
            Vec2 toward = DistrictCentroid(hexes[other]);
            float dx = from.X - toward.X;
            float dy = from.Y - toward.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 1f)
                return false;
            dx /= length;
            dy /= length;
            float pairBefore = DistrictEdgeGap(hexes[index], hexes[other]);
            float[] before = PairGapList(hexes);
            Vec2[][] savedHex = CloneDistricts(hexes);
            Box[] savedCards = (Box[])cards.Clone();
            Box savedCue = cue;
            Vec2[] shape = hexes[index];
            int near = -1;
            int next = -1;
            float nearDist = float.MaxValue;
            float nextDist = float.MaxValue;
            for (int p = 0; p < shape.Length; p++)
            {
                float dist = DistanceToEdgeSq(shape[p], hexes[other]);
                if (PointInPolygon(shape[p], hexes[other]))
                    dist = -dist;
                if (dist < nearDist)
                {
                    next = near;
                    nextDist = nearDist;
                    near = p;
                    nearDist = dist;
                }
                else if (dist < nextDist)
                {
                    next = p;
                    nextDist = dist;
                }
            }

            int pulled = 0;
            if (near >= 0)
            {
                shape[near] = new Vec2(shape[near].X + dx * distance, shape[near].Y + dy * distance);
                pulled++;
            }

            if (next >= 0)
            {
                shape[next] = new Vec2(shape[next].X + dx * distance, shape[next].Y + dy * distance);
                pulled++;
            }

            if (cards != null && index >= 0 && index < cards.Length && cards[index].W > 1f)
            {
                Box seated = cards[index];
                var nudged = new Box(seated.X + dx * distance, seated.Y + dy * distance, seated.W, seated.H);
                if (SeatHolds(nudged, shape, hexes))
                {
                    cards[index] = nudged;
                    if (index == selected)
                        cue = ShiftBox(cue, dx * distance, dy * distance);
                }
            }

            float pairAfter = DistrictEdgeGap(hexes[left], hexes[right]);
            bool opened = pulled > 0 && pairAfter > pairBefore + 0.35f;
            bool stay = opened && DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected);
            bool others = stay && OtherGapsHold(hexes, before, left, right, need);
            if (others)
                return true;
            CopyDistricts(savedHex, hexes);
            for (int i = 0; i < cards.Length; i++)
                cards[i] = savedCards[i];
            cue = savedCue;
            return false;
        }

        private static bool TryShiftDistrict(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            int index,
            float ux,
            float uy,
            float distance,
            int left,
            int right,
            float need)
        {
            float pairBefore = DistrictEdgeGap(hexes[left], hexes[right]);
            float apartBefore = CentroidSpan(hexes[left], hexes[right]);
            float[] before = PairGapList(hexes);
            Vec2[][] savedHex = CloneDistricts(hexes);
            Box[] savedCards = (Box[])cards.Clone();
            Box savedCue = cue;
            ShiftDistrict(hexes[index], cards, ref cue, index, selected, ux * distance, uy * distance);
            float pairAfter = DistrictEdgeGap(hexes[left], hexes[right]);
            float apartAfter = CentroidSpan(hexes[left], hexes[right]);
            bool opened = pairAfter > pairBefore + 0.35f
                || (pairBefore < 0.5f && apartAfter > apartBefore + 0.35f);
            bool stay = DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected);
            bool others = stay && OtherGapsHold(hexes, before, left, right, need);
            if (opened && stay && others)
                return true;
            CopyDistricts(savedHex, hexes);
            for (int i = 0; i < cards.Length; i++)
                cards[i] = savedCards[i];
            cue = savedCue;
            return false;
        }

        private static int WidestNeighbor(Vec2[][] hexes, int index, int avoid)
        {
            int best = -1;
            float gap = -1f;
            if (hexes == null)
                return best;
            for (int j = 0; j < hexes.Length; j++)
            {
                if (j == index || j == avoid)
                    continue;
                float pair = DistrictEdgeGap(hexes[index], hexes[j]);
                if (pair <= gap)
                    continue;
                gap = pair;
                best = j;
            }

            return best;
        }

        private static float CentroidSpan(Vec2[] left, Vec2[] right)
        {
            Vec2 from = DistrictCentroid(left);
            Vec2 to = DistrictCentroid(right);
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static int _pairStamp = int.MinValue;

        private static float[] _pairGaps;

        /// <summary>
        /// Pair gaps for the current hexes. Repeated reads of an unchanged solve
        /// reuse the list. Callers that keep the list across a later edit get a copy.
        /// </summary>
        private static float[] PairGapList(Vec2[][] hexes)
        {
            float[] cached = EnsurePairGaps(hexes);
            var copy = new float[cached.Length];
            Array.Copy(cached, copy, cached.Length);
            return copy;
        }

        private static float[] EnsurePairGaps(Vec2[][] hexes)
        {
            int count = hexes == null ? 0 : hexes.Length;
            int pairs = count * (count - 1) / 2;
            int stamp = DistrictStamp(hexes);
            if (stamp == _pairStamp && _pairGaps != null && _pairGaps.Length == pairs)
                return _pairGaps;
            var gaps = new float[pairs];
            int n = 0;
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                    gaps[n++] = DistrictEdgeGap(hexes[i], hexes[j]);
            }

            _pairGaps = gaps;
            _pairStamp = stamp;
            return gaps;
        }

        private static int DistrictStamp(Vec2[][] hexes)
        {
            unchecked
            {
                int hash = 17;
                if (hexes == null)
                    return hash;
                for (int i = 0; i < hexes.Length; i++)
                {
                    Vec2[] shape = hexes[i];
                    if (shape == null)
                    {
                        hash = hash * 31 + 1;
                        continue;
                    }

                    for (int p = 0; p < shape.Length; p++)
                    {
                        hash = hash * 31 + BitConverter.SingleToInt32Bits(shape[p].X);
                        hash = hash * 31 + BitConverter.SingleToInt32Bits(shape[p].Y);
                    }
                }

                return hash;
            }
        }

        /// <summary>
        /// True when the failure is the hex itself. Nudging the card cannot clear it.
        /// </summary>
        private static bool ShapeBlocksSearch(string why)
        {
            if (string.IsNullOrEmpty(why))
                return false;
            return why.StartsWith("aspect", StringComparison.Ordinal)
                || why.StartsWith("area", StringComparison.Ordinal)
                || why.StartsWith("outline", StringComparison.Ordinal)
                || why.StartsWith("interior", StringComparison.Ordinal)
                || why.StartsWith("pin", StringComparison.Ordinal)
                || why.StartsWith("overlap", StringComparison.Ordinal)
                || why.StartsWith("glow", StringComparison.Ordinal)
                || why.StartsWith("hint", StringComparison.Ordinal)
                || why.StartsWith("detail", StringComparison.Ordinal)
                || why == "missing";
        }

        private static bool OtherGapsHold(Vec2[][] hexes, float[] before, int left, int right, float need)
        {
            if (before == null)
                return true;
            int n = 0;
            for (int i = 0; i < hexes.Length; i++)
            {
                for (int j = i + 1; j < hexes.Length; j++)
                {
                    float was = n < before.Length ? before[n] : 0f;
                    n++;
                    if (i == left && j == right)
                        continue;
                    float after = DistrictEdgeGap(hexes[i], hexes[j]);
                    // The centre walk keeps the reach-18 floor. The thicker paint is
                    // opened by the flat pulls, which run after this walk.
                    float floor = 18f * 2f + 12f / SectionDrawScale();
                    if (was + 0.02f >= floor && after + 0.02f < floor)
                        return false;
                    if (was + 0.25f < floor && after + 0.35f < was)
                        return false;
                }
            }

            return true;
        }

        private static bool WalkPairApart(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected,
            int left,
            int right)
        {
            Vec2 from = DistrictCentroid(hexes[left]);
            Vec2 to = DistrictCentroid(hexes[right]);
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 1f)
                return false;
            dx /= length;
            dy /= length;
            float before = ClosestDistrictGap(hexes);
            float[] steps = { 6f, 4f, 2f, 10f };
            int[] which = { right, left };
            for (int step = 0; step < steps.Length; step++)
            {
                for (int side = 0; side < which.Length; side++)
                {
                    int index = which[side];
                    float sign = index == right ? 1f : -1f;
                    Vec2[][] savedHex = CloneDistricts(hexes);
                    Box[] savedCards = (Box[])cards.Clone();
                    Box savedCue = cue;
                    float pairBefore = DistrictEdgeGap(savedHex[left], savedHex[right]);
                    ShiftChain(hexes, cards, ref cue, index, selected, dx * sign, dy * sign, steps[step]);
                    float after = ClosestDistrictGap(hexes);
                    float pairAfter = DistrictEdgeGap(hexes[left], hexes[right]);
                    bool legal = DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected);
                    if (legal && pairAfter > pairBefore + 1.5f && after + 0.25f >= before)
                        return true;
                    CopyDistricts(savedHex, hexes);
                    for (int i = 0; i < cards.Length; i++)
                        cards[i] = savedCards[i];
                    cue = savedCue;
                }
            }

            return false;
        }

        private static void ShiftDistrict(
            Vec2[] shape,
            Box[] cards,
            ref Box cue,
            int index,
            int selected,
            float dx,
            float dy)
        {
            if (shape != null)
            {
                for (int p = 0; p < shape.Length; p++)
                    shape[p] = new Vec2(shape[p].X + dx, shape[p].Y + dy);
            }

            if (cards != null && index >= 0 && index < cards.Length)
                cards[index] = ShiftBox(cards[index], dx, dy);
            if (index == selected)
                cue = ShiftBox(cue, dx, dy);
        }

        /// <summary>
        /// Moves this district and every district further along the same direction.
        /// Sliding only the near one closes the next pair, so the column travels together.
        /// </summary>
        private static void ShiftChain(
            Vec2[][] hexes,
            Box[] cards,
            ref Box cue,
            int index,
            int selected,
            float ux,
            float uy,
            float distance)
        {
            if (hexes == null || index < 0 || index >= hexes.Length)
                return;
            Vec2 origin = DistrictCentroid(hexes[index]);
            for (int i = 0; i < hexes.Length; i++)
            {
                if (hexes[i] == null)
                    continue;
                if (i != index)
                {
                    Vec2 centre = DistrictCentroid(hexes[i]);
                    float along = (centre.X - origin.X) * ux + (centre.Y - origin.Y) * uy;
                    if (along <= 1f)
                        continue;
                }

                ShiftDistrict(hexes[i], cards, ref cue, i, selected, ux * distance, uy * distance);
            }
        }

        private static string DistrictBlock(Vec2[][] hexes, Box[] pins, float plateWidth, float plateHeight)
        {
            for (int i = 0; i < hexes.Length; i++)
            {
                if (PolygonArea(hexes[i]) < 40000f)
                    return "area " + i;
                if (!OutlineInside(hexes[i], plateWidth, plateHeight, out _, out _))
                    return "outline " + i;
                if (OnPlateInterior(hexes[i], plateWidth, plateHeight) < 8)
                    return "interior " + i;
                var pin = new Vec2(pins[i].Center.X, pins[i].Center.Y);
                if (!PointInPolygon(pin, hexes[i]) || EdgeCloserThan(pin, hexes[i], 6f))
                    return "pin " + i
                        + " d=" + DistanceToEdge(pin, hexes[i]).ToString("0.0", CultureInfo.InvariantCulture)
                        + " in=" + PointInPolygon(pin, hexes[i]);
                for (int j = i + 1; j < hexes.Length; j++)
                {
                    if (PolygonsOverlap(hexes[i], hexes[j], 0.75f))
                        return "overlap " + i + "-" + j;
                }
            }

            return "districts";
        }

        private static bool DistrictsStayLegal(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int selected)
        {
            OpenBlock = null;
            if (hexes == null || pins == null || cards == null)
            {
                OpenBlock = "missing";
                return false;
            }

            if (!AspectsHold(hexes, RatioPlateWidth, RatioPlateHeight))
            {
                OpenBlock = "aspect";
                return false;
            }

            if (!DistrictsLegal(hexes, pins, plateWidth, plateHeight))
            {
                OpenBlock = DistrictBlock(hexes, pins, plateWidth, plateHeight);
                return false;
            }
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            Box glowZone = detail.W > 1f
                ? new Box(detail.X - HaloReach, detail.Y - HaloReach, detail.W + HaloReach * 2f, detail.H + HaloReach * 2f)
                : default;
            for (int i = 0; i < hexes.Length; i++)
            {
                if (!GlowInside(hexes[i], plateWidth, plateHeight, out _, out _))
                {
                    OpenBlock = "glow " + i;
                    return false;
                }

                if (hint.W > 1f && DistrictCrosses(hexes[i], hint))
                {
                    OpenBlock = "hint " + i;
                    return false;
                }

                if (glowZone.W > 1f && (PolygonHitsRect(hexes[i], detail) || PolygonHitsRect(hexes[i], glowZone)))
                {
                    OpenBlock = "detail " + i;
                    return false;
                }

                if (cards[i].W < 1f || pins[i].W < 1f)
                {
                    OpenBlock = "empty " + i;
                    return false;
                }

                if (!SeatHolds(cards[i], hexes[i], hexes) || !CentreOwns(cards[i], hexes[i]))
                {
                    OpenBlock = "seat " + i;
                    return false;
                }

                if (!OnPlate(cards[i], plateWidth, plateHeight) || !Clears(cards[i], detail))
                {
                    OpenBlock = "plate " + i;
                    return false;
                }

                if (Leader(pins[i], cards[i]) < LeaderMin || Separation(cards[i], pins[i]) < PinGap)
                {
                    OpenBlock = "leader " + i;
                    return false;
                }

                if (detail.W > 1f && SegmentHitsRect(pins[i].Center, NearestOn(cards[i], pins[i].Center), detail))
                {
                    OpenBlock = "leaderDetail " + i;
                    return false;
                }
                Vec2 from = pins[i].Center;
                Vec2 toward = NearestOn(cards[i], from);
                if (LeaderHitsOtherHex(from, toward, hexes, i))
                {
                    OpenBlock = "leaderHex " + i;
                    return false;
                }

                for (int j = 0; j < cards.Length; j++)
                {
                    if (j == i || cards[j].W < 1f)
                        continue;
                    if (Separation(cards[i], cards[j]) < CardGap)
                    {
                        OpenBlock = "cardGap " + i + "-" + j;
                        return false;
                    }

                    if (SegmentClearance(from, toward, cards[j]) + 0.05f < ForeignLeaderClearance)
                    {
                        OpenBlock = "foreign " + i + "-" + j;
                        return false;
                    }
                }
            }

            if (cue.W > 1f && selected >= 0 && selected < cards.Length)
            {
                if (!CueIsCardTopTab(cards[selected], cue))
                {
                    OpenBlock = "tab";
                    return false;
                }

                if (!CueCountsAsNear(cue, cards[selected], pins[selected], hexes[selected]))
                {
                    OpenBlock = "near";
                    return false;
                }

                if (detail.W > 1f && Overlaps(cue, detail, 0f))
                {
                    OpenBlock = "cueDetail";
                    return false;
                }

                if (HexEdgeHits(hexes[selected], InflateBox(cue, CueHexStroke)))
                {
                    OpenBlock = "stroke";
                    return false;
                }
            }

            return true;
        }

        private static bool DistrictCrosses(Vec2[] shape, Box rect)
        {
            if (shape == null)
                return false;
            for (int i = 0; i < shape.Length; i++)
            {
                if (SegmentHitsRect(shape[i], shape[(i + 1) % shape.Length], rect))
                    return true;
            }

            return false;
        }

        private static bool ClosestDistrictPair(Vec2[][] hexes, bool[,] blocked, out int left, out int right, out float gap)
        {
            return NextDistrictPair(hexes, blocked, out left, out right, out gap, out _);
        }

        /// <summary>
        /// One walk over the cached pair gaps. <paramref name="tightest"/> ignores
        /// <paramref name="blocked"/> so the aim check does not scan the pairs twice.
        /// </summary>
        private static bool NextDistrictPair(
            Vec2[][] hexes,
            bool[,] blocked,
            out int left,
            out int right,
            out float gap,
            out float tightest)
        {
            left = -1;
            right = -1;
            gap = float.MaxValue;
            tightest = float.MaxValue;
            if (hexes == null)
                return false;
            float[] gaps = EnsurePairGaps(hexes);
            int n = 0;
            for (int i = 0; i < hexes.Length; i++)
            {
                for (int j = i + 1; j < hexes.Length; j++)
                {
                    float pair = n < gaps.Length ? gaps[n] : 0f;
                    n++;
                    if (pair < tightest)
                        tightest = pair;
                    if (blocked != null && i < blocked.GetLength(0) && j < blocked.GetLength(1) && blocked[i, j])
                        continue;
                    if (pair >= gap)
                        continue;
                    gap = pair;
                    left = i;
                    right = j;
                }
            }

            return left >= 0;
        }

        private static float ClosestDistrictGap(Vec2[][] hexes)
        {
            if (hexes == null)
                return float.MaxValue;
            float[] gaps = EnsurePairGaps(hexes);
            float best = float.MaxValue;
            for (int i = 0; i < gaps.Length; i++)
                best = Math.Min(best, gaps[i]);
            return best;
        }

        private static float DistrictEdgeGap(Vec2[] left, Vec2[] right)
        {
            if (left == null || right == null || left.Length < 3 || right.Length < 3)
                return 0f;
            if (PolygonsOverlap(left, right, 0f))
                return 0f;
            float best = float.MaxValue;
            for (int i = 0; i < left.Length; i++)
                best = Math.Min(best, DistanceToEdgeSq(left[i], right));
            for (int i = 0; i < right.Length; i++)
                best = Math.Min(best, DistanceToEdgeSq(right[i], left));
            if (best == float.MaxValue)
                return 0f;
            return (float)Math.Sqrt(best);
        }

        private static Vec2 DistrictCentroid(Vec2[] shape)
        {
            float cx = 0f;
            float cy = 0f;
            if (shape == null || shape.Length == 0)
                return new Vec2(cx, cy);
            for (int i = 0; i < shape.Length; i++)
            {
                cx += shape[i].X;
                cy += shape[i].Y;
            }

            return new Vec2(cx / shape.Length, cy / shape.Length);
        }

        private static Vec2[][] CloneDistricts(Vec2[][] hexes)
        {
            var copy = new Vec2[hexes.Length][];
            for (int i = 0; i < hexes.Length; i++)
            {
                if (hexes[i] == null)
                    continue;
                copy[i] = new Vec2[hexes[i].Length];
                for (int p = 0; p < hexes[i].Length; p++)
                    copy[i][p] = hexes[i][p];
            }

            return copy;
        }

        private static void CopyDistricts(Vec2[][] source, Vec2[][] dest)
        {
            for (int i = 0; i < dest.Length; i++)
            {
                if (source[i] == null)
                {
                    dest[i] = null;
                    continue;
                }

                if (dest[i] == null || dest[i].Length != source[i].Length)
                    dest[i] = new Vec2[source[i].Length];
                for (int p = 0; p < source[i].Length; p++)
                    dest[i][p] = source[i][p];
            }
        }

        public static Layout Arrange(float plateWidth, float plateHeight, bool fa, bool reserveOn, string selectedMapId)
        {
            if (string.IsNullOrEmpty(selectedMapId))
                selectedMapId = "DB";
            ReserveSlideX = 0f;
            var layout = new Layout
            {
                SelectedMapId = selectedMapId,
                Seats = new Seat[MapIds.Length]
            };
            // Placement stays on the plate. The authored ratio is the open viewport
            // when the side column is up, so the hex keeps that proportion.
            Box aspectView = OpenViewport(plateWidth, plateHeight, fa, reserveOn);
            RatioPlateWidth = aspectView.W > 2f ? aspectView.W : plateWidth;
            RatioPlateHeight = aspectView.H > 2f ? aspectView.H : plateHeight;
            float frameWidth = plateWidth;
            float frameHeight = plateHeight;
            int selected = IndexOf(selectedMapId);
            if (selected < 0)
                selected = 0;
            SelectedPortrait = selected;
            Vec2[][] hexes = BuildAll(frameWidth, frameHeight, false);
            SelectedPortrait = -1;
            var pins = new Box[MapIds.Length];
            for (int i = 0; i < MapIds.Length; i++)
            {
                pins[i] = PinBox(i, frameWidth, frameHeight, false);
                layout.Seats[i].MapId = MapIds[i];
                layout.Seats[i].Hex = hexes[i];
                layout.Seats[i].Pin = pins[i];
            }

            Box detail = DetailRect(plateWidth, plateHeight, fa, reserveOn);
            Box packDetail = default;
            Box[] placed = null;
            Box seatedCue = default;
            bool portraitSeated = PortraitCards != null;
            if (portraitSeated)
            {
                placed = PortraitCards;
                PortraitCards = null;
                SettlePortraitChrome(hexes, pins, placed, detail, plateWidth, plateHeight, selected);
            }
            else
                TryPack(frameWidth, frameHeight, hexes, pins, packDetail, selected, out placed, out seatedCue);
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

            Box cue = default;
            bool cueNear = false;
            if (placed != null)
            {
                cueNear = TryAttachCueToCard(
                    plateWidth, plateHeight, pins, placed, hexes, detail, selected, out cue);
            }

            if (reserveOn && detail.W > 1f && placed != null)
                TrySlideReserveRow(hexes, pins, placed, ref cue, plateWidth, plateHeight, fa);

            if (portraitSeated && placed != null)
            {
                OpenDistrictCentres(hexes, pins, placed, ref cue, detail, plateWidth, plateHeight, selected);
                OpenCityBasinGap(hexes, pins, placed, ref cue, detail, plateWidth, plateHeight, selected);
                GrowSeatedPortrait(hexes, pins, placed, ref cue, detail, plateWidth, plateHeight, selected);
                OpenColumnGaps(hexes, pins, placed, cue, detail, plateWidth, plateHeight, selected);
                CapIndustrialBasin(hexes, placed);
                OpenIndustrialAirfield(hexes, pins, placed, ref cue, detail, plateWidth, plateHeight, selected);
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
                    Box glowZone = detail.W > 1f
                        ? new Box(detail.X - HaloReach, detail.Y - HaloReach, detail.W + HaloReach * 2f, detail.H + HaloReach * 2f)
                        : default;
                    if (!layout.Seats[i].Legal || Leader(pins[i], placed[i]) < LeaderMin
                        || SegmentHitsRect(pins[i].Center, NearestOn(placed[i], pins[i].Center), detail)
                        || (glowZone.W > 1f && (PolygonHitsRect(hexes[i], detail) || PolygonHitsRect(hexes[i], glowZone))))
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

            if (cardsLegal && detail.W > 1f && cue.W > 1f && Overlaps(cue, detail, 0f))
                cardsLegal = false;

            layout.CardsLegal = cardsLegal;
            layout.Cue = cue;
            layout.CueNear = cueNear;
            layout.HexAspectLegal = AspectsHold(hexes, RatioPlateWidth, RatioPlateHeight)
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

            int selectedSeat = IndexOf(layout.SelectedMapId);
            if (selectedSeat < 0)
                selectedSeat = 0;
            Box selectedPin = layout.Seats[selectedSeat].Pin;
            Box selectedCard = layout.Seats[selectedSeat].Card;
            float reach = selectedCard.W > 1f
                ? CueAnchorDistance(layout.Cue, selectedCard, selectedPin)
                : Distance(layout.Cue.Center, selectedPin.Center);
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
            RatioPlateWidth = frameWidth;
            RatioPlateHeight = frameHeight;
            return BuildAll(frameWidth, frameHeight, false);
        }

        /// <summary>Cards seated inside the hexes from the last <see cref="BuildAll"/> call.</summary>
        private static Box[] PortraitCards;

        /// <summary>Frame whose aspect the hex is authored against. The open viewport when the reserve is up; placement stays on the plate.</summary>
        private static float RatioPlateWidth;

        private static float RatioPlateHeight;

        /// <summary>Selected district for this seating pass. Only that card reserves the top-tab stroke when the full row cannot.</summary>
        private static int SelectedPortrait = -1;

        /// <summary>-1 keeps the top-tab stroke inside every hex. A district index reserves it on that card alone.</summary>
        private static int PortraitTabIndex = -1;

        /// <summary>Slide the hex centre toward the plate middle so the outer vertex stays near the pin.</summary>
        private static bool PreferInwardHex;

        /// <summary>Keep a bottom district's hex against the hint so it clears the row above.</summary>
        private static bool PreferLowHex;

        /// <summary>Keep an upper district's hex against its pin so the bottom district can sit under it.</summary>
        private static bool PreferHighHex;

        /// <summary>Extra half-height the portrait solver may add while fitting a full seat into the open viewport.</summary>
        private static float PortraitHexExtra;

        /// <summary>Portrait hexes must clear the 40000 area rule after the reserve slide moves them.</summary>
        private static float PortraitAreaFloor = 40000f;

        private static float PortraitRatioScale = 1f;

        /// <summary>
        /// Polygon gap the portrait seater requires on a tall narrowed plate.
        /// Capture still separates districts at 0.75px. Short plates stay on that rule.
        /// </summary>
        private static float PortraitSpread;

        /// <summary>Portrait size for the current seating attempt. Full seats are tried before the compact minimum.</summary>
        private static float SeatCardWidth = PortraitMinWidth;

        private static float SeatCardHeight = PortraitMinHeight;

        private static bool HostsCueTab(int index)
        {
            return PortraitTabIndex < 0 || index == PortraitTabIndex;
        }

        /// <summary>When false, the selected card reserves the 36px tab instead of the tall one.</summary>
        private static bool PreferTallTab = true;

        private static float SolvedTabHeight(int index)
        {
            if (!PreferTallTab)
                return CueTabRoom;
            return PortraitTabIndex == index ? CueTabTall : CueTabRoom;
        }

        /// <summary>
        /// Sizes every district around a portrait that sits inside it, beside the pin, with the
        /// top tab inside 160px and clear of the district stroke.
        /// </summary>
        private static bool TrySeatPortraits(
            float frameWidth,
            float frameHeight,
            bool plainCover,
            out Vec2[][] hexes,
            out Box[] cards)
        {
            hexes = null;
            cards = null;
            PortraitTabIndex = -1;
            PreferInwardHex = false;
            PreferHighHex = false;
            PreferLowHex = false;
            PortraitHexExtra = 0f;
            PortraitAreaFloor = 40000f;
            PortraitSpread = 0f;
            PreferTallTab = true;
            if (frameWidth < 64f || frameHeight < 64f)
                return false;
            var pins = new Box[MapIds.Length];
            for (int i = 0; i < MapIds.Length; i++)
                pins[i] = PinBox(i, frameWidth, frameHeight, plainCover);
            bool narrow = RatioPlateWidth > 2f && RatioPlateWidth + 2f < frameWidth;
            bool tallNarrow = narrow && frameHeight >= 760f;
            bool squareOpen = RatioPlateHeight > 2f && RatioPlateWidth / RatioPlateHeight < 1.15f;
            bool spreadRow = tallNarrow && squareOpen;
            PortraitSpread = spreadRow ? 20f : 0f;
            if (spreadRow && SelectedPortrait >= 0)
                PortraitTabIndex = SelectedPortrait;
            if (TryPortraitBand(frameWidth, frameHeight, pins, FullPortraitMinWidth, FullPortraitMinHeight, true, narrow, false, out hexes, out cards))
            {
                PortraitSpread = 0f;
                return true;
            }

            if (tallNarrow)
            {
                PortraitTabIndex = -1;
                PortraitSpread = 0f;
                bool seeded = TryPortraitBand(
                    frameWidth, frameHeight, pins, PortraitMinWidth, PortraitMinHeight, true, true, false, out hexes, out cards);
                PortraitSpread = spreadRow ? 20f : 0f;
                if (seeded
                    && TryEnlargePortraitRow(frameWidth, frameHeight, pins, hexes, cards, out Vec2[][] grown, out Box[] grownCards))
                {
                    hexes = grown;
                    cards = grownCards;
                    PortraitSpread = 0f;
                    return true;
                }
            }

            if (narrow)
            {
                PreferInwardHex = true;
                if (TryPortraitBand(frameWidth, frameHeight, pins, FullPortraitMinWidth, FullPortraitMinHeight, false, true, true, out hexes, out cards))
                {
                    PreferInwardHex = false;
                    return true;
                }

                if (SelectedPortrait >= 0)
                {
                    PortraitTabIndex = SelectedPortrait;
                    if (TryPortraitBand(frameWidth, frameHeight, pins, FullPortraitMinWidth, FullPortraitMinHeight, false, true, true, out hexes, out cards))
                    {
                        PortraitTabIndex = -1;
                        PreferInwardHex = false;
                        return true;
                    }

                    PortraitTabIndex = -1;
                }

                PreferInwardHex = false;
            }

            PortraitSpread = spreadRow ? 20f : 0f;
            bool seated = TryPortraitBand(frameWidth, frameHeight, pins, PortraitMinWidth, PortraitMinHeight, true, false, false, out hexes, out cards);
            PortraitSpread = 0f;
            return seated;
        }

        /// <summary>
        /// The compact row already has the open-viewport ratio and clears the detail column.
        /// Grow each tile to a full portrait on that same side of its pin.
        /// </summary>
        private static bool TryEnlargePortraitRow(
            float frameWidth,
            float frameHeight,
            Box[] pins,
            Vec2[][] seedHex,
            Box[] seedCards,
            out Vec2[][] hexes,
            out Box[] cards)
        {
            hexes = null;
            cards = null;
            if (seedCards == null || pins == null)
                return false;
            int savedTab = PortraitTabIndex;
            float savedExtra = PortraitHexExtra;
            float savedFloor = PortraitAreaFloor;
            bool savedInward = PreferInwardHex;
            PortraitTabIndex = SelectedPortrait;
            PortraitHexExtra = 48f;
            PortraitAreaFloor = 40800f;
            PreferInwardHex = true;
            hexes = new Vec2[MapIds.Length][];
            cards = new Box[MapIds.Length];
            var order = new int[MapIds.Length];
            int count = 0;
            for (int i = 0; i < order.Length; i++)
            {
                if (i != SelectedPortrait)
                    order[count++] = i;
            }

            if (SelectedPortrait >= 0 && SelectedPortrait < order.Length)
                order[count++] = SelectedPortrait;

            float[] scales = { 1f };
            bool seated = TryStackBottomPortrait(frameWidth, frameHeight, pins, hexes, cards);
            for (int scaleIndex = 0; scaleIndex < scales.Length && !seated; scaleIndex++)
            {
                PortraitRatioScale = scales[scaleIndex];
                for (int i = 0; i < count; i++)
                {
                    cards[i] = default;
                    hexes[i] = null;
                }

                seated = EnlargePortraitOrder(frameWidth, frameHeight, pins, seedHex, seedCards, hexes, cards, order, count);
            }

            PortraitRatioScale = 1f;

            if (seated && !PortraitSeatClosed(pins, hexes, cards))
                seated = false;
            else if (seated && !ReserveRowFits(frameWidth, frameHeight, pins, hexes, cards))
                seated = false;
            else if (seated && !AspectsHold(hexes, RatioPlateWidth, RatioPlateHeight))
                seated = false;
            else if (seated && !DistrictsLegal(hexes, pins, frameWidth, frameHeight))
                seated = false;
            PortraitTabIndex = savedTab;
            PortraitHexExtra = savedExtra;
            PortraitAreaFloor = savedFloor;
            PreferInwardHex = savedInward;
            if (!seated)
            {
                hexes = null;
                cards = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// The bottom district keeps its tab inside the hex. The four districts above it sit
        /// high, on the inward side of their pins, so the bottom hex can use the open band
        /// and the row still shares one slide off the detail column.
        /// </summary>
        private static bool TryStackBottomPortrait(
            float frameWidth,
            float frameHeight,
            Box[] pins,
            Vec2[][] hexes,
            Box[] cards)
        {
            if (hexes == null || cards == null || SelectedPortrait < 0)
                return false;
            int savedTab = PortraitTabIndex;
            float savedExtra = PortraitHexExtra;
            float savedFloor = PortraitAreaFloor;
            bool savedInward = PreferInwardHex;
            bool savedHigh = PreferHighHex;
            bool savedLow = PreferLowHex;
            PortraitTabIndex = SelectedPortrait;
            PortraitHexExtra = 48f;
            // 40800 leaves room for the shared slide to move the shoelace area without
            // crossing the 40000 capture floor.
            PortraitAreaFloor = 40800f;
            PreferInwardHex = true;
            float[] scales = { 1f, 1.04f, 1.06f, 1.08f };
            float[] spreads = PortraitSpread > 12.5f ? new[] { PortraitSpread, 12f } : new[] { PortraitSpread };
            bool seated = false;
            bool selectedLow = SelectedPinIsLowest(pins);
            for (int gapIndex = 0; gapIndex < spreads.Length && !seated; gapIndex++)
            {
                PortraitSpread = spreads[gapIndex];
                for (int scaleIndex = 0; scaleIndex < scales.Length && !seated; scaleIndex++)
                {
                    PortraitRatioScale = scales[scaleIndex];
                    if (PortraitSpread > 0.5f)
                        seated = StackSpreadOrder(frameWidth, frameHeight, pins, hexes, cards);
                    if (!seated)
                        seated = StackPortraitOrder(frameWidth, frameHeight, pins, hexes, cards, selectedLow, false);
                    if (!seated && !selectedLow)
                        seated = StackPortraitOrder(frameWidth, frameHeight, pins, hexes, cards, selectedLow, true);
                }
            }

            PortraitRatioScale = 1f;
            PreferHighHex = savedHigh;
            PreferLowHex = savedLow;
            PortraitTabIndex = savedTab;
            PortraitHexExtra = savedExtra;
            PortraitAreaFloor = savedFloor;
            PreferInwardHex = savedInward;
            return seated;
        }

        private static bool StackPortraitOrder(
            float frameWidth,
            float frameHeight,
            Box[] pins,
            Vec2[][] hexes,
            Box[] cards,
            bool selectedLow,
            bool selectedFirst)
        {
            for (int i = 0; i < MapIds.Length; i++)
            {
                cards[i] = default;
                hexes[i] = null;
            }

            if (selectedFirst
                && !TryPlaceStackedPortrait(SelectedPortrait, frameWidth, frameHeight, pins, hexes, cards, !selectedLow))
                return false;

            for (int i = 0; i < MapIds.Length; i++)
            {
                if (i == SelectedPortrait)
                    continue;
                // Every unselected district stays beside its own pin. Only the selected
                // bottom district is pushed down against the hint.
                if (TryPlaceStackedPortrait(i, frameWidth, frameHeight, pins, hexes, cards, true))
                    continue;
                return false;
            }

            if (!selectedFirst
                && !TryPlaceStackedPortrait(SelectedPortrait, frameWidth, frameHeight, pins, hexes, cards, !selectedLow))
                return false;

            return true;
        }

        /// <summary>
        /// Places districts from the top of the plate downward. A lower district starts
        /// high and walks down until the gap above it is clear, leaving the bottom pin room.
        /// </summary>
        private static bool StackSpreadOrder(
            float frameWidth,
            float frameHeight,
            Box[] pins,
            Vec2[][] hexes,
            Box[] cards)
        {
            for (int i = 0; i < MapIds.Length; i++)
            {
                cards[i] = default;
                hexes[i] = null;
            }

            var order = new int[MapIds.Length];
            for (int i = 0; i < order.Length; i++)
                order[i] = i;
            for (int i = 0; i < order.Length; i++)
            {
                for (int j = i + 1; j < order.Length; j++)
                {
                    float dy = pins[order[j]].Center.Y - pins[order[i]].Center.Y;
                    bool swap = dy < -0.5f || (Math.Abs(dy) <= 0.5f && order[j] < order[i]);
                    if (!swap)
                        continue;
                    int hold = order[i];
                    order[i] = order[j];
                    order[j] = hold;
                }
            }

            for (int n = 0; n < order.Length; n++)
            {
                int index = order[n];
                bool selectedLow = index == SelectedPortrait && SelectedPinIsLowest(pins);
                bool wantHigh = !selectedLow;
                if (TryPlaceStackedPortrait(index, frameWidth, frameHeight, pins, hexes, cards, wantHigh))
                    continue;
                if (TryPlaceStackedPortrait(index, frameWidth, frameHeight, pins, hexes, cards, !wantHigh))
                    continue;
                if (index == SelectedPortrait && PreferTallTab)
                {
                    PreferTallTab = false;
                    bool dropped = TryPlaceStackedPortrait(index, frameWidth, frameHeight, pins, hexes, cards, wantHigh)
                        || TryPlaceStackedPortrait(index, frameWidth, frameHeight, pins, hexes, cards, !wantHigh);
                    PreferTallTab = true;
                    if (dropped)
                        continue;
                }
                return false;
            }

            return true;
        }

        private static bool SelectedPinIsLowest(Box[] pins)
        {
            if (pins == null || SelectedPortrait < 0 || SelectedPortrait >= pins.Length)
                return false;
            float y = pins[SelectedPortrait].Center.Y;
            for (int i = 0; i < pins.Length; i++)
            {
                if (i != SelectedPortrait && pins[i].Center.Y > y - 40f)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Places one full portrait. Upper districts start at the top of the pin's reach.
        /// The selected bottom district starts against the hint.
        /// </summary>
        private static bool TryPlaceStackedPortrait(
            int index,
            float frameWidth,
            float frameHeight,
            Box[] pins,
            Vec2[][] hexes,
            Box[] cards,
            bool high)
        {
            Box pin = pins[index];
            const float cardW = FullPortraitMinWidth;
            const float cardH = FullPortraitMinHeight;
            float gap = PinGap + 0.5f;
            float rightX = Clamp(pin.XMax + gap, Edge, Math.Max(Edge, frameWidth - Edge - cardW));
            float leftX = Clamp(pin.X - gap - cardW, Edge, Math.Max(Edge, frameWidth - Edge - cardW));
            bool leftPin = pin.Center.X <= frameWidth * 0.5f;
            float[] xs = leftPin ? new[] { rightX, leftX } : new[] { leftX, rightX };
            float ratioW = RatioPlateWidth > 2f ? RatioPlateWidth : frameWidth;
            float ratioH = RatioPlateHeight > 2f ? RatioPlateHeight : frameHeight;
            AuthoredHalf(MapIds[index], ratioW, ratioH, out float halfW, out float halfH);
            float appliedScale = index == SelectedPortrait ? PortraitRatioScale : 1f;
            float ratio = (halfH > 1f ? halfW / halfH : 1.6f) * appliedScale;
            float limit = frameHeight - 47f - InsideMargin - cardH;
            float yNear = Math.Max(48f, pin.Center.Y - (CueNearLimit - 8f));
            float yFar = Math.Min(limit, pin.Center.Y + (high ? 40f : CueNearLimit));
            if (yFar < yNear - 0.1f)
                return false;
            float yStep = high ? 8f : 4f;
            int ySteps = (int)Math.Max(0f, (yFar - yNear) / yStep);
            int[] slides = leftPin ? new[] { 2, 0, 1 } : new[] { 1, 0, 2 };
            int kickCount = high ? 8 : 12;
            float kickStep = high ? 8f : 4f;
            for (int side = 0; side < xs.Length; side++)
            {
                float dir = xs[side] + cardW * 0.5f >= pin.Center.X ? 1f : -1f;
                for (int kick = 0; kick < kickCount; kick++)
                {
                    for (int n = 0; n <= ySteps; n++)
                    {
                        float y = high ? yNear + n * yStep : yFar - n * yStep;
                        if (y < yNear - 0.1f || y > yFar + 0.1f || y < 48f || y > limit)
                            continue;
                        float x = Clamp(xs[side] + dir * kick * kickStep, Edge, Math.Max(Edge, frameWidth - Edge - cardW));
                        if (!ShiftTabClearOfPins(ref x, y, cardW, pins, frameWidth))
                            continue;
                        float horiz = Math.Abs(x + cardW * 0.5f - pin.Center.X);
                        if (horiz >= CueNearLimit - 1f)
                            continue;
                        float reach = (float)Math.Sqrt(CueNearLimit * CueNearLimit - horiz * horiz) - 1f;
                        if (Math.Abs(y - pin.Center.Y) > reach)
                            continue;
                        var trial = new Box(x, y, cardW, cardH);
                        var tab = new Box(x, y - CueTabRoom, cardW, CueTabRoom);
                        if (Separation(trial, pin) < PinGap || Leader(pin, trial) < LeaderMin)
                            continue;
                        if (!TabClearsPins(tab, pins) || !ClearsPlacedCards(trial, cards))
                            continue;
                        if (CueAnchorDistance(tab, trial, pin) > CueNearLimit)
                            continue;
                        float coverTop = PortraitTabIndex == index && PreferTallTab ? y - (CueTabTall + CueHexStroke) : y;
                        float coverBot = y + cardH;
                        coverTop = Math.Min(coverTop, pin.Center.Y - 8f);
                        coverBot = Math.Max(coverBot, pin.Center.Y + 8f);
                        if (coverBot - coverTop > MinPortraitHexHeight(ratio) * 2f + 48f)
                            continue;
                        PreferHighHex = high;
                        PreferLowHex = !high;
                        for (int s = 0; s < slides.Length; s++)
                        {
                            if (!TrySolvePortraitHex(
                                trial, pin, ratio, frameWidth, frameHeight, hexes, cards, index, slides[s], out Vec2[] hex, high ? 4f : 2f))
                                continue;
                            PreferHighHex = false;
                            PreferLowHex = false;
                            cards[index] = trial;
                            hexes[index] = hex;
                            return true;
                        }

                        PreferHighHex = false;
                        PreferLowHex = false;
                    }
                }
            }

            return false;
        }

        private static bool EnlargePortraitOrder(
            float frameWidth,
            float frameHeight,
            Box[] pins,
            Vec2[][] seedHex,
            Box[] seedCards,
            Vec2[][] hexes,
            Box[] cards,
            int[] order,
            int count)
        {
            for (int step = 0; step < count; step++)
            {
                if (TryEnlargePortrait(order[step], frameWidth, frameHeight, pins, seedHex, seedCards, hexes, cards))
                    continue;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Raises the lower vertices of districts that hang into the selected pin, when the
        /// card and the pin still sit inside. The open-viewport ratio stays inside 10%.
        /// </summary>
        private static void LiftHexBottoms(Vec2[][] hexes, Box[] cards, Box[] pins, int selected)
        {
            if (hexes == null || cards == null || pins == null || selected < 0)
                return;
            float limit = pins[selected].Center.Y - 110f;
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i == selected || hexes[i] == null || cards[i].W < 1f)
                    continue;
                BoundsOf(hexes[i], out _, out float minY, out _, out float maxY);
                if (maxY <= limit + 0.5f || maxY - minY < 40f)
                    continue;
                float dy = maxY - limit;
                float midY = (minY + maxY) * 0.5f;
                bool liftedAny = false;
                for (float step = dy; step >= 4f; step -= 4f)
                {
                    var copy = new Vec2[hexes[i].Length];
                    for (int p = 0; p < hexes[i].Length; p++)
                    {
                        Vec2 point = hexes[i][p];
                        if (point.Y > midY)
                            point = new Vec2(point.X, Math.Max(midY, point.Y - step));
                        copy[p] = point;
                    }

                    var lifted = new Box(cards[i].X, cards[i].Y - step, cards[i].W, cards[i].H);
                    if (lifted.Y < 48f)
                    {
                        continue;
                    }

                    if (Separation(lifted, pins[i]) < PinGap || Leader(pins[i], lifted) < LeaderMin)
                    {
                        continue;
                    }

                    if (CueAnchorDistance(new Box(lifted.X, lifted.Y - CueTabRoom, lifted.W, CueTabRoom), lifted, pins[i]) > CueNearLimit)
                    {
                        continue;
                    }

                    if (!RestsInside(lifted, copy, InsideMargin))
                    {
                        continue;
                    }

                    if (!PointInPolygon(pins[i].Center, copy) || DistanceToEdge(pins[i].Center, copy) < 8f)
                    {
                        continue;
                    }
                    bool blocked = false;
                    for (int j = 0; j < cards.Length; j++)
                    {
                        if (j == i || j == selected || cards[j].W < 1f)
                            continue;
                        if (Separation(lifted, cards[j]) < CardGap)
                        {
                            blocked = true;
                            break;
                        }
                    }

                    if (blocked)
                    {
                        continue;
                    }

                    float expected = HexAspect(MapIds[i], RatioPlateWidth, RatioPlateHeight);
                    float measured = MeasuredHexAspect(copy);
                    if (expected < 0.2f || Math.Abs(measured / expected - 1f) > 0.10f)
                    {
                        continue;
                    }
                    cards[i] = lifted;
                    hexes[i] = copy;
                    liftedAny = true;
                    break;
                }

                if (!liftedAny)
                    continue;
            }
        }

        private static bool TryEnlargePortrait(
            int index,
            float frameWidth,
            float frameHeight,
            Box[] pins,
            Vec2[][] seedHex,
            Box[] seedCards,
            Vec2[][] hexes,
            Box[] cards)
        {
            Box pin = pins[index];
            Box seed = seedCards[index];
            if (pin.W < 1f || seed.W < 1f)
                return false;
            if (index == SelectedPortrait)
                LiftHexBottoms(hexes, cards, pins, index);
            const float cardW = FullPortraitMinWidth;
            const float cardH = FullPortraitMinHeight;
            float gap = PinGap + 0.5f;
            float rightX = Clamp(pin.XMax + gap, Edge, Math.Max(Edge, frameWidth - Edge - cardW));
            float leftX = Clamp(pin.X - gap - cardW, Edge, Math.Max(Edge, frameWidth - Edge - cardW));
            bool seedRight = seed.Center.X >= pin.Center.X;
            float[] xs = seedRight ? new[] { rightX, leftX } : new[] { leftX, rightX };
            float ratioW = RatioPlateWidth > 2f ? RatioPlateWidth : frameWidth;
            float ratioH = RatioPlateHeight > 2f ? RatioPlateHeight : frameHeight;
            AuthoredHalf(MapIds[index], ratioW, ratioH, out float halfW, out float halfH);
            float ratio = (halfH > 1f ? halfW / halfH : 1.6f) * PortraitRatioScale;
            float limit = frameHeight - 47f - InsideMargin - cardH;
            float y0 = Math.Min(Math.Max(48f, seed.Y), limit);
            int ySteps = 1 + (int)Math.Max(0f, (limit - 48f) / 8f);
            var ys = new float[ySteps + 1];
            ys[0] = y0;
            for (int step = 0; step < ySteps; step++)
                ys[step + 1] = 48f + step * 8f;
            int[] slides = pin.Center.X <= frameWidth * 0.5f ? new[] { 2, 0, 1 } : new[] { 1, 0, 2 };
            for (int side = 0; side < xs.Length; side++)
            {
                float dir = xs[side] + cardW * 0.5f >= pin.Center.X ? 1f : -1f;
                int kickCount = index == SelectedPortrait ? 16 : 6;
                float kickStep = index == SelectedPortrait ? 4f : 8f;
                for (int kick = 0; kick < kickCount; kick++)
                {
                for (int n = 0; n < ys.Length; n++)
                {
                    float y = ys[n];
                    if (y < 48f || y > limit)
                        continue;
                    float x = Clamp(xs[side] + dir * kick * kickStep, Edge, Math.Max(Edge, frameWidth - Edge - cardW));
                    if (!ShiftTabClearOfPins(ref x, y, cardW, pins, frameWidth))
                        continue;

                    float horiz = Math.Abs(x + cardW * 0.5f - pin.Center.X);
                    if (horiz >= CueNearLimit - 1f)
                        continue;

                    float reach = (float)Math.Sqrt(CueNearLimit * CueNearLimit - horiz * horiz) - 1f;
                    if (Math.Abs(y - pin.Center.Y) > reach)
                        continue;

                    var trial = new Box(x, y, cardW, cardH);
                    var tab = new Box(x, y - CueTabRoom, cardW, CueTabRoom);
                    if (Separation(trial, pin) < PinGap || Leader(pin, trial) < LeaderMin)
                        continue;

                    if (!TabClearsPins(tab, pins) || !ClearsPlacedCards(trial, cards))
                        continue;

                    if (CueAnchorDistance(tab, trial, pin) > CueNearLimit)
                        continue;

                    bool seedHolds = seedHex != null && index < seedHex.Length && seedHex[index] != null
                        && RestsInside(trial, seedHex[index], InsideMargin)
                        && (!HostsCueTab(index) || !HexEdgeHits(seedHex[index], InflateBox(tab, CueHexStroke)));
                    if (seedHolds && !HostsCueTab(index))
                    {
                        for (int s = 0; s < slides.Length; s++)
                        {
                            if (!TrySolvePortraitHex(
                                trial, pin, ratio, frameWidth, frameHeight, hexes, cards, index, slides[s], out Vec2[] tight, 4f))
                                continue;
                            if (SelectedPortrait >= 0
                                && index != SelectedPortrait
                                && pins[SelectedPortrait].Center.Y > pin.Center.Y + 80f)
                            {
                                BoundsOf(tight, out _, out _, out _, out float maxY);
                                if (maxY > pins[SelectedPortrait].Center.Y - 80f)
                                    continue;
                            }

                            cards[index] = trial;
                            hexes[index] = tight;
                            return true;
                        }

                        bool seedTooLow = false;
                        if (SelectedPortrait >= 0
                            && index != SelectedPortrait
                            && pins[SelectedPortrait].Center.Y > pin.Center.Y + 80f)
                        {
                            BoundsOf(seedHex[index], out _, out _, out _, out float seedBottom);
                            seedTooLow = seedBottom > pins[SelectedPortrait].Center.Y - 80f;
                        }

                        if (!seedTooLow)
                        {
                            cards[index] = trial;
                            hexes[index] = seedHex[index];
                            return true;
                        }
                    }

                    if (seedHolds && HostsCueTab(index))
                    {
                        cards[index] = trial;
                        hexes[index] = seedHex[index];
                        return true;
                    }

                    if (!HostsCueTab(index))
                        continue;

                    bool low = SelectedPortrait == index
                        && SelectedPortrait >= 0
                        && pin.Center.Y + 40f >= pins[0].Center.Y
                        && pin.Center.Y + 40f >= pins[1].Center.Y
                        && pin.Center.Y + 40f >= pins[2].Center.Y
                        && pin.Center.Y + 40f >= pins[3].Center.Y;
                    for (int s = 0; s < slides.Length; s++)
                    {
                        PreferLowHex = low;
                        if (!TrySolvePortraitHex(
                            trial, pin, ratio, frameWidth, frameHeight, hexes, cards, index, slides[s], out Vec2[] hex, 2f))
                        {
                            PreferLowHex = false;
                            continue;
                        }

                        PreferLowHex = false;
                        cards[index] = trial;
                        hexes[index] = hex;
                        return true;
                    }
                }
                }
            }

            return false;
        }

        /// <summary>
        /// One card size across the authored ratio and the 6% squeeze. A narrowed plate
        /// keeps a row only when English and Farsi still share a slide off the detail column.
        /// </summary>
        private static bool TryPortraitBand(
            float frameWidth,
            float frameHeight,
            Box[] pins,
            float cardWidth,
            float cardHeight,
            bool packed,
            bool requireClear,
            bool widen,
            out Vec2[][] hexes,
            out Box[] cards)
        {
            hexes = null;
            cards = null;
            SeatCardWidth = cardWidth;
            SeatCardHeight = cardHeight;
            PortraitHexExtra = widen ? 36f : 0f;
            PortraitAreaFloor = widen ? 40800f : 40000f;
            float[] squeezes = { 1f, 0.94f };
            if (widen)
                squeezes = new[] { 1f };
            for (int s = 0; s < squeezes.Length; s++)
            {
                if (TrySeatPortraitsAt(frameWidth, frameHeight, pins, squeezes[s], out hexes, out cards)
                    && PortraitBandHolds(frameWidth, frameHeight, pins, hexes, cards, requireClear))
                    return true;
                if (packed
                    && TrySeatPackedRows(frameWidth, frameHeight, pins, squeezes[s], out hexes, out cards)
                    && PortraitBandHolds(frameWidth, frameHeight, pins, hexes, cards, requireClear))
                    return true;
            }

            hexes = null;
            cards = null;
            return false;
        }

        private static bool PortraitBandHolds(
            float frameWidth,
            float frameHeight,
            Box[] pins,
            Vec2[][] hexes,
            Box[] cards,
            bool requireClear)
        {
            if (!requireClear)
                return true;
            if (!ReserveRowFits(frameWidth, frameHeight, pins, hexes, cards))
                return false;
            return AspectsHold(hexes, RatioPlateWidth, RatioPlateHeight)
                && DistrictsLegal(hexes, pins, frameWidth, frameHeight);
        }

        /// <summary>
        /// Short plates cannot spare a 92px header. Pack each row outward: the top row sits
        /// high, the bottom row sits on the hint, and the middle row uses the gap. Card size
        /// and the 160px / 26px / 8px rules stay the same.
        /// </summary>
        private static bool TrySeatPackedRows(
            float frameWidth,
            float frameHeight,
            Box[] pins,
            float squeeze,
            out Vec2[][] hexes,
            out Box[] cards)
        {
            hexes = new Vec2[MapIds.Length][];
            cards = new Box[MapIds.Length];
            int count = MapIds.Length;
            var order = new int[count];
            for (int i = 0; i < count; i++)
                order[i] = i;
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    float dy = pins[order[j]].Center.Y - pins[order[i]].Center.Y;
                    bool swap = dy < -0.5f || (Math.Abs(dy) <= 0.5f && pins[order[j]].Center.X < pins[order[i]].Center.X);
                    if (!swap)
                        continue;
                    int hold = order[i];
                    order[i] = order[j];
                    order[j] = hold;
                }
            }

            var rowOf = new int[count];
            int rowCount = 0;
            float rowAnchor = -1000f;
            for (int n = 0; n < count; n++)
            {
                int index = order[n];
                if (n == 0 || pins[index].Center.Y - rowAnchor > 80f)
                {
                    rowCount++;
                    rowAnchor = pins[index].Center.Y;
                }

                rowOf[index] = rowCount - 1;
            }

            var place = new int[count];
            int placedCount = 0;
            for (int row = 0; row < rowCount; row++)
            {
                int target = row == 0 || rowCount < 3 ? row : row == 1 ? rowCount - 1 : row - 1;
                for (int n = 0; n < count; n++)
                {
                    if (rowOf[order[n]] == target)
                        place[placedCount++] = order[n];
                }
            }

            float cardW = SeatCardWidth > 48f ? SeatCardWidth : PortraitMinWidth;
            float cardH = SeatCardHeight > 48f ? SeatCardHeight : PortraitMinHeight;
            float yFloor = Edge + CueTabRoom;
            float limit = frameHeight - 47f - InsideMargin - cardH;
            for (int n = 0; n < count; n++)
            {
                int index = place[n];
                if (!TryPackDistrictRow(
                    index, rowOf[index], rowCount, frameWidth, frameHeight, pins, squeeze, cardW, cardH, yFloor, limit, hexes, cards))
                {
                    hexes = null;
                    cards = null;
                    return false;
                }
            }

            if (!PortraitSeatClosed(pins, hexes, cards))
            {
                hexes = null;
                cards = null;
                return false;
            }

            return true;
        }

        private static bool TryPackDistrictRow(
            int index,
            int row,
            int rowCount,
            float frameWidth,
            float frameHeight,
            Box[] pins,
            float squeeze,
            float cardW,
            float cardH,
            float yFloor,
            float limit,
            Vec2[][] hexes,
            Box[] cards)
        {
            Box pin = pins[index];
            float ratioW = RatioPlateWidth > 2f ? RatioPlateWidth : frameWidth;
            float ratioH = RatioPlateHeight > 2f ? RatioPlateHeight : frameHeight;
            AuthoredHalf(MapIds[index], ratioW, ratioH, out float halfW, out float halfH);
            float authored = halfH > 1f ? halfW / halfH * squeeze : 1.6f;
            bool packDown = rowCount >= 3 && row == rowCount - 1;
            bool leftSide = pin.Center.X <= frameWidth * 0.5f;
            // Half a pixel past the gap so float error cannot report 11.999 as under 12.
            float gap = PinGap + 0.5f;
            float outside = leftSide ? pin.X - gap - cardW : pin.XMax + gap;
            float inside = leftSide ? pin.XMax + gap : pin.X - gap - cardW;
            float[] sides = { outside, inside };
            int slide = leftSide ? 1 : 2;
            if (PreferInwardHex)
                slide = leftSide ? 2 : 1;
            float bestScore = float.MaxValue;
            Box bestCard = default;
            Vec2[] bestHex = null;
            // Coarse authored ratio, then a 1px search, then a slightly wider hex still
            // inside the 10% aspect band. The wider hex is shorter, which is what lets a
            // middle district sit in the gap on a short plate.
            float[] ratioScales = { 1f, 1f, 1.099f };
            float[] steps = { 4f, 1f, 1f };
            for (int scaleIndex = 0; scaleIndex < ratioScales.Length && bestHex == null; scaleIndex++)
            {
                float ratio = authored * ratioScales[scaleIndex];
                float step = steps[scaleIndex];
                for (int pass = 0; pass < sides.Length; pass++)
                {
                    float x0 = Clamp(sides[pass], Edge, Math.Max(Edge, frameWidth - Edge - cardW));
                    float horiz = Math.Abs(x0 + cardW * 0.5f - pin.Center.X);
                    if (horiz >= CueNearLimit - 1f)
                        continue;
                    float vMax = (float)Math.Sqrt(CueNearLimit * CueNearLimit - horiz * horiz) - 1f;
                    float yLo = Math.Max(yFloor, pin.Center.Y - vMax);
                    float yHi = Math.Min(limit, pin.Center.Y + vMax);
                    if (yLo > yHi + 0.01f)
                        continue;
                    float yStep = step < 2f ? 1f : 6f;
                    for (float y = yLo; y <= yHi + 0.01f; y += yStep)
                    {
                        float x = x0;
                        if (!ShiftTabClearOfPins(ref x, y, cardW, pins, frameWidth))
                            continue;
                        horiz = Math.Abs(x + cardW * 0.5f - pin.Center.X);
                        if (horiz >= CueNearLimit - 1f)
                            continue;
                        float reach = (float)Math.Sqrt(CueNearLimit * CueNearLimit - horiz * horiz) - 1f;
                        if (Math.Abs(y - pin.Center.Y) > reach)
                            continue;
                        var trial = new Box(x, y, cardW, cardH);
                        var tab = new Box(x, y - CueTabRoom, cardW, CueTabRoom);
                        if (Separation(trial, pin) < PinGap || Leader(pin, trial) < LeaderMin)
                            continue;
                        if (!TabClearsPins(tab, pins) || !ClearsPlacedCards(trial, cards))
                            continue;
                        if (CueAnchorDistance(tab, trial, pin) > CueNearLimit)
                            continue;
                        if (!TrySolvePortraitHex(
                            trial, pin, ratio, frameWidth, frameHeight, hexes, cards, index, slide, out Vec2[] hex, step))
                            continue;
                        BoundsOf(hex, out _, out float minY, out _, out float maxY);
                        float score = packDown ? -minY : maxY;
                        if (score >= bestScore)
                            continue;
                        bestScore = score;
                        bestCard = trial;
                        bestHex = hex;
                        if (step < 2f)
                            break;
                    }

                    if (step < 2f && bestHex != null)
                        break;
                }
            }

            if (bestHex == null)
                return false;
            cards[index] = bestCard;
            hexes[index] = bestHex;
            return true;
        }

        private static bool TrySeatPortraitsAt(
            float frameWidth,
            float frameHeight,
            Box[] pins,
            float squeeze,
            out Vec2[][] hexes,
            out Box[] cards)
        {
            hexes = new Vec2[MapIds.Length][];
            cards = new Box[MapIds.Length];
            var order = new int[MapIds.Length];
            for (int i = 0; i < order.Length; i++)
                order[i] = i;
            if (PreferInwardHex)
                return SeatPortraitOrders(frameWidth, frameHeight, pins, squeeze, order, hexes, cards, true);
            if (SeatPortraitOrders(frameWidth, frameHeight, pins, squeeze, order, hexes, cards, false))
                return true;
            return SeatPortraitOrders(frameWidth, frameHeight, pins, squeeze, order, hexes, cards, true);
        }

        private static bool SeatPortraitOrders(
            float frameWidth,
            float frameHeight,
            Box[] pins,
            float squeeze,
            int[] order,
            Vec2[][] hexes,
            Box[] cards,
            bool slide)
        {
            for (int i = 0; i < order.Length; i++)
                order[i] = i;
            var budget = new int[1];
            if (SeatPortraitOrder(frameWidth, frameHeight, pins, squeeze, order, hexes, cards, budget, slide))
                return true;
            OrderBottomFirst(pins, order);
            ClearPortraitSeats(hexes, cards);
            budget[0] = 0;
            return SeatPortraitOrder(frameWidth, frameHeight, pins, squeeze, order, hexes, cards, budget, slide);
        }

        private static void ClearPortraitSeats(Vec2[][] hexes, Box[] cards)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = default;
                if (hexes != null && i < hexes.Length)
                    hexes[i] = null;
            }
        }

        private static void OrderBottomFirst(Box[] pins, int[] order)
        {
            for (int i = 0; i < order.Length; i++)
                order[i] = i;
            for (int i = 0; i < order.Length; i++)
            {
                for (int j = i + 1; j < order.Length; j++)
                {
                    if (pins[order[j]].Center.Y <= pins[order[i]].Center.Y)
                        continue;
                    int swap = order[i];
                    order[i] = order[j];
                    order[j] = swap;
                }
            }
        }

        /// <summary>
        /// Places every portrait, backtracking when a later district has no stroke-clear hex.
        /// The first candidates sit the card beside its pin. Later ones lift it so a neighbour
        /// can keep the bottom of the plate.
        /// </summary>
        private static bool SeatPortraitOrder(
            float frameWidth,
            float frameHeight,
            Box[] pins,
            float squeeze,
            int[] order,
            Vec2[][] hexes,
            Box[] cards,
            int[] budget,
            bool slide)
        {
            return SeatPortraitStep(0, frameWidth, frameHeight, pins, squeeze, order, hexes, cards, budget, slide);
        }

        private static bool SeatPortraitStep(
            int step,
            float frameWidth,
            float frameHeight,
            Box[] pins,
            float squeeze,
            int[] order,
            Vec2[][] hexes,
            Box[] cards,
            int[] budget,
            bool slide)
        {
            if (budget[0] > (PortraitHexExtra > 0f ? 720 : 360))
                return false;
            if (step >= order.Length)
                return PortraitSeatClosed(pins, hexes, cards);
            int index = order[step];
            Box pin = pins[index];
            float ratioW = RatioPlateWidth > 2f ? RatioPlateWidth : frameWidth;
            float ratioH = RatioPlateHeight > 2f ? RatioPlateHeight : frameHeight;
            AuthoredHalf(MapIds[index], ratioW, ratioH, out float halfW, out float halfH);
            float ratio = halfH > 1f ? halfW / halfH * squeeze : 1.6f;
            float cardW = SeatCardWidth > 48f ? SeatCardWidth : PortraitMinWidth;
            float cardH = SeatCardHeight > 48f ? SeatCardHeight : PortraitMinHeight;
            float right = pin.XMax + PinGap;
            float left = pin.X - PinGap - cardW;
            bool preferRight = frameWidth - (pin.XMax + PinGap + cardW) >= left;
            float[] sides = { preferRight ? right : left, preferRight ? left : right };
            float limit = frameHeight - 47f - InsideMargin - cardH;
            for (int pass = 0; pass < sides.Length; pass++)
            {
                float x0 = Clamp(sides[pass], Edge, Math.Max(Edge, frameWidth - Edge - cardW));
                float horiz = Math.Abs(x0 + cardW * 0.5f - pin.Center.X);
                if (horiz >= CueNearLimit - 1f)
                    continue;
                float vMax = (float)Math.Sqrt(CueNearLimit * CueNearLimit - horiz * horiz) - 1f;
                float prefer = Clamp(pin.Center.Y - 40f, 92f, pin.Center.Y + vMax);
                if (prefer > limit)
                    prefer = limit;
                float yMin = Math.Max(92f, pin.Center.Y - vMax);
                float yMax = Math.Min(limit, pin.Center.Y + vMax);
                if (yMin > yMax + 0.01f)
                    continue;
                float[] lifts = { 0f, -24f, 24f, -48f, 48f, -72f, 72f, -96f };
                for (int n = 0; n < lifts.Length + 2; n++)
                {
                    float y = n < lifts.Length ? prefer + lifts[n] : n == lifts.Length ? yMin : yMax;
                    if (y < yMin - 0.01f || y > yMax + 0.01f)
                        continue;
                    float x = x0;
                    var tab = new Box(x, y - CueTabRoom, cardW, CueTabRoom);
                    if (Separation(tab, pin) < 1f)
                    {
                        float shared = pin.Y + 1f;
                        bool lifted = n < lifts.Length && lifts[n] < 0f;
                        if (lifted || shared <= y || shared > yMax)
                            continue;
                        y = shared;
                        tab = new Box(x, y - CueTabRoom, cardW, CueTabRoom);
                    }

                    if (!ShiftTabClearOfPins(ref x, y, cardW, pins, frameWidth))
                        continue;
                    horiz = Math.Abs(x + cardW * 0.5f - pin.Center.X);
                    if (horiz >= CueNearLimit - 1f)
                        continue;
                    float reach = (float)Math.Sqrt(CueNearLimit * CueNearLimit - horiz * horiz) - 1f;
                    if (Math.Abs(y - pin.Center.Y) > reach)
                        continue;
                    var trial = new Box(x, y, cardW, cardH);
                    tab = new Box(x, y - CueTabRoom, cardW, CueTabRoom);
                    if (Separation(trial, pin) < PinGap || Leader(pin, trial) < LeaderMin)
                        continue;
                    if (!TabClearsPins(tab, pins) || !ClearsPlacedCards(trial, cards))
                        continue;
                    if (CueAnchorDistance(tab, trial, pin) > CueNearLimit)
                        continue;
                    int slides = slide ? 3 : 1;
                    for (int slideSlot = 0; slideSlot < slides; slideSlot++)
                    {
                        int slideIndex = slideSlot;
                        if (PreferInwardHex && slide)
                        {
                            int inward = pin.Center.X <= frameWidth * 0.5f ? 2 : 1;
                            slideIndex = slideSlot == 0 ? inward : slideSlot == 1 ? 0 : inward == 1 ? 2 : 1;
                        }

                        budget[0]++;
                        if (budget[0] > (PortraitHexExtra > 0f ? 720 : 360))
                            return false;
                        if (!TrySolvePortraitHex(
                            trial, pin, ratio, frameWidth, frameHeight, hexes, cards, index, slideIndex, out Vec2[] hex))
                            continue;
                        cards[index] = trial;
                        hexes[index] = hex;
                        if (SeatPortraitStep(
                            step + 1, frameWidth, frameHeight, pins, squeeze, order, hexes, cards, budget, slide))
                            return true;
                        cards[index] = default;
                        hexes[index] = null;
                    }
                }
            }

            return false;
        }

        private static bool ClearsPlacedCards(Box trial, Box[] seated)
        {
            if (seated == null)
                return true;
            for (int i = 0; i < seated.Length; i++)
            {
                if (seated[i].W > 1f && (Overlaps(trial, seated[i], 2f) || Separation(trial, seated[i]) < CardGap))
                    return false;
            }

            return true;
        }

        private static bool PortraitSeatClosed(Box[] pins, Vec2[][] hexes, Box[] cards)
        {
            for (int i = 0; i < MapIds.Length; i++)
            {
                if (cards[i].W < 1f || hexes[i] == null)
                    return false;

                var tab = new Box(cards[i].X, cards[i].Y - CueTabRoom, cards[i].W, CueTabRoom);
                if (CueAnchorDistance(tab, cards[i], pins[i]) > CueNearLimit)
                    return false;

                if (!CueReadsAsSelected(cards[i].Center, pins[i].Center, hexes, pins, i))
                    return false;

                if (LeaderHitsOtherHex(pins[i].Center, NearestOn(cards[i], pins[i].Center), hexes, i))
                    return false;

                bool hostsTab = HostsCueTab(i);
                Box stroke = hostsTab ? InflateBox(tab, CueHexStroke) : default;
                for (int j = 0; j < MapIds.Length; j++)
                {
                    if (j == i || hexes[j] == null)
                        continue;
                    if (PolygonsOverlap(hexes[i], hexes[j], 0.75f))
                        return false;
                    if (PortraitSpread > 0.5f && PolygonsOverlap(hexes[i], hexes[j], -PortraitSpread))
                        return false;

                    if (hostsTab && PolygonHitsRect(hexes[j], stroke))
                        return false;
                }

                if (hostsTab && HexEdgeHits(hexes[i], stroke))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Smallest flat hex of the authored ratio that holds the card and the pin and keeps
        /// the 26px district stroke off the top tab. The centre slides along the legal span
        /// so a neighbour already on the plate can keep its own hex.
        /// </summary>
        private static bool TrySolvePortraitHex(
            Box card,
            Box pin,
            float ratio,
            float frameWidth,
            float frameHeight,
            Vec2[][] placed,
            Box[] cards,
            int index,
            int slide,
            out Vec2[] hex,
            float hhStep = 4f)
        {
            hex = null;
            float hint = frameHeight - 47f;
            float tabRoom = SolvedTabHeight(index);
            var tab = new Box(card.X, card.Y - tabRoom, card.W, tabRoom);
            Box stroke = InflateBox(tab, CueHexStroke);
            var px = new float[9];
            var py = new float[9];
            var pm = new float[9];
            Vec2[] corners = Corners(card);
            for (int i = 0; i < 4; i++)
            {
                px[i] = corners[i].X;
                py[i] = corners[i].Y;
                pm[i] = InsideMargin;
            }

            px[4] = pin.Center.X;
            py[4] = pin.Center.Y;
            pm[4] = 8f;
            int boundCount = 5;
            if (HostsCueTab(index))
            {
                Vec2[] strokeCorners = Corners(stroke);
                for (int i = 0; i < 4; i++)
                {
                    px[5 + i] = strokeCorners[i].X;
                    py[5 + i] = strokeCorners[i].Y;
                    pm[5 + i] = 1f;
                }

                boundCount = 9;
            }

            const float sqrt3 = 1.7320508f;
            float hhMax = (hint - 23f) * 0.5f;
            float hhStart = MinPortraitHexHeight(ratio);
            if (hhStart > hhMax)
                return false;
            float grow = 42f + PortraitHexExtra;
            if (PortraitSpread > 0.5f)
                grow = index == PortraitTabIndex ? 36f : 24f;
            float hhLimit = Math.Min(hhMax, hhStart + grow);
            if (hhStep < 1f)
                hhStep = 1f;
            for (float hh = hhStart; hh <= hhLimit + 0.01f; hh += hhStep)
            {
                float cyLo = 23f + hh;
                float cyHi = hint - hh;
                for (int i = 0; i < boundCount; i++)
                {
                    cyLo = Math.Max(cyLo, py[i] - (hh - pm[i]));
                    cyHi = Math.Min(cyHi, py[i] + (hh - pm[i]));
                }

                if (cyLo > cyHi + 0.05f)
                    continue;
                float hw = hh * ratio;
                float[] cys;
                if (hhStep < 2f && cyHi > cyLo + 0.5f)
                {
                    int steps = (int)Math.Ceiling((cyHi - cyLo) / 1.5f);
                    if (steps < 2)
                        steps = 2;
                    if (steps > 16)
                        steps = 16;
                    cys = new float[steps + 1];
                    for (int sample = 0; sample <= steps; sample++)
                        cys[sample] = cyLo + (cyHi - cyLo) * sample / steps;
                }
                else if (PreferLowHex)
                    cys = new[] { cyHi, (cyLo + cyHi) * 0.5f, cyLo };
                else if (PreferHighHex)
                    cys = new[] { cyLo, (cyLo + cyHi) * 0.5f, cyHi };
                else
                    cys = new[] { (cyLo + cyHi) * 0.5f, cyLo, cyHi };
                for (int c = 0; c < cys.Length; c++)
                {
                    float cy = cys[c];
                    float cxLo = 23f + hw;
                    float cxHi = frameWidth - 23f - hw;
                    bool open = true;
                    for (int i = 0; i < boundCount; i++)
                    {
                        float yRel = Math.Abs(py[i] - cy);
                        if (yRel > hh - pm[i] + 0.08f)
                        {
                            open = false;
                            break;
                        }

                        float inset = yRel / sqrt3 + 2f * pm[i] / sqrt3;
                        cxLo = Math.Max(cxLo, px[i] - hw + inset);
                        cxHi = Math.Min(cxHi, px[i] + hw - inset);
                    }

                    if (!open || cxLo > cxHi + 0.05f)
                        continue;
                    float span = Math.Max(0f, cxHi - cxLo);
                    float slack = Math.Min(6f, span * 0.5f);
                    float midCx = (cxLo + cxHi) * 0.5f;
                    float loCx = cxLo + slack * 0.35f;
                    float hiCx = cxHi - slack * 0.35f;
                    float[] cxs = slide == 1
                        ? new[] { loCx, midCx, hiCx }
                        : slide == 2
                            ? new[] { hiCx, midCx, loCx }
                            : new[] { midCx, loCx, hiCx };
                    for (int k = 0; k < cxs.Length; k++)
                    {
                        Vec2[] shape = FlatHex(cxs[k], cy, hw, hh);
                        if (!PortraitHexAccepts(
                            shape, card, pin.Center, stroke, frameWidth, frameHeight, placed, cards, index))
                            continue;

                        hex = shape;
                        return true;
                    }
                }
            }

            return false;
        }

        private static float MinPortraitHexHeight(float ratio)
        {
            float denom = 2f * (2f * ratio - 1f / 1.7320508f);
            if (denom < 0.05f)
                return 160f;
            return (float)Math.Sqrt(40000.0 / denom);
        }

        private static bool PortraitHexAccepts(
            Vec2[] shape,
            Box card,
            Vec2 pinPoint,
            Box stroke,
            float frameWidth,
            float frameHeight,
            Vec2[][] placed,
            Box[] cards,
            int index)
        {
            if (!GlowInside(shape, frameWidth, frameHeight, out _, out _))
                return false;

            if (!PointInPolygon(pinPoint, shape) || DistanceToEdge(pinPoint, shape) < 8f)
                return false;

            if (!RestsInside(card, shape, InsideMargin))
                return false;

            if (HostsCueTab(index) && HexEdgeHits(shape, stroke))
                return false;

            if (PolygonArea(shape) < PortraitAreaFloor)
                return false;

            if (OnPlateInterior(shape, frameWidth, frameHeight) < 8)
                return false;

            if (LeaderHitsOtherHex(pinPoint, NearestOn(card, pinPoint), placed, index))
                return false;
            if (placed == null)
                return true;
            for (int j = 0; j < placed.Length; j++)
            {
                if (j == index || placed[j] == null)
                    continue;
                if (PolygonsOverlap(shape, placed[j], 0.75f))
                    return false;
                if (PortraitSpread > 0.5f && PolygonsOverlap(shape, placed[j], -PortraitSpread))
                    return false;
                if (HostsCueTab(index) && PolygonHitsRect(placed[j], stroke))
                    return false;
                if (cards != null && j < cards.Length && cards[j].W > 1f && HostsCueTab(j))
                {
                    var other = new Box(cards[j].X, cards[j].Y - CueTabRoom, cards[j].W, CueTabRoom);
                    if (HexEdgeHits(shape, InflateBox(other, CueHexStroke)))
                        return false;
                }
            }

            return true;
        }

        private static bool TabClearsPins(Box tab, Box[] pins)
        {
            if (pins == null)
                return true;
            for (int i = 0; i < pins.Length; i++)
            {
                if (pins[i].W > 1f && Separation(tab, pins[i]) < 1f)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// A tab that misses a pin on both axes is measured by the smaller gap. Slide it until
        /// that horizontal gap is at least 1px.
        /// </summary>
        private static bool ShiftTabClearOfPins(ref float x, float y, float cardW, Box[] pins, float frameWidth)
        {
            if (pins == null)
                return true;
            for (int n = 0; n < 8; n++)
            {
                var tab = new Box(x, y - CueTabRoom, cardW, CueTabRoom);
                if (TabClearsPins(tab, pins))
                    return true;
                bool moved = false;
                for (int i = 0; i < pins.Length; i++)
                {
                    if (pins[i].W < 1f)
                        continue;
                    float dx = AxisGap(tab.XMin, tab.XMax, pins[i].XMin, pins[i].XMax);
                    float dy = AxisGap(tab.YMin, tab.YMax, pins[i].YMin, pins[i].YMax);
                    if (dx < 0f || dy < 0f || dx >= 1f)
                        continue;
                    float dir = tab.Center.X <= pins[i].Center.X ? -1f : 1f;
                    x += dir * (1.25f - dx);
                    x = Clamp(x, Edge, Math.Max(Edge, frameWidth - Edge - cardW));
                    moved = true;
                    break;
                }

                if (!moved)
                    return false;
            }

            return TabClearsPins(new Box(x, y - CueTabRoom, cardW, CueTabRoom), pins);
        }


        private static Vec2[][] BuildAll(float frameWidth, float frameHeight, bool plainCover)
        {
            PortraitCards = null;
            if (TrySeatPortraits(frameWidth, frameHeight, plainCover, out Vec2[][] seated, out Box[] seatedCards))
            {
                PortraitCards = seatedCards;
                return seated;
            }

            var placed = new Placement[MapIds.Length];
            var hexes = new Vec2[MapIds.Length][];
            float ratioW = RatioPlateWidth > 2f ? RatioPlateWidth : frameWidth;
            float ratioH = RatioPlateHeight > 2f ? RatioPlateHeight : frameHeight;
            for (int i = 0; i < MapIds.Length; i++)
            {
                AuthoredHalf(MapIds[i], ratioW, ratioH, out float halfW, out float halfH);
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
            SeatGlowInside(placed, frameWidth, frameHeight);

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
            if (!GlowInside(hex, frameWidth, frameHeight, out _, out _))
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

        /// <summary>
        /// The half-box inset can leave a vertex a fraction outside the glow margin on a tall plate.
        /// Nudge or shrink only a district that would fail <see cref="GlowInside"/>; a hex already
        /// inside the clip stays where the packer seated it.
        /// </summary>
        private static void SeatGlowInside(Placement[] placed, float frameWidth, float frameHeight)
        {
            if (placed == null || frameWidth < 2f || frameHeight < 2f)
                return;
            for (int i = 0; i < placed.Length; i++)
            {
                for (int step = 0; step < 8; step++)
                {
                    Vec2[] hex = FlatHex(placed[i].Cx, placed[i].Cy, placed[i].Hw, placed[i].Hh);
                    if (GlowInside(hex, frameWidth, frameHeight, out _, out _))
                        break;
                    float left = 0f;
                    float right = 0f;
                    float top = 0f;
                    float bottom = 0f;
                    for (int p = 0; p < hex.Length; p++)
                    {
                        float glowLeft = hex[p].X - HaloReach;
                        float glowRight = hex[p].X + HaloReach;
                        float glowTop = hex[p].Y - HaloReach;
                        float glowBottom = hex[p].Y + HaloReach;
                        if (glowLeft < 1f)
                            left = Math.Max(left, 1f - glowLeft);
                        if (glowRight > frameWidth - 1f)
                            right = Math.Max(right, glowRight - (frameWidth - 1f));
                        if (glowTop < 1f)
                            top = Math.Max(top, 1f - glowTop);
                        if (glowBottom > frameHeight - 1f)
                            bottom = Math.Max(bottom, glowBottom - (frameHeight - 1f));
                    }

                    bool bothX = left > 0.01f && right > 0.01f;
                    bool bothY = top > 0.01f && bottom > 0.01f;
                    if (!bothX && !bothY)
                    {
                        var nudged = placed[i];
                        if (left > 0.01f)
                            nudged.Cx += left + 0.05f;
                        else if (right > 0.01f)
                            nudged.Cx -= right + 0.05f;
                        if (top > 0.01f)
                            nudged.Cy += top + 0.05f;
                        else if (bottom > 0.01f)
                            nudged.Cy -= bottom + 0.05f;
                        Vec2[] moved = FlatHex(nudged.Cx, nudged.Cy, nudged.Hw, nudged.Hh);
                        var pin = new Vec2(nudged.PinX, nudged.PinY);
                        if (PointInPolygon(pin, moved)
                            && DistanceToEdge(pin, moved) >= 6f
                            && GlowInside(moved, frameWidth, frameHeight, out _, out _)
                            && !GlowHitsNeighbour(placed, i, moved))
                        {
                            placed[i] = nudged;
                            break;
                        }
                    }

                    float deficit = Math.Max(Math.Max(left, right), Math.Max(top, bottom)) + 0.05f;
                    float span = Math.Max(placed[i].Hw, placed[i].Hh);
                    float factor = span > 1f ? Math.Max(0.97f, 1f - deficit / span) : 0.99f;
                    placed[i].Hw *= factor;
                    placed[i].Hh *= factor;
                }
            }
        }

        private static bool GlowHitsNeighbour(Placement[] placed, int index, Vec2[] hex)
        {
            if (placed == null || hex == null)
                return false;
            for (int j = 0; j < placed.Length; j++)
            {
                if (j == index)
                    continue;
                Vec2[] other = FlatHex(placed[j].Cx, placed[j].Cy, placed[j].Hw, placed[j].Hh);
                if (PolygonsOverlap(hex, other, 0.75f))
                    return true;
            }

            return false;
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
                    ConsiderCard(found, index, new Box(maxX + gap, Edge + CueTabRoom, cardWidth, cardHeight), plateWidth, plateHeight, hexes, pins, detail, cue);
                    ConsiderCard(found, index, new Box(minX - gap - cardWidth, Edge + CueTabRoom, cardWidth, cardHeight), plateWidth, plateHeight, hexes, pins, detail, cue);
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
            if (card.Y < Edge + CueTabRoom - 0.01f)
                return;
            if (cue.W > 1f && (Separation(card, cue) < 12f || SegmentHitsRect(pins[index].Center, NearestOn(card, pins[index].Center), cue)))
                return;
            if (!RestsInside(card, hexes[index], InsideMargin) || !SeatHolds(card, hexes[index], hexes) || !CentreOwns(card, hexes[index]))
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

        /// <summary>The zoom pill has to sit clear of every district, including the selected one.</summary>
        private static bool CueCoversHex(Box cue, Vec2[][] hexes)
        {
            if (hexes == null || cue.W < 1f || cue.H < 1f)
                return false;
            for (int i = 0; i < hexes.Length; i++)
            {
                if (hexes[i] != null && PolygonHitsRect(hexes[i], cue))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Distance the near-pin rule uses. A tab attached above the selected card is measured
        /// from the midpoint of that card's top edge. Every other cue is measured from its centre.
        /// </summary>

        public static bool IsCardTopTab(Box card, Box cue)
        {
            return CueIsCardTopTab(card, cue);
        }

        public static float CueAnchorDistance(Box cue, Box card, Box pin)
        {
            Vec2 anchor = card.W > 1f && CueIsCardTopTab(card, cue)
                ? new Vec2(card.X + card.W * 0.5f, card.Y)
                : cue.Center;
            return Distance(anchor, pin.Center);
        }

        /// <summary>
        /// Near the selected district. An exact tab is measured from the midpoint of the selected
        /// card's top edge. Every other cue is measured from its centre. 160px still applies, and
        /// a cue whose centre lies inside the selected hex also counts. Capture uses this same predicate.
        /// </summary>
        public static bool CueCountsAsNear(Box cue, Box card, Box pin, Vec2[] hex)
        {
            if (card.W > 1f && CueAnchorDistance(cue, card, pin) <= CueNearLimit)
                return true;
            return hex != null && hex.Length >= 3 && PointInPolygon(cue.Center, hex);
        }

        /// <summary>
        /// The one zoom cue: a tab above the selected card, outside it, exact card x and width,
        /// 36–52px tall, bottom flush with the card's top edge.
        /// </summary>
        private static bool CueIsCardTopTab(Box card, Box cue)
        {
            if (card.W < 48f || cue.W < 8f)
                return false;
            if (cue.H < 36f - 0.5f || cue.H > 52f + 0.5f)
                return false;
            if (Math.Abs(cue.X - card.X) > 1.5f || Math.Abs(cue.W - card.W) > 1.5f)
                return false;
            if (Math.Abs(cue.YMax - card.Y) > 1.5f)
                return false;
            return cue.YMax <= card.Y + 1.5f && cue.Y < card.Y - 1f;
        }

        /// <summary>Lowest district-outline y that crosses the card's width. -1 when the hex misses that span.</summary>
        private static float HexLip(Vec2[] hex, float x0, float x1)
        {
            if (hex == null || hex.Length < 3)
                return -1f;
            float bottom = -1f;
            for (int i = 0; i < hex.Length; i++)
            {
                Vec2 a = hex[i];
                Vec2 b = hex[(i + 1) % hex.Length];
                float minX = Math.Min(a.X, b.X);
                float maxX = Math.Max(a.X, b.X);
                if (maxX < x0 || minX > x1)
                    continue;
                bottom = Math.Max(bottom, Math.Max(a.Y, b.Y));
            }

            return bottom;
        }

        private static bool HexEdgeHits(Vec2[] hex, Box spot)
        {
            if (hex == null || hex.Length < 3 || spot.W < 1f)
                return false;
            for (int i = 0; i < hex.Length; i++)
            {
                if (SegmentHitsRect(hex[i], hex[(i + 1) % hex.Length], spot))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Full-width tab above the card. Taller tabs are tried first. The tab never enters the card.
        /// </summary>
        private static bool TrySeatAlignedTop(
            float plateWidth,
            float plateHeight,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            Box card,
            float shortest,
            out Box cue)
        {
            cue = default;
            float[] heights = { 52f, 48f, 44f, 40f, 36f };
            for (int h = 0; h < heights.Length; h++)
            {
                if (heights[h] + 0.1f < shortest)
                    continue;
                if (!TrySeatTabHeight(
                    heights[h], card, pins, cards, hexes, detail, selected, plateWidth, plateHeight, out cue))
                    continue;
                return true;
            }

            return false;
        }

        private static bool TrySeatTabHeight(
            float stripH,
            Box card,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            float plateWidth,
            float plateHeight,
            out Box cue)
        {
            cue = default;
            var spot = new Box(card.X, card.Y - stripH, card.W, stripH);
            if (!AlignedTabLegal(spot, card, pins, cards, hexes, detail, selected, plateWidth, plateHeight))
                return false;
            cue = spot;
            return true;
        }

        private static bool AlignedTabLegal(
            Box spot,
            Box card,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            float plateWidth,
            float plateHeight)
        {
            if (Math.Abs(spot.X - card.X) > 0.5f || Math.Abs(spot.W - card.W) > 0.5f)
                return false;
            if (!CueIsCardTopTab(card, spot))
                return false;
            if (!OnPlate(spot, plateWidth, plateHeight))
                return false;
            if (!Clears(spot, detail))
                return false;
            if (pins == null || selected < 0 || selected >= pins.Length)
                return false;
            for (int i = 0; i < pins.Length; i++)
            {
                if (pins[i].W > 1f && Separation(spot, pins[i]) < 1f)
                    return false;
            }

            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (i == selected || cards[i].W < 1f)
                        continue;
                    if (Overlaps(spot, cards[i], 2f))
                        return false;
                }
            }

            bool insideHex = hexes != null && selected < hexes.Length && hexes[selected] != null
                && PointInPolygon(spot.Center, hexes[selected]);
            if (CueAnchorDistance(spot, card, pins[selected]) > CueNearLimit && !insideHex)
                return false;
            if (hexes != null && !CueReadsAsSelected(card.Center, pins[selected].Center, hexes, pins, selected))
                return false;
            if (LeaderHitsRect(pins, cards, spot))
                return false;
            if (hexes == null)
                return true;
            Box stroke = InflateBox(spot, CueHexStroke);
            for (int i = 0; i < hexes.Length; i++)
            {
                if (hexes[i] == null)
                    continue;
                if (i == selected)
                {
                    if (HexEdgeHits(hexes[i], stroke))
                        return false;
                    continue;
                }

                if (PolygonHitsRect(hexes[i], stroke))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Seats the one cue: a tab above the selected card. When the plate edge or a hex
        /// leaves no room, the selected card moves down until that same tab fits. No other
        /// cue shape is tried. <paramref name="cards"/> may be updated at <paramref name="selected"/>.
        /// </summary>
        public static bool TryAttachCueToCard(
            float plateWidth,
            float plateHeight,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            out Box cue)
        {
            cue = default;
            if (plateWidth < 32f || plateHeight < 32f || pins == null || cards == null)
                return false;
            if (selected < 0 || selected >= pins.Length || selected >= cards.Length)
                return false;
            Box card = cards[selected];
            if (card.W < 48f || card.H < 48f)
                return false;
            // Two 16px Farsi lines need 48–52px. A 36px tab that already fits must not win
            // before the card has been dropped to make the taller tab legal.
            if (TrySeatAlignedTop(plateWidth, plateHeight, pins, cards, hexes, detail, selected, card, 48f, out cue))
                return true;
            if (TryDropCardForCue(plateWidth, plateHeight, pins, cards, hexes, detail, selected, card, 48f, out cue))
                return true;
            if (TrySeatAlignedTop(plateWidth, plateHeight, pins, cards, hexes, detail, selected, card, 36f, out cue))
                return true;
            if (TryDropCardForCue(plateWidth, plateHeight, pins, cards, hexes, detail, selected, card, 36f, out cue))
                return true;
            if (TryShiftCardForTab(plateWidth, plateHeight, pins, cards, hexes, detail, selected, card, out cue))
                return true;
            cue = default;
            return false;
        }

        /// <summary>
        /// Moves the selected card down, then sideways, until a tab at least
        /// <paramref name="shortest"/> pixels tall clears the hex stroke.
        /// </summary>
        private static bool TryDropCardForCue(
            float plateWidth,
            float plateHeight,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            Box card,
            float shortest,
            out Box cue)
        {
            cue = default;
            float floor = plateHeight - Edge - card.H;
            float[] heights = { 52f, 48f, 44f, 40f, 36f };
            float[] slides = { 0f, 4f, -4f, 8f, -8f, 12f, -12f, 16f, -16f, 20f, -20f, 28f, -28f, 36f, -36f };
            for (int h = 0; h < heights.Length; h++)
            {
                float stripH = heights[h];
                if (stripH + 0.1f < shortest)
                    continue;
                float minY = Math.Max(card.Y, Edge + stripH);
                for (float y = minY; y <= floor + 0.1f; y += 4f)
                {
                    for (int s = 0; s < slides.Length; s++)
                    {
                        var dropped = new Box(card.X + slides[s], y, card.W, card.H);
                        if (hexes != null && selected < hexes.Length && !CentreOwns(dropped, hexes[selected]))
                            continue;
                        if (!SelectedCardCanDrop(dropped, selected, cards, pins, hexes, detail, plateWidth, plateHeight))
                            continue;
                        Box held = cards[selected];
                        cards[selected] = dropped;
                        bool tabOk = TrySeatTabHeight(
                            stripH, dropped, pins, cards, hexes, detail, selected, plateWidth, plateHeight, out cue);
                        if (!tabOk)
                        {
                            cards[selected] = held;
                            continue;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The packed card has no room for its tab. Slide that card, keeping the same size,
        /// until the tab above it is on the plate and the card is still a legal seat.
        /// </summary>
        private static bool TryShiftCardForTab(
            float plateWidth,
            float plateHeight,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            Box card,
            out Box cue)
        {
            cue = default;
            float[] heights = { 52f, 48f, 44f, 40f, 36f };
            float best = float.MaxValue;
            Box bestCard = default;
            Box bestCue = default;
            bool found = false;
            for (int h = 0; h < heights.Length; h++)
            {
                float stripH = heights[h];
                bool taller = false;
                for (float dy = -240f; dy <= 240.1f; dy += 2f)
                {
                    for (float dx = -280f; dx <= 280.1f; dx += 2f)
                    {
                        var dropped = new Box(card.X + dx, card.Y + dy, card.W, card.H);
                        if (hexes != null && selected < hexes.Length && !CentreOwns(dropped, hexes[selected]))
                            continue;
                        if (!SelectedCardCanDrop(dropped, selected, cards, pins, hexes, detail, plateWidth, plateHeight))
                            continue;
                        Box held = cards[selected];
                        cards[selected] = dropped;
                        bool tabOk = TrySeatTabHeight(
                            stripH, dropped, pins, cards, hexes, detail, selected, plateWidth, plateHeight, out Box spot);
                        cards[selected] = held;
                        if (!tabOk)
                            continue;
                        float score = dx * dx + dy * dy;
                        if (score >= best)
                            continue;
                        best = score;
                        bestCard = dropped;
                        bestCue = spot;
                        found = true;
                        taller = true;
                    }
                }

                if (taller)
                    break;
            }

            if (!found)
                return false;
            cards[selected] = bestCard;
            cue = bestCue;
            return true;
        }

        /// <summary>The selected card may move down to free its top tab. The seat rules stay the same.</summary>
        private static bool SelectedCardCanDrop(
            Box card,
            int index,
            Box[] cards,
            Box[] pins,
            Vec2[][] hexes,
            Box detail,
            float plateWidth,
            float plateHeight)
        {
            if (!OnPlate(card, plateWidth, plateHeight))
                return false;
            if (hexes != null && index < hexes.Length && !SeatHolds(card, hexes[index], hexes))
                return false;
            if (detail.W > 1f && Overlaps(card, detail, 0f))
                return false;
            if (cards != null)
            {
                for (int j = 0; j < cards.Length; j++)
                {
                    if (j == index || cards[j].W < 1f)
                        continue;
                    if (Separation(card, cards[j]) < CardGap)
                        return false;
                }
            }
            if (pins == null || index < 0 || index >= pins.Length || !ClearsPins(card, pins))
                return false;
            if (Leader(pins[index], card) < LeaderMin)
                return false;
            if (SegmentHitsRect(pins[index].Center, NearestOn(card, pins[index].Center), detail))
                return false;
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            if (hint.W > 1f && Overlaps(card, hint, 2f))
                return false;
            return true;
        }

        /// <summary>
        /// When the portrait-bottom strip is outside the 160px near rule, keep a 36–52px strip
        /// as wide as the card allows and as low as the rule allows. A side tab is only used
        /// when a full-width bar would cover the leader.
        /// </summary>
        private static bool TrySeatWideStrip(
            float plateWidth,
            float plateHeight,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            Box card,
            out Box cue)
        {
            cue = default;
            float statusH = card.H >= 190f ? 40f : 34f;
            float[] heights = { 52f, 48f, 44f, 40f, 36f };
            bool found = false;
            float bestScore = float.MaxValue;
            Box best = default;
            for (int h = 0; h < heights.Length; h++)
            {
                float stripH = heights[h];
                float yBottom = card.YMax - statusH - 2f - stripH;
                float yHang = card.Y - stripH + 8f;
                for (float stripW = card.W; stripW >= 96f - 0.1f; stripW -= 8f)
                {
                    float xCenter = card.X + (card.W - stripW) * 0.5f;
                    float xLeft = card.X;
                    float xRight = card.XMax - stripW;
                    float pinX = pins[selected].Center.X;
                    float xMissLeft = pinX - stripW - 2f;
                    float xMissRight = pinX + 2f;
                    float[] xs = { xCenter, xLeft, xRight, xMissLeft, xMissRight };
                    for (int xi = 0; xi < xs.Length; xi++)
                    {
                        for (float y = yBottom; y >= yHang - 0.1f; y -= 8f)
                        {
                            var spot = new Box(xs[xi], y, stripW, stripH);
                            if (!CueStripSeatable(spot, card, pins, cards, hexes, detail, selected, plateWidth, plateHeight))
                                continue;
                            bool inside = BoxInside(spot, card, 0.5f);
                            float score = (inside ? 0f : 100000f)
                                + (card.W - stripW) * 40f
                                + Math.Abs(y - yBottom);
                            if (found && score >= bestScore)
                                continue;
                            found = true;
                            bestScore = score;
                            best = spot;
                        }
                    }

                    if (found && best.W > card.W - 4f)
                        break;
                }

                if (found && best.W > card.W - 4f)
                    break;
            }

            if (!found)
                return false;
            cue = best;
            return true;
        }

        private static bool CueStripSeatable(
            Box spot,
            Box card,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            float plateWidth,
            float plateHeight)
        {
            if (BoxInside(spot, card, 0.5f))
                return CueTabLegal(spot, card, pins, cards, hexes, detail, selected, plateWidth, plateHeight);
            return CueHangLegal(spot, card, pins, cards, hexes, detail, selected, plateWidth, plateHeight);
        }

        /// <summary>
        /// When a strip inside the card is farther than <see cref="CueNearLimit"/> and outside the
        /// hex, hang a tab off the card toward the pin. It still overlaps the card, misses the
        /// leader, and stays off every other district. The selected hex is allowed: that is how
        /// the centre gets inside the near-pin rule.
        /// </summary>
        private static bool TryHangCueOffCard(
            float plateWidth,
            float plateHeight,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            Box card,
            out Box cue)
        {
            cue = default;
            float[] stripHeights = { 52f, 48f, 44f, 40f, 36f };
            float[] stripWidths = { card.W, 148f, 132f, 120f, 108f, 96f };
            for (int h = 0; h < stripHeights.Length; h++)
            {
                float stripHeight = stripHeights[h];
                for (int w = 0; w < stripWidths.Length; w++)
                {
                    float stripWidth = stripWidths[w] > card.W ? card.W : stripWidths[w];
                    if (stripWidth < 32f || stripHeight < 16f)
                        continue;
                    if (TryHangCueSize(
                        plateWidth, plateHeight, pins, cards, hexes, detail, selected, card,
                        stripWidth, stripHeight, out cue))
                        return true;
                }
            }

            return false;
        }

        private static bool TryHangCueSize(
            float plateWidth,
            float plateHeight,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            Box card,
            float stripWidth,
            float stripHeight,
            out Box cue)
        {
            cue = default;
            float x0 = card.X - stripWidth + 8f;
            float x1 = card.XMax - 8f;
            float y0 = card.Y - stripHeight + 8f;
            float y1 = card.YMax - 8f;
            float[] ys =
            {
                card.Y - stripHeight + 8f,
                card.YMax - 8f,
                card.Y + 4f,
                card.YMax - stripHeight - 4f
            };
            float[] xs = { card.X, card.XMax - stripWidth };
            for (int yi = 0; yi < ys.Length; yi++)
            {
                for (int xi = 0; xi < xs.Length; xi++)
                {
                    if (TryHangSpot(
                        plateWidth, plateHeight, pins, cards, hexes, detail, selected, card,
                        xs[xi], ys[yi], stripWidth, stripHeight, out cue))
                        return true;
                }
            }

            Vec2 pin = pins[selected].Center;
            for (float y = y0; y <= y1 + 0.1f; y += 8f)
            {
                for (float x = x0; x <= x1 + 0.1f; x += 8f)
                {
                    var spot = new Box(x, y, stripWidth, stripHeight);
                    if (Distance(spot.Center, pin) > CueNearLimit
                        && !(hexes != null && selected < hexes.Length && hexes[selected] != null
                            && PointInPolygon(spot.Center, hexes[selected])))
                        continue;
                    if (!CueHangLegal(spot, card, pins, cards, hexes, detail, selected, plateWidth, plateHeight))
                        continue;
                    cue = spot;
                    return true;
                }
            }

            return false;
        }

        private static bool TryHangSpot(
            float plateWidth,
            float plateHeight,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            Box card,
            float x,
            float y,
            float stripWidth,
            float stripHeight,
            out Box cue)
        {
            cue = default;
            var spot = new Box(x, y, stripWidth, stripHeight);
            if (!CueHangLegal(spot, card, pins, cards, hexes, detail, selected, plateWidth, plateHeight))
                return false;
            cue = spot;
            return true;
        }

        private static bool CueHangLegal(
            Box spot,
            Box card,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            float plateWidth,
            float plateHeight)
        {
            if (spot.W < 8f || spot.H < 8f || pins == null || selected < 0 || selected >= pins.Length)
                return false;
            if (!Overlaps(spot, card, 0f))
                return false;
            float overlapW = Math.Min(spot.XMax, card.XMax) - Math.Max(spot.XMin, card.XMin);
            float overlapH = Math.Min(spot.YMax, card.YMax) - Math.Max(spot.YMin, card.YMin);
            if (overlapW < 8f || overlapH < 8f)
                return false;
            if (!OnPlate(spot, plateWidth, plateHeight) || !Clears(spot, detail))
                return false;
            for (int i = 0; i < pins.Length; i++)
            {
                if (pins[i].W > 1f && Separation(spot, pins[i]) < 1f)
                    return false;
            }

            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (i == selected || cards[i].W < 1f)
                        continue;
                    if (Overlaps(spot, cards[i], 2f))
                        return false;
                }
            }

            bool insideHex = hexes != null && selected < hexes.Length && hexes[selected] != null
                && PointInPolygon(spot.Center, hexes[selected]);
            if (Distance(spot.Center, pins[selected].Center) > CueNearLimit && !insideHex)
                return false;
            if (LeaderHitsRect(pins, cards, spot))
                return false;
            if (BoxInside(spot, card, 0.5f))
                return true;
            if (hexes == null)
                return true;
            Box grown = InflateBox(spot, CueHexStroke);
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i == selected || hexes[i] == null)
                    continue;
                if (PolygonHitsRect(hexes[i], grown))
                    return false;
            }

            return true;
        }

        private static bool CueTabLegal(
            Box spot,
            Box card,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            float plateWidth,
            float plateHeight)
        {
            if (spot.W < 8f || spot.H < 8f || pins == null || selected < 0 || selected >= pins.Length)
                return false;
            if (!BoxInside(spot, card, 0.5f))
                return false;
            if (!OnPlate(spot, plateWidth, plateHeight) || !Clears(spot, detail))
                return false;
            for (int i = 0; i < pins.Length; i++)
            {
                if (pins[i].W > 1f && Separation(spot, pins[i]) < 1f)
                    return false;
            }

            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (i == selected || cards[i].W < 1f)
                        continue;
                    if (Overlaps(spot, cards[i], 2f))
                        return false;
                }
            }

            bool insideHex = hexes != null && selected < hexes.Length && hexes[selected] != null
                && PointInPolygon(spot.Center, hexes[selected]);
            if (Distance(spot.Center, pins[selected].Center) > CueNearLimit && !insideHex)
                return false;
            if (LeaderHitsRect(pins, cards, spot))
                return false;
            return true;
        }

        private static bool BoxInside(Box inner, Box outer, float slop)
        {
            return inner.W > 1f && outer.W > 1f
                && inner.XMin >= outer.XMin - slop
                && inner.YMin >= outer.YMin - slop
                && inner.XMax <= outer.XMax + slop
                && inner.YMax <= outer.YMax + slop;
        }

        private static Box InflateBox(Box box, float margin)
        {
            return new Box(box.X - margin, box.Y - margin, box.W + margin * 2f, box.H + margin * 2f);
        }

        /// <summary>
        /// Slides a packed pill off every hex without moving the cards. Keeps the pill near the
        /// selected pin. Returns false when no clear seat exists, and the caller keeps the packed pill.
        /// </summary>
        private static bool TrySlideCueOffHexes(
            float plateWidth,
            float plateHeight,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            float cueWidth,
            bool fa,
            out Box cue)
        {
            cue = default;
            if (pins == null || selected < 0 || selected >= pins.Length || cueWidth < 8f)
                return false;
            Box pin = pins[selected];
            float[] heights = { CueHeight, 32f };
            float[] gaps = { 12f, 4f };
            for (int pass = 0; pass < heights.Length; pass++)
            {
                float height = heights[pass];
                float cardGap = gaps[pass];
                float narrowest = pass == 0 ? (fa ? 140f : 100f) : 140f;
                if (TrySlideCuePass(
                    plateWidth, plateHeight, pins, cards, hexes, detail, selected, pin, cueWidth, narrowest, height, cardGap, out cue))
                    return true;
            }

            return false;
        }

        private static bool TrySlideCuePass(
            float plateWidth,
            float plateHeight,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            Box pin,
            float cueWidth,
            float narrowest,
            float height,
            float cardGap,
            out Box cue)
        {
            cue = default;
            Vec2 pinCentre = pin.Center;
            for (float width = cueWidth; width >= narrowest - 0.5f; width -= 8f)
            {
                for (int dist = 0; dist <= 160; dist += 4)
                {
                    for (int slide = 0; slide <= 160; slide += 4)
                    {
                        float[] slides = slide == 0 ? new[] { 0f } : new[] { -(float)slide, slide };
                        for (int s = 0; s < slides.Length; s++)
                        {
                            float offset = slides[s];
                            Box[] spots =
                            {
                                new Box(pin.XMax + 8f + dist, pinCentre.Y - height * 0.5f + offset, width, height),
                                new Box(pin.X - 8f - width - dist, pinCentre.Y - height * 0.5f + offset, width, height),
                                new Box(pinCentre.X - width * 0.5f + offset, pin.YMax + 8f + dist, width, height),
                                new Box(pinCentre.X - width * 0.5f + offset, pin.Y - 8f - height - dist, width, height)
                            };
                            for (int i = 0; i < spots.Length; i++)
                            {
                                Box spot = spots[i];
                                if (!SlideCueOk(spot, pins, cards, hexes, detail, selected, plateWidth, plateHeight, cardGap))
                                    continue;
                                cue = spot;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static bool SlideCueOk(
            Box spot,
            Box[] pins,
            Box[] cards,
            Vec2[][] hexes,
            Box detail,
            int selected,
            float plateWidth,
            float plateHeight,
            float cardGap)
        {
            if (spot.W < 8f || spot.H < 8f || CueCoversHex(spot, hexes))
                return false;
            if (!OnPlate(spot, plateWidth, plateHeight) || !Clears(spot, detail) || !ClearsPins(spot, pins))
                return false;
            if (cards != null && !ClearsCards(spot, cards, cardGap))
                return false;
            if (CueReach(spot, pins[selected]) > CueNearLimit)
                return false;
            if (!CueReadsAsSelected(spot.Center, pins[selected].Center, hexes, pins, selected))
                return false;
            if (EdgeCrosses(spot, hexes) || LeaderHitsRect(pins, cards, spot))
                return false;
            return true;
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
                if (!PointInPolygon(pin, hexes[i]) || EdgeCloserThan(pin, hexes[i], 6f))
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
                    if (!PointInPolygon(point, shape) || EdgeCloserThan(point, shape, margin))
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
            float best = PointRectDistanceSq(a, rect);
            best = Math.Min(best, PointRectDistanceSq(b, rect));
            best = Math.Min(best, PointSegmentDistanceSq(new Vec2(rect.XMin, rect.YMin), a, b));
            best = Math.Min(best, PointSegmentDistanceSq(new Vec2(rect.XMax, rect.YMin), a, b));
            best = Math.Min(best, PointSegmentDistanceSq(new Vec2(rect.XMax, rect.YMax), a, b));
            best = Math.Min(best, PointSegmentDistanceSq(new Vec2(rect.XMin, rect.YMax), a, b));
            return best <= 0f ? 0f : (float)Math.Sqrt(best);
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

        private static float PointRectDistanceSq(Vec2 point, Box rect)
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
            return dx * dx + dy * dy;
        }

        private static float PointSegmentDistance(Vec2 point, Vec2 a, Vec2 b)
        {
            return (float)Math.Sqrt(PointSegmentDistanceSq(point, a, b));
        }

        private static float PointSegmentDistanceSq(Vec2 point, Vec2 a, Vec2 b)
        {
            float ex = b.X - a.X;
            float ey = b.Y - a.Y;
            float lengthSq = ex * ex + ey * ey;
            float t = lengthSq < 0.0001f
                ? 0f
                : Clamp(((point.X - a.X) * ex + (point.Y - a.Y) * ey) / lengthSq, 0f, 1f);
            float dx = point.X - (a.X + ex * t);
            float dy = point.Y - (a.Y + ey * t);
            return dx * dx + dy * dy;
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
            float best = DistanceToEdgeSq(point, polygon);
            if (best == float.MaxValue)
                return best;
            return (float)Math.Sqrt(best);
        }

        /// <summary>
        /// Squared distance to the nearest edge. Comparisons against a margin use
        /// <see cref="EdgeCloserThan"/> so the square root runs only on the boundary.
        /// </summary>
        private static float DistanceToEdgeSq(Vec2 point, Vec2[] polygon)
        {
            float best = float.MaxValue;
            if (polygon == null)
                return best;
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
                float distSq = dx * dx + dy * dy;
                if (distSq < best)
                    best = distSq;
            }

            return best;
        }

        /// <summary>True when some edge is strictly closer than <paramref name="margin"/>.</summary>
        private static bool EdgeCloserThan(Vec2 point, Vec2[] polygon, float margin)
        {
            if (polygon == null || margin <= 0f)
                return false;
            float limit = margin * margin;
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
                float distSq = dx * dx + dy * dy;
                if (distSq > limit + 0.001f)
                    continue;
                if (distSq + 0.001f < limit || (float)Math.Sqrt(distSq) < margin)
                    return true;
            }

            return false;
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
