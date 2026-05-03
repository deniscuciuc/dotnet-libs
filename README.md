# DenisCuciuc.LibName

> One sentence - what it does and why it exists.

Derived from `lib-template` for the shared repository files and extended here with
.NET-specific build, test, packaging, and usage setup.

## Installation

Choose the distribution model that fits the repo.

### NuGet (recommended for stable versions)

```bash
dotnet add package DenisCuciuc.LibName
```

### Git submodule (recommended during active development)

```bash
git submodule add https://github.com/deniscuciuc/dotnet-libname libs/dotnet-libname
```

Then in your `.csproj` use a conditional reference so you can switch between
local development and NuGet without changing the project file:

```xml
<Project>
	<PropertyGroup>
		<UseLocalLibs>false</UseLocalLibs>
	</PropertyGroup>
</Project>
```

```xml
<ItemGroup Condition="'$(UseLocalLibs)' == 'true'">
	<ProjectReference Include="libs/dotnet-libname/src/DenisCuciuc.LibName/DenisCuciuc.LibName.csproj" />
</ItemGroup>

<ItemGroup Condition="'$(UseLocalLibs)' != 'true'">
	<PackageReference Include="DenisCuciuc.LibName" Version="0.1.0" />
</ItemGroup>
```

In downstream apps, keep `UseLocalLibs` unset in CI so package restore uses NuGet.
For local development, put `UseLocalLibs=true` in an untracked `Directory.Build.local.props`.
The template's own example project already follows this pattern and defaults to a
local project reference when the source tree exists.

## Quick start

```csharp
using DenisCuciuc.LibName;

var message = LibraryMessage.Create("platform team");
Console.WriteLine(message);
```

## Why this exists

Explain the problem this library solves, why the built-in alternatives were not enough, and when someone should pick it over a bespoke implementation.

## Features

- Multi-targets .NET 8 and .NET 9.
- Ships with nullable reference types, XML docs, and warnings-as-errors enabled.
- Works both as a NuGet package and as source checked in via git submodule.

## Template structure

Shared from `lib-template`:

- `LICENSE`
- `CONTRIBUTING.md`
- `CHANGELOG.md`
- `CLAs/signed.md`
- `.github/pull_request_template.md`
- `.github/ISSUE_TEMPLATE/*`
- `.github/workflows/secret-scan.yml`

Specific to `dotnet-lib-template`:

- `src/`
- `tests/`
- `examples/`
- `.editorconfig`
- `Directory.Build.props`
- `global.json`
- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`

## Documentation

Add package documentation in `/docs` or the repository wiki as the API stabilizes.

## Release model

CI always builds, tests, and format-checks the repository. Publishing is manual.

- Use the `Release` GitHub Actions workflow when you actually want to pack or publish.
- The workflow always produces `.nupkg` artifacts.
- NuGet publishing is opt-in through a workflow input instead of automatic on tag push.
- GitHub Release creation is also opt-in.

## License

[PolyForm Strict](LICENSE) - source visible, commercial use requires permission.
Contact: <denis@deniscuciuc.dev>