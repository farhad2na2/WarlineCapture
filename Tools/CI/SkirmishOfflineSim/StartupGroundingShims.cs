// Data-container shims only; original production surface decoding and geometry
// sources are linked unchanged. Do not reference Unity.Entities.dll here.
namespace Unity.Collections
{
    public readonly struct NativeArray<T>
    {
        readonly T[] values;
        public NativeArray(T[] values) { this.values = values; }
        public bool IsCreated => values != null;
        public int Length => values == null ? 0 : values.Length;
        public T this[int index] => values[index];
    }
}
namespace Unity.Entities
{
    public interface IComponentData { }
    public interface IBufferElementData { }
    public readonly struct BlobArray<T>
    {
        readonly T[] values;
        public BlobArray(T[] values) { this.values = values; }
        public int Length => values == null ? 0 : values.Length;
        public T this[int index] => values[index];
    }
    public struct BlobAssetReference<T> where T : struct { }
}
