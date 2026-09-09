using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class MatchHudResourceIconView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Sprite fuel, credits;

        public void ShowMissionCredits(bool active)
        {
            if (icon != null) icon.sprite = active ? credits : fuel;
        }
    }
}
