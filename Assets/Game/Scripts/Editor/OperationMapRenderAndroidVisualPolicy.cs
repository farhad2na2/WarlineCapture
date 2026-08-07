using System;
using Game.Components;

namespace Game.Editor
{
    internal readonly struct OperationMapRenderAndroidVisualPolicyResult
    {
        internal OperationMapRenderAndroidVisualPolicyResult(
            OperationMapRenderPolicyKey policy,
            OperationMapRenderLodFlags lodFlags,
            string reasonCode)
        {
            Policy = policy;
            LodFlags = lodFlags;
            ReasonCode = reasonCode;
        }

        internal OperationMapRenderPolicyKey Policy { get; }
        internal OperationMapRenderLodFlags LodFlags { get; }
        internal string ReasonCode { get; }
    }

    internal static class OperationMapRenderAndroidVisualPolicy
    {
        internal const float SmallDetailMaximumExtentMeters =
            DenseCityPresentationBudgetValidator.SmallDetailMaximumExtentMeters;

        internal static bool TryApply(
            DenseCityPresentationSemanticCategory category,
            float maximumWorldExtentMeters,
            OperationMapRenderMaterialSurface materialSurface,
            in OperationMapRenderPolicyKey sourcePolicy,
            out OperationMapRenderAndroidVisualPolicyResult result,
            out string error)
        {
            result = default;

            if (!Enum.IsDefined(typeof(DenseCityPresentationSemanticCategory), category) ||
                category == DenseCityPresentationSemanticCategory.Unknown)
            {
                error = $"Unknown dense-city semantic category: {(byte)category}.";
                return false;
            }

            if (float.IsNaN(maximumWorldExtentMeters) ||
                float.IsInfinity(maximumWorldExtentMeters) ||
                maximumWorldExtentMeters < 0f)
            {
                error =
                    $"Maximum world extent must be finite and nonnegative, but was " +
                    $"{maximumWorldExtentMeters}.";
                return false;
            }

            if (!OperationMapRenderPolicyClassifier.TryValidate(sourcePolicy, out error))
                return false;

            if (sourcePolicy.Bucket == OperationMapRenderPolicyBucket.AlwaysResidentException)
            {
                error = "Android candidate visual policy cannot rewrite an always-resident exception.";
                return false;
            }

            bool categoryShadowOff = category is
                DenseCityPresentationSemanticCategory.Vegetation or
                DenseCityPresentationSemanticCategory.Prop;
            bool smallDetailShadowOff =
                maximumWorldExtentMeters <= SmallDetailMaximumExtentMeters;
            OperationMapRenderShadowFlags sourceShadowFlags = sourcePolicy.ShadowFlags;
            bool sourceCasts =
                (sourceShadowFlags & OperationMapRenderShadowFlags.CastShadows) != 0;

            if ((!categoryShadowOff && !smallDetailShadowOff) || !sourceCasts)
            {
                result = new OperationMapRenderAndroidVisualPolicyResult(
                    sourcePolicy,
                    OperationMapRenderLodFlags.Lod0,
                    "android-evidence-policy-unchanged");
                error = null;
                return true;
            }

            OperationMapRenderShadowFlags revisedShadowFlags =
                sourceShadowFlags &
                ~(OperationMapRenderShadowFlags.CastShadows |
                  OperationMapRenderShadowFlags.StaticShadowCaster);
            var input = new OperationMapRenderPolicyClassificationInput(
                materialSurface,
                sourcePolicy.Layer,
                sourcePolicy.RenderingLayerMask,
                sourcePolicy.MotionVectorMode,
                revisedShadowFlags);
            if (!OperationMapRenderPolicyClassifier.TryClassify(
                    input,
                    out OperationMapRenderPolicyKey revisedPolicy,
                    out error))
            {
                return false;
            }

            string reasonCode = categoryShadowOff
                ? category == DenseCityPresentationSemanticCategory.Vegetation
                    ? "android-evidence-vegetation-shadow-off"
                    : "android-evidence-prop-shadow-off"
                : "android-evidence-small-detail-shadow-off";
            result = new OperationMapRenderAndroidVisualPolicyResult(
                revisedPolicy,
                OperationMapRenderLodFlags.Lod0,
                reasonCode);
            error = null;
            return true;
        }
    }
}
