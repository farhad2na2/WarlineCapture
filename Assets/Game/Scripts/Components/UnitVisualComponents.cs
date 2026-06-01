using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct UnitPrevWorldPos : IComponentData
{
    public float3 Value;
}

public struct UnitMoveVisualState : IComponentData
{
    public byte IsMoving; // 0/1
    public float StillSeconds;
}

public struct UnitRotationHold : IComponentData
{
    public quaternion Rotation;
}

public struct UnitAnimationSettings : IComponentData
{
    public float IdleDelayMinSeconds;
    public float IdleDelayMaxSeconds;
    public float IdleWanderDistanceMin;
    public float IdleWanderDistanceMax;
    public float AttackAnimationSeconds;
    public float DeathAnimationSeconds;
}

public struct UnitAnimationOrderEntry : IBufferElementData
{
    public byte Kind;
}

public struct UnitIdleWanderState : IComponentData
{
    public uint RandomState;
    public float RetrySeconds;
    public float CurrentIdleDelaySeconds;
}

public struct UnitAttackAnimationState : IComponentData
{
    public float TimeRemaining;
}

public struct UnitResolvedAnimationIndex : IComponentData
{
    public byte Value;
}

public struct UnitDeathAnimationState : IComponentData
{
    public float TimeRemaining;
}

public struct UnitDestroyedVisualReference : IComponentData
{
    public Entity AliveVisual;
    public Entity DestroyedVisual;
    public float AliveVisibleScale;
    public float DestroyedVisibleScale;
}

public struct UnitModelPrefabReference : IComponentData
{
    public Entity Prefab;
}

public struct UnitModelLocalTransform : IComponentData
{
    public float3 Position;
    public quaternion Rotation;
    public float Scale;
}

public struct UnitModelInstanceReference : IComponentData
{
    public Entity Instance;
}

public struct UnitDetailedVisualReference : IComponentData
{
    public Entity Root;
}

public struct UnitMidLodPrefabReference : IComponentData
{
    public Entity Prefab;
}

public struct UnitMidLodInstanceReference : IComponentData
{
    public Entity Instance;
}

public struct UnitLowLodPrefabReference : IComponentData
{
    public Entity Prefab;
}

public struct UnitLowLodInstanceReference : IComponentData
{
    public Entity Instance;
}

public struct UnitMassRenderSettingsApplied : IComponentData
{
}

public struct UnitRenderSafetyPatchedTag : IComponentData
{
}

public struct UnitRenderVisualReadyTag : IComponentData
{
}

public struct UnitRenderBudgetCulledTag : IComponentData
{
}

public struct UnitRenderBudgetCulledUnitTag : IComponentData
{
}

public enum UnitRenderVisualKind : byte
{
    Unknown = 0,
    Detail = 1,
    Mid = 2,
    Low = 3,
    Far = 4
}

public struct UnitRenderVisualState : IComponentData
{
    public byte Current;
    public byte Desired;
    public int LastChangedFrame;
}

public struct UnitMidLodRenderRootTag : IComponentData
{
}

public struct UnitSafeVisibleCharacterLodTag : IComponentData
{
}

public struct UnitUsesSafeVisibleCharacterLodTag : IComponentData
{
}

public struct UnitVisibleCharacterLodSpawnDeferredTag : IComponentData
{
}

public struct UnitTransportHiddenVisualScale : IBufferElementData
{
    public Entity Visual;
    public float PreviousScale;
    public byte WasDisabled;
}

public struct UnitHelicopterBladeReference : IBufferElementData
{
    public Entity Blade;
    public byte Axis;
}

public struct UnitDestroyedVisualInitialized : IComponentData
{
}

public struct VehicleSelectionMarkerPrefabReference : IComponentData
{
    public Entity Prefab;
}

public struct VehicleSelectionMarkerInstanceReference : IComponentData
{
    public Entity Instance;
}

public struct VehicleHealthBarPrefabReference : IComponentData
{
    public Entity Prefab;
}

public struct VehicleHealthBarInstanceReference : IComponentData
{
    public Entity Instance;
}

public struct VehicleDestroyedVisualPrefabReference : IComponentData
{
    public Entity Prefab;
}

public struct VehicleDestroyedVisualInstanceReference : IComponentData
{
    public Entity Instance;
}

public struct VehicleDestroyedVisualSpawnRequest : IComponentData
{
}

public struct VehicleWreckState : IComponentData
{
    public float TimeRemaining;
}

public struct AutoWanderMoveTag : IComponentData
{
}

public sealed class UnitAttachedLightSet : IComponentData
{
    [System.Serializable]
    public sealed class Entry
    {
        public string Name;
        public LightType Type;
        public Color Color;
        public float Intensity;
        public float Range;
        public float SpotAngle;
        public float InnerSpotAngle;
        public bool CastShadows;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
    }

    public Entry[] Entries;
}

public sealed class UnitAttachedLightRuntime : IComponentData
{
    public GameObject[] Instances;
}
