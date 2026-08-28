using Game.Catalog.Contracts;
using Game.Narrative.Contracts;

namespace Game.Configs
{
    public readonly struct M02NarrativeLocalizedLine
    {
        public readonly string LineId;
        public readonly string TextKey;
        public readonly NarrativeSpeakerId Speaker;
        public readonly string English;
        public readonly string Persian;

        public M02NarrativeLocalizedLine(
            string lineId,
            string textKey,
            NarrativeSpeakerId speaker,
            string english,
            string persian)
        {
            LineId = lineId;
            TextKey = textKey;
            Speaker = speaker;
            English = english;
            Persian = persian;
        }
    }

    public static class M02EstablishBaseLocalizedText
    {
        public static readonly M02NarrativeLocalizedLine[] Brief =
        {
            new(
                "m02-brief.line.1",
                "narrative.m02.brief.dalia",
                NarrativeSpeakerId.Dalia,
                "This forward post is abandoned, but we need it. Restore it and prepare to defend the clinic road.",
                "این پاسگاه متروکه است، اما به آن نیاز داریم. آن را دوباره فعال کنید و برای دفاع از مسیر درمانگاه آماده شوید."),
            new(
                "m02-brief.line.2",
                "narrative.m02.brief.aria",
                NarrativeSpeakerId.Aria,
                "Build a Barracks here, then train one rifle squad. That will make the post operational.",
                "اینجا یک سربازخانه بسازید، سپس یک گروه تفنگدار آموزش دهید. با این کار پاسگاه دوباره فعال می‌شود."),
            new(
                "m02-brief.line.3",
                "narrative.m02.brief.samira",
                NarrativeSpeakerId.Samira,
                "The clinic and city crews use this road. Holding the post keeps their route open.",
                "درمانگاه و نیروهای خدمات شهری از این مسیر استفاده می‌کنند. حفظ پاسگاه، راه آن‌ها را باز نگه می‌دارد.")
        };

        public static readonly M02NarrativeLocalizedLine[] Comms =
        {
            new(
                "m02-comms.line.1",
                "narrative.m02.comms.dalia",
                NarrativeSpeakerId.Dalia,
                "Enemy patrol approaching from the west. Hold the post and keep them away from the clinic road.",
                "یک گشت دشمن از غرب نزدیک می‌شود. پاسگاه را حفظ کنید و نگذارید به مسیر درمانگاه برسند."),
            new(
                "m02-comms.line.2",
                "narrative.m02.comms.aria",
                NarrativeSpeakerId.Aria,
                "We found a city access list on one attacker. It was copied before the first strike.",
                "یک فهرست دسترسی شهری همراه یکی از مهاجمان پیدا شد. این فهرست پیش از نخستین حمله کپی شده است."),
            new(
                "m02-comms.line.3",
                "narrative.m02.comms.samira",
                NarrativeSpeakerId.Samira,
                "It marks power stations, service gates, and tunnels. Someone stole it before the attack.",
                "در آن، پست‌های برق، ورودی‌های خدماتی و تونل‌ها مشخص شده‌اند. کسی پیش از حمله آن را دزدیده است.")
        };

        public static readonly M02NarrativeLocalizedLine[] Debrief =
        {
            new(
                "m02-debrief.line.1",
                "narrative.m02.debrief.samira",
                NarrativeSpeakerId.Samira,
                "The post is active again. The clinic road and city response teams are connected.",
                "پاسگاه دوباره فعال است. مسیر درمانگاه و تیم‌های امداد شهری دوباره به هم متصل شده‌اند."),
            new(
                "m02-debrief.line.2",
                "narrative.m02.debrief.dalia",
                NarrativeSpeakerId.Dalia,
                "Commander, Dalia Rahim. I will lead the ground response from this post.",
                "فرمانده، دالیا رحیم هستم. از این پاسگاه، هدایت نیروهای زمینی را بر عهده می‌گیرم."),
            new(
                "m02-debrief.line.3",
                "narrative.m02.debrief.aria",
                NarrativeSpeakerId.Aria,
                "The warning network ahead has gone dark. Armored vehicles are moving toward the next sector.",
                "شبکه هشدار در مسیر پیش رو خاموش شده است. خودروهای زرهی به سمت منطقه بعدی حرکت می‌کنند.")
        };

        public static bool TryGetTutorial(
            byte step,
            FirstLaunchNarrativeLanguage language,
            out string title,
            out string body)
        {
            bool persian = language == FirstLaunchNarrativeLanguage.Persian;
            (title, body) = (step, persian) switch
            {
                (2, false) => ("Open Build", "Open the Build menu."),
                (3, false) => ("Select Barracks", "Select Barracks from the building list."),
                (4, false) => ("Place the Barracks", "Place the Barracks inside the green area, then confirm construction."),
                (5, false) => ("Check the cost", "Check the resource bar. The Barracks cost 40,000 Credits and 90 Materials."),
                (6, false) => ("Train a rifle squad", "Open production and recruit one rifle squad."),
                (7, false) => ("Enemy patrol incoming", "An enemy patrol is approaching from the west. Prepare your squad at the marked lane."),
                (8, false) => ("Defend the post", "Hold the marked lane and protect the forward post."),
                (2, true) => ("منوی ساخت را باز کنید", "منوی ساخت را باز کنید."),
                (3, true) => ("سربازخانه را انتخاب کنید", "سربازخانه را از فهرست ساختمان‌ها انتخاب کنید."),
                (4, true) => ("سربازخانه را بسازید", "سربازخانه را داخل محدوده سبز قرار دهید و ساخت را تأیید کنید."),
                (5, true) => ("هزینه را بررسی کنید", "نوار منابع را بررسی کنید. سربازخانه ۴۰ هزار اعتبار و ۹۰ واحد مصالح هزینه دارد."),
                (6, true) => ("یک گروه تفنگدار آموزش دهید", "بخش تولید را باز کنید و یک گروه تفنگدار آموزش دهید."),
                (7, true) => ("گشت دشمن نزدیک می‌شود", "یک گشت دشمن از غرب نزدیک می‌شود. گروه خود را در مسیر علامت‌گذاری‌شده آماده کنید."),
                (8, true) => ("از پاسگاه دفاع کنید", "مسیر علامت‌گذاری‌شده را حفظ کنید و از پاسگاه دفاع کنید."),
                _ => (string.Empty, string.Empty)
            };
            return step is >= 2 and <= 8;
        }
    }
}
