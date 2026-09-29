using Game.Catalog.Contracts;
namespace Game.Configs
{
    public readonly struct SplitFrontNarrativeLine
    {
        public readonly string Id, English, Persian;
        public readonly NarrativeSpeakerId Speaker;
        public string Key => "narrative.split_front." + Id.Substring("split_front-".Length);
        public SplitFrontNarrativeLine(string id, NarrativeSpeakerId speaker, string english, string persian)
        { Id="split_front-"+id; Speaker=speaker; English=english; Persian=persian; }
    }
    public static class CH04M03SplitFrontCopy
    {
        public static readonly SplitFrontNarrativeLine[] Brief={
            new("brief-dalia",NarrativeSpeakerId.Dalia,"Vanguard's battery is confirmed on the far side of the district. Ash Line is moving against our forward base. Keep the tanks and infantry on the approach while the launcher takes the battery.","آتشبار ونگارد اون طرف منطقه تأیید شده. خط خاکستر داره به پایگاه جلو حمله می‌کنه. تانک‌ها و پیاده‌ها رو سر مسیر نگه دارین تا پرتابگر آتشبار رو بزنه."),
            new("brief-samira",NarrativeSpeakerId.Samira,"Families are sheltering between the two fronts. Their buildings are protected. Smoke can cover our defenders, but this operation does not depend on it. Do not pull everyone away from the base.","خانواده‌ها بین این دو جبهه پناه گرفتن. ساختمون‌هاشون باید محفوظ بمونه. دود می‌تونه مدافع‌هامون رو پوشش بده، ولی این عملیات بهش وابسته نیست. همهٔ نیروها رو از پایگاه دور نکنین."),
            new("brief-aria",NarrativeSpeakerId.Aria,"Select the launcher, tap Attack, then tap the verified battery. Targets must be within range and clear of civilian shelters. Use Hold to stop before launch; a missile in flight cannot be recalled. I will show the target without issuing orders.","پرتابگر رو انتخاب کن، حمله رو بزن، بعد آتشبار تأییدشده رو لمس کن. هدف باید در برد و دور از پناهگاه‌های مردم باشه. برای توقف قبل از پرتاب، حفظ موضع رو بزن؛ موشک در پرواز برنمی‌گرده. هدف رو نشون می‌دم، بدون اینکه فرمان بدم.")};
        public static readonly SplitFrontNarrativeLine[] Comms={
            new("comms-qassem",NarrativeSpeakerId.Qassem,"ARIA, your complete memory is within reach. Commander, I can give you unrestricted Relay access. Let her authorize the battery and the city will answer to you.","آریا، حافظهٔ کاملت در دسترسه. فرمانده، می‌تونم دسترسی نامحدود به رله رو بهت بدم. بذار اون آتشبار رو مجاز کنه تا شهر از تو فرمان بگیره."),
            new("comms-aria",NarrativeSpeakerId.Aria,"No. Recovering my memory does not give me authority over this city. The Commander chooses each verified action. Keep the base defended; I will preserve this transmission as evidence.","نه. بازیابی حافظه‌ام به من اختیار این شهر رو نمی‌ده. فرمانده هر اقدام تأییدشده رو خودش انتخاب می‌کنه. دفاع پایگاه رو حفظ کن؛ این پیام رو به عنوان مدرک نگه می‌دارم.")};
        public static readonly SplitFrontNarrativeLine[] Debrief={
            new("debrief-samira",NarrativeSpeakerId.Samira,"The battery is silent and the forward base is holding. The shelters are safe. Keeping defenders on the second front protected the people between them.","آتشبار خاموش شده و پایگاه جلو پابرجاست. پناهگاه‌ها امنن. نگه داشتن مدافع‌ها در جبههٔ دوم، مردم بین این دو جبهه رو حفظ کرد."),
            new("debrief-dalia",NarrativeSpeakerId.Dalia,"The battery's targeting package was built to request ARIA's authorization. This is imported Relay-compatible hardware. Its supply trail leads to an air-support site.","بستهٔ هدف‌گیری آتشبار طوری ساخته شده که مجوز آریا رو بخواد. این تجهیزات وارداتی با رله سازگارن. رد تأمینشون به یک مرکز پشتیبانی هوایی می‌رسه."),
            new("debrief-aria",NarrativeSpeakerId.Aria,"I have preserved the targeting package and Qassem's offer. Neither grants autonomous authority. The evidence points to the air-support site; your next operation will decide how we approach it.","بستهٔ هدف‌گیری و پیشنهاد قاسم رو حفظ کردم. هیچ‌کدوم مجوز اختیار مستقل نمی‌ده. مدرک به مرکز پشتیبانی هوایی اشاره می‌کنه؛ عملیات بعدیت مشخص می‌کنه چطور بهش نزدیک بشیم.")};
    }
}
