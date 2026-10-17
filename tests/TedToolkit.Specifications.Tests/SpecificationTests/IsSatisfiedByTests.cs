// -----------------------------------------------------------------------
// <copyright file="IsSatisfiedByTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Tests.SpecificationTests;

internal sealed class IsSatisfiedByTests
{
    /// <summary>
    /// Verifies all scalar comparisons against nullable C# value semantics.
    /// </summary>
    [Test]
    [Arguments(null, null)]
    [Arguments(null, 1)]
    [Arguments(1, null)]
    [Arguments(0, 0)]
    [Arguments(-1, 1)]
    [Arguments(1, -1)]
    [Arguments(int.MinValue, int.MaxValue)]
    public async Task Should_match_nullable_comparison_semanticsAsync(int? actual, int? operand)
    {
        var field = Selection.For<Entity>().Number;
        var entity = new Entity(actual, null);
        await Assert.That((field == operand).IsSatisfiedBy(entity)).IsEqualTo(actual == operand);
        await Assert.That((field != operand).IsSatisfiedBy(entity)).IsEqualTo(actual != operand);
        await Assert.That((field > operand).IsSatisfiedBy(entity)).IsEqualTo(actual > operand);
        await Assert.That((field >= operand).IsSatisfiedBy(entity)).IsEqualTo(actual >= operand);
        await Assert.That((field < operand).IsSatisfiedBy(entity)).IsEqualTo(actual < operand);
        await Assert.That((field <= operand).IsSatisfiedBy(entity)).IsEqualTo(actual <= operand);
        await Assert.That(!(field > operand).IsSatisfiedBy(entity)).IsEqualTo(!(actual > operand));
    }

    /// <summary>
    /// Verifies that null, empty, Unicode, and SQL wildcard strings retain ordinal semantics.
    /// </summary>
    [Test]
    [Arguments(null, "")]
    [Arguments("", "")]
    [Arguments("Ada", "ada")]
    [Arguments("a%b_c\\d", "%b_")]
    [Arguments("a😀é", "😀")]
    [Arguments("e\u0301", "é")]
    [Arguments("a ", "a")]
    public async Task Should_use_ordinal_string_matchingAsync(string? actual, string operand)
    {
        var field = Selection.For<Entity>().Text;
        var entity = new Entity(null, actual);
        await Assert.That(field.Contains(operand).IsSatisfiedBy(entity))
            .IsEqualTo(actual?.Contains(operand, StringComparison.Ordinal) == true);
        await Assert.That(field.StartsWith(operand).IsSatisfiedBy(entity))
            .IsEqualTo(actual?.StartsWith(operand, StringComparison.Ordinal) == true);
        await Assert.That(field.EndsWith(operand).IsSatisfiedBy(entity))
            .IsEqualTo(actual?.EndsWith(operand, StringComparison.Ordinal) == true);
        await Assert.That((field == operand).IsSatisfiedBy(entity)).IsEqualTo(string.Equals(actual, operand, StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies the truth table of AND, OR, and NOT through the public DSL.
    /// </summary>
    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task Should_follow_boolean_truth_tablesAsync(bool left, bool right)
    {
        var fields = Selection.For<Entity>();
        var entity = new Entity(left ? 1 : 0, null, right);
        var first = fields.Number.Compare(1, ComparisonOperator.EQUAL);
        var second = fields.Enabled.EqualTo(true);
        await Assert.That((first & second).IsSatisfiedBy(entity)).IsEqualTo(left && right);
        await Assert.That((first | second).IsSatisfiedBy(entity)).IsEqualTo(left || right);
        await Assert.That((first && second).IsSatisfiedBy(entity)).IsEqualTo(left && right);
        await Assert.That((first || second).IsSatisfiedBy(entity)).IsEqualTo(left || right);
        await Assert.That(first.BitwiseAnd(second).IsSatisfiedBy(entity)).IsEqualTo(left && right);
        await Assert.That(first.BitwiseOr(second).IsSatisfiedBy(entity)).IsEqualTo(left || right);
        await Assert.That(first.LogicalNot().IsSatisfiedBy(entity)).IsEqualTo(!left);
        await Assert.That((!first).IsSatisfiedBy(entity)).IsEqualTo(!left);
    }

    /// <summary>
    /// Verifies that null entities are rejected even for constant specifications.
    /// </summary>
    [Test]
    public async Task Should_reject_null_entitiesAsync()
    {
        await Assert.That(() => Specification.True<Entity>().IsSatisfiedBy(null!)).Throws<ArgumentNullException>();
    }
}