using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct ThreatDetectionWarningSystem
    {
        private Entity PendingPingSensor(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<CampaignMissionRootComponent>(out Entity root) ||
                !state.EntityManager.HasComponent<RadarPingState>(root)) return Entity.Null;
            EntityManager em = state.EntityManager;
            var ping = em.GetComponentData<RadarPingState>(root);
            if (ping.PendingRequestId == 0) return Entity.Null;
            var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if (RadarPingRequestSystem.Matches(in ping, in runtime) && RadarPingRequestSystem.IsLiveSensor(em, ping.PendingSensor))
                return ping.PendingSensor;
            ping.PendingRequestId = 0; ping.PendingSensor = Entity.Null;
            ping.Result = RadarPingResultKind.NoSensor; ping.Version++;
            em.SetComponentData(root, ping);
            return Entity.Null;
        }

        private void FinishPing(ref SystemState state, Entity sensor, NativeList<Entity> contacts)
        {
            if (sensor == Entity.Null || !SystemAPI.TryGetSingletonEntity<CampaignMissionRootComponent>(out Entity root)) return;
            EntityManager em = state.EntityManager;
            var ping = em.GetComponentData<RadarPingState>(root);
            if (ping.PendingRequestId == 0 || ping.PendingSensor != sensor) return;
            PublishDefenseObservations(ref state, contacts, ThreatWarningSourceKind.RadarPing);
            int elapsed = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root).ElapsedMilliseconds;
            // A completed empty scan is useful information and costs one use. Rejected requests cost nothing.
            ping.Charges--; ping.ReadyAtMilliseconds = elapsed + ping.CooldownMilliseconds;
            ping.PendingSensor = Entity.Null; ping.PendingRequestId = 0;
            ping.LastContactCount = contacts.Length; ping.Result = RadarPingResultKind.Accepted; ping.Version++;
            em.SetComponentData(root, ping);
            var defense = em.GetComponentData<CampaignMissionDefenseStateComponent>(root);
            defense.PingUsed = 1; em.SetComponentData(root, defense);
        }
    }
}
