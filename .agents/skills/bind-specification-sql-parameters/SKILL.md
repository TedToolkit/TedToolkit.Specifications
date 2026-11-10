---
name: bind-specification-sql-parameters
description: >-
  Use TedToolkit.Specifications.Sql for shared translation and column mapping when a typed specification must become parameterized SQL.
---

# Bind Specification SQL Parameters

- Choose a concrete translator for the target dialect. Call `ToSql(specification, out var parameters)` and bind the returned parameters through the database command.
- Use an explicit `SqlColumnMap<T>` when storage column names differ from the default member paths. Keep selection translation separate from predicate translation.
- Verify that generated SQL and bound values preserve the in-memory predicate’s semantics for nullable members. Handle unsupported translation explicitly.
