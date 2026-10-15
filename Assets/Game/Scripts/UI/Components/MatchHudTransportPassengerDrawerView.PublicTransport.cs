using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudTransportPassengerDrawerView
    {
        public bool TryReadPresentedExitControls(out Button exit,out Button close)
        {
            exit=null;close=null;
            if(!isActiveAndEnabled||drawerRoot==null||!drawerRoot.activeInHierarchy)return false;
            if(exitAllButton!=null&&exitAllButton.gameObject.activeInHierarchy&&exitAllButton.interactable)exit=exitAllButton;
            if(closeButton!=null&&closeButton.gameObject.activeInHierarchy&&closeButton.interactable)close=closeButton;
            // An open, disabled exit is still presented. The caller must not tap
            // an unavailable exit or infer a successful native disembark.
            return true;
        }
    }
}
