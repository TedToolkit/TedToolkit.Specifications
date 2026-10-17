// -----------------------------------------------------------------------
// <copyright file="SourceTypesTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationGeneratorTests;

internal sealed class SourceTypesTests
{
    /// <summary>
    /// Verifies special CLR source names remain legal throughout generated sibling property signatures.
    /// </summary>
    [Test]
    [Arguments("dynamic")]
    [Arguments("nint")]
    [Arguments("nuint")]
    [Arguments("System.IntPtr")]
    [Arguments("System.UIntPtr")]
    [Arguments("System.Collections.Generic.List<dynamic>")]
    [Arguments("dynamic[]")]
    [Arguments("System.Collections.Generic.List<nint?[]>")]
    public async Task Should_preserve_special_type_namesAsync(string type)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public sealed class Entity
            {
                public {{type}} Payload { get; set; } = default!;
                public int Score { get; set; }
            }
            public static class Usage
            {
                public static bool Run() => (Selection.For<Entity>().Score == 42).IsSatisfiedBy(new Entity { Score = 42 });
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Evaluate()).IsTrue();
    }

    /// <summary>
    /// Verifies unnamed generic factory arguments do not corrupt unrelated generated code or root null checks.
    /// </summary>
    [Test]
    [Arguments("new { Score = 1 }")]
    [Arguments("new[] { new { Score = 1 } }.ToList()")]
    [Arguments("new[] { (new { Score = 1 }, 2) }.ToList()")]
    public async Task Should_skip_unnameable_models_without_invalid_generated_sourceAsync(string value)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using System.Linq;
            using TedToolkit.Specifications;
            public sealed record Entity(int Score);
            public static class Usage
            {
                private static Selection<T, T> Select<T>(T value) => Selection.For<T>();
                public static bool Run()
                {
                    var value = {{value}};
                    var anonymous = Select(value);
                    var candidate = new Entity(42);
                    return !anonymous.IsNull().IsSatisfiedBy(value)
                        && (Selection.For<Entity>().Score == 42).IsSatisfiedBy(candidate);
                }
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Generation.Results.Any(result => result.Exception is not null)).IsFalse();
        await Assert.That(probe.Evaluate()).IsTrue();
    }
}