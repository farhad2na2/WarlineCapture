using System;
using System.Globalization;
using System.IO;
using Game.UI.Runtime;

internal static class Program
{
    private const string ReplayRoot = "Design/AgentReports/SkirmishBattleLibrary/2026-10-14";

    private static int Main(string[] args)
    {
        int seated = 0;
        int blocked = 0;
        int failed = 0;
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
        if (!DrawnFramesHold() || !StampHolds() || !DumpRoundTrips() || !MirrorHolds() || !ReplayPlateHolds() || !ReplayFourByThreeFaIb())
            failed++;

        RunCanvasPlates(ref seated, ref blocked, ref failed);
        RunNotchCheckedPlates(ref seated, ref blocked, ref failed);
        RunFittedPlates(ref seated, ref blocked, ref failed);
        RunCompositionPlates(ref seated, ref blocked, ref failed);
        ReplayDumps(ref seated, ref blocked, ref failed);

        Console.WriteLine(
            "THRESHOLD ReserveViewportFloor "
            + TheatreSeatGeometry.ReserveViewportFloor.X.ToString("0", CultureInfo.InvariantCulture)
            + "x"
            + TheatreSeatGeometry.ReserveViewportFloor.Y.ToString("0", CultureInfo.InvariantCulture)
            + " skips the 4:3 reserve when either axis is under the floor.");
        if (failed == 0 && blocked == 0)
        {
            Console.WriteLine("TheatreSeatCheck result=Passed seated=" + seated + " blocked=0");
            return 0;
        }

        Console.WriteLine("TheatreSeatCheck result=Failed count=" + failed + " seated=" + seated + " blocked=" + blocked);
        return 1;
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
                            faults.Add(ratio + " interior " + part.Trim());
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
        const float referenceWidth = 1672f;
        const float referenceHeight = 941f;
        float scale = Math.Min(canvasWidth / referenceWidth, canvasHeight / referenceHeight);
        float extra = Math.Max(0f, canvasWidth / scale - referenceWidth);
        TheatreSeatGeometry.CaptureMapSize(referenceWidth + extra, referenceHeight, out float plateWidth, out float plateHeight);
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
        bool ok = layout.CardsLegal && layout.CueNear && layout.HexAspectLegal && outlines
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
        if (!ok)
        {
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
            && Math.Abs(liveWidth - 1039.6f) < 0.2f;
        Console.WriteLine((ok ? "PASS" : "FAIL") + " replay 4:3 plate "
            + plateWidth.ToString("0.0", CultureInfo.InvariantCulture)
            + "x" + plateHeight.ToString("0.0", CultureInfo.InvariantCulture)
            + " stored=" + storedHeight.ToString("0.0", CultureInfo.InvariantCulture));
        return ok;
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
        string path = Path.Combine(ReplayRoot, dumpName);
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
        if (!Directory.Exists(ReplayRoot))
        {
            Console.WriteLine("REPLAY files=0");
            return;
        }

        string[] files = Directory.GetFiles(ReplayRoot, "seat-*.json", SearchOption.AllDirectories);
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
