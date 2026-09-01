using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class SkirmishSetupV3SegmentVisual : MonoBehaviour
    {
        [SerializeField] private Button[] buttons;
        [SerializeField] private V3GradientGraphic[] gradients;
        [SerializeField] private int fixedSelectedIndex = -1;
        [SerializeField] private Color normalTop;
        [SerializeField] private Color normalBottom;
        [SerializeField] private Color normalBorder;
        [SerializeField] private Color selectedTop;
        [SerializeField] private Color selectedBottom;
        [SerializeField] private Color selectedBorder;
        [SerializeField] private float borderWidth = 3f;

        private int _lastSelected = int.MinValue;

        public void Configure(
            Button[] configuredButtons,
            V3GradientGraphic[] configuredGradients,
            int configuredFixedSelectedIndex,
            Color configuredNormalTop,
            Color configuredNormalBottom,
            Color configuredNormalBorder,
            Color configuredSelectedTop,
            Color configuredSelectedBottom,
            Color configuredSelectedBorder,
            float configuredBorderWidth)
        {
            buttons = configuredButtons;
            gradients = configuredGradients;
            fixedSelectedIndex = configuredFixedSelectedIndex;
            normalTop = configuredNormalTop;
            normalBottom = configuredNormalBottom;
            normalBorder = configuredNormalBorder;
            selectedTop = configuredSelectedTop;
            selectedBottom = configuredSelectedBottom;
            selectedBorder = configuredSelectedBorder;
            borderWidth = configuredBorderWidth;
            Refresh(true);
        }

        private void OnEnable() => Refresh(true);

        private void LateUpdate() => Refresh(false);

        private void Refresh(bool force)
        {
            int selected = ResolveSelectedIndex();
            if (!force && selected == _lastSelected)
                return;
            _lastSelected = selected;
            int count = Mathf.Min(buttons?.Length ?? 0, gradients?.Length ?? 0);
            for (int i = 0; i < count; i++)
            {
                V3GradientGraphic gradient = gradients[i];
                if (gradient == null)
                    continue;
                bool isSelected = i == selected;
                gradient.ConfigureCorners(
                    Color.Lerp(isSelected ? selectedTop : normalTop, Color.white, .035f),
                    isSelected ? selectedTop : normalTop,
                    Color.Lerp(isSelected ? selectedBottom : normalBottom, Color.black, .12f),
                    isSelected ? selectedBottom : normalBottom,
                    isSelected ? selectedBorder : normalBorder,
                    borderWidth);
            }
        }

        private int ResolveSelectedIndex()
        {
            if (fixedSelectedIndex >= 0)
                return fixedSelectedIndex;
            if (buttons == null)
                return -1;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && !buttons[i].interactable)
                    return i;
            }
            return -1;
        }
    }
}
