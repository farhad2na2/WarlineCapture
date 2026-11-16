using System;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S004TransportOwnerDiagnostic
{
    internal static JObject Run(EntityManager em)
    {
        using var orders = em.CreateEntityQuery(typeof(UnitTransportDeployOrder), typeof(UnitGrid));
        using var grid = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable),
            typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
        int before = orders.CalculateEntityCount();
        if (grid.CalculateEntityCount() != 1) throw new InvalidOperationException("One same-world grid is required");
        var owner = new UnitTransportDeployOrderSystem();
        var state = new SystemState { EntityManager = em, Enabled = true };
        state.Begin(); try { owner.OnCreate(ref state); } finally { state.End(); }
        bool ran = false;
        state.Begin();
        try { if (state.Enabled && state.RequiredQueriesPresent) { owner.OnUpdate(ref state); ran = true; } }
        finally { state.End(); }
        using var boardingTargets = em.CreateEntityQuery(typeof(UnitTransportBoardingTarget));
        using var attackTargets = em.CreateEntityQuery(typeof(UnitTransportDeployAttackTarget));
        int boardingBefore = boardingTargets.CalculateEntityCount();
        int attackBefore = attackTargets.CalculateEntityCount();
        var boarding = new UnitTransportBoardingSystem();
        var boardingState = new SystemState { EntityManager = em, Enabled = true };
        boardingState.Begin(); try { boarding.OnCreate(ref boardingState); } finally { boardingState.End(); }
        bool boardingRan = false;
        boardingState.Begin();
        try { if (boardingState.Enabled && boardingState.RequiredQueriesPresent) { boarding.OnUpdate(ref boardingState); boardingRan = true; } }
        finally { boardingState.End(); }
        var attack = new UnitTransportDeployAttackSystem();
        var attackState = new SystemState { EntityManager = em, Enabled = true };
        attackState.Begin(); try { attack.OnCreate(ref attackState); } finally { attackState.End(); }
        // Airdrop is not admitted in this diagnostic. Do not run its dependent
        // attack owner when work exists; that would invent an owner order.
        bool airdropRequested = Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_AIRDROP_OWNER_DIAGNOSTIC") == "1";
        if (!airdropRequested && attackState.Enabled && attackState.RequiredQueriesPresent)
            throw new NotSupportedException("Original airdrop owner must precede deploy attack");
        JObject airdropResult = null;
        if (airdropRequested) {
            bool supportRequested = Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_SUPPORT_OWNER_DIAGNOSTIC") == "1";
            var type = System.Reflection.Assembly.GetExecutingAssembly().GetType(supportRequested ? "S004SupportOwnerDiagnostic" : "S004AirdropOwnerDiagnostic");
            var run = type?.GetMethod("Run", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (run == null) throw new InvalidOperationException("Original airdrop diagnostic is not compiled");
            airdropResult = (JObject)run.Invoke(null, new object[] { em });
        }
        return new JObject { ["scope"] = "SingleOriginalDeployOwner_NotFullScheduler",
            ["matching_orders_before"] = before, ["matching_orders_after"] = orders.CalculateEntityCount(),
            ["owner_update_invoked"] = ran,
            ["boarding_targets_before"] = boardingBefore, ["boarding_targets_after"] = boardingTargets.CalculateEntityCount(),
            ["boarding_update_invoked"] = boardingRan, ["deploy_attack_targets_before"] = attackBefore,
            ["deploy_attack_update_invoked"] = airdropResult != null && (bool)airdropResult["deploy_attack_update_invoked"], ["airdrop_owner_admitted"] = airdropRequested,
            ["source_airdrop_diagnostic"] = airdropResult,
            ["native_input_claimed"] = false, ["mission_ready"] = false };
    }
}
