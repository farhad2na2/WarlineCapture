using Unity.Entities;
using UnityEngine;

public sealed partial class RuntimeCameraReferenceSystem : SystemBase
{
    public Camera WorldCamera { get; private set; }

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnDestroy()
    {
        ClearWorldCamera();
    }

    public void SetWorldCamera(Camera camera)
    {
        WorldCamera = camera;
    }

    public void ClearWorldCamera()
    {
        WorldCamera = null;
    }

    public static bool TryGetWorldCamera(EntityManager entityManager, out Camera camera)
    {
        return TryGetWorldCamera(entityManager.World, out camera);
    }

    public static bool TryGetWorldCamera(World world, out Camera camera)
    {
        camera = null;
        if (world == null || !world.IsCreated)
            return false;

        RuntimeCameraReferenceSystem referenceSystem = world.GetExistingSystemManaged<RuntimeCameraReferenceSystem>();
        if (referenceSystem == null || referenceSystem.WorldCamera == null)
            return false;

        camera = referenceSystem.WorldCamera;
        return camera != null;
    }
}
