using System;
using System.Collections.Generic;

namespace Game.Operations.Content
{
    /// <summary>
    /// Operations-owned EN/FA copy for O001–O003. Does not edit the shipping localization catalog.
    /// Mobile-ready coach / escort / result keys live here; list any new keys for Game Design.
    /// </summary>
    public static class OperationsLocalizedCopy
    {
        static readonly Dictionary<string, string> English = BuildEnglish();
        static readonly Dictionary<string, string> Farsi = BuildFarsi();

        public static bool TryGet(string key, string language, out string value)
        {
            Dictionary<string, string> table = string.Equals(language, "fa", StringComparison.OrdinalIgnoreCase)
                ? Farsi
                : English;
            return table.TryGetValue(key, out value);
        }

        public static string Require(string key, string language)
        {
            if (!TryGet(key, language, out string value))
                throw new InvalidOperationException("missing_key:" + key);
            return value;
        }

        public static bool HasMissionKeys(string missionSlug)
        {
            string[] suffixes =
            {
                ".title", ".brief", ".objective.primary", ".warning.deadline", ".result.victory",
                ".result.partial", ".result.defeat", ".result.withdrawn", ".approach.main", ".approach.safe"
            };
            for (int index = 0; index < suffixes.Length; index++)
            {
                string key = "operations." + missionSlug + suffixes[index];
                if (!English.ContainsKey(key) || !Farsi.ContainsKey(key))
                    return false;
            }

            return true;
        }

        static Dictionary<string, string> BuildEnglish()
        {
            var table = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["operations.o001.title"] = "Street Signals",
                ["operations.o001.brief"] = "Locate the active relay by checking three known search courtyards.",
                ["operations.o001.objective.primary"] = "Scan three courtyards, recover relay evidence, and extract.",
                ["operations.o001.warning.deadline"] = "Hard deadline: 12 minutes after control begins.",
                ["operations.o001.result.victory"] = "Relay evidence secured. Public relay hint persists.",
                ["operations.o001.result.partial"] = "Partial: two scans and two infantry extracted without full evidence.",
                ["operations.o001.result.defeat"] = "Street Signals failed.",
                ["operations.o001.result.withdrawn"] = "Withdrawn from Street Signals.",
                ["operations.o001.approach.main"] = "Split scouts across courtyards for speed.",
                ["operations.o001.approach.safe"] = "Keep the squad together for protection.",

                ["operations.o002.title"] = "Clinic Supply Route",
                ["operations.o002.brief"] = "Bring medical trucks to the clinic. Choose the exposed street or the covered service loop.",
                ["operations.o002.objective.primary"] = "Scan the junction, escort two trucks, and hold the clinic.",
                ["operations.o002.warning.deadline"] = "Hard deadline: 14 minutes after control begins.",
                ["operations.o002.result.victory"] = "Clinic route opened. Aid deliveries recorded.",
                ["operations.o002.result.partial"] = "Partial: one truck unloaded while the clinic survived.",
                ["operations.o002.result.defeat"] = "Clinic Supply Route failed.",
                ["operations.o002.result.withdrawn"] = "Withdrawn from Clinic Supply Route.",
                ["operations.o002.approach.main"] = "Direct street — shorter, more exposed.",
                ["operations.o002.approach.safe"] = "Service loop — longer with infantry cover.",

                ["operations.o003.title"] = "Courtyard Water Point",
                ["operations.o003.brief"] = "Restore two neighborhood pumps while preserving the clinic.",
                ["operations.o003.objective.primary"] = "Clear pump guards, repair both pumps, and hold the service court.",
                ["operations.o003.warning.deadline"] = "Hard deadline: 15 minutes after control begins.",
                ["operations.o003.result.victory"] = "Service milestone set. Both pumps restored.",
                ["operations.o003.result.partial"] = "Partial: one pump restored with the clinic intact.",
                ["operations.o003.result.defeat"] = "Courtyard Water Point failed.",
                ["operations.o003.result.withdrawn"] = "Withdrawn from Courtyard Water Point.",
                ["operations.o003.approach.main"] = "Repair sequentially with full cover.",
                ["operations.o003.approach.safe"] = "Split specialists to reduce exposure time.",

                ["operations.day_report.title"] = "End of Day Report",
                ["operations.day_report.continue"] = "Continue",
                ["operations.aria.focus"] = "Focus camera on the active objective.",
                ["operations.aria.scan"] = "Issue Scan on the selected site.",
                ["operations.aria.interact"] = "Issue Interact on the selected object.",
                ["operations.aria.repair"] = "Issue Repair with a repair specialist.",
                ["operations.aria.escort_go"] = "Release the convoy on the chosen route.",
                ["operations.aria.escort_hold"] = "Hold the convoy in place.",
                ["operations.aria.hold"] = "Hold the marked zone.",
                ["operations.aria.extract"] = "Extract eligible infantry to the ground exit.",
                ["operations.aria.attack"] = "Attack a hostile in range.",
                ["operations.aria.conclude"] = "Conclude when the partial predicate is true.",
                ["operations.aria.withdraw"] = "Withdraw with confirmation.",

                ["operations.coach.o001.scan.title"] = "Scan",
                ["operations.coach.o001.scan.body"] = "Scan the marked courtyards. Keep moving — coaching never locks you out.",
                ["operations.coach.o001.evidence.title"] = "Evidence",
                ["operations.coach.o001.evidence.body"] = "Recover the relay evidence at the revealed site.",
                ["operations.coach.o001.extract.title"] = "Extract",
                ["operations.coach.o001.extract.body"] = "Extract at least two infantry to the ground exit.",
                ["operations.coach.o001.done.title"] = "Ready",
                ["operations.coach.o001.done.body"] = "Objectives complete. Finish the mission.",

                ["operations.controls.escort.go"] = "Go",
                ["operations.controls.escort.hold"] = "Hold",
                ["operations.controls.route.main"] = "Main street",
                ["operations.controls.route.safe"] = "Service loop",
                ["operations.controls.repair"] = "Repair",
                ["operations.controls.defend.hold"] = "Hold court",
                ["operations.warning.clinic"] = "Protect the clinic",
                ["operations.warning.pumps"] = "Pumps under pressure",
                ["operations.warning.severity.watch"] = "Watch",
                ["operations.warning.severity.critical"] = "Critical",

                ["operations.result.continue"] = "Continue",
                ["operations.result.practice"] = "Practice",
                ["operations.result.delta.trust"] = "Trust",
                ["operations.result.delta.intel"] = "Intel",
                ["operations.result.delta.heat"] = "Heat",
                ["operations.result.delta.generic"] = "District",

                ["operations.teach.partial.conclude.title"] = "Conclude (Partial)",
                ["operations.teach.partial.withdraw.title"] = "Withdraw",

                ["operations.hud.shell"] = "OPERATIONS",
                ["operations.hud.in_progress"] = "IN PROGRESS",
                ["operations.hud.victory"] = "VICTORY",
                ["operations.hud.objectives"] = "Objectives",
                ["operations.hud.timer"] = "Time left",
                ["operations.hud.done"] = "done",
                ["operations.hud.failed"] = "failed",
                ["operations.hud.active"] = "active",
                ["operations.hud.locked"] = "locked",
                ["operations.hud.channeling"] = "Working",
                ["operations.hud.pressure"] = "Stay on target",
                ["operations.hud.hold_refresh"] = "Re-confirm Hold — do not AFK",
                ["operations.hud.deadline_pressure"] = "Deadline pressure — decide now",
                ["operations.hud.reward_credits"] = "Credits",
                ["operations.hud.reward_xp"] = "Commander XP",
                ["operations.hud.continue"] = "Tap Continue to return to Operations",

                ["operations.objective.scan_signals"] = "Scan courtyards",
                ["operations.objective.interact_relay"] = "Recover relay evidence",
                ["operations.objective.extract_force"] = "Extract infantry",
                ["operations.objective.scan_junction"] = "Scan junction",
                ["operations.objective.escort_trucks"] = "Escort two medical trucks",
                ["operations.objective.hold_clinic"] = "Hold the clinic",
                ["operations.objective.protect_clinic"] = "Protect the clinic",
                ["operations.objective.clear_pump_guards"] = "Clear pump guards",
                ["operations.objective.repair_pump_west"] = "Repair west pump",
                ["operations.objective.repair_pump_east"] = "Repair east pump",
                ["operations.objective.hold_service_court"] = "Hold service court",
                ["operations.objective.protect_clinic_pumps"] = "Protect clinic and pumps",

                ["operations.o001.objective.scan_signals"] = "Scan courtyards"
            };
            return table;
        }

        static Dictionary<string, string> BuildFarsi()
        {
            var table = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["operations.o001.title"] = "نشانه‌های خیابان",
                ["operations.o001.brief"] = "با بررسی سه حیاط جستجو، رله فعال را پیدا کنید.",
                ["operations.o001.objective.primary"] = "سه حیاط را اسکن کنید، مدرک رله را بگیرید و خارج شوید.",
                ["operations.o001.warning.deadline"] = "مهلت سخت: ۱۲ دقیقه پس از شروع کنترل.",
                ["operations.o001.result.victory"] = "مدرک رله امن شد. راهنمای عمومی رله باقی می‌ماند.",
                ["operations.o001.result.partial"] = "جزئی: دو اسکن و دو پیاده خارج شدند بدون مدرک کامل.",
                ["operations.o001.result.defeat"] = "نشانه‌های خیابان شکست خورد.",
                ["operations.o001.result.withdrawn"] = "از نشانه‌های خیابان عقب‌نشینی شد.",
                ["operations.o001.approach.main"] = "پیشاهنگان را برای سرعت جدا کنید.",
                ["operations.o001.approach.safe"] = "گروه را برای حفاظت یکجا نگه دارید.",

                ["operations.o002.title"] = "مسیر تدارکات درمانگاه",
                ["operations.o002.brief"] = "کامیون‌های پزشکی را به درمانگاه برسانید. خیابان باز یا حلقه خدماتی را انتخاب کنید.",
                ["operations.o002.objective.primary"] = "تقاطع را اسکن کنید، دو کامیون را اسکورت کنید و درمانگاه را نگه دارید.",
                ["operations.o002.warning.deadline"] = "مهلت سخت: ۱۴ دقیقه پس از شروع کنترل.",
                ["operations.o002.result.victory"] = "مسیر درمانگاه باز شد. تحویل کمک ثبت شد.",
                ["operations.o002.result.partial"] = "جزئی: یک کامیون تخلیه شد و درمانگاه زنده ماند.",
                ["operations.o002.result.defeat"] = "مسیر تدارکات درمانگاه شکست خورد.",
                ["operations.o002.result.withdrawn"] = "از مسیر تدارکات درمانگاه عقب‌نشینی شد.",
                ["operations.o002.approach.main"] = "خیابان مستقیم — کوتاه‌تر و در معرض‌تر.",
                ["operations.o002.approach.safe"] = "حلقه خدماتی — طولانی‌تر با پوشش پیاده.",

                ["operations.o003.title"] = "نقطه آب حیاط",
                ["operations.o003.brief"] = "دو پمپ محله را بازسازی کنید و درمانگاه را حفظ کنید.",
                ["operations.o003.objective.primary"] = "نگهبانان پمپ را پاک کنید، هر دو پمپ را تعمیر کنید و حیاط خدمت را نگه دارید.",
                ["operations.o003.warning.deadline"] = "مهلت سخت: ۱۵ دقیقه پس از شروع کنترل.",
                ["operations.o003.result.victory"] = "نقطه عطف خدمات ثبت شد. هر دو پمپ بازسازی شدند.",
                ["operations.o003.result.partial"] = "جزئی: یک پمپ بازسازی شد و درمانگاه سالم ماند.",
                ["operations.o003.result.defeat"] = "نقطه آب حیاط شکست خورد.",
                ["operations.o003.result.withdrawn"] = "از نقطه آب حیاط عقب‌نشینی شد.",
                ["operations.o003.approach.main"] = "تعمیر پیاپی با پوشش کامل.",
                ["operations.o003.approach.safe"] = "متخصصان را برای کاهش زمان در معرض بودن تقسیم کنید.",

                ["operations.day_report.title"] = "گزارش پایان روز",
                ["operations.day_report.continue"] = "ادامه",
                ["operations.aria.focus"] = "دوربین را روی هدف فعال متمرکز کنید.",
                ["operations.aria.scan"] = "اسکن را روی سایت انتخاب‌شده صادر کنید.",
                ["operations.aria.interact"] = "تعامل را روی شیء انتخاب‌شده صادر کنید.",
                ["operations.aria.repair"] = "تعمیر را با متخصص تعمیر صادر کنید.",
                ["operations.aria.escort_go"] = "کاروان را در مسیر انتخاب‌شده رها کنید.",
                ["operations.aria.escort_hold"] = "کاروان را در جای خود نگه دارید.",
                ["operations.aria.hold"] = "منطقه مشخص‌شده را نگه دارید.",
                ["operations.aria.extract"] = "پیاده‌نظام واجد شرایط را به خروج زمینی ببرید.",
                ["operations.aria.attack"] = "به دشمن در برد حمله کنید.",
                ["operations.aria.conclude"] = "وقتی شرط جزئی درست است، نتیجه را ببندید.",
                ["operations.aria.withdraw"] = "با تأیید عقب‌نشینی کنید.",

                ["operations.coach.o001.scan.title"] = "اسکن",
                ["operations.coach.o001.scan.body"] = "حیاط‌های علامت‌گذاری‌شده را اسکن کنید. به حرکت ادامه دهید — مربی شما را قفل نمی‌کند.",
                ["operations.coach.o001.evidence.title"] = "مدرک",
                ["operations.coach.o001.evidence.body"] = "مدرک رله را در محل آشکارشده بگیرید.",
                ["operations.coach.o001.extract.title"] = "خروج",
                ["operations.coach.o001.extract.body"] = "حداقل دو پیاده را به خروج زمینی ببرید.",
                ["operations.coach.o001.done.title"] = "آماده",
                ["operations.coach.o001.done.body"] = "اهداف کامل شد. مأموریت را تمام کنید.",

                ["operations.controls.escort.go"] = "حرکت",
                ["operations.controls.escort.hold"] = "توقف",
                ["operations.controls.route.main"] = "خیابان اصلی",
                ["operations.controls.route.safe"] = "حلقه خدماتی",
                ["operations.controls.repair"] = "تعمیر",
                ["operations.controls.defend.hold"] = "نگه داشتن حیاط",
                ["operations.warning.clinic"] = "از درمانگاه محافظت کنید",
                ["operations.warning.pumps"] = "پمپ‌ها تحت فشارند",
                ["operations.warning.severity.watch"] = "مراقب",
                ["operations.warning.severity.critical"] = "بحرانی",

                ["operations.result.continue"] = "ادامه",
                ["operations.result.practice"] = "تمرین",
                ["operations.result.delta.trust"] = "اعتماد",
                ["operations.result.delta.intel"] = "اطلاعات",
                ["operations.result.delta.heat"] = "فشار",
                ["operations.result.delta.generic"] = "منطقه",

                ["operations.teach.partial.conclude.title"] = "بستن (جزئی)",
                ["operations.teach.partial.withdraw.title"] = "عقب‌نشینی",

                ["operations.hud.shell"] = "عملیات",
                ["operations.hud.in_progress"] = "در حال انجام",
                ["operations.hud.victory"] = "پیروزی",
                ["operations.hud.objectives"] = "اهداف",
                ["operations.hud.timer"] = "زمان باقی‌مانده",
                ["operations.hud.done"] = "انجام شد",
                ["operations.hud.failed"] = "شکست",
                ["operations.hud.active"] = "فعال",
                ["operations.hud.locked"] = "قفل",
                ["operations.hud.channeling"] = "در حال کار",
                ["operations.hud.pressure"] = "روی هدف بمانید",
                ["operations.hud.hold_refresh"] = "نگه‌داری را دوباره تأیید کنید — AFK نکنید",
                ["operations.hud.deadline_pressure"] = "فشار مهلت — الان تصمیم بگیرید",
                ["operations.hud.reward_credits"] = "اعتبار",
                ["operations.hud.reward_xp"] = "تجربه فرمانده",
                ["operations.hud.continue"] = "برای بازگشت به عملیات ادامه را بزنید",

                ["operations.objective.scan_signals"] = "اسکن حیاط‌ها",
                ["operations.objective.interact_relay"] = "بازیابی مدرک رله",
                ["operations.objective.extract_force"] = "خروج پیاده‌نظام",
                ["operations.objective.scan_junction"] = "اسکن تقاطع",
                ["operations.objective.escort_trucks"] = "اسکورت دو کامیون پزشکی",
                ["operations.objective.hold_clinic"] = "نگه‌داری درمانگاه",
                ["operations.objective.protect_clinic"] = "حفاظت از درمانگاه",
                ["operations.objective.clear_pump_guards"] = "پاکسازی نگهبانان پمپ",
                ["operations.objective.repair_pump_west"] = "تعمیر پمپ غربی",
                ["operations.objective.repair_pump_east"] = "تعمیر پمپ شرقی",
                ["operations.objective.hold_service_court"] = "نگه‌داری حیاط خدمت",
                ["operations.objective.protect_clinic_pumps"] = "حفاظت درمانگاه و پمپ‌ها",

                ["operations.o001.objective.scan_signals"] = "اسکن حیاط‌ها"
            };
            return table;
        }
    }
}
