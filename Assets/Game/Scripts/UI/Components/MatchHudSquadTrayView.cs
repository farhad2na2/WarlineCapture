using System;
using Game.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MatchHudSquadTrayView : MonoBehaviour, IMatchHudSquadTrayView
    {
        // UiAssistantHighlightModel deliberately carries the ECS target kind as a byte so
        // Game.UI.Runtime does not depend on the gameplay-components assembly.
        private const byte AssistantSquadTargetKind = 6;

        [Serializable]
        public sealed class Card
        {
            public Button Button;
            public Image FrameImage;
            public Image PortraitImage;
        }

        [SerializeField] private Sprite normalFrameSprite;
        [SerializeField] private Sprite selectedFrameSprite;
        [SerializeField] private Card[] cards = new Card[5];
        [SerializeField] private TMP_FontAsset cardLabelFont;

        private static readonly string[] CardLabels =
        {
            "RIFLE SQUAD",
            "APC",
            "TANK",
            "HELICOPTER",
            "TRANSPORT"
        };
        private static readonly Color CardLabelColor = new(0.86f, 0.84f, 0.74f, 1f);
        private static readonly Color CardLabelStripColor = new(0f, 0f, 0f, 0.45f);
        private static readonly Color V3SelectedBorderColor = new Color32(0, 188, 224, 255);
        private static readonly Color V3NormalBorderColor = new Color32(48, 166, 69, 255);
        private static readonly Color V3MissionDisabledBorderColor = new Color32(28, 166, 232, 255);
        private static readonly Color V3MissionDisabledWashColor = new(0.015f, 0.30f, 0.68f, 0.48f);
        private static readonly Color V3GuidanceYellow = new(1f, 0.72f, 0.02f, 1f);
        private readonly Color[] _frameBaseColors = new Color[5];
        private readonly Color[] _portraitBaseColors = new Color[5];
        private readonly bool[] _missionDisabled = new bool[5];
        private readonly Image[] _missionDisabledWashes = new Image[5];
        private Action<MatchHudSquadTraySlot> _cardClicked;
        private Canvas _cachedCanvas;
        private RectTransform _assistantGuidanceCue;
        private CanvasGroup _assistantGuidanceGroup;
        private bool _assistantGuidanceActive;
        private MatchHudSquadTraySlot _selectedSlot = MatchHudSquadTraySlot.None;
        private bool _restrictionStateInitialized;
        private bool _lastCombatVehiclesDisabled;
        private bool _lastAirDisabled;
        private bool _lastTransportDisabled;
        private bool _lastHideUnrelatedControls;

        internal RectTransform AssistantGuidanceTarget
        {
            get
            {
                return TryGetCard(0, out Card soldierCard) && soldierCard.Button != null
                    ? soldierCard.Button.transform as RectTransform
                    : null;
            }
        }

        internal bool IsAssistantGuidanceTargetSelected =>
            _selectedSlot == MatchHudSquadTraySlot.Soldiers;

        private void Awake()
        {
            MatchHudCanvasBatchingUtility.EnsureLocalCanvas(gameObject, needsRaycaster: true);
            CacheBaseFrameColors();
            CreateCardLabels();
            CreateAssistantGuidanceCue();
            SetSelectedSlot(MatchHudSquadTraySlot.Soldiers);
        }

        private void OnEnable()
        {
            RefreshMissionRestrictions();
        }

        private void CreateCardLabels()
        {
            for (int i = 0; i < cards.Length && i < CardLabels.Length; i++)
            {
                if (!TryGetCard(i, out Card card) || card.Button == null)
                    continue;

                RectTransform cardRect = card.Button.transform as RectTransform;
                if (cardRect == null)
                    continue;

                Transform existingStrip = cardRect.Find("NameStrip");
                GameObject stripObject = existingStrip != null ? existingStrip.gameObject : new GameObject("NameStrip");
                if (existingStrip == null)
                    stripObject.transform.SetParent(cardRect, false);
                stripObject.layer = cardRect.gameObject.layer;
                RectTransform stripRect = stripObject.GetComponent<RectTransform>() ?? stripObject.AddComponent<RectTransform>();
                stripRect.anchorMin = new Vector2(0f, 0f);
                stripRect.anchorMax = new Vector2(1f, 0f);
                stripRect.pivot = new Vector2(0.5f, 0f);
                stripRect.anchoredPosition = new Vector2(0f, 6f);
                stripRect.sizeDelta = new Vector2(-12f, 34f);
                Image stripImage = stripObject.GetComponent<Image>() ?? stripObject.AddComponent<Image>();
                stripImage.color = CardLabelStripColor;
                stripImage.raycastTarget = false;

                Transform existingLabel = stripRect.Find("Label");
                GameObject labelObject = existingLabel != null ? existingLabel.gameObject : new GameObject("Label");
                if (existingLabel == null)
                    labelObject.transform.SetParent(stripRect, false);
                labelObject.layer = stripObject.layer;
                RectTransform labelRect = labelObject.GetComponent<RectTransform>() ?? labelObject.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(4f, 0f);
                labelRect.offsetMax = new Vector2(-4f, 0f);
                TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>() ?? labelObject.AddComponent<TextMeshProUGUI>();
                if (cardLabelFont != null)
                    label.font = cardLabelFont;
                label.text = CardLabels[i];
                label.fontStyle = FontStyles.Bold;
                label.fontSize = 21f;
                label.enableAutoSizing = true;
                label.fontSizeMin = 13f;
                label.fontSizeMax = 21f;
                label.color = CardLabelColor;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.raycastTarget = false;
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnTransformParentChanged()
        {
            _cachedCanvas = null;
        }

        private void Update()
        {
            // Mission data becomes available after the HUD prefab is enabled. Poll the cheap
            // read model and only rebuild visuals when its restriction flags actually change.
            RefreshMissionRestrictions();

            if (_assistantGuidanceActive && _assistantGuidanceCue != null && _assistantGuidanceGroup != null)
                _assistantGuidanceGroup.alpha = 1f;
        }

        public void Bind(Action<MatchHudSquadTraySlot> cardClicked)
        {
            Unbind();
            _cardClicked = cardClicked;
            RefreshMissionRestrictions();

            for (int i = 0; i < cards.Length; i++)
            {
                if (!TryGetCard(i, out Card card) || card.Button == null)
                    continue;

                int index = i;
                card.Button.onClick.AddListener(() => OnCardClicked(index));
            }
        }

        internal void RefreshMissionRestrictions()
        {
            bool combatVehiclesDisabled = false;
            bool airDisabled = false;
            bool transportDisabled = false;
            bool hideUnrelatedControls = false;
            if (UiShellRuntimeGateway.TryReadMissionHudRestrictions(out UiMissionHudRestrictionsModel restrictions))
            {
                combatVehiclesDisabled = restrictions.ProductionDisabled;
                airDisabled = restrictions.AirDisabled;
                transportDisabled = restrictions.TransportDisabled;
                hideUnrelatedControls = restrictions.HideUnrelatedControls;
            }

            if (_restrictionStateInitialized &&
                _lastCombatVehiclesDisabled == combatVehiclesDisabled &&
                _lastAirDisabled == airDisabled &&
                _lastTransportDisabled == transportDisabled &&
                _lastHideUnrelatedControls == hideUnrelatedControls)
                return;

            _restrictionStateInitialized = true;
            _lastCombatVehiclesDisabled = combatVehiclesDisabled;
            _lastAirDisabled = airDisabled;
            _lastTransportDisabled = transportDisabled;
            _lastHideUnrelatedControls = hideUnrelatedControls;

            ApplyMissionRestrictionVisibility(
                combatVehiclesDisabled, airDisabled, transportDisabled, hideUnrelatedControls);
        }

        public void ApplyMissionRestrictionVisibility(
            bool combatVehiclesDisabled,
            bool airDisabled,
            bool transportDisabled,
            bool hideUnrelatedControls = false)
        {
            SetCardDisabled(0, disabled: false, outsideMissionScope: false);
            SetCardDisabled(1, combatVehiclesDisabled, hideUnrelatedControls);
            SetCardDisabled(2, airDisabled, hideUnrelatedControls);
            SetCardDisabled(3, airDisabled, hideUnrelatedControls);
            SetCardDisabled(4, transportDisabled, hideUnrelatedControls);
        }

        public void Unbind()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (TryGetCard(i, out Card card) && card.Button != null)
                    card.Button.onClick.RemoveAllListeners();
            }

            _cardClicked = null;
        }

        public void SetSelectedSlot(MatchHudSquadTraySlot selectedSlot)
        {
            _selectedSlot = selectedSlot;
            for (int i = 0; i < cards.Length; i++)
            {
                if (!TryGetCard(i, out Card card) || card.FrameImage == null)
                    continue;

                bool selected = ToSlot(i) == selectedSlot;
                SetImageSprite(card.FrameImage, selected ? selectedFrameSprite : normalFrameSprite);
                SetImageColor(card.FrameImage, _frameBaseColors[i]);
                V3GradientGraphic v3Frame = card.Button.GetComponentInChildren<V3GradientGraphic>(true);
                if (v3Frame != null)
                {
                    Color border = _missionDisabled[i]
                        ? V3MissionDisabledBorderColor
                        : selected ? V3SelectedBorderColor : V3NormalBorderColor;
                    v3Frame.SetBorder(border, _missionDisabled[i] ? 5f : 3f);
                }
            }
        }

        public void ClearActiveSlot()
        {
            SetSelectedSlot(MatchHudSquadTraySlot.None);
        }

        public void FlashDisabled(MatchHudSquadTraySlot slot)
        {
            int index = ToIndex(slot);
            if (!TryGetCard(index, out _))
                return;

            // Unavailable mission cards use one persistent blue treatment. A click must not
            // flash a single card and imply that another unavailable card is interactive.
            ApplyMissionDisabledTreatment(index, _missionDisabled[index]);
        }

        public bool TryGetPortraitSprite(MatchHudSquadTraySlot slot, out Sprite sprite)
        {
            sprite = null;
            int index = ToIndex(slot);
            if (!TryGetCard(index, out Card card) || card.PortraitImage == null)
                return false;

            sprite = card.PortraitImage.sprite;
            return sprite != null;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            if (!isActiveAndEnabled)
                return false;

            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform == null)
                return false;

            Camera eventCamera = ResolveEventCamera();
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera);
        }

        private Camera ResolveEventCamera()
        {
            Canvas canvas = ResolveCanvas();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }

        private Canvas ResolveCanvas()
        {
            if (_cachedCanvas == null)
                _cachedCanvas = GetComponentInParent<Canvas>();
            return _cachedCanvas;
        }

        private void CacheBaseFrameColors()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                bool hasCard = TryGetCard(i, out Card card);
                _frameBaseColors[i] = hasCard && card.FrameImage != null ? card.FrameImage.color : Color.white;
                _portraitBaseColors[i] = hasCard && card.PortraitImage != null ? card.PortraitImage.color : Color.white;
            }
        }

        private bool TryGetCard(int index, out Card card)
        {
            card = null;
            if (cards == null || index < 0 || index >= cards.Length)
                return false;

            card = cards[index];
            return card != null;
        }

        private void SetCardDisabled(int index, bool disabled, bool outsideMissionScope)
        {
            if (!TryGetCard(index, out Card card) || card.Button == null)
                return;

            // Mission scope changes capability, never layout. Keeping every authored card
            // active prevents the M02 tray from collapsing/reflowing as guidance advances and
            // matches M01's visible grayscale treatment for unavailable categories.
            bool unavailable = disabled || outsideMissionScope;
            _missionDisabled[index] = unavailable;
            if (!card.Button.gameObject.activeSelf)
                card.Button.gameObject.SetActive(true);
            UiDisabledMaterialUtility.SetSelectableDisabled(
                card.Button,
                UiDisabledVisualReason.MissionRestriction,
                unavailable);
            UiDisabledMaterialUtility.SetDisabled(
                card.Button.gameObject,
                UiDisabledVisualReason.MissionRestriction,
                unavailable);
            card.Button.interactable = !unavailable;
            ApplyMissionDisabledTreatment(index, unavailable);
        }

        private void ApplyMissionDisabledTreatment(int index, bool unavailable)
        {
            if (!TryGetCard(index, out Card card) || card.Button == null)
                return;

            TintMissionDisabledGraphics(card, unavailable);

            Image wash = EnsureMissionDisabledWash(index, card);
            if (wash != null)
            {
                // Reassert the shared V3 overlay after the grayscale material pass so every
                // unavailable card has the same static blue tint instead of a one-off flash.
                wash.material = null;
                wash.color = V3MissionDisabledWashColor;
                wash.transform.SetAsLastSibling();
                if (wash.gameObject.activeSelf != unavailable)
                    wash.gameObject.SetActive(unavailable);
            }

            V3GradientGraphic v3Frame = card.Button.GetComponentInChildren<V3GradientGraphic>(true);
            if (v3Frame != null)
            {
                bool selected = ToSlot(index) == _selectedSlot;
                Color border = unavailable
                    ? V3MissionDisabledBorderColor
                    : selected ? V3SelectedBorderColor : V3NormalBorderColor;
                v3Frame.SetBorder(border, unavailable ? 5f : 3f);
            }
        }

        private static void TintMissionDisabledGraphics(Card card, bool unavailable)
        {
            if (!unavailable || card?.Button == null)
                return;

            Graphic[] graphics = card.Button.GetComponentsInChildren<Graphic>(true);
            for (int graphicIndex = 0; graphicIndex < graphics.Length; graphicIndex++)
            {
                Graphic graphic = graphics[graphicIndex];
                if (graphic == null || graphic is TMP_Text ||
                    graphic.gameObject.name == "MissionDisabledBlueWash")
                    continue;

                UiDisabledMaterialState disabledState = graphic.GetComponent<UiDisabledMaterialState>();
                Color source = disabledState != null ? disabledState.OriginalColor : graphic.color;
                float neutral = Mathf.Clamp(source.grayscale, 0.42f, 0.82f);
                Color disabledBlue = new(0.12f, 0.50f, 0.82f, source.a);
                graphic.color = Color.Lerp(
                    new Color(neutral, neutral, neutral, source.a),
                    disabledBlue,
                    0.46f);
            }
        }

        private Image EnsureMissionDisabledWash(int index, Card card)
        {
            if (_missionDisabledWashes[index] != null)
                return _missionDisabledWashes[index];

            Transform existing = card.Button.transform.Find("MissionDisabledBlueWash");
            GameObject washObject = existing != null
                ? existing.gameObject
                : new GameObject("MissionDisabledBlueWash", typeof(RectTransform), typeof(Image));
            if (existing == null)
                washObject.transform.SetParent(card.Button.transform, false);
            washObject.layer = card.Button.gameObject.layer;
            washObject.transform.SetAsLastSibling();
            RectTransform rect = washObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image wash = washObject.GetComponent<Image>();
            wash.material = null;
            wash.color = V3MissionDisabledWashColor;
            wash.raycastTarget = false;
            _missionDisabledWashes[index] = wash;
            return wash;
        }

        private void OnCardClicked(int index)
        {
            UIAudioEventGateway.Raise(UIAudioEventKind.ButtonPrimaryClick);
            _cardClicked?.Invoke(ToSlot(index));
            if (_assistantGuidanceActive && index == 0)
            {
                UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(
                    UiAssistantCommandIntentKind.StopAssistantControl);
                SetAssistantGuidanceCueVisible(false);
            }
        }

        internal void ApplyAssistantGuidance(UiAssistantHighlightModel model)
        {
            CreateAssistantGuidanceCue();
            bool pointToSoldiers = model.Active && model.TargetKind == AssistantSquadTargetKind;
            SetAssistantGuidanceCueVisible(pointToSoldiers);
        }

        internal void ClearAssistantGuidance()
        {
            SetAssistantGuidanceCueVisible(false);
        }

        private void CreateAssistantGuidanceCue()
        {
            if (_assistantGuidanceCue != null || !TryGetCard(0, out Card soldierCard) || soldierCard.Button == null)
                return;

            GameObject cue = new(
                "AriaButtonGuidance",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(V3GradientGraphic));
            cue.transform.SetParent(soldierCard.Button.transform, false);
            cue.transform.SetAsLastSibling();
            cue.layer = soldierCard.Button.gameObject.layer;
            _assistantGuidanceCue = cue.GetComponent<RectTransform>();
            _assistantGuidanceCue.anchorMin = Vector2.zero;
            _assistantGuidanceCue.anchorMax = Vector2.one;
            _assistantGuidanceCue.pivot = new Vector2(0.5f, 0.5f);
            _assistantGuidanceCue.offsetMin = new Vector2(-8f, -8f);
            _assistantGuidanceCue.offsetMax = new Vector2(8f, 8f);
            _assistantGuidanceGroup = cue.GetComponent<CanvasGroup>();
            _assistantGuidanceGroup.interactable = false;
            _assistantGuidanceGroup.blocksRaycasts = false;
            V3GradientGraphic background = cue.GetComponent<V3GradientGraphic>();
            background.ConfigureCorners(
                Color.clear,
                Color.clear,
                Color.clear,
                Color.clear,
                V3GuidanceYellow,
                7f);
            background.raycastTarget = false;

            GameObject captionObject = new(
                "TopBorderCaption",
                typeof(RectTransform),
                typeof(V3GradientGraphic));
            captionObject.transform.SetParent(cue.transform, false);
            captionObject.layer = cue.layer;
            RectTransform captionRect = captionObject.GetComponent<RectTransform>();
            captionRect.anchorMin = new Vector2(0.5f, 1f);
            captionRect.anchorMax = new Vector2(0.5f, 1f);
            captionRect.pivot = new Vector2(0.5f, 0.5f);
            captionRect.anchoredPosition = Vector2.zero;
            captionRect.sizeDelta = new Vector2(210f, 42f);
            V3GradientGraphic caption = captionObject.GetComponent<V3GradientGraphic>();
            caption.ConfigureCorners(
                new Color(0.11f, 0.095f, 0.025f, 0.98f),
                new Color(0.16f, 0.13f, 0.025f, 0.98f),
                new Color(0.025f, 0.035f, 0.038f, 0.98f),
                new Color(0.025f, 0.035f, 0.038f, 0.98f),
                V3GuidanceYellow,
                4f);
            caption.raycastTarget = false;

            GameObject labelObject = new("Instruction", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(captionObject.transform, false);
            labelObject.layer = cue.layer;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 3f);
            labelRect.offsetMax = new Vector2(-10f, -3f);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            if (cardLabelFont != null)
                label.font = cardLabelFont;
            label.text = GameLocalization.Get("ui.hud.tap_rifle_squad", "SELECT SQUAD");
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 27f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 30f;
            label.color = V3GuidanceYellow;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            cue.SetActive(false);
        }

        private void SetAssistantGuidanceCueVisible(bool visible)
        {
            _assistantGuidanceActive = visible;
            if (_assistantGuidanceCue != null && _assistantGuidanceCue.gameObject.activeSelf != visible)
                _assistantGuidanceCue.gameObject.SetActive(visible);
        }

        private static void SetImageSprite(Image image, Sprite sprite)
        {
            if (image != null && image.sprite != sprite)
                image.sprite = sprite;
        }

        private static void SetImageColor(Image image, Color color)
        {
            if (image != null && image.color != color)
                image.color = color;
        }

        private static MatchHudSquadTraySlot ToSlot(int index)
        {
            return index switch
            {
                0 => MatchHudSquadTraySlot.Soldiers,
                1 => MatchHudSquadTraySlot.CombatVehicles,
                2 => MatchHudSquadTraySlot.AttackHelicopter,
                3 => MatchHudSquadTraySlot.Jet,
                4 => MatchHudSquadTraySlot.Transport,
                _ => MatchHudSquadTraySlot.None
            };
        }

        private static int ToIndex(MatchHudSquadTraySlot slot)
        {
            return slot switch
            {
                MatchHudSquadTraySlot.Soldiers => 0,
                MatchHudSquadTraySlot.CombatVehicles => 1,
                MatchHudSquadTraySlot.AttackHelicopter => 2,
                MatchHudSquadTraySlot.Jet => 3,
                MatchHudSquadTraySlot.Transport => 4,
                _ => -1
            };
        }
    }
}
