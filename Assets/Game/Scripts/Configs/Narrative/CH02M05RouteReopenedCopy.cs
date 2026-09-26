using Game.Catalog.Contracts;

namespace Game.Configs
{
    public readonly struct RouteReopenedNarrativeLine
    {
        public readonly string Id,Key,English,Persian;
        public readonly NarrativeSpeakerId Speaker;
        public RouteReopenedNarrativeLine(string id,NarrativeSpeakerId speaker,string english,string persian)
        {
            Id="route_reopened-"+id;Key="narrative.route_reopened."+id.Replace('-','.');Speaker=speaker;English=english;Persian=persian;
        }
    }
    public static class CH02M05RouteReopenedCopy
    {
        public static readonly RouteReopenedNarrativeLine[] Brief={
            new("brief-01",NarrativeSpeakerId.Samira,
                "The relief network is still moving: clinic supplies on the south lane, Fuel for water pumps on the north. The Ash Line logistics hub controls both stolen routes, but the city cannot wait for the fighting to end.",
                "شبکهٔ امداد هنوز در حرکته: تجهیزات درمانگاه در مسیر جنوبی و سوخت پمپ‌های آب در مسیر شمالی. مرکز تدارکات اش‌لاین هر دو مسیر دزدیده‌شده رو کنترل می‌کنه، اما شهر نمی‌تونه تا پایان درگیری منتظر بمونه."),
            new("brief-02",NarrativeSpeakerId.Dalia,
                "Split the force. Keep both lifelines open, put the road crew on the disrupted lane, and take the assault squad through the hub gate. This is a controlled capture, not a demolition.",
                "نیرو رو تقسیم کن. هر دو مسیر حیاتی رو باز نگه دار، گروه راه‌سازی رو روی مسیر قطع‌شده بذار و گروه تهاجم رو از دروازهٔ مرکز وارد کن. این عملیات تصرف کنترل‌شده‌ست، نه تخریب."),
            new("brief-03",NarrativeSpeakerId.Aria,
                "The hub's routing archive links the Fuel diversions, market manifests and power handshake. Preserve those records while I correlate each physical delivery with dormant Civic Relay activity.",
                "بایگانی مسیرهای مرکز، انحراف سوخت، بارنامه‌های بازار و دست‌دهی برق رو به هم وصل می‌کنه. اسناد رو سالم نگه دارید تا هر تحویل فیزیکی رو با فعالیت گره‌های خاموش شبکهٔ شهری تطبیق بدم.")};
        public static readonly RouteReopenedNarrativeLine[] Comms={
            new("comms-01",NarrativeSpeakerId.Aria,
                "Commander, that delivery activated another dormant Relay node. The network is not merely stealing resources; it is using the city's emergency traffic as a deliberate activation pattern. Keep the archive intact.",
                "فرمانده، اون تحویل یک گرهٔ خاموش دیگه رو فعال کرد. این شبکه فقط منابع رو نمی‌دزده؛ از رفت‌وآمد اضطراری شهر به‌عنوان الگوی عمدی فعال‌سازی استفاده می‌کنه. بایگانی رو سالم نگه دارید.")};
        public static readonly RouteReopenedNarrativeLine[] Debrief={
            new("debrief-01",NarrativeSpeakerId.Dalia,
                "The hub is ours, both lifelines stayed open, and the road crew restored the broken lane under fire. Samira's teams are already returning the captured equipment to civilian service.",
                "مرکز دست ماست، هر دو مسیر حیاتی باز موند و گروه راه‌سازی زیر آتش مسیر قطع‌شده رو برگردوند. گروه‌های سمیرا همین حالا تجهیزات ضبط‌شده رو به خدمات شهری برمی‌گردونن."),
            new("debrief-02",NarrativeSpeakerId.Aria,
                "Protocol Fragment Two assembled. Every diverted road, Fuel load, market transfer and power handshake fed selected dormant Civic Relay nodes. The attacks were preparation, not random destruction.",
                "قطعهٔ دوم پروتکل کامل شد. هر انحراف جاده، محمولهٔ سوخت، انتقال بازار و دست‌دهی برق، گره‌های خاموش مشخصی از شبکهٔ شهری رو تغذیه کرده. این حمله‌ها آماده‌سازی بودن، نه تخریب تصادفی."),
            new("debrief-03",NarrativeSpeakerId.Qassem,
                "A city under stress authorizes what it rejected in peace. You reopened the route for me. Find my signal among the people you insist on protecting.",
                "شهری که زیر فشاره چیزی رو تأیید می‌کنه که در زمان صلح رد کرده بود. تو مسیر رو برای من باز کردی. حالا سیگنالم رو میان همون مردمی پیدا کن که اصرار داری ازشون محافظت کنی.")};
    }
}
