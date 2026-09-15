using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class AriaTutorialBriefingView
    {
        private Button selectionButton;
        private TMP_Text selectionButtonLabel;
        private string selectionInstruction;
        private float selectionQueuedAt=-1;
        public Button SelectionButton => selectionButton;
        internal void SetSelectionActionAvailable(bool available,string labelKey="ui.aria.select_group")
        {
            if(!available)
            {
                if(selectionButton!=null) selectionButton.gameObject.SetActive(false);
                selectionQueuedAt=-1;
                if(selectionInstruction!=null && _currentInstructionBody==selectionInstruction)
                    ApplyInteractionState(Game.Tactical.Contracts.TacticalCommandMode.None,false);
                selectionInstruction=null;
                return;
            }
            if(selectionButton==null)
            {
                selectionButton=Instantiate(doItButton,doItButton.transform.parent);
                selectionButton.name="SelectMissionGroupButton";
                selectionButton.onClick=new Button.ButtonClickedEvent();
                selectionButton.onClick.AddListener(SelectLessonGroup);
                selectionButtonLabel=selectionButton.GetComponentInChildren<TMP_Text>(true);
            }
            bool pending=selectionQueuedAt>=0 && Time.unscaledTime-selectionQueuedAt<1;
            selectionButton.gameObject.SetActive(!pending);
            selectionButton.interactable=!pending;
            string label=UiShellRuntimeGateway.Localization.Get(labelKey ?? "ui.aria.select_group");
            UiLocalizedText.Set(selectionButtonLabel,label);
            selectionInstruction=string.Format(UiShellRuntimeGateway.Localization.Get("ui.aria.select_group.body"),label);
            if(_currentInstructionBody!=selectionInstruction)
                ApplyInstruction(UiShellRuntimeGateway.Localization.Get("ui.aria.select_group.title"),selectionInstruction,Game.UI.Contracts.UiTutorialNarrationPhase.PrimaryAction);
            SetContinueAvailable(false);
        }
        private void SelectLessonGroup()
        {
            if(selectionButton==null || !selectionButton.IsActive() || !selectionButton.IsInteractable()) return;
            if(!UiShellRuntimeGateway.TrySelectMissionTutorialGroup()) return;
            selectionQueuedAt=Time.unscaledTime;
            selectionButton.interactable=false;
            selectionButton.gameObject.SetActive(false);
            RefreshContentLayout();
        }
    }
}
