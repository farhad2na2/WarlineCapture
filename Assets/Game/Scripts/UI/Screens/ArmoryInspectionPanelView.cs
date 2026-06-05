using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArmoryInspectionPanelView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private ArmoryCatalogCategoryVisualSet[] categoryVisuals;

    public void Bind(ArmoryCatalogItem model)
    {
        if (titleText != null)
            titleText.text = model.DisplayName;

        if (typeText != null)
            typeText.text = ArmoryCatalogCategoryFormatter.Format(model.Category);

        BindCategoryVisuals(model.Category, model.Portrait);
    }

    public void Clear()
    {
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
