using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MatchHudTransportPassengerItemView : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Button exitButton;

    private UiEntityHandle _passenger;
    private Action<UiEntityHandle> _exitRequested;
    private Button _boundExitButton;

    private void Awake()
    {
        BindUnityEvents();
    }

    private void OnDestroy()
    {
        UnbindButton(ref _boundExitButton, HandleExit);
    }

    public void Bind(MatchHudSelectionPanelPassengerItemModel model, Action<UiEntityHandle> exitRequested)
    {
        BindUnityEvents();

        _passenger = model.Passenger;
        _exitRequested = exitRequested;

        SetImage(portraitImage, model.PortraitSprite);
        SetText(nameText, model.DisplayName);
        SetText(roleText, model.RoleText);
        SetText(healthText, model.HealthText);
        SetHealthFill(model.Health01);
        if (exitButton != null)
            exitButton.interactable = model.ExitEnabled;
    }

    private void BindUnityEvents()
    {
        BindButton(exitButton, ref _boundExitButton, HandleExit);
    }

    private void HandleExit()
    {
        if (!_passenger.IsNull)
            _exitRequested?.Invoke(_passenger);
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = string.IsNullOrWhiteSpace(value) ? "-" : value;
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
