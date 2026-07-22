using System;
using System.Collections.Generic;
using Game.Components;
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
            Vector2 footprintSize,
            float foundationElevation,
            Bounds blockerBounds,
            Vector3 frontageDirection,
            int factionId,
            float maximumHealth,
            OperationMapBuildingBlockerPolicy blockerPolicy,
            DenseCityRecordIdentity foundationSurfaceIdentity,
            DenseCityRecordIdentity blockerSurfaceIdentity,
            DenseCityRecordIdentity intactPresentationIdentity,
            DenseCityRecordIdentity destroyedPresentationIdentity)
        {
            RequireFiniteMatrix(worldMatrix, nameof(worldMatrix));
            if (!IsFinite(footprintSize) || footprintSize.x <= 0f || footprintSize.y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(footprintSize));
            if (!float.IsFinite(foundationElevation))
                throw new ArgumentOutOfRangeException(nameof(foundationElevation));
            if (!IsFinite(blockerBounds.center) || !IsFinite(blockerBounds.size) ||
                blockerBounds.size.x <= 0f || blockerBounds.size.y <= 0f || blockerBounds.size.z <= 0f)
                throw new ArgumentOutOfRangeException(nameof(blockerBounds));
            if (!IsFinite(frontageDirection) || frontageDirection.sqrMagnitude <= 0.000001f)
                throw new ArgumentOutOfRangeException(nameof(frontageDirection));
            if (factionId < 0)
                throw new ArgumentOutOfRangeException(nameof(factionId));
            if (!float.IsFinite(maximumHealth) || maximumHealth <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            if (blockerPolicy != OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked)
                throw new ArgumentOutOfRangeException(nameof(blockerPolicy));

            Identity = identity;
            WorldMatrix = worldMatrix;
            FootprintSize = footprintSize;
            FoundationElevation = foundationElevation;
            BlockerBounds = blockerBounds;
            FrontageDirection = frontageDirection.normalized;
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
        internal Vector2 FootprintSize { get; }
        internal float FoundationElevation { get; }
        internal Bounds BlockerBounds { get; }
        internal Vector3 FrontageDirection { get; }
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
            if (movementMask == 0)
                throw new ArgumentOutOfRangeException(nameof(movementMask));
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
