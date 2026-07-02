using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class UIToggleRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private Toggle toggle;
        [SerializeField] private RectTransform handle;

        public TMP_Text LabelText => labelText;
        public TMP_Text DescriptionText => descriptionText;
        public Toggle Toggle => toggle;

        private void Awake()
        {
            if (toggle != null)
                toggle.onValueChanged.AddListener(UpdateStateVisual);
        }

        private void OnDestroy()
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(UpdateStateVisual);
        }

        public void Bind(string label, string description, bool value)
        {
            if (labelText != null)
                labelText.text = label;

            if (descriptionText != null)
                descriptionText.text = description;

            if (toggle != null)
                toggle.SetIsOnWithoutNotify(value);

            UpdateStateVisual(value);
        }

        private void UpdateStateVisual(bool value)
        {
            if (stateText != null)
            {
                stateText.text = value ? "ON" : "OFF";
                stateText.alignment = value ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            }

            if (handle != null)
            {
                handle.anchorMin = value ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
                handle.anchorMax = value ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
                handle.pivot = value ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
                handle.anchoredPosition = value ? new Vector2(-5f, 0f) : new Vector2(5f, 0f);
            }
        }
    }
}
