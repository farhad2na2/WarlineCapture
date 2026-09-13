using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class CommandWheelPanelView
    {
        [SerializeField] private Button[] selectionActions = new Button[0];
        [SerializeField] private Image selectionPortrait;
        private MatchHudSelectionPanelView _selection;
        public Button NextBoardButton => IsOpen && selectionActions.Length == 4 ? selectionActions[3] : OpenButton;

        private void BindSelectionWheelActions()
        {
            for (int i = 0; i < selectionActions.Length; i++)
            {
                int action = i;
                selectionActions[i].onClick.AddListener(() => InvokeSelectionAction(action));
            }
        }

        private void RefreshOpenSelection()
        {
            if (IsOpen && selectionActions.Length > 0 && !RefreshSelection()) Close();
        }

        public bool RefreshSelection()
        {
            if (_selection == null && openButton != null)
                _selection = openButton.GetComponentInParent<MatchHudSelectionPanelView>();
            if (_selection == null || !_selection.HasWheelSelection) return false;
            if (selectionPortrait != null)
            {
                selectionPortrait.sprite = _selection.WheelPortrait;
                selectionPortrait.enabled = selectionPortrait.sprite != null;
            }
            for (int i = 0; i < selectionActions.Length; i++)
            {
                Button source = _selection.ResolveWheelAction(i);
                bool enabled = source != null && source.interactable;
                selectionActions[i].interactable = enabled;
                var group = selectionActions[i].GetComponent<CanvasGroup>();
                if (group != null) group.alpha = enabled ? 1f : .65f;
            }
            return true;
        }

        private void InvokeSelectionAction(int index)
        {
            if (!RefreshSelection()) { Close(); return; }
            Button source = _selection.ResolveWheelAction(index);
            if (source == null || !source.interactable) return;
            // Close before dispatch: Board can enter targeting and Camera can change modes.
            Close();
            source.onClick.Invoke();
        }
    }
}
