using System;
using System.IO;
using System.Security.Cryptography;
using Game.Runtime;
using Game.Runtime.TransportPose;
using Unity.Entities;
using Unity.Mathematics;

internal static class S004AirSelfAuthoredPoseControl
{
    static void Need(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static void Main(string[] args)
    {
        Need(args.Length == 3, "repository, source inventory and hash required");
        PoseMath.AdmitInstalledMono();
        var prefab = new SourceUnitPrefab("Unit_Veh_Helicopter_Attack_Small", new Entity { Index = 7, Version = 1 },
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Attack_Small.prefab", "fcc8f80798992470f8b8769d3bc4c41a", 5616646512338707422L);
        var pose = new SourceS004AirSelfAuthoredPose(args[0], args[1], args[2], prefab);
        Need(pose.RotorCount == 2, "source rotor count");
        var a = pose.RotorRotation(0); var b = pose.RotorRotation(1);
        pose.SetRoot(new float3(10, 7, 15), quaternion.identity); pose.Spin(1f / 60f);
        Need(math.all(pose.RootPosition == new float3(10, 7, 15)) && math.all(pose.Forward == new float3(0, 0, 1)), "source root pose");
        Need(PoseMath.QError(a, pose.RotorRotation(0)) > .01f && PoseMath.QError(b, pose.RotorRotation(1)) > .01f, "source rotors advanced");
        bool rejected = false;
        try { new SourceS004AirSelfAuthoredPose(args[0], args[1], new string('0', 64), prefab); } catch (InvalidDataException) { rejected = true; }
        Need(rejected, "inventory hash tamper");
        rejected = false;
        try { new SourceS004AirSelfAuthoredPose(args[0], args[1], args[2], new SourceUnitPrefab("wrong", new Entity { Index = 8, Version = 1 })); }
        catch (InvalidDataException) { rejected = true; }
        Need(rejected, "wrong prefab");
        Console.WriteLine("[S004AirSelfAuthoredPoseControl] result=Passed scope=SourcePinnedRootAndRotors_NoRendererOrMission");
    }
}
