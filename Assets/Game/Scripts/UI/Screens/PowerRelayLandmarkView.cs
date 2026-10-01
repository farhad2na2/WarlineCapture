using UnityEngine;
using Game.Configs;
namespace Game.UI.Runtime
{
    // Labels the existing school shelter frontage; owns no building or command.
    public sealed class PowerRelayLandmarkView : MonoBehaviour
    {
        private TacticalGroundMarkerView marker;
        private void LateUpdate()
        {
            bool active=UiShellRuntimeGateway.TryReadPowerRelayShelter(out var center,out bool sheltered);
            if(!active){if(marker!=null)marker.gameObject.SetActive(false);return;}
            if(marker==null){var go=new GameObject("School shelter landmark");go.transform.SetParent(transform,false);marker=go.AddComponent<TacticalGroundMarkerView>();}
            marker.gameObject.SetActive(true);
            marker.Configure(center,8,sheltered?new Color32(92,205,137,255):new Color32(255,191,48,255),
                GameText.Get("mission.power_relay.marker.shelter"),sheltered?TacticalGroundMarkerView.Symbol.Check:TacticalGroundMarkerView.Symbol.People,true);
        }
        private void OnDisable(){if(marker!=null)marker.gameObject.SetActive(false);}
    }
}
