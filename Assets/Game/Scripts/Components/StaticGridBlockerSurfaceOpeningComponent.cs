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
