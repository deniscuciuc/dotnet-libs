# Release process

There are no packages to publish. A "release" here is a git tag on `main` that consumers
point their submodule at.

## Cutting a release

1. Make sure `main` is green: format, build and test on Linux and Windows, plus the
   integration suites.
2. Move the `## [Unreleased]` entries in [`CHANGELOG.md`](../CHANGELOG.md) into a new
   version section with today's date, and update the link definitions at the bottom.
3. Commit, then tag and push:

   ```bash
   git tag -a v1.1.0 -m "v1.1.0"
   git push origin main --follow-tags
   ```

4. Create the GitHub release from the tag, using the changelog section as the notes:

   ```bash
   gh release create v1.1.0 --title v1.1.0 --notes-file <(
     awk '/^## \[1\.1\.0\]/{f=1;next} f&&/^## \[/{exit} f' CHANGELOG.md)
   ```

## Versioning

[Semantic versioning](https://semver.org/), applied to the repository as a whole — the three
modules are tagged together because they reference each other by project path.

Because consumers track this repository by submodule, a breaking change is felt the moment
someone runs `git submodule update --remote`. Anything that renames a public type, changes a
configuration section name, or changes a registration method's behaviour is a major bump, and
the changelog entry has to say what a consumer must do.

## Updating a consumer

```bash
cd libs/dotnet-libs
git fetch --tags
git checkout v1.1.0
cd -
git add libs/dotnet-libs
git commit -m "chore: bump dotnet-libs to v1.1.0"
```
