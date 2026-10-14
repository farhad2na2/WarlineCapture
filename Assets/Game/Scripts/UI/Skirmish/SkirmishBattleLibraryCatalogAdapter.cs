using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Skirmish.Contracts;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Temporary WP2 seam. Projects the battle catalog plus the medal read model and access evaluator.
    /// Recommended Next is left empty on purpose. Replace this type when Programmer 1 lands the projection.
    /// </summary>
    public sealed class SkirmishBattleLibraryCatalogAdapter : ISkirmishBattleLibraryProjection
    {
        private readonly IReadOnlyList<SkirmishBattleCatalogEntry> entries;
        private readonly ISkirmishMedalReadModel medals;
        private readonly SkirmishLibraryAccessEvaluator access;

        public SkirmishBattleLibraryCatalogAdapter(
            IReadOnlyList<SkirmishBattleCatalogEntry> entries,
            ISkirmishMedalReadModel medals,
            SkirmishLibraryAccessEvaluator access)
        {
            this.entries = entries ?? Array.Empty<SkirmishBattleCatalogEntry>();
            this.medals = medals ?? SkirmishMedalReadModelEmpty.Instance;
            this.access = access ?? SkirmishBattleLibraryServices.DefaultAccess;
        }

        public SkirmishLibrarySnapshot Project(in SkirmishLibraryQuery query)
        {
            var snapshot = new SkirmishLibrarySnapshot();
            medals.TotalsByDifficulty(
                out snapshot.MedalRecruit,
                out snapshot.MedalRegular,
                out snapshot.MedalVeteran,
                out snapshot.MedalCommander);

            var battlefields = new SkirmishLibraryBattlefieldModel[SkirmishBattleLibraryArt.MapIds.Length];
            for (int i = 0; i < SkirmishBattleLibraryArt.MapIds.Length; i++)
            {
                string mapId = SkirmishBattleLibraryArt.MapIds[i];
                SkirmishBattleLibraryArt.Copy(mapId, query.RightToLeft, out string title, out string geography);
                battlefields[i] = new SkirmishLibraryBattlefieldModel
                {
                    MapId = mapId,
                    Title = title,
                    Geography = geography,
                    Won = medals.BattlesWon(mapId),
                    Published = CountPublished(mapId, null),
                    MedalRecruit = CountMedals(mapId, null, SkirmishDifficultyId.Recruit),
                    MedalRegular = CountMedals(mapId, null, SkirmishDifficultyId.Regular),
                    MedalVeteran = CountMedals(mapId, null, SkirmishDifficultyId.Veteran),
                    MedalCommander = CountMedals(mapId, null, SkirmishDifficultyId.Commander)
                };
                snapshot.TheaterWon += battlefields[i].Won;
                snapshot.TheaterPublished += battlefields[i].Published;
            }

            snapshot.Battlefields = battlefields;
            snapshot.Objectives = ProjectObjectives(query);
            snapshot.Cards = ProjectCards(query, out string focusId);
            snapshot.FocusCatalogId = focusId ?? string.Empty;
            snapshot.RecommendedCatalogId = string.Empty;
            snapshot.RecommendedTitle = string.Empty;

            for (int i = 0; i < snapshot.Cards.Length; i++)
            {
                if (!string.Equals(snapshot.Cards[i].CatalogId, query.SelectedCatalogId, StringComparison.Ordinal))
                    continue;
                snapshot.Selected = snapshot.Cards[i];
                snapshot.HasSelection = true;
                break;
            }

            if (!snapshot.HasSelection)
            {
                for (int i = 0; i < snapshot.Cards.Length; i++)
                {
                    if (snapshot.Cards[i].Access != SkirmishLibraryCardAccess.OwnedPlayable)
                        continue;
                    snapshot.Selected = snapshot.Cards[i];
                    snapshot.HasSelection = true;
                    break;
                }
            }

            return snapshot;
        }

        private SkirmishLibraryObjectiveModel[] ProjectObjectives(in SkirmishLibraryQuery query)
        {
            var objectives = new SkirmishLibraryObjectiveModel[SkirmishBattleLibraryArt.ObjectiveIds.Length];
            for (int i = 0; i < SkirmishBattleLibraryArt.ObjectiveIds.Length; i++)
            {
                string objectiveId = SkirmishBattleLibraryArt.ObjectiveIds[i];
                SkirmishBattleLibraryArt.ObjectiveCopy(objectiveId, query.RightToLeft, out string title, out string shortLabel);
                objectives[i] = new SkirmishLibraryObjectiveModel
                {
                    ObjectiveId = objectiveId,
                    Title = title,
                    ShortLabel = shortLabel,
                    Won = medals.BattlesWon(query.MapId, objectiveId),
                    Published = CountPublished(query.MapId, objectiveId)
                };
            }

            return objectives;
        }

        private SkirmishLibraryCardModel[] ProjectCards(in SkirmishLibraryQuery query, out string focusId)
        {
            focusId = string.Empty;
            var cards = new List<SkirmishLibraryCardModel>(24);
            string search = (query.Search ?? string.Empty).Trim();
            for (int i = 0; i < entries.Count; i++)
            {
                SkirmishBattleCatalogEntry entry = entries[i];
                if (string.IsNullOrEmpty(entry.ScenarioId))
                    continue;
                if (!string.IsNullOrEmpty(query.MapId) &&
                    !string.Equals(entry.MapId, query.MapId, StringComparison.OrdinalIgnoreCase))
                    continue;

                bool playable = entry.IsPlayable;
                SkirmishLibraryCardAccess cardAccess = playable
                    ? access(entry.ScenarioId, true)
                    : SkirmishLibraryCardAccess.Planned;
                if (cardAccess == SkirmishLibraryCardAccess.Planned && !query.DevelopmentBuild)
                    continue;

                var card = BuildCard(entry, cardAccess, query.RightToLeft);
                bool searchHit = string.IsNullOrEmpty(search) || Matches(entry, card, search);
                if (!string.IsNullOrEmpty(search) && searchHit && string.IsNullOrEmpty(focusId))
                    focusId = card.CatalogId;
                if (!searchHit)
                    continue;
                if (!string.IsNullOrEmpty(query.ObjectiveId) &&
                    !string.Equals(card.ObjectiveId, query.ObjectiveId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(query.ArmyFilter) &&
                    !string.Equals(card.ArmyProfile, query.ArmyFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(query.StartFilter) &&
                    !string.Equals(card.StartProfile, query.StartFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (query.MissingMedalOnly && card.HasMedal(query.MedalDifficulty))
                    continue;
                cards.Add(card);
            }

            return cards.ToArray();
        }

        private SkirmishLibraryCardModel BuildCard(SkirmishBattleCatalogEntry entry, SkirmishLibraryCardAccess cardAccess, bool rightToLeft)
        {
            SkirmishBattleLibraryArt.ProfileCopy(entry.ArmyProfile, rightToLeft, out string armyLabel);
            SkirmishBattleLibraryArt.ProfileCopy(entry.StartProfile, rightToLeft, out string startLabel);
            string purpose = rightToLeft && !string.IsNullOrEmpty(entry.DescriptionFarsi)
                ? entry.DescriptionFarsi
                : entry.DescriptionEnglish;
            if (string.IsNullOrEmpty(purpose))
                purpose = entry.TitleEnglish ?? entry.ScenarioId;
            return new SkirmishLibraryCardModel
            {
                CatalogId = entry.ScenarioId,
                MapId = entry.MapId ?? string.Empty,
                ObjectiveId = entry.ObjectiveId ?? string.Empty,
                ArmyProfile = entry.ArmyProfile ?? string.Empty,
                StartProfile = entry.StartProfile ?? string.Empty,
                Title = string.IsNullOrEmpty(entry.TitleEnglish) ? entry.ScenarioId : entry.TitleEnglish,
                Purpose = purpose,
                ArmyLabel = armyLabel,
                StartLabel = startLabel,
                PreviewKey = SkirmishBattleLibraryArt.PreviewKey(entry.ScenarioId, entry.MapId),
                MedalRecruit = medals.Has(entry.ScenarioId, SkirmishDifficultyId.Recruit),
                MedalRegular = medals.Has(entry.ScenarioId, SkirmishDifficultyId.Regular),
                MedalVeteran = medals.Has(entry.ScenarioId, SkirmishDifficultyId.Veteran),
                MedalCommander = medals.Has(entry.ScenarioId, SkirmishDifficultyId.Commander),
                Access = cardAccess
            };
        }

        private int CountPublished(string mapId, string objectiveId)
        {
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                SkirmishBattleCatalogEntry entry = entries[i];
                if (!entry.IsPlayable)
                    continue;
                if (!string.IsNullOrEmpty(mapId) && !string.Equals(entry.MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(objectiveId) && !string.Equals(entry.ObjectiveId, objectiveId, StringComparison.OrdinalIgnoreCase))
                    continue;
                count++;
            }

            return count;
        }

        private int CountMedals(string mapId, string objectiveId, SkirmishDifficultyId difficulty)
        {
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                SkirmishBattleCatalogEntry entry = entries[i];
                if (!string.IsNullOrEmpty(mapId) && !string.Equals(entry.MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(objectiveId) && !string.Equals(entry.ObjectiveId, objectiveId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (medals.Has(entry.ScenarioId, difficulty))
                    count++;
            }

            return count;
        }

        private static bool Matches(SkirmishBattleCatalogEntry entry, SkirmishLibraryCardModel card, string search)
        {
            return Contains(entry.ScenarioId, search)
                || Contains(entry.TitleEnglish, search)
                || Contains(entry.MapId, search)
                || Contains(entry.ObjectiveId, search)
                || Contains(entry.ArmyProfile, search)
                || Contains(entry.StartProfile, search)
                || Contains(card.Title, search)
                || Contains(card.ArmyLabel, search)
                || Contains(card.StartLabel, search);
        }

        private static bool Contains(string value, string search)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
