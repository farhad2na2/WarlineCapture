using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class WarlineCaptureUiAssistantRuntimeBindingTests
{
    private const string AssistantPanelPrefabPath = "Assets/Game/Prefabs/UI/Components/PREFAB-05_AssistantPanel.prefab";
    private const string AssistantButtonPrefabPath = "Assets/Game/Prefabs/UI/Components/PREFAB-04_AssistantButton.prefab";
    private const string MatchOverlayPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab";
    private const string RuntimeBindingPath = "Assets/Game/Scripts/UI/Screens/AssistantRuntimeBinding.cs";

    [Test]
    public void AssistantRuntimeBinding_DisplaysLiveServicePresentationInsteadOfPlaceholder()
    {
        using RuntimeBindingFixture fixture = RuntimeBindingFixture.Create(_ => CreateObjectiveContext());

        AssistantPanelPresentationData presentation = fixture.Binding.RefreshNow();
        Assert.AreEqual(M01AssistantIds.ObjectivesRecommendationId, presentation.RecommendationId);
        Assert.AreEqual("Read the objective", presentation.Title);
        Assert.AreEqual(AssistantButtonVisualState.Recommendation, fixture.ButtonView.CurrentState);

        fixture.Controller.TogglePresentation();

        Assert.IsTrue(fixture.Controller.IsOpen);
        Assert.AreEqual("Read the objective", fixture.PanelView.RecommendationTitleText.text);
        Assert.AreEqual("Destroy the hostile patrol and keep the command squad alive.", fixture.PanelView.RecommendationBodyText.text);
        Assert.AreNotEqual("M01 placeholder", fixture.PanelView.ChipLabels[1].text);
    }

    [Test]
    public void AssistantRuntimeBinding_DrivesFiveButtonStatesFromTypedRuntimeContext()
    {
        AssistantContext context = CreateObjectiveContext();
        using RuntimeBindingFixture fixture = RuntimeBindingFixture.Create(_ => context);

        context = CreateInactiveContext();
        fixture.Binding.RefreshNow();
        Assert.AreEqual(AssistantButtonVisualState.Idle, fixture.ButtonView.CurrentState);

        context = CreateObjectiveContext();
        fixture.Binding.RefreshNow();
        Assert.AreEqual(AssistantButtonVisualState.Recommendation, fixture.ButtonView.CurrentState);

        context = CreateCriticalRecoveryContext();
        fixture.Binding.RefreshNow();
        Assert.AreEqual(AssistantButtonVisualState.Critical, fixture.ButtonView.CurrentState);

        context = CreateObjectiveContext();
        context.CurrentControlOwnerState = AssistantControlOwnerState.AssistantTakeover;
        fixture.Binding.RefreshNow();
        Assert.AreEqual(AssistantButtonVisualState.Takeover, fixture.ButtonView.CurrentState);

        context = CreateObjectiveContext();
        context.AssistantMuted = true;
        fixture.Binding.RefreshNow();
        Assert.AreEqual(AssistantButtonVisualState.Muted, fixture.ButtonView.CurrentState);
    }

    [Test]
    public void AssistantRuntimeBinding_RoutesShowDoAndStopThroughAssistantRuntimeBoundary()
    {
        using RuntimeBindingFixture fixture = RuntimeBindingFixture.Create(session =>
        {
            AssistantContext context = CreateSelectSquadContext();
            context.CurrentControlOwnerState = string.IsNullOrEmpty(session.ActivePreviewIntentId)
                ? AssistantControlOwnerState.Player
                : AssistantControlOwnerState.AssistantPreview;
            return context;
        });

        fixture.Binding.RefreshNow();
        fixture.Controller.TogglePresentation();

        Assert.IsTrue(fixture.PanelView.ShowMeButton.interactable);
        Assert.IsTrue(fixture.PanelView.DoItButton.interactable);
        Assert.IsFalse(fixture.PanelView.StopButton.interactable);

        fixture.PanelView.ShowMeButton.onClick.Invoke();

        Assert.AreEqual("show.select_squad", fixture.Service.SessionState.ActivePreviewIntentId);
        Assert.IsTrue(fixture.PanelView.StopButton.interactable);

        fixture.PanelView.DoItButton.onClick.Invoke();

        Assert.IsTrue(fixture.Binding.HasLastDoItResult);
        Assert.IsFalse(fixture.Binding.LastDoItResult.Accepted);
        Assert.AreEqual(TacticalCommandReasonCode.TargetNotAttackable, fixture.Binding.LastDoItResult.ReasonCode);

        fixture.PanelView.StopButton.onClick.Invoke();

        Assert.AreEqual(string.Empty, fixture.Service.SessionState.ActivePreviewIntentId);
        Assert.AreEqual(string.Empty, fixture.Service.SessionState.ActiveTakeoverIntentId);
    }

    [Test]
    public void AssistantRuntimeBinding_ShowsTakeoverOwnershipAndPlayerInputReleasesControl()
    {
        using RuntimeBindingFixture fixture = RuntimeBindingFixture.Create(session =>
        {
            AssistantContext context = CreateObjectiveContext();
            context.CurrentControlOwnerState = string.IsNullOrEmpty(session.ActiveTakeoverIntentId)
                ? AssistantControlOwnerState.Player
                : AssistantControlOwnerState.AssistantTakeover;
            return context;
        });

        fixture.Service.SessionState.SetActiveTakeover("do.test.takeover");
        fixture.Binding.RefreshNow();
        fixture.Controller.TogglePresentation();

        Assert.AreEqual(AssistantButtonVisualState.Takeover, fixture.ButtonView.CurrentState);
        Assert.AreEqual("ARIA CONTROL ACTIVE - PLAYER INPUT OVERRIDES", fixture.PanelView.StatusText.text);
        Assert.IsTrue(fixture.PanelView.StopButton.interactable);

        Assert.IsTrue(fixture.Binding.NotifyPlayerInputOutsideAssistant());

        Assert.IsTrue(fixture.Binding.LastPlayerInputOverrideHandled);
        Assert.AreEqual(string.Empty, fixture.Service.SessionState.ActiveTakeoverIntentId);
        Assert.AreEqual(string.Empty, fixture.Service.SessionState.ActivePreviewIntentId);
        Assert.AreEqual(AssistantButtonVisualState.Recommendation, fixture.ButtonView.CurrentState);
        Assert.AreEqual("ADAPTIVE RESPONSE INTELLIGENCE ASSISTANT", fixture.PanelView.StatusText.text);
    }

    [Test]
    public void AssistantRuntimeBinding_ResultExplainStopDismissesAssistantOnlyAndLeavesResultPopupOpen()
    {
        GameObject flowHost = new("ResultFlowHost");
        GameObject popup = new("MissionResultPopup");
        try
        {
            WarlineCaptureMatchResultFlow resultFlow = flowHost.AddComponent<WarlineCaptureMatchResultFlow>();
            MissionResultPopupController popupController = popup.AddComponent<MissionResultPopupController>();
            popup.SetActive(true);
            SetPrivateField(resultFlow, "_activePopup", popupController);

            using RuntimeBindingFixture fixture = RuntimeBindingFixture.Create(_ => CreateResultExplainContext());
            fixture.Binding.RefreshNow();
            fixture.Controller.TogglePresentation();

            Assert.AreEqual(M01AssistantIds.ResultExplainRecommendationId, fixture.Controller.ActiveRecommendationId);
            Assert.IsTrue(fixture.PanelView.ShowMeButton.interactable);
            Assert.IsFalse(fixture.PanelView.DoItButton.interactable);
            Assert.IsFalse(fixture.PanelView.StopButton.interactable);

            fixture.PanelView.ShowMeButton.onClick.Invoke();

            Assert.AreEqual("show.result_popup", fixture.Service.SessionState.ActivePreviewIntentId);
            Assert.IsTrue(fixture.PanelView.StopButton.interactable);

            fixture.PanelView.StopButton.onClick.Invoke();

            Assert.IsTrue(resultFlow.HasActivePopup, "ARIA Stop must not close or acknowledge POP-05_MissionResult.");
            Assert.IsFalse(fixture.Controller.IsOpen, "Result Stop dismisses the assistant explanation panel only.");
            Assert.IsTrue(fixture.Service.SessionState.IsRecommendationDismissed(M01AssistantIds.ResultExplainRecommendationId));
            Assert.AreEqual(string.Empty, fixture.Service.SessionState.ActivePreviewIntentId);
            Assert.AreEqual(string.Empty, fixture.Service.SessionState.ActiveTakeoverIntentId);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(popup);
            UnityEngine.Object.DestroyImmediate(flowHost);
        }
    }

    [Test]
    public void MatchOverlay_MountsAssistantRuntimeBindingToAcceptedButtonAndPanel()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchOverlayPrefabPath);
        Assert.NotNull(prefab);

        AssistantRuntimeBinding binding = prefab.GetComponent<AssistantRuntimeBinding>();
        AssistantPanelController controller = prefab.GetComponent<AssistantPanelController>();
        AssistantButtonView buttonView = prefab.transform.Find("AssistantLayer/AssistantEntryButton").GetComponent<AssistantButtonView>();

        Assert.NotNull(binding);
        Assert.AreSame(controller, binding.PanelController);
        Assert.AreSame(buttonView, binding.ButtonView);
    }

    [Test]
    public void AssistantRuntimeBinding_DoesNotUseScreenCoordinatesOrNamedUiExecution()
    {
        string source = File.ReadAllText(ResolveRepoFilePath(RuntimeBindingPath));

        StringAssert.DoesNotContain(".Find(", source);
        StringAssert.DoesNotContain("FindObject", source);
        StringAssert.DoesNotContain("Screen.", source);
        StringAssert.DoesNotContain("mousePosition", source);
        StringAssert.DoesNotContain("anchoredPosition", source);
        StringAssert.DoesNotContain("NameText", source);
        StringAssert.DoesNotContain("SelectedEntityPanel", source);
    }

    private static AssistantContext CreateInactiveContext()
    {
        return new AssistantContext
        {
            MissionId = string.Empty,
            LastCommandResultAccepted = true
        };
    }

    private static AssistantContext CreateObjectiveContext()
    {
        return CreateM01Context(objectiveVisible: true);
    }

    private static AssistantContext CreateSelectSquadContext()
    {
        AssistantContext context = CreateM01Context(objectiveVisible: false);
        context.CommandSquadSpawned = true;
        context.CommandSquadAlive = true;
        context.TypedCommandHooksAvailable = true;
        return context;
    }

    private static AssistantContext CreateCriticalRecoveryContext()
    {
        AssistantContext context = CreateSelectSquadContext();
        context.LastCommandResultAccepted = false;
        context.LastCommandReasonCode = TacticalCommandReasonCode.NoSelection;
        context.LastCommandReasonText = "Select a squad before issuing orders.";
        return context;
    }

    private static AssistantContext CreateResultExplainContext()
    {
        AssistantContext context = CreateM01Context(objectiveVisible: false);
        context.ResultPopupVisible = true;
        context.EnemyPatrolDestroyed = true;
        return context;
    }

    private static AssistantContext CreateM01Context(bool objectiveVisible)
    {
        return new AssistantContext
        {
            ActiveRoute = WarlineCaptureRoute.Match.ToString(),
            MissionId = M01AssistantIds.MissionId,
            ScenarioSetupId = M01AssistantIds.ScenarioSetupId,
            LevelId = M01AssistantIds.LevelId,
            IsoMapId = M01AssistantIds.IsoMapId,
            IsMatchOverlayActive = true,
            ObjectivePanelVisible = objectiveVisible,
            CommandSquadAlive = true,
            LastCommandResultAccepted = true,
            LastCommandReasonCode = TacticalCommandReasonCode.None
        };
    }

    private static string ResolveRepoFilePath(string relativePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        return string.IsNullOrEmpty(projectRoot)
            ? Path.GetFullPath(relativePath)
            : Path.Combine(projectRoot, relativePath);
    }

    private static void InvokeAwake(MonoBehaviour component)
    {
        MethodInfo awake = component.GetType().GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(awake);
        awake.Invoke(component, null);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, fieldName);
        field.SetValue(target, value);
    }

    private sealed class RuntimeBindingFixture : IDisposable
    {
        private readonly GameObject _host;
        private readonly GameObject _panelInstance;
        private readonly GameObject _buttonInstance;

        private RuntimeBindingFixture(
            GameObject host,
            GameObject panelInstance,
            GameObject buttonInstance,
            AssistantPanelController controller,
            AssistantPanelView panelView,
            AssistantButtonView buttonView,
            AssistantRuntimeBinding binding,
            WarlineCaptureAssistantService service)
        {
            _host = host;
            _panelInstance = panelInstance;
            _buttonInstance = buttonInstance;
            Controller = controller;
            PanelView = panelView;
            ButtonView = buttonView;
            Binding = binding;
            Service = service;
        }

        public AssistantPanelController Controller { get; }
        public AssistantPanelView PanelView { get; }
        public AssistantButtonView ButtonView { get; }
        public AssistantRuntimeBinding Binding { get; }
        public WarlineCaptureAssistantService Service { get; }

        public static RuntimeBindingFixture Create(Func<TutorialSessionState, AssistantContext> contextFactory)
        {
            GameObject host = new("AssistantRuntimeBindingHost");
            GameObject panelInstance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssistantPanelPrefabPath));
            GameObject buttonInstance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssistantButtonPrefabPath));

            AssistantPanelView panelView = panelInstance.GetComponent<AssistantPanelView>();
            AssistantButtonView buttonView = buttonInstance.GetComponent<AssistantButtonView>();
            AssistantPanelController controller = host.AddComponent<AssistantPanelController>();
            AssistantRuntimeBinding binding = host.AddComponent<AssistantRuntimeBinding>();
            WarlineCaptureAssistantService service = new(new M01AssistantRecommendationProvider(), new TutorialSessionState());

            controller.SetPanelViewForTests(panelView);
            controller.SetOpenButtonForTests(buttonView.Button);
            InvokeAwake(controller);
            binding.SetRuntimeForTests(
                controller,
                buttonView,
                service,
                contextFactory,
                new CommandIntentExecutor(service.SessionState));

            return new RuntimeBindingFixture(host, panelInstance, buttonInstance, controller, panelView, buttonView, binding, service);
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(_host);
            UnityEngine.Object.DestroyImmediate(_panelInstance);
            UnityEngine.Object.DestroyImmediate(_buttonInstance);
        }
    }
}
