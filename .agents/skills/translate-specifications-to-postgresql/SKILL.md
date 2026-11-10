---
name: translate-specifications-to-postgresql
description: >-
  Use TedToolkit.Specifications.Sql.PostgreSql to translate a typed predicate into a PostgreSQL condition and bind its parameters.
---

# Translate Specifications to PostgreSQL

- Use `PostgreSqlTranslator` with `ToSql(specification, out var parameters)`. Bind the parameter collection on the PostgreSQL command.
- Verify case-sensitive text behavior, nullable values, and parameter limits when the rule depends on them.
- Compare rows returned by PostgreSQL with the same specification evaluated in memory for representative boundary data.
