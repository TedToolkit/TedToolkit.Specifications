# TedToolkit.Specifications.Sql

Share SQL translation, immutable column maps, and parameter bindings across database adapters. Applications normally reference [SQLite](https://github.com/TedToolkit/TedToolkit.Specifications/blob/development/src/TedToolkit.Specifications.Sql.Sqlite/README.md) or [PostgreSQL](https://github.com/TedToolkit/TedToolkit.Specifications/blob/development/src/TedToolkit.Specifications.Sql.PostgreSql/README.md), which inherit `SqlTranslator` and include this package transitively.

## Translate SELECT and WHERE expressions

```csharp
using TedToolkit.Specifications;
using TedToolkit.Specifications.Sql;
using TedToolkit.Specifications.Sql.PostgreSql;

var order = Selection.For<Order>();
var eligible = order.Amount > 100m && order.Customer.Name.StartsWith("A");
SqlTranslator translator = new PostgreSqlTranslator();

string column = translator.ToSql(order.Amount);
string condition = translator.ToSql(eligible, out var parameters);
string sql = $"SELECT {column} FROM orders WHERE {condition}";

public sealed record Order(decimal Amount, Customer? Customer);
public sealed record Customer(string? Name);
```

Both overloads return a string. A selection produces a quoted column expression without `SELECT` or `FROM`. A specification produces a condition without `WHERE`; its `out IReadOnlyDictionary<string, object?>` contains immutable parameter bindings. Parameter names omit the `@` prefix. Values are provider-ready objects; bind null as `DBNull.Value` with ADO.NET.

The application owns table names, joins, command execution, and transactions. Bind every returned parameter to the command that uses its condition. For example, given an ADO.NET `command`:

```csharp
command.CommandText = sql;
foreach (var binding in parameters)
{
    var parameter = command.CreateParameter();
    parameter.ParameterName = binding.Key;
    parameter.Value = binding.Value ?? DBNull.Value;
    command.Parameters.Add(parameter);
}
```

For Dapper, copy the bindings into `DynamicParameters`. This package has no driver or Dapper dependency.

## Optional column maps

No map is needed when database column names match member paths. `Amount` becomes `"Amount"`; `Customer.Name` becomes **`"Customer.Name"`**, a single identifier containing a dot. It does not imply a join or JSON traversal.

```csharp
var columns = new SqlColumnMap<Order>()
    .Map(order.Amount, "amount", "o")
    .Map(order.Customer.Name, "customer_name", "o");

string mappedColumn = translator.ToSql(order.Amount, columns); // "o"."amount"
string mappedCondition = translator.ToSql(eligible, out var bindings, columns);
```

Maps are immutable and may contain only the overrides you need. Unmapped properties retain default names. Names and aliases are separately quoted, and embedded quotes are escaped. Duplicate mappings, blank identifiers, and zero characters are rejected. Identifiers are not raw SQL expressions.

Scalar roots have no member name and require a map:

```csharp
var number = Selection.For<int>();
var scalarColumns = new SqlColumnMap<int>().Map(number, "amount");
string predicate = translator.ToSql(number > 1, out var values, scalarColumns);
```

Nested paths can map to flattened columns or equivalent columns of an application-supplied join. Missing parents must yield SQL NULL child columns. A complex selection used with `IsNull()` needs a column whose nullness represents that object's presence. Translation does not validate schemas or invoke entity getters.

## Parameters and supported values

`ToSql(specification, out parameters, columns, parameterPrefix, maxParameters)` defaults to prefix `spec` and a budget of 1000 parameters. Choose distinct prefixes when combining separately translated conditions in one command. Prefixes must be ASCII identifiers. Budgets must be positive and cannot exceed the adapter limit: 32766 for SQLite or 65535 for PostgreSQL.

Supported SQL scalars include integer types, enums, Boolean, finite floating-point values, decimal, string, Guid, DateOnly, TimeOnly, DateTime, and DateTimeOffset, including nullable forms. Storage encodings and precision follow the [semantic contract](https://github.com/TedToolkit/TedToolkit.Specifications/blob/development/docs/semantics.md). Unsupported CLR types, non-finite numbers, NUL, and invalid Unicode fail explicitly when used as SQL predicate operands. Core in-memory specifications remain independent of these storage restrictions.

Every predicate leaf yields a non-null Boolean, preserving in-memory null behavior under NOT and composition. String comparison and matching use explicit ordinal collation. Matching uses substring functions, so `%`, `_`, and backslashes stay literal. Translation never interpolates operand values into SQL.

`SqlTranslator` exposes protected hooks for database parameter limits, text collation, text matching, and instant normalization; its selection overload is virtual. Built-in adapters are stateless and can be reused. There is no public dialect enum or query-result wrapper.

The common SQL assembly exports only `SqlTranslator` and `SqlColumnMap<TCandidate>`. Each database assembly exports its translator class. SQL writers, type classification, value validation, and column resolution stay internal. Protected translation hooks remain available because adapters inherit across assembly boundaries; public translation uses the two `ToSql` overloads.

Runtime targets are .NET 8, 9, and 10; generated member properties require C# 14. See [repository setup and local packaging](https://github.com/TedToolkit/TedToolkit.Specifications/blob/development/README.md), the [core guide](https://github.com/TedToolkit/TedToolkit.Specifications/blob/development/src/TedToolkit.Specifications/README.md), and [diagnostic catalog](https://github.com/TedToolkit/TedToolkit.Specifications/blob/development/docs/diagnostics.md).

