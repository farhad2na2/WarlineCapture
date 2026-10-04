using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    // Applied by the native builders after bindings are installed. No save or gameplay ownership.
    public static class MenuUiApprovedAuthoring
    {
        private static readonly Color Top = new Color32(20,31,35,252), Bottom = new Color32(6,13,16,252), Border = new Color32(70,82,86,255);
        internal static RectTransform Find(GameObject root, string name) => root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == name);
        internal static TMP_Text Text(GameObject root, string name) => Find(root,name)?.GetComponent<TMP_Text>();
        internal static void Place(RectTransform r, float x, float y, float w, float h)
        {
            if (r == null) return;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0,1);
            r.anchoredPosition = new Vector2(x,-y); r.sizeDelta = new Vector2(w,h);
        }
        internal static void Hide(GameObject root, params string[] names)
        { foreach (string n in names) { var r=Find(root,n); if(r != null) r.gameObject.SetActive(false); } }
        private static void Copy(RectTransform root, string child, string value)
        { var text=root?.Find(child)?.GetComponent<TMP_Text>(); if(text!=null) Bind(text,value); }
        internal static RectTransform Panel(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);Place(r,x,y,w,h);
            r.gameObject.AddComponent<V3GradientGraphic>().Configure(Top,Bottom,Border,2);return r;
        }
        internal static TMP_Text NewText(Transform parent,string name,string copy,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<RectTransform>();r.SetParent(parent,false);Place(r,x,y,w,h);
            var t=r.GetComponent<TMP_Text>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset");
            t.fontSize=30;t.color=Color.white;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;
            Bind(t,copy);return t;
        }
        internal static void Bind(TMP_Text text,string copy)
        {
            if(text==null)return;
            text.text=copy;
            string key="menu.approved."+new string(copy.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
            if(UiShellRuntimeGateway.Localization.TryGetBySource(copy,out string existing,out _))key=existing;
            (text.GetComponent<V3LocalizedTextBindingView>()??text.gameObject.AddComponent<V3LocalizedTextBindingView>()).Configure(key,copy);
        }
        public static void Localize()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>("Assets/Game/Resources/Localization/V3UiLocalizationCatalog.asset");
            var copy=new Dictionary<string,string> {
                {"YOUR COMMANDER","فرمانده شما"},{"VIEW LOG","مشاهده گزارش"},{"Search battles","جستجوی نبردها"},
                {"DEFAULTS","پیش‌فرض"},{"SHUFFLE TACTICS","تاکتیک تصادفی"},
                {"MISSION PROGRESS","پیشرفت ماموریت"},{"CAMPAIGN RECORD","کارنامه عملیات"},
                {"ENEMIES","دشمنان"},{"COMMAND LOG","گزارش فرماندهی"},
                {"Your progress is saved after each mission.","پیشرفت شما پس از هر ماموریت ذخیره می‌شود."},
                {"CHARACTER","شخصیت"},{"COMMAND RECORD","سوابق فرماندهی"},{"DISTRICT OVERVIEW","نمای منطقه"},{"Starting","در حال آغاز"},{"Purchases are currently unavailable.","خرید در حال حاضر در دسترس نیست."},
                {"NO DISTRICT REPORT","گزارش منطقه در دسترس نیست"},{"DISTRICT REPORT","گزارش منطقه"},{"VIEW OPERATIONS","مشاهده عملیات"},{"No district report is available. Open Operations to review current objectives and available missions.","گزارش منطقه در دسترس نیست. برای مرور اهداف جاری و ماموریت‌های موجود، عملیات را باز کنید."},
                {"SQUAD PREPARATION","آماده‌سازی نیروها"},{"MISSION BRIEFING","شرح ماموریت"},{"MISSION FORCE","نیروهای ماموریت"},{"MISSION PLAN","طرح ماموریت"},{"MISSION CONDITIONS","شرایط ماموریت"},{"PRIMARY OBJECTIVES","اهداف اصلی"},{"ENEMY INTEL","اطلاعات دشمن"},{"STAR GOALS","اهداف ستاره‌ها"},{"VIEW ARMORY","مشاهده زرادخانه"},{"Review the mission objectives before deployment.","پیش از اعزام، اهداف ماموریت را مرور کنید."},{"DISTRICT DETAIL","جزئیات منطقه"},{"COMMAND FEED","گزارش فرماندهی"},
                {"PREPARING","آماده‌سازی"},{"LOADING","در حال بارگذاری"},{"Ready","آماده"},{"Loading","در حال بارگذاری"}
            };
            var tables=new List<GameLocaleTable>();
            foreach(var table in catalog.Locales)
            {
                var entries=table.Entries.ToList();
                foreach(var item in copy)
                {
                    string key="menu.approved."+new string(item.Key.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
                    if(UiShellRuntimeGateway.Localization.TryGetBySource(item.Key,out string existing,out _))key=existing;
                    entries.RemoveAll(e=>e.Key==key);
                    entries.Add(new GameLocalizedStringRecord(key,table.LocaleCode=="fa-IR"?item.Value:item.Key));
                }
                tables.Add(new GameLocaleTable(table.LocaleCode,table.DisplayName,table.ShortLabel,table.RightToLeft,table.FontAsset,entries));
            }
            catalog.Configure(catalog.SourceLocaleCode,tables);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        public static void Apply(GameObject root, string screen)
        {
            if(screen=="main-menu") Home(root);
            if(screen=="campaign") { MenuUiCampaignMediaPreservation.Apply(root); Campaign(root); }
            if(screen=="skirmish") Skirmish(root);
            if(screen=="operations") Operations(root);
            if(screen=="commander") Commander(root);
            Header(root,screen);
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.enableAutoSizing=false;
                if(text.transform.parent?.name=="CommanderStatsPanel")text.textWrappingMode=TextWrappingModes.Normal;
                if(text.name=="MenuTitle"||text.name=="ScreenTitle")text.alignment=TextAlignmentOptions.MidlineLeft;
                text.fontSize=text.text=="YOUR COMMANDER"||text.name=="MenuTitle" ? 30 : text.fontSize>=44 ? 52 : text.fontSize>=32 ? 38 : 30;
                text.fontSizeMin=text.fontSize; text.fontSizeMax=text.fontSize;
                text.GetComponent<V3LocalizedTextBindingView>()?.ConfigureFontBounds(text.fontSize,text.fontSize);
            }
            foreach(var frame in root.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true)) if(frame.name!="MenuBackgroundContent")ConfigureFrame(frame,screen);
        }
        internal static void Header(GameObject root,string screen)
        {
            var frame=root.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true).FirstOrDefault(v=>v.name=="HeaderContent") ?? root.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            var parent=frame.transform;
            var credits=Find(root,"Credits")??Find(root,"CreditsChip")??Find(root,"CreditsVisualPanel")??Find(root,"CreditsPanel");
            if(credits==null)
            {
                credits=Panel(parent,"Credits",1296,10,360,86);
                var icon=new GameObject("Icon",typeof(RectTransform),typeof(Image)).GetComponent<Image>();icon.transform.SetParent(credits,false);
                icon.sprite=V3UiFoundationBuilder.RequireCatalog().CreditsIcon;icon.raycastTarget=false;Place(icon.rectTransform,16,16,54,54);
                NewText(credits,"Label","CREDITS",82,4,264,34);
                var value=NewText(credits,"Value","—",82,38,264,44);
                credits.gameObject.AddComponent<MainMenuAccountHeaderView>().Configure(value);
            }
            Place(credits,1296,10,360,86);
            Place(credits.Find("Icon") as RectTransform,16,16,54,54);
            Place(credits.Find("Label") as RectTransform,82,4,264,34);
            credits.Find("Label")?.gameObject.SetActive(false);
            Place(credits.Find("Value") as RectTransform,82,8,264,70);
            Place(Find(root,"SettingsButton"),1166,10,118,86);
            if(screen=="main-menu") { Place(Find(root,"HeaderLogoPanel"),14,10,800,86); return; }
            Hide(root,"WarlineLogo","TitleDivider","TitleSection");
            var back=Find(root,screen=="campaign"?"MissionBackButton":"BackButton")??Find(root,"HeaderBackButton");
            if(back!=null)
            {
                back.SetParent(parent,false);Place(back,14,10,180,86);Copy(back,"Label","BACK");
                Place(back.Find("Icon") as RectTransform,12,22,42,42);
                Place(back.Find("Label") as RectTransform,58,8,110,70);
            }
            var title=Find(root,"ScreenTitlePanel");
            if(title!=null) { Place(title,206,10,948,86); Place(title.Find("ScreenTitle") as RectTransform,24,8,900,70); }
            else
            {
                title=Panel(parent,"MenuTitlePanel",206,10,948,86);
                NewText(title,"MenuTitle",screen=="campaign"?"CAMPAIGN":screen=="mission-briefing"?"MISSION BRIEFING":screen=="squad-preparation"?"SQUAD PREPARATION":screen=="store"?"STORE":screen=="armory"?"ARMORY":screen=="district-detail"?"DISTRICT DETAIL":screen=="command-feed"?"COMMAND FEED":"COMMANDER",24,8,900,70).fontSize=44;
            }
            if(screen=="campaign") Hide(root,"ScreenTitle");
        }
        private static void Home(GameObject root)
        {
            var hero=Find(root,"Card_Campaign");
            if(hero!=null)
            {
                var border=hero.GetComponent<V3GradientGraphic>();
                if(border!=null)border.Configure(Color.clear,Color.clear,Border,2);
                var outline=Panel(hero,"CampaignOutline",0,0,hero.rect.width,hero.rect.height);Stretch(outline,0);
                outline.GetComponent<V3GradientGraphic>().Configure(Color.clear,Color.clear,Border,2);
                outline.GetComponent<V3GradientGraphic>().raycastTarget=false;
            }
            var shade=Find(root,"CampaignReadability");
            if(shade!=null)shade.GetComponent<V3GradientGraphic>()?.Configure(new Color(0,0,0,.25f),new Color(0,0,0,.96f),Color.clear,0);
            var campaign=Find(root,"Card_Campaign");
            Place(campaign?.Find("CampaignLabel") as RectTransform,32,264,1170,42);
            Place(campaign?.Find("Chapter") as RectTransform,32,308,1170,40);
            Place(campaign?.Find("Title") as RectTransform,32,348,1170,62);
            Place(campaign?.Find("Purpose") as RectTransform,32,412,1170,80);
            Place(campaign?.Find("ContinueButton") as RectTransform,32,501,650,72);
            Place(campaign?.Find("StoryArchiveButton") as RectTransform,710,501,360,72);
            if(campaign?.Find("Purpose")?.GetComponent<TMP_Text>() is TMP_Text purpose)purpose.overflowMode=TextOverflowModes.Overflow;
            var aria=Find(root,"AriaPanel");
            Place(aria?.Find("Title") as RectTransform,20,3,190,62);
            Place(aria?.Find("Description") as RectTransform,20,68,182,148);
            // Status copy and unaccented border distinguish the informational ARIA tile.
            aria?.GetComponent<V3GradientGraphic>()?.Configure(Top,Bottom,Border,2);
            var commander=Find(root,"CommanderPanel");
            Copy(commander,"Title","YOUR COMMANDER");
            Place(commander?.Find("IdentityName") as RectTransform,18,62,200,130);
        }
        private static void Campaign(GameObject root)
        {
            var campaignView=root.GetComponentInChildren<CampaignOperationsScreenView>(true);
            var chapterBindings=new SerializedObject(campaignView);
            chapterBindings.FindProperty("chapterOneButton").objectReferenceValue=campaignView.ChapterOneButton;
            chapterBindings.FindProperty("chapterTwoButton").objectReferenceValue=campaignView.ChapterTwoButton;
            chapterBindings.ApplyModifiedPropertiesWithoutUndo();
            var map=Find(root,"StrategicMap");Place(map,414,108,748,680);
            var clip=Find(root,"MapClip");if(clip!=null) Stretch(clip,4);
            var rail=Find(root,"ChapterRail");Place(rail,14,108,388,680);
            if(rail!=null) foreach(RectTransform child in rail)
            {
                child.sizeDelta=new Vector2(388,126);
                var title=child.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name=="Title");
                var subtitle=child.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name=="Subtitle");
                if(title!=null) { Place(title.rectTransform,90,8,274,72);title.fontSize=30; }
                if(subtitle!=null)Place(subtitle.rectTransform,90,82,274,36);
                if(title!=null)title.overflowMode=TextOverflowModes.Overflow;
            }
            // Numbered pins + route are the sole map selector. Drop the competing thumbnail labels.
            if(map!=null) foreach(var r in map.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="MissionLabel"))r.gameObject.SetActive(false);
            var briefing=Find(root,"MissionBriefing");Place(briefing,1174,108,482,680);
            if(briefing!=null)
            {
                var content=ScrollContent(briefing,"BriefingScroll",1340);
                Place(content.Find("MissionNumber") as RectTransform,20,8,442,40);
                Place(content.Find("MissionName") as RectTransform,20,50,442,90);
                Place(content.Find("MissionPreviewClip") as RectTransform,20,148,442,164);
                Place(content.Find("MissionBriefingText") as RectTransform,20,324,442,180);
                if(content.Find("MissionBriefingText")?.GetComponent<TMP_Text>() is TMP_Text purpose)purpose.overflowMode=TextOverflowModes.Overflow;
                Place(content.Find("ObjectivesTitle") as RectTransform,20,518,442,44);
                var objectives=content.Cast<Transform>().Where(t=>t.name.StartsWith("Objective")&&t.GetComponent<V3GradientGraphic>()!=null).ToArray();
                for(int i=0;i<objectives.Length;i++)
                {
                    Place((RectTransform)objectives[i],20,570+i*102,442,94);
                    Place(objectives[i].Find("Icon") as RectTransform,14,24,46,46);
                    Place(objectives[i].Find("Label") as RectTransform,78,10,348,74);
                }
                Place(content.Find("RewardsTitle") as RectTransform,20,880,442,44);
                Place(content.Find("RewardSummaryText") as RectTransform,20,930,442,100);
                Place(content.Find("GoalsTitle") as RectTransform,20,1042,442,44);
                for(int i=0;i<3;i++)
                {
                    var goal=content.Find("Goal"+i) as RectTransform;Place(goal,20,1094+i*66,442,60);
                    Place(goal?.Find("Star") as RectTransform,14,10,40,40);
                    Place(goal?.Find("Copy") as RectTransform,70,4,352,52);
                }
            }
            var flow=briefing.Find("BriefingScroll/Content") as RectTransform;
            string[] sequence={"MissionNumber","MissionName","MissionPreviewClip","MissionBriefingText","ObjectivesTitle","RewardsTitle","RewardSummaryText","GoalsTitle","Goal0","Goal1","Goal2"};
            var ordered=sequence.Select(n=>flow.Find(n) as RectTransform).Where(r=>r!=null).ToList();
            ordered.InsertRange(ordered.FindIndex(r=>r.name=="RewardsTitle"),flow.Cast<Transform>().Where(t=>t.name=="Objective").Cast<RectTransform>());
            flow.gameObject.AddComponent<MenuBriefingFlowView>().Configure(ordered.ToArray());
            var chapters=Find(root,"ShowChapterSelectButton");
            // The chapter rail already exposes every chapter. Keep this binding dormant.
            if(chapters!=null)chapters.gameObject.SetActive(false);
            var launch=Find(root,"LaunchMissionButton");Place(launch,688,804,968,123);
            var archive=Find(root,"FooterStoryArchiveButton");
            if(archive!=null)
            {archive.SetParent(launch.parent,false);archive.gameObject.SetActive(true);Place(archive,14,804,662,123);Copy(archive,"Label","STORY ARCHIVE");}
        }
        private static void Skirmish(GameObject root)
        {
            var preview=Find(root,"OperationPreview");Place(preview,14,108,800,680);
            var name=Text(root,"OperationName");if(name!=null){Place(name.rectTransform,8,0,784,54);name.overflowMode=TextOverflowModes.Overflow;}
            Place(Find(root,"BattleLibraryMapTabs"),8,54,784,102);
            Place(Find(root,"BattleLibrarySearch"),8,168,784,96);
            var placeholder=Find(root,"BattleLibrarySearch")?.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name=="Placeholder");
            if(placeholder!=null)Bind(placeholder,"Search battles");
            Hide(root,"BattleLibraryCount");
            Place(Find(root,"BattleLibrary"),8,276,784,396);
            var map=Find(root,"MapPreviewClip");Place(map,812,0,830,250);
            Place(Find(root,"SeedHelp"),812,262,830,106);
            var rules=Find(root,"BaseAssaultRules");Place(rules,826,488,830,300);
            if(rules!=null)
            {
                var content=ScrollContent(rules,"RulesScroll",860);
                string[] names={"Title","Opponent","Objective","Roster","Economy","Intel"};
                float[] y={12,72,182,346,518,686};float[] h={50,100,154,162,158,154};
                for(int i=0;i<names.Length;i++)Place(content.Find(names[i]) as RectTransform,24,y[i],782,h[i]);
            }
            Copy(Find(root,"ResetButton"),"Label","DEFAULTS");
            Copy(Find(root,"RandomizeSeedButton"),"Label","SHUFFLE TACTICS");
        }
        private static void Operations(GameObject root)
        {
            Stretch(Find(root,"DistrictMap")?.Find("MapClip") as RectTransform,3);
            var rail=Find(root,"ReadinessRail");
            if(rail!=null)foreach(RectTransform card in rail)
            {
                Place(card.Find("Label") as RectTransform,114,4,220,74);
                Place(card.Find("Value") as RectTransform,114,78,220,44);
                var label=card.Find("Label")?.GetComponent<TMP_Text>();
                if(label!=null){label.textWrappingMode=TextWrappingModes.Normal;label.overflowMode=TextOverflowModes.Overflow;}
            }
            Place(Find(root,"DailyBriefing"),1250,115,415,556);
            Place(Find(root,"ActiveWarnings"),1250,683,415,112);
            var warning=Find(root,"WarningRow0");
            Hide(root,"WarningRow0Icon");
            if(warning!=null)
            {
                warning.Find("Icon")?.gameObject.SetActive(false);warning.Find("Divider")?.gameObject.SetActive(false);
                Place(warning,12,50,391,54);
                Place(warning.Find("Label") as RectTransform,12,4,367,46);
            }
            foreach(var marker in root.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name.EndsWith("Marker")))
            {
                if(marker.Find("Label")==null)continue;
                var backdrop=Panel(marker,"LabelBackdrop",0,0,280,78);backdrop.SetAsFirstSibling();
                Place(marker.Find("Icon") as RectTransform,10,17,44,44);
                Place(marker.Find("Label") as RectTransform,62,4,208,70);
                var label=marker.Find("Label").GetComponent<TMP_Text>();label.textWrappingMode=TextWrappingModes.Normal;label.overflowMode=TextOverflowModes.Overflow;
            }
            var bar=Find(root,"CommandBar");
            if(bar!=null)foreach(Transform child in bar)
            {
                child.GetComponent<V3GradientGraphic>()?.Configure(Top,Bottom,Border,2);
                var label=child.Find("Label") as RectTransform;
                if(label!=null) { Stretch(label,12);label.offsetMin=new Vector2(12,8);label.offsetMax=new Vector2(-12,-54); }
                Place(child.Find("Icon") as RectTransform,16,12,36,36);
                var le=child.GetComponent<LayoutElement>();if(le!=null) { le.minWidth=0;le.flexibleWidth=1; }
            }
            Find(root,"EndDayButton")?.GetComponent<V3GradientGraphic>()?.Configure(new Color32(76,154,24,255),new Color32(22,82,22,255),new Color32(98,184,33,255),2);
        }
        private static void Commander(GameObject root)
        {
            Hide(root,"Rank","RankBadge","EditCommanderButton","XpTrack","Milestone");
            Stretch(Find(root,"CommanderArtPanel")?.Find("ArtClip") as RectTransform,5);
            var scene=Find(root,"CommanderScene")?.GetComponent<Image>();
            var variants=Enumerable.Range(0,6).Select(i=>new MainMenuCommanderVariantView.CommanderVariant(i.ToString(),AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuV3PrefabBuilder.CommanderPanelPath(i)))).ToArray();
            if(scene!=null)
            {
                scene.sprite=variants[0].Sprite;scene.rectTransform.localScale=Vector3.one;scene.rectTransform.anchoredPosition=Vector2.zero;
                var fitter=scene.GetComponent<AspectRatioFitter>();if(fitter!=null)fitter.aspectRatio=scene.sprite.rect.width/scene.sprite.rect.height;
                var view=scene.gameObject.AddComponent<MainMenuCommanderVariantView>();view.Configure(scene,variants,"0");view.ConfigureIdentity(Text(root,"CommanderName"));
            }
            var rewards=Find(root,"CommanderRewardTrack");
            var history=Find(root,"RecentHistory");
            if(rewards!=null)foreach(Transform child in rewards)child.gameObject.SetActive(child.name=="Title");
            if(history!=null)
            {
                var heading=history.Find("Title");
                foreach(Transform child in history)child.gameObject.SetActive(child==heading||child.name=="ViewAll");
            }
            Copy(rewards,"Title","MISSION PROGRESS");Copy(history,"Title","CAMPAIGN RECORD");
            Place(rewards?.Find("Title") as RectTransform,24,4,483,80);
            var progress=NewText(rewards,"SavedProgress","",24,88,483,170);
            var record=NewText(history,"SavedRecord","",24,88,483,220);
            Place(history?.Find("Title") as RectTransform,24,4,483,80);
            history?.Find("ViewAll")?.gameObject.SetActive(false);
            Place(Find(root,"CommanderName"),28,110,374,104);
            Place(Find(root,"CommanderSubtitle"),28,212,374,50);
            Place(Find(root,"LevelLabel"),28,278,374,40);
            Place(Find(root,"Level"),28,322,374,68);
            Place(Find(root,"XpLabel"),28,376,374,48);
            Place(Find(root,"Xp"),28,424,374,52);
            Place(Find(root,"CommanderArtPanel"),262,108,420,480);
            Place(Find(root,"CommanderIdentityPanel"),694,108,428,480);
            var stats=Find(root,"CommanderStatsPanel");Place(stats,262,600,860,190);
            if(stats!=null)
            {
                foreach(var icon in stats.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="Icon"))icon.gameObject.SetActive(false);
                var labels=stats.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="Label").ToArray();
                var values=stats.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="Value").ToArray();
                for(int i=0;i<labels.Length;i++)
                {Place(labels[i].rectTransform,i*172+8,4,156,108);labels[i].overflowMode=TextOverflowModes.Overflow;labels[i].alignment=TextAlignmentOptions.Center;
                Place(values[i].rectTransform,i*172+8,110,156,68);values[i].alignment=TextAlignmentOptions.Center;values[i].overflowMode=TextOverflowModes.Overflow;}
                if(labels.Length>2)Bind(labels[2],"ENEMIES");
                root.GetComponentInChildren<CommanderProfileContentView>(true).ConfigureFacts(Text(root,"Level"),Text(root,"Xp"),values,null,null);
                history.parent.gameObject.AddComponent<CommanderProfileContentView>().ConfigureFacts(null,null,null,record,progress);
            }
            foreach(var temp in root.GetComponentsInChildren<ButtonTemporaryFeedbackView>(true))UnityEngine.Object.DestroyImmediate(temp);
            Hide(root,"OVERVIEWTab","STATSTab","BADGESTab","UPGRADESTab");
            var historyTab=Find(root,"HISTORYTab");Place(historyTab,14,121,236,118);Copy(historyTab,"Label","COMMANDER");
            if(historyTab!=null)historyTab.GetComponent<Button>().interactable=false;
            Place(historyTab?.Find("Icon") as RectTransform,18,14,42,42);
            Place(historyTab?.Find("Label") as RectTransform,12,42,212,76);
            var left=historyTab?.parent;
            if(left!=null)NewText(left,"ProfileHelp","Your progress is saved after each mission.",30,275,208,210);
            var change=Find(root,"ChangeCommanderButton");
            if(change!=null)change.gameObject.AddComponent<CommanderPortraitPickerView>().Configure(change.GetComponent<Button>(),variants.Select(v=>v.Sprite).ToArray());
            var armory=Find(root,"OpenArmoryButton");Place(armory,14,804,951,123);
            armory?.GetComponent<V3GradientGraphic>()?.Configure(Top,Bottom,Border,2);
        }
        private static void Stretch(RectTransform r,float padding)
        {if(r==null)return;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=Vector2.one*padding;r.offsetMax=Vector2.one*-padding;}
        internal static RectTransform ScrollContent(RectTransform panel,string name,float height)
        {
            var children=panel.Cast<Transform>().ToArray();
            var viewport=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(RectMask2D)).GetComponent<RectTransform>();viewport.SetParent(panel,false);Stretch(viewport,4);
            viewport.GetComponent<Image>().color=new Color(0,0,0,.01f);
            var content=new GameObject("Content",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport,false);
            content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(0,1);content.sizeDelta=new Vector2(0,height);content.anchoredPosition=Vector2.zero;
            foreach(var child in children)child.SetParent(content,false);
            var scroll=panel.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            return content;
        }
        private static void ConfigureFrame(MainMenuV3SectionLayoutView frame,string screen)
        {
            var rules=new List<MenuFrameTarget>();
            var right=new HashSet<RectTransform>(frame.RightAnchoredTargets.Where(t=>t!=null));
            foreach(var r in frame.GetComponentsInChildren<RectTransform>(true))
            {
                if(r==frame.transform||r.parent.GetComponent<LayoutGroup>()!=null)continue;
                var old=frame.HorizontalResponsiveTargets.FirstOrDefault(t=>t.Target==r);
                float x=old.PositionFactor,w=old.WidthFactor;
                if(right.Contains(r))x=1;
                var so=new SerializedObject(frame);
                var expanded=so.FindProperty("widthExpandedTargets");
                for(int i=0;i<expanded.arraySize;i++)if(expanded.GetArrayElementAtIndex(i).objectReferenceValue==r)w=1;
                bool top=r.parent==frame.transform;
                float y=0,h=0;
                if(top && r.anchoredPosition.y < -790)y=1;
                if(screen=="main-menu")
                {
                    if(r.name=="Card_Campaign") {w=1;h=1;}
                    if(r.parent?.name=="Card_Campaign" && (r.GetComponent<TMP_Text>()!=null||r.name.EndsWith("Button")))y=1;
                    if(r.name=="AriaPanel"){x=1;y=.5f;h=.5f;}
                    if(r.name=="CommanderPanel"){x=1;h=.5f;}
                    if(r.parent?.name=="CommanderPanel"&&r.name=="ViewCommanderButton")y=.5f;
                    if(r.name=="Card_Operations"||r.name=="Card_Skirmish")y=1;
                    if(r.name=="StoreButton"||r.name=="OpenArmoryButton"){x=1;y=1;}
                }
                if(screen=="campaign")
                {
                    if(r.name=="MissionSelectState"||r.name=="ChapterSelectState"){w=1;h=1;}
                    if(r.name=="StrategicMap"){w=1;h=1;}
                    if(r.name=="MissionBriefing"){x=1;h=1;}
                    if(r.name=="ChapterRail")h=1;
                    if(r.name=="LaunchMissionButton"){x=0;w=1;y=1;}
                    if(r.name=="FooterStoryArchiveButton"){x=0;y=1;}
                }
                if(screen=="skirmish")
                {
                    if(r.name=="OperationPreview") {w=0;h=1;}
                    if(r.name=="BattleLibrary")h=1;
                    if(r.name=="MapPreviewClip"||r.name=="SeedHelp"){w=1;x=0;}
                    if(r.name=="BaseAssaultRules"){x=0;w=1;h=1;}
                }
                if(screen=="operations")
                {
                    if(r.name=="DistrictMap")h=1;
                    if(r.name=="ReadinessRail")h=1;
                    if(r.parent?.name=="ReadinessRail") { y=r.GetSiblingIndex()*.2f;h=.2f; }
                    if(r.name=="DailyBriefing")h=1;
                    if(r.name=="ActiveWarnings")y=1;
                    if(r.name=="CommandBar"){w=1;y=1;}
                }
                if(screen=="commander")
                {
                    if(r.name=="CommanderArtPanel"){w=.4f;h=1;}
                    if(r.name=="CommanderIdentityPanel"){x=.4f;w=.6f;h=1;}
                    if(r.name=="CommanderStatsPanel"){w=1;y=1;}
                    if(r.parent?.name=="CommanderStatsPanel" && r.GetComponent<TMP_Text>()!=null)
                    { x=Mathf.Floor(r.anchoredPosition.x/172f)*.2f; w=.2f; }
                    if(r.name=="CommanderRewardTrack"||r.name=="RecentHistory")x=1;
                    if(r.name=="RecentHistory")h=1;
                    if(r.name=="ChangeCommanderButton")x=1;
                    if(r.name=="OpenArmoryButton")w=1;
                }
                if(screen=="store") { if(r.name=="OffersPanel"){w=1;h=1;} if(r.name=="CategoryRail")h=1; if(r.name=="DetailPanel"){x=1;h=1;} }
                if(screen=="armory") { if(r.name=="CatalogPanel"||r.name=="CatalogViewport"){w=1;h=1;} if(r.name=="InspectionPanel"){x=1;h=1;} if(r.name=="CategoryRail")h=1; }
                if(screen=="district-detail") { if(r.name=="DistrictVisual"){w=1;h=1;} if(r.name=="DistrictReport"){x=1;h=1;} if(r.name=="DistrictOperationsButton"){w=1;y=1;} }
                if(screen=="command-feed") { if(r.name=="FeedViewport"){w=1;h=1;} if(r.name=="RightRail"){x=1;h=1;} if(r.name=="FilterRail")h=1; }
                if(r.name=="MenuTitlePanel"||r.name=="ScreenTitlePanel")w=1;
                if(screen=="commander"&&r.parent?.name=="CommanderStatsPanel"&&r.name.StartsWith("Divider"))x=r.anchoredPosition.x/860f;
                if(r.name=="Credits"||r.name=="CreditsChip"||r.name=="SettingsButton")x=1;
                bool mirror=top||r.parent?.name=="MissionSelectState"||(screen=="skirmish"&&(r.name=="MapPreviewClip"||r.name=="SeedHelp"));
                // Stretch anchors already inherit their parent; never drive those a second time.
                if(r.anchorMin!=r.anchorMax)continue;
                if(x!=0||w!=0||y!=0||h!=0||mirror)rules.Add(new MenuFrameTarget(r,new Vector2(x,y),new Vector2(w,h),mirror));
            }
            frame.gameObject.AddComponent<MenuTypographyView>().Configure(frame.GetComponentsInChildren<TMP_Text>(true));
            frame.ConfigureMenuFrame(rules.ToArray());
        }
    }
}
