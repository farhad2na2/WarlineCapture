using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;
using Game.Configs;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudSquadTrayView
    {
        [SerializeField] private RectTransform paginationRoot;
        [SerializeField] private Button previousPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button clearSelectionButton;
        [SerializeField] private TMP_Text pageRangeLabel;
        [SerializeField] private TMP_Text selectionSummaryLabel;
        private UiExpandedSquadPage _lastPagination;
        private bool _hasPaginationState;
        private bool _expandedCardLayoutApplied;
        private readonly CardLayoutSnapshot[] _cardLayouts = new CardLayoutSnapshot[5];

        private struct RectSnapshot
        {
            public Vector2 AnchorMin, AnchorMax, Pivot, AnchoredPosition, SizeDelta;
            public static RectSnapshot Capture(RectTransform rect) => new RectSnapshot
            {
                AnchorMin = rect.anchorMin, AnchorMax = rect.anchorMax, Pivot = rect.pivot,
                AnchoredPosition = rect.anchoredPosition, SizeDelta = rect.sizeDelta
            };
            public void Restore(RectTransform rect)
            {
                rect.anchorMin = AnchorMin; rect.anchorMax = AnchorMax; rect.pivot = Pivot;
                rect.anchoredPosition = AnchoredPosition; rect.sizeDelta = SizeDelta;
            }
        }

        private struct CardLayoutSnapshot
        {
            public RectSnapshot Portrait, Name, Health, Alive;
            public bool HasPortrait, HasName, HasHealth, HasAlive;
            public GameObject LegacyBadge;
            public bool LegacyBadgeActive;
            public float NameFontSize, NameFontMin, NameFontMax;
            public bool NameAutoSizing;
            public TextWrappingModes NameWrapping;
            public TextOverflowModes NameOverflow;
        }
        private string _lastPaginationLocale;
        private bool _lastInputReady;
        private System.Action<int> _changePage;
        private System.Action _clearSelection;
        private System.Action<uint> _selectGroup;

        public Button PreviousPageButton => previousPageButton;
        public Button NextPageButton => nextPageButton;
        public Button ClearSelectionButton => clearSelectionButton;

        private void CaptureCardLayouts()
        {
            for (int i = 0; i < _cardLayouts.Length; i++)
            {
                if (!TryGetCard(i, out Card card) || card.Button == null) continue;
                var state = new CardLayoutSnapshot();
                if (card.PortraitImage != null)
                { state.HasPortrait = true; state.Portrait = RectSnapshot.Capture(card.PortraitImage.rectTransform); }
                if (card.NameStrip != null)
                { state.HasName = true; state.Name = RectSnapshot.Capture(card.NameStrip); }
                RectTransform health = card.HealthFillImage != null ? card.HealthFillImage.transform.parent as RectTransform : null;
                if (health != null)
                { state.HasHealth = true; state.Health = RectSnapshot.Capture(health); }
                if (card.AliveCountLabel != null)
                { state.HasAlive = true; state.Alive = RectSnapshot.Capture(card.AliveCountLabel.rectTransform); }
                state.LegacyBadge = card.Button.transform.Find("Frame/NumberBadge")?.gameObject;
                state.LegacyBadgeActive = state.LegacyBadge != null && state.LegacyBadge.activeSelf;
                if (card.NameLabel != null)
                {
                    state.NameFontSize = card.NameLabel.fontSize;
                    state.NameFontMin = card.NameLabel.fontSizeMin;
                    state.NameFontMax = card.NameLabel.fontSizeMax;
                    state.NameAutoSizing = card.NameLabel.enableAutoSizing;
                    state.NameWrapping = card.NameLabel.textWrappingMode;
                    state.NameOverflow = card.NameLabel.overflowMode;
                }
                _cardLayouts[i] = state;
            }
        }

        private static void PlaceExpanded(RectTransform rect, float top, float height, float inset)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-2f * inset, height);
        }

        private void ApplyExpandedCardLayout(bool expanded)
        {
            if (_expandedCardLayoutApplied == expanded) return;
            for (int i = 0; i < _cardLayouts.Length; i++)
            {
                if (!TryGetCard(i, out Card card) || card.Button == null) continue;
                CardLayoutSnapshot state = _cardLayouts[i];
                RectTransform health = card.HealthFillImage != null ? card.HealthFillImage.transform.parent as RectTransform : null;
                if (expanded)
                {
                    if (state.HasPortrait) PlaceExpanded(card.PortraitImage.rectTransform, 8f, 110f, 7f);
                    if (state.HasName) PlaceExpanded(card.NameStrip, 122f, 30f, 7f);
                    if (state.HasHealth && health != null) PlaceExpanded(health, 156f, 10f, 9f);
                    if (state.HasAlive) PlaceExpanded(card.AliveCountLabel.rectTransform, 170f, 19f, 9f);
                    if (state.LegacyBadge != null) state.LegacyBadge.SetActive(false);
                    if (card.NameLabel != null)
                    {
                        card.NameLabel.alignment = TextAlignmentOptions.Center;
                        card.NameLabel.enableAutoSizing = true;
                        card.NameLabel.fontSizeMin = 11f;
                        card.NameLabel.fontSizeMax = 18f;
                        card.NameLabel.textWrappingMode = TextWrappingModes.Normal;
                        card.NameLabel.overflowMode = TextOverflowModes.Ellipsis;
                    }
                }
                else
                {
                    if (state.HasPortrait) state.Portrait.Restore(card.PortraitImage.rectTransform);
                    if (state.HasName) state.Name.Restore(card.NameStrip);
                    if (state.HasHealth && health != null) state.Health.Restore(health);
                    if (state.HasAlive) state.Alive.Restore(card.AliveCountLabel.rectTransform);
                    if (state.LegacyBadge != null) state.LegacyBadge.SetActive(state.LegacyBadgeActive);
                    if (card.NameLabel != null)
                    {
                        card.NameLabel.enableAutoSizing = state.NameAutoSizing;
                        card.NameLabel.fontSize = state.NameFontSize;
                        card.NameLabel.fontSizeMin = state.NameFontMin;
                        card.NameLabel.fontSizeMax = state.NameFontMax;
                        card.NameLabel.textWrappingMode = state.NameWrapping;
                        card.NameLabel.overflowMode = state.NameOverflow;
                    }
                }
            }
            _expandedCardLayoutApplied = expanded;
        }
        public void ConfigureExpandedPagination(System.Action<int> changePage, System.Action clearSelection)
        { _changePage = changePage; _clearSelection = clearSelection; }
        public void ConfigureExpandedGroupSelection(System.Action<uint> selectGroup) => _selectGroup = selectGroup;

        private void BindPagination()
        {
            UnbindPagination();
            if (previousPageButton != null) previousPageButton.onClick.AddListener(OnPreviousPage);
            if (nextPageButton != null) nextPageButton.onClick.AddListener(OnNextPage);
            if (clearSelectionButton != null) clearSelectionButton.onClick.AddListener(OnClearSelection);
        }

        private void UnbindPagination()
        {
            if (previousPageButton != null) previousPageButton.onClick.RemoveListener(OnPreviousPage);
            if (nextPageButton != null) nextPageButton.onClick.RemoveListener(OnNextPage);
            if (clearSelectionButton != null) clearSelectionButton.onClick.RemoveListener(OnClearSelection);
            _hasPaginationState = false;
        }

        private void RefreshPagination(UiExpandedSquadPage page, bool inputReady)
        {
            if (paginationRoot == null) return;
            string locale = UiShellRuntimeGateway.Localization.CurrentLocaleCode;
            if (_hasPaginationState && locale == _lastPaginationLocale &&
                _lastPagination.PageIndex == page.PageIndex && _lastPagination.PageCount == page.PageCount &&
                _lastPagination.TotalGroups == page.TotalGroups && _lastPagination.SelectedGroups == page.SelectedGroups &&
                _lastPagination.SelectedOffPage == page.SelectedOffPage && _lastPagination.PreviousPage == page.PreviousPage &&
                _lastPagination.NextPage == page.NextPage && _lastInputReady == inputReady) return;
            paginationRoot.gameObject.SetActive(true);
            bool manyPages = page.TotalGroups > 5;
            V3GradientGraphic surface = paginationRoot.GetComponent<V3GradientGraphic>();
            if (surface != null) surface.enabled = true;
            bool rtl = UiShellRuntimeGateway.Localization.IsRightToLeft;
            var cardLayout = cards.Length > 0 && cards[0]?.Button != null
                ? cards[0].Button.GetComponentInParent<HorizontalLayoutGroup>() : null;
            if (cardLayout != null) cardLayout.reverseArrangement = rtl;
            Transform row = paginationRoot.Find("NavigationRow");
            Transform spacer = row != null ? row.Find("FlexibleSpacer") : null;
            if (spacer != null) spacer.gameObject.SetActive(false);
            if (previousPageButton != null)
            {
                previousPageButton.gameObject.SetActive(manyPages);
                previousPageButton.interactable = inputReady && page.PreviousPage && _changePage != null;
            }
            if (nextPageButton != null)
            {
                nextPageButton.gameObject.SetActive(manyPages);
                nextPageButton.interactable = inputReady && page.NextPage && _changePage != null;
            }
            if (pageRangeLabel != null)
            {
                pageRangeLabel.gameObject.SetActive(true);
                pageRangeLabel.margin = page.SelectedGroups > 0 || page.TotalGroups == 0
                    ? new Vector4(0f, 1f, 0f, 36f) : Vector4.zero;
                int first = page.TotalGroups == 0 ? 0 : page.PageIndex * 5 + 1;
                int last = Mathf.Min(page.TotalGroups, (page.PageIndex + 1) * 5);
                UiLocalizedText.Set(pageRangeLabel, UiShellRuntimeGateway.Localization.Format("ui.skirmish.squads.range",
                    rtl ? "گروه‌های {0} تا {1} از {2}" : "SQUADS {0}–{1} OF {2}", first, last, page.TotalGroups));
            }
            if (clearSelectionButton != null)
            {
                clearSelectionButton.gameObject.SetActive(page.SelectedGroups > 0);
                clearSelectionButton.interactable = inputReady && page.SelectedGroups > 0 && _clearSelection != null;
                TMP_Text clearLabel = clearSelectionButton.GetComponentInChildren<TMP_Text>(true);
                if (clearLabel != null)
                {
                    UiLocalizedText.Set(clearLabel, UiShellRuntimeGateway.Localization.Get("ui.skirmish.squads.clear", "CLEAR"));
                    clearLabel.raycastTarget = false;
                }
            }
            if (previousPageButton != null)
                previousPageButton.gameObject.name = UiShellRuntimeGateway.Localization.Get("ui.skirmish.squads.previous", "Previous squads");
            if (nextPageButton != null)
                nextPageButton.gameObject.name = UiShellRuntimeGateway.Localization.Get("ui.skirmish.squads.next", "Next squads");
            if (selectionSummaryLabel != null)
            {
                selectionSummaryLabel.gameObject.SetActive(page.SelectedGroups > 0 || page.TotalGroups == 0);
                string summary = page.TotalGroups == 0
                    ? UiShellRuntimeGateway.Localization.Get("ui.skirmish.squads.empty", "NO SQUADS AVAILABLE")
                    : page.SelectedGroups == 0
                        ? UiShellRuntimeGateway.Localization.Get("ui.skirmish.squads.none_selected", "Tap squads to select")
                        : page.SelectedOffPage > 0
                            ? UiShellRuntimeGateway.Localization.Format("ui.skirmish.squads.selected_off_page", "{0} selected • {1} off page", page.SelectedGroups, page.SelectedOffPage)
                            : UiShellRuntimeGateway.Localization.Format("ui.skirmish.squads.selected", "{0} selected", page.SelectedGroups);
                UiLocalizedText.Set(selectionSummaryLabel, summary);
                selectionSummaryLabel.enableAutoSizing = false;
                selectionSummaryLabel.fontSize = 24f;
                selectionSummaryLabel.overflowMode = TextOverflowModes.Overflow;
            }
            ArrangePaginationRow(row, rtl);
            _lastPagination = page;
            _lastPaginationLocale = locale;
            _lastInputReady = inputReady;
            _hasPaginationState = true;
        }

        private void ArrangePaginationRow(Transform row, bool rtl)
        {
            if (row == null || previousPageButton == null || nextPageButton == null || clearSelectionButton == null || pageRangeLabel == null) return;
            previousPageButton.transform.SetAsFirstSibling();
            pageRangeLabel.transform.SetSiblingIndex(1);
            nextPageButton.transform.SetSiblingIndex(2);
            clearSelectionButton.transform.SetSiblingIndex(3);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout != null) layout.reverseArrangement = rtl;
            Vector3 arrowScale = new Vector3(rtl ? -1f : 1f, 1f, 1f);
            previousPageButton.transform.localScale = arrowScale;
            nextPageButton.transform.localScale = arrowScale;
        }

        private void HidePagination()
        {
            if (paginationRoot != null) paginationRoot.gameObject.SetActive(false);
            var cardLayout = cards.Length > 0 && cards[0]?.Button != null
                ? cards[0].Button.GetComponentInParent<HorizontalLayoutGroup>() : null;
            if (cardLayout != null) cardLayout.reverseArrangement = false;
            _hasPaginationState = false;
        }

        private void OnPreviousPage() => _changePage?.Invoke(-1);
        private void OnNextPage() => _changePage?.Invoke(1);
        private void OnClearSelection() => _clearSelection?.Invoke();

        private void CapturePressedGroup(int index, UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (eventData == null || eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Left) return;
            if (index < 0 || index >= _pressedGroupIds.Length || !TryGetCard(index, out Card card) ||
                card.Button == null || !card.Button.interactable) return;
            _pressedGroupIds[index] = _displayedGroupIds[index];
        }

        private void SetExpandedSelected(int index, bool selected)
        {
            if (!TryGetCard(index, out Card card) || card.Button == null) return;
            V3GradientGraphic frame = card.Button.GetComponentInChildren<V3GradientGraphic>(true);
            if (frame != null) frame.SetBorder(selected ? V3ExpandedSelectedBorderColor : V3NormalBorderColor, selected ? 4f : 3f);
        }
        private static readonly Color V3ExpandedSelectedBorderColor = new Color32(0, 196, 70, 255);
    }
}
