using Game.Catalog.Contracts;
namespace Game.Configs
{
    public readonly struct GroundedSignalNarrativeLine
    {
        public readonly string Id, English, Persian;
        public readonly NarrativeSpeakerId Speaker;
        public string Key => "narrative.grounded_signal." + Id.Substring("grounded_signal-".Length);
        public GroundedSignalNarrativeLine(string id, NarrativeSpeakerId speaker, string english, string persian)
        { Id="grounded_signal-"+id; Speaker=speaker; English=english; Persian=persian; }
    }
    public static class CH04M04GroundedSignalCopy
    {
        public static readonly GroundedSignalNarrativeLine[] Brief={
            new("brief-01",NarrativeSpeakerId.Laila,"The military relay sits beside the civilian airfield. Disable the relay, recover its control hardware, and bring both specialists home. The terminal must stay intact.","رلهٔ نظامی کنار فرودگاه غیرنظامیه. رله رو از کار بنداز، تجهیزات کنترلش رو بردار و هر دو متخصص رو سالم برگردون. ترمینال باید سالم بمونه."),
            new("brief-02",NarrativeSpeakerId.Karim,"We'll unload at the apron and leave by APC. Keep the runway clear. Use the service gate to reach the compound, and protect our way back.","توی محوطهٔ توقف پیاده می‌شیم و با نفربر برمی‌گردیم. باند رو باز نگه دار. از درِ خدماتی وارد محوطه شو و مسیر برگشتمون رو حفظ کن."),
            new("brief-03",NarrativeSpeakerId.Yusuf,"I need the control hardware, not rubble. Once the military relay is disabled, get us to the recovery point. We'll verify and secure the device there.","تجهیزات کنترل رو می‌خوام، نه آوارش رو. وقتی رلهٔ نظامی از کار افتاد، ما رو به محل بازیابی برسون. همون‌جا دستگاه رو بررسی و تحویل می‌گیریم.")};
        public static readonly GroundedSignalNarrativeLine[] Comms={
            new("comms-01",NarrativeSpeakerId.Yusuf,"The interfaces match the Civic Relay specifications. This equipment was prepared for our network. We still need the physical unit to prove it.","اتصال‌ها با مشخصات رلهٔ شهری یکیه. این تجهیزات رو برای شبکهٔ ما آماده کردن. برای اثباتش هنوز باید خودِ دستگاه رو برداریم."),
            new("comms-02",NarrativeSpeakerId.Laila,"Vanguard knows you're there. Keep the specialists together and the service road open. Recover the hardware, board the APC, then reach the guarded exit.","ونگارد فهمیده اونجایین. متخصص‌ها رو کنار هم نگه دار و جادهٔ خدماتی رو باز بذار. تجهیزات رو بردار، سوار نفربر شو و به خروجی محافظت‌شده برس.")};
        public static readonly GroundedSignalNarrativeLine[] Debrief={
            new("debrief-01",NarrativeSpeakerId.Karim,"Both specialists and the hardware are back. The civilian terminal is intact. The service route held long enough to get everyone out.","هر دو متخصص و تجهیزات برگشتن. ترمینال غیرنظامی هم سالمه. مسیر خدماتی تا وقتی همه رو خارج کردیم باز موند."),
            new("debrief-02",NarrativeSpeakerId.Yusuf,"The connectors and control layout fit our Civic Relay. That compatibility was deliberate. They weren't just borrowing an airfield.","اتصال‌ها و چیدمان کنترل با رلهٔ شهری ما جور درمیاد. این سازگاری عمدی بوده. اونا فقط از یه فرودگاه استفاده نمی‌کردن."),
            new("debrief-03",NarrativeSpeakerId.Aria,"The recovered schedule identifies Vanguard's command group and its next link window. I can mark the approach for your review. You decide when we move.","برنامه‌ای که بازیابی کردیم، گروه فرماندهی ونگارد و زمان اتصال بعدیشون رو مشخص می‌کنه. می‌تونم مسیر نزدیک شدن رو برای بررسی نشون بدم. زمان حرکت رو شما تعیین می‌کنین.")};
    }
}
