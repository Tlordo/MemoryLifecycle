using MemoryLifecycle.Gc;

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

switch (command)
{
    case "gc-pressure":
        PressureScenarios.RunAll();
        break;

    case "all":
        PressureScenarios.RunAll();
        break;

    default:
        Console.WriteLine("Использование: dotnet run --project src/MemoryLifecycle -c Release -- <команда>");
        Console.WriteLine("Команды:");
        Console.WriteLine("  gc-pressure  КТ №40: сценарии давления на GC");
        Console.WriteLine("  all          все реализованные демонстрации");
        break;
}
