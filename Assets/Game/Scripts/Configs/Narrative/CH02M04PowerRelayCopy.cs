using Game.Catalog.Contracts;

namespace Game.Configs
{
    public readonly struct PowerRelayNarrativeLine
    {
        public readonly string Id,Key,English,Persian;
        public readonly NarrativeSpeakerId Speaker;
        public PowerRelayNarrativeLine(string id,NarrativeSpeakerId speaker,string english,string persian)
        {
            Id="power_relay-"+id;Key="narrative.power_relay."+id.Replace('-','.');Speaker=speaker;English=english;Persian=persian;
        }
    }
    public static class CH02M04PowerRelayCopy
    {
        public static readonly PowerRelayNarrativeLine[] Brief={
            new("brief-01",NarrativeSpeakerId.Lina,
                "The substation serving our homes, shelters and water treatment has failed. Displaced families are waiting in the open, and the nearest shelter is already overcrowded.",
                "پست برقی که به خونه‌ها، پناهگاه‌ها و تصفیه‌خونه آب می‌رسونه از کار افتاده. خانواده‌های آواره بیرون منتظرن و نزدیک‌ترین پناهگاه هم بیش از حد شلوغه."),
            new("brief-02",NarrativeSpeakerId.Aria,
                "The exposed industrial lane is the shortest route to shelter. My initial recommendation is to move the family convoy through it, then deliver Fuel and engineers to the relay.",
                "مسیر صنعتیِ روباز، کوتاه‌ترین راه تا پناهگاهه. پیشنهاد اولیه‌م اینه که کاروان خانواده‌ها رو از همون‌جا عبور بدیم، بعد سوخت و مهندس‌ها رو به رله برسونیم."),
            new("brief-03",NarrativeSpeakerId.Samira,
                "Do not use the shortest lane. The shelter at its end is full, but the school on the longer service road is open. Take the protected route, then keep the engineers supplied while they restore power.",
                "از کوتاه‌ترین مسیر نرید. پناهگاه انتهای اون پره، اما مدرسهٔ مسیر خدماتیِ طولانی‌تر بازه. خانواده‌ها رو از مسیر محافظت‌شده ببرید، بعد تا برگشت برق، سوخت مهندس‌ها رو تأمین کنید.")};
        public static readonly PowerRelayNarrativeLine[] Comms={
            new("comms-01",NarrativeSpeakerId.Aria,
                "Recommendation revised. Samira's shelter-capacity report changes the optimal route: protect the family convoy along the service road, then secure the repair crew. My first route omitted people the formal map could not see.",
                "پیشنهاد اصلاح شد. گزارش ظرفیت پناهگاهِ سمیرا مسیر بهینه رو عوض می‌کنه: از کاروان خانواده‌ها در جادهٔ خدماتی محافظت کنید، بعد گروه تعمیر رو امن نگه دارید. مسیر اول من آدم‌هایی رو نادیده گرفته بود که روی نقشهٔ رسمی دیده نمی‌شدن.")};
        public static readonly PowerRelayNarrativeLine[] Debrief={
            new("debrief-01",NarrativeSpeakerId.Dalia,
                "Power and clean-water service are returning across the east district. The families reached the school shelter without crossing the exposed lane, and there is room for everyone tonight.",
                "برق و آب سالم دارن در سراسر منطقهٔ شرقی برمی‌گردن. خانواده‌ها بدون عبور از مسیر روباز به پناهگاه مدرسه رسیدن و امشب برای همه جا هست."),
            new("debrief-02",NarrativeSpeakerId.Aria,
                "The restored substation emitted a dormant Civic Relay handshake. I optimized for the city I could see.",
                "پست برقِ بازیابی‌شده یک دست‌دهیِ خاموش از شبکهٔ شهری فرستاد. من برای شهری بهینه‌سازی کردم که می‌تونستم ببینم."),
            new("debrief-03",NarrativeSpeakerId.Samira,
                "Then keep asking about the one you can't. The handshake points to a logistics hub; that is where we go next.",
                "پس دربارهٔ شهری که نمی‌تونی ببینی هم سؤال بپرس. این دست‌دهی به یک مرکز تدارکات اشاره می‌کنه؛ مقصد بعدی ما همون‌جاست.")};
    }
}
