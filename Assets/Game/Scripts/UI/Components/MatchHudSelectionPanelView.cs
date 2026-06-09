using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public sealed class MatchHudSelectionPanelView : MonoBehaviour
{
    public readonly struct Model
    {
        public readonly bool Visible;
        public readonly string Title;
        public readonly string Subtitle;
        public readonly string CurrentOrder;
        public readonly string HealthText;
        public readonly float Health01;
        public readonly Sprite PortraitSprite;
        public readonly bool BadgeVisible;
        public readonly Sprite BadgeSprite;
        public readonly bool ReturnEnabled;
        public readonly bool DestroyEnabled;
        public readonly bool BoardEnabled;

        public Model(
            bool visible,
            string title,
            string subtitle,
            string currentOrder,
            string healthText,
            float health01,
            Sprite portraitSprite,
            bool badgeVisible,
            Sprite badgeSprite,
            bool returnEnabled,
            bool destroyEnabled,
            bool boardEnabled)
        {
            Visible = visible;
            Title = title;
            Subtitle = subtitle;
            CurrentOrder = currentOrder;
            HealthText = healthText;
            Health01 = health01;
            PortraitSprite = portraitSprite;
            BadgeVisible = badgeVisible;
            BadgeSprite = badgeSprite;
            ReturnEnabled = returnEnabled;
            DestroyEnabled = destroyEnabled;
            BoardEnabled = boardEnabled;
        }

        public static Model Hidden => new(
            false,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            0f,
            null,
            false,
            null,
            false,
            false,
            false);
    }

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

    private System.Action _returnRequested;
    private System.Action _destroyRequested;
    private System.Action _boardRequested;
    private Button _boundReturnAction;
    private Button _boundDestroyAction;
    private Button _boundBoardAction;

    private void Awake()
    {
        BindUnityEvents();
        HideSelection();
    }

    private void OnDestroy()
    {
        ClearActions();
        RemoveUnityEvents();
    }

    public void BindActions(System.Action returnRequested, System.Action destroyRequested, System.Action boardRequested)
    {
        BindUnityEvents();
        _returnRequested = returnRequested;
        _destroyRequested = destroyRequested;
        _boardRequested = boardRequested;
    }

    public void ClearActions()
    {
        _returnRequested = null;
        _destroyRequested = null;
        _boardRequested = null;
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
            SetSelectionPortrait(portraitSprite);
    }

    public void SetSelectionPortrait(Sprite portraitSprite)
    {
        if (selectedPortraitImage == null)
            return;

        selectedPortraitImage.sprite = portraitSprite;
        selectedPortraitImage.enabled = portraitSprite != null;
        selectedPortraitImage.preserveAspect = true;
    }

    public bool ContainsScreenPoint(Vector2 screenPosition)
    {
        if (selectedSquadPanel == null || !selectedSquadPanel.activeInHierarchy)
            return false;

        return ContainsScreenPoint(selectedSquadPanel.transform as RectTransform, screenPosition) ||
               ContainsScreenPoint(returnAction != null ? returnAction.transform as RectTransform : null, screenPosition) ||
               ContainsScreenPoint(destroyAction != null ? destroyAction.transform as RectTransform : null, screenPosition) ||
               ContainsScreenPoint(boardAction != null ? boardAction.transform as RectTransform : null, screenPosition);
    }

    public void Apply(Model model)
    {
        SetSelectionVisible(model.Visible);
        if (!model.Visible)
            return;

        SetText(titleText, model.Title);
        SetText(subtitleText, model.Subtitle);
        SetText(currentOrderText, model.CurrentOrder);
        SetText(healthText, model.HealthText);
        SetSelectionPortrait(model.PortraitSprite);
        SetHealthFill(model.Health01);
        SetBadge(model.BadgeVisible, model.BadgeSprite);
        SetActionState(returnAction, model.Visible);
        SetActionState(destroyAction, model.Visible);
        SetActionState(boardAction, model.Visible);
    }

    private void BindUnityEvents()
    {
        BindButton(returnAction, ref _boundReturnAction, HandleReturnAction);
        BindButton(destroyAction, ref _boundDestroyAction, HandleDestroyAction);
        BindButton(boardAction, ref _boundBoardAction, HandleBoardAction);
    }

    private void RemoveUnityEvents()
    {
        UnbindButton(ref _boundReturnAction, HandleReturnAction);
        UnbindButton(ref _boundDestroyAction, HandleDestroyAction);
        UnbindButton(ref _boundBoardAction, HandleBoardAction);
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
}
