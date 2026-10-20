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
        public static async Task<int> Run()
        {
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
                    Dump(c);await Shot(output,language,"campaign-chapters");await Matrix(output,language,"campaign-chapters",c);
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
                    if(s.ShowingMissions)await Tap(touch,s.BackButton);await Shot(output,language,"skirmish-maps");await Matrix(output,language,"skirmish-maps",s);
                    await Tap(touch,s.NavigationCards[0].Button);await Tap(touch,s.PrimaryButton);await Until(()=>s.ShowingMissions,15);await Shot(output,language,"skirmish-missions");
                    await Tap(touch,s.Regions[0].Cards[1].Button);await Until(()=>skirmish.LibraryViewModel.Briefing.ScenarioId=="S002",15);await Shot(output,language,"skirmish-s002");
                    if(skirmish.MissionSizeControl==null||!skirmish.MissionSizeControl.gameObject.activeInHierarchy)throw new InvalidOperationException("Real S002 mission-size control is missing");
                    await Matrix(output,language,"skirmish-missions",s);
                    var from=Center(s.MissionScroll.viewport)-new Vector2(0,180);var to=from+new Vector2(0,420);
                    if(!touch.TryGesture(from,to,.15f,.7f,Time.unscaledTime))throw new InvalidOperationException("Map swipe rejected");
                    while(touch.IsBusy){touch.Tick(Time.unscaledTime);await Task.Delay(50);}await Task.Delay(500);
                    if(s.MissionScroll.verticalNormalizedPosition>.97f)throw new InvalidOperationException("Native map did not scroll");
                    await Tap(touch,s.NavigationCards[3].Button);await Until(()=>s.VisibleRegion==3,15);await Shot(output,language,"skirmish-convoy");
                    if(s.Regions[3].Cards.Count(x=>x.Rect.gameObject.activeSelf)!=6)throw new InvalidOperationException("Skirmish objective must show six choices");
                    await Tap(touch,s.BackButton);await Tap(touch,s.BackButton);await Route(UIRoute.MainMenu);
                }
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
                foreach(var c in view.NavigationCards.Concat(view.ShowingMissions?Array.Empty<MissionAtlasScreenView.Card>():view.TerritoryCards))if(c.Rect.gameObject.activeInHierarchy)
                {
                    foreach(var t in new[]{c.Title,c.Subtitle}){t.ForceMeshUpdate();var bounds=t.textBounds;var rect=t.rectTransform.rect;
                        // TMP textBounds include the transparent SDF padding around glyphs.
                        float padding=t.font!=null?t.font.atlasPadding*t.fontSize/Mathf.Max(1,t.font.faceInfo.pointSize):0;
                        float tolerance=4+padding;
                        bool outside=bounds.min.x<rect.xMin-tolerance || bounds.max.x>rect.xMax+tolerance || bounds.min.y<rect.yMin-tolerance || bounds.max.y>rect.yMax+tolerance;
                        if(t.isTextTruncated||outside)throw new InvalidOperationException("Navigation glyph clipping: "+t.text+" at "+size+" glyphs="+bounds+" rect="+rect);}
                }
                Debug.Log("[ApprovedMissionAtlasReview] ratio="+size+" locale="+locale+" navigationText=Passed");
            }
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
