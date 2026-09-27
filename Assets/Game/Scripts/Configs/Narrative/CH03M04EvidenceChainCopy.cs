using Game.Catalog.Contracts;

namespace Game.Configs
{
    public readonly struct EvidenceChainNarrativeLine
    {
        public readonly string Id, Key, English, Persian;
        public readonly NarrativeSpeakerId Speaker;

        public EvidenceChainNarrativeLine(string id, NarrativeSpeakerId speaker, string english, string persian)
        {
            Id = "evidence_chain-" + id;
            Key = "narrative.evidence_chain." + id.Replace('-', '.');
            Speaker = speaker;
            English = english;
            Persian = persian;
        }
    }

    public static class CH03M04EvidenceChainCopy
    {
        public static readonly EvidenceChainNarrativeLine[] Brief =
        {
            new("brief-01", NarrativeSpeakerId.Lina,
                "The archive custodian made it to my clinic. The witness has the physical record, and the roads outside are closing. They need a safe passage, not another interrogation.",
                "نگهبان بایگانی خودش رو به درمانگاه من رسونده. شاهد سندِ اصلی رو همراهش داره و راه‌های بیرون دارن بسته می‌شن. الان به یک مسیر امن نیاز داره، نه بازجوییِ بیشتر."),
            new("brief-02", NarrativeSpeakerId.Laila,
                "I can take them by air if you secure the landing zone. If the sky closes, the armored carrier is the way through. Keep both options in sight.",
                "اگه منطقهٔ فرود رو امن کنی، می‌تونم هوایی ببرمشون. اگه آسمون بسته شد، نفربر زرهی راه عبوره. هر دو گزینه رو در نظر داشته باش."),
            new("brief-03", NarrativeSpeakerId.Dalia,
                "The witness and the archive stay together. Board them in the carrier, keep the escorts close, then transfer them only at a secure landing zone. We do not chase fighters at their expense.",
                "شاهد و بایگانی باید کنار هم بمونن. سوار نفربرشون کن، نیروهای همراه رو نزدیک نگه دار و فقط در منطقهٔ فرودِ امن منتقلشون کن. برای تعقیب مهاجم‌ها رهاشون نمی‌کنیم.")
        };

        public static readonly EvidenceChainNarrativeLine[] Comms =
        {
            new("comms-01", NarrativeSpeakerId.Dalia,
                "Ambush ahead. Salma is holding the rear. Open a path for the carrier and keep the witness out of the firing line; the retreating fighters are not the priority.",
                "جلو کمین گذاشتن. سلما پشت سر رو نگه داشته. برای نفربر راه باز کن و شاهد رو از تیررس دور نگه دار؛ تعقیب نیروهای عقب‌نشسته اولویت نیست."),
            new("comms-02", NarrativeSpeakerId.Aria,
                "The archive just authenticated with a self-sealing command bearing my signature. I created that barrier. Protect the case; I cannot open the missing key record from here.",
                "بایگانی با یک فرمانِ خودمُهر تأیید شد که امضای من رو داره. این مانع رو خودم ساختم. از محفظه محافظت کن؛ از اینجا به کلیدِ گم‌شده دسترسی ندارم.")
        };

        public static readonly EvidenceChainNarrativeLine[] Debrief =
        {
            new("debrief-01", NarrativeSpeakerId.Lina,
                "The witness is safe, and the archive reached analysis without leaving authorized hands. Thank you for making their safety part of the mission.",
                "شاهد در امانه و بایگانی بدون اینکه از دست افراد مسئول خارج بشه به مرکز بررسی رسید. ممنون که امنیت اون رو بخشی از مأموریت دونستی."),
            new("debrief-02", NarrativeSpeakerId.Aria,
                "The self-seal is mine, not an imitation. I isolated this part of the original audit. Its key record is still in the active audit bunker, and Ash Line is moving to erase it.",
                "این خودمُهر مال منه، نه یک تقلید. من این بخش از بازرسی اصلی رو جدا کردم. کلیدش هنوز توی پناهگاهِ بازرسیِ فعاله و خط خاکستر داره برای پاک کردنش حرکت می‌کنه.")
        };
    }
}
