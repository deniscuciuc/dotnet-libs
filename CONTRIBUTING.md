# Contributing

## Contributor License Agreement

By submitting a pull request to this repository you agree that:

1. You own the contribution and it does not violate any third-party IP rights.
2. You grant Denis Cuciuc a perpetual, worldwide, royalty-free license to use,
   modify, sublicense, and distribute your contribution under any license.
3. Your contribution may be used in commercial products.

To formally sign the CLA, add your name to CLAs/signed.md in your PR:

Your Name (@github-username) - YYYY-MM-DD

## Branch naming

feat/short-description
fix/short-description
docs/short-description
refactor/short-description
test/short-description
chore/short-description

Examples:
feat/add-bulk-insert
fix/transaction-rollback
docs/update-quickstart

## Commit convention

Follow [Conventional Commits](https://www.conventionalcommits.org):
feat: add bulk insert support
fix: correct transaction rollback on timeout
docs: update quickstart example
test: add coverage for edge cases
refactor: extract pipeline builder
chore: bump dependencies

Breaking changes:
feat!: rename IRepository to IDataRepository
BREAKING CHANGE: IRepository has been renamed to IDataRepository.
Update all references accordingly.

## Pull requests

- Branch from `main`, target `main`
- One concern per PR - do not mix features with refactoring
- PR title must follow commit convention: `feat: add bulk insert support`
- All CI checks must pass before merge
- Add or update tests for every change
- Coverage must not decrease - PRs that drop coverage will be rejected
- Update `CHANGELOG.md` under the `[Unreleased]` section
- Sign the CLA in `CLAs/signed.md` if this is your first contribution

## Tests

- All changes must include tests
- Coverage must not decrease
- See language-specific setup in `README.md`

## Code style

- Follow the repository formatter and linter configuration
- Keep public API documentation at the standard expected by the language ecosystem
- No warnings or failing checks in CI