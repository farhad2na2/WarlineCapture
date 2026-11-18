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

    /// <summary>
    /// Player copy for Frontline Control, Breakthrough and Convoy Escort end reasons and mission text.
    /// This is the in-code fallback for the same keys carried in SkirmishResultUiStrings.json and the
    /// baked catalog, so a result never shows a raw reason identifier. Farsi is a draft pending review.
    /// </summary>
    public static class SkirmishObjectiveCopyTable
    {
        public readonly struct CopyRow
        {
            public readonly string Key, English, Farsi;
            public CopyRow(string key, string english, string farsi) { Key = key; English = english; Farsi = farsi; }
        }

        public static readonly CopyRow[] Rows =
        {
            new CopyRow("ui.skirmish.reason.ticketsdepleted.victory",
                "You held the zones and drained the enemy's tickets to zero.",
                "مناطق را نگه داشتی و امتیاز دشمن به صفر رسید."),
            new CopyRow("ui.skirmish.reason.ticketsdepleted.defeat",
                "The enemy held the zones and drained your tickets to zero.",
                "دشمن مناطق را نگه داشت و امتیاز تو به صفر رسید."),
            new CopyRow("ui.skirmish.reason.ticketsdepleted.draw",
                "Both sides ran out of tickets together.",
                "امتیاز هر دو طرف هم‌زمان به صفر رسید."),
            new CopyRow("ui.skirmish.reason.ticketsatdeadline.victory",
                "Time expired. You held the zones and kept more tickets.",
                "زمان تمام شد. مناطق را نگه داشتی و امتیاز بیشتری داشتی."),
            new CopyRow("ui.skirmish.reason.ticketsatdeadline.defeat",
                "Time expired. The enemy kept more tickets.",
                "زمان تمام شد. دشمن امتیاز بیشتری داشت."),
            new CopyRow("ui.skirmish.reason.ticketsatdeadline.draw",
                "Time expired with the tickets level.",
                "زمان تمام شد و امتیازها برابر بود."),
            new CopyRow("ui.skirmish.reason.breakthroughevacuated",
                "Breakthrough complete. Enough riflemen reached the exit.",
                "شکستن خط کامل شد. تفنگدارهای کافی به خروجی رسیدند."),
            new CopyRow("ui.skirmish.reason.breakthroughimpossible",
                "Too many riflemen were lost. The breakthrough can no longer succeed.",
                "تفنگدارهای بیش از حد از دست رفتند. شکستن خط دیگر ممکن نیست."),
            new CopyRow("ui.skirmish.reason.convoydelivered",
                "Enough convoy trucks reached the destination.",
                "کامیون‌های کافی کاروان به مقصد رسیدند."),
            new CopyRow("ui.skirmish.reason.convoyimpossible",
                "Too many convoy trucks were lost. The convoy can no longer arrive.",
                "کامیون‌های کاروان بیش از حد از دست رفتند. کاروان دیگر نمی‌تواند برسد."),
            new CopyRow("ui.skirmish.reason.objectivedeadline",
                "Time ran out before the objective was completed.",
                "زمان پیش از تکمیل هدف تمام شد."),
            new CopyRow("ui.skirmish.reason.startupfailure",
                "The mission could not start.",
                "ماموریت شروع نشد."),
            new CopyRow("ui.skirmish.reason.contentfailure",
                "The mission could not continue.",
                "ماموریت ادامه پیدا نکرد."),
            new CopyRow("ui.skirmish.reason.checkpointfailure",
                "The mission could not resume from the saved point.",
                "ماموریت از نقطه ذخیره ادامه پیدا نکرد."),
            new CopyRow("skirmish.s007.brief",
                "Hold the frontline zones. Keep your infantry in two or more of the three zones to drain the enemy's tickets before yours run out. Send your infantry to the nearest zone first.",
                "مناطق خط مقدم را نگه دار. پیاده‌نظامت را در دست‌کم دو منطقه از سه منطقه نگه دار تا پیش از تمام‌شدن امتیاز خودت، امتیاز دشمن را تمام کنی. اول پیاده‌نظام را به نزدیک‌ترین منطقه بفرست."),
            new CopyRow("skirmish.s007.objective",
                "Hold at least two of the three zones until the enemy's tickets reach zero.",
                "دست‌کم دو منطقه از سه منطقه را نگه دار تا امتیاز دشمن به صفر برسد."),
            new CopyRow("skirmish.s007.first_order",
                "Send your infantry to the nearest zone and hold it.",
                "پیاده‌نظام را به نزدیک‌ترین منطقه بفرست و آن را نگه دار."),
            new CopyRow("skirmish.s007.result.victory",
                "Enemy main base destroyed.",
                "پایگاه اصلی دشمن نابود شد."),
            new CopyRow("skirmish.s007.result.defeat",
                "Your main base was destroyed.",
                "پایگاه اصلی تو نابود شد."),
            new CopyRow("skirmish.s007.result.surrender",
                "Surrender accepted.",
                "تسلیم پذیرفته شد."),
            new CopyRow("skirmish.s007.result.draw_bases",
                "Both main bases were destroyed.",
                "هر دو پایگاه اصلی نابود شدند."),
            new CopyRow("skirmish.s007.result.draw_deadline",
                "Time expired with neither side ahead.",
                "زمان تمام شد و هیچ طرفی پیش نبود."),
            new CopyRow("skirmish.s013.brief",
                "Break through the enemy line. Hold a corridor with your infantry for twenty seconds to open the exit, then walk at least eight of your twelve riflemen out through it. Move your infantry to the nearest corridor first.",
                "خط دشمن را بشکن. یکی از گذرگاه‌ها را بیست ثانیه با پیاده‌نظام نگه دار تا خروجی باز شود، سپس دست‌کم هشت نفر از دوازده تفنگدارت را پیاده از آن خارج کن. اول پیاده‌نظام را به نزدیک‌ترین گذرگاه ببر."),
            new CopyRow("skirmish.s013.objective",
                "Open the exit, then evacuate at least eight of your twelve riflemen on foot.",
                "خروجی را باز کن، سپس دست‌کم هشت نفر از دوازده تفنگدار را پیاده تخلیه کن."),
            new CopyRow("skirmish.s013.first_order",
                "Move your infantry to the nearest corridor and hold it.",
                "پیاده‌نظام را به نزدیک‌ترین گذرگاه ببر و آن را نگه دار."),
            new CopyRow("skirmish.s013.result.victory",
                "The breakthrough succeeded.",
                "شکستن خط موفق شد."),
            new CopyRow("skirmish.s013.result.defeat",
                "The breakthrough failed.",
                "شکستن خط ناموفق بود."),
            new CopyRow("skirmish.s013.result.surrender",
                "Surrender accepted.",
                "تسلیم پذیرفته شد."),
            new CopyRow("skirmish.s019.brief",
                "Escort the convoy. Get at least two of your three supply trucks into the destination and keep them there for five seconds while your army clears the way. Send your infantry ahead along the route first.",
                "کاروان را اسکورت کن. دست‌کم دو کامیون از سه کامیون تدارکاتی را به مقصد برسان و پنج ثانیه آنجا نگه دار، در حالی که ارتشت راه را باز می‌کند. اول پیاده‌نظام را جلوتر در مسیر بفرست."),
            new CopyRow("skirmish.s019.objective",
                "Deliver at least two of your three convoy trucks to the destination.",
                "دست‌کم دو کامیون از سه کامیون کاروان را به مقصد برسان."),
            new CopyRow("skirmish.s019.first_order",
                "Send your infantry ahead along the route, then bring the trucks to the destination.",
                "پیاده‌نظام را جلوتر در مسیر بفرست، سپس کامیون‌ها را به مقصد برسان."),
            new CopyRow("skirmish.s019.result.victory",
                "The convoy arrived.",
                "کاروان به مقصد رسید."),
            new CopyRow("skirmish.s019.result.defeat",
                "The convoy was lost.",
                "کاروان از دست رفت."),
            new CopyRow("skirmish.s019.result.surrender",
                "Surrender accepted.",
                "تسلیم پذیرفته شد."),
            new CopyRow("skirmish.s013.result.draw_bases",
                "Both main bases were destroyed.",
                "هر دو پایگاه اصلی نابود شدند."),
            new CopyRow("skirmish.s013.result.draw_deadline",
                "Time expired with neither side ahead.",
                "زمان تمام شد و هیچ طرفی پیش نبود."),
            new CopyRow("skirmish.s019.result.draw_bases",
                "Both main bases were destroyed.",
                "هر دو پایگاه اصلی نابود شدند."),
            new CopyRow("skirmish.s019.result.draw_deadline",
                "Time expired with neither side ahead.",
                "زمان تمام شد و هیچ طرفی پیش نبود."),
        };

        public static bool TryGet(string key, out CopyRow row)
        {
            if (!string.IsNullOrEmpty(key))
                for (int i = 0; i < Rows.Length; i++)
                    if (string.Equals(Rows[i].Key, key, StringComparison.Ordinal)) { row = Rows[i]; return true; }
            row = default;
            return false;
        }

        // Result key for a non-base objective end. Tickets endings differ by outcome (zones held or lost);
        // a plain time limit reads as the objective deadline because there is no base-survival wording here.
        public static string ReasonKey(SkirmishEndReasonKind reason, SkirmishOutcomeKind outcome)
        {
            if (reason == SkirmishEndReasonKind.None) return "ui.skirmish.result.match_complete";
            string kind = reason == SkirmishEndReasonKind.TimeLimit ? "objectivedeadline" : reason.ToString().ToLowerInvariant();
            string key = "ui.skirmish.reason." + kind;
            if (reason == SkirmishEndReasonKind.TicketsDepleted || reason == SkirmishEndReasonKind.TicketsAtDeadline)
                key += outcome == SkirmishOutcomeKind.Victory ? ".victory" : outcome == SkirmishOutcomeKind.Defeat ? ".defeat" : ".draw";
            return key;
        }
    }

    public static class SkirmishExpandedCopyProjection
    {
        public const string S002CatalogId = "S002";
        public const string S003CatalogId = "S003";
        public const string S003TitleKey = "skirmish.s003.title";
        public const string S003BriefKey = "skirmish.s003.brief";
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

        public static bool IsExpandedS003(string catalogId) =>
            string.Equals(catalogId, S003CatalogId, StringComparison.Ordinal);

        // Copy presentation uses actual current authored identity, never a stale row version.
        private static int CurrentCodedLibraryVersion(string id,string definitionId)
        {
            var catalog=SkirmishExpansionCatalogConfig.Load();
            return catalog!=null&&catalog.TryGetDefinition(id,out var definition)&&definition!=null&&
                definition.DefinitionId==definitionId&&definition.ContentVersion>0?definition.ContentVersion:0;
        }

        public static void ApplyLibraryCopyS003(ref SkirmishBattleCatalogEntry entry)
        {
            if (!IsExpandedS003(entry.ScenarioId))
                return;
            entry.TitleKey = S003TitleKey;
            entry.DefinitionId = "skirmish.s003";
            entry.ContentVersion = CurrentCodedLibraryVersion(entry.ScenarioId,entry.DefinitionId);
            entry.ReadinessManifestId = "publication.skirmish.s003";
            if (string.IsNullOrEmpty(entry.DescriptionEnglish))
            {
                entry.DescriptionEnglish = Resolve(
                    S003BriefKey,
                    GameLocalization.EnglishLocaleCode,
                    "Hold a light ground screen and contest the highway. Build Helipad before you commit attack helicopters. Escort an APC push or transition to air while you guard supply.");
            }

            if (string.IsNullOrEmpty(entry.DescriptionFarsi))
            {
                entry.DescriptionFarsi = Resolve(
                    S003BriefKey,
                    GameLocalization.PersianLocaleCode,
                    "یک پوشش زمینی سبک نگه دار و بزرگراه را بگیر. قبل از هلی‌کوپترهای تهاجمی پد بالگرد بساز. یا با نفربر فشار بده یا به هوا برو و تدارکات را نگه دار.");
            }
        }

        public static void ApplyLibraryCopyS004(ref SkirmishBattleCatalogEntry entry)
        {
            if (entry.ScenarioId != SkirmishBattleCatalogConfig.DesertBaseAirMobileEstablishedScenarioId)
                return;
            entry.TitleKey = "skirmish.s004.title";
            entry.DefinitionId = SkirmishBattleCatalogConfig.DesertBaseAirMobileEstablishedDefinitionId;
            entry.ContentVersion = 2;
            entry.ReadinessManifestId = "publication.skirmish.s004";
            entry.DescriptionEnglish = Resolve("skirmish.s004.brief", GameLocalization.EnglishLocaleCode,
                "Scout with infantry and the starting transport helicopter. AA covers the staging area. Lift infantry toward the north ruins, or build offensive air while a ground reserve stays home.");
            entry.DescriptionFarsi = Resolve("skirmish.s004.brief", GameLocalization.PersianLocaleCode,
                "با پیاده و هلی‌کوپتر ترابری شروع‌شده شناسایی کن. پدافند هوایی محل استقرار را می‌پوشاند. پیاده را به خرابه‌های شمال ببر، یا هوایی تهاجمی بساز و یک ذخیره زمینی در خانه نگه دار.");
        }

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
            if (setup == null)
                return false;

            // Any expanded entry with compiled copy keys resolves here; the key prefix
            // convention (skirmish.sNNN.*) keeps result keys derivable per entry.
            bool s002 = IsExpandedS002(setup.CatalogId);
            if (!s002 && string.IsNullOrEmpty(setup.ObjectiveKey))
                return false;

            string objectiveKey = s002 ? ObjectiveKey : setup.ObjectiveKey;
            string objective = Resolve(objectiveKey, locale, "Destroy the original enemy Barracks while your original Barracks survives.");
            if (outcome == SkirmishOutcomeKind.None)
            {
                copy = new SkirmishHudCopy(objectiveKey, string.Empty, objective, string.Empty);
                return true;
            }

            if (!TryResultKey(setup, outcome, reason, out string resultKey, out string english))
                return false;
            copy = new SkirmishHudCopy(objectiveKey, resultKey, objective, Resolve(resultKey, locale, english));
            return true;
        }

        public static bool TryResultKey(
            SkirmishResolvedSetup setup,
            SkirmishOutcomeKind outcome,
            SkirmishEndReasonKind reason,
            out string key,
            out string english)
        {
            if (setup != null && setup.ObjectiveKind != SkirmishObjectiveKind.None &&
                setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault &&
                reason != SkirmishEndReasonKind.MainBaseDestroyed &&
                reason != SkirmishEndReasonKind.BothBasesDestroyed &&
                reason != SkirmishEndReasonKind.Surrender)
            {
                // None means the legacy HUD forwarded no reason: neutral copy. Otherwise the reason key
                // (and outcome for ticket endings) resolves to player text from the catalog or code table.
                key = SkirmishObjectiveCopyTable.ReasonKey(reason, outcome);
                english = SkirmishObjectiveCopyTable.TryGet(key, out var row) ? row.English : "MATCH COMPLETE";
                return outcome != SkirmishOutcomeKind.None;
            }

            if (setup != null && !IsExpandedS002(setup.CatalogId))
            {
                string prefix = "skirmish." + setup.CatalogId.ToLowerInvariant();
                return TryMapResultKey(
                    outcome,
                    reason,
                    string.IsNullOrEmpty(setup.ResultVictoryKey) ? prefix + ".result.victory" : setup.ResultVictoryKey,
                    string.IsNullOrEmpty(setup.ResultDefeatKey) ? prefix + ".result.defeat" : setup.ResultDefeatKey,
                    prefix + ".result.surrender",
                    prefix + ".result.draw_deadline",
                    prefix + ".result.draw_bases",
                    out key,
                    out english);
            }

            return TryMapResultKey(
                outcome,
                reason,
                ResultVictoryKey,
                ResultDefeatKey,
                ResultSurrenderKey,
                ResultDrawDeadlineKey,
                ResultDrawBasesKey,
                out key,
                out english);
        }

        private static bool TryMapResultKey(
            SkirmishOutcomeKind outcome,
            SkirmishEndReasonKind reason,
            string victoryKey,
            string defeatKey,
            string surrenderKey,
            string drawDeadlineKey,
            string drawBasesKey,
            out string key,
            out string english)
        {
            if (outcome == SkirmishOutcomeKind.Victory)
            {
                key = victoryKey;
                english = "Enemy main base destroyed.";
                return true;
            }

            if (outcome == SkirmishOutcomeKind.Defeat && reason == SkirmishEndReasonKind.Surrender)
            {
                key = surrenderKey;
                english = "Surrender accepted.";
                return true;
            }

            if (outcome == SkirmishOutcomeKind.Defeat)
            {
                key = defeatKey;
                english = "Your main base was destroyed.";
                return true;
            }

            if (reason == SkirmishEndReasonKind.TimeLimit || reason == SkirmishEndReasonKind.ObjectiveDeadline)
            {
                key = drawDeadlineKey;
                english = "Time expired with both original bases standing.";
                return true;
            }

            if (reason == SkirmishEndReasonKind.BothBasesDestroyed || outcome == SkirmishOutcomeKind.Draw)
            {
                key = drawBasesKey;
                english = "Both original bases were destroyed.";
                return true;
            }

            key = string.Empty;
            english = string.Empty;
            return false;
        }

        public static void ApplyLibraryCopy(ref SkirmishBattleCatalogEntry entry)
        {
            if(IsExpandedS003(entry.ScenarioId)){ApplyLibraryCopyS003(ref entry);return;}
            if (!IsExpandedS002(entry.ScenarioId))
                return;
            entry.TitleKey = TitleKey;
            entry.DefinitionId = "skirmish.s002";
            entry.ContentVersion = CurrentCodedLibraryVersion(entry.ScenarioId,entry.DefinitionId);
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
            if (!string.IsNullOrEmpty(key))
            {
                if (string.Equals(locale, GameLocalization.CurrentLocaleCode, StringComparison.OrdinalIgnoreCase))
                {
                    if (GameLocalization.TryGet(key, out string live) && !string.IsNullOrEmpty(live))
                        return live;
                }
                else
                {
                    // Authoring resolves both languages without changing the active UI locale.
                    foreach (GameLocaleTable table in GameLocalization.AvailableLocales)
                    {
                        if (table == null || !string.Equals(table.LocaleCode, locale, StringComparison.OrdinalIgnoreCase))
                            continue;
                        foreach (GameLocalizedStringRecord entry in table.Entries)
                            if (entry != null && entry.Key == key && !string.IsNullOrEmpty(entry.Value))
                                return entry.Value;
                    }
                }
            }
            bool persian = string.Equals(locale, GameLocalization.PersianLocaleCode, StringComparison.OrdinalIgnoreCase);
            if (persian && TryKnownFa(key, out string fa))
                return fa;
            if (SkirmishObjectiveCopyTable.TryGet(key, out var row))
                return persian ? row.Farsi : row.English;
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
