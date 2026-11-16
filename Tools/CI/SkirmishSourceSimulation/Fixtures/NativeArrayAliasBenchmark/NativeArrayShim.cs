namespace SourceShim;

// Small source-only replica of the backing choices and indexer behavior in
// Tools/CI/SkirmishSourceSimulation/Adapters/059_AutonomousEcs.cs.
public readonly record struct Entity(int Index, int Version);

public readonly struct DataArrayView<T>
{
    public T[] Array { get; }
    public int Offset { get; }
    public int Count { get; }

    public DataArrayView(T[] array, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(array);
        if (offset < 0 || count < 0 || offset > array.Length - count)
            throw new ArgumentException("Invalid borrowed range");
        Array = array;
        Offset = offset;
        Count = count;
    }

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count) throw new IndexOutOfRangeException();
            return Array[Offset + index];
        }
    }
}

public readonly struct NativeArray<T>
{
    private readonly T[]? data;
    private readonly List<T>? list;
    private readonly Func<int, T>? getter;
    private readonly int offset;
    private readonly int count;

    public NativeArray(T[] values) : this(values, 0, values?.Length ?? 0) { }

    public NativeArray(T[] values, int start, int length)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (start < 0 || length < 0 || start > values.Length - length)
            throw new ArgumentException("Invalid borrowed range");
        data = values;
        list = null;
        getter = null;
        offset = start;
        count = length;
    }

    public NativeArray(int length, Func<int, T> valueGetter)
    {
        if (length < 0) throw new ArgumentException("Invalid length");
        data = null;
        list = null;
        getter = valueGetter ?? throw new ArgumentNullException(nameof(valueGetter));
        offset = 0;
        count = length;
    }

    private NativeArray(List<T> owner)
    {
        data = null;
        list = owner ?? throw new ArgumentNullException(nameof(owner));
        getter = null;
        offset = 0;
        count = owner.Count;
    }

    public static NativeArray<T> BorrowList(List<T> owner) => new(owner);
    public int Length => count;

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)count) throw new IndexOutOfRangeException();
            return getter is not null ? getter(index) : list is not null ? list[offset + index] : data![offset + index];
        }
    }

    public bool TryGetDataView(out DataArrayView<T> view)
    {
        if (getter is not null || list is not null || data is null)
        {
            view = default;
            return false;
        }
        view = new DataArrayView<T>(data, offset, count);
        return true;
    }
}
