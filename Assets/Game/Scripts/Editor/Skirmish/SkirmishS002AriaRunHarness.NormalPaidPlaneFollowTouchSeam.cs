#if UNITY_EDITOR
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    // Disabled narrow extension seam; no entry and no automatic actuation.
    // Caller first proves exact normal paid Transport single selection, idle
    // ordinary driver, native Move receipt/cruise and presented HUD read model.
    public static partial class SkirmishS002AriaRunHarness
    {
        static bool TryTapNormalPaidPlanePresentedFollow()
        {
            var panels=Object.FindObjectsByType<MatchHudSelectionPanelView>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
            if(panels.Length!=1||!panels[0].isActiveAndEnabled||playerResumeTouch==null||playerResumeTouch.IsBusy)return false;
            using(var serialized=new SerializedObject(panels[0])){
                var property=serialized.FindProperty("cameraAction");
                var button=property!=null?property.objectReferenceValue as Button:null;
                if(button==null||!button.isActiveAndEnabled||!button.interactable)return false;
                return PlayerResumeTap(button);
            }
        }
    }
}
#endif
