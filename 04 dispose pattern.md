# КТ №43. Паттерн Dispose. IAsyncDisposable (.NET 10)

> **Вариант 1 — «Соединение с БД»** (дополнительно реализован вариант 2 — «Файловый буфер»)
> Команда запуска: `dotnet run -c Release -- dispose`

## Цель

Реализовать полный паттерн Dispose с разделением управляемых и нативных ресурсов,
поддержать асинхронное освобождение через `IAsyncDisposable` и показать разницу
между детерминированным освобождением (`using` / `await using`) и финализатором.

## Задачи

1. Реализовать `DatabaseConnection : IDisposable, IAsyncDisposable` с управляемым
   ресурсом `List<string>` (журнал запросов) и «нативным» `int connectionId`.
2. Реализовать `protected virtual void Dispose(bool disposing)`: управляемое
   освобождается только при `disposing == true`, `connectionId` — всегда.
3. Реализовать публичный `Dispose()` с `GC.SuppressFinalize(this)`.
4. Добавить финализатор, вызывающий `Dispose(false)`.
5. Реализовать `DisposeAsync()` через `DisposeAsyncCore()`; асинхронное закрытие
   имитируется `await Task.Delay(10)`.
6. Продемонстрировать `using` и `await using`.

## Реализация

| Файл | Назначение |
|---|---|
| `Resurces/DatabaseConnection.cs` | Вариант 1: журнал запросов + дескриптор соединения |
| `Resurces/FileBuffer.cs` | Вариант 2: `MemoryStream` + дескриптор файла |
| `Demos/DisposeDemos.cs` | Сценарии демонстрации |
| `Program.cs` | Команда `dispose` |

Схема паттерна в `DatabaseConnection`:

```csharp
public void Dispose()
{
    Dispose(disposing: true);   // освобождаем всё
    GC.SuppressFinalize(this);  // финализатор больше не нужен
}

public async ValueTask DisposeAsync()
{
    try   { await DisposeAsyncCore().ConfigureAwait(false); } // управляемое — асинхронно
    finally
    {
        Dispose(disposing: false);  // нативное — даже если async-часть упала
        GC.SuppressFinalize(this);
    }
}

protected virtual void Dispose(bool disposing)
{
    if (disposed) return;                 // идемпотентность
    if (disposing) { queryLog?.Clear(); queryLog = null; } // только из Dispose()
    disposed = true;                      // нативный ресурс — всегда
    connectionId = 0;
}

~DatabaseConnection() => Dispose(disposing: false); // страховка
```

Проверка критериев методички:

| Требование | Где выполнено |
|---|---|
| Проверка `disposed` в `Dispose(bool)` и `DisposeAsyncCore()` | первые строки обоих методов |
| Управляемое освобождается только внутри `if (disposing)` | `Dispose(bool)` |
| `GC.SuppressFinalize` и в `Dispose()`, и в `DisposeAsync()` | оба метода |
| Нативный ресурс закрывается даже при исключении в async-части | `finally` в `DisposeAsync()` |
| `await using` только в `async Task`-методе | `RunSyncAndAsyncDemoAsync()` |
| Использование после освобождения запрещено | `ObjectDisposedException.ThrowIf(disposed, this)` |

## Сценарии демонстрации

**Вариант 1. DatabaseConnection**

1. `using` — три запроса, затем «журнал очищен» → «соединение закрыто».
2. `await using` — «журнал очищен асинхронно» → «соединение закрыто».
3. Проверка `SuppressFinalize`: после `using` вызываются `GC.Collect()` +
   `GC.WaitForPendingFinalizers()` — сообщения финализатора **нет**.
4. Объект без `Dispose()` (создаётся в методе с `NoInlining`) → после GC появляется
   «финализатор: Dispose() не был вызван» и закрывается **только дескриптор**,
   строки «журнал запросов очищен» нет.
5. Запрос после `Dispose()` → `ObjectDisposedException`.

**Вариант 2. FileBuffer** — `using`, `await using`, повторный `Dispose()` (без
повторного освобождения и без исключения), финализатор без `Dispose()`.

> В сценарии 3 строка «повторный Dispose() — ничего не делаем» выводится дважды:
> второй вызов `Dispose()` и затем `DisposeAsync()` (его `DisposeAsyncCore()` сразу
> выходит по флагу `disposed`, а `Dispose(false)` печатает сообщение).

## Результаты

**Условия запуска:** Windows 10 (build 19045.6456), .NET SDK 10.0.401, конфигурация `Release`.

```text
========== Вариант 1. DatabaseConnection ==========
1. Синхронный сценарий using
[DatabaseConnection 1] соединение открыто
[DatabaseConnection 1] запрос: CREATE TABLE Students (Id INT, Name TEXT)
[DatabaseConnection 1] запрос: INSERT INTO Students VALUES (1, 'Иван')
[DatabaseConnection 1] запрос: SELECT * FROM Students
[DatabaseConnection 1] журнал запросов очищен
[DatabaseConnection 1] соединение закрыто (дескриптор 1 освобождён)

2. Асинхронный сценарий await using
[DatabaseConnection 2] соединение открыто
[DatabaseConnection 2] запрос: SELECT * FROM Topics
[DatabaseConnection 2] журнал запросов очищен асинхронно
[DatabaseConnection 2] соединение закрыто (дескриптор 2 освобождён)

3. Проверка GC.SuppressFinalize после using
[DatabaseConnection 3] соединение открыто
[DatabaseConnection 3] запрос: SELECT 1
[DatabaseConnection 3] журнал запросов очищен
[DatabaseConnection 3] соединение закрыто (дескриптор 3 освобождён)
После сборки: сообщение финализатора отсутствует

4. Финализатор без Dispose
[DatabaseConnection 4] соединение открыто
[DatabaseConnection 4] запрос: UPDATE Students SET Name = 'Test'
[DatabaseConnection 4] финализатор: Dispose() не был вызван
[DatabaseConnection 4] соединение закрыто (дескриптор 4 освобождён)
После сборки: финализатор освободил только нативный ресурс (журнал не тронут)

5. Запрос после Dispose()
[DatabaseConnection 5] соединение открыто
[DatabaseConnection 5] журнал запросов очищен
[DatabaseConnection 5] соединение закрыто (дескриптор 5 освобождён)
Поймано ObjectDisposedException: MemoryLifecycle.Resources.DatabaseConnection

========== Вариант 2. FileBuffer ==========
1. Синхронный сценарий using
[FileBuffer 1] буфер открыт, дескриптор 1
[FileBuffer 1] записано 4 байт (всего 4)
[FileBuffer 1] буфер освобождён
[FileBuffer 1] дескриптор 1 закрыт

2. Асинхронный сценарий await using
[FileBuffer 2] буфер открыт, дескриптор 2
[FileBuffer 2] записано 3 байт (всего 3)
[FileBuffer 2] буфер освобождён асинхронно
[FileBuffer 2] дескриптор 2 закрыт

3. Повторный Dispose()
[FileBuffer 3] буфер открыт, дескриптор 3
[FileBuffer 3] записано 2 байт (всего 2)
[FileBuffer 3] буфер освобождён
[FileBuffer 3] дескриптор 3 закрыт
[FileBuffer 3] повторный Dispose() — ничего не делаем
[FileBuffer 3] повторный Dispose() — ничего не делаем

4. Финализатор без Dispose
[FileBuffer 4] буфер открыт, дескриптор 4
[FileBuffer 4] записано 3 байт (всего 3)
[FileBuffer 4] финализатор: Dispose() не был вызван
[FileBuffer 4] дескриптор 4 закрыт
После сборки: закрыт только fileHandle, MemoryStream не тронут
```

![Вывод команды dispose](img/kt43-dispose.jpg)

## Выводы

1. `Dispose()` освобождает ресурс **детерминированно** — ровно в конце `using`;
   финализатор срабатывает когда-нибудь после сборки и только как страховка.
2. `Dispose(bool disposing)` разделяет ресурсы: при `true` можно трогать управляемые
   объекты, при `false` (из финализатора) — только нативные, т.к. управляемые
   объекты могли быть уже собраны. В выводе сценария 4 это видно: журнал не очищается.
3. `GC.SuppressFinalize` снимает объект с очереди финализации — после `using`
   сообщение финализатора не появляется, объект собирается за одну сборку.
4. `IAsyncDisposable` + `await using` позволяет освобождать ресурсы с асинхронным
   I/O (сеть, сброс на диск) без блокировки потока; реализация обоих интерфейсов
   даёт работу и в синхронном, и в асинхронном коде.

## Ответы на контрольные вопросы

**20. Зачем нужен `Dispose(bool disposing)`?**
Это единая точка освобождения для двух путей. `true` — вызов из `Dispose()`:
освобождаются управляемые (`List`, `Stream`) и нативные ресурсы. `false` — вызов из
финализатора: только нативные (дескрипторы), потому что управляемые объекты могут
быть уже финализированы.

**21. Что делает `GC.SuppressFinalize`? Почему он и в `Dispose()`, и в `DisposeAsync()`?**
Убирает объект из очереди финализации: финализатор не вызовется, объект не
переживёт лишнюю сборку. Вызывается в обоих методах, потому что любой из них может
быть единственным способом освобождения объекта.

**22. Что такое `IAsyncDisposable`?**
Интерфейс с `ValueTask DisposeAsync()`. Нужен, когда освобождение само по себе
асинхронное: закрытие сетевого соединения, сброс буфера в файл/поток, завершение
транзакции.

**23. Почему реализуют оба интерфейса?**
Потребитель может работать как в синхронном коде (`using`), так и в асинхронном
(`await using`). Класс только с `IAsyncDisposable` нельзя использовать в обычном
`using`, а синхронный путь нужен и для финализатора.

**24. Что делает `await using`?**
Компилятор разворачивает его в `try { ... } finally { await obj.DisposeAsync(); }`.
Обычный `using` вызывает синхронный `Dispose()`. `await using` можно применять
только в `async`-методе.

**25. Как производный класс переопределяет `Dispose(bool)` и `DisposeAsyncCore()`?**
Через `protected override`: сначала освобождает свои ресурсы (с собственной
проверкой `disposed`), затем вызывает `base.Dispose(disposing)` /
`await base.DisposeAsyncCore()`. Публичные `Dispose()`/`DisposeAsync()` и финализатор
не переопределяются.

**26. Как доказать, что финализатор не вызвался после `Dispose()`?**
После `using` вызвать `GC.Collect()` и `GC.WaitForPendingFinalizers()` и убедиться,
что сообщение из финализатора не выведено (сценарий 3 в `DisposeDemos`).
Для контраста сценарий 4 без `Dispose()` это сообщение выводит.

**27. Типичные ошибки в `DisposeAsync()`**
- нет `GC.SuppressFinalize`;
- нативный ресурс не закрывается, если async-часть бросила исключение (нет `finally`);
- нет проверки `disposed` в `DisposeAsyncCore()` → повторная очистка;
- класс реализует только `IAsyncDisposable`, но используется через обычный `using`;
- `await using` в методе, который не является `async`.