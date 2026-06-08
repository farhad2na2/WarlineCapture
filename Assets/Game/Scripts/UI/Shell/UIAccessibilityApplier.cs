using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIAccessibilityApplier : MonoBehaviour
{
    [SerializeField] private RectTransform scaleRoot;
    [SerializeField] private bool applyScale = true;
    [SerializeField] private bool skipScaleWhenParentApplierExists = true;
    [SerializeField] private float largeTextScale = 1.08f;
    [SerializeField] private Graphic[] highContrastTargets = Array.Empty<Graphic>();
    [SerializeField] private Color normalTargetColor = new(0.004f, 0.016f, 0.019f, 1f);
    [SerializeField] private Color highContrastTargetColor = Color.black;

    private void OnEnable()
    {
        SettingsService.RuntimeApplied += Apply;
        Apply(SettingsService.Load());
    }

    private void OnDisable()
    {
        SettingsService.RuntimeApplied -= Apply;
    }

    public void Apply(UISettingsModel model)
    {
        if (applyScale && scaleRoot != null && ShouldApplyScale())
            scaleRoot.localScale = model.Accessibility.LargeText ? Vector3.one * largeTextScale : Vector3.one;

        Color targetColor = model.Accessibility.HighContrastUi ? highContrastTargetColor : normalTargetColor;
        foreach (Graphic target in highContrastTargets)
        {
            if (target != null)
                target.color = targetColor;
        }
    }

    private bool ShouldApplyScale()
    {
        if (!skipScaleWhenParentApplierExists)
            return true;

        Transform parent = transform.parent;
        while (parent != null)
        {
            if (parent.GetComponent<UIAccessibilityApplier>() != null)
                return false;

            parent = parent.parent;
        }

        return true;
    }
}
