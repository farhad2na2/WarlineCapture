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
    public static class InnerScreenUiMissionReview
    {
        public static async Task<int> Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Requires idle Editor");
            string output="Design/AgentReports/InnerScreenUiAudit/After/input-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(output);
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
                string language="fa-IR";
                GameLocalization.SetLocale(language,false);await Task.Delay(1200);
                await Tap(touch,Button("ContinueButton"));await Route(UIRoute.Campaign);
                await Tap(touch,UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>().ChapterOneButton);
                await Tap(touch,Button("LaunchMissionButton"));await Route(UIRoute.MissionBriefing);Shot(output,language,"briefing-m01");await Scroll(touch,UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).First(v=>v.name=="ApprovedMissionIntel"));Shot(output,language,"briefing-goals-m01");
                if(!UiShellRuntimeGateway.TryReadMissionBriefing(out var selected)||selected.MissionId!="saga.ch01.m01.first_contact")throw new InvalidOperationException("M01 not selected");
                var replay=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>().ReplayTutorialToggle;
                if(replay!=null&&replay.IsActive()&&!replay.isOn) {var point=Center((RectTransform)replay.transform);await Gesture(touch,point,point,0);}
                await Tap(touch,Button("LoadoutButton"));await Route(UIRoute.LoadoutSquadPrep);Shot(output,language,"preparation-m01");
                await Tap(touch,Button("DeployButton"));
                await Until(()=>UiShellRuntimeGateway.TryReadShellState(out var shell)&&shell.CurrentMode==UiShellMode.MatchHud&&!shell.IsTransitionRunning,150);
                double deadline=EditorApplication.timeSinceStartup+480,lastLog=0;int actions=0;bool started=false;
                while(!UiShellRuntimeGateway.TryReadMissionResult(out var result))
                {
                    if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Normal M01 journey timed out");
                    var aria=UiShellRuntimeGateway.ReadAriaPlay();actions=Math.Max(actions,aria.Actions);
                    if(aria.Active){if(!started){started=true;Shot(output,language,"aria-running");}touch?.Dispose();touch=null;}
                    else if(!started)
                    {
                        var narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
                        if(narrative!=null&&GroupVisible(narrative,"rootGroup"))
                        {
                            var confirm=narrative.SkipConfirmationView;bool confirming=confirm!=null&&GroupVisible(confirm,"group");
                            object owner=confirming?(object)confirm:narrative.PlaybackControlsView;
                            var skip=FieldButton(owner,confirming?"confirmButton":"skipButton");
                            if(skip!=null&&skip.IsActive()&&skip.IsInteractable())await Tap(touch,skip);
                        }
                        else
                        {
                            var buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
                            var play=buttons.FirstOrDefault(b=>b.name=="ConfirmWatchAria"&&b.IsActive()&&b.IsInteractable()&&Visible(b.transform))??buttons.FirstOrDefault(b=>b.name=="WatchAriaPlay"&&b.IsActive()&&b.IsInteractable()&&Visible(b.transform));
                            if(play!=null)await Tap(touch,play);
                        }
                    }
                    if(EditorApplication.timeSinceStartup-lastLog>15){lastLog=EditorApplication.timeSinceStartup;Debug.Log("[InnerScreenUiMission] aria="+aria.Phase+" actions="+actions);}
                    await Task.Delay(150);
                }
                if(!started||actions==0)throw new InvalidOperationException("ARIA never performed player-input actions");
                UiShellRuntimeGateway.TryReadMissionResult(out var outcome);
                Shot(output,language,"mission-result");await Task.Delay(1500);
                if(outcome.Outcome!=UiMissionResultOutcome.Victory||outcome.SettlementFailed)throw new InvalidOperationException("Normal mission outcome="+outcome.Outcome+" settlementFailed="+outcome.SettlementFailed);
                touch=new AriaTouchInputUiSystemHelper();if(!touch.Start())throw new InvalidOperationException("Return touch unavailable");
                await Tap(touch,FieldButton(UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>(),"primaryButton"));await Route(UIRoute.MainMenu);Shot(output,language,"return-main");await Task.Delay(1000);
                passed=true;
                Debug.Log("[InnerScreenUiMission] result=Passed input=InputSystemTouch locale=fa-IR mission=M01 deploy=True aria=True result=Victory return=MainMenu injectedOutcome=False deviceAcceptance=Pending output="+output);
            }
            catch(Exception error){Debug.LogException(error);Debug.LogError("[InnerScreenUiMission] result=Failed output="+output);}
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
        private static Button FieldButton(object owner,string name)=>owner?.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.GetValue(owner) as Button;
        private static bool GroupVisible(object owner,string name)=>owner?.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.GetValue(owner) is CanvasGroup group&&group.alpha>.9f&&group.gameObject.activeInHierarchy;
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
            string targetName=button.name;await Gesture(touch,point,point,0);Debug.Log("[InnerScreenUiMission] touch="+targetName);
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
