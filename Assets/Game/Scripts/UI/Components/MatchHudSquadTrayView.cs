using System;
using UnityEngine;
using UnityEngine.UI;

public enum MatchHudSquadTraySlot
{
    None = 0,
    Soldiers = 1,
    CombatVehicles = 2,
    AttackHelicopter = 3,
    Jet = 4,
    Transport = 5
}

[DisallowMultipleComponent]
public sealed class MatchHudSquadTrayView : MonoBehaviour
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

    private readonly Color[] _frameBaseColors = new Color[5];
    private Action<MatchHudSquadTraySlot> _cardClicked;
    private float _disabledFlashUntil;
    private int _disabledFlashIndex = -1;

    private void Awake()
    {
        CacheBaseFrameColors();
        SetSelectedSlot(MatchHudSquadTraySlot.Soldiers);
    }

    private void OnDestroy()
    {
        Unbind();
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
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
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
