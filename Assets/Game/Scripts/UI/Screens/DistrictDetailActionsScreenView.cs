using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class DistrictDetailActionsScreenView : MonoBehaviour
    {
        [SerializeField] private UIShellRouteButtonView backRouteButton;
        [SerializeField] private RawImage districtImage;
        [SerializeField] private Image ariaPortrait;
        [SerializeField] private TMP_Text districtName;
        [SerializeField] private TMP_Text threatLabel;
        [SerializeField] private Slider intelConfidence;
        [SerializeField] private Button[] actionButtons;

        public UIShellRouteButtonView BackRouteButton => backRouteButton;
        public RawImage DistrictImage => districtImage;
        public Image AriaPortrait => ariaPortrait;
        public TMP_Text DistrictName => districtName;
        public TMP_Text ThreatLabel => threatLabel;
        public Slider IntelConfidence => intelConfidence;
        public Button[] ActionButtons => actionButtons;
    }
}
