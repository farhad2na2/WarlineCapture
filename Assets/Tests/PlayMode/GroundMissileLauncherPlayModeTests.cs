#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class GroundMissileLauncherPlayModeTests
{
    [Test]
    public void GroundMissileLauncherRuntime_FiresVisibleRocketAndDamagesEnemyOnImpact()
    {
        using var world = new World("GroundMissileLauncherPlayModeRuntime");
        EntityManager em = world.EntityManager;

        Entity target = CreateTarget(em, new float3(10f, 0f, 0f), health: 100);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f));
        Entity rocketParent = em.CreateEntity(typeof(LocalTransform));
        Entity rocket = em.CreateEntity(typeof(LocalTransform), typeof(LocalToWorld), typeof(Parent));
        em.SetComponentData(rocketParent, LocalTransform.Identity);
        em.SetComponentData(rocket, LocalTransform.FromPosition(new float3(1f, 0f, 0f)));
        em.SetComponentData(rocket, new LocalToWorld { Value = float4x4.Translate(new float3(1f, 0f, 0f)) });
        em.SetComponentData(rocket, new Parent { Value = rocketParent });
        DynamicBuffer<GroundMissileLauncherRocketVisualComponent> rockets =
            em.AddBuffer<GroundMissileLauncherRocketVisualComponent>(launcher);
        rockets.Add(new GroundMissileLauncherRocketVisualComponent
        {
            Rocket = rocket,
            SlotIndex = 0,
            InitialLocalPosition = new float3(1f, 0f, 0f),
            InitialLocalRotation = quaternion.identity,
            InitialLocalScale = 1f
        });
        em.SetComponentData(launcher, new GroundMissileLauncherStateComponent
        {
            Phase = (byte)GroundMissileLauncherPhase.Preparing,
            TargetEntity = target,
            TargetCell = new int2(10, 0),
            TargetWorldPosition = new float3(10f, 0f, 0f),
            Timer = 0f,
            SelectedRocketSlot = 0
        });

        SystemHandle fireSystem = world.CreateSystem<GroundMissileLauncherFireSystem>();
        SystemHandle rocketVisualSystem = world.CreateSystem<GroundMissileFlyingRocketVisualSystem>();
        SystemHandle projectileSystem = world.CreateSystem<GroundMissileProjectileFlightSystem>();
        SystemHandle impactSystem = world.CreateSystem<GroundMissileImpactSystem>();

        world.SetTime(new TimeData(0.1d, 0.1f));
        fireSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<GroundMissileFlyingRocketVisualComponent>(rocket), "The selected rack rocket must detach as the visible flying rocket.");
        Assert.IsFalse(em.HasComponent<Parent>(rocket), "The flying rocket should leave the launcher hierarchy during flight.");
        Assert.AreEqual(100, em.GetComponentData<UnitHealth>(target).Current, "The target must not take damage on launch.");

        world.SetTime(new TimeData(1d, 1f));
        rocketVisualSystem.Update(world.Unmanaged);
        projectileSystem.Update(world.Unmanaged);
        impactSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<GroundMissileFlyingRocketVisualComponent>(rocket), "The flying rocket visual should restore after the arc completes.");
        Assert.IsTrue(em.HasComponent<Parent>(rocket), "The rocket visual must return to the original rack parent.");
        Assert.AreEqual(rocketParent, em.GetComponentData<Parent>(rocket).Value);
        Assert.AreEqual(10, em.GetComponentData<UnitHealth>(target).Current, "Missile damage should apply on impact.");
    }

    private static Entity CreateLauncher(EntityManager em, float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(GroundMissileLauncherComponent),
            typeof(GroundMissileLauncherStateComponent),
            typeof(LocalTransform));

        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = new int2(0, 0) });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(2, 2) });
        em.SetComponentData(entity, new UnitHealth { Current = 450, Max = 450 });
        em.SetComponentData(entity, new GroundMissileLauncherComponent
        {
            MinRange = 5f,
            MaxRange = 600f,
            PrepareSeconds = 0.01f,
            ReloadSeconds = 3f,
            BatteryElevatedAngleDegrees = -30f,
            RocketSpeed = 100f,
            ArcHeight = 10f,
            DamageRadius = 5f,
            Damage = 90
        });
        em.SetComponentData(entity, new GroundMissileLauncherStateComponent
        {
            Phase = (byte)GroundMissileLauncherPhase.Idle,
            TargetEntity = Entity.Null,
            TargetCell = default,
            TargetWorldPosition = default,
            Timer = 0f,
            SelectedRocketSlot = -1
        });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateTarget(EntityManager em, float3 position, int health)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = new int2(10, 0) });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }
}
#endif
