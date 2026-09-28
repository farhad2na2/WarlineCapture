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
        [SerializeField] private TMP_Text status;
        [SerializeField] private V3GradientGraphic frame;
        private void OnEnable() => button.onClick.AddListener(Select);
        private void OnDisable() => button.onClick.RemoveListener(Select);
        private void Select() => UiShellRuntimeGateway.SelectSupport(kind);
        public void Configure(byte ability,Button action,TMP_Text statusText,V3GradientGraphic border)
        {kind=ability;button=action;status=statusText;frame=border;}
        public void Render(UiSupportAbilityModel model,bool selected)
        {
            frame.Configure(new Color32(30,41,47,255),new Color32(3,9,13,255),selected?new Color32(255,193,20,255):new Color32(95,115,124,255),selected?4:2);
            string cost=UiShellRuntimeGateway.Localization.Format("support.card.status","{0} charges · {1} Fuel",model.Charges,model.FuelCost);
            string reason=UiShellRuntimeGateway.Localization.Get(model.ReasonKey,"");
            UiLocalizedText.Set(status,cost+(model.Available?"":"\n"+reason));
        }
    }
}
