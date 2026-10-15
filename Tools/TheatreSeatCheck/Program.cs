using System;
using Game.UI.Runtime;

internal static class Program
{
    private static int Main()
    {
        var plates = new[]
        {
            new[] { "4:3", "1039.6", "620" },
            new[] { "16:9", "1294", "620" },
            new[] { "20:9", "1774", "620" },
            new[] { "4:3", "1039.6", "724" },
            new[] { "16:9", "1294", "724" },
            new[] { "20:9", "1774", "724" }
        };
        int failed = 0;
        for (int p = 0; p < plates.Length; p++)
        {
            float width = float.Parse(plates[p][1], System.Globalization.CultureInfo.InvariantCulture);
            float height = float.Parse(plates[p][2], System.Globalization.CultureInfo.InvariantCulture);
            for (int locale = 0; locale < 2; locale++)
            {
                bool fa = locale == 1;
                for (int reserve = 0; reserve < 2; reserve++)
                {
                    bool reserveOn = reserve == 1;
                    TheatreSeatGeometry.Layout layout = TheatreSeatGeometry.Arrange(
                        width, height, fa, reserveOn, "DB");
                    string name = plates[p][0] + " h=" + plates[p][2] + (fa ? " FA" : " EN") + (reserveOn ? " reserve" : " open");
                    bool ok = layout.CardsLegal && layout.CueNear && layout.HexAspectLegal && layout.Seats.Length == 5;
                    if (!ok)
                        failed++;
                    Console.WriteLine((ok ? "PASS " : "FAIL ") + name
                        + " cards=" + layout.CardsLegal
                        + " cue=" + layout.CueNear
                        + " aspect=" + layout.HexAspectLegal);
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
}
