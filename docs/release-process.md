# Release Process

## CI

The `CI` workflow runs on pushes and pull requests targeting `main`.

It performs:

1. `dotnet restore`
2. `dotnet build --configuration Release`
3. `dotnet test --configuration Release`
4. `dotnet format --verify-no-changes --no-restore`

## Manual release workflow

Publishing is intentionally manual through `.github/workflows/release.yml`.

The workflow accepts three inputs:

1. `version` - package version to build
2. `publish_nuget` - whether to push packages to NuGet.org
3. `create_github_release` - whether to create a GitHub release with the built packages attached

Every run always builds, tests, packs, and uploads the generated `.nupkg`
artifacts. Publishing and GitHub release creation are opt-in.

## Local release validation

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet pack \
 --configuration Release \
 -p:PackageVersion=1.0.0 \
 -o .\artifacts
```

## Publish from GitHub Actions

To publish from the workflow, configure:

1. `NUGET_API_KEY` as a repository secret when `publish_nuget` is enabled
2. `contents: write` permission for GitHub release creation, which is already declared in the workflow

## Recommended release flow

1. Update `CHANGELOG.md` so the publish snapshot is documented before tagging or packing.
2. Update the shared version metadata in `Directory.Build.props` if you want the repo default to move forward.
3. Merge the intended release state to `main`.
4. Run the `Release` workflow manually with the target version.
5. Enable `publish_nuget` only when you want to push packages.
6. Enable `create_github_release` when you want a tagged GitHub release and attached artifacts.
