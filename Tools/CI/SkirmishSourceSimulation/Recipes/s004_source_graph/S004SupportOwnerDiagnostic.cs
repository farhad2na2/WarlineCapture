using System;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S004SupportOwnerDiagnostic
{
    internal static JObject Run(EntityManager em)
    {
        using var requestRoots = em.CreateEntityQuery(typeof(SupportSessionComponent), typeof(SupportRequestElement));
        using var flights = em.CreateEntityQuery(typeof(SupportFlightComponent));
        using var drops = em.CreateEntityQuery(typeof(UnitTransportAirdropRequest));
        using var attackTargets = em.CreateEntityQuery(typeof(UnitTransportDeployAttackTarget));
        int requestBefore = requestRoots.CalculateEntityCount(), flightBefore = flights.CalculateEntityCount();
        int dropBefore = drops.CalculateEntityCount(), attackBefore = attackTargets.CalculateEntityCount();
        var request = new SupportAbilityRequestSystem(); var requestState = new SystemState { EntityManager = em, Enabled = true };
        requestState.Begin(); try { request.OnCreate(ref requestState); } finally { requestState.End(); }
        bool requestRan = false; requestState.Begin();
        try { if (requestState.Enabled && requestState.RequiredQueriesPresent) { request.OnUpdate(ref requestState); requestRan = true; } }
        finally { requestState.End(); }
        var flight = new SupportFlightSystem(); var flightState = new SystemState { EntityManager = em, Enabled = true };
        flightState.Begin(); try { flight.OnUpdate(ref flightState); } finally { flightState.End(); }
        var airdrop = new UnitTransportAirdropSystem(); var dropState = new SystemState { EntityManager = em, Enabled = true };
        dropState.Begin(); try { airdrop.OnCreate(ref dropState); } finally { dropState.End(); }
        bool airdropRan = false; dropState.Begin();
        try { if (dropState.Enabled && dropState.RequiredQueriesPresent) { airdrop.OnUpdate(ref dropState); airdropRan = true; } }
        finally { dropState.End(); }
        if (!airdropRan) throw new InvalidOperationException("Original airdrop grid prerequisite missing");
        var attack = new UnitTransportDeployAttackSystem(); var attackState = new SystemState { EntityManager = em, Enabled = true };
        attackState.Begin(); try { attack.OnCreate(ref attackState); } finally { attackState.End(); }
        bool attackRan = false; attackState.Begin();
        try { if (attackState.Enabled && attackState.RequiredQueriesPresent) { attack.OnUpdate(ref attackState); attackRan = true; } }
        finally { attackState.End(); }
        return new JObject { ["scope"] = "OriginalSupportRequestFlightAirdropAttack_SourceSubset_NotFullScheduler",
            ["support_request_roots_before"] = requestBefore, ["support_request_owner_update_invoked"] = requestRan,
            ["support_flights_before"] = flightBefore, ["support_flights_after"] = flights.CalculateEntityCount(), ["support_flight_owner_update_invoked"] = true,
            ["airdrop_requests_before"] = dropBefore, ["airdrop_requests_after"] = drops.CalculateEntityCount(), ["airdrop_update_invoked"] = airdropRan,
            ["deploy_attack_targets_before"] = attackBefore, ["deploy_attack_targets_after"] = attackTargets.CalculateEntityCount(),
            ["deploy_attack_update_invoked"] = attackRan, ["native_input_claimed"] = false, ["mission_ready"] = false };
    }
}
