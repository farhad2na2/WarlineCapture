using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed class CommandNodeMarkersView : MonoBehaviour
    {
        private TacticalGroundMarkerView marker;
        private void LateUpdate()
        {
            if(!UiShellRuntimeGateway.TryReadCommandNode(out var m)||UiShellRuntimeGateway.TryReadMissionCameraTour()){if(marker!=null)marker.gameObject.SetActive(false);return;}
            if(marker==null)marker=new GameObject("Civic Relay active objective").AddComponent<TacticalGroundMarkerView>();
            Vector3 p=m.Stage switch{2=>m.IsolationStep==0?m.IsolationClinic:m.IsolationUtility,4=>m.CoverReady?m.BreachGate:m.CoverGate,6=>m.CoreAudit,7=>m.AuditRelease,8=>m.SafeReceiving,_=>Vector3.zero};
            marker.gameObject.SetActive(m.Stage is 2 or 4 or 6 or 7 or 8);
            if(marker.gameObject.activeSelf)marker.Configure(p,4,new Color32(0,198,235,255),UiShellRuntimeGateway.Localization.Get("mission.command_node.marker.stage."+m.Stage),TacticalGroundMarkerView.Symbol.Clock,true);
        }
        private void OnDisable(){if(marker!=null)marker.gameObject.SetActive(false);}private void OnDestroy(){if(marker!=null)Destroy(marker.gameObject);}
    }
}
