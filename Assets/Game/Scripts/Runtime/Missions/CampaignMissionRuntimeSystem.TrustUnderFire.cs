using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private static readonly FixedString64Bytes TrustId = CampaignMissionSequence.TrustUnderFire;
        private bool TryAdvanceTrustUnderFire(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime)
        {
            if (!runtime.MissionId.Equals(TrustId)) return false;
            if (runtime.Outcome != MissionOutcomeKind.None) return true;
            var em = system.EntityManager; var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            if (runtime.Phase == MissionPhaseKind.Preparing && (runtime.ReadyReadiness & runtime.RequiredReadiness) == runtime.RequiredReadiness)
            { if (TryTransition(in runtime, MissionPhaseKind.InteractiveBrief, MissionOutcomeKind.None, MissionReturnDestinationKind.None, out var brief)) em.SetComponentData(root, brief); return true; }
            if (runtime.Phase == MissionPhaseKind.InteractiveBrief && facts.InteractiveBriefCompleted != 0)
            { if (TryTransition(in runtime, MissionPhaseKind.FindSquad, MissionOutcomeKind.None, MissionReturnDestinationKind.None, out var launch)) em.SetComponentData(root, launch); return true; }
            if (facts.CommandSquadSpawned == 0 || !SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) || !metadata.Blob.IsCreated) return true;
            if (!em.HasComponent<CampaignMissionTrustUnderFireState>(root) || em.GetComponentData<CampaignMissionTrustUnderFireState>(root).Initialized == 0)
                InitializeTrustUnderFire(em, root, in runtime, ref metadata.Blob.Value);
            var mission = em.GetComponentData<CampaignMissionTrustUnderFireState>(root);
            if (!CampaignMissionTrustUnderFireRuleUtility.Matches(in mission, in runtime)) return true;
            ProjectTrustBuildings(em, root, ref metadata.Blob.Value, ref mission);
            var scope = new SupportFuelScopeComponent { Storage = mission.Reserve, Required = 1 };
            if (em.HasComponent<SupportFuelScopeComponent>(root)) em.SetComponentData(root, scope); else em.AddComponentData(root, scope);
            bool opening = em.HasComponent<CampaignMissionOpeningPresentationComponent>(root) &&
                em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).SessionToken.Equals(runtime.SessionToken) &&
                em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage >= 6;
            bool active = runtime.Phase == MissionPhaseKind.Engage && opening && mission.Ready != 0;
            ApplyTrustActivationPolicy(em, root, active);
            int delta = active ? (int)math.round(SystemAPI.Time.DeltaTime * 1000f) : 0;
            if (active) mission.ElapsedMilliseconds = SaturatingAddMilliseconds(mission.ElapsedMilliseconds, SystemAPI.Time.DeltaTime);
            var members = em.GetBuffer<CampaignMissionTrustUnderFireMember>(root);
            int initialized = 0, hostiles = 0, defeated = 0, north = 0, northDead = 0, south = 0, southDead = 0, relay = 0, relayDead = 0, staff = 0, staffDead = 0, armedAlive = 0, losses = 0;
            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i]; bool exists = em.Exists(member.Entity) && em.HasComponent<UnitHealth>(member.Entity);
                if (!exists && member.Dead == 0) { MarkTrustIntegrity(ref mission, $"original disappeared index={i} entity={member.Entity} kind={member.Kind}"); continue; }
                var health = exists ? em.GetComponentData<UnitHealth>(member.Entity) : default;
                if (health.Max > 0) member.HealthInitialized = 1;
                if (member.HealthInitialized != 0 && health.Current <= 0) member.Dead = 1;
                initialized += member.HealthInitialized; members[i] = member;
                bool hostile = member.Kind is TrustUnderFireMemberKind.NorthHostile or TrustUnderFireMemberKind.SouthHostile or TrustUnderFireMemberKind.RelayHostile;
                if (hostile)
                {
                    hostiles++; defeated += member.Dead;
                    if (member.Kind == TrustUnderFireMemberKind.NorthHostile) { north++; northDead += member.Dead; }
                    if (member.Kind == TrustUnderFireMemberKind.SouthHostile) { south++; southDead += member.Dead; }
                    if (member.Kind == TrustUnderFireMemberKind.RelayHostile) { relay++; relayDead += member.Dead; }
                    if (exists && em.HasComponent<SupportTargetEligibilityComponent>(member.Entity))
                    { var known = em.GetComponentData<SupportTargetEligibilityComponent>(member.Entity); byte visible = (byte)(active && member.Dead == 0 && !em.HasComponent<CampaignMissionCombatSuppressedTag>(member.Entity) ? 1 : 0);
                      if (known.CurrentlyVisible != visible || known.HostileConfirmed != visible) { known.CurrentlyVisible = known.HostileConfirmed = visible; known.SourceVersion++; em.SetComponentData(member.Entity, known); } }
                }
                else if (member.Kind is TrustUnderFireMemberKind.NorthStaff or TrustUnderFireMemberKind.SouthStaff) { staff++; staffDead += member.Dead; }
                else
                {
                    losses += member.Dead;
                    if (member.Dead != 0)
                    { if (member.Kind == TrustUnderFireMemberKind.NorthConvoy) mission.Failure = TrustUnderFireFailure.NorthConvoyLost;
                      if (member.Kind == TrustUnderFireMemberKind.SouthConvoy) mission.Failure = TrustUnderFireFailure.SouthConvoyLost;
                      if (member.Kind == TrustUnderFireMemberKind.Engineer) mission.Failure = TrustUnderFireFailure.EngineerLost; }
                    if (member.Dead == 0 && member.Kind is TrustUnderFireMemberKind.NorthEscort or TrustUnderFireMemberKind.SouthEscort) armedAlive++;
                }
            }
            if (members.Length != 26 || hostiles != 9 || north != 3 || south != 3 || relay != 3 || staff != 4) MarkTrustIntegrity(ref mission, $"roster={members.Length} military={hostiles} north={north} south={south} relay={relay} staff={staff}");
            if (staffDead > 0) mission.Failure = TrustUnderFireFailure.StaffLost;
            if (CampaignMissionTrustUnderFireRuleUtility.ShouldFailEscortLoss(initialized, armedAlive, hostiles, defeated)) mission.Failure = TrustUnderFireFailure.EscortLost;
            if (mission.ElapsedMilliseconds >= CampaignMissionTrustUnderFireRuleUtility.DeadlineMilliseconds) mission.Failure = TrustUnderFireFailure.Deadline;
            mission.NorthCleared = (byte)(north == 3 && northDead == 3 ? 1 : 0); mission.SouthCleared = (byte)(south == 3 && southDead == 3 ? 1 : 0);
            mission.RelayCleared = (byte)(relay == 3 && relayDead == 3 ? 1 : 0); mission.MilitaryCleared = (byte)(hostiles == 9 && defeated == 9 ? 1 : 0);
            if (active)
            {
                mission.NorthPassagePhase = ObserveTrustPassage(em, mission.NorthConvoy, mission.NorthPassagePhase, mission.NorthCrossing);
                mission.SouthPassagePhase = ObserveTrustPassage(em, mission.SouthConvoy, mission.SouthPassagePhase, mission.SouthCrossing);
                mission.NorthCrossed = (byte)(mission.NorthPassagePhase == 3 ? 1 : 0);
                mission.SouthCrossed = (byte)(mission.SouthPassagePhase == 3 ? 1 : 0);
            }
            mission.NorthHoldMilliseconds = CampaignMissionTrustUnderFireRuleUtility.AdvanceHold(mission.NorthHoldMilliseconds, delta,
                active && mission.NorthCleared != 0 && mission.NorthCrossed != 0 && CitywideStoppedAt(em, mission.NorthConvoy, mission.NorthArrival, 12) && TrustLive(em, mission.NorthShelter));
            mission.SouthHoldMilliseconds = CampaignMissionTrustUnderFireRuleUtility.AdvanceHold(mission.SouthHoldMilliseconds, delta,
                active && mission.SouthCleared != 0 && mission.SouthCrossed != 0 && CitywideStoppedAt(em, mission.SouthConvoy, mission.SouthArrival, 12) && TrustLive(em, mission.SouthShelter));
            mission.NorthArrived = (byte)(mission.NorthHoldMilliseconds >= 6000 ? 1 : 0); mission.SouthArrived = (byte)(mission.SouthHoldMilliseconds >= 6000 ? 1 : 0);
            if (active && TrustNear(em, ResolveTrustEscort(em, root, TrustUnderFireMemberKind.NorthEscort), mission.RelayApproach, 14)) mission.RelayApproached = 1;
            mission.VerificationMilliseconds = CampaignMissionTrustUnderFireRuleUtility.AdvanceHold(mission.VerificationMilliseconds, delta,
                active && mission.RelayCleared != 0 && CitywideStoppedAt(em, mission.Engineer, mission.RelayGate, 6) && TrustLive(em, mission.Broadcast));
            mission.RelayVerified = (byte)(mission.VerificationMilliseconds >= 6000 ? 1 : 0);
            if (mission.NorthArrived != 0 || mission.SouthArrived != 0) mission.CommsReady = 1;
            bool stable = active && mission.NorthArrived != 0 && mission.SouthArrived != 0 && mission.RelayVerified != 0 && mission.MilitaryCleared != 0 && mission.Failure == TrustUnderFireFailure.None;
            mission.StableMilliseconds = CampaignMissionTrustUnderFireRuleUtility.AdvanceHold(mission.StableMilliseconds, delta, stable);
            if (initialized == 26 && mission.NorthShelter != Entity.Null && mission.SouthShelter != Entity.Null && mission.Broadcast != Entity.Null && mission.Reserve != Entity.Null) mission.Ready = 1;
            facts.ElapsedMilliseconds = mission.ElapsedMilliseconds; facts.HostileTotalCount = hostiles; facts.HostileDefeatedCount = defeated;
            facts.CivilianTotalCount = staff; facts.CivilianLossCount = staffDead; facts.SquadLossCount = losses; facts.CommandSquadAlive = (byte)(TrustLive(em, mission.Engineer) ? 1 : 0);
            facts.TrustNorthCrossed = mission.NorthCrossed; facts.TrustSouthCrossed = mission.SouthCrossed;
            facts.TrustNorthArrived = mission.NorthArrived; facts.TrustSouthArrived = mission.SouthArrived; facts.TrustRelayVerified = mission.RelayVerified;
            facts.TrustMilitaryCleared = mission.MilitaryCleared; facts.TrustCommsReady = mission.CommsReady; facts.TrustStableMilliseconds = mission.StableMilliseconds; facts.TrustFailure = mission.Failure;
            if (mission.Failure == TrustUnderFireFailure.Integrity) facts.HostileRosterIntegrityFault = 1;
            if (em.HasBuffer<CampaignMissionDefenseMember>(root))
            { var defense = em.GetBuffer<CampaignMissionDefenseMember>(root); for (int i = 0; i < defense.Length; i++) { var member = defense[i]; foreach (var original in members) if (original.Entity == member.Entity) { member.HealthInitialized = original.HealthInitialized; member.Defeated = original.Dead; break; } defense[i] = member; } }
            em.SetComponentData(root, mission); em.SetComponentData(root, facts);
            if (CampaignMissionTrustUnderFireRuleUtility.TryAdvance(in runtime, in facts, in mission, opening, out var next)) em.SetComponentData(root, next);
            return true;
        }
        internal static Entity ResolveTrustEscort(EntityManager em, Entity root, TrustUnderFireMemberKind preferred)
        {
            if (!em.HasBuffer<CampaignMissionTrustUnderFireMember>(root)) return Entity.Null;
            var roster = em.GetBuffer<CampaignMissionTrustUnderFireMember>(root, true);
            var alternate = preferred == TrustUnderFireMemberKind.NorthEscort ? TrustUnderFireMemberKind.SouthEscort : TrustUnderFireMemberKind.NorthEscort;
            // Prefer the surviving tank's range to an exposed short-range actor.
            Entity fallback = Entity.Null;
            for (int pass = 0; pass < 2; pass++)
            { float bestRange = 0; Entity best = Entity.Null;
              foreach (var member in roster)
              { if (member.Kind != (pass == 0 ? preferred : alternate) || member.Dead != 0 || !TrustLive(em, member.Entity) || !em.HasComponent<UnitCombat>(member.Entity) || !em.HasComponent<UnitAttack>(member.Entity) || !em.HasComponent<LocalTransform>(member.Entity)) continue;
                var weapon = em.GetComponentData<UnitAttack>(member.Entity);
                if (em.GetComponentData<UnitCombat>(member.Entity).CanAttack != 0 && weapon.Damage > 0 && weapon.Range > bestRange) { bestRange = weapon.Range; best = member.Entity; } }
              if (best != Entity.Null) return best;
            }
            return fallback;
        }
        private static bool TrustLive(EntityManager em, Entity entity) => em.Exists(entity) && em.HasComponent<UnitHealth>(entity) && em.GetComponentData<UnitHealth>(entity).Max > 0 && em.GetComponentData<UnitHealth>(entity).Current > 0;
        private static byte ObserveTrustPassage(EntityManager em, Entity convoy, byte phase, float3 crossing) =>
            TrustLive(em, convoy) && em.HasComponent<LocalTransform>(convoy) && !em.HasComponent<UnitTransportPassenger>(convoy)
                ? CampaignMissionTrustUnderFireRuleUtility.AdvancePassage(phase, em.GetComponentData<LocalTransform>(convoy).Position, crossing) : (byte)0;
        private static bool TrustNear(EntityManager em, Entity entity, float3 point, float radius) => TrustLive(em, entity) && em.HasComponent<LocalTransform>(entity) && !em.HasComponent<UnitTransportPassenger>(entity) && math.distancesq(em.GetComponentData<LocalTransform>(entity).Position.xz, point.xz) <= radius * radius;
        private static void MarkTrustIntegrity(ref CampaignMissionTrustUnderFireState mission, string detail)
        { if (mission.Failure != TrustUnderFireFailure.Integrity) UnityEngine.Debug.LogError($"[TrustIntegrity] session={mission.SessionToken} attempt={mission.AttemptOrdinal} source={mission.SourceVersion} {detail}"); mission.Failure = TrustUnderFireFailure.Integrity; }
        internal static void InitializeTrustUnderFire(EntityManager em, Entity root, in CampaignMissionRuntimeComponent runtime, ref OperationMapBlob map)
        {
            var mission = new CampaignMissionTrustUnderFireState { SessionToken = runtime.SessionToken, AttemptOrdinal = runtime.AttemptOrdinal, SourceVersion = runtime.SourceVersion, Initialized = 1 };
            string[] suffixes = { "north_crossing", "south_crossing", "north_arrival", "south_arrival", "relay_gate", "relay_approach" };
            for (int i = 0; i < suffixes.Length; i++)
            { if (!CampaignMissionSpawnSystem.TryFindAnchor(ref map, new FixedString64Bytes("anchor.ch05.m02." + suffixes[i]), out var a)) MarkTrustIntegrity(ref mission, "missing anchor " + suffixes[i]);
              if (i == 0) mission.NorthCrossing = a.Position; if (i == 1) mission.SouthCrossing = a.Position; if (i == 2) mission.NorthArrival = a.Position; if (i == 3) mission.SouthArrival = a.Position; if (i == 4) mission.RelayGate = a.Position; if (i == 5) mission.RelayApproach = a.Position; }
            if (!em.HasBuffer<CampaignMissionTrustUnderFireMember>(root)) em.AddBuffer<CampaignMissionTrustUnderFireMember>(root);
            var roster = em.GetBuffer<CampaignMissionTrustUnderFireMember>(root); roster.Clear();
            using var query = em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent), typeof(UnitHealth), typeof(LocalTransform)); using var entities = query.ToEntityArray(Allocator.Temp);
            using var commands = new EntityCommandBuffer(Allocator.Temp);
            foreach (var entity in entities)
            {
                var role = em.GetComponentData<CampaignMissionUnitRoleComponent>(entity); if (!role.SessionToken.Equals(runtime.SessionToken)) continue;
                if (!TryTrustKind(in role, out var kind)) { MarkTrustIntegrity(ref mission, "unknown role " + role.MissionRoleId); continue; }
                roster.Add(new CampaignMissionTrustUnderFireMember { Entity = entity, Kind = kind });
                if (kind == TrustUnderFireMemberKind.NorthConvoy) mission.NorthConvoy = entity;
                if (kind == TrustUnderFireMemberKind.SouthConvoy) mission.SouthConvoy = entity;
                if (kind == TrustUnderFireMemberKind.Engineer) mission.Engineer = entity;
                bool noncombat = kind is TrustUnderFireMemberKind.NorthConvoy or TrustUnderFireMemberKind.SouthConvoy or TrustUnderFireMemberKind.Engineer or TrustUnderFireMemberKind.NorthStaff or TrustUnderFireMemberKind.SouthStaff;
                if (noncombat) QualifyCitywideEngineer(em, entity);
                if (kind is TrustUnderFireMemberKind.NorthStaff or TrustUnderFireMemberKind.SouthStaff or TrustUnderFireMemberKind.NorthHostile or TrustUnderFireMemberKind.SouthHostile or TrustUnderFireMemberKind.RelayHostile)
                    if (!em.HasComponent<CampaignMissionStationaryUnitTag>(entity)) commands.AddComponent<CampaignMissionStationaryUnitTag>(entity);
                if (em.HasComponent<UnitMovementBehavior>(entity)) { var movement = em.GetComponentData<UnitMovementBehavior>(entity); movement.AllowIdleWander = 0; em.SetComponentData(entity, movement); }
                if (kind is TrustUnderFireMemberKind.NorthHostile or TrustUnderFireMemberKind.SouthHostile or TrustUnderFireMemberKind.RelayHostile && em.HasComponent<UnitFuelConsumption>(entity))
                { var fuel = em.GetComponentData<UnitFuelConsumption>(entity); fuel.Enabled = 0; em.SetComponentData(entity, fuel); }
            }
            int count = roster.Length; commands.Playback(em);
            if (count != 26 || mission.NorthConvoy == Entity.Null || mission.SouthConvoy == Entity.Null || mission.Engineer == Entity.Null) MarkTrustIntegrity(ref mission, $"initialized roster={count} convoys={mission.NorthConvoy}/{mission.SouthConvoy} engineer={mission.Engineer}");
            if (!em.HasBuffer<CampaignMissionTrustBuildingRequest>(root)) em.AddBuffer<CampaignMissionTrustBuildingRequest>(root);
            var bindings = em.GetBuffer<CampaignMissionTrustBuildingRequest>(root); bindings.Clear(); for (int i = 0; i < 4; i++) bindings.Add(default);
            if (em.HasComponent<CampaignMissionTrustUnderFireState>(root)) em.SetComponentData(root, mission); else em.AddComponentData(root, mission);
        }
        private static bool TryTrustKind(in CampaignMissionUnitRoleComponent role, out TrustUnderFireMemberKind kind)
        {
            kind = role.MissionRoleId.ToString() switch {
                "role.friendly.escort.north" => TrustUnderFireMemberKind.NorthEscort, "role.friendly.escort.south" => TrustUnderFireMemberKind.SouthEscort,
                "role.friendly.convoy.north" => TrustUnderFireMemberKind.NorthConvoy, "role.friendly.convoy.south" => TrustUnderFireMemberKind.SouthConvoy,
                "role.friendly.engineer" => TrustUnderFireMemberKind.Engineer,
                "role.hostile.north" => TrustUnderFireMemberKind.NorthHostile, "role.hostile.south" => TrustUnderFireMemberKind.SouthHostile, "role.hostile.relay" => TrustUnderFireMemberKind.RelayHostile,
                "role.civilian.protected" => role.UnitGroupId.ToString().EndsWith("staff.north") ? TrustUnderFireMemberKind.NorthStaff : role.UnitGroupId.ToString().EndsWith("staff.south") ? TrustUnderFireMemberKind.SouthStaff : (TrustUnderFireMemberKind)255,
                _ => (TrustUnderFireMemberKind)255 }; return (byte)kind != 255;
        }
        private static void ApplyTrustActivationPolicy(EntityManager em, Entity root, bool active)
        {
            foreach (var member in em.GetBuffer<CampaignMissionTrustUnderFireMember>(root, true))
            {
                if (member.Dead != 0 || !em.Exists(member.Entity) || !em.HasComponent<ArmorBreakUnitActivationState>(member.Entity)) continue;
                var original = em.GetComponentData<ArmorBreakUnitActivationState>(member.Entity);
                if (em.HasComponent<UnitFuelConsumption>(member.Entity)) { var fuel = em.GetComponentData<UnitFuelConsumption>(member.Entity); byte enabled = active ? original.OriginalFuelEnabled : (byte)0; if (enabled != fuel.Enabled) { fuel.Enabled = enabled; em.SetComponentData(member.Entity, fuel); if (enabled != 0 && em.HasComponent<UnitFuelConsumptionState>(member.Entity)) em.SetComponentData(member.Entity, default(UnitFuelConsumptionState)); } }
                if (em.HasComponent<UnitCombat>(member.Entity)) { var combat = em.GetComponentData<UnitCombat>(member.Entity); combat.AutoEngage = active ? original.OriginalAutoEngage : (byte)0; em.SetComponentData(member.Entity, combat); }
            }
        }
    }
}
