using MemoryLifecycle.Demos;
using MemoryLifecycle.Gc;

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

switch (command)
{
    case "gc-pressure":
        PressureScenarios.RunAll();
        break;

    case "logfile":
        ResourceDemos.RunLogFile();
        break;

    case "dispose":
        await DisposeDemos.RunSyncAndAsyncDemoAsync();
        await DisposeDemos.RunFileBufferDemoAsync();
        break;

    case "all":
        PressureScenarios.RunAll();
        ResourceDemos.RunLogFile();
        await DisposeDemos.RunSyncAndAsyncDemoAsync();
        await DisposeDemos.RunFileBufferDemoAsync();
        break;

    default:
        Console.WriteLine("Использование: dotnet run --project src/MemoryLifecycle -c Release -- <команда>");
        Console.WriteLine("Команды:");
        Console.WriteLine("  gc-pressure  КТ №40: сценарии давления на GC");
        Console.WriteLine("  logfile      КТ, вариант 2: LogFile и Dispose-паттерн");
        Console.WriteLine("  dispose      КТ №43: DatabaseConnection, FileBuffer, IAsyncDisposable");
        Console.WriteLine("  all          все реализованные демонстрации");
        break;
}
