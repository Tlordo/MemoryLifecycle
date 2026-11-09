namespace MemoryLifecycle.Gc;

/// <summary>
/// Снимок счётчиков сборщика мусора и управляемой кучи.
/// </summary>
public readonly record struct GcSnapshot(
    int Gen0,
    int Gen1,
    int Gen2,
    long AllocatedBytes,
    long HeapSizeBytes)
{
    public static GcSnapshot Capture() => new(
        GC.CollectionCount(0),
        GC.CollectionCount(1),
        GC.CollectionCount(2),
        GC.GetTotalAllocatedBytes(precise: true),
        GC.GetGCMemoryInfo().HeapSizeBytes);

    public GcDelta Diff(GcSnapshot previous) => new(
        Gen0 - previous.Gen0,
        Gen1 - previous.Gen1,
        Gen2 - previous.Gen2,
        AllocatedBytes - previous.AllocatedBytes,
        HeapSizeBytes - previous.HeapSizeBytes);
}

/// <summary>
/// Прирост показателей между двумя снимками.
/// </summary>
public readonly record struct GcDelta(
    int Gen0,
    int Gen1,
    int Gen2,
    long AllocatedBytes,
    long HeapSizeDelta)
{
    public double AllocatedMegabytes => AllocatedBytes / 1024d / 1024d;

    public double HeapDeltaMegabytes => HeapSizeDelta / 1024d / 1024d;
}
