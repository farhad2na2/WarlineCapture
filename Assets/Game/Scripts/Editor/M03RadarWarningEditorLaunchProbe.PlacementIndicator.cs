using System;
using System.Reflection;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private static bool placementIndicatorAudit, placementIndicatorVerified;
        private static float placementIndicatorStart, placementArrowTravel;
        private static int placementIndicatorFrames;
        private static Vector3 placementArrowFirst;
        public static void RunPlacementIndicatorEnglish() { RunBuildingJourneyEnglish(); BeginPlacementIndicator(); }
        public static void RunPlacementIndicatorPersian() { RunBuildingJourneyPersian(); BeginPlacementIndicator(); }
        public static void RunPlacementIndicatorWidePersian()
        { RunPlacementIndicatorPersian(); MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080); }
        private static void BeginPlacementIndicator()
        {
            placementIndicatorAudit=true; placementIndicatorVerified=false;
            placementIndicatorStart=-1; placementIndicatorFrames=0; placementArrowTravel=0;
        }
        private static bool ObservePlacementIndicator(Button button)
        {
            if (!placementIndicatorAudit || placementIndicatorVerified) return false;
            var cue=GameObject.Find("AriaAssistantTargetIndicatorRuntime");
            if(cue==null) throw new InvalidOperationException("Placement confirm guidance is missing.");
            var frame=(RectTransform)cue.transform;
            var target=(RectTransform)button.transform;
            var border=IndicatorScreenBounds(frame); var bounds=IndicatorScreenBounds(target);
            if(placementIndicatorStart<0)
            {
                ScreenCapture.CaptureScreenshot(Output+"/placement-indicator-first-"+GameLocalization.CurrentLocaleCode+".png");
                var canvas=button.GetComponentInParent<Canvas>();
                var pulse=cue.GetComponent<TutorialAttentionPulseView>();
                var callback=typeof(TutorialAttentionPulseView).GetField("prepareFrame",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pulse) as Action;
                var owner=callback?.Target;
                var actual=owner?.GetType().GetField("_directTutorialTarget",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner) as RectTransform;
                Debug.Log($"[PlacementIndicatorTarget] same={actual==target} actual={actual?.name} bounds={(actual!=null?IndicatorScreenBounds(actual):default)} frameRect={frame.rect} frameScale={frame.lossyScale}");
                Debug.Log($"[PlacementIndicator] button={bounds} frame={border} canvas={canvas.name}/{canvas.renderMode}/{canvas.worldCamera?.name} root={canvas.rootCanvas.name}/{canvas.rootCanvas.renderMode}/{canvas.rootCanvas.worldCamera?.name} rawRect={target.rect} scale={target.lossyScale}");
            }
            if(Vector2.Distance(border.center,bounds.center)>2 ||
                border.xMin>bounds.xMin || border.yMin>bounds.yMin || border.xMax<bounds.xMax || border.yMax<bounds.yMax)
                throw new InvalidOperationException("Placement guide does not surround the actual green confirm button: target="+bounds+" frame="+border);
            var arrow=frame.Find("AttentionTapPointer") as RectTransform;
            if(arrow==null || !arrow.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Placement confirm guide arrow is missing.");
            var caption=frame.Find("TopBorderCaption") as RectTransform;
            if(caption==null || !caption.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Placement guide caption is missing.");
            foreach(var decoration in new[]{arrow,caption})
            {
                var rect=IndicatorScreenBounds(decoration);
                if(rect.xMin<0 || rect.yMin<0 || rect.xMax>Screen.width || rect.yMax>Screen.height)
                    throw new InvalidOperationException("Placement guide decoration leaves the screen.");
                foreach(var map in UnityEngine.Object.FindObjectsByType<MatchHudMinimapView>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
                    if(map.MapRect!=null && rect.Overlaps(IndicatorScreenBounds(map.MapRect)))
                        throw new InvalidOperationException("Placement guide covers the minimap.");
                foreach(var other in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
                    if(other!=button && other.IsActive() && other.IsInteractable() && !other.transform.IsChildOf(frame) &&
                        rect.Overlaps(IndicatorScreenBounds((RectTransform)other.transform)))
                        throw new InvalidOperationException("Placement guide covers another control: "+other.name);
            }
            if(placementIndicatorStart<0) {placementIndicatorStart=Time.unscaledTime;placementArrowFirst=arrow.position;}
            placementIndicatorFrames++;
            placementArrowTravel=Mathf.Max(placementArrowTravel,Vector3.Distance(placementArrowFirst,arrow.position));
            tutorialBuildNext=0;
            if(Time.unscaledTime-placementIndicatorStart<3) return true;
            if(placementIndicatorFrames<20 || !Game.UI.Runtime.SettingsService.Load().Accessibility.ReducedMotion && placementArrowTravel<5)
                throw new InvalidOperationException("Placement pointer did not animate across enough rendered frames.");
            AssertRenderedGold(frame,100); AssertRenderedGold(arrow,30);
            ScreenCapture.CaptureScreenshot(Output+"/placement-indicator-final-"+GameLocalization.CurrentLocaleCode+".png");
            Debug.Log($"[PlacementIndicator] frames={placementIndicatorFrames} travel={placementArrowTravel} aligned,visible,screen-safe");
            placementIndicatorVerified=true; return false;
        }
    }
}
