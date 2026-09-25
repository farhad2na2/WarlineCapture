using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Unity.Entities;
using UnityEngine;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiSkirmishGateway, IUiSkirmishReadinessGateway
    {
        bool IUiSkirmishReadinessGateway.TryReadSkirmishReadiness(out UiSkirmishReadinessModel model)
        {
            model = default;
            if (!TrySkirmish(out var em, out var session, out var match) || match.Phase != SkirmishPhase.Playing ||
                !SkirmishExpandedSessionControlService.IsExpanded(em, session) ||
                !em.HasComponent<SkirmishResearchStateComponent>(session)) return false;
            model.Visible = true;
            model.Stage = (byte)em.GetComponentData<SkirmishResearchStateComponent>(session).Readiness;
            model.CanUpgrade = SkirmishResearchService.CanQueue(em, session,
                Game.Skirmish.Contracts.SkirmishResearchKind.Readiness, 1, out var decision, out var seconds);
            model.MaterialsCost = decision.MaterialsCost;
            model.RemainingSeconds = seconds;
            model.Reason = decision.Reason.ToString();
            if (em.HasBuffer<SkirmishResearchQueueItem>(session))
                foreach (var item in em.GetBuffer<SkirmishResearchQueueItem>(session))
                    if (item.FactionId == 1 && item.Kind == Game.Skirmish.Contracts.SkirmishResearchKind.Readiness &&
                        item.Phase is Game.Skirmish.Contracts.SkirmishResearchPhase.Queued or Game.Skirmish.Contracts.SkirmishResearchPhase.Researching)
                    {
                        model.InProgress = model.CanCancel = true;
                        model.CanUpgrade = false;
                        model.ResearchId = item.ResearchId;
                        model.MaterialsCost = item.MaterialsPaid;
                        model.RemainingSeconds = item.RemainingSeconds;
                        break;
                    }
            using var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (gameplay.CalculateEntityCount() == 1 && gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive == 0)
                model.CanUpgrade = model.CanCancel = false;
            return true;
        }

        bool IUiSkirmishReadinessGateway.TryRequestSkirmishReadiness(uint cancelResearchId)
        {
            if (!((IUiSkirmishReadinessGateway)this).TryReadSkirmishReadiness(out var model) ||
                (cancelResearchId == 0 ? !model.CanUpgrade : !model.CanCancel || cancelResearchId != model.ResearchId) ||
                !TrySkirmish(out var em, out var session, out _) || !em.HasBuffer<SkirmishReadinessRequest>(session)) return false;
            var requests = em.GetBuffer<SkirmishReadinessRequest>(session);
            if (requests.Length != 0) return false;
            requests.Add(new SkirmishReadinessRequest { CancelResearchId = cancelResearchId,
                InputReceipt = AriaCommandEvidence.ClaimRelease(UnityEngine.Time.frameCount) });
            return true;
        }

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
            bool expanded = SkirmishExpandedSessionControlService.IsExpanded(em, session);
            // Expanded sessions own their objective clock/deadline; the legacy
            // 900-second constant would describe a different rules model.
            float elapsedSeconds = match.ElapsedSeconds;
            int deadlineSeconds = (int)SkirmishPresetConfig.MatchDurationSeconds;
            if (expanded && em.HasComponent<SkirmishObjectiveClockComponent>(session))
            {
                var objectiveClock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
                elapsedSeconds = objectiveClock.ElapsedSeconds;
                deadlineSeconds = objectiveClock.DeadlineSeconds;
            }
            int remaining=Mathf.Max(0,Mathf.CeilToInt(deadlineSeconds-elapsedSeconds));
            using var gameplay=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            bool paused=gameplay.CalculateEntityCount()==1&&gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive==0;
            string outcome=match.Outcome.ToString().ToLowerInvariant();
            string reason=match.Reason.ToString().ToLowerInvariant();
            if (match.Reason == SkirmishEndReason.MainBaseDestroyed)
                reason = match.Outcome == SkirmishOutcome.Victory ? "enemy_base_destroyed" : "player_base_destroyed";
            int infantry=0;
            int infantryCap = SkirmishPresetConfig.InfantryLimitPerFaction;
            if (expanded && em.HasComponent<SkirmishCapacityComponent>(session))
            {
                var capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
                infantry = capacity.InfantryLive;
                infantryCap = capacity.InfantryCap;
            }
            else
            {
                using(var units=em.CreateEntityQuery(typeof(SkirmishSquadMember),typeof(UnitHealth)))
                {
                    using var entities=units.ToEntityArray(Unity.Collections.Allocator.Temp);
                    foreach(var unit in entities)if(em.GetComponentData<SkirmishSquadMember>(unit).Slot<4&&em.GetComponentData<UnitHealth>(unit).Current>0)infantry++;
                }
            }
            bool playerAlive = true;
            bool enemyAlive = true;
            if (expanded && em.HasComponent<SkirmishBaseAssaultFactComponent>(session))
            {
                var facts = em.GetComponentData<SkirmishBaseAssaultFactComponent>(session);
                playerAlive = facts.PlayerDesignatedAlive != 0;
                enemyAlive = facts.EnemyDesignatedAlive != 0;
            }
            int playerHealth=VisibleBaseHealth(em,match.PlayerMainBase);
            int enemyHealth=VisibleBaseHealth(em,match.EnemyMainBase);
            string playerBase=BaseLabel(em,match.PlayerMainBase,"player_base","YOUR MAIN BASE");
            string enemyBase=BaseLabel(em,match.EnemyMainBase,"enemy_base","ENEMY MAIN BASE");
            if(SkirmishExpandedSessionControlService.IsExpanded(em,session))
            {
                var playerReadout=SkirmishBaseAssaultHudProjection.ReadPlayerBase(em,session);
                var enemyReadout=SkirmishBaseAssaultHudProjection.ReadEnemyBase(em,session);
                playerHealth=playerReadout.DisplayedCurrent;
                enemyHealth=enemyReadout.DisplayedCurrent;
                playerBase=HiddenAwareBaseLabel(playerReadout,"player_base","YOUR MAIN BASE");
                enemyBase=HiddenAwareBaseLabel(enemyReadout,"enemy_base","ENEMY MAIN BASE");
            }
            string objective=GameText.Get("ui.skirmish.objective_explanation","Destroy the marked enemy Barracks. Protect your own main base.");
            string resultTitle=GameText.Get("ui.skirmish.result."+outcome,match.Outcome.ToString());
            string resultDetail=GameText.Get("ui.skirmish.reason."+reason,match.Reason.ToString());
            if(SkirmishExpandedSessionControlService.IsExpanded(em,session) &&
               SkirmishExpandedHudCopy.TryResolve(em,session,match,GameLocalization.CurrentLocaleCode,out string expandedObjective,out string expandedResult,out string expandedDetail))
            {
                objective=expandedObjective;
                if(!string.IsNullOrEmpty(expandedResult))
                    resultTitle=expandedResult;
                if(!string.IsNullOrEmpty(expandedDetail))
                    resultDetail=expandedDetail;
            }
            model=new UiSkirmishModel
            {
                ScenarioIndex=match.ScenarioIndex,
                Finished=match.Phase==SkirmishPhase.Finished,Paused=paused,
                InfantryCount=infantry, PlayerHealth=playerHealth, EnemyHealth=enemyHealth,
                Expanded=expanded, PlayerDesignatedAlive=playerAlive, EnemyDesignatedAlive=enemyAlive,
                Infantry=infantry+" / "+infantryCap,
                PlayerBase=playerBase,
                EnemyBase=enemyBase,
                Clock=GameText.Get("ui.skirmish.time_remaining","TIME LEFT")+"  "+remaining/60+":"+(remaining%60).ToString("00"),
                Objective=objective,
                ResultTitle=resultTitle,
                ResultDetail=resultDetail,
                Statistics=GameText.Format("ui.skirmish.statistics","Time {0} • Units lost {1} / defeated {2}\nBuildings lost {3} / destroyed {4}",
                    ((int)match.ElapsedSeconds/60)+":"+((int)match.ElapsedSeconds%60).ToString("00"),match.PlayerUnitsLost,match.EnemyUnitsLost,match.PlayerBuildingsLost,match.EnemyBuildingsLost)
            };
            if (expanded && em.HasComponent<SkirmishResolvedSetupRecord>(session))
            {
                var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                if (setup != null)
                {
                    var publicView = SkirmishAriaPublicProjection.FromSession(em, session,
                        SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId));
                    model.OwnMaterials = publicView.OwnMaterials;
                    model.CanAffordRifle = publicView.CanAffordRifle;
                    model.CanAffordAntiAir = publicView.CanAffordAntiAir;
                    model.CanAffordLogisticsTruck = publicView.CanAffordLogisticsTruck;
                    model.CanBuildAirPad = publicView.CanBuildAirPad;
                    model.LogisticsTruckCommitted = publicView.LogisticsTruckCommitted;
                    model.RifleRecruitPending = publicView.RifleRecruitPending;
                    model.AirProfile = publicView.AirProfile;
                    model.PadPresent = publicView.PadPresent;
                    model.ReadinessEligible = publicView.ReadinessEligible;
                    model.CanQueueAir = publicView.AirQueueOffered;
                    model.VisibleHostileCombat = publicView.VisibleHostileCombat;
                    model.VisibleHostileAir = publicView.VisibleHostileAir;
                }
            }
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
        private static int VisibleBaseHealth(EntityManager em, Entity entity) =>
            em.Exists(entity) && em.HasComponent<UnitHealth>(entity)
                ? Mathf.CeilToInt(Mathf.Max(0, em.GetComponentData<UnitHealth>(entity).Current)) : 0;
        private static string BaseLabel(EntityManager em,Entity entity,string key,string fallback)
        {
            float health=0,max=0;
            if(em.Exists(entity)&&em.HasComponent<UnitHealth>(entity)){var h=em.GetComponentData<UnitHealth>(entity);health=Mathf.Max(0,h.Current);max=h.Max;}
            return GameText.Get("ui.skirmish."+key,fallback)+"\n"+Mathf.CeilToInt(health)+" / "+Mathf.CeilToInt(max);
        }
        private static string HiddenAwareBaseLabel(SkirmishHealthReadout readout,string key,string fallback)
        {
            return GameText.Get("ui.skirmish."+key,fallback)+"\n"+
                   Mathf.Max(0,readout.DisplayedCurrent)+" / "+Mathf.Max(0,readout.Max);
        }
    }
}
