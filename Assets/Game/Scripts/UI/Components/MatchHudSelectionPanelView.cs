using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MatchHudSelectionPanelView : MonoBehaviour, IMatchHudSelectionPanelView
    {
        [SerializeField] private GameObject selectedSquadPanel;
        [SerializeField] private Image selectedPortraitImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text currentOrderText;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private Image badgeImage;
        [SerializeField] private Button returnAction;
        [SerializeField] private Button destroyAction;
        [SerializeField] private Button boardAction;
        [SerializeField] private Button cameraAction;
        [SerializeField] private GameObject passengerChipRoot;
        [SerializeField] private Button passengerChipButton;
        [SerializeField] private TMP_Text passengerChipLabel;
        [SerializeField] private MatchHudTransportPassengerDrawerView passengerDrawer;

        [Header("Fallback Portraits")]
        [SerializeField] private Sprite genericSquadPortraitSprite;
        [SerializeField] private Sprite soldierSquadPortraitSprite;
        [SerializeField] private Sprite vehicleSquadPortraitSprite;
        [SerializeField] private Sprite aircraftSquadPortraitSprite;
        [SerializeField] private Sprite transportSquadPortraitSprite;
        [SerializeField] private Sprite buildingSquadPortraitSprite;
        [SerializeField] private Sprite mixedForcePortraitSprite;
        [SerializeField] private Sprite mixedSoldierVehiclePortraitSprite;
        [SerializeField] private Sprite mixedSoldierAircraftPortraitSprite;
        [SerializeField] private Sprite mixedVehicleAircraftPortraitSprite;
        [SerializeField] private Sprite mixedSoldierVehicleAircraftPortraitSprite;

        private System.Action _returnRequested;
        private System.Action _destroyRequested;
        private System.Action _boardRequested;
        private Action _cameraRequested;
        private Action _passengerChipRequested;
        private Action _passengerDrawerCloseRequested;
        private Action _passengerExitAllRequested;
        private Action<UiEntityHandle> _passengerExitRequested;
        private Button _boundReturnAction;
        private Button _boundDestroyAction;
        private Button _boundBoardAction;
        private Button _boundCameraAction;
        private Button _boundPassengerChipButton;
        private Sprite _boardActionNormalSprite;
        private Sprite _cameraActionNormalSprite;
        private bool _boardActionNormalSpriteCached;
        private bool _cameraActionNormalSpriteCached;
        private Color _boardActionNormalColor;
        private Color _cameraActionNormalColor;
        private bool _boardActionNormalColorCached;
        private bool _cameraActionNormalColorCached;
        private bool _boardActionSelected;
        private bool _cameraActionSelected;
        private bool _cameraActionEnabled;
        private bool _passengerDrawerOpen;
        private UiEntityHandle _passengerDrawerTransport;
        private readonly List<MatchHudSelectionPanelPassengerItemModel> _emptyPassengers = new();

        private void Awake()
        {
            MatchHudCanvasBatchingUtility.EnsureLocalCanvas(selectedSquadPanel, needsRaycaster: true);
            BindUnityEvents();
            CacheBoardActionNormalSprite();
            CacheCameraActionNormalSprite();
            _cameraActionEnabled = cameraAction != null && cameraAction.interactable;
            ApplyTransportPassengers(MatchHudTransportPassengersModel.Hidden);
            HideSelection();
        }

        private void OnDestroy()
        {
            ClearActions();
            ClearTransportPassengerActions();
            RemoveUnityEvents();
        }

        public void BindActions(System.Action returnRequested, System.Action destroyRequested, System.Action boardRequested)
        {
            BindUnityEvents();
            _returnRequested = returnRequested;
            _destroyRequested = destroyRequested;
            _boardRequested = boardRequested;
        }

        public void BindCameraAction(Action cameraRequested)
        {
            BindUnityEvents();
            _cameraRequested = cameraRequested;
        }

        public void BindTransportPassengerActions(
            Action passengerChipRequested,
            Action passengerDrawerCloseRequested,
            Action passengerExitAllRequested,
            Action<UiEntityHandle> passengerExitRequested)
        {
            BindUnityEvents();
            _passengerChipRequested = passengerChipRequested;
            _passengerDrawerCloseRequested = passengerDrawerCloseRequested;
            _passengerExitAllRequested = passengerExitAllRequested;
            _passengerExitRequested = passengerExitRequested;
            passengerDrawer?.BindActions(HandlePassengerExitAll, HandlePassengerDrawerClose, HandlePassengerExit);
        }

        public void ClearActions()
        {
            _returnRequested = null;
            _destroyRequested = null;
            _boardRequested = null;
            _cameraRequested = null;
        }

        public void ClearTransportPassengerActions()
        {
            _passengerChipRequested = null;
            _passengerDrawerCloseRequested = null;
            _passengerExitAllRequested = null;
            _passengerExitRequested = null;
            passengerDrawer?.ClearActions();
        }

        public void ShowSelection()
        {
            SetSelectionVisible(true);
        }

        public void HideSelection()
        {
            SetSelectionVisible(false);
            SetActionState(boardAction, true);
            SetActionState(cameraAction, false);
        }

        public void SetSelectionVisible(bool visible)
        {
            if (selectedSquadPanel != null && selectedSquadPanel.activeSelf != visible)
                selectedSquadPanel.SetActive(visible);
        }

        public void SetSelectionVisible(bool visible, Sprite portraitSprite)
        {
            SetSelectionVisible(visible);
            if (visible)
                SetSelectionPortrait(FirstNonNull(portraitSprite, ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.GenericSquad)));
        }

        public void SetSelectionPortrait(Sprite portraitSprite)
        {
            if (selectedPortraitImage == null)
                return;

            if (selectedPortraitImage.sprite != portraitSprite)
                selectedPortraitImage.sprite = portraitSprite;
            bool enabled = portraitSprite != null;
            if (selectedPortraitImage.enabled != enabled)
                selectedPortraitImage.enabled = enabled;
            if (!selectedPortraitImage.preserveAspect)
                selectedPortraitImage.preserveAspect = true;
        }

        public Sprite ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind kind)
        {
            return kind switch
            {
                SelectionSummaryPortraitKind.Soldiers => FirstNonNull(soldierSquadPortraitSprite, genericSquadPortraitSprite),
                SelectionSummaryPortraitKind.Vehicles => FirstNonNull(vehicleSquadPortraitSprite, genericSquadPortraitSprite),
                SelectionSummaryPortraitKind.Aircraft => FirstNonNull(aircraftSquadPortraitSprite, genericSquadPortraitSprite),
                SelectionSummaryPortraitKind.Transports => FirstNonNull(vehicleSquadPortraitSprite, transportSquadPortraitSprite, genericSquadPortraitSprite),
                SelectionSummaryPortraitKind.Buildings => FirstNonNull(buildingSquadPortraitSprite, genericSquadPortraitSprite),
                SelectionSummaryPortraitKind.MixedSoldierVehicle => FirstNonNull(mixedSoldierVehiclePortraitSprite, mixedForcePortraitSprite, genericSquadPortraitSprite),
                SelectionSummaryPortraitKind.MixedSoldierAircraft => FirstNonNull(mixedSoldierAircraftPortraitSprite, mixedForcePortraitSprite, genericSquadPortraitSprite),
                SelectionSummaryPortraitKind.MixedVehicleAircraft => FirstNonNull(mixedVehicleAircraftPortraitSprite, mixedForcePortraitSprite, genericSquadPortraitSprite),
                SelectionSummaryPortraitKind.MixedSoldierVehicleAircraft => FirstNonNull(mixedSoldierVehicleAircraftPortraitSprite, mixedForcePortraitSprite, genericSquadPortraitSprite),
                SelectionSummaryPortraitKind.MixedForce => FirstNonNull(mixedForcePortraitSprite, genericSquadPortraitSprite),
                SelectionSummaryPortraitKind.GenericSquad => genericSquadPortraitSprite,
                _ => genericSquadPortraitSprite
            };
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            if (selectedSquadPanel == null || !selectedSquadPanel.activeInHierarchy)
                return false;

            return ContainsScreenPoint(selectedSquadPanel.transform as RectTransform, screenPosition) ||
                   ContainsScreenPoint(returnAction != null ? returnAction.transform as RectTransform : null, screenPosition) ||
                   ContainsScreenPoint(destroyAction != null ? destroyAction.transform as RectTransform : null, screenPosition) ||
                   ContainsScreenPoint(boardAction != null ? boardAction.transform as RectTransform : null, screenPosition) ||
                   ContainsScreenPoint(cameraAction != null ? cameraAction.transform as RectTransform : null, screenPosition) ||
                   ContainsScreenPoint(passengerChipButton != null ? passengerChipButton.transform as RectTransform : null, screenPosition) ||
                   (passengerDrawer != null && passengerDrawer.ContainsScreenPoint(screenPosition));
        }

        public void Apply(MatchHudSelectionPanelModel model)
        {
            SetSelectionVisible(model.Visible);
            if (!model.Visible)
                return;

            SetText(titleText, model.Title);
            SetText(subtitleText, model.Subtitle);
            SetText(currentOrderText, model.CurrentOrder);
            SetText(healthText, model.HealthText);
            SetSelectionPortrait(FirstNonNull(model.PortraitSprite, ResolveFallbackPortraitSprite(model.PortraitKind)));
            SetHealthFill(model.Health01);
            SetBadge(model.BadgeVisible, model.BadgeSprite);
            SetActionState(returnAction, model.Visible && model.ReturnEnabled);
            SetActionState(destroyAction, model.Visible && model.DestroyEnabled);
            // BoardButton is hosted in the command rail; keep it pressable so the
            // command system can show no-selection/invalid-selection feedback.
            SetActionState(boardAction, true);
            SetBoardActionSelected(_boardActionSelected);
            SetActionState(cameraAction, model.Visible && _cameraActionEnabled);
            SetCameraActionSelected(_cameraActionSelected);
        }

        public void ToggleTransportPassengerDrawer()
        {
            _passengerDrawerOpen = !_passengerDrawerOpen;
        }

        public void CloseTransportPassengerDrawer()
        {
            _passengerDrawerOpen = false;
        }

        public void ApplyTransportPassengers(MatchHudTransportPassengersModel model)
        {
            bool isPassengerModel = model.StorageKind == MatchHudStorageChipKind.Passengers;
            if (!model.Visible || (isPassengerModel && model.Transport.IsNull))
            {
                _passengerDrawerOpen = false;
                _passengerDrawerTransport = UiEntityHandle.Null;
                ApplyPassengerChip(MatchHudTransportPassengersModel.Hidden);
                passengerDrawer?.Apply(MatchHudTransportPassengersModel.Hidden);
                return;
            }

            if (!isPassengerModel)
            {
                _passengerDrawerOpen = false;
                _passengerDrawerTransport = UiEntityHandle.Null;
                ApplyPassengerChip(model);
                passengerDrawer?.Apply(MatchHudTransportPassengersModel.Hidden);
                return;
            }

            if (_passengerDrawerTransport != model.Transport)
            {
                _passengerDrawerTransport = model.Transport;
                _passengerDrawerOpen = false;
            }

            ApplyPassengerChip(model);
            passengerDrawer?.Apply(new MatchHudTransportPassengersModel(
                true,
                _passengerDrawerOpen,
                model.Transport,
                model.PassengerCount,
                model.Capacity,
                model.ExitAllEnabled,
                model.Passengers ?? _emptyPassengers,
                model.SoldierPassengerCount,
                model.SoldierCapacity,
                model.VehiclePassengerCount,
                model.VehicleCapacity,
                model.StorageKind,
                model.OilCurrent,
                model.OilCapacity,
                model.FuelCurrent,
                model.FuelCapacity));
        }

        public void SetBoardActionSelected(bool selected)
        {
            if (boardAction == null || boardAction.targetGraphic is not Image image)
                return;

            _boardActionSelected = selected;
            CacheBoardActionNormalSprite();
            CacheBoardActionNormalColor(image);
            Sprite selectedSprite = boardAction.spriteState.selectedSprite;
            if (selected && selectedSprite != null)
            {
                SetImageSprite(image, selectedSprite);
                if (_boardActionNormalColorCached)
                    SetImageColor(image, _boardActionNormalColor);
            }
            else if (_boardActionNormalSprite != null)
            {
                SetImageSprite(image, _boardActionNormalSprite);
                if (_boardActionNormalColorCached)
                    SetImageColor(image, _boardActionNormalColor);
            }
            else
            {
                if (selected)
                    SetImageColor(image, boardAction.colors.selectedColor);
                else if (_boardActionNormalColorCached)
                    SetImageColor(image, _boardActionNormalColor);
            }
        }

        public void SetCameraActionSelected(bool selected)
        {
            ApplyCameraActionSelected(selected, force: false);
        }

        private void ApplyCameraActionSelected(bool selected, bool force)
        {
            if (cameraAction == null || cameraAction.targetGraphic is not Image image)
                return;
            if (!force && _cameraActionSelected == selected)
                return;

            _cameraActionSelected = selected;
            CacheCameraActionNormalSprite();
            CacheCameraActionNormalColor(image);
            Sprite selectedSprite = cameraAction.spriteState.selectedSprite;
            if (selected && selectedSprite != null)
            {
                SetImageSprite(image, selectedSprite);
                if (image.overrideSprite != selectedSprite)
                    image.overrideSprite = selectedSprite;
                if (_cameraActionNormalColorCached)
                    SetImageColor(image, _cameraActionNormalColor);
            }
            else if (_cameraActionNormalSprite != null)
            {
                SetImageSprite(image, _cameraActionNormalSprite);
                if (image.overrideSprite == selectedSprite)
                    image.overrideSprite = null;
                if (_cameraActionNormalColorCached)
                    SetImageColor(image, _cameraActionNormalColor);
            }
            else
            {
                SetImageSprite(image, null);
                if (image.overrideSprite == selectedSprite)
                    image.overrideSprite = null;
                if (selected)
                    SetImageColor(image, cameraAction.colors.selectedColor);
                else if (_cameraActionNormalColorCached)
                    SetImageColor(image, _cameraActionNormalColor);
            }
        }

        public void SetCameraActionEnabled(bool enabled)
        {
            bool effectiveEnabled = selectedSquadPanel == null || selectedSquadPanel.activeSelf ? enabled : false;
            if (_cameraActionEnabled == enabled &&
                (cameraAction == null || cameraAction.interactable == effectiveEnabled))
            {
                return;
            }

            _cameraActionEnabled = enabled;
            SetActionState(cameraAction, effectiveEnabled);
            ApplyCameraActionSelected(_cameraActionSelected, force: true);
        }

        private void BindUnityEvents()
        {
            BindButton(returnAction, ref _boundReturnAction, HandleReturnAction);
            BindButton(destroyAction, ref _boundDestroyAction, HandleDestroyAction);
            BindButton(boardAction, ref _boundBoardAction, HandleBoardAction);
            BindButton(cameraAction, ref _boundCameraAction, HandleCameraAction);
            BindButton(passengerChipButton, ref _boundPassengerChipButton, HandlePassengerChip);
        }

        private void CacheBoardActionNormalSprite()
        {
            if (_boardActionNormalSpriteCached ||
                boardAction == null ||
                boardAction.targetGraphic is not Image image)
            {
                return;
            }

            _boardActionNormalSprite = image.sprite;
            _boardActionNormalSpriteCached = true;
        }

        private void CacheCameraActionNormalSprite()
        {
            if (_cameraActionNormalSpriteCached ||
                cameraAction == null ||
                cameraAction.targetGraphic is not Image image)
            {
                return;
            }

            _cameraActionNormalSprite = image.sprite;
            _cameraActionNormalSpriteCached = true;
        }

        private void CacheBoardActionNormalColor(Image image)
        {
            if (_boardActionNormalColorCached || image == null)
                return;

            _boardActionNormalColor = image.color;
            _boardActionNormalColorCached = true;
        }

        private void CacheCameraActionNormalColor(Image image)
        {
            if (_cameraActionNormalColorCached || image == null)
                return;

            _cameraActionNormalColor = image.color;
            _cameraActionNormalColorCached = true;
        }

        private void RemoveUnityEvents()
        {
            UnbindButton(ref _boundReturnAction, HandleReturnAction);
            UnbindButton(ref _boundDestroyAction, HandleDestroyAction);
            UnbindButton(ref _boundBoardAction, HandleBoardAction);
            UnbindButton(ref _boundCameraAction, HandleCameraAction);
            UnbindButton(ref _boundPassengerChipButton, HandlePassengerChip);
        }

        private void HandleReturnAction()
        {
            _returnRequested?.Invoke();
        }

        private void HandleDestroyAction()
        {
            _destroyRequested?.Invoke();
        }

        private void HandleBoardAction()
        {
            _boardRequested?.Invoke();
        }

        private void HandleCameraAction()
        {
            _cameraRequested?.Invoke();
        }

        private void HandlePassengerChip()
        {
            ToggleTransportPassengerDrawer();
            _passengerChipRequested?.Invoke();
        }

        private void HandlePassengerDrawerClose()
        {
            CloseTransportPassengerDrawer();
            _passengerDrawerCloseRequested?.Invoke();
        }

        private void HandlePassengerExitAll()
        {
            _passengerExitAllRequested?.Invoke();
        }

        private void HandlePassengerExit(UiEntityHandle passenger)
        {
            _passengerExitRequested?.Invoke(passenger);
        }

        private void ApplyPassengerChip(MatchHudTransportPassengersModel model)
        {
            bool visible = model.Visible;
            if (passengerChipRoot != null && passengerChipRoot.activeSelf != visible)
                passengerChipRoot.SetActive(visible);

            if (passengerChipButton != null)
            {
                bool interactable = visible && model.StorageKind == MatchHudStorageChipKind.Passengers;
                if (passengerChipButton.interactable != interactable)
                    passengerChipButton.interactable = interactable;
            }

            if (passengerChipLabel != null)
            {
                string text = visible ? ResolveStorageChipLabel(model) : string.Empty;
                SetRawText(passengerChipLabel, text);
            }
        }

        private static string ResolveStorageChipLabel(MatchHudTransportPassengersModel model)
        {
            return model.StorageKind switch
            {
                MatchHudStorageChipKind.OilBarrels =>
                    $"OIL BARRELS {Mathf.Max(0, model.OilCurrent)}/{Mathf.Max(0, model.OilCapacity)}",
                MatchHudStorageChipKind.FuelBarrels =>
                    $"FUEL {Mathf.Max(0, model.FuelCurrent)}/{Mathf.Max(0, model.FuelCapacity)}",
                MatchHudStorageChipKind.OilAndFuel =>
                    $"OIL {Mathf.Max(0, model.OilCurrent)}/{Mathf.Max(0, model.OilCapacity)} | FUEL {Mathf.Max(0, model.FuelCurrent)}/{Mathf.Max(0, model.FuelCapacity)}",
                MatchHudStorageChipKind.ResourceCargo =>
                    ResolveResourceCargoChipLabel(model),
                _ =>
                    $"PASSENGERS {Mathf.Max(0, model.PassengerCount)}/{Mathf.Max(0, model.Capacity)}"
            };
        }

        private static string ResolveResourceCargoChipLabel(MatchHudTransportPassengersModel model)
        {
            int capacity = Mathf.Max(0, model.Capacity);
            int oil = Mathf.Max(0, model.OilCurrent);
            int fuel = Mathf.Max(0, model.FuelCurrent);
            if (oil > 0 && fuel > 0)
                return $"OIL {oil}/{capacity} | FUEL {fuel}/{capacity}";
            if (fuel > 0)
                return $"FUEL {fuel}/{capacity}";
            if (oil > 0)
                return $"OIL {oil}/{capacity}";
            return $"CARGO 0/{capacity}";
        }

        private void SetHealthFill(float health01)
        {
            if (healthFillImage == null)
                return;

            if (healthFillImage.type != Image.Type.Filled)
                healthFillImage.type = Image.Type.Filled;
            if (healthFillImage.fillMethod != Image.FillMethod.Horizontal)
                healthFillImage.fillMethod = Image.FillMethod.Horizontal;
            if (healthFillImage.fillOrigin != 0)
                healthFillImage.fillOrigin = 0;
            float fillAmount = Mathf.Clamp01(health01);
            if (!Mathf.Approximately(healthFillImage.fillAmount, fillAmount))
                healthFillImage.fillAmount = fillAmount;
        }

        private void SetBadge(bool visible, Sprite sprite)
        {
            if (badgeRoot != null && badgeRoot.activeSelf != visible)
                badgeRoot.SetActive(visible);
            if (badgeImage == null)
                return;

            if (sprite != null)
                SetImageSprite(badgeImage, sprite);
            bool enabled = visible && badgeImage.sprite != null;
            if (badgeImage.enabled != enabled)
                badgeImage.enabled = enabled;
        }

        private static void SetActionState(Selectable action, bool enabled)
        {
            if (action != null && action.interactable != enabled)
                action.interactable = enabled;
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

        private static void SetText(TMP_Text text, string value)
        {
            if (text == null)
                return;

            SetRawText(text, string.IsNullOrWhiteSpace(value) ? "-" : value);
        }

        private static void SetRawText(TMP_Text text, string value)
        {
            value ??= string.Empty;
            if (text.text != value)
                text.text = value;
        }

        private static void SetImageSprite(Image image, Sprite sprite)
        {
            if (image != null && image.sprite != sprite)
                image.sprite = sprite;
        }

        private static void SetImageColor(Image image, Color color)
        {
            if (image != null && image.color != color)
                image.color = color;
        }

        private static bool ContainsScreenPoint(RectTransform rectTransform, Vector2 screenPosition)
        {
            return rectTransform != null &&
                   rectTransform.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition);
        }

        private static Sprite FirstNonNull(params Sprite[] sprites)
        {
            if (sprites == null)
                return null;

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                    return sprites[i];
            }

            return null;
        }
    }
}
