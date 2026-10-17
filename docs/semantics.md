# Selection and specification semantics

`Selection.For<T>()` returns `Selection<T, T>`. Generated member properties retain the root candidate type and change the selected value type. Comparisons produce `Specification<TCandidate>`, which alone exposes candidate evaluation and logical composition. There is no implicit conversion between a selection and a predicate, including for Boolean selections.

Member selections contain their parent selection, name, and a generated typed accessor. Actual comparison and composite specification objects evaluate candidates themselves. SQL reads these same objects and their member paths, without another rule representation, reflection over entity members, or expression compilation.

## Missing values and Boolean logic

| Operation | Result |
| --- | --- |
| Null or absent parent | Subsequent member is absent |
| Equality to null / `IsNull()` | True for null or absent members |
| Equality to a non-null value | False for null or absent members |
| Inequality | Exact complement of equality |
| Ordered comparison with null or an absent member | False |
| Empty membership set | False |
| Membership containing null | Matches null and absent members |
| Membership of a non-nullable value | An absent member never becomes its default value |
| String operation on null or an absent member | False, including an empty pattern |
| Empty string pattern | True for every present, non-null string |
| And, Or, Not | Ordinary two-valued Boolean logic |

A null root entity is rejected. Nullable scalars and string roots may themselves be null. In-memory And/Or short-circuit. SQL does not promise evaluation order; getters must expose stable values and applications must not rely on side effects.

`&&` and `||` construct complete And/Or specifications, as do `&` and `|`. Construction evaluates both operand expressions; short-circuiting applies when evaluating a candidate. The paired C# `true` and `false` operators both return false to prevent construction from omitting either operand. A specification used directly in a control-flow condition has no candidate and is rejected by TSPEC017.

## Core and SQL boundaries

Core uses .NET equality, membership, and supported ordering operations. It permits ordinary .NET values, including strings not representable as PostgreSQL text and non-finite floating-point equality. SQL storage constraints do not restrict construction or in-memory evaluation.

The SQL adapter classifies CLR types and converts parameter values. It rejects unsupported scalar encodings, NUL or invalid UTF-16 text, and non-finite floating-point operands. SQL-related Analyzer checks apply when the SQL assembly is referenced. Arbitrary value objects can be compared in memory; SQL requires a supported persisted scalar instead. Unsupported translations fail rather than falling back to client filtering.

Text comparisons are ordinal and case-sensitive. `%`, `_`, quotes, and backslashes are literal data. Unicode normalization is not performed. SQL uses guarded comparisons and explicit null checks so UNKNOWN cannot change the meaning of a surrounding NOT.

## Storage mappings

Member selections default to complete property paths quoted as single column names. For example, `Customer.Name` resolves to `"Customer.Name"`. Optional immutable maps override individual names or table aliases; omitted members keep their default names. Scalar roots require an explicit column binding because their path is empty. Nested paths may map to flattened columns or equivalent columns of supplied joins. A missing parent must yield NULL child columns. A complex selection used with `IsNull()` requires a column whose nullness matches that selection's presence. Maps do not infer joins or validate schemas.

| CLR value | SQLite storage | PostgreSQL storage |
| --- | --- | --- |
| Integers, enums | INTEGER; NUMERIC for the full `ulong` range | integer / bigint; numeric(20) for `ulong` |
| Boolean | 0 or 1 | boolean |
| Finite `float`, `double` | REAL | real / double precision |
| `decimal` | NUMERIC affinity | numeric |
| `string` | TEXT | text or varchar; avoid fixed-width char padding |
| `Guid` | Canonical Microsoft.Data.Sqlite Guid text | uuid |
| `DateOnly`, `TimeOnly`, `DateTime` | Canonical provider text | date, time, timestamp without time zone |
| `DateTimeOffset` | SQLite-readable ISO text, compared through julianday | timestamp with time zone |

Numeric and temporal precision follows ordinary provider behavior; exact decimal or timestamp round-tripping is not promised. `DateTime` uses wall-clock ticks. `DateTimeOffset` represents an instant and SQL parameters are normalized to UTC. Tests seed SQLite with Microsoft.Data.Sqlite and PostgreSQL with Npgsql; other drivers must use equivalent encodings.

The SQLite and PostgreSQL packages provide subclasses of `SqlTranslator`. Its selection overload returns a column expression for SELECT; its specification overload returns a condition for WHERE or HAVING and exposes parameter bindings through an out argument. Both return strings. Complete queries, repositories, joins, transactions, ordering, and pagination belong to the application. An Analyzer cannot prove a database schema or a computed column matches an application's getter. Diagnostics are a correctness aid; runtime translation checks remain necessary for generic and separately compiled code.
