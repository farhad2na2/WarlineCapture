using System;
using System.Reflection;
using Game.UI.Runtime;
using Game.UI.Contracts;
using UnityEditor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class M03PlacementRenderOrderTests
{
    [Test]
    public void GuideFollowsFinalButtonAfterValidityAndCanvasLayoutChange()
    {
        var hud=new GameObject("Late layout HUD",typeof(RectTransform),typeof(Canvas));
        var overlay=new GameObject("Guidance overlay",typeof(RectTransform),typeof(Canvas));
        var target=new GameObject("Confirm",typeof(RectTransform),typeof(Image),typeof(Button));
        var cue=new GameObject("Cue",typeof(RectTransform));
        TutorialAttentionPulseView pulse=null;
        try
        {
            hud.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            overlay.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            target.transform.SetParent(hud.transform,false);cue.transform.SetParent(overlay.transform,false);
            var button=(RectTransform)target.transform;var frame=(RectTransform)cue.transform;
            button.anchorMin=button.anchorMax=new Vector2(1,0);button.pivot=new Vector2(1,0);
            button.anchoredPosition=new Vector2(-40,40);button.sizeDelta=new Vector2(290,220);
            frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(.5f,.5f);
            Canvas.ForceUpdateCanvases();
            var type=typeof(AriaTutorialBriefingView).Assembly.GetType("Game.UI.Runtime.AssistantHighlightPresentationSystemHelper");
            var owner=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
            void Set(string name,object value)=>type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,value);
            Set("_directTutorialCue",true);Set("_directTutorialTarget",button);Set("_screenTargetCanvas",overlay.GetComponent<Canvas>());
            Set("_screenTargetIndicator",frame);Set("_commandButtonCorners",new Vector3[4]);
            var callback=(Action)Delegate.CreateDelegate(typeof(Action),owner,type.GetMethod("TickCommandCue",BindingFlags.Instance|BindingFlags.NonPublic));
            var flags=BindingFlags.Static|BindingFlags.NonPublic;
            typeof(TutorialAttentionPulseView).GetMethod("BindFrame",flags).Invoke(null,new object[]{frame,callback});
            pulse=cue.GetComponent<TutorialAttentionPulseView>();
            // EditMode tests explicitly exercise the runtime enable/disable lifecycle.
            typeof(TutorialAttentionPulseView).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pulse,null);
            foreach(float scale in new[]{1f,.85f,1.2f})
            {
                typeof(TutorialAttentionPulseView).GetMethod("Present",flags).Invoke(null,new object[]{frame,Time.unscaledTime,true,1,0f});
                // The footer reflows after the HUD's Update, as validity/viewport changes do.
                button.anchoredPosition+=new Vector2(-27,41);button.localScale=Vector3.one*scale;
                Canvas.ForceUpdateCanvases();
                var a=Bounds(button);var b=Bounds(frame);
                Assert.That(Vector2.Distance(a.center,b.center),Is.LessThan(1f));
                Assert.IsTrue(b.Contains(a.min)&&b.Contains(a.max),"Guide must enclose the rendered button after late layout.");
            }
        }
        finally
        {
            if(pulse!=null) typeof(TutorialAttentionPulseView).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pulse,null);
            UnityEngine.Object.DestroyImmediate(hud);UnityEngine.Object.DestroyImmediate(overlay);
        }
    }
    [Test]
    public void SkippingOptionalLessonCancelsItsUnfinishedPreview()
    {
        var canvas=new GameObject("Placement skip QA",typeof(RectTransform),typeof(Canvas));
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab");
            var bar=BuildPlacementConfirmationBarView.Ensure(prefab,(RectTransform)canvas.transform);
            var command=new PreviewCommand();bar.BindRuntimeCommands(command);
            var go=new GameObject("Mission HUD",typeof(MissionDefenseHudView));go.transform.SetParent(canvas.transform,false);
            typeof(MissionDefenseHudView).GetMethod("Skip",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(go.GetComponent<MissionDefenseHudView>(),null);
            Assert.IsFalse(command.HasPendingBuildingPlacement,"Skipping may not leave placement controls behind the next lesson.");
            Assert.AreEqual(1,command.Cancelled);
        }
        finally {UnityEngine.Object.DestroyImmediate(canvas);}
    }
    private sealed class PreviewCommand : IBuildingUiCommand
    {
        public int Cancelled; public int CurrentDollars=>50000;
        public bool HasPendingBuildingPlacement=>Cancelled==0;
        public bool CanConfirmBuildingPlacement=>true;
        public string PlacementStatusText=>"Valid placement";
        public int ActivePlacementCost=>15;public int ActivePlacementCreditsCost=>6000;
        public float ActivePlacementDurationSeconds=>30;public int MaxQueuedUnitProductions=>25;
        public BuildingUiCommandFailure GetCampRequestFailure(GameObject p,int price,out string name){name="";return default;}
        public BuildingUiCommandFailure TryRequestCampItem(GameObject p,int price,out string name,bool focus){name="";return default;}
        public bool CancelProduction(int id,int index)=>false;
        public bool ConfirmBuildingPlacement()=>true;
        public void CancelBuildingPlacement()=>Cancelled++;
        public bool RotateBuildingPlacement()=>true;
    }
    private static Rect Bounds(RectTransform rect)
    {
        var c=new Vector3[4];rect.GetWorldCorners(c);
        return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y);
    }
    public static void Run()
    {
        try
        {
            new M03PlacementRenderOrderTests().GuideFollowsFinalButtonAfterValidityAndCanvasLayoutChange();
            new M03PlacementRenderOrderTests().SkippingOptionalLessonCancelsItsUnfinishedPreview();
            new HudRightColumnLayoutValidation().MinimapDockAndContentHeightFollowActualControlsAndCopy();
            var pointers=new TutorialAttentionAndContinueTests();
            pointers.PointerRotatesInsideEveryEdgeAndAvoidsOtherControls();
            pointers.PlacementPointerFindsOpenCornerBesideMinimapAndFooter();
            Debug.Log("[M03PlacementRenderOrder] result=Passed late-layout,validity-scale,edge-pointer");
            M03BattleClarityTests.Run();
        }
        catch(Exception e){Debug.LogException(e);ValidationExit.Failed();}
    }
}
