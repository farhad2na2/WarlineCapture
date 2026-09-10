using Unity.Burst;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ThreatDetectionWarningSystem))]
    [UpdateAfter(typeof(CampaignMissionRuntimeSystem))]
    [BurstCompile]
    public partial struct ThreatWarningResolveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<ThreatWarningLedgerState>();

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<CampaignMissionRootComponent>(out Entity root)) return;
            EntityManager em = state.EntityManager;
            if (!em.HasComponent<ThreatWarningLedgerState>(root)) return;
            var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            var ledger = em.GetComponentData<ThreatWarningLedgerState>(root);
            var observations = em.GetBuffer<ThreatWarningObservation>(root);
            var records = em.GetBuffer<ThreatWarningRecord>(root);
            if (!Matches(in ledger, in runtime) || runtime.Outcome != MissionOutcomeKind.None)
            {
                if (ledger.Active != 0) { ledger.Active = 0; ledger.Version++; em.SetComponentData(root, ledger); }
                observations.Clear();
                return;
            }
            if (!SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) ||
                gameplay.PlayRequested == 0 || gameplay.SimulationActive == 0) return;
            var catalog = em.GetComponentData<CampaignMissionCatalogComponent>(root);
            if (!CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int definitionIndex)) return;
            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            if (definition.Defense.Enabled == 0 || !SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) ||
                !metadata.Blob.IsCreated) return;
            var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            bool changed = false, notify = false;
            for (int i = 0; i < observations.Length; i++)
            {
                ThreatWarningObservation observation = observations[i];
                if (!Accepts(in ledger, in observation) || observation.ElementIndex < 0 ||
                    observation.ElementIndex >= definition.Defense.Elements.Length ||
                    observation.ObservedAtMilliseconds > facts.ElapsedMilliseconds) continue;
                int index = Find(records, observation.ElementIndex);
                bool created = index < 0;
                if (created && records.Length >= 4) continue;
                ref CampaignMissionConvoyElementBlob authored = ref definition.Defense.Elements[observation.ElementIndex];
                ThreatWarningRecord record = created ? new ThreatWarningRecord
                {
                    ElementIndex = observation.ElementIndex, ElementId = authored.ElementId,
                    RouteId = authored.RouteId, ContactAnchorId = authored.ContactAnchorId,
                    KnownVehicleCount = -1, ContactAtMilliseconds = authored.ContactAtMilliseconds,
                    FirstReportedAtMilliseconds = observation.ObservedAtMilliseconds
                } : records[index];
                if (record.Resolved != 0 || observation.ObservedAtMilliseconds < record.ObservedAtMilliseconds) continue;
                // A later scout message cannot erase confirmed contact information.
                if (!created && observation.Source == ThreatWarningSourceKind.ScoutReport &&
                    record.Source != ThreatWarningSourceKind.ScoutReport) continue;
                bool upgraded = observation.Source != ThreatWarningSourceKind.ScoutReport &&
                                record.Source == ThreatWarningSourceKind.ScoutReport;
                record.Source = observation.Source;
                record.ObservedAtMilliseconds = observation.ObservedAtMilliseconds;
                record.KnownVehicleCount = observation.KnownVehicleCount;
                record.ObservedTarget = observation.Target;
                if (observation.HasPosition != 0)
                { record.FocusPosition = observation.Position; record.HasFocus = 1; }
                else if (CampaignMissionSpawnSystem.TryFindAnchor(ref metadata.Blob.Value, authored.ContactAnchorId, out var anchor))
                { record.FocusPosition = anchor.Position; record.HasFocus = 1; }
                if (created) records.Add(record); else records[index] = record;
                changed = true;
                notify |= created || upgraded;
            }
            observations.Clear();
            var elements = em.GetBuffer<CampaignMissionConvoyElementState>(root, true);
            int focus = -1;
            for (int i = 0; i < records.Length; i++)
            {
                ThreatWarningRecord old = records[i], record = old;
                if(record.ElementIndex<0 || record.ElementIndex>=elements.Length) {record.Resolved=1; records[i]=record; changed=true; continue;}
                record.Resolved = elements[record.ElementIndex].Resolved;
                UpdateFreshness(ref record, facts.ElapsedMilliseconds);
                changed |= old.EtaSeconds != record.EtaSeconds || old.Stale != record.Stale ||
                           old.Critical != record.Critical || old.Resolved != record.Resolved ||
                           old.AttentionEscalated != record.AttentionEscalated;
                notify |= old.Critical == 0 && record.Critical != 0 && record.Resolved == 0;
                records[i] = record;
                if (record.Resolved == 0 && (focus < 0 || RanksAhead(in record, records[focus]))) focus = i;
            }
            ledger.Active = focus >= 0 ? (byte)1 : (byte)0;
            ledger.FocusElementIndex = focus >= 0 ? records[focus].ElementIndex : -1;
            if (changed) ledger.Version++;
            if (notify) ledger.PresentationVersion++;
            em.SetComponentData(root, ledger);
        }

        internal static bool Matches(in ThreatWarningLedgerState ledger, in CampaignMissionRuntimeComponent runtime) =>
            ledger.SessionToken.Equals(runtime.SessionToken) && ledger.AttemptOrdinal == runtime.AttemptOrdinal &&
            ledger.SourceVersion == runtime.SourceVersion;

        internal static bool Accepts(in ThreatWarningLedgerState ledger, in ThreatWarningObservation observation) =>
            ledger.SessionToken.Equals(observation.SessionToken) && ledger.AttemptOrdinal == observation.AttemptOrdinal &&
            ledger.SourceVersion == observation.SourceVersion && observation.KnownVehicleCount >= -1 &&
            observation.ObservedAtMilliseconds>=0 && (observation.HasPosition==0 || math.all(math.isfinite(observation.Position))) &&
            observation.Source is >= ThreatWarningSourceKind.ScoutReport and <= ThreatWarningSourceKind.RadarPing;

        internal static void UpdateFreshness(ref ThreatWarningRecord record, int elapsed)
        {
            // One persistent chip reminder per unread element; it never replays speech
            // or upgrades tactical severity. Mission time makes pause and retry deterministic.
            if (record.ReadByPlayer == 0 && record.Resolved == 0 &&
                elapsed - record.FirstReportedAtMilliseconds >= 30000) record.AttentionEscalated = 1;
            record.EtaSeconds = math.max(0, (record.ContactAtMilliseconds - elapsed + 999) / 1000);
            record.ContactWindowOpen = elapsed >= record.ContactAtMilliseconds ? (byte)1 : (byte)0;
            record.Stale = record.Source != ThreatWarningSourceKind.ScoutReport &&
                           elapsed - record.ObservedAtMilliseconds > 15000 ? (byte)1 : (byte)0;
            record.Critical = record.EtaSeconds <= 15 && record.Resolved == 0 ? (byte)1 : (byte)0;
        }

        internal static bool RanksAhead(in ThreatWarningRecord candidate, in ThreatWarningRecord current) =>
            candidate.Critical != current.Critical ? candidate.Critical > current.Critical :
            candidate.ContactAtMilliseconds != current.ContactAtMilliseconds
                ? candidate.ContactAtMilliseconds < current.ContactAtMilliseconds : candidate.ElementIndex < current.ElementIndex;

        private static int Find(DynamicBuffer<ThreatWarningRecord> records, int element)
        {
            for (int i = 0; i < records.Length; i++) if (records[i].ElementIndex == element) return i;
            return -1;
        }
    }
}
