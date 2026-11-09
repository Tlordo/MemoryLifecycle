using System.Diagnostics;

namespace MemoryLifecycle.Gc;

/// <summary>
/// Сценарии, создающие разное давление на управляемую кучу.
/// </summary>
public static class PressureScenarios
{
    private static readonly List<byte[]> LongLivedStorage = new();

    // Мелкие объекты живут недолго: весь мусор убирает дешёвая сборка Gen0,
    // до Gen2 они не доживают, поэтому Gen2 = 0. Gen0 может быть 0, если
    // 12 МБ аллокаций не исчерпали бюджет молодого поколения.
    public static ScenarioResult RunSmallObjects(int count = 100_000)
    {
        var before = GcSnapshot.Capture();
        var stopwatch = Stopwatch.StartNew();

        long checksum = 0;
        for (var i = 0; i < count; i++)
        {
            var buffer = new byte[100];
            buffer[0] = (byte)i;
            checksum += buffer[0];
        }

        stopwatch.Stop();
        var after = GcSnapshot.Capture();
        GC.KeepAlive(checksum);

        return new ScenarioResult("Мелкие объекты (byte[100] × 100 000)",
            stopwatch.Elapsed, after.Diff(before));
    }

    // Массив больше 85 000 байт попадает сразу в LOH, минуя Gen0, поэтому
    // Gen0 не растёт. Gen2 растёт только при реальной сборке Gen2,
    // а ~1 МБ аллокаций для её запуска мало.
    public static ScenarioResult RunLargeObjects(int count = 10)
    {
        var before = GcSnapshot.Capture();
        var stopwatch = Stopwatch.StartNew();

        var retained = new List<byte[]>(count);
        for (var i = 0; i < count; i++)
        {
            retained.Add(new byte[100_000]);
        }

        stopwatch.Stop();
        var after = GcSnapshot.Capture();
        GC.KeepAlive(retained);

        return new ScenarioResult("Крупные объекты (byte[100 000] × 10)",
            stopwatch.Elapsed, after.Diff(before));
    }

    // Объекты удерживаются статическим списком, поэтому переживают сборки Gen0
    // и продвигаются в старшие поколения: куча растёт (Heap Δ ≈ объём данных).
    // Gen2 не растёт: продвижение в Gen2 — это ещё не сборка Gen2.
    public static ScenarioResult RunLongLived(int count = 20_000)
    {
        var before = GcSnapshot.Capture();
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < count; i++)
        {
            LongLivedStorage.Add(new byte[1024]);
        }

        stopwatch.Stop();
        var after = GcSnapshot.Capture();

        return new ScenarioResult("Удерживаемые (byte[1024] × 20 000)",
            stopwatch.Elapsed, after.Diff(before));
    }

    // Полная сборка Gen2 включает сборку младших поколений,
    // поэтому Gen0, Gen1 и Gen2 растут на единицу одновременно.
    public static ScenarioResult RunForcedGen2()
    {
        var before = GcSnapshot.Capture();
        var stopwatch = Stopwatch.StartNew();

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        stopwatch.Stop();
        var after = GcSnapshot.Capture();

        return new ScenarioResult("Принудительный Gen2 (blocking)",
            stopwatch.Elapsed, after.Diff(before));
    }

    public static void RunAll()
    {
        var startAllocated = GC.GetTotalAllocatedBytes(precise: true);

        Console.WriteLine("Сценарии давления на GC");
        Console.WriteLine($"Аллоцировано с начала процесса: {startAllocated / 1024d / 1024d:F2} МБ");
        GcObserver.PrintMemoryInfo("Состояние кучи до экспериментов");

        Console.WriteLine(new string('-', 92));
        Console.WriteLine(
            "{0,-38}{1,10}{2,6}{3,6}{4,6}{5,11}{6,12}",
            "Сценарий", "Время, мс", "Gen0", "Gen1", "Gen2",
            "Alloc, МБ", "Heap Δ, МБ");
        Console.WriteLine(new string('-', 92));

        var results = new[]
        {
            RunSmallObjects(),
            RunLargeObjects(),
            RunLongLived(),
            RunForcedGen2()
        };

        foreach (var result in results)
        {
            Console.WriteLine(
                "{0,-38}{1,10:F2}{2,6}{3,6}{4,6}{5,11:F2}{6,12:F2}",
                result.Name,
                result.Elapsed.TotalMilliseconds,
                result.Delta.Gen0,
                result.Delta.Gen1,
                result.Delta.Gen2,
                result.Delta.AllocatedMegabytes,
                result.Delta.HeapDeltaMegabytes);
        }

        Console.WriteLine(new string('-', 92));

        var totalAllocated = GC.GetTotalAllocatedBytes(precise: true) - startAllocated;
        Console.WriteLine($"Всего аллоцировано за эксперименты: {totalAllocated / 1024d / 1024d:F2} МБ");
        GcObserver.PrintMemoryInfo("Состояние кучи после экспериментов");

        Console.WriteLine(
            "Значения зависят от машины, режима GC и момента запуска. " +
            "Важна не абсолютная величина, а соотношение счётчиков.");
    }
}

public readonly record struct ScenarioResult(
    string Name, TimeSpan Elapsed, GcDelta Delta);
