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
    public Mesh Mesh;
    public MeshRenderer Renderer;
    public MeshFilter MeshFilter;
    public Material Material;
    public Entity[] SoldierEntities;
    public MeshRenderer[] SoldierRenderers;
    public Material[] SoldierMaterials;
    public Entity[] SoldierFactionMaskEntities;
    public Material[] SoldierFactionMaskMaterials;
    public Entity[] SoldierShadowEntities;
    public Material[] SoldierShadowMaterials;
    public MeshRenderer SelectionRenderer;
    public Material SelectionMaterial;
    public Entity[] SelectionEntities;
    public MeshRenderer[] SelectionRenderers;
    public Material[] SelectionMaterials;
    public Entity TargetMarkerEntity;
    public Material TargetMarkerMaterial;
    public Entity[] EnemyReadabilityEntities;
    public Material[] EnemyReadabilityMaterials;
    public Entity[] EnemyHealthBarEntities;
    public Material[] EnemyHealthBarMaterials;
    public Vector3[] SoldierLocalPositions;
    public Vector3[] SoldierFactionMaskLocalPositions;
    public Vector3[] SoldierShadowLocalPositions;
    public Vector3[] SelectionLocalPositions;
    public Vector3[] SelectionLocalScales;
    public Vector3[] EnemyReadabilityLocalPositions;
    public Vector3[] EnemyReadabilityLocalScales;
    public Vector3[] EnemyHealthBarLocalPositions;
    public Vector3[] EnemyHealthBarLocalScales;
    public Vector3 TargetMarkerWorldPosition;
    public Vector3 TargetMarkerWorldScale;
    public bool[] SoldierVisible;
    public bool[] SoldierFactionMaskVisible;
    public bool[] SoldierShadowVisible;
    public bool[] SelectionVisible;
    public bool[] EnemyReadabilityVisible;
    public bool[] EnemyHealthBarVisible;
    public bool TargetMarkerVisible;
    public string TargetMarkerKind;
    public string CurrentSpriteId;
    public string CurrentFacingId;
    public string CurrentAnimationFrameKey;
    public int SoldierCount;
    public Vector2 SoldierPivotNormalized;
    public float AnimationPhase;
    public float AnimationElapsed;
    public Vector3 InstancePosition;
    public Quaternion InstanceRotation;
    public float InstanceScale;
}

public struct MissionRuntimeEcsVisualTag : IComponentData
{
}

public struct MissionRuntimeSelectionMarkerVisualTag : IComponentData
{
}

public struct MissionRuntimeTargetMarkerVisualTag : IComponentData
{
}

public sealed class MissionRuntimeTerrainSurfaceRendererRuntime : IComponentData
{
    public GameObject Instance;
    public SpriteRenderer Renderer;
    public Sprite GroundSprite;
    public Camera GroundCamera;
    public Vector3 GroundPosition;
    public Vector3 GroundScale;
    public bool GroundFollowsCamera;
    public Sprite[] ProductionTacticalPlateSprites;
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
