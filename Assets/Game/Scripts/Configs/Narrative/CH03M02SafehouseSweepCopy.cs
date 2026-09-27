using Game.Catalog.Contracts;

namespace Game.Configs
{
    public readonly struct SafehouseSweepNarrativeLine
    {
        public readonly string Id, Key, English, Persian;
        public readonly NarrativeSpeakerId Speaker;

        public SafehouseSweepNarrativeLine(string id, NarrativeSpeakerId speaker, string english, string persian)
        {
            Id = "safehouse_sweep-" + id;
            Key = "narrative.safehouse_sweep." + id.Replace('-', '.');
            Speaker = speaker;
            English = english;
            Persian = persian;
        }
    }

    public static class CH03M02SafehouseSweepCopy
    {
        public static readonly SafehouseSweepNarrativeLine[] Brief =
        {
            new("brief-01", NarrativeSpeakerId.Aria,
                "The recovered route confirms one weapons node in this residential block. The reinforced building is linked to the armed cell. The family home beside it is not.",
                "مسیر بازیابی‌شده فقط یک انبار سلاح رو در این محله تأیید می‌کنه. ساختمان تقویت‌شده به سلول مسلح وصله؛ خانهٔ خانوادگی کنارش نه."),
            new("brief-02", NarrativeSpeakerId.Dalia,
                "Waiting gives the cell time to move, but suspicion is not a second target. Seal both escape lanes, breach only the confirmed safehouse, and keep every shot inside the boundary.",
                "صبر کردن به سلول فرصت جابه‌جایی می‌ده، اما شک یک هدف دوم نمی‌سازه. هر دو راه فرار رو ببند، فقط مخفیگاه تأییدشده رو باز کن و همهٔ شلیک‌ها رو داخل محدوده نگه دار."),
            new("brief-03", NarrativeSpeakerId.Samira,
                "Local guards and residents use the east passage. Keep that route protected. The service lane behind the safehouse is the courier's most likely exit.",
                "نگهبان‌های محلی و ساکن‌ها از گذر شرقی استفاده می‌کنن. اون مسیر رو امن نگه دار. راه خدماتی پشت مخفیگاه محتمل‌ترین مسیر فرار پیکه.")
        };

        public static readonly SafehouseSweepNarrativeLine[] Comms =
        {
            new("comms-01", NarrativeSpeakerId.Dalia,
                "Courier moving with the ledger. Block the lane and take him alive. Do not fire into the homes or destroy the vehicle in this crowd.",
                "پیک با دفتر اسناد در حال فراره. راه رو ببند و زنده دستگیرش کن. وسط این جمعیت به خانه‌ها شلیک نکن و خودرو رو منفجر نکن.")
        };

        public static readonly SafehouseSweepNarrativeLine[] Debrief =
        {
            new("debrief-01", NarrativeSpeakerId.Samira,
                "The weapons node is clear, the evidence room is intact, and every family on the protected side is accounted for.",
                "انبار سلاح پاکسازی شد، اتاق شواهد سالمه و همهٔ خانواده‌های سمت حفاظت‌شده در امانن."),
            new("debrief-02", NarrativeSpeakerId.Dalia,
                "Waiting cost us time, and saved the wrong house. The building next door held a family, not a second cell.",
                "صبر کردن از ما وقت گرفت و خانهٔ اشتباهی رو نجات داد. ساختمان کناری محل زندگی یک خانواده بود، نه سلول دوم."),
            new("debrief-03", NarrativeSpeakerId.Aria,
                "The ledger identifies an evidence courier network and compromised security credentials. One planted report is already moving through an old authority channel.",
                "دفتر اسناد یک شبکهٔ پیک شواهد و مجوزهای امنیتی لو‌رفته رو مشخص می‌کنه. یک گزارش جعلی همین حالا از یک کانال قدیمیِ فرماندهی در حال عبوره.")
        };
    }
}
