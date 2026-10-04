using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Game.Runtime;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    // Native prefab fixture. Checks presentation only; does not claim normal mission readiness.
    public static class MatchHudUiUxReview
    {
        public static Task<int> RunBefore()=>Run(false);
        public static Task<int> RunAfter()=>Run(true);
        private static async Task<int> Run(bool verify)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Requires idle Editor");
            var setup=EditorSceneManager.GetSceneManagerSetup();var locale=GameLocalization.CurrentLocaleCode;
            var options=EditorSettings.enterPlayModeOptions;bool enabled=EditorSettings.enterPlayModeOptionsEnabled;
            string output="Design/AgentReports/MatchHudUiUxAudit/"+(verify?"After/":"Before/")+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(output);
            bool passed=false;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=options|EnterPlayModeOptions.DisableDomainReload;
                MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);
                EditorApplication.EnterPlaymode();await Until(()=>EditorApplication.isPlaying);
                Application.runInBackground=true;
                var camera=new GameObject("HudReviewCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.028f,.032f,.038f,1);camera.orthographic=true;camera.orthographicSize=540;camera.depth=100;camera.transform.position=new Vector3(0,0,-10);
                var canvas=new GameObject("HudReviewCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=30000;
                foreach(var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(other.rootCanvas!=canvas)other.gameObject.SetActive(false);
                var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(4800,2160);scaler.matchWidthOrHeight=.5f;
                var hud=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab"),canvas.transform,false);
                var rect=(RectTransform)hud.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                var banner=hud.GetComponentInChildren<MatchHudCurrentOrderBannerView>(true);
                foreach(var language in new[]{"en","fa-IR"})foreach(int width in new[]{1920,2400})
                {
                    GameLocalization.SetLocale(language,false);MainMenuV3PrefabBuilder.SetGameViewResolution(width,1080);await Task.Delay(700);
                    bool rtl=language=="fa-IR";
                    var move=new MatchHudCurrentOrderBannerModel(true,TacticalCommandMode.Move,GameLocalization.Get("tactical.banner.mode.move.title"),GameLocalization.Get("tactical.banner.mode.move.description"),null);
                    for(int i=0;i<5;i++){banner.Apply(move);await Task.Delay(100);}
                    Canvas.ForceUpdateCanvases();
                    if(verify)
                    {
                        CheckBinding(banner.OrderText,move.OrderText,rtl);CheckBinding(banner.DescriptionText,move.DescriptionText,rtl);
                        foreach(var strip in hud.GetComponentsInChildren<RectTransform>(true).Where(r=>r.name=="NameStrip"))
                        {
                            var health=strip.parent.Find("Frame/HealthFrame") as RectTransform;if(health==null)continue;
                            var corners=new Vector3[4];health.GetWorldCorners(corners);float healthTop=strip.parent.InverseTransformPoint(corners[1]).y;
                            strip.GetWorldCorners(corners);float nameBottom=strip.parent.InverseTransformPoint(corners[0]).y;
                            if(nameBottom-healthTop<5.5f)throw new InvalidOperationException("Squad name overlaps health bar: "+strip.parent.name);
                            float nameTop=strip.parent.InverseTransformPoint(corners[1]).y;
                            if(nameBottom<((RectTransform)strip.parent).rect.yMin||nameTop>((RectTransform)strip.parent).rect.yMax)throw new InvalidOperationException("Squad name left card bounds: "+strip.parent.name);
                        }
                        banner.Hide();banner.Apply(move);await Task.Delay(100);CheckBinding(banner.OrderText,move.OrderText,rtl);
                    }
                    ScreenCapture.CaptureScreenshot(output+"/hud-"+language+"-"+width+".png");await Task.Delay(500);
                    banner.Hide();
                    if(verify&&(!string.IsNullOrEmpty(banner.OrderText.GetComponent<V3LocalizedTextBindingView>().SourceValue)||!string.IsNullOrEmpty(banner.DescriptionText.GetComponent<V3LocalizedTextBindingView>().SourceValue)))throw new InvalidOperationException("Hidden banner retained stale localized source");
                }
                await Until(()=>Directory.GetFiles(output,"*.png").Length==4&&Directory.GetFiles(output,"*.png").All(p=>new FileInfo(p).Length>0));passed=true;
                Debug.Log("[MatchHudUiUxReview] result=Passed fixture=True locales=en,fa-IR aspects=16:9,20:9 localizedUpdates="+verify+" nameHealthClearance="+verify+" missionReadiness=NotClaimed output="+output);
            }
            catch(Exception error){Debug.LogException(error);Debug.LogError("[MatchHudUiUxReview] result=Failed output="+output);}
            finally
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.ExitPlaymode();await Until(()=>!EditorApplication.isPlayingOrWillChangePlaymode);}
                await Task.Delay(1000);EditorSettings.enterPlayModeOptions=options;EditorSettings.enterPlayModeOptionsEnabled=enabled;GameLocalization.SetLocale(locale,false);EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            return passed?0:1;
        }
        private static void CheckBinding(TMP_Text text,string source,bool rtl)
        {
            var binding=text.GetComponent<V3LocalizedTextBindingView>();if(text==null)throw new InvalidOperationException("Missing native banner label");if(binding==null)throw new InvalidOperationException("Missing localized owner: "+text.name);
            if(GameLocalization.TryGetSourceByLocalized(source,out _,out string authored))source=authored;
            if(binding.SourceValue!=source)throw new InvalidOperationException("Stale source: "+text.name);
            if(rtl&&text.font!=GameLocalization.CurrentFontAsset)throw new InvalidOperationException("Wrong locale font: "+text.name);
            if(text.fontSharedMaterial.mainTexture!=text.font.material.mainTexture)throw new InvalidOperationException("Wrong font atlas: "+text.name);
        }
        private static T Field<T>(object owner,string name)=> (T)owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
        private static async Task Until(Func<bool> condition){double end=EditorApplication.timeSinceStartup+90;while(!condition()){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("HUD fixture timed out");await Task.Delay(100);}}
    }
}
