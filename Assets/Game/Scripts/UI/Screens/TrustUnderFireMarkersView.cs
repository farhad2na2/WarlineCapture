using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed class TrustUnderFireMarkersView : MonoBehaviour
    {
        private GameObject root;
        private TacticalGroundMarkerView north, south, northCrossing, southCrossing, relay;
        private void LateUpdate()
        {
            if (!UiShellRuntimeGateway.TryReadTrustUnderFire(out var m) || UiShellRuntimeGateway.TryReadMissionCameraTour())
            { if (root != null) root.SetActive(false); return; }
            if (root == null)
            {
                root = new GameObject("Trust Under Fire objectives");
                north = Create("North shelter"); south = Create("South shelter");
                northCrossing = Create("North crossing"); southCrossing = Create("South crossing");
                relay = Create("Broadcast gate");
            }
            root.SetActive(true);
            var cyan = new Color32(0, 198, 235, 255);
            var amber = new Color32(255, 191, 48, 255);
            var green = new Color32(94, 224, 112, 255);
            Configure(north, m.NorthArrival, 4, m.NorthArrived ? green : amber, "north", TacticalGroundMarkerView.Symbol.People);
            Configure(south, m.SouthArrival, 4, m.SouthArrived ? green : amber, "south", TacticalGroundMarkerView.Symbol.People);
            northCrossing.gameObject.SetActive(m.Stage <= 4 && !m.NorthCrossed);
            southCrossing.gameObject.SetActive(m.Stage <= 4 && !m.SouthCrossed);
            Configure(northCrossing, m.NorthCrossing, 5, cyan, "north_crossing", TacticalGroundMarkerView.Symbol.Clock);
            Configure(southCrossing, m.SouthCrossing, 5, cyan, "south_crossing", TacticalGroundMarkerView.Symbol.Clock);
            relay.gameObject.SetActive(m.Stage >= 5);
            Configure(relay, m.RelayGate, 4, m.RelayVerified ? green : amber, "relay", TacticalGroundMarkerView.Symbol.Clock);
        }
        private static void Configure(TacticalGroundMarkerView marker, Vector3 position, float radius, Color color, string key, TacticalGroundMarkerView.Symbol symbol)
            => marker.Configure(position, radius, color, UiShellRuntimeGateway.Localization.Get("mission.trust_under_fire.marker." + key), symbol, true);
        private TacticalGroundMarkerView Create(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform, false);
            return go.AddComponent<TacticalGroundMarkerView>();
        }
        private void OnDisable() { if (root != null) root.SetActive(false); }
        private void OnDestroy() { if (root != null) Destroy(root); }
    }
}
