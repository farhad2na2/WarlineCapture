using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArmoryInspectionPanelView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private ArmoryCatalogCategoryVisualSet[] categoryVisuals;

    public void Bind(ArmoryCatalogItem model)
    {
        if (titleText != null)
            titleText.text = model.DisplayName;

        if (typeText != null)
            typeText.text = model.TypeLabel;

        if (descriptionText != null)
            descriptionText.text = model.Description;

        BindCategoryVisuals(model.Category, model.InspectionPortrait);
    }

    public void Clear()
    {
        if (titleText != null)
            titleText.text = string.Empty;

        if (typeText != null)
            typeText.text = string.Empty;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (categoryVisuals == null)
            return;

        for (int i = 0; i < categoryVisuals.Length; i++)
            categoryVisuals[i]?.Bind(false, null);
    }

    private void BindCategoryVisuals(ArmoryCatalogCategory selectedCategory, Sprite portrait)
    {
        if (categoryVisuals == null)
            return;

        for (int i = 0; i < categoryVisuals.Length; i++)
        {
            ArmoryCatalogCategoryVisualSet visualSet = categoryVisuals[i];
            visualSet?.Bind(visualSet.Category == selectedCategory, portrait);
        }
    }
}
