using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class UnitAttackTraceSystem : IUnitAttackTraceRenderer
{
    private const int MaxBatchSize = 1023;
    private static readonly int TraceColorId = Shader.PropertyToID("_TraceColor");
    private static readonly int TraceParamsId = Shader.PropertyToID("_TraceParams");
    private static readonly Vector3[] TraceVertices =
    {
        new(-0.5f, 0f, 0f),
        new(0.5f, 0f, 0f),
        new(-0.5f, 0f, 1f),
        new(0.5f, 0f, 1f)
    };
    private static readonly Vector2[] TraceUvs =
    {
        new(0f, 0f),
        new(1f, 0f),
        new(0f, 1f),
        new(1f, 1f)
    };
    private static readonly int[] TraceTriangles = { 0, 2, 1, 2, 3, 1 };

    private UnitAttackTraceSystemConfig config;
    private Camera worldCamera;
    private float sourceHeightOffset = 0.9f;
    private float targetHeightOffset = 0.9f;
    private Shader traceShader;
    private int _renderLayer;

    private Mesh _traceMesh;
    private Material _traceMaterial;
    private MaterialPropertyBlock _propertyBlock;
    private EntityQuery _traceQuery;
    private World _cachedWorld;
    private readonly Matrix4x4[] _matrices = new Matrix4x4[MaxBatchSize];
    private readonly Vector4[] _colors = new Vector4[MaxBatchSize];
    private readonly Vector4[] _traceParams = new Vector4[MaxBatchSize];
    public void Init(UnitAttackTraceSystemConfig configAsset, Camera sceneWorldCamera, int renderLayer)
    {
        config = configAsset;
        worldCamera = sceneWorldCamera;
        _renderLayer = renderLayer;
        ApplyConfigIfAvailable();

        EnsureRenderResources();
    }

    private void ApplyConfigIfAvailable()
    {
        if (config == null)
            return;

        if (config.WorldCamera != null)
            worldCamera = config.WorldCamera;
        sourceHeightOffset = config.SourceHeightOffset;
        targetHeightOffset = config.TargetHeightOffset;
        traceShader = config.TraceShader;
    }

    public void Dispose()
    {
        if (_traceMesh != null)
            Object.Destroy(_traceMesh);
        if (_traceMaterial != null)
            Object.Destroy(_traceMaterial);
    }

    public void LateUpdate()
    {
        if (worldCamera == null)
            return;

        if (!EnsureRenderResources() || !EnsureQuery(out var em))
            return;

        using var entities = _traceQuery.ToEntityArray(Allocator.Temp);
        int batchCount = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            UnitAttackTraceComponent trace = em.GetComponentData<UnitAttackTraceComponent>(entity);
            if (trace.TimeRemaining <= 0f)
                continue;

            EngageTarget engage = em.GetComponentData<EngageTarget>(entity);
            if (engage.Target == Entity.Null || !em.Exists(engage.Target) || !em.HasComponent<LocalTransform>(engage.Target))
                continue;

            LocalTransform sourceTransform = em.GetComponentData<LocalTransform>(entity);
            LocalTransform targetTransform = em.GetComponentData<LocalTransform>(engage.Target);
            UnitAttack attack = em.GetComponentData<UnitAttack>(entity);
            float3 sourcePosition = sourceTransform.Position;
            if (em.HasComponent<UnitTurretReference>(entity))
            {
                UnitTurretReference turretRef = em.GetComponentData<UnitTurretReference>(entity);
                if (em.Exists(turretRef.Turret) && em.HasComponent<LocalToWorld>(turretRef.Turret))
                    sourcePosition = em.GetComponentData<LocalToWorld>(turretRef.Turret).Position;
            }

            Vector3 start = (Vector3)sourcePosition + new Vector3(0f, sourceHeightOffset, 0f);
            Vector3 end = (Vector3)targetTransform.Position + new Vector3(0f, targetHeightOffset, 0f);
            Vector3 direction = end - start;
            float length = direction.magnitude;
            if (length <= 0.01f)
                continue;

            direction /= length;
            Vector3 cameraForward = worldCamera.transform.forward;
            Vector3 right = Vector3.Cross(cameraForward, direction);
            if (right.sqrMagnitude <= 0.0001f)
                right = Vector3.Cross(Vector3.up, direction);
            right.Normalize();
            Vector3 up = Vector3.Cross(direction, right).normalized;

            Quaternion rotation = Quaternion.LookRotation(direction, up);
            _matrices[batchCount] = Matrix4x4.TRS(start, rotation, new Vector3(math.max(0.01f, attack.TraceWidth), 1f, length));
            _colors[batchCount] = attack.TraceColor;
            _traceParams[batchCount] = new Vector4(
                math.max(1f, attack.TraceDashDensity),
                trace.Phase + (Time.time * math.max(0.1f, attack.TraceScrollSpeed)),
                0f,
                0f);

            batchCount++;
            if (batchCount == MaxBatchSize)
            {
                DrawBatch(batchCount);
                batchCount = 0;
            }
        }

        if (batchCount > 0)
            DrawBatch(batchCount);
    }

    private bool EnsureQuery(out EntityManager entityManager)
    {
        entityManager = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        if (_cachedWorld != world)
        {
            _cachedWorld = world;
            _traceQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UnitAttackTraceComponent>(),
                ComponentType.ReadOnly<UnitAttack>(),
                ComponentType.ReadOnly<EngageTarget>(),
                ComponentType.ReadOnly<LocalTransform>());
        }

        entityManager = world.EntityManager;
        return true;
    }

    private bool EnsureRenderResources()
    {
        if (_traceMesh == null)
            _traceMesh = BuildTraceMesh();

        if (traceShader == null)
            traceShader = Shader.Find("Game/AttackTraceInstanced");
        if (traceShader == null)
            return false;

        if (_traceMaterial == null)
        {
            _traceMaterial = new Material(traceShader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                enableInstancing = true
            };
        }

        _propertyBlock ??= new MaterialPropertyBlock();
        return true;
    }

    private void DrawBatch(int count)
    {
        _propertyBlock.Clear();
        _propertyBlock.SetVectorArray(TraceColorId, _colors);
        _propertyBlock.SetVectorArray(TraceParamsId, _traceParams);
        Graphics.DrawMeshInstanced(
            _traceMesh,
            0,
            _traceMaterial,
            _matrices,
            count,
            _propertyBlock,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            false,
            _renderLayer,
            worldCamera);
    }

    private static Mesh BuildTraceMesh()
    {
        var mesh = new Mesh
        {
            name = "UnitAttackTraceQuad"
        };

        mesh.vertices = TraceVertices;
        mesh.uv = TraceUvs;
        mesh.triangles = TraceTriangles;
        mesh.RecalculateBounds();
        return mesh;
    }
}
