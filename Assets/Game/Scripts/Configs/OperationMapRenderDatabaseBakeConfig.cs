using System;
using System.Collections.Generic;
using Game.Components;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public sealed class OperationMapRenderMeshConfigRecord
    {
        [SerializeField] private string assetGuid;
        [SerializeField] private long localId;
        [SerializeField] private Mesh mesh;

        public string AssetGuid => assetGuid;
        public long LocalId => localId;
        public Mesh Mesh => mesh;

        public OperationMapRenderMeshConfigRecord(string assetGuid, long localId, Mesh mesh)
        {
            this.assetGuid = assetGuid;
            this.localId = localId;
            this.mesh = mesh;
        }
    }

    [Serializable]
    public sealed class OperationMapRenderMaterialConfigRecord
    {
        [SerializeField] private string assetGuid;
        [SerializeField] private long localId;
        [SerializeField] private Material material;

        public string AssetGuid => assetGuid;
        public long LocalId => localId;
        public Material Material => material;

        public OperationMapRenderMaterialConfigRecord(
            string assetGuid,
            long localId,
            Material material)
        {
            this.assetGuid = assetGuid;
            this.localId = localId;
            this.material = material;
        }
    }

    [Serializable]
    public sealed class OperationMapRenderPrototypeConfigRecord
    {
        [SerializeField] private ulong contentIdentityLow;
        [SerializeField] private ulong contentIdentityHigh;
        [SerializeField] private int firstPart;
        [SerializeField] private int partCount;
        [SerializeField] private Bounds combinedLocalBounds;
        [SerializeField] private DenseCityPresentationSemanticCategory semanticCategory;
        [SerializeField] private OperationMapRenderEligibilityFlags eligibilityFlags;

        public ulong ContentIdentityLow => contentIdentityLow;
        public ulong ContentIdentityHigh => contentIdentityHigh;
        public int FirstPart => firstPart;
        public int PartCount => partCount;
        public Bounds CombinedLocalBounds => combinedLocalBounds;
        public DenseCityPresentationSemanticCategory SemanticCategory => semanticCategory;
        public OperationMapRenderEligibilityFlags EligibilityFlags => eligibilityFlags;

        public OperationMapRenderPrototypeConfigRecord(
            ulong contentIdentityLow,
            ulong contentIdentityHigh,
            int firstPart,
            int partCount,
            Bounds combinedLocalBounds,
            DenseCityPresentationSemanticCategory semanticCategory,
            OperationMapRenderEligibilityFlags eligibilityFlags)
        {
            this.contentIdentityLow = contentIdentityLow;
            this.contentIdentityHigh = contentIdentityHigh;
            this.firstPart = firstPart;
            this.partCount = partCount;
            this.combinedLocalBounds = combinedLocalBounds;
            this.semanticCategory = semanticCategory;
            this.eligibilityFlags = eligibilityFlags;
        }
    }

    [Serializable]
    public sealed class OperationMapRenderPrototypePartConfigRecord
    {
        [SerializeField] private ulong rendererPathIdentityLow;
        [SerializeField] private ulong rendererPathIdentityHigh;
        [SerializeField] private int meshIndex;
        [SerializeField] private int materialIndex;
        [SerializeField] private int subMeshIndex;
        [SerializeField] private Matrix4x4 localToPlacement;
        [SerializeField] private Bounds localBounds;
        [SerializeField] private Color linearBaseColor;
        [SerializeField] private OperationMapRenderPolicyBucket policyBucket;
        [SerializeField] private int poolBucketIndex;
        [SerializeField] private OperationMapRenderLodFlags lodFlags;
        [SerializeField] private OperationMapRenderShadowFlags shadowFlags;

        public ulong RendererPathIdentityLow => rendererPathIdentityLow;
        public ulong RendererPathIdentityHigh => rendererPathIdentityHigh;
        public int MeshIndex => meshIndex;
        public int MaterialIndex => materialIndex;
        public int SubMeshIndex => subMeshIndex;
        public Matrix4x4 LocalToPlacement => localToPlacement;
        public Bounds LocalBounds => localBounds;
        public Color LinearBaseColor => linearBaseColor;
        public OperationMapRenderPolicyBucket PolicyBucket => policyBucket;
        public int PoolBucketIndex => poolBucketIndex;
        public OperationMapRenderLodFlags LodFlags => lodFlags;
        public OperationMapRenderShadowFlags ShadowFlags => shadowFlags;

        public OperationMapRenderPrototypePartConfigRecord(
            ulong rendererPathIdentityLow,
            ulong rendererPathIdentityHigh,
            int meshIndex,
            int materialIndex,
            int subMeshIndex,
            Matrix4x4 localToPlacement,
            Bounds localBounds,
            Color linearBaseColor,
            OperationMapRenderPolicyBucket policyBucket,
            int poolBucketIndex,
            OperationMapRenderLodFlags lodFlags,
            OperationMapRenderShadowFlags shadowFlags)
        {
            this.rendererPathIdentityLow = rendererPathIdentityLow;
            this.rendererPathIdentityHigh = rendererPathIdentityHigh;
            this.meshIndex = meshIndex;
            this.materialIndex = materialIndex;
            this.subMeshIndex = subMeshIndex;
            this.localToPlacement = localToPlacement;
            this.localBounds = localBounds;
            this.linearBaseColor = linearBaseColor;
            this.policyBucket = policyBucket;
            this.poolBucketIndex = poolBucketIndex;
            this.lodFlags = lodFlags;
            this.shadowFlags = shadowFlags;
        }
    }

    [Serializable]
    public sealed class OperationMapRenderPlacementConfigRecord
    {
        [SerializeField] private ulong stableIdentityLow;
        [SerializeField] private ulong stableIdentityHigh;
        [SerializeField] private ulong sourceOwnerIdentityLow;
        [SerializeField] private ulong sourceOwnerIdentityHigh;
        [SerializeField] private int prototypeIndex;
        [SerializeField] private Matrix4x4 worldMatrix;
        [SerializeField] private int cellIndex;
        [SerializeField] private int stateOwnerIndex;
        [SerializeField] private OperationMapRenderVisualState requiredVisualState;
        [SerializeField] private int priority;
        [SerializeField] private DenseCityPresentationSemanticCategory semanticCategory;

        public ulong StableIdentityLow => stableIdentityLow;
        public ulong StableIdentityHigh => stableIdentityHigh;
        public ulong SourceOwnerIdentityLow =>
            sourceOwnerIdentityLow == 0 && sourceOwnerIdentityHigh == 0
                ? stableIdentityLow
                : sourceOwnerIdentityLow;
        public ulong SourceOwnerIdentityHigh =>
            sourceOwnerIdentityLow == 0 && sourceOwnerIdentityHigh == 0
                ? stableIdentityHigh
                : sourceOwnerIdentityHigh;
        public int PrototypeIndex => prototypeIndex;
        public Matrix4x4 WorldMatrix => worldMatrix;
        public int CellIndex => cellIndex;
        public int StateOwnerIndex => stateOwnerIndex;
        public OperationMapRenderVisualState RequiredVisualState => requiredVisualState;
        public int Priority => priority;
        public DenseCityPresentationSemanticCategory SemanticCategory => semanticCategory;

        public OperationMapRenderPlacementConfigRecord(
            ulong stableIdentityLow,
            ulong stableIdentityHigh,
            int prototypeIndex,
            Matrix4x4 worldMatrix,
            int cellIndex,
            int stateOwnerIndex,
            OperationMapRenderVisualState requiredVisualState,
            int priority,
            DenseCityPresentationSemanticCategory semanticCategory,
            ulong sourceOwnerIdentityLow = 0,
            ulong sourceOwnerIdentityHigh = 0)
        {
            this.stableIdentityLow = stableIdentityLow;
            this.stableIdentityHigh = stableIdentityHigh;
            this.sourceOwnerIdentityLow = sourceOwnerIdentityLow == 0 &&
                                          sourceOwnerIdentityHigh == 0
                ? stableIdentityLow
                : sourceOwnerIdentityLow;
            this.sourceOwnerIdentityHigh = sourceOwnerIdentityLow == 0 &&
                                           sourceOwnerIdentityHigh == 0
                ? stableIdentityHigh
                : sourceOwnerIdentityHigh;
            this.prototypeIndex = prototypeIndex;
            this.worldMatrix = worldMatrix;
            this.cellIndex = cellIndex;
            this.stateOwnerIndex = stateOwnerIndex;
            this.requiredVisualState = requiredVisualState;
            this.priority = priority;
            this.semanticCategory = semanticCategory;
        }
    }

    [Serializable]
    public sealed class OperationMapRenderCellConfigRecord
    {
        [SerializeField] private Vector2Int coordinate;
        [SerializeField] private Bounds worldBounds;
        [SerializeField] private int firstPlacementIndex;
        [SerializeField] private int placementIndexCount;

        public Vector2Int Coordinate => coordinate;
        public Bounds WorldBounds => worldBounds;
        public int FirstPlacementIndex => firstPlacementIndex;
        public int PlacementIndexCount => placementIndexCount;

        public OperationMapRenderCellConfigRecord(
            Vector2Int coordinate,
            Bounds worldBounds,
            int firstPlacementIndex,
            int placementIndexCount)
        {
            this.coordinate = coordinate;
            this.worldBounds = worldBounds;
            this.firstPlacementIndex = firstPlacementIndex;
            this.placementIndexCount = placementIndexCount;
        }
    }

    [Serializable]
    public sealed class OperationMapRenderPoolBucketConfigRecord
    {
        [SerializeField] private OperationMapRenderPolicyBucket policyBucket;
        [SerializeField] private int layer;
        [SerializeField] private uint renderingLayerMask;
        [SerializeField] private OperationMapRenderMotionVectorMode motionVectorMode;
        [SerializeField] private OperationMapRenderShadowFlags shadowFlags;
        [SerializeField] private int firstSlot;
        [SerializeField] private int capacity;
        [SerializeField] private int peakRequiredCount;
        [SerializeField] private int headroomCount;
        [SerializeField] private ulong reportIdentityLow;
        [SerializeField] private ulong reportIdentityHigh;

        public OperationMapRenderPolicyBucket PolicyBucket => policyBucket;
        public int Layer => layer;
        public uint RenderingLayerMask => renderingLayerMask;
        public OperationMapRenderMotionVectorMode MotionVectorMode => motionVectorMode;
        public OperationMapRenderShadowFlags ShadowFlags => shadowFlags;
        public int FirstSlot => firstSlot;
        public int Capacity => capacity;
        public int PeakRequiredCount => peakRequiredCount;
        public int HeadroomCount => headroomCount;
        public ulong ReportIdentityLow => reportIdentityLow;
        public ulong ReportIdentityHigh => reportIdentityHigh;

        public OperationMapRenderPoolBucketConfigRecord(
            OperationMapRenderPolicyBucket policyBucket,
            int layer,
            uint renderingLayerMask,
            OperationMapRenderMotionVectorMode motionVectorMode,
            OperationMapRenderShadowFlags shadowFlags,
            int firstSlot,
            int capacity,
            int peakRequiredCount,
            int headroomCount,
            ulong reportIdentityLow,
            ulong reportIdentityHigh)
        {
            this.policyBucket = policyBucket;
            this.layer = layer;
            this.renderingLayerMask = renderingLayerMask;
            this.motionVectorMode = motionVectorMode;
            this.shadowFlags = shadowFlags;
            this.firstSlot = firstSlot;
            this.capacity = capacity;
            this.peakRequiredCount = peakRequiredCount;
            this.headroomCount = headroomCount;
            this.reportIdentityLow = reportIdentityLow;
            this.reportIdentityHigh = reportIdentityHigh;
        }
    }

    public sealed class OperationMapRenderDatabaseBakeConfig : ScriptableObject
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private int schemaVersion;
        [SerializeField] private string operationMapId;
        [SerializeField] private string contentHash;
        [SerializeField] private float cellSize;
        [SerializeField] private Vector3 gridOrigin;
        [SerializeField] private Vector2Int gridDimensions;
        [SerializeField] private OperationMapRenderMeshConfigRecord[] meshes =
            Array.Empty<OperationMapRenderMeshConfigRecord>();
        [SerializeField] private OperationMapRenderMaterialConfigRecord[] materials =
            Array.Empty<OperationMapRenderMaterialConfigRecord>();
        [SerializeField] private OperationMapRenderPrototypeConfigRecord[] prototypes =
            Array.Empty<OperationMapRenderPrototypeConfigRecord>();
        [SerializeField] private OperationMapRenderPrototypePartConfigRecord[] parts =
            Array.Empty<OperationMapRenderPrototypePartConfigRecord>();
        [SerializeField] private OperationMapRenderPlacementConfigRecord[] placements =
            Array.Empty<OperationMapRenderPlacementConfigRecord>();
        [SerializeField] private OperationMapRenderCellConfigRecord[] cells =
            Array.Empty<OperationMapRenderCellConfigRecord>();
        [SerializeField] private int[] cellPlacementIndices = Array.Empty<int>();
        [SerializeField] private OperationMapRenderPoolBucketConfigRecord[] poolBuckets =
            Array.Empty<OperationMapRenderPoolBucketConfigRecord>();

        public int SchemaVersion => schemaVersion;
        public string OperationMapId => operationMapId;
        public string ContentHash => contentHash;
        public float CellSize => cellSize;
        public Vector3 GridOrigin => gridOrigin;
        public Vector2Int GridDimensions => gridDimensions;
        public IReadOnlyList<OperationMapRenderMeshConfigRecord> Meshes => meshes;
        public IReadOnlyList<OperationMapRenderMaterialConfigRecord> Materials => materials;
        public IReadOnlyList<OperationMapRenderPrototypeConfigRecord> Prototypes => prototypes;
        public IReadOnlyList<OperationMapRenderPrototypePartConfigRecord> Parts => parts;
        public IReadOnlyList<OperationMapRenderPlacementConfigRecord> Placements => placements;
        public IReadOnlyList<OperationMapRenderCellConfigRecord> Cells => cells;
        public IReadOnlyList<int> CellPlacementIndices => cellPlacementIndices;
        public IReadOnlyList<OperationMapRenderPoolBucketConfigRecord> PoolBuckets => poolBuckets;

        public void InitializeGeneratedData(
            string generatedOperationMapId,
            string generatedContentHash,
            float generatedCellSize,
            Vector3 generatedGridOrigin,
            Vector2Int generatedGridDimensions,
            OperationMapRenderMeshConfigRecord[] generatedMeshes,
            OperationMapRenderMaterialConfigRecord[] generatedMaterials,
            OperationMapRenderPrototypeConfigRecord[] generatedPrototypes,
            OperationMapRenderPrototypePartConfigRecord[] generatedParts,
            OperationMapRenderPlacementConfigRecord[] generatedPlacements,
            OperationMapRenderCellConfigRecord[] generatedCells,
            int[] generatedCellPlacementIndices,
            OperationMapRenderPoolBucketConfigRecord[] generatedPoolBuckets)
        {
            schemaVersion = CurrentSchemaVersion;
            operationMapId = generatedOperationMapId;
            contentHash = generatedContentHash;
            cellSize = generatedCellSize;
            gridOrigin = generatedGridOrigin;
            gridDimensions = generatedGridDimensions;
            meshes = generatedMeshes ?? Array.Empty<OperationMapRenderMeshConfigRecord>();
            materials = generatedMaterials ?? Array.Empty<OperationMapRenderMaterialConfigRecord>();
            prototypes =
                generatedPrototypes ?? Array.Empty<OperationMapRenderPrototypeConfigRecord>();
            parts = generatedParts ?? Array.Empty<OperationMapRenderPrototypePartConfigRecord>();
            placements =
                generatedPlacements ?? Array.Empty<OperationMapRenderPlacementConfigRecord>();
            cells = generatedCells ?? Array.Empty<OperationMapRenderCellConfigRecord>();
            cellPlacementIndices = generatedCellPlacementIndices ?? Array.Empty<int>();
            poolBuckets =
                generatedPoolBuckets ?? Array.Empty<OperationMapRenderPoolBucketConfigRecord>();

            if (!TryValidateSchema(out string error))
                throw new InvalidOperationException(error);
        }

        public bool TryValidateSchema(out string error)
        {
            if (schemaVersion != CurrentSchemaVersion)
            {
                error =
                    $"Render database bake schema must be {CurrentSchemaVersion}, " +
                    $"but was {schemaVersion}.";
                return false;
            }

            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId))
            {
                error = "Render database bake config requires a valid operation-map id.";
                return false;
            }

            if (!IsLowerHex(contentHash, 64))
            {
                error =
                    "Render database bake config content hash must be 64 lowercase hex characters.";
                return false;
            }

            if (!IsFinite(cellSize) || cellSize <= 0f ||
                !IsFinite(gridOrigin) ||
                gridDimensions.x <= 0 ||
                gridDimensions.y <= 0)
            {
                error =
                    "Render database bake grid requires finite positive cell size, finite origin, " +
                    "and positive dimensions.";
                return false;
            }

            if (!RequireNonempty(meshes, "meshes", out error) ||
                !RequireNonempty(materials, "materials", out error) ||
                !RequireNonempty(prototypes, "prototypes", out error) ||
                !RequireNonempty(parts, "parts", out error) ||
                !RequireNonempty(placements, "placements", out error) ||
                !RequireNonempty(cells, "cells", out error) ||
                !RequireNonempty(cellPlacementIndices, "cellPlacementIndices", out error) ||
                !RequireNonempty(poolBuckets, "poolBuckets", out error))
            {
                return false;
            }

            if (!ValidateAssets(out error) ||
                !ValidateBuckets(out error) ||
                !ValidateParts(out error) ||
                !ValidatePrototypes(out error) ||
                !ValidatePlacements(out error) ||
                !ValidateCells(out error))
            {
                return false;
            }

            error = null;
            return true;
        }

        private bool ValidateAssets(out string error)
        {
            OperationMapRenderMeshConfigRecord previousMesh = null;
            for (int index = 0; index < meshes.Length; index++)
            {
                OperationMapRenderMeshConfigRecord record = meshes[index];
                if (record == null || record.Mesh == null ||
                    !IsLowerHex(record.AssetGuid, 32) ||
                    record.LocalId == 0)
                {
                    error = $"meshes[{index}] has invalid asset identity or reference.";
                    return false;
                }

                if (previousMesh != null &&
                    CompareAssetIdentity(
                        previousMesh.AssetGuid,
                        previousMesh.LocalId,
                        record.AssetGuid,
                        record.LocalId) >= 0)
                {
                    error = "meshes must be strictly sorted by GUID/local id.";
                    return false;
                }
                previousMesh = record;
            }

            OperationMapRenderMaterialConfigRecord previousMaterial = null;
            for (int index = 0; index < materials.Length; index++)
            {
                OperationMapRenderMaterialConfigRecord record = materials[index];
                if (record == null || record.Material == null ||
                    !IsLowerHex(record.AssetGuid, 32) ||
                    record.LocalId == 0)
                {
                    error = $"materials[{index}] has invalid asset identity or reference.";
                    return false;
                }

                if (previousMaterial != null &&
                    CompareAssetIdentity(
                        previousMaterial.AssetGuid,
                        previousMaterial.LocalId,
                        record.AssetGuid,
                        record.LocalId) >= 0)
                {
                    error = "materials must be strictly sorted by GUID/local id.";
                    return false;
                }
                previousMaterial = record;
            }

            error = null;
            return true;
        }

        private bool ValidateBuckets(out string error)
        {
            int expectedFirstSlot = 0;
            OperationMapRenderPoolBucketConfigRecord previousBucket = null;
            for (int index = 0; index < poolBuckets.Length; index++)
            {
                OperationMapRenderPoolBucketConfigRecord bucket = poolBuckets[index];
                if (bucket == null ||
                    !Enum.IsDefined(typeof(OperationMapRenderPolicyBucket), bucket.PolicyBucket) ||
                    bucket.Layer < 0 ||
                    bucket.Layer > 31 ||
                    bucket.RenderingLayerMask == 0u ||
                    !Enum.IsDefined(
                        typeof(OperationMapRenderMotionVectorMode),
                        bucket.MotionVectorMode) ||
                    !HasKnownShadowFlags(bucket.ShadowFlags) ||
                    !IsBucketShadowPolicyValid(
                        bucket.PolicyBucket,
                        bucket.ShadowFlags) ||
                    bucket.FirstSlot != expectedFirstSlot ||
                    bucket.PeakRequiredCount <= 0 ||
                    bucket.Capacity <= 0 ||
                    bucket.HeadroomCount != bucket.Capacity - bucket.PeakRequiredCount ||
                    bucket.Capacity !=
                        ((bucket.PeakRequiredCount * 120L + 99L) / 100L) ||
                    IsZeroIdentity(bucket.ReportIdentityLow, bucket.ReportIdentityHigh))
                {
                    error = $"poolBuckets[{index}] has invalid fixed policy or capacity.";
                    return false;
                }

                if (previousBucket != null &&
                    ComparePolicy(previousBucket, bucket) >= 0)
                {
                    error = "poolBuckets must be strictly sorted by complete fixed policy.";
                    return false;
                }

                previousBucket = bucket;
                expectedFirstSlot += bucket.Capacity;
            }

            error = null;
            return true;
        }

        private bool ValidateParts(out string error)
        {
            for (int index = 0; index < parts.Length; index++)
            {
                OperationMapRenderPrototypePartConfigRecord part = parts[index];
                if (part == null ||
                    IsZeroIdentity(
                        part.RendererPathIdentityLow,
                        part.RendererPathIdentityHigh) ||
                    part.MeshIndex < 0 ||
                    part.MeshIndex >= meshes.Length ||
                    part.MaterialIndex < 0 ||
                    part.MaterialIndex >= materials.Length ||
                    part.SubMeshIndex < 0 ||
                    part.SubMeshIndex >= meshes[part.MeshIndex].Mesh.subMeshCount ||
                    part.PoolBucketIndex < 0 ||
                    part.PoolBucketIndex >= poolBuckets.Length ||
                    part.PolicyBucket != poolBuckets[part.PoolBucketIndex].PolicyBucket ||
                    !IsFinite(part.LocalToPlacement) ||
                    !IsValidBounds(part.LocalBounds) ||
                    !IsFinite(part.LinearBaseColor) ||
                    part.LinearBaseColor.r < 0f ||
                    part.LinearBaseColor.g < 0f ||
                    part.LinearBaseColor.b < 0f ||
                    part.LinearBaseColor.a < 0f ||
                    part.LinearBaseColor.a > 1f ||
                    part.LodFlags == OperationMapRenderLodFlags.None ||
                    !HasKnownLodFlags(part.LodFlags) ||
                    part.ShadowFlags != poolBuckets[part.PoolBucketIndex].ShadowFlags)
                {
                    error = $"parts[{index}] has invalid identity, reference, transform, or policy.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private bool ValidatePrototypes(out string error)
        {
            for (int index = 0; index < prototypes.Length; index++)
            {
                OperationMapRenderPrototypeConfigRecord prototype = prototypes[index];
                if (prototype == null ||
                    IsZeroIdentity(
                        prototype.ContentIdentityLow,
                        prototype.ContentIdentityHigh) ||
                    prototype.FirstPart < 0 ||
                    prototype.PartCount <= 0 ||
                    prototype.FirstPart > parts.Length - prototype.PartCount ||
                    !IsValidBounds(prototype.CombinedLocalBounds) ||
                    !Enum.IsDefined(
                        typeof(DenseCityPresentationSemanticCategory),
                        prototype.SemanticCategory) ||
                    prototype.EligibilityFlags == OperationMapRenderEligibilityFlags.None)
                {
                    error = $"prototypes[{index}] has invalid identity, range, bounds, or policy.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private bool ValidatePlacements(out string error)
        {
            var placementIdentities = new HashSet<(ulong, ulong)>();
            var stateOwnerIndices = new Dictionary<(ulong, ulong), int>();
            var stateOwnerIdentities = new Dictionary<int, (ulong, ulong)>();
            for (int index = 0; index < placements.Length; index++)
            {
                OperationMapRenderPlacementConfigRecord placement = placements[index];
                if (placement == null ||
                    IsZeroIdentity(
                        placement.StableIdentityLow,
                        placement.StableIdentityHigh) ||
                    IsZeroIdentity(
                        placement.SourceOwnerIdentityLow,
                        placement.SourceOwnerIdentityHigh) ||
                    placement.PrototypeIndex < 0 ||
                    placement.PrototypeIndex >= prototypes.Length ||
                    placement.CellIndex < 0 ||
                    placement.CellIndex >= cells.Length ||
                    placement.StateOwnerIndex < -1 ||
                    !Enum.IsDefined(
                        typeof(OperationMapRenderVisualState),
                        placement.RequiredVisualState) ||
                    !Enum.IsDefined(
                        typeof(DenseCityPresentationSemanticCategory),
                        placement.SemanticCategory) ||
                    !IsFinite(placement.WorldMatrix))
                {
                    error = $"placements[{index}] has invalid identity, reference, state, or matrix.";
                    return false;
                }
                if (!placementIdentities.Add(
                        (placement.StableIdentityLow, placement.StableIdentityHigh)))
                {
                    error = $"placements[{index}] duplicates a logical placement identity.";
                    return false;
                }

                bool requiresStateOwner =
                    (prototypes[placement.PrototypeIndex].EligibilityFlags &
                     OperationMapRenderEligibilityFlags.RequiresStateOwner) != 0;
                if (requiresStateOwner
                        ? placement.StateOwnerIndex < 0 ||
                          placement.RequiredVisualState == OperationMapRenderVisualState.Any
                        : placement.StateOwnerIndex != -1 ||
                          placement.RequiredVisualState != OperationMapRenderVisualState.Any ||
                          placement.SourceOwnerIdentityLow != placement.StableIdentityLow ||
                          placement.SourceOwnerIdentityHigh != placement.StableIdentityHigh)
                {
                    error = $"placements[{index}] has inconsistent state-owner policy.";
                    return false;
                }
                if (!requiresStateOwner)
                    continue;

                var sourceOwner = (
                    placement.SourceOwnerIdentityLow,
                    placement.SourceOwnerIdentityHigh);
                if ((stateOwnerIndices.TryGetValue(sourceOwner, out int existingIndex) &&
                     existingIndex != placement.StateOwnerIndex) ||
                    (stateOwnerIdentities.TryGetValue(
                         placement.StateOwnerIndex,
                         out (ulong, ulong) existingOwner) &&
                     existingOwner != sourceOwner))
                {
                    error = $"placements[{index}] aliases a building state-owner index.";
                    return false;
                }
                stateOwnerIndices[sourceOwner] = placement.StateOwnerIndex;
                stateOwnerIdentities[placement.StateOwnerIndex] = sourceOwner;
            }

            for (int index = 0; index < stateOwnerIdentities.Count; index++)
            {
                if (!stateOwnerIdentities.ContainsKey(index))
                {
                    error = "Building state-owner indices must be contiguous from zero.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private bool ValidateCells(out string error)
        {
            for (int index = 0; index < cells.Length; index++)
            {
                OperationMapRenderCellConfigRecord cell = cells[index];
                if (cell == null ||
                    !IsValidBounds(cell.WorldBounds) ||
                    cell.FirstPlacementIndex < 0 ||
                    cell.PlacementIndexCount <= 0 ||
                    cell.FirstPlacementIndex >
                        cellPlacementIndices.Length - cell.PlacementIndexCount)
                {
                    error = $"cells[{index}] has invalid bounds or placement range.";
                    return false;
                }
            }

            for (int index = 0; index < cellPlacementIndices.Length; index++)
            {
                if (cellPlacementIndices[index] < 0 ||
                    cellPlacementIndices[index] >= placements.Length)
                {
                    error = $"cellPlacementIndices[{index}] is outside placements.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool RequireNonempty<T>(T[] values, string name, out string error)
        {
            if (values == null || values.Length == 0)
            {
                error = $"Render database bake config requires nonempty {name}.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool IsLowerHex(string value, int length)
        {
            if (string.IsNullOrEmpty(value) || value.Length != length)
                return false;

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f'))
                {
                    return false;
                }
            }
            return true;
        }

        private static int CompareAssetIdentity(
            string leftGuid,
            long leftLocalId,
            string rightGuid,
            long rightLocalId)
        {
            int comparison = string.CompareOrdinal(leftGuid, rightGuid);
            return comparison != 0 ? comparison : leftLocalId.CompareTo(rightLocalId);
        }

        private static int ComparePolicy(
            OperationMapRenderPoolBucketConfigRecord left,
            OperationMapRenderPoolBucketConfigRecord right)
        {
            int comparison =
                ((byte)left.PolicyBucket).CompareTo((byte)right.PolicyBucket);
            if (comparison != 0)
                return comparison;
            comparison = left.Layer.CompareTo(right.Layer);
            if (comparison != 0)
                return comparison;
            comparison = left.RenderingLayerMask.CompareTo(right.RenderingLayerMask);
            if (comparison != 0)
                return comparison;
            comparison =
                ((byte)left.MotionVectorMode).CompareTo((byte)right.MotionVectorMode);
            return comparison != 0
                ? comparison
                : ((byte)left.ShadowFlags).CompareTo((byte)right.ShadowFlags);
        }

        private static bool HasKnownShadowFlags(OperationMapRenderShadowFlags flags)
        {
            const OperationMapRenderShadowFlags known =
                OperationMapRenderShadowFlags.CastShadows |
                OperationMapRenderShadowFlags.ReceiveShadows |
                OperationMapRenderShadowFlags.StaticShadowCaster;
            return (flags & ~known) == 0 &&
                   ((flags & OperationMapRenderShadowFlags.StaticShadowCaster) == 0 ||
                    (flags & OperationMapRenderShadowFlags.CastShadows) != 0);
        }

        private static bool HasKnownLodFlags(OperationMapRenderLodFlags flags)
        {
            const OperationMapRenderLodFlags known =
                OperationMapRenderLodFlags.Lod0 |
                OperationMapRenderLodFlags.Lod1 |
                OperationMapRenderLodFlags.Lod2;
            return (flags & ~known) == 0;
        }

        private static bool IsBucketShadowPolicyValid(
            OperationMapRenderPolicyBucket bucket,
            OperationMapRenderShadowFlags flags)
        {
            bool casts = (flags & OperationMapRenderShadowFlags.CastShadows) != 0;
            switch (bucket)
            {
                case OperationMapRenderPolicyBucket.OpaqueShadowsOn:
                case OperationMapRenderPolicyBucket.AlphaClippedShadowsOn:
                    return casts;
                case OperationMapRenderPolicyBucket.OpaqueShadowsOff:
                case OperationMapRenderPolicyBucket.AlphaClippedShadowsOff:
                case OperationMapRenderPolicyBucket.TransparentShadowsOff:
                    return !casts &&
                           (flags & OperationMapRenderShadowFlags.StaticShadowCaster) == 0;
                case OperationMapRenderPolicyBucket.AlwaysResidentException:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsZeroIdentity(ulong low, ulong high)
        {
            return low == 0ul && high == 0ul;
        }

        private static bool IsValidBounds(Bounds bounds)
        {
            return IsFinite(bounds.center) &&
                   IsFinite(bounds.extents) &&
                   bounds.extents.x >= 0f &&
                   bounds.extents.y >= 0f &&
                   bounds.extents.z >= 0f;
        }

        private static bool IsFinite(Matrix4x4 value)
        {
            for (int index = 0; index < 16; index++)
            {
                if (!IsFinite(value[index]))
                    return false;
            }
            return true;
        }

        private static bool IsFinite(Color value)
        {
            return IsFinite(value.r) &&
                   IsFinite(value.g) &&
                   IsFinite(value.b) &&
                   IsFinite(value.a);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
