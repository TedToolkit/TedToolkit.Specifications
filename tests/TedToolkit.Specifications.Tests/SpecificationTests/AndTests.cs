// -----------------------------------------------------------------------
// <copyright file="AndTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Tests.SpecificationTests;

internal sealed class AndTests
{
    /// <summary>
    /// Verifies composite specifications skip the right operand when its value cannot change the result.
    /// </summary>
    [Test]
    public async Task Should_short_circuit_the_right_specificationAsync()
    {
        var comparison = Selection.For<ThrowingCandidate>().Number > 0;
        var candidate = new ThrowingCandidate();
        await Assert.That((Specification.False<ThrowingCandidate>() & comparison).IsSatisfiedBy(candidate)).IsFalse();
        await Assert.That((Specification.True<ThrowingCandidate>() | comparison).IsSatisfiedBy(candidate)).IsTrue();
        await Assert.That(() => (Specification.True<ThrowingCandidate>() & comparison).IsSatisfiedBy(candidate))
            .Throws<InvalidOperationException>();
        await Assert.That(() => (Specification.False<ThrowingCandidate>() | comparison).IsSatisfiedBy(candidate))
            .Throws<InvalidOperationException>();
        await Assert.That((Specification.False<ThrowingCandidate>() && comparison).IsSatisfiedBy(candidate)).IsFalse();
        await Assert.That((Specification.True<ThrowingCandidate>() || comparison).IsSatisfiedBy(candidate)).IsTrue();
        await Assert.That(() => (Specification.True<ThrowingCandidate>() && comparison).IsSatisfiedBy(candidate))
            .Throws<InvalidOperationException>();
        await Assert.That(() => (Specification.False<ThrowingCandidate>() || comparison).IsSatisfiedBy(candidate))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies conditional operators construct both operands before any candidate is evaluated.
    /// </summary>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Should_construct_both_conditional_operandsAsync(bool leftMatches)
    {
        var constructions = 0;
        var left = leftMatches ? Specification.True<Entity>() : Specification.False<Entity>();
        Specification<Entity> Right()
        {
            constructions++;
            return Selection.For<Entity>().Number == 1;
        }

        var and = left && Right();
        var or = left || Right();
        await Assert.That(constructions).IsEqualTo(2);
        await Assert.That(and.IsSatisfiedBy(new Entity(1, null))).IsEqualTo(leftMatches);
        await Assert.That(or.IsSatisfiedBy(new Entity(0, null))).IsEqualTo(leftMatches);
    }

    /// <summary>
    /// Verifies deep compositions preserve their result and short-circuiting without recursive evaluation.
    /// </summary>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Should_evaluate_deep_compositions_without_a_depth_limitAsync(bool useAnd)
    {
        var leaf = Selection.For<int>() > 0;
        var rule = leaf;
        for (var index = 0; index < 100_000; index++)
        {
            rule = useAnd ? rule.And(leaf) : rule.Or(leaf);
        }

        await Assert.That(rule.IsSatisfiedBy(1)).IsTrue();
        await Assert.That(rule.IsSatisfiedBy(-1)).IsFalse();
    }

    /// <summary>
    /// Verifies deep negation and mixed continuations resume the correct branch without reading skipped getters.
    /// </summary>
    [Test]
    public async Task Should_resume_mixed_compositions_after_deep_negationAsync()
    {
        var leaf = Selection.For<ThrowingCandidate>().Number > 0;
        var candidate = new ThrowingCandidate();
        var rule = Specification.True<ThrowingCandidate>();
        for (var index = 0; index < 100_000; index++)
        {
            rule = !rule;
        }

        await Assert.That(rule.Or(leaf).IsSatisfiedBy(candidate)).IsTrue();
        await Assert.That((!rule).And(leaf).IsSatisfiedBy(candidate)).IsFalse();
        await Assert.That(rule.And(Specification.False<ThrowingCandidate>()).Not().IsSatisfiedBy(candidate)).IsTrue();
        await Assert.That(candidate.Reads).IsEqualTo(0);
    }
}