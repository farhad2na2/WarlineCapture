using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
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
                        PlayMuzzleFlash(value);
                        break;
                    case UnitAttackVfxRequestKind.Impact:
                        PlayImpact(value);
                        break;
                }
            }

            em.DestroyEntity(_requestQuery);
        }

        private static void PlayMuzzleFlash(UnitAttackVfxRequest request)
        {
            GameObject muzzlePrefab = request.Prefab.Value;
            if (muzzlePrefab == null)
                return;

            Quaternion rotation = ToUnityQuaternion(request.PlaybackRotation);
            int originCount = ResolveMuzzleFlashOriginCount(request.OriginCount, request.LateralOffset);
            float3 sideRight = math.normalizesafe(request.SideRight, new float3(1f, 0f, 0f));
            for (int originIndex = 0; originIndex < originCount; originIndex++)
            {
                float sideSign = ResolveMuzzleFlashSideSign(originIndex, originCount);
                float3 sideOffset = sideRight * (sideSign * math.max(0f, request.LateralOffset));
                UnitAttackImpactVfxView.Play(muzzlePrefab, (Vector3)(request.PlaybackPosition + sideOffset), rotation);
            }
        }

        private static void PlayImpact(UnitAttackVfxRequest request)
        {
            GameObject impactPrefab = request.Prefab.Value;
            if (impactPrefab == null)
                return;

            UnitAttackImpactVfxView.Play(impactPrefab, request.PlaybackPosition, ToUnityQuaternion(request.PlaybackRotation));
        }

        private static int ResolveMuzzleFlashOriginCount(byte originCount, float lateralOffset)
        {
            if (originCount <= 1 || lateralOffset <= 0f)
                return 1;

            return math.clamp(originCount, 1, MaxMuzzleFlashOriginCount);
        }

        private static float ResolveMuzzleFlashSideSign(int index, int count)
        {
            if (count <= 1)
                return 0f;
            if (count == 2)
                return index == 0 ? -1f : 1f;

            return math.lerp(-1f, 1f, index / (float)(count - 1));
        }

        private static Quaternion ToUnityQuaternion(quaternion rotation) =>
            new(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
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

        private static Quaternion ToUnityQuaternion(quaternion rotation) =>
            new(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
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
}
