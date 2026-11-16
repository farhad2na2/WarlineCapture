using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// The ground-only extraction deliberately fails closed when production changes.
// Math implementation identity/loading is owned by the host's pinned bootstrap.
internal static class BodyTransitionSourcePins
{
    static bool verified;
    internal static readonly IReadOnlyDictionary<string,string> Expected = new Dictionary<string,string>
    {
        { "Assets/Game/Scripts/Systems/VehicleFuelConsumptionSystem.cs", "bf6cec55182cb8ac7a5fbb221642638e091b2de1aacf3f4976eea95d073bb0f0" },
        { "Assets/Game/Scripts/Components/UnitVehicleMovementUtility.cs", "86b6c15ef132708b0d65b346e1231a4181fdb9f9e37ba791554e4ec0ac14f9a5" },
        { "Assets/Game/Scripts/Components/GridComponents.cs", "eecfe09c12836db541242b45133db5e75659acb269469db9fdda884a1510ff02" },
        { "Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs", "b81c797aad5a0256fb331d772aa445c52d2337ce47617863f51b80f1011498d5" },
        { "Assets/Game/Scripts/Components/UnitCombatComponents.cs", "ee997571f3c93adfeeb0d39b49e13d48df34c954a2d5f8cbce9c936c18d1e5dc" },
        { "Tools/CI/SkirmishOfflineSim/ProductionGroundFuelKernel.cs", "f81a0059691f4ec88fc67a31450c9194fbe9f481119700f0de0b56b8c1e25326" },
        { "Tools/CI/SkirmishOfflineSim/BodyTransitionData.cs", "9f3c70553245c725bb139e803e18be7a142d402b417a0d422be7b7369760f07a" },
        { "Tools/CI/SkirmishOfflineSim/CapturedPhysicalFuel.cs", "0e35872d311050a36b4e79b510fb75439e6c7eed76f46c7a0c0585a0119522b2" },
        { "Tools/CI/SkirmishOfflineSim/CapturedVehicleSteering.cs", "056b0cc34e8b4c4748f460be12fd8da3640e20a77a0b9000e22a07ff8f05b8ac" },
        { "Assets/Game/Scripts/Systems/UnitGridMovementSystem.cs", "7f0e34acb45f1dc78ad2922a06725e466a3516f836ffe7bdddb9b21cae172779" },
        { "Assets/Game/Scripts/Components/UnitFootprintUtility.cs", "af2205647cd3bf6d99df509af5525840e60bda99659b3e0ffa17bc24e4303599" },
        { "Assets/Game/Scripts/Components/GridCoordinateKernel.cs", "6c1c954af6a53498e2d61700c73ded320d32c2000570cd9f8964eadefffc4711" },
        { "Assets/Game/Scripts/Systems/UnitGridMovementPolicy.cs", "24772ff0c60be60acd4af2ff5346530d6a6d1fa3b4731c452b6533eb36289ded" },
        { "Assets/Game/Scripts/Systems/GridPathMovementKernel.cs", "27cff713257cb5b723aaabb3308ee35c8045db735f527d0413ab3f7a3f5fda1f" },
        { "Assets/Game/Scripts/Systems/InfantryPathLifecycleKernel.cs", "1e858fc382458a994138912be33346228cac162a07ab364c68a85d6e71117687" },
        { "Assets/Game/Scripts/Systems/Pathfinding/PathPlacementKernel.cs", "21ff4ca290aae7b90747b16a6b15a5cbc0ae34cf98ef15da3ccfc4ce0d2ba6b6" },
        { "Tools/CI/SkirmishOfflineSim/VehicleCaller/UnitGridJobs.original.cs", "7c57a6bb033925532f1aa1c337a1f917804a624d2ffbfcdb1824467ceff78882" },
        { "Tools/CI/SkirmishOfflineSim/VehicleCaller/VehicleCallerBindings.cs", "a7a2b47e68d1338eaad0b6256215a6b0f3be2b056c97019455fbaa92e8e97986" },
        { "Tools/CI/SkirmishOfflineSim/VehicleCaller/VehicleCallerData.cs", "10378bca7c157d2992a8b2dbf7625afb2bb38f66bc497ab44e31570bbf133586" },
        { "Tools/CI/SkirmishOfflineSim/VehicleCaller/CapturedVehicleCaller.cs", "b6bf819e7971be72ca9e629b27af324e69547511c2fa2b0458c2afaed5728312" },
        { "Tools/CI/SkirmishOfflineSim/VehicleCallerReplay.cs", "05024361ee2cad7831a6078884f3a6deb945cdc0f9155dfd5230ff95cdacf54d" },
    };
    internal static void Verify(string projectRoot)
    {
        verified=false;
        var verifiedBytes=new Dictionary<string,byte[]>(StringComparer.Ordinal);
        foreach(var entry in Expected)
        {
            string path=Path.Combine(projectRoot,entry.Key);var file=new FileInfo(path);
            if(!file.Exists||(file.Attributes&(FileAttributes.ReparsePoint|FileAttributes.Directory|FileAttributes.Device))!=0||file.Length<=0||file.Length>16*1024*1024)
                throw new ArgumentException("Bounded regular production/body source required: "+entry.Key);
            byte[] bytes=new byte[checked((int)file.Length)];
            using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                if(!stream.CanSeek||stream.Length!=bytes.Length)throw new ArgumentException("Production source size/type changed before read: "+entry.Key);
                stream.ReadExactly(bytes);
                if(stream.ReadByte()!=-1)throw new ArgumentException("Production source grew during bounded read: "+entry.Key);
            }
            string actual=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if(actual!=entry.Value)throw new ArgumentException("Production/body source pin changed: "+entry.Key);
            verifiedBytes.Add(entry.Key,bytes);
        }
        // The accumulation span is verbatim; only the known ECS storage iteration
        // header and access visibility differ in DrainRequestedFuel.
        string native=Encoding.UTF8.GetString(verifiedBytes["Assets/Game/Scripts/Systems/VehicleFuelConsumptionSystem.cs"]);
        string kernel=Encoding.UTF8.GetString(verifiedBytes["Tools/CI/SkirmishOfflineSim/ProductionGroundFuelKernel.cs"]);
        native=ShippingProjection(native);
        int start=native.IndexOf("                if (stateRw.Initialized == 0)",StringComparison.Ordinal);
        int end=native.IndexOf("            }\n\n            if(steelPush)",start,StringComparison.Ordinal);
        if(start<0||end<start||!kernel.Contains(native.Substring(start,end-start).TrimEnd(),StringComparison.Ordinal))
            throw new ArgumentException("Verbatim ground fuel accumulation span changed");
        string nativeDrain=native.Substring(native.IndexOf("        private void DrainRequestedFuel(",StringComparison.Ordinal));
        int close=nativeDrain.LastIndexOf("    }",StringComparison.Ordinal);
        nativeDrain=nativeDrain.Substring(0,close).TrimEnd();
        nativeDrain=nativeDrain.Replace("private void DrainRequestedFuel","public void DrainRequestedFuel").Replace("foreach (RefRW<BuildingResourceStorageComponent> storageRef in SystemAPI.Query<RefRW<BuildingResourceStorageComponent>>())","foreach (StorageRef storageRef in query.Enumerate())");
        int predicate=nativeDrain.IndexOf("        private static bool IsUsableFuelStorage(",StringComparison.Ordinal);
        if(predicate<0||!kernel.Contains(nativeDrain.Substring(0,predicate).TrimEnd(),StringComparison.Ordinal)||!kernel.Contains(nativeDrain.Substring(predicate),StringComparison.Ordinal))
            throw new ArgumentException("Exact fuel drain/predicate extraction changed");
        string productionJobs=ShippingProjection(Encoding.UTF8.GetString(verifiedBytes["Assets/Game/Scripts/Systems/UnitGridMovementSystem.cs"]));
        int jobsStart=productionJobs.IndexOf("    [BurstCompile]\n    [WithNone(",StringComparison.Ordinal);
        int jobsEnd=productionJobs.IndexOf("    [BurstCompile]\n    [UpdateInGroup(",jobsStart,StringComparison.Ordinal);
        string extracted=Encoding.UTF8.GetString(verifiedBytes["Tools/CI/SkirmishOfflineSim/VehicleCaller/UnitGridJobs.original.cs"]);
        if(jobsStart<0||jobsEnd<jobsStart||!extracted.Contains(productionJobs.Substring(jobsStart,jobsEnd-jobsStart).TrimEnd(),StringComparison.Ordinal))
            throw new ArgumentException("Verbatim movement/cleanup job extraction changed");
        verified=true;
    }
    // Only the pinned, simple Editor observer sections are projected. Unknown
    // directives fail closed; the runtime accumulation/drain remain verbatim.
    static string ShippingProjection(string source)
    {
        var result=new StringBuilder(); bool inside=false, editorArm=false;
        foreach(string line in source.Replace("\r\n","\n").Split('\n'))
        {
            string directive=line.Trim();
            if(directive=="#if UNITY_EDITOR")
            { if(inside)throw new ArgumentException("Nested observer directive unsupported");inside=true;editorArm=true;continue; }
            if(directive=="#else")
            { if(!inside||!editorArm)throw new ArgumentException("Unexpected observer else");editorArm=false;continue; }
            if(directive=="#endif")
            { if(!inside)throw new ArgumentException("Unexpected observer endif");inside=false;editorArm=false;continue; }
            if(directive.StartsWith("#",StringComparison.Ordinal))throw new ArgumentException("Unknown fuel source directive");
            if(!inside||!editorArm)result.Append(line).Append('\n');
        }
        if(inside)throw new ArgumentException("Unclosed observer directive");
        return result.ToString();
    }
    internal static void RequireVerified()
    {
        if(!verified)throw new ArgumentException("Host must verify body production source pins before replay");
    }
}
