using System;
using System.Collections.Generic;
using System.Linq;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.Editor
{
    public sealed class SkirmishLibraryJourneyTests
    {
        private const string SetupPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab";
        private static readonly string[] Objectives = { "BA", "FC", "BT", "CE" };
        private static readonly string[] Armies = { "G", "G", "A", "A", "C", "C" };
        private static readonly string[] Starts = { "F", "E", "F", "E", "F", "E" };

        [TearDown]
        public void TearDown()
        {
            SkirmishLibraryLaunchContext.Difficulty = SkirmishDifficultyId.Regular;
            SkirmishLibraryReturn.Queue(string.Empty);
        }

        [Test]
        public void NormalInputJourneyMatchesLedgerAndCatalogCounts()
        {
            List<SkirmishBattleCatalogEntry> entries = DesertGridPlusCity();
            var ledger = new SkirmishMedalLedger(null, entries);
            var empty = Model(entries, new SkirmishMedalLedger(null, entries));
            Assert.AreEqual(0, Pin(empty, "DB").WonCount);
            Assert.AreEqual(24, Pin(empty, "DB").PublishedCount);
            Assert.AreEqual(0, empty.Theater.OverallWonCount);
            Assert.AreEqual(25, empty.Theater.OverallPublishedCount);
            Assert.AreEqual(100, empty.Theater.OverallMedalsPossible);

            Assert.IsTrue(ledger.TryAward(Victory("S001", SkirmishDifficultyId.Recruit), out _));
            Assert.IsTrue(ledger.TryAward(Victory("S001", SkirmishDifficultyId.Regular), out _));
            Assert.IsTrue(ledger.TryAward(Victory("S002", SkirmishDifficultyId.Veteran), out _));
            Assert.IsTrue(ledger.TryAward(Victory("S007", SkirmishDifficultyId.Commander), out _));
            Assert.AreEqual(3, ledger.CountFor("DB"));
            Assert.AreEqual(2, ledger.CountFor("DB", "BA"));
            Assert.AreEqual(1, ledger.CountFor("DB", "FC"));
            Assert.AreEqual(3, ledger.BattlesWon());

            var model = Model(entries, ledger);
            SkirmishLibraryInteraction last = default;
            int sounds = 0;
            model.Interaction += interaction =>
            {
                last = interaction;
                sounds++;
            };
            Assert.IsTrue(SkirmishLibraryNavigation.OpenTheaterBattlefieldCard(model, "DB", "S002"));
            Assert.GreaterOrEqual(sounds, 1);
            Assert.AreEqual(SkirmishLibraryInteractionKind.Selected, last.Kind);
            Assert.AreEqual("S002", last.TargetId);
            Assert.AreEqual(UIAudioEventKind.CardSelect, last.Sound);
            Assert.AreEqual(SkirmishLibraryLevel.Battlefield, model.Level);
            Assert.AreEqual("S002", model.Briefing.ScenarioId);
            Assert.IsTrue(model.Briefing.CanLaunch);
            Assert.AreEqual(SkirmishDifficultyId.Regular, SkirmishLibraryLaunchContext.Difficulty);

            SkirmishTheaterPinState desert = Pin(model, "DB");
            Assert.AreEqual(ledger.CountFor("DB"), desert.WonCount);
            Assert.AreEqual(24, desert.PublishedCount);
            Assert.AreEqual(1, desert.Medals.Recruit);
            Assert.AreEqual(1, desert.Medals.Regular);
            Assert.AreEqual(1, desert.Medals.Veteran);
            Assert.AreEqual(1, desert.Medals.Commander);
            Assert.IsFalse(desert.IsRecommended);

            SkirmishTheaterPinState city = Pin(model, "CC");
            Assert.AreEqual(0, city.WonCount);
            Assert.AreEqual(1, city.PublishedCount);
            Assert.IsTrue(city.IsRecommended);
            Assert.AreEqual("S025", model.Theater.RecommendedScenarioId);
            Assert.AreEqual(3, model.Theater.OverallWonCount);
            Assert.AreEqual(25, model.Theater.OverallPublishedCount);
            Assert.AreEqual(4, model.Theater.OverallMedalsEarned);
            Assert.AreEqual(100, model.Theater.OverallMedalsPossible);
            Assert.AreEqual(1, model.Theater.OverallMedalTotals.Recruit);
            Assert.AreEqual(1, model.Theater.OverallMedalTotals.Regular);
            Assert.AreEqual(1, model.Theater.OverallMedalTotals.Veteran);
            Assert.AreEqual(1, model.Theater.OverallMedalTotals.Commander);

            Assert.AreEqual(2, Tab(model, "BA").WonCount);
            Assert.AreEqual(6, Tab(model, "BA").PublishedCount);
            Assert.AreEqual(1, Tab(model, "FC").WonCount);
            Assert.AreEqual(6, Tab(model, "FC").PublishedCount);
            Assert.AreEqual(0, Tab(model, "BT").WonCount);
            Assert.AreEqual(6, Tab(model, "BT").PublishedCount);
            Assert.AreEqual(0, Tab(model, "CE").WonCount);
            Assert.AreEqual(6, Tab(model, "CE").PublishedCount);
            Assert.AreEqual(6, model.Battlefield.MapCards.Length);
            Assert.AreEqual(24, model.Battlefield.GridCards.Length);

            AssertMedal(model, ledger, "S001", "DB.BA.0", SkirmishMedalMask.Recruit | SkirmishMedalMask.Regular);
            AssertMedal(model, ledger, "S002", "DB.BA.1", SkirmishMedalMask.Veteran);
            AssertMedal(model, ledger, "S007", "DB.FC.0", SkirmishMedalMask.Commander);
            Assert.IsTrue(Card(model.Battlefield.MapCards, "DB.BA.1").IsSelected);
            Assert.AreEqual(SkirmishCardAccess.Playable, Card(model.Battlefield.MapCards, "DB.BA.1").Access);
            Assert.AreEqual(string.Empty, Card(model.Battlefield.MapCards, "DB.BA.1").LockedReasonLocKey);

            AssertResolved(model.Briefing.TitleLocKey);
            AssertResolved(model.Briefing.DescriptionLocKey);
            AssertResolved(model.Briefing.PurposeLocKey);
            AssertResolved(desert.TitleLocKey);
            AssertResolved(desert.SubtitleLocKey);
            AssertResolved(city.TitleLocKey);
            AssertResolved(city.SubtitleLocKey);
            for (int i = 0; i < model.Battlefield.Tabs.Length; i++)
                AssertResolved(model.Battlefield.Tabs[i].TitleLocKey);
            for (int i = 0; i < model.Battlefield.GridCards.Length; i++)
            {
                SkirmishBattleCardState card = model.Battlefield.GridCards[i];
                if (string.IsNullOrEmpty(card.ScenarioId))
                    continue;
                AssertResolved(card.TitleLocKey);
                AssertResolved(card.SubtitleLocKey);
                AssertResolved(card.ArmyLocKey);
                AssertResolved(card.StartLocKey);
            }
        }

        [Test]
        public void PublishedCountsFollowCatalogRowsRatherThanAFixedGrid()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0),
                Entry("S002", "DB", "BA", "G", "E", true, 2)
            };
            var model = Model(entries, new SkirmishMedalLedger(null, entries));
            Assert.IsTrue(model.SelectMapCollection("DB"));
            Assert.AreEqual(2, Pin(model, "DB").PublishedCount);
            Assert.AreEqual(0, Pin(model, "DB").WonCount);
            Assert.AreEqual(2, Tab(model, "BA").PublishedCount);
            Assert.AreEqual(0, Tab(model, "FC").PublishedCount);
            Assert.AreEqual(6, model.Battlefield.MapCards.Length);
            Assert.AreEqual(24, model.Battlefield.GridCards.Length);
            Assert.AreEqual(2, model.Theater.OverallPublishedCount);
            Assert.AreEqual(8, model.Theater.OverallMedalsPossible);
        }

        [Test]
        public void TheaterHeaderTotalsUsePlayableCountsInReleaseAndDev()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0),
                Entry("S002", "DB", "BA", "G", "E", true, 2),
                Entry("S003", "DB", "BA", "A", "F", true, 4),
                Entry("S006", "DB", "BA", "C", "E", false, -1)
            };
            var ledger = new SkirmishMedalLedger(null, entries);
            Assert.IsTrue(ledger.TryAward(Victory("S001", SkirmishDifficultyId.Recruit), out _));
            Assert.IsTrue(ledger.TryAward(Victory("S001", SkirmishDifficultyId.Regular), out _));
            Assert.IsTrue(ledger.TryAward(Victory("S002", SkirmishDifficultyId.Veteran), out _));
            Assert.IsFalse(ledger.TryAward(Victory("S006", SkirmishDifficultyId.Commander), out _));
            Assert.AreEqual(2, ledger.BattlesWon());

            SkirmishTheaterState release = Model(entries, ledger, showPlanned: false).Theater;
            Assert.AreEqual(2, release.OverallWonCount);
            Assert.AreEqual(3, release.OverallPublishedCount);
            Assert.AreEqual(3, release.OverallMedalsEarned);
            Assert.AreEqual(release.OverallPublishedCount * 4, release.OverallMedalsPossible);
            Assert.AreEqual(1, release.OverallMedalTotals.Recruit);
            Assert.AreEqual(1, release.OverallMedalTotals.Regular);
            Assert.AreEqual(1, release.OverallMedalTotals.Veteran);
            Assert.AreEqual(0, release.OverallMedalTotals.Commander);

            SkirmishTheaterState dev = Model(entries, ledger, showPlanned: true).Theater;
            Assert.AreEqual(release.OverallWonCount, dev.OverallWonCount);
            Assert.AreEqual(release.OverallPublishedCount, dev.OverallPublishedCount);
            Assert.AreEqual(3, dev.OverallPublishedCount);
            Assert.AreEqual(release.OverallMedalsEarned, dev.OverallMedalsEarned);
            Assert.AreEqual(12, dev.OverallMedalsPossible);
            Assert.AreEqual(dev.OverallPublishedCount * 4, dev.OverallMedalsPossible);
            Assert.AreEqual(release.OverallMedalTotals.Recruit, dev.OverallMedalTotals.Recruit);
            Assert.AreEqual(release.OverallMedalTotals.Regular, dev.OverallMedalTotals.Regular);
            Assert.AreEqual(release.OverallMedalTotals.Veteran, dev.OverallMedalTotals.Veteran);
            Assert.AreEqual(release.OverallMedalTotals.Commander, dev.OverallMedalTotals.Commander);
        }

        [Test]
        public void FreshProfileNavigationKeepsS001PlayableAndLocksTheRest()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0),
                Entry("S002", "DB", "BA", "G", "E", true, 2),
                Entry("S025", "CC", "BA", "G", "F", true, 1),
                Entry("S073", "IB", "BA", "G", "F", true, 3)
            };
            using (ContentAccessRuntime.UseValidationProvider(new OwnsNothing()))
            using (var world = new World(nameof(FreshProfileNavigationKeepsS001PlayableAndLocksTheRest)))
            {
                var model = Model(entries, new SkirmishMedalLedger(null, entries), id => ContentAccessRuntime.Evaluate(id));
                Assert.IsTrue(SkirmishLibraryNavigation.OpenTheaterBattlefieldCard(model, "DB", "S001"));
                Assert.AreEqual(SkirmishCardAccess.Playable, model.Briefing.Access);
                Assert.AreEqual(string.Empty, model.Briefing.LockedReasonLocKey);
                Assert.IsTrue(model.Launch());

                foreach (var locked in new[] { ("S002", "DB"), ("S025", "CC"), ("S073", "IB") })
                {
                    Assert.IsTrue(SkirmishLibraryNavigation.OpenTheaterBattlefieldCard(model, locked.Item2, locked.Item1), locked.Item1);
                    Assert.AreEqual(SkirmishCardAccess.NotOwned, model.Briefing.Access, locked.Item1);
                    Assert.AreEqual("ui.skirmish.locked.not_owned", model.Briefing.LockedReasonLocKey, locked.Item1);
                    Assert.IsFalse(string.IsNullOrEmpty(model.Briefing.DescriptionLocKey), locked.Item1);
                    Assert.AreEqual(ContentAccessAuthority.CampaignProduct, model.Briefing.StoreRouteProductId, locked.Item1);
                    Assert.IsFalse(model.Briefing.CanLaunch, locked.Item1);
                    Assert.IsFalse(model.Launch(), locked.Item1);
                    Assert.IsFalse(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                        world.EntityManager, locked.Item1, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, 1,
                        null, null, null, out _, out _, out List<SkirmishCompileReason> denied));
                    Assert.AreEqual("contentAccess", denied[0].Field, locked.Item1);
                }

                Assert.IsFalse(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                    world.EntityManager, "S001", SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, 1,
                    null, null, null, out _, out _, out List<SkirmishCompileReason> sample));
                Assert.IsFalse(sample.Exists(reason => reason.Field == "contentAccess"));
            }
        }

        [Test]
        public void ShippedFreshProfileResolverRejectsEveryNonSampleCatalogEntry()
        {
            var entries=SkirmishBattleCatalogConfig.Load().Entries;Assert.AreEqual(120,entries.Count);
            using(ContentAccessRuntime.UseValidationProvider(new OwnsNothing()))
            using(var world=new World(nameof(ShippedFreshProfileResolverRejectsEveryNonSampleCatalogEntry)))
                foreach(var entry in entries)
                {
                    if(entry.ScenarioId=="S001")continue;
                    Assert.IsFalse(SkirmishExpandedLaunchResolver.TryCompileAndQueue(world.EntityManager,entry.ScenarioId,
                        SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,1,null,null,null,out _,out _,out List<SkirmishCompileReason> reasons),entry.ScenarioId);
                    Assert.AreEqual("contentAccess",reasons[0].Field,entry.ScenarioId);
                }
        }

        [Test]
        public void LibraryLocKeysResolveInEnglishAndPersian()
        {
            GameLocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            Assert.IsNotNull(catalog);
            string previous = GameLocalization.CurrentLocaleCode;
            try
            {
                GameLocalization.Initialize(catalog, GameLocalization.EnglishLocaleCode, false);
                List<V3UiLocalizationCatalogBuilder.UiStringConfigEntry> entries = V3UiLocalizationCatalogBuilder.ReadUiStringConfigs();
                var keys = new HashSet<string>(entries.Select(entry => entry.key), StringComparer.Ordinal);
                for (int i = 1; i <= 120; i++)
                {
                    string id = i.ToString("000");
                    foreach (string suffix in new[] { "title", "subtitle", "description", "purpose" })
                        Assert.IsTrue(keys.Contains("ui.skirmish.s" + id + "." + suffix), suffix + " " + id);
                }

                foreach (string key in new[]
                         {
                             "ui.skirmish.theater.mp.title",
                             "ui.skirmish.theater.ap.title",
                             "ui.skirmish.theater.db.subtitle",
                             "ui.skirmish.objective.ba",
                             "ui.skirmish.army.g",
                             "ui.skirmish.start.f",
                             "ui.skirmish.size.standard",
                             "ui.skirmish.size.war",
                             "ui.skirmish.size.large_war",
                             "ui.skirmish.locked.not_owned",
                             "ui.skirmish.medal.first",
                             "ui.skirmish.result.battlefield"
                         })
                    Assert.IsTrue(keys.Contains(key), key);

                int resolved = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    V3UiLocalizationCatalogBuilder.UiStringConfigEntry entry = entries[i];
                    if (!IsLibraryKey(entry.key))
                        continue;
                    GameLocalization.SetLocale(GameLocalization.EnglishLocaleCode, false);
                    Assert.AreEqual(entry.source, GameLocalization.Get(entry.key), entry.key);
                    string persian = entry.translations.Single(translation => translation.locale == GameLocalization.PersianLocaleCode).value;
                    GameLocalization.SetLocale(GameLocalization.PersianLocaleCode, false);
                    Assert.AreEqual(persian, GameLocalization.Get(entry.key), entry.key);
                    Assert.AreNotEqual(entry.source, persian, entry.key);
                    resolved++;
                }

                Assert.GreaterOrEqual(resolved, (120 * 4) + 8);
            }
            finally
            {
                GameLocalization.Initialize(catalog, previous, false);
            }
        }

        [Test]
        public void FirstMedalLineIsVisibleOnlyForAFirstAward()
        {
            SkirmishResultMedalLine first = SkirmishResultMedalPresentation.Describe(true, "S002", (byte)SkirmishDifficultyId.Regular);
            Assert.IsTrue(first.Visible);
            Assert.AreEqual("ui.skirmish.medal.first", first.FirstLocKey);
            Assert.AreEqual("ui.skirmish.medal.regular", first.DifficultyLocKey);
            Assert.IsFalse(SkirmishResultMedalPresentation.Describe(false, "S002", (byte)SkirmishDifficultyId.Regular).Visible);
            Assert.IsFalse(SkirmishResultMedalPresentation.Describe(true, string.Empty, (byte)SkirmishDifficultyId.Recruit).Visible);
            Assert.IsFalse(SkirmishResultMedalPresentation.Describe(true, "S002", 0).Visible);
            Assert.IsFalse(SkirmishResultMedalPresentation.Describe(true, "S002", 5).Visible);
            Assert.AreEqual("ui.skirmish.medal.commander", SkirmishResultMedalPresentation.Describe(true, "S007", 4).DifficultyLocKey);
        }

        [Test]
        public void ResultCardShowsTheFirstMedalAndBattlefieldBack()
        {
            var root = new GameObject("ResultCardHost", typeof(RectTransform));
            try
            {
                CampaignStyleResultCard card = CampaignStyleResultCard.Mount(root.transform, null);
                bool returned = false;
                card.Bind(() => { }, () => { }, () => { }, () => { });
                card.BindBattlefield(() => returned = true);
                card.Present(new ModeResultContent
                {
                    Mode = ResultScreenMode.Skirmish, Victory = true,
                    Title = "VICTORY",
                    ShowMedal = true,
                    MedalText = "First medal\nS002\nRegular medal",
                    ShowBattlefieldBack = true,
                    BattlefieldBackLabel = "BATTLEFIELD",
                    ShowAdjust = true,
                    LeaveLabel = "MAIN MENU",
                    ActionsEnabled = true,
                    Signature = "medal"
                });

                var controls=card.SharedView.Controls;
                Assert.IsTrue(controls.Medal.gameObject.activeSelf);
                Assert.AreEqual("First medal\nS002\nRegular medal", SharedMissionResultView.Source(controls.InfoBody));
                Assert.IsTrue(controls.Primary.gameObject.activeSelf);
                Assert.IsTrue(controls.Tertiary.gameObject.activeSelf);
                controls.Primary.onClick.Invoke();
                Assert.IsTrue(returned);

                card.Present(new ModeResultContent
                {
                    Mode = ResultScreenMode.Skirmish, Victory=true,
                    ShowMedal = false, ShowBattlefieldBack = false, ShowAdjust = true,
                    Signature = "medal"
                });
                Assert.IsTrue(controls.Medal.gameObject.activeSelf, "Unearned medals retain the neutral slot.");
                Assert.AreEqual("No new medal", SharedMissionResultView.Source(controls.InfoBody));
                Assert.IsFalse(controls.Primary.gameObject.activeSelf, "An unbound return action cannot be shown.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BackToLibraryRestoresTheBattlefieldCard()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0),
                Entry("S002", "DB", "BA", "G", "E", true, 2)
            };
            var model = Model(entries, new SkirmishMedalLedger(null, entries), prefs: new MemoryPrefs());
            Assert.AreEqual(SkirmishLibraryLevel.Theater, model.Level);
            SkirmishLibraryReturn.Queue("S002");
            Assert.IsTrue(SkirmishLibraryReturn.Apply(model));
            Assert.AreEqual(string.Empty, SkirmishLibraryReturn.PendingScenarioId);
            Assert.AreEqual(SkirmishLibraryLevel.Battlefield, model.Level);
            Assert.AreEqual("DB", model.Battlefield.MapId);
            Assert.AreEqual("S002", model.Briefing.ScenarioId);
            SkirmishBattleCardState card = Card(model.Battlefield.MapCards, "DB.BA.1");
            Assert.AreEqual("S002", card.ScenarioId);
            Assert.IsTrue(card.IsSelected);
        }

        [Test]
        public void QuickBattleRaisesOneClickAndDoesNotAlsoRaiseTheCardSound()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0),
                Entry("S002", "DB", "BA", "G", "E", true, 2)
            };
            var model = Model(entries, new SkirmishMedalLedger(null, entries), randomIndex: _ => 1);
            var sounds = new List<UIAudioEventKind>();
            model.Interaction += interaction => sounds.Add(interaction.Sound);
            Assert.AreEqual("S002", model.QuickBattle());
            Assert.AreEqual(1, sounds.Count);
            Assert.AreEqual(UIAudioEventKind.ButtonPrimaryClick, sounds[0]);
            Assert.IsTrue(model.Briefing.CanLaunch);
            Assert.AreEqual(SkirmishDifficultyId.Regular, SkirmishLibraryLaunchContext.Difficulty);
        }

        private static void AssertMedal(
            SkirmishLibraryViewModel model,
            SkirmishMedalLedger ledger,
            string scenarioId,
            string pinId,
            SkirmishMedalMask expected)
        {
            SkirmishBattleCardState card = Card(model.Battlefield.GridCards, pinId);
            Assert.AreEqual(scenarioId, card.ScenarioId, pinId);
            Assert.AreEqual(expected, card.Medals, pinId);
            Assert.AreEqual(ledger.Has(scenarioId, SkirmishDifficultyId.Recruit), (card.Medals & SkirmishMedalMask.Recruit) != 0, scenarioId);
            Assert.AreEqual(ledger.Has(scenarioId, SkirmishDifficultyId.Regular), (card.Medals & SkirmishMedalMask.Regular) != 0, scenarioId);
            Assert.AreEqual(ledger.Has(scenarioId, SkirmishDifficultyId.Veteran), (card.Medals & SkirmishMedalMask.Veteran) != 0, scenarioId);
            Assert.AreEqual(ledger.Has(scenarioId, SkirmishDifficultyId.Commander), (card.Medals & SkirmishMedalMask.Commander) != 0, scenarioId);
        }

        private static void AssertResolved(string key)
        {
            Assert.IsFalse(string.IsNullOrEmpty(key), "loc key");
            GameLocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            string previous = GameLocalization.CurrentLocaleCode;
            try
            {
                GameLocalization.Initialize(catalog, GameLocalization.EnglishLocaleCode, false);
                string english = GameLocalization.Get(key);
                GameLocalization.SetLocale(GameLocalization.PersianLocaleCode, false);
                string persian = GameLocalization.Get(key);
                Assert.IsFalse(string.IsNullOrEmpty(english), key);
                Assert.IsFalse(string.IsNullOrEmpty(persian), key);
                Assert.AreNotEqual(english, persian, key);
            }
            finally
            {
                GameLocalization.Initialize(catalog, previous, false);
            }
        }

        private static bool IsLibraryKey(string key) =>
            (key.StartsWith("ui.skirmish.s", StringComparison.Ordinal) && key.Length > 14 && char.IsDigit(key[14])) ||
            key.StartsWith("ui.skirmish.theater.", StringComparison.Ordinal) ||
            key.StartsWith("ui.skirmish.objective.", StringComparison.Ordinal) ||
            key.StartsWith("ui.skirmish.army.", StringComparison.Ordinal) ||
            key.StartsWith("ui.skirmish.start.", StringComparison.Ordinal) ||
            key.StartsWith("ui.skirmish.size.", StringComparison.Ordinal) ||
            key.StartsWith("ui.skirmish.locked.", StringComparison.Ordinal) ||
            key.StartsWith("ui.skirmish.medal.", StringComparison.Ordinal) ||
            key == "ui.skirmish.result.battlefield";

        private static List<SkirmishBattleCatalogEntry> DesertGridPlusCity()
        {
            var entries = new List<SkirmishBattleCatalogEntry>(25);
            int number = 1;
            for (int row = 0; row < Objectives.Length; row++)
            {
                for (int column = 0; column < Armies.Length; column++)
                {
                    entries.Add(Entry("S" + number.ToString("000"), "DB", Objectives[row], Armies[column], Starts[column], true, number - 1));
                    number++;
                }
            }

            entries.Add(Entry("S025", "CC", "BA", "G", "F", true, 24));
            return entries;
        }

        private static SkirmishLibraryViewModel Model(
            List<SkirmishBattleCatalogEntry> entries,
            SkirmishMedalLedger ledger,
            Func<string, ContentAccessState> access = null,
            Func<int, int> randomIndex = null,
            bool showPlanned = false,
            ISkirmishLibraryPrefs prefs = null) =>
            new(
                entries,
                new FakeSkirmishMedalReader(ledger),
                access ?? (_ => ContentAccessState.Allowed),
                randomIndex,
                showPlanned,
                prefs ?? new MemoryPrefs(),
                difficulty => SkirmishLibraryLaunchContext.Difficulty = difficulty);

        private static SkirmishTheaterPinState Pin(SkirmishLibraryViewModel model, string mapId)
        {
            for (int i = 0; i < model.Theater.Pins.Length; i++)
            {
                if (model.Theater.Pins[i].MapId == mapId)
                    return model.Theater.Pins[i];
            }

            Assert.Fail(mapId);
            return default;
        }

        private static SkirmishObjectiveTabState Tab(SkirmishLibraryViewModel model, string objectiveId)
        {
            for (int i = 0; i < model.Battlefield.Tabs.Length; i++)
            {
                if (model.Battlefield.Tabs[i].ObjectiveId == objectiveId)
                    return model.Battlefield.Tabs[i];
            }

            Assert.Fail(objectiveId);
            return default;
        }

        private static SkirmishBattleCardState Card(SkirmishBattleCardState[] cards, string pinId)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].PinId == pinId)
                    return cards[i];
            }

            Assert.Fail(pinId);
            return default;
        }

        private static SkirmishBattleCatalogEntry Entry(
            string id, string map, string objective, string army, string start, bool playable, int index) =>
            new()
            {
                ScenarioId = id,
                MapId = map,
                ObjectiveId = objective,
                ArmyProfile = army,
                StartProfile = start,
                TitleEnglish = id,
                Status = playable ? SkirmishBattleCatalogStatus.Playable : SkirmishBattleCatalogStatus.Planned,
                PlayableScenarioIndex = playable ? index : -1
            };

        private static SkirmishResultReceipt Victory(string catalogId, SkirmishDifficultyId difficulty) =>
            new()
            {
                CatalogId = catalogId,
                SessionId = catalogId + "-" + difficulty,
                DefinitionId = "skirmish." + catalogId.ToLowerInvariant(),
                DifficultyId = difficulty,
                Outcome = SkirmishOutcomeKind.Victory,
                Reason = SkirmishEndReasonKind.MainBaseDestroyed,
                ContentVersion = 1
            };

        private sealed class MemoryPrefs : ISkirmishLibraryPrefs
        {
            private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
            public string Get(string key, string fallback) => _values.TryGetValue(key, out string value) ? value : fallback;
            public void Set(string key, string value) => _values[key] = value ?? string.Empty;
            public void Save() { }
        }

        private sealed class OwnsNothing : IVerifiedContentEntitlements
        {
            public bool Owns(string productId) => false;
        }
    }
}
