using Game.Catalog.Contracts;
namespace Game.Configs
{
    public readonly struct LastCorridorNarrativeLine
    {
        public readonly string Id, English, Persian;
        public readonly NarrativeSpeakerId Speaker;
        public string Key => "narrative.last_corridor." + Id.Substring("last_corridor-".Length);
        public LastCorridorNarrativeLine(string id, NarrativeSpeakerId speaker, string english, string persian)
        { Id="last_corridor-"+id; Speaker=speaker; English=english; Persian=persian; }
    }
    public static class CH05M04LastCorridorNarrativeCopy
    {
        public static readonly LastCorridorNarrativeLine[] Brief={
            new("brief-01",NarrativeSpeakerId.Dalia,"Fuel, medicine, the engineer, reinforcements and the physical authority keys must reach the city center. Samira and I set one plan: protect the relief vehicles and the military escort together. Clear the blockade before moving the column.","سوخت، دارو، مهندس، نیروهای کمکی و کلیدهای فیزیکی اختیار باید به مرکز شهر برسن. سمیرا و من یک برنامهٔ مشترک داریم؛ خودروهای امداد و اسکورت نظامی رو با هم حفظ کنین. پیش از حرکت ستون، راه‌بندان دشمن رو پاک‌سازی کنین."),
            new("brief-02",NarrativeSpeakerId.Samira,"Hold the original engineer at the repair marker for six seconds. Medicine takes the safer alternate lane. Fuel and the key APC may use the repaired main lane or the alternate. Follow each route’s marked entry, midpoint and exit, then hold at receiving for six seconds.","مهندس اصلی رو شش ثانیه روی نشان تعمیر نگه دارین. دارو از مسیر جایگزین امن‌تر می‌ره. سوخت و نفربر کلیدها می‌تونن از مسیر اصلی تعمیرشده یا مسیر جایگزین برن. ورودی، نقطهٔ میانی و خروجی مشخص‌شدهٔ هر مسیر رو دنبال کنین، بعد شش ثانیه در محل دریافت بمونین."),
            new("brief-03",NarrativeSpeakerId.Laila,"Karim and I are tracking the air approaches, but we cannot certify an air delivery through this threat picture. Keep every required load on the marked ground routes. Preserve the original trucks and key APC; a replacement cannot carry their custody record.","کریم و من مسیرهای هوایی رو زیر نظر داریم، ولی با این وضعیت تهدید نمی‌تونیم انتقال هوایی رو تأیید کنیم. همهٔ محموله‌های ضروری رو در مسیرهای زمینی مشخص‌شده نگه دارین. کامیون‌های اصلی و نفربر کلیدها رو حفظ کنین؛ خودروی جایگزین، سابقهٔ نگهداری اون‌ها رو حمل نمی‌کنه.")};
        public static readonly LastCorridorNarrativeLine[] Comms={
            new("comms-01",NarrativeSpeakerId.Samira,"The main lane is obstructed. The original engineer can restore its marked link with a six-second hold; the protected alternate remains connected. Keep medicine on the safer lane and choose the confirmed route for Fuel and the keys. This guidance does not move the convoy for you.","مسیر اصلی مسدوده. مهندس اصلی می‌تونه با شش ثانیه توقف، بخش مشخص‌شده رو تعمیر کنه؛ مسیر جایگزین محافظت‌شده همچنان وصله. دارو رو در مسیر امن‌تر نگه دارین و برای سوخت و کلیدها، مسیر تأییدشده رو انتخاب کنین. این راهنمایی، کاروان رو به جای شما حرکت نمی‌ده.")};
        public static readonly LastCorridorNarrativeLine[] Debrief={
            new("debrief-01",NarrativeSpeakerId.Dalia,"Medicine, Fuel and the original reinforcements reached the receiving points. The engineer and physical authority keys arrived in the original APC. All five delivery categories are accounted for, and the column reached the city center before the corridor closed.","دارو، سوخت و نیروهای کمکی اصلی به محل‌های دریافت رسیدن. مهندس و کلیدهای فیزیکی اختیار با نفربر اصلی رسیدن. هر پنج دستهٔ تحویل ثبت شدن و ستون، پیش از بسته شدن مسیر به مرکز شهر رسید."),
            new("debrief-02",NarrativeSpeakerId.Samira,"The repaired link and the protected alternate served the same city. Military and relief needs stayed in one plan. The medical teams, engineers, pilots and soldiers can now take their final roles without leaving Sahrin’s recovery behind.","بخش تعمیرشده و مسیر جایگزین محافظت‌شده، هر دو به یک شهر خدمت کردن. نیازهای نظامی و امدادی در یک برنامه موندن. تیم‌های پزشکی، مهندس‌ها، خلبان‌ها و سربازها حالا می‌تونن مسئولیت‌های نهاییشون رو بگیرن، بدون اینکه بازیابی سهرین رو کنار بذارن."),
            new("debrief-03",NarrativeSpeakerId.Aria,"The delivered keys preserve bounded access; they do not grant permanent unilateral authority. The Relay complex is still connected to power, clinics, shelters, transport and communications. Those city systems must survive the final approach. I will keep your decisions explicit.","کلیدهای تحویل‌شده، دسترسی محدود رو حفظ می‌کنن؛ اختیار دائمی یک‌طرفه نمی‌دن. مجموعهٔ رله هنوز به برق، درمانگاه‌ها، پناهگاه‌ها، حمل‌ونقل و ارتباطات وصله. این سامانه‌های شهری باید از نزدیک شدن نهایی سالم عبور کنن. تصمیم‌های شما رو صریح نگه می‌دارم.")};
    }
}
