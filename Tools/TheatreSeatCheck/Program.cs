using System;
using System.Globalization;
using System.IO;
using Game.UI.Runtime;

internal static class Program
{
    private const string ReplayRoot = "Design/AgentReports/SkirmishBattleLibrary/2026-10-14";

    private static int Main()
    {
        int seated = 0;
        int blocked = 0;
        int failed = 0;
        if (!StampHolds() || !DumpRoundTrips())
            failed++;

        RunCanvasPlates(ref seated, ref blocked, ref failed);
        RunNotchCheckedPlates(ref seated, ref blocked, ref failed);
        RunFittedPlates(ref seated, ref blocked, ref failed);
        RunCompositionPlates(ref seated, ref blocked, ref failed);
        ReplayDumps(ref seated, ref blocked, ref failed);

        Console.WriteLine(
            "THRESHOLD propose leaving the reserve off at 4:3 when the open viewport is under "
            + TheatreSeatGeometry.ReserveViewportMinWidth.ToString("0", CultureInfo.InvariantCulture)
            + " wide or under "
            + TheatreSeatGeometry.ReserveViewportMinHeight.ToString("0", CultureInfo.InvariantCulture)
            + " tall. Wider plates still pack below that height. The reserve stays on until that is agreed.");
        if (failed == 0)
        {
            Console.WriteLine("TheatreSeatCheck result=Passed seated=" + seated + " blocked=" + blocked);
            return 0;
        }

        Console.WriteLine("TheatreSeatCheck result=Failed count=" + failed + " seated=" + seated + " blocked=" + blocked);
        return 1;
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

        for (int locale = 0; locale < 2; locale++)
        {
            bool fa = locale == 1;
            for (int reserve = 0; reserve < 2; reserve++)
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
                    notch == "none",
                    ref seated,
                    ref blocked,
                    ref failed);
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
        ref int seated,
        ref int blocked,
        ref int failed)
    {
        string locale = fa ? "fa" : "en";
        TheatreSeatGeometry.SetShot(ratio, notch, locale, reserveOn, canvasWidth, canvasHeight, left, top, right, bottom);
        TheatreSeatGeometry.Layout layout = TheatreSeatGeometry.Arrange(plateWidth, plateHeight, fa, reserveOn, "DB");
        string[] violations = TheatreSeatGeometry.CollectViolations(plateWidth, plateHeight, fa, reserveOn, "DB");
        bool outlines = OutlinesHold(layout, plateWidth, plateHeight);
        bool ok = layout.CardsLegal && layout.CueNear && layout.HexAspectLegal && outlines
            && layout.Seats != null && layout.Seats.Length == 5 && violations.Length == 0
            && FieldsHold(violations);
        TheatreSeatGeometry.Box view = TheatreSeatGeometry.OpenViewport(plateWidth, plateHeight, fa, reserveOn);
        bool below = BelowFloor(plateWidth, plateHeight, fa, reserveOn);
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
        else if (below)
        {
            blocked++;
            state = "BLOCK";
        }
        else
        {
            failed++;
            state = "FAIL";
        }

        Console.WriteLine(state + " " + ratio
            + " notch=" + notch
            + (fa ? " FA" : " EN")
            + (reserveOn ? " reserve=on" : " reserve=off")
            + " plate=" + plateWidth.ToString("0.0", CultureInfo.InvariantCulture)
            + "x" + plateHeight.ToString("0.0", CultureInfo.InvariantCulture)
            + " view=" + view.W.ToString("0.0", CultureInfo.InvariantCulture)
            + "x" + view.H.ToString("0.0", CultureInfo.InvariantCulture)
            + " cards=" + layout.CardsLegal
            + " cue=" + layout.CueNear
            + " aspect=" + layout.HexAspectLegal
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

    private static bool BelowFloor(float plateWidth, float plateHeight, bool fa, bool reserveOn)
    {
        if (reserveOn)
            return !TheatreSeatGeometry.ReserveViewportHolds(plateWidth, plateHeight, fa);
        return plateWidth + 0.5f < TheatreSeatGeometry.ReserveViewportMinWidth
            || plateHeight + 0.5f < TheatreSeatGeometry.ReserveViewportMinHeight;
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
                Text = "DESERT BASE",
                Font = 16f,
                BoxWidth = 112f,
                BoxHeight = 44f,
                PreferredWidth = 108f,
                PreferredHeight = 40f,
                Band = 212f,
                CardWidth = 156f,
                CardHeight = 208f
            }
        };
        string json = TheatreSeatGeometry.FormatDump(1039.6f, 724f, detail, view, names);
        bool read = TheatreSeatGeometry.TryReadDump(json, out float plateWidth, out float plateHeight, out bool reserve, out string ratio, out string notch, out string locale);
        bool ok = read
            && reserve
            && ratio == "4:3"
            && notch == "notch"
            && locale == "en"
            && Math.Abs(plateWidth - 1039.6f) < 0.05f
            && Math.Abs(plateHeight - 724f) < 0.05f
            && json.IndexOf("\"portraitMinW\": 124", StringComparison.Ordinal) >= 0
            && json.IndexOf("DESERT BASE", StringComparison.Ordinal) >= 0;
        Console.WriteLine((ok ? "PASS" : "FAIL") + " dump round-trip plate="
            + plateWidth.ToString("0.0", CultureInfo.InvariantCulture)
            + "x" + plateHeight.ToString("0.0", CultureInfo.InvariantCulture)
            + " " + ratio + " notch=" + notch + " " + locale + " reserve=" + reserve);
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
            if (!TheatreSeatGeometry.TryReadDump(json, out float plateWidth, out float plateHeight, out bool reserve, out string ratio, out string notch, out string locale))
            {
                failed++;
                Console.WriteLine("FAIL replay unreadable " + files[i]);
                continue;
            }

            bool fa = locale != null && locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            Evaluate(
                ratio,
                notch,
                fa,
                reserve,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                plateWidth,
                plateHeight,
                false,
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
