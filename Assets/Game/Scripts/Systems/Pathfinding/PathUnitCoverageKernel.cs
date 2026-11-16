// Conservative broadphase only. Unknown/overflow/work-limit cases retain the original unit scan.
namespace Game.Runtime.Pathfinding
{
    public interface IPathUnitCoverageStorage { bool IsMarked(int index); void Mark(int index); }
    public readonly struct PathUnitCoverageGrid
    {
        public readonly int Width, Height, TileWidth, TileHeight, TileCount;
        public const int TileSize = 4;
        internal PathUnitCoverageGrid(int width,int height,int tw,int th)
        { Width=width;Height=height;TileWidth=tw;TileHeight=th;TileCount=tw*th; }
    }
    public static class PathUnitCoverageKernel
    {
        // An optimization resource bound, never a mission/admission rule: callers fall back on false.
        public const int MaxGridCells = 4194304;
        public static bool TryCreateGrid(int width,int height,out PathUnitCoverageGrid grid)
        {
            grid=default;
            if(width<=0||height<=0||(long)width*height>MaxGridCells)return false;
            int tw=(width+PathUnitCoverageGrid.TileSize-1)/PathUnitCoverageGrid.TileSize;
            int th=(height+PathUnitCoverageGrid.TileSize-1)/PathUnitCoverageGrid.TileSize;
            grid=new PathUnitCoverageGrid(width,height,tw,th);return true;
        }
        private static bool Bounds(int x,int z,int sx,int sz,out long minX,out long minZ,out long maxX,out long maxZ)
        {
            long width=System.Math.Max(1,sx),height=System.Math.Max(1,sz);
            minX=(long)x-(width-1)/2;minZ=(long)z-(height-1)/2;maxX=minX+width;maxZ=minZ+height;
            // Original placement uses signed int bounds; do not optimize wrapped geometry.
            return minX>=int.MinValue&&minZ>=int.MinValue&&maxX<=int.MaxValue&&maxZ<=int.MaxValue;
        }
        public static bool TryAppend<T>(in PathUnitCoverageGrid grid,ref T storage,int x,int z,int sx,int sz,ref int writesRemaining) where T:struct,IPathUnitCoverageStorage
        {
            if(grid.TileCount<=0||!Bounds(x,z,sx,sz,out long minX,out long minZ,out long maxX,out long maxZ))return false;
            minX=System.Math.Max(0,minX);minZ=System.Math.Max(0,minZ);maxX=System.Math.Min(grid.Width,maxX);maxZ=System.Math.Min(grid.Height,maxZ);
            if(minX>=maxX||minZ>=maxZ)return true;
            int x0=(int)minX/4,z0=(int)minZ/4,x1=((int)maxX-1)/4,z1=((int)maxZ-1)/4;
            long count=(long)(x1-x0+1)*(z1-z0+1);if(count>writesRemaining)return false;
            writesRemaining-=(int)count;
            for(int row=z0;row<=z1;row++)for(int column=x0;column<=x1;column++)storage.Mark(row*grid.TileWidth+column);
            return true;
        }
        public static bool MayOverlap<T>(in PathUnitCoverageGrid grid,ref T storage,int x,int z,int sx,int sz) where T:struct,IPathUnitCoverageStorage
        {
            if(grid.TileCount<=0||!Bounds(x,z,sx,sz,out long minX,out long minZ,out long maxX,out long maxZ)||minX<0||minZ<0||maxX>grid.Width||maxZ>grid.Height)return true;
            int x0=(int)minX/4,z0=(int)minZ/4,x1=((int)maxX-1)/4,z1=((int)maxZ-1)/4;
            for(int row=z0;row<=z1;row++)for(int column=x0;column<=x1;column++)if(storage.IsMarked(row*grid.TileWidth+column))return true;
            return false;
        }
    }
}
