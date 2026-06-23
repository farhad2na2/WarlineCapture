using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(UnitAttackSystem))]
[UpdateBefore(typeof(UnitDeathSystem))]
public partial class UnitAttackVfxRequestSystem : SystemBase
{
    private const int MaxMuzzleFlashOriginCount = 4;
    private EntityQuery _requestQuery;

    protected override void OnCreate()
    {
        _requestQuery = GetEntityQuery(ComponentType.ReadOnly<UnitAttackVfxRequest>());
        RequireForUpdate(_requestQuery);
    }

    protected override void OnUpdate()
    {
        EntityManager em = EntityManager;
        foreach (RefRO<UnitAttackVfxRequest> request in SystemAPI.Query<RefRO<UnitAttackVfxRequest>>())
        {
            UnitAttackVfxRequest value = request.ValueRO;
            switch ((UnitAttackVfxRequestKind)value.Kind)
            {
                case UnitAttackVfxRequestKind.MuzzleFlash:
                    PlayMuzzleFlash(em, value);
                    break;
                case UnitAttackVfxRequestKind.Impact:
                    PlayImpact(em, value);
                    break;
            }
        }

        em.DestroyEntity(_requestQuery);
    }

    private static void PlayMuzzleFlash(EntityManager em, UnitAttackVfxRequest request)
    {
        if (request.Source == Entity.Null ||
            !em.Exists(request.Source) ||
            !em.HasComponent<UnitMuzzleFlashVfxReference>(request.Source))
        {
            return;
        }

        UnitMuzzleFlashVfxReference muzzleVfx = em.GetComponentData<UnitMuzzleFlashVfxReference>(request.Source);
        GameObject muzzlePrefab = muzzleVfx.Prefab.Value;
        if (muzzlePrefab == null)
            return;

        LocalTransform sourceTransform = em.HasComponent<LocalTransform>(request.Source)
            ? em.GetComponentData<LocalTransform>(request.Source)
            : LocalTransform.FromPosition(request.SourcePosition);

        float3 muzzlePosition = sourceTransform.Position;
        if (em.HasComponent<UnitTurretReference>(request.Source))
        {
            UnitTurretReference turretRef = em.GetComponentData<UnitTurretReference>(request.Source);
            if (em.Exists(turretRef.Turret) && em.HasComponent<LocalToWorld>(turretRef.Turret))
                muzzlePosition = em.GetComponentData<LocalToWorld>(turretRef.Turret).Position;
        }

        muzzlePosition.y += math.max(0f, muzzleVfx.HeightOffset);
        Quaternion rotation = ResolveLookRotation(em, request.Target, request.TargetPosition, sourceTransform.Position, sourceTransform.Rotation);
        float forwardOffset = math.max(0f, muzzleVfx.ForwardOffset);
        if (forwardOffset > 0f)
            muzzlePosition += (float3)(rotation * Vector3.forward) * forwardOffset;

        UnitAttackTraceOriginPattern originPattern = em.HasComponent<UnitAttackTraceOriginPattern>(request.Source)
            ? em.GetComponentData<UnitAttackTraceOriginPattern>(request.Source)
            : default;
        int originCount = ResolveMuzzleFlashOriginCount(originPattern);
        Vector3 sideRight = ResolveMuzzleFlashSideRight(sourceTransform.Rotation, request.TargetPosition - sourceTransform.Position);
        for (int originIndex = 0; originIndex < originCount; originIndex++)
        {
            float sideSign = ResolveMuzzleFlashSideSign(originIndex, originCount);
            float3 sideOffset = (float3)sideRight * (sideSign * math.max(0f, originPattern.LateralOffset));
            UnitAttackImpactVfxView.Play(muzzlePrefab, (Vector3)(muzzlePosition + sideOffset), rotation);
        }
    }

    private static void PlayImpact(EntityManager em, UnitAttackVfxRequest request)
    {
        if (request.Source == Entity.Null ||
            !em.Exists(request.Source) ||
            !em.HasComponent<UnitAttackImpactVfxReference>(request.Source))
        {
            return;
        }

        UnitAttackImpactVfxReference impactVfx = em.GetComponentData<UnitAttackImpactVfxReference>(request.Source);
        GameObject impactPrefab = impactVfx.Prefab.Value;
        if (impactPrefab == null)
            return;

        float3 targetPosition = request.TargetPosition;
        if (request.Target != Entity.Null &&
            em.Exists(request.Target) &&
            em.HasComponent<LocalTransform>(request.Target))
        {
            targetPosition = em.GetComponentData<LocalTransform>(request.Target).Position;
        }

        float3 toAttacker = request.SourcePosition - targetPosition;
        toAttacker.y = 0f;
        Quaternion impactRotation = math.lengthsq(toAttacker) > 1e-4f
            ? Quaternion.LookRotation((Vector3)toAttacker)
            : Quaternion.identity;
        UnitAttackImpactVfxView.Play(impactPrefab, targetPosition, impactRotation);
    }

    private static Quaternion ResolveLookRotation(
        EntityManager em,
        Entity target,
        float3 fallbackTargetPosition,
        float3 sourcePosition,
        quaternion fallbackRotation)
    {
        float3 targetPosition = fallbackTargetPosition;
        if (target != Entity.Null &&
            em.Exists(target) &&
            em.HasComponent<LocalTransform>(target))
        {
            targetPosition = em.GetComponentData<LocalTransform>(target).Position;
        }

        float3 toTarget = targetPosition - sourcePosition;
        toTarget.y = 0f;
        return math.lengthsq(toTarget) > 1e-4f
            ? Quaternion.LookRotation((Vector3)toTarget)
            : new Quaternion(
                fallbackRotation.value.x,
                fallbackRotation.value.y,
                fallbackRotation.value.z,
                fallbackRotation.value.w);
    }

    private static int ResolveMuzzleFlashOriginCount(UnitAttackTraceOriginPattern pattern)
    {
        if (pattern.OriginCount <= 1 || pattern.LateralOffset <= 0f)
            return 1;

        return math.clamp(pattern.OriginCount, 1, MaxMuzzleFlashOriginCount);
    }

    private static float ResolveMuzzleFlashSideSign(int index, int count)
    {
        if (count <= 1)
            return 0f;
        if (count == 2)
            return index == 0 ? -1f : 1f;

        return math.lerp(-1f, 1f, index / (float)(count - 1));
    }

    private static Vector3 ResolveMuzzleFlashSideRight(quaternion sourceRotation, float3 aim)
    {
        Quaternion rotation = new(sourceRotation.value.x, sourceRotation.value.y, sourceRotation.value.z, sourceRotation.value.w);
        Vector3 right = rotation * Vector3.right;
        right.y = 0f;
        if (right.sqrMagnitude > 1e-5f)
            return right.normalized;

        Vector3 flatAim = (Vector3)aim;
        flatAim.y = 0f;
        if (flatAim.sqrMagnitude <= 1e-5f)
            return Vector3.right;

        return Vector3.Cross(Vector3.up, flatAim).normalized;
    }
}

[UpdateAfter(typeof(GroundMissileImpactSystem))]
[UpdateAfter(typeof(AirMissileImpactSystem))]
public partial class CombatGameObjectVfxPlaybackSystem : SystemBase
{
    private EntityQuery _requestQuery;

    protected override void OnCreate()
    {
        _requestQuery = GetEntityQuery(ComponentType.ReadOnly<CombatGameObjectVfxRequest>());
        RequireForUpdate(_requestQuery);
    }

    protected override void OnUpdate()
    {
        EntityManager em = EntityManager;
        foreach (RefRO<CombatGameObjectVfxRequest> request in SystemAPI.Query<RefRO<CombatGameObjectVfxRequest>>())
        {
            CombatGameObjectVfxRequest value = request.ValueRO;
            GameObject prefab = value.Prefab.Value != null ? value.Prefab.Value : value.FallbackPrefab.Value;
            if (prefab != null)
            {
                Quaternion rotation = ToUnityQuaternion(value.Rotation);
                switch ((CombatGameObjectVfxRequestKind)value.Kind)
                {
                    case CombatGameObjectVfxRequestKind.Play:
                        UnitAttackImpactVfxView.Play(prefab, value.Position, rotation);
                        break;
                    case CombatGameObjectVfxRequestKind.TimedLoop:
                        UnitAttackImpactVfxView.PlayTimedLoop(
                            prefab,
                            value.Position,
                            rotation,
                            value.EmitSeconds,
                            value.ActiveSeconds);
                        break;
                }
            }
        }

        em.DestroyEntity(_requestQuery);
    }

    private static Quaternion ToUnityQuaternion(quaternion rotation)
    {
        return new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
    }
}

internal static class CombatGameObjectVfxRequests
{
    public static void Enqueue(
        EntityCommandBuffer ecb,
        UnityObjectRef<GameObject> prefab,
        float3 position,
        quaternion rotation,
        CombatGameObjectVfxRequestKind kind,
        float emitSeconds = 0f,
        float activeSeconds = 0f,
        UnityObjectRef<GameObject> fallbackPrefab = default)
    {
        Entity request = ecb.CreateEntity();
        ecb.AddComponent(request, new CombatGameObjectVfxRequest
        {
            Kind = (byte)kind,
            Prefab = prefab,
            FallbackPrefab = fallbackPrefab,
            Position = position,
            Rotation = rotation,
            EmitSeconds = emitSeconds,
            ActiveSeconds = activeSeconds
        });
    }
}
