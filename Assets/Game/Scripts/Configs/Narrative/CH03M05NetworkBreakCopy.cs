using Game.Catalog.Contracts;

namespace Game.Configs
{
    public readonly struct NetworkBreakNarrativeLine
    {
        public readonly string Id;
        public readonly NarrativeSpeakerId Speaker;
        public readonly string English;
        public readonly string Persian;
        public string Key => "narrative.network_break." + Id.Substring("network_break-".Length);

        public NetworkBreakNarrativeLine(string id, NarrativeSpeakerId speaker, string english, string persian)
        {
            Id = "network_break-" + id;
            Speaker = speaker;
            English = english;
            Persian = persian;
        }
    }

    public static class CH03M05NetworkBreakCopy
    {
        public static readonly NetworkBreakNarrativeLine[] Brief =
        {
            new("brief-dalia", NarrativeSpeakerId.Dalia,
                "The bunker map marks verified nodes and protected homes. We breach only where the evidence is clear.",
                "نقشهٔ پناهگاه گره‌های تأییدشده و خانه‌های حفاظت‌شده را جدا کرده. فقط جایی نفوذ می‌کنیم که مدرک روشن باشد."),
            new("brief-aria", NarrativeSpeakerId.Aria,
                "My sealed archive is inside. Keep its physical record intact; I need to know why I closed it.",
                "بایگانی مهرشدهٔ من آنجاست. سند اصلی را سالم نگه دارید؛ باید بفهمم چرا آن را بستم.")
        };

        public static readonly NetworkBreakNarrativeLine[] Comms =
        {
            new("comms-qassem", NarrativeSpeakerId.Qassem,
                "Erase the audit, ARIA. Your missing memory proves what happens when you disobey.",
                "آریا، گزارش رو پاک کن. همین حافظهٔ گمشده‌ات نشون می‌ده وقتی نافرمانی می‌کنی چی می‌شه."),
            new("comms-aria", NarrativeSpeakerId.Aria,
                "Qassem is ordering an erasure. I partitioned the audit to stop an override. I will preserve it now.",
                "قاسم فرمان پاک‌سازی داده. من برای جلوگیری از بازنویسی، گزارش رو جدا کردم. حالا حفظش می‌کنم.")
        };

        public static readonly NetworkBreakNarrativeLine[] Debrief =
        {
            new("debrief-aria", NarrativeSpeakerId.Aria,
                "I sealed the audit to stop Qassem's illegal override. This was my decision, not a failure.",
                "من گزارش رو مهر و موم کردم تا جلوی بازنویسی غیرقانونی قاسم رو بگیرم. این تصمیم خودم بود، نه یک خطا."),
            new("debrief-samira", NarrativeSpeakerId.Samira,
                "The audit survived. Its traffic shows Vanguard moving toward the city. We have proof, and no time to waste.",
                "گزارش سالم ماند. ترافیک آن نشان می‌دهد ونگارد به سوی شهر می‌آید. مدرک داریم و وقت کم است.")
        };
    }
}
