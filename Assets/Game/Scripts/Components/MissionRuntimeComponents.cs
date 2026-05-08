using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct MissionRuntimeEntityId : IComponentData
{
    public FixedString64Bytes Value;
}

public struct MissionRuntimeCommandSquadTag : IComponentData
{
}

public struct MissionRuntimeEnemyPatrolTag : IComponentData
{
}

public struct MissionRuntimeObjectiveTarget : IComponentData
{
    public FixedString64Bytes ObjectiveId;
}

public enum MissionRuntimeSpriteVisualState : byte
{
    Idle = 0,
    Move = 1,
    Attack = 2,
    Damaged = 3,
    Destroyed = 4
}

public struct MissionRuntimeSpritePresenter : IComponentData
{
    public FixedString64Bytes RuntimeEntityId;
    public FixedString64Bytes ManifestAssetId;
    public FixedString64Bytes IdleSpriteId;
    public FixedString64Bytes MoveSpriteId;
    public FixedString64Bytes AttackSpriteId;
    public FixedString64Bytes DamagedSpriteId;
    public FixedString64Bytes DestroyedSpriteId;
    public FixedString64Bytes DestructionVfxSpriteId;
    public FixedString64Bytes CurrentSpriteId;
    public byte CurrentState;
    public byte RequiresFixedDirectionBakedContactShadow;
    public byte UsesSeparateDestroyedChild;
    public byte FinalAtlasArtReady;
}

public struct MissionRuntimeSpritePresenterSuppressesLegacyModelTag : IComponentData
{
}

public struct MissionRuntimeTerrainSurface : IComponentData
{
    public FixedString64Bytes RuntimeEntityId;
    public FixedString64Bytes MapId;
    public FixedString64Bytes MissionId;
    public float2 WorldOrigin;
    public float2 VisibleWorldSize;
    public int2 GridSize;
    public byte RuntimePlane;
    public byte SpriteUpAlignsPositiveWorldZ;
}

public sealed class MissionRuntimeSpriteRendererRuntime : IComponentData
{
    public GameObject Instance;
    public SpriteRenderer Renderer;
    public string CurrentSpriteId;
}

public sealed class MissionRuntimeAtlasQuadRuntime : IComponentData
{
    public GameObject Instance;
    public MeshRenderer Renderer;
    public MeshFilter MeshFilter;
    public Material Material;
    public MeshRenderer[] SoldierRenderers;
    public Material[] SoldierMaterials;
    public MeshRenderer SelectionRenderer;
    public Material SelectionMaterial;
    public MeshRenderer[] SelectionRenderers;
    public Material[] SelectionMaterials;
    public string CurrentSpriteId;
    public int SoldierCount;
    public float AnimationPhase;
}

public sealed class MissionRuntimeTerrainSurfaceRendererRuntime : IComponentData
{
    public GameObject Instance;
    public SpriteRenderer Renderer;
    public Sprite GroundSprite;
}

public struct MissionRuntimePatrolRoute : IComponentData
{
    public int2 WaypointA;
    public int2 WaypointB;
    public int2 WaypointC;
    public byte WaypointCount;
    public byte CurrentWaypointIndex;
    public byte HoldAtEnd;
}

public struct MissionRuntimeOpeningControlProtection : IComponentData
{
}
