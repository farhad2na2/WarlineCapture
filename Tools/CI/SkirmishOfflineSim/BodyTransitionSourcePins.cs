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
        { "Assets/Game/Scripts/Systems/VehicleFuelConsumptionSystem.cs", "a3e538ba1531019f19540607c36fad81e344cc7da259d2965b9e83f29ff4c154" },
        { "Assets/Game/Scripts/Components/UnitVehicleMovementUtility.cs", "86b6c15ef132708b0d65b346e1231a4181fdb9f9e37ba791554e4ec0ac14f9a5" },
        { "Assets/Game/Scripts/Components/GridComponents.cs", "eecfe09c12836db541242b45133db5e75659acb269469db9fdda884a1510ff02" },
        { "Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs", "b81c797aad5a0256fb331d772aa445c52d2337ce47617863f51b80f1011498d5" },
        { "Assets/Game/Scripts/Components/UnitCombatComponents.cs", "ee997571f3c93adfeeb0d39b49e13d48df34c954a2d5f8cbce9c936c18d1e5dc" },
        { "Tools/CI/SkirmishOfflineSim/ProductionGroundFuelKernel.cs", "f81a0059691f4ec88fc67a31450c9194fbe9f481119700f0de0b56b8c1e25326" },
        { "Tools/CI/SkirmishOfflineSim/BodyTransitionData.cs", "9f3c70553245c725bb139e803e18be7a142d402b417a0d422be7b7369760f07a" },
        { "Tools/CI/SkirmishOfflineSim/CapturedPhysicalFuel.cs", "0e35872d311050a36b4e79b510fb75439e6c7eed76f46c7a0c0585a0119522b2" },
        { "Tools/CI/SkirmishOfflineSim/CapturedVehicleSteering.cs", "056b0cc34e8b4c4748f460be12fd8da3640e20a77a0b9000e22a07ff8f05b8ac" },
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
        verified=true;
    }
    internal static void RequireVerified()
    {
        if(!verified)throw new ArgumentException("Host must verify body production source pins before replay");
    }
}
