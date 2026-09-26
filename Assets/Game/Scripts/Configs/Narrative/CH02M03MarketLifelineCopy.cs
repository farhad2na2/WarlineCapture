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
            new("brief-01", NarrativeSpeakerId.Samira,
                "These stalls feed the district, but three relief trucks are stranded outside the market. Keep trade open while you bring them to the marked delivery yard.",
                "این غرفه‌ها غذای محله رو تأمین می‌کنن، اما سه کامیون امدادی بیرون بازار گیر افتادن. مسیر دادوستد رو باز نگه دارید و کامیون‌ها رو به محوطهٔ تحویل مشخص‌شده برسونید."),
            new("brief-02", NarrativeSpeakerId.Samira,
                "The market is not the enemy. Escort all three trucks, defeat the armed cell, then move one rifle squad to the manifest table and hold there for six seconds to verify the transfer.",
                "بازار دشمن نیست. هر سه کامیون رو اسکورت کنید، گروه مسلح رو شکست بدید، بعد یک گروه تفنگدار رو به میز بارنامه ببرید و شش ثانیه همون‌جا نگه دارید تا انتقال بررسی بشه."),
            new("brief-03", NarrativeSpeakerId.Dalia,
                "Use Select, Move, Attack and Hold—the same field commands as before. No search action is needed. Once the manifest is verified, keep the delivery yard secure for ten seconds.",
                "از همون فرمان‌های قبلی انتخاب، حرکت، حمله و توقف استفاده کنید. فرمان جست‌وجوی جداگانه‌ای لازم نیست. وقتی بارنامه تأیید شد، محوطهٔ تحویل رو ده ثانیه امن نگه دارید.")
        };

        public static readonly MarketLifelineNarrativeLine[] Comms =
        {
            new("comms-01", NarrativeSpeakerId.Samira,
                "That manifest copies a real market route, but the quantities and seals do not match our trade. The false transfer leads to old storage sites around Relay infrastructure.",
                "این بارنامه از یک مسیر واقعی بازار کپی شده، اما مقدارها و مهرها با دادوستد ما جور درنمیاد. انتقال جعلی به انبارهای قدیمی اطراف زیرساخت شبکه می‌رسه.")
        };

        public static readonly MarketLifelineNarrativeLine[] Debrief =
        {
            new("debrief-01", NarrativeSpeakerId.Samira,
                "All three relief loads reached Old Market, and legitimate trade never stopped. Families can collect food and medicine without crossing a military cordon.",
                "هر سه محمولهٔ امدادی به بازار قدیمی رسید و دادوستد عادی هم متوقف نشد. خانواده‌ها می‌تونن بدون عبور از محاصرهٔ نظامی غذا و دارو بگیرن."),
            new("debrief-02", NarrativeSpeakerId.Dalia,
                "This is the compromised transfer. The same false seals appear at several Relay-era storage sites. Take the record; the market workers will keep the real route moving.",
                "این هم انتقال دستکاری‌شده. همین مهرهای جعلی توی چند انبار قدیمی شبکه دیده می‌شن. مدرک رو ببرید؛ کارگرهای بازار مسیر واقعی رو باز نگه می‌دارن."),
            new("debrief-03", NarrativeSpeakerId.Aria,
                "Evidence correlation confirmed. One listed storage site has just lost power, displacing nearby families. The next operation is the power relay.",
                "تطبیق مدارک تأیید شد. برق یکی از انبارهای فهرست‌شده همین حالا قطع شده و خانواده‌های اطراف رو آواره کرده. عملیات بعدی، رلهٔ برقه.")
        };
    }
}
