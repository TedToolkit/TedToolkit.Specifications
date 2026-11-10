---
name: translate-specifications-to-sqlite
description: >-
  Use TedToolkit.Specifications.Sql.Sqlite to translate a typed predicate into a SQLite condition and bind its parameters.
---

# Translate Specifications to SQLite

- Use `SqliteTranslator` with `ToSql(specification, out var parameters)`. Bind the parameter collection on the SQLite command.
- Verify text comparison, nullable values, and date or instant comparisons on SQLite when the rule depends on them.
- Compare the rows returned by SQLite with the same specification evaluated in memory for boundary data.
