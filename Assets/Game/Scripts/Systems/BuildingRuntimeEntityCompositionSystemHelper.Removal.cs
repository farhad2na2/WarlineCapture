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
