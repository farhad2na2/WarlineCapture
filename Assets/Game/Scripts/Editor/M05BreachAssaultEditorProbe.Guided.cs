using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static partial class M05BreachAssaultEditorProbe
    {
        private static double nextGuidedClick;
        public static void RunGuidedEnglish()
        {
            SessionState.SetBool("Warline.M05.GuideOnly",false);SessionState.SetBool("Warline.M05.RetryProbe",false);SessionState.SetBool("Warline.M05.Guided",true);
            Run();
        }
        private static void TickGuided()
        {
            if(EditorApplication.timeSinceStartup<nextGuidedClick)return;
            var views=Object.FindObjectsByType<AriaTutorialBriefingView>(FindObjectsInactive.Exclude);
            foreach(var view in views)
            {
                if(!view.IsPresentationVisible)continue;
                if(view.ShowMeButton!=null && view.ShowMeButton.isActiveAndEnabled && view.ShowMeButton.interactable)
                    view.ShowMeButton.onClick.Invoke();
                if(view.DoItButton!=null && view.DoItButton.isActiveAndEnabled && view.DoItButton.interactable)
                    view.DoItButton.onClick.Invoke();
                ScreenCapture.CaptureScreenshot(Output+"/guided-current-en.png");
                nextGuidedClick=EditorApplication.timeSinceStartup+3;
                return;
            }
        }
    }
}
