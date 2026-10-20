using System;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        [SerializeField] private MissionAtlasScreenView approvedSkirmishAtlas;
        [SerializeField] private Sprite[] approvedObjectiveIcons;
        private bool approvedAtlasBound;
        private string atlasBrowseMap;
        public MissionAtlasScreenView ApprovedAtlas=>approvedSkirmishAtlas;
        private static string AtlasArmy(string id,bool fa)=>id=="A"?(fa?"هوایی":"AIR"):id=="C"?(fa?"ترکیبی":"COMBINED"):(fa?"زمینی":"GROUND");
        private static string AtlasStart(string id,bool fa)=>id=="E"?(fa?"پایگاه":"BASE"):(fa?"میدانی":"FIELD");
        private string AtlasText(string key,string fallback="")=>UiShellRuntimeGateway.Localization.Get(key,fallback);
        private void OpenAtlasMap(string id)
        {
            EnsureLibraryViewModel();
            if(!_libraryViewModel.SelectMapCollection(id))return;
            _mapFilter=id;EnsureLibraryCards();
            var cards=_libraryViewModel.Battlefield.GridCards;
            string first=null;
            foreach(var card in cards)
            {
                if(string.IsNullOrEmpty(card.ScenarioId))continue;
                first??=card.ScenarioId;
                if(card.Access==SkirmishCardAccess.Playable){first=card.ScenarioId;break;}
            }
            if(!string.IsNullOrEmpty(first))SelectScenarioId(first);
        }
        private void SelectAtlasObjective(int index)
        {
            SelectLibraryObjective(SkirmishBattleLibraryArt.ObjectiveIds[index]);
            string id=_libraryViewModel.Briefing.ScenarioId;if(!string.IsNullOrEmpty(id))SelectScenarioId(id);
        }
        private void PresentApprovedSkirmishAtlas()
        {
            if(approvedSkirmishAtlas==null || _libraryPresenting)return;
            _libraryPresenting=true;
            try
            {
                EnsureLibraryCards();EnsureLibraryViewModel();
                if(!approvedAtlasBound)
                {
                    approvedAtlasBound=true;
                    approvedSkirmishAtlas.Back=()=> {if(LibraryLevel==SkirmishLibraryLevel.Battlefield)OnLibraryTheater();else UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute,UIRoute.MainMenu,false);};
                    approvedSkirmishAtlas.Secondary=()=> {if(LibraryLevel==SkirmishLibraryLevel.Battlefield)OnLibraryTheater();else OnLibraryQuickBattle();};
                    approvedSkirmishAtlas.Primary=()=> {if(LibraryLevel==SkirmishLibraryLevel.Battlefield)LaunchMatch();else OpenAtlasMap(atlasBrowseMap??LibraryMapId);};
                    approvedSkirmishAtlas.Navigate=i=>
                    {
                        if(LibraryLevel==SkirmishLibraryLevel.Theater){if(i<5){atlasBrowseMap=SkirmishBattleLibraryArt.MapIds[i];PresentApprovedSkirmishAtlas();}}
                        else if(i<4)SelectAtlasObjective(i);
                    };
                    approvedSkirmishAtlas.ChooseZone=i=> {atlasBrowseMap=SkirmishBattleLibraryArt.MapIds[i];PresentApprovedSkirmishAtlas();};
                    approvedSkirmishAtlas.ChooseMission=i=> {var cards=_libraryViewModel.Battlefield.GridCards;if(i<cards.Length && !string.IsNullOrEmpty(cards[i].ScenarioId))SelectScenarioId(cards[i].ScenarioId);};
                    approvedSkirmishAtlas.ChooseDifficulty=i=>{_libraryViewModel.SelectDifficulty((SkirmishDifficultyId)(i+1));PresentScenarioChoices();};
                    approvedSkirmishAtlas.ScrollRegion=i=>{if(i<4)SelectAtlasObjective(i);};
                }
                var theater=_libraryViewModel.Theater;var field=_libraryViewModel.Battlefield;var briefing=_libraryViewModel.Briefing;
                bool missions=LibraryLevel==SkirmishLibraryLevel.Battlefield,fa=UiShellRuntimeGateway.Localization.IsRightToLeft;
                string map=missions?field.MapId:atlasBrowseMap??theater.SelectedMapId;
                atlasBrowseMap=map;bool mapVisible=Array.Exists(theater.Pins,p=>p.MapId==map);SkirmishBattleLibraryArt.Copy(map,fa,out string mapTitle,out string geography);
                int objective=Array.IndexOf(SkirmishBattleLibraryArt.ObjectiveIds,field.SelectedObjectiveId);objective=Mathf.Max(0,objective);
                var navigation=new MissionAtlasItem[missions?4:5];
                for(int i=0;i<navigation.Length;i++)
                {
                    if(missions)
                    {
                        var tab=field.Tabs[i];string name=AtlasText(tab.TitleLocKey).ToUpperInvariant();
                        navigation[i]=new MissionAtlasItem {Id=SkirmishLibraryControlNames.ObjectiveTab(tab.ObjectiveId),Title=name,Subtitle=fa?"۶ نبرد":"6 BATTLES",Enabled=true,Selected=i==objective,Icon=true,Status=tab.PublishedCount>0?tab.WonCount+" / "+tab.PublishedCount:"",Comic=approvedObjectiveIcons!=null&&i<approvedObjectiveIcons.Length?approvedObjectiveIcons[i]:Resources.Load<Sprite>(SkirmishBattleLibraryArt.ObjectiveIcon(tab.ObjectiveId)),Medals=-1};
                    }
                    else
                    {
                        string id=SkirmishBattleLibraryArt.MapIds[i];SkirmishBattleLibraryArt.Copy(id,fa,out string title,out string geo);
                        var pin=Array.Find(theater.Pins,p=>p.MapId==id);
                        navigation[i]=new MissionAtlasItem {Id=SkirmishLibraryControlNames.TheaterPin(id),Title=title,Subtitle=fa?"۲۴ ماموریت":"24 MISSIONS",Enabled=true,Selected=map==id,Medals=-1,Status=pin.PublishedCount>0?pin.WonCount+" / "+pin.PublishedCount:"",Texture=SkirmishLibraryPreviewAssets.Load(SkirmishLibraryPreviewAssets.ResolvePlateKey(id))};
                    }
                }
                var items=new MissionAtlasItem[24];
                for(int i=0;i<24;i++)
                {
                    var card=field.GridCards[i];
                    // Preserve actual catalog IDs on release placeholders without changing availability.
                    if(string.IsNullOrEmpty(card.ScenarioId))
                    {
                        int mapIndex=Array.IndexOf(SkirmishBattleLibraryArt.MapIds,map);
                        string id="S"+(mapIndex*24+i+1).ToString("000");
                        if(mapIndex>=0 && TryFindEntry(id,out var future))card.ScenarioId=future.ScenarioId;
                    }
                    bool ready=card.Access==SkirmishCardAccess.Playable;
                    items[i]=new MissionAtlasItem {Id=SkirmishLibraryControlNames.BattleCard(card.ScenarioId),Title=card.ScenarioId,Subtitle=ready?AtlasArmy(card.ArmyProfile,fa)+" · "+AtlasStart(card.StartProfile,fa):(card.Access==SkirmishCardAccess.NotOwned?AtlasText(card.LockedReasonLocKey):SkirmishBattleLibraryArt.ComingSoon(fa)),Medals=ready?(int)card.Medals:-1,Dimmed=!ready,Status="",Texture=ResolveGridPreview(card.PreviewSpriteKey,card.ScenarioId,card.MapId,out _),Selected=card.IsSelected,Enabled=!string.IsNullOrEmpty(card.ScenarioId) && (ready || SkirmishLibraryViewModel.ShowPlannedCards)};
                }
                string[] titles=new string[4];for(int i=0;i<4;i++)titles[i]=AtlasText(field.Tabs[i].TitleLocKey)+" · "+(i+1)+" / 4";
                Texture plate=SkirmishLibraryPreviewAssets.Load(SkirmishLibraryPreviewAssets.ResolvePlateKey(map));
                Texture preview=plate;var metadata=Array.Find(field.GridCards,c=>c.ScenarioId==briefing.ScenarioId);
                foreach(var card in field.GridCards)if(card.ScenarioId==briefing.ScenarioId)preview=ResolveGridPreview(card.PreviewSpriteKey,card.ScenarioId,card.MapId,out _);
                string purpose=AtlasText(briefing.PurposeLocKey,AtlasText(briefing.DescriptionLocKey));
                if((purpose.StartsWith("Objective for ",StringComparison.OrdinalIgnoreCase)||purpose.StartsWith("هدف ",StringComparison.Ordinal))&&field.SelectedObjectiveId=="BA")
                    purpose=fa?"ستاد دشمن را نابود کن و از ستاد خودت محافظت کن.":"Destroy the enemy HQ. Keep your own HQ standing.";
                if(briefing.Access!=SkirmishCardAccess.Playable)purpose=AtlasText(briefing.LockedReasonLocKey,SkirmishBattleLibraryArt.ComingSoon(fa));
                approvedSkirmishAtlas.Present(new MissionAtlasPresentation {Missions=missions,Skirmish=true,Heading=missions?(fa?"درگیری / ":"SKIRMISH / ")+mapTitle:(fa?"درگیری / انتخاب نقشه":"SKIRMISH / SELECT MAP"),Navigation=navigation,MissionsOnMap=items,RegionTitles=titles,SelectedRegion=objective,Overview=SkirmishLibraryPreviewAssets.Load(SkirmishLibraryPreviewAssets.TheatreBaseKey),Map=plate,DetailTitle=missions?briefing.ScenarioId+" · "+AtlasText(briefing.ObjectiveLocKey):mapTitle,Purpose=missions?purpose:geography,Objectives=missions?string.Empty:fa?"نقشه را انتخاب کنید، سپس ماموریت را انتخاب کنید.":"Choose a map, then select a mission.",Preview=missions?preview:plate,Primary=missions?(_savedMatchAvailable?(fa?"ادامه نبرد":"CONTINUE BATTLE"):(fa?"آغاز نبرد":"START BATTLE")):(fa?"انتخاب ماموریت‌ها":"CHOOSE MISSIONS"),Secondary=missions?(fa?"نقشه‌ها":"MAPS"):(fa?"نبرد سریع":"QUICK BATTLE"),CanLaunch=missions?briefing.CanLaunch:mapVisible});
                bool[] allowed=new bool[4];for(int i=0;i<4;i++)allowed[i]=Array.IndexOf(briefing.AvailableDifficulties??Array.Empty<SkirmishDifficultyId>(),(SkirmishDifficultyId)(i+1))>=0;
                approvedSkirmishAtlas.SetDifficulties(fa?new[]{"تازه‌کار","معمولی","کهنه‌کار","فرمانده"}:new[]{"RECRUIT","REGULAR","VETERAN","COMMANDER"},(int)briefing.SelectedDifficulty-1,allowed);
                if(missionSizeSegmented!=null && missionSizeSegmented.transform.parent==approvedSkirmishAtlas.ExtraControls)
                {MissionAtlasScreenView.Fill(missionSizeSegmented.transform as RectTransform);missionSizeSegmented.gameObject.SetActive(missions && _config.SkirmishSelection.Kind==UiSkirmishLibraryLaunchKind.ExpandedDefinition);}
            }
            finally {_libraryPresenting=false;}
        }
    }
}
