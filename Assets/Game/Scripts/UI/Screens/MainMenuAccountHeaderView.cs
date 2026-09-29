using Game.UI.Contracts;
using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed class MainMenuAccountHeaderView : MonoBehaviour
    {
        [SerializeField] private TMP_Text credits;
        public TMP_Text Value => credits;
        public void Configure(TMP_Text value) => credits = value;
        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (credits == null) return;
            string value = UiShellRuntimeGateway.TryReadMainMenuResources(out var model)
                && !string.IsNullOrEmpty(model.CreditsText) ? model.CreditsText : "—";
            if (credits.text != value) credits.text = value;
        }
    }
}
