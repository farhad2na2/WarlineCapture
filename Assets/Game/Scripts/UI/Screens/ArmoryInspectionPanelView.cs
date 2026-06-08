using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArmoryInspectionPanelView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text healthValueText;
    [SerializeField] private TMP_Text damageValueText;
    [SerializeField] private TMP_Text rangeValueText;
    [SerializeField] private TMP_Text speedValueText;
    [SerializeField] private TMP_Text moveCapabilityText;
    [SerializeField] private TMP_Text patrolCapabilityText;
    [SerializeField] private TMP_Text attackCapabilityText;
    [SerializeField] private TMP_Text holdCapabilityText;
    [SerializeField] private ArmoryCatalogCategoryVisualSet[] categoryVisuals;

    public void Bind(ArmoryCatalogItem model)
    {
        if (titleText != null)
            titleText.text = model.DisplayName;

        if (typeText != null)
            typeText.text = model.TypeLabel;

        if (descriptionText != null)
            descriptionText.text = model.Description;

        if (healthValueText != null)
            healthValueText.text = model.HealthValue;

        if (damageValueText != null)
            damageValueText.text = model.DamageValue;

        if (rangeValueText != null)
            rangeValueText.text = model.RangeValue;

        if (speedValueText != null)
            speedValueText.text = model.SpeedValue;

        if (moveCapabilityText != null)
            moveCapabilityText.text = model.MoveCapability;

        if (patrolCapabilityText != null)
            patrolCapabilityText.text = model.PatrolCapability;

        if (attackCapabilityText != null)
            attackCapabilityText.text = model.AttackCapability;

        if (holdCapabilityText != null)
            holdCapabilityText.text = model.HoldCapability;

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

        if (healthValueText != null)
            healthValueText.text = string.Empty;

        if (damageValueText != null)
            damageValueText.text = string.Empty;

        if (rangeValueText != null)
            rangeValueText.text = string.Empty;

        if (speedValueText != null)
            speedValueText.text = string.Empty;

        if (moveCapabilityText != null)
            moveCapabilityText.text = string.Empty;

        if (patrolCapabilityText != null)
            patrolCapabilityText.text = string.Empty;

        if (attackCapabilityText != null)
            attackCapabilityText.text = string.Empty;

        if (holdCapabilityText != null)
            holdCapabilityText.text = string.Empty;

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
