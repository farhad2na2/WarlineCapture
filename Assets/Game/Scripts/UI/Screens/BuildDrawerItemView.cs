using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BuildDrawerItemView : MonoBehaviour
    {
        [SerializeField] private Button selectionButton;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private TMP_Text descriptionText;
        [FormerlySerializedAs("creditsCostText"), SerializeField] private TMP_Text materialsCostText;
        [FormerlySerializedAs("suppliesCostText"), SerializeField] private TMP_Text fuelCostText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text requirementsText;
        [SerializeField] private GameObject disabledOverlay;

        private Sprite _normalFrameSprite;

        public Button SelectionButton => selectionButton;
        public Image FrameImage => frameImage;
        public Image ThumbnailImage => thumbnailImage;

        private void Awake()
        {
            CaptureNormalFrameSprite();
            DisableTransientSelectableFrameState();
        }

        public void CaptureNormalFrameSprite()
        {
            if (_normalFrameSprite == null && frameImage != null)
                _normalFrameSprite = frameImage.sprite;
        }

        public void BindText(
            string displayName,
            string role,
            string description,
            string materialsCost,
            string fuelCost,
            string time,
            string requirements)
        {
            SetText(nameText, displayName);
            SetText(roleText, role);
            SetText(descriptionText, description);
            SetCost(materialsCostText, materialsCost);
            SetCost(fuelCostText, fuelCost);
            SetText(timeText, time);
            SetText(requirementsText, requirements);
        }

        public void BindThumbnail(Sprite sprite)
        {
            if (thumbnailImage == null)
                return;

            thumbnailImage.sprite = sprite;
            thumbnailImage.enabled = sprite != null;
        }

        public void SetSelected(bool selected, Sprite selectedFrameSprite)
        {
            CaptureNormalFrameSprite();
            DisableTransientSelectableFrameState();
            if (frameImage == null)
                return;

            Sprite target = selected ? selectedFrameSprite : _normalFrameSprite;
            if (target != null)
                frameImage.sprite = target;
        }

        public void SetInteractable(bool interactable)
        {
            DisableTransientSelectableFrameState();
            if (selectionButton != null)
                selectionButton.interactable = interactable;

            if (disabledOverlay != null)
                disabledOverlay.SetActive(!interactable);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }

        private static void SetCost(TMP_Text text, string value)
        {
            if (text == null)
                return;

            bool visible = !string.IsNullOrWhiteSpace(value);
            text.text = visible ? value : string.Empty;
            Transform costGroup = text.transform.parent;
            if (costGroup != null)
                costGroup.gameObject.SetActive(visible);
        }

        private void DisableTransientSelectableFrameState()
        {
            if (selectionButton != null)
                selectionButton.transition = Selectable.Transition.None;
        }
    }
}
