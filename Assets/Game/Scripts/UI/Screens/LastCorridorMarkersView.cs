using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed class LastCorridorMarkersView : MonoBehaviour
    {
        private GameObject root;private TacticalGroundMarkerView repair,medicine,fuel,troops,keys;
        private void LateUpdate()
        {
            if(!UiShellRuntimeGateway.TryReadLastCorridor(out var m)||UiShellRuntimeGateway.TryReadMissionCameraTour()){if(root!=null)root.SetActive(false);return;}
            if(root==null){root=new GameObject("Last Corridor receiving objectives");repair=Create("Repair gate");medicine=Create("Medical receiving");fuel=Create("Fuel receiving");troops=Create("Reinforcement receiving");keys=Create("Key custody receiving");}
            root.SetActive(true);var cyan=new Color32(0,198,235,255);var amber=new Color32(255,191,48,255);var green=new Color32(94,224,112,255);
            Configure(repair,m.RepairGate,4,m.LinkRecovered?green:amber,"repair");Configure(medicine,m.MedicineGate,5,m.MedicineDelivered?green:cyan,"medical");Configure(fuel,m.FuelGate,5,m.FuelDelivered?green:amber,"fuel");Configure(troops,m.ReinforcementGate,5,m.ReinforcementsDelivered?green:cyan,"reinforcements");Configure(keys,m.KeyReceiver,5,m.KeysDelivered?green:amber,"keys");
            repair.gameObject.SetActive(m.Stage<=2);medicine.gameObject.SetActive(m.Stage==3);fuel.gameObject.SetActive(m.Stage==4);troops.gameObject.SetActive(m.Stage==5);keys.gameObject.SetActive(m.Stage==8);
        }
        private void Configure(TacticalGroundMarkerView marker,Vector3 p,float radius,Color c,string key)=>marker.Configure(p,radius,c,UiShellRuntimeGateway.Localization.Get("mission.last_corridor.marker."+key),TacticalGroundMarkerView.Symbol.Clock,true);
        private TacticalGroundMarkerView Create(string name){var go=new GameObject(name);go.transform.SetParent(root.transform,false);return go.AddComponent<TacticalGroundMarkerView>();}
        private void OnDisable(){if(root!=null)root.SetActive(false);}private void OnDestroy(){if(root!=null)Destroy(root);}
    }
}
