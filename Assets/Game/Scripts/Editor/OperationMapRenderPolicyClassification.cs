using System;
using Game.Components;

namespace Game.Editor
{
    internal enum OperationMapRenderMaterialSurface : byte
    {
        Opaque = 0,
        AlphaClipped = 1,
        Transparent = 2
    }

    internal enum OperationMapRenderMotionVectorMode : byte
    {
        Camera = 0,
        Object = 1,
        ForceNoMotion = 2
    }

    internal readonly struct OperationMapRenderPolicyClassificationInput
    {
        internal OperationMapRenderPolicyClassificationInput(
            OperationMapRenderMaterialSurface materialSurface,
            int layer,
            uint renderingLayerMask,
            OperationMapRenderMotionVectorMode motionVectorMode,
            OperationMapRenderShadowFlags shadowFlags,
            bool alwaysResidentException = false)
        {
            MaterialSurface = materialSurface;
            Layer = layer;
            RenderingLayerMask = renderingLayerMask;
            MotionVectorMode = motionVectorMode;
            ShadowFlags = shadowFlags;
            AlwaysResidentException = alwaysResidentException;
        }

        internal OperationMapRenderMaterialSurface MaterialSurface { get; }
        internal int Layer { get; }
        internal uint RenderingLayerMask { get; }
        internal OperationMapRenderMotionVectorMode MotionVectorMode { get; }
        internal OperationMapRenderShadowFlags ShadowFlags { get; }
        internal bool AlwaysResidentException { get; }
    }

    internal readonly struct OperationMapRenderPolicyKey :
        IEquatable<OperationMapRenderPolicyKey>
    {
        internal OperationMapRenderPolicyKey(
            OperationMapRenderPolicyBucket bucket,
            int layer,
            uint renderingLayerMask,
            OperationMapRenderMotionVectorMode motionVectorMode,
            OperationMapRenderShadowFlags shadowFlags)
        {
            Bucket = bucket;
            Layer = layer;
            RenderingLayerMask = renderingLayerMask;
            MotionVectorMode = motionVectorMode;
            ShadowFlags = shadowFlags;
        }

        internal OperationMapRenderPolicyBucket Bucket { get; }
        internal int Layer { get; }
        internal uint RenderingLayerMask { get; }
        internal OperationMapRenderMotionVectorMode MotionVectorMode { get; }
        internal OperationMapRenderShadowFlags ShadowFlags { get; }

        public bool Equals(OperationMapRenderPolicyKey other)
        {
            return Bucket == other.Bucket &&
                   Layer == other.Layer &&
                   RenderingLayerMask == other.RenderingLayerMask &&
                   MotionVectorMode == other.MotionVectorMode &&
                   ShadowFlags == other.ShadowFlags;
        }

        public override bool Equals(object obj)
        {
            return obj is OperationMapRenderPolicyKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Bucket;
                hash = (hash * 397) ^ Layer;
                hash = (hash * 397) ^ (int)RenderingLayerMask;
                hash = (hash * 397) ^ (int)MotionVectorMode;
                hash = (hash * 397) ^ (int)ShadowFlags;
                return hash;
            }
        }
    }

    internal static class OperationMapRenderPolicyClassifier
    {
        private const OperationMapRenderShadowFlags KnownShadowFlags =
            OperationMapRenderShadowFlags.CastShadows |
            OperationMapRenderShadowFlags.ReceiveShadows |
            OperationMapRenderShadowFlags.StaticShadowCaster;

        internal static bool TryClassify(
            in OperationMapRenderPolicyClassificationInput input,
            out OperationMapRenderPolicyKey policy,
            out string error)
        {
            policy = default;

            if (!Enum.IsDefined(typeof(OperationMapRenderMaterialSurface), input.MaterialSurface))
            {
                error = $"Unknown material surface: {(byte)input.MaterialSurface}.";
                return false;
            }

            if (input.Layer < 0 || input.Layer > 31)
            {
                error = $"Render layer must be in [0,31], but was {input.Layer}.";
                return false;
            }

            if (input.RenderingLayerMask == 0u)
            {
                error = "Rendering-layer mask must contain at least one layer.";
                return false;
            }

            if (!Enum.IsDefined(typeof(OperationMapRenderMotionVectorMode), input.MotionVectorMode))
            {
                error = $"Unknown motion-vector mode: {(byte)input.MotionVectorMode}.";
                return false;
            }

            if ((input.ShadowFlags & ~KnownShadowFlags) != 0)
            {
                error = $"Unknown render shadow flags: {(byte)input.ShadowFlags}.";
                return false;
            }

            bool castsShadows =
                (input.ShadowFlags & OperationMapRenderShadowFlags.CastShadows) != 0;
            bool staticShadowCaster =
                (input.ShadowFlags & OperationMapRenderShadowFlags.StaticShadowCaster) != 0;
            if (staticShadowCaster && !castsShadows)
            {
                error = "Static-shadow caster policy requires CastShadows.";
                return false;
            }

            OperationMapRenderPolicyBucket bucket;
            if (input.AlwaysResidentException)
            {
                bucket = OperationMapRenderPolicyBucket.AlwaysResidentException;
            }
            else
            {
                switch (input.MaterialSurface)
                {
                    case OperationMapRenderMaterialSurface.Opaque:
                        bucket = castsShadows
                            ? OperationMapRenderPolicyBucket.OpaqueShadowsOn
                            : OperationMapRenderPolicyBucket.OpaqueShadowsOff;
                        break;
                    case OperationMapRenderMaterialSurface.AlphaClipped:
                        bucket = castsShadows
                            ? OperationMapRenderPolicyBucket.AlphaClippedShadowsOn
                            : OperationMapRenderPolicyBucket.AlphaClippedShadowsOff;
                        break;
                    case OperationMapRenderMaterialSurface.Transparent:
                        if (castsShadows || staticShadowCaster)
                        {
                            error =
                                "Transparent render policy does not support cast or static shadows.";
                            return false;
                        }

                        bucket = OperationMapRenderPolicyBucket.TransparentShadowsOff;
                        break;
                    default:
                        error = $"Unknown material surface: {(byte)input.MaterialSurface}.";
                        return false;
                }
            }

            policy = new OperationMapRenderPolicyKey(
                bucket,
                input.Layer,
                input.RenderingLayerMask,
                input.MotionVectorMode,
                input.ShadowFlags);
            error = null;
            return true;
        }
    }
}
