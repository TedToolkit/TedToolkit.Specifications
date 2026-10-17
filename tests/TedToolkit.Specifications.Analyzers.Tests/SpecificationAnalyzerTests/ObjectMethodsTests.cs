// -----------------------------------------------------------------------
// <copyright file="ObjectMethodsTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationAnalyzerTests;

internal sealed class ObjectMethodsTests
{
    /// <summary>
    /// Verifies casts and reversed equality cannot hide object operations on DSL instances.
    /// </summary>
    [Test]
    [Arguments("((object)selection).Equals(1)")]
    [Arguments("((object)selection).ToString()")]
    [Arguments("((object)selection).GetType()")]
    [Arguments("((object)selection).GetHashCode()")]
    [Arguments("1.Equals(selection)")]
    [Arguments("\"value\".Equals(selection)")]
    [Arguments("object.Equals(1, selection)")]
    [Arguments("object.ReferenceEquals(selection, null)")]
    [Arguments("((object)(selection > 0)).Equals(true)")]
    public async Task Should_diagnose_object_operations_through_conversionsAsync(string expression)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public static class Usage
            {
                public static object? Run()
                {
                    var selection = Selection.For<int>();
                    return {{expression}};
                }
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Compilation.GetDiagnostics().Where(diagnostic =>
            diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(probe.Diagnostics.Single().Id).IsEqualTo("TSPEC008");
    }

    /// <summary>
    /// Verifies ordinary object methods and user helpers with matching names remain valid.
    /// </summary>
    [Test]
    [Arguments("1.Equals(1)")]
    [Arguments("\"text\".ToString() == \"text\"")]
    [Arguments("Helpers.Equals(Selection.For<int>())")]
    [Arguments("Selection.For<int>().ToString(1)")]
    public async Task Should_allow_unrelated_object_methods_and_helpersAsync(string expression)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public static class Helpers
            {
                public static bool Equals(object value) => true;
                public static bool ToString(this Selection<int, int> value, int format) => true;
            }
            public static class Usage { public static bool Run() => {{expression}}; }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Evaluate()).IsTrue();
    }
}