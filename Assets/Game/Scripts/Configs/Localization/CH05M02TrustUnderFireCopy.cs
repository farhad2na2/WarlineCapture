using Game.Catalog.Contracts;
namespace Game.Configs
{
    public readonly struct TrustUnderFireNarrativeLine
    {
        public readonly string Id, English, Persian;
        public readonly NarrativeSpeakerId Speaker;
        public string Key => "narrative.trust_under_fire." + Id.Substring("trust_under_fire-".Length);
        public TrustUnderFireNarrativeLine(string id, NarrativeSpeakerId speaker, string english, string persian)
        { Id="trust_under_fire-"+id; Speaker=speaker; English=english; Persian=persian; }
    }
    public static class CH05M02TrustUnderFireCopy
    {
        public static readonly TrustUnderFireNarrativeLine[] Brief={
            new("brief-01",NarrativeSpeakerId.Samira,"Some residents remember help reaching them. Others remember delays and have reason to be afraid. The shelters have room, but both routes are exposed. Today's proof is open routes and safe arrivals.","بعضی ساکنان رسیدن کمک رو به خاطر دارن. بعضی‌ها تأخیرها رو یادشونه و حق دارن بترسن. پناهگاه‌ها جا دارن، ولی هر دو مسیر در خطرن. مدرک امروز ما، مسیرهای باز و رسیدن سالم مردمه."),
            new("brief-02",NarrativeSpeakerId.Dalia,"Qassem's broadcast says we abandoned these neighborhoods. Evacuation is a military priority. Clear the attackers on each protected route, keep the transports intact, and escort the groups to both shelters. Do not take a destructive shortcut through occupied buildings.","پیام قاسم می‌گه این محله‌ها رو رها کردیم. تخلیهٔ مردم یک اولویت نظامیه. مهاجم‌های هر مسیر محافظت‌شده رو پاک‌سازی کنین، خودروهای انتقال رو سالم نگه دارین و گروه‌ها رو تا هر دو پناهگاه همراهی کنین. برای میان‌بُر، ساختمان‌های محل حضور مردم رو تخریب نکنین."),
            new("brief-03",NarrativeSpeakerId.Lina,"We are ready to receive the protected convoys at both shelters. Keep their approaches open. A civilian witness recognized the maintenance noise behind Qassem's broadcasts. Preserve the broadcast compound and its equipment so that report can be checked.","در هر دو پناهگاه آمادهٔ پذیرش کاروان‌های محافظت‌شده هستیم. مسیر رسیدن به اون‌ها رو باز نگه دارین. یک شاهد غیرنظامی، صدای تعمیرات پشت پیام‌های قاسم رو شناخت. محوطهٔ فرستنده و تجهیزاتش رو حفظ کنین تا بتونیم گزارشش رو بررسی کنیم.")};
        public static readonly TrustUnderFireNarrativeLine[] Comms={
            new("comms-01",NarrativeSpeakerId.Samira,"The witness has identified the marked broadcast compound. ARIA has matched its signal to the recording. Secure its external access point and preserve the equipment. Our answer to the abandonment claim must remain an honest record, not another promise people cannot check.","شاهد، محوطهٔ فرستندهٔ مشخص‌شده رو شناسایی کرده. آریا سیگنالش رو با صدای ضبط‌شده تطبیق داده. محل دسترسی بیرونی رو امن کنین و تجهیزات رو حفظ کنین. پاسخ ما به ادعای رها کردن مردم باید یک گزارش صادقانه باشه، نه یک وعدهٔ دیگه که مردم نتونن بررسیش کنن.")};
        public static readonly TrustUnderFireNarrativeLine[] Debrief={
            new("debrief-01",NarrativeSpeakerId.Samira,"The evacuation groups reached their shelters, and the routes remained open. People could see where help was arriving. We will also keep an honest record of delays and damage; a protected road does not erase what still needs repair.","گروه‌های تخلیه به پناهگاه‌ها رسیدن و مسیرها باز موندن. مردم دیدن که کمک از کجا می‌رسه. تأخیرها و خسارت‌ها رو هم صادقانه ثبت می‌کنیم؛ یک جادهٔ امن، چیزهایی رو که هنوز باید درست بشن پاک نمی‌کنه."),
            new("debrief-02",NarrativeSpeakerId.Aria,"The secured transmitter and the witness recording identify the same source. I have preserved the signal records and their custody trail. They point to Qassem's final command network and remain available to civilian and military oversight.","فرستندهٔ امن‌شده و صدای ضبط‌شدهٔ شاهد، هر دو یک منبع رو مشخص می‌کنن. داده‌های سیگنال و مسیر نگهداریشون رو حفظ کردم. این مدارک به شبکهٔ نهایی فرماندهی قاسم می‌رسن و در اختیار نظارت غیرنظامی و نظامی می‌مونن."),
            new("debrief-03",NarrativeSpeakerId.Dalia,"The next strike must leave that proof intact. We will isolate the verified network nodes and protect the people and structures around them. If the evidence dies there, Qassem becomes another voice claiming we lied.","حملهٔ بعدی باید این مدرک رو سالم نگه داره. گره‌های تأییدشدهٔ شبکه رو جدا می‌کنیم و از مردم و ساختمان‌های اطرافشون محافظت می‌کنیم. اگه مدرک اونجا از بین بره، قاسم تبدیل می‌شه به یک صدای دیگه که می‌گه دروغ گفتیم.")};
    }
}
