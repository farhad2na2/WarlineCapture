using Game.Catalog.Contracts;
namespace Game.Configs
{
    public readonly struct SignalTraceNarrativeLine
    {
        public readonly string Id,Key,English,Persian;public readonly NarrativeSpeakerId Speaker;
        public SignalTraceNarrativeLine(string id,NarrativeSpeakerId speaker,string english,string persian){Id="signal_trace-"+id;Key="narrative.signal_trace."+id.Replace('-', '.');Speaker=speaker;English=english;Persian=persian;}
    }
    public static class CH03M01SignalTraceCopy
    {
        public static readonly SignalTraceNarrativeLine[] Brief={
            new("brief-01",NarrativeSpeakerId.Aria,"Three moving transmitters share the Relay signature. One belongs to the clinic, one to a delivery cooperative, and one is travelling with an armed escort. Proximity alone is not proof.","سه فرستندهٔ متحرک امضای شبکه رو دارن. یکی مال درمانگاهه، یکی مال تعاونی پخشه و یکی با اسکورت مسلح حرکت می‌کنه. فقط نزدیک بودن، مدرک نیست."),
            new("brief-02",NarrativeSpeakerId.Samira,"The clinic and delivery crews use radios the old registry never recorded. Keep those signals protected. Compare the two observation points before you identify the carrier.","درمانگاه و گروه‌های پخش از بی‌سیم‌هایی استفاده می‌کنن که هیچ‌وقت توی فهرست قدیمی ثبت نشدن. اون سیگنال‌ها باید امن بمونن. قبل از شناسایی حامل، هر دو نقطهٔ دیده‌بانی رو بررسی کن."),
            new("brief-03",NarrativeSpeakerId.Dalia,"We wait for corroboration, then isolate the armed escort. No fire near the protected transmitters. Recover the device intact.","تا تأیید دوم صبر می‌کنیم، بعد اسکورت مسلح رو جدا می‌کنیم. نزدیک فرستنده‌های حفاظت‌شده شلیک نکن. دستگاه رو سالم تحویل بگیر.")};
        public static readonly SignalTraceNarrativeLine[] Comms={
            new("comms-01",NarrativeSpeakerId.Aria,"Evidence correlated. The escorted transmitter crossed a restricted service tunnel and changed routes with the armed patrol. Confidence is now confirmed. The clinic and delivery signals remain protected.","مدارک با هم جور شد. فرستندهٔ اسکورت‌شده از تونل خدماتی محدود رد شد و همراه گشت مسلح مسیر عوض کرد. حالا هدف قطعی تأیید شده. سیگنال درمانگاه و پخش همچنان حفاظت‌شده‌ان.")};
        public static readonly SignalTraceNarrativeLine[] Debrief={
            new("debrief-01",NarrativeSpeakerId.Dalia,"The escort is down and the device is intact. We stopped the cell without turning the district's radios into targets.","اسکورت از کار افتاد و دستگاه سالمه. سلول رو متوقف کردیم، بدون اینکه بی‌سیم‌های محله رو هدف کنیم."),
            new("debrief-02",NarrativeSpeakerId.Samira,"The clinic never lost contact and the delivery cooperative kept moving. People saw us verify first and act only on the armed carrier.","ارتباط درمانگاه حتی یک لحظه قطع نشد و تعاونی پخش هم به کارش ادامه داد. مردم دیدن که اول بررسی کردیم و فقط سراغ حامل مسلح رفتیم."),
            new("debrief-03",NarrativeSpeakerId.Aria,"The recovered device uses a Civic Relay routing signature known only to continuity planners. Its final route points to a verified weapons safehouse beside occupied homes.","دستگاه بازیابی‌شده از امضای مسیریابی شبکهٔ شهری استفاده می‌کنه؛ امضایی که فقط برنامه‌ریزهای تداوم فرمان می‌شناختن. مسیر آخرش به یک مخفیگاه تأییدشدهٔ سلاح کنار خانه‌های مسکونی می‌رسه.")};
    }
}
