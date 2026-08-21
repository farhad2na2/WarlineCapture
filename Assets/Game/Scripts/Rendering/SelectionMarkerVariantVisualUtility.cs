using Game.Components;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;

namespace Game.Rendering
{
    internal static class SelectionMarkerVariantVisualUtility
    {
        public static bool TryApplyExplicitVariants(
            EntityManager entityManager,
            Entity marker,
            bool usesVehicleMarker,
            bool showVehicleGroundMarker)
        {
            if (!entityManager.HasComponent<SelectionMarkerVariantVisuals>(marker))
                return false;

            SelectionMarkerVariantVisuals visuals =
                entityManager.GetComponentData<SelectionMarkerVariantVisuals>(marker);
            SetVisible(entityManager, visuals.InfantryGroundRing, !usesVehicleMarker);
            SetVisible(entityManager, visuals.VehicleFootprintFill, showVehicleGroundMarker);
            SetVisible(entityManager, visuals.VehicleCornerBrackets, showVehicleGroundMarker);
            SetVisible(entityManager, visuals.VehicleBoundsFrame, showVehicleGroundMarker);
            return true;
        }

        public static void SetRendering(EntityManager entityManager, Entity entity, bool visible)
        {
            if (entity == Entity.Null || !entityManager.Exists(entity) ||
                !entityManager.HasComponent<MaterialMeshInfo>(entity))
            {
                return;
            }

            bool renderingDisabled = entityManager.HasComponent<DisableRendering>(entity);
            if (visible && renderingDisabled)
                entityManager.RemoveComponent<DisableRendering>(entity);
            else if (!visible && !renderingDisabled)
                entityManager.AddComponent<DisableRendering>(entity);
        }

        private static void SetVisible(EntityManager entityManager, Entity entity, bool visible)
        {
            if (entity == Entity.Null || !entityManager.Exists(entity) ||
                !entityManager.HasComponent<LocalTransform>(entity))
            {
                return;
            }

            LocalTransform transform = entityManager.GetComponentData<LocalTransform>(entity);
            transform.Scale = visible ? 1f : 0f;
            entityManager.SetComponentData(entity, transform);
            SetRendering(entityManager, entity, visible);
        }
    }
}
