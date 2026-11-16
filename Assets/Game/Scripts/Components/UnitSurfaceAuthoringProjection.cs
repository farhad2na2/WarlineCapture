using Unity.Mathematics;

namespace Game.Components
{
    /// <summary>
    /// Data-only initial surface state shared by UnitGridAuthoring and source controls.
    /// The input is the already evaluated authoring transform Y value.
    /// </summary>
    public readonly struct UnitSurfaceAuthoringProjection
    {
        public readonly int SurfaceId;
        public readonly int LayerId;
        public readonly float LastSampledHeight;
        public readonly float3 LastSampledNormal;
        public readonly byte HasSurface;
        public readonly byte IsGrounded;

        private UnitSurfaceAuthoringProjection(float authoredY)
        {
            SurfaceId = -1;
            LayerId = 0;
            LastSampledHeight = authoredY;
            LastSampledNormal = new float3(0f, 1f, 0f);
            HasSurface = 0;
            IsGrounded = 0;
        }

        public static UnitSurfaceAuthoringProjection FromAuthoredHeight(float authoredY) =>
            new UnitSurfaceAuthoringProjection(authoredY);
    }
}
