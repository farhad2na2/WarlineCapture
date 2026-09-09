using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct ThreatDetectionWarningSystem
    {
        private bool PublishDefenseObservations(ref SystemState state, NativeList<Entity> groundContacts,
            ThreatWarningSourceKind sourceOverride = ThreatWarningSourceKind.None)
        {
            if (!SystemAPI.TryGetSingletonEntity<CampaignMissionRootComponent>(out Entity root)) return false;
            EntityManager em = state.EntityManager;
            if (!em.HasComponent<ThreatWarningLedgerState>(root)) return false;
            var ledger = em.GetComponentData<ThreatWarningLedgerState>(root);
            var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if (!ThreatWarningResolveSystem.Matches(in ledger, in runtime)) return false;
            if (runtime.Outcome != MissionOutcomeKind.None || runtime.Phase != MissionPhaseKind.Engage) return true;
            var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            // Snapshot before adding LastSeen components; structural changes invalidate buffers.
            using var members = em.GetBuffer<CampaignMissionDefenseMember>(root, true).ToNativeArray(Allocator.Temp);
            int elementCount = em.GetBuffer<CampaignMissionConvoyElementState>(root, true).Length;
            for (int elementIndex = 0; elementIndex < elementCount; elementIndex++)
            {
                int count = 0;
                Entity target = Entity.Null;
                float3 position = default;
                ThreatWarningSourceKind source = ThreatWarningSourceKind.VisualContact;
                for (int i = 0; i < members.Length; i++)
                {
                    CampaignMissionDefenseMember member = members[i];
                    if (member.ElementIndex != elementIndex || member.Defeated != 0 ||
                        !groundContacts.Contains(member.Entity) || !em.Exists(member.Entity) ||
                        !em.HasComponent<LocalTransform>(member.Entity)) continue;
                    int2 cell = em.GetComponentData<UnitGrid>(member.Entity).Cell;
                    float3 observed = em.GetComponentData<LocalTransform>(member.Entity).Position;
                    count++;
                    if (target == Entity.Null) { target = member.Entity; position = observed; }
                    if (sourceOverride != ThreatWarningSourceKind.None) source = sourceOverride;
                    else if (HasGroundCoverage(ref state, cell)) source = ThreatWarningSourceKind.GroundSensor;
                    if (!em.HasComponent<ScanIntelRevealedTag>(member.Entity)) em.AddComponent<ScanIntelRevealedTag>(member.Entity);
                    var seen = new ScanIntelLastSeen
                    {
                        Cell = cell, Position = observed, FactionId = member.FactionId,
                        LastScanFrame = facts.ElapsedMilliseconds
                    };
                    if (em.HasComponent<ScanIntelLastSeen>(member.Entity)) em.SetComponentData(member.Entity, seen);
                    else em.AddComponentData(member.Entity, seen);
                }
                if (count == 0) continue;
                var observations = em.GetBuffer<ThreatWarningObservation>(root);
                if (observations.Length >= 16) continue;
                observations.Add(new ThreatWarningObservation
                {
                    SessionToken = runtime.SessionToken, AttemptOrdinal = runtime.AttemptOrdinal,
                    SourceVersion = runtime.SourceVersion, ElementIndex = elementIndex, Source = source,
                    KnownVehicleCount = count, ObservedAtMilliseconds = facts.ElapsedMilliseconds,
                    Target = target, Position = position, HasPosition = 1
                });
            }
            return true;
        }

        private bool HasGroundCoverage(ref SystemState state, int2 cell)
        {
            foreach (var (detector, faction, grid, health) in
                     SystemAPI.Query<RefRO<ThreatDetector>, RefRO<Faction>, RefRO<UnitGrid>, RefRO<UnitHealth>>())
                if (faction.ValueRO.Id == PlayerFactionId && health.ValueRO.Current > 0 &&
                    detector.ValueRO.Kind == (byte)ThreatDetectionKind.Ground && detector.ValueRO.RadiusCells > 0 &&
                    ChebyshevDistance(cell, grid.ValueRO.Cell) <= detector.ValueRO.RadiusCells) return true;
            return false;
        }

        private void PublishLegacyWarning(ref SystemState state, ThreatScanResult scan, int groundCount, int airCount)
        {
            bool ground = scan.HasNewGroundThreat != 0 &&
                          (scan.HasNewAirThreat == 0 || scan.BestGroundEtaSeconds <= scan.BestAirEtaSeconds);
            if (!ground && scan.HasNewAirThreat == 0) return;
            ThreatWarningType type = ground ? ThreatWarningType.Ground : ThreatWarningType.Air;
            float eta = ground ? scan.BestGroundEtaSeconds : scan.BestAirEtaSeconds;
            if (eta == float.MaxValue) eta = 0;
            int count = ground ? groundCount : airCount;
            ThreatWarningRuntimeState.RequestWarning(state.EntityManager, _warningStateQuery, type, eta, count);
            ThreatWarningAudioEventUtility.TryEmit(state.EntityManager, type, eta, count, (float)SystemAPI.Time.ElapsedTime);
        }
    }
}
