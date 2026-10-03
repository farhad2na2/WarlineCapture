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
        private static readonly FixedString64Bytes CommandNodeId = CampaignMissionSequence.CommandNode;
        private bool TryAdvanceCommandNode(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime)
        {
            if (!runtime.MissionId.Equals(CommandNodeId)) return false;
            var em = system.EntityManager;
            var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            if (runtime.Outcome != MissionOutcomeKind.None) return true;
            if (runtime.Phase == MissionPhaseKind.Preparing) { if (TryTransition(in runtime, MissionPhaseKind.InteractiveBrief, MissionOutcomeKind.None, MissionReturnDestinationKind.None, out var next)) em.SetComponentData(root, next); return true; }
            if (runtime.Phase == MissionPhaseKind.InteractiveBrief && facts.InteractiveBriefCompleted != 0) { if (TryTransition(in runtime, MissionPhaseKind.FindSquad, MissionOutcomeKind.None, MissionReturnDestinationKind.None, out var next)) em.SetComponentData(root, next); return true; }
            if (facts.CommandSquadSpawned == 0 || !SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) || !metadata.Blob.IsCreated) return true;
            if (!em.HasComponent<CampaignMissionCommandNodeState>(root) || em.GetComponentData<CampaignMissionCommandNodeState>(root).Initialized == 0) InitializeCommandNode(em, root, in runtime, ref metadata.Blob.Value);
            var m = em.GetComponentData<CampaignMissionCommandNodeState>(root);
            if (!CampaignMissionCommandNodeRuleUtility.Matches(in m, in runtime)) return true;
            ProjectCommandBuildings(em, root, ref metadata.Blob.Value, ref m);
            var scope = new SupportFuelScopeComponent { Storage = m.Reserve, Required = 1 };
            if (em.HasComponent<SupportFuelScopeComponent>(root)) em.SetComponentData(root, scope); else em.AddComponentData(root, scope);
            bool opening = em.HasComponent<CampaignMissionOpeningPresentationComponent>(root) && em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).SessionToken.Equals(runtime.SessionToken) && em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage >= 6;
            bool active = runtime.Phase == MissionPhaseKind.Engage && opening && m.Ready != 0;
            ApplyCommandActivation(em, root, active);
            int delta = active ? (int)math.round(SystemAPI.Time.DeltaTime * 1000) : 0;
            if (active) m.ElapsedMilliseconds = SaturatingAddMilliseconds(m.ElapsedMilliseconds, SystemAPI.Time.DeltaTime);
            var members = em.GetBuffer<CampaignMissionCommandNodeMember>(root);
            int initialized = 0, hostiles = 0, defeated = 0, exterior = 0, exteriorDead = 0, staff = 0, staffDead = 0, escorts = 0, losses = 0, specialists = 0;
            bool auditTeam = true, safeTeam = true;
            using var commands = new EntityCommandBuffer(Allocator.Temp);
            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i]; bool exists = em.Exists(member.Entity) && em.HasComponent<UnitHealth>(member.Entity);
                if (!exists && member.Dead == 0) { MarkCommandIntegrity(ref m, "Original actor disappeared: " + member.Kind); continue; }
                var hp = exists ? em.GetComponentData<UnitHealth>(member.Entity) : default;
                if (hp.Max > 0) member.HealthInitialized = 1;
                if (member.HealthInitialized != 0 && hp.Current <= 0) member.Dead = 1;
                initialized += member.HealthInitialized; members[i] = member;
                bool military = member.Kind is CommandNodeMemberKind.Exterior or CommandNodeMemberKind.PerimeterNode or CommandNodeMemberKind.CoreGuard or CommandNodeMemberKind.Qassem;
                if (military) { hostiles++; defeated += member.Dead; }
                if (member.Kind == CommandNodeMemberKind.Exterior) { exterior++; exteriorDead += member.Dead; }
                if (member.Kind == CommandNodeMemberKind.CivicStaff) { staff++; staffDead += member.Dead; }
                if (member.Kind == CommandNodeMemberKind.Escort) { losses += member.Dead; if (member.Dead == 0) escorts++; }
                if (member.Kind == CommandNodeMemberKind.Engineer && member.Dead != 0) m.Failure = CommandNodeFailure.EngineerLost;
                if (member.Kind == CommandNodeMemberKind.Specialist)
                {
                    specialists++; if (member.Dead != 0) m.Failure = CommandNodeFailure.SpecialistsLost;
                    auditTeam &= member.Dead == 0 && CitywideStoppedAt(em, member.Entity, m.CoreAudit, 6);
                    safeTeam &= member.Dead == 0 && CitywideStoppedAt(em, member.Entity, m.SafeReceiving, 6);
                }
                if (member.Kind == CommandNodeMemberKind.PerimeterNode)
                {
                    if (member.Dead != 0) { m.NodeDisabled = 1; if (m.ClinicIsolated == 0 || m.UtilityIsolated == 0) m.Failure = CommandNodeFailure.UnisolatedNodeDestroyed; }
                    else SetCommandSuppression(em, commands, member.Entity, !active || m.ClinicIsolated == 0 || m.UtilityIsolated == 0);
                }
                if (member.Kind is CommandNodeMemberKind.CoreGuard or CommandNodeMemberKind.Qassem)
                {
                    if (member.Kind == CommandNodeMemberKind.Qassem && member.Dead != 0) m.QassemDefeated = 1;
                    if (member.Dead == 0) SetCommandSuppression(em, commands, member.Entity, !active || m.Breached == 0);
                }
            }
            int count = members.Length; commands.Playback(em);
            if (count != 23 || hostiles != 10 || exterior != 4 || specialists != 2 || staff != 4) MarkCommandIntegrity(ref m, "Closed original roster mismatch");
            if (staffDead != 0) m.Failure = CommandNodeFailure.StaffLost;
            if (initialized == 23 && escorts == 0 && defeated < hostiles) m.Failure = CommandNodeFailure.EscortLost;
            if (m.ElapsedMilliseconds >= CampaignMissionCommandNodeRuleUtility.DeadlineMilliseconds) m.Failure = CommandNodeFailure.Deadline;
            m.ExteriorCleared = (byte)(exteriorDead == 4 ? 1 : 0);
            m.MilitaryCleared = (byte)(hostiles == 10 && defeated == 10 ? 1 : 0);
            if (initialized == 23 && m.Clinic != Entity.Null && m.Utility != Entity.Null && m.Audit != Entity.Null && m.Reserve != Entity.Null) m.Ready = 1;
            if (active && m.ExteriorCleared != 0 && (m.ClinicIsolated == 0 || m.UtilityIsolated == 0))
            {
                var point = m.ClinicIsolated == 0 ? m.IsolationClinic : m.IsolationUtility;
                m.IsolationMilliseconds = CampaignMissionCommandNodeRuleUtility.AdvanceHold(m.IsolationMilliseconds, delta, CitywideStoppedAt(em, m.Engineer, point, 6));
                if (m.IsolationMilliseconds >= 6000) { if (m.ClinicIsolated == 0) m.ClinicIsolated = 1; else m.UtilityIsolated = 1; m.IsolationMilliseconds = 0; }
            }
            if (m.Breached == 0)
            {
                m.CoverReady = (byte)(CommandCoverReady(em, root, in m) ? 1 : 0);
                if (initialized == 23 && m.NodeDisabled != 0 && ResolveCommandCoverActor(em, root) == Entity.Null) m.Failure = CommandNodeFailure.EscortLost;
                m.BreachMilliseconds = CampaignMissionCommandNodeRuleUtility.AdvanceHold(m.BreachMilliseconds, delta, active && m.NodeDisabled != 0 && m.CoverReady != 0 && CitywideStoppedAt(em, m.Engineer, m.BreachGate, 6));
                if (m.BreachMilliseconds >= 6000) m.Breached = 1;
            }
            if (m.AuditPreserved == 0)
            {
                m.AuditMilliseconds = CampaignMissionCommandNodeRuleUtility.AdvanceHold(m.AuditMilliseconds, delta, active && m.MilitaryCleared != 0 && m.Breached != 0 && auditTeam && specialists == 2);
                if (m.AuditMilliseconds >= 6000) { m.AuditPreserved = 1; m.ReleaseOrderBaseline = CommandMoveRequestId(em); }
            }
            if (active && m.AuditPreserved != 0 && m.ReleaseOrdered == 0 && HasCommandReleaseOrder(em, in runtime, in m, in metadata.Blob.Value.Grid)) m.ReleaseOrdered = 1;
            if (m.AuditReleased == 0)
            {
                m.ReleaseMilliseconds = CampaignMissionCommandNodeRuleUtility.AdvanceHold(m.ReleaseMilliseconds, delta, active && CampaignMissionCommandNodeRuleUtility.CanRelease(in m) && CitywideStoppedAt(em, m.Engineer, m.AuditRelease, 6));
                if (m.ReleaseMilliseconds >= 6000) m.AuditReleased = 1;
            }
            if (m.SpecialistsSafe == 0)
            {
                m.ReceivingMilliseconds = CampaignMissionCommandNodeRuleUtility.AdvanceHold(m.ReceivingMilliseconds, delta, active && m.AuditReleased != 0 && safeTeam && specialists == 2);
                if (m.ReceivingMilliseconds >= 6000) m.SpecialistsSafe = 1;
            }
            if (m.AuditPreserved != 0) m.CommsReady = 1;
            facts.ElapsedMilliseconds = m.ElapsedMilliseconds; facts.HostileTotalCount = hostiles; facts.HostileDefeatedCount = defeated; facts.CivilianTotalCount = staff; facts.CivilianLossCount = staffDead; facts.SquadLossCount = losses; facts.CommandSquadAlive = (byte)(NetworkLive(em, m.Engineer) ? 1 : 0);
            facts.CommandNodeNetworkSeparated = (byte)(m.ClinicIsolated != 0 && m.UtilityIsolated != 0 && m.NodeDisabled != 0 ? 1 : 0);
            facts.CommandNodeAuditReleased = m.AuditReleased; facts.CommandNodeSpecialistsSafe = m.SpecialistsSafe; facts.CommandNodeCommsReady = m.CommsReady;
            facts.CommandNodeServicesIntact = (byte)(NetworkLive(em, m.Clinic) && NetworkLive(em, m.Utility) && NetworkLive(em, m.Audit) ? 1 : 0);
            facts.CommandNodeFailure = m.Failure;
            if (m.Failure == CommandNodeFailure.Integrity) facts.HostileRosterIntegrityFault = 1;
            em.SetComponentData(root, m); em.SetComponentData(root, facts);
            if (CampaignMissionCommandNodeRuleUtility.TryAdvance(in runtime, in facts, in m, opening, out var changed)) em.SetComponentData(root, changed);
            return true;
        }
        private static void MarkCommandIntegrity(ref CampaignMissionCommandNodeState state, string detail)
        { if (state.Failure != CommandNodeFailure.Integrity) UnityEngine.Debug.LogError("[CommandNodeIntegrity] " + detail); state.Failure = CommandNodeFailure.Integrity; }
        private static void SetCommandSuppression(EntityManager em, EntityCommandBuffer commands, Entity actor, bool suppressed)
        {
            if (suppressed && !em.HasComponent<CampaignMissionCombatSuppressedTag>(actor)) commands.AddComponent<CampaignMissionCombatSuppressedTag>(actor);
            if (suppressed && em.HasComponent<EngageTarget>(actor)) commands.RemoveComponent<EngageTarget>(actor);
            if (!suppressed && em.HasComponent<CampaignMissionCombatSuppressedTag>(actor)) commands.RemoveComponent<CampaignMissionCombatSuppressedTag>(actor);
            if (em.HasComponent<UnitCombat>(actor)) { var combat = em.GetComponentData<UnitCombat>(actor); combat.AutoEngage = (byte)(suppressed ? 0 : 1); em.SetComponentData(actor, combat); }
            if (em.HasComponent<SupportTargetEligibilityComponent>(actor)) { var eligibility = em.GetComponentData<SupportTargetEligibilityComponent>(actor); eligibility.CurrentlyVisible = eligibility.HostileConfirmed = (byte)(suppressed ? 0 : 1); em.SetComponentData(actor, eligibility); }
        }
        internal static int CommandMoveRequestId(EntityManager em)
        { using var q = em.CreateEntityQuery(typeof(UnitMoveOrderQueueComponent)); return q.CalculateEntityCount() == 1 ? em.GetComponentData<UnitMoveOrderQueueComponent>(q.GetSingletonEntity()).LastRequestId : 0; }
        internal static bool HasCommandReleaseOrder(EntityManager em, in CampaignMissionRuntimeComponent runtime, in CampaignMissionCommandNodeState mission, in OperationMapGridBlob grid)
        {
            if (!CampaignMissionCommandNodeRuleUtility.Matches(in mission, in runtime) || mission.AuditPreserved == 0 || !NetworkLive(em, mission.Engineer) || !em.HasComponent<CampaignMissionUnitRoleComponent>(mission.Engineer)) return false;
            var role = em.GetComponentData<CampaignMissionUnitRoleComponent>(mission.Engineer);
            if (!role.SessionToken.Equals(runtime.SessionToken) || role.MissionRoleId.ToString() != "role.friendly.engineer") return false;
            using var q = em.CreateEntityQuery(typeof(UnitMoveOrderResultElement)); using var owners = q.ToEntityArray(Allocator.Temp);
            int2 gate = CampaignMissionSpawnSystem.ToGridCell(mission.AuditRelease, in grid);
            foreach (var owner in owners) foreach (var result in em.GetBuffer<UnitMoveOrderResultElement>(owner, true))
                if (result.RequestId > mission.ReleaseOrderBaseline && result.Issued != 0 && result.Entity == mission.Engineer && result.Kind == UnitMoveOrderRequestKind.GroupedManual && math.distancesq((float2)result.Goal, (float2)gate) * grid.CellSize * grid.CellSize <= 36) return true;
            return false;
        }
        internal static Entity ResolveCommandCoverActor(EntityManager em, Entity root)
        {
            if (!em.HasBuffer<CampaignMissionCommandNodeMember>(root)) return Entity.Null;
            foreach (var member in em.GetBuffer<CampaignMissionCommandNodeMember>(root, true))
            {
                if (member.Kind != CommandNodeMemberKind.Escort || member.Dead != 0 || !NetworkLive(em, member.Entity) || !em.HasComponent<UnitSourcePrefabKey>(member.Entity) || !em.HasComponent<UnitCombat>(member.Entity) || !em.HasComponent<UnitAttack>(member.Entity)) continue;
                var source = em.GetComponentData<UnitSourcePrefabKey>(member.Entity).Value;
                var weapon = em.GetComponentData<UnitAttack>(member.Entity);
                if (IsCommandCoverSource(in source) && em.GetComponentData<UnitCombat>(member.Entity).CanAttack != 0 && weapon.Damage > 0) return member.Entity;
            }
            return Entity.Null;
        }
        internal static bool CommandCoverReady(EntityManager em, Entity root, in CampaignMissionCommandNodeState mission)
        {
            if (!em.HasBuffer<CampaignMissionCommandNodeMember>(root)) return false;
            float3 gate = mission.BreachGate + new float3(0,0,20);
            foreach (var member in em.GetBuffer<CampaignMissionCommandNodeMember>(root, true))
            {
                if (member.Kind != CommandNodeMemberKind.Escort || member.Dead != 0 || !NetworkLive(em, member.Entity) || !em.HasComponent<UnitSourcePrefabKey>(member.Entity) || !em.HasComponent<UnitCombat>(member.Entity) || !em.HasComponent<UnitAttack>(member.Entity)) continue;
                var source = em.GetComponentData<UnitSourcePrefabKey>(member.Entity).Value;
                if (IsCommandCoverSource(in source) && em.GetComponentData<UnitCombat>(member.Entity).CanAttack != 0 && em.GetComponentData<UnitAttack>(member.Entity).Damage > 0 && CitywideStoppedAt(em, member.Entity, gate, 6)) return true;
            }
            return false;
        }
        private static bool IsCommandCoverSource(in FixedString64Bytes source)
        {
            FixedString64Bytes expected = "unit_veh_tank_usa";
            if (source.Length != expected.Length) return false;
            for (int index = 0; index < source.Length; index++)
            {
                byte value = source[index];
                if (value >= (byte)'A' && value <= (byte)'Z') value += (byte)('a' - 'A');
                if (value != expected[index]) return false;
            }
            return true;
        }
        internal static Entity ResolveCommandActor(EntityManager em, Entity root, CommandNodeMemberKind kind)
        {
            Entity best = Entity.Null; float range = -1;
            foreach (var member in em.GetBuffer<CampaignMissionCommandNodeMember>(root, true))
            {
                if (member.Kind != kind || member.Dead != 0 || !NetworkLive(em, member.Entity)) continue;
                if (kind != CommandNodeMemberKind.Escort) return member.Entity;
                if (!em.HasComponent<UnitCombat>(member.Entity) || em.GetComponentData<UnitCombat>(member.Entity).CanAttack == 0 || !em.HasComponent<UnitAttack>(member.Entity)) continue;
                var weapon = em.GetComponentData<UnitAttack>(member.Entity); if (weapon.Damage > 0 && weapon.Range > range) { best = member.Entity; range = weapon.Range; }
            }
            return best;
        }
        internal static void InitializeCommandNode(EntityManager em, Entity root, in CampaignMissionRuntimeComponent runtime, ref OperationMapBlob map)
        {
            var mission = new CampaignMissionCommandNodeState { SessionToken = runtime.SessionToken, AttemptOrdinal = runtime.AttemptOrdinal, SourceVersion = runtime.SourceVersion, Initialized = 1 };
            string[] names = { "isolate_clinic", "isolate_utility", "breach", "core_audit", "audit_release", "safe_receiving" };
            var points = new float3[6];
            for (int i = 0; i < 6; i++) { if (!CampaignMissionSpawnSystem.TryFindAnchor(ref map, new FixedString64Bytes("anchor.ch05.m05." + names[i]), out var anchor)) MarkCommandIntegrity(ref mission, "Missing anchor " + names[i]); points[i] = anchor.Position; }
            mission.IsolationClinic = points[0]; mission.IsolationUtility = points[1]; mission.BreachGate = points[2]; mission.CoreAudit = points[3]; mission.AuditRelease = points[4]; mission.SafeReceiving = points[5];
            if (!em.HasBuffer<CampaignMissionCommandNodeMember>(root)) em.AddBuffer<CampaignMissionCommandNodeMember>(root);
            var roster = em.GetBuffer<CampaignMissionCommandNodeMember>(root); roster.Clear();
            using var q = em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent), typeof(UnitHealth), typeof(LocalTransform)); using var actors = q.ToEntityArray(Allocator.Temp); using var commands = new EntityCommandBuffer(Allocator.Temp);
            foreach (var actor in actors)
            {
                var role = em.GetComponentData<CampaignMissionUnitRoleComponent>(actor); if (!role.SessionToken.Equals(runtime.SessionToken)) continue;
                var kind = role.MissionRoleId.ToString() switch { "role.friendly.escort" => CommandNodeMemberKind.Escort, "role.friendly.engineer" => CommandNodeMemberKind.Engineer, "role.friendly.specialist" => CommandNodeMemberKind.Specialist, "role.hostile.exterior" => CommandNodeMemberKind.Exterior, "role.hostile.node" => CommandNodeMemberKind.PerimeterNode, "role.hostile.core" => CommandNodeMemberKind.CoreGuard, "role.hostile.qassem" => CommandNodeMemberKind.Qassem, "role.civilian.protected" => CommandNodeMemberKind.CivicStaff, _ => (CommandNodeMemberKind)255 };
                if ((byte)kind == 255) { MarkCommandIntegrity(ref mission, "Unknown original role " + role.MissionRoleId); continue; }
                roster.Add(new CampaignMissionCommandNodeMember { Entity = actor, Kind = kind });
                if (kind == CommandNodeMemberKind.Engineer) mission.Engineer = actor;
                if (kind == CommandNodeMemberKind.PerimeterNode) mission.Node = actor;
                if (kind == CommandNodeMemberKind.Qassem) mission.Qassem = actor;
                if (kind is CommandNodeMemberKind.Engineer or CommandNodeMemberKind.Specialist or CommandNodeMemberKind.CivicStaff or CommandNodeMemberKind.PerimeterNode) QualifyCitywideEngineer(em, actor);
                if (kind is CommandNodeMemberKind.Exterior or CommandNodeMemberKind.PerimeterNode or CommandNodeMemberKind.CoreGuard or CommandNodeMemberKind.Qassem or CommandNodeMemberKind.CivicStaff) if (!em.HasComponent<CampaignMissionStationaryUnitTag>(actor)) commands.AddComponent<CampaignMissionStationaryUnitTag>(actor);
                if (kind is CommandNodeMemberKind.PerimeterNode or CommandNodeMemberKind.CoreGuard or CommandNodeMemberKind.Qassem) SetCommandSuppression(em, commands, actor, true);
                if (em.HasComponent<UnitMovementBehavior>(actor)) { var movement = em.GetComponentData<UnitMovementBehavior>(actor); movement.AllowIdleWander = 0; em.SetComponentData(actor, movement); }
            }
            int count = roster.Length; commands.Playback(em);
            if (count != 23 || mission.Engineer == Entity.Null || mission.Node == Entity.Null || mission.Qassem == Entity.Null) MarkCommandIntegrity(ref mission, "Original roster incomplete");
            if (!em.HasBuffer<CampaignMissionCommandBuildingRequest>(root)) em.AddBuffer<CampaignMissionCommandBuildingRequest>(root);
            var bindings = em.GetBuffer<CampaignMissionCommandBuildingRequest>(root); bindings.Clear(); for (int i = 0; i < 4; i++) bindings.Add(default);
            if (em.HasComponent<CampaignMissionCommandNodeState>(root)) em.SetComponentData(root, mission); else em.AddComponentData(root, mission);
        }
        private static void ApplyCommandActivation(EntityManager em, Entity root, bool active)
        {
            foreach (var member in em.GetBuffer<CampaignMissionCommandNodeMember>(root, true))
            {
                if (member.Dead != 0 || !em.Exists(member.Entity) || !em.HasComponent<ArmorBreakUnitActivationState>(member.Entity)) continue;
                var original = em.GetComponentData<ArmorBreakUnitActivationState>(member.Entity);
                if (em.HasComponent<UnitFuelConsumption>(member.Entity)) { var fuel = em.GetComponentData<UnitFuelConsumption>(member.Entity); fuel.Enabled = active ? original.OriginalFuelEnabled : (byte)0; em.SetComponentData(member.Entity, fuel); }
                if (em.HasComponent<UnitCombat>(member.Entity)) { var combat = em.GetComponentData<UnitCombat>(member.Entity); combat.AutoEngage = active ? original.OriginalAutoEngage : (byte)0; em.SetComponentData(member.Entity, combat); }
            }
        }
    }
}
