using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MatchOverlayCommandInputSystem
{
    private readonly Dictionary<MatchOverlayCommandControlsView, Binding> _bindings = new();

    public void Bind(
        MatchOverlayCommandControlsView view,
        ISelectionUiCommand selectionUiCommandSystem,
        BattleHudRuntimeFeedbackView runtimeFeedbackView = null,
        Action showBuildDrawer = null,
        Action closeBuildDrawer = null,
        ISelectionDiagnosticsSink diagnosticsSink = null,
        ISelectionUiReadModel selectionUiReadModel = null)
    {
        if (view == null)
            return;

        Unbind(view);
        ResetCommandControlRuntimeListeners(view);

        var binding = new Binding(
            view,
            selectionUiCommandSystem,
            runtimeFeedbackView,
            showBuildDrawer,
            closeBuildDrawer,
            diagnosticsSink,
            selectionUiReadModel);
        binding.Bind();
        _bindings.Add(view, binding);
    }

    public void Unbind(MatchOverlayCommandControlsView view)
    {
        if (view == null || !_bindings.TryGetValue(view, out Binding binding))
            return;

        binding.Unbind();
        _bindings.Remove(view);
    }

    public void RefreshCommandControlState(ISelectionUiReadModel selectionUiReadModel = null)
    {
        foreach (Binding binding in _bindings.Values)
            binding.RefreshCommandControlState(selectionUiReadModel);
    }

    private static void ResetCommandControlRuntimeListeners(MatchOverlayCommandControlsView view)
    {
        ClearButtonListeners(view.SelectButton);
        ClearButtonListeners(view.MoveButton);
        ClearButtonListeners(view.AttackButton);
        ClearButtonListeners(view.ScanButton);
        ClearButtonListeners(view.BuildButton);
        ClearButtonListeners(view.HoldButton);
        ClearButtonListeners(view.StopButton);
        ClearButtonListeners(view.CommandWheelStopButton);

        MatchOverlayCommandTabView[] tabs = view.CommandTabGroup != null ? view.CommandTabGroup.Tabs : null;
        if (tabs == null)
            return;

        for (int i = 0; i < tabs.Length; i++)
            ClearButtonListeners(tabs[i]?.Button);
    }

    private static void ClearButtonListeners(Button button)
    {
        button?.onClick.RemoveAllListeners();
    }

    private sealed class Binding
    {
        private readonly MatchOverlayCommandControlsView _view;
        private readonly ISelectionUiCommand _selectionUiCommandSystem;
        private readonly BattleHudRuntimeFeedbackView _runtimeFeedbackView;
        private readonly Action _showBuildDrawer;
        private readonly Action _closeBuildDrawer;
        private readonly ISelectionDiagnosticsSink _diagnosticsSink;
        private readonly ISelectionUiReadModel _selectionUiReadModel;
        private readonly List<(Button Button, UnityEngine.Events.UnityAction Action)> _commandTabRuntimeListeners = new();
        private MatchOverlayCommandPointerProbe _pointerProbe;
        private bool _buildDrawerOpen;
        private bool? _lastLoggedScanCanScan;
        private TacticalCommandReasonCode _lastLoggedScanDisabledReason = TacticalCommandReasonCode.None;
        private int _lastScanButtonClickFrame = -1;
        private int _lastScanFallbackFrame = -1;

        public Binding(
            MatchOverlayCommandControlsView view,
            ISelectionUiCommand selectionUiCommandSystem,
            BattleHudRuntimeFeedbackView runtimeFeedbackView,
            Action showBuildDrawer,
            Action closeBuildDrawer,
            ISelectionDiagnosticsSink diagnosticsSink,
            ISelectionUiReadModel selectionUiReadModel)
        {
            _view = view;
            _selectionUiCommandSystem = selectionUiCommandSystem;
            _runtimeFeedbackView = runtimeFeedbackView;
            _showBuildDrawer = showBuildDrawer;
            _closeBuildDrawer = closeBuildDrawer;
            _diagnosticsSink = diagnosticsSink;
            _selectionUiReadModel = selectionUiReadModel;
        }

        public void Bind()
        {
            RepairScanButtonRaycastTarget();
            LogMoveCommandTrace(
                $"matchHudCommandControlsBind view={_view.name} " +
                $"select={DescribeButton(_view.SelectButton)} move={DescribeButton(_view.MoveButton)} " +
                $"attack={DescribeButton(_view.AttackButton)} scan={DescribeButton(_view.ScanButton)} " +
                $"build={DescribeButton(_view.BuildButton)} tabs={CountTabs(_view.CommandTabGroup)}");
            LogScanCommandTrace(
                $"commandControlsBind view={_view.name} scanButton={DescribeButton(_view.ScanButton)} " +
                $"hasCommandSystem={_selectionUiCommandSystem != null} hasReadModel={_selectionUiReadModel != null} frame={UnityEngine.Time.frameCount}");
            _runtimeFeedbackView?.BindFeedbackActionCallbacks(OnBoardAllFeedbackClicked, OnCancelFeedbackClicked);

            _view.SelectButton?.onClick.AddListener(OnSelectButtonClicked);
            _view.MoveButton?.onClick.AddListener(OnMoveButtonClicked);
            _view.AttackButton?.onClick.AddListener(OnAttackButtonClicked);
            _view.ScanButton?.onClick.AddListener(OnScanButtonClicked);
            _view.BuildButton?.onClick.AddListener(OnBuildButtonClicked);
            _view.HoldButton?.onClick.AddListener(OnHoldButtonClicked);
            _view.StopButton?.onClick.AddListener(OnStopButtonClicked);
            _view.CommandWheelStopButton?.onClick.AddListener(OnCommandWheelStopButtonClicked);
            BindCommandTabRuntimeDiagnosticsAndFallbacks();
            InstallPointerProbe();
            RefreshCommandControlState();
        }

        private static string DescribeButton(Button button)
        {
            return button != null ? button.name : "null";
        }

        private static int CountTabs(MatchOverlayCommandTabGroupView tabGroup)
        {
            if (tabGroup == null)
                return -1;

            MatchOverlayCommandTabView[] tabs = tabGroup.Tabs;
            return tabs != null ? tabs.Length : 0;
        }

        private void RepairScanButtonRaycastTarget()
        {
            Button button = _view.ScanButton;
            if (button == null)
                return;

            Graphic currentTarget = button.targetGraphic;
            bool targetBelongsToButton = currentTarget != null &&
                currentTarget.transform != null &&
                currentTarget.transform.IsChildOf(button.transform);
            if (targetBelongsToButton && currentTarget.raycastTarget)
                return;

            Image hitTarget = button.GetComponent<Image>();
            if (hitTarget == null)
                hitTarget = button.gameObject.AddComponent<Image>();

            hitTarget.color = new Color(0f, 0f, 0f, 0f);
            hitTarget.raycastTarget = true;
            button.targetGraphic = hitTarget;
            LogScanCommandTrace(
                $"scanButtonRaycastTargetRepaired view={_view.name} button={button.name} " +
                $"previous={DescribeGraphic(currentTarget)} new={DescribeGraphic(hitTarget)} frame={UnityEngine.Time.frameCount}");
        }

        private static string DescribeGraphic(Graphic graphic)
        {
            return graphic != null ? graphic.name : "null";
        }

        public void Unbind()
        {
            _runtimeFeedbackView?.ClearFeedbackActionCallbacks();

            _view.SelectButton?.onClick.RemoveListener(OnSelectButtonClicked);
            _view.MoveButton?.onClick.RemoveListener(OnMoveButtonClicked);
            _view.AttackButton?.onClick.RemoveListener(OnAttackButtonClicked);
            _view.ScanButton?.onClick.RemoveListener(OnScanButtonClicked);
            _view.BuildButton?.onClick.RemoveListener(OnBuildButtonClicked);
            _view.HoldButton?.onClick.RemoveListener(OnHoldButtonClicked);
            _view.StopButton?.onClick.RemoveListener(OnStopButtonClicked);
            _view.CommandWheelStopButton?.onClick.RemoveListener(OnCommandWheelStopButtonClicked);
            UnbindCommandTabRuntimeDiagnosticsAndFallbacks();
            UninstallPointerProbe();
        }

        private void OnSelectButtonClicked()
        {
            bool enterSelectionMode = !IsCommandModePresented(TacticalCommandMode.Select);
            bool queued = _selectionUiCommandSystem != null &&
                (enterSelectionMode
                    ? _selectionUiCommandSystem.RequestEnterSelectionMode()
                    : _selectionUiCommandSystem.RequestExitSelectionMode());

            if (!queued)
                BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Selection command unavailable."));
        }

        private void OnBuildButtonClicked()
        {
            if (_showBuildDrawer != null)
            {
                _showBuildDrawer.Invoke();
                _buildDrawerOpen = true;
                BattleHudRuntimeFeedbackBoundary.ApplyStickyCommandMode(_runtimeFeedbackView, TacticalCommandMode.Build);
                return;
            }

            BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                TacticalCommandReasonCode.BuildUnavailable,
                "Build drawer is not ready."));
        }

        private void OnMoveButtonClicked()
        {
            LogMoveCommandTrace(
                $"moveButtonClicked view={_view.name} hasSelectionUi={_selectionUiCommandSystem != null}");
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestMoveCommandMode();
            LogMoveCommandTrace($"moveButtonRequestMoveCommandMode queued={queued}");

            if (!queued)
                BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Move command unavailable."));
        }

        private void OnAttackButtonClicked()
        {
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestAttackCommandMode();

            if (!queued)
                BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Attack command unavailable."));
        }

        private void OnScanButtonClicked()
        {
            _lastScanButtonClickFrame = UnityEngine.Time.frameCount;
            LogScanCommandTrace(
                $"scanButtonClicked view={_view.name} buttonInteractable={(_view.ScanButton == null ? "null" : _view.ScanButton.interactable.ToString())} " +
                $"hasCommandSystem={_selectionUiCommandSystem != null} hasReadModel={_selectionUiReadModel != null} " +
                $"canScan={(_selectionUiReadModel == null ? "unknown" : _selectionUiReadModel.FocusedUnitCanScan.ToString())} " +
                $"reason={(_selectionUiReadModel == null ? TacticalCommandReasonCode.None : _selectionUiReadModel.FocusedUnitScanDisabledReason)} " +
                $"frame={UnityEngine.Time.frameCount}");

            if (!TryAcceptCapability(CommandCapability.Scan))
                return;

            CloseBuildDrawerIfOpen();
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestScanCommandMode();
            LogScanCommandTrace($"scanButtonRequestScanCommandMode queued={queued} frame={UnityEngine.Time.frameCount}");

            if (!queued)
                BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Scan command unavailable."));
        }

        private void InstallPointerProbe()
        {
            _pointerProbe = _view.GetComponent<MatchOverlayCommandPointerProbe>();
            if (_pointerProbe == null)
                _pointerProbe = _view.gameObject.AddComponent<MatchOverlayCommandPointerProbe>();

            _pointerProbe.Configure(
                _view,
                () => _lastScanButtonClickFrame,
                OnScanPointerFallback);
        }

        private void UninstallPointerProbe()
        {
            if (_pointerProbe == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(_pointerProbe);
            else
                UnityEngine.Object.DestroyImmediate(_pointerProbe);
            _pointerProbe = null;
        }

        private void OnScanPointerFallback()
        {
            if (_lastScanFallbackFrame == UnityEngine.Time.frameCount)
                return;

            _lastScanFallbackFrame = UnityEngine.Time.frameCount;
            LogScanCommandTrace($"scanPointerFallbackInvoked view={_view.name} frame={UnityEngine.Time.frameCount}");
            OnScanButtonClicked();
        }

        private void BindCommandTabRuntimeDiagnosticsAndFallbacks()
        {
            MatchOverlayCommandTabView[] tabs = _view.CommandTabGroup != null ? _view.CommandTabGroup.Tabs : null;
            if (tabs == null)
                return;

            for (int i = 0; i < tabs.Length; i++)
            {
                Button button = tabs[i]?.Button;
                if (button == null)
                    continue;

                Button capturedButton = button;
                bool scanAlias = IsScanAliasCommandButton(capturedButton);
                UnityEngine.Events.UnityAction action = () => OnCommandTabRuntimeClick(capturedButton, scanAlias);
                capturedButton.onClick.AddListener(action);
                _commandTabRuntimeListeners.Add((capturedButton, action));
                LogScanCommandTrace(
                    $"commandTabRuntimeBind index={i} name={capturedButton.name} known={IsKnownCommandButton(capturedButton)} scanAlias={scanAlias} frame={UnityEngine.Time.frameCount}");
            }
        }

        private void UnbindCommandTabRuntimeDiagnosticsAndFallbacks()
        {
            for (int i = 0; i < _commandTabRuntimeListeners.Count; i++)
                _commandTabRuntimeListeners[i].Button?.onClick.RemoveListener(_commandTabRuntimeListeners[i].Action);

            _commandTabRuntimeListeners.Clear();
        }

        private void OnCommandTabRuntimeClick(Button button, bool scanAlias)
        {
            LogScanCommandTrace(
                $"commandTabClicked name={DescribeButton(button)} known={IsKnownCommandButton(button)} scanAlias={scanAlias} frame={UnityEngine.Time.frameCount}");

            if (scanAlias)
                OnScanButtonClicked();
        }

        private bool IsKnownCommandButton(Button button)
        {
            return button != null &&
                   (button == _view.SelectButton ||
                    button == _view.MoveButton ||
                    button == _view.AttackButton ||
                    button == _view.ScanButton ||
                    button == _view.BuildButton ||
                    button == _view.HoldButton ||
                    button == _view.StopButton ||
                    button == _view.CommandWheelStopButton);
        }

        private bool IsScanAliasCommandButton(Button button)
        {
            return button != null &&
                   !IsKnownCommandButton(button) &&
                   string.Equals(button.name, "SupportCommand", StringComparison.OrdinalIgnoreCase);
        }

        private void CloseBuildDrawerIfOpen()
        {
            if (!_buildDrawerOpen)
                return;

            if (_closeBuildDrawer != null)
                _closeBuildDrawer.Invoke();

            _buildDrawerOpen = false;
            BattleHudRuntimeFeedbackBoundary.ClearStickyCommandMode(_runtimeFeedbackView, TacticalCommandMode.Build);
        }

        private bool IsCommandModePresented(TacticalCommandMode mode)
        {
            BattleHudRuntimeFeedbackState state = BattleHudRuntimeFeedbackBoundary.GetState(_runtimeFeedbackView);
            return state.CurrentCommandMode == mode ||
                state.StickyCommandMode == mode;
        }

        private void OnHoldButtonClicked()
        {
            if (!TryAcceptCapability(CommandCapability.Hold))
                return;

            _selectionUiCommandSystem?.RequestHoldPosition();
        }

        private void OnStopButtonClicked()
        {
            if (!TryAcceptCapability(CommandCapability.Stop))
                return;

            _selectionUiCommandSystem?.RequestStop();
        }

        private void OnCommandWheelStopButtonClicked()
        {
            if (!TryAcceptCapability(CommandCapability.Stop))
                return;

            _view.CommandWheelPanel?.Close();
            _selectionUiCommandSystem?.RequestStop();
        }

        public void RefreshCommandControlState(ISelectionUiReadModel selectionUiReadModel = null)
        {
            ISelectionUiReadModel readModel = selectionUiReadModel ?? _selectionUiReadModel;
            ApplyButtonInteractable(_view.HoldButton, readModel == null || readModel.FocusedUnitCanHold);
            ApplyButtonInteractable(_view.StopButton, readModel == null || readModel.FocusedUnitCanStop);
            ApplyButtonInteractable(_view.CommandWheelStopButton, readModel == null || readModel.FocusedUnitCanStop);
            // Keep Scan pressable so unavailable units surface an explicit rejection message.
            ApplyButtonInteractable(_view.ScanButton, true);

            bool canScan = readModel == null || readModel.FocusedUnitCanScan;
            TacticalCommandReasonCode reason = readModel == null
                ? TacticalCommandReasonCode.None
                : readModel.FocusedUnitScanDisabledReason;
            if (_lastLoggedScanCanScan != canScan || _lastLoggedScanDisabledReason != reason)
            {
                _lastLoggedScanCanScan = canScan;
                _lastLoggedScanDisabledReason = reason;
                LogScanCommandTrace(
                    $"scanButtonStateRefreshed view={_view.name} button={DescribeButton(_view.ScanButton)} " +
                    $"interactable={(_view.ScanButton != null && _view.ScanButton.interactable)} canScan={canScan} reason={reason} " +
                    $"hasReadModel={readModel != null} frame={UnityEngine.Time.frameCount}");
            }
        }

        private bool TryAcceptCapability(CommandCapability capability)
        {
            ISelectionUiReadModel readModel = _selectionUiReadModel;
            if (readModel == null)
                return true;

            bool accepted = capability switch
            {
                CommandCapability.Hold => readModel.FocusedUnitCanHold,
                CommandCapability.Stop => readModel.FocusedUnitCanStop,
                CommandCapability.Scan => readModel.FocusedUnitCanScan,
                _ => true
            };
            if (accepted)
            {
                if (capability == CommandCapability.Scan)
                    LogScanCommandTrace($"scanCapabilityAccepted frame={UnityEngine.Time.frameCount}");
                return true;
            }

            TacticalCommandReasonCode reason = capability switch
            {
                CommandCapability.Hold => readModel.FocusedUnitHoldDisabledReason,
                CommandCapability.Stop => readModel.FocusedUnitStopDisabledReason,
                CommandCapability.Scan => readModel.FocusedUnitScanDisabledReason,
                _ => TacticalCommandReasonCode.CommandUnavailable
            };
            if (capability == CommandCapability.Scan)
                LogScanCommandTrace($"scanCapabilityRejected reason={reason} frame={UnityEngine.Time.frameCount}");
            BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(
                _runtimeFeedbackView,
                TacticalCommandResult.Rejected(reason));
            return false;
        }

        private static void ApplyButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        private void OnBoardAllFeedbackClicked()
        {
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestBoardAllSelectedTransport();

            if (!queued)
                BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Board all unavailable."));
        }

        private void OnCancelFeedbackClicked()
        {
            bool queued = _selectionUiCommandSystem != null &&
                _selectionUiCommandSystem.RequestCancelActiveCommandMode();

            if (!queued)
                BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Cancel unavailable."));
        }

        private void LogMoveCommandTrace(string message)
        {
            _diagnosticsSink?.LogMoveCommandTrace(message);
        }

        private static void LogScanCommandTrace(string message)
        {
            UnityEngine.Debug.Log($"[ScanCommandTrace] {message}");
        }

        private enum CommandCapability
        {
            Hold,
            Stop,
            Scan
        }
    }
}

internal sealed class MatchOverlayCommandPointerProbe : MonoBehaviour
{
    private readonly List<RaycastResult> _raycastResults = new();
    private MatchOverlayCommandControlsView _view;
    private Func<int> _lastScanButtonClickFrame;
    private Action _scanFallback;
    private PointerEventData _pointerEventData;
    private bool _pointerDownInsideScan;

    public void Configure(
        MatchOverlayCommandControlsView view,
        Func<int> lastScanButtonClickFrame,
        Action scanFallback)
    {
        _view = view;
        _lastScanButtonClickFrame = lastScanButtonClickFrame;
        _scanFallback = scanFallback;
    }

    private void Update()
    {
        if (_view == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 position = Input.mousePosition;
            _pointerDownInsideScan = ContainsButton(_view.ScanButton, position);
            LogPointerProbe("down", position);
        }

        if (!Input.GetMouseButtonUp(0))
            return;

        Vector2 releasePosition = Input.mousePosition;
        bool releaseInsideScan = ContainsButton(_view.ScanButton, releasePosition);
        LogPointerProbe("up", releasePosition);
        if (_pointerDownInsideScan && releaseInsideScan)
            StartCoroutine(InvokeScanFallbackIfButtonDidNotFire(UnityEngine.Time.frameCount));

        _pointerDownInsideScan = false;
    }

    private IEnumerator InvokeScanFallbackIfButtonDidNotFire(int pointerFrame)
    {
        yield return null;

        int lastScanButtonClickFrame = _lastScanButtonClickFrame?.Invoke() ?? -1;
        if (lastScanButtonClickFrame >= pointerFrame)
            yield break;

        Debug.Log($"[ScanCommandTrace] commandPointerProbe scanFallbackEligible pointerFrame={pointerFrame} frame={UnityEngine.Time.frameCount}");
        _scanFallback?.Invoke();
    }

    private void LogPointerProbe(string phase, Vector2 position)
    {
        bool scanRect = ContainsButton(_view.ScanButton, position);
        bool holdRect = ContainsButton(_view.HoldButton, position);
        bool stopRect = ContainsButton(_view.StopButton, position);
        string tabHit = ResolveCommandTabHit(position);
        string topHit = ResolveTopRaycastHit(position, out string hitSummary);
        Debug.Log(
            $"[ScanCommandTrace] commandPointerProbe phase={phase} pos={position} " +
            $"tabHit={tabHit} scanRect={scanRect} holdRect={holdRect} stopRect={stopRect} " +
            $"topHit={topHit} hits={hitSummary} frame={UnityEngine.Time.frameCount}");
    }

    private string ResolveCommandTabHit(Vector2 position)
    {
        MatchOverlayCommandTabView[] tabs = _view.CommandTabGroup != null ? _view.CommandTabGroup.Tabs : null;
        if (tabs == null)
            return "NoTabs";

        for (int i = 0; i < tabs.Length; i++)
        {
            Button button = tabs[i]?.Button;
            if (ContainsButton(button, position))
                return button.name;
        }

        return "None";
    }

    private string ResolveTopRaycastHit(Vector2 position, out string hitSummary)
    {
        hitSummary = "None";
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return "NoEventSystem";

        _pointerEventData ??= new PointerEventData(eventSystem);
        _pointerEventData.position = position;
        _raycastResults.Clear();
        eventSystem.RaycastAll(_pointerEventData, _raycastResults);

        if (_raycastResults.Count == 0)
            return "None";

        int count = Mathf.Min(5, _raycastResults.Count);
        string[] names = new string[count];
        for (int i = 0; i < count; i++)
            names[i] = BuildPath(_raycastResults[i].gameObject.transform);

        hitSummary = string.Join(" | ", names);
        return names[0];
    }

    private static bool ContainsButton(Button button, Vector2 screenPosition)
    {
        RectTransform rect = button != null ? button.transform as RectTransform : null;
        if (rect == null || !button.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = button.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
    }

    private static string BuildPath(Transform transform)
    {
        if (transform == null)
            return "None";

        string path = transform.name;
        Transform current = transform.parent;
        int depth = 0;
        while (current != null && depth < 4)
        {
            path = current.name + "/" + path;
            current = current.parent;
            depth++;
        }

        return path;
    }
}
