using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct UnitPrevWorldPos : IComponentData
{
    public float3 Value;
}

public struct UnitMoveVisualComponent : IComponentData
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

public struct UnitIdleWanderComponent : IComponentData
{
    public uint RandomState;
    public float RetrySeconds;
    public float CurrentIdleDelaySeconds;
}

public struct UnitAttackAnimationComponent : IComponentData
{
    public float TimeRemaining;
}

public struct UnitResolvedAnimationIndex : IComponentData
{
    public byte Value;
    public byte Changed;
    public byte Updated;
}

public struct UnitDeathAnimationComponent : IComponentData
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

public struct UnitSelectionHitbox : IComponentData
{
    public float3 Center;
    public float3 Extents;
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

public struct UnitRenderVisualComponent : IComponentData
{
    public byte Current;
    public byte Desired;
    public int LastChangedFrame;
}

public struct UnitMidLodRenderRootTag : IComponentData
{
}

public struct UnitLowLodRenderRootTag : IComponentData
{
}

public struct UnitRenderBudgetLodHierarchyHiddenTag : IComponentData
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

public struct UnitSelectionMarkerPrefabReference : IComponentData
{
    public Entity Prefab;
}

public struct UnitVisualPrefabReferencesBackfilledTag : IComponentData
{
}

public struct UnitSelectionMarkerInstanceReference : IComponentData
{
    public Entity Instance;
}

public struct UnitHealthBarPrefabReference : IComponentData
{
    public Entity Prefab;
}

public struct UnitHealthBarInstanceReference : IComponentData
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

public struct VehicleWreckComponent : IComponentData
{
    public float TimeRemaining;
}

public struct AutoWanderMoveTag : IComponentData
{
}

public struct UnitAttachedLightSetupElement : IBufferElementData
{
    public FixedString64Bytes Name;
    public LightType Type;
    public Color Color;
    public float Intensity;
    public float Range;
    public float SpotAngle;
    public float InnerSpotAngle;
    public byte CastShadows;
    public float3 LocalPosition;
    public quaternion LocalRotation;
}

public struct UnitAttachedLightCleanupRequest : IComponentData
{
}
