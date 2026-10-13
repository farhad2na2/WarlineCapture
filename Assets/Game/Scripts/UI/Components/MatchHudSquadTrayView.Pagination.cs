using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudSquadTrayView
    {
        // Retired serialized fields allow old content prefabs to load. The builder removes
        // their objects; runtime also hides chrome in content saved before this change.
        [SerializeField] private RectTransform paginationRoot;
        [SerializeField] private Button previousPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button clearSelectionButton;
        [SerializeField] private TMP_Text pageRangeLabel;
        [SerializeField] private TMP_Text selectionSummaryLabel;
        private Action<int> _changePage;
        private Action<uint> _selectGroup;
        private Action<List<UiMatchHudSquadTrayCardModel>> _readRoster;
        private Func<uint, Sprite> _resolveRosterPortrait;
        private readonly List<UiMatchHudSquadTrayCardModel> _rosterModels = new();
        private readonly List<Card> _rosterCards = new();
        private readonly List<uint> _rosterIds = new();
        private readonly Dictionary<int, uint> _rosterPresses = new();
        private ScrollRect _squadScroll;
        private RectTransform _scrollContent;
        private Button _scrollPrevious, _scrollNext;
        private bool _rosterActive, _syncingScroll;
        private int _logicalPage;
        private float _nextRosterRefresh;
        private bool _rosterInputReady;
        private Sprite[] _categoryPortraits;
        private const float CardWidth = 135f, CardGap = 5f, CardInset = 4f;
        private const float CardStride = CardWidth + CardGap;
        private int _geometryCount = -1;
        private Vector2 _geometryViewport;
        private bool _geometryRtl;

        public Button PreviousPageButton => _scrollPrevious;
        public Button NextPageButton => _scrollNext;
        // ARIA changes its selection by tapping the same checked cards as a player.
        // This returns a presented card (or a scroll edge leading to it), never a Clear button.
        public Button ClearSelectionButton
        {
            get
            {
                for (int i = 0; i < _rosterModels.Count; i++)
                {
                    if (!_rosterModels[i].Selected) continue;
                    var button = _rosterCards[i].Button;
                    if (IsCardPresented(button)) return button;
                    bool before = i < _logicalPage * 5;
                    return before ? _scrollPrevious : _scrollNext;
                }
                return null;
            }
        }

        public void ConfigureExpandedRoster(Action<List<UiMatchHudSquadTrayCardModel>> readRoster,
            Func<uint, Sprite> resolvePortrait)
        { _readRoster = readRoster; _resolveRosterPortrait = resolvePortrait; EnsureScrollingLayout(); }
        public void ConfigureExpandedPagination(Action<int> changePage, Action clearSelection)
        { _changePage = changePage; } // Selection reset is now ordinary card deselection.
        public void ConfigureExpandedGroupSelection(Action<uint> selectGroup) => _selectGroup = selectGroup;
        private void CaptureCardLayouts() { }
        private void ApplyExpandedCardLayout(bool expanded) { }
        private void BindPagination()
        {
            EnsureScrollingLayout();
            for (int i = 0; i < _rosterIds.Count; i++) _rosterIds[i] = 0;
            _nextRosterRefresh = 0;
        }
        private void UnbindPagination()
        {
            _rosterPresses.Clear();
            for (int i = cards.Length; i < _rosterCards.Count; i++)
                _rosterCards[i].Button.onClick.RemoveAllListeners();
        }
        private void HidePagination() { if (paginationRoot != null) paginationRoot.gameObject.SetActive(false); }
        private void RefreshPagination(UiExpandedSquadPage page, bool inputReady) => HidePagination();

        private void EnsureScrollingLayout()
        {
            HidePagination();
            if (_squadScroll != null || cards.Length == 0 || cards[0]?.Button == null) return;
            var viewport = transform as RectTransform;
            if (viewport == null) return;
            CreateCardLabels();
            var oldLayout = GetComponent<HorizontalLayoutGroup>();
            if (oldLayout != null) oldLayout.enabled = false;
            if (GetComponent<RectMask2D>() == null) gameObject.AddComponent<RectMask2D>();
            var contentObject = new GameObject("SquadScrollContent", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contentObject.layer = gameObject.layer;
            _scrollContent = contentObject.GetComponent<RectTransform>();
            _scrollContent.SetParent(transform, false);
            _scrollContent.anchorMin = new Vector2(0f, 0f);
            _scrollContent.anchorMax = new Vector2(0f, 1f);
            _scrollContent.pivot = new Vector2(0f, .5f);
            var layout = contentObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = CardGap;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            _categoryPortraits = new Sprite[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                if (card?.Button == null) continue;
                card.Button.transform.SetParent(_scrollContent, false);
                _categoryPortraits[i] = card.PortraitImage != null ? card.PortraitImage.sprite : null;
                ConfigurePortraitCard(card);
                _rosterCards.Add(card);
                _rosterIds.Add(0);
            }
            _squadScroll = GetComponent<ScrollRect>() ?? gameObject.AddComponent<ScrollRect>();
            _squadScroll.viewport = viewport;
            _squadScroll.content = _scrollContent;
            _squadScroll.horizontal = true;
            _squadScroll.vertical = false;
            _squadScroll.movementType = ScrollRect.MovementType.Clamped;
            _squadScroll.inertia = true;
            _squadScroll.scrollSensitivity = CardStride;
            _squadScroll.onValueChanged.AddListener(OnSquadScrollChanged);
            _scrollPrevious = CreateScrollEdge("PreviousSquads", false);
            _scrollNext = CreateScrollEdge("NextSquads", true);
            _scrollPrevious.onClick.AddListener(() => ScrollPage(-1));
            _scrollNext.onClick.AddListener(() => ScrollPage(1));
            RefreshScrollGeometry();
        }

        private void ConfigurePortraitCard(Card card)
        {
            var element = card.Button.GetComponent<LayoutElement>() ?? card.Button.gameObject.AddComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = CardWidth;
            element.flexibleWidth = 0;
            if (card.PortraitImage != null)
            {
                var clipObject = new GameObject("PortraitClip", typeof(RectTransform), typeof(RectMask2D));
                clipObject.layer = gameObject.layer;
                var clip = clipObject.GetComponent<RectTransform>();
                clip.SetParent(card.PortraitImage.transform.parent, false);
                clip.SetSiblingIndex(1); // Keep the gradient background behind the portrait.
                clip.anchorMin = Vector2.zero; clip.anchorMax = Vector2.one;
                clip.offsetMin = new Vector2(4, 4); clip.offsetMax = new Vector2(-4, -4);
                card.PortraitImage.rectTransform.SetParent(clip, false);
                card.PortraitImage.rectTransform.anchorMin = Vector2.zero;
                card.PortraitImage.rectTransform.anchorMax = Vector2.one;
                card.PortraitImage.rectTransform.offsetMin = card.PortraitImage.rectTransform.offsetMax = Vector2.zero;
                var aspect = card.PortraitImage.GetComponent<AspectRatioFitter>() ?? card.PortraitImage.gameObject.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                aspect.aspectRatio = card.PortraitImage.sprite != null
                    ? card.PortraitImage.sprite.rect.width / card.PortraitImage.sprite.rect.height : 1f;
                card.PortraitImage.raycastTarget = false;
            }
            if (card.NameStrip != null)
            {
                card.NameStrip.anchorMin = new Vector2(0, 0); card.NameStrip.anchorMax = new Vector2(1, 0);
                card.NameStrip.pivot = new Vector2(.5f, 0);
                card.NameStrip.anchoredPosition = new Vector2(0, 14);
                card.NameStrip.sizeDelta = new Vector2(-10, 26);
                card.NameStrip.SetAsLastSibling();
            }
            if (card.NameLabel != null)
            {
                card.NameLabel.enableAutoSizing = true;
                card.NameLabel.fontSizeMin = 11; card.NameLabel.fontSizeMax = 18;
                card.NameLabel.textWrappingMode = TextWrappingModes.NoWrap;
                card.NameLabel.overflowMode = TextOverflowModes.Ellipsis;
            }
            var health = card.HealthFillImage != null ? card.HealthFillImage.transform.parent as RectTransform : null;
            if (health != null)
            {
                card.HealthFillImage.sprite = null;
                card.HealthFillImage.color = new Color32(75, 205, 45, 255);
                card.HealthFillImage.type = Image.Type.Filled;
                card.HealthFillImage.fillMethod = Image.FillMethod.Horizontal;
                var fillRect = card.HealthFillImage.rectTransform;
                fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = new Vector2(1, 1); fillRect.offsetMax = new Vector2(-1, -1);
                health.anchorMin = new Vector2(0, 0); health.anchorMax = new Vector2(1, 0);
                health.pivot = new Vector2(.5f, 0); health.anchoredPosition = new Vector2(0, 6);
                health.sizeDelta = new Vector2(-14, 6);
            }
            if (card.AliveCountLabel != null) card.AliveCountLabel.gameObject.SetActive(false);
            if (card.DisabledWash != null) card.DisabledWash.gameObject.SetActive(false);
        }

        private Button CreateScrollEdge(string name, bool right)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.layer = gameObject.layer; go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(right ? 1 : 0, .5f);
            rect.pivot = new Vector2(right ? 1 : 0, .5f); rect.sizeDelta = new Vector2(44, 56);
            go.GetComponent<Image>().color = new Color(0, 0, 0, .35f);
            // Two white strokes avoid missing chevron glyphs in localized fonts.
            for (int i = 0; i < 2; i++)
            {
                var stroke = new GameObject("Chevron" + i, typeof(RectTransform), typeof(Image));
                stroke.layer = go.layer; stroke.transform.SetParent(rect, false);
                var line = stroke.GetComponent<RectTransform>();
                line.anchorMin = line.anchorMax = new Vector2(.5f, .5f);
                line.sizeDelta = new Vector2(4, 17);
                line.anchoredPosition = new Vector2(0, i == 0 ? 5 : -5);
                line.localRotation = Quaternion.Euler(0, 0, (i == 0 ? 45 : -45) * (right ? 1 : -1));
                stroke.GetComponent<Image>().raycastTarget = false;
            }
            return go.GetComponent<Button>();
        }

        private Card CloneRosterCard()
        {
            var template = cards[0];
            var clone = Instantiate(template.Button.gameObject, _scrollContent);
            clone.name = "RosterCard" + (_rosterCards.Count + 1);
            Transform Map(Transform original)
            {
                if (original == null) return null;
                string path = original.name;
                while (original.parent != template.Button.transform)
                { original = original.parent; path = original.name + "/" + path; }
                return clone.transform.Find(path);
            }
            T Component<T>(T original) where T : Component => original != null ? Map(original.transform)?.GetComponent<T>() : null;
            var card = new Card { Button = clone.GetComponent<Button>(), FrameImage = Component(template.FrameImage),
                PortraitImage = Component(template.PortraitImage), NameStrip = Component(template.NameStrip),
                NameLabel = Component(template.NameLabel), DisabledWash = Component(template.DisabledWash),
                OrdinalBadge = Component(template.OrdinalBadge), SelectedCheck = template.SelectedCheck != null ? Map(template.SelectedCheck.transform)?.gameObject : null,
                AliveCountLabel = Component(template.AliveCountLabel), HealthFillImage = Component(template.HealthFillImage) };
            var cue = clone.transform.Find("AriaButtonGuidance");
            if (cue != null) cue.gameObject.SetActive(false);
            card.Button.onClick.RemoveAllListeners();
            _rosterCards.Add(card); _rosterIds.Add(0);
            return card;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void TraceSquadInput(string detail)
        {
#if UNITY_EDITOR
            if (Environment.GetEnvironmentVariable("WARLINE_SQUAD_INPUT_DIAGNOSTIC") == "1")
                Debug.Log("[SquadCardInput] " + detail);
#endif
        }

        private void BindRosterCard(int index)
        {
            var card = _rosterCards[index];
            card.Button.onClick.RemoveAllListeners();
            var relay = card.Button.GetComponent<MatchHudSquadCardPressRelay>() ?? card.Button.gameObject.AddComponent<MatchHudSquadCardPressRelay>();
            relay.Bind(e =>
            {
                if (e.button != PointerEventData.InputButton.Left) return;
                _rosterPresses[index] = _rosterIds[index];
                TraceSquadInput("down index=" + index + " group=" + _rosterIds[index]);
            });
            relay.ConfigureScroll(_squadScroll, () => _rosterPresses.Remove(index));
            card.Button.onClick.AddListener(() =>
            {
                TraceSquadInput("click index=" + index + " group=" + _rosterIds[index] + " dragged=" + relay.Dragged +
                    " captured=" + _rosterPresses.ContainsKey(index) + " callback=" + (_selectGroup != null));
                if (relay.Dragged || !_rosterPresses.TryGetValue(index, out uint pressed)) return;
                _rosterPresses.Remove(index);
                if (pressed == 0 || pressed != _rosterIds[index] || index >= _rosterModels.Count || !card.Button.interactable) return;
                UIAudioEventGateway.Raise(UIAudioEventKind.ButtonPrimaryClick);
                _selectGroup?.Invoke(pressed);
                _nextRosterRefresh = 0;
            });
        }

        private void RefreshScrollingRoster(bool inputReady)
        {
            EnsureScrollingLayout(); HidePagination();
            if (_readRoster == null || _squadScroll == null) return;
            if (_rosterActive && inputReady == _rosterInputReady && Time.unscaledTime < _nextRosterRefresh) return;
            _rosterInputReady = inputReady;
            _nextRosterRefresh = Time.unscaledTime + .1f;
            bool entering = !_rosterActive;
            _rosterActive = true;
            _readRoster(_rosterModels);
            while (_rosterCards.Count < _rosterModels.Count) CloneRosterCard();
            for (int i = 0; i < _rosterCards.Count; i++)
            {
                var card = _rosterCards[i];
                bool visible = i < _rosterModels.Count && _rosterModels[i].Visible;
                card.Button.gameObject.SetActive(visible);
                if (!visible) { _rosterIds[i] = 0; continue; }
                var model = _rosterModels[i];
                if (entering || _rosterIds[i] != model.GroupId)
                {
                    _rosterPresses.Remove(i); BindRosterCard(i);
                    var sprite = _resolveRosterPortrait?.Invoke(model.GroupId);
                    if (card.PortraitImage != null)
                    {
                        card.PortraitImage.sprite = sprite; card.PortraitImage.enabled = sprite != null;
                        if (sprite != null) card.PortraitImage.GetComponent<AspectRatioFitter>().aspectRatio = sprite.rect.width / sprite.rect.height;
                    }
                }
                _rosterIds[i] = model.GroupId;
                card.Button.interactable = inputReady;
                UiDisabledMaterialUtility.SetSelectableDisabled(card.Button, UiDisabledVisualReason.MissionRestriction, false);
                UiDisabledMaterialUtility.SetDisabled(card.Button.gameObject, UiDisabledVisualReason.MissionRestriction, false);
                UiLocalizedText.Set(card.NameLabel, model.Title);
                if (card.OrdinalBadge != null)
                {
                    card.OrdinalBadge.transform.parent.gameObject.SetActive(true);
                    card.OrdinalBadge.gameObject.SetActive(true);
                    card.OrdinalBadge.text = model.Ordinal.ToString();
                }
                if (card.SelectedCheck != null) card.SelectedCheck.SetActive(model.Selected);
                if (card.AliveCountLabel != null) card.AliveCountLabel.gameObject.SetActive(false);
                if (card.HealthFillImage != null) { card.HealthFillImage.enabled = true; card.HealthFillImage.fillAmount = model.Health01; }
                var legacyBadge = card.Button.transform.Find("Frame/NumberBadge");
                if (legacyBadge != null) legacyBadge.gameObject.SetActive(false);
                var gradient = card.Button.GetComponentInChildren<V3GradientGraphic>(true);
                if (gradient != null) gradient.SetBorder(model.Selected ? V3SelectedBorderColor : V3NormalBorderColor, 3);
            }
            RefreshScrollGeometry();
            if (UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page) && page.PageIndex != _logicalPage)
            { _logicalPage = page.PageIndex; MoveToPage(_logicalPage); }
            if (entering) MoveToPage(_logicalPage);
            _scrollPrevious.interactable = _scrollNext.interactable = inputReady;
        }

        private void RestoreCategoryCards()
        {
            if (!_rosterActive) return;
            _rosterActive = false; _rosterModels.Clear(); _rosterPresses.Clear(); _logicalPage = 0;
            for (int i = 0; i < _rosterCards.Count; i++)
            {
                var card = _rosterCards[i]; _rosterIds[i] = 0;
                card.Button.gameObject.SetActive(i < cards.Length);
                if (i >= cards.Length) continue;
                card.Button.onClick.RemoveAllListeners();
                int slot = i; card.Button.onClick.AddListener(() => OnCardClicked(slot));
                var relay = card.Button.GetComponent<MatchHudSquadCardPressRelay>();
                relay.Bind(e => CapturePressedGroup(slot, e));
                relay.ConfigureScroll(_squadScroll, () => _pressedGroupIds[slot] = 0);
                if (card.PortraitImage != null) { card.PortraitImage.sprite = _categoryPortraits[i]; card.PortraitImage.enabled = true; }
                if (card.OrdinalBadge != null) card.OrdinalBadge.transform.parent.gameObject.SetActive(false);
                var legacyBadge = card.Button.transform.Find("Frame/NumberBadge");
                if (legacyBadge != null) legacyBadge.gameObject.SetActive(true);
                if (card.SelectedCheck != null) card.SelectedCheck.SetActive(false);
                UiLocalizedText.Set(card.NameLabel, CardLabels[i]);
                if (card.PortraitImage != null && _categoryPortraits[i] != null)
                    card.PortraitImage.GetComponent<AspectRatioFitter>().aspectRatio = _categoryPortraits[i].rect.width / _categoryPortraits[i].rect.height;
            }
            _restrictionStateInitialized = false;
            RefreshScrollGeometry(); MoveToPage(0); SetSelectedSlot(_selectedSlot);
        }

        private void RefreshScrollGeometry()
        {
            if (_scrollContent == null) return;
            bool rtl = UiShellRuntimeGateway.Localization.IsRightToLeft;
            _scrollContent.GetComponent<HorizontalLayoutGroup>().reverseArrangement = rtl;
            int count = 0;
            foreach (var card in _rosterCards) if (card.Button.gameObject.activeSelf) count++;
            if (_geometryCount == count && _geometryRtl == rtl && _geometryViewport == _squadScroll.viewport.rect.size)
            { UpdateScrollEdges(); return; }
            _geometryCount = count; _geometryRtl = rtl; _geometryViewport = _squadScroll.viewport.rect.size;
            _syncingScroll = true;
            _scrollContent.sizeDelta = new Vector2(Mathf.Max(0, count * CardStride - CardGap) + 2 * CardInset, 0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollContent);
            if (_scrollContent.rect.width <= _squadScroll.viewport.rect.width) _squadScroll.horizontalNormalizedPosition = rtl ? 1 : 0;
            _syncingScroll = false;
            UpdateScrollEdges();
        }
        private float MaximumScroll => _squadScroll != null ? Mathf.Max(0, _scrollContent.rect.width - _squadScroll.viewport.rect.width) : 0;
        private float LogicalScroll => UiShellRuntimeGateway.Localization.IsRightToLeft
            ? (1 - _squadScroll.horizontalNormalizedPosition) * MaximumScroll : _squadScroll.horizontalNormalizedPosition * MaximumScroll;
        private void MoveToPage(int page)
        {
            if (_squadScroll == null) return;
            _syncingScroll = true;
            _squadScroll.StopMovement();
            float position = MaximumScroll > 0 ? Mathf.Clamp01(page * 5 * CardStride / MaximumScroll) : 0;
            _squadScroll.horizontalNormalizedPosition = UiShellRuntimeGateway.Localization.IsRightToLeft ? 1 - position : position;
            _syncingScroll = false; UpdateScrollEdges();
        }
        private void ScrollPage(int delta)
        {
            if (!_rosterActive || _changePage == null) return;
            _changePage(delta);
            if (UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page))
            { _logicalPage = page.PageIndex; MoveToPage(_logicalPage); }
        }
        private void OnSquadScrollChanged(Vector2 position)
        {
            UpdateScrollEdges();
            if (_syncingScroll || !_rosterActive || _changePage == null || MaximumScroll <= 0) return;
            int pages = Mathf.Max(1, (_rosterModels.Count + 4) / 5);
            int nearest = 0; float distance = float.MaxValue;
            for (int i = 0; i < pages; i++)
            {
                // Last page may have fewer than five cards; its aligned position is clamped.
                float d = Mathf.Abs(LogicalScroll - Mathf.Min(i * 5 * CardStride, MaximumScroll));
                if (d < distance) { distance = d; nearest = i; }
            }
            if (nearest == _logicalPage) return;
            int delta = nearest > _logicalPage ? 1 : -1;
            while (_logicalPage != nearest) { _changePage(delta); _logicalPage += delta; }
        }
        private void UpdateScrollEdges()
        {
            if (_scrollPrevious == null || _scrollNext == null) return;
            bool overflow = MaximumScroll > 1;
            bool rtl = UiShellRuntimeGateway.Localization.IsRightToLeft;
            _scrollPrevious.gameObject.SetActive(overflow && LogicalScroll > 1);
            _scrollNext.gameObject.SetActive(overflow && LogicalScroll < MaximumScroll - 1);
            foreach (var pair in new[] { (_scrollPrevious, rtl), (_scrollNext, !rtl) })
            {
                var rect = (RectTransform)pair.Item1.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(pair.Item2 ? 1 : 0, .5f);
                rect.pivot = new Vector2(pair.Item2 ? 1 : 0, .5f);
                rect.localScale = Vector3.one;
                foreach (Transform stroke in rect)
                    stroke.localScale = new Vector3(rtl ? -1 : 1, 1, 1);
                foreach (Transform stroke in rect)
                    if (stroke is RectTransform line)
                        line.localRotation = Quaternion.Euler(0, 0,
                            (stroke.name == "Chevron0" ? 45 : -45) * (pair.Item2 ? 1 : -1));
            }
        }
        public RectTransform ExpandedScrollViewport => _rosterActive ? _squadScroll?.viewport : null;
        public int LogicalPageIndex => _logicalPage;

        public bool TryGetFirstSelectedExpandedCardTarget(out uint groupId, out Button visibleCard,
            out Vector2 swipeFrom, out Vector2 swipeTo)
        {
            groupId = 0; visibleCard = null; swipeFrom = swipeTo = Vector2.zero;
            foreach (var model in _rosterModels)
                if (model.Selected)
                {
                    groupId = model.GroupId;
                    return TryGetExpandedCardTarget(groupId, out visibleCard, out swipeFrom, out swipeTo);
                }
            return false;
        }

        public uint ExpandedSlotGroupId(int slot)
        {
            int index = _logicalPage * 5 + slot;
            return slot >= 0 && slot < 5 && index < _rosterModels.Count ? _rosterModels[index].GroupId : 0;
        }

        public float ExpandedCardScrollDistance(uint groupId)
        {
            if (_squadScroll == null) return 0;
            for (int i = 0; i < _rosterModels.Count; i++)
                if (_rosterModels[i].GroupId == groupId)
                {
                    var viewport = _squadScroll.viewport;
                    var card = (RectTransform)_rosterCards[i].Button.transform;
                    var low = viewport.InverseTransformPoint(card.TransformPoint(card.rect.min));
                    var high = viewport.InverseTransformPoint(card.TransformPoint(card.rect.max));
                    return Mathf.Max(0, viewport.rect.xMin - low.x, high.x - viewport.rect.xMax) / Mathf.Max(1, viewport.rect.width);
                }
            return 0;
        }

        public bool TryGetExpandedPageSwipe(int direction, out Vector2 from, out Vector2 to)
        {
            from = to = Vector2.zero;
            if (!_rosterActive || _squadScroll == null || MaximumScroll <= 0 || (direction != -1 && direction != 1)) return false;
            if ((direction < 0 && LogicalScroll <= 1) || (direction > 0 && LogicalScroll >= MaximumScroll - 1)) return false;
            var viewport = _squadScroll.viewport;
            bool rtl = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float sign = direction * (rtl ? -1 : 1);
            float half = viewport.rect.width * .40f;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvas = canvas.rootCanvas;
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            from = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(new Vector3(viewport.rect.center.x + sign * half, viewport.rect.center.y)));
            to = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(new Vector3(viewport.rect.center.x - sign * half, viewport.rect.center.y)));
            return true;
        }

        public bool TryGetExpandedSlotTarget(int logicalSlot, out Button visibleCard,
            out Vector2 swipeFrom, out Vector2 swipeTo)
        {
            visibleCard = null; swipeFrom = swipeTo = Vector2.zero;
            int index = _logicalPage * 5 + logicalSlot;
            return logicalSlot >= 0 && logicalSlot < 5 && index < _rosterModels.Count &&
                TryGetExpandedCardTarget(_rosterModels[index].GroupId, out visibleCard, out swipeFrom, out swipeTo);
        }

        // Read-only input geometry. Callers must execute a normal pointer swipe/tap;
        // this method never changes scrolling, selection, camera focus or troop orders.
        public bool TryGetExpandedCardTarget(uint groupId, out Button visibleCard,
            out Vector2 swipeFrom, out Vector2 swipeTo)
        {
            visibleCard = null; swipeFrom = swipeTo = Vector2.zero;
            if (!_rosterActive || _squadScroll == null || groupId == 0) return false;
            for (int i = 0; i < _rosterModels.Count; i++)
            {
                if (_rosterModels[i].GroupId != groupId) continue;
                var button = _rosterCards[i].Button;
                if (IsCardPresented(button)) { visibleCard = button; return true; }
                var viewport = _squadScroll.viewport;
                var cardRect = (RectTransform)button.transform;
                var low = viewport.InverseTransformPoint(cardRect.TransformPoint(cardRect.rect.min));
                var high = viewport.InverseTransformPoint(cardRect.TransformPoint(cardRect.rect.max));
                bool moveLeft = high.x > viewport.rect.xMax;
                float missing = moveLeft ? high.x - viewport.rect.xMax : viewport.rect.xMin - low.x;
                // A nearly visible card needs a small alignment gesture, not a whole
                // window jump that would change the caller's logical slot identity.
                float half = Mathf.Min(viewport.rect.width * .56f, Mathf.Max(12f, missing + 8f)) * .5f;
                float y = viewport.rect.center.y;
                var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvas = canvas.rootCanvas;
                var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                swipeFrom = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(new Vector3(viewport.rect.center.x + (moveLeft ? half : -half), y)));
                swipeTo = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(new Vector3(viewport.rect.center.x + (moveLeft ? -half : half), y)));
                return true;
            }
            return false;
        }

        private bool IsCardPresented(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return false;
            if (_squadScroll == null) return true;
            var viewport = _squadScroll.viewport;
            var rect = (RectTransform)button.transform;
            var low = viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.min));
            var high = viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.max));
            return low.x >= viewport.rect.xMin - .5f && high.x <= viewport.rect.xMax + .5f &&
                low.y >= viewport.rect.yMin - .5f && high.y <= viewport.rect.yMax + .5f;
        }
        private void CapturePressedGroup(int index, PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && index >= 0 && index < _pressedGroupIds.Length)
                _pressedGroupIds[index] = _displayedGroupIds[index];
        }
        private void SetExpandedSelected(int index, bool selected)
        {
            if (!TryGetCard(index, out var card) || card.Button == null) return;
            card.Button.GetComponentInChildren<V3GradientGraphic>(true)?.SetBorder(selected ? V3SelectedBorderColor : V3NormalBorderColor, 3);
        }
    }
}
