// -----------------------------------------------------------------------
// <copyright file="ScalarTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Tests.SpecificationTests;

internal sealed class ScalarTests
{
    /// <summary>
    /// Verifies scalar comparisons preserve the candidate type and produce composable specifications.
    /// </summary>
    [Test]
    public async Task Should_build_scalar_specifications_from_selectionsAsync()
    {
        var value = Selection.For<int>();
        var rule = (value > 1) & (value <= 5);
        await Assert.That(rule.IsSatisfiedBy(3)).IsTrue();
        await Assert.That(rule.IsSatisfiedBy(1)).IsFalse();
        await Assert.That(rule.And(value != 4).IsSatisfiedBy(4)).IsFalse();
        await Assert.That((!rule).Or(value == 3).IsSatisfiedBy(3)).IsTrue();
        await Assert.That(Selection.For<bool>().EqualTo(true).IsSatisfiedBy(false)).IsFalse();
    }

    /// <summary>
    /// Verifies null operators build actual specifications for nullable scalar selections.
    /// </summary>
    [Test]
    public async Task Should_support_nullable_scalarsAsync()
    {
        var value = Selection.For<int?>();
        await Assert.That(value.IsNull().IsSatisfiedBy(null)).IsTrue();
        await Assert.That(value.EqualTo(null).IsSatisfiedBy(null)).IsTrue();
        await Assert.That(value.GreaterThan(1).Not().IsSatisfiedBy(null)).IsTrue();
        var text = Selection.For<string?>();
        await Assert.That(text.Contains("a").IsSatisfiedBy(null)).IsFalse();
        await Assert.That(text.EqualTo(null).IsSatisfiedBy(null)).IsTrue();
    }

    /// <summary>
    /// Verifies selectors cannot evaluate candidates or compose predicates, including Boolean selectors.
    /// </summary>
    [Test]
    public async Task Should_keep_selection_and_predicate_contracts_distinctAsync()
    {
        await Assert.That(typeof(Selection<,>).GetMethod("IsSatisfiedBy")).IsNull();
        await Assert.That(typeof(Selection<bool, bool>).GetMethod("And")).IsNull();
        await Assert.That(typeof(Specification<>).GetMethod("IsSatisfiedBy")).IsNotNull();
    }
}