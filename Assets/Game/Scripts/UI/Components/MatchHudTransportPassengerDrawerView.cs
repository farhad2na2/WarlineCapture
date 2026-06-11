using System;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

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

    private readonly List<MatchHudTransportPassengerItemView> _runtimeItems = new();
    private Action _exitAllRequested;
    private Action _closeRequested;
    private Action<Entity> _exitPassengerRequested;
    private Button _boundExitAllButton;
    private Button _boundCloseButton;

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
    }

    public void BindActions(Action exitAllRequested, Action closeRequested, Action<Entity> exitPassengerRequested)
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

        SetText(headerText, $"PASSENGERS {model.PassengerCount}/{model.Capacity}");
        SetText(emptyStateText, "NO PASSENGERS ONBOARD");
        SetText(exitAllLabel, "EXIT ALL");
        SetText(closeLabel, "CLOSE");
        if (exitAllButton != null)
            exitAllButton.interactable = model.ExitAllEnabled;

        IReadOnlyList<MatchHudSelectionPanelPassengerItemModel> passengers = model.Passengers;
        int passengerCount = passengers?.Count ?? 0;
        if (emptyStateRoot != null)
            emptyStateRoot.SetActive(passengerCount == 0);

        EnsureItemPool(passengerCount);
        for (int i = 0; i < _runtimeItems.Count; i++)
        {
            MatchHudTransportPassengerItemView item = _runtimeItems[i];
            bool active = i < passengerCount;
            if (item == null)
                continue;

            item.gameObject.SetActive(active);
            if (active)
            {
                item.gameObject.name = $"PassengerItemView - {passengers[i].DisplayName}";
                item.Bind(passengers[i], _exitPassengerRequested);
            }
        }

        if (itemTemplate != null && passengerCount == 0)
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
        if (drawerRoot != null)
            drawerRoot.SetActive(visible);
        else
            gameObject.SetActive(visible);
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
        if (text != null)
            text.text = value ?? string.Empty;
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
