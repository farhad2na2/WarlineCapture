using System;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal sealed class S004MissileTickOwners
{
    private delegate void Call<T>(ref T owner, ref SystemState state) where T : struct;
    private sealed class Owner<T> where T : struct
    {
        private T owner; private SystemState state; private readonly Call<T> update;
        internal int Updates;
        internal Owner(EntityManager em, Call<T> create, Call<T> step)
        {
            state = new SystemState { EntityManager = em, Enabled = true }; update = step;
            state.Begin(); try { create(ref owner, ref state); } finally { state.End(); }
            state.LastSystemVersion = 0;
        }
        internal void Run()
        {
            state.Begin();
            try { if (state.Enabled && state.RequiredQueriesPresent) { update(ref owner, ref state); Updates++; } }
            finally { state.End(); }
        }
    }
    private readonly EntityManager em;
    private readonly Owner<GroundMissileLauncherFireSystem> fire;
    private readonly Owner<GroundMissileLauncherVisualSystem> visual;
    private readonly Owner<GroundMissileFlyingRocketVisualSystem> flying;
    private readonly Owner<GroundMissileRocketTrailSystem> trail;
    private readonly Owner<GroundMissileProjectileFlightSystem> flight;
    private readonly Owner<MissileFlightAudioSystem> audio;
    private readonly Owner<GroundMissileImpactSystem> impact;
    internal S004MissileTickOwners(EntityManager manager)
    {
        em = manager ?? throw new ArgumentNullException(nameof(manager));
        fire = new Owner<GroundMissileLauncherFireSystem>(em, (ref GroundMissileLauncherFireSystem s, ref SystemState st) => s.OnCreate(ref st), (ref GroundMissileLauncherFireSystem s, ref SystemState st) => s.OnUpdate(ref st));
        visual = new Owner<GroundMissileLauncherVisualSystem>(em, (ref GroundMissileLauncherVisualSystem s, ref SystemState st) => s.OnCreate(ref st), (ref GroundMissileLauncherVisualSystem s, ref SystemState st) => s.OnUpdate(ref st));
        flying = new Owner<GroundMissileFlyingRocketVisualSystem>(em, (ref GroundMissileFlyingRocketVisualSystem s, ref SystemState st) => s.OnCreate(ref st), (ref GroundMissileFlyingRocketVisualSystem s, ref SystemState st) => s.OnUpdate(ref st));
        trail = new Owner<GroundMissileRocketTrailSystem>(em, (ref GroundMissileRocketTrailSystem s, ref SystemState st) => s.OnCreate(ref st), (ref GroundMissileRocketTrailSystem s, ref SystemState st) => s.OnUpdate(ref st));
        flight = new Owner<GroundMissileProjectileFlightSystem>(em, (ref GroundMissileProjectileFlightSystem s, ref SystemState st) => s.OnCreate(ref st), (ref GroundMissileProjectileFlightSystem s, ref SystemState st) => s.OnUpdate(ref st));
        audio = new Owner<MissileFlightAudioSystem>(em, (ref MissileFlightAudioSystem s, ref SystemState st) => s.OnCreate(ref st), (ref MissileFlightAudioSystem s, ref SystemState st) => s.OnUpdate(ref st));
        impact = new Owner<GroundMissileImpactSystem>(em, (ref GroundMissileImpactSystem s, ref SystemState st) => s.OnCreate(ref st), (ref GroundMissileImpactSystem s, ref SystemState st) => s.OnUpdate(ref st));
    }
    internal void Fire() => fire.Run(); internal void Visual() => visual.Run(); internal void Flying() => flying.Run();
    internal void Trail() => trail.Run(); internal void Flight() => flight.Run(); internal void Audio() => audio.Run(); internal void Impact() => impact.Run();
    internal JObject Observe()
    {
        using var projectiles = em.CreateEntityQuery(typeof(GroundMissileProjectileComponent));
        using var impacts = em.CreateEntityQuery(typeof(GroundMissileImpactRequestComponent));
        return new JObject { ["fire_updates"] = fire.Updates, ["visual_updates"] = visual.Updates, ["flying_visual_updates"] = flying.Updates,
            ["trail_updates"] = trail.Updates, ["flight_updates"] = flight.Updates, ["audio_updates"] = audio.Updates,
            ["impact_updates"] = impact.Updates, ["projectiles_remaining"] = projectiles.CalculateEntityCount(), ["impact_requests_remaining"] = impacts.CalculateEntityCount() };
    }
}
