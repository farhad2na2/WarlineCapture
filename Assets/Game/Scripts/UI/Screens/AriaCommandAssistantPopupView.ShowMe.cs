namespace Game.UI.Runtime
{
    public sealed partial class AriaCommandAssistantPopupView
    {
        internal void SetShowMeAvailable(bool available)
        {
            if (_showMeButton != null) _showMeButton.interactable = available;
        }
    }
}
