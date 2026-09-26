using Game.Catalog.Contracts;

namespace Game.Configs
{
    public readonly struct MarketLifelineNarrativeLine
    {
        public readonly string Id, Key, English, Persian;
        public readonly NarrativeSpeakerId Speaker;

        public MarketLifelineNarrativeLine(string id, NarrativeSpeakerId speaker, string english, string persian)
        {
            Id = "market_lifeline-" + id;
            Key = "narrative.market_lifeline." + id.Replace('-', '.');
            Speaker = speaker;
            English = english;
            Persian = persian;
        }
    }

    public static class CH02M03MarketLifelineCopy
    {
        public static readonly MarketLifelineNarrativeLine[] Brief =
        {
            new("brief-01", NarrativeSpeakerId.Yasin,
                "Old Market is short of food and medicine, but the market workers are not your enemy. Two manifests claim the same trade route; one covers our three relief trucks, and one is a copied transfer.",
                "بازار قدیمی با کمبود غذا و دارو روبه‌روئه، اما کارگرهای بازار دشمن شما نیستن. دو بارنامه ادعای یک مسیر تجاری رو دارن؛ یکی برای سه کامیون امدادی ماست و یکی انتقالی کپی‌شده‌ست."),
            new("brief-02", NarrativeSpeakerId.Yasin,
                "Inspect both marked manifest tables. The legitimate record follows our usual quantities, seals and delivery rhythm. The copy diverts cargo toward Relay-era storage sites.",
                "هر دو میز بارنامهٔ مشخص‌شده رو بررسی کنید. سند واقعی از مقدارها، مهرها و زمان‌بندی همیشگی ما پیروی می‌کنه. نسخهٔ کپی‌شده بار رو به انبارهای دورهٔ شبکه منحرف می‌کنه."),
            new("brief-03", NarrativeSpeakerId.Samira,
                "Compare first, then escort only the three legitimate relief trucks to the delivery yard. Defeat the armed cell without closing ordinary trade, and do not deliver the copied transfer.",
                "اول مقایسه کنید، بعد فقط سه کامیون امدادی واقعی رو به محوطهٔ تحویل برسونید. گروه مسلح رو بدون بستن دادوستد عادی شکست بدید و انتقال کپی‌شده رو تحویل ندید.")
        };

        public static readonly MarketLifelineNarrativeLine[] Comms =
        {
            new("comms-01", NarrativeSpeakerId.Yasin,
                "There—the copied manifest borrows a real route, but its quantities and seals do not match our trade. The three relief trucks are legitimate; the separate transfer leads to old storage sites around Relay infrastructure.",
                "همینه—بارنامهٔ کپی‌شده از یک مسیر واقعی استفاده می‌کنه، اما مقدارها و مهرها با دادوستد ما جور درنمیاد. سه کامیون امدادی واقعی‌ان؛ انتقال جداگانه به انبارهای قدیمی اطراف زیرساخت شبکه می‌رسه.")
        };

        public static readonly MarketLifelineNarrativeLine[] Debrief =
        {
            new("debrief-01", NarrativeSpeakerId.Yasin,
                "All three legitimate relief loads reached Old Market, and ordinary trade never stopped. You investigated the transfer without turning the market into the suspect.",
                "هر سه محمولهٔ امدادی واقعی به بازار قدیمی رسید و دادوستد عادی هم متوقف نشد. انتقال رو بررسی کردید، بدون اینکه خود بازار رو متهم کنید."),
            new("debrief-02", NarrativeSpeakerId.Samira,
                "Families can collect food and medicine without crossing a military cordon. The compromised manifest names several Relay-era storage sites; market workers will keep the real route moving.",
                "خانواده‌ها می‌تونن بدون عبور از محاصرهٔ نظامی غذا و دارو بگیرن. بارنامهٔ دستکاری‌شده چند انبار دورهٔ شبکه رو نام می‌بره؛ کارگرهای بازار مسیر واقعی رو باز نگه می‌دارن."),
            new("debrief-03", NarrativeSpeakerId.Aria,
                "Evidence correlation confirmed. One listed storage site has just lost power, displacing nearby families. The next operation is the power relay.",
                "تطبیق مدارک تأیید شد. برق یکی از انبارهای فهرست‌شده همین حالا قطع شده و خانواده‌های اطراف رو آواره کرده. عملیات بعدی، رلهٔ برقه.")
        };
    }
}
