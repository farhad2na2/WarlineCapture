using Game.Catalog.Contracts;

namespace Game.Configs
{
    public readonly struct AirCorridorNarrativeLine
    {
        public readonly string Id, English, Persian;
        public readonly NarrativeSpeakerId Speaker;
        public string Key => "narrative.air_corridor." + Id.Substring("air_corridor-".Length);
        public AirCorridorNarrativeLine(string id, NarrativeSpeakerId speaker, string english, string persian)
        { Id="air_corridor-"+id; Speaker=speaker; English=english; Persian=persian; }
    }
    public static class CH04M01AirCorridorCopy
    {
        public static readonly AirCorridorNarrativeLine[] Brief = {
            new("brief-laila", NarrativeSpeakerId.Laila, "Those formations aren't Ash Line improvisation. Vanguard aircraft are approaching the relief corridor. Keep our radar and launchers covered so the medical flights can get through.", "این آرایش پرواز کار خط خاکستر نیست. هواگردهای ونگارد دارن به مسیر امداد نزدیک می‌شن. رادار و پرتابگرها رو پوشش بدین تا پروازهای پزشکی رد بشن."),
            new("brief-aria", NarrativeSpeakerId.Aria, "Confirmed hostile aircraft approach from the west, followed by a northern strike. Keep the launchers near the radar. They acquire and fire automatically; use normal movement to manage their coverage.", "هواگردهای دشمن از غرب نزدیک می‌شن؛ حملهٔ بعدی از شماله. پرتابگرها رو نزدیک رادار نگه دار. خودشون هدف رو می‌گیرن و شلیک می‌کنن؛ با فرمان معمول حرکت، پوشششون رو تنظیم کن.")
        };
        public static readonly AirCorridorNarrativeLine[] Comms = {
            new("comms-laila", NarrativeSpeakerId.Laila, "The western probe is breaking up. Don't chase it. The main formation is turning in from the north, and our relief flights still need this corridor.", "حملهٔ غربی داره از هم می‌پاشه. دنبالشون نرین. آرایش اصلی از شمال داره می‌پیچه؛ پروازهای امداد هنوز به این مسیر نیاز دارن."),
            new("comms-aria", NarrativeSpeakerId.Aria, "The next tracks are confirmed Vanguard attackers. Radar support improves range and tracking, but a displaced launcher can lose that support. Protect the radar and keep the coverage linked.", "ردهای بعدی، مهاجم‌های تأییدشدهٔ ونگاردن. پشتیبانی رادار برد و دقت رو بهتر می‌کنه، ولی پرتابگری که دور بشه اون پشتیبانی رو از دست می‌ده. رادار رو حفظ کن و پوشش رو بهش وصل نگه دار.")
        };
        public static readonly AirCorridorNarrativeLine[] Debrief = {
            new("debrief-laila", NarrativeSpeakerId.Laila, "The medical cargo and evacuees are through. These pilots flew a coordinated military plan. Vanguard isn't just supplying Qassem anymore; it's moving to take the Relay.", "محمولهٔ پزشکی و تخلیه‌شده‌ها رد شدن. این خلبان‌ها با یک نقشهٔ نظامی هماهنگ پرواز می‌کردن. ونگارد دیگه فقط به قاسم تجهیزات نمی‌ده؛ اومده رله رو تصرف کنه."),
            new("debrief-aria", NarrativeSpeakerId.Aria, "Recovered flight data confirms that assessment. Armored columns are now approaching the fuel reserves. The corridor is open, but the next defense will be on the ground.", "داده‌های پروازِ بازیابی‌شده این گزارش رو تأیید می‌کنه. ستون‌های زرهی دارن به ذخیره‌های سوخت نزدیک می‌شن. مسیر هوایی بازه، ولی دفاع بعدی روی زمینه.")
        };
    }
}
