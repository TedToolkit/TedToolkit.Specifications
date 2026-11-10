---
name: build-typed-specifications
description: >-
  Use TedToolkit.Specifications for generated C# predicates, in-memory evaluation, and parameterized SQLite or PostgreSQL translation.
---

# Build Typed Specifications

Use C# 14 with the SDK required by the package. Start with `Selection.For<T>()`, follow generated member accessors, and compare the selected values to build a `Specification<T>`.

```csharp
using TedToolkit.Specifications;

var order = Selection.For<Order>();
var eligible = order.Amount > 100m;
```

Use `IsSatisfiedBy` for in-memory evaluation. Compose actual predicates with `And`, `Or`, and `Not`. Check nested nullable members and include values exactly on comparison boundaries.

Use [SQL translation](references/sql.md) for a database condition or projection. Check that supported in-memory and translated predicates select the same boundary rows.
