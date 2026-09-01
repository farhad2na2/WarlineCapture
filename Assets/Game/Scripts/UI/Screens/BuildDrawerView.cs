using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BuildDrawerView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject drawerRoot;
        [SerializeField] private Button closeButton;

        [Header("Tabs")]
        [SerializeField] private BuildDrawerTabView[] tabs;
        [SerializeField] private Sprite selectedTabFrameSprite;
        [SerializeField] private Sprite normalTabFrameSprite;

        [Header("Catalog")]
        [SerializeField] private RectTransform itemContentRoot;
        [SerializeField] private BuildDrawerItemView itemTemplate;
        [SerializeField] private Sprite selectedItemFrameSprite;

        [Header("Detail")]
        [SerializeField] private Image previewImage;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private TMP_Text descriptionText;
        [FormerlySerializedAs("creditsCostText"), SerializeField] private TMP_Text materialsCostText;
        [FormerlySerializedAs("suppliesCostText"), SerializeField] private TMP_Text fuelCostText;
        [SerializeField] private TMP_Text productionTimeText;
        [SerializeField] private TMP_Text placementText;
        [SerializeField] private TMP_Text requirementsText;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button orderButton;
        [SerializeField] private TMP_Text primaryActionLabelText;

        [Header("Availability")]
        [SerializeField] private GameObject unavailablePanel;
        [SerializeField] private TMP_Text unavailableTitleText;
        [SerializeField] private TMP_Text unavailableDescriptionText;

        [Header("Instruction")]
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private Image instructionIcon;
        [SerializeField] private Sprite instructionInfoIcon;
        [SerializeField] private Sprite instructionReadyIcon;
        [SerializeField] private Sprite instructionWarningIcon;
        [SerializeField] private Sprite instructionErrorIcon;

        [Header("Queue")]
        [SerializeField] private GameObject productionPanel;
        [SerializeField] private GameObject productionPanelActive;
        [SerializeField] private GameObject noProductionView;
        [SerializeField] private TMP_Text noProductionText;
        [SerializeField] private RectTransform queueContentRoot;
        [SerializeField] private BuildDrawerQueueItemView queuedItemTemplate;
        [SerializeField] private BuildDrawerQueueItemView activeItemView;
        [SerializeField] private Slider queueProgressSlider;
        [SerializeField] private TMP_Text queuePercentageText;
        [SerializeField] private TMP_Text queueTimeText;
        [SerializeField] private TMP_Text queueNumbersText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button rushButton;
        [SerializeField] private Button clearButton;

        public GameObject DrawerRoot => drawerRoot;
        public Button CloseButton => closeButton;
        public BuildDrawerTabView[] Tabs => tabs;
        public RectTransform ItemContentRoot => itemContentRoot;
        public BuildDrawerItemView ItemTemplate => itemTemplate;
        public Sprite SelectedItemFrameSprite => selectedItemFrameSprite;
        public Button PrimaryActionButton => buildButton != null ? buildButton : orderButton;
        public Button BuildButton => buildButton;
        public Button OrderButton => orderButton;
        public RectTransform QueueContentRoot => queueContentRoot;
        public BuildDrawerQueueItemView QueuedItemTemplate => queuedItemTemplate;
        public BuildDrawerQueueItemView ActiveItemView => activeItemView;
        public Button CancelButton => cancelButton;
        public Button RushButton => rushButton;
        public Button ClearButton => clearButton;
        public TMP_Text InstructionText => instructionText;
        public Image InstructionIcon => instructionIcon;
        public bool IsOpen => drawerRoot != null ? drawerRoot.activeInHierarchy : gameObject.activeInHierarchy;

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            RectTransform rect = drawerRoot != null
                ? drawerRoot.transform as RectTransform
                : transform as RectTransform;
            if (rect == null || !rect.gameObject.activeInHierarchy)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, null);
        }

        public void ApplyTabVisuals(BuildDrawerCategory selectedCategory, int[] counts, bool[] enabledStates)
        {
            if (tabs == null)
                return;

            for (int i = 0; i < tabs.Length; i++)
            {
                BuildDrawerTabView tab = tabs[i];
                if (tab == null)
                    continue;

                int count = counts != null && i < counts.Length ? counts[i] : 0;
                bool interactable = enabledStates == null || i >= enabledStates.Length || enabledStates[i];
                tab.Apply(
                    tab.Category == selectedCategory,
                    interactable,
                    count,
                    selectedTabFrameSprite,
                    normalTabFrameSprite);
            }

            bool anyAvailable = false;
            if (enabledStates != null)
            {
                for (int i = 0; i < enabledStates.Length; i++)
                    anyAvailable |= enabledStates[i];
            }

            ApplyAvailability(anyAvailable);
        }

        public void ApplyAvailability(
            bool available,
            string title = "BUILD UNAVAILABLE",
            string description = "Mission does not allow construction.")
        {
            if (unavailablePanel != null)
                unavailablePanel.SetActive(!available);
            SetText(unavailableTitleText, title);
            SetText(unavailableDescriptionText, description);
            if (previewImage != null)
                previewImage.color = available ? Color.white : new Color(.46f, .51f, .53f, 1f);
            if (primaryActionLabelText != null)
                primaryActionLabelText.color = available ? Color.white : new Color(.52f, .55f, .56f, 1f);

            if (itemContentRoot != null)
            {
                BuildDrawerItemView[] items = itemContentRoot.GetComponentsInChildren<BuildDrawerItemView>(true);
                for (int i = 0; i < items.Length; i++)
                    items[i].SetInteractable(available);

                for (int i = 0; i < itemContentRoot.childCount; i++)
                {
                    Transform card = itemContentRoot.GetChild(i);
                    Transform overlay = card.Find("DisabledOverlay");
                    if (overlay != null)
                        overlay.gameObject.SetActive(!available);

                    Image art = card.Find("ArtClip/Thumb")?.GetComponent<Image>();
                    if (art != null)
                        art.color = available ? Color.white : new Color(.42f, .47f, .49f, 1f);

                    if (card.GetComponent<BuildDrawerItemView>() == null)
                    {
                        V3GradientGraphic cardGradient = card.GetComponent<V3GradientGraphic>();
                        if (cardGradient != null)
                        {
                            cardGradient.Configure(
                                available ? new Color32(53, 65, 70, 252) : new Color32(43, 49, 52, 255),
                                available ? new Color32(3, 8, 10, 254) : new Color32(10, 15, 17, 255),
                                available ? new Color32(112, 127, 131, 255) : new Color32(76, 85, 88, 255),
                                3f);
                        }
                    }
                }
            }

            if (!available)
                ApplyPrimaryActionState(false);
        }

        public void BindDetail(
            string displayName,
            string role,
            string description,
            string materialsCost,
            string fuelCost,
            string productionTime,
            string placement,
            string requirements,
            Sprite preview,
            Sprite thumbnail,
            string actionLabel,
            bool actionEnabled)
        {
            SetText(nameText, displayName);
            SetText(roleText, role);
            SetText(descriptionText, description);
            SetCost(materialsCostText, materialsCost);
            SetCost(fuelCostText, fuelCost);
            SetText(productionTimeText, productionTime);
            SetText(placementText, placement);
            SetText(requirementsText, requirements);
            SetImage(previewImage, preview);
            SetImage(thumbnailImage, thumbnail);
            SetText(primaryActionLabelText, actionLabel);
            ApplyPrimaryActionState(actionEnabled);
        }

        public void ApplyQueueSummary(bool hasProduction, float progress01, string percentage, string time, string numbers)
        {
            if (productionPanel != null)
                productionPanel.SetActive(true);

            if (productionPanelActive != null)
                productionPanelActive.SetActive(hasProduction);

            if (noProductionView != null)
                noProductionView.SetActive(!hasProduction);

            if (!hasProduction)
                SetText(noProductionText, "NO PRODUCTION QUEUED");

            if (queueProgressSlider != null)
                queueProgressSlider.value = Mathf.Clamp01(progress01);

            SetText(queuePercentageText, percentage);
            SetText(queueTimeText, time);
            SetText(queueNumbersText, numbers);
        }

        public void ApplySecondaryQueueControls(bool cancelEnabled, bool rushEnabled, bool clearEnabled)
        {
            if (cancelButton != null)
                cancelButton.interactable = cancelEnabled;

            if (rushButton != null)
                rushButton.interactable = rushEnabled;

            if (clearButton != null)
                clearButton.interactable = clearEnabled;
        }

        public void ApplyInstruction(string text, BuildDrawerInstructionSeverity severity)
        {
            if (instructionText != null)
                instructionText.text = text ?? string.Empty;

            if (instructionIcon != null)
            {
                Sprite icon = ResolveInstructionIcon(severity);
                instructionIcon.sprite = icon;
                instructionIcon.enabled = icon != null;
            }
        }

        private Sprite ResolveInstructionIcon(BuildDrawerInstructionSeverity severity)
        {
            return severity switch
            {
                BuildDrawerInstructionSeverity.Ready => instructionReadyIcon,
                BuildDrawerInstructionSeverity.Warning => instructionWarningIcon,
                BuildDrawerInstructionSeverity.Error => instructionErrorIcon,
                _ => instructionInfoIcon
            };
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }

        private static void SetCost(TMP_Text text, string value)
        {
            if (text == null)
                return;

            bool visible = !string.IsNullOrWhiteSpace(value);
            text.text = visible ? value : string.Empty;
            Transform costGroup = text.transform.parent;
            if (costGroup != null)
                costGroup.gameObject.SetActive(visible);
        }

        private static void SetImage(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
            AspectRatioFitter fitter = image.GetComponent<AspectRatioFitter>();
            if (fitter != null && sprite != null)
            {
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            }
        }

        private void ApplyPrimaryActionState(bool actionEnabled)
        {
            Button primaryButton = PrimaryActionButton;
            if (buildButton != null)
            {
                buildButton.gameObject.SetActive(buildButton == primaryButton);
                buildButton.interactable = buildButton == primaryButton && actionEnabled;
            }

            if (orderButton != null)
            {
                orderButton.gameObject.SetActive(orderButton == primaryButton);
                orderButton.interactable = orderButton == primaryButton && actionEnabled;
            }

            V3GradientGraphic gradient = primaryButton != null
                ? primaryButton.GetComponent<V3GradientGraphic>()
                : null;
            if (gradient != null)
            {
                gradient.Configure(
                    actionEnabled ? new Color32(61, 166, 63, 255) : new Color32(64, 70, 72, 255),
                    actionEnabled ? new Color32(9, 73, 28, 255) : new Color32(21, 25, 27, 255),
                    actionEnabled ? new Color32(92, 224, 79, 255) : new Color32(90, 99, 102, 255),
                    3f);
            }
        }
    }
}
