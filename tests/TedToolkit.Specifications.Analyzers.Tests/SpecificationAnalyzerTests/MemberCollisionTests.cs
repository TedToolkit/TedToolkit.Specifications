// -----------------------------------------------------------------------
// <copyright file="MemberCollisionTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationAnalyzerTests;

internal sealed class MemberCollisionTests
{
    /// <summary>
    /// Verifies a DSL method does not select a computed model property that happens to have the same name.
    /// </summary>
    [Test]
    public async Task Should_allow_dsl_methods_sharing_computed_property_namesAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public sealed class Entity { public bool IsNull => true; }
            public static class Usage
            {
                public static bool Run() => !Selection.For<Entity>().IsNull().IsSatisfiedBy(new Entity());
            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Evaluate()).IsTrue();
    }

    /// <summary>
    /// Verifies conflicting property names receive a precise diagnostic instead of being described as unreadable getters.
    /// </summary>
    [Test]
    [Arguments("Compare")]
    [Arguments("EqualTo")]
    [Arguments("NotEqualTo")]
    [Arguments("GreaterThan")]
    [Arguments("GreaterThanOrEqual")]
    [Arguments("LessThan")]
    [Arguments("LessThanOrEqual")]
    [Arguments("In")]
    [Arguments("IsNull")]
    [Arguments("IsNotNull")]
    [Arguments("Contains")]
    [Arguments("StartsWith")]
    [Arguments("EndsWith")]
    [Arguments("Equals")]
    [Arguments("GetHashCode")]
    [Arguments("ToString")]
    [Arguments("GetType")]
    public async Task Should_identify_existing_instance_member_namesAsync(string name)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public class Entity { public int {{name}} { get; set; } }
            public static class Usage { public static object Run() => Selection.For<Entity>().{{name}} > 0; }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Diagnostics.Select(diagnostic => diagnostic.Id)).Contains("TSPEC018");
        await Assert.That(probe.Diagnostics.Select(diagnostic => diagnostic.Id)).DoesNotContain("TSPEC003");
        await Assert.That(probe.Generation.Results.Any(result => result.Exception is not null)).IsFalse();
    }

    /// <summary>
    /// Verifies genuine selection method calls and method-group delegates remain valid when a model has a namesake property.
    /// </summary>
    [Test]
    public async Task Should_allow_method_calls_and_delegates_with_unused_namesake_propertiesAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public sealed record Entity(int Compare, int IsNull, int Name);
            public static class Usage
            {
                public static bool Run()
                {
                    var selection = Selection.For<Entity>();
                    System.Func<Entity, ComparisonOperator, Specification<Entity>> compare = selection.Compare;
                    var candidate = new Entity(1, 2, 3);
                    return !selection.IsNull().IsSatisfiedBy(candidate)
                        && compare(candidate, ComparisonOperator.EQUAL).IsSatisfiedBy(candidate)
                        && (selection.Name == 3).IsSatisfiedBy(candidate);
                }
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Evaluate()).IsTrue();
    }
}