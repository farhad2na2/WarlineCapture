using Game.Components;using Game.Runtime;using Game.UI.Contracts;using Unity.Entities;
namespace Game.Runtime{internal static class SourceS002ReadinessGateway{
internal static bool Read(EntityManager sourceEm,Entity sourceSession,out UiSkirmishReadinessModel model)
        {
            model = default;
            if (!TrySkirmish(sourceEm,sourceSession,out var em, out var session, out var match) || match.Phase != SkirmishPhase.Playing ||
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
private static bool TrySkirmish(EntityManager sourceEm,Entity sourceSession,out EntityManager em,out Entity entity,out SkirmishMatchState match){
 em=sourceEm;entity=Entity.Null;match=default;if(em==null)return false;using var query=em.CreateEntityQuery(typeof(SkirmishMatchState));if(query.CalculateEntityCount()!=1)return false;entity=query.GetSingletonEntity();if(entity!=sourceSession)throw new System.NotSupportedException("Readiness singleton is not the owned source session");match=em.GetComponentData<SkirmishMatchState>(entity);return true;
}
}}
