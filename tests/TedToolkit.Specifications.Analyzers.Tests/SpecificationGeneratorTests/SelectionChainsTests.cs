// -----------------------------------------------------------------------
// <copyright file="SelectionChainsTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationGeneratorTests;

internal sealed class SelectionChainsTests
{
    /// <summary>
    /// Verifies wrapped selection chains preserve the selected path and null-parent behavior through actual evaluation.
    /// </summary>
    [Test]
    [Arguments("root.Child!", false)]
    [Arguments("(root.Child!)", false)]
    [Arguments("choose ? root.Child : root.Other", true)]
    [Arguments("choose ? root.Child! : root.Other!", true)]
    [Arguments("root.Child ?? root.Other", false)]
    [Arguments("choose switch { true => root.Child, false => root.Other }", true)]
    [Arguments("Identity(root.Child)", false)]
    [Arguments("root.Child.IdentityExtension()", false)]
    [Arguments("root.Child.IdentitySelection()", false)]
    public async Task Should_resolve_wrapped_selection_aliasesAsync(string expression, bool branches)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public sealed record Root(Child? Child, Child? Other);
            public sealed record Child(Leaf? Leaf);
            public sealed record Leaf(string? Name);
            public static class Helpers
            {
                public static T IdentityExtension<T>(this T value) => value;
                public static Selection<TCandidate, TValue> IdentitySelection<TCandidate, TValue>(this Selection<TCandidate, TValue> value)
                    => value;
            }
            public static class Usage
            {
                private static T Identity<T>(T value) => value;
                private static Specification<Root> Build(bool choose)
                {
                    var root = Selection.For<Root>();
                    var child = {{expression}};
                    return child.Leaf.Name == "Ada";
                }
                public static bool Run()
                {
                    var candidate = new Root(new Child(new Leaf("Ada")), new Child(new Leaf("Bob")));
                    return Build(true).IsSatisfiedBy(candidate)
                        && Build(false).IsSatisfiedBy(candidate) == {{(branches ? "false" : "true")}}
                        && !Build(true).IsSatisfiedBy(new Root(null, null))
                        && !Build(true).IsSatisfiedBy(new Root(new Child(null), null));
                }
            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Evaluate()).IsTrue();
    }

    /// <summary>
    /// Verifies concrete object results do not expand model members that the application has not selected.
    /// </summary>
    [Test]
    [Arguments("choose ? root.Child : new object()")]
    [Arguments("choose switch { true => root.Child, false => new object() }")]
    [Arguments("Identity<object>(root.Child)")]
    [Arguments("Merge(root.Child, new object())")]
    [Arguments("Identity((object)root.Child)")]
    [Arguments("choose ? (object)root.Child : new object()")]
    public async Task Should_stop_inference_at_known_non_selection_resultsAsync(string expression)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public class Parent { public int Hidden { get; set; } }
            public sealed class Child : Parent { public new int Hidden { get; set; } }
            public sealed record Root(Child? Child);
            public static class Helpers { public static bool Accept(this object value) => true; }
            public static class Usage
            {
                private static T Identity<T>(T value) => value;
                private static T Merge<T>(T first, T second) => second;
                private static bool Build(bool choose)
                {
                    var root = Selection.For<Root>();
                    var opaque = {{expression}};
                    return opaque.Accept();
                }
                public static bool Run() => Build(true) && Build(false);
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Generation.GeneratedTrees.Length).IsEqualTo(1);
        await Assert.That(probe.Evaluate()).IsTrue();
    }

    /// <summary>
    /// Verifies selections from different candidate roots do not become one selected model through an object-typed conditional.
    /// </summary>
    [Test]
    public async Task Should_preserve_distinct_candidate_types_in_object_conditionalsAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public class Parent { public int Hidden { get; set; } }
            public sealed class Child : Parent { public new int Hidden { get; set; } }
            public sealed record First(Child? Child);
            public sealed record Second(Child? Child);
            public static class Helpers { public static bool Accept(this object value) => true; }
            public static class Usage
            {
                private static bool Build(bool choose)
                {
                    var first = Selection.For<First>();
                    var second = Selection.For<Second>();
                    object opaque = choose ? first.Child : second.Child;
                    return opaque.Accept();
                }
                public static bool Run() => Build(true) && Build(false);
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Generation.GeneratedTrees.Length).IsEqualTo(2);
        await Assert.That(probe.Evaluate()).IsTrue();
    }

    /// <summary>
    /// Verifies conditional access and pattern bindings request every used level without recursively expanding unused children.
    /// </summary>
    [Test]
    [Arguments("var name = root.Child?.Leaf.Name; return (name! == \"Ada\").IsSatisfiedBy(candidate);")]
    [Arguments("return (root.Child!.Leaf!.Name == \"Ada\").IsSatisfiedBy(candidate);")]
    [Arguments("return root is { Child: var child } && (child.Leaf.Name == \"Ada\").IsSatisfiedBy(candidate);")]
    [Arguments("return root is { Child: { Leaf: var leaf } } && (leaf.Name == \"Ada\").IsSatisfiedBy(candidate);")]
    [Arguments("return root switch { { Child: var child } => (child.Leaf.Name == \"Ada\").IsSatisfiedBy(candidate), _ => false };")]
    public async Task Should_resolve_conditional_access_and_pattern_variablesAsync(string statement)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public sealed record Root(Child? Child);
            public sealed record Child(Leaf? Leaf);
            public sealed record Leaf(string? Name);
            public static class Usage
            {
                public static bool Run()
                {
                    var root = Selection.For<Root>();
                    var candidate = new Root(new Child(new Leaf("Ada")));
                    {{statement}}
                }
            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Generation.GeneratedTrees.Length).IsEqualTo(3);
        await Assert.That(probe.Evaluate()).IsTrue();
    }

    /// <summary>
    /// Verifies nested and extended property patterns receive legal accessors and the intended DSL identity diagnostic.
    /// </summary>
    [Test]
    [Arguments("root is { Child: { Leaf: { Name: not null } } }")]
    [Arguments("root is { Child.Leaf.Name: not null }")]
    public async Task Should_diagnose_nested_reference_patterns_after_generating_the_chainAsync(string expression)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public sealed record Root(Child? Child);
            public sealed record Child(Leaf? Leaf);
            public sealed record Leaf(string? Name);
            public static class Usage
            {
                public static bool Run()
                {
                    var root = Selection.For<Root>();
                    return {{expression}};
                }
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Compilation.GetDiagnostics().Where(diagnostic =>
            diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(probe.Diagnostics.Single().Id).IsEqualTo("TSPEC016");
    }
}