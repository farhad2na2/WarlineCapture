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
                test.HiddenPlacementDoesNotClipTheMovementLesson(width,persian);
            new HudRightColumnLayoutValidation().MinimapDockAndContentHeightFollowActualControlsAndCopy();
            new ProductionSourceGrowthArchitectureTests().AllBaselinedPathsRespectRatchetedLineAndByteCeilings();
            Debug.Log("[AriaInstructionViewport] result=Passed cases=4 hidden-placement,full-copy,buttons,dock");
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    [TestCase(1920f,false)] [TestCase(1920f,true)]
    [TestCase(2400f,false)] [TestCase(2400f,true)]
    public void HiddenPlacementDoesNotClipTheMovementLesson(float width,bool persian)
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
            view.Apply(new UiAssistantPanelModel(1,true,0,UiAssistantGoalRowModel.Empty,UiAssistantGoalRowModel.Empty,UiAssistantGoalRowModel.Empty,
                UiAssistantMessageRowModel.Empty,UiAssistantMessageRowModel.Empty,UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,UiAssistantMessageRowModel.Empty,UiAssistantTargetLockModel.Empty,UiAssistantNarrationModel.Empty,true,
                persian?copy.PersianTitle:copy.Title,persian?copy.PersianBody:copy.Body,"","CONTINUE",true,true,false,false,"","",
                tutorialStep:5,tutorialStepCount:12,tutorialRightToLeft:persian));
            view.SetPresentationVisible(true); view.ApplyAccessibility(false,false); Canvas.ForceUpdateCanvases(); view.RefreshContentLayout();
            view.BodyText.ForceMeshUpdate(true,true);
            var viewport=(RectTransform)view.BodyText.transform.parent;
            Assert.That(viewport.rect.height,Is.GreaterThanOrEqualTo(view.BodyText.preferredHeight),"The entire movement instruction must fit above the action buttons.");
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
