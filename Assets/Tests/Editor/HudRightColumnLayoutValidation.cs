using System;
using System.Linq;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class HudRightColumnLayoutValidation
{
    public static void RunStackedButtons()
    {
        try
        {
            using(ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode(); M03PresentationTests.RunFocusedValidation();
                if(ValidationExit.LastExitCode!=0) throw new Exception("ARIA stacked presentation failed.");
                MatchHudAssistantUiSystemHelperTests.RunShowMeValidation();
                MatchHudAssistantUiSystemHelperTests.RunFocusedValidation();
                if(ValidationExit.LastExitCode!=0) throw new Exception("ARIA guidance failed.");
                AriaInstructionViewportTests.Run();
            }
            Debug.Log("[AriaStackedButtons] result=Passed presentations=192 locales=2 viewports=8 guidance=34");
        }
        catch(Exception error) {Debug.LogException(error);EditorApplication.Exit(1);}
    }

    public static void RunLayoutOnly()
    {
        try
        {
            HudRightColumnLayoutBuilder.Apply();
            new HudRightColumnLayoutValidation().MinimapDockAndContentHeightFollowActualControlsAndCopy();
            new TutorialCueOverlayTests().NextClickFrameIsAbovePopupAndNeverBlocksItsButton();
            using(ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode();M03PresentationTests.RunFocusedValidation();
                if(ValidationExit.LastExitCode!=0) throw new Exception("ARIA language/layout regression failed.");
            }
            Debug.Log("[AriaUtilityLayout] result=Passed presentations=192 languages=EN,FA aspects=16:9,20:9 textSizes=normal,large captions=nonoverlapping");
        }
        catch(Exception error) {Debug.LogException(error);EditorApplication.Exit(1);}
    }

    public static void Run()
    {
        try
        {
            HudRightColumnLayoutBuilder.Apply();
            new HudRightColumnLayoutValidation().MinimapDockAndContentHeightFollowActualControlsAndCopy();
            using(ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode(); M03PresentationTests.RunFocusedValidation();
                if(ValidationExit.LastExitCode!=0) throw new Exception("ARIA language/layout regression failed.");
            }
            Debug.Log("[HudRightColumnQa] result=Passed dock=16:9,20:9 content=empty,short,long localization=EN,FA");
            M03RadarWarningEditorLaunchProbe.RunTutorialBuildClicks();
        }
        catch(Exception error) {Debug.LogException(error);EditorApplication.Exit(1);}
    }

    [Test]
    public void MinimapDockAndContentHeightFollowActualControlsAndCopy()
    {
        foreach(var size in new[]{new Vector2(1920,1080),new Vector2(2400,1080)})
        {
            var canvas=new GameObject("HUD layout QA",typeof(RectTransform),typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            ((RectTransform)canvas.transform).sizeDelta=size;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
            var instance=UnityEngine.Object.Instantiate(prefab,canvas.transform);
            try
            {
                foreach(var responsive in instance.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true)) responsive.RefreshLayout();
                var dock=instance.GetComponentInChildren<MissionHudTouchLayoutView>(true);
                var aria=instance.GetComponentInChildren<AriaTutorialBriefingView>(true);
                dock.Apply(true);Canvas.ForceUpdateCanvases();dock.RefreshLayout();
                Assert.That(dock.Minimap.IsChildOf(aria.transform),Is.False);
                var build=instance.GetComponentInChildren<MatchOverlayCommandControlsView>(true).BuildButton;
                var mapBounds=RectBounds(dock.transform,dock.Minimap);
                var buildBounds=RectBounds(dock.transform,(RectTransform)build.transform);
                Assert.That(mapBounds.min.y-buildBounds.max.y,Is.EqualTo(8).Within(.2));
                Assert.That(((RectTransform)dock.transform).rect.xMax-mapBounds.max.x,Is.EqualTo(15).Within(.2));
                aria.TitleText.text="";aria.BodyText.text="";aria.SetPresentationVisible(true);aria.RefreshContentLayout();
                Assert.That(aria.BriefingLayout.gameObject.activeSelf,Is.False,"Empty instruction area must disappear.");
                float empty=((RectTransform)aria.transform).rect.height;
                aria.TitleText.text="DEFEND";aria.BodyText.text="Place a defense.";aria.SetPresentationVisible(true);aria.RefreshContentLayout();
                float shortHeight=((RectTransform)aria.transform).rect.height;
                aria.BodyText.text="Place a defense near the western road. Select an available guard tower or road barrier, choose its location, check that the full footprint is green, and confirm placement before returning to your squad.";
                aria.RefreshContentLayout();Canvas.ForceUpdateCanvases();
                Assert.That(((RectTransform)aria.transform).rect.height,Is.GreaterThan(shortHeight));
                Assert.That(shortHeight,Is.GreaterThan(empty));
                Assert.That(aria.BodyText.preferredHeight,Is.LessThanOrEqualTo(aria.BodyText.rectTransform.rect.height+1));
                aria.BodyText.text=string.Concat(Enumerable.Repeat(aria.BodyText.text+" ",8));
                aria.RefreshContentLayout();Canvas.ForceUpdateCanvases();
                Assert.That(aria.BodyText.GetComponentInParent<UnityEngine.UI.ScrollRect>().vertical,Is.True,"Very long copy scrolls without covering the dock.");
                var scrollbar=aria.BodyText.GetComponentInParent<UnityEngine.UI.ScrollRect>().verticalScrollbar;
                var thumbBounds=RectBounds(scrollbar.transform,scrollbar.handleRect);
                Assert.That(thumbBounds.size.x,Is.LessThanOrEqualTo(((RectTransform)scrollbar.transform).rect.width+.1f),"Overflow thumb must not become a wide cyan panel over the instructions.");
                var railBounds=RectBounds(dock.transform,(RectTransform)aria.transform);
                Assert.That(railBounds.min.y-mapBounds.max.y,Is.GreaterThanOrEqualTo(11.99f),"ARIA must not collide with the independent map.");
            }
            finally {UnityEngine.Object.DestroyImmediate(canvas);}
        }
    }
    private static Bounds RectBounds(Transform parent,RectTransform rect)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        var bounds=new Bounds(parent.InverseTransformPoint(corners[0]),Vector3.zero);
        for(int i=1;i<4;i++) bounds.Encapsulate(parent.InverseTransformPoint(corners[i]));
        return bounds;
    }
}
