using System;
using Game.Components;
using Game.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

internal static class S004MissileFlightOwnerControl
{
    private static void Main()
    {
        int next = 1;
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var projectile = em.CreateEntity();
        em.AddComponentData(projectile, LocalTransform.Identity);
        em.AddComponentData(projectile, new GroundMissileProjectileComponent {
            StartPosition = new float3(0, 0, 0), TargetPosition = new float3(10, 0, 0), DurationSeconds = 2f / 60f,
            Damage = 23, FactionId = 2 });
        em.WorldUnmanaged.Time = new TimeData { ElapsedTime = 1.0 / 60, DeltaTime = 1f / 60f };
        var owner = new GroundMissileProjectileFlightSystem(); var state = new SystemState { EntityManager = em, Enabled = true };
        state.Begin(); owner.OnCreate(ref state); state.End();
        state.Begin(); owner.OnUpdate(ref state); state.End();
        if (math.abs(em.GetComponentData<LocalTransform>(projectile).Position.x - 5f) > 0.0001f ||
            !em.HasComponent<GroundMissileProjectileComponent>(projectile) || em.HasComponent<GroundMissileImpactRequestComponent>(projectile))
            throw new InvalidOperationException("Projectile midpoint differs");
        em.WorldUnmanaged.Time = new TimeData { ElapsedTime = 2.0 / 60, DeltaTime = 1f / 60f };
        state.Begin(); owner.OnUpdate(ref state); state.End();
        if (em.HasComponent<GroundMissileProjectileComponent>(projectile) || !em.HasComponent<GroundMissileImpactRequestComponent>(projectile))
            throw new InvalidOperationException("Original projectile did not publish its impact request");
        var impact = em.GetComponentData<GroundMissileImpactRequestComponent>(projectile);
        if (impact.Damage != 23 || impact.FactionId != 2 || math.abs(impact.Position.x - 10f) > 0.0001f)
            throw new InvalidOperationException("Original impact request payload differs");
        Console.WriteLine("[S004MissileFlightOwnerControl] result=Passed midpoint=Passed impactRequest=Observed scope=SourceFixtureOnly");
    }
}
