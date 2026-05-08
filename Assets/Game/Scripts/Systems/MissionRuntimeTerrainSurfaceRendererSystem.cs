using Unity.Entities;
using UnityEngine;

[UpdateAfter(typeof(M01LegacyEcsRenderingSuppressionSystem))]
public partial class MissionRuntimeTerrainSurfaceRendererSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityManager em = EntityManager;
        EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<MissionRuntimeTerrainSurface>(),
            ComponentType.ReadOnly<MissionRuntimeTerrainSurfaceRendererRuntime>());
        using Unity.Collections.NativeArray<Entity> entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity))
                continue;

            MissionRuntimeTerrainSurface surface = em.GetComponentData<MissionRuntimeTerrainSurface>(entity);
            MissionRuntimeTerrainSurfaceRendererRuntime runtime = em.GetComponentObject<MissionRuntimeTerrainSurfaceRendererRuntime>(entity);
            UpdateRenderer(surface, runtime);
        }
    }

    private static void UpdateRenderer(in MissionRuntimeTerrainSurface surface, MissionRuntimeTerrainSurfaceRendererRuntime runtime)
    {
        if (runtime == null || runtime.Instance == null || runtime.Renderer == null)
            return;

        runtime.Instance.transform.SetPositionAndRotation(
            Vector3.zero,
            surface.RuntimePlane == (byte)TacticalMapRuntimePlane.GameplayXZ
                ? Quaternion.Euler(90f, 0f, 0f)
                : Quaternion.identity);
        runtime.Instance.transform.localScale = Vector3.one;
        runtime.Renderer.sprite = runtime.GroundSprite;
        runtime.Renderer.sortingOrder = 0;
        runtime.Renderer.enabled = runtime.GroundSprite != null;
    }
}
