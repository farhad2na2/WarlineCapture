using System;
using System.Collections.Generic;
using System.Linq;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.MenuUiApprovedAuthoring;
namespace Game.Editor
{
    public static class InnerScreenUiAuthoring
    {
        public static void Apply(GameObject root,string screen)
        {
            if(screen=="mission-briefing") Briefing(root);
            if(screen=="squad-preparation") Prep(root);
            NormalizeHeader(root);
            if(screen=="store") Store(root);
            if(screen=="armory") Armory(root);
            if(screen=="district-detail") District(root);
            if(screen=="command-feed") Feed(root);
            MenuUiApprovedAuthoring.Apply(root,screen);
            // Retain action bindings, replace decorative header copy with the shared title.
            Hide(root,"CommandChip","CloseButton","HeaderBackdropClip");
            var title=Find(root,"ScreenTitlePanel")?.Find("ScreenTitle")?.GetComponent<TMP_Text>();
            if(title!=null)Bind(title,screen=="squad-preparation"?"SQUAD PREPARATION":screen=="armory"?"ARMORY":screen=="store"?"STORE":screen=="district-detail"?"DISTRICT DETAIL":screen=="command-feed"?"COMMAND FEED":"MISSION BRIEFING");
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
            { text.overflowMode=TextOverflowModes.Overflow; if(text.name!="MissionNumber")text.textWrappingMode=TextWrappingModes.Normal; }
            foreach(var button in root.GetComponentsInChildren<Button>(true))
            { var label=button.transform.Find("Label")?.GetComponent<TMP_Text>();if(label==null)continue;float font=button.transform.parent?.name=="CategoryRail"?24:30;label.fontSize=font;label.GetComponent<V3LocalizedTextBindingView>()?.ConfigureFontBounds(font,font); }
            foreach(var typography in root.GetComponentsInChildren<MenuTypographyView>(true))typography.Configure(typography.GetComponentsInChildren<TMP_Text>(true));
            if(screen=="mission-briefing"||screen=="squad-preparation") Frame(root,screen);
        }
        private static void NormalizeHeader(GameObject root)
        {
            var frame=root.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true).FirstOrDefault(v=>v.name=="HeaderContent")??root.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            foreach(string name in new[]{"Credits","CreditsChip","CreditsVisualPanel","CreditsPanel","SettingsButton","BackButton","HeaderBackButton","ScreenTitlePanel"})
            {var r=Find(root,name);if(r!=null)r.SetParent(frame.transform,false);}
            var back=Find(root,"BackButton")??Find(root,"HeaderBackButton");if(back?.Find("BackIcon") is Transform oldIcon)oldIcon.name="Icon";
            var credits=Find(root,"CreditsPanel");var icon=credits?.Find("Icon");if(icon!=null){foreach(Transform child in icon)child.gameObject.SetActive(false);var gold=icon.GetComponent<Image>()??icon.gameObject.AddComponent<Image>();gold.sprite=V3UiFoundationBuilder.RequireCatalog().CreditsIcon;gold.raycastTarget=false;}
            Hide(root,"Header","HeaderBar","StoreBrand");
        }
        private static void Store(GameObject root)
        {
            Hide(root,"DetailTimer");
            var purchase=root.GetComponent<StoreCommandExchangeV3View>().PurchaseButton;
            purchase.GetComponent<V3GradientGraphic>()?.Configure(new Color32(30,40,44,255),new Color32(8,15,18,255),new Color32(70,82,86,255),2);
            foreach(var text in purchase.GetComponentsInChildren<TMP_Text>(true))text.color=new Color32(166,178,183,255);
            foreach(var slot in root.GetComponentsInChildren<RectTransform>(true).Where(r=>r.name.StartsWith("OfferSlot_")))
            {
                var title=slot.Find("Title") as RectTransform;title.offsetMin=new Vector2(17,-94);title.offsetMax=new Vector2(-17,-8);
                var summary=slot.Find("Summary") as RectTransform;summary.offsetMin=new Vector2(17,-284);summary.offsetMax=new Vector2(-144,-178);
                summary.GetComponent<TMP_Text>().textWrappingMode=TextWrappingModes.Normal;
            }
            var detail=Find(root,"DetailPanel");var content=ScrollContent(detail,"DetailScroll",1000);
            Place(content.Find("DetailTitle") as RectTransform,20,8,399,100);
            Place(content.Find("DetailArtClip") as RectTransform,12,120,423,230);
            Place(content.Find("IncludesTitle") as RectTransform,20,366,399,48);
            for(int i=0;i<4;i++){var row=content.Find("DetailLine_"+i) as RectTransform;Place(row,20,426+i*112,399,100);Place(row?.Find("Text") as RectTransform,60,0,327,100);if(row?.Find("Text")?.GetComponent<TMP_Text>() is TMP_Text t)t.textWrappingMode=TextWrappingModes.Normal;}
            Place(content.Find("DetailNote") as RectTransform,20,898,399,160);
            content.sizeDelta=new Vector2(0,1080);
            Hide(root,"Shield");
            foreach(var label in Find(root,"CategoryRail").GetComponentsInChildren<TMP_Text>(true)){Place(label.rectTransform,96,4,152,92);label.textWrappingMode=TextWrappingModes.Normal;}
            var reason=Text(root,"UnavailableReason");if(reason!=null){Place(reason.rectTransform,20,34,550,76);reason.textWrappingMode=TextWrappingModes.Normal;}
            Find(root,"Eligibility")?.gameObject.SetActive(false);
        }
        private static void Armory(GameObject root)
        {
            Hide(root,"LevelText","LevelLabel","LevelValue","LevelTrack","LevelFill","UpgradeButton","EquipButton");
            var grid=Find(root,"CatalogGrid");grid.GetComponent<ArmoryV3ResponsiveCatalogGridView>().Configure(3,370);
            var item=Find(root,"ItemView");
            foreach(string n in new[]{"OwnedBand","OwnedText","ProgressTrack","ProgressFill"})item.Find(n)?.gameObject.SetActive(false);
            foreach(var r in item.GetComponentsInChildren<RectTransform>(true).Where(r=>r.name.Contains("Chevron")))r.gameObject.SetActive(false);
            var title=item.Find("TitleText") as RectTransform;title.offsetMin=new Vector2(8,-96);title.offsetMax=new Vector2(-8,-6);title.GetComponent<TMP_Text>().textWrappingMode=TextWrappingModes.Normal;
            foreach(Transform child in item)if(child.name.StartsWith("CardArt")){var r=child as RectTransform;r.offsetMin=new Vector2(3,-290);r.offsetMax=new Vector2(-3,-106);}
            var type=item.Find("TypeText") as RectTransform;Place(type,10,300,268,62);type.GetComponent<TMP_Text>().textWrappingMode=TextWrappingModes.Normal;
            Place(Find(root,"FilterButton"),440,6,240,90);Place(Find(root,"SortButton"),692,6,240,90);
            foreach(string n in new[]{"FilterButton","SortButton"}){var r=Find(root,n);Place(r.Find("Label") as RectTransform,12,4,202,82);r.Find("Label").GetComponent<TMP_Text>().textWrappingMode=TextWrappingModes.Normal;}
            var viewport=Find(root,"CatalogViewport");Place(viewport,12,108,922,578);Place(Find(root,"HeaderDivider"),12,102,922,2);
            var inspection=Find(root,"InspectionPanel");var content=ScrollContent(inspection,"InspectionScroll",1200);
            Place(content.Find("TitleText") as RectTransform,18,8,373,100);Place(content.Find("TypeText") as RectTransform,18,116,373,70);
            foreach(Transform child in content)if(child.name.StartsWith("InspectionArt"))Place(child as RectTransform,16,196,377,200);
            var description=Find(root,"DescriptionText");description.SetParent(content,false);description.gameObject.SetActive(true);Place(description,18,408,373,180);description.GetComponent<TMP_Text>().textWrappingMode=TextWrappingModes.Normal;
            string[] stats={"HealthRow","DamageRow","RangeRow","SpeedRow"};for(int i=0;i<stats.Length;i++){var row=Find(root,stats[i]);Place(row,16,600+i*112,377,100);Place(row.Find("Label") as RectTransform,48,4,214,92);Place(row.Find("Value") as RectTransform,266,4,96,92);row.Find("Label").GetComponent<TMP_Text>().textWrappingMode=TextWrappingModes.Normal;}
        }
        private static void District(GameObject root)
        {
            var composition=Composition(root);
            Hide(root,"KeyStats","IntelConfidence","KnownThreats","RecentActivity","AriaStatus","TacticalInset","ActionBar");
            var visual=Find(root,"DistrictVisual");Place(visual,14,108,954,680);
            var clip=Find(root,"DistrictClip");clip.anchorMin=Vector2.zero;clip.anchorMax=Vector2.one;clip.offsetMin=Vector2.one*3;clip.offsetMax=Vector2.one*-3;
            Place(Find(root,"DistrictName"),28,20,886,110);Place(Find(root,"ThreatLabel"),28,140,886,80);Bind(Text(root,"ThreatLabel"),"NO DISTRICT REPORT");Bind(Text(root,"DistrictName"),"DISTRICT OVERVIEW");
            var panel=Panel(composition,"DistrictReport",980,108,676,680);
            NewText(panel,"ReportTitle","DISTRICT REPORT",24,20,628,90);
            NewText(panel,"ReportHelp","No district report is available. Open Operations to review current objectives and available missions.",24,134,628,220);
            var back=Panel(composition,"DistrictOperationsButton",14,804,1642,123);var button=back.gameObject.AddComponent<Button>();button.targetGraphic=back.GetComponent<V3GradientGraphic>();back.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenMenuRoute,UIRoute.Operations,false);NewText(back,"Label","VIEW OPERATIONS",24,8,1594,107);
        }
        private static void Feed(GameObject root)
        {
            var composition=Composition(root);var rows=Find(root,"FeedRows");Hide(root,"SituationPanel","LiveStatus","OpenIntelButton");
            foreach(Transform filter in Find(root,"FilterRail")){filter.Find("Count")?.gameObject.SetActive(false);Place(filter.Find("Label") as RectTransform,92,10,213,70);}
            Bind(Text(root,"LiveFeed"),"COMMAND RECORD");Place(Find(root,"LiveFeed"),17,3,670,49);
            var panel=Panel(composition,"FeedViewport",342,176,891,750);rows.SetParent(panel,false);rows.anchorMin=new Vector2(0,1);rows.anchorMax=new Vector2(1,1);rows.sizeDelta=new Vector2(0,2000);rows.anchoredPosition=Vector2.zero;
            panel.gameObject.AddComponent<RectMask2D>();var scroll=panel.gameObject.AddComponent<ScrollRect>();scroll.viewport=panel;scroll.content=rows;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var ordered=new List<RectTransform>();foreach(RectTransform row in rows){Place(row,0,0,891,320);ordered.Add(row);foreach(string n in new[]{"Time","Tag","ArtClip"})row.Find(n)?.gameObject.SetActive(false);var icon=row.Find("IconPanel") as RectTransform;Place(icon,8,8,102,304);var copy=row.Find("Copy") as RectTransform;Place(copy,118,8,757,304);Place(copy.Find("Title") as RectTransform,12,4,733,100);copy.Find("Subtitle")?.gameObject.SetActive(false);Place(copy.Find("Body") as RectTransform,12,114,733,178);copy.Find("Title").GetComponent<TMP_Text>().textWrappingMode=TextWrappingModes.Normal;}
            Flow(rows,ordered);root.AddComponent<CommandFeedFactsView>().Configure(ordered.Select(r=>r.Find("Copy/Title").GetComponent<TMP_Text>()).ToArray(),ordered.Select(r=>r.Find("Copy/Body").GetComponent<TMP_Text>()).ToArray());
            var aria=Find(root,"AriaPanel");Hide(aria.gameObject,"Role","Status");Place(aria,0,0,413,600);NewText(aria,"FeedAriaHelp","Review the mission objectives before deployment.",16,314,381,260);
            Place(Find(root,"ViewOperationButton"),0,690,413,123);Place(Find(root,"OpenIntelButton"),0,588,413,92);
            var pause=Find(root,"PauseButton");Place(pause,703,0,86,64);var search=Find(root,"SearchButton");Place(search,797,0,86,64);Place(Find(root,"LiveStatus"),170,4,516,56);
        }
        private static RectTransform Composition(GameObject root)=>root.GetComponentInChildren<MainMenuV3SectionLayoutView>(true).GetComponent<RectTransform>();
        private static TMP_Text Dynamic(Transform p,string n,float w,float h)
        {var t=NewText(p,n,"",24,0,w-48,h);t.overflowMode=TextOverflowModes.Overflow;return t;}
        private static TMP_Text Section(RectTransform content,string heading,string name,float width,float minimum,List<RectTransform> flow)
        {var h=NewText(content,name+"Heading",heading,24,0,width-48,46);h.color=new Color32(148,190,220,255);flow.Add(h.rectTransform);var copy=Dynamic(content,name,width,minimum);flow.Add(copy.rectTransform);return copy;}
        private static void Flow(RectTransform content,List<RectTransform> rows)
        {content.gameObject.AddComponent<MenuBriefingFlowView>().Configure(rows.ToArray(),true);}
        private static void Back(RectTransform parent)
        {
            var back=Panel(parent,"BackButton",14,10,180,86);
            back.gameObject.AddComponent<Button>();back.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.BackMenuRoute,UIRoute.Campaign,false);
            NewText(back,"Label","BACK",20,8,140,70);
        }
        private static void Footer(GameObject root,RectTransform composition,string secondaryName,string secondaryCopy)
        {
            var secondary=Find(root,secondaryName);secondary.SetParent(composition,false);Place(secondary,14,804,610,123);
            Bind(secondary.Find("Label")?.GetComponent<TMP_Text>(),secondaryCopy);
            if(secondaryName=="EditLoadoutButton")foreach(var graphic in secondary.GetComponentsInChildren<Image>(true))graphic.gameObject.SetActive(false);
            Place(secondary.Find("Label") as RectTransform,90,10,500,103);
            secondary.GetComponent<V3GradientGraphic>()?.Configure(new Color32(23,111,190,255),new Color32(3,52,101,255),new Color32(68,137,190,255),2);
            var deploy=root.GetComponentInChildren<MissionBriefingScreenView>(true)?.DeployOperationButton??root.GetComponentInChildren<LoadoutSquadPrepScreenView>(true).DeployButton;
            deploy.transform.SetParent(composition,false);Place(deploy.GetComponent<RectTransform>(),636,804,1020,123);
            Bind(deploy.transform.Find("Label")?.GetComponent<TMP_Text>(),"DEPLOY OPERATION");
            Place(deploy.transform.Find("Label") as RectTransform,24,8,940,107);
            deploy.GetComponent<V3GradientGraphic>()?.Configure(new Color32(80,145,45,255),new Color32(22,73,24,255),new Color32(116,181,48,255),2);
            if(deploy.transform.Find("Label")?.GetComponent<TMP_Text>() is TMP_Text t)t.color=Color.white;
            Hide(deploy.gameObject,"CostIcon","RightChevrons","LeftChevrons");
        }
        private static void Briefing(GameObject root)
        {
            var composition=Composition(root);var view=root.GetComponentInChildren<MissionBriefingScreenView>(true);view.ConfigureApprovedInnerLayout();
            Hide(root,"PrimaryObjectives","TacticalConditions","EnemyIntel","Rewards","EnemyCommander","StarGoals","TitleBand","TitleRule","BriefingLabel");
            Place(view.MissionOverview,14,108,938,680);
            foreach(string name in new[]{"MissionArtClip","MissionCopyOverlay"}){var r=Find(root,name);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*3;r.offsetMax=Vector2.one*-3;}
            Place(view.MissionNumber.rectTransform,28,26,850,48);Place(view.MissionTitle.rectTransform,28,80,850,100);
            var chapter=Find(root,"ChapterLabel").GetComponent<TMP_Text>();Place(chapter.rectTransform,28,182,850,76);
            Place(view.MissionSummary.rectTransform,28,350,850,302);view.MissionSummary.gameObject.SetActive(false);
            var visibleSummary=NewText(view.MissionOverview,"ApprovedHeroSummary","",28,350,850,302);visibleSummary.textWrappingMode=TextWrappingModes.Normal;
            var panel=Panel(composition,"ApprovedMissionIntel",964,108,692,680);var content=ScrollContent(panel,"IntelScroll",1800);var rows=new List<RectTransform>();
            var objectives=Section(content,"PRIMARY OBJECTIVES","ApprovedObjectives",684,180,rows);
            var conditions=Section(content,"MISSION CONDITIONS","ApprovedConditions",684,260,rows);
            var intel=Section(content,"ENEMY INTEL","ApprovedIntel",684,100,rows);
            var rewards=Section(content,"REWARDS","ApprovedRewards",684,96,rows);
            var goals=Section(content,"STAR GOALS","ApprovedGoals",684,220,rows);
            var evidence=Find(root,"EvidenceChainRoutes");evidence.SetParent(content,false);Place(evidence,0,0,684,390);rows.Add(evidence);
            Flow(content,rows);
            root.AddComponent<CampaignInnerMissionView>().Configure(view,chapter,null,visibleSummary,objectives,conditions,intel,rewards,goals,null,null,null,null);
            Footer(root,composition,"LoadoutButton","SQUAD PREPARATION");
            MenuAccountHeaderAuthoring.Apply(root);
        }
        private static void Prep(GameObject root)
        {
            var composition=Composition(root);Hide(root,"SelectedUnits","SupportAndGear","MissionSummary","Footer");Back(composition);
            var force=Panel(composition,"ApprovedForce",14,108,610,680);
            var portrait=Find(root,"RifleSquadArt").GetComponent<Image>();var image=new GameObject("MissionForcePortrait",typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.transform.SetParent(force,false);image.sprite=portrait.sprite;image.preserveAspect=true;image.raycastTarget=false;Place(image.rectTransform,6,6,598,400);
            NewText(force,"ForceHeading","MISSION FORCE",24,410,562,46);var forceCopy=Dynamic(force,"AssignedForce",610,146);Place(forceCopy.rectTransform,24,468,562,184);
            var plan=Panel(composition,"ApprovedPlan",636,108,550,680);var planContent=ScrollContent(plan,"PlanScroll",1500);var rows=new List<RectTransform>();
            var chapter=Dynamic(planContent,"MissionChapter",542,68);rows.Add(chapter.rectTransform);var title=Dynamic(planContent,"SelectedMissionTitle",542,94);title.fontSize=38;rows.Add(title.rectTransform);
            var artClip=new GameObject("SelectedMissionArtClip",typeof(RectTransform),typeof(RectMask2D)).GetComponent<RectTransform>();artClip.SetParent(planContent,false);Place(artClip,24,0,494,190);var artwork=new GameObject("SelectedMissionArt",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter)).GetComponent<RawImage>();artwork.transform.SetParent(artClip,false);artwork.raycastTarget=false;artwork.GetComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;rows.Add(artClip);
            var summary=Section(planContent,"MISSION PLAN","SelectedMissionSummary",542,160,rows);var objectives=Section(planContent,"PRIMARY OBJECTIVES","AssignedObjectives",542,220,rows);Flow(planContent,rows);
            var info=Panel(composition,"ApprovedConditionsPanel",1198,108,458,680);var infoContent=ScrollContent(info,"ConditionsScroll",1500);rows=new List<RectTransform>();
            var conditions=Section(infoContent,"MISSION CONDITIONS","AssignedConditions",450,290,rows);NewText(infoContent,"AriaHeading","ARIA",24,0,402,46);rows.Add(infoContent.Find("AriaHeading") as RectTransform);
            var aria=NewText(infoContent,"AriaHelp","Review the mission objectives before deployment.",24,0,402,120);rows.Add(aria.rectTransform);
            var ariaPortrait=new GameObject("PreparationAriaPortrait",typeof(RectTransform),typeof(Image)).GetComponent<Image>();ariaPortrait.transform.SetParent(infoContent,false);ariaPortrait.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(V3UiFoundationBuilder.SharedAriaPortraitPath);ariaPortrait.preserveAspect=true;ariaPortrait.raycastTarget=false;Place(ariaPortrait.rectTransform,24,0,180,160);rows.Add(ariaPortrait.rectTransform);
            var goals=Section(infoContent,"STAR GOALS","AssignedGoals",450,260,rows);Flow(infoContent,rows);
            var pref=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab");
            var missionSource=pref!=null?pref.GetComponentInChildren<CampaignOperationsScreenView>(true):null;
            var prefixes=new List<string>();var textures=new List<Texture>();
            if(missionSource!=null){var data=new SerializedObject(missionSource);string[] names={"m01","m02","m03","m04","m05","gridlock","supplyLine","marketLifeline","powerRelay","routeReopened","signalTrace","safehouseSweep","falseFront","networkBreak","evidenceChain"};foreach(string n in names){var prop=data.FindProperty(n+"MissionPreview");if(prop==null)continue;prefixes.Add("mission."+(n.StartsWith("m")&&n.Length==3?n:System.Text.RegularExpressions.Regex.Replace(n,"[A-Z]",m=>"_"+m.Value.ToLowerInvariant()))+".");textures.Add(prop.objectReferenceValue as Texture);}}
            root.AddComponent<CampaignInnerMissionView>().Configure(null,chapter,title,summary,objectives,conditions,null,null,goals,forceCopy,artwork,prefixes.ToArray(),textures.ToArray());
            Footer(root,composition,"EditLoadoutButton","VIEW ARMORY");
        }
        private static void Frame(GameObject root,string screen)
        {
            foreach(var frame in root.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
            {
                var rules=new List<MenuFrameTarget>();
                foreach(RectTransform r in frame.transform)
                {
                    bool footer=r.anchoredPosition.y < -790;float x=0,w=0,h=0;
                    if(r.name=="MissionOverview"||r.name=="ApprovedForce"){w=screen=="mission-briefing"?.58f:.38f;h=1;}
                    if(r.name=="ApprovedMissionIntel"){x=.58f;w=.42f;h=1;}
                    if(r.name=="ApprovedPlan"){x=.38f;w=.34f;h=1;}
                    if(r.name=="ApprovedConditionsPanel"){x=.72f;w=.28f;h=1;}
                    if(r.name=="MenuTitlePanel"||r.name=="ScreenTitlePanel")w=1;
                    if(r.name=="CreditsChip"||r.name=="Credits"||r.name=="SettingsButton")x=1;
                    if(footer&&r.name.Contains("Deploy")){x=.38f;w=.62f;}
                    if(footer&&(r.name=="LoadoutButton"||r.name=="EditLoadoutButton"))w=.38f;
                    rules.Add(new MenuFrameTarget(r,new Vector2(x,footer?1:0),new Vector2(w,h),true));
                }
                frame.ConfigureMenuFrame(rules.ToArray());
            }
        }
    }
}
