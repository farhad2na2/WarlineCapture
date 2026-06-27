using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class BattleScenarioEcsSpawnHelpersTests
{
    [Test]
    public void CreateGroundMissileLauncher_AddsExpectedComponents()
    {
        using World world = new("BattleScenarioSpawnHelpers_GroundLauncher");
        EntityManager em = world.EntityManager;

        Entity entity = BattleScenarioEcsSpawnHelpers.CreateGroundMissileLauncher(
            em,
            new float3(4f, 0f, 6f),
            FactionIdentity.EnemyFactionId,
            300,
            new GroundMissileLauncherComponent
            {
                MaxRange = 180f,
                PrepareSeconds = 0.1f,
                RocketSpeed = 90f,
                Damage = 120
            },
            new GroundMissileLauncherStateComponent
            {
                Phase = (byte)GroundMissileLauncherPhase.Idle,
                SelectedRocketSlot = -1
            });

        Assert.IsTrue(em.HasComponent<Faction>(entity));
        Assert.IsTrue(em.HasComponent<UnitHealth>(entity));
        Assert.IsTrue(em.HasComponent<GroundMissileLauncherComponent>(entity));
        Assert.IsTrue(em.HasComponent<GroundMissileLauncherStateComponent>(entity));
        Assert.AreEqual(FactionIdentity.EnemyFactionId, em.GetComponentData<Faction>(entity).Id);
        Assert.AreEqual(300, em.GetComponentData<UnitHealth>(entity).Current);
        Assert.AreEqual(new float3(4f, 0f, 6f), em.GetComponentData<LocalTransform>(entity).Position);
    }

    [Test]
    public void CreateAirMissileLauncher_AddsExpectedComponents()
    {
        using World world = new("BattleScenarioSpawnHelpers_Launcher");
        EntityManager em = world.EntityManager;

        Entity entity = BattleScenarioEcsSpawnHelpers.CreateAirMissileLauncher(
            em,
            new float3(1f, 2f, 3f),
            FactionIdentity.PlayerFactionId,
            250,
            new AirMissileLauncherComponent
            {
                BaseDetectionRange = 140f,
                LockSeconds = 0.8f,
                TrackingQuality = 0.7f
            },
            new AirMissileLauncherStateComponent
            {
                Phase = (byte)AirMissileLauncherPhase.Idle,
                EffectiveRange = 140f,
                EffectiveLockSeconds = 0.8f,
                EffectiveTrackingQuality = 0.7f,
                SelectedMissileSlot = -1
            },
            new AirDefenseSupportLinkComponent
            {
                LockTimeMultiplier = 1f
            });

        Assert.IsTrue(em.HasComponent<Faction>(entity));
        Assert.IsTrue(em.HasComponent<UnitHealth>(entity));
        Assert.IsTrue(em.HasComponent<AirMissileLauncherComponent>(entity));
        Assert.IsTrue(em.HasComponent<AirMissileLauncherStateComponent>(entity));
        Assert.IsTrue(em.HasComponent<AirDefenseSupportLinkComponent>(entity));
        Assert.AreEqual(FactionIdentity.PlayerFactionId, em.GetComponentData<Faction>(entity).Id);
        Assert.AreEqual(250, em.GetComponentData<UnitHealth>(entity).Current);
        Assert.AreEqual(new float3(1f, 2f, 3f), em.GetComponentData<LocalTransform>(entity).Position);
    }

    [Test]
    public void CreateIncomingGroundMissile_AddsInterceptableThreatComponents()
    {
        using World world = new("BattleScenarioSpawnHelpers_GroundMissile");
        EntityManager em = world.EntityManager;
        float3 start = new(80f, 10f, 0f);
        float3 target = new(-20f, 0f, 0f);

        Entity entity = BattleScenarioEcsSpawnHelpers.CreateIncomingGroundMissile(
            em,
            start,
            target,
            FactionIdentity.EnemyFactionId,
            4f,
            9f,
            7f,
            100);

        Assert.IsTrue(em.HasComponent<GroundMissileProjectileComponent>(entity));
        Assert.IsTrue(em.HasComponent<MissileInterceptionTargetComponent>(entity));
        GroundMissileProjectileComponent projectile = em.GetComponentData<GroundMissileProjectileComponent>(entity);
        Assert.AreEqual(start, projectile.StartPosition);
        Assert.AreEqual(target, projectile.TargetPosition);
        Assert.AreEqual(4f, projectile.DurationSeconds);
        Assert.AreEqual(1, projectile.Interceptable);
        Assert.AreEqual(FactionIdentity.EnemyFactionId, projectile.FactionId);
        Assert.AreEqual(FactionIdentity.EnemyFactionId, em.GetComponentData<MissileInterceptionTargetComponent>(entity).FactionId);
    }

    [Test]
    public void CreateAirDefenseSupportProvider_AddsRadarSupportData()
    {
        using World world = new("BattleScenarioSpawnHelpers_Support");
        EntityManager em = world.EntityManager;

        Entity entity = BattleScenarioEcsSpawnHelpers.CreateAirDefenseSupportProvider(
            em,
            new float3(8f, 0f, 0f),
            FactionIdentity.PlayerFactionId,
            AirDefenseSupportProviderKind.Radar,
            1,
            90f,
            90f,
            0.5f,
            0.2f,
            50f);

        Assert.IsTrue(em.HasComponent<Faction>(entity));
        Assert.IsTrue(em.HasComponent<AirDefenseSupportProviderComponent>(entity));
        AirDefenseSupportProviderComponent provider = em.GetComponentData<AirDefenseSupportProviderComponent>(entity);
        Assert.AreEqual((byte)AirDefenseSupportProviderKind.Radar, provider.Kind);
        Assert.AreEqual(90f, provider.SupportRadius);
        Assert.AreEqual(90f, provider.RangeBonus);
        Assert.AreEqual(0.5f, provider.LockTimeMultiplier);
        Assert.AreEqual(0.2f, provider.TrackingBonus);
        Assert.AreEqual(50f, provider.TurnRateBonus);
    }

    [Test]
    public void CreateAirTarget_AddsAirMovementAndPreviousPosition()
    {
        using World world = new("BattleScenarioSpawnHelpers_AirTarget");
        EntityManager em = world.EntityManager;
        float3 position = new(100f, 14f, 0f);

        Entity entity = BattleScenarioEcsSpawnHelpers.CreateAirTarget(
            em,
            position,
            FactionIdentity.EnemyFactionId,
            120,
            14f,
            5f);

        Assert.IsTrue(em.HasComponent<Faction>(entity));
        Assert.IsTrue(em.HasComponent<UnitHealth>(entity));
        Assert.IsTrue(em.HasComponent<UnitAirMovement>(entity));
        Assert.IsTrue(em.HasComponent<UnitPrevWorldPos>(entity));
        Assert.AreEqual(FactionIdentity.EnemyFactionId, em.GetComponentData<Faction>(entity).Id);
        Assert.AreEqual(120, em.GetComponentData<UnitHealth>(entity).Current);
        Assert.AreEqual(position, em.GetComponentData<LocalTransform>(entity).Position);
        Assert.AreEqual(position, em.GetComponentData<UnitPrevWorldPos>(entity).Value);
        Assert.AreEqual(14f, em.GetComponentData<UnitAirMovement>(entity).CruiseHeight);
    }

    [Test]
    public void CreateGroundTarget_AddsHealthFactionAndTransform()
    {
        using World world = new("BattleScenarioSpawnHelpers_GroundTarget");
        EntityManager em = world.EntityManager;
        float3 position = new(24f, 0f, -8f);

        Entity entity = BattleScenarioEcsSpawnHelpers.CreateGroundTarget(
            em,
            position,
            FactionIdentity.PlayerFactionId,
            220);

        Assert.IsTrue(em.HasComponent<Faction>(entity));
        Assert.IsTrue(em.HasComponent<UnitHealth>(entity));
        Assert.IsTrue(em.HasComponent<LocalTransform>(entity));
        Assert.AreEqual(FactionIdentity.PlayerFactionId, em.GetComponentData<Faction>(entity).Id);
        Assert.AreEqual(220, em.GetComponentData<UnitHealth>(entity).Current);
        Assert.AreEqual(position, em.GetComponentData<LocalTransform>(entity).Position);
    }
}
