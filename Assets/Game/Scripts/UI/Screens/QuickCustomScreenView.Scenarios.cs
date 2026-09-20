using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        [SerializeField] private Button[] scenarioButtons;
        [SerializeField] private TMP_Text scenarioDescription;

        private void WireScenarioChoices()
        {
            if (scenarioButtons == null) return;
            for (int i = 0; i < scenarioButtons.Length; i++)
            {
                int index = i;
                if (scenarioButtons[i] != null) scenarioButtons[i].onClick.AddListener(() => SelectScenario(index));
            }
        }

        public void SelectScenario(int index)
        {
            if (!baseAssaultPreset || index < 0 || index > 1) return;
            _config.ScenarioIndex = index;
            Bind(_config);
            _configStore?.Apply(ReadConfigFromControls());
        }

        private void PresentScenarioChoices()
        {
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (scenarioButtons != null)
                for (int i = 0; i < scenarioButtons.Length; i++)
                {
                    var button = scenarioButtons[i];
                    if (button == null) continue;
                    bool selected = i == _config.ScenarioIndex;
                    string title = i == 0 ? (fa ? "۱ · پایگاه کویری" : "1 · DESERT BASE")
                        : (fa ? "۲ · چهارراه شهر" : "2 · CITY CROSSROADS");
                    UiLocalizedText.Set(button.GetComponentInChildren<TMP_Text>(),
                        title + "\n" + (selected ? (fa ? "انتخاب‌شده" : "SELECTED") : (fa ? "انتخاب نبرد" : "SELECT BATTLE")));
                    if (button.targetGraphic is V3GradientGraphic gradient)
                        gradient.Configure(selected ? new Color(0,.38f,.52f) : new Color(.12f,.17f,.18f),
                            new Color(.02f,.06f,.08f), selected ? new Color(0,.8f,1) : new Color(.3f,.36f,.38f), selected ? 4 : 2);
                }
            if (scenarioDescription != null)
                UiLocalizedText.Set(scenarioDescription, _config.ScenarioIndex == 1
                    ? (fa ? "از شمال شهر به پایگاه جنوبی حمله کن؛ از خیابون‌ها یا مسیرهای کناری جلو برو." : "Attack from the north toward the southern base. Use city streets or flank around the blocks.")
                    : (fa ? "از پایگاه غربی جلو برو؛ از وسط شهر یا مسیر کنار شهر به دشمن برس." : "Advance from the western base. Fight through the city or approach around its edge."));
        }
    }
}
