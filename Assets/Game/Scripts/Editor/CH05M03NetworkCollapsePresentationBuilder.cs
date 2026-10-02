using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH05M03NetworkCollapsePresentationBuilder
    {
        public const string GuidePath="Assets/Game/Configs/Missions/Chapter05/CH05M03_NetworkCollapse_FieldGuide.asset";
        public static void Build()
        {
            CH05M03NetworkCollapseNarrativeBuilder.SeedCopyLocalization();SeedLocalization();BuildGuide();AssetDatabase.SaveAssets();
            var hud=PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
            try{if(hud.GetComponent<NetworkCollapseMarkersView>()==null)hud.AddComponent<NetworkCollapseMarkersView>();PrefabUtility.SaveAsPrefabAsset(hud,"Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");}
            finally{PrefabUtility.UnloadPrefabContents(hud);}
            Debug.Log("[NetworkCollapsePresentation] result=Passed existingHud=1 newCommandButtons=0 locales=2 orderedGroundReconFallback=Explicit voices=NotRequested");
        }
        private static void SeedLocalization()
        {
            var copy=new (string key,string en,string fa)[]{
                ("name","Network Collapse","فروپاشی شبکه"),
                ("summary","Confirm three command nodes in order. Preserve civic services, recover the complete audit and extract the evidence team.","سه گره فرماندهی رو به‌ترتیب تأیید کن. خدمات شهری رو حفظ کن، حسابرسی کامل رو بازیابی کن و تیم شواهد رو خارج کن."),
                ("location","Sahrin · civic communications district","سهرین · محلهٔ ارتباطات شهری"),
                ("objective.protection","Preserve civic services and staff","خدمات شهری و کارکنان رو حفظ کن"),
                ("objective.nodes","Verify and disable three nodes","سه گره رو تأیید و غیرفعال کن"),
                ("objective.audit","Recover the complete audit","حسابرسی کامل رو بازیابی کن"),
                ("objective.extraction","Extract the evidence team","تیم شواهد رو خارج کن"),
                ("forces","Two tanks · four riflemen · evidence engineer · APC","دو تانک · چهار سرباز · مهندس شواهد · نفربر"),
                ("resources","80/240 Materials · 100 Fuel · 20 civic Fuel protected","۸۰ از ۲۴۰ مصالح · ۱۰۰ سوخت · ۲۰ سوخت شهری محفوظ"),
                ("label.fuel","FUEL","سوخت"),
                ("label.deadline","DEADLINE","مهلت"),
                ("deadline","Complete within fifteen minutes","تا پانزده دقیقه کامل کن"),
                ("intel.military","VERIFIED MILITARY","نظامیان تأییدشده"),
                ("intel.confirmed","3 + 6","۳ + ۶"),
                ("intel.civilian","CIVIC SERVICES","خدمات شهری"),
                ("intel.protected","2 + 4","۲ + ۴"),
                ("enemy_intel","Three separately verified radar nodes, each guarded by two riflemen. Preserve neutral services and audit custody.","سه گره رادار با دو نگهبان برای هر گره. خدمات بی‌طرف و زنجیرهٔ شواهد رو حفظ کن."),
                ("reward.card","Commander XP · Credits · complete audit evidence","تجربهٔ فرمانده · اعتبار · شواهد حسابرسی کامل"),
                ("reward.audit","Complete audit","حسابرسی کامل"),
                ("star.1","Complete the mission","مأموریت رو کامل کن"),
                ("star.2","No escort losses","بدون تلفات اسکورت"),
                ("star.3","No civilian losses","بدون تلفات غیرنظامیان"),
                ("hud.recon_hold","Verify node {0}: {1}/6 s","تأیید گره {0}: {1} از ۶ ثانیه"),
                ("hud.audit_hold","Recover audit: {0}/6 s","بازیابی حسابرسی: {0} از ۶ ثانیه"),
                ("hud.extraction_hold","Evidence custody: {0}/6 s","حفاظت از شواهد: {0} از ۶ ثانیه"),
                ("hud.time","Evidence window: {0}m {1}s","فرصت شواهد: {0} دقیقه و {1} ثانیه"),
                ("hud.fuel","Fuel: {0} · civic reserve protected","سوخت: {0} · ذخیرهٔ شهری محفوظ"),
                ("marker.audit","AUDIT","حسابرسی"),
                ("marker.extraction","EXTRACTION","خروج"),
                ("guide.example","Clear guards → recon on foot → disable verified node → preserve audit → extract.","پاک‌سازی نگهبان‌ها ← شناسایی پیاده ← غیرفعال‌سازی گره تأییدشده ← حفظ حسابرسی ← خروج"),
                ("guide.mistake","An unverified node is not a valid target. Show Me moves only the camera.","گره تأییدنشده هدف مجاز نیست. «نشان بده» فقط دوربین رو حرکت می‌ده."),
                ("guide.diagram","ORDERED VERIFICATION | CIVIC PROTECTION | AUDIT CUSTODY","تأیید به‌ترتیب | حفظ خدمات شهری | حفاظت از حسابرسی"),
                ("result.victory","AUDIT SECURED","حسابرسی محفوظ"),
                ("result.defeat","EVIDENCE RESPONSE FAILED","عملیات شواهد شکست خورد"),
                ("result.subtitle","NETWORK COLLAPSE · COMPLETE AUDIT","فروپاشی شبکه · حسابرسی کامل"),
                ("result.success","All verified nodes are disabled. Civic services and staff survived; the original evidence engineer delivered the complete audit.","همهٔ گره‌های تأییدشده غیرفعال شدن. خدمات و کارکنان زنده موندن؛ مهندس اصلی حسابرسی کامل رو تحویل داد."),
                ("result.success_short","Three nodes disabled. Complete audit extracted.","سه گره غیرفعال شد. حسابرسی کامل خارج شد."),
                ("result.loss","Preserve the evidence engineer, APC, civic services and audit. Verify nodes in order before attacking.","مهندس شواهد، نفربر، خدمات شهری و حسابرسی رو حفظ کن. پیش از حمله گره‌ها رو به‌ترتیب تأیید کن."),
                ("marker.node1","NODE 1","گره 1"),
                ("marker.recon1","VERIFY 1","تأیید 1"),
                ("marker.node2","NODE 2","گره 2"),
                ("marker.recon2","VERIFY 2","تأیید 2"),
                ("marker.node3","NODE 3","گره 3"),
                ("marker.recon3","VERIFY 3","تأیید 3"),
                ("result.engineer_lost","The original evidence engineer was lost.","مهندس اصلی شواهد از دست رفت."),
                ("result.carrier_lost","The extraction APC was lost.","نفربر خروج از دست رفت."),
                ("result.civic_lost","A protected civic service was destroyed.","خدمات شهری محافظت‌شده نابود شد."),
                ("result.staff_lost","Original civic staff were lost.","کارکنان اصلی خدمات شهری از دست رفتن."),
                ("result.audit_lost","The audit archive was destroyed.","بایگانی حسابرسی نابود شد."),
                ("result.fuel_lost","The civic Fuel reserve was lost.","ذخیرهٔ سوخت شهری از دست رفت."),
                ("result.escort_lost","All escorts were lost while military threats remained.","همهٔ اسکورت‌ها از دست رفتن و تهدیدهای نظامی باقی موندن."),
                ("result.timeout","The fifteen-minute evidence window closed.","فرصت پانزده‌دقیقه‌ای شواهد تموم شد."),
                ("result.unverified","A node was disabled before ordered verification.","گره پیش از تأیید به‌ترتیب غیرفعال شد."),
                ("result.objectives","MISSION OBJECTIVES","اهداف مأموریت"),
                ("result.nodes","Verified nodes","گره‌های تأییدشده"),
                ("result.audit","Complete audit","حسابرسی کامل"),
                ("result.extraction","Evidence extracted","شواهد خارج شد"),
                ("result.escort_losses","Escort losses","تلفات اسکورت"),
                ("result.staff_losses","Staff losses","تلفات کارکنان"),
                ("result.secured","SECURED","محفوظ"),
                ("result.verified","VERIFIED","تأییدشده"),
                ("result.safe","SAFE","امن"),
                ("result.incomplete","INCOMPLETE","ناقص"),
                ("result.losses","LOSSES","تلفات"),
                ("supply.optional","Optional Supply: 40 Materials and 3 Fuel. Collect by moving a ground unit to the crate; the mission works without it.","تدارکات اختیاری: ۴۰ مصالح و ۳ سوخت. یک نیروی زمینی رو به صندوق ببر؛ مأموریت بدونش هم کامل می‌شه."),
                ("result.integrity","The original evidence custody chain could not be established.","زنجیرهٔ اصلی حفاظت از شواهد برقرار نشد.")};
            var stages=new (string en,string fa,string body,string faBody)[]{
                ("Verify node one","گره اول رو تأیید کن","Move the escort into firing position and Attack the first guards. Move the original engineer to the external recon marker; hold six seconds after the guards are defeated.","اسکورت رو مستقر کن و به نگهبان‌های اول حمله کن. مهندس اصلی رو به نشان شناسایی بیرونی ببر؛ پس از شکست نگهبان‌ها شش ثانیه نگه دار."),
                ("Disable the verified first node","گره اول تأییدشده رو غیرفعال کن","Attack only the confirmed first radar node. Preserve the civic service and original staff.","فقط به گره رادار اولِ تأییدشده حمله کن. خدمات شهری و کارکنان اصلی رو حفظ کن."),
                ("Verify node two","گره دوم رو تأیید کن","Clear the second guards, then Move the original engineer to the second recon marker for six seconds.","نگهبان‌های دوم رو پاک‌سازی کن، سپس مهندس اصلی رو شش ثانیه به نشان دوم ببر."),
                ("Disable the verified second node","گره دوم تأییدشده رو غیرفعال کن","Attack the confirmed second radar node. Keep the audit archive intact.","به گره رادار دومِ تأییدشده حمله کن. بایگانی حسابرسی سالم بمونه."),
                ("Verify node three","گره سوم رو تأیید کن","Clear the third guards. Move the original engineer to the third recon marker for six seconds.","نگهبان‌های سوم رو پاک‌سازی کن. مهندس اصلی رو شش ثانیه به نشان سوم ببر."),
                ("Disable the verified third node","گره سوم تأییدشده رو غیرفعال کن","Attack the confirmed third radar node. Preserve both neutral services and the complete audit.","به گره سومِ تأییدشده حمله کن. هر دو خدمت بی‌طرف و حسابرسی کامل رو حفظ کن."),
                ("Recover the complete audit","حسابرسی کامل رو بازیابی کن","Move the original engineer to the external audit gate. Hold six uncontested seconds with the archive intact.","مهندس اصلی رو به دروازهٔ بیرونی حسابرسی ببر. با بایگانی سالم شش ثانیه بدون دشمن نگه دار."),
                ("Extract the evidence team","تیم شواهد رو خارج کن","Board the original engineer into the APC and Move to extraction. Preserve the full audit chain and hold six seconds.","مهندس اصلی رو سوار نفربر کن و به خروج ببر. زنجیرهٔ کامل شواهد رو حفظ کن و شش ثانیه نگه دار.")};
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath)??throw new InvalidOperationException("Localization missing");var locales=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en"&&locale.LocaleCode!="fa-IR"){locales.Add(locale);continue;}
                bool fa=locale.LocaleCode=="fa-IR";var entries=locale.Entries.ToDictionary(e=>e.Key,e=>e.Value,StringComparer.Ordinal);
                foreach(var item in copy)entries["mission.network_collapse."+item.key]=fa?item.fa:item.en;
                for(int i=0;i<stages.Length;i++){var stage=stages[i];entries[$"mission.network_collapse.tutorial.{i+1}.title"]=fa?stage.fa:stage.en;entries[$"mission.network_collapse.tutorial.{i+1}.body"]=fa?stage.faBody:stage.body;entries[$"mission.network_collapse.hud.stage.{i+1}"]=fa?stage.fa:stage.en;}
                foreach(int i in new[]{1,2,3,4,5,6})entries[$"mission.network_collapse.tutorial.{i}.body.approach"]=fa?"اسکورت رو به برد شلیک ببر، سپس با فرمان حمله هدف نظامی تأییدشده رو بزن.":"Move the escort into weapon range, then use Attack against the verified military target.";
                foreach(int i in new[]{1,3,5})entries[$"mission.network_collapse.tutorial.{i}.body.recon"]=fa?"نگهبان‌ها رو شکست بده. مهندس اصلی رو به نشان شناسایی بیرونی ببر و شش ثانیه بدون دشمن نگه دار.":"Defeat the guards. Move the original engineer to the external recon marker and hold uncontested for six seconds.";
                entries["mission.network_collapse.tutorial.8.body.board"]=fa?"مهندس اصلی رو انتخاب کن و با فرمان سوار شدن وارد نفربر تدارک‌دیده‌شده کن. سپس نفربر رو انتخاب کن و به نشان خروج ببر.":"Select the original engineer and use Board to enter the supplied APC. Then select the APC and Move to the extraction marker.";
                entries["mission.network_collapse.objective.network"]=entries["mission.network_collapse.objective.nodes"];
                entries["mission.network_collapse.objective.extract"]=entries["mission.network_collapse.objective.extraction"];
                entries["ui.home.archive.network_collapse.audit"]=fa?"حسابرسی کامل تأییدشده":"VERIFIED COMPLETE AUDIT";
                locales.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,entries.OrderBy(e=>e.Key,StringComparer.Ordinal).Select(e=>new GameLocalizedStringRecord(e.Key,e.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,locales);EditorUtility.SetDirty(catalog);
        }
        private static void BuildGuide()
        {
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);
            if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,GuidePath);}
            var basis=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);
            guide.Configure(Enumerable.Range(1,8).Select(i=>new MissionGuideTopic{TitleKey=$"mission.network_collapse.tutorial.{i}.title",BodyKey=$"mission.network_collapse.tutorial.{i}.body",ExampleKey="mission.network_collapse.guide.example",MistakeKey="mission.network_collapse.guide.mistake",DiagramKey="mission.network_collapse.guide.diagram"}).ToArray(),basis.Classes.ToArray());EditorUtility.SetDirty(guide);
            var root=PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try{var data=new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));var property=data.FindProperty("networkCollapseGuide");if(property==null)throw new InvalidOperationException("Network field-guide runtime binding missing");property.objectReferenceValue=guide;data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,M03RadarWarningUiBuilder.GuidePath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
