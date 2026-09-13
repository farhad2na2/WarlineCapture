using Game.UI.Contracts;
using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class BuildDrawerView
    {
        [SerializeField] private TMP_Text materialsBalance, oilBalance, fuelBalance, creditsBalance;
        [SerializeField] private TMP_Text creditPrice;

        public void RefreshResources()
        {
            bool materialsOnly = UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions) &&
                restrictions.UsesMaterialsOnlyConstruction;
            if (creditsBalance != null) creditsBalance.transform.parent.gameObject.SetActive(!materialsOnly);
            if (creditPrice != null) creditPrice.transform.parent.gameObject.SetActive(!materialsOnly);
            if (UiShellRuntimeGateway.TryReadMatchHudHeader(out var header))
                UiLocalizedText.Set(materialsBalance, header.MaterialsText);
            if (!UiShellRuntimeGateway.TryReadMatchHudResourceValues(out var values) || !values.IsValid)
            {
                UiLocalizedText.Set(oilBalance, "—");
                UiLocalizedText.Set(fuelBalance, "—");
                UiLocalizedText.Set(creditsBalance, "—");
                return;
            }
            UiLocalizedText.Set(oilBalance, values.Oil.ToString("N0"));
            UiLocalizedText.Set(fuelBalance, values.Fuel.ToString("N0"));
            UiLocalizedText.Set(creditsBalance, values.Credits.ToString("N0"));
        }

        public void BindCreditCost(int credits) =>
            UiLocalizedText.Set(creditPrice, Mathf.Max(0, credits).ToString("N0"));
    }
}
