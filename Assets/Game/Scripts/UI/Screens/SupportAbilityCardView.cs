using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    public sealed class SupportAbilityCardView : MonoBehaviour
    {
        [SerializeField] private byte kind;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text status,charges,fuel;
        [SerializeField] private GameObject unavailable;
        [SerializeField] private V3GradientGraphic frame;
        private void OnEnable() => button.onClick.AddListener(Select);
        private void OnDisable() => button.onClick.RemoveListener(Select);
        private void Select() => UiShellRuntimeGateway.SelectSupport(kind);
        public void Configure(byte ability,Button action,TMP_Text statusText,V3GradientGraphic border)
        {kind=ability;button=action;status=statusText;frame=border;}
        public void ConfigureCost(TMP_Text chargesText,TMP_Text fuelText,GameObject unavailableArea)
        {charges=chargesText;fuel=fuelText;unavailable=unavailableArea;}
        public void Render(UiSupportAbilityModel model,bool selected)
        {
            frame.Configure(new Color32(30,41,47,255),new Color32(3,9,13,255),selected?new Color32(255,193,20,255):new Color32(95,115,124,255),selected?4:2);
            UiLocalizedText.Set(charges,model.Charges.ToString());
            UiLocalizedText.Set(fuel,model.FuelCost.ToString());
            string reason=UiShellRuntimeGateway.Localization.Get(model.ReasonKey,"");
            if(model.Cooldown>0)reason=UiShellRuntimeGateway.Localization.Format("support.ui.cooldown","Ready in {0}s",model.Cooldown);
            UiLocalizedText.Set(status,reason);
            unavailable.SetActive(!model.Available);

        }
    }
}
