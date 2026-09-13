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
            new("brief-01",NarrativeSpeakerId.Samira,"The engineers traced the outages to this fortified relay. Break the road gate, disable its transmitter, and secure the archive. Those records can tell us who ordered the attacks.","مهندسان منشأ قطعی‌ها را در این مرکز ارتباطی مستحکم پیدا کردند. دروازهٔ جاده را بشکنید، فرستنده را از کار بیندازید و بایگانی را امن کنید. این اسناد می‌توانند آمر حمله‌ها را مشخص کنند."),
            new("brief-02",NarrativeSpeakerId.Dalia,"Two rifle squads and a heavy APC are yours. Use the armor to open the gate, then bring the rifles through together. Preserve the APC if you can; losing it does not end the assault.","دو گروه تفنگدار و یک نفربر سنگین در اختیار شماست. با زره‌پوش دروازه را باز کنید، سپس تفنگدارها را با هم عبور دهید. اگر می‌توانید نفربر را حفظ کنید؛ از دست دادن آن به معنای پایان حمله نیست."),
            new("brief-03",NarrativeSpeakerId.Aria,"The relay will erase its archive in twelve minutes. Stop the transmitter, defeat the responding guard, and hold the marked archive area for twenty clear seconds. I will guide each action.","این مرکز تا دوازده دقیقهٔ دیگر بایگانی را پاک می‌کند. فرستنده را متوقف کنید، نیروی واکنش دشمن را شکست دهید و محدودهٔ مشخص‌شده را بیست ثانیه امن نگه دارید. هر حرکت را راهنمایی می‌کنم.")};
        public static readonly M05NarrativeLine[] Comms={
            new("comms-01",NarrativeSpeakerId.Aria,"The archive is intact. I have recovered Protocol Fragment One. Its authorization record includes a credential issued to me, then revoked before these attacks.","بایگانی سالم است. قطعهٔ اول پروتکل را بازیابی کردم. در سابقهٔ مجوز آن، اعتبارنامه‌ای به نام من ثبت شده که پیش از این حمله‌ها لغو شده بود.")};
        public static readonly M05NarrativeLine[] Debrief={
            new("debrief-01",NarrativeSpeakerId.Samira,"The relay is silent and the records are safe. We have evidence now, not another rumor. Take care of the wounded; the city needs everyone we can bring home.","مرکز ارتباطی خاموش شده و اسناد در امان‌اند. حالا مدرک داریم، نه شایعه‌ای دیگر. به زخمی‌ها رسیدگی کنید؛ شهر به همهٔ کسانی که سالم برمی‌گردانیم نیاز دارد."),
            new("debrief-02",NarrativeSpeakerId.Aria,"The credential was revoked. Someone used its signature anyway. One encrypted message survived: “Qassem. The first node is lost.” I cannot yet identify the sender.","اعتبارنامه لغو شده بود، اما کسی باز هم از امضای آن استفاده کرده است. یک پیام رمزگذاری‌شده باقی مانده: «قاسم. گرهٔ اول از دست رفت.» هنوز نمی‌توانم فرستنده را شناسایی کنم."),
            new("debrief-03",NarrativeSpeakerId.Dalia,"First Response is complete. The reconnaissance team and armor parts are assigned to your roster. Review your results, then return to the campaign map. We will follow the evidence.","فصل واکنش نخست کامل شد. تیم شناسایی و قطعات زره به تجهیزات شما اضافه شدند. نتیجه را مرور کنید و به نقشهٔ کارزار برگردید. مسیر شواهد را دنبال می‌کنیم.")};
        public static readonly (string Title,string PersianTitle,string Body,string PersianBody,string Example,string PersianExample,string Mistake,string PersianMistake)[] Lessons={
            ("Read the assault plan","طرح حمله را بخوانید","Break the gate, destroy the transmitter, then secure the archive for 20 seconds. You have 12 minutes. Keep the APC alive and finish under 9 minutes for bonus stars.","دروازه را بشکنید، فرستنده را نابود کنید و سپس بایگانی را ۲۰ ثانیه امن نگه دارید. ۱۲ دقیقه فرصت دارید. حفظ نفربر و پایان در کمتر از ۹ دقیقه ستارهٔ اضافی دارند.","Press Continue when ready.","وقتی آماده‌اید ادامه را بزنید.","The APC is a bonus objective, not your only way to win.","نفربر هدف امتیازی است، نه تنها راه پیروزی."),
            ("Select your assault unit","نیروی حمله را انتخاب کنید","Select the heavy APC. If it is lost, use a surviving rifle squad. Show Me marks the unit to select.","نفربر سنگین را انتخاب کنید. اگر از دست رفته، از تفنگدارهای باقی‌مانده استفاده کنید. نشانم بده، نیروی مورد نظر را مشخص می‌کند.","Tap the marked unit.","روی نیروی مشخص‌شده بزنید.","Orders require a selected friendly unit.","برای صدور فرمان باید نیروی خودی انتخاب شده باشد."),
            ("Breach the road gate","دروازهٔ جاده را بشکنید","Press Attack, then tap the marked enemy gate. Wait for it to fall. Show Me follows your next required click.","حمله را بزنید و سپس دروازهٔ مشخص‌شدهٔ دشمن را انتخاب کنید. صبر کنید تا نابود شود. نشانم بده، کلیک بعدی را مشخص می‌کند.","Keep the APC supported by the rifles.","تفنگدارها از نفربر پشتیبانی کنند.","Move orders do not attack the gate.","فرمان حرکت به دروازه حمله نمی‌کند."),
            ("Bring the team through","تیم را از دروازه عبور دهید","Select your rifles, press Move, and tap the marked approach beyond the gate. Keep the team together; enemy reinforcements are responding.","تفنگدارها را انتخاب کنید، حرکت را بزنید و مقصد مشخص‌شده در آن سوی دروازه را انتخاب کنید. تیم را کنار هم نگه دارید؛ نیروی کمکی دشمن در راه است.","Move into the open area by the relay.","به فضای باز کنار مرکز ارتباطی بروید.","Do not leave the rifles behind while armor fights alone.","تفنگدارها را عقب نگذارید تا زره‌پوش تنها بجنگد."),
            ("Silence the transmitter","فرستنده را خاموش کنید","Select a friendly unit, press Attack, then tap the marked satellite transmitter. Destroying it opens the archive objective.","یک نیروی خودی را انتخاب کنید، حمله را بزنید و فرستندهٔ ماهواره‌ای مشخص‌شده را انتخاب کنید. نابودی آن هدف بایگانی را باز می‌کند.","Concentrate fire on the marked transmitter.","آتش را روی فرستندهٔ مشخص‌شده متمرکز کنید.","Unrelated airport buildings are not objectives.","ساختمان‌های دیگر فرودگاه هدف مأموریت نیستند."),
            ("Stop the counterattack","ضدحمله را متوقف کنید","Attack the remaining guards. Show Me marks a live enemy. You must defeat the garrison and its reinforcements before securing the archive.","به نگهبان‌های باقی‌مانده حمله کنید. نشانم بده، دشمن زنده را مشخص می‌کند. پیش از امن‌کردن بایگانی باید پادگان و نیروی کمکی را شکست دهید.","Use the APC and rifles together.","از نفربر و تفنگدارها با هم استفاده کنید.","The archive cannot be secured while the guard is still active.","تا وقتی نگهبان‌ها فعال‌اند، بایگانی امن نمی‌شود."),
            ("Reach the archive","به بایگانی برسید","Select a surviving unit, press Move, then tap the marked archive area. Keep at least one unit there while the records are recovered.","یک نیروی باقی‌مانده را انتخاب کنید، حرکت را بزنید و محدودهٔ مشخص‌شدهٔ بایگانی را انتخاب کنید. هنگام بازیابی اسناد دست‌کم یک نیرو در آنجا نگه دارید.","The secure timer starts when the area is clear.","زمان‌سنج امنیت پس از پاک‌سازی محل آغاز می‌شود.","Leaving the area resets the twenty-second hold.","خروج از محدوده شمارش بیست‌ثانیه‌ای را از نو آغاز می‌کند."),
            ("Protect the recovered records","از اسناد بازیابی‌شده محافظت کنید","Hold the archive area for 20 uninterrupted seconds. Keep a survivor in the marked area until ARIA confirms recovery.","محدودهٔ بایگانی را ۲۰ ثانیهٔ پیوسته حفظ کنید. تا تأیید بازیابی توسط آریا، یک نیروی زنده در محل نگه دارید.","Wait for the recovery confirmation.","منتظر تأیید بازیابی بمانید.","A destroyed transmitter alone does not complete the mission.","نابودی فرستنده به‌تنهایی مأموریت را کامل نمی‌کند.")};
        public static readonly (string Key,string English,string Persian)[] Ui={
            ("mission.m05.guide.title","FIELD GUIDE · BREACH ASSAULT","راهنمای میدانی · تهاجم نفوذی"),
            ("mission.m05.name","BREACH ASSAULT","تهاجم نفوذی"),
            ("mission.m05.summary","Breach the relay gate, silence its transmitter, and recover the archive.","دروازهٔ مرکز ارتباطی را بشکنید، فرستنده را خاموش کنید و بایگانی را بازیابی کنید."),
            ("mission.m05.location","Eastern district · fortified relay","بخش شرقی · مرکز ارتباطی مستحکم"),
            ("mission.m05.enemy_intel","Garrison infantry; reinforcements respond 15 seconds after the gate takes damage.","پادگان پیاده‌نظام؛ نیروی کمکی ۱۵ ثانیه پس از آسیب به دروازه واکنش نشان می‌دهد."),
            ("mission.m05.resources","Mission-supplied heavy APC; no construction required","نفربر سنگین با ذخیرهٔ مأموریت؛ ساخت‌وساز لازم نیست"),
            ("mission.m05.access","8 rifles · heavy APC","۸ تفنگدار · نفربر سنگین"),
            ("mission.m05.options","Breach together; preserve the armor; recover the records","با هم نفوذ کنید؛ زره‌پوش را حفظ کنید؛ اسناد را بازیابی کنید"),
            ("mission.m05.objective.gate","Destroy the road gate","دروازهٔ جاده را نابود کنید"),
            ("mission.m05.objective.core","Disable the transmitter","فرستنده را از کار بیندازید"),
            ("mission.m05.objective.archive","Secure the archive for 20 seconds","بایگانی را ۲۰ ثانیه امن نگه دارید"),
            ("mission.guide.lesson_count","{0} lessons","{0} درس"),
            ("mission.m05.guide.availability.0","Friendly in M5","خودی در مأموریت پنجم"),
            ("mission.m05.guide.availability.1","Hostile in M5","دشمن در مأموریت پنجم"),
            ("mission.m05.guide.availability.2","Protected civilian in M5","غیرنظامی محافظت‌شده در مأموریت پنجم"),
            ("mission.m05.guide.availability.3","Reference only · unavailable in M5","فقط برای آشنایی · در مأموریت پنجم در دسترس نیست"),
            ("mission.m05.objective.gate.body","Attack the marked gate to open the road.","به دروازهٔ مشخص‌شده حمله کنید تا جاده باز شود."),
            ("mission.m05.objective.core.body","After the breach, destroy the marked satellite transmitter.","پس از نفوذ، فرستندهٔ ماهواره‌ای مشخص‌شده را نابود کنید."),
            ("mission.m05.objective.archive.body","Defeat all guards and hold the marked area for 20 seconds.","همهٔ نگهبان‌ها را شکست دهید و محدودهٔ مشخص‌شده را ۲۰ ثانیه حفظ کنید."),
            ("mission.m05.star.1","Recover the archive","بایگانی را بازیابی کنید"),
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
            ("mission.m05.result.timeout","The archive was erased. Retry and reach the relay before twelve minutes.","بایگانی پاک شد. دوباره تلاش کنید و پیش از دوازده دقیقه به مرکز برسید."),
            ("mission.m05.result.loss","The assault force was lost. Retry with the rifles supporting the APC.","نیروی حمله از دست رفت. دوباره تلاش کنید و با تفنگدارها از نفربر پشتیبانی کنید."),
            ("mission.m05.result.setup","The assault could not be prepared. Retry the mission.","آماده‌سازی حمله کامل نشد. مأموریت را دوباره آغاز کنید."),
            ("mission.m05.result.chapter","CHAPTER ONE COMPLETE","فصل اول کامل شد"),
            ("mission.m05.tutorial.4.fallback","Your rifles are lost. Select the surviving APC, press Move, then tap the marked approach beyond the gate.","تفنگدارها از دست رفته‌اند. نفربر زنده را انتخاب کنید، حرکت را بزنید و مقصد مشخص‌شده پشت دروازه را انتخاب کنید."),
            ("mission.m05.result.unit_losses","ASSAULT UNITS LOST","نیروهای ازدست‌رفته"),
            ("mission.m05.result.support","HEAVY APC","نفربر سنگین"),
            ("mission.m05.result.lost","LOST","از دست رفت"),
            ("mission.m05.result.survived","SURVIVED","سالم"),
            ("mission.m05.hud.status","Archive {0}/{3}s · Guards {1} · {2} remaining","بایگانی: {0} از {3} ثانیه · نگهبان‌ها: {1} · زمان: {2}"),
            ("mission.m05.hud.counterattack","Enemy reinforcements in {0}s","نیروی کمکی دشمن: {0} ثانیه دیگر")};
    }
}
