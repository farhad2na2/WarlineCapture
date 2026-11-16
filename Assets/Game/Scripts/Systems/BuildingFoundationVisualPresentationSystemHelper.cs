using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingFoundationVisualPresentationSystemHelper
    {
        public void ApplyVisualFoundation(GameObject instance, BuildingSurfacePlacementUtilitySystemHelper.Result surfaceResult)
        {
            if (instance == null)
                return;

            Vector3 position = instance.transform.position;
            position.y = surfaceResult.FoundationHeight;
            instance.transform.position = position;
        }

        public void ApplyCombatEntityFoundation(
            EntityManager em,
            Entity entity,
            BuildingSurfacePlacementUtilitySystemHelper.Result surfaceResult,
            BuildingSurfacePlacementUtilitySystemHelper surfacePlacementSystem)
        {
            BuildingRuntimeDataRegistrationSystemHelper.ApplyCombatEntityFoundation(
                em, entity, surfaceResult, surfacePlacementSystem);
        }
    }
}
