# TedToolkit.Specifications

Build a business predicate once, evaluate it in memory, and translate it into parameterized SQLite or PostgreSQL SQL. C# 14 extension properties provide typed member access without entity attributes or reflection over entity properties.

## 🚀 First example

```csharp
using TedToolkit.Specifications;
using TedToolkit.Specifications.Sql.PostgreSql;

var order = Selection.For<Order>();
Selection<Order, decimal> amount = order.Amount;
Specification<Order> eligible = amount > 100m
    && (order.Customer.Name == "Ada" || order.Customer.Name.IsNull());

bool matches = eligible.IsSatisfiedBy(new Order(120m, new Customer("Ada"))); // true

var translator = new PostgreSqlTranslator();
string column = translator.ToSql(amount); // "Amount"
string condition = translator.ToSql(eligible, out var parameters);
string sql = $"SELECT {column} FROM orders WHERE {condition}";
// Bind every entry in parameters when executing sql.

public sealed record Order(decimal Amount, Customer? Customer);
public sealed record Customer(string? Name);
```

`Selection<TCandidate, TValue>` selects a value. Comparing it produces `Specification<TCandidate>`, the Boolean predicate with `IsSatisfiedBy`, `And`, `Or`, and `Not`. For example, `Selection.For<int>() > 1` is a `Specification<int>`. A Boolean selection also needs a comparison such as `order.Enabled.EqualTo(true)`.

Generated member properties expose the abstract selection API. The public `MemberSelection` constructor is a bridge for generated code in another assembly; its `Parent` and `Name` metadata are internal. See the [public API boundary](src/TedToolkit.Specifications/README.md#public-api-boundary).

The runtime libraries target .NET 8, 9, and 10 and support trimming and Native AOT. Generated extension properties require **C# 14 and a compatible .NET 10 SDK**, including when targeting .NET 8. Set `<LangVersion>14.0</LangVersion>` in the consuming project.

## Packages

| Package | Responsibility | Consumer guide |
| --- | --- | --- |
| `TedToolkit.Specifications` | Selections, predicates, generated properties, bundled Analyzer | [Core](src/TedToolkit.Specifications/README.md) |
| `TedToolkit.Specifications.Sql` | Shared translator base, optional column maps, parameter binding contract | [Shared SQL](src/TedToolkit.Specifications.Sql/README.md) |
| `TedToolkit.Specifications.Sql.Sqlite` | `SqliteTranslator` | [SQLite](src/TedToolkit.Specifications.Sql.Sqlite/README.md) |
| `TedToolkit.Specifications.Sql.PostgreSql` | `PostgreSqlTranslator` | [PostgreSQL](src/TedToolkit.Specifications.Sql.PostgreSql/README.md) |

Choose core for in-memory specifications, or one database package for SQL. Database packages supply shared SQL, core, the generator, and Analyzer transitively. No database driver is required by the library; execution belongs to the application.

These packages are currently built locally; publication to NuGet is not part of repository verification. To build the source:

```powershell
git submodule update --init --recursive
dotnet build TedToolkit.Specifications.slnx -c Release
```

To collect the four local NuGet packages after building, run:

```powershell
dotnet pack TedToolkit.Specifications.slnx -c Release --no-build -o .artifacts/packages
```

Add the chosen package to your application using the version in `Directory.Build.props` and `.artifacts/packages` as a NuGet source. CI compiles and runs this README's first C# example against those packages in an isolated consumer project.

The database's official name is [PostgreSQL](https://www.postgresql.org/about/policies/project-name/); Postgres is an accepted alias. The package, namespace, and C# type consistently use `PostgreSql`.

## Matching in-memory and SQL behavior

- Null and missing parents stay absent. They never turn into `default(int)` or another invented value.
- Every SQL predicate returns a non-null Boolean, so `!`, `&&`, and `||` preserve in-memory truth values, including under `NOT`.
- String operations use ordinal, case-sensitive literal matching. `%` and `_` are ordinary data.
- `&&` and `||` construct both operands; candidate evaluation short-circuits. TSPEC017 rejects `if (specification)` because it has no candidate.
- Member paths are quoted as single column names by default: `Customer.Name` becomes `"Customer.Name"`. Optional immutable maps override names and table aliases. Scalar roots require an explicit column binding.

The [semantic contract](docs/semantics.md) explains storage assumptions, null behavior, supported scalar types, and database precision. Schema mappings and persisted computed values must match the values exposed by the model. SQL translation rejects unsupported values rather than silently switching to client evaluation.

## Analyzer languages

All 18 diagnostic titles and messages use `TedToolkit.Localizations` generated strings directly. Standard base and `zh-CN` JSON resources provide English and simplified Chinese; language selection and fallback follow Localizations. TSPEC018 explains model-property names that conflict with the existing DSL API. See the [diagnostic catalog](docs/diagnostics.md) for IDs, triggers, and fixes.

## Development and shared CI

The main branch is `development`. `externals/TedToolkit` is a pinned Git submodule providing shared CodeAnalysis, NuGet, and build infrastructure. Owned projects use the shared `.editorconfig`, StyleCop configuration, and .NET, Roslynator, StyleCop, and Sonar analyzers.

```powershell
git submodule update --init --recursive
dotnet build TedToolkit.Specifications.slnx -c Release
dotnet run --project tests/TedToolkit.Specifications.Tests -c Release --no-build
dotnet run --project tests/TedToolkit.Specifications.Analyzers.Tests -c Release --no-build
dotnet run --project tests/TedToolkit.Specifications.Sql.Tests -c Release --no-build
```

Core and SQL tests run on .NET 8, 9, and 10 in CI. The default test target is .NET 10; select an older runtime with `-p:TestFramework=net8.0` or `-p:TestFramework=net9.0` and allow that invocation to build:

```powershell
dotnet run --project tests/TedToolkit.Specifications.Tests -c Release -p:TestFramework=net8.0
dotnet run --project tests/TedToolkit.Specifications.Sql.Tests -c Release -p:TestFramework=net9.0
```

Install the corresponding .NET runtime before running each target. Roslyn Analyzer tests use .NET 10, and Native AOT verification publishes the .NET 10 TUnit projects. The same generator regressions also execute through a statically compiled core test under Native AOT.

`Build/Program.cs` follows Localizations: Sourcy supplies typed solution and project paths, and `TedPipeline.ExecuteAsync()` runs the standard shared pipeline. `.github/workflows/build.yml` calls the TedToolkit reusable workflow, which runs this entry point and manages build and release outputs. `verify.yml` separately supplies PostgreSQL and runs the repository's package and Native AOT verification.

The shared workflow invokes `dotnet run --project Build/Build.csproj`. Local verification uses the direct dotnet commands above. Build orchestration is excluded from ordinary solution builds.

The core project directly packs the Analyzer and its dependencies under `analyzers/dotnet/cs`. Solution `BuildDependency` entries build the Analyzer before the core package and source tests. Build the solution before running individual source projects; NuGet consumers receive the bundled Analyzer automatically.

For real PostgreSQL checks, set `SPECIFICATION_POSTGRES` before running the SQL tests. Tests create connection-local temporary tables. Without that setting, PostgreSQL cases are explicitly skipped; CI requires the setting and supplies a PostgreSQL service.

To run the core and SQL TUnit tests as Native AOT applications on Windows, build the solution first, then publish and run each test project:

```powershell
$env:SPECIFICATION_POSTGRES = 'Host=localhost;Database=specification_test;Username=test;Password=test'
dotnet publish tests/TedToolkit.Specifications.Tests -c Release -r win-x64 --self-contained -p:NativeAotTests=true -o .artifacts/aot/core
& ./.artifacts/aot/core/TedToolkit.Specifications.Tests.exe
dotnet publish tests/TedToolkit.Specifications.Sql.Tests -c Release -r win-x64 --self-contained -p:NativeAotTests=true -o .artifacts/aot/sql
& ./.artifacts/aot/sql/TedToolkit.Specifications.Sql.Tests.exe
```

The [verification workflow](.github/workflows/verify.yml) runs the managed tests, checks isolated NuGet consumption and packaged Analyzer errors, and publishes and executes the core and SQL TUnit projects as Native AOT applications. The Roslyn Analyzer tests run on the managed runtime. On Linux, use `linux-x64`, omit the executable's `.exe` suffix, and install the platform's Native AOT toolchain.

The two public API metadata tests run only in the managed pass because they inspect complete, untrimmed assembly exports. All core and SQL behavior tests also run under Native AOT.

## License

LGPL-3.0-only. See [COPYING.LESSER](COPYING.LESSER) and [COPYING](COPYING).

