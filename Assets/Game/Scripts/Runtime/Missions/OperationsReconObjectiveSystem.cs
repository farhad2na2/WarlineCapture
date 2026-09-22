using Game.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    /// <summary>Recon rules read the shared combat world's entities; there is no shadow simulation.</summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct OperationsReconObjectiveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<OperationsReconMissionComponent>();
            state.RequireForUpdate<RuntimeGameplayStateComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) || gameplay.SimulationActive == 0)
                return;
            float dt = SystemAPI.Time.DeltaTime;
            if (!math.isfinite(dt) || dt <= 0f) return;
            var health = SystemAPI.GetComponentLookup<UnitHealth>(true);
            var positions = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var passengers = SystemAPI.GetComponentLookup<UnitTransportPassenger>(true);
            // Requests use random-access lookups before the later hostile query can
            // synchronize transforms. Finish shared movement/combat writers first.
            state.Dependency.Complete();
            bool hasSurface = SystemAPI.TryGetSingleton(out MapSurfaceComponent surface) && surface.SurfaceBlob.IsCreated;

            foreach (var (missionRef, evidenceRef, siteBuffer, roster, requests) in
                     SystemAPI.Query<RefRW<OperationsReconMissionComponent>, RefRW<OperationsReconEvidenceComponent>,
                         DynamicBuffer<OperationsReconSiteElement>, DynamicBuffer<OperationsReconRosterElement>,
                         DynamicBuffer<OperationsReconActionElement>>())
            {
                var sites = siteBuffer;
                ref var mission = ref missionRef.ValueRW;
                ref var evidence = ref evidenceRef.ValueRW;
                if (mission.Phase != OperationsReconPhase.Playing)
                {
                    requests.Clear();
                    continue;
                }
                float missionDelta = math.min(dt, math.max(0f, mission.DeadlineSeconds - mission.ElapsedSeconds));
                mission.ElapsedSeconds += missionDelta;
                bool conclude = false, withdraw = false;
                foreach (var request in requests)
                {
                    if (!request.SessionId.Equals(mission.SessionId)) continue;
                    if (request.Action == OperationsReconAction.Conclude) { conclude = true; continue; }
                    if (request.Action == OperationsReconAction.Withdraw) { withdraw = true; continue; }
                    if (!IsOriginal(roster, request.Actor) || !IsDismounted(request.Actor, health, positions, passengers)) continue;
                    if (request.Action == OperationsReconAction.CancelInteraction)
                    {
                        Cancel(request.Actor, sites, ref evidence);
                        continue;
                    }
                    if (request.Action == OperationsReconAction.Scan && request.SiteIndex >= 0 && request.SiteIndex < sites.Length)
                    {
                        var site = sites[request.SiteIndex];
                        if (site.Completed != 0 || !hasSurface ||
                            !CanReach(positions[request.Actor].Position, site.Position, site.Radius, ref surface)) continue;
                        Cancel(request.Actor, sites, ref evidence);
                        site.Actor = request.Actor;
                        site.ChannelSeconds = 0f;
                        sites[request.SiteIndex] = site;
                    }
                    if (request.Action == OperationsReconAction.RecoverEvidence && mission.CompletedScans == sites.Length &&
                        evidence.Carrier == Entity.Null && hasSurface &&
                        CanReach(positions[request.Actor].Position, evidence.Position, 6f, ref surface))
                    {
                        Cancel(request.Actor, sites, ref evidence);
                        evidence.Actor = request.Actor;
                        evidence.ChannelSeconds = 0f;
                    }
                }
                requests.Clear();

                mission.SurvivingInfantry = 0;
                mission.InfantryAtExit = 0;
                bool exitSafe = true, evidenceSafe = true;
                foreach (var (hostileHealth, hostilePosition, faction) in
                         SystemAPI.Query<RefRO<UnitHealth>, RefRO<LocalTransform>, RefRO<Faction>>())
                {
                    if (faction.ValueRO.Id <= 1 || hostileHealth.ValueRO.Current <= 0) continue;
                    if (math.distancesq(hostilePosition.ValueRO.Position.xz, mission.ExitPosition.xz) <= 144f) exitSafe = false;
                    if (math.distancesq(hostilePosition.ValueRO.Position.xz, evidence.Position.xz) <= 144f) evidenceSafe = false;
                }
                foreach (var member in roster)
                {
                    if (!health.HasComponent(member.Unit) || health[member.Unit].Current <= 0)
                    {
                        if (member.Recon != 0 && mission.CompletedScans < sites.Length) mission.ReconLostBeforeScansComplete = 1;
                        continue;
                    }
                    mission.SurvivingInfantry++;
                    if (exitSafe && IsDismounted(member.Unit, health, positions, passengers) &&
                        math.distancesq(positions[member.Unit].Position.xz, mission.ExitPosition.xz) <= mission.ExitRadius * mission.ExitRadius)
                        mission.InfantryAtExit++;
                }
                // Cache the carrier's last location while alive so deletion in combat leaves
                // the evidence recoverable at a real world position.
                if (evidence.Carrier != Entity.Null)
                {
                    if (positions.HasComponent(evidence.Carrier)) evidence.Position = positions[evidence.Carrier].Position;
                    if (!health.HasComponent(evidence.Carrier) || health[evidence.Carrier].Current <= 0)
                    {
                        evidence.Carrier = Entity.Null;
                        evidence.Actor = Entity.Null;
                        evidence.ChannelSeconds = 0f;
                    }
                }

                for (int i = 0; i < sites.Length; i++)
                {
                    var site = sites[i];
                    if (site.Completed != 0 || site.Actor == Entity.Null) continue;
                    if (!hasSurface || !IsDismounted(site.Actor, health, positions, passengers) ||
                        !CanReach(positions[site.Actor].Position, site.Position, site.Radius, ref surface))
                    { site.Actor = Entity.Null; site.ChannelSeconds = 0f; }
                    else
                    {
                        site.ChannelSeconds += missionDelta;
                        if (site.ChannelSeconds >= mission.ScanSeconds)
                        {
                            site.Completed = 1;
                            site.Actor = Entity.Null;
                            mission.CompletedScans++;
                            if (mission.FirstScanWaveTriggered == 0) mission.FirstScanPosition = site.Position;
                            mission.FirstScanWaveTriggered = 1;
                        }
                    }
                    sites[i] = site;
                }
                if (mission.CompletedScans == sites.Length && mission.ReconLostBeforeScansComplete == 0)
                    mission.MasteryCompleted = 1;
                if (evidence.Actor != Entity.Null)
                {
                    if (!evidenceSafe || !hasSurface || !IsDismounted(evidence.Actor, health, positions, passengers) ||
                        !CanReach(positions[evidence.Actor].Position, evidence.Position, 6f, ref surface))
                    { evidence.Actor = Entity.Null; evidence.ChannelSeconds = 0f; }
                    else
                    {
                        evidence.ChannelSeconds += missionDelta;
                        if (evidence.ChannelSeconds >= mission.EvidenceSeconds)
                        {
                            evidence.Carrier = evidence.Actor;
                            evidence.Actor = Entity.Null;
                            evidence.Recovered = 1;
                            mission.EvidenceWaveTriggered = 1;
                        }
                    }
                }
                bool evidenceAtExit = evidence.Recovered != 0 && exitSafe &&
                    IsDismounted(evidence.Carrier, health, positions, passengers) &&
                    math.distancesq(positions[evidence.Carrier].Position.xz, mission.ExitPosition.xz) <= mission.ExitRadius * mission.ExitRadius;
                bool partial = mission.CompletedScans >= 2 && mission.InfantryAtExit >= 2;
                mission.PartialAvailable = partial ? (byte)1 : (byte)0;
                if (mission.SurvivingInfantry < 2) mission.Outcome = OperationsReconOutcome.Defeat;
                else if (mission.CompletedScans == sites.Length && mission.InfantryAtExit >= 2 && evidenceAtExit)
                    mission.Outcome = OperationsReconOutcome.Victory;
                else if (mission.ElapsedSeconds >= mission.DeadlineSeconds)
                    mission.Outcome = partial ? OperationsReconOutcome.Partial : OperationsReconOutcome.Defeat;
                else if (withdraw) mission.Outcome = OperationsReconOutcome.Withdraw;
                else if (conclude && partial) mission.Outcome = OperationsReconOutcome.Partial;
                if (mission.Outcome != OperationsReconOutcome.None) mission.Phase = OperationsReconPhase.Terminal;
            }
        }

        private static bool IsOriginal(DynamicBuffer<OperationsReconRosterElement> roster, Entity unit)
        {
            foreach (var member in roster) if (member.Unit == unit) return true;
            return false;
        }

        private static bool IsDismounted(Entity unit, ComponentLookup<UnitHealth> health,
            ComponentLookup<LocalTransform> positions, ComponentLookup<UnitTransportPassenger> passengers) =>
            unit != Entity.Null && health.HasComponent(unit) && health[unit].Current > 0 &&
            positions.HasComponent(unit) && (!passengers.HasComponent(unit) || passengers[unit].Transport == Entity.Null);

        private static void Cancel(Entity actor, DynamicBuffer<OperationsReconSiteElement> sites,
            ref OperationsReconEvidenceComponent evidence)
        {
            for (int i = 0; i < sites.Length; i++)
            {
                var site = sites[i];
                if (site.Actor != actor) continue;
                site.Actor = Entity.Null; site.ChannelSeconds = 0f; sites[i] = site;
            }
            if (evidence.Actor == actor) { evidence.Actor = Entity.Null; evidence.ChannelSeconds = 0f; }
        }

        private static bool CanReach(float3 from, float3 to, float radius, ref MapSurfaceComponent surface)
        {
            if (math.distancesq(from.xz, to.xz) > radius * radius || surface.CellSize <= 0f) return false;
            // O001 ground interactions cannot see through an impassable footprint. The
            // short ray is sampled at half-cell intervals, including both endpoints.
            int steps = math.max(1, (int)math.ceil(math.distance(from.xz, to.xz) / surface.CellSize * 2f));
            for (int i = 0; i <= steps; i++)
            {
                float3 point = math.lerp(from, to, (float)i / steps);
                int2 cell = (int2)math.floor((point.xz - surface.GridOrigin.xz) / surface.CellSize);
                if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value, cell, out var sample) ||
                    sample.SurfaceType == MapSurfaceType.Blocked) return false;
            }
            return true;
        }
    }
}
