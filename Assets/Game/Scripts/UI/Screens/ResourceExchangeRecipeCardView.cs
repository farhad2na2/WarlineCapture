using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ResourceExchangeRecipeCardView : MonoBehaviour
    {
        [SerializeField] private Button selectionButton;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private Image selectedCheckImage;
        [SerializeField] private Image lockImage;
        [SerializeField] private Image warningImage;
        [SerializeField] private GameObject disabledOverlay;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text inputText;
        [SerializeField] private TMP_Text outputText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private TMP_Text reasonText;

        public Button SelectionButton => selectionButton;
        public Image FrameImage => frameImage;
        public Image ThumbnailImage => thumbnailImage;
        public TMP_Text TitleText => titleText;
        public TMP_Text ReasonText => reasonText;

        public void Bind(
            string title,
            string input,
            string output,
            string duration,
            string reason,
            Sprite thumbnail,
            bool selected,
            bool enabled,
            bool locked,
            bool warning,
            Sprite defaultFrame,
            Sprite selectedFrame,
            Sprite lockedFrame)
        {
            SetText(titleText, title);
            SetText(inputText, input);
            SetText(outputText, output);
            SetText(durationText, duration);
            SetText(reasonText, reason);
            SetImage(thumbnailImage, thumbnail);

            if (frameImage != null)
            {
                Sprite frame = selected ? selectedFrame : locked ? lockedFrame : defaultFrame;
                if (frame != null)
                    frameImage.sprite = frame;
            }

            SetActive(selectedCheckImage, selected);
            SetActive(lockImage, locked);
            SetActive(warningImage, warning);
            if (disabledOverlay != null)
                disabledOverlay.SetActive(!enabled);

            if (selectionButton != null)
            {
                selectionButton.transition = Selectable.Transition.None;
                selectionButton.interactable = enabled;
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
