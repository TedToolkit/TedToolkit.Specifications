// -----------------------------------------------------------------------
// <copyright file="ReferencePatternsTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationAnalyzerTests;

internal sealed class ReferencePatternsTests
{
    /// <summary>
    /// Verifies compound patterns and both switch forms cannot test the DSL object's nullness silently.
    /// </summary>
    [Test]
    [Arguments("return selection is not null and {};")]
    [Arguments("return selection is {};")]
    [Arguments("return selection is null or {};")]
    [Arguments("return selection is not (null or null);")]
    [Arguments("return ((object)selection) is not null and {};")]
    [Arguments("return selection switch { null => false, _ => true };")]
    [Arguments("return selection switch { {} => true, _ => false };")]
    [Arguments("return ((object)selection) switch { null => false, _ => true };")]
    [Arguments("switch (selection) { case null: return false; default: return true; }")]
    [Arguments("switch ((object)selection) { case not null and {}: return true; default: return false; }")]
    [Arguments("return (selection > 0) is {};")]
    [Arguments("return selection is object;")]
    [Arguments("return selection is object value;")]
    [Arguments("return selection is not object;")]
    [Arguments("return selection switch { object value => true, _ => false };")]
    [Arguments("return (selection > 0) is object;")]
    [Arguments("return Selection.For<Entity>() is { Score: not null };")]
    [Arguments("return Selection.For<Entity>() is { Score: {} };")]
    [Arguments("return Selection.For<Entity>() switch { { Score: not null } => true, _ => false };")]
    [Arguments("switch (Selection.For<Entity>()) { case { Score: not null and {} }: return true; default: return false; }")]
    public async Task Should_diagnose_reference_patternsAsync(string statement)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public sealed record Entity(int Score);
            public static class Usage
            {
                public static bool Run()
                {
                    var selection = Selection.For<int>();
                    {{statement}}
                }
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Compilation.GetDiagnostics().Where(diagnostic =>
            diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(probe.Diagnostics.Single().Id).IsEqualTo("TSPEC016");
    }

    /// <summary>
    /// Verifies patterns applied to candidates and explicitly evaluated results remain ordinary C#.
    /// </summary>
    [Test]
    [Arguments("\"value\" is not null and {}")]
    [Arguments("\"value\" switch { null => false, _ => true }")]
    [Arguments("Selection.For<string?>().IsNotNull().IsSatisfiedBy(\"value\") is true")]
    [Arguments("Selection.For<int>() switch { _ => true }")]
    [Arguments("new Entity(1) is { Score: > 0 }")]
    [Arguments("new Entity(1) is object")]
    [Arguments("Selection.For<int>() is var value && (value == 1).IsSatisfiedBy(1)")]
    [Arguments("Selection.For<Entity>() is { Score: var score } && (score > 0).IsSatisfiedBy(new Entity(1))")]
    public async Task Should_allow_patterns_without_reference_tests_on_dsl_instancesAsync(string expression)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public sealed record Entity(int Score);
            public static class Usage { public static bool Run() => {{expression}}; }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Evaluate()).IsTrue();
    }
}