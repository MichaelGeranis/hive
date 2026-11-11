## Summary

<!-- What does this change and why? Link the issue it closes, e.g. "Closes #12". -->

## Changes

-

## Test plan

- [ ] `make test` passes
- [ ] `dotnet format --verify-no-changes` passes
- [ ] `npm run typecheck` and `npm run lint` pass (if the desktop app changed)
- [ ] New behaviour is covered by tests

## Checklist

- [ ] Domain rules live on the entity; no business logic in controllers or the frontend
- [ ] Both repository implementations and both DI registrations, if a repository was added
- [ ] A migration, if an entity changed
- [ ] `CODEBASE.md` updated for added or removed files; `ARCHITECTURE.md` / `BUSINESS.md` if structure or rules changed
- [ ] `CHANGELOG.md` updated under `Unreleased` for user-visible changes
