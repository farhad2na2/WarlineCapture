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
            var preset = Resources.Load<SkirmishPresetConfig>(SkirmishPresetConfig.ResourceName);
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
                if (em.HasComponent<ManualMoveOrderTag>(unit) || em.HasComponent<HoldPositionOrderTag>(unit) || em.HasComponent<EngageTarget>(unit)) continue;
                var spread = new float3((unit.Index % 3) * 2 - 2, 0, (unit.Index % 5) * 2 - 4);
                UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, unit, GridUtils.WorldToCell(grid, rally + spread));
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
            var preset=Resources.Load<SkirmishPresetConfig>(SkirmishPresetConfig.ResourceName);
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
            var preset=Resources.Load<SkirmishPresetConfig>(SkirmishPresetConfig.ResourceName);
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
                float best=preset.baseDefenseRadius*preset.baseDefenseRadius;
                bool threatened=false;
                foreach(var candidate in targets)
                {
                    byte faction=em.GetComponentData<Faction>(candidate).Id;
                    if(faction==0||faction==squad.FactionId||em.GetComponentData<UnitHealth>(candidate).Current<=0||em.GetComponentData<UnitAttack>(candidate).Damage<=0)continue;
                    float distance=math.distancesq(em.GetComponentData<LocalTransform>(candidate).Position.xz,em.GetComponentData<LocalTransform>(own).Position.xz);
                    if(distance>=best)continue;
                    best=distance;target=candidate;threatened=true;
                }
                // Attackers must fight the defenders they meet, rather than let
                // those defenders shoot them while every order targets the base.
                if (!threatened && em.HasBuffer<AISquadUnit>(squadEntity))
                {
                    best = float.MaxValue;
                    foreach (var member in em.GetBuffer<AISquadUnit>(squadEntity))
                    {
                        var unit = member.Unit;
                        if (!em.Exists(unit) || !em.HasComponent<UnitHealth>(unit) ||
                            em.GetComponentData<UnitHealth>(unit).Current <= 0 ||
                            !em.HasComponent<LocalTransform>(unit) || !em.HasComponent<UnitAttack>(unit)) continue;
                        // A slower vehicle must not drag the squad's detection
                        // center behind infantry already under fire.
                        var position = em.GetComponentData<LocalTransform>(unit).Position.xz;
                        float radius = em.GetComponentData<UnitAttack>(unit).Range + 8;
                        foreach (var candidate in targets)
                        {
                            byte faction = em.GetComponentData<Faction>(candidate).Id;
                            if (faction == 0 || faction == squad.FactionId || em.GetComponentData<UnitHealth>(candidate).Current <= 0 || em.GetComponentData<UnitAttack>(candidate).Damage <= 0) continue;
                            float distance = math.distancesq(em.GetComponentData<LocalTransform>(candidate).Position.xz, position);
                            if (distance >= radius * radius || distance >= best) continue;
                            best = distance; target = candidate; threatened = true;
                        }
                    }
                }
                if(!threatened && match.ElapsedSeconds<preset.firstAttackSeconds)target=Entity.Null;
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
    }
}
