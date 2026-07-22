using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityRenderOnlyPresentationRecordInput
    {
        internal DenseCityRenderOnlyPresentationRecordInput(
            string generatorSchema,
            int seed,
            int districtId,
            int sequence,
            string recordKind,
            DenseCityPresentationCategory category,
            string sourceAssetGuid,
            long sourceLocalId,
            IReadOnlyList<string> materialAssetGuids,
            Matrix4x4 worldMatrix,
            bool castsShadows,
            bool batchingEligible,
            byte lodImportance)
        {
            GeneratorSchema = generatorSchema;
            Seed = seed;
            DistrictId = districtId;
            Sequence = sequence;
            RecordKind = recordKind;
            Category = category;
            SourceAssetGuid = sourceAssetGuid;
            SourceLocalId = sourceLocalId;
            MaterialAssetGuids = materialAssetGuids;
            WorldMatrix = worldMatrix;
            CastsShadows = castsShadows;
            BatchingEligible = batchingEligible;
            LodImportance = lodImportance;
        }

        internal string GeneratorSchema { get; }
        internal int Seed { get; }
        internal int DistrictId { get; }
        internal int Sequence { get; }
        internal string RecordKind { get; }
        internal DenseCityPresentationCategory Category { get; }
        internal string SourceAssetGuid { get; }
        internal long SourceLocalId { get; }
        internal IReadOnlyList<string> MaterialAssetGuids { get; }
        internal Matrix4x4 WorldMatrix { get; }
        internal bool CastsShadows { get; }
        internal bool BatchingEligible { get; }
        internal byte LodImportance { get; }
    }

    internal static class DenseCityRenderOnlyPresentationRecordFactory
    {
        internal static DenseCityPresentationBakeRecord Create(
            DenseCityRenderOnlyPresentationRecordInput input)
        {
            RequireRenderOnlyCategory(input.Category);
            var identity = new DenseCityRecordIdentity(
                input.GeneratorSchema,
                input.Seed,
                input.DistrictId,
                input.RecordKind,
                input.Sequence,
                input.SourceAssetGuid,
                input.SourceLocalId);
            return new DenseCityPresentationBakeRecord(
                identity,
                input.Category,
                input.SourceAssetGuid,
                null,
                input.MaterialAssetGuids,
                input.WorldMatrix,
                input.CastsShadows,
                input.BatchingEligible,
                input.LodImportance);
        }

        internal static void RequireRenderOnlyCategory(DenseCityPresentationCategory category)
        {
            if (category is not (DenseCityPresentationCategory.Infrastructure or
                DenseCityPresentationCategory.Vegetation or
                DenseCityPresentationCategory.Prop or
                DenseCityPresentationCategory.Horizon))
            {
                throw new ArgumentOutOfRangeException(nameof(category));
            }
        }
    }
}
