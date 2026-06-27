using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public static class BattleScenarioEcsSpawnHelpers
{
    public static Entity CreateGroundMissileLauncher(
        EntityManager em,
        float3 position,
        byte factionId,
        int health,
        GroundMissileLauncherComponent config,
        GroundMissileLauncherStateComponent state)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform),
            typeof(GroundMissileLauncherComponent),
            typeof(GroundMissileLauncherStateComponent));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, config);
        em.SetComponentData(entity, state);
        return entity;
    }

    public static Entity CreateAirMissileLauncher(
        EntityManager em,
        float3 position,
        byte factionId,
        int health,
        AirMissileLauncherComponent config,
        AirMissileLauncherStateComponent state,
        AirDefenseSupportLinkComponent supportLink)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform),
            typeof(AirMissileLauncherComponent),
            typeof(AirMissileLauncherStateComponent),
            typeof(AirDefenseSupportLinkComponent));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, config);
        em.SetComponentData(entity, state);
        em.SetComponentData(entity, supportLink);
        return entity;
    }

    public static Entity CreateIncomingGroundMissile(
        EntityManager em,
        float3 start,
        float3 target,
        byte factionId,
        float durationSeconds,
        float arcHeight,
        float damageRadius,
        int damage)
    {
        Entity entity = em.CreateEntity(
            typeof(LocalTransform),
            typeof(GroundMissileProjectileComponent),
            typeof(MissileInterceptionTargetComponent));
        em.SetComponentData(entity, LocalTransform.FromPosition(start));
        em.SetComponentData(entity, new GroundMissileProjectileComponent
        {
            Source = Entity.Null,
            TargetEntity = Entity.Null,
            TargetCell = default,
            StartPosition = start,
            TargetPosition = target,
            ElapsedSeconds = 0f,
            DurationSeconds = durationSeconds,
            ArcHeight = arcHeight,
            DamageRadius = damageRadius,
            Damage = damage,
            FactionId = factionId,
            Interceptable = 1
        });
        em.SetComponentData(entity, new MissileInterceptionTargetComponent
        {
            Source = Entity.Null,
            FactionId = factionId
        });
        return entity;
    }

    public static Entity CreateAirDefenseSupportProvider(
        EntityManager em,
        float3 position,
        byte factionId,
        AirDefenseSupportProviderKind kind,
        byte level,
        float supportRadius,
        float rangeBonus,
        float lockTimeMultiplier,
        float trackingBonus,
        float turnRateBonus)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(LocalTransform),
            typeof(AirDefenseSupportProviderComponent));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new AirDefenseSupportProviderComponent
        {
            Kind = (byte)kind,
            Level = level,
            SupportRadius = supportRadius,
            RangeBonus = rangeBonus,
            LockTimeMultiplier = lockTimeMultiplier,
            TrackingBonus = trackingBonus,
            TurnRateBonus = turnRateBonus
        });
        return entity;
    }

    public static Entity CreateAirTarget(
        EntityManager em,
        float3 position,
        byte factionId,
        int health,
        float cruiseHeight,
        float runwayTaxiSpeed)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform),
            typeof(UnitPrevWorldPos),
            typeof(UnitAirMovement));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new UnitPrevWorldPos { Value = position });
        em.SetComponentData(entity, new UnitAirMovement
        {
            CruiseHeight = cruiseHeight,
            RunwayTaxiSpeed = runwayTaxiSpeed
        });
        return entity;
    }

    public static Entity CreateGroundTarget(
        EntityManager em,
        float3 position,
        byte factionId,
        int health)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }
}
