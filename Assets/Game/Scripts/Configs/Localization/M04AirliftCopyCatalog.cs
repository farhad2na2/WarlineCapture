using Game.Catalog.Contracts;
namespace Game.Configs
{
    public readonly struct M04NarrativeLine
    {
        public readonly string Id,Key,English,Persian;public readonly NarrativeSpeakerId Speaker;
        public M04NarrativeLine(string id,NarrativeSpeakerId speaker,string en,string fa){Id="m04-"+id;Key="narrative.m04."+id.Replace('-','.');Speaker=speaker;English=en;Persian=fa;}
    }
    public static class M04AirliftCopyCatalog
    {
        public static readonly M04NarrativeLine[] Brief={
            new("brief-01",NarrativeSpeakerId.Laila,"Captain Laila Nasser. My helicopter is ready at the eastern landing area. Bring the four specialists to me in the APC; I can keep this extraction window open for ten minutes.","سروان لیلا ناصر هستم. بالگردم در محل فرود شرقی آماده است. چهار متخصص را با نفربر به من برسانید؛ فرصت تخلیه را ده دقیقه باز نگه می‌دارم."),
            new("brief-02",NarrativeSpeakerId.Samira,"Two medics and two engineers are stranded by the western road. They are the people who can reopen the clinics and restore the network. Bring every one of them home.","دو امدادگر و دو مهندس کنار جادهٔ غربی گرفتار شده‌اند. آن‌ها می‌توانند درمانگاه‌ها را باز کنند و شبکه را بازگردانند. همه را سالم برگردانید."),
            new("brief-03",NarrativeSpeakerId.Dalia,"Rifles cover the road; the APC carries the team. Unload beside Laila, board the helicopter, hold the area clear for twenty seconds, then fly to the marked departure zone.","تفنگدارها جاده را پوشش می‌دهند؛ نفربر تیم را منتقل می‌کند. کنار لیلا پیاده شوید، سوار بالگرد شوید، بیست ثانیه محل را امن نگه دارید و سپس به محدودهٔ خروج پرواز کنید.")};
        public static readonly M04NarrativeLine[] Comms={new("comms-01",NarrativeSpeakerId.Laila,"All four are aboard. The landing area is clear and departure is authorized. Fly to the marked exit; I will take the team from there.","هر چهار نفر سوار شده‌اند. محل فرود امن است و اجازهٔ خروج داریم. به محل خروج مشخص‌شده پرواز کنید؛ از آنجا تیم را منتقل می‌کنم.")};
        public static readonly M04NarrativeLine[] Debrief={
            new("debrief-01",NarrativeSpeakerId.Laila,"Four passengers accounted for. Your escort gave us the time we needed. I am joining this operation; you will have an airlift pilot when the next team needs a way out.","هر چهار مسافر حاضرند. پوشش شما زمان لازم را فراهم کرد. به این عملیات می‌پیوندم؛ از این پس برای نجات تیم بعدی یک خلبان ترابری هوایی خواهید داشت."),
            new("debrief-02",NarrativeSpeakerId.Samira,"The engineers brought their repair records. Several outages lead back to the same fortified communications node. We finally have a place to investigate.","مهندسان سوابق تعمیر را آورده‌اند. چند قطعی به یک گرهٔ ارتباطی مستحکم برمی‌گردد. سرانجام محلی برای بررسی داریم."),
            new("debrief-03",NarrativeSpeakerId.Aria,"Extraction confirmed. Captain Nasser is on the roster. Next objective: assess that communications node. First, let the medical team get back to work.","تخلیه تأیید شد. سروان ناصر به گروه پیوست. هدف بعدی: بررسی آن گرهٔ ارتباطی. ابتدا بگذارید تیم پزشکی به کارش برگردد.")};
        public static readonly (string Title,string PersianTitle,string Body,string PersianBody,string Example,string PersianExample,string Mistake,string PersianMistake)[] Lessons={
            ("A rescue, in two stages","نجات در دو مرحله","Rescue all four specialists: APC first, helicopter second. You have ten minutes. Enemy kills are optional; keep the escort alive for a bonus star.","هر چهار متخصص را نجات دهید: ابتدا نفربر، سپس بالگرد. ده دقیقه فرصت دارید. کشتن دشمن الزامی نیست؛ سالم نگه‌داشتن محافظان یک ستارهٔ اضافی دارد.","Read the route, then continue.","مسیر را بخوانید و ادامه دهید.","Do not leave a specialist behind.","هیچ متخصصی را جا نگذارید."),
            ("Select the APC","نفربر را انتخاب کنید","Select the wheeled APC beside your rifles. Keep its four seats free for the specialists. Show Me locates the APC.","نفربر چرخ‌دار کنار تفنگدارها را انتخاب کنید. چهار صندلی آن را برای متخصصان خالی نگه دارید. نشانم بده، محل نفربر را نشان می‌دهد.","Select the APC before giving its movement order.","پیش از فرمان حرکت، نفربر را انتخاب کنید.","A selected rifle squad is not the transport.","گروه تفنگدار انتخاب‌شده نفربر نیست."),
            ("Reach the stranded team","به تیم گرفتار برسید","Press Move, then tap beside the four specialists on the western road. Show Me locates them. Send the rifles forward to cover the pickup.","حرکت را بزنید و کنار چهار متخصص در جادهٔ غربی مقصد بدهید. نشانم بده، محل آن‌ها را نشان می‌دهد. تفنگدارها را برای پوشش به جلو بفرستید.","The road gives a clear approach for the APC.","جاده مسیر مناسبی برای نزدیک‌شدن نفربر است.","Do not drive through buildings or leave the APC in enemy fire.","از میان ساختمان‌ها عبور نکنید و نفربر را زیر آتش رها نکنید."),
            ("Select the specialists","متخصصان را انتخاب کنید","Select all four specialists beside the road. Keep armed escorts outside the selection. This step completes when all four are selected.","هر چهار متخصص کنار جاده را انتخاب کنید. محافظان مسلح را وارد انتخاب نکنید. با انتخاب هر چهار نفر این مرحله تمام می‌شود.","Use a selection box around the four people.","کادر انتخاب را دور هر چهار نفر بکشید.","Keep armed escorts out of this selection.","محافظان مسلح را وارد این انتخاب نکنید."),
            ("Board all four into the APC","هر چهار نفر را سوار نفربر کنید","Press Board, then tap the APC. Wait until its passenger list shows all four specialists before moving.","سوارشدن را بزنید و سپس نفربر را انتخاب کنید. پیش از حرکت صبر کنید فهرست مسافران، هر چهار متخصص را نشان دهد.","Check the passenger count before moving.","پیش از حرکت تعداد مسافران را بررسی کنید.","Moving too soon can leave a passenger walking behind.","حرکت زودهنگام ممکن است یک مسافر را جا بگذارد."),
            ("Escort the APC to Laila","نفربر را تا لیلا همراهی کنید","Move the loaded APC east to the marked landing area. Show Me locates Laila. Keep the rifles covering the approaching patrol.","نفربر پر را به محدودهٔ فرود مشخص‌شده در شرق ببرید. نشانم بده، محل لیلا را نشان می‌دهد. تفنگدارها مسیر گشتی دشمن را پوشش دهند.","Keep the passenger count at four during the trip.","در طول مسیر تعداد مسافران را روی چهار نگه دارید.","Do not leave the escort behind the convoy.","محافظان را پشت کاروان جا نگذارید."),
            ("Unload beside the helicopter","کنار بالگرد پیاده شوید","Open the APC passenger panel, then tap Unload. Wait until all four specialists stand beside the helicopter. Move into open ground if unloading is blocked.","بخش مسافران نفربر را باز کنید و پیاده‌شدن را بزنید. صبر کنید هر چهار متخصص کنار بالگرد بایستند. اگر محل بسته است، نفربر را به فضای باز ببرید.","Unload close to the helicopter while the rifles cover.","با پوشش تفنگدارها نزدیک بالگرد پیاده شوید.","A rejected unload leaves passengers safely aboard; it does not transfer them.","ردشدن فرمان پیاده‌شدن مسافران را در نفربر نگه می‌دارد؛ آن‌ها منتقل نمی‌شوند."),
            ("Inspect the helicopter","بالگرد را بررسی کنید","Select Laila’s helicopter. Keep it landed beside the specialists until all four have boarded.","بالگرد لیلا را انتخاب کنید. آن را کنار متخصصان روی زمین نگه دارید تا هر چهار نفر سوار شوند.","Keep it at the landing area until all four are aboard.","تا سوارشدن هر چهار نفر آن را در محل فرود نگه دارید.","An airborne helicopter cannot instantly collect a passenger.","بالگرد در حال پرواز نمی‌تواند مسافر را فوری سوار کند."),
            ("Transfer the specialists","متخصصان را منتقل کنید","Select all four specialists, press Board, then tap the landed helicopter. Check that its passenger list shows four people.","هر چهار متخصص را انتخاب کنید، سوارشدن را بزنید و بالگرد فرودآمده را انتخاب کنید. فهرست مسافران باید چهار نفر را نشان دهد.","Board the team, not the escort rifle squads.","تیم متخصصان را سوار کنید، نه تفنگدارهای محافظ را.","Flying straight to the team skips the required APC leg and will not complete the mission.","پرواز مستقیم به سمت تیم، مرحلهٔ نفربر را حذف می‌کند و مأموریت را کامل نمی‌کند."),
            ("Hold the landing area clear","محل فرود را امن نگه دارید","Keep the loaded helicopter inside the marked landing area for twenty clear seconds. Use the rifles to stop threats entering the ring. Leaving or enemy entry resets the count.","بالگرد پر را بیست ثانیه در محدودهٔ امن فرود نگه دارید. تفنگدارها مانع ورود دشمن به حلقه شوند. خروج بالگرد یا ورود دشمن زمان را از نو آغاز می‌کند.","Wait for Laila's departure clearance.","منتظر اجازهٔ خروج لیلا بمانید.","Taking off early does not award extraction progress.","بلندشدن زودهنگام پیشرفت تخلیه را ثبت نمی‌کند."),
            ("Fly to the departure zone","به محدودهٔ خروج پرواز کنید","Select the helicopter, press Move, then tap the marked exit. Show Me locates the exit. Keep all four specialists aboard until extraction is confirmed.","بالگرد را انتخاب کنید، حرکت را بزنید و به خروج مشخص‌شده مقصد بدهید. نشانم بده، خروج را نشان می‌دهد. تا تأیید نجات، هر چهار متخصص سوار بمانند.","Cross the departure area with the helicopter airborne.","با بالگرد در حال پرواز وارد محدودهٔ خروج شوید.","The APC reaching the exit does not count as an airlift.","رسیدن نفربر به خروج، تخلیهٔ هوایی محسوب نمی‌شود."),
            ("Review your rescue","نجات را مرور کنید","Extraction is being confirmed. Keep the passengers aboard and protect the helicopter until the result appears.","نجات در حال تأیید است. تا نمایش نتیجه، مسافران را سوار نگه دارید و از بالگرد محافظت کنید.","Laila joins the campaign after the first successful extraction.","لیلا پس از نخستین تخلیهٔ موفق به کارزار می‌پیوندد.","Failure never grants rescue rewards or shows a successful departure.","شکست پاداش نجات نمی‌دهد و خروج موفق نشان نمی‌دهد.")};
        public static readonly (string Key,string English,string Persian)[] Ui={
            ("mission.m04.camera.opening","RESCUE ROUTE\nReviewing the pickup and landing areas. ARIA starts when the camera returns.","مسیر نجات\nدوربین محل سوارشدن و فرود را نشان می‌دهد. آموزش آریا پس از بازگشت دوربین آغاز می‌شود."),
            ("mission.m04.camera.victory","AIRLIFT COMPLETE\nAll four specialists are safe. Laila is departing.","نجات کامل شد\nهر چهار متخصص سالم‌اند. لیلا در حال خروج است."),
            ("mission.m04.specialist.name","Specialist","متخصص"),
            ("mission.m04.specialist.role","Rescue passenger","مسافر نجات"),
            ("mission.m04.specialist.description","Rescue passenger. Keep all four specialists safe.","مسافر نجات؛ هر چهار متخصص را سالم نگه دارید."),
            ("mission.m04.result.star_objectives","STAR OBJECTIVES","اهداف ستاره‌ها"),
            ("mission.m04.patrol.warning","Enemy patrol moves in {0}s","حرکت گشت دشمن: {0} ثانیه دیگر"),
            ("mission.m04.patrol.active","Enemy patrol advancing · Protect the team","گشت دشمن در حال پیشروی است · از تیم محافظت کنید"),
            ("mission.m04.action.passengers","Open passengers","باز کردن مسافران"),
            ("mission.m04.result.rescued_stat","SPECIALISTS RESCUED","متخصصان نجات‌یافته"),
            ("mission.m04.result.star.complete","Rescue all four","نجات هر چهار نفر"),
            ("mission.m04.result.star.escort","No escort losses","بدون تلفات محافظان"),
            ("mission.m04.result.star.time","Under 7 minutes","کمتر از ۷ دقیقه"),
            ("mission.m04.result.earned","EARNED","دریافت شد"),
            ("mission.m04.result.missed","NOT EARNED","دریافت نشد"),
            ("mission.m04.name","AIRLIFT","تخلیهٔ هوایی"),("mission.m04.summary","Rescue four specialists by APC, then escort their helicopter out.","چهار متخصص را با نفربر نجات دهید و سپس بالگردشان را تا خروج همراهی کنید."),
            ("mission.m04.location","Eastern district · medical landing area","بخش شرقی · محل فرود امدادی"),("mission.m04.enemy_intel","A western patrol moves 15 seconds after boarding, or after two minutes.","گشت غربی ۱۵ ثانیه پس از سوار شدن تیم، یا پس از دو دقیقه حرکت می‌کند."),
            ("mission.m04.resources","Mission-supplied APC and helicopter","نفربر و بالگرد با ذخیرهٔ مأموریت"),("mission.m04.access","8 rifles · APC · helicopter · 4 specialists","۸ تفنگدار · نفربر · بالگرد · ۴ متخصص"),
            ("mission.m04.options","Cover the approach, board safely, leave together","مسیر را پوشش دهید، امن سوار شوید، با هم خارج شوید"),
            ("mission.m04.objective.extract","Extract all four specialists","هر چهار متخصص را تخلیه کنید"),("mission.m04.objective.transport","Protect the APC and helicopter","نفربر و بالگرد را حفظ کنید"),("mission.m04.objective.landing","Hold the landing area clear for 20 seconds","محل فرود را ۲۰ ثانیه امن نگه دارید"),
            ("mission.m04.objective.extract.body","APC → helicopter → departure zone; every specialist must survive.","نفربر، سپس بالگرد، سپس خروج؛ همهٔ متخصصان باید زنده بمانند."),
            ("mission.m04.objective.transport.body","Keep the APC until transfer and the helicopter through departure.","نفربر را تا پایان انتقال و بالگرد را تا خروج حفظ کنید."),
            ("mission.m04.objective.landing.body","All four aboard; keep the helicopter inside the clear landing area.","هر چهار نفر سوار باشند؛ بالگرد را در محل فرود امن نگه دارید."),
            ("mission.m04.star.1","All four extracted","هر چهار نفر تخلیه شدند"),("mission.m04.star.2","No escort losses","بدون تلفات محافظان"),("mission.m04.star.3","Finish within 7 minutes","پایان در ۷ دقیقه"),
            ("mission.m04.reward.laila","Captain Laila Nasser joins","پیوستن سروان لیلا ناصر"),("mission.m04.reward.transport","Transport access","دسترسی به ترابری"),
            ("mission.m04.reward.card","500 XP | 2,500 CREDITS | LAILA | TRANSPORT","۵۰۰ تجربه | ۲۵۰۰ اعتبار | لیلا | ترابری"),
            ("mission.m04.hud.aboard","SPECIALISTS ABOARD","متخصصان سوار"),
            ("mission.m04.hud.carrier","APC TRANSFER","انتقال با نفربر"),
            ("mission.m04.hud.secure","LANDING AREA CLEAR","امنیت محل فرود"),
            ("mission.m04.hud.remaining","TIME REMAINING","زمان باقی‌مانده"),
            ("mission.m04.hud.route","APC → LANDING AREA → DEPARTURE","نفربر ← محل فرود ← خروج"),
            ("mission.m04.hud.status","Aboard {0}/4 · APC leg {1}/4 · Clear {2}/20s · {3}s left","سوار: {0} از ۴ · مسیر نفربر: {1} از ۴ · امن: {2} از ۲۰ ثانیه · زمان: {3} ثانیه"),
            ("mission.m04.hud.contested","LANDING AREA CONTESTED","محل فرود زیر تهدید است"),("mission.m04.hud.cleared","CLEARED FOR DEPARTURE","اجازهٔ خروج صادر شد"),
            ("mission.m04.focus.team","TEAM","تیم"),("mission.m04.focus.landing","LANDING AREA","محل فرود"),("mission.m04.focus.departure","DEPARTURE","خروج"),
            ("mission.m04.result.victory","EXTRACTION COMPLETE","تخلیه کامل شد"),("mission.m04.result.defeat","EXTRACTION FAILED","تخلیه ناموفق بود"),
            ("mission.m04.result.subtitle","AIRLIFT · MEDICAL LANDING ZONE","تخلیه هوایی · محل فرود امدادی"),
            ("mission.m04.result.objectives","RESCUE OBJECTIVES","هدف‌های نجات"),
            ("unit.apc.fast.description","Fast armored personnel carrier for quickly transporting infantry across the battlefield.","نفربر زرهی سریع برای جابه‌جایی پیاده‌نظام در میدان نبرد."),
            ("unit.helicopter.transport.description","Transport helicopter that lands for boarding and deploys soldiers by rope while airborne.","بالگرد ترابری برای سوار کردن نیروها روی زمین و پیاده‌سازی با طناب هنگام پرواز."),
            ("mission.m04.result.extract","Specialists rescued","متخصصان نجات‌یافته"),
            ("mission.m04.result.transport","Transport protection","حفاظت از ترابری"),
            ("mission.m04.result.landing","20-second LZ clearance","۲۰ ثانیه امنیت محل فرود"),
            ("mission.m04.guide.title","FIELD GUIDE · AIRLIFT","راهنمای میدانی · تخلیهٔ هوایی"),
            ("mission.m04.guide.availability.0","Friendly in M4","خودی در مأموریت ۴"),
            ("mission.m04.guide.availability.1","Hostile in M4","دشمن در مأموریت ۴"),
            ("mission.m04.guide.availability.2","Protected specialist in M4","متخصص محافظت‌شده در مأموریت ۴"),
            ("mission.m04.guide.availability.3","Reference only · unavailable in M4","فقط برای آشنایی · در مأموریت ۴ در دسترس نیست"),
            ("mission.m04.guide.role.protected","ROLE · Protected specialist. All four must ride the APC, transfer to the helicopter and leave alive. Losing anyone fails the rescue.","نقش · متخصص محافظت‌شده. هر چهار نفر باید سوار نفربر شوند، به بالگرد منتقل شوند و زنده خارج شوند. از دست دادن حتی یک نفر باعث شکست نجات می‌شود."),
            ("mission.m04.guide.role.transport","ROLE · Transport. Passenger capacity does not imply an onboard weapon. Use the APC for the road leg, unload at the landing area and board all four specialists onto the helicopter.","نقش · ترابری. ظرفیت مسافر به معنی داشتن سلاح نیست. مسیر زمینی را با نفربر طی کنید، در محل فرود پیاده شوید و هر چهار متخصص را سوار بالگرد کنید."),
            ("mission.m04.guide.notice","Use the supplied escorts, APC and helicopter. Reinforcements are unavailable in this mission.","از محافظان، نفربر و بالگرد موجود استفاده کنید. در این مأموریت نیروی کمکی در دسترس نیست."),
            ("mission.m04.result.success","All four specialists reached safety by APC and helicopter. Captain Laila Nasser joins the campaign.","هر چهار متخصص با نفربر و بالگرد به محل امن رسیدند. سروان لیلا ناصر به کارزار می‌پیوندد."),
            ("mission.m04.result.passenger_lost","A specialist was lost. Keep all four protected during boarding and transfer.","یک متخصص از دست رفت. هنگام سوارشدن و انتقال از هر چهار نفر محافظت کنید."),
            ("mission.m04.result.aircraft_lost","The helicopter was destroyed. Protect it until all four reach the departure zone.","بالگرد نابود شد. تا رسیدن هر چهار نفر به محل خروج از آن محافظت کنید."),
            ("mission.m04.result.carrier_lost","The APC was lost before the team completed the transfer.","نفربر پیش از پایان انتقال تیم از دست رفت."),
            ("mission.m04.result.timeout","The ten-minute extraction window closed before departure.","فرصت ده‌دقیقه‌ای تخلیه پیش از خروج پایان یافت."),
            ("mission.m04.result.integrity","The rescue roster could not be verified. Retry the operation.","فهرست تیم نجات قابل تأیید نبود. مأموریت را دوباره آغاز کنید."),
            ("mission.m04.result.escort_lost","The escort was lost. Keep at least one rifle soldier alive until departure.","محافظان از دست رفتند. دست‌کم یک تفنگدار را تا خروج زنده نگه دارید."),
            ("mission.m04.result.failure","Extraction failed. Review the passenger and transport status, then retry.","تخلیه ناموفق بود. وضعیت مسافران و وسایل را بررسی کنید و دوباره تلاش کنید."),
            ("narrative.speaker.laila.name","Captain Laila Nasser","سروان لیلا ناصر"),("narrative.speaker.laila.role","Airlift pilot","خلبان ترابری هوایی")};
    }
}
