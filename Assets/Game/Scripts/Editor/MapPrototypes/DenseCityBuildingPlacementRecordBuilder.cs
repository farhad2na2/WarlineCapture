using System;
using System.Collections.Generic;
using Game.Configs;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityBuildingPlacementRecordRequest
    {
        internal DenseCityBuildingPlacementRecordRequest(
            string generatorSchema,
            int seed,
            int districtId,
            int sequenceStart,
            GameObject intactPrefab,
            GameObject destroyedPrefab,
            DenseCityBuildingMaterialSelection materialSelection,
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
            uint movementMask,
            int surfaceLayer,
            Vector2Int chunk,
            string identityKindPrefix = null)
        {
            GeneratorSchema = generatorSchema;
            Seed = seed;
            DistrictId = districtId;
            SequenceStart = sequenceStart;
            IntactPrefab = intactPrefab;
            DestroyedPrefab = destroyedPrefab;
            MaterialSelection = materialSelection;
            WorldMatrix = worldMatrix;
            OriginCell = originCell;
            FootprintCells = footprintCells;
            FootprintSize = footprintSize;
            FoundationElevation = foundationElevation;
            BlockerBounds = blockerBounds;
            FrontageDirection = frontageDirection;
            Role = role;
            DefinitionConfigAssetGuid = definitionConfigAssetGuid;
            FactionId = factionId;
            MaximumHealth = maximumHealth;
            MovementMask = movementMask;
            SurfaceLayer = surfaceLayer;
            Chunk = chunk;
            IdentityKindPrefix = identityKindPrefix;
        }

        internal string GeneratorSchema { get; }
        internal int Seed { get; }
        internal int DistrictId { get; }
        internal int SequenceStart { get; }
        internal GameObject IntactPrefab { get; }
        internal GameObject DestroyedPrefab { get; }
        internal DenseCityBuildingMaterialSelection MaterialSelection { get; }
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
        internal uint MovementMask { get; }
        internal int SurfaceLayer { get; }
        internal Vector2Int Chunk { get; }
        internal string IdentityKindPrefix { get; }
    }

    internal static class DenseCityBuildingPlacementRecordBuilder
    {
        internal static DenseCityBuildingRecordGroup Create(
            DenseCityBuildingPlacementRecordRequest request,
            DenseCityBuildingMaterialLibrary materialLibrary)
        {
            if (request.IntactPrefab == null)
                throw new ArgumentNullException(nameof(request.IntactPrefab));
            if (request.DestroyedPrefab == null)
                throw new ArgumentNullException(nameof(request.DestroyedPrefab));
            if (materialLibrary == null)
                throw new ArgumentNullException(nameof(materialLibrary));

            DenseCityVisualAssetMetadata intactMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(
                    request.IntactPrefab,
                    material => materialLibrary.Resolve(material, request.MaterialSelection),
                    renderer => DenseCityBuildingIntactVisualPolicy.ShouldIncludeRenderer(
                        request.IntactPrefab,
                        renderer));
            DenseCityVisualAssetMetadata destroyedMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(request.DestroyedPrefab);
            var input = new DenseCityBuildingRecordInput(
                request.GeneratorSchema,
                request.Seed,
                request.DistrictId,
                request.SequenceStart,
                intactMetadata.PrefabAssetGuid,
                intactMetadata.PrefabLocalId,
                destroyedMetadata.PrefabAssetGuid,
                destroyedMetadata.PrefabLocalId,
                Copy(intactMetadata.MaterialAssetGuids),
                Copy(destroyedMetadata.MaterialAssetGuids),
                request.WorldMatrix,
                request.OriginCell,
                request.FootprintCells,
                request.FootprintSize,
                request.FoundationElevation,
                request.BlockerBounds,
                request.FrontageDirection,
                request.Role,
                request.DefinitionConfigAssetGuid,
                request.FactionId,
                request.MaximumHealth,
                request.MovementMask,
                request.SurfaceLayer,
                request.Chunk,
                request.IdentityKindPrefix);
            return DenseCityBuildingRecordFactory.Create(input);
        }

        private static string[] Copy(IReadOnlyList<string> values)
        {
            var copy = new string[values.Count];
            for (int index = 0; index < values.Count; index++)
                copy[index] = values[index];
            return copy;
        }
    }
}
