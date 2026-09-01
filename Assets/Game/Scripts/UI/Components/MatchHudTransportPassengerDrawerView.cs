using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MatchHudTransportPassengerDrawerView : MonoBehaviour
    {
        [SerializeField] private GameObject drawerRoot;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private TMP_Text emptyStateText;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private MatchHudTransportPassengerItemView itemTemplate;
        [SerializeField] private Button exitAllButton;
        [SerializeField] private TMP_Text exitAllLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text closeLabel;
        [SerializeField] private RectTransform capacitySlotsRoot;
        [SerializeField] private Button ropeDropButton;

        private readonly List<MatchHudTransportPassengerItemView> _runtimeItems = new();
        private Action _exitAllRequested;
        private Action _closeRequested;
        private Action<UiEntityHandle> _exitPassengerRequested;
        private Button _boundExitAllButton;
        private Button _boundCloseButton;
        private Button _boundRopeDropButton;
        private RectTransform _selectionPanel;
        private RectTransform _selectionFrame;
        private RectTransform _title;
        private RectTransform _subtitle;
        private RectTransform _portraitFrame;
        private RectTransform _healthPanel;
        private RectTransform _orderLabel;
        private RectTransform _playerControl;
        private RectTransform _commandButtons;
        private RectTransform _passengerChip;
        private Vector2 _widePanelSize;
        private Vector2 _wideTitleSize;
        private Vector2 _wideTitlePosition;
        private Vector2 _wideSubtitleSize;
        private Vector2 _wideSubtitlePosition;
        private Vector2 _widePortraitSize;
        private Vector2 _wideHealthSize;
        private Vector2 _wideOrderSize;
        private Vector2 _widePlayerControlSize;
        private Vector2 _wideCommandButtonsSize;
        private Vector2 _wideCommandButtonsPosition;
        private Vector2 _widePassengerChipSize;
        private Vector2 _widePassengerChipPosition;
        private Vector2 _wideHealthFrameSize;
        private Vector2 _wideHealthFillSize;
        private Vector2 _wideHealthTextPosition;
        private Vector2 _wideHealthTextSize;
        private Vector2 _wideCommandCellSize;
        private GridLayoutGroup.Constraint _wideCommandConstraint;
        private int _wideCommandConstraintCount;
        private float _wideTitleFontSize;
        private bool _widePlayerControlActive;
        private bool _wideReturnActive;
        private bool _wideDestroyActive;
        private bool _wideCameraActive;
        private bool _wideRopeDropActive;
        private RectTransform _returnButton;
        private RectTransform _destroyButton;
        private RectTransform _boardButton;
        private RectTransform _cameraButton;
        private bool _selectionLayoutCached;

        private void Awake()
        {
            BindUnityEvents();
            SetVisible(false);
        }

        private void OnDestroy()
        {
            ClearActions();
            UnbindButton(ref _boundExitAllButton, HandleExitAll);
            UnbindButton(ref _boundCloseButton, HandleClose);
            UnbindButton(ref _boundRopeDropButton, HandleExitAll);
        }

        public void BindActions(Action exitAllRequested, Action closeRequested, Action<UiEntityHandle> exitPassengerRequested)
        {
            BindUnityEvents();
            _exitAllRequested = exitAllRequested;
            _closeRequested = closeRequested;
            _exitPassengerRequested = exitPassengerRequested;
        }

        public void ClearActions()
        {
            _exitAllRequested = null;
            _closeRequested = null;
            _exitPassengerRequested = null;
        }

        public void Apply(MatchHudTransportPassengersModel model)
        {
            SetVisible(model.Visible && model.DrawerOpen);
            if (!model.Visible || !model.DrawerOpen)
                return;

            SetText(headerText, ResolveHeaderText(model));
            SetText(emptyStateText, "NO PASSENGERS ONBOARD");
            SetText(exitAllLabel, "EXIT ALL");
            SetText(closeLabel, "CLOSE");
            ApplyCapacitySlots(model.PassengerCount, model.Capacity);
            if (exitAllButton != null)
            {
                if (exitAllButton.interactable != model.ExitAllEnabled)
                    exitAllButton.interactable = model.ExitAllEnabled;
            }

            IReadOnlyList<MatchHudSelectionPanelPassengerItemModel> passengers = model.Passengers;
            int passengerCount = passengers?.Count ?? 0;
            if (emptyStateRoot != null && emptyStateRoot.activeSelf != (passengerCount == 0))
                emptyStateRoot.SetActive(passengerCount == 0);

            EnsureItemPool(passengerCount);
            for (int i = 0; i < _runtimeItems.Count; i++)
            {
                MatchHudTransportPassengerItemView item = _runtimeItems[i];
                bool active = i < passengerCount;
                if (item == null)
                    continue;

                if (item.gameObject.activeSelf != active)
                    item.gameObject.SetActive(active);
                if (active)
                {
                    item.gameObject.name = $"PassengerItemView - {passengers[i].DisplayName}";
                    item.Bind(passengers[i], _exitPassengerRequested);
                }
            }

            if (itemTemplate != null && passengerCount == 0 && itemTemplate.gameObject.activeSelf)
                itemTemplate.gameObject.SetActive(false);
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            RectTransform rect = drawerRoot != null
                ? drawerRoot.transform as RectTransform
                : transform as RectTransform;
            return rect != null &&
                   rect.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition);
        }

        private void SetVisible(bool visible)
        {
            if (visible)
                ApplySelectionLayout(true);
            else if (_selectionLayoutCached)
                ApplySelectionLayout(false);
            if (drawerRoot != null)
            {
                if (drawerRoot.activeSelf != visible)
                    drawerRoot.SetActive(visible);
            }
            else if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        private void EnsureItemPool(int count)
        {
            if (itemTemplate == null || contentRoot == null)
                return;

            if (!_runtimeItems.Contains(itemTemplate))
                _runtimeItems.Insert(0, itemTemplate);

            while (_runtimeItems.Count < count)
            {
                MatchHudTransportPassengerItemView item = Instantiate(itemTemplate, contentRoot, false);
                _runtimeItems.Add(item);
            }
        }

        private void BindUnityEvents()
        {
            BindButton(exitAllButton, ref _boundExitAllButton, HandleExitAll);
            BindButton(closeButton, ref _boundCloseButton, HandleClose);
            BindButton(ropeDropButton, ref _boundRopeDropButton, HandleExitAll);
        }

        private void HandleExitAll()
        {
            _exitAllRequested?.Invoke();
        }

        private void HandleClose()
        {
            _closeRequested?.Invoke();
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text == null)
                return;

            value ??= string.Empty;
            if (text.text != value)
                text.text = value;
        }

        private static string ResolveHeaderText(MatchHudTransportPassengersModel model)
        {
            if (model.SoldierCapacity > 0 && model.VehicleCapacity <= 0)
                return $"PASSENGERS {model.PassengerCount}/{model.Capacity} | SOLDIERS {model.SoldierPassengerCount}/{model.SoldierCapacity}";

            if (model.SoldierCapacity <= 0 && model.VehicleCapacity <= 0)
                return $"PASSENGERS {model.PassengerCount}/{model.Capacity}";

            return $"PASSENGERS {model.PassengerCount}/{model.Capacity} | SOLDIERS {model.SoldierPassengerCount}/{model.SoldierCapacity} | VEHICLES {model.VehiclePassengerCount}/{model.VehicleCapacity}";
        }

        private void ApplyCapacitySlots(int occupiedCount, int capacity)
        {
            if (capacitySlotsRoot == null)
                return;

            int visibleCapacity = Mathf.Clamp(capacity, 0, capacitySlotsRoot.childCount);
            int visibleOccupied = Mathf.Clamp(occupiedCount, 0, visibleCapacity);
            for (int i = 0; i < capacitySlotsRoot.childCount; i++)
            {
                Transform slot = capacitySlotsRoot.GetChild(i);
                bool visible = i < visibleCapacity;
                if (slot.gameObject.activeSelf != visible)
                    slot.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                V3GradientGraphic gradient = slot.GetComponentInChildren<V3GradientGraphic>(true);
                bool occupied = i < visibleOccupied;
                gradient?.Configure(
                    occupied ? new Color32(102, 207, 57, 255) : new Color32(20, 32, 34, 255),
                    occupied ? new Color32(42, 130, 20, 255) : new Color32(5, 12, 14, 255),
                    occupied ? new Color32(129, 232, 73, 255) : new Color32(72, 94, 99, 255),
                    2f);
            }
        }

        private void ApplySelectionLayout(bool compact)
        {
            CacheSelectionLayout();
            if (!_selectionLayoutCached)
                return;

            if (!compact)
            {
                _selectionPanel.sizeDelta = _widePanelSize;
                _title.sizeDelta = _wideTitleSize;
                _title.anchoredPosition = _wideTitlePosition;
                _subtitle.sizeDelta = _wideSubtitleSize;
                _subtitle.anchoredPosition = _wideSubtitlePosition;
                _portraitFrame.sizeDelta = _widePortraitSize;
                _healthPanel.sizeDelta = _wideHealthSize;
                _orderLabel.sizeDelta = _wideOrderSize;
                _playerControl.sizeDelta = _widePlayerControlSize;
                _commandButtons.sizeDelta = _wideCommandButtonsSize;
                _commandButtons.anchoredPosition = _wideCommandButtonsPosition;
                _passengerChip.sizeDelta = _widePassengerChipSize;
                _passengerChip.anchoredPosition = _widePassengerChipPosition;
                SetHealthLayout(_wideHealthFrameSize, _wideHealthFillSize, _wideHealthTextPosition, _wideHealthTextSize);
                GridLayoutGroup wideGrid = _commandButtons.GetComponent<GridLayoutGroup>();
                if (wideGrid != null)
                {
                    wideGrid.cellSize = _wideCommandCellSize;
                    wideGrid.constraint = _wideCommandConstraint;
                    wideGrid.constraintCount = _wideCommandConstraintCount;
                }
                TMP_Text wideTitle = _title.GetComponent<TMP_Text>();
                if (wideTitle != null)
                    wideTitle.fontSize = _wideTitleFontSize;
                _playerControl.gameObject.SetActive(_widePlayerControlActive);
                _returnButton.gameObject.SetActive(_wideReturnActive);
                _destroyButton.gameObject.SetActive(_wideDestroyActive);
                _cameraButton.gameObject.SetActive(_wideCameraActive);
                ApplyCompactCommandVisual(_boardButton, false);
                ApplyCompactCommandVisual(ropeDropButton != null ? ropeDropButton.transform as RectTransform : null, false);
                ApplyCompactPassengerChip(false);
                if (ropeDropButton != null)
                    ropeDropButton.gameObject.SetActive(_wideRopeDropActive);
                return;
            }

            _selectionPanel.sizeDelta = new Vector2(306f, _widePanelSize.y);
            _title.sizeDelta = new Vector2(272f, _wideTitleSize.y);
            _title.anchoredPosition = new Vector2(17f, _wideTitlePosition.y);
            _subtitle.sizeDelta = new Vector2(272f, _wideSubtitleSize.y);
            _subtitle.anchoredPosition = new Vector2(17f, _wideSubtitlePosition.y);
            _portraitFrame.sizeDelta = new Vector2(278f, _widePortraitSize.y);
            _healthPanel.sizeDelta = new Vector2(272f, _wideHealthSize.y);
            _orderLabel.sizeDelta = new Vector2(272f, _wideOrderSize.y);
            _playerControl.sizeDelta = new Vector2(272f, _widePlayerControlSize.y);
            _playerControl.gameObject.SetActive(false);
            _passengerChip.anchoredPosition = new Vector2(_widePassengerChipPosition.x, -337f);
            _passengerChip.sizeDelta = new Vector2(272f, 78f);
            _commandButtons.anchoredPosition = new Vector2(_wideCommandButtonsPosition.x, -427f);
            _commandButtons.sizeDelta = new Vector2(272f, 179f);
            SetHealthLayout(new Vector2(183f, 18f), new Vector2(170f, 10f), new Vector2(194f, 0f), new Vector2(60f, 31f));
            GridLayoutGroup compactGrid = _commandButtons.GetComponent<GridLayoutGroup>();
            if (compactGrid != null)
            {
                compactGrid.cellSize = new Vector2(272f, 84f);
                compactGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                compactGrid.constraintCount = 1;
            }
            _returnButton.gameObject.SetActive(false);
            _destroyButton.gameObject.SetActive(false);
            _cameraButton.gameObject.SetActive(false);
            if (ropeDropButton != null)
                ropeDropButton.gameObject.SetActive(true);
            ApplyCompactCommandVisual(_boardButton, true);
            ApplyCompactCommandVisual(ropeDropButton != null ? ropeDropButton.transform as RectTransform : null, true);
            ApplyCompactPassengerChip(true);
            TMP_Text titleText = _title.GetComponent<TMP_Text>();
            if (titleText != null)
                titleText.fontSize = 18.5f;
        }

        private void CacheSelectionLayout()
        {
            if (_selectionLayoutCached)
                return;

            _selectionFrame = transform.parent as RectTransform;
            _selectionPanel = _selectionFrame != null ? _selectionFrame.parent as RectTransform : null;
            if (_selectionFrame == null || _selectionPanel == null)
                return;

            _title = FindDirectChild(_selectionFrame, "Title");
            _subtitle = FindDirectChild(_selectionFrame, "Subtitle");
            _portraitFrame = FindDirectChild(_selectionFrame, "PortraitFrame");
            _healthPanel = FindDirectChild(_selectionFrame, "HealthPanel");
            _orderLabel = FindDirectChild(_selectionFrame, "OrderLabel");
            _playerControl = FindDirectChild(_selectionFrame, "V3PlayerControl");
            _commandButtons = FindDirectChild(_selectionFrame, "CommandButtons");
            _passengerChip = FindDirectChild(_selectionFrame, "PassengerChip");
            _returnButton = FindDirectChild(_commandButtons, "ReturnButton");
            _destroyButton = FindDirectChild(_commandButtons, "DestroyButton");
            _boardButton = FindDirectChild(_commandButtons, "BoardButton");
            _cameraButton = FindDirectChild(_commandButtons, "CameraButton");
            if (_title == null || _subtitle == null || _portraitFrame == null || _healthPanel == null ||
                _orderLabel == null || _playerControl == null || _commandButtons == null || _passengerChip == null ||
                _returnButton == null || _destroyButton == null || _boardButton == null || _cameraButton == null)
            {
                return;
            }

            RectTransform healthFrame = FindDirectChild(_healthPanel, "HealthFrame");
            RectTransform healthFill = FindDirectChild(_healthPanel, "HealthFill");
            RectTransform healthText = FindDirectChild(_healthPanel, "HealthText");
            if (healthFrame == null || healthFill == null || healthText == null)
                return;

            _widePanelSize = _selectionPanel.sizeDelta;
            _wideTitleSize = _title.sizeDelta;
            _wideTitlePosition = _title.anchoredPosition;
            _wideSubtitleSize = _subtitle.sizeDelta;
            _wideSubtitlePosition = _subtitle.anchoredPosition;
            _widePortraitSize = _portraitFrame.sizeDelta;
            _wideHealthSize = _healthPanel.sizeDelta;
            _wideOrderSize = _orderLabel.sizeDelta;
            _widePlayerControlSize = _playerControl.sizeDelta;
            _wideCommandButtonsSize = _commandButtons.sizeDelta;
            _wideCommandButtonsPosition = _commandButtons.anchoredPosition;
            _widePassengerChipSize = _passengerChip.sizeDelta;
            _widePassengerChipPosition = _passengerChip.anchoredPosition;
            _wideHealthFrameSize = healthFrame.sizeDelta;
            _wideHealthFillSize = healthFill.sizeDelta;
            _wideHealthTextPosition = healthText.anchoredPosition;
            _wideHealthTextSize = healthText.sizeDelta;
            GridLayoutGroup grid = _commandButtons.GetComponent<GridLayoutGroup>();
            _wideCommandCellSize = grid != null ? grid.cellSize : Vector2.zero;
            _wideCommandConstraint = grid != null ? grid.constraint : GridLayoutGroup.Constraint.Flexible;
            _wideCommandConstraintCount = grid != null ? grid.constraintCount : 2;
            TMP_Text titleText = _title.GetComponent<TMP_Text>();
            _wideTitleFontSize = titleText != null ? titleText.fontSize : 29f;
            _widePlayerControlActive = _playerControl.gameObject.activeSelf;
            _wideReturnActive = _returnButton.gameObject.activeSelf;
            _wideDestroyActive = _destroyButton.gameObject.activeSelf;
            _wideCameraActive = _cameraButton.gameObject.activeSelf;
            _wideRopeDropActive = ropeDropButton != null && ropeDropButton.gameObject.activeSelf;
            _selectionLayoutCached = true;
        }

        private void SetHealthLayout(
            Vector2 frameSize,
            Vector2 fillSize,
            Vector2 textPosition,
            Vector2 textSize)
        {
            RectTransform healthFrame = FindDirectChild(_healthPanel, "HealthFrame");
            RectTransform healthFill = FindDirectChild(_healthPanel, "HealthFill");
            RectTransform healthText = FindDirectChild(_healthPanel, "HealthText");
            healthFrame.sizeDelta = frameSize;
            healthFill.sizeDelta = fillSize;
            healthText.anchoredPosition = textPosition;
            healthText.sizeDelta = textSize;
        }

        private static RectTransform FindDirectChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                    return child as RectTransform;
            }
            return null;
        }

        private static RectTransform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;
            if (parent.name == childName)
                return parent as RectTransform;
            for (int i = 0; i < parent.childCount; i++)
            {
                RectTransform found = FindDeepChild(parent.GetChild(i), childName);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void ApplyCompactCommandVisual(RectTransform button, bool compact)
        {
            if (button == null)
                return;
            RectTransform icon = FindDeepChild(button, "Icon");
            RectTransform label = FindDeepChild(button, "Label");
            if (compact)
            {
                SetTopLeft(icon, 108f, 6f, 56f, 48f);
                SetTopLeft(label, 9f, 57f, 254f, 22f);
            }
            else
            {
                SetTopLeft(icon, 51f, 11f, 70f, 70f);
                SetTopLeft(label, 9f, 76f, 154f, 27f);
            }
        }

        private void ApplyCompactPassengerChip(bool compact)
        {
            RectTransform icon = FindDirectChild(_passengerChip, "Icon");
            RectTransform label = FindDirectChild(_passengerChip, "Label");
            if (compact)
            {
                SetTopLeft(icon, 17f, 17f, 44f, 44f);
                SetTopLeft(label, 72f, 10f, 184f, 58f);
                TMP_Text text = label != null ? label.GetComponent<TMP_Text>() : null;
                if (text != null)
                    text.alignment = TextAlignmentOptions.MidlineLeft;
            }
            else
            {
                SetTopLeft(icon, 9f, 7f, 25f, 25f);
                SetTopLeft(label, 42f, 3f, 300f, 33f);
                TMP_Text text = label != null ? label.GetComponent<TMP_Text>() : null;
                if (text != null)
                    text.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null)
                return;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        private static void BindButton(Button button, ref Button boundButton, UnityEngine.Events.UnityAction action)
        {
            if (boundButton == button)
                return;

            UnbindButton(ref boundButton, action);
            boundButton = button;
            if (boundButton != null)
                boundButton.onClick.AddListener(action);
        }

        private static void UnbindButton(ref Button boundButton, UnityEngine.Events.UnityAction action)
        {
            if (boundButton == null)
                return;

            boundButton.onClick.RemoveListener(action);
            boundButton = null;
        }
    }
}
