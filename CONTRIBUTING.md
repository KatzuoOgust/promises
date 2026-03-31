# Contributing

Fork → branch → change → test → PR.

## Setup

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).

```sh
git clone <your-fork>
cd promises
dotnet build
dotnet test
```

## Development workflow

- Branch from `main`; use short descriptive names (`feature/fs-store-locking`, `fix/null-result`)
- Run `dotnet format` before committing — enforced by `.editorconfig`
- All tests must pass: `dotnet test`
- Prefer small, focused PRs over large sweeping ones

## Code conventions

- Tabs for indentation in `.cs` files (see `.editorconfig`)
- `ConfigureAwait(false)` on all `await` calls in library code
- Exceptions live in `Exceptions.cs`; add new ones there
- `IPromiseStore<T>` is the extension point — new store implementations belong in their own project under `src/`

## Adding a new store

1. Create `src/Promises.<Name>/` with a `Promises.<Name>.csproj`
2. Set `AssemblyName` and `RootNamespace` to `KatzuoOgust.Promises.<Name>`
3. Implement `IPromiseStore<T>`
4. Add tests in `tests/Promises/` using the existing test patterns

## Out of scope

- Polling or retry loops inside the library — callers own that
- Auto-generated files under `src/*/obj/` and `src/*/bin/`
