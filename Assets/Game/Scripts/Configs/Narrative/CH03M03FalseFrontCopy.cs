using Game.Catalog.Contracts;

namespace Game.Configs
{
    public readonly struct FalseFrontNarrativeLine
    {
        public readonly string Id, Key, English, Persian;
        public readonly NarrativeSpeakerId Speaker;

        public FalseFrontNarrativeLine(string id, NarrativeSpeakerId speaker, string english, string persian)
        {
            Id = "false_front-" + id;
            Key = "narrative.false_front." + id.Replace('-', '.');
            Speaker = speaker;
            English = english;
            Persian = persian;
        }
    }

    public static class CH03M03FalseFrontCopy
    {
        public static readonly FalseFrontNarrativeLine[] Brief =
        {
            new("brief-01", NarrativeSpeakerId.Samira,
                "Three evacuation vehicles are gathering at the east road. A sealed authority report says the threat is at the old depot, but my people have seen no fighters there.",
                "سه خودروی تخلیه توی جادهٔ شرقی جمع شدن. یک گزارش مُهرشده می‌گه خطر کنار انبار قدیمیه، اما آدم‌های من اونجا هیچ نیروی مسلحی ندیدن."),
            new("brief-02", NarrativeSpeakerId.Aria,
                "The report's seal is valid, but its source trail is incomplete. The depot is unconfirmed. I recommend checking both approaches while keeping the evacuation route protected.",
                "مُهر گزارش معتبره، ولی مسیر منبعش کامل نیست. خطرِ انبار هنوز تأیید نشده. پیشنهاد می‌کنم هر دو مسیر رو بررسی کنی و جادهٔ تخلیه رو امن نگه داری."),
            new("brief-03", NarrativeSpeakerId.Dalia,
                "No strike on a rumor. Put our squad on the two observation points, move the three civilian vehicles toward shelter, and be ready to turn when we know where the ambush is.",
                "با شایعه حمله نمی‌کنیم. گروه رو به دو نقطهٔ دیده‌بانی ببر، سه خودروی غیرنظامی رو به سمت پناهگاه حرکت بده و آماده باش وقتی جای کمین روشن شد مسیر رو عوض کنی.")
        };

        public static readonly FalseFrontNarrativeLine[] Comms =
        {
            new("comms-01", NarrativeSpeakerId.Samira,
                "The depot is empty. Ash Line fighters are building a roadblock beyond the second checkpoint, right across the evacuation lane. That's the real ambush.",
                "انبار خالیه. نیروهای خط خاکستر بعد از ایستگاه دوم، درست روی مسیر تخلیه دارن راه‌بند می‌سازن. کمین واقعی اونجاست."),
            new("comms-02", NarrativeSpeakerId.Aria,
                "My first interpretation was wrong. I treated the seal as stronger evidence than the missing source trail. Redirect to the confirmed roadblock; keep the vehicles out of its fire.",
                "برداشت اول من اشتباه بود. به مُهر گزارش بیشتر از جای خالیِ مسیر منبعش وزن دادم. به سمت راه‌بندِ تأییدشده برو و خودروها رو از تیررسش دور نگه دار.")
        };

        public static readonly FalseFrontNarrativeLine[] Debrief =
        {
            new("debrief-01", NarrativeSpeakerId.Dalia,
                "All three vehicles reached shelter. The confirmed ambush is down, and the depot was never attacked. That distinction kept civilians alive.",
                "هر سه خودرو به پناهگاه رسیدن. کمینِ تأییدشده از بین رفت و به انبار حمله نکردیم. همین فرق جون غیرنظامی‌ها رو نجات داد."),
            new("debrief-02", NarrativeSpeakerId.Aria,
                "I preserved the false report. It traveled through a compromised Relay-era authority channel with a seal compatible with mine. I need to examine whether my missing archive made it.",
                "گزارش جعلی رو نگه داشتم. از یک کانال فرماندهیِ لو‌رفتهٔ دورهٔ رله اومده و مُهرش با مُهر من سازگاره. باید بررسی کنم که آیا بایگانی گم‌شدهٔ من ساخته‌تش یا نه.")
        };
    }
}
