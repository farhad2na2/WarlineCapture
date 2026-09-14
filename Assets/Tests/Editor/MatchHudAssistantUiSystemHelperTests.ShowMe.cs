using System;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.Tactical.Contracts;
using NUnit.Framework;
using UnityEngine;

public sealed partial class MatchHudAssistantUiSystemHelperTests
{
    public static void RunShowMeValidation()
    {
        RunCase(test => test.ShowMeFollowsSelectionCommandAndDestination(false));
        RunCase(test => test.ShowMeFollowsSelectionCommandAndDestination(true));
        RunCase(test => test.EveryEnabledMissionShowMeHasATarget(false));
        RunCase(test => test.EveryEnabledMissionShowMeHasATarget(true));
        RunCase(test => test.M4UsesLiveSelectionModeAndHidesShowMeForVisibleIndicators());
        RunCase(test => test.M4GuidesRealSelectionBeforeBoardingOrMoving());
        foreach (byte count in new byte[] { 5, 9, 12, 8 })
            RunCase(test => test.CampaignActionsFollowLiveModeAndArrival(count));
        Debug.Log("[MissionShowMe] result=Passed tests=10 M1-M5=selection,command,destination,waiting");
    }

    [TestCase((byte)5)] [TestCase((byte)9)] [TestCase((byte)12)] [TestCase((byte)8)]
    public void CampaignActionsFollowLiveModeAndArrival(byte count)
    {
        CreateHudHarness(true,out var overlay,out var header,out _);
        byte step = count == 5 ? (byte)2 : count == 9 ? (byte)8 : count == 8 ? (byte)4 : (byte)5;
        var required = count == 9 ? TacticalCommandMode.Attack : TacticalCommandMode.Move;
        var model = CreateStructuredModel(1, recommendationKind:(byte)(count == 9 ? 3 : 2),
            recommendationTargetKind:1, tutorialStep:step, tutorialStepCount:count);
        var gateway = new FakeAssistantPanelGateway(model, UiAssistantHighlightModel.Empty) {
            HasTutorialTarget = true, HasCommandState = true, CommandMode = TacticalCommandMode.Select,
            TutorialTarget = new UiMissionTutorialTarget(Vector3.zero, Vector3.right * 10, true, false) };
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI(); ui.Init(null,new FakeMatchRuntimeState());
        try
        {
            ui.BindMatchHudAssistant(header.gameObject,overlay,LoadPopupPrefab());
            var controls = CreateCommandControls(overlay); ui.BindMatchHudCommandControls(controls);
            var helper = GetPrivateField<MatchHudAssistantUiSystemHelper>(ui,"_matchHudAssistantUiSystem");
            var view = header.Find("AriaAssistantButton").GetComponent<AriaTutorialBriefingView>();
            var highlight = GetPrivateField<AssistantHighlightPresentationSystemHelper>(helper,"_highlightPresentationSystem");
            helper.ApplyReadModel(model); helper.TickHighlight(10);
            Assert.IsNull(GetPrivateField<RectTransform>(highlight,"_directTutorialTarget"), "Select already accepted: indicate the unit instead.");
            Assert.IsTrue(highlight.HasDirectTutorialTarget);
            gateway.TutorialTarget = new UiMissionTutorialTarget(Vector3.zero,Vector3.right*10,false,false);
            helper.TickHighlight(11);
            Assert.AreSame((required == TacticalCommandMode.Move ? controls.MoveButton : controls.AttackButton).transform,
                GetPrivateField<RectTransform>(highlight,"_directTutorialTarget"));
            Assert.IsFalse(view.ShowMeButton.gameObject.activeSelf, "A visible cue replaces Show Me.");
            gateway.CommandMode = required; helper.TickHighlight(12);
            Assert.IsNull(GetPrivateField<RectTransform>(highlight,"_directTutorialTarget"), "Accepted command points to its destination.");
            Assert.IsTrue(highlight.HasDirectTutorialTarget);
            gateway.TutorialTarget = new UiMissionTutorialTarget(Vector3.zero,Vector3.right*10,false,true);
            helper.TickHighlight(13);
            Assert.IsFalse(highlight.HasDirectTutorialTarget, "Wait for real arrival without asking for the command again.");
            Assert.IsFalse(view.ShowMeButton.gameObject.activeSelf);
            gateway.HasTutorialTarget = false; helper.TickHighlight(14);
            Assert.IsFalse(view.ShowMeButton.gameObject.activeSelf, "Missing targets cannot produce a clickable no-op.");
        }
        finally { ui.Dispose(); }
    }

    [TestCase(false)] [TestCase(true)]
    public void ShowMeFollowsSelectionCommandAndDestination(bool extraction)
    {
        CreateHudHarness(true,out var overlay,out var header,out _);
        var model=CreateStructuredModel(1000,recommendationKind:2,recommendationTargetKind:1,
            tutorialStep:(byte)(extraction?3:5),tutorialStepCount:12);
        var gateway=new FakeAssistantPanelGateway(model,UiAssistantHighlightModel.Empty) {
            HasTutorialTarget=true, Extraction=extraction,
            TutorialTarget=new UiMissionTutorialTarget(new Vector3(120,3,90),new Vector3(300,2,70),true,false) };
        UiShellRuntimeGateway.Register(gateway);
        var ui=new MainMenuPlayUI(); ui.Init(null,new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject,overlay,LoadPopupPrefab());
        var controls=CreateCommandControls(overlay); ui.BindMatchHudCommandControls(controls);
        var helper=GetPrivateField<MatchHudAssistantUiSystemHelper>(ui,"_matchHudAssistantUiSystem");
        var view=header.Find("AriaAssistantButton").GetComponent<AriaTutorialBriefingView>();
        helper.ApplyReadModel(model); helper.TickHighlight(100);
        Assert.IsTrue(view.ShowMeButton.interactable); view.ShowMeButton.onClick.Invoke();
        Assert.AreEqual(gateway.TutorialTarget.Selection,gateway.LastTutorialFocus);
        gateway.TutorialTarget=new UiMissionTutorialTarget(gateway.TutorialTarget.Selection,gateway.TutorialTarget.Destination,false,false);
        helper.TickHighlight(101);
        var popup=overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        popup.Show();
        Assert.IsTrue(popup.IsOpen);
        FindNamed(popup.transform,"ShowMeButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen,"Show Me must expose its battlefield/HUD target outside the popup.");
        var highlight=GetPrivateField<AssistantHighlightPresentationSystemHelper>(helper,"_highlightPresentationSystem");
        Assert.AreSame(controls.MoveButton.transform,GetPrivateField<RectTransform>(highlight,"_directTutorialTarget"));
        controls.MoveButton.onClick.Invoke(); helper.TickHighlight(102); view.ShowMeButton.onClick.Invoke();
        Assert.AreEqual(gateway.TutorialTarget.Destination,gateway.LastTutorialFocus);
        Assert.IsTrue(highlight.HasDirectTutorialTarget);
        gateway.TutorialTarget=new UiMissionTutorialTarget(gateway.TutorialTarget.Selection,gateway.TutorialTarget.Destination,false,true);
        helper.TickHighlight(103); Assert.IsFalse(view.ShowMeButton.interactable,"No misleading clickable Show Me while waiting for arrival.");
        Assert.IsFalse(FindNamed(popup.transform,"ShowMeButton").GetComponent<UnityEngine.UI.Button>().interactable);
        gateway.HasTutorialTarget=false; helper.TickHighlight(104); Assert.IsFalse(view.ShowMeButton.interactable);
        ui.Dispose();
    }

    [TestCase(false)] [TestCase(true)]
    public void EveryEnabledMissionShowMeHasATarget(bool extraction)
    {
        CreateHudHarness(true,out var overlay,out var header,out _);
        var gateway=new FakeAssistantPanelGateway(CreateStructuredModel(1),UiAssistantHighlightModel.Empty) {
            HasTutorialTarget=true, Extraction=extraction, TutorialTarget=new UiMissionTutorialTarget(Vector3.one,Vector3.right*20,true,false) };
        UiShellRuntimeGateway.Register(gateway);
        var ui=new MainMenuPlayUI(); ui.Init(null,new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject,overlay,LoadPopupPrefab());
        ui.BindMatchHudCommandControls(CreateCommandControls(overlay));
        var helper=GetPrivateField<MatchHudAssistantUiSystemHelper>(ui,"_matchHudAssistantUiSystem");
        var view=header.Find("AriaAssistantButton").GetComponent<AriaTutorialBriefingView>();
        var highlight=GetPrivateField<AssistantHighlightPresentationSystemHelper>(helper,"_highlightPresentationSystem");
        for(byte step=1;step<=12;step++)
        {
            helper.ApplyReadModel(CreateStructuredModel((uint)(100+step),recommendationKind:9,recommendationTargetKind:4,tutorialStep:step,tutorialStepCount:12));
            helper.TickHighlight(100+step);
            if(!view.ShowMeButton.interactable) continue;
            view.ShowMeButton.onClick.Invoke();
            Assert.IsTrue(highlight.HasDirectTutorialTarget,"Enabled Show Me was a no-op at lesson "+step);
        }
        ui.Dispose();
    }

    [Test]
    public void M4UsesLiveSelectionModeAndHidesShowMeForVisibleIndicators()
    {
        CreateHudHarness(true,out var overlay,out var header,out _);
        var model=CreateStructuredModel(1,recommendationKind:9,recommendationTargetKind:4,tutorialStep:4,tutorialStepCount:12);
        var gateway=new FakeAssistantPanelGateway(model,UiAssistantHighlightModel.Empty) {HasTutorialTarget=true,Extraction=true,
            HasCommandState=true,CommandMode=TacticalCommandMode.Move,TutorialTarget=new UiMissionTutorialTarget(Vector3.zero,Vector3.right*10,true,false,4)};
        UiShellRuntimeGateway.Register(gateway);
        var cameraObject=new GameObject("Tutorial visibility camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=20;camera.transform.position=new Vector3(0,100,0);camera.transform.LookAt(Vector3.zero);gateway.FocusCamera=camera;
        var ui=new MainMenuPlayUI();ui.Init(null,new FakeMatchRuntimeState());
        try
        {
            ui.BindMatchHudAssistant(header.gameObject,overlay,LoadPopupPrefab());
            var controls=CreateCommandControls(overlay);ui.BindMatchHudCommandControls(controls);
            var helper=GetPrivateField<MatchHudAssistantUiSystemHelper>(ui,"_matchHudAssistantUiSystem");helper.BindWorldCamera(camera);
            var view=header.Find("AriaAssistantButton").GetComponent<AriaTutorialBriefingView>();
            var highlight=GetPrivateField<AssistantHighlightPresentationSystemHelper>(helper,"_highlightPresentationSystem");
            helper.ApplyReadModel(model);helper.TickHighlight(1);
            Assert.AreSame(controls.SelectButton.transform,GetPrivateField<RectTransform>(highlight,"_directTutorialTarget"));
            Assert.IsFalse(view.ShowMeButton.gameObject.activeSelf,"An already visible Select cue must hide Show Me.");
            gateway.CommandMode=TacticalCommandMode.Select;helper.TickHighlight(2);
            Assert.IsNull(GetPrivateField<RectTransform>(highlight,"_directTutorialTarget"),"Accepted Select must replace its stale UI cue with a world target.");
            Assert.IsTrue(highlight.HasVisibleDirectTutorialTarget);
            Assert.AreEqual((int)UnityEngine.Rendering.CompareFunction.Always,GetPrivateField<Material>(highlight,"_worldRingMaterial").GetInt("_ZTest"));
            Assert.IsFalse(view.ShowMeButton.gameObject.activeSelf);
            Assert.IsFalse(view.DoItButton.gameObject.activeSelf,"Do It must not toggle Select off while waiting for a drag gesture.");
            camera.transform.position+=Vector3.right*1000;helper.TickHighlight(3);
            Assert.IsTrue(view.ShowMeButton.gameObject.activeSelf,"Offscreen world target is a useful Show Me action.");
            gateway.FocusCamera=null; // Accepted camera request completes on a later frame.
            view.ShowMeButton.onClick.Invoke();
            Assert.IsFalse(view.ShowMeButton.gameObject.activeSelf,"Hide immediately while the accepted camera move is in flight.");
            camera.transform.position-=Vector3.right*1000;helper.TickHighlight(4);
            Assert.IsFalse(view.ShowMeButton.gameObject.activeSelf,"Show Me disappears after revealing its target.");
            gateway.TutorialTarget=new UiMissionTutorialTarget(Vector3.zero,Vector3.right*10,false,false,4);helper.TickHighlight(5);
            Assert.IsFalse(highlight.HasDirectTutorialTarget,"A completed selection must clear its cue while the next lesson is projected.");
            Assert.AreEqual((int)UnityEngine.Rendering.CompareFunction.LessEqual,GetPrivateField<Material>(highlight,"_worldRingMaterial").GetInt("_ZTest"));
        }
        finally {ui.Dispose();UnityEngine.Object.DestroyImmediate(cameraObject);}
    }

    [Test]
    public void M4GuidesRealSelectionBeforeBoardingOrMoving()
    {
        CreateHudHarness(true,out var overlay,out var header,out _);
        var model=CreateStructuredModel(1,recommendationKind:9,recommendationTargetKind:4,tutorialStep:9,tutorialStepCount:12);
        var gateway=new FakeAssistantPanelGateway(model,UiAssistantHighlightModel.Empty){HasTutorialTarget=true,Extraction=true,
            HasCommandState=true,CommandMode=TacticalCommandMode.Move,TutorialTarget=new UiMissionTutorialTarget(Vector3.zero,Vector3.one,true,false,4)};
        UiShellRuntimeGateway.Register(gateway);
        var ui=new MainMenuPlayUI();ui.Init(null,new FakeMatchRuntimeState());
        try
        {
            ui.BindMatchHudAssistant(header.gameObject,overlay,LoadPopupPrefab());
            var controls=CreateCommandControls(overlay);ui.BindMatchHudCommandControls(controls);
            int selections=0,moves=0;
            controls.SelectButton.onClick.AddListener(()=>selections++);
            controls.MoveButton.onClick.AddListener(()=>moves++);
            var helper=GetPrivateField<MatchHudAssistantUiSystemHelper>(ui,"_matchHudAssistantUiSystem");
            var view=header.Find("AriaAssistantButton").GetComponent<AriaTutorialBriefingView>();
            helper.ApplyReadModel(model);helper.TickHighlight(1);
            Assert.IsFalse(view.DoItButton.gameObject.activeInHierarchy);
            controls.SelectButton.onClick.Invoke();
            Assert.AreEqual(1,selections,"The highlighted Select control activates selection before a boarding order.");
            Assert.AreEqual(0,moves);
            gateway.CommandMode=TacticalCommandMode.Select;helper.TickHighlight(2);
            Assert.IsFalse(view.DoItButton.gameObject.activeSelf,"Dragging is the next action; Do It must not toggle selection off.");
        }
        finally {ui.Dispose();}
    }

    private sealed partial class FakeAssistantPanelGateway : IUiMissionTutorialTargetGateway,IUiMissionTutorialFocusGateway,IUiMissionExtractionGateway
    {
        public bool HasTutorialTarget, Extraction, HasCommandState;
        public TacticalCommandMode CommandMode;
        public Camera FocusCamera;
        public UiMissionTutorialTarget TutorialTarget;
        public Vector3 LastTutorialFocus;
        public bool TryReadMissionTutorialTarget(out UiMissionTutorialTarget target) {target=TutorialTarget;return HasTutorialTarget;}
        public bool TryFocusMissionTutorialTarget(bool selection) {LastTutorialFocus=selection?TutorialTarget.Selection:TutorialTarget.Destination;if(FocusCamera!=null){FocusCamera.transform.position=LastTutorialFocus+Vector3.up*100;FocusCamera.transform.LookAt(LastTutorialFocus);}return HasTutorialTarget;}
        public bool TryReadMissionExtraction(out UiMissionExtractionModel model) {model=default;return Extraction;}
        public bool TryRequestExtractionAction(UiMissionExtractionAction action) => Extraction;
        public bool IsExtractionGuideContext() => Extraction;
    }
}
