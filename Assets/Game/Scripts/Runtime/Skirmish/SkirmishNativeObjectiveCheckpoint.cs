using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    // Installed only by the future definition-referenced area/view compiler.
    // There is deliberately no loader which constructs this from checkpoint data.
    internal sealed class SkirmishNativeObjectiveRecoveryBinding : IComponentData
    {
        internal readonly SkirmishCompiledObjectiveAreas Areas;
        internal readonly string MissionViewProfileFingerprint;
        // Empty managed defaults cannot prepare recovery: Areas/view are null.
        public SkirmishNativeObjectiveRecoveryBinding() {}
        internal SkirmishNativeObjectiveRecoveryBinding(SkirmishCompiledObjectiveAreas areas,string view)
        { Areas=areas;MissionViewProfileFingerprint=view; }
    }
    // Native Snapshot.Objective is serialized INSIDE the existing checksummed
    // payload. This is not the fixture codec and does not authorize a resume.
    internal static class SkirmishNativeObjectiveCheckpoint
    {
        internal const int NativeVersion = 8;
        internal sealed class Prepared
        {
            internal readonly string PayloadJson;
            internal readonly SkirmishCompiledObjectiveAreas Areas;
            internal readonly string View;
            internal Prepared(Payload payload,SkirmishNativeObjectiveRecoveryBinding binding)
            {PayloadJson=JsonUtility.ToJson(payload);Areas=binding.Areas;View=binding.MissionViewProfileFingerprint;}
        }
        [Serializable] internal sealed class Payload
        {
            public int Version = 1;
            public string LogicalFingerprint, GeometryFingerprint, LayoutBindingFingerprint, MissionViewProfileFingerprint;
            public SkirmishObjectiveKind Kind;
            public SkirmishObjectiveCollectorClock Clock;
            public SkirmishObjectiveCollectorOriginalRow[] Originals;
            // Exactly one canonical typed JSON state. No geometry or entity handles.
            public string Reducer;
        }

        internal static bool Schema(int version, Payload payload, SkirmishObjectiveKind kind) =>
            kind == SkirmishObjectiveKind.BaseAssault ? version == 7 && payload == null :
            version == NativeVersion && payload != null && payload.Version == 1 && payload.Kind == kind;

        internal static bool TryCapture(EntityManager em, Entity session, out Payload payload, out string error)
        {
            payload = null; error = "Native objective owner missing.";
            if(!em.HasComponent<SkirmishObjectiveStateComponent>(session))return false;
            var objective=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            if(objective.Terminal!=0||objective.Outcome!=SkirmishOutcomeKind.None||objective.State!=SkirmishObjectiveStateKind.Active)return false;
            if (!em.HasComponent<SkirmishNativeObjectiveRecord>(session)) return false;
            var record = em.GetComponentObject<SkirmishNativeObjectiveRecord>(session);
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (record?.Collector == null || record.Areas == null || !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                !record.Collector.TryExportCheckpointRows(em, session, out var originals, out error)) return false;
            var value = new Payload { Kind = state.ObjectiveKind, Originals = originals, Clock = record.Clock,
                LogicalFingerprint = record.Areas.LogicalFingerprint, GeometryFingerprint = record.Areas.GeometryFingerprint,
                LayoutBindingFingerprint = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup.LayoutBindingFingerprint,
                MissionViewProfileFingerprint = record.MissionViewProfileFingerprint };
            if (value.Kind == SkirmishObjectiveKind.FrontlineControl) value.Reducer = JsonUtility.ToJson(record.Frontline);
            else if (value.Kind == SkirmishObjectiveKind.Breakthrough) value.Reducer = JsonUtility.ToJson(record.Breakthrough);
            else if (value.Kind == SkirmishObjectiveKind.ConvoyEscort) value.Reducer = JsonUtility.ToJson(record.Convoy);
            else return false;
            payload = value; error = null; return true;
        }

        internal static bool TryPrepare(EntityManager em,Entity session,
            SkirmishNativeCheckpointSnapshotService.Snapshot saved,SkirmishResolvedSetup setup,
            out Prepared prepared,out string error)
        {
            prepared=null;error="Fresh definition-referenced objective binding unavailable.";
            if (!Validate(saved?.Objective,saved,setup,out error) ||
                !em.HasComponent<SkirmishNativeObjectiveRecoveryBinding>(session) ||
                em.HasComponent<SkirmishNativeObjectiveRecord>(session)) return false;
            var binding=em.GetComponentObject<SkirmishNativeObjectiveRecoveryBinding>(session);
            if(binding?.Areas==null || binding.Areas.ObjectiveKind!=setup.ObjectiveKind ||
                binding.Areas.LogicalFingerprint!=saved.Objective.LogicalFingerprint ||
                binding.Areas.GeometryFingerprint!=saved.Objective.GeometryFingerprint ||
                binding.MissionViewProfileFingerprint!=saved.Objective.MissionViewProfileFingerprint)return false;
            prepared=new Prepared(saved.Objective,binding);error=null;return true;
        }

        internal static void Commit(EntityManager em,Entity session,Prepared prepared)
        {
            if(prepared==null || em.HasComponent<SkirmishNativeObjectiveRecord>(session) ||
                !em.HasComponent<SkirmishNativeObjectiveRecoveryBinding>(session))
                throw new InvalidOperationException("Fresh objective commit owner changed.");
            var binding=em.GetComponentObject<SkirmishNativeObjectiveRecoveryBinding>(session);
            if(!ReferenceEquals(binding.Areas,prepared.Areas)||binding.MissionViewProfileFingerprint!=prepared.View)
                throw new InvalidOperationException("Fresh objective binding changed after preparation.");
            var payload=JsonUtility.FromJson<Payload>(prepared.PayloadJson);
            if(!TryRebind(em,session,payload,prepared.Areas,prepared.View,out var record,out string error))
                throw new InvalidOperationException("Native objective rebind failed: "+error);
            em.AddComponentObject(session,record);
        }

        // Called after native Parts validation. The session/actor bytes below are
        // the actual native save bytes, never rows synthesized from the payload.
        internal static bool Validate(Payload value, SkirmishNativeCheckpointSnapshotService.Snapshot saved,
            SkirmishResolvedSetup setup, out string error)
        {
            error = "Invalid native objective payload.";
            if (value == null || saved == null || setup == null || !Schema(saved.Version, value, setup.ObjectiveKind) ||
                !Hex(value.LogicalFingerprint) || !Hex(value.GeometryFingerprint) || !Hex(value.MissionViewProfileFingerprint) ||
                value.LayoutBindingFingerprint != setup.LayoutBindingFingerprint || value.Originals == null ||
                string.IsNullOrEmpty(value.Reducer) || value.Reducer.Length > 65536 ||
                !double.IsFinite(value.Clock.LastAuthoritativeSeconds) || value.Clock.LastAuthoritativeSeconds < 0 ||
                !double.IsFinite(value.Clock.FractionalTicks) || value.Clock.FractionalTicks < 0 || value.Clock.FractionalTicks >= 1 ||
                !SkirmishNativeCheckpointParts.TryReadComponent(saved.SessionParts, out SkirmishObjectiveClockComponent clock) ||
                !float.IsFinite(clock.ElapsedSeconds) || clock.ElapsedSeconds != value.Clock.LastAuthoritativeSeconds ||
                !SkirmishNativeCheckpointParts.TryReadComponent(saved.SessionParts, out SkirmishExpandedSessionComponent session) ||
                session.ObjectiveKind != value.Kind || session.Phase != SkirmishSessionPhase.Playing || session.SpawnComplete == 0)
                return false;
            if(!SkirmishNativeCheckpointParts.TryReadComponent(saved.SessionParts,out SkirmishObjectiveStateComponent objective)||
                objective.Terminal!=0||objective.Outcome!=SkirmishOutcomeKind.None||objective.State!=SkirmishObjectiveStateKind.Active)return false;
            try
            {
                int count = value.Kind == SkirmishObjectiveKind.FrontlineControl ? 2 : value.Kind == SkirmishObjectiveKind.Breakthrough ? 12 : 3;
                if (value.Originals.Length != count) return false;
                var rows = new Dictionary<string, SkirmishObjectiveCollectorOriginalRow>(StringComparer.Ordinal);
                foreach (var row in value.Originals)
                {
                    if (string.IsNullOrEmpty(row.StableOriginalId) || string.IsNullOrEmpty(row.ObjectiveStableId) ||
                        !rows.TryAdd(row.StableOriginalId, row) || row.IsStructure > 1 || row.Faction < 1 || row.Faction > 2 ||
                        (byte)row.Disposition > 3) return false;
                    SkirmishNativeCheckpointSnapshotService.Actor actor = null;
                    foreach (var candidate in saved.Actors) if (candidate.Id == row.StableOriginalId) actor = candidate;
                    bool present = actor != null;
                    if (row.Disposition == SkirmishOriginalObjectiveDisposition.Live && !present ||
                        (row.Disposition == SkirmishOriginalObjectiveDisposition.Evacuated || row.Disposition == SkirmishOriginalObjectiveDisposition.Delivered) && present) return false;
                    if (present)
                    {
                        if (!SkirmishNativeCheckpointParts.TryReadComponent(actor.Parts, out SkirmishAttemptOwnedComponent owner) ||
                            owner.FactionId != row.Faction || owner.IsStructure != row.IsStructure || owner.ReservationId != 0 ||
                            !SkirmishNativeCheckpointParts.TryReadComponent(actor.Parts,out Faction actualFaction) || actualFaction.Id != row.Faction ||
                            (row.Disposition == SkirmishOriginalObjectiveDisposition.Live && owner.CapacityReleased != 0) ||
                            !SkirmishNativeCheckpointParts.TryReadComponent(actor.Parts, out SkirmishObjectiveRoleComponent objectiveRole) ||
                            objectiveRole.Role != row.Role || objectiveRole.FactionId != row.Faction || objectiveRole.StableObjectId.ToString() != row.ObjectiveStableId ||
                            !SkirmishNativeCheckpointParts.TryReadComponent(actor.Parts, out UnitHealth health) ||
                            (row.Disposition == SkirmishOriginalObjectiveDisposition.Live ? health.Current <= 0 : health.Current > 0)) return false;
                        if(row.IsStructure==0 &&
                            (!SkirmishNativeCheckpointParts.TryReadComponent(actor.Parts,out SkirmishUnitRoleComponent unitRole) ||
                            unitRole.Role!=row.UnitRole || unitRole.Category!=row.Category))return false;
                    }
                }
                if (!MatchesOriginalPlan(setup, rows)) return false;
                long elapsed;
                if (value.Kind == SkirmishObjectiveKind.FrontlineControl)
                {
                    var state = Parse<SkirmishFrontlineState>(value.Reducer);
                    if (state.Terminal || state.Reason != SkirmishEndReasonKind.None || state.PlayerTickets<=0 || state.EnemyTickets<=0 ||
                        state.ElapsedTicks>=state.DeadlineTicks) return false;
                    var baseline = SkirmishFrontlineReducer.Create(setup.SizeId, state.ZoneA.StableZoneId, state.ZoneB.StableZoneId, state.ZoneC.StableZoneId);
                    if (state.DeadlineTicks != baseline.DeadlineTicks) return false;
                    SkirmishFrontlineReducer.Step(ref state, new SkirmishFrontlineTickFacts { Paused = true });
                    foreach (var row in rows.Values) if (row.Disposition != SkirmishOriginalObjectiveDisposition.Live || row.IsStructure != 1) return false;
                    elapsed = state.ElapsedTicks;
                }
                else if (value.Kind == SkirmishObjectiveKind.Breakthrough)
                {
                    var state = Parse<SkirmishBreakthroughState>(value.Reducer);
                    if (state.Terminal || state.Reason != SkirmishEndReasonKind.None || state.OriginalSubjects == null ||
                        state.ElapsedTicks>=state.DeadlineTicks) return false;
                    int alive=0,escaped=0;
                    var ids = new string[state.OriginalSubjects.Length]; var facts = new SkirmishBreakthroughSubjectFacts[ids.Length];
                    for (int i=0;i<ids.Length;i++)
                    {
                        var subject = state.OriginalSubjects[i]; ids[i] = subject.StableOriginalId;
                        if (!rows.TryGetValue(ids[i], out var row) || (byte)row.Disposition != (byte)subject.Status) return false;
                        if(subject.Status==SkirmishBreakthroughSubjectStatus.Alive)alive++;
                        if(subject.Status==SkirmishBreakthroughSubjectStatus.Evacuated)escaped++;
                        facts[i] = new SkirmishBreakthroughSubjectFacts { StableOriginalId = ids[i], AuthoritativeLifeKnown = true };
                    }
                    var baseline = SkirmishBreakthroughReducer.Create(setup.SizeId, state.CorridorA.StableCorridorId, state.CorridorB.StableCorridorId, ids);
                    if (state.DeadlineTicks != baseline.DeadlineTicks) return false;
                    if(escaped>=8 || alive+escaped<8)return false;
                    SkirmishBreakthroughReducer.Step(ref state, new SkirmishBreakthroughTickFacts { Paused = true, OriginalSubjects = facts }); elapsed = state.ElapsedTicks;
                }
                else if (value.Kind == SkirmishObjectiveKind.ConvoyEscort)
                {
                    var state = Parse<SkirmishConvoyState>(value.Reducer);
                    if (state.Terminal || state.Reason != SkirmishEndReasonKind.None || state.OriginalTrucks == null ||
                        state.ElapsedTicks>=state.DeadlineTicks) return false;
                    int alive=0,delivered=0;
                    var ids = new string[state.OriginalTrucks.Length]; var facts = new SkirmishConvoySubjectFacts[ids.Length];
                    for (int i=0;i<ids.Length;i++)
                    {
                        var subject = state.OriginalTrucks[i]; ids[i] = subject.StableOriginalId;
                        if (!rows.TryGetValue(ids[i], out var row) ||
                            row.Disposition != (subject.Status == SkirmishConvoySubjectStatus.Delivered ? SkirmishOriginalObjectiveDisposition.Delivered : (SkirmishOriginalObjectiveDisposition)subject.Status)) return false;
                        if(subject.Status==SkirmishConvoySubjectStatus.Alive)alive++;
                        if(subject.Status==SkirmishConvoySubjectStatus.Delivered)delivered++;
                        facts[i] = new SkirmishConvoySubjectFacts { StableOriginalId = ids[i], AuthoritativeLifeKnown = true };
                    }
                    var baseline = SkirmishConvoyReducer.Create(setup.SizeId, state.StableDestinationId, ids);
                    if (state.DeadlineTicks != baseline.DeadlineTicks) return false;
                    if(delivered>=2 || alive+delivered<2)return false;
                    SkirmishConvoyReducer.Step(ref state, new SkirmishConvoyTickFacts { Paused = true, OriginalTrucks = facts }); elapsed = state.ElapsedTicks;
                }
                else return false;
                // Cursor and reducer advance atomically in the live collector owner.
                double ticks = value.Clock.LastAuthoritativeSeconds * TimeSpan.TicksPerSecond;
                if (elapsed < 0 || Math.Abs(ticks - elapsed - value.Clock.FractionalTicks) > 2) return false;
                error = null; return true;
            }
            catch (Exception e) when (e is ArgumentException || e is OverflowException) { return false; }
        }

        // The native reconstruction owner calls this AFTER saved Parts restore,
        // BEFORE unsuppression. The binding comes from current authored compilation,
        // not loaded geometry/digests. Caller also validates the immutable spawn plan.
        internal static bool TryRebind(EntityManager em, Entity session, Payload value,
            SkirmishCompiledObjectiveAreas freshAreas, string freshViewFingerprint,
            out SkirmishNativeObjectiveRecord record, out string error)
        {
            record = null; error = "Fresh authored objective binding differs.";
            if (freshAreas == null || value == null || freshAreas.LogicalFingerprint != value.LogicalFingerprint ||
                freshAreas.GeometryFingerprint != value.GeometryFingerprint || freshViewFingerprint != value.MissionViewProfileFingerprint ||
                !SkirmishNativeObjectiveCollector.TryRebindCheckpoint(em, session, freshAreas, value.LogicalFingerprint, value.Originals, out var collector, out error)) return false;
            var staged = new SkirmishNativeObjectiveRecord { Collector = collector, Clock = value.Clock, Areas = freshAreas, MissionViewProfileFingerprint = freshViewFingerprint };
            string Id(SkirmishObjectiveAreaRole role) => freshAreas.TryRead(role,out var area)?area.StableAreaId:null;
            if (value.Kind == SkirmishObjectiveKind.FrontlineControl)
            {
                staged.Frontline = Parse<SkirmishFrontlineState>(value.Reducer);
                if(staged.Frontline.ZoneA.StableZoneId!=Id(SkirmishObjectiveAreaRole.FrontlineA)||
                    staged.Frontline.ZoneB.StableZoneId!=Id(SkirmishObjectiveAreaRole.FrontlineB)||
                    staged.Frontline.ZoneC.StableZoneId!=Id(SkirmishObjectiveAreaRole.FrontlineC))return false;
            }
            else if (value.Kind == SkirmishObjectiveKind.Breakthrough)
            {
                staged.Breakthrough = Parse<SkirmishBreakthroughState>(value.Reducer);
                if(staged.Breakthrough.CorridorA.StableCorridorId!=Id(SkirmishObjectiveAreaRole.BreakthroughCorridorA)||
                    staged.Breakthrough.CorridorB.StableCorridorId!=Id(SkirmishObjectiveAreaRole.BreakthroughCorridorB))return false;
            }
            else if (value.Kind == SkirmishObjectiveKind.ConvoyEscort)
            {
                staged.Convoy = Parse<SkirmishConvoyState>(value.Reducer);
                if(staged.Convoy.StableDestinationId!=Id(SkirmishObjectiveAreaRole.ConvoyDestination))return false;
            }
            else return false;
            record = staged; error = null; return true;
        }
        private static T Parse<T>(string json)
        { var value = JsonUtility.FromJson<T>(json); if (JsonUtility.ToJson(value) != json) throw new ArgumentException("Noncanonical objective reducer state."); return value; }
        private static bool Hex(string value)
        { if (value == null || value.Length != 64) return false; foreach(char c in value) if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')) return false; return true; }

        private static bool MatchesOriginalPlan(SkirmishResolvedSetup setup,
            Dictionary<string,SkirmishObjectiveCollectorOriginalRow> rows)
        {
            if (setup.ObjectiveKind == SkirmishObjectiveKind.FrontlineControl)
            {
                if (setup.Structures == null) return false;
                int count=0;var factions=new HashSet<byte>();
                foreach(var structure in setup.Structures)
                {
                    if (!structure.DesignatedBase) continue;
                    string id=structure.StructureId+"."+structure.FactionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (!rows.TryGetValue(id,out var row) || !factions.Add(structure.FactionId) ||
                        row.ObjectiveStableId!=structure.ObjectiveRoleId || row.Faction!=structure.FactionId || row.IsStructure!=1 ||
                        row.Role!=(structure.FactionId==1?SkirmishObjectiveRoleKind.PlayerBase:SkirmishObjectiveRoleKind.EnemyBase)) return false;
                    count++;
                }
                return count==2 && factions.Contains(1) && factions.Contains(2);
            }
            if (!SkirmishOriginalObjectiveSpawnHooks.TryPlan(setup,out var plan,out _) || plan.Length!=rows.Count) return false;
            foreach(var expected in plan)
            {
                if (!rows.TryGetValue(expected.StableActorId,out var row) || row.ObjectiveStableId!=expected.StableActorId ||
                    row.Role!=expected.ObjectiveRole || row.Faction!=1 || row.IsStructure!=0) return false;
                bool bt=setup.ObjectiveKind==SkirmishObjectiveKind.Breakthrough;
                if (row.UnitRole!=(bt?SkirmishRoleKind.Rifle:SkirmishRoleKind.ConvoyLogic) ||
                    row.Category!=(bt?SkirmishRoleIds.Category(SkirmishRoleKind.Rifle):SkirmishPopulationCategory.ObjectiveSupport) ||
                    row.Disposition==(bt?SkirmishOriginalObjectiveDisposition.Delivered:SkirmishOriginalObjectiveDisposition.Evacuated)) return false;
            }
            return true;
        }
    }
}
