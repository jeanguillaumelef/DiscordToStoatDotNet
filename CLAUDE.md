# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this project is

DiscordToStoat bridges Discord to [Stoat](https://stoat.chat) (the chat platform formerly
known as Revolt). The long-term goal is to mirror/forward Discord activity into a Stoat server.

## Stack

- .NET 9 SDK, C# (nullable reference types enabled, implicit usings enabled).
- A multi-project solution: `DiscordToStoat.csproj` is the composition root; the domain and
  each adapter are their own projects (see Layout).
- Discord adapter (`DiscordRepository`) uses [Discord.Net](https://github.com/discord-net/Discord.Net)
  ([NuGet](https://www.nuget.org/packages/Discord.Net)) as its SDK.
- Stoat adapter (`StoatRepository`) uses [StoatSharp](https://www.nuget.org/packages/StoatSharp)
  as its SDK.

## Commands

```bash
dotnet build   # builds the whole solution; fails on type/nullability errors in any project
dotnet run --project DiscordToStoat.csproj   # builds and runs the app
dotnet test    # runs the test project(s)
```

Those are the only commands. Do not document or reference scripts/targets that don't exist in
the `.csproj`/solution.

## Layout

Hexagonal architecture (ports and adapters), one .NET project per side of the hexagon.
"Repository" in this project means an external system (Discord, Stoat), not an EF Core /
persistence repository. Only the composition root keeps the `DiscordToStoat` name; the domain
and adapter projects are named plainly (no shared prefix).

```
DiscordToStoat.csproj    # composition root: reads configuration, builds adapters, wires them into the domain
Domain                       # its own project: business logic + port definitions (interfaces)
<System>Repository           # one project per external system: DiscordRepository/, StoatRepository/
```

- Project references only flow one way: `DiscordToStoat.csproj` references `Domain` and every
  adapter project; each adapter project references only `Domain`. Adapter projects never reference
  each other, and `Domain` references nothing in this solution.
- Tests live in a sibling test project per project under test (e.g. `Domain.Tests/`,
  `DiscordRepository.Tests/`), referencing only the project it tests. Keeping tests in separate
  assemblies keeps test-framework references out of `Domain` and the adapter projects.
- Create a new project only when its first real module is written; add it to the solution file
  when it's created.

When editing `Domain`:
- Keep it pure: no network, filesystem, environment variables, or third-party NuGet packages —
  and no `ProjectReference` to `DiscordToStoat.csproj` or any adapter project.
- Define ports as C# interfaces.
- Receive adapters via constructor injection. Never `new` up an adapter.
- Test with in-memory fakes of the ports, never real adapters.

When editing an adapter project (`DiscordRepository`, `StoatRepository`, ...):
- Reference `Domain`, the BCL, and that system's own SDK/NuGet package. Never add a
  `ProjectReference` to another adapter project or to `DiscordToStoat.csproj`.
- Receive config (tokens, URLs) via constructor arguments (bound from `IConfiguration`). Never
  read environment variables or config directly inside an adapter class — bind and pass it in
  from `DiscordToStoat.csproj`.

## Conventions

- PascalCase for types, methods, and public members; camelCase with a leading underscore for
  private fields (`_client`); camelCase for locals and parameters.
- Use `async`/`await`, not `.Result`/`.Wait()`/raw `ContinueWith`. Never leave a `Task`
  unawaited/unobserved.
- Avoid `dynamic` and bare `object`; prefer strongly-typed models, generics, or sealed
  record/class hierarchies when a shape genuinely varies.
- Secrets (Discord/Stoat tokens) come from environment variables or `dotnet user-secrets` in
  development — never from a committed `appsettings.json`. Never commit tokens. Document new
  required variables in `appsettings.Example.json` or the README when they are introduced.
- Prefer the BCL first. When a NuGet package is needed, add it with `dotnet add package <pkg>` so
  the `.csproj`/lock file is updated, and say why it was chosen in the commit message.
- One class or interface per file, named after the type it contains (e.g. `IDiscordRepository`
  goes in `IDiscordRepository.cs`). Don't group multiple types into one file.

## Tooling

- Tests: xUnit, run via `dotnet test`. Never add NUnit, MSTest, or another test framework.
- All new or changed tests must be written by delegating to the `test-writer` agent (Agent tool,
  `subagent_type: test-writer`), not written directly in the main session. The agent runs
  `dotnet test` itself; after it returns, confirm with a `dotnet test` run.
- Type-check as part of the build (the C# compiler fails `dotnet build` on type and nullability
  errors); there is no separate typecheck step.
- Format: `dotnet format`. Run it only through the command that exists for this repo — don't wire
  up a new formatter/linter as a side effect of another task.

## Working with the owner

Do only what is asked. Do not add tooling, restructure files, or install dependencies as a
side effect of another task — propose it instead.
