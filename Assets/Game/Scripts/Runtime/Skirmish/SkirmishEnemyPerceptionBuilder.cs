using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    // This projection is the commander's only live contact input. The visibility
    // gate precedes all hostile health, role and transform reads.
    public static class SkirmishEnemyPerceptionBuilder
    {
        public sealed class Group
        {
            public uint Id;
            public SkirmishRoleKind Role;
            public SkirmishPopulationCategory Category;
            public SkirmishTargetDomain Domains;
            public float3 Position;
            public int Members, Supply, Health, MaximumHealth;
            public bool StructureCapable => SkirmishExpandedEngagementService.DomainAllows(
                Domains, true, SkirmishPopulationCategory.None);
            public bool MobileCapable => SkirmishExpandedEngagementService.DomainAllows(
                Domains, false, SkirmishPopulationCategory.Infantry) ||
                SkirmishExpandedEngagementService.DomainAllows(Domains, false, SkirmishPopulationCategory.Ground);
        }
        public struct Contact
        {
            public Entity Entity;
            public FixedString64Bytes StableId;
            public float3 Position;
            public SkirmishPopulationCategory Category;
            public SkirmishRoleKind Role;
            public bool Structure, ThreatensStructure;
        }
        public sealed class Snapshot
        {
            public readonly List<Group> Groups = new();
            public readonly List<Contact> Visible = new();
            public readonly List<float3> OwnFacilities = new();
            public readonly List<float3> LastSeen = new();
            public float3 Home;
            public int HomeHealth, HomeMaximum, CombatSupply, LocalThreatSupply;
            public uint EventHash = 2166136261;
            public bool HomeAlive, ObjectiveAlive;
        }

        public static Snapshot Build(EntityManager em, Entity session, SkirmishResolvedSetup setup)
        {
            var result = new Snapshot
            {
                Home = new float3(setup.EnemyBaseWorldX, 0, setup.EnemyBaseWorldZ),
                HomeAlive = true, ObjectiveAlive = true
            };
            var sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            if (setup.ObjectiveKind==SkirmishObjectiveKind.BaseAssault && em.HasComponent<SkirmishBaseAssaultFactComponent>(session))
            {
                var facts = em.GetComponentData<SkirmishBaseAssaultFactComponent>(session);
                result.HomeAlive = facts.EnemyDesignatedAlive != 0;
                result.ObjectiveAlive = facts.PlayerDesignatedAlive != 0;
            }
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using var entities = query.ToEntityArray(Allocator.Temp);
            // Own facts never disclose hostile state. Embarked passengers are not
            // an independently commandable group and contribute no field force.
            foreach (var entity in entities)
            {
                var own = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (!own.SessionId.Equals(sessionId) || own.FactionId != setup.EnemyFaction ||
                    !SkirmishArmyGroupSystem.IsAlive(em, entity) || !em.HasComponent<LocalTransform>(entity)) continue;
                var position = em.GetComponentData<LocalTransform>(entity).Position;
                if (own.IsStructure != 0)
                {
                    result.OwnFacilities.Add(position);
                    if (em.HasComponent<SkirmishObjectiveRoleComponent>(entity) &&
                        em.GetComponentData<SkirmishObjectiveRoleComponent>(entity).Role == SkirmishObjectiveRoleKind.EnemyBase)
                    {
                        result.Home = position;
                        if (em.HasComponent<UnitHealth>(entity))
                        {
                            var health = em.GetComponentData<UnitHealth>(entity);
                            result.HomeHealth = health.Current; result.HomeMaximum = health.Max;
                        }
                    }
                    continue;
                }
                if (em.HasComponent<UnitTransportPassenger>(entity) ||
                    !em.HasComponent<SkirmishArmyGroupMembershipComponent>(entity) ||
                    !em.HasComponent<SkirmishUnitRoleComponent>(entity) ||
                    !em.HasComponent<SkirmishRoleOverlayComponent>(entity)) continue;
                var overlay = em.GetComponentData<SkirmishRoleOverlayComponent>(entity);
                if (overlay.Damage <= 0) continue;
                var role = em.GetComponentData<SkirmishUnitRoleComponent>(entity);
                // Air orchestration is separately enabled only after its native
                // operational owner has been certified; ground allocation never
                // issues a ground Move to an aircraft.
                if (role.Category == SkirmishPopulationCategory.Air) continue;
                uint id = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(entity).GroupId;
                if (id == 0) continue;
                var group = result.Groups.Find(g => g.Id == id);
                if (group == null)
                {
                    group = new Group { Id = id, Role = role.Role, Category = role.Category };
                    result.Groups.Add(group);
                }
                group.Members++; group.Position += position; group.Supply += role.SupplyCost;
                group.Domains |= overlay.TargetDomains;
                if (em.HasComponent<UnitHealth>(entity))
                {
                    var health = em.GetComponentData<UnitHealth>(entity);
                    group.Health += health.Current; group.MaximumHealth += health.Max;
                }
            }
            result.Groups.Sort((a,b) => a.Id.CompareTo(b.Id));
            foreach (var group in result.Groups)
            {
                group.Position /= group.Members;
                result.CombatSupply += group.Supply;
                Mix(ref result.EventHash, group.Id); Mix(ref result.EventHash, (uint)group.Members);
            }
            Mix(ref result.EventHash, (uint)result.HomeHealth);
            foreach (var entity in entities)
            {
                var own = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (!own.SessionId.Equals(sessionId) || own.FactionId != setup.PlayerFaction) continue;
                if (!SkirmishFogService.IsVisibleToFaction(em, entity, (byte)setup.EnemyFaction)) continue;
                if (!SkirmishArmyGroupSystem.IsAlive(em, entity) || !em.HasComponent<LocalTransform>(entity) ||
                    em.HasComponent<UnitTransportPassenger>(entity)) continue;
                var role = em.HasComponent<SkirmishUnitRoleComponent>(entity)
                    ? em.GetComponentData<SkirmishUnitRoleComponent>(entity) : default;
                var overlay = em.HasComponent<SkirmishRoleOverlayComponent>(entity)
                    ? em.GetComponentData<SkirmishRoleOverlayComponent>(entity) : default;
                var contact = new Contact
                {
                    Entity = entity, StableId = own.StableObjectId,
                    Position = em.GetComponentData<LocalTransform>(entity).Position,
                    Category = role.Category, Role = role.Role, Structure = own.IsStructure != 0,
                    ThreatensStructure = overlay.Damage > 0 && SkirmishExpandedEngagementService.DomainAllows(
                        overlay.TargetDomains, true, SkirmishPopulationCategory.None)
                };
                result.Visible.Add(contact);
            }
            result.Visible.Sort((a,b) => a.StableId.CompareTo(b.StableId));
            foreach (var contact in result.Visible)
            {
                foreach (char value in contact.StableId.ToString()) Mix(ref result.EventHash, value);
                bool local = math.distancesq(contact.Position.xz, result.Home.xz) <= 90f * 90f;
                if (!local) foreach (var facility in result.OwnFacilities)
                    if (math.distancesq(contact.Position.xz, facility.xz) <= 55f * 55f) { local = true; break; }
                if (local)
                {
                    result.LocalThreatSupply += contact.ThreatensStructure ? 4 : 1;
                    Mix(ref result.EventHash, 1);
                }
            }
            // Ledger positions are frozen observations, never refreshed here from
            // a live hidden Entity. The fog owner expires stale knowledge.
            if (em.HasBuffer<SkirmishContactLedgerElement>(session) &&
                em.HasComponent<SkirmishContactLedgerStateComponent>(session) &&
                em.GetComponentData<SkirmishContactLedgerStateComponent>(session).SessionId.Equals(sessionId))
            {
                var ledger = em.GetBuffer<SkirmishContactLedgerElement>(session);
                for (int i = 0; i < ledger.Length; i++)
                    if (ledger[i].ObserverFactionId == setup.EnemyFaction &&
                        ledger[i].Sight == SkirmishContactSight.LastSeen)
                        result.LastSeen.Add(ledger[i].LastSeenPosition);
            }
            return result;
        }
        private static void Mix(ref uint hash, uint value) => hash = unchecked((hash ^ value) * 16777619);
    }
}
