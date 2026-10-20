using System;
using System.Globalization;
using System.IO;
using Game.UI.Runtime;

internal static class Program
{
    private const string ReplayRoot = "Design/AgentReports/SkirmishBattleLibrary/2026-10-14";

    /// <summary>
    /// The dump lives in the repo. Unity and the seat check are not always started
    /// with that repo as the working directory, so walk parents from the process
    /// and from the current directory until the fixture directory is found.
    /// </summary>
    private static string ResolveReplayRoot()
    {
        if (Directory.Exists(ReplayRoot))
            return ReplayRoot;
        string[] starts =
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };
        for (int s = 0; s < starts.Length; s++)
        {
            var dir = new DirectoryInfo(starts[s]);
            for (int hop = 0; hop < 8 && dir != null; hop++)
            {
                string candidate = Path.Combine(dir.FullName, ReplayRoot);
                if (Directory.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }
        }

        return ReplayRoot;
    }

    private static int _cuePlates;
    private static int _cueWorstMiss;
    private static int _cueWorstHit;

    private static int Main(string[] args)
    {
        int seated = 0;
        int blocked = 0;
        int failed = 0;
        if (args != null && Array.IndexOf(args, "composition") >= 0)
        {
            RunComposition("4:3", 1440f, 1080f, ref seated, ref blocked, ref failed);
            Console.WriteLine("composition seated=" + seated + " failed=" + failed);
            return failed == 0 ? 0 : 1;
        }
        if (args != null && Array.IndexOf(args, "pill") >= 0)
            return ReplaySixteenByNineFaPill() ? 0 : 1;
        if (args != null && Array.IndexOf(args, "frames") >= 0)
            return DrawnFramesHold() ? 0 : 1;
        if (args != null && args.Length > 0 && args[0] == "probe")
        {
            string path = args.Length > 1 ? args[1] : "probe-territory.txt";
            using (var writer = new StreamWriter(path, false))
            {
                writer.AutoFlush = true;
                Probe(writer, "16:9", 1920f, 1080f);
                Probe(writer, "20:9", 2400f, 1080f);
                Probe(writer, "4:3", 1440f, 1080f);
            }

            Console.WriteLine("probe wrote " + path);
            return 0;
        }
        if (args != null && Array.IndexOf(args, "reserve") >= 0)
            return FourByThreeReserveHolds() ? 0 : 1;
        if (args != null && Array.IndexOf(args, "art") >= 0)
            return CardArtBandsHold() ? 0 : 1;
        if (args != null && Array.IndexOf(args, "diag") >= 0)
            return Diagnose();
        if (args != null && Array.IndexOf(args, "cover") >= 0)
            return Cover();
        if (!CardArtBandsHold() || !RectangularTerritoryRejected() || !MapPanelWidthsHold() || !FourByThreeReserveHolds() || !DrawnFramesHold() || !StampHolds() || !DumpRoundTrips() || !MirrorHolds() || !ReplayPlateHolds()
            || !ReplayFourByThreeFaIb() || !ReplaySixteenByNineFaPill() || !ReplayRuntimeCards()
            || !ReplayZoomInLabel())
            failed++;

        RunCanvasPlates(ref seated, ref blocked, ref failed);
        RunNotchCheckedPlates(ref seated, ref blocked, ref failed);
        RunFittedPlates(ref seated, ref blocked, ref failed);
        RunCompositionPlates(ref seated, ref blocked, ref failed);
        ReplayDumps(ref seated, ref blocked, ref failed);

        Console.WriteLine(
            "THRESHOLD 4:3 detail reserve stays on for every district. Map floor "
            + TheatreSeatGeometry.MapPanelWidthFloorPx.ToString("0", CultureInfo.InvariantCulture)
            + ".");
        int enSteps = TheatreSeatGeometry.ZoomCueSchedule(false, 160f, 36f).Length;
        int faSteps = TheatreSeatGeometry.ZoomCueSchedule(true, 160f, 44f).Length;
        Console.WriteLine("[ZoomCueAttempt] cap="
            + TheatreSeatGeometry.ZoomCueAttemptCap.ToString(CultureInfo.InvariantCulture)
            + " enSchedule=" + enSteps.ToString(CultureInfo.InvariantCulture)
            + " faSchedule=" + faSteps.ToString(CultureInfo.InvariantCulture)
            + " worstMiss=" + _cueWorstMiss.ToString(CultureInfo.InvariantCulture)
            + " worstHit=" + _cueWorstHit.ToString(CultureInfo.InvariantCulture)
            + " plates=" + _cuePlates.ToString(CultureInfo.InvariantCulture));
        if (failed == 0 && blocked == 0)
        {
            Console.WriteLine("TheatreSeatCheck result=Passed seated=" + seated + " blocked=0");
            return 0;
        }

        Console.WriteLine("TheatreSeatCheck result=Failed count=" + failed + " seated=" + seated + " blocked=" + blocked);
        return 1;
    }

    private static int Cover()
    {
        CoverOne("16:9", 1920f, 1080f, false, false);
        CoverOne("16:9", 1920f, 1080f, true, false);
        CoverOne("16:9", 1920f, 1080f, false, true);
        CoverOne("20:9", 2400f, 1080f, false, false);
        CoverOne("20:9", 2400f, 1080f, true, false);
        CoverOne("20:9", 2400f, 1080f, false, true);
        CoverOne("4:3", 1440f, 1080f, false, true);
        CoverOne("4:3", 1440f, 1080f, true, true);
        CoverOne("4:3", 1079.6f, 844f, false, true);
        CoverOne("4:3", 1440f, 1080f, false, false);
        return 0;
    }

    private static void CoverOne(string ratio, float canvasWidth, float canvasHeight, bool fa, bool reserve)
    {
        float plateWidth;
        float plateHeight;
        if (Math.Abs(canvasWidth - 1079.6f) < 0.1f)
        {
            plateWidth = 1079.6f;
            plateHeight = canvasHeight;
        }
        else
            TheatreSeatGeometry.CaptureMapSize(canvasWidth, canvasHeight, out plateWidth, out plateHeight);
        bool four = ratio == "4:3";
        bool applied = TheatreSeatGeometry.DetailReserveOn(plateWidth, plateHeight, fa, reserve, four, "DB");
        TheatreSeatGeometry.SetShot(ratio, "none", fa ? "fa" : "en", applied, canvasWidth, canvasHeight, 0f, 0f, 0f, 0f);
        TheatreSeatGeometry.ShotSelected = "DB";
        TheatreSeatGeometry.Layout layout = TheatreSeatGeometry.Arrange(plateWidth, plateHeight, fa, applied, "DB");
        float coverage = TheatreSeatGeometry.DrawnTerritoryCoverage(layout, plateWidth, plateHeight, fa, applied);
        TheatreSeatGeometry.Box view = TheatreSeatGeometry.OpenViewport(plateWidth, plateHeight, fa, applied);
        Console.WriteLine(TheatreSeatGeometry.FormatTerritoryCoverage(ratio, fa ? "fa" : "en", coverage, applied)
            + " plate=" + plateWidth.ToString("0.0", CultureInfo.InvariantCulture)
            + "x" + plateHeight.ToString("0.0", CultureInfo.InvariantCulture)
            + " view=" + view.W.ToString("0.0", CultureInfo.InvariantCulture)
            + "x" + view.H.ToString("0.0", CultureInfo.InvariantCulture)
            + " reserve=" + (applied ? "on" : "off"));
    }

    private static int Diagnose()
    {
        DiagnoseOne("16:9", 1920f, 1080f, false, false, "DB");
        DiagnoseOne("16:9", 1920f, 1080f, false, false, "IB");
        DiagnoseOne("20:9", 2400f, 1080f, false, false, "IB");
        DiagnoseOne("20:9", 2400f, 1080f, false, false, "DB");
        DiagnoseOne("4:3", 1440f, 1080f, false, false, "IB");
        DiagnoseOne("4:3", 1440f, 1080f, false, true, "IB");
        DiagnoseOne("4:3", 1440f, 1080f, true, true, "IB");
        DiagnoseOne("16:9", 1920f, 1080f, true, false, "DB");
        return 0;
    }

    private static void DiagnoseOne(string ratio, float canvasWidth, float canvasHeight, bool fa, bool reserve, string selected)
    {
        TheatreSeatGeometry.CaptureMapSize(canvasWidth, canvasHeight, out float plateWidth, out float plateHeight);
        bool applied = TheatreSeatGeometry.DetailReserveOn(plateWidth, plateHeight, fa, reserve, ratio == "4:3", selected);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        TheatreSeatGeometry.SetShot(ratio, "none", fa ? "fa" : "en", applied, canvasWidth, canvasHeight, 0f, 0f, 0f, 0f);
        TheatreSeatGeometry.ShotSelected = selected;
        TheatreSeatGeometry.ShiftCardScans = 0;
        TheatreSeatGeometry.Layout layout = TheatreSeatGeometry.Arrange(plateWidth, plateHeight, fa, applied, selected);
        clock.Stop();
        TheatreSeatGeometry.Box detail = TheatreSeatGeometry.DetailRect(plateWidth, plateHeight, fa, applied);
        TheatreSeatGeometry.Box hint = TheatreSeatGeometry.ZoomHintRect(plateWidth, plateHeight);
        Console.WriteLine("DIAG " + ratio + (fa ? " FA" : " EN") + " sel=" + selected
            + " reserve=" + applied
            + " plate=" + plateWidth.ToString("0.0", CultureInfo.InvariantCulture) + "x" + plateHeight.ToString("0.0", CultureInfo.InvariantCulture)
            + " ms=" + clock.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture)
            + " legal=" + layout.CardsLegal + " cueNear=" + layout.CueNear + " aspect=" + layout.HexAspectLegal
            + " cue=" + layout.Cue.X.ToString("0.0", CultureInfo.InvariantCulture) + "," + layout.Cue.Y.ToString("0.0", CultureInfo.InvariantCulture)
            + " " + layout.Cue.W.ToString("0", CultureInfo.InvariantCulture) + "x" + layout.Cue.H.ToString("0", CultureInfo.InvariantCulture)
            + " scans=" + TheatreSeatGeometry.ShiftCardScans.ToString(CultureInfo.InvariantCulture));
        if (layout.Seats == null)
            return;
        var hexes = new TheatreSeatGeometry.Vec2[layout.Seats.Length][];
        var cards = new TheatreSeatGeometry.Box[layout.Seats.Length];
        for (int i = 0; i < layout.Seats.Length; i++)
        {
            hexes[i] = layout.Seats[i].Hex;
            cards[i] = layout.Seats[i].Card;
        }

        int selectedIndex = 0;
        for (int i = 0; i < layout.Seats.Length; i++)
        {
            if (string.Equals(layout.Seats[i].MapId, selected, StringComparison.Ordinal))
                selectedIndex = i;
        }

        float nearest = float.MaxValue;
        float widest = 0f;
        string pair = "";
        string widePair = "";
        var drawnShapes = new TheatreSeatGeometry.Vec2[layout.Seats.Length][];
        for (int i = 0; i < layout.Seats.Length; i++)
        {
            TheatreSeatGeometry.Vec2[][] others = new TheatreSeatGeometry.Vec2[hexes.Length][];
            for (int j = 0; j < hexes.Length; j++)
            {
                if (j != i)
                    others[j] = hexes[j];
            }

            TheatreSeatGeometry.Box cue = i == selectedIndex ? layout.Cue : default;
            drawnShapes[i] = TheatreSeatGeometry.FinishTerritory(
                hexes[i], cards[i], layout.Seats[i].Pin, layout.Seats[i].MapId, others, cue, plateWidth, plateHeight, detail, hint);
        }

        for (int i = 0; i < drawnShapes.Length; i++)
        {
            for (int j = i + 1; j < drawnShapes.Length; j++)
            {
                float gap = TheatreSeatGeometry.DrawnDistrictGap(drawnShapes[i], drawnShapes[j]);
                if (gap < nearest)
                {
                    nearest = gap;
                    pair = layout.Seats[i].MapId + "-" + layout.Seats[j].MapId;
                }

                if (gap > widest)
                {
                    widest = gap;
                    widePair = layout.Seats[i].MapId + "-" + layout.Seats[j].MapId;
                }
            }
        }

        Console.WriteLine("  drawnHalo near=" + pair + ":" + nearest.ToString("0.0", CultureInfo.InvariantCulture)
            + " far=" + widePair + ":" + widest.ToString("0.0", CultureInfo.InvariantCulture));
        string[] pairNames = { "DB-CC", "MP-IB", "DB-MP", "CC-IB", "MP-AP", "IB-AP" };
        int[] pairA = { 0, 2, 0, 1, 2, 3 };
        int[] pairB = { 1, 3, 2, 3, 4, 4 };
        var seam = new System.Text.StringBuilder("  seams");
        for (int p = 0; p < pairA.Length && pairB[p] < drawnShapes.Length; p++)
        {
            float gap = TheatreSeatGeometry.DrawnDistrictGap(drawnShapes[pairA[p]], drawnShapes[pairB[p]]);
            seam.Append(' ').Append(pairNames[p]).Append('=').Append(gap.ToString("0.0", CultureInfo.InvariantCulture));
        }

        Console.WriteLine(seam.ToString());
        for (int i = 0; i < layout.Seats.Length; i++)
        {
            TheatreSeatGeometry.Vec2[] hex = layout.Seats[i].Hex;
            TheatreSeatGeometry.Vec2[] drawn = drawnShapes[i];
            TheatreSeatGeometry.Box card = layout.Seats[i].Card;
            Bounds(hex, out float minX, out float minY, out float maxX, out float maxY);
            float dx = card.W > 1f ? card.Center.X - (minX + maxX) * 0.5f : 0f;
            float dy = card.W > 1f ? card.Center.Y - (minY + maxY) * 0.5f : 0f;
            Bounds(drawn, out float dMinX, out float dMinY, out float dMaxX, out float dMaxY);
            float ddx = card.W > 1f && drawn != null ? card.Center.X - (dMinX + dMaxX) * 0.5f : 0f;
            float ddy = card.W > 1f && drawn != null ? card.Center.Y - (dMinY + dMaxY) * 0.5f : 0f;
            float round = Roundness(drawn);
            float cueDx = 0f;
            float cueDy = 0f;
            bool cueRight = false;
            if (i == selectedIndex && layout.Cue.W > 1f && card.W > 1f)
            {
                cueDx = layout.Cue.Center.X - card.Center.X;
                cueDy = layout.Cue.Center.Y - card.Center.Y;
                cueRight = layout.Cue.X + 4f >= card.XMax && layout.Cue.Center.Y >= card.Y - 8f && layout.Cue.Center.Y <= card.YMax + 8f;
            }

            Console.WriteLine("  " + layout.Seats[i].MapId
                + " hexN=" + (hex != null ? hex.Length : 0)
                + " drawnN=" + (drawn != null ? drawn.Length : 0)
                + " box=" + (maxX - minX).ToString("0", CultureInfo.InvariantCulture) + "x" + (maxY - minY).ToString("0", CultureInfo.InvariantCulture)
                + " cardOff=" + dx.ToString("0.0", CultureInfo.InvariantCulture) + "," + dy.ToString("0.0", CultureInfo.InvariantCulture)
                + " drawnOff=" + ddx.ToString("0.0", CultureInfo.InvariantCulture) + "," + ddy.ToString("0.0", CultureInfo.InvariantCulture)
                + " round=" + round.ToString("0.00", CultureInfo.InvariantCulture)
                + " drawnBox=" + (dMaxX - dMinX).ToString("0", CultureInfo.InvariantCulture) + "x" + (dMaxY - dMinY).ToString("0", CultureInfo.InvariantCulture)
                + (i == selectedIndex ? " cueOff=" + cueDx.ToString("0.0", CultureInfo.InvariantCulture) + "," + cueDy.ToString("0.0", CultureInfo.InvariantCulture) + (cueRight ? " RIGHT" : "") : ""));
        }
    }

    /// <summary>0 is a circle. A flat hex sits higher because its radii jump at the corners.</summary>
    private static float Roundness(TheatreSeatGeometry.Vec2[] shape)
    {
        if (shape == null || shape.Length < 3)
            return 0f;
        float cx = 0f;
        float cy = 0f;
        for (int i = 0; i < shape.Length; i++)
        {
            cx += shape[i].X;
            cy += shape[i].Y;
        }

        cx /= shape.Length;
        cy /= shape.Length;
        float mean = 0f;
        for (int i = 0; i < shape.Length; i++)
        {
            float dx = shape[i].X - cx;
            float dy = shape[i].Y - cy;
            mean += (float)Math.Sqrt(dx * dx + dy * dy);
        }

        mean /= shape.Length;
        if (mean < 1f)
            return 0f;
        float dev = 0f;
        for (int i = 0; i < shape.Length; i++)
        {
            float dx = shape[i].X - cx;
            float dy = shape[i].Y - cy;
            float radius = (float)Math.Sqrt(dx * dx + dy * dy);
            float delta = radius - mean;
            dev += delta * delta;
        }

        return (float)Math.Sqrt(dev / shape.Length) / mean;
    }

    private static void Bounds(TheatreSeatGeometry.Vec2[] shape, out float minX, out float minY, out float maxX, out float maxY)
    {
        minX = minY = float.MaxValue;
        maxX = maxY = float.MinValue;
        if (shape == null)
            return;
        for (int i = 0; i < shape.Length; i++)
        {
            minX = Math.Min(minX, shape[i].X);
            minY = Math.Min(minY, shape[i].Y);
            maxX = Math.Max(maxX, shape[i].X);
            maxY = Math.Max(maxY, shape[i].Y);
        }
    }

    private static bool DrawnFramesHold()
    {
        var faults = new System.Collections.Generic.List<string>();
        NoteFrame(faults, "16:9", 1920f, 1080f);
        NoteFrame(faults, "20:9", 2400f, 1080f);
        NoteFrame(faults, "4:3", 1440f, 1080f);
        if (faults.Count == 0)
        {
            Console.WriteLine("PASS drawn territories");
            return true;
        }

        for (int i = 0; i < faults.Count; i++)
            Console.WriteLine(faults[i]);
        return false;
    }

    private static void NoteFrame(System.Collections.Generic.List<string> faults, string ratio, float canvasWidth, float canvasHeight)
    {
        TheatreSeatGeometry.CaptureMapSize(canvasWidth, canvasHeight, out float plateWidth, out float plateHeight);
        string[] selected = ratio == "4:3" ? TheatreSeatGeometry.MapIds : new[] { "DB" };
        foreach (bool fa in new[] { false, true })
        {
            foreach (bool reserve in new[] { false, true })
            {
                for (int i = 0; i < selected.Length; i++)
                {
                    string report = TheatreSeatGeometry.ProbePlate(plateWidth, plateHeight, fa, reserve, selected[i]);
                    if (report.Contains(" OUT") || report.Contains(" GLOW") || report.Contains(" HINT") || report.Contains(" PANEL"))
                        faults.Add(ratio + " " + report);
                    foreach (string part in report.Split('|'))
                    {
                        int aspect = part.IndexOf("aspect=", StringComparison.Ordinal);
                        int slash = aspect < 0 ? -1 : part.IndexOf('/', aspect);
                        int hex = slash < 0 ? -1 : part.IndexOf(" hex=", slash, StringComparison.Ordinal);
                        int samples = part.IndexOf(" in=", StringComparison.Ordinal);
                        int cut = samples < 0 ? -1 : part.IndexOf('/', samples + 4);
                        if (aspect < 0 || slash < 0 || hex < 0 || samples < 0 || cut < 0)
                            continue;
                        if (!float.TryParse(part.Substring(aspect + 7, slash - (aspect + 7)), NumberStyles.Float, CultureInfo.InvariantCulture, out float measured))
                            continue;
                        if (!float.TryParse(part.Substring(slash + 1, hex - (slash + 1)), NumberStyles.Float, CultureInfo.InvariantCulture, out float expected))
                            continue;
                        if (!int.TryParse(part.Substring(cut + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int blocked))
                        {
                            int end = cut + 1;
                            while (end < part.Length && char.IsDigit(part[end]))
                                end++;
                            int.TryParse(part.Substring(cut + 1, end - (cut + 1)), NumberStyles.Integer, CultureInfo.InvariantCulture, out blocked);
                        }

                        // Capture rejects a ratio more than 10% off. A 2-decimal print of a
                        // value sitting on the line parses back a hair over 0.10.
                        if (expected >= 0.2f && Math.Abs(measured / expected - 1f) > 0.101f)
                            faults.Add(ratio + " aspect " + part.Trim());
                        if (blocked < 8)
                            faults.Add(ratio + " interior " + (fa ? "fa " : "en ")
                                + (reserve ? "reserve " : "open ") + "sel=" + selected[i] + " " + part.Trim());
                        if (ratio == "20:9" && part.IndexOf("AP card=", StringComparison.Ordinal) >= 0)
                        {
                            int notchAt = part.IndexOf("notchIn=", StringComparison.Ordinal);
                            int notchSamples = 0;
                            if (notchAt >= 0)
                            {
                                int end = notchAt + 8;
                                while (end < part.Length && char.IsDigit(part[end]))
                                    end++;
                                int.TryParse(part.Substring(notchAt + 8, Math.Max(0, end - (notchAt + 8))), NumberStyles.Integer, CultureInfo.InvariantCulture, out notchSamples);
                            }

                            if (notchAt >= 0 && notchSamples < 8)
                                faults.Add(ratio + " notch interior " + part.Trim());
                        }
                    }
                }
            }
        }
    }

    private static void Probe(StreamWriter writer, string ratio, float canvasWidth, float canvasHeight)
    {
        TheatreSeatGeometry.CaptureMapSize(canvasWidth, canvasHeight, out float plateWidth, out float plateHeight);
        foreach (bool fa in new[] { false, true })
        {
            foreach (bool reserve in new[] { false, true })
            {
                writer.WriteLine(ratio + " " + TheatreSeatGeometry.ProbePlate(plateWidth, plateHeight, fa, reserve, "DB"));
                if (ratio != "4:3")
                    continue;
                writer.WriteLine(ratio + " " + TheatreSeatGeometry.ProbePlate(plateWidth, plateHeight, fa, reserve, "AP"));
                writer.WriteLine(ratio + " " + TheatreSeatGeometry.ProbePlate(plateWidth, plateHeight, fa, reserve, "CC"));
                writer.WriteLine(ratio + " " + TheatreSeatGeometry.ProbePlate(plateWidth, plateHeight, fa, reserve, "IB"));
            }
        }
    }

    private static void RunCanvasPlates(ref int seated, ref int blocked, ref int failed)
    {
        RunRatio("16:9", "none", 1920f, 1080f, 0f, 0f, 0f, 0f, PlateKind.Canvas, ref seated, ref blocked, ref failed);
        RunRatio("20:9", "none", 2400f, 1080f, 0f, 0f, 0f, 0f, PlateKind.Canvas, ref seated, ref blocked, ref failed);
        RunRatio("4:3", "none", 1440f, 1080f, 0f, 0f, 0f, 0f, PlateKind.Canvas, ref seated, ref blocked, ref failed);
    }

    /// <summary>
    /// Capture's 20:9 notch is applied after the theatre checks, so those checks still
    /// see the plain plate. The label records the notch the screenshot will draw.
    /// </summary>
    private static void RunNotchCheckedPlates(ref int seated, ref int blocked, ref int failed)
    {
        RunRatio("20:9", "notch", 2400f, 1080f, 132f, 84f, 0f, 0f, PlateKind.Canvas, ref seated, ref blocked, ref failed);
    }

    private static void RunFittedPlates(ref int seated, ref int blocked, ref int failed)
    {
        RunRatio("16:9", "safe", 1920f, 1080f, 0f, 47f, 0f, 0f, PlateKind.Fitted, ref seated, ref blocked, ref failed);
        RunRatio("20:9", "safe", 2400f, 1080f, 0f, 47f, 0f, 0f, PlateKind.Fitted, ref seated, ref blocked, ref failed);
        RunRatio("4:3", "safe", 1440f, 1080f, 0f, 47f, 0f, 0f, PlateKind.Fitted, ref seated, ref blocked, ref failed);
        RunRatio("4:3", "safe", 1440f, 1080f, 0f, 47f, 0f, 21f, PlateKind.Inset, ref seated, ref blocked, ref failed);
        RunRatio("16:9", "notch", 1920f, 1080f, 132f, 84f, 0f, 0f, PlateKind.Fitted, ref seated, ref blocked, ref failed);
        RunRatio("20:9", "notch", 2400f, 1080f, 132f, 84f, 0f, 0f, PlateKind.Fitted, ref seated, ref blocked, ref failed);
        RunRatio("4:3", "notch", 1440f, 1080f, 132f, 84f, 0f, 0f, PlateKind.Fitted, ref seated, ref blocked, ref failed);
    }

    private static void RunCompositionPlates(ref int seated, ref int blocked, ref int failed)
    {
        RunComposition("16:9", 1920f, 1080f, ref seated, ref blocked, ref failed);
        RunComposition("20:9", 2400f, 1080f, ref seated, ref blocked, ref failed);
        RunComposition("4:3", 1440f, 1080f, ref seated, ref blocked, ref failed);
    }

    private enum PlateKind
    {
        Canvas,
        Fitted,
        Inset
    }

    private static void RunRatio(
        string ratio,
        string notch,
        float canvasWidth,
        float canvasHeight,
        float left,
        float top,
        float right,
        float bottom,
        PlateKind kind,
        ref int seated,
        ref int blocked,
        ref int failed)
    {
        float plateWidth;
        float plateHeight;
        if (kind == PlateKind.Inset)
        {
            TheatreSeatGeometry.CaptureMapSize(
                Math.Max(2f, canvasWidth - left - right),
                Math.Max(2f, canvasHeight - top - bottom),
                out plateWidth,
                out plateHeight);
        }
        else if (kind == PlateKind.Fitted)
        {
            TheatreSeatGeometry.ApplySafeFrame(
                canvasWidth, canvasHeight, left, top, right, bottom, out float usableWidth, out float usableHeight);
            TheatreSeatGeometry.CaptureMapSize(usableWidth, usableHeight, out plateWidth, out plateHeight);
        }
        else
            TheatreSeatGeometry.CaptureMapSize(canvasWidth, canvasHeight, out plateWidth, out plateHeight);

        // Capture builds a fresh library for every shot. The recommended district is Industrial Basin
        // (S073 holds a Recruit medal and fewer wins than Desert Base). City Crossroads fails the same
        // 4:3 reserve, so the plain canvases seat every district. Other plates stay on Desert Base,
        // which is the seat the 16:9 and 20:9 screens already show.
        string[] maps = notch == "none"
            ? TheatreSeatGeometry.MapIds
            : new[] { "DB" };
        for (int locale = 0; locale < 2; locale++)
        {
            bool fa = locale == 1;
            for (int reserve = 0; reserve < 2; reserve++)
            {
                for (int map = 0; map < maps.Length; map++)
                {
                    Evaluate(
                        ratio,
                        notch,
                        fa,
                        reserve == 1,
                        canvasWidth,
                        canvasHeight,
                        left,
                        top,
                        right,
                        bottom,
                        plateWidth,
                        plateHeight,
                        notch == "none" && maps[map] == "DB",
                        maps[map],
                        ref seated,
                        ref blocked,
                        ref failed);
                }
            }
        }
    }

    private static void RunComposition(string ratio, float canvasWidth, float canvasHeight, ref int seated, ref int blocked, ref int failed)
    {
        // Match the live library's reference frame, including the shell's dense canvas.
        // The old 941-high section model no longer describes the full-screen library.
        TheatreSeatGeometry.ResolveSagaFrame(canvasWidth, canvasHeight, out float frameWidth, out float frameHeight, out float scale);
        TheatreSeatGeometry.CaptureMapSize(frameWidth, frameHeight, out float plateWidth, out float plateHeight);
        Console.WriteLine("[CompositionFrame] ratio="+ratio+" canvas="+canvasWidth+"x"+canvasHeight+" frame="+frameWidth+"x"+frameHeight+" scale="+scale);
        for (int locale = 0; locale < 2; locale++)
        {
            bool fa = locale == 1;
            for (int reserve = 0; reserve < 2; reserve++)
            {
                Evaluate(
                    ratio,
                    "section",
                    fa,
                    reserve == 1,
                    canvasWidth,
                    canvasHeight,
                    0f,
                    0f,
                    0f,
                    0f,
                    plateWidth,
                    plateHeight,
                    false,
                    "DB",
                    ref seated,
                    ref blocked,
                    ref failed);
            }
        }
    }

    private static void Evaluate(
        string ratio,
        string notch,
        bool fa,
        bool reserveOn,
        float canvasWidth,
        float canvasHeight,
        float left,
        float top,
        float right,
        float bottom,
        float plateWidth,
        float plateHeight,
        bool logSeats,
        string selected,
        ref int seated,
        ref int blocked,
        ref int failed)
    {
        string locale = fa ? "fa" : "en";
        if (string.IsNullOrEmpty(selected))
            selected = "DB";
        bool fourByThree = string.Equals(ratio, "4:3", StringComparison.Ordinal);
        bool applied = TheatreSeatGeometry.DetailReserveOn(plateWidth, plateHeight, fa, reserveOn, fourByThree, selected);
        TheatreSeatGeometry.NoteDetailReserve(applied);
        TheatreSeatGeometry.SetShot(ratio, notch, locale, applied, canvasWidth, canvasHeight, left, top, right, bottom);
        TheatreSeatGeometry.ShotSelected = selected;
        TheatreSeatGeometry.Layout layout = TheatreSeatGeometry.Arrange(plateWidth, plateHeight, fa, applied, selected);
        string[] violations = TheatreSeatGeometry.CollectViolations(plateWidth, plateHeight, fa, applied, selected);
        bool outlines = OutlinesHold(layout, plateWidth, plateHeight);
        bool full = ReservedSeatsAreFull(layout, applied);
        bool cueBound = CueAttemptsBounded(ratio, locale, selected, fa, layout.Cue, false);
        bool ok = layout.CardsLegal && layout.CueNear && layout.HexAspectLegal && outlines && cueBound
            && layout.Seats != null && layout.Seats.Length == 5 && violations.Length == 0
            && FieldsHold(violations)
            && TheatreSeatGeometry.TheatreDetailReserved == applied
            && full;
        TheatreSeatGeometry.Box view = TheatreSeatGeometry.OpenViewport(plateWidth, plateHeight, fa, applied);
        TheatreSeatGeometry.Box reservedView = TheatreSeatGeometry.OpenViewport(plateWidth, plateHeight, fa, true);
        bool underFloor = reservedView.W + 0.5f < TheatreSeatGeometry.ReserveViewportFloor.X
            || reservedView.H + 0.5f < TheatreSeatGeometry.ReserveViewportFloor.Y;
        string card = layout.Seats != null && layout.Seats.Length > 0
            ? layout.Seats[0].Card.W.ToString("0", CultureInfo.InvariantCulture)
                + "x" + layout.Seats[0].Card.H.ToString("0", CultureInfo.InvariantCulture)
            : "-";
        string state;
        if (ok)
        {
            seated++;
            state = "PASS";
        }
        else
        {
            failed++;
            state = "FAIL";
        }

        Console.WriteLine(state + " " + ratio
            + " notch=" + notch
            + (fa ? " FA" : " EN")
            + (applied ? " reserve=on" : " reserve=off")
            + (reserveOn && !applied ? (underFloor ? " floor" : " sheet") : "")
            + " plate=" + plateWidth.ToString("0.0", CultureInfo.InvariantCulture)
            + "x" + plateHeight.ToString("0.0", CultureInfo.InvariantCulture)
            + " view=" + view.W.ToString("0.0", CultureInfo.InvariantCulture)
            + "x" + view.H.ToString("0.0", CultureInfo.InvariantCulture)
            + " cards=" + layout.CardsLegal
            + " cue=" + layout.CueNear
            + " aspect=" + layout.HexAspectLegal
            + " sel=" + selected
            + " DB=" + card
            + " violations=" + violations.Length);
        WriteCardCentres();
        if (!ok)
        {
            Console.WriteLine("  why outlines=" + outlines
                + " full=" + full
                + " cueBound=" + cueBound
                + " fields=" + FieldsHold(violations)
                + " reserved=" + (TheatreSeatGeometry.TheatreDetailReserved == applied)
                + " compact=" + TheatreSeatGeometry.CompactNote);
            int shown = Math.Min(violations.Length, 8);
            for (int i = 0; i < shown; i++)
                Console.WriteLine("  " + violations[i]);
            if (violations.Length > shown)
                Console.WriteLine("  … " + (violations.Length - shown) + " more");
        }
        else if (logSeats && layout.LogLines != null)
        {
            for (int i = 0; i < layout.LogLines.Length; i++)
                Console.WriteLine(layout.LogLines[i]);
        }
    }

    private static bool ReservedSeatsAreFull(TheatreSeatGeometry.Layout layout, bool reserved)
    {
        if (!reserved)
            return true;
        if (layout.Seats == null)
            return false;
        for (int i = 0; i < layout.Seats.Length; i++)
        {
            TheatreSeatGeometry.Box card = layout.Seats[i].Card;
            if (card.W + 0.5f < TheatreSeatGeometry.FullPortraitMinWidth
                || card.H + 0.5f < TheatreSeatGeometry.FullPortraitMinHeight)
                return false;
        }

        return true;
    }

    private static bool FieldsHold(string[] violations)
    {
        if (violations == null)
            return false;
        for (int i = 0; i < violations.Length; i++)
        {
            string message = violations[i] ?? string.Empty;
            if (message.IndexOf(" ratio=", StringComparison.Ordinal) < 0
                || message.IndexOf(" notch=", StringComparison.Ordinal) < 0
                || message.IndexOf(" locale=", StringComparison.Ordinal) < 0
                || message.IndexOf(" reserve=", StringComparison.Ordinal) < 0)
                return false;
        }

        return true;
    }

    private static bool StampHolds()
    {
        TheatreSeatGeometry.SetShot("4:3", "safe", "fa", true, 1440f, 1080f, 0f, 47f, 0f, 0f);
        string once = TheatreSeatGeometry.Stamp("Theatre zone name draws past its box for DB locale=fa text=DESERT BASE.");
        string twice = TheatreSeatGeometry.Stamp(once);
        string hex = TheatreSeatGeometry.Stamp(
            "Theatre district hex is stretched locale=en map=DB ratio=1.27 expected=1.00.");
        bool ok = once == twice
            && once.IndexOf(" ratio=4:3", StringComparison.Ordinal) >= 0
            && once.IndexOf(" notch=safe", StringComparison.Ordinal) >= 0
            && once.IndexOf(" locale=fa", StringComparison.Ordinal) >= 0
            && once.IndexOf(" reserve=on", StringComparison.Ordinal) >= 0
            && hex.IndexOf(" notch=safe", StringComparison.Ordinal) >= 0
            && hex.IndexOf(" ratio=4:3", StringComparison.Ordinal) >= 0
            && hex.IndexOf(" reserve=on", StringComparison.Ordinal) >= 0;
        Console.WriteLine((ok ? "PASS" : "FAIL") + " stamp fields");
        if (!ok)
        {
            Console.WriteLine("  " + once);
            Console.WriteLine("  " + hex);
        }

        return ok;
    }

    private static bool MirrorHolds()
    {
        bool ok = TheatreSeatGeometry.FaChromeMirrors(out string failure);
        Console.WriteLine((ok ? "PASS" : "FAIL") + " fa mirror" + (ok ? "" : " " + failure));
        return ok;
    }

    private static bool DumpRoundTrips()
    {
        TheatreSeatGeometry.SetShot("4:3", "notch", "en", true, 1440f, 1080f, 132f, 84f, 0f, 0f);
        TheatreSeatGeometry.ShotSelected = "DB";
        var detail = TheatreSeatGeometry.DetailRect(1039.6f, 724f, false, true);
        var view = TheatreSeatGeometry.OpenViewport(1039.6f, 724f, false, true);
        var names = new[]
        {
            new TheatreSeatGeometry.NameMeasure
            {
                MapId = "DB",
                Text = "DESERT\nBASE",
                Font = 16f,
                BoxWidth = 112f,
                BoxHeight = 44f,
                PreferredWidth = 108f,
                PreferredHeight = 40f,
                StatusHeight = 56f,
                Band = 212f,
                CardWidth = 156f,
                CardHeight = 208f
            }
        };
        string json = TheatreSeatGeometry.FormatDump(1039.6f, 724f, detail, view, names);
        bool read = TheatreSeatGeometry.TryReadDump(
            json, out float plateWidth, out float plateHeight, out bool reserve, out string ratio, out string notch, out string locale, out string selected);
        bool rawBreak = false;
        int textAt = json.IndexOf("\"text\":", StringComparison.Ordinal);
        if (textAt >= 0)
        {
            int end = json.IndexOf("\"font\"", textAt, StringComparison.Ordinal);
            string slice = end > textAt ? json.Substring(textAt, end - textAt) : json.Substring(textAt);
            rawBreak = slice.IndexOf('\n') >= 0 || slice.IndexOf('\r') >= 0;
        }

        string dirty = json.Replace("\\n", "\n");
        bool dirtyRejected = !TheatreSeatGeometry.TryReadDump(
            dirty, out _, out _, out _, out _, out _, out _, out _);
        bool ok = read
            && reserve
            && ratio == "4:3"
            && notch == "notch"
            && locale == "en"
            && selected == "DB"
            && !rawBreak
            && dirtyRejected
            && json.IndexOf("\\n", StringComparison.Ordinal) >= 0
            && Math.Abs(plateWidth - 1039.6f) < 0.05f
            && Math.Abs(plateHeight - 724f) < 0.05f
            && json.IndexOf("\"portraitMinW\": 112", StringComparison.Ordinal) >= 0
            && json.IndexOf("DESERT\\nBASE", StringComparison.Ordinal) >= 0
            && !TheatreSeatGeometry.TryReadNameOverflow(json, out _, out _, out _, out _)
            && !TheatreSeatGeometry.TryReadNameStack(json, out _, out _, out _, out _, out _);
        var overflowName = names[0];
        overflowName.MapId = "MP";
        overflowName.Text = "گذرگاه کوهستان";
        overflowName.PreferredWidth = 115.9f;
        overflowName.BoxWidth = 112f;
        string overflow = TheatreSeatGeometry.FormatDump(1039.6f, 724f, detail, view, new[] { overflowName });
        bool caught = TheatreSeatGeometry.TryReadNameOverflow(
            overflow, out string overflowMap, out string overflowText, out float overflowPreferred, out float overflowBox)
            && overflowMap == "MP"
            && overflowText.IndexOf("گذرگاه", StringComparison.Ordinal) >= 0
            && Math.Abs(overflowPreferred - 115.9f) < 0.05f
            && Math.Abs(overflowBox - 112f) < 0.05f
            && !TheatreSeatGeometry.TryReadNameStack(overflow, out _, out _, out _, out _, out _);
        var stackedName = names[0];
        stackedName.MapId = "MP";
        stackedName.Text = "گذرگاه کوهستان";
        stackedName.PreferredWidth = 100f;
        stackedName.BoxWidth = 112f;
        stackedName.PreferredHeight = 54f;
        stackedName.StatusHeight = 40f;
        stackedName.CardHeight = 156f;
        string stacked = TheatreSeatGeometry.FormatDump(1039.6f, 724f, detail, view, new[] { stackedName });
        bool stackCaught = TheatreSeatGeometry.TryReadNameStack(
            stacked, out string stackMap, out string stackText, out float stackName, out float stackStatus, out float stackCard)
            && stackMap == "MP"
            && stackText.IndexOf("گذرگاه", StringComparison.Ordinal) >= 0
            && Math.Abs(stackName - 54f) < 0.05f
            && Math.Abs(stackStatus - 40f) < 0.05f
            && Math.Abs(stackCard - 156f) < 0.05f
            && !TheatreSeatGeometry.TryReadNameOverflow(stacked, out _, out _, out _, out _);
        ok = ok && caught && stackCaught;
        Console.WriteLine((ok ? "PASS" : "FAIL") + " dump round-trip plate="
            + plateWidth.ToString("0.0", CultureInfo.InvariantCulture)
            + "x" + plateHeight.ToString("0.0", CultureInfo.InvariantCulture)
            + " " + ratio + " notch=" + notch + " " + locale + " reserve=" + reserve
            + " dirty=" + (dirtyRejected ? "rejected" : "accepted"));
        return ok;
    }

    private static void WriteCardCentres()
    {
        string[] lines = TheatreSeatGeometry.CardCentreLines;
        if (lines == null)
            return;
        for (int i = 0; i < lines.Length; i++)
        {
            if (!string.IsNullOrEmpty(lines[i]))
                Console.WriteLine(lines[i]);
        }
    }

    /// <summary>
    /// A plain rectangle, an 8-point chamfer, a sampled rectangle, a wide flat hex,
    /// and a rectangle with one jagged side all fail. An equal-sided hex passes.
    /// The same predicate is applied to every drawn coast inside CollectViolations.
    /// </summary>
    /// <summary>
    /// 4:3 English CC and IB grow a two-line title. That growth has to stay in the
    /// top bar. Parking it on the bottom is the blank black painting.
    /// </summary>
    private static bool CardArtBandsHold()
    {
        bool ok = true;
        ok &= ArtBand("4:3 CC", 132f, 156f, 8f, 52f, 36f);
        ok &= ArtBand("4:3 IB", 144f, 156f, 8f, 52f, 36f);
        ok &= ArtBand("4:3 short", 124f, 140f, 8f, 46f, 44f);
        ok &= ArtBand("grown title", 132f, 156f, 8f, 120f, 44f);
        Console.WriteLine((ok ? "PASS" : "FAIL") + " card art bands");
        return ok;
    }

    private static bool ArtBand(string label, float cardW, float cardH, float titleTop, float titleNeed, float statusNeed)
    {
        TheatreSeatGeometry.ZoneCardBands bands = TheatreSeatGeometry.LayoutZoneCardBands(
            cardW, cardH, titleTop, titleNeed, statusNeed);
        bool inside = TheatreSeatGeometry.ZoneCardArtInside(bands, cardW, cardH);
        if (inside)
            return true;
        Console.WriteLine("FAIL card art " + label
            + " title=" + bands.TitleY.ToString("0.0", CultureInfo.InvariantCulture)
            + "+" + bands.TitleH.ToString("0.0", CultureInfo.InvariantCulture)
            + " art=" + bands.ArtY.ToString("0.0", CultureInfo.InvariantCulture)
            + "+" + bands.ArtH.ToString("0.0", CultureInfo.InvariantCulture)
            + " status=" + bands.StatusY.ToString("0.0", CultureInfo.InvariantCulture)
            + "+" + bands.StatusH.ToString("0.0", CultureInfo.InvariantCulture));
        return false;
    }

    private static bool RectangularTerritoryRejected()
    {
        var rectangle = new[]
        {
            new TheatreSeatGeometry.Vec2(10f, 20f),
            new TheatreSeatGeometry.Vec2(310f, 20f),
            new TheatreSeatGeometry.Vec2(310f, 220f),
            new TheatreSeatGeometry.Vec2(10f, 220f)
        };
        const float chamfer = 4f;
        float cx = 160f;
        float cy = 120f;
        float halfW = 150f;
        float halfH = 100f;
        var clipped = new[]
        {
            new TheatreSeatGeometry.Vec2(cx - halfW + chamfer, cy - halfH),
            new TheatreSeatGeometry.Vec2(cx + halfW - chamfer, cy - halfH),
            new TheatreSeatGeometry.Vec2(cx + halfW, cy - halfH + chamfer),
            new TheatreSeatGeometry.Vec2(cx + halfW, cy + halfH - chamfer),
            new TheatreSeatGeometry.Vec2(cx + halfW - chamfer, cy + halfH),
            new TheatreSeatGeometry.Vec2(cx - halfW + chamfer, cy + halfH),
            new TheatreSeatGeometry.Vec2(cx - halfW, cy + halfH - chamfer),
            new TheatreSeatGeometry.Vec2(cx - halfW, cy - halfH + chamfer)
        };
        float dx = halfH / 1.7320508f;
        var hex = new[]
        {
            new TheatreSeatGeometry.Vec2(cx - halfW + dx, cy - halfH),
            new TheatreSeatGeometry.Vec2(cx + halfW - dx, cy - halfH),
            new TheatreSeatGeometry.Vec2(cx + halfW, cy),
            new TheatreSeatGeometry.Vec2(cx + halfW - dx, cy + halfH),
            new TheatreSeatGeometry.Vec2(cx - halfW + dx, cy + halfH),
            new TheatreSeatGeometry.Vec2(cx - halfW, cy)
        };
        var sampled = new TheatreSeatGeometry.Vec2[24];
        for (int i = 0; i < 6; i++)
            sampled[i] = new TheatreSeatGeometry.Vec2(10f + 300f * (i / 6f), 20f);
        for (int i = 0; i < 6; i++)
            sampled[6 + i] = new TheatreSeatGeometry.Vec2(310f, 20f + 200f * (i / 6f));
        for (int i = 0; i < 6; i++)
            sampled[12 + i] = new TheatreSeatGeometry.Vec2(310f - 300f * (i / 6f), 220f);
        for (int i = 0; i < 6; i++)
            sampled[18 + i] = new TheatreSeatGeometry.Vec2(10f, 220f - 200f * (i / 6f));
        var equal = new TheatreSeatGeometry.Vec2[6];
        const float radius = 90f;
        for (int i = 0; i < 6; i++)
        {
            double angle = (60.0 * i + 30.0) * Math.PI / 180.0;
            equal[i] = new TheatreSeatGeometry.Vec2(
                cx + (float)(Math.Cos(angle) * radius),
                cy + (float)(Math.Sin(angle) * radius));
        }

        var jagged = new[]
        {
            new TheatreSeatGeometry.Vec2(10f, 220f),
            new TheatreSeatGeometry.Vec2(310f, 220f),
            new TheatreSeatGeometry.Vec2(310f, 20f),
            new TheatreSeatGeometry.Vec2(270f, 46f),
            new TheatreSeatGeometry.Vec2(230f, 18f),
            new TheatreSeatGeometry.Vec2(190f, 50f),
            new TheatreSeatGeometry.Vec2(150f, 16f),
            new TheatreSeatGeometry.Vec2(110f, 44f),
            new TheatreSeatGeometry.Vec2(70f, 18f),
            new TheatreSeatGeometry.Vec2(10f, 20f)
        };
        bool rectFails = TheatreSeatGeometry.TerritoryReadsRectangular(rectangle);
        bool chamferFails = TheatreSeatGeometry.TerritoryReadsRectangular(clipped);
        bool sampledFails = TheatreSeatGeometry.TerritoryReadsRectangular(sampled);
        bool wideFlatFails = TheatreSeatGeometry.TerritoryReadsRectangular(hex);
        bool jaggedFails = TheatreSeatGeometry.TerritoryReadsRectangular(jagged);
        bool equalPasses = !TheatreSeatGeometry.TerritoryReadsRectangular(equal);
        bool ok = rectFails && chamferFails && sampledFails && wideFlatFails && jaggedFails && equalPasses;
        Console.WriteLine((ok ? "PASS" : "FAIL") + " rectangular coast"
            + " rect=" + TheatreSeatGeometry.EdgeIrregularity(rectangle).ToString("0.000", CultureInfo.InvariantCulture)
            + " chamfer=" + TheatreSeatGeometry.EdgeIrregularity(clipped).ToString("0.000", CultureInfo.InvariantCulture)
            + " sampled=" + TheatreSeatGeometry.EdgeIrregularity(sampled).ToString("0.000", CultureInfo.InvariantCulture)
            + " wide=" + TheatreSeatGeometry.LongestStraightRun(hex).ToString("0.000", CultureInfo.InvariantCulture)
            + " jagged=" + TheatreSeatGeometry.LongestStraightRun(jagged).ToString("0.000", CultureInfo.InvariantCulture)
            + " equal=" + TheatreSeatGeometry.LongestStraightRun(equal).ToString("0.000", CultureInfo.InvariantCulture)
            + " floor=" + TheatreSeatGeometry.EdgeIrregularityFloor.ToString("0.00", CultureInfo.InvariantCulture)
            + " run=" + TheatreSeatGeometry.StraightRunCeiling.ToString("0.00", CultureInfo.InvariantCulture));
        return ok;
    }

    /// <summary>
    /// P1's 4:3 map is 1079.6. The detail reserve stays on for every district, and the
    /// seats still have to clear the existing area, halo gap, and cue floors.
    /// </summary>
    private static bool FourByThreeReserveHolds()
    {
        const float plateWidth = 1079.6f;
        const float plateHeight = 844f;
        float areaScale = TheatreSeatGeometry.AreaFloorScale(plateWidth, plateHeight, true);
        float areaFloor = TheatreSeatGeometry.DistrictAreaFloor(plateWidth, plateHeight, true);
        float cardFloor = TheatreSeatGeometry.CardAreaFloor(plateWidth, plateHeight, true);
        Console.WriteLine(TheatreSeatGeometry.FormatAreaFloor("4:3", TheatreSeatGeometry.DistrictAreaFloorBase, areaScale, areaFloor, false));
        Console.WriteLine(TheatreSeatGeometry.FormatAreaFloor("4:3", TheatreSeatGeometry.CardAreaFloorBase, areaScale, cardFloor, true));
        bool ccOn = TheatreSeatGeometry.DetailReserveOn(plateWidth, plateHeight, false, true, true, "CC");
        bool mpOn = TheatreSeatGeometry.DetailReserveOn(plateWidth, plateHeight, true, true, true, "MP");
        bool underFloor = !TheatreSeatGeometry.DetailReserveOn(900f, 700f, false, true, true, "CC");
        bool ok = ccOn && mpOn && underFloor;
        if (!ok)
            Console.WriteLine("FAIL 4:3 reserve rule cc=" + ccOn + " mp=" + mpOn + " underFloorSkipped=" + underFloor);
        foreach (bool fa in new[] { false, true })
        {
            foreach (string map in TheatreSeatGeometry.MapIds)
            {
                TheatreSeatGeometry.SetShot("4:3", "none", fa ? "fa" : "en", true, 1440f, 1080f, 0f, 0f, 0f, 0f);
                TheatreSeatGeometry.ShotSelected = map;
                bool reserved = TheatreSeatGeometry.DetailReserveOn(plateWidth, plateHeight, fa, true, true, map);
                TheatreSeatGeometry.NoteDetailReserve(reserved);
                TheatreSeatGeometry.Layout layout = TheatreSeatGeometry.Arrange(plateWidth, plateHeight, fa, true, map);
                string[] violations = TheatreSeatGeometry.CollectViolations(plateWidth, plateHeight, fa, true, map);
                float minGap = float.MaxValue;
                float minArea = float.MaxValue;
                string shortArea = "";
                string tightPair = "";
                if (layout.Seats != null)
                {
                    var hexes = new TheatreSeatGeometry.Vec2[layout.Seats.Length][];
                    for (int i = 0; i < layout.Seats.Length; i++)
                    {
                        hexes[i] = layout.Seats[i].Hex;
                        float area = SeatArea(layout.Seats[i].Hex);
                        if (area < minArea)
                        {
                            minArea = area;
                            shortArea = layout.Seats[i].MapId;
                        }
                    }

                    for (int i = 0; i < layout.Seats.Length; i++)
                    {
                        for (int j = i + 1; j < layout.Seats.Length; j++)
                        {
                            float gap = TheatreSeatGeometry.DrawnDistrictGap(layout.Seats[i].Hex, layout.Seats[j].Hex);
                            if (gap < minGap)
                            {
                                minGap = gap;
                                tightPair = layout.Seats[i].MapId + "-" + layout.Seats[j].MapId;
                            }
                        }
                    }
                }

                bool cue = layout.Cue.W + 0.5f >= TheatreSeatGeometry.PackedCueMinWidth && layout.Cue.H + 0.5f >= 44f && layout.CueNear;
                bool gaps = minGap + 0.05f >= 12f;
                bool areaOk = minArea + 0.5f >= areaFloor;
                bool cueBound = CueAttemptsBounded("4:3", fa ? "fa" : "en", map, fa, layout.Cue, true);
                bool pass = reserved && layout.CardsLegal && layout.HexAspectLegal && violations.Length == 0 && cue && gaps && areaOk && cueBound;
                if (!pass)
                    ok = false;
                if (!string.IsNullOrEmpty(TheatreSeatGeometry.CompactNote))
                    Console.WriteLine("  compact " + TheatreSeatGeometry.CompactNote);
                Console.WriteLine((pass ? "PASS" : "FAIL") + " 4:3 reserve " + (fa ? "fa" : "en") + " " + map
                    + " reserved=" + reserved
                    + " legal=" + layout.CardsLegal
                    + " cue=" + layout.Cue.W.ToString("0", CultureInfo.InvariantCulture)
                    + "x" + layout.Cue.H.ToString("0", CultureInfo.InvariantCulture)
                    + " gap=" + tightPair + ":" + minGap.ToString("0.0", CultureInfo.InvariantCulture)
                    + " area=" + shortArea + ":" + minArea.ToString("0", CultureInfo.InvariantCulture)
                    + " violations=" + violations.Length);
                WriteCardCentres();
                if (!gaps)
                    Console.WriteLine("  " + tightPair + " halo gap " + minGap.ToString("0.0", CultureInfo.InvariantCulture) + " is " + (12f - minGap).ToString("0.0", CultureInfo.InvariantCulture) + " under 12");
                if (!areaOk)
                    Console.WriteLine("  " + shortArea + " area " + minArea.ToString("0", CultureInfo.InvariantCulture) + " is " + (areaFloor - minArea).ToString("0", CultureInfo.InvariantCulture) + " under " + areaFloor.ToString("0", CultureInfo.InvariantCulture));
                if (map == "DB" && layout.Seats != null)
                    WriteReserveTable(layout, fa, areaFloor);
                if (!cue)
                    Console.WriteLine("  cue " + layout.Cue.W.ToString("0.0", CultureInfo.InvariantCulture) + "x" + layout.Cue.H.ToString("0.0", CultureInfo.InvariantCulture) + " near=" + layout.CueNear);
                for (int i = 0; i < violations.Length && i < 4; i++)
                    Console.WriteLine("  " + violations[i]);
            }
        }

        Console.WriteLine((ok ? "PASS" : "FAIL") + " 4:3 reserve panel");
        return ok;
    }

    private static float SeatArea(TheatreSeatGeometry.Vec2[] shape)
    {
        if (shape == null || shape.Length < 3)
            return 0f;
        float sum = 0f;
        for (int i = 0; i < shape.Length; i++)
        {
            TheatreSeatGeometry.Vec2 a = shape[i];
            TheatreSeatGeometry.Vec2 b = shape[(i + 1) % shape.Length];
            sum += a.X * b.Y - b.X * a.Y;
        }

        return Math.Abs(sum) * 0.5f;
    }

    private static void WriteReserveTable(TheatreSeatGeometry.Layout layout, bool fa, float areaFloor)
    {
        Console.WriteLine("TABLE 4:3 " + (fa ? "fa" : "en") + " map=1079.6 floor=" + areaFloor.ToString("0.0", CultureInfo.InvariantCulture));
        for (int i = 0; i < layout.Seats.Length; i++)
        {
            float nearest = float.MaxValue;
            string other = "";
            for (int j = 0; j < layout.Seats.Length; j++)
            {
                if (j == i)
                    continue;
                float gap = TheatreSeatGeometry.DrawnDistrictGap(layout.Seats[i].Hex, layout.Seats[j].Hex);
                if (gap < nearest)
                {
                    nearest = gap;
                    other = layout.Seats[j].MapId;
                }
            }

            TheatreSeatGeometry.Box card = layout.Seats[i].Card;
            float area = SeatArea(layout.Seats[i].Hex);
            Console.WriteLine("  " + layout.Seats[i].MapId
                + " halo=" + other + ":" + nearest.ToString("0.0", CultureInfo.InvariantCulture)
                + " area=" + area.ToString("0", CultureInfo.InvariantCulture)
                + " floor=" + areaFloor.ToString("0.0", CultureInfo.InvariantCulture)
                + " edge=" + TheatreSeatGeometry.EdgeIrregularity(layout.Seats[i].Hex).ToString("0.00", CultureInfo.InvariantCulture)
                + " card=" + card.W.ToString("0", CultureInfo.InvariantCulture) + "x" + card.H.ToString("0", CultureInfo.InvariantCulture)
                + " cue=" + layout.Cue.W.ToString("0", CultureInfo.InvariantCulture) + "x" + layout.Cue.H.ToString("0", CultureInfo.InvariantCulture));
        }
    }

    private static bool NoteAreaFloors()
    {
        NoteOneAreaFloor("16:9", 1920f, 1080f, false);
        NoteOneAreaFloor("20:9", 2400f, 1080f, false);
        NoteOneAreaFloor("4:3", 1440f, 1080f, true);
        return true;
    }

    private static void NoteOneAreaFloor(string ratio, float canvasWidth, float canvasHeight, bool reserveOn)
    {
        TheatreSeatGeometry.CaptureMapSize(canvasWidth, canvasHeight, out float plateWidth, out float plateHeight);
        float scale = TheatreSeatGeometry.AreaFloorScale(plateWidth, plateHeight, reserveOn);
        Console.WriteLine(TheatreSeatGeometry.FormatAreaFloor(
            ratio, TheatreSeatGeometry.DistrictAreaFloorBase, scale, TheatreSeatGeometry.DistrictAreaFloor(plateWidth, plateHeight, reserveOn), false));
        Console.WriteLine(TheatreSeatGeometry.FormatAreaFloor(
            ratio, TheatreSeatGeometry.CardAreaFloorBase, scale, TheatreSeatGeometry.CardAreaFloor(plateWidth, plateHeight, reserveOn), true));
    }

    /// <summary>
    /// The zoom-cue schedule is the same walk the theatre runs. A miss stops at the
    /// schedule, which is under the cap, and the same inputs return the same count.
    /// A candidate that fits stops on that step. 4:3 reserve-on at 1079.6 is logged.
    /// </summary>
    private static bool CueAttemptsBounded(
        string ratio,
        string locale,
        string selected,
        bool fa,
        TheatreSeatGeometry.Box cue,
        bool logPlate)
    {
        float boxW = cue.W > 8f ? cue.W : 160f;
        float boxH = cue.H > 8f ? cue.H : (fa ? 44f : 36f);
        int scheduled = TheatreSeatGeometry.ZoomCueSchedule(fa, boxW, boxH).Length;
        int miss = TheatreSeatGeometry.WalkZoomCueAttempts(fa, boxW, boxH, _ => false, out bool missFits, out string why, out _);
        int again = TheatreSeatGeometry.WalkZoomCueAttempts(fa, boxW, boxH, _ => false, out bool againFits, out string againWhy, out _);
        int hit = TheatreSeatGeometry.WalkZoomCueAttempts(fa, boxW, boxH, _ => true, out bool hitFits, out _, out _);
        _cuePlates++;
        if (miss > _cueWorstMiss)
            _cueWorstMiss = miss;
        if (hit > _cueWorstHit)
            _cueWorstHit = hit;
        bool ok = scheduled > 0
            && scheduled <= TheatreSeatGeometry.ZoomCueAttemptCap
            && miss == scheduled
            && miss == again
            && !missFits
            && !againFits
            && why == againWhy
            && hit == 1
            && hitFits
            && miss <= TheatreSeatGeometry.ZoomCueAttemptCap;
        if (logPlate || !ok)
        {
            Console.WriteLine("[ZoomCueAttempt] ratio=" + ratio
                + " locale=" + locale
                + " sel=" + selected
                + " box=" + boxW.ToString("0.0", CultureInfo.InvariantCulture)
                + "x" + boxH.ToString("0.0", CultureInfo.InvariantCulture)
                + " miss=" + miss.ToString(CultureInfo.InvariantCulture)
                + " hit=" + hit.ToString(CultureInfo.InvariantCulture)
                + " cap=" + TheatreSeatGeometry.ZoomCueAttemptCap.ToString(CultureInfo.InvariantCulture)
                + " why=" + why
                + (logPlate ? " map=1079.6" : ""));
        }

        if (!ok)
            Console.WriteLine("FAIL zoom cue attempts " + ratio + " " + locale + " " + selected
                + " miss=" + miss.ToString(CultureInfo.InvariantCulture)
                + " hit=" + hit.ToString(CultureInfo.InvariantCulture));
        return ok;
    }

    /// <summary>
    /// Live map x and briefing x follow the measured column. Programmer 1's 334.4
    /// briefing puts the 1079.6 map 40px left of the old 374.4 frame. A 10px gap
    /// miss is outside the 2px tolerance.
    /// </summary>
    private static bool NoteLivePanelRects()
    {
        bool ok = true;
        ok &= NoteLivePanelRect("4:3", "en", 1440f, 1080f, 334.4f, 1079.6f, true, true, 346.4f, 14f);
        ok &= NoteLivePanelRect("4:3", "fa", 1440f, 1080f, 334.4f, 1079.6f, true, false, 14f, 1091.6f);
        float wideBrief = TheatreSeatGeometry.BriefingColumnWidth(1920f, 1080f);
        TheatreSeatGeometry.CaptureMapSize(1920f, 1080f, out float wideMap, out _);
        ok &= NoteLivePanelRect("16:9", "en", 1920f, 1080f, wideBrief, wideMap, true, true, 14f + wideBrief - 2f, 14f);
        ok &= NoteLivePanelRect("16:9", "fa", 1920f, 1080f, wideBrief, wideMap, true, false, 14f, 14f + wideMap - 2f);
        float ultraBrief = TheatreSeatGeometry.BriefingColumnWidth(2400f, 1080f);
        TheatreSeatGeometry.CaptureMapSize(2400f, 1080f, out float ultraMap, out _);
        ok &= NoteLivePanelRect("20:9", "en", 2400f, 1080f, ultraBrief, ultraMap, true, true, 14f + ultraBrief - 2f, 14f);
        ok &= NoteLivePanelRect("20:9", "fa", 2400f, 1080f, ultraBrief, ultraMap, true, false, 14f, 14f + ultraMap - 2f);
        float slipped = 346.4f + 10f;
        ok &= Math.Abs(slipped - 346.4f) > TheatreSeatGeometry.MapPanelWidthTolerance;
        if (!ok)
            Console.WriteLine("FAIL live panel rect");
        else
            Console.WriteLine("PASS live panel rect");
        return ok;
    }

    private static bool NoteLivePanelRect(
        string ratio,
        string locale,
        float canvasWidth,
        float canvasHeight,
        float briefingSpan,
        float mapSpan,
        bool theatre,
        bool briefingOnLeft,
        float expectMapX,
        float expectBriefingX)
    {
        TheatreSeatGeometry.ExpectedLivePanels(
            canvasWidth,
            canvasHeight,
            briefingSpan,
            mapSpan,
            theatre,
            briefingOnLeft,
            out float mapX,
            out float briefingX,
            out _,
            out _);
        bool match = Math.Abs(mapX - expectMapX) <= TheatreSeatGeometry.MapPanelWidthTolerance
            && Math.Abs(briefingX - expectBriefingX) <= TheatreSeatGeometry.MapPanelWidthTolerance;
        if (briefingOnLeft
            && Math.Abs(briefingSpan - 334.4f) < 0.05f
            && Math.Abs(mapX - 386.4f) <= TheatreSeatGeometry.MapPanelWidthTolerance)
        {
            Console.WriteLine("FAIL live map x is the frame value 386.4");
            match = false;
        }

        Console.WriteLine("[MapPanelRect] ratio=" + ratio
            + " locale=" + locale
            + " mapX=" + mapX.ToString("0.0", CultureInfo.InvariantCulture)
            + " expected=" + expectMapX.ToString("0.0", CultureInfo.InvariantCulture)
            + " briefingX=" + briefingX.ToString("0.0", CultureInfo.InvariantCulture)
            + " expected=" + expectBriefingX.ToString("0.0", CultureInfo.InvariantCulture)
            + " briefingW=" + briefingSpan.ToString("0.0", CultureInfo.InvariantCulture)
            + " mapW=" + mapSpan.ToString("0.0", CultureInfo.InvariantCulture)
            + " theatre=" + (theatre ? "1" : "0")
            + " briefingOnLeft=" + (briefingOnLeft ? "1" : "0"));
        if (!match)
            Console.WriteLine("FAIL live rect " + ratio + " mapX=" + mapX.ToString("0.0") + " briefingX=" + briefingX.ToString("0.0"));
        return match;
    }

    /// <summary>
    /// One log line per ratio and language. The live width is the safe width minus the
    /// measured columns and the gutters. A 4:3 map under the floor fails even when it
    /// matches that formula. The 2px tolerance is the existing panel slack.
    /// </summary>
    private static bool MapPanelWidthsHold()
    {
        bool ok = true;
        ok &= NoteMapPanelWidth("4:3", 1440f, 1080f);
        ok &= NoteMapPanelWidth("16:9", 1920f, 1080f);
        ok &= NoteMapPanelWidth("20:9", 2400f, 1080f);
        ok &= NoteAreaFloors();
        ok &= NoteLivePanelRects();
        ok &= MapPanelWidthRuleHolds();
        Console.WriteLine((ok ? "PASS" : "FAIL") + " map panel width");
        return ok;
    }

    private static bool NoteMapPanelWidth(string ratio, float canvasWidth, float canvasHeight)
    {
        TheatreSeatGeometry.CaptureMapSize(canvasWidth, canvasHeight, out float actual, out _);
        bool ok = true;
        foreach (bool fa in new[] { false, true })
        {
            float column = TheatreSeatGeometry.BriefingColumnWidth(canvasWidth, canvasHeight);
            float left = fa ? 0f : column;
            float right = fa ? column : 0f;
            int sides = column > 1f ? 1 : 0;
            float gutters = TheatreSeatGeometry.MapPanelGutters(sides, true);
            float expected = TheatreSeatGeometry.ExpectedMapPanelWidth(canvasWidth, left, right, true);
            float floor = TheatreSeatGeometry.MapPanelWidthFloor(canvasWidth);
            bool fourByThree = TheatreSeatGeometry.MapPanelWidthFourByThree(canvasWidth, canvasHeight);
            Console.WriteLine(TheatreSeatGeometry.FormatMapPanelWidth(
                ratio, fa ? "fa" : "en", actual, expected, left, right, gutters, floor));
            if (!TheatreSeatGeometry.MapPanelWidthHolds(actual, expected, floor, fourByThree, out string failure))
            {
                Console.WriteLine("FAIL " + failure);
                ok = false;
            }
        }

        return ok;
    }

    private static bool MapWidthMatchesColumns(float canvasWidth, float canvasHeight, float actual)
    {
        float column = TheatreSeatGeometry.BriefingColumnWidth(canvasWidth, canvasHeight);
        float expected = TheatreSeatGeometry.ExpectedMapPanelWidth(canvasWidth, column, 0f, true);
        float floor = TheatreSeatGeometry.MapPanelWidthFloor(canvasWidth);
        bool fourByThree = TheatreSeatGeometry.MapPanelWidthFourByThree(canvasWidth, canvasHeight);
        return TheatreSeatGeometry.MapPanelWidthHolds(actual, expected, floor, fourByThree, out _);
    }

    /// <summary>
    /// The column formula still rejects a map that is not the measured remainder, and a
    /// 4:3 map under the floor. A missing side stays zero. An extra column costs its width
    /// plus one more gap.
    /// </summary>
    private static bool MapPanelWidthRuleHolds()
    {
        const float canvas = 1440f;
        float floor = TheatreSeatGeometry.MapPanelWidthFloor(canvas);
        float tip = TheatreSeatGeometry.ExpectedMapPanelWidth(canvas, 374.4f, 0f, true);
        float narrowed = TheatreSeatGeometry.ExpectedMapPanelWidth(canvas, 334.4f, 0f, true);
        float withRight = TheatreSeatGeometry.ExpectedMapPanelWidth(canvas, 334.4f, 200f, true);
        float guttersTwo = TheatreSeatGeometry.MapPanelGutters(2, true);
        bool ok = Math.Abs(tip - 1039.6f) < 0.05f
            && Math.Abs(narrowed - 1079.6f) < 0.05f
            && Math.Abs(withRight - (canvas - 334.4f - 200f - guttersTwo)) < 0.05f
            && withRight < narrowed - 14f
            && Math.Abs(TheatreSeatGeometry.ExpectedMapPanelWidth(canvas, 374.4f, 0.4f, true) - tip) < 0.05f
            && !TheatreSeatGeometry.MapPanelWidthHolds(1079.6f, 1039.6f, floor, true, out _)
            && TheatreSeatGeometry.MapPanelWidthHolds(1039.6f, 1041.6f, floor, true, out _)
            && !TheatreSeatGeometry.MapPanelWidthHolds(1039.6f, 1041.8f, floor, true, out _)
            && !TheatreSeatGeometry.MapPanelWidthHolds(floor - 1f, floor - 1f, floor, true, out _)
            && TheatreSeatGeometry.MapPanelWidthHolds(floor, floor, floor, true, out _)
            && TheatreSeatGeometry.MapPanelWidthHolds(floor - 1f, floor - 1f, floor, false, out _)
            && Math.Abs(floor - 1020f) < 0.05f
            && 1039.6f + 0.01f >= floor;
        if (!ok)
            Console.WriteLine("FAIL map panel width rule tip="
                + tip.ToString("0.0", CultureInfo.InvariantCulture)
                + " narrowed=" + narrowed.ToString("0.0", CultureInfo.InvariantCulture)
                + " withRight=" + withRight.ToString("0.0", CultureInfo.InvariantCulture)
                + " floor=" + floor.ToString("0.0", CultureInfo.InvariantCulture));
        return ok;
    }

    private static bool ReplayPlateHolds()
    {
        TheatreSeatGeometry.CaptureMapSize(1440f, 1080f, out float liveWidth, out float liveHeight);
        TheatreSeatGeometry.SetShot("4:3", "none", "en", false, 1440f, 1080f, 0f, 0f, 0f, 0f);
        string json = TheatreSeatGeometry.FormatDump(1039.6f, 724f, default, default, null);
        bool read = TheatreSeatGeometry.TryReadDump(
            json, out float storedWidth, out float storedHeight, out _, out string ratio, out string notch, out _, out _);
        bool canvas = TheatreSeatGeometry.TryReadCanvas(
            json, out float canvasWidth, out float canvasHeight, out float left, out float top, out float right, out float bottom);
        TheatreSeatGeometry.ResolveReplayPlate(
            storedWidth, storedHeight, canvasWidth, canvasHeight, notch, left, top, right, bottom,
            out float plateWidth, out float plateHeight);
        bool ok = read && canvas && ratio == "4:3" && notch == "none"
            && Math.Abs(storedHeight - 724f) < 0.1f
            && Math.Abs(plateWidth - liveWidth) < 0.1f
            && Math.Abs(plateHeight - liveHeight) < 0.1f
            && plateHeight > 800f
            && MapWidthMatchesColumns(1440f, 1080f, liveWidth);
        Console.WriteLine((ok ? "PASS" : "FAIL") + " replay 4:3 plate "
            + plateWidth.ToString("0.0", CultureInfo.InvariantCulture)
            + "x" + plateHeight.ToString("0.0", CultureInfo.InvariantCulture)
            + " stored=" + storedHeight.ToString("0.0", CultureInfo.InvariantCulture));
        return ok;
    }

    /// <summary>
    /// 16:9 Farsi with Industrial Basin selected, and the same plate with Desert Base
    /// selected (the flow shot). The 206×56 pill has to clear Desert Base and stay off
    /// the selected title. Both cards have to sit inside the finished coast.
    /// </summary>
    private static bool ReplaySixteenByNineFaPill()
    {
        TheatreSeatGeometry.CaptureMapSize(1920f, 1080f, out float plateW, out float plateH);
        bool ib = ReplayFaPill("IB", plateW, plateH);
        bool db = ReplayFaPill("DB", plateW, plateH);
        bool ok = ib && db;
        Console.WriteLine((ok ? "PASS" : "FAIL") + " replay 16:9 fa pill IB=" + ib + " DB=" + db);
        return ok;
    }

    private static bool ReplayFaPill(string selected, float plateW, float plateH)
    {
        bool reserve = TheatreSeatGeometry.DetailReserveOn(plateW, plateH, true, plateW >= 600f, false, selected);
        TheatreSeatGeometry.NoteDetailReserve(reserve);
        TheatreSeatGeometry.SetShot("16:9", "none", "fa", reserve, 1920f, 1080f, 0f, 0f, 0f, 0f);
        TheatreSeatGeometry.ShotSelected = selected;
        TheatreSeatGeometry.Layout layout = TheatreSeatGeometry.Arrange(plateW, plateH, true, reserve, selected);
        if (layout.Seats == null)
            return false;
        var hexes = new TheatreSeatGeometry.Vec2[layout.Seats.Length][];
        var cards = new TheatreSeatGeometry.Box[layout.Seats.Length];
        int own = -1;
        int db = -1;
        int ib = -1;
        for (int i = 0; i < layout.Seats.Length; i++)
        {
            hexes[i] = layout.Seats[i].Hex;
            cards[i] = layout.Seats[i].Card;
            if (layout.Seats[i].MapId == selected)
                own = i;
            if (layout.Seats[i].MapId == "DB")
                db = i;
            if (layout.Seats[i].MapId == "IB")
                ib = i;
        }

        if (own < 0 || db < 0 || ib < 0)
            return false;
        TheatreSeatGeometry.Box pill = layout.Cue;
        TheatreSeatGeometry.Box card = cards[own];
        bool wide = pill.W + 0.5f >= 206f && pill.H + 0.5f >= 56f;
        bool onMap = pill.X >= 12f && pill.Y >= 12f
            && pill.X + pill.W <= plateW - 12f
            && pill.Y + pill.H <= plateH - 12f;
        // Desert Base must be the real 206×56 pill, inside the map. Industrial Basin
        // capture stays on the seated tab; a missing shelf still hangs the wide pill
        // above that card, and that hang has to clear Desert Base.
        if (wide && !onMap)
        {
            Console.WriteLine("FAIL replay 16:9 fa " + selected
                + " pill=" + pill.W.ToString("0", CultureInfo.InvariantCulture)
                + "x" + pill.H.ToString("0", CultureInfo.InvariantCulture)
                + " at=" + pill.X.ToString("0", CultureInfo.InvariantCulture)
                + "," + pill.Y.ToString("0", CultureInfo.InvariantCulture)
                + " not inside the map");
            return false;
        }

        bool parked = wide && onMap;
        if (selected == "DB" && !parked)
        {
            Console.WriteLine("FAIL replay 16:9 fa DB pill="
                + pill.W.ToString("0", CultureInfo.InvariantCulture)
                + "x" + pill.H.ToString("0", CultureInfo.InvariantCulture)
                + " at=" + pill.X.ToString("0", CultureInfo.InvariantCulture)
                + "," + pill.Y.ToString("0", CultureInfo.InvariantCulture));
            return false;
        }

        if (!parked)
        {
            float width = Math.Max(206f, card.W);
            float top = card.Y - 56f;
            float x = card.X + card.W - width;
            if (x > card.X)
                x = card.X;
            pill = new TheatreSeatGeometry.Box(x, top, width, 56f);
        }

        bool insideCard = pill.X >= card.X - 1f && pill.Y >= card.Y + 8f
            && pill.X + pill.W <= card.X + card.W + 1f
            && pill.Y + pill.H <= card.Y + card.H + 1f;
        bool offTitle = insideCard
            || pill.Y + pill.H <= card.Y + 1f
            || pill.X >= card.X + card.W - 1f
            || pill.X + pill.W <= card.X + 1f;
        TheatreSeatGeometry.Box detail = TheatreSeatGeometry.DetailRect(plateW, plateH, true, reserve);
        TheatreSeatGeometry.Box hint = TheatreSeatGeometry.ZoomHintRect(plateW, plateH);
        bool inside = true;
        bool clear = true;
        for (int i = 0; i < layout.Seats.Length; i++)
        {
            var others = new TheatreSeatGeometry.Vec2[hexes.Length][];
            for (int j = 0; j < hexes.Length; j++)
            {
                if (j != i)
                    others[j] = hexes[j];
            }

            TheatreSeatGeometry.Vec2[] shape = TheatreSeatGeometry.FinishTerritory(
                hexes[i], cards[i], layout.Seats[i].Pin, layout.Seats[i].MapId, others, pill, plateW, plateH, detail, hint);
            if ((layout.Seats[i].MapId == "DB" || layout.Seats[i].MapId == "IB")
                && !TheatreSeatGeometry.CardInsideTerritory(cards[i], shape))
                inside = false;
            // A seater-parked pill has to clear its own stroke. The UI hang above a
            // card that has no shelf is the capture-clean case: it must clear Desert
            // Base, and it may sit on the selected district's own stroke.
            if (i == own && (insideCard || !parked))
                continue;
            if (!TheatreSeatGeometry.FarsiPillClearsDistrict(pill, shape, i == own))
                clear = false;
        }

        Console.WriteLine((inside && clear && offTitle ? "PASS" : "FAIL")
            + " replay 16:9 fa " + selected
            + " pill=" + pill.W.ToString("0", CultureInfo.InvariantCulture)
            + "x" + pill.H.ToString("0", CultureInfo.InvariantCulture)
            + " at=" + pill.X.ToString("0", CultureInfo.InvariantCulture)
            + "," + pill.Y.ToString("0", CultureInfo.InvariantCulture)
            + " title=" + offTitle
            + " clear=" + clear
            + " inside=" + inside);
        return inside && clear && offTitle && layout.CardsLegal && layout.HexAspectLegal;
    }

    /// <summary>
    /// 4:3 fa IB from the Unity seat dump. The dump records the card Unity drew
    /// (124×156), the name box (112) and the wrapped floor (42), plus the glow gaps
    /// that were negative. Reseating that plate has to open every drawn gap to 12px,
    /// keep the card inside its hex, fit the name, and leave 8px under the cue.
    /// </summary>
    private static bool ReplayFourByThreeFaIb()
    {
        const string dumpName = "seat-4x3-section-fa-reserve-off-plate-1111.0x705.0-sel-IB.json";
        string path = Path.Combine(ResolveReplayRoot(), dumpName);
        if (!File.Exists(path))
        {
            Console.WriteLine("FAIL replay 4:3 fa IB missing " + dumpName);
            return false;
        }

        string json = File.ReadAllText(path);
        bool read = TheatreSeatGeometry.TryReadDump(
            json, out float storedW, out float storedH, out bool reserve, out string ratio, out string notch, out string locale, out string selected);
        bool canvas = TheatreSeatGeometry.TryReadCanvas(
            json, out float canvasW, out float canvasH, out float left, out float top, out float right, out float bottom);
        bool unityCard = json.IndexOf("\"cardW\": 124.0", StringComparison.Ordinal) >= 0
            && json.IndexOf("\"cardH\": 156.0", StringComparison.Ordinal) >= 0
            && json.IndexOf("\"boxW\": 112.0", StringComparison.Ordinal) >= 0
            && json.IndexOf("\"preferredH\": 42.0", StringComparison.Ordinal) >= 0
            && json.IndexOf("\"DB-MP\"", StringComparison.Ordinal) >= 0
            && json.IndexOf("-27.0", StringComparison.Ordinal) >= 0
            && json.IndexOf("-31.0", StringComparison.Ordinal) >= 0;
        if (!read || !canvas || !unityCard || ratio != "4:3" || locale != "fa" || selected != "IB")
        {
            Console.WriteLine("FAIL replay 4:3 fa IB dump");
            return false;
        }

        TheatreSeatGeometry.ResolveReplayPlate(
            storedW, storedH, canvasW, canvasH, notch, left, top, right, bottom, out float plateW, out float plateH);
        bool applied = TheatreSeatGeometry.DetailReserveOn(plateW, plateH, true, reserve, true, "IB");
        TheatreSeatGeometry.NoteDetailReserve(applied);
        TheatreSeatGeometry.SetShot(ratio, notch, locale, applied, canvasW, canvasH, left, top, right, bottom);
        TheatreSeatGeometry.ShotSelected = "IB";
        TheatreSeatGeometry.Layout layout = TheatreSeatGeometry.Arrange(plateW, plateH, true, applied, "IB");
        if (layout.Seats == null)
        {
            Console.WriteLine("FAIL replay 4:3 fa IB no seats");
            return false;
        }

        int ib = -1;
        var hexes = new TheatreSeatGeometry.Vec2[layout.Seats.Length][];
        var cards = new TheatreSeatGeometry.Box[layout.Seats.Length];
        var pins = new TheatreSeatGeometry.Box[layout.Seats.Length];
        for (int i = 0; i < layout.Seats.Length; i++)
        {
            hexes[i] = layout.Seats[i].Hex;
            cards[i] = layout.Seats[i].Card;
            pins[i] = layout.Seats[i].Pin;
            if (layout.Seats[i].MapId == "IB")
                ib = i;
        }

        if (ib < 0)
        {
            Console.WriteLine("FAIL replay 4:3 fa IB missing");
            return false;
        }

        TheatreSeatGeometry.Box seated = cards[ib];
        var restored = TheatreSeatGeometry.RestoreAuthoredCard(seated, 124f, 156f, hexes[ib], hexes);
        bool holds = TheatreSeatGeometry.SeatHolds(restored, hexes[ib], hexes);
        bool gaps = TheatreSeatGeometry.DrawnGlowGapsHold(hexes);
        string title = "حوضه صنعتی";
        float clearance = TheatreSeatGeometry.NameClearanceBelowCue(title, restored.W, restored.H, false);
        float drawnClearance = clearance * TheatreSeatGeometry.SectionDrawScale();
        bool nameFits = TheatreSeatGeometry.TryFitZoneName(
            title, restored.W, restored.H, restored.H, false, out _, out _, out _);
        var afterCards = (TheatreSeatGeometry.Box[])cards.Clone();
        afterCards[ib] = restored;
        var after = new System.Collections.Generic.List<string>();
        TheatreSeatGeometry.Box detail = TheatreSeatGeometry.DetailRect(plateW, plateH, true, applied);
        TheatreSeatGeometry.Box hint = TheatreSeatGeometry.ZoomHintRect(plateW, plateH);
        TheatreSeatGeometry.CollectViolations(
            "fa", plateW, plateH, "IB", hexes, afterCards, pins, layout.Cue, layout.Cue.W > 1f, detail, hint, true, after);
        bool stillStraddle = false;
        bool stillName = false;
        for (int i = 0; i < after.Count; i++)
        {
            if (after[i].IndexOf("ZoneCard_IB", StringComparison.Ordinal) >= 0
                && after[i].IndexOf("straddles", StringComparison.Ordinal) >= 0)
                stillStraddle = true;
            if (after[i].IndexOf("حوضه صنعتی", StringComparison.Ordinal) >= 0
                && after[i].IndexOf("draws past its box", StringComparison.Ordinal) >= 0)
                stillName = true;
        }

        bool ok = holds && gaps && nameFits && drawnClearance + 0.05f >= 8f && !stillStraddle && !stillName
            && layout.CardsLegal && layout.HexAspectLegal;
        Console.WriteLine((ok ? "PASS" : "FAIL") + " replay 4:3 fa IB dump"
            + " plate=" + plateW.ToString("0.0", CultureInfo.InvariantCulture)
            + "x" + plateH.ToString("0.0", CultureInfo.InvariantCulture)
            + " card=" + restored.W.ToString("0.0", CultureInfo.InvariantCulture)
            + "x" + restored.H.ToString("0.0", CultureInfo.InvariantCulture)
            + " holds=" + holds
            + " gaps=" + (gaps ? "12" : "short")
            + " name=" + (nameFits ? "fits" : "past")
            + " cueGap=" + drawnClearance.ToString("0.0", CultureInfo.InvariantCulture));
        return ok;
    }

    private static void ReplayDumps(ref int seated, ref int blocked, ref int failed)
    {
        string replayRoot = ResolveReplayRoot();
        if (!Directory.Exists(replayRoot))
        {
            Console.WriteLine("REPLAY files=0");
            return;
        }

        string[] files = Directory.GetFiles(replayRoot, "seat-*.json", SearchOption.AllDirectories);
        Console.WriteLine("REPLAY files=" + files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            string json = File.ReadAllText(files[i]);
            if (!TheatreSeatGeometry.TryReadDump(
                json, out float plateWidth, out float plateHeight, out bool reserve, out string ratio, out string notch, out string locale, out string selected))
            {
                failed++;
                Console.WriteLine("FAIL replay unreadable " + files[i]);
                continue;
            }

            bool fa = locale != null && locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            float replayWidth = plateWidth;
            float replayHeight = plateHeight;
            float canvasWidth = 0f;
            float canvasHeight = 0f;
            float insetLeft = 0f;
            float insetTop = 0f;
            float insetRight = 0f;
            float insetBottom = 0f;
            if (TheatreSeatGeometry.TryReadCanvas(
                json, out canvasWidth, out canvasHeight, out insetLeft, out insetTop, out insetRight, out insetBottom))
            {
                TheatreSeatGeometry.ResolveReplayPlate(
                    plateWidth, plateHeight, canvasWidth, canvasHeight, notch,
                    insetLeft, insetTop, insetRight, insetBottom,
                    out replayWidth, out replayHeight);
            }

            if (TheatreSeatGeometry.TryReadNameOverflow(
                json, out string overflowMap, out string overflowText, out float overflowPreferred, out float overflowBox))
            {
                failed++;
                Console.WriteLine("FAIL replay name " + Path.GetFileName(files[i])
                    + " map=" + overflowMap
                    + " preferred=" + overflowPreferred.ToString("0.0", CultureInfo.InvariantCulture)
                    + " width=" + overflowBox.ToString("0.0", CultureInfo.InvariantCulture)
                    + " text=" + overflowText);
            }

            if (TheatreSeatGeometry.TryReadNameStack(
                json, out string stackMap, out string stackText, out float stackName, out float stackStatus, out float stackCard))
            {
                failed++;
                Console.WriteLine("FAIL replay name stack " + Path.GetFileName(files[i])
                    + " map=" + stackMap
                    + " name=" + stackName.ToString("0.0", CultureInfo.InvariantCulture)
                    + " status=" + stackStatus.ToString("0.0", CultureInfo.InvariantCulture)
                    + " card=" + stackCard.ToString("0.0", CultureInfo.InvariantCulture)
                    + " text=" + stackText);
            }

            Evaluate(
                ratio,
                notch,
                fa,
                reserve,
                canvasWidth,
                canvasHeight,
                insetLeft,
                insetTop,
                insetRight,
                insetBottom,
                replayWidth,
                replayHeight,
                false,
                selected,
                ref seated,
                ref blocked,
                ref failed);
        }
    }

    /// <summary>
    /// The coast is finished, then the runtime attach may move a card. The inside
    /// check uses that later rect against the coast that was already drawn.
    /// </summary>
    private static bool ReplayRuntimeCards()
    {
        bool ok = true;
        ok &= ReplayRuntimePlate(1440f, 1080f, "4:3");
        ok &= ReplayRuntimePlate(1920f, 1080f, "16:9");
        Console.WriteLine((ok ? "PASS" : "FAIL") + " replay runtime cards");
        return ok;
    }

    private static bool ReplayRuntimePlate(float canvasW, float canvasH, string ratio)
    {
        TheatreSeatGeometry.CaptureMapSize(canvasW, canvasH, out float plateW, out float plateH);
        bool ok = true;
        foreach (bool fa in new[] { false, true })
        {
            foreach (string selected in new[] { "DB", "IB", "CC" })
            {
                bool reserve = TheatreSeatGeometry.DetailReserveOn(plateW, plateH, fa, true, ratio == "4:3", selected);
                TheatreSeatGeometry.NoteDetailReserve(reserve);
                TheatreSeatGeometry.SetShot(ratio, "none", fa ? "fa" : "en", reserve, canvasW, canvasH, 0f, 0f, 0f, 0f);
                TheatreSeatGeometry.ShotSelected = selected;
                TheatreSeatGeometry.Layout layout = TheatreSeatGeometry.Arrange(plateW, plateH, fa, reserve, selected);
                if (layout.Seats == null)
                    return false;
                var hexes = new TheatreSeatGeometry.Vec2[layout.Seats.Length][];
                var cards = new TheatreSeatGeometry.Box[layout.Seats.Length];
                var pins = new TheatreSeatGeometry.Box[layout.Seats.Length];
                int own = 0;
                for (int i = 0; i < layout.Seats.Length; i++)
                {
                    hexes[i] = layout.Seats[i].Hex;
                    cards[i] = layout.Seats[i].Card;
                    pins[i] = layout.Seats[i].Pin;
                    if (layout.Seats[i].MapId == selected)
                        own = i;
                }

                TheatreSeatGeometry.Box detail = TheatreSeatGeometry.DetailRect(plateW, plateH, fa, reserve);
                TheatreSeatGeometry.Box hint = TheatreSeatGeometry.ZoomHintRect(plateW, plateH);
                var coasts = new TheatreSeatGeometry.Vec2[layout.Seats.Length][];
                for (int i = 0; i < layout.Seats.Length; i++)
                {
                    var others = new TheatreSeatGeometry.Vec2[hexes.Length][];
                    for (int j = 0; j < hexes.Length; j++)
                    {
                        if (j != i)
                            others[j] = hexes[j];
                    }

                    coasts[i] = TheatreSeatGeometry.FinishTerritory(
                        hexes[i], cards[i], pins[i], layout.Seats[i].MapId, others, layout.Cue, plateW, plateH, detail, hint);
                }

                var snapshot = new TheatreSeatGeometry.Box[cards.Length];
                for (int i = 0; i < cards.Length; i++)
                    snapshot[i] = cards[i];
                var drawn = new TheatreSeatGeometry.Vec2[coasts.Length][];
                for (int i = 0; i < coasts.Length; i++)
                    drawn[i] = coasts[i] != null ? coasts[i] : hexes[i];
                bool attached = fa
                    ? TheatreSeatGeometry.TryAttachFarsiCue(plateW, plateH, pins, cards, drawn, detail, own, out TheatreSeatGeometry.Box seated)
                        || TheatreSeatGeometry.TryAttachCueToCard(plateW, plateH, pins, cards, drawn, detail, own, out seated)
                    : TheatreSeatGeometry.TryAttachCueToCard(plateW, plateH, pins, cards, drawn, detail, own, out seated);
                if (attached)
                {
                    for (int i = 0; i < cards.Length; i++)
                    {
                        if (coasts[i] == null || cards[i].W < 1f)
                            continue;
                        if (!TheatreSeatGeometry.CardInsideTerritory(cards[i], coasts[i]))
                            cards[i] = snapshot[i];
                    }
                }

                TheatreSeatGeometry.Box pill = layout.Cue;
                bool pillOk = true;
                if (selected == "DB" && ratio == "16:9")
                {
                    float minW = fa ? 206f : 176f;
                    float minH = fa ? 56f : 44f;
                    pillOk = pill.W + 0.5f >= minW && pill.H + 0.5f >= minH
                        && pill.X >= 12f && pill.Y >= 12f
                        && pill.X + pill.W <= plateW - 12f
                        && pill.Y + pill.H <= plateH - 12f
                        && TheatreSeatGeometry.FarsiPillClearsDistrict(pill, coasts[own], true);
                }

                bool inside = true;
                bool centred = true;
                for (int i = 0; i < layout.Seats.Length; i++)
                {
                    if (coasts[i] == null || !TheatreSeatGeometry.CardInsideTerritory(cards[i], coasts[i]))
                        inside = false;
                    if (layout.Seats[i].MapId != "DB" && layout.Seats[i].MapId != "CC")
                        continue;
                    BoundsCentre(coasts[i], out float cx, out float cy);
                    float dx = Math.Abs(cards[i].X + cards[i].W * 0.5f - cx);
                    float dy = Math.Abs(cards[i].Y + cards[i].H * 0.5f - cy);
                    if (dx > 36f || dy > 36f)
                        centred = false;
                }

                if (!pillOk || !inside || !centred)
                {
                    Console.WriteLine("FAIL runtime " + ratio + " " + (fa ? "fa" : "en") + " sel=" + selected
                        + " pill=" + pill.W.ToString("0") + "x" + pill.H.ToString("0")
                        + " at=" + pill.X.ToString("0") + "," + pill.Y.ToString("0")
                        + " pillOk=" + pillOk + " inside=" + inside + " centre=" + centred);
                    ok = false;
                }
            }
        }

        return ok;
    }

    private static void BoundsCentre(TheatreSeatGeometry.Vec2[] shape, out float cx, out float cy)
    {
        cx = 0f;
        cy = 0f;
        if (shape == null || shape.Length == 0)
            return;
        float minX = shape[0].X;
        float maxX = shape[0].X;
        float minY = shape[0].Y;
        float maxY = shape[0].Y;
        for (int i = 1; i < shape.Length; i++)
        {
            if (shape[i].X < minX) minX = shape[i].X;
            if (shape[i].X > maxX) maxX = shape[i].X;
            if (shape[i].Y < minY) minY = shape[i].Y;
            if (shape[i].Y > maxY) maxY = shape[i].Y;
        }

        cx = (minX + maxX) * 0.5f;
        cy = (minY + maxY) * 0.5f;
    }

    /// <summary>4:3 English footer. The Industrial Basin zoom label has to sit inside the button.</summary>
    private static bool ReplayZoomInLabel()
    {
        float width = TheatreSeatGeometry.FooterButtonWidth(1440f, 6);
        bool fits = TheatreSeatGeometry.ZoomInBasinFits(width, 16f);
        Console.WriteLine((fits ? "PASS" : "FAIL") + " replay 4:3 en zoom label width="
            + width.ToString("0.0", CultureInfo.InvariantCulture));
        return fits;
    }

    private static bool OutlinesHold(TheatreSeatGeometry.Layout layout, float width, float height)
    {
        if (layout.Seats == null)
            return false;
        for (int i = 0; i < layout.Seats.Length; i++)
        {
            if (TheatreSeatGeometry.OutlineInside(layout.Seats[i].Hex, width, height, out _, out _))
                continue;
            return false;
        }

        return true;
    }

}
