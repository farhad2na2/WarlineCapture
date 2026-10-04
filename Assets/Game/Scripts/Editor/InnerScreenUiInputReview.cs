using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Game.Configs;
using Game.Composition;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Entities;
using Game.UI.Shell.Contracts.Ecs;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class InnerScreenUiInputReview
    {
        public static Task<int> Run()=>RunCore(false);
        public static Task<int> RunComic()=>RunCore(true);
        private static async Task<int> RunCore(bool comicOnly)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Requires idle Editor");
            string output="Design/AgentReports/InnerScreenUiAudit/After/input-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(output);
            string saveRoot=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"),locale=GameLocalization.CurrentLocaleCode;
            string background=Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION");
            bool optionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;var options=EditorSettings.enterPlayModeOptions;
            var oldInput=InputSystem.settings;InputSettings fixture=null;AriaTouchInputUiSystemHelper touch=null;
            bool passed=false;Keyboard keyboard=null;
            try
            {
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",Path.Combine(Path.GetTempPath(),"warline-menu-touch-"+Guid.NewGuid().ToString("N")));
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","1");
                MenuUiAuditCapture.PrepareSave();
                var saves=SaveService.CreateDefault();var before=saves.LoadProfile();before.commanderLevel=3;before.commanderXp=120;before.victories=2;before.defeats=1;before.missionsCompleted=3;before.starsEarned=7;before.enemiesDefeated=42;before.unitsLost=4;before.credits=1250;saves.SaveProfile(before);
                int credits=before.credits,missions=before.missionsCompleted;
                EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=options|EnterPlayModeOptions.DisableDomainReload;
                MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
                EditorApplication.EnterPlaymode();await Until(()=>EditorApplication.isPlaying,90);
                fixture=UnityEngine.Object.Instantiate(oldInput);fixture.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                fixture.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings=fixture;Application.runInBackground=true;
                await Route(UIRoute.MainMenu);
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                keyboard=InputSystem.AddDevice<Keyboard>();
                touch=new AriaTouchInputUiSystemHelper();if(!touch.Start())throw new InvalidOperationException("Touch device unavailable");
                if(!comicOnly)foreach(string language in new[]{"en","fa-IR"})
                {
                    GameLocalization.SetLocale(language,false);await Task.Delay(1200);
                    await Tap(touch,Button("ContinueButton"));await Route(UIRoute.Campaign);
                    var chapters=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();
                    await Tap(touch,chapters.ChapterOneButton);await Tap(touch,chapters.ChapterTwoButton);await Tap(touch,chapters.ChapterThreeButton);
                    await DismissNarrative(touch);
                    await Tap(touch,chapters.MissionNodeButtons[2]);
                    await DismissNarrative(touch);
                    await Tap(touch,Button("LaunchMissionButton"));await Route(UIRoute.MissionBriefing);Shot(output,language,"briefing");
                    await Tap(touch,Button("SettingsButton"));await Until(()=>UnityEngine.Object.FindAnyObjectByType<SettingsPopupView>()!=null,20);
                    await SystemBack(keyboard);await Until(()=>UnityEngine.Object.FindAnyObjectByType<SettingsPopupView>()==null,20);
                    if(!UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.name=="LoadoutButton"&&b.IsActive()))throw new InvalidOperationException("Settings close lost deployed inner screen");
                    await Scroll(touch,UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).First(v=>v.name=="ApprovedMissionIntel"));Shot(output,language,"briefing-goals");
                    await Tap(touch,Button("LoadoutButton"));await Route(UIRoute.LoadoutSquadPrep);Shot(output,language,"preparation");
                    if(!UiShellRuntimeGateway.TryReadMissionBriefing(out var selected)||!selected.IsValid)throw new InvalidOperationException("Preparation lost selected mission");
                    var selectedTitle=UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).First(t=>t.name=="SelectedMissionTitle");
                    string expectedTitle=GameLocalization.Get(selected.DisplayNameKey);
                    if(GameLocalization.TryGetSourceByLocalized(expectedTitle,out _,out string authoredTitle))expectedTitle=authoredTitle;
                    if(selectedTitle.GetComponent<V3LocalizedTextBindingView>().SourceValue!=expectedTitle)throw new InvalidOperationException("Preparation title mismatch");
                    await Tap(touch,Button("EditLoadoutButton"));await Route(UIRoute.Armory);
                    await Tap(touch,Button("VehiclesTab"));await Tap(touch,Button("UnitsTab"));Shot(output,language,"armory");
                    await Scroll(touch,UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).First(v=>v.name=="InspectionPanel"));Shot(output,language,"armory-stats");
                    await SystemBack(keyboard);await Route(UIRoute.LoadoutSquadPrep);
                    await SystemBack(keyboard);await Route(UIRoute.MissionBriefing);
                    await SystemBack(keyboard);await Route(UIRoute.Campaign);
                    await SystemBack(keyboard);await Route(UIRoute.MainMenu);
                    await Tap(touch,Card("StoreButton"));await Route(UIRoute.CommandExchange);
                    await Tap(touch,Button("Category_1"));await Tap(touch,Button("OfferSlot_1"));Shot(output,language,"store");
                    await Scroll(touch,UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).First(v=>v.name=="DetailPanel"));Shot(output,language,"store-details");
                    if(UnityEngine.Object.FindAnyObjectByType<StoreCommandExchangeV3View>().PurchaseButton.interactable)throw new InvalidOperationException("Purchase unexpectedly enabled");
                    await SystemBack(keyboard);await Route(UIRoute.MainMenu);
                    await Tap(touch,Card("Card_Operations"));await Route(UIRoute.Operations);
                    await Tap(touch,Button("Patrol"));await Route(UIRoute.DistrictDetail);Shot(output,language,"district");
                    await SystemBack(keyboard);await Route(UIRoute.Operations);
                    await Tap(touch,Button("IntelReport"));await Route(UIRoute.CommandFeed);
                    await Tap(touch,Button("AriaFilter"));await Tap(touch,Button("AllFilter"));
                    await Scroll(touch,UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).First(v=>v.name=="FeedViewport"));Shot(output,language,"feed");
                    await SystemBack(keyboard);await Route(UIRoute.Operations);
                    await SystemBack(keyboard);await Route(UIRoute.MainMenu);
                    await Tap(touch,Card("Card_Skirmish"));await Route(UIRoute.QuickCustomSetup);
                    await SystemBack(keyboard);await Route(UIRoute.MainMenu);
                    await Tap(touch,Button("CommanderPanelHotspot"));await Route(UIRoute.CommanderProfile);
                    await SystemBack(keyboard);await Route(UIRoute.MainMenu);
                    if(!UnityEngine.Object.FindAnyObjectByType<UISystemBackView>().CanLeaveFromRoot())throw new InvalidOperationException("Root Back did not delegate to Android");
                    await Task.Delay(700);
                    var after=saves.LoadProfile();if(after.credits!=credits||after.missionsCompleted!=missions)throw new InvalidOperationException("Menu navigation changed mission rewards");
                }
                GameLocalization.SetLocale("fa-IR",false);await Task.Delay(900);
                var progress=new CampaignMissionProgressStore(saves);
                for(int i=0;i<CampaignMissionSequence.RegisteredMissionCount;i++){progress.EnsureAvailable(CampaignMissionSequence.IdAt(i));progress.Settle(CampaignMissionSequence.IdAt(i),"archive-audit-"+i,i,true,3,60000,null);}
                await Task.Delay(2000);
                await Tap(touch,Button("SettingsButton"));await Until(()=>UnityEngine.Object.FindAnyObjectByType<SettingsPopupView>()!=null,20);Shot(output,"fa-IR","settings");
                await SystemBack(keyboard);await Task.Delay(1000);
                var archive=UnityEngine.Object.FindAnyObjectByType<MainMenuStoryArchiveView>();
                await Tap(touch,Button("StoryArchiveButton"));await Until(()=>archive.IsOpen,20);Shot(output,"fa-IR","story-archive");
                await Tap(touch,Button("Story0brief"));await Until(()=>archive.IsPlaying,20);await Task.Delay(2000);
                if(!UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None).Any(i=>i.name=="StoryArchiveBackdrop"&&i.gameObject.activeInHierarchy&&i.color.a==1))throw new InvalidOperationException("Archive backdrop missing");
                await ComicShot(output,"fa-IR","story-playback",true);
                await SystemBack(keyboard);await Until(()=>!archive.IsPlaying,20);
                GameLocalization.SetLocale("en",false);await Task.Delay(900);
                await Tap(touch,Button("Story0brief"));await Until(()=>archive.IsPlaying,20);await Task.Delay(1200);
                await ComicShot(output,"en","story-playback",false);
                await SystemBack(keyboard);await Until(()=>!archive.IsPlaying,20);
                GameLocalization.SetLocale("fa-IR",false);await Task.Delay(900);
                await Tap(touch,Button("Story0brief"));await Until(()=>archive.IsPlaying,20);await Task.Delay(1200);
                MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);await Task.Delay(1000);await ComicShot(output,"fa-IR","story-playback-phone",true);
                MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1200);await Task.Delay(1000);await ComicShot(output,"fa-IR","story-playback-tablet",true);
                MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);await Task.Delay(1000);
                var narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
                var skip=narrative.PlaybackControlsView.GetType().GetField("skipButton",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.GetValue(narrative.PlaybackControlsView) as Button;
                await Tap(touch,skip);await Until(()=>!archive.IsPlaying,20);await Task.Delay(1000);await SystemBack(keyboard);await Until(()=>!archive.IsOpen,20);
                await Task.Delay(700);Shot(output,"fa-IR","home-after-story");
                if(UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>().IsVisible)throw new InvalidOperationException("Narrative leaked onto home after archive close");
                await PauseBackFixture(keyboard,output);
                await Until(()=>Directory.GetFiles(output,"*.png").Length==(comicOnly?10:28)&&Directory.GetFiles(output,"*.png").All(f=>new FileInfo(f).Length>0),20);
                passed=true;
                Debug.Log("[InnerScreenUiInput] result=Passed input=InputSystemTouch locales=en,fa-IR "+(comicOnly?"scope=ComicAndPause":"screens=6 systemBackRoutes=10 innerScrolling=True selectedMission=True storeDisabled=True armoryCategories=True rewardsUnchanged=True")+" comicRtl=True completeMission=NotClaimed deviceAcceptance=Pending output="+output);
            }
            catch(Exception error){Debug.LogException(error);Debug.LogError("[InnerScreenUiInput] result=Failed output="+output);}
            finally
            {
                touch?.Dispose();if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
                if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.ExitPlaymode();await Until(()=>!EditorApplication.isPlayingOrWillChangePlaymode,60);}
                InputSystem.settings=oldInput;if(fixture!=null)UnityEngine.Object.DestroyImmediate(fixture);
                EditorSettings.enterPlayModeOptions=options;EditorSettings.enterPlayModeOptionsEnabled=optionsEnabled;
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",saveRoot);Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",background);
                GameLocalization.SetLocale(locale,false);
            }
            return passed?0:1;
        }
        private static async Task SystemBack(Keyboard keyboard)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));await Task.Delay(150);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(700);
            Debug.Log("[InnerScreenUiInput] systemBack=Escape");
        }
        private static async Task ComicShot(string output,string locale,string screen,bool rtl)
        {
            ComicDirection(rtl);Bounds("NextPanel");Bounds("Portrait");
            var icon=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>().DialogueView.transform.Find("Pointer/SharedAdvanceIcon") as RectTransform;
            var corners=new Vector3[4];icon.GetWorldCorners(corners);var canvas=icon.GetComponentInParent<Canvas>().rootCanvas;
            foreach(var c in corners){var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,c);if(point.x<0||point.x>Screen.width)throw new InvalidOperationException("Comic advance icon off-screen");}
            Shot(output,locale,screen);string path=output+"/"+screen+"-"+locale+".png";
            await Until(()=>File.Exists(path)&&new FileInfo(path).Length>0,10);await Task.Delay(300);
        }
        private static void ComicDirection(bool rtl)
        {
            var dialogue=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>().DialogueView.transform;
            var portrait=dialogue.Find("Portrait") as RectTransform;var next=dialogue.Find("NextPanel") as RectTransform;
            if(portrait==null||next==null||((Center(portrait).x>Center(next).x)!=rtl))throw new InvalidOperationException("Comic portrait/Next reading order mismatch");
        }
        private static async Task PauseBackFixture(Keyboard keyboard,string output)
        {
            // Isolated shell-state fixture: verifies Back dispatch and native Pause, not mission readiness.
            var em=World.DefaultGameObjectInjectionWorld.EntityManager;
            using var query=em.CreateEntityQuery(typeof(UiShellStateComponent),typeof(UiShellActivePopupComponent));
            var root=query.GetSingletonEntity();var original=em.GetComponentData<UiShellStateComponent>(root);
            var state=original;state.CurrentMode=UiShellMode.MatchHud;state.ActiveRoute=UIRoute.Match;state.IsTransitionRunning=0;state.Phase=UiShellTransitionPhase.MatchHudReady;em.SetComponentData(root,state);
            string name=UnityEngine.Object.FindAnyObjectByType<UIShellContentView>().PauseMenuPopupPrefab.name;
            await SystemBack(keyboard);await Until(()=>GameObject.Find(name)!=null,20);Shot(output,"fa-IR","pause-back-fixture");
            await SystemBack(keyboard);await Until(()=>GameObject.Find(name)==null,20);
            if(!UiShellRuntimeGateway.TryReadShellState(out var resumed)||resumed.CurrentMode!=UiShellMode.MatchHud||resumed.ActiveRoute!=UIRoute.Match)throw new InvalidOperationException("Pause Back left match");
            UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.OpenSettings);await Until(()=>UnityEngine.Object.FindAnyObjectByType<SettingsPopupView>()!=null,20);await Task.Delay(1000);Shot(output,"fa-IR","match-settings-back-fixture");
            await SystemBack(keyboard);await Until(()=>UnityEngine.Object.FindAnyObjectByType<SettingsPopupView>()==null,20);
            if(em.GetComponentData<UiShellActivePopupComponent>(root).Visible!=0)throw new InvalidOperationException("Back left hidden modal state active");
            UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.ReturnToMainMenu,UIRoute.MainMenu,false);
            await Route(UIRoute.MainMenu);
            Shot(output,"fa-IR","home-after-match-fixture");await Task.Delay(500);
            var gameViewType=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var gameView=EditorWindow.GetWindow(gameViewType);
            var gizmos=gameViewType.GetProperty("drawGizmos",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if((bool)gizmos.GetValue(gameView))throw new InvalidOperationException("Editor gizmos contaminated player review");
            Debug.Log("[MenuReturnReview] result=Passed route=MainMenu editorGizmos=False realMission=NotClaimed");
            Debug.Log("[SystemBackFixture] result=Passed pauseOpens=True pauseBackResumes=True settingsDismisses=True realMission=NotClaimed");
        }
        private static async Task DismissNarrative(AriaTouchInputUiSystemHelper touch)
        {
            await Task.Delay(1200);
            var view=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
            if(view==null||!view.gameObject.activeInHierarchy)return;
            var control=view.PlaybackControlsView;
            var skip=control?.GetType().GetField("skipButton",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.GetValue(control) as Button;
            if(skip!=null&&skip.IsActive()&&skip.IsInteractable()) { await Tap(touch,skip); await Task.Delay(1200); }
        }
        private static Button Button(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name==name&&b.isActiveAndEnabled&&Visible(b.transform));
        private static bool Visible(Transform target)=>target.gameObject.activeInHierarchy&&target.GetComponentsInParent<CanvasGroup>().All(g=>g.alpha>.05f);
        private static Button Card(string name)=>UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).First(r=>r.name==name&&Visible(r)).GetComponentInChildren<Button>();
        private static async Task Route(UIRoute route)
        {await Until(()=>UiShellRuntimeGateway.TryReadShellState(out var s)&&s.ActiveRoute==route&&s.CurrentMode==UiShellMode.MainMenu&&!s.IsTransitionRunning,90);await Task.Delay(700);}
        private static async Task Until(Func<bool> condition,double seconds)
        {double end=EditorApplication.timeSinceStartup+seconds;while(!condition()){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("Menu input state timed out");await Task.Delay(100);}}
        private static Vector2 Center(RectTransform rect)
        {var c=rect.GetComponentInParent<Canvas>().rootCanvas;return RectTransformUtility.WorldToScreenPoint(c.renderMode==RenderMode.ScreenSpaceOverlay?null:c.worldCamera,rect.TransformPoint(rect.rect.center));}
        private static async Task Tap(AriaTouchInputUiSystemHelper touch,Button button)
        {
            await Until(()=>button!=null&&button.IsActive()&&button.IsInteractable(),15);Vector2 point=Center((RectTransform)button.transform);
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new InvalidOperationException("Obstructed native target: "+button.name+" hit="+(hits.Count>0?hits[0].gameObject.name:"none"));
            string targetName=button.name;await Gesture(touch,point,point,0);Debug.Log("[InnerScreenUiInput] touch="+targetName);
        }
        private static async Task Scroll(AriaTouchInputUiSystemHelper touch,ScrollRect scroll)
        {Vector2 center=Center(scroll.viewport);for(int i=0;i<10&&scroll.verticalNormalizedPosition>.05f;i++)await Gesture(touch,center-new Vector2(0,170),center+new Vector2(0,170),.6f);if(scroll.verticalNormalizedPosition>.05f)throw new InvalidOperationException("Native content did not scroll to the end via touch");}
        private static async Task Gesture(AriaTouchInputUiSystemHelper touch,Vector2 from,Vector2 to,float travel)
        {if(!touch.TryGesture(from,to,.18f,travel,Time.unscaledTime))throw new InvalidOperationException("Touch rejected");double end=EditorApplication.timeSinceStartup+10;do{touch.Tick(Time.unscaledTime);await Task.Delay(50);if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("Touch did not complete");}while(touch.IsBusy);await Task.Delay(500);}
        private static void Shot(string output,string locale,string screen)=>ScreenCapture.CaptureScreenshot(output+"/"+screen+"-"+locale+".png");
        private static void Bounds(string name)
        {
            var rect=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).First(t=>t.name==name&&Visible(t));
            var canvas=rect.GetComponentInParent<Canvas>().rootCanvas;var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            foreach(var c in corners){var p=RectTransformUtility.WorldToScreenPoint(camera,c);if(p.x<0||p.y<0||p.x>Screen.width||p.y>Screen.height)throw new InvalidOperationException("Off-screen native panel: "+name);}
        }
        private static void Columns()
        {
            string[] names={"CreditsVisualPanel","CommanderPanel","AriaPanel","StoreButton","OpenArmoryButton"};float? left=null,right=null;
            foreach(string name in names)
            {
                var rect=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).First(t=>t.name==name);
                var corners=new Vector3[4];rect.GetWorldCorners(corners);
                if(left.HasValue&&(Mathf.Abs(left.Value-corners[0].x)>.5f||Mathf.Abs(right.Value-corners[2].x)>.5f))throw new InvalidOperationException("Column alignment failed: "+name);
                left=corners[0].x;right=corners[2].x;
            }
        }
    }
}
