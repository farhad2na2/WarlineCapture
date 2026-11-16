using System;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S004AirdropOwnerDiagnostic
{
    internal static JObject Run(EntityManager em)
    {
        using var sessions = em.CreateEntityQuery(typeof(SupportSessionComponent));
        using var flights = em.CreateEntityQuery(typeof(SupportFlightComponent));
        using var requests = em.CreateEntityQuery(typeof(SupportRequestElement));
        if (sessions.CalculateEntityCount() != 0 || flights.CalculateEntityCount() != 0 || requests.CalculateEntityCount() != 0)
            throw new NotSupportedException("Support state requires its original flight and paratrooper owners");
        using var drops = em.CreateEntityQuery(typeof(UnitTransportAirdropRequest));
        using var attackTargets = em.CreateEntityQuery(typeof(UnitTransportDeployAttackTarget));
        int dropBefore = drops.CalculateEntityCount(), attackBefore = attackTargets.CalculateEntityCount();
        var airdrop = new UnitTransportAirdropSystem();
        var state = new SystemState { EntityManager = em, Enabled = true };
        state.Begin(); try { airdrop.OnCreate(ref state); } finally { state.End(); }
        bool airdropRan = false;
        state.Begin();
        try { if (state.Enabled && state.RequiredQueriesPresent) { airdrop.OnUpdate(ref state); airdropRan = true; } }
        finally { state.End(); }
        if (!airdropRan) throw new InvalidOperationException("Original airdrop grid prerequisite missing");
        var attack = new UnitTransportDeployAttackSystem();
        var attackState = new SystemState { EntityManager = em, Enabled = true };
        attackState.Begin(); try { attack.OnCreate(ref attackState); } finally { attackState.End(); }
        bool attackRan = false;
        attackState.Begin();
        try { if (attackState.Enabled && attackState.RequiredQueriesPresent) { attack.OnUpdate(ref attackState); attackRan = true; } }
        finally { attackState.End(); }
        return new JObject { ["scope"] = "SourceOnly_NoSupport_AirdropThenDeployAttack_NotFullScheduler",
            ["support_sessions"] = 0, ["support_flights"] = 0, ["support_request_buffers"] = 0,
            ["airdrop_requests_before"] = dropBefore, ["airdrop_requests_after"] = drops.CalculateEntityCount(),
            ["airdrop_update_invoked"] = airdropRan, ["deploy_attack_targets_before"] = attackBefore,
            ["deploy_attack_targets_after"] = attackTargets.CalculateEntityCount(), ["deploy_attack_update_invoked"] = attackRan,
            ["native_input_claimed"] = false, ["mission_ready"] = false };
    }
}
