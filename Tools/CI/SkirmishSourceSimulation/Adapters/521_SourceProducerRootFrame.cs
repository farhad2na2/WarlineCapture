using System;
using Unity.Mathematics;

namespace Game.Runtime
{
    // The source wrapper's TransformPoint/position/forward read domain only.
    // This is deliberately not a claim about its visual child/renderer hierarchy.
    internal readonly struct SourceProducerRootFrame
    {
        internal readonly float3 Position;
        internal readonly quaternion Rotation;
        internal readonly float UniformScale;

        internal SourceProducerRootFrame(float3 position, quaternion rotation, float uniformScale)
        {
            if (!math.all(math.isfinite(position)) || !math.all(math.isfinite(rotation.value)) ||
                !math.isfinite(uniformScale) || uniformScale <= 0 ||
                math.abs(math.lengthsq(rotation.value) - 1f) > 0.0001f)
                throw new ArgumentException("Finite genuine source producer root transform required");
            Position = position; Rotation = rotation; UniformScale = uniformScale;
        }
        internal float3 Point(float3 local) => math.transform(
            float4x4.TRS(Position, Rotation, new float3(UniformScale)), local);
        internal float3 Forward => math.rotate(Rotation, new float3(0, 0, 1));
    }
}
