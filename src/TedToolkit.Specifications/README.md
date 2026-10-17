# TedToolkit.Specifications

Construct typed business specifications without expression compilation, model attributes, hand-authored field metadata, or a separate rule tree. The NuGet package includes its generator and Analyzer.

Reference `TedToolkit.Specifications` for in-memory use, or a database adapter package to also obtain SQL translation. Packages are currently built locally; see [repository setup and packaging](https://github.com/TedToolkit/TedToolkit.Specifications/blob/development/README.md). Use a compatible .NET 10 SDK and `<LangVersion>14.0</LangVersion>` even when targeting .NET 8.

```csharp
using TedToolkit.Specifications;

var customer = Selection.For<Customer>();
Selection<Customer, decimal> balance = customer.Balance;
Specification<Customer> eligible = (balance >= 100m)
    && customer.Address.City.StartsWith("A")
    && (customer.Enabled == true);
bool matches = eligible.IsSatisfiedBy(new Customer(120m, new Address("Amsterdam"), true));

public sealed record Customer(decimal Balance, Address? Address, bool Enabled);
public sealed record Address(string? City);
```

`Selection<TCandidate, TValue>` retains both the root candidate type and the current selected type. A comparison returns `Specification<TCandidate>`. Only specifications can evaluate candidates or combine Boolean predicates. Even `Selection<bool, bool>` has no `IsSatisfiedBy`; compare it with `true` or `false` first. Scalar roots work as well: `Selection.For<int>() > 1` returns `Specification<int>`.

The generator discovers `Selection.For<T>()`, explicit selection declarations, and member-access syntax, including local aliases. Selecting `customer.Address` generates only the customer's immediate properties. Accessing `customer.Address.City` requests the next level. Recursive model graphs are not recursively expanded. All generated extensions live in `TedToolkit.Specifications`, so importing that namespace is sufficient across model namespaces.

Nested discovery also follows null-forgiving and conditional-access syntax, matching conditional/coalescing/switch branches, pattern-bound selections, and helpers whose argument and return types are the same selection type. These C# constructs operate on the selection objects when constructing a rule. For example, `choose ? order.Customer : order.OtherCustomer` chooses the member path immediately; `??` does not substitute another customer's value for a missing candidate member. Missing candidate parents are handled by the resulting selection's ordinary null propagation.

Public readable ordinary properties, inherited properties, overrides, interfaces, and closed generic types are supported, including types nested inside generic containers such as `Outer<int>.Item<string>`. Native integers and `dynamic` metadata are rendered as legal static getter signatures; dynamic values are exposed as `object` selections. Anonymous types, including generic arguments containing anonymous types, cannot be named in generated extensions and receive no generated properties. They remain usable through generic core operations. Overloaded indexers are ignored without affecting ordinary properties. Ref-returning properties, inaccessible getters, pointer properties, and ref-like values cannot become generated selections. Complex members may be traversed; their equality is usable in memory, while SQL translation requires a supported scalar representation. No entity must be partial or carry a marker attribute.

Public instance members of `Selection` and `Object` take precedence over generated extension properties. Model properties named `Compare`, `EqualTo`, `NotEqualTo`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `In`, `IsNull`, `IsNotNull`, `Contains`, `StartsWith`, `EndsWith`, `Equals`, `GetHashCode`, `ToString`, or `GetType` cannot be selected with dot access. TSPEC018 identifies this conflict. Rename the model property, or use a query model with an equivalent property under a different name and map that selection to the original SQL column. Existing DSL method calls remain available; an unused conflicting model property does not invalidate other properties. `Name` and `Parent` remain valid model property names.

Inherited getters bind to their declaring type. Ignored private members, static properties, and fields do not redirect a generated getter to a different member. A genuinely hidden public instance property is still rejected by TSPEC009; ordinary overrides are supported.

| Operation | API |
| --- | --- |
| Equality | `==`, `!=`, `EqualTo`, `NotEqualTo` |
| Numeric and temporal ordering | `>`, `>=`, `<`, `<=`, and named counterparts |
| Null and missing-parent checks | `IsNull`, `IsNotNull` |
| Frozen membership | `In(values)` |
| Ordinal literal text | `Contains`, `StartsWith`, `EndsWith` |
| Predicate composition | `&&`, `||`, `!`, `&`, `|`, `And`, `Or`, `Not`, `BitwiseAnd`, `BitwiseOr`, `LogicalNot` |
| Constants | `Specification.True<T>()`, `Specification.False<T>()` |
| Evaluation | `specification.IsSatisfiedBy(candidate)` |

`Specification.True<T>()` matches every valid candidate; `Specification.False<T>()` matches none. They are useful starting values when accumulating conditions: AND starts from `True`, and OR starts from `False`. Both still reject a null root entity.

A missing parent makes subsequent members absent. It does not substitute a value type's default: a missing `Customer.Score` does not equal zero. Equality to null and `IsNull` match absent values; ordered comparisons are false. Negation is the actual Boolean complement. A null root entity is rejected. Nullable scalar roots and null string roots can be tested explicitly.

Core does not enforce database storage restrictions. For example, valid .NET strings containing NUL, non-finite floating-point equality, and comparable values such as `TimeSpan` or `BigInteger` can be evaluated in memory. String, Boolean, GUID, and enum ordering is prohibited. The SQL adapter rejects unrepresentable operands when translating, and SQL-specific Analyzer checks activate when that adapter is referenced. The Analyzer checks constant operands and directly supplied membership elements; it does not assume that a constant used inside a computation or user-defined conversion is the final SQL value. Sanitized text and other computed operands are checked by the SQL adapter at runtime.

`Compare(value, ComparisonOperator.GREATER_THAN)` selects a comparison at runtime. Candidate evaluation uses `IsSatisfiedBy`; there is no public `IsTrue` member.

## Public API boundary

The core exports only `Selection`, `Selection<TCandidate, TValue>`, `Specification`, `Specification<TCandidate>`, `ComparisonOperator`, and `MemberSelection<TCandidate, TParent, TValue>`.

Generated properties return `Selection<TCandidate, TValue>`. `MemberSelection` and its constructor remain public so generated code can construct a selection from the consuming assembly. Its `Parent` and `Name` properties are internal; they are implementation metadata, not application navigation APIs. Application code uses the generated model properties, including model properties named `Parent` or `Name`, and TSPEC005 rejects handwritten member construction.

Comparison, membership, text, null, And, Or, and Not implementations are internal. Named operator alternatives support ordinary method calls. The conditional truth operator has a focused CA2225 exception because truth requires a candidate. Core has no `ISpecification`, `Field`, `SpecificationNode`, `Rule`, or factory abstraction. Framework constructors keep the grammar closed; application helpers return combinations of existing selections and specifications.

Selections and completed specifications are immutable. Membership values are copied immediately. Root candidate and member values must remain stable during an evaluation. Logical evaluation and SQL translation use iterative traversal without a fixed framework nesting limit or a stored depth counter. Database expression limits still apply to executing generated SQL.

Prefer `&&` and `||` for composition. They construct both sides and produce the same specifications as `&` and `|`. Candidate evaluation short-circuits AND and OR regardless of which syntax constructed them. C# enables conditional composition through the paired `true` and `false` operators, both of which return false because a specification has no truth value without a candidate. TSPEC017 rejects direct Boolean conditions such as `if (specification)`; call `specification.IsSatisfiedBy(candidate)` instead.

Runtime targets are .NET 8, 9, and 10; generation requires C# 14. Runtime code supports trimming and Native AOT. See [semantics](https://github.com/TedToolkit/TedToolkit.Specifications/blob/development/docs/semantics.md), [diagnostics](https://github.com/TedToolkit/TedToolkit.Specifications/blob/development/docs/diagnostics.md), and [repository verification](https://github.com/TedToolkit/TedToolkit.Specifications/blob/development/README.md#development-and-shared-ci).

All Analyzer titles and messages use strings generated by `TedToolkit.Localizations`, with English base text and a `zh-CN` resource. Language selection and fallback follow `LocalizationSettings.Culture`; diagnostic descriptors capture those strings when initialized. No localization dependency is added to the application's runtime dependency graph.
