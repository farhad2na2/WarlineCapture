using System;
using Game.Missions.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class CH02M01GridlockWatchProbe
    {
        private const string ManualModeKey="Warline.Gridlock.ManualTouchProbe";
        private static bool ManualMode=>SessionState.GetBool(ManualModeKey,false);
        private static double nextManualCapture;

        // Feasibility-only fixture. The agent supplies individual screen gestures
        // after inspecting captures; this path never selects entities or issues orders.
        public static void RunManual()
        {
            SessionState.SetBool(ManualModeKey,true);
            Begin();
        }

        private static void TickManual(MissionPhaseKind phase,byte ready)
        {
            if(phase!=MissionPhaseKind.Engage || ready==0)return;
            if(preparationTouch==null)
            {
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                preparationTouch=new AriaTouchInputUiSystemHelper();
                preparationTouch.Start();
            }
            preparationTouch.Tick(Time.unscaledTime);
            if(EditorApplication.timeSinceStartup>=nextManualCapture)
            {
                nextManualCapture=EditorApplication.timeSinceStartup+5;
                ScreenCapture.CaptureScreenshot(Output+"/manual-current.png");
            }
        }

        public static bool SubmitManualTouch(float x,float y,float endX,float endY,bool drag)
        {
            if(!ManualMode || preparationTouch==null || preparationTouch.IsBusy)return false;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            if(!preparationTouch.IsRunning && !preparationTouch.Start())return false;
            bool accepted=preparationTouch.TryGesture(new Vector2(x,y),new Vector2(endX,endY),drag ? .5f : .3f,drag ? .9f : 0,Time.unscaledTime);
            Debug.Log($"[GridlockManual] accepted={accepted} drag={drag} from=({x},{y}) to=({endX},{endY})");
            return accepted;
        }
    }
}
