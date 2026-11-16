#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using Game.Components;
namespace Game.Tests.Editor
{
    public static class SharedBlockerOriginalOpening
    {
        static SharedBlockerOriginalOpeningComponent Original(in StaticGridBlockerSurfaceOpeningComponent o)=>new SharedBlockerOriginalOpeningComponent {Center=o.Center,Right=o.Right,Forward=o.Forward,HalfExtents=o.HalfExtents};
        public static bool Valid(in StaticGridBlockerSurfaceOpeningComponent o)=>Original(o).IsValid;
        public static bool Contains(in StaticGridBlockerSurfaceOpeningComponent o,in GridConfig grid,int x,int z)=>Original(o).ContainsFullCell(grid,x,z);
        public static bool Same(in StaticGridBlockerSurfaceOpeningComponent a,in StaticGridBlockerSurfaceOpeningComponent b)=>Original(a).SameGeometry(Original(b));
    }
}
#endif
