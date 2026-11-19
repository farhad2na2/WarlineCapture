using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Rendering.Contracts;
using Game.Components;
using Game.Configs;

namespace Game.Rendering
{
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

        private readonly System.Collections.Generic.Dictionary<GameObject, bool> _rocketMuzzles = new();
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
            _rocketMuzzles.Clear();
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
                if (!SkirmishFogRenderVisibility.PlayerVisible(em, entity)) continue;
                UnitAttackTraceComponent trace = em.GetComponentData<UnitAttackTraceComponent>(entity);
                if (trace.TimeRemaining <= 0f)
                    continue;

                LocalTransform sourceTransform = em.GetComponentData<LocalTransform>(entity);
                UnitAttack attack = em.GetComponentData<UnitAttack>(entity);
                // Unarmed contact attacks have no authored weapon effect.
                if (attack.Range <= 8f && !em.HasComponent<UnitMuzzleFlashVfxReference>(entity))
                    continue;
                float3 sourcePosition = sourceTransform.Position;
                if (em.HasComponent<UnitTurretReference>(entity))
                {
                    UnitTurretReference turretRef = em.GetComponentData<UnitTurretReference>(entity);
                    if (em.Exists(turretRef.Turret) && em.HasComponent<LocalToWorld>(turretRef.Turret))
                        sourcePosition = em.GetComponentData<LocalToWorld>(turretRef.Turret).Position;
                }

                float height = sourceHeightOffset;
                float forward = sourceForwardOffset;
                bool rocket = false;
                if (em.HasComponent<UnitMuzzleFlashVfxReference>(entity))
                {
                    var muzzle = em.GetComponentData<UnitMuzzleFlashVfxReference>(entity);
                    height = muzzle.HeightOffset;
                    forward = muzzle.ForwardOffset;
                    var prefab = muzzle.Prefab.Value;
                    if (prefab != null && !_rocketMuzzles.TryGetValue(prefab, out rocket))
                    {
                        rocket = prefab.name == "Vfx_MuzzleFlash_Rocket";
                        _rocketMuzzles.Add(prefab, rocket);
                    }
                }
                Vector3 baseStart = (Vector3)sourcePosition + Vector3.up * height;
                if (em.HasBuffer<BuildingDefenseAttackSlot>(entity))
                {
                    // A heavy tower can fire on four targets in one update.
                    // EngageTarget only retains one of them.
                    var slots = em.GetBuffer<BuildingDefenseAttackSlot>(entity, true);
                    for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
                    {
                        var slot = slots[slotIndex];
                        if (slot.ShotCounter <= 0 || slot.ShotCounter % math.max(1, attack.TracerEveryNthShot) != 0 ||
                            !TryGetVisibleEnd(em, slot.Target, out Vector3 slotEnd))
                            continue;
                        var slotTrace = trace;
                        slotTrace.TimeRemaining = attack.TraceVisibleSeconds - (attack.CooldownSeconds - slot.CooldownRemaining);
                        if (slotTrace.TimeRemaining > 0f)
                            QueueTraceInstance(ref batchCount, baseStart, slotEnd, attack, slotTrace, forward, false, slotIndex * 0.071f);
                    }
                    continue;
                }
                EngageTarget engage = em.GetComponentData<EngageTarget>(entity);
                if (!TryGetVisibleEnd(em, engage.Target, out Vector3 baseEnd))
                    continue;
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
                        forward, rocket,
                        sideSign * 0.071f);
                }
            }

            if (batchCount > 0)
                DrawBatch(batchCount);
        }

        private bool TryGetVisibleEnd(EntityManager em, Entity target, out Vector3 end)
        {
            end = default;
            if (target == Entity.Null || !em.Exists(target) || !em.HasComponent<LocalTransform>(target) ||
                !SkirmishFogRenderVisibility.PlayerVisible(em, target))
                return false;
            end = (Vector3)em.GetComponentData<LocalTransform>(target).Position + Vector3.up * targetHeightOffset;
            return true;
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
            float muzzleForward,
            bool rocket,
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
            float forwardOffset = Mathf.Min(muzzleForward, length * 0.4f);
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
            // A thin tracer needs a few pixels of silhouette even on pale desert roads.
            float depth = Mathf.Max(0.1f, Vector3.Dot(start - worldCamera.transform.position, cameraForward));
            float viewHeight = worldCamera.orthographic ? 2f * worldCamera.orthographicSize :
                2f * depth * Mathf.Tan(worldCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float pixelSize = viewHeight / Mathf.Max(1, worldCamera.pixelHeight);
            float width = Mathf.Max(attack.TraceWidth * 4.8f, pixelSize * (3.0f + 4f * attack.TraceWidth));
            float progress = 1f - Mathf.Clamp01(trace.TimeRemaining / Mathf.Max(0.01f, attack.TraceVisibleSeconds));
            float density = Mathf.Max(1f, attack.TraceDashDensity);
            float segment = length / density;
            // The authored dash count and scroll rate still control the train of projectiles.
            // Single shells cross the shot once and never wrap back to the muzzle.
            float scroll = density <= 1f ? progress : Mathf.Repeat(phase + progress *
                attack.TraceVisibleSeconds * attack.TraceScrollSpeed, 1f);
            float streakLength = Mathf.Clamp(0.9f + 12f * attack.TraceWidth + 4f * attack.TraceVisibleSeconds,
                pixelSize * 9f, Mathf.Max(pixelSize * 9f, segment * 0.65f));
            float fraction = Mathf.Clamp(streakLength / Mathf.Max(0.01f, segment), 0.07f, 0.88f);
            _matrices[batchCount] = Matrix4x4.TRS(start, rotation, new Vector3(width, 1f, length));
            _colors[batchCount] = attack.TraceColor;
            _traceParams[batchCount] = new Vector4(density, scroll, fraction,
                rocket ? 1f : 0f);

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
}
