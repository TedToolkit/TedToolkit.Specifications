// -----------------------------------------------------------------------
// <copyright file="NestedModelsTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationGeneratorTests;

internal sealed class NestedModelsTests
{
    private const string MODELS = """
        using TedToolkit.Specifications;
        public class Outer<T>
        {
            public class Entity { public int Score { get; set; } public T Value { get; set; } = default!; }
            public class Inner<U> { public int Score { get; set; } public U Value { get; set; } = default!; }
            public class Middle<U>
            {
                public class Inner<V> { public int Score { get; set; } public V Value { get; set; } = default!; }
            }
        }
        """;

    /// <summary>
    /// Verifies closed generic containing types and nested type arguments survive generation and direct getter execution.
    /// </summary>
    [Test]
    [Arguments("Outer<int>.Entity")]
    [Arguments("Outer<int>.Inner<string>")]
    [Arguments("Outer<int>.Middle<string>.Inner<decimal>")]
    public async Task Should_evaluate_closed_nested_generic_modelsAsync(string model)
    {
        var probe = await CompilationProbe.RunAsync(MODELS + $$"""
            public static class Usage
            {
                public static bool Run()
                {
                    var candidate = new {{model}} { Score = 42 };
                    var score = Selection.For<{{model}}>().Score;
                    return (score > 41).IsSatisfiedBy(candidate) && !(score > 42).IsSatisfiedBy(candidate);
                }
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Generation.Results.Any(result => result.Exception is not null)).IsFalse();
        await Assert.That(probe.Evaluate()).IsTrue();
    }

    /// <summary>
    /// Verifies nested generic types inside sibling properties, arrays, and generic arguments preserve ordinary property access.
    /// </summary>
    [Test]
    public async Task Should_preserve_nested_generic_siblings_and_member_pathsAsync()
    {
        var probe = await CompilationProbe.RunAsync(MODELS + """
            public sealed record Root(int Score, Outer<int>.Entity Child,
                System.Collections.Generic.List<Outer<int>.Inner<string>?>? Items, Outer<int>.Entity[]? Array,
                Outer<int>.Entity?[,]? Matrix, Outer<int>.Entity?[][]? Jagged,
                (Outer<int>.Entity? Child, int Value)? Pair);
            public static class Usage
            {
                public static bool Run()
                {
                    var candidate = new Root(7, new Outer<int>.Entity { Score = 42, Value = 9 }, null, null, null, null, null);
                    var root = Selection.For<Root>();
                    return (root.Score == 7).IsSatisfiedBy(candidate)
                        && (root.Child.Value == 9).IsSatisfiedBy(candidate);
                }
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Evaluate()).IsTrue();
    }

    /// <summary>
    /// Verifies an open containing type is diagnosed without crashing the generator or losing unrelated generated accessors.
    /// </summary>
    [Test]
    public async Task Should_diagnose_open_containing_types_without_generator_failureAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public class Outer<T>
            {
                public class Entity { public int Score { get; set; } }
                public static object Build() => Selection.For<Entity>();
            }
            public sealed record Valid(int Score);
            public static class Usage { public static object Run() => Selection.For<Valid>().Score > 0; }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Diagnostics.Select(diagnostic => diagnostic.Id)).Contains("TSPEC001");
        await Assert.That(probe.Generation.Results.Any(result => result.Exception is not null)).IsFalse();
        await Assert.That(probe.Compilation.GetDiagnostics().Any(diagnostic => diagnostic.Id is "CS8785" or "CS1061")).IsFalse();
    }
}