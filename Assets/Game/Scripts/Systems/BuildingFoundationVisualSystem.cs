using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

internal sealed partial class BuildingFoundationVisualSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void ApplyVisualFoundation(GameObject instance, BuildingSurfacePlacementSystem.Result surfaceResult)
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
        BuildingSurfacePlacementSystem.Result surfaceResult,
        BuildingSurfacePlacementSystem surfacePlacementSystem)
    {
        if (entity == Entity.Null || !em.Exists(entity) || surfacePlacementSystem == null)
            return;

        if (em.HasComponent<LocalTransform>(entity))
        {
            LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
            transform.Position.y = surfaceResult.FoundationHeight;
            em.SetComponentData(entity, transform);
        }

        BuildingSurfaceComponent surfaceComponent = surfacePlacementSystem.ToComponent(surfaceResult);
        if (em.HasComponent<BuildingSurfaceComponent>(entity))
            em.SetComponentData(entity, surfaceComponent);
        else
            em.AddComponentData(entity, surfaceComponent);
    }
}
