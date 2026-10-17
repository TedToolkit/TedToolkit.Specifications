// -----------------------------------------------------------------------
// <copyright file="NestedAccessTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationGeneratorTests;

internal sealed class NestedAccessTests
{
    private const string MODELS = """
        using TedToolkit.Specifications;
        public sealed record Root(Child? Child, decimal Amount);
        public sealed record Child(Leaf? Leaf, int Score);
        public sealed record Leaf(string? Name);
        """;

    /// <summary>
    /// Verifies successive member access preserves the root type through nullable parents and local aliases.
    /// </summary>
    [Test]
    public async Task Should_compile_nested_access_and_preserve_the_root_candidateAsync()
    {
        var probe = await CompilationProbe.RunAsync(MODELS + """
            public static class Usage
            {
                public static Specification<Root> Build()
                {
                    var root = Selection.For<Root>();
                    var child = root.Child;
                    Selection<Root, string?> name = child.Leaf.Name;
                    return (name == null) | ((name == "Ada") & (child.Score > 0));
                }
            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Warning).ToArray())
            .IsEmpty();
        var image = new MemoryStream();

        await using var imageLifetime = image.ConfigureAwait(false);
        await Assert.That(probe.Compilation.Emit(image).Success).IsTrue();
    }

    /// <summary>
    /// Verifies a selected property does not recursively generate its property's own extensions before another dot is used.
    /// </summary>
    [Test]
    public async Task Should_generate_only_the_requested_member_levelsAsync()
    {
        var first = await CompilationProbe.RunAsync(
            MODELS + "public class Usage { public object Run() => Selection.For<Root>().Child; }").ConfigureAwait(false);
        await Assert.That(first.Errors).IsEmpty();
        await Assert.That(first.Generation.GeneratedTrees.Length).IsEqualTo(1);
        var second = await CompilationProbe.RunAsync(
            MODELS + "public class Usage { public object Run() => Selection.For<Root>().Child.Leaf; }").ConfigureAwait(false);
        await Assert.That(second.Errors).IsEmpty();
        await Assert.That(second.Generation.GeneratedTrees.Length).IsEqualTo(2);
        var incomplete = await CompilationProbe.RunAsync(
            MODELS + "public class Usage { public object Run() => Selection.For<Root>().Child.; }").ConfigureAwait(false);
        await Assert.That(incomplete.Generation.GeneratedTrees.Length).IsEqualTo(2);
        await Assert.That(incomplete.Diagnostics.Any(item => item.Id == "AD0001")).IsFalse();
    }

    /// <summary>
    /// Verifies recursive model graphs generate finite extensions based on actual member usage.
    /// </summary>
    [Test]
    public async Task Should_handle_recursive_models_and_closed_generic_membersAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public record Node(Node? Next, System.Collections.Generic.List<Node> Children, int Number);
            public class Usage
            {
                public Specification<Node> Build() => (Selection.For<Node>().Next.Next.Number > 0)
                    & (Selection.For<Node>().Children.Count > 0);
            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Generation.GeneratedTrees.Length).IsEqualTo(2);
        await Assert.That(probe.Compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Warning).ToArray())
            .IsEmpty();
    }

    /// <summary>
    /// Verifies only business specifications can evaluate candidates; Boolean selections remain selections.
    /// </summary>
    [Test]
    [Arguments("Selection.For<Root>().IsSatisfiedBy(null!)", "TSPEC006")]
    [Arguments("Selection.For<bool>().IsSatisfiedBy(true)", "TSPEC006")]
    public async Task Should_reject_evaluation_of_a_selectionAsync(string expression, string id)
    {
        var probe = await CompilationProbe.RunAsync(MODELS + "public class Usage { public object Run() => " + expression + "; }")
            .ConfigureAwait(false);
        await Assert.That(probe.Diagnostics.Select(item => item.Id)).Contains(id);
        await Assert.That(probe.Compilation.GetDiagnostics().Any(item => item.Severity == DiagnosticSeverity.Error)).IsTrue();
    }

    /// <summary>
    /// Verifies the compiler rejects implicit conversion from a Boolean selection to a specification.
    /// </summary>
    [Test]
    public async Task Should_require_an_explicit_predicate_for_boolean_valuesAsync()
    {
        var probe = await CompilationProbe.RunAsync(
            "using TedToolkit.Specifications; public class Usage { public Specification<bool> Build() => Selection.For<bool>(); }")
            .ConfigureAwait(false);
        await Assert.That(probe.Compilation.GetDiagnostics().Any(item => item.Id == "CS0029")).IsTrue();
    }
}