using Game.UI.Contracts;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    public sealed partial class AriaTutorialBriefingView
    {
        // Kept as an empty compatibility property for older integrations. Selection
        // now uses the normal command bar and world input, never a panel shortcut.
        private Button selectionButton;
        private string selectionInstruction;
        public Button SelectionButton => null;
        private void EnsureSelectionButton() { }
        internal void SetSelectionActionAvailable(bool available,string labelKey="ui.aria.select_group")
        {
            if(selectionButton!=null)selectionButton.gameObject.SetActive(false);
            if(available)return;
            if(selectionInstruction!=null && _currentInstructionBody==selectionInstruction)
                ApplyInteractionState(Game.Tactical.Contracts.TacticalCommandMode.None,false);
            selectionInstruction=null;
        }
        internal void SetNormalSelectionInstruction(bool selectMode,bool drag)
        {
            bool fa=UiShellRuntimeGateway.Localization.CurrentLocaleCode.StartsWith("fa");
            string key=!selectMode?"ui.aria.press_select.body":drag?"ui.aria.drag_select.body":"ui.aria.tap_select.body";
            string fallback=!selectMode?(fa?"دکمهٔ «انتخاب» رو توی نوار فرمان بزن.":"Press Select on the command bar."):
                drag?(fa?"از گوشهٔ کادر مشخص‌شده بکش تا دور همهٔ نیروها کادر بیفته، بعد رها کن.":"Drag from one corner of the marked box to the opposite corner, enclosing the units, then release."):
                (fa?"روی نیروی مشخص‌شده توی میدان بزن.":"Tap the marked unit in the world.");
            selectionInstruction=UiShellRuntimeGateway.Localization.Get(key,fallback);
            if(_currentInstructionBody!=selectionInstruction)
                ApplyInstruction(UiShellRuntimeGateway.Localization.Get("ui.aria.normal_selection.title",fa?"نیروها رو انتخاب کن":"Select units"),selectionInstruction,UiTutorialNarrationPhase.PrimaryAction);
            SetContinueAvailable(false);
        }
    }
}
