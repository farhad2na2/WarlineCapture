using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ApprovedMissionAtlasReview
    {
        private static readonly List<string> LabelIssues=new List<string>();
        public static async Task<int> Run()
        {
            LabelIssues.Clear();
            string output="Design/AgentReports/ApprovedMissionAtlas/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(output);
            string save=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"),background=Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION"),locale=GameLocalization.CurrentLocaleCode;
            bool options=EditorSettings.enterPlayModeOptionsEnabled;var flags=EditorSettings.enterPlayModeOptions;
            var oldInput=InputSystem.settings;InputSettings input=null;AriaTouchInputUiSystemHelper touch=null;bool passed=false;
            try
            {
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",Path.Combine(Path.GetTempPath(),"warline-approved-atlas-"+Guid.NewGuid().ToString("N")));
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","1");MenuUiAuditCapture.PrepareSave();
                EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=flags|EnterPlayModeOptions.DisableDomainReload;
                MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
                EditorApplication.EnterPlaymode();await Until(()=>EditorApplication.isPlaying,90);
                input=UnityEngine.Object.Instantiate(oldInput);input.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;input.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=input;
                Application.runInBackground=true;await Route(UIRoute.MainMenu);await Task.Delay(6000);
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();touch=new AriaTouchInputUiSystemHelper();if(!touch.Start())throw new InvalidOperationException("Touch device unavailable");
                foreach(string language in new[]{"en","fa-IR"})
                {
                    GameLocalization.SetLocale(language,false);await Task.Delay(900);
                    await Open(UIRoute.Campaign);var campaign=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();
                    if(campaign==null||campaign.ApprovedAtlas==null)throw new InvalidOperationException("Active Campaign native binding missing");
                    var c=campaign.ApprovedAtlas;
                    if(c.ShowingMissions)await Tap(touch,c.BackButton);
                    CheckOverviewArt(c);Dump(c);await Shot(output,language,"campaign-chapters");await Matrix(output,language,"campaign-chapters",c);
                    await Tap(touch,c.NavigationCards[2].Button);await Until(()=>campaign.IsChapterThree,15);await Task.Delay(700);
                    var comic=UnityEngine.Object.FindAnyObjectByType<FutureMissionComicPreviewController>();
                    if(comic!=null && comic.IsOpen)
                    {
                        await Shot(output,language,"campaign-chapter-comic");
                        Button skip=null;
                        await Until(()=> (skip=UnityEngine.Object.FindObjectsByType<Button>().FirstOrDefault(b=>b.name.Contains("Skip")&&Presented(b)))!=null,20);
                        await Tap(touch,skip);
                    }
                    await Until(()=>c.ShowingMissions,15);await Shot(output,language,"campaign-missions");
                    var card=c.Regions[0].Cards[2];await Tap(touch,card.Button);
                    await Until(()=>UiShellRuntimeGateway.TryReadCampaignOperations(out var model)&&model.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.FalseFront,15);await Shot(output,language,"campaign-false-front");
                    if(language=="fa-IR")
                    {
                        string copy=c.GetComponentsInChildren<TMPro.TMP_Text>(false).First(t=>t.name=="Purpose").text;
                        if(!copy.Any(ch=>(ch>=0x0600&&ch<=0x06ff)||(ch>=0xfb50&&ch<=0xfeff)))
                            throw new InvalidOperationException("Campaign purpose lost its Persian translation when shortened");
                    }
                    await Matrix(output,language,"campaign-missions",c);
                    await Tap(touch,c.PrimaryButton);await Route(UIRoute.MissionBriefing);
                    var briefingBack=UnityEngine.Object.FindObjectsByType<UIShellRouteButtonView>().First(b=>b.Route==UIRoute.Campaign&&Presented(b.GetComponent<Button>()));
                    await Tap(touch,briefingBack.GetComponent<Button>());await Route(UIRoute.Campaign);
                    campaign=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();c=campaign.ApprovedAtlas;
                    await Until(()=>c.ShowingMissions,15);
                    if(c.Regions[0].Cards.Count(x=>x.Rect.gameObject.activeSelf)!=5)throw new InvalidOperationException("Campaign must show exactly five missions");
                    for(int i=0;i<5;i++)if(c.Regions[0].Cards[i].Art.Comic==null)throw new InvalidOperationException("Missing mission comic "+i);
                    await Tap(touch,c.BackButton);if(c.ShowingMissions)throw new InvalidOperationException("Back did not return to chapter atlas");
                    await Tap(touch,c.BackButton);await Route(UIRoute.MainMenu);
                    await Open(UIRoute.QuickCustomSetup);var skirmish=UnityEngine.Object.FindAnyObjectByType<QuickCustomScreenView>();var s=skirmish.ApprovedAtlas;
                    if(s.ShowingMissions)await Tap(touch,s.BackButton);CheckOverviewArt(s);await Shot(output,language,"skirmish-maps");await Matrix(output,language,"skirmish-maps",s);
                    await Tap(touch,s.TerritoryCards[1].Button);
                    if(s.TerritoryCards[1].Title.color==Color.white)throw new InvalidOperationException("Map choice lost its territory accent");
                    await Tap(touch,s.TerritoryCards[0].Button);await Tap(touch,s.PrimaryButton);await Until(()=>s.ShowingMissions,15);await Shot(output,language,"skirmish-missions");
                    await Tap(touch,s.Regions[0].Cards[1].Button);await Until(()=>skirmish.LibraryViewModel.Briefing.ScenarioId=="S002",15);await Shot(output,language,"skirmish-s002");
                    if(skirmish.MissionSizeControl==null||!skirmish.MissionSizeControl.gameObject.activeInHierarchy)throw new InvalidOperationException("Real S002 mission-size control is missing");
                    await Matrix(output,language,"skirmish-missions",s);
                    foreach(int option in new[]{1,2,0})
                    {
                        var control=skirmish.MissionSizeControl;
                        var button=control.SegmentButtons[option];
                        var expected=option==1?UiSkirmishMissionSize.War:option==2?UiSkirmishMissionSize.LargeWar:UiSkirmishMissionSize.Standard;
                        if(control.SelectedIndex!=option && button.IsInteractable())
                        {
                            await Tap(touch,button);
                            await Until(()=>skirmish.SelectedMissionSize==expected,10);
                            var compiled=skirmish.PresentedMissionSizeSetup;
                            var expectedSize=option==1?Game.Skirmish.Contracts.SkirmishSizeId.War:option==2?Game.Skirmish.Contracts.SkirmishSizeId.LargeWar:Game.Skirmish.Contracts.SkirmishSizeId.Standard;
                            if(compiled==null||compiled.SizeId!=expectedSize)throw new InvalidOperationException("Army-size choice does not match compiled setup");
                            await Shot(output,language,"army-size-"+expected);
                        }
                        else if(control.SelectedIndex!=option)
                        {
                            if(!button.transform.Find("UnavailableChoice").gameObject.activeSelf)throw new InvalidOperationException("Unavailable army-size choice lacks its lock cue");
                            var before=skirmish.SelectedMissionSize;
                            Vector2 point=Center((RectTransform)button.transform);
                            if(!touch.TryGesture(point,point,.18f,0,Time.unscaledTime))throw new InvalidOperationException("Unavailable choice touch rejected");
                            do{touch.Tick(Time.unscaledTime);await Task.Delay(50);}while(touch.IsBusy);await Task.Delay(700);
                            if(skirmish.SelectedMissionSize!=before)throw new InvalidOperationException("Unavailable army-size choice changed setup");
                            Debug.Log("[ArmySizeChoiceReview] unavailableChoice="+expected+" touchIgnored=true locale="+language);
                        }
                        if(control.SegmentButtons.Count(b=>b.transform.Find("SelectedChoice").gameObject.activeSelf)!=1 || !control.SegmentButtons[control.SelectedIndex].transform.Find("SelectedChoice").gameObject.activeSelf)
                            throw new InvalidOperationException("Army-size choice check does not match the real selected size");
                    }
                    Debug.Log("[ArmySizeChoiceReview] result=Passed input=InputSystemTouch actualSelectionAndAvailability=true locale="+language);
                    await Tap(touch,s.DifficultyButtons[3]);
                    await Until(()=>skirmish.LibraryViewModel.Briefing.SelectedDifficulty==Game.Skirmish.Contracts.SkirmishDifficultyId.Commander,10);
                    await Tap(touch,s.DifficultyButtons[1]);
                    await Tap(touch,s.NextRegionButton);await Until(()=>s.VisibleRegion==1,10);
                    await Tap(touch,s.NavigationCards[0].Button);await Until(()=>s.VisibleRegion==0,10);
                    var pinned=s.transform.Find("AtlasFrame/MapPanel/MissionMap/PinnedRegionHeading") as RectTransform;
                    Vector2 pinnedBefore=Center(pinned);
                    var from=Center(s.MissionScroll.viewport)-new Vector2(0,180);var to=from+new Vector2(0,420);
                    if(!touch.TryGesture(from,to,.15f,.7f,Time.unscaledTime))throw new InvalidOperationException("Map swipe rejected");
                    while(touch.IsBusy){touch.Tick(Time.unscaledTime);await Task.Delay(50);}await Task.Delay(500);
                    if(s.MissionScroll.verticalNormalizedPosition>.97f)throw new InvalidOperationException("Native map did not scroll");
                    if(Vector2.Distance(pinnedBefore,Center(pinned))>2)throw new InvalidOperationException("Pinned region heading moved during map swipe");
                    await Tap(touch,s.NavigationCards[3].Button);await Until(()=>s.VisibleRegion==3,15);await Shot(output,language,"skirmish-convoy");
                    if(s.Regions[3].Cards.Count(x=>x.Rect.gameObject.activeSelf)!=6)throw new InvalidOperationException("Skirmish objective must show six choices");
                    await Tap(touch,s.BackButton);await Tap(touch,s.BackButton);await Route(UIRoute.MainMenu);
                }
                if(LabelIssues.Count>0)throw new InvalidOperationException(string.Join("\n",LabelIssues.Distinct()));
                passed=true;Debug.Log("[ApprovedMissionAtlasReview] result=Passed input=InputSystemTouch campaignFiveComics=true skirmishSixPerRegion=true stageBack=true locales=en,fa-IR nativeScreens=true completeMission=Pending deviceAcceptance=Pending output="+output);
            }
            catch(Exception error){Debug.LogException(error);Debug.LogError("[ApprovedMissionAtlasReview] result=Failed output="+output);}
            finally
            {
                touch?.Dispose();if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.ExitPlaymode();await Until(()=>!EditorApplication.isPlayingOrWillChangePlaymode,60);}
                InputSystem.settings=oldInput;if(input!=null)UnityEngine.Object.DestroyImmediate(input);EditorSettings.enterPlayModeOptions=flags;EditorSettings.enterPlayModeOptionsEnabled=options;
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",save);Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",background);GameLocalization.SetLocale(locale,false);
                MissionEditorValidationExit.Complete(passed);
            }
            return passed?0:1;
        }
        private static async Task Matrix(string output,string locale,string name,MissionAtlasScreenView view)
        {
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1440,1080),new Vector2Int(2400,1080)})
            {
                MainMenuV3PrefabBuilder.SetGameViewResolution(size.x,size.y);await Until(()=>Screen.width==size.x&&Screen.height==size.y,15);await Task.Delay(600);
                await Shot(output,locale,name+"-"+size.x+"x"+size.y);
                var settings=view.transform.Find("AtlasFrame/SettingsButton") as RectTransform;
                var gear=settings.Find("Icon") as RectTransform;
                if(Vector2.Distance(Center(settings),Center(gear))>.5f)throw new InvalidOperationException("Settings gear is not centered in its own box");
                if(view.ShowingMissions)
                {
                    var bar=view.MissionScroll.verticalScrollbar;
                    if(bar.gameObject.activeInHierarchy)
                    {
                        var corners=new Vector3[4];bar.handleRect.GetWorldCorners(corners);
                        foreach(var point in corners)
                        {
                            Vector2 local=((RectTransform)bar.transform).InverseTransformPoint(point);
                            Rect track=((RectTransform)bar.transform).rect;track.xMin-=.5f;track.xMax+=.5f;track.yMin-=.5f;track.yMax+=.5f;
                            if(!track.Contains(local))throw new InvalidOperationException("Native scrollbar thumb extends outside its track");
                        }
                    }
                }
                int issuesBefore=LabelIssues.Count;
                var army=view.ExtraControls.GetComponentInChildren<UISegmentedControlView>(false);
                if(army!=null)
                {
                    foreach(var button in army.SegmentButtons)
                    {
                        var text=button.GetComponentInChildren<TMPro.TMP_Text>();var owner=(RectTransform)button.transform;var ink=InkBounds(text,owner);var bounds=owner.rect;
                        if(ink.xMin<bounds.xMin+8||ink.xMax>bounds.xMax-8||ink.yMin<bounds.yMin+5||ink.yMax>bounds.yMax-12)
                            LabelIssues.Add("Army-size label padding: "+text.text+" at "+size+" ink="+ink+" owner="+bounds);
                        if(button.targetGraphic is not V3GradientGraphic)throw new InvalidOperationException("Army-size choice is missing its native bordered face");
                    }
                }
                foreach(var c in view.NavigationCards.Concat(view.ShowingMissions?view.Regions.SelectMany(r=>r.Cards):view.TerritoryCards))if(c.Rect.gameObject.activeInHierarchy)
                {
                    if(c.MissionCode!=null&&c.MissionCode.gameObject.activeInHierarchy)CheckCardSpacing(c,size);
                    foreach(var t in new[]{c.Title,c.Subtitle,c.Status,c.MissionCode}.Where(t=>t!=null&&t.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(t.text))){t.ForceMeshUpdate();var rect=t.rectTransform.rect;
                        // Glyph metrics use each character's actual fallback font and exclude transparent SDF padding.
                        // The primary Latin font's padding cannot describe Persian fallback glyph quads.
                        bool outside=false;
                        for(int i=0;i<t.textInfo.characterCount;i++)
                        {
                            var ch=t.textInfo.characterInfo[i];if(!ch.isVisible||ch.textElement==null)continue;
                            var m=ch.textElement.glyph.metrics;
                            float left=ch.origin+m.horizontalBearingX*ch.scale,top=ch.baseLine+m.horizontalBearingY*ch.scale;
                            float right=left+m.width*ch.scale,bottom=top-m.height*ch.scale;
                            outside|=left<rect.xMin-4||right>rect.xMax+4||bottom<rect.yMin-4||top>rect.yMax+4;
                        }
                        if(t.isTextTruncated||outside)LabelIssues.Add("Navigation glyph clipping: "+t.text+" at "+size+" rect="+rect);}

                }
                foreach(var t in view.GetComponentsInChildren<TMPro.TMP_Text>(false))
                {
                    if(string.IsNullOrWhiteSpace(t.text))continue;
                    t.ForceMeshUpdate();
                    if(t.isTextTruncated)LabelIssues.Add("Native label truncation: "+t.name+"="+t.text+" at "+size);
                }
                Debug.Log("[ApprovedMissionAtlasReview] ratio="+size+" locale="+locale+" navigationText="+(LabelIssues.Count==issuesBefore?"Passed":"Failed"));
            }
        }
        private static void CheckOverviewArt(MissionAtlasScreenView view)
        {
            for(int index=0;index<view.TerritoryCards.Length;index++)
            {
                var card=view.TerritoryCards[index];
                if(!card.Rect.gameObject.activeInHierarchy)continue;
                var rail=view.NavigationCards[index];
                if(card.Title.color!=rail.Title.color||card.Subtitle.color!=rail.Subtitle.color||card.Status.color!=rail.Status.color)throw new InvalidOperationException("Map/chapter rail and territory copy colors differ: "+card.Rect.name);
                if(!card.Art.gameObject.activeInHierarchy || (card.Art.Comic==null&&card.Art.Image.texture==null))
                    throw new InvalidOperationException("Map/chapter choice is missing its own thumbnail: "+card.Rect.name);
            }
        }
        private static Rect InkBounds(TMPro.TMP_Text text,RectTransform owner)
        {
            text.ForceMeshUpdate();Vector2 lo=new(float.PositiveInfinity,float.PositiveInfinity),hi=new(float.NegativeInfinity,float.NegativeInfinity);
            for(int i=0;i<text.textInfo.characterCount;i++)
            {
                var ch=text.textInfo.characterInfo[i];if(!ch.isVisible||ch.textElement==null)continue;
                var m=ch.textElement.glyph.metrics;float x=ch.origin+m.horizontalBearingX*ch.scale,y=ch.baseLine+m.horizontalBearingY*ch.scale;
                Vector2 a=owner.InverseTransformPoint(text.rectTransform.TransformPoint(new Vector2(x,y)));
                Vector2 b=owner.InverseTransformPoint(text.rectTransform.TransformPoint(new Vector2(x+m.width*ch.scale,y-m.height*ch.scale)));
                lo=Vector2.Min(lo,Vector2.Min(a,b));hi=Vector2.Max(hi,Vector2.Max(a,b));
            }
            return Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);
        }
        private static void CheckCardSpacing(MissionAtlasScreenView.Card c,Vector2Int size)
        {
            var title=InkBounds(c.Title,c.Rect);var code=InkBounds(c.MissionCode,c.Rect);var profile=InkBounds(c.Subtitle,c.Rect);
            string context=c.Rect.name+" at "+size;
            if(title.yMin-code.yMax<2||code.yMin-profile.yMax<2)LabelIssues.Add("Mission text overlaps: "+context);
            foreach(var ink in new[]{title,code,profile})
                if(ink.xMin<c.Rect.rect.xMin+8||ink.xMax>c.Rect.rect.xMax-8||ink.yMax>c.Rect.rect.yMax-6||ink.yMin<c.Rect.rect.yMin+6)
                    LabelIssues.Add("Mission text too close to card edge: "+context);
            if(c.MedalIcons.Length>0&&c.MedalIcons[0].gameObject.activeInHierarchy)
            {
                var medal=c.MedalIcons[0].rectTransform;float top=c.Rect.InverseTransformPoint(medal.TransformPoint(new Vector2(medal.rect.xMin,medal.rect.yMax))).y;
                if(profile.yMin-top<2)LabelIssues.Add("Mission profile overlaps medal row: "+context);
            }
            else if(c.Status.gameObject.activeInHierarchy&&!string.IsNullOrWhiteSpace(c.Status.text)&&profile.yMin-InkBounds(c.Status,c.Rect).yMax<2)
                LabelIssues.Add("Mission profile overlaps release status: "+context);
        }
        private static void Dump(MissionAtlasScreenView view)
        {
            var canvas=view.GetComponentInParent<Canvas>().rootCanvas;
            Debug.Log("[ApprovedMissionAtlasReview] canvas="+((RectTransform)canvas.transform).rect+" scale="+canvas.transform.lossyScale+" host="+view.transform.lossyScale+" frame="+view.transform.Find("AtlasFrame").lossyScale+" rect="+((RectTransform)view.transform.Find("AtlasFrame")).rect);
        }
        private static async Task Open(UIRoute route) {UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute,route,false);await Route(route);}
        private static async Task Route(UIRoute route)=>await Until(()=>UiShellRuntimeGateway.TryReadShellState(out var s)&&s.ActiveRoute==route&&s.CurrentMode==UiShellMode.MainMenu&&!s.IsTransitionRunning,90);
        private static async Task Until(Func<bool> f,double seconds) {double end=EditorApplication.timeSinceStartup+seconds;while(!f()){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("Approved map-menu state timed out");await Task.Delay(100);}await Task.Delay(450);}
        private static Vector2 Center(RectTransform r) {var c=r.GetComponentInParent<Canvas>().rootCanvas;return RectTransformUtility.WorldToScreenPoint(c.renderMode==RenderMode.ScreenSpaceOverlay?null:c.worldCamera,r.TransformPoint(r.rect.center));}
        private static bool Presented(Button b)
        {
            if(!b.isActiveAndEnabled||!b.IsInteractable())return false;
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=Center(b.transform as RectTransform)},hits);
            return hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b;
        }
        private static async Task Tap(AriaTouchInputUiSystemHelper touch,Button button)
        {
            await Until(()=>button!=null&&button.IsActive()&&button.IsInteractable(),15);Vector2 point=Center(button.transform as RectTransform);
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new InvalidOperationException("Obstructed native target "+button.name+" hit="+(hits.Count>0?hits[0].gameObject.name:"none"));
            string targetName=button.name;
            if(!touch.TryGesture(point,point,.18f,0,Time.unscaledTime))throw new InvalidOperationException("Touch rejected");
            double end=EditorApplication.timeSinceStartup+10;do{touch.Tick(Time.unscaledTime);await Task.Delay(50);if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("Touch timeout");}while(touch.IsBusy);
            await Task.Delay(700);Debug.Log("[ApprovedMissionAtlasReview] touch="+targetName);
        }
        private static async Task Shot(string output,string locale,string name) {Canvas.ForceUpdateCanvases();string path=output+"/"+name+"-"+locale+".png";ScreenCapture.CaptureScreenshot(path);await Until(()=>File.Exists(path)&&new FileInfo(path).Length>0,15);}
    }
}
