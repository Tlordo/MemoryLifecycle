using System.Runtime.CompilerServices;
using MemoryLifecycle.Resources;

namespace MemoryLifecycle.Demos;

/// <summary>
/// Демонстрации жизненного цикла LogFile (КТ, вариант 2).
/// </summary>
public static class ResourceDemos
{
    public static void RunLogFile()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(dir);

        CorrectUsingBlock(Path.Combine(dir, "correct.log"));
        IncorrectNoDispose(Path.Combine(dir, "forgotten.log"));
        UsingDeclaration(Path.Combine(dir, "test.log"));

        Console.WriteLine();
        Console.WriteLine($"Файлы лежат в: {dir}");
    }

    // 3. Корректный сценарий: using-блок, Dispose() вызывается автоматически
    //    в конце блока (компилятор разворачивает его в try/finally).
    private static void CorrectUsingBlock(string path)
    {
        Header("1) Корректно: using-блок");
        using (var log = new LogFile(path))
        {
            log.Write("Приложение запущено");
            log.Write("Обработка данных");
            Console.WriteLine("--- конец блока using ---");
        }
        Console.WriteLine("После блока: файл уже закрыт, финализатор не вызовется.");
    }

    // 4. Некорректный сценарий: ни using, ни Dispose().
    private static void IncorrectNoDispose(string path)
    {
        Header("2) Некорректно: без using и без Dispose()");
        CreateAndForget(path);

        Console.WriteLine("Вызываем GC.Collect() и GC.WaitForPendingFinalizers()...");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine("Финализатор отработал. Записи, оставшиеся в буфере StreamWriter, потеряны.");
    }

    // Объект создаётся в отдельном методе, чтобы после возврата на него
    // гарантированно не осталось ссылок (в Debug JIT продлевает жизнь
    // локальных переменных до конца метода).
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateAndForget(string path)
    {
        var log = new LogFile(path);
        log.Write("Эту запись забыли сбросить на диск");
        // Dispose() не вызван — ссылка просто теряется.
    }

    // 5. using-декларация без скобок (C# 8+): Dispose() вызывается
    //    при выходе из области видимости — в конце метода.
    private static void UsingDeclaration(string path)
    {
        Header("3) using-декларация: using var log = ...");
        using var log = new LogFile(path);
        log.Write("Запись через using-декларацию");
        Console.WriteLine("--- конец метода, сейчас будет вызван Dispose() ---");
    }

    private static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }
}
