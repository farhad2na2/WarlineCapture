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

    public static class M02EstablishBaseTextCatalog
    {
        public static readonly M02NarrativeLocalizedLine[] Brief =
        {
            new(
                "m02-brief.line.1",
                "narrative.m02.brief.dalia",
                NarrativeSpeakerId.Dalia,
                "This forward post is abandoned, but we need it. Restore it and prepare to defend the clinic road.",
                "این پاسگاه خالی مونده، ولی بهش نیاز داریم. دوباره راهش بندازید و آماده باشید از جادهٔ درمانگاه دفاع کنید."),
            new(
                "m02-brief.line.2",
                "narrative.m02.brief.aria",
                NarrativeSpeakerId.Aria,
                "Build a Barracks here, then train one rifle squad. That will make the post operational.",
                "اینجا یه سربازخانه بساز، بعد یه گروه تفنگدار آموزش بده. با همین دو کار، پاسگاه دوباره راه می‌افته."),
            new(
                "m02-brief.line.3",
                "narrative.m02.brief.samira",
                NarrativeSpeakerId.Samira,
                "The clinic and city crews use this road. Holding the post keeps their route open.",
                "امدادگرها و نیروهای خدمات شهری از این جاده می‌رن. اگه پاسگاه رو نگه داریم، راهشون باز می‌مونه.")
        };

        public static readonly M02NarrativeLocalizedLine[] Comms =
        {
            new(
                "m02-comms.line.1",
                "narrative.m02.comms.dalia",
                NarrativeSpeakerId.Dalia,
                "Enemy patrol approaching from the west. Hold the post and keep them away from the clinic road.",
                "گشت دشمن داره از غرب میاد. پاسگاه رو نگه دارید؛ نذارید به جادهٔ درمانگاه برسن."),
            new(
                "m02-comms.line.2",
                "narrative.m02.comms.aria",
                NarrativeSpeakerId.Aria,
                "We found a city access list on one attacker. It was copied before the first strike.",
                "همراه یکی از مهاجم‌ها یه فهرست دسترسی شهری پیدا کردیم. قبل از اولین حمله کپی شده."),
            new(
                "m02-comms.line.3",
                "narrative.m02.comms.samira",
                NarrativeSpeakerId.Samira,
                "It marks power stations, service gates, and tunnels. Someone stole it before the attack.",
                "جای پست‌های برق، ورودی‌های خدماتی و تونل‌ها توشه. یکی قبل از حمله این فهرست رو دزدیده.")
        };

        public static readonly M02NarrativeLocalizedLine[] Debrief =
        {
            new(
                "m02-debrief.line.1",
                "narrative.m02.debrief.samira",
                NarrativeSpeakerId.Samira,
                "The post is active again. The clinic road and city response teams are connected.",
                "پاسگاه دوباره راه افتاد. حالا تیم‌های امداد می‌تونن از جادهٔ درمانگاه رفت‌وآمد کنن."),
            new(
                "m02-debrief.line.2",
                "narrative.m02.debrief.dalia",
                NarrativeSpeakerId.Dalia,
                "Commander, Dalia Rahim. I will lead the ground response from this post.",
                "فرمانده، من دالیا رحیمم. هدایت نیروهای زمینی رو از همین پاسگاه به عهده می‌گیرم."),
            new(
                "m02-debrief.line.3",
                "narrative.m02.debrief.aria",
                NarrativeSpeakerId.Aria,
                "The warning network ahead has gone dark. Armored vehicles are moving toward the next sector.",
                "شبکهٔ هشدار منطقهٔ بعدی قطع شده. خودروهای زرهی دارن به اون سمت می‌رن.")
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
                (5, false) => ("Plan your Materials", "You started with 120 Materials. The Barracks used 90, leaving 30. Save 20 to train your rifle squad."),
                (6, false) => ("Train a rifle squad", "Open production and recruit one rifle squad."),
                (7, false) => ("Enemy patrol incoming", "An enemy patrol is approaching from the west. Prepare your squad at the marked lane."),
                (8, false) => ("Defend the post", "Hold the marked lane and protect the forward post."),
                (2, true) => ("منوی ساخت رو باز کن", "منوی «ساخت» رو باز کن."),
                (3, true) => ("سربازخانه رو انتخاب کن", "از فهرست ساختمان‌ها، «سربازخانه» رو انتخاب کن."),
                (4, true) => ("سربازخانه رو بساز", "سربازخانه رو توی محدودهٔ سبز بذار، بعد ساختش رو تأیید کن."),
                (5, true) => ("مصالح رو مدیریت کن", "با صد و بیست واحد مصالح شروع کردی. سربازخانه نود تا مصرف کرد؛ سی تا مونده. بیست تاش رو برای آموزش گروه تفنگدار نگه دار."),
                (6, true) => ("یک گروه تفنگدار آموزش بده", "بخش تولید رو باز کن و یه گروه تفنگدار آموزش بده."),
                (7, true) => ("گشت دشمن نزدیک می‌شه", "گشت دشمن داره از غرب میاد. گروهت رو توی مسیر مشخص‌شده آماده کن."),
                (8, true) => ("از پاسگاه دفاع کن", "مسیر مشخص‌شده رو نگه دار و از پاسگاه دفاع کن."),
                _ => (string.Empty, string.Empty)
            };
            return step is >= 2 and <= 8;
        }
    }
}
