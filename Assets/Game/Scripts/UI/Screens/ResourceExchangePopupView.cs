using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ResourceExchangePopupView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button closeButton;

        [Header("Header")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text queueCapacityText;
        [SerializeField] private TMP_Text materialsText;
        [SerializeField] private TMP_Text oilText;
        [SerializeField] private TMP_Text fuelText;
        [SerializeField] private TMP_Text rushTicketsText;

        [Header("Tabs")]
        [SerializeField] private Button exportTabButton;
        [SerializeField] private Button importTabButton;
        [SerializeField] private Image exportTabFrameImage;
        [SerializeField] private Image importTabFrameImage;
        [SerializeField] private TMP_Text exportCountText;
        [SerializeField] private TMP_Text importCountText;
        [SerializeField] private Sprite selectedTabFrameSprite;
        [SerializeField] private Sprite defaultTabFrameSprite;

        [Header("Recipes")]
        [SerializeField] private RectTransform recipeContentRoot;
        [SerializeField] private ResourceExchangeRecipeCardView recipeCardTemplate;
        [SerializeField] private ResourceExchangeRecipeCardView[] staticRecipeCards;
        [SerializeField] private Sprite defaultRecipeCardFrameSprite;
        [SerializeField] private Sprite selectedRecipeCardFrameSprite;
        [SerializeField] private Sprite lockedRecipeCardFrameSprite;
        [SerializeField] private Sprite[] recipeThumbnailSprites;

        [Header("Details")]
        [SerializeField] private Image detailThumbnailImage;
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailRouteText;
        [SerializeField] private TMP_Text detailRateText;
        [SerializeField] private TMP_Text detailAmountText;
        [SerializeField] private TMP_Text detailInputText;
        [SerializeField] private TMP_Text detailOutputText;
        [SerializeField] private TMP_Text detailDurationText;
        [SerializeField] private TMP_Text detailRequirementsText;
        [SerializeField] private TMP_Text detailInstructionText;
        [SerializeField] private Image detailWarningImage;
        [SerializeField] private Button amountDecreaseButton;
        [SerializeField] private Button amountIncreaseButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text confirmButtonText;

        [Header("Queue")]
        [SerializeField] private RectTransform queueContentRoot;
        [SerializeField] private ResourceExchangeQueueItemView queueRowTemplate;
        [SerializeField] private ResourceExchangeQueueItemView[] staticQueueRows;
        [SerializeField] private Button rushAllButton;
        [SerializeField] private Button clearCompletedButton;

        [Header("Instruction")]
        [SerializeField] private TMP_Text instructionText;

        private Canvas _cachedCanvas;

        public GameObject PopupRoot => popupRoot != null ? popupRoot : gameObject;
        public Button CloseButton => closeButton;
        public Button ExportTabButton => exportTabButton;
        public Button ImportTabButton => importTabButton;
        public Button ConfirmButton => confirmButton;
        public Button AmountDecreaseButton => amountDecreaseButton;
        public Button AmountIncreaseButton => amountIncreaseButton;
        public Button RushAllButton => rushAllButton;
        public Button ClearCompletedButton => clearCompletedButton;
        public RectTransform RecipeContentRoot => recipeContentRoot;
        public ResourceExchangeRecipeCardView RecipeCardTemplate => recipeCardTemplate;
        public ResourceExchangeRecipeCardView[] StaticRecipeCards => staticRecipeCards;
        public RectTransform QueueContentRoot => queueContentRoot;
        public ResourceExchangeQueueItemView QueueRowTemplate => queueRowTemplate;
        public ResourceExchangeQueueItemView[] StaticQueueRows => staticQueueRows;
        public bool IsOpen => PopupRoot != null && PopupRoot.activeInHierarchy;

        private void Awake()
        {
            if (popupRoot == null)
                popupRoot = gameObject;
        }

        private void OnTransformParentChanged()
        {
            _cachedCanvas = null;
        }

        public void Show()
        {
            if (PopupRoot != null)
                PopupRoot.SetActive(true);
        }

        public void Hide()
        {
            if (PopupRoot != null)
                PopupRoot.SetActive(false);
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            RectTransform rect = PopupRoot != null ? PopupRoot.transform as RectTransform : transform as RectTransform;
            if (rect == null || !rect.gameObject.activeInHierarchy)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, ResolveEventCamera(rect));
        }

        public void ApplyHeader(
            string queueCapacity,
            string materials,
            string oil,
            string fuel,
            string rushTickets)
        {
            SetText(titleText, "RESOURCE EXCHANGE");
            SetText(queueCapacityText, queueCapacity);
            SetText(materialsText, materials);
            SetText(oilText, oil);
            SetText(fuelText, fuel);
            SetText(rushTicketsText, rushTickets);
        }

        public void ApplyTabs(bool exportSelected, int exportCount, int importCount)
        {
            ApplyTab(exportTabFrameImage, selected: exportSelected);
            ApplyTab(importTabFrameImage, selected: !exportSelected);
            SetText(exportCountText, exportCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            SetText(importCountText, importCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public void ApplyDetail(
            string name,
            string route,
            string rate,
            string amount,
            string input,
            string output,
            string duration,
            string requirements,
            string instruction,
            bool confirmEnabled,
            bool warningVisible,
            Sprite thumbnail)
        {
            SetImage(detailThumbnailImage, thumbnail);
            SetText(detailNameText, name);
            SetText(detailRouteText, route);
            SetText(detailRateText, rate);
            SetText(detailAmountText, amount);
            SetText(detailInputText, input);
            SetText(detailOutputText, output);
            SetText(detailDurationText, duration);
            SetText(detailRequirementsText, requirements);
            SetText(detailInstructionText, instruction);
            SetText(instructionText, instruction);
            SetText(confirmButtonText, "CONFIRM");
            SetActive(detailWarningImage, warningVisible);
            if (confirmButton != null)
                confirmButton.interactable = confirmEnabled;
        }

        public void ApplyModel(UiResourceExchangeModel model)
        {
            ApplyHeader(
                model.QueueCapacityText,
                model.MaterialsText,
                model.OilText,
                model.FuelText,
                model.RushTicketsText);
            ApplyTabs(
                model.ActiveTab == UiResourceExchangeTabKind.Export,
                model.ExportRecipeCount,
                model.ImportRecipeCount);
            ApplyRecipeCards(model);
            ApplyDetail(
                model.Detail.Name,
                model.Detail.RouteText,
                model.Detail.RateText,
                model.Detail.AmountText,
                model.Detail.InputCostText,
                model.Detail.OutputPreviewText,
                model.Detail.DurationText,
                model.Detail.RequirementsText,
                model.Detail.InstructionText,
                model.Detail.ConfirmEnabled,
                model.Detail.WarningVisible,
                ResolveRecipeThumbnail(model.SelectedRecipeSlot));
            ApplyQueueRows(model);
            ApplyQueueControls(model.RushAllEnabled, model.ClearCompletedEnabled);
        }

        public void ApplyRecipeCards(UiResourceExchangeModel model)
        {
            ResourceExchangeRecipeCardView[] cards = staticRecipeCards;
            if (cards == null)
                return;

            for (int i = 0; i < cards.Length; i++)
            {
                ResourceExchangeRecipeCardView cardView = cards[i];
                if (cardView == null)
                    continue;

                bool visible = i < model.RecipeCardCount;
                cardView.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                UiResourceExchangeRecipeCardModel card = model.GetRecipeCard(i);
                cardView.Bind(
                    card.Title,
                    card.InputText,
                    card.OutputText,
                    card.DurationText,
                    card.ReasonText,
                    ResolveRecipeThumbnail(i),
                    card.Selected,
                    card.Enabled,
                    card.Locked,
                    card.WarningVisible,
                    defaultRecipeCardFrameSprite,
                    selectedRecipeCardFrameSprite,
                    lockedRecipeCardFrameSprite);
            }
        }

        public void ApplyQueueRows(UiResourceExchangeModel model)
        {
            ResourceExchangeQueueItemView[] rows = staticQueueRows;
            if (rows == null)
                return;

            for (int i = 0; i < rows.Length; i++)
            {
                ResourceExchangeQueueItemView rowView = rows[i];
                if (rowView == null)
                    continue;

                bool visible = i < model.QueueRowCount;
                rowView.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                UiResourceExchangeQueueRowModel row = model.GetQueueRow(i);
                rowView.Bind(
                    row.NumberText,
                    row.Name,
                    row.InputText,
                    row.OutputText,
                    row.TimeText,
                    row.PercentText,
                    row.StateText,
                    row.Progress01,
                    ResolveRecipeThumbnail(i),
                    row.RushEnabled,
                    row.CancelEnabled,
                    row.CompletedVisible,
                    row.WarningVisible);
            }
        }

        public void ApplyQueueControls(bool rushAllEnabled, bool clearCompletedEnabled)
        {
            if (rushAllButton != null)
                rushAllButton.interactable = rushAllEnabled;
            if (clearCompletedButton != null)
                clearCompletedButton.interactable = clearCompletedEnabled;
        }

        public Sprite ResolveRecipeThumbnail(int index)
        {
            if (recipeThumbnailSprites == null || recipeThumbnailSprites.Length == 0)
                return null;

            return recipeThumbnailSprites[Mathf.Clamp(index, 0, recipeThumbnailSprites.Length - 1)];
        }

        private void ApplyTab(Image image, bool selected)
        {
            if (image == null)
                return;

            Sprite sprite = selected ? selectedTabFrameSprite : defaultTabFrameSprite;
            if (sprite != null)
                image.sprite = sprite;
        }

        private Camera ResolveEventCamera(RectTransform rect)
        {
            Canvas canvas = ResolveCanvas(rect);
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }

        private Canvas ResolveCanvas(RectTransform rect)
        {
            if (_cachedCanvas == null && rect != null)
                _cachedCanvas = rect.GetComponentInParent<Canvas>();
            return _cachedCanvas;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value ?? string.Empty;
        }

        private static void SetImage(Image target, Sprite sprite)
        {
            if (target == null)
                return;

            target.sprite = sprite;
            target.enabled = sprite != null;
        }

        private static void SetActive(Image target, bool active)
        {
            if (target != null)
                target.gameObject.SetActive(active);
        }
    }
}
