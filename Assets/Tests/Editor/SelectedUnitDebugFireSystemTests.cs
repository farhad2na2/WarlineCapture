using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;
using Game.Runtime;

public sealed class SelectedUnitDebugFireSystemTests
{
    private World _world;

    public static void RunFocusedValidation()
    {
        try
        {
            RunTest(test => test.HoldingDebugFireCreatesInvisibleTargetAndEngageTarget());
            RunTest(test => test.ReleaseDebugFireRestoresPreviousEngageTargetAndDestroysDebugTarget());
            RunTest(test => test.DeselectingUnitCleansUpDebugFireState());
            RunTest(test => test.HoldingDebugFireForGroundMissileLauncherTargetsEnemyBaseBeyondNormalRange());
            RunTest(test => test.HoldingDebugFireForGroundMissileLauncherArmsMissileAttackDirectly());
            RunTest(test => test.HoldingDebugFireForGroundMissileLauncherCreatesMissileProjectileThroughCooldown());
            RunTest(test => test.HoldingDebugFireForAirMissileLauncherTargetsAlongBatteryDirection());
            RunTest(test => test.HoldingDebugFireForReloadingAirMissileLauncherCreatesDebugState());
            UnityEngine.Debug.Log("[SelectedUnitDebugFireFocusedValidation] result=Passed tests=8");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError("[SelectedUnitDebugFireFocusedValidation] result=Failed");
            throw;
        }
    }

    private static void RunTest(System.Action<SelectedUnitDebugFireSystemTests> test)
    {
        var fixture = new SelectedUnitDebugFireSystemTests();
        fixture.SetUp();
        try
        {
            test(fixture);
        }
        finally
        {
            fixture.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _world = new World("SelectedUnitDebugFireSystemTests");
    }

    [TearDown]
    public void TearDown()
    {
        if (_world != null && _world.IsCreated)
            _world.Dispose();
    }

    [Test]
    public void HoldingDebugFireCreatesInvisibleTargetAndEngageTarget()
    {
        EntityManager em = _world.EntityManager;
        GridConfig grid = CreateGrid(em);
        Entity unit = CreateSelectedAttackUnit(em, new float3(3f, 0f, 3f));

        SelectedUnitDebugFireSystem.ApplyDebugFire(em, grid, true);

        Assert.IsTrue(em.HasComponent<SelectedUnitDebugFireState>(unit));
        Assert.IsTrue(em.HasComponent<EngageTarget>(unit));

        SelectedUnitDebugFireState debugState = em.GetComponentData<SelectedUnitDebugFireState>(unit);
        EngageTarget engage = em.GetComponentData<EngageTarget>(unit);
        Assert.AreEqual(debugState.Target, engage.Target);
        Assert.IsTrue(em.Exists(debugState.Target));
        Assert.IsTrue(em.HasComponent<DebugFireTargetTag>(debugState.Target));
        Assert.AreEqual(unit, em.GetComponentData<DebugFireTargetTag>(debugState.Target).Source);
        Assert.Greater(em.GetComponentData<UnitHealth>(debugState.Target).Current, 1000);
        Assert.AreEqual(1, engage.IsCommanded);
    }

    [Test]
    public void ReleaseDebugFireRestoresPreviousEngageTargetAndDestroysDebugTarget()
    {
        EntityManager em = _world.EntityManager;
        GridConfig grid = CreateGrid(em);
        Entity unit = CreateSelectedAttackUnit(em, new float3(3f, 0f, 3f));
        Entity previousTarget = CreateTarget(em, new float3(4f, 0f, 3f));
        EngageTarget previous = new()
        {
            Target = previousTarget,
            Cell = new int2(4, 3),
            Position = new float3(4f, 0f, 3f),
            IsCommanded = 1
        };
        em.AddComponentData(unit, previous);

        SelectedUnitDebugFireSystem.ApplyDebugFire(em, grid, true);
        Entity debugTarget = em.GetComponentData<SelectedUnitDebugFireState>(unit).Target;
        SelectedUnitDebugFireSystem.ApplyDebugFire(em, grid, false);

        Assert.IsFalse(em.Exists(debugTarget));
        Assert.IsFalse(em.HasComponent<SelectedUnitDebugFireState>(unit));
        Assert.IsTrue(em.HasComponent<EngageTarget>(unit));
        EngageTarget restored = em.GetComponentData<EngageTarget>(unit);
        Assert.AreEqual(previous.Target, restored.Target);
        Assert.AreEqual(previous.Cell, restored.Cell);
        Assert.AreEqual(previous.Position, restored.Position);
        Assert.AreEqual(previous.IsCommanded, restored.IsCommanded);
    }

    [Test]
    public void DeselectingUnitCleansUpDebugFireState()
    {
        EntityManager em = _world.EntityManager;
        GridConfig grid = CreateGrid(em);
        Entity unit = CreateSelectedAttackUnit(em, new float3(3f, 0f, 3f));

        SelectedUnitDebugFireSystem.ApplyDebugFire(em, grid, true);
        Entity debugTarget = em.GetComponentData<SelectedUnitDebugFireState>(unit).Target;
        em.RemoveComponent<SelectedUnitTag>(unit);

        SelectedUnitDebugFireSystem.ApplyDebugFire(em, grid, true);

        Assert.IsFalse(em.Exists(debugTarget));
        Assert.IsFalse(em.HasComponent<SelectedUnitDebugFireState>(unit));
        Assert.IsFalse(em.HasComponent<EngageTarget>(unit));
    }

    [Test]
    public void HoldingDebugFireForGroundMissileLauncherTargetsEnemyBaseBeyondNormalRange()
    {
        EntityManager em = _world.EntityManager;
        GridConfig grid = CreateGrid(em, width: 256, height: 256);
        Entity launcher = CreateSelectedAttackUnit(em, new float3(3f, 0f, 3f), attackRange: 6f);
        AddGroundMissileLauncher(em, launcher, minRange: 35f, maxRange: 100f);
        float3 enemyBasePosition = new(210f, 0f, 120f);
        CreateEnemyRuntimeBuilding(em, enemyBasePosition, "Enemy Command Base");

        SelectedUnitDebugFireSystem.ApplyDebugFire(em, grid, true);

        Entity target = em.GetComponentData<SelectedUnitDebugFireState>(launcher).Target;
        float3 targetPosition = em.GetComponentData<LocalTransform>(target).Position;
        float distance = math.distance(
            new float2(3f, 3f),
            new float2(targetPosition.x, targetPosition.z));
        Assert.Greater(distance, 100f);
        Assert.AreEqual(enemyBasePosition, targetPosition);
        Assert.AreEqual(1, em.GetComponentData<EngageTarget>(launcher).IsCommanded);
    }

    [Test]
    public void HoldingDebugFireForGroundMissileLauncherArmsMissileAttackDirectly()
    {
        EntityManager em = _world.EntityManager;
        GridConfig grid = CreateGrid(em, width: 256, height: 256);
        Entity launcher = CreateSelectedAttackUnit(em, new float3(3f, 0f, 3f), attackRange: 6f);
        AddGroundMissileLauncher(em, launcher, minRange: 35f, maxRange: 100f);
        CreateEnemyRuntimeBuilding(em, new float3(210f, 0f, 120f), "Enemy Command Base");

        SelectedUnitDebugFireSystem.ApplyDebugFire(em, grid, true);

        GroundMissileLauncherStateComponent launcherState = em.GetComponentData<GroundMissileLauncherStateComponent>(launcher);
        Assert.AreEqual((byte)GroundMissileLauncherPhase.Preparing, launcherState.Phase);
        Assert.AreEqual(em.GetComponentData<SelectedUnitDebugFireState>(launcher).Target, launcherState.TargetEntity);
        Assert.Greater(em.GetComponentData<UnitAttackCooldownComponent>(launcher).CooldownRemaining, 0f);
    }

    [Test]
    public void HoldingDebugFireForGroundMissileLauncherCreatesMissileProjectileThroughCooldown()
    {
        EntityManager em = _world.EntityManager;
        GridConfig grid = CreateGrid(em, width: 256, height: 256);
        Entity launcher = CreateSelectedAttackUnit(em, new float3(3f, 0f, 3f), attackRange: 6f);
        AddGroundMissileLauncher(em, launcher, minRange: 35f, maxRange: 100f);
        em.SetComponentData(launcher, new UnitAttackCooldownComponent { CooldownRemaining = 99f });
        CreateEnemyRuntimeBuilding(em, new float3(210f, 0f, 120f), "Enemy Command Base");

        SelectedUnitDebugFireSystem.ApplyDebugFire(em, grid, true);

        SystemHandle fireSystem = _world.CreateSystem<GroundMissileLauncherFireSystem>();
        _world.SetTime(new TimeData(0.4d, 0.3f));
        fireSystem.Update(_world.Unmanaged);

        EntityQuery projectileQuery = em.CreateEntityQuery(typeof(GroundMissileProjectileComponent));
        Assert.AreEqual(0, projectileQuery.CalculateEntityCount());

        _world.SetTime(new TimeData(1.4d, 1f));
        fireSystem.Update(_world.Unmanaged);

        Assert.AreEqual(1, projectileQuery.CalculateEntityCount());
    }

    [Test]
    public void HoldingDebugFireForAirMissileLauncherTargetsAlongBatteryDirection()
    {
        EntityManager em = _world.EntityManager;
        GridConfig grid = CreateGrid(em, width: 256, height: 256);
        Entity launcher = CreateSelectedAttackUnit(em, new float3(3f, 0f, 3f), attackRange: 24f);
        AddAirMissileLauncher(em, launcher);

        float3 missilePosition = new(4f, 3f, 5f);
        quaternion batteryRotation = quaternion.RotateY(math.radians(45f));
        float3 expectedForward = math.rotate(batteryRotation, new float3(0f, 0f, 1f));
        Entity missile = em.CreateEntity(typeof(LocalToWorld));
        em.SetComponentData(missile, new LocalToWorld
        {
            Value = float4x4.TRS(missilePosition, batteryRotation, new float3(1f))
        });
        DynamicBuffer<AirMissileLauncherMissileVisualComponent> missiles = em.AddBuffer<AirMissileLauncherMissileVisualComponent>(launcher);
        missiles.Add(new AirMissileLauncherMissileVisualComponent
        {
            Missile = missile,
            SlotIndex = 0,
            InitialLocalPosition = float3.zero,
            InitialLocalRotation = quaternion.identity,
            InitialLocalScale = 1f
        });

        SelectedUnitDebugFireSystem.ApplyDebugFire(em, grid, true);

        Assert.IsTrue(em.HasComponent<AirMissileLauncherTargetComponent>(launcher));
        AirMissileLauncherStateComponent launcherState = em.GetComponentData<AirMissileLauncherStateComponent>(launcher);
        Assert.AreEqual((byte)AirMissileLauncherPhase.Locked, launcherState.Phase);
        Assert.AreEqual(0f, launcherState.Timer, 0.001f);

        AirMissileLauncherTargetComponent target = em.GetComponentData<AirMissileLauncherTargetComponent>(launcher);
        float3 targetPosition = em.GetComponentData<LocalTransform>(target.Target).Position;
        float3 actualForward = math.normalizesafe(targetPosition - missilePosition, new float3(0f, 0f, 1f));
        Assert.Greater(math.dot(actualForward, expectedForward), 0.99f);
        Assert.AreEqual(missilePosition.y, targetPosition.y, 0.001f);
    }

    [Test]
    public void HoldingDebugFireForReloadingAirMissileLauncherCreatesDebugState()
    {
        EntityManager em = _world.EntityManager;
        GridConfig grid = CreateGrid(em, width: 256, height: 256);
        Entity launcher = CreateSelectedAttackUnit(em, new float3(3f, 8f, 3f), attackRange: 24f);
        AddAirMissileLauncher(em, launcher);
        AirMissileLauncherStateComponent launcherState = em.GetComponentData<AirMissileLauncherStateComponent>(launcher);
        launcherState.Phase = (byte)AirMissileLauncherPhase.Reloading;
        launcherState.Timer = 0.5f;
        em.SetComponentData(launcher, launcherState);

        SelectedUnitDebugFireSystem.ApplyDebugFire(em, grid, true);

        Assert.IsTrue(em.HasComponent<SelectedUnitDebugFireState>(launcher), "Reloading air missile launchers still need debug-fire state so air movement remains suppressed while F is held.");
        SelectedUnitDebugFireState debugState = em.GetComponentData<SelectedUnitDebugFireState>(launcher);
        Assert.IsTrue(em.Exists(debugState.Target));
        Assert.IsTrue(em.HasComponent<DebugFireTargetTag>(debugState.Target));
        Assert.AreEqual(launcher, em.GetComponentData<DebugFireTargetTag>(debugState.Target).Source);
        Assert.IsTrue(em.HasComponent<UnitAirMovement>(debugState.Target));
        Assert.IsFalse(em.HasComponent<EngageTarget>(launcher));
        Assert.IsFalse(em.HasComponent<AirMissileLauncherTargetComponent>(launcher), "Reloading launchers should not create a new missile target until reload finishes.");
    }

    private static GridConfig CreateGrid(EntityManager em, int width = 16, int height = 16)
    {
        Entity entity = em.CreateEntity(typeof(GridConfig));
        GridConfig grid = new()
        {
            Width = width,
            Height = height,
            CellSize = 1f,
            Origin = float3.zero
        };
        em.SetComponentData(entity, grid);
        return grid;
    }

    private static Entity CreateSelectedAttackUnit(EntityManager em, float3 position, float attackRange = 6f)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(SelectedUnitTag),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(UnitAttackCooldownComponent),
            typeof(UnitAttackTraceComponent),
            typeof(UnitAttackAnimationComponent),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 0 });
        em.SetComponentData(entity, new UnitAttack
        {
            Range = attackRange,
            CooldownSeconds = 0.5f,
            Damage = 10,
            TraceVisibleSeconds = 0.08f,
            TraceWidth = 0.15f,
            TraceDashDensity = 8f,
            TraceScrollSpeed = 10f
        });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static void AddGroundMissileLauncher(EntityManager em, Entity entity, float minRange, float maxRange)
    {
        em.AddComponentData(entity, new GroundMissileLauncherComponent
        {
            MinRange = minRange,
            MaxRange = maxRange,
            PrepareSeconds = 0.25f,
            ReloadSeconds = 1f,
            BatteryElevatedAngleDegrees = -30f,
            RocketSpeed = 80f,
            ArcHeight = 10f,
            DamageRadius = 5f,
            Damage = 90
        });
        em.AddComponentData(entity, new GroundMissileLauncherStateComponent
        {
            Phase = (byte)GroundMissileLauncherPhase.Idle,
            TargetEntity = Entity.Null,
            TargetCell = default,
            TargetWorldPosition = default,
            Timer = 0f,
            SelectedRocketSlot = -1
        });
    }

    private static void AddAirMissileLauncher(EntityManager em, Entity entity)
    {
        em.AddComponentData(entity, new AirMissileLauncherComponent
        {
            MinRange = 2f,
            BaseDetectionRange = 80f,
            MaxDetectionRange = 120f,
            MaxSupportRangeBonus = 40f,
            AirTargetPriority = 100f,
            IncomingMissilePriority = 50f,
            LockSeconds = 0.25f,
            LaunchDelaySeconds = 0.1f,
            ReloadSeconds = 1f,
            AimToleranceDegrees = 180f,
            MissileSpeed = 35f,
            MissileAcceleration = 0f,
            MissileTurnRateDegreesPerSecond = 120f,
            MissileLifetimeSeconds = 4f,
            ProximityFuseRadius = 1f,
            AirTargetDamage = 25,
            IncomingMissileDamage = 10,
            TrackingQuality = 1f
        });
        em.AddComponentData(entity, new AirMissileLauncherStateComponent
        {
            Phase = (byte)AirMissileLauncherPhase.Idle,
            TargetEntity = Entity.Null,
            TargetKind = (byte)AirMissileTargetKind.None,
            EffectiveRange = 80f,
            EffectiveLockSeconds = 0.25f,
            EffectiveTrackingQuality = 1f,
            EffectiveTurnRateDegreesPerSecond = 120f,
            SelectedMissileSlot = -1
        });
    }

    private static Entity CreateTarget(EntityManager em, float3 position)
    {
        Entity entity = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform));
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateEnemyRuntimeBuilding(EntityManager em, float3 position, string name)
    {
        Entity entity = em.CreateEntity(
            typeof(RuntimeBuildingCombatTag),
            typeof(RuntimeBuildingCombatInfo),
            typeof(Faction),
            typeof(UnitHealth),
            typeof(UnitSourcePrefabKey),
            typeof(UnitDisplayInfo),
            typeof(LocalTransform));
        em.SetComponentData(entity, new RuntimeBuildingCombatInfo
        {
            RuntimeBuildingId = 1,
            OwnerFactionId = FactionIdentity.EnemyFactionId,
            OriginCell = new int2((int)position.x, (int)position.z),
            FootprintCells = new int2(20, 20),
            IsWall = 0,
            IsGate = 0
        });
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(entity, new UnitHealth { Current = 500, Max = 500 });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(name) });
        em.SetComponentData(entity, new UnitDisplayInfo { Name = new FixedString64Bytes(name) });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }
}
