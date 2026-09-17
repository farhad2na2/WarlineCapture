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
            PublishScanFeedback(em, sensor, ping);
        }
        private static void PublishScanFeedback(EntityManager em, Entity sensor, in RadarPingState ping)
        {
            if (!em.HasComponent<UnitGrid>(sensor) || !em.HasComponent<Unity.Transforms.LocalTransform>(sensor)) return;
            using var queue = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RtsSelectionInputRequestQueueComponent, RtsSelectionCommandResultElement>().Build(em);
            if (queue.CalculateEntityCount() != 1) return;
            em.GetBuffer<RtsSelectionCommandResultElement>(queue.GetSingletonEntity()).Add(new RtsSelectionCommandResultElement
            {
                Kind = RtsSelectionCommandIntentKind.Scan, RequestId = (int)ping.LastRequestId,
                SourceEntity = sensor, HasSourceEntity = 1, Accepted = 1, HasCommandResult = 1,
                TargetCell = em.GetComponentData<UnitGrid>(sensor).Cell, HasTargetCell = 1,
                WorldPosition = em.GetComponentData<Unity.Transforms.LocalTransform>(sensor).Position, HasWorldPosition = 1,
                RadiusCells = em.GetComponentData<ThreatDetector>(sensor).RadiusCells,
                RevealedCount = ping.LastContactCount, ShowWorldMarkers = 1
            });
        }
    }
}
