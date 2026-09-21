using System;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    public readonly struct SkirmishLibraryBriefing
    {
        public readonly string CatalogId;
        public readonly string TitleKey;
        public readonly string BriefKey;
        public readonly string ObjectiveKey;
        public readonly string Title;
        public readonly string Brief;
        public readonly string Objective;
        public readonly bool Playable;

        public SkirmishLibraryBriefing(
            string catalogId,
            string titleKey,
            string briefKey,
            string objectiveKey,
            string title,
            string brief,
            string objective,
            bool playable)
        {
            CatalogId = catalogId;
            TitleKey = titleKey;
            BriefKey = briefKey;
            ObjectiveKey = objectiveKey;
            Title = title;
            Brief = brief;
            Objective = objective;
            Playable = playable;
        }
    }

    public readonly struct SkirmishHudCopy
    {
        public readonly string ObjectiveKey;
        public readonly string ResultKey;
        public readonly string Objective;
        public readonly string Result;

        public SkirmishHudCopy(string objectiveKey, string resultKey, string objective, string result)
        {
            ObjectiveKey = objectiveKey;
            ResultKey = resultKey;
            Objective = objective;
            Result = result;
        }
    }

    public static class SkirmishExpandedCopyProjection
    {
        public const string S002CatalogId = "S002";
        public const string TitleKey = "skirmish.s002.title";
        public const string BriefKey = "skirmish.s002.brief";
        public const string ObjectiveKey = "skirmish.s002.objective";
        public const string WarningOffensiveAirKey = "skirmish.s002.warning.offensive_air";
        public const string WarningReplacementBaseKey = "skirmish.s002.warning.replacement_base";
        public const string ResultVictoryKey = "skirmish.s002.result.victory";
        public const string ResultDefeatKey = "skirmish.s002.result.defeat";
        public const string ResultDrawBasesKey = "skirmish.s002.result.draw_bases";
        public const string ResultDrawDeadlineKey = "skirmish.s002.result.draw_deadline";
        public const string ResultSurrenderKey = "skirmish.s002.result.surrender";

        public static readonly string[] RequiredS002Keys =
        {
            TitleKey,
            BriefKey,
            ObjectiveKey,
            WarningOffensiveAirKey,
            WarningReplacementBaseKey,
            ResultVictoryKey,
            ResultDefeatKey,
            ResultDrawBasesKey,
            ResultDrawDeadlineKey,
            ResultSurrenderKey
        };

        public static bool IsExpandedS002(string catalogId) =>
            string.Equals(catalogId, S002CatalogId, StringComparison.Ordinal);

        public static bool TryResolveS002LibraryBriefing(string locale, out SkirmishLibraryBriefing briefing)
        {
            briefing = new SkirmishLibraryBriefing(
                S002CatalogId,
                TitleKey,
                BriefKey,
                ObjectiveKey,
                Resolve(TitleKey, locale, "Desert Base · Base Assault · Established"),
                Resolve(BriefKey, locale, "Use the starting tank and APC to contest the highway. Keep one infantry squad at home. Press now or scout the south sweep before siege."),
                Resolve(ObjectiveKey, locale, "Destroy the original enemy Barracks while your original Barracks survives."),
                playable: false);
            return true;
        }

        public static bool TryResolveLibraryBriefing(
            SkirmishScenarioDefinitionConfig definition,
            SkirmishDifficultyId difficulty,
            SkirmishSizeId size,
            string locale,
            out SkirmishLibraryBriefing briefing)
        {
            briefing = default;
            if (definition == null || !IsExpandedS002(definition.CatalogId))
                return false;
            if (difficulty != SkirmishDifficultyId.Regular || size != SkirmishSizeId.Standard)
                return false;

            briefing = new SkirmishLibraryBriefing(
                definition.CatalogId,
                definition.TitleKey,
                definition.BriefingKey,
                definition.ObjectiveKey,
                Resolve(definition.TitleKey, locale, "Desert Base · Base Assault · Established"),
                Resolve(definition.BriefingKey, locale, "Use the starting tank and APC to contest the highway. Keep one infantry squad at home. Press now or scout the south sweep before siege."),
                Resolve(definition.ObjectiveKey, locale, "Destroy the original enemy Barracks while your original Barracks survives."),
                playable: false);
            return true;
        }

        public static bool TryResolveHud(
            SkirmishResolvedSetup setup,
            SkirmishOutcomeKind outcome,
            SkirmishEndReasonKind reason,
            string locale,
            out SkirmishHudCopy copy)
        {
            copy = default;
            if (setup == null || !IsExpandedS002(setup.CatalogId))
                return false;

            string objective = Resolve(ObjectiveKey, locale, "Destroy the original enemy Barracks while your original Barracks survives.");
            if (outcome == SkirmishOutcomeKind.None)
            {
                copy = new SkirmishHudCopy(ObjectiveKey, string.Empty, objective, string.Empty);
                return true;
            }

            if (!TryResultKey(outcome, reason, out string resultKey, out string english))
                return false;
            copy = new SkirmishHudCopy(ObjectiveKey, resultKey, objective, Resolve(resultKey, locale, english));
            return true;
        }

        public static bool TryResultKey(
            SkirmishOutcomeKind outcome,
            SkirmishEndReasonKind reason,
            out string key,
            out string english)
        {
            if (outcome == SkirmishOutcomeKind.Victory)
            {
                key = ResultVictoryKey;
                english = "Enemy main base destroyed.";
                return true;
            }

            if (outcome == SkirmishOutcomeKind.Defeat && reason == SkirmishEndReasonKind.Surrender)
            {
                key = ResultSurrenderKey;
                english = "Surrender accepted.";
                return true;
            }

            if (outcome == SkirmishOutcomeKind.Defeat)
            {
                key = ResultDefeatKey;
                english = "Your main base was destroyed.";
                return true;
            }

            if (reason == SkirmishEndReasonKind.TimeLimit || reason == SkirmishEndReasonKind.ObjectiveDeadline)
            {
                key = ResultDrawDeadlineKey;
                english = "Time expired with both original bases standing.";
                return true;
            }

            if (reason == SkirmishEndReasonKind.BothBasesDestroyed || outcome == SkirmishOutcomeKind.Draw)
            {
                key = ResultDrawBasesKey;
                english = "Both original bases were destroyed.";
                return true;
            }

            key = string.Empty;
            english = string.Empty;
            return false;
        }

        public static void ApplyLibraryCopy(ref SkirmishBattleCatalogEntry entry)
        {
            if (!IsExpandedS002(entry.ScenarioId))
                return;
            entry.TitleKey = TitleKey;
            entry.DefinitionId = "skirmish.s002";
            entry.ContentVersion = 1;
            entry.ReadinessManifestId = "publication.skirmish.s002";
            if (string.IsNullOrEmpty(entry.DescriptionEnglish))
            {
                entry.DescriptionEnglish = Resolve(
                    BriefKey,
                    GameLocalization.EnglishLocaleCode,
                    "Use the starting tank and APC to contest the highway. Keep one infantry squad at home. Press now or scout the south sweep before siege.");
            }

            if (string.IsNullOrEmpty(entry.DescriptionFarsi))
            {
                entry.DescriptionFarsi = Resolve(
                    BriefKey,
                    GameLocalization.PersianLocaleCode,
                    "با تانک و نفربر شروع‌شده بزرگراه را بگیر. یک دسته پیاده در خانه بماند. یا سریع فشار بده یا جنوب را شناسایی کن.");
            }
        }

        public static string Resolve(string key, string locale, string englishFallback)
        {
            if (string.Equals(locale, GameLocalization.PersianLocaleCode, StringComparison.OrdinalIgnoreCase))
            {
                if (TryKnownFa(key, out string fa))
                {
                    if (GameLocalization.TryGet(key, out string live) &&
                        !string.Equals(live, englishFallback, StringComparison.Ordinal))
                        return live;
                    return fa;
                }
            }

            if (!string.IsNullOrEmpty(key) && GameLocalization.TryGet(key, out string resolved) &&
                !string.IsNullOrEmpty(resolved))
                return resolved;
            return englishFallback ?? string.Empty;
        }

        private static bool TryKnownFa(string key, out string value)
        {
            switch (key)
            {
                case TitleKey:
                    value = "پایگاه صحرا · حمله به پایگاه · پایگاه برقرار";
                    return true;
                case BriefKey:
                    value = "با تانک و نفربر شروع‌شده بزرگراه را بگیر. یک دسته پیاده در خانه بماند. یا سریع فشار بده یا جنوب را شناسایی کن.";
                    return true;
                case ObjectiveKey:
                    value = "سربازخانه اصلی دشمن را نابود کن، در حالی که سربازخانه اصلی خودت سالم است.";
                    return true;
                case WarningOffensiveAirKey:
                    value = "پروفایل زمینی حمله هوایی تهاجمی را صف نمی‌کند.";
                    return true;
                case WarningReplacementBaseKey:
                    value = "سربازخانه جایگزین پایگاه پیروزی تعیین‌شده نیست.";
                    return true;
                case ResultVictoryKey:
                    value = "پایگاه اصلی دشمن نابود شد.";
                    return true;
                case ResultDefeatKey:
                    value = "پایگاه اصلی تو نابود شد.";
                    return true;
                case ResultDrawBasesKey:
                    value = "هر دو پایگاه اصلی نابود شدند.";
                    return true;
                case ResultDrawDeadlineKey:
                    value = "زمان تمام شد و هر دو پایگاه اصلی سالم ماندند.";
                    return true;
                case ResultSurrenderKey:
                    value = "تسلیم پذیرفته شد.";
                    return true;
                default:
                    value = string.Empty;
                    return false;
            }
        }
    }
}
