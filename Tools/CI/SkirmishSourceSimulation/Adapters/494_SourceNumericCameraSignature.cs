using Game.Components;using Unity.Mathematics;namespace Game.Runtime{internal static class SourceNumericCameraSignature{
internal static OperationMapRenderIdentity128 ComputeSignature(
            float3 position,
            quaternion rotation,
            float4x4 worldToCamera,
            float4x4 projection)
        {
            const ulong lowPrime = 1099511628211ul;
            const ulong highPrime = 14029467366897019727ul;
            ulong low = 14695981039346656037ul;
            ulong high = 7809847782465536322ul;
            Hash(ref low, ref high, math.asuint(position.x), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(position.y), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(position.z), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(rotation.value.x), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(rotation.value.y), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(rotation.value.z), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(rotation.value.w), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(worldToCamera.c0), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(worldToCamera.c1), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(worldToCamera.c2), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(worldToCamera.c3), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(projection.c0), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(projection.c1), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(projection.c2), lowPrime, highPrime);
            Hash(ref low, ref high, math.asuint(projection.c3), lowPrime, highPrime);
            return new OperationMapRenderIdentity128
            {
                Low = low,
                High = high
            };
        }
private static void Hash(
            ref ulong low,
            ref ulong high,
            uint4 values,
            ulong lowPrime,
            ulong highPrime)
        {
            Hash(ref low, ref high, values.x, lowPrime, highPrime);
            Hash(ref low, ref high, values.y, lowPrime, highPrime);
            Hash(ref low, ref high, values.z, lowPrime, highPrime);
            Hash(ref low, ref high, values.w, lowPrime, highPrime);
        }
private static void Hash(
            ref ulong low,
            ref ulong high,
            uint value,
            ulong lowPrime,
            ulong highPrime)
        {
            low = (low ^ value) * lowPrime;
            high ^= (ulong)value + 0x9e3779b97f4a7c15ul +
                    (high << 6) + (high >> 2);
            high *= highPrime;
        }
}}
