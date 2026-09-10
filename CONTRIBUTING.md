# Contributing to CoreLibs

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — the exact version is
  pinned in [`global.json`](global.json)
- Docker or Podman, for the integration tests
- An editor that honours `.editorconfig`

## Setup

```bash
git clone https://github.com/deniscuciuc/dotnet-libs.git
cd dotnet-libs
dotnet restore CoreLibs.slnx
```

## Building and testing

```bash
dotnet build CoreLibs.slnx -c Release
dotnet test CoreLibs.slnx -c Release
```

The integration suites (`*.Integration.Tests`, `*.IntegrationTests`) start MongoDB, Postgres
and Redis via Testcontainers. With Podman, point Testcontainers at the socket first:

```bash
export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock
```

Run only the unit suites while iterating:

```bash
for p in $(find . -name '*.csproj' | grep -i test | grep -vi integration); do
  dotnet test "$p" -c Release
done
```

## Code style

Enforced by the build and by `dotnet format --verify-no-changes` in CI:

- `net10.0`, `LangVersion latest`, nullable reference types and implicit usings on
- `TreatWarningsAsErrors` — no warnings in `src/`
- File-scoped namespaces, one namespace per project, matching the project name
- XML doc comments on public types and members
- `ArgumentNullException.ThrowIfNull` for guards; no `#region` blocks
- Prefer primary constructors for DI, `readonly` fields, and read-only collection types in
  public signatures

## Repository layout

Each module owns its `src/`, `tests/`, `examples/`, `docs/` and its own
`Directory.Packages.props`. The root `Directory.Packages.props` holds only repo-wide
transitive security pins and is imported by each module. Add a package version to the module
that uses it, not to the root.

Project name, folder name, assembly name and root namespace are all the same string, and
none of them is declared explicitly — renaming the folder and the `.csproj` is the whole
rename. Keep it that way.

## Adding a project

1. `dotnet new classlib -o platform/src/CoreLibs.YourThing -n CoreLibs.YourThing`
2. Strip the generated `.csproj` down to the SDK element plus its references — everything
   else is inherited from `Directory.Build.props`.
3. Add it to `CoreLibs.slnx`.
4. If it registers services into the startup orchestrator, add a matching
   `CoreLibs.YourThing.Startup` project rather than putting startup logic in the library.
5. Add `platform/tests/CoreLibs.YourThing.Tests`.
6. Document it in `platform/docs/` and add a row to the table in `README.md`.

## Pull requests

- Keep a PR to a single concern.
- New behaviour and bug fixes need tests.
- Conventional commits: `feat(mongodb): …`, `fix(startup): …`, `docs(readme): …`.
- Note breaking changes in `CHANGELOG.md`. Because consumers track this repository by
  submodule, a breaking change is felt the moment someone moves their pointer — say what
  they have to do.
- CI must be green: format, build and test on Linux and Windows, plus the integration suites.

## Releasing

See [docs/release-process.md](docs/release-process.md). Releases are git tags; there is
nothing to publish.

## Third-party licensing

Before adding a dependency, check its license. This repository is MIT and intends to stay
easy to consume. `Hangfire` (LGPL-3.0 or commercial) and the deliberate `MediatR 12.1.1` pin
are documented in the README — do not let a dependency update cross the MediatR pin without
a decision.
