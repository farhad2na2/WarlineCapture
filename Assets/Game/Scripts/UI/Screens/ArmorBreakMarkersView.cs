using UnityEngine;
namespace Game.UI.Runtime
{
    // Uses the approved marker renderer; has no input surface or command controls.
    public sealed class ArmorBreakMarkersView : MonoBehaviour
    {
        private GameObject root;
        private TacticalGroundMarkerView coverage,authority,relief;
        private void LateUpdate()
        {
            bool active=UiShellRuntimeGateway.TryReadArmorBreak(out var model) && !UiShellRuntimeGateway.TryReadMissionCameraTour();
            if(!active){if(root!=null)root.SetActive(false);return;}
            if(root==null)
            {root=new GameObject("Armor Break objectives");coverage=Create("Air defense coverage");authority=Create("Authority package");relief=Create("Protected relief");}
            root.SetActive(true);
            var cyan=new Color32(0,198,235,255);var amber=new Color32(255,191,48,255);var green=new Color32(94,224,112,255);
            coverage.gameObject.SetActive(model.Stage<=2);
            coverage.Configure(model.Coverage,6,cyan,UiShellRuntimeGateway.Localization.Get("mission.armor_break.marker.coverage"),TacticalGroundMarkerView.Symbol.Clock,true);
            authority.gameObject.SetActive(model.Stage>=5);
            authority.Configure(model.Authority,5,amber,UiShellRuntimeGateway.Localization.Get("mission.armor_break.marker.authority"),TacticalGroundMarkerView.Symbol.People,true);
            relief.Configure(model.Relief,6,green,UiShellRuntimeGateway.Localization.Get("mission.armor_break.marker.relief"),TacticalGroundMarkerView.Symbol.People,true);
        }
        private TacticalGroundMarkerView Create(string name)
        {var go=new GameObject(name);go.transform.SetParent(root.transform,false);return go.AddComponent<TacticalGroundMarkerView>();}
        private void OnDisable(){if(root!=null)root.SetActive(false);}
        private void OnDestroy(){if(root!=null)Destroy(root);}
    }
}
