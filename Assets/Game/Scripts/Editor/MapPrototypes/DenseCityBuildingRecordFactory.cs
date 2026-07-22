using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityBuildingRecordInput
    {
        internal DenseCityBuildingRecordInput(
            string generatorSchema,
            int seed,
            int districtId,
            int sequenceStart,
            string intactPrefabGuid,
            long intactPrefabLocalId,
            string destroyedPrefabGuid,
            long destroyedPrefabLocalId,
            IReadOnlyList<string> intactMaterialGuids,
            IReadOnlyList<string> destroyedMaterialGuids,
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
            if (sequenceStart < 0 || sequenceStart > int.MaxValue - 4)
                throw new ArgumentOutOfRangeException(nameof(sequenceStart));

            GeneratorSchema = generatorSchema;
            Seed = seed;
            DistrictId = districtId;
            SequenceStart = sequenceStart;
            IntactPrefabGuid = intactPrefabGuid;
            IntactPrefabLocalId = intactPrefabLocalId;
            DestroyedPrefabGuid = destroyedPrefabGuid;
            DestroyedPrefabLocalId = destroyedPrefabLocalId;
            IntactMaterialGuids = intactMaterialGuids;
            DestroyedMaterialGuids = destroyedMaterialGuids;
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
        internal string IntactPrefabGuid { get; }
        internal long IntactPrefabLocalId { get; }
        internal string DestroyedPrefabGuid { get; }
        internal long DestroyedPrefabLocalId { get; }
        internal IReadOnlyList<string> IntactMaterialGuids { get; }
        internal IReadOnlyList<string> DestroyedMaterialGuids { get; }
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

    internal readonly struct DenseCityBuildingRecordGroup
    {
        internal DenseCityBuildingRecordGroup(
            DenseCityBuildingBakeRecord building,
            DenseCitySurfaceBakeRecord foundation,
            DenseCitySurfaceBakeRecord blocker,
            DenseCityPresentationBakeRecord intactPresentation,
            DenseCityPresentationBakeRecord destroyedPresentation)
        {
            Building = building;
            Foundation = foundation;
            Blocker = blocker;
            IntactPresentation = intactPresentation;
            DestroyedPresentation = destroyedPresentation;
        }

        internal DenseCityBuildingBakeRecord Building { get; }
        internal DenseCitySurfaceBakeRecord Foundation { get; }
        internal DenseCitySurfaceBakeRecord Blocker { get; }
        internal DenseCityPresentationBakeRecord IntactPresentation { get; }
        internal DenseCityPresentationBakeRecord DestroyedPresentation { get; }
    }

    internal static class DenseCityBuildingRecordFactory
    {
        internal static DenseCityBuildingRecordGroup Create(DenseCityBuildingRecordInput input)
        {
            DenseCityRecordIdentity buildingIdentity = CreateIdentity(input, 0, Kind(input, "building"), false);
            DenseCityRecordIdentity foundationIdentity = CreateIdentity(input, 1, Kind(input, "foundation"), false);
            DenseCityRecordIdentity blockerIdentity = CreateIdentity(input, 2, Kind(input, "blocker"), false);
            DenseCityRecordIdentity intactIdentity = CreateIdentity(input, 3, Kind(input, "building-intact"), false);
            DenseCityRecordIdentity destroyedIdentity = CreateIdentity(input, 4, Kind(input, "building-destroyed"), true);
            Vector2[] footprintPolygon = CreateFootprintPolygon(input);

            var building = new DenseCityBuildingBakeRecord(
                buildingIdentity,
                input.WorldMatrix,
                input.OriginCell,
                input.FootprintCells,
                input.FootprintSize,
                input.FoundationElevation,
                input.BlockerBounds,
                input.FrontageDirection,
                input.Role,
                input.DefinitionConfigAssetGuid,
                input.FactionId,
                input.MaximumHealth,
                OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked,
                foundationIdentity,
                blockerIdentity,
                intactIdentity,
                destroyedIdentity);
            var foundation = new DenseCitySurfaceBakeRecord(
                foundationIdentity,
                DenseCitySurfaceRecordKind.Terrain,
                footprintPolygon,
                input.FoundationElevation,
                input.MovementMask,
                input.SurfaceLayer,
                input.Chunk);
            var blocker = new DenseCitySurfaceBakeRecord(
                blockerIdentity,
                DenseCitySurfaceRecordKind.Blocker,
                footprintPolygon,
                input.FoundationElevation,
                0,
                input.SurfaceLayer,
                input.Chunk);
            var intact = new DenseCityPresentationBakeRecord(
                intactIdentity,
                DenseCityPresentationCategory.GameplayBuildingIntact,
                input.IntactPrefabGuid,
                null,
                input.IntactMaterialGuids,
                input.WorldMatrix,
                true,
                true,
                3);
            var destroyed = new DenseCityPresentationBakeRecord(
                destroyedIdentity,
                DenseCityPresentationCategory.GameplayBuildingDestroyed,
                input.DestroyedPrefabGuid,
                null,
                input.DestroyedMaterialGuids,
                input.WorldMatrix,
                true,
                true,
                3);
            return new DenseCityBuildingRecordGroup(building, foundation, blocker, intact, destroyed);
        }

        internal static void Add(DenseCityGenerationRecordSet records, DenseCityBuildingRecordGroup group)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            records.AddBuildingGroup(
                group.Building,
                group.Foundation,
                group.Blocker,
                group.IntactPresentation,
                group.DestroyedPresentation);
        }

        private static DenseCityRecordIdentity CreateIdentity(
            DenseCityBuildingRecordInput input,
            int sequenceOffset,
            string kind,
            bool destroyedSource) =>
            new(
                input.GeneratorSchema,
                input.Seed,
                input.DistrictId,
                kind,
                input.SequenceStart + sequenceOffset,
                destroyedSource ? input.DestroyedPrefabGuid : input.IntactPrefabGuid,
                destroyedSource ? input.DestroyedPrefabLocalId : input.IntactPrefabLocalId);

        private static string Kind(DenseCityBuildingRecordInput input, string suffix) =>
            string.IsNullOrEmpty(input.IdentityKindPrefix)
                ? suffix
                : string.Concat(input.IdentityKindPrefix, "-", suffix);

        private static Vector2[] CreateFootprintPolygon(DenseCityBuildingRecordInput input)
        {
            Vector3 center = input.WorldMatrix.GetColumn(3);
            Vector3 right = input.WorldMatrix.GetColumn(0);
            Vector3 forward = input.WorldMatrix.GetColumn(2);
            right.y = 0f;
            forward.y = 0f;
            if (right.sqrMagnitude <= 0.000001f || forward.sqrMagnitude <= 0.000001f)
                throw new ArgumentOutOfRangeException(nameof(input), "Building horizontal axes must be non-zero.");
            right.Normalize();
            forward.Normalize();
            right *= input.FootprintSize.x * 0.5f;
            forward *= input.FootprintSize.y * 0.5f;
            return new[]
            {
                ToXZ(center - right - forward),
                ToXZ(center + right - forward),
                ToXZ(center + right + forward),
                ToXZ(center - right + forward)
            };
        }

        private static Vector2 ToXZ(Vector3 value) => new(value.x, value.z);
    }

    internal sealed class DenseCityBuildingDefinitionLibrary
    {
        private const string HousePath =
            "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_House_Config.asset";
        private const string ShopPath =
            "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Shop_Config.asset";
        private const string CivicPath =
            "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Hall_Config.asset";

        private readonly Entry house;
        private readonly Entry shop;
        private readonly Entry civic;

        private DenseCityBuildingDefinitionLibrary(Entry house, Entry shop, Entry civic)
        {
            this.house = house;
            this.shop = shop;
            this.civic = civic;
        }

        internal static DenseCityBuildingDefinitionLibrary LoadExisting() =>
            new(Load(HousePath), Load(ShopPath), Load(CivicPath));

        internal string ResolveAssetGuid(GeneratedCityBuildingRole role) => Resolve(role).AssetGuid;

        internal BuildingDefinitionAuthoringConfig ResolveAsset(GeneratedCityBuildingRole role) =>
            Resolve(role).Asset;

        private Entry Resolve(GeneratedCityBuildingRole role) => role switch
        {
            GeneratedCityBuildingRole.Shop => shop,
            GeneratedCityBuildingRole.Civic => civic,
            GeneratedCityBuildingRole.House or GeneratedCityBuildingRole.Other => house,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

        private static Entry Load(string path)
        {
            BuildingDefinitionAuthoringConfig asset =
                AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(path);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (asset == null || string.IsNullOrEmpty(guid))
                throw new InvalidOperationException($"Dense-city building definition is unavailable: '{path}'.");
            return new Entry(guid, asset);
        }

        private readonly struct Entry
        {
            internal Entry(string assetGuid, BuildingDefinitionAuthoringConfig asset)
            {
                AssetGuid = assetGuid;
                Asset = asset;
            }

            internal string AssetGuid { get; }
            internal BuildingDefinitionAuthoringConfig Asset { get; }
        }
    }
}
