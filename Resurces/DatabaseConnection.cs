namespace MemoryLifecycle.Resources;

/// <summary>
/// КТ №43, вариант 1. Имитация соединения с базой данных:
/// управляемый ресурс — журнал запросов (List&lt;string&gt;),
/// «нативный» ресурс — дескриптор соединения (int connectionId).
/// </summary>
public class DatabaseConnection : IDisposable, IAsyncDisposable
{
    private static int counter;

    // «Нативный» дескриптор: в реальном драйвере это был бы хэндл сокета ОС.
    private int connectionId = Interlocked.Increment(ref counter);

    // Управляемый ресурс.
    private List<string>? queryLog = new();

    private bool disposed;

    public DatabaseConnection()
    {
        Console.WriteLine($"[DatabaseConnection {connectionId}] соединение открыто");
    }

    public bool IsDisposed => disposed;

    public int ConnectionId => connectionId;

    public void Execute(string query)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        queryLog!.Add(query);
        Console.WriteLine($"[DatabaseConnection {connectionId}] запрос: {query}");
    }

    // Синхронный путь: освобождаем всё и снимаем объект с очереди финализации.
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    // Асинхронный путь: сначала асинхронная очистка управляемого,
    // затем (в finally — даже при исключении) закрытие нативного.
    public async ValueTask DisposeAsync()
    {
        try
        {
            await DisposeAsyncCore().ConfigureAwait(false);
        }
        finally
        {
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            // Только из Dispose(): из финализатора управляемые объекты трогать нельзя.
            queryLog?.Clear();
            queryLog = null;
            Console.WriteLine($"[DatabaseConnection {connectionId}] журнал запросов очищен");
        }

        // Нативный ресурс освобождается всегда.
        disposed = true;
        Console.WriteLine($"[DatabaseConnection {connectionId}] соединение закрыто (дескриптор {connectionId} освобождён)");
        connectionId = 0;
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (disposed)
        {
            return;
        }

        await Task.Delay(10).ConfigureAwait(false); // имитация закрытия по сети
        queryLog?.Clear();
        queryLog = null;
        Console.WriteLine($"[DatabaseConnection {connectionId}] журнал запросов очищен асинхронно");
    }

    // Последний шанс, если Dispose() забыли: освобождает только нативное.
    ~DatabaseConnection()
    {
        Console.WriteLine($"[DatabaseConnection {connectionId}] финализатор: Dispose() не был вызван");
        Dispose(disposing: false);
    }
}
