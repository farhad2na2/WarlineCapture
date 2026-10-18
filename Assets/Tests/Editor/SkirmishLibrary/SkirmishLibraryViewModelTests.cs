using System;
using System.Collections.Generic;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using Unity.Entities;

namespace Game.Tests.Editor
{
    public sealed class SkirmishLibraryViewModelTests
    {
        [TearDown]
        public void TearDown() =>
            SkirmishLibraryLaunchContext.Difficulty = SkirmishDifficultyId.Regular;

        [Test]
        public void RecommendedNextUsesTheBattlefieldWithFewestWinsThenCatalogOrder()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0),
                Entry("S002", "DB", "BA", "G", "E", true, 2),
                Entry("S025", "CC", "BA", "G", "F", true, 1),
                Entry("S026", "CC", "BA", "G", "E", true, 4)
            };
            var ledger = new SkirmishMedalLedger(null, entries);
            Assert.IsTrue(ledger.TryAward(Victory("S001", SkirmishDifficultyId.Recruit), out _));
            var tied = Model(entries, ledger);
            Assert.AreEqual("S025", tied.Theater.RecommendedScenarioId);
            Assert.IsTrue(Pin(tied, "CC").IsRecommended);
            Assert.IsFalse(Pin(tied, "DB").IsRecommended);

            var empty = Model(entries, new SkirmishMedalLedger(null, entries));
            Assert.AreEqual("S001", empty.Theater.RecommendedScenarioId);

            Assert.IsTrue(ledger.TryAward(Victory("S001", SkirmishDifficultyId.Regular), out _));
            Assert.IsTrue(ledger.TryAward(Victory("S025", SkirmishDifficultyId.Regular), out _));
            Assert.IsTrue(ledger.TryAward(Victory("S026", SkirmishDifficultyId.Regular), out _));
            Assert.AreEqual("S002", Model(entries, ledger).Theater.RecommendedScenarioId);
        }

        [Test]
        public void RecommendedNextSkipsNotOwnedUntilNothingOwnedRemains()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0),
                Entry("S002", "DB", "BA", "G", "E", true, 2)
            };
            var ledger = new SkirmishMedalLedger(null, entries);
            Assert.IsTrue(ledger.TryAward(Victory("S001", SkirmishDifficultyId.Regular), out _));
            var ownedSample = Model(entries, ledger, id =>
                id == "skirmish.s001" ? ContentAccessState.Allowed : ContentAccessState.NotOwned);
            Assert.AreEqual(string.Empty, ownedSample.Theater.RecommendedScenarioId);

            var noneOwned = Model(entries, new SkirmishMedalLedger(null, entries), _ => ContentAccessState.NotOwned);
            Assert.AreEqual("S001", noneOwned.Theater.RecommendedScenarioId);
        }

        [Test]
        public void QuickBattleSkipsTheSelectedScenarioWhenAnotherIsPlayable()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0),
                Entry("S002", "DB", "BA", "G", "E", true, 2)
            };
            var model = Model(entries, new SkirmishMedalLedger(null, entries), randomIndex: _ => 0);
            string first = model.QuickBattle();
            string second = model.QuickBattle();
            string third = model.QuickBattle();
            Assert.IsNotEmpty(first);
            Assert.AreNotEqual(first, second);
            Assert.AreNotEqual(second, third);
        }

        [Test]
        public void QuickBattleSelectsAnOwnedScenarioAndDoesNotLaunch()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0),
                Entry("S002", "DB", "BA", "G", "E", true, 2)
            };
            SkirmishLibraryLaunchContext.Difficulty = SkirmishDifficultyId.Regular;
            var model = Model(entries, new SkirmishMedalLedger(null, entries), randomIndex: _ => 1);
            Assert.AreEqual("S002", model.QuickBattle());
            Assert.AreEqual(SkirmishLibraryLevel.Battlefield, model.Level);
            Assert.AreEqual("S002", model.Briefing.ScenarioId);
            Assert.IsTrue(model.Briefing.CanLaunch);
            Assert.AreEqual(SkirmishDifficultyId.Regular, SkirmishLibraryLaunchContext.Difficulty);
        }

        [Test]
        public void ReleaseHidesPlannedOnlyBattlefieldsAndKeepsPublishedCounts()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0),
                Entry("S006", "DB", "BA", "C", "E", false, -1),
                Entry("S097", "AP", "BA", "G", "F", false, -1)
            };
            var release = Model(entries, new SkirmishMedalLedger(null, entries), showPlanned: false);
            Assert.IsNull(FindPin(release, "AP"));
            Assert.NotNull(FindPin(release, "DB"));
            Assert.AreEqual(1, Pin(release, "DB").PublishedCount);
            Assert.IsFalse(release.SelectMapCollection("AP"));
            SkirmishBattleCardState plannedSlot = Card(release.Battlefield.GridCards, "DB.BA.5");
            Assert.AreEqual("S006", plannedSlot.ScenarioId);
            Assert.AreEqual(SkirmishCardAccess.Planned, plannedSlot.Access);

            var development = Model(entries, new SkirmishMedalLedger(null, entries), showPlanned: true);
            Assert.NotNull(FindPin(development, "AP"));
            Assert.AreEqual(0, Pin(development, "AP").PublishedCount);
            Assert.IsTrue(development.SelectMapCollection("AP"));
            Assert.AreEqual(SkirmishCardAccess.Planned, development.Battlefield.MapCards[0].Access);
            Assert.IsFalse(development.Briefing.CanLaunch);
        }

        [Test]
        public void GridIsRowMajorAndMapShowsTheSelectedObjective()
        {
            var entries = new List<SkirmishBattleCatalogEntry>();
            string[] objectives = { "BA", "FC", "BT", "CE" };
            string[] armies = { "G", "G", "A", "A", "C", "C" };
            string[] starts = { "F", "E", "F", "E", "F", "E" };
            var expected = new string[24];
            int id = 1;
            for (int row = 0; row < objectives.Length; row++)
            {
                for (int column = 0; column < armies.Length; column++)
                {
                    string scenario = "S" + id.ToString("000");
                    expected[(row * 6) + column] = scenario;
                    entries.Add(Entry(scenario, "DB", objectives[row], armies[column], starts[column], true, id - 1));
                    id++;
                }
            }

            entries.Reverse();
            var model = Model(entries, new SkirmishMedalLedger(null, entries));
            Assert.IsTrue(model.SelectMapCollection("DB"));
            Assert.AreEqual(24, model.Battlefield.GridCards.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], model.Battlefield.GridCards[i].ScenarioId, "slot " + i);
                Assert.AreEqual("DB." + model.Battlefield.GridCards[i].ObjectiveId + "." + (i % 6), model.Battlefield.GridCards[i].PinId);
            }

            Assert.IsTrue(model.SelectObjective("FC"));
            Assert.AreEqual(6, model.Battlefield.MapCards.Length);
            Assert.AreEqual("S007", model.Battlefield.MapCards[0].ScenarioId);
            Assert.AreEqual("S012", model.Battlefield.MapCards[5].ScenarioId);
            Assert.AreEqual(SkirmishBattlefieldLayout.Map, model.Battlefield.Layout);
            model.SetLayout(SkirmishBattlefieldLayout.Grid);
            Assert.AreEqual(SkirmishBattlefieldLayout.Grid, model.Battlefield.Layout);
        }

        [Test]
        public void NotOwnedCardsKeepTheFullBriefingAndStoreRoute()
        {
            var model = Model(
                new List<SkirmishBattleCatalogEntry> { Entry("S002", "DB", "BA", "G", "E", true, 2) },
                new SkirmishMedalLedger(null, null),
                _ => ContentAccessState.NotOwned);
            Assert.IsTrue(model.SelectScenarioId("S002"));
            Assert.AreEqual(SkirmishCardAccess.NotOwned, model.Briefing.Access);
            Assert.AreEqual("ui.skirmish.s002.description", model.Briefing.DescriptionLocKey);
            Assert.AreEqual("ui.skirmish.s002.purpose", model.Briefing.PurposeLocKey);
            Assert.AreEqual("ui.skirmish.army.g", model.Briefing.ArmyLocKey);
            Assert.AreEqual("ui.skirmish.start.e", model.Briefing.StartLocKey);
            Assert.AreEqual("ui.skirmish.locked.not_owned", model.Briefing.LockedReasonLocKey);
            Assert.AreEqual(ContentAccessAuthority.CampaignProduct, model.Briefing.StoreRouteProductId);
            Assert.AreEqual(4, model.Briefing.AvailableDifficulties.Length);
            Assert.IsFalse(model.Briefing.CanLaunch);
            Assert.IsFalse(model.Launch());
        }

        [Test]
        public void SearchAndMedalFiltersJumpToTheMatchingBattlefield()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0, "Desert Gate"),
                Entry("S002", "DB", "BA", "G", "E", true, 2, "Desert Hold"),
                Entry("S025", "CC", "FC", "A", "F", true, 1, "Crossroads")
            };
            var ledger = new SkirmishMedalLedger(null, entries);
            Assert.IsTrue(ledger.TryAward(Victory("S001", SkirmishDifficultyId.Regular), out _));
            var model = Model(entries, ledger);
            model.ApplyFilter(new SkirmishLibraryFilter { SearchText = "Crossroads" });
            Assert.AreEqual(SkirmishLibraryLevel.Battlefield, model.Level);
            Assert.AreEqual("CC", model.Battlefield.MapId);
            Assert.AreEqual("FC", model.Battlefield.SelectedObjectiveId);
            Assert.AreEqual("S025", model.Briefing.ScenarioId);

            model.ApplyFilter(new SkirmishLibraryFilter
            {
                ObjectiveId = "BA",
                ArmyProfile = "G",
                StartProfile = "E",
                MedalMissing = SkirmishDifficultyId.Regular
            });
            Assert.AreEqual("S002", model.Briefing.ScenarioId);
            Assert.AreEqual("DB", model.Battlefield.MapId);
        }

        [Test]
        public void RestoreFromPrefsReturnsToTheBattlefieldCardAndDifficulty()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S002", "DB", "BA", "G", "E", true, 2)
            };
            var prefs = new MemoryPrefs();
            var ledger = new SkirmishMedalLedger(null, entries);
            var first = Model(entries, ledger, prefs: prefs);
            Assert.IsTrue(first.SelectScenarioId("S002"));
            first.SelectDifficulty(SkirmishDifficultyId.Veteran);
            first.SetLayout(SkirmishBattlefieldLayout.Grid);
            Assert.IsTrue(ledger.TryAward(Victory("S002", SkirmishDifficultyId.Veteran), out _));

            var restored = Model(entries, ledger, prefs: prefs);
            restored.RestoreFromPrefs();
            Assert.AreEqual(SkirmishLibraryLevel.Battlefield, restored.Level);
            Assert.AreEqual("DB", restored.Battlefield.MapId);
            Assert.AreEqual("BA", restored.Battlefield.SelectedObjectiveId);
            Assert.AreEqual(SkirmishBattlefieldLayout.Grid, restored.Battlefield.Layout);
            Assert.AreEqual("S002", restored.Briefing.ScenarioId);
            Assert.AreEqual(SkirmishDifficultyId.Veteran, restored.Briefing.SelectedDifficulty);
            Assert.AreEqual(SkirmishMedalMask.Veteran, restored.Briefing.Medals);
            Assert.AreEqual(SkirmishMedalMask.Veteran, restored.Battlefield.MapCards[1].Medals);
            Assert.AreEqual(1, restored.Theater.Pins[0].WonCount);
            Assert.AreEqual(1, restored.Theater.Pins[0].Medals.Veteran);
        }

        [Test]
        public void FreshProfileWithOnlyS001OwnedCannotLaunchTheRest()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0),
                Entry("S002", "DB", "BA", "G", "E", true, 2),
                Entry("S025", "CC", "BA", "G", "F", true, 1),
                Entry("S073", "IB", "BA", "G", "F", true, 3)
            };
            using (ContentAccessRuntime.UseValidationProvider(new OwnsNothing()))
            using (var world = new World(nameof(FreshProfileWithOnlyS001OwnedCannotLaunchTheRest)))
            {
                var model = Model(entries, new SkirmishMedalLedger(null, entries), id => ContentAccessRuntime.Evaluate(id));
                Assert.IsTrue(model.SelectScenarioId("S001"));
                Assert.AreEqual(SkirmishCardAccess.Playable, model.Briefing.Access);
                Assert.IsTrue(model.Launch());

                foreach (string locked in new[] { "S002", "S025", "S073" })
                {
                    Assert.IsTrue(model.SelectScenarioId(locked), locked);
                    Assert.AreEqual(SkirmishCardAccess.NotOwned, model.Briefing.Access, locked);
                    Assert.IsFalse(string.IsNullOrEmpty(model.Briefing.DescriptionLocKey), locked);
                    Assert.AreEqual(ContentAccessAuthority.CampaignProduct, model.Briefing.StoreRouteProductId, locked);
                    Assert.IsFalse(model.Launch(), locked);
                    Assert.IsFalse(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                        world.EntityManager, locked, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, 1,
                        null, null, null, out _, out _, out List<SkirmishCompileReason> denied));
                    Assert.AreEqual("contentAccess", denied[0].Field, locked);
                }

                Assert.IsFalse(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                    world.EntityManager, "S001", SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, 1,
                    null, null, null, out _, out _, out List<SkirmishCompileReason> sample));
                Assert.IsFalse(sample.Exists(reason => reason.Field == "contentAccess"));
            }
        }

        [Test]
        public void NormalInputJourneyKeepsTheMedalOnTheCardGridAndTheater()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0),
                Entry("S002", "DB", "BA", "G", "E", true, 2),
                Entry("S007", "DB", "FC", "G", "F", true, 5)
            };
            var prefs = new MemoryPrefs();
            var ledger = new SkirmishMedalLedger(null, entries);
            var model = Model(entries, ledger, prefs: prefs);
            Assert.AreEqual(SkirmishLibraryLevel.Theater, model.Level);
            model.ZoomIn();
            Assert.AreEqual(SkirmishLibraryLevel.Battlefield, model.Level);
            Assert.IsTrue(model.SelectObjective("BA"));
            Assert.IsTrue(model.SelectScenarioId("S001"));
            model.SelectDifficulty(SkirmishDifficultyId.Recruit);
            Assert.IsTrue(model.Briefing.CanLaunch);
            Assert.IsTrue(model.Launch());
            Assert.AreEqual(SkirmishDifficultyId.Recruit, SkirmishLibraryLaunchContext.Difficulty);
            Assert.IsTrue(ledger.TryAward(Victory("S001", SkirmishDifficultyId.Recruit), out SkirmishMedalAwardResult award));
            Assert.IsTrue(award.IsFirstAward);

            Assert.AreEqual(SkirmishMedalMask.Recruit, model.Battlefield.MapCards[0].Medals);
            Assert.AreEqual(SkirmishMedalMask.Recruit, model.Battlefield.GridCards[0].Medals);
            Assert.AreEqual(1, model.Theater.Pins[0].WonCount);
            Assert.AreEqual(1, model.Theater.Pins[0].Medals.Recruit);
            Assert.AreEqual(1, model.Battlefield.Tabs[0].WonCount);

            var returned = Model(entries, ledger, prefs: prefs);
            returned.RestoreFromPrefs();
            Assert.AreEqual(SkirmishLibraryLevel.Battlefield, returned.Level);
            Assert.AreEqual("DB", returned.Battlefield.MapId);
            Assert.AreEqual("S001", returned.Briefing.ScenarioId);
            Assert.AreEqual(SkirmishMedalMask.Recruit, returned.Briefing.Medals);
            Assert.AreEqual(SkirmishMedalMask.Recruit, returned.Battlefield.GridCards[0].Medals);
            Assert.AreEqual(1, returned.Theater.Pins[0].Medals.Recruit);
            Assert.IsTrue(model.SelectMapCollection(string.Empty));
            Assert.AreEqual(SkirmishLibraryLevel.Theater, model.Level);
        }

        [Test]
        public void PlayableBriefingWithoutCompiledDefinitionUsesLegacyClockAndLeavesSizeBlank()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0)
            };
            var model = Model(entries, new SkirmishMedalLedger(null, entries));
            Assert.IsTrue(model.SelectScenarioId("S001"));
            SkirmishBriefingState briefing = model.Briefing;
            Assert.AreEqual("ui.skirmish.objective.ba", briefing.ObjectiveLocKey);
            Assert.AreEqual(900, briefing.DeadlineSeconds);
            Assert.IsTrue(briefing.ShowTime);
            Assert.IsFalse(briefing.ShowSize);
            Assert.AreEqual(string.Empty, briefing.SizeLocKey);
            Assert.AreEqual(SkirmishSizeId.None, briefing.RecommendedSize);
            Assert.AreEqual(0, briefing.StartingStructureCount);
            Assert.AreEqual(0f, briefing.MapWidthMetres);
            Assert.AreEqual(0f, briefing.MapDepthMetres);

            string previewAsset = SkirmishLibraryPreviewAssets.PreviewPath("S001");
            string plateAsset = SkirmishLibraryPreviewAssets.PlatePath("DB");
            Assert.AreEqual(
                SkirmishLibraryPreviewAssets.HasAsset(previewAsset) ? previewAsset : "skirmish.preview.S001",
                Card(model.Battlefield.MapCards, "DB.BA.0").PreviewSpriteKey);
            Assert.AreEqual(
                SkirmishLibraryPreviewAssets.HasAsset(plateAsset) ? plateAsset : "skirmish.plate.DB",
                Pin(model, "DB").PlatePreviewKey);
        }

        [Test]
        public void CompiledBriefingSourceSuppliesDeadlineSizeStructuresAndMapExtent()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S001", "DB", "BA", "G", "F", true, 0)
            };
            var source = new SkirmishLibraryBriefingSource
            {
                Found = true,
                DeadlineSeconds = 1080,
                RecommendedSize = SkirmishSizeId.Standard,
                StartingStructureCount = 7,
                MapWidthMetres = 600f,
                MapDepthMetres = 420f
            };
            var model = Model(entries, new SkirmishMedalLedger(null, entries), briefingSource: _ => source);
            Assert.IsTrue(model.SelectScenarioId("S001"));
            SkirmishBriefingState briefing = model.Briefing;
            Assert.AreEqual(1080, briefing.DeadlineSeconds);
            Assert.IsTrue(briefing.ShowTime);
            Assert.IsTrue(briefing.ShowSize);
            Assert.AreEqual("ui.skirmish.size.standard", briefing.SizeLocKey);
            Assert.AreEqual(SkirmishSizeId.Standard, briefing.RecommendedSize);
            Assert.AreEqual(7, briefing.StartingStructureCount);
            Assert.AreEqual(600f, briefing.MapWidthMetres);
            Assert.AreEqual(420f, briefing.MapDepthMetres);
            Assert.AreNotEqual(900, briefing.DeadlineSeconds);
        }

        [Test]
        public void PlannedBriefingWithoutCompiledDefinitionDoesNotInventTimeOrSize()
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S005", "DB", "BA", "G", "E", false, -1)
            };
            var model = Model(entries, new SkirmishMedalLedger(null, entries), showPlanned: true);
            Assert.IsTrue(model.SelectScenarioId("S005"));
            SkirmishBriefingState briefing = model.Briefing;
            Assert.AreEqual(0, briefing.DeadlineSeconds);
            Assert.IsFalse(briefing.ShowTime);
            Assert.IsFalse(briefing.ShowSize);
            Assert.AreEqual(string.Empty, briefing.SizeLocKey);
            Assert.AreEqual(SkirmishSizeId.None, briefing.RecommendedSize);
            Assert.AreEqual(0, briefing.StartingStructureCount);
            Assert.AreEqual(0f, briefing.MapWidthMetres);
            Assert.AreEqual(0f, briefing.MapDepthMetres);
            string plateAsset = SkirmishLibraryPreviewAssets.PlatePath("DB");
            Assert.AreEqual(
                SkirmishLibraryPreviewAssets.HasAsset(plateAsset) ? plateAsset : "skirmish.plate.DB",
                Card(model.Battlefield.MapCards, "DB.BA.1").PreviewSpriteKey);
        }

        [Test]
        public void SizeLocKeyNamesWarAndLargeWarWithoutUsingStructureCount()
        {
            AssertSizeName(SkirmishSizeId.War, "ui.skirmish.size.war");
            AssertSizeName(SkirmishSizeId.LargeWar, "ui.skirmish.size.large_war");
        }

        private static void AssertSizeName(SkirmishSizeId size, string key)
        {
            var entries = new List<SkirmishBattleCatalogEntry>
            {
                Entry("S002", "DB", "BA", "G", "E", true, 4)
            };
            var source = new SkirmishLibraryBriefingSource
            {
                Found = true,
                DeadlineSeconds = 1500,
                RecommendedSize = size,
                StartingStructureCount = 10,
                MapWidthMetres = 600f,
                MapDepthMetres = 420f
            };
            var model = Model(entries, new SkirmishMedalLedger(null, entries), briefingSource: _ => source);
            Assert.IsTrue(model.SelectScenarioId("S002"));
            Assert.IsTrue(model.Briefing.ShowTime);
            Assert.IsTrue(model.Briefing.ShowSize);
            Assert.AreEqual(key, model.Briefing.SizeLocKey);
            Assert.AreEqual(10, model.Briefing.StartingStructureCount);
            Assert.AreEqual(size, model.Briefing.RecommendedSize);
        }

        private static SkirmishLibraryViewModel Model(
            List<SkirmishBattleCatalogEntry> entries,
            SkirmishMedalLedger ledger,
            Func<string, ContentAccessState> access = null,
            Func<int, int> randomIndex = null,
            bool showPlanned = false,
            ISkirmishLibraryPrefs prefs = null,
            Func<string, SkirmishLibraryBriefingSource> briefingSource = null) =>
            new(
                entries,
                new FakeSkirmishMedalReader(ledger),
                access ?? (_ => ContentAccessState.Allowed),
                randomIndex,
                showPlanned,
                prefs ?? new MemoryPrefs(),
                difficulty => SkirmishLibraryLaunchContext.Difficulty = difficulty,
                briefingSource);

        private static SkirmishTheaterPinState Pin(SkirmishLibraryViewModel model, string mapId)
        {
            SkirmishTheaterPinState? pin = FindPin(model, mapId);
            Assert.IsTrue(pin.HasValue, mapId);
            return pin.Value;
        }

        private static SkirmishTheaterPinState? FindPin(SkirmishLibraryViewModel model, string mapId)
        {
            for (int i = 0; i < model.Theater.Pins.Length; i++)
            {
                if (model.Theater.Pins[i].MapId == mapId)
                    return model.Theater.Pins[i];
            }

            return null;
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
            string id, string map, string objective, string army, string start, bool playable, int index, string title = null) =>
            new()
            {
                ScenarioId = id,
                MapId = map,
                ObjectiveId = objective,
                ArmyProfile = army,
                StartProfile = start,
                TitleEnglish = title ?? id,
                DescriptionEnglish = (title ?? id) + " briefing",
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

    internal sealed class FakeSkirmishMedalReader : ISkirmishMedalReader
    {
        private readonly SkirmishMedalLedger _ledger;

        public FakeSkirmishMedalReader(SkirmishMedalLedger ledger) => _ledger = ledger;

        public bool Has(string catalogId, SkirmishDifficultyId difficulty) =>
            _ledger.Has(catalogId, difficulty);

        public int CountFor(string mapId) => _ledger.CountFor(mapId);

        public int CountFor(string mapId, string objectiveId) =>
            _ledger.CountFor(mapId, objectiveId);

        public int CountTier(SkirmishDifficultyId difficulty)
        {
            SkirmishMedalDifficultyTotals totals = _ledger.TotalsByDifficulty();
            switch (difficulty)
            {
                case SkirmishDifficultyId.Recruit: return totals.Recruit;
                case SkirmishDifficultyId.Regular: return totals.Regular;
                case SkirmishDifficultyId.Veteran: return totals.Veteran;
                case SkirmishDifficultyId.Commander: return totals.Commander;
                default: return 0;
            }
        }

        public void Refresh() { }
    }
}
