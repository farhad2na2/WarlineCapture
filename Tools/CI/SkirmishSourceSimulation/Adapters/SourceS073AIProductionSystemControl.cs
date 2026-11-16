using System;
using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    // Standalone compiler-control entrypoint. Invoke through its own csc -main
    // response file; this is not the normal source simulation entrypoint.
    internal static class SourceS073AIProductionSystemControl
    {
        private const byte FactionId = 2;
        private static int _nextEntity;

        private sealed class Case
        {
            public EntityManager Manager;
            public Entity Boundary;
            public Entity Economy;
            public Entity Plan;
        }

        private static void Need(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static EntityManager NewManager()
        {
            var manager = new EntityManager { LogicalAllocatorAdmitted = true };
            manager.AllocateLogicalIdentity = () => new Entity { Index = ++_nextEntity, Version = 1 };
            manager.WorldUnmanaged.Time = new TimeData { ElapsedTime = 2d, DeltaTime = 1f / 60f };
            return manager;
        }

        private static Case CreateCase(bool hasFunds)
        {
            var manager = NewManager();
            Entity runtime = manager.CreateEntity();
            manager.AddComponentData(runtime, new RuntimeGameplayStateComponent { SimulationActive = 1 });

            Entity economy = manager.CreateEntity();
            manager.AddComponentData(economy, new FactionEconomy
            {
                FactionId = FactionId,
                Money = hasFunds ? 100 : 0
            });
            manager.AddComponentData(economy, new FactionTacticalMaterialsComponent
            {
                FactionId = FactionId,
                Current = hasFunds ? 100 : 0,
                Capacity = 100
            });

            Entity boundary = manager.CreateEntity();
            manager.AddComponentData(boundary, new BuildingRuntimeStateTag());
            manager.AddBuffer<BuildingConfiguredUnitReadModel>(boundary).Add(new BuildingConfiguredUnitReadModel
            {
                UnitId = new FixedString128Bytes("source-control-rifle"),
                DisplayName = new FixedString128Bytes("Source Control Rifle"),
                CreditsCost = 10,
                Price = 5,
                CanRequest = 1,
                IsVehicle = 0
            });
            manager.AddBuffer<BuildingRuntimeUnitProductionSummary>(boundary);
            manager.AddBuffer<BuildingFactionUnitProductionRequest>(boundary);
            manager.AddBuffer<BuildingRuntimeFactionSummary>(boundary);
            manager.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);

            Entity plan = manager.CreateEntity();
            manager.AddComponentData(plan, new AIProductionPlan
            {
                FactionId = FactionId,
                Enabled = 1,
                TargetProducedUnits = 1,
                MaxQueuedUnits = 1,
                UnitProductionIntervalSeconds = 1f,
                LastProductionTime = 0f
            });
            manager.AddBuffer<AIProductionPlanEntry>(plan).Add(new AIProductionPlanEntry
            {
                UnitId = new FixedString64Bytes("source-control-rifle")
            });

            return new Case { Manager = manager, Boundary = boundary, Economy = economy, Plan = plan };
        }

        private static void RunOriginalOwner(Case fixture)
        {
            var state = new SystemState { EntityManager = fixture.Manager };
            var owner = new AIProductionSystem();
            owner.OnCreate(ref state);
            state.Begin();
            owner.OnUpdate(ref state);
            state.End();
        }

        private static int Run()
        {
            Case affordable = CreateCase(true);
            int affordableMoneyBefore = affordable.Manager.GetComponentData<FactionEconomy>(affordable.Economy).Money;
            int affordableMaterialsBefore = affordable.Manager.GetComponentData<FactionTacticalMaterialsComponent>(affordable.Economy).Current;
            RunOriginalOwner(affordable);

            FactionEconomy spentEconomy = affordable.Manager.GetComponentData<FactionEconomy>(affordable.Economy);
            FactionTacticalMaterialsComponent spentMaterials = affordable.Manager.GetComponentData<FactionTacticalMaterialsComponent>(affordable.Economy);
            AIProductionPlan requestedPlan = affordable.Manager.GetComponentData<AIProductionPlan>(affordable.Plan);
            DynamicBuffer<BuildingFactionUnitProductionRequest> affordableRequests =
                affordable.Manager.GetBuffer<BuildingFactionUnitProductionRequest>(affordable.Boundary);
            Need(affordableRequests.Length == 1, "Affordable source plan did not enqueue exactly one original production request");
            BuildingFactionUnitProductionRequest request = affordableRequests[0];
            Need(request.Status == BuildingFactionUnitProductionRequest.Pending && request.ResourcesReserved == 1 &&
                 request.FactionId == FactionId && request.UnitId.ToString() == "source-control-rifle" &&
                 request.Cost == 10 && request.MaterialsCost == 5,
                "Original production request fields did not match the configured affordable unit");
            Need(spentEconomy.Money == affordableMoneyBefore - 10 && spentMaterials.Current == affordableMaterialsBefore - 5 &&
                 spentMaterials.LifetimeConstructionSpent == 5 && requestedPlan.LastProductionTime == 2f &&
                 requestedPlan.NextUnitIndex == 1,
                "Original affordable decision did not spend configured costs and advance the plan");

            Case unaffordable = CreateCase(false);
            int blockedMoneyBefore = unaffordable.Manager.GetComponentData<FactionEconomy>(unaffordable.Economy).Money;
            int blockedMaterialsBefore = unaffordable.Manager.GetComponentData<FactionTacticalMaterialsComponent>(unaffordable.Economy).Current;
            RunOriginalOwner(unaffordable);

            FactionEconomy blockedEconomy = unaffordable.Manager.GetComponentData<FactionEconomy>(unaffordable.Economy);
            FactionTacticalMaterialsComponent blockedMaterials = unaffordable.Manager.GetComponentData<FactionTacticalMaterialsComponent>(unaffordable.Economy);
            AIProductionPlan blockedPlan = unaffordable.Manager.GetComponentData<AIProductionPlan>(unaffordable.Plan);
            DynamicBuffer<BuildingFactionUnitProductionRequest> blockedRequests =
                unaffordable.Manager.GetBuffer<BuildingFactionUnitProductionRequest>(unaffordable.Boundary);
            Need(blockedRequests.Length == 0, "Insufficient-funds decision emitted a production request");
            Need(blockedEconomy.Money == blockedMoneyBefore && blockedMaterials.Current == blockedMaterialsBefore &&
                 blockedMaterials.LifetimeConstructionSpent == 0 && blockedPlan.LastProductionTime == 2f &&
                 blockedPlan.NextUnitIndex == 0,
                "Insufficient-funds decision changed resources or advanced the plan");

            Console.WriteLine("[S073AIProductionSystemControl] result=Passed owner=OriginalAIProductionSystem " +
                "affordable=Requested credits_spent=10 materials_spent=5 insufficient_funds=Blocked " +
                "blocked_requests=0 source_world_used=false native_readiness=false");
            return 0;
        }

        public static int Main(string[] args)
        {
            try { return Run(); }
            catch (Exception error)
            {
                Console.Error.WriteLine("[S073AIProductionSystemControl] result=Failed " + error);
                return 1;
            }
        }
    }
}
