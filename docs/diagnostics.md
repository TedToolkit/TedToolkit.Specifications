# Specification diagnostics

The core package bundles its generator and Analyzer under `analyzers/dotnet/cs`. SQL-specific checks activate when `TedToolkit.Specifications.Sql` is referenced. Normal compiler checks also enforce candidate types and the separation between selections and specifications.

| ID | Default | Trigger | Resolution |
| --- | --- | --- | --- |
| TSPEC001 | Error | Inaccessible, open generic, static, ref-like, or file-local selected model | Use an accessible closed model type |
| TSPEC002 | Error, SQL consumers | Comparing or using membership on an unsupported SQL value type | Select its persisted scalar representation |
| TSPEC003 | Error | Reading an unavailable generated property | Use an ordinary readable property with an accessible getter |
| TSPEC004 | Error | Language version before C# 14 | Use C# 14 and a compatible .NET 10 SDK |
| TSPEC005 | Error | Handwritten member-accessor construction | Use `Selection.For<T>()` and generated properties |
| TSPEC006 | Error | Calling predicate evaluation or composition on a selection | Build a comparison to obtain `Specification<TCandidate>` |
| TSPEC007 | Warning | Direct null check on a non-nullable scalar root | Remove it or use a nullable scalar |
| TSPEC008 | Error | Calling object equality, formatting, identity, or type inspection on DSL objects | Use typed comparisons and logical composition |
| TSPEC009 | Error | Hidden inherited property with an ambiguous name | Rename or override the member |
| TSPEC010 | Error, SQL consumers | Constant text contains NUL or invalid Unicode | Use representable text for SQL operands |
| TSPEC011 | Error | Application code derives from closed framework classes | Return compositions of existing selections and specifications |
| TSPEC012 | Warning, SQL consumers | Selecting a computed getter | Map it only to an equivalent persisted value |
| TSPEC013 | Error | Type-inappropriate text or ordering operation | Use text matching on strings and supported ordering on comparable scalars |
| TSPEC014 | Error, SQL consumers | Constant NaN or infinite SQL operand | Use finite SQL values |
| TSPEC015 | Error | A null text pattern | Use `IsNull` or provide a non-null pattern |
| TSPEC016 | Error | Reference equality, null/non-null or type patterns on DSL objects, including compound patterns, switches, and forced object conversions | Use value comparisons, `IsNull`, or `IsNotNull` |
| TSPEC017 | Error | A specification used directly as a control-flow condition | Call `IsSatisfiedBy(candidate)` to obtain a Boolean |
| TSPEC018 | Error | A model property name conflicts with a public Selection or Object instance member | Rename the property or use an equivalent differently named query-model property |

Null comparisons on `Selection` use ordinary class operators and build actual specifications. A Boolean selection is still a selection: `selection.Enabled == true` produces its predicate. Compile-time tests cover nested chains, aliases, incomplete dot access, recursive models, closed generic collections, metadata models, and invalid API use.

Generation is demand-driven. A selected type's immediate public properties are emitted; another member-access level requests the next type. Unsupported comparison types remain usable as intermediate selections. Core-only ordering accepts comparable CLR types such as `TimeSpan`, `BigInteger`, and `Int128`; SQL consumers additionally require a supported SQL scalar type. String, Boolean, GUID, and enum ordering remains prohibited by the core contract. TSPEC013 also diagnoses arrays and sealed CLR types that implement neither `IComparable` nor a compatible `IComparable<T>`. Open runtime types and generic parameters are checked at evaluation. Runtime SQL checks handle operands and types unknown to static analysis.

TSPEC010 and TSPEC014 inspect the actual constant operand, ordinary transparent casts, nullable conversions, and directly supplied array or collection elements. They do not infer the result of an arbitrary method call, conditional expression, or user-defined conversion from its internal constants. For example, `selection.EqualTo("\0".Replace("\0", "", StringComparison.Ordinal))` is allowed; translation validates the resulting empty string. Directly supplying `"\0"` remains an error for SQL consumers.

TSPEC008 follows ordinary casts to `object` and detects reversed object equality such as `1.Equals(selection)`. TSPEC016 also covers `selection is {}`, `selection is not null and {}`, null arms in switch statements or expressions, and property patterns such as `selection is { Score: not null }` where the property is itself a generated selection. These inspect the DSL instance without evaluating a candidate. Ordinary application helpers and patterns on candidate values or evaluated Boolean results remain valid.

Equivalent type tests such as `selection is object` and `selection is object value` also receive TSPEC016. Binding-only `is var value` and property captures such as `root is { Child: var child }` remain valid for constructing subsequent selections. Nested and extended property patterns request their used member levels before the Analyzer checks them.

TSPEC018 distinguishes a readable model property from an existing DSL method with the same name. The [core guide](../src/TedToolkit.Specifications/README.md) lists the reserved names and the query-model/column-map remedy. Ordinary DSL method calls and method-group delegates remain valid. Overloaded indexers are ignored before hidden-property checks; static members and fields cannot redirect an inherited property's generated getter.

## Diagnostic languages

All titles and messages come directly from the strings generated by `TedToolkit.Localizations`, using [Localization.json](../src/TedToolkit.Specifications.Analyzers/Localizations/Localization.json) for English and [Localization.zh-CN.json](../src/TedToolkit.Specifications.Analyzers/Localizations/Localization.zh-CN.json) for simplified Chinese. Specification adds no localization adapter or language-selection rules.

Language selection uses `LocalizationSettings.Culture`, whose default is the runtime's current culture. Localizations selects the matching resource and falls back to the base text. Diagnostic descriptors capture those generated strings when initialized. Diagnostic IDs, severities, and format arguments are identical in both languages. For example, TSPEC017 reads:

- English: `A specification cannot be used as a Boolean condition; call IsSatisfiedBy(candidate)`
- 简体中文：`规格不能直接用作布尔条件；请调用 IsSatisfiedBy(candidate)`

Localization is bundled with the Analyzer. Applications do not need to configure or reference `TedToolkit.Localizations` separately.
