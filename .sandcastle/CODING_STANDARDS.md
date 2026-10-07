# Coding Standards

The reviewer agent loads this file during code review via @.sandcastle/CODING_STANDARDS.md
so these standards are enforced during review without costing tokens during implementation.
Mirrors the conventions in the repo's own CLAUDE.md — keep the two in sync if either changes.

## Style

- PascalCase for types, methods, and public members; camelCase with a leading underscore for
  private fields (`_client`); camelCase for locals and parameters.
- Use `async`/`await`, not `.Result`/`.Wait()`/raw `ContinueWith`. Never leave a `Task`
  unawaited/unobserved.
- Avoid `dynamic` and bare `object`; prefer strongly-typed models, generics, or sealed
  record/class hierarchies when a shape genuinely varies.
- One class or interface per file, named after the type it contains (e.g. `IDiscordRepository`
  goes in `IDiscordRepository.cs`). Don't group multiple types into one file.
- Prefer the BCL first. When a NuGet package is genuinely needed, add it with
  `dotnet add package <pkg>` so the `.csproj`/lock file is updated, and say why it was chosen
  in the commit message.

## Testing

- xUnit only. Never add NUnit, MSTest, or another test framework.
- Tests are written before the implementation (red-green-refactor): one failing test, confirm
  it fails for the right reason, then only enough code to pass it.
- Unit tests live in a sibling test project per project under test (e.g. `Domain.Tests/`,
  `DiscordRepository.Tests/`), referencing only the project it tests.
- Integration tests live in the single whole-solution `IntegrationTests` project and are tagged
  `Category=Integration` so they can be filtered separately from unit tests.

## Architecture

Hexagonal architecture (ports and adapters), one .NET project per side of the hexagon.
"Repository" in this project means an external system (Discord, Stoat), not an EF Core /
persistence repository.

- Project references only flow one way: `DiscordToStoat.csproj` references `Domain` and every
  adapter project; each adapter project references only `Domain`. Adapter projects never
  reference each other, and `Domain` references nothing in this solution.
- `Domain` stays pure: no network, filesystem, environment variables, or third-party NuGet
  packages, and no `ProjectReference` to `DiscordToStoat.csproj` or any adapter project. Ports
  are C# interfaces; adapters are received via constructor injection, never `new`'d up.
- Adapter projects (`DiscordRepository`, `StoatRepository`, ...) reference `Domain`, the BCL, and
  that system's own SDK/NuGet package only. Never add a `ProjectReference` to another adapter
  project or to `DiscordToStoat.csproj`. Config (tokens, URLs) comes in via constructor
  arguments bound from `IConfiguration` in `DiscordToStoat.csproj` — never read directly inside
  an adapter class.
- Create a new project only when its first real module is written; add it to the solution file
  when it's created.
- Secrets (Discord/Stoat tokens) come from environment variables or `dotnet user-secrets` —
  never from a committed `appsettings.json`.
