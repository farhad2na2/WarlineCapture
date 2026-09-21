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
        RunCase(test => test.GroupSelectionExposesVisibleDragAndClearsWhenControlChanges());
        foreach (byte count in new byte[] { 5, 9, 12, 8, 10 })
            RunCase(test => test.CampaignActionsFollowLiveModeAndArrival(count));
        Debug.Log("[MissionShowMe] result=Passed tests=12 M1-M5,Gridlock=normal-selection,drag,command,destination,waiting");
    }

    [Test]
    public void GroupSelectionExposesVisibleDragAndClearsWhenControlChanges()
    {
        CreateHudHarness(true,out var overlay,out var header,out _);
        var model=CreateStructuredModel(1,recommendationKind:2,recommendationTargetKind:1,tutorialStep:1,tutorialStepCount:10);
        var gateway=new FakeAssistantPanelGateway(model,UiAssistantHighlightModel.Empty) {HasTutorialTarget=true,HasCommandState=true,
            CommandMode=TacticalCommandMode.Select,TutorialTarget=new UiMissionTutorialTarget(Vector3.zero,Vector3.right*10,true,false,4,
                dragSelection:true,selectionMin:new Vector3(-3,0,-3),selectionMax:new Vector3(3,2,3))};
        UiShellRuntimeGateway.Register(gateway);
        var cameraObject=new GameObject("Selection drag camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
        camera.pixelRect=new Rect(0,0,Screen.width,Screen.height);
        camera.orthographic=true;camera.orthographicSize=20;camera.transform.position=Vector3.up*100;camera.transform.LookAt(Vector3.zero);
        var ui=new MainMenuPlayUI();ui.Init(null,new FakeMatchRuntimeState());
        GameObject dragBlocker=null,selectionEvents=null;
        try
        {
            ui.BindMatchHudAssistant(header.gameObject,overlay,LoadPopupPrefab());ui.BindMatchHudCommandControls(CreateCommandControls(overlay));
            var helper=GetPrivateField<MatchHudAssistantUiSystemHelper>(ui,"_matchHudAssistantUiSystem");helper.BindWorldCamera(camera);
            var highlight=GetPrivateField<AssistantHighlightPresentationSystemHelper>(helper,"_highlightPresentationSystem");
            helper.ApplyReadModel(model);helper.TickHighlight(1);
            Assert.IsTrue(highlight.TryObserveVisibleSelectionDrag(out var start,out var end),$"screen={Screen.width}x{Screen.height} safe={Screen.safeArea} camera={camera.pixelRect} requested={GetPrivateField<bool>(highlight,"_selectionBoxRequested")} mode={GetPrivateField<TacticalCommandMode>(helper,"_activeCommandMode")}");
            Assert.Greater(end.x,start.x);Assert.Greater(end.y,start.y);
            Assert.IsTrue(Screen.safeArea.Contains(start)&&Screen.safeArea.Contains(end));
            Assert.AreEqual(0,gateway.GroupSelectionRequests);
            if(UnityEngine.EventSystems.EventSystem.current==null)
            {
                selectionEvents=new GameObject("Selection test events",typeof(UnityEngine.EventSystems.EventSystem));
                typeof(UnityEngine.EventSystems.EventSystem).GetMethod("OnEnable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                    .Invoke(selectionEvents.GetComponent<UnityEngine.EventSystems.EventSystem>(),null);
            }
            dragBlocker=new GameObject("Selection HUD obstruction",typeof(DragSelectionTestRaycaster));
            var blocker=dragBlocker.GetComponent<DragSelectionTestRaycaster>();
            blocker.Bounds=new Rect(start-Vector2.one*8,new Vector2(64,64));
            if(!UnityEngine.EventSystems.RaycasterManager.GetRaycasters().Contains(blocker))
                UnityEngine.EventSystems.RaycasterManager.GetRaycasters().Add(blocker);
            Assert.IsNotNull(UnityEngine.EventSystems.EventSystem.current,"Edit-mode fixture needs an active event system.");
            helper.TickHighlight(1.5f);
            Assert.IsFalse(highlight.TryObserveVisibleSelectionDrag(out _,out _),"HUD-covered drag endpoints are not actionable.");
            Assert.IsFalse(highlight.HasVisibleDirectTutorialTarget,"Show Me must remain available for a covered group.");
            UnityEngine.EventSystems.RaycasterManager.GetRaycasters().Remove(blocker);
            UnityEngine.Object.DestroyImmediate(dragBlocker);dragBlocker=null;
            gateway.CommandMode=TacticalCommandMode.Move;helper.TickHighlight(2);
            Assert.IsFalse(highlight.TryObserveVisibleSelectionDrag(out _,out _),"Changing controls clears the stale drag.");
        }
        finally
        {
            ui.Dispose();UnityEngine.Object.DestroyImmediate(cameraObject);
            if(dragBlocker!=null)
            {
                UnityEngine.EventSystems.RaycasterManager.GetRaycasters().Remove(dragBlocker.GetComponent<DragSelectionTestRaycaster>());
                UnityEngine.Object.DestroyImmediate(dragBlocker);
            }
            if(selectionEvents!=null)
            {
                typeof(UnityEngine.EventSystems.EventSystem).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                    .Invoke(selectionEvents.GetComponent<UnityEngine.EventSystems.EventSystem>(),null);
                UnityEngine.Object.DestroyImmediate(selectionEvents);
            }
        }
    }

    public sealed class DragSelectionTestRaycaster : UnityEngine.EventSystems.BaseRaycaster
    {
        public Rect Bounds;
        public override Camera eventCamera => null;
        public override void Raycast(UnityEngine.EventSystems.PointerEventData eventData,
            System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> results)
        {
            if(Bounds.Contains(eventData.position)) results.Add(new UnityEngine.EventSystems.RaycastResult
                {gameObject=gameObject,module=this,screenPosition=eventData.position,index=results.Count});
        }
    }

    [TestCase((byte)5)] [TestCase((byte)9)] [TestCase((byte)12)] [TestCase((byte)8)] [TestCase((byte)10)]
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
            Assert.IsNull(view.SelectionButton,"Tutorial must not create a substitute gameplay control.");
            Assert.IsNull(GetPrivateField<RectTransform>(highlight,"_directTutorialTarget"),"Select mode points to the actual unit in the world.");
            Assert.IsTrue(highlight.HasDirectTutorialTarget);
            Assert.AreEqual(0,gateway.GroupSelectionRequests,"Guidance must never issue group-selection commands.");
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
        var selectionHighlight=GetPrivateField<AssistantHighlightPresentationSystemHelper>(helper,"_highlightPresentationSystem");
        Assert.IsNull(view.SelectionButton);
        Assert.AreSame(controls.SelectButton.transform,GetPrivateField<RectTransform>(selectionHighlight,"_directTutorialTarget"));
        Assert.AreEqual(0,gateway.GroupSelectionRequests);
        helper.ApplyCommandMode(TacticalCommandMode.Select);helper.TickHighlight(100.5f);
        view.ShowMeButton.onClick.Invoke();
        Assert.AreEqual(gateway.TutorialTarget.Selection,gateway.LastTutorialFocus,"Show Me frames the real selection target.");
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
            Assert.IsFalse(view.ShowMeButton.gameObject.activeSelf);
            camera.transform.position+=Vector3.right*1000;helper.TickHighlight(2);
            Assert.AreSame(controls.SelectButton.transform,GetPrivateField<RectTransform>(highlight,"_directTutorialTarget"));
            gateway.CommandMode=TacticalCommandMode.Select;helper.TickHighlight(2.1f);
            Assert.IsTrue(view.ShowMeButton.gameObject.activeSelf,"Offscreen world targets offer camera guidance.");
            view.ShowMeButton.onClick.Invoke();helper.TickHighlight(2.2f);
            Assert.AreEqual(Vector3.zero,gateway.LastTutorialFocus);
            Assert.AreEqual(0,gateway.GroupSelectionRequests);
            gateway.TutorialTarget=new UiMissionTutorialTarget(Vector3.zero,Vector3.right*10,false,false,4);helper.TickHighlight(3);
            Assert.IsFalse(highlight.HasDirectTutorialTarget,"Completed selection clears the guide while the next lesson is projected.");
            Assert.IsNull(view.SelectionButton);

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
            var highlight=GetPrivateField<AssistantHighlightPresentationSystemHelper>(helper,"_highlightPresentationSystem");
            Assert.AreSame(controls.SelectButton.transform,GetPrivateField<RectTransform>(highlight,"_directTutorialTarget"));
            controls.SelectButton.onClick.Invoke();
            Assert.AreEqual(0,gateway.GroupSelectionRequests,"Tutorial cannot bypass the normal world selection gesture.");
            Assert.AreEqual(1,selections);Assert.AreEqual(0,moves);
            Assert.IsNull(view.SelectionButton);
            Assert.IsFalse(view.DoItButton.gameObject.activeSelf);

        }
        finally {ui.Dispose();}
    }

    private sealed partial class FakeAssistantPanelGateway : IUiMissionTutorialTargetGateway,IUiMissionTutorialFocusGateway,IUiMissionExtractionGateway
    {
        public bool HasTutorialTarget, Extraction, HasCommandState;
        public int GroupSelectionRequests;
        public bool TrySelectMissionTutorialGroup() {GroupSelectionRequests++;return HasTutorialTarget;}
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
