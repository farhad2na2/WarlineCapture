using Game.Catalog.Contracts;
namespace Game.Configs
{
    public readonly struct GridlockNarrativeLine
    {
        public readonly string Id,Key,English,Persian;
        public readonly NarrativeSpeakerId Speaker;
        public GridlockNarrativeLine(string id,NarrativeSpeakerId speaker,string english,string persian)
        {Id="gridlock-"+id;Key="narrative.gridlock."+id.Replace('-','.');Speaker=speaker;English=english;Persian=persian;}
    }
    public static class CH02M01GridlockNarrativeCopy
    {
        public static readonly GridlockNarrativeLine[] Opening={
            new("opening-01",NarrativeSpeakerId.Samira,"The city survived the first attacks. But a quiet street is not a working city. Relief trucks are trapped behind blocked roads and dark intersections.","شهر از حمله‌های اول جون سالم به در برد. ولی خیابون ساکت یعنی شهر راه افتاده؟ نه. کامیون‌های امداد پشت راه‌های بسته و چهارراه‌های خاموش گیر کردن."),
            new("opening-02",NarrativeSpeakerId.Dalia,"The clinic's generator is running low. Its Fuel is on the other side of a damaged corridor. Keeping the ward open starts with getting that road back.","بنزین ژنراتور درمانگاه داره تموم می‌شه. محموله اون طرف مسیر آسیب‌دیده گیر کرده. برای باز موندن بخش درمان، اول باید راه رو باز کنیم."),
            new("opening-03",NarrativeSpeakerId.Samira,"Roads, Fuel, markets, power, shelters—and the people who keep them working. Lose one link and the others begin to fail. We have to restore them together.","راه، بنزین، بازار، برق، پناهگاه... و آدم‌هایی که این‌ها رو سر پا نگه می‌دارن. یکی از این‌ها که از کار بیفته، بقیه هم ضربه می‌خورن. باید به هم وصلشون کنیم."),
            new("opening-04",NarrativeSpeakerId.Aria,"My map covers the formal road network. Fadi knows service lanes and local connections it does not show. We will need both kinds of knowledge.","نقشهٔ من شبکهٔ رسمی جاده‌ها رو پوشش می‌ده. فادی مسیرهای فرعی و راه‌های محلی‌ای رو می‌شناسه که توی نقشه نیستن. به هر دو جور اطلاعات نیاز داریم."),
            new("opening-05",NarrativeSpeakerId.Dalia,"Commander, restore the lifelines. Protect the crews doing the work. And watch which roads the saboteurs leave open—they may be choosing where we go next.","فرمانده، راه‌های حیاتی رو دوباره راه بنداز. مراقب گروه‌هایی باش که دارن کار می‌کنن. حواست هم به راه‌هایی باش که خرابکارها باز گذاشتن؛ شاید دارن مسیر بعدی ما رو تعیین می‌کنن.")};
        public static readonly GridlockNarrativeLine[] Brief={
            new("brief-01",NarrativeSpeakerId.Samira,"The hospital still has patients, but its supply road is blocked. That truck carries the relief they need. We must get it through.","بیمارستان هنوز پر از مریضه، ولی راه تدارکاتش بسته‌ست. امدادی که لازم دارن توی اون کامیونه. باید برسونیمش."),
            new("brief-02",NarrativeSpeakerId.Fadi,"Fadi here. The main road is gone, but we kept a service lane open for years. Two obstructions are all that stand between us and the hospital. Protect my crew while we clear them.","فادی هستم. جادهٔ اصلی از بین رفته، ولی ما سال‌ها از این مسیر فرعی استفاده می‌کردیم. فقط دو تا مانع بین ما و بیمارستانه. تا بازشون می‌کنیم، مراقب گروهم باشین."),
            new("brief-03",NarrativeSpeakerId.Dalia,"Rifles first; workers behind cover. Keep Fadi and at least one worker at each site for twenty-five seconds of safe work. Then protect the truck and hold the hospital clear for twenty seconds. We have twelve minutes.","تفنگدارها جلو برن؛ کارگرها پشت پوشش بمونن. فادی و حداقل یک کارگر باید توی هر محل بیست‌وپنج ثانیه امن کار کنن. بعد از کامیون محافظت کنید و بیمارستان رو بیست ثانیه امن نگه دارید. دوازده دقیقه وقت داریم.")};
        public static readonly GridlockNarrativeLine[] Comms={
            new("comms-01",NarrativeSpeakerId.Aria,"Fadi's service lane is usable. It was missing from my map. I have updated the route; local knowledge matters here.","مسیر فرعی فادی قابل استفاده‌ست. توی نقشهٔ من نبود. مسیر رو به‌روز کردم؛ اینجا شناخت محلی مهمه.")};
        public static readonly GridlockNarrativeLine[] Debrief={
            new("debrief-01",NarrativeSpeakerId.Samira,"The relief truck has reached the hospital. The ward can keep working. We opened a route people can depend on.","کامیون امداد به بیمارستان رسید. بخش درمان می‌تونه به کارش ادامه بده. راهی باز کردیم که مردم می‌تونن روش حساب کنن."),
            new("debrief-02",NarrativeSpeakerId.Aria,"The obstructions form a pattern. They steer traffic toward a dormant substation. I cannot yet tell why that route was preserved.","مانع‌ها یه الگو دارن. ترافیک رو به سمت یک پست برق خاموش هدایت می‌کنن. هنوز معلوم نیست چرا اون مسیر رو باز نگه داشتن."),
            new("debrief-03",NarrativeSpeakerId.Dalia,"The road is open. Next we need to keep supplies moving, and Fuel is running short. Return to the campaign map to review the operation.","راه باز شده. حالا باید جریان تدارکات رو حفظ کنیم، اما بنزین داره کم میاد. برای دیدن نتیجه برگرد به نقشهٔ کارزار.")};
    }
}
