using UnityEngine;
namespace Game.UI.Runtime
{
    // Uses the approved marker renderer; has no input surface or command controls.
    public sealed class GroundedSignalMarkersView : MonoBehaviour
    {
        private GameObject root;
        private TacticalGroundMarkerView apron,hardware,exit;
        private void LateUpdate()
        {
            bool active=UiShellRuntimeGateway.TryReadGroundedSignal(out var model) && !UiShellRuntimeGateway.TryReadMissionCameraTour();
            if(!active){if(root!=null)root.SetActive(false);return;}
            if(root==null)
            {
                root=new GameObject("Grounded Signal objectives");
                apron=Create("Insertion apron");hardware=Create("Relay hardware");exit=Create("Guarded exit");
            }
            root.SetActive(true);
            var cyan=new Color32(0,198,235,255);var amber=new Color32(255,191,48,255);var green=new Color32(94,224,112,255);
            apron.gameObject.SetActive(model.Stage==1);
            apron.Configure(model.Apron,5,cyan,UiShellRuntimeGateway.Localization.Get("mission.grounded_signal.marker.apron"),TacticalGroundMarkerView.Symbol.People,true);
            hardware.gameObject.SetActive(model.Stage is 2 or 3);
            hardware.Configure(model.Hardware,5,amber,UiShellRuntimeGateway.Localization.Get("mission.grounded_signal.marker.hardware"),TacticalGroundMarkerView.Symbol.Clock,true);
            exit.gameObject.SetActive(model.HardwareRecovered);
            exit.Configure(model.Exit,8,model.Contested?amber:green,UiShellRuntimeGateway.Localization.Get("mission.grounded_signal.marker.exit"),TacticalGroundMarkerView.Symbol.Departure,true);
        }
        private TacticalGroundMarkerView Create(string name)
        {var go=new GameObject(name);go.transform.SetParent(root.transform,false);return go.AddComponent<TacticalGroundMarkerView>();}
        private void OnDisable(){if(root!=null)root.SetActive(false);}
        private void OnDestroy(){if(root!=null)Destroy(root);}
    }
}
