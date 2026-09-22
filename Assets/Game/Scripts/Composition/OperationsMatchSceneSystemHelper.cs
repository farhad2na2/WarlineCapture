using System;
using System.IO;
using Game.Operations.Capture;
using Game.Operations.Content;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Composition
{
    /// <summary>
    /// Binds the shipping Ops dashboard, district raid confirm, and match command
    /// buttons to operation.o001. The authored session runs inside MatchSceneView.
    /// There is no shipping Old Quarter art asset in this slice.
    /// </summary>
    public sealed class OperationsMatchSceneSystemHelper : MonoBehaviour
    {
        public const string ProfileFolderName = "OperationsO001Player";
        public const string AriaFlagName = "aria-regular-en.txt";

        OperationsO001PlayerShell _shell;
        string _directory;
        OperationsDashboardScreenView _dashboard;
        DistrictDetailActionsScreenView _district;
        OperationsTacticalWorldShell _world;
        Button _continueButton;
        float _nextStep;
        bool _loggedMissingDistrict;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (UnityEngine.Object.FindAnyObjectByType<OperationsMatchSceneSystemHelper>() != null)
                return;
            var host = new GameObject("OperationsMatchScene");
            DontDestroyOnLoad(host);
            host.AddComponent<OperationsMatchSceneSystemHelper>();
        }

        void OnEnable()
        {
            OperationsShippingInputGate.TryConsume = ConsumeShippingControl;
        }

        void OnDisable()
        {
            if (OperationsShippingInputGate.TryConsume == ConsumeShippingControl)
                OperationsShippingInputGate.TryConsume = null;
            if (_district != null)
                _district.ActionRequested -= OnDistrictAction;
        }

        void Start()
        {
            _directory = Path.Combine(Application.persistentDataPath, ProfileFolderName);
            RestoreEnvelope();
            _shell = OperationsO001PlayerShell.Bind(_directory, OperationsO001PlayerShell.RegularEnSeed);
            _nextStep = Time.unscaledTime + 1f;
        }

        void Update()
        {
            if (_shell == null)
                return;
            BindDashboard();
            BindDistrict();
            PresentWorld();
            PresentContinue();
            StepClock();
        }

        bool ConsumeShippingControl(string shippingControl)
        {
            OperationsPlayerShellFrame frame = _shell.Read();
            bool ownsAttempt = frame.HasSharedLaunchRequest && frame.InvokesSharedSceneView;
            if (!ownsAttempt)
                return false;
            if (OperationsMatchVisibleControls.Press(
                    _shell,
                    shippingControl,
                    string.Empty,
                    string.Empty,
                    string.Empty))
            {
                PublishEnvelope();
                ReturnToOpsIfSettled();
            }

            return true;
        }

        void BindDashboard()
        {
            OperationsDashboardScreenView dashboard = UnityEngine.Object.FindAnyObjectByType<OperationsDashboardScreenView>();
            if (dashboard == null || dashboard == _dashboard)
                return;
            Button[] buttons = dashboard.DistrictButtons;
            if (buttons == null || buttons.Length == 0 || buttons[0] == null)
            {
                if (!_loggedMissingDistrict)
                {
                    _loggedMissingDistrict = true;
                    Debug.LogError("[OperationsSharedLaunch] The Ops dashboard has no district button for D01.");
                }

                return;
            }

            buttons[0].onClick.AddListener(OnDistrictPressed);
            _dashboard = dashboard;
        }

        void BindDistrict()
        {
            DistrictDetailActionsScreenView district = UnityEngine.Object.FindAnyObjectByType<DistrictDetailActionsScreenView>();
            if (district == null || district == _district)
                return;
            if (_district != null)
                _district.ActionRequested -= OnDistrictAction;
            district.ActionRequested += OnDistrictAction;
            _district = district;
        }

        void OnDistrictPressed()
        {
            if (_shell == null)
                return;
            if (!OperationsMatchVisibleControls.Press(
                    _shell,
                    OperationsMatchVisibleControls.District,
                    string.Empty,
                    string.Empty,
                    string.Empty))
                return;
            PublishEnvelope();
            UiShellRuntimeGateway.TryEnqueueRouteRequest(
                UiShellRouteIntent.OpenMenuRoute,
                UIRoute.DistrictDetail,
                true);
        }

        void OnDistrictAction(DistrictOperationActionKind action)
        {
            if (action != DistrictOperationActionKind.Raid || _shell == null)
                return;
            if (!OperationsMatchVisibleControls.Press(
                    _shell,
                    OperationsMatchVisibleControls.Raid,
                    string.Empty,
                    string.Empty,
                    string.Empty))
                return;
            PublishEnvelope();
            OperationsPlayerShellFrame frame = _shell.Read();
            if (!frame.InvokesSharedSceneView || !frame.HasMission)
                return;
            MatchSceneView scene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            if (scene != null)
                scene.AcceptOperationsSharedLaunch(frame.MissionId, frame.ScenarioId, frame.MapId);
            else
                QueueLaunch(frame);
            UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.EnterMatch, UIRoute.Match, false);
        }

        static void QueueLaunch(OperationsPlayerShellFrame frame)
        {
            MatchSceneView.PendingOperationsSharedLaunch = true;
            MatchSceneView.PendingOperationsMissionId = frame.MissionId;
            MatchSceneView.PendingOperationsScenarioId = frame.ScenarioId;
            MatchSceneView.PendingOperationsMapId = frame.MapId;
        }

        void PresentWorld()
        {
            OperationsPlayerShellFrame frame = _shell.Read();
            MatchSceneView scene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            if (scene == null || !frame.HasMission)
            {
                if (_world != null)
                    _world.Clear();
                return;
            }

            if (MatchSceneView.PendingOperationsSharedLaunch)
                scene.AcceptOperationsSharedLaunch(frame.MissionId, frame.ScenarioId, frame.MapId);
            _world = OperationsTacticalWorldShell.Ensure(scene.transform);
            _world.Sync(_shell.Session, _shell.SelectedUnitId, true);
            _world.PlaceInFrontOf(scene.WorldCamera);
        }

        void PresentContinue()
        {
            if (!ContinueEnabled())
            {
                if (_continueButton != null)
                    Destroy(_continueButton.gameObject);
                _continueButton = null;
                return;
            }

            if (_continueButton != null)
                return;
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;
            var buttonObject = new GameObject("OperationsContinue", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvas.transform, false);
            buttonObject.transform.SetAsLastSibling();
            var rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(280f, 64f);
            rect.anchoredPosition = new Vector2(0f, 48f);
            _continueButton = buttonObject.GetComponent<Button>();
            _continueButton.onClick.AddListener(OnContinuePressed);
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var label = labelObject.AddComponent<Text>();
            label.text = "Continue";
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.black;
            label.font = BuiltinFont();
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        void OnContinuePressed()
        {
            if (_shell == null)
                return;
            if (!OperationsMatchVisibleControls.Press(
                    _shell,
                    OperationsMatchVisibleControls.Continue,
                    string.Empty,
                    string.Empty,
                    string.Empty))
                return;
            PublishEnvelope();
            ReturnToOpsIfSettled();
        }

        void StepClock()
        {
            if (Time.unscaledTime < _nextStep)
                return;
            _nextStep = Time.unscaledTime + 1f;
            if (AriaArmed())
            {
                OperationsVisibleStepKind kind = OperationsAriaVisibleControls.Step(_shell, out string detail);
                PublishEnvelope();
                if (kind == OperationsVisibleStepKind.Stuck)
                    Debug.LogWarning("[OperationsSharedLaunch] Aria stopped. " + detail);
                if (kind == OperationsVisibleStepKind.Victory)
                    ClearAriaFlag();
                return;
            }

            OperationsPlayerShellFrame frame = _shell.Read();
            if (!frame.HasMission || frame.Phase != OperationsLoopPhase.Active || frame.Terminal)
                return;
            if (OperationsMatchVisibleControls.Press(
                    _shell,
                    OperationsMatchVisibleControls.Wait,
                    string.Empty,
                    string.Empty,
                    string.Empty))
                PublishEnvelope();
        }

        void ReturnToOpsIfSettled()
        {
            OperationsPlayerShellFrame frame = _shell.Read();
            if (!frame.O001Victory || !frame.ReturnAcknowledged || frame.Phase != OperationsLoopPhase.Dashboard)
                return;
            MatchSceneView scene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            if (scene != null)
                scene.ClearOperationsSharedLaunch();
            if (_world != null)
                _world.Clear();
            UiShellRuntimeGateway.TryEnqueueRouteRequest(
                UiShellRouteIntent.OpenMenuRoute,
                UIRoute.Operations,
                false);
        }

        static Font BuiltinFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                return font;
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        bool ContinueEnabled()
        {
            OperationsPlayerControl[] controls = _shell.Read().Controls ?? Array.Empty<OperationsPlayerControl>();
            for (int index = 0; index < controls.Length; index++)
            {
                if (controls[index].Enabled && controls[index].Id == OperationsO001PlayerShell.ContinueId)
                    return true;
            }

            return false;
        }

        bool AriaArmed()
        {
            return !string.IsNullOrEmpty(_directory) &&
                File.Exists(Path.Combine(_directory, AriaFlagName));
        }

        void ClearAriaFlag()
        {
            string path = Path.Combine(_directory, AriaFlagName);
            if (File.Exists(path))
                File.Delete(path);
        }

        void RestoreEnvelope()
        {
            SaveService service = SaveService.CreateDefault();
            PlayerProfileSaveData profile = service.LoadProfile();
            if (string.IsNullOrEmpty(profile.operationsEnvelope))
                return;
            OperationsShippingEnvelope.Unpack(profile.operationsEnvelope, _directory);
        }

        void PublishEnvelope()
        {
            if (!OperationsDurableProfile.Exists(_directory))
                return;
            SaveService service = SaveService.CreateDefault();
            PlayerProfileSaveData profile = service.LoadProfile();
            profile.operationsEnvelope = OperationsShippingEnvelope.Pack(_directory);
            service.SaveProfile(profile);
        }
    }
}
