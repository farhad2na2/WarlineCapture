using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    public static class SkirmishCombatPolicy
    {
        internal static void RallyPlayerReinforcements(EntityManager em, SkirmishMatchState match)
        {
            if (match.Phase != SkirmishPhase.Playing || !em.Exists(match.PlayerMainBase) ||
                !em.HasComponent<LocalTransform>(match.PlayerMainBase)) return;
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (grids.CalculateEntityCount() != 1) return;
            var grid = grids.GetSingleton<GridConfig>();
            var preset = SkirmishPresetResolver.Load(em);
            var rally = em.GetComponentData<LocalTransform>(match.PlayerMainBase).Position + (float3)preset.playerReinforcementRallyOffset;
            using var producers = em.CreateEntityQuery(typeof(BuildingProducedUnitReadModel));
            using var roots = producers.ToEntityArray(Allocator.Temp);
            // Queue first: processing movement can structurally change entities and
            // invalidate the production read-model buffer being enumerated.
            using var recruits = new NativeList<Entity>(Allocator.Temp);
            foreach (var root in roots)
                foreach (var produced in em.GetBuffer<BuildingProducedUnitReadModel>(root, true))
                {
                    var unit = produced.Unit;
                    if (!em.Exists(unit) || em.HasComponent<SkirmishRallyAssigned>(unit) ||
                        !em.HasComponent<SkirmishSquadMember>(unit) ||
                        em.GetComponentData<SkirmishSquadMember>(unit).Slot >= 4 ||
                        !em.HasComponent<UnitMove>(unit) || !em.HasComponent<UnitHealth>(unit) ||
                        em.GetComponentData<UnitHealth>(unit).Current <= 0 || recruits.Contains(unit)) continue;
                    recruits.Add(unit);
                }
            foreach (var unit in recruits)
            {
                em.AddComponent<SkirmishRallyAssigned>(unit);
                if (em.HasComponent<AttackMoveOrder>(unit) || em.HasComponent<ManualMoveOrderTag>(unit) || em.HasComponent<HoldPositionOrderTag>(unit) || em.HasComponent<EngageTarget>(unit)) continue;
                var spread = new float3((unit.Index % 3) * 2 - 2, 0, (unit.Index % 5) * 2 - 4);
                if (UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, unit, GridUtils.WorldToCell(grid, rally + spread)))
                {
                    // This is an automatic rally, not a player instruction to ignore combat.
                    // Allow normal engagement/retaliation to interrupt the path.
                    em.RemoveComponent<ManualMoveOrderTag>(unit);
                }
            }
        }

        // Dead squads must release formation slots so paid reinforcements can deploy.
        internal static void RetireEmptySquads(EntityManager em)
        {
            using var session = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>());
            if (session.IsEmptyIgnoreFilter) return;
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<AISquad>(), ComponentType.ReadWrite<AISquadUnit>());
            using var squads = query.ToEntityArray(Allocator.Temp);
            foreach (var squad in squads)
            {
                var members = em.GetBuffer<AISquadUnit>(squad);
                for (int i = members.Length - 1; i >= 0; i--)
                {
                    var unit = members[i].Unit;
                    if (!em.Exists(unit) || !em.HasComponent<UnitHealth>(unit) || em.GetComponentData<UnitHealth>(unit).Current <= 0)
                        members.RemoveAt(i);
                }
                if (members.Length == 0) em.DestroyEntity(squad);
            }
        }

        // Shared by both factions and every replacement. Campaign prefab values stay intact.
        internal static void ApplyRoster(EntityManager em, SkirmishMatchState match)
        {
            var preset=SkirmishPresetResolver.Load(em);
            using var query=em.CreateEntityQuery(new EntityQueryDesc{
                All=new[]{ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<UnitAttack>(),ComponentType.ReadOnly<UnitSourcePrefabKey>()},
                None=new[]{ComponentType.ReadOnly<SkirmishCombatTuned>(),ComponentType.ReadOnly<OperationMapBuildingComponent>()}});
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)
            {
                byte faction=em.GetComponentData<Faction>(entity).Id;if(faction is not (1 or 2))continue;
                string key=em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString().ToLowerInvariant();
                bool rifle=key.Contains("soldier"), car=key.Contains("light_armored_car"), tower=key.Contains("guardtower");
                if(rifle||car||tower)
                {
                    var attack=em.GetComponentData<UnitAttack>(entity);
                    attack.Range=tower?preset.watchtowerRange:rifle?preset.rifleRange:preset.armoredCarRange;
                    attack.Damage=tower?preset.watchtowerDamage:rifle?preset.rifleDamage:preset.armoredCarDamage;
                    attack.CooldownSeconds=tower?preset.watchtowerCooldown:rifle?preset.rifleCooldown:preset.armoredCarCooldown;
                    em.SetComponentData(entity,attack);
                    // Buildings fire through this component, not UnitAttack. Keep the
                    // actual weapon consistent with the skirmish roster for both sides.
                    if(tower && em.HasComponent<BuildingDefenseWeapon>(entity))
                    {
                        var weapon=em.GetComponentData<BuildingDefenseWeapon>(entity);
                        weapon.Range=attack.Range;
                        weapon.Damage=attack.Damage;
                        weapon.CooldownSeconds=attack.CooldownSeconds;
                        em.SetComponentData(entity,weapon);
                    }
                    if(em.HasComponent<UnitCombat>(entity))
                    {
                        var combat=em.GetComponentData<UnitCombat>(entity);
                        combat.AggroRangeCells=(int)attack.Range+8;combat.ChaseBreakDistance=attack.Range+24;
                        em.SetComponentData(entity,combat);
                    }
                }
                em.AddComponent<SkirmishCombatTuned>(entity);
            }
        }

        internal static void AssignInitialDefenders(EntityManager em)
        {
            using var query=em.CreateEntityQuery(new EntityQueryDesc{
                All=new[]{ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<UnitSourcePrefabKey>(),ComponentType.ReadOnly<UnitMove>()},
                None=new[]{ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),ComponentType.ReadOnly<UnitResourceHauler>()}});
            using var units=query.ToEntityArray(Allocator.Temp);int assigned=0;
            foreach(var unit in units)
                if(em.GetComponentData<Faction>(unit).Id==2 &&
                    em.GetComponentData<UnitSourcePrefabKey>(unit).Value.ToString().ToLowerInvariant().Contains("soldier") && assigned<4)
                {em.AddComponent<SkirmishBaseDefender>(unit);assigned++;}
        }

        // Keep the existing squad/order systems. This preset changes their objective
        // selection: defend a threatened base, otherwise advance on the opposing base.
        internal static bool TryAssignTargets(EntityManager em)
        {
            using var session=em.CreateEntityQuery(typeof(SkirmishMatchState));
            if(session.CalculateEntityCount()!=1)return false;
            var match=session.GetSingleton<SkirmishMatchState>();
            if(match.Phase!=SkirmishPhase.Playing)return true;
            var preset=SkirmishPresetResolver.Load(em);
            using var squads=em.CreateEntityQuery(typeof(AISquad));
            using var squadEntities=squads.ToEntityArray(Allocator.Temp);
            using var enemies=em.CreateEntityQuery(new EntityQueryDesc{
                All=new[]{ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<UnitHealth>(),ComponentType.ReadOnly<UnitAttack>(),ComponentType.ReadOnly<LocalTransform>()},
                None=new[]{ComponentType.ReadOnly<UnitResourceHauler>(),ComponentType.ReadOnly<OperationMapBuildingComponent>()}});
            using var targets=enemies.ToEntityArray(Allocator.Temp);
            foreach(var squadEntity in squadEntities)
            {
                var squad=em.GetComponentData<AISquad>(squadEntity);
                var own=squad.FactionId==2?match.EnemyMainBase:match.PlayerMainBase;
                var target=squad.FactionId==2?match.PlayerMainBase:match.EnemyMainBase;
                if(!em.Exists(own)||!em.HasComponent<LocalTransform>(own))continue;
                var ownPosition = em.GetComponentData<LocalTransform>(own).Position;
                var context = new SquadTargetContext
                {
                    Manager = em, Targets = targets, SelfFaction = squad.FactionId,
                    HasMembers = em.HasBuffer<AISquadUnit>(squadEntity),
                    Members = em.HasBuffer<AISquadUnit>(squadEntity) ? em.GetBuffer<AISquadUnit>(squadEntity) : default
                };
                var selection = SkirmishSquadTargetingKernel.Select(ref context, new SquadTargetInput
                {
                    Faction = squad.FactionId, OwnBaseX = ownPosition.x, OwnBaseZ = ownPosition.z,
                    DefenseRadius = preset.baseDefenseRadius, ElapsedSeconds = match.ElapsedSeconds,
                    FirstAttackSeconds = preset.firstAttackSeconds,
                    OpposingBase = new SquadTargetIdentity(target.Index, target.Version)
                });
                target = new Entity { Index = selection.Target.Index, Version = selection.Target.Version };
                bool threatened = selection.Threatened;
                squad.TargetEntity=target;
                if(em.Exists(target)&&em.HasComponent<UnitGrid>(target))
                {
                    squad.TargetCell=em.GetComponentData<UnitGrid>(target).Cell;
                    squad.TargetFactionId=(byte)(squad.FactionId==2?1:2);
                    squad.TargetKind=(byte)(threatened?AITargetKind.Threat:AITargetKind.Building);
                }
                em.SetComponentData(squadEntity,squad);
            }
            return true;
        }
        private struct SquadTargetContext : ISquadTargetContext
        {
            public EntityManager Manager;
            public byte SelfFaction;
            public NativeArray<Entity> Targets;
            public bool HasMembers;
            public DynamicBuffer<AISquadUnit> Members;
            public int CandidateCount => Targets.Length;
            public int MemberCount => HasMembers ? Members.Length : 0;
            public SquadTargetCandidate Candidate(int index)
            {
                Entity entity = Targets[index];
                var candidate = new SquadTargetCandidate { Faction = Manager.GetComponentData<Faction>(entity).Id };
                if (candidate.Faction == 0 || candidate.Faction == SelfFaction) return candidate;
                candidate.Health = Manager.GetComponentData<UnitHealth>(entity).Current;
                if (candidate.Health <= 0) return candidate;
                candidate.Damage = Manager.GetComponentData<UnitAttack>(entity).Damage;
                if (candidate.Damage <= 0) return candidate;
                var position = Manager.GetComponentData<LocalTransform>(entity).Position;
                candidate.Entity = new SquadTargetIdentity(entity.Index, entity.Version);
                candidate.X = position.x; candidate.Z = position.z;
                return candidate;
            }
            public SquadTargetMember Member(int index)
            {
                Entity entity = Members[index].Unit;
                if (!Manager.Exists(entity) || !Manager.HasComponent<UnitHealth>(entity) ||
                    Manager.GetComponentData<UnitHealth>(entity).Current <= 0 ||
                    !Manager.HasComponent<LocalTransform>(entity) || !Manager.HasComponent<UnitAttack>(entity)) return default;
                var position = Manager.GetComponentData<LocalTransform>(entity).Position;
                return new SquadTargetMember
                {
                    Eligible = true, X = position.x, Z = position.z,
                    Range = Manager.GetComponentData<UnitAttack>(entity).Range
                };
            }
        }
    }
}
