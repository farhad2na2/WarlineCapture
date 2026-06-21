using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed partial class RuntimeCameraReferenceSystem : SystemBase
{
    private Entity _snapshotEntity;

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
        WorldCamera = null;
        _snapshotEntity = Entity.Null;
    }

    public void SetWorldCamera(Camera camera)
    {
        WorldCamera = camera;
        PublishSnapshot();
    }

    public void ClearWorldCamera()
    {
        WorldCamera = null;
        PublishInvalidSnapshot();
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

    public static bool TryGetCameraSnapshot(World world, out RuntimeCameraSnapshotComponent snapshot)
    {
        snapshot = default;
        if (world == null || !world.IsCreated)
            return false;

        RuntimeCameraReferenceSystem referenceSystem = world.GetExistingSystemManaged<RuntimeCameraReferenceSystem>();
        if (referenceSystem == null)
            return false;

        referenceSystem.PublishSnapshot();
        return referenceSystem.TryReadSnapshot(out snapshot) && snapshot.IsValid != 0;
    }

    private bool TryReadSnapshot(out RuntimeCameraSnapshotComponent snapshot)
    {
        snapshot = default;
        if (_snapshotEntity == Entity.Null || World == null || !World.IsCreated)
            return false;

        EntityManager em = EntityManager;
        if (!em.Exists(_snapshotEntity) || !em.HasComponent<RuntimeCameraSnapshotComponent>(_snapshotEntity))
            return false;

        snapshot = em.GetComponentData<RuntimeCameraSnapshotComponent>(_snapshotEntity);
        return true;
    }

    private void PublishSnapshot()
    {
        if (WorldCamera == null || World == null || !World.IsCreated)
        {
            PublishInvalidSnapshot();
            return;
        }

        Entity entity = EnsureSnapshotEntity();
        if (entity == Entity.Null)
            return;

        float4x4 worldToCamera = ToFloat4x4(WorldCamera.worldToCameraMatrix);
        float4x4 projection = ToFloat4x4(WorldCamera.projectionMatrix);
        EntityManager.SetComponentData(entity, new RuntimeCameraSnapshotComponent
        {
            IsValid = 1,
            Position = WorldCamera.transform.position,
            Rotation = WorldCamera.transform.rotation,
            WorldToCamera = worldToCamera,
            Projection = projection,
            ViewProjection = math.mul(projection, worldToCamera)
        });
    }

    private void PublishInvalidSnapshot()
    {
        if (World == null || !World.IsCreated)
            return;

        Entity entity = EnsureSnapshotEntity();
        if (entity != Entity.Null)
            EntityManager.SetComponentData(entity, default(RuntimeCameraSnapshotComponent));
    }

    private Entity EnsureSnapshotEntity()
    {
        if (World == null || !World.IsCreated)
            return Entity.Null;

        EntityManager em = EntityManager;
        if (_snapshotEntity != Entity.Null &&
            em.Exists(_snapshotEntity) &&
            em.HasComponent<RuntimeCameraSnapshotComponent>(_snapshotEntity))
        {
            return _snapshotEntity;
        }

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<RuntimeCameraSnapshotComponent>());
        if (!query.IsEmptyIgnoreFilter)
        {
            _snapshotEntity = query.GetSingletonEntity();
            return _snapshotEntity;
        }

        _snapshotEntity = em.CreateEntity(typeof(RuntimeCameraSnapshotComponent));
        em.SetName(_snapshotEntity, "RuntimeCameraSnapshot");
        return _snapshotEntity;
    }

    private static float4x4 ToFloat4x4(Matrix4x4 value)
    {
        return new float4x4(
            new float4(value.m00, value.m10, value.m20, value.m30),
            new float4(value.m01, value.m11, value.m21, value.m31),
            new float4(value.m02, value.m12, value.m22, value.m32),
            new float4(value.m03, value.m13, value.m23, value.m33));
    }
}
