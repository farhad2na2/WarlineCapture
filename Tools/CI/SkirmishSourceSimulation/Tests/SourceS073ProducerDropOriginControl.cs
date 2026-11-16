using System;
using Game.Runtime.TransportPose;
using Unity.Mathematics;

internal static class SourceS073ProducerDropOriginControl
{
    static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("S073 producer drop origin: " + message);
    }

    static bool RejectsMissingFrame()
    {
        try
        {
            SourceTransportGeometry.DiagnosticDrop(false, true, new float3(0, 0, 0),
                quaternion.identity, 1f, new float3(1, 0, 0));
            return false;
        }
        catch (NotSupportedException) { return true; }
    }

    static bool RejectsInvalidPose(float3 position, quaternion rotation, float scale)
    {
        try
        {
            SourceTransportGeometry.DiagnosticDrop(true, true, position, rotation,
                scale, new float3(1, 0, 0));
            return false;
        }
        catch (ArgumentException) { return true; }
    }

    static void Main()
    {
        float3 producer = new float3(120f, 3f, 240f);
        float3 authoredLocal = new float3(2f, 7f, -3f);
        float3 slot = SourceTransportGeometry.DiagnosticDrop(true, true, producer,
            quaternion.identity, 2f, authoredLocal);
        Need(math.all(slot == new float3(124f, 0.5f, 234f)),
            "authored slot must use the live producer frame and authored scale");

        float3 fallback = SourceTransportGeometry.DiagnosticDrop(true, false, producer,
            quaternion.identity, 2f, default);
        Need(math.all(fallback == new float3(120f, 0.5f, 244f)),
            "missing authored slot must use the native forward-distance fallback");

        Need(RejectsMissingFrame(), "missing live producer frame must fail closed");
        Need(!SourceTransportGeometry.DiagnosticProducerIdentityAdmitted(false, true, true, false, false),
            "unregistered or foreign producer reference must be rejected");
        Need(!SourceTransportGeometry.DiagnosticProducerIdentityAdmitted(true, false, true, false, false),
            "mismatched same-world runtime identity must be rejected");
        Need(!SourceTransportGeometry.DiagnosticProducerIdentityAdmitted(true, true, true, true, false),
            "parented local transform must be rejected");
        Need(!SourceTransportGeometry.DiagnosticProducerIdentityAdmitted(true, true, true, false, true),
            "post-transform matrix producer must be rejected");
        Need(RejectsInvalidPose(new float3(float.NaN, 0, 0), quaternion.identity, 1f),
            "nonfinite position must be rejected");
        Need(RejectsInvalidPose(producer, new quaternion(float.NaN, 0, 0, 1), 1f),
            "nonfinite rotation must be rejected");
        Need(RejectsInvalidPose(producer, quaternion.identity, float.PositiveInfinity),
            "nonfinite scale must be rejected");
        float3 unchangedDefault = SourceTransportGeometry.Drop(null, false, null, 0);
        Need(math.all(unchangedDefault == new float3(0f, 0.5f, 0f)),
            "non-opt-in/default null-instance branch must remain unchanged");

        Console.WriteLine("[SourceS073ProducerDropOriginControl] result=Passed scope=OptInLiveProducerSlotFallbackMissingFrame_DefaultBranchPreserved");
    }
}
