using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH05M05CommandNodePresentationBuilder
    {
        public const string GuidePath="Assets/Game/Configs/Missions/Chapter05/CH05M05_CommandNode_FieldGuide.asset";
        public static void Build()
        {
            CH05M05CommandNodeNarrativeBuilder.SeedCopyLocalization();
            SeedLocalization(); BuildGuide();
            var markerType=typeof(MissionFieldGuideView).Assembly.GetType("Game.UI.Runtime.CommandNodeMarkersView")
                ?? throw new InvalidOperationException("Command Node marker runtime binding missing");
            var hud=PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
            try {
                var sections=hud.GetComponent<UIShellContentSectionsView>();
                if(sections==null || !sections.TryGetSection(UIShellContentSectionId.Header,out var nativeHeader) || nativeHeader==null)
                    throw new InvalidOperationException("Command Node native HUD header section missing");
                // InstallMatchHud instantiates authored sections individually, not the prefab root.
                var unusedRootOwner=hud.GetComponent(markerType);
                if(unusedRootOwner!=null)UnityEngine.Object.DestroyImmediate(unusedRootOwner);
                if(nativeHeader.GetComponent(markerType)==null)nativeHeader.AddComponent(markerType);
                PrefabUtility.SaveAsPrefabAsset(hud,"Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab"); }
            finally { PrefabUtility.UnloadPrefabContents(hud); }
            AssetDatabase.SaveAssets();
            Debug.Log("[CommandNodePresentation] result=Passed existingHud=1 newCommandButtons=0 locales=2 finale=Captioned voices=NotRequested");
        }
        private static void SeedLocalization()
        {
            var copy=new (string key,string en,string fa)[]{
                ("name","Command Node","مرکز فرمان"),
                ("summary","Isolate the military network, release the audit and preserve civic services.","شبکهٔ نظامی رو جدا کن، گزارش رو منتشر کن و خدمات شهری رو حفظ کن."),
                ("location","Sahrin · Civic Relay complex","سهرین · مجموعهٔ رلهٔ شهری"),
                ("enemy_intel","Ten military threats. Isolate both links before attacking the perimeter node. Preserve civic services and staff.","۱۰ تهدید نظامی؛ پیش از حمله به گره، هر دو اتصال رو جدا کن. خدمات و کارکنان رو حفظ کن."),
                ("objective.network","Isolate the military network","شبکهٔ نظامی رو جدا کن"),
                ("objective.audit","Release the complete audit","گزارش کامل رو منتشر کن"),
                ("objective.safe","Receive both specialists","هر دو متخصص رو امن برسون"),
                ("objective.protection","Protect services and staff","خدمات و کارکنان رو حفظ کن"),
                ("forces","Escort · original engineer · two specialists","اسکورت · مهندس اصلی · دو متخصص"),
                ("resources","Finite Fuel · civic reserve protected","سوخت محدود · سهم خدمات محفوظ"),
                ("label.fuel","FUEL RESERVE","ذخیرهٔ سوخت"),
                ("label.deadline","OPERATION WINDOW","فرصت عملیات"),
                ("deadline","Complete within the fifteen-minute operation window","در فرصت پانزده‌دقیقه‌ای عملیات کامل کن"),
                ("intel.military","MILITARY THREATS","تهدیدهای نظامی"),
                ("intel.confirmed","10","۱۰"),
                ("intel.civilian","CIVIC SERVICES","خدمات شهری"),
                ("intel.protected","SAFE","امن"),
                ("reward.card","Commander XP · Credits · public audit","تجربهٔ فرمانده · اعتبار · گزارش عمومی"),
                ("star.1","Complete the mission","مأموریت رو کامل کن"),
                ("star.2","No escort losses","بدون تلفات اسکورت"),
                ("star.3","No civilian losses","بدون تلفات غیرنظامیان"),
                ("hud.combat","{0}/2 isolated · {1}/10 cleared","{0}/۲ جدا · {1}/۱۰ پاک‌شده"),
                ("hud.status","{0}/2 isolated · {1}/6 s","{0}/۲ جدا · {1}/۶ ثانیه"),
                ("hud.clock","Time left: {0}:{1}","زمان باقی: {0}:{1}"),
                ("hud.isolation","Service links isolated: {0}/2","اتصال جداشده: {0} از ۲"),
                ("hud.hold","Hold position: {0}/6 s","ثابت بمون: {0} از ۶ ثانیه"),
                ("hud.elapsed","Mission time: {0}m {1}s","زمان مأموریت: {0} دقیقه و {1} ثانیه"),
                ("hud.time","Operation window: {0}m {1}s","فرصت عملیات: {0} دقیقه و {1} ثانیه"),
                ("guide.title","COMMAND NODE · FIELD GUIDE","مرکز فرمان · راهنمای میدان"),
                ("guide.example","Clear → isolate → breach → preserve audit → release → safe reception.","پاک‌سازی ← جداسازی ← رخنه ← حفظ گزارش ← انتشار ← دریافت امن"),
                ("guide.mistake","Keep original teams and civic services alive. Show Me moves only the camera. Authority covers this release alone.","تیم‌های اصلی و خدمات رو حفظ کن. «نشان بده» فقط دوربین رو حرکت می‌ده. اجازه فقط برای همین انتشاره."),
                ("guide.diagram","SERVICES | AUDIT | BOUNDED AUTHORITY","خدمات | گزارش | اختیار محدود"),
                ("result.victory","OVERRIDE STOPPED","تصاحب متوقف شد"),
                ("result.defeat","COMMAND NODE OPERATION FAILED","عملیات مرکز فرمان شکست خورد"),
                ("result.subtitle","COMMAND NODE · CIVIC RELAY","مرکز فرمان · رلهٔ شهری"),
                ("result.success","Military controls isolated. Complete audit released. Original specialists and civic services safe.","کنترل‌های نظامی جدا شدن. گزارش کامل منتشر شد. متخصص‌های اصلی و خدمات شهری سالم موندن."),
                ("result.success_short","Audit public. Specialists safe. Shared authority restored.","گزارش عمومیه. متخصص‌ها امن رسیدن. اختیار مشترک برقرار شد."),
                ("result.loss","Preserve original teams and services. Isolate the network, secure the audit and authorize its bounded release.","تیم‌های اصلی و خدمات رو حفظ کن. شبکه رو جدا کن، گزارش رو حفظ کن و اجازهٔ انتشار محدودش رو بده."),
                ("result.objectives","MISSION OBJECTIVES","اهداف مأموریت"),
                ("result.network","Military network isolated","شبکهٔ نظامی جداشده"),
                ("result.audit","Public audit released","گزارش عمومی منتشرشده"),
                ("result.safe","Specialists received","متخصص‌ها دریافت شدن"),
                ("result.escort_losses","Escort losses","تلفات اسکورت"),
                ("result.staff_losses","Staff losses","تلفات کارکنان"),
                ("result.secured","ISOLATED","جداشده"),
                ("result.verified","RELEASED","منتشرشده"),
                ("result.received","RECEIVED","دریافت‌شده"),
                ("result.losses","LOST","ازدست‌رفته"),
                ("result.incomplete","INCOMPLETE","ناقص"),
                ("result.engineer_lost","An original team or protected operation requirement was lost. Preserve the custody chain and civic services.","تیم اصلی یا یک نیاز ضروری عملیات از دست رفت. زنجیرهٔ نگهداری و خدمات شهری رو حفظ کن."),
                ("result.specialist_lost","An original team or protected operation requirement was lost. Preserve the custody chain and civic services.","تیم اصلی یا یک نیاز ضروری عملیات از دست رفت. زنجیرهٔ نگهداری و خدمات شهری رو حفظ کن."),
                ("result.civic_lost","An original team or protected operation requirement was lost. Preserve the custody chain and civic services.","تیم اصلی یا یک نیاز ضروری عملیات از دست رفت. زنجیرهٔ نگهداری و خدمات شهری رو حفظ کن."),
                ("result.staff_lost","An original team or protected operation requirement was lost. Preserve the custody chain and civic services.","تیم اصلی یا یک نیاز ضروری عملیات از دست رفت. زنجیرهٔ نگهداری و خدمات شهری رو حفظ کن."),
                ("result.escort_lost","An original team or protected operation requirement was lost. Preserve the custody chain and civic services.","تیم اصلی یا یک نیاز ضروری عملیات از دست رفت. زنجیرهٔ نگهداری و خدمات شهری رو حفظ کن."),
                ("result.fuel_lost","An original team or protected operation requirement was lost. Preserve the custody chain and civic services.","تیم اصلی یا یک نیاز ضروری عملیات از دست رفت. زنجیرهٔ نگهداری و خدمات شهری رو حفظ کن."),
                ("result.audit_lost","An original team or protected operation requirement was lost. Preserve the custody chain and civic services.","تیم اصلی یا یک نیاز ضروری عملیات از دست رفت. زنجیرهٔ نگهداری و خدمات شهری رو حفظ کن."),
                ("result.timeout","An original team or protected operation requirement was lost. Preserve the custody chain and civic services.","تیم اصلی یا یک نیاز ضروری عملیات از دست رفت. زنجیرهٔ نگهداری و خدمات شهری رو حفظ کن."),
                ("result.integrity","An original team or protected operation requirement was lost. Preserve the custody chain and civic services.","تیم اصلی یا یک نیاز ضروری عملیات از دست رفت. زنجیرهٔ نگهداری و خدمات شهری رو حفظ کن.")};
            var stages=new (string en,string fa,string body,string faBody)[]{
                ("Clear outer guards","نگهبان‌های بیرونی رو شکست بده","Move the original escort into range and Attack all four exterior military threats.","اسکورت اصلی رو به برد شلیک ببر و به هر چهار تهدید بیرونی حمله کن."),
                ("Isolate both service links","هر دو اتصال رو جدا کن","Move the original engineer to clinic isolation and hold six seconds, then repeat at utility isolation.","مهندس اصلی رو به نشان جداسازی درمانگاه ببر و شش ثانیه بمون؛ بعد در نشان خدمات تکرار کن."),
                ("Disable the perimeter node","گره بیرونی رو از کار بنداز","After both links are isolated, Attack the military perimeter node with the original escort.","پس از جداسازی هر دو اتصال، با اسکورت اصلی به گره نظامی بیرونی حمله کن."),
                ("Open the protected breach","رخنهٔ امن رو باز کن","Move the original engineer to the breach marker and hold six seconds.","مهندس اصلی رو به نشان رخنه ببر و شش ثانیه ثابت بمون."),
                ("Defeat the core force","نیروهای مرکز رو شکست بده","Attack the four core guards and Qassem’s tank. Preserve civic services and the original teams.","به چهار نگهبان مرکز و تانک قاسم حمله کن. خدمات و تیم‌های اصلی رو حفظ کن."),
                ("Preserve the complete audit","گزارش کامل رو حفظ کن","Move both original specialists to the audit marker. Both must hold there for six seconds.","هر دو متخصص اصلی رو به نشان گزارش ببر. هر دو شش ثانیه اونجا ثابت بمونن."),
                ("Authorize bounded release","اجازهٔ انتشار محدود بده","Move the original engineer to the release marker and hold six seconds. This authorizes only the public audit release.","مهندس اصلی رو به نشان انتشار ببر و شش ثانیه بمون. اجازه فقط برای انتشار عمومی گزارشه."),
                ("Receive both specialists","هر دو متخصص رو امن برسون","Move both original specialists to safe receiving and hold six seconds.","هر دو متخصص اصلی رو به دریافت امن ببر و شش ثانیه بمون.")};
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath)
                ?? throw new InvalidOperationException("Localization missing");
            var locales=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en"&&locale.LocaleCode!="fa-IR"){locales.Add(locale);continue;}
                bool fa=locale.LocaleCode=="fa-IR";
                var entries=locale.Entries.ToDictionary(e=>e.Key,e=>e.Value,StringComparer.Ordinal);
                foreach(var item in copy)entries["mission.command_node."+item.key]=fa?item.fa:item.en;
                for(int i=0;i<stages.Length;i++)
                {var stage=stages[i];entries[$"mission.command_node.tutorial.{i+1}.title"]=fa?stage.fa:stage.en;
                    entries[$"mission.command_node.tutorial.{i+1}.body"]=fa?stage.faBody:stage.body;
                    entries[$"mission.command_node.hud.stage.{i+1}"]=fa?stage.fa:stage.en;}
                foreach(int stage in new[]{1,3,5})entries[$"mission.command_node.tutorial.{stage}.body.approach"]=fa?"اسکورت اصلی رو به نشان نزدیک شدن در برد شلیک ببر؛ بعد به تهدید تأییدشده حمله کن.":"Move the original escort to the approach marker within range, then Attack the confirmed threat.";
                foreach(int stage in new[]{2,4,6,7,8})entries[$"mission.command_node.marker.stage.{stage}"]=fa?stages[stage-1].fa:stages[stage-1].en;
                entries["mission.command_node.tutorial.4.body.cover"]=fa?"اسکورت اصلی رو از دهانهٔ رخنه به نشان پوشش ببر. پیش از اینکه مهندس رخنه رو باز کنه، یک تانک اصلی باید اونجا متوقف بشه.":"Move the original escort through the breach opening to the cover marker. An original tank must stop there before the engineer opens the breach.";
                entries["mission.command_node.hud.stage.4"]=fa?"رخنه رو پوشش بده، بعد بازش کن":"Cover the breach, then open";
                entries["mission.command_node.tutorial.2.body.clinic"]=fa?"مهندس اصلی رو به نشان درمانگاه ببر و شش ثانیه ثابت بمون.":"Move the original engineer to clinic isolation and hold six seconds.";
                entries["mission.command_node.tutorial.2.body.utility"]=fa?"با همون مهندس به نشان خدمات برو و شش ثانیه بمون.":"Move the same engineer to utility isolation and hold six seconds.";
                entries["mission.command_node.objective.specialists"]=entries["mission.command_node.objective.safe"];
                entries["mission.command_node.result.safe_status"]=fa?"امن":"SAFE";
                foreach(string key in new[]{"specialists_lost","services_lost","unsafe_node"})entries["mission.command_node.result."+key]=entries["mission.command_node.result.loss"];
                string[] archive={"fragment5","audit","governance","emphasis","postscript"};
                string[] archiveEn={"PROTOCOL FRAGMENT V","COMPLETE QASSEM AUDIT","SHARED BOUNDED GOVERNANCE","RECORDED RECOVERY EMPHASIS","RECOVERY WATCH"};
                string[] archiveFa={"قطعهٔ پنجم پروتکل","گزارش کامل قاسم","اختیار مشترک و محدود","وضعیت ثبت‌شدهٔ بازسازی","نگهبانی بازسازی"};
                for(int i=0;i<archive.Length;i++)entries["ui.home.archive.command_node."+archive[i]]=fa?archiveFa[i]:archiveEn[i];
                locales.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,
                    entries.OrderBy(e=>e.Key,StringComparer.Ordinal).Select(e=>new GameLocalizedStringRecord(e.Key,e.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,locales);EditorUtility.SetDirty(catalog);
        }
        private static void BuildGuide()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(GuidePath));
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);
            if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,GuidePath);}
            var basis=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path)
                ?? throw new InvalidOperationException("Field guide class catalog missing");
            guide.Configure(Enumerable.Range(1,8).Select(i=>new MissionGuideTopic{
                TitleKey=$"mission.command_node.tutorial.{i}.title",BodyKey=$"mission.command_node.tutorial.{i}.body",
                ExampleKey="mission.command_node.guide.example",MistakeKey="mission.command_node.guide.mistake",
                DiagramKey="mission.command_node.guide.diagram"}).ToArray(),basis.Classes.ToArray());
            EditorUtility.SetDirty(guide);
            var root=PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try {var data=new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));
                var property=data.FindProperty("commandNodeGuide")
                    ?? throw new InvalidOperationException("Command Node field-guide runtime binding missing");
                property.objectReferenceValue=guide;data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,M03RadarWarningUiBuilder.GuidePath);}
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
