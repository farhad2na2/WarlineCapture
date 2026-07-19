using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class Aph806SelectionMoveAttackPlayModeTests
{
    private World _previousWorld;
    private World _world;
    private GameObject _cameraObject;

    [SetUp]
    public void SetUp()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World(nameof(Aph806SelectionMoveAttackPlayModeTests));
        World.DefaultGameObjectInjectionWorld = _world;
    }

    [TearDown]
    public void TearDown()
    {
        if (_world is { IsCreated: true })
            _world.Dispose();

        if (_cameraObject != null)
            Object.DestroyImmediate(_cameraObject);

        World.DefaultGameObjectInjectionWorld = _previousWorld;
        _world = null;
        _previousWorld = null;
    }

    [Test]
    public void SelectionMoveAttack_ProductionCommandGatewaysApplyDeterministicOrders()
    {
        EntityManager em = _world.EntityManager;
        var inputGateway = new RtsSelectionInputCompositionSystemHelper(Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager);
        Entity attacker = CreateAttacker(em);
        Entity target = CreateTarget(em);
        _cameraObject = new GameObject("APH806 Selection Camera");
        Camera worldCamera = _cameraObject.AddComponent<Camera>();
        _cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Assert.That(
            inputGateway.QueueSelectionRectangleRequest(
                RtsSelectionPointerRequestKind.SelectionRectCommitted,
                new Rect(-1000f, -1000f, 100000f, 100000f),
                frame: 10,
                VisibleUnitSelectionCameraSystemHelper.Filter.All),
            Is.True,
            "The production selection gateway rejected the rectangle request.");

        Assert.That(inputGateway.TryGetPointerRequests(out _, out var pointerRequests), Is.True);
        Assert.That(pointerRequests.Length, Is.EqualTo(1));
        Assert.That(pointerRequests[0].Kind, Is.EqualTo(RtsSelectionPointerRequestKind.SelectionRectCommitted));

        var selectionState = new SelectionStateCompositionSystemHelper();
        bool selectionProcessed = new SelectionRectangleRequestCompositionSystemHelper().ProcessPendingRequests(
            em,
            pointerRequests,
            worldCamera,
            new SelectionUiReadModelLookup(),
            new VisibleUnitSelectionCameraSystemHelper(),
            selectionState,
            new FocusedUnitLifecycleCompositionSystemHelper(),
            new List<Entity>(),
            (_, _) => { },
            selectionState.CacheSelectedMoveEntities,
            (_, _) => { },
            _ => { },
            _ => { },
            () => { },
            _ => false);

        Assert.That(selectionProcessed, Is.True);
        Assert.That(inputGateway.TryGetPointerRequests(out _, out pointerRequests), Is.True);
        Assert.That(pointerRequests.Length, Is.EqualTo(0));
        Assert.That(em.HasComponent<SelectedUnitTag>(attacker), Is.True);
        Assert.That(selectionState.FocusedUnit, Is.EqualTo(attacker));
        Assert.That(selectionState.CachedSelectedMoveEntities, Does.Contain(attacker));

        int2 moveCell = new(6, 7);
        int moveRequestId = UnitMoveOrderRequestSystem.EnqueueImmediateMoveOrder(em, attacker, moveCell);
        UnitMoveOrderRequestSystem.ProcessPendingRequests(em);
        Assert.That(UnitMoveOrderRequestSystem.TryGetResult(em, moveRequestId, out var moveResult), Is.True);
        Assert.That(moveResult.Issued, Is.EqualTo(1));
        Assert.That(em.GetComponentData<UnitTarget>(attacker).Cell, Is.EqualTo(moveCell));
        Assert.That(em.HasComponent<ManualMoveOrderTag>(attacker), Is.True);

        Assert.That(
            inputGateway.QueueAttackCommandRequest(
                new Vector2(400f, 300f),
                target,
                explicitAttackTargetModeActive: true,
                frame: 20),
            Is.True,
            "The production attack gateway rejected the resolved target request.");

        SystemHandle attackSystem = _world.CreateSystem<AttackOrderCommandSystem>();
        attackSystem.Update(_world.Unmanaged);

        Assert.That(inputGateway.TryGetCommandBuffers(out _, out var requests, out var results), Is.True);
        Assert.That(requests.Length, Is.EqualTo(0));
        Assert.That(results.Length, Is.EqualTo(1));
        Assert.That(results[0].Accepted, Is.EqualTo(1));
        Assert.That(results[0].TargetEntity, Is.EqualTo(target));
        Assert.That(em.GetComponentData<EngageTarget>(attacker).Target, Is.EqualTo(target));
        Assert.That(em.HasComponent<SelectedUnitTag>(attacker), Is.True, "Orders must preserve selection.");
    }

    private static Entity CreateAttacker(EntityManager em)
    {
        Entity attacker = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(LocalTransform),
            typeof(LocalToWorld));
        em.SetComponentData(attacker, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(attacker, new UnitGrid { Cell = new int2(1, 1) });
        em.SetComponentData(attacker, new UnitMove { Speed = 4f, WalkSpeed = 4f, ArriveDistance = 0.1f });
        em.SetComponentData(attacker, new UnitCombat { CanAttack = 1 });
        em.SetComponentData(attacker, new UnitAttack { Range = 20f, Damage = 10, CooldownSeconds = 1f });
        em.SetComponentData(attacker, LocalTransform.FromPosition(new float3(1.5f, 0f, 1.5f)));
        em.SetComponentData(attacker, new LocalToWorld { Value = float4x4.Translate(new float3(1.5f, 0f, 1.5f)) });
        return attacker;
    }

    private static Entity CreateTarget(EntityManager em)
    {
        Entity target = em.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(LocalTransform));
        em.SetComponentData(target, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(target, new UnitGrid { Cell = new int2(9, 9) });
        em.SetComponentData(target, LocalTransform.FromPosition(new float3(9.5f, 0f, 9.5f)));
        return target;
    }
}
#endif
