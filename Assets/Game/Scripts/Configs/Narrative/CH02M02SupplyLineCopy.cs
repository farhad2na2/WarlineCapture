using Game.Catalog.Contracts;
namespace Game.Configs
{
    public readonly struct SupplyLineNarrativeLine
    {
        public readonly string Id,Key,English,Persian;
        public readonly NarrativeSpeakerId Speaker;
        public SupplyLineNarrativeLine(string id,NarrativeSpeakerId speaker,string english,string persian)
        {Id="supply_line-"+id;Key="narrative.supply_line."+id.Replace('-','.');Speaker=speaker;English=english;Persian=persian;}
    }
    public static class CH02M02SupplyLineCopy
    {
        public static readonly SupplyLineNarrativeLine[] Brief={
            new("brief-01",NarrativeSpeakerId.Aria,"The pump extracts Oil. The tray truck carries it to the refinery. The tanker then delivers refined Fuel to the warehouse. The trucks handle their routes automatically; protect every link.","پمپ، نفت استخراج می‌کنه. کامیون کفی اون رو به پالایشگاه می‌بره. بعد تانکر، سوخت تولیدشده رو به انبار می‌رسونه. کامیون‌ها خودکار کار می‌کنن؛ از همهٔ حلقه‌های زنجیره محافظت کن."),
            new("brief-02",NarrativeSpeakerId.Samira,"This reserve keeps clinic generators and water pumps running. Twenty barrels must stay available for those services. Military transport cannot take everything.","این ذخیره، ژنراتورهای درمانگاه و پمپ‌های آب رو روشن نگه می‌داره. بیست بشکه باید برای این خدمات باقی بمونه. ترابری نظامی نمی‌تونه همه‌اش رو برداره."),
            new("brief-03",NarrativeSpeakerId.Dalia,"Agreed. Forty barrels: twenty for essential services, twenty for relief transport. Protect both trucks and all three sites, defeat the sabotage teams, then hold the reserve for twenty seconds. We have twelve minutes.","موافقم. چهل بشکه: بیست تا برای خدمات ضروری، بیست تا برای ترابری امداد. از هر دو کامیون و هر سه محل محافظت کن، خرابکارها رو شکست بده و بعد ذخیره رو بیست ثانیه نگه دار. دوازده دقیقه وقت داریم.")};
        public static readonly SupplyLineNarrativeLine[] Comms={
            new("comms-01",NarrativeSpeakerId.Aria,"Fuel has reached the reserve. A captured manifest shows the diverted tankers used the same dormant corridor we found at the hospital road.","سوخت به انبار رسیده. بارنامهٔ به‌دست‌اومده نشون می‌ده تانکرهای منحرف‌شده از همون مسیر متروکی رفتن که کنار جادهٔ بیمارستان پیدا کردیم.")};
        public static readonly SupplyLineNarrativeLine[] Debrief={
            new("debrief-01",NarrativeSpeakerId.Samira,"The reserve is secure. Clinic generators, water pumps and relief vehicles can keep working. Protecting their supply is part of protecting the district.","ذخیره امنه. ژنراتورهای درمانگاه، پمپ‌های آب و خودروهای امداد می‌تونن کار کنن. محافظت از تدارکاتشون بخشی از محافظت از محله‌ست."),
            new("debrief-02",NarrativeSpeakerId.Aria,"The stolen Fuel route matches Gridlock's preserved corridor. Decommissioned Civic Relay maps place dormant systems at its destination. The repeated pattern is now evidence, not coincidence.","مسیر سوخت دزدیده‌شده با مسیر دست‌نخوردهٔ مأموریت قبلی یکیه. نقشه‌های قدیمی شبکهٔ شهری، سامانه‌های خاموشی رو در مقصد نشون می‌دن. این الگوی تکراری دیگه تصادف نیست؛ مدرکه."),
            new("debrief-03",NarrativeSpeakerId.Dalia,"Samira's reserve kept both services and transport alive. Old Market is reporting another manipulated supply route. We will follow the manifests next.","ذخیره‌ای که سمیرا خواست، هم خدمات رو سر پا نگه داشت، هم ترابری رو. از بازار قدیمی خبر رسیده که یک مسیر تدارکاتی دیگه هم دستکاری شده. قدم بعدی، دنبال کردن بارنامه‌هاست.")};
    }
}
