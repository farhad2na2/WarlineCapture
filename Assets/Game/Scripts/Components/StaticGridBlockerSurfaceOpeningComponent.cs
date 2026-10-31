using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    // Removes only this blocker's contribution on the authored traversable surface.
    // Placement reservation, other overlapping blockers and occupancy remain independent.
    public struct StaticGridBlockerSurfaceOpeningComponent : IComponentData
    {
        public float2 Center;
        public float2 Right;
        public float2 Forward;
        public float2 HalfExtents;

        public static bool TryCreateFromWorldRunway(float3 center, quaternion rotation, float3 halfExtents,
            out StaticGridBlockerSurfaceOpeningComponent surface)
        {
            surface = default;
            float rotationLength = math.lengthsq(rotation.value);
            if (!math.all(math.isfinite(center)) || !math.all(math.isfinite(rotation.value)) ||
                !math.all(math.isfinite(halfExtents)) || math.abs(rotationLength - 1f) > 0.001f ||
                halfExtents.x <= 0f || halfExtents.y <= 0f || halfExtents.z <= 0f)
                return false;

            float3 right = math.mul(rotation, new float3(1f, 0f, 0f));
            float3 forward = math.mul(rotation, new float3(0f, 0f, 1f));
            surface = new StaticGridBlockerSurfaceOpeningComponent
            {
                Center = new float2(center.x, center.z),
                Right = new float2(right.x, right.z),
                Forward = new float2(forward.x, forward.z),
                HalfExtents = new float2(halfExtents.x, halfExtents.z)
            };
            return surface.IsValid;
        }

        public BlockerOpening ToKernel() => new BlockerOpening { CenterX=Center.x,CenterZ=Center.y,RightX=Right.x,RightZ=Right.y,
            ForwardX=Forward.x,ForwardZ=Forward.y,HalfX=HalfExtents.x,HalfZ=HalfExtents.y };
        public bool IsValid => StaticBlockerKernel.IsValid(ToKernel());
        public bool ContainsFullCell(in GridConfig grid,int x,int z) => StaticBlockerKernel.ContainsFullCell(
            new BlockerGrid {Width=grid.Width,Height=grid.Height,OriginX=grid.Origin.x,OriginY=grid.Origin.y,OriginZ=grid.Origin.z,CellSize=grid.CellSize},ToKernel(),x,z);
        public bool SameGeometry(in StaticGridBlockerSurfaceOpeningComponent other) => StaticBlockerKernel.SameOpening(ToKernel(),other.ToKernel());

    }

    // Retained after native entity destruction so subtraction exactly matches admission.
    public struct StaticGridBlockerPrevSurfaceOpeningComponent : ICleanupComponentData
    {
        public StaticGridBlockerSurfaceOpeningComponent Value;
    }
}
