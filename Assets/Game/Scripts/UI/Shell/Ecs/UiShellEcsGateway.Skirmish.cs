using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
using Unity.Entities;
using UnityEngine;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiSkirmishGateway
    {
        bool IUiSkirmishGateway.TryReadSkirmish(out UiSkirmishModel model)
        {
            model=default;
            if(!TrySkirmish(out var em,out var session,out var match)||(match.Phase<SkirmishPhase.Playing&&match.StartupFailure==SkirmishStartupFailureCode.None))return false;
            if(match.StartupFailure!=SkirmishStartupFailureCode.None)
            {
                model.StartupFailed=true;
                model.ResultTitle=GameText.Get("ui.skirmish.start_failed", "Skirmish could not start");
                model.ResultDetail=GameText.Get("ui.skirmish.start_failed."+match.StartupFailure.ToString().ToLowerInvariant());
                return true;
            }
            int remaining=Mathf.Max(0,Mathf.CeilToInt(SkirmishPresetConfig.MatchDurationSeconds-match.ElapsedSeconds));
            using var gameplay=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            bool paused=gameplay.CalculateEntityCount()==1&&gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive==0;
            string outcome=match.Outcome.ToString().ToLowerInvariant();
            string reason=match.Reason.ToString().ToLowerInvariant();
            if (match.Reason == SkirmishEndReason.MainBaseDestroyed)
                reason = match.Outcome == SkirmishOutcome.Victory ? "enemy_base_destroyed" : "player_base_destroyed";
            int infantry=0;
            using(var units=em.CreateEntityQuery(typeof(SkirmishSquadMember),typeof(UnitHealth)))
            {
                using var entities=units.ToEntityArray(Unity.Collections.Allocator.Temp);
                foreach(var unit in entities)if(em.GetComponentData<SkirmishSquadMember>(unit).Slot<4&&em.GetComponentData<UnitHealth>(unit).Current>0)infantry++;
            }
            model=new UiSkirmishModel
            {
                Finished=match.Phase==SkirmishPhase.Finished,Paused=paused,
                Infantry=infantry+" / "+SkirmishPresetConfig.InfantryLimitPerFaction,
                PlayerBase=BaseLabel(em,match.PlayerMainBase,"player_base","YOUR MAIN BASE"),
                EnemyBase=BaseLabel(em,match.EnemyMainBase,"enemy_base","ENEMY MAIN BASE"),
                Clock=GameText.Get("ui.skirmish.time_remaining","TIME LEFT")+"  "+remaining/60+":"+(remaining%60).ToString("00"),
                Objective=GameText.Get("ui.skirmish.objective_explanation","Destroy the marked enemy Barracks. Protect your own main base."),
                ResultTitle=GameText.Get("ui.skirmish.result."+outcome,match.Outcome.ToString()),
                ResultDetail=GameText.Get("ui.skirmish.reason."+reason,match.Reason.ToString()),
                Statistics=GameText.Format("ui.skirmish.statistics","Time {0} • Units lost {1} / defeated {2}\nBuildings lost {3} / destroyed {4}",
                    ((int)match.ElapsedSeconds/60)+":"+((int)match.ElapsedSeconds%60).ToString("00"),match.PlayerUnitsLost,match.EnemyUnitsLost,match.PlayerBuildingsLost,match.EnemyBuildingsLost)
            };
            return true;
        }
        bool IUiSkirmishGateway.TryRequestSkirmish(UiSkirmishAction action)
        {
            if(!TrySkirmish(out var em,out var entity,out var match)||(match.Phase<SkirmishPhase.Playing&&match.StartupFailure==SkirmishStartupFailureCode.None))return false;
            if(match.StartupFailure!=SkirmishStartupFailureCode.None && action!=UiSkirmishAction.Replay &&
                action!=UiSkirmishAction.AdjustSetup && action!=UiSkirmishAction.MainMenu)return false;
            var requests=em.GetBuffer<SkirmishActionRequest>(entity);
            if(requests.Length!=0)return false;
            requests.Add(new SkirmishActionRequest{Action=(SkirmishAction)action});return true;
        }
        private static bool TrySkirmish(out EntityManager em,out Entity entity,out SkirmishMatchState match)
        {
            em=default;entity=Entity.Null;match=default;
            var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return false;
            em=world.EntityManager;using var query=em.CreateEntityQuery(typeof(SkirmishMatchState));
            if(query.CalculateEntityCount()!=1)return false;
            entity=query.GetSingletonEntity();match=em.GetComponentData<SkirmishMatchState>(entity);return true;
        }
        private static string BaseLabel(EntityManager em,Entity entity,string key,string fallback)
        {
            float health=0,max=0;
            if(em.Exists(entity)&&em.HasComponent<UnitHealth>(entity)){var h=em.GetComponentData<UnitHealth>(entity);health=Mathf.Max(0,h.Current);max=h.Max;}
            return GameText.Get("ui.skirmish."+key,fallback)+"\n"+Mathf.CeilToInt(health)+" / "+Mathf.CeilToInt(max);
        }
    }
}
