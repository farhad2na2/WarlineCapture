using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class BaseBreachValidationTests
{
    private const string BuildingPlacementConfigPath = "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset";
    private const string InitialSpawnConfigPath = "Assets/Game/Configs/Scene/GameSubScene_InitialUnitsSpawner_Config.asset";
    private const string WallPrefabPath = "Assets/Game/Prefabs/Buildings/Wall_Dirt_Straight.prefab";
    private const string GatePrefabPath = "Assets/Game/Prefabs/Buildings/Building_Road_Barrier.prefab";

    [Test]
    public void BaseBreachResolver_PrefersRoadBarrierGateForTargetInsideEnemyWalls()
    {
        WithRuntimeBase((buildingPlacement, wallPrefab, gatePrefab) =>
        {
            SpawnThreeSidedWall(buildingPlacement, wallPrefab, ownerFactionId: 0);
            Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingPlacementFootprint(gatePrefab, false, out Vector2Int gateFootprint));
            Assert.IsTrue(buildingPlacement.TrySpawnRuntimeBuilding(
                gatePrefab,
                new Vector2Int(100 - gateFootprint.x / 2, 60),
                out _,
                out _,
                out _,
                ownerFactionId: 0));

            Assert.IsTrue(buildingPlacement.TryResolveBaseBreachTarget(
                attackerFactionId: 1,
                finalTarget: Entity.Null,
                finalTargetCell: new int2(100, 100),
                attackerCell: new int2(100, 40),
                out Entity breachTarget,
                out _,
                out _,
                out _));
            Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingCombatInfo(breachTarget, out bool isGate, out _, out byte ownerFactionId));
            Assert.IsTrue(isGate, "Enemy units should shoot the Road Barrier gate first when the target is inside a walled base.");
            Assert.AreEqual(0, ownerFactionId);
        });
    }

    [Test]
    public void BaseBreachResolver_FallsBackToWallWhenNoGateExists()
    {
        WithRuntimeBase((buildingPlacement, wallPrefab, gatePrefab) =>
        {
            SpawnThreeSidedWall(buildingPlacement, wallPrefab, ownerFactionId: 0);

            Assert.IsTrue(buildingPlacement.TryResolveBaseBreachTarget(
                attackerFactionId: 1,
                finalTarget: Entity.Null,
                finalTargetCell: new int2(100, 100),
                attackerCell: new int2(100, 40),
                out Entity breachTarget,
                out _,
                out _,
                out _));
            Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingCombatInfo(breachTarget, out bool isGate, out bool isWall, out byte ownerFactionId));
            Assert.IsFalse(isGate);
            Assert.IsTrue(isWall, "Enemy units should shoot a wall segment when no Road Barrier gate exists.");
            Assert.AreEqual(0, ownerFactionId);
        });
    }

    [Test]
    public void BaseBreachOrderSystem_PathsToFinalTargetAfterBreachDies()
    {
        using var world = new World("BaseBreachOrderValidation");
        EntityManager em = world.EntityManager;
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            Entity finalTarget = em.CreateEntity(typeof(LocalTransform), typeof(UnitHealth));
            em.SetComponentData(finalTarget, LocalTransform.FromPosition(new float3(10f, 0f, 10f)));
            em.SetComponentData(finalTarget, new UnitHealth { Current = 100, Max = 100 });

            Entity deadGate = em.CreateEntity(typeof(LocalTransform), typeof(UnitHealth));
            em.SetComponentData(deadGate, LocalTransform.FromPosition(new float3(5f, 0f, 5f)));
            em.SetComponentData(deadGate, new UnitHealth { Current = 0, Max = 100 });

            Entity attacker = em.CreateEntity(typeof(EngageTarget), typeof(BaseBreachOrder));
            em.SetComponentData(attacker, new EngageTarget
            {
                Target = deadGate,
                Cell = new int2(5, 5),
                Position = new float3(5f, 0f, 5f),
                IsCommanded = 1
            });
            em.SetComponentData(attacker, new BaseBreachOrder
            {
                FinalTarget = finalTarget,
                FinalCell = new int2(10, 10),
                FinalPosition = new float3(10f, 0f, 10f),
                BreachTarget = deadGate,
                BreachCell = new int2(5, 5),
                BreachPosition = new float3(5f, 0f, 5f),
                Stage = BaseBreachOrder.StageAttackingBreach,
                IsCommanded = 1
            });

            SystemHandle system = world.CreateSystem<BaseBreachOrderSystem>();
            world.SetTime(new TimeData(0.1d, 0.1f));
            system.Update(world.Unmanaged);

            Assert.IsFalse(em.HasComponent<EngageTarget>(attacker));
            Assert.IsTrue(em.HasComponent<UnitPathRequest>(attacker));
            Assert.AreEqual(new int2(10, 10), em.GetComponentData<UnitPathRequest>(attacker).Goal);
            Assert.IsTrue(em.HasComponent<BaseBreachOrder>(attacker));
            Assert.AreEqual(BaseBreachOrder.StageMovingToFinalTarget, em.GetComponentData<BaseBreachOrder>(attacker).Stage);

            em.RemoveComponent<UnitPathRequest>(attacker);
            system.Update(world.Unmanaged);

            EngageTarget restored = em.GetComponentData<EngageTarget>(attacker);
            Assert.AreEqual(finalTarget, restored.Target);
            Assert.AreEqual(new int2(10, 10), restored.Cell);
            Assert.IsFalse(em.HasComponent<BaseBreachOrder>(attacker));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void BaseBreachOrderSystem_IssuesFinalPathRequestsForMultipleAttackersAfterBreachDies()
    {
        World world = new("BaseBreachDestroyedGateMultiPathValidation");
        EntityManager em = world.EntityManager;
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeList<int2> pathPool = default;

        try
        {
            CreateGrid(em, 220, 220, out blockerCounts, out blocked, out occupied, out friendlyPassFactionIds, out pathPool);
            CreateStaticBlocker(em, new int2(60, 60), new int2(1, 81));
            CreateStaticBlocker(em, new int2(140, 60), new int2(1, 81));
            CreateStaticBlocker(em, new int2(60, 140), new int2(81, 1));
            CreateStaticBlocker(em, new int2(60, 60), new int2(36, 1));
            CreateStaticBlocker(em, new int2(105, 60), new int2(36, 1));

            Entity finalTarget = em.CreateEntity(typeof(LocalTransform), typeof(UnitHealth));
            em.SetComponentData(finalTarget, LocalTransform.FromPosition(new float3(100f, 0f, 100f)));
            em.SetComponentData(finalTarget, new UnitHealth { Current = 100, Max = 100 });

            Entity deadGate = em.CreateEntity(typeof(LocalTransform), typeof(UnitHealth));
            em.SetComponentData(deadGate, LocalTransform.FromPosition(new float3(100f, 0f, 60f)));
            em.SetComponentData(deadGate, new UnitHealth { Current = 0, Max = 100 });

            Entity[] attackers = new Entity[3];
            int2[] starts = { new(96, 48), new(100, 48), new(104, 48) };
            for (int i = 0; i < attackers.Length; i++)
            {
                Entity attacker = em.CreateEntity(
                    typeof(Faction),
                    typeof(UnitGrid),
                    typeof(UnitFootprint),
                    typeof(UnitMovementBehavior),
                    typeof(EngageTarget),
                    typeof(BaseBreachOrder));
                em.SetComponentData(attacker, new Faction { Id = 1 });
                em.SetComponentData(attacker, new UnitGrid { Cell = starts[i] });
                em.SetComponentData(attacker, new UnitFootprint { Size = new int2(1, 1) });
                em.SetComponentData(attacker, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
                em.SetComponentData(attacker, new EngageTarget
                {
                    Target = deadGate,
                    Cell = new int2(100, 60),
                    Position = new float3(100f, 0f, 60f),
                    IsCommanded = 1
                });
                em.SetComponentData(attacker, new BaseBreachOrder
                {
                    FinalTarget = finalTarget,
                    FinalCell = new int2(100, 100),
                    FinalPosition = new float3(100f, 0f, 100f),
                    BreachTarget = deadGate,
                    BreachCell = new int2(100, 60),
                    BreachPosition = new float3(100f, 0f, 60f),
                    Stage = BaseBreachOrder.StageAttackingBreach,
                    IsCommanded = 1
                });
                attackers[i] = attacker;
            }

            SystemHandle blockerSystem = world.CreateSystem<StaticGridBlockerUpdateSystem>();
            blockerSystem.Update(world.Unmanaged);
            SystemHandle breachSystem = world.CreateSystem<BaseBreachOrderSystem>();
            breachSystem.Update(world.Unmanaged);

            for (int i = 0; i < attackers.Length; i++)
            {
                Assert.IsFalse(em.HasComponent<EngageTarget>(attackers[i]), $"Attacker {i} should stop direct wall-line combat movement after the gate is destroyed.");
                Assert.IsTrue(em.HasComponent<UnitPathRequest>(attackers[i]), $"Attacker {i} should receive a final-target path request after the gate is destroyed.");
                Assert.AreEqual(new int2(100, 100), em.GetComponentData<UnitPathRequest>(attackers[i]).Goal);
                Assert.AreEqual(BaseBreachOrder.StageMovingToFinalTarget, em.GetComponentData<BaseBreachOrder>(attackers[i]).Stage);
            }
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (world.IsCreated)
                world.Dispose();
            if (pathPool.IsCreated)
                pathPool.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void BaseBreachOrderSystem_ReissuesEnemyBreachPathUntilApproachReached()
    {
        using var world = new World("BaseBreachOrderPathRetryValidation");
        EntityManager em = world.EntityManager;
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            Entity finalTarget = em.CreateEntity(typeof(LocalTransform), typeof(UnitHealth));
            em.SetComponentData(finalTarget, LocalTransform.FromPosition(new float3(40f, 0f, 40f)));
            em.SetComponentData(finalTarget, new UnitHealth { Current = 100, Max = 100 });

            Entity gate = em.CreateEntity(typeof(LocalTransform), typeof(UnitHealth));
            em.SetComponentData(gate, LocalTransform.FromPosition(new float3(30f, 0f, 30f)));
            em.SetComponentData(gate, new UnitHealth { Current = 100, Max = 100 });

            Entity attacker = em.CreateEntity(typeof(UnitGrid), typeof(BaseBreachOrder));
            em.SetComponentData(attacker, new UnitGrid { Cell = new int2(10, 10) });
            em.SetComponentData(attacker, new BaseBreachOrder
            {
                FinalTarget = finalTarget,
                FinalCell = new int2(40, 40),
                FinalPosition = new float3(40f, 0f, 40f),
                BreachTarget = gate,
                BreachCell = new int2(30, 30),
                BreachPosition = new float3(30f, 0f, 30f),
                Stage = BaseBreachOrder.StageMovingToEnemyBreach,
                IsCommanded = 1
            });

            SystemHandle system = world.CreateSystem<BaseBreachOrderSystem>();
            world.SetTime(new TimeData(0.1d, 0.1f));
            system.Update(world.Unmanaged);

            Assert.IsFalse(em.HasComponent<EngageTarget>(attacker), "A breach unit must not start attacking before reaching the enemy gate/wall approach cell.");
            Assert.IsTrue(em.HasComponent<UnitPathRequest>(attacker));
            Assert.IsTrue(em.HasComponent<UnitTarget>(attacker));
            Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(attacker));
            Assert.AreEqual(new int2(30, 30), em.GetComponentData<UnitPathRequest>(attacker).Goal);
            Assert.AreEqual(BaseBreachOrder.StageMovingToEnemyBreach, em.GetComponentData<BaseBreachOrder>(attacker).Stage);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void AICombatOrderSystem_RedirectsSquadToGateBeforeInteriorTarget()
    {
        WithRuntimeBase((buildingPlacement, wallPrefab, gatePrefab) =>
        {
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            RuntimeGameplayStateTestHelper.SetBuildingPlacement(em, buildingPlacement);
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            SpawnThreeSidedWall(buildingPlacement, wallPrefab, ownerFactionId: 0);
            Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingPlacementFootprint(gatePrefab, false, out Vector2Int gateFootprint));
            Assert.IsTrue(buildingPlacement.TrySpawnRuntimeBuilding(
                gatePrefab,
                new Vector2Int(100 - gateFootprint.x / 2, 60),
                out _,
                out _,
                out _,
                ownerFactionId: 0));

            Entity target = CreateTarget(em, 0, new int2(100, 100), new float3(100f, 0f, 100f));
            Entity attacker = CreateAttacker(em, 1, new int2(100, 40), new float3(100f, 0f, 40f));
            Entity squadEntity = em.CreateEntity(typeof(AISquad));
            em.SetComponentData(squadEntity, new AISquad
            {
                SquadId = 7,
                FactionId = 1,
                Purpose = (byte)AISquadPurpose.Attack,
                TargetFactionId = 0,
                TargetKind = (byte)AITargetKind.Threat,
                TargetEntity = target,
                RallyCell = new int2(100, 40),
                TargetCell = new int2(100, 100),
                TargetScore = 150,
                MinUnits = 1,
                MaxUnits = 4,
                LastOrderTime = -999f,
                LastLogTime = -999f
            });
            DynamicBuffer<AISquadUnit> members = em.AddBuffer<AISquadUnit>(squadEntity);
            members.Add(new AISquadUnit { Unit = attacker });

            SystemHandle system = World.DefaultGameObjectInjectionWorld.CreateSystem<AICombatOrderSystem>();
            system.Update(World.DefaultGameObjectInjectionWorld.Unmanaged);

            Assert.IsFalse(em.HasComponent<EngageTarget>(attacker));
            Assert.IsTrue(em.HasComponent<UnitPathRequest>(attacker));
            Assert.IsTrue(em.HasComponent<BaseBreachOrder>(attacker));
            BaseBreachOrder routeOrder = em.GetComponentData<BaseBreachOrder>(attacker);
            Assert.AreEqual(BaseBreachOrder.StageMovingToEnemyBreach, routeOrder.Stage);
            Assert.AreEqual(target, routeOrder.FinalTarget);
            Assert.AreEqual(routeOrder.BreachCell, em.GetComponentData<UnitPathRequest>(attacker).Goal);

            em.SetComponentData(attacker, new UnitGrid { Cell = routeOrder.BreachCell });
            SystemHandle routeSystem = World.DefaultGameObjectInjectionWorld.CreateSystem<BaseBreachOrderSystem>();
            routeSystem.Update(World.DefaultGameObjectInjectionWorld.Unmanaged);

            Assert.IsTrue(em.HasComponent<EngageTarget>(attacker));
            EngageTarget engage = em.GetComponentData<EngageTarget>(attacker);
            Assert.AreNotEqual(target, engage.Target);
            Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingCombatInfo(engage.Target, out bool isGate, out _, out byte ownerFactionId));
            Assert.IsTrue(isGate);
            Assert.AreEqual(0, ownerFactionId);
            Assert.AreEqual(BaseBreachOrder.StageAttackingBreach, em.GetComponentData<BaseBreachOrder>(attacker).Stage);
        });
    }

    [Test]
    public void AICombatOrderSystem_RoutesToEnemyGateApproachThroughFriendlyGatePassability()
    {
        WithRuntimeBase((buildingPlacement, wallPrefab, gatePrefab) =>
        {
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            RuntimeGameplayStateTestHelper.SetBuildingPlacement(em, buildingPlacement);
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            int2 friendlyGateCell = SpawnThreeSidedWallWithGate(
                buildingPlacement,
                wallPrefab,
                gatePrefab,
                ownerFactionId: 1,
                left: 20,
                right: 100,
                bottom: 60,
                top: 140);
            SpawnThreeSidedWallWithGate(
                buildingPlacement,
                wallPrefab,
                gatePrefab,
                ownerFactionId: 0,
                left: 150,
                right: 230,
                bottom: 60,
                top: 140);

            Entity target = CreateTarget(em, 0, new int2(190, 100), new float3(190f, 0f, 100f));
            Entity attacker = CreateAttacker(em, 1, new int2(60, 100), new float3(60f, 0f, 100f));
            Entity squadEntity = CreateAttackSquad(em, 8, 1, 0, target, new int2(60, 100), new int2(190, 100));
            em.AddBuffer<AISquadUnit>(squadEntity).Add(new AISquadUnit { Unit = attacker });

            SystemHandle combatSystem = World.DefaultGameObjectInjectionWorld.CreateSystem<AICombatOrderSystem>();
            combatSystem.Update(World.DefaultGameObjectInjectionWorld.Unmanaged);

            Assert.IsFalse(em.HasComponent<EngageTarget>(attacker));
            Assert.IsTrue(em.HasComponent<UnitPathRequest>(attacker));
            Assert.AreNotEqual(friendlyGateCell, em.GetComponentData<UnitPathRequest>(attacker).Goal);
            Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(attacker));
            Assert.IsTrue(em.HasComponent<BaseBreachOrder>(attacker));
            BaseBreachOrder routeOrder = em.GetComponentData<BaseBreachOrder>(attacker);
            Assert.AreEqual(BaseBreachOrder.StageMovingToEnemyBreach, routeOrder.Stage);
            Assert.AreEqual(target, routeOrder.FinalTarget);
            Assert.AreEqual(routeOrder.BreachCell, em.GetComponentData<UnitPathRequest>(attacker).Goal);

            SystemHandle routeSystem = World.DefaultGameObjectInjectionWorld.CreateSystem<BaseBreachOrderSystem>();
            em.SetComponentData(attacker, new UnitGrid { Cell = routeOrder.BreachCell });
            routeSystem.Update(World.DefaultGameObjectInjectionWorld.Unmanaged);

            Assert.IsTrue(em.HasComponent<EngageTarget>(attacker));
            EngageTarget engage = em.GetComponentData<EngageTarget>(attacker);
            Assert.AreNotEqual(target, engage.Target);
            Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingCombatInfo(engage.Target, out bool isGate, out _, out byte ownerFactionId));
            Assert.IsTrue(isGate);
            Assert.AreEqual(0, ownerFactionId);
            Assert.IsFalse(em.HasComponent<UnitPathRequest>(attacker));
            Assert.AreEqual(BaseBreachOrder.StageAttackingBreach, em.GetComponentData<BaseBreachOrder>(attacker).Stage);
        });
    }

    [Test]
    public void RuntimeRoadBarrier_AllowsOwnerFactionThroughBlockedGateCells()
    {
        WithRuntimeBase((buildingPlacement, wallPrefab, gatePrefab) =>
        {
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            Assert.IsTrue(buildingPlacement.TrySpawnRuntimeBuilding(
                gatePrefab,
                new Vector2Int(80, 60),
                out _,
                out Vector2Int gateOrigin,
                out Vector2Int gateFootprint,
                ownerFactionId: 1));

            SystemHandle blockerSystem = World.DefaultGameObjectInjectionWorld.CreateSystem<StaticGridBlockerUpdateSystem>();
            blockerSystem.Update(World.DefaultGameObjectInjectionWorld.Unmanaged);

            using EntityQuery gridQuery = em.CreateEntityQuery(typeof(GridConfig));
            Entity gridEntity = gridQuery.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            DynamicBlockerData blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
            DynamicOccupancyData occupancyData = em.GetComponentData<DynamicOccupancyData>(gridEntity);
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            int2 gateCell = new(
                gateOrigin.x + Mathf.Max(1, gateFootprint.x) / 2,
                gateOrigin.y + Mathf.Max(1, gateFootprint.y) / 2);
            int gateIndex = GridUtils.CellToIndex(gateCell, grid.Width);

            Assert.IsTrue(blockerData.Blocked.IsSet(gateIndex), "The road barrier must still block non-friendly pathing.");
            Assert.AreEqual(1, blockerData.FriendlyPassFactionIds[gateIndex], "The road barrier pass faction must match the owning faction.");
            Assert.IsTrue(UnitFootprintUtility.CanPlace(
                grid,
                walkable,
                blockerData.Blocked,
                blockerData.FriendlyPassFactionIds,
                occupancyData.Occupied,
                gateCell,
                new int2(1, 1),
                new int2(80, 50),
                1));
            Assert.IsFalse(UnitFootprintUtility.CanPlace(
                grid,
                walkable,
                blockerData.Blocked,
                blockerData.FriendlyPassFactionIds,
                occupancyData.Occupied,
                gateCell,
                new int2(1, 1),
                new int2(80, 50),
                0));
        });
    }

    [Test]
    public void RuntimeRoadBarrier_StartsVisuallyClosed()
    {
        WithRuntimeBase((buildingPlacement, wallPrefab, gatePrefab, runtimeRoot) =>
        {
            Transform prefabDoor = FindDescendantByName(gatePrefab.transform, "Door_Z");
            Assert.NotNull(prefabDoor);
            float authoredOpenZ = NormalizeSignedAngle(prefabDoor.localEulerAngles.z);

            Assert.IsTrue(buildingPlacement.TrySpawnRuntimeBuilding(
                gatePrefab,
                new Vector2Int(80, 60),
                out _,
                ownerFactionId: 1));

            Transform runtimeDoor = FindDescendantByName(runtimeRoot.transform, "Door_Z");
            Assert.NotNull(runtimeDoor);
            Assert.AreEqual(0f, NormalizeSignedAngle(runtimeDoor.localEulerAngles.z), 0.1f);
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(0f, authoredOpenZ)), 20f, "The prefab should still define a visible open angle for Door_Z.");
        });
    }

    [Test]
    public void RuntimeRoadBarrier_OpensForNearbyOwnerFactionUnit()
    {
        WithRuntimeBase((buildingPlacement, wallPrefab, gatePrefab, runtimeRoot) =>
        {
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            Assert.IsTrue(buildingPlacement.TrySpawnRuntimeBuilding(
                gatePrefab,
                new Vector2Int(80, 60),
                out int buildingId,
                out Vector2Int gateOrigin,
                out Vector2Int gateFootprint,
                ownerFactionId: 0));

            Transform runtimeDoor = FindDescendantByName(runtimeRoot.transform, "Door_Z");
            Assert.NotNull(runtimeDoor);
            float closedZ = NormalizeSignedAngle(runtimeDoor.localEulerAngles.z);
            buildingPlacement.UpdateRoadBarrierDoorsForTests(1f);
            Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingDoorOpen01ForTests(buildingId, out float idleOpen01));
            Assert.AreEqual(0f, idleOpen01, 0.01f, "A road barrier should not open because of its own runtime building combat entity.");

            int2 gateCenter = new(
                gateOrigin.x + Mathf.Max(1, gateFootprint.x) / 2,
                gateOrigin.y + Mathf.Max(1, gateFootprint.y) / 2);
            CreateDoorTriggerUnit(em, 0, gateCenter + new int2(0, 6));

            buildingPlacement.UpdateRoadBarrierDoorsForTests(1f);

            Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingDoorOpen01ForTests(buildingId, out float open01));
            Assert.Greater(open01, 0.5f, "A road barrier should open when an owning-faction unit approaches it.");
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(closedZ, NormalizeSignedAngle(runtimeDoor.localEulerAngles.z))), 20f,
                "The road barrier Door_Z transform should visibly rotate open, not only update an internal open value.");
        });
    }

    [Test]
    public void RuntimeRoadBarrier_DestroyedGateClearsBlockerAndStopsFutureBreachRedirects()
    {
        WithRuntimeBase((buildingPlacement, wallPrefab, gatePrefab) =>
        {
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            RuntimeGameplayStateTestHelper.SetBuildingPlacement(em, buildingPlacement);
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            SpawnThreeSidedWall(buildingPlacement, wallPrefab, ownerFactionId: 0);
            Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingPlacementFootprint(gatePrefab, false, out Vector2Int gateFootprint));
            Vector2Int gateOrigin = new(100 - gateFootprint.x / 2, 60);
            Assert.IsTrue(buildingPlacement.TrySpawnRuntimeBuilding(
                gatePrefab,
                gateOrigin,
                out int gateBuildingId,
                out Vector2Int actualGateOrigin,
                out Vector2Int actualGateFootprint,
                ownerFactionId: 0));

            SystemHandle blockerSystem = World.DefaultGameObjectInjectionWorld.CreateSystem<StaticGridBlockerUpdateSystem>();
            blockerSystem.Update(World.DefaultGameObjectInjectionWorld.Unmanaged);

            using EntityQuery gridQuery = em.CreateEntityQuery(typeof(GridConfig));
            Entity gridEntity = gridQuery.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            DynamicBlockerData blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
            DynamicOccupancyData occupancyData = em.GetComponentData<DynamicOccupancyData>(gridEntity);
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            int2 gateCell = new(
                actualGateOrigin.x + Mathf.Max(1, actualGateFootprint.x) / 2,
                actualGateOrigin.y + Mathf.Max(1, actualGateFootprint.y) / 2);
            int gateIndex = GridUtils.CellToIndex(gateCell, grid.Width);
            Assert.IsTrue(blockerData.Blocked.IsSet(gateIndex), "The live enemy gate should block attackers before it is destroyed.");

            Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingEntitiesForTests(gateBuildingId, out Entity gateCombatEntity, out _));
            Assert.AreNotEqual(Entity.Null, gateCombatEntity);
            UnitHealth gateHealth = em.GetComponentData<UnitHealth>(gateCombatEntity);
            gateHealth.Current = 0;
            em.SetComponentData(gateCombatEntity, gateHealth);

            buildingPlacement.SyncDestroyedRuntimeBuildingCombatEntitiesForTests();
            blockerSystem.Update(World.DefaultGameObjectInjectionWorld.Unmanaged);
            grid = em.GetComponentData<GridConfig>(gridEntity);
            blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
            occupancyData = em.GetComponentData<DynamicOccupancyData>(gridEntity);
            walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();

            Assert.IsTrue(buildingPlacement.IsRuntimeBuildingDestroyedForTests(gateBuildingId));
            Assert.IsFalse(blockerData.Blocked.IsSet(gateIndex), "A destroyed gate must immediately clear its static path blocker so the squad can use the breach.");
            Assert.IsTrue(UnitFootprintUtility.CanPlace(
                grid,
                walkable,
                blockerData.Blocked,
                blockerData.FriendlyPassFactionIds,
                occupancyData.Occupied,
                gateCell,
                new int2(1, 1),
                new int2(100, 40),
                1));

            Entity target = CreateTarget(em, 0, new int2(100, 100), new float3(100f, 0f, 100f));
            Assert.IsFalse(buildingPlacement.TryResolveBaseBreachTarget(
                attackerFactionId: 1,
                finalTarget: target,
                finalTargetCell: new int2(100, 100),
                attackerCell: new int2(100, 40),
                out _,
                out _,
                out _,
                out _),
                "Once a perimeter gate is destroyed, future orders should path through the open breach instead of redirecting to another gate or wall.");
        });
    }

    [Test]
    public void UnitPathfinding_RoutesOwnerFactionThroughFriendlyRoadBarrierGate()
    {
        World world = new("FriendlyGatePathfindingValidation");
        EntityManager em = world.EntityManager;
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeList<int2> pathPool = default;
        bool previousPlayRequested = InitialUnitsRuntimeState.PlayRequested;

        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            CreateGrid(em, 220, 220, out blockerCounts, out blocked, out occupied, out friendlyPassFactionIds, out pathPool);
            CreateStaticBlocker(em, new int2(60, 60), new int2(1, 81));
            CreateStaticBlocker(em, new int2(140, 60), new int2(1, 81));
            CreateStaticBlocker(em, new int2(60, 140), new int2(81, 1));
            CreateStaticBlocker(em, new int2(60, 60), new int2(36, 1));
            CreateStaticBlocker(em, new int2(105, 60), new int2(36, 1));
            CreateStaticBlocker(em, new int2(96, 60), new int2(9, 1), 0);

            Entity unit = CreatePathfindingUnit(em, 0, new int2(100, 100), new int2(160, 160));

            SystemHandle blockerSystem = world.CreateSystem<StaticGridBlockerUpdateSystem>();
            blockerSystem.Update(world.Unmanaged);
            SystemHandle pathSystem = world.CreateSystem<UnitPathfindingSystem>();
            world.SetTime(new TimeData(0.1d, 0.1f));
            for (int i = 0; i < 6 && !em.HasComponent<UnitPathRange>(unit); i++)
                pathSystem.Update(world.Unmanaged);

            Assert.IsTrue(em.HasComponent<UnitPathRange>(unit), "Pathfinding should produce a path out of the base.");
            UnitPathRange range = em.GetComponentData<UnitPathRange>(unit);
            using EntityQuery gridQuery = em.CreateEntityQuery(typeof(GridConfig));
            Entity gridEntity = gridQuery.GetSingletonEntity();
            NativeList<int2> cells = em.GetComponentData<PathPoolData>(gridEntity).Cells;
            bool pathUsesGate = false;
            for (int i = 0; i < range.Length; i++)
            {
                int2 cell = cells[range.Start + i];
                if (cell.y == 60 && cell.x >= 96 && cell.x < 105)
                {
                    pathUsesGate = true;
                    break;
                }
            }

            Assert.IsTrue(pathUsesGate, "A player-owned gate must be treated as open path space for player units.");
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = previousPlayRequested;
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (world.IsCreated)
                world.Dispose();
            if (pathPool.IsCreated)
                pathPool.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void InitialBaseLayout_PathfindingAndDoorUseRealFriendlyGate()
    {
        BuildingPlacementSystemConfig placementConfig = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(BuildingPlacementConfigPath);
        InitialUnitsSpawnerAuthoringConfig spawnConfig = AssetDatabase.LoadAssetAtPath<InitialUnitsSpawnerAuthoringConfig>(InitialSpawnConfigPath);
        Assert.NotNull(placementConfig);
        Assert.NotNull(spawnConfig);

        World world = new("InitialBaseFriendlyGatePathValidation");
        EntityManager em = world.EntityManager;
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeList<int2> pathPool = default;
        Entity gridEntity = Entity.Null;
        GameObject runtimeRoot = null;
        BuildingGameplayTestHarness buildingPlacement = null;
        bool previousPlayRequested = InitialUnitsRuntimeState.PlayRequested;

        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            CreateGrid(em, 720, 360, out blockerCounts, out blocked, out occupied, out friendlyPassFactionIds, out pathPool);
            runtimeRoot = new GameObject("InitialBaseFriendlyGatePath_Root");
            buildingPlacement = new BuildingGameplayTestHarness();
            buildingPlacement.Init(placementConfig, null, runtimeRoot.transform, null, null, null, null);

            Vector2Int anchor = new(220, 180);
            var gateRects = new List<RectInt>();
            var gateBuildingIds = new List<int>();
            SpawnActualInitialBase(buildingPlacement, placementConfig, spawnConfig, anchor, 0, gateRects, gateBuildingIds);

            SystemHandle blockerSystem = world.CreateSystem<StaticGridBlockerUpdateSystem>();
            blockerSystem.Update(world.Unmanaged);
            SystemHandle occupancySystem = world.CreateSystem<DynamicOccupancyRebuildSystem>();
            occupancySystem.Update(world.Unmanaged);
            occupied = default;

            gridEntity = em.CreateEntityQuery(typeof(GridConfig)).GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            DynamicBlockerData blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
            DynamicOccupancyData occupancyData = em.GetComponentData<DynamicOccupancyData>(gridEntity);
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            for (int i = 0; i < gateRects.Count; i++)
            {
                RectInt gate = gateRects[i];
                int2 gateCell = new(gate.xMin + gate.width / 2, gate.yMin + gate.height / 2);
                int gateIndex = GridUtils.CellToIndex(gateCell, grid.Width);
                Assert.IsFalse(
                    occupancyData.Occupied.IsSet(gateIndex),
                    $"Actual initial-base gate {i} center must not be occupied by the runtime gate combat entity. rect={gate}");
                Assert.IsTrue(
                    UnitFootprintUtility.CanPlace(grid, walkable, blockerData.Blocked, blockerData.FriendlyPassFactionIds, occupancyData.Occupied, gateCell, new int2(1, 1), gateCell, 0),
                    $"Actual initial-base gate {i} center should be passable for owning faction. rect={gate}");
            }
            int2 startCell = FindFreeCellNear(grid, walkable, blockerData.Blocked, blockerData.FriendlyPassFactionIds, occupancyData.Occupied, anchor + new Vector2Int(-72, -8), 0);
            int2 goalCell = FindFreeCellNear(grid, walkable, blockerData.Blocked, blockerData.FriendlyPassFactionIds, occupancyData.Occupied, anchor + new Vector2Int(spawnConfig.BaseHalfWidthCells + 70, 42), 0);
            Entity unit = CreatePathfindingUnit(em, 0, startCell, goalCell);

            for (int i = 0; i < gateBuildingIds.Count; i++)
            {
                RectInt gate = gateRects[i];
                CreateDoorTriggerUnit(em, 0, new int2(gate.xMin + gate.width / 2, gate.yMin + gate.height / 2));
                buildingPlacement.UpdateRoadBarrierDoorsForTests(1f);
                Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingDoorOpen01ForTests(gateBuildingIds[i], out float open01));
                Assert.Greater(open01, 0.5f, $"Actual initial-base gate {i} should open for a nearby owning-faction unit.");
            }

            SystemHandle pathSystem = world.CreateSystem<UnitPathfindingSystem>();
            world.SetTime(new TimeData(0.1d, 0.1f));
            for (int i = 0; i < 8 && !em.HasComponent<UnitPathRange>(unit); i++)
                pathSystem.Update(world.Unmanaged);

            Assert.IsTrue(em.HasComponent<UnitPathRange>(unit), "Actual initial-base pathfinding should produce a route out of the base.");
            UnitPathRange range = em.GetComponentData<UnitPathRange>(unit);
            NativeList<int2> cells = em.GetComponentData<PathPoolData>(gridEntity).Cells;
            Assert.IsTrue(
                PathUsesAnyRect(cells, range, gateRects),
                $"Actual initial-base path must leave through one of the friendly road barrier gate footprints, not a wall/corner. start={startCell} goal={goalCell} gates={FormatRects(gateRects)} path={FormatPath(cells, range)}");
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = previousPlayRequested;
            buildingPlacement?.Dispose();
            if (runtimeRoot != null)
                Object.DestroyImmediate(runtimeRoot);
            if (world.IsCreated)
            {
                if (gridEntity != Entity.Null &&
                    em.Exists(gridEntity) &&
                    em.HasComponent<DynamicOccupancyData>(gridEntity))
                {
                    DynamicOccupancyData currentOccupancy = em.GetComponentData<DynamicOccupancyData>(gridEntity);
                    if (currentOccupancy.Occupied.IsCreated)
                    {
                        currentOccupancy.Occupied.Dispose();
                        currentOccupancy.Occupied = default;
                        em.SetComponentData(gridEntity, currentOccupancy);
                    }
                }
                world.Dispose();
            }
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (pathPool.IsCreated)
                pathPool.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void InitialBaseLayout_AttackRouteToEnemyBaseUsesPlayerGate()
    {
        BuildingPlacementSystemConfig placementConfig = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(BuildingPlacementConfigPath);
        InitialUnitsSpawnerAuthoringConfig spawnConfig = AssetDatabase.LoadAssetAtPath<InitialUnitsSpawnerAuthoringConfig>(InitialSpawnConfigPath);
        Assert.NotNull(placementConfig);
        Assert.NotNull(spawnConfig);

        World world = new("InitialBaseAttackGateRouteValidation");
        EntityManager em = world.EntityManager;
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeList<int2> pathPool = default;
        Entity gridEntity = Entity.Null;
        GameObject runtimeRoot = null;
        BuildingGameplayTestHarness buildingPlacement = null;
        bool previousPlayRequested = InitialUnitsRuntimeState.PlayRequested;

        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            CreateGrid(em, 720, 360, out blockerCounts, out blocked, out occupied, out friendlyPassFactionIds, out pathPool);
            runtimeRoot = new GameObject("InitialBaseAttackGateRoute_Root");
            buildingPlacement = new BuildingGameplayTestHarness();
            buildingPlacement.Init(placementConfig, null, runtimeRoot.transform, null, null, null, null);

            Assert.IsTrue(TryGetFactionSpawnCell(spawnConfig, 0, out Vector2Int playerAnchor));
            Assert.IsTrue(TryGetFactionSpawnCell(spawnConfig, 1, out Vector2Int enemyAnchor));
            var playerGateRects = new List<RectInt>();
            var playerGateIds = new List<int>();
            var enemyGateRects = new List<RectInt>();
            var enemyGateIds = new List<int>();
            SpawnActualInitialBase(buildingPlacement, placementConfig, spawnConfig, playerAnchor, 0, playerGateRects, playerGateIds);
            SpawnActualInitialBase(buildingPlacement, placementConfig, spawnConfig, enemyAnchor, 1, enemyGateRects, enemyGateIds);

            SystemHandle blockerSystem = world.CreateSystem<StaticGridBlockerUpdateSystem>();
            blockerSystem.Update(world.Unmanaged);
            SystemHandle occupancySystem = world.CreateSystem<DynamicOccupancyRebuildSystem>();
            occupancySystem.Update(world.Unmanaged);
            occupied = default;

            gridEntity = em.CreateEntityQuery(typeof(GridConfig)).GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            DynamicBlockerData blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
            DynamicOccupancyData occupancyData = em.GetComponentData<DynamicOccupancyData>(gridEntity);
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();

            int2 startCell = FindFreeCellNear(grid, walkable, blockerData.Blocked, blockerData.FriendlyPassFactionIds, occupancyData.Occupied, playerAnchor + new Vector2Int(-96, 6), 0);
            Entity target = FindEnemyInnerBuildingTarget(buildingPlacement, em, 1, new int2(enemyAnchor.x, enemyAnchor.y));
            Assert.AreNotEqual(Entity.Null, target, "The actual initial enemy base should expose a non-wall building combat target.");
            int2 targetCell = em.GetComponentData<UnitGrid>(target).Cell;
            Assert.IsTrue(buildingPlacement.TryResolveBaseBreachTarget(
                0,
                target,
                targetCell,
                startCell,
                out Entity breachTarget,
                out int2 breachCell,
                out _,
                out string reason));
            Assert.AreEqual("Gate", reason);
            Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingCombatInfo(breachTarget, out bool breachIsGate, out _, out byte breachOwnerFaction));
            Assert.IsTrue(breachIsGate);
            Assert.AreEqual(1, breachOwnerFaction);

            Entity attacker = CreatePathfindingUnit(em, 0, startCell, breachCell);
            SystemHandle pathSystem = world.CreateSystem<UnitPathfindingSystem>();
            world.SetTime(new TimeData(0.1d, 0.1f));
            for (int i = 0; i < 12 && !em.HasComponent<UnitPathRange>(attacker); i++)
                pathSystem.Update(world.Unmanaged);

            Assert.IsTrue(em.HasComponent<UnitPathRange>(attacker), "Actual initial-base attack pathfinding should produce a route to the enemy gate approach.");
            UnitPathRange range = em.GetComponentData<UnitPathRange>(attacker);
            NativeList<int2> cells = em.GetComponentData<PathPoolData>(gridEntity).Cells;
            Assert.IsTrue(
                PathUsesAnyRect(cells, range, playerGateRects),
                $"Actual attack route must leave through a player road-barrier gate, not a wall/corner. start={startCell} breach={breachCell} playerGates={FormatRects(playerGateRects)} enemyGates={FormatRects(enemyGateRects)} path={FormatPath(cells, range)}");
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = previousPlayRequested;
            buildingPlacement?.Dispose();
            if (runtimeRoot != null)
                Object.DestroyImmediate(runtimeRoot);
            if (world.IsCreated)
            {
                if (gridEntity != Entity.Null &&
                    em.Exists(gridEntity) &&
                    em.HasComponent<DynamicOccupancyData>(gridEntity))
                {
                    DynamicOccupancyData currentOccupancy = em.GetComponentData<DynamicOccupancyData>(gridEntity);
                    if (currentOccupancy.Occupied.IsCreated)
                    {
                        currentOccupancy.Occupied.Dispose();
                        currentOccupancy.Occupied = default;
                        em.SetComponentData(gridEntity, currentOccupancy);
                    }
                }
                world.Dispose();
            }
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (pathPool.IsCreated)
                pathPool.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void RuntimeWallSegments_RenderAlongRequestedAxis()
    {
        WithRuntimeBase((buildingPlacement, wallPrefab, _, runtimeRoot) =>
        {
            Assert.IsTrue(buildingPlacement.TrySpawnRuntimeWallSegment(
                wallPrefab,
                new Vector2Int(20, 20),
                rotateVertical: false,
                ownerFactionId: 0));
            Assert.IsTrue(buildingPlacement.TrySpawnRuntimeWallSegment(
                wallPrefab,
                new Vector2Int(40, 20),
                rotateVertical: true,
                ownerFactionId: 0));

            Transform buildingRoot = runtimeRoot.transform.Find("RuntimeBuildings");
            Assert.NotNull(buildingRoot);
            Assert.GreaterOrEqual(buildingRoot.childCount, 2);
            InvokeRuntimeBuildingLinks(buildingRoot);

            Bounds horizontalBounds = GetWorldRendererBounds(buildingRoot.GetChild(0));
            Bounds verticalBounds = GetWorldRendererBounds(buildingRoot.GetChild(1));
            Assert.Greater(horizontalBounds.size.x, horizontalBounds.size.z, "Horizontal wall visuals should be longer on world X than world Z.");
            Assert.Greater(verticalBounds.size.z, verticalBounds.size.x, "Vertical wall visuals should be longer on world Z than world X.");
        });
    }

    private static void SpawnThreeSidedWall(BuildingGameplayTestHarness buildingPlacement, GameObject wallPrefab, byte ownerFactionId)
    {
        SpawnThreeSidedWall(buildingPlacement, wallPrefab, ownerFactionId, 60, 140, 60, 140);
    }

    private static void SpawnThreeSidedWall(
        BuildingGameplayTestHarness buildingPlacement,
        GameObject wallPrefab,
        byte ownerFactionId,
        int left,
        int right,
        int bottom,
        int top)
    {
        Assert.Greater(buildingPlacement.TrySpawnRuntimeWallRun(wallPrefab, new Vector2Int(left, bottom), new Vector2Int(left, top), ownerFactionId), 0);
        Assert.Greater(buildingPlacement.TrySpawnRuntimeWallRun(wallPrefab, new Vector2Int(right, bottom), new Vector2Int(right, top), ownerFactionId), 0);
        Assert.Greater(buildingPlacement.TrySpawnRuntimeWallRun(wallPrefab, new Vector2Int(left, top), new Vector2Int(right, top), ownerFactionId), 0);
    }

    private static int2 SpawnThreeSidedWallWithGate(
        BuildingGameplayTestHarness buildingPlacement,
        GameObject wallPrefab,
        GameObject gatePrefab,
        byte ownerFactionId,
        int left,
        int right,
        int bottom,
        int top)
    {
        SpawnThreeSidedWall(buildingPlacement, wallPrefab, ownerFactionId, left, right, bottom, top);
        Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingPlacementFootprint(gatePrefab, false, out Vector2Int gateFootprint));
        int centerX = (left + right) / 2;
        Vector2Int gateOrigin = new(centerX - gateFootprint.x / 2, bottom);
        Assert.IsTrue(buildingPlacement.TrySpawnRuntimeBuilding(
            gatePrefab,
            gateOrigin,
            out _,
            out _,
            out _,
            ownerFactionId: ownerFactionId));

        return new int2(gateOrigin.x + Mathf.Max(1, gateFootprint.x) / 2, gateOrigin.y + Mathf.Max(1, gateFootprint.y) / 2);
    }

    private static Entity CreateAttacker(EntityManager em, byte factionId, int2 cell, float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(AIControlledTag),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(entity, new UnitAttack { Range = 4f, CooldownSeconds = 1f, Damage = 10 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateTarget(EntityManager em, byte factionId, int2 cell, float3 position)
    {
        Entity entity = em.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(UnitHealth), typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitHealth { Current = 500, Max = 500 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateDoorTriggerUnit(EntityManager em, byte factionId, int2 cell)
    {
        Entity entity = em.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(UnitFootprint));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        return entity;
    }

    private static Entity CreatePathfindingUnit(EntityManager em, byte factionId, int2 startCell, int2 goalCell)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMovementBehavior),
            typeof(UnitPathRequest),
            typeof(UnitTarget),
            typeof(ManualMoveOrderTag));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = startCell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitPathRequest { Goal = goalCell });
        em.SetComponentData(entity, new UnitTarget { Cell = goalCell });
        return entity;
    }

    private static Entity CreateStaticBlocker(EntityManager em, int2 origin, int2 size, byte? friendlyPassFactionId = null)
    {
        Entity entity = em.CreateEntity(typeof(UnitGrid), typeof(GridBlockerSize), typeof(StaticGridBlocker));
        em.SetComponentData(entity, new UnitGrid { Cell = origin });
        em.SetComponentData(entity, new GridBlockerSize { Size = size });
        if (friendlyPassFactionId.HasValue)
            em.AddComponentData(entity, new FriendlyPassGridBlocker { AllowedFactionId = friendlyPassFactionId.Value });
        return entity;
    }

    private static bool TryGetFactionSpawnCell(InitialUnitsSpawnerAuthoringConfig spawnConfig, byte factionId, out Vector2Int spawnCell)
    {
        spawnCell = default;
        if (spawnConfig == null || spawnConfig.Factions == null)
            return false;

        for (int i = 0; i < spawnConfig.Factions.Count; i++)
        {
            InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = spawnConfig.Factions[i];
            if (faction == null || faction.FactionId != factionId)
                continue;

            spawnCell = faction.SpawnCell;
            return true;
        }

        return false;
    }

    private static Entity FindEnemyInnerBuildingTarget(BuildingGameplayTestHarness buildingPlacement, EntityManager em, byte factionId, int2 baseCenter)
    {
        Entity best = Entity.Null;
        int bestScore = int.MaxValue;
        if (buildingPlacement == null)
            return best;

        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitHealth>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<RuntimeBuildingCombatTag>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.GetComponentData<Faction>(entity).Id != factionId ||
                em.GetComponentData<UnitHealth>(entity).Current <= 0 ||
                !buildingPlacement.TryGetRuntimeBuildingCombatInfo(entity, out bool isGate, out bool isWall, out _) ||
                isGate ||
                isWall)
            {
                continue;
            }

            int2 cell = em.GetComponentData<UnitGrid>(entity).Cell;
            int2 delta = cell - baseCenter;
            int score = delta.x * delta.x + delta.y * delta.y;
            if (score >= bestScore)
                continue;

            bestScore = score;
            best = entity;
        }

        return best;
    }

    private static void SpawnActualInitialBase(
        BuildingGameplayTestHarness buildingPlacement,
        BuildingPlacementSystemConfig placementConfig,
        InitialUnitsSpawnerAuthoringConfig spawnConfig,
        Vector2Int anchor,
        byte factionId,
        List<RectInt> gateRects,
        List<int> gateBuildingIds)
    {
        Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingPlacementFootprint(spawnConfig.BaseGatePrefab, false, out Vector2Int bottomGateFootprint));
        Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingPlacementFootprint(spawnConfig.BaseGatePrefab, true, out Vector2Int sideGateFootprint));
        Assert.IsTrue(buildingPlacement.TryGetRuntimeWallSegmentFootprint(spawnConfig.BaseWallPrefab, false, out Vector2Int bottomWallFootprint));
        Assert.IsTrue(buildingPlacement.TryGetRuntimeWallSegmentFootprint(spawnConfig.BaseWallPrefab, true, out Vector2Int sideWallFootprint));

        int gateHalfGap = InitialFactionBaseLayoutPlanner.CalculateGateHalfGap(bottomGateFootprint, sideGateFootprint, bottomWallFootprint, sideWallFootprint);
        var wallRuns = new List<InitialFactionBaseWallRun>();
        InitialFactionBaseLayoutPlanner.BuildWallRuns(spawnConfig.BaseHalfWidthCells, spawnConfig.BaseHalfHeightCells, gateHalfGap, wallRuns);
        var gateFlankWalls = new List<InitialFactionBaseGateFlankWall>();
        InitialFactionBaseLayoutPlanner.BuildGateFlankWalls(
            spawnConfig.BaseHalfWidthCells,
            spawnConfig.BaseHalfHeightCells,
            bottomGateFootprint,
            sideGateFootprint,
            bottomWallFootprint,
            sideWallFootprint,
            gateFlankWalls);
        var placements = new List<InitialFactionBasePlacement>();
        InitialFactionBaseLayoutPlanner.BuildPlacements(spawnConfig.BaseHalfWidthCells, spawnConfig.BaseHalfHeightCells, placements);

        for (int i = 0; i < wallRuns.Count; i++)
        {
            InitialFactionBaseWallRun run = wallRuns[i];
            Assert.Greater(
                buildingPlacement.TrySpawnRuntimeWallRun(spawnConfig.BaseWallPrefab, anchor + run.StartOffset, anchor + run.EndOffset, factionId),
                0,
                $"Wall run {i} should spawn for actual initial base validation.");
        }

        for (int i = 0; i < gateFlankWalls.Count; i++)
        {
            InitialFactionBaseGateFlankWall flank = gateFlankWalls[i];
            Assert.IsTrue(buildingPlacement.TrySpawnRuntimeWallSegment(
                spawnConfig.BaseWallPrefab,
                anchor + flank.OriginOffset,
                flank.RotateVertical,
                factionId,
                allowExistingWallOverlap: true));
        }

        for (int i = 0; i < placements.Count; i++)
        {
            InitialFactionBasePlacement placement = placements[i];
            GameObject prefab = placement.Kind == InitialFactionBasePlacementKind.Gate
                ? spawnConfig.BaseGatePrefab
                : FindSpawnablePrefab(placementConfig, placement.PrefabKey);
            Assert.NotNull(prefab, $"Missing prefab for actual initial base validation: {placement.PrefabKey}");

            Assert.IsTrue(buildingPlacement.TryGetRuntimeBuildingPlacementFootprint(prefab, placement.RotateVertical, out Vector2Int footprint));
            Vector2Int origin = InitialFactionBaseLayoutPlanner.ResolvePlacementOrigin(anchor, placement, footprint);
            Assert.IsTrue(buildingPlacement.TrySpawnRuntimeBuilding(
                prefab,
                origin,
                out int buildingId,
                out Vector2Int actualOrigin,
                out Vector2Int actualFootprint,
                ownerFactionId: factionId,
                rotateVertical: placement.RotateVertical));

            if (placement.Kind == InitialFactionBasePlacementKind.Gate)
            {
                gateBuildingIds.Add(buildingId);
                gateRects.Add(new RectInt(actualOrigin, actualFootprint));
            }
        }
    }

    private static int2 FindFreeCellNear(
        GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray blocked,
        NativeArray<byte> friendlyPassFactionIds,
        NativeBitArray occupied,
        Vector2Int preferred,
        byte factionId)
    {
        int2 center = new(preferred.x, preferred.y);
        for (int radius = 0; radius <= 80; radius++)
        {
            int steps = Mathf.Max(1, radius * 8);
            for (int step = 0; step < steps; step++)
            {
                int2 candidate = center + SquareRingOffset(radius, step);
                if (!GridUtils.InBounds(candidate, grid.Width, grid.Height))
                    continue;
                if (UnitFootprintUtility.CanPlace(grid, walkable, blocked, friendlyPassFactionIds, occupied, candidate, new int2(1, 1), center, factionId))
                    return candidate;
            }
        }

        Assert.Fail($"Could not find free cell near {preferred}.");
        return center;
    }

    private static bool PathUsesAnyRect(NativeList<int2> cells, UnitPathRange range, List<RectInt> rects)
    {
        for (int i = 0; i < range.Length; i++)
        {
            int2 cell = cells[range.Start + i];
            for (int r = 0; r < rects.Count; r++)
            {
                RectInt rect = rects[r];
                if (cell.x >= rect.xMin && cell.x < rect.xMax && cell.y >= rect.yMin && cell.y < rect.yMax)
                    return true;
            }
        }

        return false;
    }

    private static string FormatRects(List<RectInt> rects)
    {
        if (rects == null || rects.Count == 0)
            return "[]";

        System.Text.StringBuilder builder = new();
        builder.Append('[');
        for (int i = 0; i < rects.Count; i++)
        {
            if (i > 0)
                builder.Append(", ");
            RectInt rect = rects[i];
            builder.Append('(').Append(rect.xMin).Append(',').Append(rect.yMin).Append(' ')
                .Append(rect.width).Append('x').Append(rect.height).Append(')');
        }
        builder.Append(']');
        return builder.ToString();
    }

    private static string FormatPath(NativeList<int2> cells, UnitPathRange range)
    {
        System.Text.StringBuilder builder = new();
        builder.Append('[');
        int max = Mathf.Min(range.Length, 180);
        for (int i = 0; i < max; i++)
        {
            if (i > 0)
                builder.Append(", ");
            int2 cell = cells[range.Start + i];
            builder.Append('(').Append(cell.x).Append(',').Append(cell.y).Append(')');
        }
        if (range.Length > max)
            builder.Append(", ... len=").Append(range.Length);
        builder.Append(']');
        return builder.ToString();
    }

    private static int2 SquareRingOffset(int radius, int step)
    {
        if (radius <= 0)
            return int2.zero;

        int side = radius * 2;
        int perimeter = side * 4;
        step = ((step % perimeter) + perimeter) % perimeter;
        if (step < side)
            return new int2(-radius + step, -radius);
        if (step < side * 2)
            return new int2(radius, -radius + (step - side));
        if (step < side * 3)
            return new int2(radius - (step - (side * 2)), radius);

        return new int2(-radius, radius - (step - (side * 3)));
    }

    private static Entity CreateAttackSquad(EntityManager em, int squadId, byte factionId, byte targetFactionId, Entity target, int2 rallyCell, int2 targetCell)
    {
        Entity squadEntity = em.CreateEntity(typeof(AISquad));
        em.SetComponentData(squadEntity, new AISquad
        {
            SquadId = squadId,
            FactionId = factionId,
            Purpose = (byte)AISquadPurpose.Attack,
            TargetFactionId = targetFactionId,
            TargetKind = (byte)AITargetKind.Threat,
            TargetEntity = target,
            RallyCell = rallyCell,
            TargetCell = targetCell,
            TargetScore = 150,
            MinUnits = 1,
            MaxUnits = 4,
            LastOrderTime = -999f,
            LastLogTime = -999f
        });
        return squadEntity;
    }

    private static Bounds GetWorldRendererBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Assert.IsNotEmpty(renderers);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private static void InvokeRuntimeBuildingLinks(Transform root)
    {
        MethodInfo updateMethod = typeof(RuntimeBuildingEntityLink).GetMethod(
            "Update",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(updateMethod);

        RuntimeBuildingEntityLink[] links = root.GetComponentsInChildren<RuntimeBuildingEntityLink>(true);
        Assert.IsNotEmpty(links);
        for (int i = 0; i < links.Length; i++)
            updateMethod.Invoke(links[i], null);
    }

    private static Transform FindDescendantByName(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindDescendantByName(root.GetChild(i), name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static GameObject FindSpawnablePrefab(BuildingPlacementSystemConfig placementConfig, string prefabKey)
    {
        if (placementConfig == null || string.IsNullOrWhiteSpace(prefabKey))
            return null;

        string normalizedKey = NormalizePrefabKey(prefabKey);
        for (int i = 0; i < placementConfig.Spawnables.Count; i++)
        {
            GameObject prefab = placementConfig.Spawnables[i];
            if (prefab == null)
                continue;

            if (NormalizePrefabKey(prefab.name) == normalizedKey)
                return prefab;

            string path = AssetDatabase.GetAssetPath(prefab);
            if (!string.IsNullOrEmpty(path) &&
                NormalizePrefabKey(Path.GetFileNameWithoutExtension(path)) == normalizedKey)
                return prefab;
        }

        return null;
    }

    private static string NormalizePrefabKey(string key)
    {
        return string.IsNullOrWhiteSpace(key)
            ? string.Empty
            : key.Trim().ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
    }

    private static float NormalizeSignedAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }

    private static void WithRuntimeBase(System.Action<BuildingGameplayTestHarness, GameObject, GameObject> test)
    {
        WithRuntimeBase((buildingPlacement, wallPrefab, gatePrefab, _) => test(buildingPlacement, wallPrefab, gatePrefab));
    }

    private static void WithRuntimeBase(System.Action<BuildingGameplayTestHarness, GameObject, GameObject, GameObject> test)
    {
        BuildingPlacementSystemConfig placementConfig = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(BuildingPlacementConfigPath);
        GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WallPrefabPath);
        GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GatePrefabPath);
        Assert.NotNull(placementConfig);
        Assert.NotNull(wallPrefab);
        Assert.NotNull(gatePrefab);

        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World world = new("BaseBreachRuntimeValidation");
        World.DefaultGameObjectInjectionWorld = world;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeList<int2> pathPool = default;
        BuildingGameplayTestHarness buildingPlacement = null;
        GameObject runtimeRoot = null;

        try
        {
            CreateGrid(world.EntityManager, 280, 220, out blockerCounts, out blocked, out occupied, out friendlyPassFactionIds, out pathPool);
            runtimeRoot = new GameObject("BaseBreachRuntime_Root");
            buildingPlacement = new BuildingGameplayTestHarness();
            buildingPlacement.Init(placementConfig, null, runtimeRoot.transform, null, null, null, null);
            test(buildingPlacement, wallPrefab, gatePrefab, runtimeRoot);
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            buildingPlacement?.Dispose();
            if (runtimeRoot != null)
                Object.DestroyImmediate(runtimeRoot);
            if (world.IsCreated)
                world.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (pathPool.IsCreated)
                pathPool.Dispose();
        }
    }

    private static void CreateGrid(
        EntityManager em,
        int width,
        int height,
        out NativeArray<int> blockerCounts,
        out NativeBitArray blocked,
        out NativeBitArray occupied,
        out NativeArray<byte> friendlyPassFactionIds,
        out NativeList<int2> pathPool)
    {
        int gridSize = width * height;
        blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
        for (int i = 0; i < friendlyPassFactionIds.Length; i++)
            friendlyPassFactionIds[i] = byte.MaxValue;

        pathPool = new NativeList<int2>(1024, Allocator.Persistent);
        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerData), typeof(DynamicOccupancyData), typeof(PathPoolData));
        em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        em.SetComponentData(gridEntity, new DynamicBlockerData
        {
            GridSize = gridSize,
            Counts = blockerCounts,
            Blocked = blocked,
            FriendlyPassFactionIds = friendlyPassFactionIds
        });
        em.SetComponentData(gridEntity, new DynamicOccupancyData
        {
            GridSize = gridSize,
            Occupied = occupied
        });
        em.SetComponentData(gridEntity, new PathPoolData { Cells = pathPool });

        em.AddBuffer<GridWalkable>(gridEntity);
        em.AddBuffer<GridRoad>(gridEntity);
        em.AddBuffer<GridRoadSidewalk>(gridEntity);
        em.AddBuffer<GridRoadDirt>(gridEntity);
        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBuffer<GridRoad> roads = em.GetBuffer<GridRoad>(gridEntity);
        DynamicBuffer<GridRoadSidewalk> sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity);
        DynamicBuffer<GridRoadDirt> dirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        roads.ResizeUninitialized(gridSize);
        sidewalks.ResizeUninitialized(gridSize);
        dirtRoads.ResizeUninitialized(gridSize);
        for (int i = 0; i < gridSize; i++)
        {
            walkable[i] = new GridWalkable { Value = 1 };
            roads[i] = new GridRoad { Value = 0 };
            sidewalks[i] = new GridRoadSidewalk { Value = 0 };
            dirtRoads[i] = new GridRoadDirt { Value = 0 };
        }
    }
}
