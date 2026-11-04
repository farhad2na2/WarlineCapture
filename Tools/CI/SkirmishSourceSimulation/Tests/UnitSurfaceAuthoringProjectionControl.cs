using System;
using Game.Components;

internal static class UnitSurfaceAuthoringProjectionControl
{
    public static void Verify()
    {
        const float authoredY = 12.375f;
        UnitSurfaceAuthoringProjection surface = UnitSurfaceAuthoringProjection.FromAuthoredHeight(authoredY);
        Require(surface.SurfaceId == -1 && surface.LayerId == 0, "original surface and layer defaults");
        Require(surface.LastSampledHeight == authoredY, "authored Y preserved exactly");
        Require(surface.LastSampledNormal.x == 0f && surface.LastSampledNormal.y == 1f && surface.LastSampledNormal.z == 0f,
            "original up normal");
        Require(surface.HasSurface == 0 && surface.IsGrounded == 0, "original surface flags");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("assertion failed: " + label);
    }
}
