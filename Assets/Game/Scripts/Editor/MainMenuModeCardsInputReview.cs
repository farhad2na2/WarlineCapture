using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Configs;
using Game.Runtime;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Game.Editor
{
    public static class MainMenuModeCardsInputReview
    {
        private static string output;private static int taps;
        public static async void Run()
        {
            bool passed=false;
            string oldRoot=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            string oldBackground=Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION");
            string locale=GameLocalization.CurrentLocaleCode;
            bool oldEnabled=EditorSettings.enterPlayModeOptionsEnabled;
            var oldOptions=EditorSettings.enterPlayModeOptions;
            var oldInput=InputSystem.settings;
            InputSettings fixture=null;AriaTouchInputUiSystemHelper touch=null;bool suspended=false;
            try
            {
                await Task.Delay(300);
                await Until(()=>!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode,90);
                MainMenuModeCardsValidationDependencies.Prepare();
                output=MainMenuModeCardsValidation.Root+"/Live";Directory.CreateDirectory(output);
                taps=0;
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",Path.Combine(Path.GetTempPath(),"warline-home-next-input-"+Guid.NewGuid().ToString("N")));
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","1");
                MenuUiAuditCapture.PrepareSave();
                var saves=SaveService.CreateDefault();var profile=saves.LoadProfile();
                profile.skirmishMedals=new[]{new SkirmishMedalSaveData{catalogId="S001",difficulty=2,sessionId="home-input-fixture"}};
                saves.SaveProfile(profile);
                EditorSettings.enterPlayModeOptionsEnabled=true;
                EditorSettings.enterPlayModeOptions=oldOptions|EnterPlayModeOptions.DisableDomainReload;
                MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);
                EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
                AssetDatabase.DisallowAutoRefresh();suspended=true;
                EditorApplication.EnterPlaymode();await Until(()=>EditorApplication.isPlaying,90);
                fixture=UnityEngine.Object.Instantiate(oldInput);fixture.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                fixture.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings=fixture;Application.runInBackground=true;
                await Route(UIRoute.MainMenu);
                touch=new AriaTouchInputUiSystemHelper();
                // Respect physical input: wait for released controls before starting a touch review.
                await Until(()=>touch.Start(),30);
                for(int phase=0;phase<2;phase++)
                {
                    if(phase==1)
                    {
                        await Tap(touch,Card(MainMenuMode.Operations).Action);await Route(UIRoute.Operations);
                        var store=new CampaignMissionProgressStore(saves);
                        for(int i=0;i<CampaignMissionSequence.RegisteredMissionCount;i++)
                        {
                            string id=CampaignMissionSequence.IdAt(i);store.EnsureAvailable(id);
                            store.Settle(id,"home-complete-fixture-"+i,i,true,3,60000,null);
                        }
                        await ReturnHome(touch);
                    }
                    foreach(string language in new[]{"en","fa-IR"})
                    {
                        GameLocalization.SetLocale(language,false);await Task.Delay(1200);
                        var home=UnityEngine.Object.FindAnyObjectByType<MainMenuModeCardsView>();
                        await Until(()=>home.Expanded==(phase==1),20);
                        AssertNoOutcome();
                        string before=JsonUtility.ToJson(saves.LoadProfile());
                        string expected=Card(MainMenuMode.Skirmish).State.MissionId;
                        if(string.IsNullOrEmpty(expected)||Card(MainMenuMode.Skirmish).PreviewTexture==null)throw new InvalidOperationException("Home next thumbnail missing");
                        ScreenCapture.CaptureScreenshot(output+"/"+(phase==0?"A-campaign":"C-complete")+"-"+language+".png");
                        await Task.Delay(350);
                        await Tap(touch,Card(MainMenuMode.Skirmish).Action);await Route(UIRoute.QuickCustomSetup);
                        var quick=UnityEngine.Object.FindAnyObjectByType<QuickCustomScreenView>();
                        Debug.Log("[MainMenuNextMissionSelection] expected="+expected+" selected="+quick.SelectedScenarioId+" briefing="+quick.Library?.Briefing.ScenarioId+" queued="+SkirmishLibraryReturn.PendingScenarioId);
                        await Until(()=>quick.Library!=null&&quick.Library.Briefing.ScenarioId==expected,20);
                        var launch=quick.ReadConfigFromControls();
                        if(launch.SkirmishSelection.CatalogId!=expected)
                            throw new InvalidOperationException("Displayed mission and actual launch identity differ: "+expected+" / "+launch.SkirmishSelection.CatalogId);
                        ScreenCapture.CaptureScreenshot(output+"/next-skirmish-"+phase+"-"+language+".png");
                        await ReturnHome(touch);
                        await Tap(touch,Card(MainMenuMode.Operations).Action);await Route(UIRoute.Operations);
                        ScreenCapture.CaptureScreenshot(output+"/operations-"+phase+"-"+language+".png");
                        await ReturnHome(touch);
                        await Tap(touch,Button("ContinueButton"));await Route(UIRoute.Campaign);
                        await ReturnHome(touch);
                        if(JsonUtility.ToJson(saves.LoadProfile())!=before)throw new InvalidOperationException("Menu navigation mutated profile/rewards/AP/medals");
                        AssertNoOutcome();
                    }
                }
                Debug.Log("[MainMenuModeCardsInput] result=Passed input=InputSystemTouch taps="+taps+" states=A,C locales=en,fa-IR nextScenarioMatches=true actualThumbnails=true campaignReturn=true operationsReturn=true profileUnchanged=true fixtureSaves=true completeMission=NotClaimed deviceAcceptance=Pending output="+output);
                passed=true;
            }
            catch(Exception error)
            {
                if(!string.IsNullOrEmpty(output))
                {
                    ScreenCapture.CaptureScreenshot(output+"/input-failure.png");
                    if(UiShellRuntimeGateway.TryReadShellState(out var failedShell))
                        File.WriteAllText(output+"/input-failure.txt","route="+failedShell.ActiveRoute+" mode="+failedShell.CurrentMode+" transition="+failedShell.IsTransitionRunning+" taps="+taps);
                }
                Debug.LogException(error);Debug.LogError("[MainMenuModeCardsInput] result=Failed output="+output);
            }
            finally
            {
                touch?.Dispose();
                if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.ExitPlaymode();await Until(()=>!EditorApplication.isPlayingOrWillChangePlaymode,90);}
                InputSystem.settings=oldInput;if(fixture!=null)UnityEngine.Object.DestroyImmediate(fixture);
                if(suspended)AssetDatabase.AllowAutoRefresh();
                EditorSettings.enterPlayModeOptionsEnabled=oldEnabled;EditorSettings.enterPlayModeOptions=oldOptions;
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",oldRoot);
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",oldBackground);
                GameLocalization.SetLocale(locale,false);
                MissionEditorValidationExit.Complete(passed);
            }
        }
        private static MainMenuModeCardView Card(MainMenuMode mode)=>UnityEngine.Object.FindObjectsByType<MainMenuModeCardView>(FindObjectsSortMode.None).First(v=>v.Mode==mode&&v.isActiveAndEnabled);
        private static Button Button(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name==name&&b.isActiveAndEnabled&&b.GetComponentsInParent<CanvasGroup>().All(g=>g.alpha>.05f));
        private static void AssertNoOutcome()
        {
            if(UiShellRuntimeGateway.ReadAriaPlay().Active)throw new InvalidOperationException("Unexpected ARIA activation during menu-only review");
            if(UiShellRuntimeGateway.TryReadShellState(out var s)&&s.CurrentMode!=UiShellMode.MainMenu)throw new InvalidOperationException("Unexpected mission deployment");
        }
        private static async Task ReturnHome(AriaTouchInputUiSystemHelper touch)
        {
            for(int i=0;i<3;i++)
            {
                if(UiShellRuntimeGateway.TryReadShellState(out var s)&&s.ActiveRoute==UIRoute.MainMenu&&!s.IsTransitionRunning)return;
                await Tap(touch,Button("BackButton"));await Task.Delay(900);
            }
            await Route(UIRoute.MainMenu);
        }
        private static async Task Route(UIRoute route)
        {await Until(()=>UiShellRuntimeGateway.TryReadShellState(out var s)&&s.ActiveRoute==route&&s.CurrentMode==UiShellMode.MainMenu&&!s.IsTransitionRunning,90);await Task.Delay(700);}
        private static async Task Until(Func<bool> condition,double seconds)
        {double end=EditorApplication.timeSinceStartup+seconds;while(!condition()){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("Menu input state timed out");await Task.Delay(100);}}
        private static async Task Tap(AriaTouchInputUiSystemHelper touch,Button button)
        {
            await Until(()=>button!=null&&button.IsActive()&&button.IsInteractable(),15);
            string targetName=button.name;
            var rect=(RectTransform)button.transform;var canvas=rect.GetComponentInParent<Canvas>().rootCanvas;
            var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new InvalidOperationException("Obstructed native target: "+button.name);
            if(!touch.TryGesture(point,point,.18f,0,Time.unscaledTime))throw new InvalidOperationException("Touch rejected");
            double end=EditorApplication.timeSinceStartup+10;
            do{touch.Tick(Time.unscaledTime);await Task.Delay(50);if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("Touch did not complete");}while(touch.IsBusy);
            taps++;Debug.Log("[MainMenuModeTouch] target="+targetName+" count="+taps);await Task.Delay(500);
        }
    }
}
