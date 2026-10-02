using Game.Catalog.Contracts;
namespace Game.Configs
{
    public readonly struct NetworkCollapseNarrativeLine
    {
        public readonly string Id, English, Persian;
        public readonly NarrativeSpeakerId Speaker;
        public string Key => "narrative.network_collapse." + Id.Substring("network_collapse-".Length);
        public NetworkCollapseNarrativeLine(string id, NarrativeSpeakerId speaker, string english, string persian)
        { Id="network_collapse-"+id; Speaker=speaker; English=english; Persian=persian; }
    }
    public static class CH05M03NetworkCollapseNarrativeCopy
    {
        public static readonly NetworkCollapseNarrativeLine[] Brief={
            new("brief-01",NarrativeSpeakerId.Dalia,"Three command nodes sit among homes and civic services. A blind long-range strike would destroy the audit with them. We take the ground route: clear each node’s guards, verify its external access, then disable only the confirmed node.","سه گرهٔ فرماندهی بین خانه‌ها و خدمات شهری قرار دارن. یک حملهٔ دوربرد کور، گزارش بازرسی رو هم با اون‌ها نابود می‌کنه. از مسیر زمینی می‌ریم؛ نگهبان‌های هر گره رو پاک‌سازی کنین، دسترسی بیرونیش رو تأیید کنین، بعد فقط گرهٔ تأییدشده رو از کار بندازین."),
            new("brief-02",NarrativeSpeakerId.Aria,"Work through the three nodes in order. After clearing each guard group, hold the original engineer at its recon marker for six seconds. That confirms the node for Attack. After the third node, bring the engineer to the audit custody marker and hold for six seconds.","سه گره رو به ترتیب پیش ببرین. بعد از پاک‌سازی نگهبان‌های هر گره، مهندس اصلی رو شش ثانیه روی نشان شناسایی همون گره نگه دارین. با این کار گره برای حمله تأیید می‌شه. بعد از گرهٔ سوم، مهندس رو به نشان نگهداری گزارش برسونین و شش ثانیه نگه دارین."),
            new("brief-03",NarrativeSpeakerId.Samira,"Protect both civic buildings, their staff, the original engineer and the APC. Once the audit is secured, board the engineer into the APC and move to the marked extraction point. Hold there for six seconds. Preserving the people and the proof is part of the same mission.","از هر دو ساختمان خدمات، کارکنانشون، مهندس اصلی و نفربر محافظت کنین. وقتی گزارش امن شد، مهندس رو سوار نفربر کنین و به محل مشخص‌شدهٔ خروج حرکت بدین. شش ثانیه اونجا بمونین. حفظ مردم و مدرک، هر دو بخشی از همین مأموریتن.")};
        public static readonly NetworkCollapseNarrativeLine[] Comms={
            new("comms-01",NarrativeSpeakerId.Aria,"The first verified node is isolated. The opening records connect Qassem’s authorization to the original shutdown and the current attacks. The remaining audit still needs its physical chain. Continue in order; keep the engineer, protected services and custody equipment intact.","اولین گرهٔ تأییدشده جدا شد. سندهای اولیه، مجوز قاسم رو به خاموشی اصلی و حمله‌های فعلی وصل می‌کنن. بخش باقی‌موندهٔ گزارش هنوز به زنجیرهٔ فیزیکیش نیاز داره. به ترتیب ادامه بدین؛ مهندس، خدمات محافظت‌شده و تجهیزات نگهداری مدرک رو سالم نگه دارین.")};
        public static readonly NetworkCollapseNarrativeLine[] Debrief={
            new("debrief-01",NarrativeSpeakerId.Dalia,"The verified network is down, and the engineer brought the complete audit out safely. The protected buildings and staff survived. We chose the slower route because destroying the evidence would only leave Qassem another chance to claim we lied.","شبکهٔ تأییدشده از کار افتاد و مهندس، گزارش کامل رو سالم بیرون آورد. ساختمان‌ها و کارکنان محافظت‌شده سالم موندن. مسیر کندتر رو انتخاب کردیم، چون نابود کردن مدرک فقط یک فرصت دیگه به قاسم می‌داد تا بگه دروغ گفتیم."),
            new("debrief-02",NarrativeSpeakerId.Aria,"The complete audit ties Qassem to the original override, the attempted erasure, Ash Line bombings, false reports, diverted Fuel and Vanguard hardware. Civilian and military authorities have copies. He manufactured the crisis to make permanent emergency rule appear necessary.","گزارش کامل، قاسم رو به فرمان اصلی، تلاش برای پاک کردن مدارک، بمب‌گذاری‌های خط خاکستر، گزارش‌های دروغ، سوخت منحرف‌شده و تجهیزات ونگارد وصل می‌کنه. مقام‌های غیرنظامی و نظامی نسخه‌هاش رو دارن. اون بحران رو ساخت تا حکومت اضطراری دائمی رو ضروری جلوه بده."),
            new("debrief-03",NarrativeSpeakerId.Samira,"The Relay complex is still sealed. The physical access keys, engineers, medical supplies, Fuel and reinforcements must reach the city center. The next corridor carries what Sahrin needs to recover; keep those priorities connected.","مجموعهٔ رله هنوز بسته‌ست. کلیدهای فیزیکی دسترسی، مهندس‌ها، تجهیزات پزشکی، سوخت و نیروهای کمکی باید به مرکز شهر برسن. مسیر بعدی، چیزهایی رو حمل می‌کنه که سهرین برای بازیابی نیاز داره؛ این اولویت‌ها رو به هم پیوسته نگه دارین.")};
    }
}
