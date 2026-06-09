using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BuildPlacementConfirmationBarView : MonoBehaviour
{
    private const float RefreshIntervalSeconds = 0.1f;

    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text durationText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button rotateButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Sprite panelFrameSprite;
    [SerializeField] private Sprite statusChipSprite;
    [SerializeField] private Sprite secondaryButtonSprite;
    [SerializeField] private Sprite goldActionButtonSprite;
    [SerializeField] private Sprite squareButtonSprite;
    [SerializeField] private Sprite instructionStripSprite;
    [SerializeField] private Sprite creditsIconSprite;
    [SerializeField] private Sprite timeIconSprite;
    [SerializeField] private Sprite cancelIconSprite;
    [SerializeField] private Sprite rotateIconSprite;
    [SerializeField] private Sprite confirmIconSprite;
    [SerializeField] private Sprite infoIconSprite;

    private CanvasGroup _canvasGroup;
    private BuildingUiCommandSystem _commandSystem;
    private BuildingUiCommandSystem.Context _commandContext;
    private UnityAction _cancelListener;
    private UnityAction _rotateListener;
    private UnityAction _confirmListener;
    private float _nextRefreshAt;

    public RectTransform Root => root != null ? root : transform as RectTransform;

    private void Awake()
    {
        EnsureRuntimeLayout();
    }

    public static BuildPlacementConfirmationBarView Ensure(GameObject prefab, RectTransform parent)
    {
        if (parent == null)
            return null;

        BuildPlacementConfirmationBarView existing = parent.GetComponentInChildren<BuildPlacementConfirmationBarView>(true);
        if (existing != null)
            return existing;

        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, parent, false);
            instance.name = prefab.name;
            ApplyDefaultPlacementAnchors(instance.GetComponent<RectTransform>());

            BuildPlacementConfirmationBarView view = instance.GetComponent<BuildPlacementConfirmationBarView>();
            if (view == null)
                view = instance.AddComponent<BuildPlacementConfirmationBarView>();

            view.EnsureRuntimeLayout();
            view.Hide();
            return view;
        }

        return Ensure(parent);
    }

    public static BuildPlacementConfirmationBarView Ensure(RectTransform parent)
    {
        if (parent == null)
            return null;

        BuildPlacementConfirmationBarView existing = parent.GetComponentInChildren<BuildPlacementConfirmationBarView>(true);
        if (existing != null)
            return existing;

        GameObject instance = new("BuildPlacementConfirmationBar", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        ApplyDefaultPlacementAnchors(rect);

        Image background = instance.GetComponent<Image>();
        background.color = new Color(0.035f, 0.045f, 0.04f, 0.94f);
        background.raycastTarget = true;

        BuildPlacementConfirmationBarView view = instance.AddComponent<BuildPlacementConfirmationBarView>();
        view.root = rect;
        view._canvasGroup = instance.GetComponent<CanvasGroup>();
        view.BuildGeneratedLayout();
        view.Hide();
        return view;
    }

    public bool ContainsScreenPoint(Vector2 screenPosition)
    {
        RectTransform rect = Root;
        if (rect == null || !rect.gameObject.activeInHierarchy)
            return false;
        if (_canvasGroup != null && (!_canvasGroup.blocksRaycasts || _canvasGroup.alpha <= 0.01f))
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, ResolveEventCamera());
    }

    public void BindRuntimeCommands(BuildingUiCommandSystem commandSystem, BuildingUiCommandSystem.Context commandContext)
    {
        _commandSystem = commandSystem;
        _commandContext = commandContext;
        WireButtons();
        Refresh(force: true);
    }

    private void OnEnable()
    {
        EnsureRuntimeLayout();
        WireButtons();
        Refresh(force: true);
    }

    private void OnDisable()
    {
        UnwireButtons();
        _nextRefreshAt = 0f;
    }

    private void Update()
    {
        Refresh(force: false);
    }

    private void WireButtons()
    {
        if (_cancelListener == null)
            _cancelListener = OnCancelClicked;
        if (_rotateListener == null)
            _rotateListener = OnRotateClicked;
        if (_confirmListener == null)
            _confirmListener = OnConfirmClicked;

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(_cancelListener);
            cancelButton.onClick.AddListener(_cancelListener);
        }

        if (rotateButton != null)
        {
            rotateButton.onClick.RemoveListener(_rotateListener);
            rotateButton.onClick.AddListener(_rotateListener);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(_confirmListener);
            confirmButton.onClick.AddListener(_confirmListener);
        }
    }

    private void UnwireButtons()
    {
        if (_cancelListener != null)
            cancelButton?.onClick.RemoveListener(_cancelListener);
        if (_rotateListener != null)
            rotateButton?.onClick.RemoveListener(_rotateListener);
        if (_confirmListener != null)
            confirmButton?.onClick.RemoveListener(_confirmListener);
    }

    private void Refresh(bool force)
    {
        if (!force && Time.unscaledTime < _nextRefreshAt)
            return;

        _nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
        bool hasPlacement = _commandSystem != null && _commandSystem.HasPendingBuildingPlacement(_commandContext);
        if (!hasPlacement)
        {
            Hide();
            return;
        }

        Show();

        string rawStatus = _commandSystem.PlacementStatusText(_commandContext);
        SplitPlacementStatus(rawStatus, out string title, out string status);
        SetText(titleText, string.IsNullOrWhiteSpace(title) ? "PLACE BUILDING" : $"PLACE {title.ToUpperInvariant()}");
        SetText(costText, FormatCost(_commandSystem.ActivePlacementCost(_commandContext)));
        SetText(durationText, FormatDuration(_commandSystem.ActivePlacementDurationSeconds(_commandContext)));
        SetText(instructionText, "DRAG TO POSITION, CONFIRM TO BUILD");

        bool canConfirm = _commandSystem.CanConfirmBuildingPlacement(_commandContext);
        SetText(statusText, string.IsNullOrWhiteSpace(status) ? "DRAG TO POSITION" : status.ToUpperInvariant());
        if (statusText != null)
            statusText.color = canConfirm
                ? new Color(0.62f, 0.98f, 0.35f)
                : new Color(1f, 0.35f, 0.22f);

        if (confirmButton != null)
            confirmButton.interactable = canConfirm;
        if (cancelButton != null)
            cancelButton.interactable = true;
        if (rotateButton != null)
            rotateButton.interactable = true;
    }

    private void OnCancelClicked()
    {
        _commandSystem?.CancelBuildingPlacement(_commandContext);
        BattleHudRuntimeFeedbackSystem.ClearStickyCommandMode(TacticalCommandMode.Build);
        BattleHudRuntimeFeedbackSystem.ApplyCommandResult(TacticalCommandResult.Success("PLACEMENT CANCELLED"));
        Refresh(force: true);
    }

    private void OnRotateClicked()
    {
        bool rotated = _commandSystem != null && _commandSystem.RotateBuildingPlacement(_commandContext);
        BattleHudRuntimeFeedbackSystem.ApplyCommandResult(rotated
            ? TacticalCommandResult.Success("ROTATED 90 DEGREES")
            : TacticalCommandResult.Rejected(TacticalCommandReasonCode.BuildUnavailable, "No active building placement."));
        Refresh(force: true);
    }

    private void OnConfirmClicked()
    {
        bool placed = _commandSystem != null && _commandSystem.ConfirmBuildingPlacement(_commandContext);
        BattleHudRuntimeFeedbackSystem.ApplyCommandResult(placed
            ? TacticalCommandResult.Success("BUILDING PLACED")
            : TacticalCommandResult.Rejected(TacticalCommandReasonCode.BuildUnavailable, "Place on valid ground."));
        Refresh(force: true);
    }

    private void Show()
    {
        if (Root != null && !Root.gameObject.activeSelf)
            Root.gameObject.SetActive(true);

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    private void Hide()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    private void BuildGeneratedLayout()
    {
        titleText = CreateText("Title", Root, new Vector2(0.07f, 0.67f), new Vector2(0.39f, 0.94f), 26, TextAlignmentOptions.Left, new Color(0.96f, 0.88f, 0.67f));

        RectTransform statusChip = CreateImage(
            "StatusChip",
            Root,
            new Vector2(0.405f, 0.68f),
            new Vector2(0.555f, 0.93f),
            statusChipSprite,
            new Color(0.14f, 0.30f, 0.12f, 0.92f),
            false);
        statusText = CreateText("Status", statusChip, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f), 17, TextAlignmentOptions.Center, new Color(0.62f, 0.98f, 0.35f));

        CreateImage("CreditsIcon", Root, new Vector2(0.60f, 0.70f), new Vector2(0.645f, 0.92f), creditsIconSprite, Color.white, false);
        costText = CreateText("Cost", Root, new Vector2(0.645f, 0.68f), new Vector2(0.735f, 0.94f), 22, TextAlignmentOptions.Left, Color.white);
        CreateImage("TimeIcon", Root, new Vector2(0.77f, 0.70f), new Vector2(0.815f, 0.92f), timeIconSprite, Color.white, false);
        durationText = CreateText("Duration", Root, new Vector2(0.815f, 0.68f), new Vector2(0.92f, 0.94f), 22, TextAlignmentOptions.Left, Color.white);

        cancelButton = CreateButton(
            "CancelButton",
            "CANCEL",
            Root,
            new Vector2(0.03f, 0.27f),
            new Vector2(0.38f, 0.63f),
            secondaryButtonSprite,
            cancelIconSprite,
            new Color(0.18f, 0.17f, 0.15f, 0.96f));
        rotateButton = CreateButton(
            "RotateButton",
            string.Empty,
            Root,
            new Vector2(0.47f, 0.24f),
            new Vector2(0.55f, 0.65f),
            squareButtonSprite,
            rotateIconSprite,
            new Color(0.13f, 0.14f, 0.13f, 0.92f));
        confirmButton = CreateButton(
            "ConfirmButton",
            "CONFIRM",
            Root,
            new Vector2(0.65f, 0.27f),
            new Vector2(0.97f, 0.63f),
            goldActionButtonSprite,
            confirmIconSprite,
            new Color(0.72f, 0.48f, 0.11f, 0.98f));

        RectTransform instructionStrip = CreateImage(
            "InstructionStrip",
            Root,
            new Vector2(0.03f, 0.04f),
            new Vector2(0.97f, 0.21f),
            instructionStripSprite,
            new Color(0.05f, 0.06f, 0.055f, 0.88f),
            false);
        CreateImage("InfoIcon", instructionStrip, new Vector2(0.27f, 0.13f), new Vector2(0.325f, 0.88f), infoIconSprite, Color.white, false);
        instructionText = CreateText("Instruction", instructionStrip, new Vector2(0.33f, 0.05f), new Vector2(0.83f, 0.90f), 15, TextAlignmentOptions.Left, new Color(0.80f, 0.79f, 0.72f));
    }

    private void EnsureRuntimeLayout()
    {
        if (root == null)
            root = transform as RectTransform;
        if (root == null)
            return;

        _canvasGroup ??= GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Image background = GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();
        if (panelFrameSprite != null)
        {
            ApplySprite(background, panelFrameSprite, Image.Type.Sliced);
            background.color = Color.white;
        }
        else
        {
            background.color = new Color(0.035f, 0.045f, 0.04f, 0.94f);
        }
        background.raycastTarget = true;

        if (HasRequiredLayoutReferences())
            return;

        if (root.childCount == 0)
            BuildGeneratedLayout();
    }

    private bool HasRequiredLayoutReferences()
    {
        return titleText != null &&
               statusText != null &&
               costText != null &&
               durationText != null &&
               instructionText != null &&
               cancelButton != null &&
               rotateButton != null &&
               confirmButton != null;
    }

    private static void ApplyDefaultPlacementAnchors(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.255f, 0.205f);
        rect.anchorMax = new Vector2(0.725f, 0.385f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static TMP_Text CreateText(
        string name,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(10f, fontSize * 0.55f);
        text.fontSizeMax = fontSize;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateImage(
        string name,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Sprite sprite,
        Color color,
        bool raycastTarget)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Image image = imageObject.GetComponent<Image>();
        ApplySprite(image, sprite, Image.Type.Simple);
        image.color = color;
        image.raycastTarget = raycastTarget;
        image.preserveAspect = sprite != null;
        return rect;
    }

    private static Button CreateButton(
        string name,
        string label,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Sprite backgroundSprite,
        Sprite iconSprite,
        Color backgroundColor)
    {
        GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Image image = buttonObject.GetComponent<Image>();
        ApplySprite(image, backgroundSprite, Image.Type.Sliced);
        image.color = backgroundColor;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        if (iconSprite != null)
        {
            Vector2 iconMin = string.IsNullOrEmpty(label) ? new Vector2(0.20f, 0.18f) : new Vector2(0.13f, 0.20f);
            Vector2 iconMax = string.IsNullOrEmpty(label) ? new Vector2(0.80f, 0.82f) : new Vector2(0.30f, 0.80f);
            CreateImage("Icon", rect, iconMin, iconMax, iconSprite, Color.white, false);
        }

        if (!string.IsNullOrEmpty(label))
            CreateText("Label", rect, new Vector2(0.25f, 0.10f), new Vector2(0.93f, 0.90f), 18, TextAlignmentOptions.Center, Color.white).text = label;

        return button;
    }

    private static void ApplySprite(Image image, Sprite sprite, Image.Type type)
    {
        if (image == null || sprite == null)
            return;

        image.sprite = sprite;
        image.type = type;
    }

    private static void SplitPlacementStatus(string rawStatus, out string title, out string status)
    {
        title = "BUILDING";
        status = rawStatus;
        if (string.IsNullOrWhiteSpace(rawStatus))
            return;

        int separator = rawStatus.IndexOf(':');
        if (separator < 0)
            return;

        title = rawStatus.Substring(0, separator).Trim();
        status = rawStatus.Substring(separator + 1).Trim();
    }

    private static string FormatCost(int cost)
    {
        return cost > 0
            ? cost.ToString("N0", CultureInfo.InvariantCulture)
            : "0";
    }

    private static string FormatDuration(float seconds)
    {
        if (seconds <= 0f)
            return "00:00";

        int totalSeconds = Mathf.CeilToInt(seconds);
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }

    private Camera ResolveEventCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }
}
