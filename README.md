# KatzuoOgust.Promises

Tiny .NET 10 library for tracking distributed and background task results through a **promise** abstraction.

A promise is created when a task is dispatched. The worker resolves or fails it later. The caller checks the result **on demand** — each call re-queries the store with no hidden polling.

## Quick start

```csharp
using KatzuoOgust.Promises;
using KatzuoOgust.Promises.InMemory;

IPromiseStore<string> store = new InMemoryPromiseStore<string>();

// Dispatch side — create a pending promise and pass its ID to the worker
Promise<string> promise = await store.CreateAsync();

// Worker side — resolve or fail by ID
await store.ResolveAsync(promise.Id, "result value");
// or
await store.FailAsync(promise.Id, "something went wrong");

// Caller side — check on demand; each call re-queries the store
PromiseRecord<string> record = await promise.CheckAsync();   // any status
string result = await promise.GetResultAsync();              // T or throws
```

`GetResultAsync` throws:
- `PromiseNotResolvedException` — promise is still pending
- `PromiseFailedException` — promise failed (message included)
- `PromiseNotFoundException` — id not found in the store

## Projects

| Package | Purpose |
|---|---|
| `KatzuoOgust.Promises` | Core — `IPromiseStore<T>`, `Promise<T>`, `PromiseRecord<T>`, exceptions |
| `KatzuoOgust.Promises.InMemory` | `InMemoryPromiseStore<T>` — thread-safe, process-scoped |
| `KatzuoOgust.Promises.FileSystem` | `FileSystemPromiseStore<T>` — one JSON file per promise |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).
