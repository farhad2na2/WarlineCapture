namespace Game.Components
{
    public struct GridCoordinateFrame
    {
        public float OriginX, OriginY, OriginZ, CellSize;
    }
    public struct GridCellValue { public int X, Z; }
    public struct GridPositionValue
    {
        public float X, Y, Z;
        public GridPositionValue(float x, float y, float z) { X=x; Y=y; Z=z; }
        public static GridPositionValue operator +(GridPositionValue a, GridPositionValue b) =>
            new GridPositionValue(a.X+b.X, a.Y+b.Y, a.Z+b.Z);
        public static GridPositionValue operator -(GridPositionValue a, GridPositionValue b) =>
            new GridPositionValue(a.X-b.X, a.Y-b.Y, a.Z-b.Z);
    }

    // Exact scalar grid conversions shared by native GridUtils and offline hosts.
    public static class GridCoordinateKernel
    {
        public static GridCellValue WorldToCell(in GridCoordinateFrame frame, float x, float z)
        {
            // Preserve float3 field-store boundaries from GridUtils, including on Mono.
            var local = new GridPositionValue(x, 0f, z) -
                new GridPositionValue(frame.OriginX, frame.OriginY, frame.OriginZ);
            return new GridCellValue
            {
                X = (int)(float)System.Math.Floor((float)(local.X / frame.CellSize)),
                Z = (int)(float)System.Math.Floor((float)(local.Z / frame.CellSize))
            };
        }
        public static GridPositionValue CellToWorldCenter(in GridCoordinateFrame frame, int x, int z)
        {
            // Original code constructs the offset vector before adding the origin.
            // A scalar multiply-add expression changes Mono rounding by one ULP.
            var origin = new GridPositionValue(frame.OriginX, frame.OriginY, frame.OriginZ);
            var offset = new GridPositionValue((x + 0.5f) * frame.CellSize, 0f,
                (z + 0.5f) * frame.CellSize);
            return origin + offset;
        }
    }
}
