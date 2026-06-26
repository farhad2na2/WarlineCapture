using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

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
    private Action _passengerChipRequested;
    private Action _passengerDrawerCloseRequested;
    private Action _passengerExitAllRequested;
    private Action<UiEntityHandle> _passengerExitRequested;
    private Button _boundReturnAction;
    private Button _boundDestroyAction;
    private Button _boundBoardAction;
    private Button _boundPassengerChipButton;
    private Sprite _boardActionNormalSprite;
    private Color _boardActionNormalColor;
    private bool _boardActionNormalColorCached;
    private bool _boardActionSelected;
    private bool _passengerDrawerOpen;
    private UiEntityHandle _passengerDrawerTransport;
    private readonly List<MatchHudSelectionPanelPassengerItemModel> _emptyPassengers = new();

    private void Awake()
    {
        BindUnityEvents();
        CacheBoardActionNormalSprite();
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
    }

    public void SetSelectionVisible(bool visible)
    {
        if (selectedSquadPanel != null)
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

        selectedPortraitImage.sprite = portraitSprite;
        selectedPortraitImage.enabled = portraitSprite != null;
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
        SetActionState(boardAction, model.Visible && model.BoardEnabled);
        SetBoardActionSelected(_boardActionSelected);
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
        if (!model.Visible || model.Transport.IsNull)
        {
            _passengerDrawerOpen = false;
            _passengerDrawerTransport = UiEntityHandle.Null;
            ApplyPassengerChip(false, 0, 0);
            passengerDrawer?.Apply(MatchHudTransportPassengersModel.Hidden);
            return;
        }

        if (_passengerDrawerTransport != model.Transport)
        {
            _passengerDrawerTransport = model.Transport;
            _passengerDrawerOpen = false;
        }

        ApplyPassengerChip(true, model.PassengerCount, model.Capacity);
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
            model.VehicleCapacity));
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
            image.sprite = selectedSprite;
            image.color = _boardActionNormalColorCached ? _boardActionNormalColor : image.color;
        }
        else if (_boardActionNormalSprite != null)
        {
            image.sprite = _boardActionNormalSprite;
            image.color = _boardActionNormalColorCached ? _boardActionNormalColor : image.color;
        }
        else
        {
            image.color = selected
                ? boardAction.colors.selectedColor
                : (_boardActionNormalColorCached ? _boardActionNormalColor : image.color);
        }
    }

    private void BindUnityEvents()
    {
        BindButton(returnAction, ref _boundReturnAction, HandleReturnAction);
        BindButton(destroyAction, ref _boundDestroyAction, HandleDestroyAction);
        BindButton(boardAction, ref _boundBoardAction, HandleBoardAction);
        BindButton(passengerChipButton, ref _boundPassengerChipButton, HandlePassengerChip);
    }

    private void CacheBoardActionNormalSprite()
    {
        if (_boardActionNormalSprite != null ||
            boardAction == null ||
            boardAction.targetGraphic is not Image image)
        {
            return;
        }

        _boardActionNormalSprite = image.sprite;
    }

    private void CacheBoardActionNormalColor(Image image)
    {
        if (_boardActionNormalColorCached || image == null)
            return;

        _boardActionNormalColor = image.color;
        _boardActionNormalColorCached = true;
    }

    private void RemoveUnityEvents()
    {
        UnbindButton(ref _boundReturnAction, HandleReturnAction);
        UnbindButton(ref _boundDestroyAction, HandleDestroyAction);
        UnbindButton(ref _boundBoardAction, HandleBoardAction);
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

    private void ApplyPassengerChip(bool visible, int passengerCount, int capacity)
    {
        if (passengerChipRoot != null)
            passengerChipRoot.SetActive(visible);

        if (passengerChipButton != null)
            passengerChipButton.interactable = visible;

        if (passengerChipLabel != null)
            passengerChipLabel.text = visible
                ? $"PASSENGERS {Mathf.Max(0, passengerCount)}/{Mathf.Max(0, capacity)}"
                : string.Empty;
    }

    private void SetHealthFill(float health01)
    {
        if (healthFillImage == null)
            return;

        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Horizontal;
        healthFillImage.fillOrigin = 0;
        healthFillImage.fillAmount = Mathf.Clamp01(health01);
    }

    private void SetBadge(bool visible, Sprite sprite)
    {
        if (badgeRoot != null)
            badgeRoot.SetActive(visible);
        if (badgeImage == null)
            return;

        if (sprite != null)
            badgeImage.sprite = sprite;
        badgeImage.enabled = visible && badgeImage.sprite != null;
    }

    private static void SetActionState(Selectable action, bool enabled)
    {
        if (action != null)
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
        if (text != null)
            text.text = string.IsNullOrWhiteSpace(value) ? "-" : value;
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
