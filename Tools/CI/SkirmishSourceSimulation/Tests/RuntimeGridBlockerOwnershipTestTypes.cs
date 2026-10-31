namespace Unity.Entities
{
    internal readonly struct Entity
    {
        internal static readonly Entity Null = default;
        internal readonly int Index;
        internal readonly int Version;

        internal Entity(int index, int version)
        {
            Index = index;
            Version = version;
        }

        public static bool operator ==(Entity left, Entity right) => left.Index == right.Index && left.Version == right.Version;
        public static bool operator !=(Entity left, Entity right) => !(left == right);
        public override bool Equals(object value) => value is Entity other && this == other;
        public override int GetHashCode() => (Index * 397) ^ Version;
    }
}

namespace Unity.Mathematics
{
    internal readonly struct int2
    {
        internal readonly int x;
        internal readonly int y;

        internal int2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}

namespace Game.Components
{
    internal static class GridUtils
    {
        internal static int CellToIndex(Unity.Mathematics.int2 cell, int width) => cell.x + cell.y * width;
    }
}
