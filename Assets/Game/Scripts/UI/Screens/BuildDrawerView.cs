using TMPro;
using UnityEngine;
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
        [SerializeField] private TMP_Text creditsCostText;
        [SerializeField] private TMP_Text suppliesCostText;
        [SerializeField] private TMP_Text productionTimeText;
        [SerializeField] private TMP_Text placementText;
        [SerializeField] private TMP_Text requirementsText;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button orderButton;
        [SerializeField] private TMP_Text primaryActionLabelText;

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
        }

        public void BindDetail(
            string displayName,
            string role,
            string description,
            string creditsCost,
            string suppliesCost,
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
            SetText(creditsCostText, creditsCost);
            SetText(suppliesCostText, suppliesCost);
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

        private static void SetImage(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
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
        }
    }
}
