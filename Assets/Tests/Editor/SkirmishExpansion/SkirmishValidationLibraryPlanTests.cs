#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    /// <summary>Pure tests for the Editor validation library plan: which Planned authored rows are presented, and what stays untouched.</summary>
    public sealed class SkirmishValidationLibraryPlanTests
    {
        private static string DefinitionIdOf(string id) => SkirmishDefinitionId.FromCatalog(new SkirmishCatalogId(id)).Value;

        private static SkirmishBattleCatalogEntry Planned(string id, string title = "Title") => new()
        {
            ScenarioId = id, Status = SkirmishBattleCatalogStatus.Planned, PlayableScenarioIndex = -1, TitleEnglish = title,
            TitleKey = "skirmish." + id.ToLowerInvariant() + ".title", DescriptionEnglish = "Brief", DescriptionFarsi = "خلاصه"
        };

        private static SkirmishBattleCatalogEntry Playable(string id, int index) => new()
        {
            ScenarioId = id, Status = SkirmishBattleCatalogStatus.Playable, PlayableScenarioIndex = index, TitleEnglish = "Shipped",
            DefinitionId = "shipped." + id, ContentVersion = 9
        };

        private static SkirmishValidationLibraryPlan.AuthoredSource Source(string id, int version = 1, bool map = true, bool layout = true,
            string definitionId = null) =>
            new(id, definitionId ?? DefinitionIdOf(id), version, map, layout);

        [Test]
        public void AuthoredPlannedMissionIsPresentedAsABadgedLaunchableCardWithATransportIndex()
        {
            var shipped = new List<SkirmishBattleCatalogEntry> { Planned("S026", "Mountain Pass") };
            var plan = SkirmishValidationLibraryPlan.Build(shipped, new[] { Source("S026", 3) });
            Assert.AreEqual(new[] { "S026" }, plan.Planned.ToArray());
            var row = plan.Rows[0];
            Assert.IsTrue(row.IsPlayable);
            Assert.AreEqual(1026, row.PlayableScenarioIndex);
            Assert.AreEqual(DefinitionIdOf("S026"), row.DefinitionId);
            Assert.AreEqual(3, row.ContentVersion);
            StringAssert.StartsWith("[VALIDATION] ", row.TitleEnglish);
            StringAssert.EndsWith("Mountain Pass", row.TitleEnglish);
            Assert.AreEqual(string.Empty, row.TitleKey, "a localization key would hide the badge");
            StringAssert.StartsWith("Validation candidate, not published.", row.DescriptionEnglish);
            StringAssert.StartsWith("Validation candidate, not published.", row.DescriptionFarsi);
        }

        [Test]
        public void ShippedRowsAreNeverModifiedInPlace()
        {
            var shipped = new List<SkirmishBattleCatalogEntry> { Planned("S026") };
            SkirmishValidationLibraryPlan.Build(shipped, new[] { Source("S026") });
            Assert.AreEqual(SkirmishBattleCatalogStatus.Planned, shipped[0].Status);
            Assert.AreEqual(-1, shipped[0].PlayableScenarioIndex);
            Assert.AreEqual("Title", shipped[0].TitleEnglish);
        }

        [Test]
        public void RowOrderIsKeptAndAlreadyPlayableRowsAreUntouched()
        {
            var shipped = new List<SkirmishBattleCatalogEntry> { Playable("S002", 4), Planned("S026"), Planned("S027") };
            var plan = SkirmishValidationLibraryPlan.Build(shipped, new[] { Source("S026") });
            Assert.AreEqual("S002", plan.Rows[0].ScenarioId);
            Assert.AreEqual("Shipped", plan.Rows[0].TitleEnglish);
            Assert.AreEqual(4, plan.Rows[0].PlayableScenarioIndex);
            Assert.AreEqual(new[] { "S026" }, plan.Planned.ToArray());
            Assert.IsFalse(plan.Rows[2].IsPlayable, "no authored definition: stays hidden");
            Assert.AreEqual(1, plan.WithoutDefinition);
            Assert.AreEqual(0, plan.Rejected.Count);
        }

        [Test]
        public void MissionsWithoutASingleCompleteAuthoredDefinitionAreRejectedWithReasons()
        {
            var shipped = new List<SkirmishBattleCatalogEntry>
            {
                Planned("S010"), Planned("S011"), Planned("S012"), Planned("S013"), Planned("S014"), Planned("S015")
            };
            var authored = new List<SkirmishValidationLibraryPlan.AuthoredSource>
            {
                Source("S010", map: false),
                Source("S011", layout: false),
                Source("S012", version: 0),
                Source("S013", definitionId: "skirmish.s099"),
                Source("S014"), Source("S014")
            };
            var plan = SkirmishValidationLibraryPlan.Build(shipped, authored);
            Assert.AreEqual(0, plan.Planned.Count);
            CollectionAssert.AreEquivalent(new[]
            {
                "S010:mapOrLayoutNotBound", "S011:mapOrLayoutNotBound", "S012:contentVersionNotPositive",
                "S013:definitionIdMismatch", "S014:ambiguousAuthoredDefinition"
            }, plan.Rejected);
            Assert.AreEqual(1, plan.WithoutDefinition, "S015 has no authored definition");
            foreach (var row in plan.Rows) Assert.IsFalse(row.IsPlayable);
        }

        [Test]
        public void LegacyIndexMissionsDuplicateRowsAndMalformedRowsAreRejected()
        {
            var malformed = Planned("S030");
            malformed.PlayableScenarioIndex = 5;
            var shipped = new List<SkirmishBattleCatalogEntry> { Planned("S005"), Planned("S026"), Planned("S026"), malformed };
            var plan = SkirmishValidationLibraryPlan.Build(shipped,
                new[] { Source("S005"), Source("S026"), Source("S030") });
            Assert.AreEqual(0, plan.Planned.Count);
            CollectionAssert.AreEquivalent(new[]
            {
                "S005:legacyIndexMission", "S026:duplicateLibraryRow", "S026:duplicateLibraryRow", "S030:malformedPlannedRow"
            }, plan.Rejected);
        }

        [Test]
        public void TransportIndexCollisionWithAnAlreadyPlayableRowIsRejected()
        {
            var shipped = new List<SkirmishBattleCatalogEntry> { Playable("S002", 1026), Planned("S026") };
            var plan = SkirmishValidationLibraryPlan.Build(shipped, new[] { Source("S026") });
            Assert.AreEqual(0, plan.Planned.Count);
            Assert.AreEqual(new[] { "S026:transportIndexCollision" }, plan.Rejected.ToArray());
        }

        [Test]
        public void BuildIsIdempotentOverItsOwnOutputAndNeverDoubleBadges()
        {
            var shipped = new List<SkirmishBattleCatalogEntry> { Planned("S026", "Pass") };
            var first = SkirmishValidationLibraryPlan.Build(shipped, new[] { Source("S026") });
            var second = SkirmishValidationLibraryPlan.Build(first.Rows, new[] { Source("S026") });
            Assert.AreEqual(0, second.Planned.Count, "a flipped row is already visible, not a candidate again");
            Assert.AreEqual(first.Rows[0].TitleEnglish, second.Rows[0].TitleEnglish);
            // A title that already carries the badge is not prefixed twice.
            var badged = Planned("S027", "[VALIDATION] Ridge");
            var plan = SkirmishValidationLibraryPlan.Build(new[] { badged }, new[] { Source("S027") });
            Assert.AreEqual("[VALIDATION] Ridge", plan.Rows[0].TitleEnglish);
        }

        [Test]
        public void EmptyDescriptionsStillCarryTheBadgeAndMissingInputsAreSafe()
        {
            var row = Planned("S026");
            row.DescriptionEnglish = null;
            row.DescriptionFarsi = string.Empty;
            var plan = SkirmishValidationLibraryPlan.Build(new[] { row }, new[] { Source("S026") });
            Assert.AreEqual("Validation candidate, not published.", plan.Rows[0].DescriptionEnglish);
            Assert.AreEqual("Validation candidate, not published.", plan.Rows[0].DescriptionFarsi);
            Assert.AreEqual(0, SkirmishValidationLibraryPlan.Build(null, null).Rows.Length);
        }

        [Test]
        public void LogLinesNameThePlannedIdsAndStateNothingIsPublished()
        {
            var shipped = new List<SkirmishBattleCatalogEntry> { Planned("S026"), Planned("S049"), Planned("S050"), Planned("S099") };
            var plan = SkirmishValidationLibraryPlan.Build(shipped, new[] { Source("S026"), Source("S049"), Source("S050", map: false) });
            string line = SkirmishValidationLibraryPlan.FormatEnabled(plan, "unitTest");
            StringAssert.StartsWith("[SkirmishValidationLibrary] enabled=1 planned=S026,S049 ", line);
            StringAssert.Contains("rejected=S050:mapOrLayoutNotBound", line);
            StringAssert.Contains("withoutDefinition=1", line);
            StringAssert.Contains("scope=EditorMemoryOnly shippedCatalogModified=0 published=0 readinessGranted=0", line);
            StringAssert.Contains("planned=none", SkirmishValidationLibraryPlan.FormatEnabled(
                SkirmishValidationLibraryPlan.Build(new List<SkirmishBattleCatalogEntry>(), null), "unitTest"));
            Assert.AreEqual("[SkirmishValidationLibrary] enabled=0 reason=catalogMissing", SkirmishValidationLibraryPlan.FormatDisabled("catalogMissing"));
        }

        [Test]
        public void MenuJourneyPassedLineMarksValidationCandidatesOnlyWhenAsked()
        {
            var path = new[] { "Card_Skirmish", "ScenarioCard_S026", "LaunchMissionButton" };
            string plain = SkirmishMenuJourneyLog.FormatPassed("S026", path, 7, "Standard", "Regular");
            StringAssert.DoesNotContain("validationCandidate", plain);
            string candidate = SkirmishMenuJourneyLog.FormatPassed("S026", path, 7, "Standard", "Regular", true);
            StringAssert.EndsWith("matchStartedFromMenu=1 validationCandidate=1 published=0", candidate);
        }

        [Test]
        public void ValidationLibraryMakesAPlannedAuthoredMissionPassTheMenuPreflight()
        {
            var shipped = new List<SkirmishBattleCatalogEntry> { Planned("S026") };
            StringAssert.StartsWith("catalogEntryNotPlayable", SkirmishMenuJourneyLog.Preflight(shipped, "S026"));
            var plan = SkirmishValidationLibraryPlan.Build(shipped, new[] { Source("S026") });
            Assert.IsNull(SkirmishMenuJourneyLog.Preflight(plan.Rows, "S026"));
        }
    }
}
#endif
