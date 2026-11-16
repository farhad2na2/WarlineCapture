using System.Diagnostics;
using SourceShim;

const int EntityCount = 4_972;
const int Samples = 15;
const int RepetitionsPerSample = 128;

var source = new Entity[EntityCount];
for (int i = 0; i < source.Length; i++)
    source[i] = new Entity(i % 11 == 0 ? -i : i, (i * 17) % 13 - 6);

// Match EntityQuery.ToEntityArray: own a cloned, data-backed entity buffer.
var clonedEntities = (Entity[])source.Clone();
var nativeArray = new NativeArray<Entity>(clonedEntities);
if (!nativeArray.TryGetDataView(out var fullArrayView))
    throw new InvalidOperationException("Expected the cloned data-backed array to expose a view.");
if (!ReferenceEquals(clonedEntities, fullArrayView.Array) || fullArrayView.Offset != 0 || fullArrayView.Count != EntityCount)
    throw new InvalidOperationException("View must alias the exact cloned Entity[] range.");

ValidateSameValues(nativeArray, fullArrayView, source);
ValidateBoundsAndFallbacks(clonedEntities);

// Warm both paths and alternate run order to reduce order and tiering bias.
long expectedChecksum = Scan(nativeArray, 1);
if (expectedChecksum != ScanView(fullArrayView, 1))
    throw new InvalidOperationException("Warmup checksums differ.");

var nativeTicks = new long[Samples];
var viewTicks = new long[Samples];
long nativeChecksum = 0;
long viewChecksum = 0;
for (int sample = 0; sample < Samples; sample++)
{
    if ((sample & 1) == 0)
    {
        nativeTicks[sample] = Time(() => nativeChecksum ^= Scan(nativeArray, RepetitionsPerSample));
        viewTicks[sample] = Time(() => viewChecksum ^= ScanView(fullArrayView, RepetitionsPerSample));
    }
    else
    {
        viewTicks[sample] = Time(() => viewChecksum ^= ScanView(fullArrayView, RepetitionsPerSample));
        nativeTicks[sample] = Time(() => nativeChecksum ^= Scan(nativeArray, RepetitionsPerSample));
    }
}

if (nativeChecksum != viewChecksum)
    throw new InvalidOperationException("Measured checksums differ.");

double operations = (double)EntityCount * RepetitionsPerSample;
Console.WriteLine("fixture=source-only NativeArray shim alias check");
Console.WriteLine($"entity_values={EntityCount} samples={Samples} repetitions_per_sample={RepetitionsPerSample}");
Console.WriteLine($"validation=Passed cloned_array_alias=Passed values_and_order=Passed bounds_and_fallbacks=Passed");
Console.WriteLine($"native_indexer_median_ns_per_value={Median(nativeTicks) * 1e9 / Stopwatch.Frequency / operations:F3}");
Console.WriteLine($"array_view_median_ns_per_value={Median(viewTicks) * 1e9 / Stopwatch.Frequency / operations:F3}");
Console.WriteLine("scope=fixture-only; no simulator speedup claim");

static void ValidateSameValues(NativeArray<Entity> nativeArray, DataArrayView<Entity> view, Entity[] expected)
{
    if (nativeArray.Length != expected.Length || view.Count != expected.Length)
        throw new InvalidOperationException("Length mismatch.");
    for (int i = 0; i < expected.Length; i++)
    {
        if (!nativeArray[i].Equals(expected[i]) || !view[i].Equals(expected[i]))
            throw new InvalidOperationException($"Value/order mismatch at {i}.");
    }
}

static void ValidateBoundsAndFallbacks(Entity[] clonedEntities)
{
    // Borrowed subranges must retain their offset and never expose adjacent values.
    var offsetArray = new NativeArray<Entity>(clonedEntities, 7, 19);
    if (!offsetArray.TryGetDataView(out var offsetView) || offsetView.Offset != 7 || offsetView.Count != 19)
        throw new InvalidOperationException("Offset data-backed view mismatch.");
    for (int i = 0; i < offsetView.Count; i++)
        if (!offsetArray[i].Equals(offsetView[i]) || !offsetView[i].Equals(clonedEntities[i + 7]))
            throw new InvalidOperationException($"Offset view mismatch at {i}.");
    Throws<IndexOutOfRangeException>(() => _ = offsetView[-1]);
    Throws<IndexOutOfRangeException>(() => _ = offsetView[offsetView.Count]);
    Throws<ArgumentException>(() => _ = new NativeArray<Entity>(clonedEntities, -1, 1));
    Throws<ArgumentException>(() => _ = new NativeArray<Entity>(clonedEntities, 0, clonedEntities.Length + 1));

    // Getter-backed and list-backed shim arrays are intentionally not aliasable.
    var getterArray = new NativeArray<Entity>(2, i => clonedEntities[i + 4]);
    if (getterArray.TryGetDataView(out _))
        throw new InvalidOperationException("Getter-backed arrays must reject direct views.");
    if (!getterArray[0].Equals(clonedEntities[4]) || !getterArray[1].Equals(clonedEntities[5]))
        throw new InvalidOperationException("Getter-backed indexer changed values.");

    var listArray = NativeArray<Entity>.BorrowList(new List<Entity> { clonedEntities[8], clonedEntities[9] });
    if (listArray.TryGetDataView(out _))
        throw new InvalidOperationException("List-backed arrays must reject direct views.");
    if (!listArray[0].Equals(clonedEntities[8]) || !listArray[1].Equals(clonedEntities[9]))
        throw new InvalidOperationException("List-backed indexer changed values.");
}

static long Scan(NativeArray<Entity> array, int repetitions)
{
    long checksum = 0;
    for (int repeat = 0; repeat < repetitions; repeat++)
        for (int i = 0; i < array.Length; i++) checksum += Mix(array[i], i);
    return checksum;
}

static long ScanView(DataArrayView<Entity> view, int repetitions)
{
    long checksum = 0;
    for (int repeat = 0; repeat < repetitions; repeat++)
        for (int i = 0; i < view.Count; i++) checksum += Mix(view[i], i);
    return checksum;
}

static long Mix(Entity entity, int position) => ((long)entity.Index * 31 + entity.Version) * 17 + position;
static long Time(Action action) { var watch = Stopwatch.StartNew(); action(); watch.Stop(); return watch.ElapsedTicks; }
static long Median(long[] values) { var copy = (long[])values.Clone(); Array.Sort(copy); return copy[copy.Length / 2]; }
static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}
