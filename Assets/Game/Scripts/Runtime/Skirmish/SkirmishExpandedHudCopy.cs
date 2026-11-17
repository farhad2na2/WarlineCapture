using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishExpandedHudCopy
    {
        public static bool TryResolve(
            EntityManager em,
            Entity session,
            SkirmishMatchState match,
            string locale,
            out string objective,
            out string resultTitle,
            out string resultDetail)
        {
            objective = string.Empty;
            resultTitle = string.Empty;
            resultDetail = string.Empty;
            if (!em.HasComponent<SkirmishResolvedSetupRecord>(session))
                return false;
            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (setup == null ||
                (!SkirmishExpandedCopyProjection.IsExpandedS002(setup.CatalogId) &&
                 string.IsNullOrEmpty(setup.ObjectiveKey)))
                return false;

            MapOutcome(match, out SkirmishOutcomeKind outcome, out SkirmishEndReasonKind reason);
            if (!SkirmishExpandedCopyProjection.TryResolveHud(setup, outcome, reason, locale, out SkirmishHudCopy copy))
                return false;
            objective = copy.Objective;
            // Keep the shared localized Victory/Defeat/Draw heading; this copy
            // explains the mission-specific reason beneath it.
            resultDetail = copy.Result;
            return true;
        }

        public static void MapOutcome(
            SkirmishMatchState match,
            out SkirmishOutcomeKind outcome,
            out SkirmishEndReasonKind reason)
        {
            outcome = match.Outcome == SkirmishOutcome.Victory
                ? SkirmishOutcomeKind.Victory
                : match.Outcome == SkirmishOutcome.Defeat
                    ? SkirmishOutcomeKind.Defeat
                    : match.Outcome == SkirmishOutcome.Draw
                        ? SkirmishOutcomeKind.Draw
                        : SkirmishOutcomeKind.None;
            switch (match.Reason)
            {
                case SkirmishEndReason.MainBaseDestroyed: reason = SkirmishEndReasonKind.MainBaseDestroyed; break;
                case SkirmishEndReason.BothBasesDestroyed: reason = SkirmishEndReasonKind.BothBasesDestroyed; break;
                case SkirmishEndReason.TimeLimit: reason = SkirmishEndReasonKind.TimeLimit; break;
                case SkirmishEndReason.Surrender: reason = SkirmishEndReasonKind.Surrender; break;
                case SkirmishEndReason.TicketsDepleted: reason = SkirmishEndReasonKind.TicketsDepleted; break;
                case SkirmishEndReason.TicketsAtDeadline: reason = SkirmishEndReasonKind.TicketsAtDeadline; break;
                case SkirmishEndReason.BreakthroughEvacuated: reason = SkirmishEndReasonKind.BreakthroughEvacuated; break;
                case SkirmishEndReason.BreakthroughImpossible: reason = SkirmishEndReasonKind.BreakthroughImpossible; break;
                case SkirmishEndReason.ConvoyDelivered: reason = SkirmishEndReasonKind.ConvoyDelivered; break;
                case SkirmishEndReason.ConvoyImpossible: reason = SkirmishEndReasonKind.ConvoyImpossible; break;
                case SkirmishEndReason.ObjectiveDeadline: reason = SkirmishEndReasonKind.ObjectiveDeadline; break;
                case SkirmishEndReason.StartupFailure: reason = SkirmishEndReasonKind.StartupFailure; break;
                case SkirmishEndReason.ContentFailure: reason = SkirmishEndReasonKind.ContentFailure; break;
                case SkirmishEndReason.CheckpointFailure: reason = SkirmishEndReasonKind.CheckpointFailure; break;
                default: reason = SkirmishEndReasonKind.None; break;
            }
        }
    }
}
