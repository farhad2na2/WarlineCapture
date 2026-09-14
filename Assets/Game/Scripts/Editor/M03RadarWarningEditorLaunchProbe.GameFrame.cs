using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private static bool insideBuildingGameFrame;
        private static BuildingGameFrameDriver buildingGameFrameDriver;
        private static void EnsureBuildingGameFrame()
        {
            if(buildingGameFrameDriver!=null) return;
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if(type!=null) EditorWindow.GetWindow(type).Focus();
            var owner=new GameObject("M3 construction gameplay QA");
            Object.DontDestroyOnLoad(owner);
            buildingGameFrameDriver=owner.AddComponent<BuildingGameFrameDriver>();
        }
        public sealed class BuildingGameFrameDriver : MonoBehaviour
        {
            private IEnumerator Start()
            {
                while(SessionState.GetBool(ActiveKey,false))
                {
                    yield return new WaitForEndOfFrame();
                    insideBuildingGameFrame=true;
                    try {Tick();} finally {insideBuildingGameFrame=false;}
                }
            }
        }
    }
}
