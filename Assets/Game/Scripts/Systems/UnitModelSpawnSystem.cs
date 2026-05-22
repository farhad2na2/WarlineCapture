using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using SnivelerCode.GpuAnimation.Scripts.Components;

[UpdateBefore(typeof(UnitDestroyedVisualSystem))]
public partial struct UnitModelSpawnSystem : ISystem
{
    private static readonly bool EnableModelSpawnDiagnostics = false;
    private static readonly bool EnableModelSpawnFreezeLogs = false;
    private const double FreezeLogThresholdSeconds = 0.05d;
    private const int MaxModelSpawnsPerFrame = 4096;
    private const float ProxySoldierMidLodScale = 1.75f;
    private const int DiagnosticIntervalFrames = 120;

    private int _nextDiagnosticFrame;
    private EntityQuery _modelPrefabQuery;
    private EntityQuery _detailedVisualQuery;
    private EntityQuery _midLodPrefabQuery;
    private EntityQuery _lowLodPrefabQuery;
    private EntityQuery _modelInstanceQuery;
    private EntityQuery _midLodInstanceQuery;
    private EntityQuery _lowLodInstanceQuery;
    private EntityQuery _deferredVisibleCharacterLodQuery;
    private EntityQuery _cameraReferenceQuery;
    private int _lastLoggedTotalVisualUnits;
    private int _lastLoggedDetailReady;
    private int _lastLoggedMidReady;
    private int _lastLoggedLowReady;
    private int _lastLoggedDeferredVisibleLod;

    public void OnCreate(ref SystemState state)
    {
        _modelPrefabQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitModelPrefabReference>());
        _detailedVisualQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitDetailedVisualReference>());
        _midLodPrefabQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitMidLodPrefabReference>());
        _lowLodPrefabQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitLowLodPrefabReference>());
        _modelInstanceQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitModelInstanceReference>());
        _midLodInstanceQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitMidLodInstanceReference>());
        _lowLodInstanceQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitLowLodInstanceReference>());
        _deferredVisibleCharacterLodQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitVisibleCharacterLodSpawnDeferredTag>());
        _cameraReferenceQuery = state.GetEntityQuery(ComponentType.ReadOnly<RuntimeCameraReferenceComponent>());
        _lastLoggedTotalVisualUnits = -1;
        _lastLoggedDetailReady = -1;
        _lastLoggedMidReady = -1;
        _lastLoggedLowReady = -1;
        _lastLoggedDeferredVisibleLod = -1;
    }

    public void OnUpdate(ref SystemState state)
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
        int spawnedCount = 0;
        int spawnedMidCount = 0;
        int spawnedSafeMidCount = 0;
        int spawnedAuthoredSafeMidCount = 0;
        int spawnedProxyMidCount = 0;
        int spawnedLowCount = 0;
        int spawnedSafeLowCount = 0;
        int spawnedCompleteVisualSetCount = 0;
        int spawnedVisibleCompleteVisualSetCount = 0;
        int spawnedDetailOnlyVisibleCharacterCount = 0;
        int skippedVisibleCharacterLodCount = 0;
        int deferredVisibleCharacterLodCount = 0;
        int releasedVisibleCharacterLodCount = 0;
        string midPrefabSamples = string.Empty;
        EntityManager em = state.EntityManager;
        RuntimeCameraReferenceSystem.TryGetWorldCamera(em, _cameraReferenceQuery, out Camera camera);
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        using NativeHashSet<Entity> deferredThisFrame = new(256, Allocator.Temp);

        foreach (var (_, entity) in SystemAPI
                 .Query<RefRO<UnitVisibleCharacterLodSpawnDeferredTag>>()
                 .WithEntityAccess())
        {
            if (!IsVisibleCharacter(em, entity, camera))
            {
                ecb.RemoveComponent<UnitVisibleCharacterLodSpawnDeferredTag>(entity);
                releasedVisibleCharacterLodCount++;
            }
        }

        foreach (var (modelPrefab, entity) in SystemAPI
                 .Query<RefRO<UnitModelPrefabReference>>()
                 .WithNone<UnitModelInstanceReference>()
                 .WithNone<MissionRuntimeSpritePresenterSuppressesLegacyModelTag>()
                 .WithEntityAccess())
        {
            if (modelPrefab.ValueRO.Prefab == Entity.Null)
                continue;
            if (spawnedCount >= MaxModelSpawnsPerFrame)
                break;

            Entity modelInstance = ecb.Instantiate(modelPrefab.ValueRO.Prefab);
            spawnedCount++;
            ecb.AddComponent(entity, new UnitModelInstanceReference { Instance = modelInstance });
            ecb.AddComponent<UnitRenderSafetyPatchedTag>(modelInstance);
            ecb.AddComponent(modelInstance, new Parent { Value = entity });
            LocalTransform modelTransform = LocalTransform.Identity;
            if (SystemAPI.HasComponent<UnitModelLocalTransform>(entity))
            {
                UnitModelLocalTransform local = SystemAPI.GetComponent<UnitModelLocalTransform>(entity);
                modelTransform = LocalTransform.FromPositionRotationScale(local.Position, local.Rotation, local.Scale);
            }
            ecb.SetComponent(modelInstance, modelTransform);
            if (SystemAPI.HasComponent<UnitResolvedAnimationIndex>(entity) &&
                SystemAPI.HasComponent<MaterialAnimationIndex>(modelPrefab.ValueRO.Prefab))
            {
                byte animationIndex = SystemAPI.GetComponent<UnitResolvedAnimationIndex>(entity).Value;
                if (animationIndex != byte.MaxValue)
                    ecb.SetComponent(modelInstance, new MaterialAnimationIndex { Value = animationIndex });
            }

            if (SystemAPI.HasComponent<UnitDestroyedVisualReference>(entity))
            {
                UnitDestroyedVisualReference visualRef = SystemAPI.GetComponent<UnitDestroyedVisualReference>(entity);
                visualRef.AliveVisual = modelInstance;
                ecb.SetComponent(entity, visualRef);
            }

            bool visibleCharacter = IsVisibleCharacter(em, entity, camera);
            bool spawnedMid = TrySpawnMidLod(
                ref ecb,
                ref spawnedCount,
                em,
                entity,
                ref spawnedMidCount,
                ref spawnedSafeMidCount,
                ref spawnedAuthoredSafeMidCount,
                ref spawnedProxyMidCount,
                ref midPrefabSamples);
            bool spawnedLow = TrySpawnLowLod(
                ref ecb,
                ref spawnedCount,
                em,
                entity,
                ref spawnedLowCount,
                ref spawnedSafeLowCount);
            if (spawnedMid || spawnedLow)
            {
                spawnedCompleteVisualSetCount++;
                if (visibleCharacter)
                    spawnedVisibleCompleteVisualSetCount++;
            }
            else if (visibleCharacter)
            {
                spawnedDetailOnlyVisibleCharacterCount++;
            }
        }

        foreach (var (modelPrefab, entity) in SystemAPI
                 .Query<RefRO<UnitMidLodPrefabReference>>()
                 .WithNone<UnitMidLodInstanceReference>()
                 .WithNone<MissionRuntimeSpritePresenterSuppressesLegacyModelTag>()
                 .WithEntityAccess())
        {
            if (modelPrefab.ValueRO.Prefab == Entity.Null)
                continue;
            if (spawnedCount >= MaxModelSpawnsPerFrame)
                break;
            if (!HasDetailedVisualReady(em, entity))
                continue;

            TrySpawnMidLod(
                ref ecb,
                ref spawnedCount,
                em,
                entity,
                ref spawnedMidCount,
                ref spawnedSafeMidCount,
                ref spawnedAuthoredSafeMidCount,
                ref spawnedProxyMidCount,
                ref midPrefabSamples);
        }

        foreach (var (modelPrefab, entity) in SystemAPI
                 .Query<RefRO<UnitLowLodPrefabReference>>()
                 .WithNone<UnitLowLodInstanceReference>()
                 .WithNone<MissionRuntimeSpritePresenterSuppressesLegacyModelTag>()
                 .WithEntityAccess())
        {
            if (modelPrefab.ValueRO.Prefab == Entity.Null)
                continue;
            if (spawnedCount >= MaxModelSpawnsPerFrame)
                break;
            if (!HasDetailedVisualReady(em, entity))
                continue;

            TrySpawnLowLod(
                ref ecb,
                ref spawnedCount,
                em,
                entity,
                ref spawnedLowCount,
                ref spawnedSafeLowCount);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
        int authoredDetailReady = _detailedVisualQuery.CalculateEntityCount();
        int totalVisualUnits = math.max(_modelPrefabQuery.CalculateEntityCount(), authoredDetailReady);
        int expectedMid = _midLodPrefabQuery.CalculateEntityCount();
        int expectedLow = _lowLodPrefabQuery.CalculateEntityCount();
        int detailReady = math.max(_modelInstanceQuery.CalculateEntityCount(), authoredDetailReady);
        int midReady = _midLodInstanceQuery.CalculateEntityCount();
        int lowReady = _lowLodInstanceQuery.CalculateEntityCount();
        int deferredReady = _deferredVisibleCharacterLodQuery.CalculateEntityCount();
        bool readinessChanged =
            totalVisualUnits != _lastLoggedTotalVisualUnits ||
            detailReady != _lastLoggedDetailReady ||
            midReady != _lastLoggedMidReady ||
            lowReady != _lastLoggedLowReady ||
            deferredReady != _lastLoggedDeferredVisibleLod;
        bool readinessIncomplete =
            totalVisualUnits > 0 &&
            (detailReady < totalVisualUnits ||
             midReady < expectedMid ||
             lowReady < expectedLow ||
             deferredReady > 0);
        bool shouldLogSkipped = skippedVisibleCharacterLodCount > 0 && Time.frameCount >= _nextDiagnosticFrame;
        bool shouldLogReadiness =
            totalVisualUnits > 0 &&
            (_lastLoggedTotalVisualUnits < 0 ||
             (readinessIncomplete && Time.frameCount >= _nextDiagnosticFrame) ||
             (!readinessIncomplete && readinessChanged));
        if (shouldLogSkipped || shouldLogReadiness)
            _nextDiagnosticFrame = Time.frameCount + DiagnosticIntervalFrames;
        if (EnableModelSpawnDiagnostics && (spawnedMidCount > 0 || spawnedLowCount > 0 || shouldLogSkipped || shouldLogReadiness || releasedVisibleCharacterLodCount > 0 || spawnedCompleteVisualSetCount > 0 || spawnedDetailOnlyVisibleCharacterCount > 0))
        {
            _lastLoggedTotalVisualUnits = totalVisualUnits;
            _lastLoggedDetailReady = detailReady;
            _lastLoggedMidReady = midReady;
            _lastLoggedLowReady = lowReady;
            _lastLoggedDeferredVisibleLod = deferredReady;
            Debug.Log($"[UnitModelSpawnDiag] frame={Time.frameCount} totalVisualUnits={totalVisualUnits} authoredDetailReady={authoredDetailReady} detailReady={detailReady}/{totalVisualUnits} midReady={midReady}/{expectedMid} lowReady={lowReady}/{expectedLow} deferredVisibleLod={deferredReady} ready={(readinessIncomplete ? 0 : 1)} detailSpawned={spawnedCount - spawnedMidCount - spawnedLowCount} completeVisualSets={spawnedCompleteVisualSetCount} visibleCompleteVisualSets={spawnedVisibleCompleteVisualSetCount} detailOnlyVisibleCharacters={spawnedDetailOnlyVisibleCharacterCount} midSpawned={spawnedMidCount} safeMidSpawned={spawnedSafeMidCount} authoredSafeMidSpawned={spawnedAuthoredSafeMidCount} proxyMidSpawned={spawnedProxyMidCount} lowSpawned={spawnedLowCount} safeLowSpawned={spawnedSafeLowCount} skippedVisibleCharacterLod={skippedVisibleCharacterLodCount} deferredVisibleCharacterLod={deferredVisibleCharacterLodCount} releasedVisibleCharacterLod={releasedVisibleCharacterLodCount} proxyMidScale={ProxySoldierMidLodScale:F2} midPrefabs={midPrefabSamples}");
        }
        if (EnableModelSpawnFreezeLogs && elapsed >= FreezeLogThresholdSeconds)
            Debug.Log($"[FreezeDetect:ECS] UnitModelSpawnSystem frame={Time.frameCount} {(elapsed * 1000d):F1}ms spawned={spawnedCount}");
    }

    private static string GetEntityName(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return "null";

        return em.GetName(entity).ToString();
    }

    private static string AppendSample(string samples, string value)
    {
        if (string.IsNullOrEmpty(value))
            value = "empty";

        return string.IsNullOrEmpty(samples)
            ? value
            : samples + "|" + value;
    }

    private static bool TrySpawnMidLod(
        ref EntityCommandBuffer ecb,
        ref int spawnedCount,
        EntityManager em,
        Entity entity,
        ref int spawnedMidCount,
        ref int spawnedSafeMidCount,
        ref int spawnedAuthoredSafeMidCount,
        ref int spawnedProxyMidCount,
        ref string midPrefabSamples)
    {
        if (!em.Exists(entity) ||
            !em.HasComponent<UnitMidLodPrefabReference>(entity) ||
            em.HasComponent<UnitMidLodInstanceReference>(entity))
        {
            return false;
        }

        Entity prefab = em.GetComponentData<UnitMidLodPrefabReference>(entity).Prefab;
        if (prefab == Entity.Null)
            return false;

        Entity modelInstance = ecb.Instantiate(prefab);
        spawnedCount++;
        spawnedMidCount++;
        ecb.AddComponent(entity, new UnitMidLodInstanceReference { Instance = modelInstance });
        ecb.AddComponent(modelInstance, new UnitMidLodRenderRootTag());
        string midPrefabName = GetEntityName(em, prefab);
        if (midPrefabSamples.Length < 240)
            midPrefabSamples = AppendSample(midPrefabSamples, midPrefabName);
        bool isAuthoredSafeMid = em.HasComponent<UnitSafeVisibleCharacterLodTag>(prefab);
        bool isProxySoldierLod = IsProxySoldierMidLodPrefab(midPrefabName);
        bool usesSafeVisibleLod = isAuthoredSafeMid || isProxySoldierLod;
        if (usesSafeVisibleLod)
        {
            if (!em.HasComponent<UnitUsesSafeVisibleCharacterLodTag>(entity))
                ecb.AddComponent<UnitUsesSafeVisibleCharacterLodTag>(entity);
            ecb.AddComponent<UnitSafeVisibleCharacterLodTag>(modelInstance);
            spawnedSafeMidCount++;
        }
        if (isAuthoredSafeMid)
            spawnedAuthoredSafeMidCount++;
        if (isProxySoldierLod)
            spawnedProxyMidCount++;

        ConfigureSpawnedLodVisual(ref ecb, em, entity, prefab, modelInstance, isProxySoldierLod);
        return true;
    }

    private static bool HasDetailedVisualReady(EntityManager em, Entity entity)
    {
        return em.Exists(entity) &&
               (em.HasComponent<UnitModelInstanceReference>(entity) ||
                em.HasComponent<UnitDetailedVisualReference>(entity));
    }

    private static bool TrySpawnLowLod(
        ref EntityCommandBuffer ecb,
        ref int spawnedCount,
        EntityManager em,
        Entity entity,
        ref int spawnedLowCount,
        ref int spawnedSafeLowCount)
    {
        if (!em.Exists(entity) ||
            !em.HasComponent<UnitLowLodPrefabReference>(entity) ||
            em.HasComponent<UnitLowLodInstanceReference>(entity))
        {
            return false;
        }

        Entity prefab = em.GetComponentData<UnitLowLodPrefabReference>(entity).Prefab;
        if (prefab == Entity.Null)
            return false;

        Entity modelInstance = ecb.Instantiate(prefab);
        spawnedCount++;
        spawnedLowCount++;
        ecb.AddComponent(entity, new UnitLowLodInstanceReference { Instance = modelInstance });
        if (em.HasComponent<UnitSafeVisibleCharacterLodTag>(prefab))
        {
            ecb.AddComponent<UnitSafeVisibleCharacterLodTag>(modelInstance);
            spawnedSafeLowCount++;
        }

        ConfigureSpawnedLodVisual(ref ecb, em, entity, prefab, modelInstance, false);
        return true;
    }

    private static void ConfigureSpawnedLodVisual(
        ref EntityCommandBuffer ecb,
        EntityManager em,
        Entity entity,
        Entity prefab,
        Entity modelInstance,
        bool scaleProxySoldier)
    {
        ecb.AddComponent(modelInstance, new UnitRenderBudgetCulledTag());
        ecb.AddComponent<UnitRenderSafetyPatchedTag>(modelInstance);
        ecb.AddComponent<DisableRendering>(modelInstance);
        ecb.AddComponent(modelInstance, new Parent { Value = entity });
        LocalTransform modelTransform = LocalTransform.Identity;
        if (em.HasComponent<UnitModelLocalTransform>(entity))
        {
            UnitModelLocalTransform local = em.GetComponentData<UnitModelLocalTransform>(entity);
            modelTransform = LocalTransform.FromPositionRotationScale(local.Position, local.Rotation, local.Scale);
        }
        if (scaleProxySoldier)
            modelTransform.Scale *= ProxySoldierMidLodScale;

        ecb.SetComponent(modelInstance, modelTransform);
        if (em.HasComponent<UnitResolvedAnimationIndex>(entity) &&
            em.HasComponent<MaterialAnimationIndex>(prefab))
        {
            byte animationIndex = em.GetComponentData<UnitResolvedAnimationIndex>(entity).Value;
            if (animationIndex != byte.MaxValue)
                ecb.SetComponent(modelInstance, new MaterialAnimationIndex { Value = animationIndex });
        }
    }

    private static bool IsProxySoldierMidLodPrefab(string prefabName)
    {
        return !string.IsNullOrEmpty(prefabName) &&
               prefabName.StartsWith("ProxyLOD_Unit_Chr_Soldier_Male_02_Alt_04", System.StringComparison.Ordinal);
    }

    private static bool IsVisibleCharacter(EntityManager em, Entity entity, Camera camera)
    {
        if (camera == null ||
            !em.Exists(entity) ||
            !em.HasComponent<UnitSourcePrefabKey>(entity) ||
            !em.HasComponent<LocalTransform>(entity))
        {
            return false;
        }

        var key = em.GetComponentData<UnitSourcePrefabKey>(entity).Value;
        if (!key.ToString().StartsWith("Unit_Chr_", System.StringComparison.Ordinal))
            return false;

        float3 position = em.GetComponentData<LocalTransform>(entity).Position;
        Vector3 viewportPosition = camera.WorldToViewportPoint(new Vector3(position.x, position.y, position.z));
        return viewportPosition.z > 0f &&
               viewportPosition.x >= -0.05f && viewportPosition.x <= 1.05f &&
               viewportPosition.y >= -0.05f && viewportPosition.y <= 1.05f;
    }
}
