using Game.Catalog.Contracts;
namespace Game.Configs
{
    public readonly struct M05NarrativeLine
    {
        public readonly string Id,Key,English,Persian; public readonly NarrativeSpeakerId Speaker;
        public M05NarrativeLine(string id,NarrativeSpeakerId speaker,string en,string fa)
        {Id="m05-"+id;Key="narrative.m05."+id.Replace('-','.');Speaker=speaker;English=en;Persian=fa;}
    }
    public static class M05BreachAssaultCopyCatalog
    {
        public static readonly M05NarrativeLine[] Brief={
            new("brief-01",NarrativeSpeakerId.Samira,"The engineers traced the outages to this fortified relay. Break the road gate, disable its transmitter, and secure the archive. Those records can tell us who ordered the attacks.","مهندس‌ها رد قطعی‌ها رو توی این مرکز ارتباطی پیدا کردن. دروازهٔ جاده رو بشکنید، فرستنده رو از کار بندازید و بایگانی رو امن کنید. شاید این اسناد نشون بدن کی دستور حمله‌ها رو داده."),
            new("brief-02",NarrativeSpeakerId.Dalia,"Two rifle squads and a heavy APC are yours. Use the armor to open the gate, then bring the rifles through together. Preserve the APC if you can; losing it does not end the assault.","دو گروه تفنگدار و یه نفربر سنگین دارید. با زره‌پوش دروازه رو باز کنید، بعد تفنگدارها رو با هم ببرید اون‌طرف. تا می‌تونید نفربر رو سالم نگه دارید؛ اگه از دست رفت هم حمله تموم نشده."),
            new("brief-03",NarrativeSpeakerId.Aria,"The relay will erase its archive in twelve minutes. Stop the transmitter, defeat the responding guard, and hold the marked archive area for twenty clear seconds. I will guide each action.","تا دوازده دقیقهٔ دیگه بایگانی رو پاک می‌کنن. فرستنده رو متوقف کن، نیروی واکنش دشمن رو شکست بده و محدودهٔ مشخص‌شده رو بیست ثانیه امن نگه دار. قدم‌به‌قدم راهنماییت می‌کنم.")};
        public static readonly M05NarrativeLine[] Comms={
            new("comms-01",NarrativeSpeakerId.Aria,"The archive is intact. I have recovered Protocol Fragment One. Its authorization record includes a credential issued to me, then revoked before these attacks.","بایگانی سالمه. قطعهٔ اول پروتکل رو پیدا کردم. یه مجوز به اسم من توی سوابقشه؛ ولی این مجوز قبل از حمله‌ها باطل شده بود.")};
        public static readonly M05NarrativeLine[] Debrief={
            new("debrief-01",NarrativeSpeakerId.Samira,"The relay is silent and the records are safe. We have evidence now, not another rumor. Take care of the wounded; the city needs everyone we can bring home.","مرکز ارتباطی خاموش شد و اسناد سالم‌ان. حالا مدرک داریم. به زخمی‌ها برسید؛ شهر به تک‌تک آدم‌هایی که برمی‌گردونیم نیاز داره."),
            new("debrief-02",NarrativeSpeakerId.Aria,"The credential was revoked. Someone used its signature anyway. One encrypted message survived: “Qassem. The first node is lost.” I cannot yet identify the sender.","مجوز باطل شده بود، ولی یکی دوباره از امضاش استفاده کرده. یه پیام رمزگذاری‌شده هم مونده: «قاسم. گرهٔ اول از دست رفت.» هنوز نمی‌دونم کی فرستادتش."),
            new("debrief-03",NarrativeSpeakerId.Dalia,"First Response is complete. The reconnaissance team and armor parts are assigned to your roster. Review your results, then return to the campaign map. We will follow the evidence.","فصل واکنش نخست تموم شد. تیم شناسایی و قطعات زره به تجهیزاتت اضافه شدن. نتیجه رو ببین و برگرد به نقشهٔ کارزار. رد این مدارک رو دنبال می‌کنیم.")};
        public static readonly (string Title,string PersianTitle,string Body,string PersianBody,string Example,string PersianExample,string Mistake,string PersianMistake)[] Lessons={
            ("Read the assault plan","طرح حمله رو بخون","Break the gate, destroy the transmitter, then secure the archive for 20 seconds. You have 12 minutes. Keep the APC alive and finish under 9 minutes for bonus stars.","دروازه رو بشکن، فرستنده رو نابود کن، بعد بایگانی رو بیست ثانیه امن نگه دار. دوازده دقیقه وقت داری. اگه نفربر سالم بمونه و زیر نه دقیقه تموم کنی، ستارهٔ اضافه می‌گیری.","Press Continue when ready.","وقتی آماده‌ای، «ادامه» رو بزن.","The APC is a bonus objective, not your only way to win.","نفربر هدف امتیازی است، نه تنها راه پیروزی."),
            ("Select your assault unit","نیروی حمله رو انتخاب کن","Select the heavy APC. If it is lost, use a surviving rifle squad. Show Me marks the unit to select.","نفربر سنگین رو انتخاب کن. اگه از دست رفته، از تفنگدارهای باقی‌مونده استفاده کن. «نشان بده» نیروی مورد نظر رو نشونت می‌ده.","Tap the marked unit.","دکمهٔ انتخاب نیروی مشخص‌شده رو بزن.","Orders require a selected friendly unit.","برای صدور فرمان باید نیروی خودی انتخاب شده باشد."),
            ("Breach the road gate","دروازهٔ جاده رو بشکن","Press Attack, then tap the marked enemy gate. Wait for it to fall. Show Me follows your next required click.","«حمله» رو بزن، بعد دروازهٔ مشخص‌شدهٔ دشمن رو انتخاب کن. صبر کن تا نابود بشه. «نشان بده» می‌گه قدم بعدی رو کجا بزنی.","Keep the APC supported by the rifles.","تفنگدارها از نفربر پشتیبانی کنند.","Move orders do not attack the gate.","فرمان حرکت به دروازه حمله نمی‌کنه."),
            ("Bring the team through","تیم رو از دروازه عبور بده","Select your rifles, press Move, and tap the marked approach beyond the gate. Keep the team together; enemy reinforcements are responding.","گروه تفنگدارها رو انتخاب کن، «حرکت» رو بزن و ببرشون به مقصد مشخص‌شده اون‌طرف دروازه. کنار هم نگهشون دار؛ نیروی کمکی دشمن توی راهه.","Move into the open area by the relay.","به فضای باز کنار مرکز ارتباطی بروید.","Do not leave the rifles behind while armor fights alone.","تفنگدارها رو عقب جا نذار؛ زره‌پوش نباید تنها بجنگه."),
            ("Silence the transmitter","فرستنده رو خاموش کن","Select a friendly unit, press Attack, then tap the marked satellite transmitter. Destroying it opens the archive objective.","یه نیروی خودی رو انتخاب کن، «حمله» رو بزن، بعد فرستندهٔ ماهواره‌ای مشخص‌شده رو بزن. وقتی نابود بشه، می‌تونی سراغ بایگانی بری.","Concentrate fire on the marked transmitter.","آتش رو روی فرستندهٔ مشخص‌شده متمرکز کن.","Focus on the marked transmitter inside the compound.","روی فرستندهٔ مشخص‌شده داخل پایگاه تمرکز کن."),
            ("Stop the counterattack","ضدحمله رو متوقف کن","Attack the remaining guards. Show Me marks a live enemy. You must defeat the garrison and its reinforcements before securing the archive.","به نگهبان‌های باقی‌مونده حمله کن. «نشان بده» دشمن باقی‌مونده رو نشونت می‌ده. قبل از امن‌کردن بایگانی باید نیروهای پادگان و نیروی کمکی رو شکست بدی.","Use the APC and rifles together.","از نفربر و تفنگدارها با هم استفاده کن.","The archive cannot be secured while the guard is still active.","تا وقتی نگهبان‌ها فعال‌اند، بایگانی امن نمی‌شه."),
            ("Reach the archive","به بایگانی برسید","Select a surviving unit, press Move, then tap the marked archive area. Keep at least one unit there while the records are recovered.","یه نیروی باقی‌مونده رو انتخاب کن، «حرکت» رو بزن و بفرستش توی محدودهٔ بایگانی. تا اسناد بازیابی بشن، حداقل یه نیرو رو اونجا نگه دار.","The secure timer starts when the area is clear.","زمان‌سنج امنیت بعد از پاک‌سازی محل شروع می‌شه.","Leaving the area resets the twenty-second hold.","خروج از محدوده شمارش بیست‌ثانیه‌ای رو از نو آغاز می‌کنه."),
            ("Recover the archive","اسناد رو بازیابی کن","Hold the archive area for 20 uninterrupted seconds. Keep a survivor in the marked area until ARIA confirms recovery.","محدودهٔ بایگانی رو بیست ثانیه بدون وقفه نگه دار. تا بازیابی رو تأیید نکردم، حداقل یه نیروی زنده باید اونجا بمونه.","Wait for the recovery confirmation.","منتظر تأیید بازیابی بمون.","A destroyed transmitter alone does not complete the mission.","نابودی فرستنده به‌تنهایی مأموریت رو کامل نمی‌کنه.")};
        public static readonly (string Key,string English,string Persian)[] Ui={
            ("mission.m05.guide.title","FIELD GUIDE · BREACH ASSAULT","راهنمای میدانی · تهاجم نفوذی"),
            ("mission.m05.name","BREACH ASSAULT","تهاجم نفوذی"),
            ("mission.m05.compound.gate", "Relay Gate", "دروازهٔ پایگاه"),
            ("mission.m05.compound.wall", "Concrete Perimeter", "دیوار بتنی"),
            ("mission.m05.compound.archive", "Archive", "بایگانی"),
            ("mission.m05.compound.guard_post", "Guard Post", "پست نگهبانی"),
            ("mission.m05.summary","Breach the relay gate, silence its transmitter, and recover the archive.","دروازهٔ مرکز ارتباطی رو بشکن، فرستنده رو خاموش کن و اسناد بایگانی رو بردار."),
            ("mission.m05.location","Eastern district · fortified relay","بخش شرقی · مرکز ارتباطی مستحکم"),
            ("mission.m05.enemy_intel","Garrison infantry; reinforcements respond 15 seconds after the gate takes damage.","نیروهای پادگان، ۱۵ ثانیه بعد از آسیب‌دیدن دروازه واکنش نشون می‌دن."),
            ("mission.m05.resources","Mission-supplied heavy APC; no construction required","نفربر سنگین با ذخیرهٔ مأموریت؛ ساخت‌وساز لازم نیست"),
            ("mission.m05.access","8 rifles · heavy APC","۸ تفنگدار · نفربر سنگین"),
            ("mission.m05.options","Breach together; preserve the armor; recover the records","با هم نفوذ کن؛ زره‌پوش رو سالم نگه دار؛ اسناد رو بازیابی کن"),
            ("mission.m05.objective.gate","Breach the compound gate","دروازهٔ پایگاه رو بشکن"),
            ("mission.m05.objective.core","Disable the transmitter","فرستنده رو از کار بنداز"),
            ("mission.m05.objective.archive","Secure the archive for 20 seconds","بایگانی رو ۲۰ ثانیه امن نگه دار"),
            ("mission.guide.lesson_count","{0} lessons","{0} درس"),
            ("mission.m05.guide.availability.0","Friendly in M5","خودی در مأموریت پنجم"),
            ("mission.m05.guide.availability.1","Hostile in M5","دشمن در مأموریت پنجم"),
            ("mission.m05.guide.availability.2","Protected civilian in M5","غیرنظامی محافظت‌شده در مأموریت پنجم"),
            ("mission.m05.guide.availability.3","Reference only · unavailable in M5","فقط برای آشنایی · در مأموریت پنجم در دسترس نیست"),
            ("mission.m05.objective.gate.body","Attack the marked gate to open the entrance into the base.","به دروازهٔ مشخص‌شده حمله کن تا ورودی پایگاه باز بشه."),
            ("mission.m05.objective.core.body","After the breach, destroy the marked satellite transmitter.","بعد از نفوذ، فرستندهٔ ماهواره‌ای مشخص‌شده رو نابود کن."),
            ("mission.m05.objective.archive.body","Defeat all guards and hold the marked area for 20 seconds.","همهٔ نگهبان‌ها رو شکست بده و محدودهٔ مشخص‌شده رو ۲۰ ثانیه حفظ کن."),
            ("mission.m05.star.1","Recover the archive","بایگانی رو بازیابی کن"),
            ("mission.m05.star.2","Heavy APC survives","نفربر سنگین زنده بماند"),
            ("mission.m05.star.3","Finish under 9 minutes","پایان در کمتر از ۹ دقیقه"),
            ("mission.m05.reward.ghillie","Ghillie reconnaissance team","تیم شناسایی استتاری"),
            ("mission.m05.reward.armor","APC armor parts","قطعات زره نفربر"),
            ("mission.m05.reward.card","750 XP | 4,000 CREDITS | RECON | 35 ARMOR PARTS","۷۵۰ تجربه | ۴۰۰۰ اعتبار | شناسایی | ۳۵ قطعهٔ زره"),
            ("mission.m05.camera.opening","ASSAULT ROUTE · Gate → transmitter → archive","مسیر حمله · دروازه ← فرستنده ← بایگانی"),
            ("mission.m05.camera.victory","ARCHIVE RECOVERED · Protocol Fragment One secured","بایگانی بازیابی شد · قطعهٔ اول پروتکل در امان است"),
            ("mission.m05.result.victory","FIRST RESPONSE COMPLETE","واکنش نخست کامل شد"),
            ("mission.m05.result.defeat","ASSAULT FAILED","حمله ناموفق بود"),
            ("mission.m05.result.success","The relay is silent. Protocol Fragment One is secured.","مرکز ارتباطی خاموش است. قطعهٔ اول پروتکل در امان است."),
            ("mission.m05.result.timeout","The archive was erased. Retry and reach the relay before twelve minutes.","بایگانی پاک شد. دوباره امتحان کن؛ باید قبل از دوازده دقیقه به مرکز برسی."),
            ("mission.m05.result.loss","The assault force was lost. Retry with the rifles supporting the APC.","نیروی حمله از دست رفت. دوباره تلاش کن و با تفنگدارها از نفربر پشتیبانی کن."),
            ("mission.m05.result.setup","The assault could not be prepared. Retry the mission.","آماده‌سازی حمله کامل نشد. مأموریت رو دوباره آغاز کن."),
            ("mission.m05.result.chapter","CHAPTER ONE COMPLETE","فصل اول کامل شد"),
            ("mission.m05.tutorial.4.fallback","Your rifles are lost. Select the surviving APC, press Move, then tap the marked approach beyond the gate.","تفنگدارها رو از دست دادی. نفربر باقی‌مونده رو انتخاب کن، «حرکت» رو بزن و بفرستش به مقصد مشخص‌شده پشت دروازه."),
            ("mission.m05.result.unit_losses","ASSAULT UNITS LOST","نیروهای ازدست‌رفته"),
            ("mission.m05.result.support","HEAVY APC","نفربر سنگین"),
            ("mission.m05.result.lost","LOST","از دست رفت"),
            ("mission.m05.result.survived","SURVIVED","سالم"),
            ("mission.m05.hud.recovery.progress","Recovering the archive: {0} seconds left. Keep at least one unit inside the marked area. Leaving resets recovery.","دارم اسناد رو بازیابی می‌کنم؛ {0} ثانیه مونده. حداقل یه نیرو رو توی محدوده نگه دار. اگه همه خارج بشن، باید از اول شروع کنیم."),
            ("mission.m05.hud.recovery.enter","Move at least one unit into the marked archive area to start the 20-second recovery.","حداقل یه نیرو رو ببر توی محدودهٔ بایگانی تا بازیابی بیست‌ثانیه‌ای شروع بشه."),
            ("mission.m05.hud.recovery.enemies","Recovery is blocked by enemies. Defeat the remaining guards, then keep a unit inside the archive area for 20 seconds.","دشمن‌ها نمی‌ذارن بازیابی رو شروع کنم. نگهبان‌های باقی‌مونده رو شکست بده، بعد یه نیرو رو بیست ثانیه توی محدودهٔ بایگانی نگه دار."),
            ("mission.m05.hud.recovery.reinforcements","Enemy reinforcements are approaching. Defeat them before archive recovery can begin.","نیروی کمکی دشمن داره می‌رسه. شکستشون بده تا بتونم اسناد رو بازیابی کنم."),
            ("mission.m05.hud.recovery.complete","Archive recovered. Mission complete.","اسناد بازیابی شد. مأموریت تمومه."),
            ("mission.m05.hud.status","Archive {0}/{3}s · Guards {1} · {2} remaining","بایگانی: {0} از {3} ثانیه · نگهبان‌ها: {1} · زمان: {2}"),
            ("mission.m05.hud.counterattack","Enemy reinforcements in {0}s","نیروی کمکی دشمن: {0} ثانیه دیگر")};
    }
}
