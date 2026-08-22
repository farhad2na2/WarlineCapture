using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using SnivelerCode.GpuAnimation.Scripts.Components;
using Game.Components;

namespace Game.Rendering
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
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
        private EntityQuery _pendingVisualSpawnQuery;
        private EntityQuery _lodRootsNeedingInitialHideQuery;
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
            _pendingVisualSpawnQuery = state.GetEntityQuery(
                new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<UnitModelPrefabReference>() },
                    None = new[] { ComponentType.ReadOnly<UnitModelInstanceReference>() }
                },
                new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<UnitMidLodPrefabReference>() },
                    None = new[] { ComponentType.ReadOnly<UnitMidLodInstanceReference>() }
                },
                new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<UnitLowLodPrefabReference>() },
                    None = new[] { ComponentType.ReadOnly<UnitLowLodInstanceReference>() }
                },
                new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<UnitVisibleCharacterLodSpawnDeferredTag>() }
                });
            _lodRootsNeedingInitialHideQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<UnitRenderBudgetCulledTag>() },
                Any = new[]
                {
                    ComponentType.ReadOnly<UnitMidLodRenderRootTag>(),
                    ComponentType.ReadOnly<UnitLowLodRenderRootTag>()
                },
                None = new[] { ComponentType.ReadOnly<UnitRenderBudgetLodHierarchyHiddenTag>() }
            });
            state.RequireForUpdate(_pendingVisualSpawnQuery);
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
            bool hasCameraSnapshot = RuntimeCameraReferenceSystem.TryGetCameraSnapshot(state.World, out RuntimeCameraSnapshotComponent camera);
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            using NativeHashSet<Entity> deferredThisFrame = new(256, Allocator.Temp);

            foreach (var (_, entity) in SystemAPI
                     .Query<RefRO<UnitVisibleCharacterLodSpawnDeferredTag>>()
                     .WithEntityAccess())
            {
                if (!hasCameraSnapshot || !IsVisibleCharacter(em, entity, camera))
                {
                    ecb.RemoveComponent<UnitVisibleCharacterLodSpawnDeferredTag>(entity);
                    releasedVisibleCharacterLodCount++;
                }
            }

            foreach (var (modelPrefab, entity) in SystemAPI
                     .Query<RefRO<UnitModelPrefabReference>>()
                     .WithNone<UnitModelInstanceReference>()
                     .WithEntityAccess())
            {
                if (SystemAPI.HasComponent<UnitDetailedVisualReference>(entity))
                {
                    ecb.RemoveComponent<UnitModelPrefabReference>(entity);
                    continue;
                }
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

                bool visibleCharacter = hasCameraSnapshot && IsVisibleCharacter(em, entity, camera);
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

            HideInitiallyCulledLodHierarchies(state.EntityManager, _lodRootsNeedingInitialHideQuery);

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
            ecb.AddComponent(modelInstance, new UnitLowLodRenderRootTag());
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

        internal static void HideInitiallyCulledLodHierarchies(EntityManager em, EntityQuery query)
        {
            if (query.IsEmptyIgnoreFilter)
                return;

            using NativeArray<Entity> roots = query.ToEntityArray(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            for (int i = 0; i < roots.Length; i++)
            {
                Entity root = roots[i];
                if (!em.Exists(root))
                    continue;

                HideCulledRenderableTree(em, ref ecb, root);
                if (!em.HasComponent<UnitRenderBudgetLodHierarchyHiddenTag>(root))
                    ecb.AddComponent<UnitRenderBudgetLodHierarchyHiddenTag>(root);
            }

            ecb.Playback(em);
            ecb.Dispose();
        }

        internal static void HideCulledRenderableTree(EntityManager em, ref EntityCommandBuffer ecb, Entity entity)
        {
            if (entity == Entity.Null || !em.Exists(entity))
                return;

            if (!em.HasComponent<DisableRendering>(entity))
                ecb.AddComponent<DisableRendering>(entity);
            if (!em.HasComponent<UnitRenderBudgetCulledTag>(entity))
                ecb.AddComponent<UnitRenderBudgetCulledTag>(entity);

            if (!em.HasBuffer<Child>(entity))
                return;

            DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
            for (int i = 0; i < children.Length; i++)
                HideCulledRenderableTree(em, ref ecb, children[i].Value);
        }

        private static bool IsProxySoldierMidLodPrefab(string prefabName)
        {
            return !string.IsNullOrEmpty(prefabName) &&
                   prefabName.StartsWith("ProxyLOD_Unit_Chr_Soldier_Male_02_Alt_04", System.StringComparison.Ordinal);
        }

        private static bool IsVisibleCharacter(EntityManager em, Entity entity, RuntimeCameraSnapshotComponent camera)
        {
            if (camera.IsValid == 0 ||
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
            float4 worldPosition = new(position, 1f);
            float4 cameraPosition = math.mul(camera.WorldToCamera, worldPosition);
            float4 clipPosition = math.mul(camera.ViewProjection, worldPosition);
            float invW = math.abs(clipPosition.w) > 0.000001f ? 1f / clipPosition.w : 0f;
            float viewportX = clipPosition.x * invW * 0.5f + 0.5f;
            float viewportY = clipPosition.y * invW * 0.5f + 0.5f;
            float viewportZ = -cameraPosition.z;
            return viewportZ > 0f &&
                   viewportX >= -0.05f && viewportX <= 1.05f &&
                   viewportY >= -0.05f && viewportY <= 1.05f;
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitModelSpawnSystem))]
    public partial struct UnitRenderBudgetInitialLodVisibilitySystem : ISystem
    {
        private EntityQuery _lodRootsNeedingInitialHideQuery;

        public void OnCreate(ref SystemState state)
        {
            _lodRootsNeedingInitialHideQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<UnitRenderBudgetCulledTag>() },
                Any = new[]
                {
                    ComponentType.ReadOnly<UnitMidLodRenderRootTag>(),
                    ComponentType.ReadOnly<UnitLowLodRenderRootTag>()
                },
                None = new[] { ComponentType.ReadOnly<UnitRenderBudgetLodHierarchyHiddenTag>() }
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            UnitModelSpawnSystem.HideInitiallyCulledLodHierarchies(
                state.EntityManager,
                _lodRootsNeedingInitialHideQuery);
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitRenderBudgetSystem))]
    public partial struct UnitRenderVisualExclusivitySystem : ISystem
    {
        private const int UpdateIntervalFrames = 2;
        private EntityQuery _visualRootsQuery;
        private int _nextUpdateFrame;

        public void OnCreate(ref SystemState state)
        {
            _visualRootsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                Any = new[]
                {
                    ComponentType.ReadOnly<UnitDetailedVisualReference>(),
                    ComponentType.ReadOnly<UnitModelInstanceReference>(),
                    ComponentType.ReadOnly<UnitMidLodInstanceReference>(),
                    ComponentType.ReadOnly<UnitLowLodInstanceReference>(),
                    ComponentType.ReadOnly<UnitDestroyedVisualReference>(),
                    ComponentType.ReadOnly<VehicleDestroyedVisualInstanceReference>()
                },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            int frame = Time.frameCount;
            if (frame < _nextUpdateFrame)
                return;
            _nextUpdateFrame = frame + UpdateIntervalFrames;

            if (_visualRootsQuery.IsEmptyIgnoreFilter)
                return;

            EntityManager em = state.EntityManager;
            using NativeArray<Entity> visualRoots = _visualRootsQuery.ToEntityArray(Allocator.Temp);
            for (int visualRootIndex = 0; visualRootIndex < visualRoots.Length; visualRootIndex++)
            {
                Entity entity = visualRoots[visualRootIndex];
                if (!em.Exists(entity))
                    continue;

                bool destroyed = new UnitDeathRenderPolicy().ShouldUseDestroyedVisual(em, entity);
                if (destroyed)
                {
                    SetLinkedOriginalVisualsVisible(em, entity, false);
                    SetLiveVisualRootsVisible(em, entity, false, false, false);
                    if (em.HasComponent<UnitDestroyedVisualReference>(entity))
                    {
                        UnitDestroyedVisualReference visualRef = em.GetComponentData<UnitDestroyedVisualReference>(entity);
                        SetRenderTreeVisible(em, visualRef.AliveVisual, false);
                        SetRenderTreeVisible(em, visualRef.DestroyedVisual, false);
                    }

                    if (em.HasComponent<VehicleDestroyedVisualInstanceReference>(entity))
                        SetRenderTreeVisible(em, em.GetComponentData<VehicleDestroyedVisualInstanceReference>(entity).Instance, true);
                    SetAppliedState(em, entity, UnitRenderVisualKind.Unknown, destroyed: true);
                    continue;
                }

                UnitRenderVisualKind currentVisual = UnitRenderVisualKind.Detail;
                if (em.HasComponent<UnitRenderVisualComponent>(entity))
                {
                    currentVisual = (UnitRenderVisualKind)em.GetComponentData<UnitRenderVisualComponent>(entity).Current;
                    if (currentVisual == UnitRenderVisualKind.Unknown)
                        currentVisual = UnitRenderVisualKind.Detail;
                }

                if (IsLiveVisualStateAlreadyApplied(em, entity, currentVisual))
                    continue;

                SetLiveVisualRootsVisible(
                    em,
                    entity,
                    currentVisual == UnitRenderVisualKind.Detail,
                    currentVisual == UnitRenderVisualKind.Mid,
                    currentVisual == UnitRenderVisualKind.Low);
                SetAppliedState(em, entity, currentVisual, destroyed: false);
            }
        }

        private static bool IsLiveVisualStateAlreadyApplied(EntityManager em, Entity entity, UnitRenderVisualKind currentVisual)
        {
            if (!em.HasComponent<UnitRenderVisualExclusivityAppliedState>(entity))
                return false;

            UnitRenderVisualExclusivityAppliedState state =
                em.GetComponentData<UnitRenderVisualExclusivityAppliedState>(entity);
            return state.Destroyed == 0 && state.Visual == (byte)currentVisual;
        }

        private static void SetAppliedState(EntityManager em, Entity entity, UnitRenderVisualKind currentVisual, bool destroyed)
        {
            UnitRenderVisualExclusivityAppliedState state = new()
            {
                Visual = (byte)currentVisual,
                Destroyed = destroyed ? (byte)1 : (byte)0
            };
            if (em.HasComponent<UnitRenderVisualExclusivityAppliedState>(entity))
            {
                em.SetComponentData(entity, state);
                return;
            }

            em.AddComponentData(entity, state);
        }

        private static void SetLiveVisualRootsVisible(
            EntityManager em,
            Entity unit,
            bool detailVisible,
            bool midVisible,
            bool lowVisible)
        {
            bool hasAuthoredDetail = em.HasComponent<UnitDetailedVisualReference>(unit);
            if (hasAuthoredDetail)
                SetRenderTreeVisible(em, em.GetComponentData<UnitDetailedVisualReference>(unit).Root, detailVisible);

            if (em.HasComponent<UnitModelInstanceReference>(unit))
                SetRenderTreeVisible(em, em.GetComponentData<UnitModelInstanceReference>(unit).Instance, detailVisible && !hasAuthoredDetail);

            if (em.HasComponent<UnitMidLodInstanceReference>(unit))
                SetRenderTreeVisible(em, em.GetComponentData<UnitMidLodInstanceReference>(unit).Instance, midVisible);

            if (em.HasComponent<UnitLowLodInstanceReference>(unit))
                SetRenderTreeVisible(em, em.GetComponentData<UnitLowLodInstanceReference>(unit).Instance, lowVisible);
        }

        private static void SetRenderTreeVisible(EntityManager em, Entity entity, bool visible)
        {
            if (entity == Entity.Null || !em.Exists(entity))
                return;

            using NativeList<Entity> tree = new(Allocator.Temp);
            using NativeHashSet<Entity> visited = new(16, Allocator.Temp);
            CollectRenderTree(em, entity, tree, visited);
            for (int i = 0; i < tree.Length; i++)
                SetRenderEntityVisible(em, tree[i], visible);
        }

        private static void SetLinkedOriginalVisualsVisible(EntityManager em, Entity unit, bool visible)
        {
            if (unit == Entity.Null || !em.Exists(unit) || !em.HasBuffer<LinkedEntityGroup>(unit))
                return;

            using NativeList<Entity> tree = new(Allocator.Temp);
            using NativeHashSet<Entity> visited = new(64, Allocator.Temp);
            DynamicBuffer<LinkedEntityGroup> linkedEntities = em.GetBuffer<LinkedEntityGroup>(unit);
            for (int i = 0; i < linkedEntities.Length; i++)
            {
                Entity linkedEntity = linkedEntities[i].Value;
                if (linkedEntity == unit)
                    continue;

                CollectRenderTree(em, linkedEntity, tree, visited);
            }

            for (int i = 0; i < tree.Length; i++)
                SetRenderEntityVisible(em, tree[i], visible);
        }

        private static void CollectRenderTree(EntityManager em, Entity entity, NativeList<Entity> tree, NativeHashSet<Entity> visited)
        {
            if (entity == Entity.Null || !em.Exists(entity))
                return;
            if (!visited.Add(entity))
                return;

            tree.Add(entity);
            if (em.HasBuffer<LinkedEntityGroup>(entity))
            {
                DynamicBuffer<LinkedEntityGroup> linkedEntities = em.GetBuffer<LinkedEntityGroup>(entity);
                for (int i = 0; i < linkedEntities.Length; i++)
                    CollectRenderTree(em, linkedEntities[i].Value, tree, visited);
            }

            if (!em.HasBuffer<Child>(entity))
                return;

            DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
            for (int i = 0; i < children.Length; i++)
                CollectRenderTree(em, children[i].Value, tree, visited);
        }

        private static void SetRenderEntityVisible(EntityManager em, Entity entity, bool visible)
        {
            if (entity == Entity.Null || !em.Exists(entity))
                return;

            if (visible)
            {
                if (em.HasComponent<Disabled>(entity))
                    em.RemoveComponent<Disabled>(entity);
                if (em.HasComponent<DisableRendering>(entity))
                    em.RemoveComponent<DisableRendering>(entity);
                if (em.HasComponent<UnitRenderBudgetCulledTag>(entity))
                    em.RemoveComponent<UnitRenderBudgetCulledTag>(entity);
            }
            else
            {
                if (!em.HasComponent<DisableRendering>(entity))
                    em.AddComponent<DisableRendering>(entity);
                if (!em.HasComponent<UnitRenderBudgetCulledTag>(entity))
                    em.AddComponent<UnitRenderBudgetCulledTag>(entity);
            }
        }
    }
}
