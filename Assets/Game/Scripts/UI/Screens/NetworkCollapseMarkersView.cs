using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed class NetworkCollapseMarkersView : MonoBehaviour
    {
        private GameObject root;private TacticalGroundMarkerView one,two,three,audit,exit;
        private void LateUpdate()
        {
            if(!UiShellRuntimeGateway.TryReadNetworkCollapse(out var m)||UiShellRuntimeGateway.TryReadMissionCameraTour()){if(root!=null)root.SetActive(false);return;}
            if(root==null){root=new GameObject("Network evidence objectives");one=Create("Recon one");two=Create("Recon two");three=Create("Recon three");audit=Create("Audit custody");exit=Create("Extraction");}
            root.SetActive(true);var cyan=new Color32(0,198,235,255);var amber=new Color32(255,191,48,255);var green=new Color32(94,224,112,255);
            Configure(one,m.ReconOne,4,m.NodesVerified>=1?green:cyan,"recon1");Configure(two,m.ReconTwo,4,m.NodesVerified>=2?green:cyan,"recon2");Configure(three,m.ReconThree,4,m.NodesVerified>=3?green:cyan,"recon3");Configure(audit,m.AuditGate,4,m.AuditRecovered?green:amber,"audit");Configure(exit,m.Extraction,5,m.Extracted?green:amber,"extraction");audit.gameObject.SetActive(m.Stage>=7);exit.gameObject.SetActive(m.Stage==8);
        }
        private void Configure(TacticalGroundMarkerView marker,Vector3 p,float radius,Color c,string key)=>marker.Configure(p,radius,c,UiShellRuntimeGateway.Localization.Get("mission.network_collapse.marker."+key),key=="extraction"?TacticalGroundMarkerView.Symbol.Departure:TacticalGroundMarkerView.Symbol.Clock,true);
        private TacticalGroundMarkerView Create(string name){var go=new GameObject(name);go.transform.SetParent(root.transform,false);return go.AddComponent<TacticalGroundMarkerView>();}
        private void OnDisable(){if(root!=null)root.SetActive(false);}private void OnDestroy(){if(root!=null)Destroy(root);}
    }
}
