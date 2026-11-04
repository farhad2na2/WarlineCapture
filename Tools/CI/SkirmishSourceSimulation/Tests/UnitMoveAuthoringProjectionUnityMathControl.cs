using System;
using Game.Components;
using Unity.Mathematics;

internal static class UnitMoveAuthoringProjectionUnityMathControl
{
    private static int Main(string[] args)
    {
        if (args.Length != 4) return Fail("expected authored rifle values");
        float speed = float.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
        float walk = float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
        float road = float.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);
        float arrive = float.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture);
        Check(speed, walk, road, arrive, "authored-rifle");

        int[] patterns = {
            unchecked((int)0x7fc12345), unchecked((int)0xffc54321), 0x7f800000, unchecked((int)0xff800000),
            0x00000000, unchecked((int)0x80000000), 0x3c23d709, 0x3c23d70a, 0x3c23d70b,
            0x00000001, unchecked((int)0x80000001), 0x3f800000, unchecked((int)0xbf800000)
        };
        int compared = 0;
        foreach (int speedBits in patterns)
        foreach (int walkBits in patterns)
        {
            Check(FromBits(speedBits), FromBits(walkBits), FromBits(speedBits), FromBits(walkBits),
                "edge-" + Bits(speedBits) + "-" + Bits(walkBits));
            compared++;
        }
        Console.WriteLine("[UnitMoveAuthoringProjectionUnityMathControl] result=Passed pairs=" + compared);
        return 0;
    }

    private static void Check(float speed, float walk, float road, float arrive, string label)
    {
        float expectedWalk = Unity.Mathematics.math.min(Unity.Mathematics.math.max(0.01f, walk),
            Unity.Mathematics.math.max(0.01f, speed));
        UnitMoveAuthoringProjection actual = UnitMoveAuthoringProjection.FromAuthoredValues(speed, walk, road, arrive);
        Need(Bits(actual.Speed) == Bits(speed), label + " speed bits");
        Need(Bits(actual.WalkSpeed) == Bits(expectedWalk), label + " walk bits");
        Need(Bits(actual.RoadSpeedMultiplier) == Bits(road), label + " road bits");
        Need(Bits(actual.ArriveDistance) == Bits(arrive), label + " arrive bits");
    }

    private static float FromBits(int value) => BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
    private static int Bits(float value) => BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
    private static string Bits(int value) => unchecked((uint)value).ToString("x8");
    private static void Need(bool ok, string label) { if (!ok) throw new InvalidOperationException("assertion failed: " + label); }
    private static int Fail(string text) { Console.Error.WriteLine("[UnitMoveAuthoringProjectionUnityMathControl] result=Failed " + text); return 1; }
}
