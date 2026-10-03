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
    public static class MenuUiApprovedInputReview
    {
        public static async Task<int> Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Requires idle Editor");
            string output="Design/AgentReports/MenuUiUxAudit/After/input-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(output);
            string saveRoot=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"),locale=GameLocalization.CurrentLocaleCode;
            string background=Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION");
            bool optionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;var options=EditorSettings.enterPlayModeOptions;
            var oldInput=InputSystem.settings;InputSettings fixture=null;AriaTouchInputUiSystemHelper touch=null;
            bool passed=false;
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
                touch=new AriaTouchInputUiSystemHelper();if(!touch.Start())throw new InvalidOperationException("Touch device unavailable");
                foreach(string language in new[]{"en","fa-IR"})
                {
                    GameLocalization.SetLocale(language,false);await Task.Delay(1200);
                    Columns();
                    await Tap(touch,Button("ContinueButton"));await Route(UIRoute.Campaign);Shot(output,language,"campaign");
                    var scroll=UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).First(s=>s.name=="MissionBriefing");
                    await Scroll(touch,scroll);Shot(output,language,"campaign-goals");
                    await Tap(touch,Button("MissionBackButton"));await Route(UIRoute.MainMenu);
                    await Tap(touch,Card("Card_Skirmish"));await Route(UIRoute.QuickCustomSetup);Bounds("MapPreviewClip");Shot(output,language,"skirmish");
                    await Tap(touch,Button("RandomizeSeedButton"));await Tap(touch,Button("ResetButton"));
                    await Tap(touch,Button("BackButton"));await Route(UIRoute.MainMenu);
                    await Tap(touch,Card("Card_Operations"));await Route(UIRoute.Operations);Shot(output,language,"operations");
                    await Tap(touch,Button("BackButton"));await Route(UIRoute.MainMenu);
                    await Tap(touch,Button("CommanderPanelHotspot"));await Route(UIRoute.CommanderProfile);
                    if(!UiShellRuntimeGateway.TryReadCommanderProfile(out var facts)||facts.Level!=3||facts.Xp!=120||facts.Victories!=2||facts.Defeats!=1||facts.Missions!=3||facts.Stars!=7||facts.Enemies!=42||facts.UnitsLost!=4)throw new InvalidOperationException("Commander did not project saved profile facts");
                    Shot(output,language,"commander");
                    await Tap(touch,Button("ChangeCommanderButton"));
                    await Tap(touch,Button(language=="en"?"Portrait1":"Portrait0"));
                    int selected=language=="en"?1:0;
                    await Until(()=>saves.LoadProfile().firstLaunchCommanderPortraitIndex==selected,15);
                    await Tap(touch,Button("BackButton"));await Route(UIRoute.MainMenu);Columns();
                    var portrait=UnityEngine.Object.FindObjectsByType<MainMenuCommanderVariantView>(FindObjectsSortMode.None).First(v=>v.isActiveAndEnabled);
                    if(portrait.Target.sprite!=portrait.Variants[selected].Sprite)throw new InvalidOperationException("Home portrait did not follow saved choice");
                    Shot(output,language,"home-return");await Task.Delay(700);
                    var after=saves.LoadProfile();if(after.credits!=credits||after.missionsCompleted!=missions)throw new InvalidOperationException("Menu navigation changed mission rewards");
                }
                passed=true;
                Debug.Log("[MenuUiApprovedInput] result=Passed input=InputSystemTouch routeActions=Buttons locales=en,fa-IR screens=5 campaignScroll=True portraitSaveAndReturn=True columns=Aligned savedProfileFacts=True rewardsUnchanged=True completeMission=NotClaimed deviceAcceptance=Pending output="+output);
            }
            catch(Exception error){Debug.LogException(error);Debug.LogError("[MenuUiApprovedInput] result=Failed output="+output);}
            finally
            {
                touch?.Dispose();
                if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.ExitPlaymode();await Until(()=>!EditorApplication.isPlayingOrWillChangePlaymode,60);}
                InputSystem.settings=oldInput;if(fixture!=null)UnityEngine.Object.DestroyImmediate(fixture);
                EditorSettings.enterPlayModeOptions=options;EditorSettings.enterPlayModeOptionsEnabled=optionsEnabled;
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",saveRoot);Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",background);
                GameLocalization.SetLocale(locale,false);
            }
            return passed?0:1;
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
            string targetName=button.name;await Gesture(touch,point,point,0);Debug.Log("[MenuUiApprovedInput] touch="+targetName);
        }
        private static async Task Scroll(AriaTouchInputUiSystemHelper touch,ScrollRect scroll)
        {Vector2 center=Center(scroll.viewport);await Gesture(touch,center-new Vector2(0,170),center+new Vector2(0,170),.6f);if(scroll.verticalNormalizedPosition>.95f)throw new InvalidOperationException("Briefing did not scroll via touch");}
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
