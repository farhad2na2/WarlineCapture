using System;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private static bool indicatorAudit;
        private static float indicatorStarted;
        private static Vector3 indicatorFirst;
        private static float indicatorTravel;
        private static int indicatorFrames;
        public static void RunIndicatorEnglish() {RunBuildingJourneyEnglish(); BeginIndicatorAudit();}
        public static void RunIndicatorPersian() {RunBuildingJourneyPersian(); BeginIndicatorAudit();}
        private static void BeginIndicatorAudit() {indicatorAudit=true; indicatorStarted=-1; indicatorFrames=0; indicatorTravel=0;}
        private static void AssertRenderedGold(RectTransform rect,int minimum)
        {
            var corners=new Vector3[4]; rect.GetWorldCorners(corners);
            var root=rect.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera=root.renderMode==RenderMode.ScreenSpaceOverlay?null:root.worldCamera;
            var min=new Vector2(float.MaxValue,float.MaxValue); var max=new Vector2(float.MinValue,float.MinValue);
            foreach(var corner in corners) {var p=RectTransformUtility.WorldToScreenPoint(camera,corner); min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            int x=Mathf.Max(0,Mathf.FloorToInt(min.x)),y=Mathf.Max(0,Mathf.FloorToInt(min.y));
            int w=Mathf.Min(Screen.width-x,Mathf.CeilToInt(max.x)-x),h=Mathf.Min(Screen.height-y,Mathf.CeilToInt(max.y)-y);
            var image=new Texture2D(w,h,TextureFormat.RGB24,false);
            try
            {
                image.ReadPixels(new Rect(x,y,w,h),0,0); image.Apply(); int gold=0;
                foreach(var pixel in image.GetPixels32()) if(pixel.r>170 && pixel.g>130 && pixel.b<95 && pixel.r>pixel.b*2) gold++;
                if(gold<minimum) throw new InvalidOperationException("Guide is active but not visibly rendered: "+rect.name+" goldPixels="+gold);
                Debug.Log("[IndicatorPixels] "+rect.name+" gold="+gold);
            }
            finally {UnityEngine.Object.Destroy(image);}
        }

        private static Rect IndicatorScreenBounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            foreach (var corner in corners)
            {
                var point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static bool ObserveIndicator(AriaTutorialBriefingView aria)
        {
            if(!indicatorAudit || !aria.ContinueButton.IsActive()) return false;
            var cue=GameObject.Find("AriaAssistantTargetIndicatorRuntime");
            if(cue==null) return false;
            var pulse=cue.GetComponent<TutorialAttentionPulseView>();
            var callback=typeof(TutorialAttentionPulseView).GetField("prepareFrame",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.GetValue(pulse) as Action;
            var target=callback?.Target;
            var button=target?.GetType().GetField("_directTutorialTarget",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.GetValue(target) as RectTransform;
            if(button!=aria.ContinueButton.transform) return false;
            var frame=(RectTransform)cue.transform; var corners=new Vector3[4]; var actual=new Vector3[4];
            frame.GetWorldCorners(corners); button.GetWorldCorners(actual);
            if(Vector3.Distance((corners[0]+corners[2])*.5f,(actual[0]+actual[2])*.5f)>2 ||
               corners[0].x>actual[0].x || corners[0].y>actual[0].y || corners[2].x<actual[2].x || corners[2].y<actual[2].y)
                throw new InvalidOperationException("M3 final ARIA button and frame do not align.");
            var arrow=frame.Find("AttentionTapPointer") as RectTransform;
            var caption=frame.Find("TopBorderCaption") as RectTransform;
            if(arrow==null || !arrow.gameObject.activeInHierarchy || caption==null || !caption.gameObject.activeInHierarchy)
                throw new InvalidOperationException("M3 guide arrow/caption blinked off.");
            if(cue.GetComponentInChildren<TutorialFocusFrameGraphic>()==null) throw new InvalidOperationException("M3 double edge border missing.");
            foreach(var rect in new[]{arrow,caption})
            {
                rect.GetWorldCorners(corners);
                var canvas=rect.GetComponentInParent<Canvas>().rootCanvas;
                var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                foreach(var corner in corners)
                {
                    var pixel=RectTransformUtility.WorldToScreenPoint(camera,corner);
                    if(pixel.x<0 || pixel.y<0 || pixel.x>Screen.width || pixel.y>Screen.height)
                        throw new InvalidOperationException("M3 guide decoration left the screen.");
                }
            }
            foreach (var text in new[] { aria.TitleText, aria.BodyText })
                if (text != null && text.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(text.text))
                    foreach (var decoration in new[] { arrow, caption })
                        if (IndicatorScreenBounds(decoration).Overlaps(IndicatorScreenBounds(text.rectTransform)))
                            throw new InvalidOperationException("Guide decoration overlaps the visible ARIA instruction.");
            if(indicatorStarted<0)
            {
                indicatorStarted=Time.unscaledTime; indicatorFirst=arrow.position;
                foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
                    Debug.Log($"[IndicatorCanvas] name={canvas.name} root={canvas.rootCanvas.name} mode={canvas.renderMode} rootMode={canvas.rootCanvas.renderMode} order={canvas.sortingOrder} layer={canvas.sortingLayerID} override={canvas.overrideSorting} scale={canvas.scaleFactor}");
                Debug.Log($"[IndicatorGeometry] frame={frame.rect} arrow={arrow.rect} rotation={arrow.localEulerAngles} lossyScale={frame.lossyScale}");
                ScreenCapture.CaptureScreenshot(Output+"/indicator-first-"+GameLocalization.CurrentLocaleCode+".png");
            }
            indicatorTravel=Mathf.Max(indicatorTravel,Vector3.Distance(indicatorFirst,arrow.position)); indicatorFrames++;
            guidanceNext=0;
            if(Time.unscaledTime-indicatorStarted<3) return true;
            if(indicatorFrames<20 || !Game.UI.Runtime.SettingsService.Load().Accessibility.ReducedMotion && indicatorTravel<5)
                throw new InvalidOperationException("M3 attention animation did not visibly move over rendered frames.");
            ScreenCapture.CaptureScreenshot(Output+"/indicator-final-"+GameLocalization.CurrentLocaleCode+".png");
            AssertRenderedGold(frame, 100); AssertRenderedGold(arrow, 30);
            indicatorAudit=false;
            Complete(true,"M3 indicator: centered double frame, stable caption, safe rotating arrow; frames="+indicatorFrames+" travel="+indicatorTravel);
            return true;
        }
    }
}
