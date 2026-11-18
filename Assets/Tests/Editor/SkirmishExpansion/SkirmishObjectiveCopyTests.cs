using System;
using System.Linq;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests.Editor
{
    // Player copy for the Frontline Control (S007), Breakthrough (S013) and Convoy Escort (S019) prototypes.
    // Farsi is a draft pending review; these tests only require that it exists and is not the English text.
    public sealed class SkirmishObjectiveCopyTests
    {
        private static readonly (string id, SkirmishObjectiveKind kind)[] Missions =
        {
            ("S007", SkirmishObjectiveKind.FrontlineControl),
            ("S013", SkirmishObjectiveKind.Breakthrough),
            ("S019", SkirmishObjectiveKind.ConvoyEscort),
        };

        private static readonly string[] Locales = { GameLocalization.EnglishLocaleCode, GameLocalization.PersianLocaleCode };

        private GameLocalizationCatalog _catalog;
        private string _previousLocale;

        [SetUp]
        public void SetUp()
        {
            _previousLocale = GameLocalization.CurrentLocaleCode;
            _catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            Assert.IsNotNull(_catalog, "Localization catalog missing.");
            GameLocalization.Initialize(_catalog, GameLocalization.EnglishLocaleCode, false);
        }

        [TearDown]
        public void TearDown() => GameLocalization.Initialize(_catalog, _previousLocale, false);

        private static SkirmishResolvedSetup Setup(string id, SkirmishObjectiveKind kind)
        {
            string p = "skirmish." + id.ToLowerInvariant() + ".";
            return new SkirmishResolvedSetup
            {
                CatalogId = id, ObjectiveKind = kind, ObjectiveKey = p + "objective",
                ResultVictoryKey = p + "result.victory", ResultDefeatKey = p + "result.defeat"
            };
        }

        private static bool HasArabicScript(string text) => text.Any(c => c >= '؀' && c <= 'ۿ');

        private static void AssertPlayerText(string english, string farsi, string context)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(english), context + ": empty English");
            Assert.IsFalse(string.IsNullOrWhiteSpace(farsi), context + ": empty Farsi");
            Assert.IsFalse(english.StartsWith("ui.", StringComparison.Ordinal) || english.Contains("skirmish."), context + ": key shown: " + english);
            Assert.AreNotEqual("MATCH COMPLETE", english, context + ": neutral fallback");
            Assert.IsTrue(HasArabicScript(farsi), context + ": Farsi has no Persian text: " + farsi);
            Assert.AreNotEqual(english, farsi, context + ": Farsi equals English");
        }

        [Test]
        public void EveryEndReasonAndOutcomeShowsPlayerTextForFcBtCe()
        {
            foreach (var (id, kind) in Missions)
            {
                var setup = Setup(id, kind);
                foreach (SkirmishEndReasonKind reason in Enum.GetValues(typeof(SkirmishEndReasonKind)))
                {
                    if (reason == SkirmishEndReasonKind.None) continue;
                    foreach (var outcome in new[] { SkirmishOutcomeKind.Victory, SkirmishOutcomeKind.Defeat, SkirmishOutcomeKind.Draw })
                    {
                        string context = id + "/" + reason + "/" + outcome;
                        Assert.IsTrue(SkirmishExpandedCopyProjection.TryResolveHud(setup, outcome, reason, Locales[0], out var en), context);
                        Assert.IsTrue(SkirmishExpandedCopyProjection.TryResolveHud(setup, outcome, reason, Locales[1], out var fa), context);
                        AssertPlayerText(en.Result, fa.Result, context);
                        Assert.AreNotEqual(reason.ToString(), en.Result, context + ": raw reason name");
                    }
                }
            }
        }

        [Test]
        public void TicketsEndingsNameTheZoneOutcome()
        {
            var setup = Setup("S007", SkirmishObjectiveKind.FrontlineControl);
            SkirmishExpandedCopyProjection.TryResolveHud(setup, SkirmishOutcomeKind.Victory, SkirmishEndReasonKind.TicketsDepleted, GameLocalization.EnglishLocaleCode, out var win);
            SkirmishExpandedCopyProjection.TryResolveHud(setup, SkirmishOutcomeKind.Defeat, SkirmishEndReasonKind.TicketsDepleted, GameLocalization.EnglishLocaleCode, out var loss);
            StringAssert.Contains("You held the zones", win.Result);
            StringAssert.Contains("The enemy held the zones", loss.Result);
            Assert.AreEqual("ui.skirmish.reason.ticketsdepleted.victory", win.ResultKey);
            Assert.AreEqual("ui.skirmish.reason.ticketsdepleted.defeat", loss.ResultKey);
        }

        [Test]
        public void PlainTimeLimitOnANonBaseObjectiveReadsAsObjectiveDeadline()
        {
            var setup = Setup("S013", SkirmishObjectiveKind.Breakthrough);
            Assert.IsTrue(SkirmishExpandedCopyProjection.TryResolveHud(setup, SkirmishOutcomeKind.Defeat, SkirmishEndReasonKind.TimeLimit, GameLocalization.EnglishLocaleCode, out var copy));
            Assert.AreEqual("ui.skirmish.reason.objectivedeadline", copy.ResultKey);
            StringAssert.DoesNotContain("Main Base", copy.Result);
        }

        [Test]
        public void CopyTableIsCarriedByTheCatalogAndConfigInBothLocales()
        {
            var en = _catalog.FindLocale(GameLocalization.EnglishLocaleCode);
            var fa = _catalog.FindLocale(GameLocalization.PersianLocaleCode);
            var configs = V3UiLocalizationCatalogBuilder.ReadUiStringConfigs().ToDictionary(e => e.key);
            foreach (var row in SkirmishObjectiveCopyTable.Rows)
            {
                Assert.AreEqual(row.English, En(en, row.Key), "catalog EN " + row.Key);
                Assert.AreEqual(row.Farsi, En(fa, row.Key), "catalog FA " + row.Key);
                Assert.IsTrue(configs.TryGetValue(row.Key, out var config), "config entry " + row.Key);
                Assert.AreEqual(row.English, config.source, "config source " + row.Key);
                Assert.AreEqual(row.Farsi, config.translations.Single(t => t.locale == GameLocalization.PersianLocaleCode).value, "config FA " + row.Key);
            }
        }

        private static string En(GameLocaleTable table, string key)
        {
            foreach (var record in table.Entries)
                if (record != null && record.Key == key) return record.Value;
            return null;
        }

        [Test]
        public void MissionCardsAndBriefingsHaveNoFallbackText()
        {
            foreach (var (id, kind) in Missions)
            {
                string lower = id.ToLowerInvariant();
                foreach (string key in new[] { "ui.skirmish." + lower + ".description", "ui.skirmish." + lower + ".purpose" })
                {
                    string english = En(_catalog.FindLocale(GameLocalization.EnglishLocaleCode), key);
                    string farsi = En(_catalog.FindLocale(GameLocalization.PersianLocaleCode), key);
                    AssertPlayerText(english, farsi, key);
                    Assert.IsFalse(english.StartsWith("Briefing for ", StringComparison.Ordinal) ||
                                   english.StartsWith("Objective for ", StringComparison.Ordinal), key + ": generated copy");
                    Assert.IsFalse(farsi.StartsWith("توجیه ", StringComparison.Ordinal) ||
                                   farsi.StartsWith("هدف ", StringComparison.Ordinal), key + ": generated Farsi copy");
                }

                foreach (string suffix in new[] { "brief", "objective", "first_order", "result.victory", "result.defeat", "result.surrender" })
                {
                    string key = "skirmish." + lower + "." + suffix;
                    Assert.IsTrue(SkirmishObjectiveCopyTable.TryGet(key, out var row), key + " missing from the copy table");
                    AssertPlayerText(row.English, row.Farsi, key);
                }

                // The in-match objective line is the mission objective, not the Base Assault fallback.
                var setup = Setup(id, kind);
                foreach (string locale in Locales)
                {
                    Assert.IsTrue(SkirmishExpandedCopyProjection.TryResolveHud(setup, SkirmishOutcomeKind.None, SkirmishEndReasonKind.None, locale, out var hud));
                    StringAssert.DoesNotContain("Barracks", hud.Objective, id + " objective uses Base Assault text");
                    Assert.AreEqual(SkirmishExpandedCopyProjection.Resolve(setup.ObjectiveKey, locale, string.Empty), hud.Objective);
                }

                // First player action comes from the HUD guidance data path used by S026.
                Assert.IsTrue(SkirmishMissionHudGuidanceCatalog.TryGet(id, out var guidance), id + " guidance row");
                Assert.IsTrue(guidance.HasFirstOrder);
                Assert.IsTrue(SkirmishObjectiveCopyTable.TryGet(guidance.FirstOrderKey, out var order));
                Assert.AreEqual(order.English, guidance.FirstOrderEnglish);
            }
        }

        [Test]
        public void BriefingCountsMatchTheObjectiveRules()
        {
            string[] words = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve" };
            SkirmishObjectiveCopyTable.TryGet("skirmish.s013.objective", out var bt);
            StringAssert.Contains(words[SkirmishBreakthroughReducer.RequiredEvacuated], bt.English);
            StringAssert.Contains(words[SkirmishBreakthroughReducer.OriginalCount], bt.English);
            SkirmishObjectiveCopyTable.TryGet("skirmish.s019.objective", out var ce);
            StringAssert.Contains(words[SkirmishConvoyReducer.RequiredDelivered], ce.English);
            StringAssert.Contains(words[SkirmishConvoyReducer.OriginalCount], ce.English);
        }
    }
}
