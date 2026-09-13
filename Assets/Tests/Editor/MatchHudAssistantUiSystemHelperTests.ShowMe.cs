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
        Debug.Log("[MissionShowMe] result=Passed tests=4 M3,M4=selection,command,destination,waiting,all-lessons");
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

    private sealed partial class FakeAssistantPanelGateway : IUiMissionTutorialTargetGateway,IUiMissionTutorialFocusGateway,IUiMissionExtractionGateway
    {
        public bool HasTutorialTarget, Extraction;
        public UiMissionTutorialTarget TutorialTarget;
        public Vector3 LastTutorialFocus;
        public bool TryReadMissionTutorialTarget(out UiMissionTutorialTarget target) {target=TutorialTarget;return HasTutorialTarget;}
        public bool TryFocusMissionTutorialTarget(bool selection) {LastTutorialFocus=selection?TutorialTarget.Selection:TutorialTarget.Destination;return HasTutorialTarget;}
        public bool TryReadMissionExtraction(out UiMissionExtractionModel model) {model=default;return Extraction;}
        public bool TryRequestExtractionAction(UiMissionExtractionAction action) => Extraction;
        public bool IsExtractionGuideContext() => Extraction;
    }
}
