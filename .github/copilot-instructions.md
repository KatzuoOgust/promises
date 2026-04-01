# Cqrs — Copilot Instructions

> Design rules, naming conventions, namespace policy, and architecture details are in **[AGENTS.md](../AGENTS.md)** — read it first.

## Build & test

```sh
make build    # dotnet build Cqrs.slnx
make test     # dotnet test Cqrs.slnx

# Single test class
dotnet test tests/Cqrs.Tests --filter "FullyQualifiedName~NullCommandHandlerTests"

# Single test method
dotnet test tests/Cqrs.Tests --filter "FullyQualifiedName~NullCommandHandlerTests.HandleAsync_CompletesWithoutThrowing"
```
