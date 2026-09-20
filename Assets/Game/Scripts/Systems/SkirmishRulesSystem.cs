using Game.Components;
using Game.Configs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Game.Runtime
{
    public static class SkirmishOutcomeRules
    {
        public static bool Evaluate(ref SkirmishMatchState match, bool playerBaseAlive, bool enemyBaseAlive,
            float deltaSeconds, bool simulationActive, bool surrender = false)
        {
            if (match.Phase != SkirmishPhase.Playing) return false;
            if (simulationActive && math.isfinite(deltaSeconds)) match.ElapsedSeconds += math.max(0, deltaSeconds);
            if (!playerBaseAlive || !enemyBaseAlive)
            {
                match.Outcome = playerBaseAlive ? SkirmishOutcome.Victory : enemyBaseAlive ? SkirmishOutcome.Defeat : SkirmishOutcome.Draw;
                match.Reason = !playerBaseAlive && !enemyBaseAlive ? SkirmishEndReason.BothBasesDestroyed : SkirmishEndReason.MainBaseDestroyed;
            }
            else if (surrender) { match.Outcome=SkirmishOutcome.Defeat; match.Reason=SkirmishEndReason.Surrender; }
            else if (match.ElapsedSeconds >= SkirmishPresetConfig.MatchDurationSeconds)
            { match.Outcome=SkirmishOutcome.Draw; match.Reason=SkirmishEndReason.TimeLimit; }
            else return false;
            match.ElapsedSeconds=math.min(match.ElapsedSeconds,SkirmishPresetConfig.MatchDurationSeconds);
            match.Phase=SkirmishPhase.Finished;
            return true;
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct SkirmishRulesSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishMatchState>();
            state.RequireForUpdate<RuntimeGameplayStateComponent>();
        }
        public void OnUpdate(ref SystemState state)
        {
            var entity=SystemAPI.GetSingletonEntity<SkirmishMatchState>();
            var match=SystemAPI.GetSingleton<SkirmishMatchState>();
            var em=state.EntityManager;
            var gameplay=SystemAPI.GetSingleton<RuntimeGameplayStateComponent>();
            if(match.StartupFailure!=SkirmishStartupFailureCode.None &&
               !(match.ScenarioIndex==SkirmishPresetConfig.StressScaleProbeScenarioIndex &&
                 match.StartupFailure==SkirmishStartupFailureCode.Timeout))
                return;
            if(match.Phase is SkirmishPhase.Preparing or SkirmishPhase.Playing)
                SkirmishWorldSetup.NormalizeScenery(em);
            if(match.Phase==SkirmishPhase.Preparing && gameplay.SimulationActive!=0)
            {
                SkirmishWorldSetup.UseTacticalResources(em);
                using var query=em.CreateEntityQuery(typeof(RuntimeBuildingCombatTag),typeof(UnitSourcePrefabKey),typeof(Faction),typeof(UnitHealth));
                using var buildings=query.ToEntityArray(Allocator.Temp);
                foreach(var building in buildings)
                {
                    if(em.HasComponent<OperationMapBuildingComponent>(building) ||
                       !em.GetComponentData<UnitSourcePrefabKey>(building).Value.ToString().ToLowerInvariant().Contains("building_barrack")) continue;
                    byte faction=em.GetComponentData<Faction>(building).Id;
                    if(faction==1 && match.PlayerMainBase==Entity.Null)match.PlayerMainBase=building;
                    if(faction==2 && match.EnemyMainBase==Entity.Null)match.EnemyMainBase=building;
                }
                if(match.PlayerMainBase!=Entity.Null && match.EnemyMainBase!=Entity.Null)
                {
                    em.AddComponentData(match.PlayerMainBase,new SkirmishMainBase{FactionId=1});
                    em.AddComponentData(match.EnemyMainBase,new SkirmishMainBase{FactionId=2});
                    SkirmishWorldSetup.SeedSupplyAndAnchors(em);
                    SkirmishCombatPolicy.ApplyRoster(em,match);
                    SkirmishCombatPolicy.AssignInitialDefenders(em);
                    match.Phase=SkirmishPhase.Playing;
                    TrackLosses(em,entity,ref match);
                }
                em.SetComponentData(entity,match);
                return;
            }
            if(match.Phase!=SkirmishPhase.Playing)return;
            SkirmishCombatPolicy.ApplyRoster(em,match);
            SkirmishSquadAssignment.Assign(em);
            if (gameplay.SimulationActive != 0) SkirmishCombatPolicy.RallyPlayerReinforcements(em, match);
            TrackLosses(em,entity,ref match);
            bool ended=SkirmishOutcomeRules.Evaluate(ref match,Alive(em,match.PlayerMainBase),Alive(em,match.EnemyMainBase),
                SystemAPI.Time.DeltaTime,gameplay.SimulationActive!=0,match.SurrenderRequested!=0);
            em.SetComponentData(entity,match);
            if(ended)
            {
                gameplay.SimulationActive=0; gameplay.PlayRequested=0; gameplay.SelectionModeActive=0; gameplay.BuildModeActive=0;
                em.SetComponentData(SystemAPI.GetSingletonEntity<RuntimeGameplayStateComponent>(),gameplay);
            }
        }
        private static void TrackLosses(EntityManager em,Entity session,ref SkirmishMatchState match)
        {
            var tracked=em.GetBuffer<SkirmishTrackedUnit>(session);
            for(int i=tracked.Length-1;i>=0;i--)
            {
                var item=tracked[i];
                if(Alive(em,item.Entity))continue;
                if(item.FactionId==1){if(item.Building!=0)match.PlayerBuildingsLost++;else match.PlayerUnitsLost++;}
                else {if(item.Building!=0)match.EnemyBuildingsLost++;else match.EnemyUnitsLost++;}
                tracked.RemoveAtSwapBack(i);
            }
            // Authored scenery is neutral in this mode and cannot contribute losses.
            // Exclude it in the query instead of scanning thousands of map buildings.
            using var query=em.CreateEntityQuery(new EntityQueryDesc{
                All=new[]{ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<UnitHealth>()},
                None=new[]{ComponentType.ReadOnly<OperationMapBuildingComponent>(),ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>()}});
            using var units=query.ToEntityArray(Allocator.Temp);
            foreach(var unit in units)
            {
                byte faction=em.GetComponentData<Faction>(unit).Id;
                if(faction is not (1 or 2)||!Alive(em,unit))continue;
                bool found=false;for(int i=0;i<tracked.Length;i++)if(tracked[i].Entity==unit){found=true;break;}
                if(!found)tracked.Add(new SkirmishTrackedUnit{Entity=unit,FactionId=faction,Building=(byte)(em.HasComponent<RuntimeBuildingCombatTag>(unit)?1:0)});
            }
        }
        private static bool Alive(EntityManager em,Entity entity)=>entity!=Entity.Null && em.Exists(entity) &&
            em.HasComponent<UnitHealth>(entity) && em.GetComponentData<UnitHealth>(entity).Current>0;
    }
}
