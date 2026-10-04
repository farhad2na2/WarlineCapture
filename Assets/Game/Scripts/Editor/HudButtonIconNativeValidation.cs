using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Native screen and touch-control checks. Does not establish a complete match or device acceptance.</summary>
    public static class HudButtonIconNativeValidation
    {
        private const string Output="Design/AgentReports/AriaHudButtonIcons/Native";
        public static Task<int> RunSkirmishScreens() => RunScreens(false);
        public static Task<int> RunIconAlignmentScreens() => RunScreens(true);
        public static Task<int> RunPlacementProbe() => RunScreens(true, SkirmishBuildingPlacementProbe.Inspect);
        private static async Task<int> RunScreens(bool alignmentOnly, Action inspect = null)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Requires Edit mode");
            Directory.CreateDirectory(Output);
            bool optionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;
            var options=EditorSettings.enterPlayModeOptions;
            var inputSettings=InputSystem.settings;
            InputSettings fixture=null;
            AriaTouchInputUiSystemHelper touch=null;
            string background=Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION");
            string locale=GameLocalization.CurrentLocaleCode;
            bool previousBackground=Application.runInBackground;
            const string scenarioPreference="Warline.Skirmish.LastScenarioId";
            bool hadScenarioPreference=PlayerPrefs.HasKey(scenarioPreference);
            string previousScenario=PlayerPrefs.GetString(scenarioPreference);
            try
            {
                EditorSettings.enterPlayModeOptionsEnabled=true;
                EditorSettings.enterPlayModeOptions=options|EnterPlayModeOptions.DisableDomainReload;
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","1");
                MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
                AriaPlayEditorValidation.Launch(0,"en");
                await Until(()=>EditorApplication.isPlaying,60);
                fixture=UnityEngine.Object.Instantiate(inputSettings);
                fixture.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                fixture.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings=fixture; Application.runInBackground=true;
                await Until(()=>UiShellRuntimeGateway.TryEnqueueRouteRequest(
                    UiShellRouteIntent.OpenMenuRoute,UIRoute.QuickCustomSetup,false),60);
                await Until(()=>UnityEngine.Object.FindAnyObjectByType<QuickCustomScreenView>()?.isActiveAndEnabled==true,60);
                var setup=UnityEngine.Object.FindAnyObjectByType<QuickCustomScreenView>();
                setup.SelectScenario(SkirmishPresetConfig.DesertBaseAirMobileFieldScenarioIndex);
                setup.LaunchMatch();
                await Until(()=>UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>()?.PlayerFocusButton?.IsActive()==true,120);
                inspect?.Invoke();
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                touch=new AriaTouchInputUiSystemHelper();
                if(!touch.Start())throw new InvalidOperationException("Native touch fixture unavailable");
                var tray=UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
                await Tap(touch,tray.VisibleCardButton(0));
                var selection=UnityEngine.Object.FindAnyObjectByType<MatchHudSelectionPanelView>();
                await Until(()=>selection.CommandWheelOpenButton.IsActive(),15);
                foreach(string language in new[]{"en","fa-IR"})
                {
                    GameLocalization.SetLocale(language,false);
                    foreach(int width in new[]{1920,1280})
                    {
                        int height=width==1920?1080:720;
                        MainMenuV3PrefabBuilder.SetGameViewResolution(width,height);
                        await Task.Delay(1800);
                        var match=UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
                        UiShellRuntimeGateway.TryReadMatchHudSelection(out var before);
                        await Tap(touch,match.EnemyFocusButton);
                        UiShellRuntimeGateway.TryReadMatchHudSelection(out var after);
                        if(before.CurrentOrder!=after.CurrentOrder)throw new InvalidOperationException("Enemy camera focus changed the selected order");
                        await Tap(touch,match.PlayerFocusButton);
                        var aria=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
                        AuditButtons(alignmentOnly);
                        await Shot(language+"-"+width+"-hud",width,height);
                        var watch=Field<Button>(aria,"watchButton");
                        await Tap(touch,watch);
                        await Task.Delay(400);
                        AuditButtons(alignmentOnly);
                        await Shot(language+"-"+width+"-consent",width,height);
                        await Tap(touch,Field<Button>(aria,"watchCancel"));
                    }
                }
                await Tap(touch,selection.CommandWheelOpenButton);
                await Task.Delay(700);
                await Shot("fa-IR-1280-commands-open",1280,720);
                Debug.Log((alignmentOnly ? "[HudIconAlignmentNative]" : "[HudButtonIconsNative]") + " result=Passed screens=9 locales=en,fa-IR widths=1920,1280 input=Touch cameraOrders=Unchanged completeMatch=NotClaimed deviceAcceptance=Pending");
                return 0;
            }
            catch(Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[HudButtonIconsNative] result=Failed");
                return 1;
            }
            finally
            {
                touch?.Dispose();
                UiShellRuntimeGateway.StopAriaPlay();
                if(EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.ExitPlaymode();
                    await Until(()=>!EditorApplication.isPlayingOrWillChangePlaymode,60);
                }
                InputSystem.settings=inputSettings;
                if(fixture!=null)UnityEngine.Object.DestroyImmediate(fixture);
                Application.runInBackground=previousBackground;
                Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",background);
                GameLocalization.SetLocale(locale,false);
                EditorSettings.enterPlayModeOptions=options;
                EditorSettings.enterPlayModeOptionsEnabled=optionsEnabled;
                if(hadScenarioPreference)PlayerPrefs.SetString(scenarioPreference,previousScenario);
                else PlayerPrefs.DeleteKey(scenarioPreference);
                PlayerPrefs.Save();
                MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            }
        }
        private static T Field<T>(object owner,string name) where T:class=>
            owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner) as T;
        private static async Task Until(Func<bool> condition,double seconds)
        {
            double end=EditorApplication.timeSinceStartup+seconds;
            while(!condition())
            {
                if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("HUD native state timed out");
                await Task.Delay(100);
            }
        }
        private static async Task Tap(AriaTouchInputUiSystemHelper touch,Button button)
        {
            await Until(()=>button!=null && button.IsActive() && button.IsInteractable(),15);
            var rect=(RectTransform)button.transform;
            var canvas=button.GetComponentInParent<Canvas>();
            var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,
                rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)
                throw new InvalidOperationException("Native hit target obstructed: "+button.name);
            if(!touch.TryGesture(point,point,.18f,0,Time.unscaledTime))
                throw new InvalidOperationException("Touch rejected: "+button.name);
            double end=EditorApplication.timeSinceStartup+5;
            do
            {
                touch.Tick(Time.unscaledTime);
                await Task.Delay(50);
                if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("Touch did not complete");
            }while(touch.IsBusy);
            await Task.Delay(400);
            Debug.Log("[HudButtonIconsNative] touch="+button.name);
        }
        private static void AuditButtons(bool alignmentOnly)
        {
            HudButtonIconValidation.ValidateNativeArtworkCentres();
            foreach(var view in UnityEngine.Object.FindObjectsByType<HudIconButtonView>(FindObjectsSortMode.None))
            {
                var text=view.GetComponentInChildren<TMP_Text>();
                if(text==null||!text.gameObject.activeInHierarchy)continue;
                if(!alignmentOnly && (text.enableAutoSizing||Mathf.Abs(text.fontSize-HudIconButtonView.LabelSize)>.01f))
                    throw new InvalidOperationException("Inconsistent native label: "+view.name);
                if(!alignmentOnly && text.GetPreferredValues().x>text.rectTransform.rect.width+1)
                    throw new InvalidOperationException("Native label overflows: "+view.name);
                var icon=view.transform.Find("HudActionIcon")?.GetComponent<Image>();
                if(icon==null||icon.sprite==null||icon.raycastTarget)
                    throw new InvalidOperationException("Missing or interactive decorative icon: "+view.name);
                var graphics=view.GetComponentsInChildren<V3GradientGraphic>();
                foreach(var graphic in graphics)
                    if(graphic.name!="HudRoleSurface"&&graphic.enabled)
                        throw new InvalidOperationException("Inherited role background visible: "+view.name);
            }
        }
        private static async Task Shot(string name,int width,int height)
        {
            string path=Output+"/"+name+".png";
            ScreenCapture.CaptureScreenshot(path);
            await Task.Delay(900);
            await Until(()=>File.Exists(path),10);
            byte[] png=File.ReadAllBytes(path);
            int actualWidth=png[16]<<24|png[17]<<16|png[18]<<8|png[19];
            int actualHeight=png[20]<<24|png[21]<<16|png[22]<<8|png[23];
            if(actualWidth!=width||actualHeight!=height)
                throw new InvalidOperationException("Native capture size mismatch "+actualWidth+"x"+actualHeight);
        }
    }
}
