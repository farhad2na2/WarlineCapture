using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ThreatDetectionWarningSystem))]
    public partial struct RadarPingRequestSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<RadarPingState>();

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<CampaignMissionRootComponent>(out Entity root)) return;
            EntityManager em = state.EntityManager;
            if (!em.HasComponent<RadarPingState>(root) || !em.HasBuffer<RadarPingRequest>(root)) return;
            var ping = em.GetComponentData<RadarPingState>(root);
            var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            var requests = em.GetBuffer<RadarPingRequest>(root);
            bool live = Matches(in ping, in runtime) && runtime.Phase == MissionPhaseKind.Engage &&
                        runtime.Outcome == MissionOutcomeKind.None;
            bool playing = SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) && gameplay.PlayRequested != 0;
            if (!live || !playing)
            {
                if (ping.PendingRequestId != 0) { ping.PendingRequestId = 0; ping.PendingSensor = Entity.Null; ping.Result = RadarPingResultKind.Unavailable; ping.Version++; }
                requests.Clear(); em.SetComponentData(root, ping); return;
            }
            int elapsed = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root).ElapsedMilliseconds;
            for (int i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                if (!request.SessionToken.Equals(runtime.SessionToken) || request.AttemptOrdinal != runtime.AttemptOrdinal ||
                    request.SourceVersion != runtime.SourceVersion || request.RequestId == 0 || request.RequestId <= ping.LastRequestId) continue;
                ping.LastRequestId = request.RequestId;
                if (ping.PendingRequestId != 0) { ping.Result = RadarPingResultKind.Busy; ping.Version++; continue; }
                Entity sensor = request.Sensor == Entity.Null ? FindSensor(em, root) : request.Sensor;
                var rejected = Validate(in ping, elapsed, gameplay.SimulationActive != 0, IsLiveSensor(em, sensor));
                ping.Result = rejected;
                ping.Version++;
                if (rejected != RadarPingResultKind.Pending) continue;
                ping.PendingRequestId = request.RequestId;
                ping.PendingSensor = sensor;
            }
            requests.Clear();
            em.SetComponentData(root, ping);
        }

        internal static RadarPingResultKind Validate(in RadarPingState ping, int elapsed, bool simulationActive, bool sensorLive)
        {
            if (!simulationActive) return RadarPingResultKind.Paused;
            if (ping.Charges <= 0) return RadarPingResultKind.NoCharges;
            if (elapsed < ping.ReadyAtMilliseconds) return RadarPingResultKind.Cooldown;
            return sensorLive ? RadarPingResultKind.Pending : RadarPingResultKind.NoSensor;
        }

        internal static bool Matches(in RadarPingState ping, in CampaignMissionRuntimeComponent runtime) =>
            ping.SessionToken.Equals(runtime.SessionToken) && ping.AttemptOrdinal == runtime.AttemptOrdinal && ping.SourceVersion == runtime.SourceVersion;

        public static bool IsLiveSensor(EntityManager em, Entity sensor) =>
            sensor != Entity.Null && em.Exists(sensor) && !em.HasComponent<CampaignMissionCombatSuppressedTag>(sensor) &&
            em.HasComponent<Faction>(sensor) && FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(sensor).Id) &&
            em.HasComponent<UnitHealth>(sensor) && em.GetComponentData<UnitHealth>(sensor).Current > 0 &&
            em.HasComponent<UnitGrid>(sensor) && em.HasComponent<ThreatDetector>(sensor) &&
            em.GetComponentData<ThreatDetector>(sensor).Kind == (byte)ThreatDetectionKind.Ground &&
            em.GetComponentData<ThreatDetector>(sensor).RadiusCells > 0;

        public static Entity FindSensor(EntityManager em, Entity root)
        {
            if (!em.HasBuffer<CampaignMissionDefenseMember>(root)) return Entity.Null;
            var members = em.GetBuffer<CampaignMissionDefenseMember>(root, true);
            for (int i = 0; i < members.Length; i++)
                if (members[i].IsSensor != 0 && IsLiveSensor(em, members[i].Entity)) return members[i].Entity;
            return Entity.Null;
        }
    }
}
