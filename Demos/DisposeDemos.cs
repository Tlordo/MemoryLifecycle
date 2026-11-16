using System.Runtime.CompilerServices;
using MemoryLifecycle.Resources;

namespace MemoryLifecycle.Demos;

/// <summary>
/// КТ №43. Демонстрация полного паттерна Dispose и IAsyncDisposable.
/// </summary>
public static class DisposeDemos
{
    // ---------- Вариант 1. DatabaseConnection ----------
    public static async Task RunSyncAndAsyncDemoAsync()
    {
        Header("Вариант 1. DatabaseConnection");

        Console.WriteLine("1. Синхронный сценарий using");
        using (var connection = new DatabaseConnection())
        {
            connection.Execute("CREATE TABLE Students (Id INT, Name TEXT)");
            connection.Execute("INSERT INTO Students VALUES (1, 'Иван')");
            connection.Execute("SELECT * FROM Students");
        }

        Console.WriteLine();
        Console.WriteLine("2. Асинхронный сценарий await using");
        await using (var connection = new DatabaseConnection())
        {
            connection.Execute("SELECT * FROM Topics");
            await Task.Delay(1);
        }

        VerifySuppressFinalize();

        Console.WriteLine();
        Console.WriteLine("4. Финализатор без Dispose");
        CreateConnectionWithoutDispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine("После сборки: финализатор освободил только нативный ресурс (журнал не тронут)");

        Console.WriteLine();
        Console.WriteLine("5. Запрос после Dispose()");
        var closed = new DatabaseConnection();
        closed.Dispose();
        try
        {
            closed.Execute("SELECT 1");
        }
        catch (ObjectDisposedException ex)
        {
            Console.WriteLine($"Поймано ObjectDisposedException: {ex.ObjectName}");
        }
    }

    public static void VerifySuppressFinalize()
    {
        Console.WriteLine();
        Console.WriteLine("3. Проверка GC.SuppressFinalize после using");
        using (var connection = new DatabaseConnection())
        {
            connection.Execute("SELECT 1");
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine("После сборки: сообщение финализатора отсутствует");
    }

    // NoInlining: после возврата ссылок на объект гарантированно нет,
    // и GC может его собрать даже в Debug-сборке.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateConnectionWithoutDispose()
    {
        var connection = new DatabaseConnection();
        connection.Execute("UPDATE Students SET Name = 'Test'");
    }

    // ---------- Вариант 2. FileBuffer ----------
    public static async Task RunFileBufferDemoAsync()
    {
        Header("Вариант 2. FileBuffer");

        Console.WriteLine("1. Синхронный сценарий using");
        using (var buffer = new FileBuffer())
        {
            buffer.Write(new byte[] { 1, 2, 3, 4 });
        }

        Console.WriteLine();
        Console.WriteLine("2. Асинхронный сценарий await using");
        await using (var buffer = new FileBuffer())
        {
            buffer.Write(new byte[] { 5, 6, 7 });
            await Task.Delay(1);
        }

        Console.WriteLine();
        Console.WriteLine("3. Повторный Dispose()");
        var twice = new FileBuffer();
        twice.Write(new byte[] { 8, 9 });
        twice.Dispose();
        twice.Dispose(); // без повторного освобождения и без исключения
        await twice.DisposeAsync(); // тоже ничего не делает

        Console.WriteLine();
        Console.WriteLine("4. Финализатор без Dispose");
        CreateBufferWithoutDispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine("После сборки: закрыт только fileHandle, MemoryStream не тронут");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateBufferWithoutDispose()
    {
        var buffer = new FileBuffer();
        buffer.Write(new byte[] { 10, 11, 12 });
    }

    private static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"========== {title} ==========");
    }
}
