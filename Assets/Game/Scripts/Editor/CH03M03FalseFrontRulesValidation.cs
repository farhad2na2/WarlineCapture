using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M03FalseFrontRulesValidation
    {
        [MenuItem("Game/Campaign/False Front/Validate Rules")]
        public static void Run()
        {
            var rules = new CampaignMissionMarketLifelineDefinitionBlob {RequiredDeliveries = 3, ManifestHoldMilliseconds = 5000, VictoryHoldMilliseconds = 10000, DeadlineMilliseconds = 540000};
            var state = new CampaignMissionMarketLifelineState {LegitimateManifestInspected = 1, CorruptManifestInspected = 1, ManifestVerified = 1, DeliveredCount = 3, VictoryHoldMilliseconds = 10000};
            Require(CampaignMissionMarketLifelineRuleUtility.IsVictory(in state, in rules), "Two verified approaches, three vehicles and a cleared ambush must win.");
            state.CorruptManifestInspected = 0; state.ManifestVerified = 0;
            Require(!CampaignMissionMarketLifelineRuleUtility.IsVictory(in state, in rules), "One observation point cannot confirm the route.");
            state.CorruptManifestInspected = 1; state.ManifestVerified = 1; state.DeliveredCount = 2;
            Require(!CampaignMissionMarketLifelineRuleUtility.IsVictory(in state, in rules), "All three civilian vehicles must reach shelter.");
            state.DeliveredCount = 3; state.Failure = MarketLifelineFailure.WrongTransfer;
            Require(!CampaignMissionMarketLifelineRuleUtility.IsVictory(in state, in rules), "The decoy route must not count as safe evacuation.");
            Require(CampaignMissionSequence.Next(CampaignMissionSequence.SafehouseSweep) == CampaignMissionSequence.FalseFront, "Safehouse Sweep must unlock False Front.");
            Debug.Log("[FalseFrontRules] result=Passed observations=2 civilianVehicles=3 ambush=required decoyRoute=failure controls=existing-only");
        }

        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
