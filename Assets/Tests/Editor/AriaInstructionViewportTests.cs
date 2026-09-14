using System;
using Game.Configs;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AriaInstructionViewportTests
{
    public static void Run()
    {
        try
        {
            var test = new AriaInstructionViewportTests();
            foreach (float width in new[]{1920f,2400f})
            foreach (bool persian in new[]{false,true})
                { test.HiddenPlacementDoesNotClipTheMovementLesson(width,persian); test.BreachStatusFits(width,persian); }
            new HudRightColumnLayoutValidation().MinimapDockAndContentHeightFollowActualControlsAndCopy();
            new ProductionSourceGrowthArchitectureTests().AllBaselinedPathsRespectRatchetedLineAndByteCeilings();
            Debug.Log("[AriaInstructionViewport] result=Passed cases=8 hidden-placement,full-copy,breach-status,buttons,dock");
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    [TestCase(1920f,false)] [TestCase(1920f,true)]
    [TestCase(2400f,false)] [TestCase(2400f,true)]
    public void HiddenPlacementDoesNotClipTheMovementLesson(float width,bool persian)
        => CheckInstruction(width,persian,false);

    [TestCase(1920f,false)] [TestCase(1920f,true)]
    [TestCase(2400f,false)] [TestCase(2400f,true)]
    public void BreachStatusFits(float width,bool persian) => CheckInstruction(width,persian,true);

    private static void CheckInstruction(float width,bool persian,bool breach)
    {
        var previousLocale=GameLocalization.CurrentLocaleCode;
        var canvas=new GameObject("ARIA viewport QA",typeof(RectTransform),typeof(Canvas));
        canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        ((RectTransform)canvas.transform).sizeDelta=new Vector2(width,1080);
        try
        {
            GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath),persian?"fa-IR":"en",false);
            var hud=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab"),canvas.transform);
            var placement=BuildPlacementConfirmationBarView.Ensure(AssetDatabase.LoadAssetAtPath<GameObject>(BuildPlacementConfirmationBarPrefabSetupEditor.PrefabPath),(RectTransform)canvas.transform);
            Assert.IsTrue(placement.Root.gameObject.activeInHierarchy,"Reproduce the hidden but active placement hierarchy.");
            Assert.IsFalse(placement.HasPendingPlacement);
            Assert.AreEqual(0f,placement.GetComponent<CanvasGroup>().alpha);
            foreach(var layout in hud.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true)) layout.RefreshLayout();
            var dock=hud.GetComponentInChildren<MissionHudTouchLayoutView>(true);
            dock.RefreshLayout(); Canvas.ForceUpdateCanvases(); dock.RefreshLayout();
            var view=hud.GetComponentInChildren<AriaTutorialBriefingView>(true);
            var copy=M03RadarWarningTutorialCopyCatalog.Steps[4];
            view.transform.Find("M03Actions").gameObject.SetActive(true);
            string title=persian?copy.PersianTitle:copy.Title, body=persian?copy.PersianBody:copy.Body;
            if(breach)
            {
                title=GameLocalization.Get("mission.m05.tutorial.2.title");
                body=GameLocalization.Get("mission.m05.tutorial.2.body")+"\n\n"+
                    string.Format(GameLocalization.Get("mission.m05.hud.status"),0,8,"11:59",20);
            }
            view.Apply(new UiAssistantPanelModel(1,true,0,UiAssistantGoalRowModel.Empty,UiAssistantGoalRowModel.Empty,UiAssistantGoalRowModel.Empty,
                UiAssistantMessageRowModel.Empty,UiAssistantMessageRowModel.Empty,UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,UiAssistantMessageRowModel.Empty,UiAssistantTargetLockModel.Empty,UiAssistantNarrationModel.Empty,true,
                title,body,"","CONTINUE",true,true,false,false,"","",
                tutorialStep:breach?(byte)2:(byte)5,tutorialStepCount:breach?(byte)8:(byte)12,tutorialRightToLeft:persian));
            view.SetPresentationVisible(true); view.ApplyAccessibility(false,false); Canvas.ForceUpdateCanvases(); view.RefreshContentLayout();
            view.BodyText.ForceMeshUpdate(true,true);
            var viewport=(RectTransform)view.BodyText.transform.parent;
            if(breach) Assert.IsFalse(view.BodyText.isTextOverflowing,"M5 status, including the clock, must fit.");
            // Full-width stacked controls may require scrolling; the content must remain
            // complete, visibly scrollable, and stable while mission status changes.
            {
                var scroll=viewport.GetComponent<UnityEngine.UI.ScrollRect>();
                Assert.That(view.BodyText.rectTransform.rect.height,Is.GreaterThanOrEqualTo(view.BodyText.preferredHeight));
                if(scroll.vertical)
                {
                    Assert.IsTrue(scroll.verticalScrollbar.gameObject.activeInHierarchy,"Long instructions must visibly advertise scrolling.");
                    scroll.verticalNormalizedPosition=.35f;
                    var rtl=view.BodyText as RTLTMPro.RTLTextMeshPro;
                    view.BodyText.text=(rtl!=null?rtl.OriginalText:view.BodyText.text).Replace("11:59","11:58");
                    view.RefreshContentLayout();
                    Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(.35f).Within(.01f),"A ticking status must not snap the reader back to the top.");
                }
            }
            var bodyBounds=BoundsIn(view.transform,viewport);
            var buttonBounds=BoundsIn(view.transform,(RectTransform)view.ShowMeButton.transform);
            Assert.That(bodyBounds.min.y-buttonBounds.max.y,Is.GreaterThanOrEqualTo(15.9f));
            var build=hud.GetComponentInChildren<MatchOverlayCommandControlsView>(true).BuildButton;
            Assert.That(BoundsIn(dock.transform,dock.Minimap).min.y-BoundsIn(dock.transform,(RectTransform)build.transform).max.y,Is.EqualTo(8f).Within(.2f));
        }
        finally {UnityEngine.Object.DestroyImmediate(canvas);GameLocalization.SetLocale(previousLocale,false);}
    }

    private static Bounds BoundsIn(Transform parent,RectTransform rect)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        var bounds=new Bounds(parent.InverseTransformPoint(corners[0]),Vector3.zero);
        for(int i=1;i<4;i++) bounds.Encapsulate(parent.InverseTransformPoint(corners[i]));
        return bounds;
    }
}
