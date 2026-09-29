using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class CH04M03SplitFrontInputProbe
    {
        private static GameObject clickHost;
        private static Button queuedUiButton;
        private sealed class PlayerFrameUiInputHost:MonoBehaviour
        {
            private void Update()
            {
                var button=queuedUiButton;queuedUiButton=null;
                if(button==null||!button.isActiveAndEnabled||!button.IsInteractable())return;
                // Dispatch native UI in a player frame. EditorApplication.update's
                // Screen dimensions can describe the editor surface, so invoking a
                // narrative transition there selects the wrong aspect crop.
                Debug.Log($"[SplitFrontNativeUiClick] button={button.name} screen={Screen.width}x{Screen.height}");
                ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
            }
        }
        private static bool QueuePlayerFrameClick(Button button)
        {
            if(button==null||!button.isActiveAndEnabled||!button.IsInteractable()||queuedUiButton!=null)return false;
            if(clickHost==null)clickHost=new GameObject("Split Front Native UI Input",typeof(PlayerFrameUiInputHost)){hideFlags=HideFlags.DontSave};
            queuedUiButton=button;return true;
        }
        private static void FinishPlayerFrameInput()
        {queuedUiButton=null;if(clickHost!=null)Object.Destroy(clickHost);clickHost=null;}
    }
}
