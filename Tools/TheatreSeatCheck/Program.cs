using System;
using System.Globalization;
using Game.UI.Runtime;

internal static class Program
{
    private static int Main()
    {
        var canvases = new[]
        {
            new[] { "16:9", "1920", "1080" },
            new[] { "20:9", "2400", "1080" },
            new[] { "4:3", "1440", "1080" }
        };
        int failed = 0;
        for (int p = 0; p < canvases.Length; p++)
        {
            float canvasWidth = float.Parse(canvases[p][1], CultureInfo.InvariantCulture);
            float canvasHeight = float.Parse(canvases[p][2], CultureInfo.InvariantCulture);
            TheatreSeatGeometry.CaptureMapSize(canvasWidth, canvasHeight, out float width, out float height);
            Console.WriteLine("map " + canvases[p][0]
                + " canvas=" + canvases[p][1] + "x" + canvases[p][2]
                + " plate=" + width.ToString("0.0", CultureInfo.InvariantCulture)
                + "x" + height.ToString("0.0", CultureInfo.InvariantCulture));
            for (int locale = 0; locale < 2; locale++)
            {
                bool fa = locale == 1;
                for (int reserve = 0; reserve < 2; reserve++)
                {
                    bool reserveOn = reserve == 1;
                    TheatreSeatGeometry.Layout layout = TheatreSeatGeometry.Arrange(
                        width, height, fa, reserveOn, "DB");
                    string name = canvases[p][0] + " h=" + height.ToString("0", CultureInfo.InvariantCulture)
                        + (fa ? " FA" : " EN") + (reserveOn ? " reserve" : " open");
                    bool outlines = OutlinesHold(layout, width, height, name);
                    bool ok = layout.CardsLegal && layout.CueNear && layout.HexAspectLegal && outlines && layout.Seats.Length == 5;
                    if (!ok)
                        failed++;
                    Console.WriteLine((ok ? "PASS " : "FAIL ") + name
                        + " cards=" + layout.CardsLegal
                        + " cue=" + layout.CueNear
                        + " aspect=" + layout.HexAspectLegal
                        + " outlines=" + outlines);
                    if (layout.LogLines != null)
                    {
                        for (int i = 0; i < layout.LogLines.Length; i++)
                            Console.WriteLine(layout.LogLines[i]);
                    }

                    if (!ok)
                    {
                        for (int i = 0; i < layout.Seats.Length; i++)
                        {
                            TheatreSeatGeometry.Vec2[] hex = layout.Seats[i].Hex;
                            float minX = hex[0].X, maxX = hex[0].X, minY = hex[0].Y, maxY = hex[0].Y;
                            for (int v = 1; v < hex.Length; v++)
                            {
                                if (hex[v].X < minX) minX = hex[v].X;
                                if (hex[v].X > maxX) maxX = hex[v].X;
                                if (hex[v].Y < minY) minY = hex[v].Y;
                                if (hex[v].Y > maxY) maxY = hex[v].Y;
                            }

                            TheatreSeatGeometry.Box pin = layout.Seats[i].Pin;
                            Console.WriteLine("  hex " + layout.Seats[i].MapId
                                + " aabb=" + minX.ToString("0") + "," + minY.ToString("0")
                                + " " + maxX.ToString("0") + "," + maxY.ToString("0")
                                + " pin=" + pin.Center.X.ToString("0.0") + "," + pin.Center.Y.ToString("0.0"));
                        }
                    }
                }
            }
        }

        Console.WriteLine(failed == 0
            ? "TheatreSeatCheck result=Passed"
            : "TheatreSeatCheck result=Failed count=" + failed);
        return failed == 0 ? 0 : 1;
    }

    private static bool OutlinesHold(TheatreSeatGeometry.Layout layout, float width, float height, string name)
    {
        if (layout.Seats == null)
            return false;
        bool ok = true;
        for (int i = 0; i < layout.Seats.Length; i++)
        {
            TheatreSeatGeometry.Vec2[] hex = layout.Seats[i].Hex;
            if (TheatreSeatGeometry.OutlineInside(hex, width, height, out float localX, out float localY))
                continue;
            ok = false;
            Console.WriteLine("  outline " + layout.Seats[i].MapId
                + " outside " + name
                + " local=" + localX.ToString("0.0", CultureInfo.InvariantCulture)
                + "," + localY.ToString("0.0", CultureInfo.InvariantCulture));
        }

        return ok;
    }
}
