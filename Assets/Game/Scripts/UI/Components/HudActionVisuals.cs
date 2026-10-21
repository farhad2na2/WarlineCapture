using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Keep every layer of an unavailable action opaque and neutral, without changing its gameplay gate.</summary>
    public static class HudActionVisuals
    {
        public static void Refresh(Button button)
        {
            if (button == null) return;
            bool disabled = !button.IsInteractable();
            UiDisabledMaterialUtility.SetDisabled(button.gameObject, UiDisabledVisualReason.UnavailableAction, disabled);
            UiDisabledMaterialUtility.SetSelectableDisabled(button, UiDisabledVisualReason.UnavailableAction, disabled);
        }
    }
}
