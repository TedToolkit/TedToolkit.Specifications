# TedToolkit.Specifications.Sql.PostgreSql

Translate typed selections into SELECT expressions and specifications into parameterized WHERE conditions with `PostgreSqlTranslator`. The package includes the shared SQL layer, core library, generator, and Analyzer transitively. It does not execute queries or depend on a database driver.

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

Import `TedToolkit.Specifications` and `TedToolkit.Specifications.Sql.PostgreSql`. Optional `SqlColumnMap<T>` overrides property paths or table aliases. See the [shared SQL guide](https://github.com/TedToolkit/TedToolkit.Specifications/blob/development/src/TedToolkit.Specifications.Sql/README.md) for null semantics, storage assumptions, and parameter binding. Runtime targets are .NET 8, 9, and 10, including Native AOT.

Use C# 14 with a compatible .NET 10 SDK. See [repository setup](https://github.com/TedToolkit/TedToolkit.Specifications/blob/development/README.md) for source use and local NuGet packages. Text comparisons use C collation; literal matching uses `strpos`, `left`, and `right`. The parameter budget defaults to 1000 and cannot exceed 65535. `PostgreSql` is the C# identifier spelling of the official database name PostgreSQL.
