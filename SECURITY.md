# Security Policy

## Reporting a Vulnerability

**Please do not open a public issue for a security vulnerability.**

Report it through GitHub's private vulnerability reporting, which is the preferred channel:

<https://github.com/deniscuciuc/dotnet-libs/security/advisories/new>

If you cannot use GitHub, email **denis@deniscuciuc.dev** instead.

Please include a description of the vulnerability and its impact, steps to reproduce or a
proof of concept, the affected module, and any suggested mitigation.

## What to Expect

| Stage | Target |
|---|---|
| Acknowledgement of your report | Within 48 hours |
| Initial assessment and severity triage | Within 5 working days |
| Fix released for a high or critical issue | Within 30 days of triage |
| Fix released for a moderate or low issue | Next scheduled release |

If you have not heard back within 48 hours, please follow up — an unanswered report usually
means it did not arrive.

## Supported Versions

This repository is consumed as a git submodule, so there is no package to patch: fixes land
on `main` and consumers move their submodule pointer. Only `main` receives security fixes.

## Scope

In scope: the three modules in this repository. `CoreLibs.Identity` is the highest-value
target — token validation, the internal shared-secret scheme and JWT audience handling — and
reports there are especially welcome. Credential handling in `CoreLibs.Configuration`,
`CoreLibs.MongoDB` and `CoreLibs.Postgres` is also in scope.

Out of scope: the projects under each module's `examples/`, which exist to illustrate usage
and are not consumed by anything; and vulnerabilities in third-party dependencies themselves,
though we would still like to know so the version can be pinned or worked around.

## Dependency Advisories

The build does not suppress NuGet vulnerability warnings. `NuGetAudit` is on with
`NuGetAuditMode=all` and `NuGetAuditLevel=low`, and `TreatWarningsAsErrors` makes any
published advisory — including transitive ones — fail the build. Advisories that cannot be
resolved by a direct upgrade are pinned in the root
[`Directory.Packages.props`](Directory.Packages.props) with the GHSA identifier in a comment.

Dependabot is enabled for NuGet and GitHub Actions, CodeQL runs on every push to `main` and
weekly, and gitleaks scans the full history on every push and pull request.
