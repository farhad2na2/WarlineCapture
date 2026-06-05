using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ArmoryCatalogItemView : MonoBehaviour
{
    [SerializeField] private Button selectionButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private ArmoryCatalogCategoryVisualSet[] categoryVisuals;

    public Button SelectionButton => selectionButton;

    public void Bind(ArmoryCatalogItem model)
    {
        if (titleText != null)
            titleText.text = model.DisplayName;

        if (typeText != null)
            typeText.text = ArmoryCatalogCategoryFormatter.Format(model.Category);

        BindCategoryVisuals(model.Category, model.Portrait);
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
