using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class SelectedUnitDebugFireSystemTests
{
    private World _world;

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
    public void HoldingDebugFireForGroundMissileLauncherUsesValidMinimumRange()
    {
        EntityManager em = _world.EntityManager;
        GridConfig grid = CreateGrid(em);
        Entity launcher = CreateSelectedAttackUnit(em, new float3(3f, 0f, 3f), attackRange: 40f);
        AddGroundMissileLauncher(em, launcher, minRange: 35f, maxRange: 100f);

        SelectedUnitDebugFireSystem.ApplyDebugFire(em, grid, true);

        Entity target = em.GetComponentData<SelectedUnitDebugFireState>(launcher).Target;
        float3 targetPosition = em.GetComponentData<LocalTransform>(target).Position;
        float distance = math.distance(
            new float2(3f, 3f),
            new float2(targetPosition.x, targetPosition.z));
        Assert.Greater(distance, 35f);
        Assert.LessOrEqual(distance, 40f * 0.95f + 0.001f);
    }

    [Test]
    public void HoldingDebugFireForGroundMissileLauncherArmsMissileAttack()
    {
        EntityManager em = _world.EntityManager;
        GridConfig grid = CreateGrid(em);
        Entity launcher = CreateSelectedAttackUnit(em, new float3(3f, 0f, 3f), attackRange: 40f);
        AddGroundMissileLauncher(em, launcher, minRange: 35f, maxRange: 100f);

        SelectedUnitDebugFireSystem.ApplyDebugFire(em, grid, true);
        SystemHandle attackSystem = _world.CreateSystem<UnitAttackSystem>();
        _world.SetTime(new TimeData(0.1d, 0.1f));
        attackSystem.Update(_world.Unmanaged);

        GroundMissileLauncherStateComponent launcherState = em.GetComponentData<GroundMissileLauncherStateComponent>(launcher);
        Assert.AreEqual((byte)GroundMissileLauncherPhase.Preparing, launcherState.Phase);
        Assert.AreEqual(em.GetComponentData<SelectedUnitDebugFireState>(launcher).Target, launcherState.TargetEntity);
    }

    private static GridConfig CreateGrid(EntityManager em)
    {
        Entity entity = em.CreateEntity(typeof(GridConfig));
        GridConfig grid = new()
        {
            Width = 16,
            Height = 16,
            CellSize = 1f,
            Origin = float3.zero
        };
        em.SetComponentData(entity, grid);
        return grid;
    }

    private static Entity CreateSelectedAttackUnit(EntityManager em, float3 position, float attackRange = 6f)
    {
        Entity entity = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(UnitAttackCooldownComponent),
            typeof(UnitAttackTraceComponent),
            typeof(UnitAttackAnimationComponent),
            typeof(UnitHealth),
            typeof(LocalTransform));
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

    private static Entity CreateTarget(EntityManager em, float3 position)
    {
        Entity entity = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform));
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }
}
