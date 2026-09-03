using TMPro;
using System.Collections.Generic;
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

        public void Bind(string[] labels, int selectedIndex)
        {
            if (segmentButtons == null || segmentLabels == null)
                return;

            EnsureCapacity(labels?.Length ?? 0);
            int count = Mathf.Min(labels?.Length ?? 0, segmentLabels.Length);
            for (int i = 0; i < segmentLabels.Length; i++)
            {
                bool active = i < count;
                if (segmentLabels[i] != null)
                    segmentLabels[i].text = active ? labels[i] : string.Empty;

                if (segmentButtons.Length > i && segmentButtons[i] != null)
                {
                    segmentButtons[i].gameObject.SetActive(active);
                    bool selected = i == selectedIndex;
                    ApplyVisualState(i, selected);
                    segmentButtons[i].interactable = active && !selected;
                }
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
