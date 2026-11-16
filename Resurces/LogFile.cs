namespace MemoryLifecycle.Resurces;

/// <summary>
/// КТ, вариант 2. Лог-файл: обёртка над StreamWriter, реализующая
/// полный Dispose-паттерн (IDisposable + финализатор как страховка).
/// </summary>
public sealed class LogFile : IDisposable
{
    private readonly string _path;
    private StreamWriter? _writer;
    private bool _disposed;

    public LogFile(string path)
    {
        _path = path;
        _writer = new StreamWriter(path, append: false);
        Console.WriteLine($"[LogFile] файл открыт: {_path}");
    }

    public void Write(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _writer!.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {message}");
        Console.WriteLine($"[LogFile] запись: {message}");
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        // Ресурс уже освобождён — финализатор больше не нужен,
        // объект не попадёт в очередь финализации.
        GC.SuppressFinalize(this);
    }

    // Финализатор срабатывает, только если Dispose() забыли вызвать.
    ~LogFile()
    {
        Console.WriteLine($"[LogFile] финализатор! — файл не был закрыт явно: {_path}");
        Dispose(disposing: false);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Вызов из Dispose(): управляемые объекты ещё живы,
            // их можно безопасно закрыть (Flush + закрытие дескриптора).
            _writer?.Dispose();
            Console.WriteLine($"[LogFile] файл закрыт: {_path}");
        }
        // Из финализатора к _writer обращаться нельзя: он сам может быть
        // уже финализирован. Его FileStream/SafeHandle закроется своим
        // финализатором, но буфер StreamWriter при этом будет потерян.

        _writer = null;
        _disposed = true;
    }
}
