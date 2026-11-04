using System;
using Game.Components;
using Unity.Mathematics;

internal static class UnitAttackAuthoringProjectionControl
{
    private static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static int Bits(float value) => BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
    private static bool Same(float a, float b) => Bits(a) == Bits(b);

    private static UnitAttack Project(float range, float cooldown, int damage, float color, float width,
        float scroll, float dash, float visible, int tracer) =>
        UnitAttackAuthoringProjection.FromConfiguredValues(range, cooldown, damage,
            color, -color, color, -color, width, scroll, dash, visible, tracer);

    private static void Check(float range, float cooldown, int damage, float color, float width,
        float scroll, float dash, float visible, int tracer)
    {
        UnitAttack actual = Project(range, cooldown, damage, color, width, scroll, dash, visible, tracer);
        Need(Same(actual.Range, range) && Same(actual.CooldownSeconds, cooldown) && actual.Damage == damage,
            "configured combat getter values must pass through unchanged");
        Need(Same(actual.TraceColor.x, color) && Same(actual.TraceColor.y, -color) &&
             Same(actual.TraceColor.z, color) && Same(actual.TraceColor.w, -color),
            "all trace color channel bits must pass through unchanged");
        Need(Same(actual.TraceWidth, math.max(0.01f, width)), "Unity.Mathematics width floor parity");
        Need(Same(actual.TraceScrollSpeed, math.max(0.1f, scroll)), "Unity.Mathematics scroll floor parity");
        Need(Same(actual.TraceDashDensity, math.max(1f, dash)), "Unity.Mathematics dash floor parity");
        Need(Same(actual.TraceVisibleSeconds, math.max(0.01f, visible)), "Unity.Mathematics visibility floor parity");
        Need(actual.TracerEveryNthShot == math.max(1, tracer), "Unity.Mathematics integer cadence floor parity");
    }

    private static int Main()
    {
        int[] patterns = {
            unchecked((int)0x7fc12345), unchecked((int)0xffc54321), 0x7f800000,
            unchecked((int)0xff800000), 0, unchecked((int)0x80000000),
            1, unchecked((int)0x80000001), 0x3c23d709, 0x3c23d70a, 0x3c23d70b,
            0x3f800000, unchecked((int)0xbf800000), 0x7f7fffff
        };
        foreach (int bits in patterns)
        {
            float value = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
            Check(value, -value, bits, value, value, value, value, value, bits);
        }

        Check(float.NaN, float.PositiveInfinity, int.MinValue, -0f,
            float.NegativeInfinity, -0f, float.NaN, float.PositiveInfinity, int.MinValue);
        Check(float.NegativeInfinity, float.NaN, int.MaxValue, float.PositiveInfinity,
            float.NegativeInfinity, float.NaN, -float.Epsilon, float.NaN, int.MaxValue);

        Console.WriteLine("[UnitAttackAuthoringProjectionControl] result=Passed floatPatterns=14 nonfinite=NaN,+Inf,-Inf negativeControls=0 scope=UnitAttackValues");
        return 0;
    }
}
