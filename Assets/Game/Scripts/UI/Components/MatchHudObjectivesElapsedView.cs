using TMPro;
using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MatchHudObjectivesElapsedView : MonoBehaviour
    {
        [SerializeField] private TMP_Text objectivesTitle;
        [SerializeField] private TMP_Text objective0Text;
        [SerializeField] private TMP_Text objective1Text;
        [SerializeField] private TMP_Text objective2Text;
        [SerializeField] private TMP_Text elapsedText;

        private void Update()
        {
            if (UiShellRuntimeGateway.TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel model))
                Apply(in model);
        }

        public void Apply(in UiMatchHudStatusSurfacesModel model)
        {
            SetText(objectivesTitle, model.ObjectivesTitle);
            SetText(objective0Text, model.Objective0.Text);
            SetText(objective1Text, model.Objective1.Text);
            SetText(objective2Text, model.Objective2.Text);
            SetText(elapsedText, model.ElapsedText);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null && target.text != value)
                target.text = value;
        }
    }
}
