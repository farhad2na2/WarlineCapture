using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;

namespace Game.Tests.Editor
{
    public sealed class SkirmishEndReasonProjectionTests
    {
        [TestCase(SkirmishEndReasonKind.None, "None", 0)]
        [TestCase(SkirmishEndReasonKind.MainBaseDestroyed, "MainBaseDestroyed", 1)]
        [TestCase(SkirmishEndReasonKind.BothBasesDestroyed, "BothBasesDestroyed", 2)]
        [TestCase(SkirmishEndReasonKind.TimeLimit, "TimeLimit", 3)]
        [TestCase(SkirmishEndReasonKind.Surrender, "Surrender", 4)]
        [TestCase(SkirmishEndReasonKind.TicketsDepleted, "TicketsDepleted", 5)]
        [TestCase(SkirmishEndReasonKind.TicketsAtDeadline, "TicketsAtDeadline", 6)]
        [TestCase(SkirmishEndReasonKind.BreakthroughEvacuated, "BreakthroughEvacuated", 7)]
        [TestCase(SkirmishEndReasonKind.BreakthroughImpossible, "BreakthroughImpossible", 8)]
        [TestCase(SkirmishEndReasonKind.ConvoyDelivered, "ConvoyDelivered", 9)]
        [TestCase(SkirmishEndReasonKind.ConvoyImpossible, "ConvoyImpossible", 10)]
        [TestCase(SkirmishEndReasonKind.ObjectiveDeadline, "ObjectiveDeadline", 11)]
        [TestCase(SkirmishEndReasonKind.StartupFailure, "StartupFailure", 12)]
        [TestCase(SkirmishEndReasonKind.ContentFailure, "ContentFailure", 13)]
        [TestCase(SkirmishEndReasonKind.CheckpointFailure, "CheckpointFailure", 14)]
        public void FrozenResultProjectsHarnessToken(SkirmishEndReasonKind reason, string token, int value)
        {
            using var world = new World(nameof(FrozenResultProjectsHarnessToken));
            var em = world.EntityManager;
            var session = em.CreateEntity(typeof(SkirmishMatchState), typeof(SkirmishResultComponent));
            em.SetComponentData(session, new SkirmishResultComponent
                { Frozen = 1, Outcome = SkirmishOutcomeKind.Defeat, Reason = reason });
            SkirmishExpandedSessionControlService.ProjectTerminalMatch(em, session);
            var projected = em.GetComponentData<SkirmishMatchState>(session).Reason;
            Assert.AreEqual(value, (int)projected);
            Assert.AreEqual(token, projected.ToString());
            if (reason != SkirmishEndReasonKind.None)
                Assert.AreNotEqual(SkirmishEndReason.None, projected);
        }

        [TestCase(SkirmishEndReason.TicketsDepleted, SkirmishObjectiveKind.FrontlineControl, "ui.skirmish.reason.ticketsdepleted")]
        [TestCase(SkirmishEndReason.TicketsAtDeadline, SkirmishObjectiveKind.FrontlineControl, "ui.skirmish.reason.ticketsatdeadline")]
        [TestCase(SkirmishEndReason.BreakthroughEvacuated, SkirmishObjectiveKind.Breakthrough, "ui.skirmish.reason.breakthroughevacuated")]
        [TestCase(SkirmishEndReason.BreakthroughImpossible, SkirmishObjectiveKind.Breakthrough, "ui.skirmish.reason.breakthroughimpossible")]
        [TestCase(SkirmishEndReason.ConvoyDelivered, SkirmishObjectiveKind.ConvoyEscort, "ui.skirmish.reason.convoydelivered")]
        [TestCase(SkirmishEndReason.ConvoyImpossible, SkirmishObjectiveKind.ConvoyEscort, "ui.skirmish.reason.convoyimpossible")]
        [TestCase(SkirmishEndReason.ObjectiveDeadline, SkirmishObjectiveKind.Breakthrough, "ui.skirmish.reason.objectivedeadline")]
        [TestCase(SkirmishEndReason.StartupFailure, SkirmishObjectiveKind.FrontlineControl, "ui.skirmish.reason.startupfailure")]
        [TestCase(SkirmishEndReason.ContentFailure, SkirmishObjectiveKind.Breakthrough, "ui.skirmish.reason.contentfailure")]
        [TestCase(SkirmishEndReason.CheckpointFailure, SkirmishObjectiveKind.ConvoyEscort, "ui.skirmish.reason.checkpointfailure")]
        public void HudForwardsReasonKeyAndUsesNeutralFallback(
            SkirmishEndReason matchReason, SkirmishObjectiveKind objective, string expectedKey)
        {
            var setup = new SkirmishResolvedSetup
                { CatalogId = "S999", ObjectiveKind = objective, ObjectiveKey = "missing.objective" };
            var match = new SkirmishMatchState { Outcome = SkirmishOutcome.Defeat, Reason = matchReason };
            using var world = new World(nameof(HudForwardsReasonKeyAndUsesNeutralFallback));
            var em = world.EntityManager;
            var session = em.CreateEntity();
            em.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = setup });

            SkirmishExpandedHudCopy.MapOutcome(match, out var outcome, out var reason);
            foreach (string locale in new[] { GameLocalization.EnglishLocaleCode, GameLocalization.PersianLocaleCode })
            {
                Assert.AreEqual("missing", SkirmishExpandedCopyProjection.Resolve(expectedKey, locale, "missing"));
                Assert.IsTrue(SkirmishExpandedCopyProjection.TryResolveHud(setup, outcome, reason, locale, out var copy));
                Assert.AreEqual(expectedKey, copy.ResultKey);
                string neutral = SkirmishExpandedCopyProjection.Resolve("ui.skirmish.result.match_complete", locale, "MATCH COMPLETE");
                Assert.AreEqual(neutral, copy.Result);
                Assert.IsTrue(SkirmishExpandedHudCopy.TryResolve(em, session, match, locale,
                    out _, out _, out string detail));
                Assert.AreEqual(neutral, detail);
            }
        }

        [TestCase(SkirmishObjectiveKind.FrontlineControl)]
        [TestCase(SkirmishObjectiveKind.Breakthrough)]
        [TestCase(SkirmishObjectiveKind.ConvoyEscort)]
        public void MissingReasonFromHudUsesNeutralCopy(SkirmishObjectiveKind objective)
        {
            var setup = new SkirmishResolvedSetup
                { CatalogId = "S999", ObjectiveKind = objective, ObjectiveKey = "missing.objective" };
            foreach (string locale in new[] { GameLocalization.EnglishLocaleCode, GameLocalization.PersianLocaleCode })
            {
                Assert.IsTrue(SkirmishExpandedCopyProjection.TryResolveHud(setup, SkirmishOutcomeKind.Victory,
                    SkirmishEndReasonKind.None, locale, out var copy));
                Assert.AreEqual("ui.skirmish.result.match_complete", copy.ResultKey);
                Assert.AreEqual(SkirmishExpandedCopyProjection.Resolve(copy.ResultKey, locale, "MATCH COMPLETE"), copy.Result);
            }
        }
    }
}
