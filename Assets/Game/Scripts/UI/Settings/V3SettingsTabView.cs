using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class V3SettingsTabView : MonoBehaviour
    {
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private GameObject[] pages;
        [SerializeField] private V3GradientGraphic[] tabBackgrounds;
        [SerializeField] private Image[] selectionRails;
        [SerializeField] private TMP_Text[] tabLabels;
        [SerializeField] private Color[] accentColors;
        [SerializeField] private Color inactiveBackground = new(0.035f, 0.055f, 0.064f, 0.96f);
        [SerializeField] private Color inactiveText = new(0.78f, 0.82f, 0.84f, 1f);
        [SerializeField] private Color inactiveBorder = new(0.30f, 0.33f, 0.34f, 1f);
        [SerializeField] private int defaultTab;

        private UnityAction[] _callbacks;
        private int _selectedIndex = -1;

        public Button[] TabButtons => tabButtons;
        public GameObject[] Pages => pages;
        public int SelectedIndex => _selectedIndex;

        private void Awake()
        {
            WireButtons();
            SelectTab(defaultTab);
        }

        private void OnDestroy()
        {
            if (tabButtons == null || _callbacks == null)
                return;

            for (int i = 0; i < tabButtons.Length && i < _callbacks.Length; i++)
            {
                if (tabButtons[i] != null && _callbacks[i] != null)
                    tabButtons[i].onClick.RemoveListener(_callbacks[i]);
            }
        }

        public void SelectTab(int index)
        {
            int count = Mathf.Min(tabButtons?.Length ?? 0, pages?.Length ?? 0);
            if (count == 0)
                return;

            _selectedIndex = Mathf.Clamp(index, 0, count - 1);
            for (int i = 0; i < count; i++)
            {
                bool selected = i == _selectedIndex;
                if (pages[i] != null)
                    pages[i].SetActive(selected);

                Color accent = accentColors != null && i < accentColors.Length
                    ? accentColors[i]
                    : Color.cyan;
                if (tabBackgrounds != null && i < tabBackgrounds.Length && tabBackgrounds[i] != null)
                {
                    Color inactiveTopLeft = Color.Lerp(inactiveBackground, Color.white, 0.1f);
                    Color inactiveTopRight = Color.Lerp(inactiveBackground, Color.white, 0.045f);
                    Color inactiveBottomLeft = Color.Lerp(inactiveBackground, Color.black, 0.34f);
                    Color inactiveBottomRight = Color.Lerp(inactiveBackground, Color.black, 0.18f);
                    Color saturatedAccent = new Color(
                        0f,
                        accent.g * 0.5f,
                        accent.b * 0.55f,
                        1f);
                    Color selectedTopLeft = saturatedAccent;
                    Color selectedTopRight = Color.Lerp(Color.black, saturatedAccent, 0.92f);
                    Color selectedBottomLeft = Color.Lerp(Color.black, saturatedAccent, 0.44f);
                    Color selectedBottomRight = Color.Lerp(Color.black, saturatedAccent, 0.50f);
                    tabBackgrounds[i].SetGradientCorners(
                        selected ? selectedTopLeft : inactiveTopLeft,
                        selected ? selectedTopRight : inactiveTopRight,
                        selected ? selectedBottomLeft : inactiveBottomLeft,
                        selected ? selectedBottomRight : inactiveBottomRight);
                    tabBackgrounds[i].SetBorder(
                        selected ? Color.Lerp(saturatedAccent, Color.cyan, 0.62f) : inactiveBorder,
                        3f);
                }
                if (tabButtons[i] != null && tabButtons[i].targetGraphic != null)
                    tabButtons[i].targetGraphic.color = Color.white;
                if (selectionRails != null && i < selectionRails.Length && selectionRails[i] != null)
                {
                    selectionRails[i].color = accent;
                    selectionRails[i].gameObject.SetActive(selected);
                }
                if (tabLabels != null && i < tabLabels.Length && tabLabels[i] != null)
                    tabLabels[i].color = selected ? Color.white : inactiveText;
            }
        }

        private void WireButtons()
        {
            if (tabButtons == null)
                return;

            _callbacks = new UnityAction[tabButtons.Length];
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int capturedIndex = i;
                _callbacks[i] = () => SelectTab(capturedIndex);
                if (tabButtons[i] != null)
                    tabButtons[i].onClick.AddListener(_callbacks[i]);
            }
        }
    }
}
