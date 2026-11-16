extern alias ActualCore;
using System;
using Game.Runtime.TransportPose;
using Game.Simulation;
using Unity.Mathematics;
using V3 = ActualCore::UnityEngine.Vector3;

namespace Game.Runtime
{
    // Source-only projection of the AirSelf launch branch. Camera presence,
    // lane and touchdown must be supplied by the actual same-world owners.
    internal static class SourceAirSelfLaunchGeometry
    {
        internal readonly struct Result
        {
            internal readonly float3 Entry, Hover, Touchdown, Exit;
            internal readonly quaternion Rotation;
            internal Result(float3 entry, float3 hover, float3 touchdown, float3 exit, quaternion rotation)
            { Entry = entry; Hover = hover; Touchdown = touchdown; Exit = exit; Rotation = rotation; }
        }
        internal static Result Resolve(float3 touchdown, bool cameraPresent, float3 cameraRight, float3 cameraForward, int lane, int maxConcurrent)
        {
            PoseMath.Ready(); PoseMath.Finite(touchdown); PoseMath.Finite(cameraRight); PoseMath.Finite(cameraForward);
            if (lane < 0 || maxConcurrent < 1 || lane >= maxConcurrent) throw new ArgumentOutOfRangeException(nameof(lane));
            if (cameraPresent && (math.lengthsq(cameraRight) < 1e-12f || math.lengthsq(cameraForward) < 1e-12f))
                throw new NotSupportedException("Actual camera basis required");
            var position = new V3(touchdown.x, touchdown.y, touchdown.z);
            var hover = position + new V3(0f, 6f, 0f);
            hover += SharedProductionDeliveryKernel.ProductionTransportLaneOffset(cameraPresent,
                new V3(cameraForward.x, cameraForward.y, cameraForward.z), lane, maxConcurrent);
            var horizontal = cameraPresent ? -new V3(cameraRight.x, cameraRight.y, cameraRight.z).normalized * 70f : new V3(-70f, 0f, 0f);
            var entry = hover + horizontal + new V3(0f, 16f, 0f);
            var forward = (hover - entry).normalized;
            var rotation = PoseMath.Look(new float3(forward.x, forward.y, forward.z), new float3(0f, 1f, 0f));
            return new Result(new float3(entry.x, entry.y, entry.z), new float3(hover.x, hover.y, hover.z), touchdown,
                new float3(hover.x, hover.y, hover.z), rotation);
        }
    }
}
