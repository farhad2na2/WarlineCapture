using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed partial class BuildingRuntimeEntityCompositionSystemHelper
    {
        public bool DeleteBuildingById(Context context, int buildingId)
        {
            if (context.TryGetEntityManager != null && context.TryGetEntityManager(out var em))
            {
                using var query = em.CreateEntityQuery(typeof(SkirmishMainBase), typeof(RuntimeBuildingCombatInfo), typeof(UnitHealth));
                using var entities = query.ToEntityArray(Allocator.Temp);
                foreach (var entity in entities)
                    if (em.GetComponentData<RuntimeBuildingCombatInfo>(entity).RuntimeBuildingId == buildingId &&
                        em.GetComponentData<UnitHealth>(entity).Current > 0) return false;
            }
            return context.CombatSystem != null &&
                context.CombatSystem.DeleteBuilding(
                    context.CombatContext,
                    buildingId,
                    destroyVisual: true,
                    context.GetTime?.Invoke() ?? 0f,
                    context.DestroyedBuildingLifetimeSeconds);
        }

        internal void FinalizeBuildingRemoval(Context context,int buildingId)=>
            context.CombatSystem?.FinalizeDestroyedBuilding(context.CombatContext,buildingId);

        public void HandleRuntimeBuildingEntityDestroyed(
            Context context,
            int buildingId,
            Entity blockerEntity,
            GameObject buildingObject)
        {
            context.CombatSystem?.HandleRuntimeBuildingEntityDestroyed(
                context.CombatContext,
                buildingId,
                blockerEntity,
                buildingObject);
        }

    }
}
