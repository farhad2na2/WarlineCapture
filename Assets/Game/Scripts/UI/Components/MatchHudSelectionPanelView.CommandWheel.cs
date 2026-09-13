using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudSelectionPanelView
    {
        public bool HasWheelSelection => selectedSquadPanel != null && selectedSquadPanel.activeInHierarchy;
        public Sprite WheelPortrait => selectedPortraitImage != null ? selectedPortraitImage.sprite : null;

        // These original controls retain the selection command bindings and enabled state.
        // Their retired visual container stays hidden; the wheel is the only action surface.
        public Button ResolveWheelAction(int index) => index switch
        {
            0 => destroyAction,
            1 => returnAction,
            2 => cameraAction,
            3 => boardAction,
            _ => null
        };
    }
}
