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
            resultTitle = copy.Result;
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
            reason = match.Reason == SkirmishEndReason.Surrender
                ? SkirmishEndReasonKind.Surrender
                : match.Reason == SkirmishEndReason.TimeLimit
                    ? SkirmishEndReasonKind.TimeLimit
                    : match.Reason == SkirmishEndReason.BothBasesDestroyed
                        ? SkirmishEndReasonKind.BothBasesDestroyed
                        : match.Reason == SkirmishEndReason.MainBaseDestroyed
                            ? SkirmishEndReasonKind.MainBaseDestroyed
                            : SkirmishEndReasonKind.None;
        }
    }
}
