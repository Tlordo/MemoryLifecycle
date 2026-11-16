namespace MemoryLifecycle.Resources;

/// <summary>
/// КТ №43, вариант 2. Имитация файлового буфера:
/// управляемый ресурс — MemoryStream, «нативный» — дескриптор файла (int fileHandle).
/// </summary>
public class FileBuffer : IDisposable, IAsyncDisposable
{
    private static int counter;

    private int fileHandle = Interlocked.Increment(ref counter);
    private readonly int id;
    private MemoryStream? buffer = new();
    private bool disposed;

    public FileBuffer()
    {
        id = fileHandle;
        Console.WriteLine($"[FileBuffer {id}] буфер открыт, дескриптор {fileHandle}");
    }

    public bool IsDisposed => disposed;

    public long Length => buffer?.Length ?? 0;

    public void Write(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        buffer!.Write(data);
        Console.WriteLine($"[FileBuffer {id}] записано {data.Length} байт (всего {buffer.Length})");
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

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
            Console.WriteLine($"[FileBuffer {id}] повторный Dispose() — ничего не делаем");
            return;
        }

        if (disposing)
        {
            buffer?.Dispose();
            buffer = null; // повторный вызов не обратится к закрытому потоку
            Console.WriteLine($"[FileBuffer {id}] буфер освобождён");
        }

        disposed = true;
        Console.WriteLine($"[FileBuffer {id}] дескриптор {fileHandle} закрыт");
        fileHandle = 0;
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (disposed)
        {
            return;
        }

        if (buffer is not null)
        {
            // Имитация асинхронного сброса буфера на диск.
            await buffer.FlushAsync().ConfigureAwait(false);
            await Task.Delay(10).ConfigureAwait(false);
            await buffer.DisposeAsync().ConfigureAwait(false);
            buffer = null;
        }
        Console.WriteLine($"[FileBuffer {id}] буфер освобождён асинхронно");
    }

    ~FileBuffer()
    {
        Console.WriteLine($"[FileBuffer {id}] финализатор: Dispose() не был вызван");
        Dispose(disposing: false);
    }
}
