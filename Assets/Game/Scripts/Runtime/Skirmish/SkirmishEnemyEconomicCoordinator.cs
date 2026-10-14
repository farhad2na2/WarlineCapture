using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public enum SkirmishEnemyEconomicGate : byte
    { None, ConstructionPending, OtherPaidPending, ConstructionRequested, NativeFitUnknown, CurrentRoleBlocked, FitWindowClosed, ConstructionQuoteRejected }

    // Stateless admission, not an extra update loop. Native construction,
    // production and research remain the sole debit/delivery owners.
    public static class SkirmishEnemyEconomicCoordinator
    {
        public struct Decision
        {
            public SkirmishEnemyEconomyService.Decision Economy;
            public SkirmishEnemyEconomicGate Gate;
            public string StructureId;
        }
        // Call after EnsureSession and before ObserveDue can return. Tactical
        // orders must not stop while a paid request or exact refund reconciles.
        public static void Reconcile(EntityManager em,Entity session)
        {
            SkirmishEnemyConstructionAdapter.Tick(em,session);
            if(!em.Exists(session) || !em.HasComponent<SkirmishEnemyProductionIntentV1Component>(session))return;
            var intent=em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(session);
            if(intent.State==SkirmishEnemyProductionIntentState.RequestPending || intent.State==SkirmishEnemyProductionIntentState.AcceptedWaiting)
            {
                if(!SkirmishEnemyEconomyService.TryReadContext(em,session,2,out var context))return;
                var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                // Exact already-owned token/receipt reconciliation only. Backoff
                // admission remains inside the one strategic spending decision.
                SkirmishEnemyPaidProductionAdapter.Tick(em,session,SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId),context.Elapsed);
            }
        }

        public static Decision Tick(EntityManager em,Entity session,SkirmishRoleKind combatRole,
            bool immediateDefense,SkirmishRoleKind readinessGoal,ref SkirmishEnemyPlannerV1Component planner,
            SkirmishRoleKind capabilityGoal=SkirmishRoleKind.None)
        {
            Reconcile(em,session);
            var result=new Decision();
            if(!SkirmishEnemyEconomyService.TryReadContext(em,session,2,out var context))return result;
            if(SkirmishEnemyConstructionAdapter.BlocksOtherPaidActions(em,session))
            {result.Gate=SkirmishEnemyEconomicGate.ConstructionPending;return result;}
            if(context.OpenResearchId!=0)
            {result.Gate=SkirmishEnemyEconomicGate.OtherPaidPending;
             result.Economy.Action=SkirmishEnemyEconomyAction.ResearchPending;result.Economy.ResearchId=context.OpenResearchId;return result;}
            var paid=em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(session);
            bool pending=paid.State==SkirmishEnemyProductionIntentState.RequestPending || paid.State==SkirmishEnemyProductionIntentState.AcceptedWaiting;
            if(em.HasBuffer<SkirmishProductionReservation>(session))foreach(var row in em.GetBuffer<SkirmishProductionReservation>(session))
                if(row.FactionId==2 && (row.Phase==SkirmishReservationPhase.Reserved || row.Phase==SkirmishReservationPhase.Producing))pending=true;
            if(pending){result.Gate=SkirmishEnemyEconomicGate.OtherPaidPending;result.Economy.Action=SkirmishEnemyEconomyAction.PaidIntentPending;return result;}
            bool urgent=immediateDefense && combatRole!=SkirmishRoleKind.None &&
                SkirmishEnemyPaidProductionAdapter.CanDispatchNow(em,session,combatRole);
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if(!urgent)
            {
                long reserve=RoleCost(setup,combatRole)+(context.LogisticsMembers==0?RoleCost(setup,SkirmishRoleKind.LogisticsTruck):0);
                if(reserve<=int.MaxValue)
                {
                    var needs=new[]{combatRole,context.LogisticsMembers==0?SkirmishRoleKind.LogisticsTruck:SkirmishRoleKind.None,
                        readinessGoal,capabilityGoal};
                    foreach(var role in needs)
                    {
                        if(!Supported(setup,role,out var overlay))continue;
                        string structure=SkirmishStructureIds.FromVisualKey(SkirmishProductionCatalog.ProducerPrefabKey(overlay.Producer));
                        if(SkirmishEnemyConstructionAdapter.CountLiving(em,session,structure)==0)
                        {
                            if(TryConstruction(em,session,context,role,structure,(int)reserve,ref planner,ref result))return result;
                            break; // Highest missing actual goal, not speculative lower-priority expansion.
                        }
                        if(context.Readiness<SkirmishProductionEligibility.RequiredReadiness(role) &&
                            SkirmishEnemyConstructionAdapter.CountLiving(em,session,SkirmishStructureIds.Barracks)==0)
                        {
                            if(TryConstruction(em,session,context,role,SkirmishStructureIds.Barracks,(int)reserve,ref planner,ref result))return result;
                            break;
                        }
                    }
                    // Only recover original own supply facilities; no second
                    // factory/pump or new facility from a global mission assumption.
                    if(result.StructureId==null && setup.Structures!=null)
                        foreach(var original in setup.Structures)
                        {
                            if(original.FactionId!=2 || !SupplyFacility(original.StructureId) ||
                                SkirmishEnemyConstructionAdapter.CountLiving(em,session,original.StructureId)>0)continue;
                            var benefit=SkirmishRoleKind.None;
                            foreach(var role in needs)if(Supported(setup,role,out _)){benefit=role;break;}
                            if(TryConstruction(em,session,context,benefit,original.StructureId,(int)reserve,ref planner,ref result))return result;
                            break;
                        }
                }
            }
            var recovery=default(SkirmishEnemyEconomyService.ResearchRecoveryEvidence);
            if(SkirmishEnemyConstructionAdapter.TryReadResearchRecoveryEvidence(em,session,out var attempt,out var failed,
                out var previous,out var rebuilt))
                recovery=new SkirmishEnemyEconomyService.ResearchRecoveryEvidence(attempt,failed,previous,rebuilt);
            result.Economy=SkirmishEnemyEconomyService.Tick(em,session,2,combatRole,immediateDefense,readinessGoal,
                planner.PlanRevision,in recovery);
            return result;
        }
        static bool TryConstruction(EntityManager em,Entity session,SkirmishEnemyEconomyService.Context context,
            SkirmishRoleKind benefit,string structure,int reserve,ref SkirmishEnemyPlannerV1Component planner,ref Decision result)
        {
            result.StructureId=structure;
            if(string.IsNullOrEmpty(structure) || !TryReadNativeFit(em,session,benefit,context.Readiness,out float seconds,out int researchCost))
            {result.Gate=SkirmishEnemyEconomicGate.NativeFitUnknown;return false;}
            if(!ProjectedRoleFitsCurrentStocks(em,session,benefit))
            {result.Gate=SkirmishEnemyEconomicGate.CurrentRoleBlocked;return false;}
            if(context.Remaining<=seconds){result.Gate=SkirmishEnemyEconomicGate.FitWindowClosed;return false;}
            // Protect only the next real research payment. Reserving all
            // future steps before building would exceed the native cap and
            // deadlock a legal sequential readiness path. Full timing stays gated.
            var required=SkirmishProductionEligibility.RequiredReadiness(benefit);
            int nextResearch=context.Readiness>=required?0:context.Readiness<SkirmishReadinessStage.Established?
                SkirmishResearchCosts.ReadinessEstablishedMaterials:SkirmishResearchCosts.ReadinessFullArsenalMaterials;
            long protectedMaterials=(long)reserve+nextResearch;
            if(protectedMaterials>int.MaxValue || planner.NextActionKey==uint.MaxValue ||
                !SkirmishEnemyConstructionAdapter.TryPrepare(em,session,structure,(int)protectedMaterials,out var quote))
            {result.Gate=SkirmishEnemyEconomicGate.ConstructionQuoteRejected;return false;}
            var key=new FixedString64Bytes("enemy.build."+(planner.NextActionKey+1));
            if(!SkirmishEnemyConstructionAdapter.TryRequest(em,session,quote,key,planner.PlanRevision))
            {result.Gate=SkirmishEnemyEconomicGate.ConstructionQuoteRejected;return false;}
            planner.NextActionKey++;result.Gate=SkirmishEnemyEconomicGate.ConstructionRequested;return true;
        }
        // Native queue metadata and authored native research durations only.
        // No yield forecast, construction speed or income rate is invented.
        public static bool TryReadNativeFit(EntityManager em,Entity session,SkirmishRoleKind role,
            SkirmishReadinessStage readiness,out float seconds,out int researchMaterials)
        {
            seconds=0;researchMaterials=0;
            if(!em.Exists(session) || !em.HasComponent<SkirmishProductionCatalogRecord>(session) ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session))return false;
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var catalog=em.GetComponentObject<SkirmishProductionCatalogRecord>(session).Catalog;
            if(!Supported(setup,role,out _) || catalog==null || !catalog.TryReadProductionDeliverySeconds(role,out seconds) ||
                !float.IsFinite(seconds) || seconds<=0)return false;
            var required=SkirmishProductionEligibility.RequiredReadiness(role);
            if(readiness<SkirmishReadinessStage.Established && required>=SkirmishReadinessStage.Established)
            {seconds+=SkirmishResearchCosts.ReadinessEstablishedSeconds;researchMaterials+=SkirmishResearchCosts.ReadinessEstablishedMaterials;}
            if(readiness<SkirmishReadinessStage.FullArsenal && required>=SkirmishReadinessStage.FullArsenal)
            {seconds+=SkirmishResearchCosts.ReadinessFullArsenalSeconds;researchMaterials+=SkirmishResearchCosts.ReadinessFullArsenalMaterials;}
            return float.IsFinite(seconds);
        }
        // A projection admits no producer/readiness and pays nothing. It checks
        // the same native role/cap/fuel rules before investing in a missing one.
        static bool ProjectedRoleFitsCurrentStocks(EntityManager em,Entity session,SkirmishRoleKind role)
        {
            if(!em.HasComponent<SkirmishEnemyCapacityComponent>(session))return false;
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var cap=em.GetComponentData<SkirmishEnemyCapacityComponent>(session);
            var request=new SkirmishProductionRequest{RoleId=SkirmishRoleIds.ToId(role),RoleKind=role,SquadCount=1,
                BarracksPresent=true,GroundStagingPresent=true,HelipadPresent=true,AirportPresent=true,IntelStationPresent=true,
                EnforceStocks=true,MaterialsAvailable=SkirmishMaterialsService.Read(em,session,2),FuelAvailable=SkirmishStartingSupplyService.ReadFuel(em,session,2),
                InfantryCap=cap.InfantryCap,GroundCap=cap.GroundCap,AirCap=cap.AirCap,SupplyCap=cap.SupplyCap,
                InfantryLive=cap.InfantryLive,GroundLive=cap.GroundLive,AirLive=cap.AirLive,SupplyLive=cap.SupplyLive,
                InfantryReserved=cap.InfantryReserved,GroundReserved=cap.GroundReserved,AirReserved=cap.AirReserved,SupplyReserved=cap.SupplyReserved};
            var decision=SkirmishProductionEligibility.Evaluate(request,SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId),
                SkirmishProductionEligibility.RequiredReadiness(role),setup.RoleOverlays);
            return decision.Accepted && (SkirmishRoleIds.Category(role)!=SkirmishPopulationCategory.LogisticsSupport ||
                (long)SkirmishEnemyEconomyService.CountOwnedOrPending(em,session,2,SkirmishRoleKind.LogisticsTruck)+
                SkirmishEnemyEconomyService.CountOwnedOrPending(em,session,2,SkirmishRoleKind.Tanker)+decision.MemberCount<=setup.LogisticsSupportCapEach);
        }
        static bool Supported(SkirmishResolvedSetup setup,SkirmishRoleKind role,out SkirmishRoleOverlay overlay)
        {
            overlay=default;var army=setup==null?null:SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
            return role!=SkirmishRoleKind.None && army!=null && army.Allows(SkirmishRoleIds.ToId(role)) &&
                (!SkirmishRoleIds.IsOffensiveAir(role) || army.AllowsOffensiveAir) &&
                SkirmishRoleOverlayCatalog.TryGet(setup.RoleOverlays,role,out overlay);
        }
        static int RoleCost(SkirmishResolvedSetup setup,SkirmishRoleKind role) =>
            Supported(setup,role,out var overlay)?System.Math.Max(0,overlay.MaterialsCost):0;
        static bool SupplyFacility(string id) => id==SkirmishStructureIds.OilPump || id==SkirmishStructureIds.Refinery ||
            id==SkirmishStructureIds.FuelBladder || id==SkirmishStructureIds.FabricationDepot;
    }
}
