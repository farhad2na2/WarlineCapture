using System;
using Game.Runtime;
using Game.Configs;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishEnemyDecisionRulesTests
    {
        public static void RunFocusedValidation()
        {
            int checks = 0;
            void Check(bool condition, string reason)
            {
                checks++; if (!condition) throw new InvalidOperationException(reason);
            }
            Check(!SkirmishEnemyDecisionRules.ShouldSwitch(40,true,54,15,false), "Below-margin plan must persist");
            Check(!SkirmishEnemyDecisionRules.ShouldSwitch(40,true,55,15,false), "Exactly-margin plan must persist");
            Check(SkirmishEnemyDecisionRules.ShouldSwitch(40,true,56,15,false), "Higher utility must replace plan");
            Check(SkirmishEnemyDecisionRules.ShouldSwitch(100,false,1,15,false), "Invalid current plan cannot lock planner");
            Check(SkirmishEnemyDecisionRules.ShouldSwitch(100,true,80,15,true), "Terminal danger interrupts commitment");
            Check(!SkirmishEnemyDecisionRules.Due(4,4,true,5), "Periodic tick must not bypass chosen reaction delay");
            Check(SkirmishEnemyDecisionRules.Due(5,8,true,5), "Reaction event must not wait for periodic tick");
            Check(SkirmishEnemyDecisionRules.Due(8,8,false,10), "Periodic reevaluation must run when due");
            Check(SkirmishEnemyDecisionRules.CoalesceResponse(true,5,4,3)==5, "New event cannot postpone pending response");
            Check(SkirmishEnemyDecisionRules.CoalesceResponse(false,0,4,3)==7, "Reaction delay starts at observation");
            Check(SkirmishEnemyDecisionRules.ReserveTarget(0,25)==0, "Empty army does not invent reserve");
            Check(SkirmishEnemyDecisionRules.ReserveTarget(21,25)==6, "Reserve target rounds upward before whole-group allocation");
            Check(!SkirmishEnemyDecisionRules.Progressed(0,0,1,0,2), "Subthreshold movement does not reset stuck timer");
            Check(SkirmishEnemyDecisionRules.Progressed(0,0,2,0,2), "Real movement clears stuck timer");
            Check(SkirmishEnemyDecisionRules.SameDestination(0,0,1,1,2), "Small centroid jitter must not spam orders");
            Check(!SkirmishEnemyDecisionRules.SameDestination(0,0,3,0,2), "Material destination change must invalidate key");
            Check(SkirmishEnemyDecisionRules.ObserveDue(1.1f,1.25f,true,1.1f), "Pending due response bypasses cheap probe cadence");
            Check(!SkirmishEnemyDecisionRules.ObserveDue(1.09f,1.25f,true,1.1f), "Response must not run before deadline");
            foreach (int length in new[] { 90, 360 })
            {
                var route = new SkirmishEnemyRouteSnapshot("two", SkirmishMeasuredRouteKind.MainHighway, 4, true, false,
                    new[] { new SkirmishEnemyWaypoint(0,0),new SkirmishEnemyWaypoint(length,0) },length,20,10,false);
                Check(math.distance(SkirmishEnemyDecisionRules.ScoutWaypoint(route,1,1,out _),new float3(length/3f,0,0))<.001f,
                    "Two-waypoint scout advances to physical nearest third");
                Check(math.distance(SkirmishEnemyDecisionRules.ScoutWaypoint(route,-1,0,out _),new float3(length*2/3f,0,0))<.001f,
                    "Reverse scout advances from its actual home endpoint");
            }
            var uneven = new SkirmishEnemyRouteSnapshot("uneven",SkirmishMeasuredRouteKind.MainHighway,4,true,false,
                new[] {new SkirmishEnemyWaypoint(0,0),new SkirmishEnemyWaypoint(10,0),new SkirmishEnemyWaypoint(120,0)},120,20,10,false);
            Check(math.distance(SkirmishEnemyDecisionRules.ScoutWaypoint(uneven,1,2,out _),new float3(40,0,0))<.001f,
                "Scout limit uses physical distance rather than waypoint count");
            var frozen = new[] {new float3(500,0,0),new float3(20,0,0),new float3(float.NaN,0,0)};
            Check(SkirmishEnemyDecisionRules.InvestigationDestination(float3.zero,30,new float3(30,0,0),float3.zero,frozen).x==20,
                "Investigation selects only finite frozen knowledge inside patrol reach");
            var intent = new SkirmishEnemyProductionIntentV1Component { State=SkirmishEnemyProductionIntentState.AcceptedWaiting,Role=SkirmishRoleKind.Rifle,ReservationId=12 };
            Check(!SkirmishEnemyCommanderService.MayStartProduction(intent,SkirmishRoleKind.Rocketeer),"Accepted purchase cannot become another semantic action");
            intent.State=SkirmishEnemyProductionIntentState.Abandoned;intent.RejectDomain=(byte)SkirmishEnemyRejectDomain.NativeResult;
            Check(!SkirmishEnemyCommanderService.MayStartProduction(intent,SkirmishRoleKind.Rifle),"Native abandonment cannot be renamed by stock income");
            intent.RejectDomain=(byte)SkirmishEnemyRejectDomain.Feasibility;
            Check(SkirmishEnemyCommanderService.MayStartProduction(intent,SkirmishRoleKind.Rifle),"Feasibility retry may reach authoritative preflight again");
            Debug.Log("[SkirmishEnemyDecisionRulesTests] result=Passed checks="+checks);
        }
    }
}
