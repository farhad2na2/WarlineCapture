using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class UnitAttackTracePresentationSystemHelper : IUnitAttackTraceRenderer
{
    private const int MaxBatchSize = 1023;
    private const int MaxTraceOriginCount = 4;
    private const float TraceEndJitter = 0.25f;
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
    private float sourceForwardOffset = 0.45f;
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
        sourceForwardOffset = config.SourceForwardOffset;
        traceShader = config.TraceShader;
    }

    public void Dispose()
    {
        DisposeRenderResources();
    }

    private void DisposeRenderResources()
    {
        if (_traceMesh != null)
            Object.Destroy(_traceMesh);
        if (_traceMaterial != null)
            Object.Destroy(_traceMaterial);

        _traceMesh = null;
        _traceMaterial = null;
        _propertyBlock = null;
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

            Vector3 baseStart = (Vector3)sourcePosition + new Vector3(0f, sourceHeightOffset, 0f);
            Vector3 baseEnd = (Vector3)targetTransform.Position + new Vector3(0f, targetHeightOffset, 0f);
            UnitAttackTraceOriginPattern originPattern = em.HasComponent<UnitAttackTraceOriginPattern>(entity)
                ? em.GetComponentData<UnitAttackTraceOriginPattern>(entity)
                : default;
            int originCount = ResolveTraceOriginCount(originPattern);
            Vector3 sideRight = ResolveTraceSideRight(sourceTransform.Rotation, baseEnd - baseStart);
            for (int originIndex = 0; originIndex < originCount; originIndex++)
            {
                float sideSign = ResolveTraceSideSign(originIndex, originCount);
                Vector3 sourceSideOffset = sideRight * (sideSign * Mathf.Max(0f, originPattern.LateralOffset));
                Vector3 targetSideOffset = sideRight * (sideSign * Mathf.Max(0f, originPattern.TargetLateralOffset));
                QueueTraceInstance(
                    ref batchCount,
                    baseStart + sourceSideOffset,
                    baseEnd + targetSideOffset,
                    attack,
                    trace,
                    sideSign * 0.071f);
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

    private void QueueTraceInstance(
        ref int batchCount,
        Vector3 start,
        Vector3 end,
        UnitAttack attack,
        UnitAttackTraceComponent trace,
        float phaseOffset)
    {
        float phase = Mathf.Repeat(trace.Phase + phaseOffset, 1f);

        // Per-shot end-point jitter so consecutive shots aren't laser-locked
        // onto the exact same line. Deterministic from the shot's phase.
        Vector3 aim = end - start;
        if (aim.sqrMagnitude > 1e-4f)
        {
            Vector3 jitterRight = Vector3.Cross(Vector3.up, aim);
            if (jitterRight.sqrMagnitude > 1e-6f)
            {
                jitterRight.Normalize();
                float seedA = Mathf.Repeat(phase * 13.37f, 1f) * 2f - 1f;
                float seedB = Mathf.Repeat(phase * 7.91f, 1f) * 2f - 1f;
                end += jitterRight * (seedA * TraceEndJitter) + Vector3.up * (seedB * TraceEndJitter * 0.5f);
            }
        }

        Vector3 direction = end - start;
        float length = direction.magnitude;
        if (length <= 0.01f)
            return;

        direction /= length;

        // Start the tracer at the gun muzzle instead of the body center.
        float forwardOffset = Mathf.Min(sourceForwardOffset, length * 0.4f);
        if (forwardOffset > 0f)
        {
            start += direction * forwardOffset;
            length -= forwardOffset;
        }

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
            phase + (UnityEngine.Time.time * math.max(0.1f, attack.TraceScrollSpeed)),
            0f,
            0f);

        batchCount++;
        if (batchCount == MaxBatchSize)
        {
            DrawBatch(batchCount);
            batchCount = 0;
        }
    }

    private static int ResolveTraceOriginCount(UnitAttackTraceOriginPattern pattern)
    {
        if (pattern.OriginCount <= 1 || pattern.LateralOffset <= 0f)
            return 1;

        return Mathf.Clamp(pattern.OriginCount, 1, MaxTraceOriginCount);
    }

    private static float ResolveTraceSideSign(int index, int count)
    {
        if (count <= 1)
            return 0f;
        if (count == 2)
            return index == 0 ? -1f : 1f;

        return Mathf.Lerp(-1f, 1f, index / (float)(count - 1));
    }

    private static Vector3 ResolveTraceSideRight(quaternion sourceRotation, Vector3 aim)
    {
        Quaternion rotation = new(sourceRotation.value.x, sourceRotation.value.y, sourceRotation.value.z, sourceRotation.value.w);
        Vector3 right = rotation * Vector3.right;
        right.y = 0f;
        if (right.sqrMagnitude > 1e-5f)
            return right.normalized;

        Vector3 flatAim = aim;
        flatAim.y = 0f;
        if (flatAim.sqrMagnitude <= 1e-5f)
            return Vector3.right;

        return Vector3.Cross(Vector3.up, flatAim).normalized;
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
