---
name: new-migration
description: Create a new EF Core database migration, verify the generated code, and list all migrations
disable-model-invocation: false
---

# New Migration Skill

The user wants to create a new EF Core migration.

1. Ask the user for the migration name (PascalCase describing the schema change, e.g. `AddSkillLevel`)
2. Run: `make migration-add` (it will prompt for the name)
3. Read the generated migration file in `src/Hive.Infrastructure/Migrations/`
4. Verify the `Up()` and `Down()` methods look correct
5. Run `make migration-list` and show the current migration state
6. Remind the user to run `make migration-update` when ready to apply
