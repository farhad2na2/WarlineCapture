using System.Collections.Generic;
using Game.Components;
using Game.Tactical.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed class RtsSelectionExternalCommandUtility
    {
        private readonly List<Entity> _missionSquadSelectionScratch = new();

        public TacticalCommandResult ValidateControllableEntity(
            RtsSelectionFocusCommandCompositionSystemHelper.Context context,
            Entity entity)
        {
            if (entity == Entity.Null ||
                context.TryGetEntityManager == null ||
                !context.TryGetEntityManager(out EntityManager em) ||
                !em.Exists(entity) ||
                !em.HasComponent<Faction>(entity) ||
                !em.HasComponent<UnitMove>(entity) ||
                !FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id) ||
                (em.HasComponent<UnitHealth>(entity) && em.GetComponentData<UnitHealth>(entity).Current <= 0))
            {
                return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
            }

            return TacticalCommandResult.Success();
        }

        public bool SelectMissionSquad(
            RtsSelectionFocusCommandCompositionSystemHelper.Context context,
            Entity representative)
        {
            if (!context.TryGetEntityManager(out EntityManager em) ||
                representative == Entity.Null ||
                !em.Exists(representative) ||
                !em.HasComponent<CampaignMissionUnitRoleComponent>(representative))
            {
                return false;
            }

            CampaignMissionUnitRoleComponent representativeRole =
                em.GetComponentData<CampaignMissionUnitRoleComponent>(representative);
            _missionSquadSelectionScratch.Clear();
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitMove>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            using NativeArray<CampaignMissionUnitRoleComponent> roles =
                query.ToComponentDataArray<CampaignMissionUnitRoleComponent>(Allocator.Temp);
            using NativeArray<Faction> factions = query.ToComponentDataArray<Faction>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!roles[i].SessionToken.Equals(representativeRole.SessionToken) ||
                    !roles[i].UnitGroupId.Equals(representativeRole.UnitGroupId) ||
                    !FactionIdentity.IsPlayerControlled(factions[i].Id) ||
                    (em.HasComponent<UnitHealth>(entities[i]) &&
                     em.GetComponentData<UnitHealth>(entities[i]).Current <= 0))
                {
                    continue;
                }

                _missionSquadSelectionScratch.Add(entities[i]);
            }

            if (_missionSquadSelectionScratch.Count == 0)
                return false;

            context.ClearCurrentSelection?.Invoke(em, "AssistantFocusSquad");
            for (int i = 0; i < _missionSquadSelectionScratch.Count; i++)
            {
                Entity entity = _missionSquadSelectionScratch[i];
                if (!em.HasComponent<SelectedUnitTag>(entity))
                    em.AddComponent<SelectedUnitTag>(entity);
            }

            context.SelectionStateCompositionSystemHelper.CacheSelectedMoveEntities(
                em, _missionSquadSelectionScratch);
            context.FocusedUnitLifecycleCompositionSystemHelper.ApplySelectionFocus(
                em,
                context.SelectionStateCompositionSystemHelper,
                _missionSquadSelectionScratch,
                _missionSquadSelectionScratch.Count,
                (entityManager, entity) => context.ApplyHudSelection?.Invoke(entityManager, entity),
                _ => context.ApplyHudSelection?.Invoke(em, representative));
            context.BuildingPlacementInteractionCompositionSystemHelper?.ClearSelectedBuilding(
                context.BuildingPlacementInteractionContext, "RTSSelection.FocusSquad");
            context.InputSystem.ClearActiveCommandMode();
            context.InputSystem.ClearQueuedMoveOrder();
            context.InputSystem.ClearPendingMoveCommandRequests();
            context.SetCameraDragging?.Invoke(false);
            context.LogSelectionDiagnostic?.Invoke(
                $"focusSquad result=True representative={representative} selected={_missionSquadSelectionScratch.Count}");
            return true;
        }
    }
}
