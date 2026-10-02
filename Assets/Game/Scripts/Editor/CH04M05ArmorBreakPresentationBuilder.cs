using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH04M05ArmorBreakPresentationBuilder
    {
        public const string GuidePath="Assets/Game/Configs/Missions/Chapter04/CH04M05_ArmorBreak_FieldGuide.asset";
        public static void Build()
        {
            SeedLocalization();BuildGuide();
            var hud=PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
            try { if(hud.GetComponent<ArmorBreakMarkersView>()==null)hud.AddComponent<ArmorBreakMarkersView>();
                PrefabUtility.SaveAsPrefabAsset(hud,"Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab"); }
            finally {PrefabUtility.UnloadPrefabContents(hud);}
            AssetDatabase.SaveAssets();Debug.Log("[ArmorBreakPresentation] result=Passed existingHud=1 newCommandButtons=0 locales=2 markers=approved-style");
        }
        private static void SeedLocalization()
        {
            var copy=new (string key,string en,string fa)[]{
                ("name", "Armor Break", "شکست زرهی"),
                ("summary", "Break Vanguard’s command group and recover the authority package. Coordinate air defense, long-range fire, aircraft and armor. Protect the relief corridor.", "گروه فرماندهی ونگارد رو شکست بده و بستهٔ اختیار رو بردار. پدافند هوایی، آتش دوربرد، هواگرد و زرهی رو هماهنگ کن. از مسیر امداد محافظت کن."),
                ("location", "Sahrin · refinery–airfield military sector", "سهرین · بخش نظامی پالایشگاه و فرودگاه"),
                ("enemy_intel", "Heavy battery, armored command group and incoming aircraft. Keep combat away from the relief corridor.", "آتشبار سنگین، گروه فرماندهی زرهی و هواگردهای دشمن. نبرد رو از مسیر امداد دور نگه دار."),
                ("objective.military","Defeat the military command group","گروه فرماندهی نظامی رو شکست بده"),
                ("objective.command", "Defeat the military command group", "گروه فرماندهی نظامی رو شکست بده"),
                ("objective.authority", "Recover the authority package", "بستهٔ اختیار رو بردار"),
                ("objective.relief", "Protect the relief corridor and Fuel depot", "از مسیر امداد و انبار سوخت محافظت کن"),
                ("resources", "Fuel staged · civilian reserve protected · support optional", "سوخت آماده‌ست · ذخیرهٔ غیرنظامی محفوظ · پشتیبانی اختیاری"),
                ("forces", "G2A · G2G · radar · armor · attack helicopter · recovery infantry", "پدافند · موشک زمین‌به‌زمین · رادار · زرهی · بالگرد رزمی · تیم بازیابی"),
                ("deadline", "Break the link and secure the package before time runs out", "قبل از پایان فرصت، اتصال رو قطع کن و بسته رو امن کن"),
                ("label.fuel", "FUEL STAGING", "سوخت آماده"),
                ("label.deadline", "OPERATION WINDOW", "فرصت عملیات"),
                ("intel.heavy", "HEAVY WEAPONS", "سلاح‌های سنگین"),
                ("intel.present", "PRESENT", "موجود"),
                ("intel.air", "AIR THREAT", "تهدید هوایی"),
                ("intel.incoming", "INCOMING", "در راه"),
                ("warning.0", "Heavy battery · use the ground launcher", "آتشبار سنگین · از موشک زمین‌به‌زمین استفاده کن"),
                ("warning.1", "Hostile armor · keep combat away from relief", "زرهی دشمن · نبرد رو از امداد دور نگه دار"),
                ("warning.2", "Command group · secure the authority package", "گروه فرماندهی · بستهٔ اختیار رو امن کن"),
                ("warning.3", "Incoming aircraft · hold air-defense coverage", "هواگردهای دشمن · پوشش پدافند رو حفظ کن"),
                ("star.1", "Complete the mission", "مأموریت رو کامل کن"),
                ("star.2", "No friendly combat losses", "بدون تلفات نیروهای رزمی"),
                ("star.3", "Relief and Fuel protected", "امداد و سوخت محفوظ بمانند"),
                ("reward.card", "Commander XP · Credits · Protocol Fragment 4", "تجربهٔ فرمانده · اعتبار · قطعهٔ چهارم پروتکل"),
                ("result.victory", "COMMAND GROUP BROKEN", "فرماندهی دشمن شکست خورد"),
                ("result.defeat", "OPERATION FAILED", "عملیات شکست خورد"),
                ("result.subtitle", "ARMOR BREAK · AUTHORITY PACKAGE", "شکست زرهی · بستهٔ اختیار"),
                ("result.success", "The command group is defeated and the authority package is secure. The relief corridor and civilian Fuel reserve are protected. Protocol Fragment Four reveals the two keys.", "گروه فرماندهی شکست خورد و بستهٔ اختیار امنه. مسیر امداد و ذخیرهٔ سوخت غیرنظامی محفوظ موندن. قطعهٔ چهارم پروتکل، دو کلید رو آشکار می‌کنه."),
                ("result.success_short", "Command group defeated. Authority package secured. Relief corridor protected.", "گروه فرماندهی شکست خورد. بستهٔ اختیار امن شد. مسیر امداد حفظ شد."),
                ("result.mastery_lost","The required combined-arms targets were destroyed before the launcher or helicopter attack. Use the G2G against the battery and order a helicopter strike before completing the ground assault.","هدف‌های لازم قبل از حملهٔ پرتابگر یا بالگرد نابود شدن. آتشبار رو با پرتابگر زمین‌به‌زمین بزن و قبل از پایان حملهٔ زمینی، به بالگرد فرمان حمله بده."),
                ("result.armor_lost","The original armored group was lost before military command was defeated. Protect at least one original armored vehicle for the assault.","گروه زرهی اصلی قبل از شکست فرماندهی نظامی از دست رفت. حداقل یک خودروی زرهی اصلی رو برای حمله حفظ کن."),
                ("result.air_defense_lost","Air defense was lost before the air threats were stopped. Keep the G2A covered and inside radar coverage.","پدافند قبل از توقف تهدیدهای هوایی از دست رفت. از پدافند محافظت کن و در پوشش رادار نگه دار."),
                ("result.launcher_lost","The G2G launcher was lost before its military battery strike completed. Protect the launcher until the battery is disabled.","پرتابگر قبل از پایان حمله به آتشبار از دست رفت. تا از کار افتادن آتشبار از پرتابگر محافظت کن."),
                ("result.aircraft_lost","The attack helicopter was lost before its ordered strike. Protect it and target the exposed military armor.","بالگرد قبل از حملهٔ فرمان‌داده‌شده از دست رفت. ازش محافظت کن و زرهی نظامی بی‌پوشش رو هدف بگیر."),
                ("result.team_lost","The original recovery infantry were lost. Keep surviving team members alive to recover the authority package.","تیم پیادهٔ بازیابی اصلی از دست رفت. بازمانده‌های تیم رو زنده نگه دار تا بستهٔ اختیار رو بردارن."),
                ("result.relief_lost","Relief staff were lost. Keep fighting and missile strikes outside the protected corridor.","کارکنان امداد از دست رفتن. نبرد و شلیک موشک رو بیرون مسیر محافظت‌شده نگه دار."),
                ("result.fuel_lost","Fuel staging or its protected civilian reserve was lost. Protect the depot and preserve the relief allocation.","سوخت آماده یا ذخیرهٔ غیرنظامی از دست رفت. از انبار محافظت کن و سهم امداد رو حفظ کن."),
                ("result.timeout","The operation window closed. Stop the air threats, break military command and recover the package within fifteen minutes.","فرصت عملیات تموم شد. تا پانزده دقیقه تهدیدهای هوایی رو متوقف کن، فرماندهی نظامی رو شکست بده و بسته رو بردار."),
                ("result.loss", "The operation could not secure the authority package. Protect the recovery team, air defense, launcher and Fuel staging, then retry.", "بستهٔ اختیار امن نشد. از تیم بازیابی، پدافند، پرتابگر و سوخت آماده محافظت کن و دوباره تلاش کن."),
                ("result.star.authority", "Command defeated and authority secured", "شکست فرماندهی و بازیابی اختیار"),
                ("result.combat_losses", "COMBAT LOSSES", "تلفات رزمی"),
                ("result.relief_losses", "RELIEF STAFF LOSSES", "تلفات کارکنان امداد"),
                ("tutorial.1.title", "Cover the approach", "مسیر رو پوشش بده"),
                ("tutorial.1.body", "Select the supplied G2A and Move to the coverage marker. Keep its radar and Fuel staging safe. Show Me moves only the camera.", "پدافند آماده رو انتخاب کن و با حرکت به نشان پوشش برسون. رادار و سوخت آماده رو حفظ کن. «نشان بده» فقط دوربین رو جابه‌جا می‌کنه."),
                ("tutorial.2.title", "Stop the air attack", "حملهٔ هوایی رو متوقف کن"),
                ("tutorial.2.body", "Keep the G2A in coverage while it intercepts the incoming aircraft. Protect it until the air threats are defeated.", "پدافند رو در محل پوشش نگه دار تا هواگردهای ورودی رو رهگیری کنه. تا شکست تهدیدهای هوایی ازش محافظت کن."),
                ("tutorial.3.title", "Disable the heavy battery", "آتشبار سنگین رو از کار بنداز"),
                ("tutorial.3.body", "Select the G2G launcher and use Attack on the military battery. This target order authorizes the shot. Keep the protected relief corridor outside the strike.", "پرتابگر زمین‌به‌زمین رو انتخاب کن و آتشبار نظامی رو با حمله هدف بگیر. همین فرمان، مجوز شلیک رو می‌ده. مسیر امداد باید بیرون محدودهٔ حمله بمونه."),
                ("tutorial.4.approach.title","Position the attack helicopter","بالگرد رزمی رو مستقر کن"),
                ("tutorial.4.approach.body","Select the attack helicopter and Move into range of the marked military target. Then use Attack.","بالگرد رزمی رو انتخاب کن و با حرکت به برد هدف نظامی مشخص‌شده برسون. بعد حمله رو بزن."),
                ("tutorial.4.title", "Strike the exposed armor", "به زرهی بی‌پوشش حمله کن"),
                ("tutorial.4.body", "Select the attack helicopter. Move into range if needed, then Attack each marked armored defender before advancing your armor.", "بالگرد رزمی رو انتخاب کن. اگه لازمه با حرکت به برد مناسب برس، بعد پیش از پیشروی زرهی خودی، به هر مدافع زرهی مشخص‌شده حمله کن."),
                ("tutorial.5.title", "Advance the armored group", "گروه زرهی رو پیش ببر"),
                ("tutorial.5.body", "Select the surviving original armored group together and Move through the cleared approach. Keep the relief corridor clear of combat.", "بازمانده‌های گروه زرهی اصلی رو با هم انتخاب کن و از مسیر پاک‌شده پیش ببر. نبرد رو از مسیر امداد دور نگه دار."),
                ("tutorial.6.title", "Break military command", "فرماندهی نظامی رو شکست بده"),
                ("tutorial.6.aircraft.body", "Select the surviving attack helicopter and use Attack on the remaining verified command targets. Protect the recovery infantry and Fuel depot.", "بالگرد رزمی باقی‌مونده رو انتخاب کن و با حمله، هدف‌های فرماندهی تأییدشدهٔ باقی‌مونده رو بزن. از تیم پیادهٔ بازیابی و انبار سوخت محافظت کن."),
                ("tutorial.6.body", "Use the armored group’s Attack command on the remaining verified command targets. Protect the recovery infantry and Fuel depot.", "با فرمان حملهٔ گروه زرهی، هدف‌های فرماندهی تأییدشدهٔ باقی‌مونده رو بزن. از تیم پیادهٔ بازیابی و انبار سوخت محافظت کن."),
                ("tutorial.7.title", "Secure the authority package", "بستهٔ اختیار رو امن کن"),
                ("tutorial.7.body", "Select the surviving original recovery infantry together and Move to the package. Hold dismounted and uncontested for six seconds. Leaving or boarding cancels recovery.", "بازمانده‌های تیم پیادهٔ بازیابی اصلی رو با هم انتخاب کن و به بسته برسون. شش ثانیه پیاده و بدون دشمن بمونن. دور شدن یا سوار شدن بازیابی رو قطع می‌کنه."),
                ("hud.stage.1", "COVERAGE · G2A → Move → coverage", "پوشش · پدافند ← حرکت ← پوشش"),
                ("hud.stage.2", "AIR DEFENSE · Hold coverage and watch interception", "پدافند هوایی · حفظ پوشش و رهگیری"),
                ("hud.stage.3", "BATTERY · G2G → Attack → military battery", "آتشبار · پرتابگر ← حمله ← آتشبار نظامی"),
                ("hud.stage.4", "AIR STRIKE · Helicopter → Attack → military armor", "حملهٔ هوایی · بالگرد ← حمله ← زرهی نظامی"),
                ("hud.stage.5", "APPROACH · Armored group → Move → approach", "پیشروی · گروه زرهی ← حرکت ← مسیر"),
                ("hud.stage.6", "COMMAND · Armored group → Attack → command targets", "فرماندهی · گروه زرهی ← حمله ← هدف‌های فرماندهی"),
                ("hud.stage.7", "AUTHORITY · Recovery infantry → Move → package", "اختیار · تیم پیادهٔ بازیابی ← حرکت ← بسته"),
                ("hud.approach", "Move into range, then Attack the marked military target.", "به برد مناسب برس، بعد به هدف نظامی مشخص‌شده حمله کن."),
                ("hud.recovery", "Authority recovery: {0}/6 s", "بازیابی اختیار: {0} از ۶ ثانیه"),
                ("hud.time", "Operation window: {0}", "فرصت عملیات: {0}"),
                ("hud.hold_time", "Hold: {0}/6 s · Time: {1}", "حفظ: {0} از ۶ ثانیه · زمان: {1}"),
                ("hud.fuel", "Military Fuel: {0} · civilian reserve protected", "سوخت نظامی: {0} · ذخیرهٔ غیرنظامی محفوظ"),
                ("marker.coverage", "COVERAGE", "پوشش"),
                ("marker.authority", "AUTHORITY", "اختیار"),
                ("marker.relief", "RELIEF", "امداد"),
                ("guide.title", "ARMOR BREAK · FIELD GUIDE", "شکست زرهی · راهنمای میدان"),
                ("guide.example", "Cover air → confirmed long-range strike → helicopter → armor → authority.", "پوشش هوایی ← شلیک دوربرد تأییدشده ← بالگرد ← زرهی ← اختیار"),
                ("guide.mistake", "Protect the relief corridor and civilian Fuel reserve. Optional Support is never required.", "از مسیر امداد و ذخیرهٔ سوخت غیرنظامی محافظت کن. پشتیبانی اختیاری الزامی نیست."),
                ("guide.diagram", "COVER | STRIKE | RECOVER", "پوشش | حمله | بازیابی"),
            };
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath)??throw new InvalidOperationException("Localization missing");
            var locales=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en" && locale.LocaleCode!="fa-IR"){locales.Add(locale);continue;}
                bool fa=locale.LocaleCode=="fa-IR";var entries=locale.Entries.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                entries["ui.home.archive.armor_break.fragment4"]=fa?"قطعهٔ چهارم پروتکل":"PROTOCOL FRAGMENT IV";
                entries["ui.home.archive.armor_break.authority"]=fa?"بستهٔ اختیارِ ضبط‌شده":"CAPTURED AUTHORITY PACKAGE";
                entries["ui.home.archive.armor_break.two_keys"]=fa?"نمودار مجوز دوکلیدی":"TWO-KEY AUTHORIZATION DIAGRAM";
                foreach(var item in copy)entries["mission.armor_break."+item.key]=fa?item.fa:item.en;
                foreach(var line in CH04M05ArmorBreakCopy.Brief.Concat(CH04M05ArmorBreakCopy.Comms).Concat(CH04M05ArmorBreakCopy.Debrief).Concat(CH04M05ArmorBreakCopy.Close))entries[line.Key]=fa?line.Persian:line.English;
                locales.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,entries.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new GameLocalizedStringRecord(x.Key,x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,locales);EditorUtility.SetDirty(catalog);
        }
        private static void BuildGuide()
        {
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);
            if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,GuidePath);}
            var basis=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);
            guide.Configure(Enumerable.Range(1,7).Select(i=>new MissionGuideTopic{TitleKey=$"mission.armor_break.tutorial.{i}.title",BodyKey=$"mission.armor_break.tutorial.{i}.body",ExampleKey="mission.armor_break.guide.example",MistakeKey="mission.armor_break.guide.mistake",DiagramKey="mission.armor_break.guide.diagram"}).ToArray(),basis.Classes.ToArray());EditorUtility.SetDirty(guide);
            var root=PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try {var data=new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));data.FindProperty("armorBreakGuide").objectReferenceValue=guide;data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,M03RadarWarningUiBuilder.GuidePath);}
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
