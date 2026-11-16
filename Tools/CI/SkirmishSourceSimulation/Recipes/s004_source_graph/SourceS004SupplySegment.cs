using System;
using Game.Components;
using Game.Configs;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    // One original supply-policy segment, not the complete ARIA planner.
    internal sealed class SourceS004SupplySegment
    {
        internal enum PriorityBlock { None, PublicHomeThreat, OpeningDefense }
        internal static PriorityBlock EarlierPriority(bool threat, bool air, int infantry, int towers, byte assaultStarted)
        {
            if (threat) return PriorityBlock.PublicHomeThreat;
            if (air && infantry >= 16 && towers < 2 && assaultStarted == 0) return PriorityBlock.OpeningDefense;
            return PriorityBlock.None;
        }
        readonly EntityManager em;
        readonly Entity session;
        readonly SkirmishArmyProfileConfig army;
        readonly SourceAriaRecruitmentBoundary recruitment;
        AriaPreparationState state;

        internal SourceS004SupplySegment(EntityManager em, Entity session,
            SkirmishArmyProfileConfig army, SourceAriaRecruitmentBoundary recruitment)
        {
            this.em = em ?? throw new ArgumentNullException(nameof(em));
            this.army = army ?? throw new ArgumentNullException(nameof(army));
            this.recruitment = recruitment ?? throw new ArgumentNullException(nameof(recruitment));
            this.session = session;
            if (!ReferenceEquals(recruitment.WorldIdentity, em) || recruitment.SessionIdentity != session)
                throw new InvalidOperationException("Same-world semantic recruitment required");
        }

        internal Result Step()
        {
#if !WARLINE_S004_PUBLIC_THREAT_PROBE
            throw new NotSupportedException("Source public home-threat observation required before supply decisions");
#else
            if (!em.Exists(session) || !em.HasComponent<SkirmishMatchState>(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session))
                throw new InvalidOperationException("Actual expanded source session required");
            var match = em.GetComponentData<SkirmishMatchState>(session);
            var expanded = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (setup == null || setup.CatalogId != "S004" || setup.ArmyProfileId != army.Kind ||
                match.ScenarioIndex != 6 || match.Phase != SkirmishPhase.Playing ||
                match.Outcome != SkirmishOutcome.None || expanded.Phase != SkirmishSessionPhase.Playing)
                throw new InvalidOperationException("Exact active S004 source session required");
            float now = (float)em.WorldUnmanaged.Time.ElapsedTime;
            if (!float.IsFinite(now) || now < 0) throw new InvalidOperationException("Actual source clock required");
            var observed = SkirmishAriaPublicProjection.FromSession(em, session, army);
#if WARLINE_S004_PUBLIC_THREAT_PROBE
            var publicHomeThreat = SourceAriaPublicHomeThreat.ReadS004(em, session);
#endif
            var logistics = recruitment.Read(SkirmishRoleKind.LogisticsTruck);
            var tanker = recruitment.Read(SkirmishRoleKind.Tanker);
            var facts = new AriaPreparationView {
                AirProfile=observed.AirProfile, CombinedArmsField=setup.ArmyProfileId==SkirmishArmyProfileId.CombinedArms &&
                    setup.StartPackageId==SkirmishStartPackageId.FieldBase,
                Infantry=observed.OwnInfantry, Time=now, OwnStructureAttackUnits=observed.OwnStructureAttackUnits,
                LogisticsTruckCommitted=observed.LogisticsTruckCommitted, CanAffordLogisticsTruck=observed.CanAffordLogisticsTruck,
                LogisticsTargetAvailable=logistics.Present && logistics.CanRequest,
                TankersCommitted=observed.TankersCommitted, CanAffordTanker=observed.CanAffordTanker,
                TankerTargetAvailable=tanker.Present && tanker.CanRequest };
            bool threatPresent=false;
#if WARLINE_S004_PUBLIC_THREAT_PROBE
            threatPresent=publicHomeThreat.HasValue;
#endif
            // The original planner considers opening defenses before supply. Until
            // the construction owner is available here, do not issue supply when
            // that earlier stage could need to build a tower.
            var earlier=EarlierPriority(threatPresent,observed.AirProfile,observed.OwnInfantry,observed.OwnDefenseTowers,state.AssaultStarted);
            if (earlier==PriorityBlock.PublicHomeThreat)
                return new Result(facts,logistics,tanker,default,false,default,state,observed.OwnDefenseTowers,false,true,true);
            if (earlier==PriorityBlock.OpeningDefense)
                return new Result(facts,logistics,tanker,default,false,default,state,observed.OwnDefenseTowers,true);
            var action=AriaExpandedPreparationPolicy.Supply(facts,ref state);
            SkirmishRoleKind role;
            if(action==AriaPreparationAction.RecruitLogisticsTruck)role=SkirmishRoleKind.LogisticsTruck;
            else if(action==AriaPreparationAction.RecruitTanker)role=SkirmishRoleKind.Tanker;
            else return new Result(facts,logistics,tanker,action,false,default,state,observed.OwnDefenseTowers,false);
            var receipt=recruitment.Request(role,now);
            return new Result(facts,logistics,tanker,action,true,receipt,state,observed.OwnDefenseTowers,false);
#endif
        }

        internal readonly struct Result
        {
            internal readonly AriaPreparationView Facts;
            internal readonly SourceAriaRecruitmentSurface Logistics, Tanker;
            internal readonly AriaPreparationAction Action;
            internal readonly bool RequestAttempted;
            internal readonly SourceAriaRecruitmentReceipt Receipt;
            internal readonly AriaPreparationState State;
            internal readonly int OwnDefenseTowers;
            internal readonly bool OpeningDefensePriorityUnresolved;
            internal readonly bool PublicThreatChecked, PublicThreatPresent;
            internal Result(AriaPreparationView facts,SourceAriaRecruitmentSurface logistics,
                SourceAriaRecruitmentSurface tanker,AriaPreparationAction action,bool attempted,
                SourceAriaRecruitmentReceipt receipt,AriaPreparationState state,int towers,bool defenseUnresolved,
                bool publicThreatChecked=false,bool publicThreatPresent=false)
            { Facts=facts; Logistics=logistics; Tanker=tanker; Action=action; RequestAttempted=attempted; Receipt=receipt; State=state; OwnDefenseTowers=towers; OpeningDefensePriorityUnresolved=defenseUnresolved;
#if WARLINE_S004_PUBLIC_THREAT_PROBE
              PublicThreatChecked=true; PublicThreatPresent=publicThreatPresent;
#else
              PublicThreatChecked=publicThreatChecked; PublicThreatPresent=publicThreatPresent;
#endif
            }
        }
    }
}
