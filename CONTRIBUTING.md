# Contributing to Hive

Thanks for your interest in Hive. This guide covers setup, the rules the
codebase follows, and how to get a change merged.

By participating you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).
To report a security problem, follow [SECURITY.md](SECURITY.md) instead of
opening a public issue.

## Before you start

- **Bugs and small fixes:** open a pull request directly, or an issue first if
  you are unsure it is a bug.
- **Features and larger changes:** open an issue to discuss the approach first.
  Hive is opinionated about what an Engineering Manager needs; a short
  conversation saves you work.

## Development setup

You need the [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0),
[Node.js 26](https://nodejs.org/) and `make`.

```bash
make install    # NuGet packages and frontend dependencies
make dev        # backend on :5002 (in-memory database) and the UI on :5173
make test       # backend and frontend tests
```

No test touches a real database, Jira, Google or an AI model.

## Rules the codebase follows

Hive uses Clean Architecture. Read [ARCHITECTURE.md](ARCHITECTURE.md) before
structural changes; it has step-by-step recipes for a new entity, page and
dashboard widget. The rules reviewers check most often:

- **The dependency rule is absolute.** `Core` depends on nothing;
  `Application` depends only on `Core`.
- **Validation lives on the entity.** Entities have private setters and
  intent-revealing methods; services, controllers and the frontend never
  re-validate.
- **Controllers map HTTP to services and return DTOs, never entities.**
- **Every repository is implemented twice** (in-memory and SQLite) and
  registered twice.
- **The frontend renders API responses.** All API calls go through
  `services/api.ts`.
- **Dates show as `dd/MM/yyyy`.**

Use the domain's ubiquitous language from [BUSINESS.md](BUSINESS.md). The full
pre-commit checklist is in [CLAUDE.md](CLAUDE.md).

## Making a change

1. Fork the repository and create a branch from `main`
   (`feat/short-description`, `fix/short-description`, `docs/topic`).
2. Make the change with tests: entity tests construct entities directly,
   service tests mock repositories with Moq, frontend tests use Vitest and MSW.
3. Run the checks:
   ```bash
   make test
   dotnet format Hive.sln --verify-no-changes
   cd src/Hive.Desktop && npm run typecheck && npm run lint
   ```
4. Update [CODEBASE.md](CODEBASE.md) if you added or removed a file, and add a
   line under `Unreleased` in [CHANGELOG.md](CHANGELOG.md) for user-visible changes.
5. Commit using [Conventional Commits](https://www.conventionalcommits.org/)
   and open a pull request against `main`.

Pull requests are squash-merged to keep `main` linear.

## Licence

By contributing, you agree that your contributions are licensed under the
[Apache License 2.0](LICENSE).
