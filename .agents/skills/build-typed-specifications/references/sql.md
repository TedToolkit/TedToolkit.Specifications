# Parameterized SQL translation

## Bind Specification SQL Parameters

- Choose a concrete translator for the target dialect. Call `ToSql(specification, out var parameters)` and bind the returned parameters through the database command.
- Use an explicit `SqlColumnMap<T>` when storage column names differ from the default member paths. Keep selection translation separate from predicate translation.
- Verify that generated SQL and bound values preserve the in-memory predicate’s semantics for nullable members. Handle unsupported translation explicitly.

## SQLite

- Use `SqliteTranslator` with `ToSql(specification, out var parameters)`. Bind the parameter collection on the SQLite command.
- Verify text comparison, nullable values, and date or instant comparisons on SQLite when the rule depends on them.
- Compare the rows returned by SQLite with the same specification evaluated in memory for boundary data.

```csharp
using TedToolkit.Specifications;
using TedToolkit.Specifications.Sql.Sqlite;

var translator = new SqliteTranslator();
var order = Selection.For<Order>();
var column = translator.ToSql(order.Amount);
var condition = translator.ToSql(order.Amount > 0, out var parameters);
var sql = $"SELECT {column} FROM orders WHERE {condition}";

public sealed record Order(decimal Amount);
```

## PostgreSQL

- Use `PostgreSqlTranslator` with `ToSql(specification, out var parameters)`. Bind the parameter collection on the PostgreSQL command.
- Verify case-sensitive text behavior, nullable values, and parameter limits when the rule depends on them.
- Compare rows returned by PostgreSQL with the same specification evaluated in memory for representative boundary data.

```csharp
using TedToolkit.Specifications;
using TedToolkit.Specifications.Sql.PostgreSql;

var translator = new PostgreSqlTranslator();
var order = Selection.For<Order>();
var column = translator.ToSql(order.Amount);
var condition = translator.ToSql(order.Amount > 0, out var parameters);
var sql = $"SELECT {column} FROM orders WHERE {condition}";

public sealed record Order(decimal Amount);
```

Use a column map when database names differ from selected members. Bind the values with the provider command. Test nullable parents and dialect-specific comparisons rather than rewriting the SQL string.
