#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;

public sealed class SelectionCommandRequestResultContractTests
{
    public static void RunBatchValidation()
    {
        try
        {
            var tests = new SelectionCommandRequestResultContractTests();
            tests.CommandIntentRequest_CarriesPreResolvedTargetData();
            tests.CommandResult_CarriesFeedbackMarkerAndLifetimeData();
            tests.TacticalCommandReasonCodes_IncludeTransportFailureCodes();
            tests.ScanCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds();
            tests.MoveCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds();
            tests.AttackCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds();
            tests.AttackCommandSystem_OnUpdateConsumesPreResolvedEntityRequest();
            tests.ScanCommandSystem_OnUpdateConsumesPreResolvedCellRequest();
            tests.TransportCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds();
            tests.ScanCommandFlush_DrainsResultsOnceAndDoesNotDuplicateFeedback();
            tests.MoveCommandFlush_ReacquiresCommandBuffersAfterQuerySetupStructuralChange();
            UnityEngine.Debug.Log("[SelectionCommandRequestResultContractValidation] result=Passed tests=11");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError("[SelectionCommandRequestResultContractValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void CommandIntentRequest_CarriesPreResolvedTargetData()
    {
        Entity target = new() { Index = 7, Version = 1 };
        Entity secondaryTarget = new() { Index = 8, Version = 1 };

        var request = new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Attack,
            RequestId = 42,
            Frame = 123,
            TargetEntity = target,
            SecondaryTargetEntity = secondaryTarget,
            TargetCell = new int2(12, 34),
            WorldPosition = new float3(5f, 6f, 7f),
            ScreenPosition = new float2(800f, 450f),
            DragStart = new float2(100f, 200f),
            DragCurrent = new float2(300f, 400f),
            TargetKind = RtsSelectionCommandTargetKind.WorldPosition,
            ExplicitAttackTargetMode = 1,
            HasTargetEntity = 1,
            HasSecondaryTargetEntity = 1,
            HasTargetCell = 1,
            HasWorldPosition = 1,
            HasScreenPosition = 1,
            HasScreenRect = 1
        };

        Assert.AreEqual(RtsSelectionCommandIntentKind.Attack, request.Kind);
        Assert.AreEqual(target, request.TargetEntity);
        Assert.AreEqual(secondaryTarget, request.SecondaryTargetEntity);
        Assert.AreEqual(new int2(12, 34), request.TargetCell);
        Assert.AreEqual(new float3(5f, 6f, 7f), request.WorldPosition);
        Assert.AreEqual(RtsSelectionCommandTargetKind.WorldPosition, request.TargetKind);
        Assert.AreEqual(1, request.HasTargetEntity);
        Assert.AreEqual(1, request.HasWorldPosition);
        Assert.AreEqual(1, request.HasScreenPosition);
        Assert.AreEqual(1, request.HasScreenRect);
    }

    [Test]
    public void CommandResult_CarriesFeedbackMarkerAndLifetimeData()
    {
        Entity target = new() { Index = 17, Version = 2 };

        var result = new RtsSelectionCommandResultElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger,
            RequestId = 77,
            Frame = 456,
            TargetEntity = target,
            TargetCell = new int2(21, 43),
            ScreenPosition = new float2(512f, 256f),
            WorldPosition = new float3(9f, 0f, 11f),
            TargetKind = RtsSelectionCommandTargetKind.Entity,
            CommandMode = (int)TacticalCommandMode.Board,
            HasCommandResult = 1,
            Accepted = 0,
            ReasonCode = (int)TacticalCommandReasonCode.TransportFull,
            FeedbackLifetime = RtsSelectionCommandFeedbackLifetime.Transient,
            FeedbackDurationSeconds = 2.25f,
            EmitScreenMarker = 1,
            MarkerFactionId = 2,
            HasTargetEntity = 1,
            HasTargetCell = 1,
            HasWorldPosition = 1,
            ShowWorldMarkers = 1,
            RevealedCount = 3,
            RadiusCells = 5,
            Message = new FixedString64Bytes("Transport is full.")
        };

        Assert.AreEqual(RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger, result.Kind);
        Assert.AreEqual(target, result.TargetEntity);
        Assert.AreEqual(RtsSelectionCommandTargetKind.Entity, result.TargetKind);
        Assert.AreEqual((int)TacticalCommandMode.Board, result.CommandMode);
        Assert.AreEqual(0, result.Accepted);
        Assert.AreEqual((int)TacticalCommandReasonCode.TransportFull, result.ReasonCode);
        Assert.AreEqual(RtsSelectionCommandFeedbackLifetime.Transient, result.FeedbackLifetime);
        Assert.AreEqual(2.25f, result.FeedbackDurationSeconds);
        Assert.AreEqual(1, result.EmitScreenMarker);
        Assert.AreEqual(1, result.ShowWorldMarkers);
        Assert.AreEqual("Transport is full.", result.Message.ToString());
    }

    [Test]
    public void TacticalCommandReasonCodes_IncludeTransportFailureCodes()
    {
        Assert.AreEqual("Select a transport vehicle or aircraft first.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.InvalidTransport));
        Assert.AreEqual("Select soldiers that can board.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.InvalidPassenger));
        Assert.AreEqual("Transport is full.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.TransportFull));
        Assert.AreEqual("No nearby soldiers can board this transport.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.NoEligiblePassengers));
        Assert.AreEqual("No clear exit point for passengers.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.NoDisembarkCell));
        Assert.AreEqual("Passenger is not inside this transport.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.TransportPassengerMissing));
    }

    [Test]
    public void ScanCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds()
    {
        using World world = new("SelectionCommandRequestResultContractTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity();
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Scan,
            RequestId = 1,
            Frame = 10,
            HasScreenPosition = 1
        });
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Attack,
            RequestId = 2,
            Frame = 10,
            HasScreenPosition = 1
        });
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Scan,
            RequestId = 3,
            Frame = 11,
            HasScreenPosition = 1
        });

        using EntityQuery emptyGridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        var processor = new ScanIntelCommandSystem();

        bool handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            emptyGridConfigQuery,
            null);

        Assert.IsTrue(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Attack, requests[0].Kind);
        Assert.AreEqual(2, requests[0].RequestId);
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual(1, results[0].RequestId);
        Assert.AreEqual(3, results[1].RequestId);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, results[0].Kind);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, results[1].Kind);
        Assert.AreEqual((int)TacticalCommandMode.Scan, results[0].CommandMode);
        Assert.AreEqual(RtsSelectionCommandFeedbackLifetime.Transient, results[0].FeedbackLifetime);
        Assert.AreEqual((int)TacticalCommandReasonCode.ScanUnavailable, results[0].ReasonCode);
        Assert.AreEqual((int)TacticalCommandReasonCode.ScanUnavailable, results[1].ReasonCode);

        handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            emptyGridConfigQuery,
            null);

        Assert.IsFalse(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(2, results.Length);
    }

    [Test]
    public void MoveCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds()
    {
        using World world = new("SelectionCommandMoveProcessorTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity();
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement { Kind = RtsSelectionCommandIntentKind.Move, RequestId = 4, Frame = 20 });
        requests.Add(new RtsSelectionCommandIntentRequestElement { Kind = RtsSelectionCommandIntentKind.Scan, RequestId = 5, Frame = 20 });

        using EntityQuery emptySelectedMoveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        using EntityQuery emptyGridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        using EntityQuery emptyMapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        var processor = new SelectedMoveOrderCommandSystem();

        bool handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            emptySelectedMoveQuery,
            emptyGridConfigQuery,
            emptyMapSurfaceQuery,
            null,
            new UnitMoveOrderSystem(),
            new SelectionOrderMarkerSystem(),
            null,
            null);

        Assert.IsTrue(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, requests[0].Kind);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Move, results[0].Kind);
        Assert.AreEqual((int)TacticalCommandMode.Move, results[0].CommandMode);
        Assert.AreEqual(RtsSelectionCommandFeedbackLifetime.Transient, results[0].FeedbackLifetime);
        Assert.AreEqual((int)TacticalCommandReasonCode.NoSelection, results[0].ReasonCode);

        handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            emptySelectedMoveQuery,
            emptyGridConfigQuery,
            emptyMapSurfaceQuery,
            null,
            new UnitMoveOrderSystem(),
            new SelectionOrderMarkerSystem(),
            null,
            null);

        Assert.IsFalse(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(1, results.Length);
    }

    [Test]
    public void AttackCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds()
    {
        using World world = new("SelectionCommandAttackProcessorTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity();
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Attack,
            RequestId = 6,
            Frame = 30,
            ExplicitAttackTargetMode = 1
        });
        requests.Add(new RtsSelectionCommandIntentRequestElement { Kind = RtsSelectionCommandIntentKind.Move, RequestId = 7, Frame = 30 });
        var processor = new AttackOrderCommandSystem();

        bool handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            new UnitTargetOrderSystem(),
            TryGetNoClickedUnit,
            null,
            null,
            default);

        Assert.IsTrue(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Move, requests[0].Kind);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Attack, results[0].Kind);
        Assert.AreEqual((int)TacticalCommandMode.Attack, results[0].CommandMode);
        Assert.AreEqual(RtsSelectionCommandTargetKind.None, results[0].TargetKind);
        Assert.AreEqual(RtsSelectionCommandFeedbackLifetime.Transient, results[0].FeedbackLifetime);
        Assert.AreEqual((int)TacticalCommandReasonCode.TargetNotAttackable, results[0].ReasonCode);

        handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            new UnitTargetOrderSystem(),
            TryGetNoClickedUnit,
            null,
            null,
            default);

        Assert.IsFalse(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(1, results.Length);
    }

    [Test]
    public void AttackCommandSystem_OnUpdateConsumesPreResolvedEntityRequest()
    {
        using World world = new("SelectionCommandAttackSystemOnUpdateTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);

        Entity attacker = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitMove),
            typeof(UnitGrid),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(LocalTransform));
        em.SetComponentData(attacker, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        em.SetComponentData(attacker, new UnitGrid { Cell = new int2(1, 1) });
        em.SetComponentData(attacker, new UnitCombat { CanAttack = 1 });
        em.SetComponentData(attacker, new UnitAttack { Range = 20f, Damage = 10, CooldownSeconds = 1f });
        em.SetComponentData(attacker, LocalTransform.FromPosition(new float3(1.5f, 0f, 1.5f)));

        Entity target = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(LocalTransform));
        em.SetComponentData(target, new Faction { Id = FactionIdentitySystem.EnemyFactionId });
        em.SetComponentData(target, new UnitGrid { Cell = new int2(7, 8) });
        em.SetComponentData(target, LocalTransform.FromPosition(new float3(7.5f, 0f, 8.5f)));

        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Attack,
            RequestId = 66,
            Frame = 44,
            TargetEntity = target,
            TargetKind = RtsSelectionCommandTargetKind.Entity,
            HasTargetEntity = 1
        });

        SystemHandle system = world.CreateSystem<AttackOrderCommandSystem>();
        system.Update(world.Unmanaged);

        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Attack, results[0].Kind);
        Assert.AreEqual(66, results[0].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(1, results[0].HasCommandResult);
        Assert.AreEqual(RtsSelectionCommandTargetKind.Entity, results[0].TargetKind);
        Assert.AreEqual(target, results[0].TargetEntity);
        Assert.IsTrue(em.HasComponent<EngageTarget>(attacker));
        EngageTarget engageTarget = em.GetComponentData<EngageTarget>(attacker);
        Assert.AreEqual(target, engageTarget.Target);
        Assert.AreEqual(new int2(7, 8), engageTarget.Cell);
        Assert.AreEqual(1, engageTarget.IsCommanded);
    }

    [Test]
    public void ScanCommandSystem_OnUpdateConsumesPreResolvedCellRequest()
    {
        using World world = new("SelectionCommandScanSystemOnUpdateTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);

        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 32,
            Height = 32,
            CellSize = 1f,
            Origin = float3.zero
        });

        Entity target = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(target, new Faction { Id = FactionIdentitySystem.EnemyFactionId });
        em.SetComponentData(target, new UnitGrid { Cell = new int2(6, 5) });
        em.SetComponentData(target, new UnitHealth { Current = 10, Max = 10 });
        em.SetComponentData(target, LocalTransform.FromPosition(new float3(6.5f, 0f, 5.5f)));

        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Scan,
            RequestId = 77,
            Frame = 88,
            TargetCell = new int2(5, 5),
            WorldPosition = new float3(5.5f, 0f, 5.5f),
            TargetKind = RtsSelectionCommandTargetKind.Cell,
            HasTargetCell = 1,
            HasWorldPosition = 1
        });

        SystemHandle system = world.CreateSystem<ScanIntelCommandSystem>();
        system.Update(world.Unmanaged);

        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, results[0].Kind);
        Assert.AreEqual(77, results[0].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(1, results[0].HasCommandResult);
        Assert.AreEqual(new int2(5, 5), results[0].TargetCell);
        Assert.AreEqual(RtsSelectionCommandTargetKind.Cell, results[0].TargetKind);
        Assert.AreEqual(1, results[0].HasWorldPosition);
        Assert.AreEqual(1, results[0].RevealedCount);
        Assert.IsTrue(em.HasComponent<ScanIntelRevealedTag>(target));
        Assert.IsTrue(em.HasComponent<ScanIntelLastSeen>(target));
    }

    [Test]
    public void TransportCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds()
    {
        using World world = new("SelectionCommandTransportProcessorTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity();
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement { Kind = RtsSelectionCommandIntentKind.DisembarkTransport, RequestId = 8, Frame = 40 });
        requests.Add(new RtsSelectionCommandIntentRequestElement { Kind = RtsSelectionCommandIntentKind.Scan, RequestId = 9, Frame = 40 });
        var processor = new TransportBoardingCommandSystem();

        bool handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            new UnitTransportCapacitySystem(),
            new UnitTransportBoardingQuerySystem(),
            new UnitTransportBoardingRuleSystem(),
            new UnitTransportApproachCellSystem(),
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            null,
            null,
            null);

        Assert.IsTrue(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, requests[0].Kind);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.DisembarkTransport, results[0].Kind);
        Assert.AreEqual((int)TacticalCommandMode.Board, results[0].CommandMode);
        Assert.AreEqual(RtsSelectionCommandFeedbackLifetime.Hidden, results[0].FeedbackLifetime);
        Assert.AreEqual(0, results[0].Accepted);

        handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            new UnitTransportCapacitySystem(),
            new UnitTransportBoardingQuerySystem(),
            new UnitTransportBoardingRuleSystem(),
            new UnitTransportApproachCellSystem(),
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            null,
            null,
            null);

        Assert.IsFalse(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(1, results.Length);
    }

    [Test]
    public void ScanCommandFlush_DrainsResultsOnceAndDoesNotDuplicateFeedback()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("SelectionCommandScanFlushTests");
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            var inputSystem = new RtsSelectionInputSystem();
            Assert.IsTrue(inputSystem.QueueScanCommandRequest(new UnityEngine.Vector2(10f, 20f), 50));
            Assert.IsTrue(inputSystem.QueueScanCommandRequest(new UnityEngine.Vector2(30f, 40f), 51));
            Assert.IsTrue(inputSystem.TryGetCommandBuffers(
                out _,
                out Entity commandEntity,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
                out DynamicBuffer<RtsSelectionCommandResultElement> results));
            Assert.AreEqual(2, requests.Length);
            Assert.AreEqual(0, results.Length);

            using EntityQuery emptySelectedMoveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            using EntityQuery emptyGridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            using EntityQuery emptyMapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            int feedbackCount = 0;
            var flushSystem = new RtsSelectionCommandResultFlushSystem();
            RtsSelectionCommandResultFlushSystem.Context context = CreateFlushContext(
                inputSystem,
                emptySelectedMoveQuery,
                emptyGridConfigQuery,
                emptyMapSurfaceQuery,
                _ => feedbackCount++,
                em);

            bool processed = flushSystem.ProcessScanCommandRequests(context);

            requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            Assert.IsTrue(processed);
            Assert.AreEqual(0, requests.Length);
            Assert.AreEqual(0, results.Length);
            Assert.AreEqual(2, feedbackCount);

            processed = flushSystem.ProcessScanCommandRequests(context);

            requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            Assert.IsFalse(processed);
            Assert.AreEqual(0, requests.Length);
            Assert.AreEqual(0, results.Length);
            Assert.AreEqual(2, feedbackCount);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void MoveCommandFlush_ReacquiresCommandBuffersAfterQuerySetupStructuralChange()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("SelectionCommandMoveFlushBufferRefreshTests");
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            var inputSystem = new RtsSelectionInputSystem();
            Assert.IsTrue(inputSystem.QueueMoveCommandRequest(new UnityEngine.Vector2(10f, 20f), 70));
            Assert.IsTrue(inputSystem.TryGetCommandBuffers(
                out _,
                out Entity commandEntity,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
                out DynamicBuffer<RtsSelectionCommandResultElement> results));
            Assert.AreEqual(1, requests.Length);
            Assert.AreEqual(0, results.Length);

            using EntityQuery emptySelectedMoveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            using EntityQuery emptyGridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            using EntityQuery emptyMapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            int feedbackCount = 0;
            var flushSystem = new RtsSelectionCommandResultFlushSystem();
            RtsSelectionCommandResultFlushSystem.Context context = CreateFlushContext(
                inputSystem,
                emptySelectedMoveQuery,
                emptyGridConfigQuery,
                emptyMapSurfaceQuery,
                _ => feedbackCount++,
                em,
                manager => manager.CreateEntity(typeof(RtsSelectionInputRequestQueueComponent)));

            Assert.DoesNotThrow(() => flushSystem.ProcessMoveCommandRequests(context));

            requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            Assert.AreEqual(0, requests.Length);
            Assert.AreEqual(0, results.Length);
            Assert.AreEqual(1, feedbackCount);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    private static bool TryGetNoClickedUnit(UnityEngine.Vector2 screenPosition, EntityManager em, out Entity entity)
    {
        entity = Entity.Null;
        return false;
    }

    private static RtsSelectionCommandResultFlushSystem.Context CreateFlushContext(
        RtsSelectionInputSystem inputSystem,
        EntityQuery selectedMoveQuery,
        EntityQuery gridConfigQuery,
        EntityQuery mapSurfaceQuery,
        System.Action<TacticalCommandResult> applyHudCommandResult,
        EntityManager em,
        System.Action<EntityManager> ensureEntityQueries = null)
    {
        return new RtsSelectionCommandResultFlushSystem.Context(
            inputSystem,
            new SelectionHudFeedbackSystem(),
            new SelectionOrderMarkerSystem(),
            new SelectedMoveOrderCommandSystem(),
            new AttackOrderCommandSystem(),
            new ScanIntelCommandSystem(),
            new TransportBoardingCommandSystem(),
            new UnitMoveOrderSystem(),
            new UnitTargetOrderSystem(),
            new UnitTransportCapacitySystem(),
            new UnitTransportBoardingQuerySystem(),
            new UnitTransportBoardingRuleSystem(),
            new UnitTransportApproachCellSystem(),
            new UnitTransportAirPickupSystem(),
            new SelectionStateSystem(),
            null,
            default,
            selectedMoveQuery,
            gridConfigQuery,
            mapSurfaceQuery,
            TryGetEntityManager,
            ensureEntityQueries,
            null,
            null,
            applyHudCommandResult,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        bool TryGetEntityManager(out EntityManager resolvedEntityManager)
        {
            resolvedEntityManager = em;
            return true;
        }
    }
}
#endif
