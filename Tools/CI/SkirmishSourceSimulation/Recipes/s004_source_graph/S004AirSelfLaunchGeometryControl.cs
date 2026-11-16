using System;
using Game.Runtime;
using Game.Runtime.TransportPose;
using Unity.Mathematics;

internal static class S004AirSelfLaunchGeometryControl
{
    static void Need(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    static void Main()
    {
        PoseMath.AdmitInstalledMono();
        var a = SourceAirSelfLaunchGeometry.Resolve(new float3(100, 1, 200), false, float3.zero, float3.zero, 0, 1);
        Need(math.all(a.Hover == new float3(100, 7, 200)) && math.all(a.Entry == new float3(30, 23, 200)), "no camera geometry");
        var b = SourceAirSelfLaunchGeometry.Resolve(new float3(100, 1, 200), true, new float3(1, 0, 0), new float3(0, 0, 1), 0, 1);
        Need(math.all(a.Hover == b.Hover) && math.all(a.Entry == b.Entry), "camera geometry");
        bool rejected = false;
        try { SourceAirSelfLaunchGeometry.Resolve(float3.zero, true, float3.zero, float3.zero, 0, 1); } catch (NotSupportedException) { rejected = true; }
        Need(rejected, "unknown camera basis");
        rejected = false;
        try { SourceAirSelfLaunchGeometry.Resolve(float3.zero, false, float3.zero, float3.zero, 1, 1); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Need(rejected, "unknown lane");
        Console.WriteLine("[S004AirSelfLaunchGeometryControl] result=Passed scope=SyntheticFixture_NoLaunch");
    }
}
