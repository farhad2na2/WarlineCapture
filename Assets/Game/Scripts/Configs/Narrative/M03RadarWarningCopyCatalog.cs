using Game.Catalog.Contracts;

namespace Game.Configs
{
    public readonly struct M03NarrativeLine
    {
        public readonly string Id, Key, English, Persian;
        public readonly NarrativeSpeakerId Speaker;
        public M03NarrativeLine(string id, NarrativeSpeakerId speaker, string english, string persian)
        { Id = "m03-"+id; Key = "narrative.m03."+id.Replace('-','.'); Speaker = speaker; English = english; Persian = persian; }
    }

    public static class M03RadarWarningCopyCatalog
    {
        public static readonly M03NarrativeLine[] Brief =
        {
            new("brief-01",NarrativeSpeakerId.Aria,
                "The warning sector is offline. Scouts report an armored convoy on the western road. Our Ground Radar Tank can confirm vehicles once they enter coverage.",
                "شبکهٔ هشدار اینجا قطع شده. دیده‌بان‌ها می‌گن یه کاروان زرهی توی جادهٔ غربیه. وقتی خودروها به محدودهٔ رادارمون برسن، می‌تونیم حضورشون رو تأیید کنیم."),
            new("brief-02",NarrativeSpeakerId.Dalia,
                "Hold them at the junction or defend closer to the post. Keep the clinic corridor open. Even an unarmed carrier must not reach the inner core.",
                "جلوی کاروان رو توی تقاطع بگیرید؛ یا نزدیک‌تر به پاسگاه دفاع کنید. راه درمانگاه باید باز بمونه. حتی نفربر بی‌سلاح هم نباید به محدودهٔ مرکزی برسه."),
            new("brief-03",NarrativeSpeakerId.Aria,
                "We have roughly a minute to prepare. Two rifle squads and a working Barracks are ready. Choose a Tower, a Barrier, or reinforcements. The first warning is an estimate.",
                "حدود یه دقیقه وقت داریم آماده بشیم. دو گروه تفنگدار و یه سربازخانهٔ آماده داری. خودت انتخاب کن: برج، مانع جاده یا نیروی کمکی. هشدار اول فقط یه تخمینه.")
        };
        public static readonly M03NarrativeLine[] Comms =
        {
            new("comms-01",NarrativeSpeakerId.Aria,
                "The convoy's departure report matches the warning outage. The timing suggests advance knowledge. Keep defending; we will compare the records afterward.",
                "طبق گزارش، کاروان درست موقع قطع شبکهٔ هشدار راه افتاده. انگار از قبل خبر داشتن. فعلاً دفاع رو ادامه بده؛ بعد از نبرد، گزارش‌ها رو کنار هم می‌ذاریم.")
        };
        public static readonly M03NarrativeLine[] Debrief =
        {
            new("debrief-01",NarrativeSpeakerId.Dalia,
                "The convoy is stopped and the post still stands. Check the damage and account for everyone. We need an accurate report.",
                "کاروان متوقف شد، پاسگاه هم سر جاشه. ببینید چقدر آسیب دیدیم و همه چه وضعی دارن. گزارش دقیق می‌خوام."),
            new("debrief-02",NarrativeSpeakerId.Aria,
                "The recovered orders name the warning outage before it happened. That is evidence of advance knowledge. It does not yet tell us who supplied it.",
                "توی دستورهایی که پیدا کردیم، قطع شبکهٔ هشدار از قبل ثبت شده. پس ازش خبر داشتن. هنوز نمی‌دونیم کی این اطلاعات رو داده."),
            new("debrief-03",NarrativeSpeakerId.Samira,
                "A medical and engineering team is cut off beyond the road. They need an airlift. Prepare a safe route for them.",
                "یه تیم پزشکی و مهندسی اون‌طرف جاده گیر افتاده. باید با بالگرد بیاریمشون بیرون. یه مسیر امن براشون آماده کنید.")
        };
        public static readonly M03NarrativeLine[] DebriefOutcomes =
        {
            new("debrief-clean",NarrativeSpeakerId.Dalia,
                "The convoy is stopped. The post is undamaged, and every civilian is accounted for. Keep the clinic corridor open.",
                "کاروان متوقف شد. پاسگاه آسیبی ندیده و همهٔ مردم سالم‌ان. راه درمانگاه رو باز نگه دارید."),
            new("debrief-damaged",NarrativeSpeakerId.Dalia,
                "The convoy is stopped and every civilian is safe. The post took damage. Repair it before the next response.",
                "کاروان متوقف شد و همهٔ مردم سالم‌ان. پاسگاه آسیب دیده؛ قبل از عملیات بعدی تعمیرش کنید."),
            new("debrief-civilian-loss",NarrativeSpeakerId.Dalia,
                "The convoy is stopped and the post is undamaged. We lost civilians. Record the losses and secure the clinic corridor.",
                "کاروان متوقف شد و پاسگاه سالمه، ولی چند نفر از مردم رو از دست دادیم. تلفات رو ثبت کنید و راه درمانگاه رو امن نگه دارید."),
            new("debrief-mixed",NarrativeSpeakerId.Dalia,
                "The convoy is stopped, but the post took damage and civilians were lost. Record both. The corridor still needs protection.",
                "کاروان متوقف شد، ولی پاسگاه آسیب دید و چند نفر از مردم رو از دست دادیم. هر دو رو ثبت کنید. هنوز باید از جاده محافظت کنیم.")
        };
    }
}
