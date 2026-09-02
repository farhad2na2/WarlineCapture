using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ResourceExchangeQueueItemView : MonoBehaviour
    {
        [SerializeField] private Button rushButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private Image completedImage;
        [SerializeField] private Image warningImage;
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text inputText;
        [SerializeField] private TMP_Text outputText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text percentText;
        [SerializeField] private TMP_Text stateText;

        public Button RushButton => rushButton;
        public Button CancelButton => cancelButton;
        public Image ProgressFillImage => progressFillImage;
        public TMP_Text NameText => nameText;

        public void Bind(
            string number,
            string displayName,
            string input,
            string output,
            string time,
            string percent,
            string state,
            float progress01,
            Sprite thumbnail,
            bool rushEnabled,
            bool cancelEnabled,
            bool completedVisible,
            bool warningVisible)
        {
            SetText(numberText, number);
            SetText(nameText, displayName);
            SetText(inputText, input);
            SetText(outputText, output);
            SetText(timeText, time);
            SetText(percentText, percent);
            SetText(stateText, state);
            SetImage(thumbnailImage, thumbnail);

            if (progressFillImage != null)
            {
                progressFillImage.type = Image.Type.Filled;
                progressFillImage.fillMethod = Image.FillMethod.Horizontal;
                progressFillImage.fillOrigin = 0;
                progressFillImage.fillAmount = Mathf.Clamp01(progress01);
            }

            SetActive(completedImage, completedVisible);
            SetActive(warningImage, warningVisible);

            if (rushButton != null)
            {
                rushButton.gameObject.SetActive(rushEnabled && !completedVisible);
                rushButton.interactable = rushEnabled;
            }
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(cancelEnabled && !completedVisible);
                cancelButton.interactable = cancelEnabled;
            }
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
