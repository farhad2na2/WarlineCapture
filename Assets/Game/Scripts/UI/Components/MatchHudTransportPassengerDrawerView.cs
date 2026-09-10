using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed partial class MatchHudTransportPassengerDrawerView : MonoBehaviour
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
                SetTopLeft(icon, 10f, (_passengerChip.rect.height - 36f) * .5f, 36f, 36f);
                SetTopLeft(label, 52f, 6f, _passengerChip.rect.width - 62f, _passengerChip.rect.height - 12f);
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
