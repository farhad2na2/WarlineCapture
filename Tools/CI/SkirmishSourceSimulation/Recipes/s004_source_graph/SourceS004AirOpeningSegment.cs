using System;
using Game.Components;
using Game.Configs;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    // A bounded source-only segment, not a substitute for the complete ARIA planner.
    // It does not construct presentation targets or claim player/native input.
    internal sealed class SourceS004AirOpeningSegment
    {
        readonly EntityManager em;
        readonly Entity session;
        readonly SkirmishArmyProfileConfig army;
        readonly SourceAriaRecruitmentBoundary recruitment;
        AriaPreparationState state;

        internal SourceS004AirOpeningSegment(EntityManager em, Entity session,
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
            var publicView = SkirmishAriaPublicProjection.FromSession(em, session, army);
            var offer = recruitment.Read(SkirmishRoleKind.Rifle);
            var facts = new AriaPreparationView { AirProfile = publicView.AirProfile,
                Infantry = publicView.OwnInfantry, Time = now, RifleRecruitPending = publicView.RifleRecruitPending,
                CanAffordRifle = publicView.CanAffordRifle };
            if (!offer.Present) return new Result(false, offer.CanRequest, offer.Reason,
                AriaAirOpeningAction.NotApplicable, false, default, facts, state);
            var action = AriaExpandedPreparationPolicy.AirOpening(facts, ref state);
            if (action != AriaAirOpeningAction.RecruitRifle || !offer.CanRequest)
                return new Result(true, offer.CanRequest, offer.Reason, action, false, default, facts, state);
            var command = recruitment.Request(SkirmishRoleKind.Rifle, now);
            return new Result(true, true, offer.Reason, action, true, command, facts, state);
        }

        internal readonly struct Result
        {
            internal readonly bool RolePresent, CanRequest, RequestAttempted;
            internal readonly string AvailabilityReason;
            internal readonly AriaAirOpeningAction Action;
            internal readonly SourceAriaRecruitmentReceipt Receipt;
            internal readonly AriaPreparationView Facts;
            internal readonly AriaPreparationState State;
            internal Result(bool present, bool canRequest, string reason, AriaAirOpeningAction action,
                bool attempted, SourceAriaRecruitmentReceipt receipt, AriaPreparationView facts, AriaPreparationState state)
            { RolePresent = present; CanRequest = canRequest; AvailabilityReason = reason;
              Action = action; RequestAttempted = attempted; Receipt = receipt; Facts = facts; State = state; }
        }
    }
}
