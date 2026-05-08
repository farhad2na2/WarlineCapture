using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AssistantPanelController : MonoBehaviour
{
    [SerializeField] private AssistantPanelView panelPrefab;
    [SerializeField] private AssistantPanelView panelView;
    [SerializeField] private Transform panelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private bool hideOnAwake = true;

    private AssistantPanelView _registeredView;
    private AssistantPanelPresentationData _activeRecommendation;
    private Func<AssistantPanelPresentationData> _presentationProvider;

    public event Action<string> ShowMeRequested;
    public event Action<string> DoItRequested;
    public event Action<string> StopRequested;

    public AssistantPanelView PanelView => panelView;
    public AssistantPanelView PanelPrefab => panelPrefab;
    public Transform PanelRoot => panelRoot;
    public Button OpenButton => openButton;
    public bool IsOpen => panelView != null && panelView.gameObject.activeSelf;
    public string ActiveRecommendationId => _activeRecommendation.RecommendationId;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = transform;

        if (panelView != null)
            RegisterPanelView(panelView);

        if (openButton != null)
            openButton.onClick.AddListener(TogglePresentation);

        if (hideOnAwake)
            Hide();
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(TogglePresentation);

        UnregisterPanelView();
    }

    public void SetPanelPrefabForTests(AssistantPanelView prefab)
    {
        panelPrefab = prefab;
    }

    public void SetPanelViewForTests(AssistantPanelView view)
    {
        panelView = view;
        RegisterPanelView(panelView);
    }

    public void SetPanelRootForTests(Transform root)
    {
        panelRoot = root;
    }

    public void SetOpenButtonForTests(Button button)
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(TogglePresentation);

        openButton = button;

        if (openButton != null)
            openButton.onClick.AddListener(TogglePresentation);
    }

    public void SetPresentationProvider(Func<AssistantPanelPresentationData> provider)
    {
        _presentationProvider = provider;
    }

    public AssistantPanelView ShowCurrentPresentation()
    {
        return _presentationProvider != null
            ? ShowRecommendation(_presentationProvider())
            : ShowPlaceholder();
    }

    public AssistantPanelView ShowPlaceholder()
    {
        return ShowRecommendation(AssistantPanelPresentationData.CreatePlaceholder());
    }

    public void TogglePlaceholder()
    {
        if (IsOpen)
            Hide();
        else
            ShowPlaceholder();
    }

    public void TogglePresentation()
    {
        if (IsOpen)
            Hide();
        else
            ShowCurrentPresentation();
    }

    public AssistantPanelView ShowRecommendation(AssistantPanelPresentationData recommendation)
    {
        AssistantPanelView view = EnsurePanelView();
        if (view == null)
            return null;

        _activeRecommendation = recommendation;
        view.BindRecommendation(
            recommendation.Title,
            recommendation.Body,
            recommendation.Chips,
            recommendation.CanShow,
            recommendation.CanExecute,
            recommendation.CanStop);

        if (view.StatusText != null && !string.IsNullOrWhiteSpace(recommendation.StatusLabel))
            view.StatusText.text = recommendation.StatusLabel;

        view.gameObject.SetActive(true);
        return view;
    }

    public void Hide()
    {
        if (panelView != null)
            panelView.gameObject.SetActive(false);
    }

    private AssistantPanelView EnsurePanelView()
    {
        if (panelView == null && panelPrefab != null)
        {
            Transform root = panelRoot != null ? panelRoot : transform;
            panelView = Instantiate(panelPrefab, root, false);
            panelView.name = panelPrefab.name;
        }

        RegisterPanelView(panelView);
        return panelView;
    }

    private void RegisterPanelView(AssistantPanelView view)
    {
        if (_registeredView == view)
            return;

        UnregisterPanelView();
        _registeredView = view;

        if (_registeredView == null)
            return;

        if (_registeredView.ShowMeButton != null)
            _registeredView.ShowMeButton.onClick.AddListener(HandleShowMeRequested);

        if (_registeredView.DoItButton != null)
            _registeredView.DoItButton.onClick.AddListener(HandleDoItRequested);

        if (_registeredView.StopButton != null)
            _registeredView.StopButton.onClick.AddListener(HandleStopRequested);
    }

    private void UnregisterPanelView()
    {
        if (_registeredView == null)
            return;

        if (_registeredView.ShowMeButton != null)
            _registeredView.ShowMeButton.onClick.RemoveListener(HandleShowMeRequested);

        if (_registeredView.DoItButton != null)
            _registeredView.DoItButton.onClick.RemoveListener(HandleDoItRequested);

        if (_registeredView.StopButton != null)
            _registeredView.StopButton.onClick.RemoveListener(HandleStopRequested);

        _registeredView = null;
    }

    private void HandleShowMeRequested()
    {
        ShowMeRequested?.Invoke(_activeRecommendation.RecommendationId);
    }

    private void HandleDoItRequested()
    {
        DoItRequested?.Invoke(_activeRecommendation.RecommendationId);
    }

    private void HandleStopRequested()
    {
        StopRequested?.Invoke(_activeRecommendation.RecommendationId);
    }
}

public readonly struct AssistantPanelPresentationData
{
    public AssistantPanelPresentationData(
        string recommendationId,
        string title,
        string body,
        string[] chips,
        bool canShow,
        bool canExecute,
        bool canStop)
        : this(recommendationId, title, body, chips, canShow, canExecute, canStop, string.Empty)
    {
    }

    public AssistantPanelPresentationData(
        string recommendationId,
        string title,
        string body,
        string[] chips,
        bool canShow,
        bool canExecute,
        bool canStop,
        string statusLabel)
    {
        RecommendationId = recommendationId ?? string.Empty;
        Title = title ?? string.Empty;
        Body = body ?? string.Empty;
        Chips = chips ?? Array.Empty<string>();
        CanShow = canShow;
        CanExecute = canExecute;
        CanStop = canStop;
        StatusLabel = statusLabel ?? string.Empty;
    }

    public string RecommendationId { get; }
    public string Title { get; }
    public string Body { get; }
    public string[] Chips { get; }
    public bool CanShow { get; }
    public bool CanExecute { get; }
    public bool CanStop { get; }
    public string StatusLabel { get; }

    public AssistantPanelPresentationData WithStatusLabel(string statusLabel)
    {
        return new AssistantPanelPresentationData(
            RecommendationId,
            Title,
            Body,
            Chips,
            CanShow,
            CanExecute,
            CanStop,
            statusLabel);
    }

    public static AssistantPanelPresentationData CreatePlaceholder()
    {
        return new AssistantPanelPresentationData(
            "placeholder.ui.assistant_panel.presentation_shell",
            "Read the objective",
            "Destroy the hostile patrol and keep the command squad alive.",
            new[] { "Check objective tracker", "M01 placeholder" },
            canShow: true,
            canExecute: false,
            canStop: false);
    }
}
