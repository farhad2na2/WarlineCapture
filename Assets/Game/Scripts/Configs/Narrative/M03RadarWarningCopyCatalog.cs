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
                "شبکهٔ هشدار این بخش قطع است. دیده‌بان‌ها از یک کاروان زرهی در جادهٔ غربی خبر می‌دهند. خودروی رادار زمینی ما پس از ورود خودروها به محدودهٔ پوشش، تماس را تأیید می‌کند."),
            new("brief-02",NarrativeSpeakerId.Dalia,
                "Hold them at the junction or defend closer to the post. Keep the clinic corridor open. Even an unarmed carrier must not reach the inner core.",
                "در تقاطع جلوی آن‌ها را بگیرید یا نزدیک‌تر به پاسگاه دفاع کنید. مسیر درمانگاه را باز نگه دارید. حتی نفربر بی‌سلاح هم نباید به محدودهٔ مرکزی برسد."),
            new("brief-03",NarrativeSpeakerId.Aria,
                "We have roughly a minute to prepare. Two rifle squads and a working Barracks are ready. Choose a Tower, a Barrier, or reinforcements. The first warning is an estimate.",
                "حدود یک دقیقه برای آماده‌شدن فرصت داریم. دو گروه تفنگدار و یک پادگان آماده در اختیار شماست. برج، مانع یا نیروی کمکی را انتخاب کنید. هشدار نخست یک برآورد است.")
        };
        public static readonly M03NarrativeLine[] Comms =
        {
            new("comms-01",NarrativeSpeakerId.Aria,
                "The convoy's departure report matches the warning outage. The timing suggests advance knowledge. Keep defending; we will compare the records afterward.",
                "زمان حرکت کاروان در گزارش با زمان قطع شبکهٔ هشدار یکی است. این هم‌زمانی نشان از اطلاع قبلی دارد. به دفاع ادامه دهید؛ پس از نبرد سوابق را مقایسه می‌کنیم.")
        };
        public static readonly M03NarrativeLine[] Debrief =
        {
            new("debrief-01",NarrativeSpeakerId.Dalia,
                "The convoy is stopped and the post still stands. Check the damage and account for everyone. We need an accurate report.",
                "کاروان متوقف شد و پاسگاه پابرجاست. خسارت و وضعیت همه را بررسی کنید. به گزارشی دقیق نیاز داریم."),
            new("debrief-02",NarrativeSpeakerId.Aria,
                "The recovered orders name the warning outage before it happened. That is evidence of advance knowledge. It does not yet tell us who supplied it.",
                "در دستورهای بازیابی‌شده، قطع شبکهٔ هشدار پیش از وقوعش ثبت شده است. این مدرکِ اطلاع قبلی است، اما هنوز نمی‌دانیم چه کسی آن را داده است."),
            new("debrief-03",NarrativeSpeakerId.Samira,
                "A medical and engineering team is cut off beyond the road. They need an airlift. Prepare a safe route for them.",
                "یک تیم پزشکی و مهندسی آن سوی جاده گرفتار شده است. به تخلیهٔ هوایی نیاز دارند. مسیر امنی برای آن‌ها آماده کنید.")
        };
        public static readonly M03NarrativeLine[] DebriefOutcomes =
        {
            new("debrief-clean",NarrativeSpeakerId.Dalia,
                "The convoy is stopped. The post is undamaged, and every civilian is accounted for. Keep the clinic corridor open.",
                "کاروان متوقف شد. پاسگاه آسیب ندیده و همهٔ غیرنظامیان سالم هستند. مسیر درمانگاه را باز نگه دارید."),
            new("debrief-damaged",NarrativeSpeakerId.Dalia,
                "The convoy is stopped and every civilian is safe. The post took damage. Repair it before the next response.",
                "کاروان متوقف شد و همهٔ غیرنظامیان سالم هستند. پاسگاه آسیب دیده است. پیش از عملیات بعدی آن را تعمیر کنید."),
            new("debrief-civilian-loss",NarrativeSpeakerId.Dalia,
                "The convoy is stopped and the post is undamaged. We lost civilians. Record the losses and secure the clinic corridor.",
                "کاروان متوقف شد و پاسگاه آسیب ندیده است. غیرنظامیانی را از دست دادیم. تلفات را ثبت و مسیر درمانگاه را امن کنید."),
            new("debrief-mixed",NarrativeSpeakerId.Dalia,
                "The convoy is stopped, but the post took damage and civilians were lost. Record both. The corridor still needs protection.",
                "کاروان متوقف شد، اما پاسگاه آسیب دید و غیرنظامیانی را از دست دادیم. هر دو را ثبت کنید. مسیر همچنان به حفاظت نیاز دارد.")
        };
    }
}
