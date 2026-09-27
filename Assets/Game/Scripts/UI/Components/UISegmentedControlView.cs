using TMPro;
using System.Collections.Generic;
using Game.Configs;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class UISegmentedControlView : MonoBehaviour
    {
        [SerializeField] private Transform segmentRoot;
        [SerializeField] private Button[] segmentButtons;
        [SerializeField] private TMP_Text[] segmentLabels;
        [SerializeField] private bool applyVisualSelection;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Color normalBackgroundColor = Color.white;
        [SerializeField] private Color selectedBackgroundColor = new(0f, 0.74f, 0.88f, 1f);
        [SerializeField] private Color normalLabelColor = new(0.88f, 0.94f, 0.95f, 1f);
        [SerializeField] private Color selectedLabelColor = new(0.98f, 1f, 1f, 1f);

        public event System.Action SelectionChanged;

        private int _activeCount;
        private bool _layingOut;

        public Transform SegmentRoot => segmentRoot;
        public Button[] SegmentButtons => segmentButtons;
        public TMP_Text[] SegmentLabels => segmentLabels;

        public void EnsureCapacity(int requiredCount)
        {
            requiredCount = Mathf.Max(0, requiredCount);
            if (requiredCount == 0 || segmentButtons == null || segmentButtons.Length == 0 ||
                segmentLabels == null || segmentLabels.Length == 0)
            {
                return;
            }

            List<Button> buttons = new(segmentButtons);
            List<TMP_Text> labels = new(segmentLabels);
            Transform parent = segmentRoot != null ? segmentRoot : segmentButtons[0].transform.parent;
            while (buttons.Count < requiredCount)
            {
                Button template = buttons[buttons.Count - 1];
                Button clone = Instantiate(template, parent);
                clone.name = $"Segment{buttons.Count}";
                clone.onClick.RemoveAllListeners();
                TMP_Text label = clone.GetComponentInChildren<TMP_Text>(includeInactive: true);
                if (label == null)
                {
                    Destroy(clone.gameObject);
                    break;
                }

                buttons.Add(clone);
                labels.Add(label);
            }

            segmentButtons = buttons.ToArray();
            segmentLabels = labels.ToArray();
        }

        public void Bind(string[] labels, int selectedIndex) => Bind(labels, selectedIndex, localize: true);

        public void Bind(string[] labels, int selectedIndex, bool localize)
        {
            if (segmentButtons == null || segmentLabels == null)
                return;

            EnsureCapacity(labels?.Length ?? 0);
            int count = Mathf.Min(labels?.Length ?? 0, segmentLabels.Length);
            _activeCount = count;
            for (int i = 0; i < segmentLabels.Length; i++)
            {
                bool active = i < count;
                if (segmentLabels[i] != null)
                {
                    if (active && !localize)
                        ApplyFixedLabel(segmentLabels[i], labels[i]);
                    else
                        UiLocalizedText.Set(segmentLabels[i], active ? labels[i] : string.Empty);
                }

                if (segmentButtons.Length > i && segmentButtons[i] != null)
                {
                    segmentButtons[i].gameObject.SetActive(active);
                    bool selected = i == selectedIndex;
                    ApplyVisualState(i, selected);
                    segmentButtons[i].interactable = active && !selected;
                }
            }
            LayoutActiveSegments();
            SelectionChanged?.Invoke();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (Application.isPlaying)
                LayoutActiveSegments();
        }

        private void ApplyFixedLabel(TMP_Text label, string value)
        {
            V3LocalizedTextBindingView binding = label.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.enabled = false;

            bool arabic = ContainsArabic(value);
            if (arabic)
            {
                if (ResolveRightToLeftFont() is TMP_FontAsset font)
                    label.font = font;
                label.isRightToLeftText = true;
                label.text = V3LocalizedTextBindingView.ShapeForRendering(value);
                return;
            }

            label.isRightToLeftText = false;
            label.text = value ?? string.Empty;
        }

        private static TMP_FontAsset ResolveRightToLeftFont()
        {
            IReadOnlyList<GameLocaleTable> locales = GameLocalization.AvailableLocales;
            for (int i = 0; i < locales.Count; i++)
            {
                if (locales[i] != null && locales[i].RightToLeft && locales[i].FontAsset is TMP_FontAsset font)
                    return font;
            }
            return null;
        }

        private static bool ContainsArabic(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character is >= '\u0600' and <= '\u06ff')
                    return true;
            }
            return false;
        }

        private void LayoutActiveSegments()
        {
            if (_layingOut || !Application.isPlaying || segmentRoot is not RectTransform root || segmentButtons == null || _activeCount <= 0)
                return;

            _layingOut = true;
            try
            {
                LayoutActiveSegments(root);
            }
            finally
            {
                _layingOut = false;
            }
        }

        private void LayoutActiveSegments(RectTransform root)
        {

            float width = root.rect.width;
            if (width <= 1f)
                return;

            const float gap = 10f;
            float segmentWidth = (width - (_activeCount - 1) * gap) / _activeCount;
            float height = root.rect.height > 1f ? root.rect.height : segmentButtons[0].GetComponent<RectTransform>().rect.height;
            for (int i = 0; i < _activeCount && i < segmentButtons.Length; i++)
            {
                if (segmentButtons[i] == null)
                    continue;
                RectTransform rect = segmentButtons[i].GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(segmentWidth, height);
                rect.anchoredPosition = new Vector2(i * (segmentWidth + gap) + segmentWidth * 0.5f, 0f);
            }
        }

        private void ApplyVisualState(int index, bool selected)
        {
            if (!applyVisualSelection || segmentButtons == null || index < 0 || index >= segmentButtons.Length)
                return;

            Image background = segmentButtons[index].targetGraphic as Image;
            if (background != null)
            {
                Sprite stateSprite = selected ? selectedSprite : normalSprite;
                if (stateSprite != null)
                    background.sprite = stateSprite;
                background.color = selected ? selectedBackgroundColor : normalBackgroundColor;
            }

            if (segmentLabels != null && index < segmentLabels.Length && segmentLabels[index] != null)
                segmentLabels[index].color = selected ? selectedLabelColor : normalLabelColor;
        }
    }
}
