using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            chevrons.SetActive(model.ChevronsVisible);
        }

        if (icon != null)
        {
            icon.sprite = model.IconSprite;
            icon.preserveAspect = true;
            icon.enabled = model.IconSprite != null;
        }

        if (orderText != null)
        {
            orderText.text = model.OrderText;
        }

        if (descriptionText != null)
        {
            descriptionText.text = model.DescriptionText;
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
            chevrons.SetActive(false);
        }

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        if (orderText != null)
        {
            orderText.text = string.Empty;
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
        }
    }
}
