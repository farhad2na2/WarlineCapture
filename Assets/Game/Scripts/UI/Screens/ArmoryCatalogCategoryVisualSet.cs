using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class ArmoryCatalogCategoryVisualSet
{
    [SerializeField] private ArmoryCatalogCategory category;
    [SerializeField] private GameObject backgroundRoot;
    [SerializeField] private Image artImage;

    public ArmoryCatalogCategory Category => category;
    public GameObject BackgroundRoot => backgroundRoot;
    public Image ArtImage => artImage;

    public void Bind(bool selected, Sprite portrait)
    {
        if (backgroundRoot != null)
            backgroundRoot.SetActive(selected);

        if (artImage == null)
            return;

        if (selected)
        {
            artImage.sprite = portrait;
            artImage.preserveAspect = true;
            artImage.enabled = portrait != null;
            return;
        }

        artImage.enabled = false;
    }
}
