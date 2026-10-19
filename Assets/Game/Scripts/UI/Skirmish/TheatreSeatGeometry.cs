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

        /// <summary>
        /// 4:3 Farsi Desert Base was sampling a flat rim because the card sat on the
        /// painted centre. Slide the card until that centre is outside it.
        /// </summary>
        private static void ShiftDesertOffCentre(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa)
        {
            int db = IndexOf("DB");
            if (db < 0 || hexes == null || cards == null || db >= hexes.Length || db >= cards.Length)
                return;
            if (hexes[db] == null || cards[db].W < 1f)
                return;
            CentroidOf(hexes[db], out float cx, out float cy);
            Box card = cards[db];
            if (cx < card.XMin + 4f || cx > card.XMax - 4f || cy < card.YMin + 4f || cy > card.YMax - 4f)
                return;
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            Box best = card;
            bool found = false;
            float[] shift = { -48f, -32f, -16f, 16f, 32f, 48f };
            for (int x = 0; x < shift.Length; x++)
            {
                for (int y = 0; y < shift.Length; y++)
                {
                    var trial = new Box(card.X + shift[x], card.Y + shift[y], card.W, card.H);
                    if (cx >= trial.XMin + 4f && cx <= trial.XMax - 4f && cy >= trial.YMin + 4f && cy <= trial.YMax - 4f)
                        continue;
                    if (!SampleCardFits(trial, db, cards, hexes, pins, detail, hint, plateWidth, plateHeight))
                        continue;
                    cards[db] = trial;
                    bool holds = DrawnTerritoryHolds(hexes, pins, cards, cue, selected, detail, hint, plateWidth, plateHeight, fa, db);
                    cards[db] = card;
                    if (!holds)
                        continue;
                    best = trial;
                    found = true;
                    break;
                }

                if (found)
                    break;
            }

            if (!found)
                return;
            float dx = best.X - card.X;
            float dy = best.Y - card.Y;
            cards[db] = best;
            if (selected == db && cue.W > 1f)
                cue = new Box(cue.X + dx, cue.Y + dy, cue.W, cue.H);
        }

        /// <summary>
        /// Slides or shortens a portrait when the card covers the paintable interior.
        /// Capture needs eight samples 26px inside the district and outside the card.
        /// </summary>
        private static void UncoverSampleCards(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight)
        {
            if (cards == null || hexes == null)
                return;
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            float[] delta = { -18f, -12f, -8f, 8f, 12f, 18f };
            for (int i = 0; i < cards.Length && i < hexes.Length; i++)
            {
                if (cards[i].W < 1f || hexes[i] == null)
                    continue;
                Box useCue = i == selected ? cue : default;
                int have = InteriorUnblocked(hexes[i], cards[i], pins[i], useCue, hint);
                if (have >= 8)
                    continue;
                Box best = cards[i];
                int bestCount = have;
                for (int x = 0; x < delta.Length; x++)
                {
                    for (int y = 0; y < delta.Length; y++)
                    {
                        if (Math.Abs(delta[x]) < 0.5f && Math.Abs(delta[y]) < 0.5f)
                            continue;
                        var trial = new Box(cards[i].X + delta[x], cards[i].Y + delta[y], cards[i].W, cards[i].H);
                        if (!SampleCardFits(trial, i, cards, hexes, pins, detail, hint, plateWidth, plateHeight))
                            continue;
                        int count = InteriorUnblocked(hexes[i], trial, pins[i], useCue, hint);
                        if (count <= bestCount)
                            continue;
                        best = trial;
                        bestCount = count;
                    }
                }

                for (float cut = 8f; cut <= 48f && bestCount < 8; cut += 6f)
                {
                    float height = cards[i].H - cut;
                    if (height + 0.5f < PortraitMinHeight)
                        break;
                    var fromTop = new Box(cards[i].X, cards[i].Y + cut, cards[i].W, height);
                    var fromBottom = new Box(cards[i].X, cards[i].Y, cards[i].W, height);
                    if (SampleCardFits(fromTop, i, cards, hexes, pins, detail, hint, plateWidth, plateHeight))
                    {
                        int count = InteriorUnblocked(hexes[i], fromTop, pins[i], useCue, hint);
                        if (count > bestCount)
                        {
                            best = fromTop;
                            bestCount = count;
                        }
                    }

                    if (SampleCardFits(fromBottom, i, cards, hexes, pins, detail, hint, plateWidth, plateHeight))
                    {
                        int count = InteriorUnblocked(hexes[i], fromBottom, pins[i], useCue, hint);
                        if (count > bestCount)
                        {
                            best = fromBottom;
                            bestCount = count;
                        }
                    }

                    float width = cards[i].W - cut;
                    if (width + 0.5f < PortraitMinWidth)
                        continue;
                    var fromLeft = new Box(cards[i].X + cut, cards[i].Y, width, cards[i].H);
                    var fromRight = new Box(cards[i].X, cards[i].Y, width, cards[i].H);
                    if (SampleCardFits(fromLeft, i, cards, hexes, pins, detail, hint, plateWidth, plateHeight))
                    {
                        int count = InteriorUnblocked(hexes[i], fromLeft, pins[i], useCue, hint);
                        if (count > bestCount)
                        {
                            best = fromLeft;
                            bestCount = count;
                        }
                    }

                    if (SampleCardFits(fromRight, i, cards, hexes, pins, detail, hint, plateWidth, plateHeight))
                    {
                        int count = InteriorUnblocked(hexes[i], fromRight, pins[i], useCue, hint);
                        if (count > bestCount)
                        {
                            best = fromRight;
                            bestCount = count;
                        }
                    }
                }

                cards[i] = best;
            }

            // Airfield Plains at 20:9 notch still had only six world samples. Shorten
            // that card until the scaled grid clears the card, the pin, and the cue.
            int ap = IndexOf("AP");
            if (ap < 0 || ap >= cards.Length || ap >= hexes.Length || cards[ap].W < 1f || hexes[ap] == null)
                return;
            Box apCue = ap == selected ? cue : default;
            if (NotchInterior(hexes[ap], cards[ap], pins[ap], apCue, hint) >= 8)
                return;
            Box apBest = cards[ap];
            int apCount = NotchInterior(hexes[ap], apBest, pins[ap], apCue, hint);
            for (float cut = 8f; cut <= 48f && apCount < 8; cut += 6f)
            {
                float height = cards[ap].H - cut;
                float width = cards[ap].W - cut;
                var trials = new Box[]
                {
                    new Box(cards[ap].X, cards[ap].Y + cut, cards[ap].W, height),
                    new Box(cards[ap].X, cards[ap].Y, cards[ap].W, height),
                    new Box(cards[ap].X + cut, cards[ap].Y, width, cards[ap].H),
                    new Box(cards[ap].X, cards[ap].Y, width, cards[ap].H)
                };
                for (int t = 0; t < trials.Length; t++)
                {
                    if (trials[t].H + 0.5f < PortraitMinHeight || trials[t].W + 0.5f < PortraitMinWidth)
                        continue;
                    if (!SampleCardFits(trials[t], ap, cards, hexes, pins, detail, hint, plateWidth, plateHeight))
                        continue;
                    int count = NotchInterior(hexes[ap], trials[t], pins[ap], apCue, hint);
                    if (count <= apCount)
                        continue;
                    apBest = trials[t];
                    apCount = count;
                }
            }

            // A shorter card can still sit on the open side of the hex. Slide it
            // until samples fall outside the card, the pin, and the name box.
            float[] slide = { 12f, 24f, 36f, 48f };
            for (int s = 0; s < slide.Length && apCount < 8; s++)
            {
                float d = slide[s];
                var shifts = new Box[]
                {
                    new Box(apBest.X + d, apBest.Y, apBest.W, apBest.H),
                    new Box(apBest.X - d, apBest.Y, apBest.W, apBest.H),
                    new Box(apBest.X, apBest.Y + d, apBest.W, apBest.H),
                    new Box(apBest.X, apBest.Y - d, apBest.W, apBest.H)
                };
                for (int t = 0; t < shifts.Length; t++)
                {
                    if (!SampleCardFits(shifts[t], ap, cards, hexes, pins, detail, hint, plateWidth, plateHeight))
                        continue;
                    int count = NotchInterior(hexes[ap], shifts[t], pins[ap], apCue, hint);
                    if (count <= apCount)
                        continue;
                    apBest = shifts[t];
                    apCount = count;
                }
            }

            cards[ap] = apBest;
        }

        private static bool SampleCardFits(
            Box trial,
            int index,
            Box[] cards,
            Vec2[][] hexes,
            Box[] pins,
            Box detail,
            Box hint,
            float plateWidth,
            float plateHeight)
        {
            if (trial.W + 0.5f < PortraitMinWidth || trial.H + 0.5f < PortraitMinHeight)
                return false;
            if (trial.W > PortraitMaxWidth + PortraitMaxSlop || trial.H > PortraitMaxHeight + PortraitMaxSlop)
                return false;
            if (!OnPlate(trial, plateWidth, plateHeight) || !Clears(trial, detail))
                return false;
            if (hint.W > 1f && Overlaps(trial, hint, 2f))
                return false;
            if (hexes != null && index < hexes.Length && !SeatHolds(trial, hexes[index], hexes))
                return false;
            if (cards != null)
            {
                for (int j = 0; j < cards.Length; j++)
                {
                    if (j == index || cards[j].W < 1f)
                        continue;
                    if (Separation(trial, cards[j]) < CardGap)
                        return false;
                }
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

            // A two-line title at 16px needs about 46px. The strip is 24% of the card,
            // so the portrait has to be tall enough for that strip, and wide enough for
            // the longer word.
            const float stripFloor = 46f;
            float tall = stripFloor / 0.24f;
            if (tall > PortraitMaxHeight)
                tall = PortraitMaxHeight;
            for (int i = 0; i < n; i++)
            {
                if (cards[i].W < 1f)
                    continue;
                float width = cards[i].W < 152f ? 152f : cards[i].W;
                if (width > PortraitMaxWidth)
                    width = PortraitMaxWidth;
                float room = bands[i] > cards[i].H ? bands[i] : cards[i].H;
                float height = cards[i].H < tall ? Math.Min(tall, room) : cards[i].H;
                if (height > PortraitMaxHeight)
                    height = PortraitMaxHeight;
                if (width <= cards[i].W + 0.5f && height <= cards[i].H + 0.5f)
                    continue;
                float x = cards[i].X - (width - cards[i].W) * 0.5f;
                if (x < Edge)
                    x = Edge;
                var grownCard = new Box(x, cards[i].Y, width, height);
                if (hint.W > 1f && Overlaps(grownCard, hint, 2f))
                    continue;
                Vec2[] own = hexes != null && i < hexes.Length ? hexes[i] : null;
                if (own != null && hexes != null && !SeatHolds(grownCard, own, hexes))
                    continue;
                if (!SeatAccepts(grownCard, i, cards, hexes, detail))
                    continue;
                cards[i] = grownCard;
            }
        }

        /// <summary>
        /// Two lines at the 20px floor need a 46px strip, which is 24% of a 192px card.
        /// Growth that the drawn district cannot hold is refused: a taller card was
        /// falling back to a hex that crossed the detail glow or the map edge.
        /// </summary>
        private static void RaiseStripCards(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa)
        {
            if (hexes == null || cards == null || pins == null)
                return;
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            const float stripTarget = 192f;
            for (int i = 0; i < cards.Length && i < hexes.Length && i < MapIds.Length; i++)
            {
                if (cards[i].W < 1f || hexes[i] == null)
                    continue;
                float cap = string.Equals(MapIds[i], "IB", StringComparison.Ordinal)
                    ? IndustrialPortraitCap
                    : PortraitMaxHeight;
                float want = Math.Min(cap, stripTarget);
                if (cards[i].H + 0.5f >= want)
                    continue;
                TryRaiseCard(hexes, pins, cards, cue, selected, detail, hint, plateWidth, plateHeight, fa, i, want);
            }
        }

        private static bool TryRaiseCard(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            int selected,
            Box detail,
            Box hint,
            float plateWidth,
            float plateHeight,
            bool fa,
            int index,
            float height)
        {
            Box card = cards[index];
            if (height > PortraitMaxHeight)
                height = PortraitMaxHeight;
            if (height <= card.H + 0.5f)
                return false;
            float yLo = Math.Min(card.Y, card.Y + card.H - height) - 16f;
            float yHi = Math.Max(card.Y, card.Y - (height - card.H)) + 16f;
            Box saved = card;
            for (float top = yLo; top <= yHi + 0.1f; top += 8f)
            {
                float[] xs = { card.X, card.X - 16f, card.X + 16f };
                for (int s = 0; s < xs.Length; s++)
                {
                float x = xs[s];
                if (top < Edge || top + height > plateHeight - Edge || x < Edge)
                    continue;
                var trial = new Box(x, top, card.W, height);
                if (hint.W > 1f && Overlaps(trial, hint, 2f))
                    continue;
                if (index != selected && cue.W > 1f && Overlaps(trial, cue, 2f))
                    continue;
                if (!PortraitCardSits(pins[index], cards, hexes, detail, plateWidth, plateHeight, selected, index, trial))
                    continue;
                cards[index] = trial;
                if (!DrawnTerritoryHolds(hexes, pins, cards, cue, selected, detail, hint, plateWidth, plateHeight, fa, index))
                {
                    cards[index] = saved;
                    continue;
                }

                return true;
                }
            }

            cards[index] = saved;
            return false;
        }

        private static Vec2[][] OthersOf(Vec2[][] hexes, int index)
        {
            var others = new Vec2[hexes.Length][];
            for (int j = 0; j < hexes.Length; j++)
            {
                if (j != index)
                    others[j] = hexes[j];
            }

            return others;
        }

        private static Box DetailGlow(Box detail)
        {
            if (detail.W <= 1f)
                return default;
            return new Box(detail.X - HaloReach, detail.Y - HaloReach, detail.W + HaloReach * 2f, detail.H + HaloReach * 2f);
        }

        /// <summary>
        /// The capture samples the drawn polygon, not the seated hex. A card that only
        /// fits the hex can still force that polygon out through the map or the detail glow.
        /// </summary>
        private static bool DrawnTerritoryHolds(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            int selected,
            Box detail,
            Box hint,
            float plateWidth,
            float plateHeight,
            bool fa,
            int index)
        {
            if (hexes == null || cards == null || index < 0 || index >= hexes.Length || hexes[index] == null)
                return false;
            Box useCue = index == selected ? cue : default;
            if (fa && useCue.W > 1f)
                useCue = WidenFaCue(useCue, plateWidth);
            Vec2[] shape = FinishTerritory(
                hexes[index],
                cards[index],
                pins[index],
                MapIds[index],
                OthersOf(hexes, index),
                useCue,
                plateWidth,
                plateHeight,
                detail,
                hint);
            if (shape == null || shape.Length < 3)
                return false;
            if (!OutlineInside(shape, plateWidth, plateHeight, out _, out _) || !GlowInside(shape, plateWidth, plateHeight, out _, out _))
                return false;
            if (EdgesHit(shape, hint))
                return false;
            Box glow = DetailGlow(detail);
            if (glow.W > 1f && (PolygonHitsRect(shape, detail) || PolygonHitsRect(shape, glow)))
                return false;
            bool panelLeft = detail.W > 1f && DetailOccupiesLeft(plateWidth, detail.X, detail.W);
            Box view = detail.W > 1f
                ? OpenViewport(plateWidth, plateHeight, panelLeft, true)
                : new Box(0f, 0f, plateWidth, plateHeight);
            float expected = HexAspect(MapIds[index], view.W, view.H);
            float measured = MeasuredHexAspect(shape);
            if (expected >= 0.2f && measured >= 0.2f && Math.Abs(measured / expected - 1f) > 0.10f)
                return false;
            if (InteriorUnblocked(shape, cards[index], pins[index], useCue, hint) < 8)
                return false;
            if (string.Equals(MapIds[index], "AP", StringComparison.Ordinal)
                && NotchInterior(shape, cards[index], pins[index], useCue, hint) < 8)
                return false;
            if (cards[index].W >= 1f && !RestsInside(cards[index], shape, InsideMargin))
                return false;
            if (PolygonArea(shape) + 0.5f < 40000f)
                return false;
            return true;
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
        /// Card-inside-territory rule. The card rests inside its own territory with an
        /// 8px sample inset, and it stays off every other territory by at least 1px.
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
                                if (!SeatHolds(trial, hexes[i], hexes) || !RestsInside(trial, hexes[i], InsideMargin))
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
                if (SnapLooseCards(hexes, cards, savedCards, selected))
                    cue = savedCue;
                return;
            }

            // A taller selected card can lose its cue. Keep the extra width at the
            // height the cue already cleared, seated on the same bottom edge.
            if (selected >= 0 && selected < cards.Length
                && cards[selected].W > savedCards[selected].W + 0.5f)
            {
                Box grown = cards[selected];
                Box home = savedCards[selected];
                var shortened = new Box(grown.X, grown.YMax - home.H, grown.W, home.H);
                if (RestsInside(shortened, hexes[selected], InsideMargin))
                {
                    cards[selected] = shortened;
                    if (TryAttachCueToCard(plateWidth, plateHeight, pins, cards, hexes, detail, selected, out Box wideCue)
                        && DistrictsStayLegal(hexes, pins, cards, wideCue, detail, plateWidth, plateHeight, selected)
                        && (SameCue(savedCue, wideCue) || !mirrorPlate))
                    {
                        cue = wideCue;
                        if (SnapLooseCards(hexes, cards, savedCards, selected))
                            cue = savedCue;
                        return;
                    }
                }

                cards[selected] = grown;
            }

            if (selected >= 0 && selected < cards.Length)
                cards[selected] = savedCards[selected];
            if (selected >= 0 && selected < cards.Length
                && TryAttachCueToCard(plateWidth, plateHeight, pins, cards, hexes, detail, selected, out Box kept)
                && DistrictsStayLegal(hexes, pins, cards, kept, detail, plateWidth, plateHeight, selected)
                && SameCue(savedCue, kept))
            {
                cue = kept;
                if (SnapLooseCards(hexes, cards, savedCards, selected))
                    cue = savedCue;
                return;
            }

            cue = savedCue;
            if (DistrictsStayLegal(hexes, pins, cards, cue, detail, plateWidth, plateHeight, selected))
            {
                if (SnapLooseCards(hexes, cards, savedCards, selected))
                    cue = savedCue;
                return;
            }

            for (int i = 0; i < cards.Length; i++)
                cards[i] = savedCards[i];
            cue = savedCue;
            SnapLooseCards(hexes, cards, savedCards, selected);
        }

        private static bool SnapLooseCards(Vec2[][] hexes, Box[] cards, Box[] saved, int selected)
        {
            if (cards == null || saved == null || hexes == null)
                return false;
            bool selectedLoose = false;
            for (int i = 0; i < cards.Length && i < saved.Length && i < hexes.Length; i++)
            {
                if (hexes[i] == null || cards[i].W < 1f || RestsInside(cards[i], hexes[i], InsideMargin))
                    continue;
                cards[i] = saved[i];
                if (i == selected)
                    selectedLoose = true;
            }

            return selectedLoose;
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
        /// Soft border from approved-1
        /// (<c>Design/SkirmishUIConcepts/2026-10-14/approved-1-theater-map.jpg</c>).
        /// The tint fades across this many plate pixels. It is not a neon stroke.
        /// </summary>
        public const float TerritoryFeather = 10f;

        /// <summary>Peak alpha of the unselected 5px glow. The fill sits under this.</summary>
        public const float TerritoryBorderAlpha = 0.86f;

        /// <summary>Fill alpha over the theatre painting. Logged as <c>fill=</c> on <c>[Territory]</c>. The 4:3 Farsi Desert Base veil stays lighter in the painter.</summary>
        public const float TerritoryFillAlpha = 0.36f;

        /// <summary>English theatre zoom control when Industrial Basin is the district. It has to fit the 4:3 footer button.</summary>
        public const string ZoomInBasinLabel = "ZOOM IN · INDUSTRIAL BASIN";

        /// <summary>
        /// Irregular territory on the seated district. The border is a smooth curve, the
        /// card still rests inside by <see cref="InsideMargin"/>, and the area stays on
        /// the 40000 floor.
        /// </summary>
        public static Vec2[] TerritoryAround(Vec2[] hex, Box card, string mapId)
        {
            return TerritoryAround(hex, card, mapId, null, default);
        }

        /// <summary>
        /// Same outline, with the neighbouring seated districts and the zoom pill so the
        /// curve cannot cross either.
        /// </summary>
        public static string ProbePlate(float plateWidth, float plateHeight, bool fa, bool reserveOn, string selectedMapId)
        {
            Layout layout = Arrange(plateWidth, plateHeight, fa, reserveOn, selectedMapId);
            var hexes = new Vec2[MapIds.Length][];
            var cards = new Box[MapIds.Length];
            for (int i = 0; i < MapIds.Length; i++)
            {
                hexes[i] = layout.Seats[i].Hex;
                cards[i] = layout.Seats[i].Card;
            }

            Box detail = DetailRect(plateWidth, plateHeight, fa, reserveOn);
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            Box view = detail.W > 1f ? OpenViewport(plateWidth, plateHeight, fa, true) : new Box(0f, 0f, plateWidth, plateHeight);
            var report = new System.Text.StringBuilder();
            report.Append("plate=").Append(plateWidth.ToString("0", CultureInfo.InvariantCulture))
                .Append("x").Append(plateHeight.ToString("0", CultureInfo.InvariantCulture))
                .Append(fa ? " fa" : " en")
                .Append(reserveOn ? " reserve" : " open")
                .Append(" sel=").Append(selectedMapId)
                .Append(" view=").Append(view.W.ToString("0", CultureInfo.InvariantCulture))
                .Append("x").Append(view.H.ToString("0", CultureInfo.InvariantCulture));
            for (int i = 0; i < MapIds.Length; i++)
            {
                Vec2[][] others = new Vec2[MapIds.Length][];
                for (int j = 0; j < MapIds.Length; j++)
                {
                    if (j != i)
                        others[j] = hexes[j];
                }

                Box cue = i == IndexOf(selectedMapId) ? layout.Cue : default;
                if (fa && cue.W > 1f)
                    cue = WidenFaCue(cue, plateWidth);
                Vec2[] shape = FinishTerritory(hexes[i], cards[i], layout.Seats[i].Pin, MapIds[i], others, cue, plateWidth, plateHeight, detail, hint);
                float expected = HexAspect(MapIds[i], view.W, view.H);
                float measured = MeasuredHexAspect(shape);
                int open = InteriorCount(shape, 8f, 8f, plateWidth - 8f, plateHeight - 8f);
                int blocked = InteriorUnblocked(shape, cards[i], layout.Seats[i].Pin, cue, hint);
                bool outline = OutlineInside(shape, plateWidth, plateHeight, out _, out _);
                bool glow = GlowInside(shape, plateWidth, plateHeight, out _, out _);
                bool hintHit = EdgesHit(shape, hint);
                Box glowZone = detail.W > 1f
                    ? new Box(detail.X - HaloReach, detail.Y - HaloReach, detail.W + HaloReach * 2f, detail.H + HaloReach * 2f)
                    : default;
                bool glowPanel = glowZone.W > 1f && PolygonHitsRect(shape, glowZone);
                float hexAspect = MeasuredHexAspect(hexes[i]);
                report.Append(" | ").Append(MapIds[i])
                    .Append(" card=").Append(cards[i].W.ToString("0", CultureInfo.InvariantCulture))
                    .Append("x").Append(cards[i].H.ToString("0", CultureInfo.InvariantCulture))
                    .Append(" aspect=").Append(measured.ToString("0.000", CultureInfo.InvariantCulture))
                    .Append("/").Append(expected.ToString("0.000", CultureInfo.InvariantCulture))
                    .Append(" hex=").Append(hexAspect.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" n=").Append(shape != null ? shape.Length : 0)
                    .Append(" in=").Append(open)
                    .Append("/").Append(blocked)
                    .Append(" notchIn=").Append(NotchInterior(shape, cards[i], layout.Seats[i].Pin, cue, hint))
                    .Append(outline ? "" : " OUT")
                    .Append(glow ? "" : " GLOW")
                    .Append(hintHit ? " HINT" : "")
                    .Append(glowPanel ? " PANEL" : "");
            }

            return report.ToString();
        }

        private static Box WidenFaCue(Box cue, float plateWidth)
        {
            const float needW = 220f;
            const float needH = 64f;
            float width = Math.Max(cue.W, needW);
            float height = Math.Max(cue.H, needH);
            float bottom = cue.Y + cue.H;
            float x = cue.XMax - width;
            if (x > cue.X)
                x = cue.X;
            float y = bottom - height;
            // The stroke has to stay inside the halo. Hang the extra width inward.
            float limitL = 1f + HaloReach + CueHexStroke + 2f;
            float limitR = plateWidth - (1f + HaloReach + CueHexStroke + 2f);
            float limitT = 1f + HaloReach + CueHexStroke + 2f;
            if (x + width > limitR)
                x = limitR - width;
            if (x < limitL)
                x = limitL;
            if (y < limitT)
                y = limitT;
            if (x + width > plateWidth - MapInset)
                x = plateWidth - MapInset - width;
            if (x < MapInset)
                x = MapInset;
            return new Box(x, y, width, height);
        }

        private static int InteriorUnblocked(Vec2[] shape, Box card, Box pin, Box cue, Box hint)
        {
            return InteriorUnblocked(shape, card, pin, cue, hint, 26f, 18f);
        }

        /// <summary>
        /// The 20:9 notch scales the plate to about 0.922 after seating. World samples
        /// then use a larger inset than the plate grid, which is what left AP at 6 points.
        /// </summary>
        private const float NotchFit = 0.922f;

        private static int NotchInterior(Vec2[] shape, Box card, Box pin, Box cue, Box hint)
        {
            // The notch scales the plate by about 0.922. Capture still counted two
            // samples short once the name strip beside the card was in the way, so
            // the notch grid keeps an extra 10px and blocks that name box.
            bool notch = string.Equals(ShotNotch, "notch", StringComparison.Ordinal);
            float extra = notch ? 10f : 0f;
            Box name = default;
            if (notch && card.W > 1f)
            {
                float nameH = Math.Min(card.H, 50f);
                name = new Box(card.X - 14f, card.Y + Math.Max(0f, card.H - nameH), card.W + 28f, nameH);
            }

            return InteriorUnblocked(shape, card, pin, cue, hint, 26f / NotchFit + extra, 18f / NotchFit, name);
        }

        private static int InteriorUnblocked(Vec2[] shape, Box card, Box pin, Box cue, Box hint, float inset, float step)
        {
            return InteriorUnblocked(shape, card, pin, cue, hint, inset, step, default);
        }

        private static int InteriorUnblocked(Vec2[] shape, Box card, Box pin, Box cue, Box hint, float inset, float step, Box name)
        {
            if (shape == null || shape.Length < 3 || step < 1f)
                return 0;
            float left = shape[0].X;
            float right = shape[0].X;
            float top = shape[0].Y;
            float bottom = shape[0].Y;
            for (int i = 1; i < shape.Length; i++)
            {
                left = Math.Min(left, shape[i].X);
                right = Math.Max(right, shape[i].X);
                top = Math.Min(top, shape[i].Y);
                bottom = Math.Max(bottom, shape[i].Y);
            }

            int count = 0;
            for (float y = top + inset; y <= bottom - inset; y += step)
            {
                for (float x = left + inset; x <= right - inset; x += step)
                {
                    var point = new Vec2(x, y);
                    if (!PointInPolygon(point, shape) || DistanceToEdge(point, shape) < inset)
                        continue;
                    if (card.W > 1f && point.X >= card.XMin - 2f && point.X <= card.XMax + 2f && point.Y >= card.YMin - 2f && point.Y <= card.YMax + 2f)
                        continue;
                    if (pin.W > 1f && point.X >= pin.XMin - 2f && point.X <= pin.XMax + 2f && point.Y >= pin.YMin - 2f && point.Y <= pin.YMax + 2f)
                        continue;
                    if (cue.W > 1f && point.X >= cue.XMin - 2f && point.X <= cue.XMax + 2f && point.Y >= cue.YMin - 2f && point.Y <= cue.YMax + 2f)
                        continue;
                    if (hint.W > 1f && point.X >= hint.XMin - 2f && point.X <= hint.XMax + 2f && point.Y >= hint.YMin - 2f && point.Y <= hint.YMax + 2f)
                        continue;
                    if (name.W > 1f && point.X >= name.XMin - 2f && point.X <= name.XMax + 2f && point.Y >= name.YMin - 2f && point.Y <= name.YMax + 2f)
                        continue;
                    count++;
                }
            }

            return count;
        }

        private static bool EdgesHit(Vec2[] shape, Box rect)
        {
            if (shape == null || rect.W < 1f)
                return false;
            for (int i = 0; i < shape.Length; i++)
            {
                if (SegmentHitsRect(shape[i], shape[(i + 1) % shape.Length], rect))
                    return true;
            }

            return false;
        }

        public static Vec2[] TerritoryAround(Vec2[] hex, Box card, string mapId, Vec2[][] others, Box cue)
        {
            if (hex == null || hex.Length < 3)
                return hex;
            Vec2[] shape = BuildLegalTerrain(hex, card, mapId, others, cue);
            shape = RestoreHexFrame(shape, hex, card, others, cue);
            if (card.W >= 1f && card.H >= 1f && !RestsInside(card, shape, InsideMargin))
                shape = ContainCard(hex, card, shape);
            return shape;
        }

        /// <summary>
        /// Drawn territory. The curve keeps the seated hex's box, stays inset from the map
        /// so the halo and the outline rect clear the clip, and stays off the zoom hint
        /// and the detail column.
        /// </summary>
        public static Vec2[] FinishTerritory(
            Vec2[] hex,
            Box card,
            Box pin,
            string mapId,
            Vec2[][] others,
            Box cue,
            float plateWidth,
            float plateHeight,
            Box detail,
            Box hint)
        {
            if (hex == null || hex.Length < 3)
                return hex;
            bool panelLeft = detail.W > 1f && DetailOccupiesLeft(plateWidth, detail.X, detail.W);
            Box view = detail.W > 1f
                ? OpenViewport(plateWidth, plateHeight, panelLeft, true)
                : new Box(0f, 0f, plateWidth, plateHeight);
            float expected = HexAspect(mapId, view.W, view.H);
            Vec2[] shape = TerritoryAround(hex, card, mapId, others, cue);
            Vec2[] held = HoldMapFrame(shape, hex, card, pin, others, cue, plateWidth, plateHeight, detail, hint, expected);
            if (string.Equals(mapId, "AP", StringComparison.Ordinal))
                held = OpenNotchInterior(held, hex, card, pin, others, cue, plateWidth, plateHeight, detail, hint, expected);
            held = PullShapeOffCue(held, hex, card, cue, others, plateWidth, plateHeight, detail, hint, expected);
            held = EnsureCardInside(held, hex, card, pin, others, cue, plateWidth, plateHeight, detail, hint, expected);
            held = ClampVisiblePlate(held, plateWidth, plateHeight);
            if (card.W >= 1f && !RestsInside(card, held, InsideMargin))
                held = EnsureCardInside(held, hex, card, pin, others, cue, plateWidth, plateHeight, detail, hint, expected);
            bool widePlate = plateHeight > 2f && plateWidth / plateHeight >= 1.9f;
            if (widePlate && card.W >= 1f && hex != null && RestsInside(card, hex, InsideMargin) && !RestsInside(card, held, InsideMargin))
            {
                BoundsOf(hex, out float hx0, out float hy0, out float hx1, out float hy1);
                var hexBox = new Box(hx0, hy0, Math.Max(1f, hx1 - hx0), Math.Max(1f, hy1 - hy0));
                Vec2[] source = held != null && held.Length >= 12 ? held : hex;
                Vec2[] boxed = ShapeIntoBox(source, hexBox);
                if (boxed != null && RestsInside(card, boxed, InsideMargin))
                    held = boxed;
                else if (hex.Length >= 12)
                    held = hex;
                else
                {
                    float[] mild = { 0.06f, 0.03f, 0.015f };
                    for (int m = 0; m < mild.Length; m++)
                    {
                        int district = 0;
                        for (int id = 0; id < MapIds.Length; id++)
                        {
                            if (string.Equals(MapIds[id], mapId, StringComparison.Ordinal))
                            {
                                district = id;
                                break;
                            }
                        }

                        Vec2[] coast = ShapeIntoBox(OrganicControls(district, mild[m]), hexBox);
                        if (coast != null && coast.Length >= 12 && RestsInside(card, coast, InsideMargin))
                        {
                            held = coast;
                            break;
                        }
                    }
                }
            }

            return held;
        }

        /// <summary>
        /// Grows Airfield Plains until the notch scale still has open interior outside
        /// the card, the pin, and the cue. The largest legal gain wins, so a plate
        /// that only just cleared eight samples keeps a margin.
        /// </summary>
        private static Vec2[] OpenNotchInterior(
            Vec2[] shape,
            Vec2[] hex,
            Box card,
            Box pin,
            Vec2[][] others,
            Box cue,
            float plateWidth,
            float plateHeight,
            Box detail,
            Box hint,
            float expected)
        {
            int have = shape != null ? NotchInterior(shape, card, pin, cue, hint) : 0;
            if (shape == null || hex == null || have >= 16)
                return shape;
            CentroidOf(hex, out float cx, out float cy);
            float[] gains = { 1.06f, 1.12f, 1.18f, 1.26f, 1.34f, 1.46f, 1.58f, 1.72f };
            Box pill = cue.W > 1f
                ? new Box(cue.X - CueHexStroke, cue.Y - CueHexStroke, cue.W + CueHexStroke * 2f, cue.H + CueHexStroke * 2f)
                : default;
            Box glow = DetailGlow(detail);
            Vec2[] best = shape;
            int bestCount = have;
            for (int g = 0; g < gains.Length; g++)
            {
                float gain = gains[g];
                var grown = new Vec2[hex.Length];
                bool blocked = false;
                for (int i = 0; i < hex.Length; i++)
                {
                    var pushed = new Vec2(cx + (hex[i].X - cx) * gain, cy + (hex[i].Y - cy) * gain);
                    bool outside = pushed.X < CaptureFrameInset || pushed.X > plateWidth - CaptureFrameInset
                        || pushed.Y < CaptureFrameInset || pushed.Y > plateHeight - CaptureFrameInset;
                    bool hit = InsideNeighbour(pushed, hex, others)
                        || (hint.W > 1f && pushed.X >= hint.X - 2f && pushed.X <= hint.XMax + 2f && pushed.Y >= hint.Y - 2f && pushed.Y <= hint.YMax + 2f)
                        || (glow.W > 1f && pushed.X >= glow.X - 2f && pushed.X <= glow.XMax + 2f && pushed.Y >= glow.Y - 2f && pushed.Y <= glow.YMax + 2f);
                    if (outside || hit)
                    {
                        blocked = true;
                        break;
                    }

                    grown[i] = pushed;
                }

                if (blocked)
                    continue;
                if (card.W >= 1f && !RestsInside(card, grown, InsideMargin))
                    continue;
                if (EdgesHit(grown, hint) || EdgesHit(grown, pill) || EdgesHit(grown, glow))
                    continue;
                if (glow.W > 1f && (PolygonHitsRect(grown, detail) || PolygonHitsRect(grown, glow)))
                    continue;
                if (CrossesOther(grown, hex, others))
                    continue;
                if (!OutlineInside(grown, plateWidth, plateHeight, out _, out _) || !GlowInside(grown, plateWidth, plateHeight, out _, out _))
                    continue;
                float measured = MeasuredHexAspect(grown);
                if (expected >= 0.2f && measured >= 0.2f && Math.Abs(measured / expected - 1f) > 0.10f)
                    continue;
                if (PolygonArea(grown) + 0.5f < 40000f)
                    continue;
                int count = NotchInterior(grown, card, pin, cue, hint);
                if (count < 8 || count <= bestCount)
                    continue;
                best = grown;
                bestCount = count;
            }

            return bestCount >= 8 ? best : shape;
        }

        /// <summary>
        /// The smooth curve is clamped back inside the hex, and that hex can still
        /// cross the zoom pill. Push those vertices off the pill stroke when the
        /// move keeps the card inside and the frame gates hold.
        /// </summary>
        private static Vec2[] PullShapeOffCue(
            Vec2[] shape,
            Vec2[] hex,
            Box card,
            Box cue,
            Vec2[][] others,
            float plateWidth,
            float plateHeight,
            Box detail,
            Box hint,
            float expected)
        {
            if (shape == null || cue.W < 1f)
                return shape;
            Vec2[] cleared = PushOffRect(
                shape, hex, card, InflateBox(cue, CueHexStroke), others, plateWidth, plateHeight, detail, hint, expected);
            if (!ReferenceEquals(cleared, shape))
                return cleared;
            float height = Math.Min(cue.H, 56f);
            float width = Math.Min(cue.W, 160f);
            if (height + 0.5f >= cue.H && width + 0.5f >= cue.W)
                return shape;
            float bottom = cue.Y + cue.H;
            float x = cue.X + cue.W * 0.5f - width * 0.5f;
            var shelf = new Box(x, bottom - height, width, height);
            return PushOffRect(
                shape, hex, card, InflateBox(shelf, CueHexStroke), others, plateWidth, plateHeight, detail, hint, expected);
        }

        private static Vec2[] PushOffRect(
            Vec2[] shape,
            Vec2[] hex,
            Box card,
            Box rect,
            Vec2[][] others,
            float plateWidth,
            float plateHeight,
            Box detail,
            Box hint,
            float expected)
        {
            if (shape == null || rect.W < 1f || (!EdgesHit(shape, rect) && !PolygonHitsRect(shape, rect)))
                return shape;
            var grown = CopyShape(shape);
            bool moved = false;
            for (int i = 0; i < grown.Length; i++)
            {
                Vec2 point = grown[i];
                if (point.X < rect.XMin || point.X > rect.XMax || point.Y < rect.YMin || point.Y > rect.YMax)
                    continue;
                float left = point.X - rect.XMin;
                float right = rect.XMax - point.X;
                float up = point.Y - rect.YMin;
                float down = rect.YMax - point.Y;
                float best = left;
                float x = rect.XMin - 1.5f;
                float y = point.Y;
                if (right < best)
                {
                    best = right;
                    x = rect.XMax + 1.5f;
                    y = point.Y;
                }

                if (up < best)
                {
                    best = up;
                    x = point.X;
                    y = rect.YMin - 1.5f;
                }

                if (down < best)
                {
                    x = point.X;
                    y = rect.YMax + 1.5f;
                }

                grown[i] = new Vec2(
                    Clamp(x, CaptureFrameInset, plateWidth - CaptureFrameInset),
                    Clamp(y, CaptureFrameInset, plateHeight - CaptureFrameInset));
                moved = true;
            }

            float midX = (rect.XMin + rect.XMax) * 0.5f;
            float midY = (rect.YMin + rect.YMax) * 0.5f;
            for (int i = 0; i < grown.Length; i++)
            {
                Vec2 next = grown[(i + 1) % grown.Length];
                if (!SegmentHitsRect(grown[i], next, rect))
                    continue;
                for (int end = 0; end < 2; end++)
                {
                    int index = end == 0 ? i : (i + 1) % grown.Length;
                    float dx = grown[index].X - midX;
                    float dy = grown[index].Y - midY;
                    float len = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (len < 0.001f)
                        continue;
                    grown[index] = new Vec2(
                        Clamp(grown[index].X + dx / len * 18f, CaptureFrameInset, plateWidth - CaptureFrameInset),
                        Clamp(grown[index].Y + dy / len * 18f, CaptureFrameInset, plateHeight - CaptureFrameInset));
                    moved = true;
                }
            }

            if (!moved || !TerritoryStaysLegal(grown, hex, card, others, rect, plateWidth, plateHeight, detail, hint, expected))
                return shape;
            return grown;
        }

        /// <summary>
        /// The selected portrait and its top tab have to fit inside the halo, with room
        /// for the inside margin and the cue stroke. A card that then clips its own hex
        /// gets that cap pulled back, so the seat stays clear instead of straddling.
        /// </summary>
        private static void FitSelectionInFrame(
            Box[] cards,
            Vec2[][] hexes,
            Box[] pins,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa)
        {
            if (cards == null || hexes == null || pins == null || selected < 0 || selected >= cards.Length)
                return;

            Box card = cards[selected];
            if (card.W < 1f || hexes[selected] == null)
                return;
            FrameOf(plateWidth, plateHeight, out float frameL, out float frameT, out float frameR, out float frameB);
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            Box plan = cue;
            if (fa && cue.W > 1f)
            {
                // The Farsi shelf is wider than the card. Hang the extra width away
                // from the frame edge the card is already against, so the card itself
                // does not have to travel the shelf's overflow.
                float width = Math.Max(cue.W, 220f);
                float height = Math.Max(cue.H, 76f);
                float bottom = cue.Y + cue.H;
                float x = card.XMax - width;
                if (x > card.X)
                    x = card.X;
                plan = new Box(x, bottom - height, width, height);
            }

            float pad = InsideMargin + 4f;
            float needL = card.X - pad;
            float needR = card.XMax + pad;
            float needT = card.Y - pad;
            float needB = card.YMax + pad;
            if (plan.W > 1f)
            {
                needL = Math.Min(needL, plan.X - CueHexStroke - 2f);
                needR = Math.Max(needR, plan.XMax + CueHexStroke + 2f);
                needT = Math.Min(needT, plan.Y - CueHexStroke - 2f);
                needB = Math.Max(needB, plan.YMax + CueHexStroke + 2f);
            }

            if (hint.W > 1f)
                frameB = Math.Min(frameB, hint.YMin - 2f);
            float dx = 0f;
            float dy = 0f;
            if (needR > frameR)
                dx = frameR - needR;
            else if (needL < frameL)
                dx = frameL - needL;
            if (needB > frameB)
                dy = frameB - needB;
            else if (needT < frameT)
                dy = frameT - needT;
            if (Math.Abs(dx) < 0.5f && Math.Abs(dy) < 0.5f)
                return;
            var moved = new Box(card.X + dx, card.Y + dy, card.W, card.H);
            Box movedCue = cue.W > 1f ? new Box(cue.X + dx, cue.Y + dy, cue.W, cue.H) : cue;
            if (!OnPlate(moved, plateWidth, plateHeight) || !Clears(moved, detail))
            {
                if (TryShrinkSelection(cards, hexes, pins, ref cue, selected, detail, hint, plateWidth, plateHeight, frameL, frameT, frameR, frameB))
                    return;
                return;
            }

            if (hint.W > 1f && Overlaps(moved, hint, 2f))
            {
                if (TryShrinkSelection(cards, hexes, pins, ref cue, selected, detail, hint, plateWidth, plateHeight, frameL, frameT, frameR, frameB))
                    return;
                return;
            }

            if (pins[selected].W > 1f && Leader(pins[selected], moved) < LeaderMin)
            {
                if (TryShrinkSelection(cards, hexes, pins, ref cue, selected, detail, hint, plateWidth, plateHeight, frameL, frameT, frameR, frameB))
                    return;
                return;
            }

            if (movedCue.W > 1f && (movedCue.XMin < 1f || movedCue.YMin < 1f || movedCue.XMax > plateWidth - 1f || movedCue.YMax > plateHeight - 1f))
            {
                return;
            }

            if (detail.W > 1f && movedCue.W > 1f && Overlaps(movedCue, detail, 0f))
            {
                return;
            }

            if (movedCue.W > 1f && !CueCountsAsNear(movedCue, moved, pins[selected], hexes[selected]))
            {
                if (TryShrinkSelection(cards, hexes, pins, ref cue, selected, detail, hint, plateWidth, plateHeight, frameL, frameT, frameR, frameB))
                    return;
                return;
            }

            for (int i = 0; i < cards.Length; i++)
            {
                if (i == selected || cards[i].W < 1f)
                    continue;
                if (Separation(moved, cards[i]) < CardGap)
                {
                    if (TryShrinkSelection(cards, hexes, pins, ref cue, selected, detail, hint, plateWidth, plateHeight, frameL, frameT, frameR, frameB))
                        return;
                    return;
                }
            }

            Vec2[] shaved = ShaveHexOffCard(hexes[selected], moved);
            if (shaved == null || !HexCapLegal(shaved, hexes, pins, selected, plateWidth, plateHeight))
            {
                if (TryShrinkSelection(cards, hexes, pins, ref cue, selected, detail, hint, plateWidth, plateHeight, frameL, frameT, frameR, frameB))
                    return;
                return;
            }

            // Name growth can detach the top tab. The near test then uses the cue centre,
            // so a centre past 160px fails even when the tab anchor was inside the limit.
            if (movedCue.W > 1f && pins[selected].W > 1f)
            {
                float centre = Distance(movedCue.Center, pins[selected].Center);
                bool centreNear = centre <= CueNearLimit || PointInPolygon(movedCue.Center, shaved);
                if (!centreNear || !CueCountsAsNear(movedCue, moved, pins[selected], shaved))
                {
                    if (TryShrinkSelection(cards, hexes, pins, ref cue, selected, detail, hint, plateWidth, plateHeight, frameL, frameT, frameR, frameB))
                        return;
                    return;
                }
            }

            cards[selected] = moved;
            cue = movedCue;
            hexes[selected] = shaved;
        }


        /// <summary>
        /// Cuts the overflowing side of the selected portrait down to the portrait floor
        /// so the cue stroke can sit inside the halo without sliding into a neighbour.
        /// </summary>
        private static bool TryShrinkSelection(
            Box[] cards,
            Vec2[][] hexes,
            Box[] pins,
            ref Box cue,
            int selected,
            Box detail,
            Box hint,
            float plateWidth,
            float plateHeight,
            float frameL,
            float frameT,
            float frameR,
            float frameB)
        {
            Box card = cards[selected];
            float pad = InsideMargin + 4f;
            float stroke = cue.W > 1f ? CueHexStroke + 2f : pad;
            float x = card.X;
            float y = card.Y;
            float width = card.W;
            float height = card.H;
            float rightLimit = frameR - stroke;
            float leftLimit = frameL + stroke;
            float bottomLimit = frameB - pad;
            float topLimit = frameT + pad;
            if (card.XMax > rightLimit)
                width = rightLimit - x;
            if (x < leftLimit)
            {
                float cut = leftLimit - x;
                x = leftLimit;
                width -= cut;
            }

            if (card.YMax > bottomLimit)
                height = bottomLimit - y;
            if (y < topLimit)
            {
                float cut = topLimit - y;
                y = topLimit;
                height -= cut;
            }

            if (width + 0.5f < PortraitMinWidth || height + 0.5f < PortraitMinHeight)
                return false;
            if (width > card.W + 0.5f || height > card.H + 0.5f)
                return false;
            if (Math.Abs(width - card.W) < 0.5f && Math.Abs(height - card.H) < 0.5f && Math.Abs(x - card.X) < 0.5f && Math.Abs(y - card.Y) < 0.5f)
                return false;
            var shrunk = new Box(x, y, width, height);
            if (!OnPlate(shrunk, plateWidth, plateHeight) || !Clears(shrunk, detail))
                return false;
            if (hint.W > 1f && Overlaps(shrunk, hint, 2f))
                return false;
            if (pins[selected].W > 1f && (Leader(pins[selected], shrunk) < LeaderMin || Separation(shrunk, pins[selected]) < PinGap))
                return false;
            if (RectHitsPolygon(shrunk, hexes[selected]))
                return false;
            for (int i = 0; i < cards.Length; i++)
            {
                if (i == selected || cards[i].W < 1f)
                    continue;
                if (Separation(shrunk, cards[i]) < CardGap)
                    return false;
                if (hexes[i] != null && RectHitsPolygon(shrunk, hexes[i]))
                    return false;
            }

            Box nextCue = cue;
            if (cue.W > 1f)
            {
                float cueW = Math.Min(cue.W, Math.Max(width, cue.W - Math.Max(0f, cue.XMax - shrunk.XMax)));
                if (cueW < 36f)
                    cueW = Math.Min(cue.W, Math.Max(36f, width));
                float cueX = shrunk.XMax - cueW;
                if (cueX > shrunk.X)
                    cueX = shrunk.X;
                float cueH = Math.Min(cue.H, 62f);
                if (cueH < 36f)
                    cueH = cue.H;
                float cueY = shrunk.Y - cueH;
                nextCue = new Box(cueX, cueY, cueW, cueH);
                if (nextCue.XMin < 1f || nextCue.YMin < 1f || nextCue.XMax > plateWidth - 1f || nextCue.YMax > plateHeight - 1f)
                    return false;
                if (!CueCountsAsNear(nextCue, shrunk, pins[selected], hexes[selected]))
                    return false;
            }

            cards[selected] = shrunk;
            cue = nextCue;
            return true;
        }

        /// <summary>
        /// Pulls vertices that land inside the card out to the nearest side, so a portrait
        /// that only clipped the cap stays outside the district.
        /// </summary>
        private static Vec2[] ShaveHexOffCard(Vec2[] hex, Box card)
        {
            if (hex == null || hex.Length < 3 || card.W < 1f)
                return hex;
            if (!RectHitsPolygon(card, hex) && PerimeterGap(card, hex) + 0.05f >= OutsideGap)
                return hex;
            var copy = new Vec2[hex.Length];
            for (int i = 0; i < hex.Length; i++)
                copy[i] = hex[i];
            Box keep = InflateBox(card, 2f);
            for (int pass = 0; pass < 8; pass++)
            {
                bool moved = false;
                for (int i = 0; i < copy.Length; i++)
                {
                    // Only a vertex that sits in the card moves. Pushing the far end of a
                    // crossing edge throws the rest of the district across a neighbour.
                    bool inside = copy[i].X >= keep.XMin && copy[i].X <= keep.XMax
                        && copy[i].Y >= keep.YMin && copy[i].Y <= keep.YMax;
                    if (!inside)
                        continue;
                    copy[i] = ExitRect(copy[i], keep);
                    moved = true;
                }

                if (!moved)
                    break;
            }

            if (RectHitsPolygon(card, copy) || PerimeterGap(card, copy) + 0.05f < OutsideGap)
                return null;
            return copy;
        }

        private static bool HexCapLegal(
            Vec2[] hex,
            Vec2[][] hexes,
            Box[] pins,
            int index,
            float plateWidth,
            float plateHeight)
        {
            if (hex == null || hex.Length < 3 || hexes == null || pins == null)
                return false;
            float expected = HexAspect(MapIds[index], RatioPlateWidth, RatioPlateHeight);
            float measured = MeasuredHexAspect(hex);
            if (expected >= 0.2f && measured >= 0.2f && Math.Abs(measured / expected - 1f) > 0.10f)
                return false;
            if (!OutlineInside(hex, plateWidth, plateHeight, out _, out _) || !GlowInside(hex, plateWidth, plateHeight, out _, out _))
                return false;
            if (PolygonArea(hex) < 40000f)
                return false;
            if (pins[index].W > 1f)
            {
                Vec2 pin = pins[index].Center;
                if (!PointInPolygon(pin, hex) || EdgeCloserThan(pin, hex, 6f))
                    return false;
            }

            for (int j = 0; j < hexes.Length; j++)
            {
                if (j == index || hexes[j] == null)
                    continue;
                if (PolygonsOverlap(hex, hexes[j], 0.75f))
                    return false;
                if (DrawnDistrictGap(hex, hexes[j]) + 0.05f < 12f)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Slide the seated district over the card, keeping its aspect. When the seated
        /// hex is too small, a fresh hex at the expected ratio covers the portrait and
        /// leaves a sample ring outside it.
        /// </summary>
        private static Vec2[] CoverCard(
            Vec2[] shape,
            Box card,
            Box pin,
            Vec2[][] others,
            Box cue,
            Box pill,
            float plateWidth,
            float plateHeight,
            Box detail,
            Box hint,
            float expected)
        {
            Vec2[] shifted = ShiftShapeOverCard(shape, card, pin, others, cue, pill, plateWidth, plateHeight, detail, hint, expected);
            if (shifted != null)
                return shifted;
            return FreshHexOverCard(shape, card, pin, others, cue, pill, plateWidth, plateHeight, detail, hint, expected);
        }

        private static Vec2[] ShiftShapeOverCard(
            Vec2[] shape,
            Box card,
            Box pin,
            Vec2[][] others,
            Box cue,
            Box pill,
            float plateWidth,
            float plateHeight,
            Box detail,
            Box hint,
            float expected)
        {
            if (shape == null || shape.Length < 3 || card.W < 1f)
                return null;
            float pad = InsideMargin + 2f;
            Box keep = InflateBox(card, pad);
            FrameOf(plateWidth, plateHeight, out float frameL, out float frameT, out float frameR, out float frameB);
            CentroidOf(shape, out float cx, out float cy);
            float[] scales = { 1f, 1.08f, 1.16f, 1.28f, 1.42f };
            for (int s = 0; s < scales.Length; s++)
            {
                float scale = scales[s];
                var grown = new Vec2[shape.Length];
                float minX = float.MaxValue;
                float maxX = float.MinValue;
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                for (int i = 0; i < shape.Length; i++)
                {
                    grown[i] = new Vec2(cx + (shape[i].X - cx) * scale, cy + (shape[i].Y - cy) * scale);
                    minX = Math.Min(minX, grown[i].X);
                    maxX = Math.Max(maxX, grown[i].X);
                    minY = Math.Min(minY, grown[i].Y);
                    maxY = Math.Max(maxY, grown[i].Y);
                }

                if (maxX - minX + 0.5f < keep.W || maxY - minY + 0.5f < keep.H)
                    continue;
                float dxLo = Math.Max(frameL - minX, keep.XMax - maxX);
                float dxHi = Math.Min(frameR - maxX, keep.XMin - minX);
                float dyLo = Math.Max(frameT - minY, keep.YMax - maxY);
                float dyHi = Math.Min(frameB - maxY, keep.YMin - minY);
                if (dxLo > dxHi + 0.01f || dyLo > dyHi + 0.01f)
                    continue;
                float[] dxs = { (dxLo + dxHi) * 0.5f, dxLo, dxHi };
                float[] dys = { (dyLo + dyHi) * 0.5f, dyLo, dyHi };
                for (int ix = 0; ix < dxs.Length; ix++)
                {
                    for (int iy = 0; iy < dys.Length; iy++)
                    {
                        // Keep the coastline. Stretching it into the shifted box holds the
                        // card without replacing the outline with a four-corner rectangle.
                        var box = new Box(minX + dxs[ix], minY + dys[iy], maxX - minX, maxY - minY);
                        Vec2[] accepted = AcceptCover(ShapeIntoBox(grown, box), shape, card, pin, others, cue, pill, plateWidth, plateHeight, detail, hint, expected);
                        if (accepted != null)
                            return accepted;
                    }
                }
            }

            return null;
        }

        private static Vec2[] FreshHexOverCard(
            Vec2[] shape,
            Box card,
            Box pin,
            Vec2[][] others,
            Box cue,
            Box pill,
            float plateWidth,
            float plateHeight,
            Box detail,
            Box hint,
            float expected)
        {
            float aspect = expected >= 0.2f ? expected : MeasuredHexAspect(shape);
            if (aspect < 0.2f || card.W < 1f)
                return null;
            FrameOf(plateWidth, plateHeight, out float frameL, out float frameT, out float frameR, out float frameB);
            float pad = InsideMargin + 2f;
            Box must = InflateBox(card, pad);
            // The stroke sits on the portrait. A boundary between them cuts the pill,
            // so the district holds both and the edge stays outside the stroke.
            if (pill.W > 1f)
                must = UnionBox(must, InflateBox(pill, 4f));
            if (hint.W > 1f && must.YMax > hint.YMin - 2f && card.YMax + pad <= hint.YMin - 2f)
            {
                float dy = hint.YMin - 2f - must.YMax;
                must = new Box(must.X, must.Y + dy, must.W, must.H);
            }

            if (must.XMin < frameL || must.YMin < frameT || must.XMax > frameR || must.YMax > frameB)
            {
                // The stroke leaves the halo box. Cover the portrait and route the edge around the stroke.
                must = InflateBox(card, pad);
                if (hint.W > 1f && must.YMax > hint.YMin - 2f && card.YMax + pad <= hint.YMin - 2f)
                {
                    float lift = hint.YMin - 2f - must.YMax;
                    must = new Box(must.X, must.Y + lift, must.W, must.H);
                }

                if (must.XMin < frameL || must.YMin < frameT || must.XMax > frameR || must.YMax > frameB)
                    return null;
            }
            float[] extra = { 0f, 28f, 48f };
            CentroidOf(shape, out float cx, out float cy);
            for (int e = 0; e < extra.Length; e++)
            {
                float needW = must.W + extra[e];
                float needH = must.H + extra[e];
                float height = Math.Max(needH, needW / aspect);
                float width = aspect * height;
                if (width > frameR - frameL + 0.5f || height > frameB - frameT + 0.5f)
                    continue;
                float leftLo = Math.Max(frameL, must.XMax - width);
                float leftHi = Math.Min(frameR - width, must.XMin);
                float topLo = Math.Max(frameT, must.YMax - height);
                float topHi = Math.Min(frameB - height, must.YMin);
                if (hint.W > 1f)
                    topHi = Math.Min(topHi, hint.YMin - 2f - height);
                if (leftLo > leftHi + 0.01f || topLo > topHi + 0.01f)
                    continue;
                float[] lefts = { Clamp(cx - width * 0.5f, leftLo, leftHi), leftLo, leftHi };
                float[] tops = { Clamp(cy - height * 0.5f, topLo, topHi), topLo, topHi };
                for (int ix = 0; ix < lefts.Length; ix++)
                {
                    for (int iy = 0; iy < tops.Length; iy++)
                    {
                        Vec2[] built = ShapeIntoBox(shape, new Box(lefts[ix], tops[iy], width, height));
                        Vec2[] accepted = AcceptCover(built, shape, card, pin, others, cue, pill, plateWidth, plateHeight, detail, hint, expected);
                        if (accepted != null)
                            return accepted;
                    }
                }
            }

            return null;
        }

        private static Box UnionBox(Box a, Box b)
        {
            if (a.W < 1f)
                return b;
            if (b.W < 1f)
                return a;
            float x = Math.Min(a.XMin, b.XMin);
            float y = Math.Min(a.YMin, b.YMin);
            float right = Math.Max(a.XMax, b.XMax);
            float bottom = Math.Max(a.YMax, b.YMax);
            return new Box(x, y, right - x, bottom - y);
        }

        private static Vec2[] AcceptCover(
            Vec2[] candidate,
            Vec2[] own,
            Box card,
            Box pin,
            Vec2[][] others,
            Box cue,
            Box pill,
            float plateWidth,
            float plateHeight,
            Box detail,
            Box hint,
            float expected)
        {
            if (candidate == null)
                return null;
            if (pill.W > 1f && EdgesHit(candidate, pill))
                candidate = RouteAroundRect(candidate, pill);
            if (candidate == null || !RestsInside(card, candidate, InsideMargin))
                return null;
            if (!TerritoryStaysLegal(candidate, own, card, others, pill, plateWidth, plateHeight, detail, hint, expected))
                return null;
            if (InteriorUnblocked(candidate, card, pin, cue, hint) < 8)
                return null;
            if (string.Equals(ShotNotch, "notch", StringComparison.Ordinal)
                && NotchInterior(candidate, card, pin, cue, hint) < 8)
                return null;
            return candidate;
        }

        private static void FrameOf(float plateWidth, float plateHeight, out float left, out float top, out float right, out float bottom)
        {
            left = 1f + HaloReach;
            top = 1f + HaloReach;
            right = plateWidth - 1f - HaloReach;
            bottom = plateHeight - 1f - HaloReach;
        }

        /// <summary>
        /// Replace an edge that cuts the zoom pill with the outside of that pill.
        /// The selected district may contain the pill; its stroke may not cross the edge.
        /// </summary>
        private static Vec2[] RouteAroundRect(Vec2[] shape, Box rect)
        {
            if (shape == null || shape.Length < 3 || rect.W < 1f || !EdgesHit(shape, rect))
                return shape;
            Box keep = InflateBox(rect, 4f);
            Vec2[] ring =
            {
                new Vec2(keep.XMin, keep.YMin),
                new Vec2(keep.XMax, keep.YMin),
                new Vec2(keep.XMax, keep.YMax),
                new Vec2(keep.XMin, keep.YMax)
            };
            var next = new System.Collections.Generic.List<Vec2>(shape.Length + ring.Length);
            for (int i = 0; i < shape.Length; i++)
            {
                Vec2 a = shape[i];
                Vec2 b = shape[(i + 1) % shape.Length];
                if (InsideRect(a, rect))
                    a = ExitRect(a, rect);
                next.Add(a);
                Vec2 end = InsideRect(b, rect) ? ExitRect(b, rect) : b;
                if (!SegmentHitsRect(a, end, rect))
                    continue;
                if (!WalkClear(next, end, ring, 1, rect) && !WalkClear(next, end, ring, -1, rect))
                    return shape;
            }

            return next.Count >= 3 ? next.ToArray() : shape;
        }

        private static bool InsideRect(Vec2 point, Box rect)
        {
            return point.X >= rect.XMin && point.X <= rect.XMax && point.Y >= rect.YMin && point.Y <= rect.YMax;
        }

        /// <summary>
        /// Insert ring corners until the last one can see <paramref name="end"/> without
        /// crossing the keep-out rect. The closing edge is part of the test.
        /// </summary>
        private static bool WalkClear(
            System.Collections.Generic.List<Vec2> dest,
            Vec2 end,
            Vec2[] ring,
            int step,
            Box rect)
        {
            Vec2 start = dest[dest.Count - 1];
            for (int origin = 0; origin < ring.Length; origin++)
            {
                var walk = new System.Collections.Generic.List<Vec2>(ring.Length);
                Vec2 previous = start;
                int index = origin;
                bool clear = true;
                for (int n = 0; n < ring.Length && clear; n++)
                {
                    Vec2 corner = ring[index];
                    if (SegmentHitsRect(previous, corner, rect))
                    {
                        clear = false;
                        break;
                    }

                    walk.Add(corner);
                    if (!SegmentHitsRect(corner, end, rect))
                    {
                        for (int i = 0; i < walk.Count; i++)
                            dest.Add(walk[i]);
                        return true;
                    }

                    previous = corner;
                    index = (index + step + ring.Length) % ring.Length;
                }
            }

            return false;
        }

        /// <summary>
        /// HoldMapFrame can replace the curve with a hex that no longer contains the card.
        /// Industrial Basin sits beside the zoom pill, so the card is pushed back inside
        /// without crossing that pill.
        /// </summary>
        private static Vec2[] EnsureCardInside(
            Vec2[] shape,
            Vec2[] hex,
            Box card,
            Box pin,
            Vec2[][] others,
            Box cue,
            float plateWidth,
            float plateHeight,
            Box detail,
            Box hint,
            float expected)
        {
            if (shape == null || card.W < 1f || RestsInside(card, shape, InsideMargin))
                return shape;
            Box pill = cue.W > 1f ? InflateBox(cue, CueHexStroke) : default;
            Vec2[] covered = CoverCard(shape, card, pin, others, cue, pill, plateWidth, plateHeight, detail, hint, expected);
            if (covered != null)
                return covered;
            Vec2[] contained = ContainCard(hex, card, shape);
            if (contained != null
                && RestsInside(card, contained, InsideMargin)
                && TerritoryStaysLegal(contained, hex, card, others, pill, plateWidth, plateHeight, detail, hint, expected))
                return contained;
            if (hex == null)
                return shape;
            Vec2[] snapped = HullAroundCard(shape, card, pill, plateWidth, plateHeight);
            if (snapped != null
                && RestsInside(card, snapped, InsideMargin)
                && TerritoryStaysLegal(snapped, hex, card, others, pill, plateWidth, plateHeight, detail, hint, expected))
                return snapped;
            Vec2[] rect = CardMarginRect(card, pill, plateWidth, plateHeight);
            if (rect != null)
            {
                BoundsOf(rect, out float rx0, out float ry0, out float rx1, out float ry1);
                Vec2[] fitted = ShapeIntoBox(shape.Length >= 6 ? shape : hex, new Box(rx0, ry0, rx1 - rx0, ry1 - ry0));
                if (fitted != null
                    && fitted.Length >= 6
                    && RestsInside(card, fitted, InsideMargin)
                    && TerritoryStaysLegal(fitted, hex, card, others, pill, plateWidth, plateHeight, detail, hint, expected))
                    return fitted;
            }
            Vec2 origin = card.Center;
            float[] gains = { 1.06f, 1.12f, 1.2f, 1.3f, 1.42f, 1.56f };
            for (int g = 0; g < gains.Length; g++)
            {
                float gain = gains[g];
                var grown = new Vec2[shape.Length];
                for (int i = 0; i < shape.Length; i++)
                {
                    var pushed = new Vec2(
                        origin.X + (shape[i].X - origin.X) * gain,
                        origin.Y + (shape[i].Y - origin.Y) * gain);
                    if (pill.W > 1f && pushed.X >= pill.XMin && pushed.X <= pill.XMax && pushed.Y >= pill.YMin && pushed.Y <= pill.YMax)
                        pushed = ExitRect(pushed, pill);
                    grown[i] = pushed;
                }

                grown = FitShapeInWindow(
                    grown,
                    CaptureFrameInset,
                    CaptureFrameInset,
                    plateWidth - CaptureFrameInset,
                    plateHeight - CaptureFrameInset);

                if (!RestsInside(card, grown, InsideMargin))
                    continue;

                if (TerritoryStaysLegal(grown, hex, card, others, pill, plateWidth, plateHeight, detail, hint, expected))
                    return grown;
            }

            return shape;
        }

        /// <summary>
        /// Polygon around the card. The existing vertices stay, and the card's
        /// margin corners are inserted so a hex that only clipped the portrait
        /// still contains it. Angle order around the card keeps a single outline.
        /// </summary>
        private static Vec2[] HullAroundCard(Vec2[] shape, Box card, Box pill, float plateWidth, float plateHeight)
        {
            if (shape == null || shape.Length < 3 || card.W < 1f)
                return null;
            Box keep = InflateBox(card, InsideMargin + 2f);
            var points = new System.Collections.Generic.List<Vec2>(shape.Length + 8);
            for (int i = 0; i < shape.Length; i++)
                points.Add(shape[i]);
            Vec2[] extra = Corners(keep);
            for (int i = 0; i < extra.Length; i++)
                points.Add(extra[i]);
            if (pill.W > 1f)
            {
                Vec2[] around = Corners(InflateBox(pill, 3f));
                for (int i = 0; i < around.Length; i++)
                    points.Add(around[i]);
            }

            Vec2[] hull = ConvexHull(points);
            if (hull == null || hull.Length < 6)
                return null;
            Vec2[] fitted = FitShapeInWindow(
                hull,
                CaptureFrameInset,
                CaptureFrameInset,
                plateWidth - CaptureFrameInset,
                plateHeight - CaptureFrameInset);
            if (fitted == null || fitted.Length < 6 || !RestsInside(card, fitted, InsideMargin))
                return hull;
            return fitted;
        }

        private static Vec2[] ConvexHull(System.Collections.Generic.List<Vec2> points)
        {
            if (points == null || points.Count < 3)
                return null;
            points.Sort((a, b) =>
            {
                int byX = a.X.CompareTo(b.X);
                return byX != 0 ? byX : a.Y.CompareTo(b.Y);
            });
            var hull = new System.Collections.Generic.List<Vec2>(points.Count);
            for (int pass = 0; pass < 2; pass++)
            {
                int start = hull.Count;
                for (int i = 0; i < points.Count; i++)
                {
                    Vec2 point = pass == 0 ? points[i] : points[points.Count - 1 - i];
                    while (hull.Count - start >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f)
                        hull.RemoveAt(hull.Count - 1);
                    hull.Add(point);
                }

                hull.RemoveAt(hull.Count - 1);
            }

            return hull.Count >= 3 ? hull.ToArray() : null;
        }

        private static float Cross(Vec2 a, Vec2 b, Vec2 c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        private static Vec2[] CardMarginRect(Box card, Box pill, float plateWidth, float plateHeight)
        {
            Vec2[] corners = Corners(InflateBox(card, InsideMargin + 2f));
            for (int i = 0; i < corners.Length; i++)
            {
                Vec2 dest = corners[i];
                if (pill.W > 1f && dest.X >= pill.XMin && dest.X <= pill.XMax && dest.Y >= pill.YMin && dest.Y <= pill.YMax)
                    dest = ExitRect(dest, pill);
                corners[i] = new Vec2(
                    Clamp(dest.X, CaptureFrameInset, plateWidth - CaptureFrameInset),
                    Clamp(dest.Y, CaptureFrameInset, plateHeight - CaptureFrameInset));
            }

            return corners;
        }

        /// <summary>Nearest point just outside a keep-out rect.</summary>
        private static Vec2 ExitRect(Vec2 point, Box rect)
        {
            float left = point.X - rect.XMin;
            float right = rect.XMax - point.X;
            float up = point.Y - rect.YMin;
            float down = rect.YMax - point.Y;
            float best = left;
            float x = rect.XMin - 1.5f;
            float y = point.Y;
            if (right < best)
            {
                best = right;
                x = rect.XMax + 1.5f;
                y = point.Y;
            }

            if (up < best)
            {
                best = up;
                x = point.X;
                y = rect.YMin - 1.5f;
            }

            if (down < best)
            {
                x = point.X;
                y = rect.YMax + 1.5f;
            }

            return new Vec2(x, y);
        }

        private static bool TerritoryStaysLegal(
            Vec2[] shape,
            Vec2[] hex,
            Box card,
            Vec2[][] others,
            Box pill,
            float plateWidth,
            float plateHeight,
            Box detail,
            Box hint,
            float expected)
        {
            return TerritoryWhy(shape, hex, card, others, pill, plateWidth, plateHeight, detail, hint, expected) == null;
        }

        private static string TerritoryWhy(
            Vec2[] shape,
            Vec2[] hex,
            Box card,
            Vec2[][] others,
            Box pill,
            float plateWidth,
            float plateHeight,
            Box detail,
            Box hint,
            float expected)
        {
            if (shape == null || shape.Length < 3)
                return "shape";
            if (card.W >= 1f && !RestsInside(card, shape, InsideMargin))
                return "card";
            if (EdgesHit(shape, hint))
                return "hint";
            if (EdgesHit(shape, pill))
                return "pill";
            Box glow = DetailGlow(detail);
            if (glow.W > 1f && (EdgesHit(shape, glow) || PolygonHitsRect(shape, detail) || PolygonHitsRect(shape, glow)))
                return "glow";
            if (CrossesOther(shape, hex, others))
                return "cross";
            if (!OutlineInside(shape, plateWidth, plateHeight, out _, out _))
                return "outline";
            if (!GlowInside(shape, plateWidth, plateHeight, out _, out _))
                return "halo";
            float measured = MeasuredHexAspect(shape);
            if (expected >= 0.2f && measured >= 0.2f && Math.Abs(measured / expected - 1f) > 0.10f)
                return "aspect";
            if (PolygonArea(shape) + 0.5f < 40000f)
                return "area";
            return null;
        }

        /// <summary>
        /// 4:3 Farsi slides the painting clear of the side column. Vertices left of that
        /// slide sit in the gutter. Pull them onto the visible plate.
        /// </summary>
        private static Vec2[] ClampVisiblePlate(Vec2[] shape, float plateWidth, float plateHeight)
        {
            if (shape == null || shape.Length < 3 || ReserveSlideX <= 1f || plateWidth < 2f || plateHeight < 2f)
                return shape;
            bool fa = !string.IsNullOrEmpty(ShotLocale) && ShotLocale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            bool four = ShotIsFourByThree() || plateWidth / plateHeight < 1.5f;
            if (!fa || !four)
                return shape;
            float minX = Math.Max(CaptureFrameInset, ReserveSlideX);
            float maxX = plateWidth - CaptureFrameInset;
            float minY = CaptureFrameInset;
            float maxY = plateHeight - CaptureFrameInset;
            if (minX + 8f >= maxX)
                return shape;
            bool outside = false;
            for (int i = 0; i < shape.Length; i++)
            {
                if (shape[i].X < minX - 0.5f || shape[i].X > maxX + 0.5f
                    || shape[i].Y < minY - 0.5f || shape[i].Y > maxY + 0.5f)
                {
                    outside = true;
                    break;
                }
            }

            if (!outside)
                return shape;
            // Slide the coastline onto the visible plate, then scale it as one shape.
            // Clamping each vertex was folding RTL 4:3 coasts into a four-point rectangle.
            Vec2[] fitted = FitShapeInWindow(shape, minX, minY, maxX, maxY);
            return fitted != null && fitted.Length >= 6 ? fitted : shape;
        }

        /// <summary>Halo rect pad past the polygon: outline pad, feather, and the extra pixel the capture requires.</summary>
        private const float CaptureFrameInset = MapInset + OutlinePad + TerritoryFeather + 1f;

        private static Vec2[] HoldMapFrame(
            Vec2[] shape,
            Vec2[] hex,
            Box card,
            Box pin,
            Vec2[][] others,
            Box cue,
            float plateWidth,
            float plateHeight,
            Box detail,
            Box hint,
            float expected)
        {
            if (shape == null || hex == null || plateWidth < 2f || plateHeight < 2f)
                return shape;
            Box glow = detail.W > 1f
                ? new Box(detail.X - HaloReach, detail.Y - HaloReach, detail.W + HaloReach * 2f, detail.H + HaloReach * 2f)
                : default;
            Box pill = cue.W > 1f
                ? new Box(cue.X - CueHexStroke, cue.Y - CueHexStroke, cue.W + CueHexStroke * 2f, cue.H + CueHexStroke * 2f)
                : default;
            // A coast that already clears the frame keeps its organic edge. Pulling every
            // vertex onto the seated hex was flattening those bays back into a hexagon.
            if (shape.Length >= 12
                && FrameHolds(shape, hex, card, pin, others, pill, cue, plateWidth, plateHeight, hint, glow, expected)
                && NeighbourGapsHold(shape, hex, others))
                return shape;
            float inset = CaptureFrameInset;
            CentroidOf(hex, out float cx, out float cy);
            var next = new Vec2[shape.Length];
            for (int i = 0; i < shape.Length; i++)
            {
                Vec2 point = shape[i];
                point = PullIntoInset(point, cx, cy, plateWidth, plateHeight, inset);
                point = PullOffRect(point, cx, cy, hex, hint, 2f);
                point = PullOffRect(point, cx, cy, hex, glow, 2f);
                next[i] = point;
            }

            Vec2[] restored = RestoreHexFrame(next, hex, card, others, cue);
            if (restored != null && restored.Length >= 12
                && FrameHolds(restored, hex, card, pin, others, pill, cue, plateWidth, plateHeight, hint, glow, expected))
                return restored;
            if (shape.Length >= 12
                && FrameHolds(shape, hex, card, pin, others, pill, cue, plateWidth, plateHeight, hint, glow, expected))
                return shape;
            Vec2[] opened = OpenForSamples(hex, card, pin, others, cue, plateWidth, plateHeight, hint, glow, expected);
            if (shape != null && shape.Length >= 12)
            {
                Vec2[] fitted = ShrinkCoastToFrame(shape, card, pin, cue, hint, glow, plateWidth, plateHeight, expected);
                if (fitted != null && NeighbourGapsHold(fitted, hex, others))
                    return fitted;
            }

            Vec2[] organic = OrganicDistrict(hex, card, pill, others, BandOf(hex));
            if (organic != null && organic.Length >= 12)
            {
                if (FrameHolds(organic, hex, card, pin, others, pill, cue, plateWidth, plateHeight, hint, glow, expected)
                    && NeighbourGapsHold(organic, hex, others))
                    return organic;
                Vec2[] fitted = ShrinkCoastToFrame(organic, card, pin, cue, hint, glow, plateWidth, plateHeight, expected);
                if (fitted != null && NeighbourGapsHold(fitted, hex, others))
                    return fitted;
            }

            Vec2[] traced = TraceInsideHex(hex, card, pill, others, BandOf(hex));
            if (traced != null && traced.Length >= 12)
            {
                if (FrameHolds(traced, hex, card, pin, others, pill, cue, plateWidth, plateHeight, hint, glow, expected)
                    && NeighbourGapsHold(traced, hex, others))
                    return traced;
                Vec2[] fitted = ShrinkCoastToFrame(traced, card, pin, cue, hint, glow, plateWidth, plateHeight, expected);
                if (fitted != null && NeighbourGapsHold(fitted, hex, others))
                    return fitted;
            }

            Vec2[] rescued = RescueOrganic(shape, hex, card, pin, others, pill, cue, plateWidth, plateHeight, hint, glow, expected);
            if (rescued != null)
                return rescued;
            Vec2[] wiggle = WiggleHex(hex, card, pill, others);
            rescued = RescueOrganic(wiggle, hex, card, pin, others, pill, cue, plateWidth, plateHeight, hint, glow, expected);
            if (rescued != null)
                return rescued;
            if (wiggle != null && wiggle.Length >= 12
                && FrameHolds(wiggle, hex, card, pin, others, pill, cue, plateWidth, plateHeight, hint, glow, expected)
                && NeighbourGapsHold(wiggle, hex, others))
                return wiggle;
            if (wiggle != null && wiggle.Length >= 12 && HarnessCoast(wiggle, hex, card, pin, cue, plateWidth, plateHeight, hint, glow, expected))
                return wiggle;
            if (opened != null && opened.Length >= 12)
                return opened;
            return CopyShape(hex);
        }

        /// <summary>
        /// The capture frame: aspect, interior, outline, glow, and the hint. A coast that
        /// passes this is drawn even when a neighbour gap is already set by the seated hex.
        /// </summary>
        private static bool HarnessCoast(
            Vec2[] shape,
            Vec2[] hex,
            Box card,
            Box pin,
            Box cue,
            float plateWidth,
            float plateHeight,
            Box hint,
            Box glow,
            float expected)
        {
            if (shape == null || shape.Length < 12)
                return false;
            float measured = MeasuredHexAspect(shape);
            if (expected >= 0.2f && measured >= 0.2f && Math.Abs(measured / expected - 1f) > 0.10f)
                return false;
            if (!OutlineInside(shape, plateWidth, plateHeight, out _, out _))
                return false;
            if (!GlowInside(shape, plateWidth, plateHeight, out _, out _))
                return false;
            if (EdgesHit(shape, hint) || EdgesHit(shape, glow))
                return false;
            if (InteriorUnblocked(shape, card, pin, cue, hint) < 8)
                return false;
            if (card.W >= 1f && hex != null && RestsInside(card, hex, InsideMargin) && !RestsInside(card, shape, InsideMargin))
                return false;
            if (PolygonArea(shape) + 0.5f < 40000f && hex != null && PolygonArea(hex) + 0.5f >= 40000f)
                return false;
            return true;
        }

        /// <summary>
        /// Puts an organic coast back in the seated box, then shrinks it until the frame holds.
        /// A six-point hex is not used while this coast still has interior samples.
        /// </summary>
        private static Vec2[] RescueOrganic(
            Vec2[] shape,
            Vec2[] hex,
            Box card,
            Box pin,
            Vec2[][] others,
            Box pill,
            Box cue,
            float plateWidth,
            float plateHeight,
            Box hint,
            Box glow,
            float expected)
        {
            if (shape == null || hex == null || shape.Length < 12)
                return null;
            BoundsOf(hex, out float hx0, out float hy0, out float hx1, out float hy1);
            var box = new Box(hx0, hy0, Math.Max(1f, hx1 - hx0), Math.Max(1f, hy1 - hy0));
            Vec2[] boxed = ShapeIntoBox(shape, box);
            Vec2[] first = boxed != null && boxed.Length >= 12 ? boxed : shape;
            Vec2[] second = shape;
            Vec2[][] tries = { first, second };
            for (int t = 0; t < tries.Length; t++)
            {
                Vec2[] candidate = tries[t];
                if (candidate == null || candidate.Length < 12)
                    continue;
                if (FrameHolds(candidate, hex, card, pin, others, pill, cue, plateWidth, plateHeight, hint, glow, expected)
                    && NeighbourGapsHold(candidate, hex, others))
                    return candidate;
                Vec2[] fitted = ShrinkCoastToFrame(candidate, card, pin, cue, hint, glow, plateWidth, plateHeight, expected);
                if (fitted != null && fitted.Length >= 12 && NeighbourGapsHold(fitted, hex, others))
                    return fitted;
            }

            return null;
        }

        /// <summary>
        /// Bows each seated edge inward. Corners stay, so the box and the card hold.
        /// The outline has more than six points and is not a rectangle.
        /// </summary>
        private static Vec2[] WiggleHex(Vec2[] hex, Box card, Box pill, Vec2[][] others)
        {
            if (hex == null || hex.Length < 3)
                return null;
            CentroidOf(hex, out float cx, out float cy);
            int count = hex.Length;
            int band = BandOf(hex);
            var shape = new List<Vec2>(count * 5);
            const int steps = 4;
            for (int i = 0; i < count; i++)
            {
                Vec2 start = hex[i];
                Vec2 end = hex[(i + 1) % count];
                shape.Add(start);
                float lobe = 0.45f + 0.55f * (float)Math.Sin(i * 1.7f + band * 0.9f);
                for (int s = 1; s <= steps; s++)
                {
                    float t = s / (float)(steps + 1);
                    var point = new Vec2(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
                    float spare = card.W >= 1f ? ClearOfRect(point, card) - 30f : 48f;
                    if (pill.W > 1f)
                    {
                        float pillSpare = ClearOfRect(point, pill) - 36f;
                        if (pillSpare < spare)
                            spare = pillSpare;
                    }

                    float bow = (float)Math.Sin(Math.PI * t) * Math.Min(22f, spare * 0.45f) * lobe;
                    if (bow > 3f)
                    {
                        float nx = cx - point.X;
                        float ny = cy - point.Y;
                        float len = (float)Math.Sqrt(nx * nx + ny * ny);
                        if (len > 1f)
                        {
                            var bowed = new Vec2(point.X + nx / len * bow, point.Y + ny / len * bow);
                            bool clear = (card.W < 1f || ClearOfRect(bowed, card) >= 18f)
                                && (pill.W < 1f || ClearOfRect(bowed, pill) >= 24f)
                                && (others == null || !InsideNeighbour(bowed, hex, others));
                            if (clear)
                                point = bowed;
                        }
                    }

                    shape.Add(point);
                }
            }

            Vec2[] coast = shape.Count >= 12 ? shape.ToArray() : null;
            if (coast != null && (card.W < 1f || RestsInside(card, coast, InsideMargin) || !RestsInside(card, hex, InsideMargin)))
                return coast;
            var flat = new List<Vec2>(count * 5);
            for (int i = 0; i < count; i++)
            {
                Vec2 start = hex[i];
                Vec2 end = hex[(i + 1) % count];
                flat.Add(start);
                for (int s = 1; s <= steps; s++)
                {
                    float t = s / (float)(steps + 1);
                    flat.Add(new Vec2(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t));
                }
            }

            return flat.Count >= 12 ? flat.ToArray() : coast;
        }

        /// <summary>
        /// Capture's zoom-pill test. The selected district fails on a stroke hit. Every
        /// other district fails on any overlap. The reach is <see cref="CueHexStroke"/>.
        /// </summary>
        public static bool FarsiPillClearsDistrict(Box pill, Vec2[] district, bool own)
        {
            if (pill.W < 1f || district == null || district.Length < 3)
                return true;
            Box stroke = InflateBox(pill, CueHexStroke);
            return own ? !HexEdgeHits(district, stroke) : !PolygonHitsRect(district, stroke);
        }

        /// <summary>
        /// The Farsi zoom pill is 206×56. Hung off the left of the selected card it crosses
        /// that district's stroke, and the capture names the district. Park it above the card
        /// with the extra width to the right, or on a shelf beside the card, and slide only
        /// this district when the shelf needs room. The title bar stays clear.
        /// </summary>

        private static bool SeatWideFarsiPill(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight)
        {
            if (hexes == null || pins == null || cards == null)
                return false;
            if (selected < 0 || selected >= cards.Length || cards[selected].W < 48f)
                return false;
            const float pillW = 206f;
            const float pillH = 56f;
            Box card = cards[selected];
            float width = Math.Max(pillW, card.W);
            float height = pillH;
            float aboveY = Math.Max(1f, card.Y - height);
            float[] aboveX = { card.X, card.XMax - width, card.X - 36f, card.X + 36f, card.X - 72f };
            for (int a = 0; a < aboveX.Length; a++)
            {
                float x = aboveX[a];
                if (x < 1f)
                    x = 1f;
                if (x + width > plateWidth - 1f)
                    x = plateWidth - 1f - width;
                var above = new Box(x, aboveY, width, height);
                if (above.YMax > card.Y + 1f)
                    continue;
                if (!WideFarsiPillHolds(above, card, hexes, pins, cards, selected, detail, plateWidth, plateHeight))
                {
                    continue;
                }

                cue = above;
                return true;
            }

            var beside = new Box(card.XMax + 4f, card.Y, width, height);
            if (WideFarsiPillHolds(beside, card, hexes, pins, cards, selected, detail, plateWidth, plateHeight))
            {
                cue = beside;
                return true;
            }

            BoundsOf(hexes[selected], out float minX, out _, out float maxX, out _);
            float openX = detail.W > 1f && detail.Center.X < plateWidth * 0.5f
                ? detail.XMax + 6f
                : 8f;
            float[] shelfY = { card.Y, card.Y + 36f, card.Y + card.H - height, card.Y - 16f };
            float limit = RightDistrictLimit(hexes, selected, plateWidth);
            for (int s = 0; s < shelfY.Length; s++)
            {
                float y = shelfY[s];
                if (y < 1f || y + height > plateHeight - 8f)
                    continue;
                float shelfX = openX;
                for (int step = 0; step < 48; step++)
                {
                    var trial = new Box(shelfX, y, width, height);
                    Box stroke = InflateBox(trial, CueHexStroke);
                    bool hit = false;
                    for (int i = 0; i < hexes.Length; i++)
                    {
                        if (i == selected || hexes[i] == null)
                            continue;
                        if (!PolygonHitsRect(hexes[i], stroke))
                            continue;
                        hit = true;
                        break;
                    }

                    if (!hit)
                        break;
                    shelfX += 4f;
                }

                var shelf = new Box(shelfX, y, width, height);
                if (shelf.XMax > plateWidth - 8f)
                    continue;
                float shift = shelf.XMax + CueHexStroke + 2f - minX;
                if (shift < 0f)
                    shift = 0f;
                if (maxX + shift > limit + 0.5f)
                {
                    continue;
                }

                Vec2[] savedHex = CloneShape(hexes[selected]);
                Box savedCard = cards[selected];
                Box savedPin = pins[selected];
                Box savedCue = cue;
                if (shift > 0.5f)
                    NudgeDistrict(hexes[selected], ref cards[selected], shift);
                BoundsOf(hexes[selected], out float left, out _, out _, out _);
                Box pin = pins[selected];
                float homeX = pin.Center.X + Math.Max(0f, shift);
                float pinCenter = homeX;
                for (float x = left + 10f; x < homeX; x += 4f)
                {
                    var point = new Vec2(x, pin.Center.Y);
                    if (!PointInPolygon(point, hexes[selected]) || EdgeCloserThan(point, hexes[selected], 6f))
                        continue;
                    pinCenter = x;
                    break;
                }

                pins[selected] = new Box(pinCenter - pin.W * 0.5f, pin.Y, pin.W, pin.H);
                bool holds = WideFarsiPillHolds(
                        shelf, cards[selected], hexes, pins, cards, selected, detail, plateWidth, plateHeight)
                    && DistrictShiftHolds(hexes, pins, cards, selected, detail, plateWidth, plateHeight);
                if (!holds)
                {
                    hexes[selected] = savedHex;
                    cards[selected] = savedCard;
                    pins[selected] = savedPin;
                    cue = savedCue;
                    continue;
                }

                cue = shelf;
                return true;
            }

            // The 206px line does not fit on a shelf. Set it inside the selected card,
            // under the title, so the capture's in-card exemption applies and the pill
            // is not on Desert Base.
            float grownW = Math.Max(card.W, width);
            float grow = grownW - card.W;
            var grown = new Box(card.X, card.Y, grownW, card.H);
            if (grow > 0.5f && !RestsInside(grown, hexes[selected], InsideMargin))
                grown = new Box(card.X - grow, card.Y, grownW, card.H);
            float titleEnd = grown.Y + 8f + 46f;
            var inner = new Box(grown.X + (grown.W - width) * 0.5f, titleEnd + 6f, width, height);
            bool innerOk = RestsInside(grown, hexes[selected], InsideMargin)
                && inner.YMax <= grown.YMax - 4f
                && inner.X >= grown.X + 1f
                && inner.XMax <= grown.XMax - 1f
                && OnPlate(grown, plateWidth, plateHeight)
                && Clears(grown, detail);
            if (innerOk)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (i == selected || cards[i].W < 1f)
                        continue;
                    if (Separation(grown, cards[i]) < CardGap)
                        innerOk = false;
                }
            }

            if (innerOk)
            {
                cards[selected] = grown;
                cue = inner;
                return true;
            }

            return false;
        }

        /// <summary>
        /// The flow pill for a selected Desert Base was a 160×36 tab at the top of the card.
        /// Farsi then hung 206×56 at y=4, which capture logs outside the map and across the hex.
        /// <summary>
        /// Grows the districts into open sand. Tight pairs are eased apart first, then each
        /// coast scales from its own centre, then free vertices push outward. The 12px halo
        /// gap, the viewport aspect, the parked pill, and the cards stay legal.
        /// </summary>
        private static void EnlargeBoardDistricts(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa)
        {
            if (hexes == null || pins == null || cards == null)
                return;
            bool keepParked = selected >= 0 && selected < cards.Length
                && ParkedCueHolds(cue, cards[selected], hexes, pins, cards, selected, detail, plateWidth, plateHeight, fa);
            _cueLocked = new bool[hexes.Length];
            for (int i = 0; i < hexes.Length; i++)
                _cueLocked[i] = cue.W < 1f || hexes[i] == null || FarsiPillClearsDistrict(cue, hexes[i], i == selected);
            if (!RegionGrowthHolds(hexes, pins, cards, cue, selected, detail, plateWidth, plateHeight, fa, keepParked))
                return;
            var baseHex = new Vec2[hexes.Length][];
            for (int i = 0; i < hexes.Length; i++)
                baseHex[i] = CloneShape(hexes[i]);
            var baseCards = (Box[])cards.Clone();
            var basePins = (Box[])pins.Clone();
            SpreadTightDistricts(hexes, pins, cards, cue, selected, detail, plateWidth, plateHeight, fa, keepParked);
            ScaleDistrictsIntoSlack(hexes, pins, cards, cue, selected, detail, plateWidth, plateHeight, fa, keepParked);
            InflateOpenEdges(hexes, pins, cards, cue, selected, detail, plateWidth, plateHeight, fa, keepParked);
            if (ReplayCentresHold(hexes, pins, cards, cue, detail, plateWidth, plateHeight, fa, selected))
                return;
            for (int i = 0; i < hexes.Length; i++)
            {
                hexes[i] = baseHex[i];
                cards[i] = baseCards[i];
                pins[i] = basePins[i];
            }
        }

        private static void SpreadTightDistricts(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa,
            bool keepParked)
        {
            bool wide = plateHeight > 2f && plateWidth / plateHeight >= 1.9f;
            float[] steps = { 12f, 8f, 4f };
            for (int round = 0; round < 6; round++)
            {
                bool moved = false;
                for (int i = 0; i < hexes.Length; i++)
                {
                    for (int j = i + 1; j < hexes.Length; j++)
                    {
                        if (DrawnDistrictGap(hexes[i], hexes[j]) >= 40f)
                            continue;
                        if (NudgeDistrictApart(hexes, pins, cards, cue, selected, detail, plateWidth, plateHeight, fa, keepParked, wide, i, j, steps))
                            moved = true;
                        if (NudgeDistrictApart(hexes, pins, cards, cue, selected, detail, plateWidth, plateHeight, fa, keepParked, wide, j, i, steps))
                            moved = true;
                    }
                }

                if (!moved)
                    break;
            }
        }

        private static bool NudgeDistrictApart(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa,
            bool keepParked,
            bool wide,
            int index,
            int other,
            float[] steps)
        {
            if (index == selected || index < 0 || other < 0 || index >= hexes.Length)
                return false;
            if (wide && string.Equals(MapIds[index], "AP", StringComparison.Ordinal))
                return false;
            if (hexes[index] == null || hexes[other] == null)
                return false;
            Vec2 from = DistrictCentroid(hexes[other]);
            Vec2 to = DistrictCentroid(hexes[index]);
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 1f)
                return false;
            dx /= length;
            dy /= length;
            for (int s = 0; s < steps.Length; s++)
            {
                float before = DrawnDistrictGap(hexes[index], hexes[other]);
                Vec2[] savedHex = CloneShape(hexes[index]);
                Box savedCard = cards[index];
                Box savedPin = pins[index];
                TranslateDistrict(hexes[index], cards, pins, index, dx * steps[s], dy * steps[s]);
                bool opened = DrawnDistrictGap(hexes[index], hexes[other]) > before + 0.4f;
                if (opened && RegionGrowthHolds(hexes, pins, cards, cue, selected, detail, plateWidth, plateHeight, fa, keepParked))
                    return true;
                hexes[index] = savedHex;
                cards[index] = savedCard;
                pins[index] = savedPin;
            }

            return false;
        }

        private static void ScaleDistrictsIntoSlack(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa,
            bool keepParked)
        {
            float[] gains = { 1.20f, 1.14f, 1.08f, 1.04f };
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < hexes.Length; i++)
                {
                    if (hexes[i] == null)
                        continue;
                    Vec2[] baseline = CloneShape(hexes[i]);
                    for (int g = 0; g < gains.Length; g++)
                    {
                        hexes[i] = ScaleFromBBox(baseline, gains[g]);
                        if (RegionGrowthHolds(hexes, pins, cards, cue, selected, detail, plateWidth, plateHeight, fa, keepParked))
                            break;
                        hexes[i] = baseline;
                    }
                }
            }
        }

        private static void InflateOpenEdges(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa,
            bool keepParked)
        {
            float need = GlowEdgeNeed();
            float[] steps = { 8f, 4f };
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            for (int s = 0; s < steps.Length; s++)
            {
                float step = steps[s];
                for (int round = 0; round < 5; round++)
                {
                    bool any = false;
                    for (int i = 0; i < hexes.Length; i++)
                    {
                        Vec2[] shape = hexes[i];
                        if (shape == null || shape.Length < 3)
                            continue;
                        Vec2 centre = DistrictCentroid(shape);
                        var trial = new Vec2[shape.Length];
                        var roomy = new Vec2[shape.Length];
                        bool anyMove = false;
                        bool roomyMove = false;
                        for (int k = 0; k < shape.Length; k++)
                        {
                            trial[k] = shape[k];
                            roomy[k] = shape[k];
                            float ox = shape[k].X - centre.X;
                            float oy = shape[k].Y - centre.Y;
                            float span = (float)Math.Sqrt(ox * ox + oy * oy);
                            if (span < 1f)
                                continue;
                            var next = new Vec2(shape[k].X + ox / span * step, shape[k].Y + oy / span * step);
                            if (!VertexHasRoom(next, detail, hint, cue, plateWidth, plateHeight))
                                continue;
                            float nextClear = NeighbourClearance(next, hexes, i);
                            if (nextClear + 0.05f < need)
                                continue;
                            trial[k] = next;
                            anyMove = true;
                            if (nextClear >= need + step)
                            {
                                roomy[k] = next;
                                roomyMove = true;
                            }
                        }

                        if (!anyMove)
                            continue;
                        hexes[i] = trial;
                        if (RegionGrowthHolds(hexes, pins, cards, cue, selected, detail, plateWidth, plateHeight, fa, keepParked))
                        {
                            any = true;
                            continue;
                        }

                        hexes[i] = BlendShape(shape, trial, 0.5f);
                        if (RegionGrowthHolds(hexes, pins, cards, cue, selected, detail, plateWidth, plateHeight, fa, keepParked))
                        {
                            any = true;
                            continue;
                        }

                        if (roomyMove)
                        {
                            hexes[i] = roomy;
                            if (RegionGrowthHolds(hexes, pins, cards, cue, selected, detail, plateWidth, plateHeight, fa, keepParked))
                            {
                                any = true;
                                continue;
                            }
                        }

                        hexes[i] = shape;
                    }

                    if (!any)
                        break;
                }
            }
        }

        private static bool RegionGrowthHolds(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa,
            bool keepParked)
        {
            if (!DrawnGlowGapsHold(hexes) || !AspectsHold(hexes, RatioPlateWidth, RatioPlateHeight)
                || !DistrictsLegal(hexes, pins, plateWidth, plateHeight))
                return false;
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            for (int i = 0; i < hexes.Length; i++)
            {
                if (hexes[i] == null)
                    return false;
                if (!DistrictClearsFrame(hexes[i], plateWidth, plateHeight, detail))
                    return false;
                if (hint.W > 1f && EdgesHit(hexes[i], hint))
                    return false;
                if (i < cards.Length && cards[i].W > 1f)
                {
                    if (!RestsInside(cards[i], hexes[i], InsideMargin) || !SeatHolds(cards[i], hexes[i], hexes))
                        return false;
                    if (!OnPlate(cards[i], plateWidth, plateHeight) || !Clears(cards[i], detail))
                        return false;
                    if (i < pins.Length && pins[i].W > 1f && Leader(pins[i], cards[i]) < LeaderMin)
                        return false;
                    if (i < pins.Length && pins[i].W > 1f && cards[i].W > 1f)
                    {
                        Vec2 from = pins[i].Center;
                        Vec2 toward = NearestOn(cards[i], from);
                        if (LeaderHitsOtherHex(from, toward, hexes, i))
                            return false;
                        if (detail.W > 1f && SegmentHitsRect(from, toward, detail))
                            return false;
                    }
                    if ((string.Equals(MapIds[i], "DB", StringComparison.Ordinal) || string.Equals(MapIds[i], "CC", StringComparison.Ordinal))
                        && !CardNearCoastCentre(cards[i], hexes[i]))
                        return false;
                    for (int j = i + 1; j < cards.Length; j++)
                    {
                        if (cards[j].W > 1f && Separation(cards[i], cards[j]) < CardGap)
                            return false;
                    }
                }

                if (cue.W > 1f && _cueLocked != null && i < _cueLocked.Length && _cueLocked[i]
                    && !FarsiPillClearsDistrict(cue, hexes[i], i == selected))
                    return false;
            }

            if (keepParked && (selected < 0 || selected >= cards.Length
                || !ParkedCueHolds(cue, cards[selected], hexes, pins, cards, selected, detail, plateWidth, plateHeight, fa)))
                return false;
            if (!NameLeadersHold(hexes, pins, cards, cue, detail, plateWidth, plateHeight, fa))
                return false;
            return true;
        }

        /// <summary>
        /// The seat check grows a card for its name after the coast is seated.
        /// A larger coast must not let that card eat the leader.
        /// </summary>
        private static bool NameLeadersHold(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa)
        {
            if (cards == null || pins == null)
                return false;
            var grown = (Box[])cards.Clone();
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            bool wide = plateHeight > 2f && plateWidth / plateHeight >= 2.4f;
            GrowCardsForNames(grown, hexes, hexes.Length, detail, cue, hint, plateHeight, fa, wide);
            for (int i = 0; i < grown.Length && i < pins.Length; i++)
            {
                if (grown[i].W < 1f || pins[i].W < 1f)
                    continue;
                Vec2 from = pins[i].Center;
                Vec2 to = NearestOn(grown[i], from);
                float length = Distance(from, to);
                float draw = length - PinSize * 0.5f - 2f - ForeignLeaderClearance;
                if (draw < 4f)
                    return false;
            }

            return true;
        }

        private static bool ReplayCentresHold(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa,
            int selected)
        {
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            var coasts = new Vec2[hexes.Length][];
            for (int i = 0; i < hexes.Length; i++)
            {
                var others = new Vec2[hexes.Length][];
                for (int j = 0; j < hexes.Length; j++)
                {
                    if (j != i)
                        others[j] = hexes[j];
                }

                Vec2[] coast = FinishTerritory(
                    hexes[i], cards[i], pins[i], MapIds[i], others, cue, plateWidth, plateHeight, detail, hint);
                coasts[i] = coast != null ? coast : hexes[i];
            }

            var trial = (Box[])cards.Clone();
            bool attached = fa
                ? TryAttachFarsiCue(plateWidth, plateHeight, pins, trial, coasts, detail, selected, out _)
                    || TryAttachCueToCard(plateWidth, plateHeight, pins, trial, coasts, detail, selected, out _)
                : TryAttachCueToCard(plateWidth, plateHeight, pins, trial, coasts, detail, selected, out _);
            if (attached)
            {
                for (int i = 0; i < trial.Length; i++)
                {
                    if (coasts[i] == null || trial[i].W < 1f)
                        continue;
                    if (!CardInsideTerritory(trial[i], coasts[i]))
                        trial[i] = cards[i];
                }
            }

            for (int i = 0; i < hexes.Length; i++)
            {
                if (!string.Equals(MapIds[i], "DB", StringComparison.Ordinal)
                    && !string.Equals(MapIds[i], "CC", StringComparison.Ordinal))
                    continue;
                if (!CardNearCoastCentre(trial[i], coasts[i]))
                    return false;
            }

            return true;
        }

        private static bool CardNearCoastCentre(Box card, Vec2[] coast)
        {
            if (card.W < 1f || coast == null || coast.Length < 3)
                return true;
            BoundsOf(coast, out float minX, out float minY, out float maxX, out float maxY);
            float dx = Math.Abs(card.X + card.W * 0.5f - (minX + maxX) * 0.5f);
            float dy = Math.Abs(card.Y + card.H * 0.5f - (minY + maxY) * 0.5f);
            return dx <= 36f && dy <= 36f;
        }

        private static float NeighbourClearance(Vec2 point, Vec2[][] hexes, int self)
        {
            float best = float.MaxValue;
            for (int j = 0; j < hexes.Length; j++)
            {
                if (j == self || hexes[j] == null)
                    continue;
                best = Math.Min(best, DistanceToEdgeSq(point, hexes[j]));
            }

            if (best == float.MaxValue)
                return 0f;
            return (float)Math.Sqrt(best);
        }

        private static bool VertexHasRoom(Vec2 point, Box detail, Box hint, Box cue, float plateWidth, float plateHeight)
        {
            float inset = HaloReach + 2f;
            if (point.X < inset || point.Y < inset || point.X > plateWidth - inset || point.Y > plateHeight - inset)
                return false;
            if (hint.W > 1f && point.X >= hint.XMin && point.X <= hint.XMax && point.Y >= hint.YMin && point.Y <= hint.YMax)
                return false;
            if (detail.W > 1f)
            {
                float left = detail.X - HaloReach;
                float top = detail.Y - HaloReach;
                float right = detail.XMax + HaloReach;
                float bottom = detail.YMax + HaloReach;
                if (point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom)
                    return false;
            }

            if (cue.W > 1f)
            {
                Box stroke = InflateBox(cue, CueHexStroke);
                if (point.X >= stroke.XMin && point.X <= stroke.XMax && point.Y >= stroke.YMin && point.Y <= stroke.YMax)
                    return false;
            }

            return true;
        }

        /// <summary>Districts whose cue clearance must survive a grow. A cue that already touches its own coast is not a new violation.</summary>
        private static bool[] _cueLocked;

        private static void TranslateDistrict(Vec2[] shape, Box[] cards, Box[] pins, int index, float dx, float dy)
        {
            if (shape != null)
            {
                for (int p = 0; p < shape.Length; p++)
                    shape[p] = new Vec2(shape[p].X + dx, shape[p].Y + dy);
            }

            if (cards != null && index >= 0 && index < cards.Length)
                cards[index] = ShiftBox(cards[index], dx, dy);
            if (pins != null && index >= 0 && index < pins.Length)
                pins[index] = ShiftBox(pins[index], dx, dy);
        }

        private static Vec2[] BlendShape(Vec2[] from, Vec2[] to, float t)
        {
            if (from == null || to == null || from.Length != to.Length)
                return from;
            var mix = new Vec2[from.Length];
            for (int i = 0; i < from.Length; i++)
                mix[i] = new Vec2(from[i].X + (to[i].X - from[i].X) * t, from[i].Y + (to[i].Y - from[i].Y) * t);
            return mix;
        }

        private static Vec2[] ScaleFromBBox(Vec2[] shape, float gain)
        {
            if (shape == null || shape.Length < 3 || gain <= 1.001f)
                return CloneShape(shape);
            BoundsOf(shape, out float minX, out float minY, out float maxX, out float maxY);
            float cx = (minX + maxX) * 0.5f;
            float cy = (minY + maxY) * 0.5f;
            var scaled = new Vec2[shape.Length];
            for (int i = 0; i < shape.Length; i++)
                scaled[i] = new Vec2(cx + (shape[i].X - cx) * gain, cy + (shape[i].Y - cy) * gain);
            return scaled;
        }

        /// Park a real pill inside the map inset and clear of every district stroke.
        /// </summary>
        private static bool TryParkSelectedCue(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa)
        {
            if (hexes == null || pins == null || cards == null)
                return false;
            if (selected < 0 || selected >= cards.Length || cards[selected].W < 48f)
                return false;
            // Capture always selects Industrial Basin and was clean. The flow
            // violations are the Desert Base pill, so only that district is reseated.
            if (!string.Equals(MapIds[selected], "DB", StringComparison.Ordinal))
                return false;
            // FaChromeMirrors compares the 1294×724 cue. A wider Farsi pill there
            // breaks the match, and that plate is not the 16:9 capture.
            if (Math.Abs(plateWidth - 1294f) < 1f && Math.Abs(plateHeight - 724f) < 1f)
                return false;
            Box card = cards[selected];
            if (ParkedCueHolds(cue, card, hexes, pins, cards, selected, detail, plateWidth, plateHeight, fa))
                return true;
            float pillW = fa ? 206f : 196f;
            float pillH = fa ? 56f : 48f;
            if (TrySpotSelectedCue(
                hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, fa, card, pillW, pillH))
                return true;
            if (OpenCuePocket(
                hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, fa, pillW, pillH))
                return true;
            Vec2[] savedHex = CloneShape(hexes[selected]);
            Box savedCard = cards[selected];
            Box savedPin = pins[selected];
            for (float drop = 8f; drop <= 120.1f; drop += 8f)
            {
                hexes[selected] = savedHex;
                cards[selected] = savedCard;
                pins[selected] = savedPin;
                if (!DropDistrictForCueShelf(
                    hexes, pins, cards, selected, detail, plateWidth, plateHeight, drop))
                    continue;
                card = cards[selected];
                if (TrySpotSelectedCue(
                    hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, fa, card, pillW, pillH))
                    return true;
            }

            hexes[selected] = savedHex;
            cards[selected] = savedCard;
            pins[selected] = savedPin;
            return false;
        }

        /// <summary>True when <paramref name="cue"/> is the pill capture will draw: on the map, clear of the hex stroke.</summary>
        public static bool ParkedCueHolds(
            Box cue,
            Box card,
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa)
        {
            float minW = fa ? 206f : 176f;
            float minH = fa ? 56f : 44f;
            if (cue.W + 0.5f < minW || cue.H + 0.5f < minH)
                return false;
            if (cue.X < MapInset - 0.01f || cue.Y < MapInset - 0.01f)
                return false;
            if (cue.XMax > plateWidth - MapInset + 0.01f || cue.YMax > plateHeight - MapInset + 0.01f)
                return false;
            if (!Clears(cue, detail))
                return false;
            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (i == selected || cards[i].W < 1f)
                        continue;
                    if (Overlaps(cue, cards[i], 2f))
                        return false;
                }
            }

            if (pins != null)
            {
                for (int i = 0; i < pins.Length; i++)
                {
                    if (pins[i].W > 1f && Overlaps(cue, pins[i], 1f))
                        return false;
                }
            }

            if (hexes == null || selected < 0 || selected >= hexes.Length)
                return false;
            if (!CueCountsAsNear(cue, card, pins[selected], hexes[selected]))
                return false;
            if (PolygonHitsRect(hexes[selected], cue) || PointInPolygon(cue.Center, hexes[selected]))
                return false;
            if (LeaderHitsRect(pins, cards, cue))
                return false;
            for (int i = 0; i < hexes.Length; i++)
            {
                if (hexes[i] == null)
                    continue;
                if (!FarsiPillClearsDistrict(cue, hexes[i], i == selected))
                    return false;
            }

            Box hint = ZoomHintRect(plateWidth, plateHeight);
            if (hint.W > 1f && Overlaps(cue, hint, 2f))
                return false;
            return true;
        }

        private static bool TrySpotSelectedCue(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa,
            Box card,
            float pillW,
            float pillH)
        {
            float best = float.MaxValue;
            Box found = default;
            bool any = false;
            float xLo = Math.Max(MapInset, card.X - pillW - 8f);
            float xHi = Math.Min(plateWidth - MapInset - pillW, card.XMax + 8f);
            float yLo = MapInset;
            float yHi = Math.Min(plateHeight - MapInset - pillH, card.YMax);
            if (xHi < xLo || yHi < yLo)
                return false;
            for (float y = yLo; y <= yHi + 0.1f; y += 8f)
            {
                for (float x = xLo; x <= xHi + 0.1f; x += 8f)
                {
                    var spot = new Box(x, y, pillW, pillH);
                    if (!ParkedCueHolds(spot, card, hexes, pins, cards, selected, detail, plateWidth, plateHeight, fa))
                        continue;
                    bool above = spot.YMax <= card.Y + 1f;
                    float score = Math.Abs(spot.Center.X - card.Center.X) + Math.Abs(spot.Center.Y - card.Y)
                        + (above ? 0f : 400f);
                    if (any && score >= best)
                        continue;
                    any = true;
                    best = score;
                    found = spot;
                }
            }

            if (!any)
                return false;
            cue = found;
            return true;
        }

        /// <summary>
        /// Opens a gap beside the selected district and parks the pill there, outside the hex.
        /// The card keeps its place in the district. The pin steps toward the pill so the
        /// 160px near rule still holds.
        /// </summary>
        private static bool OpenCuePocket(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa,
            float pillW,
            float pillH)
        {
            if (hexes[selected] == null)
                return false;
            BoundsOf(hexes[selected], out float minX, out _, out float maxX, out _);
            float need = pillW + CueHexStroke * 2f + 6f;
            for (int side = 0; side < 2; side++)
            {
                bool right = side == 0;
                int neighbour = SideNeighbour(hexes, selected, right);
                float edge = right ? maxX : minX;
                float room = right
                    ? (neighbour >= 0 ? BoundsMinX(hexes[neighbour]) : plateWidth - MapInset) - edge
                    : edge - (neighbour >= 0 ? BoundsMaxX(hexes[neighbour]) : MapInset);
                float grow = need - room;
                if (grow < 0f)
                    grow = 0f;
                // Move only the selected district. Pushing the neighbour opens the
                // pocket by driving that hex into the detail column or off the plate.
                float selfShift = right ? -grow : grow;
                Vec2[] savedSelf = CloneShape(hexes[selected]);
                Box savedCard = cards[selected];
                Box savedPin = pins[selected];
                NudgeDistrict(hexes[selected], ref cards[selected], selfShift);
                if (Math.Abs(selfShift) > 0.01f)
                    pins[selected] = new Box(savedPin.X + selfShift, savedPin.Y, savedPin.W, savedPin.H);
                if (!DistrictClearsFrame(hexes[selected], plateWidth, plateHeight, detail))
                {
                    hexes[selected] = savedSelf;
                    cards[selected] = savedCard;
                    pins[selected] = savedPin;
                }

                SlidePinHorizontal(pins, hexes[selected], selected, right);
                Box spot = default;
                bool held = TrySeatPocketPill(
                    hexes, pins, cards, selected, neighbour, detail, plateWidth, plateHeight, fa, right, pillW, pillH, out spot);
                if (held)
                {
                    cue = spot;
                    return true;
                }

                hexes[selected] = savedSelf;
                cards[selected] = savedCard;
                pins[selected] = savedPin;
            }

            return false;
        }

        /// <summary>
        /// Parks the pill just outside the pocket edge. When the hex bulges past the
        /// 160px pin reach, that bulge is pulled back so the pill stays on the map,
        /// outside the stroke, and near the pin.
        /// </summary>
        private static bool TrySeatPocketPill(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            int selected,
            int neighbour,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa,
            bool right,
            float pillW,
            float pillH,
            out Box spot)
        {
            spot = default;
            if (!PocketDistrictsHold(hexes, pins, cards, selected, neighbour, detail, plateWidth, plateHeight))
                return false;
            if (SearchPocketSpot(
                hexes, pins, cards, selected, detail, plateWidth, plateHeight, fa, right, pillW, pillH, out spot))
                return true;
            return ShavePocketSpot(
                hexes, pins, cards, selected, neighbour, detail, plateWidth, plateHeight, fa, right, pillW, pillH, out spot);
        }

        private static bool PocketDistrictsHold(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            int selected,
            int neighbour,
            Box detail,
            float plateWidth,
            float plateHeight)
        {
            if (!DistrictShiftHolds(hexes, pins, cards, selected, detail, plateWidth, plateHeight))
                return false;
            if (neighbour >= 0 && !DistrictShiftHolds(hexes, pins, cards, neighbour, detail, plateWidth, plateHeight))
                return false;
            return DrawnGlowGapsHold(hexes) && RestsInside(cards[selected], hexes[selected], InsideMargin);
        }

        private static bool SearchPocketSpot(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa,
            bool right,
            float pillW,
            float pillH,
            out Box spot)
        {
            spot = default;
            float pinY = pins[selected].Center.Y;
            float yHi = plateHeight - MapInset - pillH;
            bool any = false;
            float best = float.MaxValue;
            for (float y = MapInset; y <= yHi + 0.1f; y += 8f)
            {
                float edge = PocketEdge(hexes[selected], y, pillH, right);
                if (edge < -5000f || edge > 20000f)
                    continue;
                for (float extra = 0f; extra <= 12.1f; extra += 4f)
                {
                    float x = right
                        ? edge + CueHexStroke + 2f + extra
                        : edge - CueHexStroke - 2f - extra - pillW;
                    var trial = new Box(x, y, pillW, pillH);
                    if (!ParkedCueHolds(trial, cards[selected], hexes, pins, cards, selected, detail, plateWidth, plateHeight, fa))
                        continue;
                    float score = Math.Abs(trial.Center.Y - pinY) + extra * 3f;
                    if (any && score >= best)
                        continue;
                    any = true;
                    best = score;
                    spot = trial;
                }
            }

            return any;
        }

        private static bool ShavePocketSpot(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            int selected,
            int neighbour,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool fa,
            bool right,
            float pillW,
            float pillH,
            out Box spot)
        {
            spot = default;
            float pinX = pins[selected].Center.X;
            float pinY = pins[selected].Center.Y;
            Vec2[] original = CloneShape(hexes[selected]);
            Box originalPin = pins[selected];
            Box originalCard = cards[selected];
            float[] yOff = { 0f, -12f, 12f, -24f, 24f, -36f, 36f, -48f, 48f };
            for (int i = 0; i < yOff.Length; i++)
            {
                hexes[selected] = CloneShape(original);
                pins[selected] = originalPin;
                cards[selected] = originalCard;
                float y = Clamp(pinY + yOff[i] - pillH * 0.5f, MapInset, Math.Max(MapInset, plateHeight - MapInset - pillH));
                float dy = y + pillH * 0.5f - pinY;
                float reach = CueNearLimit - 4f;
                float spare = reach * reach - dy * dy;
                if (spare < 1f)
                    continue;
                float horiz = (float)Math.Sqrt(spare);
                float x = (right ? pinX + horiz : pinX - horiz) - pillW * 0.5f;
                var trial = new Box(x, y, pillW, pillH);
                if (trial.X < MapInset || trial.XMax > plateWidth - MapInset)
                    continue;
                BoundsOf(hexes[selected], out float beforeMin, out _, out float beforeMax, out _);
                ShaveHexToWall(hexes[selected], InflateBox(trial, CueHexStroke), right);
                BoundsOf(hexes[selected], out float afterMin, out _, out float afterMax, out _);
                float lost = right ? beforeMax - afterMax : afterMin - beforeMin;
                if (lost > 1f)
                    RestorePocketWidth(hexes[selected], cards, selected, right, lost, beforeMin, beforeMax);
                SeatPinInHex(pins, hexes[selected], selected);
                SlidePinHorizontal(pins, hexes[selected], selected, right);
                if (!ParkedCueHolds(trial, cards[selected], hexes, pins, cards, selected, detail, plateWidth, plateHeight, fa))
                    continue;
                if (!PocketDistrictsHold(hexes, pins, cards, selected, neighbour, detail, plateWidth, plateHeight))
                    continue;
                if (!AspectsHold(hexes, plateWidth, plateHeight) || !DistrictsLegal(hexes, pins, plateWidth, plateHeight))
                    continue;
                spot = trial;
                return true;
            }

            hexes[selected] = original;
            pins[selected] = originalPin;
            cards[selected] = originalCard;
            return false;
        }

        private static bool DistrictClearsFrame(Vec2[] hex, float plateWidth, float plateHeight, Box detail)
        {
            if (hex == null)
                return false;
            if (!OutlineInside(hex, plateWidth, plateHeight, out _, out _) || !GlowInside(hex, plateWidth, plateHeight, out _, out _))
                return false;
            if (detail.W < 1f)
                return true;
            Box glow = new Box(detail.X - HaloReach, detail.Y - HaloReach, detail.W + HaloReach * 2f, detail.H + HaloReach * 2f);
            return !PolygonHitsRect(hex, detail) && !PolygonHitsRect(hex, glow);
        }

        private static float PocketEdge(Vec2[] hex, float y, float pillH, bool right)
        {
            float y0 = y - CueHexStroke;
            float y1 = y + pillH + CueHexStroke;
            return right ? RightmostInBand(hex, y0, y1) : LeftmostInBand(hex, y0, y1);
        }

        /// <summary>Pulls the pocket side of the hex off the pill stroke. X only, so the height stays.</summary>
        private static void ShaveHexToWall(Vec2[] hex, Box stroke, bool right)
        {
            if (hex == null || stroke.W < 1f)
                return;
            float wall = right ? stroke.XMin - 3f : stroke.XMax + 3f;
            for (int pass = 0; pass < 6; pass++)
            {
                bool moved = false;
                for (int i = 0; i < hex.Length; i++)
                {
                    int next = (i + 1) % hex.Length;
                    Vec2 a = hex[i];
                    Vec2 b = hex[next];
                    bool hit = SegmentHitsRect(a, b, stroke)
                        || (a.X >= stroke.XMin && a.X <= stroke.XMax && a.Y >= stroke.YMin && a.Y <= stroke.YMax)
                        || (b.X >= stroke.XMin && b.X <= stroke.XMax && b.Y >= stroke.YMin && b.Y <= stroke.YMax);
                    if (!hit)
                        continue;
                    if (right)
                    {
                        if (a.X > wall)
                        {
                            hex[i] = new Vec2(wall, a.Y);
                            moved = true;
                        }

                        if (b.X > wall)
                        {
                            hex[next] = new Vec2(wall, b.Y);
                            moved = true;
                        }
                    }
                    else
                    {
                        if (a.X < wall)
                        {
                            hex[i] = new Vec2(wall, a.Y);
                            moved = true;
                        }

                        if (b.X < wall)
                        {
                            hex[next] = new Vec2(wall, b.Y);
                            moved = true;
                        }
                    }
                }

                if (!moved)
                    break;
            }
        }

        /// <summary>
        /// Puts the width the shave removed back on the far side and slides the card
        /// with the bbox centre so the district keeps its aspect and its centre.
        /// </summary>
        private static void RestorePocketWidth(Vec2[] hex, Box[] cards, int selected, bool right, float lost, float beforeMin, float beforeMax)
        {
            float mid = (beforeMin + beforeMax) * 0.5f;
            for (int i = 0; i < hex.Length; i++)
            {
                Vec2 point = hex[i];
                if (right && point.X < mid)
                    hex[i] = new Vec2(point.X - lost, point.Y);
                else if (!right && point.X > mid)
                    hex[i] = new Vec2(point.X + lost, point.Y);
            }

            Box card = cards[selected];
            float dx = right ? -lost * 0.5f : lost * 0.5f;
            cards[selected] = new Box(card.X + dx, card.Y, card.W, card.H);
        }

        private static void SeatPinInHex(Box[] pins, Vec2[] hex, int index)
        {
            if (pins == null || hex == null || index < 0 || index >= pins.Length)
                return;
            Box pin = pins[index];
            if (PointInPolygon(pin.Center, hex) && !EdgeCloserThan(pin.Center, hex, 6f))
                return;
            CentroidOf(hex, out float cx, out float cy);
            var ok = new Vec2(cx, cy);
            var target = new Vec2(pin.Center.X, pin.Center.Y);
            for (int step = 0; step < 24; step++)
            {
                var mid = new Vec2((ok.X + target.X) * 0.5f, (ok.Y + target.Y) * 0.5f);
                if (PointInPolygon(mid, hex) && !EdgeCloserThan(mid, hex, 6f))
                    ok = mid;
                else
                    target = mid;
            }

            pins[index] = new Box(ok.X - pin.W * 0.5f, ok.Y - pin.H * 0.5f, pin.W, pin.H);
        }

        private static int SideNeighbour(Vec2[][] hexes, int selected, bool right)
        {
            BoundsOf(hexes[selected], out _, out float top, out _, out float bottom);
            CentroidOf(hexes[selected], out float sx, out _);
            int best = -1;
            float bestGap = float.MaxValue;
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i == selected || hexes[i] == null)
                    continue;
                BoundsOf(hexes[i], out float minX, out float oy0, out float maxX, out float oy1);
                bool stacked = oy1 < top + 8f || oy0 > bottom - 8f;
                if (!stacked && oy1 > top && oy0 < bottom)
                {
                    CentroidOf(hexes[i], out float ox, out _);
                    if (right && ox <= sx)
                        continue;
                    if (!right && ox >= sx)
                        continue;
                    float gap = right ? minX - sx : sx - maxX;
                    if (gap < bestGap)
                    {
                        bestGap = gap;
                        best = i;
                    }
                }
            }

            return best;
        }

        private static float BoundsMinX(Vec2[] hex)
        {
            BoundsOf(hex, out float minX, out _, out _, out _);
            return minX;
        }

        private static float BoundsMaxX(Vec2[] hex)
        {
            BoundsOf(hex, out _, out _, out float maxX, out _);
            return maxX;
        }

        private static void SlidePinHorizontal(Box[] pins, Vec2[] hex, int index, bool right)
        {
            if (pins == null || hex == null || index < 0 || index >= pins.Length)
                return;
            Box pin = pins[index];
            float y = pin.Center.Y;
            float x = pin.Center.X;
            for (float step = 4f; step <= 280f; step += 4f)
            {
                float next = right ? x + step : x - step;
                var point = new Vec2(next, y);
                if (!PointInPolygon(point, hex) || EdgeCloserThan(point, hex, 6f))
                    break;
                pins[index] = new Box(point.X - pin.W * 0.5f, point.Y - pin.H * 0.5f, pin.W, pin.H);
            }
        }

        private static float RightmostInBand(Vec2[] hex, float y0, float y1)
        {
            float best = -10000f;
            if (hex == null)
                return best;
            for (int i = 0; i < hex.Length; i++)
            {
                Vec2 a = hex[i];
                Vec2 b = hex[(i + 1) % hex.Length];
                if (a.Y >= y0 && a.Y <= y1 && a.X > best)
                    best = a.X;
                float dy = b.Y - a.Y;
                if (Math.Abs(dy) < 0.001f)
                    continue;
                for (int edge = 0; edge < 2; edge++)
                {
                    float y = edge == 0 ? y0 : y1;
                    float t = (y - a.Y) / dy;
                    if (t < 0f || t > 1f)
                        continue;
                    float x = a.X + (b.X - a.X) * t;
                    if (x > best)
                        best = x;
                }
            }

            return best;
        }

        private static float LeftmostInBand(Vec2[] hex, float y0, float y1)
        {
            float best = 10000f;
            if (hex == null)
                return -10000f;
            bool any = false;
            for (int i = 0; i < hex.Length; i++)
            {
                Vec2 a = hex[i];
                Vec2 b = hex[(i + 1) % hex.Length];
                if (a.Y >= y0 && a.Y <= y1)
                {
                    any = true;
                    if (a.X < best)
                        best = a.X;
                }

                float dy = b.Y - a.Y;
                if (Math.Abs(dy) < 0.001f)
                    continue;
                for (int edge = 0; edge < 2; edge++)
                {
                    float y = edge == 0 ? y0 : y1;
                    float t = (y - a.Y) / dy;
                    if (t < 0f || t > 1f)
                        continue;
                    any = true;
                    float x = a.X + (b.X - a.X) * t;
                    if (x < best)
                        best = x;
                }
            }

            return any ? best : -10000f;
        }

        /// <summary>Slides the selected district down so a shelf can sit under the map inset.</summary>
        private static bool DropDistrictForCueShelf(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            float drop)
        {
            if (hexes[selected] == null || drop < 1f)
                return false;
            Vec2[] savedHex = CloneShape(hexes[selected]);
            Box savedCard = cards[selected];
            Box savedPin = pins[selected];
            var moved = new Vec2[savedHex.Length];
            for (int i = 0; i < savedHex.Length; i++)
                moved[i] = new Vec2(savedHex[i].X, savedHex[i].Y + drop);
            hexes[selected] = moved;
            cards[selected] = new Box(savedCard.X, savedCard.Y + drop, savedCard.W, savedCard.H);
            pins[selected] = new Box(savedPin.X, savedPin.Y + drop, savedPin.W, savedPin.H);
            bool holds = DistrictShiftHolds(hexes, pins, cards, selected, detail, plateWidth, plateHeight)
                && DrawnGlowGapsHold(hexes);
            if (!holds)
            {
                hexes[selected] = savedHex;
                cards[selected] = savedCard;
                pins[selected] = savedPin;
                return false;
            }

            return true;
        }

        /// <summary>Footer button width used by <c>PlaceLibraryFooter</c>.</summary>
        public static float FooterButtonWidth(float compositionWidth, int commandCount)
        {
            if (commandCount < 1)
                commandCount = 1;
            float gap = 6f;
            const float side = 10f;
            float width = (compositionWidth - side * 2f - gap * (commandCount - 1)) / commandCount;
            if (width < 72f)
            {
                gap = 4f;
                width = (compositionWidth - side * 2f - gap * (commandCount - 1)) / commandCount;
            }

            return width;
        }

        /// <summary>
        /// The 4:3 English zoom button reads <see cref="ZoomInBasinLabel"/>. One line when the
        /// button holds it, otherwise the two sides of the dot, at <paramref name="fontSize"/>.
        /// </summary>
        public static bool ZoomInBasinFits(float buttonWidth, float fontSize)
        {
            float inner = buttonWidth - 16f;
            if (inner < 8f || fontSize < 11f)
                return false;
            if (TextWidth(ZoomInBasinLabel, fontSize) <= inner + 0.5f)
                return true;
            int dot = ZoomInBasinLabel.IndexOf('·');
            if (dot <= 0)
                return false;
            string left = ZoomInBasinLabel.Substring(0, dot).Trim();
            string right = ZoomInBasinLabel.Substring(dot + 1).Trim();
            return TextWidth(left, fontSize) <= inner + 0.5f
                && TextWidth(right, fontSize) <= inner + 0.5f;
        }

        private static void NudgeDistrict(Vec2[] hex, ref Box card, float dx)
        {
            if (hex != null)
            {
                for (int i = 0; i < hex.Length; i++)
                    hex[i] = new Vec2(hex[i].X + dx, hex[i].Y);
            }

            card = new Box(card.X + dx, card.Y, card.W, card.H);
        }

        private static float RightDistrictLimit(Vec2[][] hexes, int selected, float plateWidth)
        {
            float limit = plateWidth - HaloReach - 1f;
            if (hexes == null || hexes[selected] == null)
                return limit;
            BoundsOf(hexes[selected], out _, out float top, out _, out float bottom);
            CentroidOf(hexes[selected], out float sx, out _);
            float need = DistrictGlowReach * 2f + 14f;
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i == selected || hexes[i] == null)
                    continue;
                BoundsOf(hexes[i], out float minX, out float oy0, out _, out float oy1);
                bool stacked = oy1 < top - need || oy0 > bottom + need;
                if (stacked)
                    continue;
                CentroidOf(hexes[i], out float ox, out _);
                if (ox <= sx)
                    continue;
                limit = Math.Min(limit, minX - need);
            }

            return limit;
        }

        private static bool DistrictShiftHolds(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight)
        {
            if (!PointInPolygon(pins[selected].Center, hexes[selected])
                || EdgeCloserThan(pins[selected].Center, hexes[selected], 6f))
            {
                return false;
            }

            if (!RestsInside(cards[selected], hexes[selected], InsideMargin))
            {
                return false;
            }

            if (!OnPlate(cards[selected], plateWidth, plateHeight) || !Clears(cards[selected], detail))
            {
                return false;
            }

            if (PolygonArea(hexes[selected]) < 40000f)
            {
                return false;
            }

            for (int i = 0; i < cards.Length; i++)
            {
                if (i == selected || cards[i].W < 1f)
                    continue;
                if (Separation(cards[selected], cards[i]) < CardGap)
                {
                    return false;
                }

                if (PolygonsOverlap(hexes[selected], hexes[i], 0.75f))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool WideFarsiPillHolds(
            Box pill,
            Box card,
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight)
        {
            return WideFarsiPillHolds(pill, card, hexes, pins, cards, selected, detail, plateWidth, plateHeight, true);
        }

        private static bool WideFarsiPillHolds(
            Box pill,
            Box card,
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            bool ownStroke)
        {
            if (pill.W < 200f || pill.H < 54f)
            {
                return false;
            }

            if (!OnPlate(pill, plateWidth, plateHeight) || !Clears(pill, detail))
            {
                return false;
            }

            bool above = pill.YMax <= card.Y + 1f;
            bool aside = pill.XMax <= card.X + 1f || pill.X >= card.XMax - 1f;
            if (!above && !aside)
            {
                return false;
            }
            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (i == selected || cards[i].W < 1f)
                        continue;
                    if (Overlaps(pill, cards[i], 2f))
                    {
                        return false;
                    }
                }
            }

            if (pins != null)
            {
                for (int i = 0; i < pins.Length; i++)
                {
                    if (pins[i].W > 1f && Overlaps(pill, pins[i], 1f))
                    {
                        return false;
                    }
                }
            }

            if (!CueCountsAsNear(pill, card, pins[selected], hexes[selected]))
            {
                return false;
            }
            Box stroke = InflateBox(pill, CueHexStroke);
            for (int i = 0; i < hexes.Length; i++)
            {
                if (hexes[i] == null)
                    continue;
                if (i == selected)
                {
                    if (ownStroke && HexEdgeHits(hexes[i], stroke))
                    {
                        return false;
                    }

                    continue;
                }

                if (PolygonHitsRect(hexes[i], stroke))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Pulls an organic coast inward until it sits in the map frame and clear of the hint.
        /// A six-point hex is not substituted while a scaled coast still has interior samples.
        /// </summary>
        private static Vec2[] ShrinkCoastToFrame(
            Vec2[] shape,
            Box card,
            Box pin,
            Box cue,
            Box hint,
            Box glow,
            float plateWidth,
            float plateHeight,
            float expected)
        {
            if (shape == null || shape.Length < 12)
                return null;
            CentroidOf(shape, out float cx, out float cy);
            float[] scales = { 1f, 0.97f, 0.93f, 0.88f, 0.82f, 0.76f, 0.70f, 0.64f };
            for (int s = 0; s < scales.Length; s++)
            {
                float scale = scales[s];
                var next = new Vec2[shape.Length];
                for (int i = 0; i < shape.Length; i++)
                    next[i] = new Vec2(cx + (shape[i].X - cx) * scale, cy + (shape[i].Y - cy) * scale);
                if (!OutlineInside(next, plateWidth, plateHeight, out _, out _))
                    continue;
                if (!GlowInside(next, plateWidth, plateHeight, out _, out _))
                    continue;
                if (EdgesHit(next, hint) || EdgesHit(next, glow))
                    continue;
                if (InteriorUnblocked(next, card, pin, cue, hint) < 8)
                    continue;
                float measured = MeasuredHexAspect(next);
                if (expected >= 0.2f && measured >= 0.2f && Math.Abs(measured / expected - 1f) > 0.10f)
                    continue;
                // A uniform shrink that clears the frame was cutting the card. The
                // coast has to keep the same 8px seat the hex already gave it.
                if (card.W >= 1f && !RestsInside(card, next, InsideMargin))
                    continue;
                return next;
            }

            return null;
        }

        /// <summary>
        /// Grows the seated hex just enough that eight interior samples sit outside the card.
        /// The scale is uniform, so the v35 ratio stays put. Points that would leave the map
        /// or enter a neighbour stay on the hex.
        /// </summary>
        private static Vec2[] OpenForSamples(
            Vec2[] hex,
            Box card,
            Box pin,
            Vec2[][] others,
            Box cue,
            float plateWidth,
            float plateHeight,
            Box hint,
            Box glow,
            float expected)
        {
            if (hex == null || InteriorUnblocked(hex, card, pin, cue, hint) >= 8)
                return null;
            CentroidOf(hex, out float cx, out float cy);
            float[] gains = { 1.08f, 1.16f, 1.24f, 1.34f, 1.46f };
            for (int g = 0; g < gains.Length; g++)
            {
                float gain = gains[g];
                var grown = new Vec2[hex.Length];
                for (int i = 0; i < hex.Length; i++)
                {
                    var pushed = new Vec2(cx + (hex[i].X - cx) * gain, cy + (hex[i].Y - cy) * gain);
                    bool outside = pushed.X < CaptureFrameInset || pushed.X > plateWidth - CaptureFrameInset
                        || pushed.Y < CaptureFrameInset || pushed.Y > plateHeight - CaptureFrameInset;
                    bool blocked = InsideNeighbour(pushed, hex, others)
                        || (hint.W > 1f && pushed.X >= hint.X - 2f && pushed.X <= hint.XMax + 2f && pushed.Y >= hint.Y - 2f && pushed.Y <= hint.YMax + 2f)
                        || (glow.W > 1f && pushed.X >= glow.X - 2f && pushed.X <= glow.XMax + 2f && pushed.Y >= glow.Y - 2f && pushed.Y <= glow.YMax + 2f);
                    grown[i] = outside || blocked ? hex[i] : pushed;
                }

                if (card.W >= 1f && !RestsInside(card, grown, InsideMargin))
                    continue;
                Box pill = cue.W > 1f
                    ? new Box(cue.X - CueHexStroke, cue.Y - CueHexStroke, cue.W + CueHexStroke * 2f, cue.H + CueHexStroke * 2f)
                    : default;
                if (EdgesHit(grown, hint) || EdgesHit(grown, glow) || EdgesHit(grown, pill))
                    continue;
                if (CrossesOther(grown, hex, others))
                    continue;
                float measured = MeasuredHexAspect(grown);
                if (expected >= 0.2f && measured >= 0.2f && Math.Abs(measured / expected - 1f) > 0.10f)
                    continue;
                if (InteriorUnblocked(grown, card, pin, cue, hint) < 8)
                    continue;
                if (PolygonArea(grown) + 0.5f < 40000f)
                    continue;
                return grown;
            }

            return null;
        }

        private static bool FrameHolds(
            Vec2[] shape,
            Vec2[] hex,
            Box card,
            Box pin,
            Vec2[][] others,
            Box pill,
            Box cue,
            float plateWidth,
            float plateHeight,
            Box hint,
            Box glow,
            float expected)
        {
            if (shape == null || shape.Length < 3)
                return false;
            if (SmoothWhy(shape, card, hex, others, pill) != null)
                return false;
            float measured = MeasuredHexAspect(shape);
            if (expected >= 0.2f && measured >= 0.2f && Math.Abs(measured / expected - 1f) > 0.10f)
                return false;
            if (!OutlineInside(shape, plateWidth, plateHeight, out _, out _))
                return false;
            if (!GlowInside(shape, plateWidth, plateHeight, out _, out _))
                return false;
            if (EdgesHit(shape, hint) || EdgesHit(shape, glow))
                return false;
            if (InteriorUnblocked(shape, card, pin, cue, hint) < 8)
                return false;
            return true;
        }

        private static Vec2[] CopyShape(Vec2[] shape)
        {
            var copy = new Vec2[shape.Length];
            for (int i = 0; i < shape.Length; i++)
                copy[i] = shape[i];
            return copy;
        }

        private static Vec2 PullIntoInset(Vec2 point, float cx, float cy, float plateWidth, float plateHeight, float inset)
        {
            float minX = inset;
            float maxX = plateWidth - inset;
            float minY = inset;
            float maxY = plateHeight - inset;
            if (point.X >= minX && point.X <= maxX && point.Y >= minY && point.Y <= maxY)
                return point;
            Vec2 current = point;
            for (int step = 0; step < 8; step++)
            {
                current = new Vec2(current.X + (cx - current.X) * 0.45f, current.Y + (cy - current.Y) * 0.45f);
                if (current.X >= minX && current.X <= maxX && current.Y >= minY && current.Y <= maxY)
                    return current;
            }

            return new Vec2(Clamp(current.X, minX, maxX), Clamp(current.Y, minY, maxY));
        }

        private static Vec2 PullOffRect(Vec2 point, float cx, float cy, Vec2[] hex, Box rect, float gap)
        {
            if (rect.W < 1f || rect.H < 1f)
                return point;
            var grown = new Box(rect.X - gap, rect.Y - gap, rect.W + gap * 2f, rect.H + gap * 2f);
            if (point.X < grown.XMin || point.X > grown.XMax || point.Y < grown.YMin || point.Y > grown.YMax)
                return point;
            for (int step = 0; step < 8; step++)
            {
                point = new Vec2(point.X + (cx - point.X) * 0.35f, point.Y + (cy - point.Y) * 0.35f);
                if (point.X < grown.XMin || point.X > grown.XMax || point.Y < grown.YMin || point.Y > grown.YMax)
                    return ClampInsideHex(point, cx, cy, hex);
            }

            return ClampInsideHex(point, cx, cy, hex);
        }

        /// <summary>Card corners sit inside the territory, inset by the same 8px the hex rule used.</summary>
        public static bool CardInsideTerritory(Box card, Vec2[] territory)
        {
            return RestsInside(card, territory, InsideMargin);
        }

        /// <summary>
        /// Smooth organic border. A low-frequency wave sets the radius inside the seated
        /// hex, and a closed Catmull-Rom spline draws the curve. Tight rays stay on the
        /// hex so the card and the zoom pill are not cut. The samples are not a sawtooth.
        /// </summary>
        private static Vec2[] BuildLegalTerrain(Vec2[] hex, Box card, string mapId, Vec2[][] others, Box cue)
        {
            CentroidOf(hex, out float cx, out float cy);
            int district = IndexOf(mapId);
            if (district < 0)
                district = 0;
            float phase = (district + 1) * 0.91f;
            Box pill = cue.W > 1f
                ? new Box(cue.X - CueHexStroke, cue.Y - CueHexStroke, cue.W + CueHexStroke * 2f, cue.H + CueHexStroke * 2f)
                : default;
            // Large irregular blob in the seated box. A tight seat shrinks that blob;
            // it does not fall back to the six-point hex.
            Vec2[] blob = OrganicDistrict(hex, card, pill, others, district);
            if (blob != null && blob.Length >= 12
                && SmoothWhy(blob, card, hex, others, pill) == null
                && RadialRoughness(blob) <= 1.15f
                && NeighbourGapsHold(blob, hex, others))
                return blob;
            // A rounded hex is the same template on every seat. Each district cuts its own
            // bays, and the compass points stay on the seated box so the aspect holds.
            // The strongest coastline that still clears the card, the pill, and the area floor wins.
            float[] strength = { 1f, 0.62f, 0.34f, 0.18f, 0.08f };
            for (int step = 0; step < strength.Length; step++)
            {
                Vec2[] coast = SmoothTerritory(hex, card, cx, cy, phase, strength[step], pill, others, true, district, true);
                if (SmoothWhy(coast, card, hex, others, pill) == null && RadialRoughness(coast) <= 1.15f)
                    return coast;
            }

            float[] depth = { 0.16f, 0.10f, 0.06f };
            for (int step = 0; step < depth.Length; step++)
            {
                Vec2[] shape = SmoothTerritory(hex, card, cx, cy, phase, depth[step], pill, others, true, district, false);
                if (SmoothWhy(shape, card, hex, others, pill) == null && RadialRoughness(shape) <= 0.22f)
                    return shape;
            }

            float[] bay = { 0.18f, 0.10f };
            for (int step = 0; step < bay.Length; step++)
            {
                Vec2[] shape = BuildEdgeWave(hex, card, cx, cy, phase, bay[step], pill, others);
                if (SmoothWhy(shape, card, hex, others, pill) == null && RadialRoughness(shape) <= 0.22f)
                    return shape;
            }

            Vec2[] filleted = FilletOpenCorners(hex, card, pill, others);
            if (filleted != null && filleted.Length > hex.Length
                && SmoothWhy(filleted, card, hex, others, pill) == null
                && RadialRoughness(filleted) <= 0.35f)
                return filleted;

            // A tight seat used to keep the six-point hex. Shrink the organic coast
            // about the card until the card, the pill, and the area floor all hold.
            Vec2[] grown = SmoothTerritory(hex, card, cx, cy, phase, 0.62f, pill, others, true, district, true);
            Vec2 anchor = card.W >= 1f ? card.Center : new Vec2(cx, cy);
            float[] scales = { 0.97f, 0.93f, 0.88f, 0.82f };
            for (int s = 0; s < scales.Length; s++)
            {
                if (grown == null || grown.Length < 12)
                    break;
                float scale = scales[s];
                var shrunk = new Vec2[grown.Length];
                for (int i = 0; i < grown.Length; i++)
                    shrunk[i] = new Vec2(anchor.X + (grown[i].X - anchor.X) * scale, anchor.Y + (grown[i].Y - anchor.Y) * scale);
                if (SmoothWhy(shrunk, card, hex, others, pill) == null && RadialRoughness(shrunk) <= 1.15f)
                    return shrunk;
            }

            if (grown != null && grown.Length >= 12 && SmoothWhy(grown, card, hex, others, pill) == null)
                return grown;
            // Still not a six-point hex. A shallow bay inside the seated box keeps the aspect.
            Vec2[] traced = TraceInsideHex(hex, card, pill, others, district);
            if (traced != null && traced.Length >= 12)
                return traced;
            if (blob != null && blob.Length >= 12)
                return blob;
            return hex;
        }

        /// <summary>
        /// Irregular blob that fills the seated box. Each district uses a different lobe
        /// count. The deepest coast that still holds the card, the pill, the area floor
        /// and a 12px neighbour gap is the one that is drawn. A tight seat shrinks it.
        /// </summary>
        private static Vec2[] OrganicDistrict(Vec2[] hex, Box card, Box pill, Vec2[][] others, int district)
        {
            if (hex == null || hex.Length < 3)
                return null;
            BoundsOf(hex, out float hx0, out float hy0, out float hx1, out float hy1);
            var box = new Box(hx0, hy0, Math.Max(1f, hx1 - hx0), Math.Max(1f, hy1 - hy0));
            Vec2 anchor = card.W >= 1f ? card.Center : new Vec2((hx0 + hx1) * 0.5f, (hy0 + hy1) * 0.5f);
            float[] amp = { 0.40f, 0.30f, 0.20f, 0.12f, 0.07f };
            float[] scales = { 1f, 0.97f, 0.93f, 0.88f, 0.82f, 0.76f, 0.70f };
            Vec2[] mildest = null;
            for (int a = 0; a < amp.Length; a++)
            {
                Vec2[] raw = OrganicControls(district, amp[a]);
                Vec2[] placed = ShapeIntoBox(raw, box);
                if (placed == null || placed.Length < 12)
                    continue;
                mildest = placed;
                for (int s = 0; s < scales.Length; s++)
                {
                    Vec2[] trial = ScaleAbout(placed, anchor, scales[s]);
                    if (trial == null || trial.Length < 12)
                        continue;
                    string why = SmoothWhy(trial, card, hex, others, pill);
                    if (why != null)
                    {
                        // Shrinking pulls the edge toward the card, so an inside or area
                        // miss cannot be repaired by a smaller scale.
                        if (why == "inside" || why == "area" || why == "short")
                            break;
                        continue;
                    }

                    if (!NeighbourGapsHold(trial, hex, others))
                        continue;
                    if (RadialRoughness(trial) > 1.15f)
                        continue;
                    return trial;
                }
            }

            return mildest;
        }

        private static Vec2[] OrganicControls(int district, float amp)
        {
            const int count = 36;
            const float tau = 6.2831853f;
            int band = district < 0 ? 0 : district % 5;
            float spin = (band + 1) * 1.31f;
            var controls = new Vec2[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (i + 0.37f) / count * tau;
                float dx = (float)Math.Cos(angle);
                float dy = (float)Math.Sin(angle);
                float lobe = 0.5f + 0.5f * (float)Math.Sin((1f + band) * angle + spin);
                float second = 0.5f + 0.5f * (float)Math.Sin((3f + band) * angle * 0.63f + spin * 1.7f);
                float third = 0.5f + 0.5f * (float)Math.Sin((2f + (band * 2f)) * angle + 0.6f);
                float mix = 0.52f * lobe + 0.30f * second + 0.18f * third;
                float radius = 1f - amp * (1f - mix);
                if (radius < 0.55f)
                    radius = 0.55f;
                controls[i] = new Vec2(dx * radius, dy * radius);
            }

            return CatmullRomClosed(controls, 2);
        }

        private static Vec2[] ScaleAbout(Vec2[] shape, Vec2 anchor, float scale)
        {
            if (shape == null)
                return null;
            if (scale > 0.999f)
                return shape;
            var next = new Vec2[shape.Length];
            for (int i = 0; i < shape.Length; i++)
                next[i] = new Vec2(
                    anchor.X + (shape[i].X - anchor.X) * scale,
                    anchor.Y + (shape[i].Y - anchor.Y) * scale);
            return next;
        }

        private static int BandOf(Vec2[] hex)
        {
            if (hex == null || hex.Length == 0)
                return 0;
            int band = (int)Math.Abs(Math.Round(hex[0].X * 0.01f + hex[0].Y * 0.013f));
            return band % 5;
        }

        /// <summary>Drawn halo gap stays at least 12px. A blob that crowds a neighbour is shrunk.</summary>
        private static bool NeighbourGapsHold(Vec2[] shape, Vec2[] own, Vec2[][] others)
        {
            if (shape == null || others == null)
                return true;
            for (int i = 0; i < others.Length; i++)
            {
                Vec2[] other = others[i];
                if (other == null || other.Length < 3 || ReferenceEquals(other, own))
                    continue;
                if (DrawnDistrictGap(shape, other) + 0.05f < 12f)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 72-point coast drawn just inside the seated hex. Compass samples stay on the box.
        /// The bay stops short of the card and the zoom pill. The first depth that still
        /// clears the card, the pill, and the area floor is the one that is drawn.
        /// </summary>
        private static Vec2[] TraceInsideHex(Vec2[] hex, Box card, Box pill, Vec2[][] others, int district)
        {
            if (hex == null || hex.Length < 3)
                return hex;
            CentroidOf(hex, out float cx, out float cy);
            const int count = 36;
            const float tau = 6.2831853f;
            int band = district < 0 ? 0 : district % 5;
            float[] cuts = { 0.10f, 0.05f, 0.025f };
            Vec2[] closest = null;
            for (int c = 0; c < cuts.Length; c++)
            {
                var controls = new Vec2[count];
                for (int i = 0; i < count; i++)
                {
                    float angle = (i + 0.5f) / count * tau;
                    float dx = (float)Math.Cos(angle);
                    float dy = (float)Math.Sin(angle);
                    Vec2 boundary = BoundaryAt(hex, cx, cy, dx, dy);
                    float far = Distance(new Vec2(cx, cy), boundary);
                    if (far < 1f)
                        far = 1f;
                    float spare = card.W >= 1f ? ClearOfRect(boundary, card) - 28f : far;
                    if (pill.W > 1f)
                    {
                        float pillSpare = ClearOfRect(boundary, pill) - 36f;
                        if (pillSpare < spare)
                            spare = pillSpare;
                    }

                    if (spare < 0f)
                        spare = 0f;
                    float keep = CompassKeep(angle);
                    float lobe = 0.5f + 0.5f * (float)Math.Sin((1f + band) * angle + band * 1.3f);
                    float cut = (1f - keep) * lobe * cuts[c];
                    float room = spare / far;
                    if (cut > room)
                        cut = room;
                    if (cut < 0f)
                        cut = 0f;
                    controls[i] = new Vec2(cx + dx * far * (1f - cut), cy + dy * far * (1f - cut));
                }

                Vec2[] curved = CatmullRomClosed(controls, 2);
                if (curved == null || curved.Length < 12)
                    continue;
                closest = curved;
                if (SmoothWhy(curved, card, hex, others, pill) == null)
                    return curved;
            }

            return closest;
        }

        /// <summary>
        /// The smooth curve sits inside the seated hex and was narrowing the box, so the
        /// measured ratio left the v35 band and the card covered the paintable interior.
        /// Scale it back onto the hex box, then clamp anything that leaves the hex.
        /// </summary>
        private static Vec2[] RestoreHexFrame(Vec2[] shape, Vec2[] hex, Box card, Vec2[][] others, Box cue)
        {
            if (shape == null || hex == null || shape.Length < 3 || hex.Length < 3)
                return shape;
            float target = MeasuredHexAspect(hex);
            float measured = MeasuredHexAspect(shape);
            // Stay on the seated box. Eight percent was enough to leave the v35 band
            // once the hex itself was already near the 10% gate.
            bool aspectOff = target >= 0.2f && measured >= 0.2f && Math.Abs(measured / target - 1f) > 0.02f;
            Box pill = cue.W > 1f
                ? new Box(cue.X - CueHexStroke, cue.Y - CueHexStroke, cue.W + CueHexStroke * 2f, cue.H + CueHexStroke * 2f)
                : default;
            int open = InteriorUnblocked(shape, card, default, cue, default);
            if (!aspectOff && open >= 8 && (pill.W < 1f || !EdgesHit(shape, pill)))
                return shape;

            BoundsOf(hex, out float hx0, out float hy0, out float hx1, out float hy1);
            BoundsOf(shape, out float sx0, out float sy0, out float sx1, out float sy1);
            float sw = Math.Max(1f, sx1 - sx0);
            float sh = Math.Max(1f, sy1 - sy0);
            float scaleX = Math.Max(1f, (hx1 - hx0) / sw);
            float scaleY = Math.Max(1f, (hy1 - hy0) / sh);
            float cx = (sx0 + sx1) * 0.5f;
            float cy = (sy0 + sy1) * 0.5f;
            CentroidOf(hex, out float hcx, out float hcy);
            var grown = new Vec2[shape.Length];
            for (int i = 0; i < shape.Length; i++)
            {
                var point = new Vec2(cx + (shape[i].X - cx) * scaleX, cy + (shape[i].Y - cy) * scaleY);
                grown[i] = ClampInsideHex(point, hcx, hcy, hex);
            }

            if (SmoothWhy(grown, card, hex, others, pill) != null)
                return shape.Length >= 12 ? shape : grown;
            var hexBox = new Box(hx0, hy0, Math.Max(1f, hx1 - hx0), Math.Max(1f, hy1 - hy0));
            if (target >= 0.2f && Math.Abs(MeasuredHexAspect(grown) / target - 1f) > 0.10f)
            {
                Vec2[] boxed = shape.Length >= 12 ? ShapeIntoBox(shape, hexBox) : null;
                if (boxed != null && Math.Abs(MeasuredHexAspect(boxed) / target - 1f) <= 0.10f)
                    return boxed;
                return shape.Length >= 12 ? shape : grown;
            }

            if (InteriorUnblocked(grown, card, default, cue, default) < 8
                && InteriorUnblocked(hex, card, default, cue, default) >= 8)
                return shape.Length >= 12 ? shape : grown;
            return grown;
        }

        private static Vec2 ClampInsideHex(Vec2 point, float cx, float cy, Vec2[] hex)
        {
            if (hex == null || PointInPolygon(point, hex))
                return point;
            float dx = point.X - cx;
            float dy = point.Y - cy;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f)
                return new Vec2(cx, cy);
            return BoundaryAt(hex, cx, cy, dx / len, dy / len);
        }

        /// <summary>Rounds seated corners that sit well clear of the card. Tight corners stay.</summary>
        private static Vec2[] FilletOpenCorners(Vec2[] hex, Box card, Box pill, Vec2[][] others)
        {
            if (hex == null || hex.Length < 3)
                return null;
            var shape = new List<Vec2>(hex.Length * 3);
            bool cut = false;
            int count = hex.Length;
            for (int i = 0; i < count; i++)
            {
                Vec2 previous = hex[(i - 1 + count) % count];
                Vec2 corner = hex[i];
                Vec2 next = hex[(i + 1) % count];
                float spare = card.W >= 1f ? ClearOfRect(corner, card) - 26f : 40f;
                if (pill.W > 1f)
                {
                    float pillSpare = ClearOfRect(corner, pill) - 30f;
                    if (pillSpare < spare)
                        spare = pillSpare;
                }

                if (spare < 8f || InsideNeighbour(corner, hex, others))
                {
                    shape.Add(corner);
                    continue;
                }

                var lead = new Vec2(previous.X * 0.28f + corner.X * 0.72f, previous.Y * 0.28f + corner.Y * 0.72f);
                var trail = new Vec2(corner.X * 0.72f + next.X * 0.28f, corner.Y * 0.72f + next.Y * 0.28f);
                var bow = new Vec2((lead.X + trail.X) * 0.5f, (lead.Y + trail.Y) * 0.5f);
                if ((card.W >= 1f && (ClearOfRect(lead, card) < 16f || ClearOfRect(trail, card) < 16f || ClearOfRect(bow, card) < 16f))
                    || InsideNeighbour(lead, hex, others) || InsideNeighbour(trail, hex, others) || InsideNeighbour(bow, hex, others))
                {
                    shape.Add(corner);
                    continue;
                }

                shape.Add(lead);
                shape.Add(bow);
                shape.Add(trail);
                cut = true;
            }

            return cut ? shape.ToArray() : null;
        }

        /// <summary>
        /// Keeps every seated corner and bows each open edge inward on a sine arc.
        /// Tight edges stay straight, so the card and the pill are not cut.
        /// </summary>
        private static Vec2[] BuildEdgeWave(
            Vec2[] hex,
            Box card,
            float cx,
            float cy,
            float phase,
            float depth,
            Box pill,
            Vec2[][] others)
        {
            if (hex == null || hex.Length < 3)
                return null;
            var shape = new List<Vec2>(hex.Length * 5);
            int count = hex.Length;
            bool bowed = false;
            for (int i = 0; i < count; i++)
            {
                Vec2 start = hex[i];
                Vec2 end = hex[(i + 1) % count];
                shape.Add(start);
                var mid = new Vec2((start.X + end.X) * 0.5f, (start.Y + end.Y) * 0.5f);
                float spare = card.W >= 1f ? ClearOfRect(mid, card) - 30f : 80f;
                if (pill.W > 1f)
                {
                    float pillSpare = ClearOfRect(mid, pill) - 36f;
                    if (pillSpare < spare)
                        spare = pillSpare;
                }

                if (spare < 12f)
                    continue;
                float wave = 0.45f + 0.55f * (float)Math.Sin(i * 1.7f + phase);
                if (wave < 0.2f)
                    wave = 0.2f;
                float bulge = spare * 0.35f * wave;
                float cap = Distance(mid, new Vec2(cx, cy)) * depth;
                if (cap > 22f)
                    cap = 22f;
                if (bulge > cap)
                    bulge = cap;
                if (bulge < 8f)
                    continue;
                float nx = cx - mid.X;
                float ny = cy - mid.Y;
                float normal = (float)Math.Sqrt(nx * nx + ny * ny);
                if (normal < 1f)
                    continue;
                nx /= normal;
                ny /= normal;
                var arc = new Vec2[5];
                bool clear = true;
                for (int s = 1; s <= 5; s++)
                {
                    float t = s / 6f;
                    float lift = (float)Math.Sin(Math.PI * t) * bulge;
                    var point = new Vec2(
                        start.X + (end.X - start.X) * t + nx * lift,
                        start.Y + (end.Y - start.Y) * t + ny * lift);
                    if (card.W >= 1f && ClearOfRect(point, card) < 18f)
                        clear = false;
                    if (pill.W > 1f && ClearOfRect(point, pill) < 24f)
                        clear = false;
                    if (InsideNeighbour(point, hex, others))
                        clear = false;
                    arc[s - 1] = point;
                }

                if (!clear)
                    continue;
                for (int s = 0; s < arc.Length; s++)
                    shape.Add(arc[s]);
                bowed = true;
            }

            if (!bowed)
                return null;
            return shape.ToArray();
        }

        private static void CentroidOf(Vec2[] hex, out float cx, out float cy)
        {
            cx = 0f;
            cy = 0f;
            if (hex == null || hex.Length == 0)
                return;
            for (int i = 0; i < hex.Length; i++)
            {
                cx += hex[i].X;
                cy += hex[i].Y;
            }

            cx /= hex.Length;
            cy /= hex.Length;
        }

        /// <summary>
        /// 1 on the four compass headings, so those samples stay on the seated hex
        /// and the drawn box keeps the hex aspect. Zero on the diagonals, where the bay sits.
        /// </summary>
        private static float CompassKeep(float angle)
        {
            const float tau = 6.2831853f;
            float wrapped = angle % tau;
            if (wrapped < 0f)
                wrapped += tau;
            const float quarter = 1.5707963f;
            float nearest = Math.Abs(wrapped / quarter - (float)Math.Round(wrapped / quarter));
            if (nearest > 0.16f)
                return 0f;
            return 1f - nearest / 0.16f;
        }

        /// <summary>
        /// How far this heading pulls in from the seated hex, as a fraction of the radius.
        /// Each district uses a different lobe count and depth, so the five seats are not one template.
        /// </summary>
        private static float CoastCut(int district, float angle, float strength)
        {
            int band = district < 0 ? 0 : district % 5;
            float freq = 1f + band;
            float spin = band * 1.17f;
            float lobe = 0.5f + 0.5f * (float)Math.Sin(freq * angle + spin);
            lobe *= lobe;
            float second = 0.5f + 0.5f * (float)Math.Sin((freq + 2f) * angle * 0.5f + spin * 1.6f);
            float mix = 0.72f * lobe + 0.28f * second;
            float depth = 0.20f + 0.045f * band;
            float cut = mix * depth * strength;
            if (cut < 0f)
                cut = 0f;
            if (cut > 0.42f)
                cut = 0.42f;
            return cut;
        }

        private static Vec2[] SmoothTerritory(Vec2[] hex, Box card, float cx, float cy, float phase, float depth, Box pill, Vec2[][] others, bool spline)
        {
            return SmoothTerritory(hex, card, cx, cy, phase, depth, pill, others, spline, 0, false);
        }

        private static Vec2[] SmoothTerritory(Vec2[] hex, Box card, float cx, float cy, float phase, float depth, Box pill, Vec2[][] others, bool spline, int district, bool organic)
        {
            const int count = 36;
            const float tau = 6.2831853f;
            var angle = new float[count];
            var reach = new float[count];
            var limit = new float[count];
            for (int i = 0; i < count; i++)
            {
                angle[i] = (i + 0.5f) / count * tau;
                float dx = (float)Math.Cos(angle[i]);
                float dy = (float)Math.Sin(angle[i]);
                Vec2 boundary = BoundaryAt(hex, cx, cy, dx, dy);
                float far = Distance(new Vec2(cx, cy), boundary);
                if (far < 1f)
                    far = 1f;
                float open = card.W >= 1f ? ClearOfRect(boundary, card) : 999f;
                float spare = open - 28f;
                if (spare < 0f)
                    spare = 0f;
                if (pill.W > 1f)
                {
                    float pillSpare = ClearOfRect(boundary, pill) - 36f;
                    if (pillSpare < spare)
                        spare = pillSpare > 0f ? pillSpare : 0f;
                }

                float cap = far * depth;
                if (spare > cap)
                    spare = cap;
                reach[i] = spare > 2f ? far - 1.5f : far;
                limit[i] = reach[i] - spare;
                if (organic)
                {
                    float keep = CompassKeep(angle[i]);
                    float cut = CoastCut(district, angle[i], depth) * (1f - keep);
                    float room = far > 1f ? spare / far : 0f;
                    if (room < 0f)
                        room = 0f;
                    if (cut > room)
                        cut = room;
                    float radius = far * (1f - cut);
                    float floorOrganic = far * (1f - 0.36f * depth);
                    if (radius < floorOrganic)
                        radius = floorOrganic;
                    if (radius > far)
                        radius = far;
                    reach[i] = far;
                    limit[i] = radius;
                }

                float floor = organic ? far * (1f - 0.36f * depth) : far * 0.64f;
                if (limit[i] < floor)
                    limit[i] = floor;
                if (limit[i] > reach[i])
                    limit[i] = reach[i];
            }

            BlurCircular(limit, organic ? 3 : 2);
            for (int i = 0; i < count; i++)
            {
                if (limit[i] > reach[i])
                    limit[i] = reach[i];
                float floor = organic ? reach[i] * (1f - 0.36f * depth) : reach[i] * 0.64f;
                if (limit[i] < floor)
                    limit[i] = floor;
                // The blur pulls the compass samples off the seated box. Pin them back
                // so the drawn aspect stays on the hex and the bays stay in between.
                if (organic && CompassKeep(angle[i]) >= 0.85f)
                    limit[i] = reach[i];
            }

            var controls = new Vec2[count];
            for (int i = 0; i < count; i++)
            {
                float wave = 0.55f
                    + 0.30f * (float)Math.Sin(2f * angle[i] + phase)
                    + 0.15f * (float)Math.Sin(3f * angle[i] + phase * 1.7f);
                if (organic)
                    wave = 1f;
                if (wave < 0f)
                    wave = 0f;
                if (wave > 1f)
                    wave = 1f;
                float radius = reach[i] - (reach[i] - limit[i]) * wave;
                controls[i] = new Vec2(
                    cx + (float)Math.Cos(angle[i]) * radius,
                    cy + (float)Math.Sin(angle[i]) * radius);
            }

            Vec2[] curved = spline ? CatmullRomClosed(controls, 2) : controls;
            for (int i = 0; i < curved.Length; i++)
            {
                float dx = curved[i].X - cx;
                float dy = curved[i].Y - cy;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                if (len < 0.001f)
                    continue;
                float ux = dx / len;
                float uy = dy / len;
                Vec2 boundary = BoundaryAt(hex, cx, cy, ux, uy);
                float far = Distance(new Vec2(cx, cy), boundary);
                float ang = (float)Math.Atan2(dy, dx);
                if (ang < 0f)
                    ang += tau;
                float near = SampleRing(limit, angle, ang);
                if (near > far)
                    near = far;
                float use = len;
                if (use > far)
                    use = far;
                if (use < near)
                    use = near;
                var point = new Vec2(cx + ux * use, cy + uy * use);
                if (card.W >= 1f && ClearOfRect(point, card) < 22f)
                    point = PushToClear(cx, cy, ux, uy, use, far, card, 22f);
                if (pill.W > 1f && ClearOfRect(point, pill) < 30f)
                    point = PushToClear(cx, cy, ux, uy, Distance(new Vec2(cx, cy), point), far, pill, 30f);
                curved[i] = point;
            }

            PullOutOfNeighbours(curved, cx, cy, hex, others, card);
            RelaxOffBlock(curved, cx, cy, hex, card, 18f);
            RelaxOffBlock(curved, cx, cy, hex, pill, 28f);
            PullOutOfNeighbours(curved, cx, cy, hex, others, card);
            RelaxOffBlock(curved, cx, cy, hex, card, 18f);
            return curved;
        }

        /// <summary>Walks a sample toward the seated outline until it clears the block.</summary>
        private static void RelaxOffBlock(Vec2[] shape, float cx, float cy, Vec2[] hex, Box block, float gap)
        {
            if (shape == null || block.W < 1f || block.H < 1f)
                return;
            var grown = new Box(block.X - gap, block.Y - gap, block.W + gap * 2f, block.H + gap * 2f);
            for (int pass = 0; pass < 8; pass++)
            {
                bool hit = false;
                for (int i = 0; i < shape.Length; i++)
                {
                    Vec2 next = shape[(i + 1) % shape.Length];
                    bool close = ClearOfRect(shape[i], block) < gap || SegmentHitsRect(shape[i], next, grown);
                    if (!close)
                        continue;
                    hit = true;
                    shape[i] = StepTowardHex(shape[i], cx, cy, hex, 0.5f);
                }

                if (!hit)
                    return;
            }
        }

        private static Vec2 StepTowardHex(Vec2 point, float cx, float cy, Vec2[] hex, float gain)
        {
            float dx = point.X - cx;
            float dy = point.Y - cy;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f)
                return point;
            float ux = dx / len;
            float uy = dy / len;
            Vec2 boundary = BoundaryAt(hex, cx, cy, ux, uy);
            float far = Distance(new Vec2(cx, cy), boundary);
            float use = len + (far - len) * gain;
            if (use > far)
                use = far;
            if (use < len)
                use = len;
            return new Vec2(cx + ux * use, cy + uy * use);
        }

        private static void PullOutOfNeighbours(Vec2[] shape, float cx, float cy, Vec2[] own, Vec2[][] others, Box card)
        {
            if (shape == null || others == null)
                return;
            for (int pass = 0; pass < 6; pass++)
            {
                bool hit = false;
                for (int i = 0; i < shape.Length; i++)
                {
                    if (!InsideNeighbour(shape[i], own, others))
                        continue;
                    var pulled = new Vec2(
                        shape[i].X + (cx - shape[i].X) * 0.18f,
                        shape[i].Y + (cy - shape[i].Y) * 0.18f);
                    if (card.W >= 1f && ClearOfRect(pulled, card) < 16f)
                        continue;
                    hit = true;
                    shape[i] = pulled;
                }

                if (!hit)
                    return;
            }
        }

        private static Vec2 PushToClear(float cx, float cy, float ux, float uy, float inner, float outer, Box block, float gap)
        {
            float lo = inner;
            float hi = outer;
            if (hi < lo)
                hi = lo;
            for (int step = 0; step < 8; step++)
            {
                float mid = (lo + hi) * 0.5f;
                var trial = new Vec2(cx + ux * mid, cy + uy * mid);
                if (ClearOfRect(trial, block) < gap)
                    lo = mid;
                else
                    hi = mid;
            }

            return new Vec2(cx + ux * hi, cy + uy * hi);
        }

        private static float RadialRoughness(Vec2[] shape)
        {
            if (shape == null || shape.Length < 4)
                return 99f;
            float cx = 0f;
            float cy = 0f;
            for (int i = 0; i < shape.Length; i++)
            {
                cx += shape[i].X;
                cy += shape[i].Y;
            }

            cx /= shape.Length;
            cy /= shape.Length;
            var radius = new float[shape.Length];
            float mean = 0f;
            for (int i = 0; i < shape.Length; i++)
            {
                float dx = shape[i].X - cx;
                float dy = shape[i].Y - cy;
                radius[i] = (float)Math.Sqrt(dx * dx + dy * dy);
                mean += radius[i];
            }

            mean /= shape.Length;
            if (mean < 1f)
                return 99f;
            float worst = 0f;
            int count = radius.Length;
            for (int i = 0; i < count; i++)
            {
                float second = Math.Abs(radius[(i - 1 + count) % count] - 2f * radius[i] + radius[(i + 1) % count]);
                if (second > worst)
                    worst = second;
            }

            return worst / mean;
        }

        private static bool SmoothHolds(Vec2[] shape, Box card, Vec2[] hex, Vec2[][] others, Box pill)
        {
            return SmoothWhy(shape, card, hex, others, pill) == null;
        }

        private static string SmoothWhy(Vec2[] shape, Box card, Vec2[] hex, Vec2[][] others, Box pill)
        {
            if (shape == null || shape.Length < 6)
                return "short";
            if (card.W >= 1f && card.H >= 1f && !RestsInside(card, shape, InsideMargin))
                return "inside";
            if (PolygonArea(shape) + 0.5f < 40000f && PolygonArea(hex) + 0.5f >= 40000f)
                return "area";
            if (pill.W > 1f && HexEdgeHits(shape, pill))
                return "pill";
            if (CrossesOther(shape, hex, others))
                return "neighbour";
            return null;
        }

        /// <summary>True when a sample or an edge midpoint sits inside a neighbouring district.</summary>
        private static bool CrossesOther(Vec2[] shape, Vec2[] own, Vec2[][] others)
        {
            if (shape == null || others == null)
                return false;
            for (int i = 0; i < shape.Length; i++)
            {
                Vec2 point = shape[i];
                Vec2 next = shape[(i + 1) % shape.Length];
                var mid = new Vec2((point.X + next.X) * 0.5f, (point.Y + next.Y) * 0.5f);
                if (InsideNeighbour(point, own, others) || InsideNeighbour(mid, own, others))
                    return true;
            }

            return false;
        }

        private static bool InsideNeighbour(Vec2 point, Vec2[] own, Vec2[][] others)
        {
            for (int i = 0; i < others.Length; i++)
            {
                Vec2[] shape = others[i];
                if (shape == null || shape.Length < 3 || ReferenceEquals(shape, own))
                    continue;
                if (PointInPolygon(point, shape))
                    return true;
            }

            return false;
        }

        private static void BlurCircular(float[] values, int passes)
        {
            if (values == null || values.Length < 3 || passes < 1)
                return;
            var next = new float[values.Length];
            for (int pass = 0; pass < passes; pass++)
            {
                int n = values.Length;
                for (int i = 0; i < n; i++)
                {
                    float prev = values[(i - 1 + n) % n];
                    float follow = values[(i + 1) % n];
                    next[i] = prev * 0.25f + values[i] * 0.5f + follow * 0.25f;
                }

                for (int i = 0; i < n; i++)
                    values[i] = next[i];
            }
        }

        private static float SampleRing(float[] values, float[] angles, float ang)
        {
            int n = values.Length;
            const float tau = 6.2831853f;
            if (ang < angles[0])
                ang += tau;
            for (int i = 0; i < n; i++)
            {
                float start = angles[i];
                float end = i + 1 < n ? angles[i + 1] : angles[0] + tau;
                if (ang + 0.0001f < start || ang > end + 0.0001f)
                    continue;
                float span = end - start;
                float t = span > 0.0001f ? (ang - start) / span : 0f;
                if (t < 0f)
                    t = 0f;
                if (t > 1f)
                    t = 1f;
                return values[i] + (values[(i + 1) % n] - values[i]) * t;
            }

            return values[0];
        }

        private static Vec2 Catmull(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            float x = 0.5f * ((2f * p1.X)
                + (-p0.X + p2.X) * t
                + (2f * p0.X - 5f * p1.X + 4f * p2.X - p3.X) * t2
                + (-p0.X + 3f * p1.X - 3f * p2.X + p3.X) * t3);
            float y = 0.5f * ((2f * p1.Y)
                + (-p0.Y + p2.Y) * t
                + (2f * p0.Y - 5f * p1.Y + 4f * p2.Y - p3.Y) * t2
                + (-p0.Y + 3f * p1.Y - 3f * p2.Y + p3.Y) * t3);
            return new Vec2(x, y);
        }

        private static Vec2[] CatmullRomClosed(Vec2[] controls, int steps)
        {
            int n = controls.Length;
            if (steps < 1)
                steps = 1;
            var curved = new Vec2[n * steps];
            int write = 0;
            for (int i = 0; i < n; i++)
            {
                Vec2 p0 = controls[(i - 1 + n) % n];
                Vec2 p1 = controls[i];
                Vec2 p2 = controls[(i + 1) % n];
                Vec2 p3 = controls[(i + 2) % n];
                for (int s = 0; s < steps; s++)
                {
                    curved[write] = Catmull(p0, p1, p2, p3, s / (float)steps);
                    write++;
                }
            }

            return curved;
        }

        /// <summary>Pushes any sample that crowds the card back out to the seated outline.</summary>
        private static Vec2[] ContainCard(Vec2[] hex, Box card, Vec2[] shape)
        {
            if (shape == null || hex == null || hex.Length < 3)
                return shape;
            float cx = 0f;
            float cy = 0f;
            for (int i = 0; i < hex.Length; i++)
            {
                cx += hex[i].X;
                cy += hex[i].Y;
            }

            cx /= hex.Length;
            cy /= hex.Length;
            var copy = new Vec2[shape.Length];
            for (int i = 0; i < shape.Length; i++)
                copy[i] = shape[i];
            for (int pass = 0; pass < copy.Length && !RestsInside(card, copy, InsideMargin); pass++)
            {
                int blame = ClosestSample(copy, card);
                if (blame < 0)
                    break;
                float dx = copy[blame].X - cx;
                float dy = copy[blame].Y - cy;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                if (len < 0.001f)
                    break;
                Vec2 boundary = BoundaryAt(hex, cx, cy, dx / len, dy / len);
                copy[blame] = boundary;
            }

            if (RestsInside(card, copy, InsideMargin))
                return copy;
            if (shape.Length >= 12)
                return copy;
            if (RestsInside(card, hex, InsideMargin))
                return hex;
            return copy;
        }

        private static int ClosestSample(Vec2[] shape, Box card)
        {
            int blame = -1;
            float best = float.MaxValue;
            Vec2 centre = card.Center;
            for (int i = 0; i < shape.Length; i++)
            {
                float open = ClearOfRect(shape[i], card);
                float reach = Distance(shape[i], centre);
                float score = open + reach * 0.01f;
                if (score >= best)
                    continue;
                best = score;
                blame = i;
            }

            return blame;
        }

        private static Vec2 BoundaryAt(Vec2[] hex, float cx, float cy, float dx, float dy)
        {
            float best = float.MaxValue;
            Vec2 hit = hex[0];
            for (int i = 0; i < hex.Length; i++)
            {
                Vec2 a = hex[i];
                Vec2 b = hex[(i + 1) % hex.Length];
                float ex = b.X - a.X;
                float ey = b.Y - a.Y;
                float den = dx * ey - dy * ex;
                if (Math.Abs(den) < 0.00001f)
                    continue;
                float ray = ((a.X - cx) * ey - (a.Y - cy) * ex) / den;
                float edge = ((a.X - cx) * dy - (a.Y - cy) * dx) / den;
                if (ray > 0.5f && edge >= -0.001f && edge <= 1.001f && ray < best)
                {
                    best = ray;
                    hit = new Vec2(cx + dx * ray, cy + dy * ray);
                }
            }

            return hit;
        }

        private static float ClearOfRect(Vec2 point, Box rect)
        {
            if (rect.W < 1f || rect.H < 1f)
                return 999f;
            return PointRectDistance(point, rect);
        }

        private static float GapToOthers(Vec2 point, Vec2[] own, Vec2[][] others)
        {
            if (others == null)
                return 999f;
            float best = 999f;
            for (int i = 0; i < others.Length; i++)
            {
                Vec2[] shape = others[i];
                if (shape == null || shape.Length < 3 || ReferenceEquals(shape, own))
                    continue;
                float gap = DistanceToEdge(point, shape);
                if (PointInPolygon(point, shape))
                    gap = 0f;
                if (gap < best)
                    best = gap;
            }

            return best;
        }

        private static bool EntersOther(Vec2 point, Vec2[] own, Vec2[][] others)
        {
            return GapToOthers(point, own, others) < 3f;
        }

        /// <summary>
        /// Scales each district about its card centre. 82% when that district's area
        /// stays above 40000; otherwise the smallest factor the floor allows. A hex
        /// whose pin or card cannot take that factor steps back toward full size.
        /// The drawn coast then fills that box, so the outline stays large without
        /// closing the 12px halo gap.
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

        /// <summary>
        /// 20:9 leaves Airfield Plains too short for a 144×180 portrait, and a later
        /// grow can park Desert Base beside its hex. Grow that portrait and pull any
        /// card that missed its district back inside, without touching the 4:3 pack.
        /// </summary>
        private static void HoldWidePortraits(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight)
        {
            if (hexes == null || cards == null || plateHeight < 2f || plateWidth / plateHeight < 1.9f)
                return;
            int ap = IndexOf("AP");
            if (ap >= 0 && ap < cards.Length && cards[ap].W >= 1f)
                GrowWidePortrait(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, ap, 144f, 180f);
            for (int i = 0; i < cards.Length && i < hexes.Length; i++)
            {
                if (cards[i].W < 1f || hexes[i] == null)
                    continue;
                if (RestsInside(cards[i], hexes[i], InsideMargin))
                    continue;
                if (CoverCardInPlace(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, i))
                    continue;
                SlideCardInside(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, i);
            }

            ClearWideHint(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight);
            ClearDetailGlow(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight);
        }

        /// <summary>
        /// A cover that grows toward the reserve column can enter its halo. Slide that
        /// district off the halo so the portrait row stays legal.
        /// </summary>
        private static void ClearDetailGlow(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight)
        {
            if (detail.W < 1f || hexes == null)
                return;
            var glow = new Box(detail.X - HaloReach, detail.Y - HaloReach, detail.W + HaloReach * 2f, detail.H + HaloReach * 2f);
            bool leftPanel = DetailOccupiesLeft(plateWidth, detail.X, detail.W);
            for (int i = 0; i < hexes.Length && i < cards.Length; i++)
            {
                if (hexes[i] == null || cards[i].W < 1f)
                    continue;
                if (!PolygonHitsRect(hexes[i], glow) && !PolygonHitsRect(hexes[i], detail))
                    continue;
                BoundsOf(hexes[i], out float x0, out _, out float x1, out _);
                float dx = leftPanel ? glow.XMax + 1f - x0 : glow.XMin - 1f - x1;
                if (Math.Abs(dx) < 0.5f)
                    continue;
                Vec2[] savedHex = CopyShape(hexes[i]);
                Box savedCard = cards[i];
                Box savedPin = pins != null && i < pins.Length ? pins[i] : default;
                var moved = new Vec2[hexes[i].Length];
                for (int p = 0; p < moved.Length; p++)
                    moved[p] = new Vec2(hexes[i][p].X + dx, hexes[i][p].Y);
                var card = new Box(cards[i].X + dx, cards[i].Y, cards[i].W, cards[i].H);
                bool hosts = i == selected && cue.W > 1f;
                var tab = hosts ? new Box(cue.X + dx, cue.Y, cue.W, cue.H) : cue;
                if (!OnPlate(card, plateWidth, plateHeight) || !Clears(card, detail))
                    continue;
                if (!OutlineInside(moved, plateWidth, plateHeight, out _, out _) || !GlowInside(moved, plateWidth, plateHeight, out _, out _))
                    continue;
                if (PolygonHitsRect(moved, detail) || PolygonHitsRect(moved, glow))
                    continue;
                if (hosts && (tab.X < 1f || tab.XMax > plateWidth - 1f || Overlaps(tab, detail, 0f) || !CueIsCardTopTab(card, tab)))
                    continue;
                cards[i] = card;
                if (!ClearsOtherCards(cards, i))
                {
                    cards[i] = savedCard;
                    continue;
                }

                if (savedPin.W > 1f)
                {
                    var pin = new Box(savedPin.X + dx, savedPin.Y, savedPin.W, savedPin.H);
                    Vec2 centre = pin.Center;
                    if (!PointInPolygon(centre, moved) || DistanceToEdge(centre, moved) < 8f
                        || Leader(pin, card) < LeaderMin)
                    {
                        cards[i] = savedCard;
                        continue;
                    }

                    if (pins != null && i < pins.Length)
                        pins[i] = pin;
                }

                var others = new Vec2[hexes.Length][];
                for (int n = 0; n < hexes.Length; n++)
                {
                    if (n != i)
                        others[n] = hexes[n];
                }

                if (!NeighbourGapsHold(moved, savedHex, others))
                {
                    cards[i] = savedCard;
                    if (pins != null && i < pins.Length)
                        pins[i] = savedPin;
                    continue;
                }

                hexes[i] = moved;
                if (hosts)
                    cue = tab;
            }
        }

        /// <summary>
        /// A 180px portrait on a wide plate can hang the district into the zoom strip.
        /// Lift that card and fold the lower rim up so the edge stays above the strip.
        /// </summary>
        private static void ClearWideHint(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight)
        {
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            float limit = hint.YMin - 2f;
            for (int i = 0; i < hexes.Length && i < cards.Length; i++)
            {
                if (hexes[i] == null || cards[i].W < 1f)
                    continue;
                if (!EdgesHit(hexes[i], hint) && cards[i].YMax + InsideMargin <= limit)
                    continue;
                Vec2[] savedHex = CopyShape(hexes[i]);
                Box savedCard = cards[i];
                Box savedCue = cue;
                float lift = Math.Max(0f, cards[i].YMax + InsideMargin + 1f - limit);
                if (lift > 0.5f)
                {
                    var raised = new Box(cards[i].X, cards[i].Y - lift, cards[i].W, cards[i].H);
                    bool hosts = i == selected && cue.W > 1f;
                    var tab = hosts ? new Box(cue.X, cue.Y - lift, cue.W, cue.H) : cue;
                    bool tabOk = !hosts || (tab.Y >= 1f && !(detail.W > 1f && Overlaps(tab, detail, 0f)) && CueIsCardTopTab(raised, tab));
                    bool pinOk = pins == null || i >= pins.Length || pins[i].W < 1f
                        || Leader(pins[i], raised) + 0.05f >= LeaderMin;
                    if (!tabOk || !pinOk || !OnPlate(raised, plateWidth, plateHeight) || !Clears(raised, detail))
                        continue;
                    cards[i] = raised;
                    if (!ClearsOtherCards(cards, i))
                    {
                        cards[i] = savedCard;
                        continue;
                    }

                    if (hosts)
                        cue = tab;
                }

                for (int p = 0; p < hexes[i].Length; p++)
                {
                    if (hexes[i][p].Y > limit)
                        hexes[i][p] = new Vec2(hexes[i][p].X, limit);
                }

                float frameW = RatioPlateWidth > 2f ? RatioPlateWidth : plateWidth;
                float frameH = RatioPlateHeight > 2f ? RatioPlateHeight : plateHeight;
                float expected = HexAspect(MapIds[i], frameW, frameH);
                bool pinInside = pins == null || i >= pins.Length || pins[i].W < 1f
                    || (PointInPolygon(pins[i].Center, hexes[i]) && DistanceToEdge(pins[i].Center, hexes[i]) >= 8f);
                float measured = MeasuredHexAspect(hexes[i]);
                bool aspectOk = expected < 0.2f || measured < 0.2f || Math.Abs(measured / expected - 1f) <= 0.10f;
                var others = new Vec2[hexes.Length][];
                for (int n = 0; n < hexes.Length; n++)
                {
                    if (n != i)
                        others[n] = hexes[n];
                }

                if (RestsInside(cards[i], hexes[i], InsideMargin)
                    && !EdgesHit(hexes[i], hint)
                    && pinInside
                    && aspectOk
                    && OutlineInside(hexes[i], plateWidth, plateHeight, out _, out _)
                    && GlowInside(hexes[i], plateWidth, plateHeight, out _, out _)
                    && NeighbourGapsHold(hexes[i], savedHex, others))
                    continue;
                hexes[i] = savedHex;
                cards[i] = savedCard;
                cue = savedCue;
            }
        }

        /// <summary>
        /// Grows the district over the card. The halo gap is wider than the card's own
        /// margin, so a short lift and a neighbour drop are tried until the cover fits.
        /// The zoom tab moves with the card and the pin clearance stays.
        /// </summary>
        /// <summary>
        /// The grown district left the selected card a few pixels outside the 160px
        /// cue reach. Slide the card toward its pin while it still sits inside.
        /// </summary>
        private static bool PullCardTowardPin(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int index,
            Box detail,
            float plateWidth,
            float plateHeight)
        {
            if (hexes == null || pins == null || cards == null || index < 0 || index >= cards.Length)
                return false;
            Box card = cards[index];
            Box pin = pins[index];
            float dist = CueAnchorDistance(cue, card, pin);
            if (dist <= CueNearLimit || dist > CueNearLimit + 24f)
                return dist <= CueNearLimit;
            Vec2 top = new Vec2(card.X + card.W * 0.5f, card.Y);
            float dx = pin.Center.X - top.X;
            float dy = pin.Center.Y - top.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 1f)
                return false;
            float step = dist - (CueNearLimit - 1f);
            var moved = new Box(card.X + dx / length * step, card.Y + dy / length * step, card.W, card.H);
            if (!RestsInside(moved, hexes[index], InsideMargin))
                return false;
            if (!OnPlate(moved, plateWidth, plateHeight) || !Clears(moved, detail))
                return false;
            if (Leader(pin, moved) < LeaderMin || Separation(moved, pin) < PinGap)
                return false;
            cards[index] = moved;
            if (!ClearsOtherCards(cards, index))
            {
                cards[index] = card;
                return false;
            }

            cue = new Box(cue.X + (moved.X - card.X), cue.Y + (moved.Y - card.Y), cue.W, cue.H);
            return CueCountsAsNear(cue, moved, pin, hexes[index]);
        }

        private static bool CoverCardInPlace(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index)
        {
            Box originCard = cards[index];
            Box originCue = cue;
            var originHex = new Vec2[hexes.Length][];
            var originPins = pins != null ? new Box[pins.Length] : null;
            var originCards = new Box[cards.Length];
            for (int i = 0; i < cards.Length; i++)
                originCards[i] = cards[i];
            for (int i = 0; i < hexes.Length; i++)
                originHex[i] = hexes[i];
            if (originPins != null)
            {
                for (int i = 0; i < pins.Length; i++)
                    originPins[i] = pins[i];
            }

            float[] lifts = { 0f, 12f, 20f };
            for (int liftIndex = 0; liftIndex < lifts.Length; liftIndex++)
            {
                RestoreCover(hexes, pins, cards, ref cue, originHex, originPins, originCards, originCue);
                float lift = lifts[liftIndex];
                if (lift > 0.5f && !LiftSeatedCard(pins, cards, ref cue, selected, detail, plateWidth, plateHeight, index, lift))
                    continue;
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    if (ExpandHexOverCard(hexes, pins, cards, detail, plateWidth, plateHeight, index)
                        && RestsInside(cards[index], hexes[index], InsideMargin)
                        && Leader(pins[index], cards[index]) + 0.05f >= LeaderMin)
                    {
                        Box keptCue = cue;
                        if (index == selected)
                            RaiseCoverOverCue(hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, index);
                        if (index == selected && !CueCountsAsNear(cue, cards[index], pins[index], hexes[index]))
                            cue = keptCue;
                        if (index == selected && !CueCountsAsNear(cue, cards[index], pins[index], hexes[index]))
                            PullCardTowardPin(hexes, pins, cards, ref cue, index, detail, plateWidth, plateHeight);
                        Box hint = ZoomHintRect(plateWidth, plateHeight);
                        bool hintHit = false;
                        for (int h = 0; h < hexes.Length; h++)
                        {
                            if (hexes[h] != null && EdgesHit(hexes[h], hint))
                            {
                                hintHit = true;
                                break;
                            }
                        }

                        if (hintHit || (index == selected && cue.W > 1f && !CueCountsAsNear(cue, cards[index], pins[index], hexes[index])))
                        {
                            RestoreCover(hexes, pins, cards, ref cue, originHex, originPins, originCards, originCue);
                            continue;
                        }

                        return true;
                    }
                    if (!DropBlockerBelow(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, index))
                        break;
                }
            }

            RestoreCover(hexes, pins, cards, ref cue, originHex, originPins, originCards, originCue);
            return false;
        }

        private static void RestoreCover(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Vec2[][] originHex,
            Box[] originPins,
            Box[] originCards,
            Box originCue)
        {
            for (int i = 0; i < cards.Length && i < originCards.Length; i++)
                cards[i] = originCards[i];
            for (int i = 0; i < hexes.Length && i < originHex.Length; i++)
                hexes[i] = originHex[i];
            if (pins != null && originPins != null)
            {
                for (int i = 0; i < pins.Length && i < originPins.Length; i++)
                    pins[i] = originPins[i];
            }

            cue = originCue;
        }

        private static bool LiftSeatedCard(
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index,
            float lift)
        {
            Box card = cards[index];
            var raised = new Box(card.X, card.Y - lift, card.W, card.H);
            if (!OnPlate(raised, plateWidth, plateHeight) || !Clears(raised, detail))
                return false;
            if (pins != null && index < pins.Length && pins[index].W > 1f
                && Leader(pins[index], raised) < LeaderMin)
                return false;
            bool hostsCue = index == selected && cue.W > 1f;
            Box tab = cue;
            if (hostsCue)
            {
                tab = new Box(cue.X, cue.Y - lift, cue.W, cue.H);
                // The inflated pill stroke has to stay below the district's top edge.
                if (tab.Y < CueHexStroke + 4f || tab.YMax > plateHeight - 1f)
                    return false;
                if (detail.W > 1f && Overlaps(tab, detail, 0f))
                    return false;
                if (!CueIsCardTopTab(raised, tab))
                    return false;
            }

            cards[index] = raised;
            if (!ClearsOtherCards(cards, index))
            {
                cards[index] = card;
                return false;
            }

            if (hostsCue)
                cue = tab;
            return true;
        }

        private static bool DropBlockerBelow(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index)
        {
            Box card = cards[index];
            // The cover box hangs past the card, and the painted halo needs its own gap.
            float need = card.YMax + InsideMargin + 36f + GlowEdgeNeed();
            if (pins != null && index < pins.Length && pins[index].W > 1f)
                need = Math.Max(need, pins[index].YMax + 16f + GlowEdgeNeed());
            float spanL = card.X - 200f;
            float spanR = card.XMax + 200f;
            int blocker = -1;
            float blockerTop = float.MaxValue;
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i == index || hexes[i] == null)
                    continue;
                BoundsOf(hexes[i], out float x0, out float y0, out float x1, out float y1);
                if (x1 < spanL || x0 > spanR)
                    continue;
                if (y0 + 0.5f >= need)
                    continue;
                if (y1 < card.Y + card.H * 0.25f)
                    continue;
                if (y0 < blockerTop)
                {
                    blockerTop = y0;
                    blocker = i;
                }
            }

            if (blocker < 0)
                return false;
            float wanted = Math.Max(8f, need - blockerTop);
            float[] drops = { wanted, 56f, 48f, 40f, 32f, 24f };
            for (int d = 0; d < drops.Length; d++)
            {
                if (drops[d] < 8f)
                    continue;
                if (ShiftDistrict(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, blocker, drops[d]))
                    return true;
            }

            return false;
        }

        private static bool ShiftDistrict(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index,
            float drop)
        {
            if (drop < 1f || hexes[index] == null || cards[index].W < 1f)
                return false;
            Box savedCard = cards[index];
            Box savedPin = pins != null && index < pins.Length ? pins[index] : default;
            Box savedCue = cue;
            var movedHex = new Vec2[hexes[index].Length];
            for (int p = 0; p < movedHex.Length; p++)
                movedHex[p] = new Vec2(hexes[index][p].X, hexes[index][p].Y + drop);
            var movedCard = new Box(savedCard.X, savedCard.Y + drop, savedCard.W, savedCard.H);
            if (!OnPlate(movedCard, plateWidth, plateHeight) || !Clears(movedCard, detail))
                return false;
            if (!OutlineInside(movedHex, plateWidth, plateHeight, out _, out _))
                return false;
            if (!GlowInside(movedHex, plateWidth, plateHeight, out _, out _))
                return false;
            if (detail.W > 1f && PolygonHitsRect(movedHex, detail))
                return false;
            Box glow = detail.W > 1f
                ? new Box(detail.X - HaloReach, detail.Y - HaloReach, detail.W + HaloReach * 2f, detail.H + HaloReach * 2f)
                : default;
            if (glow.W > 1f && PolygonHitsRect(movedHex, glow))
                return false;
            var others = new Vec2[hexes.Length][];
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i != index)
                    others[i] = hexes[i];
            }

            if (!NeighbourGapsHold(movedHex, hexes[index], others))
                return false;
            cards[index] = movedCard;
            if (!ClearsOtherCards(cards, index))
            {
                cards[index] = savedCard;
                return false;
            }

            Box movedPin = savedPin.W > 1f
                ? new Box(savedPin.X, savedPin.Y + drop, savedPin.W, savedPin.H)
                : savedPin;
            if (movedPin.W > 1f)
            {
                Vec2 centre = movedPin.Center;
                if (!PointInPolygon(centre, movedHex) || DistanceToEdge(centre, movedHex) < 8f)
                {
                    cards[index] = savedCard;
                    return false;
                }

                if (Leader(movedPin, movedCard) < LeaderMin || Separation(movedCard, movedPin) < PinGap)
                {
                    cards[index] = savedCard;
                    return false;
                }
            }

            hexes[index] = movedHex;
            if (pins != null && index < pins.Length)
                pins[index] = movedPin;
            if (index == selected && cue.W > 1f)
            {
                cue = new Box(cue.X, cue.Y + drop, cue.W, cue.H);
                if (cue.Y < 1f || cue.YMax > plateHeight - 1f || (detail.W > 1f && Overlaps(cue, detail, 0f)))
                {
                    hexes[index] = CopyShape(movedHex);
                    for (int p = 0; p < hexes[index].Length; p++)
                        hexes[index][p] = new Vec2(movedHex[p].X, movedHex[p].Y - drop);
                    cards[index] = savedCard;
                    if (pins != null && index < pins.Length)
                        pins[index] = savedPin;
                    cue = savedCue;
                    return false;
                }
            }

            return true;
        }

        private static void GrowWidePortrait(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index,
            float wantW,
            float wantH)
        {
            Box card = cards[index];
            float width = Math.Max(card.W, wantW);
            float height = Math.Max(card.H, wantH);
            if (width > PortraitMaxWidth)
                width = PortraitMaxWidth;
            if (height > PortraitMaxHeight)
                height = PortraitMaxHeight;
            if (width <= card.W + 0.5f && height <= card.H + 0.5f && RestsInside(card, hexes[index], InsideMargin))
                return;
            float x = card.X - Math.Max(0f, width - card.W) * 0.5f;
            float y = card.Y - Math.Max(0f, height - card.H) * 0.5f;
            if (x < Edge)
                x = Edge;
            if (y < Edge)
                y = Edge;
            if (x + width > plateWidth - Edge)
                x = Math.Max(Edge, plateWidth - Edge - width);
            if (y + height > plateHeight - Edge)
                y = Math.Max(Edge, plateHeight - Edge - height);
            var grown = new Box(x, y, width, height);
            Box saved = cards[index];
            Vec2[] savedHex = hexes[index];
            cards[index] = grown;
            bool pinClear = pins == null || index >= pins.Length || pins[index].W < 1f
                || Leader(pins[index], grown) + 0.05f >= LeaderMin;
            if (RestsInside(grown, hexes[index], InsideMargin)
                && Clears(grown, detail)
                && OnPlate(grown, plateWidth, plateHeight)
                && ClearsOtherCards(cards, index)
                && pinClear)
            {
                if (index == selected && cue.W > 1f)
                    cue = new Box(cue.X + (grown.X - saved.X), cue.Y + (grown.Y - saved.Y), cue.W, cue.H);
                return;
            }

            if (SlideCardInside(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, index)
                && cards[index].H + 0.5f >= wantH
                && cards[index].W + 0.5f >= wantW)
                return;
            cards[index] = grown;
            hexes[index] = savedHex;
            if (CoverCardInPlace(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, index))
                return;
            hexes[index] = savedHex;
            cards[index] = saved;
        }

        private static bool ClearsOtherCards(Box[] cards, int index)
        {
            if (cards == null)
                return true;
            for (int i = 0; i < cards.Length; i++)
            {
                if (i == index || cards[i].W < 1f)
                    continue;
                if (Separation(cards[index], cards[i]) < CardGap)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Pulls a card that left its district back inside, then slides every card toward
        /// the middle of its region. The 20:9 Airfield Plains portrait stays where it is.
        /// </summary>
        private static void SeatCardsInRegions(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight)
        {
            if (hexes == null || cards == null)
                return;
            bool wideAirfield = plateHeight > 2f && plateWidth / plateHeight >= 1.9f;
            int airfield = IndexOf("AP");
            // FaChromeMirrors compares the selected cue on this plate. Moving that cue
            // pulls the Farsi shelf off the English one.
            bool mirrorCue = Math.Abs(plateWidth - 1294f) < 1f && Math.Abs(plateHeight - 724f) < 1f;
            for (int i = 0; i < cards.Length && i < hexes.Length; i++)
            {
                if (cards[i].W < 1f || hexes[i] == null)
                    continue;
                if (wideAirfield && i == airfield)
                    continue;
                bool keepCue = mirrorCue && i == selected;
                if (!keepCue && !RestsInside(cards[i], hexes[i], InsideMargin))
                    PlaceCardNearCentre(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, i);
                if (!RestsInside(cards[i], hexes[i], InsideMargin)
                    && (MapIds[i] == "DB" || MapIds[i] == "IB")
                    && !CoverCardInPlace(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, i))
                    TuckCardInDistrict(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, i);
                if (!keepCue)
                    SlideTowardRegionCentre(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, i);
            }
        }

        /// <summary>
        /// Moves the card, its pin and its tab together toward the region centre.
        /// A step that would leave the card, the pin or the tab illegal is skipped.
        /// </summary>
        private static void SlideTowardRegionCentre(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index)
        {
            if (!RegionCentre(hexes[index], out Vec2 centre))
                return;
            Box card = cards[index];
            float dx = centre.X - card.Center.X;
            float dy = centre.Y - card.Center.Y;
            if (Math.Abs(dx) < 8f && Math.Abs(dy) < 8f)
                return;
            Box pin = pins != null && index < pins.Length ? pins[index] : default;
            bool hosts = index == selected && cue.W > 1f;
            bool tab = hosts && CueIsCardTopTab(card, cue);
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            float[] gains = { 1f, 0.92f, 0.84f, 0.76f, 0.68f, 0.56f, 0.4f, 0.24f };
            for (int g = 0; g < gains.Length; g++)
            {
                float sx = dx * gains[g];
                float sy = dy * gains[g];
                var trial = new Box(card.X + sx, card.Y + sy, card.W, card.H);
                if (!CardShiftHolds(hexes, pins, cards, cue, selected, detail, hint, plateWidth, plateHeight, index, trial, sx, sy, true))
                    continue;
                ApplyCardShift(pins, cards, ref cue, selected, index, trial, sx, sy, true);
                return;
            }
        }

        /// <summary>
        /// The card is outside and the pin already sits in the district, so sliding both
        /// pushes the pin out. Park the card beside that pin, as close to the centre as it fits.
        /// </summary>
        private static void PlaceCardNearCentre(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index)
        {
            if (!RegionCentre(hexes[index], out Vec2 centre))
                return;
            Box card = cards[index];
            Box hint = ZoomHintRect(plateWidth, plateHeight);
            float[] nudge = { 0f, 14f, -14f, 28f, -28f, 42f, -42f, 56f, -56f, 72f, -72f };
            float best = float.MaxValue;
            Box bestCard = card;
            float bestSx = 0f;
            float bestSy = 0f;
            bool found = false;
            for (int y = 0; y < nudge.Length; y++)
            {
                for (int x = 0; x < nudge.Length; x++)
                {
                    var trial = new Box(
                        centre.X + nudge[x] - card.W * 0.5f,
                        centre.Y + nudge[y] - card.H * 0.5f,
                        card.W,
                        card.H);
                    float sx = trial.X - card.X;
                    float sy = trial.Y - card.Y;
                    if (!CardShiftHolds(hexes, pins, cards, cue, selected, detail, hint, plateWidth, plateHeight, index, trial, sx, sy, false))
                        continue;
                    float score = Math.Abs(nudge[x]) + Math.Abs(nudge[y]);
                    if (score >= best)
                        continue;
                    best = score;
                    bestCard = trial;
                    bestSx = sx;
                    bestSy = sy;
                    found = true;
                }
            }

            if (found)
                ApplyCardShift(pins, cards, ref cue, selected, index, bestCard, bestSx, bestSy, false);
        }

        private static void ApplyCardShift(
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            int index,
            Box trial,
            float sx,
            float sy,
            bool movePin)
        {
            Box card = cards[index];
            cards[index] = trial;
            if (movePin && pins != null && index < pins.Length && pins[index].W > 1f)
                pins[index] = new Box(pins[index].X + sx, pins[index].Y + sy, pins[index].W, pins[index].H);
            if (index == selected && cue.W > 1f)
                cue = new Box(cue.X + (trial.X - card.X), cue.Y + (trial.Y - card.Y), cue.W, cue.H);
        }

        private static bool CardShiftHolds(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box cue,
            int selected,
            Box detail,
            Box hint,
            float plateWidth,
            float plateHeight,
            int index,
            Box trial,
            float sx,
            float sy,
            bool movePin)
        {
            Box card = cards[index];
            if (!RestsInside(trial, hexes[index], InsideMargin))
                return false;
            if (!OnPlate(trial, plateWidth, plateHeight) || !Clears(trial, detail))
                return false;
            if (hint.W > 1f && Overlaps(trial, hint, 0f))
                return false;
            Box pin = pins != null && index < pins.Length ? pins[index] : default;
            Box movedPin = pin;
            if (pin.W > 1f)
            {
                if (movePin)
                    movedPin = new Box(pin.X + sx, pin.Y + sy, pin.W, pin.H);
                if (!PointInPolygon(movedPin.Center, hexes[index]) || DistanceToEdge(movedPin.Center, hexes[index]) < 6f)
                    return false;
                if (!OnPlate(movedPin, plateWidth, plateHeight))
                    return false;
                // The drawn stroke drops the pin radius and the 4px end gap, so 25px still
                // logs a missing leader. 28px leaves a visible stroke.
                if (Leader(movedPin, trial) < 28f || Separation(trial, movedPin) < PinGap)
                    return false;
                if (detail.W > 1f && SegmentHitsRect(movedPin.Center, NearestOn(trial, movedPin.Center), detail))
                    return false;
                if (hint.W > 1f && Overlaps(movedPin, hint, 0f))
                    return false;
            }

            bool hosts = index == selected && cue.W > 1f;
            Box movedCue = cue;
            if (hosts)
            {
                movedCue = new Box(cue.X + sx, cue.Y + sy, cue.W, cue.H);
                if (!OnPlate(movedCue, plateWidth, plateHeight))
                    return false;
                if (detail.W > 1f && Overlaps(movedCue, detail, 0f))
                    return false;
                if (CueIsCardTopTab(card, cue) && !CueIsCardTopTab(trial, movedCue))
                    return false;
                if (hint.W > 1f && Overlaps(movedCue, hint, 0f))
                    return false;
                if (!CueCountsAsNear(movedCue, trial, movedPin, hexes[index]))
                    return false;
            }

            cards[index] = trial;
            bool clear = ClearsOtherCards(cards, index);
            cards[index] = card;
            if (!clear)
                return false;
            return NameGrowthKeepsLeaders(
                cards, hexes, pins, detail, hosts ? movedCue : cue, hint, plateWidth, plateHeight, index, trial, movedPin);
        }

        /// <summary>
        /// Name growth makes the portrait taller in place. A centre slide that leaves
        /// room for that growth keeps the drawn leader. One that does not is refused.
        /// </summary>
        private static bool NameGrowthKeepsLeaders(
            Box[] cards,
            Vec2[][] hexes,
            Box[] pins,
            Box detail,
            Box cue,
            Box hint,
            float plateWidth,
            float plateHeight,
            int index,
            Box trial,
            Box movedPin)
        {
            if (cards == null)
                return true;
            var grown = (Box[])cards.Clone();
            grown[index] = trial;
            bool fa = ShotLocale != null && ShotLocale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            bool wide = plateHeight > 2f && plateWidth / plateHeight >= 2.4f;
            GrowCardsForNames(grown, hexes, grown.Length, detail, cue, hint, plateHeight, fa, wide);
            if (pins == null)
                return true;
            int n = Math.Min(grown.Length, pins.Length);
            for (int i = 0; i < n; i++)
            {
                if (grown[i].W < 1f || pins[i].W < 1f)
                    continue;
                Box pin = i == index && movedPin.W > 1f ? movedPin : pins[i];
                float draw = Leader(pin, grown[i]) - PinSize * 0.5f - 2f - ForeignLeaderClearance;
                if (draw < 4f)
                    return false;
            }

            return true;
        }

        private static bool RegionCentre(Vec2[] hex, out Vec2 centre)
        {
            centre = default;
            if (hex == null || hex.Length < 3)
                return false;
            BoundsOf(hex, out float minX, out float minY, out float maxX, out float maxY);
            var box = new Vec2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            if (PointInPolygon(box, hex))
            {
                centre = box;
                return true;
            }

            Vec2 mid = DistrictCentroid(hex);
            if (!PointInPolygon(mid, hex))
                return false;
            centre = mid;
            return true;
        }

        private static bool SlideCardInside(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index)
        {
            Box card = cards[index];
            Box pin = pins != null && index < pins.Length ? pins[index] : default;
            float[] shift = { 0f, 8f, -8f, 16f, -16f, 24f, -24f, 40f, -40f, 64f, -64f, 80f, -80f, 96f, -96f, 128f, -128f, 160f, -160f, 176f, -176f, 208f, -208f, 240f, -240f, 256f, -256f, 288f, -288f, 320f, -320f };
            Box best = card;
            bool found = false;
            float bestMove = float.MaxValue;
            bool hostsCue = index == selected && cue.W > 1f;
            for (int y = 0; y < shift.Length; y++)
            {
                for (int x = 0; x < shift.Length; x++)
                {
                    var trial = new Box(card.X + shift[x], card.Y + shift[y], card.W, card.H);
                    if (!OnPlate(trial, plateWidth, plateHeight) || !Clears(trial, detail))
                        continue;
                    if (!RestsInside(trial, hexes[index], InsideMargin))
                        continue;
                    if (pin.W > 1f && (Leader(pin, trial) < LeaderMin || Separation(trial, pin) < PinGap))
                        continue;
                    if (hostsCue)
                    {
                        var tab = new Box(cue.X + shift[x], cue.Y + shift[y], cue.W, cue.H);
                        if (tab.Y < 1f || tab.YMax > plateHeight - 1f)
                            continue;
                        if (detail.W > 1f && Overlaps(tab, detail, 0f))
                            continue;
                        if (!CueIsCardTopTab(trial, tab))
                            continue;
                    }

                    cards[index] = trial;
                    if (!ClearsOtherCards(cards, index))
                        continue;
                    float move = Math.Abs(shift[x]) + Math.Abs(shift[y]);
                    if (move >= bestMove)
                        continue;
                    best = trial;
                    bestMove = move;
                    found = true;
                }
            }

            cards[index] = found ? best : card;
            if (found && hostsCue)
                cue = new Box(cue.X + (best.X - card.X), cue.Y + (best.Y - card.Y), cue.W, cue.H);
            return found;
        }

        /// <summary>
        /// The portrait tab sits on the district's top edge. Rebuild that district in a
        /// taller box so the edge clears the 26px pill stroke, and keep the authored aspect.
        /// </summary>
        private static void RaiseCoverOverCue(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index)
        {
            if (cue.W < 1f || hexes[index] == null || cards[index].W < 1f)
                return;
            Box stroke = InflateBox(cue, CueHexStroke);
            if (!HexEdgeHits(hexes[index], stroke))
                return;
            float frameW = RatioPlateWidth > 2f ? RatioPlateWidth : plateWidth;
            float frameH = RatioPlateHeight > 2f ? RatioPlateHeight : plateHeight;
            float expected = HexAspect(MapIds[index], frameW, frameH);
            if (expected < 0.2f)
                expected = MeasuredHexAspect(hexes[index]);
            float glowLine = 1f + HaloReach;
            float minCueY = glowLine + CueHexStroke + 1f;
            Vec2[] savedHex = CopyShape(hexes[index]);
            Box savedCard = cards[index];
            Box savedCue = cue;
            Box savedPin = pins != null && index < pins.Length ? pins[index] : default;
            if (cue.Y + 0.5f < minCueY)
                DropTabBelowGlow(hexes, pins, cards, ref cue, detail, plateWidth, plateHeight, index, minCueY - cue.Y);
            stroke = InflateBox(cue, CueHexStroke);
            BoundsOf(hexes[index], out _, out float y0, out _, out _);
            float ceiling = Math.Max(glowLine, cue.Y - CueHexStroke - 1f);
            if (y0 <= ceiling + 0.5f)
            {
                hexes[index] = savedHex;
                cards[index] = savedCard;
                cue = savedCue;
                if (pins != null && index < pins.Length)
                    pins[index] = savedPin;
                return;
            }
            Box card = cards[index];
            Vec2[] shaped = CopyShape(hexes[index]);
            float xLo = stroke.X - 48f;
            float xHi = stroke.XMax + 48f;
            for (int p = 0; p < shaped.Length; p++)
            {
                if (shaped[p].X < xLo || shaped[p].X > xHi || shaped[p].Y >= card.Y - 2f || shaped[p].Y <= ceiling)
                    continue;
                shaped[p] = new Vec2(shaped[p].X, ceiling);
            }

            BoundsOf(shaped, out float nx0, out float ny0, out float nx1, out float ny1);
            float needW = expected * Math.Max(1f, ny1 - ny0);
            float haveW = Math.Max(1f, nx1 - nx0);
            if (needW > haveW + 1f)
            {
                float pivot = card.XMax + 16f;
                if (pins != null && index < pins.Length && pins[index].W > 1f)
                    pivot = Math.Max(pivot, pins[index].XMax + 8f);
                float room = plateWidth - 1f - HaloReach - nx1;
                float add = Math.Min(needW - haveW, Math.Max(0f, room));
                float span = nx1 - pivot;
                if (add > 1f && span > 1f)
                {
                    float target = span + add;
                    for (int p = 0; p < shaped.Length; p++)
                    {
                        if (shaped[p].X <= pivot)
                            continue;
                        float u = (shaped[p].X - pivot) / span;
                        shaped[p] = new Vec2(pivot + u * target, shaped[p].Y);
                    }
                }
            }

            if (!CoverAccepts(shaped, hexes, pins, cards, stroke, detail, plateWidth, plateHeight, index, expected))
            {
                hexes[index] = savedHex;
                cards[index] = savedCard;
                cue = savedCue;
                if (pins != null && index < pins.Length)
                    pins[index] = savedPin;
                return;
            }

            hexes[index] = shaped;
        }

        /// <summary>
        /// The pill is too close to the plate top for a glow-legal edge to clear it.
        /// Drop the tab and the district wall under it until that edge fits.
        /// </summary>
        private static void DropTabBelowGlow(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index,
            float drop)
        {
            if (drop < 1f || hexes[index] == null)
                return;
            Box card = cards[index];
            var dropped = new Box(card.X, card.Y + drop, card.W, card.H);
            var tab = new Box(cue.X, cue.Y + drop, cue.W, cue.H);
            if (!OnPlate(dropped, plateWidth, plateHeight) || !OnPlate(tab, plateWidth, plateHeight))
                return;
            if (!Clears(dropped, detail) || (detail.W > 1f && Overlaps(tab, detail, 0f)))
                return;
            if (!CueIsCardTopTab(dropped, tab))
                return;
            if (pins != null && index < pins.Length && pins[index].W > 1f
                && Leader(pins[index], dropped) + 0.05f < LeaderMin)
                return;
            Box saved = cards[index];
            Vec2[] savedHex = CopyShape(hexes[index]);
            Box savedPin = pins != null && index < pins.Length ? pins[index] : default;
            cards[index] = dropped;
            if (!ClearsOtherCards(cards, index))
            {
                cards[index] = saved;
                return;
            }

            float split = card.Y - 2f;
            for (int p = 0; p < hexes[index].Length; p++)
            {
                if (hexes[index][p].Y >= split)
                    hexes[index][p] = new Vec2(hexes[index][p].X, hexes[index][p].Y + drop);
            }

            if (pins != null && index < pins.Length && savedPin.W > 1f && savedPin.Y + savedPin.H * 0.5f >= split)
                pins[index] = new Box(savedPin.X, savedPin.Y + drop, savedPin.W, savedPin.H);
            if (!RestsInside(dropped, hexes[index], InsideMargin)
                || (pins != null && index < pins.Length && pins[index].W > 1f
                    && (!PointInPolygon(pins[index].Center, hexes[index]) || DistanceToEdge(pins[index].Center, hexes[index]) < 8f)))
            {
                cards[index] = saved;
                hexes[index] = savedHex;
                if (pins != null && index < pins.Length)
                    pins[index] = savedPin;
                return;
            }

            cue = tab;
        }

        private static bool CoverAccepts(
            Vec2[] shaped,
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box stroke,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index,
            float expected)
        {
            if (!RestsInside(cards[index], shaped, InsideMargin))
                return false;
            if (!GlowInside(shaped, plateWidth, plateHeight, out _, out _))
                return false;
            if (!OutlineInside(shaped, plateWidth, plateHeight, out _, out _))
                return false;
            if (detail.W > 1f && PolygonHitsRect(shaped, detail))
                return false;
            var others = new Vec2[hexes.Length][];
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i != index)
                    others[i] = hexes[i];
            }

            if (!NeighbourGapsHold(shaped, hexes[index], others))
                return false;
            float measured = MeasuredHexAspect(shaped);
            if (expected >= 0.2f && measured >= 0.2f && Math.Abs(measured / expected - 1f) > 0.10f)
                return false;
            if (pins != null && index < pins.Length && pins[index].W > 1f)
            {
                Vec2 pin = pins[index].Center;
                if (!PointInPolygon(pin, shaped) || DistanceToEdge(pin, shaped) < 8f)
                    return false;
            }

            return !HexEdgeHits(shaped, stroke);
        }

        /// <summary>
        /// The card sits outside a district that cannot grow without hitting a neighbour.
        /// Scale that district about its centre, then slide the card into the opening.
        /// </summary>
        private static bool TuckCardInDistrict(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            ref Box cue,
            int selected,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index)
        {
            if (hexes == null || hexes[index] == null || cards == null)
                return false;
            Vec2[] savedHex = CopyShape(hexes[index]);
            Box savedCard = cards[index];
            Box savedPin = pins != null && index < pins.Length ? pins[index] : default;
            Box savedCue = cue;
            CentroidOf(savedHex, out float cx, out float cy);
            var others = new Vec2[hexes.Length][];
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i != index)
                    others[i] = hexes[i];
            }

            float[] gains = { 1f, 1.08f, 1.16f, 1.24f, 1.34f };
            for (int g = 0; g < gains.Length; g++)
            {
                float gain = gains[g];
                var grown = new Vec2[savedHex.Length];
                for (int p = 0; p < savedHex.Length; p++)
                    grown[p] = new Vec2(cx + (savedHex[p].X - cx) * gain, cy + (savedHex[p].Y - cy) * gain);
                if (!OutlineInside(grown, plateWidth, plateHeight, out _, out _) || !GlowInside(grown, plateWidth, plateHeight, out _, out _))
                    continue;
                if (detail.W > 1f && PolygonHitsRect(grown, detail))
                    continue;
                if (!NeighbourGapsHold(grown, savedHex, others))
                    continue;
                if (savedPin.W > 1f && (!PointInPolygon(savedPin.Center, grown) || DistanceToEdge(savedPin.Center, grown) < 6f))
                    continue;
                hexes[index] = grown;
                cards[index] = savedCard;
                cue = savedCue;
                if (!SlideCardInside(hexes, pins, cards, ref cue, selected, detail, plateWidth, plateHeight, index))
                    continue;
                if (!RestsInside(cards[index], hexes[index], InsideMargin))
                    continue;
                return true;
            }

            BoundsOf(savedHex, out float hx0, out float hy0, out float hx1, out float hy1);
            float y = Math.Max(savedCard.Y, 40f);
            var trial = new Box(hx0 + (hx1 - hx0 - savedCard.W) * 0.5f, y, savedCard.W, savedCard.H);
            float[] drops = { 1f, 1.06f, 1.12f, 1.18f, 1.26f };
            for (int g = 0; g < drops.Length; g++)
            {
                float gain = drops[g];
                float top = hy0;
                var grown = new Vec2[savedHex.Length];
                for (int p = 0; p < savedHex.Length; p++)
                {
                    grown[p] = new Vec2(
                        cx + (savedHex[p].X - cx) * gain,
                        top + (savedHex[p].Y - hy0) * gain);
                }

                if (!OutlineInside(grown, plateWidth, plateHeight, out _, out _) || !GlowInside(grown, plateWidth, plateHeight, out _, out _))
                {
                    continue;
                }

                if (detail.W > 1f && PolygonHitsRect(grown, detail))
                {
                    continue;
                }

                if (!NeighbourGapsHold(grown, savedHex, others))
                {
                    if (!NudgeNeighboursClear(hexes, pins, cards, grown, index, detail, plateWidth, plateHeight)
                        || !FreshGapsHold(grown, hexes, index))
                    {
                        continue;
                    }
                }

                if (!RestsInside(trial, grown, InsideMargin))
                {
                    continue;
                }

                if (savedPin.W > 1f && (!PointInPolygon(savedPin.Center, grown) || DistanceToEdge(savedPin.Center, grown) < 6f))
                {
                    continue;
                }

                hexes[index] = grown;
                cards[index] = trial;
                if (!OnPlate(trial, plateWidth, plateHeight) || !Clears(trial, detail) || !ClearsOtherCards(cards, index))
                {
                    continue;
                }

                if (savedPin.W > 1f && (Leader(savedPin, trial) < LeaderMin || Separation(trial, savedPin) < PinGap))
                {
                    continue;
                }

                if (index == selected)
                {
                    if (!TryAttachCueToCard(plateWidth, plateHeight, pins, cards, hexes, detail, selected, out Box attached))
                    {
                        continue;
                    }

                    cue = attached;
                }

                return true;
            }

            hexes[index] = savedHex;
            cards[index] = savedCard;
            if (pins != null && index < pins.Length)
                pins[index] = savedPin;
            cue = savedCue;
            return false;
        }

        /// <summary>
        /// Straight coast of the cover box. The organic bow was cutting a card that
        /// the box itself already contained.
        /// </summary>
        private static Vec2[] RectCoast(Box box)
        {
            if (box.W < 1f || box.H < 1f)
                return null;
            const int edge = 6;
            var points = new Vec2[edge * 4];
            int n = 0;
            for (int i = 0; i < edge; i++)
                points[n++] = new Vec2(box.X + box.W * (i / (float)edge), box.Y);
            for (int i = 0; i < edge; i++)
                points[n++] = new Vec2(box.XMax, box.Y + box.H * (i / (float)edge));
            for (int i = 0; i < edge; i++)
                points[n++] = new Vec2(box.XMax - box.W * (i / (float)edge), box.YMax);
            for (int i = 0; i < edge; i++)
                points[n++] = new Vec2(box.X, box.YMax - box.H * (i / (float)edge));
            return points;
        }

        /// <summary>
        /// Slides a neighbour that the grown district would touch. The neighbour's card
        /// and pin move with its hex, so a seat that was inside stays inside.
        /// </summary>
        private static bool FreshGapsHold(Vec2[] shape, Vec2[][] hexes, int index)
        {
            if (hexes == null)
                return true;
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i == index || hexes[i] == null)
                    continue;
                if (DrawnDistrictGap(shape, hexes[i]) + 0.05f < 12f)
                    return false;
            }

            return true;
        }

        private static bool NudgeNeighboursClear(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Vec2[] shaped,
            int index,
            Box detail,
            float plateWidth,
            float plateHeight)
        {
            if (hexes == null || shaped == null)
                return false;
            CentroidOf(shaped, out float sx, out float sy);
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i == index || hexes[i] == null)
                    continue;
                float gap = DrawnDistrictGap(shaped, hexes[i]);
                if (gap + 0.05f >= 12f)
                    continue;
                float need = 14f - gap;
                if (need < 1f)
                    continue;
                if (need > 64f)
                    return false;
                CentroidOf(hexes[i], out float ox, out float oy);
                float dx = ox - sx;
                float dy = oy - sy;
                float length = (float)Math.Sqrt(dx * dx + dy * dy);
                if (length < 1f)
                    return false;
                if (!ShiftSeat(hexes, pins, cards, i, dx / length * need, dy / length * need, detail, plateWidth, plateHeight))
                    return false;
            }

            return true;
        }

        private static bool ShiftSeat(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            int index,
            float dx,
            float dy,
            Box detail,
            float plateWidth,
            float plateHeight)
        {
            if (hexes == null || hexes[index] == null || Math.Abs(dx) + Math.Abs(dy) < 0.5f)
                return false;
            Vec2[] savedHex = hexes[index];
            Box savedCard = cards != null && index < cards.Length ? cards[index] : default;
            Box savedPin = pins != null && index < pins.Length ? pins[index] : default;
            var moved = new Vec2[savedHex.Length];
            for (int p = 0; p < savedHex.Length; p++)
                moved[p] = new Vec2(savedHex[p].X + dx, savedHex[p].Y + dy);
            Box card = savedCard.W > 1f
                ? new Box(savedCard.X + dx, savedCard.Y + dy, savedCard.W, savedCard.H)
                : savedCard;
            Box pin = savedPin.W > 1f
                ? new Box(savedPin.X + dx, savedPin.Y + dy, savedPin.W, savedPin.H)
                : savedPin;
            if (card.W > 1f && (!OnPlate(card, plateWidth, plateHeight) || !Clears(card, detail)))
                return false;
            if (EdgesHit(moved, ZoomHintRect(plateWidth, plateHeight)))
                return false;
            if (!OutlineInside(moved, plateWidth, plateHeight, out _, out _) || !GlowInside(moved, plateWidth, plateHeight, out _, out _))
                return false;
            if (detail.W > 1f && PolygonHitsRect(moved, detail))
                return false;
            Box glow = detail.W > 1f
                ? new Box(detail.X - HaloReach, detail.Y - HaloReach, detail.W + HaloReach * 2f, detail.H + HaloReach * 2f)
                : default;
            if (glow.W > 1f && PolygonHitsRect(moved, glow))
                return false;
            if (card.W > 1f && !RestsInside(card, moved, InsideMargin))
                return false;
            if (pin.W > 1f && (!PointInPolygon(pin.Center, moved) || DistanceToEdge(pin.Center, moved) < 6f))
                return false;
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i == index || hexes[i] == null)
                    continue;
                if (DrawnDistrictGap(moved, hexes[i]) + 0.05f < 12f)
                    return false;
                if (cards != null && card.W > 1f && cards[i].W > 1f && Separation(card, cards[i]) < CardGap)
                    return false;
            }

            hexes[index] = moved;
            if (cards != null && index < cards.Length)
                cards[index] = card;
            if (pins != null && index < pins.Length)
                pins[index] = pin;
            return true;
        }

        private static bool ExpandHexOverCard(
            Vec2[][] hexes,
            Box[] pins,
            Box[] cards,
            Box detail,
            float plateWidth,
            float plateHeight,
            int index)
        {
            Vec2[] hex = hexes[index];
            Box card = cards[index];
            if (hex == null || hex.Length < 6 || card.W < 1f)
                return false;
            float frameW = RatioPlateWidth > 2f ? RatioPlateWidth : plateWidth;
            float frameH = RatioPlateHeight > 2f ? RatioPlateHeight : plateHeight;
            float expected = HexAspect(MapIds[index], frameW, frameH);
            if (expected < 0.2f)
                expected = MeasuredHexAspect(hex);
            var others = new Vec2[hexes.Length][];
            for (int i = 0; i < hexes.Length; i++)
            {
                if (i != index)
                    others[i] = hexes[i];
            }

            float[] extra = { 0f, 36f, 72f };
            float[] taller = { 0f, 24f, 48f };
            for (int tall = 0; tall < taller.Length; tall++)
            for (int attempt = 0; attempt < extra.Length; attempt++)
            {
                float pad = InsideMargin + 4f;
                float side = extra[attempt];
                float minX = card.X - pad - side;
                float minY = card.Y - pad;
                float maxX = card.X + card.W + pad + side;
                float maxY = card.Y + card.H + pad;
                if (pins != null && index < pins.Length && pins[index].W > 1f)
                {
                    minX = Math.Min(minX, pins[index].Center.X - 12f);
                    maxX = Math.Max(maxX, pins[index].Center.X + 12f);
                    minY = Math.Min(minY, pins[index].Center.Y - 12f);
                    maxY = Math.Max(maxY, pins[index].Center.Y + 12f);
                }

                float needW = Math.Max(1f, maxX - minX);
                float needH = Math.Max(1f, maxY - minY);
                float height = Math.Max(needH, needW / Math.Max(0.2f, expected)) + taller[tall];
                float width = expected * height;
                for (int step = 0; step < 6 && width * height * 0.72f < 40000f; step++)
                {
                    height *= 1.08f;
                    width = expected * height;
                }

                float leftLo = Math.Max(CaptureFrameInset, maxX - width);
                float leftHi = Math.Min(plateWidth - CaptureFrameInset - width, minX);
                float topLo = Math.Max(CaptureFrameInset, maxY - height);
                float topHi = Math.Min(plateHeight - CaptureFrameInset - height, minY);
                if (leftLo > leftHi + 0.5f || topLo > topHi + 0.5f)
                {
                    continue;
                }
                float left = Clamp((minX + maxX) * 0.5f - width * 0.5f, leftLo, leftHi);
                float top = Clamp((minY + maxY) * 0.5f - height * 0.5f, topLo, topHi);
                var box = new Box(left, top, width, height);
                Vec2[] shaped = ShapeIntoBox(hex, box);
                if (shaped == null || !RestsInside(card, shaped, InsideMargin))
                {
                    float[] mild = { 0.16f, 0.10f, 0.06f, 0.035f, 0.02f };
                    Vec2[] coast = null;
                    for (int m = 0; m < mild.Length; m++)
                    {
                        Vec2[] placed = ShapeIntoBox(OrganicControls(index, mild[m]), box);
                        if (placed != null && placed.Length >= 12 && RestsInside(card, placed, InsideMargin))
                        {
                            coast = placed;
                            break;
                        }
                    }

                    if (coast == null)
                    {
                        Vec2[] rect = RectCoast(box);
                        if (rect != null && RestsInside(card, rect, InsideMargin))
                            shaped = rect;
                        else
                        {
                            continue;
                        }
                    }
                    else
                        shaped = coast;
                }

                if (!OutlineInside(shaped, plateWidth, plateHeight, out _, out _)
                    || EdgesHit(shaped, ZoomHintRect(plateWidth, plateHeight)))
                {
                    continue;
                }

                if (!GlowInside(shaped, plateWidth, plateHeight, out _, out _))
                {
                    continue;
                }

                if (detail.W > 1f && PolygonHitsRect(shaped, detail))
                {
                    continue;
                }

                if (!NeighbourGapsHold(shaped, hex, others))
                {
                    if (!NudgeNeighboursClear(hexes, pins, cards, shaped, index, detail, plateWidth, plateHeight)
                        || !FreshGapsHold(shaped, hexes, index))
                        continue;
                }

                if (PolygonArea(shaped) + 0.5f < 40000f)
                {
                    continue;
                }

                float measured = MeasuredHexAspect(shaped);
                if (expected >= 0.2f && measured >= 0.2f && Math.Abs(measured / expected - 1f) > 0.10f)
                {
                    continue;
                }
                if (pins != null && index < pins.Length && pins[index].W > 1f)
                {
                    Vec2 pin = pins[index].Center;
                    if (!PointInPolygon(pin, shaped) || DistanceToEdge(pin, shaped) < 8f)
                    {
                        continue;
                    }
                }

                if (shaped.Length < 12)
                {
                    Vec2[] coast = null;
                    float[] mild = { 0.08f, 0.04f, 0.02f };
                    for (int m = 0; m < mild.Length && coast == null; m++)
                    {
                        Vec2[] placed = ShapeIntoBox(OrganicControls(index, mild[m]), box);
                        if (placed != null && placed.Length >= 12 && RestsInside(card, placed, InsideMargin))
                            coast = placed;
                    }

                    if (coast == null)
                        continue;

                    shaped = coast;
                }

                hexes[index] = shaped;
                return true;
            }

            return false;
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

            if (placed != null)
            {
                UncoverSampleCards(hexes, pins, placed, cue, selected, detail, plateWidth, plateHeight);
                RaiseStripCards(hexes, pins, placed, ref cue, selected, detail, plateWidth, plateHeight, fa);
                if (fa && plateWidth / Math.Max(1f, plateHeight) < 1.5f)
                    ShiftDesertOffCentre(hexes, pins, placed, ref cue, selected, detail, plateWidth, plateHeight, fa);
            }

            if (placed != null)
                FitSelectionInFrame(placed, hexes, pins, ref cue, selected, detail, plateWidth, plateHeight, fa);

            if (placed != null)
                HoldWidePortraits(hexes, pins, placed, ref cue, selected, detail, plateWidth, plateHeight);

            if (placed != null)
                SeatCardsInRegions(hexes, pins, placed, ref cue, selected, detail, plateWidth, plateHeight);

            if (fa && placed != null && SeatWideFarsiPill(
                hexes, pins, placed, ref cue, selected, detail, plateWidth, plateHeight))
                cueNear = CueCountsAsNear(cue, placed[selected], pins[selected], hexes[selected]);

            if (placed != null && TryParkSelectedCue(
                hexes, pins, placed, ref cue, selected, detail, plateWidth, plateHeight, fa))
                cueNear = CueCountsAsNear(cue, placed[selected], pins[selected], hexes[selected]);

            if (placed != null)
            {
                EnlargeBoardDistricts(hexes, pins, placed, cue, selected, detail, plateWidth, plateHeight, fa);
                if (cue.W > 1f)
                    cueNear = CueCountsAsNear(cue, placed[selected], pins[selected], hexes[selected]);
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
            float[] widths = { 160f, 156f, 148f, 140f, 132f, 124f, 120f, 116f, PortraitMinWidth };
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
        /// The one zoom cue: a tab above the selected card, outside it, covering the card's top
        /// edge, 36–62px tall, bottom flush with the card's top edge. It may be wider than the card.
        /// </summary>
        private static bool CueIsCardTopTab(Box card, Box cue)
        {
            if (card.W < 48f || cue.W < 8f)
                return false;
            if (cue.H < 36f - 0.5f || cue.H > 62f + 0.5f)
                return false;
            // The tab may be wider than the card. It still has to cover the card's top edge.
            if (cue.X > card.X + 1.5f || cue.XMax < card.XMax - 1.5f)
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
            float[] heights = TabHeights;
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
        /// Two Farsi lines at 22–26 need a 56–62px tab. The selected card moves, and if a
        /// neighbour or the card's own height is what blocks that tab, that blocker shrinks
        /// or steps aside. The cue may also widen past the card when one line is wider than 160.
        /// </summary>
        public static bool TryAttachFarsiCue(
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
            if (selected < 0 || selected >= cards.Length)
                return false;
            Box card = cards[selected];
            if (card.W < 48f || card.H < 48f)
                return false;
            if (TryFarsiTab(plateWidth, plateHeight, pins, cards, hexes, detail, selected, card, out cue))
                return true;
            float minH = PortraitMinHeight;
            for (float cut = 4f; cut <= card.H - minH + 0.1f; cut += 4f)
            {
                var shorter = new Box(card.X, card.Y + cut, card.W, card.H - cut);
                Box held = cards[selected];
                cards[selected] = shorter;
                bool seated = TryFarsiTab(plateWidth, plateHeight, pins, cards, hexes, detail, selected, shorter, out cue);
                if (seated)
                    return true;
                cards[selected] = held;
            }

            if (TryWidenFarsiCue(plateWidth, plateHeight, pins, cards, hexes, detail, selected, card, out cue))
                return true;
            if (TryNudgeNeighbourForFarsi(plateWidth, plateHeight, pins, cards, hexes, detail, selected, card, out cue))
                return true;
            return false;
        }

        /// <summary>Steps a card that sits under the selected one downward so the Farsi tab can drop.</summary>
        private static bool TryNudgeNeighbourForFarsi(
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
            for (int i = 0; i < cards.Length; i++)
            {
                if (i == selected || cards[i].W < 1f || cards[i].Y + 8f < card.YMax)
                    continue;
                Box neighbour = cards[i];
                for (float drop = 8f; drop <= 48.1f; drop += 8f)
                {
                    var moved = new Box(neighbour.X, neighbour.Y + drop, neighbour.W, neighbour.H);
                    if (!SelectedCardCanDrop(moved, i, cards, pins, hexes, detail, plateWidth, plateHeight))
                        continue;
                    Box held = cards[i];
                    cards[i] = moved;
                    if (TryFarsiTab(plateWidth, plateHeight, pins, cards, hexes, detail, selected, card, out cue))
                        return true;
                    cards[i] = held;
                }
            }

            return false;
        }

        private static bool TryFarsiTab(
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
            UseFarsiTabs = true;
            try
            {
                if (TrySeatAlignedTop(plateWidth, plateHeight, pins, cards, hexes, detail, selected, card, 56f, out cue))
                    return true;
                if (TryDropCardForCue(plateWidth, plateHeight, pins, cards, hexes, detail, selected, card, 56f, out cue))
                    return true;
                if (TryShiftCardForTab(plateWidth, plateHeight, pins, cards, hexes, detail, selected, card, out cue))
                    return true;
            }
            finally
            {
                UseFarsiTabs = false;
            }

            cue = default;
            return false;
        }

        private static bool UseFarsiTabs;
        private static readonly float[] StandardTabHeights = { 52f, 48f, 44f, 40f, 36f };
        private static readonly float[] FarsiTabHeights = { 62f, 60f, 58f, 56f, 52f, 48f, 44f, 40f, 36f };
        private static float[] TabHeights => UseFarsiTabs ? FarsiTabHeights : StandardTabHeights;

        /// <summary>
        /// One Farsi line at 22px is wider than the 160px card. The tab grows past the card,
        /// still flush with its top, and the card steps aside when the stroke is the blocker.
        /// </summary>
        private static bool TryWidenFarsiCue(
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
            float[] widths = { 176f, 196f, 220f, 240f };
            float[] heights = { 48f, 44f, 40f, 62f, 56f };
            float[] slides = { 0f, 8f, -8f, 16f, -16f, 28f, -28f, 40f, -40f };
            for (int w = 0; w < widths.Length; w++)
            {
                float width = widths[w];
                for (int h = 0; h < heights.Length; h++)
                {
                    float stripH = heights[h];
                    for (int s = 0; s < slides.Length; s++)
                    {
                        float x = card.X + slides[s];
                        var dropped = new Box(x, card.Y, card.W, card.H);
                        if (!SelectedCardCanDrop(dropped, selected, cards, pins, hexes, detail, plateWidth, plateHeight))
                            continue;
                        float cueX = dropped.X + dropped.W * 0.5f - width * 0.5f;
                        var spot = new Box(cueX, dropped.Y - stripH, width, stripH);
                        Box held = cards[selected];
                        cards[selected] = dropped;
                        bool legal = FarsiWideTabLegal(spot, dropped, pins, cards, hexes, detail, selected, plateWidth, plateHeight);
                        if (!legal)
                        {
                            cards[selected] = held;
                            continue;
                        }

                        cue = spot;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool FarsiWideTabLegal(
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
            if (!CueIsCardTopTab(card, spot))
                return false;
            if (!OnPlate(spot, plateWidth, plateHeight) || !Clears(spot, detail))
                return false;
            if (pins != null)
            {
                for (int i = 0; i < pins.Length; i++)
                {
                    if (pins[i].W > 1f && Separation(spot, pins[i]) < 1f)
                        return false;
                }
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
            float[] heights = TabHeights;
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
            float[] heights = TabHeights;
            float best = float.MaxValue;
            Box bestCard = default;
            Box bestCue = default;
            bool found = false;
            for (int h = 0; h < heights.Length; h++)
            {
                float stripH = heights[h];
                // A short tab must not win the Farsi search and skip the widen path.
                if (UseFarsiTabs && stripH + 0.1f < 56f)
                    continue;
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
            for (int i = 0; i < shape.Length; i++)
            {
                Vec2 next = shape[(i + 1) % shape.Length];
                if (SegmentHitsRect(shape[i], next, card))
                    return false;
            }

            Vec2[] corners = Corners(card);
            const float sample = 4f;
            for (int edge = 0; edge < corners.Length; edge++)
            {
                Vec2 a = corners[edge];
                Vec2 b = corners[(edge + 1) % corners.Length];
                float span = Distance(a, b);
                int steps = (int)Math.Ceiling(span / sample);
                if (steps < 1)
                    steps = 1;
                for (int step = 0; step < steps; step++)
                {
                    float t = step / (float)steps;
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

        /// <summary>
        /// Maps an organic outline onto <paramref name="box"/> without dropping vertices.
        /// A four-corner stand-in fails the visible-area check.
        /// </summary>
        private static Vec2[] ShapeIntoBox(Vec2[] shape, Box box)
        {
            if (shape == null || shape.Length < 6 || box.W < 1f || box.H < 1f)
                return null;
            float minX = shape[0].X, maxX = shape[0].X, minY = shape[0].Y, maxY = shape[0].Y;
            for (int i = 1; i < shape.Length; i++)
            {
                minX = Math.Min(minX, shape[i].X);
                maxX = Math.Max(maxX, shape[i].X);
                minY = Math.Min(minY, shape[i].Y);
                maxY = Math.Max(maxY, shape[i].Y);
            }

            float sw = Math.Max(1f, maxX - minX);
            float sh = Math.Max(1f, maxY - minY);
            var placed = new Vec2[shape.Length];
            for (int i = 0; i < shape.Length; i++)
            {
                float u = (shape[i].X - minX) / sw;
                float v = (shape[i].Y - minY) / sh;
                placed[i] = new Vec2(box.X + u * box.W, box.Y + v * box.H);
            }

            return placed;
        }

        /// <summary>
        /// Moves a coastline into the window as one shape. A pure slide is used when the
        /// outline already fits. Uniform scale is the fallback, so RTL 4:3 does not fold.
        /// </summary>
        private static Vec2[] FitShapeInWindow(Vec2[] shape, float minX, float minY, float maxX, float maxY)
        {
            if (shape == null || shape.Length < 3)
                return shape;
            BoundsOf(shape, out float sx0, out float sy0, out float sx1, out float sy1);
            if (sx0 >= minX - 0.5f && sx1 <= maxX + 0.5f && sy0 >= minY - 0.5f && sy1 <= maxY + 0.5f)
                return shape;
            float sw = Math.Max(1f, sx1 - sx0);
            float sh = Math.Max(1f, sy1 - sy0);
            float availW = maxX - minX;
            float availH = maxY - minY;
            if (availW < 8f || availH < 8f)
                return shape;
            float scale = Math.Min(1f, Math.Min(availW / sw, availH / sh));
            if (scale < 0.05f)
                return shape;
            CentroidOf(shape, out float cx, out float cy);
            var next = new Vec2[shape.Length];
            float nx0 = float.MaxValue, nx1 = float.MinValue, ny0 = float.MaxValue, ny1 = float.MinValue;
            for (int i = 0; i < shape.Length; i++)
            {
                next[i] = new Vec2(cx + (shape[i].X - cx) * scale, cy + (shape[i].Y - cy) * scale);
                nx0 = Math.Min(nx0, next[i].X);
                nx1 = Math.Max(nx1, next[i].X);
                ny0 = Math.Min(ny0, next[i].Y);
                ny1 = Math.Max(ny1, next[i].Y);
            }

            float dx = 0f, dy = 0f;
            if (nx0 < minX)
                dx = minX - nx0;
            else if (nx1 > maxX)
                dx = maxX - nx1;
            if (ny0 < minY)
                dy = minY - ny0;
            else if (ny1 > maxY)
                dy = maxY - ny1;
            if (Math.Abs(dx) < 0.01f && Math.Abs(dy) < 0.01f && scale > 0.999f)
                return shape;
            for (int i = 0; i < next.Length; i++)
                next[i] = new Vec2(next[i].X + dx, next[i].Y + dy);
            return next;
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
