using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

internal static class S004SessionInitializationOwnerControl
{
    static void Need(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    static string Hash(byte[] value) { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(value)).Replace("-", "").ToLowerInvariant(); }
    static int Main(string[] args)
    {
        try
        {
            string path = Path.GetFullPath(args[0]); var bytes = File.ReadAllBytes(path);
            var setup = SourceS004CompiledSetupInput.Read(path, Hash(bytes));
            var manager = new EntityManager { LogicalAllocatorAdmitted = true }; int next = 400000;
            manager.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
            var session = manager.CreateEntity();
            manager.AddComponentData(session, new SkirmishExpandedSessionComponent { SessionId = new FixedString64Bytes("s004-initialization-fixture"), Phase = SkirmishSessionPhase.Queued, IsLegacy = 0 });
            manager.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = setup });
            var missing = manager.CreateEntity();
            manager.AddComponentData(missing, new SkirmishExpandedSessionComponent { SessionId = new FixedString64Bytes("s004-missing-fixture"), Phase = SkirmishSessionPhase.Queued, IsLegacy = 0 });
            var state = new SystemState { EntityManager = manager, Enabled = true };
            var owner = new SkirmishSessionInitializationSystem(); owner.OnUpdate(ref state);
            var initialized = manager.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var failed = manager.GetComponentData<SkirmishExpandedSessionComponent>(missing);
            Need(initialized.InitializationComplete == 1 && initialized.SpawnComplete == 0 && initialized.Phase == SkirmishSessionPhase.Spawning,
                "Original S004 initializer did not enter spawning");
            Need(failed.Phase == SkirmishSessionPhase.Failed && failed.FailureCode == SkirmishReasonCode.MissingResolvedSetup,
                "Missing setup must fail with original reason");
            var stock = manager.GetComponentData<SkirmishEconomyStockComponent>(session);
            var capacity = manager.GetComponentData<SkirmishCapacityComponent>(session);
            Need(stock.Materials == 900 && stock.Oil == 240 && stock.Fuel == 700 && capacity.InfantryCap == 48 && capacity.GroundCap == 8 && capacity.AirCap == 2,
                "Original startup stock/capacity projection differs from authored setup");
            Need(!manager.HasComponent<SkirmishEnemyPlannerV1Component>(session), "S004 policyless compiled setup must not gain an invented planner");
            owner.OnUpdate(ref state);
            Need(manager.GetComponentData<SkirmishExpandedSessionComponent>(session).InitializationComplete == 1,
                "Repeated initialization changed completion");
            Console.WriteLine("[S004SessionInitializationOwnerControl] result=Passed sourceSetup=1 missingSetup=Rejected stock=Passed caps=Passed scheduler=0 mission=0 readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
