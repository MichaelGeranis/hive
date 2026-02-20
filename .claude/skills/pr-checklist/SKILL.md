---
name: pr-checklist
description: Run a pre-PR checklist for Hive — check DI registrations, DTO completeness, test coverage, and CLAUDE.md accuracy
disable-model-invocation: false
---

# PR Checklist Skill

Review the current git diff and staged changes, then verify:

1. **New entities**: If a new entity was added, confirm it has: interface in Core, repository in Infrastructure, service in Application, controller in Api, and DI registration in both `DependencyInjection.cs` files
2. **DTOs**: Confirm no entities are directly exposed via API responses (only DTOs)
3. **Tests**: Check that new services and controllers have corresponding test files
4. **Build**: Run `make build` and confirm it succeeds
5. **Tests**: Run `make test` and confirm all pass
6. **CLAUDE.md**: If a new page, entity, or controller was added, suggest updates to CLAUDE.md

Output a checklist with ✅/❌ for each item.
