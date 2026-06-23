using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MatchHudSquadTrayView : MonoBehaviour, IMatchHudSquadTrayView
{
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
    [SerializeField, Min(0.5f)] private float disabledFlashSeconds = 0.12f;
    [SerializeField] private TMP_FontAsset cardLabelFont;

    private static readonly string[] CardLabels =
    {
        "RIFLE SQUAD",
        "ARMOR",
        "GUNSHIP",
        "JET WING",
        "TRANSPORT"
    };
    private static readonly Color CardLabelColor = new(0.86f, 0.84f, 0.74f, 1f);
    private static readonly Color CardLabelStripColor = new(0f, 0f, 0f, 0.45f);

    private readonly Color[] _frameBaseColors = new Color[5];
    private Action<MatchHudSquadTraySlot> _cardClicked;
    private float _disabledFlashUntil;
    private int _disabledFlashIndex = -1;
    private Canvas _cachedCanvas;

    private void Awake()
    {
        CacheBaseFrameColors();
        CreateCardLabels();
        SetSelectedSlot(MatchHudSquadTraySlot.Soldiers);
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

            GameObject stripObject = new("NameStrip");
            stripObject.transform.SetParent(cardRect, false);
            stripObject.layer = cardRect.gameObject.layer;
            RectTransform stripRect = stripObject.AddComponent<RectTransform>();
            stripRect.anchorMin = new Vector2(0f, 1f);
            stripRect.anchorMax = new Vector2(1f, 1f);
            stripRect.pivot = new Vector2(0.5f, 1f);
            stripRect.anchoredPosition = new Vector2(29f, -12f);
            stripRect.sizeDelta = new Vector2(-82f, 40f);
            Image stripImage = stripObject.AddComponent<Image>();
            stripImage.color = CardLabelStripColor;
            stripImage.raycastTarget = false;

            GameObject labelObject = new("Label");
            labelObject.transform.SetParent(stripRect, false);
            labelObject.layer = stripObject.layer;
            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-6f, 0f);
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            if (cardLabelFont != null)
                label.font = cardLabelFont;
            label.text = CardLabels[i];
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 26f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 26f;
            label.color = CardLabelColor;
            label.alignment = TextAlignmentOptions.MidlineLeft;
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
        if (_disabledFlashIndex < 0 || Time.unscaledTime < _disabledFlashUntil)
            return;

        if (TryGetCard(_disabledFlashIndex, out Card card) && card.FrameImage != null)
            card.FrameImage.color = _frameBaseColors[_disabledFlashIndex];
        _disabledFlashIndex = -1;
    }

    public void Bind(Action<MatchHudSquadTraySlot> cardClicked)
    {
        Unbind();
        _cardClicked = cardClicked;

        for (int i = 0; i < cards.Length; i++)
        {
            if (!TryGetCard(i, out Card card) || card.Button == null)
                continue;

            int index = i;
            card.Button.onClick.AddListener(() => _cardClicked?.Invoke(ToSlot(index)));
        }
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
        for (int i = 0; i < cards.Length; i++)
        {
            if (!TryGetCard(i, out Card card) || card.FrameImage == null)
                continue;

            bool selected = ToSlot(i) == selectedSlot;
            card.FrameImage.sprite = selected ? selectedFrameSprite : normalFrameSprite;
            card.FrameImage.color = _frameBaseColors[i];
        }
    }

    public void ClearActiveSlot()
    {
        SetSelectedSlot(MatchHudSquadTraySlot.None);
    }

    public void FlashDisabled(MatchHudSquadTraySlot slot)
    {
        int index = ToIndex(slot);
        if (!TryGetCard(index, out Card card) || card.FrameImage == null)
            return;

        card.FrameImage.color = new Color(1f, 0.82f, 0.35f, 0.92f);
        _disabledFlashIndex = index;
        _disabledFlashUntil = Time.unscaledTime + disabledFlashSeconds;
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
            _frameBaseColors[i] = TryGetCard(i, out Card card) && card.FrameImage != null ? card.FrameImage.color : Color.white;
    }

    private bool TryGetCard(int index, out Card card)
    {
        card = null;
        if (cards == null || index < 0 || index >= cards.Length)
            return false;

        card = cards[index];
        return card != null;
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
