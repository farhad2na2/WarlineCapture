using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MatchHudCurrentOrderBannerView : MonoBehaviour
    {
        [SerializeField] private GameObject bannerRoot;
        [SerializeField] private GameObject chevrons;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text orderText;
        [SerializeField] private TMP_Text descriptionText;

        public GameObject BannerRoot => bannerRoot;
        public GameObject Chevrons => chevrons;
        public Image Icon => icon;
        public TMP_Text OrderText => orderText;
        public TMP_Text DescriptionText => descriptionText;

        private void OnEnable()
        {
            Apply(MatchHudCurrentOrderBannerModel.Hidden);
        }

        public void Apply(MatchHudCurrentOrderBannerModel model)
        {
            SetRootVisible(model.Visible);

            if (!model.Visible)
            {
                ClearVisuals();
                return;
            }

            if (chevrons != null)
            {
                if (chevrons.activeSelf != model.ChevronsVisible)
                    chevrons.SetActive(model.ChevronsVisible);
            }

            if (icon != null)
            {
                if (icon.sprite != model.IconSprite)
                    icon.sprite = model.IconSprite;
                if (!icon.preserveAspect)
                    icon.preserveAspect = true;
                bool iconEnabled = model.IconSprite != null;
                if (icon.enabled != iconEnabled)
                    icon.enabled = iconEnabled;
            }

            if (orderText != null)
            {
                string text = model.OrderText ?? string.Empty;
                if (orderText.text != text)
                    orderText.text = text;
            }

            if (descriptionText != null)
            {
                string text = model.DescriptionText ?? string.Empty;
                if (descriptionText.text != text)
                    descriptionText.text = text;
            }
        }

        public void Hide()
        {
            Apply(MatchHudCurrentOrderBannerModel.Hidden);
        }

        private void SetRootVisible(bool visible)
        {
            if (bannerRoot != null && bannerRoot.activeSelf != visible)
            {
                bannerRoot.SetActive(visible);
            }
        }

        private void ClearVisuals()
        {
            if (chevrons != null)
            {
                if (chevrons.activeSelf)
                    chevrons.SetActive(false);
            }

            if (icon != null)
            {
                if (icon.sprite != null)
                    icon.sprite = null;
                if (icon.enabled)
                    icon.enabled = false;
            }

            if (orderText != null)
            {
                if (!string.IsNullOrEmpty(orderText.text))
                    orderText.text = string.Empty;
            }

            if (descriptionText != null)
            {
                if (!string.IsNullOrEmpty(descriptionText.text))
                    descriptionText.text = string.Empty;
            }
        }
    }
}
