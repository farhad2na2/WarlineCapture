using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using UnityEngine;

namespace Game.Editor
{
    internal enum DenseCityPresentationCategory : byte
    {
        Unknown = 0,
        GameplayBuildingIntact = 1,
        GameplayBuildingDestroyed = 2,
        BuildingAttachmentIntact = 3,
        BuildingAttachmentDestroyed = 4,
        Infrastructure = 5,
        Vegetation = 6,
        Prop = 7,
        Horizon = 8
    }

    internal enum DenseCitySurfaceRecordKind : byte
    {
        Unknown = 0,
        Terrain = 1,
        Road = 2,
        Bridge = 3,
        Ramp = 4,
        Blocker = 5
    }

    internal readonly struct DenseCityRecordIdentity : IComparable<DenseCityRecordIdentity>
    {
        internal DenseCityRecordIdentity(
            string generatorSchema,
            int seed,
            int districtId,
            string kind,
            int deterministicSequence,
            string sourceAssetGuid,
            long sourceLocalId)
        {
            if (string.IsNullOrWhiteSpace(generatorSchema))
                throw new ArgumentException("Generator schema is required.", nameof(generatorSchema));
            if (districtId < 0)
                throw new ArgumentOutOfRangeException(nameof(districtId));
            if (!IsCanonicalKind(kind))
                throw new ArgumentException("Record kind must be lowercase kebab-case.", nameof(kind));
            if (deterministicSequence < 0)
                throw new ArgumentOutOfRangeException(nameof(deterministicSequence));
            if (!IsLowerHexGuid(sourceAssetGuid))
                throw new ArgumentException("Source asset GUID must be 32 lowercase hexadecimal characters.", nameof(sourceAssetGuid));
            if (sourceLocalId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceLocalId));

            GeneratorSchema = generatorSchema;
            Seed = seed;
            DistrictId = districtId;
            Kind = kind;
            DeterministicSequence = deterministicSequence;
            SourceAssetGuid = sourceAssetGuid;
            SourceLocalId = sourceLocalId;
            StableKey = string.Concat(
                generatorSchema, ":", seed.ToString("D10"), ":", districtId.ToString("D6"), ":",
                kind, ":", deterministicSequence.ToString("D10"), ":", sourceAssetGuid, ":",
                sourceLocalId.ToString("D20"));
        }

        internal string GeneratorSchema { get; }
        internal int Seed { get; }
        internal int DistrictId { get; }
        internal string Kind { get; }
        internal int DeterministicSequence { get; }
        internal string SourceAssetGuid { get; }
        internal long SourceLocalId { get; }
        internal string StableKey { get; }

        internal string CreateBakedStableId()
        {
            byte[] input = Encoding.UTF8.GetBytes(StableKey);
            byte[] hash;
            using (SHA256 sha = SHA256.Create())
                hash = sha.ComputeHash(input);
            var characters = new char[10 + hash.Length * 2];
            const string prefix = "densecity.";
            prefix.CopyTo(0, characters, 0, prefix.Length);
            const string hexadecimal = "0123456789abcdef";
            for (int index = 0; index < hash.Length; index++)
            {
                characters[prefix.Length + index * 2] = hexadecimal[hash[index] >> 4];
                characters[prefix.Length + index * 2 + 1] = hexadecimal[hash[index] & 0x0f];
            }
            return new string(characters);
        }

        public int CompareTo(DenseCityRecordIdentity other) =>
            string.Compare(StableKey, other.StableKey, StringComparison.Ordinal);

        private static bool IsCanonicalKind(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 48 || value[0] == '-' || value[^1] == '-')
                return false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!((character >= 'a' && character <= 'z') ||
                      (character >= '0' && character <= '9') || character == '-'))
                    return false;
            }
            return true;
        }

        private static bool IsLowerHexGuid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                return false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f')))
                    return false;
            }
            return true;
        }
    }

    internal readonly struct DenseCityBuildingBakeRecord
    {
        internal DenseCityBuildingBakeRecord(
            DenseCityRecordIdentity identity,
            Matrix4x4 worldMatrix,
            Vector2Int originCell,
            Vector2Int footprintCells,
            Vector2 footprintSize,
            float foundationElevation,
            Bounds blockerBounds,
            Vector3 frontageDirection,
            GeneratedCityBuildingRole role,
            string definitionConfigAssetGuid,
            int factionId,
            float maximumHealth,
            OperationMapBuildingBlockerPolicy blockerPolicy,
            DenseCityRecordIdentity foundationSurfaceIdentity,
            DenseCityRecordIdentity blockerSurfaceIdentity,
            DenseCityRecordIdentity intactPresentationIdentity,
            DenseCityRecordIdentity destroyedPresentationIdentity)
        {
            RequireFiniteMatrix(worldMatrix, nameof(worldMatrix));
            if (originCell.x < 0 || originCell.y < 0)
                throw new ArgumentOutOfRangeException(nameof(originCell));
            if (footprintCells.x <= 0 || footprintCells.y <= 0)
                throw new ArgumentOutOfRangeException(nameof(footprintCells));
            if (!IsFinite(footprintSize) || footprintSize.x <= 0f || footprintSize.y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(footprintSize));
            if (!float.IsFinite(foundationElevation))
                throw new ArgumentOutOfRangeException(nameof(foundationElevation));
            if (!IsFinite(blockerBounds.center) || !IsFinite(blockerBounds.size) ||
                blockerBounds.size.x <= 0f || blockerBounds.size.y <= 0f || blockerBounds.size.z <= 0f)
                throw new ArgumentOutOfRangeException(nameof(blockerBounds));
            if (!IsFinite(frontageDirection) || frontageDirection.sqrMagnitude <= 0.000001f)
                throw new ArgumentOutOfRangeException(nameof(frontageDirection));
            if (role is <= GeneratedCityBuildingRole.None or > GeneratedCityBuildingRole.Other)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (!IsLowerHexGuid(definitionConfigAssetGuid))
                throw new ArgumentException(
                    "Building definition config GUID must be 32 lowercase hexadecimal characters.",
                    nameof(definitionConfigAssetGuid));
            if (factionId < 0)
                throw new ArgumentOutOfRangeException(nameof(factionId));
            if (!float.IsFinite(maximumHealth) || maximumHealth <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            if (blockerPolicy != OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked)
                throw new ArgumentOutOfRangeException(nameof(blockerPolicy));

            Identity = identity;
            WorldMatrix = worldMatrix;
            OriginCell = originCell;
            FootprintCells = footprintCells;
            FootprintSize = footprintSize;
            FoundationElevation = foundationElevation;
            BlockerBounds = blockerBounds;
            FrontageDirection = frontageDirection.normalized;
            Role = role;
            DefinitionConfigAssetGuid = definitionConfigAssetGuid;
            FactionId = factionId;
            MaximumHealth = maximumHealth;
            BlockerPolicy = blockerPolicy;
            FoundationSurfaceIdentity = foundationSurfaceIdentity;
            BlockerSurfaceIdentity = blockerSurfaceIdentity;
            IntactPresentationIdentity = intactPresentationIdentity;
            DestroyedPresentationIdentity = destroyedPresentationIdentity;
        }

        internal DenseCityRecordIdentity Identity { get; }
        internal Matrix4x4 WorldMatrix { get; }
        internal Vector2Int OriginCell { get; }
        internal Vector2Int FootprintCells { get; }
        internal Vector2 FootprintSize { get; }
        internal float FoundationElevation { get; }
        internal Bounds BlockerBounds { get; }
        internal Vector3 FrontageDirection { get; }
        internal GeneratedCityBuildingRole Role { get; }
        internal string DefinitionConfigAssetGuid { get; }
        internal int FactionId { get; }
        internal float MaximumHealth { get; }
        internal OperationMapBuildingBlockerPolicy BlockerPolicy { get; }
        internal DenseCityRecordIdentity FoundationSurfaceIdentity { get; }
        internal DenseCityRecordIdentity BlockerSurfaceIdentity { get; }
        internal DenseCityRecordIdentity IntactPresentationIdentity { get; }
        internal DenseCityRecordIdentity DestroyedPresentationIdentity { get; }

        private static void RequireFiniteMatrix(Matrix4x4 matrix, string argumentName)
        {
            for (int index = 0; index < 16; index++)
            {
                if (!float.IsFinite(matrix[index]))
                    throw new ArgumentOutOfRangeException(argumentName);
            }
        }

        private static bool IsFinite(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y);

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private static bool IsLowerHexGuid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                return false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f')))
                    return false;
            }
            return true;
        }
    }

    internal readonly struct DenseCitySurfaceBakeRecord
    {
        private readonly Vector2[] polygon;

        internal DenseCitySurfaceBakeRecord(
            DenseCityRecordIdentity identity,
            DenseCitySurfaceRecordKind kind,
            IReadOnlyList<Vector2> polygon,
            float elevation,
            uint movementMask,
            int layer,
            Vector2Int chunk)
        {
            if (kind is <= DenseCitySurfaceRecordKind.Unknown or > DenseCitySurfaceRecordKind.Blocker)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (polygon == null || polygon.Count < 3 || polygon.Count > 64)
                throw new ArgumentOutOfRangeException(nameof(polygon), "Surface polygon requires 3-64 vertices.");
            this.polygon = new Vector2[polygon.Count];
            for (int index = 0; index < polygon.Count; index++)
            {
                Vector2 point = polygon[index];
                if (!float.IsFinite(point.x) || !float.IsFinite(point.y))
                    throw new ArgumentOutOfRangeException(nameof(polygon));
                this.polygon[index] = point;
            }
            if (!float.IsFinite(elevation))
                throw new ArgumentOutOfRangeException(nameof(elevation));
            if (kind == DenseCitySurfaceRecordKind.Blocker)
            {
                if (movementMask != 0)
                    throw new ArgumentOutOfRangeException(nameof(movementMask));
            }
            else if (movementMask == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(movementMask));
            }
            if (layer < 0 || layer > 31)
                throw new ArgumentOutOfRangeException(nameof(layer));

            Identity = identity;
            Kind = kind;
            Elevation = elevation;
            MovementMask = movementMask;
            Layer = layer;
            Chunk = chunk;
        }

        internal DenseCityRecordIdentity Identity { get; }
        internal DenseCitySurfaceRecordKind Kind { get; }
        internal ReadOnlyMemory<Vector2> Polygon => polygon;
        internal float Elevation { get; }
        internal uint MovementMask { get; }
        internal int Layer { get; }
        internal Vector2Int Chunk { get; }
    }

    internal readonly struct DenseCityPresentationBakeRecord
    {
        private readonly string[] materialAssetGuids;

        internal DenseCityPresentationBakeRecord(
            DenseCityRecordIdentity identity,
            DenseCityPresentationCategory category,
            string prefabAssetGuid,
            string meshAssetGuid,
            IReadOnlyList<string> materialAssetGuids,
            Matrix4x4 worldMatrix,
            bool castsShadows,
            bool batchingEligible,
            byte lodImportance,
            string buildingOwnerStableKey = null)
        {
            if (category is <= DenseCityPresentationCategory.Unknown or > DenseCityPresentationCategory.Horizon)
                throw new ArgumentOutOfRangeException(nameof(category));
            if (!IsOptionalGuid(prefabAssetGuid) || !IsOptionalGuid(meshAssetGuid) ||
                string.IsNullOrEmpty(prefabAssetGuid) == string.IsNullOrEmpty(meshAssetGuid))
                throw new ArgumentException("Exactly one prefab or mesh source GUID is required.");
            if (materialAssetGuids == null || materialAssetGuids.Count == 0 || materialAssetGuids.Count > 16)
                throw new ArgumentOutOfRangeException(nameof(materialAssetGuids));
            this.materialAssetGuids = new string[materialAssetGuids.Count];
            for (int index = 0; index < materialAssetGuids.Count; index++)
            {
                string materialGuid = materialAssetGuids[index];
                if (!IsGuid(materialGuid))
                    throw new ArgumentException("Material GUID is malformed.", nameof(materialAssetGuids));
                this.materialAssetGuids[index] = materialGuid;
            }
            for (int index = 0; index < 16; index++)
            {
                if (!float.IsFinite(worldMatrix[index]))
                    throw new ArgumentOutOfRangeException(nameof(worldMatrix));
            }
            bool isAttachment = category is DenseCityPresentationCategory.BuildingAttachmentIntact or
                DenseCityPresentationCategory.BuildingAttachmentDestroyed;
            if (isAttachment == string.IsNullOrWhiteSpace(buildingOwnerStableKey))
                throw new ArgumentException("Only building attachments require a building owner stable key.");

            Identity = identity;
            Category = category;
            PrefabAssetGuid = prefabAssetGuid;
            MeshAssetGuid = meshAssetGuid;
            WorldMatrix = worldMatrix;
            CastsShadows = castsShadows;
            BatchingEligible = batchingEligible;
            LodImportance = lodImportance;
            BuildingOwnerStableKey = buildingOwnerStableKey;
        }

        internal DenseCityRecordIdentity Identity { get; }
        internal DenseCityPresentationCategory Category { get; }
        internal string PrefabAssetGuid { get; }
        internal string MeshAssetGuid { get; }
        internal ReadOnlyMemory<string> MaterialAssetGuids => materialAssetGuids;
        internal Matrix4x4 WorldMatrix { get; }
        internal bool CastsShadows { get; }
        internal bool BatchingEligible { get; }
        internal byte LodImportance { get; }
        internal string BuildingOwnerStableKey { get; }

        private static bool IsOptionalGuid(string value) => string.IsNullOrEmpty(value) || IsGuid(value);

        private static bool IsGuid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                return false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f')))
                    return false;
            }
            return true;
        }
    }

    internal sealed class DenseCityGenerationRecordSet : IDisposable
    {
        private readonly List<DenseCityBuildingBakeRecord> buildings;
        private readonly List<DenseCitySurfaceBakeRecord> surfaces;
        private readonly List<DenseCityPresentationBakeRecord> presentations;
        private readonly HashSet<string> stableKeys;
        private readonly int buildingCapacity;
        private readonly int surfaceCapacity;
        private readonly int presentationCapacity;
        private bool sealedForRead;
        private bool disposed;

        internal DenseCityGenerationRecordSet(
            int buildingCapacity,
            int surfaceCapacity,
            int presentationCapacity)
        {
            this.buildingCapacity = RequireCapacity(buildingCapacity, nameof(buildingCapacity));
            this.surfaceCapacity = RequireCapacity(surfaceCapacity, nameof(surfaceCapacity));
            this.presentationCapacity = RequireCapacity(presentationCapacity, nameof(presentationCapacity));
            buildings = new List<DenseCityBuildingBakeRecord>(buildingCapacity);
            surfaces = new List<DenseCitySurfaceBakeRecord>(surfaceCapacity);
            presentations = new List<DenseCityPresentationBakeRecord>(presentationCapacity);
            stableKeys = new HashSet<string>(
                buildingCapacity + surfaceCapacity + presentationCapacity,
                StringComparer.Ordinal);
        }

        internal IReadOnlyList<DenseCityBuildingBakeRecord> Buildings => RequireSealed(buildings);
        internal IReadOnlyList<DenseCitySurfaceBakeRecord> Surfaces => RequireSealed(surfaces);
        internal IReadOnlyList<DenseCityPresentationBakeRecord> Presentations => RequireSealed(presentations);

        internal void Add(DenseCityBuildingBakeRecord record) =>
            Add(record, record.Identity, buildings, buildingCapacity, "building");

        internal void Add(DenseCitySurfaceBakeRecord record) =>
            Add(record, record.Identity, surfaces, surfaceCapacity, "surface");

        internal void Add(DenseCityPresentationBakeRecord record) =>
            Add(record, record.Identity, presentations, presentationCapacity, "presentation");

        internal void AddRenderOnlyPresentation(DenseCityPresentationBakeRecord presentation)
        {
            DenseCityRenderOnlyPresentationRecordFactory.RequireRenderOnlyCategory(presentation.Category);
            Add(presentation);
        }

        internal void RemoveRenderOnlyPresentation(DenseCityPresentationBakeRecord presentation)
        {
            RequireWritable();
            DenseCityRenderOnlyPresentationRecordFactory.RequireRenderOnlyCategory(presentation.Category);
            int index = FindIndex(presentations, presentation.Identity.StableKey, record => record.Identity);
            if (index < 0)
            {
                throw new InvalidOperationException(
                    $"Dense-city render-only presentation is missing: '{presentation.Identity.StableKey}'.");
            }
            stableKeys.Remove(presentation.Identity.StableKey);
            presentations.RemoveAt(index);
        }

        internal void AddBuildingAttachment(DenseCityPresentationBakeRecord attachment)
        {
            RequireWritable();
            if (attachment.Category is not (DenseCityPresentationCategory.BuildingAttachmentIntact or
                DenseCityPresentationCategory.BuildingAttachmentDestroyed))
            {
                throw new ArgumentException(
                    "Building attachment presentation category is required.",
                    nameof(attachment));
            }
            if (FindIndex(buildings, attachment.BuildingOwnerStableKey, record => record.Identity) < 0)
            {
                throw new InvalidOperationException(
                    $"Dense-city attachment owner is not committed: '{attachment.BuildingOwnerStableKey}'.");
            }

            Add(attachment);
        }

        internal void RemoveBuildingAttachment(DenseCityPresentationBakeRecord attachment)
        {
            RequireWritable();
            int index = FindIndex(presentations, attachment.Identity.StableKey, record => record.Identity);
            if (index < 0)
            {
                throw new InvalidOperationException(
                    $"Dense-city attachment record is missing: '{attachment.Identity.StableKey}'.");
            }
            stableKeys.Remove(attachment.Identity.StableKey);
            presentations.RemoveAt(index);
        }

        internal void AddInfrastructureGroup(
            DenseCitySurfaceBakeRecord surface,
            DenseCityPresentationBakeRecord presentation)
        {
            RequireWritable();
            if (surface.Kind is DenseCitySurfaceRecordKind.Unknown or DenseCitySurfaceRecordKind.Blocker)
            {
                throw new ArgumentException(
                    "Infrastructure surfaces must be terrain, road, bridge, or ramp records.",
                    nameof(surface));
            }
            if (presentation.Category != DenseCityPresentationCategory.Infrastructure)
            {
                throw new ArgumentException(
                    "Infrastructure presentation category is required.",
                    nameof(presentation));
            }
            if (surfaces.Count >= surfaceCapacity || presentations.Count >= presentationCapacity)
                throw new InvalidOperationException("Dense-city infrastructure record group exceeds a configured capacity.");

            string surfaceKey = surface.Identity.StableKey;
            string presentationKey = presentation.Identity.StableKey;
            if (surfaceKey == presentationKey || stableKeys.Contains(surfaceKey) || stableKeys.Contains(presentationKey))
            {
                throw new InvalidOperationException(
                    $"Duplicate dense-city record identity: '{(stableKeys.Contains(surfaceKey) ? surfaceKey : presentationKey)}'.");
            }

            stableKeys.Add(surfaceKey);
            stableKeys.Add(presentationKey);
            surfaces.Add(surface);
            presentations.Add(presentation);
        }

        internal void AddVisualBlockerGroup(
            DenseCitySurfaceBakeRecord blocker,
            DenseCityPresentationBakeRecord presentation)
        {
            RequireWritable();
            if (blocker.Kind != DenseCitySurfaceRecordKind.Blocker || blocker.MovementMask != 0)
                throw new ArgumentException("Visual blocker requires a non-traversable blocker record.", nameof(blocker));
            if (presentation.Category != DenseCityPresentationCategory.Infrastructure)
                throw new ArgumentException("Visual blocker presentation must be infrastructure.", nameof(presentation));
            if (surfaces.Count >= surfaceCapacity || presentations.Count >= presentationCapacity)
                throw new InvalidOperationException("Dense-city visual blocker record group exceeds a configured capacity.");

            string blockerKey = blocker.Identity.StableKey;
            string presentationKey = presentation.Identity.StableKey;
            if (blockerKey == presentationKey || stableKeys.Contains(blockerKey) || stableKeys.Contains(presentationKey))
            {
                throw new InvalidOperationException(
                    $"Duplicate dense-city record identity: '{(stableKeys.Contains(blockerKey) ? blockerKey : presentationKey)}'.");
            }

            stableKeys.Add(blockerKey);
            stableKeys.Add(presentationKey);
            surfaces.Add(blocker);
            presentations.Add(presentation);
        }

        internal void RemoveVisualBlockerGroup(
            DenseCitySurfaceBakeRecord blocker,
            DenseCityPresentationBakeRecord presentation)
        {
            RequireWritable();
            int blockerIndex = FindIndex(surfaces, blocker.Identity.StableKey, record => record.Identity);
            int presentationIndex = FindIndex(
                presentations,
                presentation.Identity.StableKey,
                record => record.Identity);
            if (blockerIndex < 0 || presentationIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Dense-city visual blocker record group is incomplete: '{blocker.Identity.StableKey}'.");
            }

            stableKeys.Remove(blocker.Identity.StableKey);
            stableKeys.Remove(presentation.Identity.StableKey);
            surfaces.RemoveAt(blockerIndex);
            presentations.RemoveAt(presentationIndex);
        }

        internal void RemoveInfrastructureGroup(
            DenseCitySurfaceBakeRecord surface,
            DenseCityPresentationBakeRecord presentation)
        {
            RequireWritable();
            int surfaceIndex = FindIndex(surfaces, surface.Identity.StableKey, record => record.Identity);
            int presentationIndex = FindIndex(
                presentations,
                presentation.Identity.StableKey,
                record => record.Identity);
            if (surfaceIndex < 0 || presentationIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Dense-city infrastructure record group is incomplete: '{surface.Identity.StableKey}'.");
            }

            stableKeys.Remove(surface.Identity.StableKey);
            stableKeys.Remove(presentation.Identity.StableKey);
            surfaces.RemoveAt(surfaceIndex);
            presentations.RemoveAt(presentationIndex);
        }

        internal void AddCanalWaterGroup(
            DenseCitySurfaceBakeRecord exclusion,
            DenseCityPresentationBakeRecord bedPresentation,
            DenseCityPresentationBakeRecord waterPresentation)
        {
            RequireWritable();
            if (exclusion.Kind != DenseCitySurfaceRecordKind.Blocker || exclusion.MovementMask != 0)
                throw new ArgumentException("Canal water requires a non-traversable blocker record.", nameof(exclusion));
            if (bedPresentation.Category != DenseCityPresentationCategory.Infrastructure)
                throw new ArgumentException("Canal bed presentation must be infrastructure.", nameof(bedPresentation));
            if (waterPresentation.Category != DenseCityPresentationCategory.Infrastructure)
                throw new ArgumentException("Canal water presentation must be infrastructure.", nameof(waterPresentation));
            if (surfaces.Count >= surfaceCapacity || presentations.Count > presentationCapacity - 2)
                throw new InvalidOperationException("Dense-city canal water record group exceeds a configured capacity.");

            var pendingKeys = new HashSet<string>(StringComparer.Ordinal);
            RequireUniquePendingKey(exclusion.Identity.StableKey, pendingKeys);
            RequireUniquePendingKey(bedPresentation.Identity.StableKey, pendingKeys);
            RequireUniquePendingKey(waterPresentation.Identity.StableKey, pendingKeys);

            stableKeys.Add(exclusion.Identity.StableKey);
            stableKeys.Add(bedPresentation.Identity.StableKey);
            stableKeys.Add(waterPresentation.Identity.StableKey);
            surfaces.Add(exclusion);
            presentations.Add(bedPresentation);
            presentations.Add(waterPresentation);
        }

        internal void RemoveCanalWaterGroup(
            DenseCitySurfaceBakeRecord exclusion,
            DenseCityPresentationBakeRecord bedPresentation,
            DenseCityPresentationBakeRecord waterPresentation)
        {
            RequireWritable();
            int exclusionIndex = FindIndex(surfaces, exclusion.Identity.StableKey, record => record.Identity);
            int bedIndex = FindIndex(
                presentations,
                bedPresentation.Identity.StableKey,
                record => record.Identity);
            int waterIndex = FindIndex(
                presentations,
                waterPresentation.Identity.StableKey,
                record => record.Identity);
            if (exclusionIndex < 0 || bedIndex < 0 || waterIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Dense-city canal water record group is incomplete: '{exclusion.Identity.StableKey}'.");
            }

            stableKeys.Remove(exclusion.Identity.StableKey);
            stableKeys.Remove(bedPresentation.Identity.StableKey);
            stableKeys.Remove(waterPresentation.Identity.StableKey);
            surfaces.RemoveAt(exclusionIndex);
            int firstPresentationIndex = Math.Max(bedIndex, waterIndex);
            int secondPresentationIndex = Math.Min(bedIndex, waterIndex);
            presentations.RemoveAt(firstPresentationIndex);
            presentations.RemoveAt(secondPresentationIndex);
        }

        internal void AddTerrainVisualGroup(
            DenseCitySurfaceBakeRecord terrain,
            IReadOnlyList<DenseCityPresentationBakeRecord> visualPresentations)
        {
            RequireWritable();
            if (terrain.Kind != DenseCitySurfaceRecordKind.Terrain)
                throw new ArgumentException("Terrain surface record is required.", nameof(terrain));
            if (visualPresentations == null || visualPresentations.Count == 0 || visualPresentations.Count > 16)
                throw new ArgumentOutOfRangeException(nameof(visualPresentations));
            if (surfaces.Count >= surfaceCapacity ||
                presentations.Count > presentationCapacity - visualPresentations.Count)
            {
                throw new InvalidOperationException("Dense-city terrain visual group exceeds a configured capacity.");
            }

            var pendingKeys = new HashSet<string>(StringComparer.Ordinal);
            RequireUniquePendingKey(terrain.Identity.StableKey, pendingKeys);
            for (int index = 0; index < visualPresentations.Count; index++)
            {
                DenseCityPresentationBakeRecord presentation = visualPresentations[index];
                if (presentation.Category != DenseCityPresentationCategory.Infrastructure)
                {
                    throw new ArgumentException(
                        "Terrain visual presentations must be infrastructure.",
                        nameof(visualPresentations));
                }
                RequireUniquePendingKey(presentation.Identity.StableKey, pendingKeys);
            }

            stableKeys.Add(terrain.Identity.StableKey);
            surfaces.Add(terrain);
            for (int index = 0; index < visualPresentations.Count; index++)
            {
                DenseCityPresentationBakeRecord presentation = visualPresentations[index];
                stableKeys.Add(presentation.Identity.StableKey);
                presentations.Add(presentation);
            }
        }

        internal void RemoveTerrainVisualGroup(
            DenseCitySurfaceBakeRecord terrain,
            IReadOnlyList<DenseCityPresentationBakeRecord> visualPresentations)
        {
            RequireWritable();
            if (visualPresentations == null || visualPresentations.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(visualPresentations));

            int terrainIndex = FindIndex(surfaces, terrain.Identity.StableKey, record => record.Identity);
            var presentationIndices = new int[visualPresentations.Count];
            for (int index = 0; index < visualPresentations.Count; index++)
            {
                presentationIndices[index] = FindIndex(
                    presentations,
                    visualPresentations[index].Identity.StableKey,
                    record => record.Identity);
            }
            if (terrainIndex < 0 || Array.Exists(presentationIndices, index => index < 0))
            {
                throw new InvalidOperationException(
                    $"Dense-city terrain visual group is incomplete: '{terrain.Identity.StableKey}'.");
            }

            stableKeys.Remove(terrain.Identity.StableKey);
            surfaces.RemoveAt(terrainIndex);
            Array.Sort(presentationIndices);
            for (int index = presentationIndices.Length - 1; index >= 0; index--)
            {
                int presentationIndex = presentationIndices[index];
                stableKeys.Remove(presentations[presentationIndex].Identity.StableKey);
                presentations.RemoveAt(presentationIndex);
            }
        }

        internal void AddBridgeGroup(
            DenseCitySurfaceBakeRecord bridge,
            DenseCityPresentationBakeRecord presentation,
            DenseCitySurfaceBakeRecord firstApproachRamp,
            DenseCitySurfaceBakeRecord secondApproachRamp)
        {
            RequireWritable();
            if (bridge.Kind != DenseCitySurfaceRecordKind.Bridge)
                throw new ArgumentException("Bridge surface record is required.", nameof(bridge));
            if (presentation.Category != DenseCityPresentationCategory.Infrastructure)
                throw new ArgumentException("Infrastructure presentation category is required.", nameof(presentation));
            if (firstApproachRamp.Kind != DenseCitySurfaceRecordKind.Ramp)
                throw new ArgumentException("First approach must be a ramp surface record.", nameof(firstApproachRamp));
            if (secondApproachRamp.Kind != DenseCitySurfaceRecordKind.Ramp)
                throw new ArgumentException("Second approach must be a ramp surface record.", nameof(secondApproachRamp));
            if (surfaces.Count > surfaceCapacity - 3 || presentations.Count >= presentationCapacity)
                throw new InvalidOperationException("Dense-city bridge record group exceeds a configured capacity.");

            string[] keys =
            {
                bridge.Identity.StableKey,
                presentation.Identity.StableKey,
                firstApproachRamp.Identity.StableKey,
                secondApproachRamp.Identity.StableKey
            };
            var pendingKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < keys.Length; index++)
            {
                string key = keys[index];
                if (!pendingKeys.Add(key) || stableKeys.Contains(key))
                    throw new InvalidOperationException($"Duplicate dense-city record identity: '{key}'.");
            }

            for (int index = 0; index < keys.Length; index++)
                stableKeys.Add(keys[index]);
            surfaces.Add(bridge);
            surfaces.Add(firstApproachRamp);
            surfaces.Add(secondApproachRamp);
            presentations.Add(presentation);
        }

        internal void RemoveBridgeGroup(
            DenseCitySurfaceBakeRecord bridge,
            DenseCityPresentationBakeRecord presentation,
            DenseCitySurfaceBakeRecord firstApproachRamp,
            DenseCitySurfaceBakeRecord secondApproachRamp)
        {
            RequireWritable();
            int bridgeIndex = FindIndex(surfaces, bridge.Identity.StableKey, record => record.Identity);
            int firstRampIndex = FindIndex(
                surfaces,
                firstApproachRamp.Identity.StableKey,
                record => record.Identity);
            int secondRampIndex = FindIndex(
                surfaces,
                secondApproachRamp.Identity.StableKey,
                record => record.Identity);
            int presentationIndex = FindIndex(
                presentations,
                presentation.Identity.StableKey,
                record => record.Identity);
            if (bridgeIndex < 0 || firstRampIndex < 0 || secondRampIndex < 0 || presentationIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Dense-city bridge record group is incomplete: '{bridge.Identity.StableKey}'.");
            }

            int[] surfaceIndices = { bridgeIndex, firstRampIndex, secondRampIndex };
            Array.Sort(surfaceIndices);
            for (int index = surfaceIndices.Length - 1; index >= 0; index--)
            {
                int surfaceIndex = surfaceIndices[index];
                stableKeys.Remove(surfaces[surfaceIndex].Identity.StableKey);
                surfaces.RemoveAt(surfaceIndex);
            }
            stableKeys.Remove(presentation.Identity.StableKey);
            presentations.RemoveAt(presentationIndex);
        }

        internal void AddRoadGroup(
            DenseCitySurfaceBakeRecord road,
            DenseCityPresentationBakeRecord presentation,
            IReadOnlyList<DenseCitySurfaceBakeRecord> shoulders)
        {
            RequireWritable();
            if (road.Kind != DenseCitySurfaceRecordKind.Road)
                throw new ArgumentException("Road surface record is required.", nameof(road));
            if (presentation.Category != DenseCityPresentationCategory.Infrastructure)
                throw new ArgumentException("Infrastructure presentation category is required.", nameof(presentation));
            if (shoulders == null)
                throw new ArgumentNullException(nameof(shoulders));
            for (int index = 0; index < shoulders.Count; index++)
            {
                if (shoulders[index].Kind != DenseCitySurfaceRecordKind.Terrain)
                    throw new ArgumentException("Road shoulders must be terrain surface records.", nameof(shoulders));
            }

            int requiredSurfaceCount = checked(1 + shoulders.Count);
            if (surfaces.Count > surfaceCapacity - requiredSurfaceCount ||
                presentations.Count >= presentationCapacity)
            {
                throw new InvalidOperationException("Dense-city road record group exceeds a configured capacity.");
            }

            var pendingKeys = new HashSet<string>(StringComparer.Ordinal);
            RequireUniquePendingKey(road.Identity.StableKey, pendingKeys);
            RequireUniquePendingKey(presentation.Identity.StableKey, pendingKeys);
            for (int index = 0; index < shoulders.Count; index++)
                RequireUniquePendingKey(shoulders[index].Identity.StableKey, pendingKeys);

            stableKeys.Add(road.Identity.StableKey);
            stableKeys.Add(presentation.Identity.StableKey);
            surfaces.Add(road);
            presentations.Add(presentation);
            for (int index = 0; index < shoulders.Count; index++)
            {
                DenseCitySurfaceBakeRecord shoulder = shoulders[index];
                stableKeys.Add(shoulder.Identity.StableKey);
                surfaces.Add(shoulder);
            }
        }

        internal void RemoveRoadGroup(
            DenseCitySurfaceBakeRecord road,
            DenseCityPresentationBakeRecord presentation,
            IReadOnlyList<DenseCitySurfaceBakeRecord> shoulders)
        {
            RequireWritable();
            if (shoulders == null)
                throw new ArgumentNullException(nameof(shoulders));

            var surfaceIndices = new int[1 + shoulders.Count];
            surfaceIndices[0] = FindIndex(surfaces, road.Identity.StableKey, record => record.Identity);
            for (int index = 0; index < shoulders.Count; index++)
            {
                surfaceIndices[index + 1] = FindIndex(
                    surfaces,
                    shoulders[index].Identity.StableKey,
                    record => record.Identity);
            }
            int presentationIndex = FindIndex(
                presentations,
                presentation.Identity.StableKey,
                record => record.Identity);
            for (int index = 0; index < surfaceIndices.Length; index++)
            {
                if (surfaceIndices[index] < 0)
                {
                    throw new InvalidOperationException(
                        $"Dense-city road record group is incomplete: '{road.Identity.StableKey}'.");
                }
            }
            if (presentationIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Dense-city road record group is incomplete: '{road.Identity.StableKey}'.");
            }

            Array.Sort(surfaceIndices);
            for (int index = surfaceIndices.Length - 1; index >= 0; index--)
            {
                int surfaceIndex = surfaceIndices[index];
                stableKeys.Remove(surfaces[surfaceIndex].Identity.StableKey);
                surfaces.RemoveAt(surfaceIndex);
            }
            stableKeys.Remove(presentation.Identity.StableKey);
            presentations.RemoveAt(presentationIndex);
        }

        internal void RemoveSurface(DenseCitySurfaceBakeRecord surface)
        {
            RequireWritable();
            int surfaceIndex = FindIndex(surfaces, surface.Identity.StableKey, record => record.Identity);
            if (surfaceIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Dense-city surface record is missing: '{surface.Identity.StableKey}'.");
            }

            stableKeys.Remove(surface.Identity.StableKey);
            surfaces.RemoveAt(surfaceIndex);
        }

        internal void AddBuildingGroup(
            DenseCityBuildingBakeRecord building,
            DenseCitySurfaceBakeRecord foundation,
            DenseCitySurfaceBakeRecord blocker,
            DenseCityPresentationBakeRecord intactPresentation,
            DenseCityPresentationBakeRecord destroyedPresentation)
        {
            RequireWritable();
            if (foundation.Kind != DenseCitySurfaceRecordKind.Terrain)
                throw new ArgumentException("Building foundation must be a terrain surface record.", nameof(foundation));
            if (blocker.Kind != DenseCitySurfaceRecordKind.Blocker)
                throw new ArgumentException("Building blocker must be a blocker surface record.", nameof(blocker));
            if (foundation.Identity.StableKey != building.FoundationSurfaceIdentity.StableKey)
                throw new ArgumentException("Building foundation identity mismatch.", nameof(foundation));
            if (blocker.Identity.StableKey != building.BlockerSurfaceIdentity.StableKey)
                throw new ArgumentException("Building blocker identity mismatch.", nameof(blocker));
            if (intactPresentation.Category != DenseCityPresentationCategory.GameplayBuildingIntact ||
                intactPresentation.Identity.StableKey != building.IntactPresentationIdentity.StableKey)
            {
                throw new ArgumentException("Building intact presentation identity/category mismatch.", nameof(intactPresentation));
            }
            if (destroyedPresentation.Category != DenseCityPresentationCategory.GameplayBuildingDestroyed ||
                destroyedPresentation.Identity.StableKey != building.DestroyedPresentationIdentity.StableKey)
            {
                throw new ArgumentException("Building destroyed presentation identity/category mismatch.", nameof(destroyedPresentation));
            }
            if (buildings.Count >= buildingCapacity || surfaces.Count > surfaceCapacity - 2 ||
                presentations.Count > presentationCapacity - 2)
            {
                throw new InvalidOperationException("Dense-city building record group exceeds a configured capacity.");
            }

            string[] keys =
            {
                building.Identity.StableKey,
                foundation.Identity.StableKey,
                blocker.Identity.StableKey,
                intactPresentation.Identity.StableKey,
                destroyedPresentation.Identity.StableKey
            };
            var pendingKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < keys.Length; index++)
            {
                string key = keys[index];
                if (!pendingKeys.Add(key) || stableKeys.Contains(key))
                    throw new InvalidOperationException($"Duplicate dense-city record identity: '{key}'.");
            }

            for (int index = 0; index < keys.Length; index++)
                stableKeys.Add(keys[index]);
            buildings.Add(building);
            surfaces.Add(foundation);
            surfaces.Add(blocker);
            presentations.Add(intactPresentation);
            presentations.Add(destroyedPresentation);
        }

        internal void RemoveBuildingGroup(DenseCityBuildingBakeRecord building)
        {
            RequireWritable();
            int buildingIndex = FindIndex(buildings, building.Identity.StableKey, record => record.Identity);
            int intactIndex = FindIndex(
                presentations,
                building.IntactPresentationIdentity.StableKey,
                record => record.Identity);
            int destroyedIndex = FindIndex(
                presentations,
                building.DestroyedPresentationIdentity.StableKey,
                record => record.Identity);
            int foundationIndex = FindIndex(
                surfaces,
                building.FoundationSurfaceIdentity.StableKey,
                record => record.Identity);
            int blockerIndex = FindIndex(
                surfaces,
                building.BlockerSurfaceIdentity.StableKey,
                record => record.Identity);
            if (buildingIndex < 0 || intactIndex < 0 || destroyedIndex < 0 ||
                foundationIndex < 0 || blockerIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Dense-city building record group is incomplete: '{building.Identity.StableKey}'.");
            }

            RemovePresentationIndices(intactIndex, destroyedIndex);
            RemoveSurfaceIndices(foundationIndex, blockerIndex);
            buildings.RemoveAt(buildingIndex);
            stableKeys.Remove(building.Identity.StableKey);
            stableKeys.Remove(building.IntactPresentationIdentity.StableKey);
            stableKeys.Remove(building.DestroyedPresentationIdentity.StableKey);
        }

        internal void Seal()
        {
            RequireWritable();
            buildings.Sort((left, right) => left.Identity.CompareTo(right.Identity));
            surfaces.Sort((left, right) => left.Identity.CompareTo(right.Identity));
            presentations.Sort((left, right) => left.Identity.CompareTo(right.Identity));
            sealedForRead = true;
        }

        public void Dispose()
        {
            if (disposed)
                return;
            buildings.Clear();
            surfaces.Clear();
            presentations.Clear();
            stableKeys.Clear();
            disposed = true;
            sealedForRead = false;
        }

        private void Add<T>(T record, DenseCityRecordIdentity identity, List<T> destination, int capacity, string kind)
        {
            RequireWritable();
            if (destination.Count >= capacity)
                throw new InvalidOperationException($"Dense-city {kind} record capacity {capacity} exceeded.");
            if (!stableKeys.Add(identity.StableKey))
                throw new InvalidOperationException($"Duplicate dense-city record identity: '{identity.StableKey}'.");
            destination.Add(record);
        }

        private static int FindIndex<T>(
            List<T> records,
            string stableKey,
            Func<T, DenseCityRecordIdentity> identitySelector)
        {
            for (int index = 0; index < records.Count; index++)
            {
                if (identitySelector(records[index]).StableKey == stableKey)
                    return index;
            }
            return -1;
        }

        private void RequireUniquePendingKey(string key, HashSet<string> pendingKeys)
        {
            if (!pendingKeys.Add(key) || stableKeys.Contains(key))
                throw new InvalidOperationException($"Duplicate dense-city record identity: '{key}'.");
        }

        private void RemovePresentationIndices(int first, int second)
        {
            int high = Math.Max(first, second);
            int low = Math.Min(first, second);
            stableKeys.Remove(presentations[high].Identity.StableKey);
            presentations.RemoveAt(high);
            stableKeys.Remove(presentations[low].Identity.StableKey);
            presentations.RemoveAt(low);
        }

        private void RemoveSurfaceIndices(int first, int second)
        {
            int high = Math.Max(first, second);
            int low = Math.Min(first, second);
            stableKeys.Remove(surfaces[high].Identity.StableKey);
            surfaces.RemoveAt(high);
            stableKeys.Remove(surfaces[low].Identity.StableKey);
            surfaces.RemoveAt(low);
        }

        private IReadOnlyList<T> RequireSealed<T>(List<T> records)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DenseCityGenerationRecordSet));
            if (!sealedForRead)
                throw new InvalidOperationException("Dense-city records must be sealed before reading.");
            return records;
        }

        private void RequireWritable()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DenseCityGenerationRecordSet));
            if (sealedForRead)
                throw new InvalidOperationException("Dense-city records are sealed.");
        }

        private static int RequireCapacity(int value, string argumentName)
        {
            if (value <= 0 || value > 1_000_000)
                throw new ArgumentOutOfRangeException(argumentName);
            return value;
        }
    }
}
