using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed class MatchOverlayCommandControlsView : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private Button moveButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button scanButton;
        [SerializeField] private Button boardButton;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button holdButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button commandWheelStopButton;
        [SerializeField] private Image selectIcon;
        [SerializeField] private Image moveIcon;
        [SerializeField] private Image attackIcon;
        [SerializeField] private Image scanIcon;
        [SerializeField] private Image boardIcon;
        [SerializeField] private Image buildIcon;
        [SerializeField] private Image holdIcon;
        [SerializeField] private Image stopIcon;
        [SerializeField] private CommandWheelPanelView commandWheelPanel;
        [SerializeField] private MatchOverlayCommandTabGroupView commandTabGroup;

        private Canvas _cachedCanvas;

        public Button SelectButton => selectButton;
        public Button MoveButton => moveButton;
        public Button AttackButton => attackButton;
        public Button ScanButton => scanButton;
        public Button BoardButton => boardButton;
        public Button BuildButton => buildButton;
        public Button HoldButton => holdButton;
        public Button StopButton => stopButton;
        public Button CommandWheelStopButton => commandWheelStopButton;
        public Image SelectIcon => selectIcon;
        public Image MoveIcon => moveIcon;
        public Image AttackIcon => attackIcon;
        public Image ScanIcon => scanIcon;
        public Image BoardIcon => boardIcon;
        public Image BuildIcon => buildIcon;
        public Image HoldIcon => holdIcon;
        public Image StopIcon => stopIcon;
        public CommandWheelPanelView CommandWheelPanel => commandWheelPanel;
        public MatchOverlayCommandTabGroupView CommandTabGroup => commandTabGroup;

        private void OnEnable()
        {
            RefreshMissionRestrictions();
        }

        internal void RefreshMissionRestrictions()
        {
            bool buildDisabled = false;
            bool supportDisabled = false;
            if (UiShellRuntimeGateway.TryReadMissionHudRestrictions(
                    out UiMissionHudRestrictionsModel restrictions))
            {
                buildDisabled = restrictions.BuildingDisabled || restrictions.ProductionDisabled;
                supportDisabled = restrictions.AirDisabled || restrictions.TransportDisabled;
            }

            ApplyMissionRestrictionState(buildDisabled, supportDisabled);
        }

        public void ApplyMissionRestrictionState(bool buildDisabled, bool supportDisabled)
        {
            SetMissionRestricted(buildButton, buildDisabled);
            SetMissionRestricted(FindCommandTabButton("SupportCommand"), supportDisabled);
        }

        public Sprite ResolveCommandIconSprite(TacticalCommandMode mode)
        {
            Image image = mode switch
            {
                TacticalCommandMode.Select => selectIcon,
                TacticalCommandMode.Move => moveIcon,
                TacticalCommandMode.Attack => attackIcon,
                TacticalCommandMode.Hold => holdIcon,
                TacticalCommandMode.Stop => stopIcon,
                TacticalCommandMode.Build => buildIcon,
                TacticalCommandMode.Scan => scanIcon,
                TacticalCommandMode.Board => boardIcon,
                _ => null
            };

            return image != null ? image.sprite : null;
        }

        public void ApplyV3SelectedCommand(TacticalCommandMode mode)
        {
            SetV3Selected(selectButton, mode == TacticalCommandMode.Select);
            SetV3Selected(moveButton, mode == TacticalCommandMode.Move);
            SetV3Selected(attackButton, mode == TacticalCommandMode.Attack);
            SetV3Selected(holdButton, mode == TacticalCommandMode.Hold);
            SetV3Selected(stopButton, mode == TacticalCommandMode.Stop);
            SetV3Selected(scanButton, mode == TacticalCommandMode.Scan);
            SetV3Selected(boardButton, mode == TacticalCommandMode.Board);
            SetV3Selected(buildButton, mode == TacticalCommandMode.Build);
        }

        private void OnTransformParentChanged()
        {
            _cachedCanvas = null;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            Camera eventCamera = ResolveEventCamera();
            if (commandWheelPanel != null && commandWheelPanel.IsOpen)
                return true;

            return ContainsButton(selectButton, screenPosition, eventCamera) ||
                   ContainsButton(moveButton, screenPosition, eventCamera) ||
                   ContainsButton(attackButton, screenPosition, eventCamera) ||
                   ContainsButton(scanButton, screenPosition, eventCamera) ||
                   ContainsButton(boardButton, screenPosition, eventCamera) ||
                   ContainsButton(buildButton, screenPosition, eventCamera) ||
                   ContainsButton(holdButton, screenPosition, eventCamera) ||
                   ContainsButton(stopButton, screenPosition, eventCamera) ||
                   ContainsButton(commandWheelStopButton, screenPosition, eventCamera);
        }

        public string DescribeScreenPointHit(Vector2 screenPosition)
        {
            Camera eventCamera = ResolveEventCamera();
            if (ContainsButton(selectButton, screenPosition, eventCamera))
                return "SelectCommand";
            if (ContainsButton(moveButton, screenPosition, eventCamera))
                return "MoveCommand";
            if (ContainsButton(attackButton, screenPosition, eventCamera))
                return "AttackCommand";
            if (ContainsButton(scanButton, screenPosition, eventCamera))
                return "ScanCommand";
            if (ContainsButton(boardButton, screenPosition, eventCamera))
                return "BoardCommand";
            if (ContainsButton(buildButton, screenPosition, eventCamera))
                return "BuildCommand";
            if (ContainsButton(holdButton, screenPosition, eventCamera))
                return "HoldCommand";
            if (ContainsButton(stopButton, screenPosition, eventCamera))
                return "StopCommand";
            if (ContainsButton(commandWheelStopButton, screenPosition, eventCamera))
                return "CommandWheelStop";

            return commandWheelPanel != null && commandWheelPanel.IsOpen
                ? "CommandWheelOverlay"
                : "None";
        }

        private Camera ResolveEventCamera()
        {
            Canvas canvas = ResolveCanvas();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }

        private Canvas ResolveCanvas()
        {
            if (_cachedCanvas == null)
                _cachedCanvas = GetComponentInParent<Canvas>();
            return _cachedCanvas;
        }

        private static bool ContainsButton(Button button, Vector2 screenPosition, Camera eventCamera)
        {
            RectTransform rect = button != null ? button.transform as RectTransform : null;
            return rect != null &&
                   button.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
        }

        private static void SetV3Selected(Button button, bool selected)
        {
            if (button == null)
                return;

            Transform selectedVisual = button.transform.Find("V3SelectedState");
            if (selectedVisual != null && selectedVisual.gameObject.activeSelf != selected)
                selectedVisual.gameObject.SetActive(selected);
        }

        private Button FindCommandTabButton(string buttonName)
        {
            MatchOverlayCommandTabView[] tabs = commandTabGroup != null ? commandTabGroup.Tabs : null;
            if (tabs == null)
                return null;

            for (int index = 0; index < tabs.Length; index++)
            {
                Button button = tabs[index]?.Button;
                if (button != null && button.name == buttonName)
                    return button;
            }

            return null;
        }

        private static void SetMissionRestricted(Button button, bool disabled)
        {
            if (button == null)
                return;

            if (!button.gameObject.activeSelf)
                button.gameObject.SetActive(true);
            UiDisabledMaterialUtility.SetSelectableDisabled(
                button,
                UiDisabledVisualReason.MissionRestriction,
                disabled);
            UiDisabledMaterialUtility.SetDisabled(
                button.gameObject,
                UiDisabledVisualReason.MissionRestriction,
                disabled);
            button.interactable = !disabled;
        }
    }
}
