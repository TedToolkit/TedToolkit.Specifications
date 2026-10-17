// -----------------------------------------------------------------------
// <copyright file="InheritedMembersTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationGeneratorTests;

internal sealed class InheritedMembersTests
{
    /// <summary>
    /// Verifies ignored overloaded indexers cannot make ordinary readable properties ambiguous.
    /// </summary>
    [Test]
    public async Task Should_ignore_overloaded_indexers_before_checking_property_namesAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public class Entity
            {
                public int Score { get; set; }
                public int this[int index] => index;
                public string this[string key] => key;
            }
            public static class Usage
            {
                public static bool Run() => (Selection.For<Entity>().Score == 7).IsSatisfiedBy(new Entity { Score = 7 });
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Evaluate()).IsTrue();
    }

    /// <summary>
    /// Verifies inherited getters bind to their declaring type when ignored static, field, or private members reuse the name.
    /// </summary>
    [Test]
    [Arguments("public new static int Score { get; set; } = 99;")]
    [Arguments("public new string Score = \"hidden\";")]
    [Arguments("private new int Score { get; set; } = 99;")]
    [Arguments("public override int Score { get; set; }")]
    public async Task Should_bind_inherited_properties_to_their_declarationsAsync(string member)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public class Parent { public virtual int Score { get; set; } }
            public class Entity : Parent { {{member}} public int Other { get; set; } }
            public static class Usage
            {
                public static bool Run()
                {
                    var candidate = new Entity { Other = 1 };
                    ((Parent)candidate).Score = 7;
                    var entity = Selection.For<Entity>();
                    return ((entity.Score == 7) && (entity.Other == 1)).IsSatisfiedBy(candidate);
                }
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Evaluate()).IsTrue();
    }
}