using Unity.Entities;
using UnityEngine;

[UpdateAfter(typeof(M01LegacyEcsRenderingSuppressionSystem))]
public partial class MissionRuntimeTerrainSurfaceRendererSystem : SystemBase
{
    protected override void OnUpdate()
    {
        RefreshTerrainSurfaceRenderers(EntityManager);
    }

    public static void RefreshTerrainSurfaceRenderers(EntityManager em)
    {
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
            ResolveGroundPosition(surface, runtime),
            surface.RuntimePlane == (byte)TacticalMapRuntimePlane.GameplayXZ
                ? Quaternion.Euler(90f, 0f, 0f)
                : Quaternion.identity);
        Vector3 groundScale = ResolveGroundScale(runtime);
        runtime.Instance.transform.localScale = groundScale == Vector3.zero ? Vector3.one : groundScale;
        runtime.Renderer.sprite = runtime.GroundSprite;
        runtime.Renderer.sortingOrder = 0;
        runtime.Renderer.enabled = runtime.GroundSprite != null;
    }

    private static Vector3 ResolveGroundPosition(in MissionRuntimeTerrainSurface surface, MissionRuntimeTerrainSurfaceRendererRuntime runtime)
    {
        if (runtime.GroundFollowsCamera && runtime.GroundCamera != null)
        {
            Transform cameraTransform = runtime.GroundCamera.transform;
            return surface.RuntimePlane == (byte)TacticalMapRuntimePlane.GameplayXZ
                ? new Vector3(cameraTransform.position.x, 0f, cameraTransform.position.z)
                : new Vector3(cameraTransform.position.x, cameraTransform.position.y, 0f);
        }

        return runtime.GroundPosition;
    }

    private static Vector3 ResolveGroundScale(MissionRuntimeTerrainSurfaceRendererRuntime runtime)
    {
        if (runtime.GroundFollowsCamera &&
            runtime.GroundCamera != null &&
            runtime.GroundCamera.orthographic &&
            runtime.GroundCamera.aspect > 0.0001f &&
            runtime.GroundSprite != null)
        {
            Vector2 spriteWorldSize = runtime.GroundSprite.bounds.size;
            if (spriteWorldSize.x > 0.0001f && spriteWorldSize.y > 0.0001f)
            {
                return new Vector3(
                    runtime.GroundCamera.orthographicSize * 2f * runtime.GroundCamera.aspect / spriteWorldSize.x,
                    runtime.GroundCamera.orthographicSize * 2f / spriteWorldSize.y,
                    1f);
            }
        }

        return runtime.GroundScale;
    }
}
