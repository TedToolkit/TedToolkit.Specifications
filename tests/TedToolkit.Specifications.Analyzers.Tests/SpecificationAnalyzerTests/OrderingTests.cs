// -----------------------------------------------------------------------
// <copyright file="OrderingTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationAnalyzerTests;

internal sealed class OrderingTests
{
    /// <summary>
    /// Verifies sealed non-comparable values receive an ordering diagnostic before evaluation can throw.
    /// </summary>
    [Test]
    [Arguments("public sealed record Entity(int Score);", "Entity", "new Entity(1)")]
    [Arguments("public readonly record struct Entity(int Score);", "Entity", "new Entity(1)")]
    [Arguments("public readonly record struct Entity(int Score);", "Entity?", "new Entity(1)")]
    [Arguments("", "int[]", "new[] { 1 }")]
    public async Task Should_reject_statically_non_comparable_orderingAsync(string declaration, string type, string value)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            {{declaration}}
            public static class Usage
            {
                public static object Run() => Selection.For<{{type}}>().GreaterThan({{value}});
                public static object Binary() => Selection.For<{{type}}>() > {{value}};
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Compilation.GetDiagnostics().Where(diagnostic =>
            diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(probe.Diagnostics.Count(diagnostic => diagnostic.Id == "TSPEC013")).IsEqualTo(2);
    }

    /// <summary>
    /// Verifies generic, non-generic, and contravariant comparison interfaces remain supported by core ordering.
    /// </summary>
    [Test]
    [Arguments("System.IComparable<Entity>", "Entity?", "Entity")]
    [Arguments("System.IComparable", "object?", "Entity")]
    [Arguments("System.IComparable<object>", "object?", "Entity")]
    [Arguments("System.IComparable<Entity>", "Entity?", "Entity?")]
    public async Task Should_allow_comparable_value_objectsAsync(string comparison, string argument, string type)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public sealed record Entity(int Score) : {{comparison}}
            {
                public int CompareTo({{argument}} other) => Score.CompareTo((other as Entity)?.Score ?? 0);
            }
            public static class Usage
            {
                public static bool Run() => (Selection.For<{{type}}>() > new Entity(1)).IsSatisfiedBy(new Entity(2));
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Evaluate()).IsTrue();
    }

    /// <summary>
    /// Verifies an open CLR runtime type can still supply an ordinarily comparable derived instance.
    /// </summary>
    [Test]
    public async Task Should_allow_runtime_comparable_values_when_static_type_is_openAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public static class Usage
            {
                public static bool Run() => (Selection.For<object>() > 1).IsSatisfiedBy(2);
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Evaluate()).IsTrue();
    }

    /// <summary>
    /// Verifies comparable CLR values remain available without the SQL storage whitelist.
    /// </summary>
    [Test]
    [Arguments("System.TimeSpan", "System.TimeSpan.Zero", "System.TimeSpan.FromSeconds(1)")]
    [Arguments("System.Numerics.BigInteger", "System.Numerics.BigInteger.Zero", "System.Numerics.BigInteger.One")]
    [Arguments("System.Int128", "System.Int128.Zero", "System.Int128.One")]
    [Arguments("System.UInt128", "System.UInt128.Zero", "System.UInt128.One")]
    public async Task Should_allow_core_ordering_and_reject_unsupported_sql_storageAsync(string type, string zero, string one)
    {
        var source = $$"""
            using TedToolkit.Specifications;
            public static class Usage
            {
                public static bool Run()
                {
                    var selection = Selection.For<{{type}}>();
                    return (selection > {{zero}}).IsSatisfiedBy({{one}})
                        && selection.GreaterThan({{zero}}).IsSatisfiedBy({{one}});
                }
            }
            """;
        var core = await CompilationProbe.RunAsync(source, includeSql: false).ConfigureAwait(false);
        await Assert.That(core.Errors).IsEmpty();
        await Assert.That(core.Diagnostics).IsEmpty();
        await Assert.That(core.Evaluate()).IsTrue();
        var sql = await CompilationProbe.RunAsync(source).ConfigureAwait(false);
        await Assert.That(sql.Diagnostics.Count(diagnostic => diagnostic.Id == "TSPEC002")).IsEqualTo(2);
        await Assert.That(sql.Diagnostics.Select(diagnostic => diagnostic.Id)).DoesNotContain("TSPEC013");
    }

    /// <summary>
    /// Verifies ordering prohibited by the core contract remains diagnosed without SQL references.
    /// </summary>
    [Test]
    [Arguments("string", "\"a\"")]
    [Arguments("bool", "false")]
    [Arguments("System.Guid", "System.Guid.Empty")]
    [Arguments("System.DayOfWeek", "System.DayOfWeek.Monday")]
    public async Task Should_reject_unsupported_core_orderingAsync(string type, string value)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public static class Usage { public static object Run() => Selection.For<{{type}}>().GreaterThan({{value}}); }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Diagnostics.Single().Id).IsEqualTo("TSPEC013");
    }
}