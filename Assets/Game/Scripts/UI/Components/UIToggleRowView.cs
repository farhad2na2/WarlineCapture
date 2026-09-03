using Game.Configs;
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
        [SerializeField] private Image trackImage;
        [SerializeField] private Image handleImage;
        [SerializeField] private V3GradientGraphic trackGradient;
        [SerializeField] private V3GradientGraphic handleGradient;
        [SerializeField] private Color onTrackColor = new(0.02f, 0.34f, 0.58f, 1f);
        [SerializeField] private Color offTrackColor = new(0.08f, 0.1f, 0.11f, 1f);
        [SerializeField] private Color onHandleColor = new(0.25f, 0.82f, 0.12f, 1f);
        [SerializeField] private Color offHandleColor = new(0.42f, 0.45f, 0.46f, 1f);

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
                stateText.text = value
                    ? GameLocalization.Get("ui.common.on", "ON")
                    : GameLocalization.Get("ui.common.off", "OFF");
                stateText.alignment = value ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            }

            if (handle != null)
            {
                handle.anchorMin = value ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
                handle.anchorMax = value ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
                handle.pivot = value ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
                handle.anchoredPosition = value ? new Vector2(-5f, 0f) : new Vector2(5f, 0f);
            }

            if (trackImage != null)
                trackImage.color = value ? onTrackColor : offTrackColor;
            if (handleImage != null)
                handleImage.color = value ? onHandleColor : offHandleColor;
            if (trackGradient != null)
            {
                Color trackTop = Color.Lerp(value ? onTrackColor : offTrackColor, Color.white, value ? 0.16f : 0.07f);
                Color trackBottom = Color.Lerp(value ? onTrackColor : offTrackColor, Color.black, 0.24f);
                trackGradient.ConfigureCorners(
                    Color.Lerp(trackTop, Color.white, value ? 0.1f : 0.03f),
                    trackTop,
                    Color.Lerp(trackBottom, Color.black, 0.18f),
                    trackBottom,
                    value ? new Color(0f, 0.62f, 0.94f, 1f) : offHandleColor,
                    4f);
            }
            if (handleGradient != null)
            {
                Color handleTop = Color.Lerp(value ? onHandleColor : offHandleColor, Color.white, value ? 0.2f : 0.08f);
                Color handleBottom = Color.Lerp(value ? onHandleColor : offHandleColor, Color.black, 0.22f);
                handleGradient.ConfigureCorners(
                    Color.Lerp(handleTop, Color.white, value ? 0.14f : 0.04f),
                    handleTop,
                    Color.Lerp(handleBottom, Color.black, 0.16f),
                    handleBottom,
                    value ? new Color(0.18f, 0.72f, 0.12f, 1f) : offHandleColor,
                    4f);
            }
        }
    }
}
