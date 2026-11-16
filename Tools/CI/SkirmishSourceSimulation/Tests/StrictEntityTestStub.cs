namespace Unity.Entities
{
    public struct Entity
    {
        public int Index;
        public int Version;
        public Entity(int index, int version) { Index = index; Version = version; }
        public static Entity Null => default;
        public override bool Equals(object other) => other is Entity e && Index == e.Index && Version == e.Version;
        public override int GetHashCode() => (Index * 397) ^ Version;
        public static bool operator ==(Entity left, Entity right) => left.Equals(right);
        public static bool operator !=(Entity left, Entity right) => !left.Equals(right);
    }
}
