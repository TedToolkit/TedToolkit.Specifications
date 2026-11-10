---
name: build-typed-specifications
description: >-
  Use TedToolkit.Specifications to build generated typed member selections and compose reusable business predicates in C#.
---

# Build Typed Specifications

- Start with `Selection.For<T>()` and use the generated member chain to select the required value. Compare selections with the operators exposed by the package.
- Compose `Specification<T>` predicates with `And`, `Or`, or `Not` and evaluate with `IsSatisfiedBy`. Check the behavior for optional nested members.
- Build with C# 14 to surface generator diagnostics. Cover the rule’s boundary cases in memory and use the SQL package only when a database translation is needed.
