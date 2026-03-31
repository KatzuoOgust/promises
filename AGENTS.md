# Agent Context

## Project identity

.NET 10 class library. Language: C#. Build tool: `dotnet`. Test framework: xUnit.

## Commands

```sh
dotnet build          # build all projects
dotnet test           # run all 18 tests
dotnet format         # apply .editorconfig formatting (tabs, etc.)
```

## Structure

```
src/
  Promises/             core library (KatzuoOgust.Promises)
  Promises.InMemory/    in-memory store (KatzuoOgust.Promises.InMemory)
  Promises.FileSystem/  file-system store (KatzuoOgust.Promises.FileSystem)
tests/
  Promises/             xUnit tests for all three projects
```

## Hard rules

- **Tabs** for C# indentation — `.editorconfig` enforces this; never use spaces in `.cs` files
- **`ConfigureAwait(false)`** on every `await` in library code (`src/`)
- **`RootNamespace` and `AssemblyName`** must be set explicitly in every `.csproj` — folder names do not carry the `KatzuoOgust.` prefix
- Test namespace is `KatzuoOgust.Promises` (no `.Tests` suffix) — `RootNamespace` in `tests/Promises/Promises.csproj` is set accordingly
- New store implementations go in their own project under `src/`; never add store code to the core `Promises` project
- All exceptions extend `PromiseException` and live in `src/Promises/Exceptions.cs`
- Do not add polling, retry, or scheduling logic to this library — that is the caller's responsibility

## Conventions

- `IPromiseStore<T>` is the primary extension point
- `Promise<T>` is a lightweight handle — it holds an id and a store reference; it must remain stateless
- Every store read is non-caching: each `CheckAsync` / `GetResultAsync` call hits the store
- `FileSystemPromiseStore<T>` uses `System.Text.Json` with `JsonStringEnumConverter`
